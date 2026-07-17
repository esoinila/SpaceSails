namespace SpaceSails.Core;

/// <summary>
/// The autopilot's promise, kept honest at arm time (issues #146/#147). Before the ship commits to
/// an armed auto-orbit, this flies the WHOLE journey in Core with the exact same decision logic the
/// live tick loop uses — <see cref="OrbitRule.AutopilotDecision"/> → <see cref="OrbitRule.Approach"/>
/// / <see cref="OrbitRule.Insert"/>, the identical pattern the OrbitAutopilotTests e2e loops run —
/// and reports what it would actually cost: every approach burn plus the insertion, whether it ever
/// captures, and how long it takes. The owner's ruling was absolute: "dropping from autopilot is the
/// horrible scenario. It should never ever happen when there was nothing external to cause it." So
/// the whole feasibility question is settled HERE, on the click, not discovered mid-flight when the
/// tank runs dry at 10,000× warp.
///
/// <para><b>Fidelity, stated honestly.</b> This is a faithful predictor, not a bit-identical replay
/// of the live loop. It reuses the same OrbitRule primitives and the same fixed-frame ephemeris, but
/// (1) it coarsens the timestep far from the target — where the ship only coasts and no burn is ever
/// possible — to stay well under a second in WASM, dropping to a fine step once inside capture range
/// where the burns actually happen; and (2) the live loop's decision cadence rides the frame/warp
/// accumulator rather than a fixed step. Total pulse cost is proportional to total Δv, which is
/// robust to step size, so the headline number the arm-time contract reads is trustworthy; exact
/// burn COUNT can differ by a few. The generous reserve floor (<see cref="ReserveFraction"/>) is what
/// absorbs that residual error, so an accepted plan keeps a real cushion. The permanent fix — a
/// parent-frame transfer that flies the giant's well cheaply instead of fighting it — is the separate
/// follow-up lane (#146 stays open); this guard just refuses the trips that would strand you.</para>
/// </summary>
public static class AutopilotRehearsal
{
    /// <summary>The reserve the autopilot always keeps back, as a fraction of tank capacity. The
    /// arm-time contract refuses any journey whose rehearsed cost would eat into it, and the live
    /// loop stands down loudly rather than burn through it — so the ship is never stranded a burn
    /// short of anywhere. 18% of a 250-pulse tank is ~45 pulses: a real cushion that also soaks up
    /// the rehearsal's step-size error (see the fidelity note above).</summary>
    public const double ReserveFraction = 0.18;

    /// <summary>Reserve pulses for a tank of the given capacity (at least 1).</summary>
    public static int ReservePulses(int capacity) => Math.Max(1, (int)Math.Ceiling(ReserveFraction * capacity));

    /// <summary>Fine step (s) used once inside capture range, where the approach/insert burns
    /// happen — matches the 60 s the OrbitAutopilotTests e2e loops thread the window with.</summary>
    public const double FineStepSeconds = 60.0;

    /// <summary>Coarse step (s) used while merely coasting far outside capture range, where
    /// <see cref="OrbitRule.AutopilotDecision"/> can only return None — no burn is possible out
    /// there, so a wider step is honest and keeps a multi-day inbound cheap to rehearse.</summary>
    public const double CoastStepSeconds = 1800.0;

    /// <summary>Beyond this multiple of the capture range the ship is only coasting; step coarse.</summary>
    public const double CoastRangeFactor = 1.25;

    /// <summary>Hard horizon: give up predicting capture past this. A journey that cannot be shown
    /// to capture within it is treated as un-promisable and refused.</summary>
    public const double DefaultMaxHorizonSeconds = 120.0 * 86400.0;

    /// <summary>Hard iteration cap — the wall-time guard for the on-click WASM solve.</summary>
    public const int DefaultMaxIterations = 40_000;

    /// <param name="Captured">The rehearsal reached a bound insertion — the autopilot can deliver.</param>
    /// <param name="Pulses">Total mass pulses the whole journey would spend: every approach burn
    /// plus the insertion (the number the arm-time contract quotes).</param>
    /// <param name="ApproachBurns">How many approach burns it took (diagnostic; less trustworthy
    /// than <see cref="Pulses"/> across step sizes — see the fidelity note).</param>
    /// <param name="SimDurationSeconds">Sim time from arm to insertion (or to the cutoff).</param>
    /// <param name="BudgetExceeded">The rehearsed cost ran past the affordable budget before it
    /// could capture — the classic #146 deep-well fuel bleed. This is a refusal reason.</param>
    /// <param name="HorizonReached">Neither captured nor blew the budget within the horizon —
    /// also un-promisable, so also a refusal.</param>
    /// <param name="Path">The rehearsed trajectory (time-stamped), for drawing the autopilot's
    /// INTENDED path instead of the ballistic loops it will never fly (#148). May be empty when
    /// path capture is not requested.</param>
    public readonly record struct RehearsalResult(
        bool Captured,
        int Pulses,
        int ApproachBurns,
        double SimDurationSeconds,
        bool BudgetExceeded,
        bool HorizonReached,
        IReadOnlyList<TrajectorySample> Path)
    {
        /// <summary>The autopilot can honestly promise this journey: it captures within budget.</summary>
        public bool Deliverable => Captured && !BudgetExceeded && !HorizonReached;
    }

