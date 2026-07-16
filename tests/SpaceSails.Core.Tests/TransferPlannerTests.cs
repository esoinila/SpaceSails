using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for <see cref="TransferPlanner"/> — the #146 in-well moon-run planner. The subset under
/// test is the sol.json Saturn system reduced to Saturn + Titan + Enceladus (the exact scenario
/// numbers, lines 20-22). The headline gate flies the planner's own departure burn through the real
/// <see cref="Simulator"/> and asserts the arc genuinely closes on Titan — Lambert proposes, the
/// integrator disposes. Bands not exact trajectories; refusals must carry verbatim reasons.
/// </summary>
public class TransferPlannerTests
{
    private const double SaturnMu = 3.7931187e16;
    private const double TitanMu = 8.9781e12;
    private const double EnceladusMu = 7.211e9;

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

    // A ship free-flying just outside Enceladus's Hill sphere, riding Enceladus's orbit about Saturn
    // — the Enceladus-doorstep departure the lab flies. Offset radially so the planner is not (rightly)
    // refused for sitting inside Enceladus's Hill sphere.
    private static ShipState EnceladusDoorstep(ICelestialEphemeris eph, double t0 = 0)
    {
        Vector2d encPos = eph.Position("enceladus", t0);
        Vector2d encVel = TransferMath.BodyVelocity(eph, "enceladus", t0);
        Vector2d outward = encPos.Normalized();
        return new ShipState(encPos + outward * 2.0e6, encVel, t0);
    }

    private static double TitanHill(ICelestialEphemeris eph)
    {
        CelestialBody titan = eph.Bodies.First(b => b.Id == "titan");
        return OrbitRule.HillRadius(titan, SaturnMu);
    }

    [Fact]
    public void FindsACheapCapturablePlan_WellUnderTheStatusQuo()
    {
        var (eph, sim) = MakeSaturnSystem();
        var req = new TransferPlanner.Request(EnceladusDoorstep(eph), "saturn", "titan", MaxWaitSeconds: 0);

        var result = TransferPlanner.Solve(sim, eph, req);

        Assert.True(result.Ok, $"planner must find a plan (failure: {result.Failure})");
        Assert.Single(result.Burns);
        // The whole point of #146: ~6 km/s honest, vs the ~33 km/s Approach-loop hemorrhage.
        Assert.True(result.PlannedDeltaVTotal < 8000,
            $"plan must beat 8 km/s (was {result.PlannedDeltaVTotal / 1000:F2} km/s)");
        // Arrival must be capturable by the existing OrbitRule machinery.
        Assert.True(result.ArrivalRelativeSpeed < OrbitRule.MaxRelativeSpeed,
            $"arrival must be under the capture speed cap (was {result.ArrivalRelativeSpeed / 1000:F2} km/s)");
        Assert.True(result.EstimatedPulses > 0, "an honest pulse estimate must be quoted");
        Assert.False(string.IsNullOrEmpty(result.Summary));
    }

    [Fact]
    public void PlannersBurn_FlownThroughTheRealSimulator_ClosesOnTitan()
    {
        // The house gate: fly the planner's ONE departure burn through the deterministic N-body
        // Simulator and assert the ballistic arc's closest approach to Titan falls inside Titan's
        // capture range — the pocket a < 200 m/s mid-course would close (lab 17). Lambert proposes,
        // the integrator disposes.
        var (eph, sim) = MakeSaturnSystem();
        ShipState ship = EnceladusDoorstep(eph);
        var result = TransferPlanner.Solve(sim, eph, new TransferPlanner.Request(ship, "saturn", "titan", MaxWaitSeconds: 0));
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
            double d = (eph.Position("titan", s.SimTime) - s.Position).Length;
            closest = Math.Min(closest, d);
        }

        double captureRange = OrbitRule.CaptureRange(TitanHill(eph));
        Assert.True(closest < captureRange,
            $"flown arc must reach Titan's capture range: closest {closest / 1e6:F0} Mm vs range {captureRange / 1e6:F0} Mm");
    }

    [Fact]
    public void Deterministic_TwoRunsAreByteIdentical()
    {
        var (eph1, sim1) = MakeSaturnSystem();
        var (eph2, sim2) = MakeSaturnSystem();
        var r1 = TransferPlanner.Solve(sim1, eph1, new TransferPlanner.Request(EnceladusDoorstep(eph1), "saturn", "titan", 0));
        var r2 = TransferPlanner.Solve(sim2, eph2, new TransferPlanner.Request(EnceladusDoorstep(eph2), "saturn", "titan", 0));

        Assert.True(r1.Ok && r2.Ok);
        Assert.Equal(r1.DepartTime, r2.DepartTime, precision: 9);
        Assert.Equal(r1.TimeOfFlightSeconds, r2.TimeOfFlightSeconds, precision: 9);
        Assert.Equal(r1.PlannedDeltaVTotal, r2.PlannedDeltaVTotal, precision: 9);
        Assert.Equal(r1.Burns[0].DeltaV.X, r2.Burns[0].DeltaV.X, precision: 9);
        Assert.Equal(r1.Burns[0].DeltaV.Y, r2.Burns[0].DeltaV.Y, precision: 9);
        Assert.Equal(r1.EstimatedPulses, r2.EstimatedPulses);
    }

    [Fact]
    public void Refuses_TargetThatDoesNotOrbitTheNamedParent()
    {
        var (eph, sim) = MakeSaturnSystem();
        // Enceladus orbits Saturn, not Titan — the planner must refuse with a specific reason.
        var req = new TransferPlanner.Request(EnceladusDoorstep(eph), "titan", "enceladus", 0);

        var result = TransferPlanner.Solve(sim, eph, req);

        Assert.False(result.Ok);
        Assert.Contains("does not orbit", result.Failure);
        Assert.Empty(result.Burns);
    }

    [Fact]
    public void Refuses_ShipInsideAnotherMoonsHillSphere()
    {
        var (eph, sim) = MakeSaturnSystem();
        // Sitting ON Enceladus (distance 0 < its Hill radius) while trying to plan to Titan.
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d encVel = TransferMath.BodyVelocity(eph, "enceladus", 0);
        var ship = new ShipState(encPos, encVel, 0);

        var result = TransferPlanner.Solve(sim, eph, new TransferPlanner.Request(ship, "saturn", "titan", 0));

        Assert.False(result.Ok);
        Assert.Contains("Hill sphere", result.Failure);
        Assert.Contains("Enceladus", result.Failure);
    }

    [Fact]
    public void Refuses_WhenTheBestPlanExceedsTheDeltaVCeiling()
    {
        var (eph, sim) = MakeSaturnSystem();
        // A punishingly low ceiling: even the cheapest ~6 km/s plan is over it.
        var req = new TransferPlanner.Request(EnceladusDoorstep(eph), "saturn", "titan", MaxWaitSeconds: 0, MaxDeltaV: 100);

        var result = TransferPlanner.Solve(sim, eph, req);

        Assert.False(result.Ok);
        Assert.Contains("ceiling", result.Failure);
        Assert.Empty(result.Burns);
    }

    [Fact]
    public void Refuses_UnknownBody()
    {
        var (eph, sim) = MakeSaturnSystem();
        var result = TransferPlanner.Solve(sim, eph, new TransferPlanner.Request(EnceladusDoorstep(eph), "saturn", "pluto", 0));

        Assert.False(result.Ok);
        Assert.Contains("no body named 'pluto'", result.Failure);
    }
}
