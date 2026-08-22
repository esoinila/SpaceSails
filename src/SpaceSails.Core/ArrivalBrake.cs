using System;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #304 — THE ARRIVAL BRAKE ASKS. Owner ruling (2026-07-18, answering the Second Wind's Q1): "Let's have
/// it ask, it is hard to remember in the heat of the moment otherwise. :-D" — overriding the plan doc's
/// "insertions fire, docks ask". The #262/#284 arrival insertion brake — the burn a long haul must fire at
/// arrival to shed the hot approach speed into the clamp window (<see cref="LongHaul.SolveInsertion"/>) —
/// and its #290/#301 aerobrake variant are now classed with the are-you-sure family: at the brake window
/// the ship ASKS the captain in-voice, carrying the quoted bill, and fires ONLY on consent.
///
/// <para><b>The timing law, as a pure state machine</b> so it unit-tests without a browser (the
/// <see cref="OverlayLayout"/> / <see cref="HarborVocabulary"/> house style — geometry and words kept out
/// of the razor): ASK when the window OPENS (early enough to act); on consent FIRE exactly once (the
/// <see cref="Phase.Fired"/> guard forbids a second fire — no double-billing, no double-fire); on HOLD
/// leave the manual state untouched AND leave the captain alone. Never silently skip, never silently fire.
/// The window shutting (the ship braked, docked, or left) resets the gate so the NEXT arrival asks afresh.</para>
///
/// <para><b>#962 · "Hold" means held.</b> Owner, reading his own screenshot: "the jupiter brake re-appears
/// after I click I'll fly by hand." The gate used to snooze and nag back eight seconds later, which is a
/// fine shape for a reminder and a terrible one for an ANSWER. A decline is now terminal for this arrival:
/// the gate sits in <see cref="Phase.Held"/> until the window shuts, and the next arrival asks afresh.</para>
///
/// <para>Pure and deterministic: the transitions are a function of the gate and whether the window is
/// open, and nothing else. No clock at all now (#962 retired the snooze deadline, which was the only thing
/// that ever needed one), no wall clock, no randomness.</para>
/// </summary>
public static class ArrivalBrake
{
    /// <summary>Where the gate stands for one arrival's brake decision.</summary>
    public enum Phase
    {
        /// <summary>No ask pending — the window is shut (no brake owed) or the decision is spent.</summary>
        Dormant,

        /// <summary>The prompt is up, awaiting the captain's consent or decline.</summary>
        Asking,

        /// <summary>The captain answered "hold — I'll shed by hand". Nothing fired, the manual state is
        /// untouched, and the ask does NOT come back for this arrival (#962). The gate leaves this phase
        /// only when the window shuts, which is what makes the NEXT arrival ask afresh.</summary>
        Held,

        /// <summary>The brake fired (once). The guard keeps it fired for the rest of this window so a
        /// double-click or a re-entrant frame can never fire the burn — or bill it — twice.</summary>
        Fired,
    }

    /// <summary>The gate: which <see cref="Phase"/> the brake decision is in. A value type so the client
    /// holds it as one field and the tests read it like a coordinate.</summary>
    public readonly record struct Gate(Phase State)
    {
        /// <summary>The window-shut / not-yet-opened gate.</summary>
        public static readonly Gate Closed = new(Phase.Dormant);

        /// <summary>True only while the prompt should be on screen awaiting the captain.</summary>
        public bool Asking => State == Phase.Asking;

        /// <summary>True once the brake has fired this window — the once-guard the fire handler checks.</summary>
        public bool HasFired => State == Phase.Fired;

        /// <summary>True once the captain has answered "hold" for this arrival — the ask is SPENT, not
        /// merely hidden, so nothing re-raises it while this window lasts (#962).</summary>
        public bool IsHeld => State == Phase.Held;
    }

