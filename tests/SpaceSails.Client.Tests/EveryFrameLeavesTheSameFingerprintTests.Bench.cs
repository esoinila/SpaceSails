using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 · <b>THE BENCH THIS GUARD STANDS ON</b> — the six worlds, the five input sequences, the drive
/// that turns one (world, sequence) into a reading, and the boot that builds each world.
///
/// <para>Split out of the law next door under #251's method: the assertions in
/// <c>EveryFrameLeavesTheSameFingerprintTests.cs</c> are about what is PINNED, and this file is about what
/// is DRIVEN. Nothing here asserts a pin; everything here is the world a pin was taken in. Not one member
/// changed its name, its signature or its visibility on the way over.</para>
///
/// <para>The two enums are public because the <c>[Theory]</c> next door names their members in
/// <c>[InlineData]</c>, and the ledger's scene names are <c>&lt;world&gt;.&lt;sequence&gt;</c> off their
/// <c>ToString</c> — so renaming a member here re-keys thirty pinned rows, which is exactly as loud as it
/// should be.</para>
/// </summary>
public sealed partial class EveryFrameLeavesTheSameFingerprintTests
{
    // ── THE WORLDS ────────────────────────────────────────────────────────────────────────────────────

    public enum World
    {
        /// <summary>Walking her own deck while she flies: deck mode, no excursion, warp up so the near-body
        /// caps and the adaptive quanta are both in play.</summary>
        HerOwnDeckInFlight,

        /// <summary>Set down on Luna's regolith in a suit — the surface deck, the air, the tracker, the tide.</summary>
        TheRegolithOnFoot,

        /// <summary>A pressurised floor of the Hive, which is the one world that carves a patrol.</summary>
        AHiveFloorWithAPatrol,

        /// <summary>The same floor with the captain sat on a bench — the seat law rides every frame.</summary>
        ACaptainInAChair,

        /// <summary>The map itself: the flight branch, painted into the real command buffer, up to the flush
        /// that cannot cross into JavaScript on a test runner (see the class note).</summary>
        TheMapFrameInFlight,

        /// <summary>Her own deck again, but under <c>scenarios/sol-eu.json</c> with the contactor running —
        /// the one scenario in the game with a <see cref="PlasmaEnvironment"/> in it, and therefore the only
        /// world where the frame's charge lane is not a single early return.</summary>
        TheElectricUniverse,
    }

    public enum Sequence
    {
        /// <summary>A hundred and twenty frames at a steady sixty.</summary>
        SteadyFrames,

        /// <summary>Twenty steady, then the owner's own sixteen-second stall, then forty more.</summary>
        OneLongGap,

        /// <summary>A movement key held down from the sixth frame on.</summary>
        AHeldKey,

        /// <summary>A route clicked on the sixth frame and then walked out.</summary>
        APlannedRoute,

        /// <summary>
        /// The warp slider MOVED under the frame — down to 1× on the fortieth frame, back to 1000× on the
        /// eightieth.
        ///
        /// <para>Written because the other four could not tell a lie either. In all six worlds above the warp
        /// is set once at boot and never touched again, so <c>_effectiveWarp</c> is a CONSTANT for the whole
        /// run — and a constant cannot show a one-frame lag. Moving the phase that PICKS the warp to the far
        /// side of the phase that SPENDS it left all twenty-four fingerprints identical: the accumulator was
        /// buying its seconds at last frame's rate, and last frame's rate was this frame's rate. The one frame
        /// where that is not true is the very first — and on the very first frame <c>dtRealSeconds</c> is
        /// exactly zero, so nothing is bought at any rate at all.</para>
        ///
        /// <para>With a hand on the slider the boundary is real: on the fortieth frame the accumulator buys
        /// either <c>dt × 1</c> or <c>dt × 1000</c> depending purely on which side of it the write landed, and
        /// the sim clock says which happened for the eighty frames after. That is the accumulator boundary
        /// this file exists to hold still.</para>
        /// </summary>
        AHandOnTheWarpSlider,
    }

    // ── DRIVING ONE ROW ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Every row of the matrix driven and read, as ledger rows — the measurement the re-pin command
    /// writes down, and the same one the guards above compare against what is written down.</summary>
    internal static IReadOnlyList<PinLedger.Row> MeasureEveryRow()
    {
        var rows = new List<PinLedger.Row>();
        foreach ((string Field, string Type) field in SweptRoster())
        {
            rows.Add(new PinLedger.Row(RosterProbe, field.Field, field.Type));
        }
        foreach (World world in Enum.GetValues<World>())
        {
            foreach (Sequence sequence in Enum.GetValues<Sequence>())
            {
                string scene = SceneName(world, sequence);
                foreach ((string probe, string value) in DriveAndFingerprint(world, sequence).Rows)
                {
                    rows.Add(new PinLedger.Row(probe, scene, value));
                }
            }
        }
        return rows;
    }

