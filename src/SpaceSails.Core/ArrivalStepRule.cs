using System;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #952/#955/#957 — <b>THE ARRIVE STEP: the cherry on top of the flight plan.</b> The owner, plotting a
/// multi-burn run to Mars and iterating the ±p/±d/±h buttons until the pass looked right:
/// <i>"the cherry on top of the cake is missing when I cannot add the step to the end of the plan to orbit
/// Mars here. It feels incomplete. … Let's find an elegant step that makes the orbit step visually
/// identical to the other scrubs. It should have some bit valid / invalid step, so that if we ruin it mid
/// flight it would say so in the list and as a pop-up that we no longer have safely ending flight plan."</i>
///
/// <para>This is PR-D1 of <c>docs/WednesdayPlan/UnifiedNavListNotes.md</c> finally landing whole: the plan
/// is one list, dock-to-dock, and its LAST step is the arrival — an orbit insertion or a ⚓ dock. The row
/// carries a valid/invalid bit, and this class owns the law behind that bit and the sentence that explains
/// it.</para>
///
/// <para><b>Every number here is borrowed, never invented.</b> This repo has a named bug class — <i>the sim
/// does one thing while a sentence reports another</i> — so the thresholds are read straight off the rules
/// the flight actually obeys:
/// <list type="bullet">
///   <item><b>Orbit</b> — distance against <see cref="OrbitRule.CaptureRange"/> (the range from which the
///   armed autopilot can take the ship: the same test <c>PassIsOrbitable</c> applies to offer the arm at
///   all), relative speed against <see cref="OrbitRule.MaxRelativeSpeed"/> (the window
///   <see cref="OrbitRule.WindowOpen"/> opens under).</item>
///   <item><b>Dock</b> — distance against <see cref="DockRule.EnvelopeMeters"/> and relative speed against
///   <see cref="DockRule.MatchSpeed"/>: verbatim the two numbers <see cref="DockRule.InEnvelope"/> judges,
///   which is what the μ=0 station branch of <see cref="AutopilotRehearsal.Rehearse"/> calls "captured".</item>
/// </list>
/// The comparison SENSES are borrowed with the same care — <see cref="OrbitRule.WindowOpen"/> is strict on
/// speed (<c>relSpeed &lt; MaxRelativeSpeed</c>) while <see cref="DockRule.InEnvelope"/> is inclusive
/// (<c>relSpeed &lt;= MatchSpeed</c>) — so a badge can never be green on a pass the sim would refuse.</para>
///
/// <para>Pure: geometry and words only, no ship and no ephemeris, so the whole law unit-tests without a
/// browser (the <see cref="HarborVocabulary"/> / <see cref="ArrivalBrake"/> house style).</para>
/// </summary>
public static class ArrivalStepRule
{
    /// <summary>What the plan's terminal step arrives INTO. A body with mass is orbited; a μ=0 dock haven
    /// is clamped onto — the same fork <see cref="AutopilotRehearsal.Rehearse"/> takes on <c>isStation</c>.</summary>
    public enum ArrivalKind
    {
        /// <summary>Insert into a kept orbit (<see cref="OrbitRule"/>).</summary>
        Orbit,

        /// <summary>Coast into the dock envelope and clamp on (<see cref="DockRule"/>).</summary>
        Dock,
    }

    /// <summary>
    /// One arrival, judged. The caller measures the pass off the plotted path (distance and relative speed
    /// at the closest approach) and hands the geometry over; this record answers whether that pass can
    /// become an arrival, and says so in numbers.
    /// </summary>
    /// <param name="Kind">Orbit insertion or ⚓ dock.</param>
    /// <param name="BodyName">The body the plan ends at — named in every sentence, so a row can never be
    /// read against the wrong world (#950: the armed step quoted the body the ship was PARKED at).</param>
    /// <param name="Distance">Closest-approach distance on the plotted path (m).</param>
    /// <param name="DistanceLimit">The real gate: capture range, or the dock envelope (m).</param>
    /// <param name="RelSpeed">Ship speed relative to the body at that pass (m/s).</param>
    /// <param name="SpeedLimit">The real gate: the insertion window's speed cap, or the clamp's match speed (m/s).</param>
    public readonly record struct ArrivalCheck(
        ArrivalKind Kind,
        string BodyName,
        double Distance,
        double DistanceLimit,
        double RelSpeed,
        double SpeedLimit)
    {
        /// <summary>The pass comes close enough for the arrival's own rule.</summary>
        public bool DistanceOk => DistanceWithin(Kind, Distance, DistanceLimit);

        /// <summary>The pass is slow enough for the arrival's own rule.</summary>
        public bool SpeedOk => SpeedWithin(Kind, RelSpeed, SpeedLimit);

        /// <summary>The plan ends safely: both gates clear. This is the row's ✓/✗ bit.</summary>
        public bool Valid => DistanceOk && SpeedOk;

        /// <summary>How much too far the pass is (m); 0 when the distance gate clears.</summary>
        public double DistanceShortfall => DistanceOk ? 0 : Distance - DistanceLimit;

        /// <summary>How much too fast the pass is (m/s); 0 when the speed gate clears.</summary>
        public double SpeedShortfall => SpeedOk ? 0 : RelSpeed - SpeedLimit;
    }

