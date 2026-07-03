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

    public double RangeMeters { get; } = rangeMeters;
    public double GlareHalfAngleRad { get; } = glareHalfAngleRad;
    public double GlareRangeFactor { get; } = glareRangeFactor;

    public bool TryObserve(Vector2d observerPosition, string targetId, ShipState target, double simTime, out Observation observation)
    {
        Vector2d toTarget = target.Position - observerPosition;
        double distance = toTarget.Length;

        double effectiveRange = RangeMeters;
        Vector2d toSun = Vector2d.Zero - observerPosition;
        double sunDistance = toSun.Length;
        if (distance > 0 && sunDistance > 0)
        {
            double cosAngle = (toTarget.X * toSun.X + toTarget.Y * toSun.Y) / (distance * sunDistance);
            if (cosAngle > Math.Cos(GlareHalfAngleRad))
            {
                effectiveRange *= GlareRangeFactor;
            }
        }

        if (distance > effectiveRange)
        {
            observation = default;
            return false;
        }

        observation = new Observation(targetId, simTime, target.Position, target.Velocity);
        return true;
    }
}
