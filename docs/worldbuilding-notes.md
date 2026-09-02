# Worldbuilding notes — themes waiting for their milestone

*Captured 2026-07-03 from the project owner. Nothing here is scheduled; this is the pantry to
pull from when content is needed — M6 (economy/piracy loop), M7 (Electric Universe layer),
M8 (Wheel of the World scenario), M10 (polish/onboarding text). Plotting mode is the heart of
the game — where space-captains earn their salt — and world content should feed it, not
distract from it.*

## 1. AI compute in space + lunar mass drivers

- Orbital AI compute is a first-class cargo/economy pillar alongside the energy (He3) trade.
- Moon factories launch standardized packages by **mass driver** into transfer orbits (the
  SpaceX-style vision; Zubrin's book didn't have these). Mechanical gift: a mass-driver pod
  has **zero maneuver budget** — its `PathPredictor` cone stays needle-thin
  (`ManeuverBudgetAcceleration = 0`). Pods are the perfect prediction-tutorial targets and the
  pirate's "milk run": trivially interceptable, low-value, high-volume. Escorted or decoy pods
  later raise the skill ceiling.
- Departure boards can list launches (origin: moon factories) exactly like ship departures —
  the M5 traffic-board abstraction already fits.

## 2. Fourth humanity & the ancients in the background

- Premise: this is humanity's fourth run on Earth (Ancient Apocalypse flavor). Not every prior
  civilization vanished — some helped the post-reset survivors, then retreated to the
  background. They still fly **ancient hardware**: pyramid-style AI satellites in odd orbits.
- Game hooks: an NPC class that ignores the traffic board entirely (no departure entry — the
  board is a *human* institution), extremely old ephemeris-stable orbits, and sensor behavior
  that doesn't match SensorModel expectations (e.g. they see *you* through sun glare). Mystery
  encounters, not combat. Possibly the in-fiction source of scenario B's "Wheel of the World"
  cosmology (M8): the Wheel is how the ancients describe the sky.

## 3. Mercury's polar craters — the compute capital

- Mercury's north-pole craters: permanent shadow (huge cold sink) meters away from relentless
  sunlight — thermodynamically ideal for energy-hungry compute. Fiction: the inner system's
  AI-compute capital sits in a Mercury polar crater rim farm.
