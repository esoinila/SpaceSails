using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #167 · A BURN MUST BE FELT — AT THE PEN, AND AT EVERY KIND OF BURN.
///
/// <para>Owner, 2026-07-16: <i>"Now we should have some burn-happening sound and visual effect also."</i>
/// <c>TheExhaustLeavesHerSternTests</c> in Core holds the GEOMETRY — <see cref="BurnPlume"/>, the pure
/// function. These rows hold the WIRING, which is where this issue's actual difficulty lives: not "does a
/// flame have the right shape" but <b>"does EVERY kind of burn reach it"</b>. The issue names the trap in
/// one line — <i>"one hook where the impulse is applied"</i> — because feedback bolted onto each burn site
/// separately is feedback present at eight sites and missing at the ninth.</para>
///
/// <h3>Two kinds of guard, and they need each other</h3>
/// <list type="bullet">
/// <item><b>The laws</b> read the SOURCE: that the burn cue is fired in exactly one place, and that every
/// call into Core's burn machinery is followed by the hook. A source scan is the only thing that can say
/// "and nowhere else", which is the half a runtime test can never reach.</item>
/// <item><b>The drives</b> read the PEN: a real <see cref="Pages.Map"/> over the shipping
/// <c>scenarios/sol.json</c>, each burn kind actually fired, and the flame read back out of the real
/// command buffer as floats. Nothing below asserts that a line of code exists; everything below asserts
/// what got drawn. (The flush at the end of <c>PaintTheMapFrame</c> is the one line that crosses into
/// JavaScript and throws off-browser — the buffer is complete by then, the seam
/// <see cref="TheDischargeIsAPlumeOffHerMastTests"/> rides.)</item>
/// </list>
///
/// <para>The cue itself cannot be heard from a bench: <c>RendererInterop.PlayCue</c> is a no-op off-browser
/// by design (#837). So the hook counts its own firings, and the rows below read that counter — which is
/// exactly as strong as hearing it, because the counter and the <c>PlayCue</c> call are the same two lines
/// of the same method and the first law proves there is no other.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class EveryBurnIsFeltTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    // ── LAW ONE · ONE BURN, ONE CUE, ONE PLACE ────────────────────────────────────────────────────────

    /// <summary>
    /// The <c>burn</c> cue is fired from exactly ONE place in the shipped client, and that place is
    /// <c>BurnFired</c>. This is what makes "one cue per burn" a property of the code rather than a habit:
    /// a second caller anywhere would be a burn that either double-sounds or sounds without arming a flame,
    /// and both of those are invisible in a browser and inaudible on a bench.
    ///
    /// <para>ANTI-VACUOUS: the scan states how many <c>PlayCue</c> sites it found in total, because a regex
    /// that matched nothing would make "exactly one burn caller" trivially true — the bug class this repo
    /// named after a guard handed a world that cannot tell pass from fail.</para>
    ///
    /// <para>RED PROOF: put the old <c>RendererInterop.PlayCue("pulse")</c> back beside the hand's burn and
    /// nothing here moves (it is a different name); put <c>PlayCue("burn")</c> back in
    /// <c>ArrivalBrakeFire</c> and this row fails naming Map.LongHaul.cs as a second mouth.</para>
    /// </summary>
    [Fact]
    public void TheBurnCueIsFiredInExactlyOnePlace()
    {
        var everyCueSite = CueSites();
        Assert.True(everyCueSite.Count >= 100,
            $"only {everyCueSite.Count} PlayCue call site(s) found under {ClientRoot} — the scan proved nothing");

        var burnSites = everyCueSite.Where(s => s.Names.Contains("burn")).ToList();

        Assert.True(burnSites.Count == 1,
            $"the `burn` cue is fired from {burnSites.Count} place(s), and one burn must make one noise:\n  "
            + string.Join("\n  ", burnSites.Select(s => s.Where)));
        Assert.StartsWith("src/SpaceSails.Client/Pages/Map.Burn.cs:", burnSites[0].Where, StringComparison.Ordinal);
    }

    // ── LAW TWO · EVERY KIND ENDS AT THE HOOK ─────────────────────────────────────────────────────────

    /// <summary>
    /// THE ENUMERATION. Nine kinds of burn exist in this client and every one of them ends at
    /// <c>BurnFired</c>: the hand, the plotted node, the scheduled transfer, the autopilot's approach, its
    /// insertion, its station-keeping trim, the panel's orbital insertion, the terminal dock match, and the
    /// arrival brake. Each call site carries its own <c>#167 BURN KIND n/9</c> marker, and this row demands
    /// the run 1…9 with no gaps and no repeats — so a kind quietly dropped from the hook cannot leave the
    /// enumeration looking complete.
    ///
    /// <para>AND THE OTHER DIRECTION, which is the one that catches the burn nobody thought of: every call
    /// this client makes into Core's burn machinery — <c>OrbitRule.Approach</c>, <c>OrbitRule.Insert</c>,
    /// <c>OrbitKeeping.Trim</c>, <c>ArrivalBrake.FireBrake</c> — is followed by the hook inside the same
    /// paragraph of code. Those four names mean "the drive fired" and nothing else, so a tenth burn site added later
    /// goes red on the day it is written rather than on the day somebody notices it is silent.</para>
    ///
    /// <para>RED PROOF: delete the <c>BurnFired</c> line under <c>OrbitKeeping.Trim</c>, and its kind
    /// marker with it — the tidy version of the mistake — and the row still fails, naming
    /// Map.Autopilot.cs's trim as a place that fires the drive and reaches no hook.</para>
    /// </summary>
    [Fact]
    public void EveryBurnKindEndsAtTheOneHook()
    {
        var files = ClientSources().ToList();
        Assert.True(files.Count > 50, $"only {files.Count} client source file(s) read — the scan proved nothing");

        // ── the nine kinds, by their own markers ──
        var kinds = new List<(int Kind, string Where)>();
        var hookSites = new List<string>();
        int declarations = 0;
        foreach ((string path, string src) in files)
        {
            // Call sites only: the negative lookahead drops the hook's own declaration, which is the one
            // line matching this that is not a burn. (It is counted separately, below — there must be
            // exactly one of it, or "the one hook" is not one hook.)
            foreach (Match call in Regex.Matches(
                src, @"^(?![^\r\n]*\bprivate\b)[^\r\n/]*\bBurnFired\(", RegexOptions.Multiline))
            {
                int line = src.Take(call.Index).Count(c => c == '\n') + 1;
                hookSites.Add($"{Relative(path)}:{line}");
            }

            declarations += Regex.Matches(src, @"^\s*private\s[^\r\n]*\bBurnFired\(", RegexOptions.Multiline).Count;

            foreach (Match marker in Regex.Matches(src, @"#167 BURN KIND (\d)/9"))
            {
                int line = src.Take(marker.Index).Count(c => c == '\n') + 1;
                kinds.Add((int.Parse(marker.Groups[1].Value), $"{Relative(path)}:{line}"));
            }
        }

        Assert.Equal(1, declarations);
        Assert.Equal(9, hookSites.Count);
        Assert.Equal(
            Enumerable.Range(1, 9),
            kinds.Select(k => k.Kind).OrderBy(k => k));

        // ── and nothing burns without one ──
        string[] theDriveFired =
        [
            "OrbitRule.Approach(", "OrbitRule.Insert(", "OrbitKeeping.Trim(", "ArrivalBrake.FireBrake(",
        ];

        var unhooked = new List<string>();
        int burnCalls = 0;
        foreach ((string path, string src) in files)
        {
            string[] lines = src.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                    || line.TrimStart().StartsWith("///", StringComparison.Ordinal)
                    || line.TrimStart().StartsWith("*", StringComparison.Ordinal)
                    || !theDriveFired.Any(n => line.Contains(n, StringComparison.Ordinal)))
                {
                    continue;
                }

                burnCalls++;
                // Thirty lines, not five: two of the nine compute the burn's result first (the terminal match
                // builds a candidate ShipState it may still refuse; the arrival brake prices the shed before
                // it spends it) and only then commit and hook. Still one paragraph — a hook that has to be
                // hunted for further than this is not obviously the same event.
                bool hooked = lines.Skip(i).Take(30).Any(l => l.Contains("BurnFired(", StringComparison.Ordinal));
                if (!hooked)
                {
                    unhooked.Add($"{Relative(path)}:{i + 1}: {line.Trim()}");
                }
            }
        }

        Assert.True(burnCalls >= 4,
            $"only {burnCalls} call(s) into Core's burn machinery found — the scan proved nothing");
        Assert.True(unhooked.Count == 0,
            $"{unhooked.Count} place(s) fire the drive and reach no hook, so they make no flame and no "
            + "noise:\n  " + string.Join("\n  ", unhooked));
    }

    // ── THE DRIVES · EACH KIND, ONCE, ON THE GLASS ────────────────────────────────────────────────────

    /// <summary>
    /// THE HAND. An arrow-key pulse fires the hook exactly once and puts a flame on the map.
    ///
    /// <para>RED PROOF: delete the <c>BurnFired</c> line from the pulse branch in <c>Map.Sim.Keys.cs</c>
    /// and the row fails with 0 burns fired and an empty frame.</para>
    /// </summary>
    [Fact]
    public void TheHandsPulseIsFelt()
    {
        Pages.Map map = Boot();
        Set(map, "_audioArmed", true);   // the gesture unlock is a JS call; the bench has no browser

        AtTheWallClock(map, 5_000);
        Invoke(map, "OnKeyDown", new KeyboardEventArgs { Key = "ArrowUp" });

        Assert.Equal(1, Burns(map));
        Assert.NotEmpty(Feathers(Paint(map)));
    }

    /// <summary>
    /// THE PLOTTED NODE — the kind that showed the captain nothing at all before this lane, because the
    /// integrator applied the impulse and the accountant only settled the mass.
    ///
    /// <para>Both burn modes, because they take different routes to a direction: a Factor node pushes along
    /// her track (or back down it), a Vector node along its own world heading.</para>
    ///
    /// <para>RED PROOF: delete the <c>BurnFired</c> line from <c>AccountForFiredNodes</c> — restoring the
    /// pure book-keeping this lane found — and both halves fail with nothing drawn.</para>
    /// </summary>
    [Fact]
    public void ThePlottedNodeIsFelt()
    {
        foreach (BurnMode mode in new[] { BurnMode.Factor, BurnMode.Vector })
        {
            Pages.Map map = Boot();
            AtTheWallClock(map, 5_000);

            object node = Activator.CreateInstance(NodeType)!;
            NodeField("SimTime").SetValue(node, ((ShipState)Read(map, "_ship")!).SimTime - 1);
            NodeField("Pulses").SetValue(node, 6);
            NodeField("Mode").SetValue(node, mode);
            NodeField("HeadingDegrees").SetValue(node, 37.0);
            NodeField("Action").SetValue(node, ManeuverAction.Accelerate);
            Nodes(map).Add(node);

            Invoke(map, "AccountForFiredNodes");

            Assert.Equal(1, Burns(map));
            Assert.NotEmpty(Feathers(Paint(map)));
        }
    }

    /// <summary>
    /// THE SCHEDULED TRANSFER BURN — the kind this whole issue was raised about: it fires ITSELF at its
    /// epoch, at four figures of warp, with no hand anywhere near the ship.
    ///
    /// <para>RED PROOF: delete the <c>BurnFired</c> line from <c>ApplyTransferBurn</c> and the row fails
    /// with 0 burns fired.</para>
    /// </summary>
    [Fact]
    public void TheScheduledTransferBurnIsFelt()
    {
        Pages.Map map = ArmedAt(Titan);
        AtTheWallClock(map, 5_000);

        Invoke(map, "ApplyTransferBurn", new Vector2d(120.0, -45.0));

        Assert.Equal(1, Burns(map));
        Assert.NotEmpty(Feathers(Paint(map)));
    }

    /// <summary>
    /// THE AUTOPILOT'S OWN TWO — the approach burn and the insertion — driven through the shipping
    /// <c>CheckArmedInsertion</c>, with the ship placed where <c>OrbitRule.AutopilotDecision</c> returns
    /// each of them. The decision is asked, not assumed: the row asserts which action it got before it
    /// fires, so a retune that moves the bands cannot leave this silently testing the same branch twice.
    ///
    /// <para>RED PROOF: delete either <c>BurnFired</c> line in the <c>switch</c> in
    /// <c>CheckArmedInsertion</c> and the matching half fails with 0 burns fired.</para>
    /// </summary>
    [Theory]
    [InlineData(OrbitRule.AutopilotAction.Insert)]
    [InlineData(OrbitRule.AutopilotAction.Approach)]
    public void TheAutopilotsOwnBurnsAreFelt(OrbitRule.AutopilotAction wanted)
    {
        Pages.Map map = ArmedAt(Titan);
        AtTheWallClock(map, 5_000);

        (CelestialBody body, CelestialBody parent, Vector2d bodyPos, Vector2d bodyVel) = TheMoon(map, Titan);
        double hill = OrbitRule.HillRadius(body, parent.Mu);

        // Co-moving with the moon (zero relative speed) at a radius inside the band that decides `wanted`:
        // inside the parking radius is an insertion, out near the capture range is an approach.
        double radius = wanted == OrbitRule.AutopilotAction.Insert
            ? OrbitRule.ParkingRadius(body, hill) * 0.9
            : OrbitRule.CaptureRange(hill) * 0.9;
        ShipState ship = (ShipState)Read(map, "_ship")!;
        Set(map, "_ship", ship with { Position = bodyPos + new Vector2d(radius, 0), Velocity = bodyVel });

        Assert.Equal(wanted, OrbitRule.AutopilotDecision(
            (ShipState)Read(map, "_ship")!, bodyPos, bodyVel, body, hill));

        Invoke(map, "CheckArmedInsertion");

        Assert.Equal(1, Burns(map));
        Assert.NotEmpty(Feathers(Paint(map)));
    }

    /// <summary>
    /// THE PANEL'S ORBITAL INSERTION — the <c>o</c> key, the one burn on this list a captain asks for by
    /// name and then watches happen.
    ///
    /// <para>RED PROOF: delete the <c>BurnFired</c> line from <c>EnterOrbit</c> and the row fails with 0
    /// burns fired.</para>
    /// </summary>
    [Fact]
    public void ThePanelsOrbitalInsertionIsFelt()
    {
        Pages.Map map = Boot();
        AtTheWallClock(map, 5_000);

        (CelestialBody body, CelestialBody parent, Vector2d bodyPos, Vector2d bodyVel) = TheMoon(map, Titan);
        double hill = OrbitRule.HillRadius(body, parent.Mu);
        // At the park radius, and NOT already captured: a ship sitting still relative to the moon is
        // bound, and OrbitAssistInfo refuses to re-insert a bound ship. So she comes through on a slow
        // hyperbolic pass — a hair over the local escape speed, far under OrbitRule.MaxRelativeSpeed —
        // which is exactly the state the ⊙ button exists to convert into a park.
        double radius = OrbitRule.ParkingRadius(body, hill);
        double escape = Math.Sqrt(2 * body.Mu / radius);
        ShipState ship = (ShipState)Read(map, "_ship")!;
        Set(map, "_ship", ship with
        {
            Position = bodyPos + new Vector2d(radius, 0),
            Velocity = bodyVel + new Vector2d(0, escape * 1.05),
        });
        Set(map, "_destinationBodyId", Titan);   // the panel follows the chosen destination first

        Invoke(map, "EnterOrbit");

        Assert.Equal(1, Burns(map));
        Assert.NotEmpty(Feathers(Paint(map)));
    }

    // ── AND THE OTHER HALF OF EVERY ONE OF THEM ───────────────────────────────────────────────────────

    /// <summary>
    /// A SHIP THAT HAS NOT BURNED DRAWS NO FLAME. The control for every row above: a freshly booted map,
    /// painted, has no feather on it and no beat down its ribbon. Without this row every assertion above is
    /// satisfied by a renderer that draws a flame unconditionally.
    ///
    /// <para>RED PROOF: let the pen ignore the clock — <c>BurnPlume.Shape(Math.Max(1, _lastBurnPulses),
    /// 0.0)</c>, a flame drawn unconditionally — and this row fails with feathers on a ship that has fired
    /// nothing.</para>
    /// </summary>
    [Fact]
    public void AShipThatHasNotBurnedDrawsNoFlame()
    {
        Pages.Map map = Boot();
        AtTheWallClock(map, 5_000);

        Assert.Equal(0, Burns(map));
        float[] frame = Paint(map);
        Assert.Empty(Feathers(frame));
        Assert.Empty(Beats(frame));
    }

    /// <summary>
    /// THE ISSUE'S THIRD BULLET, AT THE PEN: <i>"At high warp a burn instant can pass in one frame — the
    /// effect should be wall-clock-timed (~1 s), not sim-time-timed, so it reads at any warp."</i>
    ///
    /// <para>So: fire a scheduled transfer burn, then advance SIM time by the four minutes one frame of
    /// 10,000× warp actually covers while the wall clock moves the 16 ms of that one frame — the flame is
    /// still there. Then freeze sim time and let the WALL clock run past the window — the flame is gone,
    /// and so is the beat. Both halves, because a plume timed on the wrong clock passes either one alone.
    /// </para>
    ///
    /// <para>RED PROOF: time the whole thing in SIM seconds — stamp <c>_lastBurnMs</c> from
    /// <c>SimTime</c> and age it against <c>SimTime</c> — and the still-lit half fails: four minutes of
    /// the world have passed inside that one frame, so the flame is long over on the frame that should be
    /// showing it.</para>
    /// </summary>
    [Fact]
    public void TheFlameIsOnTheWallClockSoItReadsAtAnyWarp()
    {
        Pages.Map map = ArmedAt(Titan);
        AtTheWallClock(map, 5_000);
        Invoke(map, "ApplyTransferBurn", new Vector2d(120.0, -45.0));

        // One frame at 10,000×: 16 ms of the player's life, four minutes of the world's.
        ShipState ship = (ShipState)Read(map, "_ship")!;
        Set(map, "_ship", ship with { SimTime = ship.SimTime + 240.0 });
        AtTheWallClock(map, 5_016);
        Invoke(map, "ReprojectTrajectory");

        float[] lit = Paint(map);
        Assert.NotEmpty(Feathers(lit));
        Assert.NotEmpty(Beats(lit));

        // …and a second of the player's life later, with the world frozen, it is over.
        AtTheWallClock(map, 5_016 + (int)BurnPlume.FlashMs);
        float[] gone = Paint(map);
        Assert.Empty(Feathers(gone));
        Assert.Empty(Beats(gone));
    }

    /// <summary>
    /// THE FLAME LEAVES HER STERN, ON THE GLASS. Core proves the geometry; this proves the map hands it the
    /// right direction and the right origin — every feather starting <see cref="BurnPlume.NozzlePx"/> off
    /// the ship dot, all from the same point, and on the side away from the burn.
    ///
    /// <para>The ship's own screen pixel is not assumed: it is read off the frame's ship dot (the filled
    /// 4 px circle in her full ink), so this row measures the flame against the marker the player sees.
    /// "Away from the burn" is stated in the bench's own arithmetic — the Δv handed to the hook, flipped
    /// into screen space here — rather than by asking <see cref="BurnPlume.ExhaustAngle"/>, which would let
    /// a bad turn swing the yardstick along with the flame.</para>
    ///
    /// <para>RED PROOF: hand <c>BurnPlume.Nozzle</c> the raw world angle (<c>Atan2(dv.Y, dv.X)</c>, without
    /// the canvas-Y flip) and the row fails on every burn whose Δv is off the axes: the flame is mirrored
    /// about the horizontal.</para>
    /// </summary>
    [Fact]
    public void TheFlameLeavesHerSternOnTheGlass()
    {
        var deltaV = new Vector2d(120.0, -45.0);
        Pages.Map map = ArmedAt(Titan);
        AtTheWallClock(map, 5_000);
        Invoke(map, "ApplyTransferBurn", deltaV);

        float[] frame = Paint(map);
        (float sx, float sy) = HerDot(frame);
        List<float[]> feathers = Feathers(frame);
        Assert.True(feathers.Count >= BurnPlume.MinFeathers,
            $"only {feathers.Count} feathers in a burning frame — this row is not looking at the flame.");

        // Screen space: world Y is up, canvas Y is down. Aft is the opposite of where she was pushed.
        double aftX = -deltaV.X / deltaV.Length;
        double aftY = deltaV.Y / deltaV.Length;

        foreach (float[] feather in feathers)
        {
            double dx = feather[0] - sx;
            double dy = feather[1] - sy;
            Assert.Equal(BurnPlume.NozzlePx, Math.Sqrt((dx * dx) + (dy * dy)), 2);
            Assert.Equal(BurnPlume.NozzlePx, (dx * aftX) + (dy * aftY), 2);
        }

        // One drive, one flame — every feather leaves the same point, and the throat sits on it.
        Assert.Single(feathers.Select(f => ($"{f[0]:F3}", $"{f[1]:F3}")).Distinct());
        Assert.Contains(FlameCircles(frame), c =>
            Math.Abs(c.X - feathers[0][0]) < 1e-2 && Math.Abs(c.Y - feathers[0][1]) < 1e-2);
    }

    // ── READING THE PEN ───────────────────────────────────────────────────────────────────────────────

    private readonly record struct Mark(float Op, (float R, float G, float B, float A) Stroke, float[] Points);

    private const float OpPolyline = 1f, OpCircle = 2f, OpPolygon = 3f, OpImage = 4f, OpImageSlice = 5f;

    /// <summary>Walk the float buffer the flush was about to hand to JavaScript, in order — the
    /// <see cref="CanvasRenderer"/>'s own encoding (docs/m2-spec.md).</summary>
    private static List<Mark> Marks(float[] buffer)
    {
        var marks = new List<Mark>();
        int i = 6;
        while (i < buffer.Length)
        {
            float op = buffer[i++];
            if (op == OpPolyline)
            {
                var stroke = (buffer[i], buffer[i + 1], buffer[i + 2], buffer[i + 3]);
                int n = (int)buffer[i + 5];
                i += 6;
                marks.Add(new Mark(op, stroke, buffer[i..(i + (n * 2))]));
                i += n * 2;
            }
            else if (op == OpCircle)
            {
                var stroke = (buffer[i + 5], buffer[i + 6], buffer[i + 7], buffer[i + 8]);
                marks.Add(new Mark(op, stroke, [buffer[i + 10], buffer[i + 11], buffer[i + 12]]));
                i += 13;
            }
            else if (op == OpPolygon)
            {
                var stroke = (buffer[i + 5], buffer[i + 6], buffer[i + 7], buffer[i + 8]);
                int n = (int)buffer[i + 10];
                i += 11;
                marks.Add(new Mark(op, stroke, buffer[i..(i + (n * 2))]));
                i += n * 2;
            }
            else if (op == OpImage) { i += 6; }
            else if (op == OpImageSlice) { i += 10; }
            else
            {
                throw new InvalidOperationException(
                    $"unknown opcode {op} at {i - 1} — this reader and CanvasRenderer's encoding have parted.");
            }
        }
        return marks;
    }

    /// <summary>Her ink, read off the page itself rather than typed in a second time.</summary>
    private static readonly RgbaColor HerInk = (RgbaColor)typeof(Pages.Map)
        .GetField("ShipColor", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
        .GetValue(null)!;

    private static bool IsHerInk(in Mark m) =>
        m.Stroke.R == HerInk.R && m.Stroke.G == HerInk.G && m.Stroke.B == HerInk.B;

    /// <summary>Just the feathers: three-point polylines in her ink. The barrel line is two points and the
    /// ribbon beat is the whole leading stretch, so neither is mistaken for a flame.</summary>
    private static List<float[]> Feathers(float[] buffer) =>
    [
        .. Marks(buffer)
            .Where(m => m.Op == OpPolyline && m.Points.Length == BurnPlume.FloatsPerFeather && IsHerInk(m))
            .Select(m => m.Points)
    ];

    /// <summary>The beat: a long polyline in her ink, which nothing but the burn beat ever draws (the
    /// ribbon itself is amber, her barrel is two points, her dart is a polygon).</summary>
    private static List<float[]> Beats(float[] buffer) =>
    [
        .. Marks(buffer)
            .Where(m => m.Op == OpPolyline && m.Points.Length > BurnPlume.FloatsPerFeather && IsHerInk(m))
            .Select(m => m.Points)
    ];

    private static List<(float X, float Y, float R)> FlameCircles(float[] buffer) =>
    [
        .. Marks(buffer)
            .Where(m => m.Op == OpCircle && IsHerInk(m) && Math.Abs(m.Points[2] - 4f) > 1e-4)
            .Select(m => (m.Points[0], m.Points[1], m.Points[2]))
    ];

    /// <summary>The ship marker's own screen pixel: the 4 px circle in her FULL ink that <c>DrawShip</c>
    /// lays after the flame.</summary>
    private static (float X, float Y) HerDot(float[] buffer)
    {
        (float X, float Y, float R)[] dots = [.. Marks(buffer)
            .Where(m => m.Op == OpCircle && Math.Abs(m.Points[2] - 4f) < 1e-4
                        && m.Stroke == (HerInk.R, HerInk.G, HerInk.B, HerInk.A))
            .Select(m => (m.Points[0], m.Points[1], m.Points[2]))];
        Assert.Single(dots);
        return (dots[0].X, dots[0].Y);
    }

    // ── DRIVING A REAL MAP ────────────────────────────────────────────────────────────────────────────

    private const string Titan = "titan";

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol = new(() =>
        ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    /// <summary>A live component over the shipping scenario — the shipping ephemeris, the shipping
    /// simulator, the ship laid down by the page's own <c>InitializeShipState</c>, and the REAL command
    /// buffer.</summary>
    private static Pages.Map Boot()
    {
        var map = new Pages.Map();
        new ARendererThatDrawsNothing().Attach(map);
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_npcSimulator", new Simulator(ephemeris, TrafficSchedule.NpcTimeStep));
        Set(map, "_ship", Invoke(map, "InitializeShipState")!);
        Set(map, "_renderer", new CanvasRenderer("burn-canvas"));
        Invoke(map, "ReprojectTrajectory");
        return map;
    }

    /// <summary>…with the autopilot armed at a moon and a (burnless) transfer schedule in hand, which is
    /// what <c>ApplyTransferBurn</c> and <c>CheckArmedInsertion</c> both require of the page.</summary>
    private static Pages.Map ArmedAt(string bodyId)
    {
        Pages.Map map = Boot();
        Set(map, "_armedOrbitBodyId", bodyId);
        Set(map, "_armedTransferSchedule",
            new TransferPlanner.Schedule(Array.Empty<TransferPlanner.BurnStep>(), 0.0));
        return map;
    }

    /// <summary>The moon, its parent and where both are right now — asked of the page's own ephemeris, so
    /// the bench cannot hold a second opinion about where anything is.</summary>
    private static (CelestialBody Body, CelestialBody Parent, Vector2d Position, Vector2d Velocity) TheMoon(
        Pages.Map map, string bodyId)
    {
        var ephemeris = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody body = ephemeris.Bodies.Single(b => b.Id == bodyId);
        CelestialBody parent = ephemeris.Bodies.Single(b => b.Id == body.ParentId);
        double t = ((ShipState)Read(map, "_ship")!).SimTime;
        Vector2d position = ephemeris.Position(bodyId, t);
        Vector2d velocity = (ephemeris.Position(bodyId, t + 1) - ephemeris.Position(bodyId, t - 1)) / 2.0;
        return (body, parent, position, velocity);
    }

    /// <summary>Put the RENDERER's clock — the one the flame lives on — at this millisecond.</summary>
    private static void AtTheWallClock(Pages.Map map, int ms) => Set(map, "_lastTimestampMs", (double?)ms);

    /// <summary>How many burns have gone through the one hook.</summary>
    private static int Burns(Pages.Map map) => (int)Read(map, "_burnsFired")!;

    private static readonly Type NodeType = typeof(Pages.Map).GetNestedType("PlanNode", Hidden)!;

    private static FieldInfo NodeField(string name) => NodeType.GetField(name, Hidden)!;

    private static System.Collections.IList Nodes(Pages.Map map) =>
        (System.Collections.IList)Read(map, "_planNodes")!;

    /// <summary>Paint one map frame and hand back the command buffer the flush was about to send.</summary>
    private static float[] Paint(Pages.Map map)
    {
        try
        {
            Invoke(map, "PaintTheMapFrame");
        }
        catch (PlatformNotSupportedException)
        {
            // The canvas flush. See the class note.
        }

        object renderer = Read(map, "_renderer")!;
        var buffer = (float[])renderer.GetType().GetField("_buffer", Hidden)!.GetValue(renderer)!;
        int length = (int)renderer.GetType().GetField("_length", Hidden)!.GetValue(renderer)!;
        return buffer[..length];
    }

    // ── READING THE SOURCE ────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<(string Path, string Source)> ClientSources() =>
        Directory.GetFiles(ClientRoot, "*.cs", SearchOption.AllDirectories)
                 .Concat(Directory.GetFiles(ClientRoot, "*.razor", SearchOption.AllDirectories))
                 .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                          && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                 .OrderBy(p => p, StringComparer.Ordinal)
                 .Select(p => (p, File.ReadAllText(p)));

    /// <summary>Every <c>PlayCue</c> call site in the shipped client and the names it can fire — the same
    /// reading <c>EveryCueTheGameFiresHasAVoiceTests</c> does, for the same reason: three sites hand a
    /// ternary and two a <c>switch</c> expression, so a scan that parsed one argument would miss them.
    /// </summary>
    private static List<(string Where, IReadOnlyList<string> Names)> CueSites()
    {
        var sites = new List<(string, IReadOnlyList<string>)>();
        foreach ((string path, string src) in ClientSources())
        {
            foreach (Match call in Regex.Matches(src, @"\bPlayCue\("))
            {
                int semicolon = src.IndexOf(';', call.Index);
                if (semicolon < 0)
                {
                    continue;
                }

                int lineStart = src.LastIndexOf('\n', Math.Max(call.Index - 1, 0)) + 1;
                string lead = src[lineStart..call.Index].TrimStart();
                if (lead.StartsWith("//", StringComparison.Ordinal) || lead.StartsWith("*", StringComparison.Ordinal))
                {
                    continue; // prose about a cue, not a cue.
                }

                int line = src.Take(call.Index).Count(c => c == '\n') + 1;
                sites.Add(($"{Relative(path)}:{line}",
                    Regex.Matches(src[call.Index..semicolon], "\"([^\"\\\\\n]*)\"")
                         .Select(m => m.Groups[1].Value)
                         .Where(n => n.Length > 0)
                         .ToList()));
            }
        }
        return sites;
    }

    private static string Relative(string path) =>
        Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/');

    private static string ClientRoot => Path.Combine(RepoRoot(), "src", "SpaceSails.Client");

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new InvalidOperationException("could not find the repository root from the test assembly.");
    }

    private static object? Read(Pages.Map map, string name) =>
        typeof(Pages.Map).GetField(name, Hidden)?.GetValue(map)
        ?? typeof(Pages.Map).GetProperty(name, Hidden)?.GetValue(map);

    private static void Set(Pages.Map map, string name, object? value)
    {
        FieldInfo? field = typeof(Pages.Map).GetField(name, Hidden);
        if (field is not null)
        {
            field.SetValue(map, value);
            return;
        }
        typeof(Pages.Map).GetProperty(name, Hidden)!.SetValue(map, value);
    }

    private static object? Invoke(Pages.Map map, string name, params object?[] args)
    {
        try
        {
            return typeof(Pages.Map).GetMethod(name, Hidden)!.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

#pragma warning disable BL0006 // the framework's own seam: a component needs a renderer to have a dispatcher
    private sealed class ARendererThatDrawsNothing : Microsoft.AspNetCore.Components.RenderTree.Renderer
    {
        public ARendererThatDrawsNothing()
            : base(NoServices.Instance, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance) { }

        public override Dispatcher Dispatcher { get; } = new RightHere();

        public void Attach(IComponent component) => AssignRootComponentId(component);

        protected override void HandleException(Exception exception) =>
            throw new InvalidOperationException("the frame threw inside the renderer", exception);

        protected override System.Threading.Tasks.Task UpdateDisplayAsync(
            in Microsoft.AspNetCore.Components.RenderTree.RenderBatch batch) =>
            System.Threading.Tasks.Task.CompletedTask;

        private sealed class RightHere : Dispatcher
        {
            public override bool CheckAccess() => true;

            public override System.Threading.Tasks.Task InvokeAsync(Action workItem)
            {
                workItem();
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public override System.Threading.Tasks.Task InvokeAsync(Func<System.Threading.Tasks.Task> workItem) =>
                workItem();

            public override System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem) =>
                System.Threading.Tasks.Task.FromResult(workItem());

            public override System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(
                Func<System.Threading.Tasks.Task<TResult>> workItem) => workItem();
        }

        private sealed class NoServices : IServiceProvider
        {
            public static readonly NoServices Instance = new();

            public object? GetService(Type serviceType) => null;
        }
    }
#pragma warning restore BL0006
}
