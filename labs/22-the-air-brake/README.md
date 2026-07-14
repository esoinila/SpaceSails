# Lab 22 — The air brake

Two of the three flight assists in [`docs/TuesdayPlan/FlightAssists.md`](../../docs/TuesdayPlan/FlightAssists.md)
need no new physics — a slingshot is lesson 19's crank with a button on it. The third does. To
brake against a planet's air (the gas-giant dip Eli's crew rode in *Stargate Universe*) or bounce
off it (the atmosphere-skip corridor Apollo return crews had to respect), the simulator has to know
that a body can have an **atmosphere**. This lesson adds exactly one honest ingredient — an
exponential density shell with a single ballistic-coefficient knob — and then computes the three
things a pilot actually needs:

- **the corridor**: how much speed a skim sheds versus how deep you dip, at Jupiter;
- **the skip**: how narrow the Apollo return corridor really is, in kilometres of periapsis altitude;
- **the fuel-out capture**: whether you can turn a hyperbolic arrival into a bound orbit on drag
  alone with the tank dry, flown pass by pass.

The same Core drag the game ships (`Simulator.DragAcceleration`, `Simulator.RunAdaptiveWithDrag`)
draws every number here — the picture this lab makes *is* the corridor gauge the game will show.

Run it:

```bash
dotnet run --project labs/22-the-air-brake -c Release
dotnet run --project labs/22-the-air-brake -c Release -- --viz          # + the browser pop-up
```

## The standard textbook take

Curtis, *Orbital Mechanics for Engineering Students*, treats atmospheric entry and aerobraking with
the same exponential (isothermal) atmosphere this lab uses — density `ρ(h) = ρ₀·exp(−h/H)` — and the
drag equation `D = ½·ρ·v²·C_d·A`, i.e. deceleration `a = ½·ρ·v²/BC` where the **ballistic
coefficient** `BC = m/(C_d·A)` bundles the whole ship into one number (kg/m²). The textbook then
notes what makes entry *hard*: the corridor is narrow because drag depends exponentially on depth,
so a small error in periapsis altitude is a large error in heat load and in speed shed. It sets up
the equations and — for anything past a single straight-in entry — hands you to a numerical
integrator. So: numerically, through the game's own.

**What we model, stated honestly (Section A):**

- `density(h) = refDensity · exp(−h / scaleHeight)`, and **exactly zero** at and above a hard shell
  top (a bounded region the integrator only ever touches on purpose);
- `drag accel = −0.5 · ρ · |v_rel| · v_rel / BC`, with **BC = 120 kg/m²** — the game's one tunable
  knob, a compact capsule-like value;
- `v_rel` = ship velocity − the body's rail velocity (the air translates with the planet).

**What we ignore on purpose:** the planet's spin, aerodynamic lift, and *all* heating physics.
"Too deep" is charged off the **peak deceleration** — the sail-holing damage the gun already
inflicts, now self-inflicted — not a thermal model. This is a game-tuned model, not a re-entry
simulator, and it says so.

### The atmosphere shells (`scenarios/sol.json`)

This is where the shell numbers are justified. Every one is deliberately **thin and low** so no
existing gameplay trajectory clips it by accident — orbit insertion parks at ~0.5 Hill radii, tens
of thousands of km out, while the deepest shell top here is under **1.2 %** of a body radius:

| body    | refDensity (kg/m³) | scale height (km) | shell top (km) | top / body radius | rationale |
|---------|--------------------|-------------------|----------------|-------------------|-----------|
| Jupiter | 4×10⁻⁶             | 30                | 400            | 0.0057            | Thin, low shell tuned so the braking corridor spans ~10 m/s to km/s over ~200 km of depth, with the damage line only near the cloud tops. |
| Earth   | 1.2                | 8                 | 140            | 0.0220            | Near-real: sea-level density, 8 km scale height, 140 km entry interface — which is exactly what makes the Apollo skip corridor honestly narrow. |
| Venus   | 65                 | 16                | 150            | 0.0248            | Thick, hot lower atmosphere (game-flavored) — the future "aerocapture is cheap here, but deep" body. |
| Saturn  | 5×10⁻⁶             | 120               | 700            | 0.0120            | A lighter, puffier gas giant than Jupiter — a gentler corridor at a larger scale height. |
| Titan   | 5.3                | 40                | 300            | 0.1165            | A thick nitrogen shell on a small moon (capped at 300 km so it stays well under 0.15 R even though the real one reaches higher). |

