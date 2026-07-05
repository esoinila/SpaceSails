namespace SpaceSails.Core;

/// <summary>
/// M28 · Ordnance (Sunday Plan F3). A slug is the simplest thing the sim already knows how
/// to fly: a ballistic point mass (the mass-driver pod proved the type) with a despawn timer
/// — self-evaporating, so a miss can't litter the system forever. A missile is a slug with a
/// small correction budget and a pursuit rule. Hit resolution reuses PR-A's closed-form
/// segment minimum (<see cref="InterceptEstimate.SegmentMin"/>), so no integrator step can
/// tunnel through a target. Deterministic throughout.
/// </summary>
public enum OrdnanceKind
{
    Slug,
    Missile,
}

/// <summary>One round in flight. Immutable identity; the mutable state lives with the caller.</summary>
public sealed record OrdnanceRound(
    string Id,
    OrdnanceKind Kind,
    string? TargetId,
    double LaunchedAtSimTime,
    bool AcrossTheBow = false);

public static class OrdnanceRule
{
    /// <summary>A slug evaporates this long after launch — the self-cleaning sky.</summary>
    public const double SlugLifetimeSeconds = 6 * 3600;

    /// <summary>A missile carries station-keeping consumables a little longer.</summary>
    public const double MissileLifetimeSeconds = 12 * 3600;

    /// <summary>Passing this close counts as a hit through the sail.</summary>
    public const double HitRadiusMeters = 5e5;

    /// <summary>Missile correction thrust, m/s² — strong for its size, tiny total budget.</summary>
    public const double MissileAccel = 5.0;

    /// <summary>Total Δv a missile can spend on corrections, m/s.</summary>
    public const double MissileDeltaVBudget = 3000;

    public static double LifetimeSeconds(OrdnanceKind kind) =>
        kind == OrdnanceKind.Missile ? MissileLifetimeSeconds : SlugLifetimeSeconds;

    public static bool Expired(OrdnanceRound round, double simTime) =>
        simTime - round.LaunchedAtSimTime >= LifetimeSeconds(round.Kind);

    /// <summary>
    /// One guidance step: pure pursuit with a lead — steer the missile's velocity toward the
    /// point the target will occupy at the current closing time, spending at most
    /// <see cref="MissileAccel"/>·dt and never more than the remaining budget. Returns the
    /// corrected state and the Δv actually spent.
    /// </summary>
    public static (ShipState State, double SpentDeltaV) Guide(
        ShipState missile, ShipState target, double dt, double remainingBudget)
    {
        if (remainingBudget <= 0 || dt <= 0)
        {
            return (missile, 0);
        }

        Vector2d toTarget = target.Position - missile.Position;
        double distance = toTarget.Length;
        if (distance <= 0)
        {
            return (missile, 0);
        }

        // Lead: aim where the target will be after the straight-line closing time.
        Vector2d relVel = missile.Velocity - target.Velocity;
        double closing = relVel.Dot(toTarget) / distance;
        double leadTime = closing > 1 ? Math.Min(distance / closing, MissileLifetimeSeconds) : distance / 1000.0;
        Vector2d aimPoint = target.Position + target.Velocity * leadTime;

        Vector2d desiredDir = (aimPoint - missile.Position).Normalized();
        double closingSpeed = Math.Max(relVel.Length, 1);
        Vector2d desiredVelocity = target.Velocity + desiredDir * closingSpeed;

        Vector2d correction = desiredVelocity - missile.Velocity;
        double want = correction.Length;
        double allowed = Math.Min(MissileAccel * dt, remainingBudget);
        if (want <= 0)
        {
            return (missile, 0);
        }

        double spend = Math.Min(want, allowed);
        Vector2d applied = correction / want * spend;
        return (missile with { Velocity = missile.Velocity + applied }, spend);
    }

    /// <summary>
    /// Did the round pass within <see cref="HitRadiusMeters"/> of the target during this step?
    /// Closed-form over the step's relative motion (Lab 06's no-tunneling rule): both endpoints
    /// are the states BEFORE and AFTER the same time span for round and target.
    /// </summary>
    public static bool StepHits(
        Vector2d roundBefore, Vector2d roundAfter,
        Vector2d targetBefore, Vector2d targetAfter)
    {
        (double min, _, double? within) = InterceptEstimate.SegmentMin(
            roundBefore - targetBefore, roundAfter - targetAfter, HitRadiusMeters);
        return within is not null || min <= HitRadiusMeters;
    }
}
