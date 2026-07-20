# Lesson 35 — The push that isn't a push (ablation)

*The owner wanted an Armageddon-movie gig, and #399/#401 shipped it: an inbound rock, a crew that lands
and DRILLS a charge, and a burn that lifts the rock's rail off the Ringside Exchange's lane. But the burn
isn't a rocket bolted to the rock — it ABLATES a jet of vaporised regolith, and the recoil of that jet is
the shove. This lesson certifies the shipped physics: the cos law that makes the spinning rock only take
the push when its bore faces the right way, the asteroid TYPE that sets how hard it drills and how eagerly
it ablates, and the honest geometry where the raised periapsis IS the miss the map draws. Then the honest
science the movie skips: real impact ejecta can AMPLIFY the deflection, not absorb it (DART proved it) —
and the game's own Electric-Universe licence for the impact flash.*

```bash
dotnet run --project labs/35-the-push-that-isnt-a-push -c Release
```

## Why this lesson exists

The deflection gig (`DeflectionGig`) is a lot of coupled physics wearing a heist's clothes: a Kepler
collision rail, a rotation window, per-composition drill and ablation constants, success bands, a heroic
payout. When a constant moves, *does the rock still just barely clear on a flawless M-type run? Does a
periapsis raise still equal the miss the nav map draws?* This lab is the answer key — it prints the
numbers the gig's constants encode, pinned by `DeflectionGigTests`, so a change that breaks the physics
shows up here first. It certifies; it does not re-implement.

## The standard-textbook take

**Ablation deflection is a reaction drive whose propellant is the target** (Curtis ch. 6 for the orbit
change; the ablation impulse itself is a laser/solar-ablation staple of the deflection literature). Bury
or focus enough energy at a point on the rock and surface material flashes to gas; the gas leaves at
several km/s and, by momentum conservation, the rock recoils the other way. Two facts the textbook states
plainly and the game must honour:

1. **The push is a cosine law in the spin.** The jet leaves along the bore normal. If the rock spins, the
   bore sweeps through every heading, and only the component of the jet aligned with the *wanted* push
   direction counts — a `0.5·(1+cos θ)` raised cosine as the bore rotates past the aim. Fire off-window
   and you spend the charge heating space.
2. **A periapsis raise is a miss.** Lifting the low point of the colliding orbit by ΔR, with the argument
   of periapsis unchanged, moves the closest approach to the station's lane by ΔR (the two share a parent,
   so the parent's motion cancels — lesson 6's honest closest-approach, lesson 25's periapsis bookkeeping).

## What the game adds that the textbook doesn't

**Zubrin's taxonomy, honestly costed** (owner 2026-07-20: *"the type would definitely be a factor"*).
Composition is an input axis, not flavour: a carbonaceous C-type is soft to drill and ablates eagerly
(volatiles flash and shove — efficiency **1.2**); a metallic M-type is brutal to bore and resists ablation
(efficiency **0.7**, *"bring a bigger charge"*); a stony S-type is the firm textbook middle (**1.0**). One
constants table (`DeflectionGig.RockProfile`), named on the mission card, tuned so even the worst rock JUST
clears on a flawless run — the drama is real because the margin is real.

## The numerical experiment

### A — the cos law of rotation

```
   spin fraction   alignment  in window?
            0.00       1.000         yes
            0.10       0.905         yes
            0.20       0.655          no
            0.25       0.500          no
            0.40       0.095          no
            0.50       0.000          no
```

The bore is aligned at spin fraction 0 and dead opposite at half a turn. The firing window
(`FiringWindowAlignment = 0.85`) is a narrow crown near alignment — the client holds the charge until the
next aligned moment, so a clean run fires at ~1.0. Force it a fifth of a turn off and the shove is already
below window; fire at the quarter-turn and you deliver half.

### B — asteroid TYPE: drill hardness and ablation efficiency

```
type                     drill (s)  ablation eff   full-charge raise (m)            band
C-type carbonaceous            9.8          1.20                54000000  FullDeflection
S-type stony                  14.0          1.00                45000000  FullDeflection
M-type metallic               22.4          0.70                31500000  FullDeflection
```

The three types span a 2.3× drill-time range (9.8 s → 22.4 s) and deliver 54 → 31.5 Mm of periapsis raise
on a full, aligned charge. All three clear the `SafeMiss = 30 Mm` line on a *flawless* run — but the M-type
clears by only 1.5 Mm, so any shortfall drops it to a graze (pinned in `WorstRock_JustClears...`). The
C-type clears by 24 Mm — a soft rock forgives a sloppy run.

### C — the deflection is honest: raise = miss

