namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-317 · The nerve gauge — the first slice of #226's Fail Forward sanity. These pin the deterministic
/// laws the client draws in a corner: the regolith's stressors fray the nerve at fixed per-second rates,
/// the ship's safety eases it back, the monolith's first sight deals one big lump, the gauge stays clamped,
/// and the house-voice flavor ladder escalates as the bar falls. Display-first — nothing here rolls or exits.
/// </summary>
public class NerveModelTests
{
    [Fact]
    public void SteadyStart_IsAFullGauge()
    {
        Assert.Equal(100.0, NerveModel.Steady, 6);
        Assert.Equal(NerveModel.Max, NerveModel.Steady, 6);
        Assert.Equal(1.0, NerveModel.Fraction(NerveModel.Steady), 6);
    }

    [Fact]
    public void QuietGround_DoesNotDrain()
    {
        // No movers, no chase, not digging, not cornered → the tracker is quiet, the nerve holds.
        var calm = new NerveModel.Stressors(MovingContacts: 0, ChaseActive: false, Digging: false, Cornered: false);
        Assert.Equal(0.0, NerveModel.DrainRatePerSecond(in calm), 6);
        Assert.Equal(90.0, NerveModel.Drain(90.0, in calm, dtSeconds: 5.0), 6);
    }

    [Fact]
    public void MovingContacts_DrainProportionally()
    {
        var one = new NerveModel.Stressors(1, false, false, false);
        var three = new NerveModel.Stressors(3, false, false, false);
        Assert.Equal(NerveModel.MovingContactDrainPerSecond, NerveModel.DrainRatePerSecond(in one), 6);
        Assert.Equal(3 * NerveModel.MovingContactDrainPerSecond, NerveModel.DrainRatePerSecond(in three), 6);
    }

    [Fact]
    public void DigUnderThreat_OnlyBitesWhenSomethingIsInbound()
    {
        // A calm dig on empty ground costs nothing beyond the (absent) contacts.
        var calmDig = new NerveModel.Stressors(0, ChaseActive: false, Digging: true, Cornered: false);
        Assert.Equal(0.0, NerveModel.DrainRatePerSecond(in calmDig), 6);

        // With a pack up, the same dig now frays: the chase rate PLUS the dig-under-threat rate.
        var threatDig = new NerveModel.Stressors(0, ChaseActive: true, Digging: true, Cornered: false);
        Assert.Equal(NerveModel.ChaseDrainPerSecond + NerveModel.DigUnderThreatDrainPerSecond,
            NerveModel.DrainRatePerSecond(in threatDig), 6);
    }

    [Fact]
    public void Cornered_AddsTheSharpestRoutineDrain()
    {
        var full = new NerveModel.Stressors(2, ChaseActive: true, Digging: true, Cornered: true);
        double expected =
            (2 * NerveModel.MovingContactDrainPerSecond)
            + NerveModel.ChaseDrainPerSecond
            + NerveModel.DigUnderThreatDrainPerSecond
            + NerveModel.CorneredDrainPerSecond;
        Assert.Equal(expected, NerveModel.DrainRatePerSecond(in full), 6);
    }

    [Fact]
    public void Drain_IsDeterministic_ForFixedInputsAndDt()
    {
        var s = new NerveModel.Stressors(1, true, false, false);
        double a = NerveModel.Drain(80.0, in s, 2.0);
        double b = NerveModel.Drain(80.0, in s, 2.0);
        Assert.Equal(a, b, 12); // same nerve + same stressors + same dt → identical result, every time

        double rate = NerveModel.DrainRatePerSecond(in s);
        Assert.Equal(80.0 - rate * 2.0, a, 9);
    }

    [Fact]
    public void MonolithShock_IsOneBigLump()
    {
        double after = NerveModel.Shock(100.0, NerveModel.MonolithSightShock);
        Assert.Equal(100.0 - NerveModel.MonolithSightShock, after, 6);
    }

    [Fact]
    public void Recover_EasesBackTowardSteady_AndClampsAtFull()
    {
        double eased = NerveModel.Recover(50.0, dtSeconds: 4.0);
        Assert.Equal(50.0 + NerveModel.AboardRecoveryPerSecond * 4.0, eased, 6);

        // The ease-off never overshoots a full gauge.
        Assert.Equal(NerveModel.Max, NerveModel.Recover(99.0, dtSeconds: 100.0), 6);
    }

