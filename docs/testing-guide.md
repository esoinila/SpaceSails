# Testing guide — the owner's regression checklist

A scripted playtest per major feature: exact clicks/keys, what you should see, and what
"broken" looks like. Run through these after any change that touches the map, the ship
simulation, or the deck views.

**Before you start:** run `./run.ps1` (Release build) and open the printed localhost URL.
Debug WASM runs on the IL interpreter and is roughly **100× slower** — choppy frames, sluggish
plotting, and timings in these scripts (rum wobble, boarding time, warp behavior) will all read
wrong under Debug. If a script feels broken, check you're not accidentally running
`./run-debug.ps1` first.

Each script links to the matching [feature doc](features/) if you need the full behavior
reference.

---

## 1. Launch + scenario select

*(See [scenarios.md](features/scenarios.md).)*

1. Open the home page (`/`).
2. Click **Launch** on the **Sol** card.
3. Confirm the map loads with the scenario name "Sol" in the HUD, no `EU ⚡` badge.
4. Go back, click **Launch** on **Sol (Electric)**.
5. Confirm the `EU ⚡` badge appears next to the scenario name.
6. Manually edit the URL to `/map?scenario=wheel` and reload.
7. Confirm "Wheel" loads (Venus/Earth/Mars visibly on a rigid spoke around Saturn once zoomed
   out).
8. Try `/map?scenario=not-a-real-scenario` — confirm it silently falls back to Sol, no crash.

**Broken looks like:** blank canvas, a spinner that never clears past "Rigging the sails…", or
the wrong scenario's bodies rendering.

---

## 2. Warp / pause / follow

*(See [map-and-warp.md](features/map-and-warp.md).)*

1. Launch Sol. Drag the warp slider to roughly the middle.
2. Confirm the readout shows an increasing multiplier (e.g. `100×`) and sim time is advancing.
3. Click **Pause**. Confirm the readout shows `∥` and sim time freezes.
4. Click **Pause** again to resume.
5. Drag the warp slider to maximum. Confirm the effective-warp readout shows `(auto-drop from
   N×)` once a planet or encounter gets close, and the multiplier actually shown is lower than
   the slider's request.
6. Drag the map to pan away from the ship, then click **Follow Ship**.
7. Confirm the camera snaps back to the ship and stays centered as time advances.

**Broken looks like:** sim time not advancing at all, warp stuck at 1×, or Follow Ship not
re-centering.

---

## 3. Hand-flying pulses

*(See [map-and-warp.md](features/map-and-warp.md).)*

1. Launch Sol, note the ship speed readout and the "circular here" value beside it.
2. Press `↑` (or `+`). Confirm speed increases ~10% and "Mass pulses" ticks down by 1.
3. Immediately press `↑` again. Confirm you see "Pulse drive cooling down…" (cooldown is ~1
   second — a second press just after the first should be rejected).
4. Wait a beat, press `Shift`+`↑`. Confirm speed increases only ~1% (fine trim).
5. Press `↓` repeatedly (with pauses) until mass pulses hit 0, away from any port zone.
6. Confirm an **Adrift** red alert bar appears with "Request rescue".

**Broken looks like:** pulses not decrementing, speed not changing, or no cooldown message on a
double-press.

---

## 4. Plotting a course + closest-pass warning

*(See [plotting-desk.md](features/plotting-desk.md).)*

1. Launch Sol. Click **Plot** (or press `P`). Confirm sim time freezes and the plot card appears.
2. Drag the scrub slider forward a few days. Confirm every planet shows a faint ghost tethered to
   its live position.
3. Click **Add burn at scrub**. Confirm a node appears in the list at that scrub time.
4. Set it to **Decelerate**, pulses = 3, percent = 10.
5. Confirm the ribbon visibly bends and a "Closest pass" line appears under the horizon controls.
6. Drag the scrub slider so the ribbon's path visibly crosses through a planet. Wait ~1 second.
7. Confirm the closest-pass line turns **red** and reads "IMPACT, captain!".
8. Adjust the node's percent down until the pass clears the planet by a few radii — confirm the
   line turns **yellow** under 5 R, and gray/neutral once well clear.
9. Click **Play** (or `P`) to resume live flight.

