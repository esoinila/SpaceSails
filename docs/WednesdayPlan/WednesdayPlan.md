# Wednesday plan — the agenda (2026-07-15)

*Where Tuesday left us: labs 19–22 shipped with pop-ups; the sling, the air brake, and the
corridor gauge are live; the find-the-car hunt runs intel → scan → reveal; the Captain's ledger
files every tip; blind-UI audit round 1 is merged with its first four fixes. Zero open PRs,
291/291 tests, live site current. Today is about answering the parked design questions and
opening the elliptical half of the world.*

## 1. Morning coffee — owner decisions (RULED 2026-07-15, playtest stream)

**Flight assists** (docs/TuesdayPlan/FlightAssists.md):

- **Q1 — RULED: skim damage stays, and the ship gets DAMAGE CONTROL.** The Expanse model: the
  crew can fix basic things. The holed sail isn't a passive 2-day timer — it's a repair job the
  ship is equipped for. (Design follow-up: damage-control as a desk/action; timer can shorten
  with attention, or stay 2 days hands-off.)
- **Q2 + Q4 — RULED: skimming BLEEDS heat, and wolves work a ticket queue.** Hunters have a lot
  of "tickets" open; if they lose sight of you they easily move on to other work. Atmosphere
  stays their blind spot — the skim is the reliable escape, and it REDUCES notoriety heat
  because you vanished off everyone's plot.

**The living solar system** (docs/TuesdayPlan/TuesdayPlanVision.md):

- **Cycler naming — RULED: mythological beings, keep the woman nod.** Flagship proposal:
  **Persephone** — the myth's original commuter, half the year in each of two worlds on a fixed
  timetable (the boomerang the owner asked after: no Mars god IS a boomerang, but the
  returning-woman myths are — Persephone (Greek), Inanna's descent-and-return (Sumerian), the
  Return of the Distant Goddess / Eye of Ra (Egyptian) for sister ships. Mars' own women —
  Nerio, Bellona (Roman) — reserved for warier hulls.)
- **Blown FALSE COLORS — RULED:** blowing cover on a SENSITIVE contract ruins the mission (pay
  voided) and it can bring in heat. No automatic hunter spawn; heat does that work.
- **Secret stations — RULED: add ASTEROID SECRET BASES to the roster.** Rocks on suitable real
  orbits where available; low-spin ones are the prize — ideal stores and material sources
  (fast tumblers are unusable as bases, which is itself a discovery result a scan can report).
- **Dynamic wings** (= runtime-appended station interiors from "Doors that grow the world"):
  per-session persistence accepted for v1; the lane itself is PRIORITY on indoors work — and
  **people cannot be static furniture**: NPCs change place, move between rooms, go behind
  locked doors.

**Housekeeping:** ⚠️ gemini CLI still asks for interactive login (the owner's relog didn't
reach the CLI credential store — run `gemini` once in a real terminal to finish the browser
flow). UI-depth + nav-list notes written as a Fable stand-in meanwhile
(docs/WednesdayPlan/UnifiedNavListNotes.md); the true Gemini pass and
`tools/playthrough/ui-audit-gemini.sh` blind round re-queue after the login.

## 2. The main lane — fix the flying FIRST, then Kepler rails, then the cycler

**PR-A0 · The Saturn playtest bugs (owner: "we need to fix the issues at repo first").**
Issues #123–#128 from this morning's hands-on at the Saturn moons:

- **#127/#128 — autopilot flies THROUGH Saturn** to reach it/Enceladus: add body-avoidance
  (periapsis-above-surface check) to auto-orbit path planning. The actual broken-flying bug.
- **#126 — "orbit Saturn" silently accepted while docked**: disable with a why-tooltip.
- **#127 — WHO IS FLYING THE SHIP must be BIG**: an unmissable pilot-in-command banner
  (MANUAL / AUTOPILOT→target / program name), always visible.
