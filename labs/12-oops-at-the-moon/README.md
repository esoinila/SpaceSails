# Lab 12 — Oops at the Moon 🌙

**LABEL: the STORY is fiction** — miners drilling into Luna and shorting "the capacitor" is not
canon; nothing in `scenarios/sol.json` records any such incident. **The numerics are entirely
real.** This is the one lesson in the whole ladder where a body leaves its rail: Luna is
un-railed at t0 and integrated forward as a genuine free body through `SpaceSails.Core`'s
actual `Simulator`, under real Newtonian gravity from Sun + Earth. Every periapsis, escape
speed, and day-count below is standard two-body/perturbed orbital mechanics (Curtis ch. 2-3)
aimed at a scenario this repo made up.

**The mechanism, chosen on purpose:** the game's *only* propulsion, everywhere else in this
codebase, is a velocity-scaling pulse (`ManeuverAction`, ±10% etc. — `ManeuverPlan.cs`). This
lesson's "capacitor" doesn't touch Earth's mass; it kicks Luna's Earth-relative velocity the
same way the ship's own engine kicks its velocity — an accident inflicting the game's own
mechanic on a moon instead of a ship. Scaling Luna's tangential orbital speed *is* a change in
specific orbital energy, which is exactly what "how tightly bound to Earth" means in the
two-body sense — the fictional "capacitor" framing and the real "binding changed" physics are
the same number.

## Run it

```bash
dotnet run --project labs/12-oops-at-the-moon -c Release
```

## Setup: Luna's real rail state

```
Earth-Luna distance r0 = 3.844000E+008 m (384400 km). Luna's Earth-relative orbital speed:
  numeric (finite-difference off the rail): 1023.154 m/s
  analytic circular speed sqrt(mu_earth/r0): 1018.303 m/s
  agreement: 4.763E-003 relative error (the rail really is circular).
```

That 0.48% gap is not finite-difference truncation error (a central difference at h=1 s against
a ~27-day period is accurate to ~13 significant figures) — it is `scenarios/sol.json` itself:
Luna's `orbitPeriodS` and its `mu`/`orbitRadiusM` pair are not perfectly Kepler-consistent with
each other (real-world source data rounded from slightly different references). Small,
harmless for the rail (which only ever uses period, never derives it from mu), but worth
surfacing rather than quietly averaging away — it is the reason the "mild" case's very first
logged perigee below lands almost exactly at t=0 rather than one full period later.

## Three severities, one mechanism

```
--- Mild: miners careless, +15% speedup (prograde) (velocity x 1.150) ---
  analytic: bound, a=578157 km, e=0.3351, periapsis=384400 km, apoapsis=771913 km, period=50.64 days (half-period to first periapsis = 25.32 days)
  numeric (400-day free-body integration, dt=300s, Sun+Earth gravity):
    perigee timeline (day, distance to Earth in km): (0.0d, 384400km), (51.6d, 229307km), (114.9d, 140777km), (177.2d, 251258km), (227.1d, 389766km), (275.9d, 268116km)
    never crosses the 6471 km grazing threshold in 400 days. Final distance to Earth: 253478 km.

--- Severe: miners reckless, -85% slowdown (retrograde) (velocity x 0.150) ---
  analytic: bound, a=194408 km, e=0.9773, periapsis=4416 km, apoapsis=384400 km, period=9.87 days (half-period to first periapsis = 4.94 days)
  numeric (10-day free-body integration, dt=60s, Sun+Earth gravity):
    perigee timeline (day, distance to Earth in km): (4.9d, 5173km)
    ATMOSPHERE-GRAZING at day 4.92 (distance <= 6471 km).

--- Panic: miners flee, +50% speedup (prograde, past escape) (velocity x 1.500) ---
  analytic: UNBOUND relative to Earth alone. v_infinity = 530.6 m/s (departs Earth's local field).
  numeric (120-day free-body integration, dt=300s, Sun+Earth gravity):
    no perigee passage detected in this horizon (still receding at the end -- consistent with departing).
    never crosses the 6471 km grazing threshold in 120 days. Final distance to Earth: 18851212 km.
```

A single signed knob — the velocity kick fraction — produces all three outcomes the prompt
asked for, and the *direction* is the whole story: a **prograde** speedup makes r0 the new
orbit's *periapsis* (Luna is now moving too fast to stay circular, so it swings outward — the
close point stays where it started), while a **retrograde** slowdown makes r0 the new orbit's
*apoapsis* (Luna is now moving too slow, so it falls inward — the close point drops below where
it started). Only the retrograde direction can ever threaten the atmosphere; the prograde
direction only ever widens the orbit or, past escape velocity, ends it.

