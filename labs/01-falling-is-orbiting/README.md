# Lab 01 — Falling is orbiting

*Standard physics. Curtis, "Orbital Mechanics for Engineering Students," ch. 2.*

## The idea

An orbit is not a track a ship glues itself to. It is a fall. Newton's cannonball thought
experiment says it plainly: fire a cannonball sideways fast enough and, as it falls, the ground
curves away from it exactly as fast as it drops — so it never lands. Give it *more* than that
speed and it climbs into an ellipse; give it *less* and the ellipse tucks in closer on one side.
Give it *zero* sideways speed and it just falls, straight down, into whatever it was orbiting.

This lab computes all three cases with `SpaceSails.Core.Simulator` — the exact fixed-step,
semi-implicit-Euler integrator the game flies ships with (see `Simulator.cs`). No shortcuts:
we drop a ship at 1 AU from the Sun (using the Sun's real `mu` and Earth's real orbit radius
from `scenarios/sol.json`) and watch the integrator's own numbers.

## The textbook take

Curtis ch. 2 derives **vis-viva** from the two-body equation of motion plus conservation of
specific orbital energy:

```
v² = mu * (2/r - 1/a)
```

where `a` is the orbit's semi-major axis. This one formula covers every conic: plug in `a = r`
for a circle, `a = r0/2` for a ship dropped from rest at `r0` (Curtis calls this the
*rectilinear* or *radial* trajectory — a real solution to the two-body problem, just squashed
flat to a line, with apoapsis `r0` and periapsis `0`), and any ellipse's own `a` for a burn away
from either extreme.

**What the textbook simplifies away:** vis-viva is exact for the *continuous* two-body problem.
The game does not integrate continuously — it takes fixed-size steps (`dt`), and every real
integrator accumulates *some* per-step truncation error. The interesting question isn't "does
vis-viva hold" (that's calculus, it's not in question) — it's "how closely does *this specific,
shippable, fixed-step integrator* track the closed form, and at what dt does the answer stop
being good enough to trust." That's a numerical question, and numerical questions get answered
by running the code.

## Run it

```bash
dotnet run --project labs/01-falling-is-orbiting -c Release
```

## Section A — radial free-fall, checked against vis-viva

A ship dropped at rest at `r0 = 1 AU` has zero angular momentum, so its trajectory is the
degenerate ellipse `a = r0/2` (apoapsis `r0`, periapsis `0`, by construction — not fit to the
data). We integrate the fall with the game's own live-play dt (1 s — see `docs/m4-spec.md`:
"real Simulator ship, dt = 1 s behind an accumulator"), record speed at three checkpoint radii,
and compare against `v = sqrt(mu * (2/r - 2/r0))`:

```
dt = 1 s (the game's live dt)
radius (AU)   v_computed (m/s)    v_vis-viva (m/s)    rel. error    sim days
0.75          24318.920703        24318.925974        2.167E-007    39.32
0.50          42121.603512        42121.615372        2.816E-007    52.84
0.25          72956.730481        72956.777921        6.502E-007    60.85
```

Sub-microstrain agreement — semi-implicit Euler at 1 s tracks a multi-week fall to seven
significant figures. Free fall from 1 AU to the Sun's center takes about 64.5 days
(`t = (π / (2√2)) · sqrt(r0³/mu)`, Kepler's third law applied to the degenerate ellipse); the
0.25 AU checkpoint arrives at day 60.85, most of the way through — the ship spends most of its
time near apoapsis, where it's barely moving, and only plunges fast at the very end. That's
also a real, computed number, not a hand-wave: falling is *slow* far out and *fast* close in,
exactly as `1/r²` gravity predicts.

**Break-it #1 — double the timestep:**

```
dt = 2 s (BREAK IT: doubled dt)
radius (AU)   v_computed (m/s)    v_vis-viva (m/s)    rel. error    sim days
0.75          24318.915431        24318.925974        4.335E-007    39.32
0.50          42121.591652        42121.615372        5.631E-007    52.84
0.25          72956.683042        72956.777921        1.300E-006    60.85
```

Doubling dt roughly doubles the relative error at every checkpoint — first-order-in-dt
behavior, exactly what you'd expect from Euler-family integration (semi-implicit Euler is
still only first-order accurate per step; it's the *symplectic*, bounded-long-term-energy-error
property that makes the game trust it over many orbits — lesson 2 measures that directly).
Both errors are still small at these dt values; lesson 3 finds where a fixed dt stops being
small, on a much faster flyby.

