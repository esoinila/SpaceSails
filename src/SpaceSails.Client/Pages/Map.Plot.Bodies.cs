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

// Subject: part of Map.Plot (#870 split; the header note lives in Map.Plot.cs) — what the map draws that is not the plan: the celestial bodies and their marks, the cargo run's parcel, the pass flash, the polyline primitive, and the ship glyph with the discharge off her mast.
public partial class Map
{
    private void DrawCelestialBodies()
    {
        ICelestialEphemeris ephemeris = _ephemeris!;
        Span<float> ring = stackalloc float[(OrbitSegments + 1) * 2];

        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (IsBodyHidden(body.Id)) continue; // off the charts until an intel-fed scan finds it (PR-A)
            // #394: the inbound rock draws its own RED threat rail (DrawAsteroidThreat) that bends on
            // deflection — suppress the default grey ring so there is only one, and it tells the story.
            bool deflectionRock = _deflection is { } dgig && body.Id == dgig.RockBodyId;
            // #405 Routes → Orbit rails / ellipses: the grey Kepler ring. The threat rock's own RED
            // rail is drawn by DrawAsteroidThreat and is never layer-gated (the pinned safety family).
            if (!deflectionRock && LayerVisible("routes.rails") && body.OrbitPeriod != 0 && body.OrbitRadius > 0)
            {
                Vector2d parentPosition = body.ParentId is null ? Vector2d.Zero : ephemeris.Position(body.ParentId, SimTime);
                // Kepler rails (PR-B): a circular body's ring is a circle of radius OrbitRadius; an
                // eccentric body's is its true ellipse, traced by sweeping the eccentric anomaly over a
                // full turn (even spacing in E, one perifocal point rotated by ω per vertex — no
                // per-vertex Kepler solve). e == 0 keeps the exact circular sweep.
                double e = body.Eccentricity;
                double semiMinor = e == 0.0 ? body.OrbitRadius : body.OrbitRadius * Math.Sqrt(1.0 - e * e);
                double cosW = Math.Cos(body.ArgPeriapsis);
                double sinW = Math.Sin(body.ArgPeriapsis);
                for (int i = 0; i <= OrbitSegments; i++)
                {
                    double t = Math.Tau * i / OrbitSegments;
                    Vector2d world;
                    if (e == 0.0)
                    {
                        world = parentPosition + new Vector2d(body.OrbitRadius * Math.Cos(t), body.OrbitRadius * Math.Sin(t));
                    }
                    else
                    {
                        double px = body.OrbitRadius * (Math.Cos(t) - e);
                        double py = semiMinor * Math.Sin(t);
                        world = parentPosition + new Vector2d(cosW * px - sinW * py, sinW * px + cosW * py);
                    }

                    (float x, float y) = _camera.WorldToScreen(world);
                    ring[i * 2] = x;
                    ring[i * 2 + 1] = y;
                }

                _renderer!.DrawPolyline(ring, OrbitColor);
            }

            Vector2d position = ephemeris.Position(body.Id, SimTime);
            (float sx, float sy) = _camera.WorldToScreen(position);
            bool isStation = body.Kind == BodyKind.Station;
            float radiusPx = (float)Math.Max(isStation ? 1.5 : 2.0, body.BodyRadius / _camera.MetersPerPixel);
            if (isStation)
            {
                radiusPx = Math.Min(radiusPx, 3.5f); // a built thing, not a world — stays a small blip
            }

            RgbaColor color = BodyColor(body);

            _renderer!.DrawCircle(sx, sy, radiusPx, color, color);
            bool isDestination = body.Id == _destinationBodyId;
            if (isDestination && !PlotMode)
            {
                // The chosen destination reads at any zoom — full gun-camera lock (M25).
                // In plot mode the GHOST carries the one and only lock (owner: three targets on
                // screen made the scrub read as frozen — he was watching the live body).
                DrawTargetLock(sx, sy, radiusPx, DestinationColor, "DEST");
            }
            if (isDestination || _camera.MetersPerPixel < body.BodyRadius * 500 || (isStation && _camera.MetersPerPixel < LabelZoomThresholdForStations))
            {
                RgbaColor labelColor = body.IsHaven ? HavenLabelColor : LabelColor;
                // A ⚓ flags the mass-less grey-market docks — the havens you can clamp onto to lie
                // low (moon havens you orbit instead, so they carry the pink wash but no anchor).
                string label = IsDockableHaven(body) ? $"⚓ {body.Name}" : body.Name;
                int labelPriority = BodyLabelPriority(body);
                // #402/#405: two decluttering seams. (1) MANUAL — the 🗺 Layers tree. A MINOR station
                // name label (the depot clutter) rides the Labels → "Minor / depot labels" leaf; every
                // other body name rides Labels → "Body names". The docked/destination/armed station the
                // captain is working outranks LabelPriorityStation, so it counts as a body name, never a
                // minor label. (2) AUTOMATIC — surviving labels are enqueued and FlushNavLabels de-collides
                // them by priority, so the Saturn knot reads even with all layers on.
                bool minorStationLabel = labelPriority == LabelPriorityStation;
                if (LayerVisible(minorStationLabel ? "labels.minor" : "labels.bodies"))
                {
                    EnqueueNavLabel(sx + radiusPx + 4, sy - radiusPx, label, labelColor, labelPriority);
                }

                // The ⚓'s sibling: a 🛬 under any shuttle-landable ground (a moon, by the same pure
                // ShuttleExcursion.IsLandableSurface the destination board uses — never a hardcoded body
                // list, so it lights up correctly whatever the moons' phases). Bright + a size up when
                // that ground is within the shuttle's reach of the ship right now (the _landableInRange
                // set, the board's own range truth); dim regolith tan when landable only in principle.
                if (ShuttleExcursion.IsLandableSurface(body.Kind) && LayerVisible("labels.landable")) // #405 Labels → Landable marks
                {
                    bool inReach = _landableInRangeIds.Contains(body.Id);
                    _renderer.DrawText(sx + radiusPx + 4, sy + radiusPx + 20, "🛬",
                        inReach ? LandableInRangeColor : LandableBaseColor,
                        inReach ? "13px sans-serif" : "11px sans-serif", TextAlign.Left);
                }
            }
        }
    }

    private static readonly RgbaColor CargoMarkerColor = new(235, 190, 120); // parcel amber — reads on the star field

    // #175: while a cargo run is in hand, its destination carries a 📦 so the delivery point isn't
    // invisible on the map. Modest — a small tag under the body's own label, drawn only for the
    // Active run's destination (it clears the instant the parcel is delivered).
    private void DrawCargoRunMarkers()
    {
        if (_ephemeris is null) return;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (IsBodyHidden(body.Id) || ActiveCargoRunTo(body.Id) is null) continue;
            Vector2d position = _ephemeris.Position(body.Id, SimTime);
            (float sx, float sy) = _camera.WorldToScreen(position);
            bool isStation = body.Kind == BodyKind.Station;
            float radiusPx = (float)Math.Max(isStation ? 1.5 : 2.0, body.BodyRadius / _camera.MetersPerPixel);
            if (isStation) radiusPx = Math.Min(radiusPx, 3.5f);
            _renderer!.DrawText(sx + radiusPx + 4, sy + radiusPx + 8, $"📦 deliver to {body.Name}", CargoMarkerColor,
                "11px sans-serif", TextAlign.Left);
        }
    }
    private static readonly RgbaColor PassFlashColor = new(150, 255, 210);

    private double _frameNowMs;

    private void DrawPassFlash()
    {
        if (_trackingPost?.LastPassFlash is not { } flash)
        {
            return;
        }

        double ageMs = (DateTime.UtcNow - flash.WallTime).TotalMilliseconds;
        if (ageMs is < 0 or > 1200 || ContactPosition(flash.ShipId) is not { } position)
        {
            return;
        }

        (float sx, float sy) = _camera.WorldToScreen(position);
        byte alpha = (byte)(220 * (1 - ageMs / 1200));
        DrawCornerBrackets(sx, sy, 13f, PassFlashColor with { A = alpha });
        _renderer!.DrawText(sx + 16, sy - 10, "updating fix", PassFlashColor with { A = alpha },
            "10px monospace", TextAlign.Left);
    }

    private void DrawCornerBrackets(float sx, float sy, float r, RgbaColor color)
    {
        Span<float> corner = stackalloc float[6];
        for (int xSign = -1; xSign <= 1; xSign += 2)
        {
            for (int ySign = -1; ySign <= 1; ySign += 2)
            {
                corner[0] = sx + xSign * r;
                corner[1] = sy + ySign * (r - 5);
                corner[2] = sx + xSign * r;
                corner[3] = sy + ySign * r;
                corner[4] = sx + xSign * (r - 5);
                corner[5] = sy + ySign * r;
                _renderer!.DrawPolyline(corner, color, 1.5f);
            }
        }
    }

    private static string FormatFlightTime(double seconds) =>
        seconds < 3600 ? $"{seconds / 60:F0} min"
        : seconds < 86400 ? $"{seconds / 3600:F1} h"
        : $"{seconds / 86400:F1} d";

    private void DrawWorldPolyline(IReadOnlyList<TrajectorySample> samples, RgbaColor color, float widthPx)
    {
        if (samples.Count < 2)
        {
            return;
        }

        int stride = Math.Max(1, samples.Count / 160);
        int points = (samples.Count + stride - 1) / stride + 1;
        float[] xy = new float[points * 2];
        int w = 0;
        for (int i = 0; i < samples.Count; i += stride)
        {
            (xy[w], xy[w + 1]) = _camera.WorldToScreen(samples[i].Position);
            w += 2;
        }

        (xy[w], xy[w + 1]) = _camera.WorldToScreen(samples[^1].Position);
        w += 2;
        _renderer!.DrawPolyline(xy.AsSpan(0, w), color, widthPx);
    }

    private void DrawShip(Vector2d shipPosition)
    {
        (float sx, float sy) = _camera.WorldToScreen(shipPosition);

        // #528 / LAB 43 · THE DISCHARGE IS A PLUME OFF HER MAST, NEVER A RING AROUND HER.
        //
        // This used to draw a hollow halo about the hull — which the lab showed is the one shape it cannot be.
        // Field strength is potential over radius of curvature, so her antenna whip runs 20,000× the field of
        // her hull skin (40 MV/m against 0.002) and sits at 4% of field-emission onset while the skin is at
        // 0.000%. A discharge leaves from the sharpest thing she has, and drawing a sphere was drawing the one
        // place it can never start. The physics handed us the better picture for free.
        DrawDischarge(sx, sy);

        // M28: the hull has a facing now — cosmetic on the map, but it SLEWS: toward the
        // firing bearing through a lock countdown, back to prograde after the round leaves.
        double heading = ShipHeadingRad();
        Vector2d barrelTip = shipPosition
            + new Vector2d(Math.Cos(heading), Math.Sin(heading)) * (12 * _camera.MetersPerPixel);
        (float bx, float by) = _camera.WorldToScreen(barrelTip);
        Span<float> barrel = stackalloc float[4];
        barrel[0] = sx; barrel[1] = sy; barrel[2] = bx; barrel[3] = by;
        _renderer!.DrawPolyline(barrel, ShipColor with { A = 200 }, 2f);

        _renderer!.DrawCircle(sx, sy, 4f, ShipColor, ShipColor);

        // #933 — …and where she is GOING, in the frame the plan is being read in. After the dot, so the
        // arrowhead sits on top of her rather than under her.
        DrawVelocityArrowhead(sx, sy);

        _renderer!.DrawText(sx + 8, sy - 6, "Ship", ShipColor);
    }

    /// <summary>
    /// #933 · WHICH WAY SHE IS GOING — a small arrowhead off the ship marker, in her own ink.
    ///
    /// <para>Owner, 2026-08-17 (playing the flight side): <i>"our ship shape on the map could indicate little
    /// better about where it is going if it was depicted as arrow like triangle … more like add shape that
    /// points to the direction the ship is going even when its motion is stopped during the burn parameter
    /// selection."</i></para>
    ///
    /// <para><b>Drawn every map frame, paused included.</b> This sits inside <c>DrawShip</c> with no gate on
    /// the clock, which is the whole point of the request: velocity is STATE, not motion. Standing still at
    /// the plotting desk with the sky frozen, the captain must still be able to see which way the ship is
    /// carrying — otherwise the one moment he most needs the answer is the one moment the map refuses it.</para>
    ///
    /// <para><b>The frame is the one the plan is being read in</b> (#135/#926) — the same
    /// <c>FrameRelativeVelocity</c> the <c>v helio</c> / <c>v rel {body}</c> readout is built from, so the
    /// shape and the number can never come to say different things. Below
    /// <see cref="VelocityArrow.RingBelowMps"/> the dart collapses to a ring: parked, or co-moving with the
    /// frame body, and honest about it.</para>
    ///
    /// <para><b>Two shapes, not one.</b> The burn's aim keeps its own picture at the node (#916) — that is
    /// PUSHING. This is GOING, and it never swings to the burn.</para>
    /// </summary>
    private void DrawVelocityArrowhead(float sx, float sy)
    {
        Vector2d velocity = FrameRelativeVelocity(SimTime);
        if (VelocityArrow.ShowsRing(velocity.Length))
        {
            _renderer!.DrawCircle(sx, sy, VelocityArrow.RingRadiusPx, null, ShipColor with { A = 200 }, 1.5f);
            return;
        }

        // World Y is up, canvas Y is down (Camera.WorldToScreen flips it) — so the screen angle is the world
        // angle mirrored, and the flip lives HERE, once, at the one call that crosses from one to the other.
        double screenRad = Math.Atan2(-velocity.Y, velocity.X);
        Span<float> head = stackalloc float[6];
        VelocityArrow.Points(screenRad, sx, sy, head);
        _renderer!.DrawPolygon(head, ShipColor with { A = 200 }, ShipColor);
    }

    /// <summary>
    /// Her mast, and whatever is coming off it. Two states, both from Lab 43 — and <b>the geometry is not
    /// here</b>. It is <see cref="DischargePlume"/> in Core, where a test can ask it questions; this method is
    /// a pen that walks what Core hands back and nothing else. A client geometry literal nobody can put a test
    /// on is this repo's first named bug class, and a plume is a physical claim.
    ///
    /// <para>ARCING is a slow crawl of short filaments — readable from the map without opening a panel, the
    /// same principle as the vacuum clocks being readable from the corridor. Its phase comes off <b>SIM</b>
    /// time, so a paused map draws the same frame twice and the fingerprint ledgers hold still.</para>
    ///
    /// <para>A DUMP is one bright snap and an afterglow on the real clock, scaled by <b>how much she actually
    /// let go of</b> — <see cref="DischargePlume.DumpBrightness"/>, which is
    /// <see cref="HullCharge.SeenFartherFactor"/>'s own excess rather than a second opinion about it. What it
    /// must NOT be is slow-motion lightning: 0.22 J is a static shock off a door handle.</para>
    /// </summary>
    private void DrawDischarge(float sx, float sy)
    {
        if (_plasma is null)
        {
            return;
        }

        double sinceDump = (_lastTimestampMs ?? 0) - _lastDischargeMs;
        DischargePlume.Plume plume =
            DischargePlume.Shape(_lastDischargeShed, HullCharge.BandOf(_ship.Charge), sinceDump);
        if (!plume.Draws)
        {
            return;
        }

        // The whip stands off her beam — read as an antenna rather than as the gun, which already draws along
        // the heading. World Y is up and canvas Y is down (Camera.WorldToScreen flips it), so her world heading
        // crosses over to a SCREEN angle here, once, at the one call that crosses — the same rule
        // DrawVelocityArrowhead obeys. Before #528 §7 this mast was built from an unflipped world angle, so it
        // swung the opposite way round the hull from her own barrel line as she slewed.
        double mastAngle = -ShipHeadingRad() - (Math.PI / 2);
        (double mastOffsetX, double mastOffsetY) = DischargePlume.Masthead(mastAngle);
        float mastX = sx + (float)mastOffsetX;
        float mastY = sy + (float)mastOffsetY;

        Span<float> mast = stackalloc float[4];
        mast[0] = sx; mast[1] = sy; mast[2] = mastX; mast[3] = mastY;
        _renderer!.DrawPolyline(mast, ArcHaloColor with { A = 140 }, 1f);

        double phase = plume.Flashing
            ? DischargePlume.FlashPhase(sinceDump)
            : DischargePlume.CrawlPhase(SimTime);

        // One buffer for the whole plume: CA2014 is right that a stackalloc inside the loop is a frame-rate
        // shaped foot-gun, and this runs on every rendered frame while she is arcing. Both spans are stack
        // room the frame already owns — the plume allocates nothing.
        Span<double> bolts = stackalloc double[DischargePlume.MaxFilaments * DischargePlume.FloatsPerFilament];
        int written = DischargePlume.Filaments(plume, mastAngle, phase, bolts);

        RgbaColor ink = ArcHaloColor with { A = DischargePlume.Alpha(plume) };
        float widthPx = DischargePlume.StrokePx(plume);
        Span<float> bolt = stackalloc float[DischargePlume.FloatsPerFilament];
        for (int i = 0; i < written; i += DischargePlume.FloatsPerFilament)
        {
            bolt[0] = sx + (float)bolts[i];
            bolt[1] = sy + (float)bolts[i + 1];
            bolt[2] = sx + (float)bolts[i + 2];
            bolt[3] = sy + (float)bolts[i + 3];
            bolt[4] = sx + (float)bolts[i + 4];
            bolt[5] = sy + (float)bolts[i + 5];
            _renderer!.DrawPolyline(bolt, ink, widthPx);
        }

        // The core sits ON the masthead, because that is where the field is — small, and brightest at the snap.
        _renderer!.DrawCircle(mastX, mastY, DischargePlume.CoreRadiusPx(plume), ink, ink);
    }

    /// <summary>When she last let go, in renderer-clock milliseconds.</summary>
    private double _lastDischargeMs = double.NegativeInfinity;

    /// <summary>…and HOW MUCH she let go of, 0…1 of hull charge. The brightness of the flash is this number
    /// read through <see cref="DischargePlume.DumpBrightness"/> — a dump off a nearly cold hull is a tick at
    /// the masthead, and a dump off an arcing one is the whole picture. Kept beside the timestamp because the
    /// two are one event: without it the flash would be the same size whatever she was carrying, which is the
    /// one thing #528 §7 asked it not to be.</summary>
    private double _lastDischargeShed;
}
