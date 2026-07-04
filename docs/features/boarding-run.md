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

See also: [traffic-board.md](traffic-board.md) for selecting and pinning a target,
[plotting-desk.md](plotting-desk.md) for flying the intercept, [depots.md](depots.md) for the
easiest possible boarding target, [dock-and-economy.md](dock-and-economy.md) for what to do with
the loot.
