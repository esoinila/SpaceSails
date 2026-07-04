# Scenarios

What this is: the different solar systems you can sail, and how the game loads them.

Where: pick one from the home page (`/`), or launch the map directly with a `?scenario=` query
string, e.g. `/map?scenario=sol-eu`.

## The four voyages

| Card | Slug | File | What it is |
|---|---|---|---|
| Sol | `sol` | `scenarios/sol.json` | The real solar system, pure Newtonian. Nine worlds on rails, He3 haulers inbound from Saturn, Luna's mass drivers lobbing compute-core pods. Learn to fly here. |
| Sol (Electric) ⚡ | `sol-eu` | `scenarios/sol-eu.json` | The same sky, awake — a charged plasma environment (see [electric-sky.md](electric-sky.md)). |
| The Wheel | `wheel` | `scenarios/wheel.json` | A rigid-spoke curiosity system: Venus, Earth, and Mars ride a spoke around Saturn as hub. The spoke isn't gravity's doing, but your ship still obeys Newton. Also electric. |
| Sol (Miners' Folly) 🌙 | `oops` | `scenarios/oops.json` | Sol, pure Newtonian, one accident later: a mining rig shorted its own capacitor and kicked Luna's orbital speed +15%. Wider, more eccentric, still bound — the honest computed aftermath from `labs/12-oops-at-the-moon`. The wreck itself is a new haven station orbiting Luna. |

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

## Stations and havens (the outer reaches)

Beyond ordinary planets and moons, a scenario body can be tagged with a `kind`:

- **`"station"`** — a lightweight orbital POI (no real gravity — `mu: 0`, just a small marker
  radius): compute farms, satellite works, trading posts. Sol has **Mercury Compute Farms** (low
  Mercury orbit), **Highport Satellite Works** (Earth LEO), and **Ringside Exchange** (Saturn orbit,
  near Titan).
- **`"moon"`** — an ordinary moon (Luna, Titan, Enceladus, Europa, Ganymede, Callisto in Sol).
- **`haven: true`** — a pirate haven: trade and repair, no questions asked, per the "scum and
  villainy work the outer reaches" theme. Sol's havens are **Enceladus** and **Ringside Exchange**
  (which is both a station and a haven).

On the map, station bodies draw as a small, distinctly colored **teal blip** regardless of id, and
haven bodies get a subtle **crimson tint** on their marker and label — a quick visual cue for
"something to trade at" versus "just scenery" while zoomed out. Every station and haven also gets
its own [orbital depot](depots.md), so there's always something to steal there too.

Scenario authors: a `ScenarioDefinition` can also carry an optional `traffic` section (`routes`,
each with `from`/`to`/`cargo`/`weight`/`publishesTimetable`, plus `podLaunchers`) to drive NPC
traffic from data instead of the hardcoded Sol tables — see the [traffic
board](traffic-board.md#off-the-books-ships) for what `publishesTimetable: false` does to a route's
ships.

See also: [electric-sky.md](electric-sky.md) for what the `EU ⚡` scenarios add,
[map-and-warp.md](map-and-warp.md) for the view all scenarios share,
[local-space.md](local-space.md) for what stations/havens offer once you're in orbit near one,
[dark-web.md](dark-web.md) for the far-trading-post/haven intel trade.