    /// <summary>
    /// The per-frame law. Given the current <paramref name="gate"/> and whether the brake
    /// <paramref name="windowOpen"/> (the ship is arrived-and-hot with a brake owed), return the gate for
    /// this frame. Opens the ask the frame the window opens; holds an <see cref="Phase.Asking"/> prompt up,
    /// a <see cref="Phase.Fired"/> decision fired and a <see cref="Phase.Held"/> answer held; and CLOSES the
    /// moment the window shuts (so a later arrival asks afresh). Never fires and never dismisses on its own
    /// — those are the captain's, through <see cref="Fire"/> and <see cref="Hold"/>.
    /// </summary>
    public static Gate Advance(Gate gate, bool windowOpen)
    {
        if (!windowOpen)
        {
            return Gate.Closed; // window shut — reset so the NEXT arrival asks again (never silently carry)
        }

        return gate.State switch
        {
            Phase.Dormant => new Gate(Phase.Asking), // window just opened → ask
            _ => gate,                               // Asking stays up; Fired stays fired; Held stays held (#962)
        };
    }

    /// <summary>The captain answered HOLD: the ask goes away and STAYS away for this arrival (#962).
    /// Stateless beyond the prompt — nothing fires, no pulses move, the manual state is exactly as it was.
    /// A later arrival (the window shutting, then re-opening) asks afresh.</summary>
    public static Gate Hold(Gate gate) => new(Phase.Held);

    // ===== Is a brake even OWED here? (the window predicate, pure so it tests without a browser) =====

    /// <summary>
    /// #962 · Whether the arrival-brake window is genuinely open this frame. Owner, from a screenshot of the
    /// ship <i>clamped on at The Red Eye, rel 0.0 km/s</i>, with a Jupiter brake card up: "This pop-up still
    /// shows when we are docked?" A clamped ship is not arriving — the berth holds her on the station's own
    /// drift (her speed relative to the PLANET the berth orbits is just the berth's orbital speed, which is
    /// what used to hold this window open forever), and no burn she could fire would mean a thing. So the
    /// window is shut whenever she is <paramref name="clamped"/>, shut while the void is being
    /// <paramref name="crossingTheVoid"/> (the world is not there yet), and otherwise open only when she is
    /// inside the destination's <paramref name="vicinityRadius"/> AND still hotter than the clamp window.
    /// </summary>
    public static bool WindowOpen(
        bool clamped,
        bool crossingTheVoid,
        double distance,
        double vicinityRadius,
        double relativeSpeed,
        double clampWindowSpeed)
    {
        if (clamped || crossingTheVoid)
        {
            return false; // berthed, or mid-jump: nothing is arriving, so nothing can be braked
        }

        return distance <= vicinityRadius && relativeSpeed > clampWindowSpeed;
    }

    /// <summary>
    /// #962 · Whether an AEROBRAKE offer is worth putting to the captain at all. The owner's screenshot
    /// carried the nonsense version — "commits the ship to 0 passes (≈0 p saved)" — which is an offer to do
    /// nothing in exchange for nothing. A real aerobrake flies at least one pass and saves at least one
    /// pulse; anything short of that is not a trade and must never reach the card.
    /// </summary>
    public static bool AerobrakeWorthOffering(int passes, int pulsesSaved) => passes > 0 && pulsesSaved > 0;

    /// <summary>The captain consented: mark the brake fired. Idempotent — a second consent is a no-op (the
    /// once-guard), so a double-click or a re-entrant frame can never fire, or bill, the burn twice.</summary>
    public static Gate Fire(Gate gate) => gate.HasFired ? gate : gate with { State = Phase.Fired };

    /// <summary>
    /// The outcome of firing the propulsive brake against a possibly-insufficient tank: the pulses actually
    /// spent (never more than the tank) and the relative speed the ship is left at. A fully-funded fire
    /// reaches the clamp window (<paramref name="targetSpeed"/>); a partial fire sheds pro-rata by pulses
    /// paid and coasts in the rest (the #262 warn-and-coast, but paid down as far as the tank reaches).
    /// </summary>
    /// <param name="PulsesSpent">Pulses taken from the tank — <c>min(quoted, tank)</c>, never negative.</param>
    /// <param name="ResultRelativeSpeed">The ship's post-brake speed relative to the target (m/s).</param>
    public readonly record struct FireResult(int PulsesSpent, double ResultRelativeSpeed);

