using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #291 — He3 is the oil of the outer system. The Sol ambient hauler economy must trade like
/// Zubrin's solar economy, not commute from Earth: the busiest lanes are the giants' He3 scoop
/// corridors feeding Jupiter's energy-hungry habitat works at Europa (the Gulf-of-Hormuz
/// corridors), with Earth demoted to one node among several. These lock the DATA-driven
/// distribution so a future scenario edit can't quietly re-Earth-center the sky. Deterministic:
/// the schedule is a pure function of (scenario, seed, count).
/// </summary>
public class ZubrinTrafficTests
{
    private static readonly string[] Giants = ["jupiter", "saturn", "uranus", "neptune"];

    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    /// <summary>The top-level planet a body ultimately orbits (a moon/station borrows its planet).</summary>
    private static string SystemOf(ICelestialEphemeris ephemeris, string bodyId)
    {
        Dictionary<string, CelestialBody> byId = ephemeris.Bodies.ToDictionary(b => b.Id);
        string current = bodyId;
        while (byId.TryGetValue(current, out CelestialBody? body)
               && body.ParentId is { } parent
               && byId.TryGetValue(parent, out CelestialBody? p)
               && p.ParentId is not null)
        {
            current = parent;
        }

        return current;
    }

    private static bool IsGiantSystem(ICelestialEphemeris ephemeris, string bodyId) =>
        Giants.Contains(SystemOf(ephemeris, bodyId));

    // A generous, smooth sample: the schedule is deterministic, so a fixed (seed, count) yields the
    // same distribution every run — count 400 just averages out the per-ship route draw.
    private static IReadOnlyList<NpcShip> Sample(CircularOrbitEphemeris ephemeris) =>
        TrafficSchedule.Generate(ephemeris, seed: 42, count: 400);

    [Fact]
    public void StartingTraffic_IsNotEarthOriginated()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> ships = Sample(ephemeris);

        int earthOrigin = ships.Count(s => s.OriginId == "earth");
        double share = (double)earthOrigin / ships.Count;

        // The owner's ask: "far less Earth originated." Bound it well under a third — Earth is one
        // node, not the hub. (Actual with this seed ~16%.)
        Assert.True(share <= 0.35,
            $"Earth-origin share {share:P1} exceeds the 35% cap — the sky is re-Earth-centering.");

        // And the giants must out-source Earth by a wide margin (they are the scoop worlds).
        int giantOrigin = ships.Count(s => IsGiantSystem(ephemeris, s.OriginId));
        Assert.True(giantOrigin > earthOrigin * 2,
            $"Giant-system origins ({giantOrigin}) should dwarf Earth origins ({earthOrigin}).");
    }

    [Fact]
    public void BusiestCorridor_IsAGiantHe3ScoopLane()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> ships = Sample(ephemeris);

        // Group ships into system-to-system corridors and find the single busiest lane.
        var corridors = ships
            .GroupBy(s => (From: SystemOf(ephemeris, s.OriginId), To: SystemOf(ephemeris, s.DestinationId)))
            .Select(g => new
            {
                g.Key.From,
                g.Key.To,
                Count = g.Count(),
                TopCargo = g.GroupBy(s => s.CargoClass).OrderByDescending(c => c.Count()).First().Key,
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        var busiest = corridors[0];

        // The Gulf-of-Hormuz lane: a giant scoop world feeding another giant's works, hauling He3.
        Assert.True(Giants.Contains(busiest.From),
            $"Busiest corridor {busiest.From}->{busiest.To} should originate in a giant system.");
        Assert.True(Giants.Contains(busiest.To),
            $"Busiest corridor {busiest.From}->{busiest.To} should feed a giant-system outpost (the energy-hungry works).");
        Assert.Equal("He3", busiest.TopCargo);

        // Concretely, it is the Saturn scoop feeding Jupiter's Europa mega-works.
        Assert.Equal("saturn", busiest.From);
        Assert.Equal("jupiter", busiest.To);
    }

    [Fact]
    public void GiantOutposts_OuttradeEarth_AsDestinations()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> ships = Sample(ephemeris);

        // The energy-hungry construction outposts sit in the giants' systems (Europa/Ganymede works,
        // the ring refineries): the giants must now be a busier SINK than Earth ever was.
        int giantDest = ships.Count(s => IsGiantSystem(ephemeris, s.DestinationId));
        int earthDest = ships.Count(s => s.DestinationId == "earth");

        Assert.True(giantDest > earthDest,
            $"Giant-system destinations ({giantDest}) should exceed Earth destinations ({earthDest}) — the works are where the money is.");
    }

    [Fact]
    public void He3_IsThePlurality_OfAmbientCargo()
    {
        var ephemeris = Sol();
        IReadOnlyList<NpcShip> ships = Sample(ephemeris);

        var byCargo = ships.GroupBy(s => s.CargoClass).OrderByDescending(g => g.Count()).ToList();
        Assert.Equal("He3", byCargo[0].Key);
    }

    [Fact]
    public void RouteTable_TellsTheConstructionEconomyStory()
    {
        TrafficDefinition? traffic = Sol().Traffic;
        Assert.NotNull(traffic);
        IReadOnlyList<RouteDefinition> routes = traffic!.Routes;

        // The Zubrin supply chain in cargo labels: scoop fuel out, capital gear and hab sections in.
        foreach (string cargo in new[] { "He3", "Reactor mass", "Habitat structurals", "Mining rigs", "Deuterium slush" })
        {
            Assert.Contains(routes, r => r.Cargo == cargo);
        }

        // At least one He3 scoop corridor terminates at Jupiter's works (Europa), not at Earth.
        Assert.Contains(routes, r => r.Cargo == "He3" && r.To == "europa");

        // Construction supply flows OUTWARD to the giant outposts (a two-way economy, not extraction-to-Earth).
        Assert.Contains(routes, r => r.To is "europa" or "ganymede" && r.Cargo is "Reactor mass" or "Mining rigs" or "Habitat structurals");
    }
}
