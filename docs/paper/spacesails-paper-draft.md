# SpaceSails: Secretly a Classroom

## Deterministic Real-Time Orbital Simulation in the Browser — and the Human-PO / AI-Head-Coder Method That Built It

*DRAFT v0.1 — 2026-07-05. Venue framing per the owner: primarily a SIGGRAPH-style
real-time-systems paper (a), with the experience report (c) carried as a first-class
section rather than a separate submission. Authors, venue target, and figure list TBD
with the owner. Every number in this draft is checkable against the repository history
and test suite; where a number may drift, it is marked (as of DATE).*

---

## Abstract

SpaceSails is a browser-native solar-system sailing and piracy game whose engine is a
deterministic 2D N-body integrator compiled to WebAssembly. Nothing in it cheats: every
trajectory is integrated, every prediction is honestly uncertain, and the hardest maneuvers
are hard because physics says so. The same properties that make the game honest make it a
classroom: the repository ships a thirteen-lesson numerical-methods lab course that runs on
the game's own engine, and the game's newest mechanic — fire control — is the shooting
method for boundary value problems, played. We describe (1) the system: a determinism-first
core, adaptive symplectic integration with exact maneuver-node landing, honest uncertainty
cones, and a closed-form no-tunneling proximity test; (2) the duty-station interface that
distributes one simulation across eight crew desks; and (3) the process: a two-person team —
a human product owner and an AI "head coder" — that took the project from empty repository
to roughly sixty reviewed, tested, merged pull requests in four days, with the owner
approving PRs from a phone and the AI implementing, verifying in a live browser, and
managing the merge train. We argue the combination — deterministic simulation as both
gameplay substrate and curriculum, plus AI-driven implementation under tight human product
direction — is a repeatable method, and we report what it cost, where it failed, and what
the failures taught the game.

---

## 1. Introduction

- The pitch: a piracy game on the surface, a numerical-methods and orbital-mechanics
  classroom underneath. "Secretly edutainment" is the design law, not an accident: the
  owner's first shipped software (SoittoPeli, 1998) was music edutainment for children,
  and SpaceSails carries the same DNA deliberately.
