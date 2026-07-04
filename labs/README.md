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
dotnet run --project labs/04-the-ten-percent-pulse -c Release
dotnet run --project labs/05-transfers-without-formulas -c Release
dotnet run --project labs/06-closest-approach-found-honestly -c Release
dotnet run --project labs/07-hill-spheres-and-bus-stops -c Release
dotnet run --project labs/08-seeing-through-uncertainty -c Release
dotnet run --project labs/09-what-the-rails-hide -c Release
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
4. [**The ±10% pulse**](04-the-ten-percent-pulse/README.md) — impulse quantization and the Oberth
   effect, measured from the game's own numbers.
5. [**Transfers without formulas**](05-transfers-without-formulas/README.md) — Hohmann analytic
   (Curtis ch. 6) vs. `RoutePlanner`'s grid search on Earth→Mars.
6. [**Closest approach, found honestly**](06-closest-approach-found-honestly/README.md) —
   scanning vs. parabola-on-d² refinement, the same technique the planner and the closest-pass
   warning use.
7. [**Hill spheres and bus stops**](07-hill-spheres-and-bus-stops/README.md) — sphere-of-influence
   checked numerically against `OrbitRule`'s formula (a jagged stability structure, not a clean
   line), plus orbit-insertion Δv priced in pulses and why the 5 km/s window exists.
8. [**Seeing through uncertainty**](08-seeing-through-uncertainty/README.md) — a real NPC's
   observation → prediction cone (`PathPredictor`) checked against its true hidden flight, then
   telescope track quality (`TrackingStation`) converted directly into boarding-envelope odds.
9. [**What the rails hide**](09-what-the-rails-hide/README.md) — a true from-scratch n-body
   integrator vs. the rails ephemeris: per-planet drift, transfer-plan divergence, sensitivity to a
   1-meter nudge, and why patched approximations are used on purpose (and where they aren't safe).
10. **Fast enough for 10,000×** *(coming)* — the M19 performance numbers, gravity math at warp.
11. **The Electric Sandbox** ⚡ *(coming)* — `PlasmaEnvironment` coupled with gravity, a clearly
    labeled speculative playground.
12. **Oops at the Moon** 🌙 *(coming)* — the finale: a shorted capacitor perturbs a moon's
    effective μ mid-sim, computed honestly as a playable catastrophe.

## Framing rule

Standard physics is presented as standard; Curtis is the reference. The EU-flavored lessons
(11, 12) are explicitly labeled as the game's fictional cosmology / speculative playgrounds.
See `docs/SaturdayPlan/GravityLab.md` for the full plan and PR lanes.
