# The Gravity Lab

A gravity-calculations tutorial series that does not simplify the way the textbooks do. Every
lesson computes numerically, in the messy situations a real integrator actually runs into —
using `SpaceSails.Core`, the exact deterministic engine the game itself flies ships with. This
is the magazine type-in listing, brought back: fork a lesson's probe, run it, break it on
purpose, and learn the programming and the physics at the same time. The game engine is the
lab equipment; you are not reading about orbital mechanics, you are running it.

Standard physics is presented as standard, with Curtis, *Orbital Mechanics for Engineering
Students*, as the reference the lab talks to by chapter. Wherever a lesson strays into the
game's fictional cosmology (mass drivers, ancients' pyramid satellites, the Electric Universe
layer), it says so plainly — **in this house we compute both, and we label which is which.**
The lab's honesty is the whole brand: every number printed in every lesson's `README.md` came
from actually running that lesson's probe. If you change a probe's code, its numbers go stale
— rerun it and re-paste, never hand-edit a table.

## How to run a lesson

Each lesson is a tiny console app that prints its own tables straight to stdout:

```bash
dotnet run --project labs/01-falling-is-orbiting -c Release
dotnet run --project labs/02-the-integrator-zoo -c Release
dotnet run --project labs/03-time-step-is-a-lie-you-choose -c Release
dotnet run --project labs/10-fast-enough-for-ten-thousand-x -c Release
dotnet run --project labs/11-the-electric-sandbox -c Release
dotnet run --project labs/12-oops-at-the-moon -c Release
```

Each lesson folder holds:

- `README.md` — the lesson: motivation, the standard-textbook take (with Curtis chapter
  pointers), what the textbook simplifies away, the numerical experiment with real printed
  output, and 2-3 "break it" exercises that intentionally damage the computation.
- `Probe.cs` + a small `LabNN.csproj` referencing `src/SpaceSails.Core` directly — determinism
  means a probe's numbers transfer 1:1 into the live game.

## The ladder

1. [**Falling is orbiting**](01-falling-is-orbiting/README.md) — two-body freefall computed
   step by step; vis-viva checked numerically against the integrator, including the game's own
   ±10% pulse turning a circle into an ellipse.
2. [**The integrator zoo**](02-the-integrator-zoo/README.md) — explicit Euler vs. the game's
   semi-implicit Euler vs. RK4 on one Mercury year and fifty: energy drift measured and
   tabulated, and why "smaller error" and "bounded error" are different guarantees.
3. [**Time step is a lie you choose**](03-time-step-is-a-lie-you-choose/README.md) — fixed dt
   vs. the game's adaptive `ProjectAdaptive` on a sun-grazing hyperbolic flyby: cost vs. accuracy,
   and a case where the adaptive default doesn't automatically win.
4. **The ±10% pulse** *(coming)* — impulse quantization and the Oberth effect, measured from the
   game's own numbers.
5. **Transfers without formulas** *(coming)* — Hohmann analytic (Curtis ch. 6) vs.
   `RoutePlanner`'s grid search on Earth→Mars.
6. **Closest approach, found honestly** *(coming)* — scanning vs. parabola-on-d² refinement, the
   same technique the planner and the closest-pass warning use.
7. **Hill spheres and bus stops** *(coming)* — sphere-of-influence numerically (`OrbitRule`),
   orbit insertion Δv priced in pulses.
8. **Seeing through uncertainty** *(coming)* — observation → prediction cones (`PathPredictor`).
9. **What the rails hide** *(coming)* — a true n-body toy vs. the rails ephemeris: chaos,
   sensitivity, and why patched approximations are used on purpose.
10. [**Fast enough for 10,000×**](10-fast-enough-for-ten-thousand-x/README.md) — the M5/M19
    performance war stories, reproduced honestly on a dev machine: `RunAdaptive`'s real per-call
    cost, one ship vs. the game's actual 23-NPC roster, and the determinism constraint
    (byte-identical below warp 100) verified rather than asserted.
11. [**The Electric Sandbox**](11-the-electric-sandbox/README.md) ⚡ — `PlasmaEnvironment`'s real
    halo and stream mechanics computed straight, plus a clearly labeled speculative playground:
    "what if effective μ depended on the electrical environment?"
12. [**Oops at the Moon**](12-oops-at-the-moon/README.md) 🌙 — the finale: the one lesson that
    un-rails a body. A velocity kick to Luna's orbit (the game's own ±% pulse mechanic, by
    accident), integrated as a genuine free body — departs, degrades, or spirals in, computed
    honestly, plus a playable `scenarios/oops.json` aftermath.

## Framing rule

Standard physics is presented as standard; Curtis is the reference. The EU-flavored lessons
(11, 12) are explicitly labeled as the game's fictional cosmology / speculative playgrounds.
See `docs/SaturdayPlan/GravityLab.md` for the full plan and PR lanes.