- Thesis: honesty is the unifying design constraint. The simulation never lies (determinism,
  §2), the instruments never lie (uncertainty cones that admit what they don't know, §3),
  and the curriculum never lies (every printed number in the labs comes from running that
  lesson's probe, §4). Play the game well and you have quietly learned the real thing.
- Contributions list (draft):
  1. A determinism-first architecture for real-time orbital simulation in WASM, with the
     concrete rules that kept client and (archived) server bit-identical.
  2. Honest uncertainty as a game mechanic: prediction cones whose growth terms were
     falsified by the game's own lab course and then fixed (the Lab-08 story, §4.2).
  3. Fire control as pedagogy: the shooting method (Newton iteration over launch bearing
     and charge, residuals evaluated by the real integrator) surfaced as a visible,
     converging "CALCULATING FIRING SOLUTION" moment, with each solution printing the
     "gunner's lesson" it embodies.
  4. An experience report of the human-PO / AI-head-coder method at unusual cadence, with
     the working agreements that made review at that pace possible.

## 2. The deterministic core

- **Determinism is law (§9 of the working agreement).** No wall clock, no `Random`, no
  environment-dependent math anywhere in `SpaceSails.Core`. Consequences: replays are free,
  the (archived) multiplayer server agreed with clients bit-for-bit, and — decisive for
  this paper — *tests and lessons are the same artifact* (a lab probe is just a test that
  prints its evidence).
- Celestial bodies ride rails (`ICelestialEphemeris`: position as a pure function of time);
  ships feel point-mass gravity from everything and integrate with semi-implicit Euler.
- **Adaptive stepping with exact node landing**: step = fraction of local dynamical time
  min√(d³/μ), clamped, and forced to land exactly on every maneuver node's timestamp so a
  plotted burn executes in the projection at the same instant the live loop fires it.
  Universal-variable Kepler / patched conics were rejected on purpose: they assume one
  attractor per arc and disagree with the integrator exactly where the game happens —
  flybys. (Lab 06 §C quantifies what adaptive stepping still misses at periapsis and what
  it costs to fix.)
- **The no-tunneling rule**: closest-approach and ordnance-hit tests use the closed-form
  minimum of piecewise-linear relative motion per integrator step — quadratic algebra, so
  no step size can tunnel a fast graze through a target. This replaced a per-sample check
  the lab course itself proved dishonest (Lab 06's fast-graze finding).
- WASM performance notes: interpreted-IL is ~100× native (measured consequences: NPC
  route search at 1-day steps; 60 s NPC quanta; the M19 high-warp adaptive quanta path).

## 3. One simulation, eight desks

- The ship is crewed, not driven: Captain (standing orders + ship status board), Nav (map,
  plotting table, orbit-assist autopilot), Sensors (telescope ledger, passive watch, the
  eyes race), War room (tactical circle, intercept clock, fire control), Trade, Comms
  (departures board, dark web), Galley (news feed, rum), Deck (walkable interior with a
  raycast first-person view whose windows show the real ephemeris sky).
- Design rules that survived playtesting: a desk's own topic owns ~70% of the screen; every
  other station rides along as a one-line "chip"; chips are current-objective summaries,
  never stat dumps; the captain's chip leads everywhere.
- Honest instruments as gameplay: sun-glare detection asymmetry makes anti-sunward the
  pirate's hunting angle; a charged hull glows farther than it sees; the mutual-visibility
  readout ("do we see them, or do they see us first") turns the sensor model itself into
  strategy.

## 4. Secretly a classroom

### 4.1 The Gravity Lab
- Thirteen type-it-in lessons (`labs/`), each a console probe referencing the game's own
  `SpaceSails.Core` — motivation, the standard-textbook take (Curtis pointers), what the
  game simplifies away, a numerical experiment with real printed output, and "break it
  yourself" exercises. The honesty rule: every printed number came from running that
  lesson's probe; rerun and re-paste, never hand-edit.

### 4.2 The labs falsify the game (and the game gets fixed)
- Lab 08 measured the prediction cone (w₀ + σᵥ·Δt + ½a·Δt²) against a target that actually
  burns and found it *flatly excluded the truth for hours after a real burn* (conservatism
  0.5× at 2 h and 6 h). The fix — an impulse term that grows linearly at the plausible
  burst Δv — shipped as a prerequisite of fire control, with the lab's dishonesty table
  reproduced as a regression test.
- Lab 06 proved the closest-approach scan could step clean through periapsis (380,845 km
  reported vs 157,866 km true) and priced the fix; the closed-form segment minimum of §2
  is that fix, and ordnance hit resolution inherits it.
- The point for the venue: the curriculum is not documentation *about* the engine; it is
  an adversarial test suite the engine must survive, written to be read.

### 4.3 Fire control as the shooting method
- The firing solution is a boundary value problem — leave the muzzle now, be at that point
  at t_hit — solved by damped 2×2 Newton over launch bearing and ejection charge, every
  residual one flight of the real integrator. The gun deck replays the iteration trace one
  step per beat ("CALCULATING FIRING SOLUTION…"), locks at T-60 s, slews the hull, fires,
  and returns control — the Norden-bombsight beat, honestly earned.
- Dispersion is the target track's cone half-width at t_hit: fire-control quality IS track
  quality, so telescope work at the Sensors desk directly buys tighter shots at the gun
  deck — one weapon system across two stations.
- Each locked solution prints its "gunner's lesson" (lead angle, flight time): the solver
  as flight instructor, teaching the same skill manual intercepts need.

## 5. Experience report: a human PO and an AI head coder

- **Roles.** The owner sets vision, plays builds, files corrections as plain language
  ("the sensors are off by default — why?"), and approves PRs (often from a phone). The AI
  implements, tests, *verifies live in a real browser via automation*, writes the PR
  narratives, and runs the merge train. Review is by playing, not by reading diffs alone.
- **Cadence (as of 2026-07-05).** From empty repo to the full game plan (M0–M10) in one
  day of implementation; ~60 merged PRs over four days total; ~180 deterministic Core
  tests; a 12-lesson lab course; the fire-control feature set shipped overnight as a
  six-PR stacked chain reviewed at morning coffee.
- **Working agreements that made the pace safe**: determinism is law (reviewability);
  Core-first with tests (every mechanic lands as a pure rule before UI); one new file per
  parallel lane + one-line appends at marked anchors in the shared hotspot file; every
  claim in a PR body verified live before it is written.
- **Failure gallery (honest):** the interpreted-WASM freeze class (twice); the destroyed-
  canvas class (a Blazor conditional unmounting a canvas leaves the renderer holding a dead
  context — found three times: scope, tracking post, and prevented in the parrot); the
  "three target locks" perception bug — the sim was right and the presentation lied;
  transient Pages deploy failures and the retry playbook; a stacked-PR merge-train pitfall
  (deleting a merged base branch auto-closes its children — recovered live and folded into
  the plan doc's procedure).
- **What the AI could not do**: choose what the game should feel like. Every mechanic in
  §3–§4 traces to an owner sentence written in plain language; the method's throughput is
  the owner's taste executed quickly, not replaced.

## 6. Related work (to expand)

- Kerbal-genre orbital games and their patched-conics compromise vs. our integrate-
  everything stance; educational-game literature on "stealth learning"; determinism-first
  netcode traditions (lockstep RTS); prior "AI pair programming" experience reports —
  positioned against a *product-owner/head-coder* split rather than pair programming.

## 7. Limitations & future work

- 2D ecliptic plane; on-rails circular ephemerides; multiplayer archived pending the
  economy's server-side move; persistence absent by design so far. Lesson 13 ("Shooting,
  literally") is drafted as the fire-control lab and lands with its probe. The Ancients'
  auto-plot deliberately rations automation to protect the pedagogy.

---

*Reproducibility: everything in this paper runs from the public repository —
`dotnet test` for the evidence, `dotnet run --project labs/NN-… -c Release` for every
printed number, and the game itself at the GitHub Pages deployment.*
