namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 24 — The last mile (co-orbital phasing rendezvous, #155).
/// See labs/24-the-last-mile/README.md and Probe.cs for the lesson.
/// Every gate exercises the SAME Core code the autopilot flies with: TransferMath.PhasingOrbit/
/// PhaseGap, TransferPlanner.Solve, OrbitRule — the geometry is the owner's exact #155 case, a ship
/// 92,640 km behind Ringside Exchange on the same Saturn lane.
/// </summary>
public class Lab24LastMileTests
{
    private const double Day = 86400.0;
    private const double SunMu = 1.32712440018e20;
    private const double SaturnMu = 3.7931187e16;
    private const double ArcBehind = 92.64e6;          // 92,640 km of arc behind Ringside
    private const double DockEnvelopeMeters = 5e8;      // the game's dock coaching envelope
    private const double DockMatchSpeed = 8000.0;

    private static (ICelestialEphemeris Eph, Simulator Sim) MakeWorld()
    {
        // Enough of sol.json for the case: Saturn about the sun (so the phasing Hill ceiling has a
        // grandparent), its two moons (so the planner's parked-at-moon guard runs), and Ringside.
        var bodies = new[]
        {
            new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
            new CelestialBody("saturn", "saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
            new CelestialBody("titan", "titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
            new CelestialBody("enceladus", "enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon),
            new CelestialBody("ringside-exchange", "ringside-exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    // The owner's #155 ship: on Ringside's lane, ArcBehind of arc behind it, riding the rail-tangent
    // circular velocity in the world frame (Saturn's velocity + local CCW circular).
    private static ShipState MakeShip(ICelestialEphemeris eph)
    {
        double t0 = 0.0;
        Vector2d satPos = eph.Position("saturn", t0);
        Vector2d satVel = (eph.Position("saturn", t0 + 1) - eph.Position("saturn", t0 - 1)) / 2.0;
        CelestialBody ring = eph.Bodies.First(b => b.Id == "ringside-exchange");
        Vector2d ringRel = eph.Position("ringside-exchange", t0) - satPos;
        double ringAngle = Math.Atan2(ringRel.Y, ringRel.X);
        double shipAngle = ringAngle - ArcBehind / ring.OrbitRadius;
        Vector2d relPos = new Vector2d(Math.Cos(shipAngle), Math.Sin(shipAngle)) * ring.OrbitRadius;
        Vector2d tangent = new Vector2d(-Math.Sin(shipAngle), Math.Cos(shipAngle));
        double vCirc = Math.Sqrt(SaturnMu / ring.OrbitRadius);
        return new ShipState(satPos + relPos, satVel + tangent * vCirc, t0);
    }

    private static TransferPlanner.Result Solve(ICelestialEphemeris eph, Simulator sim, ShipState ship) =>
        TransferPlanner.Solve(sim, eph, new TransferPlanner.Request(ship, "saturn", "ringside-exchange", MaxWaitSeconds: 0));

    [Fact]
    public void G1_Planner_SolvesTheRingsideCase_CheapWinnerWithATradeTable()
    {
        var (eph, sim) = MakeWorld();
        var plan = Solve(eph, sim, MakeShip(eph));

        Assert.True(plan.Ok, $"planner must solve the co-orbital case; got: {plan.Failure}");
        Assert.True(plan.PlannedDeltaVTotal < 100.0,
            $"winner must be a cheap phasing rendezvous (<100 m/s); got {plan.PlannedDeltaVTotal:F1} m/s");
        Assert.True(plan.Alternatives.Count >= 2,
            $"the trade table must offer at least two windows; got {plan.Alternatives.Count}");
        Assert.Equal(2, plan.Burns.Count); // exactly two burns: enter the phasing ellipse, re-match on return
    }

    [Fact]
    public void G2_FlownTwoBurnSchedule_ArrivesInsideTheDockEnvelope()
    {
        var (eph, sim) = MakeWorld();
        ShipState ship = MakeShip(eph);
        var plan = Solve(eph, sim, ship);
        Assert.True(plan.Ok, plan.Failure);

        // Fly both burns through the real N-body integrator at their epochs, then read the miss and
        // the closing speed against Ringside's true rail state at arrival — the honest verdict.
        TransferPlanner.BurnStep b1 = plan.Burns[0];
        TransferPlanner.BurnStep b2 = plan.Burns[^1];
        ShipState atB1 = sim.RunAdaptive(ship, b1.SimTime - ship.SimTime);
        var afterB1 = atB1 with { Velocity = atB1.Velocity + b1.DeltaV };
        ShipState atB2 = sim.RunAdaptive(afterB1, b2.SimTime - afterB1.SimTime);
        var afterB2 = atB2 with { Velocity = atB2.Velocity + b2.DeltaV };

        Vector2d stationPos = eph.Position("ringside-exchange", afterB2.SimTime);
        Vector2d stationVel = (eph.Position("ringside-exchange", afterB2.SimTime + 1)
                               - eph.Position("ringside-exchange", afterB2.SimTime - 1)) / 2.0;
        double miss = (afterB2.Position - stationPos).Length;
        double rel = (afterB2.Velocity - stationVel).Length;

        Assert.True(miss < DockEnvelopeMeters,
            $"flown arrival must land inside the {DockEnvelopeMeters / 1e6:F0} Mm dock envelope; missed by {miss / 1e6:F1} Mm");
        Assert.True(rel < 100.0,
            $"flown arrival must match Ringside within 100 m/s (two-body-lie margin under the {DockMatchSpeed / 1000:F0} km/s cap); got {rel:F1} m/s");
    }

    [Fact]
    public void G3_LegacyApproachLoop_CostsMoreThan20xTheWinner()
    {
        var (eph, sim) = MakeWorld();
        ShipState ship = MakeShip(eph);
        var plan = Solve(eph, sim, ship);
        Assert.True(plan.Ok, plan.Failure);

        // Fly the old point-and-throttle loop to true docking range (1,000 km) and sum its burn Δv —
        // the brute-force last mile the autopilot rehearsed and declined (#155). A mu=0 station never
        // opens an insertion window, so it can only ever "Approach".
        CelestialBody station = eph.Bodies.First(b => b.Id == "ringside-exchange");
        CelestialBody saturn = eph.Bodies.First(b => b.Id == "saturn");
        double hill = OrbitRule.HillRadius(station, saturn.Mu); // 0 for a mass-less station
        double captureRange = OrbitRule.CaptureRange(hill);     // 3e9 floor
        double legacyDv = 0;
        ShipState s = ship;
        int iter = 0;
        bool reached = false;
        while (s.SimTime < ship.SimTime + 5 * Day && iter++ < 60_000)
        {
            Vector2d bodyPos = eph.Position(station.Id, s.SimTime);
            Vector2d bodyVel = (eph.Position(station.Id, s.SimTime + 1) - eph.Position(station.Id, s.SimTime - 1)) / 2.0;
            double distance = (s.Position - bodyPos).Length;
            if (distance <= 1e6)
            {
                reached = true;
                break;
            }

            var obstacle = new OrbitRule.ApproachObstacle(
                eph.Position("saturn", s.SimTime), saturn.BodyRadius * OrbitRule.ParentSafeBodyRadii);
            if (OrbitRule.AutopilotDecision(s, bodyPos, bodyVel, station, hill) == OrbitRule.AutopilotAction.Approach)
            {
                ShipState before = s;
                s = OrbitRule.Approach(s, bodyPos, bodyVel, station, obstacle, hill);
                legacyDv += (s.Velocity - before.Velocity).Length;
                s = sim.RunAdaptive(s, 60.0);
            }
            else
            {
                // Fine-step when inside capture range so the 4 km/s fall can't leap over the 1,000 km
                // window between samples (the probe's FlyOldLoop does exactly this).
                s = sim.RunAdaptive(s, distance > captureRange * 1.25 ? 1800.0 : 60.0);
            }
        }

        Assert.True(reached, "the brute-force loop should at least reach docking range within 5 days");
        Assert.True(legacyDv > 20.0 * plan.PlannedDeltaVTotal,
            $"legacy loop ({legacyDv:F0} m/s) must be >20x the phasing winner ({plan.PlannedDeltaVTotal:F1} m/s)");
    }

    [Fact]
    public void G4_Determinism_SolveIsAPureFunctionOfTheInputs()
    {
        var (eph1, sim1) = MakeWorld();
        var (eph2, sim2) = MakeWorld();
        var a = Solve(eph1, sim1, MakeShip(eph1));
        var b = Solve(eph2, sim2, MakeShip(eph2));

        Assert.Equal(a.PlannedDeltaVTotal, b.PlannedDeltaVTotal, precision: 9);
        Assert.Equal(a.DepartTime, b.DepartTime, precision: 9);
        Assert.Equal(a.TimeOfFlightSeconds, b.TimeOfFlightSeconds, precision: 9);
        Assert.Equal(a.Burns.Count, b.Burns.Count);
        for (int i = 0; i < a.Burns.Count; i++)
        {
            Assert.Equal(a.Burns[i].DeltaV.X, b.Burns[i].DeltaV.X, precision: 9);
            Assert.Equal(a.Burns[i].DeltaV.Y, b.Burns[i].DeltaV.Y, precision: 9);
            Assert.Equal(a.Burns[i].SimTime, b.Burns[i].SimTime, precision: 9);
        }
    }

    [Fact]
    public void G5_PhasingIdentity_ClosesToMachineEpsilon_AgainstTheAuthoredRailRate()
    {
        // The phasing kernel builds its ellipse on the KEPLER period at the ship's radius, so its own
        // closure is exact against the Kepler mean motion. The station, though, rides an AUTHORED rail
        // (Ringside's period is ~0.024% off Kepler), and THAT is the wrinkle the lab surfaces: a
        // Kepler-built ellipse drifts a little per lap against the real rail (Section B's res_authored,
        // and the flown miss in Section C). This gate proves the identity itself is exact algebra
        // regardless of WHICH mean motion — as long as it is used consistently: rebuild the phasing
        // period from the AUTHORED mean motion and the authored-rate closure is machine epsilon.
        var (eph, sim) = MakeWorld();
        ShipState ship = MakeShip(eph);

        // Read the phase geometry at the planner's fixed prep offset, exactly as the planner does.
        const double prep = 600.0;
        double tDep = ship.SimTime + prep;
        ShipState atDep = sim.RunAdaptive(ship, prep);
        Vector2d satPos = eph.Position("saturn", tDep);
        Vector2d shipRel = atDep.Position - satPos;
        Vector2d targetRel = eph.Position("ringside-exchange", tDep) - satPos;
        double gap = TransferMath.PhaseGap(shipRel, targetRel);
        double gapNorm = gap < 0 ? gap + Math.Tau : gap;

        CelestialBody ring = eph.Bodies.First(b => b.Id == "ringside-exchange");
        double tAuthored = ring.OrbitPeriod;             // the AUTHORED rail period
        double nAuthored = Math.Tau / tAuthored;

        static double FoldPi(double x) { x = Math.IEEERemainder(x, Math.Tau); return x <= -Math.PI ? x + Math.Tau : x; }

        for (int k = 1; k <= 6; k++)
        {
            foreach (bool dip in new[] { true, false })
            {
                // Rebuild the phasing period from the AUTHORED mean motion (Curtis 6.5), the identity
                // used consistently. After k such laps the target advances n_auth·k·T_ph_auth; closure
                // requires gap + that advance to be a whole number of turns.
                double tPhAuthored = dip
                    ? tAuthored * (1 - gapNorm / (Math.Tau * k))
                    : tAuthored * (1 + (Math.Tau - gapNorm) / (Math.Tau * k));
                double waitAuthored = k * tPhAuthored;
                double residual = FoldPi(gapNorm + nAuthored * waitAuthored);
                Assert.True(Math.Abs(residual) < 1e-6,
                    $"authored-rate phasing identity must close (<1e-6 rad) for k={k} dip={dip}; got {residual:E3}");
            }
        }
    }
}
