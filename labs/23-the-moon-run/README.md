# Lesson 23 — The moon run

*The owner's Wednesday-night Titan approach hemorrhaged fuel fighting Saturn (#146); flown
honestly from the Enceladus doorstep, that same reset loop burns 677 pulses. The geometry says
the trip costs a fraction of that — if you stop treating the well as an enemy and start
treating it as the road. This is the lesson where the game's autopilot learns orbital mechanics,
and the Core code that flies it is the code on this page.*

```bash
dotnet run --project labs/23-the-moon-run -c Release
dotnet run --project labs/23-the-moon-run -c Release -- --viz
```

## Why this lesson exists

Lesson 17 proved the toolchain transfers to Saturn's pocket system; this lesson makes the
SHIP use it. Until today, the armed autopilot approached a moon the brute way: point at the
aim, re-set the whole velocity vector to `moonVelocity + 4 km/s toward target`, and pay
again every time Saturn's pull dragged the relative speed back over the limit. Each re-set
throws away velocity the well GAVE us and buys it back at full pulse price. Inside a
gravity well, the straight line is the most expensive path there is.

The fix is one idea: **inside a giant's Hill sphere, plan in the giant's frame.** An
Enceladus→Titan trip is not "chase Titan through space" — it is "step from a 12.6 km/s
inner lane to a 5.6 km/s outer lane of the same rotary." Two burns at the right moments;
Saturn does everything in between for free.

## The standard-textbook take

Three chapters of Curtis, each earning one section below:

- **Vis-viva** (ch. 2): on any two-body orbit, `v² = μ(2/r − 1/a)`. Speed is a function of
  where you are and how big your ellipse is — nothing else. This single line prices every
  coast in the lesson.
- **Hohmann** (ch. 6): the cheapest two-burn hop between circular lanes is the ellipse
  tangent to both — periapsis on the inner lane, apoapsis on the outer. Burn once to
  stretch your circle into the transfer ellipse (Δv₁), coast half of it, burn again to
  circularize (Δv₂). Both burns are PROGRADE — you never fight the well, you only change
  lanes. Time of flight is half the transfer ellipse's period: `TOF = π√(a_t³/μ)`.
- **The window** (ch. 6 §4, "phasing"): the hop only works if Titan is at the arrival point
  when you get there. Titan must LEAD Enceladus at departure by `α = 180° − n_Titan·TOF`
  (it covers `n_Titan·TOF` of arc while we fly our 180°). That alignment repeats every
  synodic period `T_syn = 1/|1/T_Enc − 1/T_Titan|` — the bus timetable in one formula.
- **Lambert** (ch. 5): Hohmann assumes you sit ON the inner lane and may wait for the
  perfect moment. A real ship is mid-well, mid-arc, mid-emergency. Lambert's problem drops
  the assumptions: given where you ARE, where the moon WILL BE, and a clock, it returns the
  connecting arc — any departure point, any timing, elliptical rails included. The solver
  (universal variables, Algorithm 5.2) is one root-find on a single scalar z; lesson 14
  built it, `TransferMath.Lambert` in Core is that solver grown up: adaptive hyperbolic
  bracket, certified answer or an honest `null`. Sweep Lambert over departure×TOF and you
  get the porkchop plate; its floor is Hohmann, rediscovered numerically.

