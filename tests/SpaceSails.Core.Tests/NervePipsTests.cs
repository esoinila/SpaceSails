namespace SpaceSails.Core.Tests;

/// <summary>#480 · The nerve is quantized. Owner, 2026-07-28: <i>"the sanity events should be quantized …
/// what caused the sanity loss and what we did to regain it. Now it is vague and wishy-washy."</i> These
/// pin the two promises: every change is a WHOLE pip, and every change carries a NAME.</summary>
public class NervePipsTests
{
    private static NervePips.Frame Regolith(
        NerveModel.Stressors s, double dt, bool touched = false, int fresh = 0, bool monolith = false) =>
        new(OnExcursion: true, OnRegolith: true, SeesMonolith: monolith, Stressors: s,
            FreshSightings: fresh, Touched: touched, DtSeconds: dt);

    private static NerveModel.Stressors Chased(double range, bool cornered = false, bool digging = false) =>
        new(MovingContacts: 1, ChaseActive: true, Digging: digging, Cornered: cornered,
            NearestContactRange: range);

    // ── The lattice ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheTrackIsTenWholePips()
    {
        Assert.Equal(10, NervePips.MaxPips);
        Assert.Equal(NerveModel.Max, NervePips.FromPips(NervePips.MaxPips));
        Assert.Equal(NerveModel.Min, NervePips.FromPips(NervePips.MinPips));
        Assert.Equal(NervePips.MaxPips, NervePips.PipsOf(NerveModel.Max));
    }

    [Fact]
    public void ALegacySaveValueSnapsOntoTheLattice_AndNeverDriftsAgain()
    {
        // A voyage saved before #480 carries an arbitrary float. It must read as a clean pip.
        double snapped = NervePips.Snap(63.4);

        Assert.Equal(NervePips.FromPips(6), snapped);
        Assert.Equal(snapped, NervePips.Snap(snapped)); // idempotent
    }

    // ── Sustained pressure BEATS rather than drains ───────────────────────────────────────────────────

    [Fact]
    public void ACloseHunterSpendsNothingUntilTheBeatCompletes_ThenExactlyOnePip()
    {
        double nerve = NerveModel.Max;
        var beats = NervePips.Beats.Fresh;

        // Half a beat of being hunted: the clock runs, but no pip has been earned yet.
        NervePips.Step half = NervePips.Advance(
            nerve, false, beats, Regolith(Chased(range: 2.0), NervePips.CloseBeatSeconds / 2));
        Assert.Equal(nerve, half.Nerve);
        Assert.Empty(half.Events);

        // The rest of the beat: exactly one pip, once, with its name on it.
        NervePips.Step full = NervePips.Advance(
            half.Nerve, false, half.Beats, Regolith(Chased(2.0), NervePips.CloseBeatSeconds / 2));
        Assert.Equal(NervePips.PipsOf(nerve) - 1, NervePips.PipsOf(full.Nerve));
        NervePips.Event e = Assert.Single(full.Events);
        Assert.Equal(NervePips.Cause.Close, e.Cause);
        Assert.Equal(-1, e.Delta);
        Assert.Equal("it is right there", e.Label);
    }

    [Fact]
    public void DistanceGatesTheBeatEntirely_AFarHunterIsScenery()
    {
        // #446 survives quantization: beyond the dread range the clock does not even run.
        double far = NerveModel.DreadRangeDeckUnits + 1.0;
        NervePips.Step s = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh, Regolith(Chased(far), dt: 60.0));

