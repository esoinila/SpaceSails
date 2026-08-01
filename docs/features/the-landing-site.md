# The landing site

*What a moon's ground has to be before it ships. Written 2026-08-01, after two days of the owner playing
Miranda and finding, by eye, eleven things reasoning had not.*

This is a **spec, not a description**. Every numbered rule below is either enforced by a guard in
`tests/SpaceSails.Core.Tests` / `tests/SpaceSails.Client.Tests`, or is named here as not-yet-enforced so
nobody mistakes silence for coverage.

---

## Why this document exists

Every expensive bug on this ground had one shape: **two sources of truth for one fact.**

| What disagreed | How it showed up |
| --- | --- |
| The field envelope, retyped in four test files and a lab | The world grew 16×; every audit went on flooding the old dead world and passing |
| `SpecFor` (one shelter) vs `SpecsFor` (several) | Beacons pointed at buildings that had never been built — *"the map lies"* |
| Three placers, three claim ledgers | A shelter, a hut and a maze fixture grew into one mega-complex — *"kind of funny"* |
| A claim taken before the builder's edge clamp | A building was claimed in one place and built in another |
| One console kind used for two different doors | Pressing E on a shelter door **flew the captain home** |
| A *rejecting* claim used to *record* an existing thing | Two legally-spaced shelters knocked each other out of the ledger; a hut was then built on one |
| A keep-out list passed as a *parameter* to a public function | The routed path built a different away ground than the one tests, the region builder and the labs measured |
| A footprint clamped by its *axis* half-extents | Rotated buildings hung their corners over the edge lane — at some angles and not others |
| The sim vs the sentence | A suffocation narrated as a debt-collector killing |
| A test pinning what I wrote, not what shipped | Green tests over a death card nobody could reach |
| One source, consumed **out of order** (#587) | A wall-builder's cursor walked backwards and sealed the two mouths it was opening — rooms drawn and unreachable on 35 floors |

So the standing rule for this ground is: **one copy, read by everyone, plus a guard that walks the real
object.** A guard that reads the generator's inputs instead of its output is not a guard.

> A corollary learned the hard way: `tests/SpaceSails.Client.Tests` was created to audit client geometry and
> was **never added to `SpaceSails.slnx`**, so CI never once compiled it. A guard nobody executes is a
> comment with a csproj attached. It is in the solution now.

---

## 1 · The ground

1.1 **One field envelope.** `SurfaceLayout.DefaultField` is the only place the bounds exist.
`MoonSurface.ExpeditionField()` reads it; nothing re-types the numbers. *(Enforced: the audits derive from
`DefaultField`.)*

1.2 **No rectangle.** The field's edge is `SurfaceEdge.Bound` — a wandering, outward-only bulge with a
`sin(πt)` corner taper so the chain closes. Only the top rim keeps hull ink; the other three edges are
`Unseen` walls: they collide and are never drawn.

> *"the rectangular fence spoils the site feeling... if our space has limits for some technical reasons then
> let's not advertise it, more like hide that fact."* The honest limit is the tether — your magazine, your
> tank, and the pack behind you — not a drawn line.

1.3 **The body's own geography draws as stone, not hull.** `IsStone` is heavy ink in rock; `IsHull` is the
ship's cold pressure stroke and belongs on made pressure boundaries only.

1.4 **Scenery is drawn and never collided.** Crater rims, scree, ridges, rilles live in `DeckPlan.Scenery`,
*not* in `Walls` — `CollisionSegments` is derived from `Walls`, so a decorative rim placed as a wall would
become an invisible fence.

## 2 · Buildings

2.1 **No U-shapes.** A rectangle with a side left off is not a ruin. Every structure has a real threshold you
walk *through*, and an interior partition with its own offset doorway, so a small footprint still gives two
spaces.

2.2 **Walls have thickness.** 1.6–3.0 du of piled regolith — the owner's Greenland-longhouse reasoning: on a
cold world you build from what is under your boots, and if the wall also holds pressure you build it fat.
Comfortably above the captain's 1.4 du width, so hatching never emits a segment shorter than a body.

2.3 **Every building has a door.** Door faces are ranked by length **with a floor of one**. A 12-sided drum
has 3.7 du faces; picking faces by index and dropping short ones produced sealed O-shapes with treasure
inside and no way in.

2.4 **Nothing is built on top of anything else.** One claim ledger, shared by the seeded features, the
outlying buildings and the shelters. Radii come from `SurfaceStructure.KeepOutRadius` — the **half-diagonal
plus the wall**, honest at every angle. A centre-distance test is only honest for a circle; a rotated 20×16
box with 3 du walls sweeps ~16 du, so two buildings 30 du apart overlap by design.
*(Enforced: `NoBuildingGrowsIntoAnotherTests`.)*

2.5 **A claim is taken on the FINAL position.** `AddStructure` clamps a centre inward to keep walls off the
edge lane; the claim must use `StructureFootprint`, which applies that clamp first.

2.6 **Recording is not asking.** `Claim()` means *"may I build here?"* and rejects on overlap. `Reserve()`
means *"something is already here"* and never rejects. Using the asking one for the saying job cost a site:
the shelters were pre-claimed through `Claim`, so two shelters 52 du apart (their own legal spacing) whose
square claim boxes happened to touch knocked each other out — the loser was never recorded, and the ground
under it was free. Caught by the audit on `luna/The Shadowed Rille`, 21.5 du where 31.3 was needed.

2.7 **The plan publishes real footprints.** `Plan.BuildingFootprints` exists so a guard can ask the ground
what it built instead of guessing a radius — a guess in a test is the same two-sources-of-truth bug wearing a
test's clothes. *(The first version of 2.4's guard failed on a correctly-placed building for exactly that
reason.)*