**Broken looks like:** no ribbon at all, closest-pass never updating, or the color thresholds not
matching (never turning red on an obvious intersection).

---

## 5. Traffic board + intercept

*(See [traffic-board.md](features/traffic-board.md).)*

1. Launch Sol. Press `5` (or click **5 Comms** in the station tab bar) to open the Comms desk.
   Confirm a table of callsigns/cargo/routes (the traffic board) appears.
2. Click a row for a Luna pod (`Callsign` containing a short Earth→Mars or Earth→Venus route,
   `Status` = En route or Tracked). Confirm the row highlights.
3. Confirm a **Pin** button appears in the card footer reading "Pin: brakes at *destination*".
4. Click **Pin**. Confirm a prediction cone is drawn on the map from the target outward.
5. Open **Plot**, add a burn that bends your ribbon into the cone.
6. Return to Play, warp forward, and confirm the target's row status flips through `En route` →
   `Tracked` (and to `Lost` if you stop observing it for over 2 sim-hours).

**Broken looks like:** empty table, no cone drawn after Pin, or status never changing.

---

## 6. Scope

*(See [scope.md](features/scope.md).)*

1. Launch Sol. Click **Scope**. Confirm a target renders with lock brackets, distance, and
   relative speed.
2. Confirm the corner label reads **◆ AUTO**.
3. Click **▶** a few times. Confirm the target changes each click and the label switches to
   **◆ TRACK**.
4. Click the middle **AUTO** button. Confirm it returns to auto-lock and the label flips back.
5. With nothing observed nearby, confirm it still shows a celestial body rather than "NO TARGET"
   static (static should only appear with zero candidates at all).

**Broken looks like:** scope panel blank, cycling not changing the target, or the label never
switching between AUTO/TRACK.

---

## 7. Orbit assist + armed insertion

*(See [orbit-assist.md](features/orbit-assist.md) and [plotting-desk.md](features/plotting-desk.md).)*

1. Launch Sol. Fly (or warp) toward Earth until an orbit strip appears in the HUD reading
   "🛰 Orbit Earth — …".
2. Confirm the two progress bars (distance-vs-Hill, speed-vs-limit) move as you approach.
3. Once it reads "window OPEN" and the button is green, press `O`.
4. Confirm mass pulses drop by the shown cost and the ship's relative velocity to Earth goes
   to ~0.
5. Undo by pulsing away, then open **Plot**, scrub to a pass near Mars, and confirm an "Insert at
   Mars pass…" button appears if the pass is close enough.
6. Click it to arm — confirm it turns green and reads "Insertion ARMED — will orbit Mars…".
7. Return to Play and let time run past the window opening.
8. Confirm the game auto-fires the burn, deducts pulses, shows "Planned insertion executed —
   bound to Mars 🛰", and the armed button clears.

**Broken looks like:** the button never enabling despite being inside the Hill sphere and slow
enough, or an armed insertion never firing once the window opens.

---

## 8. Depot plunder

*(See [depots.md](features/depots.md) and [boarding-run.md](features/boarding-run.md).)*

1. Launch Sol. Open **Traffic** and find a row named "*Planet* Depot" (e.g. "Mars Depot").
2. Select it, Pin it, and orbit-assist into the same planet (step 7 above).
3. Confirm your relative speed to the depot is near zero once you're both in the same orbit.
4. Wait for the boarding progress bar to appear and fill; confirm it completes close to the
   ~30-second best case (since relative speed/distance should both be tiny).
5. Dock and confirm the depot's cargo (matching the planet's flavor, e.g. Ice at Mars) is in your
   hold.

**Broken looks like:** the depot never appearing on the board, or boarding taking as long as a
sloppy high-speed intercept despite near-zero relative speed.

---

## 9. Dock, sell, upgrade

*(See [dock-and-economy.md](features/dock-and-economy.md).)*

1. With cargo in your hold, fly into Earth's, Mars's, or Venus's port zone.
2. Press `4` (or click **4 Trade** in the station tab bar) to open the Trade desk — a three-column
   trading floor as of PR-13 (local space contacts, dock market, cargo manifest).
3. Confirm the middle "Dock market" panel shows a "Docked at *body*" badge, a Sell button priced at
   your cargo's total value, and a Refill button (it shows a placeholder instead when you're not
   docked).
