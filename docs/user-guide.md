# Captain's Guide

*(This document mirrors the in-game guide at /guide.)*


SpaceSails is a solar-system sailing and piracy game with honest orbital mechanics.
Nothing here cheats: every trajectory is integrated, every orbit obeys the sun, and the
hardest maneuvers are hard because physics says so. This guide covers everything currently
aboard.



## 1. Choosing a voyage

- **Sol** — the classic system. Learn to fly here.
- **Sol (Electric)** ⚡ — same system plus a charged plasma environment:
a solar halo that charges your hull near the sun and flowing plasma streams between
planets. Charge makes you visible and eventually arcs; vent it with `V`.
- **The Wheel** — a rigid-spoke curiosity system with a plasma river,
for pilots who want strange skies.
- **Join the crew** — multiplayer: enter a callsign and share a live
session. Warp runs at the *slowest* crew member's request (the min-warp rule),
and you only see what your own sensors can see.



## 2. The map

- **Drag** to pan, **mouse wheel** to zoom, **Follow Ship** to re-center.
- **Warp slider** (top left) — logarithmic, 1× to 10,000×. Warp automatically
drops near planets and encounters so you don't overshoot the interesting parts.
- **HUD readouts** — sim time, ship speed with
*(circular here: …)* beside it: the speed that would hold a circular sun orbit at
your current distance. Match it and you coast forever; it is the difference between
matching a planet's *radius* and matching its *orbit*.
- **Mass pulses** — your reaction mass. Every burn spends pulses; refill by
docking at a market. Run dry far from port and you drift on whatever orbit you bought.



## 3. Flying by hand

- `+` / `−` (or `↑`/`↓`) — thrust pulse:
scales your velocity ±10%. Prograde only — pulses change your speed, never your heading.
- `Shift` + pulse — **fine trim**, ±1%. For station-keeping and
orbit matching.
- `V` — vent charge (Electric scenarios).
- Pulses have a short cooldown and each costs one mass pulse.


Rule of thumb: to go *inward* (Venus, Mercury), *brake* — losing speed drops
your perihelion. To go *outward* (Mars, Jupiter, Saturn), accelerate. You are always
trading speed for altitude on an ellipse.



## 4. Plotting a course

- **Plot** opens the nav desk (or press `E` at the NAV POST inside
the ship). The sim pauses while you plan.
- **Scrub slider** — slide into the future; every planet shows a
*ghost* at the scrubbed time, tethered to its live position.
- **Path length slider** — how far ahead your ribbon projects (5 days to
2 years, log scale). *auto* follows your last burn + 90 days. Short for
ship-to-ship work, long for interplanetary sails.
- **Add burn at scrub** — drops a maneuver node at the scrubbed time. Each
node has: **+/−** direction, **pulse count**, and a
**free percent field** — any decimal from 0.01% to 50% per pulse. A 10%
pulse is a hammer (~3 km/s); a 0.5% node is a scalpel.
- **Click a node marker on the ribbon** to select it — its row highlights
and the scrub jumps to that moment. **@** re-times a node to the current
scrub; **×** deletes it.
- The whole trip fits one plan: Earth→Saturn is a single sit-down (the plotting horizon
was sized for exactly that).
- **Closest pass** — the plot card names your tightest flyby along the planned path, in body
radii, with a marker on the ribbon. Under 5 R it turns yellow; through a planet it turns red
and says *IMPACT, captain*.



### Worked examples

- **Mercury**: one node, *decelerate ×3* (10%) at ~day 3 →
perihelion kisses Mercury's orbit ~day 334. At closest approach, brake twice more and
trim until ship speed equals *circular here* (47.9 km/s) — then cut the gas and
orbit forever.
- **Saturn**: one node, *accelerate ×12* at the right departure day
(phasing!) → Saturn's port zone in ~9 months. Use the ghosts to find the day when
ghost-Saturn meets your ghost-ship.



## 5. Piracy

- **Traffic** opens the shipping board: cargo pods and freighters with
routes, departure times, and last-seen data. Select one and **Pin** its
predicted path — a cone of where it can be, given its maneuver budget.
- Plot an intercept so your ribbon crosses the cone, close to within the
**capture envelope** (500,000 km and 5 km/s relative), and hold it.
The boarding clock runs on *wall-clock time* — shuttles fly in real time, warp
be damned. A tighter, slower pass boards faster.
- Or fly it yourself: walk to the **SHUTTLE BAY** while the window is
engaged and press `E` — see §7.
- Boarded cargo goes in your hold. **Dock** at a market (Earth, Mars,
Venus — get within the port zone) to sell, refill mass pulses, and buy upgrades.
He3 pays best; ice pays the rent.



## 6. The scope

- **Scope** opens the instrument screen: auto-locks the nearest interesting
contact, draws it (freighters, pods, players, planets, the sun, plasma wisps), and
reads out distance and relative speed.
- **◀ / ▶** cycle targets manually; the middle button returns to
**AUTO**.



## 7. Inside the ship

- **Deck** — top-down plan of your pirate sail. Walk with
`WASD`/arrows, interact with `E`, drag the map if the bow hides
behind a panel. Crew: droids K-77 and R-3B stand by the shuttle; V-1K patrols.
- `F` — **first person**. Walk the corridors; the windows show the
real sky — the sun blazes bigger the closer you sail, and the planets are where the
ephemeris says they are. `Q` returns to the helm.
- Consoles: **HELM / NAV POST** (plotting), **SCOPE**,
**CANTINA** (morale), **CARGO HOLD** (your loot as crates),
**VENT PANEL**, **SHUTTLE BAY**.
- **The boarding run**: with a capture window engaged, `E` at the
shuttle bay puts you on the stick. Cross the gap with `WASD` thrust, dock at
the airlock *below the speed limit* (come in hot and you bounce), and the droids
swarm aboard — instant boarding. `Q` aborts; losing the window auto-returns
the shuttle. The prey's drift is your real relative velocity — a sloppy pass by the
mothership makes a hard run for the pilot.



## 8. The electric sky ⚡

- Near the sun the plasma halo charges your hull; the flowing ribbons between planets
are **plasma streams** — riding one pushes you along it (charged hulls
feel the current).
- Charge makes you glow on everyone's sensors — it pierces even sun glare — and at full
charge you start arcing. `V` vents.
- Mercury's neighborhood runs ~75% ambient charge. Ambush country: everyone there is
visible, desperate, or both.



## 9. The physics, honestly

- **There is no drag.** A circular orbit holds forever with zero thrust
(measured: −0.025% radius drift over a full year).
- What feels like drag near a planet is the planet's gravity plus solar tide shearing
you off your line. Get inside a planet's sphere of influence and it owns you; match
speed at the same sun-distance far from it and you fly formation with it forever.
- Fire your circularization at perihelion or aphelion — those are the moments your
velocity is purely tangential, and pulses only scale velocity, never rotate it.
- Phasing beats thrust: launch day matters more than pulse count for reaching a moving
target. Scrub, watch the ghosts, move the node.



Fair winds and following gravity, captain. 🏴‍☠️
