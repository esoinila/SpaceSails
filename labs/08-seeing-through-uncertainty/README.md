# Lab 08 — Seeing through uncertainty

*This lesson is not textbook orbital mechanics — Curtis doesn't cover sensor fusion or track
maintenance — it's the game's own estimation layer (`PathPredictor`, `TrackingStation`,
M-series). The physics underneath the prediction (gravity forward-integration) is standard and
already covered by lessons 01–03; what's new here is the honesty check on the UNCERTAINTY the
game reports around that physics.*

## The idea

A sensor gives you one instant of ground truth: a position and velocity, timestamped. Everything
the game says about where a target is later is a *prediction*, and the honest way to present a
prediction is as a cone that grows with time since the last look —
`PathPredictor.HalfWidthAt`: `w0 + sigma_v*dt + 1/2*budget*dt^2`, where the quadratic term is the
target's plausible *unseen* maneuvering, not sensor noise. This lab pulls one real NPC ship out of
the actual traffic generator (`TrafficSchedule.Generate`, seeded, reading `scenarios/sol.json`'s
traffic section — the same code the live game uses to populate the board), observes it once, and
checks the cone against the ship's TRUE flight — its real, hidden `ManeuverPlan`, run through the
same `Simulator` the game flies NPCs with. Then it layers on telescope tracking
(`TrackedTarget`/`TrackingStation`): the same comparison at three track-quality levels, judged
against the real boarding envelope pirates use (`CaptureRule`). Last, a genuinely stale
observation is fed to `TrackedTargetLedger.TryConfirm` and the short look fails for real, on the
real code path.

## Run it

```bash
dotnet run --project labs/08-seeing-through-uncertainty -c Release
```

## Section 1 — is the ballistic cone honest?

```
Ship under observation: Half Hitch (npc-6), Compute cores, mercury-compute -> earth, personality Evasive.
Departs t=788400 s, plan has 3 burn node(s), estimated arrival t=4248000 s (40.0 days transit).
  - node: t=792000 s (+1.00 h from departure), Accelerate, 2 pulse(s)
  - node: t=1789940 s (+278.21 h from departure), Accelerate, 2 pulse(s)
  - node: t=3556800 s (+769.00 h from departure), Decelerate, 4 pulse(s)

horizon       cone half-width (m)   actual deviation (m)  cone contains truth?  conservatism (x)
30 min        1.067E+007            5.251E+004            yes                   203.1
2.0 h         1.850E+007            3.619E+007            NO                    0.5
6.0 h         8.214E+007            1.810E+008            NO                    0.5
24.0 h        1.138E+009            8.319E+008            yes                   1.4
72.0 h        1.011E+010            2.555E+009            yes                   4.0
```

**Surprise: the cone is not honest at every horizon.** The departure burst fires 1 h after
departure (`BurnLeadSeconds`) — 2 real, discrete +10% pulses, a step change in velocity — and
`PathPredictor`'s cone only budgets for a slow *continuous* acceleration (`ManeuverBudget`,
0.3 m/s²). Right after the real burn (2 h, 6 h) the actual deviation outruns the still-small
budgeted cone (conservatism 0.5x — the truth sits outside it, by 2x at 6 hours). The cone only
catches back up once its own dt² growth swamps the one-time impulsive jump (24 h on). So: honest
on average over a long horizon, genuinely wrong in the hour or two right after a real burn —
because the model is built for a continuously-thrusting evader, not the game's actual
discrete-pulse propulsion mechanic. That gap is exactly what the break-it below exploits.

## Section 2 — telescope tracking: quality shrinks the cone, and that's the whole game for intercepts

`UncertaintyScale(quality) = 1 - 0.7*quality`. Boarding envelope from `CaptureRule`:
`CaptureRadiusMeters` = 5.000E+008 m at under 5000 m/s relative speed. "Hit" means the scaled cone
half-width alone already fits inside the capture radius — a boarding shuttle aimed at the cone's
center is guaranteed to have the real target inside its envelope, no luck required.