    /// <summary>
    /// Fly the armed journey forward and price it. Mirrors <c>CheckArmedInsertion</c>'s per-tick
    /// dispatch exactly: resolve the target's parent, Hill sphere and (for a moon) the parent-body
    /// chord obstacle, then loop AutopilotDecision → Approach/Insert, coasting between.
    /// </summary>
    /// <param name="ship">The state at the instant of arming.</param>
    /// <param name="ephemeris">The same rails the live sim flies.</param>
    /// <param name="simulator">Used only to advance the coast (its own timestep is irrelevant; the
    /// rehearsal picks the step and calls <see cref="Simulator.RunAdaptive"/>).</param>
    /// <param name="targetBodyId">The body being armed for.</param>
    /// <param name="budgetPulses">The most the journey may spend before it is declared unaffordable
    /// (the caller passes tank − reserve). Hitting it sets <see cref="RehearsalResult.BudgetExceeded"/>
    /// and stops early — which is exactly what makes the doomed Titan case cheap to rehearse.</param>
    /// <param name="capturePath">Record the trajectory for the #148 intended-path polyline.</param>
    /// <param name="schedule">The #146 in-well transfer plan from <see cref="TransferPlanner"/>, when
    /// the arm rode a cheap Lambert arc instead of the legacy approach loop. When present, the
    /// rehearsal FIRST flies the scheduled departure burn(s) — coasting exactly to each burn epoch,
    /// pricing the impulse with the same <see cref="OrbitRule.PulsesFor"/> the live loop spends, and
    /// bailing <see cref="RehearsalResult.BudgetExceeded"/> the moment the running cost clears the
    /// budget — then coasts with the capture decision GATED (never consulting
    /// <see cref="OrbitRule.AutopilotDecision"/> until the ship is honestly near the target), and only
    /// then falls into the unchanged terminal-capture loop below. The gate is the whole point: Titan's
    /// <see cref="OrbitRule.CaptureRangeFloorMeters"/> floor makes the ship "in capture range" for the
    /// entire Enceladus→Titan cruise, so an ungated decision would immediately Approach at ~7 km/s rel
    /// and restart the velocity-reset hemorrhage right through the cheap arc.</param>
    public static RehearsalResult Rehearse(
        ShipState ship,
        ICelestialEphemeris ephemeris,
        Simulator simulator,
        string targetBodyId,
        int budgetPulses,
        bool capturePath = false,
        double maxHorizonSeconds = DefaultMaxHorizonSeconds,
        int maxIterations = DefaultMaxIterations,
        TransferPlanner.Schedule? schedule = null)
    {
        var path = new List<TrajectorySample>(capturePath ? 512 : 0);
        void Record(ShipState s)
        {
            if (capturePath && path.Count < 4096)
            {
                path.Add(new TrajectorySample(s.SimTime, s.Position));
            }
        }

        CelestialBody? body = FindBody(ephemeris, targetBodyId);
        if (body?.ParentId is null)
        {
            // Nothing orbit-able (the sun, or a parentless station): not a journey the autopilot flies.
            return new RehearsalResult(false, 0, 0, 0, false, false, path);
        }

        // #155 the last mile: a station orbits its parent (μ=0) but is NEVER orbited in turn — its
        // terminal success is the dock envelope (DockRule), not OrbitRule.Insert. The gate/coast machinery
        // below is identical; only the "captured" test in the terminal loop differs (see isStation).
        bool isStation = body.Kind == BodyKind.Station;

        CelestialBody? parent = FindBody(ephemeris, body.ParentId);
        if (parent is null)
        {
            return new RehearsalResult(false, 0, 0, 0, false, false, path);
        }

        double hill = OrbitRule.HillRadius(body, parent.Mu);
        double captureRange = OrbitRule.CaptureRange(hill);
        double startTime = ship.SimTime;
        double endTime = startTime + maxHorizonSeconds;

        int pulses = 0, approachBurns = 0, iterations = 0;
        Record(ship);

        // #146 in-well transfer: fly the scheduled departure burn(s) first, then coast to the arrival
        // with the capture decision GATED. Titan's 3e9 m capture floor makes the ship "in range" the
        // whole cruise, so consulting AutopilotDecision now would Approach at ~7 km/s rel and restart
        // the velocity-reset bleed straight through the cheap arc. We stay muzzled until the ship is
        // honestly near the target — within max(60 s, 1% TOF) of arrival, OR inside the HONEST
        // Hill-scaled capture range (CaptureRangeHillRadii·hill, WITHOUT the floor) — then fall into
        // the terminal-capture loop unchanged.
        if (schedule is { } sch)
        {
            IReadOnlyList<TransferPlanner.BurnStep> burns = sch.Burns;
            double departTime = burns.Count > 0 ? burns[0].SimTime : ship.SimTime;
            foreach (TransferPlanner.BurnStep burn in burns)
            {
                // Coast exactly to the burn epoch in ≤ CoastStepSeconds strides (RunAdaptive lands
                // exactly on the requested end time, the same way it lands on ManeuverPlan nodes), so
                // the impulse is applied from the true drifted state — never from a warp-jumped one.
                while (ship.SimTime < burn.SimTime && iterations++ < maxIterations)
                {
                    double dt = Math.Min(CoastStepSeconds, burn.SimTime - ship.SimTime);
                    ship = simulator.RunAdaptive(ship, dt);
                    Record(ship);
                }

                // Price the impulse at the ship's speed JUST BEFORE the burn, the same OrbitRule.PulsesFor
                // kernel the live loop spends with, then budget-check exactly like the Approach case.
                int burnCost = OrbitRule.PulsesFor(burn.DeltaV.Length, ship.Velocity.Length);
                pulses += burnCost;
                approachBurns++;
                if (pulses > budgetPulses)
                {
                    return new RehearsalResult(
                        false, pulses, approachBurns, ship.SimTime - startTime, true, false, path);
                }

                ship = ship with { Velocity = ship.Velocity + burn.DeltaV };
                Record(ship);
            }

            // Coast to the arrival gate, decisions muzzled. The whole Enceladus→Titan cruise sits
            // inside Titan's 3e9 m capture floor, so this transfer arc is stepped fine throughout: the
            // raw Lambert-to-centre arc is a near-collision course whose terminal capture is sensitive
            // to integration error, and a coarse cruise stride visibly changes the captured cost. To
            // keep the #148 intended-path polyline spanning the WHOLE arc without exhausting the 4096
            // sample cap on multi-day fine steps, path RECORDING is throttled to a coarse cadence while
            // the integration stays fine.
            double tof = sch.ArrivalTime - departTime;
            double gateTime = sch.ArrivalTime - Math.Max(60.0, 0.01 * tof);
            double honestRange = OrbitRule.CaptureRangeHillRadii * hill;
            double lastRecord = ship.SimTime;
            while (ship.SimTime < endTime && iterations++ < maxIterations)
            {
                Vector2d bodyPos = ephemeris.Position(body.Id, ship.SimTime);
                double distance = (ship.Position - bodyPos).Length;
                if (ship.SimTime >= gateTime || distance < honestRange)
                {
                    break; // gate open — hand off to the terminal-capture loop below.
                }

                ship = simulator.RunAdaptive(ship, FineStepSeconds);
                if (ship.SimTime - lastRecord >= CoastStepSeconds)
                {
                    Record(ship);
                    lastRecord = ship.SimTime;
                }
            }
            Record(ship); // always mark the hand-off point.
        }

        while (ship.SimTime < endTime && iterations++ < maxIterations)
        {
            Vector2d bodyPos = ephemeris.Position(body.Id, ship.SimTime);
            Vector2d bodyVel = BodyVelocity(ephemeris, body.Id, ship.SimTime);

            // #155 STATION arrival: a μ=0 station is "captured" the instant the ship is inside the dock
            // envelope (DockRule) — coasting alongside within range and matched — evaluated only once the
            // schedule's arrival gate has opened (the pre-loop above coasted us to it). The rendezvous
            // schedule's two burns are the whole cost; there is NO OrbitRule.Insert to price for a
            // mass-less body. If the gate opened but we're not yet in the envelope, we fall through to the
            // legacy decision loop: Approach still works on a μ=0 body (it matches velocity and closes the
            // short range), and its budget guard stays. Insert can never fire here (WindowOpen needs
            // distance < hill = 0), so the ship is never falsely "orbit-captured".
            if (isStation && DockRule.InEnvelope(ship, bodyPos, bodyVel, body.BodyRadius))
            {
                return new RehearsalResult(
                    true, pulses, approachBurns, ship.SimTime - startTime, false, false, path);
            }

            // A moon's parent planet is a solid body the approach chord must round; a planet's
            // parent is the sun, which is never routed around (matches CheckArmedInsertion 5026).
            OrbitRule.ApproachObstacle? obstacle = parent.ParentId is null
                ? null
                : new OrbitRule.ApproachObstacle(
                    ephemeris.Position(parent.Id, ship.SimTime), parent.BodyRadius * OrbitRule.ParentSafeBodyRadii);

            switch (OrbitRule.AutopilotDecision(ship, bodyPos, bodyVel, body, hill))
            {
                case OrbitRule.AutopilotAction.Approach:
                    int approachCost = OrbitRule.ApproachPulseCost(ship, bodyPos, bodyVel, body, obstacle, hill);
                    pulses += approachCost;
                    approachBurns++;
                    if (pulses > budgetPulses)
                    {
                        // The #146 bleed: it would burn past the tank before capturing. Bail cheaply.
                        return new RehearsalResult(
                            false, pulses, approachBurns, ship.SimTime - startTime, true, false, path);
                    }
                    ship = OrbitRule.Approach(ship, bodyPos, bodyVel, body, obstacle, hill);
                    Record(ship);
                    ship = simulator.RunAdaptive(ship, FineStepSeconds);
                    Record(ship);
                    break;

                case OrbitRule.AutopilotAction.Insert:
                    int insertCost = OrbitRule.PulseCost(ship, bodyPos, bodyVel, body);
                    pulses += insertCost;
                    ship = OrbitRule.Insert(ship, bodyPos, bodyVel, body);
                    Record(ship);
                    // The insertion is the arrival; it may spend into the reserve. Un-affordable
                    // outright (more than the whole budget-plus-reserve) is still a refusal.
                    return new RehearsalResult(
                        true, pulses, approachBurns, ship.SimTime - startTime, pulses > budgetPulses, false, path);

                default: // None — coast. Step coarse when far (no burn possible), fine when near.
                    double distance = (ship.Position - bodyPos).Length;
                    double dt = distance > captureRange * CoastRangeFactor ? CoastStepSeconds : FineStepSeconds;
                    ship = simulator.RunAdaptive(ship, dt);
                    Record(ship);
                    break;
            }
        }

        // Ran out of horizon or iterations without capturing — cannot be promised.
        return new RehearsalResult(false, pulses, approachBurns, ship.SimTime - startTime, false, true, path);
    }

