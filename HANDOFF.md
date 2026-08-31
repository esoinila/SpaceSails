# #997 — chip strip vs scope controls — HANDOFF

## Status: fix implemented, both guards proven RED/GREEN, verified at desktop + 390x700. Full suites next.

## What was done
- `src/SpaceSails.Client/Pages/Map.razor.css`
  - `.map-page` gained `--desk-chip-strip-clearance: 11rem;` (documented, mirrors #986 F1's
    `--desk-top-clearance` move for the other axis).
  - `.desk-layer`'s right padding literal (`11rem`) now reads the variable.
  - `.map-scope`, `.map-scope-tile` use
    `right: min(var(--desk-chip-strip-clearance), calc(100% - 18.5rem))`.
  - `.parrot-perch` uses `right: var(--desk-chip-strip-clearance)` plain (it's 33px wide — no
    off-screen risk at any viewport the game supports).
- `tests/SpaceSails.UiGate/HudCollisionTests.cs`
  - New test `The_desk_chip_strip_never_covers_the_scopes_own_controls` (1280x720, Nav desk):
    `.desk-chip-strip` vs `.map-scope`/`.parrot-perch`, fails on overlap.
  - New test `The_scope_stays_on_screen_at_a_phone_width` (390x700, Nav desk): `.map-scope`'s box
    X must be `>= 0`.
  - Added a private `BootIntoNav()` helper (existing `BootIntoTheDeck()` goes to the Deck tab, not
    Nav, so it couldn't be reused as-is).

## Trade-off #1 — the one the issue named: does the scope crowd .map-readouts more?
Measured with a real Playwright pass at 1280x720 on the Nav desk, BEFORE and AFTER:

| | before | after |
|---|---|---|
| `.map-scope` box | (978,386) 290x322 | (814,386) 290x322 |
| `.parrot-perch` box | (1233,308) 33x36 | (1071,308) 33x36 |
| `.desk-chip-strip` box | (1120,76) 152x342 | unchanged |
| `.map-readouts` box | (12,173) 1105x216 | unchanged |

Scope/strip overlap before: 148x32 (the bug). After: none (814..1104 vs strip's 1120..1272 — 16px
clear). Parrot/strip overlap before: 33x36 (fully under a chip). After: none.

Readouts vs scope: readouts run y 173..389, scope runs y 386..708 in BOTH states — only a ~3px
hairline that predates this change and is unaffected by the horizontal shift (stacked, not
side-by-side — moving the scope left widens the x-overlap but the y-overlap stays flat at ~3px).
**No new collision traded in on this axis.**

## Trade-off #2 — found by doing the assignment's own 390x700 check, NOT named in the issue
The issue's numbers are 1280x720-only. Checking 390x700 (as instructed) found: `.map-scope` is a
fixed ~290px card (unresponsive 280px canvas underneath, pre-existing, unrelated to this fix), so
`right: 11rem` flat walked its LEFT edge off the left of a 390px screen by 76px — not overlapping
anything, just gone. Fixed with `min(11rem, calc(100% - 18.5rem))`: bites only below a ~29.5rem
(472px) viewport, so 1280x720 is unaffected (confirmed — see guard #1 above, still green), and at
390px the card stays fully on screen at the cost of a smaller residual touch with the strip's last
chip (~66x52px, down from the original 148x32, per a real measurement: box (6,366) vs strip
(230,76) 152x342). Full mobile responsiveness for the scope card is a separate, bigger job than
#997 — flagged as a possible follow-up, not attempted here.

## RED/GREEN proof done (both guards)
1. **Strip/scope overlap guard**: published fixed CSS → PASS. Reverted `.map-scope`'s `right` to
   the bare `var(...)` (no other change) via `git checkout <parent-commit> -- Map.razor.css`,
   republished → FAIL naming `.map-scope` at (978,386) and `.parrot-perch` under the strip at the
   exact pixels the issue reported. Restored, republished → PASS.
2. **Phone-width clamp guard**: published fixed CSS (with `min()`) → PASS. Edited `.map-scope`
   back to the bare `var(...)` only, republished → FAIL, `x=-76`. Restored the `min()`,
   republished → PASS.
3. Full `HudCollisionTests` class (7 tests, including the two new ones) run clean once more after
   both restores → **all 7 PASS**.

Two throwaway probe test files (`DiagTemp.cs`, `DiagShot.cs`) were used along the way to print raw
boxes and take screenshots; both were deleted before committing (not part of the diff).

## Visual verification done
- Desktop 1280x720 (Nav desk, scope open): screenshot confirms the Scope card's `◀ AUTO ▶ –`
  header is fully clear of the chip strip, with a visible gap; the parrot sits in that gap too.
- Mobile 390x700 (Nav desk, scope open, arrival story-plate dismissed): screenshot confirms the
  scope card is fully on-screen (not clipped); it still visually touches the last chip and the
  readouts text at this narrow width (trade-off #2 above, pre-existing/residual, documented in the
  CSS comments and the new guard test's own doc comment).

## One process hiccup worth flagging for future crews
A `dotnet publish` I backgrounded took several minutes longer than expected (lock contention with
an earlier publish still holding `obj/` files) and its completion notification arrived late/out of
order. I ran a full test pass against a **stale, half-updated publish dir** in the interim (one
new test failed with the pre-fix pixel numbers even though the source was already fixed) before
noticing the mismatch by grepping the actual published CSS content. Lesson for next time: after a
backgrounded `dotnet publish`, grep the *published output* for the change before trusting a test
run against it — a wasm/dll timestamp is not proof the CSS asset was rebuilt, and a "completed"
notification can arrive after a dependent step already ran against stale content.

## Remaining
- [ ] Run full Core + Client test suites once.
- [ ] Open PR against `our-own-ship-has-compartments`, reference #997 (no auto-close — different
      base), attribution footer + session link. Do NOT merge.

## Environment notes
- Worktree: `D:/repo12/wt/997`, branch `fix/997-chip-strip`, pushed to origin.
- Used `SPACESAILS_PUBLISH_DIR=/c/temp-spacesails-diag` (a `dotnet publish -c Release` of just
  `SpaceSails.Client`) to avoid republishing WASM for every `dotnet test tests/SpaceSails.UiGate`
  run — each publish is ~1-2 min (sometimes longer under lock contention, see above).
- Did not touch port 5073 or any running dotnet dev server.
