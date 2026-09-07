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
/// the two helpers every reader calls — three of them since #1177 added the panel that COACHES the
/// insertion to the loop that flies it and the keeper that holds it.</para>
///
/// <h3>RED PROOF (watched)</h3>
/// <para>Re-introducing the pre-lane second expression in <c>StationKeep</c>, differing —
/// <c>Math.Min(OrbitRule.ParkingRadius(body, hill) * 0.9, …)</c> — turns (a) red on the two cadences
/// (64,898.75 s from the insertion vs 55,411.53 s from the keeper, at Luna) and (b) red on both counts:
/// <c>MaxKeptRadiusUnderParent</c> and <c>ParkingRadius</c> are each named twice again.</para>
///
/// <para>#1177 · <b>and it was red the day it was widened.</b> (b) read six of the eleven
/// <c>Map.Autopilot*</c> partials — <c>Map.Autopilot.cs</c> and the five #1175 cut out of it — so the
/// second site, in a partial #870 had cut off years earlier, was never in the subject. Pointed at the whole
/// family it failed at once, on today's tree, naming the line:</para>
/// <code>
/// #286 · `OrbitRule.ParkingRadius(` is named at 2 site(s) in the Map.Autopilot* family:
///     Math.Min(OrbitRule.ParkingRadius(body, hill), keptRadiusCap);
///     return $"autopilot flying the approach — insertion at ≈{FormatAltitude(
///         OrbitRule.ParkingRadius(oi.Body, oi.Hill) - oi.Body.BodyRadius)}";
/// </code>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheKeptParkIsOneRadiusTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>A roomy moon with a real parent, so #286's cap and the tide-stable park are both defined.</summary>
    private const string Moon = "luna";

    /// <summary>#1177 · the pathological inner moon — no shipped body has a biting clamp (see the test).</summary>
    private const string Skimmer = "skimmer";

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

    // ── (a2) #1177 · AND IT IS THE PARK THE PANEL QUOTES ──────────────────────────────────────────────

    /// <summary>
    /// THE COACHING LINE QUOTES THE PARK THE PILOT FLIES — <b>in a world where the clamp bites.</b>
    ///
    /// <para>This is the assertion (a) could not make and (b) could only make about text. The orbit panel's
    /// approach line says <i>"autopilot flying the approach — insertion at ≈alt N km"</i>, and until #1177 it
    /// built that N from <c>OrbitRule.ParkingRadius</c> — the UNCLAMPED tide-stable radius — while three
    /// metres of code away the loop parked at <c>min(that, #286's cap)</c>. Sentence versus sim.</para>
    ///
    /// <para><b>Why this world and not sol.json.</b> The lane was asked to fly this at a shipped body where
    /// the clamp bites. There is none, and that was measured rather than assumed: across sol.json, sol-eu.json
    /// and oops.json the cap is wider than the tide-stable park at EVERY body with mass — Luna by 18×, Titan
    /// by 67×, the tightest real case (Miranda) by 363×. The only two bodies where the cap bites at all are
    /// μ=0 stations (Highport Satellite Works, Mercury Compute Farms), which the autopilot never parks at —
    /// <i>"Insert can never fire on a μ=0 body."</i> So a guard flown at a shipped body cannot fail, which is
    /// this repo's fifth bug class: a green test that asserts nothing. It is flown instead at the pathological
    /// inner moon <c>MoonOrbitClearanceTests</c> already ships for exactly this purpose — a moon skimming its
    /// parent's cloud tops, where #286's clamp is the whole point — scaled so the park speeds stay under the
    /// window's 5 km/s limit and the insertion really fires.</para>
    ///
    /// <para><b>Nothing here is recomputed from the same expression twice.</b> The flown park is read back out
    /// of the SIM: the insertion writes <c>_keepNextCheckTime</c> as a quarter of the local period AT the
    /// radius it parked at, so inverting that period is a measurement of what the autopilot did, not a second
    /// spelling of what it should have done. The quoted park is read out of the real page's own
    /// <c>OrbitStatusLine</c> string. The two are then required to be the same number, and the unclamped
    /// number is required to be ABSENT — which is what makes this fail rather than merely look right.</para>
    ///
    /// <para><b>RED PROOF (watched):</b> restoring the old line —
    /// <c>FormatAltitude(OrbitRule.ParkingRadius(oi.Body, oi.Hill) - oi.Body.BodyRadius)</c> — fails here
    /// with both numbers on the page:</para>
    /// <code>
    /// #1177 · the panel must quote the park the autopilot PARKS AT (alt 1000 km, measured off the trim
    /// cadence the insertion wrote), never the unclamped tide-stable radius (alt 2600 km) that #286's cap
    /// cuts off. It said: "autopilot flying the approach — insertion at ≈alt 2600 km"
    /// </code>
    /// </summary>
    [Fact]
    public void ThePanelQuotesTheParkTheAutopilotActuallyParksAt_WhereTheClampBites()
    {
        CircularOrbitEphemeris eph = AMoonThatSkimsItsParent();
        CelestialBody moon = eph.Bodies.First(b => b.Id == Skimmer);
        CelestialBody parent = eph.Bodies.First(b => b.Id == moon.ParentId);

        double hill = OrbitRule.HillRadius(moon, parent.Mu);
        double unclamped = OrbitRule.ParkingRadius(moon, hill);
        double cap = OrbitRule.MaxKeptRadiusUnderParent(eph.InstantaneousOrbitRadius(moon.Id, 0), parent);

        // The world has to be one where the two answers DIFFER, or nothing below can go red.
        Assert.True(cap < unclamped,
            $"this bench proves nothing: #286's cap ({cap:e3} m) does not bite the tide-stable park " +
            $"({unclamped:e3} m), so the clamped and unclamped quotes would be the same number");
        Assert.True(cap > OrbitRule.SurfaceParkRadii * moon.BodyRadius,
            "the clamped park must still be a flyable orbit above the surface floor, or the autopilot refuses");

        // (1) WHAT THE PILOT FLIES — measured off the sim. The insertion parks the ship and sizes the first
        // trim cadence at a quarter of the local period AT THAT RADIUS; invert the period and the radius the
        // autopilot chose comes back out, without spelling its expression a second time.
        Pages.Map flying = AShipDeepInsideTheParkOf(eph, moon, Math.Min(unclamped, cap));
        Invoke(flying, "CheckArmedInsertion");
        Assert.True(Get<bool>(flying, "_orbitKept"),
            "the bench never reached the insertion — it proves nothing about the park");
        double cadence = Get<double>(flying, "_keepNextCheckTime") - Get<double>(flying, "SimTime");
        double flown = RadiusOfALocalPeriod(cadence / OrbitKeeping.TrimCadenceFraction, moon.Mu);

        // (2) WHAT THE PANEL SAYS — the real page's own sentence, for a ship on the approach: armed, inside
        // capture range, outside the Hill sphere (so the window is shut and the coaching branch is the one
        // that speaks).
        Pages.Map coaching = AShipOnTheApproachTo(eph, moon, 2 * hill);
        object oi = Invoke(coaching, "OrbitInfo")
            ?? throw new InvalidOperationException("the orbit panel has no readout for this ship — bench drift");
        string line = (string)Invoke(coaching, "OrbitStatusLine", oi)!;
        Assert.StartsWith("autopilot flying the approach", line, StringComparison.Ordinal);

        // (3) ONE NUMBER. The sentence quotes an altitude, so both radii are put through the page's own
        // formatter — the comparison is on the string the captain reads, not on a double she never sees.
        string flownAltitude = Altitude(coaching, flown - moon.BodyRadius);
        string unclampedAltitude = Altitude(coaching, unclamped - moon.BodyRadius);
        Assert.NotEqual(flownAltitude, unclampedAltitude); // …and they really do render differently

        Assert.True(
            line.Contains(flownAltitude, StringComparison.Ordinal)
                && !line.Contains(unclampedAltitude, StringComparison.Ordinal),
            $"#1177 · the panel must quote the park the autopilot PARKS AT ({flownAltitude}, measured off " +
            $"the trim cadence the insertion wrote), never the unclamped tide-stable radius " +
            $"({unclampedAltitude}) that #286's cap cuts off. It said: \"{line}\"");
    }

    // ── (b) SPELLED ONCE IN THE SOURCE ────────────────────────────────────────────────────────────────

    /// <summary>
    /// #286'S CAP IS NAMED ONCE ON THE PAGE. sol.json's cap is inert, so a second expression that dropped it
    /// would fly identically there; this is the half that catches that. Both readers ask the same two
    /// helpers, so they agree by construction rather than by coincidence.
    ///
    /// <para>#1177 · <b>THE SUBJECT IS THE WHOLE <c>Map.Autopilot*</c> FAMILY, NOT SIX ELEVENTHS OF IT.</b>
    /// This claim was written against <c>Map.Autopilot.cs</c> alone and re-pathed by #1175 onto the six
    /// files cut out of it — and the five partials #870 had cut off years earlier were never in either
    /// subject. One of them, <c>Map.Autopilot.OrbitAssist.cs</c>, held the second site the whole time: the
    /// approach coaching line quoted an UNCLAMPED <c>OrbitRule.ParkingRadius(</c> while the pilot flew the
    /// clamped park. So this half stops reading a hand-declared list and reads the family by glob — a
    /// partial that joins the autopilot tomorrow is in the subject the day it lands, rather than the day
    /// somebody remembers to add it. Ordinal order is fine here where it is not for the debit ledger: this
    /// claim is a COUNT and a PRESENCE, never a sequence.</para>
    /// </summary>
    [Fact]
    public void TheClientsAutopilotSpellsTheClampedParkAtExactlyOneSite()
    {
        // #1177 · every Map.Autopilot* partial, so "named at exactly one site" is a claim about the whole
        // autopilot and not about the subset of it a previous lane happened to list.
        string source = MapMarkup.PagesFamily("Map.Autopilot*.cs");

        NamedOnce(source, "OrbitRule.MaxKeptRadiusUnderParent(");
        NamedOnce(source, "OrbitRule.ParkingRadius(");

        // …and the one site of each is the shared helper, not a call site that happens to be alone today.
        Assert.Contains(
            "private double KeptRadiusCap(CelestialBody body, CelestialBody parent) =>",
            source, StringComparison.Ordinal);
        Assert.Contains(
            "private static double KeptParkRadius(CelestialBody body, double hill, double keptRadiusCap) =>",
            source, StringComparison.Ordinal);
        // The three readers, and nothing else: the insertion loop, the keeper, and the panel that
        // COACHES the insertion — the third joined them in #1177.
        Assert.Equal(3, Count(source, "KeptRadiusCap(body, parent)"));
        Assert.Equal(3, Count(source, "KeptParkRadius(body, hill, "));
    }

    private static int Count(string haystack, string needle) =>
        Regex.Matches(haystack, Regex.Escape(needle)).Count;

    /// <summary>#1177 · the count, and — when it is wrong — the LINES, so the failure names the second
    /// site instead of printing "expected 1, actual 2" about a family of eleven files.</summary>
    private static void NamedOnce(string source, string needle)
    {
        string[] sites = source
            .Split('\n')
            .Where(line => line.Contains(needle, StringComparison.Ordinal))
            .Select(line => "    " + line.Trim())
            .ToArray();

        Assert.True(sites.Length == 1,
            $"#286 · `{needle}` is named at {sites.Length} site(s) in the Map.Autopilot* family; the clamped " +
            "park is ONE quantity and every reader must ask the shared helper for it, or the panel can quote " +
            "a radius the pilot does not fly (#1177):\n" + string.Join("\n", sites));
    }

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

    /// <summary>#1177 · THE #286 DANGER ZONE, MADE FLYABLE. A moon skimming its parent's clearance band —
    /// the shape <c>MoonOrbitClearanceTests.ClampScenario</c> already ships — with the masses picked so the
    /// clamped park is a real orbit the autopilot can reach: circular speed there is ≈1.6 km/s, well under
    /// the window's 5 km/s limit, and the clamped radius (1,500 km) sits comfortably above the moon's
    /// 550 km surface floor and well below the tide-stable park (≈3,100 km) the cap cuts off.</summary>
    private static CircularOrbitEphemeris AMoonThatSkimsItsParent()
    {
        var giant = new CelestialBody("giant", "Giant", null, 2e13, 2.0e7, 0, 0, 0, BodyKind.Planet);
        // d = 25,500 km: 1,500 km outside the giant's #278 clearance band plus #286's grace, so the cap
        // lands at 1,500 km — inside the moon's own tide-stable park, which is what makes the clamp bite.
        const double d = 2.55e7;
        double period = 2 * Math.PI * Math.Sqrt(d * d * d / giant.Mu);
        var skimmer = new CelestialBody(Skimmer, "Skimmer", "giant", 3e12, 5e5, d, period, 0, BodyKind.Moon);
        return new CircularOrbitEphemeris([giant, skimmer]);
    }

    /// <summary>The radius whose local circular period is <paramref name="period"/> — the inverse of
    /// <c>OrbitRule.LocalOrbitPeriod</c>, so the park the autopilot chose can be MEASURED off the cadence
    /// it wrote instead of recomputed from the expression under test.</summary>
    private static double RadiusOfALocalPeriod(double period, double mu) =>
        Math.Cbrt(mu * (period / (2 * Math.PI)) * (period / (2 * Math.PI)));

    /// <summary>The page's own altitude formatting — the string the captain actually reads.</summary>
    private static string Altitude(Pages.Map map, double metresAboveSurface) =>
        (string)Invoke(map, "FormatAltitude", metresAboveSurface)!;

    /// <summary>#1177 · a ship the coaching line speaks to: armed on <paramref name="moon"/> and inside its
    /// capture range, but OUTSIDE the Hill sphere, so the window is shut and the panel falls through to the
    /// "autopilot flying the approach — insertion at ≈…" branch rather than to "window OPEN" or
    /// "bound — parked at".</summary>
    private static Pages.Map AShipOnTheApproachTo(ICelestialEphemeris eph, CelestialBody moon, double outTo)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_ephemeris", eph);
        Vector2d at = eph.Position(moon.Id, 0);
        Set(map, "_ship", new ShipState(at + new Vector2d(outTo, 0), new Vector2d(0, 0), 0));
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