Values are physically flavored but **game-tuned**: real Jupiter cloud-top density is ~0.16 kg/m³,
ours is far thinner so a single pass is survivable and the corridor is wide enough to aim at. The
numbers below are Jupiter's and Earth's; the other three are there for the game to use later.

## The corridor at Jupiter (Section B)

A fixed hyperbolic arrival (`v_inf = 5.5 km/s`), sweeping periapsis depth. The damage line is a peak
deceleration of **3 g**:

```
  peri alt km  min alt km   dv shed m/s    peak g                       outcome
            5         0.4          3803      5.76   TOO DEEP: hull holed + captured
           20        13.4          3301      3.76   TOO DEEP: hull holed + captured
           40        33.9          1664      1.95   captured, apoapsis 20.6 R_J
           60        54.2           845      1.00   captured, apoapsis 50.3 R_J
           80        74.4           431      0.52   captured, apoapsis 170.3 R_J
          100        94.6           220      0.26   exits, v_inf 2.05 km/s
          130       124.7            81      0.10   exits, v_inf 4.58 km/s
          170       164.9            21      0.03   exits, v_inf 5.31 km/s
          220       215.2             4      0.00   exits, v_inf 5.50 km/s
          300       295.6             0      0.00   exits, v_inf 5.53 km/s
```

