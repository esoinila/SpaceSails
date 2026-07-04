# Sunday Plan — Fire Control (vision)

*Owner + Fable, captured 2026-07-04 late. Fresh-context starting point for the next session.
Read docs/SaturdayPlan/ for how Saturday ran (vision → PR lanes → parallel implementers →
review → approve); repeat the pattern.*

## The owner's idea, in his words (distilled)

Calculate a shot — a slug, or a missile if course corrections are needed — to hit a position
selected on a tracked ship's estimated track in a scrub view. The slug must be
self-evaporating after some time. UI: pick the spot on the opponent's predicted path, a
"calculating firing solution" phase, then the solution locks about one minute into the
future; the ship auto-turns to take the shot and turns back after. WW2 Norden-bombsight
coolness — the bombsight flew the whole plane. The same solver could power a "calculate
path" assist in navigation — gated as an ancient-alien power-up, so players still learn to
fly themselves.

## Why the codebase is ready for this

- **The math is a boundary value problem solved by the *shooting method*** — Newton
  iteration over launch direction/speed, each evaluation one run of the deterministic
  `Simulator`. The Gravity Lab (labs/01-12) is the curriculum for exactly this; the solver
  becomes **Lesson 13: "Shooting, literally."**
- **Fire-control quality = track quality.** `PathPredictor` cones (Lab 08) give the
  predicted-position error honestly; the firing solution inherits it. Telescope work on the
  Sensors desk directly buys tighter dispersion at the gun deck — one weapon system across
  two stations. Lab 08's post-burn cone-dishonesty finding (task list) must be fixed as part
  of this: a prey that burns mid-flight should break locks *by the model*, not by accident.
- **The slug is an NpcShip-shaped thing**: ballistic (`ManeuverBudget = 0` — the mass-driver
  pod already proved the type), despawn timer (self-evaporating), missile variant = small
  nonzero budget with a homing correction rule. Lab 06's fast-graze finding (task list) also
  matters here: slug-vs-ship closest approach needs the dense-refine treatment or hits get
  missed by the integrator itself.

## The feature set

1. **F1 · The firing solution (Core)**: `FireControl.Solve(shooterState, muzzleSpeed,
   targetPredictedPos, tHit)` → launch direction + expected miss distance, by shooting-method
   Newton on the Simulator; returns the iteration trace (the UI shows convergence). Solution
   validity window + dispersion from the target's cone quality. Deterministic, tested.
2. **F2 · Gun deck UI (the Norden moment)**: on the War room desk — select a tracked target,
   scrub along its predicted path, pick the intercept point; CALCULATING FIRING SOLUTION
   (converging iterations visible); solution locks T-60s; ship auto-slews (orientation is
   cosmetic on the map, real in deck/FP view), fires, returns control. Warning shots become
   real slugs across the bow (EncounterRule already models the reaction).
3. **F3 · Slugs & missiles in the sim**: ballistic slug entity with despawn; missile with
   tiny correction budget; hit resolution via dense closest-approach refine; outcomes feed
   the news wire ("someone put a slug through a hauler's sail off Titan").
4. **F4 · The Ancients' pilot (nav power-up)**: the same solver offered as scarce auto-plot
   charges granted by a pyramid-satellite encounter (worldbuilding §2 — off-board NPC class,
   no departures entry, sensor behavior that doesn't match SensorModel). Design rule: charges
   are rare; manual flight stays the skill the game teaches.
5. **F5 · Lab lesson 13**: "Shooting, literally" — the BVP, Newton on the integrator, and
   dispersion-from-uncertainty; every number from a live probe run, as always.
6. **F6 · Piloting tips from firing solutions** (owner, late addition): the solver's outputs
   become flight instruction — in-game tips/examples derived from computed solutions ("to
   hit a spot 40° off your track, burn like THIS"), so the new mechanic teaches by worked
   example and manual flying feels familiar faster. Fits the guide and/or contextual hints
   at the gun deck and nav desk.

## The soul of it: secretly edutainment

The owner's first programming job (SoittoPeli, 1998) was music edutainment for children —
gen-AI accompaniment from a Kohonen neural network, the singing game added to make it fun.
SpaceSails carries the same DNA on purpose: a piracy game on the surface, a numerical-methods
and orbital-mechanics classroom underneath (the Gravity Lab, and now fire control as the
shooting-method lesson). Keep building features so that playing them well quietly teaches
the real thing.

## Paper draft (owner request, details to confirm)

The owner wants a paper draft in the repo, linked from the README ("the SIGGRAPH paper
draft"). To confirm with him at session start: venue/framing — options: (a) SIGGRAPH-style
(real-time deterministic WASM simulation + the duty-station UI), (b) games/education venue
(the secretly-edutainment method: a game whose lab IS its engine), (c) an experience-report
angle (human PO + AI head coder building 23 PRs in a day). Draft lives in docs/paper/,
gets a prominent README link like the labs.

## Carried backlog (from Saturday's task list)

- PR-16 the ship's parrot 🦜 (deterministic squawks; LLM stage 2 later) — natural squawk:
  "FIRING SOLUTION, CAPTAIN!"
- Fix: Nav scope inset blank after visiting Sensors (pre-existing).
- Core: PathPredictor post-burn cone honesty (Lab 08) — REQUIRED by F1.
- Core: fast-graze closest-approach miss (Lab 06) — REQUIRED by F3 hit resolution.

## Suggested PR lanes

- **PR-A · Core fire control + the two Lab-found fixes** (F1 + cone honesty + graze refine;
  they're one coherent Core package). Lesson 13 rides along or follows.
- **PR-B · Slug/missile entities + hit resolution + news** (F3), after PR-A.
- **PR-C · Gun deck Norden UI** (F2), after PR-A, parallel with PR-B.
- **PR-D · The Ancients' pilot** (F4), after PR-A, parallel.
- **PR-E · Parrot + scope-inset fix** (independent, any time).

## Working agreement unchanged

Determinism is law in Core; UI = Razor + Bootstrap; JS only in renderer.js; every lab number
from a real probe run; senior reviews/verifies/merges; owner approves PRs (push + bridge
notify per [[pr-approvals-to-mobile]] conventions).