    [Fact]
    public void Gauge_ClampsAtBothEnds()
    {
        // A brutal drain bottoms out at 0, never below (the bar "bottoming out" is a real floor).
        var brutal = new NerveModel.Stressors(6, true, true, true);
        Assert.Equal(NerveModel.Min, NerveModel.Drain(5.0, in brutal, dtSeconds: 100.0), 6);

        Assert.Equal(NerveModel.Min, NerveModel.Shock(2.0, 999.0), 6);
        Assert.Equal(NerveModel.Max, NerveModel.Clamp(9999.0), 6);
        Assert.Equal(NerveModel.Min, NerveModel.Clamp(-9999.0), 6);
    }

    [Fact]
    public void NegativeDt_IsANoOp_NotAGain()
    {
        var s = new NerveModel.Stressors(3, true, true, true);
        Assert.Equal(60.0, NerveModel.Drain(60.0, in s, dtSeconds: -1.0), 6); // a rewound clock never restores nerve
        Assert.Equal(60.0, NerveModel.Recover(60.0, dtSeconds: -1.0), 6);     // nor drains it
    }

    [Theory]
    [InlineData(100.0, NerveModel.NerveBand.Steady)]
    [InlineData(75.0, NerveModel.NerveBand.Steady)]
    [InlineData(74.9, NerveModel.NerveBand.Rattled)]
    [InlineData(50.0, NerveModel.NerveBand.Rattled)]
    [InlineData(49.9, NerveModel.NerveBand.Shaken)]
    [InlineData(25.0, NerveModel.NerveBand.Shaken)]
    [InlineData(24.9, NerveModel.NerveBand.Fraying)]
    [InlineData(10.0, NerveModel.NerveBand.Fraying)]
    [InlineData(9.9, NerveModel.NerveBand.Shot)]
    [InlineData(0.0, NerveModel.NerveBand.Shot)]
    public void FlavorLadder_EscalatesAsTheBarFalls(double nerve, NerveModel.NerveBand expected)
    {
        Assert.Equal(expected, NerveModel.BandFor(nerve));
        Assert.False(string.IsNullOrWhiteSpace(NerveModel.Flavor(expected)));
        Assert.Equal(NerveModel.Flavor(expected), NerveModel.Readout(nerve));
    }

    [Fact]
    public void BottomedOut_OnlySpeaks_ItSaysGetAboard()
    {
        // The whole "consequence" of a shot bar in THIS slice: a house-voice line, no throw, no exit.
        Assert.Equal(NerveModel.NerveBand.Shot, NerveModel.BandFor(0.0));
        Assert.Contains("aboard", NerveModel.Readout(0.0), System.StringComparison.OrdinalIgnoreCase);
    }

    // ── The one-per-frame Advance: the on-planet law, the airlock ease-off, the monolith-once hit. ──

    private static readonly NerveModel.Stressors ChasePressure = new(2, ChaseActive: true, Digging: false, Cornered: false);

    [Fact]
    public void Advance_OnPlanetOnly_GaugeVisibleDuringExcursion_HiddenAboardShip()
    {
        // Off-excursion (flying the ship / docked): the gauge is hidden.
        var offPlanet = new NerveModel.Frame(OnExcursion: false, OnRegolith: false, SeesMonolith: false, default, 0.5);
        Assert.False(NerveModel.Advance(100.0, false, in offPlanet).GaugeVisible);

        // On an excursion — whether out on the regolith or stood in the airlock — the gauge shows.
        var onRegolith = new NerveModel.Frame(true, OnRegolith: true, false, ChasePressure, 0.5);
        var inAirlock = new NerveModel.Frame(true, OnRegolith: false, false, default, 0.5);
        Assert.True(NerveModel.Advance(100.0, false, in onRegolith).GaugeVisible);
        Assert.True(NerveModel.Advance(100.0, false, in inAirlock).GaugeVisible);
    }

