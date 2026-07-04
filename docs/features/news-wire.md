# The news wire

What this is: one deterministic feed of "world events" that backs both the **Comms** desk's
ticker (the freshest few lines) and the **Galley** desk's full scrollback (the long feed) — one
source of truth, two views. See [dark-web.md](dark-web.md) for the ticker's place in the comms
room and [station-desks.md](station-desks.md) for the Galley desk generally.

![Galley news wire](../tmp_pics/saturday/galley.png)

## Two kinds of item

- **Ambient flavor** — rotating gossip, one headline per sim-day, generated purely from the
  current scenario's own bodies, cargo classes, and the sim calendar (`NewsWire.Ambient` in
  `SpaceSails.Core`). The same scenario and the same sim-day always produce the same line: revisit
  yesterday's news by scrubbing the plot and it won't have changed underneath you.
- **Event items** — a small, player-triggered set the UI pushes explicitly as they happen:
  - **Robbery committed** — boarding and cleaning out a ship (the same hook that raises heat).
  - **Hunter dispatched** — a bounty hunter fitting out at a policed body after a robbery.
  - **Intel purchased** — buying a route tip on the dark web.
  - **Orbit entered at a haven** — binding into orbit around any body flagged as a pirate haven.

Both kinds render as the same `NewsWire.NewsItem` (a sim-time + a headline string), so the two
feeds can be merged and sorted newest-first without the UI caring which kind produced which line.

## Where the state lives

`NewsWire` itself (in `SpaceSails.Core`) is pure and stateless — no `DateTime.Now`, no
`System.Random`, per the determinism rule (§9) every other Core module follows. The actual ledger
of pushed events is a small, bounded list (`Map.razor`'s `_newsEvents`, capped at 50, newest
first) — the same "Core stays pure, the mutable ledger lives with the caller" split the
[tracking post](tracking-post.md)'s own ledger uses. `Map.razor`'s `NewsFeed(ambientCount)` helper
blends that ledger with `NewsWire.Ambient` and hands the result to whichever desk is asking:

- The **Comms ticker** takes the freshest 5 lines, blended with 6 days of ambient flavor.
- The **Galley** feed takes up to 25 lines, blended with 20 days of ambient flavor, and labels each
  older line "today" / "yesterday" / "*N*d ago".

## Hooking in a new event

Pushing a new kind of event onto the wire is a one-line call at the point where the gameplay hook
already lives — `Map.razor`'s `PushNewsEvent(kind, subject, detail)` — plus a template case in
`NewsWire.Headline`. No other file needs to know the wire exists; components (`DarkWeb.razor`) that
trigger an event just raise a plain `EventCallback` (e.g. `OnIntelPurchased`) the way they already
do for laser ranging, and `Map.razor` turns that into a wire entry.

See also: [tracking-post.md](tracking-post.md) and [dark-web.md](dark-web.md) for two of the
gameplay hooks that feed the wire, [war-room.md](war-room.md) for the heat/hunter loop that feeds
the other two.
