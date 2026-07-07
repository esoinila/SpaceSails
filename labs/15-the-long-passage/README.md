# Lesson 15 — The long passage

*Six years is where small numbers stop being small. A 1 m/s error is a footnote on a Mars
run; over a Saturn passage it compounds into planetary-scale misses — and the price of
fixing it is set by the calendar, not by the size of the sin.*

```bash
dotnet run --project labs/15-the-long-passage -c Release
```

## Why this lesson exists

Everything so far happened on inner-system clocks: hours for a slug (lesson 13), months for
a Mars run (lessons 5, 14). This lesson sails for **Saturn** — a six-year passage — and
prices four things honestly: what the outer system costs at all, what one passage costs to
*solve* at six-year range, what tiny departure sins compound into, and the navigator's oldest
law: a correction's price is set by **when** you pay it.

## The standard-textbook take

Curtis ch. 6 prices the trip (Hohmann, Section A) and ch. 5 finds the arc (Lambert, lesson
14). Mid-course corrections live in ch. 8 territory and in every mission-design memo since
Mariner: fly, track, correct early, correct small. This lesson computes that folklore into a
table.

## What the game simplifies away

Circular coplanar rails, no gravity assists (a Jupiter flyby would cut the Saturn bill — the
rails support it, lesson 6 has the closest-approach tools; consider it the series' standing
homework), and departure from the game's spawn state (lesson 14's Section D explains the
head start baked into it).

## The numerical experiment

### A — the tyranny of the outer system

```
target      dv1 (m/s)  dv2 (m/s)    total  TOF (years)  window every (days)
mars             2945       2649     5593         0.71                779.9
jupiter          8794       5643    14437         2.73                398.9
saturn          10298       5439    15736         6.09                378.1
uranus          11281       4658    15940        16.05                369.7
neptune         11653       4055    15708        30.58                367.5
```

Read the shape. Past Jupiter the dv bill nearly stops growing — you are escaping the sun
either way — but the **clock explodes**. Neptune's window repeats every ~367 days; its
passage lasts ~30.6 *years*. The outer system is not expensive. It is **long**. The window
is not the scarce resource out here; lifetime is. (And a wrinkle worth noticing: Neptune's
total is *cheaper* than Uranus's — the arrival burn falls faster than the departure burn
rises. The formula has opinions the intuition doesn't.)

### B — one real passage, solved

Lesson 14's whole toolchain, reused at six-year range: Lambert scans the window cheaply,
shooting finishes the job through all nine bodies.

```
Lambert scan of one synodic cycle (378.1 days, 7-day grid): best departure day 49, total dv 15.06 km/s (TOF fixed at 6.10 years)
shooting through all nine bodies: converged True in 6 Newton iterations, final miss 2,979 km (tolerance 10,000 km)
correction the real world demanded on top of Lambert: 150.54 m/s over a 6.1-year arc
arrival speed relative to Saturn: 10.25 km/s (the braking bill, lesson 4's pulses will have to pay it)
cost of one six-year flight, adaptive integrator: 51 ms on this dev machine
   (lesson 10's point at passage scale: the Newton solve above burned 19 such flights)
```

Two numbers deserve a stare. The nine-body correction on top of Lambert was 14 m/s on the
Mars run (lesson 14); at Saturn range it is **150 m/s** — a six-year arc crosses Jupiter's
country, and the two-body lie compounds like everything else out here. And Newton needed 6
iterations, not lesson 14's 1: the target sits 1,000,000 km from a gas giant whose pull
bends the final approach — the miss is no longer nearly-linear in the guess, and the trust
region earns its keep.

### C — six years is where small numbers stop being small

Same converged passage, departure velocity deliberately wrong by a hair, along-track:

```
 error (m/s)  naive error*TOF (km)  actual miss (km)  amplification
        0.01                 1,925             7,505            3.9x
        0.10                19,250            74,429            3.9x
        1.00               192,501           718,250            3.7x
       10.00             1,925,014         8,133,896            4.2x
```

The naive column is what a straight-line mind expects (error × time). The actual miss is ~4×
worse, and the amplification is the orbit dynamics themselves: an along-track error changes
the orbit's *energy* (vis-viva, lesson 1), so the arcs don't drift apart — they arrive at
different times moving at km/s. Lesson 4's quantized pulses guarantee you never leave with
the exact right velocity. On a Mars run that footnote is survivable. Here it is the story.

### D — the same 1 m/s sin, absolved at different confessionals

Fly the 1 m/s-wrong passage, then solve a fresh arc to the same doorstep from wherever the
sin has carried you — at seven different dates:

```
  corrected at  years remaining  correction dv (m/s)  final miss (km)
        day 30             6.02                 1.26            7,882
       day 180             5.61                 2.80              931
       day 365             5.10                 3.93            8,338
       day 730             4.10                 5.96              258
      day 1460             2.10                13.68              435
      day 2000             0.62                47.42            1,058
      day 2198             0.08               347.89            2,938
```

Same sin. Same destination. The price is set only by the calendar: absolve it in the first
month and the bill (1.26 m/s) is barely more than the sin itself; sail on it for six years
and the deathbed confession costs **348 m/s** — 350× the sin, growing toward a whole new
departure burn. Mission control's daily trajectory meetings are not bureaucracy. They are
compound interest management.

### E — nobody can watch you for six years

Lesson 8's cone, evaluated at passage scale:

```
cone half-width at arrival, CREWED ship (can maneuver unobserved): 37,166 AU
cone half-width at arrival, mass-driver POD (cannot burn at all):   0.13 AU (19,260,136 km)
even the pod's cone crosses the 500,000 km capture envelope 57 days after the fix — measurement noise alone (100 m/s velocity sigma) does it
```

37,166 AU is not a typo — it is half a light-year of honest uncertainty, the cone model
telling you it has nothing to say. Even a pod that *cannot burn at all* outgrows the capture
envelope in 57 days on measurement noise alone. A six-year track does not exist. Long-haul
traffic is not tracked; it is **met** — you compute where the passage ends (Sections B–D)
and re-acquire at the far end, lesson 8's telescope work scheduled six years in advance.

## Break it yourself

1. **Confess in pulses.** Section D's corrections are exact vectors, but lesson 4 says thrust
   comes quantized: take the day-730 correction (5.96 m/s) and round it to the nearest
   ±10%-pulse combination your ship can actually fire. Fly the rounded correction and measure
   the residual miss — then decide whether a second, later correction is cheaper than a finer
   first one.
2. **Cruise on a lazy clock.** Rerun Section B's converged flight with `maxTimeStep: 604800`
   (one week). Compare the arrival against the 3600 s ceiling, then explain where the
   difference is born — the empty AU mid-passage, or the last million km where Saturn's
   near-field bends the approach? (Lesson 3 knows.)
3. **Sign the Neptune contract.** Section A quotes Neptune at 30.58 years. Re-aim Section B
   at Neptune with a 12-year TOF instead and read the dv bill for buying the clock down.
   Who signs that contract — and would lesson 11's electric sandbox change the answer?
