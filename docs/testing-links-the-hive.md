# Test links — the Hive, floor by floor

*Companion to [`testing-guide.md`](testing-guide.md) Appendix A, narrowed to one building. Every link here
boots straight into the situation, and **every floor number below was read out of the real generator** (Lab 44,
`dotnet run --project labs/44-a-lab-about-the-lab/Lab44.csproj -- --body <id>`) rather than assumed — a
document full of confident URLs that land on the wrong floor is worse than no document (§13.19's own lesson,
one head along).*

`Map.Sim.cs`'s rule applies: **a scene nobody can reach on demand is a scene that ships broken.**

---

## The three cheat sites, and their real shapes

| link | body id | what it is |
| --- | --- | --- |
| `?secretlab=1` | `secret-lab-site` | **▣ THE RECORDS ANNEX** — 4 floors, nothing under them. The shallow case |
| `?secretlab=deep` | `secret-lab-site-unlisted` | **▣ THE CLINIC** — 20 listed floors **+ an unlisted band B21–B24** (▣ THE TRANSIT STATION). The deep case, and the only one that reaches #592 |
| `?found=1` | `secret-lab-site-halls-116` | **▣ THE LABORATORY** — 7 listed, unlisted band B9–B12, then rock, then **the halls nobody dug at B17–B20** (#677) |

`?secretlab=1` **cannot** reach an unlisted band — its site is a four-floor records annex with nothing beneath
it. Use `?secretlab=deep` for anything about the band nobody listed.

### The deep site, as the generator actually builds it

```
  B1   ADMINISTRATION   air    ← the canteen, the people, the board, the facility plate
  B2   LABORATORIES     dead
  B3   LONG STORAGE     dead
  B4   PLANT            dead
  B5   ARCHIVE          air
  B6   ISOLATION        dead
  B7   DEEP STORAGE     dead
  B8   UNMARKED         dead
  B9   ADMINISTRATION   air
  B10  LABORATORIES     dead
  B11  LONG STORAGE     dead   ← where the owner found the plate on every floor (#694)
  B12  PLANT            dead
  B13  ARCHIVE          air
  B14  ISOLATION        dead
  B15  DEEP STORAGE     dead
  B16  UNMARKED         dead
  B17  ADMINISTRATION   air    ← the STAFF canteen: deepest LISTED floor that still breathes
  B18  LABORATORIES     dead
  B19  LONG STORAGE     dead
  B20  PLANT            dead
  B21  NO PLATE         air    ← the unlisted band's own lobby. THE PLATE CHANGES HERE
  B22  NO PLATE         dead
  B23  NO PLATE         dead
  B24  NO PLATE         dead   ← the bottom of the hole
```

---

## 1 · The people in the bar (#709)

```
/map?secretlab=deep&land=1&floor=1     B1 — the canteen, with people in it
```

**What to look for.** Three round tables with up to three figures on them, a `📌 THE BOARD` on the room's left
wall, and the counter along the back. Press **[E]** at a figure for one breath of their working day; press it
again and the plate pulses instead — one finding per person, filed to the field book.

The room's own plate reads `🍸 CANTEEN 1 · CARRIERS & CONTRACTORS · NO PASS REQUIRED`, which is *why* these
people are here: hauliers, fitters, agency temps and drivers, with no more right to be in the building than you
have. That is the whole cover story, standing in a room.

**Then go one floor down and check the absence:**

```
/map?secretlab=deep&land=1&floor=2     B2 — dead air, and NOBODY
```

Every floor below B1 is deserted on purpose (the owner's ruling). If you ever find a figure on B2, that is a
bug and `ThereArePeopleInTheBarTests` should have caught it.

### The room changes with the shift

Owner: *"let's have some random element of who is in the bar and where they got to sit down."*

**Who is in and which chairs they took turn over with the watch** — the same 4-hour watch the station bars
upstairs already run on (`PatronRota.WatchSeconds`), because the board on that very wall says `ROTA — WEEK 31`
and a rota is better randomness than a dice roll.

So testing it needs *time to pass*, not a page refresh:

- **Re-entering the same room in the same watch shows the same people in the same chairs.** That is correct, not
  a bug — a room that reshuffled while you stood in it would also make the **[E]** key answer about somebody
  who had moved.
- **To see a different crowd, let the sim clock advance a watch** (fly somewhere and come back). `SimTime`
  barely advances on a regolith (#469), which is exactly why the room holds still during an excursion.
- The shift is **frozen when the floor is drawn** and every later question reads that same number, so the
  figures drawn and the person the game talks about can never come apart.

## 1a · ADDED (#746) — sitting down at a table, and the job that goes downstairs

```
/map?tablescene=1                      the whole route, booted — B1, in the canteen, autowalk on
/map?tablescene=1&roll=lo              …and every rolled ask refused, so the scene MOVES on demand
/map?tablescene=1&roll=hi              …and every rolled ask lands
```

`?tablescene=1` implies `?secretlab=deep&land=1&floor=1`, sets the captain down **inside** the upper canteen
rather than at the lift head, and turns `?autowalk=1` on — the last leg of this scene is a walk across a room,
and clicking where you want to be is how this repo tests one.

**What to look for.** The round tops now carry **SEAT COUNTS** — 2, 4 or 6, seeded off the building and never
off the shift, so a table does not re-furnish itself between watches. Walk to a top with somebody at it and
press **[E]**: if they are one of the three wired regulars and a chair is free, you **ask to join** and the
table panel opens with their wave-in line. Everybody else in #709's cast keeps their one-breath tap, which is
deliberate — a canteen where every stranger has a conversation tree is a corridor with quest-givers in it.

The panel's moves are **small talk · buy the round (12 cr) · put something on the table · ask about work ·
take your leave**. Every answer is rendered **inside the panel** (#680's law: the pulse HUD sits under a modal
backdrop's blur, and this scene is nothing but text). The one line that is pulsed is *taking your leave*, and
that is correct precisely because the panel is gone by then.

**The three of them are three answers:**

| who | what they are |
| --- | --- |
| `◈ A HAND WHO HAS BEEN HERE LONGER THAN THE CONTRACT SAID` | **the door.** Their ask-about-work is the only rolled move at this table |
| `◈ A FITTER, OFF A MAINTENANCE CONTRACT` | **the dead end.** Honest scaffold work, never rolled, and the polite dodge (`Not my trade — but thanks`) costs *nothing* |
| `◈ AN AGENCY TEMP, FIRST WEEK` | **no job, but the house's ways.** Their second line needs a round bought first, and knowing it is the +1 on the Hand's ask |

**The roll** is `DiceRule`, keyed (site, floor, counterpart, move, attempt), through three Fail-Forward bands —
**YES** / **YES, BUT** / **NO — AND THE SCENE MOVES.** Modifiers are situational and named on screen: `+1` a
round bought at this table this watch · `+1` the right paper on the table · `+1` the house's ways learned
first · `−1` your nerve is marked (read off the gauge's own rungs, so the modifier and the readout can never
disagree) · `−1` you already fumbled an ask here. **Nerve is the social resource too** — YES-BUT and NO-AND
each spend a pip through the ordinary gauge, never a parallel meter.

**Watch the refusal, because it is the busiest outcome in the scene.** `?tablescene=1&roll=lo`, then ask the
Hand about work: the table hardens for the watch, the **fitter's** ask lights up, and the **temp's** second
line becomes available without a round — they overheard. That is what "no, *and*" means here.

**Putting something on the table** (any satchel item) is a conversational move:

- a **file on somebody** is LOUD — ask-about-work closes at that table for the watch and the field book keeps
  the slip. (`&floor=1` plus a file picked up on a deeper floor, or just carry one down.)
- the **SHAFT 4** authority card (band 3 or deeper) makes the Hand go quiet and auto-resolves their ask as
  YES-BUT. Fear, not friendship.
- anything else made of paper is weather on another moon.

**The payoff is a wallet card.** A successful ask grants the **DAY-LABOUR CHIT** (`🎟 DAY-LABOUR CHIT ·
CARRIERS & CONTRACTORS · CAGE CREW · SHOW AT THE CAGE`), and on a YES-BUT it is written in the book under a
name the Hand picked — the same paper, a different **fact**, and the fact rides the chit's own identity in the
satchel rather than a flag beside it (#718 will pull that thread). The chit **is** the cover state Core
answers about (`CanteenTable.Cover`), which is what #618's guards will read.

**One visible payoff today**, in the room the pass exists for:

```
/map?tablescene=1  → get the chit → &floor=17 (the staff mess, #743)
```

Walk into B17's mess carrying the chit and you get a one-time beat and a pip back — **additive**, never a
replacement: #743's own room card still fires first, and the chit beat lands on the tick after you close it,
so nothing is ever said under an open card.

## 2 · The board, and whose notice is whose (#709)

```
/map?secretlab=deep&land=1&floor=1     stand at 📌 THE BOARD, left-hand wall
```

**[E]** gives **one notice per press**, cycling through the four pinned on this site. Press it four times, then
go and talk to the people at the tables.

**The thing to check is the cross-reference, and nothing in the game will point it out:**

| a notice | the person it belongs to |
| --- | --- |
| `PUMP 2 — written up 12/4, 19/4, 2/5, 16/5. Still listed OPEN.` | the fitter, whose pump *"has been singing since spring"* |
| `COUNTERSIGNATURES — the signatory is away. No date has been given to us either.` | the carrier, *"third day sat here"* |
| `ROTA WEEK 31 — disregard the first name and use the second.` | the temp, *"they put a different one on the rota"* |
| `STORES — no soil samplers in stock. Do not query the description on your docket.` | the woman doing invoices, *"my pallet jack is a soil sampler"* |

**The board is fixed per site; the PEOPLE are not.** Which four notices are pinned is seeded off the rock, so a
given site always shows the same board — but the roster turns over with the shift (below), so **the person a
notice belongs to may be off duty when you read it.** That is deliberate: matching a notice to a face is a
lucky moment, not a checklist.

Every notice always pairs with somebody in the *cast*, though. **If a notice pairs with nobody the game could
ever seat, that is a bug** and `TheBoardInTheBarTests` should have failed.

## 3 · The facility plate, on the two floors it belongs on (#694)

The owner's original report was *"every floor has the text 'The Clinic' on it. Some kind of artifact?"* These
three links are that finding, fixed:

```
/map?secretlab=deep&land=1&floor=1     B1  — ▣ THE CLINIC, beside the shaft. PRESENT
/map?secretlab=deep&land=1&floor=11    B11 — no facility plate. This is the floor he was standing on
/map?secretlab=deep&land=1&floor=21    B21 — ▣ THE TRANSIT STATION. A DIFFERENT NAME
```

**B21 is the payoff.** It is the unlisted band's own lobby, and it is the one place in the game where this
plate names a different `Kind` from the twenty floors above it. Twenty floors of `▣ THE CLINIC`, four floors of
rock, and then a sign that says the building is something else.

Check also that **B5, B9, B13 and B17 have no facility plate** — they are shaft heads, not doorways, and the
law is deliberately *not* "every band top".

### 3a · …and the card the payoff earns (#725)

The plate was a wall stencil, and a player at deck-plan zoom could walk past the reveal of the arc with the
game none the wiser. The first arrival on B21 now stops the world with **▣ THE PLATE** (`art/the-plate.jpg`):
a lobby with no department and no livery, and a sign screwed on over a wide patch of newer paint.

```
/map?secretlab=deep&land=1&floor=21    B21 — the card, on the first arrival of the excursion
```

Once per excursion, exactly like 🫁 DEAD AIR. Which floor it belongs to is `UndergroundComplex.IsUnlistedLobby`
— #694's own plate law minus the entrance lobby — so it can never fire on B1, and never on B5/B9/B13/B17.
**The card never quotes the plate**: the text varies by site kind, so a card that transcribed a sign the
renderer draws would be the same fact in two places, as well as the one sentence that turns the find into an
answer. Guarded by `TheSilentFindsGetACardTests` (Core) and `TheSilentFindsAreRaisedOnceTests` (client).

## 4 · The amenities, and rank readable in plumbing (#707)

```
/map?secretlab=deep&land=1&floor=1     B1  — CANTEEN 1 + the washrooms
/map?secretlab=deep&land=1&floor=17    B17 — CANTEEN 2 · STAFF ONLY · PASS TO BE SHOWN
```

B17 is the deepest **listed** floor that still holds pressure, which is where the staff mess goes. Its plate
demands a pass, there is no till, and the fixtures are vending machines — *"what is NOT in it"* is the design.
**It has no people in it yet** (the B1 ruling); that room's cast is #618's question.

On both floors, look for **en-suite cells** hung off the back of principal rooms — a door to a private
washroom, with no plate on it. That absence is the tell: somebody with a name worked in there. Read the
en-suites as *apartments* and the building stops having employed people and starts having housed them.

### 4a · …and the room nobody eats in gets a card (#725)

B17's mess was map furniture — four machines, three tables and a plate — and pressing **E** was the only way
to be told anything about it. **Walking in** now raises 🍽 **THE STAFF MESS** (`art/the-staff-mess.jpg`).

```
/map?secretlab=deep&land=1&floor=17    B17 — walk into the mess room; the card lands on entry
```

**A room beat and not a floor beat**, which makes it the only one of its kind down here: the floor is an
ordinary floor and the *room* is the find. So it is the refuge idiom — poll the position, ask Core whether
the room holds you — with `UndergroundComplex.Amenity.Contains` delegating to `RefugeHolds`, the same
containment law the refuges run on. Once per excursion; the card must **not** reopen when you close it and
walk on. The trigger box is guarded against the whole floor's console list, so it can never reach the room
across the corridor.

## 5 · The odd book in the empty room (#701)

```
/map?secretlab=deep&land=1&floor=3     B3 — nine rooms to search, about one in six has a shelf
/map?secretlab=deep&land=1&floor=2&book=9    force a specific catalog entry
```

Search rooms until one gives you a **shelf line** instead of *"Stripped to the fittings"*. Nothing enters the
satchel — the book is read where it stands — and the gist files to the casebook once.

## 6 · A floor with no lights (#708)

```
/map?secretlab=deep&land=1&floor=4&dark=1    B4, and the suit's headlights are the whole of the seeing
```

## 7 · The halls nobody dug (#677)

```
/map?found=1&land=1                    the lift head, with the whole wallet
/map?found=1&land=1&floor=9            B9  — the unlisted band's lobby
/map?found=1&land=1&floor=17           B17 — PAST THE SEAM. Not ours
/map?found=1&land=1&floor=20           B20 — the bottom of something that was already there
```

Note the shape of that site: 7 listed floors, an unlisted band at **B9–B12**, **rock where B13–B16 would be**,
and the galleries at **B17–B20**. The gap is real and nothing is generated in it.

Past the seam, everything that makes a facility legible is gone — no plate, no department, no livery, no locked
door, no shelf, no lamp — and the floors still hold air with no visible means. **0 sealed doors on all four.**

## 8 · Air, and the refuges (#608)

```
/map?secretlab=deep&land=1&floor=2&air=90     a dead floor with ninety seconds in the tank
/map?secretlab=deep&land=1&floor=7            B7 — deep, dead, and one refuge somewhere on it
```

Every airless floor carries at least one pressure refuge. The tracker paints them and never the surface
shelters. The plate over the lift car says the depth, the department, **and whether you can breathe** — check
it agrees with the HUD, because §13.13's whole law is that those two may never disagree.

---

## Add-ons that stack on any of the above

| Argument | What it does |
| --- | --- |
| `&floor=N` | ride straight to BN (clamped to the site's real depth) |
| `&air=45` | 45 seconds in the tank — the point-of-no-return warning without the stroll |
| `&dark=1` | this floor has no lights (#708) |
| `&book=N` | force catalog entry N of the odd books (#701) |
| `&death=suffocated` | boot into the death you want to read |
| `&credits=50000` | price anything without grinding for it |
| `&roll=hi|lo` | force the encounter band at a table (#746) |

## What has NO link yet, because it is not built

Filed tonight and deliberately absent from this document — adding a link for an unbuilt mechanic is how a
testing guide starts lying:

- **#719** the service stair / second way out — *and it must ship before anything is allowed to stop the car*
- **#618** guards on the bottom floors, the noise trigger, the talk risk
- **#715** illegal heat, owed per entity
- **#718** the rollback, the coerced job, recognition
- **#720** the batch ending
- the **staff cantina's own people** (B17 above is furnished and empty on purpose)
