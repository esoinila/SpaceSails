# Lab 03 — Time step is a lie you choose

*Standard numerical-methods territory; Curtis doesn't cover adaptive stepping (he solves
orbits in closed form and only reaches for numerics in the perturbation chapters), but the
underlying physics — what a hyperbolic flyby's periapsis actually is — is straight Curtis
ch. 2/3. The "why not just use the closed form" argument is this repo's own history, quoted
below from `Simulator.cs`.*

## The idea

Every fixed-step integrator lesson so far (labs 1–2) picked one dt and lived with it. The live
game doesn't get that luxury: a ship in deep space and a ship skimming the Sun's corona are in
wildly different dynamical regimes, and a single dt that's safe for one is either wastefully
slow or dangerously coarse for the other. `Simulator.ProjectAdaptive` (and its live-play sibling
`RunAdaptive`, the M19 high-warp path) solves this by picking dt fresh every step: a fixed
fraction (1/64) of the **local dynamical time** `sqrt(d³/mu)`, clamped to `[1 s, 1 h]` — coarse
far from any mass, fine close to one.

Why not just use the textbook shortcut instead — solve the two-body problem in closed form
(Kepler's equation / universal variables) and skip numerical integration for the parts between
maneuvers? `Simulator.cs` answers this directly, in its own doc comment:

> The classic closed-form alternative (universal-variable Kepler / patched conics) is rejected
> on purpose — it assumes one attracting body per arc and would disagree with the integrator
> exactly where the game happens: flybys.

This lab tests both halves of that claim numerically: how much does a *fixed* dt disagree with
the truth at a flyby, and how well does the *adaptive* scheme actually do — including finding a
case where "adaptive" doesn't automatically mean "safe."

## Run it

```bash
dotnet run --project labs/03-time-step-is-a-lie-you-choose -c Release
```

## The flyby

A fast hyperbolic pass: start 3 AU out, moving straight at the Sun at 40 km/s (comet/interstellar
-visitor speed) with a 0.16 AU sideways offset (impact parameter). The ground truth — periapsis
distance — comes from the same closed-form energy + angular-momentum relations as labs 1 and 2,
which hold for *any* conic, hyperbolic included:

```
=== The flyby ===
v_infinity = 40000 m/s, impact parameter = 0.160 AU
eccentricity e = 1.025933 (hyperbolic, e > 1), semi-major axis a = -0.878829 AU
closed-form periapsis = 0.022791 AU (3.409467E+009 m, 4.90x the Sun's radius)

Integration horizon: 22440000 s (259.7 days)
```

A periapsis under 5 solar radii is a genuinely deep, fast graze — exactly the kind of encounter
`OrbitRule`/`ClosestApproach` need to get right, and exactly the kind of encounter a coarse dt
can blow straight through.

## Section A — fixed dt vs. the game's adaptive default

```
method                  steps (cost)min radius (AU) abs. error (m)rel. error
fixed dt = 3600 s       6234        0.022975        2.753E+007    8.073E-003
fixed dt = 600 s        37400       0.022795        6.095E+005    1.788E-004
fixed dt = 60 s         374000      0.022791        7.116E+003    2.087E-006
adaptive (game default) 6431        0.022980        2.837E+007    8.320E-003
```

Fixed dt = 60 s nails the periapsis to 7 km out of 3.4 million km — six-figure accuracy, at the
cost of 374,000 steps for one flyby. Fixed dt = 3600 s is 4000x cheaper and off by 27,500 km
(0.8% relative error) — plausible-looking, wrong.

**The genuine surprise:** the adaptive default costs almost exactly the same as fixed dt = 3600 s
(6431 steps vs. 6234) and is not more accurate — if anything it's very slightly worse (0.832%
vs. 0.807% relative error). That is not what "coarse in deep space, fine near a mass" promises,
and it's worth understanding rather than papering over.

The reason is in the numbers: the region where dynamical time drops enough to pull dt below the
1-hour ceiling only starts inside about 0.13 AU of the Sun (`d` where `sqrt(d³/mu)/64 < 3600 s`).
The periapsis here is 0.023 AU — well inside that zone — but the ship crosses the whole 6 AU
trip in 260 days, and almost all of that time is spent outside 0.13 AU, where adaptive dt is
already clamped to the same 3600 s ceiling as the "fixed 3600 s" run. The extra resolution only
applies for a short arc right at the point that matters most, and even there dt only shrinks to
roughly 270 s (dynamical time near periapsis is itself not that short at 4.9 solar radii — this
is not a grazing-the-photosphere encounter). Net effect: at these clamp settings, for *this*
periapsis depth, adaptive buys almost nothing over naively picking 3600 s everywhere. The 1-hour
ceiling, not the 1/64 rule, is the bottleneck — which is exactly what the break-its below
confirm by pulling on each knob separately.

