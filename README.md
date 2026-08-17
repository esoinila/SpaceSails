# SpaceSails 🏴‍☠️

A solar-system-scale sailing and piracy game. Ships move at planet-like speeds, controlled only by ±10% pulses on the velocity vector; routes are plotted in advance against the motion of the celestial bodies. You are a pirate intercepting Helium-3 cargo runs from Saturn.

**Play now: https://esoinila.github.io/SpaceSails-play/**

## What you can do in it

Fly and plot, first of all — the whole game hangs off the plotting table. Then:

- **Crew a ship from eight duty desks** — nav, sensors, war room, trade, comms, galley, the
  captain's mission board, and a walkable deck with real windows.
- **Go ashore.** Landable bodies offer a seeded set of 2–4 named landing sites, and down there
  you are on foot in a suit: bury a hoard the law can never confiscate, dig up somebody
  else's, deploy a sentry, and get out before the air does.
- **Go *under*.** Some sites have a hut with a lift in it. Below is a real facility — floors,
  departments, sealed sectors, authority cards that open exactly one shaft band, and a band
  nobody listed. Everything down there pays in information before it pays in hardware.
- **Take work that is detective work.** Fetch runs with legs, hatch-crack jobs with a real
  code, and route tips carried with their provenance — who told you, where, and when. A tip
  that leads nowhere yet is still filed, because it may matter later.
- **Get caught, and get up again.** The collector's BUSTED encounter runs on open dice; dying
  is not game over but a brain-backup, a clinic bill and a rustbucket — and anything you
  buried or banked was never aboard.
- **Pull on two long threads.** There are two slow mysteries in the world. Neither is ever
  explained to you; both are assembled from fragments you earn by playing, and they meet.

## 🧮 The Gravity Lab — learn orbital mechanics by running it

This repo is secretly edutainment. **Forty-one** type-it-in lessons under
[`labs/`](labs/README.md) teach numerical orbital mechanics on the game's own deterministic
engine — fork a probe, run it, break it on purpose, learn the physics and the programming at
once, the way magazine listings taught a generation to code:

```bash
dotnet run --project labs/01-falling-is-orbiting -c Release
```

