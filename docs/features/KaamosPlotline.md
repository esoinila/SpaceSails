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

**`berth-code` — the capstone** (earned, never a rumor):
> Assembled, the pieces answer each other: the held pod's cycler window, Vantar's dates, the
> holder's tick, the bought coordinate — one number falls out of them, the string the sealed berth
> still listens for. It is not a password so much as a name the dark already knows. Enter it on the
> board when the window opens and the berth stops being a place nobody files for. It becomes a place
> expecting you. You could go to the ice moon now. That was always the danger.

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
- **Reach notice** — the one-time loud "THE BERTH-CODE RESOLVES — Enceladus can be reached" line,
  appended on the single edge that flips `CanReachEnceladus`; gates a "route pending" message only.
- **Vault + reset** — `_kaamos` round-trips via the additive `KaamosSection` (`Map.Vault`
  BuildVault / ApplyVault) and clears on a new voyage.

**Test cheat:** `/map?kaamos=N` assembles the first N fragments in canonical order;
`/map?kaamos=all` assembles every one (opening the reach). So the readout, its state transitions, and
the reach notice are all reachable without a playthrough (`Map.Kaamos.SeedKaamosCheat`).

**Hook-only (still deliberately NOT wired, to avoid collisions):**
- **The route.** When `CanReachEnceladus(progress)` turns true, the eventual route/scenario lane
  turns it into a navigable **cycler window** to `IceMoonBodyId` — the only place that touches the
  shuttle/scenario code. **This lane does not.**
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

[#411]: https://github.com/esoinila/SpaceSails/issues/411
[#409]: https://github.com/esoinila/SpaceSails/issues/409
[#410]: https://github.com/esoinila/SpaceSails/issues/410
[#391]: https://github.com/esoinila/SpaceSails/issues/391
