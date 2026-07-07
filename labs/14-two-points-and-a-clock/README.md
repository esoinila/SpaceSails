# Lesson 14 — Two points and a clock

*Lambert's problem is the navigator's boundary value problem: where you are, where you must
be, and the clock. The textbook solves it exactly — for a universe with one attracting body.
Ours has nine. Lambert proposes; the integrator disposes.*

```bash
dotnet run --project labs/14-two-points-and-a-clock -c Release
```

## Why this lesson exists

Lesson 13 solved the gunner's boundary value problem by shooting — guess, fly, measure the
miss, let Newton fix the guess. This lesson graduates the same method from a 12-hour slug to
a 210-day passage: **be at Mars's capture doorstep on day 310**. Lesson 5 attacked transfers
with a grid search; here we finally meet the tool the textbooks reach for first — **Lambert's
problem** (Curtis ch. 5) — and then do to it what this lab does to every formula: fly it
through the real integrator and price the difference.

The honest disclosure comes first: the "analytic" Lambert solution is *itself* an iteration —
a one-dimensional root-find on the universal variable z (47 bisection steps below). The
textbook method and the shooting method differ not in kind — both iterate — but in what they
iterate **through**: Lambert iterates through two-body algebra; shooting iterates through the
world.

## The standard-textbook take

Curtis ch. 5: given r₁, r₂ and the time of flight, the universal-variable formulation finds
the connecting conic — Algorithm 5.2, the workhorse of every porkchop plot ever published.
It is exact for two-body arcs, brilliant, and blind to every other mass in the sky. The real
profession does exactly what Section C does: Lambert as the seed, then a **differential
corrector** through the full force model. We are not inventing anything here — we are
reproducing standard mission design with the game's own engine as the force model.

## What the game simplifies away

2-D ecliptic, circular coplanar rails (lesson 9), and a departure state that is the game's
spawn point: 5,000,000 km outside Earth's orbit *with Earth's full orbital speed* — remember
that detail, Section D turns it into a plot twist.

## The numerical experiment

The contract: depart day 100, be at Mars's capture doorstep (500,000 km out, lesson 7's
window scale — no navigator aims for the core of a planet) on day 310. TOF 210 days.

### A — Lambert's answer, checked in Lambert's own universe

```
universal-variable root-find: 47 bisection iterations on z (yes, the 'analytic' method iterates too)
Lambert departure velocity: 31208.0 m/s heliocentric
  dv1 vs Earth = 8368.7 m/s, dv2 vs Mars at arrival = 4388.3 m/s

Flown in a SUN-ONLY world — Lambert's own universe — at successively finer cruise steps:
max dt (s)    miss at day 310 (km)    endpoint vel vs Lambert v2 (m/s)
      3600                 299,272                               26.36
       600                  49,881                                4.39
        60                   4,988                                0.44
```

The residual shrinks linearly with dt — divide the step by six, the miss divides by six. That
is the *integrator's* first-order truncation (lesson 2), not Lambert's error. In the
one-attractor universe Curtis assumes, the formula is exactly right; the disagreement above
is the game's cruise dt, priced.

### B — the same velocity, flown in universes that actually have planets

```
world                         miss at day 310 (km)
sun only                                   299,272
sun + Earth + Mars                         502,074
all nine bodies                            443,569
```

The ship departs 5,000,000 km from Earth and still feels her; Mars pulls at the far end. The
two-body answer is now wrong *structurally*, not numerically — no amount of dt-refinement
fixes a missing force. And note the fine print: adding the other six planets made the miss
**smaller** (502,074 → 443,569 km). Perturbations are signed — they interfere, they don't
just pile up. A "more complete" error estimate that only added magnitudes would have lied to
you.

### C — shooting through the real world, seeded by Lambert

```
iter       miss (km)
   0       443,568.7
   1           441.7
converged: True (tolerance 5,000 km)
correction the real world demanded on top of Lambert: 14.12 m/s
```

One Newton step, lesson 13's punchline again — on a smooth arc the miss is nearly linear in
the guess. The entire nine-body correction on top of two-body Lambert is **14 m/s on a
31 km/s departure**: the formula was 99.95% right, and the last 0.05% was 443,569 km.

