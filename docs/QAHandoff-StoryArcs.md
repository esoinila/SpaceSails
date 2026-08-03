# QA handoff — the story arcs nobody has played

*Written 2026-08-02 for the Fable QA run. Owner is at the gym; the brief is to find real bugs in the arcs
that have shipped but have never been PLAYED.*

---

## 0 · Why this document exists

Every expensive bug on this project so far was **invisible to reasoning and to Core tests, and obvious on
sight.** That is not a slogan, it is the measured record — see the table in
[`features/the-landing-site.md`](features/the-landing-site.md#why-this-document-exists). The owner's method,
stated in his own words, is:

> *"Open EVERY scene and check all the parts are in the right place."*

So this is not a test-writing task. **It is a playing task, and the tests come second** — written only to pin
what playing found, and only after being proven to fail on the broken behaviour.

### The five named bug classes — check these FIRST in any area

| # | class | what it looks like |
| --- | --- | --- |
| 1 | **Unaudited client geometry literals** | numbers typed into a renderer that nothing derives or checks. Found wrong 3 times out of 3. |
| 2 | **A constant governing the wrong thing** | a MOON constant governing a SHIP — 4 occurrences. |
| 3 | **The sim does one thing, the SENTENCE or the DRAWN SHAPE says another** | 3 in one afternoon, all found by playing. A suffocation narrated as a killing; a card described in prose that never entered the pocket. |
| 4 | **One source consumed in the WRONG ORDER** | a list built by appending is not a list in order. Sealed 35 floors' worth of doorways while every test passed. |
| 5 | **A green test that asserts nothing** | the assertion was right; the WORLD handed to it could not tell pass from fail. A guard laid an invented 78 du field on which every body builds nothing; a threshold set to 34 du when the nearest real case the generator can produce is 34.2. Three independent instances in one afternoon — the full table is in [`features/the-landing-site.md`](features/the-landing-site.md#the-fifth-class-a-green-test-that-asserts-nothing). Use `SurfaceLayout.DefaultField`, and measure a threshold against what the generator actually produces. |

### The house laws you may not break

- **Prove a guard can fail.** Before shipping any regression test, revert the fix and watch it go RED.
  A guard that passes on the broken code is worse than none. This has caught a bad guard *and* a bad harness
  in the last day alone.
- **A test is only as honest as the world you hand it.** Two separate incidents: an A\* audit whose goal point
  was inside a wall, and a Hive guard run against an invented 78 du field that reports zero rooms on every
  floor of every site. Use `SurfaceLayout.DefaultField`; verify both endpoints are standable before trusting
  a reachability verdict.
- **Canon.** Nothing in the game ever explains the Old Ones. (They are failed restores — the same brain-backup
  tech that revives the captain, procured at scale. This is *never* stated in a card and *never* confirmed by
  a sensor.) A crew member may speculate; the game may never confirm.
- **Inference horror.** The game does not announce. `SECURITY ALERTED` as a banner is the wrong shape; a lift
  that stops answering is the right one. See [`art-manifest-hive.md`](art-manifest-hive.md).
- **One source of truth.** If two places compute the same fact, that is the bug — even when they currently
  agree.

---

## 1 · Where the code is right now

- **Base branch is `our-own-ship-has-compartments`, NOT `main`.** All PRs target it. CI (`build-and-test`,
  `ui-gate`) runs on every PR — the workflow has a bare `pull_request:` trigger with no branch filter.
- Full suite as of this writing: **~2540 Core + ~21 client.** Run it from the repo root with
  `dotnet test -c Debug`. It takes ~22 minutes.
- The owner playtests from `D:\repo12\spaceSails` with a dev server on **5073**. **Do not touch that working
  tree.** Work in your own worktree under the scratchpad and serve on your own port.
- `dotnet run` (NOT `watch`) — kill and restart the process per build.
- A shared checkout goes stale because PRs squash-merge. `git fetch` and diff against
  `origin/our-own-ship-has-compartments` before branching; three sessions running have had a PR conflict from
  skipping this.

### Landed today (the Hive)

The secret lab became a facility: sealed rooms, the visualiser lab, authority cards, an underground tracker
mode, a band nobody listed, a lift panel that goes UP, department livery and signage, an on-foot satchel with
TRY, lab ammunition with penetration, an underground death card, per-paper titles, and item reveal cards.

**These have all been played by the owner. They are the least likely place to find something new.**

---

## 2 · QA quick starts

The full cheat table is in [`testing-guide.md`](testing-guide.md) — **read it, do not re-derive it.** These
are the fast paths per arc, so nobody has to fly anywhere to test anything.

Dev server: `dotnet run --project src/SpaceSails.Client --urls http://localhost:PORT`, then
`http://localhost:PORT/map?<cheats>`.

> **This project's own rule, written in `Map.Sim.cs` next to these cheats:**
> *"a scene nobody can reach on demand is a scene that ships broken"* and *"Testing is a feature (owner's
> rule)."* That is the authority for the line above: an arc with no quick start is a finding, not an
> inconvenience.

### The Hive / secret lab

```
/map?secretlab=1&land=1           boot straight onto the rock, hidden door pre-revealed
/map?secretlab=deep&land=1        a site with the band nobody listed (#592) — the deep content
/map?secretlab=1&land=1&floor=3   ride STRAIGHT DOWN to B3 (clamped to the site's own bottom)
/map?secretlab=deep&land=1&floor=20&air=90   deep, low on air, on a dead floor
```

**`?floor=N` and `?air=N` are the two that matter most down here** and are easy to miss. A full tank is six
minutes of walking *by design* — fine to play, useless to test — so `?air=45` is how you see the
point-of-no-return warning, the refuges (#608), the vacuum card and the underground death card without
strolling. `?floor=` exists because half the Hive's open work is on floors you would otherwise have to ride
to.

Once down: `E` works everything (doors, consoles, the lift panel, a room worth searching), `I` opens the
satchel, `T` plants a sentry, `G` drops the chest.

### Surfaces — every canon ground, one URL each

```
/map?dock=the-tilt&body=miranda&site=0&land=1        The Wild Plain
/map?dock=the-tilt&body=miranda&site=1&land=1        The Shadowed Rille
/map?dock=the-tilt&body=miranda&site=2&land=1        The Ridge Camp
/map?dock=selene-gate&body=luna&site=0..3&land=1     Luna's four
/map?dock=the-space-bar&body=phobos&site=0&land=1    Phobos
/map?dock=the-tilt&site=0&land=1&reevers=4           a roused ground
```

### Wrecks and boarding

```
/map?wreck=1&land=1                        the default hull
/map?wreck=infested&land=1                 something is still aboard
/map?wreck=insurancejob&land=1             a staged loss dressed as a drive failure
/map?wreck=mutiny&land=1                   the barricade weave down the spine
/map?wreck=hullbreach&land=1               the two holes it made going through her
/map?wreck=ventedbyoneoftheirown&land=1    atmosphere arc
```

### The rest

```
/map?kaamos=N        the KAAMOS plotline (features/KaamosPlotline.md)
/map?kaamos=bounce   its FRONT DOOR — the freight agent whose docket the board keeps returning (#635)
/map?kaamos=hq&land=1  the head office under the ice: the route already ridden, boots on the ground (#411)
  …&floor=23          B23 THE WINTERING HALL · &floor=24 THE BERTH OFFICE · &floor=12 THE STANDING ORDER
/map?nebula=all      the nebula arc (features/NebulaArc.md)
/map?bond=1          the bond
/map?converge=1      arc convergence
/map?deflection=1    a live deflection gig
/map?expedition=1    an away expedition
/map?scenario=wheel  scenario boot
/map?credits=N&fuel=N&simhours=N    set up state without flying to it
```

### The full cheat surface, verified against `Map.Sim.cs` on 2026-08-02

Several of these are not in the testing guide's tables and are the fastest way into scenes that are otherwise
nearly impossible to reach on purpose.

| cheat | what it does |
| --- | --- |
| `?floor=N` | ride straight down to B*N* in a Hive; clamped to the site's own bottom |
| `?air=N` | start the excursion with *N* seconds of tank instead of a full one |
| `?collectors=N` | force a repo boat to follow you down and land *N* seconds in, whatever the heat gauge says — the scene is deliberately rare and mid-mission, i.e. *"nearly impossible to playtest on purpose"* |
| `?outpost=1` | guarantee the outpost hut on whatever site you land on |
| `?hoard=mine\|rumor\|both` | seed the ledger's 🗺 section — map card and dig doors without a bury run |
| `?fetch=intel\|active\|picked` | inject the fetch mission at a given leg |
| `?crack=active\|picked` | inject the hatch-crack job at a stage (pair with `?start=<station>`) |
| `?backroom=open\|quest` | weld V-06's back room open, or stage the crack job with its real code |
| `?tip=route` | seed a route tip with provenance into the ledger |
| `?reveal=<bodyId>` | chart a hidden body immediately (repeatable) |
| `?skim=<bodyId>` | boot a hyperbolic inbound grazing that body's cloud tops (needs an atmosphere: jupiter, earth, venus, saturn, titan) |
| `?sling=<bodyId>` | boot an inbound arc with a close pass ~12 days out |
| `?ellipse=1` | append one visibly eccentric body for the Kepler rails |
| `?start=<station>` | boot docked at a named station |
| `?ashore=1` | boot docked **and already standing in the bar** — the ship → tube → hall walk already walked, so every bar beat is one URL away even in a tab where WASD cannot land |
| `?nerve=N` | seed the nerve gauge at *N* of ten whole pips at boot — the only way to reach a sanity beat without being hunted for minutes first |

**If an arc has no quick start, saying so is itself a finding — file it.** An arc that can only be reached by
playing for an hour is an arc that will never be regression-tested again.

---

## 3 · The coverage map — what to actually go and play

Ordered by *likely payoff*, which is roughly *how long it has been shipped without anybody walking through
it.*

| priority | area | why it is suspect | issues |
| --- | --- | --- | --- |
| **1** | **Boardable derelicts, sectioned hulls, the shuttle hop** | large, geometric, and the exact profile of bug class 1 and 4. The Hive's worst bug (35 floors of sealed doorways) was this same generator family. | #488, #531, #533, #537 |
| **2** | **The black-ops sweep + keys** | shipped, never played end to end; "hide while somebody else's team sweeps" is stateful and stateful things drift. | #538, #535 |
| **3** | **Q-ships and the tells** | the tells must be readable BEFORE committing — a tell you can only confirm after the trap is not a tell. | #534 |
| **4** | **Scuttling and the castaway outcome** | an end state almost nobody reaches; end states rot silently. | #525 |
| **5** | **Damage control, the charge board, venting** | three instrument panels that must agree with one sim. Prime bug class 3. | #524, #523 |
| **6** | **The Reever observation roll** | "seeing you is a ROLL" — verify the moment of discovery is legible and that the roll is not re-rolled per frame. | #436 |
| **7** | **Chain-of-custody dread / the Cantina magician** | narrative set pieces; check canon holds and nothing explains the Old Ones. | #426, #432 |
| **8** | **Nebula Mutual / second arc convergence** | the longest arc, the least walked. | #422, #553 |

### Specific things to look for, learned the hard way

- **Walk to everything you can see.** If it is drawn, prove it is reachable. If it is reachable, prove it does
  something. The lift bug (#600) survived three PRs because an A\* audit proves you can REACH a thing, never
  that it is a way BACK.
- **Read every sentence the game prints against what the sim actually did.** That is bug class 3 and it is the
  most common one here.
- **Check both ends of every journey.** Down is not up. In is not out.
- **Open every instrument on every screen and ask what it does NOT say.** The air gauge showed a clock for
  weeks without ever saying whether the clock was running.

---

## 4 · Open decisions the owner has NOT ruled on

These are filed as decisions issues, not build tickets. **Do not build them; sharpen them.** If playing
surfaces the answer, write the answer into the issue with the evidence.

- **#618 — who is guarding the Hive.** Blocks #602 (numpad calls security) and #605 (the badge). A disguise
  is worthless without somebody to fool, and nobody has decided whether anyone is still down there.
- **#619 — the refuge that failed.** One derelict refuge as the inference-horror pair to #608.
- **#620 — admire and discuss.** The collar has a card; nobody can talk about it. Decide whether discussing
  changes anything or is voice only.
- **#615 — keep / leave on a find.** LEAVE must not destroy it.
- **#610 — shooting a door open with lab rounds**, without it becoming a skeleton key.
- **#601 — the funding trail** as the Hive's running joke and its best cover.

---

## 5 · Working method

**Fable orchestrates; Opus subagents do the legwork.** One task per subagent, one worktree each, off
`origin/our-own-ship-has-compartments`. Never point a subagent at `D:\repo12\spaceSails`.

Each finding becomes either:

1. **A PR** — the fix, a guard **proven RED on the broken behaviour**, docs updated in the relevant
   `docs/features/*.md`, full suite green, targeting `our-own-ship-has-compartments`. Standing order: both
   checks green ⇒ squash-merge. Ask only on gameplay-feel judgement calls.
2. **An issue** — when it is a decision rather than a defect. Write it as *what to do / what to decide*, with
   the options and what each one costs, not as a complaint.

Report findings with the evidence attached: the URL that reproduces it, what you expected, what the sim did,
and what the screen said. A finding without a reproduction is a rumour.

### Two traps specific to this environment

- **An MCP-driven browser tab is `document.hidden`** — rAF is throttled and timers are clamped, so `?land=1`
  hangs mid-descent and any timing number measured there is worthless. You cannot drive the game that way.
  Test by reasoning against the code and by targeted harnesses, or ask the owner to foreground a tab.
- **Do not run the full suite or a build while the owner is playtesting** — it fights for the same file locks
  (`Stop-Process -Name testhost -Force` clears a jam).