4. Confirm the right-hand "Cargo manifest" panel lists each cargo class in your hold with its
   units and estimated value, and a Total row matching the Sell button's price.
5. Click **Sell cargo**. Confirm credits increase, cargo drops to 0, and the manifest panel now
   reads "Hold empty."
6. Click **Refill mass**. Confirm mass pulses return to capacity.
7. In the Upgrades table, buy a **Reaction mass** upgrade (if you have 2000+ credits). Confirm
   the level increments, the displayed capacity increases by 150, and the next price roughly
   doubles.

**Broken looks like:** Dock button never enabling inside the zone, sell not paying out, the
manifest total disagreeing with the Sell button's price, or an upgrade button staying
enabled/disabled incorrectly relative to your credits.

---

## 10. Deck walk, rum wobble, first person

*(See [deck-view.md](features/deck-view.md).)*

1. Press `7` (or click **7 Deck** in the station tab bar). Confirm a top-down interior view loads
   with your avatar near the bridge.
2. Walk with `WASD` toward the **CANTINA**. Confirm collision stops you at walls rather than
   clipping through.
3. Press `E` at the cantina three times, each within a few seconds of the last.
4. Confirm the third interaction shows "That was the third tot. The deck feels… tilty." and your
   movement direction visibly wobbles for about 25 seconds.
5. Confirm you can still walk and interact normally during the wobble (it's cosmetic, not a
   lockout).
6. Press `F`. Confirm the view switches to first-person, with a real sky visible through the
   windows (sun and planets at their correct positions/sizes).
7. Press `Q`. Confirm you're returned to the helm view (map/plot), not just the deck plan.

**Broken looks like:** no wobble after 3 rapid tots, wobble blocking interaction entirely, or `F`
not producing a real (ephemeris-matched) sky.

---

## 11. Boarding run minigame

*(See [boarding-run.md](features/boarding-run.md).)*

1. Get a capture window open against a selected target (see script 5 or 8).
2. Press `7` for the Deck desk, walk to the **SHUTTLE BAY**, press `E`.
3. Confirm you're now flying a shuttle with `WASD` thrust toward a visible target airlock.
4. Fly in too fast on purpose. Confirm you **bounce** off (velocity reverses/halves) rather than
   docking, and the run continues.
5. Slow down and dock properly. Confirm it reports a soft dock and boarding completes instantly
   (no waiting out the timer).
6. Repeat, but this time press `Q` mid-run. Confirm the run aborts and the shuttle returns to the
   cradle without boarding.
7. Repeat once more, and deliberately let your mothership drift out of the capture window mid-run.
   Confirm the shuttle auto-returns and the run ends as a loss.

**Broken looks like:** no bounce on a hot approach, docking not completing the boarding, or the
shuttle not returning when the window closes.

---

## 12. Electric scenario venting

*(See [electric-sky.md](features/electric-sky.md).)*

1. Launch **Sol (Electric)**. Confirm the HUD shows a Charge bar under the main readouts.
2. Warp toward the sun (or into a visible plasma stream). Confirm the charge percentage climbs.
3. Let it reach 90%+. Confirm the HUD flags "⚡ ARCING — visible system-wide" and a halo ring
   appears around your ship on the map.
4. Press `V`. Confirm charge drops to roughly half its prior value and the arcing warning clears
   once under 90%.
5. Press `V` again immediately. Confirm a "Vent recharging…" message appears (cooldown) rather
   than a second instant halving.
6. Fly into a plasma stream while charged and confirm you feel a push along the stream's
   direction (speed changes without spending a pulse).

**Broken looks like:** charge never climbing near the sun, arcing warning never appearing, or
venting not reducing the charge value.

---

## 13. Tracking post scan-to-track

*(See [tracking-post.md](features/tracking-post.md).)*

1. Launch Sol. Press `2` (or click **2 Sensors** in the station tab bar) to open the Sensors desk.
   Confirm it opens full-screen with a sun-relative rosette, bearing/arc sliders, and a program
   dropdown, with the live map dimmed visibly behind it.
2. Pick a corridor program from the dropdown (e.g. "Earth–Mars corridor watch"). Confirm the
   bearing/arc sliders jump to match it and the wedge on the rosette redraws.