Three zones fall straight out of the exponential: **too shallow** (above ~220 km, under 50 m/s shed
— nothing happens), **the corridor** (~40–130 km, useful braking under the 3 g line), and **too
deep** (near the cloud tops, the sail holes — and the pass captures so hard it's academic). One
scale height deeper (30 km) multiplies the density, and the Δv, by *e*: that is why the corridor is
a band, not a cliff, and why aiming it is the whole skill.

## The skip at Earth (Section C)

Apollo-return-grade arrival (`v_inf = 1.5 km/s`, **11.18 km/s** at the 122 km interface), sweeping
entry depth. The burn-up line is **6.5 g** (Apollo crews held ~6–7):

```
  peri alt km  min alt km   dv shed m/s    peak g                   outcome
          110       108.8            56      0.08   skips out, v_inf 1.02 km/s
          100        98.6           198      0.28   CAPTURED, apo 373630 km
           95        93.5           374      0.52   CAPTURED, apo 127516 km
           90        88.4           714      0.96   CAPTURED, apo 53900 km
           85        83.1          1395      1.75   CAPTURED, apo 23021 km
           80        77.5          3019      3.12   CAPTURED, apo 7725 km
           75         0.0         14351      6.28   BURN-UP (augers in)
           70         0.0         14361      8.13   BURN-UP (augers in)
           65         0.0         14353     10.88   BURN-UP (augers in)
           60         0.0         14345     13.30   BURN-UP (augers in)
```

The honest punchline: the band that **captures without burning up** spans a periapsis altitude of
**80–100 km — about 20 km wide**. Come in shallower and you skip back out to space; come in deeper
and, with Earth's real 8 km scale height, the drag pulls you down into the surface within one pass
(the "augers in" rows: min altitude 0 — the capsule reached the ground). Twenty kilometres of margin
is the whole corridor the return crews had to hit blind, on a slide-rule re-entry angle. That
narrowness isn't game exaggeration — it's what a realistic thin atmosphere *does*.

## The fuel-out capture — the *Stargate Universe* move (Section D)

Arrive fast and hyperbolic with the tank dry (`v_inf = 6.0 km/s`, `E > 0`), aim once at a 72 km
periapsis, and **spend no propellant after the aim**. Each skim drops the apoapsis; the ledger:

```
  pass  peri alt km   dv shed m/s    peak g     energy J/kg        apoapsis
     1         66.4           564      0.67       -15699632       114.4 R_J
     2         56.0           797      0.93       -62848106        27.8 R_J
     3         44.7          1164      1.31      -130737557        12.9 R_J
     4         32.9          1732      1.85      -229410476         6.9 R_J
```

Pass 1 alone turns the hyperbolic arrival (`E > 0`) bound (`E < 0`): the atmosphere captured the
ship on drag alone. Each later pass tightens the orbit — 114 → 28 → 13 → 7 Jupiter radii — for zero
fuel. Notice the periapsis **creeping down** every pass (66 → 33 km): with no propellant to raise it,
each dip digs deeper, so free capture is a race you eventually lose to the damage line. That is the
real cost of the trick, and the game's version of it: brake on air when the tank is dry, but you
can't hold the skim forever.

> **A note on how Section D is flown.** The pass-by-pass drag is integrated at full fine-step
> resolution. The long *coast* between passes is **not** integrated — a semi-implicit Euler step
> bleeds energy through an `e ≈ 0.98` ellipse (lesson 02's drift, live and vicious here). Instead,
> because the single-body field is rotationally symmetric, a drag pass depends only on the entry
> radius and the radial/tangential speeds, never on angular position — so the next skim is
> reconstructed with the exit orbit's energy and angular momentum *exactly*. The ledger stays
> honest; the coast that would corrupt it is skipped by symmetry, not by approximation.

## The DragReport API (what the game's gauge reads)

`Simulator.RunAdaptiveWithDrag(...)` flies the exact same trajectory as `RunAdaptive` and returns a
`DragReport` alongside the final state:

```csharp
public readonly record struct DragReport(
    double PeakDecelMetersPerSecondSquared,   // the damage-line input
    double DeltaVShedMetersPerSecond,         // "pulses saved" for the gauge
    double PeakDynamicPressurePascal,
    double MinAltitudeMeters,                 // the depth read-out
    double ExitSpeedMetersPerSecond,
    string? DominantBodyId)                   // which atmosphere braked you
{
    public double PeakDecelG => PeakDecelMetersPerSecondSquared / 9.80665;
}
```

PR-I's corridor gauge consumes exactly this: `PeakDecelG` against the damage line, `DeltaVShedMetersPerSecond`
as pulses saved, `MinAltitudeMeters` as the depth read-out.

## Break it yourself

1. **Double the BC.** Only the ratio `density/BC` appears in the drag law, so doubling BC is
   *identical* to halving `refDensity`. The 80 km Jupiter pass sheds **431 m/s** at stock BC and
   **215 m/s** at 2× BC — the game's BC knob and a body's `refDensity` are the same dial.
2. **The corridor at double speed.** The same 60 km dip sheds **845 m/s** at 5.5 km/s (which
   *captures*, apoapsis 50 R_J) and **841 m/s** at 11.0 km/s (which *exits* at 4.4 km/s). Nearly the
   same Δv, opposite result: Jupiter's gravity, not `v_inf`, sets the periapsis speed, so a fast
   arrival keeps its excess energy and blows straight back out — it must dip *deeper*, toward the
   damage line, to shed the extra. That is exactly why hot aerocapture is dangerous.
3. **Skim Mercury.** No atmosphere field, so the drag term is skipped whole: Δv shed **0.0 m/s**,
   peak **0.0 m/s²**, dominant body *none*. An airless world is a vacuum flyby, byte-for-byte the
   trajectory it always flew (the regression gate proves it). Aerobraking needs air; Mercury has
   none.

## Seeing it: `--viz`

`-- --viz` writes `labviz/lab22-the-air-brake.html`: Jupiter at the origin, the Section-B corridor
family as a faded fan (each depth's flyby is the pre-drag hyperbola in, stitched to the post-drag
orbit out — the kink at periapsis *is* the skim), and the Section-D fuel-out capture spiral as the
animated ghost, tightening from a 114 R_J first orbit down through the successive skims, with a
marker at each pass's periapsis. Scrub the timeline to watch the ghost spiral in. Without `--viz`,
stdout is byte-identical.

---

*Every number in this README came from running the probe. If you change the code, rerun and
re-paste — never hand-edit a table.*
