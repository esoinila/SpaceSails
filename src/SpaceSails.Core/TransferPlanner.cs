namespace SpaceSails.Core;

/// <summary>
/// #146 · The moon run. The in-well transfer planner: given a ship free-flying inside a planet's
/// Hill sphere, find the CHEAP departure burn that rides the well onto a Lambert arc to one of the
/// planet's moons — instead of the status-quo <see cref="OrbitRule.Approach"/> loop, which re-SETS
/// the whole velocity vector every time the planet's pull drives the relative speed back over the
/// cap and so throws the well's work away and re-buys it (the owner's Wednesday Titan approach:
/// ~33 km/s, 167 pulses). Priced honestly in the parent's frame a single Hohmann-scale hop is
/// ~6 km/s — a ~5× saving before any refinement.
///
/// The house pattern, same split as the labs (14 proposes, 15/17/19 dispose):
/// <list type="bullet">
/// <item><see cref="TransferMath.Lambert"/> is the ENGINE — it solves from the ship's actual
/// coasted state at any departure time to the moon's actual rail position at arrival, elliptical
/// rails and all, no apsis assumption;</item>
/// <item><see cref="TransferMath.Hohmann"/> is the TEACHER — it supplies the time-of-flight scale
/// and the scan seed only;</item>
/// <item>the real N-body <see cref="Simulator"/> disposes — this planner emits ONE departure burn
/// and hands the terminal capture to the existing <see cref="OrbitRule"/> machinery once the arc
/// falls inside the moon's <see cref="OrbitRule.CaptureRange"/>. A mid-course of &lt; 200 m/s closes
/// the two-body↔N-body gap (lab 17 measured the pocket clean), which is why the arrival leg is a
/// quote, not a scheduled burn.</item>
/// </list>
///
/// The refusal rules are <see cref="SlingPlanner"/>'s rules, kept here on purpose: a plan that
/// would thread the planet, arrive uncapturably fast, or beat the Δv ceiling is refused with a
/// verbatim, specific reason — the planner never dresses a best-effort miss as a solution.
///
/// <para>Deterministic by construction (the whole point — client WASM and server must agree):
/// fixed grid sizes, fixed refine, no clock, no randomness. Every candidate departure state is the
/// real adaptive coast of the ship, cached incrementally grid-to-grid so the scan is WASM-cheap.</para>
/// </summary>
public static class TransferPlanner
{
    /// <summary>Departure-time cells across the wait window — the porkchop's rows. 24 samples a
    /// full ≈36 h synodic window at ≈1.5 h resolution before the ½-cell refine doubles it.</summary>
    private const int DepartureCells = 24;

    /// <summary>Time-of-flight cells per departure — the porkchop's columns, swept over
    /// [<see cref="TofLowFraction"/>, <see cref="TofHighFraction"/>] × the Hohmann time of flight so
    /// the scan straddles the closed-form seed both faster (steeper, pricier) and slower.</summary>
    private const int TofCells = 12;

    private const double TofLowFraction = 0.4;
    private const double TofHighFraction = 1.6;

    /// <summary>Coarse cap on the adaptive coast step (s). The dynamical-time stepper auto-refines
    /// through a close pass regardless; this only stops the deep-in-the-well cruise from taking a
    /// wastefully long stride. Sized well under a moon's local orbital time so the coasted grid
    /// states stay honest.</summary>
    private const double CoarseStep = 900.0;

    /// <summary>Rendezvous mode (#155) triggers when |r_ship − r_target| ≤ this fraction of the
    /// target's parent-relative radius — the near-co-orbital band where Lambert's single-rev solver
    /// is structurally blind (a phasing loop returns to its own start = the 2π singularity).</summary>
    private const double CoOrbitalRadiusFraction = 0.075;

    /// <summary>Phasing candidates are priced for k = 1..this many catch-up laps × both families
    /// (dip inward / swell outward). More laps = cheaper burns, longer wait — the trade table.</summary>
    private const int PhasingMaxRevolutions = 6;

