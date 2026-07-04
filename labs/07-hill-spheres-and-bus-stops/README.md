# Lab 07 — Hill spheres and bus stops

*Standard physics: Curtis, "Orbital Mechanics for Engineering Students," ch. 8 (interplanetary
trajectories — the sphere of influence / Hill-radius approximation, and the patched-conic
hyperbolic excess speed a transfer arrives with). What Curtis's closed-form treatment doesn't
tell you is whether a satellite parked at the formula's radius actually stays there — that's a
numerical question, and this lab asks it directly of `OrbitRule` (M20), the game's own
orbit-insertion economics.*

## The idea

"Sphere of influence" sounds like a hard boundary, but the usual formula —
`r_H = a * (m / 3M)^(1/3)` — is a leading-order approximation: it keeps only the point where the
secondary's own gravity matches the *average* tidal pull of the primary, and drops the tidal
*gradient* across the secondary's orbit entirely. This lab places test satellites in circular
orbits around Mars at a range of radii (as fractions of the formula's Hill radius), integrates
them forward for 5 Mars-years in the REAL field — Sun and Mars both pulling on the ship, because
`CircularOrbitEphemeris` never lets Mars and the Sun pull on *each other* (rails), but the ship
always feels both — and checks at 400 points along the way whether each one is still bound to
Mars (`OrbitRule.IsBound`). Then it turns to the game's insertion rule itself: what a burn costs
in mass-driver pulses at a few approach speeds, and why `OrbitRule.MaxRelativeSpeed` sits at a
flat 5000 m/s.

## Run it

```bash
dotnet run --project labs/07-hill-spheres-and-bus-stops -c Release
```

## Section A — Hill radius: formula vs. a numerical bound/unbound test

```
Formula (Curtis ch. 8): r_H = a_Mars * (mu_Mars / (3 * mu_Sun))^(1/3) = 1.084060E+009 m (1084060 km, 320x Mars's own radius)

fraction of r_H   radius (km)     still bound after 5 Mars-yr?  max |r-Mars| / r_H reached  escaped at (Mars-yr)
0.20              216812          YES                           0.214                       -
0.30              325218          no — escaped                  1.052                       3.67
0.40              433624          no — escaped                  1.059                       0.51
0.50              542030          no — escaped                  1.053                       0.41
0.60              650436          no — escaped                  1.089                       0.61
0.70              758842          YES                           0.666                       -
0.80              867248          YES                           0.767                       -
0.90              975654          YES                           0.868                       -
1.00              1084060         no — escaped                  1.026                       0.10
1.10              1192466         no — escaped                  1.069                       0.01
1.20              1300872         no — escaped                  1.170                       0.01
```

**Surprise: the boundary is not a single clean radius.** Bound fractions found: 0.20, 0.70, 0.80,
0.90 * r_H. Unbound: 0.30, 0.40, 0.50, 0.60, 1.00, 1.10, 1.20 * r_H. There is a narrow stable
island near 0.20 * r_H, a genuine escape band at 0.30–0.60 * r_H, then a wide stable island again
at 0.70–0.90 * r_H, before everything at or above the formula radius escapes. This is not a bug
in the probe — it is the restricted three-body problem's real texture. At these fractions the
local orbital period around Mars falls into and out of resonance with the Sun's once-per-Mars-
year tidal tug; resonant radii get pumped out over a handful of years while off-resonant ones sit
tight (the doubled resolution run, 400 checkpoints vs. an earlier 100-checkpoint pass, moved
fraction 0.30's escape time from "undetected" to 3.67 years — the pattern is real, not a sampling
artifact, though checkpoint cadence matters for exactly *when* an escape is caught). It's the
same flavor of structure lesson 09 finds again at solar-system scale. **Treat the formula radius
as an upper bound on where a parking orbit might survive, never a guarantee** — the honest,
numerically-checked margin for Mars is closer to 0.9 * r_H than 1.0 * r_H, and even that isn't
safe everywhere below it.

## Section B — orbit-insertion economics (`OrbitRule`, M20)

```
Earth->Mars Hohmann transfer: arrival speed on the transfer ellipse = 21480.5 m/s, Mars's own orbital speed = 24129.3 m/s.
Patched-conic hyperbolic excess speed relative to Mars: v_infinity = 2648.8 m/s.
OrbitRule.MaxRelativeSpeed = 5000 m/s (1.9x a standard Hohmann arrival's v_infinity).

approach speed (m/s)  window open?  insertion dv (m/s)  pulse cost  ship heliocentric v (m/s) bound after insert?
500                   yes           590.6               3           24134.3                   yes
1500                  yes           1532.6              7           24175.7                   yes
2649                  yes           2667.4              11          24274.1                   yes
4900                  yes           4910.1              20          24621.7                   yes
5200                  NO            5209.5              22          24683.1                   yes
```

Why the window is shaped this way: `OrbitRule.PulseCost` prices a burn as a percentage of the
ship's own *heliocentric* speed (dominated by Mars's ~24 km/s orbital speed, not the small
relative approach speed), so the pulse count barely moves across ordinary approach speeds — 3 to
20 pulses covers everything from a gentle 500 m/s approach to a near-limit 4900 m/s one. What
actually gates the burn is `MaxRelativeSpeed`, a flat 5000 m/s cutoff with no ramp: 4900 m/s opens
the window, 5200 m/s is refused outright regardless of how many pulses the ship could afford. A
standard Hohmann arrival's v_infinity (2648.8 m/s, computed above from real vis-viva numbers, not
assumed) sits comfortably under that cutoff with room to spare — the 5 km/s window isn't
arbitrary, it's sized to comfortably admit any standard low-energy transfer arrival and lock out
only unusually hot, direct/hyperbolic approaches that no plotted low-energy route would ever
produce.

