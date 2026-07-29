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
  and the worst place to be forgotten in. It is also, precisely because the board does not list it, a room
  the captain **cannot vent** — so anything in there sits out every atmosphere decision they make.

That last one is the sharpest: the whole venting system silently does not apply to the one compartment you
have not found.

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