    /// <summary>Fixed prep offset (s) before the first phasing burn — the deterministic departure
    /// epoch at which the phase gap, ship radius, and rail velocities are all read.</summary>
    private const double PhasingPrepOffsetSeconds = 600.0;

    /// <summary>A phasing ellipse's apoapsis must clear no more than this fraction of the parent's
    /// own Hill sphere (about the sun) — never swell into the tide-stripped outer Hill.</summary>
    private const double PhasingHillApoapsisFraction = 0.9;

    /// <summary>
    /// A transfer request. The caller passes the ship's state at solve time (the arm click or the
    /// lab's departure instant) — its <see cref="ShipState.SimTime"/> must be consistent with the
    /// ephemeris epoch. <paramref name="MaxWaitSeconds"/> is how long the ship may coast waiting for
    /// a cheap window; pass ≤ 0 to accept the planner's default of 1.25 × the ship↔target synodic
    /// period (just over one full window). <paramref name="MaxDeltaV"/> is the sanity ceiling — the
    /// planner refuses anything worse.
    /// </summary>
    public readonly record struct Request(
        ShipState Ship,
        string ParentBodyId,
        string TargetBodyId,
        double MaxWaitSeconds,
        double MaxDeltaV = 25_000);

    /// <summary>A scheduled impulse: add <see cref="DeltaV"/> to the ship's world velocity when the
    /// sim clock reaches <see cref="SimTime"/>. A parent-frame Δv is a world-frame Δv (frame offsets
    /// cancel in deltas), so this is applied directly to the live/rehearsed ship.</summary>
    public readonly record struct BurnStep(double SimTime, Vector2d DeltaV);

    /// <summary>
    /// The solved (or refused) transfer. On success <see cref="Burns"/> carries the single departure
    /// impulse; the arrival leg is the existing capture machinery's job once the arc is inside the
    /// moon's <see cref="OrbitRule.CaptureRange"/>, so <see cref="PlannedDeltaVTotal"/> quotes
    /// departure + arrival-matching as the honest bill. On failure <see cref="Ok"/> is false and
    /// <see cref="Failure"/> says why — shown verbatim, <see cref="SlingPlanner"/> style.
    /// </summary>
    public readonly record struct Result(
        bool Ok,
        string? Failure,
        double DepartTime,
        double TimeOfFlightSeconds,
        IReadOnlyList<BurnStep> Burns,
        double ArrivalRelativeSpeed,
        double PlannedDeltaVTotal,
        int EstimatedPulses,
        string Summary,
        IReadOnlyList<Alternative> Alternatives)
    {
        /// <summary>The lean hand-off the rehearsal and the live tick loop actually fly: just the
        /// burns and when the arc reaches the moon (<see cref="DepartTime"/> + time of flight). The
        /// arrival time is the gate that keeps <see cref="OrbitRule.AutopilotDecision"/> muzzled until
        /// the ship is honestly near the target — see <see cref="AutopilotRehearsal.Rehearse"/>.</summary>
        public Schedule ToSchedule() => new(Burns, DepartTime + TimeOfFlightSeconds);
    }

    /// <summary>One row of the cheaper-vs-sooner trade table (#155): a candidate plan the winner was
    /// chosen from, priced for the UI's tactical choice ("comes in handy when there is heat on us").
    /// The winner is always row 0; the rest are ordered by arrival time. <see cref="Label"/> names the
    /// candidate ("direct hop" for the porkchop hop, "phasing k=N (dip|swell)" for a co-orbital bus),
    /// <see cref="WaitSeconds"/> is how long the ship coasts before it is at the target, and
    /// <see cref="ArrivalTime"/> is the sim clock at rendezvous. Never null on a Result — an empty
    /// list on a refusal, a single winner row when only the porkchop found a plan.</summary>
    public readonly record struct Alternative(
        string Label,
        double DeltaVTotal,
        int EstimatedPulses,
        double WaitSeconds,
        double ArrivalTime);

