# Wolf-aim 🐺 — fire control against hunters

*How the gun deck aims at hired muscle, why it needed its own physics, and where its limits honestly are. Shipped 2026-07-06 (the wolf-aim PR); companion to the AIM/SOLVE/FIRE doctrine (PR #82) and the Debt Collector encounter loop (PR #92).*

## The problem: two kinds of future

Every shot in SpaceSails is a boundary value problem: *leave the muzzle now, be at that point at* `t_hit`. `FireControl.Solve` answers it with the shooting method — Newton iteration over launch bearing and charge, every residual a real flight through the deterministic `Simulator`. That half never cared what the target is, and it still doesn't: **the slug always flies real gravity.**

The other half is picking the aim point — *where will the target be at* `t_hit`? For freighters, pods and depots the answer is `PathPredictor`: gravity is public knowledge, so dead-reckon the observation through the simulator and only the target's unknown burns widen the cone.

Hunters break that answer twice:

1. **They feel no gravity.** By design (owner's call): a collector is dumb, relentless, thrust-only. Even a *coasting* hunter (peeled after a warning shot, sun-blinded, still fitting out) drifts in a straight line the gravity model bends away — ~1,400 km of phantom curvature over a 6 h flight at 1 AU.
2. **They thrust, always, toward you.** `EncounterRule.HunterAccelMps2 = 0.5` every 60 s quantum. The gravity model drops that entirely. Aim error grows as ½·a·τ²: ~90 km at the 600 s minimum aim (inside the 500 km hit radius — which is why point-blank shots seemed to work), crossing the hit radius at **~24 minutes of flight**, and ~13,000 km at 2 hours. Every shot past a knife-fight was a structural miss.

## The fix: the pursuit law is public knowledge too

`EncounterRule.PredictHunterPath(hunter, playerPath, horizon)` predicts a hunter by **replaying the live integrator itself** — `AdvanceHunter`, same 60 s quanta, same activation / peel / sun-blind gates — steered against the player's *plotted course* instead of the live ship. To our own gun deck, the pursuit law is knowable the way gravity is; the only honest unknown is whether the player keeps to the plot.

Properties worth remembering:

- **Exact, not sampled.** The hunter's true track is itself piecewise-linear at 60 s quanta (Euler positions), so undecimated knots reproduce the game's motion bit-for-bit. Long horizons decimate *recorded* knots (integration never coarsens); the final knot always lands exactly on `t_hit`.
- **Light, by construction.** A 2 h prediction is ~120 additions. Hunters stay exactly as cheap as they fly — no gravity solves, no autopilot. (Don't "fix" them onto orbital physics; the thrust-only chase is the design.)
- **A predicted catch or break-off freezes the track** — a spent contact holds still, same as live.
- **Your own burns bend your own solution.** The plotted course includes planned X-Pilot vector burns, so the prediction already accounts for them — but burn *off* the plot mid-flight and the collector chases the real you, not the plan. That, not sensor noise, is the real dispersion on a hunter shot; the cone keeps maneuver budgets at 0 (pod-thin) and says so in the intercept box.

Client-side, one fork — `PredictInterestPath` in `Map.razor` — routes hunter vs. freighter targets under the same `PredictedPath` contract, so **SOLVE, the window scan, the orrery backdrop and the dispersion cone are all target-agnostic.** The war-room intercept clock flies the same pursuit path and answers in *his* catch envelope (3e8 m — when does he run **us** down), not our boarding radius. While berthed at a dry-dock the player path rides the dock's rails, since the plot shows a gravity coast the clamps will never allow.

## The quantum trail: making the wolf predictable at warp

First live long-shot missed, and the root cause was not the aim: at warp, a frame spans hundreds of sim-seconds, and the hunter catch-up used to steer **every** quantum toward the single frame-end player position. Hunter paths therefore depended on frame cadence — not deterministic in sim time, and unpredictable *in principle* for any model.

The OnTick loop now records the ship's actual integrated positions through each frame (`_pursuitTrail`, pursuit cadence, only while hunters fly) and each pursuit quantum steers at the position *at its own time*. Residual frame dependence drops from ~10⁴ km at 10000× to km-scale interpolation sag.

**Abort switch:** `SteerHuntersByQuantumTrail` (const, `Map.razor`). Set `false` to restore the old frame-end steering exactly — one flag, no other code path touched. Its commit is also independently revertible.

## How to fight with it (the regimes)

| Shot | Regime | Verdict |
|------|--------|---------|
| Warning shot | Any range inside 200,000 km | Unaffected by all of this — fires at the current position, erodes nerve, peels the wolf. The intended counter. |
| Inbound leg, aim minutes–hours | One thrust regime, no flybys mid-flight | **The precision regime.** Core-tested to land inside the 500 km hit radius at a 2 h flight. Hole the sail, void the contract for good. |
| Turnaround lob, aim days | 🔭 SCAN finds windows the straight-line check can't see (it once solved a shot leading the mark by 167° — firing *away* from the wolf, into his return) | Legitimate but honest gamble: the cone grows ~100 km per 1,000 s, and each close flyby between launch and impact amplifies any residual. The deck shows you the odds; believe them. |

A hunter blasting *away* faster than the muzzle (post-flyby, 10× muzzle speed) is honestly unhittable at short aims — NO SHOT is the physics, not a bug. Deterrence doesn't need the hit anyway: with collectors, the idea is to make them doubt.

## Tests

`tests/SpaceSails.Core.Tests/HunterFireControlTests.cs`:

- **Exact replay** — the predictor must reproduce a manual `AdvanceHunter` iteration bit-for-bit (same integrator, not a lookalike).
- **The money test** — solve against the pursuit aim at 2 h, fly the slug through real gravity, demand it passes inside the hit radius of the game-integrated thrusting hunter — and demand the gravity dead-reckon is shown >10 hit-radii off.
- **Peeled coast** — a coasting hunter predicts straight where the gravity model bends past the hit radius.

## Future work

- **Threaded solver** — SOLVE and SCAN grind the deck synchronously in WASM (seconds to minutes on long windows). Moving fire control to a worker thread is planned as its own design doc after this merges.
- The "Rounds in flight — planned impact" line reads the latest *computed plan's* ETA, not the flying round's; a per-round impact clock would be more honest.
