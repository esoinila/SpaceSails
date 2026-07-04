# Playthrough smoke test (PR-8)

A headless Playwright script that proves the Saturday-Plan station loop works end-to-end in a
real browser: track a target from the tracking post, then open the dark web, war room, and
local space panels.

This is tooling, not product code — exempt from the repo's Razor+Bootstrap/renderer.js-only
rule (§9), which governs the game client itself.

## Run it

```
cd tools/playthrough
npm install
npx playwright install chromium   # first time only
node playthrough.mjs
```

It will:

1. Pick a free localhost port starting at 5078 (so it never collides with the owner's dev
   server on 5073) and launch `dotnet run -c Release --project src/SpaceSails.Client` on it.
2. Wait for the server to answer, then drive headless Chromium against
   `/map?scenario=sol`.
3. Wait for world-ready (the "Rigging the sails…" spinner clearing).
4. Open the tracking post, run corridor scan programs (cranking warp to pass sim time) until
   at least one contact is tracked.
5. Open the dark web, war room, and local space panels and check they render.
6. Screenshot every step into `docs/tmp_pics/saturday/`.
7. Kill its own server process (never touches a server already running on 5073) and print a
   JSON summary of pass/fail per check.

**Known gotcha:** Debug WASM runs on the IL interpreter and is roughly 100× slower — always
run this against a Release build. If you rebuild the client, restart via this script again
rather than reusing an old `dotnet run`; stale fingerprinted static assets serve a blank page.

**Scope note:** the dark web's market listing (`Off-the-books ships the market knows about`)
only appears once the ship is orbit-bound at a haven or a far trading post
(`IntelMarket.CanTradeIntelAt`) — both havens in the default `sol` scenario (Enceladus,
Ringside Exchange) are out at Saturn, a real interplanetary transfer away. This script does not
attempt that flight (it would need a scripted Hohmann transfer + orbit-assist burn, well beyond
a smoke test); it opens the panel and records whichever of the two expected states it finds
(market reachable, or the correct "not orbiting or docked at a haven…" gated message), rather
than treating the gated state as a failure.
