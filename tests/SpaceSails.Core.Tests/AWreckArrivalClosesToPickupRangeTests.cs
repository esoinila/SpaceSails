namespace SpaceSails.Core.Tests;

/// <summary>
/// #244 item 2 · THE ENVELOPE IS NOT THE DESTINATION.
///
/// <para>Owner, arrived at the roadster: <i>"I think we dropped out of autopilot… did we miss the dock
/// button press while warping? We should have autodock enabled because of our love of warp."</i> Read off
/// the live frame, the autopilot had SUCCEEDED — envelope stand-down, 499,721 km, rel 4.0 — and for a WRECK
/// that success was wrong twice. #1104 fixed the first half: no clamp is promised where no clamp exists.
/// This is the second. <b>A fetch pickup is proximity at a three-metre object, and stopping half a million
/// kilometres out is a car-park in the next county.</b></para>
///
/// <para>The fix is one predicate. <see cref="DockRule.Arrived"/> asks
/// <see cref="DockableHavens.IsDockable"/> — the very gate the ⚓ button obeys — and takes the arrival range
/// from the answer: the clamp's own reach where there is an arm to throw, and the errand's range where there
/// is not. The errand's range is not a new number; it is the one the fetch pickup has always used, hoisted
/// into Core so the trip now ends exactly where the work happens.</para>
///
/// <para>Both halves of the machinery ask it: the arm-time rehearsal that PRICES the last mile and the live
/// loop that FLIES it. This file holds the rehearsal end — the one that can be flown in Core — and the
/// client's <c>TheArrivalEndsWhereTheErrandIsTests</c> holds the page's.</para>
/// </summary>
public class AWreckArrivalClosesToPickupRangeTests
{
    private const int Tank = 500;

    private static (Simulator Sim, ICelestialEphemeris Eph) Sol()
    {
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        return (new Simulator(eph, timeStepSeconds: 60), eph);
    }

    private static CelestialBody Body(ICelestialEphemeris eph, string id) =>
        eph.Bodies.First(b => b.Id == id);

    private static Vector2d BodyVel(ICelestialEphemeris eph, string id, double t) =>
        (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

    // ── THE PREMISE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE TWO CLASSES REALLY ARE TWO. A scenario in which every μ=0 berth were clampable would make the
    /// fork below unreachable and every law here vacuous — and the arrival range for a wreck has to be the
    /// pickup's own, or the trip still ends somewhere the errand does not happen.
    /// </summary>
    [Fact]
    public void ThePremise_TheScenarioHasBothClassesAndTheyArriveAtDifferentRanges()
    {
        (_, ICelestialEphemeris eph) = Sol();

        CelestialBody clampable = eph.Bodies.First(DockableHavens.IsDockable);
        CelestialBody wreck = Body(eph, Derelict.RoadsterBodyId);
        Assert.False(DockableHavens.IsDockable(wreck));

        Assert.Equal(DockRule.EnvelopeMeters, DockRule.ArrivalRangeMeters(clampable));
        Assert.Equal(DockRule.AlongsideMeters, DockRule.ArrivalRangeMeters(wreck));
        Assert.True(DockRule.AlongsideMeters < DockRule.EnvelopeMeters,
            "the two ranges are the same number, so nothing about a wreck's arrival has actually changed");
    }

    // ── THE LAW ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// AT HALF A MILLION KILOMETRES OFF A WRECK, THE SHIP HAS NOT ARRIVED. The distance the owner was left
    /// at, matched at the speed he was matched at: the old test said yes, and that is the bug.
    /// </summary>
    [Fact]
    public void TheOwnersOwnStandDownDistanceIsNoLongerAnArrivalAtAWreck()
    {
        (_, ICelestialEphemeris eph) = Sol();
        CelestialBody wreck = Body(eph, Derelict.RoadsterBodyId);
        Vector2d at = eph.Position(wreck.Id, 0);
        Vector2d vel = BodyVel(eph, wreck.Id, 0);

        // 499,721 km, rel 4.0 m/s — the live frame, to the metre.
        var whereHeStopped = new ShipState(at + new Vector2d(4.99721e8, 0), vel + new Vector2d(4.0, 0), 0);

        Assert.True(DockRule.InEnvelope(whereHeStopped, at, vel, wreck.BodyRadius),
            "the old test does not even accept the owner's own frame — this bench is measuring the wrong thing");
        Assert.False(DockRule.Arrived(whereHeStopped, at, vel, wreck));

        // …and inside the pickup range it has.
        var alongside = new ShipState(at + new Vector2d(DockRule.AlongsideMeters * 0.5, 0), vel, 0);
        Assert.True(DockRule.Arrived(alongside, at, vel, wreck));
    }

    /// <summary>
    /// AND NOTHING MOVED AT A REAL BERTH. Every clampable haven in the scenario arrives exactly where it
    /// always did — a fix that tightened the whole game's docking by five would be a far bigger bug than the
    /// one it cured, and the ⚓ arm reaches what it reaches.
    /// </summary>
    [Fact]
    public void EveryBerthWithAClampArrivesExactlyWhereItAlwaysDid()
    {
        (_, ICelestialEphemeris eph) = Sol();
        int checkedBerths = 0;

        foreach (CelestialBody berth in eph.Bodies.Where(b => b.Kind == BodyKind.Station))
        {
            Vector2d at = eph.Position(berth.Id, 0);
            Vector2d vel = BodyVel(eph, berth.Id, 0);
            bool clampable = DockableHavens.IsDockable(berth);

            foreach (double range in new[] { 1e7, 5e7, 2e8, 4.5e8, 6e8, 5e9 })
            {
                var ship = new ShipState(at + new Vector2d(range, 0), vel, 0);
                bool old = DockRule.InEnvelope(ship, at, vel, berth.BodyRadius);
                bool now = DockRule.Arrived(ship, at, vel, berth);

                if (clampable)
                {
                    Assert.Equal(old, now);
                }
                else if (range > DockRule.AlongsideMeters)
                {
                    Assert.False(now, $"{berth.Id} at {range:0} m is still called arrived");
                }
            }

            checkedBerths++;
        }

        Assert.True(checkedBerths >= 10, $"only {checkedBerths} berths swept — this law proved little");
    }

    /// <summary>
    /// TOO FAST IS STILL TOO FAST. The arrival is matched-and-alongside, not merely near: a ship tearing
    /// past a wreck at ten times the match speed has not come alongside it, however close it got.
    /// </summary>
    [Fact]
    public void CloseButHotIsNotAnArrival()
    {
        (_, ICelestialEphemeris eph) = Sol();
        CelestialBody wreck = Body(eph, Derelict.RoadsterBodyId);
        Vector2d at = eph.Position(wreck.Id, 0);
        Vector2d vel = BodyVel(eph, wreck.Id, 0);

        var tearingPast = new ShipState(
            at + new Vector2d(DockRule.AlongsideMeters * 0.2, 0),
            vel + new Vector2d(DockRule.MatchSpeed * 10, 0), 0);

        Assert.False(DockRule.Arrived(tearingPast, at, vel, wreck));
    }

    /// <summary>
    /// AND THE PROMISE IS FLOWN. The arm-time rehearsal is what tells the captain the trip is deliverable
    /// and what it costs; if it still declared victory at the envelope, the estimate and the journey would be
    /// about two different destinations. So this puts the ship exactly where the owner was left — inside the
    /// clamp envelope, matched, outside pickup range — and asks the rehearsal whether it is there yet.
    ///
    /// <para>Read as SIM SECONDS rather than as a distance on purpose: the rehearsal returns the instant its
    /// terminal test passes, so "0 seconds" IS "it thinks it has already arrived". At a wreck it must fly on;
    /// at a berth with a clamp it must not, because the arm really does reach that far.</para>
    /// </summary>
    [Fact]
    public void TheRehearsalFliesOnPastTheEnvelopeAtAWreckAndStopsThereAtARealBerth()
    {
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);

        double InEnvelopeButNotAlongside(CelestialBody body)
        {
            Vector2d at = eph.Position(body.Id, 0);
            Vector2d vel = BodyVel(eph, body.Id, 0);
            var ship = new ShipState(at + new Vector2d(DockRule.EnvelopeMeters * 0.9, 0), vel, 0);

            AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
                ship, eph, sim, body.Id, budget, maxHorizonSeconds: 20 * 86400.0);
            Assert.True(r.Captured,
                $"the rehearsal cannot promise {body.Id} from inside its own envelope "
                + $"(budget exceeded: {r.BudgetExceeded}, horizon reached: {r.HorizonReached})");
            return r.SimDurationSeconds;
        }

