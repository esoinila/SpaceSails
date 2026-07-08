# Lesson 18 — The free return

*Leave Earth, coast, swing past Mars without capturing, come home — never having burned
since departure. Apollo rode this figure to the Moon (it saved 13); Rich Purnell pitched it
in "The Martian"; Aldrin made it permanent and called it a cycler. No formula produces it.
A search does.*

```bash
dotnet run --project labs/18-the-free-return -c Release
```

(The search flies ~1,800 multi-year trajectories — give it half a minute.)

## Why this lesson exists

Every arc in lessons 14–17 had two endpoints and one leg. A **free return** is a different
species: one departure burn, *two* planetary appointments, zero burns in between. The
figure exists only when two coincidences line up — Mars present at your outbound crossing,
Earth present when you come back around — and with two knobs (departure day, departure
speed) against two conditions, it is generically findable and never writable in closed
form. Curtis ch. 8 sets up the patched-conic sketch and concedes the real trajectory is
found numerically. So: numerically, with the tools this series already built — lesson 6's
closest-approach scan to score candidates, lesson 13's Newton to keep appointments.

## The standard-textbook take

Free-return trajectories (Curtis ch. 8's lunar version; Apollo's actual fail-safe), and
cycler orbits in the mission-design literature (Aldrin 1985): ballistic figures that
re-encounter two bodies repeatedly, maintained by small corrections. The fiction got the
physics right — Rich Purnell's maneuver in *The Martian* is a genuine class of trajectory,
and this lesson computes its game-world cousin.

## What the game simplifies away

Circular coplanar rails (which makes the coincidences *cleaner* than reality — no
inclination to fight), and corrections modeled as exact vectored burns, per this series'
convention since lesson 13.

## The numerical experiment

### A — the blueprint

Any ellipse tangent at Earth that reaches Mars's orbit has a period between the Hohmann
ellipse's 1.42 yr and infinity — pick the ellipse and you pick when you re-cross Earth's
orbit. Two conditions, two knobs. Solvable, never by formula.

### B — finding the bus

```
grid: 79 departure days x 21 speeds = 1659 flights, 60 had a useful Mars flyby
best ballistic round trip: depart day 30, dv 4300 m/s prograde
  Mars flyby  4,480,207 km at day 158
  Earth return 14,042,978 km at day 787 (round trip 2.07 yr)

fine polish (1-day x 10 m/s grid around the winner):
polished: depart day 30, dv 4250 m/s -> Mars flyby 4,266,954 km (day 158), Earth return 4,794,693 km (day 1488)
```

One departure burn of 4.25 km/s buys: Mars at day 158 (inside its capture range — close
enough to drop a landing party per lesson 16), and Earth again at day 1488, having burned
*nothing* since day 30. Note what the polish did: the coarse grid's best return (14M km at
day 787) gave way to a different, later Earth pass at 4.8M km — the figure crosses Earth's
orbit many times, and the search is honest about which crossing actually coincides.

### C — the fare

The fare exists because of lesson 4: the drive fires quantized pulses, so the planned
4250 m/s is unfireable —

```
planned departure dv 4250 m/s; nearest fireable burn = 13 fine pulses = 4113 m/s (a 137 m/s sin you cannot avoid)

strategy                                 fares (m/s)    total  Mars pass (km)  home kept (km)
pin BOTH appointments exactly              400 + 121    520.7       4,266,954       4,839,710
let Mars float, pin only home                    358    358.0       3,273,320       4,792,418
```

And the honest surprise: **pinning less costs less.** This figure's Mars pass is a taxi
stop, not a slingshot — at 4.3 million km out, Mars pulls 2.4×10⁻⁶ m/s² against the sun's
2.6×10⁻³ m/s² (1,086× stronger). The free return is steered by the *sun* — resonant
timetabling, not gravity theft — so the Mars appointment tolerates a floating pass (it
drifted to 3.3M km, still deep inside taxi range) and the navigator who insisted on
punctuality at both stations paid 45% extra for nothing. **Pin the appointment you need;
let the rest breathe.** (Lesson 19 meets the other kind of flyby — the kind that *is* a
lever.)

Scale check: the round trip after departure cost 358–521 m/s in corrections. Two one-way
Hohmann tickets (lesson 5) run ~11,200 m/s. Once you're on the figure, staying on it is two
orders of magnitude cheaper than starting over — Rich Purnell's whole pitch, computed.

## Break it yourself

1. **Ride it again.** Extend the horizon past day 1488 and let the corrected rider coast
   on. Where's the next Mars pass, and what's the fare to keep it? Chart three full cycles
   and decide whether this figure is a true cycler or a two-appointment wonder.
2. **Shrink the sin.** Section C's 137 m/s quantization sin came from 1% multiplicative
   trim pulses. Re-price the fares with 0.1% pulses (a finer drive) and with lesson 4's
   full ±10% mains only. Fare should scale with sin — does it, or does the 60-day drift
   time dominate?
3. **Force the slingshot.** Re-run Section B demanding a Mars pass under 50,000 km instead
   of 5.4M. The candidates get rare and the post-Mars legs get wild — you are watching the
   figure change species, from sun-steered resonance to Mars-bent assist. How does the
   Earth-return distance behave as the pass tightens? (Bring lesson 19 home for this one.)
