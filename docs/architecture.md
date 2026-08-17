# Architecture

This is the box view of SpaceSails: what runs where, why it's shaped this way, and where it
might grow. Written for people and AI landing in the repo cold — cite the files, not just the
diagram.

## Box view

```mermaid
flowchart TB
    subgraph Browser["Browser (Blazor WebAssembly)"]
        direction TB
        UI["Razor duty-station desks\n(Map.razor host + Pages/Stations/*.razor)"]
        Core["SpaceSails.Core\ndeterministic simulation\n(Simulator, ICelestialEphemeris, ManeuverPlan...)"]
        Renderer["CanvasRenderer\n(Rendering/CanvasRenderer.cs)"]
        JS["renderer.js\n(wwwroot/renderer.js, HTML canvas)"]
        UI --> Core
        UI --> Renderer
        Renderer -->|JSImport/JSExport,\nzero-copy Span float buffer| JS
    end

    Scenarios["scenarios/*.json\n(static fetch via HttpClient)"] -->|Http.GetStringAsync +\nScenarioLoader.Parse| Core

    subgraph Pipeline["Build & publish"]
        direction LR
        GHA["GitHub Actions\n(.github/workflows/pages.yml)"] --> Publish["dotnet publish\nsrc/SpaceSails.Client -c Release"]
        Publish --> Push["force-push wwwroot\nto esoinila/SpaceSails-play"]
        Push --> Pages["GitHub Pages"]
    end

    Browser -.ships as static assets via.-> Pipeline

    subgraph Archived["Archived (archive/SpaceSails.Server)"]
        direction TB
        ASPNET["ASP.NET Core host"] --> Hub["GameHub (SignalR)"]
        Hub --> Session["SessionHost\nauthoritative session, one sim tick,\nper-player sensor-filtered broadcasts"]
        Session --> Core
    end
```

Three projects carry the weight (`SpaceSails.slnx`):

