using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

// #166 — the ship's one alert channel. The playtest ask (owner, 2026-07-16): "there should be some
// kind of ship wide alert (and parrot squawk) when there is an imminent collision course or running
// out of fuel". Built once, not per-symptom: a single edge-triggered, acknowledgeable channel that
// the banner strip, the desk chips, the ledger, and the 🦜 parrot all read (the #147 single-source
// ruling, applied to alarms). The three founding conditions — collision, fuel, orbit-decay — raise
// and clear here; every reader derives its shout, its colour, and its silence from this one place.

/// <summary>Alert urgency. Amber = a warning you should act on; Red = act now. The numeric order
/// lets the banner pick the top colour and lets a fuel amber→red crossing register as an escalation.</summary>
public enum AlertSeverity
{
    Amber = 1,
    Red = 2,
}

/// <summary>The founding alert conditions (#166). Each maps to at most one live alert at a time —
/// a condition is either raised or clear, so re-arming is exact.</summary>
public enum AlertKind
{
    /// <summary>The current course has a ballistic impact / sub-surface pass in the horizon
    /// ("ROCKS AHEAD!").</summary>
    Collision,

    /// <summary>Fuel below the 18% autopilot reserve (amber) or the reach-a-pump floor (red).</summary>
    Fuel,

    /// <summary>A bound orbit drifting out of the tide-stable band (#180/#183) — migrated in as the
    /// third founding alert so the degradation warning speaks with the same voice as the rest.</summary>
    OrbitDegrade,

    /// <summary>#205 — a plunder window is open on a hull the captain has not yet acted on: the first
    /// ACTIONABLE alert. The captain's word to board (or stand down) is required from ANY desk, so the
    /// opportunity rides this channel with an <see cref="ShipAlert.ActionTargetId"/> the banner wires
    /// its approve/stand-down chips to. Shot authorization and boarding authorization are separate
    /// consents; this alert carries the boarding one.</summary>
    Boarding,

    /// <summary>#266 — the tank is empty and no pump is in reach: the ship is ADRIFT. A founding "we're
    /// stranded" condition that rides this one channel like the rest, so the banner says the state, the
    /// ledger logs it, and the parrot squawks the rescue proposal once per crossing. The rescue itself
    /// is a pop-up gate (the terms must be visible before accepting), not a banner button — this alert
    /// only SHOUTS and says-the-state; #236 keeps the action off the masthead.</summary>
    Adrift,
}

/// <summary>One live alert. Immutable snapshot; the channel replaces it wholesale on any change.</summary>
/// <param name="Kind">Which condition this is.</param>
/// <param name="Severity">Amber or Red.</param>
/// <param name="Message">The human line the banner and ledger show.</param>
/// <param name="FirstRaisedSeconds">Sim time of the rising edge (reset on an escalating re-crossing).</param>
/// <param name="Acknowledged">The captain silenced this instance; it lingers dimmed but no longer shouts.</param>
/// <param name="ActionTargetId">#205 — an optional subject the alert's actions act on (the hull id
/// for a <see cref="AlertKind.Boarding"/> opportunity). Null for the plain shout-only alerts, whose
/// only action is silencing; non-null turns the alert ACTIONABLE — the banner renders approve/
/// stand-down chips wired to this id. Carrying data (not a delegate) keeps the channel Core-pure.</param>
public readonly record struct ShipAlert(
    AlertKind Kind, AlertSeverity Severity, string Message, double FirstRaisedSeconds, bool Acknowledged,
    string? ActionTargetId = null);

/// <summary>
/// The ship-wide alert channel (#166). Edge-triggered: <see cref="Raise"/> fires (returns true) only
/// on a rising edge — a condition BECOMING active, or escalating amber→red — never per-tick while it
/// holds, so the banner shouts and the parrot squawks once per crossing, not forever. <see cref="Clear"/>
/// re-arms the edge so the next onset fires again. <see cref="Acknowledge"/> silences one instance while
/// its condition persists; it stays present (a dimmed chip) and a NEW crossing un-silences it.
/// Deterministic from sim state — the caller evaluates conditions each tick and raises/clears; the
/// channel owns nothing but the edges and the acknowledgement bits.
/// </summary>
public sealed class ShipAlerts
{
    private readonly Dictionary<AlertKind, ShipAlert> _active = new();