        CelestialBody wreck = Body(eph, Derelict.RoadsterBodyId);
        CelestialBody berth = eph.Bodies.First(DockableHavens.IsDockable);

        Assert.Equal(0.0, InEnvelopeButNotAlongside(berth));
        Assert.True(InEnvelopeButNotAlongside(wreck) > 0,
            "the rehearsal calls the wreck arrived at 450,000 km — five times the range the fetch pickup "
            + "works at, so the promise is about a place the errand does not happen.");
    }

    /// <summary>
    /// …AND IT REALLY DOES GET THERE. Refusing to stop is not the same as arriving. Flown from the standing
    /// the owner was left in, with the path captured, the LAST place the rehearsal put the ship must be
    /// inside the range the fetch pickup works at — otherwise the estimate promises a delivery the journey
    /// does not make.
    /// </summary>
    [Fact]
    public void TheRehearsedTripAtAWreckReallyClosesToPickupRange()
    {
        (Simulator sim, ICelestialEphemeris eph) = Sol();
        CelestialBody wreck = Body(eph, Derelict.RoadsterBodyId);

        Vector2d at = eph.Position(wreck.Id, 0);
        Vector2d vel = BodyVel(eph, wreck.Id, 0);
        var ship = new ShipState(at + new Vector2d(DockRule.EnvelopeMeters * 0.9, 0), vel, 0);

        AutopilotRehearsal.RehearsalResult result = AutopilotRehearsal.Rehearse(
            ship, eph, sim, wreck.Id, Tank - AutopilotRehearsal.ReservePulses(Tank),
            capturePath: true, maxHorizonSeconds: 20 * 86400.0);

        Assert.True(result.Captured, "the rehearsal never promised the wreck at all");
        Assert.True(result.Path.Count > 2, "no path was captured — nothing here is being measured");

        TrajectorySample last = result.Path[^1];
        Assert.Equal(result.SimDurationSeconds, last.SimTime, 0);   // the path really reaches the arrival

        double miss = (last.Position - eph.Position(wreck.Id, last.SimTime)).Length;
        Assert.True(miss <= DockRule.AlongsideMeters,
            $"the rehearsed trip ends {miss / 1000:N0} km off the wreck — outside the "
            + $"{DockRule.AlongsideMeters / 1000:N0} km the fetch pickup works at, so the autopilot delivers "
            + "the ship to a place the errand does not happen.");
    }
}
