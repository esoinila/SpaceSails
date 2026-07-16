# Friday plan — the ship learns to speak (2026-07-17)

*Where Thursday ended: the R&D day. Two labs shipped and immediately became flight software —
Lab 23 "The moon run" (Lambert rides the well: 677 → 96 pulses) and Lab 24 "The last mile"
(phasing rendezvous: the 229-pulse decline became a 2-pulse bus with a trade table). Then the
owner flew the first full mission end to end — contract at a bar, cross-well transfer, auto-park
at Enceladus, parcel delivered — and filed eleven issues in an afternoon, of which six were
fixed and merged the same evening (#168 fleeing buttons, #169 misplaced tutorial, #175
uncompletable moon-haven delivery, #176 silent success, #153 ghost ray, #154 inverted arrows).
The through-line of every finding: THE FLYING WORKED; THE SHIP NEVER SAID WHAT IT WAS DOING.
Tests 417/417 + the #180 lane's gates. Owner's verdict: "Well I call it progress... lots of
little fixes to do :-D" — and a standing ask for more lab ideas: "I love the R&D feel of that.
Lab tests and game puts research into use."*

## 1. Morning merges (overnight/evening lanes → inspect → land)

- **`fix/180-moon-grade-orbit` (CRITICAL, #179/#180):** the manual orbit button stops parking
  ships in the tide-chaotic band (outside the stable band it arms the autopilot descent
  instead); Core `ParkStability` verdict with Lab-16-provenance gates; edge-triggered
  "⚠ orbit degrading" warning (amber TideRisk / red Subsurface) with warp-drop + ledger;
  disarm gets an are-you-sure. Merge on green, fresh 5073, owner re-flies the Enceladus park.

## 2. Main lane — "the ship speaks" (the playtest storm, retired as ONE system)

Thursday's evening findings are all one missing organ: a single, honest, always-current
account of what the ship is doing and about to do. Build it once, not as five patches:

- **#159 banner NOW/NEXT rows** (2-3 rows, up/down arrows) fed by `FlightPlanStatusBuilder`
  as the ONLY truth — the armed autopilot lists its remaining steps ("NOW: coasting the
  transfer arc · NEXT: insert at 313 km, ~2 h"), which also closes #171 and #173's
  "approach → step 1/1" confusion.
- **#166 ShipAlerts channel** — edge-triggered, acknowledgeable, one small record the banner
  strip, desk chips, ledger, and the 🦜 parrot all read: collision alarm ("ROCKS AHEAD!"),
  fuel alarm (amber at the 18% reserve, red at can't-reach-a-pump), and #180's orbit-degrading
  warning migrates in as the third founding alert.
- **#167 burn FX** — every burn (manual, plan node, autopilot, scheduled) gets its thruster
  flash + rumble, wall-clock-timed so 10,000× warp can't hide it. First audio in the game:
  one tiny service, mute toggle, off in hidden tabs.
- **#172 warp-to-arrival** — the rehearsal already knows the whole flight; a "skip to next
  event" control jumps warp in chunks while every alert/burn/arrival still yanks it to 1×.
- **#178 the captain approves the space-crimes** — hostile boarding gets the
  authorize-the-shot treatment (owner ruling: "always have an are-you-sure before committing
  felonies... just like firing the weapons").

## 3. The lab lanes — Friday R&D candidates (owner picks; lab-first, then the game uses it)

- **Lab 25 "The tide that takes it back"** *(recommended first — it finishes #180 with
  science)*: the stability map measured honestly — drift sweeps across radius × eccentricity
  at Enceladus/Luna/Titan, WHERE the chaotic band actually bites and how fast, the cost of
  **station-keeping** (hold-orbit trims priced in pulses per day) vs re-parking. The game
  then ships the "hold orbit" autopilot option (#179's ask: "the orbit should work and not
  drop off") with thresholds taken from the lab's tables, and the #166 alert fires at the
  measured onset, not a guess.
- **Lab 26 "The soft arrival"** *(composes 22 + 23)*: aerocapture as an ARRIVAL MODE — Titan
  has an atmosphere and lab 22 has the corridor math; teach the trade (2.33 km/s of arrival
  matching vs one aimed skim), then `TransferPlanner` learns an aerocapture arrival option for
  atmosphere-bearing targets and the trade table grows a "skim arrival" row. The 96-pulse moon
  run drops toward ~30.
- **Lab 27 "The getaway"** *(the pirate lane's physics)*: escape-from-pursuit computed
  honestly — thrust-only hunters (by design) vs orbital tricks: the sling escape, the skim
  heat-bleed, the phasing juke (Lab 24's k-table read as evasion: "the cheaper-sooner tradeoff
  comes in handy when there is heat on us" — owner). Produces the wolves' honest pursuit
  envelope AND the player's escape assists; feeds the ticket-queue AI when that lane opens.
- **Lab 21 "The Commuter"** *(reserved since Tuesday)*: the Persephone cycler — Kepler rails
  are on main and Lab 24's rendezvous math is exactly the "board the cycler as it passes"
  problem. Ships her as a dockable body with a timetable.

## 4. The theme lane — the milk run becomes a product

- **#160 tutorial mission** (owner: "some easy milk run with autopilot from moon to moon") —
  after the ship-speaks lane so the tutorial narrates a ship that talks back. The full loop
  Thursday proved by hand, now guided: contract → plan → armed quote → flown → delivered → paid.
- **#157 fuel economy** — the unmistakable ⛽ trade item ("car-gauge big letters, Fill her
  up"), pulse depots at moons, and the fuel alarm riding #166.
- **Milk-run contract chain** (Thursday §3, still the north star): Ringside ↔ Enceladus ↔
  Titan short-hops with real cargo flavor — now that both leg types (well transfer + last
  mile) fly cheaply, the contracts write themselves.

## 5. Housekeeping (fit around the lanes)

- **#161 boot timeout warnings** (profile → staged load; archive alt-universe scenarios).
- **#162 Captain's-desk "New voyage"** (reopen the start picker; stop memorizing cheats).
- **#163/#164 shuttle doors + Phobos monolith** — parked for the weekend unless the day runs long.

## 6. The testing protocol (unchanged — it caught everything)

Owner flies fresh 5073 builds hands-on and streams findings (from the sofa, from mobile, from
the bookshop queue); Fable triages with code-level root causes, files/annotates issues,
dispatches Opus lanes same-session; merges under explicit or standing approval only; Gemini
cold-reads whatever ships. Every lab README pastes only numbers a probe actually printed.

*Process note from Thursday evening, now law: non-worktree lanes own the main working tree —
the lead branches from a git worktree, never under a running lane.*
