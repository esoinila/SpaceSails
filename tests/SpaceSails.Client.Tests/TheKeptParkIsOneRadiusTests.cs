using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #251 / #1142 finding 2 · <b>THE KEPT PARK IS ONE RADIUS, NOT TWO.</b>
///
/// <para>#286's clamped park — the tide-stable radius bounded so the swept circle clears the moon's PARENT
/// planet — is read by two halves of the same tick. <c>CheckArmedInsertion</c> parks the ship at it and
/// sizes the trim cadence off it; <c>StationKeep</c> trims back to it. Until this lane each half built the
/// number from its OWN expression: the loop from a <c>keptRadiusCap</c> local, the keeper from a fresh
/// <c>OrbitRule.MaxKeptRadiusUnderParent(_ephemeris!.InstantaneousOrbitRadius(…), parent)</c>. They agreed.
/// Nothing made them agree — a change to one would have split the radius the autopilot ARRIVES at from the
/// radius the keeper HOLDS, silently, with both halves still internally consistent. That is the shape of
/// bug this repo has paid for more than once (a number spelled twice, in two places, from two sentences).</para>
///
/// <h3>What is asserted</h3>
/// <para>(a) <b>Both paths, one number.</b> The park is not a field, so it cannot be read directly — but both
/// halves publish it through <c>_keepNextCheckTime</c>, which each sets to
/// <c>SimTime + OrbitKeeping.TrimCadenceFraction · OrbitRule.LocalOrbitPeriod(park, body.Mu)</c>. The bench
/// drives the real page through the INSERT path (the park), reads the cadence it wrote, forces the keeper to
/// recompute AT THE SAME SIM-TIME, and reads it again. Same sim-time, same world, so if the two expressions
/// differ by so much as a factor the two cadences differ. Both are then asserted against the one radius Core
/// itself computes, so the test cannot pass by both halves being equally wrong.</para>
///
/// <para>(b) <b>Spelled once in the source.</b> The behavioural half above is flown against sol.json, where
/// #286's cap is inert (every shipped moon's tide-stable park is far tighter than the cap) — so a second
/// expression that simply DROPPED the cap would still agree there. The source half closes that: the client's
/// autopilot names <c>MaxKeptRadiusUnderParent</c> exactly once and <c>ParkingRadius</c> exactly once, inside
/// the two helpers both readers call.</para>
///
/// <h3>RED PROOF (watched)</h3>
/// <para>Re-introducing the pre-lane second expression in <c>StationKeep</c>, differing —
/// <c>Math.Min(OrbitRule.ParkingRadius(body, hill) * 0.9, …)</c> — turns (a) red on the two cadences
/// (64,898.75 s from the insertion vs 55,411.53 s from the keeper, at Luna) and (b) red on both counts:
/// <c>MaxKeptRadiusUnderParent</c> and <c>ParkingRadius</c> are each named twice again.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheKeptParkIsOneRadiusTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>A roomy moon with a real parent, so #286's cap and the tide-stable park are both defined.</summary>
    private const string Moon = "luna";

    // ── (a) BOTH PATHS, ONE NUMBER ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PARK THE AUTOPILOT ARRIVES AT IS THE PARK THE KEEPER HOLDS. Driven on the real page, through
    /// the real <c>CheckArmedInsertion</c>, twice: once into the insertion, once into station-keeping — at
    /// one and the same sim-time, so the only thing that could make the two cadences differ is the arithmetic.
    /// </summary>
    [Fact]
    public void TheInsertionAndTheKeeperSizeTheirCadenceOffOneAndTheSameParkRadius()
    {
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        CelestialBody moon = eph.Bodies.First(b => b.Id == Moon);
        CelestialBody parent = eph.Bodies.First(b => b.Id == moon.ParentId);

        double hill = OrbitRule.HillRadius(moon, parent.Mu);
        double cap = OrbitRule.MaxKeptRadiusUnderParent(eph.InstantaneousOrbitRadius(moon.Id, 0), parent);
        double park = Math.Min(OrbitRule.ParkingRadius(moon, hill), cap);
        double cadence = OrbitKeeping.TrimCadenceFraction * OrbitRule.LocalOrbitPeriod(park, moon.Mu);

        Pages.Map map = AShipDeepInsideTheParkOf(eph, moon, park);

        // (1) THE INSERTION. The armed loop fires the park burn and sizes the first cadence off its radius.
        Invoke(map, "CheckArmedInsertion");
        Assert.True(Get<bool>(map, "_orbitKept"),
            "the bench never reached the insertion — it proves nothing about the park");
        double fromTheInsertion = Get<double>(map, "_keepNextCheckTime") - Get<double>(map, "SimTime");

        // (2) THE KEEPER, at the SAME sim-time: due now, so it recomputes the park and rewrites the cadence.
        Set(map, "_keepNextCheckTime", Get<double>(map, "SimTime"));
        Invoke(map, "CheckArmedInsertion");
        Assert.True(Get<bool>(map, "_orbitKept"),
            "keeping ended before it recomputed the park — this bench has drifted");
        double fromTheKeeper = Get<double>(map, "_keepNextCheckTime") - Get<double>(map, "SimTime");

        // The two halves wrote the same number…
        Assert.Equal(fromTheInsertion, fromTheKeeper);
        // …and it is the one radius Core computes, so they are not equally wrong.
        Assert.Equal(cadence, fromTheInsertion, 6);
        Assert.Equal(cadence, fromTheKeeper, 6);
    }

    // ── (b) SPELLED ONCE IN THE SOURCE ────────────────────────────────────────────────────────────────

    /// <summary>
    /// #286'S CAP IS NAMED ONCE ON THE PAGE. sol.json's cap is inert, so a second expression that dropped it
    /// would fly identically there; this is the half that catches that. Both readers ask the same two
    /// helpers, so they agree by construction rather than by coincidence.
    /// </summary>
    [Fact]
    public void TheClientsAutopilotSpellsTheClampedParkAtExactlyOneSite()
    {
        string source = File.ReadAllText(
            Path.Combine(RepoDir("src", "SpaceSails.Client"), "Pages", "Map.Autopilot.cs"));

        Assert.Equal(1, Count(source, "OrbitRule.MaxKeptRadiusUnderParent("));
        Assert.Equal(1, Count(source, "OrbitRule.ParkingRadius("));

        // …and the one site of each is the shared helper, not a call site that happens to be alone today.
        Assert.Contains(
            "private double KeptRadiusCap(CelestialBody body, CelestialBody parent) =>",
            source, StringComparison.Ordinal);
        Assert.Contains(
            "private static double KeptParkRadius(CelestialBody body, double hill, double keptRadiusCap) =>",
            source, StringComparison.Ordinal);
        Assert.Equal(2, Count(source, "KeptRadiusCap(body, parent)"));   // the two readers, and nothing else
        Assert.Equal(2, Count(source, "KeptParkRadius(body, hill, "));
    }

    private static int Count(string haystack, string needle) =>
        Regex.Matches(haystack, Regex.Escape(needle)).Count;

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The real page, armed on <paramref name="moon"/>, with the ship already deep enough inside the
    /// clamped park for <c>OrbitRule.AutopilotDecision</c> to answer Insert on the very first tick: a
    /// near-circular state at 0.8·park, which is inside the gate, above the surface floor and far under the
    /// relative-speed limit.</summary>
    private static Pages.Map AShipDeepInsideTheParkOf(ICelestialEphemeris eph, CelestialBody moon, double park)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_scenarioName", TestTree.Sol.Name);
        Set(map, "_ephemeris", eph);
        Set(map, "_simulator", new Simulator(eph, timeStepSeconds: 1.0));

        const double h = 1.0;
        Vector2d at = eph.Position(moon.Id, 0);
        Vector2d moonVel = (eph.Position(moon.Id, h) - eph.Position(moon.Id, -h)) / (2 * h);

        double radius = 0.8 * park;
        Vector2d offset = new(radius, 0);
        Vector2d circular = new(0, Math.Sqrt(moon.Mu / radius));
        Set(map, "_ship", new ShipState(at + offset, moonVel + circular, 0));
        Set(map, "_armedOrbitBodyId", moon.Id);

        return map;
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

    private static string RepoDir(params string[] parts)
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine([dir, .. parts]);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException(
            string.Join('/', parts) + " not found above " + AppContext.BaseDirectory);
    }
}
