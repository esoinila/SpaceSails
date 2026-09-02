using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #954 · "FLICKERING ON EVERY ORBIT IS ANNOYING HERE" — the whole system, not just the one pair.
///
/// <para><see cref="NearestDoesNotFlickerTests"/> stands where the owner stood: 0.16 AU off Mars, watching
/// "Nearest" alternate between Mars and The Rusty Roadstead. The band that fixed it is measured along the
/// SIGHTLINE, so it shrinks as the ship closes — and the same flicker came back everywhere the ship
/// actually flies. Parked 100,000 km off Earth the reading changed hands 1,744 times in five orbits of the
/// low-orbit factory; off Saturn 136, off Neptune 144, off Jupiter 76. Every one of those swaps is between
/// members of ONE neighbourhood, which is the exact bug the owner filed, just nearer in.</para>
///
/// <para>The law that settles it: a satellite defers to its primary until the ship is inside its Hill
/// sphere (<see cref="NearestRule.StandsForItself"/>), and the primary's berth is named from its RAIL
/// rather than from where the rail has carried it this second. Both halves are phase-independent, which is
/// why this bench can demand ZERO changes of mind over many orbits rather than merely fewer.</para>
///
/// <para><b>This bench's world.</b> It cannot look at pixels, so it asserts the mechanism: it drives the
/// real per-frame sweep (<c>UpdateNearestBody</c>) on the real scenario, holding the ship at a fixed range
/// from a planet while that planet's family goes round, and reads the two things the captain actually sees
/// change — the id the slot holds (the scope's AUTO picture and the HUD's distance and closing speed all
/// hang off it) and the words of the "Nearest:" line. <see cref="ThePremise_TheLiteralNearestBlinksAtThese
/// Posts"/> proves the world can tell pass from fail: the LITERAL nearest — what the sweep reported before
/// this law and would report again the moment it came out — really does change hands at these posts, and
/// where it does not, that is asserted too rather than skipped.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 34 s over 99 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class NearestHoldsTheNeighbourhoodTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    /// <summary>Where the ship is held while the family turns: a planet, and how far off it to park.
    /// The ranges are the ones the flicker was found at — from a long approach down to close aboard.</summary>
    private static IEnumerable<(string Planet, double Metres)> Posts()
    {
        string[] planets = ["mercury", "venus", "earth", "mars", "jupiter", "saturn", "uranus", "neptune"];
        double[] ranges = [1.0e10, 1.0e9, 3.0e8, 1.0e8];
        foreach (string planet in planets)
        {
            foreach (double metres in ranges)
            {
                yield return (planet, metres);
            }
        }
    }

    public static TheoryData<string, double> ThePosts()
    {
        var data = new TheoryData<string, double>();
        foreach ((string planet, double metres) in Posts())
        {
            data.Add(planet, metres);
        }

        return data;
    }

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

    private static Pages.Map Booted()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_ship", new ShipState(Vector2d.Zero, Vector2d.Zero, 0.0));
        return map;
    }

    /// <summary>How long the family takes to come round: five orbits of the planet's slowest satellite, so
    /// every member of it has been near the ship and far from it several times over.</summary>
    private static double WatchWindow(string planet) =>
        5.0 * Sol.Value.Bodies.Where(b => b.ParentId == planet)
            .Select(b => Math.Abs(b.OrbitPeriodS)).DefaultIfEmpty(1.0e5).Max();

    private const int Samples = 2000;

    /// <summary>Hold the ship at a fixed range from the planet (station-keeping in its frame, which is what
    /// the owner's screenshot is: a ship that is not going anywhere) and step the whole watch window.</summary>
    private static void WatchTheFamilyTurn(
        Pages.Map map, string planet, double metres,
        out List<string> slotIds, out List<string> readouts, out List<string> literalNearest)
    {
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        double window = WatchWindow(planet);
        slotIds = [];
        readouts = [];
        literalNearest = [];

        for (int step = 0; step <= Samples; step++)
        {
            double t = window * step / Samples;
            Set(map, "SimTime", t);
            Vector2d here = eph.Position(planet, t);
            Vector2d ship = here + (here / here.Length) * metres;
            Set(map, "_ship", new ShipState(ship, Vector2d.Zero, 0.0));

            Invoke(map, "UpdateNearestBody");
            slotIds.Add(Get<CelestialBody?>(map, "_nearestBody")?.Id ?? "(none)");
            readouts.Add((string)Invoke(map, "NearestReadoutName")!);

            string closest = "(none)";
            double best = double.MaxValue;
            foreach (CelestialBody body in eph.Bodies)
            {
                double d = (ship - eph.Position(body.Id, t)).LengthSquared;
                if (d < best) (best, closest) = (d, body.Id);
            }

            literalNearest.Add(closest);
        }
    }

    private static string Post(string planet, double metres) =>
        $"{planet} at {(metres / 1000).ToString("N0", CultureInfo.InvariantCulture)} km";

    /// <summary>
    /// THE PREMISE — without this the two guards below could pass on a world that cannot tell pass from
    /// fail. At every post, the LITERAL nearest body (what the sweep used to report, and what it would
    /// report again the moment the neighbourhood law came out) changes hands while the family turns, and
    /// every id it changes between is in that planet's Hill sphere. That is the owner's bug, reproduced
    /// thirty-two times over.
    /// </summary>
    [Fact]
    public void ThePremise_TheLiteralNearestBlinksAtThesePosts()
    {
        var blinkingPlanets = new HashSet<string>(StringComparer.Ordinal);
        var quiet = new List<string>();

        foreach ((string planet, double metres) in Posts())
        {
            Pages.Map map = Booted();
            WatchTheFamilyTurn(map, planet, metres, out _, out _, out List<string> literal);
            ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");

            // Whatever it reports, it is a member of THIS neighbourhood — otherwise the bench has wandered
            // off the bug and the guards below would be watching the wrong sky.
            var seen = literal.Distinct(StringComparer.Ordinal).ToList();
            foreach (string id in seen)
            {
                CelestialBody body = eph.Bodies.First(b => b.Id == id);
                Assert.True(id == planet || body.ParentId == planet,
                    $"{Post(planet, metres)}: the literal nearest wandered to \"{id}\", which is outside " +
                    $"{planet}'s Hill sphere — this bench is no longer watching one neighbourhood.");
            }

            // A post is one of two kinds, and both are asserted rather than skipped: either the literal
            // reading changed hands while the ship went nowhere — the owner's flicker, live — or it never
            // left the planet, which is the only other honest shape (a ship 30,000 km over Jupiter's cloud
            // tops has nothing to argue with: the nearest moon is 600,000 km further out).
            if (seen.Count > 1)
            {
                blinkingPlanets.Add(planet);
            }
            else
            {
                Assert.Equal(planet, Assert.Single(seen));
                quiet.Add(Post(planet, metres));
            }
        }

        // …and every planet that keeps more than one thing in its Hill sphere blinks SOMEWHERE among its
        // posts. Without this the whole bench could quietly decay into thirty-two of the boring kind and
        // still pass while reporting nothing.
        foreach (string planet in Sol.Value.Bodies
            .Where(b => b.ParentId is not null && b.ParentId != "sun")
            .GroupBy(b => b.ParentId!, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key))
        {
            Assert.True(blinkingPlanets.Contains(planet),
                $"{planet} keeps several bodies in its Hill sphere yet the literal nearest never changed " +
                "hands at any post — this bench has stopped reproducing the bug. Quiet posts: " +
                string.Join("; ", quiet));
        }
    }

    /// <summary>
    /// #954 REGRESSION — THE SLOT. RED CASE: drop the <c>StandsForItself</c> filter out of
    /// <c>UpdateNearestBody</c> (both the sweep's <c>continue</c> and the incumbent's) and this goes red at
    /// most of the thirty-two posts — 1,744 changes of mind at Earth, 144 at Neptune, 136 at Saturn.
    /// <para>The slot is not just a word: the scope's AUTO lock draws whatever body holds it, and the HUD
    /// quotes that body's range and closing speed, so every swap here is a picture and two numbers jumping
    /// in front of the captain.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(ThePosts))]
    public void THE_SLOT_HoldsOneBodyWhileTheWholeFamilyGoesRound(string planet, double metres)
    {
        Pages.Map map = Booted();
        WatchTheFamilyTurn(map, planet, metres, out List<string> ids, out _, out _);

        var held = ids.Distinct(StringComparer.Ordinal).ToList();
        Assert.True(held.Count == 1,
            $"{Post(planet, metres)}: the nearest slot changed hands {CountChanges(ids)} times while the " +
            "ship went nowhere — the flicker the owner reported. It held: " + string.Join(" → ", held));
    }

    /// <summary>
    /// #954 REGRESSION — THE LINE. The words of the "Nearest:" readout, which is the thing the owner
    /// actually screenshotted. RED with the filter out, and red again if the berth in the line is picked
    /// from where its rail has carried it this frame rather than from the rail itself.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThePosts))]
    public void THE_LINE_KeepsItsWordsWhileTheWholeFamilyGoesRound(string planet, double metres)
    {
        Pages.Map map = Booted();
        WatchTheFamilyTurn(map, planet, metres, out _, out List<string> readouts, out _);

        var said = readouts.Distinct(StringComparer.Ordinal).ToList();
        Assert.True(said.Count == 1,
            $"{Post(planet, metres)}: the Nearest line changed its words {CountChanges(readouts)} times " +
            "while the ship went nowhere: " + string.Join(" | ", said));
    }

    /// <summary>
    /// #954 REGRESSION — THE ANCHOR. The ⚓ hint ("dockable haven — coast within…") follows
    /// <c>_nearestHaven</c>, and it is the half of this readout that can go missing WITHOUT flickering,
    /// which is how it nearly slipped past: the first cut of the berth rule held the line perfectly steady
    /// and quietly turned the anchor off at five million km from Earth. The frame fingerprint caught it;
    /// this is the guard that would have. Two claims, and the second is the one that matters — the berth
    /// must be the SAME one all orbit, and it must be OFFERED at all wherever the neighbourhood has one.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThePosts))]
    public void THE_ANCHOR_TheNeighbourhoodGoesOnOfferingItsBerth(string planet, double metres)
    {
        Pages.Map map = Booted();
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        double window = WatchWindow(planet);

        var offered = new List<string>();
        for (int step = 0; step <= Samples; step++)
        {
            double t = window * step / Samples;
            Set(map, "SimTime", t);
            Vector2d here = eph.Position(planet, t);
            Set(map, "_ship", new ShipState(here + (here / here.Length) * metres, Vector2d.Zero, 0.0));
            Invoke(map, "UpdateNearestBody");
            offered.Add(Get<CelestialBody?>(map, "_nearestHaven")?.Id ?? "(none)");
        }

        var held = offered.Distinct(StringComparer.Ordinal).ToList();
        Assert.True(held.Count == 1,
            $"{Post(planet, metres)}: the ⚓ hint changed berths {CountChanges(offered)} times while the " +
            "ship went nowhere: " + string.Join(" → ", held));

        // …and it is not "steadily nothing" where the planet actually keeps a berth. Mercury's compute farm
        // is not a berth you clamp onto, so that post is asserted the other way round rather than skipped.
        bool planetHasABerth = eph.Bodies.Any(b => b.ParentId == planet && DockableHavens.IsDockable(b));
        Assert.True(planetHasABerth == (held[0] != "(none)"),
            planetHasABerth
                ? $"{Post(planet, metres)}: {planet} keeps a dockable berth, but the ⚓ hint offered none " +
                  "for the whole watch — the anchor went out where it should be lit."
                : $"{Post(planet, metres)}: {planet} keeps no dockable berth, yet the hint offered " +
                  $"\"{held[0]}\".");
    }

    private static int CountChanges(List<string> values)
    {
        int changes = 0;
        for (int i = 1; i < values.Count; i++)
        {
            if (!string.Equals(values[i], values[i - 1], StringComparison.Ordinal)) changes++;
        }

        return changes;
    }

    /// <summary>
    /// The law must not weld the slot shut — a "Nearest" that never moves is the same bug wearing the
    /// opposite face. Captured by a moon (inside its Hill sphere, which is the very line the market and
    /// the lying-low rule already draw) the slot IS that moon, not the planet it goes round.
    /// </summary>
    [Fact]
    public void ARRIVING_AMoonInsideWhoseHillSphereWeSitTakesTheSlot()
    {
        Pages.Map map = Booted();
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");

        CelestialBody enceladus = eph.Bodies.First(b => b.Id == "enceladus");
        CelestialBody saturn = eph.Bodies.First(b => b.Id == "saturn");
        double hill = OrbitRule.HillRadius(enceladus, saturn.Mu);
        Assert.True(hill > enceladus.BodyRadius,
            "Enceladus's Hill sphere does not clear its own surface — this bench cannot park inside it.");

        Set(map, "SimTime", 0.0);
        Vector2d moon = eph.Position("enceladus", 0.0);
        Vector2d outward = moon / moon.Length;
        Set(map, "_ship", new ShipState(moon + outward * (0.5 * (hill + enceladus.BodyRadius)), Vector2d.Zero, 0.0));
        Invoke(map, "UpdateNearestBody");

        Assert.Equal("enceladus", Get<CelestialBody?>(map, "_nearestBody")?.Id);
    }

    /// <summary>
    /// And a mass-less berth — which has no Hill sphere to be inside of, so it can never take the slot by
    /// drifting past — takes it the one way that is not arguable: the ship is clamped to it. Lying low to
    /// bleed off heat reads the slot (<c>IsHiddenAtHaven</c>), so this is not cosmetic.
    /// </summary>
    [Fact]
    public void CLAMPED_TheBerthWeAreDockedAtHoldsTheSlot()
    {
        Pages.Map map = Booted();
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");

        Set(map, "SimTime", 0.0);
        Set(map, "_dockedHavenId", "the-space-bar");
        Set(map, "_ship", new ShipState(eph.Position("the-space-bar", 0.0), Vector2d.Zero, 0.0));
        Invoke(map, "UpdateNearestBody");

        Assert.Equal("the-space-bar", Get<CelestialBody?>(map, "_nearestBody")?.Id);
    }
}
