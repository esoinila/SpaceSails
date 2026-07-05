# Sunday Second Plan — PR breakdown

*Derived from [SundaySecondVision.md](SundaySecondVision.md), 2026-07-05 afternoon. Goal: the
Sensors map becomes THE place to know and find things — you point at the sky and ask what is
there. Same working shape as the morning's fire-control chain: a stacked chain of reviewable
PRs, owner approves top-down, implementer rebases and merges each as approved.*

**Status: ALL MERGED (owner approval, 2026-07-05 evening).** PR-0 #69 · PR-A #70 · PR-B #71 ·
PR-C #73 · PR-D #74, merged to main in that order with the retarget-child-first squash
procedure — no casualties this time. Also merged alongside: **#72 "the world does not wait"**
(independent, off main) — the owner asked mid-build why the starting sky was empty, and it
was two real bootstrap bugs: inner-system depots despawned at birth (their own host body sat
inside the despawn tolerance) and short-route "mid-flight" ships could spawn with departure
times in the future (the 20–70-day lead was never clamped against the transfer). 219/219
tests on assembled main; verified live — at sim 0d0h0m the sensors desk already lists the
Earth and Highport depots as en-route passes, lanes drawn, passive watch sweeping.

**Evidence from the owner's screenshots** ([pics/](pics/)):

- [Barnacle_Click_does_nothing_here.png](pics/Barnacle_Click_does_nothing_here.png) — a
  labeled contact on the Sensors map that gives no response to a click. Everything visible
  in the sky must answer a click with scan options.
- [TradeShuttle.png](pics/TradeShuttle.png) — the owner hand-draws the trade-shuttle lane
  from the ship down to the Luna/Highport region. That corridor should be *drawn and
  selectable on the map*, not an entry in the tracking post's `— manual aim —` dropdown.

## The vision, distilled into features

| # | Feature | Lane |
| --- | --- | --- |
| F1 | **The telescope is one instrument** (Core) — a deterministic scheduler owns the single onboard Hubble: track-update passes, area scans, corridor sweeps, lost-lock searches, all from one prioritized queue | PR-A |
| F2 | **Lost locks leave a search area** (Core) — a dark ship that burned while we looked elsewhere doesn't vanish; its track becomes a growing search region (PathPredictor cone, weaponized the other way) with a rediscovery scan job | PR-A |
| F3 | **An area scan always finds *something*** (Core) — point a Hubble anywhere and there is debris, a cold pod, a rock; deterministic seeding, never "nothing" | PR-A |
| F4 | **Trade corridors as map geometry** (Core + overlay) — the Earth–Mars lane as a world-space region with a "how near is this point to a lane, here and now" query | PR-A geometry, PR-B drawing |
| F5 | **Scan-state overlays on the map** — corridor areas, the wedge the telescope is scanning *right now*, lost-lock search circles, update-pass flashes on tracked ships | PR-B |
| F6 | **Point at the sky and ask** — clicking ANY target (ship, planet, unknown label, empty space) on the Sensors desk opens a scan-contextual menu: SCAN THIS SHIP · SCAN AROUND HERE · SCAN THIS CORRIDOR; empty space near a lane says so ("near the Earth–Mars lane") | PR-C |
| F7 | **Sensors popups talk scanning, not navigation** — set-destination/auto-insert stay on the Nav desk; the same click on Sensors gets scan options | PR-C |
| F8 | **The Sensor tasks list** — a new UI: the telescope's prioritized queue, visible and reorderable; tracked-target boxes one live image per slot; PRIORITIZE REDISCOVERY at the expense of the others | PR-D |
| F9 | **Our path on the sensors map** — current trajectory + scrub line toward destination stays visible on Sensors, same as Nav; the sensors chief always knows where *we* are going | PR-B (verify + small) |
| — | **`docs/features/sensors-map.md`** — the M29 contact-menu/dossier/transponder work plus everything above finally gets a feature spec; user-guide sync | PR-D (docs ride the last PR) |

## Why a stacked chain again

Same reason as the morning: these are one subsystem in layers. The scheduler (A) is what the
overlays (B) visualize, the click menus (C) enqueue into, and the task list (D) reorders.
Every PR reviews clean against its base; the owner approves top-down.