2.8 **No sealed pockets.** Measured as the largest *connected* unreachable region, not as raw standable
count. With a doorway, inside + outside are one region; a second region is a cavity.

## 3 · Air, and the shelters

3.1 **The tank is the tether.** `SuitAir` — a play budget of 1200 s against 8 fiction-hours plus a 30-minute
reserve. Breathing rate scales with exertion, **nerve and injury**: calm captains last longer.

> *"when I was scuba diving they said to keep calm so the O₂ does not run out... keep calm in face of danger
> so you don't choke."*

3.2 **The low-air warning is distance-gated,** never a bare percentage. It fires when the walk home costs
more than you have — a point of no return, not a fuel light.

3.3 **Every site carries shelters, and more than one or two.** ~1 per 9 000 du² of field, **never under
four**. On the current field that is nine, and all nine must actually place — if the count asks for nine and
the separation rule yields three, the ask and the ground have quietly disagreed, which is this project's most
expensive habit. *(Enforced: `ShelterBeaconsTellTheTruthTests`.)*

3.4 **A shelter is a refuge, not a tap.** Inside it, air is **not spent at all** — checked before the drain
and returning outright, so no ordering can suffocate a captain sitting in one.

3.5 **The rack always gives; the WAIT is the price.** Steady production, hard stop at **66 %** of a tank.
The refusal is characterisation, not balance: somebody set that regulator to leave something for the next
person. It also keeps the tube the anchor — you always work down from two thirds.

3.6 **A shelter reloads you completely, every time, unlimited.**

> *"I want ample reloads as one is dead without them on reever land"* / *"we want in practise unlimited
> reloads of rounds at the shelters not like couple mags"*

The scarcity lives in the **walk** and is paid in air. A refuge that haggles over ammunition is a chore.

3.7 **Nothing else crosses the threshold.** `SurfaceShelter.HoldAtTheThreshold` pushes any hostile back onto
the inner face. The door reads a suit; Old Ones and repo crews may crowd it and wait, and that waiting is a
better scene than being followed in. *(Enforced: `NothingFollowsYouInsideTests`, swept across angles — an
axis-aligned-only fix would pass a lazy test and still let a pack in through every angled door on the moon.)*

3.8 **A shelter's promise must be true on the map.** Every spec the tracker points at is built at that spot,
with walls, a door, a rack and a locker, and a centre that counts as inside.

## 4 · What is out there

4.1 **About half the ruins hold something.** Empty rooms are load-bearing: if every building paid out,
entering them would stop being a decision and become a chore performed on all of them.

4.2 **Papers are texture, never testimony.** A roster, a docket, a note in a locker. Nothing found on the
ground explains the Old Ones, and nothing ever will — see `reever-origin-canon`.

