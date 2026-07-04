# Lab 10 — Fast enough for 10,000x

*Standard performance-engineering territory — nothing fictional in this lesson. Curtis has
nothing to say about wall-clock milliseconds; the "textbook" this lesson argues with is this
repo's own commit history, quoted verbatim below.*

## The idea

Every earlier lesson asked "is the number right." This one asks "is the number fast enough" —
a different question with a different kind of surprise, because a performance trick that is
*true* (produces the right answer) can still be a bad trade once you actually put a stopwatch
on it. Two real war stories set the stakes:

> M5 (commit `1bf882b`): "~1 fps at 10000x warp: NPCs integrate at Core's new
> TrafficSchedule.NpcTimeStep (60 s) instead of the player's dt=1 s... 53-57 fps after."

> M19 (commit `63b06de`): "at effective warp >= 100 the accumulator is consumed in fixed 60 s
> quanta via RunAdaptive... Measured headless in sol-eu (plasma on, full traffic): 9,969 sim-s
> per real-s at 10000x — effectively the full requested rate."

Both numbers came from a browser/server loop (M5's own notes elsewhere in this repo: "Debug
WASM = interpreter ~100x slower" than native). This lab's probe is a bare native Release
console loop instead — a genuinely different, much cheaper environment — so its numbers are
reported honestly as **a dev machine, native Release**, not as an attempted reproduction of
9,969. What transfers is the *shape*: naive dt=1 stepping is drastically slower than 60 s
quanta, and that gap is the whole reason `RunAdaptive` exists.

## Run it

```bash
dotnet run --project labs/10-fast-enough-for-ten-thousand-x -c Release
```

## Section A — cost per `Step()` call, three dt choices

The ephemeris here is the full 17-body Sol system straight out of `scenarios/sol.json` (14
bodies with nonzero mu), so "cost per step" means exactly what it costs the live game to ask
"what does gravity feel like right here" — no toy one-body stand-in.

```
method                      calls       total ms    ms/call       sim-s/call  sim-s per wall-s
fixed dt = 1 s              2000000     1982.0      9.910E-004    1           1.009E+006
fixed dt = 60 s (NPC quantum)2000000     2208.6      1.104E-003    60          5.433E+007
RunAdaptive(60 s quantum)   2000000     4647.5      2.324E-003    60          2.582E+007
```

The two *fixed* rows cost almost the same **per call** (9.91E-004 ms vs 1.10E-003 ms) — dt
doesn't change the arithmetic, it just changes how much sim-time one identical-cost call
buys. All of the dt=60 row's ~54x throughput advantage over dt=1 is exactly that: 60x more
sim-time per call, at essentially the same call cost.

**The genuine surprise** is `RunAdaptive`: its per-call cost is **not** "about the same" as
fixed dt=60 — it costs 2.10x more per call, because before `StepBy` runs its own
`GravitationalAcceleration` pass over the 14 massive bodies, `RunAdaptive` first runs a full
`DynamicalTime()` pass over the *same* 14 bodies to choose its step size. Two body-loops per
call instead of one. It still wins overall (see the fps table below), purely because one call
now covers 60 sim-seconds instead of 1 — 60x fewer calls comfortably outruns a ~2x per-call
tax. But "the fast path is free" is false; measured, it is a real, worthwhile, *non-free*
trade — exactly the kind of thing this lesson exists to catch. (Wall-clock benchmarks jitter
run to run — this ratio has moved between 2.1x and 2.3x across reruns on this same machine;
the qualitative finding, "meaningfully more than 1x," hasn't.)

### Reproducing the M5/M19 shape (not the M5/M19 numbers)

```
method                      wall-s for 10000 sim-s  implied fps at warp 10000x
fixed dt = 1 s              0.00991                 100.9
fixed dt = 60 s (NPC)       0.00018                 5433.3
RunAdaptive(60 s quantum)   0.00039                 2582.0
```

On this dev machine, native Release, one ship, no rendering: fixed dt=1 already clears 60 fps
at warp 10000x (101 fps) — nowhere near M5's "~1 fps," because a native Release console loop
is a wildly cheaper environment than Debug WASM in a browser with rendering, traffic, and
plasma alongside it. That gap **is** the lesson: the M5/M19 numbers were never really about
this arithmetic being slow in the abstract; they were about where that arithmetic runs.
`NpcTimeStep=60` (54x here) and `RunAdaptive` (26x here) both being drastically faster than
dt=1, on *any* machine, is the part that actually transfers.

## Section B — one ship vs. the game's real 23-NPC roster

```
traffic=8, pods=3, depots=12 -> total NPC entries = 23
(depots: one per planet [8] + notable stations/havens [mercury-compute, satellite-factory,
 enceladus, ringside-exchange = 4] = 12; matches M22's 'a bus stop at every planet orbit.')

roster                                  ms/frame (one 60 s quantum each)
1 integrated ship                       1.348E-003
11 integrated ships (8 traffic+3 pods)  1.279E-002
12 rails-only depots (DepotState calls) 4.119E-003
all 23 NPCs, one frame                  1.691E-002
```

The game's actual NPC roster (`TrafficSchedule.Generate` + `GeneratePods` + `GenerateDepots`
against the real Sol tables) really is 23 entries — 8 traffic haulers, 3 mass-driver pods, and
12 depots (one per planet, plus the four notable stations/havens M22 gave their own bus stop).
11 integrated ships cost 9.49x one ship — close to the expected 11x (each runs its own
independent gravity sum; the shortfall is warm-cache/branch-prediction noise, not a real
sub-linear effect). Per unit, one integrated ship costs 1.163E-003 ms/quantum; one depot costs
3.432E-004 ms/quantum — **a depot is about 3.4x cheaper, not free.** `DepotState` still walks
the parent-body chain and calls `Position()` twice (a finite-difference velocity) — it just
never runs a gravity sum. Riding rails is a real, measured discount, not the zero-cost
idealization "costs nothing to step" reads like until you actually measure it.

## Section C — the determinism constraint, verified rather than asserted

`Map.razor`'s own M19 dispatch: effective warp < 100 calls `Simulator.Step(dt=1 s)` every
frame — unchanged since before M19; effective warp >= 100 switches to `RunAdaptive` in fixed
60 s quanta. This probe reimplements that exact dispatch and diffs the result against pure
fixed dt=1 stepping over the same 20-day Earth-vicinity cruise with a mid-course burn (the
same shape as `SimulatorTests.RunAdaptive_MatchesFixedStepWithinTolerance`, run here against
the full 17-body ephemeris instead of sun+earth alone):

```
regime                            |position - truth| (m)    relative to Earth-distance
warp 50 (below threshold, dt=1s)  0.000000E+000             0.000E+000
warp 1000 (RunAdaptive, 60s quanta)2.962356E+005             5.023E-006
```

The warp-50 row is not "very small" — it is **exactly zero**. Below the threshold, the
dispatch calls the identical `Step(dt=1 s)` code path, so there is no floating-point
operation left to differ. That is what "byte-identical below warp 100" means, verified here
rather than asserted. Above the threshold, `RunAdaptive`'s 60 s quanta is a real, measured,
bounded approximation — 5E-006 relative error on this cruise, small but never zero, which is
exactly why the client only turns it on where deep space makes it cheap to be a little bit
wrong.

## Break-it — widen the quantum past the client's 60 s choice

```
quantum (s)   |position - truth| (m)    relative to Earth-distance
60            2.962356E+005             5.023E-006
600           3.007322E+006             5.099E-005
3600          1.805433E+007             3.061E-004
21600         1.805433E+007             3.061E-004
86400         1.805435E+007             3.061E-004
```

Error grows steadily from 60 s to 3600 s, then goes almost dead flat from 3600 s onward
(1.805433E+007 at 3600 s vs 1.805435E+007 at 86400 s — agreement to 6 significant figures).
**The genuine surprise:** the client's outer "quantum" is not just a call-batching
convenience. `RunAdaptive`'s own boundary rule is
`dt = min(clamp(dynamicalTime/64, 1, 3600), timeToBoundary)`, where `timeToBoundary` is capped
by *this call's own* `endTime` (start-of-call + quantum). A small outer quantum therefore
imposes an **extra** step-size ceiling tighter than the nominal 3600 s `maxTimeStep` default:
quantum=60 silently forces every internal step to <=60 s, far finer than `RunAdaptive` would
ever pick on its own out here in deep space. Once the outer quantum reaches the nominal 3600 s
ceiling, widening it further changes nothing — 3600 s was already the binding constraint,
exactly the clamp-dominates-cost lesson lab 03 found for `ProjectAdaptive`. The client's 60 s
choice, in other words, quietly buys *more* accuracy than "just like dt=60 fixed" would
suggest, at the ~2x per-call cost measured in Section A.

## Break it yourself

1. **Already above:** the quantum sweep is run for you. Decouple the two clamps: call
   `RunAdaptive` with an explicit `maxTimeStep` well above the outer quantum (e.g.
   `maxTimeStep=86400` while quantum stays 60) and confirm the outer quantum alone still pins
   the internal step to <=60 s regardless of what `maxTimeStep` allows.
2. **On your own:** repeat Section A's throughput bench with the ship parked at Mercury's
   orbit (5.791e10 m) instead of Earth's. `RunAdaptive`'s dynamical-time fraction should start
   taking real sub-60s steps that much closer to the Sun — does its per-call cost catch up to
   (or exceed) fixed dt=1?
3. **On your own:** this probe measures native Release console throughput. If you have a WASM
   build handy, run the same `Stopwatch` benchmark in the browser (Debug vs Release, per this
   repo's own ~100x interpreter note) and see how much of the M5/M19 story was the algorithm
   vs. the runtime.

## See also

- `src/SpaceSails.Core/Simulator.cs` — `Step`, `RunAdaptive`, `DynamicalTime`.
- `src/SpaceSails.Core/TrafficSchedule.cs` — `NpcTimeStep`, `GenerateDepots` (the doc comment
  this lesson's Section B numbers actually measure against).
- `src/SpaceSails.Client/Pages/Map.razor` — the M19 dispatch (`AdaptiveWarpThreshold`,
  `AdaptiveWarpQuantum`) this lesson's Section C reimplements exactly.
- Commits `1bf882b` (M5) and `63b06de` (M19) — the two war stories quoted above, in full,
  in `git log`.
- Lesson 3 (`labs/03-time-step-is-a-lie-you-choose`) — the same clamp-dominates-cost finding,
  independently rediscovered here for `RunAdaptive`'s outer quantum.