```
    raise ΔR (m)   measured miss (m)       error            band
 0 (undeflected)                   0           —          Impact
         8000000             8000000      -0.00%          Impact
        30000000            30000000      -0.00%     GrazingMiss
        45000000            45000000      -0.00%  FullDeflection
```

Flown on the real Ringside Kepler rail (`MissDistanceMeters`), an undeflected rock hits (miss ≈ 0), and a
periapsis raise of ΔR opens the closest approach to *exactly* ΔR to five figures. The lifted rail the nav
map draws is the true miss — the money shot is not a fudge.

### D — the success bands and the heroic payout

```
outcome              payout (cr)
full deflection            12002
grazing miss                6001
impact / abort              1500  (floor 1500; per crew lost −2000)
```

Full clears with margin and pays the heroic premium; a graze saves the port but pays half (it's bleeding);
an impact or abort pays only the floor — and Ringside SURVIVES either way (owner canon: heavy damage, never
destroyed). Every crew member lost to the fall docks 2,000, never below the floor.

### E — the DART reality check (honest science the movie skips)

```
type                  ablation eff  reading
C-type carbonaceous   1.20          eager — volatiles flash, plume amplifies
S-type stony          1.00          the firm middle (DART's measured class)
M-type metallic       0.70          stubborn — dense metal resists ablation
```

DART hit Dimorphos (a real S-type rubble pile) in 2022 and deflected it **~3.6× bare momentum** — the loose
surface threw an ejecta plume that ADDED thrust. So the honest lesson is the *opposite* of "a loose rock
absorbs the push": looseness can AMPLIFY it. The gig already leans this way — the eager C-type's efficiency
is **greater than 1** — so the type table is reconciled with the DART result rather than contradicting it.
(Owner 2026-07-20 dropped the rubble-pile structure axis from the game; the DART note stays as physics
context, and lesson 36 carries the ejecta enhancement β explicitly.)

### F — the Electric-Universe impact-flash model (flagged non-mainstream)

```
type                    conductivity  flash × (vs kinetic)
C-type carbonaceous             0.15                  1.23
S-type stony                    0.35                  1.52
M-type metallic                 1.00                  2.50
```

⚡ **Game canon, not textbook.** SpaceSails runs Electric-Universe rules (hull-charge vent #369), so bodies
at different electric potential ARC on contact and the flash exceeds pure vaporisation. Owner's own
arc-melter reference — **500 A across a 22 kV start spark ⇒ ~11 MW** — anchors an `ImpactArcFlash` model
where the flash scales with the rock's CONDUCTIVITY: the metallic M-type arcs hardest and flashes brightest
(2.5× a pure-kinetic flash), C/S add little. Mainstream attributes the flash to vaporisation alone; this is
the house's licence to be electric, computed and LABELLED as such. In canon the same conductivity that
brightens the flash would lend the biggest arc-assisted shove — logged here for a future deflection kick,
not yet wired into the gig.

## The gameplay hook this certifies

**The shipped #399/#401 ablation deflection gig** — drill, cos-law fire, per-type cost, honest miss, heroic
pay. This lab is its answer key. The arc-flash multiplier is the one piece of *new* Core (`ImpactArcFlash`):
a logged, labelled EU hook for the owner's charged-asteroid canon (a conductive rock that flashes bigger
and, one day, deflects harder).

## Break it on purpose

1. **Widen the firing window.** Set `FiringWindowAlignment = 0.5` and re-read section A: the client would
   fire at the quarter-turn, delivering half the charge — watch the M-type drop from *just clears* to a
   graze. The narrow window is what makes a clean run matter.
2. **Make metal ablate like stone.** Set `CompositionAblation(MType) = 1.0` and rerun section B: the M-type
   now clears by the same 15 Mm margin as the S-type and the "bring a bigger charge" drama evaporates. The
   0.7 is the tension.
3. **Rotate the argument of periapsis on the raise.** In `RaisePeriapsis`, nudge `ArgPeriapsis` as well as
   the semi-major axis and rerun section C: the miss no longer equals the raise, because the closest
   approach now happens off the impact instant. Raise-equals-miss depends on *only* the low point moving.

## The framing rule, kept

Standard physics presented as standard: the ablation reaction drive and the raised-cosine spin law are
deflection-literature staples; the raise-equals-miss geometry is lesson 6/25's honest closest-approach.
The Zubrin type costs are ours but reconciled with the real DART measurement, not against it. Section F is
explicitly the game's Electric-Universe cosmology, flagged the way lessons 11 and 12 flag theirs — in this
house we compute both and label which is which. Every number above came from running the probe.
