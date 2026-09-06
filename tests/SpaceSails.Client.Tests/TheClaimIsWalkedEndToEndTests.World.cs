using System;
using System.Collections;
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
/// #251 · <b>THE BENCH THE WHOLE SCENE IS DRIVEN ON</b> — the world, the counter, the frame and the
/// plumbing, in one place, so the three test parts next door read as assertions and nothing else.
///
/// <para>Nothing in this file asserts anything about the claim. It boots a live <see cref="Pages.Map"/>
/// over the shipping scenario, puts the captain where a posture says, walks him to the kiosk the port
/// actually built, presses the counter's own rows, and runs frames through the page's own <c>OnTick</c>.
/// Every step goes through a shipping door: that is the file's one rule, and it is why the sibling issue
/// could get three different answers by reading the source instead.</para>
/// </summary>
public sealed partial class TheClaimIsWalkedEndToEndTests
{
    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where the captain is standing, which is the whole of what the presence law asks.</summary>
    public enum Posture
    {
        /// <summary>On her own deck, in the dark, with nothing between him and the controls.</summary>
        Aboard,

        /// <summary>Walking a moon.</summary>
        OnASurface,

        /// <summary>Away in the boat.</summary>
        AwayInTheBoat,

        /// <summary>Past the tube, on somebody's concourse, with her clamped to their collar.</summary>
        AshorePastTheTube,
    }

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol = new(() =>
        ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    /// <summary>A live component over the shipping scenario, walking her own deck — the same boot the berth
    /// scuttle's own guards use, so the two files are asking one game.</summary>
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
        Set(map, "_renderer", new CanvasRenderer("claims-canvas"));
        var pen = new APenThatDrawsNothing();
        Set(map, "_deckView", new DeckView(pen));
        Set(map, "_shuttleView", new ShuttleFlightView(pen));
        Set(map, "_deckMode", true);
        Set(map, "Warp", 1);
        Invoke(map, "ReprojectTrajectory");
        return map;
    }

    /// <summary>Put the captain where the posture says, through the same doors the game uses to get him
    /// there — a real excursion, a real shuttle launch, a real walk past the tube.</summary>
    private static void PutHimIn(Pages.Map map, Posture posture)
    {
        switch (posture)
        {
            case Posture.Aboard:
                Assert.True((bool)Invoke(map, "TheMasterIsAboardHer")!);
                return;

            case Posture.OnASurface:
                PutHimOnAGround(map);
                break;

            case Posture.AwayInTheBoat:
                PutHimInTheShuttle(map);
                break;

            default:
                ClampAtThePort(map);
                WalkHimAshore(map);
                break;
        }

        Assert.False((bool)Invoke(map, "TheMasterIsAboardHer")!,
                     $"{posture} did not actually take the captain off his ship.");
    }

