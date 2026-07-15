namespace SpaceSails.Core.Tests;

/// <summary>
/// The autopilot's promise (issues #146/#147, owner ruling: "dropping from autopilot is the horrible
/// scenario — it should never ever happen when there was nothing external to cause it"). The arm-time
/// rehearsal flies the whole armed journey with the real OrbitRule decision logic and prices it, so
/// the feasibility question is settled on the click: the doomed Titan inbound is refused; the cheap
/// Enceladus park is accepted.
/// </summary>
public class AutopilotRehearsalTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public AutopilotRehearsalTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const int Tank = 250;

    private static (Simulator Sim, ICelestialEphemeris Eph) SaturnSystem()
    {
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        return (new Simulator(eph, timeStepSeconds: 60), eph);
    }

    private static Vector2d BodyVel(ICelestialEphemeris eph, string id, double t) =>
        (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

    [Fact]
    public void ReserveFloor_IsAFifthIshOfCapacity_ConstantInCore()
    {
        // 18% of the 250-pulse starter tank ≈ 45; of an upgraded 400-pulse tank ≈ 72. A real cushion
        // that also absorbs the rehearsal's step-size error.
        Assert.Equal(45, AutopilotRehearsal.ReservePulses(250));
        Assert.Equal(72, AutopilotRehearsal.ReservePulses(400));
        Assert.True(AutopilotRehearsal.ReservePulses(1) >= 1, "Reserve never rounds to zero.");
    }

    [Fact]
    public void Titan_InboundOnASaturnEllipse_IsRefused_ItWouldBleedTheTankDry()
    {
        // Tonight's live failure (build 6f483d6): armed auto-orbit Titan from ~2.4 M km while screaming
        // through Saturn's well. Titan's capture floor is 3e9 m, so 2.4e9 m is already "in range" — the
        // approach engages, Saturn keeps re-accelerating the ship, rel-speed climbs past the limit, and
        // it re-burns for days (33 km/s of Δv, 175→8 pulses). The rehearsal must SEE that and refuse.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d saturnVel = BodyVel(eph, "saturn", 0);
        Vector2d titanPos = eph.Position("titan", 0);

        // 2.4 M km from Titan, out past it from Saturn, on a fast prograde Saturn orbit (not co-moving
        // with Titan) — the "big Saturn ellipse" the owner was on.
        Vector2d outward = (titanPos - saturnPos).Normalized();
        Vector2d tangential = new Vector2d(-outward.Y, outward.X);
        var ship = new ShipState(titanPos + outward * 2.4e9, saturnVel + tangential * 8000, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(ship, eph, sim, "titan", budget);
        _out.WriteLine($"TITAN #146: captured={r.Captured} pulses={r.Pulses} burns={r.ApproachBurns} " +
            $"budgetExceeded={r.BudgetExceeded} horizon={r.HorizonReached} durDays={r.SimDurationSeconds / 86400:F2} budget={budget}");

        Assert.False(r.Deliverable,
            $"Titan inbound should be refused. captured={r.Captured} pulses={r.Pulses} burns={r.ApproachBurns} " +
            $"budgetExceeded={r.BudgetExceeded} horizon={r.HorizonReached} durDays={r.SimDurationSeconds / 86400:F1}");
        // And the honest headline: its rehearsed cost blows past what the tank can afford.
        Assert.True(r.Pulses > budget || r.HorizonReached,
            $"Expected the cost to exceed the affordable budget ({budget}) or fail to capture; got pulses={r.Pulses}.");
    }

    [Fact]
    public void Enceladus_AlongsideAtRest_IsAccepted_AndCheap()
    {
        // The realistic good case (mirrors the #136 e2e): a transfer put the ship alongside Enceladus,
        // co-moving with it in Saturn's frame; press auto-orbit. The rehearsal flies it to a bound
        // park for a fraction of the tank.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d encVel = BodyVel(eph, "enceladus", 0);
        Vector2d outward = (encPos - saturnPos).Normalized();
        var ship = new ShipState(encPos + outward * 5e6, encVel, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(ship, eph, sim, "enceladus", budget);
        _out.WriteLine($"ENCELADUS near-rest: captured={r.Captured} pulses={r.Pulses} burns={r.ApproachBurns} " +
            $"budgetExceeded={r.BudgetExceeded} horizon={r.HorizonReached} durDays={r.SimDurationSeconds / 86400:F2} budget={budget}");

        Assert.True(r.Deliverable,
            $"Enceladus alongside-at-rest should be accepted. captured={r.Captured} pulses={r.Pulses} " +
            $"burns={r.ApproachBurns} budgetExceeded={r.BudgetExceeded} horizon={r.HorizonReached}");
        Assert.True(r.Pulses > 0 && r.Pulses <= budget,
            $"Expected a cheap, affordable capture; got pulses={r.Pulses} vs budget={budget}.");
    }

    [Fact]
    public void CoastingOutsideCaptureRange_SpendsNoPulses()
    {
        // Proof the rehearsal only spends where the live loop would — inside capture range. A ship well
        // outside the capture floor and gently receding never gets a burn: it just coasts, so a
        // short-horizon rehearsal records a growing path at zero cost. (This is also why cheap moon
        // capture fundamentally needs a low-relative-speed arrival close to the moon — matching a
        // moon's 12.6 km/s orbital velocity from far out is the very fuel bleed the guard refuses;
        // the parent-frame transfer that flies it cheaply is the follow-up lane, #146.)
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d saturnVel = BodyVel(eph, "saturn", 0);
        Vector2d outward = (encPos - saturnPos).Normalized();
        // 6e9 m out (double the 3e9 capture floor), at rest in the sun frame but gently receding —
        // stays out of range across a one-day horizon.
        var ship = new ShipState(encPos + outward * 6e9, saturnVel + outward * 2000, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "enceladus", budget, capturePath: true, maxHorizonSeconds: 86400.0);

        Assert.Equal(0, r.Pulses);
        Assert.Equal(0, r.ApproachBurns);
        Assert.True(r.HorizonReached, "A far, receding coast should hit the horizon, not capture.");
        Assert.True(r.Path.Count > 1, "Coasting still advances the ship along a path.");
    }

    [Fact]
    public void PathCapture_RecordsTheIntendedTrajectory_WhenAsked()
    {
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d encVel = BodyVel(eph, "enceladus", 0);
        Vector2d outward = (encPos - saturnPos).Normalized();
        var ship = new ShipState(encPos + outward * 5e6, encVel, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult with = AutopilotRehearsal.Rehearse(ship, eph, sim, "enceladus", budget, capturePath: true);
        AutopilotRehearsal.RehearsalResult without = AutopilotRehearsal.Rehearse(ship, eph, sim, "enceladus", budget, capturePath: false);

        Assert.True(with.Path.Count > 1, "A captured journey should record a multi-point path.");
        Assert.Empty(without.Path);
        // The path is time-ordered and starts at the ship's arming position.
        Assert.Equal(ship.Position, with.Path[0].Position);
        Assert.True(with.Path[^1].SimTime > with.Path[0].SimTime, "Path samples advance in time.");
    }

    [Fact]
    public void NonOrbitableTarget_IsNotAJourney()
    {
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        var ship = new ShipState(new Vector2d(1e11, 0), Vector2d.Zero, 0);
        // The sun has no parent — not a bus stop.
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(ship, eph, sim, "sun", 200);
        Assert.False(r.Deliverable);
        Assert.Equal(0, r.Pulses);
    }
}
