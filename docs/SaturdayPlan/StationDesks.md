# Station desks — the UI refit (owner direction, 2026-07-04)

*The owner's words, distilled: at each station, that station's topic should own ~70% of the
screen. Big maps belong to navigation (and as background on sensors); specialist positions
show their topic richly — the telescope desk shows ALL targets at once, not one small box.
Nothing needs to be crammed into small pop-up panels. Other stations' info appears only as
small summary pop-ups — info-rich on the specialist desk, summary elsewhere; anything more
just clutters the UI. The player switches stations by shortcut (no walking required), the
info-space is organized by station, the bridge has seats for the relevant ones, and sitting
in the Galley fills the screen with galley stuff (news etc.) plus summaries of the rest.*

## The desks

| # | Key | Desk | The big instrument (≥70% of screen) | Its summary pop-up (shown on other desks) |
|---|-----|------|--------------------------------------|-------------------------------------------|
| 1 | `1` | **Nav** | The map + plotting card (decluttered helm) | destination from set course ("→ Mars orbit"), speed/warp |
| 2 | `2` | **Sensors** | Scope wall: one live scope PER tracked target simultaneously + big sun-blind rosette + sweep controls + ledger; dimmed map as background | target of interest / active sweep |
| 3 | `3` | **War room** | Full-screen tactical circle, weapon rings, hail/warn/bribe console, heat gauge large | threat posture ("heat 🔥N · hunter 2.1 Mkm" / "quiet skies") |
| 4 | `4` | **Trade** | Local space contacts, drone-transfer console, dock market, cargo manifest, prices | the active deal, else credits+cargo |
| 5 | `5` | **Comms** | Dark web market, departures board (big), tight-beam console, laser ranging, news wire | freshest intel / "no whispers" |
| 6 | `6` | **Galley** | News feed (world events, plunder rumors, price gossip), the rum locker, crew flavor | tots poured / wobble |
| 7 | `7` | **Deck** | Walk the ship (existing deck/first-person). Bridge seats = sit at a console → that desk opens | — |
| 0 | `0` | **Captain** | Select the SHIP'S GOAL (the mission) | the mission — echoed on every desk's chip strip |

## Rules

1. **70% rule**: the desk's own topic fills the screen; no desk crams its content into a card.
2. **Summary rule**: other desks appear only as small, standardized summary chips (one
   component per desk, reused everywhere) docked on a thin edge strip. A chip is the
   station's tightest *current-objective* summary, not raw stats. Click a chip → jump to
   that desk. No full panels on foreign desks.
3. **Switching**: number keys (0–7) + a slim station tab bar. Walking to a bridge seat and
   pressing E does the same thing.
4. **The map stays alive**: the simulation and canvas keep running regardless of desk; Nav
   shows it full; Sensors shows it dimmed behind the scope wall; others may hide it.
5. **Migration & pruning (owner: prune ALL excess buttons)**: the Nav toolbar keeps ONLY
   true nav things: warp slider, Pause, Follow Ship, Plot, Scope, `?`. Traffic board →
   Comms desk; Dock panel → Trade desk; First hunt → behind the `?` affordance; Deck
   button → the tab bar (key 7). A summary chip may carry its one most-relevant quick
   action as the desk lanes mature — organized into the pop-up, never back onto the toolbar.

## Addendum (owner, 2026-07-04 evening): objective summaries + the captain's position

- **A chip is the station's tightest *objective* summary.** Nav chip = destination implied
  by the set course ("Earth → Mars orbit"; "free sailing" when none). Sensors chip = the
  ship/target of interest — what we're looking for or the best track. Trade chip = the
  active deal. War room chip = the threat posture.
- **The captain's position** (key `0`): the captain selects the SHIP'S GOAL — e.g. "Hunt:
  He3 haulers off Titan", "Trade run: Earth → Mars", "Lay low: Enceladus", "Survey:
  Saturn–Mars corridor". The mission's detail is the captain's summary chip on all other
  desks, and stations highlight mission-relevant rows (the hunted cargo class, the lay-low
  haven...).
- **The feel**: a spaceship run by a crew (the Expanse bridge) — to do another station's
  detailed work you *move* there; from your seat you see the others' one-liners.

## PR lanes

- **PR-11 · Desk framework** — DONE (#36): ShipDesk enum, keys 1–7, tab bar, desk layer,
  DeskChips (objective style, captain slot reserved), FullScreen hosting, Galley v1,
  toolbar pruned, traffic→Comms, dock→Trade.
- **PR-12 · Sensors desk**: the scope wall (a live scope per tracked target), big rosette,
  sweep programs UI, dimmed-map background.
- **PR-13 · War room + Trade desks**: full-screen tactical circle; Trade desk unifying
  local space + dock market + cargo manifest.
- **PR-14 · Comms desk + news wire**: dark web + departures + tight-beam + laser ranging
  as one comms layout; deterministic news/rumor wire (Core) feeding Comms and the Galley;
  bridge seats in DeckView wired to desks.
- **PR-15 · The captain's position**: mission model, captain's desk (key `0`), the mission
  chip on every desk. (Mission-relevant row highlighting inside Sensors/Comms/Trade lands
  as a follow-up after 12–14 merge, to avoid cross-lane file conflicts.)

- **PR-16 · The ship's parrot 🦜** (owner, 2026-07-04): the parrot is the ship's alarm
  system with personality — deterministic squawks triggered by game state: "drunk driver!"
  (flying with rum wobble), "WE'RE GOING TO CRASH — <body>!" (closest-pass IMPACT), "hunter
  on the wind!", "she's glowing!" (near ARCING), "prey in the glass!" (boarding window),
  "off the books, off the books!" (secretive ship found by sweep). A small perch element
  visible on every desk, one squawk at a time with a cooldown, existing PlayCue audio hook.
  Lands after PR-12..15 merge (touches shared UI).

PR-12/13/14/15 run in parallel after PR-11; each owns its own component files.