One honest disclosure, same as lesson 14's: Lambert is exact in a universe with ONE
attractor. Ours has Saturn plus two massive moons (and the sun outside). So the house
pattern holds: **Lambert proposes, the integrator disposes** — the plan is only accepted
after the real N-body `Simulator` flies it (in the game, that verdict is the autopilot's
arm-time rehearsal, #151's promise).

## What the game prices that the textbook doesn't

Pulses. One pulse buys Δv equal to 1% of your current heliocentric speed
(`OrbitRule.PulsesFor` — the same public kernel the autopilot spends with, so the lab's
bill and the game's bill cannot drift apart). Note the economics this creates: deep in
Saturn's well you ride at ~22 km/s heliocentric, so a pulse there is a BIG pulse — the well
doesn't just shorten the trip, it discounts the fare. The Oberth effect, denominated in the
game's own currency (lesson 04's quantization sin, now working for us).

## The numerical experiment

### A — the bill as flown vs. the bill as priced

First, reproduce Wednesday honestly: fly the OLD approach loop (velocity re-sets) from the
Enceladus doorstep to Titan capture in the real simulator, and count. Then price the same
trip with vis-viva + Hohmann's closed form:

```
old autopilot (flown)            pulses   total dv (km/s)   sim days   captured
Enceladus doorstep -> Titan         677              54.5        3.9       True

Hohmann closed form (priced)     dv1 (km/s)   dv2 (km/s)    total    TOF (d)
Enceladus -> Titan                     3.67         2.37     6.04       3.69

vis-viva lane speeds: v_Enceladus 12.54 km/s (inner), v_Titan 5.57 km/s (outer).
```

The flown loop paid **54.5 km/s (677 pulses)** to do a job the geometry prices at **6.04
km/s** — a **9.0× overspend**, worse than even the ~5× the parent-frame arithmetic promised,
because the reset loop doesn't just fail to use the well, it actively re-buys the speed
Saturn keeps handing back. Every time the fall drives the ship over the 5 km/s cap, the loop
re-sets the whole velocity vector and the well's gift is thrown away and purchased again at
full pulse price. That is the hemorrhage this lesson stops.

### B — the window

The lead angle and the timetable, computed then verified against the rails:

```
required Titan lead at departure (alpha = pi - n_Titan*TOF): 96.6 deg
synodic period (the bus interval): 36.8 h
current Titan lead over the doorstep at t=0: 302.7 deg

window opening        t (h from now)    t (days)
#1                              21.0        0.88
#2                              57.8        2.41
#3                              94.6        3.94
```

Titan must lead Enceladus by 96.6° at departure; right now it leads by 302.7°, so the ship
waits for the inner lane to catch up. Because the ship laps Titan once every **36.8 h**
synodic beat, the windows come at 21.0 h, then a bus every 36.8 h after that. Miss one and
the honest answer, exactly as in lesson 17, is: there's another one tomorrow.

### C — Lambert rides the well: the porkchop plate

`TransferPlanner.Solve`'s own scan, printed. Departures span one synodic cycle, TOF spans
[0.4, 1.6]× the Hohmann time; each cell is one certified Lambert solve in Saturn's frame;
the cost is departure Δv + arrival matching Δv:

```
dep hr  |    2.0    2.5    3.0    3.5    4.0    4.5    5.0    5.5  <- TOF (days)
-------------------------------------------------------------------
      0 |   25.5   25.9   24.5   23.2   22.1   21.0   20.0   19.0
      4 |   21.3   20.7   21.6   23.2   25.1   26.1   25.0   23.9
      8 |   17.9   16.2   16.2   17.1   18.7   20.5   22.5   24.7
     12 |   14.8   12.6   11.9   12.2   13.1   14.4   16.0   17.8
     16 |   11.6    9.3    8.4    8.3    8.8    9.6   10.8   12.1
     20 |    9.0    7.2    6.3    6.0    6.1    6.5    7.1    8.0
     24 |   10.7    9.2    8.3    7.8    7.4    7.1    6.9    6.8
     28 |   15.6   14.0   13.0   12.2   11.5   10.9   10.3    9.7
     32 |   21.9   20.0   18.7   17.6   16.7   15.8   14.9   14.1
     36 |   25.4   25.9   24.5   23.2   22.1   21.0   20.0   19.0

cheapest hand-scanned cell: depart hour 20, TOF 3.5 d, total dv 6.01 km/s (Hohmann floor: 6.04 km/s at 3.69 d)
  Lambert at exactly 180 deg: null (refused);  at 179 deg: SOLVED.

transfer to titan: burn 3.69 km/s in 22.0 h, arrive in 3.7 d at 2.33 km/s rel (est. 46 pulses)
depart t+22.0 h, TOF 3.69 d, planned dv 6.02 km/s, arrival 2.33 km/s rel, est. 46 pulses
```

Two features of the plate are the lesson. The FLOOR sits at **6.02 km/s at 3.69 d** — Hohmann
(6.04 km/s), found by a solver that has never heard of apsides; the plate's own cheapest
hand-scanned cell (6.01 km/s at hour 20) even dips a hair under the closed form by trading a
slightly off-tangent phasing the formula's fixed geometry can't express. And the 180° blind
spot: Lambert's universal-variable form divides by sin Δθ there (the geometry that carries no
plane information), so the honest solver refuses the cell rather than certify garbage — asked
for exactly 180° it returns `null`, asked for 179° it solves. The blind spot is measure-zero,
so the coarse grid simply steps over it (no `-` cells appear), and the neighboring cells price
within a few per mille of the closed form.

### D — flown: the moon run, end to end

The planner's winning schedule applied in the real N-body sim — burn, coast the well,
hand over to the ordinary capture machinery inside Titan's Hill sphere:

```
leg                                                            event
departure burn                      3.69 km/s at t+22.0 h, 24 pulses
ballistic transfer                3.69 d coast, closest 73 Mm from Titan
capture handover (old loop)       72 pulses, 6.24 km/s, inserted True
capture range for reference          3000 Mm (57.2 Titan Hill radii)

END-TO-END pulse bill:  new 96 pulses  vs  old 677 pulses  (a 7.1x saving)
                        new 9.92 km/s dv  vs  old 54.5 km/s dv
```

One prograde departure burn (24 pulses) drops the ship onto a Lambert arc that coasts 3.69
days and passes **73 Mm from Titan** — well inside its capture range — where the ordinary
`OrbitRule` machinery, now working at short range instead of fighting the whole well, closes
the arrival in 72 pulses. The headline: **677 pulses → 96 pulses**, a 7.1× cut, with the Δv
falling from 54.5 km/s to 9.92 km/s. The well was never the enemy; the reset loop was.

With `--viz`: Saturn-centric scene — the transfer arc with its ghost ship and burn/closest
markers, and Wednesday's spiral-of-resets as a toggleable comparison group.

## Break it on purpose

1. **Hit the blind spot.** Ask Lambert for exactly 180° (`prograde` both ways). Null. Now
   nudge the clock by one minute — solved. Why can a millisecond of geometry matter to a
   method that's exact everywhere else? (Curtis 5.3 knows.)
2. **Demand the Hohmann TOF at the wrong phase.** Fix TOF at π√(a_t³/μ) and depart 18 hours
   off the window. Lambert still SOLVES — read what the bill becomes, and understand why
   the timetable exists.
3. **Shrink the wait.** Give `TransferPlanner` a `MaxWaitSeconds` smaller than the time to
   the next window and watch the refusal reason it returns. The planner would rather tell
   you "no" than quote you Wednesday's price.

## The framing rule, kept

Standard physics presented as standard: Curtis chs. 2, 5, 6. Nothing speculative in this
lesson — the fictional part of SpaceSails is WHERE the moons are, never HOW falling works.
