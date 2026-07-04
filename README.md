# SpaceSails 🏴‍☠️

A solar-system-scale sailing and piracy game. Ships move at planet-like speeds, controlled only by ±10% pulses on the velocity vector; routes are plotted in advance against the motion of the celestial bodies. You are a pirate intercepting Helium-3 cargo runs from Saturn.

**Play now: https://esoinila.github.io/SpaceSails/**

## Docs

- [Big picture / vision](docs/SpaceSails_plan_big_picture.md)
- [Detailed implementation plan](docs/SpaceSails_plan_detailed.md) — milestones, architecture, working agreement
- [Coding helpers](docs/coding-helpers.md) — driving the `grok` & `gemini` CLIs headlessly to offload implementation
- [Captain's Guide](docs/user-guide.md) — every feature, how to fly, how to steal (mirrored in-game at `/guide`)
- [Testing guide](docs/testing-guide.md) — the owner's scripted regression checklist, one playtest per major feature

### Feature guides

Small, linked pages — one station or mechanic per page — under `docs/features/`:

- [Map & warp](docs/features/map-and-warp.md) — the main view, time controls, hand-flying
- [Plotting desk](docs/features/plotting-desk.md) — scrub, burn nodes, closest-pass warning, planned insertion
- [Traffic board](docs/features/traffic-board.md) — departures, prediction cones, plotting an intercept
- [Scope](docs/features/scope.md) — the close-up instrument view, auto-lock vs. manual
- [Orbit assist](docs/features/orbit-assist.md) — the one-button "enter orbit" mechanic and its Δv cost
- [Orbital depots](docs/features/depots.md) — the one plunderable cargo depot per planet
- [Dock & economy](docs/features/dock-and-economy.md) — selling cargo, refueling, buying upgrades
- [Deck view & cantina](docs/features/deck-view.md) — walking the ship, consoles, the rum-wobble mechanic
- [Boarding run](docs/features/boarding-run.md) — the capture window, automatic timer, and shuttle minigame
- [Electric sky](docs/features/electric-sky.md) — hull charge, arcing, venting, plasma streams
- [Scenarios](docs/features/scenarios.md) — the three voyages and the `?scenario=` query string

## Stack

.NET 10 · Blazor WebAssembly (Razor + Bootstrap) · Canvas 2D rendering · GitHub Pages

## Build & run

**Single-player (client only)** — pure Blazor WASM, everything runs in the browser (this is
also exactly what ships to GitHub Pages):

```bash
dotnet run -c Release --project src/SpaceSails.Client
# open http://localhost:5073
```

Multiplayer (SignalR hub, "Join the crew") is archived — untested, and the fun core of the
game is single-player navigation and plotting. See [archive/README.md](archive/README.md) for
what's there and how to bring it back.

Tips:

- Use `-c Release` for play — Debug WASM runs on the IL interpreter and is dramatically
  slower (choppy frames, slow plotting).
- Add `--no-build` to start faster when nothing changed.
- Port already in use? `./run.ps1` handles it — finds the next free port and says so
  (`-TakePort` to stop the squatter instead).
- Run variants: `./run.ps1` (client, Release), `./run-debug.ps1` (Debug build, for
  development). Every variant handles taken ports; the banner names the build config.
- Blank page (or blank /guide) after the code changed? The running server is serving stale
  fingerprinted assets — .NET 10 renames `_framework/*.js` on every build. Restart the app
  (`Ctrl+C`, then `./run.ps1`) and reload.
- `Ctrl+C` stops the app. Run the tests with `dotnet test SpaceSails.slnx`.

How to play: in-game **Captain's Guide** at `/guide` (also [docs/user-guide.md](docs/user-guide.md)).

## Layout

| Path | What |
|------|------|
| `src/SpaceSails.Core` | Deterministic simulation (shared client/server) |
| `src/SpaceSails.Contracts` | DTOs and scenario models |
| `src/SpaceSails.Client` | Blazor WASM client |
| `archive/SpaceSails.Server` | ASP.NET Core host + SignalR hub (archived — see [archive/README.md](archive/README.md)) |
| `scenarios/` | Scenario data files (`sol.json`, `wheel.json`) |
| `tests/` | xUnit test projects |
