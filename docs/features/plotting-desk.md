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
- Each node has: **four quick selects** for its direction, a **pulse count** (1–20), and a free
  **percent field** — any decimal from 0.01% to 50% per pulse. A 10% pulse is a hammer (~3 km/s at
  interplanetary speed); a 0.5% node is a scalpel for fine matching.
- Click a node's marker on the ribbon to select it — its row highlights and the scrub jumps to
  that moment.
- **@** re-times a node to the current scrub position; **×** deletes it.
- "Planned: N / M" shows how many pulses your plan spends against how many you're carrying.

### The four directions (#838)

The planner aims a burn in the **vector view only**, and it does it in four words — with respect to
the trajectory **at that node**, not to wherever the ship's nose happens to point right now:

| Button | What it means |
| --- | --- |
| **▶ FORWARD** | with the trajectory at this node — push the orbit along (prograde) |
| **◀ BACK** | against it — hold the orbit back (retrograde) |
| **▲ UP** | away from the body — lift the orbit (radial out) |
| **▼ DOWN** | toward the body — drop the orbit (radial in) |

The body **up** and **down** are measured from is named under the buttons: the frame you have
selected if you have one, otherwise whichever body's Hill sphere holds the ghost at that moment,
otherwise the Sun. The four are always exact quarter turns apart, solved from the ghost's own
position and velocity at the node's epoch — **re-time the node and press again and you get a new
answer**, because by then the course has moved on. The pressed direction lights up, and goes dark
by itself the moment the node is re-timed out from under it.

**Free aim** stays for the exception: type any angle into the degrees field (course-relative by
default, `abs`/`rel` toggles it to the world heading) and the burn points there.

And for the captain flying with the mouse alone (owner, 2026-08-17: *"the vector rotation is good for
flying with mouse alone, without inputting … like ±5 degrees"*), **−5°** and **+5°** turn the aim five
degrees off wherever it points now — press FORWARD, then +5° twice, and the burn sits ten degrees off
the ghost's prograde without a key being touched. The step lives once, in Core
(`NodeFrame.NudgeDegrees`), and it is an ANGLE, which is why the buttons wear a degree sign.

The **+ / −** *factor* idiom is not gone from the game — it belongs to reflex flying: the live `+`,
`−`, `↑`, `↓` keys that scale the ship's velocity right now. Planning and reflex are two different
questions, and each keeps the control that answers it.

### The frame you READ the plan in (#926)

Owner, playing the vector planner (2026-08-17): *"the real thrust amounts are dependent on the
coordinate origin. I had to remember to switch to Sun to get the ship to really start moving from
Earth towards Mars."* In Earth's frame a Mars transfer looks like almost nothing; the interesting
motion is the Sun's, and the plot frame (#135/#143/#206) decides which one you are looking at.

So the step editor **names the frame it reads the plan in** — *reading in EARTH's frame* — always; and
when the plan has a destination whose trip is in a different frame, it says so in one line and offers
that frame in one press:

> You are reading this plan in EARTH's frame — the trip to MARS is in the SUN's.
> **[ Read it in the Sun's frame ]**

**The trip's frame is the common parent of both ends** (`TripFrame.Of` in Core): the deepest body that
is an ancestor-or-self of the ghost's own primary at the node AND of the destination. Earth→Mars: the
Sun. Earth→Luna: Earth. Europa→Ganymede: Jupiter. When that body is the root of the hierarchy the
answer is the null frame — the Sun / inertial one.

It **offers, and never switches by itself** (the owner's option A; auto-switching was rejected). The
press moves only what you JUDGE by — the ribbon and the numbers. The four quick selects go on aiming
in the NODE's frame whatever the map is drawn about, because an escape burn is Earth-prograde either
way, so a burn's heading is byte-identical before and after the press.

### Which way she is going — the arrowhead at the ship (#933)

Owner, playing the flight side (2026-08-17): *"our ship shape on the map could indicate little better
about where it is going … more like add shape that points to the direction the ship is going even when
its motion is stopped during the burn parameter selection."*

The ship marker carries a small triangle in her own ink: **the arrowhead is where she is going in the
frame you are reading; the nose at a node is where the burn pushes**. Two shapes, two questions, both
on screen while you plan — and the arrowhead never swings to the burn, because she is not flying that
course until she has burned it.

- It is drawn **every frame, the paused ones included**. Velocity is state, not motion: the moment you
  most need to know which way she carries is the moment the sky is frozen for planning.
- It is aligned to her velocity **in the plot frame** — the same `v helio` / `v rel {body}` the panel's
  speed readout is built from, one function read twice. Switch the frame and the arrow swings. That is
  the frame trap of the section above, drawn instead of written down.
- **Below one metre a second in that frame it is a ring, not an arrow.** Parked at a dock, or co-moving
  with the body whose frame you are reading in, she is not going anywhere *here* — and the direction of
  what is left is ephemeris noise, so a dart would spin like a compass on a magnet.
- Fixed **pixel** size at every zoom (`VelocityArrow` in Core: a 30° apex, 13 px of lead, base 5 px
  behind the dot). It is a glyph, like the ship dot itself.

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