3. Click **Start sweep**. Confirm a progress bar appears and "Sweeping…" shows underneath.
4. Warp forward until the sweep completes (a full 360° takes 6 sim-hours; a narrow wedge is
   faster). Confirm a message like "Sweep complete — N contact(s) found" (or "nothing found").
5. If something was found, confirm it appears as a live **scope wall tile** (its own little scope
   canvas, not just a table row) with a quality bar, days-since-confirm, and distance underneath.
6. Click **Confirm** on a tracked tile. Confirm the message reports either a reconfirm (quality
   bar rises) or "Lost the fix… try a fresh sweep".
7. Confirm any remaining empty tiles (telescope slots not yet holding a track) show a dark
   "no track — sweep to acquire" tile rather than nothing.
8. Return to the map and confirm the tracked ship draws with a brighter marker and a thin ring
   around it (versus an untracked ship's plain dot).

**Broken looks like:** the sweep never completing, no scope-wall tile gaining a live track despite
a plausible sweep, a tile rendering blank/broken art, or the tracked ring never appearing on the map.

---

## 14. Local space trade

*(See [local-space.md](features/local-space.md).)*

1. Launch Sol with cargo in your hold (board a depot first if empty — see script 8). Orbit-assist
   into a planet with a depot (e.g. Mars).
2. Press `4` (or click **4 Trade** in the station tab bar) to open the Trade desk. Confirm it lists
   at least the planet's depot with a 🛰 icon and a **Trade** badge/button (note: as of PR-11 the
   panel no longer auto-opens on binding to orbit — the Trade chip on other desks updates live
   instead; switching to the Trade desk is now a deliberate action).
3. Click **Trade** on the depot row. Confirm a striped progress bar appears reading "Drones
   ferrying — NN%", and the Trade summary chip on other desks (e.g. Nav) shows the same
   "drones → *name* NN%" line.
4. Let it run in real time (don't just warp — the transfer accrues on the wall clock). Confirm it
   completes and your cargo hold empties into credits, matching dock-and-economy sell prices.
5. Pulse away from orbit mid-transfer on a fresh attempt. Confirm the progress bar resets/vanishes
   and no credit is paid — the envelope broke and progress was lost, no partial credit.
6. Leave the Trade desk (press `1`) and come back (press `4`); confirm it still shows the same
   contact list.

**Broken looks like:** the Trade button staying enabled with an empty hold, credits being paid out
despite the transfer being interrupted, or the Trade chip not reflecting an active transfer.

---

## 15. Dark web intel buy/sell + laser ranging

*(See [dark-web.md](features/dark-web.md).)*

1. Launch Sol. Fly to (or orbit) a haven — e.g. Ringside Exchange or Enceladus — or a far station
   beyond ~4×10¹¹ m from the sun.
2. Press `5` (or click **5 Comms** in the station tab bar) to open the Comms desk. Confirm the
   dark-web section shows a table of off-the-books ships instead of the "not orbiting or docked
   at a haven…" message, with the traffic board rendering alongside it in its own column.
3. Buy a route tip on a listed ship. Confirm credits drop by the shown price and the button now
   reads "Known".
4. In the same desk's traffic board column, confirm the bought ship now appears in the table with
   a `🕸 stale in Nd` badge next to its callsign.
5. Get at least one tracked contact at ≥50% quality first (press `2` for the Sensors desk, run a
   sweep — see script 13), then back on the Comms desk (`5`) confirm it appears under "Your
   sellable tracks" with a **Sell** button; click it and confirm credits increase.
6. Pick a tracked contact in the tight-beam dropdown and click **Hail**. Confirm an inline reply
   appears (a destination for a publishing ship, "No flight plan filed" for a secretive one).
7. Click **Laser range** on a tracked contact. Confirm a "lit up ⚠" message appears, and back on
   the Sensors desk (`2`) that ship's scope-wall tile now shows an `aware ⚠` tag.

**Broken looks like:** the dark-web section trading from an ordinary planet/dock, a bought tip
never appearing on the traffic board, or laser ranging not marking the target aware.

---

## 16. War room warning-shot / bribe / heat loop

*(See [war-room.md](features/war-room.md).)*

1. Launch Sol. Intercept a freighter (not a pod) close enough to be inside weapon range (2×10⁸ m —
   tighter than the boarding capture envelope). Press `3` (or click **3 War room** in the station
   tab bar) to open the War room desk — full-screen as of PR-13, the tactical circle filling the
   left ~60% of the screen with a range-scale selector above it and the heat gauge blown up large
   in its bottom-left corner.
2. Confirm the tactical circle shows your ship, a weapon-range ring, and the target as a dot; the
   right-hand contact list shows a status badge (🏳 compliant or ⚔ stubborn).
3. Click one of the four range-scale buttons (100,000 km / 500,000 km / 1 M km / 5 M km). Confirm
   the circle's rings and dots rescale to match.
4. Click **Hail**. Confirm an inline threat/reply line appears matching the status badge (surrender
   line if compliant, defiance line if stubborn).
5. Click **Warn**. Confirm the button is only enabled while inside weapon range; if the target's
   compliant, board it and confirm boarding completes in roughly half the usual time.
6. On a different ship, click **Bribe** instead. Confirm credits drop by the shown price, the badge
   changes to **🤝 bribed**, and the button disables itself afterward.
7. Board and rob a (non-bribed) compliant or stubborn ship. Confirm the heat gauge in the tactical
   circle's corner ticks up at least one flame (`◌◌◌` → `🔥◌◌`) and the cooling line shows a decay
   rate.
8. Warp forward several sim-days. Confirm heat decays by one level roughly on schedule (20 days per
   level, or 4× faster if you dock/orbit at a haven the whole time).
9. After a heat-raising robbery, confirm a hunter eventually appears: a red dot with a 🐺 wolf
   glyph on the tactical circle, its own row in the desk's hunter readout (bearing, distance,
   closing speed), and — once it's within 2× weapon range — a pulsing threat line from your ship
   straight to it. Confirm hiding in continuous haven orbit for a couple of sim-days makes it
   break off.

