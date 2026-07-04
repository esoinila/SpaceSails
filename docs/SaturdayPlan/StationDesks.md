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
| 1 | `1` | **Nav** | The map + plotting card (today's view, decluttered) | ship speed / warp / next closest-pass |
| 2 | `2` | **Sensors** | Scope wall: one live scope PER tracked target simultaneously + big sun-blind rosette + sweep controls + ledger; dimmed map as background | tracks N/M, freshest + stalest track |
| 3 | `3` | **War room** | Full-screen tactical circle, weapon rings, hail/warn/bribe console, heat gauge large | heat level 🔥, nearest hunter bearing |
| 4 | `4` | **Trade** | Local space contacts, drone-transfer console, dock market, cargo manifest, prices | credits, cargo units, active transfer % |
| 5 | `5` | **Comms** | Dark web market, departures board (big), tight-beam console, laser ranging, news wire | fresh intel count, next known departure |
| 6 | `6` | **Galley** | News feed (world events, plunder rumors, price gossip), the rum locker, crew flavor | — (the galley IS the summary place) |
| 7 | `7` | **Deck** | Walk the ship (existing deck/first-person). Bridge seats = sit at a console → that desk opens | — |

## Rules

1. **70% rule**: the desk's own topic fills the screen; no desk crams its content into a card.
2. **Summary rule**: other desks appear only as small, standardized summary chips (one
   component per desk, reused everywhere) docked on a thin edge strip. Click a chip → jump
   to that desk. No full panels on foreign desks.
3. **Switching**: number keys 1–7 + a slim station tab bar. Walking to a bridge seat and
   pressing E does the same thing (the seats already exist in DeckPlan for several consoles).
4. **The map stays alive**: the simulation and canvas keep running regardless of desk; Nav
   shows it full; Sensors shows it dimmed behind the scope wall; others may hide it.
5. **Migration**: today's toolbar toggles (Track/Guns/Local/Web panels as pop-ups over the
   map) are replaced by desks; the map toolbar keeps only map things (warp, pause, follow,
   plot, scope, traffic → traffic moves to Comms eventually).

## Addendum (owner, 2026-07-04 evening): objective-style summaries + the captain's position

- **A chip is the station's tightest *objective* summary, not raw stats.** Nav chip = the
  destination implied by the set course ("Earth → Mars orbit"; "free sailing" when none).
  Sensors chip = the ship/target of interest — what we're looking for or the best track.
  Trade chip = the active deal. War room chip = the current threat posture.
- **The captain's position** (new desk, key `0`): the captain selects the SHIP'S GOAL — the
  mission, e.g. "Hunt: He3 haulers off Titan", "Trade run: Earth → Mars", "Lay low:
  Enceladus", "Survey: Saturn–Mars corridor". The selected mission's detail is what the
  other stations show as the captain's summary chip, and stations may highlight
  mission-relevant rows (the hunted cargo class, the lay-low haven...).
- **The feel**: a spaceship run by a crew (the Expanse bridge) — to do another station's
  detailed work you *move* there; from your seat you only see the others' one-liners.

## PR lanes (builds on PR-8's stripped Map.razor — merge #34/#35 first)

- **PR-11 · Desk framework**: `_activeDesk` state, number-key routing, station tab bar,
  full-screen desk container layer, the summary-chip component system, Nav desk = current
  map experience, Galley desk v1 (rum + news stub + all summary chips). Anchor comments for
  the desk lanes.
- **PR-12 · Sensors desk**: the scope wall (grid of live scope views, one per tracked
  target), big rosette, sweep programs UI, dimmed-map background.
- **PR-13 · War room + Trade desks**: full-screen versions of the gun deck and
  local-space/dock/market experiences.
- **PR-14 · Comms desk + news wire**: dark web + departures + tight-beam + laser ranging
  desk; a deterministic news/rumor generator feeding Comms and the Galley feed; bridge
  seats in DeckView wired to desks.

- **PR-15 · The captain's position**: mission model (deterministic, UI-level state or a
  small Core `ShipMission` type), captain's desk (key `0`) for selecting the goal, the
  mission chip on every desk, mission-relevant highlighting on Sensors/Comms/Trade rows.

PR-12/13/14 are parallel after PR-11 (each owns its own desk component files; Map.razor
touched only at desk anchors). PR-15 follows PR-11 and can run alongside 12/13/14.
