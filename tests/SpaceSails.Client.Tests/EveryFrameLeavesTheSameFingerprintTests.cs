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
/// #870 lane 7c · THE FRAME LEAVES THE SAME MARK IT ALWAYS DID.
///
/// <para><c>OnTick</c> was 505 lines in one method — the accumulator, the fixed-step integration, the split
/// advance onto a burn epoch, the surface-impact watch, the sweep, the reprojection cadence, the pulse expiry,
/// the shuttle run, the charge systems, the story cards, the walked frame and the HUD throttle, in one
/// straight pass. Cutting it into named phases is a refactor with nothing to hide behind: there is no unit
/// under it to test, and every one of this repo's named bug classes lives exactly here — <b>a list built by
/// appending is not a list in order</b>, and <b>the sim doing one thing while a sentence or a drawn shape
/// reports another</b>. A guard that merely re-asserted a handful of facts would pass on a build where two
/// phases had swapped, which is precisely the mistake a 505-line method invites.</para>
///
/// <h3>THE LAW: A FINGERPRINT, CAPTURED ON THE OLD CODE FIRST</h3>
///
/// <para>So the guard is a SNAPSHOT. A real <see cref="Pages.Map"/> is booted on six worlds, each is driven
/// through <c>OnTick</c> with five FIXED sequences of <c>highResTimestampMs</c> values and inputs, and
/// afterwards everything the frame wrote is serialised into one deterministic text and hashed. The hashes below were taken on the
/// commit BEFORE the split and committed on their own ("the snapshot, on the old code") so that they could
/// never be quietly re-baselined afterwards: git says which commit each number came from.</para>
///
/// <para>The text has three parts, and each one closes a different way of getting this wrong:</para>
/// <list type="number">
///   <item><b>THE LEDGER</b> — thirty-eight named readings (avatar, sim clock, accumulator, warp, the pulse
///   slot and the words in it, the nerve, the tracker, the guards' positions, the FrameGap clock, the camera,
///   the passes, the trail). Committed as readable text under <c>Fingerprints/</c>, so a red run names the
///   line that moved instead of printing two hashes that differ.</item>
///   <item><b>THE SWEEP</b> — a generic walk over EVERY instance field of the component (minus the machinery
///   listed in <see cref="NotFingerprinted"/>), hashed to one line. The ledger says WHERE; the sweep says
///   NOTHING ESCAPED. A phase that writes a field nobody thought to name is still caught.</item>
///   <item><b>THE PEN</b> — every draw call the frame issued, in order, hashed. <c>DrawWalkFrame</c> paints
///   through a recording <see cref="IRenderer"/>; the map frame paints into the real
///   <see cref="CanvasRenderer"/>'s command buffer, which is read back. This is the half a state fingerprint
///   cannot see: the third named bug class is the picture disagreeing with the sim.</item>
/// </list>
///
/// <h3>WHAT IS EXCLUDED, AND WHY</h3>
///
/// <para><b>The tail of the map frame.</b> <c>CanvasRenderer.EndFrame</c> is the one line of <c>OnTick</c>'s
/// flight path that crosses into JavaScript, and <c>[JSImport]</c> throws
/// <c>PlatformNotSupportedException: System.Runtime.InteropServices.JavaScript is not supported on this
/// platform</c> on a test runner. <c>_renderer</c> is typed to the sealed <see cref="CanvasRenderer"/>, so
/// there is no seam to substitute. <see cref="World.TheMapFrameInFlight"/> therefore drives the flight branch
/// up to that flush and pins what it wrote — including the whole command buffer, which is complete by then —
/// and asserts that the flush is exactly where it stopped. The ~40 lines AFTER the flush (the scope inset, the
/// parrot, the ship's alert strip, the long-coast advert, the arrival-brake gate, the firing-solution reveal
/// and the HUD throttle) are unreachable off-browser and are NOT fingerprinted. They are named here so that
/// nobody reads the green and believes more than it says.</para>
///
/// <para><b>One wall clock, named.</b> <c>_frameServicedAtMs</c> is <c>Environment.TickCount64</c> and has to
/// be — see <see cref="AWallClockAndNothingElse"/> for #825's own reason and for what is pinned in its place.
/// It is the ONLY exclusion of its kind: there is no <c>Stopwatch</c> and no unseeded <c>Random</c> anywhere
/// in the frame, every timestamp is handed in by the bench, and the traffic is generated from the same fixed
/// seeds (42/43) the shipping boot uses.</para>
///
/// <h3>THE SAME NUMBERS ON THE MACHINE THAT MATTERS</h3>
///
/// <para>Sets and dictionaries are rendered in SORTED order, because .NET randomises string hashing per
/// process and insertion order would otherwise make the hash a coin toss between two runs on one box. Numbers
/// are written to thirteen significant digits (see <see cref="Num(double)"/>), which is orders of magnitude
/// clear of the last-bit disagreement two C runtimes can have about <c>Math.Sin</c> and still far finer than
/// anything a reordered phase could hide in. That is not a hope: the texts below were captured on
/// Windows and then reproduced BYTE FOR BYTE by the same assembly under
/// <c>mcr.microsoft.com/dotnet/sdk:10.0</c> on Linux, which is what CI runs.</para>
///
/// <h3>PROVEN ABLE TO FAIL</h3>
///
/// <para>Moving one phase of the split frame past its neighbour reddens the rows. The verbatim run is in the
/// pull request. When a row DOES go red, <c>SPACESAILS_SWEEP_DUMP=&lt;dir&gt;</c> writes the whole swept text
/// out so the offending field can be diffed rather than guessed at from a hash.</para>
///
/// <h3>#561 · THE ONE RE-PIN, AND WHAT THE DIFF HAD TO SHOW BEFORE IT WAS ALLOWED</h3>
///
/// <para>The nerve gauge's backing plate is measured to what it backs now, and the motion tracker's column
/// top is ASKED of the nerve block rather than typed at 82 (<c>HudColumn</c>) — so on the five worlds that
/// draw a gauge one rectangle is taller and, on the regolith, the fan sits 18px further down the column.
/// Twenty-five of the thirty texts moved. In every one of them the ONLY line that changed was
/// <c>walked-view pen</c>; its CALL COUNT is identical on both sides of the diff (210720, 470880, 35917, …
/// all unchanged, because the same rectangle and the same disc were drawn at a different y); and the five
/// <see cref="World.TheMapFrameInFlight"/> texts, which draw no walked view at all, are untouched. A re-pin
/// that had moved a ledger row, a sweep row or a call count would have been a different lane's bug wearing
/// this lane's clothes.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class EveryFrameLeavesTheSameFingerprintTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    // ── THE THIRTY ROWS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Six worlds × five sequences. Each row is the fingerprint text committed beside it in
    /// <c>Fingerprints/&lt;world&gt;.&lt;sequence&gt;.txt</c>, taken on the PRE-SPLIT code — the first twenty on
    /// commit b19ef16, the plasma world's four on 04bb219, and the warp slider's six on the commit that put
    /// the unsplit method back in the tree to capture them.
    ///
    /// <para><b>#618 · ALL THIRTY WERE RE-RECORDED, AND IT IS THE STATE-SHAPE KIND OF CHANGE — the third kind
    /// <c>EveryRoundFingerprintsTheSameTests</c> names, and the same proof.</b> The sweep walks the page's
    /// fields and RENDERS each one whole, so a member added to a collaborator the page holds — here the
    /// round's three for #618 (<c>LookingIntoIt</c>, <c>TheNoise</c>, <c>ShotsAnswered</c>) — moves the
    /// sweep's hash on every row, in worlds that have no round on the floor at all and never fire anything.
    /// The field COUNT is unmoved at 657, because it counts <c>Map</c>'s own fields and #905's ratchet is
    /// untouched: no field was added to the page.</para>
    ///
    /// <para>It was proved a state change and not a behaviour change the only honest way, using this file's
    /// own <c>SPACESAILS_SWEEP_DUMP</c> hook: the whole sweep was dumped on the base (d1fbc0c) and on this
    /// lane and the sixty texts compared line by line. All thirty have the same line count either way, and on
    /// every one of them <b>exactly one line differs — <c>_patrol=</c> — and nothing in it was REMOVED or
    /// CHANGED</b>: the only difference is five tokens added, <c>LookingIntoIt=∅</c>, <c>TheNoise=(0, 0)</c>
    /// and <c>ShotsAnswered=0</c>, every one at its default, because no world here fires a gun. Every other
    /// field of the round, every guard, the escort, the wallet and the whole rest of the page are
    /// byte-identical, and so are the ledger, the pen and the canvas buffer on all thirty rows — the diff of
    /// the committed texts is 30 files, one line each, and that line is the sweep's hash.</para>
    ///
    /// <para><b>If any of these ever moves again, that is not this lane's kind of change either, and the same
    /// dump-and-diff is what settles it.</b></para>
    ///
    /// <para><b>#969 · ALL THIRTY WERE RE-RECORDED AGAIN, and this time the field COUNT itself moved — 663 →
    /// 664.</b> That is the honest signature of what #969 did: one field was added to the page,
    /// <c>_armedArrivalPassSimTime</c>, the pass epoch an arrival ARMED AT PLAN TIME was rehearsed for. It is
    /// the whole of the new state (no forked autopilot), and every world here has it at its default
    /// <c>null</c>, because none of them arms an arrival. The diff of the committed texts is again 30 files,
    /// one line each, and that line is the sweep's — the ledger, the pen, the canvas buffer, the call counts
    /// and every other row are byte-identical, which is exactly the claim: the frame's BEHAVIOUR is
    /// unchanged, only the shape of the state it sweeps.</para>
    ///
    /// <para><b>#973 L2 · AND ALL THIRTY AGAIN, with the field count 672 → 686.</b> Fourteen fields, all of
    /// them the Nebula rep's and all of them on the page: which visit's room he is remembering, the running
    /// visit count, whether the rota has him working it, the per-visit remember-you-said-no, the meeting
    /// count the bleed is clocked in, when he drifts on, which fixture he is heading for, whether he has
    /// said the one thing a rebuffed salesman says, his pitch card and the four things on it, and the
    /// <c>?rep=</c> cheat. <b>Every one of them is at its default in every world here</b>, because none of
    /// these six worlds is a hive canteen with him on the rota — the diff of the committed texts is again 30
    /// files, one line each, and that line is the sweep's own. The ledger, the pen, the canvas buffer and
    /// the call counts are byte-identical on all thirty rows, and <c>EveryFrameHashesTheSameTests</c>' draw
    /// transcripts did not move at all: he is a walker, so when he is not on a floor there is nothing extra
    /// to draw.</para>
    ///
    /// <para><b>#962 · AND ALL THIRTY ONCE MORE, field count 720 → 721.</b> The smallest version of the same
    /// signature: <b>one</b> field was added to the page, <c>_autopilotPlanBodyClearance</c> — how close the
    /// armed autopilot's rehearsed path came to each world it passed, cached at arm time beside the path and
    /// the collision pass it already cached (the #219 one-arm law), so the #180 park watchdog can ask whether
    /// the plan cleared the body the ship is BOUND to. The <c>SPACESAILS_SWEEP_DUMP</c> dump-and-diff was run
    /// on the base (35dd47a) and on this lane, and on <b>all thirty</b> rows the diff is the same single
    /// line, and it is an ADDITION, never a change or a removal:</para>
    /// <code>52a53
    /// &gt; _autopilotPlanBodyClearance=∅</code>
    /// <para>…at its default in every world, because not one of these six arms an autopilot approach. The
    /// ledger, the pen, the canvas buffer and the call counts are byte-identical on all thirty rows, and
    /// <c>EveryFrameHashesTheSameTests</c>' draw transcripts and <c>EveryRoundFingerprintsTheSameTests</c>
    /// did not move at all — the alarm this lane quiets shouts on nothing any of these worlds does.</para>
    /// </summary>
    [Theory]
    [InlineData(World.HerOwnDeckInFlight, Sequence.SteadyFrames)]
    [InlineData(World.HerOwnDeckInFlight, Sequence.OneLongGap)]
    [InlineData(World.HerOwnDeckInFlight, Sequence.AHeldKey)]
    [InlineData(World.HerOwnDeckInFlight, Sequence.APlannedRoute)]
    [InlineData(World.HerOwnDeckInFlight, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.TheRegolithOnFoot, Sequence.SteadyFrames)]
    [InlineData(World.TheRegolithOnFoot, Sequence.OneLongGap)]
    [InlineData(World.TheRegolithOnFoot, Sequence.AHeldKey)]
    [InlineData(World.TheRegolithOnFoot, Sequence.APlannedRoute)]
    [InlineData(World.TheRegolithOnFoot, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.SteadyFrames)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.OneLongGap)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.AHeldKey)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.APlannedRoute)]
    [InlineData(World.AHiveFloorWithAPatrol, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.ACaptainInAChair, Sequence.SteadyFrames)]
    [InlineData(World.ACaptainInAChair, Sequence.OneLongGap)]
    [InlineData(World.ACaptainInAChair, Sequence.AHeldKey)]
    [InlineData(World.ACaptainInAChair, Sequence.APlannedRoute)]
    [InlineData(World.ACaptainInAChair, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.TheMapFrameInFlight, Sequence.SteadyFrames)]
    [InlineData(World.TheMapFrameInFlight, Sequence.OneLongGap)]
    [InlineData(World.TheMapFrameInFlight, Sequence.AHeldKey)]
    [InlineData(World.TheMapFrameInFlight, Sequence.APlannedRoute)]
    [InlineData(World.TheMapFrameInFlight, Sequence.AHandOnTheWarpSlider)]
    [InlineData(World.TheElectricUniverse, Sequence.SteadyFrames)]
    [InlineData(World.TheElectricUniverse, Sequence.OneLongGap)]
    [InlineData(World.TheElectricUniverse, Sequence.AHeldKey)]
    [InlineData(World.TheElectricUniverse, Sequence.APlannedRoute)]
    [InlineData(World.TheElectricUniverse, Sequence.AHandOnTheWarpSlider)]
    public void EveryFrameItRunsFingerprintsTheSame(World world, Sequence sequence)
    {
        string produced = DriveAndFingerprint(world, sequence);
        string path = Path.Combine(FingerprintDirectory(), $"{world}.{sequence}.txt");

        if (Environment.GetEnvironmentVariable("SPACESAILS_FINGERPRINT_WRITE") == "1")
        {
            Directory.CreateDirectory(FingerprintDirectory());
            File.WriteAllText(path, produced);
            return;
        }

        Assert.True(File.Exists(path),
            $"no snapshot was ever taken for {world}/{sequence} — the file {path} is missing, so this row " +
            "is asserting nothing at all.");
        string expected = File.ReadAllText(path).Replace("\r\n", "\n");

        // The readable half first: name the line that moved, rather than printing two hashes that differ.
        string[] want = expected.Split('\n');
        string[] got = produced.Split('\n');
        for (int line = 0; line < Math.Min(want.Length, got.Length); line++)
        {
            Assert.True(want[line] == got[line],
                $"the frame no longer leaves the same mark on {world} / {sequence}.\n" +
                $"  line {line + 1} was: {want[line]}\n" +
                $"  line {line + 1} now: {got[line]}\n" +
                "Nothing in this lane may change what a frame writes. If a phase was reordered, put it back; " +
                "the order IS the frame.");
        }
        Assert.True(want.Length == got.Length,
            $"the fingerprint for {world} / {sequence} changed length: {want.Length} lines → {got.Length}.");

        // …and then the whole text as one number, which is the thing that was committed before the split.
        Assert.Equal(Sha256(expected), Sha256(produced));
    }

    /// <summary>The snapshot is worth nothing if the bench cannot tell one world from another — a guard handed
    /// a world it built itself cannot tell pass from fail (this repo's fifth named bug class). Thirty rows,
    /// thirty different fingerprints.</summary>
    [Fact]
    public void EveryRowIsADifferentFrame()
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(FingerprintDirectory(), "*.txt")
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            string hash = Sha256(File.ReadAllText(file).Replace("\r\n", "\n"));
            Assert.False(seen.TryGetValue(hash, out string? twin),
                $"{Path.GetFileName(file)} and {twin} produced the SAME fingerprint — two of these rows are " +
                "driving the same frame, so one of them could never fail.");
            seen[hash] = Path.GetFileName(file);
        }
        Assert.Equal(30, seen.Count);
    }

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

    private static string DriveAndFingerprint(World world, Sequence sequence)
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
        Type stateType = typeof(Pages.Map).GetNestedType("NpcState", Hidden | BindingFlags.Static)!;
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
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
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

    // ── THE FINGERPRINT ───────────────────────────────────────────────────────────────────────────────

    private static string Fingerprint(World world, Sequence sequence, Pages.Map map, RecordingPen pen,
        string? stoppedAt)
    {
        var sb = new StringBuilder();
        sb.Append("# ").Append(world).Append(" / ").Append(sequence).Append('\n');
        sb.Append("# the mark one frame after another leaves on the component — #870 lane 7c\n");
        sb.Append("stopped-at            = ").Append(stoppedAt ?? "ran to the end of the frame").Append('\n');

        // ── THE LEDGER: what the frame is for, read by name ───────────────────────────────────────────
        foreach ((string name, string member) in TheLedger)
        {
            string reading = Render(Read(map, member), 1, []);
            // A reading nobody could read is not a ledger line. Past a screenful it keeps its head —
            // enough to see WHAT it is — and folds the rest into a hash, which is all the length was
            // ever doing for us.
            if (reading.Length > 160)
            {
                reading = $"{reading[..120]}… {reading.Length} chars, sha256 {Sha256(reading)[..16]}";
            }
            sb.Append(name.PadRight(21)).Append(" = ").Append(reading).Append('\n');
        }

        // ── THE SWEEP: and nothing at all escaped ─────────────────────────────────────────────────────
        var swept = new StringBuilder();
        int fields = 0;
        foreach (FieldInfo f in typeof(Pages.Map).GetFields(Hidden).OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            if (f.IsStatic || NotFingerprinted.Contains(f.FieldType.Name)
                || AWallClockAndNothingElse.Contains(f.Name))
            {
                continue;
            }
            fields++;
            // #870 lane 6c · the walk starts with the COMPONENT already on its own path. A
            // collaborator the page hands ITSELF to (Map.Seating takes an ISeatHost) keeps a reference
            // back to the page, and that reference is not a reading: it is the very object this loop is
            // already sweeping. Seeding the path is what stops the walk re-entering the whole component
            // from inside one of its own fields -- and it is why these hashes are still the ones #905
            // captured on the old code rather than a re-baseline: with it, all thirty pinned texts are
            // byte-identical.
            swept.Append(AsWritten(f.Name)).Append('=').Append(Render(f.GetValue(map), 1, [map])).Append('\n');
        }
        sb.Append("sweep                 = ").Append(fields).Append(" fields, sha256 ")
          .Append(Sha256(swept.ToString())).Append('\n');
        if (Environment.GetEnvironmentVariable("SPACESAILS_SWEEP_DUMP") is { } dumpDir)
        {
            Directory.CreateDirectory(dumpDir);
            File.WriteAllText(Path.Combine(dumpDir, $"{world}.{sequence}.sweep.txt"), swept.ToString());
        }

        // ── THE PEN: and the picture agrees with it ───────────────────────────────────────────────────
        sb.Append("walked-view pen       = ").Append(pen.Commands).Append(" calls, sha256 ")
          .Append(pen.Sha256()).Append('\n');
        sb.Append("map-frame buffer      = ").Append(TheCanvasBuffer(map)).Append('\n');
        return sb.ToString();
    }

    /// <summary>What the frame is FOR, read by name so a red run points at a thing a person can picture.
    /// Every phase of <c>OnTick</c> writes at least one of these.</summary>
    private static readonly (string Name, string Member)[] TheLedger =
    [
        ("the real clock",       "_lastTimestampMs"),
        ("the frame's now",      "_frameNowMs"),
        ("the FrameGap clock",   "_frameGapSeconds"),
        ("the hold, said",       "_heldControlsSaid"),
        ("the accumulator",      "_simAccumulator"),
        ("the sim clock",        "SimTime"),
        ("the ship",             "_ship"),
        ("warp asked",           "Warp"),
        ("warp effective",       "_effectiveWarp"),
        ("nearest body",         "_nearestBody"),
        ("nearest body at",      "_nearestBodyPosition"),
        ("nearest body moving",  "_nearestBodyVelocity"),
        ("the pursuit trail",    "_pursuitTrail"),
        ("this frame's drag",    "_frameMaxDragDecel"),
        ("the next sweep",       "_nextSweepSimTime"),
        ("the next projection",  "_nextProjectionSimTime"),
        ("the trajectory",       "_samples"),
        ("the closest pass",     "_closestPass"),
        ("the armable pass",     "_armablePass"),
        ("the long haul",        "_longHaulReach"),
        ("the pulse",            "_pulse"),
        ("arcing",               "_wasArcing"),
        ("the camera",           "_camera"),
        ("the HUD's last paint", "_lastHudUpdateMs"),
        ("the avatar",           "_avatarX"),
        ("the avatar (y)",       "_avatarY"),
        ("the avatar's heading", "_avatarHeading"),
        ("the route",            "_autoWalk"),
        // #904 (lane 6b) moved the seat's state into Seating; the ledger reads it through the page's own
        // forwarder, which hands back the very same TableTalk — so the thirty pinned digests are unchanged.
        ("the seat",             "SeatedTable"),
        ("the nerve",            "_nerve"),
        ("the tracker",          "_chirp"),
        ("the guards",           "_guards"),
        ("the patrol clock",     "_patrolFloorSeconds"),
        ("the traffic",          "_npcStates"),
        ("the hunters",          "_hunters"),
        ("the ordnance",         "_ordnance"),
        ("the ghost",            "_beaconGhost"),
        ("the surface",          "_surface"),
        ("the pulse cooldown",   "_lastPulseSimTime"),
    ];

    /// <summary>The machinery the frame reads but is not itself: the world model, the integrators, the views
    /// and the injected browser services. Excluded BY TYPE rather than by name, so a renamed field cannot slip
    /// a whole subsystem out of the sweep.</summary>
    private static readonly HashSet<string> NotFingerprinted = new(StringComparer.Ordinal)
    {
        // The world and its integrators — constant for the whole run, and enormous.
        nameof(ICelestialEphemeris), "CircularOrbitEphemeris", nameof(Simulator),
        // The pen and the views — fingerprinted separately, as the pen.
        nameof(CanvasRenderer), nameof(DeckView), nameof(ShuttleFlightView), nameof(ScopeView),
        // Browser/framework plumbing that has no state of the frame's in it.
        "HttpClient", "NavigationManager", "IJSRuntime", "ElementReference",
        "CancellationTokenSource", "CancellationToken",
    };

    /// <summary>
    /// THE ONE FIELD THAT CANNOT BE FINGERPRINTED, and the reason, written down.
    ///
    /// <para><c>_frameServicedAtMs</c> is <c>Environment.TickCount64</c> — the wall clock, taken inside
    /// <c>MarkFrameServiced</c>, and #825's own doc says why it must be: in a starved tab the pointer event
    /// and the animation callback are two queued jobs, so a click needs to know how long ago the last frame
    /// was, not merely how long that frame took. There is nothing to seed and no clock to hand in; the bench
    /// would be fingerprinting the minute it happened to run.</para>
    ///
    /// <para>Its CONSEQUENCES are all still pinned: <c>_frameGapSeconds</c> and <c>_heldControlsSaid</c> are
    /// both in the ledger, and the banner and the threshold they feed are #825's own subject, driven end to
    /// end by <see cref="TheStallSaysSoTests"/> next door. What is excluded is one raw wall stamp.</para>
    /// </summary>
    private static readonly HashSet<string> AWallClockAndNothingElse = new(StringComparer.Ordinal)
    {
        "_frameServicedAtMs",
    };

    /// <summary>Read a field or a property or call a no-argument method — the ledger names the component's own
    /// vocabulary, and some of it is a question rather than a field (the stall banner is one).</summary>
    private static object? Read(Pages.Map map, string member)
    {
        Type t = typeof(Pages.Map);
        if (PatrolState.TryFollow(map, member, out object? onTheRound)) return onTheRound;
        if (t.GetField(member, Hidden) is { } field) return field.GetValue(map);
        if (t.GetProperty(member, Hidden) is { } prop) return prop.GetValue(map);
        if (t.GetMethod(member, Hidden, Type.EmptyTypes) is { } call) return call.Invoke(map, null);
        throw new InvalidOperationException($"the component has no `{member}` — this ledger reads a dead name.");
    }

    /// <summary>What the flight frame actually drew, read straight out of the command buffer the flush was
    /// about to hand to JavaScript. Empty on a deck world, which never opens a map frame.</summary>
    private static string TheCanvasBuffer(Pages.Map map)
    {
        object? renderer = Get(map, "_renderer");
        if (renderer is null) return "∅";
        var buffer = (float[])renderer.GetType().GetField("_buffer", Hidden)!.GetValue(renderer)!;
        int length = (int)renderer.GetType().GetField("_length", Hidden)!.GetValue(renderer)!;
        var texts = (IEnumerable)renderer.GetType().GetField("_texts", Hidden)!.GetValue(renderer)!;

        var sb = new StringBuilder();
        for (int i = 0; i < length; i++) { sb.Append(Num(buffer[i])).Append(' '); }
        int labels = 0;
        foreach (object? label in texts) { labels++; sb.Append(Render(label, 1, [])).Append('\n'); }
        return $"{length} floats, {labels} labels, sha256 {Sha256(sb.ToString())}";
    }

    // ── RENDERING A VALUE THE SAME WAY TWICE ──────────────────────────────────────────────────────────

    private const int MaxDepth = 5;
    private const int MaxItems = 64;

    private static string Render(object? v, int depth, List<object> path)
    {
        if (v is null) return "∅";
        if (v is double d) return Num(d);
        if (v is float f) return Num(f);
        if (v is string s) return "\"" + s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
        if (v is bool b) return b ? "yes" : "no";

        Type t = v.GetType();
        if (t.IsEnum) return t.Name + "." + v;
        if (v is decimal dec) return dec.ToString(CultureInfo.InvariantCulture);
        if (t.IsPrimitive) return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "?";
        if (v is DateTime or DateTimeOffset or TimeSpan or Guid) return $"<a clock or an identity: {t.Name}>";
        if (v is Delegate) return $"<{t.Name}>";
        if (NotFingerprinted.Contains(t.Name)) return $"<not fingerprinted: {t.Name}>";
        if (depth >= MaxDepth) return $"<{t.Name} …>";
        foreach (object o in path) { if (ReferenceEquals(o, v)) return $"<already above: {t.Name}>"; }

        if (v is IDictionary pairs)
        {
            var rows = new List<string>();
            foreach (DictionaryEntry e in pairs)
            {
                rows.Add(Render(e.Key, depth + 1, path) + ": " + Render(e.Value, depth + 1, path));
            }
            rows.Sort(StringComparer.Ordinal);   // .NET randomises string hashing per process
            return "{" + Trim(rows, pairs.Count) + "}";
        }

        if (v is IEnumerable items)
        {
            var rows = new List<string>();
            int n = 0;
            foreach (object? item in items)
            {
                n++;
                if (rows.Count < MaxItems) rows.Add(Render(item, depth + 1, path));
            }
            if (IsUnordered(t)) rows.Sort(StringComparer.Ordinal);
            return "[" + Trim(rows, n) + "]";
        }

        FieldInfo[] fields = [.. t.GetFields(Hidden)
            .Where(x => !x.IsStatic)
            .OrderBy(x => x.Name, StringComparer.Ordinal)];
        path.Add(v);
        var fieldRows = new List<string>();
        foreach (FieldInfo x in fields)
        {
            object? held = x.GetValue(v);

            // #870 lane 6c · …and never a field that points BACK at something already on the path.
            // A back-reference is not a reading: it is an object this walk has passed through, and
            // following it -- or even printing a placeholder for it -- would make the fingerprint
            // depend on how a collaborator is wired to its host rather than on what the frame wrote.
            if (held is not null && path.Any(o => ReferenceEquals(o, held)))
            {
                continue;
            }
            fieldRows.Add(AsWritten(x.Name) + "=" + Render(held, depth + 1, path));
        }
        path.RemoveAt(path.Count - 1);
        return t.Name + "(" + string.Join(", ", fieldRows) + ")";
    }

    /// <summary>A property's backing field, written the way the property is.
    /// <c>&lt;Charge&gt;k__BackingField</c> is the compiler talking; <c>Charge</c> is what a reader of
    /// this ledger is looking for.</summary>
    private static string AsWritten(string field) =>
        field.StartsWith('<') && field.EndsWith(">k__BackingField", StringComparison.Ordinal)
            ? field[1..^16]
            : field;

    /// <summary>A set is a bag with no order, and .NET's per-process string hash randomisation means its
    /// enumeration order is not the same twice. Rendered sorted, or the hash would be a coin toss.</summary>
    private static bool IsUnordered(Type t) =>
        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>));

    private static string Trim(List<string> rows, int total) =>
        total <= MaxItems ? string.Join(", ", rows) : string.Join(", ", rows) + $", …of {total}";

    /// <summary>
    /// A number, written the same way on every machine that runs this.
    ///
    /// <para><c>G13</c>, not round-trip. The frame's arithmetic goes through <c>Math.Sin</c>,
    /// <c>Math.Cos</c>, <c>Math.Atan2</c> and <c>Math.Pow</c>, and the C runtime under those is glibc on the
    /// CI runner and UCRT on a developer's box — both correctly rounded to well under an ulp, but not
    /// guaranteed to agree in the last bit. Thirteen significant digits is four to five orders of magnitude
    /// clear of that noise and still finer than anything a reordered phase could hide in: the smallest real
    /// difference this bench can produce is one frame of ship motion, which is hundreds of metres on a
    /// position of 1.5e11.</para>
    /// </summary>
    private static string Num(double d)
    {
        if (double.IsNaN(d)) return "NaN";
        if (double.IsPositiveInfinity(d)) return "+∞";
        if (double.IsNegativeInfinity(d)) return "-∞";
        if (Math.Abs(d) < 1e-12) return "0";
        return d.ToString("G13", CultureInfo.InvariantCulture);
    }

    private static string Num(float f) => Num((double)f);

    // ── THE RECORDING PEN ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Every mark the walked view lays, in order, folded into one hash as it goes — so a hundred and
    /// twenty frames of a Hive floor cost one hash instead of a megabyte of text.</summary>
    private sealed class RecordingPen : IRenderer
    {
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private readonly Dictionary<string, int> _images = new(StringComparer.Ordinal);
        private string? _finished;

        public long Commands { get; private set; }

        private void Mark(string line)
        {
            Commands++;
            _hash.AppendData(Encoding.UTF8.GetBytes(line + "\n"));
        }

        public string Sha256()
        {
            _finished ??= Convert.ToHexString(_hash.GetCurrentHash()).ToLowerInvariant();
            return _finished;
        }

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) =>
            Mark($"begin {widthPx} {heightPx} {C(background)}");

        public void EndFrame() => Mark("end");

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Mark($"circle {Num(x)} {Num(y)} {Num(r)} {(fill is { } c ? C(c) : "∅")} {C(stroke)} {Num(w)}");

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) =>
            Mark($"polyline {Points(pts)} {C(stroke)} {Num(w)}");

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) =>
            Mark($"polygon {Points(pts)} {(fill is { } c ? C(c) : "∅")} {C(stroke)} {Num(w)}");

        public void DrawText(float x, float y, string text, RgbaColor color, string font = "12px sans-serif",
            TextAlign align = TextAlign.Left) =>
            Mark($"text {Num(x)} {Num(y)} \"{text}\" {C(color)} {font} {align}");

        public int RegisterImage(string url)
        {
            if (!_images.TryGetValue(url, out int id))
            {
                id = _images.Count + 1;
                _images[url] = id;
            }
            Mark($"register {url} -> {id}");
            return id;
        }

        public void DrawImage(int id, float x, float y, float w, float h, float alpha = 1f) =>
            Mark($"image {id} {Num(x)} {Num(y)} {Num(w)} {Num(h)} {Num(alpha)}");

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
            float x, float y, float w, float h, float alpha = 1f) =>
            Mark($"slice {id} {Num(sx)} {Num(sy)} {Num(sw)} {Num(sh)} " +
                 $"{Num(x)} {Num(y)} {Num(w)} {Num(h)} {Num(alpha)}");

        private static string C(RgbaColor c) => $"#{c.R},{c.G},{c.B},{c.A}";

        private static string Points(ReadOnlySpan<float> pts)
        {
            var sb = new StringBuilder(pts.Length.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < pts.Length; i++) { sb.Append(':').Append(Num(pts[i])); }
            return sb.ToString();
        }
    }

    // ── A RENDERER THAT DRAWS NOTHING ─────────────────────────────────────────────────────────────────

    /// <summary>Enough of a <see cref="Renderer"/> to give the component a render handle and a dispatcher.
    /// It never paints: <c>_hasPendingQueuedRender</c> means <c>StateHasChanged</c> never queues a batch, so
    /// <see cref="UpdateDisplayAsync"/> is never reached.</summary>
