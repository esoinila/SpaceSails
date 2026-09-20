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
> **§1, the two-leg night, is a different story, and mostly not the lane's fault.** The first leg plays and
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
| Same link. Press `[E]` at a cabin plate — `CABIN 3 · CREW`. | It is refused, the way every locked leaf in this game is refused. **No card, no key, no lock to pick and no hint anywhere about whose it is** — #563's ruling: the door is TIME, not a key, and he is inside it. | A card explaining the level; a name on a door; a key that opens one. | ⚠ `09-21` — **the refusal is exactly right and the PLATES ARE NOT.** `[E]` at the fifth leaf: *"CABIN 5 · CREW — sealed. You knock; only the station's hum answers. 🔒"* — the locked-leaf idiom, no card, no name, no lock. But the five plates are **drawn on top of each other**: `CABIN 1 · CREW` collides with `CABIN 2 · CREW`, `CABIN 3 ·` is overwritten by `CABIN 4 · CREW`, and the fifth reads only `CREW`. [#1279](https://github.com/esoinila/SpaceSails/issues/1279) · `C-c0_row.png`, `B-b0_plates.png` |
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
| `/map?dock=selene-gate&ashore=1&simhours=7.5` — sit in the bar past last call and watch **GILT-EYE**. | He gets up and crosses the concourse — **not west to the tube**, but to one of the three cars. He presses it and is gone. | He walks straight out to the observation walk (that is the old one-leg route); or he gets up before last call. | ✅ `09-21` — **played, and it is the new route and not the old one.** He is at his top at boot (`Gilt-Eye` on the plate), gets up after last call, goes out through the bar's south door onto the concourse (`Q1-q1_at_him.png`), and walks **south-west** — **not** west to the tube — as far as the landing of the **L-06** car, one pace in off its doors, which is exactly the car `TheTailsNight.TheCarHeTakesDown("selene-gate","GILT-EYE",3)` names (`T1-c_full.png`, the grey labelled `Gilt-Eye` beside `SERVICE LEVEL — NO PUBLIC ACCESS · L-06`) |
| Same link. Note WHICH car he took, walk to it and ride down. | You come out on the service level and **he is in the corridor**, walking — not at the doors you just came out of, but as far along as the time you spent pressing buttons. Follow him and he walks up to one of the five cabin doors and **goes in**. The corridor is empty. | Nobody there; or a man standing exactly where the car put you, as though he had waited. | ⚠ `09-21` — **not reachable in a scripted run, and the reason is the shipped notice rule and not this lane.** A captain who is anywhere GILT-EYE can see him is NOTICED, and #1062's latch then does exactly what it promises: *he stops going where he was going* and waits to be gone past — permanently, because a script never goes past. Standing still in the bar froze him two paces from his top for five minutes (`T2-m1.png` = `T2-m2.png`); crossing the concourse behind him froze him at the L-06 landing for nine (`T1-c_full.png`). **A body IS placed on the service level when the captain rides down mid-leg** — eighteen seconds after the doors open, unprompted (`S2-b.png`), with an empty corridor on the arriving frame (`S2-a.png`) and an empty one in the `&simhours=5` control (`S4-b.png`) — but it has **no plate over it** and does not walk, so it cannot be read as him. [#1281](https://github.com/esoinila/SpaceSails/issues/1281) |
| Press `[E]` on the door he went through. | Refused. Nothing says it is his and nothing ever will — the plate is `CABIN n · CREW`, and **there is no key anywhere in the game** (#563: the door is time, and he is behind it). | A card; a name; a lock that opens. | ⚠ `09-21` — **the leaf half is played; the "the door he went through" half is not.** `[E]` on a cabin plate answers *"CABIN 5 · CREW — sealed. You knock; only the station's hum answers. 🔒"* — refused, no card, no name, no key anywhere (`E-e2_E_cabin.png`). He was never seen to go into one (row above), so which leaf is his is still unwatched. And **three of the five plates cannot be read at all**, which is the one observable this beat is made of — [#1279](https://github.com/esoinila/SpaceSails/issues/1279) |
| Wait in the corridor. | About **three minutes** at warp 1, and then the leaf opens and he walks out — to a car. **It may not be the one he came down on.** | An hour of waiting (that is the escort's ceiling, not this wait); or he never comes out. | — `09-21` **not driven.** Nothing was ever seen to go behind a leaf, so there was no wait to stand through. (No timings are reported from this bench in any case — a headless tab's clock is not the owner's.) |
| Guess right, ride up after him, hang back at the notice band and let him walk out over the drop. | Exactly the beat that shipped: he goes out to the rail, and on a look nobody is watching him **he is not there**. Walk out after the wait and the card comes up at the rail; the note files under his name. | The vanish happening anywhere but in the gallery; or the card never coming because the route forgot to put him on the concourse at all. | ❗ `09-21` — **the card did not come, in two long runs, and neither run proves a bug.** Run 1: straight out west to the tube and wait at the rail — nine and a half minutes of sim, no `There is nobody here` (`T1-c_full.png`, and he is still standing at the L-06 landing in that frame). Run 2: five minutes standing still in the bar first, then out to the walk — same silence. **In both, he had been NOTICED**, and a noticed tail raises no card by design. The ending this row asks for therefore stands on the `09-18` run in [`testing-links-2026-09-17.md`](testing-links-2026-09-17.md) §1 and has **not** been re-played since the route grew three legs |
| **Guess wrong.** Ride a car he did not take, or wait at the wrong one for him to come up. | **Nothing.** You are in a corridor with nobody in it, or standing at a car whose doors do not open. The evening goes on without you, **no card is raised and nothing is spent** — and the walk is still there the next time you tie up. Nothing anywhere tells you that you guessed wrong. | A card for a scene you did not watch; the beat spent; or a message explaining what you missed. | ⚠ `09-21` — **the "costs nothing" half is played.** Rode the captain down on **L-00** while the evening was running and spent four minutes on the service level: no card, no line, no pulse, nothing in the book, nothing taken, zero console errors (`S2-*.png`, `S3-*.png`). The "corridor with nobody in it" half is **contradicted** by the nameless body that appears there — [#1281](https://github.com/esoinila/SpaceSails/issues/1281) |
| Stand on the CONCOURSE while he is downstairs. | There is nobody up here. He is a floor down, on his own errand, and a station has one deck at a time. | A second copy of him walking the hall while he is also in the corridor. | — `09-21` **not driven as written.** He never got off the concourse in any run (he was noticed and stopped there), so there was never a frame with him below and the captain above to compare. What WAS seen is the other half of the law holding: with the captain on the service level the concourse's own people — the barkeep, the regulars, the walkers — are **not** drawn down there at all, at a busy watch (`I-i0_busyhour_below.png`, `&simhours=3`) and at a quiet one |

**The one thing worth knowing about the clock.** While you are on his floor he is a **body**, walked over the
same stone as everybody else. While you are on the other floor he is a **schedule**, and his leg takes
exactly as long as a man walking it would take (`NpcWalk.PaceDu`, the same pace). A leg that ran faster
off-screen would be a man who beats a captain who followed him properly; one that ran slower would hold him
for a captain who guessed wrong. Either is the world arranging itself around who happens to be looking, which
is the one thing a tail cannot survive.
