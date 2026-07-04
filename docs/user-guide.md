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
- Any voyage can be loaded straight from a link: `/map?scenario=sol`,
`sol-eu`, or `wheel`. Append `&mp=1&callsign=YourName` to join multiplayer
directly on that scenario, no home-page click needed.



## 2. The map

- **Drag** to pan, **mouse wheel** to zoom, **Follow Ship** to re-center.
- **Warp slider** (top left) — logarithmic, 1× to 10,000×. Warp automatically
drops near planets and encounters so you don't overshoot the interesting parts.
Above 100×, the sim advances in fixed 60-second quanta instead of every frame —
the same clock the traffic runs on, so nothing drifts out of sync at the high end.
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
and says *IMPACT, captain*. When that pass is a planet close enough to matter, an **Insert at
*body* pass** button appears — arm it and the ship parks itself in orbit the instant the window
opens during live flight (see §5). Disarm the same way, by clicking it again.



### Worked examples

- **Mercury**: one node, *decelerate ×3* (10%) at ~day 3 →
perihelion kisses Mercury's orbit ~day 334. At closest approach, brake twice more and
trim until ship speed equals *circular here* (47.9 km/s) — then cut the gas and
orbit forever.
- **Saturn**: one node, *accelerate ×12* at the right departure day
(phasing!) → Saturn's port zone in ~9 months. Use the ghosts to find the day when
ghost-Saturn meets your ghost-ship.



## 5. Orbit assist

- Get near a planet and a strip appears in the HUD: **🛰 Orbit *body* —**
*window OPEN*, *too fast (max 5.0 km/s rel)*, or *get inside the Hill sphere*.
Two bars underneath show distance-vs-Hill-sphere and speed-vs-limit at a glance.
- Press **O** (or the panel's **Enter orbit** button) once the window is open.
It's an instant burn that matches the body's velocity plus local circular
speed — the pulse cost scales honestly with the actual Δv needed, so a sloppy
fast approach costs more pulses than a gentle one. The button disables itself
if you can't afford the cost.
- The panel favors an **armed** target over merely-nearest, so plotting a
planned insertion at Mars won't get hijacked by a HUD strip for Earth on the
way past — see §4's Closest pass note for arming one in advance.
- The sun never shows this panel — you already orbit it by definition.



## 6. Piracy

- **Traffic** opens the shipping board: cargo pods, freighters, and one
plunderable **orbital depot per planet** (M22 — cargo flavor follows the world:
compute cores at Mercury, alloys at Venus, machinery at Earth, ice at Mars, He3
everywhere further out). Depots ride a fixed circular orbit and never maneuver
— orbit-assist into the same planet and boarding is close to the best case.
- Select a target and **Pin** its predicted path — a cone of where it can be,
given its maneuver budget.
- Plot an intercept so your ribbon crosses the cone, close to within the
**capture envelope** (500,000 km and 5 km/s relative), and hold it.
The boarding clock runs on *wall-clock time* — shuttles fly in real time, warp
be damned. A tighter, slower pass boards faster.
- Or fly it yourself: walk to the **SHUTTLE BAY** while the window is
engaged and press `E` — see §12.
- Boarded cargo goes in your hold. **Dock** at a market (Earth, Mars,
Venus — get within the port zone) to **sell cargo** (He3 pays best at 1200
cr/unit, then compute cores, alloys, machinery; ice pays the rent at 100) and
**refill mass pulses** for free. Spend credits on four upgrade tracks —
reaction-mass capacity, sensor range, cargo hold, and telescope count — each
a level-up costing 2,000 credits and doubling every level thereafter.



## 7. The tracking post 📡

- Press **Track 📡** to aim the ship's telescope at a bearing and arc, then
**Start sweep**. Sweeping isn't instant — a full 360° survey takes 6 sim-hours,
scaling down for a narrower wedge — but it finds ships the traffic board can't:
secretive haulers that don't publish a timetable.
- Detection range depends on which way you're looking relative to the sun: a
rosette next to the sliders shows the egg-shape live. Pointed straight at the
sun you're nearly blind (8% of the telescope's 6×10¹¹ m base range); pointed
straight away from it (anti-sunward) you see the full range — the pirate's
best hunting angle, dark sky at your back.
- A ready-made **scanning program** dropdown covers every known trade corridor
(Venus–Earth, Earth–Mars, and onward to Jupiter and Saturn) so you don't have
to eyeball the aim yourself.
- Once found, a contact goes on the **tracked-targets ledger**. Keeping the
lock is cheap: **Confirm** does a short re-look at its predicted position
rather than a fresh sweep, and bumps quality back up — skip it too long (5+
days) or let the target burn hard enough to slip the cone, and quality decays
until the lock is lost for good.
- **Telescope count** (an upgrade at the dock, alongside reaction mass, sensor
range, and cargo hold) is how many ships you can hold on the ledger at once —
1 at the base level, up to 4 at max.
- A well-tracked ship draws with a tighter ring on the map itself — a good,
fresh lock visibly sharpens the intercept, down to 30% of the ordinary
prediction-cone width at a perfect reconfirm.

## 8. Local space 🛰

