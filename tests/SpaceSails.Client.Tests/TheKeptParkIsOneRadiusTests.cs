using System;
using System.Collections.Generic;
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
/// expression that simply DROPPED the cap would still agree there. The source half closes that: THE CLIENT
/// names <c>MaxKeptRadiusUnderParent</c> exactly once and <c>ParkingRadius</c> exactly once, inside the two
/// helpers every reader calls — the insertion loop, the keeper, the panel that COACHES the insertion
/// (#1177), and since #1179 the four readers outside the autopilot's own family, which ask through a
/// parentless overload rather than each writing the parent lookup out again.</para>
///
/// <para>(a2, a3, a4) <b>And every sentence built on it means what it says.</b> Five driven guards fly the
/// real page at a moon where #286's cap BITES — the panel's coaching line (#1177), and since #1179 the
/// plan banner's orbit-insert row, the AUTOPILOT HOLDS THE ORBIT line, the emergency-descent hover and the
/// #136 warp tier. Each measures what the autopilot did off the trim cadence the insertion wrote, never off
/// a second spelling of the expression under test, and each asserts its world DISCRIMINATES before asking
/// anything of it.</para>
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
///
/// <para>#1179 · <b>and red AGAIN the day the subject grew past the family.</b> Widening to the glob found
/// the second site and then stopped at the family's edge — four more readers of the same quantity sat one
/// directory listing away, in files that are not partials of the autopilot at all. Pointed at the whole
/// client it failed on today's tree, naming every one with its file and line:</para>
/// <code>
/// #286 · `OrbitRule.ParkingRadius(` is named at 5 site(s) in src/SpaceSails.Client:
///     Pages\Map.Autopilot.Arm.cs:260  Math.Min(OrbitRule.ParkingRadius(body, hill), keptRadiusCap);
///     Pages\Map.NavToolbar.cs:77  …stable park at {FormatDistance(OrbitRule.ParkingRadius(oi.Body, oi.Hill))}…
///     Pages\Map.Plot.FlightPlan.cs:308  ? OrbitRule.ParkingRadius(keptBody, _orbitedBodyHillRadius) - keptBody.BodyRadius
///     Pages\Map.Plot.FlightPlan.cs:347  double parkAlt = OrbitRule.ParkingRadius(oi.Body, oi.Hill) - oi.Body.BodyRadius;
///     Pages\Map.Sim.Tick.Warp.cs:100  double park = OrbitRule.ParkingRadius(_nearestBody, hill);
/// </code>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheKeptParkIsOneRadiusTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;

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

    // ── (a3) #1179 · AND SO DO THE THREE SENTENCES OUTSIDE THE AUTOPILOT'S OWN FAMILY ──────────────────

    /// <summary>
    /// THE BANNER ROW AND THE COACHING LINE QUOTE ONE ALTITUDE — <b>where the clamp bites.</b>
    ///
    /// <para><c>OrbitStatusLine</c>'s comment has said since #203 that its number is <i>"the SAME number the
    /// banner's <c>orbit-insert (alt N km)</c> row shows"</i>. #1177 moved the panel onto the clamped park
    /// and left <c>InsertStepLabel</c> on the raw <c>OrbitRule.ParkingRadius</c>, which turned that sentence
    /// from a law into a coincidence — true at every shipped body, because #286's cap is inert on the whole
    /// shipped sky, and false at the first inner moon that clamps. #1178 could only soften the comment to
    /// say so. This is the guard that lets the equality be claimed again: it flies both sentences at the one
    /// world where they can disagree and requires them to quote one string.</para>
    ///
    /// <para>The park they are measured against is not a third spelling of the expression under test — it is
    /// <see cref="TheParkTheAutopilotFlies"/>, read back out of the trim cadence the insertion actually
    /// wrote.</para>
    ///
    /// <para><b>RED PROOF (watched):</b> restoring <c>InsertStepLabel</c>'s
    /// <c>OrbitRule.ParkingRadius(oi.Body, oi.Hill) - oi.Body.BodyRadius</c> fails here — the banner row and
    /// the coaching line quoting two altitudes for one arrival:</para>
    /// <code>
    /// #1179 · the plan banner's orbit-insert row must quote the park the autopilot PARKS AT (alt 1000 km,
    /// measured off the trim cadence the insertion wrote), never the unclamped tide-stable radius
    /// (alt 2600 km) that #286's cap cuts off. It said: "orbit-insert at Skimmer (alt 2600 km)"
    /// </code>
    /// </summary>
    [Fact]
    public void TheBannerRowAndTheCoachingLineQuoteOneAltitude_WhereTheClampBites()
    {
        CircularOrbitEphemeris eph = AMoonThatSkimsItsParent();
        CelestialBody moon = eph.Bodies.First(b => b.Id == Skimmer);
        double hill = HillOf(eph, moon);
        double unclamped = TheParkTheTideAloneWouldAllow(eph, moon);
        double flown = TheParkTheAutopilotFlies(eph, moon);

        Pages.Map page = AShipOnTheApproachTo(eph, moon, 2 * hill);
        object oi = Invoke(page, "OrbitInfo")
            ?? throw new InvalidOperationException("the orbit panel has no readout for this ship — bench drift");

        string banner = (string)Invoke(page, "InsertStepLabel")!;
        string coaching = (string)Invoke(page, "OrbitStatusLine", oi)!;

        string flownAltitude = Altitude(page, flown - moon.BodyRadius);
        string unclampedAltitude = Altitude(page, unclamped - moon.BodyRadius);
        Assert.NotEqual(flownAltitude, unclampedAltitude); // …and the two really do render differently

        Assert.True(
            banner.Contains(flownAltitude, StringComparison.Ordinal)
                && !banner.Contains(unclampedAltitude, StringComparison.Ordinal),
            $"#1179 · the plan banner's orbit-insert row must quote the park the autopilot PARKS AT " +
            $"({flownAltitude}, measured off the trim cadence the insertion wrote), never the unclamped " +
            $"tide-stable radius ({unclampedAltitude}) that #286's cap cuts off. It said: \"{banner}\"");

        Assert.True(
            coaching.Contains(flownAltitude, StringComparison.Ordinal),
            $"#1179 · the bench has drifted: the coaching line no longer quotes {flownAltitude} — \"{coaching}\"");

        // …and the claim OrbitStatusLine's own comment makes, stated as an equality rather than a hope.
        Assert.Equal(AltitudeQuotedIn(banner), AltitudeQuotedIn(coaching));
    }

    /// <summary>
    /// THE HOLDING LINE NAMES THE RADIUS THE KEEPER IS HOLDING — <b>where the clamp bites.</b>
    ///
    /// <para>"🛰 AUTOPILOT HOLDS THE ORBIT — <i>body</i>, alt N km" is, by its own #220/#203 comment, the
    /// STEADY park the keeper trims back to, put on the banner precisely because the live radius oscillates.
    /// Built off a raw <c>OrbitRule.ParkingRadius</c>, at a moon whose park #286's cap cuts off, it stated an
    /// altitude <c>StationKeep</c> was actively trimming AWAY from — the one number on that line that was
    /// supposed to be beyond argument.</para>
    ///
    /// <para><b>RED PROOF (watched):</b> restoring
    /// <c>OrbitRule.ParkingRadius(keptBody, _orbitedBodyHillRadius) - keptBody.BodyRadius</c> fails here —
    /// the NOW line naming an orbit the keeper is trimming away from:</para>
    /// <code>
    /// #1179 · the holding line must name the park the keeper TRIMS BACK TO (alt 1000 km, measured off the
    /// trim cadence the insertion wrote), never the unclamped tide-stable radius (alt 2600 km) it is
    /// trimming away from. It said: "AUTOPILOT HOLDS THE ORBIT - Skimmer, alt 2600 km, trim ~57 p/day"
    /// </code>
    /// </summary>
    [Fact]
    public void TheHoldingLineNamesTheParkTheKeeperTrimsBackTo_WhereTheClampBites()
    {
        CircularOrbitEphemeris eph = AMoonThatSkimsItsParent();
        CelestialBody moon = eph.Bodies.First(b => b.Id == Skimmer);
        double hill = HillOf(eph, moon);
        double unclamped = TheParkTheTideAloneWouldAllow(eph, moon);

        // Fly her in, so the line is spoken by a page that is really keeping an orbit.
        Pages.Map page = AShipDeepInsideTheParkOf(eph, moon, Math.Min(unclamped, CapOf(eph, moon)));
        Invoke(page, "CheckArmedInsertion");
        Assert.True(Get<bool>(page, "_orbitKept"),
            "the bench never reached the insertion — it proves nothing about the park");
        double flown = RadiusOfALocalPeriod(
            (Get<double>(page, "_keepNextCheckTime") - Get<double>(page, "SimTime"))
                / OrbitKeeping.TrimCadenceFraction, moon.Mu);

        // The kept body IS the bound body while keeping — what UpdateOrbitedBody would have cached.
        Set(page, "_orbitedBodyId", moon.Id);
        Set(page, "_orbitedBodyHillRadius", hill);

        var status = (FlightPlanStatus)Invoke(page, "FlightNowNext")!;
        Assert.StartsWith("🛰 AUTOPILOT HOLDS THE ORBIT", status.NowLine, StringComparison.Ordinal);

        string flownAltitude = Altitude(page, flown - moon.BodyRadius);
        string unclampedAltitude = Altitude(page, unclamped - moon.BodyRadius);
        Assert.NotEqual(flownAltitude, unclampedAltitude);

        Assert.True(
            status.NowLine.Contains(flownAltitude, StringComparison.Ordinal)
                && !status.NowLine.Contains(unclampedAltitude, StringComparison.Ordinal),
            $"#1179 · the holding line must name the park the keeper TRIMS BACK TO ({flownAltitude}, " +
            $"measured off the trim cadence the insertion wrote), never the unclamped tide-stable radius " +
            $"({unclampedAltitude}) it is trimming away from. It said: \"{status.NowLine}\"");
    }

    /// <summary>
    /// THE EMERGENCY-DESCENT PROMISE NAMES WHERE THE PRESS SENDS HER — <b>where the clamp bites.</b>
    ///
    /// <para><i>"the autopilot takes her down to the stable park at N"</i> is a promise about a berth, made
    /// on the hover of the button that hands her to the autopilot. The autopilot parks at
    /// <c>min(tide-stable, #286's cap)</c>; the promise quoted the tide-stable half alone. Unlike the two
    /// altitudes above, this one is a DISTANCE (<c>FormatDistance</c>, not <c>FormatAltitude</c>) — so it is
    /// compared as the page renders it, in the units the captain reads on the hover.</para>
    ///
    /// <para><b>RED PROOF (watched):</b> restoring
    /// <c>FormatDistance(OrbitRule.ParkingRadius(oi.Body, oi.Hill))</c> fails here, promising a berth the
    /// autopilot does not fly to:</para>
    /// <code>
    /// #1179 · the emergency-descent hover promises a berth, so it must name the one the autopilot flies to
    /// (1500 km, measured off the trim cadence the insertion wrote), never the unclamped tide-stable radius
    /// (3100 km) #286's cap cuts off. It said: "...takes her down to the stable park at 3100 km..."
    /// </code>
    /// </summary>
    [Fact]
    public void TheEmergencyDescentPromiseNamesTheParkTheAutopilotFliesTo_WhereTheClampBites()
    {
        CircularOrbitEphemeris eph = AMoonThatSkimsItsParent();
        CelestialBody moon = eph.Bodies.First(b => b.Id == Skimmer);
        double hill = HillOf(eph, moon);
        double unclamped = TheParkTheTideAloneWouldAllow(eph, moon);
        double flown = TheParkTheAutopilotFlies(eph, moon);

        Pages.Map page = AShipOnTheApproachTo(eph, moon, 2 * hill);
        object oi = Invoke(page, "OrbitInfo")
            ?? throw new InvalidOperationException("the orbit panel has no readout for this ship — bench drift");
        string tip = (string)Invoke(page, "EmergencyDescentTip", oi)!;

        string flownDistance = Distance(flown);
        string unclampedDistance = Distance(unclamped);
        Assert.NotEqual(flownDistance, unclampedDistance);

        Assert.True(
            tip.Contains(flownDistance, StringComparison.Ordinal)
                && !tip.Contains(unclampedDistance, StringComparison.Ordinal),
            $"#1179 · the emergency-descent hover promises a berth, so it must name the one the autopilot " +
            $"flies to ({flownDistance}, measured off the trim cadence the insertion wrote), never the " +
            $"unclamped tide-stable radius ({unclampedDistance}) #286's cap cuts off. It said: \"{tip}\"");
    }

    // ── (a4) #1179 · AND THE ONE READER THAT IS NOT A SENTENCE ─────────────────────────────────────────

    /// <summary>
    /// THE WARP TIER MEASURES THE BAND SHE IS CLOSING ON. #136 caps warp on final approach to a deep-well
    /// moon because its parking band is only tens of km wide — far thinner than the grazing tier's 10× step.
    /// The heuristic asks two questions about <c>park</c>, and BOTH are about the ship's nearness to the band
    /// she will fly to: <i>does that band sit inside the grazing radius</i> (3·R), and <i>how much room is
    /// left to it</i>. The armed loop flies to #286's clamped park, so the unclamped tide-stable radius was
    /// the wrong quantity for both — this is not a sentence, but it is the same wrong number.
    ///
    /// <para><b>The world has to be one where the two ANSWERS differ</b>, not merely the two radii — so this
    /// is flown at the same skimming moon pulled 1,000 km further in, where the clamped park (1,000 km) sits
    /// INSIDE the grazing radius (1,500 km) while the tide-stable park (≈3,040 km) sits well outside it. On
    /// the unclamped number the gate answered <i>"roomy moon — the tiers suffice"</i> and left warp at 10×
    /// while the ship was closing on a band a moon-radius wide; on the clamped one it caps.</para>
    ///
    /// <para><b>RED PROOF (watched):</b> restoring <c>OrbitRule.ParkingRadius(_nearestBody, hill)</c> fails
    /// here — the tier returns int.MaxValue, no cap at all:</para>
    /// <code>
    /// #1179 · the #136 warp tier must measure the band the autopilot AIMS AT — #286's clamped park,
    /// 1.000e+006 m, which is inside this moon's 1.500e+006 m grazing radius. Read off the unclamped
    /// tide-stable radius (3.039e+006 m) it answers "roomy moon — the tiers suffice" and leaves warp
    /// uncapped while the ship closes on a band a moon-radius wide.
    /// </code>
    /// </summary>
    [Fact]
    public void TheWarpTierCapsOnTheBandTheAutopilotAimsAt_WhereTheClampBites()
    {
        CircularOrbitEphemeris eph = AMoonThatSkimsItsParent(TightEnoughToPullTheBandInside);
        CelestialBody moon = eph.Bodies.First(b => b.Id == Skimmer);
        double hill = HillOf(eph, moon);
        double unclamped = TheParkTheTideAloneWouldAllow(eph, moon);
        double cap = CapOf(eph, moon);
        double grazing = 3 * moon.BodyRadius;

        // The world: the clamped band is inside the grazing radius and the unclamped one is outside it, so
        // the two expressions give the tier OPPOSITE answers. Without this the guard asserts nothing.
        Assert.True(cap < grazing, $"the clamped park ({cap:e3} m) must sit inside the grazing radius " +
            $"({grazing:e3} m), or the tier caps on neither number and this bench proves nothing");
        Assert.True(unclamped >= grazing, $"the tide-stable park ({unclamped:e3} m) must sit outside the " +
            $"grazing radius ({grazing:e3} m), or the tier caps on both numbers and this bench proves nothing");
        Assert.True(cap > OrbitRule.SurfaceParkRadii * moon.BodyRadius,
            "the clamped park must still be a flyable orbit above the surface floor, or the autopilot refuses");

        // A ship armed for the moon and closing on it, inside capture range — the state #136 is about.
        double distance = 4 * cap;
        Assert.True(distance <= OrbitRule.CaptureRange(hill), "the bench must be inside capture range");
        Pages.Map page = AShipClosingOn(eph, moon, distance);

        int warpCap = (int)Invoke(page, "DeepWellInsertionWarpCap", distance)!;
        Assert.True(warpCap < int.MaxValue,
            "#1179 · the #136 warp tier must measure the band the autopilot AIMS AT — #286's clamped park, " +
            $"{cap:e3} m, which is inside this moon's {grazing:e3} m grazing radius. Read off the unclamped " +
            $"tide-stable radius ({unclamped:e3} m) it answers \"roomy moon — the tiers suffice\" and leaves " +
            "warp uncapped while the ship closes on a band a moon-radius wide.");
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
    ///
    /// <para>#1179 · <b>AND THE FAMILY WAS STILL THE WRONG SUBJECT.</b> Widening to the glob found the second
    /// site and then stopped exactly at the family's edge — while FOUR more readers of the same quantity sat
    /// just outside it, in files that are not partials of the autopilot at all: the flight-plan board's two
    /// altitude quotes, the nav toolbar's emergency-descent promise, and the warp tier's how-far-to-the-band
    /// heuristic. The clamped park is not an autopilot-family idea; it is a CLIENT idea — the number every
    /// surface that speaks about where the ship is going, or steers by how near she is to it, has to be
    /// asking for. So the subject is <c>src/SpaceSails.Client</c>, read recursively: every <c>.cs</c> and
    /// <c>.razor</c> under it. (Raw file reads, not <c>MapMarkup.Read</c>'s composed ones: composition exists
    /// so a guard reading ONE path is not half-blind, and a sweep that already visits every file on disk is
    /// blind to nothing — while splicing surfaces into the page would count their text twice.)</para>
    /// </summary>
    [Fact]
    public void TheClientSpellsTheClampedParkAtExactlyOneSite()
    {
        // #1179 · the whole client, recursively. "Named at exactly one site" is a claim about every surface
        // that could quote the park or steer by it, not about the partials of the class that flies to it.
        NamedOnceInTheClient("OrbitRule.MaxKeptRadiusUnderParent(");
        NamedOnceInTheClient("OrbitRule.ParkingRadius(");

        // …and the one site of each is the shared helper, not a call site that happens to be alone today.
        string source = MapMarkup.PagesFamily("Map.Autopilot*.cs");
        Assert.Contains(
            "private double KeptRadiusCap(CelestialBody body, CelestialBody parent) =>",
            source, StringComparison.Ordinal);
        Assert.Contains(
            "private static double KeptParkRadius(CelestialBody body, double hill, double keptRadiusCap) =>",
            source, StringComparison.Ordinal);
        // The readers spelled `body, hill` — the insertion loop, the keeper, the panel that COACHES the
        // insertion (#1177) — plus, since #1179, the parentless overload through which the readers OUTSIDE
        // this family ask, so they get the clamp without a second spelling of it.
        Assert.Equal(4, Count(source, "KeptRadiusCap(body, parent)"));
        Assert.Equal(4, Count(source, "KeptParkRadius(body, hill, "));
    }

    private static int Count(string haystack, string needle) =>
        Regex.Matches(haystack, Regex.Escape(needle)).Count;

    /// <summary>#1179 · the subject of the one-site claim: every <c>.cs</c> and <c>.razor</c> under
    /// <c>src/SpaceSails.Client</c>, recursively, minus the build's own output.</summary>
    private static IEnumerable<string> ClientSources()
    {
        string root = Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client");
        char sep = Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{sep}obj{sep}", StringComparison.Ordinal)
                        && !path.Contains($"{sep}bin{sep}", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    /// <summary>#1177/#1179 · the count, and — when it is wrong — the FILE, LINE and TEXT of every site, so
    /// the failure names the offenders instead of printing "expected 1, actual 5" about a client of several
    /// hundred files.</summary>
    private static void NamedOnceInTheClient(string needle)
    {
        string root = Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client");
        var sites = new List<string>();
        foreach (string path in ClientSources())
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(needle, StringComparison.Ordinal))
                {
                    sites.Add($"    {Path.GetRelativePath(root, path)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Assert.True(sites.Count == 1,
            $"#286 · `{needle}` is named at {sites.Count} site(s) in src/SpaceSails.Client; the clamped park " +
            "is ONE quantity and every reader must ask the shared helper for it, or a surface can quote — or " +
            "steer by — a radius the pilot does not fly (#1177, #1179):\n" + string.Join("\n", sites));
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
    /// 550 km surface floor and well below the tide-stable park (≈3,100 km) the cap cuts off.
    ///
    /// <para>#1179 · <paramref name="orbitRadius"/> is the one dial, and its default leaves this world byte
    /// for byte what #1177 flew. Pulling the moon 1,000 km further in tightens the cap without changing the
    /// shape, which is what the warp-tier guard needs — see
    /// <see cref="TightEnoughToPullTheBandInside"/>.</para></summary>
    private static CircularOrbitEphemeris AMoonThatSkimsItsParent(double orbitRadius = 2.55e7)
    {
        var giant = new CelestialBody("giant", "Giant", null, 2e13, 2.0e7, 0, 0, 0, BodyKind.Planet);
        // d = 25,500 km: 1,500 km outside the giant's #278 clearance band plus #286's grace, so the cap
        // lands at 1,500 km — inside the moon's own tide-stable park, which is what makes the clamp bite.
        double period = 2 * Math.PI * Math.Sqrt(orbitRadius * orbitRadius * orbitRadius / giant.Mu);
        var skimmer = new CelestialBody(
            Skimmer, "Skimmer", "giant", 3e12, 5e5, orbitRadius, period, 0, BodyKind.Moon);
        return new CircularOrbitEphemeris([giant, skimmer]);
    }

    /// <summary>#1179 · the same moon 1,000 km further in, so the cap lands at 1,000 km — INSIDE the 1,500 km
    /// grazing radius the #136 warp tier gates on, while the tide-stable park (≈3,040 km) stays outside it.
    /// That is the only scaling at which the two expressions give the tier opposite answers.</summary>
    private const double TightEnoughToPullTheBandInside = 2.50e7;

    /// <summary>The moon's Hill radius in this world — read from the ephemeris, as the page reads it.</summary>
    private static double HillOf(ICelestialEphemeris eph, CelestialBody moon) =>
        OrbitRule.HillRadius(moon, eph.Bodies.First(b => b.Id == moon.ParentId).Mu);

    /// <summary>#286's cap in this world: the widest kept radius whose swept circle clears the parent.</summary>
    private static double CapOf(ICelestialEphemeris eph, CelestialBody moon) =>
        OrbitRule.MaxKeptRadiusUnderParent(
            eph.InstantaneousOrbitRadius(moon.Id, 0), eph.Bodies.First(b => b.Id == moon.ParentId));

    /// <summary>The UNCLAMPED tide-stable radius — the number every one of these sentences used to quote, and
    /// the one each of them must not.</summary>
    private static double TheParkTheTideAloneWouldAllow(ICelestialEphemeris eph, CelestialBody moon) =>
        OrbitRule.ParkingRadius(moon, HillOf(eph, moon));

    /// <summary>#1179 · WHAT THE PILOT FLIES, MEASURED OFF THE SIM — not a second spelling of the expression
    /// under test. The insertion sizes its first trim cadence at a quarter of the local period AT THE RADIUS
    /// IT PARKED AT, so inverting that period returns what the autopilot did.</summary>
    private static double TheParkTheAutopilotFlies(ICelestialEphemeris eph, CelestialBody moon)
    {
        double unclamped = TheParkTheTideAloneWouldAllow(eph, moon);
        double cap = CapOf(eph, moon);
        Assert.True(cap < unclamped,
            $"this bench proves nothing: #286's cap ({cap:e3} m) does not bite the tide-stable park " +
            $"({unclamped:e3} m), so the clamped and unclamped quotes would be the same number");

        Pages.Map flying = AShipDeepInsideTheParkOf(eph, moon, Math.Min(unclamped, cap));
        Invoke(flying, "CheckArmedInsertion");
        Assert.True(Get<bool>(flying, "_orbitKept"),
            "the bench never reached the insertion — it proves nothing about the park");
        double cadence = Get<double>(flying, "_keepNextCheckTime") - Get<double>(flying, "SimTime");
        return RadiusOfALocalPeriod(cadence / OrbitKeeping.TrimCadenceFraction, moon.Mu);
    }

    /// <summary>#1179 · a ship armed for <paramref name="moon"/> and closing on it from
    /// <paramref name="distance"/>, with the nearest-body cache the warp tier reads already filled — the
    /// final-approach state #136's cap is about.</summary>
    private static Pages.Map AShipClosingOn(ICelestialEphemeris eph, CelestialBody moon, double distance)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_ephemeris", eph);
        const double h = 1.0;
        Vector2d at = eph.Position(moon.Id, 0);
        Vector2d moonVel = (eph.Position(moon.Id, h) - eph.Position(moon.Id, -h)) / (2 * h);
        // Falling straight in at 1 km/s — closing, so the tier's "not yet closing" branch is not the one taken.
        Set(map, "_ship", new ShipState(at + new Vector2d(distance, 0), moonVel + new Vector2d(-1000, 0), 0));
        Set(map, "_armedOrbitBodyId", moon.Id);
        Set(map, "_nearestBody", moon);
        Set(map, "_nearestBodyPosition", at);
        Set(map, "_nearestBodyVelocity", moonVel);
        return map;
    }

    /// <summary>#1179 · the altitude a sentence quotes — the "alt N km" the page's own formatter wrote, cut
    /// back out of it, so the two sentences are compared on the string the captain reads.</summary>
    private static string AltitudeQuotedIn(string sentence)
    {
        Match m = Regex.Match(sentence, @"alt [^)\s][^)]*");
        Assert.True(m.Success, $"no altitude in \"{sentence}\" — this bench has drifted");
        return m.Value.TrimEnd();
    }

    /// <summary>The radius whose local circular period is <paramref name="period"/> — the inverse of
    /// <c>OrbitRule.LocalOrbitPeriod</c>, so the park the autopilot chose can be MEASURED off the cadence
    /// it wrote instead of recomputed from the expression under test.</summary>
    private static double RadiusOfALocalPeriod(double period, double mu) =>
        Math.Cbrt(mu * (period / (2 * Math.PI)) * (period / (2 * Math.PI)));

    /// <summary>The page's own altitude formatting — the string the captain actually reads.</summary>
    private static string Altitude(Pages.Map map, double metresAboveSurface) =>
        (string)Invoke(map, "FormatAltitude", metresAboveSurface)!;

    /// <summary>#1179 · the page's own DISTANCE formatting — the emergency-descent hover quotes a radius from
    /// the body's centre, not an altitude, so it is compared in the units it is written in.</summary>
    private static string Distance(double metres) =>
        (string)typeof(Pages.Map).GetMethod("FormatDistance", Hidden)!.Invoke(null, [metres])!;

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
