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

### 13a. Leaving while the telescope is still being wired (#765)

No cheat needed to reach this one, which is exactly the point: Map.razor renders the tracking post
**always** (`FullScreen="true"`, `d-none` off-desk, so a desk switch can never destroy the ledger), so
the post starts wiring itself on the map's *first* render — while the renderer module is still being
imported and the world is still being built.

1. Open the browser console, then load `/map` (Debug WASM is ideal: the boot takes tens of seconds).
2. While the loading door is still up — before the map appears — click **SpaceSails** in the nav bar,
   or press browser Back, to leave the page mid-boot.
3. Confirm the console stays clean. In particular: **no** `crit: WebAssemblyRenderer[100] Unhandled
   exception rendering component`, and no renderer.js "no canvas element with id" throw.
4. Repeat with a voyage that already holds a track (run §13 first, save, then **Continue** and leave
   mid-boot) — a non-empty ledger is what makes the abandoned wiring pass reach for a card canvas by id.
5. Discriminator — prove the guard is not just "never runs": load `/map`, let the boot **finish**, press
   `2`, run a sweep, and confirm the scope-wall tiles still animate at full frame rate. A telescope that
   stopped when nobody had left it would show dead black tiles here.

**Broken looks like:** any `WebAssemblyRenderer[100]` in the console after leaving a boot; a renderer.js
"no canvas element with id" throw; or — the quiet version — the frame rate falling a little further with
every `/map` you abandon, which is a discarded tracking post still riding `FrameTick` because it
subscribed itself *after* its own `Dispose` had run.

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

