# Tuesday Plan — the living solar system: quests that use the whole ship

*Owner direction (2026-07-14): make the flight to the car part of the quest; more quests in the
agent-team style; the world may now grow dynamically behind unlockable doors; cycler stations in
the style of The Martian's Earth↔Mars ship as meeting places and navigation challenges; secret
stations on odd orbits that only targeted, intel-fed scans reveal (Expanse-style "you can find
it if you know where and when to look"); quests should tie together several of the ship's desks;
and a Butch Cassidy angle — the pirate crew masks the ship, runs honest work to bleed heat, and
mid-mission surprises reward their pirate nature.*

## The thesis

Every mechanic this plan needs already half-exists: intel ledger (#78), telescope schedule and
area scans (#70-74), transponder FALSE COLORS (#65), heat and lying low (#93), boarding and the
gun (#84-88), walkable interiors with quest-giving strangers (#101-102), and the labs that fly
real trajectories (18/19/20). The plan's job is to *connect* the desks: a quest arrives in a bar,
turns into intel, intel turns into a sensors task, the scan turns into a navigation problem, and
the flight turns into a story. No new physics is invented that a lab hasn't computed first.

## Pillars

### 1. The hunt is the quest (find the car, properly)

Today the fetch quest's wreck sits labeled on the map from minute one. Instead: the Derelict
Roadster becomes a **hidden body** — not drawn, not clickable, not in the scope — until found.
The Fixer's job now hands you **intel**, not a map pin: an orbit estimate ("sunward of Mars,
radius ≈1.14 AU, period ≈442 d, last fix puts her near phase X on date Y"). A one-click hook on
the intel card — **"🔭 point the scope where this says"** — schedules a prioritized area scan at
the predicted position/time. The scan reveals the wreck (a real contact, then a real flight).
The intel→scan→reveal cycle is the reusable primitive every later secret in this plan uses.

### 2. Elliptical rails (the one Core feature)

`CircularOrbitEphemeris` only knows circles, so cyclers and comet-orbit secrets are impossible
today. Add **Kepler rails**: optional `eccentricity` + `argPeriapsisRad` on a scenario body;
position solves Kepler's equation (Newton on E − e·sinE = M — deterministic, a few iterations,
analytic like today). e=0 degrades exactly to the current formula, so every existing scenario is
untouched. Cascades handled in the same PR: orbit rings become ellipse polylines, the Lab Viz
viewer formula + ephemeris-parity tests extend (the parity gate is why we can trust this),
`OrbitRule` uses instantaneous r where it used OrbitRadius. Sanctioned by lesson 09's framing:
rails are the game's honest approximation, now with one more conic.

### 3. The cycler line (computed by a lab, then lived in)

**Lab 21 candidate — "The Commuter":** compute, honestly, an Earth↔Mars cycler in OUR rails'
phases (lesson 18 already built the free-return machinery): the ellipse, the encounter timetable,
what a missed window costs. The lab's baked numbers become a **scenario body**: a station-haven
on that elliptical rail — dockable, walkable, with its own bar (BuildComplex spec = 1 body + 1
StationSpec + backdrops). Catching her is the navigation challenge; her bar is the neutral
meeting place where cycler-timetable quests get handed out ("be aboard before she swings out, or
wait 26 months"). A mirrored sister ship (the slow escalator) can come later.

### 4. Secret stations and lucky scans

Hidden bodies (pillar 1's flag) on interesting rails (pillar 2's ellipses): a **Trojan cache**
sharing Jupiter's rail ±60° of phase; a **comet vault** on a long ellipse that only visits the
inner system twice a decade; a resonant smuggler's rock timed 3:2 against Mars. Rumors and intel
(bar talk, dark web, quest rewards) carry orbit estimates of varying quality — good intel gives
a tight scan box, bar gossip a wide one. Un-aimed scans can stumble on them only with real luck;
aimed scans are the payoff of knowing where AND when to look.

### 5. Honest work, dishonest skills (the Butch Cassidy line)

A **masked contract** type: fly under FALSE COLORS as an innocuous freighter and take legitimate
work — escort the payroll, ferry a surveyor, deliver medical cargo. Running honest bleeds heat
(the lying-low mechanic, but active instead of parked). The catch: each contract carries a
scripted **surprise** — pirates hit the payroll at the predictable pinch point, the "surveyor"
is a claim jumper, the medical crates aren't — and the resolution reaches for the pirate
toolkit: warning shots, boarding, aim solutions, a nose for ambush geometry. Success pays
credits AND heat reduction; blowing cover (scanned too closely while masked — the existing
LieBlown mechanic) flips the job into a mess.

### 6. Doors that grow the world

The station interiors already treat geometry as data (BuildComplex). Unlockable hatches become
**expansion joints**: opening one (crack quest, bribe, reputation) appends a wing to the deck
plan at runtime — a back room, a smuggler's tunnel to a second berth, a laboratory. Quests can
gate on rooms existing; rooms can gate on quests. First uses: a back room behind the Bonded
Stores hatch you cracked, and the cycler's engine bay.

## Quest sketches (the menu)

1. **Find the car** (upgrade of the existing fetch) — intel → scan → reveal → fly → deliver.
2. **The payroll run** (Butch Cassidy) — masked escort; ambush at the transfer window's pinch
   point; your warning-shot doctrine is the negotiation.
3. **The cold case** — a liner went dark six years ago; her last fix + PathPredictor gives a
   search ellipse; scan, find the hulk, learn why. (Expanse energy.)
4. **The ghost ship** — stale distress beacon on a comet ellipse; salvage race; the surprise is
   aboard. Ties bar rumor → sensors → navigation → boarding → interior walk.
5. **The commuter's bar** — a contact will be aboard the cycler through the next window only;
   catch her or lose the thread. The calendar is the antagonist.
6. **Witness transport** — carry someone who must never be OBSERVED (invert the sensor game:
   avoid other ships' cones, run dark, pick quiet corridors).
7. **The repo job** — sanctioned boarding of a defaulted ship; legal piracy, morally gray fence.
8. **The claim jump** — a He3 rock's coordinates leak; beat the rival crew's transfer window.

## Open questions for the owner

1. Cycler naming/flavor — obfuscated-Hermes ("the Commuter"? "the Ferrywoman"?) and how openly
   we wink at The Martian.
2. Should blowing FALSE COLORS mid-contract void the pay, spike heat, or spawn a hunter?
3. Secret-station count for v1 (plan says 2: Trojan cache + comet vault) and whether any should
   be quest-exclusive vs. free-roam discoverable.
4. Dynamic wings: persist per-session only (current save model) — acceptable for v1?
5. Lab 21 as its own PR-sized lesson first (house pattern: compute → then ship the game body)?

See [TuesdayPlanPRs.md](TuesdayPlanPRs.md) for the PR lanes and build order.
