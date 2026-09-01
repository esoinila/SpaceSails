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

## Three mastheads, one wire

Where a paper is printed is part of what it says. `NewsWire.NewsScope` picks the masthead:

- **`SystemWire`** — the anonymous system-wide wire, and the default. The ship's Galley card and the
  Comms ticker read this and nothing else.
- **`PortRag`** — a docked port's own sheet: the system wire **plus** a local ambient family (berth
  fees, a crewman who has not reported back, the tariff pool), *salted by site* so the same sim-day
  reads differently at Ceres than at Pallas.
- **`CompanyIntranet`** — a secret lab's internal feed, carrying **no** system wire content at all.
  A facility that prints only its own weather is itself a tell: badge notices, a counter of days
  since the last unscheduled decompression, and a wellness check about a shore leave you cannot
  remember.

`NewsWire.Ambient(ephemeris, simTime, count, scope, salt)` stays pure — the salt is folded into the
same FNV-1a day seed, so two ports never read alike and an absent salt is the historical stream,
byte for byte.

**Which paper a place prints** is one pure Core question: `NewsWire.ScopeAt(NewsPlace)`, where
`NewsPlace` is (aboard ship?, which site?, inside a secret lab?). It defers to
`SecretLab.ReadsCompanyIntranet` for the lab half, and that is the whole of the seam — nothing else
about a place is allowed to leak into Core.

## The law: the wire never names the captain

Every deed on the wire stays third-person anonymous — *"A ship slipped quietly into orbit at
Enceladus — the regulars ask no names."* That is the point of the mechanic: the fun is the captain
reading his own crime over a drink and keeping a straight face, and the moment a headline says
"your ship" or prints his callsign, the bar stops being somewhere he can sit. No headline ever
attributes a deed to the reader, nothing on any masthead titles or addresses *the* captain, and the
one ambient family that names a hull draws only from the traffic board's NPC callsigns. Guarded by
`TheWireNeverNamesTheCaptainTests`.

## Where the state lives

`NewsWire` itself (in `SpaceSails.Core`) is pure and stateless — no `DateTime.Now`, no
`System.Random`, per the determinism rule (§9) every other Core module follows. The actual ledger
of pushed events is a small, bounded list (`Map.razor`'s `_newsEvents`, capped at 50, newest
first) — the same "Core stays pure, the mutable ledger lives with the caller" split the
[tracking post](tracking-post.md)'s own ledger uses. `Map.razor`'s
`NewsFeed(ambientCount, scope, salt)` helper blends that ledger with `NewsWire.Ambient` and hands the
result to whichever desk is asking. `scope` defaults to `SystemWire`, so the two desks below read
exactly what they always read; a `CompanyIntranet` reader gets the facility's paper and none of the
pushed events, because a lab noticeboard does not carry news of a robbery three planets away.

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
