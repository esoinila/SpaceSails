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
    public static RehearsalResult Rehearse(
        ShipState ship,
        ICelestialEphemeris ephemeris,
        Simulator simulator,
        string targetBodyId,
        int budgetPulses,
        bool capturePath = false,
        double maxHorizonSeconds = DefaultMaxHorizonSeconds,
        int maxIterations = DefaultMaxIterations)
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
            // Nothing orbit-able (the sun, a station): not a journey the autopilot flies.
            return new RehearsalResult(false, 0, 0, 0, false, false, path);
        }

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

        while (ship.SimTime < endTime && iterations++ < maxIterations)
        {
            Vector2d bodyPos = ephemeris.Position(body.Id, ship.SimTime);
            Vector2d bodyVel = BodyVelocity(ephemeris, body.Id, ship.SimTime);

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
