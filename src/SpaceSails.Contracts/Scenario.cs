namespace SpaceSails.Contracts;

/// <summary>
/// A scenario data file (e.g. scenarios/sol.json): the celestial topography the engine runs.
/// The engine is cosmology-agnostic — the Sol system and the Wheel of the World are both just body lists.
/// </summary>
public sealed record ScenarioDefinition
{
    public required string Name { get; init; }

    public string Description { get; init; } = "";

    public required IReadOnlyList<BodyDefinition> Bodies { get; init; }
}

/// <summary>
/// A celestial body on rails: circular orbit around its parent (or fixed at the origin when it has no parent).
/// All units SI: meters, seconds, m^3/s^2.
/// </summary>
public sealed record BodyDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Id of the body this one orbits; null = fixed at the system origin.</summary>
    public string? ParentId { get; init; }

    /// <summary>Standard gravitational parameter GM in m^3/s^2. Zero = massless marker.</summary>
    public required double Mu { get; init; }

    /// <summary>Physical radius of the body in meters (rendering and collision).</summary>
    public double BodyRadiusM { get; init; }

    /// <summary>Circular orbit radius around the parent in meters. Zero when ParentId is null.</summary>
    public double OrbitRadiusM { get; init; }

    /// <summary>Orbital period in seconds. Positive = counterclockwise, negative = clockwise.</summary>
    public double OrbitPeriodS { get; init; }

    /// <summary>Angular position on the orbit at simulation time zero, in radians.</summary>
    public double InitialPhaseRad { get; init; }
}
