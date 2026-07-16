# Lesson 24 — The last mile

*Lesson 23 taught the ship to cross a giant's well for a tenth of the old price. Then the
owner tried to reach a station 92,640 km away — practically next door — and the autopilot
declined at 229 pulses. The LAST mile was the expensive one. This lesson is about why "almost
there" is its own orbital problem, and why the answer is to change your clock, not your path.*

```bash
dotnet run --project labs/24-the-last-mile -c Release
dotnet run --project labs/24-the-last-mile -c Release -- --viz
```

## Why this lesson exists

Everything in lessons 14–23 moves BETWEEN lanes: Lambert arcs, Hohmann hops, porkchop plates.
None of it can move you along your OWN lane. Ask Lambert to fly you to a point one lap ahead
on the same circle and it refuses — correctly — because that arc sweeps 2π, the one geometry
where two positions and a clock stop naming a unique plane (lesson 23 called it the blind
spot and stepped around it; this lesson is what lives inside it). And the brute alternative,
pointing the nose and throttling, is lesson 23's hemorrhage in miniature: you leave the lane,
Saturn charges you for it, and you buy the lane back at full price.

The orbital answer is older than spaceflight: **you don't chase someone on your own lane —
you change your period.** Drop your periapsis a hair and your year gets shorter; each lap you
gain a little phase on everyone still riding the lane. Ride k laps, burn back up, and the
target arrives at your doorstep exactly as you return to it. Two small burns. The well does
the chasing.

## The standard-textbook take

**The phasing maneuver** (Curtis ch. 6.5). On a circular lane of radius r and period
T = 2π√(r³/μ), a target leads you by phase gap g. Enter an ellipse whose period is

    T_ph = T · (1 − g / 2πk)        (dip inside: catch a leading target in k laps)
    T_ph = T · (1 + (2π − g) / 2πk) (swell outside: let the target lap you instead)

Kepler III names the ellipse from its period (a³ ∝ T²); vis-viva prices the burn
(Δv = |√(μ(2/r − 1/a)) − √(μ/r)|, paid once entering and once returning — the burn point
stays an apsis because you only touch the along-track speed). After exactly k laps you are
back where you burned, the target is there too, and the closure is not approximate — it is
an identity of the two periods.

Two families, one trade. The dip is usually the cheap way to catch up; the swell is how you
let a TRAILING target come to you (or how you catch a huge lead without diving at the
planet — a one-lap dip for a 310° gap needs an ellipse through Saturn's center, and the
kernel refuses it honestly). And within each family, **k is the fare dial**: more laps,
smaller period change, cheaper burns, longer wait.

## What the game adds that the textbook doesn't

The trade table is TACTICAL. The planner returns the whole timetable — every k and both
families plus the direct hop — as a cheaper-vs-sooner bus schedule (Section D). The owner's
ruling from the playtest that spawned this lesson: the cheap bus and the fast bus are both
honest answers, and which one you buy depends on who is chasing you. Heat on your tail turns
"wait 37 days and pay 27 m/s" into "pay 48 m/s and arrive 20 days sooner" — same physics,
different captain.

And one honesty the textbook skips entirely: our stations ride AUTHORED rails, not perfect
Kepler circles. Ringside's authored period is 0.024% off the Kepler period for its radius, so
the closed form's Kepler closure and the real rail drift apart a little every lap — which is
exactly why the cheapest bus turns out to be k=2, not the k=6 the textbook Δv formula alone
would pick (Section B). We compute both and label which is which.

## The numerical experiment

### A — the last mile is the expensive mile

The owner's exact geometry: same lane as Ringside Exchange (r = 1.35×10⁹ m), 92,640 km
behind it. Fly the OLD approach loop; price the closed form beside it:

```
legacy loop (flown to 1,000 km)    pulses   total dv (km/s)    reached
92,640 km behind -> Ringside           28               4.0       True

phasing k=1 dip (priced)          enter (m/s)   exit (m/s)    total   wait (d)
92,640 km behind -> Ringside            19.51        19.51     39.4       18.3

lane speed at the ring: v_circular 5.30 km/s; current gap to Ringside 3.93 deg (92640 km of arc).
```

