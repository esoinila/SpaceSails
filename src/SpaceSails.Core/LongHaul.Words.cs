using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #246/#255 · THE ONE VOICE FOR THE LONG HAUL'S WORDS — every sentence the mode speaks: the AU
/// distance, the offer, the menu action, the two refusals, the promise, the verdict, the completion
/// line; and the diegetic void overlay, the one loading bar in this game that is a FEATURE.
///
/// <para>Pure text, so it is unit-tested like every other line the mode speaks. Split out of
/// <c>LongHaul.cs</c> under #251 with no member renamed, re-scoped or re-ordered; the two
/// <c>const</c> strings of the overlay came with the lines they belong to.</para>
/// </summary>
public static partial class LongHaul
{
    // ===== The one voice for the long haul's words (HarborVocabulary-style; pure text, unit-tested) =====

    /// <summary>A metric distance spoken in AU, the outer-system unit ("2.34 AU").</summary>
    public static string FormatAu(double meters) =>
        (meters / AstronomicalUnitMeters).ToString("0.##", CultureInfo.InvariantCulture) + " AU";

    /// <summary>The banner's verbatim NOW row while the haul is (briefly) engaged (owner comment 1): the
    /// autopilot owns the leg, so the honest "YOU HAVE THE SHIP" of a manual coast must not stand.</summary>
    public static string BannerNow(string destName) =>
        $"🚀 AUTOPILOT HAS THE SHIP — NOW: long haul to {destName}";

    /// <summary>The arm-surface OFFER beside the normal options (#246 item 1): what the button says.</summary>
    public static string Offer(string destName, int pulsesNow, string arriveDateText) =>
        $"🚀 Long haul to {destName} — ≈{pulsesNow} p, arrive {arriveDateText}";

    /// <summary>The MAP CONTEXT-MENU action (owner refinement: the primary entry). One click from the map
    /// sets the destination AND engages — "autopilot to &lt;planet&gt; vicinity" (the owner's wording; it
    /// lands at the capture range, the last mile stays premium).</summary>
    public static string MenuAction(string planetName, int pulsesNow, string arriveDateText) =>
        $"🚀 Long haul — autopilot to {planetName} vicinity (≈{pulsesNow} p, arrive {arriveDateText})";

    /// <summary>The visible-but-disabled refusal when the solved departure outruns the tank (owner: never a
    /// hidden button — the affordance explains, #212). Speaks the number.</summary>
    public static string RefusalBudget(int neededPulses, int tankPulses) =>
        $"🚀 long haul needs ≈{neededPulses} p; tank has {tankPulses} — top up or find a cheaper window";

    /// <summary>#262 — the arrival insertion brake, named as a step in the trip. The flight plan's second
    /// quoted burn beside the departure: what it costs and what it buys (slowing into the clamp window).</summary>
    public static string InsertionStep(string destName, int pulses) =>
        $"🛬 arrival brake at {destName} — ≈{pulses} p to shed into the clamp window (settles on the dock)";

    /// <summary>#262 — the in-voice WARNING (the conservative default) when the departure is payable but the
    /// round bill is not: the captain may sail on, eyes-open, coasting in too hot to catch without the brake.</summary>
    public static string RoundBillWarning(string destName, int insertionPulses) =>
        $"🚀 the bus includes the departure burn — but the arrival brake at {destName} wants ≈{insertionPulses} p " +
        "the tank won't hold; sail on and you'll coast in hot, no fuel to shed speed";

    /// <summary>#262 — the hard REFUSAL of an unpayable round bill, when the owner flips
    /// <see cref="RefuseOnUnpayableRoundBill"/> to true. Speaks the whole-trip number.</summary>
    public static string RoundBillRefusal(string destName, int roundPulses, int tankPulses) =>
        $"🚀 the whole trip to {destName} wants ≈{roundPulses} p — departure plus the arrival brake — " +
        $"and the tank holds {tankPulses}; top up before the long haul";