    /// <summary>
    /// The plan pass the collision alarm should judge for an ARMED plan (#219) — the plan's ACHIEVED
    /// outcome, not its powered approach. A DELIVERABLE rehearsal proves the ship captures into a safe
    /// park; the coarse-stepped terminal coast records a target-body graze (even a sub-surface one)
    /// right before <see cref="OrbitRule.Insert"/> circularizes it back above the surface, but that
    /// graze is the insert working, NOT news — feeding it to <see cref="CollisionAlertRule"/> raw made
    /// ROCKS AHEAD cry on a perfectly valid armed approach. So for the target we judge the ACHIEVED
    /// PARK (the final, post-insert sample) instead of the transient approach minimum, while still
    /// reporting any OTHER body the arc threads and a plan whose achieved park is itself subsurface —
    /// a genuinely bad plan shouts LOUDER, not softer. A non-deliverable rehearsal (never armed, but
    /// honest for callers/tests) is judged over its whole path. Null when there is no path to judge.
    /// </summary>
    public static ClosestApproach.Pass? PlanCollisionPass(
        RehearsalResult result, ICelestialEphemeris ephemeris, string targetBodyId)
    {
        IReadOnlyList<TrajectorySample> path = result.Path;
        if (path.Count < 2)
        {
            return null;
        }

        TrajectorySample park = path[^1]; // the rehearsal returns right after the insert: the park.
        ClosestApproach.Pass? best = null;
        foreach (ClosestApproach.Pass pass in ClosestApproach.Passes(path, ephemeris))
        {
            ClosestApproach.Pass judged = pass;
            if (result.Deliverable && pass.BodyId == targetBodyId)
            {
                // Trust the deliverable insertion: measure the target from the ACHIEVED park, not the
                // approach graze it resolves. The park sits above the surface, so a valid approach is
                // silent; a park that is itself subsurface still surfaces as an Impact here.
                double d = (park.Position - ephemeris.Position(targetBodyId, park.SimTime)).Length;
                judged = pass with { Distance = d, SimTime = park.SimTime, ShipPosition = park.Position };
            }

            if (best is null || judged.Severity < best.Value.Severity)
            {
                best = judged;
            }
        }

        return best;
    }

    private static CelestialBody? FindBody(ICelestialEphemeris ephemeris, string id)
    {
        foreach (CelestialBody b in ephemeris.Bodies)
        {
            if (b.Id == id)
            {
                return b;
            }
        }
        return null;
    }

    private static Vector2d BodyVelocity(ICelestialEphemeris ephemeris, string id, double simTime) =>
        (ephemeris.Position(id, simTime + 1.0) - ephemeris.Position(id, simTime - 1.0)) / 2.0;
}
