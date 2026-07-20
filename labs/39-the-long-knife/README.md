# Lesson 39 — The long knife (laser / standoff ablation)

*Every other deflection touches the rock — the drill lands on it (lesson 35), the cannonball hits it (lesson
36), the driver sits on it (lesson 38), even the tractor hovers close (lesson 37). The long knife stands OFF and
boils the rock with a beam. A big laser holds a bright spot on the surface; the vaporised rock jets away and its
recoil is the thrust. No landing, no drilling, no thrown slug — just light and patience. It is the gentlest
active deflection there is, and the one that waits on a serious power plant: the upgrade-gated, late-game
variant.*

```bash
dotnet run --project labs/39-the-long-knife -c Release
```

## Why this lesson exists

The other five techniques all ask *can we get to the rock and put something on it?* The long knife asks a
different question — *how big a reactor can we field?* — because it deflects from a standoff and never touches
anything. That makes it the natural late-game reward: an instrument you earn by upgrading your power plant, not
a rig you fly onto a mountain. This lab computes the two honest limits that give the technique its shape — how
far it can reach, and how fast it can push — and shows why the second one, not the first, is the gate.

## The standard-textbook take

**Laser ablation as continuous low thrust** (the momentum-coupling formulation used across the laser-propulsion
and planetary-defence literature — e.g. Phipps' coupling-coefficient work). Two pieces of standard physics:

- **The spot (diffraction).** A beam through an aperture *D* cannot focus tighter than diffraction allows; at
  range *d* the spot spreads to radius ≈ d·λ/D, so its area grows as *d²* and — at fixed power — its intensity
  falls as *1/d²* (inverse-square). To keep the spot above the flux threshold that actually vaporises rock, you
  need power ∝ *d²*.
- **The thrust (momentum coupling).** The ablation jet couples momentum to the target at a coefficient *C_m*
  (newtons of thrust per watt delivered, ~50 μN/W for rock/metal). So thrust F = C_m·P, acceleration a = F/M,
  and held continuously over the warning time the miss opens as **1.5·a·T²** — the very same continuous-tow
  leverage lesson 37's gravity tractor uses (reused here directly).

## What the game adds that the textbook doesn't

**The two limits, priced against each other.** The textbook gives the spot and the coupling separately; the game
needs to know which one bites first. The answer, computed below, is the useful surprise: diffraction is *cheap*
— a diffraction-limited spot at hundreds of km is sub-millimetre, so the power to reach threshold is trivial —
and the real cost is total power for *thrust*. Standoff is almost free; deflection rate is what you pay for. That
is what makes the long knife an upgrade, not a landing.

## The numerical experiment

*(Aperture D = 10 m, λ = 1.06 μm near-IR, ablation flux threshold = 10 MW/m², momentum coupling C_m = 50 μN/W.
Reference platform power = 1 MW. Target: clear Ringside by SafeMiss = 30 Mm.)*

### A — the standoff budget: spot ∝ d, intensity ∝ 1/d², min-power ∝ d²

```
    standoff spot radius (m) min power to ablate
 1.00E+003 m       1.06E-004              0.35 W
 1.00E+004 m       1.06E-003             35.30 W
 1.00E+005 m       1.06E-002             3.53 kW
 1.00E+006 m       1.06E-001           352.99 kW
```

Diffraction keeps the spot astonishingly small — a **tenth of a millimetre at 1,000 km** — so the power merely
to reach the ablation flux is tiny (a third of a kilowatt even at 1,000 km). Reading it the other way: a **1 MW
platform can boil rock from up to ~1,683 km**, and a **100 MW platform from ~16,831 km**. Standoff is not the
problem. You can sit comfortably far off the rock and still light it up.

### B — thrust and acceleration per rock: F = C_m·P, a = F/M

*(At the 1 MW reference, thrust F = 50 N — the same for every rock, because it's the jet, not the target, that
sets it. The acceleration is what differs.)*

```
type                    radius  rock mass (kg)    accel a (m/s²)
C-type carbonaceous      140 m       1.61E+010         3.11E-009
C-type carbonaceous     1000 m       5.86E+012         8.53E-012
S-type stony             140 m       3.10E+010         1.61E-009
S-type stony            1000 m       1.13E+013         4.42E-012
M-type metallic          140 m       6.09E+010         8.21E-010
M-type metallic         1000 m       2.22E+013         2.25E-012
```

A **nanometre-per-second-squared** — the same feeble scale as the gravity tractor. A megawatt of laser makes
50 N of push, and 50 N on a 30-billion-kilogram rock is almost nothing per second. Everything now rides on
warning time and on how much more power you can bring.

### C — the headline: how long / how much power to clear a 140 m rock

```
  platform power  thrust (N)    accel (m/s²)   clear time (yr)
            1 MW          50       1.61E-009              3.53
           10 MW         500       1.61E-008              1.12
          100 MW        5000       1.61E-007              0.35
          1.0 GW       50000       1.61E-006              0.11
```

The whole gameplay shape in one table. A **starter 1 MW** rig clears the reference **140 m** rock in **~3.5
years** of continuous cutting; the **100 MW upgrade** does it in **~0.35 years (~130 days)**; a fantastical GW
plant in ~40 days. Clear time falls as 1/√P (miss = 1.5·a·T², a ∝ P), so **the long knife is POWER-gated** —
patience at low power, months at high power. That is exactly the late-game upgrade curve.

### D — cumulative Δv over a fixed 1-year burn (140 m S-type)

```
  platform power  Δv in 1 yr (m/s) miss opened (m)   clears?
            1 MW            0.0508       2.41E+006        no
           10 MW            0.5084       2.41E+007        no
          100 MW            5.0844       2.41E+008       yes
          1.0 GW           50.8437       2.41E+009       yes
```

Give the knife exactly one year of continuous cutting and read what it opens. At **1 MW** it manages 2.4 Mm — a
graze at best, not a clear; at **10 MW** it just misses SafeMiss; **100 MW clears with margin** and a GW plant
clears by 80×. Same lesson as the clear-time table, stated as a Δv budget: below ~100 MW the one-year window
isn't enough for a 140 m rock, above it the knife is comfortable.

## The gameplay hook this enables

**An upgrade-gated, late-game "standoff laser" deflection variant.** Its signature versus every other technique:
it never touches the rock — no landing, no drilling, no rendezvous with the surface — so the whole difficulty
collapses onto one axis, the **power plant you can field**. A captain with a starter reactor watches it grind
for years; a captain who has invested in a 100 MW plant clears a city-killer in a season. It composes with the
tractor's continuous-tow leverage (lesson 37) and, like the tractor and the driver, still rewards early
detection. NOT built here; this certifies the physics and prints the numbers a late-game gig variant would cite.

## The honest reconciliation

Is the long knife real? The physics is sound and the components are plausible-future, but two honesty flags ride
along. First, **diffraction is kind and thrust is stingy**: the model says standoff is essentially free while
deflection rate is bought entirely in megawatts, which is why this is an upgrade rather than an early tool.
Second, the constants are *representative* — the ablation flux threshold and the momentum-coupling coefficient
depend on the rock's real composition and on running the laser continuously (thermal load on the optics, plume
re-deposition, the rock's spin smearing the push are all glossed). The lab's claim is deliberately narrow: at
these representative constants, the long knife is a **power-limited, warning-hungry** technique, not a fast one —
an honest ceiling the owner can price a late-game gig against, and cheap to re-run with better constants.

## Break it on purpose

1. **Shrink the mirror.** Drop `ApertureMeters` to 3 and rerun section A: the spot grows (r ∝ 1/D), min-power
   rises ~11×, and the max reach falls to a few hundred km. Aperture buys standoff — the one place bigger
   optics matter.
2. **Doubt the coupling.** Halve `MomentumCouplingNewtonsPerWatt` and rerun section C: every clear time grows by
   √2, because thrust ∝ C_m and clear time ∝ 1/√a. The single most load-bearing constant, stress-tested.
3. **Cut a 1 km rock instead.** Point section C at radius 1000 m and rerun: the clear times balloon (~24× the
   mass), and even 100 MW needs years — the long knife, like the tractor, is a small-rock-or-long-warning tool.

## The framing rule, kept

Standard physics presented as standard: diffraction-limited beam spread (any optics text), the momentum-coupling
formulation of laser ablation (the laser-propulsion literature), and the continuous-tow 1.5·a·T² leverage lesson
37 already pins. The "which limit bites first" framing and the upgrade-gate reading are ours — but the
arithmetic is textbook, computed honestly and labelled where the constants are representative. Every number above
came from running the probe; change the code and rerun — never hand-edit a table.
