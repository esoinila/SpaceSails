# The atmosphere board — pressure as a mechanic

*The valve mimic, the vacuum soak, the pump, the pressure locks, and the shuttle's own airlock. Issue
[#488]. Everything here came out of one afternoon's playtesting with the owner driving.*

---

## 1. The shape of it

The board is a **damage-control mimic**: the ship drawn from her own geometry (`WreckLayout`), every
compartment a switch. Owner: *"a map with named sections so if you don't remember the name of the room you
still know to vent the right place."* You point at the place.

What makes it a system rather than a dialog is that **pressure is real everywhere**: it decides what dies,
what opens, where you can walk, and whether anyone can be carried home.

## 2. Venting is not a kill button — the soak

> Owner, mid-playtest: *"there might be a counter on how long the room has been in vacuum … so it needs
> certain time for certain infestations."*

Pulling the handle opens the compartment and **starts a clock**. The vacuum does the killing.

| `Infestation` | Vacuum needed | What it is |
|---|---|---|
| `Motile` | 20 s | the pack — warm, moving, lungs |
| `Fibrous` | 75 s | the growth over the cargo racks; no lungs to lose |
| `Encysted` | 180 s | has done this before and is in no hurry |

Seeded per compartment off the wreck id, and **never revealed**: `Read` reports that something is alive,
never what. So "how long is long enough" is a judgement, not a lookup — which is the same law the life-sign
reading already ran on.

The counter shows **how long the room has been open and deliberately not how long it needs.** The second
number does not exist for the captain.

*Design consequence worth keeping:* the long soak has to outlast a captain's patience, or the mechanic
collapses back into a button with a delay. Pinned by `TheSoakIsLongEnoughThatYouHaveToGoAndDoSomethingElse`.

## 3. Four controls that price each other

| | Costs | Buys |
|---|---|---|
| 💨 **Blow** | the air, permanently | the room opens *now* |
| 🛢 **Pump down** | ~50 s, most of it in a hot corridor | the air goes to your tanks instead of the dark |
| 🫁 **Refill** | one of two charges | pressure back — **air comes back, nobody does** |
| 🔒 **Dog the hatch** | the walk to it | that room keeps what it has, whatever happens elsewhere |

### The pump, and why the rough mark matters

> Owner, from the lab bench: *"I used to pump vacuum chamber to be empty so that air can be collected
> instead of venting, when things have power."* And then, on the shape of it: *"NASA uses cryopumps as high
> vacuums and some mechanical pumps for rough vacuum … the rough vacuum probably already does 95% of the
> job or more, so the rest is not that significant in material savings."*

Modelled exactly as described, and the asymmetry is a free mechanic:

- **`PumpRoughSeconds` (18 s)** — the mechanical stage. Effectively all the air is recovered; **the charge
  is banked here.**
- **the tail to `PumpDownSeconds` (50 s)** — the pull down to a pressure that actually kills. Recovers
  nothing.

So there is a real decision *inside a machine cycle*: **take the air and go, or stand there through the tail
for the kill.** Stopping at the rough mark is not an abort, it is the whole saving — and the button
relabels itself to say so.

## 4. Pressure locks — the doors are held, not latched

> Owner, after walking straight into a compartment he had just blown: *"we need some kind of door locked due
> to vacuum feature … maybe a gauge that says the door is shut due to pressure difference."*

One atmosphere across a door is roughly ten tonnes. A vented compartment's doorway becomes **an actual
wall**, with a gauge card at it: needle hard over, `VACUUM — that side / AIR — this side`. *It is not
locked. It is LOADED.*

`DoorHeldByPressure` is one line — air on exactly one side — which is why a hull that has been open for
forty years has no locked doors anywhere, and the only door that fights you on
`VentedByOneOfTheirOwn` is the one room somebody kept air in. Which is the room the board is in.

**Two safety invariants, both pinned by tests**, because a walled doorway is dangerous:

1. A lock only ever forms on a *vented* compartment, and the board will not vent the room you are standing
   in ⇒ **you can never be sealed in.**
2. The equalisation valve is always available ⇒ **you can never be sealed out.**

## 5. One valve, one volume — the whole-ship vent

> Owner, on finding it himself: *"I kind of like that a lot since now just having a single vented space
> allows venting the ship from that pressure equalization valve, if the actual controls are too crowded by
> infestation."*

An open door is not a boundary, it is a hole. The spine and every compartment standing open to it are **one
volume**, so `EqualiseAt` empties all of it at once. A captain who cannot fight their way aft to the valve
board can still vent the whole hull from any door — the long way round, for free.

What survives is exactly what somebody dogged a hatch on. **The infestation has not read the ship's
manual**; it never closes a door behind itself, so door discipline is a tool only the captain holds.

### The price, and why it is one-way

The corridor **cannot be refilled**: a compartment is a room, the spine is the length of the ship, and the
away team's whole reserve is two rooms' worth. *Cracking a valve is free and irreversible; refilling a room
costs and is reversible.* That asymmetry is the only thing that makes the choice at the door weigh
anything.

And equalising **kills survivors behind open doors** (the warning on the button says so), and a vacuum
corridor is one nobody can be carried out through. Which is what makes sealing the room you *think* holds
someone a real, learnable play.

## 6. The shuttle's own lock

> Owner: *"Let's keep the shuttle door locked in such a way that we don't vent our own shuttle by accident.
> Also we don't want any uninvited infestations going there … if our shuttle has an airlock then that could
> match the pressure outside first."*

`WreckLayout.ShuttleLockX` (x = 21) is a bulkhead across the spine with a 3-unit passage — aft of the
shuttle station, forward of the spawn, so the away team lands having already come through it. Two jobs:

- **It cycles rather than refuses.** Going home always works: the lock matches whatever the hull reads
  before the outer door moves, so the shuttle's air is never exposed. Crack every valve aboard and the boat
  does not notice.
- **It is crew-only** — the same rule the ship's tube runs on. The bulkhead has a passage in it, so walls
  alone would let the pack walk it exactly like the captain does; the rule that stops them is explicit in
  `StepReevers`. *It can reach the door. It cannot open the door.*

That last one also gives the fight a **rear**: the lane between the lock and the room you are emptying is
defensible with a guaranteed exit behind it.

## 7. Why it composes

Owner, on the finished loop: *"I love that pressure waiting in a hot spot with round counts dropping :-D"*

- The pump makes the fight **optional**: blow and leave poorer, or pump and be committed to the corridor.
- The sentry becomes a **timer you spend** rather than a wall — its rounds buy pump-seconds.
- The lock means the lane has a rear, so standing there is a decision rather than a gamble.

The clocks are therefore rendered **on the HUD, not only on the board** — you have to be able to read the
pump and the soak from out in the corridor while the sentry burns down, or the tension has nowhere to live.

## 8. Rules kept

- **The instrument never tells you what is alive in there.** Not before, not during, not after.
- **Air comes back; nobody does.** Refilling never undoes a consequence, only a pressure.
- **Never stranded, never trapped.** Both directions are pinned by tests, not by care.
- **Deterministic.** Kinds and survivors are seeded off the wreck; a reload cannot re-roll them into
  something easier.

[#488]: https://github.com/esoinila/SpaceSails/issues/488