    /// <summary>The distance gate for an arrival of this kind. Orbit: the range the armed autopilot can
    /// take over from (<see cref="OrbitRule.CaptureRange"/> of the body's Hill radius). Dock: the clamp's
    /// reach (<see cref="DockRule.EnvelopeMeters"/>), which ignores the Hill radius because a μ=0 station
    /// has none.</summary>
    public static double DistanceLimit(ArrivalKind kind, double hillRadius) =>
        kind == ArrivalKind.Dock ? DockRule.EnvelopeMeters : OrbitRule.CaptureRange(hillRadius);

    /// <summary>The relative-speed gate for an arrival of this kind — the insertion window's cap, or the
    /// clamp's match speed. Both are the constants the flight itself is judged on.</summary>
    public static double SpeedLimit(ArrivalKind kind) =>
        kind == ArrivalKind.Dock ? DockRule.MatchSpeed : OrbitRule.MaxRelativeSpeed;

    /// <summary>Distance test, in the sense the sim uses. Inclusive for both: the client's
    /// <c>PassIsOrbitable</c> rejects on <c>distance &gt; captureRange</c>, and
    /// <see cref="DockRule.InEnvelope"/> accepts on <c>distance &lt;= EnvelopeMeters</c>.</summary>
    public static bool DistanceWithin(ArrivalKind kind, double distance, double limit) =>
        distance <= limit;

    /// <summary>Speed test, in the sense the sim uses — and the two senses genuinely differ.
    /// <see cref="OrbitRule.WindowOpen"/> is STRICT (<c>relSpeed &lt; MaxRelativeSpeed</c>);
    /// <see cref="DockRule.InEnvelope"/> is INCLUSIVE (<c>relSpeed &lt;= MatchSpeed</c>). Borrowing the
    /// sense — not just the number — is what keeps a green badge from standing over a pass the flight
    /// would refuse at the boundary.</summary>
    public static bool SpeedWithin(ArrivalKind kind, double relSpeed, double limit) =>
        kind == ArrivalKind.Dock ? relSpeed <= limit : relSpeed < limit;

    /// <summary>Judge a pass as an arrival. <paramref name="hillRadius"/> is ignored for a dock.</summary>
    public static ArrivalCheck Check(
        ArrivalKind kind, string bodyName, double distance, double relSpeed, double hillRadius) =>
        new(kind, bodyName, distance, DistanceLimit(kind, hillRadius), relSpeed, SpeedLimit(kind));

    // ===== The one voice for the arrive step (pure text, unit-tested) =====

    /// <summary>The row's badge glyph — the owner's "valid / invalid bit", one character wide.</summary>
    public static string Badge(ArrivalCheck c) => c.Valid ? "✓" : "✗";

    /// <summary>The verb the step is named by: "orbit" or "dock".</summary>
    public static string Verb(ArrivalKind kind) => kind == ArrivalKind.Dock ? "dock" : "orbit";

    /// <summary>
    /// <b>The sentence the row and the refusal both speak.</b> Owner (#957): <i>"Also it gives totally
    /// useless error. Why does it not tell what it is complaining about … Where is the problem, so it can
    /// be fixed. If it refuses it should show where the problem is visually and with numbers."</i>
    ///
    /// <para>Valid: <c>✓ orbit at Mars — pass 64951 km (≤ 3.00 M km), rel 3.1 km/s (≤ 5.0 km/s)</c>.
    /// Invalid: <c>✗ no dock at The Rusty Roadstead — pass 3.60 M km, need ≤ 0.50 M km; rel 12.4 km/s,
    /// need ≤ 8.0 km/s (3.10 M km too far, 4.4 km/s too fast)</c> — both gates always quoted, with the
    /// shortfall spelled out so the captain knows which way to iterate the plan.</para>
    /// </summary>
    public static string Verdict(ArrivalCheck c)
    {
        string dist = FormatDistance(c.Distance);
        string distLimit = FormatDistance(c.DistanceLimit);
        string rel = FormatSpeed(c.RelSpeed);
        string relLimit = FormatSpeed(c.SpeedLimit);

        if (c.Valid)
        {
            return $"✓ {Verb(c.Kind)} at {c.BodyName} — pass {dist} (≤ {distLimit}), rel {rel} (≤ {relLimit})";
        }

        return $"✗ no {Verb(c.Kind)} at {c.BodyName} — pass {dist}, need ≤ {distLimit}; "
             + $"rel {rel}, need ≤ {relLimit} ({Shortfall(c)})";
    }

