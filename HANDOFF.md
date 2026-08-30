# #997 — chip strip vs scope controls — HANDOFF

## Status: fix implemented, guard proven RED/GREEN, verifying at 390x700 next, then full suites.

## What was done
- `src/SpaceSails.Client/Pages/Map.razor.css`
  - `.map-page` gained `--desk-chip-strip-clearance: 11rem;` (documented, mirrors #986 F1's
    `--desk-top-clearance` move for the other axis).
  - `.desk-layer`'s right padding literal (`11rem`) now reads the variable.
  - `.map-scope`, `.map-scope-tile`, `.parrot-perch` now use
    `right: var(--desk-chip-strip-clearance)` instead of their own literals
    (`0.75rem` / `0.75rem` / `0.9rem`).
- `tests/SpaceSails.UiGate/HudCollisionTests.cs`
  - New test `The_desk_chip_strip_never_covers_the_scopes_own_controls`: boots to the Nav desk at
    1280x720, measures `.desk-chip-strip` vs `.map-scope` and `.parrot-perch`, fails on overlap.
  - Added a private `BootIntoNav()` helper (existing `BootIntoTheDeck()` goes to the Deck tab, not
    Nav, so it couldn't be reused as-is).

## Trade-off re-check (the thing #1028's crew flagged)
Measured with a real Playwright pass at 1280x720 on the Nav desk, BEFORE and AFTER the CSS change
(diagnostic test, not committed — see below):

| | before | after |
|---|---|---|
| `.map-scope` box | (978,386) 290x322 | (814,386) 290x322 |
| `.parrot-perch` box | (1233,308) 33x36 | (1071,308) 33x36 |
| `.desk-chip-strip` box | (1120,76) 152x342 | (1120,76) 152x342 (unchanged) |
| `.map-readouts` box | (12,173) 1105x216 | (12,173) 1105x216 (unchanged) |

Scope/strip overlap before: 148x32 (the bug). After: none (814..1104 vs strip's 1120..1272 — 16px
clear). Parrot/strip overlap before: 33x36 (fully under a chip). After: none (16px clear).

Readouts vs scope: readouts run y 173..389, scope runs y 386..708 in BOTH states — only a ~3px
hairline touch that predates this change and is unaffected by the horizontal shift (they are
stacked, not side-by-side, so moving the scope left widens the x-overlap but the y-overlap stays
flat at ~3px). Conclusion: no new collision traded in.

## RED/GREEN proof done
1. Published Release client to `C:\temp-spacesails-diag`, ran the new test → PASS.
2. `git stash push -- src/SpaceSails.Client/Pages/Map.razor.css` (reverted the CSS only), republished,
   reran the new test → FAIL, naming `.map-scope` at (978,386) and `.parrot-perch` under the strip at
   the exact pixels the issue reported.
3. `git stash pop` to restore the fix, republished, reran the full `HudCollisionTests` class (6
   tests) → all PASS.

A throwaway `DiagTemp.cs` probe test was used to print the raw boxes above; it was deleted before
committing (not part of the diff).

## Remaining for whoever picks this up (or continuing myself)
- [ ] Verify visually at desktop size and 390x700 (one pass each, per instructions) — not yet done
      at the time of this handoff write, will follow immediately.
- [ ] Run full Core + Client test suites once.
- [ ] Open PR against `our-own-ship-has-compartments`, reference #997 (no auto-close — different
      base), attribution footer + session link. Do NOT merge.

## Environment notes
- Worktree: `D:/repo12/wt/997`, branch `fix/997-chip-strip`, pushed to origin.
- Used `SPACESAILS_PUBLISH_DIR=/c/temp-spacesails-diag` (a `dotnet publish -c Release` of just
  `SpaceSails.Client`) to avoid republishing WASM for every `dotnet test tests/SpaceSails.UiGate`
  run — each publish is ~1-2 min, each cold test run without it would repeat that.
- Did not touch port 5073 or any running dotnet dev server.
