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

## In CI

The `ui-gate` job in `.github/workflows/ci.yml` runs on every PR and main push (same workflow as the
Core suite, so a red gate is a red check). It publishes the client once, installs Chromium
`--with-deps`, runs the gate, and always uploads the artifacts.

**Deliberately kept out of `SpaceSails.slnx`** so `dotnet test SpaceSails.slnx` stays the fast,
browser-free Core suite. This gate runs as its own job by project path.
