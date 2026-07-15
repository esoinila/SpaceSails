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

    /// <summary>Enables the Electric Universe layer (charge, plasma streams). Off = pure Newtonian.</summary>
    public bool ElectricUniverse { get; init; }

    /// <summary>Plasma stream ribbons. Only meaningful when <see cref="ElectricUniverse"/> is true.</summary>
    public IReadOnlyList<StreamDefinition> Streams { get; init; } = [];

    /// <summary>
    /// Data-driven traffic (routes + pod launchers). Optional: scenarios without one (e.g. the
    /// Wheel of the World) fall back to <c>TrafficSchedule</c>'s built-in hardcoded tables.
    /// </summary>
    public TrafficDefinition? Traffic { get; init; }
}

/// <summary>
/// A scenario's traffic content: which cargo runs where, and which small bodies launch
/// mass-driver pods. De-Earth-centering (vision ¶8) lives here — central-space routes publish
/// timetables, outer-reaches routes don't have to.
/// </summary>
public sealed record TrafficDefinition
{
    public IReadOnlyList<RouteDefinition> Routes { get; init; } = [];

    public IReadOnlyList<PodLauncherDefinition> PodLaunchers { get; init; } = [];
}

/// <summary>One cargo run a scenario's traffic generator can draw from.</summary>
public sealed record RouteDefinition
{
    public required string From { get; init; }

    public required string To { get; init; }

    public required string Cargo { get; init; }

    /// <summary>Relative pick weight among routes offered to a given ship (mid-flight vs. scheduled).</summary>
    public double Weight { get; init; } = 1.0;

    /// <summary>
    /// False = a secretive hauler (He3 out of pirate country, worldbuilding notes §4): the ship
    /// still flies and is still visible to sensors in range, it just never appears on the public
    /// departures board (<c>NpcShip.PublishesTimetable</c>).
    /// </summary>
    public bool PublishesTimetable { get; init; } = true;
}

/// <summary>A mass-driver launch site (worldbuilding notes §1): a body that fires ballistic cargo pods.</summary>
public sealed record PodLauncherDefinition
{
    public required string Body { get; init; }

    public required string Cargo { get; init; }
}

/// <summary>
/// A plasma stream: a straight ribbon between two bodies (endpoints track their orbits).
/// Ships inside feel an along-stream force proportional to their charge.
/// </summary>
public sealed record StreamDefinition
{
    public required string FromBodyId { get; init; }

    public required string ToBodyId { get; init; }

    /// <summary>Half-width of the ribbon in meters.</summary>
    public required double HalfWidthM { get; init; }
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

    /// <summary>
    /// Angular position on the orbit at simulation time zero, in radians. For a circular orbit
    /// (<see cref="Eccentricity"/> = 0) this is the polar angle from +X. For an elliptical orbit it
    /// is the <em>mean anomaly at epoch</em> — the two coincide when e = 0, which is what keeps every
    /// existing circular scenario byte-identical.
    /// </summary>
    public double InitialPhaseRad { get; init; }

    /// <summary>
    /// Orbit eccentricity (Kepler rails, PR-B). Zero (the default) is a circle and takes the exact
    /// legacy circular formula — byte-identical to before this field existed. In [0, 1): a bound
    /// ellipse whose semi-major axis is <see cref="OrbitRadiusM"/>. Values &lt; 0 or ≥ 1 are rejected
    /// by the loader (no hyperbolic/degenerate orbits on rails).
    /// </summary>
    public double Eccentricity { get; init; }

    /// <summary>
    /// Argument of periapsis in radians (Kepler rails, PR-B): the orbit-plane angle, measured from
    /// +X, of the periapsis direction. Ignored when <see cref="Eccentricity"/> = 0 (a circle has no
    /// periapsis). Default 0 points periapsis along +X.
    /// </summary>
    public double ArgPeriapsisRad { get; init; }

    /// <summary>Body classification: "planet" (default), "moon", or "station" (a lightweight
    /// orbital POI — compute farm, factory, trading post). Unknown values fall back to "planet".</summary>
    public string Kind { get; init; } = "planet";

    /// <summary>
    /// Marks a small, out-of-the-way body as a pirate haven (vision ¶8: scum & villainy work the
    /// outer reaches, not next to the central powers) — trade and repair happen here, no
    /// questions asked.
    /// </summary>
    public bool Haven { get; init; }

    /// <summary>
    /// A body that is on its rail but NOT on the charts (Tuesday plan pillar 1, "Expanse rules"):
    /// it does not draw on the map, answer the picker, appear in the scope carousel, "Nearest", or
    /// any body menu until the client reveals it — a targeted, intel-fed scan finds it. Off by
    /// default, so every existing scenario body stays plainly visible.
    /// </summary>
    public bool Hidden { get; init; }

    /// <summary>
    /// Optional exponential atmosphere shell for aerobraking (the skim/skip flight assists, PR-H).
    /// Null (the default) means the body has no atmosphere and imposes zero drag — every existing
    /// scenario body keeps its exact vacuum trajectory. See <see cref="AtmosphereDefinition"/>.
    /// </summary>
    public AtmosphereDefinition? Atmosphere { get; init; }
}

/// <summary>
/// An exponential atmosphere on a body: reference density at the surface, an isothermal scale
/// height, and a hard shell-top altitude above which density is exactly zero. Game-tuned, not a
/// full atmosphere model — the drag it produces is the one aerobrake knob (see the ballistic
/// coefficient in <c>Simulator</c>). All units SI.
/// </summary>
public sealed record AtmosphereDefinition
{
    /// <summary>Mass density at the body's surface (altitude 0), in kg/m^3.</summary>
    public required double RefDensity { get; init; }

    /// <summary>e-folding altitude of the exponential density falloff, in meters.</summary>
    public required double ScaleHeightM { get; init; }

    /// <summary>Altitude in meters above which density is clamped to exactly zero (the shell top).</summary>
    public required double TopAltitudeM { get; init; }
}