    [Fact]
    public void Advance_OnRegolith_Drains_WhileTheAirlockEasesOff()
    {
        // Out on the regolith under pressure → the nerve falls.
        var regolith = new NerveModel.Frame(true, OnRegolith: true, false, ChasePressure, 1.0);
        NerveModel.Step drained = NerveModel.Advance(70.0, monolithSeen: true, in regolith);
        Assert.True(drained.Nerve < 70.0);
        Assert.Equal(70.0 - NerveModel.DrainRatePerSecond(in ChasePressure), drained.Nerve, 6);

        // Stood back up through the airlock (same excursion, no longer on the regolith) → it eases back.
        var airlock = new NerveModel.Frame(true, OnRegolith: false, false, default, 1.0);
        NerveModel.Step eased = NerveModel.Advance(70.0, monolithSeen: true, in airlock);
        Assert.Equal(70.0 + NerveModel.AboardRecoveryPerSecond, eased.Nerve, 6);
    }

    [Fact]
    public void Advance_OffPlanet_EasesTheNerveBackTowardSteady()
    {
        var flying = new NerveModel.Frame(OnExcursion: false, OnRegolith: false, false, default, 2.0);
        NerveModel.Step step = NerveModel.Advance(40.0, monolithSeen: true, in flying);
        Assert.Equal(40.0 + NerveModel.AboardRecoveryPerSecond * 2.0, step.Nerve, 6);
        Assert.False(step.GaugeVisible);
    }

    [Fact]
    public void Advance_MonolithFirstSight_FiresExactlyOnce()
    {
        var seeing = new NerveModel.Frame(true, OnRegolith: true, SeesMonolith: true, default, 0.5);

        // Frame 1 — first sight: the flag latches, the hit fires, the big lump lands.
        NerveModel.Step first = NerveModel.Advance(100.0, monolithSeen: false, in seeing);
        Assert.True(first.MonolithHitFired);
        Assert.True(first.MonolithSeen);
        Assert.Equal(100.0 - NerveModel.MonolithSightShock, first.Nerve, 6);

        // Frame 2 — still staring at it, but already seen: NO second hit (only the routine drain, here 0).
        NerveModel.Step second = NerveModel.Advance(first.Nerve, first.MonolithSeen, in seeing);
        Assert.False(second.MonolithHitFired);
        Assert.True(second.MonolithSeen);
        Assert.Equal(first.Nerve, second.Nerve, 6); // no movers/chase in this frame → no further drain
    }

    [Fact]
    public void Advance_MonolithHit_NeverFires_WhenAboard()
    {
        // The monolith can't gore you from the airlock — the hit only lands out on the regolith.
        var aboardSeeing = new NerveModel.Frame(OnExcursion: true, OnRegolith: false, SeesMonolith: true, default, 0.5);
        NerveModel.Step step = NerveModel.Advance(100.0, monolithSeen: false, in aboardSeeing);
        Assert.False(step.MonolithHitFired);
        Assert.False(step.MonolithSeen);
    }

    // ── A drink restores nerve (#308/#321 → #226): the sanity-relief seam, wired. ──────────────────
    // Owner ruling 2026-07-18: a shared drink (conversation + company) restores at any level; a lone
    // drink is weak medicine that fades to a single point at the shot floor; diminishing repeat and the
    // tilty-legs drunk gate ride on top. NUMBERS ARE FLAGGED FOR TUNING — these pin the SHAPE, not gospel.

    [Fact]
    public void FirstSharedDrink_RestoresTheFlatLump_AtSteady()
    {
        // A shared glass at a rattled nerve returns its flat value — company, not the counter.
        double before = 60.0;
        double after = NerveModel.DrinkRestore(before, NerveModel.DrinkKind.SharedWithContact, totNumber: 1);
        Assert.Equal(before + NerveModel.SharedDrinkRestore, after, 6);
    }

    [Fact]
    public void SharedDrink_RestoresEvenAtTheShotFloor()
    {
        // The whole point of the ruling: a face across the table steadies the hands even when nerves are
        // shot. A lone drink here (next test) would move the needle by only one; the shared one lands full.
        double after = NerveModel.DrinkRestore(NerveModel.Min, NerveModel.DrinkKind.SharedWithContact, 1);
        Assert.Equal(NerveModel.Min + NerveModel.SharedDrinkRestore, after, 6);
    }

    [Fact]
    public void SoloDrink_AtTheShotFloor_MovesTheNeedleByOne()
    {
        // "You cannot drink yourself back from the edge alone." A lone bar special or galley tot at the
        // floor restores exactly the single point, no more.
        Assert.Equal(NerveModel.SoloFloorRestore,
            NerveModel.RestoreAmount(NerveModel.DrinkKind.BarSpecial, NerveModel.Min, totNumber: 1), 6);
        Assert.Equal(NerveModel.SoloFloorRestore,
            NerveModel.RestoreAmount(NerveModel.DrinkKind.GalleyTot, NerveModel.Min, totNumber: 1), 6);
    }

