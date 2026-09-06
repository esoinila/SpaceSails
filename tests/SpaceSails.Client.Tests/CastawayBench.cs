using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · THE CASTAWAY BENCH — the world three files build to ask what happens when a hull is lost and her
/// master is left standing somewhere.
///
/// <para><b>Why it exists.</b> <see cref="TheClaimIsWalkedEndToEndTests"/> (the claim, at a machine and at
/// a table), <c>SheGoesAtTheBerthTests</c> (she goes while she is tied up) and
/// <c>SheGoesWhetherHeIsAboardOrNotTests</c> (she goes with him anywhere else) are three questions about
/// ONE scene, and each of them had grown its own copy of the same fifteen steps: the same boot over
/// <c>sol.json</c>, the same walk past the tube, the same shuttle launched at the same seed-42 hull, the
/// same 0.1 s frame through the page's own <c>OnTick</c>, the same four reflection helpers and the same two
/// stand-in renderers — byte for byte, in three places. That is the shape this repository has paid for four
/// times: <b>one law transcribed at its call sites</b>. Three copies of "how the castaway's world is built"
/// is three chances for two of them to answer different games while both stay green.</para>
///
/// <para><b>What is here and what is deliberately NOT.</b> Only the steps that were IDENTICAL in the files
/// that had them. Everything a file does its own way stays in that file, where a reader can see it:
/// <c>ClampAtThePort</c> (the berth guards need the slot and the berth count back and this one does not),
/// <c>ArmHerCharges</c> (the two dark files assert the word and the second key on the way through),
/// <c>RunUntilSheGoes</c> (one hands back whether the vault was asked, one does not), the hunter each file
/// puts on her (they sail from different origins) and each file's own <c>Boot()</c> wrapper. A harness that
/// swallowed those differences would be a harness that quietly changed what a guard asks.</para>
///
/// <para><b>How it is used.</b> <c>using static SpaceSails.Client.Tests.CastawayBench;</c> at the top of the
/// file, and every call site reads exactly as it did — no member here changed its name or its signature on
/// the way over, except <see cref="Boot"/>, which takes the canvas id its three callers used to spell into
/// their own copy.</para>
///
/// <para><b>It is a bench, not a fake.</b> Every step goes through a shipping door: <c>ClampOntoHaven</c>,
/// <c>RefreshAshore</c>, <c>LaunchShuttleRun</c>, <c>OnTick</c>. The two renderers are the off-browser
/// horizon <see cref="DeskBench"/> documents from the other side — a component needs a render handle and a
/// dispatcher to exist at all, and the map frame's one line that crosses into JavaScript is caught by name
/// in <see cref="Frame"/> and nowhere else.</para>
/// </summary>
internal static class CastawayBench
{
    /// <summary>Everything on the page, public or not, instance or static — these guards drive a live
    /// component through its own private verbs, which is the only way to ask a page what it does.</summary>
    internal const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The port everything ashore happens at. It is a WORKING BERTH that the deal gave a machine
    /// to — both facts asserted rather than assumed in <c>TheKiosksSquare</c>, because a port with no
    /// kiosk would make every press guard green about a console that is not there.</summary>
    internal const string Port = "selene-gate";

    /// <summary>The shipping scenario, loaded once for the whole assembly: every world below is the game's
    /// own sky, not a sky a test invented.</summary>
    internal static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol = new(() =>
        ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A live component over the shipping scenario, walking her own deck — the posture the charge panel is
    /// reached from, because it is a console on her deck plan and nowhere else.
    ///
    /// <para>The map frame paints into the REAL command buffer (nothing else can be assigned to a field
    /// typed to the sealed <see cref="CanvasRenderer"/>); the walked view and the boat get a pen that stays
    /// in managed code. That is not decoration: <c>ShuttleFlightView.Draw</c> is inside the frame's OWN
    /// try/catch, and a renderer that throws there is read as a shuttle fault and RECOVERS THE BOAT — a
    /// world in which no run can be flown for longer than one frame, and every question about a shuttle
    /// answers itself.</para>
    /// </summary>
    /// <param name="canvasId">The canvas the caller's own file names its frames after.</param>
    internal static Pages.Map Boot(string canvasId)
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
        Set(map, "_renderer", new CanvasRenderer(canvasId));
        var pen = new APenThatDrawsNothing();
        Set(map, "_deckView", new DeckView(pen));
        Set(map, "_shuttleView", new ShuttleFlightView(pen));
        Set(map, "_deckMode", true);
        Set(map, "Warp", 1);
        Invoke(map, "ReprojectTrajectory");
        return map;
    }

    /// <summary>Past the tube and into the station room — the one signal the berth scene reads, through the
    /// page's own <c>RefreshAshore</c> rather than by writing the flag, so a world where the walk cannot be
    /// made goes red here instead of quietly proving nothing.</summary>
    internal static void WalkHimAshore(Pages.Map map)
    {
        Set(map, "_avatarY", 40.0);
        Invoke(map, "RefreshAshore");
        Assert.True((bool)Read(map, "_ashore")!, "the captain never got past the tube.");
    }

