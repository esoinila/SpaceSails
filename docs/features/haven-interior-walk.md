# Going ashore — the first indoor walk into a haven

Kickoff plan for the next session. Short on purpose: enough to start building the *smallest
walkable thing* and grow it.

## The goal

You dock at **The Space Bar** (a mass-less ⚓ station haven off Mars). Right now docking just
freezes the ship alongside and bleeds off heat. The next step: **step off the ship and walk around
inside the haven in first person** — a pirate bar you can actually stand in. First milestone is one
room you can walk, not a whole station.

Why this haven first: it is literally a bar, the ⚓ dock flow now works end to end (PR #96), and
the interior art already ships — `DeckView.cs:49-51` loads `art/the-space-bar.jpg` today as the
ship cantina's backdrop. We're not starting from a blank canvas; we're pointing an existing engine
at a new room.

## What already exists (the spine we reuse)

The whole "walk around indoors" system is built for your *ship* and lives behind one page. It is
**greenfield for havens** — nothing in the client knows the word "ashore" yet — but every moving
part we need is here:

- **`Rendering/DeckPlan.cs`** — the single source of truth for interior geometry: `Walls[]`,
  `Consoles[]` (an interactable + a `ConsoleKind`), `RoomLabels[]`, spawn point, collision
  (`Move`/`Collides`, sliding), and the raycaster (`CastRay`). Today it's a `static` class holding
  one hardcoded ship. **This is the one piece we must generalize** (see below).
- **`Rendering/DeckView.cs`** — top-down interior. **`Rendering/FirstPersonView.cs`** — the 220-column
  raycast first-person twin, windows cut to the real ephemeris sky. Both render *the same*
  `DeckPlan` (DeckView.cs:8-9 says so). Swap the plan → both views change.
- **`Pages/Map.razor`** — the avatar loop and wiring: `MoveAvatar` (WASD / tank controls),
  `InteractAtConsole` (`switch (DeckPlan.NearestConsole(...))` → open a desk or fire an action),
  `LocationHint`, and the render dispatch gated on `_deckMode`/`_fpMode` at **~lines 4139-4155**.
  Controls are already taught in [deck-view.md](deck-view.md): `WASD` walk, `E` interact, `F`
  first-person, `Q` back to helm.
- **Docking state** — `_dockedHavenId` (Map.razor ~7535), `IsDockableHaven` (~7544), `ToggleDock`/
  `Undock`. **Docked state and the interior are completely independent today** — nothing in the
  deck render path or `InteractAtConsole` reads `_dockedHavenId`. That's exactly the seam to branch.

## The one real refactor

Turn `DeckPlan` from a static ship singleton into a **selectable plan**: a value (Walls, Consoles,
RoomLabels, Spawn, backdrops) that the two renderers and the avatar loop take *by reference*
instead of reaching into the static class. Then the ship is one plan and The Space Bar is another.
Everything downstream — collision, raycasting, console interaction, sky windows — works unchanged
against whichever plan is active.

## The frame (owner, 2026-07-07)

The big idea, in the owner's words: **you walk from the ship, through an airlock, into the space
station** — and each dock is a *different place*, a set of spaces that only exist while you're
clamped to that specific berth. Ashore is where you **meet people and get jobs**. The ship's own
interior is *not* touched or re-done; going ashore is a *new* room reached through a new hatch.

So a haven interior is never a reskin of your deck — it's `the-space-bar`'s bar, `cinder-roost`'s
scrap-yard, `the-tilt`'s listing lounge, each keyed by `body.Id`. "Get jobs / intel" reuses systems
the game already has (see *Wiring jobs & intel*, below) rather than inventing a parallel economy.

## Milestone 1 — ✅ shipped: the refactor + an empty walkable bar

Branch `go-ashore-space-bar`. Build clean, Core 255/255 green, both renderers verified in-browser
(ship deck + first-person unchanged after the refactor).

1. **`DeckPlan` is now a *selectable plan* (the one real refactor).** Was a `static` ship singleton;
   now an instance holding `Walls`/`Consoles`/`RoomLabels`/**`Backdrops`**/`Spawn` + per-plan droids
   (`DroidCount`/`FillDroids`) and a `Location(x,y)` hint. `DeckView.Draw` / `FirstPersonView.Draw`
   and the avatar loop take a `DeckPlan` by reference (`Map.razor._deckPlan`). `DeckPlan.Ship` holds
   today's ship data unchanged; ship-only dressing (crates/reactor/shuttle/tables) is gated to it by
   reference-equality. Backdrops moved out of `DeckView` into plan data (`Backdrop` records).
2. **`HavenInterior.ForBody("the-space-bar")`** → a 24×14 du room: four bulkheads, one panoramic
   window onto real space (the raycaster fills it from the live ephemeris while docked), the
   `the-space-bar.jpg` backdrop, and a single `⚓ BACK ABOARD` airlock. `null` for any other body.
3. **Go ashore / aboard.** New `ConsoleKind.Airlock`. A `⚓ GANGWAY` console sits on the ship's
   cantina window; press `E` docked → `GoAshore()` swaps `_deckPlan` to the haven's, drops you at its
   spawn. `E` on the bar's `⚓ BACK ABOARD` (or `Q`) → `GoAboard()`, restoring your spot aboard.
   Off a dock the gangway just explains itself; **undocking forces you aboard first**.

The bar is deliberately **empty** but for the way home — this milestone proves the plan-swap and the
entry/exit. Wiring the consoles is the next step, and it's a *data* change, not a structural one.

## Next — strangers at the tables (the follow-up, owner 2026-07-07)

**Not bar consoles — bar *tables*.** The classic RPG cliché: a mysterious figure sits at your booth
and offers a quest. The content is the *same* stuff you could dig off the online board — a dark-web
route tip, a huntable ship — but it comes to you as an *offer* across a table, not a menu you open.

Shape:

- **Tables with seated patrons.** A handful of bar tables, some occupied by a patron NPC. Patrons are
  per-plan fixtures — the same deterministic `FillDroids`-style sim-time function the ship droids use,
  but *seated* (fixed seats), each with a name/handle and a portrait/flavor.
- **Walk up, `E`, they make an offer.** Approaching an occupied table and pressing `E` opens a small
  offer card (a deck-mode overlay, extending the `.deck-pulse-toast` pattern): the stranger leans in
  with one lead — *"Word is the Gilt Sparrow runs Ceres→Mars dark. Interested?"* — and `[Take it]` /
  `[Pass]`.
- **The offers are exclusive to ashore — you can't get them off the web (owner).** That's *why* you
  go ashore: a stranger's contract is not something the online board/dark-web market would ever list.
  The *mechanics* still reuse what exists (a hunt targets a real ship, completes off the real capture
  event, pays real credits), but the specific contracts are the bar's own — not a duplicate menu.
- **Every haven offers the full range — no region-gating (owner).** Inner *and* outer havens can
  surface any quest kind. Because outer-system travel is so slow, locking quests to far docks would be
  punishing; so variety is universal. What differs per dock is *flavor* — the regulars, the framing,
  the local color — not which kinds of work exist.
- **Offers are deterministic & per-haven** — picked by `(bodyId, sim-time)` so they're stable and
  free, and each dock's regulars feel like *its* regulars, while the underlying quest set stays whole.
- **The bar counter** stays the "one beat" — a rum flavor line (reuse `PourRum`/`HeadQuip`).

**Accepting starts a tracked quest (owner's call).** Not a thin wrapper — a real contract with an
objective and a reward paid on completion. The strangers are quest-givers; the bar is where you pick
up work. Content still leans on live systems (a hunt quest targets a *real* huntable ship; it
completes off the *real* capture/defeat event; the reward is *real* credits), but there is now a
quest log that tracks state from offered → active → done → paid.

**Reuse the tutorials mechanic (owner's call).** A quest is *the same kind of thing a tutorial lesson
is* — a step/objective with completion tracking, rendered on a Captain's-desk tab — just on a **Quests
tab instead of Tutorials**, and started by accepting a stranger's offer rather than picking a lesson.
So the engine mostly exists (PR #91); the work is a Quests tab, a quest-flavoured "lesson", the
table/stranger offer that activates it, and a reward payout on completion.

### Quest system — milestones

- **M-Q1 · the loop, one vertical slice.** ✅ *built (branch `go-ashore-space-bar`; compiles, deck
  smoke-tested clean, pending a hands-on dock→hunt→payout playtest).* A hooded stranger sits at a
  Space Bar booth (`ConsoleKind.BarPatron` + a seated patron via the haven plan's `FillDroids`).
  Walk up, `E` → an offer card overlay (`.deck-offer-card`, Take/Pass). Accepting adds a `Quest`
  record (parallel to `_tutorialStep`, not sharing it); the hunt completes off the real holing
  (`npc.Disabled`) **or** boarding (`Board()`) of the stored `Ship.Id`; docking at any haven pays the
  reward into `_credits`. Targets prefer off-books ships (exclusive to ashore). *Turn-in is auto-on-
  dock for now; the Quests-tab view is M-Q2.*
- **M-Q2 · the Quests tab.** ✅ *built & verified in-browser (empty state renders, panels correctly
  hidden, no console errors).* A `📜 Quests` tab on the Captain's desk beside Tutorials, forking the
  Tutorials-tab card UI: a read-only `Contracts` ledger (`QuestItem` DTO), each card colour-accented
  by state — ▶ on the hook (amber) / ✓ done, collect at any berth (green) / 💰 paid (dim). Newest on
  top. Turn-in stays auto-on-dock; a manual "collect here" button can come later if wanted.
- **M-Q3 · more givers & a second kind.** ✅ *built (compiles, boots clean; gameplay pending a
  hands-on playtest).* Two named regulars now hold their own booths — **One-Eye Silas** (bounties →
  hunts) and **Madam Coil** (parcels → cargo runs) — each a seated patron + its own `BarPatron`
  console; `DeckPlan.NearestConsoleSpot` lets Map read *which* stranger you walked up to (from the
  console label) and pick their trade. New **cargo-run** kind: carry a parcel to another haven,
  completes when you berth there (hooked in `ToggleDock`), pays on the same dock. Offers are
  deterministic per `(sim-time, berth)` and skip anything already under contract. **Deferred:** the
  **intel** kind (an instant dark-web tip / boon, not a tracked task — needs the `IntelMarket`
  injection hook) and stranger **portraits** (Grok art) → rolled into M-Q4.
- **M-Q4 · intel kind + a third stranger.** ✅ *built (compiles, boots clean; gameplay pending
  playtest).* **Gilt-Eye** now holds the third booth as an **intel broker**. New **intel** kind: a
  whisper on an off-books ghost — accepting drops a fresh `RouteIntel` into `_intelLedger` on the spot
  (a free dark-web buy, `Price: 0`), so a ship that never shows on the public board joins your
  contacts 🕸-tagged for 30 days. Instant boon, no task; logged in the Quests tab as *🕸 Tip taken*.
  This completes the owner's original **"jobs / intel"** trio: Silas (hunts) · Coil (runs) · Gilt-Eye
  (intel). Offer card shows *"On the house — the tip's the payoff"* and a "Take the tip" button.
- **M-Q5 · texture (next).** Stranger **portraits** (Grok art); reputation/standing per haven;
  risk/reward tiers; a double-crossing stranger; offers gated by what you've done. After a playtest.

## The walk-through docking tube (2026-07-08)

Owner playtested the press-E "go ashore" and it wasn't the mental model: he expected **The Expanse** —
a **narrow umbilical tube with two automatic airlock doors** you **walk** your avatar down into the
station, continuously, no teleport. Reworked to that:

- **One welded deck while docked.** `HavenInterior.DockedDeck(id)` composes the **ship + tube +
  station** into a single `DeckPlan` (one coordinate space). The ship's cantina window wall gets a
  3-du mouth cut for the tube; the station room is offset (`StationDX/DY`) so its entrance gap lands
  on the tube's far end. Docking at a haven-with-interior swaps `_deckPlan` to this complex
  (`SetDeckForDock`); undocking reverts and pulls you aboard.
- **Two automatic airlock doors** (`DeckPlan.Door`, drawn in `DeckView`): shut across the passage,
  slide to jamb-stubs as you near them. Purely visual — always walkable, nobody gets stuck.
- **Follow-cam.** The complex is ~47 du tall — far too long for the fixed tactical frame — so
  `DeckPlan.FollowCam` makes the top-down view scroll to keep the avatar centred. The bare ship keeps
  whole-frame. First-person already follows you, so the tube works there for free.
- **No more teleport.** `GoAshore`/`GoAboard` and the gangway-console swap are gone; `_ashore` is now
  just "past the tube, in the station room," derived from avatar Y as you walk (`RefreshAshore`).
- **Tunables** (all in `HavenInterior.cs`): `TubeLeft/Right` (walkway width), `ShipTop`/`TubeTop`
  (length), `StationDX/DY` (room placement). Geometry built blind (no in-session browser) — expect to
  nudge these after the first visual playtest.
- **Next**: template the tube onto Ringside/Cinder Roost/The Tilt; FP-view doors; a board/hiss cue.

### Airlock vestibule + lobby/bar + rename (2026-07-08, owner playtest feedback)

- **Airlock off the galley.** Relocated to a **wide airlock corridor** (7-du slot between shuttle bay
  and cantina) ending in a **bumped-out vestibule** with a hatch and **two blast walls flanking it for
  cover** — an Expanse-style kill-box for repelling boarders. The bare ship seals the hatch; the
  complex opens it. Shuttle bay/cantina walls pulled back to x=-1 / x=6; cantina backdrop + window
  reanchored. `Tables` is now a plan property (cantina tables moved off the DeckView hardcode).
- **Two-room station.** Tube → **arrivals LOBBY** (Gen-AI backdrop, Mars/Phobos viewport) → **wide
  2-lane door** (x -1..6) → **BAR** (big room, 5 tables, 3 seated regulars). Constants in
  `HavenInterior.cs`: `ShipHatchY 14 · TubeTopY 22 · LobbyTopY 34 · BarTopY 50`.
- **Every bar is different.** Renamed the Mars station **The Space Bar → The Rusty Roadstead** (scenario
  body name + refs; id `the-space-bar` kept). Lobby image `art/the-rusty-roadstead-lobby.jpg` generated
  via `grok -p … --always-approve` (grok-composer image tool, style-matched to `the-space-bar.jpg`).
- **Still to do:** a stranger who *drifts over to your* table (the two open tables are for this);
  per-bar bar-room art; tune all the geometry once eyes are on it.

### Airport-sized station: the round immigration hall (2026-07-08)

Owner: "the station should be much bigger than our ship — 10+ ships docked, like an airport. A round
entrance hall with many doors (most locked), a Total Recall Mars-immigration desk, a 'most guests stay
two weeks' sign."

- **Round hall.** The rectangular lobby became a regular **12-gon ring** (`HallR 17`, ~34 du across —
  far bigger than the 20-wide ship), generated in a loop in `HavenInterior.Build`. Edge 8 (south) is
  our tube; edge 2 (north) is the wide door to the bar; the **other 10 edges are sealed berths** — a
  real wall with a cold "locked" hatch (`DeckPlan.Door.Locked`, steel-blue, drawn shut) and a
  `⚓02`…`⚓11` berth sign inside. Reads as a station with a dozen ships docked, ours in berth 1.
- **Immigration desk** (Total Recall): a counter facing the arriving player with a "Customs" officer
  droid behind it; walk around either end into the concourse. Signage: `MARS IMMIGRATION`, `most guests
  stay two weeks`, `⚓ THE RUSTY ROADSTEAD`.
- **Bar** hangs off the hall's north door (unchanged tables/regulars).
- `Door` gained a `Locked` flag; DeckView draws locked doors cold and shut. Hall geometry constants:
  `HallCenterX/Y · HallR · HallApothem · HallBottomY · HallTopY`.
- **Watch in playtest:** the hall is deliberately big, so ship→bar is a long (~55 du) walk — may want
  a shorter hall or a faster avatar; desk placement vs. the tube mouth; locked-hatch look.

### In-browser verification + bigger bar + Mars-view art (2026-07-08)

Drove the dev build via Chrome automation (held W through synthetic keydown) and confirmed the whole
walk boots and renders clean (no console errors): ship airlock vestibule → tube (auto doors) → round
immigration hall (art, locked `⚓02…11` berths, desk, officer, two-weeks sign) → bar. Fixes from what
I saw:
- **Immigration desk → a gate.** Split the solid counter into two counters with a central **gate
  aligned to the tube** (x 1..4), so you walk straight off the umbilical through the checkpoint instead
  of detouring around a wall (the old solid counter shoved the avatar around).
- **Bigger bar.** `BarLeft/Right -14..19`, `BarTopY = HallTopY+22` (was -8..13, +16) — a cavernous
  room; 7 tables spread out, 3 seated regulars.
- **Bar gets its own Mars view.** New grok backdrop `art/the-roadstead-bar.jpg` (spacious counter,
  neon bottles, a huge viewport onto Mars) replaces the reused `the-space-bar.jpg`.
- **Note:** deck walk is slow *in the Debug dev build / under automation* (~1 fps); nominal
  `AvatarSpeed` is 9 du/s, fine in the published Release build. Left unchanged.

## Start points — playtest jumps (2026-07-08)

"Jump to C so testing D is fast." A registry of named **start points** that arrange the just-built
world so a playtester lands right where the interesting bit begins — no long haul to set it up. Not
demo-only: it's also a genuine "where does this run begin?" choice ("why always Earth?").

- **Registry** (`StartPoints` in `Map.razor`): one `StartPoint(Id, Icon, Label, Blurb)` per locale,
  the single source of truth for both the URL and the picker. Today: `earth` (default), `space-bar`
  (docked **&** ashore in The Space Bar — the interior-test jump), `jupiter` (co-moving by Europa,
  fly the Galilean moons), `saturn` (by the Ringside Exchange).
- **URL fast path**: `/map?start=<id>` (parsed in the same loop as `?scenario=`). Unknown id → the
  picker. E.g. `/map?start=space-bar` boots straight into the bar's deck.
- **Boot picker**: with no `?start=`, a full-canvas chooser overlay (`_showStartPicker`) offers the
  registry; Earth just dismisses, the rest call `ApplyStart`.
- **Mechanics** (mirrors the tutorial `Seed*` jump): `PlaceShipForStart` reuses the finite-diff
  co-moving spawn idiom keyed off any body; the docked start sets `_dockedHavenId`/`_dockOffset`
  (the tick's `HoldAtDock` then pins the ship) and calls the real `GoAshore()`. `ApplyStart` is
  re-entrant (steps aboard + unclamps first), so a later Captain-tab reopen is a thin add.
- **Next**: a Captain's-desk "start here" reopen; a `mars-approach` start (undocked, to test the
  dock→gangway transition itself); per-mission optional start override.

### Hooks to reuse (verify before M-Q1)

- **The tutorials/lessons engine (PR #91)** — how a lesson defines its step(s), how completion is
  detected/advanced, and how the Captain's-desk **Tutorials tab** renders — the spine to fork for a
  Quests tab.
- **"Brought down" event** — where a hunt target is captured/holed/defeated (the objective trigger).
- **Credit payout** — how the ledger is credited (the reward).
- **Huntable-target source** — the traffic/tracking list the offer picks a live target from.

## Open decisions

- **Sky windows ashore**: while docked the ship is pinned at the dock, so the FP windows already show
  the station's ephemeris view (velocity = the station's). Good enough; revisit if a haven wants its
  own anchor.
- **Does the parrot come ashore?** Probably stays with the ship; the bar is where you *hear* rumor
  and fence goods.
- **Schema now or later?** The `bodyId → plan` map (`HavenInterior`) avoids touching `Scenario.cs`.
  Add a real per-body interior/flavor field only when several havens each want a rich room.

### Every station gets its own bar (2026-07-08)

The Rusty Roadstead's kit (airlock vestibule → tube → round immigration hall → bar) is now
**spec-driven**: `HavenInterior.BuildComplex(StationSpec)` builds the one geometry, and a
`StationSpec` supplies the name, the immigration authority, a deadpan quip, the bar's name, and the
two Gen-AI backdrops (hall + bar). `Specs[]` lists the four walkable havens; `DockedDeck` builds and
caches one complex per station on first dock. **Adding an interior station is now: a scenario body +
a one-line spec + two images.**

| Station | World | Immigration | Bar | Quip |
|---|---|---|---|---|
| The Rusty Roadstead | Mars | MARS | The Roadstead Bar | "most guests stay two weeks" |
| Cinder Roost | Venus' clouds | VENUS | The Cinder Lounge | "mind the sulphur, spacer" |
| Ringside Exchange | Saturn's rings | SATURN | The Ringside Bar | "trade fast — the rings don't wait" |
| The Tilt | Uranus | URANUS | The Tilt Bar | "everything's sideways out here" |

Each hall/bar backdrop shows that world out the glass (Venus' sulphur banks, Saturn's rings, Uranus'
sideways rings). **Start points** now cover all four docked (`?start=cinder-roost|space-bar|ringside|the-tilt`)
via a `DockedStarts` id→body map that reuses the one docked-arrival path in `ApplyStart`.

Still shared across every station (deliberately, for now): the three regulars (Silas / Coil /
Gilt-Eye) and the immigration officer — same faces, different view. Per-station strangers can come
with the multi-location missions.

### Multi-location, off-the-books missions — The Fixer's fetch job (2026-07-08)

The owner's frame: the **confidential** jobs are handed over **face to face at a bar table — no
electronic trace** — which is why they can't ride the public web board. So a job is picked up at one
station's table and **delivered in person at another**. First one, live:

- **The Fixer** — a fourth bar patron (back-corner table, seated at every station) who deals only
  confidential work. `giver.Contains("FIXER") → MakeFetchOffer`.
- **The job** (`QuestKind.Fetch`): a dead tycoon's cherry-red **Derelict Roadster** drifts sunward of
  Mars (a real `station`-kind scenario body, its GenerateDepots depot filtered out client-side so a
  wreck isn't a trading post) with an untraceable hardware wallet wedged in the seats. A one-off
  signature quest — offered only if it isn't already in the ledger.
- **Two legs, two places.** Fly to the wreck: coasting within ~100,000 km flips the quest
  `Active → PickedUp` (`CheckFetchPickup` in the tick, proximity only). Then fly to the destination
  station (an interior haven other than where you took it, `OfferIndex`-picked — e.g. The Tilt), walk
  to its bar, and press E on **The Fixer there**: `DeliverFetch` pays 4,200 cr on the spot, under the
  table — unlike a cargo run (settled on berthing), delivery only completes face to face.
- `QuestState` gains **`PickedUp`** between Active and Complete; `Quest` gains `SourceBodyId`.

Verified in-browser: walked to The Fixer, took the job (dest → The Tilt), confirmed it in the Quests
ledger and the wreck on the nav map (no depot); build clean, no console errors. The long fetch flight
+ in-person hand-off is left to hands-on playtest. Builds on Coil's cross-haven cargo-run pattern.

Next: per-station Fixer flavor / more fetch targets; a stranger who drifts to *your* table with the
job instead of you seeking them out.

## Later (beyond the follow-up)

A real bounty/contract accept-flow if the "front for existing systems" wiring proves too thin; heat
visibly cooling while ashore; a "third tot" style bar event; fencing cargo at the bar.

See also: [deck-view.md](deck-view.md), [dock-and-economy.md](dock-and-economy.md),
[wolf-aim.md](wolf-aim.md), [dark-web.md](dark-web.md), `docs/MondayPonder/3DRenovationPlan.md`.
