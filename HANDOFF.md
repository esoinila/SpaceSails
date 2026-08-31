# #954 — flickering on every orbit — HANDOFF

## Status: fix implemented, both halves proven RED by revert, Core+Client NearestRule/flicker gates green. Full suites next.

## What #954 actually was, on this branch

The issue is OPEN but a first fix (#966, `ed171e1`) is already in the base branch: `NearestRule.Unseats`
gave the nearest slot a 3% hysteresis band and `UpdateNearestNeighbourhood` learned to say
"Mars › The Rusty Roadstead". That fixed the owner's screenshot (0.16 AU off Mars). The issue is open
because PRs to `our-own-ship-has-compartments` do not auto-close — not because nothing was done.

**But the flicker was still there, everywhere the ship actually flies.** The 3% band is measured along the
SIGHTLINE, so it shrinks as the ship closes: at 0.16 AU 3% of the range is 700,000 km and Mars's whole
family fits inside it; at 100,000 km it is 3,000 km and Phobos (9,376 km rail) and the Roadstead
(12,000 km rail) start trading places again — every orbit, exactly as reported, just nearer in.

Measured on the real scenario by driving `UpdateNearestBody` at a fixed range while the family turns
(5 orbits of the planet's slowest satellite, 2,000 samples), BEFORE this change:

| post | slot changes hands | line changes its words |
|---|---|---|
| earth @ 100,000 km | **1,744** | 1,744 |
| neptune @ 100,000 km | 144 | 144 |
| saturn @ 300,000 km | 136 | 136 |
| jupiter @ 1,000,000 km | 76 | 69 |
| uranus @ 100,000 km | 29 | 29 |
| mars @ 100,000 km | 16 | 17 |
| earth @ 10,000,000 km | 0 | **19** (the berth NAME swapping) |

Read the Earth row as a floor, not a rate: 1,744 changes over 2,000 samples means it changed on nearly
every sample the bench took (one every ~14 s of sim time), so the true rate is faster than the sampling
can see — a strobe rather than a flicker.

Every one of those swaps is between members of ONE neighbourhood. The slot is not just a word: the
scope's AUTO lock draws whatever body holds it and the HUD quotes that body's range and closing speed,
so each swap is a picture and two numbers jumping — which is the owner's second comment
("same flickering on the scope on targets that have hierarchical position").

## The law

Two halves, both phase-independent, which is why the result is ZERO changes of mind rather than fewer.

1. **`NearestRule.StandsForItself`** (new, Core) — a satellite defers to its primary until the ship is
   inside its **Hill sphere**; then it speaks for itself and the existing band decides the rest.
   *Why the Hill radius and not something roomier:* a satellite's distance from a parked ship swings
   between |D−a| and D+a, so any threshold T it can cross is crossed twice an orbit for every hover range
   D in a ± T. The Hill radius (kilometres, where the rails are hundreds of thousands) shrinks that window
   to the moon's own capture width. The obvious roomier line — "nearer to it than it is to its primary",
   T = a — reopens it over half the approach; that is asserted directly in
   `StandsForItself_TheHillRadiusIsTheLaw_BecauseARoomierLineIsStraddledEveryOrbit`.
   It is also the same line `LocalMarketBody` and `IsHiddenAtHaven` already draw for "you are at this
   body", so the slot now agrees with them instead of wandering off on its own.
   A mass-less berth has no Hill sphere, so it never takes the slot by drifting past — it takes it by
   being clamped to, written in as its own clause (`_dockedHavenId == body.Id`), which is what keeps
   lying-low-at-a-dock heat cooling working.

2. **Which berth the line names is decided in the frame the question belongs to.** Case (b) of
   `UpdateNearestNeighbourhood` picked the berth nearest the SHIP, gated on `InTheSameBreath` against its
   live distance — both phase-dependent, hence the 19 name-swaps at Earth @ 10M km. Now: **inside** the
   planet's Hill sphere the ship is in among them and "nearest" means something, so take the nearest to the
   ship and hold it while the two are in the same breath (#966's law again); **outside** it, name the
   planet's own berth — the innermost, shortest rail — which is read off rails that do not turn, so it is
   the same answer every frame of every orbit. Either way the neighbourhood keeps naming a berth whenever
   it has one, so the ⚓ hint is steady all the way in instead of dropping out on approach (the old
   same-breath gate dropped it at ~400,000 km, right as the captain came inside coasting distance).

## Files

- `src/SpaceSails.Core/NearestRule.cs` — `StandsForItself` / `StandsForItselfSquared` + the reasoning.
- `src/SpaceSails.Client/Pages/Map.Sim.Tick.cs`
  - `UpdateNearestBody`: sweep skips bodies that don't stand for themselves; an incumbent that has
    stopped standing for itself hands the slot back.
  - new private `StandsForItself(CelestialBody)` — the docked clause + Hill radius, instantaneous rail.
  - `UpdateNearestNeighbourhood`: case (b) rewritten as above; new `_neighbourhoodHavenId` incumbent.
- `tests/SpaceSails.Core.Tests/NearestRuleTests.cs` — 4 new gates on the law.
- `tests/SpaceSails.Client.Tests/NearestHoldsTheNeighbourhoodTests.cs` — NEW. 32 posts (8 planets ×
  4 ranges) × {slot, line} + the premise + arrival + clamped.
- `tests/SpaceSails.Client.Tests/NearestDoesNotFlickerTests.cs` — one assertion updated (see below).

## RED proofs (both done, by revert)

- **Half 1** — took the two `StandsForItself` filters out of `UpdateNearestBody`:
  **41 of 72 failed** (20 SLOT posts, 20 LINE posts, +1). Restored → 72/72.
- **Half 2** — put the berth gate back to `InTheSameBreath` on the live distance and dropped the berth
  incumbent: **1 failed**, `THE_LINE(earth, 10,000,000 km)` — exactly the post the sweep predicted.

The guard asserts the MECHANISM (no change of the held id, no change of the readout's words while the
ship goes nowhere), not pixels. Its premise test proves the world can tell pass from fail: at each post
either the literal nearest really does change hands, or it never leaves the planet — and both branches
are asserted rather than skipped, plus every planet holding more than one body must blink SOMEWHERE.

## One existing assertion changed, deliberately

`NearestDoesNotFlickerTests.A_REAL_CHANGE_OF_NEIGHBOURHOOD_StillMovesTheReading` asserted
`_nearestBody.ParentId == "mars"` when parked 0.16 AU off Mars — i.e. that the STATION held the slot.
Under the new law Mars itself holds it (the berth defers). The assertion now reads "the reading is in
Mars's neighbourhood" (`Id == "mars" || ParentId == "mars"`), which is what that line was establishing
before it flies the ship to Jupiter. The other four tests in that file pass unchanged, including
`THE_LINE` ("Mars › The Rusty Roadstead") and `THE_ANCHOR` (`_nearestHaven == the-space-bar`).

## The frame fingerprint: all 30 re-pinned, with the evidence

`EveryFrameLeavesTheSameFingerprintTests` went red on all 30 rows — correctly: this is a BEHAVIOUR change
and that guard exists to catch exactly that. Re-recorded with `SPACESAILS_FINGERPRINT_WRITE=1` and
documented in the file's own ledger, with the diff stated rather than asserted:

- **4 ledger rows moved and no others**: `nearest body`, `nearest body at`, `nearest body moving` (the slot
  and its kinematics — one fact three times) and `sweep`. Every text is still 45 lines.
- On all 30 the reading was a **berth** (`selene-gate` ×25, `satellite-factory` ×5 on `wheel.json`) and is
  now **Earth**, the planet those berths ride. The same substitution every time.
- Field count 742 → 743: one field added, `_neighbourhoodHavenId`.
- `SPACESAILS_SWEEP_DUMP` dump-and-diff on the base (b301cc3) vs this lane: out of 743 fields, **exactly
  four differ on every row** (`_nearestBody`, `_nearestBodyPosition`, `_nearestBodyVelocity` changed,
  `_neighbourhoodHavenId` added), plus `_nearestParentName`/`_nearestChildName` to their defaults on the
  five `wheel.json` rows only — that world's works platform is not a dockable berth, so with the planet in
  the slot there is nothing for the line to name; those five had `_nearestHaven` at ∅ on both sides.
- **`_nearestHaven` is byte-identical on all 30** — the ⚓ hint is where it was. So are `walked-view pen`
  and `map-frame buffer`, every call count included: nothing new is drawn, nothing else moved.

The first cut of half 2 (a rail-width gate on the berth name) DID drop `_nearestHaven` and the hierarchy
on 25 rows; the sweep dump is what caught it, and it is why the rule became "inside the well, nearest to
the ship; outside it, the innermost berth" instead.

## Blast radius checked

`_nearestBody` is load-bearing (aerobrake seed, autopilot orbit info, combat "where"/`IsHiddenAtHaven`,
docking, `CurrentWellBodyId`, `LocalMarketBody`, `LocalSpaceBodyId`, scope AUTO). Each was read before
changing the sweep:
- `CurrentWellBodyId` walks up to the planet-level well — same answer either way.
- `LocalMarketBody` / `IsHiddenAtHaven` both require `IsBound` inside the body's Hill sphere, which is
  now exactly when that body holds the slot. Docked markets come from the `_docked && _dockBodyId`
  branch, untouched.
- Docking never reads `_nearestBody` for the affordance (`Map.Docking.cs:282`), and sets it explicitly
  on clamp-on; the docked clause keeps it.
- Scope AUTO now steadily draws Mars instead of ping-ponging — the point of the exercise.

## Cost in the tick

`StandsForItself` runs once per body per frame (29 bodies in sol.json, 14 of them satellites). It takes
the position the sweep already computed rather than re-asking the ephemeris, and finds the parent with an
explicit loop rather than a LINQ closure, so it allocates nothing — the same reason every other body
lookup in this file is written the long way. A planet costs one string compare and returns; a satellite
costs one body-list scan, one extra `Position`, and one `HillRadius`. (Timings were NOT measured in a
browser: an MCP-driven tab is `document.hidden`, so any number from one is worthless.)

## Not done / open

- No full Core+Client run yet on the final tree (one is required before the PR).
- No player-facing prose added or changed.
