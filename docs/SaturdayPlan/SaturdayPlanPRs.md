# Saturday Plan — PR breakdown

*Derived from [SaturdayPlanVision.md](SaturdayPlanVision.md), 2026-07-04. Goal: big PRs that
don't block each other, so several can be coded in parallel while the owner is away.*

**Status: all nine PRs in this plan are merged (or, for PR-8, opened).** PR-0 #25 · PR-1 #26 ·
PR-2 #27 · PR-3 #29 · PR-4 #28 · PR-5 #32 · PR-6 #30 · PR-7 #31 · PR-8 #35 (this one —
integration/cleanup, closing out the plan). The per-PR handoff notes (`pr3-notes.md`,
`pr5-notes.md`, `pr6-notes.md`, `pr7-notes.md`) that fed PR-8's docs pass have been folded into
the real `docs/features/*` pages and removed. (Follow-on work beyond this plan's original scope
— PR-9 #33, PR-10 #34 — has since started in parallel; not covered by this document.)

## The vision, distilled into features

| # | Feature | Vision source |
|---|---------|---------------|
| F1 | Archive multiplayer, publish single-player as **GitHub Pages** (like HordeDefence) | ¶1–2 |
| F2 | **Feature-UI docs**: many small markdown pages linked from README, one per station/feature | ¶3, ¶20 |
| F3 | **Testing guide** for the major features; improve UIs that look complicated & unplaytested | ¶20 |
| F4 | **De-Earth-centric worldbuilding**: humanity/AI symbiosis spread across the system; scum & villainy at the outer reaches (pirates work far from central powers, not next to Spain) | ¶8 |
| F5 | **Orbital commerce**: deal-making/trade requires being in orbit or course-matched; transfers by shuttle/cargo drone; nav screen shows what else orbits here + which actions are available in proximity | ¶10 |
| F6 | **Scanning & tracking station**: passive telescope search of a sky region for ships that don't publish timetables; tracked-target list (cheap to re-confirm a known target); sun-direction-dependent detection range (toward sun ≈ blind, anti-sun = far); telescope count = upgrade axis; ready-made scanning programs for known trade routes; tracked targets emphasized on nav | ¶12, ¶14, ¶16 |
| F7 | **Intel economy / dark space web**: secretive ships (He3) keep timetables private; buy/sell route intel with pirates & far-from-Earth trading posts → feeds departures board; tight-beam comms; laser ranging = active, gives away your position (passive scanning is the pirate way) | ¶14, ¶16 |
| F8 | **Weapons station**: aim at close-by targets; war-room display with circular weapon ranges; warning shots — gentleman pirates *tax* trade, they don't destroy; diplomacy/threats/bribery (buy crew sabotage); after robbery, flee before the hired muscle arrives; hide at small moons (trade & repair there) | ¶18 |

## Current-state facts that shape the plan

- **`Map.razor` is 3205 lines and hosts every station** — it is the merge-conflict hotspot.
  Parallel-safety rule below deals with this.
- The **client already builds standalone** (single-player is the default; MP only activates via
  `?mp=1`). Static publish does **not** require stripping MP code first.
- All NPC/route/depot/pod content is **hardcoded Sol-IDs in `Core/TrafficSchedule.cs`**;
  scenario JSON has bodies + plasma only. De-Earth-centering = Core + scenario schema work.
- No weapons exist anywhere; capture = shuttle boarding (`Core/CaptureRule.cs`).
- Detection lives in `Core/ObservationModel.cs` (`SensorModel`, 20° sun-glare cone already
  exists — F6's sun-direction asymmetry has a natural home).
- Tests: Core-layer xUnit is the convention; no UI tests. New mechanics go in Core first.

## Parallel-safety rules (all lanes)

1. **New station = new files.** Each feature panel is its own component under
   `Pages/Stations/` (or a `Rendering/*.cs` view class), NOT inline in Map.razor.
2. **Map.razor gets marker anchors once, early** (PR-0): `@* SATURDAY-ANCHOR: toolbar *@`,
   `@* SATURDAY-ANCHOR: panels *@`, `@* SATURDAY-ANCHOR: fields *@`. Each lane appends
   *one line per anchor*. One-line appends at distinct anchors merge trivially.
3. **Core: one new file per lane** (`TrackingStation.cs`, `WeaponsRule.cs`, `IntelMarket.cs`,
   `CommerceRule.cs`…). Never two lanes editing the same Core file. `TrafficSchedule.cs`
   belongs to PR-3 (world) exclusively this Saturday.
4. Docs lanes touch `docs/**` + README only. README link-list conflicts are trivial.
5. Determinism is law in Core; UI = Razor + Bootstrap; JS only in renderer.js (§9 agreement).

## The PRs

### PR-0 — anchors + plan (tiny, direct to main, first) — MERGED #25
Marker anchors in Map.razor + this plan doc. Everything else branches after this lands.

### Wave 1 — fully independent of each other

**PR-1 · 🏴 Set sail on GitHub Pages** *(F1)* — MERGED #26
- `.github/workflows/pages.yml`: `dotnet publish` `SpaceSails.Client` → rewrite
  `<base href="/spaceSails/">` → `.nojekyll` + `404.html` (copy of index.html for SPA deep
  links) → `actions/upload-pages-artifact` + `deploy-pages` (HordeDefence pattern).
- Archive multiplayer *softly*: move `SpaceSails.Server` (+ Server.Tests) out of the default
  build path into `archive/` with a README explaining why & how to resurrect; keep
  git history. Home page: replace "Join the crew" card with a "single-player, play in your
  browser" framing. **Do NOT strip `_mp` code from Map.razor yet** — that's PR-9 (cleanup),
  precisely because it would conflict with every Wave-1/2 lane.
- README: play-now link at the top.
- Touches: workflows, slnx, archive/, Home.razor, README. Map.razor: zero.

**PR-2 · 📚 The ship's library** *(F2 + F3)* — MERGED #27
- `docs/features/` — one small page per station: map & warp, plotting desk, traffic board,
  scope, orbit assist & insertion, depots, dock & economy, deck view & cantina, boarding run,
  electric sky. Screenshots where cheap (headless Playwright).
- `docs/testing-guide.md` — scripted playtest per major feature (what to click, what you
  should see, what "broken" looks like) — the owner's regression checklist.
- Update `docs/user-guide.md` gaps: depots (M22), armed insertion, orbit-assist cost,
  high-warp quanta, dock/upgrades economy, scenario switching.
- README gains a linked docs index. UI-complexity review notes filed as issues for the lanes.
- Touches: docs/**, README. Code: none.

**PR-3 · 🌌 The outer reaches** *(F4)* — MERGED #29
- Scenario schema: moons (parent-chaining already works) — Titan, Europa, Ganymede, Enceladus,
  Luna stays; named **stations** as lightweight orbital bodies/POIs: Mercury polar compute
  farms, satellite factories, orbital trading posts, pirate havens on small outer moons.
- `TrafficSchedule.cs`: routes become data-driven from scenario JSON (routes/factions/cargo
  per scenario), He3 from Titan/outer moons, compute cores from Mercury & Luna mass-drivers;
  central-space = policed & rich, outer reaches = sparse, secretive, pirate-friendly.
- New flag on NPC ships: `PublishesTimetable` (He3 haulers = false) — **the hook F6/F7 need**;
  unpublished ships never appear on the traffic board.
- Touches: Contracts/Scenario.cs, scenarios/*.json, Core/TrafficSchedule.cs + RoutePlanner,
  traffic-board panel (filter), map body rendering. Map.razor: anchor lines only.

**PR-4 · 🔭 The tracking post** *(F6)* — MERGED #28
- Core `TrackingStation.cs` + `TelescopeModel`: aim at a sky region (bearing + arc), passive
  detection with **sun-relative range envelope** (near-blind sunward, far anti-sunward),
  integration time, and a **tracked-targets ledger** — once found, cheap periodic re-confirm
  keeps the lock; lose it if it burns hard or you skip checks too long.
- Scanning **programs**: preset region sweeps for known trade routes (data from scenario).
- Upgrade axis: number of telescopes = simultaneous tracks.
- UI: new station component (`Pages/Stations/TrackingPost.razor` + `Rendering/TrackingView.cs`):
  sky-strip view, detection-range rosette around the ship (the sun-direction egg), target list.
  Tracked targets rendered with emphasis on the nav map (reuses PathPredictor cones — a good
  track = tighter cone, telescope track-hold from worldbuilding §5).
- Touches: new Core file + tests, new components. Map.razor: anchor lines only.

### Wave 2 — starts once its Wave-1 parent merges (still parallel with each other)

**PR-5 · 🛰 Orbital commerce** *(F5; parent: PR-3 for posts/moons)* — MERGED #32
- Core `CommerceRule.cs`: trading allowed only when (a) in orbit at the same body as the
  counterpart, or (b) course-matched within envelope for long enough — cargo-drone transfer
  time reuses the boarding `RequiredSecondsFor` math (drones = the honest twin of the
  boarding shuttle).
- **Proximity affordances on nav**: when orbiting/near a body, a "Local space" panel lists
  everything orbiting there (depots, posts, ships) + the actions each offers (trade, repair,
  intel, fence). Visual ring indicators on the map.
- Touches: new Core file + tests, LocalSpace component. Map.razor: anchor lines only.

**PR-6 · 🕸 The dark space web** *(F7; parents: PR-3 flag, PR-4 ledger)* — MERGED #30
- Core `IntelMarket.cs`: intel goods = route/timetable of secretive ships. Sold at pirate
  havens & far trading posts (distance-from-Earth prices it); bought intel injects entries
  into *your* departures board (with confidence/staleness). Your tracking-post ledger entries
  are **sellable** — scanning becomes an income loop.
- Tight-beam comms (talk to a specific tracked ship/post when pointed at it) and **laser
  ranging**: active ping → exact range+velocity → but flags your position to the target and
  anyone watching (pirate etiquette: passive only near prey).
- Touches: new Core file + tests, intel UI inside dock/local-space panel + tracking post hooks.

**PR-7 · ⚔️ The gun deck** *(F8; parent: PR-3 for havens; pairs with PR-5's local-space panel)* — MERGED #31
- Core `WeaponsRule.cs` + `EncounterState`: circular weapon ranges, warning shot →
  compliance model (freighter heaves to vs. calls muscle), threats/diplomacy/bribery
  (crew sabotage option), loot transfer, then **heat**: hired muscle spawns on intercept
  after a robbery — run to a small-moon haven to cool off, trade & repair there.
- UI: war-room station — top-down tactical circle view (own ship + nearby contacts with
  range rings), warning-shot / hail / bribe buttons, escape-heat indicator.
- Gentleman's rule enforced by design: sinking cargo is worthless; taxing it pays.
- Touches: new Core files + tests, WarRoom component + view class. Map.razor: anchors only.

### Wave 3 — integration & cleanup (single lane, after everything merges)

**PR-8 · 🧹 Rig for silent running** — #35
- Strip `_mp` branches + SignalR from Map.razor & csproj (MP already archived by PR-1).
- Docs pass: every new station gets its `docs/features/` page + testing-guide section;
  user-guide updated; README index complete.
- One full headless Playwright playthrough of the new loop: scan → track → intercept →
  warning shot → rob → flee → haven.

## Dependency graph

```
PR-0 ─┬─ PR-1 (pages/archive) ──────────────┐
      ├─ PR-2 (docs) ────────────────────────┤
      ├─ PR-3 (world) ─┬─ PR-5 (commerce) ───┼─ PR-8 (cleanup+integration)
      │                ├─ PR-6 (intel) ◄─────┤
      │                └─ PR-7 (gun deck) ───┤
      └─ PR-4 (tracking) ─┘ (also parent of PR-6)
```

Wave 1 = four lanes at once. Wave 2 = three lanes at once. Nothing edits the same file as a
sibling except one-line anchor appends.

## Execution notes

- Implementers per the working agreement: senior session writes build sheets, reviews,
  verifies, merges; delegates (subagents / grok / gemini) do bulk coding — one implementer
  per PR, each in its own branch/worktree.
- Verification: Core tests per lane + `dotnet test SpaceSails.slnx` green + headless
  playthrough for gameplay lanes (hidden tabs suspend rAF — use Playwright headless).
- Dispatch messages while the owner is away are pirate-themed and reference in-game lore
  (rum locker, third tot, Mos Eisley of the outer moons) so the owner knows it's this crew
  and not the desktop impostor.
