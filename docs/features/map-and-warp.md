# Map & warp

What this is: the main view — the solar system, your ship, and the time controls that make a
sailing game playable when transfers take months.

Where: this is the default screen at `/map` (or launch a scenario from the home page).

## The view

- **Drag** the canvas to pan, **mouse wheel** to zoom.
- **Follow Ship** (toolbar) re-centers the camera on your ship and keeps it centered as you fly.
- Bodies are drawn as circles on their orbit rings; the ring itself is the body's path around its
  parent (planets around the sun, moons around their planet).
- Labels only appear once a body is big enough on screen to matter — no label soup when zoomed out.

## Warp

- The **warp slider** (top left of the toolbar) is logarithmic, 1× to 10,000×, so it can hit both
  "watch a burn happen" and "skip to next month" with one control.
- **Pause** freezes sim time entirely; the readout shows `∥` instead of a warp multiplier.
- Warp auto-drops as you near a planet or an encounter, so a 10,000× cruise doesn't blow through
  the one moment you wanted to see. The readout shows `(auto-drop from N×)` when this is active.
- At warp ≥ 100×, the sim advances in fixed 60-second quanta (matching the NPC integrator) instead
  of the frame-by-frame 1-second loop — this keeps high warp frame-rate-independent and stops
  interpreted WASM from choking on tens of thousands of per-frame steps.

## HUD readouts

The dark panel under the toolbar always shows:

- Scenario name (and an `EU ⚡` badge if it's an Electric Universe scenario — see
  [electric-sky.md](electric-sky.md)).
- Sim time and current warp.
- Ship speed, with **circular here** alongside it — the speed that would hold a circular orbit
  around the sun at your current distance. Matching it means you coast forever; it's the
  difference between matching a planet's *radius* and matching its *orbit*.
- Mass pulses (your maneuvering fuel — see [plotting-desk.md](plotting-desk.md)), credits, and
  cargo (with sale value once you're carrying something worth selling).
- Nearest body, with live distance and relative speed.
- An orbit-assist strip when a planet is close enough to matter — see
  [orbit-assist.md](orbit-assist.md).

## Toolbar buttons

`Pause` · `Follow Ship` · `Plot` (opens the [plotting desk](plotting-desk.md)) · `Traffic` (opens
the [traffic board](traffic-board.md)) · `Dock` (enabled only inside a market's port zone — see
[dock-and-economy.md](dock-and-economy.md)) · `First hunt` (the built-in tutorial checklist) ·
`Scope` (opens the [scope](scope.md)) · `?` (opens the Captain's Guide in a new tab) · `Deck`
(walks you inside the ship — see [deck-view.md](deck-view.md)).

## Flying by hand

- `+`/`−` or `↑`/`↓` — a thrust pulse, ±10% of your current velocity. Pulses only ever scale your
  speed; they never rotate your heading (see [plotting-desk.md](plotting-desk.md) for why that
  matters).
- `Shift` + pulse — fine trim, ±1%, for station-keeping and orbit matching.
- `O` — enter orbit around the nearest body, when the orbit-assist window is open (see
  [orbit-assist.md](orbit-assist.md)).
- Pulses have a short cooldown and each spends one mass pulse; run out and you're **Adrift**
  (an alert bar appears with a "Request rescue" button).

Tip: to go inward (Venus, Mercury) *brake* — losing speed drops your perihelion. To go outward
(Mars, Jupiter, Saturn) accelerate. You're always trading speed for altitude on an ellipse.
