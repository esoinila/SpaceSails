# Lesson 38 — The rock that mines itself (mass driver on the rock)

*The cannonball (lesson 36) throws mass AT the rock; the gravity tractor (lesson 37) tows it with nothing but
its own weight. This one lands a driver rig on the rock and throws the rock's OWN mass off the far side — the
asteroid is both the ship and the fuel. It is the Luna mass-driver canon (lesson 30) turned on the threat: the
away-mission rig hauls up a bucket-thrower and a reactor, and every tonne of regolith flung overboard kicks the
rest of the rock a little farther from Ringside. The whole lesson is the rocket equation with the rock as its
own tankage, and the honest engineering bill that falls out of it.*

```bash
dotnet run --project labs/38-the-rock-that-mines-itself -c Release
```

## Why this lesson exists

Lessons 35–37 are the three classic deflections. The mass driver is the *self-sufficient* one — the technique
that needs no ammunition, because the ammunition is the rock. That is its whole appeal and its whole catch. The
appeal: no cargo pod to sacrifice, no hull to fly in, no external slug at all, and the rig is reusable across
visits. The catch: you are paying in TIME and POWER instead of mass, and the numbers say that bill is a
multi-visit engineering job, not an emergency punch. This lab puts the bill on the table.

## The standard-textbook take

**The rocket equation, with the rock as tankage** (Tsiolkovsky; Curtis ch. 6 for the derivation). A mass driver
flings reaction mass off the rock at exhaust (muzzle) velocity *v_ex*. Throw the rock down from *M₀* to
*M_final* and the remainder gains

  Δv = v_ex · ln(M₀ / M_final)

Equivalently, to give the remaining rock a required Δv you must throw off the **fraction**

  f = 1 − e^(−Δv / v_ex)

of its mass. The deflection Δv itself is set the along-track way the cannonball's is (lesson 36): a shove applied
roughly along-track opens a miss of 3·Δv·t over the warning time *t*, so the warning multiplies the nudge.

## What the game adds that the textbook doesn't

**The engineering clock.** The textbook stops at Δv. The game must also ask *how long does the rig run* and
*how big a reactor does it need*. Run time is the mass flung over the rig's throughput; the reactor power is the
kinetic-energy rate poured into the jet, `P = ṁ·½·v_ex²`. Those two numbers turn a clean Δv into a multi-visit
gig with a power plant — and set the honest limit on which rocks it can touch in time.

## The numerical experiment

*(Muzzle v_ex = 2500 m/s — a modest rock rig; Luna's compute-pod driver, lesson 30, throws at ~3200 m/s.
Reference throughput = 20 kg/s. Target: clear Ringside by SafeMiss = 30 Mm. Deflection Δv is along-track,
miss = 3·Δv·t.)*

### A — the rocket equation: what each fling fraction buys

```
  fraction flung   Δv gained (m/s)
         0.001 %            0.0250
         0.010 %            0.2500
         0.100 %            2.5013
         1.000 %           25.1258
        10.000 %          263.4013
```

For the small fractions a deflection actually needs, Δv is very nearly linear in the fraction flung
(f ≈ Δv/v_ex). Throwing a hundredth of a percent of the rock buys a quarter of a metre per second — and, as the
next table shows, a quarter of a metre per second with a year of warning is more than enough.

### B — the self-mine: fling to clear Ringside with 1 year of warning

*(Required Δv at 1-year lead = 0.3169 m/s. Mass flung = M₀·(1 − e^(−Δv/v_ex)) — the same 0.0127 % fraction of
every rock, because the required Δv is fixed by the warning, not the rock.)*

```
type                    radius rock mass M0 (kg)  fraction flung    mass flung (t)
C-type carbonaceous       50 m         7.33E+008        0.0127 %         9.29E+001
C-type carbonaceous      140 m         1.61E+010        0.0127 %         2.04E+003
C-type carbonaceous      370 m         2.97E+011        0.0127 %         3.76E+004
C-type carbonaceous     1000 m         5.86E+012        0.0127 %         7.43E+005
S-type stony              50 m         1.41E+009        0.0127 %         1.79E+002
S-type stony             140 m         3.10E+010        0.0127 %         3.93E+003
S-type stony             370 m         5.73E+011        0.0127 %         7.26E+004
S-type stony            1000 m         1.13E+013        0.0127 %         1.43E+006
M-type metallic           50 m         2.78E+009        0.0127 %         3.52E+002
M-type metallic          140 m         6.09E+010        0.0127 %         7.72E+003
M-type metallic          370 m         1.12E+012        0.0127 %         1.43E+005
M-type metallic         1000 m         2.22E+013        0.0127 %         2.81E+006
```

