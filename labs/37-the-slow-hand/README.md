# Lesson 37 — The slow hand (gravity tractor)

*No charge, no collision, nothing thrown. Park a heavy tug a short way off the rock and simply HOLD there —
the tug's own gravity, feeble as it is, tows the orbit over. It is the gentlest deflection and the most
demanding of one thing: TIME. This lesson computes exactly how much. The number is the whole lesson — the
pull is so faint that the slow hand only works with YEARS of warning, so it is really a measurement of how
much early detection is worth. And to hover without falling in, the tug must station-keep continuously: the
same discipline lesson 25's autopilot spends to hold a park, here spent to hold a standoff.*

```bash
dotnet run --project labs/37-the-slow-hand -c Release
```

## Why this lesson exists

Lessons 35 and 36 are the loud deflections — the drilled charge and the thrown mass, both instantaneous.
The gravity tractor is the quiet one, and it teaches the owner's favourite lesson (the one the whole
detection stack in lesson 8 exists to serve): **early detection pays.** A deflection that needs a decade of
lead is a reason to build telescopes, not charges. This lab puts a number on "how early," and ties the
technique to lesson 25's station-keeping so the two share one discipline.

## The standard-textbook take

**A gravity tractor is a hovering test mass** (the Lu & Love 2005 proposal; Curtis ch. 2 for the two-body
gravity, ch. 6 for the orbit change). A tug of mass *m_ship* held at standoff *d* from the rock's centre
pulls the rock with

  a = G · m_ship / d²

— note the rock's own mass does NOT appear: gravity acts on the ship's mass, and the induced acceleration
of the rock is set by the ship. Held continuously over lead time *T*, that steady acceleration opens a miss
of

  miss ≈ 1.5 · a · T²

(the continuous-tow analogue of lesson 36's impulsive 3·Δv·T — a linearly-growing Δv integrates to half the
leverage, and the along-track drift is quadratic in time). Solve for *T* and you have the arrival deadline.

## What the game adds that the textbook doesn't

**The hover priced as station-keeping** (the lesson 25 tie). Textbooks give the tow; the game must also pay
to *stay*. To hover at a fixed standoff the tug must thrust to cancel the rock's pull on IT —
`F = m_ship · G · M_rock / d²` — or it falls onto the rock. That is a continuous station-keeping burn, the
same law lesson 25's autopilot spends against a moon's tide, and its cost is not Δv but DURATION: a tiny
force sustained for years. The lab prices both halves.

## The numerical experiment

*(Reference tug: 100 t at 1.5 radii standoff. Target: clear Ringside by SafeMiss = 30 Mm.)*

### A — the feeble pull

```
    radius  standoff (m)    accel a (m/s²)
      50 m          75.0         1.19E-009
     140 m         210.0         1.51E-010
    1000 m        1500.0         2.97E-012
```

*(370 m row omitted here for width; it reads 555.0 m → 2.17E-011 m/s² in the probe output.)*

A nanometre-per-second-squared, falling as 1/d². This is the entire thrust of the technique — less than a
billionth of a gee. Everything downstream is this number fighting the clock.

### B — how early you must arrive

```
    radius    accel (m/s²)  lead required (yr)
      50 m       1.19E-009                4.11
     140 m       1.51E-010               11.52
     370 m       2.17E-011               30.44
    1000 m       2.97E-012               82.28
```

The headline. A 100 t tug clears a **50 m** rock with **~4 years** of towing; the reference **140 m**
city-killer needs **~11.5 years**; a **1 km** rock needs **82 years** — longer than a career. The wait grows
with the standoff (which grows with the rock's size), so the slow hand is a small-rock, long-warning tool.
Arrive late and it is simply not on the menu — the cannonball or the drilled charge is your only option.

### C — ship-mass sensitivity: √mass, not mass

```
  tug mass (t)    accel (m/s²)  lead required (yr)
            10       1.51E-011               36.43
            50       7.57E-011               16.29
           100       1.51E-010               11.52
           500       7.57E-010                5.15
          1000       1.51E-009                3.64
```

You can bring a heavier tug (or ballast), but the wait falls only as **√mass**: a 100× heavier tug (10 t →
1000 t) cuts the 140 m wait just 10× (36 → 3.6 yr), because miss ∝ a ∝ m_ship but the deadline ∝ 1/√a. The
slow hand cannot be muscled — only started early. This is the lesson stated as a scaling law.

### D — the hover bill (the lesson 25 tie)

```
type                    radius  rock mass (kg)  hover thrust (N)
C-type carbonaceous      140 m       1.61E+010              2.44
C-type carbonaceous     1000 m       5.86E+012             17.39
S-type stony             140 m       3.10E+010              4.70
S-type stony            1000 m       1.13E+013             33.55
M-type metallic          140 m       6.09E+010              9.22
M-type metallic         1000 m       2.22E+013             65.85
```

To hover off a 140 m S-type the tug thrusts a steady **4.7 N** — a hand's weight — but for the whole
11.5-year tow, without drift, or it falls in. That is a station-keeping problem, lesson 25's discipline
applied to a standoff instead of a park: the cost of the slow hand is measured in years of held attitude,
not in a fuel gauge. A dense metal rock pulls harder (M-type 1 km: 66 N) and is the worst on both the
warning axis and the hover axis.

## The gameplay hook this enables

**A gravity-tractor gig variant that rewards EARLY DETECTION** — arrive years ahead of impact and a gentle
hover saves the port with zero collision risk (no drilling complications, no thrown mass); arrive late and
the slow hand is useless. It composes directly with lesson 25: the hover IS station-keeping, so the same
autopilot loop that holds a park can hold a tow standoff. NOT built here; this certifies the physics and
puts the early-detection lesson in hard numbers the owner can price a gig against.

## Break it on purpose

1. **Close the standoff.** Set `StandoffFactor = 1.0` (hover right at the surface) and rerun section B: the
   wait shrinks (a ∝ 1/d²), but now the thrusters blast the regolith and the test-mass assumption breaks —
   the reason the standoff exists. Physics vs. plume, priced.
2. **Demand only a graze.** Swap `SafeMiss` for `DeflectionGig.GrazeMissMeters` (8 Mm) and rerun: the 140 m
   wait drops from 11.5 to ~6 years (T ∝ √miss). Half the ambition, ~⅗ the wait — the quadratic cuts both
   ways.
3. **Use lesson 36's impulsive leverage by mistake.** Change `ContinuousTowLeverage` from 1.5 to 3.0 (the
   impulsive factor) and rerun: every wait shrinks by √2 — but it's wrong, because a tractor's Δv builds up
   over the tow rather than landing all at once. The 1.5 is the continuous-ramp integral, not the impulse.

## The framing rule, kept

Standard physics presented as standard: the hovering-test-mass tow (Lu & Love), the 1/d² gravity (Curtis
ch. 2), and the quadratic-in-time along-track drift are textbook. The hover-as-station-keeping framing is
ours — but it's the same law lesson 25 already flies, computed honestly and priced in held time. Every
number above came from running the probe; change the code and rerun — never hand-edit a table.
