# Lesson 19 — The Grand Tour (Voyager)

*Leave Earth with too little energy for Saturn, swing behind Jupiter, and let the giant's own orbital motion hurl you outward — the lever, not the taxi. Voyager 2 did it; so does this lesson. Lesson 18 stole nothing from Mars (sun-steered timetabling); lesson 19 commits actual gravity theft.*

```bash
dotnet run --project labs/19-the-grand-tour -c Release
```

(The window scan and b-plane sweep fly real trajectories through the integrator — give the probe a minute or two.)

## Seeing it

Add `--viz` to get the same numbers *plus* a picture of the trajectories the probe just flew:

```bash
dotnet run --project labs/19-the-grand-tour -c Release -- --viz
```

This writes a single self-contained `labviz/lab19-the-grand-tour.html` (no external requests) and opens it in your browser — drawn in the game's own visual language: the sun, the eight planet rails, drag to pan, wheel to zoom, and a scrubber that walks time forward. What you see:

- The **crank fan** from Section B — one faded blue-gray arc per surviving aim offset, spraying past Jupiter. Toggle the `sweep` group in the legend to hide them.
- The **flown itinerary** from Section C in orange — launch, swing behind Jupiter, coast to Saturn — with a ghost ship the scrubber animates along the arc.
- Markers for every burn (launch, TCM-1, TCM-2, with Δv), the Jupiter flyby (pass distance in R_J), and the Saturn closest pass.

Add `--viz-no-open` to write the file without launching a browser. The stdout tables are untouched by `--viz` — the printed numbers stay sacred.

## Why this lesson exists

Every prior lesson either used formulas that break near planets or flew single legs. Gravity assist is the trick that makes the outer solar system reachable on 1970s hardware: you arrive with v_inf relative to Jupiter, gravity rotates that vector inside Jupiter's moving frame (13 km/s heliocentric), and you leave with a different heliocentric speed — for free, as far as the ship is concerned. Curtis ch. 8 covers patched-conic gravity assists; this lesson flies them through the real deterministic integrator the game uses, prices the TCMs honestly, and shows the ledger hole the rails create.

## The standard-textbook take

Patched-conic gravity assists (Curtis ch. 8): treat the flyby as an instantaneous rotation of the hyperbolic excess velocity vector inside the planet's sphere of influence. Incoming and outgoing |v_inf| are equal; the turn angle depends on impact parameter b, planet μ, and |v_inf|. The heliocentric velocity change is the vector difference after adding back the planet's velocity. The 4-planet alignment that enabled Voyager's Grand Tour recurs roughly every 175 years.

## What the game simplifies away

Circular coplanar rails (no inclination, no eccentricity), instantaneous exact burns, and a planet that cannot recoil. The integrator is the real one; patched-conic is used only for comparison in the probe. Lesson 9 showed what a true n-body ledger costs.

## The numerical experiment

### A — the impossible ticket

Direct Hohmann to the ice giants is measured in decades.

```
direct Earth->Saturn (lesson 15): departure 10298 m/s, 6.09 yr
direct Earth->Neptune (L15 tyranny): departure 11653 m/s, 30.58 yr
Earth->Jupiter leg only:          departure 8794 m/s, 2.73 yr
Voyager's wager: launch 1504 m/s CHEAPER than Saturn direct,
and the real prize is Neptune-range reach for ~Jupiter fare.
```

### B — one flyby, measured honestly

Lambert + shooting method to Jupiter, then controlled b-plane offset sweep on the actual approach arc. Heliocentric speed before/after, gain, and two-body bound:

```
approach: v_inf vs Jupiter = 7.64 km/s, heliocentric 5.60 km/s,
          pre-flyby orbit reaches 5.27 AU (Saturn sits at 9.58 AU)
the two-body bound: the flyby can only ROTATE v_inf, so the outgoing heliocentric speed
must land between |v_J - v_inf| = 5.42 and |v_J + v_inf| = 20.71 km/s. Watch it hold:

   aim offset  pass dist (km)  (R_J)  helio v after (km/s)  gain (km/s)  now reaches (AU)
     -3000 Mm       2,774,737   39.7                  7.71         2.11              5.74
     -1000 Mm         799,010   11.4                 12.92         7.32             10.31
      -500 Mm         352,594    5.0                 15.18         9.58             21.72
      -200 Mm         123,105    1.8  impact-grade pass — discarded (inside 2 R_J the
                                    point-mass model and the step size are both lying)
       200 Mm         133,540    1.9  impact-grade pass — discarded (inside 2 R_J the
                                    point-mass model and the step size are both lying)
       500 Mm         168,758    2.4                 15.51         9.91             20.59
      1000 Mm         276,021    3.9                 15.51         9.91             25.05
      3000 Mm         513,295    7.3                 14.25         8.65             15.53
```

Every row respects the bound. No braking rows appear because a Hohmann-class arrival is slow relative to Jupiter — you are already near the crank's minimum. The turning angle of v_inf at a representative pass is taken from the actual probe output in §D (patched-conic predicts from b, μ, v_inf; the integrator sees small perturbations from the rest of the system during the longer coasts).

### C — chaining Jupiter → Saturn

A departure-epoch × leg-length grid (capped at realistic launch Δv) finds candidate alignments, then a b-plane sweep is performed *on the live approach* (the lever amplifies small aim errors into Gm-scale errors at Saturn). Post-flyby TCM-2 walks the arrival to a chosen Saturn offset.

Typical production numbers (full grid; your run will vary slightly with exact best window):