    /// <summary>Resolve a brake fire (pure): shed <paramref name="currentRelativeSpeed"/> toward
    /// <paramref name="targetSpeed"/>, paying <c>min(quotedPulses, tankPulses)</c> and shedding pro-rata to
    /// the fraction of the bill the tank could cover. A tank that covers the whole bill reaches the window
    /// exactly; an empty tank sheds nothing and leaves the arrival as hot as it came in.</summary>
    public static FireResult FireBrake(
        double currentRelativeSpeed, double targetSpeed, int quotedPulses, int tankPulses)
    {
        // The shed is bounded by the excess over the window (0 when the arrival is already slow enough), so
        // the result never dips below the window and an already-in-window arrival is a true no-op.
        double fullShed = Math.Max(0.0, currentRelativeSpeed - targetSpeed);
        int spent = Math.Min(Math.Max(0, quotedPulses), Math.Max(0, tankPulses));
        double fraction = quotedPulses > 0 ? (double)spent / quotedPulses : 1.0;
        double applied = fullShed * Math.Clamp(fraction, 0.0, 1.0);
        return new FireResult(spent, currentRelativeSpeed - applied);
    }

    // ===== The one voice for the ask (HarborVocabulary-style; pure text, unit-tested) =====

    private static string Pulses(int p) => p.ToString(CultureInfo.InvariantCulture);

    /// <summary>The captain's-voice ASK for the propulsive arrival brake — the quoted bill, at the window.
    /// The owner's shape: "The arrival brake wants N pulses — fire?"</summary>
    public static string AskPropulsive(string destName, int pulses) =>
        $"🛬 the arrival brake at {destName} wants ≈{Pulses(pulses)} p to shed into the clamp window — fire?";

    /// <summary>The ASK when the tank won't cover the brake (the #262 unfunded warning, folded into the
    /// consent moment): fire what the tank holds and coast in the rest, or decline and shed by hand.</summary>
    public static string AskUnfunded(string destName, int pulses, int tankPulses) =>
        $"🛬 the arrival brake at {destName} wants ≈{Pulses(pulses)} p — the tank holds {Pulses(tankPulses)}; " +
        "fire what you can and coast in hot, or hold and shed by hand — fire?";

    /// <summary>The ASK for the aerobrake variant (#290/#301): ride the haze down instead of the propulsive
    /// brake. Same law — the pass is a deliberate act, so it asks before committing the ship to the air.</summary>
    public static string AskAerobrake(string destName, int passes, int pulsesSaved) =>
        $"🪂 ride the haze at {destName} — the aerobrake commits the ship to {Pulses(passes)} passes " +
        $"(≈{Pulses(pulsesSaved)} p saved) — commit the pass?";

    /// <summary>The receipt spoken when the brake fires — the pulses shed, into the window.</summary>
    public static string Fired(string destName, int pulsesSpent) =>
        $"🛬 arrival brake fired — {Pulses(pulsesSpent)} p shed at {destName}, into the clamp window.";

    /// <summary>The receipt spoken when the brake fired only partway (the tank ran out): the shed bought,
    /// and the honest note that the rest coasts in hot.</summary>
    public static string FiredHot(string destName, int pulsesSpent) =>
        $"🛬 arrival brake fired — {Pulses(pulsesSpent)} p was all the tank held; you'll coast in the rest hot at {destName}.";

    /// <summary>The receipt spoken on hold — the manual state stands, the captain has the ship, and the
    /// ask does not come back for this arrival.</summary>
    public static string Declined(string destName) =>
        $"🛬 brake held — you have the ship at {destName}; shed by hand into the clamp window when you're ready.";

    /// <summary>The receipt spoken when the captain commits the aerobrake pass.</summary>
    public static string AerobrakeCommitted(string destName) =>
        $"🪂 committed — riding the haze down at {destName}; watch the g and the gauge on the pass.";
}
