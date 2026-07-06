namespace SpaceSails.Core.Tests;

/// <summary>
/// PR-7, the gun deck (vision par. 18): weapon envelope, compliance/threats/bribery, heat and
/// the hunters it calls down. Everything here is a pure function of ship-id hashes, sim time and
/// player heat — determinism is law, so most assertions run twice and compare bit-for-bit.
/// </summary>
public class EncounterRuleTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    private static NpcShip Ship(string id, string cargo = "He3", int cargoUnits = 10, bool isPod = false) =>
        new(id, "Test Hauler", cargo, "saturn", "earth", RoutePersonality.Economical,
            DepartureTime: 0, ActivationTime: 0, InitialState: default,
            Plan: new ManeuverPlan([]), EstimatedArrivalTime: 1e6,
            CargoUnits: cargoUnits, ManeuverBudget: NpcShip.DefaultManeuverBudget, IsPod: isPod);

    // ---- weapon envelope ----

    [Fact]
    public void InWeaponRange_ShorterThanBoardingEnvelope()
    {
        Assert.True(EncounterRule.WeaponRangeMeters < CaptureRule.CaptureRadiusMeters);

        var player = new ShipState(Vector2d.Zero, Vector2d.Zero, 0);
        var close = new ShipState(new Vector2d(1e8, 0), Vector2d.Zero, 0);
        var far = new ShipState(new Vector2d(3e8, 0), Vector2d.Zero, 0);

        Assert.True(EncounterRule.InWeaponRange(player, close));
        Assert.False(EncounterRule.InWeaponRange(player, far));
    }

    // ---- compliance ----

    [Fact]
    public void ComplianceOf_Pods_HaveNothingToComply()
    {
        NpcShip pod = Ship("pod-0", isPod: true);
        Assert.Equal(ComplianceState.NothingToComply, EncounterRule.ComplianceOf(pod, playerHeat: 0));
    }

    [Fact]
    public void ComplianceOf_IsDeterministic_ByShipId()
    {
        NpcShip ship = Ship("npc-42");
        ComplianceState first = EncounterRule.ComplianceOf(ship, playerHeat: 1);
        ComplianceState second = EncounterRule.ComplianceOf(ship, playerHeat: 1);
        Assert.Equal(first, second);
    }

    [Fact]
    public void ComplianceOf_AboutOneQuarterStubborn_OverTheStandardCallsignSet()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> fleet = TrafficSchedule.Generate(ephemeris, seed: 1, count: 200);

        int stubborn = fleet.Count(s => EncounterRule.ComplianceOf(s, playerHeat: 0) == ComplianceState.Stubborn);
        double fraction = (double)stubborn / fleet.Count;

        // Deterministic hash split around the 25% baseline — generous band since 200 samples
        // still carries some hash-luck, but it must land nowhere near "everyone" or "no one".
        Assert.InRange(fraction, 0.15, 0.37);
    }

    [Fact]
    public void ComplianceOf_StubbornFraction_RisesWithHeat_ButStaysCapped()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> fleet = TrafficSchedule.Generate(ephemeris, seed: 2, count: 300);

        double FractionAt(int heat) =>
            (double)fleet.Count(s => EncounterRule.ComplianceOf(s, heat) == ComplianceState.Stubborn) / fleet.Count;

        double atZero = FractionAt(0);
        double atMax = FractionAt(EncounterRule.MaxHeatLevel);

        Assert.True(atMax >= atZero);
        Assert.True(atMax <= EncounterRule.MaxStubbornFraction + 0.05);
    }

    [Fact]
    public void ThreatOutcome_CompliantAndStubborn_GetDifferentCannedLines_PodsGetNeither()
    {
        NpcShip pod = Ship("pod-1", isPod: true);
        string podLine = EncounterRule.ThreatOutcome(pod, ComplianceState.NothingToComply);
        Assert.Contains("Nothing aboard", podLine);

        NpcShip ship = Ship("npc-7");
        string compliantLine = EncounterRule.ThreatOutcome(ship, ComplianceState.Compliant);
        string stubbornLine = EncounterRule.ThreatOutcome(ship, ComplianceState.Stubborn);
        Assert.NotEqual(compliantLine, stubbornLine);

        // Deterministic: asking twice tells the same story.
        Assert.Equal(compliantLine, EncounterRule.ThreatOutcome(ship, ComplianceState.Compliant));
    }

    // ---- bribery ----

    [Fact]
    public void BribePrice_IsCargoValueTimes035_CheaperThanRobbingIt()
    {
        NpcShip ship = Ship("npc-9", cargo: "He3", cargoUnits: 10);
        int cargoValue = 10 * CargoMarket.UnitValue("He3");
        int expected = (int)Math.Round(cargoValue * EncounterRule.BribePriceFraction);

        Assert.Equal(expected, EncounterRule.BribePrice(ship));
        Assert.True(EncounterRule.BribePrice(ship) < cargoValue);
    }

    [Fact]
    public void BribedShips_GenerateNoHeat_UnlikeARobbedCompliantOrStubbornShip()
    {
        // Mirrors Map.razor's Board() hook: bribed => +0, compliant => +1, stubborn => +2.
        static int HeatDeltaFor(NpcShip npc, bool bribed, int priorHeat) =>
            bribed ? 0 : EncounterRule.ComplianceOf(npc, priorHeat) == ComplianceState.Stubborn ? 2 : 1;

        NpcShip stubbornShip = FindByCompliance(ComplianceState.Stubborn);
        NpcShip compliantShip = FindByCompliance(ComplianceState.Compliant);

        Assert.Equal(0, HeatDeltaFor(stubbornShip, bribed: true, priorHeat: 0));
        Assert.Equal(0, HeatDeltaFor(compliantShip, bribed: true, priorHeat: 0));
        Assert.Equal(2, HeatDeltaFor(stubbornShip, bribed: false, priorHeat: 0));
        Assert.Equal(1, HeatDeltaFor(compliantShip, bribed: false, priorHeat: 0));
    }

    private static NpcShip FindByCompliance(ComplianceState wanted)
    {
        for (int i = 0; i < 500; i++)
        {
            NpcShip candidate = Ship($"search-{i}");
            if (EncounterRule.ComplianceOf(candidate, 0) == wanted)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"No sample ship hashed to {wanted} in range.");
    }

    // ---- heat ----

    [Fact]
    public void RaiseHeat_ClampsToMaxLevel()
    {
        HeatState heat = HeatState.None;
        heat = EncounterRule.RaiseHeat(heat, 2, simTime: 0);
        Assert.Equal(2, heat.Level);

        heat = EncounterRule.RaiseHeat(heat, 2, simTime: 100);
        Assert.Equal(EncounterRule.MaxHeatLevel, heat.Level);
    }

    [Fact]
    public void DecayHeat_OneLevelPerTwentyDays_AwayFromAHaven()
    {
        const double day = 86400;
        var heat = new HeatState(2, RaisedAtSimTime: 0);

        HeatState beforeDecay = EncounterRule.DecayHeat(heat, simTime: 19 * day, atHavenOrbit: false);
        Assert.Equal(2, beforeDecay.Level);

        HeatState afterOnePeriod = EncounterRule.DecayHeat(heat, simTime: 20 * day, atHavenOrbit: false);
        Assert.Equal(1, afterOnePeriod.Level);

        HeatState afterTwoPeriods = EncounterRule.DecayHeat(heat, simTime: 40 * day, atHavenOrbit: false);
        Assert.Equal(0, afterTwoPeriods.Level);
    }

    [Fact]
    public void DecayHeat_FourTimesFaster_AtAHaven()
    {
        const double day = 86400;
        var heat = new HeatState(1, RaisedAtSimTime: 0);

        // 20 days / 4 = 5 days at a haven.
        HeatState beforeDecay = EncounterRule.DecayHeat(heat, simTime: 4 * day, atHavenOrbit: true);
        Assert.Equal(1, beforeDecay.Level);

        HeatState afterDecay = EncounterRule.DecayHeat(heat, simTime: 5 * day, atHavenOrbit: true);
        Assert.Equal(0, afterDecay.Level);
    }

    [Fact]
    public void DecayHeat_AtZero_IsAStableFixedPoint()
    {
        HeatState heat = HeatState.None;
        Assert.Equal(heat, EncounterRule.DecayHeat(heat, simTime: 1e9, atHavenOrbit: false));
    }

    // ---- hunters ----

    private static readonly Vector2d EarthPosition = new(1.496e11, 0);
    private static readonly Vector2d EarthVelocity = new(0, 29780);

    [Fact]
    public void SpawnHunter_ActivatesAfterFiveDayFittingOut()
    {
        const double day = 86400;
        HunterState hunter = EncounterRule.SpawnHunter(
            "hunter-1", "Debt Collector", "earth", EarthPosition, EarthVelocity, simTime: 0);

        Assert.Equal(5 * day, hunter.ActivationSimTime);
        Assert.False(hunter.CaughtPlayer);
        Assert.False(hunter.BrokenOff);
    }

    [Fact]
    public void AdvanceHunter_BeforeActivation_OnlyCoasts_DoesNotAccelerateTowardPlayer()
    {
        const double day = 86400;
        HunterState hunter = EncounterRule.SpawnHunter(
            "hunter-2", "Repo Barque", "earth", EarthPosition, EarthVelocity, simTime: 0);

        var farAwayPlayer = new ShipState(new Vector2d(0, 0), Vector2d.Zero, day);
        HunterState afterOneDay = EncounterRule.AdvanceHunter(hunter, farAwayPlayer, simTime: day);

        Assert.Equal(EarthVelocity, afterOneDay.State.Velocity); // unchanged — still fitting out
        Assert.Equal(EarthPosition + EarthVelocity * day, afterOneDay.State.Position);
    }

    // A stationary player out at Earth's orbit with the hunter offset perpendicular (so the sun is
    // NOT behind the player from the hunter's view — glare would otherwise stop it closing).
    private static readonly Vector2d PlayerAtEarthOrbit = new(1.496e11, 0);

    [Fact]
    public void AdvanceHunter_AfterActivation_ClosesDistance_OnAStationaryPlayer()
    {
        const double day = 86400;
        var stationaryPlayer = new ShipState(PlayerAtEarthOrbit, Vector2d.Zero, 0);
        HunterState hunter = EncounterRule.SpawnHunter(
            "hunter-3", "The Adjuster", "earth", PlayerAtEarthOrbit + new Vector2d(0, 1e10), Vector2d.Zero, simTime: 0);

        double initialDistance = (hunter.State.Position - stationaryPlayer.Position).Length;

        double simTime = 5 * day;
        for (int i = 0; i < 50; i++)
        {
            simTime += EncounterRule.HunterStepSeconds;
            var playerNow = stationaryPlayer with { SimTime = simTime };
            hunter = EncounterRule.AdvanceHunter(hunter, playerNow, simTime);
        }

        double laterDistance = (hunter.State.Position - stationaryPlayer.Position).Length;
        Assert.True(laterDistance < initialDistance, $"Hunter should close in: {initialDistance:E2} -> {laterDistance:E2}");
    }

    [Fact]
    public void AdvanceHunter_CatchesPlayer_WhenCloseAndSlow()
    {
        var stationaryPlayer = new ShipState(PlayerAtEarthOrbit, Vector2d.Zero, 0);

        // Already inside catch range, at rest relative to the player, past activation. Offset
        // perpendicular so the sun isn't behind the player (no glare).
        HunterState hunter = new(
            "hunter-4", "Fair Warning", "earth",
            SpawnedAtSimTime: -10 * 86400, ActivationSimTime: -5 * 86400,
            State: new ShipState(PlayerAtEarthOrbit + new Vector2d(0, 1e8), Vector2d.Zero, 0),
            CaughtPlayer: false, BrokenOff: false);

        HunterState result = EncounterRule.AdvanceHunter(hunter, stationaryPlayer with { SimTime = 60 }, simTime: 60);

        Assert.True(result.CaughtPlayer);
    }

    [Fact]
    public void AdvanceHunter_DoesNotCatch_WhenFarOrFast()
    {
        var stationaryPlayer = new ShipState(PlayerAtEarthOrbit, Vector2d.Zero, 0);

        HunterState far = new(
            "hunter-5a", "Lien Enforcer", "earth",
            SpawnedAtSimTime: -10 * 86400, ActivationSimTime: -5 * 86400,
            State: new ShipState(PlayerAtEarthOrbit + new Vector2d(0, 5e8), Vector2d.Zero, 0),
            CaughtPlayer: false, BrokenOff: false);
        HunterState farResult = EncounterRule.AdvanceHunter(far, stationaryPlayer with { SimTime = 60 }, simTime: 60);
        Assert.False(farResult.CaughtPlayer);

        HunterState fast = new(
            "hunter-5b", "Lien Enforcer II", "earth",
            SpawnedAtSimTime: -10 * 86400, ActivationSimTime: -5 * 86400,
            State: new ShipState(PlayerAtEarthOrbit + new Vector2d(0, 1e8), new Vector2d(0, 5000), 0),
            CaughtPlayer: false, BrokenOff: false);
        HunterState fastResult = EncounterRule.AdvanceHunter(fast, stationaryPlayer with { SimTime = 60 }, simTime: 60);
        Assert.False(fastResult.CaughtPlayer);
    }

    [Fact]
    public void ApplyBreakOff_AfterTwoHiddenDays_StopsThePursuit()
    {
        const double day = 86400;
        HunterState hunter = EncounterRule.SpawnHunter(
            "hunter-6", "The Widowmaker", "earth", EarthPosition, EarthVelocity, simTime: 0);

        HunterState stillHunting = EncounterRule.ApplyBreakOff(hunter, hiddenDurationSeconds: 1.9 * day);
        Assert.False(stillHunting.BrokenOff);

        HunterState brokeOff = EncounterRule.ApplyBreakOff(hunter, hiddenDurationSeconds: 2 * day);
        Assert.True(brokeOff.BrokenOff);

        // Once broken off, further pursuit steps are a no-op.
        HunterState advanced = EncounterRule.AdvanceHunter(
            brokeOff, new ShipState(Vector2d.Zero, Vector2d.Zero, 10 * day), simTime: 10 * day);
        Assert.Equal(brokeOff.State, advanced.State);
    }

    // ---- warning shots: disposition, nerve, peel-off (owner: a collector that behaves like a person) ----

    private static HunterState ActiveHunter(string id, Vector2d position, Vector2d velocity) => new(
        id, "Debt Collector", "earth",
        SpawnedAtSimTime: -10 * 86400, ActivationSimTime: -5 * 86400,
        State: new ShipState(position, velocity, 0),
        CaughtPlayer: false, BrokenOff: false);

    [Fact]
    public void PrefersTheGoodLife_IsDeterministic_PerId()
    {
        for (int i = 0; i < 20; i++)
        {
            string id = $"hunter-glf-{i}";
            Assert.Equal(
                EncounterRule.PrefersTheGoodLife(id, playerHeat: 0),
                EncounterRule.PrefersTheGoodLife(id, playerHeat: 0));
        }
    }

    [Fact]
    public void PrefersTheGoodLife_RarerAtHigherHeat()
    {
        int atZeroHeat = 0, atMaxHeat = 0;
        for (int i = 0; i < 400; i++)
        {
            string id = $"collector-{i}";
            if (EncounterRule.PrefersTheGoodLife(id, playerHeat: 0)) { atZeroHeat++; }
            if (EncounterRule.PrefersTheGoodLife(id, playerHeat: EncounterRule.MaxHeatLevel)) { atMaxHeat++; }
        }

        // A fat bounty (heat) draws hungrier, grittier muscle — fewer good-life types.
        Assert.True(atMaxHeat < atZeroHeat, $"good-life count should fall with heat: {atZeroHeat} -> {atMaxHeat}");
    }

    [Fact]
    public void NerveThreshold_GoodLifeQuitsInOne_ProfessionalNeedsMore()
    {
        // Find one of each disposition deterministically.
        string goodLife = Enumerable.Range(0, 500).Select(i => $"n-{i}")
            .First(id => EncounterRule.PrefersTheGoodLife(id, 0));
        string professional = Enumerable.Range(0, 500).Select(i => $"n-{i}")
            .First(id => !EncounterRule.PrefersTheGoodLife(id, 0));

        Assert.Equal(1, EncounterRule.NerveThreshold(goodLife, playerHeat: 0));
        Assert.Equal(EncounterRule.BaseHunterNerve, EncounterRule.NerveThreshold(professional, playerHeat: 0));
        // Grittier by one per heat level.
        Assert.Equal(EncounterRule.BaseHunterNerve + 2, EncounterRule.NerveThreshold(professional, playerHeat: 2));
    }

    [Fact]
    public void WarnOff_FirstShotOnAProfessional_PeelsOff_ButDoesNotBreak()
    {
        string professional = Enumerable.Range(0, 500).Select(i => $"p-{i}")
            .First(id => !EncounterRule.PrefersTheGoodLife(id, 0));
        HunterState hunter = ActiveHunter(professional, new Vector2d(1e10, 0), Vector2d.Zero);

        HunterState after = EncounterRule.WarnOff(hunter, playerHeat: 0, simTime: 1000);

        Assert.False(after.BrokenOff);
        Assert.Equal(1, after.WarningShotsTaken);
        Assert.Equal(1000 + EncounterRule.HunterPeelStepDays * 86400, after.PeeledUntilSimTime);
    }

    [Fact]
    public void WarnOff_PeelWindowGrowsWithEachShot()
    {
        string professional = Enumerable.Range(0, 500).Select(i => $"grow-{i}")
            .First(id => !EncounterRule.PrefersTheGoodLife(id, 0));
        HunterState hunter = ActiveHunter(professional, new Vector2d(1e10, 0), Vector2d.Zero);

        HunterState afterOne = EncounterRule.WarnOff(hunter, playerHeat: 0, simTime: 0);
        HunterState afterTwo = EncounterRule.WarnOff(afterOne, playerHeat: 0, simTime: 0);

        double peelOne = afterOne.PeeledUntilSimTime;
        double peelTwo = afterTwo.PeeledUntilSimTime;
        Assert.True(peelTwo > peelOne, $"second peel should be longer: {peelOne} -> {peelTwo}");
        Assert.Equal(2 * EncounterRule.HunterPeelStepDays * 86400, peelTwo);
    }

    [Fact]
    public void WarnOff_ReachingNerveThreshold_VoidsTheContract()
    {
        string professional = Enumerable.Range(0, 500).Select(i => $"thr-{i}")
            .First(id => !EncounterRule.PrefersTheGoodLife(id, 0));
        HunterState hunter = ActiveHunter(professional, new Vector2d(1e10, 0), Vector2d.Zero);

        int threshold = EncounterRule.NerveThreshold(professional, playerHeat: 0);
        for (int shot = 1; shot < threshold; shot++)
        {
            hunter = EncounterRule.WarnOff(hunter, playerHeat: 0, simTime: 0);
            Assert.False(hunter.BrokenOff);
        }

        hunter = EncounterRule.WarnOff(hunter, playerHeat: 0, simTime: 0);
        Assert.True(hunter.BrokenOff);
    }

    [Fact]
    public void WarnOff_GoodLifeCollector_QuitsAtTheFirstShot()
    {
        string goodLife = Enumerable.Range(0, 500).Select(i => $"dolce-{i}")
            .First(id => EncounterRule.PrefersTheGoodLife(id, 0));
        HunterState hunter = ActiveHunter(goodLife, new Vector2d(1e10, 0), Vector2d.Zero);

        HunterState after = EncounterRule.WarnOff(hunter, playerHeat: 0, simTime: 0);
        Assert.True(after.BrokenOff);
    }

    [Fact]
    public void WarnOff_CaughtOrBrokenHunter_IsUnmoved()
    {
        HunterState caught = ActiveHunter("caught", Vector2d.Zero, Vector2d.Zero) with { CaughtPlayer = true };
        Assert.Equal(caught, EncounterRule.WarnOff(caught, 0, 0));

        HunterState broken = ActiveHunter("broken", Vector2d.Zero, Vector2d.Zero) with { BrokenOff = true };
        Assert.Equal(broken, EncounterRule.WarnOff(broken, 0, 0));
    }

    [Fact]
    public void AdvanceHunter_WhilePeeled_Coasts_ThenResumesClosing()
    {
        const double day = 86400;
        var player = new ShipState(PlayerAtEarthOrbit, Vector2d.Zero, 0);
        // Perpendicular offset (no glare), moving so we can see it coast on its velocity.
        HunterState hunter = ActiveHunter(
            "peel-coast", PlayerAtEarthOrbit + new Vector2d(0, 5e9), new Vector2d(100, 0))
            with { PeeledUntilSimTime = 3 * day };

        // Mid-peel: it must coast on its current velocity, not accelerate toward the player.
        HunterState mid = EncounterRule.AdvanceHunter(hunter, player with { SimTime = day }, simTime: day);
        Assert.Equal(new Vector2d(100, 0), mid.State.Velocity);

        // After the window lapses it re-acquires and closes again.
        double before = (mid.State.Position - player.Position).Length;
        double t = 3 * day;
        HunterState resumed = mid;
        for (int i = 0; i < 50; i++)
        {
            t += EncounterRule.HunterStepSeconds;
            resumed = EncounterRule.AdvanceHunter(resumed, player with { SimTime = t }, simTime: t);
        }

        double after = (resumed.State.Position - player.Position).Length;
        Assert.True(after < before, $"should resume closing after the peel: {before:E2} -> {after:E2}");
    }

    [Fact]
    public void AdvanceHunter_SunBehindPlayer_BlindsThePursuit()
    {
        // Player between the sun (origin) and the hunter, both far out on the +x axis: the hunter
        // stares into glare and cannot refine the chase — it coasts instead of closing.
        var player = new ShipState(new Vector2d(1e11, 0), Vector2d.Zero, 0);
        HunterState hunter = ActiveHunter("glare", new Vector2d(2e11, 0), new Vector2d(-100, 0));

        double before = (hunter.State.Position - player.Position).Length;
        double t = 0;
        for (int i = 0; i < 50; i++)
        {
            t += EncounterRule.HunterStepSeconds;
            hunter = EncounterRule.AdvanceHunter(hunter, player with { SimTime = t }, simTime: t);
        }

        // Velocity unchanged (pure coast) and it has not closed appreciably by accelerating.
        Assert.Equal(new Vector2d(-100, 0), hunter.State.Velocity);
        double after = (hunter.State.Position - player.Position).Length;
        Assert.True(after >= before - 1e7, "sun-blinded hunter should not be closing under thrust");
    }

    [Fact]
    public void SunBlinded_OnlyWhenPlayerIsSunwardAndInTheGlareCone()
    {
        var hunter = new Vector2d(2e11, 0);

        // Player directly sunward of the hunter → blinded.
        Assert.True(EncounterRule.SunBlinded(hunter, new Vector2d(1e11, 0)));

        // Player off to the side (perpendicular) → not blinded.
        Assert.False(EncounterRule.SunBlinded(hunter, new Vector2d(2e11, 1e11)));

        // Player FARTHER from the sun than the hunter (hunter between sun and player) → not blinded.
        Assert.False(EncounterRule.SunBlinded(hunter, new Vector2d(3e11, 0)));
    }

    [Fact]
    public void NearestPolicedBody_PicksAPlanet_NotAHaven()
    {
        var ephemeris = Sol();
        CelestialBody? policed = EncounterRule.NearestPolicedBody(ephemeris, playerPosition: Vector2d.Zero, simTime: 0);

        Assert.NotNull(policed);
        Assert.False(policed!.IsHaven);
        Assert.NotNull(policed.ParentId);

        // Enceladus (Saturn's haven moon) must never be picked as a source of policed muscle.
        Assert.NotEqual("enceladus", policed.Id);
    }

    [Fact]
    public void HunterPursuit_IsFullyDeterministic_AcrossTwoIdenticalRuns()
    {
        HunterState RunOnce()
        {
            HunterState hunter = EncounterRule.SpawnHunter(
                "hunter-det", "Underwriter's Claw", "earth", new Vector2d(2e9, 1e9), Vector2d.Zero, simTime: 0);
            double simTime = 5 * 86400;
            for (int i = 0; i < 20; i++)
            {
                simTime += EncounterRule.HunterStepSeconds;
                var player = new ShipState(Vector2d.Zero, Vector2d.Zero, simTime);
                hunter = EncounterRule.AdvanceHunter(hunter, player, simTime);
            }

            return hunter;
        }

        HunterState first = RunOnce();
        HunterState second = RunOnce();
        Assert.Equal(first, second); // record equality: bit-for-bit
    }
}