    /// <summary>How far the pass is from being an arrival, in the two currencies that can fail. Never
    /// empty: a check that is Valid reports "within both gates", so the sentence always ends in a fact.</summary>
    public static string Shortfall(ArrivalCheck c)
    {
        if (c.Valid)
        {
            return "within both gates";
        }
        if (!c.DistanceOk && !c.SpeedOk)
        {
            return $"{FormatDistance(c.DistanceShortfall)} too far, {FormatSpeed(c.SpeedShortfall)} too fast";
        }
        return !c.DistanceOk
            ? $"{FormatDistance(c.DistanceShortfall)} too far"
            : $"{FormatSpeed(c.SpeedShortfall)} too fast";
    }

    /// <summary>
    /// The WHY clause of the autopilot's refusal (#957) — what the client drops into its standing sentence,
    /// <c>"autopilot declines {name}: {why}. It won't strand you."</c> Deliberately only the clause: the
    /// refusal has ONE composition site (Map.Autopilot's arm) and the #928 guard reads that site's shape.
    /// What this adds is the numbers — where the ship is, how fast, and against which thresholds — in the
    /// very words the arrive step's ✗ row speaks, so the panel and the refusal cannot describe two ships.
    /// </summary>
    /// <param name="c">The arrival as it stands right now.</param>
    /// <param name="nearestWindowNote">Optional trailing clause naming the plotted course's own closest
    /// pass, so a refusal points at a moment the captain can scrub to rather than at a shrug.</param>
    public static string RefusalWhy(ArrivalCheck c, string? nearestWindowNote = null) =>
        Verdict(c) + (nearestWindowNote ?? string.Empty);

    /// <summary>
    /// <b>The mid-flight flip, as a one-shot.</b> Owner: <i>"if we ruin it mid flight it would say so in
    /// the list and as a pop-up that we no longer have safely ending flight plan but will need input at
    /// some point. That nobody is flying the ship moment is important — say our pirate captain is asleep,
    /// there needs to be a visual wake-up call about the ship going off the plan."</i>
    ///
    /// <para>So: the alarm fires on the TRANSITION from a plan that ended safely to one that does not —
    /// once, not every frame — and re-arms only by becoming valid again. A step that is invalid the
    /// moment it is added does not pop up (its row already says ✗, and the captain is looking right at
    /// it); this is the wake-up for a plan that was good and got ruined.</para>
    /// </summary>
    /// <param name="wasValid">The previous evaluation: true/false, or null when never evaluated.</param>
    /// <param name="nowValid">This evaluation.</param>
    public static bool ShouldWarn(bool? wasValid, bool nowValid) => wasValid == true && !nowValid;

    /// <summary>The pop-up's words when <see cref="ShouldWarn"/> says to wake the captain.</summary>
    public static string BrokenPlanAlarm(ArrivalCheck c) =>
        $"⚠ THE PLAN NO LONGER ENDS SAFELY — {Verdict(c)}. Nobody is flying the ship into a berth; she needs your hand.";

    // ===== #952 — THE ITERATE LOOP: A PASS THE RIBBON NEVER REACHED IS NOT A PASS =====
    //
    // The owner's own sentence on #952 is about a LOOP, not a button: "I wanted to iterate the path until I
    // could add orbit mars step to the end of my plan." The button landed in #965; the loop had a hole under
    // it. The plotted course has a LENGTH — the Path-length slider — and the closest-approach sweep reports a
    // closest approach for EVERY body in the system whether or not the ribbon goes anywhere near it. For a
    // body the projection stops short of, that "closest approach" is pinned to the ribbon's LAST SAMPLE: it
    // is the end of the picture, not an encounter.
    //
    // Judged against that artefact, an arrival reads as a confident ✗ WITH NUMBERS — "0.45 AU too far, 6.3
    // km/s too fast" — over a course that in truth arrives well inside both gates a couple of hundred days
    // later. That is this repo's own named bug class (the sim doing one thing while a sentence reports
    // another), and it is precisely fatal to the iterate loop: the shortfall points the wrong way, so no
    // amount of ±p / ±d / ±h will ever close a gap that was never there.
    //
    // So the law: an arrival whose pass sits on the ribbon's own edge is NOT JUDGED. The row says why, and
    // says which control fixes it. Silence with a reason beats a number that is not true.

