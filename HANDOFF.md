# HANDOFF — #957 "the autopilot refuses to Dock to Rustys… why… it should not"

Branch `fix/957-dock-refusal`, base `our-own-ship-has-compartments`.
Worktree `D:/repo12/wt/957`. Owner issue: https://github.com/esoinila/SpaceSails/issues/957

## The owner's report

Screenshot 1 (16:31, sim 178d 10h): NOW: manual — *autopilot declines The Rusty Roadstead:
can't verify a capture from here — no clear window within range.* Nav readout:
`3.60 M km out · clamp within 500,000 km · rel 12.4 km/s (match ≤ 8)` and
`Nearest: Mars (3.63 M km, 2.6 km/s rel)`.

Screenshot 2 (two minutes later, sim 191d): the same course is ACCEPTED —
`606096 km out · rel 4.7 km/s (match ≤ 8)`, `Nearest: Mars (602478 km, 14.4 km/s rel)`.

Owner: *"nobody will ever play the 'let's fly next to it really quiet so autopilot will
agree'."*

## ROOT CAUSE (found 2026-08-30) — a rail that is not gravity's

`scenarios/sol.json`, `the-space-bar` (**The Rusty Roadstead**, the game's Mars bar):

    "parentId": "mars", "orbitRadiusM": 12000000.0, "orbitPeriodS": 7200

Kepler for r = 12,000 km about Mars (μ = 4.2828e13) is **39,910 s**, not 7,200 s.
The rail therefore carries the station at

    2πr/T = 10,472 m/s        while a ship in that orbit flies at √(μ/r) = 1,889 m/s

**`DockRule.MatchSpeed` is 8,000 m/s.** So a ship that has matched Mars *perfectly*
still sees 10.47 km/s of relative speed at the berth and is over the clamp limit. Flying
"really quiet" makes the number WORSE, not better: the only way under 8 km/s is to be
hot in Mars' frame *and* pointed the same way the station happens to be whipping at that
instant — which is why the identical course was refused at one moment and accepted 13
sim-days later. Pure phase luck, exactly as the owner described.

Arithmetic check against the owner's own two readouts (both consistent with a
10.47 km/s station):
* shot 1: rel-Mars 2.6, rel-station 12.4  (2.6 + 10.47 ≈ 13.1, near-anti-aligned)
* shot 2: rel-Mars 14.4, rel-station 4.7  (14.4 − 10.47 ≈ 3.9, near-aligned)

