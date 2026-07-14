using SpaceSails.Contracts;

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

    /// <summary>
    /// The source scenario's data-driven traffic definition, when it provided one. Null (the
    /// default for any implementer that doesn't override it) means <c>TrafficSchedule</c> falls
    /// back to its built-in hardcoded route tables.
    /// </summary>
    TrafficDefinition? Traffic => null;
}

/// <summary>Body classification. Moons/stations don't automatically get their own orbital depot
/// (<see cref="TrafficSchedule.GenerateDepots"/>) unless flagged a station or a haven.</summary>
public enum BodyKind
{
    Planet,
    Moon,
    Station,
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
    double InitialPhase,
    BodyKind Kind = BodyKind.Planet,
    bool IsHaven = false,
    Atmosphere? Atmosphere = null);
