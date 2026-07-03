# M5 — Traffic & prediction: implementation work package

*Companion to the milestone entry in `SpaceSails_plan_detailed.md` §8 (M5). Build sheet for the
implementing helper. Builds on the M4 map page — extend it, do not fork it.*

## Goal (acceptance criteria, verbatim from the plan)

- NPC planner: He3 cargo routes Saturn→inner planets as `ManeuverPlan`s (personalities:
  economical, fast, evasive).
- `IObservationModel`: sensor range, sun-glare direction penalty; observations logged with
  timestamps.
- `PathPredictor`: dead-reckon + uncertainty cone (grows with time-since-observation and the
  target's remaining plausible maneuvers); hypothesis pinning ("brakes at Mars").
- Traffic board UI (Bootstrap table): departures, cargo class, last-seen.
- **Accept:** the cone visibly tightens as you shadow a target; a pinned correct hypothesis
  makes the predicted and actual paths converge.

## Architecture decision (senior, follow this)

The plan places the NPC planner "in Server host", but the client↔server channel (SignalR) is
M9 and the M5 accept test is single-player. Resolution: **all planning/prediction logic lives
in `SpaceSails.Core`** (deterministic — the same seed yields the same traffic everywhere), the
**Server exposes it at `GET /api/traffic`** (honoring the plan; M9 swaps the client onto it),
and the **M5 client generates traffic locally** from the same Core planner with a fixed seed.
Until M9's server-side filtering, hiding unobserved NPCs is client-side honor system — fine
for single-player.

## What exists (M4 — build on this)

- `Pages/Map.razor`: real ship + accumulator loop (`Simulator.Step(_ship, _plan)`), plotting
  mode (scrubber, ghosts, maneuver nodes), `ProjectAdaptive` ribbon, warp auto-drop, HUD,
  pulse messages.
- `SpaceSails.Core`: `Simulator` (`Step`/`Run`/`Project`/`ProjectAdaptive`, `DynamicalTime`),
  `TrajectorySample`, `ManeuverPlan`/`ManeuverNode`, `ShipState`, `CircularOrbitEphemeris`.
- **New in this milestone, already implemented by the senior model (use, don't rewrite):**
  see "Core additions" below — `DeterministicRandom`, `RoutePlanner`, `TrafficSchedule`,
  `SensorModel : IObservationModel`, `Observation`, `PathPredictor`, `PredictedPath`,
  plus the Server `/api/traffic` endpoint.

## Core additions (senior-implemented; interface summary for the client work)

- `DeterministicRandom(ulong seed)` — SplitMix64; integer math only, identical on WASM/server.
- `NpcShip` record: `Id`, `Callsign`, `CargoClass`, `OriginId`, `DestinationId`,
  `RoutePersonality`, `DepartureTime`, `InitialState` (ShipState at spawn), `Plan`
  (ManeuverPlan; nodes may lie before spawn for mid-flight ships — harmless, windows passed).
- `RoutePersonality` enum: `Economical` (one early burn, ballistic, predictable), `Fast`
  (max burn, high speed), `Evasive` (burn split into several seeded smaller burns at odd
  times — hard to dead-reckon).
- `RoutePlanner.PlanRoute(ephemeris, origin, destination, departureTime, personality, rng)` —
  small deterministic grid search (~tens of candidates on `ProjectAdaptive`) for a transfer;
  NPC arrival tolerance is loose (despawn radius 1e10 m), so the search is cheap.
- `TrafficSchedule.Generate(ephemeris, seed, count)` — deterministic departure list; includes
  **mid-flight ships** (planned from a virtual past departure at coarse dt, their state at
  t=0 *declared* the initial truth — deterministic forward from there) so the inner system
  has traffic at sim start, plus future departures spread over the first weeks.
- `Observation` record: `TargetId`, `SimTime`, `Position`, `Velocity`.
- `SensorModel(rangeMeters, glareHalfAngleRad, glareRangeFactor) : IObservationModel` —
  `TryObserve(observerPosition, targetState, simTime, out Observation)`: in range unless the
  target sits within the glare cone toward the Sun, which multiplies effective range by the
  glare factor. Pure, deterministic.
- `PathPredictor.Predict(ephemeris, observation, hypothesis, horizonSeconds)` →
  `PredictedPath { Samples (center line via ProjectAdaptive from the observed state, with the
  hypothesized plan if any), HalfWidthAt(simTime) }`. Cone half-width
  `w0 + sigmaV·Δt + 0.5·aBudget·Δt²` (constants in Core, tuned for feel): grows with time
  since the observation; a fresh observation restarts Δt — that's the "tightens as you
  shadow" mechanic. A pinned hypothesis changes the center line, not the width model.

## Client work (the delegated part — extend Map.razor)

### 1. NPC ships in the live loop
- Load `TrafficSchedule.Generate(ephemeris, seed: 42, count: 8)` at startup. Ship becomes
  *active* when `SimTime >= DepartureTime` (mid-flight ships are active immediately).
- Step every active NPC inside the existing fixed-dt accumulator loop, same dt, passing its
  `Plan`: `npc.State = _simulator.Step(npc.State, npc.Plan)` alongside the player step (same
  while-loop body — do not run a second accumulator).
- Despawn (mark `Arrived`) when within 1e10 m of its destination body after its last node.

### 2. Sensor sweep + observation log
- Every 60 s **sim time** (track `_nextSweepSimTime`, same pattern as ribbon refresh), run
  `SensorModel.TryObserve` from the player position against every active NPC. Store the
  latest `Observation` per target plus the observation count (`Dictionary<string, ...>`).
  Constants: range 1.0e11 m, glare half-angle 20°, glare factor 0.25 — Core defaults.
- NPCs are drawn on the map **only when currently observed** (marker + callsign, distinct
  color e.g. `200,120,255`). Otherwise, if ever observed: draw a small hollow "last seen"
  marker at the last observation position (dimmed).

### 3. Prediction cone for the selected target
- Traffic-board row click selects a target (toggle). For the selected target with an
  observation: `PathPredictor.Predict(...)` → draw the center line (thin, e.g.
  `150,150,220,140`) and two boundary polylines offset ±`HalfWidthAt(t)` perpendicular to
  the local path direction (scratch buffers; ~120 points each is plenty).
- Re-predict when a newer observation of that target lands, when a hypothesis is pinned, or
  every 6 h sim (reuse the refresh pattern). The tightening happens by itself: shadowing the
  target refreshes observations, resetting Δt.
- **Hypothesis pinning:** per selected target, a "Pin: brakes at <destination>" toggle
  (destination is public departures-board info). Pinned hypothesis = single `Decelerate`
  burst (10 pulses) at the predicted closest approach to the destination (Core helper
  provides it: `PathPredictor.BrakeAtHypothesis(...)`). Pinned + correct ⇒ center line and
  the target's actual track converge — the accept test.

### 4. Traffic board UI
- A Bootstrap table in a collapsible card ("Traffic" toggle button in the toolbar), columns:
  Callsign, Cargo, Route (origin→dest), Departs (sim time), Last seen (sim time or "—"),
  Status (`Scheduled`/`En route`/`Tracked`/`Lost`/`Arrived`). Row click = select target
  (highlight row). Keep `StateHasChanged` throttling; the table is small (≤8 rows).

### Constraints (working agreement §9)
- Determinism law in Core (already handled); UI = Razor + Bootstrap only; **no new JS**;
  scratch buffers for polylines, no per-frame heap churn; don't widen the milestone: no
  capture mechanics (M6), no charge/glare *fields* (M7 — glare here is only the sensor cone),
  no server push (M9).

## Definition of done
- `dotnet build` 0 warnings; `dotnet test` green (Core + Server tests already added by the
  senior model; add none unless you add Core/Server code).
- Live: traffic board fills at start; NPC markers appear/disappear with sensor range and sun
  glare; selecting a tracked target shows a cone that visibly widens while unobserved and
  snaps tight when re-observed; pinning "brakes at <dest>" on an economical/fast ship makes
  the prediction hug its actual path; an evasive ship's cone stays honest (its real track
  stays inside the cone).
- PR notes: constants chosen, any deviation from this spec, anything you were unsure about.
