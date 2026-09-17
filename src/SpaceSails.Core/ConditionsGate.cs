using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #243 · <b>THE CONDITIONS STRIP — ONE INSTRUMENT, SWAPPABLE LIMITS.</b>
///
/// <para>Owner, on approach to the roadster: <i>"Let's have a more clear display of how well we meet the
/// criteria. Are we closing or distancing. The piracy pop-up has the criteria nicely shown — green when
/// distance matches, green when relative speed difference is low enough. Now it is slow to read visually:
/// read the scope, remember the limit was 3 from another part of the screen, then check the other variable.
/// In the boarding dialog just seeing green or red told the story much faster."</i> And, the same session,
/// the generalization that is the whole design: <i>"I guess it could be a general meet-the-conditions
/// [display] in the navigation screen."</i></para>
///
/// <para>Then the distillation that turned it from four gauges into one, verbatim: <i>"trying to be
/// dockable, or capture etc — we always have the duo of relative speed and distance, and some criteria for
/// those."</i> So this is <b>not</b> a gauge per feature. It is ONE gauge, parameterized by the two numbers
/// the ACTIVE gate happens to enforce. Boarding a hull, clamping onto a haven, arming an orbit insertion and
/// stepping into the shuttle are, as instruments, the same instrument with different limits printed on it.</para>
///
/// <h3>ONE SOURCE OF TRUTH — the law this file exists to obey</h3>
///
/// <para>Every limit below is <b>read through the code that enforces it</b>, never restated:</para>
/// <list type="bullet">
///   <item><b>Boarding window</b> — <see cref="CaptureRule.CaptureRadiusMeters"/> /
///   <see cref="CaptureRule.MaxRelativeSpeed"/>, tested by <see cref="CaptureRule.RangeInWindow"/> and
///   <see cref="CaptureRule.SpeedInWindow"/>, which are the two halves <see cref="CaptureRule.IsInWindow"/>
///   itself is now written in terms of. The shuttles launch on the very predicate the chip colours on.</item>
///   <item><b>Dock envelope</b> — <see cref="DockFocus.Rows"/> verbatim: #200's panel already composes the
///   clamp's gates off the resolved <see cref="DockAffordance"/> (#212's one truth), so the strip does not
///   compose a second set. It borrows that list and hangs a trend on it.</item>
///   <item><b>Orbit capture / the armed arrival</b> — <see cref="ArrivalStepRule.Check"/>, whose limits are
///   <see cref="OrbitRule.CaptureRange"/> and <see cref="OrbitRule.MaxRelativeSpeed"/> and whose two
///   predicates borrow the SENSE as well as the number (the insertion window is strict, the clamp is
///   inclusive). That is the same judgement the #955 ARRIVE row wears its ✓/✗ from.</item>
///   <item><b>Shuttle hop</b> — <see cref="ShuttleRange.InRange"/>. One axis, and the strip says one axis:
///   the hop has a reach and no speed gate at all, and drawing an empty second chip would be an invented
///   criterion.</item>
/// </list>
///
/// <para>Not one threshold is typed in this file. <c>TheConditionsStripQuotesNobodysNumbers</c> asks each
/// gate for its limits by CALLING the enforcing code and compares, so the day a constant moves and the strip
/// does not, the strip is red rather than merely wrong.</para>
///
/// <h3>THE TREND, AND WHERE IT HONESTLY COMES FROM</h3>
///
/// <para>Owner: <i>"Are we closing or distancing."</i> A failing chip that is closing on its limit reads
/// completely differently from one drifting away from it, so each chip carries a
/// <see cref="ConditionTrend"/>. The rate that drives it is <b>#210's signed range-rate</b>
/// (<see cref="RelativeMotion.ClosingSpeed"/>) — the one rate the game already measures — negated, because
/// every axis here passes by being SMALL: range falling is range improving.</para>
///
/// <para><b>The relative-speed chip carries no arrow, on purpose.</b> Nothing in the sim computes
/// d|Δv|/dt — there is no relative-acceleration reading to read — and a chip that drew an arrow anyway
/// would be this repository's third named bug class installed by hand: a drawn shape reporting something
/// the sim never worked out. <see cref="Chip"/> takes an optional rate and draws
/// <see cref="ConditionTrend.Unknown"/> (no arrow, no word) when there is none, so the day a relative
/// acceleration exists the arrow appears with no change to the instrument.</para>
/// </summary>
public static class ConditionsGate
{
    /// <summary>
    /// The rate at which "closing" becomes "neither" — a micrometre per second, i.e. the geometry's own
    /// zero.
    ///
    /// <para><b>It is this small on purpose, and the first cut of it was much larger and wrong.</b> A band
    /// scaled to the chip's LIMIT looks principled and is not: the dock envelope is 5×10⁸ m and the speeds
    /// crossing it are ~10³ m/s, so even a tenth of a percent of the limit per second swallows every closing
    /// speed the game can produce and the arrow never moves — an instrument that says "steady" while the
    /// ship falls toward a berth. There is nothing to filter here anyway: the rate is
    /// <see cref="RelativeMotion.ClosingSpeed"/>, computed analytically from exact state rather than sampled
    /// between frames, so it carries no noise to debounce. A purely lateral pass reads exactly zero by the
    /// geometry, and that is the only case <see cref="ConditionTrend.Steady"/> is for.</para>
    /// </summary>
    public const double StillnessMps = 1e-6;

