using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — the drawn future: the fading ribbon and the autopilot's rehearsed path, the pass and destination markers, the scrub-time ghosts, and the node markers with their nose vectors.
public partial class Map
{
    private void DrawShipTrajectory()
    {
        // #148: while the autopilot has the ship and a rehearsed plan to draw, the ballistic ribbon
        // shows loops the ship will never fly (the owner's report) — hand the line over to the
        // intended-path draw. The ballistic ribbon stays for manual flight.
        if (_armedOrbitBodyId is not null && _autopilotPlanPath is { Count: >= 2 }) return;
        if (_samples.Count < 2) return;

        int requiredSize = _samples.Count * 2;
        if (_scratch.Length < requiredSize)
        {
            _scratch = new float[requiredSize];
        }

        for (int i = 0; i < _samples.Count; i++)
        {
            (float x, float y) = _camera.WorldToScreen(PlotFrame(_samples[i].Position, _samples[i].SimTime));
            _scratch[i * 2] = x;
            _scratch[i * 2 + 1] = y;
        }

        // Sun frame (or a frame with no local orbit): the pre-#145 flat polyline, byte-identical.
        double? window = FrameDisplayWindowSeconds();
        if (window is null)
        {
            _renderer!.DrawPolyline(_scratch.AsSpan(0, _samples.Count * 2), TrajectoryColor);
            return;
        }

        // A Hill-sphere frame: draw only the first `window` seconds of the ribbon, ending SOFTLY with
        // the #110 fade. The full-length data stays in _samples — scrubbing, ETA, closest-pass and
        // node markers all keep reading it; only the drawn ribbon is clipped. (Node markers past the
        // window still draw as dots — just without the connecting ribbon; the floor rule guarantees
        // the NEXT node is inside the solid part.)
        double cutoff = _ship.SimTime + window.Value;
        double fadeSpan = Math.Clamp(window.Value * FrameRibbonFadeFraction, FrameRibbonFadeMinSeconds, FrameRibbonFadeMaxSeconds);
        double fadeStart = cutoff - fadeSpan;

        // Last vertex to draw: the first sample at/after the cutoff (so the fade reaches zero just past
        // it), capped to the data.
        int end = _samples.Count - 1;
        for (int i = 1; i < _samples.Count; i++)
        {
            if (_samples[i].SimTime >= cutoff)
            {
                end = i;
                break;
            }
        }
        if (end < 1)
        {
            end = 1;
        }

        // Coalesce consecutive same-alpha segments into single DrawPolyline runs (the #110 idiom):
        // alpha is quantized into buckets, so a long ribbon still strokes in a handful of runs. Runs
        // are contiguous index ranges into _scratch and share a vertex, so the line stays connected.
        int runStartVertex = 0;
        int runBucket = FadeBucket(0.5 * (_samples[0].SimTime + _samples[1].SimTime), fadeStart, fadeSpan);
        for (int seg = 2; seg <= end; seg++)
        {
            int bucket = FadeBucket(0.5 * (_samples[seg - 1].SimTime + _samples[seg].SimTime), fadeStart, fadeSpan);
            if (bucket != runBucket)
            {
                EmitRibbonRun(runStartVertex, seg - 1, runBucket);
                runStartVertex = seg - 1;
                runBucket = bucket;
            }
        }
        EmitRibbonRun(runStartVertex, end, runBucket);
    }

    // One run of the faded ribbon: vertices [firstVertex..lastVertex] (a contiguous slice of _scratch)
    // stroked at TrajectoryColor scaled by the run's alpha bucket. Bucket 0 = invisible, skipped.
    private void EmitRibbonRun(int firstVertex, int lastVertex, int bucket)
    {
        if (lastVertex <= firstVertex || bucket <= 0)
        {
            return;
        }
        float a = (float)bucket / FrameRibbonFadeBuckets;
        RgbaColor color = TrajectoryColor with { A = (byte)(TrajectoryColor.A * a) };
        _renderer!.DrawPolyline(_scratch.AsSpan(firstVertex * 2, (lastVertex - firstVertex + 1) * 2), color);
    }

    // The #110 time-fade ramp, quantized: full strength up to fadeStart, then linear to zero across
    // fadeSpan. Segments past the cutoff land in bucket 0 (invisible).
    private static int FadeBucket(double midTime, double fadeStart, double fadeSpan)
    {
        double alpha = midTime <= fadeStart
            ? 1.0
            : Math.Max(0.0, 1.0 - (midTime - fadeStart) / fadeSpan);
        return (int)Math.Round(alpha * FrameRibbonFadeBuckets);
    }

