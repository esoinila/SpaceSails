# Lab 09 — What the rails hide

*The big honest one. Curtis's whole book (and most of this lab series) works in the two-body or
patched-conic regime because that's where closed-form orbital mechanics lives. Real N-body
dynamics — everything pulling on everything — is chaos-theory territory Curtis explicitly steps
around. This lesson builds a genuine N-body integrator from scratch, in the probe, and measures
exactly how much the game's rails ephemeris (`CircularOrbitEphemeris`) is lying, and where that
lie actually costs a player something.*

## The idea

Read `CircularOrbitEphemeris.cs` and the claim is right there in the code, not just the docs:
`Position(bodyId, simTime)` computes a body's position from only its own orbit radius, period,
phase, and its parent's position. `simTime` is the only input that varies. There is no loop over
other bodies anywhere in that method, and nowhere else in `SpaceSails.Core` does one celestial
body's gravity affect another's trajectory — only `Simulator.GravitationalAcceleration` sums
bodies' pull, and only onto the ship. **Bodies are rails: exactly where the formula says, forever,
no matter what mass is nearby.** Real solar systems are not like this — Jupiter tugs on Saturn,
the Sun itself gets tugged back and wobbles off dead-center. This lab writes a true self-contained
N-body integrator (semi-implicit Euler, every pair of massive bodies attracts, including the Sun)
for the Sun and all eight planets plus a ship, and quantifies the lie: per-planet position
drift over 10 years, ship trajectory divergence over one real transfer, sensitivity to a 1-meter
initial-condition change, and what happens when Jupiter's gravity is switched off.

**Performance note — what was picked and why:** dt = 600 s (lesson 02's own proven-safe choice for
the game's semi-implicit integrator on real solar-system timescales — no visible energy drift over
50 Mercury years there). Duration for the long-baseline sections is capped at 10 years. With at
most 11 point masses (~55 pairs after the i<j symmetry trick) per force evaluation, the whole
probe — four separate N-body integrations, one of them 525,960 steps long — runs in about 2-4
seconds on a normal machine, comfortably inside "the probe must still run in seconds."

## Run it

```bash
dotnet run --project labs/09-what-the-rails-hide -c Release
```

## Section (a) — rails vs. N-body planet positions over 10 years

```
dt = 600 s, 52596 steps/year, 10 years -> 525,960 total steps.

body      yr 1        yr 2        yr 3        yr 4        yr 5        yr 6        yr 7        yr 8        yr 9        yr 10
sun       1.13E+008   4.36E+008   9.46E+008   1.60E+009   2.34E+009   3.11E+009   3.84E+009   4.49E+009   5.00E+009   5.37E+009
mercury   1.60E+008   2.48E+008   5.59E+008   1.42E+009   2.67E+009   3.97E+009   4.90E+009   5.12E+009   4.62E+009   3.94E+009
venus     1.47E+008   3.40E+008   1.09E+009   1.56E+009   2.26E+009   3.42E+009   3.46E+009   4.86E+009   4.96E+009   5.11E+009
earth     5.02E+007   3.19E+008   7.93E+008   1.44E+009   2.19E+009   3.00E+009   3.80E+009   4.52E+009   5.11E+009   5.56E+009
mars      1.44E+008   4.57E+008   9.60E+008   1.62E+009   2.35E+009   3.10E+009   3.87E+009   4.47E+009   5.02E+009   5.36E+009
jupiter   2.10E+008   8.88E+008   2.13E+009   3.99E+009   6.29E+009   8.61E+009   1.04E+010   1.12E+010   1.07E+010   9.00E+009
saturn    4.07E+008   1.61E+009   3.57E+009   6.28E+009   9.83E+009   1.44E+010   2.00E+010   2.67E+010   3.45E+010   4.33E+010
uranus    1.56E+007   6.03E+007   1.29E+008   2.16E+008   3.16E+008   4.26E+008   5.46E+008   6.77E+008   8.20E+008   9.77E+008
neptune   6.24E+006   2.48E+007   5.54E+007   9.78E+007   1.52E+008   2.19E+008   2.99E+008   3.91E+008   4.95E+008   6.12E+008
```

Units: meters. **The Sun row is not zero.** In the real N-body sum the Sun itself gets pulled by
the giant planets and wobbles off the origin rails assumes it sits at — that wobble is real solar
physics (the reflex motion real telescopes use to find exoplanets), and rails erases it by fiat.
**Saturn drifts the most of any planet** — 43.3 million km by year 10, over an order of magnitude
more than Uranus or Neptune despite being closer to the Sun — because Jupiter's `mu` dwarfs every
other planet's and Saturn is the giant planet standing closest to it. Compare against the
break-it below: this is not a coincidence.

## Section (b) — one Earth→Mars transfer, the same plan, rails vs. N-body

```
Plan: 2 burn node(s), estimated transfer 31.0 days.
  - t=3600 s, Accelerate, 10 pulse(s)
  - t=2682000 s, Decelerate, 10 pulse(s)

Rails final position:   -2.278600E+011, 8.985780E+010 m
N-body final position:  -2.278600E+011, 8.985779E+010 m
Ship divergence at arrival: 9.215E+003 m (5.156E-008 = a 5.156E-006% fraction of the Earth-Mars distance at t=0).
For scale: 5.348E+010 m was the planner's own accepted miss distance for this route.
```

