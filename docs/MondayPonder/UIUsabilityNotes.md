# UI usability notes — captured while flying a piracy run

Running list of friction points found while actually *using* the game to hunt a
ship. Each is a candidate for a UI improvement, roughly in the order hit.

## Blind-UI audit round 1 (2026-07-14 evening — owner's protocol)

Eight screenshots of the live build (4ccb067+) given cold to fresh-context AI testers
(`tools/playthrough/ui-audit-shots.mjs` captures; Gemini CLI was auth-blocked — rerun
`run-gemini.sh` after `gemini` re-auths — so fresh Claude subagents stood in). Metric per the
owner: the less context the tester needs, the more intuitive the UI.

**Scorecard.** PASS cold: Trade (best — "Sell cargo (900 cr)" found instantly), Plot/Sling
(pass-distance slider + Lead/Trail + commit flow inferred unaided), Ledger (right button chosen),
Sensors (controls understood). NEEDED CONTEXT: Nav (worst — couldn't set course for Mars: planet
labels subtle vs. loud station labels, click-a-planet invisible affordance), War room (kill-chain
teaching panel excellent, but it references FIRE/AUTO-AIM controls that don't render until a
target exists), Deck (airlock guessed by genre convention only; nothing said "docked" or "walk
up"), Comms (buy-intel path inferred from a hint sentence, not a control).

**Fixed same evening (string-level):** deck hint bar now says "docked ⚓ walk up through the
airlock to go ashore"; the New-here banner adds "— or click any planet to set course"; the sling
slider reads "planet radii"; the Sensors empty state says "Track ledger" (disambiguating from the
Captain's ledger) and mentions cold cases.

**Queued (medium):** ghost/disabled kill-chain buttons pre-target on the gun deck; a hunt-by-name
entry on Sensors; make the dark-web market node visibly a market; dedupe the ledger's twin 🔭
buttons; planet-label visual hierarchy above stations; scope panel title vs. ship-name confusion;
partial sells (existing roadmap).

## Planet-centric frames inside the gas-giant systems (owner, 2026-07-15 — BACKLOG)

- **The symptom (owner's realization after the Saturn session):** navigating inside
  Jupiter's / Saturn's moon system "was really hard in part because we were using the
  sun-rotation speeds there... our vectors were humongous size for navigating from moon
  to moon in the Saturn space." Saturn itself is doing ~9.7 km/s around the Sun (Jupiter
  ~13 km/s), so every heliocentric velocity readout and map vector inside the moon system
  is dominated by the primary's solar orbit — the few-hundred-m/s moon-to-moon differences
  that actually matter there are invisible in the numbers and the arrow lengths.
- **The adjustment:** when the ship is inside a gas giant's sphere of influence (Hill
  sphere), the flight UI should switch its reference frame to the local primary —
  Saturn-centric speeds at Saturn, Jupiter-centric at Jupiter, same for the other giants:
  velocity readouts, relative-speed lines, plotted velocity vectors, and burn-planning
  numbers all quoted relative to the primary (or to the target moon when one is selected).
  A visible frame chip ("frame: Saturn") so the player knows which ruler is in use, and an
  honest handover at the Hill boundary.
- **Code notes:** Core already thinks body-relative where it counts (OrbitRule's approach
  and insertion work against `bodyVelocity`; the ephemeris knows every body's parent), so
  this is chiefly a **display/UX frame change**, not new physics. The trap to avoid is
  mixing frames silently — every number on screen should agree with the chip.
- Pairs naturally with the unified flight plan (UnifiedNavListNotes.md): steps inside a
  giant's system read in the local frame ("burn 320 m/s Saturn-frame"), which is also what
  makes a moon-tour milk run legible.

## The Captain's ledger (owner, 2026-07-14 playtest of the intel/sling build)

- **Tips have no inventory.** Owner took Gilt-Eye's route tip and it effectively
  vanished: route intel only surfaces as a "Known" tag on the dark-web market
  view. "Is there like an inventory for these kind of things?" — there should
  be one centralized brief.
- **Proposal:** promote Captain's 📜 *Quests* tab into 📜 **the ledger** — three
  sections: **Jobs** (current quest cards, unchanged), **Tips & intel** (every
  intel item with provenance — who/where/when — and an action link when one
  exists: scope intel keeps its 🔭 point-the-scope button, route tips get
  "→ watch on Sensors" / "→ view on dark web"; non-actionable tips still listed,
  labeled "background — may matter later"), **Standing arrangements** (later:
  masked contracts, armed insertions).
- **The pattern to generalize:** the ledger never does the task — it jumps you
  to the desk that does, context pre-loaded, exactly like the intel card's 🔭
  button (switches to Sensors and schedules the prioritized scan). "Links to
  the desks to do related tasks" — owner's words.
- Candidate lane: PR-J after the skim gauge (PR-I).

## Finding a target
- **"Traffic board" wording vs. the actual desk.** The First-hunt tutorial says
  "Open the traffic board and select the Luna pod," but there is no control
  labelled *traffic board* — it's the **Comms** desk (Contacts & intel). The
  tutorial's noun and the desk's name should match, or the tutorial should say
  "Comms desk (5)".
- **The en-route freighter list is a flat name list.** Comms → Contacts shows
  ships by name + status chip only. To learn each ship's route/cargo/distance I
  had to click every row and read its dossier one at a time. A hunter wants to
  compare prey at a glance — show route, cargo (and value), and distance/closing
  rate as columns, ideally sortable by "closest" or "richest".
- **No catchability cue in the list.** Whether a ship is *closing* or *opening*
  only appears after selecting it (map popup: "opening 6.0 km/s"). Surface a
  closing/opening arrow + rel-speed on the contact row so you can pick a
  reachable target without opening each one.
- **"Not yet inside sensor reach" targets can't be acted on.** All four en-route
  freighters read "last seen: never — not yet inside sensor reach," so the Comms
  dossier is informative but every action points elsewhere ("track her at the
  Sensors desk"). A one-click "hand this contact to Sensors / plot toward its
  route" would save the desk-hopping.

## The catchability gap (biggest one)
- **Nothing tells you *when/where* a target is actually boardable.** The window
  is rel-speed < 5 km/s within 5e8 m, but interplanetary traffic runs at
  **80–160 km/s relative** mid-transit (Tycho's Due showed 161 km/s inbound).
  A ship is only matchable when it shares your orbit — e.g. in the first hours
  after it departs a body you're near, or as it settles into a destination orbit
  you also hold. Nothing in the UI flags "this one is matchable" vs "this is a
  160 km/s fly-through you can never catch." A hunter currently learns this only
  by selecting a target and reading a scary rel-speed. Ideas: colour the contact
  by catchability (green if a plotted match is within your Δv budget), or show a
  "match cost: N pulses / impossible" estimate next to rel-speed.
- **No "ambush a fresh departure" affordance.** The natural catch — pounce on a
  ship just leaving Earth/Luna while it's still near your velocity — isn't
  surfaced anywhere. A "departing soon from <body>" flag on scheduled ships (with
  the bodies you're co-orbiting highlighted) would point new pirates at the one
  catch that actually works from a standing start.

## Scope
- **Manual target stepping is unclear.** With the scope in AUTO, clicking the
  ◀ / ▶ arrows didn't visibly cycle the locked target. It's not obvious you must
  leave AUTO first, or the arrows need a more obvious active/disabled state.

## Tutorial vs. reality
- **"First hunt" promises a catch the opening state may not offer.** The tutorial
  says to select the Luna pod and board it, but at scenario start I found no pod
  or freighter inside the boarding envelope — the nearest ship was receding and
  the rest were 160 km/s fly-throughs. Either seed a genuinely catchable target
  near the player at t=0, or have the tutorial say "wait for / ambush a fresh
  departure" so the promise matches the physics.
- **Warp + boarding interaction is unexplained.** Boarding progress accrues at
  wall-clock rate (so you can't warp through a board) — correct and fair, but the
  UI never says so; a first-timer will crank warp during a board and wonder why
  the bar crawls. A one-line hint when a window opens would help.

## Fire control (gun deck) — from a live firing run
- **The world clock is Nav-only.** Warp/pause live solely on the Nav desk, but you
  fire from the War room — so after arming a shot you must hop back to Nav to let
  the round fly. The desk you're acting on can't advance its own consequences.
  Consider a minimal warp/pause control (or at least a "resume" affordance) on
  every desk, or surface the clock globally.
- **"ROUND AWAY IN 60 s" reads as a static label, not a countdown.** It's a
  sim-time countdown, so it's frozen while paused and blinks past in one frame at
  209×/6000× — you never see it tick 60→0 at normal speeds. Owner's instinct: it
  should visibly count down. Options: tick it in real time regardless of warp, or
  show it as a progress ring, or express it as an arrival clock ("round away —
  impact 14 h") that always moves.
- **No explicit hit/miss report.** The war room promises "hit or miss, you'll hear
  it," but the only feedback is the target's Nav panel quietly flipping to "adrift
  — sail holed." There's no toast at impact and no last-shot result line on the gun
  deck. Add a clear impact event: a toast + a persistent "last round: HIT (sail
  holed) / MISS (74 km)" line where "Rounds in flight" was.
- **Richer damage reports (owner's idea).** Beyond hit/miss, vary the report by
  outcome: "secondary explosions — she's venting" vs "clean hole, no real damage"
  vs "sail holed — adrift." Gives the gun texture and tells the pirate whether the
  shot achieved the goal.
- **Name the intent, not just the effect.** A hit "holes the sail" and the target
  goes "adrift — easy prey" — which *is* the owner's "shoot the engine so she's
  easier to catch." Good mechanic, but the UI never frames firing that way up
  front. Label the shot's purpose ("cripple her drive — she can't run") so the
  tactic is legible before you pull the trigger.
- **Defensive fire is unsupported (owner's idea).** Today the gun is purely
  offensive (cripple prey). A pirate also wants to fire at a *pursuer* — a hunter
  chasing you — to force it to break course rather than shooting at you. Worth a
  design pass: let the interest/aim pipeline target hunters, with a "warn off /
  force a course change" outcome distinct from "cripple for boarding."
- **Captain's authorization is invisible until you hit FIRE.** FIRE needs "the
  captain's word — desk 0 authorizes a shot, or orders fire at will," but you only
  learn that by pressing FIRE and having nothing happen. Surface the authorization
  state on the FIRE button itself (e.g. disabled + "awaiting captain's word (desk
  0)") so it's clear before the click.

## Per-target interaction log in the scope (owner's idea) — the durable fix for feedback
Rather than transient pulses you can warp past, give **each target a persistent
rap sheet** shown in its scope/dossier: a running log of what you've done to her —
"hailed · warned (she called muscle) · fired (miss, 74 km) · fired (HIT — sail
holed) · boarded: took 12× He3." Because it's attached to the contact, you never
lose the record: it survives warp, desk-hops, and re-selection. This single feature
subsumes several notes above — the missing hit/miss report, the "you warped past
the DIRECT HIT toast," and the richer-damage-report idea all become log lines.
It also gives the pirate a memory ("did I already rob this one?") and sets up
reputation/known-associate hooks later. Implementation: append an event to the
NpcState (warned/fired/hit/miss/boarded, sim time, detail) at each interaction
site (FireWarningShot, the slug hit-check, Board), and render it in the scope
panel. Cheap, additive, and it's the right home for all gun feedback.

## Losing the tutorial target (found live driving the second hunt)
- **A running clock + a plotted burn silently carries you away from the hunt
  target, and it drifts off-screen with no cue.** While flying the gun hunt I
  left the world clock running with a burn plotted; ~6 days elapsed, the burn
  fired, the ship flew off on a new heading, and the Nervous Lark — opening at
  2.5 km/s — slid out of the (zoomed-in) view. The map just showed empty black
  space. Nothing said "your target drifted off-screen" and there was no one-click
  "re-centre on the Lark" / "follow target". The Sensors desk still read
  "Tracking Nervous Lark" the whole time, so the data was there — the *view* just
  abandoned her. Ideas: while a tutorial hunt is active, keep the target on-screen
  (edge arrow pointing to her + distance, or auto-frame ship+target), and/or a
  "the Lark drifted off — jump to her" affordance. The hunt picker's re-seed is a
  workable escape hatch (spawn a fresh one alongside), but you shouldn't *need* it.
- **The prediction cone lingers after the target marker scrolls off — it reads as
  a graphics glitch (owner).** With the Lark off-screen, her "possible directions"
  cone/curves were still drawn across an otherwise black map, with no target dot
  and no numbers — the owner's first read was "is this a render bug?" When the
  locked target leaves the viewport, DON'T leave orphaned cone geometry floating;
  instead show a persistent target-status readout that stays visible regardless of
  view: distance + whether it's **increasing/decreasing**, opening/closing rel
  speed, in/out of **weapon and boarding range**, and an edge arrow toward it.
  "Nervous Lark — 4.1 M km ↗ opening 2.5 km/s — out of range" tells the story the
  empty screen didn't. (The selected-target popup shows some of this only while
  she's on-screen; it needs to survive her leaving the frame.)

## Tutorial progression — a second hunt for the gun (owner's idea)
The "first hunt" only ever teaches the *soft* catch: select a pod, plot an
intercept, hold the window, board. It never teaches the gun — yet gunfire is the
**only** way to take a ship that refuses to stop. That's a missing lesson, and the
compliance mechanic is already built to carry it:
- pods = nothing to comply (board freely — the first hunt),
- compliant freighter = heaves to under a warning shot (fast, bloodless board),
- **stubborn freighter = ignores the warning, calls its own muscle** → you can't
  board a hull that's running and shooting back; you must **cripple her drive**
  (hole the sail → adrift → board the drift).

Proposed "Second hunt — the gun" tutorial track (unlocks after the first sale):
1. Track a stubborn freighter (Sensors) — richer cargo than a pod.
2. Fire a **warning shot** — watch her refuse and call muscle (teaches why the
   soft path fails here).
3. **AIM → SOLVE → FIRE** a slug for effect — lead the mark, mind the flight time.
4. Hit holes her sail — she goes adrift ("easy prey").
5. Close, board the drift, take the richer hold.
6. Run the loot home before the muscle arrives (ties in the heat/hunter system).

This also motivates the defensive-fire idea above: step 6's hunters are the
natural place to teach firing at a *pursuer* to force it off course.

## Selection / picker
- **The map "Which one?" chooser lists bodies and depots but not the nearby
  ship you were aiming at.** Clicking the ship cluster near Earth offered
  Earth/Luna/depots; the freighter itself came via a second path. Prey should be
  first-class (and top) in that chooser.
