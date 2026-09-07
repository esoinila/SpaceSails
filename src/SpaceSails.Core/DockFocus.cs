using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #200 · <b>THE DOCKING NUMBERS TO HIT.</b> Owner, inbound to a haven: <i>"I want to see CLEARLY how close
/// I am to docking distance and speed limits… Own pop-up of the numbers to hit, just like in piracy-hold."</i>
///
/// <para>The piracy model is the autosteal criterion box on a prey dossier — <b>current value vs required
/// value, one row per gate, obviously green when the number is inside its gate</b>. This is that box for the
/// clamp, and it is composed HERE rather than in the page for one reason: the rows must be the very numbers
/// the clamp enforces. Every reading comes off the already-resolved <see cref="DockAffordance"/> — the one
/// truth the ⚓ button and the envelope line read (#212) — and every gate is quoted straight from
/// <see cref="DockRule"/>. Nothing here re-types a threshold, so the panel cannot say "inside" about a
/// number the door refuses (the bug class: one law, two typings).</para>
///
/// <para>#761 · when the clamp refuses, the WHY has to be legible: the refusing row is the one drawn outside
/// its gate. A hopelessly hot approach (<see cref="DockPhase.TooHot"/>) is refused by a third number — the
/// mass the terminal match would cost against the mass aboard — so that row appears exactly when a match is
/// the thing being asked for, and it carries its own verdict.</para>
/// </summary>
/// <param name="Label">What the number is (the gate's name).</param>
/// <param name="Reading">Where the ship is right now, formatted.</param>
/// <param name="Gate">The value it must reach, formatted with its comparison.</param>
/// <param name="Inside">True when the reading satisfies the gate this instant.</param>
public readonly record struct DockGateRow(string Label, string Reading, string Gate, bool Inside);

/// <summary>The pure composition of the #200 docking focus panel: when it speaks, the rows it shows, and
/// the one-line verdict under them. UI-free — the client feeds it the frame's affordance and the tank.</summary>
public static class DockFocus
{
    // ---- The coaching sentences. ONE copy, shared by the focus panel's verdict and Map.Docking's
    // DockStatusLine, so the panel's last line and the nav-target line can never say different things
    // about the same approach. (These are the sentences #213 already shipped, moved, not rewritten.)

    /// <summary>Already clamped on — there is nothing left to hit.</summary>
    public const string ClampedOnLine = "clamped on — lying low";

    /// <summary>Inside the clamp window and matched: the plain ⚓ Dock is live.</summary>
    public const string ClampNowLine = "alongside and matched — hit ⚓ Dock to clamp on";

    /// <summary>In range but coasting too fast for the arm — #213's one-press terminal match.</summary>
    public const string MatchClampLine = "alongside but hot — hit ⚓ Match & clamp to null the drift into the window";

    /// <summary>Still outside the envelope: the range is the number to hit first. Quotes
    /// <see cref="DockRule.EnvelopeMeters"/>, never a typed-in distance.</summary>
    public static string CoastCloserLine() => $"coast within {Km(DockRule.EnvelopeMeters)} to clamp on";

    /// <summary>True when docking is the live intent and the panel should be on the glass: a dock haven is
    /// the captain's destination or armed target, or the clamp is in (latched) range of one. Anything else —
    /// a haven merely drifting past — shows nothing, which is the half of #200 the owner filed as "it
    /// toggles me meaningless nearby targets here".</summary>
    public static bool IsLive(DockAffordance affordance) => affordance.Phase is not DockPhase.None;

    /// <summary>The rows of the focus panel, in the order they must be hit: close the range, match the
    /// drift, and — only when a terminal match is what the door is asking for — pay for the burn.</summary>
    /// <param name="affordance">This frame's resolved dock affordance (the ⚓ button's own truth).</param>
    /// <param name="pulsesAboard">Reaction mass free to burn — the same effective tank
    /// <see cref="DockAffordanceRule.Evaluate"/> was given, so the row and the phase agree.</param>
    public static IReadOnlyList<DockGateRow> Rows(DockAffordance affordance, int pulsesAboard)
    {
        List<DockGateRow> rows = new(3);
        if (!IsLive(affordance))
        {
            return rows;
        }

        rows.Add(new DockGateRow(
            "close enough",
            Km(affordance.Distance),
            "≤ " + Km(DockRule.EnvelopeMeters),
            affordance.Distance <= DockRule.EnvelopeMeters));

        rows.Add(new DockGateRow(
            "drift matched",
            KmPerSecond(affordance.RelSpeed),
            "≤ " + KmPerSecond(DockRule.MatchSpeed),
            affordance.RelSpeed <= DockRule.MatchSpeed));

        // The third gate exists only while a terminal match is the ask (#213): in the plain clamp there is
        // no burn to pay for, and out on the approach the quote is not yet the thing standing in the way.
        if (affordance.Phase is DockPhase.MatchClamp or DockPhase.TooHot)
        {
            rows.Add(new DockGateRow(
                "match burn",
                $"{affordance.MatchPulses.ToString(CultureInfo.InvariantCulture)} p",
                $"≤ {pulsesAboard.ToString(CultureInfo.InvariantCulture)} p aboard",
                affordance.MatchPulses <= pulsesAboard));
        }

        return rows;
    }

    /// <summary>The one line under the rows: what the captain does next about the numbers above. The three
    /// non-refusing phases speak the shipped coaching sentences verbatim; the refusal (#761) names the
    /// number that refused.</summary>
    public static string Verdict(DockAffordance affordance, int pulsesAboard) => affordance.Phase switch
    {
        DockPhase.Clamp => "→ " + ClampNowLine,
        DockPhase.MatchClamp => "→ " + MatchClampLine,
        DockPhase.TooHot =>
            $"→ too hot to clamp, and the match is unaffordable — it needs {affordance.MatchPulses.ToString(CultureInfo.InvariantCulture)} p"
            + $" and {pulsesAboard.ToString(CultureInfo.InvariantCulture)} are aboard",
        DockPhase.Approach => "→ " + CoastCloserLine(),
        _ => string.Empty,
    };

    /// <summary>A distance in metres as the owner's own coaching unit — "500,000 km".</summary>
    private static string Km(double meters) =>
        (meters / 1000).ToString("N0", CultureInfo.InvariantCulture) + " km";

    /// <summary>A speed in m/s as "8 km/s" / "10.5 km/s".</summary>
    private static string KmPerSecond(double metersPerSecond) =>
        (metersPerSecond / 1000).ToString("0.#", CultureInfo.InvariantCulture) + " km/s";
}
