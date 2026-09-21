# Test links — the 2026-09-20 run (#1253, DOWN BELOW)

*Companion to [`testing-guide.md`](testing-guide.md) Appendix A. Every link boots the live build at
`https://esoinila.github.io/SpaceSails-play/map?…` straight into the situation, using only cheats that exist
in the query whitelist. Read Appendix A for what each key does.*

> **Rows with no mark are rows nobody has opened in a browser yet.** The house rule stands: a mark is only
> ever written here for something somebody actually looked at, and the guards that back these rows are named
> in each one so a reader can tell a law from a play.

> ### The `09-21` sweep — the first eyes on Down Below
>
> Every `played` cell below was written on **2026-09-21** against a **Release publish of
> `our-own-ship-has-compartments` at `bfc44a03`** (#1275 + #1276 both in), served from a lane's own port and
> driven headless. Every verdict is a screenshot somebody read; **no row is marked for anything that was
> not seen**, and the ones that could not be driven say so and why rather than inheriting a guard's word.
> **No timings are reported from this bench** — a headless tab's clock is not the owner's.
>
> **§0, the level and the lifts, is essentially sound**: the floor boots, the ring walks the whole way
> round with nothing to get stuck in, the leaf refuses in the locked-leaf idiom, and the whole of the
> tailing mechanic — *three cars, three landings, on both floors* — plays exactly as it is written. Two
> defects on the paint and the panel: [#1279](https://github.com/esoinila/SpaceSails/issues/1279) (three of
> five door numbers overdrawn) and [#1280](https://github.com/esoinila/SpaceSails/issues/1280) (the car
> panel quotes a moon's `SURFACE` / `−150 m` column and hangs a `🫁 air` tag).
>
> **§1 UPDATE, `09-21` run 3 (#1289 merged, base `b3b475f1`): the two-leg night now plays end to end.**
> Boot the documented link, touch nothing, and the whole route runs — chair → **L-06** → **CABIN 1** → the
> three-minute leaf → **L-00** → the hall, the tube, the gallery, the rail, the vanish, the card and the note
> in the book. #1285, #1286 and #1287 were each re-played from the captain's side and each one is good on the
> frame. One new defect, older than the lane and in the way of the last beat:
> [#1290](https://github.com/esoinila/SpaceSails/issues/1290), click-to-walk dying against the gallery rail.
> The run-3 block at the foot of §1 has the whole thing.
>
> **§1, the two-leg night, was a different story on run 2, and mostly not the lane's fault.** The first leg plays and
> plays correctly — he leaves his chair after last call and walks to the **L-06** car, the one his seed
> names, rather than west to the tube. Everything after that was **unreachable from a script**: #1062's
> notice latch fires the moment the captain is anywhere the man can see him, and a noticed tail *stops
> going where it was going* and waits to be gone past — which a scripted captain never does. Two runs of
> nine and five minutes both ended with him standing still and no card. One thing was seen that should not
> have been: [#1281](https://github.com/esoinila/SpaceSails/issues/1281), a nameless body drawn on the
> service level. **The rail-and-card ending has not been re-played since the route grew three legs.**

---

## 0 · THE LEVEL UNDER THE CONCOURSE (#1253)

Owner, 2026-09-20: *"could we add a basement level to the observation deck station, so the tailing task
could start from the basement cabin and end at the observation deck? Otherwise the followed distance is
easily very short. **The main hall could have multiple elevators… good for tailing.**"*

**Selene Gate** — the oldest port in the system, and the one with the most built on it (the observation
walk, the T, the tables, the tail) — has a **LOWER CONCOURSE** under its hall: a service corridor running the
whole way round, a row of **five crew cabins** whose leaves do not open for a captain, worklight, and
**three cages** on three different edges of the twelve-gon.

**The whole mechanic is that the three cars land in three places.** A man who goes down at one edge comes up
at whichever he chooses; a captain who guessed wrong arrives at an empty car and has lost him, and nothing
tells him he guessed wrong. That is the tailing craft the owner asked for.

**What is NOT here yet:** nobody lives down there. The cabins are shut, the corridor is empty, the dark-web
desk's rows are still rows, and the tail still begins and ends on the concourse. PR 2 puts GILT-EYE in one
of those cabins.

### The ride down, the cabins, and the ride up somewhere else

| What to do | What should happen | Broken looks like | played |
|---|---|---|---|
| `/map?dock=selene-gate&ashore=1&havenfloor=-1` — the one-URL boot. | You are standing **on the service level**, where the first cage's doors open, with `🛗 LIFT · L-00` at your elbow. The location strip reads **LOWER CONCOURSE**. Round you: a corridor that goes the whole way round, a row of green cabin doors along the north wall, cable trays overhead, and two more cars further round the ring. | You come up in the bar, or in a wall, or the strip still says THE EARTHRISE BAR. | ✅ `09-21` — **played.** Boots standing at the first cage's doors, `🛗 LIFT · L-00` and `[E]` at the elbow, strip reads **LOWER CONCOURSE**, five green leaves along the north wall, cable trays, and `LIFT · L-06` / `LIFT · L-10` further round. The backdrop is drawn 16:9 and **not stretched** — same letterboxed grammar the concourse's own canvas uses. Zero console errors. `A-a0_boot.png` |
| Same link. Walk the corridor **all the way round** the ring, past all five cabin doors and all three cars, and back to where you started. | It goes round. There is no dead end anywhere down here and nothing you can walk into that you cannot walk out of. | A pocket you can get into and not out of; a corner of corridor the cabin row has sealed off. | ✅ `09-21` — **walked.** North wall → west wall → south wall → east → back north: nothing stopped him, no pocket he could get into and not out of, and all three cars were in sight from the ring. `F-f2_west.png` → `F-f3_southwest.png` → `F-f6_backtostart.png`. NB most of the twelve-gon inside the canvas is unpainted black — **the concourse above is drawn the same way**, so this is the room's grammar and not this floor's fault |
| Same link. Press `[E]` at a cabin plate — `CABIN 3`. | It is refused, the way every locked leaf in this game is refused. **No card, no key, no lock to pick and no hint anywhere about whose it is** — #563's ruling: the door is TIME, not a key, and he is inside it. | A card explaining the level; a name on a door; a key that opens one. | — |
| Same link. Press `[E]` at `🛗 LIFT · L-00`. | The **🛗 CAR PANEL** — the same surface the Hive's lift draws — with two buttons on it: **CONCOURSE** and **SERVICE LEVEL**. Under the title: *You are at **SERVICE LEVEL**.* The row you are on is the disabled one. | One button; a refusing row; a band, a keypad, an authority card or a dead-air tag (none of that is a station). | ⚠ `09-21` — **both floors are offered from either one (#600 stays shut) and the panel is a moon's.** Title `🛗 CAR PANEL`, *You are at **SERVICE LEVEL**.*, `CONCOURSE` and `SERVICE LEVEL`, the row you stand on disabled with `◄ you are here`, one `Close`. Opened again from the concourse: same two rows, the other one disabled. **But the depth column is the Hive's** — `SURFACE` against CONCOURSE and `−150 m` against SERVICE LEVEL, on a station in orbit off Luna — and the other row carries a **`🫁 air` tag**, which this row's own *broken looks like* names. [#1280](https://github.com/esoinila/SpaceSails/issues/1280) · `G-g1_panel.png` |
| Press **CONCOURSE**. Then walk round the hall to a **different** car, press `[E]`, press **SERVICE LEVEL**, and look at where you are. | You come up at the car you pressed — and when you go back down on the second one you come out **somewhere else** on the lower ring. Three cars, three landings, on both floors, on the same squares as each other. | Every car putting you in the same place (then there is nothing to guess and the tail has no craft in it). | ✅ `09-21` — **played, and this is the mechanic working.** Rode up on **L-00** and came out at the ring's **north-east** edge, beside `SERVICE LEVEL — NO PUBLIC ACCESS · L-00`, able to walk away from the landing. Walked round to **L-06**, pressed `SERVICE LEVEL`, and came out **at L-06's own doors below** — bottom-left of the lower ring, nowhere near where L-00 had put him. Rode **L-10** up and came out at the ring's **east-south-east** edge by the lifeboat rail. Three cars, three landings, on both floors. `G-g2_rode_up.png` → `H-h2_down_at_L06.png` → `I-i2_up_at_L10.png` |
| Same link. Ride up, walk down the tube to the observation walk, and check the location strip; then ride back down and stand on the same coordinates one floor lower. | Upstairs it reads **OBSERVATION WALK**. Downstairs the same x/y reads **LOWER CONCOURSE** — the T is the concourse's and does not leak down a floor. | The service level announcing an observation walk with no window in it. | ⚠ `09-21` — **half driven.** Upstairs the strip reads `OBSERVATION WALK` in the tube and `LUNA IMMIGRATION` on the hall; downstairs it reads **LOWER CONCOURSE** everywhere the captain walked, including the south and west of the ring — nothing down there ever claimed a walk or a gallery. The strict same-x/y-one-floor-down comparison was **not** driven: there is no coordinate readout on the glass to line the two floors up by. `T1-c_full.png`, `F-f3_southwest.png` |
| Same link. Stand down there through a **hull shudder** (wait, or `&simhours=`). | The deck shakes and **nothing is said**. The bar's line is about glasses going back down and the concourse's is about a roomful of people; there is nobody in a service corridor to look up. | *"A shudder walks through the concourse and every conversation stops mid-word…"* in a corridor with five shut doors on it. | — `09-21` **not driven.** A shudder cannot be forced from a URL and none landed in any of the windows stood through down there (12 s at `&simhours=3`, and four minutes across the `&simhours=7.5` runs). **Nothing whatever was said on the service level in any of them**, which is the row's outcome but not its test |
| Same link. Ride down, then **cast off** without going back up. | You are put back **aboard**, in the airlock corridor, exactly as casting off from the bar does. | The captain undocking while standing in a station that is no longer welded on, with no ship under him. | — `09-21` **not driven.** Casting off wants the helm, and the docked strip's `Q — helm` road out of a station interior was not reached from below inside this sweep's budget |
| `/map?dock=selene-gate&ashore=1&simhours=7.5` — the **concourse** check. Walk the hall and read the three panels the cars took. | Three of the ring's sealed panels now read `🛗 SERVICE LEVEL — NO PUBLIC ACCESS · L-00 / L-06 / L-10`. **Every other panel on the ring says exactly what it said before** — same department, same id. | A ring whose departments have shuffled (that is the sealed-edge counter not being stepped over a car). | ⚠ `09-21` — **the three plates are there and the ring still reads as a ring.** On the concourse: `SERVICE LEVEL — NO PUBLIC ACCESS · L-00` (north-east), `· L-06` (south-west) and `· L-10` (east-south-east), with `HABITAT RING · L-03`, `CUSTOMS · L-01`, `BERTH · L-04`, `SECURITY · L-11`, `BERTH · L-07`, `DOCKMASTER · L-09`, `PIRATE INSURANCE`, `NEBULA MUTUAL · CLAIMS`, `LIFEBOAT STATION`, `LUNA IMMIGRATION` all still on it. **"Same as before" was not proved** — there is no pre-#1253 build on this bench to diff the ring against. `G-g2_rode_up.png` |
| Dock at **any other haven** (`/map?ashore=1&dock=red-eye`) and walk the hall. | Nothing has changed anywhere. No cars, no plates, no floor under it. | A lift console on a station with nothing under it — an affordance with nothing behind it. | ✅ `09-21` — **played at The Red Eye.** No car, no console, no `SERVICE LEVEL` plate anywhere on the ring: `BERTH · J-00`, `BERTH · J-04`, `PIRATE INSURANCE`, `MEDBAY`, `NEBULA MUTUAL · CLAIMS`, `SECURITY · J-11`, `DEDICATION PLAQUE`, `BONDED STORES · J-06`, `LIFEBOAT STATION`, `TRANSIT · J-10`, `BERTH · J-07`, `DOCKMASTER · J-09`, `JUPITER IMMIGRATION`. Nothing under it. `U1-b.png` |

**The one number worth knowing.** The cabins are **one deck unit under**
`UndergroundComplex.FireCodeSmallRoomDu` on their longest side, which is how a one-leaf room is let off the
fire code's second door under the exemption that already exists (#822's original, and the only dimensional
one). The corridor itself takes **no** exemption: three cars are three ways out, which is more than the code
asks for.

**And the way home is proved rather than assumed.** #600's scar is an A* audit that proves you can REACH a
lift and never that the lift is a way HOME, and it survived three PRs. So the berth column asks all three
legs of every square down there: a car is reachable, the panel on this floor offers the **CONCOURSE**, and
the **gangway** is reachable from where those doors let you out. A service level carries no tube and no
airlock; a captain who could not get back to his own would be marooned in a station rather than in a lab.

---

## 1 · HIS CABIN IS BELOW — THE TWO-LEG ROUTE (#1253 slice 2)

Owner, the same morning: *"…so the tailing task could **start from the basement cabin** and end at the
observation deck? **Otherwise the followed distance is easily very short.**"*

The tail's route was one leg — up from a chair, across the concourse, out over the drop — and a captain who
happened to be looking got the whole of it. **It is five legs now**, and three of them are on a floor the
captain has to decide to follow him onto, on a car he has to guess:

> the bar → **a car** → **his own cabin** (he goes in; the leaf shuts) → **a wait** → **a car, which may not
> be the one he came down on** → the hall → the tube → the gallery → the vanish.

**Nothing about the ending changed.** From the frame he steps out of the car on the concourse, every rule
#1254 ships is the one that runs: the notice band, the rail, the three-minute wait, the newspaper, the
turning back, the card at the blind end and the note under his name.

**Which cabin and which two cars are SEEDED**, not rolled: the same universe answers the same every visit,
which is the only reason there is anything to learn. The car he comes back up on is a *different bit of the
same seed* — so a captain who has learnt where he goes down has learnt exactly half of it.

### Following him down, and following him back up

| What to do | What should happen | Broken looks like | played |
|---|---|---|---|
| `/map?dock=selene-gate&ashore=1&simhours=7.5` — sit in the bar past last call and watch **GILT-EYE**. | He gets up and crosses the concourse — **not west to the tube**, but to one of the three cars. He presses it and is gone. | He walks straight out to the observation walk (that is the old one-leg route); or he gets up before last call. | ✅ `09-21 run 3` — **[#1285](https://github.com/esoinila/SpaceSails/issues/1285) is fixed: the night starts for a captain who only watches.** Boot the documented link on `b3b475f1` and touch **nothing at all**: he is out of his chair and already crossing the concourse on the second look (`A2-t010.png`, captain's dot on the bar threshold where `?ashore=1` put him and not a pixel moved), heading **south-west, not west to the tube** (`A3-c012.png`), and gone off the ring into the **L-06** car two looks later (`A3-c014.png`). Six frames across the first minute, six different hashes — the room is running, and it is running whether or not the captain is the sort who walks.<br>Before #1289: ❌ [#1285](https://github.com/esoinila/SpaceSails/issues/1285) — **he got up only if the captain moved first.** Boot the link and touch nothing and he is in his chair at +0, +30, +60, +120, +180, +240 and **+300 s** (`W2-c000.png`…`W2-c300.png`), with the whole room frozen round him; three presses of an unbound key change nothing in 90 s (`CTL-z00/z40/z90.png`, byte-identical). Hold `A` for 1.2 s and the evening starts at once: out of the chair at +2 s, crossing the concourse at +8 s (`N2-t08.png`), at the **L-06** car at +15 s (`N3-t15.png`), gone at +18 s. The route itself is right — it is the deal that will not fire.<br>— guarded by `HisCabinIsBelowTests.HeLeavesHisChairForACarAndTheNextLegIsAFloorDown` |
| **Try it on other watches** — the same link with `simhours=` wound forward a shift at a time (11.5, 15.5, 19.5…). | On **every** watch the rota has him in the room, he is still in his chair at last call and the night happens. The room's own hours empty other chairs around him and never his. | An evening where he is simply not there after last call, and nothing says why — that was #1277, and it was some evenings and not others with nothing to tell them apart. | ⚠ `09-21` — **partly driven.** The rota has him in the room at 7.5 and 11.5 (seated, in a different chair each watch — `WH11.5-c.png`), and 15.5 dealt an ordinary shudder card over the room (`WH15.5-c.png`), which is the sim plainly running. But with the captain unmoved he stays in the chair on every one of them, so what this row is about is masked by [#1285](https://github.com/esoinila/SpaceSails/issues/1285) until that is fixed. 19.5 not driven.<br>— guarded by `TheTailWinsTheChairTests.TheHoursNeverScheduleTheManTheWalkHasClaimed` (128 watches swept) |
| Same link. Note WHICH car he took, walk to it and ride down. | You come out on the service level and **he is in the corridor**, walking — not at the doors you just came out of, but as far along as the time you spent pressing buttons. Follow him and he walks up to one of the five cabin doors and **goes in**. The corridor is empty. | Nobody there; or a man standing exactly where the car put you, as though he had waited. | ✅ `09-21 run 3` — **[#1286](https://github.com/esoinila/SpaceSails/issues/1286) is fixed: you land clear of him and he walks on.** Stand at **L-06** before he gets there, let him press it, ride the same car down a beat behind him — and he is **up the corridor, on his own line, a clear gap of stone between him and the captain's square**, plated `Gilt-Eye` (`A5-d00.png`, `A6-d00.png` at the corridor's own scale). He is not waiting: on the next looks he has moved on up the row (`A6-d03.png`) and then he is **behind a leaf and the corridor is empty** (`A6-d04.png`…`A6-d08.png`, one hash). No body at the elbow, no body that never moves again.<br>Before #1289: ❌ [#1286](https://github.com/esoinila/SpaceSails/issues/1286) — **the car was right and the arrival was this row's own *broken*.** He takes **L-06** (`N3-t15.png`); ride the same car down and he is standing *exactly where the car put you*, plated `Gilt-Eye` at the captain's elbow on the landing — and he never moves again: **twelve frames over 245 s, one md5** (`G1-d00.png`…`G1-d245.png`). It is the captain's own square: step ~13 du down the corridor and he walks off on the frame he is cleared (`G2-b_stepped.png`), east along the row at 2.0 du/s — `NpcWalk.PaceDu` to the digit — and into **CABIN 5** (`H1-e04/e06/e08/e10/e12.png`). #1281 in the mirror: #1284 ends a leg the captain stands on the END of; this is a leg he stands on the START of.<br>— guarded by `RidingHisCarPutsTheCaptainOnTheFloorHeIsWalking` |
| Press `[E]` on the door he went through. | Refused. Nothing says it is his and nothing ever will — the plate is `CABIN n`, and **there is no key anywhere in the game** (#563: the door is time, and he is behind it). | A card; a name; a lock that opens. | ✅ `09-21 run 3` — **refused, and the plate is a number.** Followed him down on his own car, watched him step off his line into a leaf, walked to that leaf and pressed `[E]`: **`CABIN 1 — sealed. You knock; only the station's hum answers. 🔒`**, verbatim, on the glass (`A8-ref1.png`). No card, no name, no lock, no `· CREW` — the idiom holds to the word. **NB the number.** The leaf he goes through at this link is **CABIN 1**, read off the plate on the frame he is behind it; the earlier row below says CABIN 5, and that reading was taken through [#1287](https://github.com/esoinila/SpaceSails/issues/1287), which re-dealt him at the landing to walk the corridor a second time. The seed is the same either way, so one of the two readings is of a corridor he had already walked — and it is not this one.<br>Earlier `09-21`: **refused, and the plate is a number.** At the leaf he went through: **`CABIN 5 — sealed. You knock; only the station's hum answers. 🔒`**, verbatim, on the glass (`J1-b_refusal.png`). No card, no name, no lock, no `· CREW`. And #1279's law holds on the frame — the five plates are **one row, one baseline, evenly spaced, none touching**: `CABIN 1 · 2 · 3 · 4 · 5` at dpr 3 (`K1-plates.png`, `H1-plates.png`).<br>— guarded by `TheCabinLeafIsLockedAndCarriesNoName` |
| Wait in the corridor. | About **three minutes** at warp 1, and then the leaf opens and he walks out — to a car. **It may not be the one he came down on.** | An hour of waiting (that is the escort's ceiling, not this wait); or he never comes out. | ✅ `09-21 run 3` — **[#1287](https://github.com/esoinila/SpaceSails/issues/1287) is fixed: the wait is ticked for the man standing in the corridor.** Rode down on his car, walked a dozen paces clear of the landing and **stood there doing nothing**. Fourteen frames carry one hash while the leaf is shut — and then it opens and **he is out and walking**, plated `Gilt-Eye`, coming back up the row past the captain (`A9-t185.png` → `A9-t190.png`), and gone into a car two looks later. The wait is the same on the other floor — stand on the concourse instead and he is back up there with nothing touched (`B1-t205.png`). **Which car:** he goes down on **L-06** and comes back up on **L-00**, the north-east cage — a different bit of the same seed, exactly as the row promises, so a captain who learnt where he goes down has learnt half of it (`A10-t194.png`, walking clear of CABIN 5 towards L-00's doors, gone on the next frame).<br>Before #1289: ❌ [#1287](https://github.com/esoinila/SpaceSails/issues/1287) — **wait in the corridor and the leaf never opened** — the row's own *broken*. Down on **his** car and standing 14 du clear: nothing at +180, +240, +300, +390 and **+570 s** (`K1-t180.png`…`K1-t570.png`; six frames carry two hashes, which is the corridor lamp's own loop and no body). Down on the **wrong** car, never within 20 du of him: nothing from +162 to **+302 s** (`WC2-b_t120.png`…`WC2-h_t260.png`, seven frames one md5). The three minutes are real and the arithmetic is right — stay on the CONCOURSE instead and he is back up at **+220 s** (`UP-t220.png`) — but `StepHisNight`'s body branch has no arm for a man behind a leaf, so the wait is ticked by the one observer who cannot see it.<br>— guarded by `TheCabinWaitIsDerivedFromTheEscortsPatienceAndLandsOnTheWalksOwnWait` |
| Guess right, ride up after him, hang back at the notice band and let him walk out over the drop. | Exactly the beat that shipped: he goes out to the rail, and on a look nobody is watching him **he is not there**. Walk out after the wait and the card comes up at the rail; the note files under his name. | The vanish happening anywhere but in the gallery; or the card never coming because the route forgot to put him on the concourse at all. | ✅ `09-21 run 3` — **played end to end, for the first time, from the bar to the note in the book.** Rode up on **L-00** behind him, followed across the hall and down the tube keeping him in sight, and he was **standing at the rail in the gallery** when the captain came through the throat (`A16-c2.png`, `A16-c4.png` — `Gilt-Eye` plated, at the glass, captain at the north table with `[E]`). Sat at the **north table** (`You sit down. The table is yours…`, with `📰 Read the news` on it), opened the paper — and on the look after that **he is not there** (`A16-c5.png`: the rail is empty, the table panel is still up, nothing was said). Stayed sat, and the card comes up at the rail, verbatim:<br>**`👁 THE OBSERVATION WALK`** — *"The walk is lit the whole way out. The floor is glass and the drop is under it, and the far end is a wall with a rail. There is nobody here, and there is nowhere here to be."* — one page, one `…go on` (`A17-p180.png`, `A18-card1.png`). **And the note files under his name**: `✍ Work the case` → `📓 NOTES` → `Selene Gate · THE EARTHRISE BAR` carries **`👁 Followed GILT-EYE out onto the observation walk.`**, tagged `loose end` (`A19-notes.png`). Nothing anywhere names the cabin, the car or the wait.<br>**One thing in the way, and it is not this lane's:** a captain who walks to the rail before sitting fetches up against the west wall and **cannot click-to-walk anywhere after that** — [#1290](https://github.com/esoinila/SpaceSails/issues/1290). The keys still work; the mouse does not.<br>Earlier `09-21`: not driven, and **not drivable**: [#1287](https://github.com/esoinila/SpaceSails/issues/1287) meant a captain who followed him down never saw the leaf open, so the car up, the hall, the tube, the gallery, the vanish, the card at the rail and the note under his name were never reached from the two-leg route. The far end is intact and walkable on its own — the tube, the gallery, the binoculars and both tables are there (`Y1-d.png`).<br>— guarded by `TailingBothLegsArrivesAtTheVanishExactlyAsBefore` |
| **Guess wrong.** Ride a car he did not take, or wait at the wrong one for him to come up. | **Nothing.** You are in a corridor with nobody in it, or standing at a car whose doors do not open. The evening goes on without you, **no card is raised and nothing is spent** — and the walk is still there the next time you tie up. Nothing anywhere tells you that you guessed wrong. | A card for a scene you did not watch; the beat spent; or a message explaining what you missed. | ✅ `09-21 run 3` — **nothing at the landing, no card, nothing spent.** Rode **L-10** down — a car he takes neither way — and the corridor at those doors was **empty on arrival and stayed empty**, five frames one hash, no card, no line, no pulse (`C1-d00.png`, `C1-d25.png`…`C1-d265.png`); rode **L-10** back up and the hall had gone on without him (`C1-up20.png`). Purse **1,500 cr** and cargo 5/10 on every frame of it, start to finish. The car panel at L-10 is the clean one too — `🛗 CAR PANEL`, *You are at **CONCOURSE**.*, two rows with `◄ you are here` on the one you stand on, one `Close`, **no depth span and no `🫁 air` tag** (`C1-panel.png`): #1280 holds on the third cage as well.<br>**Worth knowing, and not a defect:** the corridor is one ring, so a captain who guessed the wrong CAR but stays down there long enough does eventually see him — he came out of his leaf and crossed the far end of the row on his way to L-00, plated `Gilt-Eye`, while the captain stood at L-10's doors (`C1-d185.png`, `crop-d185.png`). Nothing told the captain he had guessed wrong; he simply had to be looking. That is the craft, not a leak.<br>Earlier `09-21`: **nothing, and nothing says so.** Rode **L-10**, a car he did not take: the lower concourse is empty, all three cages unoccupied, no card, no line, no pulse at +0, +25 and +65 s (`WC-c_below.png`, `WC-d_below25.png`, `WC-e_below65.png`), purse unmoved at 1,500 cr throughout. The *not spent* half (cast off, come back, tie up again) was not driven.<br>— guarded by `TheWrongCarLosesHimAndCostsNothing` |
| Stand on the CONCOURSE while he is downstairs. | There is nobody up here. He is a floor down, on his own errand, and a station has one deck at a time. | A second copy of him walking the hall while he is also in the corridor. | ✅ `09-21 run 3` — **one deck at a time, and the whole wait ticks.** Booted the link and **stayed upstairs, touching nothing**: he crosses the ring, takes L-06 and the hall is empty of him — no second copy anywhere on it while he is a floor below, seven frames of ordinary concourse (`B1-t030.png`…`B1-t185.png`). Then he **comes back up at L-00**, the north-east cage, stepping off its landing onto the hall (`B1-t205.png`) and crossing west for the tube on the next look (`B1-t210.png`), and out along the walk into the gallery, where he stands at the rail (`B2-m3.png`). One deck at a time, both ways.<br>Earlier `09-21`: **one deck at a time.** He is at the L-06 doors at +15 s and the ring is empty of him at +18 and +21 s while he is a floor below (`N3-t15.png` → `N3-t18.png` → `N3-t21.png`); no second copy anywhere on the hall, and he reappears on the concourse only when his own leg puts him there, at +220 s (`UP-t220.png`).<br>— guarded by `HeIsOnlyEverDrawnOnTheFloorHisLegIsOn` |

**#1277 · THE BEAT IS REACHABLE ON EVERY WATCH THAT DEALS IT.** Until this was ruled on, #731's hours and
this tail both wanted the same man out of the same chair and the hours got there first: on a watch whose
schedule named GILT-EYE the room walked him out through a cellar leaf an hour before last call, and the whole
two-leg night silently did not happen. A tester at the link above saw an ordinary bar and had no way to tell
a bug from an evening he was not in. **The tail wins now** — the hours' departure roster defers to the man the
walk has claimed, and the rota's own departure of him *is* the tail's first leg. Nothing else about the room's
hours moved.

**The one thing worth knowing about the clock.** While you are on his floor he is a **body**, walked over the
same stone as everybody else. While you are on the other floor he is a **schedule**, and his leg takes
exactly as long as a man walking it would take (`NpcWalk.PaceDu`, the same pace). A leg that ran faster
off-screen would be a man who beats a captain who followed him properly; one that ran slower would hold him
for a captain who guessed wrong. Either is the world arranging itself around who happens to be looking, which
is the one thing a tail cannot survive.

---

### 🌙 THE NIGHT AS IT PLAYED, 2026-09-21 (run 3, #1289 in) — end to end, and it does not stop

Driven on `our-own-ship-has-compartments` @ `b3b475f1` (#1289 merged), a **Release publish** of that commit
served from this lane's own port and driven headless, warp 1, one-shot scripts only, every decisive frame
read off the pixels. **No timings are reported from this bench** — a headless tab's clock is not the owner's,
so everything below is beats and frames.

**All three of the night's stops are gone, and the whole route plays.** At the documented link, a captain who
boots and *watches* gets the evening: the man leaves his chair on his own, crosses the concourse, takes the
**L-06** cage, is put down on the service level **clear of a captain who followed him onto the same car**,
walks the row and goes in at **CABIN 1**; the leaf opens again on the third minute **for the man standing in
the corridor**, he comes out and takes the **L-00** cage back up — a different cage from the one he went down
on — crosses the hall, goes out along the tube to the gallery and stands at the rail. Sit at the north table,
open the paper, and on the look after that he is not there; the card comes up at the rail and the note files
in the book under his name.

| the three fixes | what was seen |
|---|---|
| **#1285** — the courtesy has a clock | Boot and touch nothing: he is off the concourse and into his cage inside the first minute, captain's dot never moved (`A2-t000/t010/t020.png`, `A3-c012/c014.png`) |
| **#1286** — he is placed clear of the captain | Ride his own cage down a beat behind him: a clear gap of stone, and he walks on and into his leaf on the next looks (`A5-d00.png`, `A6-d00/d03/d04.png`) |
| **#1287** — one clock on every floor | Wait in the corridor and the leaf opens (`A9-t180.png` → `A9-t185.png`); wait on the concourse and he comes back up (`B1-t205.png`) |

**The far end, reached from the two-leg route for the first time.** The card at the rail, verbatim and one
page: **`👁 THE OBSERVATION WALK`** — *"The walk is lit the whole way out. The floor is glass and the drop is
under it, and the far end is a wall with a rail. There is nobody here, and there is nowhere here to be."*
The book's note: **`👁 Followed GILT-EYE out onto the observation walk.`**, `loose end`, under
`Selene Gate · THE EARTHRISE BAR`. Nothing anywhere names the cabin, the cage or the wait.

**One new defect, and it is older than this lane.**
[#1290](https://github.com/esoinila/SpaceSails/issues/1290) — a captain who walks all the way to the
gallery's west rail **cannot click-to-walk anywhere after that**; the keys still move him. Four runs, four
dead click clusters, including a click on open tube floor behind him. It is in the way of the night's last
beat, because the table you have to sit at is two paces from the wall you fetched up against.

**Regression sample, and the quiet things.** Level 0 at `/map?dock=selene-gate&ashore=1` is the same room it
was: same bar and barkeep, same twelve panels and ids with the three `SERVICE LEVEL` plates on L-00 / L-06 /
L-10, same T out to the observation walk, purse 1,500 cr (`Z1-boot6.png`). The five cabin plates are one row,
one baseline, evenly spaced, none touching (`A6-d00.png`): #1279 holds. The car panel is two columns with no
depth span and no `🫁 air` tag at **all three cages** (`A4-panel.png`, `C1-panel.png`): #1280 holds. The body
below is **named** (`Gilt-Eye`, `crop-d185.png` at the corridor's own scale): #1281 holds. **No application
console error in any run** — the only console line the bench ever raised was this lane's own static file
server losing a `_framework/*.wasm` preload race, which the runtime retries and which never reaches the game.

**The one thing not seen.** In the *stay-upstairs* run the card was **not** reached: the man comes up, crosses
the hall and stands at the rail on the frame, but the scripted captain could not be seated at the gallery
table behind him — he fetched up against the rail wall and [#1290](https://github.com/esoinila/SpaceSails/issues/1290)
took the mouse away. The card and the note in that run are therefore **unreported**, not confirmed.

---

### 🌙 THE NIGHT AS IT PLAYED, 2026-09-21 (run 2) — and the three places it stopped

Driven end to end on `our-own-ship-has-compartments` @ `ade4f1e2` (#1284 merged), a Release publish served
locally, headless, warp 1, one-shot scripts, **zero console errors and zero failed requests in every run**.

**#1284's four fixes are all good on the frame.** The car panel at a berth is two columns — the floor's plate
and `◄ you are here`, `You are at CONCOURSE.` over it, **no depth span and no air tag** (`F2-b.png`, and from
the service level `U1-panel.png`): #1280 holds. The five cabin plates are **one row, one baseline, evenly
spaced, none touching** (`K1-plates.png`, dpr 3): #1279 holds. The body at the captain's elbow below is
**named** (`Gilt-Eye`, never nameless): that half of #1281 holds. And the notice band is no longer the hall:
he crosses the whole concourse, at his own pace, with the captain standing 12–30 du behind him in his line and
never once clocking him (`N3-t12.png` → `N3-t15.png`): **#1283 holds, and it is what lets the first leg be
watched at all.**

**And the night still cannot be played.** Three separate stops, each on its own row above:

1. [#1285](https://github.com/esoinila/SpaceSails/issues/1285) — it does not START. At the documented link,
   a captain who boots and watches gets an ordinary bar for ever; one step and the whole evening runs.
2. [#1286](https://github.com/esoinila/SpaceSails/issues/1286) — following him onto **his own car** sets the
   captain down on the square the ride set *him* down on, and `NpcWalk`'s courtesy freezes him there for good
   (245 s, twelve identical frames). This is #1281's shape at a leg's **start** rather than its end.
3. [#1287](https://github.com/esoinila/SpaceSails/issues/1287) — and the one with no way round it: the cabin
   wait is only ticked while the captain is on the **other** floor, so *"wait in the corridor"* is the one
   instruction in this table that guarantees the leaf never opens. Watched to +570 s on his car and to +302 s
   on the wrong one.

The half of the night that does not need the captain below is sound, and worth saying out loud: leave him
upstairs and he goes down at +17 s, is behind CABIN 5's leaf, and **walks back onto the concourse at +220 s**
heading west for the tube — 17 + 12 + the full 180 s of
`PatronRota.WatchSeconds × Escort.PatienceFraction / 20`, to the second (`UP-t220.png`). The clock and the
arithmetic agree; it is only the watching that breaks them.

**Regression sample.** Level 0 at `/map?dock=selene-gate&ashore=1` is frame-for-frame what the previous crew
photographed at the same link before #1284 — same bar, same ring, same twelve panels and ids, same T out to
the observation walk; the only difference is the salesman standing somewhere else on his round
(`Z1-boot.png` beside the earlier `D-d0_boot.png`). The gallery at the far end of the tube is intact — rail,
coin binoculars, **two** tables north and south, three vending machines (`Y1-d.png`); the **newspaper** beat
itself was not reachable, because it belongs to a man who never gets there.
