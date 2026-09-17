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

Two owner rulings from 2026-09-17, both of them "what shipped is right, the *clock* on it is wrong" —
and two more the same morning, on the two story passes that had been waiting for a decision (§4).

---

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
| **The walk is empty, and you can play the wait** (#1199 / #1062, this PR) | `/map?dock=selene-gate&ashore=1&simhours=4` — clamped at Selene Gate, ashore in **THE EARTHRISE BAR**, with the station clock four hours in so the room is **past last call** (the walk is only dealt in the last quarter of a watch; last call is at 10,800 s of 14,400). The person of interest at this berth is the haven's own named regular, **GILT-EYE**, and the rota has him at the bar on this watch. | He gets up from his top and crosses the concourse **due west**, out onto `OBSERVATION WALK` — glass floor, the limb under it, a rail at the blind end. Follow him: stay inside legible range with a clear line and **he stops and waits for you to go past** (no card, no line — he just turns). Break the line, and on a frame nobody is looking at him he is simply **not on the floor any more**. Then walk out to the rail yourself. **At warp 1 the card comes up about three minutes after the last moment you had eyes on him** — it used to be an hour. The card is the absence; the note files under his own name on THREADS; nothing is pulsed over the card. | ❌ `09-17` — **the link does not reach the beat**, see below |

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
> is only the *person* who is missing. **No `simhours` value is known to work**, because the frozen watch is
> pinned to 0 and GILT-EYE is one of watch 0's leavers, so any clock past last call is also past his
> departure. Until #1213 is ruled on, there is **no link in this file that reaches §1's beat** — do not
> re-derive one; play it.

---

## 1b · Losing YOUR tail — #1062's mirror half (this PR)

The other half of the same issue: *"or trying to lose a tail our selves :-D"*. Somebody mundane and human is
behind the captain, ashore, and **nothing in the game says a word about him** until the captain works it out.
Full feature note: [`features/losing-the-tail.md`](features/losing-the-tail.md).

| what | link | what to look for | played |
| --- | --- | --- | --- |
| **Somebody came in after you** (#1062 slice 2) | `/map?tailed=1&ashore=1&dock=selene-gate` — ashore in **THE EARTHRISE BAR** with the dev row on. The row forces only WHETHER; the world's own route in is an outfit's #715 folder at the band where it wants a face (a femme-fatale walk-in that turned out to be a setup, or a compromising chip sold at a dark-web desk). | A grey figure follows you in through the bar's north door and settles **nine to thirty deck units behind you, with a line to you, and no name over him**. He never goes to the counter. **Nothing is pulsed, nothing is carded, nothing goes in the book.** | ✅ `09-17` |
| **The chair that faces the door** | same link. Walk to a top with a clear line back to the bar's doorway and press `[E]` to take it. | After about **nine seconds in the chair**, one pulse: *"From this chair you can see the door. So can the man who came in after you, and he has not ordered."* Stand on your feet in the same room for the same nine seconds first and **nothing happens** — the sit is the whole cost. | ✅ `09-17` |
| **The same coat, two doors running** | same link. Walk out of the bar, across the concourse, and **out onto `OBSERVATION WALK`** (edge 5, due west — #1199's tube). | He follows you through the bar's doorway and then to the mouth of the walk, which is the only place in a one-way room with a line to you. Two DISTINCT doorways is the tell: *"The same grey coat, two doors running. Nobody's errand takes them through both."* A locked cellar leaf never counts. | ✅ `09-17` |
| **Losing him** | same link. Walk back **down your own gangway** toward the ship, and stay there. | He will not follow you down the umbilical. After about **nine seconds with nothing to look at** he gives up: *"The corridor behind you is only a corridor. Whoever it was is asking the wrong floor about you."* — and the field book takes one line, filed on **THREADS under the PLACE** (📍 SELENE GATE) and never under a name: *a tail, lost at … — a grey coat, never a face*. | ✅ `09-17` |
| **Failing forward — the burn** (#1062 slice 2b) | same link, plus a quiet verb at that berth while he is still on the floor: **take the favour** at a `◈` contact's table (press `B` at a table where the account is on offer), or **buy the fence's key** at the Comms desk's dark-web board. | **Nothing happens.** No line, no card, no warning — you get what you came for and walk out. Cast off, come back, and the port has **no favour and no fence row at all**, and the place says one thing: *"Tidy, in the way a place is after somebody has been through it first."* The book takes *SELENE GATE — walked before you got there, by somebody who knew where to walk*, filed under the **same PLACE** as the losing note so THREADS stacks the evening in order. Shake him first and the same verb costs nothing. | 🚫 `09-17` — needs a cast-off and a return; not drivable in one headless session |
| **…and the half that must look exactly the same** | `/map?tailed=0&ashore=1&dock=selene-gate`, and every other link in this file | **Nothing.** No man, no lines, no book entry — and the drawn frame is byte-identical: the frame-hash ledger did not move by one row in this lane. Slice 1's own beat (GILT-EYE and the empty walk, §1 above) plays exactly as it did. | ⚠ `09-17` — no man, no line, no entry with `tailed=0`; frame-hash ledger not re-run here, and §1 above is ❌ (#1213) |

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
> the tot ledger in a room with no counter in it.

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

| what | link | what to look for |
| --- | --- | --- |
| **`B` — the favour bank, at the one fixture that has one** | `/map?ashore=1` — docked and already standing in the bar. Walk to any **◈** patron's table. | The strip along the **bottom of the screen** used to read one fixed sentence — *"docked ⚓ walk up through the airlock to go ashore ∙ WASD — move ∙ E — interact ∙ Q — helm"* — whatever you were standing at. Step inside the `[E]` ring of a patron's table and it **grows a rung**: `💰 B — open an account at this table`. Step away and the rung goes. Press `B` and the favour-bank card opens on that contact; press it a pace further off and you still get the old refusal (*"Stand at a contact's table to open an account."*) — the bar offers the press exactly where the press answers, which is the `[E]`-plate law (#870 lane 7b) applied to the second verb at the same fixture. |
| **`M` — the mute, off the regolith** | any of the links above, or `/map?start=wreck` | Last rung on the same strip, in the words the ground's keybar has always used: `🔊 M — mute` / `🔇 M — unmute`, and it **swaps as you press it**. Before this, `M` was written down on the surface keybar only — a captain who never landed never met the sound switch at all. |
| **`H` — the captain's remote, on the ground** | `/map?dock=the-tilt&site=0&land=1` — boots you on the regolith with the sling loaded. | The keybar along the bottom now carries `🤖 H — weapons tight` **while you have a bot with you**, and flips to `🤖 H — WEAPONS TIGHT (press to free)` once it is set. Compare a derelict (`/map?start=wreck`, board her): the wreck's bar has said this since #538 — the ground, where the sentries were invented and where the pack actually comes, never did. |
| **`+` `−` `↑` `↓` `Shift` — the drive** | `/map?start=wreck` (any free-flying start; a berth start has no Nav toolbar) | Hover the **⛽ FUEL** gauge on the Nav desk. It used to read *"Reaction mass: 40 of 40 pulses"* and stop; it now finishes the sentence — *"— + / − (or ↑ / ↓) fires one; hold Shift for a ±1% trim"*. This is the only control in the game with **no button anywhere**, so the gauge that counts the pulses is the only honest place to say how one is spent. |
| **`P` — the plotting table** | `/map?start=wreck&dest=saturn` | Hover **🗺 Plot** on the Nav toolbar: the tip already named the body it would aim at, and now ends `(P)`. Press `P` with nothing focused and the table opens; the tip then reads *"Back to flying live… (P)"*. |
| **`V` — the vent** | `/map?scenario=electric` | Open the hull-charge board and hover ⚡ **Dump her charge**: the sentence ends `(V)`. The point of the key is that an arcing hull is a hull you are *not* standing at a console for. |

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

| what | link | what to look for |
| --- | --- | --- |
| **THE CONVERGENCE is a collision now** (#422 option B, PR #1204) — the card used to be eight sentences of third-person exposition that spent every secret both arcs still had to give, at a bar *below* either arc's own capstone, closing on a button that told you how to feel about it | [`?converge=1`](https://esoinila.github.io/SpaceSails-play/map?converge=1) | **Not a paragraph.** A stamp of two marks (`◼ ❄`) with no words, the plate, then **two sentences one above the other with nobody named over either of them** — *"It still calls the manifest in. Every window, right on the tick. Same forty names. I stopped reading who was speaking them."* and *"I've filed the same subscriber six times. Different faces, same number. Every one of them shook my hand certain they were the first."* — then one closing line, *"You have been carrying both of these for a while."*, and a plain **Close**. No copies, no premiums, no archive, no Vantar, no KAAMOS, nothing about the Old Ones. The player does the arithmetic or nobody does. |
| **…and both lines really are in your ledger** (#422, the audit) | same link → close the card → **Captain's desk → the ledger** | Both quoted sentences are readable there, in the shards that speak them. Not luck: the joint bar is still **3 + 3**, but one of each three is now the shard the card quotes (`holders-tell`, `adjuster-tell`), because the closing line claims you have been carrying them and a bare count could not make that true. |
| **NO PATTERN ON FILE — the run ends** (#640 option A, this PR) — the line has been authored, wording-tested and **read by nothing** since the archive node landed, because a card saying POLICY CLOSED over a sim that then resurrects you is this project's most expensive bug class | [`?nopattern=1&death=impact`](https://esoinila.github.io/SpaceSails-play/map?nopattern=1&death=impact) | The ordinary four-stage death — the art, the seeded line, `…wake up`. Press it and **watch what does not happen**: no clinic, no bill, no rustbucket, no successor, no new face, no filing line, no rebirth glitch. One sentence: *"NO PATTERN ON FILE — POLICY CLOSED AT SUBSCRIBER REQUEST. The clinic's welcome loop does not play. Nobody comes. You did read the label."* One way out, `Close the book`, and it opens the **front door** rather than the ship's drawer — there is no ship to go back to. |
| **…and the thread is closed, not deleted** (#640) | same link → press through to the front door | That captain is still on the shelf, with their retirees, their selfies and every banked berth. What is gone is **Continue**: it will not resume that run, and if it was your only one the door offers a new voyage instead. Another captain's thread is untouched. Loading a moment you banked still works, and should — a save is a moment that was still being lived. |
| **The handle it all hangs off, unchanged** (#640) | [`?archive=1&land=1`](https://esoinila.github.io/SpaceSails-play/map?archive=1&land=1) → walk aft to the **DEEP HOLD** | **Nothing was added to the handle.** `⏻ PURGE NODE — RESIDENT PATTERN NOT RECOVERABLE` is still the whole of the warning, there is still no confirmation dialog, and the line at the pull still names no resident. The collar — which a *bad* throw buys you before you pull, never a good one — is still the only way to know whose number is on the jar. If you ever see an "are you sure?", something has gone wrong. |
| **The other place to be reckless** (#640) | [`?nopattern=1`](https://esoinila.github.io/SpaceSails-play/map?nopattern=1) | A live run that has already spent its last life, and nothing in the world will mention it again. Go and do something dangerous. |

Choose the place you die in by combining: `?nopattern=1&death=collector` (the BUSTED ladder),
`?nopattern=1&death=suffocated&dock=the-tilt&land=1` (a landing party). There is deliberately no
`?place=` — the world you boot into decides that, as it has since #621.

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

| what | link | what to look for |
| --- | --- | --- |
| **The whole run in one URL** (#711 slice 2, this PR) | [`?dock=the-tilt&parcel=1`](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&parcel=1) — boots clamped at The Tilt with an **UNLISTED PARCEL** already in the pocket, and rides `?land=`'s own descent down onto **the ground that parcel is actually for**. The cheat forges nothing: it mints real parcels the way the desk mints them and only chooses which *window*, looking for a drop this berth can reach. A pulse names the ground it picked. | You are standing on the regolith with the box. Walk out, press **⛏ DIG HERE** and bury it (satchel → *BURY A THING FROM THE SATCHEL* is the row the shuttle door offers; the cheat lands you with the box already on you). The dig's own pulse reads exactly as it always has — *"⛏ In the ground — 1 thing from the satchel off the books. The ✗ marks this spot. …"* — and then **one sentence more**: *"In the ground, where somebody who has never seen your face will know to dig."* Open the satchel's **NOTES**: one entry, in the captain's own hand, *"a parcel, put in the ground at Phobos · The Ridge Camp for nobody you have met"*, filed under that place on **THREADS**. |
| **The job on the row it came across** (#711 slice 2) | any berth with the desk open — [`?dock=the-tilt&parcel=1`](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&parcel=1) then fly back up, or take one by hand at **Comms → 🕸 Dark web market** | While you carry one, the desk's fourth row stops offering and starts **instructing**: the plate, then *"No name. A moon, a bearing, a depth. Put it in the ground and leave."*, then one functional line — `📍 PHOBOS · THE RIDGE CAMP`. **No price, then or now.** No new panel, no quest entry, no tab: the job lives on the row the box came across and nowhere else. |
| **The money, with nobody's name on it** (#711 slice 2) | bury it, then **warp** — the lag is **2–4 watches** of sim time (8–16 h), seeded off that parcel — then dock anywhere and open **Comms → 🕸 Dark web market** | The instant the desk opens: *"💳 A payment with no sender. Somebody dug. +NNN cr"*, and the purse has it. Open the ledger's hoard: **the chest is gone from the ground.** Fly back out to that site and walk to where the ✗ was — there is a **disturbed-ground mark** there now, dated by #316's own three bands (*"Still smoking."* → *"…weeks old."*) off the moment it came due, not off the moment you were told. Nothing anywhere says who held the shovel. |
| **…and it only comes once** (#711 slice 2) | close the desk and open it again | Nothing. There is no flag to clear: the hole was the record, and somebody dug it. Two drops that came due together are two payments on two visits, one sentence each. |
| **The wrong moon is just a hole** (#711 slice 2 / #319) | [`?dock=the-tilt&parcel=1`](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&parcel=1), then at the boarding panel pick a **different site** before you go down (or fly to another moon) | Bury it there and the pulse is the ordinary one — no extra sentence, no note, no money, ever. The ✗ is on the map, the odds are the chest's own, and you can walk back and dig your box up. A parcel in the wrong ground is a buried parcel, which is exactly what it is. |
| **A man with a form took it, and the work dries up** (#711 slice 1 + 2) | [`?dock=the-tilt&parcel=1`](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&parcel=1) → walk into a **HIVE** floor with the box on you and let a round stop you (slice 1's beat) | The card is slice 1's, unchanged: fined, filed, and the box carried off. Then fly up and open the dark-web desk: **the parcel row is simply not there.** Not greyed, not refusing, not explaining — absent, for two to four watches seeded off the box that was lost. Nothing anywhere says why. Come back a day later and the row is back. |

**Reading the clock while you play.** Both lags are **sim** time and both are measured in the four-hour
watch every roster, patience and fence in this game already turns on. The warp slider is the fast-forward;
there is nothing to sit through.

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