    // #148: draw the autopilot's rehearsed INTENDED path (the arc it will actually fly to capture),
    // teal and dashed so it never reads as the amber ballistic ribbon. Only the part still ahead of
    // the ship is drawn; routed through PlotFrame like every time-parameterized track (#144).
    private void DrawAutopilotPlanPath()
    {
        if (_armedOrbitBodyId is null || _autopilotPlanPath is not { Count: >= 2 } plan)
        {
            return;
        }

        int startIdx = 0;
        while (startIdx < plan.Count - 1 && plan[startIdx].SimTime < _ship.SimTime)
        {
            startIdx++;
        }
        int remaining = plan.Count - startIdx;
        if (remaining < 2)
        {
            return;
        }

        int stride = Math.Max(1, remaining / 220);
        int maxPoints = remaining / stride + 2;
        if (_autopilotPlanScratch.Length < maxPoints * 2)
        {
            _autopilotPlanScratch = new float[maxPoints * 2];
        }

        int w = 0;
        for (int i = startIdx; i < plan.Count; i += stride)
        {
            (float x, float y) = _camera.WorldToScreen(PlotFrame(plan[i].Position, plan[i].SimTime));
            _autopilotPlanScratch[w] = x;
            _autopilotPlanScratch[w + 1] = y;
            w += 2;
        }
        (float lx, float ly) = _camera.WorldToScreen(PlotFrame(plan[^1].Position, plan[^1].SimTime));
        _autopilotPlanScratch[w] = lx;
        _autopilotPlanScratch[w + 1] = ly;
        w += 2;

        // Dashed: draw every other 2-point segment, so the teal plan reads as a distinct dashed arc.
        for (int i = 0; i + 3 < w; i += 4)
        {
            _renderer!.DrawPolyline(_autopilotPlanScratch.AsSpan(i, 4), AutopilotPlanColor, 2f);
        }
    }

    private void DrawClosestPassMarker()
    {
        if (_closestPass is not { } cp || cp.Severity > 25)
        {
            return; // beyond 25 radii nobody is embarrassed
        }

        (float sx, float sy) = _camera.WorldToScreen(PlotFrame(cp.ShipPosition, cp.SimTime));
        RgbaColor color = cp.Impact
            ? new RgbaColor(255, 80, 80, 230)
            : cp.Severity < 5 ? new RgbaColor(255, 200, 80, 220) : new RgbaColor(170, 190, 210, 160);
        _renderer!.DrawCircle(sx, sy, 8f, null, color, 1.5f);
        _renderer!.DrawCircle(sx, sy, 2f, color, color);
        _renderer!.DrawText(sx, sy - 14,
            cp.Impact ? $"IMPACT {cp.BodyName}" : $"min {cp.BodyName} {cp.Severity:0.0}R",
            color, "10px monospace", TextAlign.Center);
    }

    // M25: the target lock — loud enough to find at any zoom (owner: the destination was
    // impossible to spot in plot view). A ring plus four range ticks, like a gun-camera reticle.
    private void DrawTargetLock(float sx, float sy, float bodyRadiusPx, RgbaColor color, string? label)
    {
        float r = Math.Max(bodyRadiusPx + 8f, 16f);
        _renderer!.DrawCircle(sx, sy, r, null, color, 1.6f);
        Span<float> tick = stackalloc float[4];
        for (int i = 0; i < 4; i++)
        {
            float dx = i switch { 0 => 1f, 1 => -1f, _ => 0f };
            float dy = i switch { 2 => 1f, 3 => -1f, _ => 0f };
            tick[0] = sx + dx * (r + 2); tick[1] = sy + dy * (r + 2);
            tick[2] = sx + dx * (r + 10); tick[3] = sy + dy * (r + 10);
            _renderer.DrawPolyline(tick, color, 1.6f);
        }

        if (label is not null)
        {
            _renderer.DrawText(sx + r + 6, sy - r, label, color);
        }
    }

    // M25: where the plotted course comes nearest the destination — the pass point on the
    // ribbon, the body's position at that moment under its own lock, and the miss distance
    // drawn as a line between them.
    private void DrawDestinationPassMarker()
    {
        if (_destinationPass is not { } dp || dp.BodyId != _destinationBodyId || _ephemeris is null)
        {
            return;
        }

        (float px, float py) = _camera.WorldToScreen(PlotFrame(dp.ShipPosition, dp.SimTime));
        (float bx, float by) = _camera.WorldToScreen(PlotFrame(_ephemeris.Position(dp.BodyId, dp.SimTime), dp.SimTime));

        Span<float> line = stackalloc float[4];
        line[0] = px; line[1] = py; line[2] = bx; line[3] = by;
        _renderer!.DrawPolyline(line, DestinationColor with { A = 130 }, 1.2f);

        _renderer.DrawCircle(px, py, 5f, null, DestinationColor, 1.6f);
        _renderer.DrawCircle(px, py, 1.5f, DestinationColor, DestinationColor);
        _renderer.DrawText(px, py - 16, $"pass {FormatDistance(dp.Distance)}", DestinationColor, "10px monospace", TextAlign.Center);

        // The far end of the miss line is just a dot — only the scrub-time ghost wears a lock.
        _renderer.DrawCircle(bx, by, 3f, DestinationColor with { A = 170 }, DestinationColor with { A = 170 });
    }