        Assert.Equal(NerveModel.Max, s.Nerve);
        Assert.Empty(s.Events);
        Assert.Equal(0.0, s.Beats.Close); // and nothing banked while it was scenery
    }

    [Fact]
    public void PressureDoesNotBankBetweenEncounters()
    {
        // Most of a beat's worth of being hunted, then the hunter leaves. The part-beat must not sit
        // waiting to ambush the next encounter with a free pip.
        NervePips.Step built = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh,
            Regolith(Chased(2.0), NervePips.CloseBeatSeconds * 0.9));
        Assert.True(built.Beats.Close > 0);

        var empty = new NerveModel.Stressors(0, false, false, false, double.PositiveInfinity);
        NervePips.Step cleared = NervePips.Advance(built.Nerve, false, built.Beats, Regolith(empty, 0.1));

        Assert.Equal(0.0, cleared.Beats.Close);
        Assert.Empty(cleared.Events);
    }

    [Fact]
    public void DiggingOnlyBitesOnceSomethingIsNearEnoughToReachYou()
    {
        // A calm dig on empty ground, or with the tide on the far rim, costs nothing — the shovel is only
        // frightening when you cannot afford to finish it.
        var farDig = new NerveModel.Stressors(
            MovingContacts: 1, ChaseActive: true, Digging: true, Cornered: false,
            NearestContactRange: NerveModel.DreadRangeDeckUnits + 1);

        NervePips.Step far = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh, Regolith(farDig, dt: 60.0));
        Assert.Empty(far.Events);

        NervePips.Step near = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh,
            Regolith(Chased(2.0, digging: true), NervePips.DigBeatSeconds));
        Assert.Contains(near.Events, e => e.Cause == NervePips.Cause.DigUnderThreat);
    }

    [Fact]
    public void BeingCorneredIsCloseByDefinition_AndIsNeverDiscountedByRange()
    {
        // A net between you and the tube mouth is not less frightening because the nearest one is a few
        // units out — cornered is the one pressure range must never gate away.
        var cornered = new NerveModel.Stressors(
            MovingContacts: 0, ChaseActive: false, Digging: false, Cornered: true,
            NearestContactRange: NerveModel.DreadRangeDeckUnits + 5);

        NervePips.Step s = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh,
            Regolith(cornered, NervePips.CorneredBeatSeconds));

        NervePips.Event e = Assert.Single(s.Events);
        Assert.Equal(NervePips.Cause.Cornered, e.Cause);
    }

    [Fact]
    public void CorneredBeatsFasterThanMerelyBeingHunted()
    {
        // The pressures differ in CADENCE, not in size — one pip each, but cornered earns it sooner.
        Assert.True(NervePips.CorneredBeatSeconds < NervePips.CloseBeatSeconds);
        Assert.Equal(NervePips.BeatPips, NervePips.Cost(NervePips.Cause.Cornered));
        Assert.Equal(NervePips.BeatPips, NervePips.Cost(NervePips.Cause.Close));
    }

    [Fact]
    public void ALongFrameReportsEveryWholeBeat_NoPipIsSilentlyEaten()
    {
        NervePips.Step s = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh,
            Regolith(Chased(2.0), NervePips.CloseBeatSeconds * 3));

        Assert.Equal(3, s.Events.Count);
        Assert.All(s.Events, e => Assert.Equal(NervePips.Cause.Close, e.Cause));
        Assert.Equal(NervePips.PipsOf(NerveModel.Max) - 3, NervePips.PipsOf(s.Nerve));
    }

    // ── Lumps ─────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheFirstHandOfAnEncounterCostsItsPip_AndSaysSo()
    {
        NervePips.Step s = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh,
            Regolith(Chased(1.0), dt: 0.0, touched: true));

        NervePips.Event e = Assert.Single(s.Events);
        Assert.Equal(NervePips.Cause.Touch, e.Cause);
        Assert.Equal(-NervePips.TouchPips, e.Delta);
        Assert.Equal("it laid hands on you", e.Label);
        Assert.Equal(NervePips.MaxPips - NervePips.TouchPips, NervePips.PipsOf(s.Nerve));
        Assert.True(s.Beats.TouchSpent);
    }

    [Fact]
    public void RepeatedStrikesCostNoMoreSanity()
    {
        // Owner: "repeated strikes should not cost more of sanity … we already take medical hit from
        // reever." The blow pips charge for the mauling; nerve charges for being caught, once.
        double nerve = NerveModel.Max;
        var beats = NervePips.Beats.Fresh;
        int touchEvents = 0;

        for (int i = 0; i < 6; i++)
        {
            NervePips.Step s = NervePips.Advance(
                nerve, false, beats, Regolith(Chased(1.0), dt: 0.0, touched: true));
            (nerve, beats) = (s.Nerve, s.Beats);
            touchEvents += s.Events.Count(e => e.Cause == NervePips.Cause.Touch);
        }

        Assert.Equal(1, touchEvents);
        Assert.Equal(NervePips.MaxPips - NervePips.TouchPips, NervePips.PipsOf(nerve));
    }

    [Fact]
    public void GettingClearReArmsTheShock_SoTheNextAmbushLandsAgain()
    {
        NervePips.Step grabbed = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh, Regolith(Chased(1.0), 0.0, touched: true));
        Assert.True(grabbed.Beats.TouchSpent);

        // Back up the tube — clear.
        var safe = new NervePips.Frame(true, OnRegolith: false, false, default, 0, false, 0.1);
        NervePips.Step clear = NervePips.Advance(grabbed.Nerve, false, grabbed.Beats, safe);
        Assert.False(clear.Beats.TouchSpent);

        // Out again, grabbed again: a fresh shock.
        NervePips.Step again = NervePips.Advance(
            clear.Nerve, false, clear.Beats, Regolith(Chased(1.0), 0.0, touched: true));
        Assert.Contains(again.Events, e => e.Cause == NervePips.Cause.Touch);
    }

    [Fact]
    public void NearlyDead_EveryHandIsTerrorAgain()
    {
        // Owner: "but first one yes and running low on health more." The latch stops protecting you
        // exactly when it would matter most — fear tracks mortal danger, not novelty.
        double nerve = NerveModel.Max;
        var beats = NervePips.Beats.Fresh;
        int touchEvents = 0;
        string lastLabel = "";

        for (int i = 0; i < 4; i++)
        {
            NervePips.Step s = NervePips.Advance(
                nerve, false, beats,
                Regolith(Chased(1.0), dt: 0.0, touched: true) with { HealthPipsLeft = 1 });
            (nerve, beats) = (s.Nerve, s.Beats);
            foreach (NervePips.Event e in s.Events.Where(e => e.Cause == NervePips.Cause.Touch))
            {
                touchEvents++;
                lastLabel = e.Label;
            }
        }

        Assert.Equal(4, touchEvents); // every single one, unlike the healthy case above
        Assert.Equal("it has you and you are nearly gone", lastLabel);
    }

    [Fact]
    public void AHealthyCaptainAndADyingOneAreChargedDifferentlyForTheSameSecondStrike()
    {
        var spent = NervePips.Beats.Fresh with { TouchSpent = true };

        NervePips.Step healthy = NervePips.Advance(
            NerveModel.Max, false, spent,
            Regolith(Chased(1.0), 0.0, touched: true) with { HealthPipsLeft = NervePips.LowHealthPips + 1 });
        NervePips.Step dying = NervePips.Advance(
            NerveModel.Max, false, spent,
            Regolith(Chased(1.0), 0.0, touched: true) with { HealthPipsLeft = NervePips.LowHealthPips });

        Assert.Empty(healthy.Events);
        Assert.Single(dying.Events);
    }

    [Fact]
    public void TheMonolithFiresOnceInALife()
    {
        NervePips.Step first = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh,
            Regolith(Chased(double.PositiveInfinity), 0.0, monolith: true));

        Assert.True(first.MonolithSeen);
        Assert.Contains(first.Events, e => e.Cause == NervePips.Cause.Monolith);

        NervePips.Step again = NervePips.Advance(
            first.Nerve, first.MonolithSeen, first.Beats,
            Regolith(Chased(double.PositiveInfinity), 0.0, monolith: true));

        Assert.DoesNotContain(again.Events, e => e.Cause == NervePips.Cause.Monolith);
        Assert.Equal(first.Nerve, again.Nerve);
    }

    [Fact]
    public void AFreshSightingCostsOnePip_HabituationIsFree()
    {
        // #379's diminishing series, in whole pips: the first fright of a spell costs, the rest are free.
        NervePips.Step s = NervePips.Advance(
            NerveModel.Max, false, NervePips.Beats.Fresh,
            Regolith(Chased(double.PositiveInfinity), 0.0, fresh: 4));

        NervePips.Event e = Assert.Single(s.Events);
        Assert.Equal(NervePips.Cause.Sighting, e.Cause);
        Assert.Equal(-NervePips.SightingPips, e.Delta);
    }

    // ── Recovery is as legible as the loss ────────────────────────────────────────────────────────────

    [Fact]
    public void TheAirlockGivesPipsBackOneBeatAtATime_AndNamesEachOne()
    {
        double hurt = NervePips.FromPips(4);
        var safe = new NervePips.Frame(
            OnExcursion: true, OnRegolith: false, SeesMonolith: false,
            Stressors: default, FreshSightings: 0, Touched: false,
            DtSeconds: NervePips.AirlockBeatSeconds);

        NervePips.Step s = NervePips.Advance(hurt, false, NervePips.Beats.Fresh, safe);

        NervePips.Event e = Assert.Single(s.Events);
        Assert.Equal(NervePips.Cause.Airlock, e.Cause);
        Assert.Equal(+1, e.Delta);
        Assert.Equal("the airlock closes behind you", e.Label);
        Assert.Equal(5, NervePips.PipsOf(s.Nerve));
    }

    [Fact]
    public void RunningForTheTubeIsNotAResetButton()
    {
        // Recovery must be slower than the sharpest loss, or the tube mouth is a free heal.
        Assert.True(NervePips.AirlockBeatSeconds > NervePips.CorneredBeatSeconds);
    }

    // ── Sub-pip pressure banks rather than vanishing or inflating ─────────────────────────────────────

    [Fact]
    public void APrickleBanksInsteadOfMoving_ThenTipsAWholePipWithTheNameThatTippedIt()
    {
        // A hull chill is a fraction of a pip. Rounding it up would hit as hard as being hunted; rounding
        // it down would delete the feature. It banks.
        double small = NervePips.PipUnit / 4;
        double nerve = NerveModel.Max;
        double carry = 0.0;

        for (int i = 0; i < 3; i++)
        {
            (nerve, carry, NervePips.Event? none) = NervePips.ApplyShock(nerve, carry, small, "a cold breath");
            Assert.Null(none);
            Assert.Equal(NerveModel.Max, nerve); // nothing visible has happened yet
        }

        (nerve, carry, NervePips.Event? tipped) = NervePips.ApplyShock(nerve, carry, small, "a cold breath");

        Assert.NotNull(tipped);
        Assert.Equal(-1, tipped!.Value.Delta);
        Assert.Equal("a cold breath", tipped.Value.Label);       // the one that tipped it gets named
        Assert.Equal(NervePips.MaxPips - 1, NervePips.PipsOf(nerve));
        Assert.True(carry < NervePips.PipUnit);                   // and the remainder keeps banking
    }

    [Fact]
    public void ShocksNameThemselves_SoTheEnumNeverGrowsPerEvent()
    {
        (_, _, NervePips.Event? e) = NervePips.ApplyShock(
            NerveModel.Max, 0.0, NervePips.PipUnit * 2, "the charge goes in on a falling mountain");

        Assert.NotNull(e);
        Assert.Equal(NervePips.Cause.Shock, e!.Value.Cause);
        Assert.Equal("the charge goes in on a falling mountain", e.Value.Label);
        Assert.Equal(-2, e.Value.Delta);
    }

    // ── Relief ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ARestoreTooSmallForAWholePipChangesNothing_AndSaysNothing()
    {
        double nerve = NervePips.FromPips(5);
        (double after, NervePips.Event? e) = NervePips.ApplyRelief(nerve, NervePips.PipUnit - 0.01);

        Assert.Equal(nerve, after);
        Assert.Null(e); // "barely a flicker" must not silently move the gauge
    }

    [Fact]
    public void AGoodNightsSleepGivesWholePipsBack()
    {
        double nerve = NervePips.FromPips(3);
        (double after, NervePips.Event? e) = NervePips.ApplyRelief(nerve, NerveModel.SleepRestore);

        Assert.NotNull(e);
        Assert.Equal(NervePips.Cause.Relief, e!.Value.Cause);
        Assert.True(e.Value.Delta > 0);
        Assert.Equal(3 + (int)(NerveModel.SleepRestore / NervePips.PipUnit), NervePips.PipsOf(after));
    }

    // ── The ledger ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheLedgerKeepsTheNewestEventsFirst_Bounded()
    {
        IReadOnlyList<NervePips.Event> ledger = [];
        for (int i = 0; i < NervePips.LedgerDepth + 4; i++)
        {
            ledger = NervePips.Record(ledger, [new NervePips.Event(NervePips.Cause.Close, -1, $"beat {i}")]);
        }

        Assert.Equal(NervePips.LedgerDepth, ledger.Count);
        Assert.Equal($"beat {NervePips.LedgerDepth + 3}", ledger[0].Label);   // newest first
        Assert.Equal($"beat {4}", ledger[^1].Label);                          // oldest survivor
    }

    [Fact]
    public void RecordingNothingLeavesTheLedgerAlone()
    {
        IReadOnlyList<NervePips.Event> ledger = [new NervePips.Event(NervePips.Cause.Touch, -3)];
        Assert.Same(ledger, NervePips.Record(ledger, []));
    }

    [Fact]
    public void EveryEventLineNamesACauseAndASignedPip()
    {
        Assert.Equal("it laid hands on you  −3", new NervePips.Event(NervePips.Cause.Touch, -3).Line);
        Assert.Equal("the airlock closes behind you  +1", new NervePips.Event(NervePips.Cause.Airlock, 1).Line);
    }

    [Fact]
    public void EveryCauseHasWordsToSay()
    {
        // The whole point of #480: nothing may move the gauge anonymously.
        foreach (NervePips.Cause c in System.Enum.GetValues<NervePips.Cause>())
        {
            Assert.False(string.IsNullOrWhiteSpace(NervePips.Name(c)), $"{c} has no words");
        }
    }

    // ── Determinism ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SameStateAndSameFrameAlwaysYieldTheSameStep()
    {
        var f = Regolith(Chased(2.0, cornered: true, digging: true), 4.0, touched: true, fresh: 2);

        NervePips.Step a = NervePips.Advance(NerveModel.Max, false, NervePips.Beats.Fresh, f);
        NervePips.Step b = NervePips.Advance(NerveModel.Max, false, NervePips.Beats.Fresh, f);

        Assert.Equal(a.Nerve, b.Nerve);
        Assert.Equal(a.Events.Count, b.Events.Count);
        Assert.Equal(a.Beats, b.Beats);
    }

    [Fact]
    public void TheNerveNeverLeavesTheLatticeNorTheGauge()
    {
        double nerve = NerveModel.Max;
        var beats = NervePips.Beats.Fresh;
        bool seen = false;

        // Hammer it: cornered, digging, touched, every frame, well past the floor.
        for (int i = 0; i < 200; i++)
        {
            NervePips.Step s = NervePips.Advance(
                nerve, seen, beats, Regolith(Chased(1.0, cornered: true, digging: true), 1.0, touched: true));
            (nerve, beats, seen) = (s.Nerve, s.Beats, s.MonolithSeen);

            Assert.InRange(nerve, NerveModel.Min, NerveModel.Max);
            Assert.Equal(nerve, NervePips.Snap(nerve));  // never off the lattice
        }

        Assert.Equal(NervePips.MinPips, NervePips.PipsOf(nerve));
    }
}
