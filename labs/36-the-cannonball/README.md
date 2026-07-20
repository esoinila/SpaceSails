# Lesson 36 — The cannonball (kinetic impactor)

*The ablation gig drills and boils. The oldest deflection in the book is cruder: throw a mass at the rock
and let momentum do it. But a bare cannonball is only half the story — when it hits, it blows a plume of
ejecta off the surface, and the recoil of THAT plume can trebles the kick (DART measured it in 2022). And
a nudge is nothing on its own: it is the WARNING TIME that multiplies a millimetre-per-second shove into a
Ringside-clearing miss. This lesson prices the cannonball honestly, at real asteroid sizes, and answers the
question a "sacrifice a cargo pod as the slug" gig would ask: how much warning, plus how much mass, deflects
a given rock past the Exchange?*

```bash
dotnet run --project labs/36-the-cannonball -c Release
```

## Why this lesson exists

The shipped gig deflects with a drilled ablation charge and models the result as a periapsis raise measured
at the impact instant — a big instantaneous shove. The kinetic impactor is a *different* physical technique
with a *different* payoff curve: a tiny Δv that pays off through lead time. Lesson 35 certified the shove;
this one certifies the throw, so the owner can see both menus side by side before promoting either into a
gig variant. It is honest at real sizes: the gig's on-map rock is a 4000 km camera abstraction (so the
shuttle can land on it), but a real Ringside-killer is 50 m – 1 km, and that is where the cannonball's
numbers live.

## The standard-textbook take

**Momentum transfer with ejecta enhancement** (Curtis ch. 6 for the along-track orbit change; the β factor
is the DART/Dimorphos result, *Nature* 2023). A slug of mass *m* striking at closing speed *u* into a rock
of mass *M* imparts

  Δv = β · m · u / M

where **β** is the momentum-enhancement factor. β = 1 would be bare inelastic capture; but the impact
excavates ejecta whose recoil adds thrust, so β > 1. DART measured **β ≈ 3.6** on Dimorphos — the plume did
most of the work. Then the along-track leverage: a Δv applied *along the orbit* changes the period, so the
downrange miss grows as

  miss ≈ 3 · Δv · t_lead

the factor-of-3 secular term a period change accumulates. This is why deflection people obsess over warning
time: the same nudge, applied twice as early, misses twice as far.

## What the game adds that the textbook doesn't

