# Electric sky ⚡

What this is: the Electric Universe mechanic — a charged plasma environment that makes your hull
glow, and eventually arc, unless you vent it.

Where: only active in `EU ⚡` scenarios (**Sol (Electric)**, **The Wheel** — see
[scenarios.md](scenarios.md)); look for the `EU ⚡` badge next to the scenario name in the map
HUD. Vent with `V`, or walk to the **VENT PANEL** console on the [deck](deck-view.md) and press
`E`.

## Charge

- Your hull picks up **charge** (0–100%, shown as a HUD bar) from two sources: a **solar halo**
  around the sun (falls off with distance — roughly 75% ambient at Mercury, down to about 11% at
  Earth), and **plasma streams**, flowing ribbons between planets that saturate you to 100% the
  moment you're inside one.
- At **90% charge**, your hull starts **arcing** — the HUD flags "ARCING RISK", and on the map a
  **plume crawls off her masthead**: a short whip standing off her beam with a few filaments raking
  slowly out of its tip, so the band is readable without opening the charge board (#523, #528 §7).
  It is deliberately *not* a ring around the hull. Field strength is potential over radius of
  curvature, so her antenna whip runs some 20,000× the field of her hull skin; a real discharge
  leaves the sharpest extremity, and a sphere is the one shape it can never be. The crawl is seeded
  from **sim** time, so a paused map draws a still plume.
- Dumping the charge (`V`, or the board's own switch) **snaps**: brighter, longer filaments off the
  same masthead for about two-thirds of a second, and the brightness is scaled by the charge that
  actually left her — the sensors' own `SeenFartherFactor` excess, so the picture and the board can
  never come to disagree. The honest event is 2.2 ms; 600 ms is the smallest stylisation you can
  see happen.
- An arcing hull is heard system-wide: a charged hull is a broadband radio source and pierces even
  sun-glare sensor blind spots (see [orbit-assist.md](orbit-assist.md) and
  [traffic-board.md](traffic-board.md) — this is the same sensor model everyone else uses to spot
  you).

## Riding a stream

A charged hull *feels* the plasma current: inside a stream, a charged ship gets pushed along the
stream's direction, for free momentum — riding the Saturn–Jupiter river is genuinely faster than
a ballistic transfer, at the cost of being lit up on every sensor along the way. It's a real
speed/stealth trade, not a cosmetic flourish.

## Venting

Press `V` (or the VENT PANEL console) to vent — this **halves** your current charge instantly.
Venting has its own short cooldown, separate from the thrust-pulse cooldown, so you can't spam it
away entirely for free; you'll typically vent more than once to come all the way down from a hot
run through a stream.

## Tips

- Mercury's neighborhood runs hot enough (~75% ambient) that everyone nearby is either lit up,
  desperate, or both — ambush country.
- Watch the charge bar's color: blue is safe, amber from 60%, red from the 90% arcing threshold.

See also: [scenarios.md](scenarios.md) for which voyages have an electric sky at all,
[deck-view.md](deck-view.md) for the VENT PANEL console.
