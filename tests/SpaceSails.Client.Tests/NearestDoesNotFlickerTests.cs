using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #954 · "NEAREST" FLICKERS BETWEEN MARS AND THE RUSTY ROADSTEAD EVERY ORBIT — asked of a real
/// <see cref="Pages.Map"/>, on the owner's own geometry.
///
/// <para>His HUD alternated between <i>"Nearest: The Rusty Roadstead (0.16 AU, 4.7 km/s rel) · dockable
/// haven"</i> and <i>"Nearest: Mars (…)"</i>. Both lines were true, which is why the fix is not a tie-break
/// but a pair of them: the incumbent keeps the slot unless a challenger beats it by a real margin
/// (<see cref="NearestRule.Unseats"/>), and the two that used to trade places are named TOGETHER —
/// "Mars › The Rusty Roadstead" — because the readout had one slot for a fact that needs two.</para>
///
/// <para><see cref="Core.Tests"/> holds the band as a pure law. THIS drives the actual per-frame sweep
/// (<c>UpdateNearestBody</c>) over a full station orbit and reads what the HUD line and the scope's AUTO
/// lock would have said on every frame of it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class NearestDoesNotFlickerTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const double AU = 1.495978707e11;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    // sol.json: the-space-bar rides Mars at 12,000 km. #957 corrected its period from a hand-typed
    // 7,200 s to Kepler's 39,910 s — the sweep below wants ONE station orbit, whatever that is, so it
    // reads the rail rather than carrying a second copy of the number that was wrong in the first place.
    private static double RoadsteadPeriod =>
        Math.Abs(Sol.Value.Bodies.Single(b => b.Id == "the-space-bar").OrbitPeriodS);

    private static string ScenarioPath(string file)
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("no scenarios/ directory above the test binary")
            : System.IO.Path.Combine(dir.FullName, "scenarios", file);
    }

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);

    /// <summary>
    /// The ship parked 0.16 AU sunward of Mars, exactly where the owner's readout was taken: far enough
    /// that Mars and the station in its Hill sphere are the same distance to four decimal places, which is
    /// what makes "which is nearest" a coin toss twice per station orbit.
    /// </summary>
    private static Pages.Map ParkedOffMars()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));

        Vector2d mars = ephemeris.Position("mars", 0);
        Vector2d awayFromTheSun = mars / mars.Length;              // straight out along Mars's radius
        Set(map, "_ship", new ShipState(mars + awayFromTheSun * (0.16 * AU), Vector2d.Zero, 0.0));
        return map;
    }

    /// <summary>Every id the nearest slot held over one full station orbit, sampled fine enough to catch a
    /// swap that lasts a single frame.</summary>
    private static List<string> NearestOverOneStationOrbit(Pages.Map map, out List<string> readouts)
    {
        var ids = new List<string>();
        var lines = new List<string>();
        for (int step = 0; step <= 360; step++)
        {
            Set(map, "SimTime", RoadsteadPeriod * step / 360.0);
            Invoke(map, "UpdateNearestBody");
            ids.Add(Get<CelestialBody?>(map, "_nearestBody")?.Id ?? "(none)");
            lines.Add((string)Invoke(map, "NearestReadoutName")!);
        }

        readouts = lines;
        return ids;
    }

    /// <summary>The premise: from here the candidates really are neck and neck, and the OLD code — which
    /// took the literal minimum every frame — really would have swapped. Without this the guards below could
    /// pass on a world that cannot tell pass from fail.</summary>
    [Fact]
    public void ThePremise_TheLiteralNearestReallyDoesChangeHandsEveryOrbit()
    {
        Pages.Map map = ParkedOffMars();
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        ShipState ship = Get<ShipState>(map, "_ship");

        var literal = new HashSet<string>(StringComparer.Ordinal);
        for (int step = 0; step <= 360; step++)
        {
            double t = RoadsteadPeriod * step / 360.0;
            string closest = eph.Bodies
                .OrderBy(b => (ship.Position - eph.Position(b.Id, t)).LengthSquared)
                .First().Id;
            literal.Add(closest);
        }

        Assert.True(literal.Count > 1,
            "the literal minimum never changed hands over a whole station orbit, so this bench is not " +
            "standing where the owner stood and the guards below would prove nothing. Ids seen: " +
            string.Join(", ", literal));

        // …and every one of them is the SAME PLACE as far as a captain is concerned: Mars, or something
        // riding inside Mars's Hill sphere. That is the whole shape of the bug — the readout had one slot
        // for a neighbourhood, so it spent the orbit choosing between members of it.
        Assert.Contains("the-space-bar", literal);
        foreach (string id in literal)
        {
            CelestialBody body = eph.Bodies.First(b => b.Id == id);
            Assert.True(id == "mars" || body.ParentId == "mars",
                $"\"{id}\" is neither Mars nor in Mars's Hill sphere, so the flicker this bench reproduces " +
                "is not the flicker the owner reported.");
        }
    }

    [Fact]
    public void THE_HUD_HoldsOneAnswerForAWholeStationOrbit()
    {
        // #954 REGRESSION. RED CASE: take the hysteresis out of UpdateNearestBody (assign the running
        // minimum straight to _nearestBody, as it did) and this finds both ids in the list.
        Pages.Map map = ParkedOffMars();
        List<string> ids = NearestOverOneStationOrbit(map, out _);

        Assert.True(ids.Distinct(StringComparer.Ordinal).Count() == 1,
            "the nearest reading changed hands during one station orbit — the flicker the owner watched. " +
            "It held: " + string.Join(" → ", ids.Distinct(StringComparer.Ordinal)));
    }

    [Fact]
    public void THE_LINE_NamesThePlanetAndTheStationInIt_WhicheverHoldsTheSlot()
    {
        // The owner's ask: "present the hierarchy — Mars is closest and it contains (in its Hill sphere)
        // The Rusty Roadstead." The line must read the same all orbit, and it must carry BOTH names, so it
        // no longer matters which of the pair won the contest.
        Pages.Map map = ParkedOffMars();
        NearestOverOneStationOrbit(map, out List<string> readouts);

        Assert.True(readouts.Distinct(StringComparer.Ordinal).Count() == 1,
            "the Nearest line changed its words during one station orbit: " +
            string.Join(" | ", readouts.Distinct(StringComparer.Ordinal)));

        string line = readouts[0];
        Assert.StartsWith("Mars › ", line, StringComparison.Ordinal);
        Assert.Equal(NearestRule.Hierarchy("Mars", "The Rusty Roadstead"), line);
    }

    [Fact]
    public void THE_ANCHOR_TheDockableHavenHintFollowsTheNeighbourhood()
    {
        // The ⚓ hint used to be gated on the nearest body ITSELF being a dockable haven, so it blinked out
        // on every frame the planet held the slot — the flicker leaking into a second readout. It now
        // follows the neighbourhood's haven, which is the thing the captain would actually clamp onto.
        Pages.Map map = ParkedOffMars();
        for (int step = 0; step <= 360; step++)
        {
            Set(map, "SimTime", RoadsteadPeriod * step / 360.0);
            Invoke(map, "UpdateNearestBody");
            CelestialBody? haven = Get<CelestialBody?>(map, "_nearestHaven");
            Assert.Equal("the-space-bar", haven?.Id);
        }
    }

    [Fact]
    public void A_REAL_CHANGE_OF_NEIGHBOURHOOD_StillMovesTheReading()
    {
        // The band must not weld the readout shut. Fly the ship from beside Mars to beside Jupiter and the
        // nearest reading has to follow — a "Nearest" that never changes is the same bug wearing the
        // opposite face.
        Pages.Map map = ParkedOffMars();
        Set(map, "SimTime", 0.0);
        Invoke(map, "UpdateNearestBody");

        // The reading starts in Mars's neighbourhood. It used to be the STATION that held the slot from out
        // here; since NearestRule.StandsForItself it is Mars, because a berth outside its own Hill sphere
        // defers to the planet it rides. Either is "Mars's neighbourhood" — which is all this guard is
        // establishing before it flies the ship away.
        CelestialBody startedAt = Get<CelestialBody?>(map, "_nearestBody")!;
        Assert.True(startedAt.Id == "mars" || startedAt.ParentId == "mars",
            $"parked 0.16 AU off Mars, the nearest reading was \"{startedAt.Id}\" — not Mars's neighbourhood.");

        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        Vector2d jupiter = eph.Position("jupiter", 0);
        Set(map, "_ship", new ShipState(jupiter + new Vector2d(1.0e9, 0), Vector2d.Zero, 0.0));
        Invoke(map, "UpdateNearestBody");

        string? now = Get<CelestialBody?>(map, "_nearestBody")?.Id;
        Assert.True(now == "jupiter" || Array.Exists(eph.Bodies.ToArray(), b => b.Id == now && b.ParentId == "jupiter"),
            $"parked a million kilometres off Jupiter, the nearest reading still said \"{now}\". The " +
            "hysteresis band is a tie-break, not a lock.");
    }
}