**β as a Zubrin type axis, reconciled with DART** (owner 2026-07-20). The old intuition "a loose rubble
pile absorbs the push" is *backwards*: a looser, more volatile-rich body throws a BIGGER ejecta plume, so β
climbs. The lab costs it that way — C-type carbonaceous **β = 4.5** (over-delivers), S-type **β = 3.6**
(DART's measured class), M-type metallic **β = 1.5** (dense, little ejecta, near-inelastic). And the
"sacrifice" framing: the slug isn't a purpose-built probe, it's whatever the crew can throw — a jettisoned
**cargo pod (~20 t)** or a decommissioned **hull (~200 t)**.

## The numerical experiment

### A — asteroid mass by type + size, and the ejecta enhancement β

```
type                   ρ kg/m³       β         r=50m        r=140m        r=370m       r=1000m   (mass kg)
C-type carbonaceous       1400     4.5     7.33E+008     1.61E+010     2.97E+011     5.86E+012
S-type stony              2700     3.6     1.41E+009     3.10E+010     5.73E+011     1.13E+013
M-type metallic           5300     1.5     2.78E+009     6.09E+010     1.12E+012     2.22E+013
```

Bulk density and β are the two type levers. A 140 m S-type is 3.1×10¹⁰ kg — a stony M-type of the same size
is nearly twice the mass AND takes a third the β, so metal is the hardest kinetic target on both counts.
Mass runs as r³, so a 1 km rock is ~360× a 140 m one — size dominates everything below.

### B — the along-track leverage: warning is everything

```
  hull Δv imparted = 139.203 mm/s (β=3.6, M=3.10E+010 kg)

     lead time        miss (m)  clears Ringside?
          30 d       1.08E+006                no
         100 d       3.61E+006                no
       1.00 yr       1.32E+007                no
       3.00 yr       3.95E+007               yes
      10.00 yr       1.32E+008               yes
```

An old hull (200 t) hitting a 140 m S-type imparts **139 mm/s** — a fifth of a metre per second. Thrown 30
days out, that opens a miss of 1,080 km: the rock still hits Ringside's 30 Mm clearance. Thrown **3 years**
out, the *same* nudge opens 39.5 Mm and the port is saved. Nothing about the throw changed — only the clock.
That is the entire cannonball doctrine in five rows.

### C — the sacrifice: how much warning a pod or a hull buys

```
type                    radius   pod (yr warn)  hull (yr warn)
C-type carbonaceous       50 m            0.43            0.04
C-type carbonaceous      140 m            9.44            0.94
C-type carbonaceous      370 m          174.31           17.43
C-type carbonaceous     1000 m         3441.27          344.13
S-type stony              50 m            1.04            0.10
S-type stony             140 m           22.76            2.28
S-type stony             370 m          420.21           42.02
S-type stony            1000 m         8295.92          829.59
M-type metallic           50 m            4.89            0.49
M-type metallic          140 m          107.24           10.72
M-type metallic          370 m         1979.67          197.97
M-type metallic         1000 m        39083.01         3908.30
```

The headline table. A **50 m C-type** is a two-week problem for a hull (0.04 yr) — throw it and go. A **140 m
S-type** — the reference city-killer — wants **~2.3 years** of warning for a hull, or **~23 years** for a
mere cargo pod. Anything 370 m or bigger is decades-to-centuries for a single slug: kinetic alone doesn't
scale to the big rocks, which is exactly why the drilled ablation charge (lesson 35) exists as the
high-energy option. Metal at size is effectively unthrowable (a 1 km M-type: ~3,900 years for a hull).

### D — required slug mass at a fixed 1-year warning

```
type                    radius   slug needed (t)  = how many hulls
C-type carbonaceous       50 m               8.6               0.0
C-type carbonaceous      140 m             188.9               0.9
C-type carbonaceous      370 m            3486.2              17.4
C-type carbonaceous     1000 m           68825.4             344.1
S-type stony              50 m              20.7               0.1
S-type stony             140 m             455.3               2.3
S-type stony             370 m            8404.3              42.0
S-type stony            1000 m          165918.4             829.6
M-type metallic           50 m              97.7               0.5
M-type metallic          140 m            2144.9              10.7
M-type metallic          370 m           39593.4             198.0
M-type metallic         1000 m          781660.2            3908.3
```

Read the other way: fix the warning at one year and ask how heavy a slug it takes. A 140 m S-type wants
455 t — two-and-a-bit hulls — thrown together. A 50 m C-type falls to a single cargo pod. The "how many
hulls" column is the gig's ammunition budget in one number.

## The gameplay hook this enables

**A "sacrifice a cargo pod / an old hull as the slug" deflection variant** — the crew jettisons dead mass
onto an intercept and the mission's feasibility is set by the *warning time* the tracking gave them (a tie
to lesson 8's detection quality). The lesson the numbers teach the owner: the cannonball REWARDS WARNING
(3× per unit lead) far more than brute mass — a light pod thrown early beats a heavy hull thrown late — so a
kinetic gig is really an early-detection reward. Small rocks are cannonball food; big ones need the drilled
charge. NOT built here; certified and costed.

## Break it on purpose

1. **Kill the ejecta.** Set every β to 1.0 (bare momentum, no plume) and rerun section C: the S-type 140 m
   hull warning jumps from 2.3 to ~8.2 years. The plume DART measured is doing ~⅔ of the deflection.
2. **Aim radial instead of along-track.** Drop the `AlongTrackLeverage` from 3 to 1 (a radial push, no
   period change) and rerun section B: the 3-year row stops clearing Ringside. The factor of 3 is the whole
   reason you aim along the orbit.
3. **Halve the intercept speed.** Set `ReferenceClosingSpeed = 3000` and rerun: every required mass doubles
   and every warning time grows — Δv is linear in *u*, so a slow intercept is a heavy or early one.

## The framing rule, kept

Standard physics presented as standard: the momentum transfer and the 3·Δv·t along-track secular leverage
are Curtis ch. 6 / deflection-literature textbook, and β is the published DART/Dimorphos measurement. The
type-dependent β values are ours, but reconciled *with* the DART result (looseness amplifies) rather than
against it, and the sizes are honest city-killers, not the game's camera-scale rock. Every number above came
from running the probe — change the code and rerun; never hand-edit a table.
