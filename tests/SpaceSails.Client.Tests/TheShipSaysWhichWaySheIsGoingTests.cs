using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #933 · WHICH WAY SHE IS GOING — the velocity arrowhead at the ship marker.
///
/// <para>Owner, 2026-08-17 (playing the flight side): <i>"our ship shape on the map could indicate little
/// better about where it is going if it was depicted as arrow like triangle … more like add shape that points
/// to the direction the ship is going even when its motion is stopped during the burn parameter
/// selection."</i></para>
///
/// <h3>These guards read the PEN, not the source</h3>
/// <para>Every question here is a question about a drawn shape, and this repo's third named bug class is
/// exactly the drawn shape disagreeing with the sim. So a real <see cref="Pages.Map"/> is booted over the
/// shipping <c>scenarios/sol.json</c>, its own <c>PaintTheMapFrame</c> is run into the real
/// <see cref="CanvasRenderer"/>'s command buffer, and the triangle is read back OUT of that buffer as six
/// floats. Nothing below asserts that a line of code exists; everything below asserts what got drawn.</para>
///
/// <para>(The flush at the end of <c>PaintTheMapFrame</c> is the one line that crosses into JavaScript and
/// throws <c>PlatformNotSupportedException</c> off-browser — the buffer is complete by then, which is the
/// same seam <see cref="EveryFrameLeavesTheSameFingerprintTests"/> rides.)</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheShipSaysWhichWaySheIsGoingTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The ship's own ink (<c>Map.Plot.cs</c>'s <c>ShipColor</c>) — the arrowhead is drawn in it and
    /// in nothing new, so this is also what tells her triangle apart from every other polygon in the frame.</summary>
    private static readonly (float R, float G, float B, float A) HerInk = (255, 210, 80, 255);

    // ── GUARD (a) · THE ANGLE IS THE VELOCITY'S ANGLE IN THE FRAME THE PLAN IS READ IN ─────────────────

    /// <summary>
    /// LAW ONE — the arrowhead points along the ship's velocity <b>relative to the plot frame</b>, the same
    /// velocity the <c>v helio</c> / <c>v rel {body}</c> readout is built from (#135/#926). Switch the frame
    /// and the arrow swings, because "moving" was never a property of the ship alone.
    ///
    /// <para>ANTI-VACUOUS, and it is asserted rather than assumed: the ship is put in a real Earth orbit —
    /// Earth's own heliocentric velocity plus 3 km/s across it — so the two answers are about ninety degrees
    /// apart, and the test FIRST asserts that they differ by more than a degree. Handed a state where the two
    /// frames agreed, this row could never tell pass from fail (the fifth named bug class).</para>
    ///
    /// <para>RED PROOF: draw the heliocentric velocity under Earth's frame — i.e. give
    /// <c>DrawVelocityArrowhead</c> <c>_ship.Velocity</c> instead of <c>FrameRelativeVelocity(SimTime)</c> —
    /// and the Earth-frame half fails by ~85°, naming both angles.</para>
    /// </summary>
    [Fact]
    public void TheArrowheadPointsAlongTheVelocityInTheFrameThePlanIsReadIn()
    {
        Pages.Map sun = Boot();
        Pages.Map earth = Boot();
        InEarthOrbit(sun);
        InEarthOrbit(earth);
        Set(earth, "_plotFrameBodyId", "earth");

        double heliocentric = ScreenAngleOf(VelocityIn(sun));
        double earthRelative = ScreenAngleOf(VelocityIn(earth));

        Assert.True(Math.Abs(Wrap180(heliocentric - earthRelative)) > 1.0,
            "the Sun's frame and Earth's frame agree about which way she is going on this state — so this " +
            $"row could not tell one from the other. helio {heliocentric:F3} rad, rel {earthRelative:F3} rad.");

        AssertPointsAlong(Paint(sun), heliocentric, "the Sun's frame");
        AssertPointsAlong(Paint(earth), earthRelative, "Earth's frame");
    }

    // ── GUARD (b) · DRAWN WHILE THE BURN IS BEING PLANNED ──────────────────────────────────────────────

    /// <summary>
    /// LAW TWO — the shape is drawn with the sim PAUSED, which is the owner's own words: <i>"even when its
    /// motion is stopped during the burn parameter selection"</i>. Velocity is state, not motion.
    ///
    /// <para>The map is put into plot mode the way the game puts it there — <c>EnterPlotMode()</c>, which is
    /// what sets <c>Paused</c> — and the pause is asserted before the frame is painted, so this row cannot
    /// pass by quietly running a live map.</para>
    ///
    /// <para>RED PROOF: gate <c>DrawVelocityArrowhead</c> on <c>!Paused</c> (or on any "is the sim running"
    /// question) and this fails with no triangle in the buffer at all.</para>
    /// </summary>
    [Fact]
    public void SheStillSaysWhichWaySheIsGoingWhileTheBurnIsBeingPlanned()
    {
        Pages.Map map = Boot();
        InEarthOrbit(map);
        Invoke(map, "EnterPlotMode");

        Assert.True((bool)Read(map, "Paused")!, "EnterPlotMode did not pause the sim — this row is not " +
            "standing at the plotting desk at all, so it could never catch a gate on the clock.");
        Assert.True((bool)Read(map, "PlotMode")!, "the map is not in plot mode.");

        AssertPointsAlong(Paint(map), ScreenAngleOf(VelocityIn(map)), "the paused plotting desk");
    }

    // ── GUARD (c) · A RING WHEN SHE IS NOT GOING ANYWHERE IN THIS FRAME ────────────────────────────────

    /// <summary>
    /// LAW THREE — below <see cref="VelocityArrow.RingBelowMps"/> the dart collapses to a ring, and above it
    /// there is a dart. Both halves, so neither direction of the mistake survives.
    ///
    /// <para>The parked state is not invented for the bench: the shipping boot lays the ship down CO-MOVING
    /// with Earth (<c>InitializeShipState</c>, "velocity stays Earth's"), so read in Earth's frame she is
    /// exactly stopped — which is the very state the ring exists for, and the one that would otherwise draw a
    /// dart spinning on ephemeris noise.</para>
    ///
    /// <para>RED PROOF, both ways: delete the <c>ShowsRing</c> branch and the parked half fails (a triangle
    /// where a ring was promised); make the branch unconditional and the moving half fails (a ring on a ship
    /// doing three kilometres a second).</para>
    /// </summary>
    [Fact]
    public void ParkedInThisFrameSheGetsARingAndMovingSheGetsADart()
    {
        // Parked: the shipping boot's own co-moving ship, read in Earth's frame.
        Pages.Map parked = Boot();
        Set(parked, "_plotFrameBodyId", "earth");
        Assert.True(VelocityIn(parked).Length < VelocityArrow.RingBelowMps,
            $"the booted ship is doing {VelocityIn(parked).Length:F6} m/s in Earth's frame — that is not " +
            "parked, so this half is asserting nothing.");

        float[] parkedFrame = Paint(parked);
        Assert.Null(FindHerTriangle(parkedFrame));
        Assert.True(HasHerRing(parkedFrame),
            "she is stopped dead in this frame and the map drew no ring — a zero-length arrow, or nothing " +
            "at all, is not an answer to 'which way am I going'.");

        // Moving: the same ship in the same frame, given a real orbital velocity across Earth's.
        Pages.Map moving = Boot();
        InEarthOrbit(moving);
        Set(moving, "_plotFrameBodyId", "earth");
        Assert.True(VelocityIn(moving).Length > VelocityArrow.RingBelowMps * 100,
            "the moving half is not comfortably clear of the threshold — it would prove nothing about it.");

        float[] movingFrame = Paint(moving);
        Assert.NotNull(FindHerTriangle(movingFrame));
        Assert.False(HasHerRing(movingFrame),
            "she is doing kilometres a second and the map still drew the parked ring.");
    }

    /// <summary>…and the threshold is a REAL edge, not a number that selects everything: a hair under it is a
    /// ring, a hair over it is a dart. (A threshold no state ever sits either side of is the fourth way a
    /// green guard asserts nothing — 34 du when the nearest real case is 34.2.)</summary>
    [Fact]
    public void TheThresholdIsAnEdgeSomethingActuallySitsEitherSideOf()
    {
        Assert.True(VelocityArrow.ShowsRing(VelocityArrow.RingBelowMps * 0.99));
        Assert.False(VelocityArrow.ShowsRing(VelocityArrow.RingBelowMps * 1.01));

        Pages.Map justUnder = Boot();
        Nudge(justUnder, VelocityArrow.RingBelowMps * 0.5);
        Set(justUnder, "_plotFrameBodyId", "earth");
        Assert.True(HasHerRing(Paint(justUnder)));
        Assert.Null(FindHerTriangle(Paint(justUnder)));

        Pages.Map justOver = Boot();
        Nudge(justOver, VelocityArrow.RingBelowMps * 2.0);
        Set(justOver, "_plotFrameBodyId", "earth");
        Assert.NotNull(FindHerTriangle(Paint(justOver)));
        Assert.False(HasHerRing(Paint(justOver)));
    }

    // ── GUARD (d) · THE SAME SIZE AT EVERY ZOOM ────────────────────────────────────────────────────────

    /// <summary>
    /// LAW FOUR — it is a GLYPH. The map spans thirteen orders of magnitude of zoom, so a triangle sized in
    /// world metres would be a hair at one end and would swallow the solar system at the other. Two zoom
    /// levels four orders of magnitude apart draw a triangle with the same three side lengths, to the float.
    ///
    /// <para>RED PROOF: build the arrowhead in world space the way the barrel line is built
    /// (<c>LengthPx * _camera.MetersPerPixel</c>) and the sides come out ten-thousand-fold apart.</para>
    /// </summary>
    [Fact]
    public void TheArrowheadIsTheSameSizeAtEveryZoom()
    {
        float[] wide = SidesOfHerTriangleAt(1e9);
        float[] close = SidesOfHerTriangleAt(1e5);

        Assert.Equal(wide.Length, close.Length);
        for (int i = 0; i < wide.Length; i++)
        {
            Assert.True(Math.Abs(wide[i] - close[i]) < 1e-3,
                $"side {i} of the arrowhead is {wide[i]:F4} px at 1e9 m/px and {close[i]:F4} px at 1e5 m/px " +
                "— it is being sized in world metres, not in pixels.");
        }

        // …and the apex really is the apex angle Core names, at both ends of that zoom range.
        Assert.Equal(VelocityArrow.ApexDegrees, ApexDegreesAt(1e9), 3);
        Assert.Equal(VelocityArrow.ApexDegrees, ApexDegreesAt(1e5), 3);
    }

    // ── GUARD (e) · AND HER MAST IS EXACTLY WHERE IT WAS ───────────────────────────────────────────────

    /// <summary>
    /// LAW FIVE — Lab 43's discharge is untouched. The arrowhead is drawn AFTER the ship dot and adds one
    /// polygon (or one circle); the mast, the filaments and the core at the tip are the same marks in the
    /// same order they were before, because that plume is a physical claim this lane has no business editing.
    ///
    /// <para>The control is exact: ONE ship state — the shipping boot's own, co-moving with Earth — painted
    /// twice with the contactor arcing, once read in the Sun's frame (29.8 km/s: a dart) and once in Earth's
    /// (dead stop: a ring). The ship's position, her charge and her heading are byte-identical across the
    /// two, so the plume MUST be too; only the new pass has anything to change. RED PROOF: move
    /// <c>DrawVelocityArrowhead</c> above <c>DrawDischarge</c>, or paint it in <c>ArcHaloColor</c>, or let it
    /// touch <c>_lastDischargeMs</c>, and the plume's marks move, multiply or reorder and this fails naming
    /// the mark.</para>
    /// </summary>
    [Fact]
    public void TheDischargeOffHerMastIsUntouched()
    {
        Pages.Map dart = Boot();
        MakeHerArc(dart);
        float[] dartFrame = Paint(dart);

        List<string> plume = DischargeMarks(dartFrame);
        Assert.True(plume.Count > 0,
            "no discharge marks in the frame at all — this row is not looking at Lab 43's plume.");
        Assert.NotNull(FindHerTriangle(dartFrame));

        Pages.Map ring = Boot();
        MakeHerArc(ring);
        Set(ring, "_plotFrameBodyId", "earth");
        float[] ringFrame = Paint(ring);
        Assert.True(HasHerRing(ringFrame), "the ringed control drew no ring — the two frames are not a pair.");
        Assert.Null(FindHerTriangle(ringFrame));

        Assert.Equal(plume, DischargeMarks(ringFrame));
    }

    // ── The words the Guide teaches ────────────────────────────────────────────────────────────────────

    /// <summary>LAW SIX — the in-game Guide's plotting section teaches the two shapes apart, in the sentence
    /// the lane was given. RED PROOF: reword it and this fails on the exact text.</summary>
    [Fact]
    public void TheGuideTeachesTheTwoShapesApart()
    {
        string guide = System.Text.RegularExpressions.Regex.Replace(
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Guide.razor")),
            @"\s+", " ");
        Assert.Contains(
            "the arrowhead is where she is going in the frame you are reading; the nose at a node is where the burn pushes",
            guide);
    }

    // ── READING THE PEN ────────────────────────────────────────────────────────────────────────────────

    /// <summary>One primitive out of the command buffer: its opcode, its stroke ink, and its points.</summary>
    private readonly record struct Mark(float Op, (float R, float G, float B, float A) Stroke, float[] Points);

    private const float OpPolyline = 1f, OpCircle = 2f, OpPolygon = 3f, OpImage = 4f, OpImageSlice = 5f;

    /// <summary>Walk the float buffer the flush was about to hand to JavaScript, in order. The encoding is
    /// <c>CanvasRenderer</c>'s own (docs/m2-spec.md): six floats of header, then opcode-tagged records.</summary>
    private static List<Mark> Marks(float[] buffer)
    {
        var marks = new List<Mark>();
        int i = 6;   // the BeginFrame header: width, height, and the background's four channels
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
            else if (op == OpImage)
            {
                i += 6;
            }
            else if (op == OpImageSlice)
            {
                i += 10;
            }
            else
            {
                throw new InvalidOperationException(
                    $"unknown opcode {op} at {i - 1} — this reader and CanvasRenderer's encoding have parted.");
            }
        }
        return marks;
    }

    /// <summary>Her arrowhead: the one three-point polygon in the frame stroked in the ship's own ink. Null
    /// when the map drew none. Asserts there is never more than one, so a second dart from somewhere else
    /// could not be mistaken for this one.</summary>
    private static float[]? FindHerTriangle(float[] buffer)
    {
        float[][] triangles = [.. Marks(buffer)
            .Where(m => m.Op == OpPolygon && m.Points.Length == 6 && m.Stroke == HerInk)
            .Select(m => m.Points)];
        Assert.True(triangles.Length <= 1, $"{triangles.Length} arrowheads in her ink in one frame.");
        return triangles.Length == 1 ? triangles[0] : null;
    }

    /// <summary>…and her ring: a stroked-only circle at the documented radius, in her ink at the documented
    /// alpha. The ship's own dot is a FILLED circle of radius 4 in full ink, so the two never collide.</summary>
    private static bool HasHerRing(float[] buffer) =>
        Marks(buffer).Any(m => m.Op == OpCircle
                               && Math.Abs(m.Points[2] - VelocityArrow.RingRadiusPx) < 1e-4
                               && m.Stroke == (HerInk.R, HerInk.G, HerInk.B, 200f));

    /// <summary>Every mark Lab 43's plume laid, written out — the ink is <c>ArcHaloColor</c>, which nothing
    /// else in the frame uses.</summary>
    private static List<string> DischargeMarks(float[] buffer) =>
    [
        .. Marks(buffer)
            .Where(m => m.Stroke.R == ArcHalo.R && m.Stroke.G == ArcHalo.G && m.Stroke.B == ArcHalo.B)
            .Select(m => $"{m.Op}:{string.Join(',', m.Points.Select(p => p.ToString("R")))}:{m.Stroke}")
    ];

    /// <summary>Lab 43's ink, read off the page itself rather than typed in a second time.</summary>
    private static readonly RgbaColor ArcHalo = (RgbaColor)typeof(Pages.Map)
        .GetField("ArcHaloColor", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
        .GetValue(null)!;

    private static void AssertPointsAlong(float[] buffer, double expectedScreenRad, string what)
    {
        float[]? tri = FindHerTriangle(buffer);
        Assert.True(tri is not null, $"no velocity arrowhead was drawn in {what}.");

        // The apex is the first point Core writes; the axis runs from the base's midpoint to it.
        double baseX = (tri![2] + tri[4]) / 2.0, baseY = (tri[3] + tri[5]) / 2.0;
        double drawn = Math.Atan2(tri[1] - baseY, tri[0] - baseX);
        Assert.True(Math.Abs(Wrap180((drawn - expectedScreenRad) * 180.0 / Math.PI)) < 0.01,
            $"in {what} the arrowhead points at {drawn:F5} rad on screen, and her velocity in that frame " +
            $"points at {expectedScreenRad:F5} rad. The picture and the frame have come apart.");
    }

    private static float[] SidesOfHerTriangleAt(double metersPerPixel)
    {
        Pages.Map map = Boot();
        InEarthOrbit(map);
        var camera = (Camera)Read(map, "_camera")!;
        // Keep her ON the glass at both zooms. Four orders of magnitude out, an un-centred ship lands at a
        // pixel coordinate in the millions, where a float's own spacing is a hundredth of a pixel — and this
        // row would then be measuring the anchor's rounding rather than the arrowhead's size.
        camera.CenterOn(((ShipState)Read(map, "_ship")!).Position);
        camera.MetersPerPixel = metersPerPixel;
        float[] tri = FindHerTriangle(Paint(map))
            ?? throw new InvalidOperationException($"no arrowhead at {metersPerPixel} m/px.");
        return
        [
            Side(tri, 0, 1), Side(tri, 1, 2), Side(tri, 2, 0),
        ];
    }

    private static double ApexDegreesAt(double metersPerPixel)
    {
        float[] tri = SidesOfHerTriangleAt(metersPerPixel);
        // The two sides off the apex are tri[0] (apex→corner) and tri[2] (corner→apex); the base is tri[1].
        double a = tri[0], b = tri[2], c = tri[1];
        return Math.Acos(((a * a) + (b * b) - (c * c)) / (2 * a * b)) * 180.0 / Math.PI;
    }

    private static float Side(float[] tri, int p, int q) =>
        (float)Math.Sqrt(Math.Pow(tri[p * 2] - tri[q * 2], 2) + Math.Pow(tri[(p * 2) + 1] - tri[(q * 2) + 1], 2));

    // ── DRIVING A REAL MAP ─────────────────────────────────────────────────────────────────────────────

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol = new(() =>
        ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    /// <summary>A live component over the shipping scenario — the shipping ephemeris, the shipping simulator,
    /// the ship laid down by the page's own <c>InitializeShipState</c>, and the REAL command buffer.</summary>
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
        Set(map, "_renderer", new CanvasRenderer("velocity-arrow-canvas"));
        Invoke(map, "ReprojectTrajectory");
        return map;
    }

    /// <summary>Put her in a real orbit about Earth: Earth's own heliocentric velocity plus three kilometres
    /// a second ACROSS it. Read in the Sun's frame that is nearly Earth's own course; read in Earth's it is a
    /// quarter turn off — which is exactly the disagreement guard (a) needs to be able to see.</summary>
    private static void InEarthOrbit(Pages.Map map)
    {
        var ship = (ShipState)Read(map, "_ship")!;
        Vector2d earthV = EarthVelocity(map);
        Vector2d across = new Vector2d(-earthV.Y, earthV.X).Normalized() * 3000.0;
        Set(map, "_ship", ship with { Velocity = earthV + across });
    }

    /// <summary>Give her exactly this much speed across Earth's course, and nothing else — for standing a
    /// hair either side of the ring threshold.</summary>
    private static void Nudge(Pages.Map map, double mps)
    {
        var ship = (ShipState)Read(map, "_ship")!;
        Vector2d earthV = EarthVelocity(map);
        Set(map, "_ship", ship with { Velocity = earthV + (new Vector2d(-earthV.Y, earthV.X).Normalized() * mps) });
    }

    private static Vector2d EarthVelocity(Pages.Map map)
    {
        var ephemeris = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        double t = (double)Read(map, "SimTime")!;
        return (ephemeris.Position("earth", t + 1) - ephemeris.Position("earth", t - 1)) / 2.0;
    }

    /// <summary>Turn the contactor on and charge her past the arcing threshold, so Lab 43's plume is actually
    /// in the frame guard (e) inspects.</summary>
    private static void MakeHerArc(Pages.Map map)
    {
        double threshold = (double)typeof(Pages.Map)
            .GetField("ArcChargeThreshold", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
            .GetValue(null)!;
        var ship = (ShipState)Read(map, "_ship")!;
        Set(map, "_ship", ship with { Charge = threshold * 2 });
        Set(map, "_plasma", PlasmaEnvironment.FromScenario(
            ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol-eu.json")),
            (ICelestialEphemeris)Read(map, "_ephemeris")!));
        Assert.True(Read(map, "_plasma") is not null,
            "sol-eu.json handed the page no PlasmaEnvironment — the plume would never be drawn.");
    }

    /// <summary>Her velocity in the frame the plan is being read in, read off the page's own one function so
    /// the bench cannot hold a second opinion about it.</summary>
    private static Vector2d VelocityIn(Pages.Map map) =>
        (Vector2d)Invoke(map, "FrameRelativeVelocity", (double)Read(map, "SimTime")!)!;

    /// <summary>The screen angle of a world vector: canvas Y points down while world Y points up.</summary>
    private static double ScreenAngleOf(Vector2d world) => Math.Atan2(-world.Y, world.X);

    /// <summary>Paint one map frame and hand back the command buffer the flush was about to send. The flush
    /// itself is the one line that crosses into JavaScript; the buffer is complete by then.</summary>
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

    private static double Wrap180(double deg)
    {
        deg %= 360;
        if (deg < -180) deg += 360;
        if (deg > 180) deg -= 360;
        return deg;
    }

    // ── PLUMBING ───────────────────────────────────────────────────────────────────────────────────────

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
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static object? Read(Pages.Map map, string member)
    {
        Type t = typeof(Pages.Map);
        if (t.GetField(member, Hidden) is { } field) return field.GetValue(map);
        if (t.GetProperty(member, Hidden) is { } prop) return prop.GetValue(map);
        throw new InvalidOperationException($"the component has no `{member}` — this guard reads a dead name.");
    }

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"the component has no `{field}`.")).SetValue(o, value);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard reads a dead name.");
        try
        {
            return call!.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

#pragma warning disable BL0006 // the framework's own seam: a component needs a renderer to have a dispatcher
    private sealed class ARendererThatDrawsNothing : Renderer
    {
        public ARendererThatDrawsNothing() : base(NoServices.Instance, NullLoggerFactory.Instance) { }

        public override Dispatcher Dispatcher { get; } = new RightHere();

        public void Attach(IComponent component) => AssignRootComponentId(component);

        protected override void HandleException(Exception exception) =>
            throw new InvalidOperationException("the frame threw inside the renderer", exception);

        protected override Task UpdateDisplayAsync(in Microsoft.AspNetCore.Components.RenderTree.RenderBatch batch) =>
            Task.CompletedTask;

        private sealed class RightHere : Dispatcher
        {
            public override bool CheckAccess() => true;
            public override Task InvokeAsync(Action workItem) { workItem(); return Task.CompletedTask; }
            public override Task InvokeAsync(Func<Task> workItem) => workItem();
            public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem) =>
                Task.FromResult(workItem());
            public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem) => workItem();
        }

        private sealed class NoServices : IServiceProvider
        {
            public static readonly NoServices Instance = new();
            public object? GetService(Type serviceType) => null;
        }
    }
#pragma warning restore BL0006
}