    /// <summary>
    /// <b>Is this "closest pass" just the end of the drawn course?</b> True when the pass epoch sits within
    /// one sample step of the projection's last sample — the sweep had nowhere further to look, so the
    /// distance it returned is where the ribbon stopped, not where the ship comes nearest.
    ///
    /// <para>The tolerance is the projection's OWN local sample spacing at that end, handed in by the
    /// caller rather than picked here: a borrowed number, never an invented one. A genuine encounter that
    /// happens to fall in the final sample is treated as unjudged too — that is the safe direction, and one
    /// press of <c>auto</c> (or a nudge of Path length) moves it inside the line.</para>
    /// </summary>
    /// <param name="passSimTime">The pass the sweep returned.</param>
    /// <param name="ribbonEndSimTime">The epoch of the projection's last sample.</param>
    /// <param name="sampleStepSeconds">The projection's spacing at that end (its own last gap).</param>
    public static bool PassIsOffTheEndOfTheRibbon(
        double passSimTime, double ribbonEndSimTime, double sampleStepSeconds) =>
        passSimTime >= ribbonEndSimTime - Math.Abs(sampleStepSeconds);

    /// <summary>
    /// What the row says instead of a verdict it cannot honestly give — and which control ends the wait.
    /// Deliberately names the two the captain already has his hand on (#952: <i>"I really like those iterate
    /// buttons"</i>), so the fix is one press away rather than a puzzle.
    /// </summary>
    /// <param name="bodyName">Where the plan is trying to end.</param>
    /// <param name="ribbonEndText">How long the plotted course currently runs, on the panel's own ladder.</param>
    public static string RibbonTooShort(string bodyName, string ribbonEndText) =>
        $"⌛ not judged — the plotted path stops at {ribbonEndText}, short of {bodyName}. "
        + "Stretch Path length (or press auto) until the pass is on the line.";

    // ===== #969 — ARM IT *THEN*, NOT ONLY *NOW* =====
    //
    // The owner's ruling, 2026-08-23: "I just want the possibility to plan the trip from space to docked at
    // one go. So once the burns are planned right I can add the autopilot after the last burn to dock the
    // ship at plan time before the trip is even begun. The point is that now we only have orbit now / dock
    // now. I want dock / orbit option as normal part, a step in the plan. Say three burns and one autopilot
    // to finish the trip to Mars. After that no, absolutely no steps needed if the ship is not interfered
    // with. Now this feature is missing. I want autopilot / dock / orbit THEN not just NOW."
    //
    // Arming an arrive step used to mean "take the ship from HERE" — the rehearsal that settles the promise
    // was flown from the ship's present state, so a plan whose encounter is three burns and nine months away
    // was refused ("can't verify a capture from here") because ballistically, from here, there is no
    // encounter at all. A PLAN-TIME arm is a different sentence: the promise is rehearsed from the state the
    // PLOT delivers at the step's own pass, and the autopilot then has nothing to do until the ship gets
    // there. The two facts below are what tell those two arms apart; the client owns the rehearsal, this
    // owns the law.

    /// <summary>
    /// <b>Is this arrival a THEN?</b> True when the step's pass lies in the future AND the ship is not yet
    /// honestly near the body — which is exactly the trip the owner is describing: burns first, arrival
    /// later. False means the captain is already at the door, and the arm is the old NOW arm (rehearsed
    /// from the present state, with the #957 braking search behind it) — unchanged.
    ///
    /// <para>The same predicate does double duty: it decides which arm to make, and (with the pass epoch
    /// the arm pinned) it is what HOLDS the armed autopilot's hands during the cruise — see
    /// <see cref="ArrivalPromiseIsStillAhead"/>. One law, so the arm and the flight cannot disagree about
    /// whether the arrival has come round yet.</para>
    /// </summary>
    /// <param name="passSimTime">The step's pass on the plotted course.</param>
    /// <param name="simTime">Now.</param>
    /// <param name="distanceNow">How far the ship is from the body right now (m).</param>
    /// <param name="nearRange">The range at which the autopilot honestly has the arrival in its hands —
    /// <see cref="OrbitRule.CaptureRangeHillRadii"/>·hill for an orbit, <see cref="DockRule.EnvelopeMeters"/>
    /// for a μ=0 berth. The floor-free range on purpose: <see cref="OrbitRule.CaptureRangeFloorMeters"/>
    /// would call a ship "in range" across a whole cruise.</param>
    public static bool ArrivalIsAThen(double passSimTime, double simTime, double distanceNow, double nearRange) =>
        passSimTime > simTime && distanceNow > nearRange;