4.3 **Object persistence.** Dead Reevers stay where they were left; your caches stay buried where you buried
them. *(Owner: "the long walk should walk with expected object persistence.")*

## 5 · The instruments

5.1 **The motion tracker is motion-only.** A still contact is not a contact. This is a feature — it is what
makes a wall-blocked, momentarily-still Old One vanish from the fan.

5.2 **Beacons: home and shelters. Cache rings: your own buried chests, range-gated. Rumours: a wide soft
wash.** A tip narrows a search; it does not end one, and a dot would claim precision the information does not
have.

5.3 **The air bar is coloured by BAND, not fullness.**

## 6 · What the ship does NOT do down here

**The captain is in a suit on a moon. The hull is docked and empty somewhere above.**

6.1 **No ship voice.** The parrot, the alarm strip, the long-coast advert and the arrival-brake ask are all
skipped during an excursion — gated at the tick *and* inside `SquawkNow`, because `force: true` callers
bypass every other brake.

6.2 **No ship desks.** `SwitchDesk` refuses anything but Deck while `_surface` is live. It is documented as
the one place a desk switch happens, so number keys, the tab bar, chips and seat interactions are all covered
and nothing new can leak past by forgetting to ask.

6.3 **The way back is the shuttle.** Nothing else returns you to the ship. A console kind is a **verb** — do
not reuse `SurfaceAirlock` for a door that is not the ride home.

