# Lesson 13 — Shooting, literally

*The firing solution is a boundary value problem. The gun deck solves it the way numericists
have since the slide-rule era: guess, fly it, measure the miss, let Newton fix the guess.*

```bash
dotnet run --project labs/13-shooting-literally -c Release
```

## Why this lesson exists

Every transfer you plotted in lessons 4–5 was secretly a **boundary value problem** — *be at
that place at that time* — and you solved it by scanning candidates. That's fine when you
have all day. The war room's fire control (`Core/FireControl.cs`) doesn't: when you pick a
point on the prey's predicted track and press COMPUTE, it uses the **shooting method** —
guess a launch bearing and mass-driver charge, fly the slug through the *real* deterministic
`Simulator`, measure the 2-D miss at t_hit, and take a damped Newton step on the guess. Two
unknowns (bearing, charge) for two constraints (the aim point's x and y): with a fixed
muzzle speed an exact hit would be measure-zero luck, which is exactly why the driver's
charge is adjustable.

The iterations you watch converging in the war room's CALCULATING FIRING SOLUTION panel are
this lesson's Section B, verbatim.

## The standard-textbook take

Curtis, *Orbital Mechanics for Engineering Students*: Lambert's problem (ch. 5) is this same
BVP solved semi-analytically for two-body arcs; shooting methods for BVPs are the numerical
staple (any numerical-methods text, e.g. Ascher & Petzold ch. 7). Lambert would be faster
here — and wrong the moment a second attractor matters, which is lesson 6's whole point. The
game shoots through the integrator so the solver can never disagree with the world.

## What the game simplifies away

2-D ecliptic, point-mass slug (no drag, no attitude dynamics), impulsive launch, and the
target's future is a *dead-reckoned prediction*, not truth — which is precisely what
Section C prices.

## The numerical experiment

Geometry: we ride a circular 1 AU orbit; the prey drifts 202,237 km ahead on nearly the same
orbit. We aim at its predicted position 12 hours from now.

### A — "just aim at it" fails, even aiming where they WILL be

```
prey now:            202,237 km away
aim point (t+12 h):  1,503,651 km away, on the prey's PREDICTED track
straight-line charge: 5272.1 m/s at bearing 109.194 deg
straight-line miss:   5996.2 km  <- gravity bent the shot
```

Leading the target (lesson-one gunnery) still misses by six thousand kilometres, because the
sun bends a 12-hour flight.

### B — Newton turns the miss into the next guess

```
iter   bearing (deg)   charge (m/s)     miss (km)
   0      109.193662        5272.14      5996.235
   1      107.767583        5226.91        85.541
converged: True (tolerance 100 km)
solution:  bearing 107.7676 deg, charge 5226.9 m/s, flight 12.0 h
```

One Newton step. That is not a typo — on a smooth two-body arc the miss is nearly linear in
the guess, and Newton eats near-linear problems in a bite. (The war room's longer traces come
from nastier geometry: multi-attractor arcs, guesses pinned at the charge limit, the trust
region biting.) Each iteration cost three simulator flights: the residual plus two
finite-difference columns of the 2×2 Jacobian.

### C — the solver is exact; the FIX is not

The 85 km residual above is noise. The honest error budget is the target *track's*
uncertainty cone at t_hit (lesson 8, including the M28 impulse term):

```
   fix age    cone at t_hit (km)
       0 h               554,734
       6 h             1,037,053
      24 h             3,323,818

And per LEAD TIME with a fresh fix — why gunners shoot short leads:
    lead    cone at t_hit (km)
     1 h                34,010
     3 h                93,695
     6 h               212,383
    12 h               554,734
```

Read that top row again: even with a *fresh* fix, a 12-hour lead carries half a million
kilometres of honest dispersion — the prey is allowed to burn after you look away. Shoot
short leads, and keep the telescope on her: fire-control quality IS track quality. One
weapon system across two stations.

### D — a locked solution goes stale, measured

```
validity window reported by the solver: 120 s
fire delay (s)     miss (km)
             0          85.5
            60         387.9
           300        1641.5
           900        4779.8
          3600       18887.6
```

The T-60 s lock the war room grants you is comfortably inside the reported 120 s window;
dawdle fifteen minutes and the same bearing-and-charge misses by 4,780 km.

### E — the driver's reach is a boundary, not a bug

```
aim 943,398 km out at t+12 h with an 8 km/s driver:
converged: False, best-effort miss 356,465 km, charge pinned at 8000 m/s
```

Newton drove the charge to its 8 km/s wall and reported the best it could do. The war
room's NO SOLUTION message is this result, verbatim.

## Break it yourself

1. **Starve the trust region.** In `FireControl.Solve` the bearing step is clamped to
   ±0.5 rad. Rebuild with ±0.01 and rerun Section B — count how many iterations the same
   convergence now takes, and find the geometry where it stops converging inside
   `maxIterations` entirely.
2. **Lie to the solver.** Feed Section B an aim point from a *stale* prediction (compute the
   track from an observation 6 h old, but fly the "true" prey from its real state) and
   measure the actual closest approach of the solved slug to the real prey. Compare it to
   Section C's 6 h cone — the cone should have warned you.
3. **Beat Lambert.** Add a third body (copy Mars from lesson 6's ephemeris) between shooter
   and aim point, and rerun. The straight-line miss barely changes; the *converged* solution
   shifts. That shift is exactly what a two-body Lambert solver would have gotten wrong.
