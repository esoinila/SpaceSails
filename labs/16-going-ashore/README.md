# Lesson 16 — Going ashore

*A moon is a bus stop nested inside a bus stop. The tide trying to strip your parking orbit
is the planet's, the sun works the whole arrangement from outside — and the ground, for the
first time in this series, is close enough to land on.*

```bash
dotnet run --project labs/16-going-ashore -c Release
```

(This one integrates ~36 satellite-years around Luna at two step sizes — give it a minute.)

## Why this lesson exists

Lesson 7 priced the planetary bus stops. But the places worth *going ashore* are mostly
moons — the He3 is on moons, the Enceladus haven IS a moon — and the game engine makes
nothing simpler for you there: a ship near Luna feels Luna, Earth, and the sun at once,
honestly summed. This lesson sizes the nested stops with the game's own `OrbitRule`,
stress-tests Luna parking orbits prograde *and* retrograde (the restricted three-body
problem plays favorites), prices the arrival on the game's own meter, and then does what
the series has never done: computes the landing.

## The standard-textbook take

Curtis ch. 8 for the sphere of influence, ch. 6 for the transfer ellipse (a de-orbit is a
Hohmann whose lower stop is the ground). The pro/retro stability asymmetry is classical
celestial mechanics — Hénon's families in the restricted three-body problem; Hamilton &
Burns (1991) put the practical limits near 0.5 Hill for prograde satellites and ~0.9 Hill
for retrograde ones. Section B finds exactly that — but only after catching the integrator
telling the opposite story first.

## What the game simplifies away

Rails moons on circular orbits, no landing mechanics (yet — Section D is the mission the
deck crew would fly), no moon rotation, no Titan atmosphere.

## The numerical experiment

### A — the nested ladder

```
moon         Hill (km)  in radii  % of parent Hill  v_circ @ .5 Hill  window shell (km)
luna            61,533      35.4             4.11%           399.3 m/s           58,058
europa          13,654       8.7             0.03%           685.0 m/s           10,532
ganymede        31,720      12.0             0.06%           789.7 m/s           26,452
callisto        50,144      20.8             0.09%           535.2 m/s           45,324
titan           52,404      20.4             0.08%           585.4 m/s           47,254
enceladus          949       3.8             0.00%           123.3 m/s              444
```

Two readings. Enceladus's whole Hill sphere is ~3.8 of its own radii — the insertion window
(inside Hill, above 2 body radii, `OrbitRule.WindowOpen`) is a shell a few hundred km thick
around a 252 km snowball: **the haven that barely exists**. And every moon's 5×Hill capture
range falls below the game's 3,000,000 km floor (`CaptureRangeFloorMeters`) — that constant
isn't cosmetic; it is what makes moon stops findable at map zoom. The floor exists because
of this table.

### B — parking at Luna: the tide plays favorites, and so does the time step

Nine radii × two senses × six years in the real field (sun + Earth + Luna + everything),
run **twice**: once at the game's cruise step ceiling (3600 s), once at a fine 150 s.
"GONE" = the ship put 1.5 Hill radii between itself and Luna and stayed out.

```
--- max time step 3600 s ---
 fraction  prograde                      retrograde
      0.2  held 6 yr (max 0.21)          held 6 yr (max 0.24)
      0.3  held 6 yr (max 0.32)          held 6 yr (max 0.40)
      0.4  held 6 yr (max 0.44)          held 6 yr (max 0.54)
      0.5  held 6 yr (max 0.57)          held 6 yr (max 0.69)
      0.6  held 6 yr (max 0.71)          GONE 0.88 yr (max 4.02)
      0.7  held 6 yr (max 0.77)          GONE 0.44 yr (max 1.53)
      0.8  held 6 yr (max 0.80)          GONE 1.18 yr (max 1.79)
      0.9  GONE 0.02 yr (max 1.71)       held 6 yr (max 1.26)
      1.0  GONE 0.02 yr (max 2.71)       GONE 0.12 yr (max 1.78)

--- max time step 150 s ---
 fraction  prograde                      retrograde
      0.2  held 6 yr (max 0.22)          held 6 yr (max 0.22)
      0.3  held 6 yr (max 0.34)          held 6 yr (max 0.33)
      0.4  held 6 yr (max 0.50)          held 6 yr (max 0.46)
      0.5  held 6 yr (max 0.81)          held 6 yr (max 0.59)
      0.6  GONE 0.04 yr (max 2.08)       held 6 yr (max 0.72)
      0.7  GONE 0.04 yr (max 3.57)       held 6 yr (max 0.90)
      0.8  held 6 yr (max 0.80)          GONE 5.54 yr (max 3.88)
      0.9  GONE 0.02 yr (max 2.09)       held 6 yr (max 1.15)
      1.0  GONE 0.02 yr (max 2.98)       GONE 0.14 yr (max 1.97)
```

