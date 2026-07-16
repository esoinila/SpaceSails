using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for the co-orbital rendezvous mode (#155, Lab 24 "The last mile"). The headline case is
/// the owner's stranded arm: 92,640 km from Ringside Exchange on nearly the same Saturn lane, where
/// #152's Lambert porkchop is structurally blind (a phasing loop returns to its own start = the 2π
/// singularity) and the legacy autopilot DECLINED at ≈229 p. Rendezvous mode prices the closed-form
/// phasing bus instead and quotes a cheaper-vs-sooner trade table. As everywhere in the planner:
/// Curtis proposes (the phasing algebra), the real <see cref="Simulator"/> disposes (the flown gate).
/// </summary>
public class TransferPlannerRendezvousTests
{
    private const double SaturnMu = 3.7931187e16;

    // The Saturn subset with the Ringside Exchange station on its sol.json rail (line 23: μ=0 station,
    // orbitRadius 1.35e9, period 1.6006e6). Titan and Enceladus are present so the "another moon's
    // Hill sphere" guard and the co-orbital detector run against a realistic system.
    private static (ICelestialEphemeris Eph, Simulator Sim) MakeRingsideSystem()
    {
        var bodies = new[]
        {
            new CelestialBody("saturn", "Saturn", null, SaturnMu, 5.8232e7, 0, 0, 0),
            new CelestialBody("titan", "Titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
            new CelestialBody("enceladus", "Enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon, IsHaven: true),
            new CelestialBody("ringside-exchange", "Ringside Exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station, IsHaven: true),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    // The exact #152 moon-run subset (Saturn at the origin, Titan + Enceladus on their rails) — the
    // system the porkchop numbers were verified against, so "moon-run unaffected" compares like-for-like.
    private static (ICelestialEphemeris Eph, Simulator Sim) MakeMoonSystem()
    {
        var bodies = new[]
        {
            new CelestialBody("saturn", "Saturn", null, SaturnMu, 5.8232e7, 0, 0, 0),
            new CelestialBody("titan", "Titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
            new CelestialBody("enceladus", "Enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon, IsHaven: true),
        };
        var eph = new CircularOrbitEphemeris(bodies);
        return (eph, new Simulator(eph, timeStepSeconds: 60));
    }

    /// <summary>A ship on the exchange's circular rail, <paramref name="behindMeters"/> along-track
    /// BEHIND the station (the station leads it), moving prograde (CCW) at circular speed. Reversing
    /// the tangent gives the retrograde ship the rails forbid.</summary>
    private static ShipState RingsideShip(ICelestialEphemeris eph, double behindMeters = 9.264e7, bool retrograde = false)
    {
        Vector2d ringPos = eph.Position("ringside-exchange", 0);
        double ringR = ringPos.Length;
        double ringAngle = Math.Atan2(ringPos.Y, ringPos.X);
        double shipAngle = ringAngle - behindMeters / ringR; // trailing the CCW station
        Vector2d pos = new Vector2d(Math.Cos(shipAngle), Math.Sin(shipAngle)) * ringR;
        double vCirc = Math.Sqrt(SaturnMu / ringR);
        Vector2d tangent = new Vector2d(-Math.Sin(shipAngle), Math.Cos(shipAngle)); // CCW prograde
        Vector2d vel = tangent * (retrograde ? -vCirc : vCirc);
        return new ShipState(pos, vel, 0);
    }

    [Fact]
    public void Ringside_CoOrbital_QuotesACheapPhasingBus_InsteadOfDecliningAt229Pulses()
    {
        var (eph, sim) = MakeRingsideSystem();
        var req = new TransferPlanner.Request(RingsideShip(eph), "saturn", "ringside-exchange", MaxWaitSeconds: 0);

        TransferPlanner.Result r = TransferPlanner.Solve(sim, eph, req);

        Assert.True(r.Ok, $"rendezvous must quote a plan, not decline (failure: {r.Failure})");
        // The whole point of #155: the last mile is ~1 pulse of Δv, not the legacy 229-p refusal.
        Assert.True(r.PlannedDeltaVTotal < 100,
            $"winner must be under 100 m/s (was {r.PlannedDeltaVTotal:F1} m/s)");
        Assert.Equal(2, r.Burns.Count); // enter the phasing ellipse, re-match on return
        Assert.StartsWith("phasing", r.Alternatives[0].Label); // the winner is a phasing bus, row 0
        Assert.True(r.Alternatives.Count >= 3, $"a trade table, not one answer (was {r.Alternatives.Count})");

        // The dip family (chase the leader) must offer strictly-increasing waits — catch this bus or
        // the next one — so the sooner-vs-cheaper choice is real.
        var dipWaits = r.Alternatives
            .Where(a => a.Label.Contains("(dip)"))
            .OrderBy(a => a.WaitSeconds)
            .Select(a => a.WaitSeconds)
            .ToList();
        Assert.True(dipWaits.Count >= 3, "the dip family should span several lap counts");
        for (int i = 1; i < dipWaits.Count; i++)
        {
            Assert.True(dipWaits[i] > dipWaits[i - 1], "each extra lap must cost strictly more wait");
        }
    }

    [Fact]
    public void Ringside_BothBurnsFlownThroughTheRealSimulator_CloseOnTheStation()
    {
        // The house gate: apply BOTH phasing burns at their epochs through the deterministic N-body
        // Simulator, coast to the planned arrival, and assert the ship is genuinely on the station's
        // doorstep — within the two-body-lie margin (5e8 m, 100 m/s). Curtis proposes, the sim disposes.
        var (eph, sim) = MakeRingsideSystem();
        ShipState ship = RingsideShip(eph);
        TransferPlanner.Result r = TransferPlanner.Solve(
            sim, eph, new TransferPlanner.Request(ship, "saturn", "ringside-exchange", MaxWaitSeconds: 0));
        Assert.True(r.Ok, $"planner must find a plan (failure: {r.Failure})");

        double arrivalTime = r.ToSchedule().ArrivalTime;
        ShipState s = ship;
        foreach (TransferPlanner.BurnStep b in r.Burns)
        {
            if (b.SimTime > s.SimTime)
            {
                s = sim.RunAdaptive(s, b.SimTime - s.SimTime, maxTimeStep: 900);
            }

            s = s with { Velocity = s.Velocity + b.DeltaV };
        }

        if (arrivalTime > s.SimTime)
        {
            s = sim.RunAdaptive(s, arrivalTime - s.SimTime, maxTimeStep: 1800);
        }

        Vector2d stationPos = eph.Position("ringside-exchange", arrivalTime);
        Vector2d stationVel = TransferMath.BodyVelocity(eph, "ringside-exchange", arrivalTime);
        double miss = (stationPos - s.Position).Length;
        double relSpeed = (s.Velocity - stationVel).Length;

        Assert.True(miss < 5e8, $"flown arc must close on the station: miss {miss / 1e6:F1} Mm");
        Assert.True(relSpeed < 100, $"flown arc must arrive slow: {relSpeed:F1} m/s rel");
    }

    [Fact]
    public void Ringside_Deterministic_TwoRunsAreByteIdentical()
    {
        var (eph1, sim1) = MakeRingsideSystem();
        var (eph2, sim2) = MakeRingsideSystem();
        var req1 = new TransferPlanner.Request(RingsideShip(eph1), "saturn", "ringside-exchange", 0);
        var req2 = new TransferPlanner.Request(RingsideShip(eph2), "saturn", "ringside-exchange", 0);

        TransferPlanner.Result r1 = TransferPlanner.Solve(sim1, eph1, req1);
        TransferPlanner.Result r2 = TransferPlanner.Solve(sim2, eph2, req2);

        Assert.True(r1.Ok && r2.Ok);
        Assert.Equal(r1.PlannedDeltaVTotal, r2.PlannedDeltaVTotal, precision: 9);
        Assert.Equal(r1.TimeOfFlightSeconds, r2.TimeOfFlightSeconds, precision: 9);
        Assert.Equal(r1.Burns[0].DeltaV.X, r2.Burns[0].DeltaV.X, precision: 9);
        Assert.Equal(r1.Burns[1].DeltaV.Y, r2.Burns[1].DeltaV.Y, precision: 9);
        Assert.Equal(r1.EstimatedPulses, r2.EstimatedPulses);
        Assert.Equal(r1.Alternatives.Count, r2.Alternatives.Count);
        Assert.Equal(r1.Alternatives[0].Label, r2.Alternatives[0].Label);
    }

    [Fact]
    public void Ringside_RefusesARetrogradeShip_TheRailsRunCounterClockwise()
    {
        var (eph, sim) = MakeRingsideSystem();
        ShipState retro = RingsideShip(eph, retrograde: true);

        TransferPlanner.Result r = TransferPlanner.Solve(
            sim, eph, new TransferPlanner.Request(retro, "saturn", "ringside-exchange", MaxWaitSeconds: 0));

        Assert.False(r.Ok);
        Assert.Contains("retrograde", r.Failure);
        Assert.Empty(r.Burns);
        Assert.Empty(r.Alternatives);
    }

    [Fact]
    public void MoonToMoon_Unaffected_StillPicksThe152PorkchopHop()
    {
        // Enceladus->Titan is NOT co-orbital (radii differ by ~5x), so rendezvous mode never fires and
        // the #146/#152 porkchop path stays byte-identical — one departure burn, the same ~6 km/s bill,
        // arrival capturable — now carrying a single-row "direct hop" trade table.
        var (eph, sim) = MakeMoonSystem();
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d encVel = TransferMath.BodyVelocity(eph, "enceladus", 0);
        var ship = new ShipState(encPos + encPos.Normalized() * 2.0e6, encVel, 0);

        TransferPlanner.Result r = TransferPlanner.Solve(
            sim, eph, new TransferPlanner.Request(ship, "saturn", "titan", MaxWaitSeconds: 0));

        Assert.True(r.Ok, $"the moon run must still solve (failure: {r.Failure})");
        Assert.Single(r.Burns); // the porkchop emits ONE departure burn, not the two-burn phasing schedule
        Assert.True(r.PlannedDeltaVTotal < 8000,
            $"still the ~6 km/s Hohmann-scale hop (was {r.PlannedDeltaVTotal / 1000:F2} km/s)");
        Assert.True(r.ArrivalRelativeSpeed < OrbitRule.MaxRelativeSpeed);
        Assert.Single(r.Alternatives);
        Assert.Equal("direct hop", r.Alternatives[0].Label);
        Assert.Equal(r.PlannedDeltaVTotal, r.Alternatives[0].DeltaVTotal, precision: 9);
    }
}
