# SpaceSails.UiGate — the headless UI-verification gate (issue #293)

> Owner (2026-07-19, planning a two-day cruise where PRs get approved from a phone):
> *"there would have to be more testing done here."*

This is the confidence layer for remote approval. A real headless Chromium **boots the published
game** the whole way a player does and proves its **critical controls are present and clickable**.
A red check here means the game genuinely won't stand up — not that a unit assertion drifted.

It is **approach (d)** from Lab 34 (`labs/34-the-unclickable-lifeline/`). It complements — does not
replace — **approach (b)**, the browser-free geometry law (`SpaceSails.Core.RescueLifeline`) that
proves the rescue pill's z-band in xUnit. (b) reasons about the layout; (d) proves the live boot
path that reasoning assumes actually holds.

## What it drives (the control canary — small and stable on purpose)

One headless Chromium, one scripted voyage. Each step is a critical control the owner has to be able
to reach; the trial-clicks run Playwright's full actionability battery (visible · stable · enabled ·
not covered) **without** firing the control's side effect — the direct "would a real click land, or
is it buried?" question #293 is about.

1. Front page → **Launch** the Sol scenario (real click).
2. Wait out the boot — the "Rigging the sails…" spinner detaches on world-ready.
3. Start-picker front door → **New voyage** berth (real click).
4. The **desk tab bar** renders (and we booted into the game, not back onto the picker).
5. **Captain** desk tab → its room opens (desk switching works).
6. Captain's **"Set course to a start point…"** is reachable (trial click, no jump).
7. **Nav** desk tab → the flight HUD returns (switching both ways).
8. The **pilot banner** ("who has the ship", #127) is reachable (trial click).
9. The console is clean — no uncaught JS, no unexplained `console.error`.

## Load-speed budget (owner, cruise 2026-07-19: *"Maybe add CI test to catch too slow loads."*)

The gate also **times** the boot path at three milestones it already awaits and fails if any regresses
past an honest budget — so a slow-load regression can never merge silently. The timings are logged on
**every** run (pass or fail), giving a free perf time-series in the CI logs:

| milestone | what it measures |
| --- | --- |
| front page interactive | nav → the Launch button is actionable and clicked |
| scenario boot complete | Launch click → a live desk tab bar (the WASM boot) |
| desk switch responsive | Captain tab click → the captain's room painted |
| whole canary (total) | the entire drive, as a backstop the per-milestone budgets don't localise |

**Budgets are keyed to the AOT build Pages ships** (issue #371 Phase 2). Measured on a dev box (3 runs,
worst): front page 1.25 s · boot 4.27 s · desk switch 0.18 s · total 5.86 s. The #382 CI run put the whole
AOT canary at ~11 s (CI ≈ 1.9× this box), so budgets are anchored to that CI baseline with ~2.5-3×
headroom — **generous** (never flake on a slow runner) but **honest** (catch a milestone ballooning past
double). Shipped budgets: front page **10 s** · boot **20 s** · desk switch **8 s** · total **30 s**. A
breach names the numbers (`boot took 41.2s, budget 20s`) and still uploads the failure artifacts.

The gate auto-detects the payload straight off the served `wwwroot` — an AOT `dotnet.native.*.wasm` is
~18 MB vs ~1.5 MB interpreted — so a plain local `dotnet publish` (interpreted, ~100× slower boot) gets a
much looser ceiling instead of false-failing. Nothing to set in CI; no game code touched.

## The tall-card gate (issue #735) — `TallCardTests`

The 2026-08-06 smoke run found the Enceladus restore card rendering its one button, *"Board the
rustbucket"*, **below the fold**: the modal did not scroll, the backdrop did not dismiss, and the player
was stuck on a story card until they resized the browser. That is #680's law one level up — *in the DOM is
not on the screen* — and it is the second question only a real layout can answer, so it lives here.

Three tests, all driven through the real `?death=impact` boot (#621's cheat stages the genuine death
pipeline), at **390 × 700** — a phone in portrait, because the owner's second screen is a phone and every
viewport there is shorter than the desktop window this failed on:

1. the restore card's primary action lies **inside** the screen (hit-box + Playwright actionability);
2. a card taller than the screen is **capped and scrolls inside itself**, and its action row is still on
   screen with the card scrolled back to the top (it is pinned, not merely reachable);
3. **Enter** presses the single visible action of an open card — keys typed at whatever the app itself
   focused, so the path under test is the player's, not the harness's.

Test 2 asserts its own premise first (the card really is taller than 700 px) so a future edit that makes
the card short cannot leave these guards passing while proving nothing.

## The readability gate (issue #782) — `EveryTextReadsTests`

Owner ruling, 2026-08-08 evening, live over the counter menu that had gone dark (#780): *"All text needs
to have good contrast from the background as a general ruling… also BIG ENOUGH FONTS — we can scroll the
menu."*

One test, one boot: `?stool=1&neighbour=1` at **390 × 700**, which puts a captain on a stool at the B1
counter with the priced menu open, the first-ground lesson card still up and the deck's HUD around it — a
screenful of every kind of text this game has, standing on **the one hall that wears a gen-AI painting**
(`UndergroundComplex.CantinaHallArtUrl`, drawn onto the deck canvas at `HallArtAlpha`).

For every visible text run it computes:

* **the ink** — computed `color`, faded by every ancestor `opacity`;
* **the ground it actually sits on** — background-colours *and gradient stops* composited up the ancestor
  chain, stopping at `.map-page` because the deck canvas is a **sibling painted between** the page's own
  fill and every overlay above it. When that stack never reaches opacity — which is the case that matters
  — it reads **the canvas's own pixels** under the run's box with `getImageData` and composites the stack
  over those. The hall photograph is same-origin, so the canvas is untainted and the bytes are real: this
  is a measurement, not an estimate;
* **the contrast** — WCAG 2.1 relative luminance, floor **4.5:1** (AA body text);
* **the size** — computed `font-size`, floor **14 px**.

Runs of pure emoji are skipped (the font paints those in its own colours, so `color` says nothing about
them), and a run entirely past the fold of a scrolling card is reported as `offscreen` — it has no pixel
behind it, so it has no ratio, and whether it should be off the fold is `TallCardTests`' question.

The guard states both its premises out loud so it can never pass while proving nothing: the run it was
written from read **55 text runs, 15 of them grounded on the canvas's own pixels**, and it fails if
either number collapses.

The browser-free twin — `SpaceSails.Client.Tests.EveryTextReadsTests` — sweeps every shipped stylesheet
and every art slot in `Map.razor` for the same law, so a new picture with a new caption on it is caught
the day it is written rather than the day somebody thinks to drive to it.

## The no-button-zone gate (issue #236) — `TheBannerBandIsANoButtonZoneTests`

Owner ruling, mid-car-run (2026-07-17): *"generally we should try to keep the ship status real estate
(under them) button free in all screens to avoid unpressable buttons."*

The band is whatever `.map-topstack` lays out — desk tabs, the who-flies banner, the #166 alert strip —
at the height the banner really grew to this frame. Two laws, one boot each:

1. **No pressable control is laid out inside the band, on any screen.** Swept across 23 screens — seven
   desks at two berths at 1280×720, the same seven at 390×700 (where the tab bar wraps and the band is
   181 px rather than 111), the berth-picker gate and the BUSTED interrupt — asking every `button` /
   `input` / `a[href]` under `.map-page` whether its box is in the band. Three ways to be allowed there,
   and only three: it is the band's OWN (a descendant of `.map-topstack` — the banner growing into its
   own real estate), it is **named furniture** whose row must ALSO prove itself by `elementFromPoint`
   (`.map-layers`, `.nav-search`, `.desk-chip-strip` — all three paint above the masthead, so the gate
   makes them show it), or a **modal owns the screen** (#236's own exemption, by name). Both tables must
   stay complete in both directions: a new family with no row goes red, and a row nobody meets goes red
   too.
2. **A desk clears the band by FLOWING under it, never by a number.** Walks every ancestor from
   `.desk-content` up to `.map-flowcolumn` and requires each one in flow. A desk in flow cannot be
   covered by a growing banner — it is pushed. That is what deleted `--desk-top-clearance`.

Proven red by restoring the clearance the lane deleted (`padding-top: 5.75rem`, absolute): 27 offences,
the first five of them the Captain's Orders/Status/Tutorials/Ledger/Crew toggle — the very row the owner
caught.

## Run it locally

```bash
dotnet test tests/SpaceSails.UiGate
```

That is genuinely all — the fixture publishes the client itself (into temp) and installs Chromium on
first use, so a bare `dotnet test` is self-contained. First run is slow (a Release WASM publish plus a
one-time browser download); after that the drive itself is ~20 s.

To skip the in-test publish (much faster to iterate), point it at a publish you already have:

```bash
dotnet publish src/SpaceSails.Client -c Release -o publish
SPACESAILS_PUBLISH_DIR=./publish dotnet test tests/SpaceSails.UiGate
```

**Gotcha (same as `tools/playthrough`):** interpreted WASM under a plain publish (no AOT) is ~100×
slower than native, so timeouts here are generous and keyed on real signals (element visible), never
sleeps. The page load retries once; on failure a screenshot + console/step logs are written to
`SPACESAILS_UIGATE_ARTIFACTS` (or `bin/.../ui-gate-artifacts`) for CI to upload.

## Env vars

| var | purpose |
| --- | --- |
| `SPACESAILS_PUBLISH_DIR` | host this pre-published `wwwroot` instead of publishing in-test (CI sets it) |
| `SPACESAILS_UIGATE_ARTIFACTS` | where the failure screenshot + logs land |
| `SPACESAILS_UIGATE_NO_BUDGET` | set to `1` to log timings but NOT enforce the load-speed budget (local debugging only; CI never sets it) |

## In CI

The `ui-gate` job in `.github/workflows/ci.yml` runs on every PR and main push (same workflow as the
Core suite, so a red gate is a red check). It publishes the client once, installs Chromium
`--with-deps`, runs the gate, and always uploads the artifacts.

**Deliberately kept out of `SpaceSails.slnx`** so `dotnet test SpaceSails.slnx` stays the fast,
browser-free Core suite. This gate runs as its own job by project path.
