# Lab 04 — The ten percent pulse

*Standard physics for the Oberth half (Curtis ch. 6); the reachable-speed-lattice half is this
repo's own arithmetic — `ManeuverPlan.cs` is the only propulsion model in the game, and it is
entirely multiplicative.*

## The idea

There is no "burn 1,200 m/s" API anywhere in `SpaceSails.Core`. Every speed change a ship can
make is one of four multiplications: x1.1, x0.9 (coarse, `ManeuverAction.Accelerate`/
`Decelerate`), or x1.01, x0.99 (`Fine`), or — since M16c — a free `Percent` per node. That's it.
This lesson takes that constraint seriously and computes three things it implies:

- **(a)** starting from one speed, only a discrete *lattice* of other speeds is reachable with a
  pulse budget — not a continuum. What's the lattice, how coarse is it, and how much does fine
  trim help close the gaps?
- **(b)** the same pulse *count*, spent at periapsis vs. apoapsis of an eccentric orbit, buys
  wildly different amounts of orbital energy — the Oberth effect (Curtis ch. 6), measured, not
  asserted.
- **(c)** circularizing after a transfer costs a countable number of pulses, and Mercury and
  Saturn cost very differently — this repeats the shape of the repo's own M16 finding (commit
  `cef2ac0`: arriving at Mercury too fast, "shed the difference once (2x -10% + a trim) and the
  ship holds [0.93, 1.00] x rM for 120 days with ZERO further thrust") with a fresh, from-scratch
  transfer so the numbers are this lesson's own.

## Run it

```bash
dotnet run --project labs/04-the-ten-percent-pulse -c Release
```

## Section A — the reachable speed lattice

Starting speed is Earth's own circular solar speed (real Sun `mu` and Earth orbit radius from
`scenarios/sol.json`):

```
v0 = 29784.480 m/s
```

