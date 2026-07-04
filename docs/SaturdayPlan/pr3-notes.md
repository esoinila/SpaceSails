# PR-3 notes — for the docs lane (PR-2/PR-8)

*What changed, for whoever writes `docs/features/*` and updates `docs/user-guide.md` later. PR-3
did not touch `docs/**` itself (that lane belongs to PR-2/PR-8) — this is the handoff.*

## Scenario schema additions (`src/SpaceSails.Contracts/Scenario.cs`)

All new sections are **optional** — existing scenario files keep working unchanged.

- `BodyDefinition.kind`: `"planet"` (default) | `"moon"` | `"station"`. Stations are lightweight
  orbital POIs (compute farms, factories, trading posts) — they have `mu: 0` (massless markers)
  and a small `bodyRadiusM`.
- `BodyDefinition.haven`: `true` marks a pirate haven — trade & repair, no questions asked, per
  the "scum and villainy work the outer reaches" worldbuilding (vision par. 8).
- `ScenarioDefinition.traffic` (`TrafficDefinition`): optional data-driven traffic —
  - `routes`: `[{ from, to, cargo, weight?, publishesTimetable? }]`. `weight` (default 1) is a
    relative pick weight; `publishesTimetable` (default `true`) — `false` means the route's ships
    never appear on the traffic board (still simulated, still visible to sensors in range).
  - `podLaunchers`: `[{ body, cargo }]` — mass-driver launch sites for ballistic pods.
  - When a scenario has no `traffic` section (e.g. `scenarios/wheel.json`), `TrafficSchedule`
    falls back to its original hardcoded Sol tables — byte-identical to pre-PR-3 behavior.

## New Sol bodies (`scenarios/sol.json`, `scenarios/sol-eu.json`)

Moons (real orbital data, SI units): **Titan, Enceladus** (Saturn); **Europa, Ganymede,
Callisto** (Jupiter). Luna already existed and is now tagged `"kind": "moon"`.

Stations: **Mercury Compute Farms** (low Mercury orbit — the inner system's AI-compute capital,
worldbuilding notes §3), **Highport Satellite Works** (Earth LEO), **Ringside Exchange** (Saturn
orbit near Titan — an outer trading post, also a haven).

Havens (`"haven": true`): **Enceladus** (a small, quiet moon) and **Ringside Exchange**.

## Traffic content (`sol.json`/`sol-eu.json`'s new `traffic` section)

Reproduces the original hardcoded routes (Saturn/Jupiter→Mars/Earth He3, Mars↔Earth,
Venus→Earth), plus:

- **Titan→Mars, Titan→Earth** He3 with `publishesTimetable: false` — the secretive haulers
  (worldbuilding notes §4): moved off Saturn itself, onto the moon, per "pirates didn't operate
  next to Spain."
- **Mercury Compute Farms ↔ Earth**, both directions — the compute-core trade in and out of the
  glare country.
- Pod launchers: **Luna** (unchanged) and **Mercury Compute Farms**.

## NPC-facing changes (`src/SpaceSails.Core/TrafficSchedule.cs`, `NpcShip`)

- `NpcShip.PublishesTimetable` (default `true`). This is the flag PR-4 (tracking post) and PR-6
  (dark space web) hook into: a `false` ship is real and observable, just absent from the public
  board.
- `GenerateDepots` now also places a depot at every `station` and `haven` body, not just planets.
  NPC count went from 19 (8 traffic + 3 pods + 8 depots) to 23 in the default single-player load —
  well inside the "don't explode the NPC count" budget.

## UI (`src/SpaceSails.Client/Pages/Map.razor`)

- Traffic board panel: ships with `PublishesTimetable == false` are filtered out of the table, and
  a footer line ("N ships operating off the books") appears when any exist.
- Map body rendering: `station` bodies draw as a small, distinctly colored (synthetic teal) blip
  regardless of id; `haven` bodies get a subtle crimson tint on their marker and label. Worth a
  legend/callout whenever the map & warp feature page gets written.

## Suggested `docs/features/` follow-ups

- A short "world & traffic" page explaining the schema (routes/podLaunchers/kind/haven) for
  scenario authors, plus a callout in the map/traffic-board feature pages about off-book ships and
  station/haven markers.
