# Tuesday Plan — PR lanes

Build order: A first (no design blockers, upgrades a live quest). B unlocks C and D. E and F are
independent of B-D. Each lane = one Opus implementer with Chrome verification + cheat codes for
every leg; suite stays green at every merge (274 tests today).

## PR-0 · The plan (this doc pair)

`docs/TuesdayPlan/` — vision + lanes. Merge signals the arc is agreed.

## PR-A · The hunt is the quest (intel → scan → reveal)

- Contracts/ScenarioLoader: optional `"hidden": true` on scenario bodies; `derelict-roadster`
  becomes hidden. Loader test.
- Client: hidden bodies excluded from map draw, picker, scope, "Nearest", HUD until revealed
  (`_revealedBodyIds`, session-scoped). Reveal message + cue.
- Fetch quest upgrade: accept → intel card in the ledger (orbit estimate + expected phase
  window) + quest step "point the scope"; intel card button schedules a prioritized AreaScan at
  the predicted spot/time (TelescopeSchedule.PrioritizeNext); scan completion reveals the wreck.
  Quest card shows the staged steps (intel → scan → fly → pick up → deliver).
- Cheat codes (testing is a feature): `?fetch=intel` (accepted, pre-scan), `?fetch=active`
  (post-reveal — backward compatible), `?fetch=picked`; `?reveal=<bodyId>`. `start=wreck`
  auto-reveals.
- Docs: features/sensors-map.md gains the intel-scan cycle; haven-interior-walk.md quest section
  updated.

## PR-B · Kepler rails (Core)

- `CircularOrbitEphemeris` → supports optional `Eccentricity` + `ArgPeriapsisRad` per body
  (Kepler solve, Newton, fixed iteration budget, deterministic); e=0 path byte-identical to
  today (regression-gated). Scenario JSON schema + loader.
- Cascades in the same PR: game orbit rings drawn as true ellipse polylines; Lab Viz viewer
  formula + scene schema + BOTH ephemeris-parity tests extended (the honesty gate); OrbitRule
  reads instantaneous radius.
- New Core tests: Kepler solver vs. two-body integration of the same orbit (the lab-honest
  check), parity across a parent-chained elliptical moon.

## PR-C · Lab 21 "The Commuter" + the cycler ship

- Lab 21: compute an Earth↔Mars cycler in our phases (lesson 18 machinery), print the timetable
  honestly, --viz with the dated scrubber. Its baked ellipse becomes scenario body
  `the-commuter` (haven, elliptical rail) + StationSpec interior (bar + berths + engine-bay
  wing behind a locked hatch) + Grok backdrops.
- One cycler-timetable quest (sketch #5) handed out aboard.
- Verification: dock at her mid-flight in-browser (cheat `?start=commuter`), walk the bar.

## PR-D · Secret stations + lucky scans

- Two hidden bodies on PR-B rails: Trojan cache (Jupiter's rail, +60° phase), comet vault (long
  ellipse). Rumor/intel items carrying orbit estimates of varying tightness; aimed scans reveal;
  blind scans need luck. One quest each (sketches #4, #8 can share the vault/rock).

## PR-E · Masked contracts (Butch Cassidy lane)

- Contract type "honest work" taken while under FALSE COLORS: escort/ferry/deliver templates,
  heat bleed while under contract, scripted surprise event at a computed pinch point (uses the
  existing hunter/encounter machinery), LieBlown consequences (open question 2).
- First mission: The payroll run (sketch #2). Cheats to jump to the ambush.

## PR-F · Doors that grow the world

- BuildComplex learns runtime wing insertion keyed on unlock state; crack quest V-06 opens a
  real back room (first expansion joint); quest hooks can require/grant rooms.
- One indoor quest using it (fence's stash behind the cracked hatch).

## Standing rules for every lane

- Owner-approval before merge; stacked-PR procedure when lanes depend.
- Chrome verification by the implementer (localhost, cheat-code legs, screenshots) + lead
  re-verification. Full suite green. No DateTime.Now anywhere near determinism.
- Every new mechanic gets its cheat code the same PR — testing is a feature (owner's rule from
  the crack mission).
