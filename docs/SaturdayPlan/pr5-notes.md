# PR-5 notes — for the docs lane (PR-2/PR-8)

*What changed, for whoever writes `docs/features/*` and updates `docs/user-guide.md` later. PR-5
did not touch `docs/**` beyond this handoff.*

## The rule (vision par. 10)

Trading now requires one of two things, enforced by `src/SpaceSails.Core/CommerceRule.cs`:

- **Same-orbit trading**: you and the counterpart are bound to the same body (the classic bus
  stop — parked at a depot, station, or haven).
- **Course-matched trading**: absent that, you're within 5e8 m and under 2 km/s relative speed of
  a moving partner — a looser, faster envelope than the boarding shuttle's (`CaptureRule`), because
  cargo drones are cooperative: the partner isn't trying to shake you off.

Once trade is allowed, a cargo-drone transfer takes real time — `CommerceRule.DroneTransferSeconds`
— scaling with relative speed, distance, and cargo units (20 s/unit base, gentler penalties than
boarding's `RequiredSecondsFor`; see the constants' doc comments for the exact reasoning). Progress
accrues in real time like the boarding shuttle (warp doesn't fast-forward it) and is lost — no
partial credit — if the envelope breaks mid-transfer (the partner burns away, you drift out of
orbit, etc.).

## New Core file: `src/SpaceSails.Core/CommerceRule.cs`

- `CanTrade(player, partnerPosition, partnerVelocity, playerOrbitBodyId, partnerOrbitBodyId)` — the
  truth table above.
- `DroneTransferSeconds(relativeSpeed, distance, units)` — pure, deterministic transfer time.
- `ContactsAt(ephemeris, ships, simTime, bodyId)` — the "what else orbits here" enumeration: any
  depot at the body, every station/moon/haven child body, and any NPC ship caught inside the
  body's Hill sphere (or a fixed ~2e7 m "dockyard" radius for stations/havens, which carry no real
  gravity — `mu: 0` in scenario data, so the Hill-sphere formula degenerates to zero for them).
  Each contact is tagged with an `ActionKind` flag set: `Trade` (depots, stations),
  `Trade | Fence` (havens), `Board` (ships) — tags only; the actions themselves live where they
  already lived (dock panel selling, the existing boarding-shuttle flow). 14 new xUnit tests in
  `tests/SpaceSails.Core.Tests/CommerceRuleTests.cs`; solution total 70 → 84.

## New UI: `src/SpaceSails.Client/Pages/Stations/LocalSpace.razor`

A "Local space" card (right-middle of the map HUD, mirroring the tracking post's left-middle
slot) listing everything `ContactsAt` finds for whatever body the ship is currently orbiting (or,
absent a bind, the nearest body — so the panel still shows something while just cruising close to
a bus stop). Each row: an icon by kind (🛰 depot, 🏭 station, 🌙 moon, 🏴 haven, 🚀 ship), distance,
action badges, and — for anything tagged `Trade` — a **Trade** button gated by `CommerceRule.CanTrade`.
Trading sells the whole current cargo hold at existing `CargoMarket`/dock prices, over
`DroneTransferSeconds`, with a striped progress bar ("Drones ferrying — NN%").

Opens automatically the moment the ship enters orbit (rising edge of `OrbitRule.IsBound`), and via
a new toolbar toggle ("Local 🛰") otherwise. Also toggleable manually at any time.

## Map.razor changes beyond the SATURDAY-ANCHOR lines

- `OnTick`: two new calls next to `UpdateDockStatus()`/`UpdateCapture()` — `UpdateOrbitedBody()`
  (ground-truth "what body am I bound to right now", cached alongside its position/Hill radius) and
  `UpdateLocalTrade(dtRealSeconds)` (the real-time progress accrual described above).
- `DrawNpcs()`: contacts co-orbiting the ship's current body (same check `ContactsAt` uses) get a
  subtle extra ring on the map itself, not just in the Local Space panel — the proximity
  affordance made visible where the owner asked for it ("orbiting a planet, you should see what
  else orbits there").

## Suggested `docs/features/` follow-ups

- A short "orbital commerce" page: the same-orbit vs. course-matched rule, the drone-transfer
  progress bar, and what the Local Space panel's icons/badges mean.
- A callout in the map & warp / dock & economy pages cross-linking to it, since selling cargo now
  has two paths (dock panel at a port zone, Local Space anywhere in orbit/course-match).
