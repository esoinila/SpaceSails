using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for Lab 23 — The moon run (the #146 in-well transfer, taught). Lab-19-gate style:
/// an inline system factory, invariant BANDS not exact numbers, and every assert independent of
/// the probe (so the lesson's prose can evolve without touching these). The system is the sol.json
/// Saturn subset (Saturn parentless at the origin + Titan + Enceladus on their rails), and the
/// doorstep is the free-flying Enceladus departure the lab flies. The headline gate (G2) measures
/// the old reset loop's hemorrhage against the planner's quote from the same doorstep.
/// See labs/23-the-moon-run/README.md and Probe.cs for the lesson.
/// </summary>
public class Lab23MoonRunTests
{
    private const double SaturnMu = 3.7931187e16;
    private const double TitanMu = 8.9781e12;
    private const double EnceladusMu = 7.211e9;
    private const double Day = 86400.0;

    // Saturn parentless at the origin; Titan and Enceladus on their sol.json rails about it.
    private static (ICelestialEphemeris Eph, Simulator Sim) MakeSaturnSystem()
    {
        var bodies = new[]
        {
            new CelestialBody("saturn", "Saturn", null, SaturnMu, 5.8232e7, 0, 0, 0),
            new CelestialBody("titan", "Titan", "saturn", TitanMu, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
            new CelestialBody("enceladus", "Enceladus", "saturn", EnceladusMu, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon, IsHaven: true),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    // The Enceladus doorstep: riding Enceladus's rail velocity, offset radially outward off her
    // surface line so the planner is not (rightly) refused for sitting inside her Hill sphere.
    private static ShipState EnceladusDoorstep(ICelestialEphemeris eph, double t0 = 0)
    {
        Vector2d encPos = eph.Position("enceladus", t0);
        Vector2d encVel = TransferMath.BodyVelocity(eph, "enceladus", t0);
        Vector2d outward = encPos.Normalized();
        return new ShipState(encPos + outward * 3e6, encVel, t0);
    }

    private static double TitanHill(ICelestialEphemeris eph) =>
        OrbitRule.HillRadius(eph.Bodies.First(b => b.Id == "titan"), SaturnMu);

    // The old reset loop, reduced to a gate primitive: the exact OrbitRule dispatch the live
    // autopilot ran on Wednesday, flown from an arbitrary start until Insert or a horizon. Counts
    // pulses at the pre-burn state, caps iterations sanely. Independent of the probe's own copy.
    private static (int Pulses, bool Inserted) FlyOldLoop(
        ICelestialEphemeris eph, Simulator sim, ShipState ship, string targetId, double horizonDays)
    {
        CelestialBody body = eph.Bodies.First(b => b.Id == targetId);
        CelestialBody parent = eph.Bodies.First(b => b.Id == body.ParentId);
        double hill = OrbitRule.HillRadius(body, parent.Mu);
        double captureRange = OrbitRule.CaptureRange(hill);
        double horizon = ship.SimTime + horizonDays * Day;
        int pulses = 0, iter = 0;
        while (ship.SimTime < horizon && iter++ < 40_000)
        {
            Vector2d bodyPos = eph.Position(body.Id, ship.SimTime);
            Vector2d bodyVel = TransferMath.BodyVelocity(eph, body.Id, ship.SimTime);
            OrbitRule.ApproachObstacle? obstacle = parent.ParentId is null
                ? null
                : new OrbitRule.ApproachObstacle(
                    eph.Position(parent.Id, ship.SimTime), parent.BodyRadius * OrbitRule.ParentSafeBodyRadii);

            switch (OrbitRule.AutopilotDecision(ship, bodyPos, bodyVel, body, hill))
            {
                case OrbitRule.AutopilotAction.Approach:
                    pulses += OrbitRule.ApproachPulseCost(ship, bodyPos, bodyVel, body, obstacle, hill);
                    ship = OrbitRule.Approach(ship, bodyPos, bodyVel, body, obstacle, hill);
                    ship = sim.RunAdaptive(ship, 60.0);
                    break;
                case OrbitRule.AutopilotAction.Insert:
                    pulses += OrbitRule.PulseCost(ship, bodyPos, bodyVel, body);
                    return (pulses, true);
                default:
                    double distance = (ship.Position - bodyPos).Length;
                    ship = sim.RunAdaptive(ship, distance > captureRange * 1.25 ? 1800.0 : 60.0);
                    break;
            }
        }

        return (pulses, false);
    }

    [Fact]
    public void G1_Planner_FindsACheapCapturablePlanFromTheDoorstep()
    {
        var (eph, sim) = MakeSaturnSystem();
        var result = TransferPlanner.Solve(sim, eph,
            new TransferPlanner.Request(EnceladusDoorstep(eph), "saturn", "titan", MaxWaitSeconds: 0));

        Assert.True(result.Ok, $"planner must find a plan (failure: {result.Failure})");
        Assert.True(result.PlannedDeltaVTotal < 8000,
            $"plan must beat 8 km/s (was {result.PlannedDeltaVTotal / 1000:F2} km/s)");
        Assert.True(result.ArrivalRelativeSpeed < 5000,
            $"arrival must be under 5 km/s (was {result.ArrivalRelativeSpeed / 1000:F2} km/s)");
    }

    [Fact]
    public void G2_OldResetLoop_CostsFarMoreThanThePlannerQuote()
    {
        var (eph, sim) = MakeSaturnSystem();
        ShipState doorstep = EnceladusDoorstep(eph);
        var plan = TransferPlanner.Solve(sim, eph,
            new TransferPlanner.Request(doorstep, "saturn", "titan", MaxWaitSeconds: 0));
        Assert.True(plan.Ok, $"planner must find a plan (failure: {plan.Failure})");

        var old = FlyOldLoop(eph, sim, doorstep, "titan", horizonDays: 40.0);

        // The #146 hemorrhage: the reset loop pays multiples of the planned quote. Band, not exact.
        Assert.True(old.Pulses > 3 * plan.EstimatedPulses,
            $"old reset loop ({old.Pulses} pulses) must cost > 3x the planner quote ({plan.EstimatedPulses} pulses)");
    }

    [Fact]
    public void G3_PlannersBurn_FlownBallistic_ReachesTitansCaptureRange()
    {
        var (eph, sim) = MakeSaturnSystem();
        ShipState ship = EnceladusDoorstep(eph);
        var result = TransferPlanner.Solve(sim, eph,
            new TransferPlanner.Request(ship, "saturn", "titan", MaxWaitSeconds: 0));
        Assert.True(result.Ok, $"planner must find a plan (failure: {result.Failure})");

        TransferPlanner.BurnStep burn = result.Burns[0];
        ShipState atDepart = burn.SimTime > ship.SimTime
            ? sim.RunAdaptive(ship, burn.SimTime - ship.SimTime, maxTimeStep: 900)
            : ship;
        var departed = atDepart with { Velocity = atDepart.Velocity + burn.DeltaV };

        IReadOnlyList<TrajectorySample> path = sim.ProjectAdaptive(
            departed, null, result.TimeOfFlightSeconds, maxTimeStep: 1800, maxSamples: 20_000);

        double closest = double.MaxValue;
        foreach (TrajectorySample s in path)
        {
            closest = Math.Min(closest, (eph.Position("titan", s.SimTime) - s.Position).Length);
        }

        double captureRange = OrbitRule.CaptureRange(TitanHill(eph));
        Assert.True(closest < captureRange,
            $"flown arc must reach Titan's capture range: closest {closest / 1e6:F0} Mm vs range {captureRange / 1e6:F0} Mm");
    }

    [Fact]
    public void G4_Determinism_TwoSolvesAreByteIdentical()
    {
        var (eph1, sim1) = MakeSaturnSystem();
        var (eph2, sim2) = MakeSaturnSystem();
        var r1 = TransferPlanner.Solve(sim1, eph1, new TransferPlanner.Request(EnceladusDoorstep(eph1), "saturn", "titan", 0));
        var r2 = TransferPlanner.Solve(sim2, eph2, new TransferPlanner.Request(EnceladusDoorstep(eph2), "saturn", "titan", 0));

        Assert.Equal(r1.Ok, r2.Ok);
        Assert.Equal(r1.Failure, r2.Failure);
        Assert.Equal(r1.DepartTime, r2.DepartTime, precision: 9);
        Assert.Equal(r1.TimeOfFlightSeconds, r2.TimeOfFlightSeconds, precision: 9);
        Assert.Equal(r1.ArrivalRelativeSpeed, r2.ArrivalRelativeSpeed, precision: 9);
        Assert.Equal(r1.PlannedDeltaVTotal, r2.PlannedDeltaVTotal, precision: 9);
        Assert.Equal(r1.EstimatedPulses, r2.EstimatedPulses);
        Assert.Equal(r1.Burns[0].SimTime, r2.Burns[0].SimTime, precision: 9);
        Assert.Equal(r1.Burns[0].DeltaV.X, r2.Burns[0].DeltaV.X, precision: 9);
        Assert.Equal(r1.Burns[0].DeltaV.Y, r2.Burns[0].DeltaV.Y, precision: 9);
    }

    [Fact]
    public void G5_Window_SynodicAndLeadAngleAreInBand()
    {
        var (eph, _) = MakeSaturnSystem();
        ShipState doorstep = EnceladusDoorstep(eph);
        double r1 = (doorstep.Position - eph.Position("saturn", 0)).Length;
        double r2 = eph.Bodies.First(b => b.Id == "titan").OrbitRadius;
        double titanPeriod = eph.Bodies.First(b => b.Id == "titan").OrbitPeriod;

        double shipPeriod = OrbitRule.LocalOrbitPeriod(r1, SaturnMu);
        double synodic = TransferMath.SynodicPeriod(shipPeriod, titanPeriod);
        double leadDeg = TransferMath.HohmannLeadAngle(r1, r2, SaturnMu) * 180 / Math.PI;

        double synodicHours = synodic / 3600;
        Assert.InRange(synodicHours, 35.0, 37.0);
        Assert.InRange(leadDeg, 0.0, 180.0);
    }
}
