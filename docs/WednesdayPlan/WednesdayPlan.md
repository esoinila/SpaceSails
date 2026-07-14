# Wednesday plan — the agenda (2026-07-15)

*Where Tuesday left us: labs 19–22 shipped with pop-ups; the sling, the air brake, and the
corridor gauge are live; the find-the-car hunt runs intel → scan → reveal; the Captain's ledger
files every tip; blind-UI audit round 1 is merged with its first four fixes. Zero open PRs,
291/291 tests, live site current. Today is about answering the parked design questions and
opening the elliptical half of the world.*

## 1. Morning coffee — owner decisions (everything below flows from these)

**Flight assists** (docs/TuesdayPlan/FlightAssists.md):
- **Q1 — skim damage currency.** Shipped default: too-deep holes the sail, thrust gated for a
  2-sim-day repair. Keep, or switch to pulse ablation / a hull meter?
- **Q2 — canny wolves.** Do hunters EVER learn to follow through a skim, or is atmosphere
  forever their blind spot (keeps the escape reliable)?
- **Q4 — skim heat.** Does a flaming entry plume RAISE notoriety heat (dramatic, visible for
  light-minutes) or BLEED it (you vanished)? Gameplay pull is real in both directions.

**The living solar system** (docs/TuesdayPlan/TuesdayPlanVision.md):
- **Cycler naming/flavor** — obfuscated-Hermes: "The Commuter"? "The Ferrywoman"? How openly do
  we wink at The Martian?
- **Blown FALSE COLORS mid-contract** — void the pay, spike heat, or spawn a hunter?
- **Secret-station count for v1** (plan says two: Trojan cache + comet vault) and quest-exclusive
  vs. free-roam discoverable.
- **Dynamic wings persistence** — per-session only acceptable for v1?

**Housekeeping (5 minutes):** run `gemini` once to re-auth the CLI — then
`tools/playthrough/ui-audit-gemini.sh` runs the TRUE blind round for comparison with Tuesday's
Claude stand-ins.

## 2. The main lane — Kepler rails, then the cycler

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

- **PR-E · Masked contracts** (the Butch Cassidy line): honest work under FALSE COLORS bleeds
  heat; the payroll-run mission with its scripted ambush; LieBlown consequence per the morning's
  answer.
- **PR-F · Doors that grow the world:** BuildComplex runtime wings; the cracked V-06 hatch opens
  a real back room; one indoor quest using it.
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
- **Lab 23 candidate** — "The ambush geometry": compute where pursuit intercepts actually close
  (feeds the masked-contract ambush placement honestly).
- Blind-UI audit round 2 on whatever shipped today (the protocol is now one command).

*Process as always: spec'd lanes to Opus implementers, cheat codes for every leg, Chrome
verification by implementer and lead, owner approves merges — or grants a standing approval for
lanes he's already blessed.*