    /// <summary>Raise or refresh a condition. Returns true on a rising edge — the alert was not active,
    /// OR it escalated to a higher severity (a new crossing, e.g. fuel amber→red) — the caller's cue to
    /// squawk, log, and drop warp. A same-or-lower-severity re-raise updates the message text in place
    /// and returns false (no re-fire, no re-shout while the condition merely persists).</summary>
    public bool Raise(AlertKind kind, AlertSeverity severity, string message, double nowSeconds,
        string? actionTargetId = null)
    {
        if (_active.TryGetValue(kind, out ShipAlert existing))
        {
            bool escalated = severity > existing.Severity;
            _active[kind] = existing with
            {
                Severity = severity > existing.Severity ? severity : existing.Severity,
                Message = message,
                // The action payload rides with the text: a refresh may re-point the same live alert at
                // a new subject (a different hull comes on offer) without being a fresh edge.
                ActionTargetId = actionTargetId,
                // An escalating crossing is a fresh edge: it un-silences and re-stamps the raise time.
                Acknowledged = escalated ? false : existing.Acknowledged,
                FirstRaisedSeconds = escalated ? nowSeconds : existing.FirstRaisedSeconds,
            };
            return escalated;
        }

        _active[kind] = new ShipAlert(kind, severity, message, nowSeconds, Acknowledged: false, ActionTargetId: actionTargetId);
        return true;
    }

    /// <summary>Clear a condition (falling edge). Returns true if it was active — the caller's cue to
    /// log "cleared". Re-arms the edge so a later <see cref="Raise"/> of the same kind fires again.</summary>
    public bool Clear(AlertKind kind) => _active.Remove(kind);

    /// <summary>Silence this alert's shout while its condition persists — it stays present (a dimmed
    /// chip) but no longer counts as unacknowledged. Returns true if there was a live, un-silenced
    /// alert to quiet. A subsequent escalation or a clear+raise un-silences it (a new crossing shouts).</summary>
    public bool Acknowledge(AlertKind kind)
    {
        if (_active.TryGetValue(kind, out ShipAlert a) && !a.Acknowledged)
        {
            _active[kind] = a with { Acknowledged = true };
            return true;
        }

        return false;
    }

    /// <summary>The live alert for a kind, or null when that condition is clear.</summary>
    public ShipAlert? Get(AlertKind kind) => _active.TryGetValue(kind, out ShipAlert a) ? a : null;

    /// <summary>Is this condition currently raised (acknowledged or not)?</summary>
    public bool IsActive(AlertKind kind) => _active.ContainsKey(kind);

    /// <summary>All live alerts (any acknowledgement state) — the ledger / chip readers.</summary>
    public IEnumerable<ShipAlert> Active => _active.Values;

    /// <summary>Any alert at all is live.</summary>
    public bool Any => _active.Count > 0;

    /// <summary>Any live alert is still shouting (not yet acknowledged) — the banner-strip gate.</summary>
    public bool AnyUnacknowledged => _active.Values.Any(a => !a.Acknowledged);

    /// <summary>The highest severity among the still-shouting alerts, or null when all are quiet —
    /// the banner strip's colour and whether to pulse at all.</summary>
    public AlertSeverity? TopUnacknowledgedSeverity
    {
        get
        {
            AlertSeverity? top = null;
            foreach (ShipAlert a in _active.Values)
            {
                if (!a.Acknowledged && (top is null || a.Severity > top))
                {
                    top = a.Severity;
                }
            }

            return top;
        }
    }
}

/// <summary>#166/#157 — the fuel alarm's thresholds, as a pure rule the channel driver reads. Amber
/// at the 18% autopilot reserve (the same <see cref="AutopilotRehearsal.ReserveFraction"/> the flight
/// planner keeps back); red when the tank can no longer reach a pump.</summary>
public static class FuelAlertRule
{
    /// <summary>Amber onset: the tank has fallen to the autopilot's reserve fraction (18%).</summary>
    public const double AmberReserveFraction = AutopilotRehearsal.ReserveFraction;

    /// <summary>Red onset. TODO(#157): the honest red is "can't reach the nearest pump", priced with
    /// the transfer kernels against the current position and the depot map. Until that reach-a-pump
    /// math exists, red fires at a conservative fixed floor of the tank — a clearly marked seam so the
    /// measured threshold drops in here without touching the channel or the banner.</summary>
    public const double RedFloorFraction = 0.05;

    /// <summary>The alarm severity for the current tank, or null when there is fuel to spare. Red wins
    /// over amber. A non-positive capacity is treated as "no reading" (null).</summary>
    public static AlertSeverity? Evaluate(int pulses, int capacity)
    {
        if (capacity <= 0)
        {
            return null;
        }

        double fraction = (double)pulses / capacity;
        if (fraction <= RedFloorFraction)
        {
            return AlertSeverity.Red;
        }

        if (fraction <= AmberReserveFraction)
        {
            return AlertSeverity.Amber;
        }

        return null;
    }
}
