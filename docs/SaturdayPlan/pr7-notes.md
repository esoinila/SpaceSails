# PR-7 notes — for the docs lane (PR-2/PR-8)

*What changed, for whoever writes `docs/features/*` and updates `docs/user-guide.md` later. PR-7
did not touch `docs/**` beyond this handoff — that lane belongs to PR-2/PR-8.*

## New Core file: `src/SpaceSails.Core/EncounterRule.cs`

Pure, deterministic math for the gun deck (vision par. 18). Nothing here mutates or holds live
state — Map.razor owns all of that (which ship was warned, which is bribed, the hunter roster,
the heat gauge), the same way `NpcState.Boarded` already tracks capture.

- **Weapon envelope**: `WeaponRangeMeters = 2e8` — shorter than `CaptureRule.CaptureRadiusMeters`
  (5e8): guns speak before shuttles fly. `InWeaponRange(player, target)`.
- **Compliance**: `ComplianceOf(NpcShip, playerHeat)` hashes the ship's id (FNV-1a 64, not
  `string.GetHashCode` — that's randomized per process) into one of three states:
  - `NothingToComply` — pods; no crew, nothing to warn.
  - `Compliant` (~75% baseline) — heaves to under a warning shot.
  - `Stubborn` (~25% baseline, `BaseStubbornFraction`) — ignores the shot, calls its own muscle.
    The stubborn fraction rises slightly with heat (`StubbornFractionPerHeatLevel`, capped at
    `MaxStubbornFraction`) — reputation precedes the player.
  - A compliant (warned) or bribed target boards at `ComplianceBoardingFactor` (0.5×) the usual
    `CaptureRule.RequiredSecondsFor` time.
- **Threats/hail**: `ThreatOutcome(npc, compliance)` returns one of 2–3 canned pirate-flavored
  lines (surrender vs. defiance), chosen deterministically by the same id hash.
- **Bribery**: `BribePrice(npc) = cargoUnits × CargoMarket.UnitValue(cargoClass) × 0.35`. A bribed
  ship becomes compliant and generates **no heat** when robbed — an inside job, nobody calls the
  cavalry.
- **Heat**: `HeatState { Level 0..3, RaisedAtSimTime }`. `RaiseHeat` clamps to `MaxHeatLevel`.
  `DecayHeat` removes one level per `HeatDecayDays` (20), `HavenDecayMultiplier` (4×) faster while
  the caller reports `atHavenOrbit: true`.
- **Hunters**: `HunterState` — one spawned per heat-raising robbery (`SpawnHunter`), fitting out
  `HunterFittingOutDays` (5) at the `NearestPolicedBody` (a planet inside `PolicedThresholdMeters`
  — the same 4e11 m long-haul split `TrafficSchedule` uses internally — that isn't a haven).
  `AdvanceHunter` is a dumb, thrust-limited (`HunterAccelMps2 = 0.5`) pursuit toward the player's
  *current* position, integrated in `HunterStepSeconds` (60 s) quanta. Catches the player
  (`CatchRadiusMeters = 3e8`, `CatchRelativeSpeedMetersPerSecond = 3000`) — Map.razor's
  consequence is all cargo seized + `CatchFineCredits` (500 cr) fine, the adrift-flow shape reused
  for a robbery gone wrong. `ApplyBreakOff` ends pursuit after `BreakOffHiddenDays` (2) of
  continuous haven orbit.

Tests: `tests/SpaceSails.Core.Tests/EncounterRuleTests.cs` (20 new — 90 total in
`SpaceSails.Core.Tests`, up from 70).

## New UI: `src/SpaceSails.Client/Pages/Stations/WarRoom.razor` (+ `.razor.css`)

Self-contained station component, same shape as `TrackingPost.razor`: Map.razor feeds it thin
DTOs (`WarRoom.Contact` = `NpcShip` + live `ShipState` + warned/bribed flags,
`WarRoom.HunterContact` = id/callsign/state) and reads back which button the player pressed via
`OnWarningShot`/`OnBribe` callbacks. All compliance/threat/bribe-price math is called live from
`EncounterRule` inside the component — Map.razor never precomputes it.

- Pure SVG top-down tactical circle centered on the player (~1e9 m radius), with a weapon-range
  ring around the player and a catch-radius ring around any hunter contact.
- Per-contact row: hail (shows the canned threat line inline), warning shot (enabled only in
  weapon range, disabled for pods), bribe (shows/charges the price, disabled once bribed or if
  credits are short), status badge (🏳 compliant / ⚔ stubborn / 🤝 bribed / pod).
- Header: heat gauge (0–3 flames) + a "cooling at 1 level / Nd" line (4× at a haven) + nearest
  hunter's bearing/distance when any are hunting.
- Positioned right-middle in the map HUD (`.war-room-card`) — traffic board owns top-right, dock
  and scope own bottom-right, tracking post owns left-middle.

## `Map.razor` wiring

Anchors: toolbar button ("Guns ⚔"), panel include, one field block, one method block — all
appended after the existing PR-3/PR-4 anchor content, nothing else in those regions touched.

Beyond the anchors (per the plan's "minimal and isolated" allowance):

- `NpcState` gained two fields: `WarningShotFired`, `Bribed`.
- `UpdateCapture`: boarding time is multiplied by `EncounterRule.ComplianceBoardingFactor` when
  `IsCompliantBoarding(npc)` (bribed, or warned-and-actually-compliant).
- `Board(npc)`: now calls `RaiseHeatFromRobbery(npc)` after cargo transfer — the actual heat/hunter
  trigger. A warning shot itself never raises heat; it only narrates the ship's reaction. Robbing
  it is what calls the muscle.
- `OnTick`: one new call, `UpdateEncounters()`, right after `UpdateCapture` — decays heat, steps
  hunters (sim-time based, so it scales with warp like NPC stepping), and resolves catches/
  break-offs.
- Render pass: `DrawHunters()` called alongside `DrawNpcs()` (single-player branch only) — red
  🐺-labeled marker, catch-radius ring when within 3× weapon range.
- Hunters are **not** `NpcShip`/`TrafficSchedule` entries — they live entirely in
  `List<HunterState> _hunters` on `Map`, spawned/advanced/removed by the new methods at the
  methods anchor.

## Suggested `docs/features/` follow-ups

- A "gun deck" feature page: weapon range vs. boarding range, the compliance/bribery choice, what
  heat means and how it decays, and the haven escape loop (screenshot of the war-room card plus
  the heat gauge).
- A callout in the boarding-run page cross-referencing `ComplianceBoardingFactor` — the fastest
  boarding in the game is now a bribed or warned-compliant target, not a perfect physical
  rendezvous.