    /// <summary>Tie her up at <see cref="Port"/> through the clamp the game uses, and assert the port has an
    /// interior to walk — a port with no concourse has no machine on it and no gangway to be past.</summary>
    private static void ClampAtThePort(Pages.Map map)
    {
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody dock = sky.Bodies.First(b => b.Id == Port);
        Assert.True(HavenInterior.HasInterior(Port), $"{Port} has no interior, so it has no concourse.");

        double simTime = (double)Read(map, "SimTime")!;
        Invoke(map, "ClampOntoHaven", dock, sky.Position(Port, simTime), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
    }

    /// <summary>Past the tube, through the page's own <c>RefreshAshore</c> rather than by writing the flag,
    /// so a world where the walk cannot be made goes red instead of quietly proving nothing.</summary>
    private static void WalkHimAshore(Pages.Map map)
    {
        Set(map, "_avatarY", 40.0);
        Invoke(map, "RefreshAshore");
        Assert.True((bool)Read(map, "_ashore")!, "the captain never got past the tube.");
    }

    /// <summary>The ground the excursion arms of this file put the captain down on. Luna, because it is a
    /// real moon of a real planet in the shipping scenario and therefore has a harbour that serves it —
    /// which is what the waiting writ is filed against.</summary>
    private const string Ground = "luna";

    /// <summary>On a ground, built the way the frame guards build one: the page's own excursion record and
    /// its own <c>RebuildSurfaceDeck</c>, so the deck under his feet is a deck the game made.</summary>
    private static void PutHimOnAGround(Pages.Map map)
    {
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Ground, Ground, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Invoke(map, "RebuildSurfaceDeck");
        Assert.NotNull(Read(map, "_surface"));
    }

    /// <summary>Which ground the excursion put him on.</summary>
    private static string TheGroundHeIsOn(Pages.Map map)
    {
        object surface = Read(map, "_surface") ?? throw new InvalidOperationException("he is not on a ground.");
        return (string)Get(Get(Get(surface, "Stop")!, "Body")!, "Id")!;
    }

    /// <summary>The captain away in the boat, launched off the page's own launcher at a live, selected,
    /// authorized target — the berth scuttle's guards' own recipe.</summary>
    private static void PutHimInTheShuttle(Pages.Map map)
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

    /// <summary>A collector sitting exactly where she is, already fitted out. On any frame she is allowed to
    /// close she has closed — which is what makes the held arms of the presence law mean something.</summary>
    private static void PutACollectorOnTopOfHer(Pages.Map map, string callsign)
    {
        var ship = (ShipState)Read(map, "_ship")!;
        ((IList)Read(map, "_hunters")!).Add(new HunterState(
            Id: callsign.ToLowerInvariant(),
            Callsign: callsign,
            OriginBodyId: Port,
            SpawnedAtSimTime: (double)Read(map, "SimTime")!,
            ActivationSimTime: 0,
            State: ship,
            CaughtPlayer: false,
            BrokenOff: false));
    }

    /// <summary>The captain's word, the crew's second key, both keys together — the panel's own three verbs,
    /// in the order the panel makes the player press them. Nothing writes the clock.</summary>
    private static void ArmHerCharges(Pages.Map map)
    {
        Invoke(map, "OpenShipScuttlePanel");
        Invoke(map, "GiveTheWordAgainstHer");
        Invoke(map, "AskTheCrewForTheSecondKey");
        Invoke(map, "TurnBothKeys");
        Assert.Equal(Scuttle.OverloadSeconds, (double)Read(map, "_shipChargesSeconds")!);
        Invoke(map, "CloseShipScuttlePanel");
    }

    // ── THE COUNTER ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Stand at the machine: the console the port built, found on the deck plan and walked to.</summary>
    private static void WalkToTheKiosk(Pages.Map map)
    {
        var plan = (DeckPlan)Read(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot kiosk = plan.Consoles.First(c => c.Label == NebulaClaims.KioskPlate);

        Set(map, "_avatarX", (double)kiosk.X);
        Set(map, "_avatarY", (double)kiosk.Y);
        Set(map, "_viewObject", null);
    }

    private static IReadOnlyList<NebulaClaims.Ask> TheRows(Pages.Map map) =>
        (IReadOnlyList<NebulaClaims.Ask>)Invoke(map, "TheClaimAsks")!;

    private static void Press(Pages.Map map, NebulaClaims.Ask ask, PirateInsurance withPolicy = default) =>
        Invoke(map, "PressTheClaim", ask);

    private static int PressesTaken(Pages.Map map) =>
        Read(map, "_claimDesk") is { } desk ? (int)Get(desk, "Presses")! : 0;

    /// <summary>Three presses, all correct, through the counter's own rows — the whole lodging, for the
    /// guards that are about what happens AFTER one.</summary>
    private static void LodgeAWholeClaim(Pages.Map map)
    {
        Invoke(map, "ViewNearbyObject");
        Assert.True((bool)Read(map, "TheClaimDeskIsUp")!);

        for (int press = 0; press < NebulaClaims.Presses; press++)
        {
            IReadOnlyList<NebulaClaims.Ask> rows = TheRows(map);
            Assert.NotEmpty(rows);
            NebulaClaims.Ask take = press == 1
                ? rows.Single(r => r.Offer == (string)Invoke(map, "ShipNameNow")!)
                : rows[0];
            Press(map, take);
            Assert.Equal(press + 1, PressesTaken(map));
        }
    }

    /// <summary>The pending-writ row on the captain's ledger, or null when there is none.</summary>
    private static object? TheWritRow(Pages.Map map) => Invoke(map, "PendingWritTip");

    // ── THE FRAME ─────────────────────────────────────────────────────────────────────────────────────

    private static void RunUntilSheGoes(Pages.Map map)
    {
        for (int i = 0; i < 4000; i++)
        {
            Frame(map);
            if (Read(map, "_shipChargesSeconds") is null)
            {
                return;
            }
        }

        throw new InvalidOperationException("her ninety-second overload never reached zero in four hundred seconds.");
    }

    private static void RunFrames(Pages.Map map, double seconds)
    {
        for (int i = 0; i < (int)(seconds / FrameSeconds); i++)
        {
            Frame(map);
        }
    }

    private const double FrameSeconds = 0.1;

    private static void Frame(Pages.Map map)
    {
        double at = Convert.ToDouble(Read(map, "_lastTimestampMs") ?? 0.0) + (FrameSeconds * 1000);
        try
        {
            Invoke(map, "OnTick", at);
        }
        catch (PlatformNotSupportedException)
        {
            // The canvas flush — the one line of the frame that crosses into JavaScript.
        }
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

    private static object? Get(object owner, string name) =>
        owner.GetType().GetProperty(name, Hidden)?.GetValue(owner)
        ?? owner.GetType().GetField(name, Hidden)?.GetValue(owner);

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

    /// <summary>A renderer that records nothing and crosses into no JavaScript.</summary>
    private sealed class APenThatDrawsNothing : IRenderer
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
