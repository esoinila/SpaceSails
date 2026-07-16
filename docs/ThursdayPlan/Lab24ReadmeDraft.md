# Lesson 24 — The last mile (LEAD'S DRAFT for labs/24-the-last-mile/README.md)

> LANE NOTE: lead's math exposition, Lab-23 pattern. Move to labs/24-the-last-mile/README.md,
> strip this block, keep the prose, fill every ⟨RUN: …⟩ from the probe's real output. Delete
> this file after the move.

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

The trade table is TACTICAL. ⟨RUN: one-line reference to Section D's table⟩ The owner's
ruling from the playtest that spawned this lesson: the cheap bus and the fast bus are both
honest answers, and which one you buy depends on who is chasing you. Heat on your tail turns
"wait 18 days and pay one pulse" into "pay 80 pulses and be gone within hours" — same
physics, different captain.

## The numerical experiment

### A — the last mile is the expensive mile

The owner's exact geometry: same lane as Ringside Exchange (r = 1.35×10⁹ m), 92,640 km
behind it. Fly the OLD approach loop; price the closed form beside it:

```
⟨RUN: table — legacy loop pulses/Δv/time vs phasing k=1 Δv/wait; the 229-p decline context⟩
```

⟨RUN: paragraph with the real ratio — expected ~two orders of magnitude in Δv.⟩

### B — the bus math, checked against the rails

```
⟨RUN: closure check table (gap, k, family, residual), and the k-table for the Ringside gap⟩
```

The closure residuals are machine epsilon — the phasing identity is algebra, not
approximation. The two-body LIE (moons tugging, the sun's tide) shows up only when flown,
which is Section C's job.

### C — flown: enter, coast k laps, exit, arrive

The planner's two-burn schedule through the real N-body integrator, rehearsal-style:

```
⟨RUN: timetable — burn 1, k laps, burn 2, final miss distance and relative speed vs the dock envelope⟩
```

⟨RUN: honest paragraph: the two-body prediction vs the flown arrival — how much did the lie
cost, and does it fit inside the 500,000 km / ≤8 km/s dock envelope with margin?⟩

### D — the tactical table: cheaper vs sooner

```
⟨RUN: the full alternatives table the planner now returns — phasing k=1..6 both families +
the direct hop row: Δv, pulses, wait, arrival⟩
```

Read it like a bus schedule with a wolf at the stop. ⟨RUN: sentence naming the actual
cheapest and the actual fastest rows.⟩

## Break it on purpose

1. **Ask Lambert to phase.** r1 = r2, TOF = one lane period. Null — and now you know the
   blind spot from the inside: the plane vanishes at 2π, but the PERIOD still speaks.
2. **Dive for a 310° catch-up in one lap.** The kernel returns null. Compute the far apsis
   yourself (2a − r) and see where the ellipse wanted to go.
3. **Stack the families.** Close the same 45° gap with a k=2 dip and a k=2 swell; verify both
   arrive, then explain to your navigator why one cost six times the other.

## The framing rule, kept

Standard physics presented as standard: Curtis chs. 2, 6 (§6.5). The tactics are ours; the
algebra is 1960s rendezvous doctrine, computed honestly on our rails.
