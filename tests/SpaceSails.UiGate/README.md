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

The gate also **times** the boot path at the milestones it already awaits and fails if any regresses
past an honest budget — so a slow-load regression can never merge silently. The timings are logged on
**every** run (pass or fail), giving a free perf time-series in the CI logs:

| milestone | what it measures |
| --- | --- |
| front page interactive | nav → the Launch button is actionable and clicked |
| **front door pressable** (#161) | Launch click → the start picker's first real verb on screen **and enabled** |
| scenario boot complete | Launch click → a live desk tab bar (the WASM boot) |
| desk switch responsive | Captain tab click → the captain's room painted |
| whole canary (total) | the entire drive, as a backstop the per-milestone budgets don't localise |
| **longest synchronous boot block** (#161) | the worst single stage of the boot, read off the game's own `[SpaceSails] boot ·` console lines |

The last one is the only budget here about **the browser's dialog** rather than the captain's patience, and
it is what #161's acceptance actually names — *no "page unresponsive" warning on a cold load*. That warning
is not a function of how long the boot takes but of how long ONE uninterrupted block owns the main thread,
and every other row above is blind to it: a boot that finishes in fifteen seconds as twenty short stages
and one that finishes in fifteen seconds as four four-second freezes score identically on all of them, and
only the second gets a dialog. The number is the **game's own** — the boot prints one line per stage, each
stage sits between two yields, so a stage's cost is precisely a block the browser was not handed back — and
the gate keeps the worst line it hears. Nothing is instrumented for it.

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

## When a screen may be measured (issue #1234) — `GateReady`

`TheFollowDestButtonIsRealTests` went red on 2026-09-18 on a commit that touched no markup — *"Follow dest
sits on a different row from Follow Ship (y 165 vs 125)"* — and green on a re-run of the identical commit.
Probed on one boot, sampled every 250 ms, logged only when something moved:

```
[ 13155 ms] Follow Ship visible — THIS IS WHERE THE GATE MEASURED
[ 13173 ms] ⏭ Long coast ahead (30 d) — @388,125 w243 … Follow Ship@880,125 | Follow dest@978,125
[ 13968 ms] ⏭ Long coast ahead (29 d 23 @388,125 w274 … Follow Ship@911,125 | Follow dest@20,165
```

Eight hundred milliseconds after the boot door came down, the long-coast advert re-read its own countdown —
`30 d` → `29 d 23 h`, **31 px wider** — and `.btn-toolbar`'s `flex-wrap` (#123/#195) broke the row one
button earlier. The commit had nothing to do with it: the gate read a row that was **still being written**,
and which of the two rows it got was decided by how many milliseconds the box took to get from the door to
the bounding box (~20 ms on a quiet dev box; longer on a runner boiling four Chromiums).

**It was not the fonts.** This client has no web font at all — no `@font-face`, no font file in `wwwroot`, a
system stack in `app.css` — and the probe read `document.fonts.status = loaded` on its first sample.
`SettledAsync` still awaits `document.fonts.ready` (it costs nothing, and the day somebody adds a face is
the day that would otherwise become the cause), but the cause is the settle.

So `GateReady` is the one answer to "when may a pixel be read?", and every gate here goes through it:

| call | what it is |
| --- | --- |
| `page.BootDoorClosedAsync(t)` | the "Rigging the sails…" door **attached, then detached**. `GotoAsync` returns while the page is still an empty shell, so a bare wait-until-it-is-gone is satisfied by a door that has not been hung yet — **thirteen** call sites across nine gates had only the second half |
| `page.SettledAsync(scope)` | `document.fonts.ready`, two served animation frames, and then every laid-out box under `scope` identical across consecutive frame-pairs for 750 ms |

`SettledAsync` **throws** if the region never holds still, naming the box that kept moving and both of its
readings. It is not a retry and it never re-reads until it likes the answer: a gate that cannot get a still
screen has not measured anything. An element the page deliberately never rests — computed
`animation-iteration-count: infinite` — is excluded with its descendants, because a spinner is not evidence
that the layout is moving.

`TheGateMeasuresAStillScreenTests` is the law itself: the #1234 screen booted three times, read 0 ms,
1200 ms and 3000 ms after the door, and the toolbar must read as the **same controls on the same lines**
every time (digits knocked out of the labels — sim time may spend a character; a line may not change).
Proven red by deleting the one `SettledAsync` call inside it.

## The hidden-tab gate (issue #1244) — `TheBootIsTheSameSpeedWhenNobodyIsLookingTests`

Owner, live build, 2026-09-19, a Chrome window that was not in front: every slice of the staged boot cost
about a second (`the traffic lanes — freighter 1 of 8 — 1086 ms, 989, 860, 998…`), eighty-two slices in two
and a half minutes and still going, against ~26 ms each for the same URL in a tab he was looking at. Chrome
rations a hidden document's timers to roughly one a second, and the boot's one hand-back parks on a browser
timer by design.

**What actually makes a headless page hidden is nothing, and it is worth writing down.** Probed on this
branch, with Playwright's defaults and again with the three throttling-suppression arguments handed back
(`--disable-background-timer-throttling`, `--disable-backgrounding-occluded-windows`,
`--disable-renderer-backgrounding`):

```
alone:               visibility=visible hidden=false  5 timers=10ms  5 ticks=0ms
behind a 2nd page:   visibility=visible hidden=false  5 timers= 6ms  5 ticks=0ms
after the CDP calls: visibility=visible hidden=false  5 timers= 6ms  5 ticks=0ms
```

* `page.BringToFrontAsync()` on a second page does **not** hide the first — headless has no tab strip.
* `Emulation.setFocusEmulationEnabled` is accepted and forces focus **on**, the opposite of the question.
* `Page.setWebLifecycleState` rejects `"hidden"` outright (*"Unidentified lifecycle state"*) — it takes only
  `frozen` and `active`, and `frozen` stops the world rather than rationing it.
* A headless renderer never throttles a timer at all, with or without those flags.

So the gate does to the page **exactly what Chrome does to a background document**, in an init script, and
says so: `document.visibilityState` reads `hidden`, and `setTimeout`/`setInterval` are rationed to one call
a second. `MessageChannel` is deliberately left alone, because that is the whole of what the fix reaches
for. An emulation can lie, so the guard **asserts its own premise from inside the page after the boot** —
the document really did read hidden, five chained timers really cost ~5 s, and the boot really staged ~88
slices. The law itself is a comparison rather than a stopwatch reading (the two payloads differ by ~100×):
the same URL is booted twice in one run and the hidden boot may cost no more than 2× the visible one plus
6 s, with the payload's own boot budget doubled as an absolute backstop.

Measured here, interpreted payload: **base 102.0 s hidden / 17.5 s visible → 17.9 s hidden / 16.0 s
visible.** Both numbers are logged on every run, pass or fail.

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
