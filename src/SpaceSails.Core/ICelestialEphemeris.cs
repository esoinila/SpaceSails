namespace SpaceSails.Core;

/// <summary>
/// Celestial bodies on rails: position is a pure function of simulation time.
/// This is what makes plotting-mode time scrubbing instant and deterministic.
/// </summary>
public interface ICelestialEphemeris
{
    IReadOnlyList<CelestialBody> Bodies { get; }

    /// <summary>Absolute position of a body at simulation time <paramref name="simTime"/> (seconds).</summary>
    Vector2d Position(string bodyId, double simTime);
}

/// <summary>Runtime model of a celestial body. SI units.</summary>
public sealed record CelestialBody(
    string Id,
    string Name,
    string? ParentId,
    double Mu,
    double BodyRadius,
    double OrbitRadius,
    double OrbitPeriod,
    double InitialPhase);