The two numbers on the same panel could never both be right for a Keplerian berth
(they can differ by at most the station's 1.9 km/s orbital speed). They differ by ~10.

### The same literal is wrong in two more havens

Audit of every child body in every shipped scenario (`orbitPeriodS` vs 2π√(r³/μ)):

| body | haven | T | Kepler T | rail speed | v_circ |
|---|---|---|---|---|---|
| `the-space-bar` (The Rusty Roadstead, Mars) | yes | 7,200 | 39,910 | **10.47 km/s** | 1.89 |
| `cinder-roost` (Venus) | yes | 8,000 | 20,252 | **11.78 km/s** | 4.65 |
| `the-tilt` (Uranus) | yes | 14,000 | 59,065 | **35.90 km/s** | 8.51 |

Every other body in `sol.json` / `sol-eu.json` / `oops.json` agrees with Kepler to <1%
(`triton` is retrograde — negative period, correct magnitude — and is fine).
`wheel.json` is *declared* non-Newtonian ("the spoke is not gravity's work") and has no
stations, so it is out of scope of the law.

Named bug class, again: **an unaudited geometry literal** — and it was invisible to
reasoning and to the Core tests because nothing ever asked whether a berth's rail was a
gravity rail.

### Why the existing #957 machinery did not save it

`CaptureBrake` (landed in #965/77eee74) does the right thing and is not at fault, but
`tests/SpaceSails.Core.Tests/CaptureBrakeTests.cs` proves it on **`derelict-roadster`**
while its docblock says "The Rusty Roadstead (a μ=0 sun-parented berth)". The Rusty
Roadstead is `the-space-bar` — a **Mars-parented** berth. The one test that does use
`the-space-bar` asserts `CaptureBrake.Solve` returns **null** ("nothing on the ladder
flies") — i.e. the suite had enshrined the owner's bug as expected behaviour. The
wrong-world test class from MEMORY, verbatim.

## PLAN

1. Law test (Core): every body on a *gravity* rail in the shipped Sol scenarios must
   move at its parent's circular-orbit speed for its radius — a berth no Newtonian ship
   can ever ride alongside is a berth no autopilot can dock at. Proven RED by putting
   `7200` back.
2. Fix the three literals in `scenarios/sol.json`.
3. Re-point `CaptureBrakeTests` at the real Rusty Roadstead.
4. Play it headless from the owner's geometry and watch the dock arm.

## THE FIX (committed)

`scenarios/sol.json`: `cinder-roost` 8000 → **20252**, `the-space-bar` 7200 → **39910**,
`the-tilt` 14000 → **59065** — Kepler's period for each berth's own radius about its own
parent. Radii, phases and everything else untouched, so the map looks the same.

New guard `tests/SpaceSails.Core.Tests/EveryBerthRidesAGravityRailTests.cs` (8 cases):
Kepler on every rail in sol/sol-eu/oops; "flying quiet alongside a haven is inside the
clamp"; the wheel's declared exemption berths nobody; and the owner's own read flown.

`CaptureBrakeTests` docblock corrected — it called `derelict-roadster` "The Rusty
Roadstead"; that mis-naming is why the real berth had no test.
`NearestDoesNotFlickerTests` read its own copy of the 7,200 s literal; it now reads the
rail.

### Proof it can fail (house law)

Put 8000 / 7200 / 14000 back and run the class — 3 of 8 go RED, verified 2026-08-30:

    EveryRailInTheSolFamilyIsGravitys(sol.json)  FAIL
      cinder-roost:  period 8000 s but Kepler says 20252 s — rail 11781 m/s, Newton 4654
      the-space-bar: period 7200 s but Kepler says 39910 s — rail 10472 m/s, Newton 1889
      the-tilt:      period 14000 s but Kepler says 59065 s — rail 35904 m/s, Newton 8510
    FlyingQuietAlongsideAHaven_IsInsideTheClamp(sol.json)  FAIL
      The Rusty Roadstead: a ship flying its orbit still reads 8583 m/s; clamp shears above 8000
      The Tilt:            27394 m/s
    AtTheOwnersRead_TheAutopilotTakesTheDock  FAIL
      "The owner was 2.6 km/s off Mars; the berth beside it should not read 10043 m/s."

With the fix: 8/8 pass.

### The scene, flown (headless)

At the owner's own read — sim 178d 10h 48m, 3.60 M km behind Mars, closing at 2.6 km/s:

| | before | after |
|---|---|---|
| berth's speed about Mars | 10,472 m/s | 1,889 m/s |
| ship's rel speed at the berth | 10,043 m/s | 1,841 m/s |
| a ship at rest in Mars' frame can clamp | **0 / 720** samples of the rail | **720 / 720** |
| plain arm-time rehearsal | refuses | **Deliverable, Captured, 68 p** |

No correction burn is needed at all: the press the owner made simply works.

## THE TWO SNAPSHOT RE-PINS (both proved by dump-and-diff)

The first Client run was 1339 passed / 26 failed, all of them snapshot guards. Both were
re-taken through their own documented hooks and the diffs are small enough to read.

**`EveryFrameLeavesTheSameFingerprintTests` — 25 of 30 re-recorded.**
`SPACESAILS_SWEEP_DUMP` on old literals vs new: all thirty texts, same 743 lines, same
742 fields. `sweep` moved on 25 rows with EXACTLY ONE field line differing, `_passes`,
and inside it exactly two of twenty-nine passes (`cinder-roost`, `the-space-bar`).
`map-frame buffer` moved on the five `TheMapFrameInFlight` rows: counts unchanged (8364
floats, 24 labels), label set unchanged at 22, only ⚓ Cinder Roost / ⚓ The Rusty
Roadstead / ⚓ The Tilt moved. No ledger row, no `walked-view pen`, no call count.
The five `TheElectricUniverse` rows (wheel.json) are byte-identical.

**`TheBootBuildsTheSameWorldTests` — 81 of 82 re-pinned.**
`SPACESAILS_BOOT_FINGERPRINT_DUMP`, 1,913 lines either way, exactly two field lines per
URL: `_ephemeris` (the three OrbitPeriod tokens only) and `_npcStates` (3 of 34 records,
only `InitialState.Velocity`, every Position byte-identical). Those three are the traders
parked at the three berths, carried along at the berth's fake speed — 46,101 → 39,349 at
Cinder Roost, 34,020 → 25,880 at the Roadstead, 41,344 → 14,401 at The Tilt. A ship
"parked" at Uranus was outrunning Mercury. The equality partition is preserved exactly
(63 distinct hashes both sides over the same 82 URLs, same groupings) and the ONE URL
that did not move is `/map?scenario=sol-eu` — the scenario with none of these berths.

Both write-ups live in the two files' own docblocks.

## STATUS

- [x] diagnosis (this file)
- [x] law test RED on old data (numbers above)
- [x] data fix
- [x] headless replay of the owner's scenario
- [x] full Core suite: **4065 passed / 0 failed** (25 m 54 s, run watched 2026-08-30)
- [ ] full Client suite (running)
- [ ] PR
