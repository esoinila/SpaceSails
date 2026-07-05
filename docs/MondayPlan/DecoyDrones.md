# Decoy Drones — false hulls for the false colors

*Owner + Fable, 2026-07-05. A [MondayPlanVision](MondayPlanVision.md) feature pulled into its
own document: the physical layer of the lying game. M29 shipped the signal lies (the
transponder's DARK and FALSE COLORS, the beacon ghost); decoys make the lie survivable
against someone who actually looks.*

## Why the beacon ghost isn't enough

M29's FALSE COLORS broadcasts the ghost of the course we abandoned — and
`TransponderRule.PictureFor` is honest about its weakness: **any observer whose own optics
resolve the real hull has us provably lying** (`BeaconPicture.LieBlown`). The ghost is pure
signal; there is nothing at those coordinates. A hunter who bothers to point a telescope at
the beacon fix sees empty sky, and a hunter who spots our real hull sees a ship that
provably isn't where its transponder says. Either look kills the story.

The decoy closes exactly that hole: put a real object where the lie claims one.

## Variant A — the course decoy (the owner's original)

> "Maybe even have a fake drone continue on our course transmitting the correct looking
> signal while we go be pirates."

- **Deploy**: from the Sensors desk while FALSE COLORS is armed. The drone takes over the
  beacon ghost's state — same position, same velocity, transmitting OUR transponder id —
  and the ghost stops being fiction: it's now a hull on rails-honest ballistic flight (the
  same `Simulator` everything flies on; determinism is law).
- **What it beats**: the optical cross-check. A telescope pointed at the beacon fix now SEES
  a ship-sized return on the right course. `BeaconPicture` gains a resolution level:
  `GhostConfirmed` — the observer looked and the lie *passed*.
- **What still beats it**: closing to resolve range (suggest ~1e9 m, the active-radar
  precision band): up close the hull reads as a drone, and the picture flips to a new
  `DecoyBlown` — worse than `LieBlown`, because a drone squawking our id is premeditation,
  not coincidence. Heat accordingly.
- **Limits that keep it honest**: the drone is ballistic-only (no maneuver budget, so Lab-08
  cone honesty makes its future trivially predictable — a decoy that never corrects for a
  scheduled brake burn will MISS its declared arrival window, and a patient observer can
  catch the story going stale at the timetable level). One decoy aloft at a time.

## Variant B — chaff (the *Serenity* variant) — ⭐ THE OWNER'S PICK, build this first

> "In Serenity (the Joss Whedon movie) they used these fake transponders to give fake
> targets to the ships hunting them... decoys with minimal capabilities except to look like
> a ship when no one really looks."

- **Deploy**: a spread of N cheap fakes (suggest 3), each broadcasting a plausible NEUTRAL
  id (not ours — chaff is for saturation, not impersonation), scattered on diverging
  ballistic courses.
- **What it does**: floods a hunter's picture. Every fake is a contact that must be
  classified, and classification costs what OUR classification costs — telescope time on
  the ledger, or a close pass. Our own track-quality mechanics, turned against the wolf:
  a hunter with `MaxTracks = telescopeLevel + 1` slots and four candidate contacts is
  spending real sensor economy on cardboard.
- **What still beats it**: the same resolve range, and patience — chaff never maneuvers, so
  anything that burns is real by elimination. Chaff buys HOURS, not absolution.

## Economy and consequences

- **Dock purchase**, new upgrade-adjacent line: course decoy expensive (it's a hull with a
  transmitter), chaff cheap per unit, both consumed on launch.
- **Evidence**: a spent decoy doesn't evaporate (slugs get 6h TTL; decoys should linger).
  A boarded/scanned dead decoy carrying our id is a confession with a serial number —
  news-wire line, standing heat, maybe the one thing a haven fence will pay to make
  disappear.
- **The parrot** learns the launch: "Cardboard crew, away!" / "Let them chase the kite!"

## Build order (when Monday comes) — chaff leads, per the owner

1. Core `DecoyRule`, chaff-first: decoy state record (ballistic, beacon id, kind
   Chaff|Course), neutral-id chaff spread, TTL/linger policy, classification cost (a chaff
   contact enters candidate lists like any hull and holds a track slot until resolved).
   Tests: the resolve-at-close-range matrix and the never-maneuvers-so-burns-are-real rule.
2. Client, chaff-first: the launch button on the Sensors desk (a spread of 3), chaff drawn
   ship-like until resolve range (we always see our own as 🪁), chip line. The course decoy
   (Variant A: takes over the FALSE COLORS ghost, GhostConfirmed/DecoyBlown picture levels,
   timetable-staleness case) follows as the deluxe second step.
3. NPC consumption lands with intention inference ([MondayPlanVision](MondayPlanVision.md)
   §1): hunters allocate their track slots over contacts and get fooled by exactly the
   rules above — no scripted gullibility.

## Open questions for the owner

- Can a course decoy fly a PLANNED brake (a one-shot stored maneuver plan) so the timetable
  stays plausible through arrival — at a steeper price?
- Should chaff ids be drawn from real timetable traffic ("there are suddenly two
  *Barnacles*") — funnier, but it incriminates innocents and might deserve its own heat?
- Do hunters ever shoot a suspected decoy just to see if anyone screams?

## Working agreement unchanged

Determinism is law in Core; UI = Razor + Bootstrap; JS only in renderer.js; senior
reviews/verifies; owner approves PRs.