    /// <summary>
    /// <b>The hold.</b> A plan-time arm is a promise about a moment that has not arrived: until it does, the
    /// autopilot must keep its hands off the ship entirely — the captain's own plotted burns are flying her,
    /// and any approach burn fired now would be the autopilot flying its OWN course instead of the plan (and
    /// the convergence watchdog would stand it down for "not converging" on a trip that has not started).
    /// So while this is true the armed loop coasts, touching nothing.
    ///
    /// <para>It stops being true the moment the pass epoch is reached OR the ship is honestly near the body,
    /// whichever comes first — the ship can run early, and the epoch is only a projection. After that the
    /// arm is an ordinary armed arrival and the existing insertion/dock path finishes the trip with no
    /// further input, which is the owner's "absolutely no steps needed".</para>
    /// </summary>
    /// <param name="armedPassSimTime">The pass epoch the arm was rehearsed for; null for a NOW arm (never a
    /// hold).</param>
    /// <param name="simTime">Now.</param>
    /// <param name="distanceNow">How far the ship is from the armed body right now (m).</param>
    /// <param name="nearRange">As in <see cref="ArrivalIsAThen"/>.</param>
    public static bool ArrivalPromiseIsStillAhead(
        double? armedPassSimTime, double simTime, double distanceNow, double nearRange) =>
        armedPassSimTime is { } pass && simTime < pass && distanceNow > nearRange;

    /// <summary>
    /// <b>The sentence the owner asked to be able to read off a finished plan.</b> "Say three burns and one
    /// autopilot to finish the trip to Mars. After that no, absolutely no steps needed" — so when the arm is
    /// accepted, the plan says exactly that, counting the burns still ahead and naming the arrival and its
    /// CHARGED price (#928: never the raw Δv).
    /// </summary>
    /// <param name="burnsAhead">Plotted burns still to fire before the arrival.</param>
    /// <param name="kind">Orbit or dock.</param>
    /// <param name="bodyName">Where the plan ends.</param>
    /// <param name="whenText">A ready-formatted "in 9 mo" / "in 3 d" (the client's own duration ladder).</param>
    /// <param name="chargedPulses">What the arrival itself will cost the tank, at the autopilot's tenth.</param>
    public static string PlanIsComplete(
        int burnsAhead, ArrivalKind kind, string bodyName, string whenText, int chargedPulses)
    {
        string burns = burnsAhead == 1 ? "1 burn" : $"{burnsAhead} burns";
        string arrival = $"{Verb(kind)} {bodyName}";
        return $"{burns} · arrive {whenText} — the autopilot will {arrival} (≈{chargedPulses} p) · nothing more needed";
    }

    /// <summary>The armed THEN step's own row/banner label: the arrival is the last step, and it says who
    /// finishes the trip. "🛰 arrive Mars — the autopilot inserts" / "⚓ arrive at The Rusty Roadstead —
    /// the autopilot docks".</summary>
    public static string ArmedThenLabel(ArrivalKind kind, string bodyName) =>
        kind == ArrivalKind.Dock
            ? $"⚓ arrive at {bodyName} — the autopilot docks"
            : $"🛰 arrive {bodyName} — the autopilot inserts";

    // ===== Formatting: the same ladders the plotting panel speaks in =====

    /// <summary>Metres on the panel's own ladder — AU past a tenth of an AU, M km past a million km,
    /// km below that. Mirrors the client's <c>FormatDistance</c> so a Core-authored row reads exactly
    /// like the burn rows beside it.</summary>
    public static string FormatDistance(double meters)
    {
        const double metersPerAu = 1.495978707e11;
        if (meters >= metersPerAu / 10)
        {
            return (meters / metersPerAu).ToString("F2", CultureInfo.InvariantCulture) + " AU";
        }
        if (meters >= 1e9)
        {
            return (meters / 1e9).ToString("F2", CultureInfo.InvariantCulture) + " M km";
        }
        return (meters / 1000).ToString("F0", CultureInfo.InvariantCulture) + " km";
    }

    /// <summary>Relative speed the way every other nav readout says it: km/s to one decimal.</summary>
    public static string FormatSpeed(double metersPerSecond) =>
        (metersPerSecond / 1000).ToString("F1", CultureInfo.InvariantCulture) + " km/s";
}
