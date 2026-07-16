using System;
using System.Collections.Generic;

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

/// <summary>Which slot a banner row occupies. NOW is the pinned first line (what the ship is doing
/// this instant); NEXT is the immediate next step; LATER rows are the queue beyond it, reachable by
/// the banner's up/down arrows (#159/#184).</summary>
public enum FlightRowKind
{
    /// <summary>The pinned first line — what the ship is doing right now (always the active row).</summary>
    Now,

    /// <summary>The immediate next step — the owner's "second line" (#184).</summary>
    Next,

    /// <summary>A queued step beyond NEXT — paged into view by the ▲▼ arrows.</summary>
    Later,
}

/// <summary>One rendered line of the multi-row pilot banner (#159/#184). The banner is the ONLY
/// place the ship's plan is spoken, so a row carries everything the strip needs: its slot, the
/// already-composed human text, the step's visual state, and whether it is the active step the
/// banner auto-scrolls to keep in view.</summary>
/// <param name="Kind">NOW / NEXT / LATER slot.</param>
/// <param name="Text">The composed line, e.g. "NOW: approaching Enceladus — autopilot flying".</param>
/// <param name="State">The step's visual state (Active for NOW, Armed/Planned for queued steps).</param>
/// <param name="IsActive">The row the banner scrolls to keep visible — the step being flown now.</param>
public readonly record struct FlightPlanRow(
    FlightRowKind Kind, string Text, FlightStepState State, bool IsActive);

/// <summary>A single queued step the banner names below NOW — a pending burn, an approach, an
/// orbit-insert. The caller composes the human label and ETA from the same facts the burn list
/// reads; the builder only decides its slot (NEXT vs LATER) and stitches the row.</summary>
/// <param name="Label">Plain-language step, e.g. "orbit-insert at Enceladus (313 km)".</param>
/// <param name="Eta">Pre-built ETA, e.g. "in 2 h" or "at window"; null/empty when unknown.</param>
/// <param name="State">The step's visual state (Armed for the pending insertion, Planned for a burn).</param>
public readonly record struct FlightPlanStep(string Label, string? Eta = null, FlightStepState State = FlightStepState.Planned);

