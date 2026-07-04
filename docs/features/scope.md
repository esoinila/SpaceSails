# Scope

What this is: a close-up instrument view of one contact at a time — what it looks like, how far
away it is, and how fast it's closing.

Where: press the **Scope** toolbar button on the map, or walk to the **SCOPE** console on the
[deck](deck-view.md) and press `E`.

## What it draws

One locked target, rendered by kind:

- **Celestial body** — the sun (corona and flares) or a planet (day/night terminator; Saturn gets
  rings).
- **Freighter** — sail, hull, engines.
- **Cargo pod** — mass-driver cradle with a blinking beacon.
- **Another player's ship** (multiplayer) — a "dart" sprite.

Breathing lock brackets frame the target, with its name, kind label (`STAR`/`PLANET`/`FREIGHTER`/
`CARGO POD`/`SHIP·CREW`), formatted distance (km, thousands of km, or AU as appropriate), and
relative speed in km/s. If the target sits inside a plasma stream, a wisp overlay is drawn over
it (see [electric-sky.md](electric-sky.md)). Nothing locked shows `NO TARGET` static.

## Auto-lock vs. manual

The label in the corner reads **◆ AUTO** or **◆ TRACK**:

- **◀ / ▶** step manually through every candidate (currently-observed ships, then every celestial
  body) — this switches the label to TRACK.
- The middle button returns to **AUTO**, which re-locks by priority: your selected traffic-board
  target first, else the nearest currently-observed contact, else the nearest celestial body. The
  scope always has *something* to show — optical truth only, so an unobserved ship never appears
  even in auto mode.

See also: [traffic-board.md](traffic-board.md) for selecting a target, [boarding-run.md](boarding-run.md)
for closing to capture range.
