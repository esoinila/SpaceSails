# Deck view & cantina

What this is: the top-down interior of your ship — walk between consoles instead of clicking
buttons, and pour a tot in the cantina if you feel like it.

Where: the **Deck desk** — press `7` or click **7 Deck** in the station tab bar (see
[station-desks.md](station-desks.md)). Pressing `1`-`6` from the deck leaves it and jumps straight
to the matching desk.

## Moving around

- `WASD` or the arrow keys walk your avatar around the deck plan (screen-relative: forward is
  toward the bow).
- Movement collides with the hull and interior walls — you slide along them, you don't clip
  through.
- `E` interacts with the nearest console once you're close enough; it highlights and shows an
  `[E]` prompt when you're in range.
- `F` switches to first-person mode (see below); `Q` returns you to the helm from anywhere on
  the deck or from first-person.

## The consoles

Walking up to a console and pressing `E` opens the matching desk (see
[station-desks.md](station-desks.md)) directly, without touching the number keys:

- **HELM** / **NAV POST** — opens the **Nav** desk (`1`), same as the old plotting-desk shortcut;
  the nav post also lights the plotting table.
- **SCOPE** — opens the **Sensors** desk (`2`), the scope wall.
- **CANTINA** — opens the **Galley** desk (`6`) — the rum locker (see below) lives there now,
  alongside the news wire.
- **CARGO** (in the Cargo Hold) — shows your loot as crates.
- **VENT PANEL** — vents hull charge (Electric scenarios only — see [electric-sky.md](electric-sky.md)).
- **SHUTTLE BAY** — launches the boarding shuttle when a capture window is engaged — see
  [boarding-run.md](boarding-run.md).

## Bridge seats (PR-14)

Three more consoles sit on the bridge alongside HELM/NAV POST/SCOPE — pressing `E` at one opens
the matching desk exactly like the rest, so the whole duty roster is reachable on foot:

- **COMMS SEAT** (starboard, mirroring SCOPE) — opens the **Comms** desk (`5`).
- **TACTICAL SEAT** (port side, near the bridge door) — opens the **War room** desk (`3`).
- **TRADE SEAT** (the bow-tip nook between HELM and NAV POST) — opens the **Trade** desk (`4`).

Sitting at any bridge seat leaves deck mode the same way HELM/NAV POST/SCOPE always have — one
`E` press, no walking required to get back once you're already there.

## Crew

Three droids wander the deck, purely for atmosphere — they don't affect gameplay: **K-77** and
**R-3B** idle by the shuttle bay, and **V-1K** patrols the corridor back and forth.

## The cantina — mind the third tot

Interacting at the CANTINA opens the Galley desk, where the "Pour a tot" button pours a rum tot
with a flavor line. Pour again within 90 seconds and the tot count climbs; wait longer and it
resets to one. On the **third tot inside that 90-second window**, the deck turns tilty for **25
seconds**: your movement direction wobbles noticeably, but collision and interactions still work
exactly the same — it's a disorientation effect, not a penalty to what you can do. Pace your
drinking, or don't; nothing but your own steering suffers.

The Galley desk (`6`, see [station-desks.md](station-desks.md)) and the CANTINA console are two
doors into the exact same rum ledger, so the tot count and wobble state stay in sync no matter
which one you use to get there.

## First person

`F` from the deck plan switches to a first-person walk through the same corridors. The windows
show the real sky — the sun grows visibly larger the closer you are to it, and every planet sits
exactly where the ephemeris says it should. `Q` returns to the helm from here too, and `F` again
returns to the top-down deck plan.

See also: [plotting-desk.md](plotting-desk.md), [scope.md](scope.md),
[boarding-run.md](boarding-run.md), [electric-sky.md](electric-sky.md).
