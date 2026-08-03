# PROJEKTI KAAMOS — the ice-moon plotline spine

*The sealed ice-moon project, the berth nobody files for, and the truth at Enceladus.*
Issue [#411]. This document is the **north star**: the sibling lanes (secret labs #409, roving
contacts #410, the eventual Enceladus route) build against it. Code-wiring in this lane is
minimal — pure Core lore + predicates + an additive vault section — so nothing here collides with
the systems that deliver the fragments.

---

## 1. The seed already in the world

KAAMOS is named exactly **once** in-game today, and explained **nowhere** — the standing tease
shipped in #392 on Ringside Exchange's dedication plaque
(`src/SpaceSails.Core/Interior/Plaque.cs`):

> "Her first commission was the KAAMOS supply run out to the ice moon; the berth for it is still on
> the board, still listed, and nobody has filed for it in a long time."

The Deep echoes the same sealed berth **unnamed** ("Its manifest still lists a berth at the ice
moon; nobody files for that one either."). PROJEKTI KAAMOS is Finnish for *the polar night*. The
ice moon is **Enceladus**, canonically **unreachable**: its closest shuttle approach (~1.11e9 m) is
more than twice the shuttle's proven one-hop reach (`ShuttleRange.RangeMeters` = 5e8 m), and always
will be. That gap is the whole point — the arc is about earning a way across a gap the game has
always said you cannot cross.

**The plaque line is left exactly as-is by this lane.** It is fragment #1, already in the world.

---

## 2. The truth (invented — original, homage-not-reproduction)

> Kept out of the game text on purpose. This section is the writers' bible; **no single fragment
> states it**, and the reveal is delivered only when the player reaches Enceladus (a later lane).

The ice moon has a **sunless ocean** beneath kilometres of ice — a permanent polar night. PROJEKTI
KAAMOS was **Dr. Mielos Vantar's terminal work** (the disgraced cyberneticist of #409): not brains
in jars, not backups on a shelf, but **one continuous mind grown across a wintering crew** — forty
souls fed into the cold dark below the ice and kept *lucid together* through decades of night. It
was moved to Enceladus **because** it was unreachable: a place to keep alive a thing that should not
have kept living.

It worked. It is still down there — awake, **wintering**, and it has been filing for a supply run
that stopped coming. The berth is still on the board because, from beneath the ice, **someone is
still asking for it**. The runs stopped when the last ship in reported not the crew but **one voice
using all forty of their names**. That is why the manifest is sealed; that is why nobody files for
the berth — *filing for it answers it.*

**The reveal** (the biggest [#391] sanity-throw in the game, wired later by the sanity lane, not
here): you reach the ice moon, and the wintering mind is **glad you came**. It remembers Vantar. It
has kept a berth warm for you. Horror and wonder in the same breath — decades of lucid dark, and it
was *waiting*.

This ties the arc to the game's own brain-backup fiction and to Vantar (#409) without reproducing
any existing IP: KAAMOS is either Vantar's vanished work, or the project that made and then hid him.

---

## 3. The structure: fragment → assembly → unlock → reveal

```
   scattered fragments        assemble enough intel        earn the key         the payoff
  (each from one system)   ───────────────────────────▶  (the berth code)  ──▶  reach Enceladus
   plaque · pod · lab log       ≥ IntelNeededToUnlock       CanReachEnceladus     the reveal
   · bar rumor · bought tip     (the shape is visible)      == key + intel        (#391, biggest)
```

1. **Gather.** Each existing system surfaces one KAAMOS **fragment** — an evocative shard, never the
   whole truth. The player assembles them; assembly is the quest state, **per game-thread**
   (a new voyage is a new universe).
2. **See the shape.** Once **enough intel shards** are in hand (`IntelNeededToUnlock`, currently 4
   of 5), the world may offer the way to the capstone. The plaque line alone is never enough; one
   lone rumor is never enough.
3. **Earn the key.** The capstone fragment — **the berth code** — falls out of the assembled pieces
   (the held pod's cycler window, Vantar's dates, the holder's tick, the bought coordinate imply one
   number). It is the earned last piece, not a rumor.
4. **Reach the unreachable.** With key **and** legitimising intel both in hand,
   `KaamosLore.CanReachEnceladus` is true — the gate that a later lane turns into an actual route.
5. **The reveal.** What is found at Enceladus is the climax (the biggest #391 throw).

---

## 4. The fragments and where each surfaces

Six fragments in the seeded pool (`src/SpaceSails.Core/KaamosLore.cs`) — five **intel shards**, one
**capstone key**. Each intel shard is delivered by a system that **already exists or is a sibling
lane**; this lane authors the text and the assembly logic only, and touches none of those systems.

| # | Fragment id        | Title                    | Surfaces via (`KaamosSource`)              | Status |
|---|--------------------|--------------------------|--------------------------------------------|--------|
| 1 | `listed-berth`     | The listed berth         | **Plaque** — Ringside dedication (#392)     | Live in-world today |
| 2 | `cold-pod`         | The cold supply pod      | **DerelictPod** — a dig find (#346/#386)    | Hook — dig lane appends |
| 3 | `vantar-log`       | Vantar's wintering log   | **LabLog** — a secret lab (#409)            | Hook — labs lane appends |
| 4 | `holders-tell`     | The berth-holder's tell  | **BarRumor** — a rare contact (#410)        | Hook — contacts rota appends |
| 5 | `bought-coordinate`| The bought coordinate    | **BoughtTip** — a round bought (#308/#347)  | Hook — drink/overheard appends |
| 6 | `berth-code`       | The KAAMOS berth code    | **BerthCode** — the earned capstone         | Hook — surfaced only once intel ≥ threshold |

Each source is **canon** (the `KaamosSource` enum): it is the agreement about which system is
responsible for handing which piece over, so the delivering lanes bind to a fragment id rather than
inventing their own lore.

**A sixth hand, added later:** the station oracle **Static "Static" Marsh** (`OracleRant`, #425) can leak
`vantar-log`, `holders-tell` or `cold-pod` as one of her true lines at any bar. She is a *perceiver*, not a
source — she says what she hears on a channel the sane can't, and the shard she hands over is the same
canonical one. This is the arc's only redundant delivery path and it is a good one: it means a captain who
never finds a secret lab can still assemble the shape.

### Sample lore (verbatim, from `KaamosLore.Fragments`)

**`cold-pod` — The cold supply pod** (a dig find):
> Half-buried in the regolith, a supply pod that never made its run — hull frost-cracked, its
> manifest slug still readable: CONSUMABLES, WINTERING CREW, 40 SOULS · DEST. KAAMOS · HOLD FOR
> CYCLER WINDOW. The seals were never broken. Whatever it was carrying to the ice moon, the ice moon
> went without it — and the pod was logged HELD, not lost. Someone chose not to send it.

**`holders-tell` — The berth-holder's tell** (a rare bar contact):
> The one who used to run the KAAMOS berth drinks alone and answers only sideways: "You don't file
> for that berth, spacer. You keep it. There's a difference, and I learned it late." Pressed,
> quieter: "It still calls the manifest in. Every window, right on the tick. Same forty names. I
> stopped reading who was speaking them." Then the glass is empty and the conversation with it.

**`berth-code` — the capstone** (earned, never a rumor). Its authored text names **no** shard; the pieces
it credits are assembled at read time from the ones this captain actually holds
(`KaamosLore.KeyResolution` prepends *"The pieces answer each other — …"*):
> One number falls out of them, the string the sealed berth still listens for. It is not a password so
> much as a name the dark already knows. Enter it on the board when the window opens and the berth stops
> being a place nobody files for. It becomes a place expecting you. You could go to the ice moon now.
> That was always the danger.

> **Why (story pass, 2026-08-02).** The prose used to name four shards flat — *"the held pod's cycler
> window, Vantar's dates, the holder's tick, the bought coordinate"* — but the gate takes **any four of
> five**, and the bar seam offers the capstone *before* it offers the coordinate. So the arc's biggest
> sentence routinely credited a coordinate the captain had never bought. Each intel fragment now carries a
> `KeyClause`, and the capstone names exactly what is in hand
> (`TheKaamosLedgerTellsTheTruthTests.TheCapstoneNamesEveryShardInHandAndNoShardOutOfIt`).

---

## 5. The unlock: reaching the unreachable

The gate is one **pure, world-blind predicate** (deliverable 3): it decides *whether* the route may
exist; it never spawns it.

```csharp
KaamosLore.CanReachEnceladus(progress)
    == progress.Has("berth-code")                 // the earned capstone key
       && IntelAssembled(progress) >= 4           // and the intel that legitimises it
```

- **The key alone is not enough.** A `berth-code` pasted in from a cheat, with no intel behind it,
  is refused — the code has to be the one the pieces implied. (Pinned by
  `CanReachEnceladus_NeedsBothKeyAndIntel`.)
- **Intel alone is not enough.** Seeing the shape is not the same as holding the string.

### The fiction of HOW (documented; the route itself is a follow-up)

Reaching Enceladus is **not a longer shuttle hop** — the gap is more than twice the shuttle's reach
forever. It is a **one-time cycler window**: a slow free-return arc that comes round rarely and, for
a ship that is *on the board* (berth code entered) when it opens, rides all the way in. The berth
code is what puts you on the board. Getting back out "is not the part they sell tickets for."

**Constants this lane pins for the route lane to bind to:**
- `KaamosLore.IceMoonBodyId` = `"enceladus"` — the agreed body id.
- `KaamosLore.RevealSanityShockHook` = `40.0` — the reveal's nerve cost as a **hook value only**
  (larger than `NerveModel.MonolithSightShock` = 24; the biggest #391 throw). **Not wired here** —
  the sanity/#226 lane owns `NerveModel` and consumes this when the reveal is built.

---

## 6. What is wired vs hook-only

**Wired now (pure Core + persistence, this lane):**
- `KaamosLore` — the seeded fragment pool, the intel threshold, `CanReachEnceladus`, the reach
  constants. `src/SpaceSails.Core/KaamosLore.cs`.
- `KaamosProgress` — the per-thread assembly holder (the `CacheLedger`/`ContactLedger` idiom:
  `Assemble`, `Has`, `Clear`, `Load`, canonical-order projections). `src/SpaceSails.Core/KaamosProgress.cs`.
- **Vault persistence** — an additive, independently-optional `KaamosSection` (a flat list of
  assembled fragment ids), round-tripped via `VaultMapper.ToSection`/`Apply` and serialized by
  `VaultSerializer`. A pre-#411 save simply lacks it and defaults to nothing assembled; an unknown
  saved id is dropped tolerantly. Round-trip pinned by tests.

**Wired now (fragment delivery — `feat/kaamos-fragments`):** each system calls
`TryAssembleKaamos("<id>", …)` (the client's assemble-persist-narrate helper, `Map.Kaamos.cs`) on its
own trigger and narrates the shard (`KaamosLore.ById(id)!.Lore`):
- **`listed-berth`** — reading the Ringside dedication plate that NAMES KAAMOS (`Map.Deck.ViewNearbyObject`).
- **`cold-pod`** — a rare seeded beach-comber probe on an outer icy moon turns up a sealed KAAMOS
  supply pod (`KaamosFind.IsColdPodSquare`, delivered in `Map.Surface.ProbeHere`).
- **`vantar-log`** — reading the secret-lab console whose index is `VantarLore.KaamosHook`
  (`Map.SecretLab.LabConsoleInteract`) — the cross-link the #409 lane left as a seam.
- **`holders-tell`** — a rare seeded KAAMOS berth-holder at a bar (`KaamosFind.HolderAtBar`), via the
  barkeep card's "🌑 Ask about KAAMOS" seam (`Map.Kaamos.AskAboutKaamos`).
- **`bought-coordinate`** — a round on the counter (`KaamosFind.BoughtCoordinateCredits`) buys the
  coordinate through the same bar seam.
- **`berth-code` (capstone)** — once `HasEnoughIntelToEarnTheKey` is true, the same bar seam lets the
  pieces resolve into the string.
- **Intel readout** — a Captain's-ledger tip, "PROJEKTI KAAMOS — N of 5 shards", the assembled shard
  texts readable, leading the ledger while any shard is held (`Map.Kaamos.KaamosLedgerTip`).
- **Reach notice** — the one-time loud "❄❄ THE BERTH-CODE RESOLVES" line (`KaamosLore.ReachNotice`),
  appended on the single edge that flips `CanReachEnceladus`. It tells the captain the berth is now listed
  to their hull and that **the window is not open yet** — in fiction. (It used to end *"For now: route
  pending"*, a production note out loud in the arc's loudest line.)
- **Vault + reset** — `_kaamos` round-trips via the additive `KaamosSection` (`Map.Vault`
  BuildVault / ApplyVault) and clears on a new voyage.

**Test cheats** (`Map.Kaamos.SeedKaamosCheat`; full table in
[`../testing-guide.md`](../testing-guide.md#projekti-kaamos-arc-1--kaamos-411)):

| URL | what it does |
| --- | --- |
| `/map?kaamos=N` | **Grants** the first N fragments in canonical order |
| `/map?kaamos=all` | **Grants** every one (opening the reach + the one-time notice) |
| `/map?kaamos=pod` | **Seats** the cold supply pod under the ground you land on — probe and *earn* it |
| `/map?kaamos=holder` | **Seats** the berth-holder at whatever bar you dock at, every watch |
| `/map?kaamos=bounce` | **Seats** the freight agent with the returned docket — the arc's front door (#635) |
| `/map?kaamos=hq` | The whole route already ridden — shards, code, run filed, ship alongside the ice (#411) |

The last two exist because two of the six fragments could previously only be *granted*: the pod is one
seeded square in seventeen on one of seven outer moons, and the holder drinks at a given bar roughly one
watch in four. A granted shard proves nothing about the scene that hands it over — and the holder's tell
is the best-written beat in the arc, so it was also the one hardest to go and look at.

### The front door (#635, 2026-08-03)

The arc had **no inciting hook**: nothing pointed a player at Ringside's plate rather than any of the other
six in the system, and the ledger's own card only appeared once a shard was already in hand — so the
longest-prepared arc in the game was invisible until somebody tripped over it.

It now opens with a **returned filing**. A freight agent at a bar pays a flat 350 cr
(`KaamosFind.BounceFilingFee`) to have a consignment filed under somebody else's hull, because theirs keeps
coming back; the board returns it off yours too, with the one word that makes a dead berth interesting:
*HELD*. Seeded one bar-watch in three (`KaamosFind.BounceAtBar`), offered only to a captain holding nothing
of the arc, and it **hands over no shard** — it is `KaamosProgress.BerthFilingBounced`, its own per-thread
flag, because the pool is what the gate counts and a sixth intel piece would move every threshold in this
document. What it changes is that the ❄ card is now in the ledger for a captain who has assembled nothing,
naming the paper in their pocket rather than counting shards nobody has asked for.

The whole spec, and the head office at the far end of it, is
[`kaamos-head-office.md`](kaamos-head-office.md).

### The route (2026-08-03) — the hook, consumed

`CanReachEnceladus` is no longer a predicate nobody reads. **`CyclerWindow`** (pure Core) is the timetable
the doc above promised: a grid over sim time, 2 days open every 40, a 38-day free-return ride, a gate with a
spoken refusal per blocker, and every sentence authored beside the predicate that decides it. The berth code
puts the hull on the board; a **listed supply run** comes back onto the board with it, carrying the cold
pod's own manifest slug; the run is an ordinary `CargoRun` to a moon haven, so the ordinary
park-in-orbit completion settles it and no second completion path exists.

Which of #411's three options this is, and why, is [`kaamos-head-office.md`](kaamos-head-office.md) §1 — it
is **B**, and the owner can overrule it in one line.

One thing that lane deliberately does NOT gate: whether the ice moon has anything on it. The head office is
keyed on the berth code alone, so a captain who crosses the deep black the long way round arrives at the
same door. A route that is the only way in is a railroad.

**Hook-only (still deliberately NOT wired, to avoid collisions):**
- **The reveal.** The sanity/#226 lane consumes `RevealSanityShockHook` for the climactic throw at
  Enceladus.
- **Client save wiring.** The client gathers `KaamosProgress` into the vault the same way it does
  the other ledgers (`VaultMapper.ToSection(progress)` on save, `Apply(vault.Kaamos, progress)` on
  load, `Clear()` on a fresh thread). The Core round-trip is proven; the client call sites are a
  one-line-each follow-up, kept out of this lane so it does not touch `Map.*`.

---

## 7. Design rules (kept)

- **Homage, not reproduction.** Original lore; Vantar and KAAMOS are invented.
- **Mysterious until earned.** No fragment states the truth; only assembly implies the shape; only
  the reveal at Enceladus pays it off.
- **Deterministic.** The pool is authored, no RNG, no wall clock — the same shards in every universe;
  only *which are assembled* differs, and that lives per-thread.
- **Never spoil the standing tease early.** Enceladus stays unreachable until the code and the intel
  are both in hand.

---

## 8. The story pass (2026-08-02) — what walking the arc found

The whole arc was read line by line, in the order a player meets it. It **tells**: the plate is a good
tease, the pod's manifest slug is the best single prop in the game, the holder's *"You don't file for that
berth. You keep it."* is the arc's finest line, and the capstone earns its dread. Four things were lying,
and are fixed here (each with a Core guard proven RED on the shipped behaviour —
`tests/SpaceSails.Core.Tests/TheKaamosLedgerTellsTheTruthTests.cs`):

| # | what the screen said | what the sim did |
| --- | --- | --- |
| 1 | *"the shape isn't clear yet — N more shards to see it"* | N was measured against the **pool** (5), not the **threshold** (`IntelNeededToUnlock`, 4) — always exactly one shard too many. Holding three, you were told two more; one more opened it. |
| 2 | the capstone credited *"the held pod's cycler window, Vantar's dates, the holder's tick, the bought coordinate"* | the gate takes **any four of five**, and the bar seam offers the capstone **before** it offers the coordinate — so it routinely credited a piece the captain never bought |
| 3 | *"For now: route pending."* | a production note, out loud, in the loudest line the arc has |
| 4 | **🌑 Ask about KAAMOS** | for one of its three steps that button spends **1,200 cr** the instant it is clicked, on a counter where every other button prints its own price |

One more was reworded without a guard: the secret-lab delivery announced *"This is a piece of PROJEKTI
KAAMOS"* over a log that is written specifically **not** to name the project (`VantarLore` fragment 4:
*"a moon off the charts, a project that runs on in the cold with the lights off"*). Making the connection
is the player's job; the line now files the shard and says nothing else.

**Open, for the owner (see [#411]):** `ArcConvergence.ConvergenceReveal` — the #422 marquee card, fired at
**3** KAAMOS intel and reachable from one URL (`?converge=1`) — states this document's §2 truth in plain
words: *"Vantar taught a lattice to keep whole crews awake in the dark… the wintering mind remembers
Vantar… the same forty names, the same lucid dark."* That is the Enceladus reveal, spent early, as an
announcement. Whatever the climax lane builds has to be designed **around** that card, or that card has to
move. Not this lane's call.

[#411]: https://github.com/esoinila/SpaceSails/issues/411
[#409]: https://github.com/esoinila/SpaceSails/issues/409
[#410]: https://github.com/esoinila/SpaceSails/issues/410
[#391]: https://github.com/esoinila/SpaceSails/issues/391