The seed is not a nicety:

```
naive straight-line seed: 17981.6 m/s heliocentric, 29098.8 m/s away from Lambert's answer
iter       miss (km)
   0   232,174,873.7
  15   222,096,784.4
  30   220,144,954.5
 ...
 149   228,683,606.3
converged: False — 150 Newton iterations (~450 simulator flights) vs 2 (~6 flights) from the Lambert seed
```

From a constant-velocity gunnery guess (lesson 13's Section A trick — it worked there because
12 hours is short), Newton wanders a folded miss-landscape 1.5 AU from the answer and *never
arrives*. A good seed is not most of the meal — it is the difference between eating and not.

### D — the porkchop plate: every contract you could have signed

Lambert per cell — the seed's whole value is being this cheap: ~50 bisection steps of algebra,
no integration. Total dv = leave Earth's orbit + match Mars's.

```
dep day |    120    160    200    240    280    320    360    400  <- TOF (days)
------------------------------------------------------------------
      0 |   21.6   12.8    8.1    5.8    5.2    5.9    7.6    9.9
     76 |   13.1   10.3    9.2    8.7    8.3    7.9    7.6    7.9
    152 |   31.4   26.9   23.7   21.0   18.6   16.5   14.5   12.8
    228 |   56.9   48.0   41.6   36.4   31.9   27.9   24.4   21.3
    304 |   57.1   57.5   60.0   53.0   46.9   41.4   36.4   31.9
    380 |   54.6   40.0   43.0   69.2   62.2   55.7   49.7   44.1
    456 |   55.0   37.2   29.4   31.7   50.3   69.8   63.3   57.1
    532 |   54.1   35.8   26.0   21.7   23.9   35.2   63.2   70.0
    608 |   50.0   32.7   23.0   17.6   15.8   18.2   25.6   43.0
    684 |   40.9   26.3   18.0   13.2   11.0   11.3   14.1   19.5
    760 |   26.2   16.0   10.3    7.3    6.2    6.8    8.5   11.3

cheapest cell: depart day 0, TOF 280 days, total dv 5.20 km/s
cheapest cell VERIFIED by flying it: lands 3,258 km from Mars, endpoint velocity 0.15 m/s from Lambert's v2
  its split: dv1 = 2704.0 m/s, dv2 = 2491.6 m/s
```

Read the ridge: the plate is the Earth–Mars synodic cycle (lesson 5's 779.9 days) drawn in
delta-v — cheap valleys at days 0 and 760, a 70 km/s wall mid-cycle where the phasing is
hopeless. This is what "transfer window" actually *means*, computed.

And the plot twist: the Hohmann analytic total for this pair (lesson 5) is **5.59 km/s**, yet
the plate's verified floor is **5.20 km/s** — below the two-impulse minimum? No. Hohmann is
the minimum *between the two circular orbits*; the plate prices the **game's actual departure
state**, which spawns you 5,000,000 km up the sun's hill with Earth's full speed — roughly
960 m/s of orbital-energy head start the textbook geometry doesn't include. The cell was
flown to make sure (3,258 km landing, endpoint velocity 0.15 m/s off the algebra): the
formula answers the question it was asked. The plate answers the question the captain asked.

## Break it yourself

1. **Aim for the core.** Set the doorstep offset to 0 and rerun Section C. The aim point now
   sits where Mars pulls at hundreds of m/s² (μ_Mars/r² ≈ 428 m/s² at 10,000 km), the miss
   becomes violently nonlinear in the guess, and Newton's clean one-step convergence goes
   feral. Watch the trace, then explain why lesson 13's trust region can't save a solver
   whose *target* is the singularity.
2. **The long way round.** Flip the prograde test (`cross < 0`) in `Lambert` and rerun
   Section A: same two points, same clock, retrograde arc. Compare dv1 against 8,368.7 m/s
   and write down why cargo doesn't fly against the traffic.
3. **Cheat the root-find.** The probe validates every root by reconstructing the achieved
   TOF and rejecting lies > 1 s (bisection *always* returns a z; that doesn't make it a
   solution). Extend `zHi` past (2π)² into multi-rev territory, ask for dep day 0 / TOF 700
   days, and see whether the residual check catches what the bisection drags in.
