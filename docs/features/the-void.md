# The void — the compartment that is not on the plan

*Status: **designed, not built**. Owner's idea, 2026-07-29.*

> "some of those ships may have been used for smuggling. So there might be hidden compartments we need to
> discover somehow. ship outside is mysteriously bigger maybe because one door is camouflaged as a empty
> space but is not accessible in normal fashion. It would be hidden well enough to pass a boarding party
> doing a routine cargo check. But something could tip us off about it's existence."

---

## 1. The detector already exists

This is the part that makes the idea cheap and good: **we have already built the instrument that finds
it, and we built it for something else.**

The valve board is a mimic drawn from `WreckLayout`'s own constants — hull outline, spine, every
compartment in its true place and proportion. A captain who looks at that board is looking at *the ship's
own account of herself*. So a void is not a quest marker and not a scan result. It is **a gap on the map
between two bulkheads that no room claims**, sitting there in plain sight, waiting for somebody to notice
that the outline is bigger than the sum of its rooms.

Nobody is told. It is drawn correctly and it does not add up.

> **The rule for the whole feature: the tell is always something the player could have noticed themselves.**
> Every tip-off below is a physical consequence of the void existing, never an announcement that it does.

## 2. The tells, in the order a careful captain meets them

Tiered on purpose, so a first-time captain misses it entirely and a thorough one has three separate
reasons to go looking.

| # | Tell | Where it comes from |
|---|---|---|
| 1 | **The outline does not add up** | The mimic on the valve board. A stretch of hull between two bulkheads with no compartment name on it. |
| 2 | **Her paperwork is short** | Manifest tonnage against hold capacity — the "insured for a cargo she never had room for" seam from [the-paperwork.md](the-paperwork.md), read the other way round. |
| 3 | **The pump runs long** | 🛢 Pump down the compartment next door and the mechanical stage takes longer than that volume should need. Air is coming from somewhere the board does not list. |
| 4 | **A cold wall where there should be hull** | The 📟 operating log of the adjacent room: a bulkhead reading colder than the hull outboard of it, because there is unheated air behind it rather than vacuum. |
| 5 | **She sounds wrong** | Struck by hand, aboard. The cheapest tell and the most human one — the one a boarding party doing a routine cargo check would have had time for and did not do. |

Tells 3 and 4 are the good ones, because they are the **atmosphere system paying for itself twice**: the
pump and the operating log were built to decide whether to kill something, and here they quietly become
survey instruments. Nothing new has to be invented for either.

## 2b. THE PANEL IS NOT A BULKHEAD — pressure finds it for you

> Owner: *"the fake door might not be able to take the deformation pressure of the other side being pumped
> out and open during that :-D. The map would then grow once the room is refilled and opened."*

This is the best idea in the feature and it should be the **primary** discovery route, with the deliberate
search in §3 as the alternative for a captain who suspects and wants to be sure.

It is also simply true. A camouflaged door is **joinery** — a panel dressed to look like structure, good
enough for a boarding party's eyes and a knuckle-rap. It was never a pressure bulkhead, because making it
one would have meant fittings, welds and a survey, which is exactly the paperwork the whole thing exists to
avoid. Put one atmosphere across it and it does what unrated panels do.

> Owner, on the engineering of it: *"thin sheet metal is no pressure vessel strong bulkhead :-D"*

The numbers are not close. A doorway-sized panel is on the order of two square metres, and one atmosphere
across that is **something like twenty tonnes**. A real bulkhead carries it through a frame into the hull;
a sheet screwed to a batten does not carry it anywhere. The failure is not marginal, and it is not quiet.

**And it bows before it goes.** That is the other half of the owner's phrase — *deformation* pressure —
and it is a free warning worth building: a captain standing in the compartment while the pump runs can
watch a wall they have walked past three times start to **breathe**. Panel first sucking flat against its
battens, then the seams starting, then the fasteners.

