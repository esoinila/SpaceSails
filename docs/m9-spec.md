# M9 — Multiplayer 🛰️: implementation work package

*Companion to `SpaceSails_plan_detailed.md` §8 (M9). The biggest lift in the plan; scoped hard.*

## Goal (acceptance criteria, verbatim from the plan)

- SignalR hub: join session, authoritative tick, interest-managed state broadcast, command
  channel (maneuver nodes + live pulses), server-side observation filtering (hidden info
  stays hidden).
- Client reconciliation (snap + re-project). 2–8 players, pirates in a shared traffic system
  (PvE first; PvP piracy = stretch).
- **Accept:** two browsers hunt in one system; a ship maneuvering outside your sensor range
  does *not* update on your map.

## Scope decisions (senior — these are final for M9)

- **One shared session per server** (the "system"). Join = enter it. 2–8 players.
- **Shared sim time, shared warp**: the server's warp is the **minimum of all connected
  players' requested warps** (KSP rule: everyone must agree to skip time). Pause = warp 0.
- **Hidden info is enforced by omission**: each broadcast is computed *per player* through
  the same `SensorModel` the client uses. Unobserved ships are simply not in your packet.
  Own ship is always full-state. (This is the accept-test crux.)
- **Deferred to post-M9** (noted in PR): server-side capture/boarding/economy (PvE capture
  remains a single-player feature this milestone), PvP piracy, NPC charge, arc pings,
  persistence/reconnect-with-state, multiple sessions.

## Server (senior-implemented — the authoritative host)

- `SessionHost` (singleton `BackgroundService`):
  - Owns ephemeris + `PlasmaEnvironment` (scenario fixed at startup via config; default
    `sol`), NPC states (from `TrafficSchedule` seed 42/43 — same as single player), and a
    `PlayerShip` map (connectionId → callsign, `ShipState`, reaction mass, requested warp,
    `ManeuverPlan`, pulse/vent cooldown bookkeeping).
  - Tick loop: `PeriodicTimer(100 ms)`; `simAccumulator += elapsedReal × warp`; steps player
    ships at dt=1 with their plans (server is native — 8 players × 10⁴ steps/tick is cheap)
    and NPCs at `TrafficSchedule.NpcTimeStep`, exactly the client's integration.
  - Broadcast loop (5 Hz): for each player, build a `WorldUpdate` containing sim time,
    effective warp, own full ship state + mass, and **only** the contacts (NPCs *and* other
    players) that `SensorModel.Default.TryObserve` yields from their position — as
    `ContactDto` (id, callsign, kind, position, velocity, charge, cargo class for NPCs).
    Send via the hub to that connection only.
- `GameHub : Hub` (`/hubs/game`), thin — all state lives in `SessionHost`:
  - `Join(callsign)` → `JoinResult` (playerId, scenario slug, sim time, spawn state).
    Spawn = docked at Earth (+5e9 co-moving, like single player).
  - `Pulse(bool accelerate)` — server validates 1 s sim cooldown + reaction mass, applies
    `velocity ×= factor` to the authoritative ship. Live pulses invalidate that player's
    pending plan nodes (same rule as single player).
  - `SetPlan(PlanNodeDto[])` — replaces the player's plan (times must be future; ≤ 20 nodes,
    pulses 1–20; server clamps). Mass is debited as nodes fire (floor 0 = nodes stop firing).
  - `Vent()` — halves charge, 1 s cooldown, EU scenarios only.
  - `RequestWarp(int)` — clamps to {0,1,10,100,1000,10000}; session warp = min over players.
  - Disconnect removes the player (their warp vote too).
- DTOs live in `SpaceSails.Contracts` (`WorldUpdateDto`, `ContactDto`, `PlanNodeDto`,
  `JoinResultDto`) — plain records, System.Text.Json-friendly.
- Tests (server): hub contract via `WebApplicationFactory` + SignalR client: join yields
  spawn; pulse changes velocity and burns mass; **filtering test — two joined players far
  apart do not appear in each other's updates; moved within range, they do** (drive via a
  test hook on `SessionHost` or high warp).

## Client work (delegated — extend Map.razor)

- **MP entry**: `/map?scenario=<slug>&mp=1&callsign=<name>`. When `mp=1`:
  - Add `Microsoft.AspNetCore.SignalR.Client` to the Client csproj. Connect to `/hubs/game`
    (relative `Navigation.ToAbsoluteUri`), call `Join`, store playerId.
  - The local fixed-dt loop keeps running for smooth rendering (prediction), **but**:
    live keyboard pulses / vent send hub commands instead of (not in addition to) local
    mutation; plan edits call `SetPlan` after local rebuild; warp buttons call `RequestWarp`
    (display shows the *server's* effective warp from updates).
  - On `WorldUpdate` (hub callback `"Update"`): **snap** own ship to the authoritative state
    (position/velocity/simtime/charge/mass) and `ReprojectTrajectory()` if it moved > 1e6 m
    from the local prediction; replace the NPC/contact picture: contacts in the packet are
    the *only* things drawn (map NPC markers, other players as distinct green markers with
    callsign) — clear `CurrentlyObserved` for absent ones (they keep their last-seen marker,
    which is exactly the single-player semantics).
  - Traffic board in MP: rows built from the *public departures board* (`GET /api/traffic`,
    same seed — it's identical data) + live contact status. Prediction cone still works off
    the latest contact observation (client-side, unchanged).
  - Single-player (`mp` absent) must be byte-identical to M8 behavior.
  - Capture/dock/tutorial UI: hidden in MP mode this milestone (server doesn't arbitrate
    them yet) — hide the buttons/cards behind `!_mp`.
- HUD additions in MP: "Crew: N aboard" (player count comes in updates), connection state,
  and the session warp display.

## Constraints
- §9 as always. Core untouched except where the spec above says. No new JS (SignalR client
  is a NuGet package, not JS interop). Determinism law: the server's per-tick stepping uses
  the same Core Step — no wall-clock inside the sim itself.

## Definition of done
- Build 0 warnings; all tests green (37 existing + new server contract tests).
- **The accept run, headless with two browsers**: A and B join; both see sim time advance
  in lockstep; B watches its contact list while A is far away (A absent), A burns toward B
  (still absent while out of range), A enters sensor range (A appears with callsign);
  A's live maneuvers update on B's map only while in range. Screenshots.
- Warp rule observable: B requests 10000×, A stays at 1× ⇒ session runs at 1×.
