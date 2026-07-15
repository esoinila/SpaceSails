# Gemini second opinion — the unified flight plan (2026-07-15 evening)

*Provenance: the true Gemini CLI pass over UnifiedNavListNotes.md, run right after the owner
completed the CLI's browser login (it had been auth-hung all day). Full reply below, verbatim
in substance, lightly grouped; triage verdicts by Fable at the end.*

## 1. Missing elements / implementation bites

- **Editing mid-plan steps is unspecified.** Tweak burn #2 of 10 — do steps 3–10 re-solve or
  just go `stale`? Auto re-solve is expensive and jarring; the spec must pick a rule.
- **Immediate vs. planned dock/undock needs its UI choice spelled out** — e.g. a "Plan this
  action" checkbox on the immediate-action control that files it as a step instead.
- **Glanceability of the collapsed list**: the "next step" line must carry type + target +
  countdown ("burn → Titan · in 2d 4h"), not just a label.
- **Failed drag re-order**: snap-back alone is jarring — pair it with a non-modal toast
  ("Cannot re-order past the Titan gravity assist") and a brief red flash on the item.
- **Selection hierarchy**: clicking a node on the map vs. in the list must resolve to ONE
  selected object feeding the context card — a single source of truth for selection.

## 2. State-model additions

- **`failed`** — executed but didn't achieve its goal (bad insertion, fuel ran out mid-burn).
  Distinct from `stale` (still-valid plan whose premises moved).
- **`skipped`** — user manually bypassed a step (user-initiated, not physics).
- **`aborted`** — an `active` step cancelled mid-execution.
- **`conditional`** — future: "dock with X only if the scan comes back clean."
- **Make explicit: only ONE step is `armed` at a time** (`planned → armed` is exclusive).

## 3. Better-than-graying-out for waiting steps

- Indent waiting steps under their open-ended parent; a bracket/line showing the dependency.
- The "⏳ waits on: Ambush" marker as a hover/click target that highlights the parent step in
  the list AND on the map.
- Optional mini-timeline: the open-ended block gets a fuzzy/dashed end with dependents attached.
- Keep waiting steps expandable — show their intended parameters with a "timing is provisional,
  recomputed after 'Ambush' resolves" warning, so intent stays reviewable.

## 4. Patterns worth stealing

- **KSP maneuver nodes**: every flight-plan step is a clickable NODE on the plotted ribbon —
  the list and the map are two views of one plan.
- **Airliner FMS scratchpad**: build/edit a step in a scratchpad line, preview the projected
  result, then COMMIT it into the plan — less error-prone than editing the live plan.
- **DCS kneeboard**: the flight plan is the Nav desk's primary page, not a HUD sticker.
- **Kanban/what-if (future)**: a standby "alternative plan" the player can arrange and swap in.

## 5. Blazor + CSS specifics

- CSS Grid for the HUD (`"top-banner top-banner top-banner" / "flight-plan-rail map
  context-rail"`) — kills z-index chaos structurally.
- Flight-plan rail: flex column, inner `overflow-y: auto` + max-height; Blazor `<Virtualize>`
  if plans exceed ~20 steps.
- Accordion: animate `max-height` (not `height`), lift the expanded editor with background +
  shadow.
- Step states as `data-state` attributes styled via CSS custom properties
  (`.flight-step[data-state="active"] { border-left: 3px solid var(--color-state-active); }`).

## Fable's triage (for the D2/D3 lanes)

**Accept into D2 (step editors):** scratchpad-commit pattern; selection single-source-of-truth;
collapsed-line glanceability; "Plan this action" checkbox for dock/undock; CSS grid + accordion
+ data-state styling; steps as clickable map nodes (KSP linkage) if cheap.
**Accept into D3 (open-ended steps):** `failed`/`skipped`/`aborted` states; explicit
one-armed-step rule; dependency indent + provisional-parameters view for waiting steps;
edit-mid-plan rule = downstream goes `stale` + an explicit "re-solve times" button (never auto).
**Defer:** `conditional` steps, kanban what-if plans, mini-timeline view.
