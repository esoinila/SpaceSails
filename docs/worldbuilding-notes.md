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
