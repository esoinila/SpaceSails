# Sunday Plan — PR breakdown

*Derived from [SundayPlanVision.md](SundayPlanVision.md), 2026-07-05. Goal: the fire-control
feature set as a reviewable chain of PRs, all opened overnight so the owner can approve them
in order over morning coffee. The implementer stays on call to rebase and merge each one as
it's approved.*

**Status: ALL MERGED (owner approval, 2026-07-05 morning).** PR-0 #54 · PR-A #55 ·
PR-B #56→#60 · PR-C #57 · PR-D #58 · PR-E #59, merged to main in that order. (PR-E had
moved from "independent, off main" onto the chain tip so the parrot's star squawk —
"FIRING SOLUTION, CAPTAIN!" — could wire to PR-C's lock event directly.)

**Merge-train lesson for next time:** with squash merges, deleting a merged PR's head
branch auto-CLOSES any open PR based on it, and a closed PR can be neither reopened after
a force-push nor retargeted — #56 died this way and was resurrected as #60. The procedure
that works: retarget the child PR's base to `main` FIRST, then merge the parent (deleting
its branch), then `git rebase --onto main <original-parent-commit> <child-branch>` (the
ORIGINAL commit, not the rebased ref) and force-push.

## The vision, distilled into features

| # | Feature | Lane |
|---|---------|------|
| F1 | **Firing solution (Core)** — shooting-method Newton on the Simulator; iteration trace; validity window; dispersion from track-cone quality | PR-A |
| —  | **PathPredictor post-burn cone honesty** (Lab 08 fix) — a prey that burns must break locks *by the model* | PR-A |
| —  | **Fast-graze closest-approach refine** (Lab 06 fix) — required for honest hit resolution | PR-A |
| F3 | **Slugs & missiles in the sim** — ballistic, self-evaporating; missile = tiny correction budget; hits feed the news wire | PR-B |
| F2 | **Gun deck Norden UI** — pick the point on the prey's predicted track, CALCULATING…, solution locks T-60s, auto-slew, fire, return control | PR-C |
| F6 | **Piloting tips from firing solutions** — the solver's output as flight instruction | PR-C |
| F4 | **The Ancients' pilot** — pyramid-satellite encounter grants scarce auto-plot charges | PR-D |
| —  | **PR-16 the ship's parrot 🦜** (carried backlog) — deterministic squawks; "FIRING SOLUTION, CAPTAIN!" | PR-E |
| F5 | **Lab Lesson 13 — "Shooting, literally"** — follows once the dust settles (every number from a live probe run) | follow-up |

Carried-backlog note: the **Nav scope inset going blank after visiting Sensors** was fixed in
PR #52 (the canvas now survives desk switches) — dropped from this plan.

Open question for the owner (morning): **paper-draft venue** — (a) SIGGRAPH-style real-time
sim, (b) games/education "secretly edutainment" method, (c) experience report (human PO + AI
head coder). Draft starts in `docs/paper/` once chosen; not blocked on this plan.

## Why a stacked chain instead of Saturday's parallel lanes

Saturday's lanes were parallel because they were *different subsystems*. Sunday's features
are one subsystem in layers: the UI (C) fires the entities (B) computed by the solver (A),
and the power-up (D) reuses the same solver. Sequential branches mean every PR reviews clean
against its base, and the owner approves top-down without merge surprises. PR-E (parrot) is
genuinely independent and branches off main.

## The PRs

### PR-0 — this plan (tiny, vs main, first)
This document. Everything else assumes the merge order above.

### PR-A · 🎯 Core fire control + the two Lab-found fixes *(F1)* — branch `sunday/pr-a`
- `Core/FireControl.cs`: `Solve(shooter, muzzleSpeed, targetPos, tHit, simulator)` →
  launch direction + expected miss + full iteration trace (the UI shows convergence).
  Shooting method: Newton/secant over launch bearing, each residual evaluated by running
  the real deterministic `Simulator` — the Gravity Lab's method, weaponized.
- Solution validity window + dispersion derived from the target track's cone quality.
- **Lab 08 fix**: post-burn cone honesty in `PathPredictor` — the cone must grow from the
  last *observation*, honestly covering a target that burned since.
- **Lab 06 fix**: fast-graze closest-approach — dense refine so a slug (or ship) crossing
  a target between samples cannot tunnel through the check.
- Core + tests only; zero client changes. Determinism is law.

### PR-B · 🔫 Slugs & missiles + hit resolution + news *(F3)* — branch `sunday/pr-b` (on A)
- Ordnance entities in the live sim: ballistic slug (self-evaporating after TTL), missile
  variant with a small homing-correction budget.
- Hit resolution by PR-A's dense closest-approach refine (no integrator tunneling).
- A hit disables the target's sail (it drifts, boardable); outcomes feed the news wire.
- Core ordnance rules + Map.razor stepping/drawing; no gun-deck UI yet (test hook only).

### PR-C · 🎖 The Norden moment: gun deck UI + piloting tips *(F2 + F6)* — branch `sunday/pr-c` (on B)
- War room: select a tracked target → scrub along its predicted path → pick the intercept
  point → CALCULATING FIRING SOLUTION (iterations visibly converging) → solution locks at
  T-60s → ship auto-slews (cosmetic on the map), fires, returns control.
- Warning shots become real slugs across the bow (`EncounterRule` reaction unchanged).
- Piloting tips: each computed solution renders a "to fly this yourself…" line — the
  mechanic teaches by worked example (secretly edutainment).

### PR-D · 👁‍🗨 The Ancients' pilot *(F4)* — branch `sunday/pr-d` (on C)
- Pyramid satellites: off-board ancients' hardware — no departures entry, sensor behavior
  that doesn't match `SensorModel`. Approach one and it grants a few **auto-plot charges**.
- Spending a charge fills the maneuver plan for the current destination automatically (the
  same solver family, offered as scarce alien assistance). Design rule: charges are rare;
  manual flight stays the skill the game teaches.

### PR-E · 🦜 The ship's parrot *(carried PR-16)* — branch `sunday/pr-e` (on main, independent)
- Deterministic squawks reacting to ship events (boarding, insertion, firing solutions —
  "FIRING SOLUTION, CAPTAIN!"); a fixed line table indexed by event + deterministic counter,
  no randomness. LLM-backed stage 2 stays future work.

## Working agreement unchanged

Determinism is law in Core; UI = Razor + Bootstrap; JS only in renderer.js; every lab number
from a real probe run; senior reviews/verifies; owner approves PRs.
