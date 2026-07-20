using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #394 — THE ASTEROID DEFLECTION (owner: "asteroid deflection Armageddon movie style 🫡😎"; target ruling
/// 2026-07-20: "the asteroid must NEVER threaten Earth … Ringside Exchange"; type ruling 2026-07-20:
/// Zubrin's taxonomy "would definitely be a factor"). These pin the pure Core spine: the rock's colliding
/// Kepler rail and the honest miss math (undeflected = a hit; a periapsis raise of ΔR yields a miss of ΔR),
/// the deflection clearing the intersect, the grazing band, the doom-clock window, the diced complication
/// determinism, the seeded rock TYPE and its honestly-costed drill/ablation constants, the heroic pay
/// bands, and the per-universe plaque-gratitude append.
/// </summary>
public class DeflectionGigTests
{
    // The canonical target: the Ringside Exchange on its real circular orbit around Saturn (scenarios/sol.json).
    private const double RingRadius = 1.35e9;
    private const double RingPeriod = 1600600.0;
    private const double RingPhase = 5.0;
    private const double Impact = DeflectionGig.RailLeadSeconds; // accept at sim-time 0 → impact at the lead

    private static DeflectionGig.RockRail Rail() =>
        DeflectionGig.BuildRail(RingRadius, RingPeriod, RingPhase, Impact);

    private static double Miss(DeflectionGig.RockRail rail) =>
        DeflectionGig.MissDistanceMeters(rail, RingRadius, RingPeriod, RingPhase, Impact);

    // ── The rail: a genuine collision course, drawable on the Kepler rails ──

    [Fact]
    public void UndeflectedRail_Collides_MissIsEssentiallyZero()
    {
        double miss = Miss(Rail());
        Assert.True(miss < 1.0e4, $"undeflected rock should hit; miss was {miss:e3} m");
        Assert.Equal(DeflectionOutcome.Impact, DeflectionGig.Classify(miss));
    }

    [Fact]
    public void UndeflectedRail_PeriapsisKissesTheStationsOrbit()
    {
        // Periapsis sits exactly on the target's orbit radius — the drawn rail touches the ring at the ⚠.
        Assert.Equal(RingRadius, Rail().PeriapsisMeters, 1e-3 * RingRadius);
    }

    [Fact]
    public void BuildRail_IsDeterministic()
    {
        Assert.Equal(Rail(), Rail());
    }

    // ── The deflection: raise periapsis → the miss is the raise (the map shows the rail lift off) ──

    [Theory]
    [InlineData(5.0e6)]
    [InlineData(1.5e7)]
    [InlineData(3.0e7)]
    [InlineData(6.0e7)]
    public void RaisingPeriapsis_ProducesMissEqualToTheRaise(double raise)
    {
        DeflectionGig.RockRail deflected = DeflectionGig.RaisePeriapsis(Rail(), raise);
        double miss = Miss(deflected);
        // The closest approach after a periapsis raise of ΔR is ΔR (pinned to a tight tolerance).
        Assert.Equal(raise, miss, 0.02 * raise + 1.0e4);
        // And the rail visibly lifts: periapsis is now above the station's orbit by the raise.
        Assert.Equal(RingRadius + raise, deflected.PeriapsisMeters, 1e-3 * RingRadius);
    }

    [Fact]
    public void FullDeflection_ClearsTheIntersect()
    {
        DeflectionGig.RockRail deflected = DeflectionGig.RaisePeriapsis(Rail(), DeflectionGig.SafeMissMeters + 1e6);
        Assert.Equal(DeflectionOutcome.FullDeflection, DeflectionGig.Classify(Miss(deflected)));
    }

    [Fact]
    public void GrazingBand_SitsBetweenGrazeAndSafe()
    {
        double mid = 0.5 * (DeflectionGig.GrazeMissMeters + DeflectionGig.SafeMissMeters);
        DeflectionGig.RockRail deflected = DeflectionGig.RaisePeriapsis(Rail(), mid);
        Assert.Equal(DeflectionOutcome.GrazingMiss, DeflectionGig.Classify(Miss(deflected)));
    }

    [Theory]
    [InlineData(0.0, DeflectionOutcome.Impact)]
    [InlineData(4.0e6, DeflectionOutcome.Impact)]      // below graze
    [InlineData(8.0e6, DeflectionOutcome.GrazingMiss)] // exactly graze
    [InlineData(2.0e7, DeflectionOutcome.GrazingMiss)]
    [InlineData(3.0e7, DeflectionOutcome.FullDeflection)] // exactly safe
    [InlineData(5.0e7, DeflectionOutcome.FullDeflection)]
    public void Classify_Bands(double miss, DeflectionOutcome expected) =>
        Assert.Equal(expected, DeflectionGig.Classify(miss));

