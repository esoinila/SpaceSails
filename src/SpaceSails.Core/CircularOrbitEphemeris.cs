using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// Analytic circular orbits with parent chaining (a body may orbit a body that orbits another —
/// the Wheel of the World scenario relies on this). Deterministic and allocation-free per query.
/// </summary>
public sealed class CircularOrbitEphemeris : ICelestialEphemeris
{
    private readonly Dictionary<string, CelestialBody> _byId;
    private readonly List<CelestialBody> _bodies;
    private readonly TrafficDefinition? _traffic;

    public CircularOrbitEphemeris(IEnumerable<CelestialBody> bodies, TrafficDefinition? traffic = null)
    {
        _bodies = [.. bodies];
        _byId = _bodies.ToDictionary(b => b.Id);
        _traffic = traffic;

        foreach (CelestialBody body in _bodies)
        {
            if (body.ParentId is not null && !_byId.ContainsKey(body.ParentId))
            {
                throw new ArgumentException($"Body '{body.Id}' orbits unknown parent '{body.ParentId}'.");
            }
        }
    }

    public static CircularOrbitEphemeris FromScenario(ScenarioDefinition scenario) =>
        new(scenario.Bodies.Select(b => new CelestialBody(
            b.Id, b.Name, b.ParentId, b.Mu, b.BodyRadiusM, b.OrbitRadiusM, b.OrbitPeriodS, b.InitialPhaseRad,
            ParseKind(b.Kind), b.Haven)),
            scenario.Traffic);

    private static BodyKind ParseKind(string kind) => kind switch
    {
        "moon" => BodyKind.Moon,
        "station" => BodyKind.Station,
        _ => BodyKind.Planet,
    };

    public IReadOnlyList<CelestialBody> Bodies => _bodies;

    public TrafficDefinition? Traffic => _traffic;

    public Vector2d Position(string bodyId, double simTime)
    {
        CelestialBody body = _byId[bodyId];
        Vector2d parentPosition = body.ParentId is null ? Vector2d.Zero : Position(body.ParentId, simTime);

        if (body.OrbitPeriod == 0)
        {
            return parentPosition;
        }

        double angle = body.InitialPhase + Math.Tau * simTime / body.OrbitPeriod;
        return parentPosition + new Vector2d(body.OrbitRadius * Math.Cos(angle), body.OrbitRadius * Math.Sin(angle));
    }
}
