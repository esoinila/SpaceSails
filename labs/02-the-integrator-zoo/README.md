# Lab 02 — The integrator zoo

*Standard physics. Curtis, "Orbital Mechanics for Engineering Students," ch. 2 (the two-body
equation of motion this whole zoo is trying to integrate); the integrator comparison itself is
numerical-methods territory Curtis doesn't cover, because Curtis assumes you solve the orbit
in closed form and only worries about propagation error when you can't (perturbed orbits,
ch. 12+). We worry about it here on purpose, on the simplest possible orbit, because a
simulator that flies ships in real time doesn't get the luxury of a closed form.*

## The idea

Newton's law of gravity tells you the acceleration *right now*. Turning a stream of
instantaneous accelerations into a position an hour, a day, or a century later is a separate
problem — numerical integration — and there is more than one honest way to do it. This lab
builds three, from scratch where it matters, and races them around Mercury's real orbit
(`mu` and orbit radius straight from `scenarios/sol.json`) for one full Mercury year, then much
longer, watching what happens to **specific orbital energy** (`epsilon = v²/2 - mu/r`) — a
quantity the *continuous* two-body problem conserves exactly. Any drift in it is 100% the
integrator's fault.

- **Explicit (forward) Euler** — advance both position and velocity using the *old* velocity
  and *old* acceleration. The simplest possible scheme; also the one this lab exists to warn
  you off.
- **Semi-implicit ("symplectic") Euler** — the game's own integrator, called through the real
  `Simulator` class, not reimplemented. One line different from explicit Euler:
  `Simulator.StepBy` advances velocity with the old acceleration, then advances *position* with
  the *new* velocity (see `Simulator.cs`). That single reordering is the entire subject of this
  lesson.
- **RK4** — classical 4th-order Runge-Kutta, four acceleration evaluations per step, far smaller
  local truncation error than either Euler variant.

## Run it

```bash
dotnet run --project labs/02-the-integrator-zoo -c Release
```

## Section A — one Mercury year, three integrators, three timesteps

```
=== One Mercury year (7.60052e6 s), three integrators, three timesteps ===

dt (s)    explicit Euler        semi-implicit (game)    RK4
3600      3.481E-002            9.114E-012              -3.912E-014
600       6.156E-003            5.202E-015              2.913E-015
60        6.225E-004            -4.286E-014             1.810E-014
```

Explicit Euler leaks **3.5% of Mercury's orbital energy in a single year** at dt = 3600 s (the
kind of dt the game's projection code uses for a distant cruise) — and it always leaks in the
same direction: forward Euler systematically adds energy to an orbit, spiraling it outward,
because it evaluates acceleration at the position *before* the step instead of after. Halving
dt roughly halves the error (6.156E-3 at 600 s, 6.225E-4 at 60 s) — first-order behavior, as
expected, but even at dt = 60 s explicit Euler is still leaking energy 10 orders of magnitude
faster than the game's own integrator at the *coarsest* dt tested. **This is why the game does
not use plain Euler**, full stop, at any dt anyone would want to run at warp speed.

Semi-implicit and RK4 are both indistinguishable from floating-point noise (1e-11 to 1e-15) over
a single Mercury year at every dt tested here. One year isn't a hard test for either of them —
which is exactly why lesson B pushes further.

## Section B — the long haul: 50 Mercury years at dt = 600 s

```
=== BREAK IT: the long haul — 50 Mercury years at dt = 600 s ===

integrator          E_start (J/kg)      E_end (J/kg)        rel. drift
explicit Euler      -1.145851E+009      -9.195486E+008      1.975E-001
semi-implicit       -1.145851E+009      -1.145851E+009      3.986E-012
RK4                 -1.145851E+009      -1.145851E+009      -6.388E-014
```

Fifty orbits at a dt that looked perfectly safe in Section A, and explicit Euler has now handed
Mercury **19.75% more orbital energy** than it started with — headed straight for an unbound
escape trajectory if you let it keep running. Semi-implicit and RK4 are both still at the noise
floor. At *this* resolution neither of the two "good" integrators shows any real weakness yet —
so we went looking for where RK4's weakness actually lives.

## Section C — does RK4 ever drift? (it does — just very, very slowly)

