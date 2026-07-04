# Lab 06 — Closest approach, found honestly

*Standard numerical-methods territory (parabola vertex interpolation); the "why the client is
adaptive at all" half is straight continuity with lesson 3.*

## The idea

The planner's closest-pass warning (M18, `src/SpaceSails.Core/ClosestApproach.cs`) has to answer
"how close does this ribbon get to that planet" from a trajectory that is only ever known at a
finite set of sampled points. A flyby's true minimum distance almost never lands exactly on one
of them. `ClosestApproach.Passes` handles this with a coarse stride, a stride-1 refine around the
coarse minimum, then a parabola fit on d² between the three bracketing samples (see the doc
comment on `MostSevere`). This lesson measures what that buys on a genuine Earth-Mars near-miss —
naive per-step minimum vs. the refine, at several sample densities, judged against a much denser
scan as ground truth — and finds a real surprise along the way: the client's own adaptive
stepping does not automatically rescue a fast flyby either, for exactly the reason lesson 3
already found.

## Run it

```bash
dotnet run --project labs/06-closest-approach-found-honestly -c Release
```

## The near-miss

Departure day 40 from Earth (the same window lesson 5's own scan found productive for Mars), 3
accelerate pulses, flown as a coast with no brake — so the ship genuinely flies past Mars instead
of parking there. Mars's real (if modest) `mu` is in this probe's ephemeris, so the flyby actually
perturbs the ship.

## Section A — dense-scan ground truth

```
dt = 5 s, 1555201 samples: minimum distance = 157865.976 km at t = 116.34045 days
```

This stands in for "the true minimum" for the rest of the probe — dense enough that a coarser
scan's job is to approximate it cheaply, not to improve on it.

## Section B — naive minimum vs. the parabola refine, three densities

```
density       samples   naive (km)      naive err %     refine (km)     refine err %
1 day         92        1988971.031     1.160E+003      1937511.011     1.127E+003
3 hours       722       380845.813      1.412E+002      379058.029      1.401E+002
10 minutes    12961     170117.074      7.760E+000      170055.125      7.721E+000
```

At every density the refine beats or matches the same-density naive number, but on this
particular near-miss the improvement is modest (a few percent, not orders of magnitude) — because
this is a *fast* encounter (see Section C), and the parabola fit's job gets harder the fewer
samples land anywhere near the true dip. The refine is never worse than naive here, which is the
real, load-bearing guarantee; how much better it does depends on how well the coarse stride
happened to bracket the minimum in the first place.

## Section C — the client's real setting is adaptive, and it does NOT automatically win

`Map.razor` actually calls `_simulator.ProjectAdaptive(_ship, _plan, horizon, maxTimeStep: 3 * 3600, ...)`
— `maxTimeStep` is only a *ceiling*; the real step is lesson 3's `dt = dynamical-time/64`, clamped
to it. Running the identical adaptive configuration on this near-miss:

```
adaptive (maxTimeStep = 3 h ceiling, same as the client), 722 samples: naive = 380845.813 km (err 1.412E+002 %), refine = 379058.029 km (err 1.401E+002 %)
```

Virtually identical to the uniform "3 hours" row in Section B — adaptive stepping bought nothing
here. The samples straddling the true minimum show why:

```
  t = 116.1667 days, dist = 508309.1 km
  t = 116.2917 days, dist = 407988.8 km
  t = 116.4167 days, dist = 380845.8 km
  t = 116.5417 days, dist = 440618.5 km
  t = 116.6667 days, dist = 560139.2 km
```

**The surprise:** adaptive stepping sizes the *next* step from the *current* distance. Approaching
Mars, the ship is still outside the radius where `dynamical-time/64` drops below the 3-hour
ceiling, so it takes one more full 3-hour step — and that single step carries it clean through
periapsis (157,866 km) to the other side, landing at 380,845 km on the far side without ever
sampling anywhere near the true minimum. This is lesson 3's own finding ("adaptive doesn't
automatically win") showing up again in a completely different system: it helps enormously for an
encounter that lasts many steps, and does nothing for one fast enough to fit inside a single one.

## Section D — the M18-style accept check: how fine actually earns "within ~0.1%"

```
density       samples   refine (km)     refine err %    verdict
10 minutes    12961     170055.125      7.721E+000      fail
1 minute      129601    158992.658      7.137E-001      fail
10 seconds    777601    157968.400      6.488E-002      PASS
```

The ~0.1% M18-style tolerance (commit `339fe7a`'s own claim about the closest-pass warning) is
real and reachable — for *this* fast, deep encounter it just needs samples every 10 seconds, far
finer than the client's 3-hour ribbon ceiling. That's not a contradiction: the client's ribbon is
built for showing a ship *where it's going*, cheaply, over a 730-day horizon; a proximity check
that needs to be accurate to 0.1% on a close, fast graze is a different, much more demanding
computation than drawing the line on screen, and this probe is the honest measurement of exactly
how much finer.

## Break it #1 — land a sample exactly on the true minimum

```
dt chosen so departure + 109930 steps lands exactly on the ground-truth minimum time: dt = 60.000136 s
sample landed at t = 116.340451 days (ground truth 116.340451 days)
naive minimum = 158993.220150 km, refine = 158992.660786 km, ground truth = 157865.976268 km
```

When a sample already sits (to within microseconds) on the true minimum's time, naive and refine
agree with each other almost exactly (158,993.22 vs. 158,992.66 km) — the parabola fit has nothing
left to correct for, because there's no interpolation error to remove. Both numbers still carry
the same ~0.71% residual against the 5-second ground truth that Section D's "1 minute" row showed
— that's the dt = 60 s *integration* accuracy limit, not a sampling artifact, and the refine
doesn't do worse here; it just stops mattering. Its whole value is for the (usual) case where the
minimum falls *between* samples, which is every density in Sections B and D above.

## Break it #2 — a mid-path burn near closest approach

```
Extra 3-pulse accelerate burn inserted at t = 116.3403 days (inside the 600-second bracket straddling the original minimum near t = 116.3405 days)
new dense-scan ground truth: 157866.231 km at t = 116.3403 days
at dt = 600 s: unburned refine err = 7.721E+000 %, burned refine err = 7.760E+000 % (burned naive err = 7.760E+000 %)
```

A 3-pulse accelerate burn dropped exactly on the coarse scan's own vertex sample nudges the
refine's error the wrong way (7.721% to 7.760%) — a small effect at this density, but a real and
directionally honest one: the parabola-on-d² fit assumes smooth motion across the three
bracketing samples, and a burn dropped inside that bracket puts a genuine velocity
*discontinuity* between two of them, which the fit has no way to model — it can only ever see the
two positions, not the kink in between. (Earlier attempts at this break-it with a *decelerate*
burn instead made the fit look *better* by coincidence — slowing the ship right at closest
approach softens exactly the high relative speed that makes this encounter hard to resolve in the
first place. Both results are real; reporting only the flattering one would have been dishonest.)

## Break it yourself

1. **Already above:** Section D found 10-second sampling clears the 0.1% bar for this encounter.
   Binary-search between 60 s (0.71% — fails) and 10 s (0.065% — passes) for the actual crossover
   density, and check whether the relationship between dt and error looks linear, quadratic, or
   neither over that range.
2. **Already above:** Break-it #2 used a 3-pulse accelerate burn. Try 10 pulses at the same
   instant — does the refine's error grow roughly in proportion to the burn size, or does it stay
   small until some threshold?
3. **On your own:** Section C found that a fast, deep encounter can slip through the adaptive
   scheme's ceiling in a single step. `Simulator.RunAdaptive`/`ProjectAdaptive` compute dt from the
   position *before* the step, not after. Sketch (in comments, no need to implement) a scheme that
   looks ahead — e.g., halving dt whenever the post-step distance would undercut the pre-step
   dynamical-time estimate — and estimate how much extra cost that would add to a typical 730-day
   ribbon that never encounters anything this sharp.

## See also

- `src/SpaceSails.Core/ClosestApproach.cs` — `Passes`, `MostSevere`, and the parabola-on-d² refine
  under test here.
- `src/SpaceSails.Core/Simulator.cs` — `DynamicalTime`/`ProjectAdaptive`, and Section C's whole
  surprise.
- Lesson 3 (`labs/03-time-step-is-a-lie-you-choose`) — the original "adaptive doesn't
  automatically win" finding, on a Sun-grazing flyby instead of a Mars one.
- Commit `339fe7a` ("M18: closest-pass warning in the planner...") — the UI and the accept-style
  claim this lesson reproduces the arithmetic of.
