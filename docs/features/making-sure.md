# Making sure — the third road, and four ways to end a ship

*Status: **designed, not built**. Owner's idea, 2026-07-29, at the end of the atmosphere session.*

> "we could have a self-destruct option in the ship, like reactor overload … in cases where we want to be
> sure, or if possible set course to Sun :-D"
> "or uninhabited surface"
> "burn it in some atmosphere :-D"
> "gen AI images of panels :-D maybe … with like couple options :-D"

---

## 1. The third road

The wreck lane forks two ways: **file the report** (a fee and a contact) or **strip her and say nothing**
(the whole value, hot). Both assume you want something *from* her.

This is the third: **you want her gone.** No fee, no cargo, no contact — you give up every credit on the
hull to be certain of something. Which makes it the only road on the card that costs you everything, and
therefore the only one that can mean something.

**When a captain reaches for it:**

- The vacuum is not enough. You blew the deep hold, you waited out the soak, and you *still* do not believe
  it. (The instrument has never once told you what was in there — see [atmosphere.md](atmosphere.md).)
- You found something aboard that should not exist, and filing it means somebody comes out to look at it.
  See [the-archive-node.md](the-archive-node.md).
- You read her paperwork, and the cleanest way to end a story is to end the ship that carries it. See
  [the-paperwork.md](the-paperwork.md).

That last one matters: **making sure is also the coverup move.** It is not automatically the moral choice —
it is the choice that leaves no evidence, and the game should never tell you which one you just made.

## 2. Four methods, and they are not the same choice

Each is gated by what killed her and what is nearby, which is free content: the taxonomy already exists.

| Method | Needs | Speed | Who ever knows |
|---|---|---|---|
| ☢ **Reactor overload** | a reactor that still works — *never* on `ReactorCascade`, she has already done this | minutes, on a timer | anybody in the volume. It is a flash and a shell |
| ☀ **Course to the Sun** | a drive that answers — never on `DriveFailure` | years | nobody, ever |
| 🪨 **Course to an uninhabited surface** | a drive, and a dead rock in reach | months | she is still *there*, under regolith |
| 🔥 **Burn her in an atmosphere** | a drive, and a body with air in reach | weeks | one long light in somebody's sky |

### ☢ Reactor overload — the one with a clock

The classic, and the only one that turns disposal into **a scene**: set it, and then get off her. The
corridor you walk back down is the one you have been fighting in all boarding, the shuttle lock is at the
far end of it, and the clock does not care.

It is also the only method that works on a hull whose drive is dead — so it is the method for a ship that
cannot be sent anywhere.

*It pairs with everything already built:* the pack does not cross the lock, the vented compartments are
still vacuum, and the doors you dogged are still dogged. Your own housekeeping is either a clear road out
or a maze you built for yourself.

### ☀ Course to the Sun — the cold-blooded one

No timer, no danger, no witnesses. You point her in, and in some number of years she stops existing. The
most *deniable* method in the game: there is no wreck to find, no debris field, no flash on anyone's
sensors, and no record anywhere except in your own log.

Expensive in delta-v, which is the honest constraint — falling into the Sun is one of the hardest things to
do in a solar system. It should need her tanks to be *good*, which most wrecks' are not, and that scarcity
is what keeps it special.

### 🪨 Course to an uninhabited surface — the cheap one

Far less delta-v: drop her on a dead rock. But **she is still there.** Somebody with a good enough track
and a shovel finds a debris field one day, and everything you were trying to end is in it — which makes
this the method that can *come back*, and the one an investigator would call reckless. It also puts
whatever was aboard her onto a surface, which for an infested hull is a genuinely bad idea and the game
should be willing to prove it later.

### 🔥 Burn her in an atmosphere — the clean one

Owner's, and the best of the four for one reason: **the game already has the machinery.** Aerobraking is a
shipped system (`Map.Aerobrake.cs`), and this is the same physics pointed the other way — not shedding
speed, but going in too steep on purpose.

Total, verifiable, and *visible*: a hull entering Venus or Titan or Jupiter is a light somebody sees. So it
is the honest opposite of the Sun — everyone knows something burned, nobody can ever say what. And unlike
the rock, nothing survives to be dug up.

## 3. The panel, and the art

> "gen AI images of panels :-D maybe … with like couple options :-D"

The scuttling controls are their own console — deliberately *not* the valve board. That board is damage
control, a thing every ship has. This is the other kind of panel: keyed, placarded, and unmistakably built
for one job.

- It lives with the machinery, aft, like the valves — and on a hull whose reactor is the method, standing
  at it means standing next to the thing you are about to overload.
- It offers **only the methods this hull can actually do**, and says plainly why the others are greyed:
  *"the drive does not answer"*, *"she has already done this"*, *"nothing in reach with air in it"*.
- Gen-AI canvases, a couple of them, tracked in `docs/art-manifest-panels.md` when built. House rule as
  ever: **no readable lettering** — the copy is ours, written in code. A placarded panel behind a wired
  cover; a keyway with two positions and a chain through it; a console with a dust-filled cover flipped up.

## 4. What it costs, and what it is worth

**No credits. Ever.** The moment this becomes a payout it stops being a decision and becomes an exploit —
and the whole fork already runs on "stripping always pays more *today*", so a third road that also pays
would flatten it.

What it can pay in:

- **Certainty.** The one thing neither of the other roads sells.
- **A consequence that does not happen.** This is the strongest, and it needs the world to be willing to
  *prove* it: an infested hull left drifting should be able to come back — as another wreck, as a
  contaminated dig, as a rumour on the wire about a crew that boarded something. A captain who burned her
  never sees the thing they prevented, which is exactly what prevention feels like.
- **Nerve.** Standing off and watching her go is worth pips back, and it is the only relief in the lane you
  cannot buy in a bar.
- **Nothing on the record.** Which is either integrity or a coverup, depending on facts the game does not
  get to judge.

## 5. Build order

1. **Core: `Scuttle`** — the four methods, their gates (by `WreckCause` and by what is in reach), the
   authored lines, and the reactor timer's constants. Pure and tested.
2. **The panel** — a `WreckScuttle` console aft, offering only what this hull can do and naming why not for
   the rest.
3. **The overload sequence** — a timer, the walk back, and the shuttle. Reuses the lock, the vented rooms
   and the doors the captain has already been managing, so it is a test of their own housekeeping.
4. **The dispatch methods** — Sun / surface / atmosphere as a set course, resolving after the away team is
   home. Aerobrake entry reuses `Map.Aerobrake.cs`.
5. **Art** — a couple of panel canvases.
6. **The consequence that does not happen** — the world hook. Biggest of these by far, and the one that
   makes the road mean anything; worth doing last and properly.

## 6. Rules kept

- **It never pays.** Certainty is the product.
- **The methods are gated by the wreck herself**, so the taxonomy keeps earning its keep.
- **The game never says which choice was right.** Making sure and covering up are the same button.
- **Deterministic**, seeded, and it never strands a captain: if a hull can be scuttled at all, there is a
  road off her.
