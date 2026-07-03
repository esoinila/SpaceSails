# M7 — Electric Universe layer ⚡: implementation work package

*Companion to `SpaceSails_plan_detailed.md` §8 (M7). Build sheet for the implementing helper.
Builds on the M6 map page — extend it, do not fork it.*

## Goal (acceptance criteria, verbatim from the plan)

- Charge potential field + ship charge state, equilibration, venting control, arcing (damage +
  system-wide sensor ping), sun-side sensor shadow, plasma streams as scenario-defined ribbon
  paths with along-stream force ∝ charge.
- All under `IForceField`/scenario flags so Scenario A can run pure-Newtonian.
- **Accept:** the low-solar-orbit sneak-up ambush works; riding a stream Saturn→Jupiter is
  faster but gets you spotted.

## Design decisions (senior — follow this)

- **Sun-side sensor shadow already exists** (M5's glare cone). M7 adds its *price*: lurking
  sun-side charges your hull; stay dark by venting or glow and be seen.
- **Determinism preserved bit-exactly for chargeless scenarios:** `ShipState` gains
  `double Charge = 0` (positional default — every existing call compiles and equals compare
  unchanged); `Simulator` gains an optional `PlasmaEnvironment? environment` — null means
  every code path is byte-for-byte the M6 behavior. All M1–M6 tests must pass untouched.
- **NPCs stay chargeless in M7** (their simulator gets no environment). NPC charge + AI
  reactions to arc pings arrive with the server-authoritative sim (M9).
- **"Damage" is deferred**: no HP system exists. Arcing in M7 = the system-wide *visibility*
  consequence + HUD drama. Noted in the PR as a deliberate cut.

## What exists (M6 — build on this)

`Pages/Map.razor` (ship loop, plotting, traffic, capture, dock, tutorial), Core M1–M6,
`renderer.js` (opcode canvas painter + hidden-tab fallback).

**New in this milestone, senior-implemented (use, don't rewrite):**

- `SpaceSails.Contracts`: `ScenarioDefinition.ElectricUniverse` (bool, default false) and
  `Streams` (list of `StreamDefinition { FromBodyId, ToBodyId, HalfWidthM }`, default empty).
- `SpaceSails.Core.PlasmaEnvironment` (implements `IForceField`):
  - `AmbientCharge(position, simTime)` → [0,1]: solar halo `min(1, (5e10/r)²)` plus 1.0 inside
    any stream ribbon (segment between the two endpoint bodies' positions at `simTime`,
    within `HalfWidthM`).
  - `Acceleration(position, charge, simTime)` → along-stream unit vector ×
    `StreamAcceleration (2e-2 m/s²)` × charge when inside a ribbon; zero outside.
  - `FromScenario(scenario, ephemeris)` → null when `ElectricUniverse` is false.
- `Simulator`: optional `environment` ctor param; `Step` equilibrates charge
  (`charge += (ambient − charge) × dt/EquilibrationTau`, τ = 3600 s) and adds the stream
  acceleration. `ProjectAdaptive`/`Project`/`Run` inherit it automatically.
- `SensorModel.TryObserve` now takes the target's charge into account: effective range ×
  `(1 + ChargeGlowFactor × charge)`, `ChargeGlowFactor = 2` — a fully charged hull is seen
  3× farther. (Signature: extra `targetCharge` defaulted param — M5 call sites compile.)
- `CaptureRule`, traffic, prediction: untouched.
- `scenarios/sol-eu.json`: Sol + `"electricUniverse": true` + two streams:
  `saturn↔jupiter` (HalfWidth 3e10) and `venus↔mercury` (HalfWidth 1.5e10).
- Client scenario selection: `/map?scenario=sol-eu` loads `scenarios/{name}.json`
  (default `sol`). Already wired in `Map.razor` by the senior model — extend, don't redo.

## Client work (extend Map.razor; Razor + Bootstrap only, NO new JS)

1. **Charge HUD**: a meter in the readouts (Bootstrap progress bar, 0–100%): label
   `Charge: NN%`. Color: info < 60%, warning 60–90%, danger ≥ 90% + text "ARCING RISK".
   Only rendered when the environment is active (EU scenario).
2. **Venting**: `v`/`V` keydown = one vent pulse: `charge *= 0.5` (mutate `_ship` like the
   thrust pulses do), same 1 s sim cooldown budgeted separately (`_lastVentSimTime`), no
   reaction-mass cost, HUD flash "Venting charge". Holding/hammering v keeps you dark but
   occupies your attention — that's the ambush minigame.
3. **Arcing**: while charge ≥ 0.9: HUD danger row "⚡ ARCING — visible system-wide", and the
   ship marker drawn with a bright halo ring (extra DrawCircle, no new renderer opcodes).
   (The plan's "system-wide sensor ping" gameplay lands with M9 servers; fiction + player
   visibility (SensorModel glow) are the M7 consequence.)
4. **Stream rendering**: for each stream, draw a translucent wide polyline (single segment
   endpointsat the two bodies' current positions, `DrawPolyline` with lineWidth scaled from
   `HalfWidthM / metersPerPixel`, clamp 1–200 px; color teal ~ (80,200,220, 36)). Under
   everything else (draw first). Check `IRenderer.DrawPolyline` supports a width argument —
   it does (`lineWidth`); pass the scaled value instead of the default.
5. **Ship charge plumbing**: the ship's `Charge` comes free through `ShipState`; ensure the
   HUD reads `_ship.Charge`, and pass it to the sensor sweep... (the sweep observes NPCs — the
   charge that matters for *being seen* is the player's own; that lands in M9. In M7 the
   player-side consequence is the arc halo and the fiction.)
6. **Scenario badge**: readouts show the scenario name + "EU" tag when electric universe is
   active, so screenshots are self-describing.

### Constraints
Working agreement §9: determinism law in Core (environment must be a pure function of
state + time); Razor + Bootstrap only; scratch buffers; NO new JS. Don't widen: no damage/HP,
no NPC charge AI, no scenario-select UI (M8), no server pings (M9).

## Definition of done
- `dotnet build` 0 warnings; ALL existing tests green untouched (chargeless determinism
  preserved); new Core tests for equilibration/stream force/glow-range already exist.
- Live on `?scenario=sol-eu`: charge meter climbs near the Sun and inside streams; `v` vents
  it back down; ≥ 90% shows the arcing warning + halo; the Saturn↔Jupiter stream is visibly
  drawn and riding it accelerates the ship along the ribbon.
- Live on default `sol`: no charge meter, no streams, behavior identical to M6.
- PR notes: constants, deviations, uncertainties.
