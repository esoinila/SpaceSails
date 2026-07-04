namespace SpaceSails.Core;

/// <summary>A timestamped sensor contact: what the observer knew about the target, and when.</summary>
public readonly record struct Observation(string TargetId, double SimTime, Vector2d Position, Vector2d Velocity);

/// <summary>
/// What a sensor can see. Pure and deterministic: the same geometry always yields the same
/// answer, so client and server agree about who saw what (the server enforces it from M9).
/// </summary>
public interface IObservationModel
{
    bool TryObserve(Vector2d observerPosition, string targetId, ShipState target, double simTime, out Observation observation);
}

/// <summary>
/// Range-limited sensor with a sun-glare penalty: a target that sits within the glare cone
/// (looking from the observer toward the Sun at the system origin) is only detectable at
/// <see cref="GlareRangeFactor"/> × range. Low solar orbits are where ambushes live (plan §M7).
/// </summary>
public sealed class SensorModel(double rangeMeters, double glareHalfAngleRad, double glareRangeFactor) : IObservationModel
{
    /// <summary>Defaults tuned for the Sol scenario: 1e11 m ≈ 0.67 AU, 20° glare cone at ×0.25 range.</summary>
    public static SensorModel Default { get; } = new(1.0e11, 20.0 * Math.PI / 180.0, 0.25);

    /// <summary>A fully charged hull glows: it is seen (1 + this) × farther. M7.</summary>
    public const double ChargeGlowFactor = 2.0;

    public double RangeMeters { get; } = rangeMeters;
    public double GlareHalfAngleRad { get; } = glareHalfAngleRad;
    public double GlareRangeFactor { get; } = glareRangeFactor;

    public bool TryObserve(Vector2d observerPosition, string targetId, ShipState target, double simTime, out Observation observation)
    {
        double distance = (target.Position - observerPosition).Length;
        if (distance > EffectiveRange(observerPosition, target.Position, target.Charge))
        {
            observation = default;
            return false;
        }

        observation = new Observation(targetId, simTime, target.Position, target.Velocity);
        return true;
    }

    /// <summary>
    /// How far this sensor sees toward a specific target: sun glare dims passive detection,
    /// while a charged hull *radiates* and that glow pierces glare untouched — the sun-side
    /// ambush tradeoff (plan §M7). Exposed for the M27 mutual-visibility readout.
    /// </summary>
    public double EffectiveRange(Vector2d observerPosition, Vector2d targetPosition, double targetCharge)
    {
        Vector2d toTarget = targetPosition - observerPosition;
        double distance = toTarget.Length;
        double baseRange = RangeMeters;
        Vector2d toSun = Vector2d.Zero - observerPosition;
        double sunDistance = toSun.Length;
        if (distance > 0 && sunDistance > 0)
        {
            double cosAngle = toTarget.Dot(toSun) / (distance * sunDistance);
            if (cosAngle > Math.Cos(GlareHalfAngleRad))
            {
                baseRange *= GlareRangeFactor;
            }
        }

        return baseRange + RangeMeters * ChargeGlowFactor * targetCharge;
    }

    /// <summary>
    /// M27: who spots whom first, assuming the other side carries the same sensor fit. The
    /// telescope's double duty — watching targets AND minding our own signature toward them
    /// (our hull charge glows in THEIR range term; their glare cone is computed from THEIR sky).
    /// </summary>
    public SightAdvantage Advantage(ShipState us, ShipState them)
    {
        double distance = (them.Position - us.Position).Length;
        double ourRange = EffectiveRange(us.Position, them.Position, them.Charge);
        double theirRange = EffectiveRange(them.Position, us.Position, us.Charge);
        return new SightAdvantage(distance, ourRange, theirRange,
            distance <= ourRange, distance <= theirRange);
    }
}

/// <summary>M27: the two detection bubbles at a glance — ours on them, theirs on us.</summary>
public readonly record struct SightAdvantage(
    double Distance, double OurRange, double TheirRange, bool WeSeeThem, bool TheySeeUs)
{
    /// <summary>Positive: we'd spot them before they spot us; negative: they win the eyes race.</summary>
    public double Edge => OurRange - TheirRange;
}

/// <summary>
/// M27: the close-range active radar — exact returns inside its range regardless of sun glare
/// or a dark hull. The price is being LOUD: every ship in earshot of the ping learns exactly
/// where we are (it wakes their "aware" flag). For tight spots where stealth is already spent.
/// </summary>
public static class RadarRule
{
    /// <summary>Precise returns inside this range.</summary>
    public const double RangeMeters = 5e9;

    /// <summary>Ships this far out hear the ping and become aware of us.</summary>
    public const double LoudRangeMeters = 5e10;

    public static bool InRange(Vector2d us, Vector2d them) =>
        (them - us).LengthSquared <= RangeMeters * RangeMeters;

    public static bool HearsPing(Vector2d us, Vector2d them) =>
        (them - us).LengthSquared <= LoudRangeMeters * LoudRangeMeters;
}
