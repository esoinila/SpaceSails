# Lesson 30 — The mass-driver timetable

*The owner's canon (docs/worldbuilding-notes.md §1): Luna's factories lob standardized
compute-core packages by **mass driver** into transfer orbits. The pod has **zero maneuver
budget** — the driver gives it everything at launch, and then it is a rock on a conic. The Sol
scenario's own description already says "Luna's mass drivers lobbing compute-core pods"; this
lesson is where that stops being flavour text. We model the launch honestly, find the family of
useful trajectories, build the repeating timetable off the analytic Kepler rail, and price what it
costs to catch a pod in flight — the milk run the pirate lane will point contracts at.*

```bash
dotnet run --project labs/30-the-mass-driver-timetable -c Release
dotnet run --project labs/30-the-mass-driver-timetable -c Release -- --viz
```

## Why this lesson exists

Every lesson so far flies a *ship* — something with an engine that can change its mind. A
mass-driver pod cannot. The driver on Luna's surface imparts one impulse, in one direction, at one
instant; after that the pod is a stone, and where it goes is decided entirely by the speed and
aim it left with. That makes it the cleanest possible object in the game — its whole future is a
closed-form Kepler conic, a **rail** you can name a position on at any time without stepping an
integrator (`TransferMath.PropagateKepler`, added for exactly this). It is also the pirate's milk
run: high-volume, low-value, trivially predictable cargo. This lesson measures the driver's launch
family and the honest cost of catching what it flings.

## The standard-textbook take

**Two-body ballistics** (Curtis chs. 2–3). A body released at position **r** with velocity **v**
about an attractor μ is on a conic fixed by its energy and angular momentum:
`ε = v²/2 − μ/r`, `a = −μ/2ε`, `e = √(1 + 2εh²/μ²)`, perihelion `a(1−e)`, aphelion `a(1+e)`.
To *leave* the launch body at all the driver must beat surface escape, `√(2μ/R)`. To then leave
cislunar space the pod must beat Earth-escape at Luna's distance, `√(2μ_E/r)`. Everything past
that is a heliocentric conic, and its reach is a one-line function of the launch speed and aim.
Propagation to any later time is the universal-variable Lagrange-coefficient form (Curtis Alg.
3.4) — the same Stumpff series the Lambert solver already uses.

## What the game adds that the textbook doesn't

The **timetable** is the product. A driver firing on a cadence is a bus schedule: half the pods
are always already in flight, half are still to fire, and each one's neighbourhood passes are
knowable in advance off the rail. And the **trade** is tactical — the pirate's question is not
"how do I get to Venus" but "where and when is a pod cheapest to take?" The answer is honest and a
little surprising: a pod is cheapest at the *launch* end, and the deeper into its dive you chase
it, the more of the transfer you buy yourself.

## The numerical experiment

### A — the launch budget

```
Luna surface escape speed sqrt(2mu/R): 2376.2 m/s (2.376 km/s) — the driver floor.
Luna heliocentric speed at t=0: 29.57 km/s (Earth rides at 29.79 km/s).
Earth-escape speed at Luna's distance sqrt(2mu_E/r): 1.440 km/s — clear this,
relative to Earth, and the pod leaves cislunar space for a heliocentric conic.
```

The driver must give at least **2.376 km/s** to lift a pod off Luna at all. Luna itself is already
falling around the sun at ~29.6 km/s, so the aim of that kick — added to or subtracted from 29.6
km/s — is what decides the pod's whole heliocentric orbit.

### B — the launch family: sweep speed × direction

Azimuth is measured off Luna's heliocentric prograde: **π is dead retrograde** (bleed heliocentric
speed → dive toward the sun), **0 is prograde** (add speed → climb outward).

```
v_launch km/s azimuth      peri AU    apo AU       e  reach
--------------------------------------------------------------------------
2.60          retro (pi)     0.692     1.002   0.183  reaches Venus
3.20          retro (pi)     0.642     1.001   0.219  reaches Venus
4.00          retro (pi)     0.581     1.001   0.265  reaches Venus
5.00          retro (pi)     0.514     1.000   0.321  reaches Venus
6.00          retro (pi)     0.454     1.000   0.375  reaches Venus
7.00          retro (pi)     0.402     1.000   0.427  reaches Venus
7.60          retro (pi)     0.373     1.000   0.457  reaches Mercury

2.60          prograde (0)     0.996     1.400   0.169  inner cislunar band
3.20          prograde (0)     0.997     1.533   0.212  reaches Mars
4.00          prograde (0)     0.997     1.739   0.271  reaches Mars
7.60          prograde (0)     0.998     3.509   0.557  reaches Mars
```

The useful family reads straight off the perihelion column. A **retrograde** lob of ~**2.6 km/s**
already drops perihelion into Venus's lane (0.69 AU) — the everyday inner-system milk run. Push the
driver to ~**7.6 km/s** and perihelion falls to **0.373 AU**, threading the **Mercury compute
yards** (Mercury rides at 0.387 AU): the showpiece long shot, expensive but honest. **Prograde**
lobs climb the other way toward Mars, and just above the escape floor the pod barely leaves and
loiters in the inner cislunar band — the low-energy "into Earth's neighbourhood" case. (The 3π/4
family, in between, is Venus for everything above ~3 km/s — the full sweep is in the probe.)

