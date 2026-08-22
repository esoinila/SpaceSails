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
