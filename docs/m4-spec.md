# M4 — Plotting mode: implementation work package

*Companion to the milestone entry in `SpaceSails_plan_detailed.md` §8 (M4). Build sheet for the
implementing helper. Builds directly on the M3 map page — extend it, do not fork it.*

## Goal (acceptance criteria, verbatim from the plan)

- Pause-map: time scrubber, future positions of all bodies ghosted, player path projected.
- Maneuver nodes: add/move/delete on own path; each node = burst of ± pulses; instant
  re-projection; plan persists and auto-executes back in real-time mode.
- Live override invalidates downstream nodes (visually: they turn red/dashed).
- **Accept:** plot Earth→Mars intercept entirely in map mode, press play, arrive within capture
  threshold (provisional for M4: within **1e9 m** of Mars) without touching controls.

## What exists (M3 — build on this)

- `Pages/Map.razor`: real `Simulator`-integrated ship (dt = 1 s behind an accumulator), live
  `+`/`−` pulses (1 s sim cooldown, reaction-mass budget 250), trajectory ribbon
  (`_projectionSimulator`, dt = 1 h, 30-day horizon, re-projected on pulse + every 6 h sim),
  warp auto-drop, HUD readouts, pulse feedback messages.
- `SpaceSails.Core`: `Simulator` (`Step(state, plan)`, `Run`, `Project`), `ManeuverPlan` /
  `ManeuverNode(SimTime, Action, Pulses)` (window-based firing via `ScaleFactorInWindow`),
  `ShipState`, `CircularOrbitEphemeris`, `Vector2d`.
- **New in this milestone (already implemented by the senior model — use, don't rewrite):**
  `Simulator.ProjectAdaptive(state, plan, horizonSeconds, options)` →
  `IReadOnlyList<TrajectorySample>` where `TrajectorySample(double SimTime, Vector2d Position)`.
  Adaptive dt = clamped fraction of the local dynamical time `min_bodies sqrt(d³/μ)`; steps land
  **exactly** on plan node times so a plotted burn and its live dt=1s execution agree to ~km.
  This is the "instant re-projection" workhorse: coarse in deep space, fine near bodies.

## Design (follow this)

### 1. Plotting mode = paused sim + scrub overlay
- A `Plot` toggle button in the toolbar (and `p` key alias). Entering plotting mode pauses the
  sim (`Paused = true`) and shows the plotting UI; leaving it ("Play") resumes the previous warp.
- **Scrubber:** Bootstrap `<input type="range" class="form-range">`, 0 → `PlotHorizonSeconds`
  (const, 60 days), value = offset from current `SimTime`. Label shows the absolute scrub time
  (`FormatSimTime`).
- At scrub time `T = SimTime + offset`, draw **ghosts**: every body at `ephemeris.Position(id, T)`
  as a dimmed circle (reuse `BodyColor` at ~35% alpha, no orbit ring re-draw needed), and a ghost
  ship marker at the projected path position at `T` (linear interpolation between the two
  bracketing `TrajectorySample`s).
- Plotting mode reuses the live canvas + rAF loop; it only changes what is drawn per frame.
  Camera pan/zoom stays fully live while plotting.

### 2. Plan model & editing
- Client keeps `List<ManeuverNode> _planNodes` (sorted by SimTime) plus a rebuilt
  `ManeuverPlan _plan` after every edit. `ManeuverPlan` is immutable — rebuild, don't mutate.
- **Add:** "Add burn at scrub time" button creates a node at `T` (rounded down to a whole
  second) with `Action = Accelerate, Pulses = 1`. Reject if `T <= SimTime` (past) with the
  existing pulse-message HUD flash.
- **Edit:** a Bootstrap list (cards or table rows) of nodes: sim time (read-only text), action
  toggle (± buttons), pulses stepper (`Pulses` 1–20, number input), "set to scrub time" button
  (re-times the node, keeps it sorted), delete button.
- **Mass budget:** planned total = Σ node.Pulses over non-stale future nodes. Reject any edit
  that would push planned total above `_reactionMassPulses` (HUD flash "Not enough reaction
  mass"). Live pulses and executed nodes decrement `_reactionMassPulses` as today.
- **Node markers on the path:** draw a small filled circle (accelerate: green `80,220,120`;
  decelerate: red-orange `240,120,80`) at each node's interpolated path position, on top of the
  ribbon. Stale nodes (see §4): grey `140,140,140` at 50% alpha.
- Click-to-add on the ribbon is a **stretch goal** (nearest-sample hit test on pointerdown while
  in plot mode, > 12 px rejects); the scrubber + button flow must work without it.

### 3. Projection & execution — one source of truth
- Replace the M3 ribbon call sites with `ProjectAdaptive(_ship, _plan, PlotHorizonSeconds, …)`;
  keep the samples (`IReadOnlyList<TrajectorySample>`) in a field — the scrubber ghost-ship, node
  markers, and the polyline all read from it. Re-project on: any plan edit, any live pulse, every
  6 h sim time (as in M3), and on entering plotting mode.
- **Auto-execution:** the live loop's `_simulator.Step(_ship, plan)` already fires node windows
  deterministically — pass `_executingPlan` (non-stale future nodes only) instead of `null`.
  After stepping, any node whose `SimTime` < `_ship.SimTime` just executed: decrement
  `_reactionMassPulses` by its `Pulses` (floor at 0) and flash the HUD ("Plan: N pulses fired").
  Track executed nodes by index/time so they fire the mass accounting once.
- Draw the ribbon in two styles: up to the last node = planned course (existing warm color);
  no plan = same as M3. Optional (cheap, do it): past-node segments slightly brighter so burns
  read as kinks.

### 4. Live override invalidates downstream nodes
- A **manual** `+`/`−` pulse while future nodes exist marks every node with
  `SimTime > _ship.SimTime` **stale**: they stop executing (excluded from `_executingPlan`),
  render grey/dashed in the node list (Bootstrap `text-decoration-line-through text-secondary`)
  and grey on the map, and the HUD flashes "Plan invalidated downstream".
- Stale nodes are kept until the player deletes them or re-times them via "set to scrub time"
  (re-timing un-stales). No auto-repair in M4.

### 5. HUD / layout
- Plotting UI lives in a Bootstrap card under the existing readouts (`.map-hud` column):
  scrubber + add button + node list. Keep it compact; `max-height` with `overflow-y: auto` on
  the node list. Existing readouts unchanged. Throttled `StateHasChanged` as today — but edits
  triggered by UI events may re-render immediately (they're user-paced).

## Constraints (working agreement §9)
- **Determinism is law in `SpaceSails.Core`** — `ProjectAdaptive` is already there with tests;
  do not add wall-clock, randomness, or platform-dependent math to Core.
- UI = Razor + Bootstrap only; custom CSS only for canvas/HUD; **no new JS**.
- Reuse scratch buffers for polylines (M3 pattern); no per-frame heap churn in the rAF path.
- Don't widen the milestone: no NPCs (M5), no capture mechanics (M6), no orbit-editing gizmos.

## Definition of done
- `dotnet build` clean (0 warnings), `dotnet test` green (Core tests for `ProjectAdaptive`
  already exist; add none unless you add Core code).
- Running the client: enter Plot, scrub to see ghosted bodies + ghost ship; add/edit/delete
  burn nodes with instant re-projection; press Play and watch the plan execute hands-off with
  mass accounting; a manual pulse greys out downstream nodes.
- The Earth→Mars accept run works end-to-end (the reviewer will fly it).
- PR notes: constants chosen (plot horizon, node pulse cap), any deviation from this spec.
