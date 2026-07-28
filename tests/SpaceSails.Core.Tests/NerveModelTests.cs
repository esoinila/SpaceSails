namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-317 · The nerve gauge — the first slice of #226's Fail Forward sanity.
///
/// <para>#480 moved the per-frame LAW out of here and into <see cref="NervePips"/>: sustained pressure now
/// beats in whole pips instead of draining at a float rate, so the drain/recover/S-curve/Advance tests that
/// used to live here are gone with the code they pinned — <c>NervePipsTests</c> covers the same ground in
/// the new units. What remains is what quantization did not touch: the dread RANGE (which now gates the
/// beat), the sighting spell's bookkeeping, the relief economy, and the display ladder — whose rungs now sit
/// on pip boundaries so a band can only ever change on a whole pip.</para>
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
    public void MonolithShock_IsOneBigLump()
    {
        double after = NerveModel.Shock(100.0, NerveModel.MonolithSightShock);
        Assert.Equal(100.0 - NerveModel.MonolithSightShock, after, 6);
    }

    [Theory]
    [InlineData(100.0, NerveModel.NerveBand.Steady)]   // 10 pips
    [InlineData(80.0, NerveModel.NerveBand.Steady)]    // 8
    [InlineData(70.0, NerveModel.NerveBand.Rattled)]   // 7
    [InlineData(60.0, NerveModel.NerveBand.Rattled)]   // 6
    [InlineData(50.0, NerveModel.NerveBand.Shaken)]    // 5
    [InlineData(40.0, NerveModel.NerveBand.Shaken)]    // 4
    [InlineData(30.0, NerveModel.NerveBand.Fraying)]   // 3
    [InlineData(20.0, NerveModel.NerveBand.Fraying)]   // 2
    [InlineData(10.0, NerveModel.NerveBand.Shot)]      // 1 — one pip left is already shot
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

    // ── The relief economy (#308/#321): shared drinks are flat and level-independent, a lone drink is weak
    //    medicine that fades to a single point at the floor; diminishing repeat and the drunk gate ride on
    //    top. #480 rounds whatever this prices onto the pip lattice at the point of use. ──

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

    // ── #446 · Distance is the whole story. Survives #480 unchanged in meaning, but now it GATES the beat
    //    rather than scaling an amount: beyond the range an Old One is scenery and the clock never runs. ──

    [Fact]
    public void Dread_IsZeroBeyondRange_FullWhenNearlyOnYou_AndRampsBetween()
    {
        Assert.Equal(0.0, NerveModel.Dread(double.PositiveInfinity), 6);              // empty ground
        Assert.Equal(0.0, NerveModel.Dread(NerveModel.DreadRangeDeckUnits), 6);       // exactly at the rim
        Assert.Equal(0.0, NerveModel.Dread(NerveModel.DreadRangeDeckUnits + 20), 6);  // well beyond it
        Assert.Equal(1.0, NerveModel.Dread(NerveModel.DreadFullRangeDeckUnits), 6);   // at the full mark
        Assert.Equal(1.0, NerveModel.Dread(0.0), 6);                                  // right on top of you

        // In between it is a ramp, not a cliff — walking toward a hunter must feel like mounting pressure.
        double mid = (NerveModel.DreadRangeDeckUnits + NerveModel.DreadFullRangeDeckUnits) / 2.0;
        Assert.InRange(NerveModel.Dread(mid), 0.4, 0.6);
        Assert.True(NerveModel.Dread(mid - 1) > NerveModel.Dread(mid), "closing must always frighten more");
    }
}