    // ── The gates, as chips ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The boarding / come-alongside window (#177/#178/#186) — the gate the owner's piracy pop-up already
    /// draws, now drawn by the same component everywhere. Both limits are asked of <see cref="CaptureRule"/>
    /// by calling the predicates the shuttle launch itself runs on.
    /// </summary>
    /// <param name="callsign">The hull in the window — named, because a gate about nothing is unreadable.</param>
    /// <param name="distance">Range to her this instant (m).</param>
    /// <param name="relSpeed">Relative speed to her this instant (m/s).</param>
    /// <param name="closingSpeedMps">#210's signed range-rate (positive = the gap shrinking).</param>
    public static ConditionsReading Board(
        string callsign, double distance, double relSpeed, double closingSpeedMps) =>
        new(ConditionGateKind.Board, callsign,
        [
            Chip("close enough", Km(distance), "≤ " + Km(CaptureRule.CaptureRadiusMeters),
                CaptureRule.RangeInWindow(distance), -closingSpeedMps),
            Chip("speed match", KmPerSecond(relSpeed), "≤ " + KmPerSecond(CaptureRule.MaxRelativeSpeed),
                CaptureRule.SpeedInWindow(relSpeed), null),
        ]);

    /// <summary>
    /// The ⚓ clamp envelope — <b>borrowed whole from #200's focus panel</b> rather than composed again.
    /// <see cref="DockFocus.Rows"/> already reads every number off the resolved affordance (#212) and every
    /// gate off <see cref="DockRule"/>, including the third row that appears only when a terminal match is
    /// the thing being asked for; re-deriving that here would be two typings of one law, which is the very
    /// bug this issue is about. The strip adds exactly one thing to that list: the range row's trend.
    /// </summary>
    public static ConditionsReading Dock(DockAffordance affordance, int pulsesAboard, double closingSpeedMps)
    {
        IReadOnlyList<DockGateRow> rows = DockFocus.Rows(affordance, pulsesAboard);
        var chips = new List<GateCriterion>(rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            DockGateRow row = rows[i];
            // Only the FIRST row is a range, and only a range has a rate the game measures. The drift row
            // and the match-burn quote get no arrow rather than a guessed one.
            double? rate = i == 0 ? -closingSpeedMps : null;
            chips.Add(Chip(row.Label, row.Reading, row.Gate, row.Inside, rate));
        }

        return new ConditionsReading(
            ConditionGateKind.Dock, affordance.HavenName ?? "the berth", chips);
    }

    /// <summary>
    /// The capture gate of an orbit insertion — the two numbers the armed autopilot's arrival is judged on
    /// (#955), asked of <see cref="ArrivalStepRule"/> so the strip and the ARRIVE row's ✓/✗ can never
    /// disagree about one pass. <paramref name="hillRadius"/> is the body's, because capture range is a
    /// function of the well and not a constant.
    /// </summary>
    public static ConditionsReading Orbit(
        string bodyName, double distance, double relSpeed, double hillRadius, double closingSpeedMps)
    {
        ArrivalStepRule.ArrivalCheck check =
            ArrivalStepRule.Check(ArrivalStepRule.ArrivalKind.Orbit, bodyName, distance, relSpeed, hillRadius);
        return new ConditionsReading(ConditionGateKind.Orbit, bodyName,
        [
            Chip("close enough", Km(check.Distance), "≤ " + Km(check.DistanceLimit),
                check.DistanceOk, -closingSpeedMps),
            Chip("slow enough", KmPerSecond(check.RelSpeed), "< " + KmPerSecond(check.SpeedLimit),
                check.SpeedOk, null),
        ]);
    }

    /// <summary>
    /// The shuttle hop (#163) — <b>one chip, because the hop has one gate.</b> Reach is
    /// <see cref="ShuttleRange.InRange"/>; there is no speed criterion to be green or red about, and the
    /// strip refuses to draw a second chip for a condition nothing enforces.
    /// </summary>
    public static ConditionsReading Hop(string bodyName, double distance, double closingSpeedMps) =>
        new(ConditionGateKind.ShuttleHop, bodyName,
        [
            Chip("in shuttle reach", Km(distance), "≤ " + Km(ShuttleRange.RangeMeters),
                ShuttleRange.InRange(distance), -closingSpeedMps),
        ]);

