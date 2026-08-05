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
| **`?nerve=N`** | **Seed the nerve gauge at N of 10 whole pips at boot (#428/#480). Clamps to the gauge; `?nerve=10` is the shipped default. The only way to reach a sanity beat without being hunted for minutes first.** |
| **`?dark=1`** | **Put the FIXTURES OUT on every floor this excursion walks — the suit's forward-facing headlights become the whole of the seeing, and everything outside the cone is BLACK rather than dim (#708). The FOUND HALLS (#677) declare themselves dark and are the only floors that do, so this is how every OTHER floor is seen in the dark — and how the cone is exercised without hunting for the one site in fifty that has galleries. It changes ONE fact — what `UndergroundComplex.IsDark` answers — and nothing else: collision, air, the pack, the sentries and the motion tracker all behave exactly as they do with the lights on, so you can walk into what you cannot see, and something you cannot see can walk into you. Above ground is never dark (a surface has a sun). Pair with `&secretlab=deep&land=1&floor=4`.** |
| **`?process=N`** | **How long processing one document takes, in sim seconds — `?process=0` makes it INSTANT (#696). Leaving a paper or a file with 🫳, and reading a paper as a clue at the tracker, are a twenty-second hold of standing still; that IS the mechanic, and it is exactly the wrong thing to make a story test sit through. Any other value tunes the feel from the URL without a rebuild. Pair with `&dock=the-tilt&site=0&land=1`. There is deliberately no switch for what a hold costs in AIR, because nothing computes that: the hold passes sim time and the suit prices sim time, which is the whole of the owner's ruling.** |
| **`?book=N` / `?book=on`** | **Put THE ODD BOOK in every would-be-empty room this excursion searches (#701). `1`–`10` force that catalog entry, which is how all ten authored texts get read on demand; `on` (or `all`/`any`) forces the SEEDED entry, i.e. the shipped selection with the one-in-six gate taken off, which is how the Laboratory/Transit-station weighting is watched working. It cannot put a book in an OCCUPIED room — a book is what a would-be-empty room has *instead of* the empty line, and a cheat that laid one on top of a pallet would have you playtesting a room the game cannot produce. It is an ARGUMENT to `OddBooks.Search` and never a second answer OR-ed in beside it (the `?dark=1` rule). Pair with `&secretlab=deep&land=1&floor=2`.** |
| **`?found=1`** | **Park the one rock in the system with a band NOBODY DUG under the band nobody listed (#677), set down at the lift head, and start with every authority this site ever issued already in the wallet — including the last one, which is the way past the seam. About one site in fifty has galleries and the way in is a card somebody left in a room eleven floors down, so without this the feature is unreachable in practice. It implies `?secretlab=1` (there is no other way down). It overrides no Core fact: the rock's whole shape — its depth, its two kinds, its unlisted band and its halls — is seeded off its body id (`UndergroundComplex.FoundBandCheatSiteId`) exactly like every other site, so what you walk is what a captain would walk. The cards are minted through the real `AuthorityCard` and put in the real satchel, so the panel, the gate, the refusal ladder and the wallet fan all behave as they do for somebody who earned them. Pair with `&land=1`, and with `&floor=17` to ride straight to the first gallery.** *(It is also a button in the front door's **⚙ DEV START SITES** list — 🕳 “The halls nobody dug”.)* |

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

### Add-ons for any of the above

| Argument | What it does |
| --- | --- |
| `&air=45` | 45 seconds in the tank — the point-of-no-return warning without a six-minute stroll |
| `&reevers=4` | four Old Ones on top of you, already aware |
| `&outpost=1` | guarantee the outpost hut on this ground |
| `&collectors=20` | a repo boat sets down 20 s in, whatever your heat reads (#583) |
| `&secretlab=1` | a landable rock with a Vantar lab, hidden door already found (#409) |
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

### What each floor should be

- **the top of every shaft band holds pressure** — the tank stops, the nerve steadies, and the game says so in those words
- **everything else is dead** — the tank runs, and depth is paid for in air
- one car serves four floors; at the bottom of a band the panel simply **has no button** below, and the way down is another shaft
- **nothing is alive down there** — *superseded 2026-08-05.* The Old Ones are still a regolith tide, cleared on
  descent, but **B1 now has people on it** (#709). See the section below.

### The inhabited Hive (2026-08-05) — #707, #708, #701, #677, #709, #721

Six features landed in one day and everything above predates all of them. **Their links, verified floor numbers
and what-to-look-for live in [`testing-links-the-hive.md`](testing-links-the-hive.md)** — where every floor
number was read out of the real generator with Lab 44 rather than assumed — and the test plan is
[`QAHandoff-TheHive.md`](QAHandoff-TheHive.md).

The short version, because these are the easiest to miss:

```
/map?secretlab=deep&land=1&floor=1     B1  — the canteen: PEOPLE at the tables, a cork board, washrooms
/map?secretlab=deep&land=1&floor=17    B17 — the staff mess, pass-only, furnished and empty on purpose
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
   same people in the same chairs; that is correct, not a bug. A different crowd needs sim time to pass.

`?secretlab=deep` is the site for all of the above — a **20-floor clinic with an unlisted band at B21–B24**.
`?secretlab=1` is only four floors and cannot reach any of the deep content.

### Useful combinations

```
/map?secretlab=1&land=1&floor=2&air=90     a dead floor with ninety seconds in the tank
/map?secretlab=1&land=1&collectors=20      a repo boat lands while you are underground
```
