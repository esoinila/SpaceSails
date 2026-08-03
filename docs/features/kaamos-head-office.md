# The KAAMOS head office — the front door, the route, and the place at the end of it

*The build spec for issues [#635] (the arc has no front door) and [#411] (the climax), against the
owner's ruling of 2026-08-03, recorded in [`../worldbuilding-notes.md`](../worldbuilding-notes.md) §9.*

The writers' bible is [`KaamosPlotline.md`](KaamosPlotline.md) and it outranks this document on every
question of what is TRUE. This one answers what is BUILT.

> **The ruling this exists to implement, verbatim.** *"The KAAMOS destination is the HEAD of the
> organization. Not another outpost, not a bigger wintering camp: the place everything else answers to."*
> — *"As fancy as the secret labs."* — *"The Hive facilities are branch offices … HQ outclasses them, and
> it should outclass them **in the same vocabulary**, so a player who has crawled a Hive recognises the
> rank difference without being told it."*

---

## 0 · The one law this whole feature is built on

**The truth is never stated.** Not by a plate, not by a card, not by a console, not by a crew member,
not by a sensor. `KaamosPlotline.md` §2 is a writers' bible and it stays one. Everything below is
*evidence*: a plate that names a department, a room that is made up, a board with one line lit, a log
that files on the tick. The arithmetic is the player's, or it is nobody's.

This matters more here than anywhere else in the game, for a reason that is already filed:
**`ArcConvergence.ConvergenceReveal` (#422) already spends the bible's §2 truth in plain words**, at a
lower bar than this arc's own unlock, reachable from one URL. That is an open decision (recommendation B
pending the owner) and **this build does not resolve it and does not lean on it**. The head office is
designed so that it lands whether or not the player has seen that card:

| the player has… | what the head office is |
| --- | --- |
| never seen the convergence card | a place that is impossibly well-kept and has nobody in it, filing for a delivery |
| seen the convergence card | the same place, and they now know what the forty-first bed is for |

Neither reading is confirmed on screen. The convergence card's own last line — *"it has been waiting for
you to arrive in person"* — is the one place the two arcs touch, and arriving in person is exactly what
this feature is. It sets the appointment. The head office keeps it and says nothing.

---

## 1 · The route: which of #411's three options, and why

**Picked: option B — the berth code buys you a LISTED SUPPLY RUN, and the run rides the cycler window.**
*(Flagged as a design call: the owner ruled the DESTINATION, not the vehicle. #411's comment offers a
chartered one-way, a listed supply run, and a cycler berth. This is the middle one, with the cycler
window kept as the run's timetable rather than as a separate vehicle. One line overrules it.)*

Why this one:

- **The horror is in the paperwork, which is this game's best register.** The bible's own sentence is
  *"filing for it answers it"* — and option B is the only one where the player **literally files for it,
  at a desk, like any other job.** The dread is that the job is routine.
- **It does not end the run.** A one-way charter (option A) is castaway-shaped, and #525 is already a
  standing note that end states rot silently because almost nobody reaches them. A supply run has a
  return leg by definition: you are carrying consumables *there*, and the ship is yours.
- **It crosses the gap the arc promised.** Option C (the reveal arrives as news) never goes, and the
  arc's whole promise is arriving. The owner has now ruled the destination is a real place — which
  selects the route-happens family and rules C out as the ending.
- **It is mostly reuse.** The contract card, the accept path, the long-haul leg, the shuttle board, the
  landing, the `UndergroundComplex` generator and the lift all exist. The genuinely new Core is one
  small pure class (the window) and one facility variant.

### The shape, end to end

```
  the front door            the arc                    the key                the run                 the place
  ─────────────────         ───────────────────        ───────────────        ────────────────        ───────────────
  a docket that      →      five shards, four     →    berth-code        →    a KAAMOS SUPPLY   →     the head office
  the board returns         of them enough             resolves;              RUN on the board;       under the ice
  (#635, PR 1)              (shipped)                  the wire notices       the window opens        (PR 3+)
                                                       (#663, PR 2)           (PR 2)
```

1. **The front door (§2).** A freight agent pays you to try filing a consignment that keeps coming back.
   It bounces off your hull too. You keep the receipt. The arc now exists.
2. **The arc (shipped).** The plate, the pod, Vantar's log, the holder's tell, the bought coordinate.
3. **The key (shipped).** Four shards of five resolve the berth code; `CanReachEnceladus` flips.
4. **The run (PR 2).** The code puts your hull ON THE BOARD. A **KAAMOS SUPPLY RUN** is now a listed job:
   consumables, wintering crew, forty souls, destination the ice moon, held for cycler window. You accept
   it the way you accept any parcel. The window is a real sim-time instant; until it comes round there is
   nothing to do but hold the berth, which is what the arc has been saying all along. When it opens, the
   leg is a real crossing.
5. **The place (PR 3+).** You land. Under the ice there is a lift head, and under that there is the
   biggest building in the game.

### What the "unreachable" gap actually is, stated honestly

The bible says Enceladus is out of shuttle range and always will be, and that is true and unchanged:
`ShuttleRange.RangeMeters` is 5e8 m and the nearest berth (Ringside Exchange) is ~1.11e9 m from it. What
was never true is that a *ship* cannot go there — Enceladus has been a charted haven moon in `sol.json`
since the beginning, and a captain who flies out and parks in its orbit can already put boots on the ice.

**That is not a hole; it is the feature, and the fiction has always said so.** Nobody is stopping you
going to Enceladus. What nobody can do is be EXPECTED there. The ice is empty until your hull is on the
board — and the lift head is not a thing the world hides from you so much as a thing that is simply not
there for a ship nobody filed for. The gap the arc crosses is **permission and timetable**, not distance,
and every shard in the pool is about a filing, a window, a berth or a manifest. Not one of them is about
fuel.

> **Consequence for the build, and a rule for anyone touching it:** a captain who flies to Enceladus with
> no berth code must find *featureless ice and a good view*. Not a locked door, not a "you cannot land
> here", not a hint. An empty moon. That refusal-by-absence is the whole reason the arrival lands.

---

## 2 · The front door (#635) — SHIPPED in PR 1

**Picked: option 3 — a mission-desk contract that bounces off the sealed berth.** *(The alternatives are
costed in the issue; option 1 adds another line to bars #410 already calls too chatty, option 2 is the
game announcing where to look, option 4 costs most players six beats and the best line in the game.)*

A freight agent at a bar is holding a docket that keeps coming back. They will pay a small flat fee —
`KaamosFind.BounceFilingFee`, 350 cr — for you to put your own hull's number on it, because a
fourth-hand attempt is cheaper than an answer. You take it. The board replies before your hand is off the
plate:

> RETURNED — CONSIGNEE CANNOT BE RAISED — BERTH HELD, AWAITING CYCLER WINDOW.

**Held. Not closed, not lapsed, not struck.** That one word is the entire hook, and it is a word that
appears on real dockets. The agent shrugs, pays, and takes the parcel back to wherever it lives between
attempts. Nobody asks for the receipt, so you keep it.

### The five disciplines it is held to

1. **It hands over no shard.** The pool is what the gate counts; a sixth intel piece would move every
   threshold in the arc and every save. The bounce credits nothing, opens nothing, and is stored as its
   own per-thread flag (`KaamosProgress.BerthFilingBounced`), not as a pool id.
2. **It raises the ledger card, which is the thing the arc never had.** Before this, the one place in the
   game that says PROJEKTI KAAMOS is a thing you could be doing appeared *strictly after you had already
   started doing it*. A returned filing is now enough. The card in that state does not read
   "0 of 5 shards" — a progress bar for a quest nobody has been given — it names the paper in your
   pocket.
3. **It states nothing.** A docket may say a berth is HELD and that a window is not open. It may not say
   who is holding it. Guarded: `TheFrontDoorSaysHeldAndNeverSaysWho`.
4. **It is a door, not an event.** One bar-watch in three, seeded per (bar, day), and offered only to a
   captain who holds nothing of the arc at all — so it can never elbow a live thread aside, and it is
   gone the moment it has done its job. The rarity is *measured*, both ways, against what the seed
   actually produces: every bar opens it within a week, and no bar opens it every night. A one-sided
   check would pass on "never" and on "always", and both of those are the bug.
5. **It is a contract, not a trick.** The captain is told, before they agree, that the thing has bounced
   four times already, and the fee is printed on the same card as the button. A job that evaporates after
   you take it is a bug wearing a story.

`/map?kaamos=bounce` seats the agent at every bar for the run.

---

## 3 · What "fancier than the Hive" means, concretely

The owner's constraint is the interesting half: **outclass them in the same vocabulary.** Not a new
grammar — the *same* grammar, at a rank the player can read because they have spent hours learning the
branch-office version of it. Every row below is one line of code and one list of words.

| the grammar | a branch office (the Hive, shipped) | the head office |
| --- | --- | --- |
| **depth** | seeded 3–8 floors, sometimes 13–16, rarely 19–20 | **24 — the deepest thing in the game**, and every one of them listed |
| **the band nobody listed** | one site in four hides a band from its own staff | **none.** HQ has nothing to hide *from itself*, and the absence is the rank |
| **the lift directory** | stops where the building admits it stops; the car goes no further | lists **all six bands**, top to bottom, on the panel |
| **authority cards** | a card opens exactly one band; the way down is a card somebody left in a room | **the car answers.** You are on the board. Nothing down here asks you for anything |
| **sealed corridor mouths** | `⟶ SECTOR 7 · 2.4 km` | `⟶ WING 3 · 24.6 km` — the same plate, one order of magnitude |
| **department plates** | eight names on a cycle; B1 and B9 are both ADMINISTRATION | **twenty-four names, none repeated.** A branch reuses its plate stock; the head office had one made per floor |
| **livery** | muted paint on poured concrete, unmaintained for decades | the same six-hue language, **clean** — somebody is still painting it |
| **the doors** | about half locked, the sign doing the work | most of them **open**. A locked door implies somebody to lock out |

The last two rows are the ones that do the real work, and they are the head-office horror in one
sentence: **the building is in perfect order and there is nobody in it.**

> **Why NOT "deeper still".** `UndergroundComplex.DeepestPossibleFloor` (−24) is a **performance guard**,
> and its own doc comment says so: *"Nothing should ever read it as 'how deep the game goes'."* The head
> office takes the guard's whole allowance and beats the Hive on *quality of building*, not on a number
> nobody can perceive. A twenty-fifth floor would cost frame time and buy nothing.

### The floor list — the whole story, told only in plates

Read top to bottom. Nobody narrates any of it. This is the beat list.

| | plate | what a captain finds |
| --- | --- | --- |
| B1 | RECEPTION | a lobby built to impress somebody, with a directory that lists all twenty-four |
| B2 | ESTABLISHMENT | staffing. Filing cabinets by branch. |
| B3 | THE REGISTRY | who is on the books |
| B4 | SCHEDULING & WINDOWS | the timetable the whole arc has been about |
| B5 | PROCUREMENT | *(the word does its own work; nothing here explains it, ever)* |
| B6 | CONTRACTS | |
| B7 | BRANCH LIAISON | **the beat.** Rooms filed by moon — every Hive the player has crawled, named |
| B8 | AUDIT | |
| B9 | PAYROLL — CLOSED ACCOUNTS | |
| B10 | LONG CONTRACTS | |
| B11 | CONTINUITY | the Laboratory's own word, at head-office scale |
| B12 | THE STANDING ORDER | the room the takeable evidence is in (§5) |
| B13 | SITE ESTABLISHMENT | |
| B14 | DISPATCH | |
| B15 | THE COLD ROOMS | |
| B16 | OCCUPANCY | the vocabulary turns from things to people, and never turns back |
| B17 | WELFARE | |
| B18 | THE WINTER OFFICE | |
| B19 | RESIDENCY | |
| B20 | THE QUIET ROOMS | |
| B21 | DEEP RESIDENCY | |
| B22 | THE WATER GALLERY | the ice stops being above you and starts being beside you |
| B23 | **THE WINTERING HALL** | the room this arc was written for (§4) |
| B24 | THE BERTH OFFICE | one console, still filing (§4) |

A captain who has crawled a Hive reads B7 and understands the rank without one word of narration: the
department plates by the lift car are a branch-office idiom, and here is the office that *issued* them.

---

## 4 · Staffing by absence — the place still RUNS

This is the head-office horror and it is not a monster.

- **The lights cycle.** Not "the lights are on" — they come up ahead of you and go down behind you, on a
  schedule, the way a building does when somebody set the schedule and nobody changed it.
- **The car answers immediately**, every time, from any floor. In a Hive the lift is a negotiation. Here
  it is a service.
- **Nothing is dusty.** The Hive's whole texture is decades of neglect. HQ's texture is *maintenance* —
  and there is nobody to maintain it.
- **The berth still files.** B24's console has a log, and the log is current. The last accepted delivery
  is a lifetime old. The next filing is dated for the window you rode in on.
- **Not one body, not one Reever, not one Old One.** The head office is the only underground place in the
  game with nothing alive in it, and it is the most frightening one. Whatever is keeping it does not need
  a corridor.

### B23 — the wintering hall

One room instead of ribs and cells: a gallery the size of the whole floor, open to the black water on one
side. **Forty berths, in four rows of ten. Every one of them made up.** Turned back, aired, warm.

And at the end of the fourth row there is one more, made up the same way. **Forty-one.**

Nothing on screen says whose it is. Nothing on screen says what happened to the forty. The captain does
the arithmetic, or does not, and either way the game never confirms it.

> **This is where `KaamosLore.RevealSanityShockHook = 40.0` lands, and it is the only place it could.**
> The constant's own doc comment reads: *"reaching the wintering mind is the heaviest #391 throw in the
> game"*, and the bible's reveal is *"it has kept a berth warm for you."* The forty-first bed **is that
> sentence, as evidence.** 40 nerve is ~40% of the gauge and the biggest single throw in the game
> (the monolith is 24). Wired in PR 3, flagged loudly there, overrulable in one line.

### B24 — the berth office

One console, one board, one line lit — the same image as the capstone's own reveal plate, seen from the
other end. The log shows a filing going out on every window, on the tick, for longer than the captain has
been alive, addressed to an exchange that stopped reading them.

**It does not stop when you arrive.** That is the point, and it is also the open question in §7.

---

## 5 · What the player can take, and what may never be confirmed

### Takeable

- **THE STANDING ORDER** (B12) — one countersigned instruction that the supply runs are to continue
  *until countermanded*, and a countersignature block that was never used. A satchel item under #614's
  law: **a card may say WHAT, never WHERE.** It is evidence that somebody with authority set this going
  and nobody with authority stopped it. It names an office. It does not name a person, a moon or a
  reason.
- **Ordinary haul.** Equipment, dirt, records and keys, exactly as any Hive — the head office is still a
  building full of offices, and stripping it should pay like the deepest site in the game because it is.

### Learnable

- The scale of the operation, from the plates alone.
- That every Hive the player has been inside is on a list on B7, filed under the moon it is on.
- That the filings never stopped.

### Never confirmed, in this or any later slice

- What is in the water.
- Whether anyone is alive, was alive, or is one or forty.
- Whose the forty-first bed is.
- What the Old Ones are (standing canon, owner 2026-07-30 — and this is the single most tempting place in
  the game to break it).
- Whether the thing knows you came.

---

## 6 · How it ends: leaving is a choice, and it is not a boss

There is no fight at the bottom of the head office. There is a lift, and it works, and it will take you
back up whenever you press it — which after four days of the deepest building in the game is itself a
kind of dread, because *nothing stopped you.*

What the ending is instead: **you leave holding something, or you leave holding nothing.**

- **Take the standing order** and there is now a piece of paper in the world that says this was
  authorised. It is worth something to somebody. It can be shown.
- **Leave it** and nobody will ever see it. #615's law applies and is not negotiable: **LEAVE must not
  destroy it** — a captain who walks out can come back.

The weight is that both are irreversible in the way that matters: the taking is a decision to make it
somebody else's problem, and the leaving is a decision that it stays exactly as it is. The game does not
score either one.

---

## 7 · Open, and NOT built unilaterally: can anything stop the filing?

**The question:** B24's console is still filing for a supply run, on the tick, forever. Can the captain
countermand the standing order and switch that line off?

It is the most dramatically loaded button the game could possibly have and it changes the shape of the
game, so it is a decision, not a build:

| option | what it costs | what it buys |
| --- | --- | --- |
| **No button.** The filing cannot be stopped. | nothing to build | the arc has no OFF switch, which is honest — you found out, and finding out changes nothing but you |
| **Countermand it.** One counter-signature; the line goes out. | a persisted world change, and everything that reads the berth afterwards has to agree | the single heaviest act available to a player in this game, and the game never says whether it was mercy or murder |
| **Countermand it, and the world notices.** The wire carries a routine line about a dormant berth being struck off. | the above, plus a news hook | the ending lands twice — once under the ice, once in a bar three weeks later |

**Filed as an issue rather than built.** Recommendation, offered not decided: the middle one, with
absolutely no confirmation of what it did.

---

## 8 · The slices

| PR | what lands | state |
| --- | --- | --- |
| **1** | this document; the front door (#635 option 3) with guards proven RED; the fifth bug-class row in the story-arcs handoff | shipped |
| **2** | the route: the KAAMOS supply run on the board, the cycler window, the crossing; `ArcNewsBreaks` (#663) on the berth-code edge; `?kaamos=hq` | |
| **3+** | the facility: `UndergroundComplex.Kind.HeadOffice`, the 24 plates, the livery, the wing distances, the wintering hall, the berth office, the standing order, the 40.0 throw, the art | |

### Cheats (each documented in `../testing-guide.md` Appendix A in its own PR)

| URL | what it does |
| --- | --- |
| `/map?kaamos=bounce` | seats the freight agent with the returned docket at every bar — the front door, on demand |
| `/map?kaamos=hq` | *(PR 2)* the berth code resolved, the run accepted and the window open — put on the ice |

### The reuse rule for PR 3

**Extend `UndergroundComplex`, never fork it.** One source of truth: the head office is a `Kind` and a
handful of body-aware overloads, not a second generator. Anything that ends up copied out of that file is
the bug this repo keeps paying for, and the whole table in §3 is achievable without copying a line of
geometry.

---

[#411]: https://github.com/esoinila/SpaceSails/issues/411
[#635]: https://github.com/esoinila/SpaceSails/issues/635
