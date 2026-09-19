# Test links — the 2026-09-17 run (#161, #1199 follow-ups)

*Companion to [`testing-guide.md`](testing-guide.md) Appendix A. Every link boots the live build at
`https://esoinila.github.io/SpaceSails-play/map?…` straight into the situation, using only cheats that
exist in the query whitelist. Read Appendix A for what each key does.*

> ## 🎮 Played, not derived — the 2026-09-17 sweep
>
> Several of the rows below were written from the code and **never opened in a browser**; the crews that
> shipped them said so. Every row now carries a **`played`** column, filled by booting the link in a real
> headless Chromium against a Release publish of `our-own-ship-has-compartments` @ `6692af54`, screenshotting
> it, reading the console, and driving the row's own "what to do" as far as keys and clicks reach.
>
> | mark | means |
> | --- | --- |
> | ✅ `09-17` | booted, driven and **seen** to do what the row says |
> | ⚠ `09-17` | booted and seen, with a caveat named in the row |
> | ❌ `09-17` | booted, driven, and it **did not** do what the row says — issue filed |
> | 🚫 `09-17` | could not be driven from a headless tab; the reason is named in the row |
>
> **A mark is only ever put here for something somebody actually looked at.** A row with no mark is a row
> this sweep did not reach. Timings are deliberately absent: an automated tab is `document.hidden`-adjacent,
> rAF-throttled and shares a machine with other lanes, so its numbers say nothing about the game.
>
> ### 🔁 …and the re-play, 2026-09-18
>
> Five fix PRs landed on the back of that sweep — **#1221** (the boot cheats and `simhours` order), **#1222**
> (the stranger bond's room scope, and a held pulse under a scrim), **#1223** (the dark-web desk deals at a
> berth), **#1224** (the Nav toolbar clear of the chip column). **A guard is not a play**, so every fixed row
> was opened again in a real headless Chromium against a Release publish of
> `our-own-ship-has-compartments` @ **`1bb0748a`**. A `09-18` mark is that re-play; a `09-17` mark is the
> first sweep and was not re-driven. Two issues that never had a row of their own were re-played too:
>
> - **[#1216](https://github.com/esoinila/SpaceSails/issues/1216) ✅ `09-18`** — `/map?scenario=not-a-real-scenario`
>   now comes up on Sol's own front door with the berth picker instead of the error page, and the app raises
>   nothing; the only console line is the browser's own `404` for the file that genuinely is not there.
>   `/map?scenario=sol-eu&start=wreck` opens the Ringside Exchange picker with no exception.
> - **[#1219](https://github.com/esoinila/SpaceSails/issues/1219) ✅ `09-18`** — at **1440 × 1100** with the
>   plotting table open the Nav toolbar wraps and **`▶ Play` sits at x 20–96 on its own row** with `?` and
>   `📋 checklist` beside it; `elementFromPoint` at its left edge, middle **and right edge** all return the
>   `▶ Play` button. The desk-status chips start at x 1280 — 1,142 px clear. (Before: `▶ Play` at x 1218,
>   its right third under the Sensors chip.)
>
> Four rows already marked ✅ were sampled again for regressions and none had moved: `?converge=1`'s two
> sentences and closing line, `?nopattern=1&death=impact`'s one sentence and its single `Close the book`,
> §3's `P` tip and toolbar, and the boot narration's clean console.
>
> ### 🔁🔁 …and the second re-play, later on 2026-09-18
>
> Three more fixes landed overnight on the back of the first re-play — **#1231/#1233** (the man takes a POST
> inside the room, by the door he came in by, and the `?parcel=1` cheat hands the shovel the box),
> **#1232** (the held pulse is a QUEUE) and **#1227** (`E` on the regolith buries the parcel). Every row
> they touch was opened again in a real headless Chromium against a Release publish of
> `our-own-ship-has-compartments` @ **`3ce9d41f`**, plus three previously-✅ rows as a regression sample.
> Marks written in this pass carry `09-18` and say what was seen. Nothing in this pass was judged from the
> code: a row with no screenshot behind it did not get a mark. Console was clean and no request failed on
> every link driven.

Two owner rulings from 2026-09-17, both of them "what shipped is right, the *clock* on it is wrong" —
and two more the same morning, on the two story passes that had been waiting for a decision (§4).

---

## 0 · THE NEWSPAPER WITH EYE HOLES — AND THE BAR-ROOM LINE STAYS IN THE BAR (#1199, 2026-09-19)

Owner, live on the T: *"wow the tailed one disappeared :-D … I really like the tables there… I think the
tailed one should go to the observation deck **even if I am there before they arrive**. It is the classic
sit at a café with a newspaper with eye holes gumshoe cliché :-D"* — and, the same afternoon, *"They should
act normal **even if I tail from ahead**"* and *"another vending machine or something else that only
**momentarily** blocks the view."*

**So the hold is gone and the rule is one sentence.** There is no two-pace refusal at the throat any more
and no leg back out of it (`ObservationWalk.TooCloseToGoInDu` no longer exists). He walks his errand into
the hat whatever the captain is doing, stands at the rail and looks out — and **he is off the floor on the
first LOOK nobody is watching him.** The look clock is the game's own
(`ReeverObservation.LookIntervalSeconds`, 0.75 s). While he is WALKING it takes losing sight of him; once he
is STANDING at the rail it takes only your eyes. Four ways to answer it, all of them already in the room:

| the way | what it is |
|---|---|
| **the paper** | you are SITTING at one of the hat's two tables. Sitting is the cover; the seat panel's own **Read the news** is the eye holes, and nothing had to be wired to it |
| **the eyepiece** | any card is up — the 🔭 **COIN BINOCULARS**' own among them, or a 🥤 vending card, or the satchel |
| **the tube** | you are at the mouth or down the leg, and the throat's corner takes the line |
| **the island** | *new* — a **third 🥤 VENDING MACHINE**, standing on its own in the middle of the gallery floor, on the room's axis. Of the 8.92 du he walks from the throat to the rail, **4.13 du are out of the SOUTH table’s sight** — one look at a walker’s pace is 1.50 du — so he goes behind a machine and does not come out the other side |

**And if you never look away:** nothing happens to him. He finishes the view, turns round and walks back out
past you — **no card, no note, and nothing spent.** The walk is still there on your next visit. (That is the
one thing #1245 got wrong: it spent the beat as a punishment for standing too close.)

**The card's reach:** the rail, as always — except for a captain who was **sitting at a table** when the man
went, who gets it where he is sitting. He has already arrived; sending him four paces to be told the room is
empty would be asking him to check what he is looking at.

### The stakeout route — sit first, then wait

| What to do | What should happen | Broken looks like |
|---|---|---|
| `/map?dock=selene-gate&ashore=1&simhours=7.5` — go straight out west, through the tube and into the gallery, and **take the NORTH table** ([E]). Then just sit there. | GILT-EYE gets up after last call and **comes in anyway** — past you, across the hat, out to the rail, and stands looking out. Within a look or so of him settling you glance up and **he is not there.** Wait three minutes at warp 1 and the card comes up **at your table**; the note files under his name, once. | He stops at the throat and stares at you; or he turns round and goes back to his drink; or the card never comes because you are not standing at the rail. |
| Same link, but take the **SOUTH table** instead. | Same beat, one step earlier: he comes through the throat and **walks behind the third vending machine** — the one standing on its own in the middle of the floor — and does not come out the other side. Card at your table after the wait. | You watch him all the way to the rail with nothing between you (the island machine has been moved or lost), or he holds at the throat. |
| Same link. Follow him in on foot and then **stand at the north table's chair and stare at him**, without sitting and without opening anything. | He reaches the rail, stands there for the whole three minutes, then **turns round and walks back out past you.** No card, no note — and the beat is **NOT spent**: cast off, come back, and the walk is there again. | He vanishes in your face; or a card comes up for a scene that did not happen; or the beat is gone for the rest of the voyage. |
| Same link. Stand at the rail-end table and press **[E] on the 🔭 COIN BINOCULARS** while he is standing at the rail. | The card goes up, and when you close it **he is gone.** Eyes off the world is eyes off him, whatever your feet are doing. | He is still standing there, or the card charged you and nothing changed. |
| Same link. Follow him down the tube and **stop at the mouth**. | He turns into the hat and is **gone there and then** — the throat's corner takes the line. You walk in and the gallery is empty; the card comes at the **rail** after the wait, exactly as it always did. | He stands at the opening looking at you for ever; or the card comes to you at the throat without walking out. |
| Stand out on the **concourse** — outside THE EARTHRISE BAR — and wait for the off-deck buzzer. | Nothing. The unexplained-signal beat only speaks in the **bar**. | *"Behind the counter the staff go still as one and trade a single glance… The drinkers never look up."* said to a captain standing in an open concourse with no counter in it. |
| Walk back into the bar and wait. | The buzzer still sounds there, unchanged, prose and all. | The beat has been scoped away from the room it is about. |

**One thing that is measured and worth knowing:** the **north** table keeps a clean view of the rail, and the
island machine does not change that. The two tables sit **69°** apart as seen from the rail (wider further in), and a machine this size screens at
most **53.1°** from as close as a body may stand to it — **no single fitting can stand between him and both
tables at once.** The north table is the good
seat, and what loses him from it is the paper or the eyepiece rather than the furniture.

## 1 · The observation walk, at a wait you can sit through (#1199, PR #1201 follow-up)

Owner, on the walk as #1201 shipped it: **"YES, shorten the wait"** — it was
`Escort.PatienceSeconds`, a quarter of the bar's own watch, and every NPC-patience clock in this game is
**sim** time. At warp 1 that is **one hour of the player's own evening**, spent standing at the mouth of an
empty tube with nothing to do. The walk now has its own named fraction, `ObservationWalk.WaitFraction`, at
one eightieth of a watch: **180 s of sim time — three minutes at warp 1.**

Everything else about the beat is unchanged, deliberately: the walk still keeps Selene Gate's **edge 5**
(the owner's other ruling the same day — the MEDBAY panel stays spent), it is still walked **after last
call**, the card, the note through the one funnel, the spent-once key and the later pulse are all verbatim,
and **no other patience clock moved** — an escort still stands in a cabinet doorway for a full hour of
station time.

| what | link | what to look for | played |
| --- | --- | --- | --- |
| **The walk is empty, and you can play the wait** (#1199 / #1062, this PR) | `/map?dock=selene-gate&ashore=1&simhours=7.5` — clamped at Selene Gate, ashore in **THE EARTHRISE BAR**, with the station clock seven and a half hours in so the room is **past last call** (the walk is only dealt in the last quarter of a watch; last call is at 10,800 s of 14,400). The person of interest at this berth is the haven's own named regular, **GILT-EYE**, and the rota has him at the bar on this watch. | He gets up from his top and crosses the concourse **due west**, out onto `OBSERVATION WALK` — glass floor, the limb under it. Follow him: stay inside legible range with a clear line and **he stops and waits for you to go past** (no card, no line — he just turns). Break the line, and on a frame nobody is looking at him he is simply **not on the floor any more**. Then walk out to the rail yourself. **At warp 1 the card comes up about three minutes after the last moment you had eyes on him** — it used to be an hour. The card is the absence; the note files under his own name on THREADS; nothing is pulsed over the card. | ✅ `09-18` — **played, whole, on the repointed link**: seated at his top, up after last call, across the concourse due west, into the tube; gone off the floor on a frame nobody was looking; the card at the rail after the wait, verbatim, nothing over it.<br>🔁 **Re-sampled `09-18` on 3ce9d41f and the first two thirds are unmoved**: GILT-EYE is at his top at boot, he gets up after last call and goes due west into the tube, and when the line is broken he is simply not on the floor any more. **The card itself was not reached this time** — from the gangway the captain would not line up with the tube's mouth on the keys, so this sample stops at the mouth; the card's own verdict stands on the run above |
| **THE WALK IS A T — and there is a cafeteria at the end of it** (#1199, 2026-09-18, this PR) | same link. Walk the tube all the way out. | The tube no longer ends at a rail: it opens across its whole width into **the GALLERY**, and there is still **no second door**. **24 du of outer glass** — ten people abreast with room to spare — two paces deep, the glass floor running on underneath with its own plate. **The rail is not opposite the tube.** Stand at it and look back: the way in is **out of sight**, and from the mouth you cannot see the rail either. That is the point of the shape — it is why GILT-EYE now vanishes instead of holding at the mouth while you watch him down a straight pipe. His route ends **in the gallery** and he comes off the floor there. Against the back wall: **two 🥤 VENDING MACHINEs** and **two steel tables** you can take a seat at ([E] — the same sitting a bar top gives, but the panel says *a top in OBSERVATION WALK* and wears **the gallery's own picture**, because once you are sitting the rail and the Earth are what is in front of you). On the rail: **🔭 COIN BINOCULARS**. The two tables are **stakeout seats** — from either chair you have a line to the opening the leg comes in through, which is the only way anybody can arrive. | — |
| **Two coins, and the optics swing** (#1199, 2026-09-18, this PR) | same link. Go to the **🔭 COIN BINOCULARS** on the rail and press **[E] twice**. | Each press takes **4 cr** — a tenth of the local keep's own round, so it is per-haven and not a typed-in price — and raises a card. **The first look is OUT**: the grey, the Earth, and one line of tracks going out that does not come back this way. **The second swings DOWN** through the glass floor: the crater wall into shadow, a buried rail still carrying its lamps, and at the bottom a hatch nobody lists with its lamp on. **It alternates and never rolls**, so press again and it goes round. Neither card explains anything, and nothing is named. The book gets **one** gist, once, filed under the PLACE rather than under a person. Then try a **🥤 VENDING MACHINE**: same coin, one card, *"…somebody stocked it."* With **fewer than 4 cr** in the purse both refuse **off-plate** — a pulse, no card, no charge, in the souvenir kiosk's own words. | — |

**Reading the wait off the clock while you play:** the wait is measured from the last frame he was visible,
in **sim** seconds, so the warp slider is the fast-forward if you want it — but the point of this change is
that you should not need it. Sit at the rail at warp 1 and the beat lands inside a normal pause.

**What would say this regressed:** the card arriving the instant you reach the rail (the wait stopped being
waited), or the watch turning over before it arrives at all (the wait grew back past the quarter-watch of
room the room has left after last call).

> ❌ **Played 2026-09-17 and it does not play — [#1213](https://github.com/esoinila/SpaceSails/issues/1213).**
> The `&simhours=4` in the link is the thing that breaks it. `?dock=` clamps the ship *before* `?simhours=`
> jumps the clock, so the bar's watch is frozen at **watch 0** whatever hour you ask for — and the jumped
> clock then lands past **every** scheduled departure at once, so the room deals its whole evening in the
> first frames and **GILT-EYE is out of his chair ~1.5 s after boot**, before a tester can see him. Booted
> side by side at the same settle: without `simhours` the tops read `Coil · MADAM COIL` **and**
> `Gilt-Eye · GILT-EYE`; with `&simhours=4` they read `MADAM COIL`, `Silas` at the cellar door, and no
> GILT-EYE at all. Played on for 105 s at warp 1 at the documented URL: nobody crosses the concourse, the
> walk stays empty, no card, no note, and nothing is ever said out loud. The walk itself is fine — the room,
> the tube, the plate and the rail are all where the row says, and the captain can walk out to the rail; it
> is only the *person* who is missing. **On the build that was played, no `simhours` value could work**,
> because the frozen watch was pinned to 0 and GILT-EYE is one of watch 0's leavers, so any clock past last
> call was also past his departure.
>
> ✅ **Fixed in #1221 — re-play pending.** `?simhours=` now moves the clock *before* the berth freezes its
> watch, so the room is the room of that hour. The link in the row above has been repointed to
> **`&simhours=7.5`** and the reason is the second half of the same bug: four sim-hours is *exactly one
> watch*, so `simhours=4` lands a captain at the **start** of watch 1, where nothing is past last call and
> the walk is correctly refused. 7.5 h is the first hour that is past last call **and** on a watch the rota
> seats GILT-EYE on **and** the shift does not walk him out of. It is held by a guard that drives the
> shipping metabolism from that URL's boot state and finds him afoot on the route to the rail — but a guard
> is not a play, so the ❌ above stands until somebody opens it in a browser.

> ✅ **Re-played 2026-09-18 in a browser, and the whole beat plays.** Booted headless at
> `?dock=selene-gate&ashore=1&simhours=7.5` against a Release publish of `our-own-ship-has-compartments`
> @ `1bb0748a`. **GILT-EYE is at a top in THE EARTHRISE BAR at boot** (his name over a figure in the
> south-west of the room, the barkeep and MADAM COIL where the rota puts them). He does not move while the
> captain has a line to him — that is slice 1's own *"he stops and waits for you to go past"*, and it is the
> first thing a tester meets: **stand still in the bar and nothing happens, for as long as you like**. Walk
> away and he gets out of the chair, crosses the concourse **due west** and goes out onto `OBSERVATION WALK`;
> follow him in and he holds again at the mouth. Break the line — walk back out of the tube and down the
> concourse — and on a frame nobody is looking at him **he is off the floor**. Walk back out to the rail and
> the card comes up:
>
> > **👁 THE OBSERVATION WALK** — *"The walk is lit the whole way out. The floor is glass and the drop is
> > under it, and the far end is a wall with a rail. There is nobody here, and there is nowhere here to be."*
> > — one button, `…go on`.
>
> Verbatim against `ObservationWalk.CardTitle` / `CardBody`, the plate art under it, **nothing pulsed over
> it**, nothing clipped. **The wait is waited**: a screenshot taken at the rail before the card shows the
> captain standing at the blind end with an ordinary ambient line on the HUD and no card — it arrives later,
> which is the half of #1201's follow-up that #1199 was re-opened for. The book then holds the note,
> verbatim, in the captain's ledger under `🍺 Selene Gate · THE EARTHRISE BAR`:
> **👁 *"Followed GILT-EYE out onto the observation walk. One way in, glass underfoot, the drop below. Waited
> at the mouth. Went in. Nobody there."*** Two things this re-play did **not** see, so nobody should read
> them here: the **spent-once** re-entry (the second walk-in was driven with the Captain's desk open, so the
> captain never moved) and the **later sighting** at another counter.
>
> Screenshots: `D:/repo12/wt/replay/.qa-scratch/r1j-j5_card.png` (the card), `r1j-j4b.png` (at the rail,
> before it), `r1b-t000.png` (seated at boot), `r1m-m2_ledger.png` (the note).

> ⚠ **The room above is the STRAIGHT TUBE, and that is now history.** Everything in the re-play still holds
> word for word — the card, the note, the wait, the hold at the mouth — but the geometry it was played in
> changed the same afternoon (the owner, live: *"a tube, then an area to view… like the letter T"*). The two
> sentences that have moved: *"he holds again at the mouth"* is exactly the stall the T was built to fix —
> the man could see all the way down a straight pipe — and *"walk back out to the rail"* is now a walk into
> the **GALLERY** at the far end, where the rail is out of the mouth's line. The re-play above has not been
> re-run against the T; the two rows at the top of this section are what to open.

---

## 1b · Losing YOUR tail — #1062's mirror half (this PR)

The other half of the same issue: *"or trying to lose a tail our selves :-D"*. Somebody mundane and human is
behind the captain, ashore, and **nothing in the game says a word about him** until the captain works it out.
Full feature note: [`features/losing-the-tail.md`](features/losing-the-tail.md).

| what | link | what to look for | played |
| --- | --- | --- | --- |
| **Somebody came in after you** (#1062 slice 2) | `/map?tailed=1&ashore=1&dock=selene-gate` — ashore in **THE EARTHRISE BAR** with the dev row on. The row forces only WHETHER; the world's own route in is an outfit's #715 folder at the band where it wants a face (a femme-fatale walk-in that turned out to be a setup, or a compromising chip sold at a dark-web desk). | A grey figure follows you in through the bar's north door and settles **nine to thirty deck units behind you, with a line to you, and no name over him**. He never goes to the counter. **Nothing is pulsed, nothing is carded, nothing goes in the book.** #1233 · in a room he takes a POST instead of a band — against the wall, nearest the doorway, never at the counter and never at a top — so look for him **by the door** rather than at a distance. | ✅ `09-18` — **#1233's post re-played and it is what it says.** He is dealt on the CONCOURSE side of the bar's doorway and walks in after the captain (caught outside at 0.2 s, inside by 1.5 s), then takes a standing place **against the bar's south wall, one body clear of the stone, beside the door he came in by** — never the counter, never a top, no name over him — and he keeps it while the captain crosses the floor. Same shape at **The Tilt**. Nothing pulsed, carded or filed |
| **The chair that faces the door** | same link. Walk to a top with a clear line back to the bar's doorway and press `[E]` to take it. | After about **nine seconds in the chair**, one pulse: *"From this chair you can see the door. So can the man who came in after you, and he has not ordered."* Stand on your feet in the same room for the same nine seconds first and **nothing happens** — the sit is the whole cost. | ✅ `09-18` — **#1229 is fixed and the chair pays off.** Walked to the top due north of the bar's doorway, took it with `[E]`, and the sentence came up on the glass **verbatim, once, whole, with nothing over it** while the docked strip still read `Stand up` — and it was gone again when looked for 25 s later. Re-played at a second haven, `dock=the-tilt`, same top, same sentence, same once |
| **The same coat, two doors running** | same link. Walk out of the bar, across the concourse, and **out onto `OBSERVATION WALK`** (edge 5, due west — #1199's tube). **2026-09-18: stop at the FAR END OF THE TUBE**, where it opens into the gallery — that is where the blind end used to be. | He follows you through the bar's doorway and then to the mouth of the walk, which is the only place in a one-way room with a line to you. Two DISTINCT doorways is the tell: *"The same grey coat, two doors running. Nobody's errand takes them through both."* A locked cellar leaf never counts. | ⚠ `09-18` — **the tell fires, verbatim, but only if you actually DOUBLE BACK.** Walking straight out and on into the tube (click-to-walk, or keys with 5 s pauses) **outpaces him**: he is still crossing the hall when the tube's stone takes the line, his blind clock runs out, and he leaves without a word — which is the feature's own losing rule, not a fault, but it is not what this row's "walk out … and out onto OBSERVATION WALK" describes. What lands it: stand at the mouth until he has closed his band, then step **into** the walk and back **out** once. On that first step-out he is standing in the mouth with a line to you and the sentence comes up whole <br>📐 **2026-09-18, the walk is a T:** the far end of the leg is the throat into the **GALLERY**, and the sequence above is unchanged there. Keep going, out to the **rail in the crossbar**, and he loses you instead — that stretch of glass is out of the mouth's line by construction, which is this row's own losing rule finally getting a room worth using it in. |
| **Losing him** | same link. Walk back **down your own gangway** toward the ship, and stay there. | He will not follow you down the umbilical. After about **nine seconds with nothing to look at** he gives up: *"The corridor behind you is only a corridor. Whoever it was is asking the wrong floor about you."* — and the field book takes one line, filed on **THREADS under the PLACE** (📍 SELENE GATE) and never under a name: *a tail, lost at … — a grey coat, never a face*. | ✅ `09-18` — **played on the walk's own stone rather than the gangway** (same rule, nearer to hand): after the two-door tell, walk west up the tube and the line breaks; the sentence comes up whole and readable — *"The corridor behind you is only a corridor. Whoever it was is asking the wrong floor about you."* — and the Captain desk → 📜 Ledger then holds exactly one new line, filed under the PLACE: **🥾 Selene Gate · THE EARTHRISE BAR — 👁 a tail, lost at Selene Gate — a grey coat, never a face**. Noticed him first and you are told; never noticed him and nothing whatever is said, which is also what three of this sweep's runs saw |
| **Failing forward — the burn** (#1062 slice 2b) | same link, plus a quiet verb at that berth while he is still on the floor: **take the favour** at a `◈` contact's table (press `B` at a table where the account is on offer), or **buy the fence's key** at the Comms desk's dark-web board. **#1062 slice 2c:** the berth's THIRD quiet verb, the unlisted parcel's row (#711 slice 2), now answers to the same burn — see §5's own row for it. | **Nothing happens.** No line, no card, no warning — you get what you came for and walk out. Cast off, come back, and the port has **no favour, no fence row and no parcel row at all**, and the place says one thing: *"Tidy, in the way a place is after somebody has been through it first."* The book takes *SELENE GATE — walked before you got there, by somebody who knew where to walk*, filed under the **same PLACE** as the losing note so THREADS stacks the evening in order. Shake him first and the same verb costs nothing. | ⚠ `09-18` — the **quiet verb now plays**: with #1223 the fence's row is reachable at the berth and `Buy the key · 1,011 cr` takes the coin (1,500 → 489 cr), the row goes, and **nothing is said** — no line, no card, no warning, nine seconds of watching the HUD for it. The cast-off-and-return half is still not drivable headless |
| **…and the half that must look exactly the same** | `/map?tailed=0&ashore=1&dock=selene-gate`, and every other link in this file | **Nothing.** No man, no lines, no book entry — and the drawn frame is byte-identical: the frame-hash ledger did not move by one row in this lane. Slice 1's own beat (GILT-EYE and the empty walk, §1 above) plays exactly as it did. | ⚠ `09-17` — no man, no line, no entry with `tailed=0`; frame-hash ledger not re-run here. (§1's own beat is now ✅ `09-18`, played whole) |

**Reading the clocks while you play:** both of this half's clocks are counted in **real** seconds off the
frame stamp, not in sim time — twelve of the game's own looks at `ReeverObservation.LookIntervalSeconds`,
which is nine seconds whatever the warp slider says. The warp slider is not the fast-forward here and cannot
be used to cheat either direction.

**What would say this regressed:** a line about a tail arriving before you have done anything to earn it (the
whole feature is that it does not announce itself); a name drawn over the grey figure; him following you down
the gangway, or standing at the counter; him giving up the instant you step behind a wall, or never giving up
at all.

> ✅ **Played 2026-09-17, and this half plays.** The man is on the floor with `tailed=1` and gone with
> `tailed=0` — one extra unnamed grey figure, off the counter, inside the band, and not a word said about
> him. All three of his lines came up verbatim, in this order: the chair reading after the sit, *"The same
> grey coat, two doors running…"* on the way west across the concourse, and *"The corridor behind you is only
> a corridor…"* on the gangway. The book then held exactly one entry, **👁 a tail, lost at Selene Gate — a
> grey coat, never a face**, marked *loose end* and filed under the place with no name on it.
>
> Two things found while sitting through those clocks, both filed rather than fixed:
> [#1214](https://github.com/esoinila/SpaceSails/issues/1214) — the chair reading can be **pulsed behind a
> story card's backdrop** (the finder crosses the room on the same nine seconds) and is then spent, drawn
> and unreadable; and [#1215](https://github.com/esoinila/SpaceSails/issues/1215) — the #429 stranger-bond
> fires **out on the concourse and inside the walk**, hands the captain a cognac "on the counter" and ticks
> the tot ledger in a room with no counter in it. ✅ **Both fixed in #1222 — re-play pending:** a
> plot-significant pulse raised while any scrim is up is now held and said on the frame the glass clears, and
> the bond is scoped to the bar's own floor by the same predicate every other beat in that room uses.
>
> ✅ **[#1215](https://github.com/esoinila/SpaceSails/issues/1215) re-played 2026-09-18 and it is fixed.**
> `?bond=1&ashore=1&dock=selene-gate` forces every scare to bond. **In the bar it still fires**: the first
> shudder brings up *TWO GLASSES, AND THE SHUDDER STOPS*, the cognac line, and the Galley chip ticks to
> `1 tot poured`. Then walk west out of the bar and stand **inside the observation walk**: over 130 s
> **three** scares landed — the shudder, the caution PA and the distant tone, all still spoken — and there
> was **no card, no cognac and no second tot** (the ledger stayed at the one poured in the bar). Screenshots
> `r1215a-bar.png`, `r1215b-after.png`.
>
> ⚠ **[#1214](https://github.com/esoinila/SpaceSails/issues/1214) — the bug's signature is gone, but the
> released sentence was not seen.** With `?tailed=1&bond=1` the captain was sat at a top and the finder's
> card (*Ilse Varga*) plus the bond card both won the race: through both scrims **the chair sentence was
> never in the DOM**, which is exactly the opposite of what #1214 filed (the sentence drawn at (400, 96)
> under a z-1320 backdrop). What this sweep could **not** do is watch it come out the other side: the chair
> reading never fired at all in five runs — including a **no-card control at the same seat**, which is why
> this is a caveat about the play and not a claim about the fix. Whoever plays it next needs a top the grey
> coat is actually in sight from.
>
> ❌ **2026-09-18 — the cause is found, and it is not #1222 and not the seat.**
> [#1229](https://github.com/esoinila/SpaceSails/issues/1229). Re-played headless against a Release publish
> of `our-own-ship-has-compartments` @ `b668bd08` with **no card up at all**: the captain sits at a top the
> door is plainly visible from and the sentence never enters the DOM in seventy-five seconds. Reproduced
> frame by frame in the client bench, booting ashore the way `?ashore=1` does and taking the seat through
> the shipped `[E]`: the exposure clock reaches **2.0 s** and resets to zero, because the man **settles
> outside the room** — `TheSpotBehindYouAt` refuses only the gangway line, so at Selene Gate
> the first bearing the stone allows for a captain anywhere on the doorway's own column is **due south,
> nineteen units out onto the concourse**. The bar's south wall is between them within three seconds, his
> blind clock runs, and at nine seconds he gives up and leaves — silently, because he was never noticed.
> The chair is fine (five of the room's seven tops have a clear line to the doorway); the man's standing
> place is not. **Not fixed here:** every narrow repair trades this beat against the two-door tell, whose
> own guard is green only because the man was standing outside the door already. Design call needed.
>
> ✅ **Fixed in #1233 — re-play pending.** The design call, made and shipped: **in a room off the ring he
> takes a POST** — inside it, against the room's own stone, with a line to you, nearest the doorway he came
> in by, never the counter and never a chair — and he keeps his band only out on the ring, where the room is
> measurably big enough to hold one. He is dealt on the concourse side of the doorway and walks in after you,
> and when you leave he waits one look and follows you through the same doorway, which is what makes the
> two-door tell an honest sequence instead of an accident of where he was planted. Measured after the fix:
> **the chair reading fires from every top a captain can take that sees the door, at all seven haven bars in
> `sol.json`, nine seconds after the sit.** What to watch for on the re-play: he should be standing **by the
> door**, not out on the concourse, from the moment you turn round.
>
> ⚠ **…and #1222's hold has a second hole of its own, audited in the same lane:**
> [#1230](https://github.com/esoinila/SpaceSails/issues/1230). The release itself is healthy (it runs in
> `OnTick` above every early stop but the jump freeze), but `PulseHold` keeps **one** line, so a second
> plot-significant beat raised behind the same open card annihilates the first — and the two lines this half
> says are pulse-only and spent once, so one of them can be destroyed without a trace anywhere in the save.
>
> ✅ **Fixed in #1232 — re-play pending.** The hold is a QUEUE now: every line at the telling floor and above
> is kept, in order, and said one at a time as the slot frees up, each for its own full dwell. Rank no longer
> DROPS a line — it only orders lines raised in the same frame, highest first — an identical sentence already
> waiting is not queued twice, and the bound is soft so it can never cost a beat. What to watch for on the
> re-play: leave a card open, let **two** once-only beats fire behind it (the chair reading and then the
> losing line is the case this was filed for), close the card, and read **both**, in the order they happened.
>
> ✅ **[#1230](https://github.com/esoinila/SpaceSails/issues/1230) re-played 2026-09-18 and the queue holds —
> two once-only beats behind one card, both said, in order, neither lost.** The case was driven on
> `?tailed=1&bond=1&ashore=1&dock=selene-gate`: stand in the bar until the forced scare opens
> **TWO GLASSES, AND THE SHUDDER STOPS** (one scrim up, and the legs still work under it — the card survives
> every key press because there is no seat to stand out of), then walk the whole tail sequence with the card
> still open. Behind that one backdrop the game raised **the two-door tell** and then, nine seconds of stone
> later, **the losing line** — and through the whole of it neither sentence was ever in the DOM. Press
> `…go on` and the glass clears on the FIRST one, *"The same grey coat, two doors running…"*, whole and
> readable; six seconds later the slot frees and the SECOND arrives on its own, *"The corridor behind you is
> only a corridor…"*, also whole; six seconds after that the screen is quiet again. Order raised is order
> said, each with its own dwell, nothing annihilated. The other half of the row holds too: with **no** card
> up the same lines are said on the spot, on the frame they are raised (the chair reading at two havens, the
> two-door tell and the losing line at Selene Gate). And #1214's own release is re-seen: a chair reading held
> under the finder's *Ilse Varga* card comes out whole the instant that card goes.

---

## 2 · The boot, with no long freeze in it (#161, this run)

Nothing to play and nothing that looks different — this is the half that must look **exactly** the same.
Boot any link in this file, or any of Appendix A's, and the world is byte-identical to the one the previous
build produced from the same seed: the same eight founding freighters, the same pods, the same depots, the
same mass-driver cadence, in the same order.

What changed is *when the browser gets the frame back*. The founding traffic is now planned one **piece of
work** at a time rather than one ship at a time, so the longest single block the boot hands the main thread
falls from **5.09 s to 1.54 s** on a dev (interpreted) build. **What a tester should see: nothing** — no
"page unresponsive" dialog on a cold load, a front door that is still pressable in a fifth of a second, and
a loading line that now counts the freighters up in smaller steps. Open the browser console and the boot
narrates its own stages under `[SpaceSails] boot ·`, each with what that stage cost; the gate reads the
worst of those lines as a budget of its own.

> ✅ `09-17` **Played.** Booted from cold in a headless Chromium: no "page unresponsive" dialog, the front
> door paints and is pressable long before the world is finished, the loading line counts *"plotting the
> traffic lanes — freighter 1 of 8…"* up through 8, and the console carries the whole narration —
> `[SpaceSails] boot · the URL read`, `· the scenario fetched and parsed`, `· the ephemeris built and the
> vault read — THE FRONT DOOR IS LIVE`, `· the mission catalog, the plasma and the two simulators`, `· the
> ship laid down and her arc projected`, then `· the traffic lanes — pods` and one line per piece of work
> per freighter. Zero console errors and zero failed requests on every link in this file. **The numbers on
> those lines are deliberately not quoted here:** they came out of a throttled automated tab sharing a
> machine with three other lanes, and a perf claim read off one of those is worthless.

---

## 3 · Every hidden key comes out from behind the furniture (#440, this PR)

Owner, live 2026-07-26: *"Do an issue about all hidden functionalities like that so they are really clearly
told to a new player somewhere on the UI."* #443 fixed the shovel and #444 gave the ground its one lesson
card; this is the sweep of everything else. Four keys were being met by accident, and one of them
(**`B`**) was named in no in-the-moment telling anywhere in the shipped game.

Nothing below is a new panel or a new pop-up. Every word lands in a strip, a caption or a hover the game
already draws.

| what | link | what to look for | played |
| --- | --- | --- | --- |
| **`B` — the favour bank, at the one fixture that has one** | `/map?ashore=1` — docked and already standing in the bar. Walk to any **◈** patron's table. | The strip along the **bottom of the screen** used to read one fixed sentence — *"docked ⚓ walk up through the airlock to go ashore ∙ WASD — move ∙ E — interact ∙ Q — helm"* — whatever you were standing at. Step inside the `[E]` ring of a patron's table and it **grows a rung**: `💰 B — open an account at this table`. Step away and the rung goes. Press `B` and the favour-bank card opens on that contact; press it a pace further off and you still get the old refusal (*"Stand at a contact's table to open an account."*) — the bar offers the press exactly where the press answers, which is the `[E]`-plate law (#870 lane 7b) applied to the second verb at the same fixture. | ✅ `09-17` |
| **`M` — the mute, off the regolith** | any of the links above, or `/map?start=wreck` | Last rung on the same strip, in the words the ground's keybar has always used: `🔊 M — mute` / `🔇 M — unmute`, and it **swaps as you press it**. Before this, `M` was written down on the surface keybar only — a captain who never landed never met the sound switch at all. | ✅ `09-17` |
| **`H` — the captain's remote, on the ground** | `/map?dock=the-tilt&site=0&land=1` — boots you on the regolith with the sling loaded. | The keybar along the bottom now carries `🤖 H — weapons tight` **while you have a bot with you**, and flips to `🤖 H — WEAPONS TIGHT (press to free)` once it is set. Compare a derelict (`/map?start=wreck`, board her): the wreck's bar has said this since #538 — the ground, where the sentries were invented and where the pack actually comes, never did. | ✅ `09-17` |
| **`+` `−` `↑` `↓` `Shift` — the drive** | `/map?start=wreck` (any free-flying start; a berth start has no Nav toolbar) | Hover the **⛽ FUEL** gauge on the **Trade desk** (`4`) — *not* the Nav desk; the gauge sits in the Trade side panel. It used to read *"Reaction mass: 40 of 40 pulses"* and stop; it now finishes the sentence — *"— + / − (or ↑ / ↓) fires one; hold Shift for a ±1% trim"*. This is the only control in the game with **no button anywhere**, so the gauge that counts the pulses is the only honest place to say how one is spent. | ⚠ `09-17` — the tip is verbatim; the row said *Nav desk* and the gauge is on the **Trade** desk, corrected here |
| **`P` — the plotting table** | `/map?start=wreck&dest=saturn` | Hover **🗺 Plot** on the Nav toolbar: the tip already named the body it would aim at, and now ends `(P)`. Press `P` with nothing focused and the table opens; the tip then reads *"Back to flying live… (P)"*. | ✅ `09-17` |
| **`V` — the vent** | `/map?scenario=sol-eu` — **not `?scenario=electric`**: there is no `scenarios/electric.json` and that URL dies on the error page (see [#1216](https://github.com/esoinila/SpaceSails/issues/1216)); the plasma scenario is **`sol-eu`**, "Sol (Electric)". It opens on the berth picker, so take Ringside Exchange, go to the **Deck** (`7`) and walk to `⚡ CHARGE DUMP`. | Press `[E]` at the charge dump to open the hull-charge board and hover ⚡ **Dump her charge**: the sentence ends `(V)` — *"Dump the hull charge to space now — instant, free, and it will start climbing again immediately. (V)"*. The point of the key is that an arcing hull is a hull you are *not* standing at a console for. | ❌→✅ `09-17` — the row's own link 404'd ([#1216](https://github.com/esoinila/SpaceSails/issues/1216)); on the corrected link the tip ends `(V)` |

**And the half that must look exactly the same.** On the **regolith** nothing about the bar moved but the
one `H` rung: `WASD`, the `[E]` ladder (your feet, then the bin, then the ground), `T`/`⇧T`, `G`, the
satchel's `I`, the shoot-the-lock plate and `M` are all in the order they were in, and the standing prompt
and the tracker captions are untouched. Off the regolith, a deck with **nothing to add** — mid-flight, on
her own bridge — draws the same two fixed sentences it always drew, because the page hands the renderer
`null` and the renderer falls back to its own consts.

**What would say this regressed:** the `💰 B` rung showing at a table you cannot actually bank at (or
missing at one you can — the bar and the key ask the same `NearestConsoleSpot`), the deck strip losing its
`Q`/`E`/`WASD` opening, or the ground's keybar naming `H` while the sling is empty.

**The law behind it.** `EveryBoundKeyIsNamedOnTheGlassTests` sweeps both key tables and fails if a bound
key is not named by some in-the-moment telling. The Captain's Guide is deliberately not one of the sources
it reads — it names nearly every key in the game, and a guard that admitted it would pass on any world at
all. Nor are code comments: the first draft passed for `P` off one of its own justifications.

---

## 4 · The two story rulings, the same morning (#422, #640)

Both of these had been sitting open on the story passes: one about a card that said too much, one about a
line that could not truthfully be said at all. Both close.

| ruling | issue | what shipped |
| --- | --- | --- |
| *"love all those, let's pick the recommended one"* | [#422](https://github.com/esoinila/SpaceSails/issues/422) option **B** | the convergence card stops explaining (PR #1204) |
| *"yes let's have that possibility … it certainly closes a story arc for that captain"* | [#640](https://github.com/esoinila/SpaceSails/issues/640) option **A** | the run can end (this PR) |

| what | link | what to look for | played |
| --- | --- | --- | --- |
| **THE CONVERGENCE is a collision now** (#422 option B, PR #1204) — the card used to be eight sentences of third-person exposition that spent every secret both arcs still had to give, at a bar *below* either arc's own capstone, closing on a button that told you how to feel about it | [`?converge=1`](https://esoinila.github.io/SpaceSails-play/map?converge=1) | **Not a paragraph.** A stamp of two marks (`◼ ❄`) with no words, the plate, then **two sentences one above the other with nobody named over either of them** — *"It still calls the manifest in. Every window, right on the tick. Same forty names. I stopped reading who was speaking them."* and *"I've filed the same subscriber six times. Different faces, same number. Every one of them shook my hand certain they were the first."* — then one closing line, *"You have been carrying both of these for a while."*, and a plain **Close**. No copies, no premiums, no archive, no Vantar, no KAAMOS, nothing about the Old Ones. The player does the arithmetic or nobody does. | ✅ `09-17`, re-sampled ✅ `09-18` — verbatim on the 3ce9d41f build: the two-mark stamp, the plate, both sentences word for word, *"You have been carrying both of these for a while."* and a plain `Close` that closes it. The card still fires at boot **over the boot picker** rather than in the world |
| **…and both lines really are in your ledger** (#422, the audit) | same link → close the card → **Captain's desk → the ledger** | Both quoted sentences are readable there, in the shards that speak them. Not luck: the joint bar is still **3 + 3**, but one of each three is now the shard the card quotes (`holders-tell`, `adjuster-tell`), because the closing line claims you have been carrying them and a bare count could not make that true. | ✅ `09-17` — both tells readable in the ledger, joint bar 3 + 3; the desk draws *behind* the picker until you press Continue |
| **NO PATTERN ON FILE — the run ends** (#640 option A, this PR) — the line has been authored, wording-tested and **read by nothing** since the archive node landed, because a card saying POLICY CLOSED over a sim that then resurrects you is this project's most expensive bug class | [`?nopattern=1&death=impact`](https://esoinila.github.io/SpaceSails-play/map?nopattern=1&death=impact) | The ordinary four-stage death — the art, the seeded line, `…wake up`. Press it and **watch what does not happen**: no clinic, no bill, no rustbucket, no successor, no new face, no filing line, no rebirth glitch. One sentence: *"NO PATTERN ON FILE — POLICY CLOSED AT SUBSCRIBER REQUEST. The clinic's welcome loop does not play. Nobody comes. You did read the label."* One way out, `Close the book`, and it opens the **front door** rather than the ship's drawer — there is no ship to go back to. | ✅ `09-17`, re-sampled ✅ `09-18` — on `death=impact`: the four-stage death, `…wake up`, and then one sentence verbatim with a single `Close the book` under it, which opens the **front door** (the berth picker and the saved voyages, build stamp `3ce9d41`) and not the ship's drawer |
| **…and the thread is closed, not deleted** (#640) | same link → press through to the front door | That captain is still on the shelf, with their retirees, their selfies and every banked berth. What is gone is **Continue**: it will not resume that run, and if it was your only one the door offers a new voyage instead. Another captain's thread is untouched. Loading a moment you banked still works, and should — a save is a moment that was still being lived. | ✅ `09-17` — `Continue` is gone from the DOM on a clean single-run profile, and the closed captain is still on the shelf |
| **The handle it all hangs off, unchanged** (#640) | [`?archive=1&land=1`](https://esoinila.github.io/SpaceSails-play/map?archive=1&land=1) → walk aft to the **DEEP HOLD** | **Nothing was added to the handle.** `⏻ PURGE NODE — RESIDENT PATTERN NOT RECOVERABLE` is still the whole of the warning, there is still no confirmation dialog, and the line at the pull still names no resident. The collar — which a *bad* throw buys you before you pull, never a good one — is still the only way to know whose number is on the jar. If you ever see an "are you sure?", something has gone wrong. | ⚠ `09-17` — the plate is verbatim and names no resident; the pull itself could not be reached headless |
| **The other place to be reckless** (#640) | [`?nopattern=1`](https://esoinila.github.io/SpaceSails-play/map?nopattern=1) | A live run that has already spent its last life, and nothing in the world will mention it again. Go and do something dangerous. | ✅ `09-17` — nothing in the HUD, the rails or the desk mentions it |

Choose the place you die in by combining: `?nopattern=1&death=collector` (the BUSTED ladder),
`?nopattern=1&death=suffocated&dock=the-tilt&land=1` (a landing party). There is deliberately no
`?place=` — the world you boot into decides that, as it has since #621.

> ✅ **Played 2026-09-17 — both rulings land, word for word.** `…wake up` gives exactly one sentence —
> *"NO PATTERN ON FILE — POLICY CLOSED AT SUBSCRIBER REQUEST. The clinic's welcome loop does not play.
> Nobody comes. You did read the label."* — and exactly one button, `Close the book`, on **two** death lanes
> (`death=impact` and `death=suffocated&dock=the-tilt&land=1`; the suffocation one needs a 45 s settle for
> the descent). No clinic, bill, rustbucket, successor, new face, filing line or rebirth glitch appears
> anywhere in the page. `Close the book` opens the **front door**: on a profile where the dead run was the
> only one, `Continue` is absent from the DOM entirely and there is no `⚓ AT THE HELM` badge, while that
> captain is still on the shelf with her autosave row and its 💾 ⬇ ✎ 📥 controls — closed, not deleted. The
> convergence card is the doc's card exactly, and both quoted sentences really are in the ledger, in
> *The adjuster's tell* and *The berth-holder's tell*, on a **3 + 3** joint bar.
>
> Three notes for whoever plays these next:
>
> - **`?converge=1` fires the card over the boot picker, not in the world.** The z-stack is the map at
>   1000, the start-picker backdrop at 1300 and the convergence backdrop at 1420, so the card is correct and
>   on top, but the world behind it is the front door. Row 2's *"close the card → Captain's desk → the
>   ledger"* therefore needs one press of **Continue** (or a berth) in between: with the picker still up the
>   desk draws at z1000, underneath it, and cannot be read.
> - **`?nopattern=1&death=collector` stages the CATCH, not the death.** The card comes up correctly
>   (*"…PATTERN, DELINQUENT — RETURN TO ARCHIVE…"*, with `🤍 SUBMIT` / `💰 BRIBE` / `🔫 RESIST`), but the
>   cheat raises heat by 2 and hands you to `ApplyHunterCatch`, and at heat 1–2 the ladder is one opposed
>   roll — which is seeded, and won identically on every boot. The Bolivia is at **heat 3**, so this link
>   cannot be driven to the NO PATTERN card without real sim time and there is no `?heat=` to shorten it.
>   Use `death=impact` or `death=suffocated` to see the ending.
> - **On a profile that has never landed, the `⛏ FIRST TIME ON THE GROUND` tutorial draws on top of the
>   landing-party death card**, so the thing the link exists to show is invisible until you press
>   *Boots on, then.* The tutorial is closable, so it is not a UI-law break — but it hides the beat on the
>   first run, which is the run a tester does.

**And the half that must look exactly the same.**

- **Every other death.** The ordinary wake is untouched: clinic, bill, rustbucket, succession, the filing
  line, the rebirth glitch, the clinic's second page. The Client guard for #640 presses the *same* button
  on the *same* death with a pattern still on file and gets the clinic — that control is what makes the
  rest of it mean anything.
- **Both arcs' capstones.** `berth-code` and `policy-terms` are pinned **word for word**: "the card stops
  explaining" was not paid for by moving the explanation into a capstone. Each arc still has its own
  reveal to give, which was the actual complaint on #422.
- **The convergence plate.** `art/convergence.jpg` did not change by a pixel although the entire body it
  sits over was replaced — it was painted against the *shape* of the reveal rather than any sentence in
  it, which is what a plate is for.
- **Both shard texts.** `holders-tell` and `adjuster-tell` read exactly as they always did; the two
  sentences simply exist once now, as consts the shards quote back.

---

## 5 · The box has somewhere to be (#711 slice 2, with #319 and #794)

Slice 1 ([PR #1196](https://github.com/esoinila/SpaceSails/pull/1196)) shipped the **UNLISTED PARCEL** and
the fine that closes a folder, and said what it was still missing out loud: *"what the parcel is FOR — no
delivery, no payer yet, so a captain never inspected has carried a box for nothing."* This is that, on the
owner's own rail from [#794](https://github.com/esoinila/SpaceSails/issues/794) — the counterparty never
shows a face, the goods are physical, and the drop is the delivery.

Nothing new is saved for any of it. The job is the parcel's own id; the delivery is
[#319](https://github.com/esoinila/SpaceSails/issues/319)'s hole; the pending payment **is** that hole, due
off its own burial stamp; and the quiet after a confiscation rides the same durable register the fence's
one-key-per-window already rides.

| what | link | what to look for | played |
| --- | --- | --- | --- |
| **The whole run in one URL** (#711 slice 2, this PR) | [`?dock=the-tilt&parcel=1`](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&parcel=1) — boots clamped at The Tilt with an **UNLISTED PARCEL** already in the pocket, and rides `?land=`'s own descent down onto **the ground that parcel is actually for**. The cheat forges nothing: it mints real parcels the way the desk mints them and only chooses which *window*, looking for a drop this berth can reach. A pulse names the ground it picked. | You are standing on the regolith with the box. Walk out, press **⛏ DIG HERE** and bury it (satchel → *BURY A THING FROM THE SATCHEL* is the row the shuttle door offers; the cheat lands you with the box already on you). The dig's own pulse reads exactly as it always has — *"⛏ In the ground — 1 thing from the satchel off the books. The ✗ marks this spot. …"* — and then **one sentence more**: *"In the ground, where somebody who has never seen your face will know to dig."* Open the satchel's **NOTES**: one entry, in the captain's own hand, *"a parcel, put in the ground at Phobos · The Ridge Camp for nobody you have met"*, filed under that place on **THREADS**. | ⚠ `09-18` — **the burial plays and its pulse is verbatim** (taken by hand at the desk, chosen on the shuttle's own `🎒 BURY A THING FROM THE SATCHEL` row, buried on Miranda · The Wild Plain): *"…Now get back to the shuttle. **In the ground, where somebody who has never seen your face will know to dig.**"* Two caveats stand, both re-seen: the `?parcel=1` cheat's own ground-naming pulse is still never readable ([#1225](https://github.com/esoinila/SpaceSails/issues/1227)), and the prefix reads *"⛏ Chest buried — 5 units + 1 thing…"* while the hold is not empty.<br>✅ `09-18` **re-played on #1227's fix, and the row now plays as written — no round trip.** The cheat lands you on the regolith with the box **in the shovel's hand**: the strip reads `⛏ SOMETHING TO PUT IN THE GROUND — press E to BURY IT HERE`, and `E` buries instead of probing. Dismiss the `FIRST TIME ON THE GROUND` lesson card, walk clear of the pad and press `E` — one square answered *"⛏ The shovel rings off bedrock — this square won't take a chest. Try a step over."* and the next one took it, with the pulse verbatim and the row's own prefix this time: *"⛏ In the ground — 1 thing from the satchel off the books. The ✗ marks this spot. … Now get back to the shuttle. **In the ground, where somebody who has never seen your face will know to dig.**"* [#1218](https://github.com/esoinila/SpaceSails/issues/1218) is still on the ✗ — the cache line and the `🗺 DIG AT THE ✗` plate are drawn on the same pixel |
| **The job on the row it came across** (#711 slice 2) | any berth with the desk open — [`?dock=the-tilt&parcel=1`](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&parcel=1) then fly back up, or take one by hand at **Comms → 🕸 Dark web market** | While you carry one, the desk's fourth row stops offering and starts **instructing**: the plate, then *"No name. A moon, a bearing, a depth. Put it in the ground and leave."*, then one functional line — `📍 PHOBOS · THE RIDGE CAMP`. **No price, then or now.** No new panel, no quest entry, no tab: the job lives on the row the box came across and nowhere else. | ✅ `09-18` — with #1223 the desk deals at the clamp: `📦 Take it` turns the row into the job, verbatim — the plate, *"No name. A moon, a bearing, a depth. Put it in the ground and leave."*, then `📍 MIRANDA · THE WILD PLAIN`. No price, no panel, no quest entry, and re-opening the desk shows the same row |
| **The money, with nobody's name on it** (#711 slice 2) | bury it, then **warp** — the lag is **2–4 watches** of sim time (8–16 h), seeded off that parcel — then dock anywhere and open **Comms → 🕸 Dark web market** | The instant the desk opens: *"💳 A payment with no sender. Somebody dug. +NNN cr"*, and the purse has it. Open the ledger's hoard: **the chest is gone from the ground.** Fly back out to that site and walk to where the ✗ was — there is a **disturbed-ground mark** there now, dated by #316's own three bands (*"Still smoking."* → *"…weeks old."*) off the moment it came due, not off the moment you were told. Nothing anywhere says who held the shovel. | ⚠ `09-17` — the money arrives (1,500 → 1,730 cr after the lag, and 0 → 230 cr on a second run) but the `💳` sentence is never drawn. **Not re-driven 09-18** — the lag needs a warp and a re-dock after the burial, and the run ran out of session before it got there; the `💳` claim in this row is still unwitnessed |
| **…and it only comes once** (#711 slice 2) | close the desk and open it again | Nothing. There is no flag to clear: the hole was the record, and somebody dug it. Two drops that came due together are two payments on two visits, one sentence each. | ✅ `09-17` — selecting the node again adds nothing |
| **The wrong moon is just a hole** (#711 slice 2 / #319) | [`?dock=the-tilt&parcel=1`](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&parcel=1), then at the boarding panel pick a **different site** before you go down (or fly to another moon) | Bury it there and the pulse is the ordinary one — no extra sentence, no note, no money, ever. The ✗ is on the map, the odds are the chest's own, and you can walk back and dig your box up. A parcel in the wrong ground is a buried parcel, which is exactly what it is. | ✅ `09-17` — ordinary pulse, no extra sentence, no note, no money |
| **A man with a form took it, and the work dries up** (#711 slice 1 + 2) | [`?dock=the-tilt&parcel=1`](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&parcel=1) → walk into a **HIVE** floor with the box on you and let a round stop you (slice 1's beat) | The card is slice 1's, unchanged: fined, filed, and the box carried off. Then fly up and open the dark-web desk: **the parcel row is simply not there.** Not greyed, not refusing, not explaining — absent, for two to four watches seeded off the box that was lost. Nothing anywhere says why. Come back a day later and the row is back. | 🚫 `09-18` — The Tilt has no HIVE floor; #1217 is fixed so the after-check is now observable in principle, but the confiscation itself was not reached this sweep |
| **…and so does being followed to the desk** (#1062 slice 2c, this PR) | [`?tailed=1&ashore=1&dock=selene-gate`](https://esoinila.github.io/SpaceSails-play/map?tailed=1&ashore=1&dock=selene-gate) — ashore with the grey coat on the floor behind you. **Leave him there** (do not shake him), then do ONE quiet thing at that berth: take the favour at a `◈` contact's table, or buy the fence's key at **Comms → 🕸 Dark web market**. Then look at the desk's fourth row. | **The parcel row is not there either.** Not greyed, not refusing, not explained — absent, exactly as it is while you are already carrying one, and for the same one visit the favour and the fence are absent. **Nothing is said**: no line, no card, no book entry, on that frame or any frame of that visit. Cast off, come back, and the place says the one sentence it has always said — *"Tidy, in the way a place is after somebody has been through it first."* — and in the same breath **all three rows are back**, the parcel's among them. Nothing anywhere ever tells you a box was one of the things it cost you. **Shake him first and the same deal costs nothing.** | ⚠ `09-19` — not driven by hand: the burn's cast-off-and-return half is still not drivable headless (§1b's own caveat). Proved in the bench instead — two identical pages, identical berth, identical watch, identical press, differing only in whether anybody was on the floor behind the captain |

**Reading the clock while you play.** Both lags are **sim** time and both are measured in the four-hour
watch every roster, patience and fence in this game already turns on. The warp slider is the fast-forward;
there is nothing to sit through.

> ⚠ **Played 2026-09-17 — the ground half is real, the desk half is unreachable.** The cheat mints a real
> **📦 UNLISTED PARCEL** (it is in the satchel under CARRIED, with its own look card), rides the descent, and
> the burial pays out exactly as promised: the dig pulse carries the one extra sentence verbatim —
> *"…Now get back to the shuttle. **In the ground, where somebody who has never seen your face will know to
> dig.**"* — the NOTES page takes the one entry *"📦 a parcel, put in the ground at Miranda · The Wild Plain
> for nobody you have met"*, a different site on the same moon gets the ordinary pulse and no note at all,
> and the payment lands after the lag (1,500 → 1,730 cr) and only once. Four things the rows get wrong:
>
> - **The ground the cheat picks is never announced.** The `🧪 DEV ?parcel=1 —` pulse is written and then
>   overwritten by `?land=`'s own descent lines inside the same frame; sampling the page every 25 ms from the
>   moment the loader clears never catches it. Read the ground off `🛬 SET DOWN AT:` instead.
> - **You cannot bury it the way the row says.** `E` on the regolith with the parcel on you runs the *probe*
>   (*"🕳 Nothing but regolith down there…"*): the shuttle's deposit pick is empty, so the box has to be
>   chosen through the boarding card's `🎒 BURY A THING FROM THE SATCHEL` row — which means flying back up
>   and coming down again. Every burial in this sweep took that round trip.
> - **The pulse prefix quoted here is not what renders** when the hold is not empty: it reads *"⛏ Chest
>   buried — 5 units + 1 thing from the satchel off the books…"*, not *"⛏ In the ground — 1 thing…"*.
> - **THREADS does not show it.** The entry is filed under the place, but THREADS only lists a name written
>   down **twice**, so with one entry it reads *"Nothing in this book names the same thing twice. Yet."* The
>   place-filing is visible on 📓 NOTES → `🥾 every ground`, which is where to look.
>
> And the desk half cannot be played at all: **[#1217](https://github.com/esoinila/SpaceSails/issues/1217)**
> — clamped at a haven, with the HUD and both desk cards reading *Docked at The Tilt*, Comms → 🕸 Dark web
> market is badged `offline` and says *"Not orbiting or docked anywhere."* That takes the job row, the
> confiscation after-check and the fence's key in §1b with it. The `💳 A payment with no sender…` sentence
> is never drawn either — the purse simply rises. One more thing seen on the way:
> **[#1218](https://github.com/esoinila/SpaceSails/issues/1218)** — at your own ✗ on a haunted ground the
> cache line and the `🗺 DIG AT THE X` plate are drawn on the same pixel and come out as
> `yours ⛏ DIG AT THE ✗ ear it`.

> ✅ **Re-played 2026-09-18 — [#1217](https://github.com/esoinila/SpaceSails/issues/1217) is fixed and the
> desk half plays.** The dark-web market was opened at **all seven** dockable havens (`cinder-roost`,
> `selene-gate`, `the-space-bar`, `red-eye`, `ringside-exchange`, `the-tilt`, `the-deep`): every one is
> badged **`open`**, every one draws all four rows (the leads board, the favour bank, INSPECTORATE, the
> BLACK-OPS KEY at its own per-berth price, and the UNLISTED PARCEL), and the words *"offline"* and
> *"Not orbiting or docked anywhere"* appear **nowhere** in any of them. `📦 Take it` then turns the fourth
> row into the job (above), and the shuttle's boarding card offers
> **`🎒 BURY A THING FROM THE SATCHEL` with `○ 📦 UNLISTED PARCEL` in the pick** — so the round trip the
> 09-17 sweep had to make is not the shipped path after all: it is only the `?parcel=1` cheat that lands you
> without the box selected as the deposit, which is [#1225](https://github.com/esoinila/SpaceSails/issues/1227).
> Buried on the ground the job names, the dig pulse carries the one extra sentence verbatim.
>
> Screenshots: `D:/repo12/wt/replay/.qa-scratch/r5c-taken.png` (the job row), `r5j-panel.png` and the
> satchel pick behind it, `r5l-dug.png` (the burial pulse), `havensweep.log` (all seven desks).
>
> ✅ **[#1227](https://github.com/esoinila/SpaceSails/issues/1227) fixed 2026-09-18.** The cheat now makes
> the pick the boarding card would have made — the parcel rides `ShuttleExcursion.Pack`'s deposit, which is
> the one builder the shipped path goes through too — so `?parcel=1` lands you with the box **in the
> shovel's hand** and `E` on the regolith buries instead of probing. Guarded end to end from the URL
> (`ADropForNobodyYouHaveMetTests.TheDevDoorPutsTheBoxInTheShovelsHandAndNotOnlyInThePocket`).

**And the half that must look exactly the same.**

- **Every other buried thing.** Coin, cargo, the chip, a folded sheet, a round: identical pulse, identical
  ✗, identical `CacheSafety` line, identical return dig. There is one extra sentence on exactly one hole in
  the sky and nothing else about the shovel moved.
- **Slice 1's whole beat.** The desk row that hands one over, the look card, the fine, the tell, the folder
  that closes, the outfit that keeps looking — all verbatim and all untouched. The only new clause on the
  offer is the absence above.
- **The dark-web desk's other three rows.** The chip's buyer, the inspector's card and the fence's key are
  priced, worded and gated exactly as they were; the job row carries no price because no coin moves *to*
  the desk.
- **Every other death.** A captain who dies with an undelivered box in his coat wakes with it still in his
  coat, and a delivery already in the ground still pays — to whoever is holding the licence. Nothing was
  written for that: the wake has never reached the satchel or the hoard, and the payer never saw a face.

---

## 6 · The light keeps its own day (#759, this PR)

The last named remainder of the park behind the bar, and the only one that is arithmetic rather than
geometry. Owner, filing the room: *"The light keeps its own day — a grow-cycle that matches no watch of the
building above or below. Anyone who lingers notices the park's morning arriving at the wrong time. **Subtly
wrong is the register: never broken, never right.**"*

Until now the five floodlight masts against the far wall were posts on the plan and the "artificial day" was
a word in a comment. The park now runs a **photoperiod of 4.618034 watches — 18 h 28 m of sim time**, lit for
four fifths of it, with a phase offset seeded per site. The ratio to the building's own four-hour watch is
four watches and the *golden section* of a fifth: the worst-approximable number there is, so the park's
morning walks around the building's clock for ever and never settles on it. Over 2,000 watches the two
clocks disagree on **80%** of them — a majority, and not all.

**Nothing on the glass ever says what time it is in there.** No HUD row, no clock, no plate, no card, no
bark. A source sweep holds it: four files in the whole client may reach for the number, and none of them is
a sentence.

| what | link | what to look for |
| --- | --- | --- |
| **The park at the bottom of its cycle** (#759, this PR) | [`?park=1&parkphase=night`](https://esoinila.github.io/SpaceSails-play/map?park=1&parkphase=night) — inside the park on B1 of a deep site, with the sim clock jumped so **this site's** park is at the middle of its dark. The arithmetic has to be done for you: the cycle carries a per-site offset, so one number of `?simhours=` is this park's afternoon and the next park's night. | The gravel reads **dim** — the floor art at 55% of the alpha it wears at noon, never less, because a room whose paths you cannot see has *broken* rather than drifted. The five masts against the far wall are **small dark heads**. Everything else on the floor is exactly as it was: the beds, the benches, the lone figure, the window wall, the plate at the gate. |
| **…and at the top of it** | [`?park=1&parkphase=day`](https://esoinila.github.io/SpaceSails-play/map?park=1&parkphase=day), and `dusk` / `dawn` for the two shoulders | Same room, same plan, different hour. Each mast is now a **wash of cold horticultural white** — deliberately not the warm amber every *other* light on this deck is drawn in, because the park is not lit by the building's lamps. On `dawn` and `dusk` the discs are half up: the ramp is a ramp, not a switch. |
| **Standing in it when the morning comes up** (#759, this PR) | [`?park=1&parkphase=morning`](https://esoinila.github.io/SpaceSails-play/map?park=1&parkphase=morning) — set down on the gravel **five sim-minutes** before this park's own dawn, on a cycle the rest of the building is *not* having a morning on. | Stand still, or take a bench — it makes no difference which, because what is being noticed is a thing about the **room**. Watch the masts come up. **One pulse**, ranked so nothing displaces it: *"It is coming up to morning in here. It was not morning anywhere else in the building when you came in."* Open the satchel's **NOTES**: one entry, lower case, *"the park keeps a day of its own — set to nobody's watch"*, filed on **THREADS under the PLACE** and never under a name. **No card. No explanation anywhere of why.** |
| **…and it is the CHANGE, not the room** | same link. Before the five minutes are up, walk **out of the gate and back in**. | **Nothing lands, ever.** You came in again, on a morning, and the sentence claims you did not. Same for a second captain-visit to the same park once it has been spent: one pulse per captain per site, in the register that rides the vault, so a reload does not hand it to you twice. |
| **Two parks are not keeping the same day** | `?park=1&parkphase=day`, then boot the same key at a different site | The three sites a captain actually walks have offsets an eighth of a cycle apart or better (luna 0.177, phobos 0.294, titan 0.791). At the pinned frame time of 880 s, **phobos' and titan's parks are in broad day while luna's is dark** — the same instant, three rooms, three hours. |
| **…and the half that must look exactly the same** | `/map?park=1`, `/map?park=1&spread=1`, `/map?parkback=1`, `/map?parkwalk=1`, `/map?counter=1`, `/map?stool=1`, and every other link in this file | **Everything but the light.** The park's geometry, its walk, its twelve beds and their stencils, the six benches and the sit verb, the lone figure, the gate, the window wall, the attendance note on your first step — untouched. Off that one floor **nothing changed at all**: the frame-hash ledger moved on exactly the five cases with a park in them (+5 marks each, one per mast) and the other twenty-eight are byte-identical. |

**Reading the clock while you play.** The cycle is **sim** time, so the warp slider is the fast-forward if
you want it — but `?parkphase=morning` is set at five sim-minutes precisely so you should not need it. The
masts move continuously: the shoulders are straight ramps about 55 sim-minutes long, so at warp 1 a dawn
takes about an hour of the player's evening to complete and is visibly under way the whole time.

**What would say this regressed:** a number, a phase name or a clock **anywhere on the glass** (the whole
feature is that the player can only *see* it); the pulse arriving the instant you walk in, or arriving on a
morning the building is also having; a park you cannot make out the paths in at the bottom of the cycle
(that is broken, not subtly wrong); the masts snapping between dark and lit instead of ramping; or two sites'
parks turning over together.

**The law behind it.** `TheParkKeepsItsOwnDayTests` — twelve guards, every one shown RED before it was
trusted: the incommensurability bound (stated as *q²·|r − p/q| ≥ 0.35* for every denominator up to 200,
because Dirichlet says a list of fractions cannot be the law), both ends of "never broken, never right", the
morning that lands in all 100 hundredths of a watch over 500 cycles, the beat enumerated over its 128 inputs,
and the source sweep above.

---

## 7 · Two instruments that disagree — the derived anomaly (#533, 2026-09-19)

> *"The story ones are exceptional in some sense that we are left to wonder. Like what was such a rich ship
> doing there-kind of things 😎"* — owner, on #533

The open half of #533. An **anomaly** is two numbers the game already computes, read off two instruments,
that do not sit together — and nobody aboard is left to ask. It is **not a cause**: the report still names
what happened, and the dropdown does not grow a word. About **one hull in ten** carries one (one in three of
the hulls whose own numbers support one), and **nothing about a hull that carries one is visible until she
is read** — same eight fittings, same consoles, same deck.

Of the issue's five derived anomalies, **one is honestly derivable off shipped facts** and ships here; the
other four are audited, with their canon lines and the exact number each is waiting for, in
[`features/the-paperwork.md` §7c](features/the-paperwork.md).

| what | link | what to look for |
| --- | --- | --- |
| **A rich hull on a road nobody lists** (#533, this PR) | [`?wreck=mutiny&land=1`](https://esoinila.github.io/SpaceSails-play/map?wreck=mutiny&land=1) — the cheat seeds her as `lost-1`, the **Understudy**, cargo assessed at **318,765 cr**, and parks her off **THE TILT**: a berth that carries real tonnage, all of it discreet, **none of it on any board** (#541's own rule, and `ArrivalTubeTests` already pins it at zero). Board her and walk aft to the arms locker — 🔒 **THE ARMS LOCKER**, her cause station, the one you came here to read. | Press `[E]`. The survey card comes up as it always has — her painting, and under it *"two barricades facing each other down one corridor, and the arms locker opened with a cutting torch"* — and then, under a hairline, in the face the ship's own boards are printed in, **a fourth line**: <br><br>**`Assessed at 318,765 cr. Traffic on this road, listed, this year: none.`**<br><br>Two facts, side by side, and **nothing under them.** No heading, no verb, no third sentence. Close the card and open the Captain desk → 📜 **Ledger**: the book has taken one line, filed under the hull as a `📍` PLACE — *"Understudy — assessed at 318,765 cr, no listed traffic on this road this year, and nobody aboard to ask"*. Read her manifest two rooms away and it quotes the **same** 318,765 cr, which is the whole point: you can check it. |
| **…and the ordinary ship, which is nearly all of them** | [`?wreck=insurancejob&land=1`](https://esoinila.github.io/SpaceSails-play/map?wreck=insurancejob&land=1) — the **Quiet Sister**, assessed at **291,429 cr**, off the same unlisted berth. Rich enough to qualify; not dealt one. | Her cause station reads exactly as it did before this lane: painting, evidence caption, **and nothing after it.** No empty slot, no hairline, no gap where a line would go — a card that grew a space would tell you there was something to find on the hulls where the space is full. Eight of the ten cause hulls read like this. |
| **…and the same hull on a road that IS listed** | any wreck boarded off a berth with scheduled traffic — e.g. [`?wreck=mutiny&land=1&dock=ringside-exchange`](https://esoinila.github.io/SpaceSails-play/map?wreck=mutiny&land=1&dock=ringside-exchange) (23 on the board) or `&dock=selene-gate` (10). | **No fourth line at all**, on the same rich hull. The anomaly is about *where she is*, so moving her to a road the world lists takes it away — which is also the guard: a hull on a listed road is never called a poor road however rich she is. |
| **…and the half that must look exactly the same** | every other link in this file, and every `?wreck=` cause above | **Everything.** The deck, the eight fittings, the three stations, the cradle row, the choice card and the ten causes are untouched; the paperwork lane gains no option; the frame-hash ledger did not move a row. |

**What would say this regressed:** a line that *explains* either number rather than stating both (any of
*because / why / must / so that / explains / means* is a source-swept failure); an anomaly on a hull whose
manifest quotes a different value; a second surface, a banner, a marker on the deck or a mark on the console
label; the same hull carrying one on one boarding and not on the next; an "ANOMALY" row in the report's
cause list; or any file in the tree other than `WreckAnomaly.cs` and `Map.Wreck.cs` naming the feature —
which is how a later system would come to *resolve* one, and that is the thing #533 forbids outright.

**The law behind it.** `TwoInstrumentsDisagreeTests` (Core, 13 guards) and `TheFourthLineAtTheSurveyTests`
(Client, 6): determinism over the whole seeded fleet, at most one per hull, every dealt anomaly's inequality
re-derived from the hull's own numbers on both kinds of road, the measured rate, the causal-word and
reserved-word sweep, the two-files-only source law, and the link in this row proved to boot a hull that
really carries one.
