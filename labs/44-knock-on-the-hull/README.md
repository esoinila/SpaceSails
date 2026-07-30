# Lab 44 — Knock on the hull

> *"Sweeping could cost time… like pumping vacuum does… idea is to know where to do it… so some logic on
> selection based on map or other clues"*
> *"Also it might be noisy to say knock on walls etc."*
> — the owner, specifying the search mechanic in two messages

Three sentences, one design. A search that **costs time** makes *where* the decision instead of *whether*. A search
that makes **noise** means you cannot buy your way out of thinking, because on this hull noise is what wakes things
and what brings professionals down the corridor. So the question this lab exists to answer is the one the whole
mechanic rests on: **is believing the clue actually worth anything?**

Run it: `dotnet run --project labs/44-knock-on-the-hull/Lab44.csproj -c Release`

**Every number below came from that run.** Reaches, clocks and earshots come from `HullSounding`, so the lab and
the mechanic cannot drift apart.

---

## A — the hull, and the two gears

```
  hull outline -34…26 du x -9…9 du = 1080 square du

  gear          seconds   radius   per spot   earshot
  Knuckles           12        2       12.6        13
  Sounder             5        4       50.3        26
```

The two earshots are the wreck lane's **own** numbers (`QuietEarshot` 13, `LoudEarshot` 26), not new ones — a knock
is exactly as loud as dogging a hatch by hand, and the sounder is exactly as loud as running a pump. Nothing about
searching gets its own private acoustics.

## B — what a blind, hull-wide search costs

```
  gear           spots   standing still   in minutes
  Knuckles          86           1032 s        17.2 min
  Sounder           22            110 s         1.8 min
```

Standing-still time only — no walking between spots.

## C — what she hears while you do it

```
  gear          noises made   pack roused   sweeps alerted
  Knuckles               86             4                3
  Sounder                22             4                3
```

**FINDING 1 — the pack's noise cap is a PACING rule, not a budget, and blind searching walks straight through it.**
`NoiseRousesAtMost = 2` exists to stop *one* racket summoning the ship. It does nothing whatever about **22
rackets**: the whole authored pack of 4 is up after the second spot, and every sweeper aboard has walked to the
first. So a blind sounder search does not merely take 1.8 minutes — it takes 1.8 minutes and then kills you. **The
noise, not the clock, is what makes the deduction compulsory.**

## D — the map as the clue

```
  the SHIPPED wreck layout: 0 discrepancies
```

The right baseline: a wreck with nothing hidden in her must measure **clean**, or the clue cries wolf on every hull
in the game.

Move one bulkhead — NEAR HOLD four frames forward, which is what a void built into her aft end looks like on the
plans:

```
    DEEP HOLD runs 4 frames further aft than anything across the spine from it
      -> band x -15…-11 on the bottom side, 24 square du to search

  gear             blind   on the clue   speed-up   noise saved
  Knuckles          1032 s          24 s      43.0x          84 spots
  Sounder            110 s           5 s      22.0x          21 spots
```

**FINDING 2 — the clue is worth 22× and 21 loud events, so yes, it is a mechanic.** And it is readable *off the
mimic board*, because the board draws both rectangles from these very numbers. A captain who looks at the map
properly sees a room that runs further aft than the room opposite it. No console, no die, nobody telling them.

**FINDING 3 — and this lab caught the clue rule pointing at the wrong wall.** The first version measured each room
against the *total* overlap opposite it and then guessed where the shortfall sat. It guessed both the end and the
**side** wrong: it named `x −4…0` on the **top** side when the unaccounted space is `x −15…−11` on the **bottom**.
A clue that names the wrong wall is worse than no clue — a captain sounds it, hears solid, and stops believing the
instrument. The honest construction does not guess at all: subtract every opposite-side room from this one's run,
and whatever is left over is ship that exists on one side of the keel and not the other. The void is on the side
that is **missing** the room.

## E — is the ODD reading worth having?

Two strategies for a void somewhere along a band. **COVER**: tile the band with overlapping discs. **STRIDE**: step
by the odd band's own width and let the first non-Solid note close it in two more.

```
  gear          band     cover      stride    winner
  Knuckles        6         3 sp         4 sp    cover
  Knuckles       12         6 sp         4 sp    STRIDE
  Knuckles       24        12 sp         6 sp    STRIDE
  Knuckles       48        23 sp        10 sp    STRIDE
  Knuckles       60        29 sp        11 sp    STRIDE
  Sounder         6         1 sp         1 sp    tie
  Sounder        12         2 sp         4 sp    cover
  Sounder        24         3 sp         4 sp    cover
  Sounder        48         6 sp         6 sp    tie
  Sounder        60         8 sp         7 sp    STRIDE
```

**FINDING 4 — and it refutes what I expected.** I added the third reading believing it would make searching
converge in general. It does not. On the **sounder** at narrow bands it actively **loses** — a 4 du disc is already
big against a 24 du band, so tiling costs 3 spots where striding-and-closing costs 4.

The third answer only pays where the **reach is small against the distance** — which is precisely the **knuckles**
case (12 du band: 6 spots → 4; 60 du: 29 → 11), and precisely the case a captain is in when something is awake
aboard and the loud tool is not an option.

So `Reading.Odd` is neither decoration nor a general speed-up: **it is what makes the quiet gear usable.** Keep it,
and do not sell it as convergence.

---

## What changed because of this lab

1. **`Discrepancies` was rebuilt.** It named the wrong wall, on the wrong side of the keel. Caught here, before a
   single pixel — the same reason Lab 43 ran before anything was animated.
2. **The noise is the real cost, not the clock.** 22 loud events walks straight through a cap designed for one, so
   the mechanic's teeth are in `MakeNoiseAboard`, not in the stopwatch.
3. **`Reading.Odd` is justified narrowly and honestly** — the quiet gear's enabler, not a universal convergence,
   and the docstring now says so.
4. **A clean hull must measure clean**, and the shipped layout does: 0 discrepancies. Pinned as a law so the clue
   can never start crying wolf on every wreck in the game.
