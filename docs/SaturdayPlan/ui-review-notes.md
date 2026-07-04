# UI review notes — PR-2 pass

Written while documenting each station for the feature guides (`docs/features/`). These are
concrete, code-grounded observations about stations whose UI looked dense, undiscoverable, or
easy to misread — filed here per the vision ("if the UI seems complicated and unplaytested, make
it better") for later lanes to act on. No code was changed to produce this list.

## Plotting desk (`docs/features/plotting-desk.md`)

- **The per-node row is very dense.** One row packs: time label, a two-button direction toggle,
  a pulse-count number input, a percent number input with its own `%` suffix, a re-time button,
  and a delete button — six interactive elements in a single `d-flex` row at `small` text size.
  The free percent field (0.01%–50%, arbitrary decimals) is a tiny `<input type=number>` spinner,
  which is a fiddly way to dial in something as consequential as "0.5% vs 5%". A slider (even a
  secondary log-scale one, matching the horizon slider's pattern already in this same panel)
  would likely be faster to use than nudging a number spinner or typing digits.
- **Horizon controls are two sliders stacked with different semantics** (scrub position vs. path
  length) plus an `auto`/manual toggle button, all in the same visual weight. First-time players
  may not realize the second slider only matters when `auto` is off, since flipping between them
  isn't visually distinguished beyond the button's color.
- **Insertion-arming button text is a full sentence crammed onto one button**: `🛰 Insert at
  <body> pass — <distance> (≈<cost> p)`. It's accurate, but a lot to parse at a glance — consider
  splitting the estimate onto a tooltip or a secondary line, keeping the button label itself to
  "Arm insertion at *body*".

## Orbit assist (`docs/features/orbit-assist.md`)

- The panel switches which body it's showing (armed target vs. nearest body) based on internal
  state the player can't see directly — reasonable logic, but the only visible cue is the body
  name changing. A first-time player who arms an insertion at Mars while still near Earth may not
  immediately notice the panel silently switched away from Earth.
- No keyboard shortcut is shown anywhere in the HUD for `O` itself — it's documented in the guide
  but the in-HUD button doesn't mention the key, unlike the Plot/Dock/Scope toolbar buttons which
  at least imply their own click target. Minor, but inconsistent with how consistently keys are
  surfaced elsewhere (Plot=`P`, Vent=`V`).

## Scope (`docs/features/scope.md`)

- Target cycling (`◀ AUTO ▶`) is mouse-only. Every other frequently-used station in the game
  (pulses, plot mode, vent, orbit, deck movement) has a keyboard binding, but the scope — arguably
  the station you'd want to flip through quickly during an intercept — requires reaching for the
  mouse. A bound key pair (e.g. `[` / `]`) would match the rest of the game's input model.

## Deck view & cantina (`docs/features/deck-view.md`)

- **Console signage is inconsistent**: the room is labeled "CARGO HOLD" but the console itself is
  labeled "CARGO" — a small thing, but it's exactly the kind of mismatch a player notices and
  wonders if it's a bug. Worth a one-line fix in a later pass.
- **The third-tot wobble has zero warning.** The first two tots give an unremarkable flavor line;
  the third (within 90 seconds) suddenly imposes a 25-second movement penalty with no escalating
  cue beforehand. Since it's a fun easter egg rather than a real hazard, that's probably fine as
  designed — but if the owner wants it playtested as a "mechanic" rather than a gag, a subtler
  warning on the second tot ("one more and you'll regret it") would let players opt in rather than
  be surprised.
- There's no in-scene legend of all six consoles/keybinds — they're discoverable only by walking
  up to each one. A first-time player dropped into Deck view with no prior guide-reading has no
  on-screen hint of what's interactive. A one-time overlay (or leaning on the existing "First
  hunt" tutorial checklist to name consoles) would help.

## Boarding run (`docs/features/boarding-run.md`)

*(Correction: verified false — `ShuttleFlightView.cs` already renders a live `range {gap:F0}  ∙
closing {speed:F0} (dock ≤ {DockSpeedLimit})` readout during the flight (line 178), e.g. "range
1234  ∙  closing 42 (dock ≤ 55)", with `DockSpeedLimit = 55` px/s and the closing-speed text
color-coded (glow color under the limit, amber over it). No action needed.)*

## Electric sky (`docs/features/electric-sky.md`)

- Venting has a cooldown, but the only feedback when it's still cooling is the text message "Vent
  recharging…" with no indication of how much longer to wait. Since charge management is a real
  decision point near arcing (90%), a small cooldown timer (even just a thin progress bar like the
  boarding-progress one already used elsewhere in the HUD) would remove the guesswork.

## General

- Several panels (plot card, dock card, tutorial card) are all `position`-stacked as separate
  floating cards over the canvas at `small` Bootstrap text size. On a small viewport these could
  plausibly overlap (untested at narrow widths in this pass) — worth a quick manual check on a
  laptop-sized window, since nothing in the code appears to actively prevent overlap between,
  say, the Plot panel and the Traffic panel if both are open at once.