Pulses commute (scalar multiplication doesn't care about order), so with `j` accelerate and `k`
decelerate pulses the reachable multiplier is always `1.1^j * 0.9^k` — the firing *sequence*
never matters, only the counts. Enumerating every `(j, k)` with `j + k <= 6`:

```
With <= 6 total coarse (+-10%) pulses (any mix of accelerate/decelerate):
  distinct combinations (j,k) tried: 28
  distinct reachable multipliers:    28
  tightest neighboring gap in the lattice: 6.561000E-003 (multiplier units)
```

Every combination lands on a different multiplier here (28 combos, 28 distinct values) — up to 6
pulses the lattice hasn't started folding back on itself yet. The tightest gap between
neighboring reachable speeds is still 0.66% of `v0`; there is no way to make a *smaller* speed
change than that with this many pulses; that gap is the floor, not the average spacing.

The pure accelerate-only chain makes the geometric (not linear) growth concrete:

```
j pulses  multiplier    speed (m/s)
0         1.000000      29784.480
1         1.100000      32762.928
2         1.210000      36039.221
3         1.331000      39643.143
4         1.464100      43607.457
5         1.610510      47968.203
6         1.771561      52765.023
```

Now a physically meaningful target instead of a round number — the ratio of Mars's circular
solar speed to Earth's, `sqrt(r_Earth / r_Mars)`:

```
Target: v_circ(Mars)/v_circ(Earth) = 0.810132 (a real orbital-speed ratio, not a round number)
Best <= 6-pulse coarse-only combo: 0 accelerate + 2 decelerate = x0.810000 (target x0.810132), residual 0.0162 %
Same combo + 0 fine (+-1%) trim pulses: x0.810000, residual 0.016233 %
```

Two decelerate pulses (`0.9^2 = 0.81` exactly) already lands within 0.02% of the real Mars/Earth
speed ratio — a genuine coincidence of the numbers, not tuned. **The fine trim search adds
*zero* pulses here**, and that's the interesting part: the residual (0.016%) is already smaller
than one fine step (1%), so every fine pulse available would overshoot the target by more than
the coarse-only combo already misses by. Fine trim only helps when the *coarse* residual is
bigger than a fine step — Section C below finds cases where it does help, a lot.

## Section B — the Oberth effect (Curtis ch. 6)

An eccentric solar orbit, periapsis 0.5 AU / apoapsis 1.5 AU:

```
Starting solar orbit: periapsis 0.500 AU, apoapsis 1.500 AU, e = 0.500000, a = 1.000 AU, period = 365.265 days
vis-viva: v_periapsis = 51588.232 m/s, v_apoapsis = 17196.077 m/s, specific energy = -4.435576E+008 J/kg

Sanity check — coasting from periapsis to apoapsis with the game's own Simulator:
  computed apoapsis radius = 1.500000 AU (closed form 1.500000)
  computed apoapsis speed  = 17196.077 m/s (closed form 17196.077)
```

The game's own integrator (dt = 60 s, no shortcuts) reproduces the closed-form apoapsis to six
figures — the same integrator every burn below runs through.

Spend the *identical* maneuver — 2 accelerate pulses, x1.21 total — at each extreme:

```
Same maneuver spent at each extreme: 2 accelerate pulses (x1.2100 total)

burn location   new energy (J/kg)   new a (AU)    new e       new peri (AU)   new apo (AU)
periapsis       1.740077E+008       unbound       -           -               -
apoapsis        -3.749393E+008      1.183012      0.267950    0.866024        1.500000

specific energy gain (J/kg): periapsis burn = 6.175653E+008, apoapsis burn = 6.861836E+007
periapsis burn gains 9.000x the specific energy of the identical burn at apoapsis — same fuel (same pulse count), very different orbit. That ratio is v_p^2/v_a^2 to first order (9.000) because Delta-E ~ v * Delta-v, and Delta-v is the same fraction of v either way.
```

**The surprise worth sitting with:** the *same two pulses*, spent at periapsis instead of
apoapsis, don't just make a bigger ellipse — they blow this orbit **open entirely**. Post-burn
specific energy at periapsis is positive (`1.74e8 J/kg`, unbound — the ship escapes the Sun on a
hyperbola); the same pulses at apoapsis leave a perfectly ordinary, still-bound ellipse
(`a = 1.183 AU`, apoapsis pinned at the burn point as expected, periapsis raised to 0.866 AU).
The energy gain ratio (9.000x) lands exactly on `v_periapsis^2 / v_apoapsis^2` — Oberth in one
number: burning where you're already fast multiplies the *same* Δv into far more Δ-energy,
because kinetic energy is quadratic in speed and the burn is a fixed *fraction* of whatever speed
you already have.

## Section C — circularizing at Mercury vs. Saturn (echoes the M16 finding)

Same transfer shape as M16b: one whole-pulse burn at Earth's orbit (decelerate to fall inward,
accelerate to climb outward) turns Earth's departure radius into the *other* apsis of a new
ellipse. Searching whole pulse counts for whichever lands that apsis closest to each
destination's orbit radius:

```
--- Mercury (inward transfer) ---
transfer burn: 3 Decelerate pulses at Earth's orbit -> new periapsis = 0.3619 AU (target 0.3871 AU)
coasted (game's own adaptive integrator) to the apsis at sim time 102.62 days: radius 0.3619 AU (closed form 0.3619 AU)
crossing speed = 60.000 km/s (circular there = 49.512 km/s, ratio = 1.211843)
circularizing combo: 0 Decelerate pulses + 19 fine trim pulses -> ratio 1.001186 (residual 1.186302E-003)
120-day hold after trim, zero further thrust: r in [0.9998, 1.0050] x crossing radius
reference — exact circular speed there (not pulse-reachable): r in [0.9989, 1.0011] x crossing radius

--- Saturn (outward transfer) ---
transfer burn: 3 Accelerate pulses at Earth's orbit -> new apoapsis = 7.7551 AU (target 9.5824 AU)
coasted (game's own adaptive integrator) to the apsis at sim time 1672.72 days: radius 7.7551 AU (closed form 7.7551 AU)
crossing speed = 5.112 km/s (circular there = 10.695 km/s, ratio = 0.477953)
circularizing combo: 7 Accelerate pulses + 7 fine trim pulses -> ratio 0.998581 (residual 1.419042E-003)
120-day hold after trim, zero further thrust: r in [1.0000, 1.0000] x crossing radius
reference — exact circular speed there (not pulse-reachable): r in [1.0000, 1.0000] x crossing radius
```