    /// <summary>The executable core of a solved transfer: the departure burn(s) and the sim time the
    /// arc arrives at the target moon. Consumed by <see cref="AutopilotRehearsal.Rehearse"/> and the
    /// live armed-insertion loop — both execute due burns exactly at their epochs and gate the capture
    /// decision on <see cref="ArrivalTime"/> so the giant's pull can't restart the #146 velocity-reset
    /// bleed while the cheap arc is still in flight.</summary>
    public readonly record struct Schedule(IReadOnlyList<BurnStep> Burns, double ArrivalTime);

    /// <summary>
    /// Solve for the departure burn that rides <paramref name="request"/>'s parent well onto a
    /// Lambert arc to its target moon. A 24×12 porkchop over departure time × time of flight, scored
    /// entirely in the parent's frame (departure Δv + arrival matching Δv), with a single 5×5
    /// half-cell refine around the winner. Cells that thread the planet, arrive over
    /// <see cref="OrbitRule.MaxRelativeSpeed"/>, or have no honest single-rev Lambert solution are
    /// skipped. Deterministic: the result is a pure function of the inputs.
    /// </summary>
    public static Result Solve(Simulator simulator, ICelestialEphemeris ephemeris, Request request)
    {
        // 1. Resolve the bodies and rule out the cases this planner does not solve.
        CelestialBody? parentBody = Find(ephemeris, request.ParentBodyId);
        CelestialBody? targetBody = Find(ephemeris, request.TargetBodyId);
        if (parentBody is null)
        {
            return Failed($"no body named '{request.ParentBodyId}' to ride");
        }

        if (targetBody is null)
        {
            return Failed($"no body named '{request.TargetBodyId}' to reach");
        }

        if (targetBody.ParentId != parentBody.Id)
        {
            return Failed($"{targetBody.Name} does not orbit {parentBody.Name} — this planner rides one well to its own moons");
        }

        double parentMu = parentBody.Mu;
        ShipState ship = request.Ship;
        double t0 = ship.SimTime;
        Vector2d parentPos0 = ephemeris.Position(parentBody.Id, t0);

        // The planner solves the free-flight-in-the-well case only. Departing while parked inside
        // ANOTHER moon's Hill sphere (its gravity would dominate the first hours of the coast) is a
        // documented follow-up — refuse it loudly rather than quote a plan the coast won't honour.
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Id == targetBody.Id || body.ParentId != parentBody.Id || body.Kind != BodyKind.Moon)
            {
                continue;
            }

