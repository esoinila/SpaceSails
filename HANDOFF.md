# HANDOFF — #962 · 📡 sharpen fix, with every telescope already held

Worktree `D:/repo12/wt/962`, branch `fix/962-sensors-sharpening`, base `our-own-ship-has-compartments`.

## The bug (read off the owner's own screenshots)

Owner's second screenshot on #962 is the one that matters: **"Tracked targets (1 / 1)"**, the destination
depot holding the single telescope slot, **"Passive watch — 1 tracked, 1 slipped (telescopes full)"**, and
the *Sensor tasks* list carrying THE RED EYE DEPOT and nothing else — after 📡 *sharpen fix* was pressed on
the Debt Collector. His words: *"I click sharpen fix but the sensors do nothing useful."*

#964 fixed the empty-ledger case (the button had resolved its subject through `FindNpc`, which never sees a
hunter). The full-ledger case — which is the case in his screenshot — was still dead, for three reasons:

1. `TrackShipFromMenu` queued a `SensorTask.TrackUpdate`, which is the LEDGER'S standing custody pass.
   `TrackingPost.HandleLostAndColdTracks` sweeps those out for any contact the ledger does not hold —
   so the order was placed and deleted again on the very next tick, before the list was ever looked at.
   The pulse meanwhile promised "she is the next look": the repo's own named bug class, on the button
   filed for it.
2. Even had it survived, `HandlePass` answered a finished pass with `TrackedTargetLedger.TryConfirm`,
   which only refreshes an entry that ALREADY exists. The look would have completed and done nothing.
3. The dossier's line — "not on the telescope ledger — track her to sharpen the intel" — is identical
   before and after the press. That sentence is what *"but really HOW??????"* was written under.

## The fix

- `SensorTask.SharpenFix(id, label)` (Core): the captain's one-shot look at a contact the ledger does NOT
  hold. Same id/kind/aim as a custody pass, `Recurring: false`.
- `TrackingPost` custody sweep now only reaps **standing** (recurring) track passes.
- `TrackingPost.HandlePass` on a TrackUpdate for an UNHELD contact enters the fix the pass earned: onto the
  ledger if a telescope is free, otherwise the desk line says the look landed and custody could not be kept
  ("drop a track to keep her") instead of the job leaving the list in silence.
- `TrackShipFromMenu` orders the standing pass when custody was granted, the one-shot when it was refused.
- Dossier: a third state — 📡 *the telescope is on her — her pass is on the Sensors task list*.
- The Sensor tasks list gets a ✕ on a one-shot look (a standing custody pass still gets none: the ledger
  re-queues it every tick, so the button would do nothing visible — drop the track instead).

## Guards (all four proven RED by reverting the fix, one at a time)

In `tests/SpaceSails.Client.Tests/TheScopeGoesWhereTheCaptainPointsItTests.cs`, section (d):

| guard | reverted | result |
| --- | --- | --- |
| `…KeepsHerLookOnTheSensorTasksList` | the sweep rule, or the SharpenFix order | RED |
| `…LandsHerOnTheLedgerOnceASlotIsFreed` | either of those, or `HandlePass` | RED |
| `…TheDeskSaysWhyCustodyCouldNotBeKept` | any of the three | RED |
| `THE_DOSSIER_SaysTheScopeIsOnHerOnceThePassIsOrdered` | `scopeOrdered` never computed | RED |

## Provisional lines (canon review requested)

Plain wording only, no lore:
- "📡 the telescope is on her — her pass is on the Sensors task list"
- "📡 not on the telescope ledger — the telescope button below puts the scope on her"
- "{callsign}: fix taken, but every telescope is held — drop a track to keep her"
- "{callsign} on the telescope ledger — fix taken"

## Note for the next hand

`TheScopeGoesWhereTheCaptainPointsItTests.PressSharpenFixOnTheDossier` finds the button by searching the
shipping `Map.razor` for its label. It now searches for `📡 sharpen fix</button>` — nothing else in that
card may repeat the bare label, or the search walks onto a neighbouring `@onclick` (it did).

Hunter physics untouched, per the standing owner ruling (fixed thrust, no gravity/autopilot).
