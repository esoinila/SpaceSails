# Orbital depots

What this is: a plunderable cargo depot in orbit around every planet — "surely there is something
to steal on every planet orbit" (M22) — so piracy doesn't only exist along the Saturn He3 run.

Where: they show up as ordinary rows on the [traffic board](traffic-board.md), named `<Planet>
Depot`, e.g. "Earth Depot". Board them the same way as any other target — see
[boarding-run.md](boarding-run.md).

## What they are

- One depot per planet, riding a fixed circular orbit around it — no engine, no maneuvers, no
  hidden burns. Their position is a pure function of sim time, so they never drift and cost
  nothing to simulate.
- They orbit at roughly a quarter of the planet's Hill-sphere radius (or 8 planet radii, whichever
  is larger) — always well inside orbit-assist range of their planet.
- Each carries a fixed 4 units of cargo, flavored by the planet:

  | Planet | Cargo |
  |---|---|
  | Mercury | Compute cores |
  | Venus | Alloys |
  | Earth | Machinery |
  | Mars | Ice |
  | Jupiter, Saturn, Uranus, Neptune | He3 |

  (Moons share their parent planet's depot — there's one per planet, not per body.)

## Why they're easy

A depot never maneuvers and never runs from you, so matching its orbit is a standing-still target:
enter orbit around the same planet (see [orbit-assist.md](orbit-assist.md)) and you're already
inside the capture envelope with near-zero relative speed — boarding at that point is close to the
30-second best case. They're a reliable fallback prize when a real intercept isn't lining up.

See also: [dock-and-economy.md](dock-and-economy.md) for what depot cargo (and everything else in
your hold) is worth once you sell it, [traffic-board.md](traffic-board.md) for the board they
appear on.
