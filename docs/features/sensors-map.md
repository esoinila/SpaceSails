# The sensors map — point at the sky and ask

What this is: the Sensors desk's whole philosophy after the Sunday-second chain (PRs
[#69](https://github.com/esoinila/SpaceSails/pull/69)–D). The live map IS the desk: the sky
dims in behind the instruments but stays fully interactive, and **everything visible on it
answers a click with scan options**. The map is where you find things; this page is the spec
for how.

Where: press `2` or click **2 Sensors**. The tracking-post instrument itself (rosette, sweep
sliders, ledger mechanics) is specified in [tracking-post.md](tracking-post.md); this page
covers the map integration and the one-telescope task system layered on top of it.

## One telescope, one queue

The ship carries a single steerable telescope (`TelescopeModel`). It cannot look more than
one way and one focus at a time, so every job it's asked to do becomes a **sensor task** on a
visible, prioritized queue (`Core/SensorTasks.cs`, `TelescopeSchedule`):

| Task | What it does | Stays queued? |
| --- | --- | --- |
| 🎯 Track update | A short custody pass on a held track's predicted position (`TryConfirm`) | Standing, one per ledger slot |
| 🔭 Area scan | Point at a patch of sky and resolve what's there | Once |
| 📦 Corridor sweep | Sweep the wedge covering a trade lane | Once, or standing ("lane watch") |
| 🔍 Lost search | Search the region where a lost track must still be | Until re-acquired, abandoned, or cold |

The instrument works the list **top to bottom and wraps around** (a carousel). Each pass is
priced by the wedge it must sweep (`SensorTaskGeometry`, reusing `ScanJob`'s 6-hours-per-360°
rate, floored at 10 minutes for slew-and-settle); the cost is shown before you commit.
Reorder with ▲▼; ⏫ runs a task as the very next pass *without* stealing anyone's carousel
turn. A manual slider sweep preempts the queue entirely; the M27 **passive watch** (free
full-circle survey) runs only when the queue is empty.

**Custody is a real resource.** Tracking one ship is near-continuous custody; tracking four
plus a lane watch leaves gaps a transponder-dark ship can burn inside. The telescope
dock-market upgrade now sets both ledger slots (1→4) *and* pass speed (+50%/level).

## The sky shows its state (map overlays, Sensors desk only)

- **Trade lanes** — every anchor pair (Venus/Earth/Mars/Jupiter/Saturn) draws as a faint
  filled corridor with a label (`TradeCorridors.Regions`: segment between the bodies *now*,
  lane radius = max(1.2×10¹⁰ m, 5% of length)). The selected lane brightens.
- **The live scan wedge** — whatever the telescope is on right now, drawn from the ship and
  honestly range-limited by the sun-relative envelope; the swept-so-far portion fills in
  brighter, with a `📡 label · N%` line.
- **Lost-lock search circles** — a pulsing circle at the dead-reckoned center, radius growing
  with the same cone terms that broke the lock.
- **Update-pass flash** — corner brackets + "updating fix" on a tracked ship the moment its
  scheduled pass completes.

Nav keeps its clean plotting sky; none of these draw off the Sensors desk.

## Every click answers (PR-C)

- **Live ship contact** → the M29 contact menu (interest 🎯, track 📡) plus **🔭 SCAN AROUND
  HERE** (a vicinity area scan).
- **Labeled contact with no live fix** (the mute-Barnacle case) → "no live fix — last seen
  N d ago" and a scan to find out where she went. Last-seen markers are clickable.
- **Planet/body** → a scan menu with the telescope-time cost up front. **Set destination and
  auto-insert are Nav-desk-only**: navigation is done at the Navigation desk.
- **Inside a trade lane** → sweep now, or post a standing lane watch (the old `— manual
  aim —` corridor dropdown is retired; the lane on the map is the control).
- **Empty sky** → the ✨ Open Sky menu: distance, patch radius (follows your zoom: what looks
  like "about here" is what gets scanned), cost, and the near-lane hint ("near the Earth–Mars
  lane — sweep the lane instead?"). A genuine click only: pans and menu-dismiss clicks don't
  open it.

## The populated sky (`ScanDiscoveries`)

Point a Hubble anywhere and you find *something*: debris, rocks, cold cargo pods, the odd
derelict. Discoveries are a pure function of (scenario seed, 2×10¹⁰ m sky cell, sim day) —
deterministic, never empty (a guaranteed faint return synthesizes if the cells are quiet),
and the sky turns a page each sim day. Results land under **✨ Last area scan resolved**.

## Lost locks leave a search area (`LostTracks`)

When a track's quality decays to loss, the ledger hands it to the cold-case board instead of
forgetting it: a `LostTrack` carries the last observation and a search region centered on the
ballistic dead-reckoning, growing with the cone's own velocity-sigma + plausible-burst +
maneuver terms (capped at open so every fresh case starts searchable). A 🔍 search task
auto-enqueues. A pass that finds her re-acquires the lock; a fruitless pass still narrows the
region (×0.6); past 5×10¹⁰ m the trail is cold and the case closes. **⏫ PRIORITIZE
REDISCOVERY** pushes the search to the very next pass — every other task waits, which is
exactly the trade the vision asked the scanning desk to own.

## Tracked-target boxes (PR-D)

One card per ledger slot, each with its **own live scope box** (a `ScopeView` on the shared
render loop — PR-12's scope wall reborn, per-card). ● ON SCOPE marks the card whose update
pass is running. Cards keep the full nav-grade picture: range/closing/bearing, the eyes race,
the beacon verdict, quality bar, and interest/confirm/drop.

## Determinism notes

Everything gameplay-visible is Core and deterministic: the schedule emits identical passes
for identical operations (durations sampled once at pass start), discoveries are seeded, the
search-region math is pure. Wall-clock only decorates (the pass-flash fade, the search-circle
pulse).