One brute-force attempt to reach the station spent **4.0 km/s (28 pulses)** — pointing at a
neighbour on its own lane and leaving the rail to force the gap shut — and it arrives at the
**4 km/s approach speed it cannot shed**, so it screams past and must re-buy the whole approach
to hold: the endless re-set the **~229-pulse rehearsal** the armed autopilot declined (#155) was
only the estimate that stopped it before it flew this bill. That first 4.0 km/s is already
**102×** the 39 m/s the phasing closed form costs to close the same gap — and the phasing arrives
*matched*, not screaming past. (A mass-less station never opens an insertion window, so the old
loop can only ever "Approach"; there is no cheap terminal capture to bail it out.)

### B — the bus math, checked against the rails

```
Kepler period at the ring radius 1.3500e9 m: 1600222.9 s. Ringside's AUTHORED
period: 1600600.0 s — 0.024% off Kepler. That tiny mismatch is the whole story below.

k   family    enter m/s   exit m/s  TOTAL m/s   wait d    res_Kepler  res_authored
----------------------------------------------------------------------------------
1   dip           19.51      19.51      39.37     18.3     0.00E+000    -1.46E-003
2   dip            9.70       9.70      27.45     36.8     0.00E+000    -2.94E-003
3   dip            6.46       6.46      30.47     55.4     0.00E+000    -4.42E-003
4   dip            4.84       4.84      36.33     73.9     0.00E+000    -5.90E-003
5   dip            3.87       3.87      43.09     92.4     0.00E+000    -7.38E-003
6   dip            3.22       3.22      50.24    110.9     7.11E-015    -8.86E-003
1   swell        898.48     898.48    1798.34     36.8     0.00E+000    -2.94E-003
2   swell        589.99     589.99    1181.73     55.4    -3.55E-015    -4.42E-003
3   swell        440.26     440.26     882.94     73.9     0.00E+000    -5.90E-003
4   swell        351.37     351.37     706.27     92.4    -3.55E-015    -7.38E-003
5   swell        292.42     292.42     590.00    110.9     0.00E+000    -8.86E-003
6   swell        250.43     250.43     508.27    129.4     0.00E+000    -1.03E-002
```

Two columns tell the whole lesson. **res_Kepler** is machine epsilon down the whole table —
the phasing identity is algebra, not approximation: build the ellipse from the Kepler period
and it closes the gap to the last bit. **res_authored** is NOT zero, and it grows linearly with
k — because Ringside actually rides its authored rail, 0.024% slow, and that offset accumulates
`(n_auth − n_kepler)·k·T_ph` of drift over the coast. That drift is why the ENTER burn shrinks
with k (a smaller period change per lap) but the EXIT burn has to grow to re-match a target the
Kepler math put in slightly the wrong place. The total is a **U**: the cheapest bus is **k=2
(dip) at 27.5 m/s**, not k=1 (too much enter Δv) and not k=6 (too much accumulated authored
drift). If the rail ran at its exact Kepler rate, exit would mirror enter and k=6 would win; the
authored offset is what bends the sweet spot inward. The two-body LIE, showing up in the algebra
before a single step is flown — Section C flies it to see how much survives.

### C — flown: enter, coast k laps, exit, arrive

The planner's two-burn schedule through the real N-body integrator, rehearsal-style:

```
planner winner: rendezvous ringside-exchange: 2 lap dip phasing, 27 m/s over 36.8 d, close within 18 m/s rel (est. 2 p)
  depart t+10 min, 2 burns, wait 36.8 d, planned dv 27.5 m/s, est. 2 pulses

leg                                                                  event
burn 1 (enter phasing)                       9.7 m/s at t+10 min, 1 pulses
coast k laps (ballistic)                           36.8 d in Saturn's well
burn 2 (re-match at Ringside)               17.7 m/s at t+36.8 d, 1 pulses

flown arrival vs dock envelope (coast within 500000 km, close under 8 km/s):
  miss distance         4.92 Mm  (INSIDE the 500 Mm envelope, 101.5x margin)
  relative speed       0.002 km/s (INSIDE the 8 km/s cap)
```

The phasing closed the **92,640 km along-track gap** that drifting alone never can — you cannot
catch a neighbour on your own lane by waiting. The two-body plan predicted a machine-epsilon
Kepler closure; flown through Saturn plus two moons and the sun's tide over 36.8 days it lands
**4.9 Mm out at 2 m/s matched** — the lie is real but small, well inside the game's dock coaching
envelope (500,000 km / ≤8 km/s) with 100× distance margin. Two pulses, both under a metre-per-
second of the ship's world speed. Docking stays the captain's click.

### D — the tactical table: cheaper vs sooner

```
row                       total dv (m/s)   pulses   wait (d)  arrival (d)
-------------------------------------------------------------------------
phasing k=2 (dip)                   27.5        2       36.8         36.8
direct hop                          47.7        2        0.8         16.6
phasing k=1 (dip)                   39.4        2       18.3         18.3
phasing k=1 (swell)               1798.3       14       36.8         36.8
phasing k=2 (swell)               1181.7       10       55.4         55.4
phasing k=3 (dip)                   30.5        2       55.4         55.4
phasing k=4 (dip)                   36.3        2       73.9         73.9
phasing k=3 (swell)                882.9        8       73.9         73.9
phasing k=4 (swell)                706.3        6       92.4         92.4
phasing k=5 (dip)                   43.1        2       92.4         92.4
phasing k=5 (swell)                590.0        5      110.9        110.9
phasing k=6 (dip)                   50.2        2      110.9        110.9
phasing k=6 (swell)                508.3        4      129.4        129.5
```

Read it like a bus schedule with a wolf at the stop. The **cheapest** row is `phasing k=2 (dip)`
at 27 m/s, but you wait 36.8 days for it; the **soonest** is the `direct hop` — the near-co-orbital
arc the porkchop can still price on its bounded single lap — arriving in 16.6 days for 48 m/s,
1.7× the fare to be 20 days sooner. No heat, ride the cheap bus and wait it out; heat on our tail,
and the captain pays the fare to be gone sooner. Same physics, different captain. Every row is a
real, feasible plan the planner returns in one call — the winner is flown today (Section C); the
rest are the choices the tactical UI (#159) will offer.

With `--viz`: Saturn-centric scene — the phasing loop with its ghost ship and enter/re-match
markers, Ringside and the two moons on their rings, and the near-co-orbital direct hop and the
legacy point-and-throttle chase as toggleable comparison groups.

## Break it on purpose

1. **Ask Lambert to phase.** r1 = r2, TOF = one lane period. Null — and now you know the
   blind spot from the inside: the plane vanishes at 2π, but the PERIOD still speaks.
2. **Dive for a 310° catch-up in one lap.** The kernel returns null. Compute the far apsis
   yourself (2a − r) and see where the ellipse wanted to go.
3. **Stack the families.** Close the same 45° gap with a k=2 dip and a k=2 swell; verify both
   arrive, then explain to your navigator why one cost six times the other (look at the swell
   rows above — a swell lets the target lap you, the long way round, and pays for the privilege).

## The framing rule, kept

Standard physics presented as standard: Curtis chs. 2, 6 (§6.5). The tactics are ours; the
algebra is 1960s rendezvous doctrine, computed honestly on our rails — including the honest
admission that our rails are authored, not perfectly Keplerian, and what that costs per lap.