## Section B — circular orbit holds its speed

Now give the ship sideways speed `v_circ = sqrt(mu/r0)` instead of zero. Sampling a quarter,
half, and full orbit later (using the *initial* `a = r0` in vis-viva — any gap between predicted
and computed speed is the integrator's own energy drift, not a modeling shortcut):

```
orbit fraction  v_computed (m/s)    v_vis-viva (m/s)    rel. error    radius (AU)
0.25            29784.482829        29784.482829        2.321E-015    1.000000
0.50            29784.479864        29784.479864        9.991E-014    1.000000
1.00            29784.479864        29784.479864        3.540E-013    1.000000
```

Relative error at the 1e-13–1e-15 level — floating point noise, not integration error. A
circular orbit is the one case where semi-implicit Euler's symplectic structure shows up
almost perfectly: energy barely drifts at all over a full year, at dt = 1 s. (This is exactly
why `CircularOrbit_StaysCircular_ForThirtyDays` in `SimulatorTests.cs` can assert a drift bound
of 0.5% at dt = 60 s and pass comfortably — this lab measures the same effect at finer
resolution.)

**Break-it #2 — the ±10% pulse turns a circle into an ellipse:**

The game changes ship speed only in ±10% multiplicative pulses (see `ManeuverPlan`). Apply one
pulse to an exactly-circular orbit and it's no longer circular. We compute the resulting
ellipse two independent ways — closed-form (vis-viva + angular momentum conservation gives `a`
and `e` directly) and by literally scanning the integrator's radius over one full orbit — and
they agree:

```
speed x1.10 (accelerate pulse)
  eccentricity e = 0.210000, semi-major axis a = 1.265823 AU
                        periapsis (AU)    apoapsis (AU)
  closed form           1.000000          1.531646
  integrator scan       1.000000          1.531646

speed x0.90 (decelerate pulse)
  eccentricity e = 0.190000, semi-major axis a = 0.840336 AU
                        periapsis (AU)    apoapsis (AU)
  closed form           0.680672          1.000000
  integrator scan       0.680672          1.000000
```

A +10% pulse at 1 AU sends periapsis staying put (you pulsed at periapsis — you can't get any
closer without a retrograde burn) and apoapsis stretching out to 1.53 AU. A -10% pulse leaves
apoapsis at 1 AU (where you fired the retro-burn) and drops periapsis to 0.68 AU. Both numbers
came out of the *same* two formulas Curtis gives for any conic, and the "integrator scan" row
is not a formula at all — it's the fixed-step simulator finding its own min/max radius over a
full revolution, and it lands on the same six digits. This is your first look at what lesson 4
("The ±10% pulse") spends a whole lesson on: what orbits are and aren't reachable one pulse at
a time.

## Break it yourself

1. **Already above:** double dt on the free-fall check (Break-it #1) — try quadrupling it
   (`dt = 4`) and see whether the error keeps scaling linearly, or something else takes over.
2. **Already above:** the ±10% pulse (Break-it #2) — try ±20% or ±30% in `Probe.cs` and predict
   before running: at what pulse fraction does periapsis drop inside the Sun's body radius
   (`6.9634e8` m, about 0.00465 AU)? Then run it and check your prediction.
3. **On your own:** change `r0` from 1 AU to 4 AU (Jupiter-ish) and predict the new free-fall
   time using Kepler's third law scaling (`t ∝ r0^1.5`) before you touch the code. Then edit
   the constant and rerun — does the printed sim-day column agree with your prediction?

## See also

- `src/SpaceSails.Core/Simulator.cs` — the integrator under test.
- `tests/SpaceSails.Core.Tests/SimulatorTests.cs` — `CircularOrbit_StaysCircular_ForThirtyDays`
  and `ShipAtRest_FallsSunward` are the game's own regression tests for exactly these two cases.
- Lesson 2 (`labs/02-the-integrator-zoo`) — why semi-implicit Euler, and what Euler and RK4
  would have done instead, over a much longer run.
