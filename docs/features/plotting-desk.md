# Plotting desk

What this is: the maneuver planner — pause the sky, drag time back and forth, and drop burn
nodes until your ribbon goes where you want it.

Where: press the **Plot** toolbar button, or `P`, on the map. Press it again (or `Play`) to
resume live flight. You can also reach it via the **NAV POST** console on the [deck](deck-view.md)
by walking up and pressing `E`.

## Scrubbing

- The sim pauses the instant you enter Plot mode.
- The **scrub slider** moves a point in the future; every planet shows a *ghost* at that scrubbed
  time, tethered to its live current position by a faint line — this is how you line up a launch
  window against a moving target.
- **Path length** controls how far ahead your ribbon (projected trajectory) extends — 5 days to 2
  years, log scale, so it's precise at both ends. `auto` follows your last burn plus 90 days.
  Whole interplanetary sails (Earth→Saturn) fit in a single sit-down; the horizon was sized for
  exactly that trip.

## Burn nodes

- **Add burn at scrub** drops a maneuver node at the current scrub time.
- Each node has: a direction toggle (**+** accelerate / **−** decelerate), a **pulse count**
  (1–20), and a free **percent field** — any decimal from 0.01% to 50% per pulse. A 10% pulse is
  a hammer (~3 km/s at interplanetary speed); a 0.5% node is a scalpel for fine matching.
- Click a node's marker on the ribbon to select it — its row highlights and the scrub jumps to
  that moment.
- **@** re-times a node to the current scrub position; **×** deletes it.
- "Planned: N / M" shows how many pulses your plan spends against how many you're carrying.

## Closest-pass warning

- The plot card names your single tightest flyby along the whole planned path, in body radii, with
  a marker on the ribbon.
- Under 5 body radii it turns **yellow**; if the path actually intersects the body it turns **red**
  and reads *"IMPACT, captain!"*. This is computed ~300ms after you stop editing (a full scan is
  too heavy to run on every slider tick), so give it a beat after a drag before reading it.

## Planned (armed) insertion

When the closest pass is a planet (not the sun) and close enough to matter, a button appears:
**"Insert at *body* pass — *distance* (≈N p)"**. Click it to **arm** the insertion — the button
turns green and reads **"Insertion ARMED — will orbit *body* (≈N p)"**. Leave Plot mode and let
time run: the moment your live flight enters that body's orbit-assist window (see
[orbit-assist.md](orbit-assist.md)), the game fires the burn for you automatically, spends the
estimated pulses, and disarms. If you don't have enough mass pulses left when the window opens,
the attempt is cancelled with a warning instead of stranding you mid-burn. Click the armed button
again to disarm by hand.

## Sling past a planet

When the closest pass is a planet, a **"⤴ Sling past *body*"** button appears next to it. This is
the gravity assist without the burn-tuning: instead of nudging pulses by hand until the ribbon bends
the way you want, you tell the desk the pass you want and it solves the aiming burn for you.

