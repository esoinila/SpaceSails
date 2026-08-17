# Lab 46 — What a draw costs

*Standard performance-engineering territory, like Labs 10 and 45 — nothing fictional in this lesson. It is
also the first lab in this folder whose results table ships **EMPTY**, and the reason is the whole point of
the lab: nobody here can honestly take the reading. Only the owner can, in a foreground tab, on his own
machine.*

## The idea

Lab 45 was asked for #841's number and came back with half of it:

> **#841's gate — does wall/fixture count measurably matter at sim level?** … So **#841's viewport culling
> cannot be justified on sim cost**: the sim-side dependence on wall and fixture count is real, linear, and
> worth about a third of one percent of a frame at the game's own limits. **If culling is worth doing it has
> to be justified on DRAW cost, and this lab could not measure draw cost (Section D).**
>
> — `labs/45-what-a-frame-costs/README.md`

Section D of that lab is one paragraph of honest refusal:

> The renderer is Blazor plus a 2D canvas behind JS interop: there is no headless path to it and it carries
> no internal frame counter to read. The only remaining road is a browser, and this repo's standing law is
> that a timing taken from an MCP-driven tab is **invalid** — the tab is `document.hidden`, so rAF is
> throttled and timers are clamped. A number obtained that way would have to be disowned in the same
> paragraph that printed it.

Nobody has measured it since. #841 has been open on a missing number for weeks, and both of its halves —
viewport culling and room-granular content reveal — are lanes somebody would have to spend a day on.

**So this lab is not a program. It is an instrument fitted to the game, and a recipe for reading it.** The
harness is the shipping client with `?perf=1` on the URL; the laboratory is a real Chrome window, in the
foreground, on the machine that matters. The tables below are left blank on purpose. Filling them in is the
experiment.

## Run it

There is no `dotnet run` here, and that absence is the finding Lab 45 already published: **a draw cannot be
timed off a canvas that does not exist.** A console harness would have to fake a renderer, and what it timed
would be the fake.

```
https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=1&perf=1
```

…or, running locally, `/map?secretlab=deep&land=1&floor=1&perf=1`. It is also a button in the game's own
front door: **⚙ DEV START SITES → ⏱ "What a draw costs — the furnished floor, timed"**.

That URL is B1 of a deep site — the floor the whole issue is about, because B1 is where #834's offices,
#759's glazing, #813's park block, the desks and the cubicles all are. It is the floor whose furniture #841
proposes to cull.

**READ IT IN A REAL, FOREGROUND, FOCUSED TAB.** Not an MCP-driven tab, not a background tab, not a tab with
DevTools' own throttling on. A hidden tab throttles `requestAnimationFrame` to something like 1 Hz and
clamps its timers, so every number you would copy out of one is a number about the *browser's power saving*.
This repo has a standing rule about it and Lab 45 obeyed it by publishing nothing.

## What is being timed, and what is not

The client draws the walked view through one conductor, and #870 lane 7b already cut that conductor into
named passes with `THE ORDER IS THE PICTURE` written over them. That split is the seam the clock goes on:
**one timestamp after each pass, so a pass's cost is the gap to the one before it.** Nothing is wrapped and
nothing is re-entered.

| what | where | what it is |
|---|---|---|
| **the 18 passes** | `DeckView.Frame.cs` · `Draw` | `PaintTheGround`, `HideWhatNobodyHasLookedInto`, `FillTheStructure`, `FillTheFurniture`, `DrawTheWalls`, `DrawTheDoors`, `NameTheRooms`, `MarkTheGround`, `DressTheShip`, `DrawTheSeats`, `DrawTheFigures`, `PaintTheDark`, `DrawTheSentries`, `DrawWhatTheFanHeard`, `CountDownTheOverload`, `DrawTheConsoles`, `DrawTheCaptain`, `DrawTheInstruments` |
| **the flush** | `CanvasRenderer.EndFrame` → `RendererInterop.DrawFrame` | `FlushToTheCanvas` — the ONE line of the frame that crosses into JavaScript. Every pass above it only appends floats to an array; **nothing is drawn until here.** |
| **the whole Draw** | `DeckView.Draw`, end to end | `TOTAL_Draw` |
| **the whole walked frame** | `Map.DrawWalkFrame` | `TOTAL_DrawWalkFrame` — the Draw plus the surface HUD the page builds before it can call Draw at all (blips, smudges, ghosts, beacons, the swept grid). Draw-side work by any honest reading, and not inside the conductor. |