    // The sling made this visible: the ribbon bends where the pass body WILL BE, which at twelve
    // plotted days is hundreds of pixels from where the body is drawn now — without scrubbing,
    // the kink hangs in empty sky (owner: "the curvature happens at a spot Jupiter is not at").
    // So the pass body's ghost at the pass epoch shows where that curve pins to.
    //
    // #124 (owner playtest): PR #117 left this ALWAYS-on, and on any close planetary pass the ghost
    // planet + tether read as "a slingshot the game plotted for me — that I never selected or set".
    // With no sling engaged there is no ribbon kink to anchor, so the ghost is pure noise wearing a
    // sling costume. Gate it on real sling INTENT — the ⤴ Sling panel is open, or a sling has been
    // SOLVEd — and key it to the body actually being slung (_slingablePass), labelled as a sling
    // pass so planned-vs-hypothetical is unambiguous. The plain closest-pass marker / scrub ghosts
    // (DrawClosestPassMarker, DrawGhostBodies — both PlotMode) are untouched and keep doing their job.
    private void DrawPassEpochGhost()
    {
        if (_openEditor != FlightEditorKind.Sling && _slingResult is not { Ok: true })
        {
            return; // no sling engaged → no sling ghost (the owner never asked for one)
        }
        if (_slingablePass is not { } cp || cp.Severity > 25 || _ephemeris is null)
        {
            return; // same embarrassment threshold as the pass marker
        }

        (float sx, float sy) = _camera.WorldToScreen(PlotFrame(_ephemeris.Position(cp.BodyId, cp.SimTime), cp.SimTime));
        (float nx, float ny) = _camera.WorldToScreen(PlotFrame(_ephemeris.Position(cp.BodyId, SimTime), SimTime));
        if (Math.Abs(sx - nx) < 6 && Math.Abs(sy - ny) < 6)
        {
            return; // the ghost would sit on the live disc anyway (imminent pass or far zoom-out)
        }

        Span<float> tether = stackalloc float[4];
        tether[0] = nx; tether[1] = ny; tether[2] = sx; tether[3] = sy;
        _renderer!.DrawPolyline(tether, new RgbaColor(180, 200, 220, 30), 1f);

        float radiusPx = (float)Math.Max(3.5, cp.BodyRadius / _camera.MetersPerPixel);
        RgbaColor ghost = BodyColor(cp.BodyId) with { A = 110 };
        _renderer!.DrawCircle(sx, sy, radiusPx, ghost, ghost);
        _renderer!.DrawCircle(sx, sy, radiusPx + 2.5f, null, new RgbaColor(220, 230, 245, 90), 1.2f);
        _renderer.DrawText(sx, sy + radiusPx + 12, $"{cp.BodyName} at sling pass",
            new RgbaColor(220, 230, 245, 140), "10px monospace", TextAlign.Center);
    }

