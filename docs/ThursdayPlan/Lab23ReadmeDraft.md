# Lesson 23 — The moon run (LEAD'S DRAFT for labs/23-the-moon-run/README.md)

> LANE NOTE: this is the lead's math exposition. Lane 2: move this into
> labs/23-the-moon-run/README.md, keep the prose, and replace every ⟨RUN: …⟩ placeholder
> with output actually printed by the probe. IRONCLAD RULE applies — no hand-typed numbers.
> Delete this file after the move.

*The autopilot's Wednesday-night Titan approach burned ⟨RUN: old pulses⟩ pulses fighting
Saturn. The geometry says the trip costs a fraction of that — if you stop treating the well
as an enemy and start treating it as the road. This is the lesson where the game's autopilot
learns orbital mechanics, and the Core code that flies it is the code on this page.*

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
⟨RUN: table — old autopilot: pulses, total Δv, wall time | Hohmann closed form: Δv₁, Δv₂, TOF⟩
```

⟨RUN: one paragraph reading the actual ratio off the table — expected ~5×, state the real number.⟩

### B — the window

The lead angle and the timetable, computed then verified against the rails:

```
⟨RUN: lead angle α, synodic period, table of next 3 window openings from t=0⟩
```

### C — Lambert rides the well: the porkchop plate

`TransferPlanner.Solve`'s own scan, printed. Departures span one synodic cycle, TOF spans
[0.4, 1.6]× the Hohmann time; each cell is one certified Lambert solve in Saturn's frame;
the cost is departure Δv + arrival matching Δv:

```
⟨RUN: the plate, best cell marked⟩
```

Two features of the plate are the lesson. The FLOOR sits at ⟨RUN: floor Δv/TOF⟩ — Hohmann,
found by a solver that has never heard of apsides. And the null column at 180°: Lambert's
universal-variable form divides by sin Δθ there (the geometry that carries no plane
information), so the honest solver refuses the cell rather than certify garbage — the scan
simply steps over the blind spot, and the neighboring 179°/181° cells price within a few
per mille of the closed form.

### D — flown: the moon run, end to end

The planner's winning schedule applied in the real N-body sim — burn, coast the well,
hand over to the ordinary capture machinery inside Titan's Hill sphere:

```
⟨RUN: timetable — burn epoch/magnitude, closest approach to Titan, capture handover, END-TO-END pulse bill new vs old⟩
```

⟨RUN: closing paragraph with the real headline numbers: N pulses → M pulses.⟩

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
