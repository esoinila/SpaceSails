# HANDOFF — #1027 satchel stacking (branch `fix/1027-satchel-stacking`)

Worktree: `D:/repo12/wt/1027`. Base: `our-own-ship-has-compartments`. **Delete this file before opening the PR.**

## The bug
Press **I** under a queued arrival card (`?rip=1` → 🛗 THE SHAFT) and the satchel opens in the DOM but paints
UNDER the card: both wore `.view-object-backdrop` at z 1320, and the satchel's block is typed at Map.razor
~2480 vs the `_viewObject` block at ~3550, so document order decided it. `elementFromPoint` at the middle of
the pocket returned `<img class="view-object-img">`.

## Direction chosen: (b), the band gives the pocket a later paint slot
(a) — the satchel dismissing or deferring the cards — was rejected: the arrival beats are told-once and LATCH
when raised (`ex.HiveCantinaHallShown = true`, `HiveFloorsSeen`, `HiveUnlistedPlateShown`), so dismissing them
spends a beat nobody read; and deferring needs a `_viewObject` re-show queue, which #693 explicitly declined
and Map.Surface.Satchel.cs still records as declined. (b) destroys nothing.

## Done (all committed & pushed)
1. `OverlayBands.SatchelBackdrop = DesksAndPopups + 130` (1330) — one over the cards, ten under the lifeline.
2. `Map.razor.css`: `.satchel-backdrop { z-index: calc(var(--z-desks-popups) + 130) }`, placed AFTER
   `.view-object-backdrop` (equal specificity → source order is the tie-break).
3. `Map.razor` ~2480: backdrop div is now `class="view-object-backdrop satchel-backdrop"` (modifier idiom,
   same as `rep-backdrop`/`selfie-backdrop`, so the pop-up law's recogniser still sees a registered root).
4. `Map.Sim.Cancel.cs`: Esc chain is paint order top-down now — the `.convergence-backdrop` family (1420)
   leads, then `_showSatchel` (1330, **first time Esc has ever reached the satchel**), then `_storyCard`
   (1320) and the rest. Enter chain mirrored; `if (_showSatchel) return false;` stops Enter falling through
   the pocket onto a card nobody can see.
5. `RescueLifeline`: the forward-guard stand-in is built at the band's new ceiling (1330).
6. `CssZBandSyncTests`: `.satchel-backdrop` in the Overlays theory data + in the lifeline out-ranks sweep.
7. **Guard** `tests/SpaceSails.UiGate/TheSatchelPaintsOverTheCardTests.cs` (2 tests, Playwright).

## RED-by-revert proof (done 2026-08-31)
Reverted `.satchel-backdrop` to +120 and deleted the `_showSatchel` Esc line, republished, re-ran:
* `Pressing_I_under_an_arrival_card_puts_the_satchel_on_top` → FAIL: *"the top paint at the middle of the
  satchel is `<img class="view-object-img">`"* — the issue's own symptom.
* `The_card_underneath_is_neither_spent_nor_lost` → FAIL (Escape left the satchel up).
Fix restored, both green, twice in a row (58 s).

## Remaining
- [ ] browser-free twin in `tests/SpaceSails.Client.Tests` (band order + markup class + Esc/Enter chain order)
- [ ] full Core suite (`dotnet test SpaceSails.slnx`) + full UiGate
- [ ] delete this file, open PR (`gh pr create --base our-own-ship-has-compartments`), reference #1027

## Local run recipe
```
dotnet publish src/SpaceSails.Client -c Release -o D:/repo12/wt/1027/publish --nologo
SPACESAILS_PUBLISH_DIR=D:/repo12/wt/1027/publish dotnet test tests/SpaceSails.UiGate
```
Do not touch port 5073; never kill dotnet broadly.