    /// <summary>The fields the sweep walks, in the order it walks them — the same filter
    /// <see cref="Fingerprint"/> applies, asked as a question rather than done twice.</summary>
    private static IReadOnlyList<(string Field, string Type)> SweptRoster() =>
    [
        .. typeof(Pages.Map).GetFields(Hidden)
            .Where(f => !f.IsStatic
                        && !NotFingerprinted.Contains(f.FieldType.Name)
                        && !AWallClockAndNothingElse.Contains(f.Name))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => (AsWritten(f.Name), PinLedger.TypeLabel(f.FieldType)))
    ];

    /// <summary>#1055 · What used to cost a dump on the base, a dump on the lane and a line-by-line diff: when
    /// a <c>sweep</c> row moves, say WHICH FIELD did it, right there in the red.</summary>
    private static string WhichFieldMoved(string probe, Reading got)
    {
        if (probe != SweepProbe)
        {
            return "";
        }

        IReadOnlyDictionary<string, PinLedger.Row> pinned = PinLedger.Pinned(Suite);
        string[] appeared =
        [
            .. got.Roster.Where(f => !pinned.ContainsKey(PinLedger.Key(RosterProbe, f.Field)))
                         .Select(f => f.Field)
        ];
        var present = new HashSet<string>(got.Roster.Select(f => f.Field), StringComparer.Ordinal);
        string[] gone =
        [
            .. pinned.Values.Where(r => r.Probe == RosterProbe && !present.Contains(r.Scene))
                            .Select(r => r.Scene)
        ];

        if (appeared.Length == 0 && gone.Length == 0)
        {
            return "  the swept ROSTER is unchanged, so no field joined or left the page — a field's VALUE "
                + "moved. Dump both sides and diff them:\n"
                + "    SPACESAILS_SWEEP_DUMP=<dir> dotnet test tests/SpaceSails.Client.Tests -c Release "
                + "--filter FullyQualifiedName~EveryFrameLeavesTheSameFingerprint\n"
                + "  …run once on the base and once on this lane, then diff the two <dir>s.\n";
        }
        return (appeared.Length > 0 ? $"  sweep +{appeared.Length}: {string.Join(", ", appeared)}\n" : "")
            + (gone.Length > 0 ? $"  sweep −{gone.Length}: {string.Join(", ", gone)}\n" : "");
    }

    private static Reading DriveAndFingerprint(World world, Sequence sequence)
    {
        var pen = new RecordingPen();
        Pages.Map map = Boot(world, pen);

        double frameMs = 1000.0 / 60.0;
        string? stoppedAt = null;

        void Frame(double atMs)
        {
            try
            {
                Invoke(map, "OnTick", atMs);
            }
            catch (PlatformNotSupportedException)
            {
                // The one line of the flight path that crosses into JavaScript. See the class note.
                stoppedAt = "the canvas flush (CanvasRenderer.EndFrame → JS)";
            }
        }

        double t = 0;
        int frames = sequence == Sequence.APlannedRoute ? 240 : 120;

        for (int i = 0; i < frames; i++)
        {
            if (i == 5 && sequence == Sequence.AHeldKey)
            {
                ((HashSet<string>)Get(map, "_deckKeys")!).Add("d");
            }
            if (i == 5 && sequence == Sequence.APlannedRoute)
            {
                ClickSomewhereWorthWalkingTo(map);
            }
            if (i == 20 && sequence == Sequence.OneLongGap)
            {
                t += 16_000;   // the owner's own gap, off #825
            }
            if (sequence == Sequence.AHandOnTheWarpSlider && (i == 40 || i == 80))
            {
                // The one thing no other row does: change the rate the accumulator is about to buy at, on
                // a frame that is going to buy. See the sequence's own note — without this, the phase that
                // picks the warp and the phase that spends it can be swapped and nothing anywhere moves.
                Set(map, "Warp", i == 40 ? 1 : 1000);
            }

            Frame(t);
            t += frameMs;
        }

        return Fingerprint(world, sequence, map, pen, stoppedAt);
    }

    /// <summary>Point at the nearest fixture this deck INVITES you to walk to, through the projection the
    /// renderer is drawing with right now — never arithmetic written down a second time.</summary>
    private static void ClickSomewhereWorthWalkingTo(Pages.Map map)
    {
        var plan = (DeckPlan)Get(map, "_deckPlan")!;
        double ax = (double)Get(map, "_avatarX")!, ay = (double)Get(map, "_avatarY")!;
        DeckPlan.ConsoleSpot[] spots = [.. plan.Consoles
            .Where(c => c.DistanceFrom(ax, ay) > DeckPlan.InteractRadius * 2)
            .OrderByDescending(c => c.DistanceFrom(ax, ay))];
        if (spots.Length == 0)
        {
            return;
        }
        DeckPlan.ConsoleSpot target = spots[^1];

        DeckView.Placement glass = DeckView.PlacementFor(
            plan, (int)Get(map, "_viewportWidth")!, (int)Get(map, "_viewportHeight")!,
            ax, ay, (double)Get(map, "_deckPanX")!, (double)Get(map, "_deckPanY")!);
        Invoke(map, "ClickToWalkAt", glass.Ox + (target.X * glass.Scale), glass.Oy - (target.Y * glass.Scale));
    }

    // ── BOOTING A WORLD ───────────────────────────────────────────────────────────────────────────────

    private const string Body = "luna";

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to walk about on.");

    /// <summary>The shipping scenario, off the canonical copy at the repo root — the same JSON the client
    /// fetches out of <c>wwwroot/scenarios</c> (the csproj mirrors this file into it).</summary>
    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol = new(() => Scenario("sol"));

    /// <summary>…and the Electric Universe cut of it, which is the only scenario in the game that hands the
    /// page a <see cref="PlasmaEnvironment"/> — so it is the only world where the charge lane of the frame is
    /// anything but an early return.</summary>
    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> SolEu = new(() => Scenario("sol-eu"));

    private static SpaceSails.Contracts.ScenarioDefinition Scenario(string name) =>
        ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", $"{name}.json"));

    /// <summary>The traffic the shipping boot generates, off the shipping seeds. Cached because the planners
    /// cost seconds and every world wants the same sky.</summary>
    private static readonly Lazy<IReadOnlyList<NpcShip>> Traffic = new(() =>
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(Sol.Value);
        return
        [
            .. TrafficSchedule.GeneratePods(eph, seed: 43, count: 3),
            .. TrafficSchedule.Generate(eph, seed: 42, count: 8),
        ];
    });

    /// <summary>The sky, wrapped in the component's own private <c>NpcState</c> — one fresh wrapper per world,
    /// because the frame mutates them and two worlds sharing a ship would be two worlds sharing a bug.</summary>
    private static Array TheTrafficAsTheComponentKeepsIt()
    {
        Type stateType = typeof(Pages.Map).GetNestedType("NpcState", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        FieldInfo ship = stateType.GetField("Ship", Hidden)!;
        Array states = Array.CreateInstance(stateType, Traffic.Value.Count);
        for (int i = 0; i < Traffic.Value.Count; i++)
        {
            object one = Activator.CreateInstance(stateType, nonPublic: true)!;
            ship.SetValue(one, Traffic.Value[i]);
            states.SetValue(one, i);
        }
        return states;
    }

    /// <summary>
    /// A live component over a real world.
    ///
    /// <para>The one piece of theatre is the render handle: a <see cref="ComponentBase"/> that was never
    /// attached to a renderer throws out of <c>StateHasChanged</c>, so the component is told it already has a
    /// render queued — the framework's own early-out, which makes the call a silent no-op. That is the same
    /// bench <c>MustStandUpBeforeWalkingTests</c> and <c>TheStallSaysSoTests</c> drive.</para>
    ///
    /// <para>Everything else is the shipping object: the shipping scenario, <c>CircularOrbitEphemeris</c>,
    /// the shipping <see cref="Simulator"/> at the shipping time step, the ship laid down by the page's own
    /// <c>InitializeShipState</c>, and the traffic from the shipping seeds.</para>
    /// </summary>
    private static Pages.Map Boot(World world, RecordingPen pen)
    {
        var map = new Pages.Map();

        // The frame ends in InvokeAsync(StateHasChanged) — twice on a walked deck, once on the map — and a
        // component that was never attached to a renderer has no dispatcher to invoke ON. So it is attached
        // to a renderer that draws nothing, over a dispatcher that runs the work item RIGHT HERE. That is
        // what the browser does too: the rAF callback is already on Blazor's synchronization context, so
        // InvokeAsync there runs inline as well — the frame is not secretly being reordered by a bench.
        new ARendererThatDrawsNothing().Attach(map);

        // …and StateHasChanged itself is the framework's own early-out: told a render is already queued, it
        // returns without walking a render tree. The same one piece of theatre MustStandUpBeforeWalkingTests
        // and TheStallSaysSoTests ride on.
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on " +
                "has moved, and the frame's verbs will throw instead of running.");
        pending.SetValue(map, true);

        SpaceSails.Contracts.ScenarioDefinition scenario =
            world == World.TheElectricUniverse ? SolEu.Value : Sol.Value;
        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(scenario);
        PlasmaEnvironment? plasma = PlasmaEnvironment.FromScenario(scenario, ephemeris);
        Set(map, "_scenarioName", scenario.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_plasma", plasma);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0, plasma));
        Set(map, "_npcSimulator", new Simulator(ephemeris, TrafficSchedule.NpcTimeStep));
        Set(map, "_ship", Invoke(map, "InitializeShipState")!);
        Set(map, "_npcStates", TheTrafficAsTheComponentKeepsIt());

        // The pen. The map frame paints into the REAL command buffer (nothing else can be assigned to a
        // field typed to the sealed CanvasRenderer); every walked view paints into the recording one.
        Set(map, "_renderer", new CanvasRenderer("fingerprint-canvas"));
        Set(map, "_deckView", new DeckView(pen));

        Invoke(map, "ReprojectTrajectory");

        switch (world)
        {
            case World.HerOwnDeckInFlight:
                Set(map, "_deckMode", true);
                Set(map, "Warp", 1000);      // so the near-body caps and the adaptive quanta are both live
                break;

            case World.TheRegolithOnFoot:
                StandOnLuna(map, floor: 0);
                break;

            case World.AHiveFloorWithAPatrol:
                StandOnLuna(map, TheFloor);
                StepOffTheBench(map);
                break;

            case World.ACaptainInAChair:
                StandOnLuna(map, TheFloor);
                SitOnTheBench(map);
                break;

            case World.TheMapFrameInFlight:
                Set(map, "_deckMode", false);
                Set(map, "Warp", 100);
                break;

            case World.TheElectricUniverse:
                Set(map, "_deckMode", true);
                Set(map, "Warp", 1000);
                // The contactor RUNNING is the whole point of this world. Every other world here is a
                // Newtonian scenario, where AdvanceChargeSystems is one early return — so without this the
                // charge lane of the frame could be moved anywhere, or off the end of a branch, and twenty
                // fingerprints would go on being identical. A guard that cannot see a phase move is not
                // guarding that phase.
                Set(map, "_contactorOn", true);
                Assert.True(Get(map, "_plasma") is not null,
                    "scenarios/sol-eu.json handed the page no PlasmaEnvironment — this world's charge lane " +
                    "is the same early return as everybody else's, and it would prove nothing.");
                break;
        }

        return map;
    }

    /// <summary>Put the captain down on Luna — on the regolith (<paramref name="floor"/> 0) or on a
    /// pressurised floor of the Hive (negative), built by the page's own <c>RebuildSurfaceDeck</c>.</summary>
    private static void StandOnLuna(Pages.Map map, int floor)
    {
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, floor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Invoke(map, "RebuildSurfaceDeck");

        // The round, laid the one way the game ever lays it — off the lift ride, never off a deck
        // rebuild. Without this a "floor with a patrol" is a floor with nobody on it, and every line the
        // frame's patrol phase writes would be the same whatever that phase did: a guard handed a world
        // it built itself cannot tell pass from fail, which is this repo's fifth named bug class.
        if (floor < 0)
        {
            Invoke(map, "SpawnPatrolFor", ex);
            Assert.True(((ICollection)Get(map, "_guards")!).Count > 0,
                $"{Body} B{-floor} rostered nobody this watch — this world has no patrol in it.");
        }
    }

    /// <summary>Sit on the bench the generator carved, the way the game sits on it.</summary>
    private static void SitOnTheBench(Pages.Map map)
    {
        var plan = (DeckPlan)Get(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot bench = plan.Consoles
            .FirstOrDefault(c => c.Kind == DeckPlan.ConsoleKind.HiveBench);
        Assert.True(bench.Kind == DeckPlan.ConsoleKind.HiveBench,
            $"{Body} B{-TheFloor} carves no bench — this world has nobody to sit down.");
        Set(map, "_avatarX", (double)bench.X);
        Set(map, "_avatarY", (double)bench.Y);
        Assert.True((bool)Invoke(map, "TryTakeBench")!, "the press at the bench was not taken.");
    }

    /// <summary>…and then get up off it, so the walking worlds start from the floor's own step-off square
    /// rather than from inside the plank.</summary>
    private static void StepOffTheBench(Pages.Map map)
    {
        SitOnTheBench(map);
        Invoke(map, "StandUpBeforeWalking");
        Assert.True(Read(map, "SeatedTable") is null, "the captain is still sitting down.");
    }
}
