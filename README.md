# SpaceSails 🏴‍☠️

A solar-system-scale sailing and piracy game. Ships move at planet-like speeds, controlled only by ±10% pulses on the velocity vector; routes are plotted in advance against the motion of the celestial bodies. You are a pirate intercepting Helium-3 cargo runs from Saturn.

## Docs

- [Big picture / vision](docs/SpaceSails_plan_big_picture.md)
- [Detailed implementation plan](docs/SpaceSails_plan_detailed.md) — milestones, architecture, working agreement
- [Coding helpers](docs/coding-helpers.md) — driving the `grok` & `gemini` CLIs headlessly to offload implementation
- [Captain's Guide](docs/user-guide.md) — every feature, how to fly, how to steal

## Stack

.NET 10 · Blazor WebAssembly (Razor + Bootstrap) · Canvas 2D rendering · SignalR multiplayer · Azure Container Apps

## Build & run

Two ways to run, depending on whether you want multiplayer:

**Single-player (client only)** — pure Blazor WASM, everything runs in the browser:

```bash
dotnet run -c Release --project src/SpaceSails.Client
# open http://localhost:5073
```

**With multiplayer** — the server hosts the same client plus the SignalR hub, so the
Home page's "Join the crew" card works (open two tabs or two machines with different
callsigns; warp follows the slowest crew member):

```bash
dotnet run -c Release --project src/SpaceSails.Server
# open http://localhost:5295
```

Tips:

- Use `-c Release` for play — Debug WASM runs on the IL interpreter and is dramatically
  slower (choppy frames, slow plotting).
- Add `--no-build` to start faster when nothing changed.
- Port already in use? A previous instance is still running — stop it
  (PowerShell: `Get-NetTCPConnection -LocalPort 5073 -State Listen | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }`).
- `Ctrl+C` stops the app. Run the tests with `dotnet test SpaceSails.slnx`.

How to play: in-game **Captain's Guide** at `/guide` (also [docs/user-guide.md](docs/user-guide.md)).

## Layout

| Path | What |
|------|------|
| `src/SpaceSails.Core` | Deterministic simulation (shared client/server) |
| `src/SpaceSails.Contracts` | DTOs and scenario models |
| `src/SpaceSails.Client` | Blazor WASM client |
| `src/SpaceSails.Server` | ASP.NET Core host + SignalR hub |
| `scenarios/` | Scenario data files (`sol.json`, `wheel.json`) |
| `tests/` | xUnit test projects |
