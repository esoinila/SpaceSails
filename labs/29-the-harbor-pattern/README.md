# Lesson 29 — The harbor pattern

*Lesson 23 taught the ship to cross a giant's well cheap; lesson 24 taught it to close the last
co-orbital mile for two small burns. Both END at a harbor — and the owner's playtests kept ending
the same way: the flying worked, then the ship arrived WRONG. In orbit "by luck" at Enceladus
(#180), or drifting near a station with no way to deliver (#175), and never a word on what a safe
arrival even looks like. This lesson measures the SAFE-APPROACH CORRIDOR honestly, per harbor —
the speed-vs-range envelope that turns "arrive somewhere and hope" into a coached glideslope.*

```bash
dotnet run --project labs/29-the-harbor-pattern -c Release
dotnet run --project labs/29-the-harbor-pattern -c Release -- --viz
```

## Why this lesson exists

A harbor has a door, and the two harbor kinds in the game have doors that could not be more
different:

- A **station** (μ=0: Ringside Exchange, Cinder Roost, The Rusty Roadstead, The Tilt) is a
  mass-less thing you **clamp** onto (⚓). Its door is the `DockRule` envelope — a huge,
  gravity-free bubble: coast within **500,000 km**, match to **≤ 8 km/s**, throw the arm.
- A **moon** (μ>0: Enceladus, the #175 delivery) is a thing you **park in orbit at**. Its door is
  the **Hill sphere** — and Enceladus' whole Hill sphere is **under 1,000 km**. There is no clamp;
  "delivered" means the autopilot bent the fall into a tide-stable orbit before the ship punched
  through the moon or sailed past the Hill sphere entirely.

Same word — "arrive" — two utterly different doors. The corridor is the one honest account of what
each door will accept, measured by flying a spread of inbound trajectories straight through the real
N-body simulator with the SAME machinery the autopilot flies (`DockRule`, `OrbitRule`).

## The standard-textbook take

A terminal approach is a **glideslope**: keep your closing speed low enough that you always have
time to null it before you reach the berth. Expressed as a constant time-to-go τ, the safe ceiling
is `v ≤ range / τ`, capped at whatever hard limit the door imposes (the clamp shears above 8 km/s;
the insertion window won't open above 5 km/s). We anchor τ so that AT the door the on-pattern speed
equals the autopilot's own terminal closing speed (`OrbitRule.ApproachClosingSpeed` — 4 km/s at a
station, a few hundred m/s at a deep-well moon). Everything below is that line, measured.

## What the game adds that the textbook doesn't

Real harbors are not perfect points. A station's clamp bubble is gravity-free and forgiving, so it
**never truly refuses you** — arriving hot only costs a fat pulse bill to null the excess first. A
moon's door is tiny and its gravity is real, so an approach that is too hot for its range
**genuinely fails**: it impacts or overshoots, and whether it does depends on the exact tick timing
— which is precisely the owner's "in orbit by luck." The corridor is the envelope that takes luck
out of it. And because every gate is computed from the machinery's own constants, the game can
speak them live: `ApproachCorridor.Read(range, speed)` returns OnPattern / Hot / Missed and the next
gate, which is exactly what the banner NEXT row (#159) and the #160 tutorial narration need.

## The numerical experiment

### A — the harbors and their doors

```
harbor                    class      door: within  speed cap   park r / Hill    handover
----------------------------------------------------------------------------------------
Mercury Compute Farms     station     500,000 km       8 km/s       — (clamp)    3.0e6 km
Cinder Roost              station     500,000 km       8 km/s       — (clamp)    3.0e6 km
Highport Satellite Works  station     500,000 km       8 km/s       — (clamp)    3.0e6 km
The Rusty Roadstead       station     500,000 km       8 km/s       — (clamp)    3.0e6 km
Enceladus                 moon            949 km       5 km/s    313 / 949 km  3,000e3 km
Ringside Exchange         station     500,000 km       8 km/s       — (clamp)    3.0e6 km
The Tilt                  station     500,000 km       8 km/s       — (clamp)    3.0e6 km
```

Every station shares one door (the `DockRule` constants). Enceladus is the outlier: a **949 km**
Hill sphere with a tide-stable park at **313 km** — the number the autopilot circularizes at, and
the number the plan's "🛰 AUTOPILOT HOLDS THE ORBIT — 313 km" banner quotes.

### B — the corridor sweep at a station (Ringside Exchange)

Fly the real approach machinery from each (range, closing speed) inbound state to the clamp
envelope; the cell is the pulse bill to clamp from that gate.

```
World speed at the harbor ~14.6 km/s (one pulse buys ~146 m/s of dv); lane circular 5.30 km/s.

range \ v_close        1 km/s       2 km/s       4 km/s       8 km/s      12 km/s      16 km/s
----------------------------------------------------------------------------------------------
   3,000,000 km        134 p        127 p        112 p        141 p        164 p        180 p
   1,500,000 km         38 p         31 p         16 p         45 p         68 p         84 p
   1,000,000 km         22 p         15 p          0 p         29 p         52 p         68 p
     750,000 km         22 p         15 p          0 p         29 p         52 p         68 p
     550,000 km         22 p         15 p          0 p         29 p         52 p         68 p
```

A station never REFUSES you — the bubble is huge and gravity-free, so the machinery can always null
your excess and coast in. What it costs you is **pulses**. The sweet spot is the `4 km/s` column at
**0 p**: arrive already matched to the lane at exactly the autopilot's coast-in speed and you clamp
for FREE — no burn is needed. Arrive hot (16 km/s → **68 p** from a million km) and you pay a pulse
for every ~1% of world speed you have to shed first. (Arriving *slower* than 4 km/s costs a little
too — the machinery re-accelerates a too-slow drift back up to its closing speed; hence 1–2 km/s
cost 15–22 p, not zero.) The station corridor is a **cost gate**, and the 3,000,000-km row is dear
because that is the very edge of capture range, where the long coast-in re-burns many times.

### C — the corridor sweep at a moon (Enceladus), where approaches FAIL

Same sweep, but now the cell is the pulse bill to a STABLE park — and the failures are real.

```
Hill sphere 949 km, tide-stable park 313 km, capture speed cap 5 km/s.
World speed ~7.6 km/s (one pulse ~76 m/s). 'imp' = fell into the moon.

range \ v_close      0.3 km/s     0.5 km/s     1.0 km/s     2.0 km/s     3.0 km/s     5.0 km/s
----------------------------------------------------------------------------------------------
       5,000 km         27 p         24 p          9 p         19 p           imp        47 p
       2,000 km         29 p         27 p         11 p         21 p         29 p         50 p
         949 km         25 p         23 p         12 p         22 p           imp        46 p
         500 km         26 p         24 p         13 p           imp          imp        47 p
         376 km         26 p         25 p         13 p           imp        30 p         48 p
```

A moon is the opposite of a station. Below ~1–2 km/s the park is reliable and cheap (the **1 km/s**
column, ~9–13 p, is the floor). Above it the failures scatter in — `imp` where the too-hot fall
punches straight through the 252-km moon between ticks. Note they are **not monotone**: 3 km/s
impacts at 500 km but 5 km/s parks there; whether a hot approach crashes depends on the exact phase
and tick timing. THAT non-determinism IS the owner's "in orbit by luck" (#180). The corridor's job
is to keep you in the region where luck plays no part.

### D — a textbook arrival vs a botched one

```
ringside-exchange: inbound from 1,000,000 km
  textbook  (closing   2.0 km/s):  Clamped     15 pulses,   2.00 km/s dv,   1.9 d
  botched   (closing  16.0 km/s):  Clamped     68 pulses,  12.00 km/s dv,   1.9 d

enceladus: inbound from 5,000 km
  textbook  (closing   1.0 km/s):  Parked       9 pulses,   0.95 km/s dv,   0.1 d
  botched   (closing   3.0 km/s):  Impact       0 pulses,   0.00 km/s dv,   0.0 d
```

At Ringside the botch is merely **4.5× the fare** (68 vs 15 pulses) — money, not disaster. At
Enceladus the botch is a **smoking crater**: the on-pattern approach parks for 9 pulses; the hot one
never gets to spend a pulse at all, because it hits the moon first. Same lesson, two prices.

### E — the corridor gates, and the Core API that speaks them

The gates below are printed straight from `ApproachCorridor.For(...)` — the SAME numbers it computes
from the Core constants the autopilot flies with. The README does not hand-write them; the API does.

```
ringside-exchange — StationClamp, glideslope tau = 125,000 s (34.7 h)
  gate                  by range      be under
  handover         3,000,000 km      8.00 km/s
  clamp window       500,000 km      4.00 km/s

enceladus — MoonPark, glideslope tau = 782 s (0.2 h)
  gate                  by range      be under
  handover         3,000,000 km      5.00 km/s
  Hill sphere            949 km      1.21 km/s
  park                   313 km      0.40 km/s
```

`Read(range, closing speed)` — the live verdict + next gate the guidance seam serves:

```
 Ringside Exchange (station clamp):
     2,000,000 km @  6.00 km/s -> OnPattern (ceiling  8.00 km/s, margin   2.00 km/s) | NEXT: clamp window at 500,000 km, under 4.00 km/s
       500,000 km @  4.00 km/s -> OnPattern (ceiling  4.00 km/s, margin   0.00 km/s) | NEXT: clamp window at 500,000 km, under 4.00 km/s
       500,000 km @  7.00 km/s -> Hot       (ceiling  4.00 km/s, margin  -3.00 km/s) | NEXT: clamp window at 500,000 km, under 4.00 km/s
       300,000 km @  9.00 km/s -> Missed    (ceiling  2.40 km/s, margin  -6.60 km/s) | NEXT: clamp window at 500,000 km, under 4.00 km/s
 Enceladus (moon park):
         5,000 km @  4.00 km/s -> OnPattern (ceiling  5.00 km/s, margin   1.00 km/s) | NEXT: Hill sphere at 949 km, under 1.21 km/s
           949 km @  1.20 km/s -> OnPattern (ceiling  1.21 km/s, margin   0.01 km/s) | NEXT: park at 313 km, under 0.40 km/s
           500 km @  3.00 km/s -> Hot       (ceiling  0.64 km/s, margin  -2.36 km/s) | NEXT: park at 313 km, under 0.40 km/s
           400 km @  6.00 km/s -> Missed    (ceiling  0.51 km/s, margin  -5.49 km/s) | NEXT: park at 313 km, under 0.40 km/s
```

**OnPattern** = at or under the glideslope, a clean cheap arrival is on track. **Hot** = over the
pattern but recoverable — bleed speed before the next gate (at a moon it courts the overshoot).
**Missed** = inside the door and over the hard capture cap: overshooting the berth / too hot to grab
or insert. That last verdict is the #175 dead-end named honestly — "there, but no way in" — and the
cure is the same as any missed approach: slow down and come around again.

With `--viz`: the Enceladus corridor in the moon frame — a textbook 0.5 km/s fall coiling into a
park beside a botched 5 km/s fall that punches through.

## Break it on purpose

1. **Arrive at exactly 4 km/s at a station and pay nothing.** Section B's `4 km/s` column is 0 p:
   the machinery's coast-in speed IS the clamp speed, so no burn fires. Nudge to 3 or 5 and watch
   the pulses reappear as it corrects you back onto its line.
2. **Find the moon's luck band.** Fly Enceladus from 500 km at 2 km/s, then 5 km/s: one impacts,
   one parks. Then swap them by hand and see the tick timing flip the outcome — the corridor exists
   precisely because that coin-flip is not something to ship.
3. **Ask a station for the "Missed" verdict from far away.** `Read(2e9, 20000)` is Hot, not Missed
   — you are fast but there is still room to slow. Missed is reserved for *inside the door*, where
   there is not.

## The framing rule, kept

Standard physics presented as standard: a terminal glideslope is 1960s approach doctrine. The gates
are ours — but they are not invented, they are the game's own machinery constants (`DockRule`
envelope, `OrbitRule` closing speed / Hill sphere / park radius) read back as a speed-vs-range
envelope, then measured against a spread of real flown approaches to prove on-pattern is cheap and
safe while hot is dear or fatal. Every number here came from running `Probe.cs`; change the code and
rerun before trusting the tables.
