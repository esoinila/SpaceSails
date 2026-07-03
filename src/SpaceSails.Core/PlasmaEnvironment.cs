namespace SpaceSails.Core;

/// <summary>
/// A non-gravitational force field acting on a ship. Must be a pure function of its inputs —
/// determinism is law in Core.
/// </summary>
public interface IForceField
{
    /// <summary>Acceleration contribution at a position for a ship carrying <paramref name="charge"/>.</summary>
    Vector2d Acceleration(Vector2d position, double charge, double simTime);
}

/// <summary>
/// The Electric Universe layer (plan §M7): a solar charge halo plus plasma stream ribbons
/// strung between bodies. Hull charge equilibrates toward the local ambient level; inside a
/// stream, an along-stream force proportional to charge lets a charged ship "ride the river" —
/// faster, but a hot hull glows on everyone's sensors. Scenario-gated: chargeless scenarios
/// never construct one of these, keeping Newtonian behavior bit-identical.
/// </summary>
public sealed class PlasmaEnvironment : IForceField
{
    /// <summary>Hull charge relaxation time toward ambient, seconds.</summary>
    public const double EquilibrationTau = 3600;

    /// <summary>Along-stream acceleration at full charge, m/s². Tuned so a charged run down the
    /// Saturn→Jupiter stream beats a ballistic transfer by months.</summary>
    public const double StreamAcceleration = 2e-2;

    /// <summary>Solar halo reference radius: ambient = min(1, (R/r)²). ~0.33 AU gives Mercury a
    /// hot (0.75) neighborhood and Earth a cold (0.11) one.</summary>
    public const double SolarHaloRadius = 5e10;

    private readonly ICelestialEphemeris _ephemeris;
    private readonly IReadOnlyList<(string FromId, string ToId, double HalfWidth)> _streams;

    public PlasmaEnvironment(ICelestialEphemeris ephemeris, IEnumerable<(string FromId, string ToId, double HalfWidth)> streams)
    {
        _ephemeris = ephemeris;
        _streams = [.. streams];
    }

    /// <summary>Null when the scenario doesn't enable the Electric Universe layer.</summary>
    public static PlasmaEnvironment? FromScenario(Contracts.ScenarioDefinition scenario, ICelestialEphemeris ephemeris)
    {
        if (!scenario.ElectricUniverse)
        {
            return null;
        }

        return new PlasmaEnvironment(
            ephemeris,
            scenario.Streams.Select(s => (s.FromBodyId, s.ToBodyId, s.HalfWidthM)));
    }

    public IReadOnlyList<(string FromId, string ToId, double HalfWidth)> Streams => _streams;

    /// <summary>Local ambient charge level in [0, 1]: solar halo, saturated inside any stream.</summary>
    public double AmbientCharge(Vector2d position, double simTime)
    {
        foreach ((string fromId, string toId, double halfWidth) in _streams)
        {
            if (DistanceToStream(position, fromId, toId, simTime, out _) <= halfWidth)
            {
                return 1.0;
            }
        }

        double r = position.Length;
        if (r <= 0)
        {
            return 1.0;
        }

        double halo = SolarHaloRadius / r;
        return Math.Min(1.0, halo * halo);
    }

    public Vector2d Acceleration(Vector2d position, double charge, double simTime)
    {
        if (charge <= 0)
        {
            return Vector2d.Zero;
        }

        Vector2d total = Vector2d.Zero;
        foreach ((string fromId, string toId, double halfWidth) in _streams)
        {
            if (DistanceToStream(position, fromId, toId, simTime, out Vector2d direction) <= halfWidth)
            {
                total += direction * (StreamAcceleration * charge);
            }
        }

        return total;
    }

    /// <summary>
    /// Distance from a point to the from→to segment at <paramref name="simTime"/>;
    /// <paramref name="direction"/> is the normalized along-stream (from→to) vector.
    /// </summary>
    private double DistanceToStream(Vector2d position, string fromId, string toId, double simTime, out Vector2d direction)
    {
        Vector2d a = _ephemeris.Position(fromId, simTime);
        Vector2d b = _ephemeris.Position(toId, simTime);
        Vector2d ab = b - a;
        double lengthSquared = ab.LengthSquared;
        direction = lengthSquared > 0 ? ab / Math.Sqrt(lengthSquared) : Vector2d.Zero;

        if (lengthSquared == 0)
        {
            return (position - a).Length;
        }

        Vector2d ap = position - a;
        double t = Math.Clamp((ap.X * ab.X + ap.Y * ab.Y) / lengthSquared, 0, 1);
        Vector2d closest = a + ab * t;
        return (position - closest).Length;
    }
}
