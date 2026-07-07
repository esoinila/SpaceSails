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
dotnet run --project labs/10-fast-enough-for-ten-thousand-x -c Release
dotnet run --project labs/11-the-electric-sandbox -c Release
dotnet run --project labs/12-oops-at-the-moon -c Release
dotnet run --project labs/13-shooting-literally -c Release
dotnet run --project labs/14-two-points-and-a-clock -c Release
dotnet run --project labs/15-the-long-passage -c Release
dotnet run --project labs/16-going-ashore -c Release
dotnet run --project labs/17-the-pocket-solar-system -c Release
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
13. [**Shooting, literally**](13-shooting-literally/README.md) 🎖 — the encore: the firing
    solution as a boundary value problem, solved by the shooting method — Newton over launch
    bearing and charge, every residual flown through the real integrator (the war room's
    CALCULATING FIRING SOLUTION trace, reproduced). Straight-line gunnery misses by 5,996 km;
    one Newton step lands 85 km out. Then the honest part: dispersion is the target *track's*
    cone, not the solver's residual — fire-control quality IS track quality.
14. [**Two points and a clock**](14-two-points-and-a-clock/README.md) — Lambert's problem
    (Curtis ch. 5, universal variables, implemented in the probe) meets the shooting method:
    Lambert is exactly right about a universe with one attractor, misses by 443,569 km in the
    one with nine, and Newton through the real integrator fixes it in ONE step for 14 m/s.
    Plus a verified porkchop plate — and the plot twist of a floor *below* Hohmann, explained
    honestly (the spawn point's 5,000,000 km head start, not a broken theorem).
15. [**The long passage**](15-the-long-passage/README.md) — six years to Saturn, where small
    numbers stop being small: Hohmann's tyranny table (Neptune: ~30.6 YEARS), a shooting-solved
    passage through Jupiter country (Lambert's two-body lie now costs 150 m/s, not 14), 1 m/s of
    departure error compounding ~4× into 718,250 km, and the navigator's oldest law computed —
    the same sin absolved at day 30 costs 1.26 m/s, on the deathbed 348 m/s.
16. [**Going ashore**](16-going-ashore/README.md) — moons: bus stops nested inside bus stops.
    The Enceladus insertion window is a 444 km shell (the haven that barely exists); Luna
    parking orbits stress-tested prograde AND retrograde at two time steps — the game's cruise
    ceiling doesn't blur the classical stability map (prograde ~0.5 Hill, retrograde ~0.9), it
    nearly INVERTS it; and the series' first landing: de-orbit dv, fall time, and touchdown
    speed per moon, the Luna row flown to verify (759 min analytic, 760 flown).
17. [**The pocket solar system**](17-the-pocket-solar-system/README.md) — Saturn's moons are
    the whole course at 1/1000 scale: Enceladus→Titan has Earth→Jupiter's radius ratio with a
    3.7-DAY Hohmann and a window every 36 hours; lessons 14-15's toolchain transfers verbatim;
    and the sun's share of the correction, isolated by construction, is 0.04 m/s — pocket
    systems aren't just fast, they're CLEAN.

## Framing rule

Standard physics is presented as standard; Curtis is the reference. The EU-flavored lessons
(11, 12) are explicitly labeled as the game's fictional cosmology / speculative playgrounds.
See `docs/SaturdayPlan/GravityLab.md` for the full plan and PR lanes.
