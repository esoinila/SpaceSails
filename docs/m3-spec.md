# M3 — Fly the ship (real-time single player): implementation work package

*Companion to the milestone entry in `SpaceSails_plan_detailed.md` §8 (M3). Build sheet for the
implementing helper. Builds directly on the M2 map page — extend it, do not fork it.*

## Goal (acceptance criteria, verbatim from the plan)

- Player ship in the loop: `+`/`−` keys apply velocity pulses (with cooldown + reaction-mass
  budget), HUD (Bootstrap overlay: velocity, mass remaining, warp, nearest body).
- Warp auto-drop near bodies. Trajectory ribbon ahead of the ship (`Project` on every pulse).
- **Accept:** you can leave Earth, do a (ugly) Mars flyby by hand, and it *feels controllable*.

## What exists (M2 — build on this)

- `Pages/Map.razor` — the live map with the rAF loop (`OnTick`), camera input, warp toolbar, HUD
  readouts, and a **dummy analytic ship** (`DummyShipPosition`). M3 replaces that dummy with a real
  `Simulator`-integrated ship and adds controls + ribbon.
- `SpaceSails.Core`: `Simulator` (`Step`, `Run`, `Project`, `GravitationalAcceleration`),
  `ShipState(Position, Velocity, SimTime)`, `ManeuverPlan`/`ManeuverAction` (`AccelerateFactor=1.1`,
  `DecelerateFactor=0.9`), `CircularOrbitEphemeris`, `Vector2d`.
- `Rendering`: `Camera`, `IRenderer`/`CanvasRenderer` (`DrawCircle`, `DrawPolyline`, `DrawText`).

## Design (follow this)

### 1. Real ship, fixed-dt integration with an accumulator
Replace the dummy ship with a `ShipState` integrated by `Simulator`. **Determinism rule (plan §5/§6):
fixed simulation timestep regardless of warp.** Do not step the ship by a variable `dtReal*warp`.
Use an accumulator:

```
_simAccumulator += dtRealSeconds * Warp;          // only when not Paused
while (_simAccumulator >= sim.TimeStep) {
    _ship = sim.Step(_ship, plan: null);          // live pulses mutate _ship directly, not via a plan
    _simAccumulator -= sim.TimeStep;
}
SimTime = _ship.SimTime;                            // HUD/bodies read ship's authoritative sim time
```

- Construct `Simulator(_ephemeris, timeStepSeconds: 1.0)` (dt = 1 s, per plan M1).
- Cap steps-per-frame defensively (e.g. clamp the accumulator or a max loop count ~20000) so a long
  GC pause or a huge warp can't spiral; note any clamp in the PR.
- Celestial bodies continue to be drawn at `SimTime` (already the case in M2).

### 2. Initial ship state — co-moving with Earth
Start docked to Earth's motion so the player must actively leave:
- `position = ephemeris.Position("earth", 0)`.
- `velocity` = Earth's instantaneous orbital velocity. Compute by numeric derivative (robust, no
  assumption about orbit direction): `(Position("earth", h) - Position("earth", -h)) / (2h)` with a
  small `h` (e.g. 1.0 s). (Analytic tangential velocity is fine too, but the derivative reuses the
  ephemeris and can't disagree with it.)
- Optionally nudge slightly outward so it's visibly distinct from Earth; not required.

### 3. Controls — live velocity pulses (`+` / `−`)
- Keyboard via Razor `@onkeydown` on a focusable wrapper (`tabindex="0"`, focus it on load). **No new
  JS** — `input.js` is a later milestone; Razor keyboard events only.
- Bind `+`/`=` and `−`/`-` (and ideally `ArrowUp`/`ArrowDown` as aliases) to accelerate/decelerate.
- A pulse multiplies the **current velocity vector**: `v *= ManeuverPlan.AccelerateFactor` (1.1) or
  `DecelerateFactor` (0.9). Apply directly to `_ship` (real-time). ManeuverPlan-scheduled nodes are
  M4 (plotting) — not here.
- **Cooldown:** ~1 s **sim-time** per pulse (plan §3.1). Track `_lastPulseSimTime`; reject a pulse if
  `_ship.SimTime - _lastPulseSimTime < PulseCooldownSeconds`. (Sim-time, so warp scales it naturally.)
- **Reaction-mass budget:** a simple pulse/mass counter (e.g. `ReactionMassPulses = 100` to start).
  Each successful pulse decrements it; at 0, pulses are rejected. Show remaining in the HUD.
- On any successful pulse, recompute the trajectory ribbon (see §5) and set a "downstream stale"
  nudge isn't needed yet (that's M4). A brief on-screen flash/log of "no mass" / "cooling down" is a
  nice-to-have, not required.

