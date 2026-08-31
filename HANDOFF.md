# HANDOFF — #950 · 🎯🗺 the navigation-target panel stops losing its top edge to the Plotting panel

Worktree `D:/repo12/wt/950`, branch `fix/950-scrub-clipped`, base `our-own-ship-has-compartments`.

## What #950 actually still was

The issue's own screenshots (Plotting panel run off the bottom, "I cannot remove the insertion step",
"the disarm auto-insertion button does nothing") were already resolved by #965/#992/#994/#997: the old
"Insert at X pass" chip is gone, the arrival is a terminal step INSIDE the flight plan's own
`CappedScrollPanel` (scrolls, never runs off-window — `PlotPanelFitsTheWindowTests` proves it), and the
Disarm control lives in that step's own row. The issue stayed open only for three unrelated design rulings
the owner never answered (#957's autopilot-correction search), not because the clipping itself persisted.

Investigating anyway (docked, a flight plan, and — matching the owner's SECOND screenshot exactly — a
navigation destination set) turned up a real, still-live, and previously untested sighting one layer over
from where #992/#994/#997 already looked:

**`.map-dest-panel` (M25's navigation-target panel — "the whole point of navigating there") and `.map-plot`
(the Plotting panel) geometrically overlap by design, and nothing decided which one wins the shared
pixels.** Measured at BOTH 1280×720 and 390×700, docked with a plan and a destination set:
`.map-plot`'s own bottom edge lands 12 px inside `.map-dest-panel`'s top edge at both sizes. The two panels
were never made to coordinate — `.map-plot` lives in #992's flex column (`.map-flowcolumn`), `.map-dest-panel`
is absolutely positioned against the window's own bottom-centre — so whichever painted on top was whatever
the browser's default DOM-order stacking happened to give: `.map-hud` (`chrome + 12`) OVER `.map-dest-panel`
(`chrome + 2`), meaning the Plotting panel's edge hid a sliver of the ONE panel that answers "did the course
actually get you there" — while the captain was looking at exactly that panel, having just set the
destination.

## The fix

One line, `src/SpaceSails.Client/Pages/Map.razor.css`: `.map-dest-panel`'s z-index moves from
`chrome + 2` to `chrome + 13` — one above `.map-hud`'s `chrome + 12`, one below `.map-topstack`'s
`chrome + 14` (the pilot banner still wins over everything). The two panels still geometrically overlap by
the same handful of pixels; nothing about either panel's position or size changed. What changed is which one
paints on top when they touch: the navigation target's own words, not a stray edge of the Plotting panel.

## Guard

`tests/SpaceSails.UiGate/TheDestinationPanelIsNeverPaintedOverTests.cs` —
`The_destination_panel_is_never_painted_over_by_the_plotting_panel`, checked at BOTH 1280×720 desktop and
390×700 (TallCardTests' own phone number) in one boot (dock, plot a 4-row plan with the last editor open —
`PlotPanelFitsTheWindowTests`'s own owner scenario — then set Earth as the destination). It does NOT trust
bounding-box overlap alone (two boxes can overlap while both stay fully legible, or while one is fully
hidden — geometry alone proves nothing about paint); it reads `document.elementFromPoint` at the centre of
the two panels' own measured overlap and asserts the destination panel is what a real screen actually shows
there.

**RED PROOF (done):** reverted the z-index to `chrome + 2` and reran — fails immediately at 1280×720
("the pixel where the Plotting panel and the destination panel overlap (478,442) is painted by \"plot\", not
the destination panel"). Restored the fix and reran green. Also independently confirmed `HudCollisionTests`,
`PlotPanelFitsTheWindowTests`, and `TallCardTests` all still pass with the fix in place (ran together, no
regressions).

## Investigation dead ends (for whoever picks this up next)

Two other angles were explored and abandoned — not because they're wrong to worry about, but because
neither is fixable within this change's blast radius without new regressions or an unprovable claim at the
required viewport sizes:

1. **The arrival story-plate (`.story-plate`) can still squeeze `.map-plot` to a sliver.** Docked at a great
   port with "🛬 THE LONG WALK IN" showing and Plot open, `.map-plot` collapses to a ~78 px box around
   ~425 px of its own content at 1280×720 — genuinely a DEFICIT (topstack + readouts-floored + toolbar +
   frame + the panel's own head already total ~717 px of a 720 px window with NO plate at all), not a
   surplus the plate's own `margin-top: auto` is stealing. A `margin-top: 0` fix for that auto margin (real,
   and harmless — verified against the full existing UiGate suite) has **zero observable effect** at either
   mandated viewport size, because there is no surplus at either size for it to matter to. Making the panel's
   own box match its full natural content (dropping `.capped-scroll`'s `overflow-y: auto`) DOES fix the
   sliver, but geometrically extends `.map-plot` far enough to fail
   `HudCollisionTests.The_story_plate_never_covers_the_plotting_panel` and — at 390×700 — exposes a SEPARATE,
   pre-existing bug: `.map-hud` itself collapses to a genuine 0 px box (its own long-standing `min-height: 0`
   absorbing 100% of a deficit `.map-topstack` and the plate leave it, reproduced on the untouched baseline
   too). Fixing the sliver properly needs a real product decision about the plate's own footprint on the
   three HUD desks — not something to slip in as a side effect of #950.
2. Confirmed with a scratch test (not shipped) that even the CURRENT/baseline `PlotPanelFitsTheWindowTests`
   scenario, run at 390×700 with no plate at all, already satisfies that test's own two assertions (the
   remove control and the last ± row are both reachable via scroll) — so extending that specific test to
   phone width would not currently prove anything RED.

## Verification run (see terminal, not re-pasted here)

- `TheDestinationPanelIsNeverPaintedOverTests`: green with the fix, red on revert (both confirmed).
- `HudCollisionTests`, `PlotPanelFitsTheWindowTests`, `TallCardTests`: all green with the fix in place.
- Core + Client full suites: run before the PR (see PR body for the result — this file was written while
  they were still running under heavy machine contention from other concurrent sessions).
