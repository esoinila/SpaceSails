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

**An answer only exists once the sentence does (#749).** Sit at the fitter and look at the moves: `Take the
scaffold job` and `Not my trade — but thanks` are **not in the panel at all** until `Ask about work` has made
him say *"South face scaffold, four watches, pay at the end of each. Wind's free."* — **this sitting**. Stand
up and sit back down and they are gone again; the room still remembers you asked (that is the watch's job),
but the offer was made to somebody who then left the table. Everything you have merely not *earned* is still
drawn and still refused out loud (`Keep talking`, greyed, says why) — the difference is the difference between
a locked door and a question nobody asked.

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

**A second payoff**, in the room the pass exists for:

```
/map?tablescene=1  → get the chit → &floor=17 (the staff mess, #743)
```

Walk into B17's mess carrying the chit and you get a one-time beat and a pip back — **additive**, never a
replacement: #743's own room card still fires first, and the chit beat lands on the tick after you close it,
so nothing is ever said under an open card.

**…and the ending the scene was hired for (#752): take it to the lift.** The Hand's YES line says *"take this
to the lift and don't be clever near the counter"*, so the walk finishes at the car:

```
/map?tablescene=1&roll=hi  → sit at the Hand → small talk → buy the round → ask about work
                           → walk back to the lift → [E] → the OTHER SHAFT row
```

With the chit in the wallet the sealed row **changes in place**, into #692's affordance and not a new kind of
button: `↓ THE OTHER SHAFT` · `🎫 opens for you` · `🎟 DAY-LABOUR CHIT · CARRIERS & CONTRACTORS · CAGE CREW ·
SHOW AT THE CAGE — the gate will read it`. Press it and the car goes down to **the cage band's top floor**
— B5 on this rock, and derived from Core's own band arithmetic (`BandTop(NextShaftBelow(…))`) rather than
typed anywhere, so a site shaped differently gets its own answer. When the doors open the panel's own voice
says it, once, last of the arrival's lines:

> The gate reads the chit the way a tired man reads a timesheet: date, crew, done. The cage takes you down as
> freight takes the cage — on somebody's account, no questions carried.

…and the field book keeps the reading: *"The chit works. Downstairs is a place you are now paid to be."*

**What must NOT change, and is worth checking on the same boot:**

- **without** the chit (`&roll=lo`, or just walk to the lift first) the row is `↓ THE OTHER SHAFT — SEALED` ·
  `🔒 sealed`, and pressing it still answers, in the panel, with the refusal it has given since #590.
- the chit opens **that** gate and no other: ride to B5, press the row for the shaft below it, and it is
  sealed exactly as it always was. A day-labour chit is cover, not a clearance.
- the band nobody listed stays unlisted. Carrying the chit down to B20 does not put a button on the panel —
  #592's silence is not a lock the chit can pick.
- the countersignature card still wins the row it shares: carry the card and the row names the **card**, and
  the arrival is still the card's own beat and its 3.0 nerve shock. No shock on the chit's — a gate that
  reads a timesheet and waves you through is the least frightening thing this building has done.

## 1b · ADDED (#751) — the CANTINA HALL, its cabinets, and the watch that decides the mood

```
/map?tablescene=1                      the hall, on whatever shift the sim clock is on
/map?tablescene=1&watch=2              …the HEAVING watch — most of twenty tables taken
/map?tablescene=1&watch=5              …the SMALL watch — a handful of souls and too much room
/map?secretlab=deep&land=1&floor=17    B17: the staff mess, at the size of the shift that never came
```

Owner, 2026-08-06: *"The Canteen is way too small… It needs to house like 80 customers… I am thinking like Mos
Eisley Space port size bar"*, then *"Definitely want to make the B1 bar be fancy ... and have cabinet-spaces
for sensitive negotiations"*, then *"The canteen for only staff can also be a lot bigger."*

**What changed.** The B1 canteen left the standard room grammar and is carved as a **HALL** — it stands on a
whole rib room-column, its front wall is the rib's own face, and its two doorways are the two gaps that
corridor already had (#585's one-gap law: the hall never cuts a door of its own). Twenty round tops in the
owner's 2/4/6 mix, **eighty seats**, THE COUNTER as a long bar wall at the far end, THE BOARD by the door, and
poured pillars breaking the sightlines.

**`?watch=N` is the new lever, and it is the only way to see the design.** Nothing in the game announces
whether the hall is busy: you walk in, and the room tells you. A watch is four sim-hours and six of them are a
day, so `watch=2` is the middle of the day and `watch=5` is the small hours. Compare the two and the mood is
the whole feature. It pins the watch index and nothing else — who is in and where they sat are still the
rota's own answer for that shift, so what you walk into is the room a captain would get.

**Three tiers at the tables.**

| tier | what pressing `[E]` gets you |
| --- | --- |
| the ten **named regulars** (#709/#717) | unchanged — the wave-in, the full #746 scene, the chit |
| **background patrons** (#751) | a THIN scene: small talk (one of fourteen barks, drawn per patron per watch), **buy the round** (the +1 applies normally), take your leave. No asks, no jobs |
| **cabinets** | empty — geometry plus a rule. #731's walkers will put somebody in one. **#757:** you may now TAKE one, and nobody ever comes to it, which is what a door is for |
| **an empty top** (#757) | **take the table** — see 1c below. Until #757 this was the one tier that answered nothing at all |

**The cabinets, and the one mechanic they carry.** Three enclosed rooms down the hall's back wall, plated
`CABINET n · BY ARRANGEMENT · ASK AT THE COUNTER`, six chairs each — **and their eighteen chairs are not part
of the hall's eighty.** Walk into one and you get **🚪 THE CABINET** (a card, once per excursion) and a line in
the field book. What it teaches, by observation and never by tooltip: #746's file-on-the-table is LOUD because
*"the counter has eyes"* — and a cabinet is a room the counter cannot see, so putting a file down in one does
**not** close ask-about-work. Same slip, same person, different room.

**First entry into the hall** raises **🍸 THE HALL** (once per excursion). It belongs to the branch office's
`CANTEEN 1` — the head office's dining room has its own register and its own arrival card (#411).

**B17's staff mess is hall-class too, and it is the same carve** — one implementation, two customers, and the
only line where they differ is the seat target. The mess's is derived: `ImpliedComplement(body)` = the floors
the directory **admits to**, times four heads a department. A twenty-storey clinic runs eighty people, a
five-floor annex twenty, and the band nobody listed has nobody on the books at all. **The mess is empty on
every watch, forever** — that is #743's sentence (*"the shift has not come"*) at architectural scale, and the
only new thing it says is its size, which it says by being walked across. **#757 leaves it alone on purpose:**
the mess is hall-class and full of tops, and not one of them offers "take this table" — a room outsiders are
not admitted to is not a room you sit down in, and Core's own B1 ruling (`CanteenRegulars.PeopleSitHere`)
decides that rather than a clause in the renderer.

## 1c · ADDED (#757) — taking a free table, waiting at it, and who comes of it

```
/map?tablescene=free                   boots standing AT a top with nobody at it — press [E] to SIT DOWN
/map?tablescene=free&approach=1        …and the next SIT A WHILE brings somebody across the hall
/map?tablescene=free&watch=5&approach=0  …the quiet watch: nobody is coming, and the sit is a SHORT REST (#783)
/map?tablescene=free&watch=2&approach=0  …the heaving watch, where the same sit is back-to-the-wall (#783)
```

Owner, live in the hall: *"I have empty table but I cannot sit down"*, and, minutes later, *"the normal way to
operate in a bar or restaurant is still not implemented."* Correct, and by omission: #746's press is **ask to
join**, so it needed a counterpart — and an empty top carried **no console at all**, which is why `[E]` there
did not refuse, did not answer, and never reached the dispatch.

**What to look at, in order.**

1. **Every top is pressable now.** A top with somebody at it says their plate (ask to join, #746); a top with
   nobody at it says `🪑 A FREE TABLE — SIT DOWN`. **One dot per TABLE, never one per chair** — a six-seat
   top and a two-seat top are one prompt each. The seat is the spot you walked to; nothing teleports you onto
   the furniture.
2. **`[E]` sits you down, and the panel says so FIRST (#783).** Before a single verb: *"You sit down. The table
   is yours."* Then your own plate, **a picture of the table you are at**, the chair count one short (you are
   in one of them), and two moves: **SIT A WHILE — see who comes** and **Stand up**. Owner, live and confused
   on the shipped build: *"What does the WAIT option mean here?"* — twice. It should not need asking now.
3. **TWO REGISTERS, and the room picks (#783).** On a busy watch the sit is a watch: back to the wall, hands
   where they can be seen, over `art/b1-your-own-table.jpg` — the empty chair opposite, which *is* the wait
   beat. On a **quiet** watch (`&watch=5`), or with a **pour bought at the counter still in your hand**, it is
   a **SHORT REST**: boots up on that same chair, over `art/b1-short-rest.jpg`, and standing up afterwards
   says something different too. Buy a drink at `?counter=1` first and even the heaving watch turns.
4. **SITTING A WHILE is the verb.** Owner: *"Suppose I just want to sit down and wait to be disturbed?"*
   Sitting alone is a choice to be **findable**. Each press is a beat inside the frozen watch (the shift never
   moves — the drawn room and the pressed room stay one room), and the room answers.
5. **The wrong watch is a scene too.** `&watch=5&approach=0`: press it as often as you like and the hall
   tells you, in different words each beat, that nothing is going to happen. A **busy** hall's silence and an
   **emptied** hall's silence are different sentences and never borrow each other's words — that is the whole
   beat, and it is the one thing most likely to be mistaken for a bug.
6. **The approach inverts the roles.** `&approach=1`: a haulier with her coat still on crosses the hall and
   asks for the chair. Owner's own shape — *"1. ask to sit down, 2. maybe offer to buy me a drink, 3. tell me
   what they have in mind… think Gandalf knocking on Bilbo's door."* Pull the chair out, let her buy (or turn
   it down — **either answer opens the next rung**, because refusing a drink is still having heard the offer),
   then ask what is on her mind. **Not tonight** sends her away for free and leaves you the table.
7. **What she wants lands on something that exists.** Her lead points at the Hand who has been here longest,
   who writes the names, on the chit the cage's gate already reads (#746 → #752). It goes in the field book.
8. **A cabinet is the opposite choice.** Take a cabinet top and wait: **nobody ever comes**, on any watch, at
   any beat — and the lines say why without stating a rule. The counter has eyes everywhere except in there,
   and so does everybody else.

## 1d · ADDED (#784) — sitting down is a STATE: it shows, it constrains, and it heals

```
/map?tablescene=free&approach=0&nerve=low&hurt=3          take the table and WAIT — watch the short rest work
/map?tablescene=free&approach=0&nerve=low&hurt=3&watch=5  …in the emptied hall, where the wait is all there is
```

Owner, live over the #778 table, three rulings in the same minutes: *"Let's make the graphics say I am
sitting down at the avatar level — like different graphics etc."* · *"before moving I have to stand up… so if
I try to move when sitting down it should ask with a pop-up whether I want to stand up again."* ·
*"Sitting down relaxes and heals"*, and then the anchor: *"it is like short rest in TTRPG."*

`&nerve=low&hurt=3` is not decoration. A recovery mechanic shown to a steady, unmarked captain demonstrates
nothing at all — the relief seam is honest and gives nothing back to somebody who has lost nothing, so the
whole feature would look like a control that did not fire.

**What to look at, in order.**

1. **The figure changes.** Before you press `[E]`, the captain is a filled circle with a spoke pointing where
   they are walking. After: **no spoke** (you are going nowhere), a smaller body (folded into a chair), a bar
   behind the shoulders (the chair back) and a short bar in front (arms on the table). Nothing in the panel
   has to say "you are sitting" — the deck already did.
2. **W does not walk you out of your own table.** Press any movement key: the captain does not move a
   centimetre and a small confirm goes up — *"Stand up? You will lose the table."* — with the cost said
   underneath while there is still a rest to lose. `Esc`, the backdrop and **Stay where you are** all keep
   your seat; **Stand up** (or `Enter`) stands you up and the table is gone.
3. **WAIT is a short rest.** Each press eases a whole nerve pip — watch the corner gauge and the **nerve
   ledger**, which names it — and the panel adds one clause after the room's own silence line. On the third
   beat one of the five blows knits: the condition marker under the gauge goes *badly cut* → *bleeding*.
4. **A short rest is SHORT, and the game says so.** Keep pressing. The nerve stops moving at the ceiling and
   the panel tells you why rather than handing back a beat that silently did nothing. One blow per watch and
   never two: the rest of you comes back in the ship's bunk, which is the long rest.
5. **The pour pays in tempo.** Boot `&counter=1` instead, buy THE LOCAL POUR, then walk to a free top and
   sit: the same rest lands in half the beats. It multiplies the RATE and never the ceiling — the glass buys
   the rest *before the room takes it off you*, which matters because #757's haulier can walk into the middle
   of it. Three tots and it stops helping, which is the game's one drunkenness law and not a second opinion.
6. **Writing properly is seated-only.** Open the satchel (`I`) standing up, on a document: the ✍ control
   refuses out loud and names the register you DO have — photograph it and leave it, which is #696 untouched.
   Sit down and press it again: the book takes the entry in your own hand **and the sheet stays in your
   pocket**, which is the whole of what a table buys.

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
| `&watch=N` | pin which SHIFT the hall is on (#751) — `2` heaves, `5` echoes, six to a day |
| `?tablescene=free` | boot standing at a top with NOBODY at it (#757) — plated **SIT DOWN**, and the panel wears the table (#783) |
| `&approach=1\|0` | force whether sitting a while brings somebody over (#757); `0` is the told nobody-came beat |
| `&nerve=low` | start the captain rattled (#784) — two pips, so the short rest has something to give back |
| `&hurt=3` | start already marked (#784) — three of the five blows landed, so the healing half is watchable |

## What has NO link yet, because it is not built

Filed tonight and deliberately absent from this document — adding a link for an unbuilt mechanic is how a
testing guide starts lying:

- **#719** the service stair / second way out — *and it must ship before anything is allowed to stop the car*
- **#618** guards on the bottom floors, the noise trigger, the talk risk
- **#715** illegal heat, owed per entity
- **#718** the rollback, the coerced job, recognition
- **#720** the batch ending
- the **staff cantina's own people** (B17 above is furnished and empty on purpose)
