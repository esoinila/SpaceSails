using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;
using Pages = SpaceSails.Client.Pages;

namespace SpaceSails.Client.Tests;

/// <summary>
/// <b>BAR SEAMS (Fable rulings 2026-11-07, R1-F97 / R1-F96 / R1-F105 / R1-F104 / R1-F20).</b>
///
/// <para>One arc shard a sitting at Marsh's stool; a second-hand leak assembles the shard but raises no plate,
/// and the plate is raised later, once, on witness; no "Ask about KAAMOS" for a captain with no thread; and the
/// bar's rare contacts all read the dock clock. Every test drives the shipping methods on a booted page and
/// reads the state back; the RED for each is named on it.</para>
/// </summary>
public sealed class TheBarSeamsTests
{
    private const string Ashore = "/map?oracle=1&ashore=1";

    private static OracleLine AKaamosLine(string id) =>
        new("a true one, " + id, true, OracleTruthKind.KaamosFragment, id, false);

    private static OracleLine ANebulaLine(string id) =>
        new("a true one, " + id, true, OracleTruthKind.NebulaFragment, id, false);

    private static KaamosProgress Kaamos(DeskBench b) => (KaamosProgress)b.Peek("_kaamos")!;

    private static NebulaProgress Nebula(DeskBench b) => (NebulaProgress)b.Peek("_nebula")!;

    /// <summary>How many SHARD plates have spoken (the arc-news wire beat and the rest are not the plate).</summary>
    private static int Spoken(DeskBench b) =>
        ((IDictionary)b.Peek("_beatsSpoken")!).Keys.Cast<object>()
            .Count(k => k.GetType().GetField("Item1")!.GetValue(k)!.ToString() is "KaamosShardFound" or "NebulaShardFound");

    private static int OverheardByMarsh(DeskBench b) =>
        ((IEnumerable<OverheardLine>)b.Peek("_overheard")!).Count(o => o.Text.Contains("Static Marsh", StringComparison.Ordinal));

    private static async Task<DeskBench> SittingAsync(string url = Ashore)
    {
        DeskBench b = await DeskBench.BootAsync(url);
        await b.RenderAsync();
        b.CallOnTheDispatcher("TalkToOracle");
        Assert.True((bool)b.Peek("_oracleOpen")!, "premise: her corner card is up");
        return b;
    }

    // ── R1-F97 · one arc shard per sitting ───────────────────────────────────────────────────────────