6.4 **No heat from the ground.** Heat is the cost of doing piracy; nobody watches a moon. Defending yourself
against Reevers earns none of it. *(The one carved-out exception, robbing a secret lab, is #582 and unbuilt.)*

6.5 **Wolves hold station while you are away.** They cannot reach or catch an empty hull.
`EncounterRule.HoldStation` carries their clock forward but not the chase, so coming back aboard resumes
where it stood instead of integrating the whole excursion in one burst.

> *"we don't want to be guarding our parking lot ... that is not good game play :-D"*

6.6 **Heat comes to the captain instead.** A repo boat follows a hot captain down, mid-mission, and sets down
between them and the tube. *"FBI does not arrest cars ... they look for the driver."*

## 7 · Dying

7.1 **A death card knows WHERE it happened.** `DeathPlace` — own ship, derelict, landing party — decides the
picture; the cause decides the words. `CanHappen(cause, place)` is the law, and every enum value is walked by
a guard.

7.2 **No borrowed prose.** The ship's collector lines are boarding volleys and last stands at the controls;
reading those over a captain walked down on regolith is the bug #574 was filed about.

7.3 **The red shirt.** A captain who died on the ground gets `death-landing-party.jpg`, whoever's hand it
was.

---

## 8 · Cost

8.1 **Collision is index-backed.** `SurfaceCollision.WallIndex` files the walls into a coarse grid, and
`Blocked` / `HasLineOfSight` / `Slide` all take the indexed path when the caller hands them one. This matters
now more than ever: a site carries **1 400–2 200** collision segments after the rebuild, against a few
hundred before.

8.2 **Seeded placement is pure, not free.** `SurfaceShelter.SpecsFor` re-runs the whole placement — up to
nine shelters over thirty hashed candidate spots each, with a separation check against everything placed so
far. Calling it twice a frame to draw beacons is fine; calling it **once per Old One per frame** (which the
threshold rule 3.7 briefly did) is twenty-four hunters × ~270 hash-and-lerp attempts, sixty times a second,
to answer a question that cannot change for the whole excursion.

> *"I think it felt a little sluggish at some points."*

**Rule: anything seeded and fixed-for-the-excursion is computed once and remembered on the excursion.**
Determinism is what makes that safe — same body, same salt, same field ⇒ same answer.

8.3 **Shared caches must be safe for every caller, not just the game.** `MoonSurface`'s layout cache was a
plain `Dictionary` — correct in single-threaded WASM, and a race the moment xUnit ran two audit classes in
parallel. It surfaced as a guard that **passed alone and failed in the full run**, which is worse than no
guard at all: a flaky audit teaches you to ignore audits. Now a `ConcurrentDictionary`; building a `Layout`
is deterministic, so a racing double-build is pure waste and never a wrong answer.

8.4 **Perf must not be measured from an MCP-driven tab.** Such a tab is `document.hidden`: rAF is throttled
and timers are clamped, so any number taken from one is worthless.

## Auditing the other places

`EverySiteMeetsTheSpecTests` walks **every landing site on every body** — the real
`MoonSurface.SurfaceDeck`, not the generator's inputs — and checks 1.2, 1.3, 2.1, 2.3, 2.4, 3.3, 3.8 and 6.3.

It **reports the whole table and fails once**, rather than dying on the first bad site: *"which sites fall
short"* is the actual question, and a guard that answers it one site at a time turns an audit into a queue of
surprises.

It earned its keep on the first run, finding a shelter/building overlap on a Luna site that the Core-level
guard had passed — see 2.6.

**The away grounds are audited too.** `ForExpedition`'s three — the henge, the crashed hull, the sealed tomb —
were exactly where Miranda was two days ago: walls and a landmark, nothing to walk into. They were missed for
the same reason canon site 0 was missed: **they are authored, so they bypass the generator where all the
improvements live.** That is the trap the test exists to spring, because it will happen again the next time
somebody hand-writes a ground.

> *"we should take these upgrades to all our outside scenes now. The biggest is the real spaces with
> doors... that is the place to find stuff. And clues."*

Each authored signature is untouched — the henge, the hull and the tomb are canon. The buildings go in the
empty flanks around them, through the same shared ledger, so they cannot grow into the signature, into each
other, or into a shelter.

## 9 · Away grounds and appended rooms

9.1 **A ground must be one ground.** `ForExpedition(kind, field)` takes **no** keep-out parameter. It briefly
did, handed in only by the routed path, and the standing guards killed it in a single run: the public overload
is called directly by tests, `ExpeditionRegions` and the labs, so two callers were building two different
grounds. The way out is that a **kind names its body** — an away rock's id is `ExpeditionSite.BodyIdFor(kind)`,
a pure function of the kind — so the ground looks up its own shelters and its own chamber without being told.
One function, one answer, no parameter to forget.

9.2 **Something that WILL be there is something that IS there.** Rooms appended at runtime must be reserved
before anything is placed:

- the away grounds' sealed rooms (`ExpeditionRegions.ForceOpen`) and their doors
- **the secret lab's chamber** (`SecretLab.ChamberFootprint`), reserved on *every* body whether or not that
  body hides one — the door spot is seeded the same way regardless, so it costs one building's worth of
  ground and removes the whole class before anybody goes looking for a lab

Without this a lab or a room opens into somebody else's wall, which the region guard reports as *"a region
wall crosses the base geography"*.

9.3 **An away gig ignores the site salt.** `ExpeditionSite_IgnoresSalt` is a standing law; keep-outs taken
with a real salt would move the ground and break it.

## Not yet enforced

Named so silence is not mistaken for coverage:

- **Reachability** of every building interior is audited by flood fill on some sites, not all.
- **Findability** of shelters at field scale is a judgement call, unmeasured.
- **Scenery vs structure legibility** — whether crater rings read as buildings — is unmeasured.
- **The landable-body list** in the audits is hand-kept and must match the scenario. It held eight when the
  scenario held ten — `enceladus` and `the-clinker` were simply forgotten, so four grounds were audited by
  nobody while the file claimed to check "every site". If a moon is added, add it there.
*(The monolith was on this list and is now §10.)*

## 10 · The monolith

> *"it is supposed to be impressive... now it looks like a box in closet."*

The canon slab was **2.4 × 5 du** — the captain is 1.4 across — at the heart of a field 310 × 260. The deep
commitment anchor of the whole site, the thing the long walk is *for*, was about two captains tall.

10.1 **The fix is not a bigger box.** The deck plan is a crude grid on purpose, and on a crude grid every
rectangle is a rectangle. What reads at this scale is **ceremony**: a slab wide enough to be a wall rather
than a crate (four captains, floor), a visibly **swept apron** in a field where everything else is rubble,
four approach stubs that are unmistakably *placed*, a picture when you put your hand on it, and things left
at its foot.

10.2 **The maze is untouched.** The slab stays inside the cell the canon rows leave it. Miranda's maze is the
one piece of ground nobody has asked to change.

10.3 **Things are left there, and they change.** `Monolith.AtTheFoot(body, salt, epoch)` — seeded on the site
*and* a slow visit-window. Roughly half of all windows are empty, which is load-bearing for the same reason
the empty ruins are: if there were always something, the walk would be a shopping trip.

10.4 **A window outlasts an excursion.** `EpochSeconds` must comfortably exceed a full tank, so the ground
never changes under a captain standing on it — the object-persistence law. The window is part of the deck
cache key, or the cache would serve a console saying something is there long after it is not.

10.5 **Every line is somebody ELSE's visit.** The stone never moves, hums, glows or responds, and the card
explains nothing. The Old Ones' origin is canon and is never confirmed by a card or a sensor; the monolith is
older than the question and does not answer it. *(Enforced: `TheMonolithIsAPlaceTests` greps the prose.)*

## 11 · Colour is a language

11.1 **Every world's stonework is drawn in its own material** (`BodyPalette`). You build out of what is under
your boots, so the ink is a fact about the body, not decoration — and it does the navigation for free: after
two visits the palette alone says which moon you are on.

> *"the in-situ construction materials of the walls might be planet specific ... red for mars etc theming"* ·
> *"gray for Moon"* · *"something to spot where we are visually"*

11.2 **No two worlds share an ink, and same-system neighbours are furthest apart** — Jupiter's three are the
comparison a player actually makes. *(Enforced.)*

11.3 **A door is the hill it is set in, only brighter** — so a building reads as one object.

11.4 **An imported door is a sentence.** Off-palette means somebody shipped materials across the system to
seal this, and nobody does that for a store cupboard. Rare in ruins (1 in 7); **always** on a shelter, which
is the truth about the building — nobody swages a pressure door out of regolith.

11.5 **The imported ink must be unmistakable on EVERY world.** It was a cold blue-white first and the guard
killed it in one run: 69 from Luna's grey, closer on Enceladus — an "unmistakable" signal that vanished on
precisely the two palest worlds. It is violet now, because no rock anywhere is violet. *(Enforced: minimum
contrast against every body's door ink.)*

## 12 · The field book

12.1 **A find that is shown once is a find that is lost.** Everything discovered on a surface goes through
one recorder and lands in a durable, capped, vault-persisted book. The pulse is the doorbell; the book is the
record. Same ruling as the bar's overheard log (#347).

12.2 **Grouped by PLACE in the ledger.** "Three papers and two caches" is an inventory; *"Miranda · The Ridge
Camp"* with four lines under it is a memory of an afternoon.

12.3 **Three pieces make a person.** One is litter, two is a coincidence. The payoff is not loot — it is
somebody still waiting for news, and sometimes what they know.

12.4 **A dossier never joins the dots.** It may show a continuity researcher shaking hands with a ministry
delegation. It may not explain. *(Enforced: the prose is grepped.)*

## 13 · The Hive — the ground under the ground

A clandestine underground facility under a landing site, reached by a camouflaged lift head on the surface
(#585). Every floor reuses the surface's own coordinate envelope, so depth costs no space; the shaft bands
are the only limit. It is generated by `UndergroundComplex` and walked through `HiveInterior`.

13.1 **Every room drawn on a floor can be walked to from the lift.** Not most of them, not on most floors —
all of them, on every floor of every clandestine site.
*(Enforced: `YouCanWalkTheHiveTests.EveryRoomOnEveryFloorCanBeWalkedToFromTheLift` floods ~130 floors with
A\* over the real `DeckPlan.CollisionField`, and `ADeepFloorIsAsWalkableAsAShallowOne` pins the same law at
the deepest floor the generator can be asked for.)*

13.2 **A wall builder that sweeps a line must be given that line in order.** Both spine faces and both rib
faces are built as segments with a deliberate gap at every mouth, by a cursor running along the face. The
cursor may only ever move forward, and the mouths must be **sorted** before it starts.

This is #587, and it is a new shape for the table at the top of this document — not two sources of truth,
but *one* source consumed in the wrong order. `ribXs` holds the ribs in ascending x and then appends the
lift alcove at the shaft's x, which sits left of the right-most rib. The sweep ran out past that rib,
met the alcove behind it, and emitted a segment from the cursor **backwards** to the alcove — one long wall
lying across everything in between, re-sealing both mouths it had just been asked to open. The plan was
right, the mouths were right, and the collision field was a wall. It cost an evening of playtest and was
only ever visible as a stranded-room list from the A\* audit.

The rule generalises past this file: **a list built by appending is not a list in order.** If a builder's
correctness depends on order, sort at the point of use.

13.3 **The lift is the hardest law down here.** A captain who cannot reach the car is trapped in a building
on a dead floor, and on an unpressurised floor that is a death. The lift console is audited as a target of
13.1 like any room, and `TheCaptainCanSTANDWhereTheLiftPutsThem` checks the doors do not open into wall.

13.4 **A locked door never seals a room you are told you can enter.** Locked doors are drawn *and* backed by
a wall — that is what makes them honest — so hanging one on an enterable room's only face would strand it
while the map went on offering it. *(Enforced: `NothingIsOBSTRUCTEDByTheDoorsThatWillNeverOpen`.)*

13.5 **A card opens exactly one class of thing: the next shaft band.** `Haul.Key` shipped saying *"Something
down here will open for this"* and opening nothing — an affordance you can see and cannot use, which is worse
than none. It now runs the shaft below the band it was found in, so depth past the first band is **earned by
working the floors you are standing on** rather than handed out by the seed. Three calls, each overrulable in
one line (#590):

- **The sealed `SECTOR n · 2.4 km` doors stay sealed.** The moment one of them can open, every one of them
  becomes a puzzle and the illusion of scale turns into a lock hunt. *(Enforced: the card prose never says
  SECTOR, and `LockedLine` never says card, authority, shaft, code or pass.)*
- **Never a code the player types.** You have the card or you do not. A keypad would be out of register with
  everything around it.
- **The refusal always says why**, and names what you *are* carrying if it is the wrong card. A gate that
  just sits there is indistinguishable from a bug — this ground has shipped that mistake before.

A card is a **possession**, so it rides in the vault (`AuthoritiesSection`), not on the excursion: found
eleven floors under a moon, still in the pocket a month and a world later. The save carries the id and
nothing else — the title is a seeded property of the world, rebuilt at read time, so a file can never go
stale against the words.

13.6 **The motion tracker knows it is underground.** It is a surface instrument — its reach, its readout and
its beacons were all written for standing on a moon under an open sky — and hundreds of metres down inside
poured walls it used to behave exactly the same. Now (#591):

- **The fan's reach degrades with depth**, on a curve rather than a table, because `DepthOf` is unbounded by
  design. Full reach on B1 — the floor that still holds pressure and is still pretending to work; the
  instrument tells the same lie the floor does, and the lie is what makes the dark below it land — then
  strictly decreasing, leaning on a floor fraction it never reaches. A dead instrument is not frightening,
  it is broken.
- **A contact behind a wall is a smudge, not a clean blip**, through the same fog `#371` built for wrecks.
- **The floor is on the instrument** (`B14 · ARCHIVE`), because how deep you are is the number that decides
  whether you get back up on the air you have, and you read the plan when you are thinking and the
  instrument when you are worried.

This gives depth a **third cost** after air and time, and it is the one a player can name — without adding
a single enemy. *(Enforced: `TheTrackerUndergroundTests` — unchanged on the regolith, full at B1, strictly
monotonic, never zero at any depth including past the performance guard, a curve not a table, deterministic,
and measurably quieter at the bottom.)*

**One reach, read by everyone.** The renderer used to derive the fan's range from the viewport while the sim
used a flat 32 du half-width, so on any window that was not exactly 64:28 the blip you *saw* at the rim was
not the blip the chirp had *heard*. That drift was harmless while both were "far"; it stops being harmless
the moment one of them shortens. The hud carries the number now.

13.7 **Nothing down here explains what the Old Ones are.** A facility may be enormous, expensive and
obviously state-backed, and may never say what it was for. *(Enforced: the prose is grepped.)*

## Working method

The one that actually found these: **boot every scene and look at it.** Nearly every bug above was invisible
to reasoning and to Core tests, and obvious on sight. When something is found by eye, the fix is not complete
until a guard walks the real object — otherwise it comes back the next time two things have to agree and only
one is changed.
