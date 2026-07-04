# Lab 05 — Transfers without formulas

*Standard physics. Curtis, "Orbital Mechanics for Engineering Students," ch. 6 (Hohmann
transfers).*

## The idea

Curtis solves Earth-Mars in three closed-form numbers — two burns and a flight time — from
vis-viva algebra alone, because he assumes coplanar circular orbits and a free choice of
departure day. `scenarios/sol.json` is, quite literally, that idealization: real radii and
periods, perfectly circular, perfectly coplanar. So the formula should be dead-on here. What
does `RoutePlanner` — a small grid search over the game's own `Simulator` — buy on top of a
formula that's already exact for this solar system? This lesson computes both on the same
Earth-Mars pair, then finds a case (Earth-Venus, on a departure day nobody gets to choose) where
the search earns its keep, and a break-it where shrinking the search grid visibly hurts.

## Run it

```bash
dotnet run --project labs/05-transfers-without-formulas -c Release
```

## Section A — the Hohmann analytic transfer (Curtis ch. 6)

```
r1 (Earth) = 1.0000 AU, r2 (Mars) = 1.5237 AU
v_circ(Earth) = 29784.480 m/s, v_circ(Mars) = 24129.346 m/s
transfer ellipse speed at departure = 32729.081 m/s, at arrival = 21480.523 m/s
dv1 (departure burn)  = 2944.601 m/s
dv2 (arrival burn)    = 2648.823 m/s
total analytic dv     = 5593.423 m/s
transfer time         = 22366268 s (258.87 days)
```

Three numbers from algebra, no integration, no search — and exact for the two-body, coplanar,
circular idealization sol.json actually is. What this can't know: which day you're allowed to
leave, that real thrust only comes in ±10% pulses (lesson 4), or that NPC captains fly with
different personalities.

## Section B — RoutePlanner's grid search, same pair, real departure scan

Scanning `RoutePlanner.PlanRoute` (Economical personality) every 40 days across one full
Earth-Mars synodic period (779.9 days):

```
departure (day)   pulses   miss (km)       flight (days)
0                 10       53484128.0      31.0
40                4        898187.9        63.0
80                6        48306303.2      54.0
120               4        74401522.4      0.0
160               4        103166858.2     0.0
200               4        149882109.3     0.0
240               4        199455253.8     0.0
280               4        246604737.6     0.0
320               4        288780323.1     0.0
360               4        324343089.1     0.0
400               4        352111291.0     0.0
440               4        371230586.4     0.0
480               4        380956265.4     30.0
520               4        380576112.9     38.0
560               10       364627160.7     10.0
600               10       332916968.7     17.0
640               10       287145823.2     22.0
680               10       229404275.5     27.0
720               10       162348373.5     30.0
760               10       89880603.2      31.0
```

**A surprise worth explaining rather than hiding:** the `flight = 0.0`, hundred-million-km-miss
rows are real, not a bug. On those departure days, none of the Economical grid's four burn sizes
(4/6/8/10 pulses) ever gets the ship closer to Mars than its own starting distance within the
search horizon — the sampled distance climbs monotonically from the very first point, so the
search's own "closest point found so far" bookkeeping reports the departure instant itself as the
best it ever saw. Even for a formula-friendly pair like Earth-Mars, a badly timed departure with
a fixed, modest burn budget can simply never get there.

The scan's actual best is day 40 (4 pulses, 898,187.9 km miss, 63.0-day flight). Comparing that
plan against Section A's formula, side by side:

```
                        dv-equivalent (m/s)   flight time (days)  arrival miss (km)
Hohmann analytic        5593.423              258.87              (by definition) 0
RoutePlanner search     26460.050             63.0                898187.9
```

