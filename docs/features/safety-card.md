# The safety card — the mimic as a passenger's map

*Status: **filed, not built**. Owner's idea, 2026-07-29, straight after the valve board landed.*

> "Let's file away a plan to make the safety-cards with the same map. Like move like this towards the
> rescue pods if the ship is having an emergency. And the lights in the floor will guide you. That kind
> of set-up will need a big ship, but it could be used as a clue to wonder where the passengers might be
> and go see if any pods have been launched when we investigate."

## The one idea

The valve board (#488) proved a compartment mimic reads as a ship: hull outline, named rooms in their real
places, drawn from `WreckLayout`'s own numbers so the map cannot drift from the deck. That same drawing is
also the thing bolted to the back of every seat on a liner — **the safety card**. Same geometry, different
audience, opposite mood. One is a captain deciding who to kill; the other is a passenger being told, in
pictures, where to run.

Which means the second use of the mimic costs almost nothing to draw and buys a whole scene.

## What the card is

An in-fiction laminated card, found aboard (a lockable prop in a compartment, or on the wall by the
lifeboat cradles), that shows:

- the **deck outline**, exactly as the valve board draws it;
- the **muster route** — arrows from each compartment to the nearest pod cradle;
- the **pod cradles** themselves, numbered, with capacity;
- the **floor lights**: the route markers that lead you out when the air is going and the lights are dead.

The floor lights are the good part. They're a real thing on real ships and aircraft, they're cheap to
render (a dotted path along the spine, lit and running toward the nearest cradle), and they belong to the
*wreck* rather than to the UI: on a dead hull they may be out, half-out, or running the wrong way because
somebody re-routed them, and each of those is a sentence about what happened here.

## Why it needs a bigger ship

Owner is right, and this is the honest blocker. The current wreck is eight compartments on one spine — a
muster route is "walk twelve metres." A safety card is only interesting when the ship is big enough that
you would not know the way: multiple decks, or at least a hull long enough that the nearest cradle is a
real choice and the two rows do not both simply lead to the middle.

So this feature is **downstream of a large-hull wreck class** — a liner, a cycler, a colony transport.
That's the dependency to state plainly rather than fake.

## Why it's worth building anyway: it is evidence

This is the part that makes it more than set dressing, and it's the owner's own turn of the idea.

The card tells you **how many pods this ship carried and where they were**. The cradles tell you **how many
are gone**. Subtract, and the wreck starts talking:

| What you find | What it means |
| --- | --- |
| All cradles full | Nobody got off. Everyone aboard is still aboard — the search is now a recovery, and the compartment readings matter enormously before you vent anything. |
| Some cradles empty | Somebody got off. There is a survivor claim somewhere, and a witness who can contradict whatever you file. |
| All cradles empty, ship intact | They abandoned a ship that did not need abandoning. Why? |
| Cradles empty but pods still aboard, unlaunched | They were *loading* and stopped. Something reached them first. |
| Route lights re-routed away from the cradles | Somebody did not want the passengers reaching the pods. That is not an accident cause any more. |

That maps directly onto the existing `Derelict.WreckCause` fork: it is another **Evidence** source, read the
same way the manifest and the log are, and it can `MisreadsAs` just as loudly. A ship with every pod gone
looks like a clean evacuation until the manifest says there were four hundred aboard and the pods hold
eighty.

And it gives the passenger question a shape the player can act on: the pods went *somewhere*. A launched pod
is a body in space with its own trajectory — a follow-up salvage, a rescue, or a witness who saw who was
aboard the ship you just quietly looted.

## Build order, when it comes up

1. **Large-hull wreck class** — multi-deck or long-spine geometry in `WreckLayout` (or a sibling), with the
   A* audit (`WreckLayoutTests`) walking it on every cause, as now. *The audit is the precondition, not an
   afterthought: a big ship is exactly where a sealed-in-half bug hides.*
2. **Pod cradles in Core** — count, capacity, per-cradle launched/unlaunched state, seeded off the wreck.
3. **The card itself** — reuse the mimic renderer with a muster overlay instead of valve switches. Shared
   drawing code, so the ship on the card is provably the ship you are standing in.
4. **Floor lights** — a lit route on the deck plan itself, not only on the card. Working, dead, or lying.
5. **Evidence wiring** — cradle state feeds `Derelict.Resolve`, with its own `MisreadsAs`.
6. **Launched pods as objects** — the follow-up hook. Optional, and the biggest of these by far.

## The rule that carries over

The valve board's law was: *the mimic is drawn from the ship's own numbers, so it cannot lie about the
geometry.* The safety card inherits it — and then deliberately breaks it in one place. The card is a
**printed document from before the accident**. If somebody welded a bulkhead shut, or the drive took the
aft third with it, **the card still shows the ship as she was built**. The difference between the card and
the deck under your feet is itself the story.

That is the whole feature in one sentence: *hand the player an accurate map of a ship that no longer
exists.*