#pragma warning disable BL0006 // RenderBatch is "not recommended outside the Blazor framework" — and this IS
                               // the framework's own seam: a bench that wants a component's dispatcher has to
                               // give it a renderer, and a renderer has to be able to say "I drew nothing".
    private sealed class ARendererThatDrawsNothing : Renderer
    {
        public ARendererThatDrawsNothing() : base(NoServices.Instance, NullLoggerFactory.Instance) { }

        public override Dispatcher Dispatcher { get; } = new RightHere();

        public void Attach(IComponent component) => AssignRootComponentId(component);

        protected override void HandleException(Exception exception) =>
            throw new InvalidOperationException("the frame threw inside the renderer", exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;

        /// <summary>Run it now, on this thread, and hand back a completed task — the browser's own behaviour
        /// from inside the rAF callback, and the only way a bench can fingerprint a frame that has finished.</summary>
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

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static string FingerprintDirectory() =>
        Path.Combine(RepoRoot(), "tests", "SpaceSails.Client.Tests", "Fingerprints");

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

    /// <summary>#870 lane 6′b · The twenty-two patrol fields live on the page's <c>_patrol</c>
    /// object now, so the lookup follows them there (<see cref="PatrolState"/>); every assertion and
    /// every pinned line below still asks for the state by the name it was written with.</summary>
    private static object? Get(object o, string member) =>
        PatrolState.TryFollow(o, member, out object? onTheRound)
            ? onTheRound
            : o.GetType().GetField(member, Hidden)!.GetValue(o);

    /// <inheritdoc cref="Get"/>
    private static void Set(object o, string field, object? value)
    {
        if (!PatrolState.TrySet(o, field, value))
        {
            (o.GetType().GetField(field, Hidden)
             ?? throw new InvalidOperationException($"the component has no `{field}`.")).SetValue(o, value);
        }
    }

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        try
        {
            return call!.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