- **#124 — slingshot ghost renders before any slingshot is set**: gate it on an armed slingshot.
- **#125 — Captain's desk forgets its tab**: keep the last active tab, don't reset to the menu.
- **#123 — UI overlap / no depth**: subviews + progressive disclosure — folds into the unified
  nav list below.

**The unified navigation-steps list (owner ruling — design notes in
UnifiedNavListNotes.md; Gemini second opinion pending its CLI login):** ONE ordered list holds every flight step — both burn kinds, insertions,
orbit entries, dockings, slingshots, skims, and scripted/open-ended steps like an ambush. The
current burn list is the seed: it absorbs the loose slingshot/orbit/dock buttons (#124's ask).
Click a step → its options and settings unfold; stretch: draggable re-ordering. Open-ended
steps (ambush of another ship = unknown completion time) invalidate downstream planned burns —
gray them out with a marker saying why, and recompute when the open step resolves.

- **PR-B · Kepler rails (Core).** Optional eccentricity + argument of periapsis on scenario
  bodies; Kepler solve (deterministic Newton); e=0 byte-identical to today (regression-gated
  like the atmosphere was); cascades in the same PR: elliptical orbit rings in the game, Lab Viz
  viewer formula + BOTH parity tests extended, OrbitRule instantaneous radius. This is the one
  Core feature the cyclers, comet vaults, and resonant rocks all wait on.
- **PR-C · Lab 21 "The Commuter" + the cycler ship.** The lab computes an honest Earth↔Mars
  cycler in OUR phases (lesson 18 machinery), prints the timetable, `--viz` with the dated
  scrubber; its baked ellipse becomes the scenario body — a dockable haven with a bar
  (BuildComplex spec) and the engine-bay wing behind a locked hatch. First cycler-timetable
  quest handed out aboard. Catching her IS the navigation challenge.

## 3. Parallel lanes (independent of B/C — pick by mood)

- **PR-F · Doors that grow the world — PRIORITY on the indoors lane (owner).** BuildComplex
  runtime wings; the cracked V-06 hatch opens a real back room; one indoor quest using it.
  AND: **people cannot be static furniture** — NPCs change place on a schedule, move between
  rooms, go behind locked doors (a first pass: time-sliced positions per NPC).
- **PR-E · Masked contracts** (the Butch Cassidy line): honest work under FALSE COLORS bleeds
  heat; the payroll-run mission with its scripted ambush; LieBlown per the morning ruling —
  sensitive contract blown = mission ruined + pay voided + heat spike.
- **Asteroid secret bases (spec follow-up to PR-B):** rocks on suitable real orbits as hidden
  stores/material sources; low spin = usable base, fast tumbler = scan curiosity only.
- **UI batch 2** (from the audit's queued items): ghost/disabled kill-chain buttons pre-target,
  dedupe the ledger's twin 🔭 buttons, planet-label hierarchy over station labels, dark-web node
  visibly a market, hunt-by-name on Sensors.

## 4. Owner playtest targets (new since your last hands-on)

- The **ledger receipt flow**: take a tip, watch it say where it went, work it from 📜.
- The **skim**: `?skim=jupiter` → SOLVE mid-corridor → add the burn → then drag it into the red
  and fly it anyway — the sail-holing and the 2-day sewing window are worth experiencing once.
- The **audit fixes** in situ: the docked deck hint, the click-a-planet banner line.

## 5. Stretch / evening

- **Time-fade export to the game** — the lab viewer's ghost-path fade applied to Map.razor's
  plotted ribbon around scrub time (the spec'd follow-up from #110).
- **Lab 23 — "The ambush geometry" (owner: liked, GO):** compute where pursuit intercepts
  actually close (feeds the masked-contract ambush placement honestly). UI-side: an ambush is
  an OPEN-ENDED step type in the unified navigation list — it grays out downstream burns until
  it resolves.
- Blind-UI audit round 2 on whatever shipped today (the protocol is now one command).

*Process as always: spec'd lanes to Opus implementers, cheat codes for every leg, Chrome
verification by implementer and lead, owner approves merges — or grants a standing approval for
lanes he's already blessed.*