RK4 is not symplectic. Unlike semi-implicit Euler, it has no mathematical guarantee that its
energy error stays bounded forever — only a small *local* truncation order (dt⁵ per step).
Whether that adds up to a genuine secular (ever-growing) drift, or stays a bounded wobble like
semi-implicit's, is an empirical question at real orbital timescales. We went and checked.

**Hold dt fixed at 3600 s, stretch the duration:**

```
years     dt (s)    RK4 drift
500       3600      -2.038E-011
2000      3600      -8.135E-011
8000      3600      -3.256E-010
```

Quadrupling the duration quadruples the drift, almost exactly, every time. That is the
signature of a genuine **secular** term — RK4's tiny per-orbit energy error does accumulate
linearly with time, it just accumulates from an astronomically small starting point (about
2e-14 relative error per Mercury year at this dt).

**Hold duration fixed at 50 years, coarsen dt — RK4 vs. semi-implicit:**

```
dt (s)    RK4 drift (50 yr)     semi-implicit drift (50 yr)
3600      -2.016E-012           1.652E-010
7200      -6.514E-011           7.972E-010
14400     -2.086E-009           2.176E-008
28800     -6.675E-008           7.760E-007
```

Surprise worth sitting with: at every dt tested here, over 50 years RK4's drift is *smaller in
magnitude* than semi-implicit's, by roughly one order of magnitude — RK4 really is more accurate
per step, exactly as advertised. So why does the game use semi-implicit at all?

**Because "smaller" and "bounded" are different guarantees, and only one of them survives
arbitrarily long play sessions:**

```
years     semi-implicit drift
50        7.760E-007
200       1.636E-005
800       2.309E-004
3200      4.526E-005
```

This is the actual test. If semi-implicit's error were secular like RK4's, quadrupling the
duration four times over (50 → 3200 years, a 64x stretch) should have driven the drift up by
roughly 64x too. Instead it went up, up, and then *down* — 3200 years shows a smaller drift than
800 years did. That's not accumulation, that's oscillation: semi-implicit's energy error is
bounded, wobbling around the true value rather than walking away from it, exactly as the
symplectic-integrator literature promises. RK4's error, meanwhile, is smaller *right now*, but
it is quietly and monotonically walking away from the truth every single orbit, and nothing
stops it. Over a save file that runs for simulated millennia at high warp, "small and growing
forever" eventually loses to "bounded and never growing" — and per-step cost matters too: RK4
needs four gravity evaluations per step to semi-implicit's one, for an accuracy advantage that,
per Section A and B, the game never actually needs at any dt it runs at. That's the real
argument for what `Simulator.cs` shipped with.

## Break it yourself

1. **Already above (Section A):** at dt = 3600 s, explicit Euler leaks 3.5% of Mercury's energy
   in one year. Try `dt = 200000` (near Mercury's own orbital period / 40) and see how fast
   explicit Euler blows up — does the ship escape the Sun entirely within a handful of orbits?
2. **Already above (Section C):** RK4's secular drift is real but tiny. Push the duration table
   to `years = 100000` at `dt = 3600` and estimate: at this rate, how many millennia of Mercury
   orbiting would it take for RK4's *relative* energy error to reach semi-implicit's typical
   1e-6-ish wobble amplitude at a similarly coarse dt?
3. **On your own:** swap Mercury's orbit for Neptune's (`orbitRadiusM`/`orbitPeriodS` from
   `scenarios/sol.json`) — a much longer dynamical timescale. Do explicit Euler and RK4 need the
   *same* dt values to misbehave the same way, or does the safe dt range shift with the orbit?

## See also

- `src/SpaceSails.Core/Simulator.cs` — `StepBy`, the one line that separates semi-implicit from
  explicit Euler.
- Lesson 1 (`labs/01-falling-is-orbiting`) — the same integrator checked against vis-viva on a
  single fall and a single circular orbit, at the game's live dt = 1 s.
- Lesson 3 (`labs/03-time-step-is-a-lie-you-choose`) — dt isn't fixed at all in the live game;
  `ProjectAdaptive` picks it per-step from the local dynamical time, and that choice matters most
  exactly where this lesson's fixed-dt tables would have started lying: a fast flyby.