```
window scan ... best: depart day 6413, Earth->Jupiter leg 3.4 yr
b-plane sweep ... best aim offset -1485 Mm ->
  ballistic Saturn approach 57.86 Gm at day 9499 ...

burn                              dv (m/s)
launch (targets the crank)            8611
TCM-1 at Jupiter-90d                 155.7
TCM-2 at Jupiter+150d                458.6

route                     launch (m/s)    TCMs  braking bill  time to Saturn
direct Hohmann (L15)             10298       -      5439 m/s         6.09 yr
via Jupiter (this)                8611     614      9023 m/s         8.45 yr
```

The honest verdict: cheaper launch, but stopping at Saturn "refunds" part of the gift as higher arrival speed. Voyager never paid that bill — she kept the crank for the next leg.

### D — escape, verified not asserted

Solar specific orbital energy before and after a genuinely close, zero-burn flyby. The single burn is up front and *before* the measurement window: from the real approach state 90 d before CA, boost along v_inf (capped so the ship is still bound — `e_pre < 0`), then re-aim that boosted arc to a ~3.6 R_J pass. Everything after is one ballistic arc, so the sign flip is bought by the crank, not by propellant.

```
pre-Jupiter specific energy (Sun frame): -164947155 J/kg  (negative = bound)
D demo flyby closest approach to Jupiter: 3.59 R_J (a real pass, outside the 2 R_J point-mass floor)
post-Jupiter specific energy (Sun frame, 60 d past CA, zero burn in flyby): 43948837 J/kg  (positive = ESCAPING (sign flip))
turning in Jupiter frame conserves |v_inf|; heliocentric |v| jumps because frame is moving.
|v_inf| in 17.58 km/s, out 16.98 km/s (conserved to 3 %); turning angle 86.6 deg
patched-conic deflection from measured r_p=3.59 R_J, v_inf=17.58 km/s: 76.7 deg (measured 86.6 deg; the residual is the n-body lesson)
```

Energy flips from bound (−164.9 MJ/kg) to escaping (+43.9 MJ/kg) with zero propellant in the flyby, at a real 3.59 R_J pass. |v_inf| is conserved across the encounter to 3% (the gain lives only in the Sun frame). The patched-conic deflection, computed from the *measured* periapsis and v_inf, predicts 76.7° against the integrated 86.6° — the residual is the n-body lesson. The same construction, reduced to asserts, is QA gate G2.

### E — the window

The 53-day grid over 20 years of departures shows good alignments are sparse — exactly why 1977 mattered and why the Grand Tour was a once-per-century (or rarer in game phasing) opportunity. Away from the sweet spots the miss distances at Saturn explode or the required TCMs become prohibitive.

### F — stretch

Uranus/Neptune continuation or explicit b-plane aimpoint tracking as a war-room tie-in are left as exercises (the toolchain transfers directly).

## Break it yourself

1. **Aim closer.** Shrink the Jupiter CA in Section B until you hit capture/impact. Find the knife-edge radius where the integrator (and point-mass model) stops being trustworthy.

2. **Wrong side.** Force a pass on the "front" side of Jupiter (positive offsets that produce deceleration). Measure the heliocentric speed loss and explain the sign via the symmetry in G4.

3. **Patched-conic worship.** Take the analytic exit state from the two-body crank formula and feed it straight into the Saturn leg as truth. Measure the miss distance at Saturn (lesson 14/15 pattern, now compounded by the lever).

## QA gates (engine-internal)

See the xUnit tests in `tests/SpaceSails.Core.Tests/Lab19GrandTourTests.cs` (G1–G5). They assert:

- G1. Determinism: two identical full-chain runs are byte-identical.
- G2. Energy sign: solar specific energy < 0 before Jupiter, > 0 after (for an escaping case), zero propellant during flyby.
- G3. Heliocentric |v| gain at Jupiter matches patched-conic prediction within ~10-15% (the residual is the lesson).
- G4. Symmetry: speed relative to Jupiter equal before/after encounter (within tolerance) — the gain lives only in the Sun frame.
- G5. Time-step honesty: rerun at 2×/0.5× dt; CA distance and exit velocity stay within bound.

## Real-Voyager anchors (our ephemeris vs. history)

Our sol.json / circular-rail ephemeris is not 1977's ephemeris. Numbers are illustrative of the geometry the game flies.

| Event                  | Real Voyager 2                  | Our probe (game rails) | Tolerance / note |
|------------------------|---------------------------------|------------------------|------------------|
| Launch                 | 20 Aug 1977                    | N/A (arbitrary phase) | — |
| Jupiter flyby          | 9 Jul 1979 (~1.9 y), 570 Mm    | ~2.7–3.4 y legs        | ±25% on leg time (JPL/NASA) |
| Saturn flyby           | 26 Aug 1981 (~4.0 y), 101 Mm   | ~8 y via assist        | longer due to no 1977 alignment (JPL) |
| Asymptotic speed       | V2 ≈ 15.3 km/s today           | 10–20 km/s band after J+S | matches (NASA) |
| Jupiter Δv gain        | ~10–16 km/s order (NASA plots) | 7–10 km/s in tables    | defensible band |
| 4-planet alignment     | ~175 y cycle                   | Sparse in grid scan    | game phasing differs |

Sources: NASA Science Voyager pages, JPL Voyager mission status, Wikipedia "Voyager 2" (cross-checked 2026). R4 speed-vs-distance reproduction left as exercise (gains and reaches in §B table are the data).

## Who paid?

In the real universe Jupiter paid (minuscule orbital slowdown). In this universe Jupiter is on rails — energy appears from nowhere. Every planner and game accepts the hole on purpose. Lesson 9 showed the alternative.

Every number above came from running the probe. Rerun after edits.