### 4. HUD additions (Bootstrap overlay — extend M2's `.map-readouts`)
Show: speed (km/s, `_ship.Velocity.Length/1000`), speed relative to nearest body (optional but nice),
reaction mass remaining (pulses), current warp, and **nearest body + distance** (min over
`ephemeris.Bodies` of `(Position(body,SimTime) - _ship.Position).Length`; show name + distance in km
or AU). Keep it Razor + Bootstrap; throttled `StateHasChanged` like M2.

### 5. Trajectory ribbon ahead of the ship
- `sim.Project(_ship, plan: null, horizonSeconds, sampleEverySteps)` → polyline; transform through
  `Camera.WorldToScreen` and `DrawPolyline` (distinct color from the dummy trajectories, e.g. warm).
- Recompute **on every pulse** and whenever it goes stale — cheap approach: recompute every N frames
  or when `SimTime` advances past a threshold, since gravity bends the path over time. Keep the
  projection coarse (`sampleEverySteps` large, bounded point count ~300–600) to stay within the M2
  perf budget. Draw it under the ship marker.
- Horizon: pick something that reads well at the default zoom (e.g. 20–60 days sim-time); expose as a
  const. This is the "plot-ahead" feel that M4 will make editable.

### 6. Warp auto-drop near bodies
- When the nearest-body distance drops below a threshold (e.g. a few × that body's `OrbitRadius`
  fraction, or an absolute like 5e9 m — tune for feel), clamp the **effective** warp down (e.g. to
  ≤100×, and ≤10× very close) so the player doesn't tunnel through an encounter in one frame. Keep the
  user's selected warp, but apply `effectiveWarp = min(selectedWarp, cap(distance))` in the
  accumulator and show the capped value in the HUD (e.g. "Warp: 1000× (auto 100×)").
- This also protects the fixed-dt integrator from taking too coarse an effective step near a mass.

## Constraints (working agreement §9)
- **Determinism is law in `SpaceSails.Core`.** If M3 needs a new Core helper (e.g. a velocity helper
  on the ephemeris), add the minimal version in Core, keep it deterministic, and note it in the PR.
  Prefer computing ship velocity in the client from the existing ephemeris to avoid touching Core.
- UI = Razor + Bootstrap only; custom CSS only for canvas/HUD; **no new JS** (keyboard via Razor).
- Reuse scratch buffers for the ribbon like M2 does for trajectories; no per-frame heap churn.
- Don't widen the milestone: no plotting/maneuver-node editing (M4), no NPCs (M5), no capture (M6).

## Definition of done
- `dotnet build` clean (0 warnings), `dotnet test` green (add a Core test if you add a Core helper).
- Running the client: ship starts co-moving with Earth; `+`/`−` visibly change velocity and the ribbon
  re-projects; HUD shows speed / mass / warp / nearest body; warp auto-drops near a body; you can burn
  away from Earth and steer a rough Mars flyby by hand.
- PR notes: any accumulator clamp, the mass/cooldown constants chosen, ribbon horizon, and any Core
  addition.
