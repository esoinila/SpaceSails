using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #708 · THE SUIT'S HEADLIGHTS, AND A FLOOR THAT IS GENUINELY DARK.
///
/// <para>Owner: <i>"For dark levels our suit should have forward facing headlights … the pre-existing
/// tunnels would be scary as dark ones and totally different style."</i></para>
///
/// <para>These walk the REAL renderer — <see cref="DeckView.Draw"/> on a real
/// <see cref="HiveInterior.FloorDeck"/>, through a recording <see cref="IRenderer"/> that keeps every
/// primitive the canvas would have been asked for. The question is not whether a helper computes a cone; it
/// is whether the ink ever lands outside it. <b>Not dim — absent.</b></para>
///
/// <para>Proved able to fail against the renderer as it stood before the lamp was fitted — the darkness flag
/// plumbed through and honoured by nobody, which is exactly what "today's renderer" means here:
/// <c>NOTHINGIsDrawnOutsideTheLight</c> opens with <b>222 of 226 primitive(s) drawn in the dark, outside the
/// cone</b>, and <c>TurningRoundShowsYouADifferentBuilding</c> with <b>every facing drew the same amount of
/// building (226, 226, 226, 226)</b>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHeadlightsAreTheWholeOfTheSeeingTests
{
    // A deep floor of a real site, deterministic per body+level. Deep enough to be a corridor rather than a
    // lobby, and one the A* audits already flood (§13.1).
    private const string Body = "luna";
    private const int Level = -4;
    private const int WidthPx = 1280;
    private const int HeightPx = 560;

    private static DeckPlan Floor => HiveInterior.FloorDeck(
        Body, Level, MoonSurface.ExpeditionField(), 0, (_, _) => { }, []);

    // ── The recording pen ────────────────────────────────────────────────────────────────────────────────

    /// <summary>One thing the canvas was asked to draw: its screen points, its ink, and what kind it was.</summary>
    private sealed record Mark(string Kind, float[] Points, RgbaColor Ink, string Text = "");

    private sealed class RecordingRenderer : IRenderer
    {
        public List<Mark> Marks { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Marks.Clear();

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("circle", [x - r, y, x + r, y, x, y - r, x, y + r, x, y], fill ?? stroke));

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("polyline", pts.ToArray(), stroke));

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Marks.Add(new Mark("polygon", pts.ToArray(), fill ?? stroke));

        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) =>
            Marks.Add(new Mark("text", [x, y], c, text));

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) =>
            Marks.Add(new Mark("image", [x, y, x + w, y, x, y + h, x + w, y + h], new RgbaColor(0, 0, 0)));

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) =>
            Marks.Add(new Mark("slice", [x, y, x + w, y, x, y + h, x + w, y + h], new RgbaColor(0, 0, 0)));
    }

    /// <summary>
    /// How far the frame is nudged DOWN to tell the WORLD from the CHROME. The deck's own ink is anchored to
    /// the ground and rides the render pan; the keybar, the gauges and the standing prompt are anchored to
    /// the canvas edges and do not move. Nudging and looking at what moved is the only discriminator that
    /// cannot go stale — a test carrying a list of "HUD things to skip" is a second copy of the renderer's
    /// layout, and it would quietly start excusing real leaks the day somebody added a widget.
    ///
    /// <para>DOWN and not across, deliberately: the keybar and the standing prompt are centred on the plan
    /// origin rather than on the viewport, so they DO slide with a horizontal pan (an old quirk, and not this
    /// issue's to fix). Nothing in the chrome takes its Y off the camera.</para>
    /// </summary>
    private const double PanProbePx = 37.0;

    /// <summary>Draw one frame of a real floor, twice, and hand back the ink that belongs to the GROUND —
    /// with the canvas-anchored chrome sorted out of it — plus the screen→deck transform.</summary>
    private static (List<Mark> World, List<Mark> Chrome, Func<float, float, (double X, double Y)> ToWorld) Frame(
        DeckPlan plan, double ax, double ay, double heading, bool dark, DeckView.SurfaceHud? hud = null)
    {
        List<Mark> Once(double panY)
        {
            var pen = new RecordingRenderer();
            new DeckView(pen).Draw(plan, WidthPx, HeightPx, 0,
                new DeckView.State(ax, ay, heading, 0, 0, ShuttleAway: false, ElectricUniverse: false,
                                   Dark: dark),
                0, panY, hud);
            return pen.Marks;
        }

        List<Mark> still = Once(0);
        List<Mark> nudged = Once(PanProbePx);
        Assert.Equal(still.Count, nudged.Count);    // the pan may move ink; it may never change what exists

        var world = new List<Mark>();
        var chrome = new List<Mark>();
        for (int i = 0; i < still.Count; i++)
        {
            bool moved = still[i].Points.Length > 1
                && Math.Abs(nudged[i].Points[1] - still[i].Points[1] - PanProbePx) < 0.01;
            (moved ? world : chrome).Add(still[i]);
        }

        // DeckView's own world→screen, inverted. Written out here rather than borrowed from the renderer on
        // purpose: a test that reuses the transform under test cannot notice the transform being wrong.
        float scale = Math.Min(WidthPx / 64f, HeightPx / 28f);
        float ox = plan.FollowCam ? (WidthPx / 2f) - ((float)ax * scale) : WidthPx / 2f;
        float oy = plan.FollowCam ? (HeightPx / 2f) + ((float)ay * scale) : HeightPx / 2f;
        return (world, chrome, (px, py) => ((px - ox) / scale, (oy - py) / scale));
    }

    /// <summary>The law itself, asked of the world and not of the renderer: does the suit light this point?
    /// The ring is <see cref="DeckPlan.InteractRadius"/> — see the note on <c>DeckView.LampRingDu</c>.</summary>
    private static bool Lit(double ax, double ay, double heading, double x, double y) =>
        SuitLamp.Lights(ax, ay, heading, x, y, DeckPlan.InteractRadius);

    /// <summary>
    /// Is this mark wholly in the black — no part of it lit?
    ///
    /// <para>ALONG the runs and not merely at the corners. A Hive spine is a single stroke ninety du long, so
    /// both of its ends can be in the dark while the middle of it crosses the cone; a first cut of this test
    /// judged the vertices, called two such walls leaks, and would have gone on to demand that the renderer
    /// delete exactly the wall the captain was standing in front of.</para>
    /// </summary>
    private static bool WhollyDark(Mark m, Func<float, float, (double X, double Y)> toWorld,
                                   double ax, double ay, double heading)
    {
        int n = m.Points.Length / 2;
        if (n == 0)
        {
            return false;
        }

        (double X, double Y) At(int i) => toWorld(m.Points[i * 2], m.Points[(i * 2) + 1]);

        for (int i = 0; i < n; i++)
        {
            (double x0, double y0) = At(i);
            if (Lit(ax, ay, heading, x0, y0))
            {
                return false;
            }

            if (i + 1 >= n)
            {
                break;
            }

            (double x1, double y1) = At(i + 1);
            double len = Math.Sqrt(((x1 - x0) * (x1 - x0)) + ((y1 - y0) * (y1 - y0)));
            int steps = Math.Clamp((int)Math.Ceiling(len / 0.4), 1, 512);
            for (int s = 1; s < steps; s++)
            {
                double t = (double)s / steps;
                if (Lit(ax, ay, heading, x0 + ((x1 - x0) * t), y0 + ((y1 - y0) * t)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>The blackout itself is drawn in the black, obviously. It is the only thing allowed to be.</summary>
    private static bool IsTheBlackout(Mark m) => m.Ink is { R: 0, G: 0, B: 0, A: 255 };

    // ── 1 · The law ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// On a DARK floor, nothing whatever is drawn outside the headlights and the arm's-reach ring. Not
    /// dimmed, not hatched, not a hint of a wall — absent, the way an unlit hall on an airless world with
    /// nothing to scatter light actually is.
    /// </summary>
    [Xunit.Fact]
    public void NOTHINGIsDrawnOutsideTheLight()
    {
        DeckPlan plan = Floor;
        (double ax, double ay) = (plan.SpawnX, plan.SpawnY);
        const double heading = 0.0;

        (List<Mark> marks, _, var toWorld) = Frame(plan, ax, ay, heading, dark: true);

        List<Mark> leaks = marks
            .Where(m => !IsTheBlackout(m) && WhollyDark(m, toWorld, ax, ay, heading))
            .ToList();

        if (leaks.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{leaks.Count} of {marks.Count} primitive(s) drawn in the dark, outside the cone:");
            foreach (Mark m in leaks.Take(12))
            {
                (double wx, double wy) = toWorld(m.Points[0], m.Points[1]);
                sb.AppendLine($"  {m.Kind,-8} at ({wx,7:F1},{wy,6:F1}) du  "
                            + $"ink {m.Ink.R},{m.Ink.G},{m.Ink.B},{m.Ink.A}  "
                            + $"{(m.Text.Length > 0 ? '"' + m.Text + '"' : "")}");
            }

            Xunit.Assert.Fail(sb.ToString());
        }
    }

    /// <summary>
    /// …and the world HAS things out there to hide, which is the half of the pair that stops the one above
    /// from being a green test that asserts nothing (the fifth class). The identical frame with the lights on
    /// draws hundreds of primitives in exactly the ground the dark frame leaves empty.
    /// </summary>
    [Xunit.Fact]
    public void AndTheSameFloorWITHItsLightsOnIsFullOfThemSoTheGuardHasSomethingToCatch()
    {
        DeckPlan plan = Floor;
        (double ax, double ay) = (plan.SpawnX, plan.SpawnY);
        const double heading = 0.0;

        (List<Mark> marks, _, var toWorld) = Frame(plan, ax, ay, heading, dark: false);
        int outside = marks.Count(m => WhollyDark(m, toWorld, ax, ay, heading));

        // #563 slice 2 · The floor was 100 and is 50, re-measured rather than nudged. The frame's cull now
        // reaches doors and console plates as well as walls, and a deck's view is 64 du across while this
        // floor is three hundred — so the two hundred-odd marks that went were drawn entirely past the edge
        // of the glass and were never evidence of anything a captain could have seen. What is left is 70 of
        // 75, which is the claim this guard is actually making: with the lights ON, nearly the whole frame
        // stands in ground the dark frame leaves empty.
        Xunit.Assert.True(outside > 50,
            $"a LIT floor should be drawn far past the lamp's reach; only {outside} of {marks.Count} were");
        Xunit.Assert.True(outside * 4 > marks.Count * 3,
            $"only {outside} of {marks.Count} lit marks stand outside the cone — the blackout has almost "
            + "nothing left to hide, so its guard has almost nothing left to prove.");
        Xunit.Assert.DoesNotContain(marks, IsTheBlackout);   // and nothing is blacked out on a lit floor
    }

    /// <summary>
    /// TURNING YOUR BODY IS AN ACT OF LOOKING. The same captain, standing on the same square of the same
    /// floor, facing the other way, is shown a different building. That is the mechanic in one assertion.
    /// </summary>
    [Xunit.Fact]
    public void TurningRoundShowsYouADifferentBuilding()
    {
        DeckPlan plan = Floor;
        (double ax, double ay) = (plan.SpawnX, plan.SpawnY);

        int Ahead(double heading)
        {
            (List<Mark> marks, _, _) = Frame(plan, ax, ay, heading, dark: true);
            return marks.Count(m => !IsTheBlackout(m));
        }

        var counts = new List<int>();
        for (int q = 0; q < 4; q++)
        {
            counts.Add(Ahead(q * Math.PI / 2));
        }

        Xunit.Assert.True(counts.Distinct().Count() > 1,
            $"every facing drew the same amount of building ({string.Join(", ", counts)}) — "
            + "the cone is not pointed by the captain's heading");
        Xunit.Assert.All(counts, c => Xunit.Assert.True(c > 0, "some facing showed nothing at all"));
    }

    // ── 2 · The instruments are not part of the world ────────────────────────────────────────────────────

    /// <summary>
    /// THE MOTION FAN IS UNTOUCHED BY THE DARK. A contact the instrument hears through a wall, behind you,
    /// forty du away in a hall your lights will never reach, smudges exactly as it does with the lights on —
    /// bit for bit, the same rings in the same places. Hearing what you cannot see is the entire point of
    /// putting the lights out (#591, #371).
    ///
    /// <para>Proved able to fail by teaching the renderer to skip the smudge in darkness — one word on one
    /// line, and exactly what drawing the blackout in the wrong place would do for free: <c>5 smudge ring(s)
    /// with the lights on, 1 with them out</c>. (The one that survived was the GHOST, which is the other half
    /// of the instrument and would have been the next thing to go.)</para>
    /// </summary>
    [Xunit.Fact]
    public void WhatTheFanHEARSIsDrawnTheSameInTheDarkAsInTheLight()
    {
        DeckPlan plan = Floor;
        (double ax, double ay) = (plan.SpawnX, plan.SpawnY);
        const double heading = 0.0;

        // Behind the captain and well past the lamp: a place the cone can never reach at this facing.
        (double sx, double sy) = (ax - 30.0, ay + 6.0);
        Xunit.Assert.False(Lit(ax, ay, heading, sx, sy), "the test's own contact is standing in the light");

        DeckView.SurfaceHud Hud() => new(
            DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
            // #830: a return now says WHICH KIND it is. This one is a mover, as it always was.
            Blips: new[] { (Math.Atan2(sy - ay, sx - ax), 30.6, false) },
            Cadence: (int)MotionTracker.Cadence.Closing, Readout: "movement — 31 du",
            CacheMarks: Array.Empty<(double, double, bool)>(),
            Nerve: NerveModel.Steady, NerveReadout: "steady",
            Smudges: new[] { (sx, sy, 4.0) },
            Ghosts: new[] { (sx + 2, sy + 2, 0.6) },
            FanReach: 128.0);

        static List<string> Rings(List<Mark> marks, Func<float, float, (double X, double Y)> toWorld,
                                  double atX, double atY)
        {
            var found = new List<string>();
            foreach (Mark m in marks.Where(m => m.Kind == "circle"))
            {
                // A recorded circle keeps its centre as its last point pair.
                (double wx, double wy) = toWorld(m.Points[^2], m.Points[^1]);
                if (Math.Abs(wx - atX) < 6 && Math.Abs(wy - atY) < 6)
                {
                    (double ex, double _) = toWorld(m.Points[0], m.Points[1]);
                    found.Add($"({wx:F2},{wy:F2}) r={Math.Abs(wx - ex):F2} ink={m.Ink.R},{m.Ink.G},{m.Ink.B},{m.Ink.A}");
                }
            }

            return found;
        }

        (List<Mark> litMarks, _, var toWorldLit) = Frame(plan, ax, ay, heading, dark: false, Hud());
        (List<Mark> darkMarks, _, var toWorldDark) = Frame(plan, ax, ay, heading, dark: true, Hud());

        List<string> withLights = Rings(litMarks, toWorldLit, sx, sy);
        List<string> withoutLights = Rings(darkMarks, toWorldDark, sx, sy);

        Xunit.Assert.True(withLights.Count > 0, "the fixture drew no smudge at all — the guard proves nothing");
        Xunit.Assert.True(withLights.SequenceEqual(withoutLights),
            $"the fan reads differently in the dark:{Environment.NewLine}"
            + $"  {withLights.Count} smudge ring(s) with the lights on, {withoutLights.Count} with them out"
            + $"{Environment.NewLine}  on:  {string.Join(" | ", withLights)}"
            + $"{Environment.NewLine}  out: {string.Join(" | ", withoutLights)}");
    }

    // ── 3 · The affordance law ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #212 · AN AFFORDANCE THE GAME WILL LET YOU USE IS NEVER INVISIBLE. The arm's-reach ring is
    /// <see cref="DeckPlan.InteractRadius"/> and not a number of its own, so a console close enough to press
    /// [E] on is always drawn, however the captain happens to be facing. Stand on one, face away, and it is
    /// still there.
    /// </summary>
    [Xunit.Fact]
    public void AConsoleYouCanREACHIsAlwaysDrawnHoweverYouAreFacing()
    {
        DeckPlan plan = Floor;
        Xunit.Assert.NotEmpty(plan.Consoles);
        DeckPlan.ConsoleSpot spot = plan.Consoles[0];

        // Standing an arm's length short of it, facing directly away.
        double ax = spot.X - (DeckPlan.InteractRadius * 0.8), ay = spot.Y;
        const double away = Math.PI;

        Xunit.Assert.True(Lit(ax, ay, away, spot.X, spot.Y),
            "the fixture put the console outside the ring — nothing is being proved");

        (List<Mark> marks, _, var toWorld) = Frame(plan, ax, ay, away, dark: true);
        bool drawn = marks.Any(m =>
        {
            for (int i = 0; i + 1 < m.Points.Length; i += 2)
            {
                (double wx, double wy) = toWorld(m.Points[i], m.Points[i + 1]);
                if (!IsTheBlackout(m) && Math.Abs(wx - spot.X) < 1.0 && Math.Abs(wy - spot.Y) < 1.0)
                {
                    return true;
                }
            }

            return false;
        });

        Xunit.Assert.True(drawn,
            $"'{spot.Label}' is within [E] reach and was not drawn at all — an affordance you can use and cannot see");
    }
}