(The issue asked for "the pen's 17 passes". The conductor calls **eighteen**. The eighteenth is `PaintTheDark`
— easy to forget because it only does anything on an unlit floor, and it is marked anyway, for the reason
below.)

**A pass behind an `if` is marked OUTSIDE the `if`.** `DressTheShip` runs only on a ship deck and
`PaintTheDark` only on a dark floor, and both are marked regardless, so the table has the same rows on every
world. "Dressing the ship cost nothing on this floor because there is no ship" is a reading; a row that came
and went between frames would make a rolling window a comparison between two different questions.

**What is NOT timed, and cannot be from inside the page:** what the *browser* then does with the flushed
buffer. `renderer.js` decodes the float array and issues canvas calls, and the compositor paints whenever it
paints. `FlushToTheCanvas` is the C# side of that boundary — the marshalling and the synchronous JS work —
and it is the closest thing to a draw cost this game can measure about itself. If it turns out to dominate,
the next instrument is Chrome's own performance panel, not another line of C#.

## What the clock can resolve, and what it cannot

`Stopwatch.GetTimestamp()` works in WASM, but underneath a browser it is `performance.now()`, which is
deliberately coarsened against timing attacks — **5 µs on a cross-origin-isolated page, up to 100 µs on an
ordinary one**. GitHub Pages is an ordinary one.

So: **a single pass in a single frame is not a reading.** Most passes will land on a tick boundary and print
as 0.000 or 0.100 with nothing in between. The rolling **120-frame mean** is the reading — 120 samples of a
quantised clock against a duration that actually moves recover a mean far finer than one tick, because the
quantisation dithers. Read the means. Treat any single-frame `max` at exactly one tick as noise. Where a
mean is stuck at exactly 0.000 across a whole window, the honest report is "under the clock's floor", not
"free".

**And expect the mean to sit ABOVE the p95 on some rows.** That is not a broken percentile: one cold frame —
a JIT warm-up, a first image decode, a GC — lands far outside the 95th percentile and drags the mean over it.
It was observed on the very first CI run of this probe's own guard (`DrawTheInstruments`: mean 0.447 ms,
p95 0.069 ms, max 46.5 ms) and the guard was corrected rather than the data. When a row looks like that, the
**p95 is the steady state and the max is a one-off you should go and find**; the mean is the one number that
is telling you about both at once.

This is the same class of trap Lab 45 sprang on itself twice (tiered JIT, early exit) and published. It is
written down here **before** the table is filled in, so that a row of zeroes does not get read as a finding.

## How to read it — the recipe

1. Open the URL **in a normal Chrome window, foreground, focused**. Note the window size; the deck scales to
   the viewport, so 1920×1080 and a phone-shaped 390×700 are two different experiments and both are worth
   running.
2. A line appears across the top of the deck (`.perf-hud`), refreshed four times a second:

   ```
   PERF · frame 0.00 ms mean · 0.00 p95 · 0.00 max · draw 0.00 · flush 0.00 · furniture 0% · dearest: … · n=120
   ```

   That line is for steering — walk somewhere and watch which way it moves. It is not the thing you copy.
3. Open the browser console (F12 → Console). **Every 120 frames (two seconds at 60 fps)** the game prints
   one block, in a fixed shape:

   ```
   [perf] window=120 frames=240 rows=21
   [perf] pass=PaintTheGround mean=0.000 p95=0.000 max=0.000
   [perf] pass=FillTheFurniture mean=0.000 p95=0.000 max=0.000
   …
   [perf] pass=FlushToTheCanvas mean=0.000 p95=0.000 max=0.000
   [perf] pass=TOTAL_Draw mean=0.000 p95=0.000 max=0.000
   [perf] pass=TOTAL_DrawWalkFrame mean=0.000 p95=0.000 max=0.000
   ```

   Filter the console on `[perf]` and copy the last complete block. That is a row of the table below.
4. **Take at least four readings**, because the whole question is which of them differ:
   - **the park**, mid-green (`&perf=1` on `/map?park=1` — the most furnished square metre in the game);
   - **the cantina hall**, standing at the counter (the shipped `?perf=1` row lands near here);
   - **a bare corridor** on the same floor, with as little on screen as the floor allows;
   - **B2** (`&floor=2`), which Lab 45 measured as the LIGHT world — 135 wall segments against B1's 465.
5. Stand still for the reading. Walking changes what is on screen, which is the experiment — but a mean taken
   while you cross a doorway is a mean of two rooms.
