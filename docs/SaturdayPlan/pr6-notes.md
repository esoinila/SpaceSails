# PR-6 notes — for the docs lane (PR-2/PR-8) and PR-7 (gun deck)

*What changed, for whoever writes `docs/features/*` later, and for PR-7 which is expected to
consume the "aware" flag this PR introduces.*

## New Core (`src/SpaceSails.Core/IntelMarket.cs`, `ActiveSensors.cs`)

- `RouteIntel` / `IntelLedger`: a player's bought route tips, keyed by ship id.
  `IsFresh(simTime)` gates whether an entry still counts; `IntelLedger.Knows(shipId, simTime)` is
  the one predicate the traffic board needs.
- `IntelMarket.BuyPrice(totalCargoValue, distanceFromEarthMeters)`:
  `(300 + cargoValue * 0.4) * DistanceFactor(distance)`, where
  `DistanceFactor(d) = clamp(1.5e12 / (1e11 + d), 0.3, 3)`. Farther from Earth = cheaper — the
  outer reaches trade in secrets as a matter of course.
- `IntelMarket.SellPrice(quality, totalCargoValue) = quality * cargoValue * 0.3`, gated by
  `CanSellTrack(quality) => quality >= 0.5`. Selling a track never removes it from your own
  tracking-post ledger — information copies.
- `IntelMarket.CanTradeIntelAt(body, distanceFromSunMeters)`: true at any haven, or any `Station`
  body farther from the sun than `FarTradingPostThresholdMeters` (4e11 m, between Mars and
  Jupiter — same split `TrafficSchedule` uses for long-haul routes). Ordinary planets never trade
  intel, haven flag or not.
- `ActiveSensors.LaserRange(targetId, playerPos, targetPos, targetVel, simTime)`: returns an exact,
  zero-age `Observation` plus a `PingEvent(TargetId, SourcePosition, SimTime)`. No range limit of
  its own — the UI only offers it against already-tracked targets. Applying the `PingEvent` (the
  "aware" flag) is left to the caller; **PR-7 (weapons/heat) should consume this** for real combat
  consequences. For now it's a UI-only warning badge (see below).
- `ActiveSensors.CanTightBeam(playerPos, targetPos, maxRangeMeters = 5e10)`: range gate for hailing
  a specific contact without broadcasting.

## Public hook added to `TrackingPost.razor` (not `TrackingStation.cs` — that file is off-limits)

Three small additions, all thin wrappers over the existing `TrackedTargetLedger`:

- `public IReadOnlyCollection<TrackedTarget> Entries` — read-only view of the ledger, so the dark
  web can list sellable tracks without reaching into TrackingPost's private state.
- `public bool ApplyObservation(Observation observation)` — injects an observation via the *same*
  `_ledger.Add()` path a passive sweep hit uses. A laser-ranged perfect fix resets the elapsed-time
  term `PathPredictor` grows its cone from, so "the cone tightens" falls out of reusing existing
  machinery rather than needing new Core logic.
- `public void MarkAware(string shipId)` — a UI-only `HashSet<string>` flag rendered as `aware ⚠`
  next to the callsign in the tracked-targets table. Core's ledger doesn't know about this; it's
  bookkeeping for PR-7 to eventually wire into real behavior (hired muscle, evasive routing, etc).

## New UI (`src/SpaceSails.Client/Pages/Stations/DarkWeb.razor` + `.razor.css`)

Self-contained station component, "Comms & intel 🕸": owns its own `IntelLedger` (mirrors
`TrackingPost` owning its own `TrackedTargetLedger`). Sections: dark web (buy off-book ships'
routes / sell your own tracks ≥ 50% quality, gated by `CanTrade`), tight-beam (hail a tracked
contact, get an honest destination or "no flight plan filed"), laser ranging (perfect fix + "lit
up ⚠" warning). Credits flow through a standard `@bind-Credits` two-way binding into Map.razor's
existing `_credits` field — no new credits plumbing.

## `Map.razor` wiring

- SATURDAY-ANCHOR lines: one toolbar button (`Web 🕸`), one panel include, two fields
  (`_showDarkWeb`, `_darkWeb`), and a block of new private methods (gate/pricing helpers, the
  `MarketShip`/`TrackedShipInfo` projections, `LaserRangeTarget`) appended after the methods
  anchor — nothing existing was edited there.
- **The one functional region touched beyond anchors**, as scoped by the PR: the traffic board's
  filter predicate (now `n.Ship.PublishesTimetable || _darkWeb.KnowsRoute(n.Ship.Id)`) and the
  callsign cell, which now shows a `🕸 stale in Nd` badge for bought-intel rows. `OffBooksCount`
  and everything else in the traffic board panel is untouched.
- "Where you can trade" is derived from existing state, not a new persistent field: docked
  (`_docked`/`_dockBodyId`, market bodies only) or bound-orbiting `_nearestBody` via
  `OrbitRule.IsBound`, checked against `IntelMarket.CanTradeIntelAt`. This means dark-web trading
  currently requires either being docked at Earth/Mars/Venus (never satisfies `CanTradeIntelAt`
  today, since none of those are havens or far stations) or being bound in orbit around a haven or
  a far station. **Follow-up for PR-5/commerce lane or a later pass**: docking itself is currently
  hardcoded to `MarketBodies = ["earth", "mars", "venus"]` in Map.razor — havens/far stations have
  no dock flow yet, only the orbit-bound path works for them today. Worth revisiting once PR-5's
  local-space panel lands.

## Suggested `docs/features/` follow-ups

A "dark space web" feature page covering: where to find a haven/far station, what buying/selling
intel does to the traffic board, and the tight-beam/laser-ranging tradeoff (passive scanning stays
quiet; both active tools give your position away).
