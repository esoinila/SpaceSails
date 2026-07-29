# Boarding run

What this is: how cargo actually gets from a target's hold into yours — an automatic shuttle
timer, or a minigame if you'd rather fly it yourself.

Where: happens automatically once you're in a capture window with a selected [traffic
board](traffic-board.md) target; fly it yourself by walking to the **SHUTTLE BAY** on the
[deck](deck-view.md) and pressing `E` while the window is open.

## The capture window

The mothership never docks — she opens a window for small boarding craft. The window is open
whenever you're within **500,000 km** and under **5 km/s** relative speed of the target
simultaneously. Both conditions gate together; drop either one and the window closes.

## Automatic boarding time

While the window is open, boarding progress accumulates on its own (shown as a progress bar in
the HUD: *"Shuttles away — boarding \<callsign\> (N%)"*). How long it takes depends on how sloppy
your pass is:

- A tight, near-matched pass boards in about **30 seconds** (the best case).
- Relative speed and distance each independently make it slower — the required time roughly
  doubles for every extra 1,500 m/s of relative speed, and doubles again for every extra
  200,000 km of distance. At the sloppy corner of the envelope (5 km/s, 500,000 km) it's around
  7–8 minutes — deliberately longer than a straight flyby can sustain, so a genuine drive-by fails
  and only a deliberately matched, close pass actually boards.
- This runs on wall-clock time, not sim time — shuttles fly in real time, warp be damned.

Losing the window (drifting out of range or speed) stops the clock; re-entering it resumes.

## Flying it yourself

Press `E` at the **SHUTTLE BAY** while a window is engaged to take manual control:

- `WASD`/arrows fire your shuttle's RCS thrusters.
- Dock at the target's airlock **under the speed limit** and the droids swarm aboard —
  instant boarding, no waiting out the timer.
- Come in too hot and you **bounce** off the airlock (velocity reversed and roughly halved) —
  no damage, just try again while the window holds.
- `Q` aborts the run and returns the shuttle to the cradle.
- If the capture window itself closes while you're mid-run (your mothership drifted out of
  range), the shuttle auto-returns and the run ends as a loss.
- The target's apparent drift in the minigame is your ship's *real* relative velocity against
  it — a sloppy pass by the mothership makes for a genuinely harder shuttle flight, not just a
  cosmetic one.

## The shuttle as a tool, not a lift

*Design thread, 2026-07-29. Owner, on being told where the multi-site chooser actually belongs: "Love that
shuttle use widening."*

Today the shuttle is a **lift**: it takes the away team from the ship to the ground and back, and while
they are down there it is furniture. Everything the wreck lane has grown this week quietly argues for it
being a **tool they keep using while they are out there**.

The hinge is that a co-orbital hold costs nothing (Lab 40), so repositioning along a hull is free in fuel —
and **time is now the currency aboard a wreck.** Vacuum soaks, pump cycles, sentry magazines and an
overload countdown are all clocks running at once, so "fly round to the other end, that will take four
minutes" is a real price without needing a fuel line to justify it.

What that unlocks, in rough order of how cheap it is to build:

| | What the shuttle becomes |
|---|---|
| **Severed sections** | On a big hull, sections are not connected inside. You pick where to cut in, and the only road to the far half is back through the airlock and around. The multi-site chooser stops being a mood and becomes **geometry**. |
| **A gun that moves** | GATE-1 already sits in her airlock covering the corridor you retreat down. Reposition, and it covers a different door — so where you park is a tactical choice, not a parking space. |
| **The air supply** | Already true and worth naming: refill charges *are* the shuttle's reserve. She is the reason a compartment can be brought back to pressure at all. |
| **Retrieval** | A launched lifeboat ([safety-card.md](safety-card.md)), a section under tow, a node nobody should be carrying. Things that are *out there* rather than aboard. |

And the sharpest version: **flying between sections with the overload armed.** The clock does not care that you are in transit, and the shuttle is the only thing that can put you back inside it or take you away.

Prerequisite for the first row is the same **big-hull wreck class** the safety card needs — which is now
three features waiting on one piece of geometry, and probably means that geometry should be built next.

See also: [traffic-board.md](traffic-board.md) for selecting and pinning a target,
[plotting-desk.md](plotting-desk.md) for flying the intercept, [depots.md](depots.md) for the
easiest possible boarding target, [dock-and-economy.md](dock-and-economy.md) for what to do with
the loot, [atmosphere.md](atmosphere.md) for the valve board — the vacuum soak, the pump, the pressure
locks and the shuttle's own airlock — and [safety-card.md](safety-card.md) for the filed (unbuilt) plan to
read a liner's muster map and her empty pod cradles as accident evidence.