The exact same `ManeuverPlan` — the game's own `RoutePlanner` output, planned against the rails
ephemeris — flown through both a rails `Simulator` and this lesson's from-scratch N-body
integrator, diverges by about 9.2 km over a 31-day, ~228 million km transfer. That's roughly five
millionths of a percent of the trip distance, and more than six orders of magnitude smaller than
the planner's own accepted miss tolerance (53.5 million km) for this route. **For a single
inner-system transfer, rails is not just a convenient lie — it is an utterly negligible one.**

## Section (c) — sensitivity to initial conditions: two ships, 1 m apart, 10 years

```
year    separation (m)    growth since previous year (x)
0       1.000E+000        -
1       1.084E+001        10.84
2       1.929E+001        1.78
3       3.138E+001        1.63
4       3.878E+001        1.24
5       5.203E+001        1.34
6       5.845E+001        1.12
7       7.235E+001        1.24
8       7.842E+001        1.08
9       9.241E+001        1.18
10      9.859E+001        1.07

Total growth over 10 years: 9.859E+001x. Rough Lyapunov-exponent estimate ln(growth)/time = 1.455E-008 /s (e-folding time ~ 796 days)
```

Two ships, coasting ballistically from a Mars-orbit-radius circular start, riding the same N-body
planetary field (they don't feel each other or perturb the planets — the test-particle
assumption, so one planetary integration serves both). Starting 1 meter apart, they are **98.6
meters apart ten years later** — still a small number in absolute terms, but the growth is real,
monotonic, and entirely explained by gravity in a deterministic integrator with no other source
of randomness anywhere in this code. This is a single-trajectory-pair estimate, not a converged
Lyapunov spectrum, but the direction is the real point: any fixed error — including the rails
approximation itself — compounds over a long enough baseline in this kind of system.

## Break it — remove Jupiter, watch Saturn wander

```
year    Saturn drift from removing Jupiter (m)
1       5.873E+007
2       2.906E+008
3       8.161E+008
4       1.736E+009
5       3.070E+009
6       4.816E+009
7       6.973E+009
8       9.538E+009
9       1.250E+010
10      1.583E+010
```

With Jupiter's `mu` zeroed (it still exists and still gets pulled, it just stops pulling on
anyone), Saturn's own position — compared against the full N-body run from Section (a), which
already includes Jupiter's real pull — drifts by 58.7 million km in year 1 alone, and 15.8 *billion*
meters (nearly 0.1 AU) by year 10. That single missing coupling alone accounts for about 37% of
Saturn's total rails-vs-N-body divergence found in Section (a) (43.3 billion m at year 10) — more
than any other single planet's pull on Saturn plausibly could, given seven other planets are
splitting the remaining ~63%. Jupiter on Saturn is not one perturbation among many; it's the
single largest one in this system.

## Conclusion — how big is the lie, and where does it matter?

Rails is a deliberate, honest simplification — the same one real mission planners make with
patched conics before a numerical refinement pass, and the same one this lab series has been
making since lesson 01. Section (a) says exactly how big it is per planet over 10 years (meters
to tens of billions of meters, planet-dependent). Section (b) says it is utterly negligible for a
single inner-system transfer measured in months — a fraction of a percent of the Earth-Mars
distance, dwarfed by the planner's own accepted miss tolerance by six orders of magnitude. The
break-it says the missing physics is concentrated in specific planet-planet couplings (Jupiter on
Saturn is the single largest one, at over a third of Saturn's total divergence by itself), not
spread evenly across the system. Section (c) is the sharper warning: this
is a chaotic system, so *any* fixed error compounds given enough time, including the rails
approximation itself. **Rails is safe for the game's actual playable timescale — single transfers,
close flybys measured in days — and it is a real, quantified lie exactly where the game never
asks it a question: multi-decade outer-system trajectories and precision long-baseline flybys past
the giants.**

## Break it yourself

1. **Already above:** removing Jupiter and watching Saturn wander (58.7 million km in year 1,
   15.8 billion m by year 10).
2. **On your own:** the code already computes `nbPosByYear` for every planet in Section (a). Add a
   second break-it that zeroes Saturn's `mu` instead of Jupiter's, and check Jupiter's own drift —
   is the Jupiter→Saturn coupling reciprocal in size, or lopsided the way you'd expect from their
   very different masses?
3. **On your own:** Section (c) starts both ships at Mars's orbit radius, far from any planet.
   Rerun the same sensitivity test starting near Jupiter instead (say, at twice Jupiter's own
   orbit radius) — does the 1-meter separation grow faster or slower near a giant planet than it
   does in the calmer middle system?

## See also

- `src/SpaceSails.Core/CircularOrbitEphemeris.cs` — the rails claim, verified directly by reading
  `Position`.
- `src/SpaceSails.Core/Simulator.cs` — `GravitationalAcceleration`, confirming gravity is only ever
  summed onto the ship, never between celestial bodies.
- `src/SpaceSails.Core/RoutePlanner.cs` — the real `ManeuverPlan` this lesson's Section (b) flies
  through both gravity models unchanged.
- Lesson 02 (`labs/02-the-integrator-zoo`) — where dt = 600 s was established as safe for
  semi-implicit Euler on real solar-system timescales; this lesson reuses that finding rather than
  re-deriving it.
- Lesson 07 (`labs/07-hill-spheres-and-bus-stops`) — the same three-body-perturbation flavor
  (Section A's stability islands) at planetary-moon scale instead of solar-system scale.
