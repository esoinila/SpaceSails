# HANDOFF — #1037 · the navigation-target panel joins the flow column

Worktree `D:/repo12/wt/1037`, branch `fix/1037-dest-panel-flex`, base `our-own-ship-has-compartments`.

## The bug, and the fix that was ruled right

`.map-dest-panel` (M25) was pinned to the window's own bottom-centre corner (`position: absolute;
bottom: 0.75rem`) while `.map-plot` above it lives in #992's flex column. Two layout systems that were
never made to coordinate: docked at a great port with a plan AND a destination set, the Plotting panel's
own bottom edge landed **12 px** inside the nav panel's top edge at 1280×720 and **16 px** at 390×700, and
which of the two a real screen painted there was whatever the z-band happened to say.

The z-index route is proven wrong by mechanism (#1035): `.map-hud` is ONE stacking context, so raising a
sibling above it out-ranks every control it carries — `CriticalControlsTests` caught `autopilot-disengage`
unreachable at every size. **Do not retry it.** The ruling (issue #1037, Fable) was #994's own precedent:
put the panel in the column's arithmetic so the two can never share a pixel.

## What shipped

1. **`Map.razor`** — the `.map-dest-panel` block moved INSIDE `.map-flowcolumn`, after the story plate,
   as the column's last item. Still `@if (_activeDesk == ShipDesk.Nav)`-gated.
2. **`.map-dest-panel`** — `position: relative; align-self: center; flex: 0 1 auto; min-height: 6rem;
   margin-top: 0.5rem; margin-bottom: 0.75rem`. Everything else kept: width 38rem, `max-width`,
   `max-height: 17rem`, `overflow-y: auto`, `z-index: chrome + 2` (**unchanged** — `CssZBandSyncTests` and
   `CriticalControlsTests` never see a drift). `margin-bottom` is the old `bottom`, the same 0.75 rem off
   the glass, so `.map-dossier-raised`'s 18.25 rem and `CriticalControls.BottomCentre`'s
   `vp.Bottom - 0.75rem` are both still true of it.
3. **`.map-hud`** — `overflow: hidden auto; overscroll-behavior: contain`. **This is the load-bearing
   half.** Three of the HUD's four blocks are `flex: 0 0 auto`, so on a short window they do not shrink,
   they OVERFLOW — measured at 390×700: the HUD's own box 0 px tall with 411 px of content painting
   straight down the glass over whatever was under it. Without this, the column's arithmetic is advisory
   and the phone case cannot be fixed at all. `auto` not `hidden`: what does not fit is still reachable by
   scrolling (#997's own backstop, one level up). At 320×480 today the frame chip and the Plotting panel
   lay out BELOW the bottom of the window with no way back to them; after this they page.
4. **`.story-plate`** — `flex: 0 1 auto; min-height: 0; overflow-y: auto`, and `margin-bottom: 0.5rem`
   when it is not the column's last child. At 390 wide the plate lays out **832 px** tall (its caption
   wraps to a ~75 px column beside the art) in a 700 px window — #735's law on the one card its gate was
   never pointed at, and the reason a plate that never shrinks pushes the nav panel off the glass. The
   5.5 rem bottom margin was clearance from the window's own foot; with a panel below it, that clearance
   IS the panel.
5. **`TheKeysOfNavigationAreTheLoudOnesTests.TheRaisedDossierClearsTheCappedNavPanel`** reads
   `margin-bottom:` instead of `bottom:` — same number, same law, renamed property.

## The measurements (real browser, docked at red-eye, plan + destination, plate up)

| | 1280×720 before | 1280×720 after | 390×700 before | 390×700 after |
| --- | --- | --- | --- | --- |
| `.map-hud` box | 313 | 353 | **0** | 228 |
| `.map-plot` | 22 | 61 | 16 | 16 (paged) |
| `.story-plate` | 184 | 66 | 832 | 136 |
| `.map-dest-panel` | 272 | 139 | 272 | 103 |
| painted at the panel's head | **`.map-plot`** | `dest` ×10 | **`.map-hud`** | `dest` ×10 |

Note the after-row at 390×700: the two LAYOUT rects still overlap by 16 px, and not one pixel is painted
there — the HUD's box ends above the panel and its content is clipped inside it. That is exactly why the
guard reads `document.elementFromPoint` and **not** bounding-box overlap, as the issue specified.

## Guard

`tests/SpaceSails.UiGate/TheDestinationPanelIsNeverPaintedOverTests.cs` — one boot (PlotPanelFits' own
owner scenario plus a destination), checked at 1280×720 and 390×700 by resizing in place. It samples the
panel's own head row at five x-fractions × two depths with `elementFromPoint`, asserts all ten are the
panel, and asserts its three premises out loud (the panel is showing, the Plotting panel is showing, the
panel is on the glass — a panel pushed off the bottom would otherwise pass by being nowhere).

## Trade-offs a reviewer should know

* The Plotting panel is **bigger** than before at 1280×720 (61 px of box vs 22) because the plate now
  gives. At 390×700 the HUD pages: the frame chip and the Plotting panel are below its fold and reached by
  scrolling — where before they painted over the nav panel, and at 320×480 off the window entirely.
* `.map-hud` is `pointer-events: none` with each child re-enabling its own, so its new scrollbar is not
  draggable; the wheel and touch drag over any child do scroll it. Worth an owner's eye.
* The story plate is smaller while the column is tight (66 px of its 174 at 1280×720) and scrolls inside
  itself. It is a transient story card and the Plotting panel is the work; that is the order chosen.
