using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE DEPOTS ON RAILS — one plunderable cargo depot per planet orbit and named haven, and the pure
/// function of sim time that says where each one is.
///
/// <para>Split out of <c>TrafficSchedule.cs</c> under #251 as a pure move: one contiguous run of the base
/// file, no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class TrafficSchedule
{
    /// <summary>
    /// One plunderable cargo depot in orbit around every planet (M22, owner: "surely there is
    /// something to steal on every planet orbit"), plus one at every named station and pirate
    /// haven (vision ¶8: the outer reaches get their own bus stops too). Depots ride RAILS —
    /// their state is a pure function of sim time (host body position + circular offset), costing
    /// nothing to step and never drifting. Cargo flavor follows the worldbuilding: compute cores
    /// at Mercury, He3 in the outer system.
    /// </summary>
    public static IReadOnlyList<NpcShip> GenerateDepots(ICelestialEphemeris ephemeris, ulong seed)
    {
        var rng = new DeterministicRandom(seed);
        var depots = new List<NpcShip>();
        CelestialBody sun = ephemeris.Bodies.First(b => b.ParentId is null);

        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.ParentId == "sun" && body.Mu == 0)
            {
                continue; // a mass-less thing on a heliocentric orbit is a drifting fixture/wreck (e.g. the
                          // derelict roadster), not a real planet or a commerce port — no depot rides it
            }

            bool isPlanet = body.ParentId == "sun";
            bool isNotable = body.Kind == BodyKind.Station || body.IsHaven;
            if (!isPlanet && !isNotable)
            {
                continue; // ordinary moons share their planet's depot; only planets, stations and havens get their own
            }

            double radius;
            if (isPlanet)
            {
                double hill = body.OrbitRadius * Math.Pow(body.Mu / (3 * sun.Mu), 1.0 / 3.0);
                radius = Math.Max(body.BodyRadius * 8, hill * 0.25);
            }
            else
            {
                // Stations/havens are small POIs, not planets — no Hill-sphere math, just a marker
                // orbit comfortably clear of the body itself.
                radius = Math.Max(body.BodyRadius * 8, 2e6);
            }

            double phase = rng.NextDouble() * Math.PI * 2;
            string cargo = body.Id switch
            {
                "mercury" or "mercury-compute" => "Compute cores",
                "venus" => "Alloys",
                "earth" => "Machinery",
                "mars" => "Ice",
                _ when body.Kind == BodyKind.Station => "Machinery",
                _ => "He3", // outer moons and havens: the black-market goods everyone's really after
            };

            depots.Add(new NpcShip(
                Id: $"depot-{body.Id}",
                Callsign: $"{body.Name} Depot",
                CargoClass: cargo,
                OriginId: body.Id,
                DestinationId: body.Id,
                Personality: RoutePersonality.Economical,
                DepartureTime: 0,
                ActivationTime: 0,
                InitialState: DepotState($"depot-{body.Id}", body.Id, radius, phase, ephemeris, 0),
                Plan: new ManeuverPlan([]),
                EstimatedArrivalTime: double.MaxValue,
                CargoUnits: 4,
                ManeuverBudget: 0,
                IsPod: false,
                DepotBodyId: body.Id,
                DepotOrbitRadius: radius,
                DepotPhase: phase));
        }

        return depots;
    }

    /// <summary>Rails state of a depot at a given time: planet position plus circular orbit.</summary>
    public static ShipState DepotState(string id, string bodyId, double radius, double phase, ICelestialEphemeris ephemeris, double simTime)
    {
        CelestialBody body = ephemeris.Bodies.First(b => b.Id == bodyId);
        double angularRate = Math.Sqrt(body.Mu / (radius * radius * radius));
        double angle = phase + angularRate * simTime;
        Vector2d center = ephemeris.Position(bodyId, simTime);
        double h = 1.0;
        Vector2d centerVel = (ephemeris.Position(bodyId, simTime + h) - ephemeris.Position(bodyId, simTime - h)) / (2 * h);
        var offset = new Vector2d(Math.Cos(angle), Math.Sin(angle)) * radius;
        var tangent = new Vector2d(-Math.Sin(angle), Math.Cos(angle)) * (angularRate * radius);
        return new ShipState(center + offset, centerVel + tangent, simTime);
    }
}