    // ── Which gate is the ACTIVE one ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Read order when more than one gate is live at once, smallest number = spoken first. The order is the
    /// order of COMMITMENT: a boarding window is a felony that closes in seconds, a clamp is the thing the
    /// hand is on, an insertion is minutes away, and the shuttle hop is a door that will still be there.
    /// </summary>
    public static int Priority(ConditionGateKind kind) => kind switch
    {
        ConditionGateKind.Board => 0,
        ConditionGateKind.Dock => 1,
        ConditionGateKind.Orbit => 2,
        _ => 3,
    };

    /// <summary>
    /// The one gate the strip speaks about, or <c>null</c> — <b>and null means the strip is ABSENT</b>, not
    /// an empty box. Owner's reading complaint was about hunting across the screen; a permanently reserved
    /// rectangle that is blank most of the time is one more thing to hunt past.
    /// </summary>
    public static ConditionsReading? Active(IReadOnlyList<ConditionsReading> offered)
    {
        ArgumentNullException.ThrowIfNull(offered);
        ConditionsReading? best = null;
        foreach (ConditionsReading candidate in offered)
        {
            if (candidate.Criteria.Count == 0)
            {
                continue;   // a gate with nothing to say is not a gate
            }
            if (best is not { } held || Priority(candidate.Kind) < Priority(held.Kind))
            {
                best = candidate;
            }
        }

        return best;
    }

    // ── The voice ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The strip's heading — what is being attempted, and at what. One line, the piracy box's own
    /// grammar ("needs BOTH" becomes "needs" so a one-criterion gate is not made to lie about a second).</summary>
    public static string Title(ConditionsReading reading) =>
        $"{Glyph(reading.Kind)} {Verb(reading.Kind)} {reading.TargetName} needs:";

    /// <summary>The gate's own word for what the ship is trying to do.</summary>
    public static string Verb(ConditionGateKind kind) => kind switch
    {
        ConditionGateKind.Board => "Boarding",
        ConditionGateKind.Dock => "Clamping at",
        ConditionGateKind.Orbit => "Orbiting",
        _ => "The hop to",
    };

    /// <summary>The gate's glyph — each one already the game's own mark for that act, so the strip mints no
    /// new vocabulary: 🎯 is the autosteal box's, ⚓ is the clamp's, 🛰 is the arm-the-arrival button's and
    /// 🛸 is the shuttle's.</summary>
    public static string Glyph(ConditionGateKind kind) => kind switch
    {
        ConditionGateKind.Board => "🎯",
        ConditionGateKind.Dock => "⚓",
        ConditionGateKind.Orbit => "🛰",
        _ => "🛸",
    };

    /// <summary>
    /// #243 · <b>THE SUMMARY THE SCOPE CORNER MIRRORS.</b> Owner: <i>"Mirror the strip's summary in the
    /// Scope corner (where the eyes are during an approach)."</i> During an approach the captain is watching
    /// the glass, not the HUD column, so the same verdict is printed there — and it has to survive in a
    /// 280-pixel monospace corner, which is why it is a glyph and one tick per criterion in the strip's own
    /// order rather than a sentence. ✓/✗ are <see cref="ArrivalStepRule.Badge"/>'s marks, already the
    /// game's pass/fail characters.
    /// </summary>
    public static string ScopeSummary(ConditionsReading reading)
    {
        var marks = new System.Text.StringBuilder(reading.Criteria.Count);
        foreach (GateCriterion c in reading.Criteria)
        {
            marks.Append(c.Inside ? '✓' : '✗');
        }

        return $"{Glyph(reading.Kind)} {marks}";
    }

    /// <summary>The arrow a chip wears, or the empty string when there is no rate to draw one from. ▼ is
    /// the number FALLING toward its limit (every axis here passes by being small, so falling is good) and
    /// ▲ is it climbing away. Owner asked for "▲▼ per #210's range-rate" and these are those.</summary>
    public static string TrendGlyph(ConditionTrend trend) => trend switch
    {
        ConditionTrend.Improving => "▼",
        ConditionTrend.Worsening => "▲",
        _ => string.Empty,
    };

    /// <summary>The hover word behind the arrow — the answer to the owner's "are we closing or distancing",
    /// said once so the strip, the pop-up and anything later all word it the same way.</summary>
    public static string TrendWord(ConditionTrend trend) => trend switch
    {
        ConditionTrend.Improving => "closing on the limit",
        ConditionTrend.Worsening => "getting worse",
        ConditionTrend.Steady => "holding steady",
        _ => string.Empty,
    };