    // ── The doom clock: a real-seconds budget from accept to impact ──

    [Fact]
    public void SecondsToImpact_CountsDownAndFloorsAtZero()
    {
        Assert.Equal(DeflectionGig.ImpactBudgetSeconds, DeflectionGig.SecondsToImpact(0.0));
        Assert.Equal(0.0, DeflectionGig.SecondsToImpact(DeflectionGig.ImpactBudgetSeconds + 50.0));
        Assert.Equal(DeflectionGig.ImpactBudgetSeconds - 100.0, DeflectionGig.SecondsToImpact(100.0));
    }

    [Fact]
    public void ClassifyClock_CountingThenLastCallThenImpact()
    {
        Assert.Equal(ImpactClock.Counting, DeflectionGig.ClassifyClock(0.0));
        Assert.Equal(ImpactClock.LastCall,
            DeflectionGig.ClassifyClock(DeflectionGig.ImpactBudgetSeconds - (DeflectionGig.CriticalSeconds / 2.0)));
        Assert.Equal(ImpactClock.Impact, DeflectionGig.ClassifyClock(DeflectionGig.ImpactBudgetSeconds));
    }

    // ── The complications: diced, deterministic, no pack ──

    [Fact]
    public void Complication_IsDeterministicInTheSeed()
    {
        var type = new RockType(RockComposition.SType, RockStructure.Monolith);
        ulong seed = DeflectionGig.Seed(1234.0, DeflectionGig.BodyId, ordinal: 2);
        DeflectionComplication a = DeflectionGig.Roll(seed, type, 2);
        DeflectionComplication b = DeflectionGig.Roll(seed, type, 2);
        // (The embedded DicePool holds arrays that compare by reference, so pin the meaningful fields.)
        Assert.Equal(a.Band, b.Band);
        Assert.Equal(a.NerveHit, b.NerveHit);
        Assert.Equal(a.DrillProgressDelta, b.DrillProgressDelta);
        Assert.Equal(a.CrewLost, b.CrewLost);
        Assert.Equal(a.Event.Headline, b.Event.Headline);
        Assert.Equal(a.Event.Detail, b.Event.Detail);
    }

    [Fact]
    public void Complication_SeedsAreIndependentPerOrdinal()
    {
        double accepted = 4242.0;
        var seeds = Enumerable.Range(0, 8)
            .Select(o => DeflectionGig.Seed(accepted, DeflectionGig.BodyId, o)).ToList();
        Assert.Equal(seeds.Count, seeds.Distinct().Count());
    }

    [Fact]
    public void EpisodesElapsed_OnePerCadence()
    {
        Assert.Equal(0, DeflectionGig.EpisodesElapsed(0.0));
        Assert.Equal(0, DeflectionGig.EpisodesElapsed(DeflectionGig.EventCadenceSeconds - 0.1));
        Assert.Equal(1, DeflectionGig.EpisodesElapsed(DeflectionGig.EventCadenceSeconds));
        Assert.Equal(3, DeflectionGig.EpisodesElapsed((DeflectionGig.EventCadenceSeconds * 3) + 1));
    }

    [Fact]
    public void Complications_CoverEveryBand_OverManyRolls_AndNeverRousePack()
    {
        var type = new RockType(RockComposition.SType, RockStructure.Monolith);
        var seen = new HashSet<DeflectionBand>();
        for (int o = 0; o < 400; o++)
        {
            DeflectionComplication c = DeflectionGig.Roll(DeflectionGig.Seed(o, DeflectionGig.BodyId, o), type, o);
            seen.Add(c.Band);
            Assert.Equal(DeflectionGig.Source, c.Event.Source); // the tray caption is always DEFLECTION
        }
        foreach (DeflectionBand band in Enum.GetValues<DeflectionBand>())
        {
            Assert.Contains(band, seen);
        }
    }

    [Fact]
    public void DrillSnap_SetsBitBack_GoodBite_Gains()
    {
        // A drill snap always carries a negative progress delta; a good bite a positive one.
        var mType = new RockType(RockComposition.MType, RockStructure.Monolith);
        bool sawSnap = false, sawGain = false;
        for (int o = 0; o < 400 && !(sawSnap && sawGain); o++)
        {
            DeflectionComplication c = DeflectionGig.Roll(DeflectionGig.Seed(o, "r", o), mType, o);
            if (c.Band == DeflectionBand.DrillSnap) { Assert.True(c.DrillProgressDelta < 0); sawSnap = true; }
            if (c.Band == DeflectionBand.GoodBite) { Assert.True(c.DrillProgressDelta > 0); sawGain = true; }
        }
        Assert.True(sawSnap && sawGain);
    }

