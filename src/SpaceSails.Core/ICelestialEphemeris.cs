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
    /// Instantaneous distance from the body to its parent at <paramref name="simTime"/> (0 for a
    /// parentless body). For a circular body this is exactly its orbit radius at every instant; for
    /// an elliptical body it swings between periapsis a(1−e) and apoapsis a(1+e). This is the radius
    /// Hill-sphere and capture math should read (PR-B), so an eccentric haven's window tracks its
    /// real, changing distance instead of a fixed circle. The default measures it straight off
    /// <see cref="Position"/>, so every implementer gets the honest value for free.
    /// </summary>
    double InstantaneousOrbitRadius(string bodyId, double simTime)
    {
        CelestialBody body = Bodies.First(b => b.Id == bodyId);
        return body.ParentId is null ? 0.0 : (Position(bodyId, simTime) - Position(body.ParentId, simTime)).Length;
    }

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

/// <summary>Runtime model of a celestial body. SI units.
///
/// <para><b>Kepler rails (PR-B).</b> <see cref="Eccentricity"/> defaults to 0 — a circle, for which
/// <see cref="OrbitRadius"/> is the radius and <see cref="InitialPhase"/> is the polar angle, exactly
/// as before this field existed. For a non-zero eccentricity the orbit is an ellipse whose
/// semi-major axis is <see cref="OrbitRadius"/>, whose periapsis points along <see cref="ArgPeriapsis"/>
/// (radians from +X), and whose <see cref="InitialPhase"/> is the mean anomaly at epoch. e = 0 keeps
/// the legacy formula bit-for-bit.</para></summary>
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
    Atmosphere? Atmosphere = null,
    double Eccentricity = 0.0,
    double ArgPeriapsis = 0.0);