6. Note the machine, the browser, the window size and the date beside every row. Lab 45's rows carry theirs;
   these have to as well, or the table is four numbers from four different experiments.

## The results — **EMPTY, and only the owner can fill it**

Copy each `[perf]` block into a row. Everything in milliseconds. **One 60 fps frame is 16.67 ms.**

**Machine / browser / window / date:** _(fill in — e.g. "Win 11, Chrome 1xx, 1920×1080, 2026-08-__")_

### A · Where you were standing

| where | URL | `TOTAL_DrawWalkFrame` mean | p95 | max | `TOTAL_Draw` mean | `FlushToTheCanvas` mean | % of a 16.67 ms frame |
|---|---|---|---|---|---|---|---|
| the park, mid-green | `/map?park=1&perf=1` | | | | | | |
| the cantina hall | `/map?secretlab=deep&land=1&floor=1&perf=1` | | | | | | |
| a bare corridor, B1 | (walk there from the row above) | | | | | | |
| B2 — the light floor | `/map?secretlab=deep&land=1&floor=2&perf=1` | | | | | | |
| the regolith | `/map?dock=the-tilt&site=0&land=1&perf=1` | | | | | | |
| her own deck | `/map?dock=the-tilt&ashore=0&perf=1` | | | | | | |

### B · Where the time went, on the heaviest of those

Fill this one from the single dearest row of table A — that is the frame culling would have to pay for.

| pass | mean | p95 | max | % of `TOTAL_Draw` |
|---|---|---|---|---|
| `PaintTheGround` | | | | |
| `HideWhatNobodyHasLookedInto` | | | | |
| `FillTheStructure` | | | | |
| **`FillTheFurniture`** | | | | |
| `DrawTheWalls` | | | | |
| `DrawTheDoors` | | | | |
| `NameTheRooms` | | | | |
| `MarkTheGround` | | | | |
| `DressTheShip` | | | | |
| **`DrawTheSeats`** | | | | |
| `DrawTheFigures` | | | | |
| `PaintTheDark` | | | | |
| `DrawTheSentries` | | | | |
| `DrawWhatTheFanHeard` | | | | |
| `CountDownTheOverload` | | | | |
| **`DrawTheConsoles`** | | | | |
| `DrawTheCaptain` | | | | |
| `DrawTheInstruments` | | | | |
| `FlushToTheCanvas` | | | | |
| **`TOTAL_Draw`** | | | | 100% |
| `TOTAL_DrawWalkFrame` | | | | |

### C · The same thing on a phone-shaped window

The owner's phone is first-class here (#735/#754/#782), and it is also the viewport where a culling win would
be biggest — fewer pixels, but the same plan handed to the pen. Resize to roughly **390 × 700** and re-take
the dearest row.

| where | window | `TOTAL_DrawWalkFrame` mean | p95 | furniture passes as % of `TOTAL_Draw` |
|---|---|---|---|---|
| | 390 × 700 | | | |
| | (desktop, for comparison) | | | |

## The decision rule — written down BEFORE the numbers

This is the point of leaving the table empty. A threshold agreed after the data arrives is not a threshold;
it is a description. So:

> **#841's culling — viewport culling, or room-granular content reveal — is worth a lane if EITHER:**
>
> **(X)** the furnished floor's `TOTAL_DrawWalkFrame` **p95 exceeds X ms** at the owner's phone-class
> viewport; **OR**
>
> **(Y)** the furniture passes — `FillTheFurniture` + `DrawTheSeats` + `DrawTheConsoles` — are **more than
> Y % of `TOTAL_Draw`**.
>
> **X and Y are the owner's call.** The lab suggests **X = 4 ms** and **Y = 30 %**.

**Why 4 ms.** A 60 fps budget is 16.67 ms. The sim's own worst measured spike is Lab 45's `AutoWalk.Plan` at
6.4 ms native, and this game ships to WASM where that is worse; the client also has to leave room for Blazor's
own render tree, the 5 Hz HUD `StateHasChanged`, and the browser's compositor. 4 ms is roughly a quarter of
the budget spent on drawing one static floor — past that, the draw is a first-class cost rather than a
rounding error, and a viewport cull (which is O(what is on screen) instead of O(what is on the floor)) is the
standard, boring, obviously-correct answer.

**Why 30 %.** Culling furniture only pays for what furniture costs. If the three furniture passes are a fifth
of the draw, the very best possible cull saves a fifth of the draw and the lane buys almost nothing — the
time is going somewhere else (the flush, the walls, the figures) and *that* is the lane. At a third or more,
cutting the off-screen half of it is a visible win. Note that these two conditions are deliberately an **OR**:
a floor that is slow for some other reason still deserves a look, and furniture that dominates a cheap frame
is a trap waiting for a bigger floor.

