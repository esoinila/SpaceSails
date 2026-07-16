namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for <see cref="FuelReachability"/> — Lab 28 "The pump crawl" (#146/#157/#166), the
/// service that answers "from where I am, with the pulses I have, can I still reach a fuel pump?".
/// Lab-gate style: inline system factories, invariant BANDS and structural invariants not exact
/// probe numbers, every assert independent of the probe (the lesson's prose can evolve without
/// touching these). The Saturn system is the sol.json subset (Saturn parentless at the origin +
/// Titan + the Enceladus haven + the Ringside station-haven); the Earth system is Earth + Luna +
/// the LEO Highport station. See labs/28-the-pump-crawl/README.md and Probe.cs for the lesson.
/// </summary>
public class FuelReachabilityTests
{
    private const double SaturnMu = 3.7931187e16;
    private const double TitanMu = 8.9781e12;
    private const double EnceladusMu = 7.211e9;
    private const double EarthMu = 3.986004418e14;
    private const double LunaMu = 4.9048695e12;
    private const int BaseTank = 250;

    // Saturn parentless at the origin; Titan (dry moon), the Enceladus haven and the Ringside
    // station-haven on their sol.json rails. Two pumps orbit Saturn (Enceladus, Ringside); Titan is dry.
    private static (ICelestialEphemeris Eph, Simulator Sim) MakeSaturnSystem()
    {
        var bodies = new[]
        {
            new CelestialBody("saturn", "Saturn", null, SaturnMu, 5.8232e7, 0, 0, 0),
            new CelestialBody("titan", "Titan", "saturn", TitanMu, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
            new CelestialBody("enceladus", "Enceladus", "saturn", EnceladusMu, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon, IsHaven: true),
            new CelestialBody("ringside-exchange", "Ringside Exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station, IsHaven: true),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    // Earth parentless at the origin; Luna (dry moon) and Highport (a LEO station — the only Earth-well
    // depot host, and one the 5 km/s capture cap can't match from Luna: Luna is stranded from refuel).
    private static (ICelestialEphemeris Eph, Simulator Sim) MakeEarthSystem()
    {
        var bodies = new[]
        {
            new CelestialBody("earth", "Earth", null, EarthMu, 6.371e6, 0, 0, 0),
            new CelestialBody("luna", "Luna", "earth", LunaMu, 1.7374e6, 3.844e8, 2.3606e6, 0.0, BodyKind.Moon),
            new CelestialBody("satellite-factory", "Highport Satellite Works", "earth", 0, 300, 6.771e6, 5546, 2.4, BodyKind.Station),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    // A ship genuinely parked at a moon: inside its Hill sphere, riding the moon's velocity.
    private static ShipState ParkedAt(ICelestialEphemeris eph, string moonId, double parentMu)
    {
        CelestialBody moon = eph.Bodies.First(b => b.Id == moonId);
        double hill = OrbitRule.HillRadius(moon, parentMu);
        Vector2d pos = eph.Position(moonId, 0);
        Vector2d parentPos = eph.Position(moon.ParentId!, 0);
        Vector2d outward = (pos - parentPos).Normalized();
        double altitude = Math.Max(moon.BodyRadius * 1.5, hill * 0.3);
        return new ShipState(pos + outward * altitude, TransferMath.BodyVelocity(eph, moonId, 0), 0);
    }

    // A ship docked at a mass-less station: inside its dock envelope, matched to its rail velocity.
    private static ShipState DockedAt(ICelestialEphemeris eph, string stationId)
    {
        Vector2d pos = eph.Position(stationId, 0);
        return new ShipState(pos + new Vector2d(1e6, 0), TransferMath.BodyVelocity(eph, stationId, 0), 0);
    }

    // A free-flying cruise state on a circular parent orbit at the given radius and polar angle.
    private static ShipState Cruise(ICelestialEphemeris eph, string parentId, double mu, double radius, double angle)
    {
        Vector2d parentPos = eph.Position(parentId, 0);
        Vector2d rel = new Vector2d(Math.Cos(angle), Math.Sin(angle)) * radius;
        Vector2d tangent = new Vector2d(-Math.Sin(angle), Math.Cos(angle));
        return new ShipState(parentPos + rel, TransferMath.BodyVelocity(eph, parentId, 0) + tangent * Math.Sqrt(mu / radius), 0);
    }

    [Fact]
    public void G1_PumpMap_DepotsAtHavensAndStations_NotAtDryMoons()
    {
        var (eph, _) = MakeSaturnSystem();
        var hosts = TrafficSchedule.GenerateDepots(eph, seed: 1).Select(d => d.DepotBodyId).ToHashSet();

        Assert.Contains("enceladus", hosts);          // haven moon → pump
        Assert.Contains("ringside-exchange", hosts);  // station+haven → pump
        Assert.DoesNotContain("titan", hosts);        // ordinary moon → dry
    }

    [Fact]
    public void G2_AlongsideAPump_IsComfortable_WithZeroReach()
    {
        var (eph, sim) = MakeSaturnSystem();

        var atHaven = FuelReachability.Assess(sim, eph, ParkedAt(eph, "enceladus", SaturnMu), 30, BaseTank, "saturn");
        Assert.Equal(FuelReachability.Verdict.Comfortable, atHaven.Verdict);
        Assert.Equal(0, atHaven.NearestDepotPulses);
        Assert.Equal("enceladus", atHaven.NearestDepotBodyId);

        var atStation = FuelReachability.Assess(sim, eph, DockedAt(eph, "ringside-exchange"), 30, BaseTank, "saturn");
        Assert.Equal(FuelReachability.Verdict.Comfortable, atStation.Verdict);
        Assert.Equal(0, atStation.NearestDepotPulses);
        Assert.Equal("ringside-exchange", atStation.NearestDepotBodyId);
    }

    [Fact]
    public void G3_ParkedAtDryTitan_PricesAFiniteReachToAPump()
    {
        var (eph, sim) = MakeSaturnSystem();
        var a = FuelReachability.Assess(sim, eph, ParkedAt(eph, "titan", SaturnMu), 250, BaseTank, "saturn");

        Assert.NotEqual(int.MaxValue, a.NearestDepotPulses);
        Assert.True(a.NearestDepotPulses is > 0 and < 150,
            $"reach from parked-at-Titan should be a modest, finite pulse count (was {a.NearestDepotPulses})");
        Assert.NotNull(a.NearestDepotBodyId);
        Assert.Contains(a.NearestDepotBodyId, new[] { "enceladus", "ringside-exchange" });
        Assert.NotEmpty(a.Reachable);
    }

    [Fact]
    public void G4_Verdicts_StepFromStrandedToComfortableAsTheTankFills()
    {
        var (eph, sim) = MakeSaturnSystem();
        ShipState titan = ParkedAt(eph, "titan", SaturnMu);
        int reach = FuelReachability.Assess(sim, eph, titan, 250, BaseTank, "saturn").NearestDepotPulses;
        int flat = AutopilotRehearsal.ReservePulses(BaseTank);
        int safe = reach + flat; // the well-aware amber floor

        // Below the reach: stranded (red).
        Assert.Equal(FuelReachability.Verdict.CannotReachAPump,
            FuelReachability.Assess(sim, eph, titan, reach - 1, BaseTank, "saturn").Verdict);

        // At/above the reach but below the well-aware reserve: thin (amber).
        Assert.Equal(FuelReachability.Verdict.Thin,
            FuelReachability.Assess(sim, eph, titan, reach, BaseTank, "saturn").Verdict);
        Assert.Equal(FuelReachability.Verdict.Thin,
            FuelReachability.Assess(sim, eph, titan, safe - 1, BaseTank, "saturn").Verdict);

        // At/above the well-aware reserve: comfortable.
        Assert.Equal(FuelReachability.Verdict.Comfortable,
            FuelReachability.Assess(sim, eph, titan, safe, BaseTank, "saturn").Verdict);
    }

    [Fact]
    public void G5_MarginIsMonotoneAndVerdictNeverWorsensAsPulsesRise()
    {
        var (eph, sim) = MakeSaturnSystem();
        ShipState titan = ParkedAt(eph, "titan", SaturnMu);

        int prevMargin = int.MinValue;
        int prevSeverity = int.MaxValue; // CannotReachAPump=2 highest; verdict severity must not rise
        for (int remaining = 0; remaining <= 250; remaining += 5)
        {
            var a = FuelReachability.Assess(sim, eph, titan, remaining, BaseTank, "saturn");

            Assert.True(a.MarginPulses > prevMargin,
                $"margin must strictly increase with remaining (at {remaining}: {a.MarginPulses} vs {prevMargin})");
            prevMargin = a.MarginPulses;

            int severity = (int)a.Verdict; // Comfortable=0 < Thin=1 < CannotReachAPump=2
            Assert.True(severity <= prevSeverity,
                $"verdict must never worsen as the tank fills (at {remaining}: {a.Verdict})");
            prevSeverity = severity;
        }
    }

    [Fact]
    public void G6_SafeReserveRidesTheReach_AndExceedsTheFlatFloorOutInTheWell()
    {
        var (eph, sim) = MakeSaturnSystem();
        var a = FuelReachability.Assess(sim, eph, ParkedAt(eph, "titan", SaturnMu), 250, BaseTank, "saturn");
        int flat = AutopilotRehearsal.ReservePulses(BaseTank);

        // The amber floor is reach + the flat 18% cushion — and out in the well it is above the flat floor.
        Assert.Equal(a.NearestDepotPulses + flat, a.SafeReservePulses);
        Assert.True(a.SafeReservePulses > flat,
            $"well-aware reserve ({a.SafeReservePulses}) must exceed the flat floor ({flat}) when a crawl is priced");
    }

    [Fact]
    public void G7_Cruise_ReachesAPump_FromMidWell()
    {
        var (eph, sim) = MakeSaturnSystem();
        var a = FuelReachability.Assess(sim, eph, Cruise(eph, "saturn", SaturnMu, 6.0e8, 0.5), 250, BaseTank, "saturn");

        Assert.NotEqual(int.MaxValue, a.NearestDepotPulses);
        Assert.Equal(FuelReachability.Verdict.Comfortable, a.Verdict);
    }

    [Fact]
    public void G8_ParkedAtLuna_CannotReachAPump_TheHonestGap()
    {
        var (eph, sim) = MakeEarthSystem();
        var a = FuelReachability.Assess(sim, eph, ParkedAt(eph, "luna", EarthMu), 250, BaseTank, "earth");

        // Luna's only in-well depot host is a LEO station the 5 km/s capture cap can't match — stranded.
        Assert.Equal(FuelReachability.Verdict.CannotReachAPump, a.Verdict);
        Assert.Equal(int.MaxValue, a.NearestDepotPulses);
        Assert.Empty(a.Reachable);
    }

    [Fact]
    public void G9_NoWellOrNoPump_FailsClosed_NotOpen()
    {
        var (eph, sim) = MakeSaturnSystem();

        // An unknown well: cannot reach a pump (fails closed), with a spoken reason.
        var noWell = FuelReachability.Assess(sim, eph, DockedAt(eph, "ringside-exchange"), 100, BaseTank, "pluto");
        Assert.Equal(FuelReachability.Verdict.CannotReachAPump, noWell.Verdict);
        Assert.False(string.IsNullOrWhiteSpace(noWell.Reason));
    }

    [Fact]
    public void G10_Determinism_TwoAssessmentsAreIdentical()
    {
        var (eph1, sim1) = MakeSaturnSystem();
        var (eph2, sim2) = MakeSaturnSystem();
        var a1 = FuelReachability.Assess(sim1, eph1, ParkedAt(eph1, "titan", SaturnMu), 60, BaseTank, "saturn");
        var a2 = FuelReachability.Assess(sim2, eph2, ParkedAt(eph2, "titan", SaturnMu), 60, BaseTank, "saturn");

        Assert.Equal(a1.Verdict, a2.Verdict);
        Assert.Equal(a1.NearestDepotBodyId, a2.NearestDepotBodyId);
        Assert.Equal(a1.NearestDepotPulses, a2.NearestDepotPulses);
        Assert.Equal(a1.MarginPulses, a2.MarginPulses);
        Assert.Equal(a1.SafeReservePulses, a2.SafeReservePulses);
        Assert.Equal(a1.Reason, a2.Reason);
    }
}