Which gives the mechanic a fair out: notice the wall moving and you can stop the pump. The air is already
banked at the rough mark, and stopping costs you nothing but the kill you were not going to get anyway.
Miss it, and the ship decides for you.

Which means **the atmosphere system finds the void without the captain ever suspecting it**, as a
consequence of ordinary damage-control work:

```
  1  pump down / vent the compartment next to it
  2  the void still holds air; the panel now has Δp across it
  3  the pump RUNS LONG  ← the tell, and now it has a cause: you are pumping two rooms
  4  something lets go, audibly, somewhere else on the ship
  5  the void is open to the pumped compartment — and therefore to vacuum
  6  refill or equalise, walk in, and THE MAP GROWS
```

Step 4 is the beat. You are out in the corridor watching a counter, holding a lane with a bot, and
**something on this ship fails on its own** — a sound you did not cause and cannot place. Nothing in the
game announces what happened. The board simply has a compartment on it that was not there before.

### Why this is worth more than a search

- **It is missable in the best way.** A captain who never pumps anything never finds it. A captain who
  pumps for thrift finds it by accident and has no idea what they have done until they look.
- **It rewards the thrifty road specifically.** Pumping is the patient, careful option; it being the thing
  that uncovers the ship's secret is the correct payoff for patience.
- **It gives the "pump runs long" tell a mechanism** rather than a hint. The pump is slow *because it is
  moving more air than the board says exists*, and the number was in front of you the whole time.
- **The map growing is the payoff of the mimic.** A board drawn from the ship's own numbers gains a room.
  Nothing else in the game can do that.

### The cost nobody sees coming

If the void holds an **occupant** (§4b), this route kills them. The panel fails, their air goes into a
compartment you are pumping to vacuum, and they are gone before you have any idea there was a room. The
captain who did it was being *careful with air*.

They will find out later, on a channel nobody was transmitting on, when they scuttle her — and never know
which of the things they did that day was the one that mattered. That is the whole lane working as
designed, and it costs nothing extra to build.

*Wiring note:* once a void exists, the pump must count its volume (that is the tell), the equalisation
propagation in `HullVenting.EqualiseAt` must treat a blown panel as an open door, and a vented void must
feed `SomethingStillAliveAboard()`.

## 3. Getting in

It passed a routine cargo check, so it is not a door with a handle. The idiom already exists:
`ConsoleKind.SecretDoor` and its forcing channel, built for Vantar's labs (#409) — walk to it, force it,
and the region opens.

Reuse it exactly. What differs is the **finding**, not the opening: the lab door is revealed by a cheat or
a survey, and this one is revealed by arithmetic the captain did themselves.

Once open, the void becomes a real compartment: walkable, ventable, on the board, with a name that appears
for the first time. *A room that was not on the plan a minute ago is now a switch on the mimic* — which is
a very good moment, and free.

## 4. What is in there

Not just cargo, or it is a lockbox with extra steps. The void is **where a wreck's real story lives**:

- **Contraband** — high value, entirely hot, and *no paperwork at all*, which is its own problem. Cargo
  with no manifest entry cannot be filed, only carried.
- **The reason she died.** A hull with a void is a hull somebody was doing something with. On an
  `InsuranceJob` this is where the cargo she was supposedly carrying *actually* went.
- **An archive node** ([the-archive-node.md](the-archive-node.md)) — the obvious place to move one you
  never invoiced. The one warm thing aboard, in the one room not on the plan.
- **Somebody.** A void is airtight and unlisted, which makes it the best place on the ship to survive in
  and the worst place to be forgotten in.

## 4b. THE OCCUPANT — the part that makes it worth building

> Owner, on being told the void sits outside the venting system: *"ohhh that is scary that space could have
> extra occupants :-D"*

He is right, and it is worth being precise about *why*, because the horror here is structural rather than
written. **A void is a room the atmosphere system does not know about**, and that has consequences that
arrive on their own:

- **You cannot vent it.** Not "it is hard to vent" — the board has no switch for a compartment that is not
  on the plan. Blow every room aboard her, wait out every soak, watch every counter reach a number you are
  satisfied with, walk out — and one room still has air in it.
- **You cannot read it.** The 📟 operating log lists compartments. Something living in the space between
  two of them never appears, warm or otherwise. The instrument that spent the whole boarding refusing to
  tell you *what* is alive now cannot tell you *where* either.
- **Every tell becomes worse in hindsight.** The pump ran long because something in there is breathing that
  air. The bulkhead read cold because there is an unheated volume behind it. You noticed both, filed them
  as curiosities, and moved on.
- **It has been listening to all of it.** You have been standing at a board a few metres away, cycling
  valves and dogging hatches, for the entire boarding.

### The two features detonating together

The best consequence is one neither feature has on its own. `Scuttle.SheGoes` asks a single question —
*was anything the vacuum had not finished still aboard?* — and it never says what. If the void's occupant
counts toward that answer, then:

> **You can hear the clawing from a room you never found.**

A captain who scuttles a hull they were *sure* they had cleared gets the card anyway. Not a jump scare, not
a reveal — just the confirmation, exactly as always, saying there was something. They will go over the
boarding in their head and find nothing they did wrong, because there was nothing to find. And the game
never tells them, because the game has never once told them.

*Wiring note, small but load-bearing:* the client's `SomethingStillAliveAboard()` currently counts the pack
and unfinished infested compartments. It must count an undiscovered void's occupant too, or the sharpest
moment the wreck lane can produce quietly does not fire.

### The other half: it can also be a person

Keep both. A void is the best place aboard to survive in, and a captain who **finds** one before scuttling
can get somebody out of it — the careful road paying off in the one place nobody was looking. Which means
the same room is either the reason you should have searched harder, or the reason you are glad you did, and
it is seeded per wreck which one you got.

## 5. The invariant this feature breaks, and how to keep it honest

Worth writing down before anyone builds it, because it is a trap.

`WreckLayoutTests` currently pins **`CompartmentsShareBulkheads_LeavingNoDeadSlots`** — compartment bounds
must be contiguous, no gaps. That test exists for a real reason: gaps used to be 1-unit slots, walled both
sides and narrower than the captain, traps with no way in that existed only to go wrong.

**A smuggler's void is exactly a dead slot.** So the audit has to be taught the difference, or it will
either forbid the feature or start permitting the bug it was written to catch:

1. Voids are **declared** in the layout, not incidental. An undeclared gap stays an error.
2. `CompartmentsShareBulkheads` ignores declared voids and keeps failing on everything else.
3. A **new** test asserts each declared void is unreachable while sealed and reachable once opened — so
   "hidden" is proven, and so is "not a tomb".
4. The A* station audit must never route through a sealed void, or CI would be quietly relying on a room
   the player has not found.

## 6. Build order

1. **Core: declared voids in `WreckLayout`** — bounds, a seeded per-wreck presence, the door position, and
   the audit changes in §5. Pure and tested first, because this one edits an existing invariant.
2. **The tells** — the mimic gap is free (it draws what is declared); the pump-duration and cold-wall tells
   are small additions to systems already shipped.
3. **The forcing** — reuse `SecretDoor`'s channel.
4. **The reveal** — the void joins the compartment list, the board, and the vent rules the moment it opens.
5. **Contents** — contraband with no manifest entry; then the crossover hooks (node, arcs, occupant).

## 7. Rules kept

- **Every tell is a consequence, never an announcement.** The game never says "this ship seems larger than
  it should".
- **It has to be missable.** A void that every captain finds is just a room with a longer walk.
- **Found by arithmetic, opened by work.** Noticing is the skill; forcing it is the price.
- **Deterministic.** Which hulls have one, and what is in them, is seeded off the wreck.
