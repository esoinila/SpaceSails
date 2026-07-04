# Station desks — the duty-station refit

What this is: the UI is organized into full-screen duty-station **desks**, switched by number key
or a slim tab bar, instead of small pop-up panels stacked over the map. Each desk's topic owns the
screen; every other desk shows only as a small summary chip on a thin edge strip. This is PR-11 —
the framework and interim hosting; richer per-desk content lands in PR-12/13/14 (see
`docs/SaturdayPlan/StationDesks.md` for the full plan).

Where: always available, on top of the map at `/map`.

## The 70% rule

The owner's framing, distilled: *at each station, that station's topic should own ~70% of the
screen.* The map belongs to Nav (and dims in behind Sensors); a specialist desk shows its own
instrument richly — the Sensors desk shows one scope wall, not one small box. Nothing crams into a
pop-up card anymore. Other desks' information only ever appears as a small, standardized summary
chip — info-rich on the desk that owns it, one line elsewhere.

## The desks

| Key | Desk | What it hosts |
|-----|------|----------------|
| `1` | **Nav** | The live map, HUD readouts, and the plotting desk — today's default view, decluttered (traffic/dock/deck controls have all moved to their own desks). |
| `2` | **Sensors** | The [tracking post](tracking-post.md) full-screen, with the live map dimmed through behind it. |
| `3` | **War room** | The [war room](war-room.md) tactical circle, full-screen. |
| `4` | **Trade** | [Local space](local-space.md) plus the [dock market](dock-and-economy.md) side by side. |
| `5` | **Comms** | The [dark web](dark-web.md) plus the [traffic board](traffic-board.md) side by side. |
| `6` | **Galley** | The news wire, the rum locker, and every other desk's chip — see below. |
| `7` | **Deck** | Walk the ship ([deck view](deck-view.md)) — unchanged, now reached by desk key instead of a toolbar button. |

## Switching

- **Number keys 1-7** switch desks instantly from anywhere except while typing into a slider or
  number field (those already stop the keydown from bubbling, same pattern the plotting desk's
  scrub/pulse/percent inputs use — a digit typed into "pulses" edits the number, it doesn't jump
  you to the Trade desk).
- **The station tab bar** (top center) does the same thing by click — useful as a discoverability
  aid and for anyone who'd rather not memorize the keys.
- **`Escape`** always returns to Nav from anywhere.
- **Deck (`7`)** rides the existing walk-the-ship mode: pressing `7` enters it (or toggles it if
  you're already there), and pressing `1`-`6` from inside it leaves deck mode and switches in one
  step — you never end up in a state where the desk tracker and the deck-walk flag disagree.

## The summary chips

A thin vertical strip on the right edge shows one small chip per *other* desk — never the one
you're currently on, and never a full panel. Present on every desk, including Nav and Deck. Click
a chip to jump straight to that desk.

Each chip is the desk's tightest **objective** summary, not a stats dump:

- **Nav** — the destination implied by your current course: `→ <body> orbit` if an insertion is
  armed, `on plotted course` if a maneuver plan exists, `free sailing` otherwise; speed and warp on
  the second line, plus a closest-pass warning if one's live.
- **Sensors** — the best-quality tracked target ("Tracking *callsign*"), an active sweep
  ("Sweeping *program*"), or "no watch set"; tracks N/M on the second line.
- **War room** — the threat posture: `heat 🔥N · hunter <distance>`, or "quiet skies".
- **Trade** — the active deal ("drones → *name* NN%") or credits/cargo if idle.
- **Comms** — the freshest bought intel by callsign, or "no whispers".
- **Galley** — tots poured and whether the wobble is active.

A reserved slot at the top of the strip is for the captain's mission chip (PR-15) — it renders
nothing until that desk exists.

## Interim desk hosting (PR-11)

Sensors, War room, Trade, and Comms each host an *existing* station component with a new
`FullScreen` parameter: when true, the component fills its desk's content area and its layout
breathes (bigger SVGs/canvases, multi-column tables) instead of squeezing into a small floating
card. The floating (non-`FullScreen`) mode still exists in the components but is unused after this
PR — every station is reached through its desk now. PR-12/13/14 make each desk's own content
genuinely rich; this PR only builds the frame they sit in.

## The Galley (v1)

The Galley (`6`) is the one desk built fresh for this PR, and it doubles as a proof that the chip
strip actually shows something everywhere — "the galley IS the summary place" per the design
notes. It has:

- **The rum locker** — a "Pour a tot" button wired to the *exact same* rum ledger the deck
  cantina console uses (see [deck-view.md](deck-view.md#the-cantina--mind-the-third-tot)); tot
  count and the third-tot wobble stay in sync between the two entry points.
- **A news wire stub** — a deterministic headline per sim-day (pure function of sim time, no
  randomness), plus the last four days' headlines underneath. Flavor only: the full news/rumor
  economy (feeding this wire and the Comms desk together) arrives in PR-14.

## A deliberate deviation from pre-PR-11 behavior

Local Space used to auto-open the moment you bound into orbit around a body (a small floating
card popping up cost nothing). Now that Trade is a full-screen desk, forcing that same switch
would yank the player's whole view away from Nav mid-flight — disruptive rather than helpful. The
Trade chip updates live instead, so the player still notices a new contact; switching to look at it
stays a deliberate action (number key, tab, or chip click).

See also: [map-and-warp.md](map-and-warp.md) for what's left on the Nav toolbar,
[tracking-post.md](tracking-post.md), [war-room.md](war-room.md), [local-space.md](local-space.md),
[dark-web.md](dark-web.md), and [traffic-board.md](traffic-board.md) for each hosted station's own
detail, [dock-and-economy.md](dock-and-economy.md) for the dock panel now living on Trade.