```
-- no track (quality 0.0): UncertaintyScale = 1.00 --
horizon       scaled half-width (m)   boarding envelope
30 min        1.067E+007              HIT (guaranteed)
2.0 h         1.850E+007              HIT (guaranteed)
6.0 h         8.214E+007              HIT (guaranteed)
24.0 h        1.138E+009              miss
72.0 h        1.011E+010              miss

-- fresh sweep detect (quality 0.4): UncertaintyScale = 0.72 --
horizon       scaled half-width (m)   boarding envelope
30 min        7.680E+006              HIT (guaranteed)
2.0 h         1.332E+007              HIT (guaranteed)
6.0 h         5.914E+007              HIT (guaranteed)
24.0 h        8.196E+008              miss
72.0 h        7.282E+009              miss

-- perfect reconfirm (quality 1.0): UncertaintyScale = 0.30 --
horizon       scaled half-width (m)   boarding envelope
30 min        3.200E+006              HIT (guaranteed)
2.0 h         5.549E+006              HIT (guaranteed)
6.0 h         2.464E+007              HIT (guaranteed)
24.0 h        3.415E+008              HIT (guaranteed)
72.0 h        3.034E+009              miss
```

**Punchline:** at quality 0, every horizon past a day misses the envelope outright. A fresh sweep
detect (quality 0.4, what every new telescope contact starts at) buys back only the short
horizons. Only a well-tracked, recently reconfirmed target (quality near 1.0) keeps the
guaranteed-hit window open out to 24 hours. Track quality is not flavor text — it is the
difference between an intercept a pirate can plan and one they can only hope for.

**Caveat that matters:** "HIT (guaranteed)" is only as good as Section 1's promise that the cone
contains the truth — and Section 1 found that promise genuinely broken at the 2 h/6 h rows (right
after the real burn). Every "HIT" printed above at those two horizons is a **false guarantee** for
this ship: the target is actually outside its own cone, let alone inside the boarding envelope. A
small cone is only trustworthy where the underlying cone was honest to begin with.

## Break it — feed a stale observation, watch the short look fail for real

```
Staleness: 6 h since the one and only observation (staleness horizon for QUALITY decay is 5 days — this is not even stale enough to cost quality, and it STILL breaks the re-acquire).
Ballistic (no-burn) predicted position vs the ship's TRUE (post-burn) position: deviation = 1.810E+008 m; cone half-width at that time = 8.214E+007 m.
TrackedTargetLedger.TryConfirm(...) at the true position: FAILED.
```

It fails because `TryConfirm` dead-reckons ballistically (`hypothesis: null`) from the last
observation, exactly like Section 1 — and Section 1 already showed that a real discrete burn
outruns the small-continuous-acceleration cone within hours. A short, cheap re-acquire look
genuinely cannot find a target that maneuvered while nobody was watching; only a fresh full sweep
(paying the real cost) re-establishes the lock. **This staleness (6 hours) is nowhere near the
5-day quality-decay horizon** — `TrackedTargetLedger` would still call this contact "quality 0.4,
undecayed," and it is already wrong. Track-quality decay is not a courtesy discount for laziness:
an unwatched target that burns can be flatly, verifiably gone from where the ledger thinks it is,
well before the staleness-horizon clock would have warned anyone.

## Break it yourself

1. **Already above:** the 6-hour stale re-acquire failure. Try it at 2 hours instead (also shown
   failing in Section 1's table) — does `TryConfirm` fail even faster, right at the edge of the
   burn window?
2. **On your own:** pick a different traffic index (change `traffic[6]` to another short-haul
   index, e.g. `traffic[7]` or `traffic[8]`) — a `Fast` or `Economical` personality ship burns
   differently (one lump burst vs. the `Evasive` ship's split burst). Does the cone-under-coverage
   window right after the burn get wider, narrower, or disappear?
3. **On your own:** `ManeuverBudget` is a single constant (`NpcShip.DefaultManeuverBudget = 0.3`)
   for every NPC regardless of personality. Recompute Section 1 with a larger assumed budget (say
   3.0) passed to `PathPredictor.Predict` — how much bigger does the cone need to be to actually
   contain the post-burn truth at 2 h and 6 h, and what does that cost in false-negative
   conservatism at 72 h?

## See also

- `src/SpaceSails.Core/PathPredictor.cs` — `PredictedPath.HalfWidthAt`, the cone growth formula
  this whole lesson probes.
- `src/SpaceSails.Core/TrackingStation.cs` — `TrackedTarget.UncertaintyScale`,
  `TrackedTargetLedger.TryConfirm` — telescope tracking and the real re-acquire code path.
- `src/SpaceSails.Core/CaptureRule.cs` — the boarding envelope this lesson's "hit/miss" column is
  judged against.
- `src/SpaceSails.Core/TrafficSchedule.cs` — where the real NPC ship (and its hidden plan) this
  lesson observes comes from.
