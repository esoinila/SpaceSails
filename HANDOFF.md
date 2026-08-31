# #960 — window overlap plotting to rob the Mars depot — HANDOFF

## Status: NO CODE CHANGE NEEDED. The bug is already fixed on this branch. A new browser regression
gate was attempted and did NOT reach green locally in this session — reverted rather than shipped red.
Reporting honestly per the lead's standing order instead of burning another local round.

## What #960 actually was, and why it is already fixed

The issue is OPEN but the fix (`22fd0fe`, PR #971) is already in the base branch, and the owner's own
last comment on the issue says so: *"Done in #971: the dossier card now stacks above the
navigation-target panel (which is capped and scrolls) when both are up, and it has the same
minimise-to-tile as the scope — no dragging."* The issue stays open only because PRs to
`our-own-ship-has-compartments` do not auto-close — the same situation as #954's HANDOFF before this one.

Read the whole chain of comments in the CSS and razor to confirm it is not just a claim:

- `src/SpaceSails.Core/OverlayBands.cs` — `MapDestPanel = MapChrome + 2` (12), `MapDossier = MapChrome + 10`
  (20): the dossier is declared to out-rank the nav panel by law, checked against the live CSS by
  `CssZBandSyncTests`.
- `src/SpaceSails.Client/Pages/Map.razor.css`:
  - `.map-dest-panel` (~line 1225) — capped at `max-height: 17rem; overflow-y: auto`, with a comment
    naming #960 by number: *"CAPPED so the dossier stacked above it (.map-dossier-raised) sits at a
    KNOWN offset rather than one CSS would have to measure."*
  - `.map-dossier-raised` (~line 1768) — `bottom: 18.25rem`, declared AFTER `.map-dossier` on purpose
    (same specificity, later wins), with the comment: *"WINDOWS AT THE BOTTOM CENTRE TAKE TURNS… the nav
    panel keeps the floor… the dossier rides above it, clear of the capped panel below."*
  - `.map-dossier-tile` (~line 1776) — the minimise-to-tile the owner asked for ("option to minimize a
    window into a sugarcube tile and back would avoid the moving-windows can of worms"). No dragging,
    exactly as he specified.
- `src/SpaceSails.Client/Pages/Map.razor` (~line 7001) — the dossier's `OverlayShell` block quotes the
  issue directly: *"#960 — the owner's screenshot has this card lying ON TOP of the navigation-target
  panel, hiding its text and half its buttons. Two answers, both his… when the nav panel is up the
  dossier is RAISED to ride above it… and either way the card now minimises into a tile of its own."*
  `Stacked="DossierIsStacked"` wires the raise; `DossierIsStacked` (`Map.NavToolbar.cs:127`) is
  `PlotMode && _destinationBodyId is not null && _activeDesk == ShipDesk.Nav`.

This was carried further by the shell migration (#997, waves 7–10, PRs #1008–#1012): the dossier and the
nav panel both moved onto `OverlayShell`/`CappedScrollPanel`, gained a tall-card cap so a long file no
longer runs off the top of the glass, and `?target=<contact-id>` (`Map.Npc.SeedTargetCheat`) was added
specifically so a browser gate could reach the dossier at all without a live robbery.

**Conclusion: nothing in the product needs to change for #960.** The three owner screenshots on the
issue thread (targeting the wrong depot, scaring off the debt collector) are him continuing to play after
filing it, not further sightings of the overlap — the last comment already closes the loop.

## What I attempted, and why it is not included

Per standing practice (a fix should carry a browser-level regression guard, proven RED once by revert),
I tried to add `HudCollisionTests.The_target_dossier_never_covers_the_navigation_target_panel` —
a Playwright gate that would: boot `?start=wreck&target=collector` (dossier up), tuck it, enter Plot mode,
search-jump the camera to Mars (`Camera.CenterOn` — by construction of `Camera.WorldToScreen` this puts
Mars at exactly canvas-width/2, canvas-height/2), click canvas-centre to open her body menu, **Set
destination**, untuck the dossier, then assert `.map-dest-panel` and `.map-dossier` (now `.map-dossier-raised`)
never overlap.

Four local rounds, each fixing a real test-authoring bug and none of them a product bug:

1. `"Mars"` in the nav-search box strict-matched two rows (the planet AND the "Mars Depot" NPC) — fixed
   by matching the row's own text `"Mars · body"`.
2. `JumpToSearchResult` is `async`; Playwright's click resolves on DOM dispatch, not on the Blazor handler
   finishing — added a wait for `.nav-search-results` to close as the real completion signal.
3. A debug screenshot (worth taking before more blind edits) showed WHY the canvas click found nothing:
   the collector's dossier, **unstacked** (no destination set yet), is tall enough to cover the canvas
   centre outright — the click was landing on the card, not the canvas underneath it. Fixed by tucking the
   dossier into its tile before the search/click, untucking it only afterward to take the real measurement.
4. That surfaced a second, genuine finding worth keeping in mind for future OverlayShell work:
   `OverlayShell.RootClassList` is `Tucked ? TileClass : HostClass` — **not** `HostClass + (Tucked ? TileClass
   : "")** — so a tucked shell's root wears `.map-dossier-tile` ALONE, not `.map-dossier.map-dossier-tile`.
   My locator assumed the compound class stuck around; fixed to `.map-dossier-tile` alone.

After all four fixes the gate still failed at the same step it failed on attempt #1 — `.map-body-menu`
never appears within 30 s of the canvas-centre click — so tucking the dossier was necessary but not
sufficient. I did not find the remaining cause (possibilities not yet ruled out: the canvas's actual
`WidthPx`/`HeightPx` set by `Camera.SetViewport` may not equal its CSS `BoundingBoxAsync()` size if the
renderer sizes the backing buffer differently; or `PlotMode`/`Paused` changes what `CollectPointCandidates`
is willing to pick; or the search jump's effect is being raced by something else entirely). Per the lead's
"one verification pass" standing order, I stopped there instead of continuing to guess-and-rerun (each
local round costs ~1 minute with the publish cached, but diagnosing blind was not converging).

**The attempted test file was reverted** (`git checkout -- tests/SpaceSails.UiGate/HudCollisionTests.cs`)
rather than committed red — a known-failing gate helps nobody and the branch is otherwise clean.

## Not done / open

- No `HudCollisionTests` gate for #960 exists yet. The fix itself is real and load-bearing (z-order,
  cap, raise, minimise-to-tile all present and cross-referenced above); it is only the NEW pixel-level
  regression gate that did not land.
- Whoever picks this up next: the tuck/search-jump/canvas-click skeleton above is sound and worth
  keeping — the remaining unknown is narrow (why the post-jump canvas click finds no pick candidate at
  all, not even the wrong one). A first move worth trying: screenshot immediately after the click (before
  the 30 s wait) to see whether the game state changed at all, and read `Sol` readout's own `Zoom:` /
  frame chip line for confirmation the jump actually landed, rather than trusting `Camera.CenterOn`'s
  math blind.
- No player-facing prose changed. No source file changed. This branch's diff against
  `our-own-ship-has-compartments` is HANDOFF.md only.