> **You no longer have to type them.** The most-walked entry points are offered as buttons in the
> game's own front door, under **⚙ DEV START SITES** (collapsed, below the berth list) — owner,
> 2026-07-26: *"These special places to start should be shown in the UI and marked as dev start
> sites."* The catalogue is `SpaceSails.Core.DevStarts` and the whole section is gated on the single
> `Map.ShowDevStarts` switch, so it can be turned off in one line when the game stops wanting a
> service door. Adding a row there is adding a button; keep it and this table in step.

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
| **`?kaamos=bounce`** | **Seat the freight agent holding the docket the board keeps sending back at every bar — PROJEKTI KAAMOS's FRONT DOOR (#635). Press `[E]` at any bar patron, take the job, and the filing bounces off your hull too: the arc appears in the Captain's ledger with no shard in hand.** |
| **`?kaamos=hq`** | **The whole KAAMOS route already ridden: every shard assembled, the berth-code resolved, the supply run filed, and the ship let go alongside the ice moon (#411). Add `&land=1` to put boots on it.** |
| **`?kaamos=pod`** | **Seat the cold KAAMOS supply pod under the ground this excursion lands on — probe any square with the metal detector and *earn* fragment 2 instead of being handed it (#411). Pair with `&land=1`.** |
| **`?kaamos=holder`** | **Seat the rare KAAMOS berth-holder at whatever bar you dock at, every watch — the tell (fragment 4) becomes playable on demand (#411). Pair with `&dock=<berth>`.** |
| **`?site=N`** | **Pre-select landing site N in the boarding panel — board straight onto a specific ground to compare site A vs B → a different surface deck-plan (#320).** |
| **`?land=1`** | **Ride the shuttle down as soon as the world is ready, onto the first landable body in reach (honours `?site=N`) — the real descent, skipping only the walk to the hatch and the boarding panel. The one-URL way to playtest a surface (#464).** |
| **`?land=<bodyId>`** | **Land on a NAMED body instead of whatever happens to be nearest — matched on id OR name, case-insensitively. If it is not landable from this berth it REFUSES and lists what is, because a cheat that silently lands you somewhere else means you playtest the wrong scene and then trust the result (#320).** |
| **`?sweep=N`** | **Put N (0–3) black-ops sweepers aboard whatever hull you board — the inspection team: 20 du sight inside a 70° cone, 34 du hearing through walls, a 3 s challenge before they shoot (#538).** |
| **`?reevers=N`** | **Set N Old Ones (0–8) down ON the captain the moment they land, already aware — the chase, the pack spacing and the #453 exchange (block roll, blood, five blows) in seconds instead of a long walk (#458).** |
| **`?bond=1`** | **Boot docked at a bar and FORCE the next ambient scare (shudder/buzzer/PA) to open a STRANGER-BOND — a co-present stranger stands you a cognac, the hero beat (#429).** |
| **`?oracle=1`** | **Seat the station oracle — Solenne “Static” Marsh — in the port-back corner of whatever bar you dock at, every watch. Unforced she is a fixture only ~55 % of watches, so her whole scene was a coin flip to open (#428). Pair with `&dock=<berth>`.** |
| **`?nebula=N\|all`** | **Assemble the first N NEBULA MUTUAL fragments (canonical order), or `all` — arc 2's intel readout, its state transitions, and (only at `all`, which is the only value that includes the capstone contract) the one-time "true terms" notice, without a playthrough (#422).** |
| **`?nebula=adjuster`** | **Seat the rare Nebula Mutual adjuster at whatever bar you dock at, every watch — the tell (fragment 3) becomes playable on demand instead of merely grantable (#422). Pair with `&dock=<berth>`.** |
| **`?converge=1`** | **Seed JUST ENOUGH of BOTH arcs (each side's joint threshold) and fire THE CONVERGENCE — the marquee one-time reveal — from a single URL (#422).** |
| **`?archive=1`** | **Board a derelict that is CARRYING A COLD-ARCHIVE NODE — arc 2's only in-person scene. Implies `?wreck=ventedbyoneoftheirown`, the one cause Core guarantees a node on.** |
| **`?death=<cause>`** | **KILL THE CAPTAIN AT BOOT, through the real pipeline — the death card, the freeze beat and the brain-backup wake, without dying for them (#621).** |
| **`?ashore=1`** | **Boot docked AND ALREADY STANDING IN THE BAR — the ship → airlock → tube → immigration hall → bar walk already walked (#428). Every bar beat begins with that walk; in a hidden/automated tab it cannot be walked at all. Pairs with `?dock=` / `?start=`, and with every bar cheat.** |
| **`?watchers=1`** | **Open the MONOLITH GROUND'S attentive window and cut the dwell from forty seconds to two, so the strange-things-happen beat (#649) can be watched on demand. Stand at the stone. It is rare by design — one visit-window in three, and then only if you stay — and this changes the GATES and nothing else, so what you see is what a captain sees. Pair with `&dock=the-space-bar&body=phobos&site=0&land=1`, and with `&reevers=3` for the variant that needs a pack on the field.** |
| **`?nerve=N`** | **Seed the nerve gauge at N of 10 whole pips at boot (#428/#480). Clamps to the gauge; `?nerve=10` is the shipped default. The only way to reach a sanity beat without being hunted for minutes first. #784 adds three WORDS beside the number, for links a person has to read: `?nerve=shot` (0), `?nerve=low` (2), `?nerve=half` (5). Same flag, same clamp — the words are spellings of the number and never a second parser.** |
| **`?hurt=N`** | **STEP OUT OF THE BOAT ALREADY MARKED (#784) — N of `CaptainCondition`'s five blows already landed, so the condition pips under the nerve bar read *bruised* / *bleeding* / *badly cut* from the first frame. Built for the SHORT REST's healing half, which on an unmarked captain is a mechanic with nothing to demonstrate; it is also the one way to see the block roll's modifier stack, the wounded breathing rate and the low-health nerve beat without being caught first. It can never seed the fifth blow — booting a tester into a death card is not a demo. Pair with `&tablescene=free&approach=0&nerve=low`.** |
| **`?shelter=1`** | **SET THE BOOTS DOWN AT A SHELTER (#728) — a pace outside the door of the one building on the ground that fills a tank AND fills a magazine.** The shelters seed DEEP on purpose (`SurfaceShelter.PlacesOn` keeps them out of the landing band), so every look at their plates, their receipts and the ammunition readout above them used to cost a two-minute walk across 310 x 260 du of regolith — which is how the owner came to be standing between the two fixtures in a live smoke run saying *"on shelters I always forget which is which."* It moves ONE fact, where you are standing, and stands you OUTSIDE: the proximity cycle, the arrival line and the pressure crossing are part of what you came to look at. What a tester should see: `🫁 CHARGING RACK — FILLS YOUR TANK` on one wall and `🔫 EMERGENCY LOCKER — FILLS YOUR MAGAZINES` on the other, a `🔫 MAGAZINES · K-77 12/99 in the sling · R-3B 12/99 in the sling` line under the motion tracker, and — on `[E]` at the press — that line moving to `99/99` in the same breath as the receipt. Pair with `&mags=N`. *(Also a button in the front door's **⚙ DEV START SITES** list — ⛺ “The shelter — air on one wall, rounds on the other”.)* |
| **`?mags=N`** | **BRING THE SLING DOWN HOLDING N ROUNDS EACH (#728).** Every sentry lands full (99) on a fresh ship, so the magazines readout, the shelter press's receipt and both of the locker's refusals could otherwise only be looked at after a real firefight. `?mags=0` is the dry sling — the state the on-foot HUD had no way of showing at all before #728 — and `?mags=99` is the shipped default, which is what makes the press answer *"finds them full, and goes back to sleep"*. It sets the ONE number: the roster, the ammunition kind and every law downstream are the shipped ones. Applied where the magazines cross into the excursion, never later. |
| **`?dark=1`** | **Put the FIXTURES OUT on every floor this excursion walks — the suit's forward-facing headlights become the whole of the seeing, and everything outside the cone is BLACK rather than dim (#708). The FOUND HALLS (#677) declare themselves dark and are the only floors that do, so this is how every OTHER floor is seen in the dark — and how the cone is exercised without hunting for the one site in fifty that has galleries. It changes ONE fact — what `UndergroundComplex.IsDark` answers — and nothing else: collision, air, the pack, the sentries and the motion tracker all behave exactly as they do with the lights on, so you can walk into what you cannot see, and something you cannot see can walk into you. Above ground is never dark (a surface has a sun). Pair with `&secretlab=deep&land=1&floor=4`.** |
| **`?process=N`** | **How long processing one document takes, in sim seconds — `?process=0` makes it INSTANT (#696). Leaving a paper or a file with 🫳, and reading a paper as a clue at the tracker, are a twenty-second hold of standing still; that IS the mechanic, and it is exactly the wrong thing to make a story test sit through. Any other value tunes the feel from the URL without a rebuild. Pair with `&dock=the-tilt&site=0&land=1`. There is deliberately no switch for what a hold costs in AIR, because nothing computes that: the hold passes sim time and the suit prices sim time, which is the whole of the owner's ruling.** |
| **`?book=N` / `?book=on`** | **Put THE ODD BOOK in every would-be-empty room this excursion searches (#701). `1`–`10` force that catalog entry, which is how all ten authored texts get read on demand; `on` (or `all`/`any`) forces the SEEDED entry, i.e. the shipped selection with the one-in-six gate taken off, which is how the Laboratory/Transit-station weighting is watched working. It cannot put a book in an OCCUPIED room — a book is what a would-be-empty room has *instead of* the empty line, and a cheat that laid one on top of a pallet would have you playtesting a room the game cannot produce. It is an ARGUMENT to `OddBooks.Search` and never a second answer OR-ed in beside it (the `?dark=1` rule). Pair with `&secretlab=deep&land=1&floor=2`.** |
| **`?autowalk=1`** | **CLICK THE DECK AND THE CAPTAIN WALKS THERE — A\* over the same walls the collision uses, at the same walking speed, on the on-foot views (a surface excursion and every Hive floor) (#729). Owner: *"So our testing does not hang on slow MCP speed to browser."* An automation-driven tab moves in keypress bursts with dead time between them, and three Reever deaths in one morning were pacing artifacts rather than findings; this makes a crossing one action instead of forty. It is NOT a teleport and NOT a faster walk: every step goes through the ordinary per-frame movement, so air drains, the nerve frays, the tracker rings, the auto-doors cycle and the Old Ones close exactly as they do under WASD. **Any movement key cancels instantly** — the keys always win. Click a console, a hatch or a door and the walk stops ADJACENT to it, so `[E]` is live the moment it ends. Click somewhere no corridor reaches and it says so out loud rather than shrugging. A drag still pans the deck plan. Pair with `&secretlab=deep&land=1&floor=4`, or with `&found=1&land=1`.** |
| **`?found=1`** | **Park the one rock in the system with a band NOBODY DUG under the band nobody listed (#677), set down at the lift head, and start with every authority this site ever issued already in the wallet — including the last one, which is the way past the seam. About one site in fifty has galleries and the way in is a card somebody left in a room eleven floors down, so without this the feature is unreachable in practice. It implies `?secretlab=1` (there is no other way down). It overrides no Core fact: the rock's whole shape — its depth, its two kinds, its unlisted band and its halls — is seeded off its body id (`UndergroundComplex.FoundBandCheatSiteId`) exactly like every other site, so what you walk is what a captain would walk. The cards are minted through the real `AuthorityCard` and put in the real satchel, so the panel, the gate, the refusal ladder and the wallet fan all behave as they do for somebody who earned them. Pair with `&land=1`, and with `&floor=17` to ride straight to the first gallery.** *(It is also a button in the front door's **⚙ DEV START SITES** list — 🕳 “The halls nobody dug”.)* |
| **`?card=next` / `?card=N` / `?card=all`** | **MINT AN AUTHORITY CARD BEFORE THE FIRST RIDE (#693) — the one cheat that makes the CARDED lift row, the gate beat and the refusal ladder reachable on an ordinary site.** #692 shipped all three and closed with the honest note that none of them had been seen in a browser: *"reaching the row needs an authority card in the wallet and no dev cheat mints one"*. `next` mints the band under wherever you are set down — the gate you will be standing at — asked of `NextShaftBelow` so it steps over the band of nothing under the unlisted floors (#677). `N` mints that band specifically, which is how the WRONG-card refusal is seen. `all` is `?found=1`'s whole wallet on any rock. It names a **band**, never a card id: a body typed into a URL is a body the landing may not be on, and the cheat would mint paper no gate on the ground reads. A band the site does not have mints nothing **and says so**, naming the bands it does have. Minted through the real `AuthorityCard` into the real satchel, so the panel, the gate, the refusal and the wallet fan behave exactly as they do for a captain who earned it. Implies `?secretlab=1`; pair with `&secretlab=deep` or `&found=1` to choose the rock. Try `/map?secretlab=deep&land=1&floor=1&card=next`.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🎫 “The lift row the card unlocks”.)* |
| **`?kit=1`** | **ASSEMBLE THE FIELD DOSSIER ON THE FIRST PIECE OF SOMEBODY'S KIT, WITH EVERY SENTENCE IT CAN CARRY (#774/#588).** The dossier is the rarest beat on the regolith: it wants three *papers* rooms inside ONE excursion at one room in eight, and its four-sentence form — the person, the next of kin, what that family knows, and the phrase that opens a door somewhere else — is two more one-in-three rolls behind that. Which is exactly why #774 shipped: the card raised, all four sentences pulsed **under its own backdrop**, and nobody could stand in front of the scene to notice. This moves the two GATES and nothing behind them — the stranger, the family, the hint, the in and the moon they name are the seeded ones for the room you actually completed, so what you read is a card a captain can genuinely be handed. Pair with `&outpost=1` for the shortest road to one: the hut's SOMEBODY'S EFFECTS console is a piece of kit in its own right (#588), so one press assembles the whole thing. Try `/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1`.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🗂 “Whose kit was this — the whole dossier”.)* |
| **`?tablescene=1`** | **BOOT THE TABLE SCENE (#746) — the B1 canteen of a deep site, with people in it, one URL from the front door. Walk to a table with somebody at it, press `[E]`, and ask to join. It implies the whole route (`?secretlab=deep&land=1&floor=1`) rather than adding a fourth spelling of it, sets the captain down IN the canteen, and turns `?autowalk=1` on because the last leg is a walk across a room. It does NOT force who is at the tables: the rota is seeded off the site and the watch like any other shift (#709), and a cheat that seated THE HAND for you would be testing a room that does not ship — if this watch has no Hand in it, that is the room, and the next shift is a reload away. Three of #709's cast are scenes (the hand, the fitter, the temp); everybody else keeps their one breath.**<br><br>**#792 · READ THE ROOM BEFORE YOU CROSS IT.** Owner: *"people looking to sit down look at those like hungry wild beasts look at their prey… Now I have trouble finding a free table."* Every top now draws the chairs it actually seats, in three marks and no words: a **grey bar** is a chair nobody is in at a table nobody is at; a **green bar** is a free chair at a table somebody is ALREADY at — the **invitation**, and the whole of the second glance; a **warm filled body with a bar behind its shoulders** is somebody sitting there, in the same chair-back idiom the seated captain got in #788. **Two warm ticks struck over a top mean a conversation** — there is something to **overhear** — and a top without them holds somebody on their own, which is who `[E]` can ask to join. None of it is worked out on the glass: the occupancy and the conversation both come off `CanteenRegulars.Tables` at the frozen watch, so a chair drawn free is a chair the press will offer. Pair with `&watch=2` and then `&watch=5` — the same room, two different answers to all three glances. |
| **`?counter=1`** | **BOOT THE COUNTER (#756) — the B1 cantina hall of a deep site with the captain standing AT THE COUNTER, one URL from the front door. What a tester should see: press `[E]` and the SERVICE CARD opens, already on the menu — COMPANY COFFEE at 2 cr, the CAGE CREW'S BREAKFAST, SUB-BASEMENT STEW, and three pours that joke about the deep; order anything and the receipt answers **on the card** (#736), the purse on the card drops by exactly the price on the button, and food does not tilt the deck the way a pour does. It is the same card the Tilt bar opens (#247) pointed at a different venue — so the two verbs that need a person (a round for the room, "hear a rumor") are absent here, because nobody is behind this counter. Implies the whole route (`?secretlab=deep&land=1&floor=1`) rather than adding another spelling of it. It forces nothing about the room: the watch, the rota and the purse are whatever the boot gave you. Pair with `&credits=50000` to price the whole card, or `&watch=2` for a heaving hall.** **#780 — what BROKEN looks like here, because it shipped that way for a day:** the menu renders but reads as a greyed-out panel *behind glass*, its buy buttons dim and apparently dead, and the owner's own report was *"How do I buy the drink here?... There is no button to pay there?"*. That is the menu having slid under the card's **pinned action row**, whose sticky foot paints a 12rem near-black scrim over everything below it. The menu belongs **above** that foot, in the card's scrolling body — and the five illustrated rows must show their photographs beside the words rather than in place of them. **#792 — and now the eight stools are ON THE FLOOR.** They have been in Core since #756, occupied or not, watch by watch, and were drawn nowhere: a captain could only learn the row was full by walking up and pressing. A **hollow grey ring** is a free stool at a counter nobody is at; a **hollow green ring** is a free stool at a counter somebody IS at (the same **invitation** ink the tables use — one language for one question); a **filled warm disc** is a stool with somebody up on it, and it has no back drawn because a bar stool does not have one. Stool *n* on the floor is stool *n* in `🪑 STOOL n · THE COUNTER`, so the seat you can see is free is the seat `?stool=1`'s pick-or-default hands you. Compare `&watch=2` (a queue at the bar) with `&watch=5` (one soul and seven empty seats). *(Also a button in the front door's **⚙ DEV START SITES** list — 🍹 “The counter, ready to order”.)* |
| **`?park=1`** | **BOOT THE PARK (#759) — the same B1 route as `?counter=1`, with the last leg walked through the gate instead of to the counter, so the captain starts standing on the gravel INSIDE the park behind the bar. What a tester should see: the floor is GREEN (`art/b1-park-walk.jpg`, laid in panels across a room six times wider than it is deep, so nothing is stretched); the wall back toward the canteen is a WINDOW WALL, drawn in the ship's own window ink rather than the poured hull line — walk into it and you stop, look through it and the hall is there, which is the whole point of the room; the ways in and out are the GATES in its near wall — the hall's own rib corridor (which used to be a dead end), every other rib that points its way, and the garden walk off the main corridor: **2–5 of them**, because #775 made the room a thoroughfare rather than a cul-de-sac. Raised beds are solid boxes stencilled `🌱 BED n · <CROP> · TO CANTEEN 1` — the same CANTEEN 1 the counter's own sign says, which is the entire food connection and is never pointed out; the gravel walk bends three times down the room with a steel bench on the outside of every bend; five floodlight masts stand against the far wall; one lone figure sits on the furthest bench with nothing to press. The field book files ONE line on the first step in, once per excursion, because the plate at the gate says ATTENDANCE IS RECORDED and that is a sentence rather than a system.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🌳 “The park behind the bar”.)* |
| **`?spread=1`** | **BOOT THE SEATED SPREAD (#784) — the phase-two loop in thirty seconds.** Owner's own ask: *"we probably need a start point where we have things in our inventory we can process (when our HUD UI state is sitting down with enough privacy)."* Implies the whole `?tablescene=` route and then walks three more legs: it sets the captain down at a **CABINET top** — the private end of the exposure ladder, and the owner's canonical processing venue (*"that is the place I want to process inventory"*) — **sits them down through the same `[E]` handler a captain uses**, and puts **three finds in the sleeve** (two papers and a file on somebody: the only two kinds that have a gist). What a tester should see: **no backdrop and no card**. The seated panel is a **HUD STRIP** docked at the foot of the deck — the hall stays lit, the walkers keep moving, the park stays green — carrying the **customer line** (`🪑 A CABINET TABLE · NO POUR — nothing bought… · REST 0/3 pips · the door is shut — nobody is crossing the room to you`), the last thing the table said, and the seated verbs. Press **`[I]`**: the satchel opens on a third page, **🗂 SPREAD**, which exists only while you are in a seat. Press a paper and the **digging bar** runs over the captain's own mark on the live deck for the same 20 s a photograph takes (`?process=N` tunes it, `?process=0` makes it instant) — the same bar and the same slot, wearing the pen instead of the camera. When it fills, the gist lands in the **detective book** (📓 NOTES) **and the sheet stays in your sleeve**, which is the whole difference from 🫳. Stand up mid-dig and it is abandoned out loud with nothing filed; sit at the counter instead and the spread is refused out loud (the gumshoe rule). Try `/map?spread=1`, and `/map?spread=1&process=3` for a quick one. *(Also a button in the front door's **⚙ DEV START SITES** list — 🗂🪑 "Sat down in a cabinet with the papers out".)* |
| **`?frontdoor=1`** | **BOOT THE CANTEEN'S FRONT DOOR (#775) — the same B1 route as `?counter=1`, stopped one room SHORT: the captain stands OUT ON THE MAIN CORRIDOR, facing the hall's own entrance. Owner, walking the new B1: *"The bar/canteen needs DOORS ON THE MAIN CORRIDOR — today you have to really look for the way in; a venue's entrance should find YOU."* What a tester should see: a violet imported leaf in the spine's own long wall with **🍸 CANTEEN 1 · ENTRANCE** stencilled beside it on the corridor side, placed at the LIFT (the carve puts the first one wherever the walker is, which on most floors is directly across the corridor from the car); walk south/north into it and you are in the bar without ever turning down a rib. Then walk the corridor: a hall of 5 800 – 7 300 du² carries **⇥ EXIT 2 · KEEP CLEAR** and, on the biggest, **EXIT 3** as well — the count is `UndergroundComplex.HallEgressDoors` of the room's own floor area (one per 1 500 du², never fewer than three), so it is derived and not typed. Every one of them is a real gap in a real wall: the guards walk an A\* across each jamb inside a box that leaves no way round.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🚪 “The canteen's front door, from the corridor”.)* |
| **`?freight=1`** | **BOOT THE GOODS HOIST (#775) — the same hall, standing on the hall floor in front of the freight shutter at the end of the counter's own service band.** Owner: *"The facility needs FREIGHT ACCESS somewhere — a freight elevator or a long drive-in ramp for supplies; eighty seats of food and twelve beds of produce do not arrive through a personnel door."* What a tester should see: **🚛 GOODS HOIST 1** painted on the floor in front of a shut roller door in the counter's own line, at the end of the band nearest the park's gate — behind it, through the glass, are the beds it exists to carry. Walk into the car and you stop: it is a sealed twelve-by-five box in the service band, four walls, and the collision field agrees with the drawing. Press `[E]` on the shutter and it says **🔒 GOODS HOIST 1 · DELIVERIES 04:00–06:00 · CREW SIDE ONLY** — the refusal is a sentence rather than an absence, which is the whole of the feature. Nothing simulates freight and nothing pretends to.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🚛 “The goods hoist that will not take you”.)* |
| **`?designate=1`** | **THE WHOLE MANUAL-FIRE LOOP, AT THE SHUTTER IT WAS WRITTEN FOR (#803).** Owner: *"we might want to hand-load them into the bots for some special purposes, like shooting a mechanical lock (we will need to use the handheld captain's control to set the guns / gun to fire at something manually, that UI is missing)."* The `?freight=1` boot with the pieces assembled: you are standing at the **GOODS HOIST**, one sentry is **SET DOWN** beside you reading **05** — one round short of a hasp, deliberately — and **12 loose rounds** are in your pocket, the size of find a hut's ammunition locker deals in. What a tester should do, and see: **(1)** press `I` standing over the bot → the round row now offers it (it did not, before: the satchel would only load a bot reading exactly 00) → the drum goes **05 → 17** and the `🔫 MAGAZINES` line under the tracker says the same number; **(2)** press **📻 Remote** → **🎯 Designate a target** → the gun's own row (unit, drum, what is in it) → the shutter's row (plate, distance, what it costs); **(3)** press it → six flat cracks, the hasp comes off, the shutter's plate changes from 🔒 to 🕳, the wall behind it is gone and you can walk into the car; the drum reads **11**; the handset itself carries the shot's line, what is behind the door, and (once) what the noise actually cost. Then walk to a rib's far end and point the same gun at a **SEALED WAY**: it refuses, in its own words, and spends nothing. *(Also a button in the front door's **⚙ DEV START SITES** list — 🔫🔒 “Hand-load a gun and shoot a lock off”.)* |
| **`?parkwalk=1`** | **BOOT THE CROSSING (#775) — THE PARK IS A THOROUGHFARE NOW, not a cul-de-sac.** Stands the captain on the MAIN CORRIDOR at the mouth of the GARDEN WALK rather than inside the green, because the crossing is the feature and a crossing has to be started outside. Owner, 2026-08-09: *"let's have multiple doors to the park… it is a kind of place people like to walk through on their way."* #790 shipped it with ONE gate at the end of the hall's rib corridor. Boot it and walk: every rib that points at the park now runs the extra sixteen du and opens into it, and there is always a **GARDEN WALK** — a dedicated passage off the MAIN CORRIDOR stencilled **⟶ THE PARK**, cut where the spine's park-side face has the most room, so a park never has fewer than two ways in however the ribs' seeded directions fell. Shipped parks carry **2 – 5 gates**. What a tester should see: walk the spine, turn down the garden walk, cross the gravel, and come out of a DIFFERENT gate into a different rib corridor — the route between two places on B1 goes through the green instead of round it. The hall-side wall is still GLASS and still stops you; no gate is ever cut through it. *(Also a button in the front door's **⚙ DEV START SITES** list — 🌳🚶 “Straight through the park, and out the other side”.)* |
| **`?parkback=1`** | **BOOT THE FAR SIDE OF THE GREEN (#801) — the same B1 route as `?park=1`, walked one leg further: across the gravel to the wall that used to be the painted horizon.** Owner: *"we could have rooms to explore below the park also (on the map). Walking through the park is fun, it should not be the edge."* What a tester should see: **doors in the far wall** — one in each bay between two floodlight masts, four of them on the shipped field, each about 46 du wide and 12 deep behind. Plates beside them, on the park side, in the beds' own register: `🌱 POTTING · SOIL, TRAYS, GRIT`, `🧰 GROUNDS PLANT · LAMPS, FEED, TIMERS`, `❄ COLD ROOM · TO CANTEEN 1`, `🧤 GROUNDS STORE · TOOLS SIGNED OUT AND BACK`, `🚿 WASH-DOWN`, `📋 GROUNDS OFFICE · ROTA POSTED` — the cold room names the same CANTEEN 1 the beds do, and nothing points that out. Walk through one: it is an ordinary room with a `🔦 SEARCH THE ROOM` console in it, on the floor's own room list, and the A\* audit that walks every room from the car walks these. Then walk back out — the far wall is a wall everywhere else, and the guard proves it by pouring every one of these doors shut and demanding the rooms go dark. *(Also a button in the front door's **⚙ DEV START SITES** list — 🌳🚪 "The far side of the green".)* |
| **`?goodscar=1`** | **BOOT THE SECOND CAR (#801) — B1, standing at the OTHER lift.** Owner: *"that elevator would be so busy it would be packed and never available… it is a choke point, and the whole lab would be too easily guarded by just having the guard posted in front of the one elevator."* What a tester should see: an alcove in the **lower** face of the main corridor, at its **blind end** — past the last cross corridor, about 170 du from the cage, which is the length of the building. The console reads **🛗 GOODS CAR 2 · THIS BAND ONLY**. Press `[E]`: the panel opens with a different line under the title — *"The goods car. It runs these floors and it does not climb out: for the surface, and for anything below this band, the cage is at the other end of the corridor."* — and the rows are this band's four floors and **nothing else**: no SURFACE, no sealed row, no card named. Ride it: the doors open on the new floor at THIS car's own doorstep, on the lower face, not at the cage. Then walk the corridor to the cage and time it — that walk is the feature. *(Also a button in the front door's **⚙ DEV START SITES** list — 🛗🛗 "The other car, at the blind end of the corridor".)* |
| **`?threads=1`** | **BOOT THE RED PEN, WITH A CASE ALREADY IN THE BOOK (#741).** Owner, authorising the build: *"I dream of drawing those conspiracy board connecting red lines… I guess it could be a red pen only used to connect the things."* Implies the whole `?spread=1` route — the cabinet, the docked strip, three finds in the sleeve — and adds the one thing the pen cannot work without: **six entries already filed, from two grounds you are not standing on**. It opens the pocket straight onto 📓 NOTES in the 🧵 THE CASE reading. What a tester should see: the notes are now **title-first nodes**, collapsed, each with a caret and either a 🧵 count or the words *loose end*. Press one to fold it open into **bullets** (the full first sentence is the first bullet — the title's clip loses no word). Then press **🖊 THE RED PEN**: pressing a title now means *one end of a line*. Press a second and the line is drawn — a soft chime, a short red connector settling between the two rows, and **the list reorders so the two sit together**. The same two presses take it off again (the eraser end). **The rhyme to spot, and the game never once remarks on it:** the same door — **The Tilt** — is named in entries filed on *two different moons*, once as where a dead specialist's family is still waiting and once as where the phrase that fell out of their kit will open something. Every word of it is shipped dossier prose (#588/#774); nothing is highlighted, nothing is suggested, and **nothing congratulates you when you get it** — that is the register, by ruling. Off your feet the pen refuses out loud, and at the bar desk it refuses for the gumshoe rule, both on the same ladder the spread uses. Try `/map?threads=1`. *(Also a button in the front door's **⚙ DEV START SITES** list — 🖊🧵 "The red pen, and a case to draw on".)* |
| **`?roll=hi` / `?roll=lo`** | **FORCE THE ENCOUNTER BAND (#746). `hi` makes every rolled move land YES, `lo` makes it NO — AND THE SCENE MOVES; `mid` forces YES, BUT. Owner, in the issue: *"testing is a feature."* It overrides the BAND and never the roll — the dice still cast, the named modifier stack still reads truthfully on the panel, and the scene that plays out is the scene a captain would get, because a cheat that showed you a different scene would be worse than no cheat at all. The only rolled move today is THE HAND's ask about work, so `?tablescene=1&roll=lo` is how the refusal's three consequences (the table hardens, the fitter opens, the temp overheard it) get watched on demand. Pair with `&tablescene=1`.** |
| **`?stool=1`** | **BOOT SITTING AT THE COUNTER (#756) — the high chairs the owner asked for.** Owner, live: *"Also there should be high chairs so sitting at the bar desk is also possible."* Implies the whole `?counter=1` route and then walks the last, last leg: the service card is open **and you are up on a stool**. What a tester should see: the plate reads **🪑 STOOL n · THE COUNTER**; the picture on the card is no longer the bar desk but the **window wall and the park behind it** (#759), because standing you look at the counter and seated you look over it; **the menu is still there, unmoved** — that is what "the keep serves you seated" means, and ordering from the stool debits and answers exactly as it does standing. The only new verb is **WAIT**. Getting down leaves you standing at the counter, not out of the bar. Which stool you land on is the room's answer off the frozen watch, never the cheat's — pair with `&watch=N` to sit at a heaving counter or an empty one. *(Also a button in the front door's **⚙ DEV START SITES** list.)* |
| **`?neighbour=1` / `?neighbour=0`** | **FORCE WHETHER THE ONE BESIDE YOU TURNS (#756).** `?approach=1`'s sibling at the bar, and it needs a lever even more than the tables do: whether anybody speaks sits behind a seeded roll **and** a seeded occupancy, so a stool with empty seats either side is a silence the dice cannot break however busy the watch is — **proximity IS the invitation** (owner). `1` turns her on the very next wait: she does **not** ask to sit, because she is already sitting, so the ladder opens at a remark thrown sideways — say something back, stand her one off a counter eighteen inches away, then ask what has been bothering her (the one rung the field book keeps). `0` means nobody ever turns, which is the other half of the feature: the counter answers, in words, and which of **three** silences you get depends on whether the seat beside you is empty, taken-and-quiet, or in a hall that has been emptied. `?neighbor=` is accepted too. Try `/map?stool=1&neighbour=1` and `/map?stool=1&neighbour=0`. *(Both are buttons in the front door's **⚙ DEV START SITES** list.)* |
| **`?tablescene=free`** | **BOOT STANDING AT A TABLE WITH NOBODY AT IT (#757) — the table the owner could not sit down at.** Owner, live in the hall: *"I have empty table but I cannot sit down"*, and, minutes later, *"the normal way to operate in a bar or restaurant is still not implemented."* Same route as `?tablescene=1` (it implies `?secretlab=deep&land=1&floor=1` and turns `?autowalk=1` on) with a different last step: it sets the captain down at a **FREE top** — one of the room's own, taken off the same `CanteenRegulars.Tables` call the deck was drawn with, never a coordinate the cheat typed. The top is plated **🪑 A FREE TABLE — SIT DOWN**, and pressing `[E]` does exactly that: the panel's **first line confirms it** (*"You sit down. The table is yours."*) before it offers you a single verb. Then **SIT A WHILE — see who comes**, which is the seated state's whole verb — sitting down alone is a choice to be *findable*, and sitting a while asks the room whether it has anything for you. **Stand up** ends it. One press sits you down, the seat is where you stood, and there is no chair menu. <br><br>**#783 · The panel wears the table, in whichever of its two states you are in.** On a busy watch you get the wary line (back to the wall) over `art/b1-your-own-table.jpg` — the empty chair opposite, which *is* the wait beat. On a **quiet** watch, or with a **drink bought at the counter still in your hand**, the same sit is a SHORT REST: the relaxation line (boots up on the spare chair) over `art/b1-short-rest.jpg`, and standing up says something different too. Try `&watch=5` for the rest and `&watch=2` for the watch; buy a pour at the counter first (`?counter=1`) and even the heaving watch turns into a rest. Pair with `&approach=1` (below) to make somebody actually come, and with `&watch=N` to choose how full the hall is. |
| **`?approach=1` / `?approach=0`** | **FORCE WHETHER ANYBODY CROSSES THE ROOM (#757).** Whether a wait at a table you took alone brings somebody over is a seeded roll on (site, floor, top, watch, beat) scaled by how full the hall is that shift — so **both halves of the feature are otherwise reachable only by luck**, and #693's rule applies: *a scene nobody can reach on demand is a scene that ships broken.* `1` brings her over on the very next **SIT A WHILE**: she asks for the chair, offers to buy the round, and only then says what she came over for — the three-rung ladder (owner: *"think Gandalf knocking on Bilbo's door"*). `0` means **nobody ever comes**, which is not the absence of the feature but the other half of it: the hall answers, in words, that nothing is going to happen, and on the small watch an eighty-seat room that used to be loud saying nothing IS the beat. It forces **whether** and never **who** or **what** — her plate, her ladder and her ask are the ones a captain would get. Try `/map?tablescene=free&approach=1` and `/map?tablescene=free&watch=5&approach=0`. *(Both are buttons in the front door's **⚙ DEV START SITES** list — 🪑 “A table with nobody at it — sit down, and sit a while” and 🪑🕳 “…and the watch where nobody comes”, which on watch 5 is also the **short rest** state.)* |
| **`?tablescene=free&nerve=low&hurt=3`** | **THE SHORT REST, WATCHABLE (#784) — the same free table, with a captain who has something to get back.** Owner: *"Sitting down relaxes and heals"* / *"it is like short rest in TTRPG."* Take the table and press **WAIT**: the avatar on the deck is drawn SITTING (a chair back, folded body, arms on the table — no heading spoke), and each beat eases a whole nerve pip and, on the third, knits one of the five blows back. The panel says it after the room's own silence line, and the nerve ledger names it. Then keep pressing: the ceiling bites at `ShortRest.NervePipCapPerWatch` and the game tells you so — *a short rest is short*, and the rest of you comes back in a bunk. Add `&counter=1` first and buy a pour: the same rest lands in half the beats (the pour multiplies the RATE, never the ceiling). And press **W** while seated: the captain does not move — you are asked whether to stand up, `Esc` keeps your seat. |
| **`?patrol=1` / `?patrol=2`** | **BOOT ONTO A FLOOR WITH A ROUND ON IT (#804) — B2 of a deep site, which is the first floor under the bar and therefore the first with a security ROTA walking it. Owner: *"the rotating guards on the lower more restricted levels… ideally we could see them move and wait for them to pass before we pass them."* Implies the whole route (`?secretlab=deep&land=1&floor=2`); `2` forces the two-guard watch, which is otherwise a coin flip and is the harder scene to time. It forces nothing else — which stops the round walks, which direction it runs and who is on it are whatever the watch says, because a cheat that pinned the beat would be testing a floor that does not ship.**<br><br>**WHAT A TESTER SHOULD SEE, in this order, standing at the car and doing nothing.** First the **motion fan**: a smudged return with a bearing, moving, well before anything is on the deck — that is a guard heard through poured wall at #591's degraded reach, and it is the owner's *"we need our motion detector to warn us… before they spot us"* working with no new instrument code. Then, if they come closer without a clear line to you, one line: **👣 *Boots on shotcrete, out of sight and in no hurry…*** Then and only then a **green mark** with `PATROL 1` over it, drawn the instant your own line of sight reaches them and gone the instant a wall gets between you. **They stand five seconds at every stop and walk 3.2 du/s between them — that gap is the whole game.** Walk down a rib behind one, wait at a mouth for one to pass, and come back on the next watch to find the same stops walked in a different order.<br><br>**BROKEN LOOKS LIKE:** a mark that stays on the deck when you step behind a wall (the gate is off); a mark that appears at the far end of a floor before the fan has said anything (the eye's reach and the fan's have swapped); a guard who challenges you the moment you can see them (the two reaches have collapsed into one, and there is nothing to time); a round that walks into a wall and stops (a stop that is not on the A\* — the client sweep is the guard for that); or a guard drawn on a floor of the Hive wearing `SWEEP-1` (the sweep team's band leaking into the underground deck). Pair with `&watch=N` to change the round, and `&autowalk=1` to cross the floor without WASD. *(Also a button in the front door's **⚙ DEV START SITES** list — 👮🚶 “B2 — somebody is walking the floor”.)* |
| **`?badge=1`** | **MINT THIS SITE'S OWN PASS AND PUT YOU IN FRONT OF SOMEBODY WHO READS IT (#804).** Implies `?patrol=1`'s whole route, because a pass with nobody to show it to is not a thing anybody can test. Minting is the ONLY thing it does: the guard still has to see you, the wallet is still read by Core, and what is said is what would have been said had the pass been earned. **Earned, it comes off the cage crew.** The Hand at a B1 table hands you a day-labour chit (#746), the chit opens the gate to the band below (#752), and the site does what a site does with a body that has arrived on somebody's account — it puts you on its books at the bottom of the cage. **The gig does not pay in coin; it pays in paper.**<br><br>**WHAT A TESTER SHOULD SEE.** 🎒 `I` shows **🪪 SITE PASS · GENERAL HANDS · <SITE> SITE** in the wallet. Walk straight at a round: the card **👮 THE ROUND STOPS AT YOU** goes up, and its amber row says he reads the face, the site code and the tier, hands it back, and mentions the wet floor round the corner. Close it and the round picks up where it left off. Now try the other three answers — boot `?patrol=1` with nothing (*"Nothing comes out of your wallet that this floor has ever heard of"*), with only the chit (*"That's for the cage. This isn't the cage."*), or take a pass to a different rock (it names the other SITE, the #679 ladder one building along). **All three end the same way: he walks you back to the car, nothing is taken, nobody is called, and a line goes into a book.** There is no chase in this feature and there is nowhere for one to start.<br><br>**BROKEN LOOKS LIKE:** a challenge that ends in anything but a walk back to the lift; a refusal that does not say WHY; the escort line pulsing to the HUD *behind* the card's own backdrop (#736's law — the sentence you act on rides the card); or a pass that works on a rock it was not issued for. *(Also a button in the front door's **⚙ DEV START SITES** list — 🪪 “…and the same floor with the site's own pass in your wallet”.)* |
| **`?watch=N`** | **PIN WHICH SHIFT THE HALL IS ON (#751). The B1 cantina hall holds eighty and how many of its twenty tables are taken varies BY WATCH — a heaving day watch, a small-hours watch of a dozen souls — and **nothing in the game announces which one you walked into**: that is the design, and it is exactly the kind of design a tester cannot see without waiting four sim-hours between looks. A watch is four sim-hours (`PatronRota.WatchSeconds`) and six of them are a day, so `?watch=2` is the middle of the day and `?watch=5` is the small hours; compare the two and the whole feature is on the screen. Owner, twice over: *"testing is a feature."* It pins the watch INDEX and nothing else — who is in the room and where they sat are still the rota's own answer for that shift (#709), so what you walk into is the room a captain would get, never a rigged one. Pair with `&tablescene=1`.** |

### Walking the found halls — `?found=1` (#677)

Two gates and a band of solid rock stand between a landing and a gallery. This is the whole walk-through:

```
/map?found=1&land=1                 the lift head, wallet full — ride down and read the building
/map?found=1&land=1&floor=9         B9, the band nobody listed (a clinic under a laboratory)
/map?found=1&land=1&floor=12        its deepest floor — the thing on the pallet is in room one
/map?found=1&land=1&floor=17        the FIRST gallery: dark, sealed, and nothing says why
/map?found=1&land=1&floor=20        the deepest gallery — the chambers are visibly bigger here
/map?found=1&land=1&floor=14        a floor inside the band of NOTHING: you land in the galleries
```

**What you should see.** The lift panel from B12 shows no button below it until you look in your wallet — the
building does not admit the shaft exists, and neither does its panel. Riding it says the seam line once
(*"The pour stops. Not at a wall — at a line…"*) and then the arrival line once (*"The car has no button for
this floor…"*). On the floor: **no light but the suit's cone**, the air gauge parked on **PRESSURISED · TANK
STOPPED**, walls in a flat grey that is neither the department livery above nor any moon's stone, **no doors**
(the wall simply stops at each chamber mouth), **no plates**, no `⟶ SECTOR n · 2.4 km`, no refuge, no canteen,
no cubicle, and no book on any shelf. Nearly every chamber says *"Nothing. Not stripped — nothing was ever
here."* About one in nine holds a record: a rubbing goes in the pocket, the wall stays, and the casebook keeps
one line about a tape measure failing. Ride down through B18–B20 and count the chambers — there are half as
many and each is half again as large, which is the only sentence the plan is allowed to say.

Pair with `&dark=0` — there is no such switch, and there does not need to be: these floors are dark because
they declare it, and `?dark=1` is for the ordinary ones.

### Reading the whole shelf — `?book=N` (#701)

One would-be-empty room in six holds a book, which is rare on purpose and unplayable to test. `?book=N` takes
the gate off:

```
/map?secretlab=deep&land=1&floor=2&book=1     the oldest sea story
/map?secretlab=deep&land=1&floor=2&book=4     the catalog slip (the one that is not a book)
/map?secretlab=deep&land=1&floor=2&book=8     the mechanics text, 27th edition
/map?secretlab=deep&land=1&floor=2&book=10    the fat paperback
/map?secretlab=deep&land=1&floor=2&book=on    whatever each room's own seed picked
```

Walk the floor pressing `[E]` on the search consoles. **What you should see:** rooms that hold a file, a
crate, a card or the pallet behave exactly as they always did — the cheat changes nothing about them. Rooms
that would have said *"Stripped to the fittings"* say the shelf line instead and raise a caption-only card.
**Nothing goes into the satchel, no credits are paid, and the room is NOT struck off** — press `[E]` again and
the card opens again, which is the point: looking is free. The **casebook** (`[I]` → 📓 NOTES) gains one line
per book the first time you read it and never a second time, on this thread, ever.

The numbers are `OddBooks.Catalog` order and are part of the contract — 1 is the oldest sea story, 10 is the
fat paperback. `?book=99` and `?book=nonsense` are ignored rather than clamped, so a typo leaves you playing
the shipped roll instead of quietly testing entry 10.

### Dying on purpose — `?death=<cause>` (#621)

The death card is the one screen every player is guaranteed to see, and until now none of it could be
reached on demand. The routes were `?floor=2&air=10` (walk until you suffocate), `?reevers=8` (survive
long enough to be overdrawn) and `?collectors=20` (lose the Bolivia) — three causes out of six, one
place out of four, and nothing that reaches an impact at all.

```
/map?death=impact                                  the ship into a world at speed
/map?death=collector                               CAUGHT — the demand card, then SUBMIT / BRIBE / RESIST
/map?death=collector&dock=selene-gate              …the same catch, from a berth with muscle in reach (#777)
/map?death=suffocated&dock=the-tilt&land=1         the tank runs dry on the regolith
/map?death=reevers&dock=the-tilt&land=1            the Old Ones take you on the ground
/map?death=suffocated&wreck=1&land=1               the tank runs dry inside a dead hull
/map?death=reevers&wreck=1&land=1                  something has you against a bulkhead
/map?death=suffocated&secretlab=1&land=1&floor=2   150 m under a moon, in a poured corridor
/map?death=scuttled&wreck=1&land=1                 the overload you set yourself ran out (#525)
```

The cause is a `DeathCause` name, lowercased: `collector`, `impact`, `reevers`, `joined`, `void`,
`suffocated`, `scuttled`. It stages the **genuine trigger** — `TriggerImpact`, a real collector catch, or
`TriggerSurfaceOverdrawDeath` — never a mocked card, so what you see is what a player sees.

**There is deliberately no `?place=`.** *Where* you died is not an opinion the URL gets to hold: the
excursion's own floor and body id decide it, which is the classifier #609 was filed about. A cheat able to
override it would be a second source of truth for the exact fact that has now cost three death cards. You
choose the place by booting into it — nothing landed is `OwnShip`, `&land=1` is the landing party,
`&wreck=1&land=1` is a derelict, and `&floor=N` under `&secretlab=1` is the Hive.

A death on her deck with no `?dock=` / `?start=` of its own defaults to a berth (The Tilt), because
otherwise the boot ends at the front door and the death card opens on top of the menu. Anything you pass
still wins.

Two things are read off the LIVE state rather than invented, for the same reason: whether the *nerve* ran
out (so a full-nerve captain honestly gets the mauled caption, a shattered one the overdraw caption), and
whether the cause is legal where you are standing — ask for a `collector` death inside a wreck and the law
substitutes one that can happen there, and the game says so in a DEV pulse rather than inventing a
character. `?death=void` has no lane yet and says so.

#### …and what to look at while you are there — the HOSTED hail (#777)

`?death=collector` is also the only way to reach `StoryBeats.Beat.CollectorHail` on demand, and since #777
that beat is **hosted**: the demand panel it opens *is* the beat's canvas, so the seam keeps the books and
raises nothing. The beat is fresh on any boot (the seen-set starts empty), so one URL shows the whole shape.
The dev-start button is **⛓ CAUGHT — the hail, hosted by its own card**.

```
/map?death=collector&dock=selene-gate
```

Check four things on the panel that opens:

1. the **grapples painting** across the top (`art/collector-hail.jpg`) — that has been there since #528;
2. **the beat's own sentence under it**, naming the collector that has you. That is new, and it is the point:
   a hosted beat has no card of its own to carry its prose, so the host owes it the words (#761, #736);
3. **exactly one card.** Nothing opens over the demand panel — that second modal, showing the very same
   painting, is what #663 refused to ship and what the hosted presentation exists to avoid;
4. **the log behind it** (`⛓ GRAPPLES — Grapples come across the frame…`) filed **once**. Close the panel and
   the words are still there, which is the whole reason the seam is involved at all.

The same beat fires on the on-foot writ too (#583) — get caught out on a moon and the panel is identical.

### The salvage run — `?wreck=1` / `?wreck=<cause>` (#488)

A **derelict** is a boardable site that is neither a world nor a berth: a ship that died under way and has
been coasting since. `?wreck=1&land=1` is the one-URL path aboard — she wins the landing toss over any
moon in reach, and the away team spawns just inside her airlock, standing in a doorway.

`?wreck=<cause>` boards a wreck that died **that** way on purpose, instead of re-rolling ids until the
interesting one turns up:

```
/map?wreck=infested&land=1        ← something is still aboard; GATE-1 is live in her airlock
/map?wreck=insurancejob&land=1    ← a staged loss, dressed as an ordinary drive failure
/map?wreck=mutiny&land=1          ← the barricade weave down the spine
/map?wreck=hullbreach&land=1      ← the two holes it made going through her
```

All ten `WreckCause` names parse, lowercased — the four above plus `drivefailure`, `reactorcascade`,
`lifesupportfailure`, `navigationalerror`, `piracy` and `ventedbyoneoftheirown`. There is a guard on it
now: `TenHullsTenStoriesTests` walks every cause through `SeededWithCause`, so a cause the seeding cannot
reach fails CI instead of returning an unhelpful default hull.

**The loop:** board → walk the spine → read the three stations (the damage, the bridge log, the cargo
manifest) → the cargo console → file the report naming a cause, or strip her and say nothing.

| What to check | What you should see |
|---|---|
| **She is walkable** | Every compartment enterable, bow *and* stern reachable. `WreckLayoutTests` audits this with A* on every CI run — if it is broken here it should already be red there. |
| **The doorways are DOORS** | Drawn as auto-doors that slide as you approach. An unmarked gap in a dark box is what left the owner stuck twice. |
| **The damage is in the hull** | A cascade peels the transom; a breach is two holes; a mutiny is two barricades you weave through. Intact causes draw nothing — that *is* the finding. |
| **Stations mark ✔** | Reading one checks it off and rebuilds the deck. Two are needed before the report can be filed. |
| **Some wrecks lie** | On a staged loss or an infested hull, the choice card offers a plausible WRONG cause until you have read both the log and the manifest. |
| **Both roads quote real numbers** | The file button names the fee; the strip button names the whole value. Stripping must always pay more *today*. |
| **Stripped cargo is HOT** | It rides home stamped `salvage` through the same ledger a plundered pod uses. |
| **The honest road earns a NAME** (#652) | `/map?wreck=drivefailure&land=1` → read two stations → cargo console → **FILE THE REPORT**, naming `DriveFailure`. The 📋 FILED card now names who countersigned it (*"…countersigned the finding and put their own name under yours"* — one of six assessors, seeded from the hull, so the same wreck always hands you the same person), the same sentence lands in the log, and that assessor is on the **contact ledger** with goodwill afterwards — vaulted like every other relationship. Read her **wrong** and there is no name at all: a bad report is worse than no report. |
| **No regolith-isms** | No motion tracker, no dig/sentry keybar, no Reever tide clawing out of her deck plates. She is a ship, not a moon. |

> **The infested hull is the one to playtest by hand.** Four Old Ones are already aboard, deep aft and
> aware, and the only way out is the airlock behind you — so the walk back *is* the encounter. The
> shuttle's own gun (`GATE-1`, never bought, never dry) sits on the spine just inboard of the airlock
> covering that corridor. Check that it fires, and that the retreat is actually survivable.

### The atmosphere board (#488)

Aft in ENGINEERING — the bridge panel is dead and says so. Full behaviour in
[features/atmosphere.md](features/atmosphere.md). `?wreck=ventedbyoneoftheirown&land=1` boards the hull
that arrives already blown.

| What to check | What you should see |
|---|---|
| **The mimic is the ship** | Compartments in their real places, drawn from `WreckLayout`. Names inside the rooms; long ones wrap and shrink rather than overrunning their neighbours (`LIFEBOAT CRADLES`, `FORWARD LOCKER`). |
| **Venting starts a clock** | `VACUUM 00:12` and counting — on the board *and* on the HUD out in the corridor. The pack in that room dies when the clock says so, not when you pull. |
| **The counter never says how long it needs** | Only how long it has been open. If a build ever shows the requirement, the decision is gone. |
| **Blown rooms are walls** | Walk into one and you get the gauge card: needle hard over, *not locked — LOADED*. |
| **Two roads through a loaded door** | Crack the valve (free, empties the corridor **and every room standing open to it**) or refill from the board (one charge, keeps the air). |
| **A dogged hatch survives the valve** | Seal a room by hand at its door, crack a valve elsewhere, and that room keeps its air. This is the counterplay — the infestation never closes a door. |
| **The corridor cannot be refilled** | The refusal explains why: a compartment is a room, the spine is the ship. |
| **The pump banks early** | At ~18 s the charge lands and the button relabels to *"take the air and go"*. The remaining tail only buys the kill. |
| **The shuttle is never vented** | Leaving reports the lock cycling — it matches the hull first. Crack every valve aboard and the boat still has her air. |
| **Nothing crosses the lock** | Reevers stop at `ShuttleLockX` (x = 21). They can reach the door; they cannot open it. |
| **You are never trapped or stranded** | The board refuses to vent the room you are in; every loaded door offers the valve. Both are pinned in Core, but check them by hand once. |

> **The loop worth playtesting is the greedy one:** dog the hatch, start the pump, walk out and hold the
> lane with a sentry while the rough stage banks your charge — then decide whether to stay for the tail.
> Owner's own verdict on it: *"I love that pressure waiting in a hot spot with round counts dropping."*

### The archive node — `?archive=1`

> **The house rule this cheat exists for, written in `Map.Sim.cs` beside the others:** *"a scene nobody can
> reach on demand is a scene that ships broken."* The node is aboard about **one eligible wreck in three**,
> and the compartment it sits in is one room of one hull — which makes it, like the repo boat and the deep
> Hive floors, nearly impossible to playtest on purpose.

```
/map?archive=1&land=1     board the hull that is carrying one, straight from the URL
```

`?archive=1` implies `?wreck=ventedbyoneoftheirown`: the ship one of her own opened to space is the one
cause where Core guarantees a node, because **the node is why she died**. It is deliberately not a
"spawn a node anywhere" switch — a spar bolted into a drive failure would be a prop.

Walk aft down the spine and into the **DEEP HOLD** (top row, the second compartment from the stern). Full
design in [features/the-archive-node.md](features/the-archive-node.md).

| What to check | What you should see |
|---|---|
| **The fiction arrives before the mechanic** | The first time you cross into the field, one line about the **temperature** and nothing else — no warning, no noun for what is doing it, and the pip row has not moved yet. |
| **The dwell is the slowest beat in the game** | Walk straight through the hold and it costs you nothing worth counting. STAND there and a pip goes every `NervePips.ArchiveBeatSeconds`, each one saying *"you have stood too long beside the thing in the hold"*. |
| **The gauge agrees with the ledger** | This is the one to try to break. A wreck's interior scores as *safe*, so the airlock's give-back beat runs in there — if the two ever cancel, the ledger prints a loss while the gauge sits still. Watch the pip row actually fall. |
| **Nothing announces anything** | No prompt, no dialog, no "are you sure?". The compartment is a field you chose to stand in. |
| **Arm's length forces the throw** | Walking up to the column is enough — you do not have to press anything to be looked at. `[E]` on it does the same. Visible arithmetic on the card. |
| **The visions are art, not captions** | Five painted canvases, no caption telling you what the handlers are. The card never explains, never confirms, and never mentions the Old Ones. |
| **The collar is bought, never given** | Whose pattern is in the spar only appears on the ≤ 8 bands — the ones you were *looked at* for. A clean throw never reads it. |
| **The label is the confirmation dialog** | `⏻ PURGE NODE — RESIDENT PATTERN NOT RECOVERABLE` is stencilled on the deck. Pressing `[E]` pulls it. That is all that happens, and it is meant to be. |
| **You can pull it without paying** | The handle stands 3.5 du from the column, deliberately outside the confront radius: you may reach it, pull it, and never find out what you did. |
| **The record of a purge is the silence** | The line at the handle is the same sentence whatever was inside. If it ever names the resident, the whole §5 shape is gone. |

> **Not in this lane, and deliberately so:** the resurrection card's `NO PATTERN ON FILE` line (the feature
> doc's build step 5). It needs the owner to rule on whether purging your own pattern actually *ends* the
> policy — and a card that says the policy is closed while the rebirth still fires is the sentence-versus-sim
> bug this project has paid for three times. Filed rather than guessed.

### The quantized nerve — reading the pips and the ledger (#480)

Owner's ruling, 2026-07-28: *"the sanity events should be quantized. Why and when it drops should be made
clear to the player. Not this float stuff we have now."* The nerve is no longer a sliding bar and no longer
moves anonymously. When testing it, **read the ledger, not the gauge** — the gauge tells you how bad it is,
the ledger tells you why, and the ledger is the thing under test.

**The invariants worth breaking:**

| Rule | What you should see |
|---|---|
| Every change is a **whole pip** | The gauge only ever steps by whole segments. A half-lit pip is a bug. |
| Nothing moves it **anonymously** | Every step is accompanied by a named line in the flash and the ledger. A pip that moves with no line is a bug. |
| **Distance gates the beat** | An Old One outside the dread range costs *nothing at all* — not a slow drain, nothing. Walk to the far rim of the field and watch the gauge sit still with a pack visible on the tracker. |
| Pressure **does not bank between encounters** | Build most of a beat, walk away, come back — the part-beat must not have been saved up to fire a free pip. |
| A hand costs **one pip, once** | Let a pack maul you at full health: exactly ONE `it laid hands on you −1`, no matter how many strikes land. The *blows* keep charging (the condition pips drop); the nerve does not. |
| …**unless you are nearly gone** | Below two blows left, every hand costs again and reads `it has you and you are nearly gone`. |
| Sightings charge **once per spell** | One `something crests the tracker −1` per watch. If it repeats as the pack weaves in and out of range, that is the #482 repeat-tax regressing. |
| Recovery is **legible too** | Up the tube: `the airlock closes behind you +1`, one beat at a time, in green. Slower than the sharpest loss on purpose. |

Fast rig: `/map?dock=the-tilt&site=0&land=1&reevers=4` — boots you onto Miranda's canon ground with a roused
pack inbound. Stand still and watch the ledger fill; walk into the pack to test the touch rules; run up the
tube to test recovery. The ledger also appears on the death card under **WHAT BROKE YOU**.

**Start the gauge where the beat is — `?nerve=N` (#428).** A full gauge is ten pips and every loss is one
pip, so *watching* a sanity beat used to mean surviving five to ten minutes of being hunted first. `?nerve=N`
seeds the gauge at boot at **N of `NervePips.MaxPips` whole pips** — the same segments the corner gauge
draws, not points out of a hundred — clamped to the gauge at both ends (`?nerve=10` is the shipped default,
`?nerve=99` is the same thing). It moves the needle and nothing else: no beat is skipped, no cause is
faked, and the ledger still has to earn every line it prints.

```
/map?nerve=1&dock=the-tilt&site=0&land=1&reevers=1   one pip left, one hand inbound — the overdraw break
/map?nerve=3&dock=the-space-bar&body=phobos&site=0&land=1   the monolith's three-pip lump, onto a frayed captain
/map?nerve=2&archive=1&land=1                        the archive node's dwell with almost nothing to spend
/map?nerve=0&dock=the-tilt&site=0&land=1             the SHOT band and its readout, from a standing start
```

At **`?nerve=1` the captain is not yet overdrawn** (`CaptainSuccession.EmptyThreshold` sits under one pip),
which is the point: what you watch is the real two-step break — a hand takes the last pip, the *next* one
breaks them — rather than a death the cheat invented. `?nerve=0` is the already-empty state the next
qualifying hit ends. Both are pinned by `TheNerveSeedIsMeasuredInPipsTests`, because a seed read in the
wrong unit parses fine and silently changes which of the two you are looking at.

> **Blazor cache gotcha (cost real time on 2026-07-28):** the published Pages build is served from a
> service-worker cache. A plain reload can re-serve the OLD wasm and you will "verify" a fix that isn't
> there. Clear it in the tab console — `(await caches.keys()).forEach(k => caches.delete(k))` — **and** add a
> cache-buster query param before believing any playtest of a just-merged build. Note also that the
> `build <sha>` line on the home page is rendered by Blazor, so `curl | grep` can never see it; check deploy
> state with `gh run list` instead.

### The rebirth wake — what a new captain is owed (#477)

Die (nerve, five blows, or a hull loss) and the brain-backup issues a new captain. Check the wake state on the
Nav desk, because two things here were wrong and are easy to regress:

- **Mass pulses must read a FULL base tank** (500 / 500). The old "mercy floor" handed over exactly the
  autopilot's own reserve (90 p), which the autopilot then refuses to spend — a stranded captain with no fare,
  no purse and no cargo. Owner's ruling: *"give the same amount of fuel on rebirth as when starting a new game."*
- **The nerve must read STEADY and the ledger must be EMPTY.** Nothing reset the nerve on rebirth before, so a
  fresh captain inherited the dead one's shattered gauge — and since nerve is the commonest death, the
  replacement woke at the floor.

Still open here: **#478** — the wake also flashes `ROCKS AHEAD! — impact with The Tilt` while reading
`clamped on` at `0.0 km/s rel`. A normally-docked ship never does this (verified against a fresh dock and a
dock held 9 h at warp), so it is specific to the wake state.

### Multiple landing sites — `?site=N` (#320)

A body is a world, not a level: every landable body now offers a **seeded set of 2–4 landing sites**, each
named in the house voice with a one-line character tag (*"The Wild Plain — nobody out here will hear you"*).
The set is deterministic per body id, so a revisit re-offers the identical board. **Site 0 is always the Wild
Plain on the body's canon ground** (Phobos's MONOLITH on the Stickney rim, Miranda's false-slab maze, Luna's
mass-driver ruins, the seeded signature — unchanged); **sites 1+ re-seed a visibly different wing/feature layout** on the same body. The picked site
persists for the visit and is named in the surface header (**🛬 SET DOWN AT: …**).

`?site=N` pre-selects site N in the boarding panel (clamped to the body's real set), so you can board straight
onto a chosen ground. The verify loop for "does the choice change the surface":

1. `/map?dock=<berth>` (or any docked start) with a landable moon in shuttle range (e.g. Miranda).
2. Walk to the 🛸 shuttle-bay airlock, **Board for <body>** → the boarding panel.
3. The panel lists the body's landing sites under **🛬 Set down at**; pick one (or launch with `?site=1`).
4. **Board** → walk down. The surface header reads **SET DOWN AT: <SITE>**, and the deep-field walls/features
   differ between, say, site 0 and site 1 on the same body.
5. Lift off, board again → the same seeded set is offered; re-picking the same site yields the same ground.

### PROJEKTI KAAMOS (arc 1) — `?kaamos=` (#411)

Arc 1 is the sealed ice-moon berth nobody files for. Six fragments — five intel shards and the earned
capstone — each handed over by a different system. Progress shows in the Captain's ledger as
**"❄ PROJEKTI KAAMOS — N of 5 shards assembled"**, with every assembled shard re-readable beneath it.

**The one-URL shortcuts (these GRANT the shards):**

- **`?kaamos=N`** assembles the first N fragments in canonical order; **`?kaamos=all`** assembles every one.
  At 4 intel the ledger flips to *"Enough intel to earn the berth-code"*; with the capstone too, the
  one-time **"❄❄ THE BERTH-CODE RESOLVES"** notice fires and the ledger settles into the held-berth line.
- `/map?kaamos=3` is the fastest look at the mid-arc card; `/map?kaamos=all` is the end state.

**The front door — `?kaamos=bounce` (#635):**

The arc used to be invisible until a captain happened to read the whole of one dedication plate among
seven, and its ledger card appeared only *after* a shard was already held. It now has an inciting hook, and
it is a piece of paperwork, because that is what this arc is about:

| URL | what you walk |
| --- | --- |
| `/map?ashore=1&kaamos=bounce` | Press `[E]` at any bar patron. A freight agent offers **350 cr** to put your own hull's number on a consignment that has come back four times. Take it, and the board answers *RETURNED — CONSIGNEE CANNOT BE RAISED — BERTH HELD, AWAITING CYCLER WINDOW.* You keep the receipt; the ❄ card is now in the Captain's ledger with **nothing assembled**. |

Things to check while you are there: the fee is printed on the offer card *before* you press the button;
the docket says HELD and never says who is holding it; the ledger headline is **not** "0 of 5 shards"; and
the agent is gone from the offer rota the moment you hold any shard at all. Unforced the agent is in the
room roughly **one bar-watch in three**, seeded per (bar, day) — walk into the same bar on consecutive
watches to see it come and go without re-rolling under you.

**The route — `?kaamos=hq` (#411):**

The berth-code puts your hull ON THE BOARD, and a **KAAMOS supply run** comes back onto the listing with it:
CONSUMABLES, WINTERING CREW, 40 SOULS, the cold pod's own manifest slug word for word. Take it from any bar
patron like any other haul. Then the **cycler window** — a real grid over sim time, 2 days open every 40
(`CyclerWindow`) — and when it comes round, right-click **Enceladus** on the map and the body menu carries
**❄ Ride the cycler window in**. 38 days of being carried, no burn, no brake at the far end.

| URL | what you walk |
| --- | --- |
| `/map?kaamos=hq` | Alongside the ice moon with the run in hand — open the shuttle bay and go down. |
| `/map?kaamos=hq&land=1` | …and the shuttle takes it from there, boots on the ice in one URL. Walk to the lift head and ride down: **the head office** is under it (#411). |
| `/map?kaamos=hq&land=1&floor=23` | Straight to **B23 · THE WINTERING HALL** — the card, and the biggest nerve throw in the game (40 of 100). |
| `/map?kaamos=hq&land=1&floor=24` | **B24 · THE BERTH OFFICE** — the console that has never stopped filing. |
| `/map?kaamos=hq&land=1&floor=12&nerve=10` | **B12 · THE STANDING ORDER** — search the first room off the nearest rib for the one sheet worth carrying out. |
| `/map?kaamos=all&ashore=1` | The other end: the berth-code resolves at the bar seam, the ❄❄ notice fires, 📰 **THE STORY BREAKS** raises the arc-news card, and a housekeeping line lands on the wire. |
| `/map?kaamos=hq&arrivalphase=2&land=1&floor=23` | **#742 · the arrival phase that used to drift into the moon.** `&arrivalphase=N` (0–23) winds the clock to phase N of Enceladus's 32.9 h orbit before the arc lets her go, so the one-in-24 placement is bootable on demand instead of waited for. |

**#742 — the arrival phase nobody chose.** The window opens every 40 days and stands open for two, so *which*
phase of the moon's orbit the ride lets you go on is free. It used to decide whether you kept your ship. The
park was laid along the **Sun-outward** direction and handed the moon's velocity, which in Saturn's frame is
not a park at all but a different ellipse — semi-major axis wandering 2.193e8 … 2.592e8 m against the moon's
2.38037e8, period 104,768 … 134,641 s against the moon's 118,387 (up to **13.7 % off**) — so periapsis and
apoapsis *bracketed the rail the moon runs on*, and the period mismatch then walked the hull round that rail
until the two crossings met. **Phase 2/24 (epoch 9,866 s) struck the ice at +9.54 h**; 3/24 and 14/24 passed
at 3.157e5 and 5.816e5 m, one and two surface radii. The park is now laid along the moon's **own track**
(`BerthState.CoOrbital`) — the moon's own conic, phase-shifted — so no phase can be the unlucky one.

| what to do | what you should see |
| --- | --- |
| `/map?kaamos=hq&arrivalphase=2`, then ⏩ warp a sim day | The standoff holds near 1e7 m and the range readout stays there. Pre-fix, the ⚠ *orbit degrading — periapsis under the surface* banner came up and the hull was inside Enceladus by +9.54 h. |
| `/map?kaamos=hq&arrivalphase=2&land=1&floor=23` | The wintering-hall beat, ridden all the way down and back, with a ship still up there to come back to. This exact URL used to be the death. |
| Sweep `&arrivalphase=` 0 … 23 | Every one behaves the same. That sameness *is* the fix — the phase no longer decides anything. Phases 3 and 14 were the other two that grazed. |

Things to check: right-clicking Enceladus with no berth-code shows **no** cycler row at all (the fiction may
not arrive before the arc does); with the code but no run it is visible-but-disabled and says why; with the
run but a shut window it quotes the wait in days, and that quoted wait is the real one. Parking in the ice
moon's orbit settles the run through the ordinary moon-haven cargo path — no second completion code exists.

### The head office under the ice (#411, the owner's 2026-08-03 ruling)

`/map?kaamos=hq&land=1` and then down the lift. What you are checking is the **rank difference**, which
is said entirely in the branch-office vocabulary a Hive already taught you:

| the grammar | a Hive (a branch office) | the head office |
| --- | --- | --- |
| depth on the panel | 3–20 floors, and one site in four hides a band from its own staff | **24, and it lists every one** |
| the lift | a card opens exactly one shaft band; the button below reads `↓ THE OTHER SHAFT — SEALED` | **the car answers**, on every floor, and never asks for anything |
| department plates | eight names on a cycle — B1 and B9 are both ADMINISTRATION | **24 plates, none repeated**, RECEPTION down to THE BERTH OFFICE |
| sealed corridor mouths | `⟶ SECTOR 7 · 2.4 km` | `⟶ WING 1 · 24.6 km` |
| livery | one hue per department, on unmaintained concrete | one hue per **wing**, stepped darker per floor down it, and **kept** |
| the first descent card | the Hive's own shaft card | its own painted establishing shot |

And the thing to check that is not on any screen: **fly to Enceladus without the berth code and there
must be nothing there.** Featureless ice and a good view — not a locked door, not a refusal, not a hint.
Try `/map?start=enceladus&land=1` with no `?kaamos=` at all; the ground must have no lift head on it.

**The two seats (these let a rare beat be PLAYED, not granted):**

| URL | what you walk |
| --- | --- |
| `/map?dock=the-tilt&site=0&land=1&kaamos=pod` | Land, take out the **metal detector**, probe any square: the cold supply pod is under this ground. This is the only fragment with no direct grant-free path — one seeded square in seventeen, on seven outer moons. |
| `/map?dock=ringside-exchange&kaamos=holder` | Walk to the counter and the barkeep card carries **🌑 Ask about KAAMOS** — the berth-holder is in, this watch, at this bar. Unforced they drink at a given bar roughly one watch in four. |

**In-play delivery to verify by hand (the canonical order):**

1. **`listed-berth`** — dock at **Ringside Exchange**, walk ashore to the **⚜ DEDICATION PLAQUE** and press
   **[E]**. Ringside is the one plate in the system that names KAAMOS. (The Deep's plate echoes the sealed
   berth *unnamed* and correctly hands over nothing.)
2. **`cold-pod`** — `?kaamos=pod` above, or sweep an outer icy moon (europa, ganymede, callisto, titan,
   miranda, triton) with the detector until a square rings off metal.
3. **`vantar-log`** — `/map?secretlab=1&land=1`, force the hidden door, and read the lab console whose log
   is the **undated** one ("a moon off the charts… the manifest sealed"). It never names the project; the
   connection to the plate is yours to make.
4. **`holders-tell`** — the bar seam, `?kaamos=holder` above.
5. **`bought-coordinate`** — the same bar seam once the thread has begun. The button prices itself
   (**🌑 Buy the KAAMOS coordinate · 1,200 cr**) because clicking it spends the money.
6. **`berth-code`** — the same seam once 4 intel are in hand; the button reads **❄ Put the KAAMOS pieces
   together**, and the resolution names *the shards you actually hold*, never a fixed four.

There is also a **third hand** that leaks arc-1 shards: **Static Marsh**, the station oracle
(`OracleRant`), can speak `vantar-log`, `holders-tell` or `cold-pod` as a true line at any bar — stand her
a drink to widen the channel.

**What is deliberately NOT built YET:** the head office under the ice.
`KaamosLore.RevealSanityShockHook` (40.0) is still consumed by nothing. Both are specified, sliced and
under construction — see [`features/kaamos-head-office.md`](features/kaamos-head-office.md), issue #411,
and the owner's 2026-08-03 ruling that the destination is the HEAD OFFICE of the organization.

### NEBULA MUTUAL (arc 2) and THE CONVERGENCE — `?nebula=` / `?converge=1` (#422)

Arc 2 is the truth behind your resurrections; you gather its fragments by **dying and coming back**, by
reading the port posters twice, at a bar from a roving **Nebula adjuster**, off a **collector's writ**, and
from the **clinic's** books. Progress shows in the Captain's ledger as **"▓ NEBULA MUTUAL — N of 5 clauses"**,
the assembled shard texts readable beneath it (mirrors the KAAMOS readout).

**The one-URL shortcuts (these GRANT the shards):**

- **`?nebula=N`** assembles the first N fragments in canonical order; **`?nebula=all`** assembles every one
  (5 intel shards + the capstone contract). At **4 intel** the ledger flips to *"Enough of the small print to
  earn the contract"* and the bar seam starts offering the capstone.
- The one-time **"▓▓ THE POLICY'S TRUE TERMS RESOLVE"** notice needs the **capstone contract as well as the
  intel** (`NebulaLore.KnowsTheTruth` is *both*), and the capstone is fragment **6** in canonical order — so
  `?nebula=4` and even `?nebula=5` fire nothing, and **`?nebula=all` is the only value that fires it.** (This
  section used to say "at N ≥ 4"; it was wrong, and `HoldingEveryClauseButNotTheContractIsNotKnowingTheTruth`
  now pins it.)
- `/map?nebula=3` is the fastest look at the mid-arc card; `/map?nebula=all` is the end state.
- **`?converge=1`** is the marquee smoke test: it seeds exactly the joint threshold on **both** arcs
  (3 KAAMOS intel + 3 NEBULA intel) and fires **THE CONVERGENCE** — a full staged reveal card, above
  everything, stating that the sealed ice-moon berth and your brain-backup insurance are the same story.
  It fires **once per universe** (the seen-bit is persisted in the vault); reload and it does not replay.
  **Note the bar:** 3 NEBULA intel is *below* this arc's own capstone gate of 4, so the convergence card can
  and normally does arrive **before** `policy-terms` — see the open structural question on
  [#422](https://github.com/esoinila/SpaceSails/issues/422).

**The seat (this lets a rare beat be PLAYED, not granted):**

| URL | what you walk |
| --- | --- |
| `/map?dock=the-space-bar&nebula=adjuster` | Walk to the counter and the barkeep card carries **▓ Ask about NEBULA** — the Nebula Mutual adjuster is in, this watch, at this bar. Unforced they drink at a given bar roughly one watch in five. |
| `/map?nebula=all` | **Arc 2 reaches the wire (#663).** The truth resolves, and with it 📰 **THE STORY BREAKS** — the same beat arc 1 raises on the berth-listing edge. Check the **Comms ticker / Galley feed**: a flat filing-office note about Nebula Mutual re-lodging its standard terms, no objections entered, annexes available on request. It never says "you", never names a plot, and the card's caption is the point — *"one figure walks away from the screen instead of toward it, because the policy is not news to them."* |

**In-play delivery to verify by hand (the canonical order):**

1. **`rebirth-glitch`** — **die** (get caught by a collector, or fly into a body). On the resurrection card,
   a green monospace **glitch flash** ("…DO NOT REVIVE ORIGINAL") assembles it. The flash is seeded off the
   death, so it varies between rebirths; it shows on **every** rebirth, and the shard is gathered once.
2. **`fine-print`** — walk up to a **`📋 PIRATE INSURANCE`** poster (any port hall or bar) and press **[E]**.
   The first read only leaves you the tell (*"Your eye snags on the grey line under it… Come back and read it
   properly"*); **[E]** again to close, **[E]** a third time to re-open, and the second read assembles it.
3. **`adjuster-tell`** — the bar seam, `?nebula=adjuster` above. Static Marsh (the station oracle) can also
   speak this one as a true line.
4. **`collector-writ`** — get **grappled by a collector** in flight. The writ glimpse rides the DEMAND card,
   the once, before you choose SUBMIT / BRIBE / RESIST.
5. **`clinic-ledger`** — die a **second** time in the same universe. The bill's second page rides the wake
   card. (Its gate is this thread's own retired-captain count, so a New voyage starts naive again.)
6. **`policy-terms`** — the same bar seam once 4 intel are in hand; the button reads **▓ Put the NEBULA small
   print together**, because at that step nobody is being asked anything. Neither NEBULA step costs coin.

There is also a **third hand** that leaks arc-2 shards: **Static Marsh**, the station oracle (`OracleRant`),
can speak `rebirth-glitch`, `adjuster-tell` or `clinic-ledger` as a true line at any bar — stand her a drink
to widen the channel. Seat her on demand with **`?oracle=1`** (below).

**What is deliberately NOT built:** the sanity throws. `NebulaLore.TruthSanityShockHook` (30.0) and
`ArcConvergence.ConvergenceSanityShockHook` (64.0) are consumed by nothing, so the two biggest reveals in the
game cost the captain no nerve at all. See issue #422.

### Already in the bar — `?ashore=1` (#428)

Every bar beat this game has starts with the same walk: ship → airlock → tube → immigration hall → the
wide north door → the bar. It is a good walk. It is also, on **every single boot**, the thing standing
between a tester and the beat under test — and in an automated or backgrounded browser tab the page is
`document.hidden`, so rAF is throttled and WASD never lands. There, the walk is not slow; it is
**impossible**, and not one bar beat could be smoke-tested at all.

`?ashore=1` boots you docked (default **The Space Bar**; any `?dock=<id>` or `?start=<id>` you pass still
wins) with the captain **already standing one step inside the bar**, facing into the room, Deck up. It
seats nobody and grants nothing — it moves the captain, exactly as the walk would have.

```
/map?oracle=1&ashore=1                            the rant: one URL, one [E]
/map?ashore=1&nebula=adjuster                     arc 2's best beat, at the counter
/map?ashore=1&kaamos=holder&dock=ringside-exchange
/map?ashore=1&bond=1                              stand still; the forced scare opens the cognac beat
/map?ashore=1&simhours=9&dock=cinder-roost        the Magpie's third stop, at the back room
```

The position is **derived from the doorway the real walk crosses** (`HavenInterior.BarThreshold`, off the
hall's north door), never typed in — a boot coordinate is exactly the shape of this project's oldest bug
class. `TheAshoreBootStandsYouInTheBarTests` audits it against the shipping deck plan: it is standable, it
is in the bar and not the concourse, it is in the doorway's mouth (you can step back out), **nothing is
under `[E]` before you move**, every barkeep/patron console is walkable from it, and you can still walk
home to your own ship.

Pointed at a **pumps-only berth** — a dockable haven with no walkable complex, which clamps you on and
leaves you on the Nav map — it says so in a DEV pulse rather than teleporting the captain into a bar that
does not exist. The seven stations that *do* have one are the seven in `HavenInterior.Specs`.

### The station oracle — `?oracle=1` (#425 / #428)

**Solenne “Static” Marsh**, the ranting drunk in the corner: a lapsed Nebula Mutual pattern-auditor who
listened to the archived dead too long. Most of what she says is beautiful noise; a rare line *"sounds nuts
but is TRUE"* and can leak a real KAAMOS or NEBULA shard. She is a corner fixture only
`OracleRant.PresenceChance` (55 %) of watches, so until this cheat existed her whole scene was a coin flip
to open — and unlike `?kaamos=N` / `?nebula=N`, **nothing ever granted a rant**. `?oracle=1` boots you
docked at a bar (default **The Space Bar**; combine with `?dock=<id>` for another) and **seats her**,
whatever her rota says. It hands you the person, never the truth: the lines are the same seeded stream the
rota would have given.

1. `/map?oracle=1` → boot docked. A toast names where she is.
2. Walk into the bar and head **aft along the left wall** — her corner is `(−11, HallTopY+19)`, deliberately
   clear of every other console by more than `[E]`'s reach, so you cannot grab the wrong one.
3. **[E]** on **`◈ “STATIC” MARSH`** → her card: name, backstory, and the opening rant.
4. **🌀 Keep listening** turns the dial one line on. At zero drinks ~14 % of lines are true
   (`OracleRant.BaseTrueChance`), so expect to press it several times.
5. **🥃 Buy her a drink** (the price is on the button) → she may **wave the glass off** ("not thirsty for
   THAT vintage, it's ticking wrong") — the #347 offer-first path, and **nothing is debited on a refusal**.
   An accepted glass costs the bar's drink price, widens the channel (+7 pp true chance per drink, capped at
   45 %) and draws a fresh line at once.
6. **THE TELL:** when a line lands, the card may say *"…the room goes quiet. A faint chill."* It fires on
   most true lines **and on a minority of nonsense** — a hush that lies. The sifting is the point; do not
   read the glow as confirmation.
7. **A true line acts.** A `SecretLab` or `Collector` perception is filed to the durable **overheard book**
   as a *lead you still have to walk out*. A fragment line assembles a real shard into the arc and shows the
   canonical lore appended — check the **Captain's ledger** for the KAAMOS/NEBULA readout and for the
   `👂 Static Marsh` line under **🔭 Tips, intel & rumors**.

The reading is per-berth session state: a new dock wipes the dial and the drinks, so a drunk oracle at one
port does not stay prophetic at the next. `?oracle=1` is **not** a one-shot (unlike `?bond=1`) — she stays
seated for the whole run, because she is a conversation rather than a single beat.

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

### Hiding from a sweep — `?sweep=N` and the captain's remote (#538)

The **black-ops inspection team**: professionals who work a route through a hull while the captain is inside it.
They are the inverse of the pack — they see **20 du but only inside a 70° cone**, hear **34 du through walls and
regardless of where the lamp points**, and **challenge for 3 seconds before shooting**. So the counter-play
inverts too: you do not out-run them, you stand still somewhere the cone is not, and you do not make a sound.

They ride the **INSURANCE JOB** by default — her own fiction is that the valuable thing aboard is the *evidence*,
so what they came to remove is what you came to take. Or put them on any hull with `?sweep=N`.

```
/map?wreck=insurancejob&land=1     ← the authored scene: their boat clamps on while you are aboard
/map?wreck=mutiny&land=1&sweep=3   ← the same team on a hull that has no reason to be guarded
```

**What to check.** The clock strip carries one line for the worst state anybody aboard is in
(`SWEEPING → INVESTIGATING → HUNTING → CHALLENGING`). Their lamp cones are drawn at exactly the angle the rule
checks and stop at the first bulkhead. The motion fan hears them — if it ever reads "no movement" while three of
them walk the hull, that is the bug it read as before #545.

**The captain's remote (`📻` on the deck HUD, or `H`)** is what the scene is for. Three switches, one idea —
everything that makes you hard to find makes you slow:

| switch | what it costs |
|---|---|
| 🤖 **weapons tight** | no bot or tube gun fires, so nothing of yours makes a noise — and nothing of yours defends you |
| 🛸 **boat cold** | lamps, transponder and reactor down: 12 s to go quiet, **25 s to come back**, and her hatch is *shut* until she is warm |
| ✊ **quiet search** | knuckles instead of the sounder — slower, shorter, and not heard |

A cold boat is not a ride, an armoury or a way in: the belts are aboard her too. The warm-up is charged at the
lock, when you want to leave.

### Knocking on her walls — the hidden-void search (#537)

Some hulls hide a space that is not on the deck plan. **Her plating is honest and her manifest is not**: a lying
hull books one section of her shielding at a third of what every other section holds. Read the **cargo manifest**
(a console that was always there) and the discrepancy is the clue; on a clean hull it says the frame numbers match
all the way down the page, which it has to, or a document that only speaks up when there is something to find is
a pointer rather than a clue.

Then **`K` to knock**, standing still. Two gears, chosen on the remote:

| gear | seconds | reach | heard |
|---|---|---|---|
| 📡 sounder | 5 | 4 du | 26 du — as loud as running a pump |
| ✊ knuckles | 12 | 2 du | 13 du — as loud as dogging a hatch by hand |

Moving abandons the reading and does **not** refund the noise. Three answers: `SOLID`, `ODD` (near, not here), and
`HOLLOW` — which puts a **FALSE PLATE** on the deck to force.

**What to check.** The clock strip shows the knock and, once the manifest is read, the band to search. About one
hull in five hides something (Lab 44 probe F prints which of the ten seeded causes lie, and where). A void sits
either in the **shielding band** outboard of a room or inside a **bulkhead run** between two rooms — and what is
in it decides which: a rack of keys fits a bulkhead, a folded gun mount or a cold locker needs the band.

### The mountain lab — doors that lock, a board, and an alarm to hack (#409)

`?secretlab=1` still puts a lab in reach with its hidden door revealed. Since #552–#555 it is a **place** rather
than a vault: **THE ANTECHAMBER → THE CLEAN ROOM → THE HEART**, each narrower than the last, with a door between
each.

**The loop.** Force the hidden door → the alarm starts counting (75 s) and the garrison stands up → walk in past
doors you can shut, and with Vantar's card **key** → find the **door board** in the clean room, which throws any
door in the mountain from one wall → beat the **alarm panel** and the muscle never wakes, or lose and every door
keys at once with the card two rooms deeper than you are.

**What to check.**

- A **shut** door is a wall to the boot *and* to the eye, and the deck rebuilds when one moves.
- The pack can **force** a shut door (25 s, and it takes two of them) but never a locked one.
- The alarm panel **shows the die**: the target, the named modifier stack, and the number. A miss costs 25 s of
  the countdown, so there are only a few tries in it.
- **Beating the panel really beats it** — a disarmed alarm leaves the garrison asleep, which is the whole reason
  silent running is a play rather than a tax.
- The countdown sits at the TOP of the clock strip, and its second column says which side of a lockdown you are
  on: *"you hold the card"* / *"the card is deeper in"*.
- Two chambers are hidden **behind the lab's own walls** — knock for them exactly as you would on a hull.

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

**The afterlife** (story pass 2026-08-02): a bond that MAKES something — the cognac, or a whole new contact
— is now also filed to the durable overheard book as *"🥂 How you met …"*, so it groups under that person in
the Captain's ledger under **🔭 Tips, intel & rumors**. Open the ledger after the toast and the provenance of
the friendship is on the record; before, the hero beat lived only in a toast that faded, and nothing anywhere
said how the two of you came to know each other. A `Comment` or a `Deepen` stays passing, by design — a
shared word is a grace, not a relationship.

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

## The surface tour — every landing site, one URL each (#585)

Owner, 2026-08-01: *"let's go over all the sites we have not yet tested with the url-arguments."*

Until now that was **impossible for most of them**. `?land=1` takes the first landable body in shuttle reach,
so from The Tilt every URL in the world reached Miranda and nowhere else — two thirds of the grounds we had
just rebuilt had no way to be opened and looked at. `?body=<id>` fixes that: it wins the toss outright,
provided that body is genuinely on the shuttle board from your berth (the cheat may never reach somewhere the
player could not). Pick the wrong berth and the game tells you what *is* in reach.

The berth must be in the same system as the moon:

| Berth (`?dock=`) | System | Landable moons |
| --- | --- | --- |
| `cinder-roost` | Venus | the-clinker |
| `selene-gate` (or `satellite-factory`) | Earth | luna |
| `the-space-bar` | Mars | phobos |
| `red-eye` | Jupiter | europa, ganymede, callisto |
| `ringside-exchange` | Saturn | titan, enceladus |
| `the-tilt` | Uranus | miranda |
| `the-deep` | Neptune | triton |

**All 27 sites.** Each drops you on the open regolith with two sentries in the sling:

```
Miranda — the canon ground (3)
  /map?dock=the-tilt&body=miranda&site=0&land=1        The Wild Plain
  /map?dock=the-tilt&body=miranda&site=1&land=1        The Shadowed Rille
  /map?dock=the-tilt&body=miranda&site=2&land=1        The Ridge Camp

Luna — the mass-driver ruins (4)
  /map?dock=selene-gate&body=luna&site=0&land=1        The Wild Plain
  /map?dock=selene-gate&body=luna&site=1&land=1        The Depot Apron
  /map?dock=selene-gate&body=luna&site=2&land=1        The Derelict Pad
  /map?dock=selene-gate&body=luna&site=3&land=1        The Shadowed Rille

Phobos (4)
  /map?dock=the-space-bar&body=phobos&site=0&land=1    The Wild Plain — THE MONOLITH (#649: the one object, on the one ground)
  /map?dock=the-space-bar&body=phobos&site=1&land=1    The Ice Fissure
  /map?dock=the-space-bar&body=phobos&site=2&land=1    The Ridge Camp
  /map?dock=the-space-bar&body=phobos&site=3&land=1    The Crater Shelf

Jupiter's moons (6)
  /map?dock=red-eye&body=europa&site=0&land=1          The Wild Plain
  /map?dock=red-eye&body=europa&site=1&land=1          The Ice Fissure
  /map?dock=red-eye&body=ganymede&site=0&land=1        The Wild Plain
  /map?dock=red-eye&body=ganymede&site=1&land=1        The Ridge Camp
  /map?dock=red-eye&body=callisto&site=0&land=1        The Wild Plain
  /map?dock=red-eye&body=callisto&site=1&land=1        The Ice Fissure

Saturn's moons (4)
  /map?dock=ringside-exchange&body=titan&site=0&land=1      The Wild Plain
  /map?dock=ringside-exchange&body=titan&site=1&land=1      The Quiet Basin
  /map?dock=ringside-exchange&body=enceladus&site=0&land=1  The Wild Plain
  /map?dock=ringside-exchange&body=enceladus&site=1&land=1  The Derelict Pad

Triton (4)
  /map?dock=the-deep&body=triton&site=0&land=1         The Wild Plain
  /map?dock=the-deep&body=triton&site=1&land=1         The Shadowed Rille
  /map?dock=the-deep&body=triton&site=2&land=1         The Derelict Pad
  /map?dock=the-deep&body=triton&site=3&land=1         The Ice Fissure

The Clinker, Venus (2)
  /map?dock=cinder-roost&body=the-clinker&site=0&land=1     The Wild Plain
  /map?dock=cinder-roost&body=the-clinker&site=1&land=1     The Depot Apron
```

### What to look for

The audit (`EverySiteMeetsTheSpecTests`) already checks the geometry on all 27. What it **cannot** check is
everything in the spec's "not yet enforced" list, which is exactly what a pair of eyes is for:

- Are the shelters **findable** at this field size, or do you die looking?
- Do the crater rings read as scenery, or do you walk to one thinking it is a building?
- Does roughly half the ruins paying out feel right, or does it read as "mostly empty"?
- Does the ground look like a *place*, or like a field with objects scattered on it?

#### The shelter's two walls (#728)

```
/map?dock=the-tilt&site=0&land=1&shelter=1&mags=12
```

Sets you down a pace outside a shelter's door with both sentries holding twelve rounds. Walk in and check the three things the 2026-08-06 smoke run could not see:

1. **The plates say what they DO** — `🫁 CHARGING RACK — FILLS YOUR TANK` and `🔫 EMERGENCY LOCKER — FILLS YOUR MAGAZINES`. The owner, standing between them: *"on shelters I always forget which is which."*
2. **The magazines are on screen** — `🔫 MAGAZINES · K-77 12/99 in the sling · R-3B 12/99 in the sling`, under the motion tracker, above the key hints. Press `[E]` on the locker and watch it go to `99/99` in the same breath as the receipt says how many rounds went in. Before #728 that receipt paid into a number the player could see nowhere.
3. **Come down with nothing and the press says so** — board with no sentry in the sling and the readout reads `none down here — no sentry came with you`, and the press answers *"finds nothing to fill"* rather than claiming your magazines are full.

### Add-ons for any of the above

| Argument | What it does |
| --- | --- |
| `&air=45` | 45 seconds in the tank — the point-of-no-return warning without a six-minute stroll |
| `&reevers=4` | four Old Ones on top of you, already aware |
| `&outpost=1` | guarantee the outpost hut on this ground |
| `&shelter=1` | set down at a shelter's door instead of below the pad (#728) |
| `&mags=N` | each sentry comes down holding N rounds rather than a full 99 (#728) |
| `&kit=1` | the field dossier assembles on the first piece of kit, saying everything it can (#774) |
| `&collectors=20` | a repo boat sets down 20 s in, whatever your heat reads (#583) |
| `&secretlab=1` | a landable rock with a Vantar lab, hidden door already found (#409) |
| `&card=next` | the authority for the gate you will be standing at, in the wallet (#693) |
| `&wreck=1` | a derelict wins the toss instead of a moon |

**The secret lab's new space** appears where the hidden door is: the chamber is appended *from the door
outward toward the field's centre*, 16 du deep by 14 wide — a server spine, lab benches and stasis pods, with
the door→console lane kept clear. As of #585 that patch of deep field is reserved on every body before
anything is built, so the chamber can never open into somebody's wall.

## The Hive — reaching an underground facility without playing for it (#585)

Owner, after an evening of walking a 310 × 260 field to reach the one thing under test:

> *"instruct to put the debug cheat start next to the lab so that it can be really tested without playing to find it"* · *"I mean next to the elevator shaft"*

**The hunt is the game** — the clue that names a moon, the tracker's vague wash, the detector climbing from Faint to Screaming, the violet door on a shed marked `▤ MAINTENANCE`. It is also exactly what must not stand between a developer and the feature under test, twenty times an evening.

| URL | Where it puts you |
| --- | --- |
| `/map?secretlab=1&land=1` | **at the lift head**, a pace outside its door |
| `/map?secretlab=1&land=1&floor=1` | on **B1** — the one floor that still holds pressure |
| `/map?secretlab=1&land=1&floor=4` | on **B4**, dead air, tank running |
| `/map?secretlab=1&land=1&floor=20` | as deep as that site goes (clamped to its real depth) |

`?floor=` takes a **positive** number read as a depth: `floor=3` means B3. It is clamped to the site's own
bottom, so a shallow facility cannot be asked for a floor it does not have.

**#592 — it clamps to `TrueDepthOf`, not `DepthOf`.** A rare site has a band nobody listed under the
floors it admits to, and the point of these cheats is that the feature under test is one URL away — a
hidden floor you could only reach by first finding a card would be exactly the tax they were invented to
remove. Ask for a floor below the listed bottom and you land on the unlisted band's own shaft head
(there is a GAP between the two buildings with nothing dug in it, so the number is snapped past it).

**`/map?secretlab=deep&land=1&floor=24` is the one to use.** The ordinary cheat rock's site is seeded
like any other and happens to be four floors of records annex with nothing under it, so `?secretlab=1`
cannot reach #592 at all. `?secretlab=deep` parks a different rock — a 20-floor clinic with an
unlisted LABORATORY under it, down to the generator's own performance guard, which makes it the
deepest and most awkward site the game can produce and therefore the right one to test on. The cheat
is a body id and nothing else; the site is seeded from its name like every other site, and
`TheUnlistedBandTests` pins that it still hides something.

To find other sites that have one, run the lab — it prints `+ an UNLISTED band to Bn` in each site's
header:

```bash
dotnet run --project labs/44-a-lab-about-the-lab/Lab44.csproj -c Release
```

Both only fire under `?secretlab=1`. An ordinary landing still drops you on the open regolith at the landing band, so this can never quietly become how the game plays.

### The card, the row and the gate — `?card=` (#693)

The whole of #689/#692's beat is three things a card does, and until this cheat existed the only way to see
any of them was to find one:

```
/map?secretlab=deep&land=1&floor=1&card=next   B1 with the paper the gate downstairs reads
/map?secretlab=deep&land=1&floor=1&card=3      B1 with the WRONG band's paper — the refusal names it
/map?secretlab=deep&land=1&floor=1             B1 with nothing — the sealed row, exactly as it ships
```

Walk to the car and press `🛗`. With the right paper the gated row reads

```
B-5   ↓ THE OTHER SHAFT                                     🎫 opens for you
      🎫 SHAFT 2 · … — the gate will read it
```

and the **accepted beat lands when the doors open** on the new floor, not on the frame the panel closes
(#689). Since #693 it is also the line you will actually READ: the arrival's sayings carry a rank now and the
routine air line can no longer stand on top of a beat or a climax.

The head office (`?kaamos=hq&land=1`) has **no gate on any floor** and `?card=` there mints paper nothing
reads — that absence is the rank difference (#411), not a bug.

### The card that lands on top of the beat — `/map?secretlab=deep&land=1&card=next` (#768)

The same paper, pressed from the **SURFACE** instead of from B1 — which is the one ride that raises the
first-descent card (#585) and crosses a gate (#689) on the same arrival, and the frame #768 was filed on:

```
/map?secretlab=deep&land=1&card=next   set down at the shed, the first gate's authority in the wallet
```

Walk into the shed, press `🛗`, and take `↓ THE OTHER SHAFT` straight down from daylight. **Two things happen
on the one arrival:** a full-screen card stops the world (*"finding the elevator"*), and the arrival's
sayings — the descent, the air, and the gate reading your paper — want the one pulse line behind it.

**What to look for:** close the card. The gate's beat (🎫 *"You find the other shaft…"*) is **on the pulse
now**, with its full dwell, because the arrival held it rather than pulsing it under the backdrop. Before
#768 it was said and gone while you were reading the card, and the only place it survived was the field-notes
book — which is still where every one of the arrival's lines is filed, in the order they were said, whatever
the screen keeps. On a site whose first gated floor is the one nobody listed, the line waiting for you is
#592's climax instead: the hold obeys the same ranks the pulse does (#693), so what survives the card is
exactly what would have been on screen had no card been raised.

The same shape on the regolith rather than underground: `&collectors=20` (a repo boat sets down 20 s in) —
its arrival line and its callsign now wait behind the arrival plate instead of playing under it.

### What each floor should be

- **the SURFACE is VACUUM** (#802) — the regolith holds air on no body in the game, so the car panel's own
  `SURFACE` row reads **`dead air`**, greyed like every other airless stop, and the plate by the car says
  `NO ATMOSPHERE · TANK RUNNING` the moment you step out of it. It said `🫁 air` and titled itself *"holds
  pressure"* until #802, while the sim spent tank up there from the first step — the one button every captain
  presses on the way out was the only thing in the game that disagreed. If it ever promises air again,
  `TheSurfaceIsVacuumTests` should have caught it
- **the top of every shaft band holds pressure** — the tank stops, the nerve steadies, and the game says so in those words
- **everything else is dead** — the tank runs, and depth is paid for in air
- one car serves four floors; at the bottom of a band the panel simply **has no button** below, and the way down is another shaft
- **nothing is alive down there** — *superseded 2026-08-05, and again 2026-08-09.* The Old Ones are still a
  regolith tide, cleared on descent, but **B1 has people on it** (#709) — and **every floor between the bar
  and the bottom the directory admits to now has a security ROUND walking it** (#804). Nothing below that
  does: the unlisted band and the found halls stay empty, deliberately. See the sections below.

### The inhabited Hive (2026-08-05) — #707, #708, #701, #677, #709, #721

Six features landed in one day and everything above predates all of them. **Their links, verified floor numbers
and what-to-look-for live in [`testing-links-the-hive.md`](testing-links-the-hive.md)** — where every floor
number was read out of the real generator with Lab 44 rather than assumed — and the test plan is
[`QAHandoff-TheHive.md`](QAHandoff-TheHive.md).

The short version, because these are the easiest to miss:

```
/map?secretlab=deep&land=1&floor=1     B1  — the CANTINA HALL (#751): eighty seats, three cabinets, a board
/map?tablescene=1&watch=2              …the same hall on the heaving watch (&watch=5 for the small one)
/map?counter=1                         …the same hall, standing AT THE COUNTER, one press from ordering
/map?stool=1&neighbour=1               …up on a STOOL at that counter (#756): the park in the window, WAIT and she turns
/map?stool=1&neighbour=0               …the same stool where nobody turns, and the counter says so in words
/map?tablescene=free&approach=1        …at a FREE top (#757): [E] SITS YOU DOWN, SIT A WHILE brings her over
/map?tablescene=free&watch=5&approach=0  …the same table on the quiet watch: nobody comes, and it is a REST (#783)
/map?tablescene=free&watch=2&approach=0  …and the heaving watch, where the same sit is back-to-the-wall
/map?park=1                            …and THROUGH the glass: the PARK (#759) — gravel, beds, benches, masts
/map?frontdoor=1                       …the way IN (#775): out on the main corridor at the canteen's own front door
/map?freight=1                         …and where the food comes in: the GOODS HOIST, shut, and it says so
/map?designate=1                       …and the same shutter with a gun set down beside you: hand-load it, point it, fire
/map?patrol=2                          B2  — a ROUND on it (#804): watch the fan, then the boots, then the mark
/map?badge=1                           …the same round with the site's own pass already in your wallet
/map?parkback=1                        …and the FAR side of the green (#801): the horizon is a row of doors
/map?goodscar=1                        …the SECOND CAR (#801), at the blind end of the corridor. Now walk to the first one
/map?secretlab=deep&land=1&floor=17    B17 — the staff mess, pass-only, hall-sized, and empty on purpose
/map?secretlab=deep&land=1&floor=21    B21 — the unlisted lobby, where the plate names a DIFFERENT building
/map?secretlab=deep&land=1&floor=4&dark=1   a floor with no lights (#708)
/map?secretlab=deep&land=1&floor=2&book=9   force a specific odd book (#701)
/map?found=1&land=1&floor=17           past the seam — the halls nobody dug (#677)
```

**Three things here contradict older text in this file, and the newer answer wins:**

1. **B1 is populated.** Carriers and contractors at the canteen tables — that room's own plate reads
   `CARRIERS & CONTRACTORS · NO PASS REQUIRED`. Every floor below is deserted **on purpose**: the population
   falling to zero is what makes the descent a gradient rather than a corridor length.
2. **The facility plate is no longer on every floor** (#694). It draws on **B1** and on **the unlisted band's
   own lobby**, and nowhere else — not even on the other shaft heads.
3. **The canteen's roster turns over with the SHIFT**, not the visit. Re-entering on the same watch shows the
   same people in the same chairs; that is correct, not a bug. A different crowd needs sim time to pass — or
   `?watch=N`, which is what that cheat is for.
4. **B1's canteen is a HALL now** (#751) — eighty seats, twenty round tops in a 2/4/6 mix, a long bar counter,
   poured pillars, and **three CABINETS** down the back wall. How full it is varies **by watch** and nothing
   announces which one you are in: `?watch=2` heaves, `?watch=5` echoes. **B17's staff mess is hall-class
   too**, sized to seat the whole complement at one sitting — and it is empty on every watch, forever.

`?secretlab=deep` is the site for all of the above — a **20-floor clinic with an unlisted band at B21–B24**.
`?secretlab=1` is only four floors and cannot reach any of the deep content.

### Useful combinations

```
/map?secretlab=1&land=1&floor=2&air=90     a dead floor with ninety seconds in the tank
/map?secretlab=1&land=1&collectors=20      a repo boat lands while you are underground
```
