# Handoff — #1013 done, #997 not started (wrap-up directive, 2026-08-30)

## #1013 — the counter card's self-overlap: DONE, verified, PR'd

**Root cause, confirmed live (not just read off the CSS):** `ContactDrinkOffer` (Map.razor, the
per-bar-contact offer block rendered by the counter card, the stranger's-contract card, and the
patron's-table card) wrapped its own row in `class="deck-offer-actions"` — the SAME class the card's real
foot (Buy the special / Round for the room / …) uses, which `#735`/`#780` pin `position: sticky; bottom: 0`
with a 12rem box-shadow scrim. `PresentBarContacts()` can hand the counter several people in the room at
once (each drawing one `ContactDrinkOffer` block), so a full room drew as many sticky `bottom:0` siblings as
there were faces — all racing the real foot for the identical pinned rectangle. Reproduced live at the
Roadstead bar (`?scenario=sol&oldcrew=1`, the four-old-shipmates dev cheat) with a short viewport forcing
the card to overflow: the last contact's row and the card's foot collapsed to the exact same box, and the
foot's scrim painted over the contact row — "Round for the room" read struck through by "Offer a drink",
matching the owner's screenshot exactly.

**Fix:** `ContactDrinkOffer`'s own wrapper is `.contact-offer-row` now (all four branches), sharing the same
flex/wrap/gap/centre rule as `.deck-offer-actions` but NOT the sticky/scrim rule. Only the card's one true
foot stays pinned.

**Files:**
- `src/SpaceSails.Client/Pages/Map.razor` — renamed the four `ContactDrinkOffer` wrapper divs.
- `src/SpaceSails.Client/Pages/Map.razor.css` — split `.contact-offer-row` off the base flex rule, gave it
  `margin-bottom` instead of the sticky pin.
- `tests/SpaceSails.UiGate/HudCollisionTests.cs` — new gate
  `A_room_full_of_bar_contacts_never_covers_the_counters_own_foot`: boots `?scenario=sol&oldcrew=1`,
  click-to-walks onto the BARKEEP console (canvas-drawn, pixel click at (438,297), retried up to 5×),
  answers every "is looking at you" face-reveal so the real "Offer a drink" rows draw, shrinks the viewport
  to 1280×420 to force the card to overflow (the count/length of PresentBarContacts varies by seed, so a
  full-size window can pass by accident), then asserts no two `.contact-offer-row`/`.deck-offer-actions`
  rows overlap.

**Verified this session:**
- RED proof: reverted `.contact-offer-row` → `.deck-offer-actions`, republished, test failed naming the
  exact collision (all four contact rows + the foot collapsed onto one rectangle at (389,340)).
- GREEN proof: fix restored, republished, test passes (confirmed twice, including after tightening the
  click-to-walk retry loop for reliability).
- `dotnet build SpaceSails.slnx -c Release` — 0 warnings, 0 errors.

**Not yet run:** the full `dotnet test` suite (Core + Client) — CI will run it; per the wrap-up directive
this session did not spend another verification round on it locally.

## #997 — the chip strip / scope corner overlap: NOT STARTED

Out of time under the wrap-up directive before this was touched. The issue itself
(https://github.com/esoinila/SpaceSails/issues/997) already lays out the fix precisely — this is a
"measure it, don't reason about it" job, not a research job:

- `.desk-chip-strip` (`DeskChips.razor.css`) sits at `right: 0.5rem`, `width: 9.5rem` (column 0.5–10rem
  off the right edge).
- `.map-scope` / `.map-scope-tile` (`Map.razor.css` ~line 937) sit at `right: 0.75rem` with no `width`
  reservation, so on Nav (where both are visible at once) the scope's own header/controls land under the
  strip's last chip.
- `.desk-layer` already reserves an 11rem literal (`padding: 2rem 11rem 2rem 2rem`) for exactly this strip,
  with a comment saying so — that literal is the seam the issue wants turned into
  `--desk-chip-strip-clearance` on `.map-page`, read by `.desk-layer`, `.map-scope`, `.map-scope-tile`, and
  `.parrot-perch`'s `right`.
- Explicit trade-off the issue flags and a future session should re-confirm by looking, not assuming: moving
  the scope left to clear the strip brings it closer to `.map-readouts`' own right edge (x 1104) — check
  whether that's a new touch/overlap at 1280×720 before shipping.
- `tests/SpaceSails.UiGate/HudCollisionTests.cs` already has the regression-net idiom to copy
  (`The_desk_chip_strip_is_positioned_and_on_the_screen_on_every_desk`) — a new test should boot Nav, measure
  `.desk-chip-strip` and `.map-scope`/`.map-scope-tile`, and assert no overlap, with a RED proof (revert the
  clearance var) before it ships.

No code was written for #997. No regression test exists for it yet.

## Local dev notes for whoever picks this up

- `SPACESAILS_PUBLISH_DIR` env var points `dotnet test tests/SpaceSails.UiGate` at a pre-built Release
  publish instead of re-publishing every run (~1.5–3 min saved per iteration). This session used
  `C:/Users/ernos/AppData/Local/Temp/spacesails-uigate-publish` — re-publish it after any Client/Core change
  (`dotnet publish src/SpaceSails.Client/SpaceSails.Client.csproj -c Release -o <that dir>`) before trusting
  a `dotnet test` run against it.
- The claude-in-chrome MCP browser tools were unusable this session (every screenshot/JS-eval call timed out
  against both a fresh Debug `dotnet run` server and a Release one) — the whole investigation and both
  proofs went through Playwright directly via `dotnet test tests/SpaceSails.UiGate`, following the existing
  `HudCollisionTests`/`ClientHost` pattern. Worth mentioning to the next session before it burns time on the
  same dead end.
- `?oldcrew=1` (default dock `the-space-bar`) is the fastest way to get PresentBarContacts() non-empty
  without real ledger history — it seeds four old shipmates via `OldCrewHere` regardless of `HasHistory`.
  Reaching the BARKEEP console from there is click-to-walk (canvas-drawn, no DOM locator) at pixel
  (438, 297) on a 1280×900 viewport at that dock's default camera framing.