    // ── The rock TYPE (owner 2026-07-20): honestly costed drill + ablation ──

    [Fact]
    public void RollType_IsDeterministic_AndSpansAllCombinations()
    {
        Assert.Equal(DeflectionGig.RollType(99), DeflectionGig.RollType(99));
        var comps = new HashSet<RockComposition>();
        var structs = new HashSet<RockStructure>();
        for (ulong s = 0; s < 300; s++)
        {
            RockType t = DeflectionGig.RollType(s);
            comps.Add(t.Composition);
            structs.Add(t.Structure);
        }
        Assert.Equal(3, comps.Count);   // C, S and M all appear
        Assert.Equal(2, structs.Count); // monolith and rubble pile both appear
    }

    [Fact]
    public void DrillTime_HardensCtoStoM_AndRubbleIsSlower()
    {
        double c = DeflectionGig.RockProfile.DrillSeconds(new(RockComposition.CType, RockStructure.Monolith));
        double s = DeflectionGig.RockProfile.DrillSeconds(new(RockComposition.SType, RockStructure.Monolith));
        double m = DeflectionGig.RockProfile.DrillSeconds(new(RockComposition.MType, RockStructure.Monolith));
        Assert.True(c < s && s < m, $"C {c} < S {s} < M {m}");

        double sMono = DeflectionGig.RockProfile.DrillSeconds(new(RockComposition.SType, RockStructure.Monolith));
        double sRubble = DeflectionGig.RockProfile.DrillSeconds(new(RockComposition.SType, RockStructure.RubblePile));
        Assert.True(sRubble > sMono, "a rubble pile drills slower");
    }

    [Fact]
    public void AblationEfficiency_EagerCtoResistantM_AndRubbleSmears()
    {
        double c = DeflectionGig.RockProfile.AblationEfficiency(new(RockComposition.CType, RockStructure.Monolith));
        double s = DeflectionGig.RockProfile.AblationEfficiency(new(RockComposition.SType, RockStructure.Monolith));
        double m = DeflectionGig.RockProfile.AblationEfficiency(new(RockComposition.MType, RockStructure.Monolith));
        Assert.True(c > s && s > m, $"C {c} > S {s} > M {m}");

        double mMono = DeflectionGig.RockProfile.AblationEfficiency(new(RockComposition.MType, RockStructure.Monolith));
        double mRubble = DeflectionGig.RockProfile.AblationEfficiency(new(RockComposition.MType, RockStructure.RubblePile));
        Assert.True(mRubble < mMono, "a rubble pile smears the impulse — the hardest case");
    }

    [Fact]
    public void WorstRock_JustClears_OnAFlawlessRun_ButShortfallOnlyGrazes()
    {
        var worst = new RockType(RockComposition.MType, RockStructure.RubblePile);

        // A full charge, perfectly aligned, JUST clears the station (owner: "bring a bigger charge").
        double flawless = DeflectionGig.PeriapsisRaiseForBurn(worst, chargeFraction: 1.0, rotationAlignment: 1.0);
        Assert.Equal(DeflectionOutcome.FullDeflection, DeflectionGig.Classify(flawless));

        // Anything short of a flawless run only grazes it.
        double short90 = DeflectionGig.PeriapsisRaiseForBurn(worst, chargeFraction: 0.9, rotationAlignment: 1.0);
        Assert.Equal(DeflectionOutcome.GrazingMiss, DeflectionGig.Classify(short90));
    }

    [Fact]
    public void SoftRock_ClearsWithMargin()
    {
        var soft = new RockType(RockComposition.CType, RockStructure.Monolith);
        double raise = DeflectionGig.PeriapsisRaiseForBurn(soft, chargeFraction: 1.0, rotationAlignment: 1.0);
        Assert.True(raise > DeflectionGig.SafeMissMeters * 1.5, "a soft C-type monolith clears wide");
    }

    [Fact]
    public void ZeroCharge_DeliversNothing()
    {
        var t = new RockType(RockComposition.SType, RockStructure.Monolith);
        Assert.Equal(0.0, DeflectionGig.PeriapsisRaiseForBurn(t, 0.0, 1.0));
    }

