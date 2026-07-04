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