    // ── The arithmetic ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Build one chip. <paramref name="ratePerSecond"/> is the signed rate of change of the chip's OWN
    /// value (not of the gap to the limit), so negative is falling and therefore improving; <c>null</c> is
    /// "nothing measures this", which draws no arrow at all.
    /// </summary>
    public static GateCriterion Chip(
        string label, string reading, string gate, bool inside, double? ratePerSecond) =>
        new(label, reading, gate, inside, TrendOf(ratePerSecond));

    /// <summary>
    /// The trend law, in one place because both the chips and their guard have to agree about it. Negative
    /// is the value FALLING toward its gate (<see cref="ConditionTrend.Improving"/> — every axis here passes
    /// by being small), positive is it climbing away (<see cref="ConditionTrend.Worsening"/>), the
    /// geometry's own zero (<see cref="StillnessMps"/>) is <see cref="ConditionTrend.Steady"/>, and no rate
    /// at all is <see cref="ConditionTrend.Unknown"/> — no arrow, no word.
    /// </summary>
    public static ConditionTrend TrendOf(double? ratePerSecond)
    {
        if (ratePerSecond is not { } rate || double.IsNaN(rate))
        {
            return ConditionTrend.Unknown;
        }

        return rate < -StillnessMps ? ConditionTrend.Improving
            : rate > StillnessMps ? ConditionTrend.Worsening
            : ConditionTrend.Steady;
    }

    /// <summary>A distance in metres as the owner's own coaching unit — "500,000 km". The formatter
    /// <see cref="DockFocus"/> has always used, hoisted here so the clamp's rows and every other gate's
    /// chips are formatted by one function (#200's panel now calls this one).</summary>
    public static string Km(double meters) =>
        (meters / 1000).ToString("N0", CultureInfo.InvariantCulture) + " km";

    /// <summary>A speed in m/s as "8 km/s" / "10.5 km/s". Same hoist, same reason.</summary>
    public static string KmPerSecond(double metersPerSecond) =>
        (metersPerSecond / 1000).ToString("0.#", CultureInfo.InvariantCulture) + " km/s";
}

/// <summary>Which of the ship's two-condition gates the strip is currently speaking about (#243).</summary>
public enum ConditionGateKind
{
    /// <summary>The boarding / come-alongside window — <see cref="CaptureRule"/>.</summary>
    Board,

    /// <summary>The ⚓ clamp envelope — <see cref="DockRule"/>, via the resolved <see cref="DockAffordance"/>.</summary>
    Dock,

    /// <summary>The orbit capture gate of an armed arrival — <see cref="OrbitRule"/>, via <see cref="ArrivalStepRule"/>.</summary>
    Orbit,

    /// <summary>The shuttle-bay hop's reach — <see cref="ShuttleRange"/>. One criterion, not two.</summary>
    ShuttleHop,
}

/// <summary>Which way a criterion is moving (#210's range-rate, read as "are we closing or distancing").</summary>
public enum ConditionTrend
{
    /// <summary>Nothing in the sim measures this criterion's rate — no arrow is drawn, rather than a guessed one.</summary>
    Unknown,

    /// <summary>The number is falling toward its limit. Every axis here passes by being small, so this is good news
    /// whether the chip is currently green or red.</summary>
    Improving,

    /// <summary>Neither closing nor opening to speak of.</summary>
    Steady,

    /// <summary>The number is climbing away from its limit.</summary>
    Worsening,
}

/// <summary>
/// One condition of the active gate, ready to draw (#243). Deliberately the same four fields
/// <see cref="DockGateRow"/> has carried since #200 — <b>what the number is, where the ship is, what it must
/// reach, and whether it is there</b> — plus the trend the owner asked for, so #200's rows convert into
/// these without a translation layer and the two surfaces cannot drift into different shapes.
/// </summary>
/// <param name="Label">What the number is (the criterion's name).</param>
/// <param name="Reading">Where the ship is right now, formatted.</param>
/// <param name="Gate">The value it must reach, formatted with its comparison.</param>
/// <param name="Inside">True when the reading satisfies the criterion this instant.</param>
/// <param name="Trend">Which way it is moving, or <see cref="ConditionTrend.Unknown"/>.</param>
public readonly record struct GateCriterion(
    string Label, string Reading, string Gate, bool Inside, ConditionTrend Trend);

/// <summary>The active gate, as the strip draws it: what is being attempted, at what, and the criteria.</summary>
/// <param name="Kind">Which gate.</param>
/// <param name="TargetName">The hull, haven or body the gate is about.</param>
/// <param name="Criteria">One chip per condition, in the order they must be met.</param>
public readonly record struct ConditionsReading(
    ConditionGateKind Kind, string TargetName, IReadOnlyList<GateCriterion> Criteria)
{
    /// <summary>Every criterion met — the gate is open this instant.</summary>
    public bool AllInside
    {
        get
        {
            if (Criteria is null || Criteria.Count == 0)
            {
                return false;
            }

            foreach (GateCriterion c in Criteria)
            {
                if (!c.Inside)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
