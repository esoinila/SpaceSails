# Lab 42 — One atmosphere, however many rooms it's standing in

> *"If we only want to evacuate it then the doors to it need to be sealed … but if we evacuate multiple
> spaces then doors between those do not need to be sealed. The only check we need is to make sure we don't
> evacuate a room by accident of leaving its door open."*
> — the owner, reading my pump rules back to me

I had written that as **three** rules — a per-room interlock, a separate special case for the corridor, and
a refusal if any hatch stood open. All three were the same rule wearing different clothes, and none of them
was quite true.

A pump does not empty a *room*. It empties the volume it is plumbed into, and that volume is however many
compartments are standing open to each other. So the question was never *"is this door shut"*. It is:

> **Which spaces am I about to empty, and did the captain mean all of them?**

Ask it that way and the three rules collapse into one connectivity query and one warning.

Run it: `dotnet run --project labs/42-one-atmosphere/Lab42.csproj -c Release`

**Every number below came from that run.**

---

## A — the graph, on two rooms and a corridor

```
  BRIDGE dogged, CREW SPACES dogged
      press BRIDGE      → BRIDGE
      press THE SPINE   → THE SPINE

  BRIDGE open  , CREW SPACES dogged
      press BRIDGE      → BRIDGE + THE SPINE
      press THE SPINE   → BRIDGE + THE SPINE

  BRIDGE open  , CREW SPACES open
      press BRIDGE      → BRIDGE + CREW SPACES + THE SPINE
      press THE SPINE   → BRIDGE + CREW SPACES + THE SPINE
```

A dogged hatch is its own little world. That is the whole point of dogging it.

### Why this is a search and not two `if`s

The owner's instinct was *"the UI of pumping should use like the A\* algorithm to check the doors"* — same
family. A\* searches for the **cheapest path**; this wants **everything reachable**, so it's A\* with the
heuristic and the cost thrown away: a breadth-first flood.

And on *this* hull the graph is a **star** — every compartment has exactly one hatch and it opens onto the
spine — so the fill terminates in one hop and could have been written as two `if`s.

It isn't, for his other reason: *"ideally making it general case now saves us trouble later."* Writing the
**answer** instead of the **search** is how a rule stops being true the day somebody cuts a hatch between two
holds. The door graph is one function (`OpenNeighbours`) and geometry flows through it.

---

## B — the accident he named, priced

Same button, same press, one hatch different.

| ship state | volume the pump empties | cost | banks |
|---|---|---|---|
| every hatch dogged | `FORWARD LOCKER` | 50s | 1 |
| that one hatch open | `FORWARD LOCKER + THE SPINE` | 140s | 3 |
| the forward half open | `BRIDGE + CREW SPACES + FORWARD LOCKER + THE SPINE` | 240s | 5 |

and the board says so before it starts:

> That pump is plumbed into more than FORWARD LOCKER: BRIDGE, CREW SPACES, THE SPINE are standing open to it
> and will go down with it. Dog the hatches you meant to keep.

**It never refuses.** Evacuating half a ship on purpose is a real play — the third row is a captain clearing
the bow deliberately. The accident being guarded against is a hatch *forgotten*, and you cannot tell those
two apart from the outside. So the rule doesn't try: it names what is going and lets the captain decide.

---

## C — every ship her hatches can make

Eight compartments with one hatch each is 2⁸ = **256 ships**. Small enough that the properties below aren't
sampled — they're *exhausted*.

```
      partition failures : 0
      symmetry failures  : 0
      isolation failures : 0
      largest atmosphere : 9 spaces (8 hatches open)
```

- **Partition** — every space is in the volume it names. If this failed, a pump could empty a room twice or
  leave one unaccounted for, and the charge arithmetic would drift with nothing erroring.
- **Symmetry** — press any member and the same atmosphere answers. *This is the property that makes it a
  volume rather than a direction*, and it's the first thing a hand-written pair of `if`s breaks.
- **Isolation** — a dogged hatch is alone, whatever the rest of her is doing.

### The distribution is the model checking its own arithmetic

```
         1 volume(s) :    1 of 256 configurations
         2 volume(s) :    8 of 256
         3 volume(s) :   28 of 256
         4 volume(s) :   56 of 256
         5 volume(s) :   70 of 256
         6 volume(s) :   56 of 256
         7 volume(s) :   28 of 256
         8 volume(s) :    8 of 256
         9 volume(s) :    1 of 256
```

That's C(8,k) exactly — and it must be, because *k* dogged hatches make *k* volumes of one room each, plus
one for whatever still breathes with the corridor. Nine volumes is the ship shut tight; one volume is every
hatch open and the whole hull breathing together. **If this distribution ever stops being binomial, the door
graph has grown an edge nobody meant to add.**

---

## D — what a volume costs, and the loop that closes

```
      one room          : 50s, banks 1
      the corridor      : 90s, banks 2
      the whole ship    : 490s in one run, banks 10

      pumped compartment by compartment, she banks : 10 charges
      flooding her whole hull back costs           : 10 charges
      the away team carries                        : 5 to start with
```

**The loop closes exactly.** You can put back precisely the air you banked — which is what makes the
equalisation valve a real decision instead of a shortcut: valve air is gone forever, pumped air is in the
tanks.

And note the two ways to empty her. Dogging every hatch first isn't only safer, it's **faster**: eight small
pumps run concurrently, while one 490-second pump runs end to end.

---

## E — the trap that hid a real bug

> *"I run out of air trying to fill the ship … I even had used the pumping on all so I should still have the
> air mostly plus the reserves … how can my refill be zero now with only 4 rooms refilled?"*

He should have had eight charges. The rough mark — the countdown value where the air lands in the tanks —
was one variable shared across every pump in a frame, overwritten whenever the corridor came up in the
enumeration. Here is why that was invisible:

```
      a room's pump counts down from : 50s
      and banks when it crosses      : 32s
      the corridor banks when it hits: 72s

      can a room ever reach the corridor's mark? NO
```

A room's **whole run starts below** the corridor's mark. So measuring a room against the corridor's number
doesn't throw, doesn't warn, doesn't log — it silently never fires. The room empties, the pump finishes, the
"pumped down" line prints, and nothing arrives in the tanks.

`HullVenting.PumpRoughMark(spine)` is a pure function of one bool now. Nothing can be left holding another
pump's number.

---

## What CI holds from this lab

`HullVentingTests`:

- a dogged hatch is its own atmosphere
- an open one joins the corridor and every other open room
- **reaching is symmetric**
- a bigger volume takes longer and pays more
- the board names every room a pump will reach beyond the one pressed
- the **256-ship sweep** — partition, symmetry, isolation
- the volume count is exactly one per dogged hatch plus one

## What this changed

1. **Three rules became one query.** The per-room interlock, the corridor's special case, and the
   open-hatch refusal were all asking the same badly-posed question.
2. **The refusal became a warning.** A rule that cannot tell a deliberate evacuation from a forgotten hatch
   should not pretend to — it should say what it is about to do.
3. **The pump runs on a volume**: one clock, one rough mark, one payout, all of it going to vacuum together,
   because it was one atmosphere the whole time.

## Provenance

`Probe.cs` calls `HullVenting.SharedAtmosphere` and `HullVenting.PumpJob` — the same Core functions the
valve board runs on, so the lab's verdict and the game's pumps are one thing.

The honest history: the collapse from three rules to one was not my idea. I wrote the three, shipped them,
and the owner corrected them in three sentences after using them for ten minutes. **The general case was
cheaper than the special cases it replaced** — which is the argument this lab exists to make.