            double hill = OrbitRule.HillRadius(body, parentMu);
            double distance = (ship.Position - ephemeris.Position(body.Id, t0)).Length;
            if (distance < hill)
            {
                return Failed($"inside {body.Name}'s Hill sphere — leave the moon before planning a transfer (parked-at-moon departure is a follow-up)");
            }
        }

        // 2. Size the wait window. Default to 1.25 × the synodic period between the ship's own orbit
        //    about the parent (from its current radius) and the target's — just over one full window,
        //    so a cheap phase is guaranteed to appear somewhere on the grid.
        double shipRadius0 = (ship.Position - parentPos0).Length;
        if (!(shipRadius0 > 0) || !(parentMu > 0))
        {
            return Failed($"the ship is not in a usable orbit about {parentBody.Name}");
        }

        double shipPeriod = OrbitRule.LocalOrbitPeriod(shipRadius0, parentMu);
        double maxWait = request.MaxWaitSeconds;
        if (!(maxWait > 0))
        {
            double synodic = targetBody.OrbitPeriod > 0
                ? TransferMath.SynodicPeriod(shipPeriod, targetBody.OrbitPeriod)
                : double.PositiveInfinity;
            maxWait = double.IsFinite(synodic) ? 1.25 * synodic : shipPeriod;
        }

        // 2b. Rendezvous mode (#155): when the ship shares the target's lane, Lambert's single-rev
        //     solver is structurally blind — so ALSO price the closed-form phasing maneuver below and
        //     merge. Detect co-orbital by parent-relative radii at solve time. The 1.25×synodic wait
        //     default explodes toward infinity for equal-period lanes (exactly this case), which would
        //     make the porkchop coast an absurd, WASM-hostile span — so bound the co-orbital porkchop
        //     to a single ship lap; the phasing candidates own the long windows (all k ≤ 6). The
        //     non-co-orbital sizing is untouched, so the moon-run porkchop stays byte-identical.
        Vector2d targetPos0 = ephemeris.Position(targetBody.Id, t0);
        double targetRadius0 = (targetPos0 - parentPos0).Length;
        bool coOrbital = targetRadius0 > 0
            && Math.Abs(shipRadius0 - targetRadius0) <= CoOrbitalRadiusFraction * targetRadius0;
        if (coOrbital && !(request.MaxWaitSeconds > 0))
        {
            maxWait = shipPeriod;
        }

        // 3. Build the departure grid: 24 real coasted ship states across the window, advanced
        //    incrementally grid-to-grid (deterministic and cheap — each hop is one short adaptive
        //    coast). TrajectorySample carries no velocity, so the full state cannot be read off a
        //    single ProjectAdaptive; the incremental coast IS the honest, N-body departure state.
        double dtGrid = maxWait / (DepartureCells - 1);
        var depTimes = new double[DepartureCells];
        var depStates = new ShipState[DepartureCells];
        depStates[0] = ship;
        depTimes[0] = t0;
        for (int i = 1; i < DepartureCells; i++)
        {
            depStates[i] = simulator.RunAdaptive(depStates[i - 1], dtGrid, maxTimeStep: CoarseStep);
            depTimes[i] = depStates[i].SimTime;
        }

        // 4. The Hohmann time of flight is the scan's scale (teacher only — the engine is Lambert).
        double tofScale = TransferMath.Hohmann(shipRadius0, targetBody.OrbitRadius, parentMu).TransferSeconds;
        double tofStep = tofScale * (TofHighFraction - TofLowFraction) / (TofCells - 1);
        double parentSafe = OrbitRule.ParentSafeBodyRadii * parentBody.BodyRadius;

        // 5. Porkchop scan over the grid.
        Cell? best = null;
        for (int di = 0; di < DepartureCells; di++)
        {
            for (int ti = 0; ti < TofCells; ti++)
            {
                double tof = tofScale * (TofLowFraction + (TofHighFraction - TofLowFraction) * ti / (TofCells - 1));
                best = Better(best, EvaluateCell(ephemeris, parentBody, targetBody, parentMu, parentSafe, depStates[di], depTimes[di], tof));
            }
        }

        // 6. One 5×5 half-cell refine around the porkchop winner (½-cell spacing, so ±one coarse cell
        //    each way at double the resolution). A co-orbital lane usually leaves this null — Lambert
        //    has no honest single-rev arc across a ~0 gap — and the phasing candidates carry the plan.
        Cell? porkchopWinner = null;
        if (best is { } coarse)
        {
            double halfDep = dtGrid / 2;
            double halfTof = tofStep / 2;
            for (int i = -2; i <= 2; i++)
            {
                double tDep = coarse.DepartTime + i * halfDep;
                if (tDep < t0 || tDep > t0 + maxWait)
                {
                    continue;
                }

                ShipState st = StateAt(simulator, ship, depStates, t0, dtGrid, tDep);
                for (int j = -2; j <= 2; j++)
                {
                    double tof = coarse.Tof + j * halfTof;
                    if (!(tof > 0))
                    {
                        continue;
                    }

                    best = Better(best, EvaluateCell(ephemeris, parentBody, targetBody, parentMu, parentSafe, st, tDep, tof));
                }
            }

            porkchopWinner = best!.Value; // Better never nulls a non-null first argument.
        }

        // 7. Rendezvous mode. When co-orbital, price the closed-form phasing maneuver (Curtis 6.5) as
        //    a pool of k×family candidates and merge with the porkchop hop. Everything the schedule
        //    needs is read at a fixed prep offset so the answer is a pure function of the inputs.
        var candidates = new List<PlanCandidate>();
        if (porkchopWinner is { } pw)
        {
            candidates.Add(FromPorkchop(pw, t0, targetBody));
        }

        if (coOrbital)
        {
            double tDep = t0 + PhasingPrepOffsetSeconds;
            ShipState shipAtDep = simulator.RunAdaptive(ship, PhasingPrepOffsetSeconds, maxTimeStep: CoarseStep);
            Vector2d parentPosDep = ephemeris.Position(parentBody.Id, tDep);
            Vector2d parentVelDep = TransferMath.BodyVelocity(ephemeris, parentBody.Id, tDep);
            Vector2d shipRelPos = shipAtDep.Position - parentPosDep;
            Vector2d shipRelVel = shipAtDep.Velocity - parentVelDep;
            Vector2d targetRelPos = ephemeris.Position(targetBody.Id, tDep) - parentPosDep;
            double rDep = shipRelPos.Length;

            // The rails run counter-clockwise; a retrograde ship cannot ride a phasing bus. Refuse
            // loudly rather than quote a plan the geometry forbids.
            double angularMomentum = shipRelPos.X * shipRelVel.Y - shipRelPos.Y * shipRelVel.X;
            if (angularMomentum < 0)
            {
                return Failed(
                    $"the ship circles {parentBody.Name} retrograde (clockwise) — the rendezvous rails run " +
                    "counter-clockwise; match the lane's direction before arming a co-orbital rendezvous");
            }

            if (rDep > 0)
            {
                double gap = TransferMath.PhaseGap(shipRelPos, targetRelPos);
                Vector2d progradeUnit = new Vector2d(-shipRelPos.Y, shipRelPos.X) / rDep;
                double parentHillCeiling = ParentHillCeiling(ephemeris, parentBody);

                for (int k = 1; k <= PhasingMaxRevolutions; k++)
                {
                    for (int family = 0; family < 2; family++)
                    {
                        if (BuildPhasingCandidate(
                                ephemeris, parentBody, targetBody, parentMu, parentSafe, parentHillCeiling,
                                rDep, gap, progradeUnit, shipRelVel, shipAtDep.Velocity.Length,
                                tDep, k, dipInside: family == 0, request.MaxWaitSeconds, request.MaxDeltaV) is { } row)
                        {
                            candidates.Add(row);
                        }
                    }
                }
            }
        }

        // 8. Merge and pick the winner — the cheapest feasible plan across both pools.
        if (candidates.Count == 0)
        {
            return Failed(
                "no feasible transfer window in the coast — every arc threads the planet, arrives too fast to capture, " +
                "or has no single-rev solution; widen the wait or ease the search");
        }

        var affordable = candidates.Where(c => c.TotalDeltaV <= request.MaxDeltaV).ToList();
        if (affordable.Count == 0)
        {
            double cheapest = candidates.Min(c => c.TotalDeltaV);
            return Failed(
                $"cheapest transfer to {targetBody.Name} costs {cheapest / 1000:F1} km/s — over the " +
                $"{request.MaxDeltaV / 1000:F1} km/s ceiling; raise MaxDeltaV or find a nearer moon");
        }

        int winnerIdx = 0;
        for (int i = 1; i < affordable.Count; i++)
        {
            if (affordable[i].TotalDeltaV < affordable[winnerIdx].TotalDeltaV)
            {
                winnerIdx = i;
            }
        }

        PlanCandidate winner = affordable[winnerIdx];

        // 9. The trade table (#155): winner first, then every other affordable candidate by arrival
        //    time (deterministic tie-breaks). The direct hop is included whenever the porkchop found
        //    one — the UI's sooner-vs-cheaper choice reads exactly this list.
        var alternatives = new List<Alternative>(affordable.Count) { ToAlternative(winner) };
        alternatives.AddRange(affordable
            .Where((_, i) => i != winnerIdx)
            .OrderBy(c => c.ArrivalTime)
            .ThenBy(c => c.TotalDeltaV)
            .ThenBy(c => c.Label, StringComparer.Ordinal)
            .Select(ToAlternative));

        return new Result(
            Ok: true,
            Failure: null,
            DepartTime: winner.DepartTime,
            TimeOfFlightSeconds: winner.TimeOfFlight,
            Burns: winner.Burns,
            ArrivalRelativeSpeed: winner.ArrivalRelativeSpeed,
            PlannedDeltaVTotal: winner.TotalDeltaV,
            EstimatedPulses: winner.Pulses,
            Summary: winner.Summary,
            Alternatives: alternatives);
    }

    /// <summary>A unified candidate — either the porkchop hop or one phasing bus — carrying everything
    /// needed to emit the winning <see cref="Result"/> and its <see cref="Alternative"/> row.</summary>
    private readonly record struct PlanCandidate(
        string Label,
        IReadOnlyList<BurnStep> Burns,
        double DepartTime,
        double TimeOfFlight,
        double ArrivalRelativeSpeed,
        double TotalDeltaV,
        int Pulses,
        double WaitSeconds,
        double ArrivalTime,
        string Summary);

    private static Alternative ToAlternative(PlanCandidate c) =>
        new(c.Label, c.TotalDeltaV, c.Pulses, c.WaitSeconds, c.ArrivalTime);

    /// <summary>The porkchop winner as a candidate — its pulse pricing and summary are byte-for-byte
    /// the pre-#155 emit, so a non-co-orbital solve is unchanged but for gaining a one-row table.</summary>
    private static PlanCandidate FromPorkchop(Cell w, double t0, CelestialBody target)
    {
        int pulses = OrbitRule.PulsesFor(w.DepartDeltaV, w.ShipWorldSpeed)
                     + OrbitRule.PulsesFor(w.ArriveDeltaV, w.TargetWorldSpeed);
        string summary =
            $"transfer to {target.Name}: burn {w.DepartDeltaV / 1000:F2} km/s in {(w.DepartTime - t0) / 3600:F1} h, " +
            $"arrive in {w.Tof / 86400:F1} d at {w.ArriveDeltaV / 1000:F2} km/s rel (est. {pulses} pulses)";
        return new PlanCandidate(
            Label: "direct hop",
            Burns: [new BurnStep(w.DepartTime, w.DeltaVWorld)],
            DepartTime: w.DepartTime,
            TimeOfFlight: w.Tof,
            ArrivalRelativeSpeed: w.ArrivalRelativeSpeed,
            TotalDeltaV: w.Cost,
            Pulses: pulses,
            WaitSeconds: w.DepartTime - t0,
            ArrivalTime: w.DepartTime + w.Tof,
            Summary: summary);
    }

    /// <summary>0.9 × the parent's own Hill sphere about ITS parent (the sun, for a planet) — the
    /// apoapsis ceiling a phasing ellipse must clear. Inert (+∞) when the parent is the root or its
    /// grandparent is mass-less: there is no outer tide to respect.</summary>
    private static double ParentHillCeiling(ICelestialEphemeris ephemeris, CelestialBody parent)
    {
        if (parent.ParentId is null)
        {
            return double.PositiveInfinity;
        }

        CelestialBody? grand = Find(ephemeris, parent.ParentId);
        return grand is { Mu: > 0 }
            ? PhasingHillApoapsisFraction * OrbitRule.HillRadius(parent, grand.Mu)
            : double.PositiveInfinity;
    }

    /// <summary>
    /// Price one phasing bus: coast <paramref name="revolutions"/> laps on the ellipse
    /// <see cref="TransferMath.PhasingOrbit"/> hands back (dip inward to chase a leader, or swell
    /// outward to be lapped), then re-match. Two burns, both known at solve time: burn 1 leaves the
    /// circular lane onto the phasing ellipse (and trims any small radial drift — priced as a vector
    /// difference); apsis-to-apsis integer revs return the ship to the burn point with the same
    /// velocity vector, so burn 2 (re-matching the target that has now arrived there) is known too.
    /// Null when the geometry/lap count is impossible, the ellipse threads the planet or swells into
    /// the tide-stripped outer Hill, the wait exceeds the caller's cap, or the bill beats the ceiling.
    /// </summary>
    private static PlanCandidate? BuildPhasingCandidate(
        ICelestialEphemeris ephemeris, CelestialBody parent, CelestialBody target,
        double parentMu, double parentSafe, double parentHillCeiling,
        double rDep, double gap, Vector2d progradeUnit, Vector2d shipRelVel, double shipWorldSpeed,
        double tDep, int revolutions, bool dipInside, double maxWaitSeconds, double maxDeltaV)
    {
        if (TransferMath.PhasingOrbit(rDep, gap, parentMu, revolutions, dipInside) is not { } plan)
        {
            return null;
        }

        if (plan.Periapsis <= parentSafe || plan.Apoapsis >= parentHillCeiling)
        {
            return null;
        }

        double waitSeconds = plan.WaitSeconds;
        if (maxWaitSeconds > 0 && waitSeconds > maxWaitSeconds)
        {
            return null;
        }

        double tRdv = tDep + waitSeconds;

        // The phasing velocity at the burn apsis: prograde (radial unit rotated +90°, the CCW rails'
        // direction), magnitude from vis-viva at the burn radius. The same vector re-appears at the
        // rendezvous apsis a whole number of laps later.
        double phasingSpeed = Math.Sqrt(parentMu * (2 / rDep - 1 / plan.SemiMajorAxis));
        Vector2d vPhasing = progradeUnit * phasingSpeed;

        Vector2d dv1 = vPhasing - shipRelVel;
        Vector2d targetRelVelRdv = TransferMath.BodyVelocity(ephemeris, target.Id, tRdv)
                                   - TransferMath.BodyVelocity(ephemeris, parent.Id, tRdv);
        Vector2d dv2 = targetRelVelRdv - vPhasing;

        double dv1mag = dv1.Length;
        double dv2mag = dv2.Length;
        double total = dv1mag + dv2mag;
        if (total > maxDeltaV)
        {
            return null;
        }

        double targetWorldSpeed = TransferMath.BodyVelocity(ephemeris, target.Id, tRdv).Length;
        int pulses = OrbitRule.PulsesFor(dv1mag, shipWorldSpeed)
                     + OrbitRule.PulsesFor(dv2mag, targetWorldSpeed);

        string family = dipInside ? "dip" : "swell";
        string summary =
            $"rendezvous {target.Name}: {revolutions} lap {family} phasing, {total:F0} m/s over " +
            $"{waitSeconds / 86400:F1} d, close within {dv2mag:F0} m/s rel (est. {pulses} p)";

        return new PlanCandidate(
            Label: $"phasing k={revolutions} ({family})",
            Burns: [new BurnStep(tDep, dv1), new BurnStep(tRdv, dv2)],
            DepartTime: tDep,
            TimeOfFlight: waitSeconds,
            ArrivalRelativeSpeed: dv2mag,
            TotalDeltaV: total,
            Pulses: pulses,
            WaitSeconds: waitSeconds,
            ArrivalTime: tRdv,
            Summary: summary);
    }

    /// <summary>One scored porkchop cell: the parent-frame costs, the world-frame burn to apply, the
    /// times, and the world speeds the pulse pricing reads.</summary>
    private readonly record struct Cell(
        double Cost,
        double DepartDeltaV,
        double ArriveDeltaV,
        double Tof,
        double DepartTime,
        double ArrivalRelativeSpeed,
        Vector2d DeltaVWorld,
        double ShipWorldSpeed,
        double TargetWorldSpeed);

    private static Cell? Better(Cell? a, Cell? b) =>
        b is { } bb && (a is not { } aa || bb.Cost < aa.Cost) ? b : a;

    /// <summary>
    /// Score one departure-time × time-of-flight cell. Lambert-solves from the ship's parent-relative
    /// departure position to the moon's parent-relative arrival position, both in the parent's frame,
    /// then costs the burn to match the departure velocity plus the burn to match the moon at arrival.
    /// Returns null (SKIP — never a guess) for a cell with no honest single-rev Lambert arc, one that
    /// arrives too fast to capture, or one whose transfer ellipse dips inside the planet's safe radius.
    /// </summary>
    private static Cell? EvaluateCell(
        ICelestialEphemeris ephemeris, CelestialBody parent, CelestialBody target,
        double parentMu, double parentSafe, ShipState ship, double tDepart, double tof)
    {
        if (!(tof > 0))
        {
            return null;
        }

        double tArrive = tDepart + tof;
        Vector2d parentPosDepart = ephemeris.Position(parent.Id, tDepart);
        Vector2d parentVelDepart = TransferMath.BodyVelocity(ephemeris, parent.Id, tDepart);
        Vector2d parentPosArrive = ephemeris.Position(parent.Id, tArrive);
        Vector2d parentVelArrive = TransferMath.BodyVelocity(ephemeris, parent.Id, tArrive);

        Vector2d r1 = ship.Position - parentPosDepart;
        Vector2d r2 = ephemeris.Position(target.Id, tArrive) - parentPosArrive;

        if (TransferMath.Lambert(r1, r2, tof, parentMu) is not { } lambert)
        {
            return null;
        }

        Vector2d shipRelVel = ship.Velocity - parentVelDepart;
        Vector2d targetRelVel = TransferMath.BodyVelocity(ephemeris, target.Id, tArrive) - parentVelArrive;

        double arrivalRelSpeed = (lambert.V2 - targetRelVel).Length;
        if (arrivalRelSpeed >= OrbitRule.MaxRelativeSpeed)
        {
            return null;
        }

        // Never thread the planet: the transfer ellipse's periapsis (from the two-body elements of
        // r1, V1 about the parent) must clear the safe radius. Robust for elliptic AND hyperbolic
        // arcs via rp = p / (1 + e).
        if (Periapsis(r1, lambert.V1, parentMu) < parentSafe)
        {
            return null;
        }

        double departDeltaV = (lambert.V1 - shipRelVel).Length;
        double arriveDeltaV = arrivalRelSpeed; // matching the moon IS closing the arrival relative speed
        Vector2d deltaVWorld = lambert.V1 - shipRelVel; // world-frame Δv equals the parent-frame Δv

        return new Cell(
            Cost: departDeltaV + arriveDeltaV,
            DepartDeltaV: departDeltaV,
            ArriveDeltaV: arriveDeltaV,
            Tof: tof,
            DepartTime: tDepart,
            ArrivalRelativeSpeed: arrivalRelSpeed,
            DeltaVWorld: deltaVWorld,
            ShipWorldSpeed: ship.Velocity.Length,
            TargetWorldSpeed: TransferMath.BodyVelocity(ephemeris, target.Id, tArrive).Length);
    }

    /// <summary>Two-body periapsis of the arc that has position <paramref name="r"/> and velocity
    /// <paramref name="v"/> about a body of parameter <paramref name="mu"/>: rp = p/(1+e) with
    /// p = h²/μ and e = √(1 + 2εh²/μ²). Valid for every conic (a hyperbolic transfer still has a
    /// real, positive periapsis), so a scan cell is never wrongly kept or dropped on the arc type.</summary>
    private static double Periapsis(Vector2d r, Vector2d v, double mu)
    {
        double radius = r.Length;
        double energy = v.LengthSquared / 2 - mu / radius;
        double h = r.X * v.Y - r.Y * v.X;
        double p = h * h / mu;
        double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (mu * mu)));
        return p / (1 + e);
    }

    /// <summary>The coasted ship state at an arbitrary time on the wait window, advanced from the
    /// nearest cached grid state at or before it (deterministic given the same cache — the refine
    /// needs states between the coarse grid points).</summary>
    private static ShipState StateAt(
        Simulator simulator, ShipState ship, ShipState[] grid, double t0, double dtGrid, double t)
    {
        if (t <= t0)
        {
            return ship;
        }

        int k = Math.Clamp((int)Math.Floor((t - t0) / dtGrid), 0, grid.Length - 1);
        ShipState baseState = grid[k];
        double duration = t - baseState.SimTime;
        return duration > 0 ? simulator.RunAdaptive(baseState, duration, maxTimeStep: CoarseStep) : baseState;
    }

    private static CelestialBody? Find(ICelestialEphemeris ephemeris, string id)
    {
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Id == id)
            {
                return body;
            }
        }

        return null;
    }

    private static Result Failed(string reason) => new(
        Ok: false, Failure: reason, DepartTime: 0, TimeOfFlightSeconds: 0,
        Burns: [], ArrivalRelativeSpeed: 0, PlannedDeltaVTotal: 0, EstimatedPulses: 0, Summary: reason,
        Alternatives: []);
}