    // Ghosts of every body at the scrub time. Deliberately loud: a filled dot with an outline
    // ring and a faint tether from the live body — 2 px at 35% alpha vanished against the
    // plasma stream ribbons (the owner read Venus and Mercury as "stuck").
    private void DrawGhostBodies()
    {
        ICelestialEphemeris ephemeris = _ephemeris!;
        double t = ScrubTime;
        Span<float> tether = stackalloc float[4];
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            Vector2d position = ephemeris.Position(body.Id, t);
            (float sx, float sy) = _camera.WorldToScreen(PlotFrame(position, t));
            (float nx, float ny) = _camera.WorldToScreen(PlotFrame(ephemeris.Position(body.Id, SimTime), SimTime));

            tether[0] = nx; tether[1] = ny; tether[2] = sx; tether[3] = sy;
            _renderer!.DrawPolyline(tether, new RgbaColor(180, 200, 220, 40), 1f);

            float radiusPx = (float)Math.Max(3.5, body.BodyRadius / _camera.MetersPerPixel);
            RgbaColor baseColor = BodyColor(body.Id);
            RgbaColor ghost = baseColor with { A = 150 };
            _renderer!.DrawCircle(sx, sy, radiusPx, ghost, ghost);
            _renderer!.DrawCircle(sx, sy, radiusPx + 2.5f, null, new RgbaColor(220, 230, 245, 120), 1.2f);
            if (body.Id == _destinationBodyId)
            {
                // The destination's projected position wears the full lock — the owner couldn't
                // find the planet at all among the ghosts.
                DrawTargetLock(sx, sy, radiusPx, DestinationColor, "DEST");
            }
        }
    }

    // Ghost ship marker at the projected path position for the scrub time.
    private void DrawGhostShip()
    {
        if (_samples.Count == 0) return;
        Vector2d position = SamplePositionAt(ScrubTime);
        (float sx, float sy) = _camera.WorldToScreen(PlotFrame(position, ScrubTime));
        _renderer!.DrawCircle(sx, sy, 4f, GhostShipColor, GhostShipColor);
    }

    // Filled dots on the ribbon where each maneuver node fires.
    private void DrawNodeMarkers()
    {
        if (_planNodes.Count == 0 || _samples.Count == 0) return;
        foreach (PlanNode node in _planNodes)
        {
            if (node.Executed) continue;
            Vector2d position = SamplePositionAt(node.SimTime);
            (float sx, float sy) = _camera.WorldToScreen(PlotFrame(position, node.SimTime));
            RgbaColor color = node.Stale
                ? StaleNodeColor
                : node.Action == ManeuverAction.Accelerate ? AccelNodeColor : DecelNodeColor;
            bool selected = ReferenceEquals(node, _selectedPlanNode);

            // X-Pilot burn: draw the nose vector — the heading the burn thrusts along — anchored at
            // the node's position on the ribbon (the "scrub line position" the owner asked for).
            if (node.Mode == BurnMode.Vector && !node.Stale)
            {
                DrawNoseVector(sx, sy, node.HeadingDegrees, selected);
            }

            _renderer!.DrawCircle(sx, sy, selected ? 6.5f : 5f, color, color);
            if (selected)
            {
                _renderer!.DrawCircle(sx, sy, 10f, null, new RgbaColor(255, 255, 255, 160), 1.5f);
            }
            if (Math.Abs(node.Percent - 10) > 0.001)
            {
                _renderer!.DrawText(sx, sy - 12, $"{node.Percent:0.##}%", new RgbaColor(150, 220, 255, 200), "9px monospace", TextAlign.Center);
            }
        }
    }

    // The X-Pilot nose vector: a fixed-length arrow from the node marker along the burn heading —
    // "the direction of nose when the burn is planned, on the scrub-line position" (owner). The
    // camera is an axis-aligned scale with Y flipped (WorldToScreen maps +X→right, +Y→up), so a
    // world heading (cosθ, sinθ) becomes the screen direction (cosθ, −sinθ) directly — projecting a
    // unit world offset instead would round to the same pixel at solar-system zoom and vanish.
    private void DrawNoseVector(float sx, float sy, double headingDegrees, bool selected)
    {
        double rad = headingDegrees * Math.PI / 180.0;
        float dx = (float)Math.Cos(rad);
        float dy = -(float)Math.Sin(rad);

        const float shaft = 34f;   // fixed screen length so the heading reads at any zoom
        float tipX = sx + dx * shaft, tipY = sy + dy * shaft;
        RgbaColor c = selected ? new RgbaColor(190, 235, 255) : XPilotVectorColor;
        float width = selected ? 2.5f : 1.8f;

        Span<float> shaftPts = [sx, sy, tipX, tipY];
        _renderer!.DrawPolyline(shaftPts, c, width);

        // Arrowhead: two barbs swept back from the tip.
        const float barb = 8f;
        Span<float> headPts =
        [
            tipX + (-dx * barb - dy * barb * 0.6f), tipY + (-dy * barb + dx * barb * 0.6f),
            tipX, tipY,
            tipX + (-dx * barb + dy * barb * 0.6f), tipY + (-dy * barb - dx * barb * 0.6f),
        ];
        _renderer!.DrawPolyline(headPts, c, width);
    }

    // Position on the projected path at a given sim time, linearly interpolated between the two
    // bracketing samples. Clamps to the endpoints outside the projected horizon. #838 moved the
    // interpolation itself into Core (NodeFrame) so the ghost the ribbon DRAWS and the ghost the quick
    // selects AIM IN are read out of the samples by one function.
    private Vector2d SamplePositionAt(double simTime) =>
        NodeFrame.PositionAt(_samples, simTime, _ship.Position);

}