**And the rule's own escape clause, which matters most:** if `FlushToTheCanvas` dominates `TOTAL_Draw`, then
**neither** half of #841 is the lane. Culling reduces the number of primitives recorded, which shortens the
buffer — that helps a flush too — but if the cost is the interop crossing itself rather than the buffer's
length, the answer is a different one entirely (fewer, larger batches; or not re-registering images per
frame) and #841 should be closed as measured-and-refuted rather than half-built.

## The guard, and what it does and does not prove

`tests/SpaceSails.Client.Tests/WhatADrawCostsTests.cs` runs in CI, on a runner with no canvas at all. **Every
duration it sees is worthless and it asserts none of them.** What it asserts is the thing that decides whether
the owner's numbers mean anything:

- **the row names ARE the conductor's passes**, in the conductor's order — read back out of
  `DeckView.Frame.cs` rather than typed into the test. The marks are string literals and not `nameof` for
  exactly this reason: rename a pass and leave its mark, and the guard fails naming both spellings. That is
  this repo's fifth bug class aimed at a measurement — *a table that labels a cost with the name of code that
  did not run is worse than no table, because somebody will act on it.*
- **every row is present on every world**, ship and hive floor and dark floor alike, so the table cannot
  change shape mid-window;
- **every reading is finite and non-negative, and mean ≤ p95 ≤ max**, over a window that stops at 120;
- **the console block parses against the exact regex this recipe tells you to grep for**, one line per row,
  emitted once per window;
- **off means off**: an unarmed `DeckView` has no probe object at all, and an armed one draws a
  byte-identical transcript to an unarmed one — a measurement that changes what it measures is not one.

## Break it yourself

1. **Prove the instrument can see a cost.** Lab 45's Section E is the model: plant one. Add a 2 ms busy-spin
   inside `FillTheFurniture`, reload with `?perf=1`, and check that the furniture row moves by ~2 ms and
   `TOTAL_Draw` by the same — and, more interestingly, check whether the frame rate moves at all, because on
   a machine that is already GPU-bound it may not. **Do this before trusting any row of the table above.**
   A harness that cannot resolve a planted cost has measured nothing, and nobody has run this control yet.
2. **Find the clock's floor.** Print `1.0 / Stopwatch.Frequency` and then time an empty loop; the smallest
   non-zero gap the page can see is the number under which every row above is a zero that means "smaller
   than this", not "free". Write it at the head of the table.
3. **Cross-check against the browser's own instrument.** Chrome's performance panel records the same frames
   from outside. If its "scripting" bar and `TOTAL_DrawWalkFrame` disagree by more than the clock's floor,
   one of the two is measuring something else — and finding out which is worth more than any row in table A.
4. **The half nobody can reach from inside.** Everything after `FlushToTheCanvas` — the JS decode loop in
   `renderer.js`, the canvas calls, the compositor — is invisible to this probe. Instrument `drawFrame` in
   `renderer.js` with its own `performance.now()` pair and see how the two halves of the boundary compare.
5. **Does the plan size even reach the pen?** Count the primitives, not the milliseconds: the command buffer's
   float count is `CanvasRenderer._length` at flush. Plot it against `TOTAL_Draw` across the four locations.
   If cost is linear in primitives, culling is arithmetic; if it is not, something else is going on.

## See also

- `labs/45-what-a-frame-costs/README.md` — the sim half of #841, measured, with Section D's refusal that this
  lab exists to answer, and Section E on how a bench proves it can see anything at all.
- `src/SpaceSails.Client/Rendering/FramePerf.cs` — the probe, and its own note on why it hangs off the
  `DeckView` rather than off the `Map` component (#905's frame ledger).
- `src/SpaceSails.Client/Rendering/DeckView.Frame.cs` — the conductor, its eighteen passes, and the banner
  that says the order is the picture.
- `src/SpaceSails.Client/Rendering/CanvasRenderer.cs` — the command buffer, and `EndFrame`, which is the
  flush being timed.
- `docs/testing-guide.md` — `?perf=1`'s row, the prose twin of the front door's dev-start button.
- Lab 10 (`labs/10-fast-enough-for-ten-thousand-x`) — the other stopwatch lab, and the same lesson about
  where arithmetic runs mattering more than the arithmetic.
