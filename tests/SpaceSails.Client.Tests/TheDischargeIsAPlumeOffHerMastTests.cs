using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #528 §7 · THE PLASMA BALL, AT THE PEN. Owner, on the charge board (#523): <i>"the charge is being equalized
/// could have plasma ball like beautifull effect if physics supports it."</i>
///
/// <para><see cref="Core.Tests"/>' own <c>ThePlumeLeavesHerMastTests</c> holds the GEOMETRY —
/// <see cref="DischargePlume"/>, the pure function. These rows hold the WIRING: that the map actually calls it,
/// with the charge she actually dumped, off the masthead and not off the hull dot, and on the clock the law
/// names. A pure type nobody drew from would pass every guard in Core and show the player nothing.</para>
///
/// <h3>These guards read the PEN, not the source</h3>
/// <para>A real <see cref="Pages.Map"/> is booted over the shipping <c>scenarios/sol.json</c>, its own
/// <c>PaintTheMapFrame</c> is run into the real <see cref="CanvasRenderer"/>'s command buffer, and the plume is
/// read back OUT of that buffer as floats. Nothing below asserts that a line of code exists; everything below
/// asserts what got drawn. (The flush at the end of <c>PaintTheMapFrame</c> is the one line that crosses into
/// JavaScript and throws off-browser — the buffer is complete by then, the same seam
/// <see cref="TheShipSaysWhichWaySheIsGoingTests"/> rides.)</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDischargeIsAPlumeOffHerMastTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    // ── LAW ONE · A QUIET HULL GETS NO PLUME, AN ARCING ONE GETS ONE ──────────────────────────────────

    /// <summary>
    /// The band is readable FROM THE MAP: an arcing hull crawls, and a hull below the band — however charged —
    /// draws nothing at all. Both halves, so neither direction of the mistake survives.
    ///
    /// <para>The quiet control is not a cold ship: she is charged to just under
    /// <see cref="HullCharge.ArcThreshold"/>, which is GLOWING on the board and the loudest state that must
    /// still show nothing leaving her. A guard that used a flat hull could not tell "not arcing" from "no
    /// charge layer at all".</para>
    ///
    /// <para>RED PROOF: gate <c>DrawDischarge</c> on <c>_ship.Charge &gt; 0</c> instead of the band and the
    /// glowing half fails with filaments in the frame; return early always and the arcing half fails with
    /// none.</para>
    /// </summary>
    [Fact]
    public void ArcingSheCrawlsAndMerelyGlowingSheDoesNot()
    {
        Pages.Map arcing = Boot();
        InThePlasma(arcing, charge: HullCharge.ArcThreshold * 1.05);
        Assert.Equal(HullCharge.Band.Arcing, BandOf(arcing));
        Assert.NotEmpty(Filaments(Paint(arcing)));

        Pages.Map glowing = Boot();
        InThePlasma(glowing, charge: HullCharge.ArcThreshold - 0.01);
        Assert.Equal(HullCharge.Band.Glowing, BandOf(glowing));
        Assert.Empty(PlumeMarks(Paint(glowing)));
    }

    /// <summary>…and a Newtonian scenario — no charge layer at all — never draws one, whatever her charge field
    /// happens to hold. The plume is a claim about plasma.</summary>
    [Fact]
    public void WithNoPlasmaLayerThereIsNoPlume()
    {
        Pages.Map map = Boot();
        Set(map, "_ship", ((ShipState)Read(map, "_ship")!) with { Charge = 1.0 });
        Assert.Null(Read(map, "_plasma"));
        Assert.Empty(PlumeMarks(Paint(map)));
    }

    // ── LAW TWO · EVERY FILAMENT LEAVES THE MASTHEAD ──────────────────────────────────────────────────

    /// <summary>
    /// THE PLUME LAW, on the glass. Every drawn filament starts at the masthead —
    /// <see cref="DischargePlume.MastPx"/> off the ship marker — and not one of them starts at the ship dot.
    /// A discharge leaves the sharpest extremity; a glow centred on the hull is the one shape the physics rules
    /// out, and it is also the easiest one to draw by accident.
    ///
    /// <para>The ship's own screen pixel is not assumed: it is read off the frame's ship dot (the filled 4 px
    /// circle in her ink), so this row measures the plume against the marker the player sees.</para>
    ///
    /// <para>RED PROOF: start the bolts at <c>sx, sy</c> instead of the masthead and every filament fails,
    /// naming its distance from the dot as 0.0 px against a mast at 11.</para>
    /// </summary>
    [Fact]
    public void EveryDrawnFilamentStartsAtTheMastheadAndNoneAtTheShipDot()
    {
        Pages.Map map = Boot();
        InThePlasma(map, charge: 1.0);
        float[] frame = Paint(map);

        (float sx, float sy) = HerDot(frame);
        List<float[]> filaments = Filaments(frame);
        Assert.True(filaments.Count >= DischargePlume.ArcingFilaments,
            $"only {filaments.Count} filaments in an arcing frame — this row is not looking at the plume.");

        foreach (float[] bolt in filaments)
        {
            double fromDot = Math.Sqrt(Math.Pow(bolt[0] - sx, 2) + Math.Pow(bolt[1] - sy, 2));
            Assert.True(Math.Abs(fromDot - DischargePlume.MastPx) < 1e-2,
                $"a filament leaves a point {fromDot:F3} px from the ship dot, and her mast stands at " +
                $"{DischargePlume.MastPx} px. It is not starting at the masthead.");
        }

        // Every one of them leaves the SAME point — one whip, one plume, not a spray from several places.
        Assert.Single(filaments.Select(b => ($"{b[0]:F3}", $"{b[1]:F3}")).Distinct());

        // …and the bright core sits on that point too, because that is where the field is.
        Assert.Contains(CircleMarks(frame), c =>
            Math.Abs(c.X - filaments[0][0]) < 1e-2 && Math.Abs(c.Y - filaments[0][1]) < 1e-2);
    }

    // ── LAW THREE · THE CRAWL IS ON SIM TIME, NOT THE WALL CLOCK ──────────────────────────────────────

    /// <summary>
    /// The arcing crawl is deterministic from SIM time. Run the wall clock forward by a full second with the
    /// sim frozen and the plume is the same plume, mark for mark; advance SIM time by one crawl step and it is
    /// a different one. That is what makes a PAUSED map draw the same frame twice — the map is drawn every
    /// frame, paused included — and what keeps the fingerprint ledgers still.
    ///
    /// <para>ANTI-VACUOUS, and this half is the point: a plume that never changed at all would pass the
    /// stability check on its own, so the row also demands that sim time MOVES it.</para>
    ///
    /// <para>RED PROOF: seed the crawl from <c>_lastTimestampMs</c> — which is what the code did before this
    /// lane — and the frozen-sim half fails on the first pair, because the wall clock moved and the picture
    /// moved with it.</para>
    /// </summary>
    [Fact]
    public void TheCrawlIsSeededFromSimTimeAndNotTheWallClock()
    {
        Pages.Map map = Boot();
        InThePlasma(map, charge: 1.0);
        Set(map, "SimTime", 4321.0);
        Set(map, "_lastTimestampMs", 1_000.0);
        List<string> first = PlumeMarks(Paint(map));
        Assert.NotEmpty(first);

        // The wall clock runs on a whole second; the world does not.
        Set(map, "_lastTimestampMs", 2_000.0);
        Assert.Equal(first, PlumeMarks(Paint(map)));

        // …and the world does: one crawl step of SIM time is a different picture.
        Set(map, "SimTime", 4321.0 + DischargePlume.CrawlStepSeconds);
        Assert.NotEqual(first, PlumeMarks(Paint(map)));
    }

    // ── LAW FOUR · THE FLASH IS AS BRIGHT AS THE DUMP WAS ─────────────────────────────────────────────

    /// <summary>
    /// The wiring #528 §7 actually asked for: <i>brightness scaled by the charge actually dumped.</i> Two
    /// captains press the same key the same instant — one on a nearly full hull, one on a nearly cold one — and
    /// the frame a moment later shows the first one's discharge as the brighter and the longer.
    ///
    /// <para>The dump is not simulated by hand: <c>VentCharge()</c> is invoked, so this row also proves the
    /// page records what left her. Before this lane it recorded only WHEN, and every dump in the game — a full
    /// hull's and a whisper's — lit exactly the same picture.</para>
    ///
    /// <para>ANTI-VACUOUS: both frames are asserted to be flashing (five filaments, the dump's count) before
    /// they are compared, so a row where neither drew a flash cannot pass.</para>
    ///
    /// <para>RED PROOF: drop <c>_lastDischargeShed = shed;</c> from <c>VentCharge</c> and the two frames come
    /// back identical in alpha and length — which is exactly the bug this law names.</para>
    /// </summary>
    [Fact]
    public void ABigDumpFlashesBrighterAndFartherThanASmallOne()
    {
        (float alphaBig, double reachBig) = FlashAfterVenting(from: 1.0);
        (float alphaSmall, double reachSmall) = FlashAfterVenting(from: 0.08);

        Assert.True(alphaBig > alphaSmall,
            $"a full hull's dump draws at alpha {alphaBig} and a nearly cold one's at {alphaSmall} — the flash " +
            "is not scaled by the charge that actually left her.");
        Assert.True(reachBig > reachSmall,
            $"a full hull's filaments reach {reachBig:F3} px and a nearly cold one's {reachSmall:F3} px.");
    }

    /// <summary>Vent a hull at this charge and read the flash off the very next frame: its ink's alpha and the
    /// farthest its filaments reach off the masthead.</summary>
    private static (float Alpha, double Reach) FlashAfterVenting(double from)
    {
        Pages.Map map = Boot();
        InThePlasma(map, charge: from);
        Set(map, "_lastTimestampMs", 10_000.0);
        Invoke(map, "VentCharge");

        // The band she is left in after the dump — below ARCING for both of these, so what is on screen is
        // the flash and nothing else.
        Assert.NotEqual(HullCharge.Band.Arcing, BandOf(map));

        float[] frame = Paint(map);
        List<float[]> filaments = Filaments(frame);
        Assert.Equal(DischargePlume.FlashFilaments, filaments.Count);

        float alpha = Marks(frame)
            .Where(m => m.Op == OpPolyline && m.Points.Length == DischargePlume.FloatsPerFilament && IsPlumeInk(m))
            .Select(m => m.Stroke.A)
            .Distinct()
            .Single();

        double reach = filaments.Max(b =>
            Math.Sqrt(Math.Pow(b[4] - b[0], 2) + Math.Pow(b[5] - b[1], 2)));
        return (alpha, reach);
    }

    // ── LAW FIVE · IT NEVER GROWS INTO THE HUD'S ROOM ─────────────────────────────────────────────────

    /// <summary>
    /// The plume is a GLYPH: a fixed handful of pixels round the ship marker at every zoom, so it can never
    /// reach across the map into a HUD panel's clearance. Two zooms four orders of magnitude apart draw it at
    /// the same size, and the whole of it stays inside a small box about her dot.
    ///
    /// <para>RED PROOF: size the mast in world metres (<c>MastPx * _camera.MetersPerPixel</c>, the way the
    /// barrel line is built) and the two zooms come back ten-thousand-fold apart.</para>
    /// </summary>
    [Fact]
    public void ThePlumeIsAGlyphAndStaysInItsOwnCorner()
    {
        double[] wide = PlumeExtentAt(1e9);
        double[] close = PlumeExtentAt(1e5);

        Assert.Equal(wide.Length, close.Length);
        for (int i = 0; i < wide.Length; i++)
        {
            Assert.True(Math.Abs(wide[i] - close[i]) < 1e-3,
                $"filament {i} reaches {wide[i]:F4} px at 1e9 m/px and {close[i]:F4} px at 1e5 m/px — the plume " +
                "is being sized in world metres, not in pixels.");
        }

        // The whole picture, mast and all, inside a box a HUD panel's clearance never has to think about.
        double budget = DischargePlume.MastPx + (DischargePlume.MastPx * 1.6);
        Assert.All(wide, reach => Assert.True(reach <= budget,
            $"a filament reaches {reach:F3} px from the ship marker, past the {budget:F3} px the plume is " +
            "allowed — it is growing into the HUD's room."));
    }

    /// <summary>How far each filament's tip stands from the ship dot, at this zoom.</summary>
    private static double[] PlumeExtentAt(double metersPerPixel)
    {
        Pages.Map map = Boot();
        InThePlasma(map, charge: 1.0);
        var camera = (Camera)Read(map, "_camera")!;
        // Keep her ON the glass at both zooms: four orders of magnitude out, an un-centred ship lands at a
        // pixel coordinate in the millions, where a float's own spacing is a hundredth of a pixel — and this
        // row would then be measuring the anchor's rounding rather than the plume's size.
        camera.CenterOn(((ShipState)Read(map, "_ship")!).Position);
        camera.MetersPerPixel = metersPerPixel;

        float[] frame = Paint(map);
        (float sx, float sy) = HerDot(frame);
        return [.. Filaments(frame)
            .Select(b => Math.Sqrt(Math.Pow(b[4] - sx, 2) + Math.Pow(b[5] - sy, 2)))
            .OrderBy(d => d)];
    }

    // ── READING THE PEN ───────────────────────────────────────────────────────────────────────────────

    private readonly record struct Mark(float Op, (float R, float G, float B, float A) Stroke, float[] Points);

    private const float OpPolyline = 1f, OpCircle = 2f, OpPolygon = 3f, OpImage = 4f, OpImageSlice = 5f;

    /// <summary>Walk the float buffer the flush was about to hand to JavaScript, in order. The encoding is
    /// <see cref="CanvasRenderer"/>'s own (docs/m2-spec.md): six floats of header, then opcode-tagged
    /// records.</summary>
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

    /// <summary>Lab 43's ink, read off the page itself rather than typed in a second time. Nothing else in the
    /// frame draws in it, which is what tells the plume apart from every other mark.</summary>
    private static readonly RgbaColor PlumeInk = (RgbaColor)typeof(Pages.Map)
        .GetField("ArcHaloColor", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
        .GetValue(null)!;

    private static bool IsPlumeInk(in Mark m) =>
        m.Stroke.R == PlumeInk.R && m.Stroke.G == PlumeInk.G && m.Stroke.B == PlumeInk.B;

    /// <summary>Every mark the plume laid, written out — for comparing one frame against another.</summary>
    private static List<string> PlumeMarks(float[] buffer) =>
    [
        .. Marks(buffer)
            .Where(m => IsPlumeInk(m))
            .Select(m => $"{m.Op}:{string.Join(',', m.Points.Select(p => p.ToString("R")))}:{m.Stroke}")
    ];

    /// <summary>Just the filaments: three-point polylines in the plume's ink. The mast itself is a two-point
    /// polyline and the core is a circle, so neither is mistaken for a bolt.</summary>
    private static List<float[]> Filaments(float[] buffer) =>
    [
        .. Marks(buffer)
            .Where(m => m.Op == OpPolyline && m.Points.Length == DischargePlume.FloatsPerFilament && IsPlumeInk(m))
            .Select(m => m.Points)
    ];

    private static List<(float X, float Y, float R)> CircleMarks(float[] buffer) =>
    [
        .. Marks(buffer)
            .Where(m => m.Op == OpCircle && IsPlumeInk(m))
            .Select(m => (m.Points[0], m.Points[1], m.Points[2]))
    ];

    /// <summary>The ship marker's own screen pixel, read off the frame: the 4 px circle in her full ink that
    /// <c>DrawShip</c> lays after the plume.</summary>
    private static (float X, float Y) HerDot(float[] buffer)
    {
        var ship = (RgbaColor)typeof(Pages.Map)
            .GetField("ShipColor", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
            .GetValue(null)!;
        (float X, float Y, float R)[] dots = [.. Marks(buffer)
            .Where(m => m.Op == OpCircle && Math.Abs(m.Points[2] - 4f) < 1e-4
                        && m.Stroke == (ship.R, ship.G, ship.B, ship.A))
            .Select(m => (m.Points[0], m.Points[1], m.Points[2]))];
        Assert.Single(dots);
        return (dots[0].X, dots[0].Y);
    }

    // ── DRIVING A REAL MAP ────────────────────────────────────────────────────────────────────────────

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
        Set(map, "_renderer", new CanvasRenderer("plume-canvas"));
        Invoke(map, "ReprojectTrajectory");
        return map;
    }

    /// <summary>Put her in the Electric Universe scenario's plasma at a given hull charge — the only world
    /// where a discharge is a thing that can happen at all.</summary>
    private static void InThePlasma(Pages.Map map, double charge)
    {
        Set(map, "_ship", ((ShipState)Read(map, "_ship")!) with { Charge = charge });
        Set(map, "_plasma", PlasmaEnvironment.FromScenario(
            ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol-eu.json")),
            (ICelestialEphemeris)Read(map, "_ephemeris")!));
        Assert.True(Read(map, "_plasma") is not null,
            "sol-eu.json handed the page no PlasmaEnvironment — the plume would never be drawn at all.");
    }

    /// <summary>The band the CHARGE BOARD would print for her right now — asked of Core, so the bench cannot
    /// hold a second opinion about where the arcing edge is.</summary>
    private static HullCharge.Band BandOf(Pages.Map map) =>
        HullCharge.BandOf(((ShipState)Read(map, "_ship")!).Charge);

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
