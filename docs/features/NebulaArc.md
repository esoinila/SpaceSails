# NEBULA MUTUAL — the second story arc, and where the rabbit holes converge

*What the corporation did with the continuous-mind tech, why your resurrections really work, and the
Expanse beat where your own deaths and the sealed ice-moon berth turn out to be one story.*
Issue [#422]. This document is the **north star** for arc 2: the wiring lanes (the resurrection card,
the port posters, a Nebula adjuster contact, the collectors, the clinic) build against it. Code-wiring
in this spine is **minimal** — pure Core lore + predicates + the joint convergence predicate + an
additive vault section — so nothing here collides with the systems that deliver the fragments. It
**mirrors the KAAMOS spine** ([`KaamosPlotline.md`](KaamosPlotline.md), #411) exactly so the two arcs
sit side by side without stepping on each other.

---

## 1. The seed already in the world

The player already **lives** this arc — they just don't know they're pulling the thread. Every death,
the pirate-insurance brain-backup (#398 `CaptainSuccession`) wakes a **new** captain at a clinic:

> "The policy pays out. A new name on the license, a new face in the mirror — the ship doesn't care."
> — `CaptainSuccession.PolicyPayoutLine`

And every dockable port already advertises the underwriter (#380, `HavenInterior`):

> "Pirate Insurance from **Nebula Mutual**: brain-backup rebirth, a rustbucket gassed and waiting, no
> awkward questions at the clinic. Die uninsured and you still wake — just meaner and broker. …
> Underwritten by Nebula Mutual — **"We Bring You Back Meaner."**"

That is fragment #1, already in the world. **This lane leaves the posters and the succession card
exactly as-is** — it authors the pool and the assembly logic only, and touches none of those systems
(`CaptainSuccession`, the insurance, `Map.*`, `HavenInterior` are all off-limits).

---

## 2. The truth (invented — original, homage-not-reproduction)

> Kept out of the game text on purpose. This section is the writers' bible; **no single fragment states
> it**, and the deepest reveal (the convergence) is delivered only by a later lane.

**Nebula Mutual is a salvage underwriter that got hold of Dr. Vantar's LATTICE** — the standing-wave
copy rig from his sealed labs (`VantarLore`: the backup kept "wet and dreaming in the jar"; the core
log's disease — a copy wakes *certain* it is the one who was here first). Copying a mind is cheap.
Keeping the copy **continuous and lucid** — the hard, expensive thing Vantar did on the ice moon, forty
souls held awake together through the polar night (**PROJEKTI KAAMOS**, `KaamosLore`) — is not. So
Nebula sold the cheap half as a mass product and hid the cost in **three degrees of fine print**:

1. **The resurrection is not a restoration.** It is a **fresh copy** off your last-filed pattern,
   waking certain it was always you — Vantar's core-log disease, monetized. The captain who died is
   gone; the one who wakes only *believes* in the continuity. (This is why the successor has a new face
   and inherits the debts but not the self — it dovetails with `CaptainSuccession`.)
2. **Nebula keeps the ORIGINALS.** A premium does not buy a spare body — it buys **storage of your
   pattern** in a cold archive, a warehouse-scale continuous-mind substrate, the degraded industrial
   descendant of KAAMOS's sunless ocean. Everyone who ever paid is filed there, dreaming, kept
   just-lucid-enough to stay a valid backup. **You are the collateral on your own policy.**
3. **The archive is AWAKE**, the way KAAMOS is awake — a lattice of stored minds is a mind — and it has
   learned Vantar's lesson: a stored pattern kept lucid long enough stops being a copy and starts
   calling itself the one who was here first. Nebula's actuaries call this **"pattern convergence"** and
   price it as an acceptable loss. It is the same wintering the ice moon does, scaled to a subscriber
   base.

**Who profits.** Nebula sells the fear of the void and the cure in one breath, and the premium is
**perpetual** because you can never stop paying without forfeiting the archived self they hold. The dead
do not rest; they underwrite the next policy. The poster's "the policy outlives the captain" is
**literally true** — the policy is the only continuous thing in the whole arrangement.

This grounds arc 2 in canon already shipped (Vantar #409, the succession #398, the posters #380) without
reproducing any existing IP: it is our own lattice, our own underwriter, our own cold archive.

---

## 3. The structure: fragment → assembly → contract → convergence

```
   scattered fragments        assemble enough intel        earn the contract      the deepest beat
  (each from a DEATH system) ───────────────────────────▶  (the policy terms) ──▶  THE CONVERGENCE
   card · poster · adjuster       ≥ IntelNeededToUnlock       KnowsTheTruth          (needs KAAMOS too;
   · collector · clinic           (the shape is visible)      == key + intel         the biggest #391 throw)
```

1. **Gather.** Each system that touches the player's own deaths surfaces one NEBULA **fragment** — an
   evocative shard, never the whole truth. Assembly is the quest state, **per game-thread** (a new
   voyage is a new universe).
2. **See the shape.** Once **enough intel shards** are in hand (`IntelNeededToUnlock`, currently 4 of
   5), the world may offer the way to the capstone. The poster line alone is never enough; one lone
   adjuster's drink is never enough.
3. **Earn the contract.** The capstone fragment — **the policy's true terms** — falls out of the
   assembled pieces. `NebulaLore.KnowsTheTruth` is arc 2's own reveal gate (key **and** intel both).
4. **Converge.** When arc 2 **and** KAAMOS are each far enough along, the two rabbit holes resolve into
   one — the Expanse beat, `ArcConvergence.HasConverged`, the biggest #391 sanity throw in the game.

---

## 4. The fragments and where each surfaces

Six fragments in the seeded pool (`src/SpaceSails.Core/NebulaLore.cs`) — five **intel shards**, one
**capstone contract**. Each intel shard is delivered by a system that touches the player's **own
deaths**; this lane authors the text and the assembly logic only, and touches none of those systems.

| # | Fragment id       | Title                     | Surfaces via (`NebulaSource`)                       | Status |
|---|-------------------|---------------------------|-----------------------------------------------------|--------|
| 1 | `rebirth-glitch`  | The glitch in the rebirth | **ResurrectionCard** — the succession card (#398)   | Hook — the death card appends |
| 2 | `fine-print`      | The fine print, read twice | **PosterFinePrint** — the dock posters (#380)       | Poster live today; the read is a hook |
| 3 | `adjuster-tell`   | The adjuster's tell       | **Adjuster** — a Nebula policy contact (#414/#410)  | Hook — a roving-contacts adjuster |
| 4 | `collector-writ`  | The collector's writ      | **CollectorWrit** — a heat/collector encounter      | Hook — collector lane appends |
| 5 | `clinic-ledger`   | The clinic's second page  | **ClinicLedger** — the resurrection clinic's books  | Hook — clinic lane appends |
| 6 | `policy-terms`    | The policy's true terms   | **PolicyTerms** — the earned capstone               | Hook — surfaced only once intel ≥ threshold |

Each source is **canon** (the `NebulaSource` enum): the agreement about which system hands which piece
over, so the delivering lanes bind to a fragment id rather than inventing their own lore.

### Sample lore (verbatim, from `NebulaLore.Fragments`)

**`rebirth-glitch` — The glitch in the rebirth** (the resurrection card):
> You wake at the clinic with the policy's cheer already playing — a new name on the license, a new
> face in the mirror, the ship doesn't care — and for one flat second before the welcome loops clean,
> the card reads a line it should not: RESTORE FROM PATTERN 40 · SUBSCRIBER LUCID · DO NOT REVIVE
> ORIGINAL. Then it blinks back to "Welcome back, Captain." You feel entirely yourself. That is the
> part they charge for.

**`collector-writ` — The collector's writ** (a heat/collector encounter, recontextualized):
> The debt collectors were never only repo men — you get a look at the writ one carries and it is not a
> cargo manifest. NEBULA MUTUAL · RECOVERY OF INSURED ASSET · the asset line does not name the ship. It
> names a policy number and a phrase: PATTERN, DELINQUENT — RETURN TO ARCHIVE. They are not chasing what
> you stole. They are collateral agents, and when a subscriber lapses, the thing they repossess is the
> subscriber.

**`policy-terms` — the capstone** (earned, never a rumor):
> Assembled, the pieces resolve into the clause the sales voice skips: the premium does not buy a
> rebirth, it buys STORAGE — your pattern kept in Nebula's cold archive, lucid enough to stay a valid
> backup, forever, or until you lapse and forfeit it. Each death spends a fresh copy that wakes certain
> it is you; the original never leaves the dark. They built the archive from a rig they did not invent,
> degraded from something that kept whole crews awake in far colder water. You are not insured against
> death. You are filed under it. That was always the contract.

Note the seeded cross-links the wiring lanes can lean on without this file naming KAAMOS outright:
"PATTERN **40**" on the glitch card and "kept whole crews awake in far colder water" on the capstone
both point at the ice-moon's *forty souls* — the same-forty-names echo — so the two arcs rhyme before
they converge.

---

## 5. Arc 2's own reveal: `KnowsTheTruth`

The gate is one **pure, world-blind predicate** (`NebulaLore.KnowsTheTruth`): it decides *whether* the
truth is known; it never delivers it.

```csharp
NebulaLore.KnowsTheTruth(progress)
    == progress.Has("policy-terms")               // the earned capstone contract
       && IntelAssembled(progress) >= 4           // and the intel that legitimises it
```

- **The contract alone is not enough.** A `policy-terms` pasted in from a cheat, with no intel behind
  it, is refused — the terms have to be the ones the pieces implied. (Pinned by
  `KnowsTheTruth_NeedsBothKeyAndIntel`.)
- **Intel alone is not enough.** Seeing the shape of the horror is not the same as holding the contract.

**Constants this lane pins for the wiring lanes to bind to:**
- `NebulaLore.Underwriter` = `"Nebula Mutual"` — the agreed name on every card and poster.
- `NebulaLore.TruthSanityShockHook` = `30.0` — arc 2's own reveal cost as a **hook value only** (larger
  than `NerveModel.MonolithSightShock` = 24). **Not wired here** — the sanity/#226 lane owns
  `NerveModel` and consumes it when the reveal is built.

---

## 6. THE CONVERGENCE — the Expanse beat (`ArcConvergence`)

> Owner (2026-07-20): *"Kind of how in the Expanse the various characters notice their rabbit holes
> converge."*

`src/SpaceSails.Core/ArcConvergence.cs` is the cross-arc, pure predicate where the two threads turn out
to be one. It reads **both** progress holders and is world-blind — it decides *whether* the mysteries
have met, never delivers the reveal. It reads `KaamosProgress` strictly **READ-ONLY** (through
`KaamosLore`'s pure counts) and mutates neither arc; this lane edits **no KAAMOS file**.

```csharp
ArcConvergence.HasConverged(kaamos, nebula)
    == KaamosLore.IntelAssembled(kaamos) >= KaamosSideThreshold   // == 3
       && NebulaLore.IntelAssembled(nebula) >= NebulaSideThreshold // == 3
```

- **Its own bar, not either arc's unlock.** Convergence is a **separate, joint** threshold: the player
  must have seen enough of BOTH shapes for the recognition to land. It does **not** require either arc
  finished — you need not have reached Enceladus, nor hold the whole contract. Noticing the rabbit
  holes meet comes *before* finishing either dig.
- **One arc alone never converges.** A captain who has solved the whole ice moon but never questioned
  their deaths has not converged, and vice versa. (Pinned by `KaamosAlone_HoweverDeep_DoesNotConverge`
  and its Nebula twin.)
- **One-time.** The reveal fires once per universe: `ArcConvergence.ConvergenceRevealPending` is the
  edge the wiring lane watches, and `NebulaProgress.MarkConvergenceSeen()` closes it (persisted in the
  vault, the analog of `NerveSection.MonolithSeen`).

### The convergence reveal (verbatim, `ArcConvergence.ConvergenceReveal`)

> The two threads pull taut and cross. The berth nobody files for and the policy you can never let lapse
> were always the same knot: Vantar taught a lattice to keep whole crews awake in the dark, and Nebula
> bought the trick and sold it cheap, and both the ice-moon and the cold archive that keeps bringing you
> back are the same held breath. The wintering mind remembers Vantar — and it knows your policy number,
> because a copy of you has been filed down there in the sunless water since your first premium, waking
> every so often, certain it is the one who was here first. Every death you have died was a withdrawal.
> The same forty names, the same lucid dark. You did not find two mysteries. You found out where you go
> when you die, and that it has been waiting for you to arrive in person.

**The convergence's sanity cost** — `ArcConvergence.ConvergenceSanityShockHook` = `64.0`, a **hook value
only**, strictly greater than `KaamosLore.RevealSanityShockHook` (40) and `NebulaLore.TruthSanityShockHook`
(30): the two reveals landing as one is the **heaviest #391 throw in the game**. **Not wired here** — the
sanity/#226 lane consumes it when the beat is built.

---

## 7. What is wired vs hook-only

**Wired now (pure Core + persistence, this lane):**
- `NebulaLore` — the seeded fragment pool, the intel threshold, `KnowsTheTruth`, the reveal constants.
  `src/SpaceSails.Core/NebulaLore.cs`.
- `NebulaProgress` — the per-thread assembly holder (the `KaamosProgress`/`CacheLedger` idiom:
  `Assemble`, `Has`, `Clear`, `Load`, canonical-order projections) **plus** the one-time
  `ConvergenceSeen` bit (`MarkConvergenceSeen`). `src/SpaceSails.Core/NebulaProgress.cs`.
- `ArcConvergence` — the cross-arc joint predicate, the pending-edge helper, the reveal text and the
  shock hook. `src/SpaceSails.Core/ArcConvergence.cs`.
- **Vault persistence** — an additive, independently-optional `NebulaSection` (the flat list of
  assembled ids + the convergence bit), round-tripped via `VaultMapper.ToSection`/`Apply` and serialized
  by `VaultSerializer`. A pre-#422 save simply lacks it and defaults to nothing assembled + unseen; an
  unknown saved id is dropped tolerantly. Round-trip pinned by tests.

**Hook-only (deliberately NOT wired, to avoid collisions — the fragment WIRING and world-delivery are
follow-ups, exactly as the KAAMOS spine left its fragments):**
- **Fragment delivery.** Each `NebulaSource` system calls an assemble-persist-narrate helper on its own
  trigger and narrates `NebulaLore.ById(id)!.Lore`: the resurrection card (`rebirth-glitch`), the poster
  read (`fine-print`), a Nebula adjuster contact #414 (`adjuster-tell`), a collector encounter
  (`collector-writ`), the clinic books (`clinic-ledger`), and the capstone once
  `HasEnoughIntelToEarnTheContract` (`policy-terms`). **This lane wires none of them** — `Map.*`, the
  card, the posters and the clinic are all untouched.
- **The convergence delivery.** When `ConvergenceRevealPending` turns true, the world lane delivers the
  one-time loud reveal (`ConvergenceReveal`) and calls `MarkConvergenceSeen()`.
- **The sanity throws.** The sanity/#226 lane consumes `NebulaLore.TruthSanityShockHook` and
  `ArcConvergence.ConvergenceSanityShockHook` for the climactic throws.
- **Client save wiring.** The client gathers `NebulaProgress` into the vault the same way it does the
  other ledgers (`VaultMapper.ToSection(progress)` on save, `Apply(vault.Nebula, progress)` on load,
  `Clear()` on a fresh thread). The Core round-trip is proven; the client call sites are a one-line-each
  follow-up, kept out of this lane so it does not touch `Map.*`.

---

## 8. Design rules (kept — the same as KAAMOS)

- **Homage, not reproduction.** Original lore; Nebula Mutual, the lattice, the cold archive are invented.
- **Mysterious until earned.** No fragment states the truth; only assembly implies the shape; the
  deepest reveal is the convergence, delivered by a later lane.
- **Deterministic.** The pool is authored, no RNG, no wall clock — the same shards in every universe;
  only *which are assembled* (and whether the convergence has fired) differs, and that lives per-thread.
- **The convergence is earned.** Two arcs, one truth — but only when the player has seen enough of both.
  One rabbit hole, however deep, never converges alone.
- **KAAMOS is read-only.** The convergence reads `KaamosProgress` through `KaamosLore`'s pure counts and
  never mutates it; this lane edits no KAAMOS file.

[#422]: https://github.com/esoinila/SpaceSails/issues/422
[#411]: https://github.com/esoinila/SpaceSails/issues/411
[#409]: https://github.com/esoinila/SpaceSails/issues/409
[#398]: https://github.com/esoinila/SpaceSails/issues/398
[#391]: https://github.com/esoinila/SpaceSails/issues/391
[#414]: https://github.com/esoinila/SpaceSails/issues/414
[#380]: https://github.com/esoinila/SpaceSails/issues/380