    [Fact]
    public void RockType_LabelsAndCode()
    {
        var t = new RockType(RockComposition.MType, RockStructure.RubblePile);
        Assert.Equal("M", t.Code);
        Assert.Contains("M-type metallic", t.Label);
        Assert.Contains("rubble pile", t.Label);
        Assert.False(string.IsNullOrWhiteSpace(t.BriefLine));
    }

    // ── Rotation window: the ablation shove only counts when the bore is aligned ──

    [Fact]
    public void RotationAlignment_PeaksAligned_TroughsOpposite_AndClamps01()
    {
        // Phase 0 at t=0 → fully aligned; half a spin later → opposite.
        Assert.Equal(1.0, DeflectionGig.RotationAlignment(10.0, 0.0, 0.0), 1e-9);
        Assert.Equal(0.0, DeflectionGig.RotationAlignment(10.0, 0.0, 5.0), 1e-9);
        double mid = DeflectionGig.RotationAlignment(10.0, 0.0, 2.5);
        Assert.InRange(mid, 0.0, 1.0);
        // A non-spinning rock is always aligned.
        Assert.Equal(1.0, DeflectionGig.RotationAlignment(0.0, 1.23, 99.0));
    }

    // ── The heroic payout: bands, crew docking, floor ──

    [Fact]
    public void Pay_FullBeatsGrazingBeatsFloor()
    {
        int full = DeflectionGig.Total(DeflectionGig.BaseFee, RingRadius, RingRadius, DeflectionOutcome.FullDeflection, 0);
        int graze = DeflectionGig.Total(DeflectionGig.BaseFee, RingRadius, RingRadius, DeflectionOutcome.GrazingMiss, 0);
        int miss = DeflectionGig.Total(DeflectionGig.BaseFee, RingRadius, RingRadius, DeflectionOutcome.Impact, 0);
        Assert.True(full > graze && graze > miss, $"full {full} > graze {graze} > impact {miss}");
        Assert.Equal(DeflectionGig.Floor, miss); // an impact/abort pays only the floor
    }

    [Fact]
    public void Pay_DocksPerCrewLost_ButNeverBelowFloor()
    {
        int clean = DeflectionGig.Total(DeflectionGig.BaseFee, 0, 0, DeflectionOutcome.FullDeflection, 0);
        int oneLost = DeflectionGig.Total(DeflectionGig.BaseFee, 0, 0, DeflectionOutcome.FullDeflection, 1);
        Assert.Equal(clean - DeflectionGig.PerCrewLostPenalty, oneLost);
        // A catastrophe never pays below the floor.
        int wiped = DeflectionGig.Total(DeflectionGig.BaseFee, 0, 0, DeflectionOutcome.GrazingMiss, 99);
        Assert.Equal(DeflectionGig.Floor, wiped);
    }

    [Fact]
    public void Pay_GrowsWithHaulDistance()
    {
        int local = DeflectionGig.Total(DeflectionGig.BaseFee, RingRadius, RingRadius, DeflectionOutcome.FullDeflection, 0);
        int far = DeflectionGig.Total(DeflectionGig.BaseFee, 2.0e11, 3.0e12, DeflectionOutcome.FullDeflection, 0);
        Assert.True(far > local, "a farther haul to the rock pays more");
    }

    // ── The plaque gratitude append (persisted per-universe) ──

    [Fact]
    public void Gratitude_AppendsOnlyToSavedRingside()
    {
        Plaque ringside = Plaques.For("ringside-exchange")!;
        string saved = Plaques.DedicationLore(ringside, ringsideSaved: true, shipName: "Hull No. 77");
        string unsaved = Plaques.DedicationLore(ringside, ringsideSaved: false, shipName: "Hull No. 77");

        Assert.StartsWith(ringside.Lore, saved);              // the original bronze stays
        Assert.Contains("Hull No. 77", saved);                // the crew is named
        Assert.True(saved.Length > unsaved.Length);           // the line was appended
        Assert.Equal(ringside.Lore, unsaved);                 // unsaved reads exactly the original
    }

    [Fact]
    public void Gratitude_NeverTouchesOtherPorts_EvenWhenSaved()
    {
        Plaque selene = Plaques.For("selene-gate")!;
        Assert.Equal(selene.Lore, Plaques.DedicationLore(selene, ringsideSaved: true, shipName: "Hull No. 77"));
    }

    [Fact]
    public void RingsideSaved_DefaultsFalse_AndRoundTrips()
    {
        Assert.False(new ProgressSection().RingsideSaved);
        Assert.True((new ProgressSection { RingsideSaved = true }).RingsideSaved);
    }
}