## What already exists (so we don't rebuild it)

- **Slots**: `TrackedTargetLedger` (`Core/TrackingStation.cs:106`) is already the limited
  tracked list (`MaxTracks = telescope level + 1`, cap 4, dock upgrade).
- **Corridor wedges**: `ScanPrograms.BuildPrograms` (`Core/TrackingStation.cs:315`) already
  computes per-lane sweep arcs — today they surface only as the `<select>` at
  `TrackingPost.razor:72`. PR-B/C give them a body on the map; the dropdown then retires.
- **Cone math**: `PathPredictor.HalfWidthAt` already grows uncertainty over time — the
  lost-lock search area is the same idea drawn as a region to re-scan.
- **Target boxes with images**: `ScopeView` vector silhouettes + the tracking post's
  sensor-cards. M27 collapsed the scope wall to a single cycling tile; PR-D brings back one
  live box per slot (the owner's "each has its own image").
- **Click plumbing**: `OnPointerDown` → `TrySelectShipAt` / `TrySelectBodyAt`
  (`Map.razor:4733/3699/3948`) — PR-C adds the missing branches (unknown labels, empty space)
  instead of falling through to camera drag.

## The PRs

### PR-0 — this plan (tiny, vs main, first) — branch `sunday2/pr-0-plan`

This document plus the three screenshots promoted out of `tmp_pics` into `pics/`. Everything
else assumes the merge order below.

### PR-A · 🔭 One telescope, one queue *(F1 + F2 + F3 + F4 geometry)* — branch `sunday2/pr-a`

Core + tests only; zero client changes. Determinism is law.

- `Core/SensorTasks.cs`: `SensorTask` (kinds: **TrackUpdate**, **AreaScan**, **CorridorSweep**,
  **LostSearch**) + `TelescopeSchedule` — a deterministic scheduler that owns the single
  telescope. One look direction and one focus at a time; each task takes sim-time proportional
  to area/quality asked; round-robins by priority order. The passive proximity watch
  (`SensorModel`) is the hull's skin and stays always-on — it is *not* the telescope.
- Track updates become scheduled passes: the ledger's quality decay (`TrackedTargetLedger`)
  is now fed by actual telescope passes, so tracking 1 ship = near-continuous custody, but
  tracking 4 + scanning a corridor = real gaps a dark ship can burn inside.
- **Lost lock ⇒ search area**: when a track drops (`AdvanceTime` today just deletes it), it
  becomes a `LostTrack` carrying last observation + a `PathPredictor`-style expanding search
  region, and the scheduler auto-enqueues a **LostSearch** task. Searching the region shrinks
  it; a hit re-acquires the track; the region eventually exceeds telescope field → cold case.
- **AreaScan finds something**: deterministic discovery table seeded from scenario + quantized
  area cell + sim-day — debris, a cold cargo pod, a rock, a transponder-dark hull if one is
  truly inside. Never randomness, never "nothing".
- **Corridor geometry**: `TradeCorridor.Region(bodyA, bodyB, t)` — segment between the two
  anchors' positions now, with a lane radius — plus `NearestCorridor(point, t)` for
  "this empty spot is near the Earth–Mars lane" (feeds PR-C's popup and PR-B's drawing).
  Replaces the vantage-wedge-only view in `ScanPrograms` as the corridor's source of truth.
- Tests: `SensorTasksTests.cs`, extensions to `TrackingStationTests.cs` — custody-gap math,
  search-region growth/shrink, deterministic discoveries, corridor nearest-point.

### PR-B · 🗺 The sky shows its state *(F5 + F4 drawing + F9)* — branch `sunday2/pr-b` (on A)

- New renderer opcode `OP_POLYGON` (filled, low-alpha) in `renderer.js` + `CanvasRenderer` —
  the one JS change; everything else stays Razor-side. (Working agreement: JS only in
  renderer.js.)
- Sensors-desk overlays, drawn in the map frame builder alongside `DrawPredictionCone`:
  - **trade corridors** as faint filled lanes between their anchors (the owner's hand-drawn
    arrow, made real), brightening on hover/selection;
  - **the live scan area** — the wedge/spot the telescope is on *right now*, with sweep
    progress (the tracking post's `SweepProgressPercent`, finally on the sky itself);
  - **lost-lock search regions** — expanding circles around last-known, pulsing while a
    LostSearch task is queued;
  - **update-pass flash** — a brief bracket highlight on a tracked ship when its scheduled
    pass runs ("now checking Pod-1, because it's on our list").
- F9 verify: the ship trajectory + destination scrub line render in the always-on frame
  builder, so they should already survive on Sensors — verify and fix any desk-gating,
  keep the scrub marker visible.
- Overlays are Sensors-desk-gated (Nav keeps its clean plotting sky). Playthrough-harness
  screenshots for the visual diff.

### PR-C · 👉 Point at the sky and ask *(F6 + F7)* — branch `sunday2/pr-c` (on B)

- `OnPointerDown` on the Sensors desk gains the missing answers — nothing visible is mute:
  - **ship contact** → existing menu grows scan verbs: SCAN THIS SHIP (enqueue TrackUpdate /
    add to slots), SCAN AROUND HERE (AreaScan on its vicinity);
  - **planet/body** → SCAN THIS PLANET'S VICINITY, SCAN APPROACHES (the body's lane ends);
  - **corridor area** (hit-test PR-B's lanes) → SCAN THIS CORRIDOR — enqueues the sweep and
    lights the lane; the `— manual aim —` dropdown options move here and the dropdown retires;
  - **empty space** → a small "sky cell" popup: what a scan here would take, plus the
    guideline hint when `NearestCorridor` says we're close — *"near the Earth–Mars lane —
    scan the lane instead?"*;
  - **the Barnacle case** — labeled-but-unfixed contacts get "no fix — SCAN AROUND HERE to
    find out", never a dead click.
- **Desk separation (F7)**: on Sensors, body clicks get the scan menu; set-destination /
  auto-insert stay Nav-only. (Today `TrySelectBodyAt`'s nav menu is gated `Nav or Sensors` —
  the `or Sensors` moves to the new scan menu.)
- Every menu action lands in PR-A's queue — the map is the input device for the scheduler.

### PR-D · 📋 The Sensor tasks desk *(F8 + docs)* — branch `sunday2/pr-d` (on C)

- The new **Sensor tasks** panel on the tracking post: the telescope's queue as a visible,
  prioritized list — each entry shows kind, target/area, time-to-complete, and ▲▼ priority
  controls. What the telescope does next is never a mystery.
- Tracked-target boxes return as a fixed grid: **one live `ScopeView` box per slot**, each
  with its own image (the M27 single cycling tile becomes the fallback for tiny screens).
- **PRIORITIZE REDISCOVERY**: a lost track's card offers it in one click — LostSearch jumps
  the queue, and the UI shows what pays for it (which tracks will gap).
- `docs/features/sensors-map.md` — spec covering M29's contact menu/dossier/transponder AND
  this plan's scheduler/overlays/menus; `docs/user-guide.md` sync; retire stale bits of
  `docs/features/tracking-post.md`.

## Risks / open questions for the owner

1. **Slot economy**: with the telescope exclusive, is `MaxTracks = level + 1` still right, or
   should slots and telescope level decouple (slots = list size, level = pass speed)? Plan
   assumes *keep coupling*, level also shortens pass time.
2. **Passive watch**: stays always-on and free (hull skin), telescope is the scarce hero —
   confirm that reading of the vision ("It cannot look more than one way at a time").
3. **Corridor radius**: first cut = fixed fraction of lane length, tuned by screenshot; a
   later PR can derive it from actual traffic dispersion.

## Working agreement unchanged

Determinism is law in Core; UI = Razor + Bootstrap; JS only in renderer.js; playthrough
harness for anything visual; senior reviews/verifies; owner approves PRs. Merge-train
procedure per the lesson in [SundayPlanPRs.md](../SundayPlan/SundayPlanPRs.md) — retarget
child to main first, then merge parent, then rebase `--onto` the original commit.