- **`src/SpaceSails.Core`** — the deterministic simulator (`Simulator.cs`, `ShipState.cs`,
  `ManeuverPlan.cs`, `CircularOrbitEphemeris.cs`, `PathPredictor.cs`, `TrafficSchedule.cs`,
  `NewsWire.cs`, `DeterministicRandom.cs`, and friends). No UI, no I/O beyond
  `ScenarioLoader.LoadFile`, no wall clock. This project is the whole point of the shared-Core
  design — see [Why WebAssembly](#why-webassembly) below. The three biggest generators here are
  partial classes split by department, not single files —
  see [Where the code lives](#where-the-code-lives--the-families).
- **`src/SpaceSails.Contracts`** — DTOs and scenario models (`Scenario.cs`,
  `Multiplayer.cs`) shared by anything that talks to Core or the (archived) hub.
- **`src/SpaceSails.Client`** — the Blazor WASM app. `Pages/Map.razor` is the single host page
  (`@page "/map"`, 6,499 lines of markup): it owns the `<canvas>`, the desk tab bar, keyboard
  shortcuts, and drives `Core.Simulator` directly in-process. Its code-behind is
  `public sealed partial class Map` — **117 `Pages/Map.*.cs` partials, 45,940 lines** — plus two
  families that are no longer partials at all but collaborator objects behind a written interface
  (`Pages/Seating/`, `Pages/Patrol/`). `Rendering/CanvasRenderer.cs` implements `IRenderer`
  over that canvas; `Rendering/RendererInterop.cs` is the `[JSImport]`/`[JSExport]` boundary to
  `wwwroot/renderer.js`, chosen specifically so the per-frame vertex buffer crosses as a
  zero-copy `JSType.MemoryView` over a `Span<float>` instead of a JSON-serialized array — the
  interop budget is two calls per frame (`drawFrame` + `drawTexts`), not one per primitive.

Scenarios (`scenarios/sol.json`, `scenarios/wheel.json`, `scenarios/sol-eu.json`) are the
canonical copy at the repo root. `SpaceSails.Client.csproj`'s `CopyScenariosIntoWwwroot` MSBuild
target mirrors them into `wwwroot/scenarios` before static-web-asset discovery runs (a plain
linked `<Content>` item resolves fine at build time but 200s with a zero-length body at request
time under the dev server — the copy sidesteps that). `Map.razor` fetches them at runtime with
`Http.GetStringAsync($"scenarios/{scenarioName}.json")` and hands the JSON to
`ScenarioLoader.Parse`; the `?scenario=` query string picks which file.

The publish pipeline (`.github/workflows/pages.yml`) runs `dotnet publish
src/SpaceSails.Client -c Release`, patches `index.html` for the `SpaceSails-play` subpath and
cache-busts the stylesheets, then force-pushes `publish/wwwroot` as the entire `main` branch of
the public `esoinila/SpaceSails-play` repo, which serves GitHub Pages from it. The source repo
stays private; the built repo is the only public artifact. See
[archive/README.md](../archive/README.md) for why it's a force-push to a separate repo rather
than Pages-from-this-repo (Pages doesn't serve private repos on the plan this project is on).

The archived path — `archive/SpaceSails.Server` (`GameHub.cs`, `SessionHost.cs`) — is an
ASP.NET Core host that serves the same client plus a SignalR hub over an authoritative
`SessionHost`. It isn't part of the default build or CI; see
[archive/README.md](../archive/README.md) for what's there and how to bring it back.

## Where the code lives — the families

Features arrived faster than containers did, and by 2026-08-15 the two biggest source files in the
repo were `Pages/Map.Surface.cs` at 9,410 lines and `Core/UndergroundComplex.cs` at 8,747. Neither
was *about* anything by then — each was simply the obvious place to put the next thing. #870 cut
them, and everything else over the line, into **families**: one subject per file, the family named by
the file-name stem, and the family's header note kept in the family's own core file. **71 files under
`src/` carry a one-line `// Subject:` banner** saying what they are for and which family they belong
to (55 in `Pages/`, 11 in `Core/`); that line is the first thing to read and the cheapest thing to
grep.

Nothing was rewritten. Every one of those splits was a **pure move** — the code and its docblocks
travelled byte-identical, and the PR carried a mechanical proof that they had. See
[coding-helpers.md § House laws for structural work](coding-helpers.md#house-laws-for-structural-work-870)
for what a lane has to prove before it lands.

### The client page

`Pages/Map.razor` (6,499 lines of markup) is the host; its code-behind is
`public sealed partial class Map`, **117 `Pages/Map.*.cs` files, 45,940 lines**. Every partial still
sees every field of every other — that is what a partial class is — which is exactly why the two
families that kept colliding across crews are no longer partials at all (next section).

| family | files | lines | what lives there |
|---|---:|---:|---|
| `Map.Surface.*` | 15 | 9,548 | the excursion, one subject each: `.Tank` `.Shelter` `.Dig` `.Darkroom` `.Satchel` `.Reevers` `.Hive` `.Canteen` `.Comms` `.Nerve` `.Hud` `.Frame` `.RepoBoat` `.Cheats` |
| `Map.Sim.*` | 15 | 4,576 | the loop: `.Boot` `.Tick` `.Keys` `.Controls` `.Cancel` `.Starts` `.Cheats`, and `Map.Sim.World.*` (7 files, 2,119) — the boot's own named stages plus every `?query=` reader (`.Build` `.Start` `.Query` `.QueryArcs` `.QueryGround` `.QueryHive`) |
| `Map.Combat.*` | 6 | 3,121 | `.FireControl` (the gun deck) `.Ordnance` (what has left the tube) `.Boarding` `.Busted` `.Remote` |
| `Map.Plot.*` | 9 | 2,967 | the plotting table: `.Bodies` `.Nodes` `.Ribbon` `.Frame` `.FlightPlan` `.Destination` `.Skim` `.Sling` |
| `Map.Quests.*` | 7 | 2,737 | `.Offers` `.Contracts` `.Ledger` `.Bank` `.Bar` `.Caches` |
| `Map.Venting.*` | 6 | 1,882 | pressure: `.Pumps` `.Vacuum` `.Doors` `.Fire` `.Mimic` |
| `Map.Deck.*` | 7 | 1,820 | the walked ship: `.Walk` `.Interact` `.Fixtures` `.Comforts` `.Scope` `.Stall` |
| the seat family, page side | 7 | 1,795 | `Map.Table.cs` `Map.Seated.cs` `Map.Cubicle.cs` `Map.SitStandDesk.cs` `Map.Stool.cs` `Map.Bench.cs` `Map.OfficeChair.cs` — what a seat is a *gate* on, kept on the page |
| `Map.Patrol.*`, page side | 4 | 247 | forwarders only: the round itself moved out |

`Rendering/DeckView.*` (6 files, 2,665) is the same shape one layer down: `DeckView.Frame.cs` holds
`Draw`, which is a conductor over seventeen named passes rather than one 1,058-line method;
`.Hud` `.Seats` `.Inks` `.Dark` are the rest.

### Core

| family | files | lines | what lives there |
|---|---:|---:|---|
| `UndergroundComplex.*` | 15 | 8,845 | the Hive, by department: `.Block` `.Hall` `.FloorPlan` `.Park` `.Rooms` `.Fixtures` `.Amenities` `.Shafts` `.Signs` `.AuthorityCard` `.Arrivals` `.Air` `.Haul` `.Cards` |
| `PatrolBeat.*` | 8 | 2,102 | the round's pure half: `.Lane` `.Chase` `.Challenge` `.Checkpoints` `.CoverAct` `.Escort` `.Eye` |
| `RingOffice.*` | 5 | 1,794 | `.Layout` `.Fittings` `.Frame` `.Prose` |

### Two families are objects now, not partials

A partial class shares every field with every other partial of itself, so "who can change the seat
flag" had no answer smaller than *the whole page*. Four issues in four days landed in the seat family
and each crew had to read seven files to know what one keypress did. Two families were therefore
turned into **collaborators with a written surface** — the object holds its own state and verbs, and
what it still needs from the page is an interface you can read in one sitting:

| | the object | the door | members | the page's side |
|---|---|---|---:|---|
| the seat | `Pages/Seating/*` — 8 files, 2,485 lines (`Seating.cs`, `.Seated` `.Table` `.Stool` `.Bench` `.OfficeChair` `.Sit`) | `Pages/Seating/ISeatHost.cs` | **28** | `Map.SeatHost.cs` (145) |
| the round | `Pages/Patrol/*` — 9 files, 2,573 lines (`Patrol.cs`, `.Round` `.Floor` `.Run` `.Challenge` `.Escort` `.Hide`, `Guard.cs`) | `Pages/Patrol/IPatrolHost.cs` | **21** | `Map.PatrolHost.cs` (126) |

Both are `private sealed partial class` nested inside `Map` — the records they are made of
(`TableTalk`, `StoolSeat`, `Guard`) are the page's private types, and nesting keeps them private
instead of publishing the seat's furniture to the whole assembly. `Map.Collaborators.cs` (30 lines)
is the one neutral file that builds them, because a class gets exactly one parameterless constructor
and it may not live in either family's files.

**The member counts are the finding, and they are ratchets.** `TheSeatKeepsItsOwnStateTests` and
`ThePatrolKeepsItsOwnStateTests` assert 28 and 21 exactly: taking a member off is a good day and the
number comes down with it; adding one is a lane of its own, argued for in a PR body, because it is
the chair (or the guard) asking the page for something new. The technique that keeps the numbers
small is written into both interfaces' docblocks — **ask for the ANSWER, never for the machinery.**
Four of `ISeatHost`'s rows are one member each *instead of* the eight fields and three sweeps they
are made of.

The same guards enforce the other half: **no file outside a family names that family's fields.** The
sweep reads every `.cs` *and* `.razor` under `src/SpaceSails.Client`, word-anchored, and names file,
field, line and the line's text when it reddens. Each has an anti-vacuous half — the family files
must exist at the paths the sweep exempts, and the field names must still be found *inside* them, so
a rename cannot make the sweep pass by leaving nothing to find.

### The size gate

`tests/SpaceSails.Core.Tests/NoSourceFileIsTooLongTests.cs` — four facts, and the reason the cut
stays cut.

**No source file under `src/` may exceed 1,500 lines**, except the ones written down by name in a
`WrittenExceptions` dictionary, and those may only shrink. Three laws hold the list: no new
offenders; a listed file must be at or below its written allowance; and a listed file that has fallen
back under the line must have its row *deleted* (a list of permissions nobody revokes is how a gate
stops being a gate). A fourth fact is the anti-vacuous half — the sweep must actually find the tree
(200+ files; it finds 442), `obj/` and `bin/` must stay out, and the line must sit clear of the
largest file beneath it by at least 25 lines.

**The exception list is empty.** It was written with ten rows; #870's lanes took nine of them under
the line, and the last — a single 1,656-line method that a pure move was not allowed to split — went
behind a fingerprint of the world every boot URL builds. The longest source file in the repo today is
`Core/UndergroundComplex.Block.cs` at **1,447 lines**, which is 53 lines of daylight under the line.
From here, law 1 is the whole gate, laws 2 and 3 are vacuously green, and the first row anybody writes
will be a new debt rather than an inherited one.

The number is not about a compiler. It is about a reader: fifteen hundred lines is roughly where
"what is this file about?" stops having an answer.

## The duty-station UI architecture

The client is one live simulation viewed through full-screen "desks," not a dashboard of
panels. `Pages/ShipDesk.cs` is the enum (`Nav`, `Sensors`, `WarRoom`, `Trade`, `Comms`,
`Galley`, `Deck`, `Captain`); `Map.razor` is the single host that switches which desk's content
fills the screen, keeps the canvas and `Core.Simulator` running underneath regardless of which
desk is active, and renders a thin edge strip of `DeskChips.razor` — one small,
standardized objective-summary chip per *other* desk (`Pages/Stations/DeskChips.razor`).

The shape follows one rule from the owner (`docs/SaturdayPlan/StationDesks.md`): **the 70%
rule** — at each station, that station's own topic should own roughly 70% of the screen.
Sensors shows a full scope wall (one live scope per tracked target simultaneously, not a small
box); War room gets the full tactical circle; Trade gets local space and the dock market side
by side. Everything that isn't the current desk's topic is reduced to a one-line chip
("`→ Mars orbit`", "`heat 🔥N · hunter 2.1 Mkm`", "no whispers") — info-rich where it's owned,
summary everywhere else. This is why the older design of small pop-up cards stacked over the
map (traffic board, dock panel, first-hunt banner) was retired in favor of desks switched by
number key (`1`-`7`, `0` for Captain) or a tab bar: cramming a scope wall or a tactical circle
into a floating card left no room to actually read it. See
[docs/features/station-desks.md](features/station-desks.md) for the desk-by-desk detail.

## Multiplayer with the desk system

This is a design discussion, not a build sheet — nothing here is implemented.

The archived server (`archive/SpaceSails.Server/SessionHost.cs`) already solved the hard parts
of one flavor of multiplayer: one authoritative session, one sim tick, 2-8 pirates. Two
properties carry over directly:

- **Min-warp voting** — `SessionHost` advances time at the minimum of all connected players'
  requested warp; nobody can skip time a crewmate hasn't agreed to.
- **Per-player sensor-filtered broadcasts** — each player's state packet is filtered through the
  same `SensorModel` the single-player client already uses, from that player's own ship
  position. An unobserved ship is simply absent from the packet, not present-but-masked.

The desk refit changes what "multiplayer" most naturally means, though. The old model was
**ship-per-player**: each pirate flies their own hull in a shared system. The desk system
suggests a second, arguably more natural mode on top of it: **crew multiplayer** — several
players on *one* ship, each permanently manning a different desk. A captain holds the mission
(key `0`, `Pages/Stations/Captain.razor`) and sets the goal; someone else runs Sensors
(`TrackingPost.razor`) and calls out tracks; someone runs War room
(`WarRoom.razor`) and handles hails/bribes/warning shots; someone flies Nav. This maps the
existing desk boundaries onto player boundaries almost for free — the summary-chip strip
already exists to tell everyone what the other stations are doing, which is exactly the
information a real crewmate would want from a crewmate's station.

Ship-per-player (today's archived mode) would remain the second tier: useful for a fleet-vs-fleet
or race scenario where players don't share a hull, but not the first mode to build, since crew
multiplayer reuses the desk boundaries that already exist and ship-per-player mostly reuses the
old `SessionHost`/`GameHub` machinery as-is.

What would need to be server-authoritative vs. what can stay client-side, in either mode:

- **Server-authoritative:** the shared sim tick and time (nobody's local clock decides what
  happened), maneuver plans and pulses (so two crewmates can't issue conflicting burns), cargo
  and credits (the stakes), anything hidden-information (sensor visibility, dark-web intel) —
  the existing `SessionHost` broadcast filtering is designed exactly for this.
- **Client-side:** desk UI state (which desk *you* personally are looking at is purely local —
  a captain and a sensors officer are on different desks of the same ship at the same moment),
  rendering, camera/follow-ship, and anything cosmetic (news wire flavor, the rum locker/wobble
  — though shared-ship rum state is a fun edge case for crew mode specifically, since one
  crewmate's wobble arguably shouldn't be everyone's wobble).

The reason this is cheap either way: `SpaceSails.Core` is deterministic (`Simulator.cs`'s own
doc comment: "the same initial state, plan, and step count must produce bit-identical results
on client (WASM) and server"). That means state sync is **inputs and seeds, not snapshots** —
a server (or a peer) only needs to forward the maneuver plan changes, pulse commands, and the
`DeterministicRandom` seed for anything randomized (traffic generation), and every client
re-derives the same world by replaying the same deterministic Core. This is dramatically
cheaper than shipping full `ShipState` snapshots per tick, and it's the same property the
archived server already leaned on.

## Why WebAssembly

`SpaceSails.Core` is a plain .NET class library referenced by both the WASM client
(`SpaceSails.Client.csproj`) and, when resurrected, the ASP.NET server
(`archive/SpaceSails.Server`) — one integrator, one source of truth for orbital mechanics,
traffic, and encounter rules, running unmodified on both sides. That's the actual reason for
choosing Blazor WebAssembly over, say, a JS/canvas frontend talking to a thin API: the
simulation itself ships as browser code, not just a client for someone else's simulation.

Two more properties fall out of that choice:

- **Near-native speed for the integrator.** WASM's AOT/JIT path runs the fixed-timestep
  semi-implicit Euler integrator (`Simulator.Step`) fast enough for real-time play at high warp.
  The honest caveat, learned the hard way and now called out in the README: **Debug builds run
  on the WASM IL interpreter and are roughly 100x slower** — choppy frames and sluggish
  plotting. `-c Release` is mandatory for anything resembling real play; `run.ps1` defaults to
  it and `run-debug.ps1` exists specifically to make the slower, debuggable path opt-in rather
  than the default.
- **Zero-server static hosting.** Because the whole client is static WASM/JS/CSS output, it
  ships as files, not a running process — `.github/workflows/pages.yml` publishes and
  force-pushes the build to `esoinila/SpaceSails-play`, which GitHub Pages serves directly.
  Free, scales without attention, no ops to run or pay for. This is also *why* the source repo
  stays private while the build artifact is a separate public repo: GitHub Pages needs a public
  repo to serve from, and the built output (not the source) is what's meant to be public.

Two hosting pitfalls this project actually hit, both from static WASM publish's asset-fingerprinting
model:

- **Fingerprinted framework assets.** .NET 10 renames `_framework/*.js` on every build,
  so a browser can end up requesting an index.html that no longer matches a stale
  service-worker/cache entry, or vice versa — the fix in practice is just restarting the local
  dev server and reloading (documented in the README's tips).
- **Scoped CSS's hash mismatch (the "postage-stamp-map incident").** Standalone WASM publish
  only fingerprints `_framework` assets, not the scoped-CSS bundle
  (`SpaceSails.Client.styles.css`) or `bootstrap.min.css`. The scoped-CSS bundle's `b-xxx`
  scope attributes are regenerated per build, so a browser-cached stale stylesheet served
  against fresh DLLs silently drops all scoped styles — the map shrank to a postage stamp with
  no visible error. `pages.yml` works around this by appending a per-build query string
  (`?v=<short-sha>`) to each stylesheet link in the published `index.html`, forcing a fresh
  fetch every deploy instead of relying on fingerprinting that standalone WASM publish doesn't
  do for these files.
