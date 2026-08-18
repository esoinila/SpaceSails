# QA handoff — the Hive is populated

*Written 2026-08-05 evening, immediately after the second publish of the day (`61e1864`). Six features went
live in one push and **not one of them has been walked by a person.** This is the plan for doing that.*

Companion to [`QAHandoff-StoryArcs.md`](QAHandoff-StoryArcs.md) (the bug lens) and
[`QAHandoff-StoryTelling.md`](QAHandoff-StoryTelling.md) (the story lens). Everything those say about the five
bug classes, proven-RED guards, canon and the environment traps applies here unchanged. **Read the bug-class
list first.**

Every URL below is in [`testing-links-the-hive.md`](testing-links-the-hive.md), where the floor numbers were
read out of the real generator with Lab 48 rather than assumed.

---

## 0 · What is live, and where

**`esoinila.github.io/SpaceSails-play` is current as of `61e1864`.** For the first time tonight the live build
has everything, so this can be played in a browser without a local server. Locally it is still
`dotnet run` — **not `watch`** — and kill/restart per build.

Six features, newest first:

| issue | what shipped | walked by a person? |
| --- | --- | --- |
| **#721** | the canteen keeps a rota; the test-links file | **no** |
| **#709** | ten strangers in the top-floor bar, and the cork board | **no** |
| **#677** | the pour stops at a line — a band nobody dug, the fourth world | **no** |
| **#701** | one would-be-empty room in six holds a book that should not be there | **no** |
| **#708** | forward-facing suit headlights, genuinely dark floors | **no** |
| **#707** | a bar on the top floor, cubicles, rank readable in plumbing | **no** |

That column is the entire justification for this document.

---

## 1 · The one-screen route

```
/map?secretlab=deep&land=1&floor=1     B1  — the canteen: people, board, plate, washrooms
/map?secretlab=deep&land=1&floor=11    B11 — the floor the plate bug was found on. No plate now
/map?secretlab=deep&land=1&floor=17    B17 — the staff mess: furnished, and empty on purpose
/map?secretlab=deep&land=1&floor=21    B21 — the plate names a DIFFERENT building
/map?secretlab=deep&land=1&floor=3     B3  — nine rooms; about one in six holds a book
/map?secretlab=deep&land=1&floor=4&dark=1   B4 — no lights, headlights only
/map?found=1&land=1&floor=17           past the seam. Not ours
```

The first two are **dev-start buttons** in the UI, so they need no typing.

---

## 2 · The four questions no test can answer

Ordered by how much the answer changes what gets built next.

### 2.1 Can you match a notice on the board to the person who pinned it?

**This is the whole reason the board exists.** Read the board four times (one notice per press, then it cycles),
then talk to everyone at the tables.

| the board says | the person who pinned it |
| --- | --- |
| `PUMP 2 — written up 12/4, 19/4, 2/5, 16/5. Still listed OPEN.` | the fitter: *"it's been singing since spring"* |
| `COUNTERSIGNATURES — the signatory is away. No date has been given to us either.` | the carrier: *"third day sat here"* |
| `ROTA WEEK 31 — disregard the first name and use the second.` | the temp: *"they put a different one on the rota"* |
| `STORES — no soil samplers in stock. Do not query the description on your docket.` | the invoices woman: *"my pallet jack is a soil sampler"* |

**Nothing in the game points this out, ever.** If the connection does not land unprompted, the pairing is
wasted effort and the answer should arrive **before** anything in #718 gets built on top of it.

*Note the roster turns over with the shift, so the person a notice belongs to may be off duty. That is
deliberate — a lucky moment, not a checklist — but it does mean a single visit may not show the pairing at all.
Judge it over two or three watches before concluding it does not work.*

### 2.2 Do the figures read as PEOPLE, or as three more consoles?

They are drawn with the same console machinery as a valve or a lift panel. That is the risk, and it cannot be
asserted — a test can only prove they are *there*.

### 2.3 Is one breath each enough?

Every regular says exactly one line, filed once, and the plate pulses after. Deliberate restraint: *a room that
starts offering things is a room that is paying attention to you.* But it may simply read as truncated.

### 2.4 Does B21 land?

Twenty floors of `▣ THE CLINIC`, four floors of rock, and then a sign that says the building is something
else. That is #592's whole arithmetic delivered by one plate, and it is the best moment #694 bought. If it
passes unnoticed, the plate needs more weight rather than more words.

---

## 3 · By design, NOT a bug

Check this list before filing. Each of these looks wrong and is not:

| what you see | why |
| --- | --- |
| **The same people in the same chairs when you re-enter** | the roster is per **shift** (`PatronRota`'s 4 h watch), frozen when the floor is drawn. A room that reshuffled while you stood in it would also make **[E]** answer about somebody who had moved. A new crowd needs sim time to pass — and `SimTime` barely advances on a regolith (#469), which is why it holds still |
| **The staff mess on B17 is furnished and empty** | the B1 ruling: staff stop at band 0. Who is in the deep mess is #618's question, unbuilt |
| **No facility plate on B5, B9, B13, B17** | shaft heads are not doorways. The law is B1 + the unlisted band's own lobby, deliberately *not* "every band top" (#694) |
| **Most searched rooms still say "stripped to the fittings"** | one in six holds a book. The emptiness is load-bearing; always-something is a shopping trip (#701) |
| **A notice whose person is not in the room** | shift rota, see 2.1 |
| **Nothing below B1 has anyone on it** | the descent gradient is the feature — population falling to zero is what makes the empty floors read as *absence* |
| **The galleries past the seam have no plates, doors or lamps** | #677: nothing past the seam was ever ours |

---

## 4 · Coverage, ordered by likely payoff

| priority | what | why it is suspect |
| --- | --- | --- |
| **1** | **The canteen as a ROOM** — walk it, sit at every table, press everything | brand new, drawn with console machinery, and the only room in the building with people. Bug class 1 (drawn ≠ reachable) and the console-crowding family both live here |
| **2** | **The board's cross-reference** | see 2.1. The highest-value *design* answer available tonight |
| **3** | **B21 and the plate law** | #694 is a one-line change with a big claim; verify the plate is on exactly two floors of that site and nowhere else |
| **4** | **The found halls (#677)** | the newest generator, the largest new geometry, and the exact profile of the Hive's worst historical bug (35 floors of sealed doorways) |
| **5** | **Dark floors + headlights (#708)** | a lighting model interacting with every floor already shipped. Check you can still *find* the lift in the dark |
| **6** | **The odd books (#701)** | prose and a 1-in-6 gate; check the rate feels right rather than measured |
| **7** | **The amenities' en-suites (#707)** | rank readable in plumbing — does a private washroom actually read as "somebody mattered here"? |

### The standing method, applied here

- **Walk to everything you can see.** Drawn ⇒ prove reachable. Reachable ⇒ prove it does something. #600
  survived three PRs because an A\* audit proves you can REACH a thing, never that it is a way BACK.
- **Read every sentence against what the sim did** (bug class 3). The rota's frozen watch exists precisely to
  stop the drawn room and the pressed room disagreeing — if you ever see a figure answer as somebody else,
  that is a serious finding, not a cosmetic one.
- **Check both ends.** Down is not up.
- **Open every instrument and ask what it does NOT say.**

---

## 5 · What has NO link, because it is not built

Filed tonight from the owner's own design run. **Do not build these; they are decisions and dependencies.**
Adding a test link for an unbuilt mechanic is how a testing guide starts lying.

- **#719 — a second way out.** *Ships before anything is allowed to stop the car.* One radio call ending every
  escape is a switch, not a threat, and #600 is the scar that says reachable is not returnable. **This is the
  gate: #618 and #718 both assume an escape that does not exist yet.**
- **#618 — the guards.** Now largely ruled (see §6) but unbuilt.
- **#715 — illegal heat**, owed per entity, never a shared cheaters list.
- **#718 — the response ladder**: hired if the cover holds, rolled back if it does not; backups as inventory;
  recognition as the real threshold; the suit as anonymity; the technician who remembers you.
- **#720 — MINIMUM PRODUCTION BATCH**, the ending where the captain becomes stock. Needs art and extreme canon
  care.
- **#711 — onion covers**, and the analyst who peels them by *reading* rather than seeing.
- **The deep mess's own people** — B17 is furnished and waiting on #618.

---

## 6 · Decisions the owner RULED tonight (2026-08-05)

Recorded here because [`QAHandoff-StoryArcs.md`](QAHandoff-StoryArcs.md) §4 listed several of these as open and
they no longer are. Full text and reasoning live on the issues.

- **#618 Q1 — is anybody down there?** **Skeleton staff.** Implied by the cover story: *"our cover to go there
  can be that we look for work"* — you cannot apply for a job at an empty building.
- **#618 Q2 — what "security arrives" means.** Guards on the **bottom floors**; they do not check papers unless
  you try to walk past them; they come for **noise**; you can flee and they follow; they may not follow you
  off-site; they get their **own colour** on the tracker, *"not red but something different than now."*
- **#618 Q3 — what blows the cover.** **Talking to them.** Probing for information makes them probe back — the
  risk is chosen, every time, for something the player wants.
- **The people stop at B1.** *"for now let's keep the people in B1"* — shipped in #709.
- **Heat is per ENTITY** (#715), never a shared list: *"not like the Casinos that distribute cheaters lists in
  Vegas."*
- **No Epstein papers** among the lore finds (#701's register test): the find must be something the player can
  be *delighted to disbelieve*.

**Canon unchanged and under more pressure than ever:** nothing down here explains the Old Ones (§13.8). The
rollback, the batch, the technician and the guards are all commercial and bureaucratic. If a player assembles
the rest themselves, that is the best moment the game is capable of; if we help, it is worth nothing.
