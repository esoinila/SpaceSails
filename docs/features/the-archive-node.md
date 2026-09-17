# THE ARCHIVE NODE — the thing in the hold that remembers you

*An object aboard a wreck that costs sanity to be near, pays in visions of a place you were never
meant to see, and carries a switch that does exactly what it says on the label.*

*Owner's idea, 2026-07-29.*

**Status: LANDED and playable — `/map?archive=1&land=1`.** Core (`src/SpaceSails.Core/ArchiveNode.cs`:
placement, the field, the throw and its bands, the authored vision pool, the collar, the switch and who
is inside it), `NervePips.Cause.Archive` (the slowest sustained beat in the game, and now actually
consumed by `NervePips.Advance`), the tenth wreck cause `Derelict.WreckCause.VentedByOneOfTheirOwn`, and
`HullVenting.StartsVented`. The client lane is `src/SpaceSails.Client/Pages/Map.Archive.cs` — the dwell
field in the walk loop, the confrontation card, the handle — with the two stations in
`WreckLayout.ArchiveStation` / `ArchiveSwitchStation` so CI walks to them. Guards: `ArchiveNodeTests`,
`VentedWreckTests`, `TheArchiveNodeIsOnTheDeckTests`. Art tracked in
[../art-manifest-visions.md](../art-manifest-visions.md).

