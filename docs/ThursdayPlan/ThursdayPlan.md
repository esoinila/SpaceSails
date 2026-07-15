# Thursday plan — flying the sub-systems (2026-07-16)

*Where Wednesday ended: the biggest day yet. Sixteen PRs merged (#129–#142, #144 + the doc
set), the six morning playtest issues fixed and closed, Kepler rails opened the elliptical
world, the doors grew a back room with a roaming NPC, the flight plan got its NOW line and its
step editors, and one frame chip now rules every drawn line. Then the owner flew the evening
build at Saturn and found the deep stuff no test had: an empty insert window at Enceladus
(#136, fixed same evening), a fuel hemorrhage on the Titan approach, and the autopilot's
silent handback. Tests 346/346; the Gemini CLI is authenticated and pulling double duty as
cold test-user and design reviewer.*

**The owner's verdict, verbatim — this is the theme:** *"Flying on the sub-systems makes the
game area seem much bigger. I love that some mission can just be going from one Saturn moon
to another."*

## 1. Overnight lanes → morning merges (owner pre-approved 2026-07-16 ~01:00)

- **`fix/frame-scaled-ribbon` (#145):** in a giant's co-moving frame the drawn ribbon
  truncates to ~1.25 local orbital periods (soft time-fade end, stretches to include the next
  plan node); Sun frame byte-identical.
- **`fix/autopilot-promise` (#147 + interim guard for #146, possibly #148):** the arm-time
  REHEARSAL — the autopilot simulates the whole flight before accepting, quotes the true cost
  incl. insertion, **refuses jobs it can't afford** (owner's ruling: *"dropping from autopilot
  ... should never ever happen when there was nothing external to cause it"*). Handbacks from
  external causes are LOUD: warp auto-drops to 1×, persistent reason in the flight-plan
  status, Captain's-ledger entry; the Captain chip reads the same truth as the banner. If the
  lane took the optional #148: the rehearsal's own trajectory drawn as "the autopilot's plan"
  while it flies.

Merge on inspection, then a fresh 5073 build for the day's testing.

## 2. The main lane — teach the autopilot to ride the well (#146)

Wednesday night's Titan approach spent **~33 km/s of Δv** (167 pulses) doing what the
geometry prices at 2–3 km/s: the controller re-sets the full velocity vector against Saturn's
pull instead of using it. The fix is the flying sibling of the frame chip: **inside a giant's
Hill sphere, plan in the PARENT's frame** — a transfer arc toward the moon's orbit,
phase-matched so the moon is there on arrival, burns at the right points, coast where the
well does the work.

- **Lab-first (house pattern): Lab 23 candidate "The moon run"** — compute an honest
  Enceladus→Titan transfer in our rails (Kepler machinery from #132 helps), print the
  timetable and the Δv bill, `--viz` it; then the same Core code becomes the autopilot's
  in-well planner. The rehearsal (#147) automatically starts quoting the CHEAP plan — the
  promise and the planner compose.
- **Morning decision for the owner:** transfer flavor — patched-Hohmann (simple, teaches
  well, burns at apsides) vs. Lambert solve (general, matches lab-19 machinery)? And does the
  manual pilot get the same assist (a "transfer to <moon>" solve button in the flight plan)?

## 3. The theme lane — missions between the moons

The owner loves that a mission can be "go from one Saturn moon to another." Give that a
contract chain the moment the in-well flying is cheap:

- **Saturn-local milk runs:** short-hop contracts Ringside ↔ Enceladus ↔ Titan (He3 drums,
  crew rotation, station spares). Dock-to-dock plannable in the flight plan — the D2 editors
  + the transfer solve make it the unified-UI showcase, exactly the "milk run planned end to
  end and then just flown" vision.
- Local flavor exists already: Ringside Exchange, the Enceladus haven, Saturn Depot traffic —
  the wolves' ticket queue and skim heat-bleed rulings apply out here too when those lanes
  come up.
- Jupiter mirrors it later (Galilean-moons start already exists) — build Saturn first, port
  the pattern.

## 4. The testing protocol (unchanged — it works)

Same loop as Wednesday, after the fixes land: owner flies a fresh 5073 build hands-on and
streams findings in-chat; Fable captures the live tab (screenshots + console), files issues
with evidence, dispatches Opus fix lanes same-session; **Gemini is the cold test-user**
(blind screenshot audits + design second opinions — owner: "a great collaboration"). Blind
audit round 3 on whatever ships today.

**Playtest targets for the day:** arm auto-orbit Titan and watch the autopilot REFUSE or
quote honestly; fly the Saturn frame with the short ribbon; a full Enceladus→Titan hop once
the transfer planner lands — the first real moon-to-moon milk run.

## 5. Parked (explicitly not today unless the day goes long)

- PR-D3 — open-ended steps + the `failed`/`skipped`/`aborted` states (Gemini triage, needs
  PR-E's ambush).
- Lab 21 "The Commuter" / the Persephone cycler (PR-C) and PR-E masked contracts.
- Damage control design (Q1 ruling: the Expanse model), wolves' ticket queue, asteroid
  secret bases.
- UI leftovers: #123 depth batch, sling SOLVE near the fold on short viewports, frame-chip
  persistence across reloads, ledger twin-🔭 dedup, gun-deck ghost buttons.

*Process as always: spec'd lanes to Opus implementers, Fable plans and inspects (50% token
split), cheat codes for every leg, Chrome verification by implementer and lead, Gemini cold
reads, owner approves merges — and finds the real bugs by flying.*
