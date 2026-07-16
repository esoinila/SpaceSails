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
        string Summary);

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

        if (best is not { } coarse)
        {
            return Failed(
                "no feasible transfer window in the coast — every arc threads the planet, arrives too fast to capture, " +
                "or has no single-rev solution; widen the wait or ease the search");
        }

        // 6. One 5×5 half-cell refine around the winner (½-cell spacing, so ±one coarse cell each way
        //    at double the resolution). Off-grid departure states come from the same incremental cache.
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

        // Better never nulls a non-null first argument, so best stays set through the refine.
        Cell winner = best!.Value;
        if (winner.Cost > request.MaxDeltaV)
        {
            return Failed(
                $"cheapest transfer to {targetBody.Name} costs {winner.Cost / 1000:F1} km/s — over the " +
                $"{request.MaxDeltaV / 1000:F1} km/s ceiling; raise MaxDeltaV or find a nearer moon");
        }

        // 7. Emit the single departure burn and the honest bill. Δv is priced in pulses with the SAME
        //    OrbitRule.PulsesFor the live approach/insertion burns spend — departure at the ship's
        //    world speed at burn, arrival at the target's world speed at arrival — so the quote and
        //    the eventual spend come from one source.
        int pulses = OrbitRule.PulsesFor(winner.DepartDeltaV, winner.ShipWorldSpeed)
                     + OrbitRule.PulsesFor(winner.ArriveDeltaV, winner.TargetWorldSpeed);

        string summary =
            $"transfer to {targetBody.Name}: burn {winner.DepartDeltaV / 1000:F2} km/s in {(winner.DepartTime - t0) / 3600:F1} h, " +
            $"arrive in {winner.Tof / 86400:F1} d at {winner.ArriveDeltaV / 1000:F2} km/s rel (est. {pulses} pulses)";

        return new Result(
            Ok: true,
            Failure: null,
            DepartTime: winner.DepartTime,
            TimeOfFlightSeconds: winner.Tof,
            Burns: [new BurnStep(winner.DepartTime, winner.DeltaVWorld)],
            ArrivalRelativeSpeed: winner.ArrivalRelativeSpeed,
            PlannedDeltaVTotal: winner.Cost,
            EstimatedPulses: pulses,
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
        Burns: [], ArrivalRelativeSpeed: 0, PlannedDeltaVTotal: 0, EstimatedPulses: 0, Summary: reason);
}