The flight time and miss numbers aren't remotely close — the found route is almost 4x faster
(63 vs. 259 days) and about 4.7x more expensive in pulse-equivalent Δv (26,460 vs. 5,593 m/s).
**That gap is not the search failing to find the formula's answer — it's not looking for it.**
`RoutePersonality.Economical` is a *shape* (one modest early burn, then ballistic — see
`RoutePlanner.cs`), not a fuel-minimization target; its "cheapest" candidate among 4/6/8/10
pulses still isn't anywhere near Curtis's continuous-Δv optimum, because the game doesn't have a
continuous-Δv control to reach it with (lesson 4's whole point). The formula and the search are
answering different questions: "what is the theoretical minimum" vs. "what can an NPC actually
fly with whole ±10% pulses, on this particular day."

## Section C — where search shines: a departure day you don't get to choose

```
Hohmann analytic (idealized, any departure day): dv1 = 2495.403 m/s, dv2 = 2706.584 m/s, total = 5201.988 m/s, transfer time = 146.08 days
```

That number is the same regardless of which day you leave — the formula has no departure-day
axis at all. A real captain is handed one day (a cargo contract, a duty roster) and has to fly
from wherever Venus actually is *that* day, not from Curtis's idealized alignment.

```
Constrained to day 0: RoutePlanner still finds a route — 10 pulses, dv-equivalent 65062.576 m/s, flight 42.0 days, miss 7577354.0 km
Search allowed to pick its own day (scanning a full 584-day synodic cycle): best is day 100 (10 pulses, miss 338792.2 km, flight 385.0 days)
```

Forced to leave on day 0 (a bad window), the search still returns a flyable plan — expensive
(65,063 m/s pulse-equivalent) and not especially accurate (7.58 million km miss), but a real plan
a real captain can fly *today*. Given the freedom to pick its own day instead, the same search
finds a window 22x tighter on miss distance (338,792 km) for the same pulse count. The formula
can't do either of these: it has no way to answer "what's my best move given today's date," and
no way to tell you how much a bad day costs you versus a good one. That flexibility — not
raw Δv accuracy — is what the search buys that a closed-form Hohmann transfer structurally
cannot.

## Break it — shrink the search grid

```
Same Earth -> Mars departure (day 0) with progressively smaller candidate grids:
grid                        pulses picked   miss (km)     flight (days)
4,6,8,10                    10              53484128.0    31.0
4                           4               71567082.9    64.0
6                           6               66994951.1    49.0
8                           8               60324428.1    38.0
10                          10              53484128.0    31.0
12,16,20                    20              29398704.0    11.0
```

On this (deliberately bad, per Section B) departure day, the real Economical grid `[4,6,8,10]`
picks pulse count 10 — already the best of its own four options, so the full grid and the
single-candidate `[10]` grid tie exactly. But `RoutePlanner` doesn't know in advance which
candidate will win: shrink the grid down to `[4]` alone and the miss grows to 71,567,082.9 km,
1.3x worse than trusting all four candidates — worse not because 4 pulses is a bad number in
general, but because the search's whole value is trying several and keeping the best, and a
single-candidate "grid" has removed that safety net. Note also that `RoutePersonality.Fast`'s
real grid, `[12,16,20]`, does even better here (29,398,704 km) simply by trying bigger burns —
which is exactly why the game gives NPC captains different personalities with different grids
in the first place, rather than one fixed guess for everyone.

## Break it yourself

1. **Already above:** the Mars departure scan used a 40-day stride. Shrink it to 10 days around
   day 40 (the scan's best) — does a finer scan find an even better window nearby, or was day 40
   already close to a local optimum?
2. **Already above:** the grid break-it used `RoutePersonality.Fast`'s `[12,16,20]` as the "bigger
   grid" comparison. Try `RoutePersonality.Evasive` on the same day-0 Mars transfer (it splits the
   same pulse budget into two burns at a seeded time) — does splitting the burn change the miss
   distance at all, given `ManeuverPlan.ScaleFactorInWindow` only cares about total pulses fired
   in a window, not how they're split?
3. **On your own:** Section A's Hohmann formula assumed a free choice of departure day. Curtis
   ch. 6 also gives a phase-angle condition for when Mars must actually be positioned relative to
   Earth for a *real* Hohmann departure to work. Compute that phase angle from `r1`/`r2`, then use
   `CircularOrbitEphemeris.Position` to find which day near this lesson's scan actually satisfies
   it — does it land near day 40?

## See also

- `src/SpaceSails.Core/RoutePlanner.cs` — the grid search itself: candidate pulse counts per
  personality, the coarse `dt = 1 day` search step, and why (WASM interpreter cost).
- `src/SpaceSails.Core/ManeuverPlan.cs` — why Δv-equivalent isn't continuous (lesson 4 goes deep
  on this).
- Lesson 4 (`labs/04-the-ten-percent-pulse`) — the same transfer-burn arithmetic (a single whole
  pulse count turning a circular departure into a new ellipse) used here at solar-system scale.
- Lesson 6 (`labs/06-closest-approach-found-honestly`) — the same kind of coarse-vs-refined
  scanning tension this lesson's break-it exercises, applied to the planner's proximity warning
  instead of its arrival search.