    /// <summary>The pre-commit PROMISE, stated plainly (#246 item 3 / owner "the UI does not say I will
    /// get to Uranus"): the destination verdict, the capture radius in AU, the arrival date, the cost now
    /// and the last-mile quote.</summary>
    public static string Promise(
        string planetName, double captureRangeMeters, string arriveDateText, int pulsesNow, int lastMilePulses) =>
        $"course reaches {planetName} capture ({FormatAu(captureRangeMeters)}) on {arriveDateText} — " +
        $"≈{pulsesNow} p now, ≈{lastMilePulses} p quoted for the last mile";

    /// <summary>The honest destination verdict line for a MANUAL coast's nav-target card (#246 item 3):
    /// reaches in N d, or misses with the closest pass named. <paramref name="fromSimTime"/> is the clock
    /// the ETA counts from.</summary>
    public static string ReachVerdict(string planetName, Reach reach, double fromSimTime)
    {
        if (reach.Reaches)
        {
            int days = (int)Math.Round(reach.ElapsedSecondsFrom(fromSimTime) / 86_400.0);
            return $"this course reaches {planetName} capture in {days} d";
        }

        return $"this coast does NOT reach {planetName} — closest pass {FormatAu(reach.ClosestApproachMeters)}";
    }

    /// <summary>The arrival announcement (#246 item 1e): the void is behind you.</summary>
    public static string Completed(string destName, int daysPassed) =>
        $"🚀 long haul complete — {daysPassed} d passed; arrived at {destName} capture range";

    // ===== The diegetic jump overlay (#255) — the one loading bar that is a FEATURE, not a spinner =====
    // Say-the-state at the most dramatic moment the game has: a full-screen crossing that NARRATES the
    // void it is computing. Pure text so it is unit-tested like every other line the mode speaks.

    /// <summary>The whole void reduced to whole years, floor 1 — the counter the overlay ticks up
    /// ("year 3 of 10"). A sub-year hop still reads as a one-year crossing so the beat always lands.</summary>
    public static int VoidYears(double seconds) => Math.Max(1, (int)Math.Round(seconds / (365.0 * 86_400.0)));

    /// <summary>The overlay's headline — the crossing, named.</summary>
    public const string VoidTitle = "CROSSING THE VOID";

    /// <summary>The ticking sub-line: which year of the crossing this frame is painting.</summary>
    public static string VoidYearLine(int year, int totalYears) =>
        $"year {Math.Clamp(year, 1, Math.Max(1, totalYears))} of {Math.Max(1, totalYears)}";

    /// <summary>The charming "no cancel" note — the bus does not stop in the void (owner: ESC is not
    /// offered mid-jump, but the overlay says so with a wink rather than a greyed-out button).</summary>
    public const string VoidNoStop = "the bus does not stop in the void — settle in, we're already gone";

    /// <summary>The destination line under the bar: where this crossing lets out.</summary>
    public static string VoidBound(string destName) => $"bound for {destName}";

    /// <summary>The passbook / ledger line the jump books.</summary>
    public static string LedgerLine(string destName, int daysPassed, int pulsesSpent) =>
        $"🚀 long haul to {destName}: {daysPassed} d crossed, {pulsesSpent} p";

    /// <summary>The verbatim refusal — the reason, spoken (owner: "refusal with the reason").</summary>
    public static string RefusalText(Blocker blocker, string destName) => blocker switch
    {
        Blocker.HunterActive => "🚀 the long haul waits until the sky is clear — a hunter is still on us",
        Blocker.Keeping => $"🚀 disarm the kept orbit first, then the long haul to {destName} is yours",
        Blocker.InsideWell => $"🚀 leave the well before the long haul to {destName} — the void starts in open space",
        Blocker.ShortHop => $"🚀 {destName} is close enough to just watch — skip the coast, don't jump the void",
        Blocker.DoesNotReach => $"🚀 this course does not reach {destName} — trim it green before the long haul",
        _ => string.Empty,
    };
}