**CLOSED, 2026-09-17 — §7 step 5, the death line, is BUILT.** `ArchiveNode.NoRestoreLine` had been
authored, wording-tested and **read by nothing** for two months, because printing `NO PATTERN ON FILE —
POLICY CLOSED` on a resurrection card that then resurrects you is the sentence-vs-sim bug this project has
paid for three times. It was filed as a decision rather than guessed ([#640]), and the owner ruled
**option A, THE RUN ENDS**: *"yes let's have that possibility … it is very film noir for the characters to
want that kind of things and it certainly closes a story arc for that captain."*

`Resident.YourOwn` now has mechanical bite, and the line is simply true — see **§5a**. Reach it on demand
with `/map?nopattern=1&death=impact`.

[#640]: https://github.com/esoinila/SpaceSails/issues/640

> "Somehow this reminds me of the Space Madness episode in Ren & Stimpy, where cadet Stimpy is left to
> patrol a big red button named 'history eraser button', that nobody knows what it does. At the end
> they both go all kinds of crazy and Stimpy presses the button … which, surprise surprise, erases all
> history. :-D But some kind of thing on a ship that affects sanity … some object that going close to
> it is told to the player with GEN AI art that they see flashbacks of their cloning facility they were
> not meant to see .. some spider like beings maybe even as workers there, something that forces a
> sanity roll when confronted … some new disturbing visions … those could contain clues about some big
> plots we have in the game."

**Design lineage, stated plainly:** the *shape* of the switch is an homage to Ren & Stimpy's "Space
Madness" (1991, John Kricfalusi / Nickelodeon) — a labelled control, an honest label, and a man alone
with it too long. Nothing of theirs is reproduced: our object, our fiction, our joke. House rule from
[KaamosPlotline.md](KaamosPlotline.md) §7 applies — *homage, not reproduction*.

---

## 1. Why this fits without inventing anything

The game already says where the bodies are. [NebulaArc.md](NebulaArc.md) §2, already canon:

> Nebula keeps the **originals**. A premium does not buy a spare body — it buys **storage of your
> pattern** in a cold archive … Everyone who ever paid is filed there, dreaming, kept
> just-lucid-enough to stay a valid backup. **You are the collateral on your own policy.**

Arc 2 hands that truth over in *readings* — a card that glitches, a poster read twice, an adjuster's
drink, a collector's writ. Every one of them is **text about a place**. This lane is the first time the
player is **in the room with the place itself**, and the first time the arc costs them something before
it tells them anything.

That is the whole justification: **arc 2 currently converts curiosity into knowledge; the node converts
nerve into knowledge.** Different currency, same arc, no new mythology required.

---

## 2. The object

**A cold-archive node** — Nebula Mutual substrate hardware, one spar of the warehouse, found where it
has no business being: strapped into the hold of a dead ship as cargo, or half-installed in a wreck's
own machinery space by somebody who was trying to fix something with it.

Physically: a frost-scabbed column, wet inside, still running on its own decay heat decades after the
ship died — the **one thing aboard that still has power**. That detail does the work. Every other wreck
in the game is cold and dark ([art-manifest-wrecks.md](../art-manifest-wrecks.md) enforces it, and cost
three passes on the mutiny canvas to keep). A hold where one object is *warm* is wrong in a way the
player will feel before they can name it.

Stencilled on the housing, in the manner of every piece of industrial kit ever built, a legend that is
completely honest and that nobody reads as a warning until afterwards. See §5.

**Where it appears.** Not on every wreck — rarity is the point. Seeded off the wreck id, and only on
causes where it *explains* something: `InsuranceJob` (of course), `Mutiny` (they were arguing about
something), `LifeSupportFailure` (nobody noticed the air going because nobody was entirely present).
Never on `Infested` — that hull already has a full deck of tension and does not need two.

---

## 3. The mechanic: dwell, not press

The Stimpy joke is about **time alone with a thing**, not about a button. So the node is not primarily
an interaction — it is a **field you have to cross**, in a compartment with something you want.

Three layers, escalating, each one the player's own choice to accept:

### Layer 1 — the dwell (sustained, no dice)
Inside the node's radius, `NervePips` charges a **sustained beat** — the existing `Cause.Cornered` /
`Cause.Digging` idiom, a new `Cause.Archive`, one pip every `ArchiveBeatSeconds`. Walk through and it
costs you nothing worth counting. Stand and work in there — and the salvage you came for is *in that
compartment* — and it counts. **The player sets their own dose.** No prompt, no dialog, just the pip
row ticking while they decide whether they are done here.

*This is the same lesson as the vent panel: the interesting decision is one the game does not ask you
to make.*

### Layer 2 — the confrontation (the roll)
Approach the node itself — inner radius, or press E — and it **forces a throw**, `DiceRule` d20 with
named modifiers, the house idiom (visible arithmetic, no hidden fudging):

| Modifier | Value | Why |
|---|---|---|
| Pips remaining | `+PipsNow − 5` | Steady hands look; a frayed captain is looked *at* |
| Suited | `+2` | The helmet is between you and it |
| Seen it before | `−2` per prior confrontation aboard this wreck | It knows the way in now |
| **Fragments already held** | **`−1` per Nebula shard** | **The more of arc 2 you have assembled, the more of the vision you are equipped to understand — and understanding is the injury** |
| Never died this thread | `+3` | It has nothing of yours on file yet |

Outcome bands (the `Derelict.Resolve` fork idiom — a table, not a coin):

- **≥ 15 — you look away in time.** One pip. A half-image, no fragment. The node is still there.
- **9–14 — you see it.** `ArchiveVisionPips` (3, matching `MonolithPips`). **One vision, one arc-2
  fragment.** This is the trade the whole feature exists for.
- **4–8 — it sees you looking.** The vision plus a pip, and the next confrontation aboard this wreck
  starts at `−2` more. Something in there turned its head.
- **≤ 3 — you are in it.** The vision resolves to the rack with your own policy number on it. Heavy
  nerve, and (if `CaptainSuccession` has ever fired this thread) it is not a stranger on the slab.

### Layer 3 — the switch
See §5. It is the only part that is a button, and it is the only part that is irreversible.

---

## 4. The visions — five, seeded, each a fragment

Deterministic and authored (Core law: no RNG in the pool, the dice choose *which* is next only through
a seeded order). Each is a full-frame gen-AI still with **no caption from the game** — the image is the
evidence and naming it is the captain's job, the same rule the wreck canvases keep.

Owner's spiders are the spine of the set, and they get an in-fiction anchor that explains them without
resolving them:

> **A stored pattern has no eyes.** What the archive gives back is not footage — it is what a mind with
> no body made of the things that handled it. It remembers the *way they moved*: too many joints, too
> patient, always working in threes, always gentle. The shape it settled on to hold that memory is a
> spider. Whether the technicians are people in rigs, waldos run from somewhere else, or something that
> was never on the payroll, **the archive does not know either.** It only knows how careful they were.

That is deliberately not an answer. It is a **new question, planted in a mechanic**: *who actually
operates Nebula's warehouse?* — which is the seed for a third arc, exactly as the plaque line was for
KAAMOS.

| # | Vision | What is shown | Feeds |
|---|---|---|---|
| 1 | **The rows** | A hall of jars, kilometres of them, racked to a ceiling with no ceiling. Every one labelled, none named. Cold enough that the air has given up. | `NebulaSource` — a new shard, or `fine-print` reinforcement |
| 2 | **The handlers** | Three long-limbed shapes working a rack together, unhurried, entirely gentle, in a light that is not for eyes. They are not looking at the jar. They are *listening* to it. | The third-arc seed |
| 3 | **The intake** | A clinic gurney, a rig lowering, and — the wrong detail — **two** patterns being drawn where one subscriber lay down. One is filed. One is billed. | `policy-terms` support |
| 4 | **The wintering** | Water without a surface, and in it something the size of a district, keeping still. Not the archive: the thing the archive was **copied from**, and it is bigger. | **KAAMOS cross-link** — the arc-convergence rhyme |
| 5 | **The rack with your number** | Your own policy number, stencilled, on a jar that is occupied. It is lucid. It has been waiting the whole time you have been alive. *(Gated: only if the captain has died at least once this thread.)* | The convergence beat, `ArcConvergence` |

Vision 5 is the one that makes the feature: it is not lore about a stranger. It is the player's own save
file looking back.

---

## 5. THE SWITCH — the honest label

On the housing, stencilled by an engineer who had no reason to be coy:

```
    PURGE NODE — RESIDENT PATTERN NOT RECOVERABLE
```

It means precisely that, and the game will not restate it, hedge it, soften it, or put a confirmation
dialog in front of it. **The label is the confirmation dialog.** That is the entire Ren & Stimpy joke
and it only lands if we keep our nerve and let the player pull it.

**What it does:** the noise stops. The dwell ends, the visions end, the wreck goes as quiet and cold as
every other wreck — and `ArchiveRelief` pips come back, because *relief is real and that is the trap*.
Whatever was resident in that spar is gone.

**The catch, and the careful road out of it.** Whose pattern was in there is decided by the wreck's own
seed and is **knowable before you pull** — but only by paying: a confrontation at the ≤ 8 band reads the
jar's label. So the choice is the game's standard shape, the one the vent panel already teaches:

> *The instrument cannot tell you. The dice can. The dice cost nerve. You may pull the handle without
> paying, and you will never find out what you did.*

Three things it can be, seeded:
1. **A stranger.** Nothing happens that you will ever learn about. (Common.)
2. **A subscriber whose collector is still looking.** Purging it clears a heat/hunter thread — a real,
   payable reward for the reckless road, so the switch is not merely a punishment.
3. **Your own.** Rare. Your insurance rebirth is over for this thread and **nothing says so**. You find
   out the way Stimpy did: afterwards. The next death is simply the last one.

Case 3 must be **survivable to discover, not merely brutal**: the game tells you *at the moment of
death*, not silently — the resurrection card comes up and reads the line it has never read, and that is
the payoff for the whole feature. And because it is per-thread and seeded, a captain who wants to keep
their policy can simply not pull an unread handle. The information was always purchasable.

---

## 5a. THE RUN ENDS — what case 3 actually does (#640, owner ruling 2026-09-17)

> *"yes let's have that possibility … it is very film noir for the characters to want that kind of things
> and it certainly closes a story arc for that captain."*

**At the handle.** Nothing. `ArchiveNode.ClosesThePolicy(Resident.YourOwn)` is true, so
`NebulaProgress.MarkPolicyClosed()` is called and the flag rides the vault
(`NebulaSection.PolicyClosed`), which is what lets the handle and the death be hours and a reload apart.
No pulse, no log line, no ledger entry, and — the owner's standing instruction for this beat — **nothing is
added to the label**. It said `RESIDENT PATTERN NOT RECOVERABLE` and was telling the truth.

**At the next death.** `BustedResurrect` asks the flag as its FIRST question and hands off to
`NoPatternOnFile`, which is written as the list of things that do not happen:

| the ordinary wake | this one |
| --- | --- |
| `InsuranceRule.ApplyToRebirth` → clinic, bill, rustbucket, kit, full tank | none of it |
| `WakeAtNearestHaven` moves the ship | the ship is not moved; nobody is flying it |
| `IssueSuccessorCaptain` → new name, new face, retiree row, the filing line | no successor, ever |
| the rebirth glitch and the clinic's second page | both are things read AT a clinic |
| `Stage.Resurrected` | `Stage.NoRestore` — one line, `ArchiveNode.NoRestoreLine`, verbatim |

**What "the thread closes" means.** `GameThreadRegistry.Close(id)` marks the row
`GameThreadInfo.Ended`, and `Active()`/`Newest()` skip ended rows — so **Continue no longer leads into a
run with no captain in it**, and a shelf whose every thread has ended answers null exactly as an empty one
does. The client also puts its autosave pen down (`_threadIsOver`), so the last autosave that thread ever
wrote is the moment the captain was last alive.

**What it does NOT mean.** Nothing is deleted. The captain, the retirees, the graves, the selfies and all
ten save berths stay on the shelf and the logbook still lists them — a run ending is not a record being
erased. **No other thread is touched.** And the way off the last card is a single control, `Close the
book`, which closes the card (the general UI law of 2026-08-24) and opens the FRONT door rather than the
ship's own drawer, because there is no ship to go back to.

**Reach it on demand:** `/map?nopattern=1&death=impact` — the flag, then the real death through the real
pipeline. Combine `?nopattern=1` with any `?death=` to choose the place.

---

## 6. How it feeds the big plots

- **Arc 2 (Nebula, #422)** gets a *delivery system with a price*. Fragments 1, 3 and the capstone all
  gain a route that does not depend on being in a bar.
- **Arc 1 (KAAMOS, #411)** gets vision 4 — the archive remembering the thing it was degraded from —
  which is exactly the "rhyme before they converge" the arc doc asks for, delivered visually instead of
  in prose.
- **`ArcConvergence`** gets its most personal beat: vision 5 is a captain seeing their own filed self.
  The existing joint threshold does not need changing; this just makes reaching it *cost*.
- **A third arc is seeded** by the handlers, and by nothing else. Who runs the warehouse is asked here
  and answered nowhere.

---

## 7. Build order

1. **Core: `ArchiveNode.cs`** — pure and tested. Placement predicate (which wrecks, seeded), radius
   constants, the modifier table, `Confront(seed, state)` → band + vision id, the vision pool with its
   verbatim lore, `PurgeOutcome(seed)`, and the `NervePips.Cause.Archive` cost/beat constants. No client.
2. **Core: nerve seam** — `Cause.Archive` in `NervePips` (sustained, `ArchiveBeatSeconds`), with the
   ledger line so the captain's ledger says *why*: **"you stood too long beside the archive node."**
3. **Art** — five vision canvases via grok, `docs/art-manifest-visions.md`, same evidence-not-conclusion
   rule as the wrecks.
4. **Client wiring** — ✅ **landed.** The node as two `WreckInterior` consoles (the column and the
   handle, 3.5 du apart so the handle can be pulled without a throw) + a dwell field in the deck loop,
   the confrontation card, the switch, and the `?archive=1` quick start.
5. **The death line** — ✅ **landed (#640, 2026-09-17).** The one new page on the death card for case 3,
   and the one everything else was for. It was blocked on an owner ruling — *does purging your own
   pattern actually close the policy, or does the card merely read differently?* — because the line
   cannot ship until the sim backs it. The owner ruled **option A, the run ends**, so the sim backs it:
   see §5a for exactly what happens, and `/map?nopattern=1&death=impact` to watch it.

### What the vision cards hand over (decided in the client lane, flagged for the owner)

§4's *Feeds* column names what each vision supports, not which shard id it delivers, and the pool in
`NebulaLore` is **not** extended — a sixth clause would move `IntelNeededToUnlock`'s arithmetic under a
shipped arc. So the node is a second *route* to existing shards:

| Vision | Hands over | Why |
|---|---|---|
| `archive-rows` | `fine-print` | The poster's grey line says the thing they keep is the PATTERN. This is the warehouse, and you have now been in it. |
| `archive-intake` | `clinic-ledger` | Two patterns off one subscriber; one filed, one billed. The bill's second page is that arithmetic from the clerk's side. |
| `archive-handlers` | *nothing* | The third-arc seed. A shard would file a question as an answer. |
| `archive-wintering` | *nothing* | The KAAMOS rhyme, delivered as a picture rather than a clause. |
| `archive-your-rack` | *nothing* | Not lore about the corporation — the player's own save looking back. No ledger entry would not cheapen it. |

## 8. THE TENTH CAUSE — the ship that vented herself

> Owner, 2026-07-29: *"One ship destiny might be that somebody went crazy due to an object and vented
> most of the ship and the survivors also fell to some other craziness after that. So there would be a
> theme. :-D"*

This is the piece that makes the node a **mechanic instead of a set-piece**, and it costs almost
nothing to build because both halves already exist. A new `Derelict.WreckCause` — **`VentedByOneOfTheirOwn`** —
where the accident investigation resolves to: *someone aboard stood too long beside the thing in the
hold, walked aft to the valve board, and blew the ship compartment by compartment with her crew inside.*

### Why it is the best cause in the set

**The player has already done this.** They have stood at that board, looked at that mimic, read a life
sign the instrument could not identify, and pulled the handle anyway. The nine other wrecks are stories
about strangers. This one is a story about **the exact thing the player did an hour ago**, and it is
waiting on the deck for them, finished.

### The evidence, and the joke that makes it airtight

Every internal door on this wreck is **sealed from the spine side** — thrown at the valve board, not
barricaded from within. The compartments are vacuum-frosted. The board aft has every handle pulled.

And **one compartment is not vented.** Exactly one.

That is not a detail we have to invent, because the game already enforces it: `HullVenting.Readiness`
refuses `CaptainInside` — *the board will not blow the room you are standing in.* The single warm room
on a dead ship is **where they were standing**, and the player knows that rule from their own hands.
The evidence for who did it is a rule the player learned by being refused.

*(That is the whole design in one line: an interlock written as a safety feature, read years later as a
confession.)*

### The second madness — what the survivors did next

Whoever was in that one room, plus a couple in suits, survived their own ship. Then, per the owner's
*"fell to some other craziness after that"*:

**They kept the ship running for a crew that was no longer aboard.** The log station carries months of
entries after the venting, in an immaculate administrative hand: watch rotations, meal counts, name
after name signing on and signing off. Forty names. The tables are still set. Nobody ever writes down
what happened, because in the log **nothing did**.

That rhymes deliberately with `KaamosLore`'s **"one voice using all forty of their names"** and with the
glitch card's `PATTERN 40` — the third place the same motif surfaces, and the first where the player
finds it as a physical object rather than a rumour. See [KaamosPlotline.md](KaamosPlotline.md) §2.

### The fork this cause offers

`Derelict.Resolve` already forks honest filing against quiet looting; this cause sharpens it, because
the honest filing is **an accusation of a named dead person** on evidence that is circumstantial — and
the node responsible is still aboard, still warm, and worth a great deal to somebody.

| Road | What it costs, what it buys |
|---|---|
| **File it true** | Name what the board shows. The investigation pays; a family somewhere reads it. |
| **File the misread** | It reads perfectly well as `LifeSupportFailure` — the air went, the doors shut. Easier, pays the same, and the node stays unmentioned in the record. |
| **Take the node** | The most valuable thing on the wreck is the reason the wreck exists. Sell it and you have moved it one hull closer to the next crew. |

`MisreadsAs` is therefore `LifeSupportFailure` — and unlike the other misreads, this one is not a
mistake the wreck makes. It is the one the *captain* is invited to make.

### Build cost

Small, because the parts exist: one `WreckCause` member, its `Evidence`/`MisreadsAs`/`CauseHeadline`/
`ArtFile`, damage walls (the A* audit walks every cause automatically — a new cause is guarded the day
it lands), a vented-state preset for `Map.Venting.PrepareVenting`, the log text, and one canvas. The
node placement rule from §2 becomes: **always on this cause.**

---

## 9. The rules this feature keeps

- **The label never lies, and the game never nags.** No "are you sure?" in front of an honest legend.
- **Nerve is the currency and the dice are the engine.** Every reveal here is bought, never given.
- **The image is evidence, never the conclusion.** No caption tells you what the handlers are.
- **Deterministic.** Authored pool, seeded order, seeded resident — the same universe every reload.
- **You can always walk out.** The node is a field you choose to stand in. Nothing here happens to a
  captain who takes their salvage and leaves.
