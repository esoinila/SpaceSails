# Lab 11 — The Electric Sandbox ⚡

**LABEL: this whole lesson is the game's fictional cosmology.** `PlasmaEnvironment` — the
Electric Universe layer — has no standard-physics counterpart: real solar wind does not hand a
ship free momentum, and gravity is not measured to couple to charge anywhere. Per
`docs/SaturdayPlan/GravityLab.md`'s framing rule, "in this house we compute both, and we label
which is which." Sections A and B compute the game's *actual, shipped* EU mechanic — the real
`PlasmaEnvironment` class, unchanged. Section C is explicitly a made-up extension layered on
top for this lesson only; nowhere else in this codebase does gravity depend on charge.

## Run it

```bash
dotnet run --project labs/11-the-electric-sandbox -c Release
```

## Section A — the solar halo: ambient charge vs. distance

`PlasmaEnvironment.AmbientCharge` = `min(1, (5e10/r)²)` outside any stream. Measured in
isolation (no streams — see the "measurement artifact" note below) at five orbit radii:

```
body        orbit radius (AU)   ambient charge
Mercury     0.3871              74.5 %
Venus       0.7233              21.4 %
Earth       1.0000              11.2 %
Mars        1.5237              4.8 %
Jupiter     5.2043              0.4 %
```

`docs/features/electric-sky.md` claims "roughly 75% ambient at Mercury, down to about 11% at
Earth" — the table above *is* that claim, actually computed against the live class rather than
taken on faith: 74.5% and 11.2%.

**A measurement artifact worth knowing about:** the first version of this probe measured
Mercury's ambient charge with the *full* environment (both real `sol-eu.json` streams active)
and got 100%, not 75% — because `sol-eu.json`'s streams run venus↔mercury and saturn↔jupiter,
and sampling "ambient charge at Mercury's orbit radius" at phase 0 sits almost exactly on the
venus↔mercury stream's own endpoint (distance ≈ 0 ≤ half-width ⇒ saturated). The fix was a
stream-free `PlasmaEnvironment` for this table, isolating the pure r⁻² halo the doc is actually
describing. Left in as a reminder that "just call the API" can silently measure the wrong
thing.

### Equilibration over time

A ship parked at Earth's orbit (ambient ≈ 11.2%), charge starting at 0, stepped with the real
`Simulator` + environment at dt = 60 s — `Simulator.Step`'s own exponential-blend formula
(`charge += (ambient − charge) × min(1, dt/τ)`, τ = `PlasmaEnvironment.EquilibrationTau` =
3600 s):

```
tau multiples   sim time (s)  charge        fraction of ambient
0               0             0.00 %        0.0 %
1               3600          7.10 %        63.5 %
2               7200          9.68 %        86.7 %
3               10800         10.63 %       95.1 %
5               18000         11.10 %       99.4 %
10              36000         11.17 %       100.0 %
```

Textbook exponential approach, exactly as advertised — 63% of the way there after one τ, 95%
after three, indistinguishable from fully equilibrated after ten.

## Section B — riding the Saturn→Jupiter stream vs. a standard ballistic transfer

*Standard half: the Hohmann transfer time below is real orbital mechanics (Curtis ch. 6).
Fictional half: the stream ride uses `PlasmaEnvironment`, the game's EU layer.*

```
Standard Hohmann Saturn (1.43353e12 m) -> Jupiter (7.7857e11 m): retrograde burn -1549.1 m/s,
transfer time = 317216812 s (3671.5 days, 10.05 years).

'Arrival' = inside Jupiter's own Hill sphere (5.315E+010 m — real orbital mechanics, not an
arbitrary round number: the radius where Jupiter's own gravity starts to dominate over the Sun's).

case                                  outcome       sim time (days)   closest approach to Jupiter (m)
charged, riding the stream            ARRIVED       122.16            5.314E+010
uncharged, same departure, no stream  no arrival    400.00            1.084E+012
```

Both rows share the exact same departure: just clear of Saturn's surface, co-moving with
Saturn's own orbital velocity (like a cargo pod that just undocked) — the *only* difference is
charge=1 + the stream force in one row vs. charge=0/no environment in the other. Riding the
stream: **122.2 days**, comfortably inside the "under 150 days" this repo's Electric Sky flavor
promises, against the standard ballistic Hohmann's **3,671 days** (10.05 years) for the same
trip done the textbook way with an actual transfer burn. The uncharged ship, given the same
non-transfer departure, never gets meaningfully closer to Jupiter than 1.08e12 m in 400 days —
riding the river is a genuinely different, and genuinely faster, way to make the trip, not a
substitute for planning a real transfer (a properly-burned Hohmann still beats "drift near
Saturn forever," it just costs 30x longer than the stream).

## Section C — SPECULATIVE: what if effective μ depended on the electrical environment?

**LABEL: pure speculative fun.** `μ_eff(charge_environment) = μ0 × (1 + k·q(r))` for a made-up
`k` — nowhere else in this codebase does gravity depend on charge; real Newtonian gravity
(labs 1–3, 9, and everywhere else in this game) has μ independent of q.

```
q(Mercury) = 74.5 %, q(Earth) = 11.2 % (from Section A).
Real Mercury year: 87.971 days. Real Earth->Mars Hohmann: 258.87 days.

k         mu_eff/mu0 @ Mercury  Mercury year (days)   mu_eff/mu0 @ Earth  Earth->Mars transfer (days)
0.05      1.0373                86.376                1.0056              258.15
0.20      1.1491                82.066                1.0223              256.02
0.50      1.3727                75.084                1.0559              251.93
```

Mercury sits in a hot neighborhood (q ≈ 75%), so even a modest `k` measurably shortens its
year (87.97 → 75.08 days at k=0.5, a 15% cut); Earth's cold neighborhood (q ≈ 11%) barely moves
the Earth→Mars transfer time at the same `k` (258.87 → 251.93 days, a 2.7% cut). If gravity
really did couple to the electrical environment this way, the measurable signature would be
**inner planets running fast clocks relative to outer ones** — a clean, falsifiable "what you
would actually see" for a purely speculative toy, computed the same honest way as everything
else in this lab.

## Break it yourself

1. Section B's ride starts already at charge=1 (already hot). Start at charge=0 instead (a
   cold, sneaking departure) and see how many hours of equilibration (τ=3600 s) it costs before
   the stream force is worth anything.
2. Section C picks `k` in {0.05, 0.20, 0.50} arbitrarily — there is no in-repo constant to check
   against, because this mechanic doesn't exist outside this lesson. Find the `k` where
   Mercury's speculative year drops under half its real value.
3. Move Section B's departure point off the stream centerline by more than the ribbon's
   half-width (3e10 m) and watch the charge stop equilibrating to 1.0 — the ship goes cold and
   the "free momentum" disappears entirely, mid-flight.

## See also

- `src/SpaceSails.Core/PlasmaEnvironment.cs` — `AmbientCharge`, `Acceleration`,
  `SolarHaloRadius`, `StreamAcceleration`, `EquilibrationTau`. All real, all unchanged by this
  lesson.
- `docs/features/electric-sky.md` — the player-facing claims this lab verifies numerically.
- `scenarios/sol-eu.json` — the real streams (`saturn↔jupiter`, `venus↔mercury`) this lesson's
  Section B and the halo-contamination note both use.
- Curtis ch. 6 — the standard Hohmann-transfer formula underlying Section B's slow half (lesson
  5 in the ladder, "Transfers without formulas," will give it its own full treatment).