    /// <summary>
    /// The captain away in the boat, launched off the page's own launcher at a live, selected, authorized
    /// target.
    ///
    /// <para>The window the run flies inside is held open the way the game holds it open: a selected,
    /// observed hull at point-blank range that the captain has said the word over. Without the word this is
    /// an OPPORTUNITY, the window shuts on the first frame and the boat is recovered — which is a world in
    /// which nothing about a shuttle can be asked.</para>
    /// </summary>
    internal static void PutHimInTheShuttle(Pages.Map map)
    {
        Set(map, "_deckMode", false);

        var eph = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        NpcShip hull = TrafficSchedule.Generate(eph, seed: 42, count: 1)[0];
        Type stateType = typeof(Pages.Map).GetNestedType("NpcState", Hidden | BindingFlags.Public)!;
        object prey = Activator.CreateInstance(stateType, nonPublic: true)!;
        stateType.GetField("Ship", Hidden)!.SetValue(prey, hull);
        stateType.GetField("State", Hidden)!.SetValue(prey, (ShipState)Read(map, "_ship")!);
        stateType.GetField("Active", Hidden)!.SetValue(prey, true);
        stateType.GetField("CurrentlyObserved", Hidden)!.SetValue(prey, true);

        Array roster = Array.CreateInstance(stateType, 1);
        roster.SetValue(prey, 0);
        Set(map, "_npcStates", roster);
        Set(map, "_selectedTargetId", hull.Id);
        Set(map, "_plunderAuthorizedTargetId", hull.Id);

        Invoke(map, "LaunchShuttleRun", prey);
        Assert.NotNull(Read(map, "_shuttleRun"));
    }

    // ── THE FRAME ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The frame clock every one of these guards runs on: a tenth of a second, handed in.</summary>
    internal const double FrameSeconds = 0.1;

    /// <summary>One frame, through the page's own <c>OnTick</c>, on a real frame clock.</summary>
    internal static void Frame(Pages.Map map)
    {
        double at = Convert.ToDouble(Read(map, "_lastTimestampMs") ?? 0.0) + (FrameSeconds * 1000);
        try
        {
            Invoke(map, "OnTick", at);
        }
        catch (PlatformNotSupportedException)
        {
            // The canvas flush — the one line of the frame that crosses into JavaScript, and the same seam
            // EveryFrameLeavesTheSameFingerprintTests stops at. Everything these lanes read has already run.
        }
    }

    /// <summary>That many seconds of frames.</summary>
    internal static void RunFrames(Pages.Map map, double seconds)
    {
        for (int i = 0; i < (int)(seconds / FrameSeconds); i++)
        {
            Frame(map);
        }
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The page's own markup, for the guards that read the castaway card as text.</summary>
    internal static string TheCastawayMarkup() =>
        MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));

    /// <summary>The repo root, found by walking up from the test assembly — the same way every other guard
    /// in this project finds it, and the reason none of them depend on the working directory.</summary>
    internal static string RepoRoot()
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

    internal static object? Get(object owner, string name) =>
        owner.GetType().GetProperty(name, Hidden)?.GetValue(owner)
        ?? owner.GetType().GetField(name, Hidden)?.GetValue(owner);

    internal static object? Read(Pages.Map map, string name) =>
        typeof(Pages.Map).GetField(name, Hidden)?.GetValue(map)
        ?? typeof(Pages.Map).GetProperty(name, Hidden)?.GetValue(map);

    internal static void Set(Pages.Map map, string name, object? value)
    {
        FieldInfo? field = typeof(Pages.Map).GetField(name, Hidden);
        if (field is not null)
        {
            field.SetValue(map, value);
            return;
        }
        typeof(Pages.Map).GetProperty(name, Hidden)!.SetValue(map, value);
    }

    internal static object? Invoke(Pages.Map map, string name, params object?[] args)
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

    // ── THE TWO STAND-INS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>A renderer that records nothing and crosses into no JavaScript.</summary>
    internal sealed class APenThatDrawsNothing : IRenderer
    {
        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) { }

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f) { }

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawText(float x, float y, string text, RgbaColor color,
            string font = "12px sans-serif", TextAlign align = TextAlign.Left) { }

        public int RegisterImage(string url) => 0;

        public void DrawImage(int imageId, float x, float y, float w, float h, float alpha = 1f) { }

        public void DrawImageSlice(int imageId, float sx, float sy, float sw, float sh,
            float dx, float dy, float dw, float dh, float alpha = 1f) { }

        public void EndFrame() { }
    }

#pragma warning disable BL0006 // the framework's own seam: a component needs a renderer to have a dispatcher
    /// <summary>Enough of a renderer to give the component a render handle and a dispatcher. It never
    /// paints: <c>_hasPendingQueuedRender</c> means <c>StateHasChanged</c> never queues a batch, so
    /// <c>UpdateDisplayAsync</c> is never reached.</summary>
    internal sealed class ARendererThatDrawsNothing : Microsoft.AspNetCore.Components.RenderTree.Renderer
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