- The moment you enter orbit around any body, a **Local space** panel opens on
its own (toggle it any time with **Local 🛰**) listing everything else parked
there: depots 🛰, stations 🏭, moons 🌙, havens 🏴, and any ship 🚀 caught nearby,
each tagged with what it offers — Trade, Fence, or Board.
- Trading works two ways: **same-orbit** (you and the counterpart are bound to
the same body — the classic bus stop) or **course-matched** (within 500,000 km
and under 2 km/s relative speed of a moving partner) — a friendlier envelope
than boarding, since cooperative cargo drones aren't chasing anyone.
- Hit **Trade** and drones ferry your whole hold over in real time (a striped
progress bar, ~20 seconds per cargo unit at a clean match, slower the sloppier
the geometry) — the same sale prices as docking, just a second place to make it.
Drift out of range mid-transfer and the progress is lost, no partial credit.
- Anything the panel lists that's co-orbiting your current body also gets a
subtle ring on the map itself, right where you're already looking.

## 9. The dark space web 🕸

- Press **Web 🕸** for the black market in information. It only opens for
business at a **pirate haven** or a **far trading post** — any station beyond
4×10¹¹ m from the sun; ordinary planets and central-space stations never deal
in stolen timetables.
- **Buy** a route tip on an off-the-books ship and it appears on your own
traffic board, tagged with a **stale in Nd** badge — farther from Earth, the
tip is cheaper (secrets are common currency out where nobody's watching).
A bought tip is good for 30 sim-days.
- **Sell** your own tracking-post finds once they're 50%+ quality — selling
never erases the track, so a good lock is repeatable income, not a one-shot.
- **Tight-beam** hails a tracked contact directly (short range, no
broadcast) — an honest ship tells you its destination, a secretive one stonewalls.
- **Laser range** trades a perfect, instant fix on a tracked target for
lighting yourself up — the target (and anyone watching) now knows roughly
where the shot came from. Passive sweeping never gives you away; these two
tools are the deliberate exceptions.

## 10. The war room ⚔

- Press **Guns ⚔** for the tactical circle: your ship at the center, a weapon-
range ring (2×10⁸ m — shorter than the boarding shuttle's capture envelope),
and a catch-radius ring around any hunter on your tail.
- **Warn** a target inside weapon range. Compliant ships (about 3 in 4) heave
to and board at half the usual time; stubborn ones (about 1 in 4, rising
slightly with your heat) call their own muscle instead — which ship is which
never changes, so warning the same one twice always plays out the same way.
- **Hail** for a canned in-character reply, **Bribe** for guaranteed compliance
with zero heat generated (priced under what an honest robbery would pay) — an
inside job, nobody calls the cavalry.
- Actually robbing a ship (not just warning it) raises **heat**, a 0–3 flame
gauge that decays one level per 20 days — four times faster while you're
hidden in orbit at a haven. High enough heat and a **hunter** spawns: hired
muscle that fits out for 5 days, then hunts you down at a slow, relentless
thrust. Get caught (within 3×10⁸ m, under 3,000 m/s relative) and it seizes
your hold plus a 500 cr fine; stay hidden at a haven for 2 days straight and
it gives up the chase.
- Havens are the release valve for the whole loop: cool your heat, trade cargo
and (if it's also a far trading post) intel, and repair — no questions asked.



## 11. The scope

- **Scope** opens the instrument screen: auto-locks the nearest interesting
contact, draws it (freighters, pods, players, planets, the sun, plasma wisps), and
reads out distance and relative speed.
- **◀ / ▶** cycle targets manually; the middle button returns to
**AUTO**.



## 12. Inside the ship

- **Deck** — top-down plan of your pirate sail. Walk with
`WASD`/arrows, interact with `E`, drag the map if the bow hides
behind a panel. Crew: droids K-77 and R-3B stand by the shuttle; V-1K patrols.
- `F` — **first person**. Walk the corridors; the windows show the
real sky — the sun blazes bigger the closer you sail, and the planets are where the
ephemeris says they are. `Q` returns to the helm.
- Consoles: **HELM / NAV POST** (plotting), **SCOPE**,
**CANTINA** (rum — mind the third tot), **CARGO HOLD** (your loot as crates),
**VENT PANEL**, **SHUTTLE BAY**.
- **The boarding run**: with a capture window engaged, `E` at the
shuttle bay puts you on the stick. Cross the gap with `WASD` thrust, dock at
the airlock *below the speed limit* (come in hot and you bounce), and the droids
swarm aboard — instant boarding. `Q` aborts; losing the window auto-returns
the shuttle. The prey's drift is your real relative velocity — a sloppy pass by the
mothership makes a hard run for the pilot.



## 13. The electric sky ⚡

- Near the sun the plasma halo charges your hull; the flowing ribbons between planets
are **plasma streams** — riding one pushes you along it (charged hulls
feel the current).
- Charge makes you glow on everyone's sensors — it pierces even sun glare — and at full
charge you start arcing. `V` vents.
- Mercury's neighborhood runs ~75% ambient charge. Ambush country: everyone there is
visible, desperate, or both.



## 14. The physics, honestly

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
