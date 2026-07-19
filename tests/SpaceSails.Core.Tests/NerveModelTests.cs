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
    public void MovingContacts_NoLongerAddAContinuousRate_SightingsOwnThemNow()
    {
        // #379 / Evening wind #18 re-tune: "seeing one reever after already seeing one more does not make
        // you that much faster more nuts." The moving-contact stress is no longer a LINEAR per-second drain
        // (that linear stack is exactly why the gauge bottomed out too easily at Ganymede). It is priced now
        // as discrete, per-spell DIMINISHING sighting jolts (AdvanceSightings / SightingSeriesCost). So the
        // continuous drain rate is INDEPENDENT of the moving-contact count — a wall of movers, on its own,
        // adds no continuous rate at all.
        var one = new NerveModel.Stressors(1, false, false, false);
        var many = new NerveModel.Stressors(9, false, false, false);
        Assert.Equal(0.0, NerveModel.DrainRatePerSecond(in one), 6);
        Assert.Equal(0.0, NerveModel.DrainRatePerSecond(in many), 6);
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
        // #379 re-tune: the continuous rate is the SUSTAINED situation only — chase + dig-under-threat +
        // cornered. The moving-contact count no longer adds a term of its own (sightings own it, #18); the
        // two movers here only serve to keep the dig "under threat" (something inbound).
        var full = new NerveModel.Stressors(2, ChaseActive: true, Digging: true, Cornered: true);
        double expected =
            NerveModel.ChaseDrainPerSecond
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

        // #379: the raw rate is now shaped by the S-curve at the CURRENT level before it bites.
        double rate = NerveModel.DrainRatePerSecond(in s);
        Assert.Equal(80.0 - (rate * NerveModel.RateScale(80.0) * 2.0), a, 9);
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
        // #379: at MID-gauge (n=50) the S-curve scale is exactly 1.0 — the peak — so the pre-#379 mid-range
        // feel is preserved unchanged: recovery here is the flat per-second rate.
        Assert.Equal(1.0, NerveModel.RateScale(50.0), 9);
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
        // #379: the drain is the sustained rate shaped by the S-curve at the current level (70).
        Assert.Equal(70.0 - (NerveModel.DrainRatePerSecond(in ChasePressure) * NerveModel.RateScale(70.0)), drained.Nerve, 6);

        // Stood back up through the airlock (same excursion, no longer on the regolith) → it eases back,
        // also shaped by the S-curve at the current level.
        var airlock = new NerveModel.Frame(true, OnRegolith: false, false, default, 1.0);
        NerveModel.Step eased = NerveModel.Advance(70.0, monolithSeen: true, in airlock);
        Assert.Equal(70.0 + (NerveModel.AboardRecoveryPerSecond * NerveModel.RateScale(70.0)), eased.Nerve, 6);
    }

    [Fact]
    public void Advance_OffPlanet_EasesTheNerveBackTowardSteady()
    {
        var flying = new NerveModel.Frame(OnExcursion: false, OnRegolith: false, false, default, 2.0);
        NerveModel.Step step = NerveModel.Advance(40.0, monolithSeen: true, in flying);
        // #379: recovery shaped by the S-curve at the current level (40).
        Assert.Equal(40.0 + (NerveModel.AboardRecoveryPerSecond * NerveModel.RateScale(40.0) * 2.0), step.Nerve, 6);
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

    // --- MED BAY calming pill (owner's Evening-wind ruling, 2026-07-18): the pill reaches the nerve
    //     through THIS same relief seam. Flat, level-independent medicine — a touch stronger than a lone
    //     tot, bounded by finite stock (the client's business), not by drunkenness. ---

    [Fact]
    public void CalmingPill_RestoresFlatAndLevelIndependent_LikeMedicine()
    {
        // A pill soothes the same amount at the shot floor as at mid-nerve — it does not ride the solo
        // weak-medicine curve; medicine steadies the hands even when nerves are shot.
        double atFloor = NerveModel.RestoreAmount(NerveModel.DrinkKind.CalmingPill, NerveModel.Min, totNumber: 1);
        double atMid = NerveModel.RestoreAmount(NerveModel.DrinkKind.CalmingPill, 50.0, totNumber: 1);
        Assert.Equal(NerveModel.CalmingPillRestore, atFloor, 6);
        Assert.Equal(NerveModel.CalmingPillRestore, atMid, 6);
    }

    [Fact]
    public void CalmingPill_IsStrongerThanALoneGalleyTot()
    {
        // The owner's magnitude call: a pill restores more than a lone galley tot at any level below steady.
        double nerve = 40.0;
        double pill = NerveModel.RestoreAmount(NerveModel.DrinkKind.CalmingPill, nerve, totNumber: 1);
        double tot = NerveModel.RestoreAmount(NerveModel.DrinkKind.GalleyTot, nerve, totNumber: 1);
        Assert.True(pill > tot, "a calming pill is a touch stronger than a lone tot");
    }

    [Fact]
    public void CalmingPill_AppliedToNerve_RaisesByItsFlatRestore_AndCapsAtGauge()
    {
        double before = 30.0;
        double after = NerveModel.DrinkRestore(before, NerveModel.DrinkKind.CalmingPill, totNumber: 1);
        Assert.Equal(before + NerveModel.CalmingPillRestore, after, 6);

        // A pill at a near-full gauge cannot overflow past steady hands.
        Assert.Equal(NerveModel.Max, NerveModel.DrinkRestore(95.0, NerveModel.DrinkKind.CalmingPill, totNumber: 1), 6);
    }

    [Fact]
    public void CalmingPill_SpeaksTheMedBayVoice_ByOutcome()
    {
        Assert.Contains("takes hold",
            NerveModel.SteadyingNote(NerveModel.DrinkKind.CalmingPill, totNumber: 1, NerveModel.CalmingPillRestore));
        Assert.Contains("already steady",
            NerveModel.SteadyingNote(NerveModel.DrinkKind.CalmingPill, totNumber: 1, restored: 0.0));
    }

    // ── #379 · The S-curve rate law (owner, Ganymede 2026-07-19: "logarithmic … S-curve … slow at ends but
    //    quite fast in middle"). Continuous drain, recovery, and each sighting jolt ride RateScale(nerve). ──

    [Fact]
    public void RateScale_IsSlowAtBothEnds_FastestInTheMiddle()
    {
        // The shape the owner asked for: a floored parabola, peaking at exactly 1.0 at mid-gauge and tapering
        // to the floor at both ends. Slowest near full and near empty, fastest through the middle.
        Assert.Equal(1.0, NerveModel.RateScale(50.0), 9);                    // mid-gauge peak
        Assert.Equal(NerveModel.RateFloor, NerveModel.RateScale(0.0), 9);    // empty floor
        Assert.Equal(NerveModel.RateFloor, NerveModel.RateScale(100.0), 9);  // full floor

        // Strictly rising from the floor up to the mid peak, and symmetric across it.
        Assert.True(NerveModel.RateScale(10.0) < NerveModel.RateScale(30.0));
        Assert.True(NerveModel.RateScale(30.0) < NerveModel.RateScale(50.0));
        Assert.Equal(NerveModel.RateScale(30.0), NerveModel.RateScale(70.0), 9); // symmetry about mid
        Assert.Equal(NerveModel.RateScale(10.0), NerveModel.RateScale(90.0), 9);

        // Never below the floor, never above 1.0 — a bounded scale.
        foreach (double n in new[] { 0.0, 5.0, 12.5, 25.0, 40.0, 50.0, 60.0, 75.0, 88.0, 100.0 })
        {
            double r = NerveModel.RateScale(n);
            Assert.InRange(r, NerveModel.RateFloor, 1.0);
        }
    }

    [Fact]
    public void SCurveDrain_TapersHardAtBothEnds_ButNeverFreezes()
    {
        // Same brutal pressure and dt at a near-full, mid, and near-empty gauge: the mid drops the MOST, the
        // ends taper — yet each still moves (the floor keeps "slow" from becoming "frozen", so a captain can
        // still bottom out).
        var pressure = new NerveModel.Stressors(0, ChaseActive: true, Digging: true, Cornered: true);
        double dropNearFull = 95.0 - NerveModel.Drain(95.0, in pressure, 0.5);
        double dropMid = 50.0 - NerveModel.Drain(50.0, in pressure, 0.5);
        double dropNearEmpty = 5.0 - NerveModel.Drain(5.0, in pressure, 0.5);
        Assert.True(dropMid > dropNearFull, "the middle of the gauge slides fastest");
        Assert.True(dropMid > dropNearEmpty, "a near-shot captain frays slower than mid-gauge");
        Assert.True(dropNearFull > 0.0 && dropNearEmpty > 0.0, "the ends are slow, never frozen");
    }

    [Fact]
    public void ShatteredCaptain_RecoversSlowly_ThenFasterThroughTheMiddle()
    {
        // "A shattered one is slow to mend." From the floor, the first steps back are the smallest; the same
        // aboard easing over the same dt lifts a mid-gauge captain far more than a shot one.
        double liftFromFloor = NerveModel.Recover(2.0, 1.0) - 2.0;
        double liftFromMid = NerveModel.Recover(50.0, 1.0) - 50.0;
        double liftNearFull = NerveModel.Recover(96.0, 1.0) - 96.0;
        Assert.True(liftFromFloor < liftFromMid, "a shattered captain mends slower than a mid-gauge one");
        Assert.True(liftNearFull < liftFromMid, "and settles gently as it nears steady again");
        Assert.True(liftFromFloor > 0.0, "but a shattered captain does still mend — the floor is not a wall");

        // A shattered captain climbing out never overshoots the clamp, and the trajectory is monotonic up.
        double n = 0.0, prev = -1.0;
        for (int i = 0; i < 400; i++)
        {
            double next = NerveModel.Recover(n, 1.0);
            Assert.True(next >= n, "recovery is monotonic — never a dip");
            Assert.InRange(next, NerveModel.Min, NerveModel.Max); // no overshoot past the clamp
            prev = n;
            n = next;
        }
        Assert.Equal(NerveModel.Max, n, 6); // it does, eventually, reach steady hands again
        Assert.True(prev <= NerveModel.Max);
    }

    // ── #379 · Diminishing SIGHTINGS (Evening wind #18): first fresh contact full, subsequent within the
    //    spell a fraction, resetting after the tracker is quiet a while. ──

    [Fact]
    public void SightingSeriesCost_FirstFrightFull_EachRepeatDecays()
    {
        // The first fresh sighting of a fresh spell costs the full shock; the second a decay-fraction of it;
        // the third that again — a geometric run.
        Assert.Equal(NerveModel.SightingShock, NerveModel.SightingSeriesCost(priorSeen: 0, freshCount: 1), 6);
        Assert.Equal(NerveModel.SightingShock * NerveModel.SightingDecay,
            NerveModel.SightingSeriesCost(priorSeen: 1, freshCount: 1), 6);
        Assert.Equal(NerveModel.SightingShock * NerveModel.SightingDecay * NerveModel.SightingDecay,
            NerveModel.SightingSeriesCost(priorSeen: 2, freshCount: 1), 6);

        // A batch of fresh contacts is the SUM of that run — and three-at-once equals three-in-a-row.
        double batchOfThree = NerveModel.SightingSeriesCost(0, 3);
        double oneByOne =
            NerveModel.SightingSeriesCost(0, 1)
            + NerveModel.SightingSeriesCost(1, 1)
            + NerveModel.SightingSeriesCost(2, 1);
        Assert.Equal(oneByOne, batchOfThree, 6);

        // The whole spell's jolts are bounded — a swarm can never flood the gauge (owner #18).
        double wholeSpellCap = NerveModel.SightingShock / (1.0 - NerveModel.SightingDecay);
        Assert.True(NerveModel.SightingSeriesCost(0, 999) <= wholeSpellCap + 1e-9);
        Assert.Equal(0.0, NerveModel.SightingSeriesCost(0, 0), 6); // no fresh contacts → no cost
    }

    [Fact]
    public void AdvanceSightings_CountsRises_AndResetsAfterSustainedQuiet()
    {
        var spell = NerveModel.SightingSpell.Fresh;

        // First frame with one mover heard: a fresh contact crests.
        (spell, int fresh1) = NerveModel.AdvanceSightings(spell, movingContacts: 1, dtSeconds: 0.1);
        Assert.Equal(1, fresh1);
        Assert.Equal(1, spell.Seen);

        // The same lone mover still there next frame: no NEW contact, no fresh jolt.
        (spell, int freshHold) = NerveModel.AdvanceSightings(spell, movingContacts: 1, dtSeconds: 0.1);
        Assert.Equal(0, freshHold);
        Assert.Equal(1, spell.Seen);

        // Two more crest at once: two fresh contacts, the tally climbs to three.
        (spell, int fresh2) = NerveModel.AdvanceSightings(spell, movingContacts: 3, dtSeconds: 0.1);
        Assert.Equal(2, fresh2);
        Assert.Equal(3, spell.Seen);

        // A brief lull (shorter than the reset window) does NOT wipe the watch's habituation.
        (spell, _) = NerveModel.AdvanceSightings(spell, movingContacts: 0, dtSeconds: NerveModel.SightingQuietResetSeconds / 2.0);
        Assert.Equal(3, spell.Seen);

        // Sustained quiet past the window ends the spell — the tally resets to a fresh fright.
        (spell, _) = NerveModel.AdvanceSightings(spell, movingContacts: 0, dtSeconds: NerveModel.SightingQuietResetSeconds);
        Assert.Equal(0, spell.Seen);

        // And now the next mover is a FULL fright again.
        (spell, int freshAfterReset) = NerveModel.AdvanceSightings(spell, movingContacts: 1, dtSeconds: 0.1);
        Assert.Equal(1, freshAfterReset);
        Assert.Equal(NerveModel.SightingShock, NerveModel.SightingSeriesCost(0, freshAfterReset), 6);
    }

    [Fact]
    public void SightingDrain_RidesTheSCurve_AndDiminishesAcrossASpell()
    {
        // The same fresh sighting hurts LESS at the steady end than mid-gauge (the S-curve), and a later
        // fright in the spell hurts less than the first (the diminishing) — both laws stacked.
        double firstAtMid = 50.0 - NerveModel.SightingDrain(50.0, priorSeen: 0, freshCount: 1);
        double firstNearFull = 95.0 - NerveModel.SightingDrain(95.0, priorSeen: 0, freshCount: 1);
        double laterAtMid = 50.0 - NerveModel.SightingDrain(50.0, priorSeen: 3, freshCount: 1);
        Assert.True(firstNearFull < firstAtMid, "a steady captain shrugs off the first fright");
        Assert.True(laterAtMid < firstAtMid, "a later fright of the spell hurts less than the first");
        Assert.InRange(NerveModel.SightingDrain(0.0, 0, 5), NerveModel.Min, NerveModel.Max); // clamps, no overshoot
    }

    // ── #379 · TOUCH (Evening wind #19: "if they get to skin, that is a different thing"). ──

    [Fact]
    public void Touch_IsABigFlatLump_ThatBypassesTheDiminishAndTheSCurve()
    {
        // Touch is not a sighting: it does not diminish, and it does not ride the S-curve. The same grab
        // costs the SAME flat lump whether the captain is steady or shattered — it always hurts.
        double fromSteady = 100.0 - NerveModel.Shock(100.0, NerveModel.TouchShock);
        double fromMid = 50.0 - NerveModel.Shock(50.0, NerveModel.TouchShock);
        Assert.Equal(NerveModel.TouchShock, fromSteady, 6);
        Assert.Equal(NerveModel.TouchShock, fromMid, 6);

        // A touch bites harder than a whole spell's worth of diminishing sightings — skin is a different thing.
        double wholeSpellOfSightings = NerveModel.SightingSeriesCost(0, 999);
        Assert.True(NerveModel.TouchShock > wholeSpellOfSightings,
            "a hand on you outweighs any run of mere sightings");

        // Repeated touches keep costing the same — habituation never dulls being grabbed (no decay).
        double after = NerveModel.Shock(60.0, NerveModel.TouchShock);
        Assert.Equal(60.0 - NerveModel.TouchShock, after, 6);
        Assert.Equal(after - NerveModel.TouchShock, NerveModel.Shock(after, NerveModel.TouchShock), 6);
    }
}
