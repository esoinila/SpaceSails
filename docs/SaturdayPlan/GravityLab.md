# The Gravity Lab — numerical orbital mechanics, the type-it-in way

*Owner direction (2026-07-04 evening): the project he wants most — a gravity-calculations
tutorial series that does NOT simplify like the books do, but computes numerically in messy
situations, fast. Curtis (Orbital Mechanics for Engineering Students) sits on his shelf as
the reference the lab talks to. The ethos is the magazine type-in listing: fork it, run it,
break it, learn programming and physics at once. The game engine is the lab equipment.*

## Shape

- `labs/` at the repo root. Each lesson = one folder `labs/NN-slug/` containing:
  - `README.md` — the lesson: motivation, the standard-textbook take (with Curtis chapter
    pointers), what the textbook simplifies away, then the numerical experiment with REAL
    printed numbers (verified by actually running it — no invented outputs, ever).
  - `Probe.cs` + tiny csproj referencing `src/SpaceSails.Core` — a console probe that prints
    the lesson's tables/CSV. Precedent: the M4 MarsPlanner offline probe; determinism means
    probe results transfer 1:1 into the game.
  - **Break it** — 2-3 exercises that intentionally damage the computation ("set dt to 1 h
    and explain Mercury"; "swap the integrator order; where does the energy go?").
  - Optional `scenario.json` loadable in the game (`?scenario=`) to SEE the lesson.
- `labs/README.md` — the index, linked from the main README Docs section.
- One solution filter addition so `dotnet test`/CI stays green and probes build.

## The ladder (each lesson one PR-sized bite)

1. **Falling is orbiting** — two-body freefall computed step by step; vis-viva checked
   numerically against the integrator (the game's Sun-dive numbers as the worked example).
2. **The integrator zoo** — explicit Euler vs semi-implicit (the game's) vs RK4 on one year
   of Mercury: energy drift measured and tabulated; why the game picked what it picked.
3. **Time step is a lie you choose** — fixed dt vs the game's adaptive `ProjectAdaptive`
   (dt = dynamicalTime/64, clamps): flyby accuracy vs cost, with the M4 finding that
   patched conics mislead at flybys.
4. **The ±10% pulse** — impulse quantization: what you can and cannot reach when thrust
   comes in multiplicative pulses; Oberth effect measured from the game's own numbers.
5. **Transfers without formulas** — Hohmann analytic (Curtis ch. 6) vs `RoutePlanner`'s
   grid search on Earth→Mars: when the formula is fine, when the search finds better.
6. **Closest approach, found honestly** — scanning vs the parabola-on-d² refine the planner
   ships (`ClosestApproach`); how the closest-pass warning works.
7. **Hill spheres and bus stops** — sphere-of-influence numerically (`OrbitRule`), orbit
   insertion Δv priced in pulses, why the game's windows are shaped as they are.
8. **Seeing through uncertainty** — observation → prediction cones (`PathPredictor`):
   w0 + σv·Δt growth, why telescope tracking tightens intercepts.
9. **What the rails hide** — write a true n-body toy (all planets pulling each other and
   the ship), compare against the rails ephemeris; meet chaos, sensitivity, and the reason
   games (and mission planners) use patched approximations on purpose.
10. **Fast enough for 10,000×** — performance lesson: making gravity math run at warp in
    WASM (fixed-step batching, the 60 s NPC quantum, adaptive quanta at high warp — the
    real M19 numbers), measured with BenchmarkDotNet-style timings from the probe.
11. **The Electric Sandbox** ⚡ — the game's `PlasmaEnvironment` coupled with gravity:
    charge-dependent forces, stream riding, glare-country ambushes — and a clearly-labeled
    speculative playground: "what if effective μ depended on the electrical environment?"
    Parameterize μ(t) and compute the consequences honestly.
12. **Oops at the Moon** 🌙 — the finale the owner imagined: miners drill a moon and short
    the capacitor; its effective μ / the local force model changes mid-sim. Compute the
    decaying orbit, the ring-like-a-bell seismic flavor text, the disaster-movie timeline —
    perturbed dynamics as a playable catastrophe scenario (`labs` probe + a game scenario).

## Framing rule (public repo credibility)

Standard physics is presented as standard; Curtis is the reference. The EU-flavored lessons
(11, 12) are explicitly labeled as the game's fictional cosmology / speculative playgrounds —
"in this house we compute both, and we label which is which." The lab's honesty is the brand:
every printed number in every README comes from actually running the probe.

## PR lanes

- **PR-19 · Lab framework + lessons 1–3** (framework conventions set here; solution wiring,
  labs/README.md index, README link).
- **PR-20 · Lessons 4–6** (planner lessons) — after PR-19 sets conventions.
- **PR-21 · Lessons 7–9** — parallel with PR-20.
- **PR-22 · Lessons 10–12** (performance + EU sandbox + the finale, incl. game scenario).
