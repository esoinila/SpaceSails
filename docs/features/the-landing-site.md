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
| **A guard handed the wrong world, or a threshold that selects everything** | Three independent instances in one afternoon (2026-08-02) — see below |
| A sentence composed **before** the act it describes (#678) | The pickup line was printed, the room was struck off, and only then was the satchel asked — so a full pocket ate the find and had already claimed it. The same shape as *the sim vs the sentence*, with an ORDER at the bottom of it |
| An offset describing a building that had since been **rebuilt** (#681) | `?secretlab=…&land=1` set the captain down 7.5 du below the lift head's middle — a pace clear of the door while the head was a 10 × 8 box, and the far wall the moment #606 made it a rotated hut. *"I cannot move."* On 30 of the 34 site × cheat combinations in the sweep |
| A square the sim **places** you on that no ledger had ever claimed (#681) | Every other thing on the ground kept a claim; the captain's own landing spot had none, so a seeded hut was built straight through it on two sites |

### The fifth class: a green test that asserts nothing

On 2026-08-02 three people working in three different areas hit the same shape within hours of each other:

| where | what the guard did | what it actually proved |
| --- | --- | --- |
| `SurfaceLayoutTests.Env` | laid an invented 78 × 64 world, on which the shelters eat the whole field | **every body came out with zero buildings.** "No two bodies share a ground" was passing on eight nearly-empty fields |
| `MinRefugeDetourDu` (#608) | set to 34 du by eye | the nearest room this generator can produce is **34.2 du** out, measured over 808 floors. The threshold selected every room. The sabotage that puts the refuge in the closest room to the lift **passed** |
| the relic-room guard (#614) | ran against a 78 du-wide field | reported zero rooms on **every floor of every site**, listed and unlisted alike |

The common failure is not a wrong assertion — every one of these assertions was correct. It is that **the world
handed to the assertion could not distinguish pass from fail.** A guard is only evidence if the thing it forbids
would actually have tripped it.

So, added to the standing rules:

- **Hive and surface guards use `SurfaceLayout.DefaultField`.** Never a typed-in envelope. This is the same
  one-source law as everywhere else, and a test file is not exempt from it.
- **A threshold must be measured against what the generator can actually produce**, not chosen by eye. If the
  tightest real case sits at 34.2, a limit of 34 is a limit of zero.
- **`prove a guard can fail` catches all three**, which is why it is not optional. Two of the three were caught
  by doing it; the third was caught only because a *different* assertion in the same test happened to be strict
  enough to notice the empty world.
- **When you find a stale mirror, grep for its siblings before closing the ticket — and make the grep produce
  a CHANGE, not a sentence.** #573 fixed this exact duplicate in `SurfaceReachabilityTests` and wrote, in a
  comment directly above the fix, *"mirrors its constants, the same way `SurfaceLayoutTests.Env` does."* It
  identified the surviving copy, by name, in the file next door — and that copy still shipped for two months
  and was still laying an empty world when we found it.

  > **A comment that names a second source of truth is a TODO with no owner.**

  So the rule is not "leave a note for the next person". Either delete the sibling in the same PR, or open an
  issue for it before you close the one you are on. A note is what we already tried.

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

4.4 **The shovel takes the chest, and nothing but the chest.** The chest is a SNAPSHOT taken at the shuttle
door (`ShuttleExcursion.Pack`) — but the hold keeps living all the way down: dig an older cache back up on
this same ground and its units are aboard now, in no chest at all. Burying used to clear the whole hold, so
everything picked up since boarding was neither underground (the map card and the "off the books" line name
only the snapshot) nor aboard: it evaporated. Coin was always deducted honestly — the pending amount, never
the purse; `ShuttleExcursion.HoldAfterBurying` is cargo's half of the same law. *(Enforced:
`ShuttleExcursionTests.HoldAfterBurying_*`.)*

4.5 **The watch is part of the hoard, not a fact beside it.** Bury a chest and the game promises *"rivals may
dig it up over the coming days"*. That promise is a slow per-cache roll whose bookmark — the last whole day
resolved — used to live in a private client field and was therefore never saved. Reload, resume a voyage with
chests in the ground, and the watch came back unstarted: no rival ever dug anything up again, however many
days you flew. The bookmark now lives on `CacheLedger.LastCheckedPeriod` and rides the vault with the caches
it governs; a save older than the field reads back as *watch not started* and the client re-seeds it at the
clock the captain wakes at — never at day zero, which would resolve every day since the epoch in one pass and
empty the hoard on load. *(Enforced: `VaultMapperTests.Caches_RoundTrip_PreservesTheDiscoveryWatchBookmark`,
`Caches_OldVaultWithoutTheWatch_LoadsAsNotStarted`, `Caches_Clear_StopsTheWatch`,
`VaultSerializerTests.Caches_LegacyFileWithNoWatchField_ReadsAsWatchNotStarted`.)*

## 5 · The instruments

5.1 **The motion tracker is motion-only.** A still contact is not a contact. This is a feature — it is what
makes a wall-blocked, momentarily-still Old One vanish from the fan.

5.2 **Beacons: home and shelters. Cache rings: your own buried chests, range-gated. Rumours: a wide soft
wash.** A tip narrows a search; it does not end one, and a dot would claim precision the information does not
have.

5.3 **The air bar is coloured by BAND, not fullness.**

5.4 **The air row says WHERE the air is coming from, and there is exactly one predicate that decides it**
(#612). `SuitAir.SourceOf(floor, insideShelter, aboard)` → `TANKS` / `ROOM` / `SHIP`. The drain branches on
it, the gauge is handed its answer, and the plate by the lift asks it of the floor.

> *"Maybe we should have on our hud a AIR: Tanks / External symbol… now we don't see if we need to worry
> about O₂ from anywhere. That is really important info for the suit hud to tell us."*

- **A clock that is parked must not read like a clock that is running.** The gauge showed a countdown and
  never said whether it was counting. The readout changes *sentence*, not adjective: the reach advice
  ("N du further, then turn") is arithmetic about spending and is never quoted at somebody who is not
  spending. *(Enforced: `APARKEDClockNeverReadsLikeARunningOne`, swept across every band.)*
- **Symbol and colour before word.** A solid chip — dark letters on a block of colour — because a filled
  block is read pre-attentively and a word is not. Every source has its own glyph and its own word.
- **The bar and the chip answer different questions and both are true at once.** The bar: *can I still get
  home on this tank* — which stays a real question in a shelter, because you have to leave. The chip: *is it
  going down right now*.
- **One line on the crossing, never a state that repeats** — the tank starting or stopping, said once. A
  shelter is left to say it in its own voice; two lines for one threshold is the nag the tank mechanic was
  told not to become.

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

7.1 **A death card knows WHERE it happened.** `DeathPlace` — own ship, derelict, landing party, **underground**
(#609) — decides the picture; the cause decides the words. `CanHappen(cause, place)` is the law, and every
enum value is walked by a guard.

7.2 **No borrowed prose.** The ship's collector lines are boarding volleys and last stands at the controls;
reading those over a captain walked down on regolith is the bug #574 was filed about.

7.3 **The red shirt.** A captain who died on the ground gets `death-landing-party.jpg`, whoever's hand it
was.

7.4 **The picture is a sentence too** (#621). #574 wrote the law and then only wrote the words: for a year a
death aboard a derelict was shown `death-reevers.jpg` — boot prints in regolith, a chest, an Earth in the sky
— directly under its own line, *"No dust to leave a mark in — just a corridor."* And a suffocation aboard her
resolved to `death-suffocated.jpg`, **a file the game has never shipped.** Every place that can kill a captain
now has a card of its own, and a guard asserts each one is a file that exists in `wwwroot/art`, because a name
that resolves to nothing passes every string assertion ever written about it.

7.5 **Reaching a death on purpose** (#621). `?death=<cause>` stages the real pipeline at boot. The place is
never a parameter — the excursion decides it — so the cheat cannot be used to prove a card that the game
cannot actually stage. See `testing-guide.md` Appendix A.

7.6 **A dead hull is not her tube** (#621). The suit asked `MoonSurface.IsSafeAboard(y)` — "above the
regolith's rim at y = −20" — to decide whether the captain was breathing ship's air. A derelict's whole deck
runs −9 to +9, so the answer was YES everywhere aboard every wreck: the gauge read *"FILLING — you are on her
air"* inside a hull that has held vacuum for years, and the tank really did refill. It also made
`DeathCause.Suffocated` unreachable on a derelict, which is why the missing picture above was invisible.
`AwayTeamSide.BackAtTheShuttle` is now the one place that answers it, for both the air and the reach rule.

7.7 **And two more asked it after that** (#637). Occurrences 6 and 7 of the same pattern, found by walking
`?wreck=infested&land=1`:

- **A derelict cost no nerve.** `StepNerve`'s `onRegolith` was the moon's rule, so the ambient pressure the
  whole dread economy runs on never applied inside a hull. A captain could walk the spine of a haunted ship,
  in the dark, in vacuum, and the gauge scored it as standing in the shuttle bay. The damage half of that
  constant was fixed in #574 and the air half in #621; this was the sanity half, one call site over.
- **Comms never degraded aboard.** `CommsOnsetBias` returned its at-the-ship `0.5` at every point of every
  wreck, so the deep-in-a-dead-hull drop — the best place in the game for one — could never fire.

Both now go through `AwayTeamSide`, which gained `HowFarInside` (the same question asked with a number: Y from
the regolith's rim down to the monolith, X from the shuttle's lock aft to the transom) and `CommsOnsetBias`.
And because *a rule enforced on a function a caller is free not to use is not enforced*,
`ADerelictIsNotAMoonTests` reads the live client source and fails on any file outside `AwayTeamSide` that asks
`MoonSurface.IsSafeAboard` at all — the idiom `CssZBandSyncTests` and `TheDeathCardReadsTheNarrationSeamTests`
already use.

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

10.2 **It stands on PHOBOS, and there is exactly one of it (#649).** Owner's ruling, 2026-08-03
(`worldbuilding-notes.md` §8): *"There is ONE monolith. Not a class of object, not a kind of landmark a
generator can roll twice. If two grounds both call something 'the monolith', one of them is wrong and has to
be renamed — the word is reserved."*

Two grounds did. Every treasure map minted since #164 paces off *the monolith* on **Phobos**
(`Landmarks.PhobosMonolith` — the real 85 m boulder on the Stickney rim); the drawn slab stood on
**Miranda**, because that is where the first hand-built ground happened to put it. #648 unified the
*predicate* — everything asks `Monolith.StandsOn` — and could not fix the *fact* it answered with. The fact
is settled: `Monolith.BodyId` is `phobos`, read from `Landmarks.MonolithBodyId`, so the card in your pocket
and the thing on the horizon cannot name two moons.

Phobos's ground is **authored** now (`SurfaceLayout.MonolithScheme`, "THE STICKNEY RIM") and it is defined as
much by what it does not lay: **no maze, no corridor rows, no ruin field between you and it** — the ruling's
*"it must not sit in a fenced little plot… open enough that the object IS the horizon, not a prop in a
room."* Buildings are held clear of the signature by `AddOutlyingStructures` and end up in the flanks.

10.2b **Miranda keeps its maze and gives back the word.** The centre of the canon maze is `FalseSlab` — a
different *class* of object, not a second nameless ancient one: quarried, mortared, tool-marked and
weathering, everything the monolith's own card says it is not. Its geometry is byte-for-byte the numbers the
slab used to carry, so the ground the owner has never asked to change generates exactly the walls it
generated before; only the name, the card and the ceremony moved. Its card describes and does not account
for — it never names a builder, a purpose, or the other object.

10.2c **The ceremony belongs to the object.** The swept apron used to be drawn unconditionally, so every
landing site on every moon stood inside the monolith's ring of cleared ground — the borrowed-prose bug (#574)
in scenery instead of in a sentence, and invisible to every Core audit because scenery does not collide.
It is asked for by the thing it is swept around. *(Enforced: `TheCeremonyBelongsToTheObjectTests`.)*

10.3 **Things are left there, and they change.** `Monolith.AtTheFoot(body, salt, epoch)` — seeded on the site
*and* a slow visit-window. Roughly half of all windows are empty, which is load-bearing for the same reason
the empty ruins are: if there were always something, the walk would be a shopping trip.

10.4 **A window outlasts an excursion.** `EpochSeconds` must comfortably exceed a full tank, so the ground
never changes under a captain standing on it — the object-persistence law. The window is part of the deck
cache key, or the cache would serve a console saying something is there long after it is not.

10.4b **It is drawn at its canon size (#649).** Owner: *"The Phobos one's dimensions were huge and it
should not live in a boxed backyard but show more of its size and not having been built by us at least."*

The size has been in the game since #164 — `Landmarks.PhobosMonolith.HeightMeters` is **85 m** — and the
slab was drawn at six deck units. Four metres. So `SurfaceScale` now states what a deck unit is (0.7 m,
anchored on the captain's own 1.4 du width), and **every dimension of the monolith is derived from that one
canon number**: nothing about it is typed in, because a landmark whose canon size and drawn size differ by a
factor of twenty is bug class 1 with the wrongness baked in before the literal was written.

- **Proportions are 1 : 4 : 9** — the squares of the first three integers, identical in every unit system
  anyone could ever measure it in. That is doing real work as well as being an homage: a nine-to-one sheer
  plan with dead-parallel long faces is a shape no quarry cuts and no yard would, which is *"not built by
  us"* expressed as geometry rather than as a sentence. **The ratio is never stated anywhere in the game.**
- **The footprint is 54 × 13.5 du.** `DeckView` frames about 64 × 28, so the stone alone dominates the
  screen and its swept apron (86 du across) exceeds it — the ruling's *"the object IS the horizon, not a
  prop in a room"*, as something a guard can check.
- **The shadow is how a top-down plan says TALL.** At 18° of sun a 121 du object throws about 370 du of
  shade — longer than the walked field is deep. It runs up-field from the lit face to the landing band, so
  a captain steps off the pad into a lane of dark that runs off the bottom of the world, and the only way
  to find out what casts it is to walk down it. Nothing says so. Drawn as scenery: it does not collide.
- **It is drawn as one unbroken filled mass**, the only object on any moon that is. Every other solid is a
  hatched outline (the idiom for piled regolith); at this size that hatch is forty-nine parallel strokes
  across the one object whose own card says *"No seam."* The mass still hatches through for **collision**
  and none of it is **painted** — `SurfaceLayout.Wall.Unseen`, the same collide-but-never-draw distinction
  the field's own bound has always used.
- **Sight and arrival are both functions of the size.** First sight (the once-in-a-life nerve hit, 24, plus
  the FirstMonolith selfie) fires at 0.6 × its height, ~73 du, while it is still a shape. `ApproachLine` —
  which had existed since #586 with **no caller**, a designed-and-never-consumed failure — fires when you
  cross onto the swept ground. Two beats, two distances, both derived.
- **The object publishes the ground it occupies** (`Monolith.KeepOutOn`). Four placers used to hold the deep
  landmark at arm's length with a number each, every one sized for a six-du fixture; growing the stone under
  them would have seeded a pressure drum inside a wall.
- **The card meets you on the side you walk from.** It used to sit deep of the slab, which is harmless at
  six deck units and a fifty-du walk around solid rock at the real one.

10.4c **The site is a strange-things-happen place (#649).** Owner's ruling, in his own reference:
**Babylon 5** — Sheridan and the giants on the playground; *"background puppeteers watching if their kids
perform in the school play."* Awesome and a little scary; **parental, not predatory**.

`MonolithWatch`. Three gates, all of them Core's:

- **Place.** `Monolith.StandsOn`, and inside the stone's sight. This ground and no other — that is what
  makes it a property of the *place*.
- **Window.** About one visit-window in three is attentive, seeded on the same slow clock the foot-offerings
  use, so it holds still for a whole excursion and is the same on a revisit inside the window. Most walks
  out here are a long walk to a stone, which is what makes the other ones mean anything.
- **Dwell.** Forty seconds inside its sight, and at most once per excursion. Walking out of sight resets it.
  Nothing is watching to see you *arrive*; it is watching to see whether you **stand there**.

Six variants, and every one of them is a fact about the **world** rather than a thing that could be met:
the shadows disagree with the slab's for a beat; your own bootprints are ahead of you; every Old One on the
field stops at once and faces the stone; a tide crosses the dust on a moon with nothing to pull it; the
tracker paints one contact too many that never moves; the light drops a third with nothing crossing the sun.

**It costs nothing** (`MonolithWatch.NerveCost`, a flagged feel call). The place is already priced at 24 —
the biggest single fright in a captain's life, once ever — and a site that also bills you for standing in
it is a site you learn to avoid, whatever the prose says. The world noticing you and then not hurting you is
the more unsettling reading, and it is the parental one.

**Not a card and not a plate**, which is the hardest call in it. The picture idiom (#528) is right for
almost everything and wrong here: a frame around a thing says THIS IS A THING, and one canvas across six
variants becomes the picture a player learns to read as *that again* — confirmation by repetition. The
nearest thing this ground already has, what somebody left at the foot of the stone, is text and it works.

*(Enforced: `NothingOutHereEverSaysWhatItWasTests` — the same law as
`TheHiveTests.NothingDownHereEXPLAINSAnything`.)* Cheat: `?watchers=1`.

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

12.5 **The book is readable on foot, standing where it was written** (#690). Owner, mid-run, designing the
paper-shedding loop: *"should we have notes / clues section in our inventory ui?"* — and, on the register the
tab is written in: *"it's like our detective notepad :-D"*.

The book rendered in the Captain's ledger and nowhere else — a ship-brain surface, reached by flying home. On
foot at a sealed door the captain could not consult their own notes. §13.9's leave verb turned that from an
inconvenience into a cost: leaving a paper **files its gist to the book**, so knowledge was being deliberately
moved into a place unreachable from the ground it came off. Record the essential data, throw out the paper, and
be able to read the record standing in the dark.

The satchel has a second page: **🎒 CARRIED | 📓 NOTES**. A pocket and a notebook are both things a satchel
holds, so it is the satchel's own frame and not a second modal stacked on the door pop-up.

- **One store, and it is the book.** The page reads `_fieldNotes` through the ledger's own Core projections
  (`FieldNotes.PerPlace`, and `FieldNotes.Here` for one ground). 12.1's law is what makes the book worth
  reading, and a tab keeping its own copy — *"the notes this excursion filed"* — is the second store this repo
  has already paid for once. *(Enforced by grep: exactly one collection of `FieldNote` exists in the whole
  client, and it is `_fieldNotes`.)*
- **This ground first, every ground one tap deeper.** At a door the captain wants what THIS building has told
  them, not the memoirs. The filter names the ground through `FieldNotes.PlaceLabel` — the same function that
  *wrote* those labels — and never re-derives the format, or the tab would match today and drift the first
  time a place is renamed.
- **Read-only.** The satchel holds the book open; it does not hold a pen. Capping, de-duplication and
  persistence are the book's own laws (12.1), and a second set of rules for the same pages is how they stop
  being true.
- **Every open lands on CARRIED, on this ground.** The pocket is the primary tool — [I] is pressed by reflex to
  see what you are holding, and a dialog that remembered you were last reading notes would answer a question
  nobody asked. Opening **at a door** lands on CARRIED too, with the offer flow exactly as §13.9 left it; the
  notebook is one tap away, which is the entire point of it being there.
- **Everything it says renders inside the dialog's own layer** (#680/#686), from birth rather than after a
  playtest.

**The register is a casebook, not a quest log** — and the empty states are where a tab like this either holds
its voice or turns into a checklist. Standing somewhere the book has not been opened: *"Nothing filed for this
ground. Either you have not searched it, or it had nothing to say — the book does not know which, and neither
do you."* With no notes anywhere: *"The book is blank. It fills the way everything out here fills — one room at
a time, and only the ones you turn over."* And the this-ground header, which says what the page is and declines
to be more: *"— what was found here, latest first. The book keeps it. It keeps no opinion about it."* Places,
facts, and the space between them left to the captain: 12.4 one surface further down.

*(Enforced: `TheBookOpensWhereYouAreStandingTests` in Core — `Here` agrees with `PerPlace` ground for ground,
orders newest-first, and matches a place as the book WROTE it rather than nearly. `TheNotebookIsInTheSatchelTests`
in the client for the six source shapes: the second page reading the one book, the book read-only from the
dialog, the one-store grep, the label the filter is built from, both openers landing on the pocket, and the
in-dialog saying. Watched go RED against the #688 build, 5 of 6. The sixth is the one-store grep, which pins a
law that was already true — it was watched go red against a second `List<Core.FieldNote>` transcribed into
`Map.Surface.cs`. Core went 2 of 4 against a `Here` that read the log itself in written order; the other two
describe brand-new API and could not be red, which is said here rather than dressed up.)*

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
on a dead floor, and on an unpressurised floor that is a death. The lift console is audited as a
target of 13.1 like any room, and `TheCaptainCanSTANDWhereTheLiftPutsThem` checks the doors do not open into wall.

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
  just sits there is indistinguishable from a bug — this ground has shipped that mistake before. Since #679 it
  also says *which kind of wrong*: another shaft of this site, or another site entirely, each named (§13.10).

**And the ACCEPTANCE says why too, in both directions in time** (#689). Owner, having played the whole loop
on a deep site — found the card, fed the gate, rode past the floor the building admits to: *"It was locked
until I got it ... there was no story point about it being needed or used. Let's tell that story somehow more
clearly that it was used in the elevator."* Both halves were written and neither was legible, so by his
ruling:

- **Before the ride, the row names the card.** With the right paper in the wallet the gated row stops saying
  `🔒 sealed` and says `🎫 opens for you`, with the card's own title under it and *the gate will read it*.
  The positive twin of the refusal, decided in Core (`LiftStop.OpenedBy`) so the panel can never promise a
  reading the gate will not give. Never at the head office, which has no gate to promise anything (#411).
- **After it, the gate answers on ARRIVAL.** The accepted line used to be said on the frame the panel closes
  and the floor is torn down and rebuilt — the one instant in the loop when nobody is reading the HUD, which
  is why the owner never saw it. It is now said when the doors open on the new floor, and said **last** of
  the arrival's lines, because the pulse has one slot and the routine air line was eating it.
- **Which gate a ride crosses is a fact about the STOP**, not about the floor the press came from
  (`GateOpenedByRidingTo` asks the panel rather than doing arithmetic on the captain's own floor). The old
  derivation never looked at a card at all, and so had the head office — whose gate is deliberately absent —
  narrating a countersignature nobody was carrying.

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

13.7 **A rare site has a band nobody listed** (#592). Owner: *"we could even have a secret lab lab :-D"*.
Everything above it is a real, expensive, thoroughly documented clandestine operation; underneath *that* is
the thing the clandestine operation was hiding from its own staff.

The whole feature lives in the gap between two numbers, and every caller has to know which one it is asking
for:

| ask | function |
| --- | --- |
| what the building says about itself — the lift panel, the directory | `DepthOf` |
| how far a captain can actually walk — audits, renderers, labs, the cars | `TrueDepthOf` |
| which floors exist at all | `FloorsOf` |

- **It is a whole BAND on its own shaft**, the next one below the band the listed bottom falls in — not "four
  floors below the listed bottom", which sounds the same and is not. Bands are fixed slices counted from the
  surface because that is what a shaft *is*; a hidden band starting at an arbitrary depth would share a car
  with the floors above it and the secret would be reachable by pressing DOWN. Where the listed depth stops
  mid-band there is a **gap** under it with nothing dug in it — hence `FloorsOf`, because "−1 down to the
  depth" is no longer the shape of a building.
- **Its `Kind` always differs from the floors above.** A records annex whose bottom is a clinic tells you
  what the records were *of* with no narration at all. A hidden clinic under a clinic is a bigger clinic.
- **Nothing above it announces it.** On the last listed floor the panel behaves exactly as it does at the
  true bottom of an ordinary site: silence, and the car goes up. Not the #590 refusal (it names a shaft), not
  `EndOfTheLineLine` (it promises one is down there somewhere). The button really is not there. The way down
  is a card somebody left in a room — a piece of paper telling the truth about a building that is not.
- **It pays in information, not a bigger number.** A third of its rooms hold a file on somebody, and it pays
  *worse* in hardware than the floors above. If it paid in kit it would be a loot room with a story painted
  on it.
- **Canon holds hardest here.** It is the most tempting place in the game to explain the Old Ones. It does
  not.

*(Enforced: `TheUnlistedBandTests` — rare and seeded, never under a shallow site, invisible where absent, a
whole band on its own shaft, nothing dug in the gap, always a different Kind (checked over 400 generated
sites), its own door vocabulary, no department on the plate, the #590 card exists for it and nothing past it,
the haul weighting measured over 600 rooms rather than one floor's dice, the canon grep, and every unlisted
floor held to the same facility standard as a listed one. The client A\* audit walks them for real.)*

13.8 **Nothing down here explains what the Old Ones are.** A facility may be enormous, expensive and
obviously state-backed, and may never say what it was for. *(Enforced: the prose is grepped.)*

13.9 **What you picked up is in your pocket, and the pocket says so.**

Three faults found in one playtest, all of them the same fault wearing three coats: *the satchel did not tell
the truth about itself.*

| the captain saw | what was actually wrong |
| --- | --- |
| *"now I used e to search and then checked inventory on many rooms"* | the haul line described the ROOM and stopped. Some hauls are things you carry and some are not, and the only way to learn which was to open the satchel every time — the game asking the player to audit it |
| *"the operational papers … look identical in inventory"* | six rows reading `operational paper` is not an inventory, it is a counter. You cannot tell which one you read, which floor it came off, or whether the seventh pickup got you anything new |
| *"I picked authority card but it did not go to inventory"* | on the bottom band the card evaporated |

The third one is the one worth writing down, because it is **the third named bug class again**: the sim did
one thing and the sentence said another. `CardInRoom` returns null at the bottom band and is *right* to — a
card for a hole nobody dug would be a lie — but the client then granted a lead and put nothing in the pocket
while `KeyLine` went on describing a countersigned card in the captain's hand.

The rule that fixes it is not a special case:

> **A card is an object. You picked it up, so you have it.**

When the shaft it runs is not in this building, it is a card for **another one** — which is exactly the wallet
`WrongCardLine` has always described (*"every one of them countersigned, current, and for another shaft"*).
Until now that line described a thing the game could not give you. Now the deepest floor of one facility hands
you the way into the next, which is the best thing a bottom floor could hold, and the prose that was decoration
became literally true without a word of it changing.

Consequences worth keeping:

- **A pickup announcement names what went in and what did not.** Equipment is crated and sold; it does not fit
  a pocket, and saying so is the whole of the distinction.
- **Every paper has its own short title**, taken from the *same roll* as its text — one source, consumed once.
  Rolling twice is the cheap cousin of this repo's fourth named bug class and would hand a captain a *pay
  sheet* that opens as a *shipping manifest*. *(Enforced: `ThePaperInYourPocketIsThePaperYouOpen`, verified
  RED against a separately-seeded title.)*
- **A title is what the paperwork calls itself.** No title may contain *lead*, *clue*, *site*, *facility* or
  *secret*: the whole ladder rests on the captain deciding a document is worth something, and a form that
  announced its own importance would make that decision for them.

*(Enforced: `TheSatchelTests` for the titles and the pairing, `TheAuthorityCardTests` for the invariant the
pocket rests on — every site has a band 0, so a Key found at the bottom of one always has somewhere to point,
and a Key room issues a card exactly when there is a shaft below it and never otherwise.)*

**And the pocket never lies about itself** (#678). The rule above — *a card is an object, you picked it up, so
you have it* — held for the card and was never enforced for the SENTENCE. A live playtest four days later found
both of its residual halves in one afternoon, and they are the same fault:

| the captain saw | what was actually wrong |
| --- | --- |
| *"picked up an identity card"*, then a satchel with two papers and no card | on the bottom band the client's far-site fallback can come up empty, and `KeyLine` narrated *"an authority card, countersigned twice and still active"* anyway. Nothing was minted. The room was consumed |
| nothing at all — the worst version | at the cap `Satchel.Add` refuses (twelve of anything, back then — see the compartments below), and the *"Into your pocket"* line was composed and shown **before** it ran. A full pocket ate the find and had already claimed it |

> **A pickup line may only be printed for something that actually went in. What the pocket cannot take is not
> consumed — the find stays in the room, and searching it again offers it again.**

The second sentence is the owner's, near enough verbatim: *"If refused the item should stay where it was
investigated last — not disappear like they do now, or seem to."* It is the enforcement side of #615 (leave
must not destroy), and it is why the room key is now struck off **after** the pickup resolves rather than by
the act of looking.

- **The composition moved to Core.** `UndergroundComplex.WhatGoesInThePocket` answers all three questions in
  one call — what goes in, what is said, whether the room is emptied at all — because the bug was never in any
  one of those answers, it was in the ORDER four scattered client statements produced them. The client has one
  `Satchel.Add` now, and it adds the thing the sentence was written about.
- **`Satchel.CanTake` is asked before the room is turned over**, and it is not `!IsFull`: something already in
  the pocket merges into the row that is there, so a full satchel still takes six more of a round you carry.
- **A Key room that minted nothing describes no card.** It pays as an ordinary room does — a counterfoil book,
  a punch, a lanyard with an empty window — and never once says the captain is holding an authority.

*(Enforced: `ThePocketNeverLiesTests` — every haul × every card shape (own site / another site / none) × an
empty pocket and a full one, over every floor of ten real sites, with the sweep asserting it actually covered a
bottom band, an unlisted band and a middle band. The claim is checked against the **real satchel**: a line that
mentions the pocket must survive `Satchel.Add` actually taking the item, because the broken build was perfectly
self-consistent — it announced the card, handed it to a satchel that refused it, and struck the room off
anyway. Watched go RED against today's behaviour transcribed back in: 5 of 6 red.)*

**And the satchel has compartments, because a card is flat** (#688). Owner, live on the deep site, laughing:
*"Oh I run out of space in the inventory. How do I get Bigger pockets ... lol... I find the good keycard but my
pockets are full and I can not pocket it. Lol, love it. Lets fix it. :-D"* — and, seconds later, *"I think
bigger pockets for little papers."*

One number governed everything a captain could carry, and it was a number chosen for **bulk**. Twelve is right
for rounds, crates and relic paperwork, and it was quietly deciding that the best find in the game could be
refused because you were already holding eleven shipping manifests.

| compartment | what rides in it | how much |
| --- | --- | --- |
| **the wallet** | `Authority` | never fills. A card is flat, and it is never refused |
| **the paper sleeve** | `Paper`, `Dirt` — both are paper in the hand | 24 |
| **the pockets** | `Rounds`, `Relic`, and any kind appended later | 12, and it keeps every one of its teeth |

Nothing here loosens what #603 built. It is still a pocket and not a warehouse, what is in it is still legible
at a glance, and the pressure that makes *leave it here* a real decision still sits on the things that actually
take up room. What changed is that the pressure stopped falling on the two objects in the game that weigh
nothing — and the subtitle stopped lying: **the space-left line names what it counts**, because one figure
standing for three compartments is the third named bug class in a subtitle.

**Doors suggest keys, not paperwork** (#688). Owner: *"Let's make a bigger story point about finding any kind
of key or keycard and only suggest those at doors. Or tools, but not just like some papers."* With the satchel
open at a sealed way, a room door or a shaft gate, only an authority carries a live **try it →**; papers, files,
relic notes and rounds render inert there and keep their 🔍 lens. **Not one refusal changed** — the wrong-shaft
and wrong-site readings (§13.10, #679) are the best storytelling the Hive has, and they are still earned by
holding up a card that turns out to be for somewhere else. What stopped was the game dangling forty live offers
at a bulkhead to hide the one that mattered. *Rounds stay inert at a door deliberately: shooting a lock open
(#610) is an owner call nobody has made, and an offerable round there would pre-wire the answer.*

**And there is a way to put a thing down** (#688). Owner: *"The keycard story is already big, but no way to drop
stuff."* The satchel had a verb for offering a thing and a verb for looking at one and none at all for letting
go, while the game's own prose kept saying something had to be *read, spent or left behind*.

> **Leaving never destroys** (#615). What the captain sets down is a find again, lying on the square they are
> standing on, and pressing [E] there hands it straight back — what will not fit stays on the ground rather
> than evaporating on the way to it.

- **Its own small control per row**, never a mode — #614's reason one size down: making room must never be one
  mis-click away from offering a relic to a bulkhead.
- **The satchel stays open** (you put a thing down in order to pick a thing up), so the confirmation is said
  **inside the dialog** and never pulsed under its own backdrop (#680/#686).
- **A document leaves its gist behind in the book.** Leaving a `Paper` or `Dirt` files one field note — its
  title, its certainty, what it said — before the sheet leaves the pocket. A captain does not abandon a pay
  sheet without having read it; what they are discarding is the *paper*, and the paper was only ever costing
  them bulk. Rounds and relic notes file nothing: there is no gist to ammunition, and a book entry for one
  would be a receipt.
- **[E] answers your feet before it answers the walls**, ahead of the console dispatch — otherwise a thing set
  down while standing on a room console could never be reached.
- **v1 is excursion-scoped, and the line says so out loud**: *"Lift off this rock without it and it stays here
  with everything else this place kept."* The world does not keep a ledger of every sheet anybody ever set on a
  floor. Hardening it into the vault is an owner call, not something to sneak in under a feel pass.

**And the I key shuts the pocket it opens** (#688). Owner: *"If I press I when inventory is open, let's close it
then."* One line of feel, and the kind that is invisible until you are in a corridor with a pack coming and the
pocket you opened by reflex will not go away by the same reflex.

**And the satchel holds the notebook too** (#690, §12.5). Owner: *"should we have notes / clues section in our
inventory ui?"* — *"it's like our detective notepad :-D"*. A second page, **🎒 CARRIED | 📓 NOTES**, reading the
one field book and defaulting to the ground underfoot. It is the direct consequence of the gist-filing above:
leaving a paper moved what it said into a surface only the ship could reach. Now the captain can stand at a
sealed door, read what this building has already told them, and decide whether the card in their hand is worth
offering. The satchel still opens on CARRIED, at a door and everywhere else — the pocket is the primary tool
and the notebook is one tap away.

**And the wallet is one thing, so it comes out all at once** (#697). Owner: *"Let's also add option to try all
ID cards ... by grouping them into a folder in the inventory."* — and, on the register the answer had to be
written in: *"It is a little throw at the movie ... where he had this wallet with zillion different
contradictory IDs :-D"*

The comedy was already native and nobody had staged it. Every card down here is countersigned, current, and
issued by an office with no standing, and a captain who has worked three sites carries several that disagree
about who they work for. What was missing was the **gesture**: the wallet came out one card at a time, so four
authorities meant four presses producing four sentences, of which one was worth reading.

On the CARRIED page every `Authority` now collapses into one row — **🎫 THE WALLET (3)** — folded shut on every
open, one tap from the card rows exactly as they were, each keeping its own try and its own 🔍 lens (the faces
are the best objects in the game, §13.10). *A folder of one is bureaucracy about bureaucracy, so one card is a
card.* At a door the folder row carries the offer, as its own control beside the toggle — #614's ruling one size
up: opening a wallet must not be a mis-click away from offering everything in it to a bulkhead. One press fans
every card and the answer is **one line**:

- **A card works → the outcome IS that card's outcome**, and the fan ends through exactly the resolution a
  single successful try ends through. Not a copy of that ending — the same one. The no-double-effects claim is
  then structural rather than a promise: the two consuming branches can only fire for a paper or a handful of
  rounds, and neither is ever in a wallet.
- **Nothing works → the ladder decides** (#683). Another shaft of *this* site beats another site, which beats a
  card this build cannot even read. The most informative refusal is said **once**, instead of three shuffles,
  and it names the nearest miss because #679's sentence already named it.
- **A door with no reader answers once.** Fanning six authorities at a sealed way prints one honest sentence,
  not six, and it names no card, no shaft and no site — #590 call 2 is not something a new control gets to
  renegotiate. What it *may* do is notice the stack: *"You go through the wallet a card at a time — all six,
  every one countersigned, every one current, and no two of them agreeing about who you work for."* A reader
  works through several mutually incompatible authorities without comment, and the game never explains the joke.

The fold is Core's (`SatchelTry.OfferWallet`), pure and deterministic like everything else there, so the dialog
cannot grow a second ladder to drift the day #683's order is refined. **Precedent:** the lift panel has read the
whole wallet unprompted since #689 (`LiftPanel(..., AuthorityCardIds())`) — this brings the on-foot TRY to the
same standard, and the single card stays for deliberate captains.

*(Enforced: `TheWalletIsOneThingTests` — the working card wins from every position in the wallet and answers
with its own sentence; the ladder's best refusal is the one surfaced, from **every permutation** of the same
three cards, because what a refusal teaches must not be a fact about which pocket a card fell into; a wallet of
one answers exactly as that one card does, at every target; the sealed way's body appears exactly **once**
however thick the wallet, and never matches any single card's line; an empty wallet is answered, and at the two
FINAL doors it is answered in the door's own words so it cannot hint. Watched go RED against a naive
first-refusal fold transcribed into Core: **4 of 7**. `TheWalletFansAtTheDoorTests` for the five client shapes —
the folder row, Core's own grouping behind it, the folded-on-open reset, the press routing through `OfferWallet`
with no loop of its own, and the shared ending the fan is forbidden from copying. RED against the build that
shipped #690: **5 of 5**.)*

*(Enforced: `ThePocketsAreThreeCompartmentsTests` — the wallet never fills, the sleeve holds 24 and refuses the
25th, the bulky twelve keeps its teeth, neither compartment crowds the other, the merge law survives the
restructure for every kind, and the space-left line is identical for a satchel with six extra cards in it.
Watched go RED against the single-cap satchel, 6 of 6. `WhatYouLeaveIsStillThereTests` for the drop verb, the
door filter and the gist — new behaviour, so honestly NOT red-provable; what they pin is that no refusal was
deleted along with its offer. `TheSatchelFeelPassTests` for the four client shapes — the I toggle, the door
filter the client cannot route around, the subtitle that no longer subtracts one count from one capacity, and
the leave control whose confirmation stays inside the dialog. Watched go RED against this morning's build, 5 of
5.)*

13.10 **Some things you carry are worth looking at, and a card describes the LOCK, never the DOOR.**

Owner: *"we could have gen-AI images of plotwise important items… maybe they say something about what door
they open."*

The second half is the whole design problem. "Says what door it opens" is the tempting reading and it is a
**quest marker**: an item that names its lock does the captain's thinking, and this facility is built on the
opposite law. So the rule is:

> A card may say **which shaft**, of **which site**, and **whether it is this building**.
> It may never say **where on that ground the way in is**, or anything a tracker could act on.

**The site half of that line was redrawn by the owner in #679**, and it used to read *"it may never say where
that building is"* — the card named a shaft and an office and nothing else. Owner, holding several: *"a captain
holding three cards from three moons sees three identical shapes and cannot plan a wallet."* He is right, and
the old rule was defending the wrong thing. A pass has always had its holder's place of work printed on it; what
turns an object into an objective is not a NAME, it is a **fix** — a bearing, a distance, a mark on the
instrument. So the card face carries a site designation in the office's own register
(`🎫 SHAFT 2 · OFFICE OF WORKS · SUB-REGISTRY · MIRANDA SITE`), the look-card says the same thing in the same
words, and neither of them says a syllable about where on that ground the head stands. Finding it is still the
game.

That is the same discipline `SealedWayCard` already keeps — say what it is, never what to do about it. And it
is what makes #613's foreign card pay: a captain holding a live authority for a shaft they have not found is
holding a reason to keep flying, not a waypoint.

**A refusal sorts the wallet too** (#679). `SatchelTry.Offer` has answered every try with a reason since #603,
but the gate gave one sentence to every wrong card there is — so the second card a captain tried taught them
nothing the first had not, and TRY stopped being a verb. There are four answers now and they are
distinguishable **from the Line text alone**: the card works; it runs another shaft *of this site*, named; it
was issued for another site, named; or the way is sealed and nothing you could ever carry opens it. The last of
those is unchanged on purpose — #590 call 2 — and so is the mechanical room door: a refusal that hinted would
send a captain looking for a card that does not exist.

| gets a card | does not |
| --- | --- |
| an authority card (which shaft, which site; and *"not this one"* when it is foreign) | operational paper — it has its own reader (#603) |
| the two-stage penetrator | issue ball — it is the round you always have |
| the thing on the pallet | a file on somebody — leverage, not a display piece |

A game where every object earns a full-screen card has no objects that matter.

**The thing on the pallet.** Owner: *"kind of horror theme in a Lovecraft way … like finding a massive collar
designed for Cthulhu's neck :D"*. One per facility, only in the band nobody listed, on its deepest floor —
and it is **designated, not rolled**, for the same reason the way-down card is (#592): a seeded one-in-N
object is an object that is silently absent on some worlds *forever*, with every test still green.

Everything frightening about it is arithmetic. The pallet is the only thing in the art that gives its scale.
There is no creature, no bones, no log, no note. What the captain takes away is a **measurement** — you
cannot lift it, and a satchel claiming to contain a three-metre alloy band would be the third named bug class
all over again. And canon holds hardest exactly here, because this is the most tempting object in the game to
explain the Old Ones with. It does not.

*(Enforced: `TheThingsWorthLookingAtTests` — the shaft **and the site** are named and no nav fix ever is (this
assertion was inverted by #679: it used to forbid the body id outright, and it was verified RED at the time
against a card that appended one — the change is the owner's, recorded above, and the surviving half is the
half that was ever load-bearing), most carried things get no card, exactly one relic per facility on a
designated real room (verified RED against a rolled placement), the kind was APPENDED so old vaults still
read, and the canon grep covers every new string. `TheCardCarriesItsOwnStoryTests` holds the new law: every
card names its site, no two sites print the same row, the satchel row reads `CardTitle` rather than a hand-made
copy of it, and the four gate answers are pairwise distinguishable — verified RED on the build before it, where
the wrong shaft and the wrong moon were answered with the same sentence.)*

**A note on the harness.** The relic-room guard first failed against an invented 78 du-wide field, reporting
zero rooms on *every* floor of *every* site — listed and unlisted alike. A guard that fails on a field the
game never generates is not evidence of a bug, it is evidence of a bad harness. Hive tests use
`SurfaceLayout.DefaultField`. This is the same lesson the client A\* audit taught: *a test is only as honest
as the world you hand it.*

13.11 **The first-ground card teaches the game we are actually shipping.**

Owner: *"the E key does a lot more now"* and *"also going to ground is more than burying chest now."*

`E` meant DIG when a surface was regolith and somewhere to leave a chest. It is now the one key that touches
anything at all — a door, a console, a shelter's pump, a lift panel, a room worth turning over — and a card
still titling it *Dig* was teaching a new captain to walk past every building on the moon. The head line led
with caching for the same historical reason; what is true of every square metre of a surface, and is the clock
every other choice runs against, is the **air**.

*(Enforced: `GroundLessonTests` — the `E` lever names door, console, lift and room and never says "dig" in its
title; the head mentions air; the shelter is on the card at all; and the `I` lever is written for the empty
pockets a first landing actually has.)*

13.12 **Every airless floor carries at least one pressure refuge** (#608). Owner, after suffocating on B2 and
then ruling on it: *"there should be like at least one air replenish station in each of the airless labs
underground… for pure safety"*. **Each** — not most, not a rare one. It is a safety regulation in-world and a
law in code.

The reason is his too, and it is better than the mechanic it costs. He also ruled on why any floor down here
holds pressure at all: *"it is very difficult to work in the suit. So all work would happen out of it"* —
*"writing with a pen … reading documents … any kind of fine motor skill stuff"* does not happen in vacuum. So
an airless floor is **not an abandoned floor**: it is a floor of **suit-work** — storage, hauling, plant, hard
vacuum process — staffed all day by people in suits, and a building that staffs one and gives them nowhere to
go is one busy lift away from killing somebody. *"otherwise the elevator being busy could kill employees, and
those honest criminal scientists are hard to recruit :-D"*

- **It is one of the floor's own rooms**, carved out of the room list after the ribs are laid — three poured
  walls and a doorway, so it is walkable from the lift by the same law as every other room (13.1) rather than
  by a second placement that would have to be kept reachable separately. It stops being a haul room when it
  becomes one.
- **It is never on the way.** `MinRefugeDetourDu` = 70 du from the shaft, which is **twice** the nearest room
  the generator can produce. The first cut of this constant was 34 and was worthless — the closest room over
  808 dead floors is 34.2 du out, so the rule selected every room and its guard passed on a build rigged to
  put the refuge in the nearest box there is. **A threshold that nothing can violate is not a threshold**;
  measure the distribution before choosing the number.
- **It does not cancel #585.** Depth is still paid for in air: one room, a walk away, and its rack is the
  *surface* rack — `SurfaceShelter.Produce`/`Transfer` and the two-thirds ceiling somebody set on purpose for
  the next person through the door. Refuges buy **range**, never independence, exactly as shelters do. One
  rack law, two buildings: `Map.Surface.DrawFromRack` is the only place either of them moves air.
- **The tracker paints the refuges underground, and never the surface shelters.** Owner: *"those need to show
  in the motion detector, not the surface ones, when you are 150 meters below surface."* A shelter ring on B7
  would be the map lying in its most expensive form — a ring a captain would spend the last of a tank walking
  toward. This is also how #608's hardest requirement is met without a map or a tutorial: *a refuge you
  discover after you needed it is a cruelty*, so the instrument the captain already watches has it on it.
- **The dead-air card no longer says "there are no shelters down here".** It said so honestly when it was
  written (#609) and it would now be the most dangerous sentence in the game.

*(Enforced: `TheRefugesUndergroundTests` — one on every airless floor and none on a pressurised one, over
1 100 floors of 100 sites; never beside the lift; never also a room to search; never emptying a floor or
taking the room the way-down card is designated to; deterministic; one containment law; the canon grep.
`YouCanWalkTheHiveTests.EveryAirlessFloorHasARefugeYouCanWALKToFromTheLift` A\*s from the lift car to the
refuge on every airless floor of every site — **verifying both endpoints are standable first**, because #600
is the standing lesson that a reachability test is only as honest as its endpoints — and
`TheRefugeIsAWalkFromTheLiftAndNotAStepFromIt` measures the detour over the real corridors rather than in a
straight line. Every one of these was watched go **red** on a deliberately broken generator before it
shipped.)*

13.13 **The plate by the lift says the depth, the department, and whether you can breathe** (#612) — three
lines, one eye-line, on the wall you face when the doors open. The atmosphere line is `SuitAir.PlateLine` off
`SuitAir.SourceOf`, which is the same call the drain and the gauge make, so the sign on the wall and the tank
on your back cannot come apart. *(Enforced: `TheHudSaysWhereTheAirComesFromTests` walks every floor of every
clandestine site and fails the moment the two disagree.)*

Owner: *"I thought there is air in the base?"* / *"where here does it say if I consume tanks or have air?"*
The floor had known since #585 and nothing on screen said so.

**Three surfaces may SHOW it; exactly one may COMPUTE it.** The hud's chip, the plate over the car, and the
refuge's own `🫁` sign are all worth having — owner: *"I think the hud and level are enough … but having third
does not hurt"*. What is not worth having is three derivations, and that is what shipped: the drain branched
on its own conditions, the hud re-derived them beside it, and the plate called `HoldsPressure` for itself. It
took less than a day to bite — #608 added a fourth way to breathe and only the drain heard, so a captain
sitting in a refuge full of air was told in colour that their tank was running out.

**Signage goes on a PLATE, not merely in a brighter ink.** The depth and department shipped as worn paint at
47 % alpha; the owner said *"they are kind of hidden now"*, the ink was made yellow, and he hit it again. That
second miss is the whole lesson: the fault was never the hue. Paint has little contrast left to raise against
a corridor full of hull lines, doors and console glow, because what it competes with is **busy** rather than
bright. Text on a busy deck needs a background — which is what #348 already concluded one size down for the
room labels.

13.14 **The way in is an ordinary hut, and the doors are the only thing wrong with it** (#606).

> *"it could be in an ordinary hut, with 2 doors .. we have those. The expensive doors would be the clue... a
> clue we can get tipped about or find it in papers"* — and, after another look at the ground, *"the elevator
> still stands out on surface like a sore thumb."*

The second sentence is the law. **Whatever hides the head, it must not be the odd building on the site.**

- It is built by `SurfaceStructure` with a spec from `SurfaceStructure.Ordinary` — the same builder, the same
  size range, the same piled-regolith masonry and seeded angle as its neighbours. Before this it was five
  hand-typed lines in a 10 × 8 rectangle, which is why no amount of colour ever hid it: **it was drawn in a
  different hand from every other building on the moon,** and that reads from anywhere. Rectangular is the one
  property not seeded, and it earns the exception — a car is a box, and a rotated box is a shape the return
  spot, the keep-out and the audits can all answer *"is the captain inside this"* about without a second
  geometry to be wrong in.
- **Two doors, both `Imported` AND `Machined`.** Colour alone had already failed once (§13 / #585): violet
  marks shelters, one ruin hatch in seven, *and* the way down, so it identified nothing. Weight is the second
  channel — a heavy leaf with an inner rail and its frame picked out at the jambs, against the single thin
  stroke every other hatch on the moon is drawn with. It still retracts; **sealed is what it looks like, not
  what it does**, because a door that refused here would strand a captain in a lift head.
- **No caption.** The maintenance plate and *THE CAR IS STILL HERE* are gone, and the panel inside is named
  for what it looks like bolted to a wall (`▤ SERVICE PANEL`), never for what it does — console labels draw
  through walls, so a name is a sign on the outside whatever room it is standing in. The findability #584 was
  filed about moves to the **information**: the tip-gated tracker wash, the detector gradient, the papers that
  name a moon. *(That is the trade #606 makes on purpose: a clue chain is a better game than a caption, and a
  worse one if the chain ever stops working.)*
- **The claim ledger got recentred, not enlarged.** A hut twice the old shed's size is covered by moving
  `SecretLab.ChamberFootprint`'s disc rather than growing it — two circles, the hut on the door and the
  chamber half its depth out, and the smallest disc round both. The reservation went up by about a du. #587's
  warning stands: an over-claim is ground taken from the ordinary buildings, and it costs the world its
  variety.

*(Enforced: `TheLiftHeadIsJustAnotherHutTests` measures the DIFFERENCE between the site built with the
facility and without it — segment count, reach against the radii the plan publishes for its own buildings, no
label at all, and the doors — so it audits the drawn ground rather than the generator's intentions.
`TheLiftPutsYouSomewhereYouCanSTANDTests` still walks it: the head is bigger and rotated now, and both of
those are new ways to trap somebody.)*

13.15 **Every square the sim PLACES the captain on must be standable, steppable and connected** (#681).

> *"The second url put me into the wall... I cannot move."*

`/map?secretlab=deep&land=1` pinned the captain inside the wall of the lift head's own hut, HUD fully alive,
air counting down from 7 h 44, offering a hidden door and a regolith probe to somebody who could not take one
step. Deterministic — the same wall, every boot.

**Why nothing caught it.** Every reachability audit this project owns starts from a point it *assumes* is
good: 13.1 floods from the lift, `TheCaptainCanSTANDWhereTheLiftPutsThem` pins the car. The landing spawn
**is** that assumption. It is the same blind spot that let #600 live — an audit proves you can reach things
from X, and never once that X is somewhere a person can stand.

**The ladder, weakest to strongest.** All three, because they catch different breaks:

1. **Standable** — the square is in the walkable field. Catches spawn-in-wall.
2. **Can move** — at least one orthogonal neighbour is walkable. The owner's own ask (*"or a test on spawn
   that the player can move"*), and strictly stronger: a square can be perfectly clear and still be a **cell**
   with wall on all four sides, which rung 1 signs off happily.
3. **Can get home** — the spawn's walkable component holds the way home, the shelter, and the lift head where
   the ground has one. #600's lesson at minute zero: reachable is not returnable.

They run over **every square the sim places the captain on** — the `?land=1` drop, the `?secretlab=…&land=1`
doorstep, and the car's surface exit — across every body × landing site × `?secretlab` combination the
generator admits. *(Enforced: `TheLandingPutsYouSomewhereYouCanWALKTests`.)*

**Two causes, both of them this document's own bug classes.**

- **The offset described a building that had been rebuilt.** The landing computed its own answer — the head
  spot with 7.5 taken off its Y — and that number was written when the head was a hand-typed 10 × 8 box whose
  half-height was 4. §13.14 turned the head into an ordinary hut: 14–19.6 du wide, 11–15.4 deep, walls up to
  3 du of piled regolith, **and a seeded angle**. Seven and a half units below the middle of that is not a
  doorstep. **A caller doing its own geometry about a building it does not own** is #602 exactly, one head
  further along; the fix is the same one, `MoonSurface.LiftHeadBox.DoorStep`, the mirror of `CarFloor`.
- **Nothing had ever claimed the ground a landing lands on.** The shelters have had a keep-out since #585,
  the lab chamber since #585, the monolith since #649 — and the square the captain is actually set down on had
  none, so a seeded hut was legally built through it on `luna · The Depot Apron` and `secret-lab-site · The
  Depot Apron`. `SurfaceLayout.LandingApproach` is one answer read twice: the client asks it where to drop,
  the claim ledger asks it what to keep clear.

**The net, and why it is not the fix.** Owner, while stuck: *"Maybe some code to move the character either
side instead of spawning it so it cannot move?"* `SpawnNudge` spirals deterministically outward from a blocked
square to the nearest standable one, bounded to six paces, and every placement in the client goes through
`Map.StandCaptainAt`. It is **deterministic** (same blocked spawn → same rescue, or the next report of this
bug is unchaseable), **bounded** (past six paces the ground is broken rather than tight, and it says so
loudly instead of papering over), and **it speaks** — a pad hand takes your elbow and walks you clear, in the
pulse and in the excursion log.

> **The audit asserts the UN-NUDGED square.** A net that silently absorbs placer bugs is how the generator
> rots behind it — the same way the swept apron hid #574. Net catches the captain; audit catches the bug.

13.16 **The building says its name where you ENTER it, and nowhere else** (#694).

> *"every floor has the text 'The Clinic' on it. Some kind of artifact?"* — the owner, on B11 of a
> thirteen-floor site.

It was not an artifact and it was not a leak. `HiveInterior` drew `TitleOf(KindOn(…))` beside the shaft on
every floor, and the site's `Kind` is per-site by design — so a name that should have landed once landed
thirteen times. **The question is the finding.** A sign a player asks about because they suspect the renderer
has gone wrong is a sign that has stopped saying anything; by the third floor it was wallpaper, and the one
place where it would have been a story was already spent.

The facility plate now falls on the two floors you arrive on and no others:

- **B1** — the lobby. You came down out of the surface hut and the plate names what you have walked into.
- **The unlisted band's own shaft head** (§13.7), where the site has one. This is the single place in the
  game where the plate names a **different** `Kind` from everything above it: `▣ THE CLINIC` first seen under
  twelve floors of `RETENTION 40 YR` and `DESTRUCTION QUEUE` is that feature's whole arithmetic delivered by
  one sign, with the captain doing the sum themselves or not at all.

**Not every band head.** B5 and B9 are shaft heads too and they get nothing — a captain stepping out there has
not entered anything, they have gone deeper into the same place. What earns the plate is a `Kind` you have not
been told yet, which is why the law is *not* "is this floor a band top".

**Nothing is lost from the where-am-I answer**, because the facility title was never carrying it: the plate
over the car says `B11 · LONG STORAGE` and whether you can breathe (§13.13), the department livery says which
kind of floor this is (`LiveryFor`, #605), and both draw on every floor exactly as before. What went is a
repetition, not an answer.

**The head office takes no exception, and is better for it.** HQ has twenty-four listed floors and, by
`HasUnlistedBand`, nothing under them — so `▣ THE HEAD OFFICE` falls out of the same law on B1 alone. That is
also the more in-character reading: the head office does not have to keep telling you where you are.

**The law is Core's, the drawing is the client's.** `UndergroundComplex.ShowsFacilityPlate(bodyId, level)` is
a pure predicate over the building's own shape — `BandTop` and `HasUnlistedBand`, the same two calls the shafts
and the #590 cards are cut from — so the rule is testable without a renderer, and a renderer that answered it
for itself would be one more caller reasoning about a shaft it does not own (§13.15's second cause, one head
further along).

*(Enforced: `TheFloorTellsYouWhereYouAreTests` — over the scenario's sites plus 120 generated ones, with and
without unlisted bands, the predicate is true on exactly B1 and the unlisted shaft head and false on every
other floor and everywhere above ground; the two plates on a hiding site always contradict each other; HQ
names itself once. `TheFacilityPlateIsALobbySignTests` counts the titles on the **real deck** `FloorDeck`
returns, so the wiring cannot drift from the law. Watched go **red** against the shipped rule transcribed into
the predicate — 1 381 floors disagreed, opening with `luna B11: plate DRAWN, wanted absent — ▣ THE CLINIC`,
which is the owner's own sighting reproduced by the guard.)*

## Working method

The one that actually found these: **boot every scene and look at it.** Nearly every bug above was invisible
to reasoning and to Core tests, and obvious on sight. When something is found by eye, the fix is not complete
until a guard walks the real object — otherwise it comes back the next time two things have to agree and only
one is changed.