- **Side toggle — Lead (boost) / Trail (brake).** Which side of the planet you thread decides whether
  the flyby *donates* heliocentric speed or *sheds* it. Lead rides the planet's orbital motion for a
  boost; Trail leans against it to brake. (On a slow, near-tangent arrival, only one side is reachable
  for the fuel you're carrying — the toggle then serves whichever side the pass actually lands on, and
  the verdict's speed number tells you which you got.)
- **Pass-distance slider**, in **planet radii**, with a floor at **2 R**. That floor is not shyness:
  below a couple of radii the point-mass gravity model *and* the projector's step size are both lying
  to you (a real ship would be skimming atmosphere or hitting rock), so the desk refuses to pretend.
- **Burn node.** By default the aiming burn is a new node at your scrub time (if the scrub sits
  before the pass), otherwise ten minutes from now. The panel names which it used; the solver is free
  to place its own.

**SOLVE** runs the real integrator — the same physics the labs measured Voyager against, no patched
conics — and prints the verdict: the pass it achieved (in R), the aiming burn (Δv and pulse cost as a
Vector burn), the crank (speed gained/shed and where your new apoapsis reaches — or *escapes the
sun*), and the **lever warning**.

> **The lever.** A flyby is an amplifier. The panel reads *"±1 pulse of aim ⇒ ±X Gm at the far end —
> re-trim after the pass."* That is the honest catch: an aim error a hair wide at the burn becomes
> tens of millions of kilometres of miss by the time you're past the planet. Fly the sling, then plan
> to trim once you're through it — do not treat the far end as pinned.

If no pass that cheap can bend you where you asked, SOLVE says so plainly (with the range the flyby
*can* reach for your budget) rather than handing you a burn that misses. Happy with the verdict?
**Add the burn** drops the Vector node into your plan and the ribbon bends through the pass. The
numbers shown are re-flown at the *quantized* burn (whole pulses), so what you read is what you'll
fly.

## Skim the cloud tops

When your closest pass is a world with an **atmosphere** — a gas giant, Venus, Titan, Earth — a
second button appears beside the sling: **"🔥 Skim *body*"**. This is braking *without* burning a
drop of fuel: dip into the top of the air, let it shed your speed, and climb back out with a tighter
orbit. It's the *Stargate Universe* gas-giant dive — and, come in fast and shallow, the atmosphere
throws you back out like an Apollo capsule skipping off the top of the sky.

- **Depth slider**, in **kilometres of periapsis altitude** inside the shell. Shallow barely touches
  the air; deep bites hard.
- **SOLVE** aims the pass to that depth and then *flies it* through the real drag integrator — the
  same Core physics [lab 22](../../labs/22-the-air-brake/README.md) puts every number to. What the
  gauge shows is what actually flies.

**The corridor gauge** is the whole point — three zones, and SOLVE tells you which one you're in:

> **▲ skip / too shallow** — above the corridor the air barely bites; a *fast* (hyperbolic) arrival
> just **bounces back out**, and the gauge names the speed she leaves at. This is the Apollo skip: shed
> a little, keep going.
>
> **● the corridor** — the useful band: real braking (Δv shed, shown in m/s and as *≈pulses saved*)
> with the g-load safely under the damage line. Free speed off the tank.
>
> **▼ too deep — would hole the sail** — dip past the damage line and the drag load holes the sail,
> the same wound the gun inflicts, now self-inflicted. Shown in **red**. You *can* still fly it (a
> captain may choose the red) — the gauge warned you honestly.

The gauge also reads the **min altitude** she actually reached and whether the pass **captured** her
into orbit or let her **exit**. The corridor is narrow on purpose — one scale height deeper multiplies
the drag, so aiming it is the skill (that razor edge is exactly what made the real return corridors
so hard to hit). **Add the burn** drops the aiming node — a fine trim, close to the pass — and the
ribbon dips through the shell.

> **Fine print.** The gauge shows a **single pass** at fine-step accuracy. A fuel-out capture is flown
> *pass by pass*, and each dip creeps a little deeper than the last (no fuel to raise the periapsis
> back up) — so free braking is a race you eventually lose to the damage line. Plan the next dip after
> you've flown this one; multi-pass planning isn't on the desk (yet).

And the live half of it: if you fly a dip that's **too deep**, the sail really does hole — the drive
goes dead while the crew sews the rigging (a couple of sim-days), then answers again. The gauge is
there so you dive on purpose, not by accident.

## Worked examples

- **Mercury**: one node, decelerate ×3 (10%) at ~day 3 → perihelion kisses Mercury's orbit around
  day 334. At closest approach, brake twice more and trim until ship speed equals *circular here*
  — then cut the gas.
- **Saturn**: one node, accelerate ×12 at the right departure day (phasing matters more than pulse
  count) → Saturn's port zone in ~9 months. Scrub and watch the ghosts to find the day your
  ghost-ship and ghost-Saturn meet.

See also: [map-and-warp.md](map-and-warp.md) for pulses and warp, [orbit-assist.md](orbit-assist.md)
for what happens once you're close, [traffic-board.md](traffic-board.md) for plotting an
intercept against a moving target instead of a planet.
