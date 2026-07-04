# Scenarios

What this is: the different solar systems you can sail, and how the game loads them.

Where: pick one from the home page (`/`), or launch the map directly with a `?scenario=` query
string, e.g. `/map?scenario=sol-eu`.

## The three voyages

| Card | Slug | File | What it is |
|---|---|---|---|
| Sol | `sol` | `scenarios/sol.json` | The real solar system, pure Newtonian. Nine worlds on rails, He3 haulers inbound from Saturn, Luna's mass drivers lobbing compute-core pods. Learn to fly here. |
| Sol (Electric) ⚡ | `sol-eu` | `scenarios/sol-eu.json` | The same sky, awake — a charged plasma environment (see [electric-sky.md](electric-sky.md)). |
| The Wheel | `wheel` | `scenarios/wheel.json` | A rigid-spoke curiosity system: Venus, Earth, and Mars ride a spoke around Saturn as hub. The spoke isn't gravity's doing, but your ship still obeys Newton. Also electric. |

The home page's Launch buttons just link to `map?scenario=<slug>` — there's nothing special about
using the buttons versus typing the URL yourself.

## The query string

The map page reads three optional query parameters at load:

- **`scenario=<slug>`** — loads `scenarios/<slug>.json`. The slug is sanitized to letters, digits,
  and hyphens only before use (it becomes a URL path segment); an unrecognized or malformed value
  silently falls back to `sol`.
- **`mp=1`** — joins multiplayer instead of single-player (see the Home page's "Join the crew"
  card, which builds this URL for you).
- **`callsign=<name>`** — your multiplayer display name (only meaningful with `mp=1`).

You can combine `scenario=` with `mp=1` to run a shared multiplayer session on any scenario, not
just Sol.

See also: [electric-sky.md](electric-sky.md) for what the `EU ⚡` scenarios add,
[map-and-warp.md](map-and-warp.md) for the view all scenarios share.