Three whole pulses is the closest 3-body-free integer burn can put the transfer's own apsis to
either planet's radius — note it doesn't land exactly on either (0.3619 AU vs. Mercury's 0.3871
AU, 7.7551 AU vs. Saturn's 9.5824 AU): the *transfer* itself is quantized before circularizing
even starts, the same phenomenon as Section A, one level up.

**The real finding is the cost gap.** Mercury needs 0 coarse pulses and 19 fine trims to reach
1.0012 (the crossing speed is only 21% high, less than the ~26% a single coarse decelerate pulse
would shed, so the search wisely skips it and works entirely in 1% steps). Saturn is a
completely different bill: the transfer arrives at only 48% of local circular speed (an outward
Hohmann-style coast always arrives *slower* than circular, the same reason a thrown ball is
slowest at the top of its arc) and needs 7 accelerate pulses (x1.1^7 ≈ x1.95) *plus* 7 more fine
trims to close in. Circularizing near Mercury is cheap; circularizing near Saturn from the same
kind of transfer costs roughly triple the coarse pulses and about the same fine-trim tail. Both
holds are tight for 120 days on zero further thrust — Mercury within [0.9998, 1.0050] x its
crossing radius, Saturn within [1.0000, 1.0000] — comfortably inside the M16b-style "holds for
120 days" claim, and the pulse-quantized numbers track the exact-circular reference (last row of
each block) closely enough that the residual from Section A's lattice, not integration error, is
the whole gap between them.

## Break it — reach exactly circular speed with only pulses

```
=== BREAK IT: try to land exactly on ratio = 1.000000 with pulses only ===
Search up to 20 coarse + 60 fine pulses in either direction for the closest approach to 1.0:
Best combo found: 0 accelerate + 0 decelerate coarse, 0 fine-accelerate + 0 fine-decelerate -> residual 0.000000E+000
```

The only exact hit is the trivial do-nothing combo. Reaching precisely 1.0 with a nonzero
combination requires integers `(j, k, m, n)`, not all zero, solving
`1.1^j * 0.9^k * 1.01^m * 0.99^n = 1`, i.e. `j*ln(1.1) + k*ln(0.9) + m*ln(1.01) + n*ln(0.99) = 0`.
These four logarithms come from four unrelated decimal ratios — nothing designed them to be
commensurate — so outside of the trivial solution there is no reason to expect (and the search
up to 20+60 pulses each direction finds none) an exact cancellation. Every reachable speed other
than "don't fire" is *approximately* circular at best. That's not a bug to route around; it's
why the cockpit's "circular here" readout (M16b) is phrased as a target to trim *toward*, never a
button that zeroes itself out.

## Break it yourself

1. **Already above:** Section A's Mars-ratio search used <= 6 total pulses. Raise the cap to 12
   and rerun — does the tightest lattice gap shrink roughly like `0.1^(pulses/2)` (geometric,
   like the accelerate-only chain), and does the Mars-ratio residual actually improve, or was 2
   decelerate pulses already lucky?
2. **Already above:** Section B used 2 pulses at each extreme. Try 1 pulse, then 3. At what
   pulse count does the periapsis burn stop being "a bigger ellipse" and start being "unbound"
   for *this* orbit's periapsis speed? (Escape speed at 0.5 AU is `sqrt(2 * mu / rp)` — compute
   it and compare to `v_periapsis * factor^pulses` before you touch the code.)
3. **On your own:** Section C picked the destination's orbit radius as the search target. Swap
   in Venus (`orbitRadiusM` 1.0821e11 in `scenarios/sol.json`) — Venus circularization is a
   third inward case, cheaper or pricier than Mercury? Predict from the speed ratio before
   running.

## See also

- `src/SpaceSails.Core/ManeuverPlan.cs` — the whole propulsion model: `AccelerateFactor`,
  `DecelerateFactor`, `Fine*Factor`, and the free `Percent` node.
- `src/SpaceSails.Core/Simulator.cs` — `RunAdaptive`/`ProjectAdaptive`, used here for every
  coast and every burn.
- Commit `cef2ac0` ("M16b: 'circular here' HUD readout + the Mercury orbit-holding proof") — the
  in-game finding this lesson's Section C repeats from scratch.
- Lesson 1 (`labs/01-falling-is-orbiting`) Break-it #2 — the first look at a single pulse turning
  a circle into an ellipse; this lesson is what happens when you chain many of them.
- Lesson 5 (`labs/05-transfers-without-formulas`) — the same transfer-burn arithmetic, scaled up
  to a full Earth-Mars Hohmann comparison against `RoutePlanner`'s search.
