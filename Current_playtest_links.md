# Current playtest links

Everything needed to launch the local server, and then a bunch of cheat-code links to the events we can test.
Used by sub-models to smoke-launch every scene, and by the owner for his own playtest sessions.

## 0. ZERO-CLAUDE QUICKSTART — test runs with no tokens at all

**The branch with the most features is always `our-own-ship-has-compartments`** (the working base; every lane
merges there first). `main` only holds the last publish, so the base is equal or ahead. Cold start, copy-paste
into a PowerShell at `D:\repo12\spaceSails`:

```powershell
git fetch origin
git switch our-own-ship-has-compartments
git pull --ff-only
./run.ps1          # builds + serves the newest features at http://localhost:5073
```

If the checkout is on another branch or has a session's uncommitted work, serve the base from a throwaway
worktree instead (remove it later with `git worktree remove D:\repo12\wt\play`):

```powershell
git fetch origin
git worktree add D:\repo12\wt\play origin/our-own-ship-has-compartments
cd D:\repo12\wt\play
./run.ps1 -Port 5080     # -> http://localhost:5080
```

Then click any `[local]` link below (swap in your port if you chose one). No server at all? Use the `[live]`
links — they track the **last publish**, not the newest merges. Zombie browsers after a QA session:
`./scripts/qa-zombie-sweep.ps1`.

> **Regenerated on every publish.** The catalog in section 3 mirrors `docs/testing-guide.md` (Appendix A and the scene
> sections) - extracted, never invented - and must never be hand-edited separately: change the guide, regenerate this.
> Last regenerated: 2026-10-06.
>
> **For smoke-launch crews, two known harness facts:** (1) the first-time-on-the-ground card must be dismissed
> ("Boots on, then.") before judging a ground scene; (2) deep links return HTTP 404 on Pages (the SPA fallback) - that is
> normal, the page still boots.
>
> **Smoke-pass hygiene:** smoke passes run batched (default 3 concurrent headless browsers) and end with
> `scripts/qa-zombie-sweep.ps1`, which kills only QA-harness browsers by command line; anyone can run it by hand after a QA session.

## 1. Launch the local server

```
./run.ps1              # Release client, default port 5073  -> http://localhost:5073
./run-debug.ps1        # Debug build (slow in the browser; for development)
./run.ps1 -Port 6000   # a specific port;  -TakePort stops whatever holds it instead of moving berth
```

- Local base URL: `http://localhost:5073` (if the port is taken, `run.ps1` finds the next free berth and says so).
- Live base URL (no server needed): `https://esoinila.github.io/SpaceSails-play`

Use **local** when you are verifying a branch or a change that is not yet published, or for a playtest session in a
shared tab. Use **live** to check what is actually shipped, or when no server can be run (the live links below track the
last publish, so a scene merged since then is not there yet).

Every link below is a path; prefix it with a base, or press its `[local]` / `[live]` suffix.

---

## 2. THIS RELEASE (2026-10-06)

The newest links first. What a tester should see:

