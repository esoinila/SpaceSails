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

## 0. THE PRIORITY — orbit safety at small moons (owner ruling, end of Thursday)

The first full mission delivered, and the owner's verdict names Friday's number one:

> *"It troubles me greatly that the ship is left to be kind of in orbit by luck there. The
> orbit keeping should always be the job of autopilot this close to a moon, never a manual
> task. The navigate-to-orbit must deliver the ship to orbit where it will KEEP it
> automatically on the orbit. Not crashing into the planet. ... I think biggest priority is
> the ORBIT safety of small moons."*

**The ruling, as design law:** armed auto-orbit ends in a KEPT orbit, not an achieved one.
"Parked" means the autopilot HOLDS the park — station-keeping trims (priced in pulses, quoted
at arm time on top of the transfer, honest like everything else) — until the captain
deliberately takes the ship back (with the #179 confirm) or the tank forces a LOUD handback.
Consequences:

- The #176/#184 banner contradiction dies: a kept orbit reads "🛰 AUTOPILOT HOLDS THE ORBIT —
  Enceladus, 313 km, trim ≈N p/day", never "YOU HAVE THE SHIP" while circling a moon by luck.
- **Lab 25 "The tide that takes it back" is therefore Friday's LAB LANE, promoted** (see §3):
  the station-keeping thresholds and trim costs come from the lab's measured drift tables,
  not guesses — the R&D pattern the owner endorsed, applied to the priority.
- #183's degradation alert becomes the BACKSTOP (fires only if keeping fails/runs dry), not
  the primary defense.

## 1. Morning state (already merged Thursday night)

- **#183 (fixes #179/#180 CRITICAL)** landed: `ParkStability` verdict in Core (424/424), the
  manual out-of-band press arms the autopilot descent instead of parking in the chaotic band,
  the degradation warning shouts amber/red, disarm asks twice. Owner verifies on the fresh
  build; §0 builds ON this (keeping > warning).

## 2. Main lane — "the ship speaks" (the playtest storm, retired as ONE system)

Thursday's evening findings are all one missing organ: a single, honest, always-current
account of what the ship is doing and about to do. Build it once, not as five patches:

- **#159 banner NOW/NEXT rows** — owner's spec is now explicit (#184): the orbit/insert step
  is its own SECOND LINE that "automatically scrolls to active step once it is time", up/down
  arrows beyond two rows; fed by `FlightPlanStatusBuilder` as the ONLY truth ("NOW: coasting
  the transfer arc · NEXT: insert at 313 km, ~2 h"), which also closes #171 and #173's
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
- **#178/#177 the captain approves the space-crimes** — hostile boarding gets the
  authorize-the-shot treatment (owner ruling: "always have an are-you-sure before committing
  felonies... just like firing the weapons"). #177 escalates it: "auto-piracy stuck me while
  orbiting through the moon" — plunder can trigger from PROXIMITY during autopilot flight, so
  the fix is not just a confirm, it is that hostile acts are NEVER automatic.
- **#185 the celebration** — mission completion gets its fanfare: a completion pop-up, the
  🦜 parrot SINGS, the task giver is grateful ("Drinks free!!"), and the moment seeds the
  relationship system (we now have a history with the lady at the Ringside bar). The money
  never again just silently appears.

## 3. The lab lanes — Friday R&D candidates (owner picks; lab-first, then the game uses it)

- **Lab 25 "The tide that takes it back"** *(PROMOTED to the priority lane — it IS §0's
  science)*: the stability map measured honestly — drift sweeps across radius × eccentricity
  at Enceladus/Luna/Titan, WHERE the chaotic band actually bites and how fast, the cost of
  **station-keeping** (hold-orbit trims priced in pulses per day) vs re-parking. The game
  then ships orbit-KEEPING as the armed autopilot's default contract (§0, owner ruling:
  "the navigate-to-orbit must deliver the ship to orbit where it will keep it automatically")
  with thresholds and trim budgets from the lab's tables; the #166 alert fires at the
  measured onset as the backstop, not the defense.
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