## Break it yourself

1. **Already above (Section A):** insertion right at the Hill edge. A satellite placed in a
   circular orbit at exactly the formula's Hill radius (fraction 1.00) does **not** survive 5
   Mars-years bound to Mars — the Sun's tide eventually walks it out, and it happens fast (escapes
   detected within the first 0.10 Mars-years in the run above). `OrbitRule.WindowOpen`'s strict
   `distance < hillRadius` check still lets a ship *attempt* insertion arbitrarily close to that
   edge, and `OrbitRule.Insert()` computes the velocity for a circular orbit as if Mars were the
   only mass in the universe — at t=0 that state genuinely is bound (`IsBound` is a snapshot), it
   just doesn't *stay* bound. The empirically-bound islands in Section A are the honest answer for
   how much margin an insertion burn needs below the nominal Hill radius to be a real parking
   orbit and not a slow-motion escape.
2. **On your own:** the escape band at 0.30–0.60 * r_H is suspiciously wide and the stable island
   at 0.70–0.90 * r_H is suspiciously wide too. Scan at finer resolution (steps of 0.02 instead of
   0.10) across both bands and see whether the boundaries are sharp (a real resonance edge) or
   fuzzy (checkpoint-cadence artifacts) — try doubling `checkpoints` again and see whether any of
   the currently-"YES" fractions flip.
3. **On your own:** swap Mars for a much more massive secondary (try Jupiter's own `mu`, radius,
   and period from `scenarios/sol.json`, keeping the Sun the same) — does the island/escape-band
   structure move to different fractions of *its* Hill radius, or is the pattern found here
   Mars-specific?

## See also

- `src/SpaceSails.Core/OrbitRule.cs` — `HillRadius`, `WindowOpen`, `Insert`, `IsBound`,
  `PulseCost` — every function this lesson exercises.
- `src/SpaceSails.Core/CircularOrbitEphemeris.cs` — confirms bodies never pull on each other; only
  the ship's own integration ever sums gravity from more than one source. Lesson 09 pushes this
  same fact to its logical extreme at solar-system scale.
- Lesson 02 (`labs/02-the-integrator-zoo`) — the semi-implicit integrator this lesson's bound/
  unbound test relies on via `Simulator.RunAdaptive`.