**Broken looks like:** Warn/Bribe enabled outside their stated ranges/conditions, the range
selector not rescaling the circle, heat never rising after a robbery, or a hunter never spawning,
never getting a threat line up close, or never breaking off at a haven.

---

## 17. Desk switching

*(See [station-desks.md](features/station-desks.md).)*

1. Launch Sol. Confirm the Nav desk is active by default: the map fills the screen, the toolbar
   shows only warp/Pause/Follow Ship/Plot/Scope/`?`/first hunt, and a slim station tab bar
   (`1 Nav · 2 Sensors · 3 War room · 4 Trade · 5 Comms · 6 Galley · 7 Deck`) sits top-center.
2. Confirm a thin chip strip on the right edge shows one small summary per OTHER desk (five
   chips while on Nav) — not the active one.
3. Press `2`. Confirm the Sensors desk takes over the screen (tracking post full-screen, live map
   dimmed but visible behind it) and the Nav chip now appears in the strip instead of Sensors'.
4. Press `6`. Confirm the Galley desk appears (news wire + rum locker) with its own chip absent
   from the strip.
5. Click a chip (e.g. the War room chip) instead of pressing a number. Confirm it jumps to that
   desk exactly like the key would.
6. Press `Escape` from any desk. Confirm it returns to Nav.
7. Press `7` to enter the Deck (walk-the-ship) desk, walk a few steps, then press `1`. Confirm it
   leaves deck mode and returns to Nav in one step (not two).
8. While the Plot panel's scrub slider or a maneuver-node number field has focus, press a digit
   key. Confirm it edits the field's value — it does **not** switch desks (inputs stop the
   keydown from reaching the desk router).

**Broken looks like:** number keys not switching desks, the chip strip missing a desk or showing
the active one, digit keys leaking into text/number/slider inputs (or vice versa — desk keys not
working because an input silently ate them when it shouldn't have), or `7`/`1` leaving deck mode
and Nav desk state out of sync (e.g. stuck on a blank screen).

---

## 18. Comms room, news wire, and bridge seats

*(See [news-wire.md](features/news-wire.md), [dark-web.md](features/dark-web.md), and
[deck-view.md](features/deck-view.md).)*

1. Launch Sol. Press `5` (or click **5 Comms**). Confirm the Comms desk shows a news ticker band
   (a row of short headlines, separated by dividers) plus three consoles side by side: the
   **departures board**, the dark web market, and tight-beam/laser ranging.
2. Confirm the departures board's rows look roomier than before (regular row padding, not a
   cramped `table-sm`).