**The genuine surprise, not predicted going in:** the "mild, stays bound" case is not
*stationary*. Pure two-body Kepler mechanics says the periapsis of a closed ellipse never
changes, orbit after orbit — but Luna's new apoapsis (771,913 km) is a large enough fraction of
Earth's Hill sphere (~1.5 million km) that the Sun's own gravity meaningfully tugs on the
orbit's shape between passes. The perigee timeline visibly swings — 384,400 → 229,307 →
140,777 → 251,258 → 389,766 → 268,116 km — dropping by nearly 64% at its lowest before swinging
back out past the *original* radius. It never approaches the atmosphere in 400 days, and it
does not runaway-decay: it **oscillates**, a genuine (if here fast-forwarded) third-body
perturbation effect of exactly the kind real lunar theory calls evection — not integration
error, not a fictional decay mechanic, just the Sun doing what the Sun does to a wide enough
orbit.

The severe case is unambiguous: an 85% slowdown puts periapsis at 4,416 km from Earth's
*center* — inside Earth's own 6,371 km body radius, so this isn't really "grazing the
atmosphere," it's a lunar impact, and the numeric integration confirms it lands within half a
day of the analytic half-period prediction (4.92 vs. 4.94 days).

## Break-it — finding the X that ends the Moon in 30 days

```
slowdown X    periapsis (km)    half-period (days)  ends within 30 days?
5 %           321645            12.08               no
10 %          265877            10.68               no
20 %          183446            8.71                no
30 %          126322            7.43                no
40 %          85365             6.56                no
50 %          55514             5.94                no
60 %          33773             5.51                no
70 %          18294             5.20                no
80 %          7921              5.00                no
85 %          4416              4.94                YES
90 %          1950              4.89                YES
95 %          486               4.86                YES
```

The real surprise here is how *much* slowdown it takes: even removing 80% of Luna's orbital
speed only drops periapsis to 7,921 km — still 1,450 km above Earth's surface. The crossover
sits between 80% and 85%. Luna orbits at 384,400 km; Earth's radius is 6,371 km — roughly 60
Earth-radii out — so "aim it at Earth" from a near-circular orbit that far away costs almost
all of the orbital speed, not a modest fraction. The half-period column barely moves across the
whole scan (12.1 → 4.9 days) because a deeply plunging orbit is dominated by how *low* the
periapsis is, not how far the apoapsis still reaches.

## Flavor: the real anecdote, then the riff

**Factual:** Apollo 12's spent ascent stage, deliberately crashed into the Moon in November
1969, made the seismometer network ring for nearly an hour — real evidence the Moon has
essentially no internal damping compared to Earth's wet, faulted crust.

*[RIFF, labeled: if that is how long a spent rocket stage rings the Moon like a bell, what does
a mining rig's shorted capacitor sound like — to instruments that, in this story, are still
listening.]*

## The playable scenario

`scenarios/oops.json` is a Sol variant whose description tells this story: Luna's orbital
parameters are set to the mildest computed outcome above (the +15% "mild" case never threatens
the atmosphere and is the one a player could actually fly through), and a haven is renamed
"Miners' Folly" in the aftermath. Loads and parses via `ScenarioLoader.Parse` (see
`tests/SpaceSails.Core.Tests/OopsScenarioTests.cs`); playable at `?scenario=oops`.

## Break it yourself

1. **Already above:** the 30-day break-it scan is run for you. Bisect between 80% and 85% to
   pin down the exact slowdown fraction where periapsis crosses Earth's bare 6,371 km surface
   (rather than the +100 km grazing margin used above).
2. **On your own:** the "mild" case's perigee oscillates instead of decaying. Push the horizon
   past 400 days — does the oscillation stay bounded indefinitely, or does it eventually drift
   toward the atmosphere (or away, toward escape) once you integrate long enough?
3. **On your own:** this lesson only kicks *speed*, never direction. Try a kick perpendicular to
   Luna's orbital velocity (a plane change instead of a speed change) and see whether it takes
   more or less "carelessness" to threaten the atmosphere than the pure retrograde case above.

## See also

- `src/SpaceSails.Core/Simulator.cs` — `Step`/`GravitationalAcceleration`; the un-railed
  integration here uses exactly this, no separate "moon physics."
- `src/SpaceSails.Core/ManeuverPlan.cs` — the ±10%/etc. velocity-pulse mechanic this lesson's
  "capacitor" reuses on Luna instead of a ship.
- `scenarios/sol.json` — Luna's real (unperturbed) orbital elements this lesson starts from.
- `scenarios/oops.json` — the playable aftermath scenario.