- **The claim** (#1151 slice 1): `/map?claim=1`  [local](http://localhost:5073/map?claim=1) [live](https://esoinila.github.io/SpaceSails-play/map?claim=1)
  The Deep's adjuster's room ajar, a completed sail-mend in the book (📋, days under sail-mend), the blank claim form held. Sit at the ship's own desk and press ✍ to fill it, return and press the 🧾 console. Section 3 has the full row.
- **Two more doors** (#1332 D+E): `/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open`  [local](http://localhost:5073/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open) [live](https://esoinila.github.io/SpaceSails-play/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open)
  and `/map?dock=the-deep&ashore=1&havenfloor=-1&office=open`  [local](http://localhost:5073/map?dock=the-deep&ashore=1&havenfloor=-1&office=open) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&ashore=1&havenfloor=-1&office=open)
  The COLD-CHAIN FORWARDING desk (cabin 2, a consignment note that names Deep Storage) and Nebula Mutual's adjuster's room (cabin 4, a blank claim form). `office=shut` for the shut-door watch; section 3 has all four rows.
- **The full keepsake shelf** (#620 complete): boot any ground scene, e.g. `/map?dock=the-tilt&site=0&land=1`  [local](http://localhost:5073/map?dock=the-tilt&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&site=0&land=1)
  Press `I` -> Carried -> KEEPSAKES: the pendant pinned on top; held #973 sheets below it in their capped row (Love steadies, Money steadies less and stings); a held collar joins as the first find. In CABIN 1 the quiet minute; the pendant's first opening raises A PAGE THAT WAS NEVER WRITTEN.
- `/map?shuttle=1&land=1`  [local](http://localhost:5073/map?shuttle=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?shuttle=1&land=1)
  The charter hull beside the preserved dig: the AIRLOCK / GUN RACK / SURVEY LOG plates, and the wire's one line on the slow tick (#1074 beat 5).
- `/map?station=1&land=1`  [local](http://localhost:5073/map?station=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?station=1&land=1)
  Ledger Point, the dead station (#653 slice 1): the drum, the severed tubes, the boat's hop consoles at her dock, the lock that costs 20 s, and the cut face that spends the cutter and stays cut.

---

## 3. THE FULL CATALOG - extracted, never invented

Source of truth: `docs/testing-guide.md` (its Appendix A cheat table and scene sections), in the guide's own order and
words (digests are trimmed to one line; the guide has the whole beat). Grouped as the guide groups them. A link whose
exact URL already appeared above is not repeated.

### Walkthrough links the guide names in its prose (sections 1, 19, 20 and the scene write-ups)

- `/map?scenario=sol`  [local](http://localhost:5073/map?scenario=sol) [live](https://esoinila.github.io/SpaceSails-play/map?scenario=sol)
  section 1: the logbook opens (Continue at the top, the berth list under it) - a civilian URL, not a running voyage
- `/map?scenario=wheel`  [local](http://localhost:5073/map?scenario=wheel) [live](https://esoinila.github.io/SpaceSails-play/map?scenario=wheel)
  section 1: the logbook again; starting from it puts you in "Wheel" (Venus/Earth/Mars on a rigid spoke around Saturn once zoomed out)
- `/map?scenario=not-a-real-scenario`  [local](http://localhost:5073/map?scenario=not-a-real-scenario) [live](https://esoinila.github.io/SpaceSails-play/map?scenario=not-a-real-scenario)
  section 1: the logbook opens over Sol, silently, no crash
- `/map?scenario=sol&ellipse=1`  [local](http://localhost:5073/map?scenario=sol&ellipse=1) [live](https://esoinila.github.io/SpaceSails-play/map?scenario=sol&ellipse=1)
  section 1 / Appendix A: a cheat goes STRAIGHT IN, no logbook - the eccentric demo body already hung in the sky
- `/map?start=cinder-roost&crack=active`  [local](http://localhost:5073/map?start=cinder-roost&crack=active) [live](https://esoinila.github.io/SpaceSails-play/map?start=cinder-roost&crack=active)
  section 19: the Ledger tab lists the break-in, "Crack hatch V-06 - code ..."
- `/map?start=cinder-roost&fetch=active`  [local](http://localhost:5073/map?start=cinder-roost&fetch=active) [live](https://esoinila.github.io/SpaceSails-play/map?start=cinder-roost&fetch=active)
  section 19: the fetch reads "return to the ship - next: Derelict Roadster", direction not instruction
- `/map?ashore=1`  [local](http://localhost:5073/map?ashore=1) [live](https://esoinila.github.io/SpaceSails-play/map?ashore=1)
  section 20: take nothing on, open MISSIONS - the page still exists and says "Nothing owed on foot..."
- `/map?kaamos=3`  [local](http://localhost:5073/map?kaamos=3) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=3)
  the fastest look at the mid-arc card (KAAMOS)
- `/map?nebula=3`  [local](http://localhost:5073/map?nebula=3) [live](https://esoinila.github.io/SpaceSails-play/map?nebula=3)
  the fastest look at the mid-arc card (NEBULA MUTUAL)
- `/map?oracle=1`  [local](http://localhost:5073/map?oracle=1) [live](https://esoinila.github.io/SpaceSails-play/map?oracle=1)
  boot docked; a toast names where she is
- `/map?secretlab=1`  [local](http://localhost:5073/map?secretlab=1) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1)
  boot docked, The Hermit's Rock alongside in shuttle range
- `/map?bond=1`  [local](http://localhost:5073/map?bond=1) [live](https://esoinila.github.io/SpaceSails-play/map?bond=1)
  boot docked at The Space Bar with the regulars (strangers, no history yet) at the tables
- `/map?secretlab=1&land=1&floor=1`  [local](http://localhost:5073/map?secretlab=1&land=1&floor=1) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1&floor=1)
  both of the RARE posts on one floor: the guard's (AUDIT - NO ADMITTANCE) and the reading room out on the block's frontage (#701)
- `/map?dock=the-deep&fuel=40&credits=9000`  [local](http://localhost:5073/map?dock=the-deep&fuel=40&credits=9000) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&fuel=40&credits=9000)
  boot clamped on at The Deep out at Neptune, the tank reads 40 pulses, the purse 9000 cr, FILL HER UP is live
- `/map?dock=red-eye`  [local](http://localhost:5073/map?dock=red-eye) [live](https://esoinila.github.io/SpaceSails-play/map?dock=red-eye)
  clamped on, out at Jupiter among the Galilean moons

### Appendix A - the URL cheat table

- `?scenario=<name>`  (template - fill in the value; no fixed link)
  Load `scenarios/<name>.json` (default `sol`; unknown → silent fall back to Sol).
- `?start=<id>`  (template - fill in the value; no fixed link)
  Jump the built world to a named start point (see the boot picker's registry).
- `?dock=<haven-id>`  (template - fill in the value; no fixed link)
  Boot already CLAMPED ON at any dockable berth — clean state, live services (#288).
- `?fuel=N`  (template - fill in the value; no fixed link)
  Boot with N reaction-mass pulses in the tank (clamped to capacity) (#288).
- `?credits=N`  (template - fill in the value; no fixed link)
  Boot with N credits in the purse (#288).
- `?simhours=N`  (template - fill in the value; no fixed link)
  Jump the sim clock to N hours at boot.
- `?reveal=<bodyId>`  (template - fill in the value; no fixed link)
  Chart a hidden body at boot (repeatable).
- `?target=<contact-id>`  (template - fill in the value; no fixed link)
  Point the tactical UI at a contact and open her DOSSIER on the Nav glass at boot (#997 wave 10).
- `/map?target=collector`  [local](http://localhost:5073/map?target=collector) [live](https://esoinila.github.io/SpaceSails-play/map?target=collector)
  Send the muscle first, then read her file.
- `?dest=<body-id>`  (template - fill in the value; no fixed link)
  Boot with the NAVIGATION DESTINATION already set (#956)
- `/map?ellipse=1`  [local](http://localhost:5073/map?ellipse=1) [live](https://esoinila.github.io/SpaceSails-play/map?ellipse=1)
  Append a visibly eccentric demo body (Kepler rails).
- `?sling=<bodyId>`  (template - fill in the value; no fixed link)
- `?skim=<bodyId>`  (template - fill in the value; no fixed link)
  Boot onto an approach arc with a close pass / atmosphere graze.
- `/map?expedition=1`  [local](http://localhost:5073/map?expedition=1) [live](https://esoinila.github.io/SpaceSails-play/map?expedition=1)
  Spawn an away-team gig ALREADY ACCEPTED, its rock parked in shuttle range (#370).
- `/map?deflection=1`  [local](http://localhost:5073/map?deflection=1) [live](https://esoinila.github.io/SpaceSails-play/map?deflection=1)
  Spawn the asteroid-deflection gig accepted, rock inbound, ship docked at Ringside (#394).
- `/map?tip=route`  [local](http://localhost:5073/map?tip=route) [live](https://esoinila.github.io/SpaceSails-play/map?tip=route)
  Seed a representative route tip, with its provenance, into the ledger — the Captain's-ledger Tips & intel rendering without walking a bar for one.
- `/map?hoard=mine`  [local](http://localhost:5073/map?hoard=mine) [live](https://esoinila.github.io/SpaceSails-play/map?hoard=mine)
  Seed the ledger's 🗺 section (#223): `mine` is one of YOUR chests on Phobos, `rumor` a bought rumour map to somebody else's hoard, `both` one of each — the map card and the dig doors without flying a bury run.
- `/map?backroom=open`  [local](http://localhost:5073/map?backroom=open) [live](https://esoinila.github.io/SpaceSails-play/map?backroom=open)
  Weld the V-06 back room open on the spot, or stage the crack job with its real code so you can key the pad yourself and watch the room grow (PR-F).
- `/map?rep=1`  [local](http://localhost:5073/map?rep=1) [live](https://esoinila.github.io/SpaceSails-play/map?rep=1)
  Put the Nebula Mutual rep (Harlan Fess) on this ground whatever his rota says — or keep him off it (#973 L2).
- `/map?kolt=1`  [local](http://localhost:5073/map?kolt=1) [live](https://esoinila.github.io/SpaceSails-play/map?kolt=1)
  The same lever for Brem Kolt (#1061 beat 2), whose rota has a ceiling as well as a period — one ground in three, and never more than two grounds in a whole universe.
- `/map?walkin=1`  [local](http://localhost:5073/map?walkin=1) [live](https://esoinila.github.io/SpaceSails-play/map?walkin=1)
  Let a bar walk-in happen at this berth whatever the rota and the venue tier say, or keep her away (#973 L5b).
- `/map?finder=1`  [local](http://localhost:5073/map?finder=1) [live](https://esoinila.github.io/SpaceSails-play/map?finder=1)
  Let Ilse Varga cross this floor whatever else is true, or keep her away (#417).
- `/map?dock=selene-gate&geocache=1`  [local](http://localhost:5073/map?dock=selene-gate&geocache=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&geocache=1)
  THE GEOCACHE SALE (#319 slice 2). -- see: one row, SELL THE LOCATION, sub-line "A data chip, under <Body> · <Site>."; press 🗺 and the quote opens under it, "Escrow. The buyer lifts it; you are paid when it is lifted. The fee is the ground's: 2 ...
- `/map?dock=selene-gate&geocache=lifted`  [local](http://localhost:5073/map?dock=selene-gate&geocache=lifted) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&geocache=lifted)
  …AND THE BUYER HAS BEEN (#319).
- `/map?dock=selene-gate&ashore=1&chalk=1`  [local](http://localhost:5073/map?dock=selene-gate&ashore=1&chalk=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&ashore=1&chalk=1)
  A CHALK CROSS IN THE OBSERVATION WALK'S GALLERY (#794 slice 2 — the faceless trade's return leg, moved here from the park by the owner's ruling of 2026-09-28). -- see: on the first tick the mark's line — "Somebody has chalked the stone beside the ...
- `/map?dock=selene-gate&ashore=1&chalk=wiped`  [local](http://localhost:5073/map?dock=selene-gate&ashore=1&chalk=wiped) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&ashore=1&chalk=wiped)
  …AND ONE WATCH LATER (#794 slice 2). -- see: in the gallery, the wipe's line once — "The stone is clean. Somebody wiped it, or somebody read it. The stone does not say." — and never again for that window; no cross on the stone; and the goods still ...
- `/map?dock=selene-gate&press=1`  [local](http://localhost:5073/map?dock=selene-gate&press=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&press=1)
  CARRY THE PRESS — SHE IS ABOARD (#1202 slice 1). -- see: the DEV pulse naming her ground and the tin's words ("…the tin: N paces … of …"); in the Captain's ledger (0) a CARRY THE PRESS row, giver Rauha Lind. Board the shuttle to that ground (e.g. ...
- `/map?dock=selene-gate&press=filed`  [local](http://localhost:5073/map?dock=selene-gate&press=filed) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&press=filed)
  …AND HER STORY RAN (#1202). -- see: on the Galley (6) and the Comms ticker, "Luna is not losing people, says a hired boat's master who asked not to be named — it is losing the difference between the people it counts and the people it has. Officials ...
- `/map?dock=selene-gate&press=pending`  [local](http://localhost:5073/map?dock=selene-gate&press=pending) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&press=pending)
  …AND HER STORY IS PENDING (#1202 slice 2 QA). -- see: the DEV pulse "🧪 DEV ?press=pending — R. Lind's Luna story is 12 watches off; open Comms → dark web for SPIKE IT"; the tin in the sleeve. Open Comms → 🕸 Dark web market: the row SPIKE IT · ...
- `/map?dock=selene-gate&ashore=1&spike=1`  [local](http://localhost:5073/map?dock=selene-gate&ashore=1&spike=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&ashore=1&spike=1)
  SPIKE IT — SHE WRITES AT THE FAR TABLE (#1202 slice 2). -- see: the DEV pulse naming the body and the watches to the window; a figure plated Lind at the far (second) table, and on the first frames, once, "She is at the far table with the recorder ...
- `/map?dock=selene-gate&ashore=1&spike=spiked`  [local](http://localhost:5073/map?dock=selene-gate&ashore=1&spike=spiked) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&ashore=1&spike=spiked)
  …AND THE STORY DID NOT RUN (#1202 slice 2). -- see: nothing on the Galley wire or the Comms ticker about Luna (her story did not print, the floor has no opinion); in the field book, once, under ⬚ — "The Luna story did not run. The wire is one line ...
- `/map?dock=selene-gate&ashore=1&spike=paid`  [local](http://localhost:5073/map?dock=selene-gate&ashore=1&spike=paid) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&ashore=1&spike=paid)
  …AND THE OFFICE HAS PAID — CHARGED TO PRESERVATION (#1202 slice 4). -- see: the DEV pulse "🧪 DEV ?spike=paid — the Luna story did not run and the desk has paid; the receipt is in the satchel; the port rag's line is one cycle off"; the purse (twice ...
- `/map?dock=selene-gate&ashore=1&spike=1&tailed=1`  [local](http://localhost:5073/map?dock=selene-gate&ashore=1&spike=1&tailed=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&ashore=1&spike=1&tailed=1)
  …AND A GREY COAT BEHIND YOU — THE TAIL AT HER TABLE (#1202 slice 3). -- see: the DEV pulse naming the body and the watches, ending "a grey coat comes into the bar after you; walk him out along the tube to the gallery, then sit at her table while she ...
- `/map?dock=selene-gate&simhours=1&ashore=1`  [local](http://localhost:5073/map?dock=selene-gate&simhours=1&ashore=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&simhours=1&ashore=1)
  RAUHA LIND AT HER OWN SEAT (#1202, owner 2026-09-29: "own seat sounds like a known regular"). -- see: a figure plated Lind at one of the bar's numbered chairs with its own ◈ RAUHA LIND console, a chair no regular sits in. [E] there slides her CARRY ...
- `/map?parcel=1`  [local](http://localhost:5073/map?parcel=1) [live](https://esoinila.github.io/SpaceSails-play/map?parcel=1)
  Boot with an unlisted parcel already in the pocket and ride `?land=`'s own descent onto the ground that parcel is actually for (#711 slice 2) — the job row, the walk, the DIG HERE press and the delivery in one URL.
- `/map?crew=petition`  [local](http://localhost:5073/map?crew=petition) [live](https://esoinila.github.io/SpaceSails-play/map?crew=petition)
  A DEPUTATION — three of them in the corridor outside your door (#663).
- `/map?crew=meeting`  [local](http://localhost:5073/map?crew=meeting) [live](https://esoinila.github.io/SpaceSails-play/map?crew=meeting)
  THE MEETING YOU WERE NOT ASKED TO — the cantina at an odd watch, and a chair pulled out that nobody is sitting in (#1066).
- `/map?secretlab=sealed`  [local](http://localhost:5073/map?secretlab=sealed) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=sealed)
  Park the one rock whose site carries THE REFUGE THAT FAILED (#619); ride to it with `&land=1&floor=3`. -- see: two `🫁 REFUGE` plates at two tones, two rings on the fan that do not look alike (one calm and breathing, one grey, hollow and still), the ...
- `?kaamos=N|all`  (template - fill in the value; no fixed link)
  Assemble the first N PROJEKTI KAAMOS fragments (canonical order), or `all` — the intel readout + reach notice without a playthrough (#411).
- `/map?kaamos=bounce`  [local](http://localhost:5073/map?kaamos=bounce) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=bounce)
  Seat the freight agent holding the docket the board keeps sending back at every bar — PROJEKTI KAAMOS's FRONT DOOR (#635). Press `[E]` at any bar patron, take the job, and the filing bounces off your hull too: the arc appears in the Captain's ledger ...
- `/map?kaamos=hq`  [local](http://localhost:5073/map?kaamos=hq) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=hq)
  The whole KAAMOS route already ridden: every shard assembled, the berth-code resolved, the supply run filed, and the ship let go alongside the ice moon (#411). Add `&land=1` to put boots on it.
- `/map?kaamos=pod`  [local](http://localhost:5073/map?kaamos=pod) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=pod)
  Seat the cold KAAMOS supply pod under the ground this excursion lands on — probe any square with the metal detector and earn fragment 2 instead of being handed it (#411). Pair with `&land=1`.
- `/map?kaamos=holder`  [local](http://localhost:5073/map?kaamos=holder) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=holder)
  Seat the rare KAAMOS berth-holder at whatever bar you dock at, every watch — the tell (fragment 4) becomes playable on demand (#411). Pair with `&dock=<berth>`.
- `?site=N`  (template - fill in the value; no fixed link)
  Pre-select landing site N in the boarding panel — board straight onto a specific ground to compare site A vs B → a different surface deck-plan (#320).
- `/map?land=1`  [local](http://localhost:5073/map?land=1) [live](https://esoinila.github.io/SpaceSails-play/map?land=1)
  Ride the shuttle down as soon as the world is ready, onto the first landable body in reach (honours `?site=N`) — the real descent, skipping only the walk to the hatch and the boarding panel. The one-URL way to playtest a surface (#464).
- `?land=<bodyId>`  (template - fill in the value; no fixed link)
  Land on a NAMED body instead of whatever happens to be nearest — matched on id OR name, case-insensitively. If it is not landable from this berth it REFUSES and lists what is, because a cheat that silently lands you somewhere else means you playtest ...
- `?sweep=N`  (template - fill in the value; no fixed link)
  Put N (0–3) black-ops sweepers aboard whatever hull you board — the inspection team: 20 du sight inside a 70° cone, 34 du hearing through walls, a 3 s challenge before they shoot (#538).
- `?reevers=N`  (template - fill in the value; no fixed link)
  Set N Old Ones (0–8) down ON the captain the moment they land, already aware — the chase, the pack spacing and the #453 exchange (block roll, blood, five blows) in seconds instead of a long walk (#458).
- `?nebula=N|all`  (template - fill in the value; no fixed link)
  Assemble the first N NEBULA MUTUAL fragments (canonical order), or `all` — arc 2's intel readout, its state transitions, and (only at `all`, which is the only value that includes the capstone contract) the one-time "true terms" notice, without a ...
- `/map?nebula=adjuster`  [local](http://localhost:5073/map?nebula=adjuster) [live](https://esoinila.github.io/SpaceSails-play/map?nebula=adjuster)
  Seat the rare Nebula Mutual adjuster at whatever bar you dock at, every watch — the tell (fragment 3) becomes playable on demand instead of merely grantable (#422). Pair with `&dock=<berth>`.
- `/map?converge=1`  [local](http://localhost:5073/map?converge=1) [live](https://esoinila.github.io/SpaceSails-play/map?converge=1)
  Seed JUST ENOUGH of BOTH arcs (each side's joint threshold, including the two shards the card quotes) and fire THE CONVERGENCE — the marquee one-time COLLISION — from a single URL (#422).
- `/map?archive=1`  [local](http://localhost:5073/map?archive=1) [live](https://esoinila.github.io/SpaceSails-play/map?archive=1)
  Board a derelict that is CARRYING A COLD-ARCHIVE NODE — arc 2's only in-person scene. Implies `?wreck=ventedbyoneoftheirown`, the one cause Core guarantees a node on.
- `?death=<cause>`  (template - fill in the value; no fixed link)
  KILL THE CAPTAIN AT BOOT, through the real pipeline — the death card, the freeze beat and the brain-backup wake, without dying for them (#621).
- `/map?nopattern=1`  [local](http://localhost:5073/map?nopattern=1) [live](https://esoinila.github.io/SpaceSails-play/map?nopattern=1)
  Boot a captain whose POLICY IS ALREADY CLOSED — the purge handle, pulled on their own jar (#640). Combine with `?death=` for the death where nobody comes: no clinic, no successor, and the thread ends.
- `?havenfloor=N`  (template - fill in the value; no fixed link)
  …AND THEN ONE FLOOR DOWN (#1253). -- see: you are standing where the first cage's doors open, with `🛗 LIFT · L-00` at your elbow; the deck's location strip reads LOWER CONCOURSE; `[E]` at a cabin plate is refused, with no card and no key anywhere in ...
- `/map?dock=the-space-bar&ashore=1&havenfloor=-1`  [local](http://localhost:5073/map?dock=the-space-bar&ashore=1&havenfloor=-1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-space-bar&ashore=1&havenfloor=-1)
  EVERY HUB IS A LOBBY — THE RUSTY ROADSTEAD (#1332 A). -- see: you are standing where the first cage's doors open; the floor's one label and the deck's location strip read LONG-STAY · KEYS AT THE BAR and nothing else is written down there; five ...
- `/map?dock=cinder-roost&ashore=1&havenfloor=-1`  [local](http://localhost:5073/map?dock=cinder-roost&ashore=1&havenfloor=-1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=cinder-roost&ashore=1&havenfloor=-1)
  EVERY HUB IS A LOBBY — CINDER ROOST (#1332 A). -- see: you are standing where the first cage's doors open; the floor's one label and the deck's location strip read BERTH HOTEL · RESIDENTS ONLY and nothing else is written down there; five `CABIN n` ...
- `/map?dock=ringside-exchange&ashore=1&havenfloor=-1`  [local](http://localhost:5073/map?dock=ringside-exchange&ashore=1&havenfloor=-1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=ringside-exchange&ashore=1&havenfloor=-1)
  EVERY HUB IS A LOBBY — RINGSIDE EXCHANGE (#1332 A). -- see: you are standing where the first cage's doors open; the floor's one label and the deck's location strip read MEMBERS' ROOMS and nothing else is written down there; five doors in the cabin ...
- `/map?dock=the-tilt&ashore=1&havenfloor=-1`  [local](http://localhost:5073/map?dock=the-tilt&ashore=1&havenfloor=-1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&ashore=1&havenfloor=-1)
  EVERY HUB IS A LOBBY — THE TILT (#1332 A). -- see: you are standing where the first cage's doors open; the floor's one label and the deck's location strip read ROOMS · MIND THE FLOOR and nothing else is written down there; five `CABIN n` doors ...
- `/map?dock=red-eye&ashore=1&havenfloor=-1`  [local](http://localhost:5073/map?dock=red-eye&ashore=1&havenfloor=-1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=red-eye&ashore=1&havenfloor=-1)
  EVERY HUB IS A LOBBY — THE RED EYE (#1332 A). -- see: you are standing where the first cage's doors open; the floor's one label and the deck's location strip read CREW QUARTERS · NO PUBLIC ACCESS and nothing else is written down there; five `CABIN ...
- `/map?dock=the-deep&ashore=1&havenfloor=-1`  [local](http://localhost:5073/map?dock=the-deep&ashore=1&havenfloor=-1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&ashore=1&havenfloor=-1)
  EVERY HUB IS A LOBBY — THE DEEP (#1332 A). -- see: you are standing where the first cage's doors open; the floor's one label and the deck's location strip read COLD ROOMS · BOOK AT THE DESK and nothing else is written down there; five `CABIN n` ...
- `/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut`  [local](http://localhost:5073/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut) [live](https://esoinila.github.io/SpaceSails-play/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut)
  THE PRESERVATION OFFICE — SHUT (#1332 C). -- see: the plate over the door reads PRESERVATION · BY APPOINTMENT, folded onto two lines in a smaller stencil so it stays inside its own door's width (#1353) — the other four still `CABIN n` on one row ...
- `/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open`  [local](http://localhost:5073/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open) [live](https://esoinila.github.io/SpaceSails-play/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open)
  THE PRESERVATION OFFICE — THE CLERK'S WATCH (#1332 C). -- see: the office's door ajar — its leaf hung part-way over an open doorway — and, about three seconds in, a walker plated Clerk stepping out of it and walking (never speaking, no card) to the ...
- `/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=shut`  [local](http://localhost:5073/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=shut) [live](https://esoinila.github.io/SpaceSails-play/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=shut)
  THE FORWARDING DESK — SHUT (#1332 D). -- see: the plate over the door reads COLD-CHAIN FORWARDING · TRADE ONLY, folded onto three rows (COLD-CHAIN / FORWARDING / TRADE ONLY) in a stencil you can read, inside its own door's width — the other four still `CABIN n` on one row ...
- `/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open`  [local](http://localhost:5073/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open) [live](https://esoinila.github.io/SpaceSails-play/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open)
  THE FORWARDING DESK — AJAR (#1332 D). -- see: the door ajar — its leaf hung part-way over an open doorway — and nobody in or near it (this room keeps no walker); a desk against the back wall with one paper on it, a dot with no title from the corridor. Walk in: the paper reads A consignment note over the desk, and the first step in says ...
- `/map?dock=the-deep&ashore=1&havenfloor=-1&office=shut`  [local](http://localhost:5073/map?dock=the-deep&ashore=1&havenfloor=-1&office=shut) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&ashore=1&havenfloor=-1&office=shut)
  THE ADJUSTER'S ROOM — SHUT (#1332 E). -- see: the plate over the door reads NEBULA MUTUAL · CLAIMS · KNOCK, folded onto three rows inside its own door's width — the other four still `CABIN n` on one row, and the floor still carrying its one label, COLD ROOMS · BOOK AT THE DESK ...
- `/map?dock=the-deep&ashore=1&havenfloor=-1&office=open`  [local](http://localhost:5073/map?dock=the-deep&ashore=1&havenfloor=-1&office=open) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&ashore=1&havenfloor=-1&office=open)
  THE ADJUSTER'S ROOM — AJAR (#1332 E). -- see: the door ajar and nobody in it (no walker); a desk with one paper on it, a dot with no title from the corridor. Walk in: the paper reads A claim form, blank, and the first step in says, once per run and only when the slot is free ...
- `/map?claim=1`  [local](http://localhost:5073/map?claim=1) [live](https://esoinila.github.io/SpaceSails-play/map?claim=1)
  THE CLAIM — THE LOSS, THE FORM, THE BOOKING (#1151 slice 1). The Deep's hotel level, the adjuster's door forced ajar on her watch (the `office=open` idiom), a COMPLETED sail-mend already in the field book and the blank claim form already in the sleeve (staged through the same writers the real path uses; no credit moves anywhere — the game prices no repair, so the loss is the days the mend took). -- see: open the book: one 📋 line under Nebula Mutual and The Deep — "Hull holed — 2.0 days under sail-mend, and the sky kept its schedule without you. The policy calls lost days claimable. Claimable is not the same as paid." (the days are the mend's real elapsed days). Walk to the ship (tube, gangway), sit at her own cabin desk: the seated strip offers ✍ Copy the loss onto the claim form — only with the blank held and a loss unclaimed, only at that desk (not at a cantina top, not standing). Press it: the blank form in the sleeve becomes A claim form, filled — "Loss: hull, holed. Lost to the mend: 2.0 days under way. Claimant: the captain of record. The remaining boxes want codes the desk does not have, and the desk suspects that is their purpose." — the verb is gone (one loss per form). Back at The Deep, in the adjuster's room, with the form held and the door open on her watch, a console stands on the desk beside the paper: 🧾 Book the claim — [E]. Press it: "Booked for her watch. The cold rooms will keep you exactly as patient as you arrive." and one 📍 book entry, once, under Nebula Mutual and The Deep: "A claim, booked. Nebula Mutual resurrects a man without a form; a sail wants three. Somebody designed that, and it was not the sail." Off her watch, [E] at the shut door says the shipped line and, with the filled form held, one more: "The form stays warm in the satchel. Nothing else here is." -- broken: the loss line written when the sail is holed rather than when the mend completes, or with a made-up figure; the verb drawn standing, at a bar top, with no blank or with no loss; the loss copyable twice; the console drawn without a filled form or off her watch; the booking entry twice, or again after a reload; any credit moving; the room changing for a captain who holds no filled form. (Also a button in the front door's ⚙ DEV START SITES list — 🧾✍ "The Deep — the claim, one loss from booked".)
- `/map?dock=the-space-bar&ashore=1&garden=1`  [local](http://localhost:5073/map?dock=the-space-bar&ashore=1&garden=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-space-bar&ashore=1&garden=1)
  THE GARDEN BEHIND GLASS — THE RUSTY ROADSTEAD (#1332 B). -- see: a doorway in the ring's west-north-west face in front of you, and a second one on the next face round (north-north-west, beside the bar's own door) — both open onto the concourse, both ...
- `/map?dock=selene-gate&ashore=1&garden=1`  [local](http://localhost:5073/map?dock=selene-gate&ashore=1&garden=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&ashore=1&garden=1)
  THE GARDEN BEHIND GLASS — SELENE GATE (#1332 B). -- see: a doorway in the ring's west-north-west face in front of you, and a second one on the next face round (north-north-west, beside the bar's own door) — both open onto the concourse, both ...
- `/map?watchers=1`  [local](http://localhost:5073/map?watchers=1) [live](https://esoinila.github.io/SpaceSails-play/map?watchers=1)
  Open the MONOLITH GROUND'S attentive window and cut the dwell from forty seconds to two, so the strange-things-happen beat (#649) can be watched on demand. Stand at the stone. It is rare by design — one visit-window in three, and then only if you ...
- `?nerve=N`  (template - fill in the value; no fixed link)
  Seed the nerve gauge at N of 10 whole pips at boot (#428/#480). Clamps to the gauge; `?nerve=10` is the shipped default. The only way to reach a sanity beat without being hunted for minutes first. #784 adds three WORDS beside the number, for links a ...
- `?hurt=N`  (template - fill in the value; no fixed link)
  STEP OUT OF THE BOAT ALREADY MARKED (#784) — N of `CaptainCondition`'s five blows already landed, so the condition pips under the nerve bar read bruised / bleeding / badly cut from the first frame. Built for the SHORT REST's healing half, which on ...
- `/map?shelter=1`  [local](http://localhost:5073/map?shelter=1) [live](https://esoinila.github.io/SpaceSails-play/map?shelter=1)
  SET THE BOOTS DOWN AT A SHELTER (#728) — a pace outside the door of the one building on the ground that fills a tank AND fills a magazine. -- see: `🫁 CHARGING RACK — FILLS YOUR TANK` on one wall and `🔫 EMERGENCY LOCKER — FILLS YOUR MAGAZINES` on the ...
- `?mags=N`  (template - fill in the value; no fixed link)
  BRING THE SLING DOWN HOLDING N ROUNDS EACH (#728).
- `/map?dark=1`  [local](http://localhost:5073/map?dark=1) [live](https://esoinila.github.io/SpaceSails-play/map?dark=1)
  Put the FIXTURES OUT on every floor this excursion walks — the suit's forward-facing headlights become the whole of the seeing, and everything outside the cone is BLACK rather than dim (#708). The FOUND HALLS (#677) declare themselves dark and are ...
- `?process=N`  (template - fill in the value; no fixed link)
  How long processing one document takes, in sim seconds — `?process=0` makes it INSTANT (#696). Leaving a paper or a file with 🫳, and reading a paper as a clue at the tracker, are a twenty-second hold of standing still; that IS the mechanic, and it ...
- `?book=N`  (template - fill in the value; no fixed link)
- `/map?book=on`  [local](http://localhost:5073/map?book=on) [live](https://esoinila.github.io/SpaceSails-play/map?book=on)
  Put THE ODD BOOK in every would-be-empty room this excursion searches (#701). `1`–`10` force that catalog entry, which is how all ten authored texts get read on demand; `on` (or `all`/`any`) forces the SEEDED entry, i.e. the shipped selection with ...
- `/map?autowalk=1`  [local](http://localhost:5073/map?autowalk=1) [live](https://esoinila.github.io/SpaceSails-play/map?autowalk=1)
  RETIRED (#875) — parsed, and it changes nothing.
- `/map?found=1`  [local](http://localhost:5073/map?found=1) [live](https://esoinila.github.io/SpaceSails-play/map?found=1)
  Park the one rock in the system with a band NOBODY DUG under the band nobody listed (#677), set down at the lift head, and start with every authority this site ever issued already in the wallet — including the last one, which is the way past the ...
- `/map?buried=1`  [local](http://localhost:5073/map?buried=1) [live](https://esoinila.github.io/SpaceSails-play/map?buried=1)
  The same rock as `?found=1`, one shift later: the ground has been OPENED a whole world window ago, so the burial (#1063) fires on the way down and you land on a site whose galleries have been filled, floored and resurfaced. Implies `?found=1`. It ...
- `/map?stopped=1`  [local](http://localhost:5073/map?stopped=1) [live](https://esoinila.github.io/SpaceSails-play/map?stopped=1)
  `?buried=1`'s twin (#1074): the same rock, the same ground opened a whole world window ago, and a window chosen so the split hands this one to the AUTHORITY instead of to the neighbours. Implies `?found=1`. Nothing is filled in — the galleries are ...
- `/map?preserved=1`  [local](http://localhost:5073/map?preserved=1) [live](https://esoinila.github.io/SpaceSails-play/map?preserved=1)
  `?stopped=1` one shift further along (#1074 beat 2): the same rock, the same ground handed to the office by the split, opened TWO whole world windows ago — so the order fires on the way down and then the closed working passes into official CARE. ...
- `/map?shuttle=1`  [local](http://localhost:5073/map?shuttle=1) [live](https://esoinila.github.io/SpaceSails-play/map?shuttle=1)
  `?preserved=1` with the beat of THE RETURNING SHUTTLE (#1074 beat 5) already fired a whole world window ago: a charter survey hull is parked beside the fence on the preserved rock, her shuttle is back, and the three fixtures aboard answer. Implies ...
- `/map?station=1`  [local](http://localhost:5073/map?station=1) [live](https://esoinila.github.io/SpaceSails-play/map?station=1)
  LEDGER POINT, THE DEAD STATION (#653 slice 1) — a hub drum and four arms hanging off the berth, boarded at her crew lock. Two or three of her four tubes are severed (seeded off her id, so it is the same station every boot) and the boat is how you ...
- `/map?card=next`  [local](http://localhost:5073/map?card=next) [live](https://esoinila.github.io/SpaceSails-play/map?card=next)
- `?card=N`  (template - fill in the value; no fixed link)
- `/map?card=all`  [local](http://localhost:5073/map?card=all) [live](https://esoinila.github.io/SpaceSails-play/map?card=all)
  MINT AN AUTHORITY CARD BEFORE THE FIRST RIDE (#693) — the one cheat that makes the CARDED lift row, the gate beat and the refusal ladder reachable on an ordinary site.
- `/map?kit=1`  [local](http://localhost:5073/map?kit=1) [live](https://esoinila.github.io/SpaceSails-play/map?kit=1)
  ASSEMBLE THE FIELD DOSSIER ON THE FIRST PIECE OF SOMEBODY'S KIT, WITH EVERY SENTENCE IT CAN CARRY (#774/#588).
- `/map?tablescene=1`  [local](http://localhost:5073/map?tablescene=1) [live](https://esoinila.github.io/SpaceSails-play/map?tablescene=1)
  BOOT THE TABLE SCENE (#746) — the B1 canteen of a deep site, with people in it, one URL from the front door. Walk to a table with somebody at it, press `[E]`, and ask to join. It implies the whole route (`?secretlab=deep&land=1&floor=1`) rather than ...
- `/map?counter=1`  [local](http://localhost:5073/map?counter=1) [live](https://esoinila.github.io/SpaceSails-play/map?counter=1)
  BOOT THE COUNTER (#756) — the B1 cantina hall of a deep site with the captain standing AT THE COUNTER, one URL from the front door. What a tester should see: press `[E]` and the SERVICE CARD opens, already on the menu — COMPANY COFFEE at 2 cr, the ...
- `/map?secretlab=1&land=1&guard=posted`  [local](http://localhost:5073/map?secretlab=1&land=1&guard=posted) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1&guard=posted)
  THE MAN AT THE DOOR — POSTED (#618 slice 1, A GUARD, BUT NOT A GOOD ONE). -- see: a figure plated Gate standing a few paces right of the cage's doors on the corridor side, and as you step out the card THE MAN AT THE DOOR with, once per excursion, "A ...
- `/map?secretlab=1&land=1&guard=absent`  [local](http://localhost:5073/map?secretlab=1&land=1&guard=absent) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1&guard=absent)
  …ABSENT (#618). -- see: no figure plated Gate; beside the cage a 🪑🧥 mark (the chair by the door, with his coat on it); the first time you come within a few paces of it, once, "The chair by the door has a coat on it. The coat has no man in it."; both ...
- `/map?secretlab=1&land=1&guard=round`  [local](http://localhost:5073/map?secretlab=1&land=1&guard=round) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1&guard=round)
  …ON HIS ROUND (#618). -- see: him at the door (card, moves and the kept way down exactly as posted) — then he walks off toward the counter and, the first time only, "He walks his round the way a man walks to the canteen. Six minutes, if the canteen ...
- `/map?park=1`  [local](http://localhost:5073/map?park=1) [live](https://esoinila.github.io/SpaceSails-play/map?park=1)
  BOOT THE PARK (#759, RE-SITED #813) — the same B1 route as `?counter=1`, with the last leg walked through a gate instead of to the counter, so the captain starts standing on the gravel INSIDE the park. It is no longer "behind the bar": the Manhattan ...
- `/map?park=1&spread=1`  [local](http://localhost:5073/map?park=1&spread=1) [live](https://esoinila.github.io/SpaceSails-play/map?park=1&spread=1)
  BOOT ONTO A PARK BENCH WITH THE CASE OUT (#793) — the `?park=1` route with the last leg walked to a FREE bench, the captain already sat down on it through the very `[E]` handler a player presses, and three finds in the sleeve. -- see: the docked ...
- `/map?parkphase=night`  [local](http://localhost:5073/map?parkphase=night) [live](https://esoinila.github.io/SpaceSails-play/map?parkphase=night)
  THE PARK'S OWN HOUR (#759), with `?park=1` -- see: on `night` the gravel is drawn at 55% of its noon alpha — dim, never unreadable — and the five floodlight masts are small dark heads; on `day` each is a wash of cold horticultural white ...
- `/map?spread=1`  [local](http://localhost:5073/map?spread=1) [live](https://esoinila.github.io/SpaceSails-play/map?spread=1)
  BOOT THE SEATED SPREAD (#784) — the phase-two loop in thirty seconds. -- see: no backdrop and no card. The seated panel is a HUD STRIP docked at the foot of the deck — the hall stays lit, the walkers keep moving, the park stays green — carrying the ...
- `/map?barcase=1`  [local](http://localhost:5073/map?barcase=1) [live](https://esoinila.github.io/SpaceSails-play/map?barcase=1)
  BOOT THE CASE AT A BAR TOP, WITH NO GROUND UNDER YOU (#1016) — the seat the owner filed the bug from. -- see: the docked strip with `Work the case` on it. Press it → the satchel opens on 🗂 SPREAD with rows on it (before this issue: nothing ...
- `/map?frontdoor=1`  [local](http://localhost:5073/map?frontdoor=1) [live](https://esoinila.github.io/SpaceSails-play/map?frontdoor=1)
  BOOT THE CANTEEN'S FRONT DOOR (#775) — the same B1 route as `?counter=1`, stopped one room SHORT: the captain stands OUT ON THE MAIN CORRIDOR, facing the hall's own entrance. Owner, walking the new B1: "The bar/canteen needs DOORS ON THE MAIN ...
- `/map?freight=1`  [local](http://localhost:5073/map?freight=1) [live](https://esoinila.github.io/SpaceSails-play/map?freight=1)
  BOOT THE GOODS HOIST (#775) — the same hall, standing on the hall floor in front of the freight shutter at the end of the counter's own service band. -- see: 🚛 GOODS HOIST 1 painted on the floor in front of a shut roller door in the counter's own ...
- `/map?designate=1`  [local](http://localhost:5073/map?designate=1) [live](https://esoinila.github.io/SpaceSails-play/map?designate=1)
  THE WHOLE MANUAL-FIRE LOOP, AT THE SHUTTER IT WAS WRITTEN FOR (#803).
- `/map?parkwalk=1`  [local](http://localhost:5073/map?parkwalk=1) [live](https://esoinila.github.io/SpaceSails-play/map?parkwalk=1)
  BOOT THE CROSSING (#775, WIDENED #813) — THE PARK IS A THOROUGHFARE, not a cul-de-sac. -- see: walk the spine, turn down the gate, cross the gravel, and come out on a DIFFERENT STREET — the west end, the east end, or the back street behind the ...
- `/map?parkback=1`  [local](http://localhost:5073/map?parkback=1) [live](https://esoinila.github.io/SpaceSails-play/map?parkback=1)
  BOOT THE FAR SIDE OF THE GREEN (#801, RE-ANCHORED #813) — the same B1 route as `?park=1`, walked one leg further: across the gravel to the wall that used to be the painted horizon. -- see: doors in the far wall, one per room, four of them on the ...
- `/map?ringoffice=1`  [local](http://localhost:5073/map?ringoffice=1) [live](https://esoinila.github.io/SpaceSails-play/map?ringoffice=1)
  BOOT THE OTHER SIDE OF THE GLASS (#813) — the same B1 route as `?park=1`, with the last leg walked INTO one of the rooms that faces the park, a few paces back from its own window wall. -- see: you are indoors, in a poured box off a street, and the ...
- `/map?goodscar=1`  [local](http://localhost:5073/map?goodscar=1) [live](https://esoinila.github.io/SpaceSails-play/map?goodscar=1)
  BOOT THE SECOND CAR (#801) — B1, standing at the OTHER lift. -- see: an alcove in the lower face of the main corridor, at its blind end — past the last cross corridor, about 170 du from the cage, which is the length of the building. The console ...
- `/map?rip=1`  [local](http://localhost:5073/map?rip=1) [live](https://esoinila.github.io/SpaceSails-play/map?rip=1)
  BOOT THE DISPOSAL LOOP (#798) — rip it and bin it, in thirty seconds. -- see: press `[I]` and every paper row now carries a 🗑 beside 🫳 and ✍. Press it. The sheet is torn up and gone from the sleeve — not dropped, so `TryPickUpWhatYouLeft` cannot ...
- `/map?threads=1`  [local](http://localhost:5073/map?threads=1) [live](https://esoinila.github.io/SpaceSails-play/map?threads=1)
  BOOT THE RED PEN, WITH A CASE ALREADY IN THE BOOK (#741). -- see: the notes are now title-first nodes, collapsed, each with a caret and either a 🧵 count or the words loose end. Press one to fold it open into bullets (the full first sentence is the ...
- `/map?roll=hi`  [local](http://localhost:5073/map?roll=hi) [live](https://esoinila.github.io/SpaceSails-play/map?roll=hi)
- `/map?roll=lo`  [local](http://localhost:5073/map?roll=lo) [live](https://esoinila.github.io/SpaceSails-play/map?roll=lo)
  FORCE THE ENCOUNTER BAND (#746). `hi` makes every rolled move land YES, `lo` makes it NO — AND THE SCENE MOVES; `mid` forces YES, BUT. Owner, in the issue: "testing is a feature." It overrides the BAND and never the roll — the dice still cast, the ...
- `/map?tender=flash`  [local](http://localhost:5073/map?tender=flash) [live](https://esoinila.github.io/SpaceSails-play/map?tender=flash)
  FORCE THE TENDER'S RARE ROLL (#1022). -- see: open the card on `/map?tender=flash` — the announcement is drawn apart from his voice (indented, italic, brass) with the recovery beneath it; shut the card and open it again and he is himself, because ...
- `/map?special=story`  [local](http://localhost:5073/map?special=story) [live](https://esoinila.github.io/SpaceSails-play/map?special=story)
  FORCE THE BOARD'S RARE OUTCOME (#247). -- see: on `/map?ashore=1&dock=ringside-exchange&special=story`, press `[E]` at the counter and then 🍽 TODAY'S SPECIAL on the green-ruled board above the buttons — the purse drops by the chalked price and the ...
- `/map?stool=1`  [local](http://localhost:5073/map?stool=1) [live](https://esoinila.github.io/SpaceSails-play/map?stool=1)
  BOOT SITTING AT THE COUNTER (#756) — the high chairs the owner asked for. -- see: the plate reads 🪑 STOOL n · THE COUNTER; the picture on the card is no longer the bar desk but the window wall and the park behind it (#759), because standing you look ...
- `/map?neighbour=1`  [local](http://localhost:5073/map?neighbour=1) [live](https://esoinila.github.io/SpaceSails-play/map?neighbour=1)
- `/map?neighbour=0`  [local](http://localhost:5073/map?neighbour=0) [live](https://esoinila.github.io/SpaceSails-play/map?neighbour=0)
  FORCE WHETHER THE ONE BESIDE YOU TURNS (#756).
- `/map?tablescene=free`  [local](http://localhost:5073/map?tablescene=free) [live](https://esoinila.github.io/SpaceSails-play/map?tablescene=free)
  BOOT STANDING AT A TABLE WITH NOBODY AT IT (#757) — the table the owner could not sit down at.
- `/map?approach=1`  [local](http://localhost:5073/map?approach=1) [live](https://esoinila.github.io/SpaceSails-play/map?approach=1)
- `/map?approach=0`  [local](http://localhost:5073/map?approach=0) [live](https://esoinila.github.io/SpaceSails-play/map?approach=0)
  FORCE WHETHER ANYBODY CROSSES THE ROOM (#757).
- `/map?tablescene=free&nerve=low&hurt=3`  [local](http://localhost:5073/map?tablescene=free&nerve=low&hurt=3) [live](https://esoinila.github.io/SpaceSails-play/map?tablescene=free&nerve=low&hurt=3)
  THE SHORT REST, WATCHABLE (#784) — the same free table, with a captain who has something to get back.
- `/map?patrol=1`  [local](http://localhost:5073/map?patrol=1) [live](https://esoinila.github.io/SpaceSails-play/map?patrol=1)
- `/map?patrol=2`  [local](http://localhost:5073/map?patrol=2) [live](https://esoinila.github.io/SpaceSails-play/map?patrol=2)
  BOOT ONTO A FLOOR WITH A ROUND ON IT (#804) — B2 of a deep site, which is the first floor under the bar and therefore the first with a security ROTA walking it. Owner: "the rotating guards on the lower more restricted levels… ideally we could see ...
- `/map?badge=1`  [local](http://localhost:5073/map?badge=1) [live](https://esoinila.github.io/SpaceSails-play/map?badge=1)
  MINT THIS SITE'S OWN PASS AND PUT YOU IN FRONT OF SOMEBODY WHO READS IT (#804).
- `?watch=N`  (template - fill in the value; no fixed link)
  PIN WHICH SHIFT THE HALL IS ON (#751). The B1 cantina hall holds eighty and how many of its twenty tables are taken varies BY WATCH — a heaving day watch, a small-hours watch of a dozen souls — and
- `/map?perf=1`  [local](http://localhost:5073/map?perf=1) [live](https://esoinila.github.io/SpaceSails-play/map?perf=1)
  ARM THE DRAW-COST PROBE (#841, Lab 46) — the one measurement this repo has never had.
- `/map?holdbeats=1`  [local](http://localhost:5073/map?holdbeats=1) [live](https://esoinila.github.io/SpaceSails-play/map?holdbeats=1)
  HOLD THE STORY CARDS WHILE A SCRIPT DRIVES A BOARD (#1148) — a latch for automation, not a way to play.

### Walking the found halls — ?found=1 (#677)

- `/map?found=1&land=1`  [local](http://localhost:5073/map?found=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?found=1&land=1)
  the lift head, wallet full — ride down and read the building
- `/map?found=1&land=1&floor=9`  [local](http://localhost:5073/map?found=1&land=1&floor=9) [live](https://esoinila.github.io/SpaceSails-play/map?found=1&land=1&floor=9)
  B9, the band nobody listed (a clinic under a laboratory)
- `/map?found=1&land=1&floor=12`  [local](http://localhost:5073/map?found=1&land=1&floor=12) [live](https://esoinila.github.io/SpaceSails-play/map?found=1&land=1&floor=12)
  its deepest floor — the thing on the pallet is in room one
- `/map?found=1&land=1&floor=17`  [local](http://localhost:5073/map?found=1&land=1&floor=17) [live](https://esoinila.github.io/SpaceSails-play/map?found=1&land=1&floor=17)
  the FIRST gallery: dark, sealed, and nothing says why
- `/map?found=1&land=1&floor=20`  [local](http://localhost:5073/map?found=1&land=1&floor=20) [live](https://esoinila.github.io/SpaceSails-play/map?found=1&land=1&floor=20)
  the deepest gallery — the chambers are visibly bigger here
- `/map?found=1&land=1&floor=14`  [local](http://localhost:5073/map?found=1&land=1&floor=14) [live](https://esoinila.github.io/SpaceSails-play/map?found=1&land=1&floor=14)
  a floor inside the band of NOTHING (B13–B16): no rock — you are set down on the

### The ground that was filled in while you were away — ?buried=1 (#1063)

- `/map?buried=1&land=1`  [local](http://localhost:5073/map?buried=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?buried=1&land=1)
  the same rock as ?found=1, one shift later

### The working that was closed while you were away — ?stopped=1 (#1074)

- `/map?stopped=1&land=1`  [local](http://localhost:5073/map?stopped=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?stopped=1&land=1)
  the same rock as ?found=1, closed by order

### The site that passed into official care — ?preserved=1 (#1074 beat 2)

- `/map?preserved=1&land=1`  [local](http://localhost:5073/map?preserved=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?preserved=1&land=1)
  the same rock, one shift further along

### The charter hull — ?shuttle=1 (#1074 beat 5)

- `/map?shuttle=1&land=1`  [local](http://localhost:5073/map?shuttle=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?shuttle=1&land=1)
  the preserved rock, the charter hull parked, her shuttle back

### Ledger Point, the dead station — ?station=1 (#653 slice 1)

- `/map?station=1&land=1`  [local](http://localhost:5073/map?station=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?station=1&land=1)
  board her at the crew lock (dev-station-6: Habitat = a lock, Reactor = a face to cut)

### Reading the whole shelf — ?book=N (#701)

- `/map?secretlab=deep&land=1&floor=2&book=1`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=2&book=1) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=2&book=1)
  the oldest sea story
- `/map?secretlab=deep&land=1&floor=2&book=4`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=2&book=4) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=2&book=4)
  the catalog slip (the one that is not a book)
- `/map?secretlab=deep&land=1&floor=2&book=8`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=2&book=8) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=2&book=8)
  the mechanics text, 27th edition
- `/map?secretlab=deep&land=1&floor=2&book=10`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=2&book=10) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=2&book=10)
  the fat paperback
- `/map?secretlab=deep&land=1&floor=2&book=on`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=2&book=on) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=2&book=on)
  whatever each room's own seed picked

### Dying on purpose — ?death=<cause> (#621)

- `/map?death=impact`  [local](http://localhost:5073/map?death=impact) [live](https://esoinila.github.io/SpaceSails-play/map?death=impact)
  the ship into a world at speed
- `/map?death=collector`  [local](http://localhost:5073/map?death=collector) [live](https://esoinila.github.io/SpaceSails-play/map?death=collector)
  CAUGHT — the demand card, then SUBMIT / BRIBE / RESIST
- `/map?death=collector&dock=selene-gate`  [local](http://localhost:5073/map?death=collector&dock=selene-gate) [live](https://esoinila.github.io/SpaceSails-play/map?death=collector&dock=selene-gate)
  …the same catch, from a berth with muscle in reach (#777)
- `/map?death=suffocated&dock=the-tilt&land=1`  [local](http://localhost:5073/map?death=suffocated&dock=the-tilt&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?death=suffocated&dock=the-tilt&land=1)
  the tank runs dry on the regolith
- `/map?death=reevers&dock=the-tilt&land=1`  [local](http://localhost:5073/map?death=reevers&dock=the-tilt&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?death=reevers&dock=the-tilt&land=1)
  the Old Ones take you on the ground
- `/map?death=suffocated&wreck=1&land=1`  [local](http://localhost:5073/map?death=suffocated&wreck=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?death=suffocated&wreck=1&land=1)
  the tank runs dry inside a dead hull
- `/map?death=reevers&wreck=1&land=1`  [local](http://localhost:5073/map?death=reevers&wreck=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?death=reevers&wreck=1&land=1)
  something has you against a bulkhead
- `/map?death=suffocated&secretlab=1&land=1&floor=2`  [local](http://localhost:5073/map?death=suffocated&secretlab=1&land=1&floor=2) [live](https://esoinila.github.io/SpaceSails-play/map?death=suffocated&secretlab=1&land=1&floor=2)
  150 m under a moon, in a poured corridor
- `/map?death=scuttled&wreck=1&land=1`  [local](http://localhost:5073/map?death=scuttled&wreck=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?death=scuttled&wreck=1&land=1)
  the overload you set yourself ran out (#525)
- `/map?death=void`  [local](http://localhost:5073/map?death=void) [live](https://esoinila.github.io/SpaceSails-play/map?death=void)
  twenty days adrift ran out (#638) — her own deck only

### The salvage run — ?wreck=1 / ?wreck=<cause> (#488)

- `/map?wreck=infested&land=1`  [local](http://localhost:5073/map?wreck=infested&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?wreck=infested&land=1)
  ← something is still aboard; GATE-1 is live in her airlock
- `/map?wreck=insurancejob&land=1`  [local](http://localhost:5073/map?wreck=insurancejob&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?wreck=insurancejob&land=1)
  ← a staged loss, dressed as an ordinary drive failure
- `/map?wreck=mutiny&land=1`  [local](http://localhost:5073/map?wreck=mutiny&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?wreck=mutiny&land=1)
  ← the barricade weave down the spine
- `/map?wreck=hullbreach&land=1`  [local](http://localhost:5073/map?wreck=hullbreach&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?wreck=hullbreach&land=1)
  ← the two holes it made going through her

### The archive node — ?archive=1

- `/map?archive=1&land=1`  [local](http://localhost:5073/map?archive=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?archive=1&land=1)
  board the hull that is carrying one, straight from the URL

### THE DEATH WHERE NOBODY COMES — ?nopattern=1 (#640)

- `/map?nopattern=1&death=impact`  [local](http://localhost:5073/map?nopattern=1&death=impact) [live](https://esoinila.github.io/SpaceSails-play/map?nopattern=1&death=impact)
  on her own deck — the shortest road
- `/map?nopattern=1&death=collector`  [local](http://localhost:5073/map?nopattern=1&death=collector) [live](https://esoinila.github.io/SpaceSails-play/map?nopattern=1&death=collector)
  the BUSTED ladder
- `/map?nopattern=1&death=suffocated&dock=the-tilt&land=1`  [local](http://localhost:5073/map?nopattern=1&death=suffocated&dock=the-tilt&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?nopattern=1&death=suffocated&dock=the-tilt&land=1)
  a landing party

### The quantized nerve — reading the pips and the ledger (#480)

- `/map?nerve=1&dock=the-tilt&site=0&land=1&reevers=1`  [local](http://localhost:5073/map?nerve=1&dock=the-tilt&site=0&land=1&reevers=1) [live](https://esoinila.github.io/SpaceSails-play/map?nerve=1&dock=the-tilt&site=0&land=1&reevers=1)
  one pip left, one hand inbound — the overdraw break
- `/map?nerve=3&dock=the-space-bar&body=phobos&site=0&land=1`  [local](http://localhost:5073/map?nerve=3&dock=the-space-bar&body=phobos&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?nerve=3&dock=the-space-bar&body=phobos&site=0&land=1)
  the monolith's three-pip lump, onto a frayed captain
- `/map?nerve=2&archive=1&land=1`  [local](http://localhost:5073/map?nerve=2&archive=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?nerve=2&archive=1&land=1)
  the archive node's dwell with almost nothing to spend
- `/map?nerve=0&dock=the-tilt&site=0&land=1`  [local](http://localhost:5073/map?nerve=0&dock=the-tilt&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?nerve=0&dock=the-tilt&site=0&land=1)
  the SHOT band and its readout, from a standing start

### PROJEKTI KAAMOS (arc 1) — ?kaamos= (#411)

- `/map?ashore=1&kaamos=bounce`  [local](http://localhost:5073/map?ashore=1&kaamos=bounce) [live](https://esoinila.github.io/SpaceSails-play/map?ashore=1&kaamos=bounce)
  Press `[E]` at any bar patron. A freight agent offers 350 cr to put your own hull's number on a consignment that has come back four times.
- `/map?kaamos=hq&land=1`  [local](http://localhost:5073/map?kaamos=hq&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=hq&land=1)
  …and the shuttle takes it from there, boots on the ice in one URL.
- `/map?kaamos=hq&land=1&floor=23`  [local](http://localhost:5073/map?kaamos=hq&land=1&floor=23) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=hq&land=1&floor=23)
  Straight to B23 · THE WINTERING HALL — the card, and the biggest nerve throw in the game (40 of 100).
- `/map?kaamos=hq&land=1&floor=24`  [local](http://localhost:5073/map?kaamos=hq&land=1&floor=24) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=hq&land=1&floor=24)
  B24 · THE BERTH OFFICE — the console that has never stopped filing.
- `/map?kaamos=hq&land=1&floor=12&nerve=10`  [local](http://localhost:5073/map?kaamos=hq&land=1&floor=12&nerve=10) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=hq&land=1&floor=12&nerve=10)
  B12 · THE STANDING ORDER — search the first room off the nearest rib for the one sheet worth carrying out.
- `/map?kaamos=all&ashore=1`  [local](http://localhost:5073/map?kaamos=all&ashore=1) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=all&ashore=1)
  The other end: the berth-code resolves at the bar seam, the ❄❄ notice fires, 📰 THE STORY BREAKS raises the arc-news card, and a housekeeping line lands on the wire.
- `/map?kaamos=hq&arrivalphase=2&land=1&floor=23`  [local](http://localhost:5073/map?kaamos=hq&arrivalphase=2&land=1&floor=23) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=hq&arrivalphase=2&land=1&floor=23)
  #742 · the arrival phase that used to drift into the moon.
- `/map?kaamos=hq&arrivalphase=2`  [local](http://localhost:5073/map?kaamos=hq&arrivalphase=2) [live](https://esoinila.github.io/SpaceSails-play/map?kaamos=hq&arrivalphase=2)
  The standoff holds near 1e7 m and the range readout stays there.

### The head office under the ice (#411, the owner's 2026-08-03 ruling)

- `/map?dock=the-tilt&site=0&land=1&kaamos=pod`  [local](http://localhost:5073/map?dock=the-tilt&site=0&land=1&kaamos=pod) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&site=0&land=1&kaamos=pod)
  Land, take out the metal detector, probe any square: the cold supply pod is under this ground.
- `/map?dock=ringside-exchange&kaamos=holder`  [local](http://localhost:5073/map?dock=ringside-exchange&kaamos=holder) [live](https://esoinila.github.io/SpaceSails-play/map?dock=ringside-exchange&kaamos=holder)
  Walk to the counter and the barkeep card carries 🌑 Ask about KAAMOS — the berth-holder is in, this watch, at this bar.

### NEBULA MUTUAL (arc 2) and THE CONVERGENCE — ?nebula= / ?converge=1 (#422)

- `/map?dock=the-space-bar&nebula=adjuster`  [local](http://localhost:5073/map?dock=the-space-bar&nebula=adjuster) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-space-bar&nebula=adjuster)
  Walk to the counter and the barkeep card carries ▓ Ask about NEBULA — the Nebula Mutual adjuster is in, this watch, at this bar.
- `/map?oldcrew=1`  [local](http://localhost:5073/map?oldcrew=1) [live](https://esoinila.github.io/SpaceSails-play/map?oldcrew=1)
  #973 L5a · THE OLD CREW. Ashore at the bar with the four shipmates this universe cast working THIS berth, and one captain already in the ground.
- `/map?tailed=1`  [local](http://localhost:5073/map?tailed=1) [live](https://esoinila.github.io/SpaceSails-play/map?tailed=1)
  #1062 slice 2 · SOMEBODY IS BEHIND YOU.
- `/map?nebula=all`  [local](http://localhost:5073/map?nebula=all) [live](https://esoinila.github.io/SpaceSails-play/map?nebula=all)
  Arc 2 reaches the wire (#663). The truth resolves, and with it 📰 THE STORY BREAKS — the same beat arc 1 raises on the berth-listing edge.

### Already in the bar — ?ashore=1 (#428)

- `/map?oracle=1&ashore=1`  [local](http://localhost:5073/map?oracle=1&ashore=1) [live](https://esoinila.github.io/SpaceSails-play/map?oracle=1&ashore=1)
  the rant: one URL, one [E]
- `/map?ashore=1&nebula=adjuster`  [local](http://localhost:5073/map?ashore=1&nebula=adjuster) [live](https://esoinila.github.io/SpaceSails-play/map?ashore=1&nebula=adjuster)
  arc 2's best beat, at the counter
- `/map?ashore=1&kaamos=holder&dock=ringside-exchange`  [local](http://localhost:5073/map?ashore=1&kaamos=holder&dock=ringside-exchange) [live](https://esoinila.github.io/SpaceSails-play/map?ashore=1&kaamos=holder&dock=ringside-exchange)
- `/map?ashore=1&bond=1`  [local](http://localhost:5073/map?ashore=1&bond=1) [live](https://esoinila.github.io/SpaceSails-play/map?ashore=1&bond=1)
  stand still; the forced scare opens the cognac beat
- `/map?ashore=1&simhours=9&dock=cinder-roost`  [local](http://localhost:5073/map?ashore=1&simhours=9&dock=cinder-roost) [live](https://esoinila.github.io/SpaceSails-play/map?ashore=1&simhours=9&dock=cinder-roost)
  the Magpie's third stop, at the back room

### Hiding from a sweep — ?sweep=N and the captain's remote (#538)

- `/map?wreck=mutiny&land=1&sweep=3`  [local](http://localhost:5073/map?wreck=mutiny&land=1&sweep=3) [live](https://esoinila.github.io/SpaceSails-play/map?wreck=mutiny&land=1&sweep=3)
  ← the same team on a hull that has no reason to be guarded

### The surface tour — every landing site, one URL each (#585)

- `/map?dock=cinder-roost`  [local](http://localhost:5073/map?dock=cinder-roost) [live](https://esoinila.github.io/SpaceSails-play/map?dock=cinder-roost)
  system: Venus; landable from it: the-clinker
- `/map?dock=selene-gate`  [local](http://localhost:5073/map?dock=selene-gate) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate)
  system: Earth; landable from it: luna
- `/map?dock=the-space-bar`  [local](http://localhost:5073/map?dock=the-space-bar) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-space-bar)
  system: Mars; landable from it: phobos
- `/map?dock=red-eye&simhours=78`  [local](http://localhost:5073/map?dock=red-eye&simhours=78) [live](https://esoinila.github.io/SpaceSails-play/map?dock=red-eye&simhours=78)
  system: Jupiter; landable from it: europa (the clock wound to where the two orbits line up)
- `/map?start=callisto`  [local](http://localhost:5073/map?start=callisto) [live](https://esoinila.github.io/SpaceSails-play/map?start=callisto)
  system: Jupiter; landable from it: callisto — a free park beside the moon, one hop out (#1318)
- `/map?dock=ringside-exchange`  [local](http://localhost:5073/map?dock=ringside-exchange) [live](https://esoinila.github.io/SpaceSails-play/map?dock=ringside-exchange)
  system: Saturn; landable from it: titan
- `/map?start=enceladus`  [local](http://localhost:5073/map?start=enceladus) [live](https://esoinila.github.io/SpaceSails-play/map?start=enceladus)
  Saturn
- `/map?dock=the-tilt`  [local](http://localhost:5073/map?dock=the-tilt) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt)
  system: Uranus; landable from it: miranda
- `/map?dock=the-deep`  [local](http://localhost:5073/map?dock=the-deep) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep)
  system: Neptune; landable from it: triton
- `/map?dock=the-tilt&body=miranda&site=0&land=1`  [local](http://localhost:5073/map?dock=the-tilt&body=miranda&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&body=miranda&site=0&land=1)
  The Wild Plain
- `/map?dock=the-tilt&body=miranda&site=1&land=1`  [local](http://localhost:5073/map?dock=the-tilt&body=miranda&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&body=miranda&site=1&land=1)
  The Shadowed Rille
- `/map?dock=the-tilt&body=miranda&site=2&land=1`  [local](http://localhost:5073/map?dock=the-tilt&body=miranda&site=2&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&body=miranda&site=2&land=1)
  The Ridge Camp
- `/map?dock=selene-gate&body=luna&site=0&land=1`  [local](http://localhost:5073/map?dock=selene-gate&body=luna&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&body=luna&site=0&land=1)
  The Wild Plain
- `/map?dock=selene-gate&body=luna&site=1&land=1`  [local](http://localhost:5073/map?dock=selene-gate&body=luna&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&body=luna&site=1&land=1)
  The Depot Apron
- `/map?dock=selene-gate&body=luna&site=2&land=1`  [local](http://localhost:5073/map?dock=selene-gate&body=luna&site=2&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&body=luna&site=2&land=1)
  The Derelict Pad
- `/map?dock=selene-gate&body=luna&site=3&land=1`  [local](http://localhost:5073/map?dock=selene-gate&body=luna&site=3&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=selene-gate&body=luna&site=3&land=1)
  The Shadowed Rille
- `/map?dock=the-space-bar&body=phobos&site=0&land=1`  [local](http://localhost:5073/map?dock=the-space-bar&body=phobos&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-space-bar&body=phobos&site=0&land=1)
  The Wild Plain — THE MONOLITH (#649: the one object, on the one ground)
- `/map?dock=the-space-bar&body=phobos&site=1&land=1`  [local](http://localhost:5073/map?dock=the-space-bar&body=phobos&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-space-bar&body=phobos&site=1&land=1)
  The Ice Fissure
- `/map?dock=the-space-bar&body=phobos&site=2&land=1`  [local](http://localhost:5073/map?dock=the-space-bar&body=phobos&site=2&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-space-bar&body=phobos&site=2&land=1)
  The Ridge Camp
- `/map?dock=the-space-bar&body=phobos&site=3&land=1`  [local](http://localhost:5073/map?dock=the-space-bar&body=phobos&site=3&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-space-bar&body=phobos&site=3&land=1)
  The Crater Shelf
- `/map?dock=red-eye&simhours=78&body=europa&site=0&land=1`  [local](http://localhost:5073/map?dock=red-eye&simhours=78&body=europa&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=red-eye&simhours=78&body=europa&site=0&land=1)
  The Wild Plain
- `/map?dock=red-eye&simhours=78&body=europa&site=1&land=1`  [local](http://localhost:5073/map?dock=red-eye&simhours=78&body=europa&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=red-eye&simhours=78&body=europa&site=1&land=1)
  The Ice Fissure
- `/map?dock=red-eye&body=ganymede&site=0&land=1`  [local](http://localhost:5073/map?dock=red-eye&body=ganymede&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=red-eye&body=ganymede&site=0&land=1)
  The Wild Plain
- `/map?dock=red-eye&body=ganymede&site=1&land=1`  [local](http://localhost:5073/map?dock=red-eye&body=ganymede&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=red-eye&body=ganymede&site=1&land=1)
  The Ridge Camp
- `/map?start=callisto&body=callisto&site=0&land=1`  [local](http://localhost:5073/map?start=callisto&body=callisto&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?start=callisto&body=callisto&site=0&land=1)
  The Wild Plain
- `/map?start=callisto&body=callisto&site=1&land=1`  [local](http://localhost:5073/map?start=callisto&body=callisto&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?start=callisto&body=callisto&site=1&land=1)
  The Ice Fissure
- `/map?dock=ringside-exchange&body=titan&site=0&land=1`  [local](http://localhost:5073/map?dock=ringside-exchange&body=titan&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=ringside-exchange&body=titan&site=0&land=1)
  The Wild Plain
- `/map?dock=ringside-exchange&body=titan&site=1&land=1`  [local](http://localhost:5073/map?dock=ringside-exchange&body=titan&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=ringside-exchange&body=titan&site=1&land=1)
  The Quiet Basin
- `/map?start=enceladus&body=enceladus&site=0&land=1`  [local](http://localhost:5073/map?start=enceladus&body=enceladus&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?start=enceladus&body=enceladus&site=0&land=1)
  The Wild Plain
- `/map?start=enceladus&body=enceladus&site=1&land=1`  [local](http://localhost:5073/map?start=enceladus&body=enceladus&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?start=enceladus&body=enceladus&site=1&land=1)
  The Derelict Pad
- `/map?dock=the-deep&body=triton&site=0&land=1`  [local](http://localhost:5073/map?dock=the-deep&body=triton&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&body=triton&site=0&land=1)
  The Wild Plain
- `/map?dock=the-deep&body=triton&site=1&land=1`  [local](http://localhost:5073/map?dock=the-deep&body=triton&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&body=triton&site=1&land=1)
  The Shadowed Rille
- `/map?dock=the-deep&body=triton&site=2&land=1`  [local](http://localhost:5073/map?dock=the-deep&body=triton&site=2&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&body=triton&site=2&land=1)
  The Derelict Pad
- `/map?dock=the-deep&body=triton&site=3&land=1`  [local](http://localhost:5073/map?dock=the-deep&body=triton&site=3&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-deep&body=triton&site=3&land=1)
  The Ice Fissure
- `/map?dock=cinder-roost&body=the-clinker&site=0&land=1`  [local](http://localhost:5073/map?dock=cinder-roost&body=the-clinker&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=cinder-roost&body=the-clinker&site=0&land=1)
  The Wild Plain
- `/map?dock=cinder-roost&body=the-clinker&site=1&land=1`  [local](http://localhost:5073/map?dock=cinder-roost&body=the-clinker&site=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=cinder-roost&body=the-clinker&site=1&land=1)
  The Depot Apron

### What to look for

- `/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12`  [local](http://localhost:5073/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12)
- `/map?dock=the-tilt&site=0&land=1`  [local](http://localhost:5073/map?dock=the-tilt&site=0&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&site=0&land=1)
- `/map?dock=the-tilt&site=0&land=1&reevers=4`  [local](http://localhost:5073/map?dock=the-tilt&site=0&land=1&reevers=4) [live](https://esoinila.github.io/SpaceSails-play/map?dock=the-tilt&site=0&land=1&reevers=4)

### The Hive — reaching an underground facility without playing for it (#585)

- `/map?secretlab=1&land=1`  [local](http://localhost:5073/map?secretlab=1&land=1) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1)
  at the lift head, a pace outside its door
- `/map?secretlab=1&land=1&floor=4`  [local](http://localhost:5073/map?secretlab=1&land=1&floor=4) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1&floor=4)
  on B4, dead air, tank running
- `/map?secretlab=1&land=1&floor=20`  [local](http://localhost:5073/map?secretlab=1&land=1&floor=20) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1&floor=20)
  as deep as that site goes (clamped to its real depth)

### The card, the row and the gate — ?card= (#693)

- `/map?secretlab=deep&land=1&floor=1&card=next`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=1&card=next) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=1&card=next)
  B1 with the paper the gate downstairs reads
- `/map?secretlab=deep&land=1&floor=1&card=3`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=1&card=3) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=1&card=3)
  B1 with the WRONG band's paper — the refusal names it
- `/map?secretlab=deep&land=1&floor=1`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=1) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=1)
  B1 with nothing — the sealed row, exactly as it ships

### The card that lands on top of the beat — /map?secretlab=deep&land=1&card=next (#768)

- `/map?secretlab=deep&land=1&card=next`  [local](http://localhost:5073/map?secretlab=deep&land=1&card=next) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&card=next)
  set down at the shed, the first gate's authority in the wallet

### The inhabited Hive (2026-08-05) — #707, #708, #701, #677, #709, #721

- `/map?tablescene=1&watch=2`  [local](http://localhost:5073/map?tablescene=1&watch=2) [live](https://esoinila.github.io/SpaceSails-play/map?tablescene=1&watch=2)
  …the same hall on the heaving watch (&watch=5 for the small one)
- `/map?stool=1&neighbour=1`  [local](http://localhost:5073/map?stool=1&neighbour=1) [live](https://esoinila.github.io/SpaceSails-play/map?stool=1&neighbour=1)
  …up on a STOOL at that counter (#756): the park in the window, WAIT and she turns
- `/map?stool=1&neighbour=0`  [local](http://localhost:5073/map?stool=1&neighbour=0) [live](https://esoinila.github.io/SpaceSails-play/map?stool=1&neighbour=0)
  …the same stool where nobody turns, and the counter says so in words
- `/map?tablescene=free&approach=1`  [local](http://localhost:5073/map?tablescene=free&approach=1) [live](https://esoinila.github.io/SpaceSails-play/map?tablescene=free&approach=1)
  …at a FREE top (#757): [E] SITS YOU DOWN, SIT A WHILE brings her over
- `/map?tablescene=free&watch=5&approach=0`  [local](http://localhost:5073/map?tablescene=free&watch=5&approach=0) [live](https://esoinila.github.io/SpaceSails-play/map?tablescene=free&watch=5&approach=0)
  …the same table on the quiet watch: nobody comes, and it is a REST (#783)
- `/map?tablescene=free&watch=2&approach=0`  [local](http://localhost:5073/map?tablescene=free&watch=2&approach=0) [live](https://esoinila.github.io/SpaceSails-play/map?tablescene=free&watch=2&approach=0)
  …and the heaving watch, where the same sit is back-to-the-wall
- `/map?park=1&parkphase=night`  [local](http://localhost:5073/map?park=1&parkphase=night) [live](https://esoinila.github.io/SpaceSails-play/map?park=1&parkphase=night)
  …the same park at the bottom of ITS OWN grow-cycle (#759): dim gravel, five dark masts
- `/map?park=1&parkphase=morning`  [local](http://localhost:5073/map?park=1&parkphase=morning) [live](https://esoinila.github.io/SpaceSails-play/map?park=1&parkphase=morning)
  …and standing in it five sim-minutes before its morning comes up, at nobody else's hour
- `/map?secretlab=deep&land=1&floor=17`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=17) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=17)
  B17 — the staff mess, pass-only, hall-sized, and empty on purpose
- `/map?secretlab=deep&land=1&floor=21`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=21) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=21)
  B21 — the unlisted lobby: the car's plate reads B21 · NO PLATE, and on screen at boot the sign
- `/map?secretlab=deep&land=1&floor=4&dark=1`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=4&dark=1) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=4&dark=1)
  a floor with no lights (#708)
- `/map?secretlab=deep&land=1&floor=2&book=9`  [local](http://localhost:5073/map?secretlab=deep&land=1&floor=2&book=9) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=deep&land=1&floor=2&book=9)
  force a specific odd book (#701)

### Useful combinations

- `/map?secretlab=1&land=1&floor=2&air=90`  [local](http://localhost:5073/map?secretlab=1&land=1&floor=2&air=90) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1&floor=2&air=90)
  a dead floor with ninety seconds in the tank
- `/map?secretlab=1&land=1&collectors=20`  [local](http://localhost:5073/map?secretlab=1&land=1&collectors=20) [live](https://esoinila.github.io/SpaceSails-play/map?secretlab=1&land=1&collectors=20)
  a repo boat lands while you are underground

### Add-ons for any ground scene (append to a URL above)

- `&air=45` - 45 seconds in the tank — the point-of-no-return warning without a six-minute stroll
- `&reevers=4` - four Old Ones on top of you, already aware
- `&outpost=1` - guarantee the outpost hut on this ground
- `&shelter=1` - set down at a shelter's door instead of below the pad (#728)
- `&mags=N` - each sentry comes down holding N rounds rather than a full 99 (#728)
- `&kit=1` - the field dossier assembles on the first piece of kit, saying everything it can (#774)
- `&collectors=20` - a repo boat sets down 20 s in, whatever your heat reads (#583)
- `&secretlab=1` - a landable rock with a Vantar lab, hidden door already found (#409)
- `&card=next` - the authority for the gate you will be standing at, in the wallet (#693)
- `&wreck=1` - a derelict wins the toss instead of a moon

---

## 4. Cross-check: the guide against the DevStarts catalog

`src/SpaceSails.Core/DevStarts*.cs` holds 90 `/map?...` rows (the front door's "DEV START SITES" buttons); the guide asks that
the two be kept in step. `DevStartsTests` pins only the catalog's own shape (every row dressed, `/map?` + key=value, no
duplicates) - there is no test that compares it with the guide, so this comparison was done by hand when this file was written.

- ⚠ in DevStarts but with no matching line in the guide: `/map?start=wreck&target=collector` (the guide documents `?target=collector` and `?start=wreck` separately, never together).
- ⚠ in DevStarts but with no matching line in the guide: `/map?tablescene=free&rep=1&approach=0` (the guide documents `?tablescene=free`, `?rep=` and `?approach=` separately).
- Note: two DevStarts rows are written with a `{berth}` placeholder (`ashore=1&garden=1`, `ashore=1&havenfloor=-1`); the guide spells them out per hub (see the #1332 rows).
- Note: the keepsake shelf (section 2, #620) has no entry in the guide yet.
- The remaining DevStarts rows are instances or combinations of guide entries (e.g. `credits=50000` of `?credits=N`; `expedition=mining`, `deflection=1`, `kaamos=all` and the `watchers`, `counter&watch`, `kit`, `perf`, `secretlab=sealed` combinations of keys the guide documents on a line of their own).