3. Press `6` (or click **6 Galley**). Confirm the news wire panel shows a headline plus an
   "Earlier on the wire" list, each earlier line tagged "today" / "yesterday" / "*N*d ago".
4. Board and rob a ship (see script 16). Confirm a "Piracy alert" line naming the victim appears
   at the top of both the Comms ticker and the Galley feed, and that (once heat spawns a hunter) a
   "fitting out at ..." line appears too.
5. Buy a route tip on the dark web (script 15). Confirm a line naming the ship you bought appears
   on the wire.
6. Orbit or dock at a haven (e.g. Ringside Exchange or Enceladus). Confirm a line naming that haven
   appears on the wire the first time you bind there (not on every frame after).
7. Press `7` for the Deck desk. Walk to the bridge (near the bow) and confirm three more consoles
   are visible near HELM/NAV POST/SCOPE: **COMMS SEAT**, **TACTICAL SEAT**, **TRADE SEAT**.
8. Press `E` at each in turn. Confirm COMMS SEAT opens the Comms desk, TACTICAL SEAT opens the War
   room desk, and TRADE SEAT opens the Trade desk — each leaving deck mode in one step.
9. Walk to SCOPE and press `E`. Confirm it now opens the Sensors desk (not a small scope overlay).
   Walk to CANTINA and press `E`. Confirm it opens the Galley desk, where "Pour a tot" still works.

**Broken looks like:** the ticker missing or frozen on one line, a robbery/hunter/intel/haven-entry
never appearing on the wire, the Galley and Comms feeds disagreeing about the freshest event, or a
bridge seat not opening its desk (or opening the wrong one).

## 19. The captain's position — setting a mission

*(See [captains-position.md](features/captains-position.md).)*

1. Launch Sol. Confirm the station tab bar's leftmost entry reads **0 Captain**, ahead of
   **1 Nav**.
2. Press `0`. Confirm the Captain desk opens full-screen: "The ship's articles" header reading
   **Free sailing** in large text, then five groups (Free sailing / Hunt / Trade run / Lay low /
   Survey), each with a short flavor line and one or more selectable cards.
3. Click a card under **Hunt** (e.g. "Hunt: He3 haulers"). Confirm the articles header updates
   instantly to that mission's one-liner, with no confirmation prompt, and the card shows a
   selected/highlighted state.
4. Press `1` to return to Nav. Confirm a `☠ Captain` chip appears at the **top** of the summary
   chip strip, its second line matching the mission you just picked.
5. Press `6` for the Galley, `3` for the War room. Confirm the same `☠ Captain` chip (same text)
   appears at the top of the strip on both.
6. Click the `☠ Captain` chip from any desk. Confirm it jumps straight back to the Captain desk.
7. Pick **Free sailing** again. Confirm the chip everywhere reverts to "Free sailing".

**Broken looks like:** the Captain tab/key not present or not leading the bar, selecting a mission
requiring a second click/confirm, the chip missing from the strip or not docked at the top, or the
chip's text not matching the desk's own "ship's articles" headline.

---

## Appendix A — URL dev cheats (start from the testable situation)

Owner's bench rule (2026-07-18): *"being able to start from the testable situation helps us
smoke-test faster."* Append these to the map URL (`/map?a=1&b=2`) to boot straight into a set-up
instead of flying there. All are dev/test hooks — none affect a normal launch from the home page.