Read the two tables against each other and be a little afraid. At the cruise ceiling the map
says prograde is the hardy sense (solid to 0.8 Hill) and retrograde fragile (dies from 0.6).
At a fine step the **classical** picture emerges — prograde unravels past ~0.5 Hill,
retrograde rides to ~0.9, the distant-retrograde island of Hénon's family f — Hamilton &
Burns's limits, recovered. The coarse table isn't blurred. It is nearly **inverted**. In the
rotating frame a retrograde satellite meets the tidal bulge at a higher beat frequency, so
the same step ceiling under-resolves retrograde forcing first: truncation error masquerading
as dynamics — lesson 3's thesis biting lesson 7's method. (Rows hugging the boundary, like
prograde 0.8, stay step-sensitive at *any* dt: that's lesson 9's chaotic coastline, not
sloppiness.) The autopilot's 0.5 Hill (`AutopilotInsertHillFraction`) parks on the last
reliably-solid prograde rung — the constant is right, with thinner margin than the game's
own warp-speed integration would have you believe.

### C — the arrival bill, read off the game's own meter

```
 v_inf at Earth  speed at Luna's r  rel speed vs Luna (band)  window can open?
         500               1524                 501 .. 2548     yes, aim well
        1500               2079                1056 .. 3103     yes, aim well
        3000               3328                2305 .. 4351     yes, aim well
        5000               5203                4180 .. 6226     yes, aim well
(Luna's own orbital speed: 1023 m/s; the window needs rel speed < 5000 m/s.)
```

A ship coasting in from interplanetary space falls down Earth's well before it meets Luna —
yet even a hot 5 km/s hyperbolic excess leaves the *low end* of the relative-speed band
under the window limit. Geometry decides: chase Luna along her orbit and a modest
interplanetary arrival captures at the Moon **directly**, no Earth-orbit layover; cross her
path instead and the same v_inf slams the window shut. (Real missions know this trick from
the other side — lunar flybys as free brakes.)

```
 rel speed (m/s)  insertion dv (m/s)  pulses
             500               100.7       1
            2000              1600.7       6
            4000              3600.7      12
```

The insertion pulses are 1% of *your current speed* each (`DeltaVPerPulseFraction`) — the
meter's unit is your own heliocentric speed, so the same physical burn costs different
pulses on different headings.

### D — going ashore (and getting off again)

From circular parking, one retro burn drops periapsis to the ground — a Hohmann transfer
whose lower stop is the surface. The lander must then kill the touchdown speed:

```
moon        park at (km)  de-orbit dv  fall time  touchdown (m/s)  depart park (m/s)
luna              18,460      301.7 m/s      759 min           2271.7              213.5
europa             4,682      242.3 m/s      161 min           1754.7              342.6
ganymede           9,516      348.2 m/s      249 min           2425.2              422.3
callisto          15,043      327.8 m/s      504 min           2266.2              286.2
titan             15,721      354.8 m/s      484 min           2447.8              313.0
enceladus            757       28.6 m/s      221 min            207.1               40.4
```

And the honesty pass — Luna's row flown through the real integrator, watching for the
surface (the engine clamps gravity off inside a body; collision is a later milestone, so
the probe detects the crossing itself):

```
flown: surface contact after 760 min at 2271.0 m/s relative to Luna
analytic said:        759 min at 2271.7 m/s
```

The two-body de-orbit survives contact with the real field: the fall happens deep inside
the Hill sphere, where Luna owns the dynamics and the tide is a spectator. Then read the
table as a prospector: Enceladus is a 28.6 m/s de-orbit and a 207 m/s touchdown — a hard
bicycle crash — while Ganymede arrives like a train at 2.4 km/s. The He3 is always on the
moons with the cheap ground; the worldbuilding knew what it was doing.

## Break it yourself

1. **Trust the cruise ceiling.** Take Section B's coarse table at face value and "park" a
   retrograde ship at 0.9 Hill in the live game at high warp (the game's own integration
   runs at the 3600 s ceiling up there). Now re-run the probe's fine ladder and decide:
   is your ship parked, or is it *numerically* parked? Write down which one the player
   experiences, and why nobody has filed the bug.
2. **Aim past the shell.** Section A gives Enceladus a 444 km insertion shell. Fly an
   approach with a 500 km aiming error and count how often `WindowOpen` is true along the
   pass. Then price the same sloppiness at Callisto. Small moons don't forgive.
3. **Land on the far stop.** Repeat Section D's flown check at Enceladus — the fall is 221
   minutes through a Hill sphere only 3.8 radii deep, with Saturn's tide anything but a
   spectator. Does the analytic fall time survive? (Guess first, then fly it.)