/// <summary>The compact now/next readout for the Nav desk and pilot banner.</summary>
/// <param name="NowLine">Always present: what the ship is doing this instant.</param>
/// <param name="NextLine">The next pending step, or null when nothing is queued.</param>
public readonly record struct FlightPlanStatus(string NowLine, string? NextLine)
{
    /// <summary>The full ordered row list for the multi-row pilot banner (#159/#184). Row 0 is the
    /// pinned NOW line; row 1 (when present) is NEXT — the owner's second line; further rows are the
    /// LATER queue the ▲▼ arrows page through. <see cref="NowLine"/>/<see cref="NextLine"/> stay the
    /// first two rows' text so single-line readouts (desk chips, the Nav header) keep working
    /// unchanged. Never null — empty only in degenerate builds.</summary>
    public IReadOnlyList<FlightPlanRow> Rows { get; init; } = Array.Empty<FlightPlanRow>();
}

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
/// <param name="AutopilotInserting">The autopilot is performing the orbit-insertion burn this
/// instant (the window is open and it is circularizing). Outranks the plain approach so the NOW
/// line says "inserting into orbit", closing the #171/#173 "will it orbit or crash?" doubt.</param>
/// <param name="HoldingLine">COORDINATION SEAM (Friday §0 / owner ruling, priority lane): when the
/// armed autopilot has settled into a KEPT orbit it station-keeps, and that lane owns the verbatim
/// NOW line — "🛰 AUTOPILOT HOLDS THE ORBIT — Enceladus, 313 km, trim ≈N p/day". This builder only
/// slots it in as the NOW row (below Docked, above every flying phase). Null until that lane sets
/// it; this owner only guarantees the layout, not the text.</param>
/// <param name="UpcomingSteps">The ordered queue of steps to name below NOW — pending burns, then
/// the approach/insertion — already composed by the caller. When null the builder falls back to the
/// single <see cref="NextStepLabel"/>/<see cref="NextStepEta"/> pair (back-compat). The first entry
/// becomes NEXT; the rest are LATER rows the banner's arrows page through.</param>
public readonly record struct FlightPlanInputs(
    bool Docked,
    string? DockedHavenName,
    bool AutopilotArmed,
    bool AutopilotFlyingApproach,
    string? AutopilotBodyName,
    string? NextStepLabel,
    string? NextStepEta,
    string? HandbackReason = null,
    bool AutopilotInserting = false,
    string? HoldingLine = null,
    IReadOnlyList<FlightPlanStep>? UpcomingSteps = null);

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

    /// <summary>Builds the NOW / next lines AND the full banner row list. NOW answers "what is the
    /// ship doing this instant"; the queue below it names each step still ahead, top to bottom, so
    /// the pilot banner can speak the whole plan (#159/#184) — the approach and the orbit-insert as
    /// SEPARATE, plain-language rows (#171/#173: no more "step 1/1" with the crash-or-orbit doubt).
    /// The NOW line's phase priority, highest first: docked (nav locked) → holding a kept orbit
    /// (priority lane) → inserting → approaching → coasting the transfer arc → manual handback →
    /// plain coasting.</summary>
    public static FlightPlanStatus Build(FlightPlanInputs f)
    {
        string body = NameOr(f.AutopilotBodyName, "target");
        string now =
            f.Docked ? $"NOW: docked at {NameOr(f.DockedHavenName, "haven")}"
            // The kept-orbit line is owned verbatim by the station-keeping lane (Friday §0). It is a
            // NOW state that outranks the flying phases: the ship has arrived and is holding.
            : !string.IsNullOrWhiteSpace(f.HoldingLine)
                ? f.HoldingLine!
            : f.AutopilotArmed && f.AutopilotInserting
                ? $"NOW: inserting into orbit at {body}"
            : f.AutopilotArmed && f.AutopilotFlyingApproach
                ? $"NOW: approaching {body} — autopilot flying"
            : f.AutopilotArmed
                ? $"NOW: coasting the transfer arc to {body}"
            // The autopilot handed the ship back (#147): a persistent NOW line — not a toast that
            // vanishes at warp — so every desk reads "you have the ship" and why.
            : !string.IsNullOrWhiteSpace(f.HandbackReason)
                ? $"NOW: manual — {f.HandbackReason}"
            : "NOW: coasting";

        // The queue below NOW. Prefer the caller's ordered step list; fall back to the single
        // legacy NextStepLabel/Eta pair so existing callers and desk chips are unaffected.
        IReadOnlyList<FlightPlanStep> queue = f.UpcomingSteps ?? SingleStep(f.NextStepLabel, f.NextStepEta);

        var rows = new List<FlightPlanRow>(1 + queue.Count)
        {
            // NOW is always the active row — the step being flown, the one the banner keeps in view.
            new(FlightRowKind.Now, now, FlightStepState.Active, IsActive: true),
        };

        string? nextLine = null;
        for (int i = 0; i < queue.Count; i++)
        {
            FlightPlanStep step = queue[i];
            if (string.IsNullOrWhiteSpace(step.Label))
            {
                continue;
            }

            // The audit's round-2 cold read (docs/MondayPonder/UIUsabilityNotes.md): testers did not
            // read the old "next: …" as "what the ship does next". Uppercase NEXT:/THEN: cues that
            // mirror the NOW: line, so who-is-flying + what-next read as one unit.
            bool isNext = rows.Count == 1; // first real step below NOW
            string prefix = isNext ? "NEXT" : "THEN";
            string text = string.IsNullOrWhiteSpace(step.Eta)
                ? $"{prefix}: {step.Label}"
                : $"{prefix}: {step.Label} {step.Eta}";
            rows.Add(new FlightPlanRow(isNext ? FlightRowKind.Next : FlightRowKind.Later, text, step.State, IsActive: false));
            nextLine ??= text;
        }

        return new FlightPlanStatus(now, nextLine) { Rows = rows };
    }

    private static IReadOnlyList<FlightPlanStep> SingleStep(string? label, string? eta) =>
        string.IsNullOrWhiteSpace(label)
            ? Array.Empty<FlightPlanStep>()
            : new[] { new FlightPlanStep(label!, eta, FlightStepState.Planned) };

    private static string NameOr(string? name, string fallback) =>
        string.IsNullOrWhiteSpace(name) ? fallback : name;
}