The headline, and the honest tension. The **fraction is a rounding error** — 0.0127 %, one part in ~7,900, of
*any* rock — but the **absolute mass is enormous** because M₀ is. The reference **140 m S-type** costs
**~3,900 tonnes** flung; a **1 km** rock costs **~1.4 million tonnes**. You are moving a mountain a spoonful at
a time, and the mountain is what makes it slow.

### C — the run clock: how long the rig throws (at 20 kg/s)

```
type                    radius      lead      run time finishes in time?
S-type stony             140 m   1.00 yr        2.28 d               yes
S-type stony             140 m   5.00 yr        0.46 d               yes
S-type stony            1000 m   1.00 yr       2.27 yr                NO
S-type stony            1000 m   5.00 yr      165.92 d               yes
M-type metallic          140 m   1.00 yr        4.47 d               yes
M-type metallic          140 m   5.00 yr        0.89 d               yes
M-type metallic         1000 m   1.00 yr       4.46 yr                NO
M-type metallic         1000 m   5.00 yr      325.69 d               yes
```

The honest negative, in one column. A **140 m** rock is a few days of throwing — a doable, if patient, visit. A
**1 km** rock with only a year of warning needs the rig to run for **more than two years** — it cannot finish
before impact at all. Give it **five years** of warning instead and the same 1 km rock drops to ~166 days,
because the longer lead cuts the required Δv (and so the mass) fivefold. The mass driver is a **long-warning,
multi-visit** technique: it rewards early detection exactly as the gravity tractor does.

### D — the reactor bill: power to fling regolith at the muzzle speed

```
 throughput (kg/s) driver power (MW)
                 5             15.62
                20             62.50
               100            312.50
               500           1562.50
```

`P = ṁ·½·v_ex²`. Even the modest 20 kg/s reference rig draws **62.5 MW** — a serious reactor the away-mission
must haul and set up on the rock. Faster throughput shortens the run but multiplies the power linearly, so the
driver is a heavy-infrastructure gig by construction: the cost the crew pays is a power plant and repeat visits,
not a magazine of slugs.

## The gameplay hook this enables

**A "land a driver and eat the rock" multi-visit engineering gig.** Its signature versus the other techniques:
*no ammunition to haul* — the rock is the fuel — and a *reusable* rig you return to across the warning window.
It ties straight into the Luna mass-driver canon (lesson 30, `MassDriverSchedule`) and the away-mission rig
hauling. Its honest limits, priced here, are the gameplay: throughput and warning gate it, so big rocks on short
notice are simply out of reach and the gig is a patient, come-back-again contract — the deflection for a captain
who found the rock early. NOT built here; this certifies the physics and prints the numbers a gig variant lane
would cite.

## Break it on purpose

1. **Starve the reactor.** Drop `ReferenceThroughputKgPerSecond` to 5 and rerun section C: every run time
   quadruples — the 140 m S-type goes from ~2.3 days to over a week, and the 1 km/5 yr row slides past its
   lead. Throughput is the whole schedule.
2. **Throw harder.** Raise `ExhaustVelocityMetersPerSecond` to Luna's 3200 m/s and rerun section B: the mass
   flung falls by the ratio (≈ 2500/3200), because f ≈ Δv/v_ex. A faster muzzle is a cheaper mine — but section
   D's power grows as v_ex², so speed is not free.
3. **Ask for a graze, not a clear.** Swap `SafeMiss` for `DeflectionGig.GrazeMissMeters` (8 Mm) and rerun: the
   required Δv, the mass, and the run time all fall by the miss ratio (~0.27×) — the quadratic-free along-track
   leverage is linear in the miss, so half-ambition is proportionally cheaper.

## The framing rule, kept

Standard physics presented as standard: the Tsiolkovsky rocket equation (Curtis ch. 6) with the rock as its own
propellant tank, and the along-track 3·Δv·t leverage lesson 36 already pins. The mass-driver-on-the-rock framing
and the Luna-canon tie are ours — but the arithmetic is textbook, computed honestly and priced in run days and
reactor megawatts. Every number above came from running the probe; change the code and rerun — never hand-edit
a table.
