# Lesson 17 — The pocket solar system

*Saturn's moon family is the entire course, replayed at 1/1000 scale: the same radius
ratios, the same algebra, the same tools — but the clock runs 270× faster, and the transfer
window comes every 36 hours instead of every 378 days.*

```bash
dotnet run --project labs/17-the-pocket-solar-system -c Release
```

## Why this lesson exists

Everything lessons 14–15 built — Lambert seeds, shooting finishers, porkchop plates — was
developed on the sun's system, with its months-long arcs and once-a-year windows. If the
methods are really about *physics* and not about that particular system, they should
transfer to any primary without edits. Saturn's moons are the test: Enceladus→Titan has
almost exactly Earth→Jupiter's radius ratio (5.13 vs 5.20), so the pocket system is the big
one scaled down — same course, different μ. This lesson runs the whole toolchain there,
verbatim, and measures the one thing the pocket has that the two-body textbook doesn't: the
sun outside, tugging on the entire pocket at once.

## The standard-textbook take

Nothing new is *needed* — that is the point. Hohmann (Curtis ch. 6) and Lambert (ch. 5)
don't care whose μ they're given. What the textbook won't tell you is how clean the
approximation gets when your whole route sits 2% of the way to the primary's Hill edge —
Section B puts a number on it.

## What the game simplifies away

Rails moons, circular and coplanar; no ring hazards at Saturn; the pocket's two big moons
only (Enceladus and Titan — the haven and the destination).

## The numerical experiment

### A — the same course, two sizes

```
run                     r2/r1  total dv (km/s)         TOF   window every
Earth -> Jupiter         5.20            14.44     2.73 yr       398.9 d
Enceladus -> Titan       5.13             6.10     3.68 d        36.0 h
```

Same ratio, same algebra, same course — the pocket's clock just runs ~270× faster. Lesson
15 called the outer system *long*; its moon system is the opposite: interplanetary-grade
dv on a weekend timescale. (And the dv is no toy — Enceladus rides Saturn at 12.6 km/s.
Deep wells spin fast bus routes.)

### B — one hop, flown honestly — and the sun's share, isolated

The lesson-15 recipe, verbatim: Lambert scans the window cheaply, shooting finishes the job.
Flown twice — once in a universe containing only Saturn and its two moons, once in the full
field with the sun and every planet:

```
Lambert scan of one synodic cycle (36.0 h, half-hour grid): best departure hour 20.5, total dv 6.05 km/s (TOF fixed at the Hohmann 3.68 d)
pocket only (Saturn + two moons):  converged True in 2 iterations, correction on Lambert 77.88 m/s, arrival 2.56 km/s vs Titan
full field (sun + everything):     converged True in 2 iterations, correction on Lambert 77.92 m/s, arrival 2.56 km/s vs Titan
-> the sun's share of the correction: ~0 m/s on this 3.7-day arc.
```

Read the two rows twice, because both halves matter. The ~78 m/s correction that *does*
exist is the **moons' own gravity** — Enceladus tugging the departure (the ship leaves from
3,000 km off her surface), Titan bending the last hours of the approach. Saturn-centric
two-body Lambert can't know about either mass; same structural lie as lesson 14's Section B,
and we know it's the moons *by construction*, because the pocket-only universe contains
nothing else. And the sun's share is **0.04 m/s**: the sun accelerates the ship, Titan, and
Saturn all together, and a common-mode tug doesn't bend Saturn-relative geometry — only the
tidal *difference* does, and 2% of the way to Saturn's Hill edge, there barely is one.
Compare lesson 15, where the same sun cost a six-year passage 150 m/s of Lambert lie. Pocket
systems are not just fast — they are **clean**. Two-body intuition works better here than
anywhere in the big system.

### C — the porkchop plate that fits in a weekend

Lesson 14's plate spanned 760 days of departures. This one spans 36 hours:

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

cheapest cell: depart hour 20, TOF 3.5 days, total dv 6.01 km/s
```

Look at the first and last rows: hour 0 and hour 36 are the *same row* — the synodic beat
completing before your eyes, one full window cycle inside a table you can read whole.
Lesson 15's navigator waits 378 days for this pattern to repeat; the Saturn league's traders
watch it repeat before the weekend is out. Miss the bus in a pocket system and the honest
answer is: there's another one tomorrow.

## Break it yourself

1. **Try the other pocket.** Jupiter's system ships three big moons in `sol.json`. Rerun
   Section A for Europa→Ganymede and Europa→Callisto — which pair is the better Earth→Mars
   stand-in, and how often does *that* window come?
2. **Leave the clean zone.** Section B's sun-share was ~0 at Titan's radius (2% of Saturn's
   Hill sphere). Re-aim the hop at an imaginary stop 10× farther out (say 1.2e10 m) and
   watch the sun's share grow — find the radius where it crosses 10 m/s. That contour is
   where "pocket" stops meaning anything.
3. **Race the Hohmann.** The plate's cheapest cell (6.01 km/s, hour 20, 3.5 days) sits a
   hair under Section A's Hohmann quote (6.10 km/s at 3.68 days) — the grid found a
   slightly off-tangent phasing the formula's fixed geometry can't express. Now buy real
   speed: fly the 2.0-day column's best cell and price the hurry. Lesson 4's Oberth
   arithmetic explains the bill you get.
