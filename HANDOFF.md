# HANDOFF — #950 · investigated thoroughly; no safe code fix found tonight

Worktree `D:/repo12/wt/950`, branch `fix/950-scrub-clipped`, base `our-own-ship-has-compartments`.

## What #950 actually still was

The issue's own screenshots (Plotting panel run off the bottom, "I cannot remove the insertion step",
"the disarm auto-insertion button does nothing") were already resolved by #965/#992/#994/#997: the old
"Insert at X pass" chip is gone, the arrival is a terminal step INSIDE the flight plan's own
`CappedScrollPanel` (scrolls, never runs off-window — `PlotPanelFitsTheWindowTests` proves it), and the
Disarm control lives in that step's own row. The issue stayed open only for three unrelated design rulings
the owner never answered (#957's autopilot-correction search), not because the clipping itself persisted.

## What was found instead, and why it is NOT fixed here

Investigating anyway (docked, a flight plan, and — matching the owner's SECOND screenshot exactly — a
navigation destination set) found a real, small, cosmetic sighting: `.map-dest-panel` (M25's
navigation-target panel) and `.map-plot` (the Plotting panel) geometrically overlap by design — measured at
both 1280×720 and 390×700, docked with a plan and a destination set, `.map-plot`'s own bottom edge lands
12px inside `.map-dest-panel`'s top edge at both sizes. The two panels were never made to coordinate: one
lives in `.map-flowcolumn` (#992's flex column), the other is absolutely positioned against the window's
own bottom-centre corner.

**First attempt (reverted): raise `.map-dest-panel`'s z-index above `.map-hud`'s.** This "fixed" the sliver —
but `.map-hud` (`position: relative` + its own `z-index`) is ONE stacking context. Raising a sibling above it
does not selectively out-rank the one child (`.map-plot`) that was overlapping — it out-ranks EVERY control
`.map-hud` carries, including the safety-critical ones. `CriticalControlsTests.EveryCriticalControl_IsReachable_AtEverySize`
(#299 — every always-must-be-pressable affordance, reachable at five viewports from 320×480 to 1280×800)
caught this immediately: with the z-index raised, `.map-dest-panel`'s own bottom-centre geometry genuinely
covers `autopilot-disengage` at **every** tested size, including plain desktop 1280×800 — not only the
narrow ones. A 12px cosmetic sliver is not worth making the autopilot-disengage button unreachable. Reverted
in full (CSS z-index, the mirrored `OverlayBands.MapDestPanel` Core constant, and the browser guard that had
asserted the z-index win — all three are back to their pre-session values, verified: `CssZBandSyncTests` and
`CriticalControlsTests` both green again, 62/62).

**Why no other fix shipped tonight.** The geometrically-correct fix — matching #994's own precedent for the
identical class of bug (the arrival story-plate vs. the Plotting panel) — is to move `.map-dest-panel` INTO
`.map-flowcolumn`'s own flex arithmetic so the two panels can never share a pixel at all, geometry rather than
a z-index number deciding it. That is real, valuable work, but it is bigger than tonight's investigation
budget: `.map-flowcolumn` already has two known-fragile edges from #950/#992/#994's own history (a squeezed
`.map-plot` sliver under `.map-topstack`+`.story-plate` pressure at 1280×720 with a busy readouts block, and
a genuine `.map-hud` 0-height collapse at 390×700 with the plate up, both pre-existing and unrelated to this
change — see below) and adding a THIRD flow-column participant without a careful pass risks compounding
those rather than fixing the new one cleanly.

## Investigation dead ends (for whoever picks this up next)

1. **The arrival story-plate (`.story-plate`) can still squeeze `.map-plot` to a sliver.** Docked at a great
   port with "🛬 THE LONG WALK IN" showing and Plot open, `.map-plot` collapses to a ~78px box around
   ~425px of its own content at 1280×720 — genuinely a DEFICIT (topstack + readouts-floored + toolbar +
   frame + the panel's own head already total ~717px of a 720px window with NO plate at all), not a
   surplus the plate's own `margin-top: auto` is stealing. A `margin-top: 0` fix for that auto margin (real,
   and harmless — verified against the full existing UiGate suite) has **zero observable effect** at either
   mandated viewport size, because there is no surplus at either size for it to matter to. Making the panel's
   own box match its full natural content (dropping `.capped-scroll`'s `overflow-y: auto`) DOES fix the
   sliver, but geometrically extends `.map-plot` far enough to fail
   `HudCollisionTests.The_story_plate_never_covers_the_plotting_panel` and — at 390×700 — exposes a SEPARATE,
   pre-existing bug: `.map-hud` itself collapses to a genuine 0px box (its own long-standing `min-height: 0`
   absorbing 100% of a deficit `.map-topstack` and the plate leave it, reproduced on the untouched baseline
   too). Fixing the sliver properly needs a real product decision about the plate's own footprint on the
   three HUD desks — not something to slip in as a side effect of #950.
2. Confirmed with a scratch test (not shipped) that even the CURRENT/baseline `PlotPanelFitsTheWindowTests`
   scenario, run at 390×700 with no plate at all, already satisfies that test's own two assertions (the
   remove control and the last ± row are both reachable via scroll) — so extending that specific test to
   phone width would not currently prove anything RED.
3. **The `.map-dest-panel`/`.map-plot` z-index swap (this session's own attempt).** Written up above — kept
   here too because the failure mode (one stacking context, all-or-nothing) is the load-bearing fact for
   whoever attempts a z-index-only fix again: it cannot work. The flow-column approach is the only
   structurally sound path.

## Recommendation

No code change ships in this PR. Recommend either (a) closing #950 as substantively resolved by #965/#992/
#994/#997, with the residual `.map-dest-panel`/`.map-plot` sliver and the story-plate squeeze filed as their
own smaller, separately-scoped issues, or (b) keeping it open specifically for #957's three unanswered
design rulings, which is the only thing actually still pending on it.

## Verification run

- `CssZBandSyncTests`, `CriticalControlsTests`: 62/62 green on the FINAL (fully reverted) state.
- `HudCollisionTests`, `PlotPanelFitsTheWindowTests`, `TallCardTests`: all green (unaffected — no shipped
  change).
- `SpaceSails.Client.Tests`: 1375/1375 green (run in full before the reverted state, unaffected regardless
  since nothing here touches Client logic).
- `SpaceSails.Core.Tests`: 4065/4066 green with the (bad) z-index fix in place — the one failure was
  `CssZBandSyncTests` catching the CSS/Core drift the z-index change introduced, exactly as it's designed to.
  62/62 on the affected subset after the full revert (see above); a full Core re-run was not repeated a
  third time given the ~40 minute cost per run and that the affected files are back to their original values,
  verified line-by-line.