*(A second, smaller effect is in play too: "minimum radius over the recorded samples" is a
discretization measurement — a long step can simply land past the true periapsis without ever
sampling near it. Some of the gap between the fixed-60s truth and everything else is genuine
integration error building up over the approach; some of it is just where the samples happen to
fall. Both are real dt-choice consequences, which is the whole point of this lesson.)*

## Break-it #1 — widen the clamp

```
=== BREAK IT: widen the clamp — let adaptive dt run coarser near the Sun ===
clamp                   steps (cost)min radius (AU) abs. error (m)rel. error
max dt = 3600 s         6431        0.022980        2.837E+007    8.320E-003
max dt = 21600 s        1359        0.023027        3.532E+007    1.036E-002
max dt = 86400 s        668         0.023040        3.732E+007    1.095E-002
```

Raising the ceiling from 1 hour to 6 hours cuts cost by 4.7x for only a 25% increase in relative
error; raising it to a full day cuts cost by another 2x for another 6%. The ceiling mostly
controls **cost**, barely **accuracy** — because deep space really is uneventful, and a bigger
step out there doesn't bend the trajectory much. This is the "coarse in deep space" half of the
adaptive design working as intended, even though Section A showed the "fine near a mass" half
underperforming at this particular periapsis depth.

## Break-it #2 — loosen the /64 fraction

```
=== BREAK IT: loosen the /64 — coarser or finer dynamical-time fraction ===
fraction                steps (cost)min radius (AU) abs. error (m)rel. error
fraction = 1/8          6237        0.023146        5.313E+007    1.558E-002
fraction = 1/64         6431        0.022980        2.837E+007    8.320E-003
fraction = 1/512        8887        0.022820        4.428E+006    1.299E-003
```

Here's the flip side: going from 1/64 to 1/8 (8x coarser near the Sun) barely changes the step
count (6237 vs. 6431 — deep space still dominates the total) but nearly doubles the error.
Going to 1/512 (8x finer) does cost noticeably more (8887 steps) and buys back a real 6.4x
accuracy improvement. **Cost and accuracy are set by almost independent knobs** here: the max-dt
clamp mostly taxes cost, the dynamical-time fraction mostly taxes accuracy near the mass that
matters. Tuning "how far in warp mode you can push a save file" and "how honest the closest-pass
warning is" are, to first order, two different dials — which is worth knowing before reaching
for either one.

## Break it yourself

1. **Already above:** both clamp-widening and fraction-loosening are run for you. Try the
   opposite extreme — `minTimeStep` raised from 1 s to, say, 60 s — on this same flyby. Does
   raising the *floor* matter at all here, given how far this periapsis sits from where a 60 s
   floor would even bind?
2. **On your own:** shrink the impact parameter (say to 0.02 AU) so periapsis drops to a couple
   of solar radii instead of five. Does the adaptive default's advantage over fixed dt = 3600 s
   reappear once the encounter is genuinely deep enough to fall well inside the 0.13 AU
   threshold this lesson found?
3. **On your own:** this probe judges "closest approach" by the minimum radius among recorded
   samples. Add true quadratic refinement between the three samples nearest the sampled minimum
   (fit a parabola to d² vs. time, like lesson 6's `ClosestApproach` will) and see how much of
   the gap between fixed-3600 and fixed-60 was sampling artifact vs. real integration error.

## See also

- `src/SpaceSails.Core/Simulator.cs` — `ProjectAdaptive`, `RunAdaptive`, and `DynamicalTime`;
  the doc comment quoted above is right there in the source.
- `docs/m4-spec.md` — where `ProjectAdaptive` first shipped, and the provisional 1e9 m capture
  threshold this lab's periapsis error is small compared to (for now — try the deeper flyby in
  "break it yourself" #2 and see how that margin holds up).
- Lessons 1–2 (`labs/01-falling-is-orbiting`, `labs/02-the-integrator-zoo`) — the same
  integrator, same closed-form-vs-computed methodology, on gentler orbits where fixed dt was
  never in question.