    /// <summary><b>Proven RED</b> by deleting the <c>_oracleShardsThisVisit &gt;= 1</c> arm in
    /// <c>DeliverOracleLine</c>: both KAAMOS lines assemble.</summary>
    [Fact]
    public async Task TwoTrueFragmentLinesInOneSittingGiveOneShardAndTwoOverheardLines()
    {
        DeskBench b = await SittingAsync();
        int before = Kaamos(b).Count;

        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("holders-tell"));
        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("vantar-log"));

        Assert.Equal(before + 1, Kaamos(b).Count);
        Assert.True(Kaamos(b).Has("holders-tell"));
        Assert.False(Kaamos(b).Has("vantar-log"), "the second true fragment of the sitting assembled a second shard");
        Assert.Equal(2, OverheardByMarsh(b));
    }

    /// <summary>The cap is per arc-pair, not per arc: a NEBULA fragment after a KAAMOS one is the same sitting.
    /// <b>Proven RED</b> by removing the cap from the NEBULA arm alone.</summary>
    [Fact]
    public async Task ANebulaFragmentAfterAKaamosOneInTheSameSittingAssemblesNothing()
    {
        DeskBench b = await SittingAsync();

        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("holders-tell"));
        b.CallOnTheDispatcher("DeliverOracleLine", ANebulaLine("clinic-ledger"));

        Assert.False(Nebula(b).Has("clinic-ledger"));
        Assert.Equal(2, OverheardByMarsh(b));
    }

    /// <summary>A new sitting (a new berth) is a fresh allowance. <b>Proven RED</b> by not resetting the count in
    /// <c>EnsureOracleVisit</c>.</summary>
    [Fact]
    public async Task ANewSittingIsAFreshAllowance()
    {
        DeskBench b = await SittingAsync();
        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("holders-tell"));
        Assert.Equal(1, (int)b.Peek("_oracleShardsThisVisit")!);

        b.CallOnTheDispatcher("CloseOracle");
        b.Poke("_oracleStation", "somewhere-else");   // the next berth: EnsureOracleVisit folds the reading
        b.CallOnTheDispatcher("TalkToOracle");

        Assert.Equal(0, (int)b.Peek("_oracleShardsThisVisit")!);
        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("vantar-log"));
        Assert.True(Kaamos(b).Has("vantar-log"));
    }

    // ── R1-F96 · a leak assembles the shard, raises no plate; the plate is raised on witness ─────────

    /// <summary><b>Proven RED</b> by passing <c>witnessed: true</c> from the oracle: the leak raises the
    /// <c>KaamosShardFound</c> beat.</summary>
    [Fact]
    public async Task AnOracleLeakOfTheColdPodAssemblesItHeardAndRaisesNoPlate()
    {
        DeskBench b = await SittingAsync();
        Assert.Equal(0, Spoken(b));

        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("cold-pod"));

        Assert.True(Kaamos(b).Has("cold-pod"));
        Assert.True(Kaamos(b).WasHeard("cold-pod"));
        Assert.Equal(0, Spoken(b));
        Assert.Null(b.Peek("_storyCard"));
    }

    /// <summary>The same law on arc 2. <b>Proven RED</b> by passing <c>witnessed: true</c> from the NEBULA arm.</summary>
    [Fact]
    public async Task AnOracleLeakOfTheAdjusterTellRaisesNoPlateEither()
    {
        DeskBench b = await SittingAsync();

        b.CallOnTheDispatcher("DeliverOracleLine", ANebulaLine("adjuster-tell"));

        Assert.True(Nebula(b).Has("adjuster-tell"));
        Assert.Equal(0, Spoken(b));
    }

    /// <summary>The heard shard persists as heard (the vault carries it), and Marsh's frame is the plate it gets.
    /// <b>Proven RED</b> by dropping <c>HeardFragmentIds</c> from <c>VaultMapper.ToSection</c>.</summary>
    [Fact]
    public void AHeardShardSurvivesTheVaultAndOnlyThatShard()
    {
        var progress = new KaamosProgress();
        progress.Assemble("cold-pod");
        progress.Assemble("holders-tell");
        progress.MarkHeard("cold-pod");

        var back = new KaamosProgress();
        VaultMapper.Apply(VaultMapper.ToSection(progress), back);

        Assert.True(back.WasHeard("cold-pod"));
        Assert.False(back.WasHeard("holders-tell"));
        Assert.False(new KaamosProgress().MarkHeard("cold-pod"), "a shard nobody holds cannot be heard");
    }

    /// <summary>Later, once, on witness: a probe on the pod square raises the pod's OWN plate (Fable ruling A)
    /// and says the re-find line; the witness is then paid, and a dug pod is ordinary regolith (D3).
    /// <b>Proven RED</b> by restoring the <c>!_kaamos.Has("cold-pod")</c> guard on the pod branch in
    /// <c>ProbeHere</c>.</summary>
    [Fact]
    public void AProbeOnTheHeardPodRaisesItsOwnPlateOnceAndTheRefindLine()
    {
        Pages.Map map = OnTheIce();
        Field(map, "_kaamos", out KaamosProgress kaamos);
        kaamos.Assemble("cold-pod");
        kaamos.MarkHeard("cold-pod");
        Set(map, "_kaamosPodCheat", true);
        Assert.Empty((IDictionary)Get(map, "_beatsSpoken")!);

        Probe(map);

        IDictionary spoken = (IDictionary)Get(map, "_beatsSpoken")!;
        var key = Assert.Single(spoken.Keys.Cast<object>());
        Assert.Equal("cold-pod", (string?)key.GetType().GetField("Item2")!.GetValue(key));
        // …said on the card that went up on the same press (#736: a pulse under a card is not on the screen).
        object card = Get(map, "_storyCard")!;
        Assert.Equal("cold-pod", (string?)card.GetType().GetField("Item2")!.GetValue(card));
        Assert.Equal(
            "❄ Your probe rings off metal a foot down. You knew she was here. Knowing is not the same as standing over her.",
            (string?)card.GetType().GetField("Item3")!.GetValue(card));
        Assert.True(kaamos.WasWitnessed("cold-pod"));
    }

    /// <summary>D3 · a pod that was dug (held, not heard) or already stood over is ordinary regolith: the
    /// re-find line does not repeat forever. <b>Proven RED</b> by dropping the owed clause from the pod branch.</summary>
    [Fact]
    public void ADugPodDoesNotRepeatTheRefindLine()
    {
        Pages.Map map = OnTheIce();
        Field(map, "_kaamos", out KaamosProgress kaamos);
        kaamos.Assemble("cold-pod");   // held, never heard: dug, or granted
        Set(map, "_kaamosPodCheat", true);
        Set(map, "_pulse", default(PulseSlot));

        Probe(map);

        Assert.Null(Get(map, "_storyCard"));
        string? said = ((PulseSlot)Get(map, "_pulse")!).Message;
        Assert.DoesNotContain("You knew she was here", said ?? "", StringComparison.Ordinal);
    }

    // ── D1 · the witness survives a reload ───────────────────────────────────────────────────────────

    /// <summary>A heard shard, witnessed once, then reloaded through the vault, owes nothing; the same shard
    /// before the witness still owes it. <b>Proven RED</b> by dropping <c>WitnessedFragmentIds</c> from
    /// <c>VaultMapper.ToSection</c> (KAAMOS) and its NEBULA twin.</summary>
    [Fact]
    public void AWitnessedShardIsNotOwedAgainAfterAReload()
    {
        var k = new KaamosProgress();
        k.Assemble("holders-tell");
        k.MarkHeard("holders-tell");
        Assert.True(k.WitnessOwed("holders-tell"));
        var k2 = new KaamosProgress();
        VaultMapper.Apply(VaultMapper.ToSection(k), k2);
        Assert.True(k2.WitnessOwed("holders-tell"), "heard and not yet stood over: still owed after a reload");

        k.MarkWitnessed("holders-tell");
        var k3 = new KaamosProgress();
        VaultMapper.Apply(VaultMapper.ToSection(k), k3);
        Assert.False(k3.WitnessOwed("holders-tell"));

        var n = new NebulaProgress();
        n.Assemble("adjuster-tell");
        n.MarkHeard("adjuster-tell");
        n.MarkWitnessed("adjuster-tell");
        var n2 = new NebulaProgress();
        VaultMapper.Apply(VaultMapper.ToSection(n), n2);
        Assert.True(n2.WasHeard("adjuster-tell"));
        Assert.False(n2.WitnessOwed("adjuster-tell"));
    }

    /// <summary>An old save (no heard, no witnessed) owes nothing: a held shard that was not heard was dug or
    /// read face to face. <b>Proven RED</b> by making <c>WitnessOwed</c> ignore the heard set.</summary>
    [Fact]
    public async Task AHeldTellThatWasNotHeardGivesNoPlateStepOnAFreshPage()
    {
        DeskBench b = await DeskBench.BootAsync("/map?ashore=1&kaamos=holder");
        await b.RenderAsync();
        Kaamos(b).Assemble("holders-tell");   // an old save: held, nothing recorded about how
        Assert.NotEqual("holders-plate", b.Call("KaamosBarNextStep"));
        Nebula(b).Assemble("adjuster-tell");
        b.Poke("_nebulaAdjusterCheat", true);
        Assert.NotEqual("adjuster-plate", b.Call("NebulaBarNextStep"));
    }

    /// <summary>The bar's half: a heard tell with the holder present offers the "holders-plate" step; asking
    /// raises the holder's own card once, and the offer is spent for good (also across a reload).
    /// <b>Proven RED</b> by removing the step from <c>KaamosBarNextStep</c>.</summary>
    [Fact]
    public async Task AHeardTellWithTheHolderPresentOffersThePlateOnceAndSpendsIt()
    {
        DeskBench b = await SittingAsync("/map?oracle=1&ashore=1&kaamos=holder");
        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("holders-tell"));
        Assert.Equal(0, Spoken(b));

        Assert.Equal("holders-plate", b.Call("KaamosBarNextStep"));
        Assert.Equal("🌑 Find the one who kept the berth", b.Call("KaamosBarSeamLabel"));
        Assert.Equal("You have the words already. The barkeep knows the stool.", b.Call("KaamosBarSeamTitle"));

        b.CallOnTheDispatcher("AskAboutKaamos");

        Assert.Equal(1, Spoken(b));
        Assert.NotEqual("holders-plate", b.Call("KaamosBarNextStep"));

        // …a reload forgets the page's beat memory but not the witness.
        ((IDictionary)b.Peek("_beatsSpoken")!).Clear();
        Assert.NotEqual("holders-plate", b.Call("KaamosBarNextStep"));
    }

    /// <summary>The adjuster's half of the same law. <b>Proven RED</b> by removing the
    /// <c>adjuster-plate</c> step from <c>NebulaBarNextStep</c>.</summary>
    [Fact]
    public async Task AHeardAdjusterTellOffersThePlateStepOnceAndSpendsIt()
    {
        DeskBench b = await SittingAsync("/map?oracle=1&ashore=1&nebula=adjuster");
        b.CallOnTheDispatcher("DeliverOracleLine", ANebulaLine("adjuster-tell"));
        Assert.True(Nebula(b).WasHeard("adjuster-tell"));
        Assert.Equal(0, Spoken(b));
        Assert.Equal("adjuster-plate", b.Call("NebulaBarNextStep"));

        b.CallOnTheDispatcher("AskAboutNebula");

        Assert.Equal(1, Spoken(b));
        Assert.True(Nebula(b).WasWitnessed("adjuster-tell"));
        Assert.NotEqual("adjuster-plate", b.Call("NebulaBarNextStep"));
        ((IDictionary)b.Peek("_beatsSpoken")!).Clear();
        Assert.NotEqual("adjuster-plate", b.Call("NebulaBarNextStep"));
    }

    /// <summary>D2 · the sitting's shard allowance belongs to the dock visit: undock and redock at the SAME
    /// station is a new sitting. <b>Proven RED</b> by keying the fold on the station alone.</summary>
    [Fact]
    public async Task ARedockAtTheSameStationIsANewSitting()
    {
        DeskBench b = await SittingAsync();
        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("holders-tell"));
        Assert.Equal(1, (int)b.Peek("_oracleShardsThisVisit")!);

        b.CallOnTheDispatcher("CloseOracle");
        b.Poke("_dockVisitSimTime", (double)b.Peek("_dockVisitSimTime")! + (4 * 3600.0));   // cast off, tie up again
        b.CallOnTheDispatcher("TalkToOracle");

        Assert.Equal(0, (int)b.Peek("_oracleShardsThisVisit")!);
        b.CallOnTheDispatcher("DeliverOracleLine", AKaamosLine("vantar-log"));
        Assert.True(Kaamos(b).Has("vantar-log"));
    }

    // ── R1-F105 · no "Ask about KAAMOS" at zero shards ───────────────────────────────────────────────

    /// <summary><b>Proven RED</b> by reverting the condition to the bare
    /// <c>!_kaamos.Has("holders-tell") &amp;&amp; HolderAtBar</c>: the cold captain is offered the tell.</summary>
    [Fact]
    public async Task AColdCaptainIsNotOfferedTheHoldersTellButOneWithAThreadIs()
    {
        DeskBench b = await DeskBench.BootAsync("/map?ashore=1&kaamos=holder");
        await b.RenderAsync();
        b.Poke("_kaamosHolderCheat", false);   // the dev seat forces it; the law is for everybody else
        // Seat the holder honestly: find a dock-clock day the seeded rota puts him at this bar.
        string bar = (string)b.Peek("_dockedHavenId")!;
        int day = Enumerable.Range(0, 400).First(d => KaamosFind.HolderAtBar(bar, d));
        b.Poke("_dockVisitSimTime", day * 86400.0);

        Assert.Equal(0, Kaamos(b).Count);
        Assert.Null(b.Call("KaamosBarNextStep"));

        Kaamos(b).Assemble("vantar-log");
        Assert.Equal("holders-tell", b.Call("KaamosBarNextStep"));
    }

    [Fact]
    public async Task TheBouncedFilingIsAThreadToo()
    {
        DeskBench b = await DeskBench.BootAsync("/map?ashore=1");
        await b.RenderAsync();
        string bar = (string)b.Peek("_dockedHavenId")!;
        int day = Enumerable.Range(0, 400).First(d => KaamosFind.HolderAtBar(bar, d));
        b.Poke("_dockVisitSimTime", day * 86400.0);

        Assert.Null(b.Call("KaamosBarNextStep"));
        Kaamos(b).MarkBerthFilingBounced();
        Assert.Equal("holders-tell", b.Call("KaamosBarNextStep"));
    }

    [Fact]
    public async Task TheDevSeatStillForcesTheTell()
    {
        DeskBench b = await DeskBench.BootAsync("/map?ashore=1&kaamos=holder");
        await b.RenderAsync();
        Assert.Equal(0, Kaamos(b).Count);
        Assert.Equal("holders-tell", b.Call("KaamosBarNextStep"));
    }

    // ── R1-F104 · one dock clock ─────────────────────────────────────────────────────────────────────

    /// <summary>Dock, warp three days at the counter: the KAAMOS step, the adjuster seam and the room's
    /// roster read the frozen dock clock and do not move. Swept over forty starting days so a clock that
    /// merely happened to agree cannot pass. <b>Proven RED</b> by reading <c>SimTime</c> in
    /// <c>KaamosBarNextStep</c> (and again in <c>NebulaBarNextStep</c>).</summary>
    [Fact]
    public async Task AThreeDayWarpAtTheCounterChangesNeitherTheSeamNorTheRoom()
    {
        DeskBench b = await DeskBench.BootAsync("/map?ashore=1");
        await b.RenderAsync();
        string bar = (string)b.Peek("_dockedHavenId")!;
        Kaamos(b).Assemble("vantar-log");   // a thread, so the holder's tell is on offer at all
        Nebula(b).Assemble("clinic-ledger");

        int flippedKaamos = 0, flippedNebula = 0;
        for (int day = 0; day < 40; day++)
        {
            double at = day * 86400.0;
            b.Poke("_dockVisitSimTime", at);
            b.Poke("SimTime", at);
            object? k0 = b.Call("KaamosBarNextStep");
            object? n0 = b.Call("NebulaBarNextStep");
            object? room0 = b.Call("WhoIsInTonight");

            b.Poke("SimTime", at + (3 * 86400.0));

            Assert.Equal(k0, b.Call("KaamosBarNextStep"));
            Assert.Equal(n0, b.Call("NebulaBarNextStep"));
            Assert.Equal(room0, b.Call("WhoIsInTonight"));

            flippedKaamos += KaamosFind.HolderAtBar(bar, day) != KaamosFind.HolderAtBar(bar, day + 3) ? 1 : 0;
            flippedNebula += NebulaFind.AdjusterAtBar(bar, day) != NebulaFind.AdjusterAtBar(bar, day + 3) ? 1 : 0;
        }

        // The sweep can tell pass from fail: the live clock WOULD have moved the answer on some of those days.
        Assert.True(flippedKaamos > 0 && flippedNebula > 0,
            "no day in the sweep moves the seeded rota three days on, so a live-clock read would have passed too");
    }

    /// <summary>R1-F104 · the returned-filing agent is a rare contact too: a three-day warp at the counter does
    /// not bring him or send him home. Swept so a live-clock read cannot pass by luck. <b>Proven RED</b> by
    /// reading <c>SimTime</c> in <c>MakeKaamosBounceOffer</c>.</summary>
    [Fact]
    public async Task AThreeDayWarpDoesNotMoveTheFilingAgent()
    {
        DeskBench b = await DeskBench.BootAsync("/map?ashore=1");
        await b.RenderAsync();
        string bar = (string)b.Peek("_dockedHavenId")!;

        int flipped = 0;
        for (int day = 0; day < 60; day++)
        {
            b.Poke("_dockVisitSimTime", day * 86400.0);
            b.Poke("SimTime", day * 86400.0);
            bool before = b.Call("MakeKaamosBounceOffer", "a freight agent") is not null;
            b.Poke("SimTime", (day + 3) * 86400.0);
            Assert.Equal(before, b.Call("MakeKaamosBounceOffer", "a freight agent") is not null);
            flipped += KaamosFind.BounceAtBar(bar, day) != KaamosFind.BounceAtBar(bar, day + 3) ? 1 : 0;
        }

        Assert.True(flipped > 0, "no day in the sweep moves the seeded agent three days on, so a live-clock read would pass too");
    }

    /// <summary>Her stool is the dock's too: warping three days while she sits does not take her away.
    /// <b>Proven RED</b> by reading <c>SimTime</c> in <c>TalkToOracle</c>'s presence gate.</summary>
    [Fact]
    public async Task ThreeDaysAtHerStoolDoNotSendHerHome()
    {
        DeskBench b = await DeskBench.BootAsync("/map?ashore=1");
        await b.RenderAsync();
        string bar = (string)b.Peek("_dockedHavenId")!;
        int day = Enumerable.Range(0, 400).First(d =>
            SpaceSails.Client.Rendering.HavenInterior.OraclePresent(bar, d * 86400.0) && !SpaceSails.Client.Rendering.HavenInterior.OraclePresent(bar, (d + 3) * 86400.0));
        b.Poke("_dockVisitSimTime", day * 86400.0);
        b.Poke("SimTime", (day + 3) * 86400.0);

        b.CallOnTheDispatcher("TalkToOracle");

        Assert.True((bool)b.Peek("_oracleOpen")!, "the dock clock says she is on her stool; the live clock sent her home");
    }

    // ── plumbing for the bare surface ────────────────────────────────────────────────────────────────

    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static Pages.Map OnTheIce()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", Hidden)!.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody("europa", "Europa", "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;
        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex, new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        Set(map, "_surface", ex);
        return map;
    }

    private static void Probe(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        typeof(Pages.Map).GetMethod("ProbeHere", Hidden)!.Invoke(map, [ex, 3, 3]);
    }

    private static object? Get(Pages.Map map, string name) => typeof(Pages.Map).GetField(name, Hidden)!.GetValue(map);

    private static void Set(Pages.Map map, string name, object? value) => typeof(Pages.Map).GetField(name, Hidden)!.SetValue(map, value);

    private static void Field<T>(Pages.Map map, string name, out T value) => value = (T)Get(map, name)!;
}
