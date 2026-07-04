# Traffic board

What this is: the shipping departures list — every cargo run and mass-driver pod you can see,
with a predicted-path cone you can plot an intercept against.

Where: the **Comms desk** — press `5` or click **5 Comms** in the station tab bar (see
[station-desks.md](station-desks.md)), alongside the [dark web](dark-web.md). In multiplayer this
becomes a live **Contacts** list of other players' sensor-visible ships instead (see below).

## The board

Columns: **Callsign**, **Cargo**, **Route** (origin→destination), **Departs**, **Last seen**, and
**Status** (`Scheduled`, `En route`, `Tracked`, `Lost`, `Boarded`, or `Arrived`). `Tracked` means
you've observed it within the last 2 simulated hours; let that lapse and it flips to `Lost` until
you re-observe it.

Click a row to **select** that target — this is the prerequisite for everything else on this
page, including boarding.

## Pinning a prediction cone

Once a target is selected, **Pin** appears in the card footer: *"Pin: brakes at *destination*"*.
Pinning draws the ship's predicted-path cone on the map — the envelope of where it can plausibly
be, given its hidden maneuver budget. The cone tightens the longer you keep the target under
observation and opens again if you lose contact. Cargo pods launched by Luna's mass drivers have
no engine at all, so their cone never opens — they're the easiest intercepts in the game (and the
built-in tutorial's first target).

## Plotting the intercept

Open the [plotting desk](plotting-desk.md) and add burns until your ribbon crosses the pinned cone
inside the **capture envelope**: within 500,000 km and under 5 km/s relative speed of the target.
Hold that window and boarding starts automatically — see [boarding-run.md](boarding-run.md) for
what happens next, including flying the shuttle yourself.

## Off-the-books ships

Some NPC ships — notably the He3 haulers running Titan→Mars and Titan→Earth in the outer reaches
— don't publish a timetable (`PublishesTimetable == false`). They're real and simulated, and
sensors can still see them, but they're filtered out of this table by default: no route, no
departure, no last-seen row. A footer line reading **"N ships operating off the books"** appears
whenever any exist, so the board never silently under-counts traffic.

The [tracking post](tracking-post.md) can still find them by passive sweep, and once found, the
[dark space web](dark-web.md) can buy their route intel — a purchased tip puts that ship back onto
this table, tagged with a **🕸 stale in Nd** badge counting down until the tip expires.

## Multiplayer contacts

With `?mp=1`, the Traffic button instead shows **Contacts** — other players' ships your own
sensors can currently see (kind and cargo class only; no route/departure data, since it's live
optical observation, not a public timetable). Boarding is single-player only this milestone.

See also: [plotting-desk.md](plotting-desk.md), [boarding-run.md](boarding-run.md),
[scope.md](scope.md) for a close-up instrument view of a locked target,
[depots.md](depots.md) for orbital cargo that never has to be "intercepted" at all,
[tracking-post.md](tracking-post.md) for finding off-the-books ships,
[dark-web.md](dark-web.md) for buying their route intel, and
[scenarios.md](scenarios.md) for where stations and havens (the new body kinds feeding this board)
come from.