### C — the timetable, on the Kepler rail

Pick the everyday run: **3.2 km/s retrograde, fired every 12 h**, each pod live for 200 days. The
pod's Venus-lane pass is read off `MassDriverSchedule.PodRailState` — a pure function of time, no
integration:

```
pod             launch (d)  Venus pass (d)   pass r AU   pass v km/s
--------------------------------------------------------------------
Milk Run             -2.00           174.0       0.724         36.71
Windfall             -1.50            99.5       0.722         36.87
Ripe Plum            -1.00           102.0       0.724         36.87
Fat Goose            -0.50           177.5       0.724         36.96
Easy Keeping          0.00           108.0       0.724         37.00
Tin Kettle            0.50           111.5       0.724         37.11
Slow Coach            1.00           115.0       0.724         37.17
Ferryman's Due        1.50           179.5       0.723         37.32
```

Half the board is already in flight at t=0 (negative launch times), half still to fire — the sky is
never empty and the driver never idle. Each pod crosses Venus's radius (0.72 AU) at ~**37 km/s**, a
first pass ~100 days out (some at ~175 d catch the second crossing before the timetable window ends).

### D — the intercept: loiter-and-match vs chase-it-down

A pod off the driver is nearly co-orbital with Earth for the first hours, then it commits to the
dive and speeds away. Both catches are priced with `OrbitRule.PulsesFor`, the game's own kernel.

**(1) loiter-and-match** — the pirate is already parked in a circular heliocentric orbit at the
pod's radius (loitering where the pod passes); the catch costs only the velocity match,
`|V_pod − v_circular|`, no transfer to fly:

```
flight age    pod r AU   match m/s  pulses
------------------------------------------
0.02             0.999      3533.1      12
1.00             1.000      3514.2      12
4.00             1.001      3472.7      12
8.00             1.001      3457.1      12
16.00            0.999      3562.0      12
32.00            0.982      4224.6      15
```

**(2) chase it down** — not pre-positioned; parked at Earth, the player flies a Lambert arc to the
pod's rail position at t0+TOF and matches on arrival (Lab 23/24's kernel, `TransferMath.Lambert`
plus two priced burns):

```
TOF (d)    pod dist Gm  depart m/s  match m/s  total m/s  pulses
----------------------------------------------------------------
4                  8.7      4561.5     1117.9     5679.5      21
20                44.9      3734.2      226.1     3960.4      14
36                80.6      3638.6      127.3     3765.9      14
60               132.6      3586.0       76.0     3662.0      14
```

The cheap window is **loiter-and-match, ~3.5 km/s = 12 pulses** (cheapest at 3457 m/s for a pod
~8 days old): you take the pod for the driver's own speed, without flying anywhere. Chasing it down
once it has committed to the dive is a real transfer — the gentlest arc is still **3662 m/s = 14
pulses**, dearer.

### E — the trade: catch in flight vs pick up at either end

```
option                                   total m/s   pulses
-----------------------------------------------------------
catch in flight (loiter-and-match)            3457       12
catch in flight (chase from Earth)            3662       14
pick up at Luna (match the driver)            3200       11
pick up at Venus (Hohmann chase)              5202       17
```

The driver did the expensive part — flinging the cargo onto a transfer — so the pod is cheapest to
take at the launch end (**3200 m/s / 11 pulses**, matching the driver), or by loitering in the
near-Earth pass for almost as little (**3457 m/s / 12 pulses**) with no trip to Luna at all. Let it
commit to the dive and you must fly a real arc to it (**14 pulses**); chase it all the way to Venus
and you pay the whole transfer yourself (**5202 m/s / 17 pulses**). The milk run rewards the pirate
who reads the timetable and is already parked where the next pod passes — exactly the "caught before
it built transfer speed" fiction the game's tutorial starter-pod already leans on.

With `--viz`: a heliocentric inner-system scene — the launch fan off Luna at 3.2 km/s (retrograde,
3π/4, prograde) with the retrograde Luna→Venus rail drawn bright, over the orbits of Mercury, Venus,
Earth and Mars.

## Break it on purpose

1. **Fire below escape.** Set the driver under 2.376 km/s and read the conic: the pod never leaves
   Luna (its "heliocentric orbit" is a fiction — it's still bound to the moon). The floor is real.
2. **Aim straight prograde and push.** Watch aphelion climb past Mars, then toward the belt, while
   perihelion stays pinned near 1 AU — a prograde driver is an *outbound* driver, never an inner one.
3. **Chase a 30-day-old pod.** Extend the Section D chase sweep past 60 days: the arc keeps getting
   cheaper as it lengthens, asymptoting toward the loiter-match floor — because the gentlest possible
   intercept *is* just co-orbiting with the pod, which is what loiter-and-match already does for free.

## The framing rule, kept

Standard physics presented as standard: Curtis chs. 2–3 (two-body conics, universal-variable
propagation), the launch budget from first principles. The world content — Luna's compute-core
drivers, the Mercury yards, the pirate's milk run — is the owner's canon (docs/worldbuilding-notes.md
§1, §3), computed honestly on the game's own rails and priced with the game's own pulse kernel.
