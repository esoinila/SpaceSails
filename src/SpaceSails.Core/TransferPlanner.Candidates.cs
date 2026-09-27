namespace SpaceSails.Core;

/// <summary>
/// #251 · THE CANDIDATES AND THE GRID — the plan candidate, the porkchop cell and its evaluation, the
/// phasing candidate, and the small helpers <c>Solve</c> leans on.
///
/// <para>Split out of <c>TransferPlanner.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field. <c>Solve</c> itself is one 253-line method and stays
/// whole in the opening file.</para>
/// </summary>
public static partial class TransferPlanner
{
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
