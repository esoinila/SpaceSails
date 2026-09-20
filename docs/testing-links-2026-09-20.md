# Test links — the 2026-09-20 run (#1253, DOWN BELOW)

*Companion to [`testing-guide.md`](testing-guide.md) Appendix A. Every link boots the live build at
`https://esoinila.github.io/SpaceSails-play/map?…` straight into the situation, using only cheats that exist
in the query whitelist. Read Appendix A for what each key does.*

> **Rows with no mark are rows nobody has opened in a browser yet.** The house rule stands: a mark is only
> ever written here for something somebody actually looked at, and the guards that back these rows are named
> in each one so a reader can tell a law from a play.

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
| `/map?dock=selene-gate&ashore=1&havenfloor=-1` — the one-URL boot. | You are standing **on the service level**, where the first cage's doors open, with `🛗 LIFT · L-00` at your elbow. The location strip reads **LOWER CONCOURSE**. Round you: a corridor that goes the whole way round, a row of green cabin doors along the north wall, cable trays overhead, and two more cars further round the ring. | You come up in the bar, or in a wall, or the strip still says THE EARTHRISE BAR. | — |
| Same link. Walk the corridor **all the way round** the ring, past all five cabin doors and all three cars, and back to where you started. | It goes round. There is no dead end anywhere down here and nothing you can walk into that you cannot walk out of. | A pocket you can get into and not out of; a corner of corridor the cabin row has sealed off. | — guarded by `TheLevelUnderTheConcourseTests.EveryStandableTileOfTheLowerConcourseIsReachable` |
| Same link. Press `[E]` at a cabin plate — `CABIN 3 · CREW`. | It is refused, the way every locked leaf in this game is refused. **No card, no key, no lock to pick and no hint anywhere about whose it is** — #563's ruling: the door is TIME, not a key, and he is inside it. | A card explaining the level; a name on a door; a key that opens one. | — |
| Same link. Press `[E]` at `🛗 LIFT · L-00`. | The **🛗 CAR PANEL** — the same surface the Hive's lift draws — with two buttons on it: **CONCOURSE** and **SERVICE LEVEL**. Under the title: *You are at **SERVICE LEVEL**.* The row you are on is the disabled one. | One button; a refusing row; a band, a keypad, an authority card or a dead-air tag (none of that is a station). | — guarded by `TheLevelUnderTheConcourseTests.ThePanelOffersBothFloorsFromEitherOne` |
| Press **CONCOURSE**. Then walk round the hall to a **different** car, press `[E]`, press **SERVICE LEVEL**, and look at where you are. | You come up at the car you pressed — and when you go back down on the second one you come out **somewhere else** on the lower ring. Three cars, three landings, on both floors, on the same squares as each other. | Every car putting you in the same place (then there is nothing to guess and the tail has no craft in it). | — guarded by `TheRideDownIsAWayBackTests.TheDoorsOpenAtTheCarYouRodeOnEitherFloor` |
| Same link. Ride up, walk down the tube to the observation walk, and check the location strip; then ride back down and stand on the same coordinates one floor lower. | Upstairs it reads **OBSERVATION WALK**. Downstairs the same x/y reads **LOWER CONCOURSE** — the T is the concourse's and does not leak down a floor. | The service level announcing an observation walk with no window in it. | — guarded by `TheLevelUnderTheConcourseTests.TheWalkAndTheGalleryAnswerNothingOnTheFloorBelow` |
| Same link. Stand down there through a **hull shudder** (wait, or `&simhours=`). | The deck shakes and **nothing is said**. The bar's line is about glasses going back down and the concourse's is about a roomful of people; there is nobody in a service corridor to look up. | *"A shudder walks through the concourse and every conversation stops mid-word…"* in a corridor with five shut doors on it. | — guarded by `TheRideDownIsAWayBackTests.TheShudderHasNothingToSayOnTheServiceLevel` |
| Same link. Ride down, then **cast off** without going back up. | You are put back **aboard**, in the airlock corridor, exactly as casting off from the bar does. | The captain undocking while standing in a station that is no longer welded on, with no ship under him. | — guarded by `TheRideDownIsAWayBackTests.CastingOffFromTheServiceLevelPutsHimBackAboard` |
| `/map?dock=selene-gate&ashore=1&simhours=7.5` — the **concourse** check. Walk the hall and read the three panels the cars took. | Three of the ring's sealed panels now read `🛗 SERVICE LEVEL — NO PUBLIC ACCESS · L-00 / L-06 / L-10`. **Every other panel on the ring says exactly what it said before** — same department, same id. | A ring whose departments have shuffled (that is the sealed-edge counter not being stepped over a car). | — guarded by `TheLevelUnderTheConcourseTests.TheCagesCostTheRingThreePlatesAndNotOneWall` |
| Dock at **any other haven** (`/map?ashore=1&dock=red-eye`) and walk the hall. | Nothing has changed anywhere. No cars, no plates, no floor under it. | A lift console on a station with nothing under it — an affordance with nothing behind it. | — guarded by `TheLevelUnderTheConcourseTests.OnlyOneStationHasAFloorUnderItAndTheRestAreUntouched` |

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
| `/map?dock=selene-gate&ashore=1&simhours=7.5` — sit in the bar past last call and watch **GILT-EYE**. | He gets up and crosses the concourse — **not west to the tube**, but to one of the three cars. He presses it and is gone. | He walks straight out to the observation walk (that is the old one-leg route); or he gets up before last call. | — guarded by `HisCabinIsBelowTests.HeLeavesHisChairForACarAndTheNextLegIsAFloorDown` |
| Same link. Note WHICH car he took, walk to it and ride down. | You come out on the service level and **he is in the corridor**, walking — not at the doors you just came out of, but as far along as the time you spent pressing buttons. Follow him and he walks up to one of the five cabin doors and **goes in**. The corridor is empty. | Nobody there; or a man standing exactly where the car put you, as though he had waited. | — guarded by `RidingHisCarPutsTheCaptainOnTheFloorHeIsWalking` |
| Press `[E]` on the door he went through. | Refused. Nothing says it is his and nothing ever will — the plate is `CABIN n · CREW`, and **there is no key anywhere in the game** (#563: the door is time, and he is behind it). | A card; a name; a lock that opens. | — guarded by `TheCabinLeafIsLockedAndCarriesNoName` |
| Wait in the corridor. | About **three minutes** at warp 1, and then the leaf opens and he walks out — to a car. **It may not be the one he came down on.** | An hour of waiting (that is the escort's ceiling, not this wait); or he never comes out. | — guarded by `TheCabinWaitIsDerivedFromTheEscortsPatienceAndLandsOnTheWalksOwnWait` |
| Guess right, ride up after him, hang back at the notice band and let him walk out over the drop. | Exactly the beat that shipped: he goes out to the rail, and on a look nobody is watching him **he is not there**. Walk out after the wait and the card comes up at the rail; the note files under his name. | The vanish happening anywhere but in the gallery; or the card never coming because the route forgot to put him on the concourse at all. | — guarded by `TailingBothLegsArrivesAtTheVanishExactlyAsBefore` |
| **Guess wrong.** Ride a car he did not take, or wait at the wrong one for him to come up. | **Nothing.** You are in a corridor with nobody in it, or standing at a car whose doors do not open. The evening goes on without you, **no card is raised and nothing is spent** — and the walk is still there the next time you tie up. Nothing anywhere tells you that you guessed wrong. | A card for a scene you did not watch; the beat spent; or a message explaining what you missed. | — guarded by `TheWrongCarLosesHimAndCostsNothing` |
| Stand on the CONCOURSE while he is downstairs. | There is nobody up here. He is a floor down, on his own errand, and a station has one deck at a time. | A second copy of him walking the hall while he is also in the corridor. | — guarded by `HeIsOnlyEverDrawnOnTheFloorHisLegIsOn` |

**The one thing worth knowing about the clock.** While you are on his floor he is a **body**, walked over the
same stone as everybody else. While you are on the other floor he is a **schedule**, and his leg takes
exactly as long as a man walking it would take (`NpcWalk.PaceDu`, the same pace). A leg that ran faster
off-screen would be a man who beats a captain who followed him properly; one that ran slower would hold him
for a captain who guessed wrong. Either is the world arranging itself around who happens to be looking, which
is the one thing a tail cannot survive.