| Cheat | Effect |
|---|---|
| `?scenario=<name>` | Load `scenarios/<name>.json` (default `sol`; unknown → silent fall back to Sol). |
| `?start=<id>` | Jump the built world to a named start point (see the boot picker's registry). |
| **`?dock=<haven-id>`** | **Boot already CLAMPED ON at any dockable berth — clean state, live services (#288).** |
| **`?fuel=N`** | **Boot with N reaction-mass pulses in the tank (clamped to capacity) (#288).** |
| **`?credits=N`** | **Boot with N credits in the purse (#288).** |
| `?simhours=N` | Jump the sim clock to N hours at boot. |
| `?reveal=<bodyId>` | Chart a hidden body at boot (repeatable). |
| `?ellipse=1` | Append a visibly eccentric demo body (Kepler rails). |
| `?sling=<bodyId>` / `?skim=<bodyId>` | Boot onto an approach arc with a close pass / atmosphere graze. |
| `?expedition=1\|mining` | Spawn an away-team gig ALREADY ACCEPTED, its rock parked in shuttle range (#370). |
| `?deflection=1\|c\|s\|m` | Spawn the asteroid-deflection gig accepted, rock inbound, ship docked at Ringside (#394). |
| **`?secretlab=1`** | **Spawn a landable rock in shuttle range hiding a Vantar SECRET LAB, hidden door pre-revealed (#409).** |
| **`?kaamos=N\|all`** | **Assemble the first N PROJEKTI KAAMOS fragments (canonical order), or `all` — the intel readout + reach notice without a playthrough (#411).** |
| **`?bond=1`** | **Boot docked at a bar and FORCE the next ambient scare (shudder/buzzer/PA) to open a STRANGER-BOND — a co-present stranger stands you a cognac, the hero beat (#429).** |
| **`?nebula=N\|all`** | **Assemble the first N NEBULA MUTUAL fragments (canonical order), or `all` — arc 2's intel readout + the one-time "true terms" notice without a playthrough (#422).** |
| **`?converge=1`** | **Seed JUST ENOUGH of BOTH arcs (each side's joint threshold) and fire THE CONVERGENCE — the marquee one-time reveal — from a single URL (#422).** |

### NEBULA MUTUAL (arc 2) and THE CONVERGENCE — `?nebula=` / `?converge=1` (#422)

Arc 2 is the truth behind your resurrections; you gather its fragments by **dying and coming back**, by
reading the port posters twice, at a bar from a roving **Nebula adjuster**, off a **collector's writ**, and
from the **clinic's** books. Progress shows in the Captain's ledger as **"▓ NEBULA MUTUAL — N of 5 clauses"**,
the assembled shard texts readable beneath it (mirrors the KAAMOS readout).

- **`?nebula=N`** assembles the first N fragments in canonical order; **`?nebula=all`** assembles every one
  (5 intel shards + the capstone contract). Watch the ledger readout build, and at N ≥ 4 the one-time
  **"▓▓ THE POLICY'S TRUE TERMS RESOLVE"** notice fire.
- **`?converge=1`** is the marquee smoke test: it seeds exactly the joint threshold on **both** arcs
  (3 KAAMOS intel + 3 NEBULA intel) and fires **THE CONVERGENCE** — a full staged reveal card, above
  everything, stating that the sealed ice-moon berth and your brain-backup insurance are the same story.
  It fires **once per universe** (the seen-bit is persisted in the vault); reload and it does not replay.

**In-play delivery to verify by hand:**
- **Die** (get caught by a collector, or fly into a body) → on the resurrection card, a green monospace
  **glitch flash** ("…DO NOT REVIVE ORIGINAL") assembles `rebirth-glitch`. Getting caught by a **collector**
  first also shows the **writ glimpse** (`collector-writ`). A **second** death shows the **clinic's second
  page** (`clinic-ledger`).
- **Read a `📋 PIRATE INSURANCE` poster twice** (any port hall/bar) → the second read assembles `fine-print`.
- **At a bar**, when the seam offers **"▓ Ask about NEBULA"**, a roving adjuster gives `adjuster-tell`; once
  4 intel are in hand the same seam resolves the capstone **`policy-terms`**.

### The secret lab — `?secretlab=1` (#409)

A hidden door in the deep field conceals **Dr. Mielos Vantar's** sealed lab — seeded, rare, and normally
found only by sweeping the right square with the beach-comber metal detector. The cheat spawns **The
Hermit's Rock**, a plain landable Moon-kind body co-orbiting the berth (default Selene Gate; combine with
`?dock=<id>` to co-orbit another) comfortably inside one shuttle hop, whose surface is **forced** to hide a
lab with the **hidden door already revealed**. The loop:

1. `/map?secretlab=1` → boot docked, The Hermit's Rock alongside in shuttle range.
2. Open the shuttle door, land on the rock, walk down the tube into the deep field.
3. Find the **⚙ HIDDEN DOOR** console (it's already on the ground) and **[E]** to force it — a channelled
   progress bar; step away to abort.
4. The lab **appends** live (benches, stasis pods, a server spine). **[E]** the log screens to read Vantar's
   fragments; **[E]** `🗝 VANTAR'S CACHE` for the fat one-time payout.
5. **[E] `🖥 VANTAR — THE CORE LOG`** — the reveal: a nerve hit + a shown **d20** (≥9 salvages the tech for
   heroic pay; below, the dormant synthetics wake as a limited pack — run).

### The stranger-bond — `?bond=1` (#429)

The **warm twin** of the ambient-dread system (#430). The same scares that unsettle you — a hull-shudder, an
unexplained buzzer, a caution PA — can, at a docked bar, **open a co-present stranger** to you instead of only
chilling the room: a warm word, a whole new contact, or — the hero — a stranger who **stands you a cognac by
name** (`OLD PERIHELION`), on the fright. `?bond=1` boots you docked at a bar (default **The Space Bar**;
combine with `?dock=<id>` for another) and **forces the next ambient scare to bond**, guaranteeing the cognac
beat. The loop:

1. `/map?bond=1` → boot docked at The Space Bar with the regulars (strangers, no history yet) at the tables.
2. Walk the bar and wait a few seconds — the cheat fires the first hull-shudder within ~3 s (`〰` toast).
3. Right after the dread beat, the **stranger-bond toast** lands (`🥂 …`): a co-present stranger — “Barkeep —
   two of the **OLD PERIHELION**, on the fright.” — stands you the cognac, your goodwill with them warms, and
   the shared glass steadies your nerve (the #226 sanity-relief seam).
4. The forced bond is a **one-shot** — reload `/map?bond=1` to arm the cognac beat again. Unforced, bonds fire
   on a seeded chance (rarer, deeper when a scare runs cold) behind a cooldown, one per scare.

Effects apply through the **existing** contact systems (`ContactLedger.AddGoodwill`, `PourRum`) — a bonded
stranger becomes a **findable known contact** (they gain a drink/relationship row), never a new parallel path.

Discovery **persists per game-thread**: once found, a revisit to that body shows the door already revealed.
To exercise the *discovery* vector itself on an ordinary body, land empty-handed and **probe** (`[E]` on the
regolith) — the detector shrieks a proximity hint near the door and reveals it on the exact square.

### The dockable berths — `?dock=<id>` (#288 / #289)

`?dock=<id>` rides the **same clamp path a real arrival takes** (co-moving berth + `ClampOntoHaven`),
so a docked start is byte-for-byte a genuine dock — no parallel boot path. The berth id is any
**dockable station haven** (`IsHaven` + massless). The full list is **logged to the browser console
on every boot** (`[SpaceSails] Dockable berths — /map?dock=<id>: …`), sourced from
`SpaceSails.Core.DockableHavens`, so it's always current. In the shipped Sol scenario:

| Berth id | Where | Interior? |
|---|---|---|
| `cinder-roost` | Venus' clouds | ✅ walk ashore |
| `selene-gate` | Luna vicinity (Earth) | — |
| `the-space-bar` | off Mars (The Rusty Roadstead) | ✅ walk ashore |
| `red-eye` | **Jupiter — The Red Eye (#289 outer oasis)** | — |
| `ringside-exchange` | Saturn's rings | ✅ walk ashore |
| `the-tilt` | Uranus | ✅ walk ashore |
| `the-deep` | **Neptune — The Deep (#289 outer oasis)** | — |

The friendly `?start=` aliases work too (`?dock=ringside` == `?dock=ringside-exchange`). Every id is
swept by `DockedStartSweepTests` (boots clean, docked, pump live), and the **outer-oasis law** (#289 —
every gas giant from Jupiter out carries a self-sustaining fuel haven) is locked there as well.

**Try it:** open `/map?dock=the-deep&fuel=40&credits=9000`. Confirm you boot clamped on at The Deep out
at Neptune, the tank reads 40 pulses, the purse 9000 cr, and the Trade desk's `⛽ FILL HER UP` is live
(you're alongside a pump). Then `/map?dock=red-eye` — same, out at Jupiter among the Galilean moons.