    [Fact]
    public void SoloDrink_IsModestInTheMidRange_AndScalesWithNerve()
    {
        // Weak medicine: a lone drink helps more the steadier you already are, and least at the edge.
        double atFloor = NerveModel.RestoreAmount(NerveModel.DrinkKind.BarSpecial, 0.0, 1);
        double atMid = NerveModel.RestoreAmount(NerveModel.DrinkKind.BarSpecial, 50.0, 1);
        double atSteady = NerveModel.RestoreAmount(NerveModel.DrinkKind.BarSpecial, 100.0, 1);
        Assert.True(atFloor < atMid, "a lone drink helps less at the floor than mid-range");
        Assert.True(atMid < atSteady, "a lone drink helps most when you're already steady");
        // The steady-hands cap is the type's full base; the floor is the single point.
        Assert.Equal(NerveModel.BarSpecialBaseRestore, atSteady, 6);
        Assert.Equal(NerveModel.SoloFloorRestore, atFloor, 6);
    }

    [Fact]
    public void DrinkOrdering_SharedBeatsBar_BeatsTot_InTheMidRange()
    {
        // The ordering the owner keeps "in spirit": tot < bar < shared (shared now categorically different).
        double nerve = 50.0;
        double tot = NerveModel.RestoreAmount(NerveModel.DrinkKind.GalleyTot, nerve, 1);
        double bar = NerveModel.RestoreAmount(NerveModel.DrinkKind.BarSpecial, nerve, 1);
        double shared = NerveModel.RestoreAmount(NerveModel.DrinkKind.SharedWithContact, nerve, 1);
        Assert.True(tot < bar, "a lone tot restores less than a lone house special");
        Assert.True(bar < shared, "a lone house special restores less than a shared glass");
    }

    [Fact]
    public void RepeatedDrinks_Diminish_TheSecondSoothesLessThanTheFirst()
    {
        // Rounds in quick succession soothe less each time — keyed off the existing tot count.
        double nerve = 40.0;
        double first = NerveModel.RestoreAmount(NerveModel.DrinkKind.SharedWithContact, nerve, totNumber: 1);
        double second = NerveModel.RestoreAmount(NerveModel.DrinkKind.SharedWithContact, nerve, totNumber: 2);
        Assert.True(second < first, "the second round soothes less than the first");
        Assert.Equal(first * NerveModel.RepeatFactor(2), second, 6);
    }

    [Fact]
    public void DrunkDrink_RestoresNothing_ForEveryKind()
    {
        // Once the tilty-legs threshold is reached, the rum has stopped helping — drunk is not sane.
        Assert.True(NerveModel.DrunkAt(NerveModel.DrunkTotCount));
        foreach (NerveModel.DrinkKind kind in Enum.GetValues<NerveModel.DrinkKind>())
        {
            Assert.Equal(0.0, NerveModel.RestoreAmount(kind, 30.0, NerveModel.DrunkTotCount), 6);
            Assert.Equal(30.0, NerveModel.DrinkRestore(30.0, kind, NerveModel.DrunkTotCount), 6); // unchanged
        }
    }

    [Fact]
    public void DrinkRestore_CapsAtTheFullGauge()
    {
        // A shared drink at a near-full gauge cannot overflow past steady hands.
        double after = NerveModel.DrinkRestore(95.0, NerveModel.DrinkKind.SharedWithContact, 1);
        Assert.Equal(NerveModel.Max, after, 6);
    }

    [Fact]
    public void SteadyingNote_SpeaksTheRightVoice_ByOutcome()
    {
        // Drunk → the rum stopped helping; a real shared rise → the company steadies; a lone floor drink →
        // it admits it needs a face across the table.
        Assert.Contains("stopped helping",
            NerveModel.SteadyingNote(NerveModel.DrinkKind.BarSpecial, NerveModel.DrunkTotCount, 0.0));
        Assert.Contains("company",
            NerveModel.SteadyingNote(NerveModel.DrinkKind.SharedWithContact, 1, NerveModel.SharedDrinkRestore));
        Assert.Contains("face across the table",
            NerveModel.SteadyingNote(NerveModel.DrinkKind.BarSpecial, 1, NerveModel.SoloFloorRestore));
    }
}
