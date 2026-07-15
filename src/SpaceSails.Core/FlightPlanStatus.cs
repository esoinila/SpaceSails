namespace SpaceSails.Core;

// PR-D1 (docs/WednesdayPlan/UnifiedNavListNotes.md) — the read-only derivation behind the
// "flight plan" presentation of the burn list and the owner's NOW status ask (2026-07-15:
// "what the ship is doing is hard to know from the navigation UI. What step is it doing now
// -kind of status is missing"). This is the ONE source of truth for step states and the
// now/next line so the pilot banner, the Nav desk header, and the list never contradict each
// other. It is deliberately pure: no ship, no ephemeris, just the already-derived facts the UI
// hands it, so it is cheap to unit-test. No flight logic lives here — presentation only.

/// <summary>Visual state of a single flight-plan step.</summary>
public enum FlightStepState
{
    /// <summary>Sitting in the plan, its time still ahead.</summary>
    Planned,

    /// <summary>Armed and waiting for its window (auto-insertion the autopilot will fly).</summary>
    Armed,

    /// <summary>Being flown RIGHT NOW (the autopilot is on the approach).</summary>
    Active,

    /// <summary>Already flown.</summary>
    Done,

    /// <summary>Invalidated by a later edit — struck through, will not fire as written.</summary>
    Stale,
}

/// <summary>The compact now/next readout for the Nav desk and pilot banner.</summary>
/// <param name="NowLine">Always present: what the ship is doing this instant.</param>
/// <param name="NextLine">The next pending step, or null when nothing is queued.</param>
public readonly record struct FlightPlanStatus(string NowLine, string? NextLine);

/// <summary>Inputs for <see cref="FlightPlanStatusBuilder"/> — all already derived by the caller
/// from the same state the burn list and pilot banner read.</summary>
/// <param name="Docked">The ship is clamped on at a haven (nav is locked).</param>
/// <param name="DockedHavenName">Name of that haven, for the NOW line.</param>
/// <param name="AutopilotArmed">Auto-insertion is armed for some body.</param>
/// <param name="AutopilotFlyingApproach">The autopilot is actively flying the approach now
/// (armed AND within capture range).</param>
/// <param name="AutopilotBodyName">The body the autopilot is bound for.</param>
/// <param name="NextStepLabel">Pre-built label of the next pending step, e.g. "burn ▲ 14 p"
/// or "insertion at Titan"; null when nothing is pending.</param>
/// <param name="NextStepEta">Pre-built ETA for that step, e.g. "in 2d 4h" or "at window";
/// null/empty when unknown.</param>
/// <param name="HandbackReason">Set when the autopilot has LOUDLY handed the ship back (fuel
/// plan broken by an external burn, watchdog stand-down, docked). It is the persistent NOW line
/// — the fix for #147, where a disarm was only a 1.5-s toast, invisible at warp — and the single
/// source of truth every desk chip reads, so nothing can claim a mission the autopilot no longer
/// flies. Cleared once the captain arms again or changes course. Ignored while armed (the ship is
/// flying again) or docked (the dock line wins).</param>
public readonly record struct FlightPlanInputs(
    bool Docked,
    string? DockedHavenName,
    bool AutopilotArmed,
    bool AutopilotFlyingApproach,
    string? AutopilotBodyName,
    string? NextStepLabel,
    string? NextStepEta,
    string? HandbackReason = null);

/// <summary>Derives step states and the now/next readout — the single source of truth so the
/// banner, the Nav header, and the list stay coherent.</summary>
public static class FlightPlanStatusBuilder
{
    /// <summary>State of a burn step: an edit can strike it out; once its time passes it is done;
    /// otherwise it is simply planned. (A burn fires instantly, so it is never "active".)</summary>
    public static FlightStepState BurnState(bool stale, bool executed) =>
        stale ? FlightStepState.Stale
        : executed ? FlightStepState.Done
        : FlightStepState.Planned;

    /// <summary>State of the armed auto-insertion step: Active while the autopilot is flying the
    /// approach, Armed while it waits for the window.</summary>
    public static FlightStepState InsertionState(bool flyingApproach) =>
        flyingApproach ? FlightStepState.Active : FlightStepState.Armed;

    /// <summary>Builds the NOW / next lines. NOW answers "what is the ship doing this instant",
    /// next names the step queued after it.</summary>
    public static FlightPlanStatus Build(FlightPlanInputs f)
    {
        string now =
            f.Docked ? $"NOW: docked at {NameOr(f.DockedHavenName, "haven")}"
            : f.AutopilotArmed && f.AutopilotFlyingApproach
                ? $"NOW: autopilot approach → {NameOr(f.AutopilotBodyName, "target")}"
            : f.AutopilotArmed
                ? $"NOW: coasting — autopilot armed for {NameOr(f.AutopilotBodyName, "target")}"
            // The autopilot handed the ship back (#147): a persistent NOW line — not a toast that
            // vanishes at warp — so every desk reads "you have the ship" and why.
            : !string.IsNullOrWhiteSpace(f.HandbackReason)
                ? $"NOW: manual — {f.HandbackReason}"
            : "NOW: coasting";

        // The audit's round-2 cold read (docs/MondayPonder/UIUsabilityNotes.md): testers did not read
        // this line as "what the ship does next". An uppercase NEXT: label makes it answer that question
        // at a glance, matching the NOW: line's shape so the two read as one now/next unit.
        string? next = null;
        if (!string.IsNullOrWhiteSpace(f.NextStepLabel))
        {
            next = string.IsNullOrWhiteSpace(f.NextStepEta)
                ? $"NEXT: {f.NextStepLabel}"
                : $"NEXT: {f.NextStepLabel} {f.NextStepEta}";
        }

        return new FlightPlanStatus(now, next);
    }

    private static string NameOr(string? name, string fallback) =>
        string.IsNullOrWhiteSpace(name) ? fallback : name;
}
