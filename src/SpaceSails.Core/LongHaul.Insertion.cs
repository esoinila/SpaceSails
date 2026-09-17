using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #262 · THE ARRIVAL INSERTION AS A PLANNED, QUOTED STEP, and the ROUND bill that quotes it beside
/// the departure. The owner's stranding at The Tilt is what this half exists for: delivered inside
/// clamp distance at 29.8 km/s with 32 pulses left, and no way to have known.
///
/// <para>Split out of <c>LongHaul.cs</c> under #251 with no member renamed, re-scoped or re-ordered.
/// Its four <c>const</c>s came with it for the same reason as the departure's — a <c>const</c> is
/// folded into its uses and cannot be initialised in the wrong order.</para>
/// </summary>
public static partial class LongHaul
{
    // ===== #262 — THE ARRIVAL INSERTION as a planned, quoted STEP of the long haul =====
    // The owner's stranding (playtest 2026-07-17): delivered to The Tilt (Uranus) with 32/250 pulses at
    // 29.8 km/s relative — inside clamp distance but far above the ≤8 km/s clamp speed, no fuel to brake.
    // The bus banks the honest DEPARTURE burn (#250) and delivers to the capture range on the solved conic
    // (#249), but the ARRIVAL insertion — the 30-50 km/s a captain must shed at an outer world to slow to
    // the clamp/capture window — was an unbudgeted debt. #262 makes it a QUOTED step: the departure solve
    // already knows the arrival relative speed, so the insertion Δv (and its pulse bill) is a pure function
    // of it. The engage gate then quotes the ROUND bill (departure + insertion), and the insertion executes
    // through the existing delivery-time match-and-clamp machinery (#277) — no second billing path.

    /// <summary>The relative speed the arrival insertion brake must bleed down to before the last mile can
    /// complete — the dock clamp speed (<see cref="DockRule.MatchSpeed"/>, 8 km/s: below it a station clamp
    /// or an orbit insert can be flown, above it the ship coasts in too hot to catch, the owner's 29.8 km/s
    /// Tilt stranding). One tunable: raise it and the quote shrinks, lower it and the brake is quoted harder.</summary>
    public const double InsertionTargetSpeed = DockRule.MatchSpeed;

    /// <summary>DESIGN DEFAULT for the owner's still-open question (#262): when the departure is payable but
    /// the ROUND bill (departure + arrival brake) is not, do we hard-REFUSE or WARN-and-proceed? Conservative
    /// default is <c>false</c> — WARN the captain in-voice and let them sail on eyes-open (a payable departure
    /// already gets you moving; the brake debt is spoken, not enforced). Flip this one const to <c>true</c>
    /// to hard-refuse an unpayable round bill instead.</summary>
    public const bool RefuseOnUnpayableRoundBill = false;

    /// <summary>The arrival insertion brake, quoted (#262): the Δv to shed from the arrival relative speed
    /// down to the clamp/capture window, and its pulse bill priced with the one <see cref="OrbitRule.PulsesFor"/>
    /// kernel. <see cref="Needed"/> is false when the coast already arrives slow enough to catch (no brake owed).</summary>
    /// <param name="DeltaV">|brake| in m/s — the speed to shed (0 when the arrival is already in the window).</param>
    /// <param name="Pulses">The insertion bill, the number quoted alongside the departure and summed into the round bill.</param>
    /// <param name="ArrivalRelativeSpeed">Speed relative to the target at arrival (the debt's origin).</param>
    /// <param name="TargetSpeed">The window the brake bleeds down to (<see cref="InsertionTargetSpeed"/>).</param>
    public readonly record struct Insertion(double DeltaV, int Pulses, double ArrivalRelativeSpeed, double TargetSpeed)
    {
        /// <summary>True when the arrival is hot enough to owe a real brake (some pulses to shed).</summary>
        public bool Needed => Pulses > 0;
    }

    /// <summary>Quote the arrival insertion brake from the arrival speeds (#262). Pure: the Δv is the speed
    /// above the clamp window to shed, priced against the ship's world speed at arrival (the same "current
    /// speed" basis every assisted burn is priced against).</summary>
    public static Insertion SolveInsertion(
        double arrivalRelativeSpeed, double arrivalSpeed, double targetSpeed = InsertionTargetSpeed)
    {
        double shed = Math.Max(0.0, arrivalRelativeSpeed - targetSpeed);
        int pulses = shed > 0 ? OrbitRule.PulsesFor(shed, arrivalSpeed) : 0;
        return new Insertion(shed, pulses, arrivalRelativeSpeed, targetSpeed);
    }

    /// <summary>The arrival insertion brake for a solved departure (#262), or an empty (no-brake) quote when
    /// the departure did not solve.</summary>
    public static Insertion InsertionFor(Departure departure, double targetSpeed = InsertionTargetSpeed) =>
        departure.Ok
            ? SolveInsertion(departure.ArrivalRelativeSpeed, departure.ArrivalSpeed, targetSpeed)
            : default;

    /// <summary>The round-bill gate's verdict (#262): the engage already refuses an unpayable DEPARTURE (you
    /// cannot fire a burn you cannot pay); this extends it to the whole trip.</summary>
    public enum RoundBillVerdict
    {
        /// <summary>The tank covers departure AND the arrival brake — the whole trip is paid for.</summary>
        Clear,

        /// <summary>The departure is payable but the round bill is not — the conservative default WARNS and
        /// lets the captain sail on, coasting in hot with the brake unfunded (see <see cref="RefuseOnUnpayableRoundBill"/>).</summary>
        WarnHotArrival,

        /// <summary>The departure itself outruns the tank — the existing hard refusal (unchanged behavior).</summary>
        RefuseDeparture,

        /// <summary>The round bill outruns the tank AND the flag is flipped to hard-refuse it.</summary>
        RefuseRoundBill,
    }

    /// <summary>Evaluate the round bill against the tank (#262). Departure unaffordable is always a hard
    /// refuse (the departure burn genuinely fires at engage). A payable departure with an unpayable round
    /// bill is WARN or REFUSE per <paramref name="refuseOnUnpayableRound"/> — the one owner-flippable choice.</summary>
    public static RoundBillVerdict EvaluateRoundBill(
        int departurePulses, int insertionPulses, int budgetPulses,
        bool refuseOnUnpayableRound = RefuseOnUnpayableRoundBill)
    {
        if (departurePulses > budgetPulses)
        {
            return RoundBillVerdict.RefuseDeparture;
        }

        long round = (long)departurePulses + Math.Max(0, insertionPulses);
        if (round > budgetPulses)
        {
            return refuseOnUnpayableRound ? RoundBillVerdict.RefuseRoundBill : RoundBillVerdict.WarnHotArrival;
        }

        return RoundBillVerdict.Clear;
    }
}
