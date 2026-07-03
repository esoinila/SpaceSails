# M6 — Piracy vertical slice 🏴‍☠️: implementation work package

*Companion to `SpaceSails_plan_detailed.md` §8 (M6). Build sheet for the implementing helper.
Builds on the M5 map page — extend it, do not fork it. **This is the go/no-go milestone**: the
full loop must be playable and fun-ish.*

## Goal (acceptance criteria, verbatim from the plan)

- Capture rule (rel-velocity + distance window for N seconds), boarding resolution (simple
  first: automatic success, cargo transferred), fence/upgrade screen (reaction mass, sensor
  range, cargo hold).
- Failure states: out of reaction mass (drift — rescue fee = lose cargo), missed windows.
- Single scripted "first hunt" tutorial voyage.
- **Accept:** full loop — pick target, plot, chase, capture, sell, upgrade — is playable.

## Worldbuilding hooks landing in this milestone (docs/worldbuilding-notes.md)

- **Mass-driver pods**: Luna's factories launch ballistic compute-core pods (mass driver =
  all Δv at launch; no engine). Zero maneuver budget ⇒ their prediction cone stays
  needle-thin — the tutorial prey. Luna is now in `sol.json` (orbits Earth).
- Cargo classes get market values; He3 is the prize, pods are the milk run.

## What exists (M5 — build on this)

- `Pages/Map.razor`: ship loop, plotting mode, ribbon, warp auto-drop, HUD, traffic board,
  sensor sweeps, NPC lockstep stepping (`_npcSimulator`, dt=`TrafficSchedule.NpcTimeStep`),
  prediction cone + pin.
- Core: everything from M1–M5.
- **New in this milestone, senior-implemented (use, don't rewrite):**
  - `CaptureRule` — `IsInWindow(playerState, targetState)` (distance ≤ `CaptureRadiusMeters`
    = 1e8, rel speed ≤ `MaxRelativeSpeed` = 2000 m/s) + `RequiredSeconds` = 60. The client
    accumulates window time; Core owns the constants and the pure predicate.
  - `CargoMarket` — `UnitValue(cargoClass)`: He3 1200, Compute cores 400, Alloys 300,
    Machinery 250, Ice 100 credits/unit.
  - `NpcShip.CargoUnits` (seeded 5–20; pods 4) and `NpcShip.ManeuverBudget` (0.3 m/s²;
    **0 for pods**). `PredictedPath` now takes the budget — pods' cones stay hairline.
  - `TrafficSchedule` now also emits **pods**: `IsPod`, callsign `Pod-N`, origin `luna`,
    cargo `Compute cores`, `Plan = ManeuverPlan.Empty` — the launch burn is folded into
    `InitialState` (mass-driver fiction), so they are pure ballistics.

## Client work (extend Map.razor; Razor + Bootstrap only, no new JS)

### 1. Player economy state
- Fields: `_credits` (start 0), `_cargoUnits` + `_cargoValue` (aggregate; hold starts at
  capacity 10), upgrade levels for **Reaction mass capacity** (base 250, +150/level),
  **Sensor range** (base = `SensorModel.Default.RangeMeters`, ×1.4/level), **Cargo hold**
  (base 10, +10/level). Prices: 2000 × 2^level credits each track.
- Sensor upgrades mean the sweep must use a `SensorModel` built from the current range (same
  glare constants), not `SensorModel.Default` directly.
- HUD readouts add: credits, cargo (units used / capacity).

### 2. Capture flow
- When a **selected, currently-observed** target is within `CaptureRule` window, show a
  boarding progress bar (Bootstrap `progress`) near the readouts: "Boarding <callsign> —
  NN s". Accumulate **sim-time** while `CaptureRule.IsInWindow(player, target)` holds each
  frame; reset to 0 when it breaks. Auto-drop warp to ≤10× while the window is engaged
  (reuse the effective-warp mechanism — a capture is a close encounter by definition).
- At `RequiredSeconds`: boarding succeeds — transfer `min(target.CargoUnits, hold space)`
  units at `CargoMarket.UnitValue(cargo)` each into the hold, mark the NPC `Boarded`
  (board status "Boarded", stops being steppable prey: keep it flying but empty — a second
  boarding yields nothing), HUD flash "Captured N units of <cargo>".
- Missed window = nothing special: the bar just resets. (The *strategic* miss — target
  arrives and despawns — already exists.)

### 3. Fence / dock
- "Dock" button in the toolbar, enabled only when within `DockRadiusMeters` = 1e9 of a body
  with a market (Earth, Mars, Venus — const list). Opens a Bootstrap modal-style card:
  - **Sell cargo**: converts `_cargoValue` to credits, empties the hold.
  - **Refill reaction mass** to capacity: free (docked tankage is cheap; keeps the loop
    moving).
  - **Buy upgrades**: three rows with current level, effect, price, Buy button (disabled
    when unaffordable).
- Docking does not pause the sim; the card floats like the traffic board.

### 4. Failure state: adrift
- When `_reactionMassPulses == 0` and not docked: show a danger banner "Adrift — request
  rescue?" with a button. Rescue: teleports nothing — it refills mass to capacity but
  **confiscates all cargo** (the rescue fee) and flashes the loss. (Plan's "drift" failure,
  kept simple.)

### 5. First-hunt tutorial
- A dismissible Bootstrap card ("First hunt" toggle in the toolbar, shown by default on
  load) with a checklist that auto-advances on real game events:
  1. "Open the traffic board and select the Luna pod" (done when a pod is selected)
  2. "Plot an intercept — enter Plot, add a burn, watch the ribbon cross its cone" (done on
     first plan node added while a pod is selected)
  3. "Close to boarding range and match velocity" (done when the capture window first
     engages)
  4. "Hold the window — board it" (done on first successful boarding)
  5. "Dock at Earth and sell" (done on first sale)
  6. "Spend it — buy an upgrade" (done on first upgrade)
- Each step: bold current step, checked (✓, muted) past steps. Finishing shows "You're a
  pirate now. The He3 haulers from Saturn are worth 15× more." and the card can be closed.

### Constraints
- Working agreement §9 as always: determinism in Core, Razor+Bootstrap only, scratch
  buffers, no new JS. Don't widen: no charge/EU (M7), no multiplayer (M9), no boarding
  minigame — automatic success is the spec.

## Definition of done
- `dotnet build` 0 warnings; `dotnet test` green (Core tests for CaptureRule/market/pods
  exist; add none unless you add Core code).
- Live: a pod is selectable with a hairline cone; the capture bar engages/fills/boards;
  cargo → credits at Earth; upgrades apply (sensor range visibly extends contact range,
  mass capacity refills higher, hold takes more); adrift → rescue costs the cargo;
  tutorial checklist advances through the whole loop.
- PR notes: constants chosen, deviations, uncertainty — the reviewer plays the full hunt.