Highlights: the integrator zoo measured (explicit Euler leaks 19.75% of Mercury's energy in
50 years; the game's semi-implicit doesn't), the Oberth effect at exactly 9× from the same
burn, a from-scratch n-body integrator quantifying what the rails ephemeris hides, and
[*Oops at the Moon*](labs/12-oops-at-the-moon/README.md) 🌙, where careless miners un-rail
Luna and you compute the catastrophe (playable aftermath:
[`?scenario=oops`](https://esoinila.github.io/SpaceSails-play/map?scenario=oops)). Every
number in every lesson comes from actually running that lesson's probe.

The later labs left physics behind and kept the method: Lab 34 measures whether a rescue
button is actually *clickable* when you need it, Lab 41 runs A\* over generated interiors to
prove you can get to the back of the ship, and Lab 44 is a lab about the lab — asking not
whether a room is sealed, but *why*.

## Docs

- [Big picture / vision](docs/SpaceSails_plan_big_picture.md)
- [Detailed implementation plan](docs/SpaceSails_plan_detailed.md) — milestones, architecture, working agreement
- [Coding helpers](docs/coding-helpers.md) — driving the `grok` & `gemini` CLIs headlessly to offload implementation, and the house laws structural work obeys (purity proofs, snapshot-first splits, ratchets)
- [Architecture](docs/architecture.md) — the box view, where the code lives (the families, the two collaborator objects, the 1,500-line size gate), the duty-station UI shape, multiplayer-with-desks design notes, and why WebAssembly
- [Captain's Guide](docs/user-guide.md) — every feature, how to fly, how to steal (mirrored in-game at `/guide`)
- [Testing guide](docs/testing-guide.md) — the owner's scripted regression checklist, one playtest per major feature, and **Appendix A: the boot cheats** (31 `?param=` quick starts — a scene nobody can reach on demand is a scene that ships broken)
- [Story-arc QA handoff](docs/QAHandoff-StoryArcs.md) — how the narrative content is tested, and the five named bug classes that keep coming back
- [Worldbuilding notes](docs/worldbuilding-notes.md) — the owner's canon and standing design rulings
- [The Gravity Lab](labs/README.md) — a type-it-in numerical orbital mechanics tutorial series built on `SpaceSails.Core` itself, fork-run-break style
- [The paper](docs/paper/spacesails-paper.tex) ([PDF](docs/paper/spacesails-paper.pdf)) — *SpaceSails: Secretly a Classroom* — the SIGGRAPH-style system story (deterministic real-time orbital sim in the browser), the long arcs and the ground game, the story-QA method, and the human-PO / AI-head-coder experience report as a first-class section

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
- [Tracking post](docs/features/tracking-post.md) — the ship's telescope, sun-blind detection rosette, tracked-targets ledger
- [Local space](docs/features/local-space.md) — the "what else orbits here" panel, same-orbit/course-matched trading, drone transfers
- [Dark space web](docs/features/dark-web.md) — buying/selling route intel, tight-beam hails, laser ranging
- [War room](docs/features/war-room.md) — weapon range, warning shots, compliance, bribery, heat, and hunters
- [Station desks](docs/features/station-desks.md) — the duty-station refit: full-screen desks switched by number key, the 70% rule, summary chips, the Galley
- [The captain's position](docs/features/captains-position.md) — the mission desk (key `0`): Hunt/Trade run/Lay low/Survey/Free sailing, and the mission chip on every desk
- [The sensors map](docs/features/sensors-map.md) — point at the sky and ask; the Sensors desk's whole philosophy
- [Wolf-aim 🐺](docs/features/wolf-aim.md) — fire control against hunters, why it needed its own physics, and where its limits honestly are
- [The news wire](docs/features/news-wire.md) — one deterministic feed of world events behind Comms and the Galley
- [Lab viz](docs/features/lab-viz.md) — the optional browser pop-up that draws what a Gravity Lab lesson just computed

**On the ground and under it**

- [The landing site](docs/features/the-landing-site.md) — what a moon's ground has to be before it ships: sites, air, shelters, caches, the monolith, and the underground complex
- [Going ashore — the haven walk](docs/features/haven-interior-walk.md) — the first indoor walk into a haven
- [The captain's character](docs/features/the-captains-character.md) — a ledger, not a meter

**Wrecks, air, and the things aboard them**

- [The atmosphere board](docs/features/atmosphere.md) — pressure as a mechanic: the valve mimic, the vacuum soak, the pump, the pressure locks
- [The archive node](docs/features/the-archive-node.md) — the thing in the hold that remembers you
- Designed, not built (owner's ideas, kept honest as specs): [the void](docs/features/the-void.md), [the paperwork](docs/features/the-paperwork.md), [making sure](docs/features/making-sure.md), [the safety card](docs/features/safety-card.md)

**The long arcs** — spoilers; these are the writers' bibles, not player docs

- [PROJEKTI KAAMOS](docs/features/KaamosPlotline.md) — the sealed ice-moon project and the berth nobody files for
- [Nebula Mutual](docs/features/NebulaArc.md) — the pirate-insurance arc, and what the premium actually buys

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
| `scenarios/` | Scenario data files (`sol.json`, `sol-eu.json`, `wheel.json`, `oops.json`) |
| `labs/` | The Gravity Lab — 41 runnable lesson probes + the lab-viz host |
| `tests/` | xUnit test projects (Core, Client, and the UI gate), plus `tests/scripts/` — the Python purity/assert accounting a pure-move PR has to show |

## Provenance

The basic idea is based on a fast-paced party game the owner made at the change of the
millennium, reincarnated here as a solo navigation-and-piracy sim. In this incarnation, Erno
Soinila is the product owner and Claude Fable (Anthropic) is the head coder.

## License

[MIT](LICENSE) — fork it, learn from it, ship your own version; attribution is appreciated
but the point of this license is that you don't need to ask.
