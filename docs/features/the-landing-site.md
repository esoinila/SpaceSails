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

3.9 **And the tank prices the paperwork, because the tank prices TIME** (#696). Owner, mid-run, designing the
detective loop's cost model:

> *"How is our detective notebook / picture taking progressing for our ability to process the files etc so we
> don't need carry them. That is something one would do without using tanked air. It is good game mechanic...
> we take time to process the loot."*

Processing a find — photographing a document so the sheet can be left, deciding a paper is a map — is
**twenty seconds of standing still** (§13.9). The whole cost model follows from that one sentence and
**nothing computes it**:

| where you process it | what it costs |
| --- | --- |
| out on the regolith, or on a dead floor | the seconds, and the tank for every one of them (~16 s of air per sheet — held position is the cheapest breathing there is, `Breathing.Still`) |
| inside a shelter, inside a #608 refuge, on a floor that still holds pressure | only the seconds. The drain is already off there |
| standing in her tube | only the seconds, and the tank is filling anyway |

The issue's fourth venue — *"aboard ship: the natural place to clear a satchel after an excursion"* — is
**not** in v1, and this says so rather than implying it. The satchel is an excursion surface: `I` opens it
only while `_surface` is live, so there is no ship-side pocket to clear one from and nothing here invents a
ship UI to make the table look complete. What the captain actually has is her tube — walk back into it with a
full sleeve and every sheet processes free — plus every shelter, refuge and pressurised floor on the ground.
A satchel at a desk is an owner call, not something to sneak in under a feel pass.

That table is not implemented anywhere. `SuitAir.SourceOf` has answered *where the air comes from* on four
kinds of ground since #612, the drain is gated on its answer, and the hold does nothing but let sim time pass
— so the decision *read it here or haul it back to something pressurised* is emergent, and the two systems
are kept **deaf to each other by guard**: `Processing.cs` may not name the suit, and `SuitAir.cs` may not name
processing. The second half matters as much as the first: the walk-back arithmetic is the one number a captain
plans their life around, and it must never acquire an opinion about their filing habits.

**This is the first reason to visit a shelter when you are not dying.** It never had one. It was a place you
crawled to; it is a field darkroom now, and a captain who works a site properly comes back to it on purpose.

**An air warning fired mid-hold BREAKS the hold**, loudly, and says so. §3.2's rule — air is never a silent
timer that kills you — has an obvious failure mode the moment the game can ask a captain to stand still for
twenty seconds: a crossing line playing behind a progress bar the captain is watching fill is that silent
timer wearing a costume. Each threshold is one-shot per walk, so it is a **beat and not a lockout** — the next
press starts the same hold again, and finishing a manifest on the reserve is a decision the captain is allowed
to take.

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

5.5 **The rounds you find are yours, and nothing you were shown ever stops existing.** Every fixture that
gives ammunition — the shelter's press, the hut's locker, a ruin's half-shut drawer — fills the magazines
first, in order, and whatever the drums genuinely could not hold goes in the POCKET as loose rounds
(`SentryHandLoad.IntoThePocket`). Silent when they took everything, which is most of the time. Two of those
three used to drop the remainder on the floor, and the pocket rounds #603 wrote a whole verb for had, until
this, no way at all of reaching a captain's hand.

5.6 **You fill what you SET DOWN; the world fills what you CARRY.** The belts, the presses and the lockers
reach into the sling. A sentry standing out on the line is reached by one thing only, and it is the captain
walking rounds to it — `[I]` standing over it, any drum that is not full (#803's put verb; it used to be
only a drum reading exactly 00). One drum, one kind: a magazine at 00 takes anything, a magazine with
something in it takes more of the same, and the refusal says which. Nothing may put a number in a drum that
the two-digit readout cannot say — the instrument and the machine answer about one number or they are two
answers to one question (§the #797 law).

5.7 **A captain may point a gun by hand, and it is deliberately not autopilot.** The handset's fourth verb
(`🎯 DESIGNATE`): pick a gun you have set down, pick something it can see, say when. The arc and the sight
test are the sentry's OWN (`SentryBot.RangeDeckUnits`, `SurfaceCollision.HasLineOfSight`) so there is one
range law on this ground; the gun is aimed at the FACE of the door rather than its centre, because the
plate the captain fires at is published in floats and the wall behind it is laid in doubles, and one ulp
either way decides every lock in the game.

- **You can shoot a LOCK. You cannot shoot a WALL, and you cannot shoot a READER.** The game had already
  written this call in two sentences years apart: a room door's lock *"is mechanical and it was turned by
  somebody who then walked away with the key"*, and a sealed way has *"no reader … the bolts go through the
  frame and into the rock, and they were tightened from a side you are not on"*. So department doors and the
  goods-hoist shutter go; the sealed ways (#590 call 2) refuse, in their own words, and promise nothing.
  Every sign is recognised POSITIVELY (`UndergroundComplex.IsDoorSign` / `IsFreightShutter`) — a lock nobody
  thought about is refused rather than waved through.
- **It costs rounds and it costs NOISE.** Six rounds at a hasp, or one of anything that does its work on the
  far side of the first thing it meets. The pack's ear is rung the way every loud act on this ground rings
  it, and the shot itself is FILED — who fired, at what, where, when, how many (`GunfireHeard`). Nothing in
  this build reacts to that ledger; it is the seam the guards' lane prices, written from the first shot
  rather than re-derived later from a HUD line that has scrolled away.
- **Behind a door somebody shut is a room.** Bare floor, bare walls, bolt holes where shelving was taken
  out. Nothing that comes open ever explains the building (§13.8).

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

8.5 **The eye gets the index too** (#858, out of Lab 45). 8.1's "when the caller hands them one" was the whole
bug: everything that *walked* was handed `DeckPlan.CollisionField`, and everything that *looked* was handed a
plain `List<Segment>` that `SightBlockers()` refilled every frame. Lab 45 measured the sweep at a strict
**O(walls), ~18–25 ns a segment** — 63% of a guard's per-frame cost on the 465-segment floor, and **29× the
indexed answer at 436 segments**, where the indexed one is flat. `SightBlockers()` now files its list into a
`WallIndex` and **keeps** it, rebuilt only when the stone changes (a fresh plan, or a #371 append — both hand
`CollisionSegments` a new array) or when a door's shut-state actually flips.

**Rule: a second view of one fact is a caller waiting to be handed the wrong one.** The index is built *from*
the list, so what the eye sweeps and what a hand sweep would sweep are the same segments by construction.

8.6 **Work a body is going to need is done while it is standing still** (#858). `AutoWalk.Plan` over a guard's
leg cost a median 1.6–2.2 ms and a worst **6.4 ms — 38.6% of a 60 fps frame** — spent whole on the one frame he
leaves a stop, about twice a minute per guard, *natively*, in a game that ships to WASM. He stands at that stop
for `PatrolBeat.StandSeconds` = 5 s either way, so `DeckReachability.Search` (the same A\*, with a handle on
it) walks `PatrolBeat.PlanCellsAFrame` = 128 lattice cells a frame through the stand instead.

Three things keep it honest: `Path` **is** that class run to the end, so a sliced search and a whole one cannot
return different routes; `AutoWalk.Planner` carries the two points it was planned between, so a man whose
errand changed while he stood is never handed somebody else's route; and `Finish()` completes whatever is left
on the frame it is asked, so there is no state in which the answer is *"not yet"* and a body waits forever.
The budget is a **cell** count and not a millisecond one on purpose — it means the same thing in WASM, where
the clock this was measured on does not.

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

**And the second car does not sell depth** (#801 — written into the code against this section before this
section said it, which is the drift #826 closed). Since #801 a band has two cars, and the law above is a law
about the *building*, not about a particular hole in it — so the goods car's panel carries the band's own
four floors and **nothing else**: no SURFACE row, because the only hole with a hut on top of it is the
cage's (#606), and **no gate row**, because a second car may not become a way to buy depth without the
paper. The wrong-shaft "refusal stage" is therefore deliberately not a refusal at all — the ambitious press
never exists, because the row was never offered. What the captain gets instead is told ONCE, standing, in
the inspectorate voice: the plate says the scope (`🛗 GOODS CAR 2 · THIS BAND ONLY`) and the panel names the
way out rather than rejecting the ask — *"The goods car. It runs these floors and it does not climb out: for
the surface, and for anything below this band, the cage is at the other end of the corridor."* Within a band
the two cars are interchangeable; the moment you want to *leave* the band, you want the cage — which is what
makes the pair worth walking between rather than redundant. On the cage's panel the SURFACE row is always
present and asks the same law as every other row (#802, `HoldsPressure` decides the sentence): that row once
typed `Pressurised: true` as a literal and lied for six PRs, and it is the reason no row on the panel may
ever type its own answer again. *(Enforced: `TheOtherCarTests`, `TheLiftPanelTests`; the two panels are one
method with one clause, so "which floors does this band have" cannot drift into two answers.)*

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
the gate's refusal has always described (*"every one of them countersigned, current, and for another shaft"*;
since #684 the sharper *"this one was issued for X SITE"*).
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

**And the deck marks it, because a way back that runs on memory is not a way back** (#698). Owner, on B12 of the
clinic, within the hour of the drop verb shipping: *"I dropped 3 files on somebody here but there was nothing
marked onto the map?"* That was the judgement call #691 filed open — *"a left thing is not drawn on the deck; the
line says where, no marker; cheap to add later"* — collected the same afternoon, and he is right that it was
never a nicety. A captain who sheds weight is **planning to come back**, and #615's law only holds if the way
back is real (#600's lift proved that an audit can show you can *reach* a thing without ever showing you can get
*home* to it).

- **🗎 WHAT YOU LEFT** — one mark per **spot**, at the square's own centre, on the current floor or ground. Three
  files on one square is one mark; the plate never counts and never names them. The captain knows what they put
  down, and a plate reading "3 FILES" turns a decision into a receipt.
- **Scenery.** No wall, no structure, no collision segment. Being pinned against your own paperwork would be a
  worse bug than the missing mark.
- **`E — take back what you left`** on the keybar the whole time you are in the recovery ring, and it **replaces**
  the ground verb rather than joining it — because [E] answers your feet before the walls, so *"E — dig / use"*
  and even *"⛏ E — BURY THE CHEST HERE"* are, at that exact spot, no longer true.
- **One ring, asked once.** The ring the key obeyed was written out inline in the client. It is
  `LeftBehind.SpotInReach` now, and the mark, the offer and the press all ask it — a prompt measured off a second
  transcription of the same geometry is the house's fifth bug class waiting to happen.
- **Every deck.** Composed on all three branches of the surface rebuild — Hive floor, derelict steel, open
  regolith — the appended-region way the hidden door and the outpost hut are, so no generator and none of the A*
  audits that walk them change. It appears on the drop and clears on the recovery, both of which now redraw:
  a mark that waited for some unrelated rebuild is, at the moment the owner looks down, no mark at all.

**And processing the loot TAKES TIME, which is how the air came to price the where** (#696). Owner, mid-run,
designing the detective loop's cost model:

> *"How is our detective notebook / picture taking progressing for our ability to process the files etc so we
> don't need carry them. That is something one would do without using tanked air. It is good game mechanic...
> **we take time to process the loot**."*

Until this, the loop above had no body. A captain could stand at a door with a full sleeve and empty it into
the field book in six clicks — gist filed, paper dropped, pocket free — and the world outside the dialog was
exactly where it had been when they opened it. Knowledge was the whole reward of the ground, and it was being
collected in a frozen moment.

> **Processing a document is a HOLD: `Processing.SecondsPerDocument` (20 s) of standing still, and the effect
> fires only at the far end.**

Twenty out of the owner's fifteen-to-thirty. The bottom of that band is a pause nobody plans around; the top
is where a captain stops taking the decision seriously and starts resenting it. It is a sixtieth of a tank, so
a sleeve of six worked through on open regolith is two minutes and a tenth of your air — a real bite that
never on its own kills you.

- **Both halves of the detective loop, at the same price.** Leaving a `Paper` or a `Dirt` via 🫳 (the gist
  filing above) and reading a paper as a clue at the tracker (§13.10, #603) are the same twenty seconds.
  Charging for one and giving the other away would teach the captain to read everything on the spot and file
  nothing, which is the decision the cost model exists to create being deleted by the cost model.
- **Rounds, cards and relic notes still go down instantly.** There is no gist to a handful of ammunition, so
  there is nothing to stand still for — and the branch asks `LeftBehind.GistOf`, the same question the gist
  filing itself asks, so the rows that carry a clock are exactly the rows the hint warns about.
- **The satchel SHUTS on the way in.** This deliberately reverses the *"the satchel stays open"* call above,
  and the reason is the mechanic: the teeth are twenty seconds of being **stationary and visible**, the motion
  tracker keeps running the whole time, and a captain cannot watch a fan through a backdrop blur. The bar fills
  over the captain's own mark on the one channel bar the surface has always had (#562), wearing 📸 and the
  warning amber — the rearm is the ship helping you; this is you exposing yourself. #680's law was never *"say
  it in the dialog"*; it is *say it where the player is looking*, and one method decides that now. Reopen the
  pocket mid-hold and the pocket says what is under your hands.
- **Nothing is spent until the far end.** The document is in the sleeve for the whole hold — nothing removed,
  nothing filed, nothing on the ground — so an interruption has nothing to undo and no retry can double-file.
- **Four ways to lose it, all of them said.** Stepping off the spot (`StandingToleranceDu`, 1.5 du — a nudge,
  never a step), riding the lift to another floor, an air alarm (§3.9), and something getting a hand on you.
  The last is on the SWING and not on the wound: a captain who turns a blow aside has still had an arm come
  through the space they were photographing into. Lifting off with a hold running says so too, because
  otherwise the first thing they do at the desk is look for a gist that was never filed.
- **The far end fires the effect the game already had** — `SetItDown` for a leave, `TheOfferIsAnswered` for a
  clue. Not a copy of those endings, the same ones (#697's law, one lane later).
- **The free venues are the ones you can reach with a pocket in your hand**: her tube, every shelter, every
  #608 refuge, every floor that still holds pressure (§3.9). Not the ship — the satchel is an excursion
  surface and there is no desk-side pocket, which the air table says out loud instead of implying one.
- **`?process=0`** makes holds instant, for story tests. There is deliberately no cheat for what a hold costs
  in air, because nothing computes that.

*(Enforced: `WeTakeTimeToProcessTheLootTests` (Core) — the clock is one number in the owner's band and a real
slice of a tank; a zero-length hold is finished rather than divided by; the bar filling and the effect firing
agree on what *finished* means; the stand-still tolerance is a circle and not a box and is smaller than half a
second of walking; every interruption line still promises the paper is in the sleeve and nothing was filed, and
the four are four sentences; the hint and the hold read one number; **processing where the air is spends
nothing and processing on open regolith spends exactly the hold's worth** (16 s per sheet), driven through the
real `SuitAir` predicate and drain; and the two files may not name each other. Watched go RED against four
transcriptions — `Done` written with `>`, `Fraction` as a bare division, a per-axis tolerance, and the
coupling itself in both directions: 2, 2, 1 and 1 red. `ProcessingTheLootTakesTimeTests` (client) for the
twelve shapes — the press starts a clock instead of filing on the spot, the clue read costs the same, nothing
is spent until the far end, the far end fires the shared ending after clearing the clock, **the darkroom never
mentions the tank and is stepped after the suit on the same tick**, walking off and changing floor abandon, all
three air alarms break the hold, being reached and lifting off break it, the one bar says which slow thing it
is, the pocket speaks for itself mid-hold, the clock is one number the hint reads, and the QA cheat switches
the clock and nothing else. Watched go RED against the build that shipped #697: **12 of 12**.)*

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
5. `TheGroundKnowsWhereYouLeftItTests` (#698) for the store's two new questions — one mark per spot however much
is on it, floor-scoped both ways, the mark gone when the spot empties and **kept** when something would not fit,
and the ring's exact edge: every square of it gives, one square further out gives nothing. Watched go RED, 2 of
10, against the two naive readings they exist to rule out (the ring shrunk to the square you stand on; a spot
list not scoped to a floor); the other eight pin new behaviour and are honestly not red-provable.
`TheDeckMarksWhatYouLeftTests` (#698) builds the real Hive floor and the real regolith and counts the marks on
them, pins that a mark inside the interact radius is always inside the recovery ring — so the [E] drawn over it
is never one the pickup will refuse — and then reads the shipped source for the wiring: every branch of
`RebuildSurfaceDeck` that builds a deck composes the marks, the drop and the recovery both redraw, and the keybar
reaches its offer through the same `AnythingInReach` the key does. Watched go RED against the unmarked build,
10 of 13 — the three survivors being the three that cannot go red that way (an empty floor carries no marks, the
mark adds no collision, and the interact-radius arithmetic).)*

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

**And the panel reads it out loud** (#684). The lift panel goes through the wallet without being asked — the
owner's ruling is that this *is* its character, and a `try it →` verb at the gate would make the building
polite — but for three issues it answered out of a second set of sentences of its own (`WrongCardLine`), so
the sharpened matrix above had **no client caller at all**. That is the third named bug class in a mirror: the
sim knowing something the sentence does not say. `WrongCardLine` is gone; `UndergroundComplex.TheGateReads`
asks `SatchelTry.ReadTheWallet` and passes its answer through untouched, and the answer goes up as a card in
#528's idiom — the same title at a refusal and at a reading, the presented card's own office face, and the
matrix's line verbatim as the caption. Per #736 that caption is the outcome: the line lives **on** the card
that is up, never only in the panel row behind its backdrop. An empty wallet is pictured with
`AuthorityCardFallbackArtUrl`, the nameless face, because painting one of the five offices onto a pass the
captain does not have would be the game lying about a possession.

| gets a card | does not |
| --- | --- |
| an authority card (which shaft, which site; and *"not this one"* when it is foreign) | operational paper — it has its own reader (#603) |
| the two-stage penetrator | issue ball — it is the round you always have |
| the thing on the pallet | a file on somebody — leverage, not a display piece |

A game where every object earns a full-screen card has no objects that matter.

**And the card wears its own office's face** (#695). Owner, wallet in hand: *"I have 3 ID cards but they all
have the same gen AI image."* The letterhead had been rolling one of five offices off `hive:card:{body}:{band}`
since #679 while the picture stayed a single #528 constant, so three cards from three moons opened three
different sentences over one photograph. There are five faces now — works, liaison, estates, procurement,
inspectorate — and `AuthorityCardArtUrl(card)` reads **the same roll the title reads**
(`UndergroundComplex.OfficeOf`, one record carrying both the letterhead and the file) rather than re-deriving
it, because a second sum for a fact that already has one is this repo's most expensive habit. Compositions in
`docs/art-manifest-hive.md` §2a; the #528 original stays as the fallback for a card id nothing can parse.

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

13.17 **People worked shifts down here, and the plumbing is where it shows** (#707).

> *"all the secret labs dont have any cantina / bar nor any toilets. We should add those like to the most
> top most pressurized floor. The toilets should have like bathroom level equipments and the high level
> important rooms would have their built in bathrooms and be pressurized."* — the owner, the morning after
> walking a clinic.

It is the cheapest storytelling left in this building and the most damning. Everything down here says
**budget** — a lined shaft, poured walls, a car still running on somebody's account decades after the last
invoice — and none of it said **people**. A counter with the bottles gone and a wall of cubicles say people,
in the only register this ground is allowed to use: what somebody was made to pay for.

**One rule, three rooms, and the rank falls out of it.**

| | where | what is in it |
| --- | --- | --- |
| **the upper canteen** | the topmost floor that holds pressure (`TopPressurisedFloor`) | a counter, the service side closed off behind it, three round tops |
| **the washroom** | the room next door to it | a basin run and three cubicle dividers — *bathroom-grade*, per the owner |
| **the staff canteen** | the deepest floor the directory **admits to** that still holds pressure (`StaffCanteenFloor`) | four machines against the back wall, tables close together, no counter |
| **the en-suite** | hung off the back of every *principal* room on a floor that breathes | one pan, no plate |

**The two tiers are an inversion, and the inversion is the design** (owner ruling, 2026-08-05). The upper bar
is *"publicly accessible and just happens to be in the secret base"* — vendors drink there, normal credits
work, security is loose **by design**, and it is therefore **tight-lipped**: there are strangers in the room
and everybody knows it. The deep mess is machines and no bottles and a room where every face is known, so the
talk there is careless — **loose-lipped**, in exactly the room a stranger cannot stand in. *Safety and
information trade off opposite to where a player first looks.* The rooms ship here; the social layer that goes
in them (overhearing the next table, what a face that does not belong costs, the meal line asking for a pass)
is filed separately by the owner's own scope split.

> **And the bar is why band 0 never wanted a card.** Owner, closing the loop: *"setting access to off the
> books secret lab to partners all trying to keep things off records would be bureaucratic nightmare of
> office interorganization bureaucracy so the underground bar just is there with access from surface. It kind
> of provides cover-story as well."* The mechanic has shipped since #590 — `LiftPanel` has never asked for
> anything on the first band — and it now has its reason: credentialing every deniable partner across
> organisations that all deny existing was never going to happen, so the first floor is simply **open**, and
> the bar is why anybody believes the shed on the surface is what it pretends to be. **Access control starts
> where the drinks stop**, which is exactly how the card grammar already works. Nothing was built for this.
> The plate carries the fact (`🍸 CANTEEN 1 · CARRIERS & CONTRACTORS · NO PASS REQUIRED`) and never the
> reason — it is the one **warm** sign in a building of `DESTRUCTION QUEUE` and `MORTUARY`, and it is the only
> sign down here that is a lie. *(Enforced: `TheBarPlateSaysNoPassIsNeededAndNeverSaysWHY` checks the plate
> against the panel it is describing, so the sign cannot outlive the mechanic, and greps the six words that
> would explain it.)*

**Rank is readable in plumbing.** A plate is *principal* when it names an office or an authority — somewhere a
decision gets signed — rather than a process, a store, or a room where work is done **to** somebody.
`COLD STORE 2` is where things are kept, `SUBJECT PREP` is where things are done, `QUOTA OFFICE` is where a
person sat and ruled on other people; that person had a door of their own and did not queue for the cubicles
on B1. The cell itself carries **no plate** — a private washroom does not need a sign, and that absence is the
last word of the tell. And the ratio is the head office, emergent and never stated: **one plate in eight at a
branch, five in twelve at HQ**, so a captain who has crawled a Hive walks a head-office corridor and sees
private washrooms on half the doors, with nothing anywhere telling them what that means.

**Nothing is ever plumbed on a floor that cannot breathe, and that is the load-bearing rule.** A canteen, a
cubicle and an en-suite are all plumbing, and plumbing is for people out of their suits — the owner's own
general form of it (*"any room that would house like office work would be pressurized by that constraint ...
any kind of fine motor skill stuff"*), one notch further along. But the reason it is written as a **law** is
§13.13's: the moment one room down here breathes for a reason that is not `HoldsPressure`, the plate by the
lift and the gauge on the suit are reading two different maps. **There is one pressure fact in this building
and every amenity is asked to justify itself against it.** *(Enforced:
`NOTHINGIsEverPlumbedOnAFloorThatCannotBreathe` over 1 150 floors.)*

**Three placement calls, each overrulable in one line.**

- **The canteen is the nearest room to the car** — the exact opposite of the refuge law (§13.12) and right
  for the same reason. A refuge earns its existence by being a detour; a bar earns its by being the first
  door off the lift, because it is the room a haulier with a pallet and forty minutes actually used. No dice:
  a building puts its catering by the car, and a captain gets to learn that.
- **The washroom is next door to the canteen**, for the reason a plumber would give — a building runs **one
  wet stack** and hangs everything that needs a drain off it.
- **The mess is on a floor the directory lists.** *Deepest*, so the owner's inversion has the distance it
  needs; *listed*, because catering is a thing a directory knows about and the band nobody listed has no
  department, no livery and no plate (§13.7). A canteen sign down there would be the building admitting to a
  floor in the one place it must not. A site too shallow for a second pressurised floor simply has no mess,
  and the law says so out loud rather than inventing a room to satisfy itself.

**The head office takes no exception and answers in its own vocabulary** (#411). Not a canteen and a washroom
but `🍸 THE DINING ROOM · GUESTS & DEPUTATIONS` and `🚻 CLOAKS & WASHROOMS` — the coat rack from its own
arrival card, given a room — and its mess is `🍽 THE STAFF DINING HALL · ESTABLISHMENT ONLY`, which is the
plate on its own B2. Same one rule, same grammar, a rank nobody has to be told about. The dining room is
**laid**, for eleven, with the chair at the head pulled out by a hand's width and no date on anything in it.

**The procurement joke ships as one manifest and names nobody.** Owner: *"paperwork that says it is officially
delivered to and operating at a school far away 🤭 — the procurement comedy in one manifest line (homage
unnamed; no film titles)."* So it is a **catering** manifest, costed per head, quarterly, for a school roll on
another world — readable as a fiddle and never as a confession, because the thing this organisation actually
procures is the one thing nothing in this building may say (§13.8). At the head office the same sheet turns up
as the copy **the office kept**, which is the rank difference doing the joke's punchline for free.
*(Enforced: `TheMESSPaperworkIsDeniableAndNamesNOBODY` greps the film, the wink and the emoji;
`NoAmenityEXPLAINSWhatThisPlaceWasFor` runs §13.8's sixteen forbidden words over every plate, fixture name and
room line on both grades of building.)*

**Two mistakes this made, both of them already in the table at the top of this document.**

- **A claim ledger that only looks forward.** The en-suite hangs *outward*, off the room's back wall, into
  ground the room columns either side of a rib reach back toward — so the cell is checked against the ledger
  **before** it is built, not merely added to it afterwards. It also has to be asked **before its own parent
  claims its ground**: claim boxes are inflated 1.5 du on every side, so the first cut had every single
  en-suite in the game sitting inside its own parent's keep-out and refusing itself — 202 floors reading
  *"1 principal room(s) and 0 en-suite(s)"* with the geometry perfectly correct.
- **Three round tables at coordinates that meant something else.** `HiveInterior` passed
  `tables: DeckPlan.Ship.Tables` — the **ship's** cantina tops — so every Hive floor ever drawn has had three
  rings at `y = +7.5`, forty du above the top of the field and outside the plan entirely. Nobody had reported
  it because nobody had reason to look up there. The rings belong to a room now.

*(Enforced, Core: `TheHiveAmenitiesTests` over 101 sites and 1 150 floors — exactly one bar on the top
pressurised floor of every site and none anywhere else, exactly one mess on the deepest listed pressurised
floor or an honest none, an en-suite off every principal room on a floor that breathes **and off nothing
else**, every principal plate proved to be a plate some building actually hangs, and carving proved to be a
**relabel** rather than a removal by counting the doorways that were cut against the places they now lead.
Watched go **red** on the shipped generator first: `101 of 101 site(s) have nowhere to eat and nowhere to
wash`, `185 of 1150` floors with no canteen, `202 of 1150` with principal rooms and no cells. Client:
`YouCanWalkTheHiveTests` floods the amenities as ordinary rooms under §13.1, and
`APRIVATEWashroomBehindAnOpenDoorIsAWashroomYouCanWALKInto` walks the cells, which that flood cannot see
because a private washroom has no console in it — proved red by walling its doorway over, opening
`luna B5: the en-suite off 'CONSENT FILES' cannot be walked to from the lift`.)*

13.18 **A floor may be DARK, and on a dark floor the suit's headlights are the whole of the seeing** (#708).

Owner's ruling 2026-08-05: *"For dark levels our suit should have forward facing headlights ... the
pre-existing tunnels would be scary as dark ones and totally different style."*

**Darkness is a property a floor states about itself**, in exactly the way `HoldsPressure` states whether the
same floor can be breathed — and for the same reason §13.13 gives. The moment two things in this building can
each hold an opinion about whether the lights are on, the plate by the lift and the picture on the screen are
reading two different maps. So there is one ask, `UndergroundComplex.IsDark(bodyId, level, lampsOut)`, and
everything that cares calls it: the renderer, the boot cheat, and any sim that ever wants to know (nothing
does today). **The cheat is an ARGUMENT to that ask and never a second answer OR-ed in beside it at a call
site** — an `||` at a call site is precisely how a second source of truth gets built one honest line at a
time, and this one would have blacked out the regolith at noon.

**No shipped floor is dark, and that is the point.** Every listed floor keeps its failing facility light and
the instrument-lit look it has always had; the customer is the FOUND BAND (#677) — galleries that pre-exist
the shaft, with no fixtures, no wiring and no ventilation anybody can find — and it will answer in one line
when it is built. Until then the only way in is `?dark=1`, because a scene nobody can reach on demand is a
scene that ships broken. **Dead-air floors are not dark and do not flicker:** a flicker is a fixture reporting
that it is dying, a floor that cannot be breathed is not a floor whose lamps have failed, and wiring the two
together would have made the suit gauge and the ceiling say the same thing twice.

**The cone is the sweepers' lamp, and it has no numbers of its own.** 20 du inside a 70° cone —
`InspectionTeam.LampRange` and `LampConeHalfAngleDegrees`, the black-ops team's kit since #538. A lamp is a
lamp; inventing a second pair of numbers for the captain's would mean the two lights in this game were
different equipment for no reason anybody could name, and the first time both cones were drawn on one deck
the difference would read as a bug. The **arm's-reach ring** is deliberately not part of it: it is not a lamp,
it is the radius in which you can put your hands on something, so it is `DeckPlan.InteractRadius` — which
makes it #212's law in geometry, **an affordance the game will let you use is never invisible.**

**On a top-down plan, turning your body becomes an act of LOOKING** — the drawing carrying the fact the way
the monolith's shadow carries height (§10). Walls appear at the cone's edge as you turn. **Collision is
unchanged**: you can walk into what you cannot see, and something you cannot see can walk into you.

**The instruments are not part of the world and are never touched.** The motion fan, its wall-smudges (§13.6,
#591) and its ghosts are drawn *after* the dark is laid down and read identically with the lights out —
hearing a contact cross behind you, outside the cone, in a hall your lights will never reach, is the entire
reason to put the lights out at all. A **deployed sentry** is drawn over the dark too, and its rules are its
own: it sees what it sees, fires when it fires, and its counter is readable across a black floor. You can see
a light in an unlit hall; what you cannot see is what it is lighting.

**Three calls, each overrulable in one line.** The cone does **not** stop at bulkheads yet (a raycast per wall
per frame, for a floor that today only exists behind a cheat — filed, not forgotten). **First person (F) is
out of scope and stays as it is**: it is a raycaster whose field of view is `HalfFov = 0.62` rad, 71°, which
is the lamp's 70° to within a degree — it already shows almost exactly what the headlights light, and making
it dark would be a job about *falloff with distance*, not about a cone. And the **ordinary floors are
untouched**, which is not a promise but a guard.

*(Enforced. Client: `TheHeadlightsAreTheWholeOfTheSeeingTests` drives the REAL `DeckView.Draw` on a real
`HiveInterior.FloorDeck` through a recording renderer and asks whether the ink ever lands outside the light —
not dim, **absent** — sorting the canvas-anchored chrome out by nudging the pan and seeing what moved, rather
than carrying a list of HUD things to excuse. Watched go **red** against the renderer with the flag plumbed
and honoured by nobody: `222 of 226 primitive(s) drawn in the dark, outside the cone`, and
`every facing drew the same amount of building (226, 226, 226, 226)`. The instrument guard was proved red by
teaching the renderer one word — skip the smudge in the dark — opening `5 smudge ring(s) with the lights on,
1 with them out`. Core: `TheDarkIsAPropertyOfTheFloorTests` — no shipped floor dark, the cheat proved to be an
argument and not an `||` (**red** at `4 above-ground level(s) blacked out by the cheat: 0, 1, 3, 12`),
darkness proved to be a different law from dead air, from depth and from the unlisted band, and the two lamps
proved the same kit **behaviourally** over ten thousand points of open ground rather than by comparing two constants — which is a
test that would still pass if somebody typed the number in by hand, and was watched go red when they did:
`facing 0.00: (20.9,-14.7) — sweeper cannot see, suit lights`.)*

13.19 **One would-be-empty room in six holds a book that should not be there** (#701, v1 of the library
layer).

Owner: *"a better alternative to finding an empty room. You look around but only one book catches your
attention."* And the framing that states the stakes best: **searching a room and finding nothing is the most
common outcome in this building and the least written.** A quarter of every floor pays out one sentence about
fittings. This is that sentence, once in six, becoming something a captain remembers.

**The engine is a department that reads everything, and it is never on screen.** The facility runs a
books-as-intelligence function — staff who know they are told nothing, reading fiction, myth and fringe
cosmology, sifting for leaks about the before-worlds (§10). The homage the shape comes from stays unnamed; no
film title appears anywhere. This is also the canon solution to a feature that could very easily have
explained things: **the books never explain, and the READING of them is the staff doing exactly the inference
the player is doing.** They never found the leak either. That fact is nowhere stated and everywhere present.

**The register test, adopted as law by the owner and applied to every one of the ten:** *"the find has to be
something the player can be delighted to disbelieve."* A fourteenth-century geography of kingdoms that are
not on any chart passes. Anything with a live court docket and real victims fails it before any other
consideration applies — not squeamishness, craft: it stops being folklore the player enjoys being wrong about
and becomes the game *saying something*.

**Three laws, and each is a way the beat dies quietly:**

- **Most empties stay empty.** §10.3 applied one floor down: if there were always something, the walk is a
  shopping trip. One would-be-empty room in `OddBooks.Rate` = 6, seeded on site + floor + room, so a shelf is
  that shelf forever and a captain who walks back finds the same thing on it. **Measured, not assumed** —
  the guard sweeps seventy generated sites and reads the rate off the sweep.
- **Nothing enters the satchel and the room is never struck off.** A book is read where it stands and left
  where it stands, which is what makes re-reading possible at all. The room's line is the **shelf line**; [E]
  opens a caption-only look-card in the #528 idiom (title = the shelf line, body = the card text, no art file
  — the lifeboat-muster precedent, which never claims a picture rather than wiring one and hiding it on
  error). No credits, no pocket, no lead.
- **Looking is free, knowledge is one-shot** (#603). The card comes up every time; the **gist** files to the
  casebook once per book per game-thread, and per BOOK rather than per room — the same title on a second moon
  is still one line in the book. The read-list rides the vault beside the found secret labs (#409), because
  knowledge does not un-happen on a reload. The shelf line is a pulse and is never filed: filing both would
  put one shelf in the casebook twice, in two registers, on every search.

**The prose is authored and lifted verbatim** — ten entries, each a shelf line, a card and a gist. The house
frame around the shelf fragment is one sentence, the same for all ten, and the guard proves it is one frame by
taking the authored fragment back out of it and asking how many different remainders there are. **The two
reference texts (the mechanics text in its 27th edition, the materials reference in its 31st) count triple on
a floor that reads for work** — `Kind.Laboratory` and `Kind.TransitStation`, the two kinds whose door
vocabulary is about numbers and materials rather than about filing, grading or treating people. Everything
stays reachable everywhere: a fat paperback in a laboratory is a fact about a person, and a weighting that
became a rule would turn a shelf into a label.

**Nothing on any shelf ever names the monolith, the Old Ones, the Reevers or the found halls** (§13.8, §10) —
the staff read all of it and found nothing, which is the joke and the dread. Deliberately *not* on the
reserved list: **cyclopean**. Entry 6 is a weird-tales collection and says *cyclopean cities*, which is the
game's own register looking back at itself out of a cheap paperback; the reserved words are the ones that
would make a book name THIS world's canon, not the ones its own genre owns.

**`?book=N` is the door.** All ten on demand — 1..10 forces that catalog entry into every would-be-empty room
this excursion searches, `?book=on` forces the seeded one. It cannot put a book in an occupied room, because
a book is what a would-be-empty room has *instead of* the empty line and a tester playtesting a room the game
cannot produce has learned nothing. A scene nobody can reach on demand is a scene that ships broken.

*(v1. The occupant layer — a work shelf that says what somebody did and a freetime shelf that says who they
were, §12.3 piece-material rather than a dossier — is the other half of #701 and is not built.)*

*(Enforced. Core: `TheEmptyRoomThatHoldsOneBookTests` — the split measured over 2 700 generated rooms
(`the odd book turns up in 300 of 1773 would-be-empty rooms (16.9 %) — the law is one in 6 (16.7 %)`), the
canon grep, every entry proved drawable, the weighting measured (`41.2 %` of work-floor shelves against
`20.0 %` elsewhere) and both buckets proved to reach all ten. Watched go **red**: against the shipped roll —
the empty line, every time — at `1773 would-be-empty room(s) swept and not one of them holds a book — the
feature is dead`; against a transcription that names the canon, at `Found: "monolith"`; against a reading that
always files, at `300 shelves read, 300 gist(s) filed and only 10 of them different`; and against the
weighting removed, at `21.6 % of shelves where the work is and 20.0 % elsewhere`. Client:
`TheShelfIsReadWhereItStandsTests` guards the wiring's SHAPE, which is where this one can only die — the ask
placed before the pocket and before the room is struck off, the branch proved to contain no
`HiveRoomsEmptied`, `Satchel.Add`, `_credits` or `GrantLabLead`, the card proved caption-only, and the cheat
proved to be an argument and never a second answer. Watched go red at
`_bookCheat appears 3 time(s) in the surface wiring`, at `Found: "HiveRoomsEmptied"`, and at
`Not found: "shelf.Title, null, shelf.Card"`.)*

13.20 **A rare deep site has a band NOBODY DUG, and past the seam nothing belongs to anybody** (#677, v1 of
the found halls).

Owner ruling 2026-08-04, recorded in `worldbuilding-notes.md` §10. This is humanity's fourth run; the prior
three were ENDED, by fire then ice then flood, and every end spared a remnant **underground, into massive
halls**. Out on the moons that is this: a dig that breaks into volume which was *already there*.

**It is a different CLASS of thing from the band nobody listed (§13.7), and keeping the two apart is most of
the work.** That one is human all the way down — poured, surveyed, invoiced, and hidden from the staff who
paid for it. This one was never ours, so every single thing that makes a facility legible is absent from it:
no plate, no department, no livery, no locked door, no stencilled distance, no drain, no shelf, no lamp.

**The register, in the owner's own four words.** On the air-with-no-visible-means ruling: *"Horror served as
smooth comfy pillow 🤭 … love that"*. The halls are **comfortable**. Nothing down there threatens; everything
accommodates — good air, warm, smooth underfoot, a place kept ready. The dread is entirely in the implication:
**a pillow means you were expected.** It is the same parental-not-predatory law the monolith watch runs on
(§10.4c), delivered through amenity instead of attention — and it pairs with #707's realism pass above the
seam, where the facility's plumbing EXPLAINS itself and the halls' comfort never does. It is also why standing
in one **costs nothing extra**: a site that bills a captain by the room for being in a comfortable place is a
predator whatever the prose says.

**One band below the unlisted band, with a WHOLE BAND OF NOTHING between them.** §13.7's gap is the remainder
of a band the listed building happened to stop inside. This gap is the point: the unlisted band *fills* its
band, so the next one down would be flush against it — one shaft's floor and the next shaft's ceiling, which
is how a BUILDING continues. Four floors of untouched rock is what says the digging stopped and something else
began. `FoundBandOf` is `UnlistedBandOf + 2`; `FloorsOf` and `TrueDepthOf` carry it the way they carried
§13.7; and nothing may ever authorise or offer a button to the band between — `SiteHasBand` says no, and
`NextShaftBelow` steps over it, which is the one call every gate and every card asks now instead of
`BandOf(level) + 1`.

> **And the bottom of the building stopped being the bottom of the hole.** `UnlistedBottomOf` exists for one
> assertion: the thing on the pallet (§13.10) is crated, invoiced and has the lights left on over it, so it
> belongs on the deepest floor somebody DUG. Written as `TrueDepthOf` — which it was — it moved two bands down
> into a gallery the moment this shipped, and every existing test stayed green.

**The way down is §13.5's card idiom one rung further: a card found in the band nobody listed.** Designated
(`FoundKeyRoomFor` — room 0 of the unlisted band's own shaft head, the floor where the plate finally names a
different building, §13.16) for exactly the reason §13.7's is: a Key is one face in nine, and a band that
rolled none would leave a site's halls unreachable not for that visit but forever, with nothing on screen ever
saying so. Every other Key in that band mints the same card anyway. **The panel never admits the shaft
exists** — the same silence §13.7 keeps, for the harder version of the same reason.

**Rare, and the rate is measured.** One site in five of the ones that already hide a band — and because only
the shallower half of those has room under it for another shaft inside the performance guard, the measured
incidence is **17.2 %** of eligible sites, **8.1 %** of the sites that hide a band, and **1.87 %** of every
site the generator makes. All three are read off a 4 000-site sweep and printed by the guard, because a rate
nobody measured is a rate nobody chose (§13.12's lesson).

**The four senses, each a §10 law made physical:**

- **DARK**, and it is the floor's own claim. `DeclaresDarkness` answers true here and nowhere else — §13.18's
  promised one-liner, and its customer. The facility's failing light stops at the poured concrete; past the
  seam the dark is *original*, and the suit's cone is the whole of the seeing.
- **AIR, unexplained.** Every gallery holds pressure, through the ONE pressure fact (§13.13) — which is why
  `HoldsPressure` now takes the BODY, with no level-only overload left: whether a floor breathes stopped being
  arithmetic on a number, and the compiler made all thirty-one callers say which moon they were standing
  under. The gauge reads `PRESSURISED · TANK STOPPED`, and **nothing anywhere shows plant**: `IsPlumbed` is
  that one pressure fact plus one, and it says no, so not one duct, grille, pump, fixture, cell, counter or
  refuge is ever built down there. Both readings survive: passive geology / *the halls have always been
  provisioned*.
- **SMOOTH WALLS — the third idiom.** Owner: *"it is just built into the smooth monolith style walls."* The
  game has two wall materials and both say who built the thing: `IsHull` is poured and takes the DEPARTMENT
  LIVERY as its ink (#605), `IsStone` is the moon's own rock and takes the BODY's colour (#589). Either one
  here would quietly answer the question the feature exists to leave open, so `DeckPlan.Wall.IsSeamless` is
  drawn in **one flat constant that belongs to no palette at all**, heavier than either, with no texture,
  hatching or interior line-work — **the absence of texture IS the style**, which on this crude grid reads
  exactly as wrong as it should. The precedent is §10.4b's slab, which says *no seam* by having nothing drawn
  inside its face. **The word `monolith` never appears in any hall string** (§8 — the word is reserved); the
  player makes the connection the way they make every connection in this game.
- **AND NO DOOR LEAF.** Every doorway in this building is drawn *imported violet* — the one channel that means
  somebody flew a thing here and fitted it (§13.7's material language) — so the galleries cut none at all. The
  wall simply stops at each chamber mouth, and a rib ends in the same material as everything else rather than
  in `⟶ SECTOR 7 · 2.4 km`, which is a survey, a department and a decision about where somebody's authority
  stopped. A captain gets no number to reason with, which is worse, and is the point.

**The geometry inverts the grammar.** The whole game has taught that deeper is tighter, because cost per cubic
metre rises with every metre of overburden and the people paying knew it. Down here it inverts, and the
renderer's **room scale** says so without one word of prose. `FoundGrowthPerFloor` = 1.10, compounding with
depth into the band (1.00, 1.10, 1.21, 1.33), applied to the facility's own room module — which is published
now (`RoomWidthDu`/`RoomHeightDu`) instead of being three `const`s inside two functions, because the doorway a
room cuts and the gap its corridor leaves are the SAME gap (§13.2's family). The deepest gallery has getting
on for twice the floor area of the first and **about half as many chambers on it**. Nothing is typed: the
ratio is capped by the *actual rib spacing of the actual field*, so the growth stops where two facing chambers
would meet rather than at a number somebody guessed.

**What it pays: a measurement, and almost always nothing.** Measured at **11.4 %** of chambers over an
18 000-room sweep — one in nine, against `FoundRecordOneInN`. The rest are the authored empty line. What is
absent is each its own canon rule rather than a balance call: **no equipment** (a crate names a supplier),
**no records and no files on people** (paperwork is an institution, and a file would say who kept it), **no
card past the entry** (a second would make the halls a building with a directory). The record itself is
§13.10's law one class of object further along — what goes in the pocket is the RECORD of a thing that stays —
and the two relic-class objects tell themselves apart by the id the find is minted with (`FindId`,
`IsHallRecord`), so the look-card, the satchel row and the pocket line can never show a photograph of a pallet
to a captain standing in an empty gallery. The card is caption-only in the #528 idiom, and the **casebook keeps
the gist** rather than the sentence (§13.19's rule: looking is free, knowledge is one-shot).

**The odd book does not run here, and the cheat cannot put one there either** (§13.19). A shelf is a FACILITY
object — the engine behind it is a department that reads everything, and a department is staff, a budget, a
requisition and a room somebody was given. A paperback in a gallery would be the most explaining object the
game could put in one: it would say somebody LIVED there, and which century they came from. Guarded in ONE
place (`OddBooks.ShelvesStandHere`), because `Search`'s forced path bypasses `HoldsOne` entirely and the guard
that got missed would be the one a tester typing `?book=6` in a hall walked straight through.

**The prose is the owner's and is lifted verbatim.** Six strings, checked character-for-character, because the
way this feature dies is one helpful sentence written to fill a gap. Where the generator has nothing authored
to say — the room line of a gallery that holds a record — **it says nothing**, on purpose, and the card does
the describing. The facility's own empty-room line (*"Stripped to the fittings. Whoever cleared this room did
it carefully…"*) may never appear down here: it is a sentence about STAFF, and there was no staff. That is why
the halls' two answers are taken BEFORE `HaulLine`'s switch rather than as two more arms inside it — a default
arm is how that sentence would arrive, silently, the day somebody adds a `Haul` value.

**Two sayings, both last.** The seam line on the ride (*"The pour stops. Not at a wall — at a line, clean as a
tide mark…"*) and the arrival line on the first gallery (*"The car has no button for this floor. It stops
anyway. The air is good. Nothing here says why."*), each once per excursion, written after every routine
saying for the reason §13.5's gate line is: the pulse has one slot and the last write wins. **Neither air line
is said past the seam** — the pressurised one describes standing lights, a fan still turning and somebody's
account decades after the last invoice, which is a sentence about PLANT and about the people who were billed
for it. The authored line states the air once and the gauge answers after that. *The residual is #693's and
not this feature's: on the tick after the doors open, a crossing into pressure can still pulse the generic
supply line over the top of these — the same slot §13.7's own climax has been losing since it shipped.*

**`?found=1` is the door.** About one site in fifty has galleries and the way in is a card eleven floors down,
so the cheat parks the one rock whose seeded shape has the full chain in it and hands over every authority the
site ever issued — minted through the real `AuthorityCard`, into the real satchel, so the panel, the gate, the
refusal ladder and the wallet fan behave exactly as they do for somebody who earned them. The rock is a body
ID and nothing else (`UndergroundComplex.FoundBandCheatSiteId`, read by the cheat and by four sweeps that
would otherwise audit a universe with no galleries in it), which is the right shape and also the fragile one,
so it is pinned. *A scene nobody can reach on demand ships broken.*

**And `?floor=N` stopped doing its own arithmetic.** It used to clamp to the true bottom and then snap into
the unlisted band's shaft head — correct for a building with ONE gap in it, and a captain set down in solid
rock the day there were two. `NearestFloorTo` is the building's own answer now. This is §13.15's second cause
for the third time.

**Two things this v1 deliberately does NOT build**, both filed and neither snuck in: the **disclosure clock**
(what a captain is shown riding the slow world-side windows, so what you find is a fact about *when you went*)
and any **inhabitant** content. §13.19's judgement call applies again — a feature that ships its own second
half unasked is a feature nobody reviewed.

*(Enforced. Core: `TheFoundBandTests` — the rate measured off a 4 000-site sweep and proved strictly rarer
than §13.7's; the whole band of nothing, with every floor of it proved absent, unauthorisable and unofferable;
the pallet proved to stay in the building that paid for it; the way down designated, colliding with neither
other designation, and proved to mint the card for the shaft two bands under it; the panel's silence and its
carded row; darkness as an equivalence over every floor; the air held and the plumbing refused, driven through
the real `SuitAir` predicate; no plate, no livery, no lock, no stencil, no imported leaf — and still four
chambers and every wall inside the field; the room scale proved derived and the drawn floors proved to have
bigger-and-fewer chambers; the haul measured; the record's pocket line, caption-only card and casebook gist;
the odd book refused for the roll AND for all eleven cheat values over 800 chambers; the canon grep over
forty-six forbidden words and 200-plus strings; the facility's stripped line proved absent for EVERY `Haul`
value the enum has; the six authored strings verbatim; and the cheat rock pinned. Client:
`TheHallsAreDrawnInAThirdIdiomTests` drives the real `DeckView` through a recording pen — every wall past the
seam inked in the constant, none above it, the livery proved unable to reach them, and no door leaf, plate or
livery on the deck at all. `YouCanWalkTheHiveTests` floods the galleries under §13.1 like any other floor,
because `FloorsOf` carries them and the cheat rock is in its body list.*

*Watched go **RED**, ten reverts, each against the behaviour it replaced: darkness back to §13.18's shipped
`false` → `4 floor(s) disagree with the seam: … B17 (a gallery, and it is LIT)`; plumbing following pressure
again → `probe-moon-116 B17: a gallery with plumbing in it`; the shelf guard removed → the book turns up in a
gallery; the halls falling through to the facility's arm → `Expected: Not "🚪 Stripped to the fittings…"`; the
scale flattened → `Expected: 1.331, Actual: 1`; the pallet back on `TrueDepthOf` → `Expected: -12, Actual:
-20`, in two files; the card back on `BandOf(level) + 1` → `probe-moon-116 B9: the designated way down minted
no card — the halls are unreachable`; the find id forgetting its prefix → the wall's record answers with the
pallet's photograph; the reserved word transcribed into the empty line → `Found: "monolith" in: …`; and the
wall flag plumbed and honoured by nobody → `secret-lab-site-halls-116 B17: 120 of 120 wall(s) drawn in the
facility's material`, with `0 seamless stroke(s) drawn for 120 seamless wall(s)`.)*

13.21 **There are people in the bar, they are all outsiders, and they stop at B1** (#709, v1 of the social
layer).

> *"we should have people in the bar... we have cover story"* · *"for now let's keep the people in B1."*

The Hive's first people. #707 built the rooms and explicitly deferred the cast; this is the cast.

**They are in the upper canteen because that room's own sign already said who sits in it.** #707 stencilled
`CANTEEN 1 · CARRIERS & CONTRACTORS · NO PASS REQUIRED` before anyone had thought about the people, and it
turned out to have decided them: hauliers, fitters, agency temps, drivers — **outsiders with no more right to
be in the building than the captain has.** That is why nobody is asked for a card at band 0, and it is what
makes the job-seeker cover (#618) work: not because it is a good lie, but because *everyone else's is equally
thin*.

**B1-only is the design, not a limitation.** Staff on every floor would spend the abandoned tone, which is
load-bearing. Confining them to the top pressurised floor buys three things at once:

- **The cover acquires a natural expiry.** It holds exactly as far as the floor where an outsider plausibly
  belongs — the world's own shape answering *"what blows the cover"* instead of a rule we invented.
- **Descent becomes the horror gradient.** Each floor down is quieter and the last person you saw is further
  behind you. Corridor length was never going to do that; a population falling to zero does it for free.
- **The empty floors below read as ABSENCE**, not as unfinished content. Once a captain has seen this
  building with people in it, B7 is a floor somebody left.

**Nobody is interesting, on purpose.** The register test from §13.19, one door along: they are tired, owed
money, waiting on somebody else's paperwork, or eating. A regular who was mysterious would be a quest-giver
in a hat, and the room would stop being cover and become a corridor with clues in it. One breath each, filed
once, plate pulsed thereafter — and **no menu, no trade, no round to buy**. A room that starts offering things
is a room that is paying attention to you.

**No exposure cost here, deliberately.** #618 rules that *talking is what blows a cover* — but that is the
guards on the bottom floors, who are not built yet and whose whole point is that probing them probes back.
Wiring a cost into this conversation would pre-empt a ruling not yet made and would make the one safe room in
the building unsafe.

**Canon holds hardest here**, because this is now the only room in the Hive where somebody can talk (§13.8).
The closest any line comes is a remark about hiring that becomes horrifying only if the player has assembled
something the game never states — and the game never states it.

*(Enforced: `TheCanteenRegularsTests` — nobody outside the upper canteen on the top pressurised floor over
72 sites; never empty, never a crowd; no shared chair and nobody in the room twice; deterministic; the canon
grep walks the catalog itself so a line added tomorrow is checked tomorrow. Client:
`ThereArePeopleInTheBarTests` counts people on the **real deck**, pins every seat against Core's own
coordinates, and proves nobody is sitting in the lift car.*
*
**And the lesson this one cost.** The first sweep passed with **both** Core guards deleted — a washroom has no
tables and the staff mess is never on the top floor, so the two guards masked each other and no floor the
generator builds could tell pass from fail. That is the fifth class exactly: the assertions were right and the
world could not distinguish. The fix is `TheTwoLawsHoldEvenWHERETHEGENERATORCANNOTPUTTHEMYET`, which **forces**
the conditions with a synthetic `Amenity` rather than hunting for them — and both laws then went red
independently, at `Collection: [Seated { … ◈ A HAND WHO HAS BEEN HERE LONGER THAN THE CONTRACT SAID … }]`
sitting in the staff mess, and again on a canteen carved below B1. The client wiring was watched go red at
`10 floor(s) disagree with the B1 ruling: luna B1: the canteen floor is deserted`.)*

13.22 **The board on the canteen wall, and the person whose notice it is** (#709).

> *"let's add a bulletin board to the bar"* · *"maybe spot the person notifying in the bar."*

The second sentence is the feature. **Every notice on the cork is pinned by somebody sitting in that room**,
and nothing in the game ever says so:

| on the board | at a table |
| --- | --- |
| `PUMP 2 — written up 12/4, 19/4, 2/5, 16/5. Still listed OPEN.` | the fitter: *"it's been singing since spring"* |
| `COUNTERSIGNATURES — the signatory is away. No date has been given to us either.` | the carrier: *"third day sat here"* |
| `ROTA WEEK 31 — disregard the first name and use the second.` | the temp: *"they put a different one on the rota"* |
| `STORES — no soil samplers in stock. Do not query the description on your docket.` | the woman doing invoices: *"my pallet jack is a soil sampler"* |

**The pairing is data, not dialogue.** `Notice.Pairs` is never rendered, never spoken and never filed — it
exists so the room is *internally true*, and the player either notices or does not. Same register as the odd
books and the funding trail: **consistency is offered and the answer never is.** Which means the only thing
holding the feature up is that the authoring is actually consistent — so that is what the guard checks, and
it is the one guard here that matters.

**It makes the cover story into an object.** *"They're always hiring — nobody ever says what for"* stops being
a line a stranger says and becomes `HIRING — GENERAL HANDS. No experience necessary. Weekly pay, cleared
weekly. Apply at the desk. Bring nothing.` #618's job-seeker cover now has a notice on a wall behind it.

**One notice per press, round and round**, rather than a card with four on it. The board's whole value is that
a captain comes *back* to it: the pump notice means nothing until you have heard the fitter, and everything
afterwards. A single dump would be read once and never looked at again.

**The text stands alone without art.** Generated notices (owner: *"gen AI advertisements of jobs and missing
things"*) go on top as an enhancement — because a board that is blank when an asset is missing is a board that
breaks the first time a manifest drifts.

**Core owns where it hangs.** Offsets chosen against the counter's line (`cy + 3.6`) and the tables at the
front, so the board owns its own patch of floor to be pressed from — a renderer picking a spot on a wall would
be doing geometry about a room it does not own (§13.15).

*(Enforced: `TheBoardInTheBarTests` — every notice pairs with a member of the cast and every one of the cast
has exactly one notice; **no notice ever names its author**; the B1 law forced with a synthetic `Amenity`, not
hunted for; four different notices per site, the same four every visit; the 2 du console clearance checked
against the fixture console *and* every seated person; the canon grep. Client:
`ThereArePeopleInTheBarTests.TheBoardHangsOnTheSameOneFloorAndCoreChoseTheSpot` proves the renderer takes
Core's coordinates unmodified. The pairing guard was watched go **red** at
`Not found: "◈ A PERSON WHO IS NOT IN THE ROOM"` — a notice pinned by somebody who is not in the building.)*

13.23 **The shovel is something the GROUND has, not something the key does** (#723).

> *"The shovel rings off bedrock a foot down — too hard to dig here. Try another square."* — read on B1 ·
> ADMINISTRATION, pressurised, 150 m under the regolith, at the canteen's west face.

Bare-handed `E` on a Hive corridor ran the beach-comber probe: the animation, the dug-square mark on the
rockcrete, and that sentence. **Both halves lie.** There is no bedrock under a floor somebody invoiced — and
*"try another square"* is an **invitation**, telling the captain that some square down here *does* dig when
nothing was ever buried on any square of any corridor of the building. The kit's own first-time card had
already drawn the line: the shovel is for *"out on the open regolith."*

**Why it happened is the thing that makes the Hive cheap.** A floor reuses the surface's own coordinate
envelope — *it is not beside the field, it is under it* — so the spine corridor's (x, y) is also a perfectly
good square of open regolith, and a diggability test made of `x` and `y` alone had no way to tell them apart.
`IsDiggableGround` takes the **floor** now: a spot is a place on a level, never a bare pair of numbers.

**Gated on the ground, not on the keypress** (the owner's option A over the cheaper "fix the sentence"). The
verb does not exist underground, so nothing enters the shovel path at all: no probe, no mark, no line. Empty
hands and nothing in reach falls through to the same honest nothing `E` gives on any other deck, and the
too-hard line stays exactly where bedrock genuinely is. One fact — `MoonSurface.ShovelWorksOnThisFloor`, which
asks the level and nothing else — answers the `[E]` handler, the standing prompt, the key bar (which had been
shouting *"⛏ E — BURY THE CHEST HERE"* over a corridor) and the tracker caption that taught the press in the
first place, so none of the four can drift out of step with the other three. Same shape as §13.6: underground
is a fact the instrument asks about, not a mode somebody remembers to check.

*(Enforced: `TheShovelStaysOnTheSurfaceTests` — every spot the captain presses `E` on, on every floor of every
clandestine site in the system, plus a full standable flood of the report's own pressurised B1. Each test
carries its **control**: the same coordinates asked at level 0, where they must dig. Without it the guard
would pass just as happily on a world whose corridors had wandered outside the field rim and were being
refused for the wrong reason — the fifth bug class, a guard handed a world that cannot tell pass from fail.
Watched go **red** on the old behaviour at `18747 of 19311 squares on a pressurised poured floor take a
shovel`.)*

13.24 **An arrival composes its sayings with a RANK, and the one pulse slot keeps the biggest** (#693).

> *"#592's `UnlistedArrivalLine` — the CLIMAX of #592, the first words on the floor that does not exist — is
> overwritten by the routine pressurisation/air line on the same arrival. The biggest sentence in the feature
> has been losing to boilerplate since it shipped."* — owner, filing #693

The HUD's pulse line has exactly **one** slot and the last write won. Stepping out of the car can have five
things to say at once — the car dropped, the plan has no such floor, the air is good or gone, a gate read a
paper, the pour stopped — so *which one a player actually read* was decided by the order three blocks in
`Map.Surface.RideTheLiftTo` happened to be written in. Three of them carried a comment saying they were
deliberately **last**; #592's climax did not, and lost to *"the doors part on warm air and standing lights"*
every single time. #689's beat had already been shipped with an ordering test (#692) for the same reason,
which is what a missing law looks like from the inside: **a contract that lives in comments is not one.**

The law is two pieces:

- **`PulseRank`** — `Status` (instruments, prices, refusals, weather — the default, and what everything
  written before #693 is), `Beat` (something happened once and the book will keep it), `Climax` (the sentence
  a whole feature was built to say). The rank is about what a line **is**, never about how loud it is; a
  status line dressed as a climax to make it win is the same bug with better manners.
- **`PulseSlot`** — *a lower-ranked line may not displace a higher-ranked one that is still held; among
  equals the last written wins.* The **hold is short on purpose**: `MinDwellMs`, the pulse's own floor, and
  not the winner's full dwell. A climax can dwell eight seconds, and eight seconds in which a pressed button
  answers nothing on screen is §13.10's *"in the DOM is not on the screen"* wearing this fix as a disguise.
  The lines that race for this slot race in the same frame or the tick after it.

And the arrival is **composed once, in Core** (`UndergroundComplex.ArrivalSayings`), returning the ranked
sayings in narrative order with the beat each one is. The client says all of them — **the book keeps every
one, in the order they were said, whatever the screen does** — and hangs the cards, the nerve, the flags and
the save off the beat. No call site has to know what the call sites after it are going to say, which is the
whole point: the per-site ordering discipline #692 had to invent is gone, and its ordering test is now a rank
test.

*(Enforced: `ThePulseKeepsTheBiggestSentenceTests` sweeps **every floor arrival the generator admits** — every
site, every from-floor, with the wallet and without it — and asserts the highest-ranked saying is the one on
screen; plus a **permutation** guard, because the claim of a priority is that composing order is free, and
"a list built by appending is not a list in order" (§13.2) is this repo's own named bug class. Watched go
**red** on the old last-write-wins rule at `144 of 1319 arrivals put the wrong sentence on screen` and
`europa B5: the floor that is not on the plan says '🕳 The doors part on a floor that is not on the …' and the
screen kept '🎫 You find the other shaft…' instead`.)*

**Reaching it.** `?card=next` mints the authority for the gate you will be standing at, so the carded row,
the accepted beat and the wrong-card refusal are one URL away — #692 shipped all three and could not look at
any of them. See `docs/testing-guide.md`.

13.25 **An arrival that raises a CARD holds its sayings, and the card's dismissal plays the winner** (#768).

> *"On a from-the-surface ride straight through a gate, #585's first-descent CARD raises on the same arrival,
> and the gate-accepted pulse line plays UNDER its backdrop."* — the residual #693 declined, filed as #768

§13.24 settles a pulse losing to a pulse. It cannot settle the other loss, and the other loss is total: the
same arrival that composes the sayings also raises a **full-screen card**, so the winner of the one slot spends
its whole dwell behind a blurred backdrop and there is nothing left when the captain closes it. No rank helps —
the line is not losing to a bigger line, it is losing to the **whole HUD**. §13.10's family, arising from the
world acting rather than from a press on a pop-up, which is why #736's *"answer on the pop-up that was
pressed"* sweep could not reach it either: nothing was pressed.

**The shape: a hold, scoped to exactly this situation.** `PulseHold` (Core) keeps one sentence back —

- **the same law, minus the clock.** *A lower-ranked held line may not displace a higher-ranked one; among
  equals the last held wins.* There is no clock inside a breath: an arrival composes its sayings in one frame,
  so the rank is the whole of it. What survives the card is therefore **exactly the sentence that would have
  been on screen had no card been raised**, and that equivalence — hold-then-release against write-them-all —
  is what the sweep asserts, so there is one law about the slot and not two.
- **a release, not a queue.** #693 declined a queue and it stays declined: this is not a lifecycle the pulse's
  400-odd call sites share. An event that raises no card releases on the spot, which is an ordinary pulse and
  indistinguishable from one. The released line goes through `PulseSlot.Write`, so it takes its ordinary
  length-scaled dwell and can be outranked a breath later like anything else — **a held line is a line that
  has not been said yet, never a line with special powers.**
- **the book is untouched.** Every saying is filed at the moment it was said, in the order it was said. What
  is deferred is the doorbell, never the record and never the event.

The client half is three clauses: `RideTheLiftTo` holds instead of pulsing; it releases **after** the last card
it can raise, because that is the only point at which "is something in front of the captain?" can be asked of
the world rather than predicted from a copy of the conditions; and `CloseViewObject` / `CloseRevealCard` free
what the card was standing on. Every road out of a card — Esc, Enter, `E` again, the backdrop, the button —
already went through those two methods, and now nothing anywhere clears the field by hand.

Two more members of the family went the same way: the repo boat's arrival line and callsign behind its
arrival plate, and the *shelter is not a sanctuary* warning behind the siege plate (#583) — the one sentence
in that scene that tells a captain the pressure vessel they are standing in will not save them.

*(Enforced: `TheHeldSayingOutlivesTheCardTests` (Core) sweeps every from-the-surface ride that crosses a gate,
plus every arrival the generator admits for the hold/pulse equivalence and a permutation guard, and pins the
released line's dwell against §13.24's. `TheArrivalHoldsItsLineForTheCardTests` (Client) is the source-shape
half, including **a method that holds always releases** — a hold with no release is a sentence lost for good,
which is a worse bug than the one this fixes. Watched go **red**: with the hold made a no-op, i.e. the shipped
behaviour, `10 of 10 carded descents lost their beat to their own card` and `1319 arrival(s) hold a different
sentence than they would have pulsed`; with only the rank clause taken out, `144 arrival(s)`; with the client
files reverted, all six wiring guards.)*

**Reaching it.** `/map?secretlab=deep&land=1&card=next` — the shed, with the first gate's authority already in
the wallet. Ride straight down from daylight and the card and the beat arrive together.

13.26 **An event with FOUR things to say composes them onto its own card** (#774).

> *"`AssembleSomebody` raises the dossier card and then fires 2–4 `ShowAndFile` lines UNDER it… the hold is
> the WRONG remedy here — it releases one winner, and these four are a same-rank sequence whose survivor
> would be decided by append order, the exact contract #693 killed."* — the owner, filing #774 off the
> #768/#773 crew's verification

§13.25 settles a card standing on ONE sentence. The field dossier (#588) stands on four: the person the kit
assembles into, the next of kin who have been waiting nine years, what that family turns out to know, and
the phrase that opens a door somewhere else — all composed in one breath, all at the same rank, every one of
them pulsed under the card's own backdrop and filed where nobody was looking. A fifth is possible: the moon
the family's knowledge names (#585), announced from inside the same method with the card already up.

**Why the hold cannot help, written down so nobody re-proposes it.** A hold releases ONE winner. Among equals
the winner is the last one held, which means the sentence the captain keeps is decided by the order somebody
typed the calls in — §13.2's own named bug class, and the contract §13.24 was written to kill. The hold is
right for an arrival, which composes sayings the world happened to produce at the same instant; it is wrong
for a DEBRIEF, which is one thing said in four sentences.

**The shape.** #736's law instead — *the result of an act is readable on the pop-up the act raised* — with
the one change that makes it fit: the card's outcome is a **region, not a slot**. All four fit, so there is
no winner to pick, and the ordering question stops being about survival and becomes a question about reading.

- **The sentences and their order live in Core**, beside the rolls that decide whether each exists at all
  (#634's law: a sentence composed in the client can drift away from the sim). `FieldDossier.Beat` declares
  the order — *who they were → who is waiting → what that family knows → the in* — and it is an order of
  MEANING: you work out who before you can carry news of them, you learn who is waiting before you can learn
  what they know, and the in comes last because it is the only one that is not about the dead.
- **Nothing may read that order off the order the sayings were composed in.** `Debrief` composes them
  backwards on purpose and `InTheOrderTheyAreRead` walks the enum, so append order cannot decide anything
  even by accident.
- **The book is untouched.** Same sentences, same glyphs, same order, filed at the same moments — including
  the named moon, which is still banked between the family's hint and the in. What changed is where a
  sentence is READ, never what is recorded.
- **The object card grew an outcome row**, the reveal card's own (#736), because the surface's full-screen
  cards are `ConsoleSpot`s. `SayItWhereTheyAreLooking` now knows about it — and answers there FIRST, because
  both cards share a backdrop class and the object card is written later in `Map.razor`, which is the case
  the outpost's effects console reaches for real: one press raises the effects plate and the dossier over it.

*(Enforced: `TheDossierIsReadOnItsOwnCardTests` (Core) sweeps 360 assemblies over six grounds — every
sentence on the card, the reading order strictly ascending, every permutation of a real assembly's sayings
coming back identical, and the filing rebuilt longhand from the primitives the shipped code called.
Anti-vacuous: the sweep is asserted to contain assemblies of two, three AND four sentences.
`TheDossierCardCarriesItsOwnSayingsTests` (Client) is the source-shape half. Watched go **red**: with the
card carrying nothing — i.e. the shipped behaviour — `960 of 960 sentence(s) the assembly says are not on its
card`; with the ordering rule reverted to the list as it came, `1908 composition order(s) changed what the
card reads` and five of eight failing; with the four client files reverted, all seven wiring guards.)*

**Reaching it.** `/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1` — the hut on the edge lane, and the
dossier assembles on the first piece of kit with every sentence it can carry. Three papers rooms in one
excursion at one room in eight, times two one-in-three rolls, is why it needed a door.

13.27 **Somebody walks the working floors, the walk is a thing you can learn, and being seen is a CONVERSATION**
(#804, v1 of the guards).

> *"the rotating guards on the lower more restricted levels… ideally we could see them move and wait for them
> to pass before we pass them."* · *"Surely we should not know their movements like 100 meters out and them
> need to see us like really close to register our existence."* · *"a rolling guard has no reason to run after
> anyone just on sight, they must suspect you do not belong there for some reason first."*

§13.21 put people on B1 and said, in the same breath, that the guards on the floors below *"are not built yet
and whose whole point is that probing them probes back."* This is that cast, and the owner's three sentences
are the whole of its design.

**Where a round is walked, and where it deliberately is not.** Below the bar, and no deeper than the building
admits to — `PatrolBeat.IsPatrolled` is `CanteenRegulars`'s own B1 ruling on one side and `DepthOf` on the
other, so the floors that carry a payroll are exactly the floors the directory owns up to. **Nobody walks the
unlisted band or the found halls**, and that is a fact rather than an omission: the unlisted band is what the
clandestine operation was hiding *from its own staff* (§13.7), and a security rota down there would be the
building telling on itself. The head office has none either (#411 has no gate, no shafts and a fiction of its
own). The population going to zero on the way down is #709's horror gradient, untouched — what this adds is a
second, narrower gradient inside it, and **where the rounds stop is a sentence a captain can read without one
being written.**

**The round comes off the floor plan and never off a constant.** The car, then every rib mouth in ascending x,
and after each mouth the room that stands furthest down that rib — `ShaftAt`, `FloorPlan.Ribs` (published by
#587 for precisely this) and `FloorPlan.RoomCentres`, which §13.1's sweep already proves are walkable from the
car on every floor of every site. So a beat is walkable **by construction**, and there is an audit that walks
it anyway. The mouths are **sorted** before the round is built: #587's lesson is that a builder whose
correctness depends on order must sort at the point of use, and the order here is what makes a round learnable.

**It rotates with the WATCH, and the watch is the canteen's.** Direction and starting stop are seeded on
(site, floor, watch) off `PatronRota.WatchIndex` — the same shift the bar upstairs turns over on, because one
answer to *"how long is a shift"* is a rule and two are a bug. Inside a watch the same round is walked the same
way every time, which is what makes it learnable; across watches it turns over, which is what stops learning
one from being the end of the feature. One or two on a floor, sharing **one route** and differing only by how
far round it they start — the sweep team's rule (#538), for its reason: *being hidden from has to be legible.*

**Four bands of knowing, and the asymmetry is the feature.**

| range | what the captain gets |
| --- | --- |
| out past the eye | the MOTION FAN hears them through the rock, smudged, at #591's degraded reach |
| inside earshot, unseen | *boots on shotcrete, out of sight and in no hurry* |
| inside sightline (30 du) | a green mark on the deck — **and not one metre before** |
| inside NINE du of them | they register you, and the round stops |

The gap between the third row and the fourth is the whole stealth verb. If a guard noticed you the moment you
could see them there would be nothing to time, and *"wait for them to pass"* would not be a sentence anybody
could act on. One predicate answers both directions with a different reach (`PatrolBeat.EyesOn`), over
`SurfaceCollision.HasLineOfSight` — the same wall law the captain, the pack and the sweep team obey — because
two copies of "can this see that" is how a marker and a challenge come to disagree about whether anybody is
there. **A guard behind poured wall is not drawn dim; they are not on the deck at all**, the #371 idiom the Old
Ones already take.

**And the instrument needed nothing.** A guard walks, so a guard is a contact: they go into
`EverythingThatMoves` and the fan hears them, degrades with depth, smudges them behind wall and drops them the
moment they stand still at a stop. #591 shipped an honest underground tracker eighteen months before there was
anything down here to hear, and the bet paid: this feature added **no instrument code at all**.

**A sighting is a CHALLENGE and there is no other branch.** The round stops, the wallet is read without being
asked (#684's ruling — *the unprompted read IS the character*), and the answer is TOLD on a card with the
consequence in its own amber row (#736). Four rungs, because a refusal that sorts the wallet is the best
storytelling this ground has (#679): this site's pass, another site's pass **named**, the cage chit refused as
a cage chit — *"That's for the cage. This isn't the cage."* — and an empty wallet. The worst outcome in the
feature is a walk back to the lift and a line in a book with the time on it. **Nothing here can start a
chase**, and it is enforced by there being nowhere for one to start.

**THE BADGE, and where it comes from.** A site-scoped personnel pass in the wallet, the third grammar there:
an authority is an office vouching for a HOLE, a chit is a foreman vouching for a SHIFT, a badge is the site
vouching for a PERSON — and only the third is any use to a man on a round. It is issued **when the cage takes
you down on the day-labour chit** (#752's arrival), which is the gig *completing* rather than being offered:
the Hand's chit is a promise, going down on it is the shift you turned up for, and the field book's existing
sentence — *"Downstairs is a place you are now paid to be"* — becomes literally true without a word of it
changing.

**Register, and it is load-bearing.** A guard is an EMPLOYEE. Bored, thorough, on a rota, halfway through a
shift, and wholly uninterested in the captain until the form says otherwise. #618's canon constraint is the
one this feature could most easily have broken — *"the owner's ask is a cover that can blow, not a detection
meter"* — so there is no alert, no alarm, no lockdown, no meter and no banner, and a word-list guard keeps the
next line in the same register.

*(Enforced: `TheRoundsOnTheRestrictedFloorsTests` (Core, 22 facts) — the three exclusions swept over thirteen
sites including the head office, both head-counts proven reachable, the circuit's order and the car's place,
no two stops on one square, deterministic inside a watch and turned over on 90 %+ of floors across six,
the sightline laws in both directions at ONE range with only the wall differing, the boots band, the fan
hearing a walker the eye cannot and dropping a stander, all four challenge rungs, the pass's site scope and
its ride in the wallet, and the canon + register greps walking the catalog itself.
`TheRoundIsWalkableTests` (Client) floods every restricted floor of eleven sites on three watches with A\*
over the real `DeckPlan.CollisionField` — every stop standable, reachable from the car, and connected to the
next — plus the wiring guards for the sightline gate, the fan's one accessor, the deck's droid count and the
pass's grant.*
*
*Watched go **red**: the rib order reversed → `secret-lab-site-halls-116 B4: the round goes back on itself — a
mouth at x-5.0 after one at x93.4`, twelve times; the wall law removed from `EyesOn` → three sightline facts;
the watch dropped from the beat seeds → `only 0 of 98 floors walk a different round on a different watch`;
the `DepthOf` clause dropped → `europa B5: a round on the band nobody listed`; and the room stop moved twenty
du off its centre → `996 leg(s) of a round are not walkable`.)*

**Reaching it.** `/map?patrol=2` — B2 of a deep site with the two-guard watch forced; `/map?badge=1` — the
same floor with the site's own pass already in the wallet.

**Filed, not built** (the owner's own scope split, and the phase-2 note this leaves behind): the suspicion
ladder past one sighting, false IDs, a department tier on the pass, the challenge growing MOVES on
`Encounter.Scene` — which #746 already proved needs no new mechanics — and #715's per-entity heat taking over
from the single filed line. And the card wants a painting; it ships caption-only, which is the house
degradation law and not a hole.

**And the loudest one, which now has both halves waiting for each other.** #803/#809 shipped
`GunfireHeard.WithinEarshot`, whose own doc comment says it is *"the question #804 will ask of every patrol on
the floor, written down here so it is asked once and answered the same way"* — one call, and the predicate is
already the ground's own ear (`ReeverHearing.Noise.Gunfire`, never a second number). **It is deliberately not
consumed here.** What a round DOES about a noise is not a predicate, it is a STATE — break off the stand, walk
to the place, search it, resume — which is the first rung of the suspicion ladder this phase is scoped out of,
and it wants a sentence in the guard's own register that has not been through canon review. The seam is ready
and the caller is one `if`; the reason it is empty is a ruling rather than an oversight.
13.28 **The park is not the edge of the map: the far wall is a row of doors** (#801).

> *"we could have rooms to explore below the park also (on the map). Walking through the park is fun, it
> should not be the edge."* — the owner, 2026-08-09

§13's park (#759) is the biggest room in the game and #775 made it a **thoroughfare** — 2–5 gates in its near
wall, so the natural route between two corridors crosses the green. It still had a painted horizon down one
whole side, and a crossing that ends at a horizon is a crossing you do once.

**The back of house.** Behind the far wall, one room in each **bay between two floodlight masts** — four on
the shipped field, ~46 × 12 du each, entered directly off the gravel. Their plates say what the KITCHEN and
the GROUNDS are for and never what the building is for (§13.8): `🌱 POTTING · SOIL, TRAYS, GRIT`,
`🧰 GROUNDS PLANT · LAMPS, FEED, TIMERS`, `❄ COLD ROOM · TO CANTEEN 1`,
`🧤 GROUNDS STORE · TOOLS SIGNED OUT AND BACK`, `🚿 WASH-DOWN`, `📋 GROUNDS OFFICE · ROTA POSTED`. The cold
room names the same `CANTEEN 1` the beds are stencilled for, which is the entire food connection said a third
time and never once pointed out.

**Where the ground came from, because it is not obvious and it is the whole engineering answer.** The park's
size is a LAW — `ParkDepthDu` deep, and half again the floor of the hall behind it — and on the shipped field
the second of those binds at 38.3 du of the 42 it has. There is 3.7 du of slack in the entire feature and a
chamber module is twelve, so the band could **not** be bought by making the park shallower. It is bought from
the **last strip of the field**: the park's far wall clamps at `BottomY + EdgeMargin`, and the edge margin is
a *surface* law — the half-lane the regolith generator keeps clear of the #563 falloff. There is no falloff on
a floor with a roof on it (a Hive deck publishes no `Unseen` wall at all, so `DrawUnseenFalloff`
short-circuits and nothing is clipped). That band is 16.5 du of the field's own envelope no floor has ever
used. The back wall stands `ParkBackRockDu` inside `BottomY`, and the one law that *does* bind —
`ItNEVERLeavesTheSurfacesOwnEnvelope` — is met with rock to spare.

**Three consequences that had to be got right.**

- **The far wall is built in segments now**, exactly the way the near wall has been since #775, off the one
  list of spans a cursor sweeps in order (§13.2). The gap the wall leaves and the door the room publishes are
  one gap (§13.1's founding law).
- **The doors are laid in the bays BETWEEN the masts**, off `ParkMastXs` — one answer, asked by the carve
  that erects the masts and by the carve that lays the rooms — so a door can never be cut in front of a lamp
  post.
- **They are not `Park.Ways`.** A Way is a way *through* the green, corridor to corridor, and
  `TheHiveAmenitiesTests`' conservation sum counts each one as a place with a doorway. A back door is a way
  *out of* the green into a room that is already counted; publishing it on `Ways` would have taken that guard
  red for a reason that has nothing to do with the bug it was written for. They are published as
  `Park.BackDoors`, and each room carries its own.

*(Enforced: `TheParkIsNotTheEdgeTests` (Core) sweeps 52 parks — every room on the far side, every door on the
far line, nothing lying across one, every room on the floor's own `RoomCentres`, every plate on a wall, the
park's own depth untouched to three decimals, and the rock left over. `TheFarGatesLeadSomewhereTests`
(Client) floods the real `DeckPlan.CollisionField` from the car to every one of them and then **pours every
back door shut** and demands they all go dark, with the green itself still reachable so the plug is proved to
have measured the door. Watched go red three ways: with the carve removed, `52 park(s) are still the edge of
the map: luna B1: the far wall has nothing behind it — the park is where the building stops`; with the rooms
carved and the far wall left poured as one segment, Core says `208 park(s) are still the edge of the map:
luna B1: a wall (-144.0…134.0) lies across the back door at -88.6…-82.2` and the Client says `48 room(s)
behind the green are drawn and cannot be entered: luna B1: 📋 GROUNDS OFFICE · ROTA POSTED at (-85, -270) —
the door is a picture`; and with the far wall deleted entirely, `48 room(s) … is STILL reachable with its
door poured shut — the far wall is a picture, not a wall`.)*

**Reaching it.** `/map?parkback=1` — B1, standing on the gravel facing the doors.

13.29 **No floor has exactly one way off it: the building has two cars** (#801).

> *"that elevator would be so busy it would be packed and never available… it is a choke point, and the whole
> lab would be too easily guarded by just having the guard posted in front of the one elevator. I want to
> remove that too-easy plot-to-catch-us plot hole."* — the owner, 2026-08-09

He is right three times and the third is the one that matters. **Traffic**: a facility with a canteen for
eighty, twelve growing beds and a goods hoist does not run on one personnel car. **Pacing**: one car is a
come-back-here point on every floor. **The posted guard**: a single car is a single square somebody stands
on, and no amount of writing around that makes an escape feel earned.

**The shape.** `ShaftAt` is THE CAGE — the one the surface hut (#606) sits over, the one the plate stack is
beside, the one every older law means by "the lift". Beside it now, published from `ShaftsOn(field)`, is the
**goods car**: same spine, opposite face, at the **blind end of the main corridor** — the one stretch of
spine past the outermost cross corridor's own chambers, which is ground no room can ever be laid in and
exactly where a goods lift goes in a building anybody has ever worked in. On the shipped field that is 170 du
from the cage against a law of 92.7 (a third of the corridor), so **one person cannot watch both**.

**What it does NOT do, and this is the load-bearing half.** The goods car has **no SURFACE row** (there is
one hut on the regolith and it is over the cage) and **no gate row at all**. §13.5 is a law about the
BUILDING — depth past the first band is earned with paper — and a second car that could cross a band seam
would be a way to buy it without. Which also makes the pair worth walking between rather than
interchangeable: within a band either car will do; the moment you want the surface or anything deeper you
want the cage, and the cage is at the other end of the corridor. That is route planning, and it costs nothing
to state.

It is **not** #719's executive lift (that hangs off a principal apartment, is on no panel, and costs your
cover) and **not** #719's service stair. It is the ordinary second car a building this size has, and it ships
first because the other two are beats and this is a topology.

**The ground gets a veto.** `ServiceShaftAt` returns null on a field whose cross corridors run out to its own
end caps, and the choke law binds *where the generator admits two* and says nothing where it does not — which
is what keeps it a law rather than a tautology, and is asserted against a synthetic cramped field.

**Every singular guard grew rather than moved.** `TheLiftIsInTheSAMEPlaceOnEveryFloor`,
`EveryMouthCutInTheSpineIsSTILLOpenWhenTheWallIsFinished` (which had seeded its mouth list with a hard-coded
singleton — a second alcove walled over on the lower face is #587 one face down and would never have been
looked at), `NoRibIsRunThroughTheLiftShaft`, `ARefugeIsNeverBesideTheLift`, the hall-swallowed-the-lift
clause, the nobody-sits-in-the-car clause, and `ThereIsAlwaysAWayBackToTheLift` — which said *"exactly one
lift"*, i.e. it asserted the choke, and now says **one cage, one goods car, both always findable**.

**And one of them found a real bug rather than needing a widening.** `ARefugeIsNeverBesideTheLift` measured
the cage alone; widening it to both cars took **332 of 1130 floors** red, because #608's carve had been
choosing the room furthest from ONE shaft. The carve was fixed rather than the guard: it measures every car,
and where a tight floor has nowhere `MinRefugeDetourDu` from both it takes the **furthest** room it has
instead of a rolled one (which alone moved the worst case from 29 du to 52). The law is restated as a
comparison — *the refuge must be the furthest-from-both room the floor had, and must clear the number
wherever any room does* — with the fallback counted, capped at a twentieth of the floors that clear it
outright, and floored at 0.7 of the law, so the escape hatch is measured rather than assumed. Watched go red
by reverting the carve to one shaft: `319 of 1130 floor(s) break the law … luna B2: the refuge is 29 du from
the nearest car and this floor had a room 79 du out`.

**#707's rank-in-plumbing law was made honest in passing.** A principal room whose ground was already claimed
cannot be given an en-suite, and it used to keep a plate stating a rank the building could not back up. The
fallback had never fired on the shipped field — which is exactly why it fired on four generated moons the day
a passage moved four du. Such a room is now re-plated from the floor's own register, one step along the same
seed.

*(Enforced: `TheOtherCarTests` (Core) — every floor of 53 sites has both alcoves cut, the separation and the
opposite-faces law, nothing standing on either car, the ground's veto, and the goods car's panel offering
exactly its band with no surface row, no sealed row and no card named, asked with a wallet holding every card
the site can mint. `TheFarGatesLeadSomewhereTests` (Client) proves each car's doorstep is standable and that
from either one you can WALK to the other, which is the anti-choke claim itself. Watched go red four ways: with the second
alcove never cut, `594 floor(s) can be sealed by one person standing on one square: luna B1: the Service
car's alcove is not cut on this floor`; with the separation law dropped and the car placed at the NEAR blind
end, `the two cars are 92.2 du apart and the law wants 92.7 — one person standing between them has both`;
with the goods car given the cage's panel, `1285 goods-car panel(s) offer something the shaft does not have:
luna B1: the goods car offers B5, which is another band's floor`; and with the doorstep left as the cage's,
`130 floor(s) fail the two-car law: luna B1: riding the Service car puts a captain 170 du from the car they
rode`.)*

**The signage stack stays at the cage, and that is a call rather than an oversight.** §13.13's plate — the
depth, the department, and whether you can breathe — is 44px lettering on the wall a captain steps out onto,
and the second car does not get a copy of it. Two reasons, both already written in this file: #605 deleted a
third plate from that wall because *three plates on one wall is a wall nobody reads*, and §13.16 settled that
**a building says a thing where you ENTER it and nowhere else**. The goods car names itself
(`🛗 GOODS CAR 2 · THIS BAND ONLY`), its panel states the depth in the same `DepthPaint` the plate uses, and
the air fact reaches a captain who arrives by it through the arrival's own sayings and the suit gauge — which
is §13.13's actual law: **one** pressure fact, said by everything that says it. Nothing is left unsaid; it is
said once.

**What this means for §13.27's rounds, said out loud rather than left for somebody to trip over.**
`PatrolBeat.Circuit` opens at *the car* — `ShaftAt` plus a pace — and then walks the rib mouths and their far
rooms. That is still correct: `ShaftAt` is the cage, and the cage's doorstep is still a pace off its own face.
What is now true and was not before is that **the goods car is on nobody's round.** That is the feature
working rather than a hole in the roster — the owner's complaint was precisely that one posted guard has the
building — but it IS a design question the guards lane owns, and this PR does not answer it: whether a round
should take in both cars (and pay for it in length), whether a second guard walks the other end, or whether
the goods car being unwatched is the price the building pays for having one. Nothing here guesses. The back
of house does not enter the question at all: `IsPatrolled` excludes the bar floor, and the park is only ever
on the bar floor.

**Two things this deliberately does NOT do, so nobody has to guess later.**

- **No second SURFACE head.** The issue says "maybe a second surface shaft"; the hut is a seeded
  `SurfaceStructure` with a discovery beat, a probe ping and its own art (#606/#592), and a second one is a
  surface feature rather than a carve. The surface remains one way in — and #719's **service stair** is the
  designated answer to that, which is why it is not pre-empted here.
- **The wrong-shaft-of-this-site refusals do not get a new stage.** A card's identity is `(bodyId, band)`, so
  those lines only fire when the wallet holds a card for a *different band* than the gate wants. Both cars
  serve the same band, so the goods car cannot produce a case the cage does not. Reaching them in play needs
  a car whose band coverage is OFFSET — which moves the gate from the shaft to the band SEAM, i.e. a rewrite
  of §13.5's implementation. That wants an owner ruling, not a carve PR.

**Reaching it.** `/map?goodscar=1` — B1, standing at the second car. Then walk to the other one and see how
far that is.

## Working method

The one that actually found these: **boot every scene and look at it.** Nearly every bug above was invisible
to reasoning and to Core tests, and obvious on sight. When something is found by eye, the fix is not complete
until a guard walks the real object — otherwise it comes back the next time two things have to agree and only
one is changed.