- Mechanical tie-in that already exists: traffic to/from Mercury flies deep in the **sun-glare
  cone** (M5 `SensorModel`), so the compute trade is naturally stealthy space — ambush country
  (dovetails with M7's low-solar-orbit sneak-up accept test).

## 4. He3 and the outer moons

- Zubrin-style He3 trade anchors the outer planets and their moons in the economy (with the
  owner's Electric-Universe-flavored aside that the moons' isotope ratios closely match
  Earth's). Saturn/Jupiter moons as named origins (Titan, Europa...) rather than the gas
  giants themselves once scenarios support moons as bodies — `CircularOrbitEphemeris` already
  chains parents, so moon-origin departures are data, not code.

## 5. Ship-side telescope — the pirate's little Hubble

- A directed optical instrument, distinct from the passive sensor sweep: point it at a sky
  position and *look*. Prey whose position is known from the departures board ("known from the
  stars") can be found and evaluated long before sensor range: hull class, cargo guess, charge
  glow, maybe a burn flash when it maneuvers.
- Mechanical sketch: narrow field of view (a cone a few degrees wide) with several × the
  passive sensor range; must be aimed and *held* on a tracked target (like venting, it
  occupies the player's attention — the observation minigame). A successful track feeds
  higher-quality `Observation`s into the existing `PathPredictor` (smaller `w0`/`σv` → visibly
  tighter cone: telescope tracking literally sharpens your intercept).
- UI: a "scope" overlay — small inset viewport rendered from the ship toward the aim point
  with magnification, crosshair, and a track-lock indicator. `IObservationModel` already
  abstracts what a sensor sees; a `TelescopeModel` slots beside `SensorModel`.
- Lands naturally with M10 polish or as M9.5; no engine changes needed — it is an observation
  model + an overlay.

## 6. Aboard-ship life: deck view → the boarding sequence

- **Deck view v1 shipped (M12)**: top-down walkable interior — bridge/helm, scope alcove,
  cargo hold (crates = your actual loot), shuttle bay (shuttle goes AWAY during a boarding),
  engine room with vent panel. Consoles map to real game actions.
- **The dream (owner, 2026-07-03)**: when the boarding window opens, *walk* to the shuttle
  bay, board the shuttle, fly the little craft across to the prey (a short piloting minigame
  in the gap between the two hulls — the window timer becomes YOUR timer), dock at its
  airlock, and take the cargo from its hold room. Third-person avatar view of these interiors.
- Mechanical sketch: shuttle flight = a small local-frame 2D scene (two hulls, relative drift
  from the real rel-velocity — sloppier pass = faster drift = harder approach; the
  RequiredSecondsFor math becomes literal gameplay). Prey interiors can be generated from
  ship kind. Third-person = the same deck renderer with a camera following the avatar.


## 7. Phobos — the strangest port in Sol (owner-endorsed, 2026-07-17)

- The pieces already converging on one 27-km moon: the **85 m monolith** on the Stickney rim
  (#164 — a meeting place for deals, now a real `Landmark` since #231), the flagship
  **treasure island** (the first map card reads "PHOBOS — from the monolith, N paces..."),
  and the **beanstalk** candidate (#234 — at Phobos scale a space elevator is a rope with a
  rock on the end; materials stress is a rounding error).
- The vision: a port that shouldn't exist and does. Deals sealed in the monolith's shadow,
  hoards buried in its regolith, cargo riding a cable no engineer had to be clever about,
  and the elevator's two ends as paired walkable ports — the door family's vertical member
  (docking tube → shuttle bay → the long ride).
- Nobody agrees who built the monolith; everybody agrees the anchor bolts for the cable went
  in suspiciously easily.

## 8. The monolith is one object, and it is enormous (owner ruling, 2026-08-03)

*Settles #649 ("which moon is THE MONOLITH on?") on the fiction side. Recorded verbatim in
intent so it cannot be lost; the code decision follows from it.*

- **Unique.** There is **one** monolith. Not a class of object, not a kind of landmark that a
  generator can roll twice. If two grounds both call something "the monolith", one of them is
  wrong and has to be renamed — the word is reserved.
- **Bigger.** The owner's note is about *scale*: the real Phobos slab's dimensions were huge,
  and the game has been under-selling it. It must **show its size** on screen — you should
  read it as large from a long way off, and it should keep getting larger as you walk, the way
  only genuinely big things do.
- **Not in a boxed backyard.** It must not sit in a fenced little plot with the rest of the
  set dressing around it. Whatever ground carries it has to be open enough that the object is
  the horizon, not a prop in a room. A monolith you can pace out in ten steps of decking is
  not the monolith.
- **Not built by us.** Nothing about it may read as human manufacture — no seams that look
  like a fab, no scale that flatters a shipyard, no explanatory plate. The correct player
  reaction is *nobody I know could have put that there*.

**Shipped (#649, PR 1).** `Monolith.BodyId` is `phobos`, read from `Landmarks.MonolithBodyId` so the
map cards and the drawn slab cannot name two moons again. Phobos's ground is authored — "THE STICKNEY
RIM" — with no maze and no ruin field between the landing band and the object. Miranda keeps its canon
maze, byte-for-byte, and its centre is **the false slab**: quarried, mortared, weathering, a different
class of object that never borrows the word. The swept apron, which used to be drawn on every ground in
the game, is now asked for by the thing it is swept around. Details: `features/the-landing-site.md` §10.

**Shipped (#649, PR 2 — the scale).** `SurfaceScale` states what a deck unit is (0.7 m, anchored on the
captain's own width) and every dimension of the monolith is derived from the one canon number the game
already held: 85 m. Proportions 1 : 4 : 9, never stated in the game; footprint 54 × 13.5 du against a
64 × 28 du frame; a shadow ~370 du long — longer than the field is deep — running up to the landing band,
which is how a top-down plan says *tall*; drawn as the only unbroken filled mass on any moon, because its
own card says *no seam*. Sight range and the arrival beat are both functions of its height now, not typed
constants. Details: `features/the-landing-site.md` §10.4b.

### The monolith site is a strange-things-happen place

- The mood the owner asked for, in his own reference: **Babylon 5** — Sheridan and the giants
  on the playground; **"background puppeteers watching if their kids perform in the school
  play."** You are being *watched by something that has a stake in you and will not say so*.
- **Awesome and a little scary. Lovecraftian.** Not a jump scare and not a monster —
  the dread of scale and of attention. The nerve system already prices the sight
  (`NerveModel.MonolithSightShock`, the single biggest fright in a captain's life, fires once);
  the *place* should earn that number rather than the number carrying the place alone.
- **Per standing canon it is NEVER explained and NEVER confirmed.** No card states what it is,
  no sensor returns a reading that settles it, no NPC knows. Anything that happens near it is
  reported by the world in a way that stays deniable. This is the same law that governs the
  Reever origin: the inference is the horror, and confirming it kills it.

**Shipped (#649, PR 3 — the watch).** `MonolithWatch`: three gates (the monolith's own ground and inside
its sight; about one visit-window in three, on the foot-offerings' slow clock; forty seconds of standing
still, once per excursion). Six variants, every one a fact about the *world* and not a thing that could be
met — the shadows disagreeing with the slab's, your own prints ahead of you, the pack going still and
facing the stone, a tide in the dust, one tracker contact too many, the light dipping with nothing crossing
the sun. **It costs nothing**: the place is already priced at 24, and a site that bills you for standing in
it is a predator whatever the prose says. Not a card and not a plate — a frame around a thing says THIS IS
A THING. Cheat `?watchers=1`. Details: `features/the-landing-site.md` §10.4c.

## 9. KAAMOS is the head office (owner ruling, 2026-08-03)

*Bears on #411 (the KAAMOS plotline) and #635 (KAAMOS has no front door). Destination design,
not route design.*

- **The KAAMOS destination is the HEAD of the organization.** Not another outpost, not a
  bigger version of a wintering camp: the place everything else answers to.
- **"As fancy as the secret labs."** The secret labs set the bar for how a serious facility
  presents itself; the head office has to clear it, visibly, on first sight.
- **The Hive facilities are branch offices.** Everything the player has learned to read
  underground — sealed `SECTOR n · 2.4 km` doors, authority cards that open exactly one band,
  the department plates by the lift car, the band nobody listed — is *branch-office* grammar.
  **HQ outclasses them**, and it should outclass them in the same vocabulary, so a player who
  has crawled a Hive recognises the rank difference without being told it.

## 10. The fourth world, and the halls beneath it (owner ruling, 2026-08-04)

*Extends §2 (fourth humanity) and §8 (the monolith site) into one cosmology, and names the
material for #649's "Giants on the Playground" arc. Method note: this is the Iron Sky method —
collect the real world's strange claims and use them as lore. The quarry is the owner's
**VennsRabbitHole** encyclopedia (github.com/esoinila/VennsRabbitHole, private; the Hermes
"Rabbit Hole Monk" collects and provenances it from YouTube): Hopi emergence narratives — the
Sipapu as constructed levels, the claimed 1963 elder testimony of "cities beneath the ones
destroyed", present-tense below-keepers — the 1909 Arizona Gazette iron-door account, Tartaria
tunnel narratives, Derinkuyu, Lake Van's drowned walls, the Giza deep-SAR survey. Claims carry
provenance there; here they are fiction.*

- **This is the fourth run, and the first three were ended** — by fire, then by ice, then by
  flood (the Hopi count). §2's "Ancient Apocalypse flavor" gets its structure from this: the
  resets were not weather that the ancients happened to survive. They were *ends*, and
  something decided them.
- **The caretakers are the giants on the playground.** §2's background ancients and §8's
  watchers are the same parties: they watch the fourth civilization while the question of what
  to do with it — transition, or wipe — is being decided. The monolith's attention (the
  school-play beat) is what that deciding looks like from the ground: parental, not predatory,
  and with a verdict pending. Per standing law this is never stated, never confirmed, and no
  card ever joins these dots.
- **Some have always been allowed to survive underground, in massive halls.** Every prior end
  spared a remnant, and the remnant went under. Out on the moons this is the material: digs
  that break into volume that was *already there* — galleries that pre-exist the facility,
  chambers that get **bigger** as you go down, fire-marks in rooms no ventilation shaft
  anyone can find would serve, dry floors with no pumps. The operation's own paperwork changes
  tone where the poured concrete stops. Nobody writes down what they think it means.
- **The horror is the disclosure schedule.** As the day of decision approaches, the fourth is
  *let* to know more and more about what is under its feet. Every find must carry both
  readings at once: the mundane one (the new sounder resolves deeper; the resurvey team is
  simply better than the last one was) and the other one (this was always here, and it is
  being **shown** to us, because the time to know it has come). If any card ever settles which
  reading is true, the horror dies — this is the Reever law applied to archaeology.
- **Deep enough, or far enough, you are told things you might not be able to live happily
  with.** The arc's escalation is not bigger rooms; it is the dawning shape of the
  arrangement — that the halls have always been provisioned, that somebody has always been
  spared into them, and what that implies about everybody who wasn't. All of it by inference,
  none of it on a plate.
- **Boundaries.** The halls' builders are not the Old Ones, not the Reevers, and not confirmed
  to be anything — the Reever origin stays its own separate, uncommitted inference. The
  monolith stays unique (§8): a hall is a hall and never borrows the word. KAAMOS (§9) does
  not know either; the head office deals in filings, not answers.

Game hooks, in the grammar the game already owns: a rare **found** band beneath a deep dig —
a different class from the band nobody listed (landing-site doc §13.7), which is human all the
way down; the imported-door idiom (landing-site doc §11.4) sealed by *us* on something we
found; below-keepers appearing present-tense only in somebody else's dossier (§12.4 — a
dossier never joins the dots); and the disclosure clock riding the same slow world-side
windows the monolith's foot-offerings use, so what you are shown is a fact about *when you
went*, not a reward for how hard you looked.

**Shipped (#677, v1 — the band).** `UndergroundComplex` has a found band: on about one site in
fifty — one in five of the rare sites that already hide a band — two bands under the listed
bottom, with a **whole band of nothing dug** between the two, because the shaft was driven
through four floors of rock and broke into what was waiting. The way in is the card idiom
again, found in the band nobody listed. Past the seam every single thing that makes a facility
legible is absent: no plate, no department, no livery, no lock, no stencilled distance, no door
leaf, no drain, no shelf, no lamp. It is **dark** (the floor's own claim), it **holds pressure**
with nothing anywhere showing plant, its walls are drawn in a **third material that takes no
palette at all**, and its chambers get **bigger** the deeper you go — the game's learned grammar
inverted, said by the room scale and by no prose. Almost every chamber is empty; about one in
nine holds the record of a wall that cannot be brought back. The register is the owner's own —
*"Horror served as smooth comfy pillow"*: the halls are comfortable, and a pillow means you were
expected. Cheat `?found=1`. Details: `features/the-landing-site.md` §13.20.

**Shipped (#677, v1 — the clock).** The disclosure schedule above is a mechanic now, and only a
mechanic: `DisclosureClock` starts on the seam, once per ground, and reads in the **same slow
world-side windows the monolith's foot-offerings use** — asked of `Monolith.EpochAt` and never
re-derived, because the issue names that clock by name. Its register (which grounds have been
opened, and in which window) rides the vault, so it survives a reload. It is **never announced and
never a progress bar**, and the proof is that the type publishes no prose at all — no label, no
line, no percentage, not one string — which also settles §8 for free. It is **not farmable**:
nothing in any signature is effort, and a hundred rides inside one window read what one ride reads.
It **never runs backwards**: the first crossing is the one kept, and the reading is monotone. What
is deliberately NOT published is a capped *stage* — a cap needs a cadence and a ceiling, and neither
is derivable from anything in the world; they would be two invented facts about the schedule of a
decision no card may state. **Nothing reads it yet**, which is the sequencing #1063, #1068 and #1074
each asked for in their own words: the threshold is here, the consequences are theirs.

**Shipped (#1063, slice 1 — THE BURIAL, and the clock's first customer).** Owner ruling 2026-09-01, on the
watcher doctrine (§672): the creepiest reaction is not the world declining but *people hiding the truth
themselves* — told so convincingly they obey like automatons, bury what was found, and forget they did it.
Between two visits a ground the captain opened is GONE the way a street grade goes: filled, floored,
resurfaced. **The threshold, and its written reason:** one whole world window since the opening (a work order
takes a shift — a crew already standing there with the trucks would be a fact about THEM, and the Scully law
is spent the moment one act is only explicable by them), **and** the captain not on that body (the neighbours
do not fill a hall while he is standing in it). Evaluated once, on the descent, after the crossing's clock is
spent and before one wall of the arriving ground is laid — so a ground can only ever be filled while he is in
flight, and the burial LANDS on his return.

The erasure procedure is the issue's four clauses and it is implemented as written. **(1) Remove the element**
and **(2) remove its marks** are one gate: `UndergroundComplex.HasFoundBand` asks `Burial.IsFilled` first, and
every question the game has about the halls already goes through it — so the shaft ends at the listed bottom,
the found-key card room stops generating, the darkness stops being declared, the room scale stops growing, and
the clock stops opening. **(3) The town above keeps living**: nothing else about the site changes, and a world
where nothing has been buried is byte-identical to the world before this shipped. **(4) One specimen is kept**:
on the listed bottom, a short recess off the corridor with a single old door at the back of it that does not
open, drawn in the found band's **own** no-texture idiom on a floor that is otherwise entirely poured. It has
no plate, no sign, no card and no line, because the object is the whole statement.

The evidence lifecycle is four authored sentences and not one more, all of them ordinary facilities
paperwork: the **works notice** on the canteen board while the job is on; the **maintenance ledger**, three
lines with no instruction number where the ledger cites one for everything else; the **rag**, cheerful, once,
about the drainage and the handsome course of old kerbs in the new wall; and, if pressed, **a mason** — *"and
he means it, and he files it, and that is the whole testimony."* **LAW: THE BOOK NEVER LIES.** Nothing the
burial does removes or rewrites one field-book note, clipped story, red thread or satchel row: the captain's
record is the only witness and it stays correct, which is what the player stands on. Cheat `?buried=1`.
Details: `features/the-landing-site.md` §13.30.

**Still open and deliberately unbuilt: any inhabitant content.**

## Suggested landing spots

| Theme | Milestone | First concrete step |
|---|---|---|
| Mass-driver pods | M6 | New NPC kind: ballistic pod, `aBudget = 0` cone, cargo "Compute cores" |
| Mercury compute farms | M6/M7 | Mercury as a traffic destination; glare-country ambush routes |
| Ancient satellites | M7/M8 | Off-board NPC class with pyramid icon, no departures entry |
| He3 moon origins | M8 | Moons in scenario JSON (parent chaining already works) |
| Ship-side telescope | M9.5/M10 | `TelescopeModel : IObservationModel` + scope overlay; better obs → tighter prediction cones |
| Telescope track-hold | post-M12 | holding the scope on a target improves its Observation quality → visibly tighter cone |
| Shuttle boarding sequence | post-M12 | walk to bay → fly shuttle across (rel-velocity drift minigame) → prey interior |
| Third-person deck camera | post-M12 | deck renderer + avatar-following camera |
| Phobos, strangest port | after #225 arc | Lab 31 beanstalk numbers -> elevator haven pair at Phobos (#164/#231/#234) |
| The found halls (fourth world) | **v1 SHIPPED (#677)** | the band itself: `UndergroundComplex` found band, dark, sealed, smooth-walled, chambers growing with depth, `?found=1`; and the **disclosure clock** (`DisclosureClock` — the seam starts it, the monolith's windows measure it, nothing announces it, nothing reads it yet). Open: the inhabitants (§10) |
