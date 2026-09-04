# Testing guide — the owner's regression checklist

A scripted playtest per major feature: exact clicks/keys, what you should see, and what
"broken" looks like. Run through these after any change that touches the map, the ship
simulation, or the deck views.

This catalog is also machine-read: the [boot-every-scene workflow](workflows/boot-every-scene.md)
sweeps these rows headless after a release, using each row's "what a tester should see" as its
oracle — one more reason to keep those expectations written down and current. The workflow shelf
index is [docs/workflows/README.md](workflows/README.md).

**Running the xUnit suites:** the full run is `dotnet test SpaceSails.slnx -c Release` and takes
about six minutes; the inner-loop run is `--filter "speed!=slow"` (or `./test-fast.ps1`) and takes
about forty seconds. A green fast run means the rules hold, not that the ship flies — see
[Appendix C](#appendix-c--the-fast-run-and-the-full-run-251-item-4) for exactly what it skips and
why. CI always runs the whole suite. If your new guard installs one of Core's process-wide
registers, read
[Appendix D](#appendix-d--the-process-wide-registers-and-why-some-suites-run-alone-1108) first — it
is the difference between a suite that is correct and a suite that is correct most afternoons.

**Before you start:** run `./run.ps1` (Release build) and open the printed localhost URL.
Debug WASM runs on the IL interpreter and is roughly **100× slower** — choppy frames, sluggish
plotting, and timings in these scripts (rum wobble, boarding time, warp behavior) will all read
wrong under Debug. If a script feels broken, check you're not accidentally running
`./run-debug.ps1` first.

Each script links to the matching [feature doc](features/) if you need the full behavior
reference.

**Ask this of every script below, and of every string you add to the game (owner ruling
2026-08-08, #782): _is every new string readable where it renders, at phone size?_** — good
contrast against what it actually sits on (including over gen-AI art and behind a modal), a font
big enough to read on a phone, and when it does not fit, the panel **scrolls**; it never shrinks.
Dark-on-dark is a defect anywhere it occurs. Two guards stand behind that sentence:
`SpaceSails.Client.Tests.EveryTextReadsTests` sweeps the shipped stylesheets and every art slot in
`Map.razor`, and `SpaceSails.UiGate.EveryTextReadsTests` boots `?stool=1` at 390 × 700 and measures
the real contrast of every visible text run against the deck canvas's own pixels.

**And ask this of every change you make (owner ruling 2026-08-08, #761): _does anything
plot-significant happen here, and where does the player read it?_** — plot-significant means it
changes what the captain **knows, owes, is owed, or can do**: a reveal, a debt, a standing gained or
lost, a door that will now open, somebody who will now remember. The player is told **at the moment
it happens, on the surface they are looking at** — never only in a log. The field book and the
autopilot ledger are the record; they are not the telling, and *"it is in the book"* is the answer
this rule exists to refuse.

Three surfaces, and which one is right is a question about where the eye is, not about how loud the
moment is:

| The captain is… | Say it on | Seam |
| --- | --- | --- |
| doing nothing else — the moment IS the pause | a card, or a plate at the edge | `RaiseStoryBeat(…)` |
| looking at a pop-up | that pop-up's own outcome region | `SayItWhereTheyAreLooking(…)` (#736) |
| flying, walking, being chased | the HUD pulse, at a rank that cannot lose the slot | `ShowPulseMessage(line, Telling.Floor)` (#693) |

`SpaceSails.Client.Tests.ThePlayerIsToldTests` holds every answer already given — one row per
moment, checked against the shipping source — and it goes **red** if you give a line a
plot-significant rank without saying in that table what moment it is and where it is read. What it
cannot do is decide whether YOUR new event is plot-significant: that judgement is the question
above, and this is the only place it gets asked.

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

**What the boot should look like (#161, staged).** The front door — the berth list, the saved
voyages, **Continue — docked at &lt;haven&gt;** — comes up **live and pressable within about a
second**, long before the world behind it is finished; measured 0.2 s on the shipping (AOT) build
and 0.6 s on a plain local publish. Choosing from it while the sky is still being plotted is
supported and is the ordinary case: the door shuts, the ⚙ loading door shows through narrating the
phase it is on (*"plotting the traffic lanes — freighter 5 of 8…"*), and the voyage starts the
moment the world is ready. **Broken looks like:** a front door stuck on *"Warming the reactor — the
ship's computer is still booting"* for more than a couple of seconds, a berth click that does
nothing, or the picker reappearing **on top of** the voyage it just started.

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

## 10. Deck walk, rum wobble

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
6. Press `Q`. Confirm you're returned to the helm view (map/plot), not just the deck plan.
7. Press `F`. Confirm **nothing happens** — the walk-in view it opened was removed (#958) and the
   key was deliberately left unbound rather than remapped.

**Broken looks like:** no wobble after 3 rapid tots, wobble blocking interaction entirely, or `F`
doing something.

### 10b. Her cantina is a bar now (#1040)

Owner, on 7 Deck: *"Our on ship bar can be upgraded to match the other bars... the UI represents code
long time ago."*

1. On 7 Deck, look at the **CANTINA**. It should read as a bar rather than as three rings on an empty
   floor: a **counter** down its aft side with a filled slab for its top and the **back-bar** shelving
   behind it, a **row of three stools** tucked in along its front, the three round **tops moved under
   the panoramic window** (all three take a seat now, where one used to be refused), and the
   **CANTINA** console over on the forward window corner where the galley machines are — it opens the
   same galley card it always did.
2. Walk into the counter. Confirm it **stops you** — you belly up to a bar, you do not walk through
   one — and that you can still get round its near end into the servery behind it.
3. Walk to a stool and press `E`. Confirm you sit **on the stool you walked up to** (try the near one
   and the far one), the strip says **🪑 YOUR OWN COUNTER**, and **SIT A WHILE** answers in the
   counter's own words rather than with a line about the chair opposite.
4. With papers in the sleeve, press **Work the case** on that strip. Confirm it is **refused out
   loud** — *"Not at the bar. Everything you put on this counter is read by the keep…"* That is
   correct and it is the joke: a stool is the bar-stool rung, and the gumshoe rule holds at your own
   bar too.
5. Walk four paces to a top under the window, sit, and press **Work the case** again. Confirm it
   **opens** — the refusal is about the seat, never about the boat.
6. Walk to **CABIN 2** and press `E` at its **DESK ✍** (it has one now, like CABIN 1). Confirm the
   cabinet rung, the case spreading unconditionally, and that `E` from the chair opens the desk
   rather than putting you to bed.

**Broken looks like:** a counter you walk through; a servery you cannot get behind; `[E]` at the far
stool seating you at the near one; the case opening at the counter; the same wait line twice; a top
under the window with no seat on it; or one desk where two berths are drawn.

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
3. Let it reach 90%+. Confirm the HUD flags "⚡ ARCING — visible system-wide" **and that you can see
   it on the map without reading the HUD at all**: a short whip stands off her beam with three
   filaments crawling slowly off its tip (#528 §7). It is a PLUME off one extremity, never a ring
   around the hull — field strength is potential over radius of curvature, so a discharge leaves the
   sharpest thing she has and a sphere is the one shape it cannot be.
4. **Pause** (`space`) while she is arcing. Confirm the filaments FREEZE. The crawl is seeded from
   sim time, so a stopped world draws a stopped plume; a plume that keeps dancing while paused is
   the wall clock leaking into the picture.
5. Press `V`. Confirm charge drops to roughly half its prior value, the arcing warning clears once
   under 90%, and the dump itself SNAPS — five brighter, longer filaments off the same masthead,
   gone inside about two-thirds of a second. Do it again from a nearly cold hull and confirm the
   flash is dimmer and shorter: the brightness is the charge that actually left her.
6. Press `V` again immediately. Confirm a "Vent recharging…" message appears (cooldown) rather
   than a second instant halving.
7. Fly into a plasma stream while charged and confirm you feel a push along the stream's
   direction (speed changes without spending a pulse).

**Broken looks like:** charge never climbing near the sun, arcing warning never appearing, venting
not reducing the charge value — or the discharge drawn as a ring centred on the ship, drawn while
she is merely GLOWING, the same brightness whatever she dumped, or still dancing on a paused map.

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

1. Launch Sol. Intercept a freighter (not a pod) close enough to be inside weapon range
   (7.128×10⁸ m — the mass driver's 66 km/s over a three-hour engagement horizon, and WIDER
   than both the boarding capture envelope and a hunter's catch radius, per the owner's
   #961/#962 ruling). Press `3` (or click **3 War room** in the station
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

## 20. The carried compass — MISSIONS in the satchel (#727)

*(See [captains-position.md](features/captains-position.md).)*

1. Open `/map?start=cinder-roost&crack=active`. Press `0` and confirm the **📜 Ledger** tab lists the
   break-in with the status label **"▶ Crack hatch V-06 — code …"** (the hatch id and code are the
   station's own).
2. Leave the desk and walk the station deck. Press `I`. Confirm the satchel's tab strip reads
   **🎒 CARRIED · 📓 NOTES · 🗺 MISSIONS**, with a count beside MISSIONS.
3. Press **🗺 MISSIONS**. Confirm the title reads *WHY YOU ARE DOWN HERE*, and that the break-in's
   line shows the job's name, its why, and **the desk's step word for word** — the same hatch id and
   the same code, in the lit ink.
4. Confirm there is **no button anywhere on the page** — no burn, no dock, no "abandon". The carried
   view is read-only; you act by walking to the thing and pressing `E`.
5. Now `/map?start=cinder-roost&fetch=active`. On the same page, confirm the fetch reads
   **"⛵ return to the ship — next: Derelict Roadster"** and not "fly to the roadster" — a run the
   chair owns must render as direction, never as an instruction you cannot follow down here.
6. Walk to the hatch, press `E`, key the code. Confirm the package is pocketed and the receipt is
   readable. Then press `I` → 🗺 MISSIONS again and confirm the line has moved on to the hand-off.
7. Press `I` on the MISSIONS page, walk to The Fixer at the bar, press `E`. With the satchel open,
   confirm the pay-off sentence appears **inside the satchel**, not on the HUD banner behind it.
8. Take nothing on at all (`/map?ashore=1`) and open 🗺 MISSIONS. Confirm the page still exists and
   says *"Nothing owed on foot…"* rather than showing a blank list or hiding the tab.

**Broken looks like:** a mission on the captain's desk that is missing from the pane (or the other
way round), a foot-level step worded differently in the two places, a burn/dock/clamp instruction
shown verbatim to a captain on foot, any button on the pane, the tab vanishing when the compass is
empty, or a completion beat that fires on the banner behind the satchel's backdrop.

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
| **`?target=<contact-id>`** | **Point the tactical UI at a contact and open her DOSSIER on the Nav glass at boot (#997 wave 10).** Ids look like `npc-0`, `pod-1`, `hunter-0`; BOTH rosters are searched (a hunter is never in the traffic list — #962), and a wrong id answers with the ids this sky actually holds. Traffic is handed the fix a completed telescope pass would have entered, and the next sweep re-decides whether she is still live. |
| **`?target=collector`** | **Send the muscle first, then read her file.** Spawns a collector down the shipping heat-event road (nearest policed body, news wire and all) and opens her dossier — the fullest card the game draws: warrant, hiding, nerve, sail. The one URL behind #960's card; before it, three waves of the shell migration measured that card by hand. |
| **`?dest=<body-id>`** | **Boot with the NAVIGATION DESTINATION already set (#956)** — the thing `Follow dest` follows, laid down through the page's own `SetDestination`, so the Fly to order it writes and the pass it dirties come with it. Not the same key as `?target=`: that points the tactical UI at a *contact* (a ship), this sets the nav target on a *body*. It had to exist for the reason `?target=` did — a destination's only other road is a click on a body's menu drawn on the **canvas**, where an automated browser has no DOM to press, so `Follow dest` could not be proved on the pixels without it. Try `/map?start=wreck&dest=saturn`. **NB: pair it with a free-flying start** (`wreck`, `enceladus`); every *berth* start steps the captain ashore into the station interior, where the Nav toolbar is not on the screen at all. |
| `?ellipse=1` | Append a visibly eccentric demo body (Kepler rails). |
| `?sling=<bodyId>` / `?skim=<bodyId>` | Boot onto an approach arc with a close pass / atmosphere graze. |
| `?expedition=1\|mining` | Spawn an away-team gig ALREADY ACCEPTED, its rock parked in shuttle range (#370). |
| `?deflection=1\|c\|s\|m` | Spawn the asteroid-deflection gig accepted, rock inbound, ship docked at Ringside (#394). |
| **`?crew=petition`** | **A DEPUTATION — three of them in the corridor outside your door (#663).** Boots holding the voyage the crew send one over: five of them left on the rock (the `?deflection=` gig above is the only thing in the shipped game that kills a crewman), and every wreck since filed honestly, so the share is empty and the bunks are too. It grants those two counters and nothing else — no standing is written and no card is pushed; the ship's own clock reads the crew sheet on the next tick, finds them past `CrewTemp.Standing.Petition`, and the beat arrives through the ordinary door with its cadence spent and its line in the ledger. Read the sheet behind it on the **Captain desk → the crew's report**: PETITION at the top, GETTING HOME on the floor and THE SHARE down with it. `?crew=deputation` is the same door. |
| **`?crew=meeting`** | **THE MEETING YOU WERE NOT ASKED TO — the cantina at an odd watch, and a chair pulled out that nobody is sitting in (#1066).** The same ruined voyage as `?crew=petition` above, with nobody ashore in five berths on top of it. **The shore-leave rule:** a clamp at a GREAT PORT is a run ashore — that is Ringside Exchange and The Red Eye, the two berths the arrival tube (#541) gives a glazed gangway to — and every other berth is a working stop. Four working stops in a row breaks the captain's word, and every berth past that breaks it again, which is what carries the crew sheet from PETITION down to `CrewTemp.Standing.Ultimatum`. It grants counters and nothing else; the ship's own clock reads the sheet on the next tick and the beat arrives through the ordinary door. Read the sheet on the **Captain desk → the crew's report**: ULTIMATUM at the top, THE CAPTAIN'S WORD on the floor, and the shore-leave footnote under the bars saying how many stops it has been and where the line is. `?crew=ultimatum` is the same door. |
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
| **`?death=<cause>`** | **KILL THE CAPTAIN AT BOOT, through the real pipeline — the death card, the freeze beat and the brain-backup wake, without dying for them (#621).** Every cause has a lane now: `?death=void` was the last one that did not, and #638 gave it the twenty-day adrift clock (`VoidRule`). |
| **`?ashore=1`** | **Boot docked AND ALREADY STANDING IN THE BAR — the ship → airlock → tube → immigration hall → bar walk already walked (#428). Every bar beat begins with that walk; in a hidden/automated tab it cannot be walked at all. Pairs with `?dock=` / `?start=`, and with every bar cheat.** |
| **`?watchers=1`** | **Open the MONOLITH GROUND'S attentive window and cut the dwell from forty seconds to two, so the strange-things-happen beat (#649) can be watched on demand. Stand at the stone. It is rare by design — one visit-window in three, and then only if you stay — and this changes the GATES and nothing else, so what you see is what a captain sees. Pair with `&dock=the-space-bar&body=phobos&site=0&land=1`, and with `&reevers=3` for the variant that needs a pack on the field.** |
| **`?nerve=N`** | **Seed the nerve gauge at N of 10 whole pips at boot (#428/#480). Clamps to the gauge; `?nerve=10` is the shipped default. The only way to reach a sanity beat without being hunted for minutes first. #784 adds three WORDS beside the number, for links a person has to read: `?nerve=shot` (0), `?nerve=low` (2), `?nerve=half` (5). Same flag, same clamp — the words are spellings of the number and never a second parser.** |
| **`?hurt=N`** | **STEP OUT OF THE BOAT ALREADY MARKED (#784) — N of `CaptainCondition`'s five blows already landed, so the condition pips under the nerve bar read *bruised* / *bleeding* / *badly cut* from the first frame. Built for the SHORT REST's healing half, which on an unmarked captain is a mechanic with nothing to demonstrate; it is also the one way to see the block roll's modifier stack, the wounded breathing rate and the low-health nerve beat without being caught first. It can never seed the fifth blow — booting a tester into a death card is not a demo. Pair with `&tablescene=free&approach=0&nerve=low`.** |
| **`?shelter=1`** | **SET THE BOOTS DOWN AT A SHELTER (#728) — a pace outside the door of the one building on the ground that fills a tank AND fills a magazine.** The shelters seed DEEP on purpose (`SurfaceShelter.PlacesOn` keeps them out of the landing band), so every look at their plates, their receipts and the ammunition readout above them used to cost a two-minute walk across 310 x 260 du of regolith — which is how the owner came to be standing between the two fixtures in a live smoke run saying *"on shelters I always forget which is which."* It moves ONE fact, where you are standing, and stands you OUTSIDE: the proximity cycle, the arrival line and the pressure crossing are part of what you came to look at. What a tester should see: `🫁 CHARGING RACK — FILLS YOUR TANK` on one wall and `🔫 EMERGENCY LOCKER — FILLS YOUR MAGAZINES` on the other, a `🔫 MAGAZINES · K-77 12/99 in the sling · R-3B 12/99 in the sling` line under the motion tracker, and — on `[E]` at the press — that line moving to `99/99` in the same breath as the receipt. Pair with `&mags=N`. *(Also a button in the front door's **⚙ DEV START SITES** list — ⛺ “The shelter — air on one wall, rounds on the other”.)* |
| **`?mags=N`** | **BRING THE SLING DOWN HOLDING N ROUNDS EACH (#728).** Every sentry lands full (99) on a fresh ship, so the magazines readout, the shelter press's receipt and both of the locker's refusals could otherwise only be looked at after a real firefight. `?mags=0` is the dry sling — the state the on-foot HUD had no way of showing at all before #728 — and `?mags=99` is the shipped default, which is what makes the press answer *"finds them full, and goes back to sleep"*. It sets the ONE number: the roster, the ammunition kind and every law downstream are the shipped ones. Applied where the magazines cross into the excursion, never later. |
| **`?dark=1`** | **Put the FIXTURES OUT on every floor this excursion walks — the suit's forward-facing headlights become the whole of the seeing, and everything outside the cone is BLACK rather than dim (#708). The FOUND HALLS (#677) declare themselves dark and are the only floors that do, so this is how every OTHER floor is seen in the dark — and how the cone is exercised without hunting for the one site in fifty that has galleries. It changes ONE fact — what `UndergroundComplex.IsDark` answers — and nothing else: collision, air, the pack, the sentries and the motion tracker all behave exactly as they do with the lights on, so you can walk into what you cannot see, and something you cannot see can walk into you. Above ground is never dark (a surface has a sun). Pair with `&secretlab=deep&land=1&floor=4`.** |
| **`?process=N`** | **How long processing one document takes, in sim seconds — `?process=0` makes it INSTANT (#696). Leaving a paper or a file with 🫳, and reading a paper as a clue at the tracker, are a twenty-second hold of standing still; that IS the mechanic, and it is exactly the wrong thing to make a story test sit through. Any other value tunes the feel from the URL without a rebuild. Pair with `&dock=the-tilt&site=0&land=1`. There is deliberately no switch for what a hold costs in AIR, because nothing computes that: the hold passes sim time and the suit prices sim time, which is the whole of the owner's ruling.** |
| **`?book=N` / `?book=on`** | **Put THE ODD BOOK in every would-be-empty room this excursion searches (#701). `1`–`10` force that catalog entry, which is how all ten authored texts get read on demand; `on` (or `all`/`any`) forces the SEEDED entry, i.e. the shipped selection with the one-in-six gate taken off, which is how the Laboratory/Transit-station weighting is watched working. It cannot put a book in an OCCUPIED room — a book is what a would-be-empty room has *instead of* the empty line, and a cheat that laid one on top of a pallet would have you playtesting a room the game cannot produce. It is an ARGUMENT to `OddBooks.Search` and never a second answer OR-ed in beside it (the `?dark=1` rule). Pair with `&secretlab=deep&land=1&floor=2`.** |
| **`?autowalk=1`** | **RETIRED (#875) — parsed, and it changes nothing.** Click-to-walk shipped behind this flag as a dev cheat *"until the owner rules on always-on"*; the owner ruled on 2026-08-15: *"click to walk should always be on when the arrows for walking are active also. The two should be linked as alternative UI methods for walking."* So **a click on the deck is a control of the game now, on every walked view (a surface excursion, every Hive floor, the ship's own deck), with no URL at all** — and it is refused or held by the very same predicate that refuses or holds the arrow keys (`Map.Deck.TheCaptainsLegsAreTheirOwn`): the escort holds both (#833), a seat costs both the stand first (#847), a stalled machine says so to both (#825), and a click that plans nothing says why (#866). The flag is kept only as a **no-op alias** so old dev links still boot. What the click always was and still is: A\* over the same walls the collision uses, at the same walking speed — NOT a teleport and NOT a faster walk, so air drains, the nerve frays, the tracker rings, the auto-doors cycle and the Old Ones close exactly as they do under WASD. **Any movement key cancels instantly** — the keys always win. Click a console, a hatch or a door and the walk stops ADJACENT to it, so `[E]` is live the moment it ends. A drag still pans the deck plan. |
| **`?found=1`** | **Park the one rock in the system with a band NOBODY DUG under the band nobody listed (#677), set down at the lift head, and start with every authority this site ever issued already in the wallet — including the last one, which is the way past the seam. About one site in fifty has galleries and the way in is a card somebody left in a room eleven floors down, so without this the feature is unreachable in practice. It implies `?secretlab=1` (there is no other way down). It overrides no Core fact: the rock's whole shape — its depth, its two kinds, its unlisted band and its halls — is seeded off its body id (`UndergroundComplex.FoundBandCheatSiteId`) exactly like every other site, so what you walk is what a captain would walk. The cards are minted through the real `AuthorityCard` and put in the real satchel, so the panel, the gate, the refusal ladder and the wallet fan all behave as they do for somebody who earned them. Pair with `&land=1`, and with `&floor=17` to ride straight to the first gallery.** *(It is also a button in the front door's **⚙ DEV START SITES** list — 🕳 “The halls nobody dug”.)* |
| **`?buried=1`** | **The same rock as `?found=1`, one shift later: the ground has been OPENED a whole world window ago, so the burial (#1063) fires on the way down and you land on a site whose galleries have been filled, floored and resurfaced. Implies `?found=1`. It seeds the disclosure clock's register and nothing else — the fill runs through the ordinary `Burial.Fill` on the ordinary descent, because a cheat that wrote a filled ground straight in would be testing a code path the game does not have. The lift panel now stops at the listed bottom; on that floor there is a short recess with one old door in a flat grey that belongs to no palette and does not open; a mason is at a table in the upper canteen; the maintenance ledger is in the first room searched; and the wire has one cheerful line about drainage. Nothing anywhere says a ground was buried. See “The ground that was filled in while you were away”.** |
| **`?stopped=1`** | **`?buried=1`'s twin (#1074): the same rock, the same ground opened a whole world window ago, and a window chosen so the split hands this one to the AUTHORITY instead of to the neighbours. Implies `?found=1`. Nothing is filled in — the galleries are still there — but the shaft below the listed bottom is sealed: the lift panel stops at the listed bottom with no row and no refusal, a leaf at the blind end of the spine reads `AUTHORITY — WORKING CLOSED` and gives the order verbatim, the plant's valve-book is in the second room searched on that floor, and the week's rota is still up in the upper canteen — beside a personnel register row naming one hand off that shift, two regulars who answer about him, and a mug nobody will move (beat 4). See “The working that was closed while you were away”.** |
| **`?preserved=1`** | **`?stopped=1` one shift further along (#1074 beat 2): the same rock, the same ground handed to the office by the split, opened TWO whole world windows ago — so the order fires on the way down and then the closed working passes into official CARE. Implies `?stopped=1` and `?found=1`. Nothing below ground changes at all; everything new is on the surface, at the survey shed the lift comes up in. It stands inside a small closed ring of rail with exactly ONE gap in it, the gap faces the tube, and at the gap one line of ground label reads `AUTHORITY — THIS SITE IS PRESERVED. Its significance is under study.` — no date, no department, no name. See “The site that passed into official care”.** |
| **`?card=next` / `?card=N` / `?card=all`** | **MINT AN AUTHORITY CARD BEFORE THE FIRST RIDE (#693) — the one cheat that makes the CARDED lift row, the gate beat and the refusal ladder reachable on an ordinary site.** #692 shipped all three and closed with the honest note that none of them had been seen in a browser: *"reaching the row needs an authority card in the wallet and no dev cheat mints one"*. `next` mints the band under wherever you are set down — the gate you will be standing at — asked of `NextShaftBelow` so it steps over the band of nothing under the unlisted floors (#677). `N` mints that band specifically, which is how the WRONG-card refusal is seen. `all` is `?found=1`'s whole wallet on any rock. It names a **band**, never a card id: a body typed into a URL is a body the landing may not be on, and the cheat would mint paper no gate on the ground reads. A band the site does not have mints nothing **and says so**, naming the bands it does have. Minted through the real `AuthorityCard` into the real satchel, so the panel, the gate, the refusal and the wallet fan behave exactly as they do for a captain who earned it. Implies `?secretlab=1`; pair with `&secretlab=deep` or `&found=1` to choose the rock. Try `/map?secretlab=deep&land=1&floor=1&card=next`.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🎫 “The lift row the card unlocks”.)* |
| **`?kit=1`** | **ASSEMBLE THE FIELD DOSSIER ON THE FIRST PIECE OF SOMEBODY'S KIT, WITH EVERY SENTENCE IT CAN CARRY (#774/#588).** The dossier is the rarest beat on the regolith: it wants three *papers* rooms inside ONE excursion at one room in eight, and its four-sentence form — the person, the next of kin, what that family knows, and the phrase that opens a door somewhere else — is two more one-in-three rolls behind that. Which is exactly why #774 shipped: the card raised, all four sentences pulsed **under its own backdrop**, and nobody could stand in front of the scene to notice. This moves the two GATES and nothing behind them — the stranger, the family, the hint, the in and the moon they name are the seeded ones for the room you actually completed, so what you read is a card a captain can genuinely be handed. Pair with `&outpost=1` for the shortest road to one: the hut's SOMEBODY'S EFFECTS console is a piece of kit in its own right (#588), so one press assembles the whole thing. Try `/map?dock=the-tilt&site=0&land=1&outpost=1&kit=1`.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🗂 “Whose kit was this — the whole dossier”.)* |
| **`?tablescene=1`** | **BOOT THE TABLE SCENE (#746) — the B1 canteen of a deep site, with people in it, one URL from the front door. Walk to a table with somebody at it, press `[E]`, and ask to join. It implies the whole route (`?secretlab=deep&land=1&floor=1`) rather than adding a fourth spelling of it, and sets the captain down IN the canteen. (It used to turn `?autowalk=1` on because the last leg is a walk across a room; since #875 the click walks you on every boot, so there is nothing left to turn on.) It does NOT force who is at the tables: the rota is seeded off the site and the watch like any other shift (#709), and a cheat that seated THE HAND for you would be testing a room that does not ship — if this watch has no Hand in it, that is the room, and the next shift is a reload away. Three of #709's cast are scenes (the hand, the fitter, the temp); everybody else keeps their one breath.**<br><br>**#792 · READ THE ROOM BEFORE YOU CROSS IT.** Owner: *"people looking to sit down look at those like hungry wild beasts look at their prey… Now I have trouble finding a free table."* Every top now draws the chairs it actually seats, in three marks and no words: a **grey bar** is a chair nobody is in at a table nobody is at; a **green bar** is a free chair at a table somebody is ALREADY at — the **invitation**, and the whole of the second glance; a **warm filled body with a bar behind its shoulders** is somebody sitting there, in the same chair-back idiom the seated captain got in #788. **Two warm ticks struck over a top mean a conversation** — there is something to **overhear** — and a top without them holds somebody on their own, which is who `[E]` can ask to join. None of it is worked out on the glass: the occupancy and the conversation both come off `CanteenRegulars.Tables` at the frozen watch, so a chair drawn free is a chair the press will offer. Pair with `&watch=2` and then `&watch=5` — the same room, two different answers to all three glances. |
| **`?counter=1`** | **BOOT THE COUNTER (#756) — the B1 cantina hall of a deep site with the captain standing AT THE COUNTER, one URL from the front door. What a tester should see: press `[E]` and the SERVICE CARD opens, already on the menu — COMPANY COFFEE at 2 cr, the CAGE CREW'S BREAKFAST, SUB-BASEMENT STEW, and three pours that joke about the deep; order anything and the receipt answers **on the card** (#736), the purse on the card drops by exactly the price on the button, and food does not tilt the deck the way a pour does. It is the same card the Tilt bar opens (#247) pointed at a different venue — so the two verbs that need a person (a round for the room, "hear a rumor") are absent here, because nobody is behind this counter. Implies the whole route (`?secretlab=deep&land=1&floor=1`) rather than adding another spelling of it. It forces nothing about the room: the watch, the rota and the purse are whatever the boot gave you. Pair with `&credits=50000` to price the whole card, or `&watch=2` for a heaving hall.** **#780 — what BROKEN looks like here, because it shipped that way for a day:** the menu renders but reads as a greyed-out panel *behind glass*, its buy buttons dim and apparently dead, and the owner's own report was *"How do I buy the drink here?... There is no button to pay there?"*. That is the menu having slid under the card's **pinned action row**, whose sticky foot paints a 12rem near-black scrim over everything below it. The menu belongs **above** that foot, in the card's scrolling body — and the five illustrated rows must show their photographs beside the words rather than in place of them. **#792 — and now the eight stools are ON THE FLOOR.** They have been in Core since #756, occupied or not, watch by watch, and were drawn nowhere: a captain could only learn the row was full by walking up and pressing. A **hollow grey ring** is a free stool at a counter nobody is at; a **hollow green ring** is a free stool at a counter somebody IS at (the same **invitation** ink the tables use — one language for one question); a **filled warm disc** is a stool with somebody up on it, and it has no back drawn because a bar stool does not have one. Stool *n* on the floor is stool *n* in `🪑 STOOL n · THE COUNTER`, so the seat you can see is free is the seat `?stool=1`'s pick-or-default hands you. Compare `&watch=2` (a queue at the bar) with `&watch=5` (one soul and seven empty seats). **#791 — and now THE WHOLE DESK SERVES.** Owner, live: *"The Bar desk is really long now, but there is only one spot to get service on it… we would need an **E-bus** of the bar desk length instead of one bar keep cashier at a single spot."* The serving desk is **81.9 du** long and the press used to reach **six** of them, in the middle, unmarked — 7% of a fixture whose photograph, whose collidable wall and whose eight stools all run the full length. **What a tester should do: walk the desk end to end.** `[E]` must answer from anywhere along it and from ~3 du out into the room, the **`[E]` prompt follows you** along the counter instead of sitting at the plate, and the desk now wears a **service rail** — a line down its whole front with a serving tick struck across it every 5 du, dim while you are away and lit the moment it answers. It is still **one** console, **one** plate and **one** card: the fixture became a *run*, not a row of dots (a dozen [E] targets in a room already dotted with table consoles is the crowding #212 was filed about). Two geometry fixes ride with it — the plate (and the square this cheat stands you on) has moved to the middle of the **serving** desk instead of the middle of the counter *band*, which included the goods hoist's twelve du; and **stools 1 and 2 no longer stand in front of the freight shutter** (#792 laid the row from the band's start, twelve du before the bar's own photograph begins — hold the row against the desk picture: every seat is on it now). The keep's side (#781) is the sealed band **behind** the desk and the counter's walls still hold — there is no way to walk round the bar. *(Also a button in the front door's **⚙ DEV START SITES** list — 🍹 “The counter, ready to order”.)* |
| **`?park=1`** | **BOOT THE PARK (#759, RE-SITED #813) — the same B1 route as `?counter=1`, with the last leg walked through a gate instead of to the counter, so the captain starts standing on the gravel INSIDE the park. It is no longer "behind the bar": the Manhattan ruling (#813) makes the green the MIDDLE OF A CITY BLOCK, so the bar is one of the rooms around it rather than the room in front of it. Owner: *"The central park needs to be in the center of all the other rooms… not on the side. Think of New York."* What a tester should see: the floor is GREEN (`art/b1-park-walk.jpg`, laid in panels so nothing is stretched); **turn all the way round and every one of the four walls is somebody's window** — the bar's own pane on the spine side, the back of house's on the far side, and a room's glass at each end, all drawn in the ship's window ink rather than the poured hull line. Walk into any of it and you stop; look through it and there is a room. The ways in and out are **6 GATES** in a fixed pattern — two down through the near band off the main corridor, two up through the far band off the back street, and one through each end of the block — so the green is crossed from all four sides. On the shipped field the box is **203 × 45 du** at `(-106.5, -253.5)`–`(96.5, -208.5)`. Raised beds are solid boxes stencilled `🌱 BED n · <CROP> · TO CANTEEN 1` — the same CANTEEN 1 the counter's own sign says, which is the entire food connection and is never pointed out; the gravel walk bends three times down the room with a steel bench on the outside of every bend; floodlight masts stand along it; one lone figure sits on the furthest bench with nothing to press. The field book files ONE line on the first step in, once per excursion, because the plate at the gate says ATTENDANCE IS RECORDED and that is a sentence rather than a system.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🌳 “The park behind the bar”.)* |
| **`?park=1&spread=1`** | **BOOT ONTO A PARK BENCH WITH THE CASE OUT (#793) — the `?park=1` route with the last leg walked to a FREE bench, the captain already sat down on it through the very `[E]` handler a player presses, and three finds in the sleeve.** Owner, from play in the new park: *"E at a steel bench seats you… a park bench under a painted sky is the best breather in the base"* and *"the park bench might be used to go through inventory info loot, if we get the whole bench to ourselves to have some privacy."* What a tester should see: **the docked strip** (not a card — the park stays green behind it) reading `🪑 A PARK BENCH · … · the walk runs clear both ways — nobody crosses this gravel out of sight`, which is deliberately NOT the hall's crowd figure: you cannot see the hall from here. **Press `[I]`** → 🗂 SPREAD, pick a paper, watch the dig bar — the whole bench is yours, so the case may come out. **Press SIT A WHILE** — sitting still is the gumshoe move, and the beat says whether anything on the walk stopped when you did (today: nothing does, and that answer is honest — the only movers with routes are #804's rounds, which are laid before you arrive and cannot be tails). **Then walk down the curve to the bench with the LONE FIGURE on it** and press `[E]`: you sit on the free end, the strip still docks and the short rest still runs, and `✍ Work the case` refuses out loud — *"Not while you are sharing the bench. Half a bench is not a desk, and the other half can read."* Read the deck while you walk: every bench draws its two ends in #795's own ink — a **warm filled disc** where somebody is, a **hollow grey ring** on an empty plank, and a **green ring** for the free end BESIDE somebody, which is the same invitation mark a free chair at an occupied table wears. *(Also a button in the front door's **⚙ DEV START SITES** list — 🌳🪑 “On a park bench with the case out”.)* |
| **`?spread=1`** | **BOOT THE SEATED SPREAD (#784) — the phase-two loop in thirty seconds.** Owner's own ask: *"we probably need a start point where we have things in our inventory we can process (when our HUD UI state is sitting down with enough privacy)."* Implies the whole `?tablescene=` route and then walks three more legs: it sets the captain down at a **CABINET top** — the private end of the exposure ladder, and the owner's canonical processing venue (*"that is the place I want to process inventory"*) — **sits them down through the same `[E]` handler a captain uses**, and puts **three finds in the sleeve** (two papers and a file on somebody: the only two kinds that have a gist). What a tester should see: **no backdrop and no card**. The seated panel is a **HUD STRIP** docked at the foot of the deck — the hall stays lit, the walkers keep moving, the park stays green — carrying the **customer line** (`🪑 A CABINET TABLE · NO POUR — nothing bought… · REST 0/3 pips · the door is shut — nobody is crossing the room to you`), the last thing the table said, and the seated verbs. Press **`[I]`**: the satchel opens on a third page, **🗂 SPREAD**, which exists only while you are in a seat. Press a paper and the **digging bar** runs over the captain's own mark on the live deck for the same 20 s a photograph takes (`?process=N` tunes it, `?process=0` makes it instant) — the same bar and the same slot, wearing the pen instead of the camera. When it fills, the gist lands in the **detective book** (📓 NOTES) **and the sheet stays in your sleeve**, which is the whole difference from 🫳. Stand up mid-dig and it is abandoned out loud with nothing filed; sit at the counter instead and the spread is refused out loud (the gumshoe rule). Try `/map?spread=1`, and `/map?spread=1&process=3` for a quick one. *(Also a button in the front door's **⚙ DEV START SITES** list — 🗂🪑 "Sat down in a cabinet with the papers out".)* |
| **`?barcase=1`** | **BOOT THE CASE AT A BAR TOP, WITH NO GROUND UNDER YOU (#1016) — the seat the owner filed the bug from.** He sat at a takeable top in **The Stormwatch Bar** aboard The Red Eye (the eighth seat, #973 L5b), pressed **Work the case**, and *nothing happened at all*: *"I do not see the detective book here when I work the case? Some kind of bug?"* Every organ of the dig had been written as a fact about a `SurfaceExcursion`, and a docked berth has none. Owner's ruling: *"Maybe it might be good idea to refactor the working the case etc table options to not be tied to any location? Kind of clean separation from the arriving random encounters that are more place tied events."* The cheat implies `?ashore=1`, walks the last leg to a free top and **sits you down through the same `[E]` handler a player presses**, with **three finds in the sleeve**. What a tester should see: the **docked strip** with `Work the case` on it. Press it → the satchel opens on **🗂 SPREAD** with rows on it (before this issue: nothing, silently). Press `dig it out →` on a paper → the satchel shuts and **the dig bar fills on the strip for the full 20 s** (`?process=N` tunes it) — the strip is the only bar a berth has, because there is no surface HUD off an excursion and a station bar must not grow a motion fan. When it fills: the entry lands in **📓 NOTES** filed under *The Red Eye · The Stormwatch Bar*, reading *"read through **on the haven's deck** and copied out"* (never "on the regolith at your feet"), **the sheet stays in your sleeve**, and the row's verb changes to *already in the book*. Stand up mid-dig and it is abandoned out loud with nothing filed. **The register is the case's now, not the ground's:** it rides the vault, so a sheet dug once is dug for good, wherever you dug it. Try `/map?barcase=1`, `/map?barcase=1&dock=red-eye`, and `/map?barcase=1&process=3` for a quick one. *(Also a button in the front door's **⚙ DEV START SITES** list — 🗂🍸 "Working the case at a bar top, with no ground under you".)* |
| **`?frontdoor=1`** | **BOOT THE CANTEEN'S FRONT DOOR (#775) — the same B1 route as `?counter=1`, stopped one room SHORT: the captain stands OUT ON THE MAIN CORRIDOR, facing the hall's own entrance. Owner, walking the new B1: *"The bar/canteen needs DOORS ON THE MAIN CORRIDOR — today you have to really look for the way in; a venue's entrance should find YOU."* What a tester should see: a violet imported leaf in the spine's own long wall with **🍸 CANTEEN 1 · ENTRANCE** stencilled beside it on the corridor side, placed at the LIFT (the carve puts the first one wherever the walker is, which on most floors is directly across the corridor from the car); walk south/north into it and you are in the bar without ever turning down a rib. Then walk the corridor: a hall of 5 800 – 7 300 du² carries **⇥ EXIT 2 · KEEP CLEAR** and, on the biggest, **EXIT 3** as well — the count is `UndergroundComplex.HallEgressDoors` of the room's own floor area (one per 1 500 du², never fewer than three), so it is derived and not typed. Every one of them is a real gap in a real wall: the guards walk an A\* across each jamb inside a box that leaves no way round.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🚪 “The canteen's front door, from the corridor”.)* |
| **`?freight=1`** | **BOOT THE GOODS HOIST (#775) — the same hall, standing on the hall floor in front of the freight shutter at the end of the counter's own service band.** Owner: *"The facility needs FREIGHT ACCESS somewhere — a freight elevator or a long drive-in ramp for supplies; eighty seats of food and twelve beds of produce do not arrive through a personnel door."* What a tester should see: **🚛 GOODS HOIST 1** painted on the floor in front of a shut roller door in the counter's own line, at the end of the band nearest the park's gate — behind it, through the glass, are the beds it exists to carry. Walk into the car and you stop: it is a sealed twelve-by-five box in the service band, four walls, and the collision field agrees with the drawing. Press `[E]` on the shutter and it says **🔒 GOODS HOIST 1 · DELIVERIES 04:00–06:00 · CREW SIDE ONLY** — the refusal is a sentence rather than an absence, which is the whole of the feature. Nothing simulates freight and nothing pretends to.** *(Also a button in the front door's **⚙ DEV START SITES** list — 🚛 “The goods hoist that will not take you”.)* |
| **`?designate=1`** | **THE WHOLE MANUAL-FIRE LOOP, AT THE SHUTTER IT WAS WRITTEN FOR (#803).** Owner: *"we might want to hand-load them into the bots for some special purposes, like shooting a mechanical lock (we will need to use the handheld captain's control to set the guns / gun to fire at something manually, that UI is missing)."* The `?freight=1` boot with the pieces assembled: you are standing at the **GOODS HOIST**, one sentry is **SET DOWN** beside you reading **05** — one round short of a hasp, deliberately — and **12 loose rounds** are in your pocket, the size of find a hut's ammunition locker deals in. What a tester should do, and see: **(1)** press `I` standing over the bot → the round row now offers it (it did not, before: the satchel would only load a bot reading exactly 00) → the drum goes **05 → 17** and the `🔫 MAGAZINES` line under the tracker says the same number; **(2)** press **📻 Remote** → **🎯 Designate a target** → the gun's own row (unit, drum, what is in it) → the shutter's row (plate, distance, what it costs); **(3)** press it → six flat cracks, the hasp comes off, the shutter's plate changes from 🔒 to 🕳, the wall behind it is gone and you can walk into the car; the drum reads **11**; the handset itself carries the shot's line, what is behind the door, and (once) what the noise actually cost. Then walk to a rib's far end and point the same gun at a **SEALED WAY**: it refuses, in its own words, and spends nothing. *(Also a button in the front door's **⚙ DEV START SITES** list — 🔫🔒 “Hand-load a gun and shoot a lock off”.)* |
| **`?parkwalk=1`** | **BOOT THE CROSSING (#775, WIDENED #813) — THE PARK IS A THOROUGHFARE, not a cul-de-sac.** Stands the captain on the MAIN CORRIDOR at the mouth of a gate down through the block's near band, rather than inside the green, because the crossing is the feature and a crossing has to be started outside. Owner, 2026-08-09: *"let's have multiple doors to the park… it is a kind of place people like to walk through on their way."* #790 shipped it with ONE gate at the end of the hall's rib corridor; #775 made it two to five, depending how the ribs' seeded directions fell. **#813 stops leaving it to chance**: the green is the middle of a block with a street on every side, so a shipped park carries **6 gates** in a fixed pattern — **two down through the near band** off the main corridor, **two up through the far band** off the back street, and **one through each end** of the block. What a tester should see: walk the spine, turn down the gate, cross the gravel, and come out on a DIFFERENT STREET — the west end, the east end, or the back street behind the potting sheds — and the route between two places on B1 goes through the green instead of round it. The bar's own wall is still GLASS and still stops you; it is the one pane in the whole ring with no door in it, and no gate is ever cut through it. *(Also a button in the front door's **⚙ DEV START SITES** list — 🌳🚶 “Straight through the park, and out the other side”.)* |
| **`?parkback=1`** | **BOOT THE FAR SIDE OF THE GREEN (#801, RE-ANCHORED #813) — the same B1 route as `?park=1`, walked one leg further: across the gravel to the wall that used to be the painted horizon.** Owner: *"we could have rooms to explore below the park also (on the map). Walking through the park is fun, it should not be the edge."* What a tester should see: **doors in the far wall**, one per room, four of them on the shipped field, with the glass of each room standing as the two panes either side of its door. Plates beside them, on the park side, in the beds' own register: `🌱 POTTING · SOIL, TRAYS, GRIT`, `🧰 GROUNDS PLANT · LAMPS, FEED, TIMERS`, `❄ COLD ROOM · TO CANTEEN 1`, `🧤 GROUNDS STORE · TOOLS SIGNED OUT AND BACK`, `🚿 WASH-DOWN`, `📋 GROUNDS OFFICE · ROTA POSTED` — the cold room names the same CANTEEN 1 the beds do, and nothing points that out. Walk through one: it is an ordinary room with a `🔦 SEARCH THE ROOM` console in it, on the floor's own room list, and the A\* audit that walks every room from the car walks these. **#813 · then keep going.** These rooms are the block's far band now and there is a **BACK STREET** behind them, so each has a SECOND door on the far side — walk in off the gravel and out onto the street, or the other way round. The guard proves both: it plugs one door at a time and demands the room still be reachable by the other, then plugs both and demands it go dark. *(Also a button in the front door's **⚙ DEV START SITES** list — 🌳🚪 "The far side of the green".)* |
| **`?ringoffice=1`** | **BOOT THE OTHER SIDE OF THE GLASS (#813) — the same B1 route as `?park=1`, with the last leg walked INTO one of the rooms that faces the park, a few paces back from its own window wall.** Owner, deciding the whole carve: *"make sure the park prime real estate is not wasted and not unused, not on any side. It is the best real estate."* Every other park row in this table puts a tester on the GRAVEL, which is the side of the glass the game has always shown; prime real estate is a thing you check by standing in the room that PAID for the view. What a tester should see: you are indoors, in a poured box off a street, and **the whole wall in front of you is a window with the green behind it** — walk into it and you stop, which is the same pane the park side stops you at. The door behind you is on a STREET and never on the park: that is the ruling's third law (*nobody walks through an office to reach an office*), and you can prove it by walking out and round — the spine, the two end streets and the back street make one loop and every room on the ring hangs off it. Read the plate beside the door on your way out: the rooms WITH the view carry their own vocabulary — `REGISTERED OFFICE · GARDEN ASPECT`, `NEGOTIATION ROOM · BOOK AT THE COUNTER`, `SIGNATORY SUITE · TWO KEYS`, `SENIOR ROTA · GREEN SIDE`, `PRIVILEGED RECORDS · READING ROOM`, `RECEPTION · APPOINTMENTS HELD` — while the four CORNER rooms, which stand past the end of the park's wall and have no view at all, keep the corridors' ordinary stencils. That difference is the amenity gradient (#775) drawn on the plan, and nothing ever points it out. On the shipped field there are **14 rooms** on the ring and **10 of them have the view**; the west and east ones' glass runs **vertically**, which is the first vertical glazing in the building. *(Also a button in the front door's **⚙ DEV START SITES** list — 🏢🌳 "Inside an office, with the park out of the window".)* |
| **`?goodscar=1`** | **BOOT THE SECOND CAR (#801) — B1, standing at the OTHER lift.** Owner: *"that elevator would be so busy it would be packed and never available… it is a choke point, and the whole lab would be too easily guarded by just having the guard posted in front of the one elevator."* What a tester should see: an alcove in the **lower** face of the main corridor, at its **blind end** — past the last cross corridor, about 170 du from the cage, which is the length of the building. The console reads **🛗 GOODS CAR 2 · THIS BAND ONLY**. Press `[E]`: the panel opens with a different line under the title — *"The goods car. It runs these floors and it does not climb out: for the surface, and for anything below this band, the cage is at the other end of the corridor."* — and the rows are this band's four floors and **nothing else**: no SURFACE, no sealed row, no card named. Ride it: the doors open on the new floor at THIS car's own doorstep, on the lower face, not at the cage. Then walk the corridor to the cage and time it — that walk is the feature. *(Also a button in the front door's **⚙ DEV START SITES** list — 🛗🛗 "The other car, at the blind end of the corridor".)* |
| **`?rip=1`** | **BOOT THE DISPOSAL LOOP (#798) — rip it and bin it, in thirty seconds.** Owner, live in play: *"Now the option is to photograph the papers and leave them there… I would not like to leave them at table in canteen… we need option to destroy them by ripping and binning etc?"* Implies the whole `?spread=1` route and then walks the last leg somewhere else: to the standing spot the hall's own **SLOP BIN** publishes, on your feet, with **three finds in the sleeve**. What a tester should see: press **`[I]`** and every paper row now carries a **🗑** beside 🫳 and ✍. Press it. The sheet is **torn up and gone from the sleeve** — not dropped, so `TryPickUpWhatYouLeft` cannot hand it back — and the 📓 NOTES page gains a line naming the bucket (*"Tore up … and put it in the slop bin"*). **The book never unlearns:** dig a paper at a table first, then bin it, and the entry you wrote is still there afterwards, red threads and all. **Three bets, one verb:** the slop bin is here, the **WASTE CHUTE** is at the other end of the same room and the tidy **PAPER BIN** is by the lift — every one of them a HANDOVER rather than oblivion (#775: professionals empty every bin in this building), and the game never once says which tier was enough. Walk away from all three and the control refuses out loud, naming what would fix it. Do it where somebody can see you — at the counter, at a table with company, or with a round in the room — and a **second** line is filed: *"Tore a document up and binned it with … watching."* Nothing reacts; somebody simply knows. **#828 · AND THE BIN ITSELF TAKES `[E]`** — owner, from his own table: *"I think the trash could be an e-use … where we select from inventory the processed items we rip and deposit into trash."* The cheat drops you on the slop bin's standing spot, so just **press `[E]` where you land**: the satchel opens on a page titled after the bucket you are at (*🗑 WHAT GOES IN THE SLOP BIN*), papers only, the ones already **in the book** leading and the rest flagged ***not yet worked*** with the warning in the hint — press one anyway and it goes, because it is your paper. Same verb underneath: the filed line and the emptied sleeve are identical to the row-side press, which is the law a test proves by driving both doors. Walk three paces off mid-page and the picker says so rather than posting a sheet into a bucket down the corridor; an empty sleeve gets a polite refusal instead of a blank list. The **🗑 THE BIN** tab appears in the satchel whenever a bucket is in reach, so `[I]` and `[E]` are two grips on one page. Try `/map?rip=1`. *(Also a button in the front door's **⚙ DEV START SITES** list — 🗑📄 "Standing at the bin with the papers still on you".)* |
| **`?threads=1`** | **BOOT THE RED PEN, WITH A CASE ALREADY IN THE BOOK (#741).** Owner, authorising the build: *"I dream of drawing those conspiracy board connecting red lines… I guess it could be a red pen only used to connect the things."* Implies the whole `?spread=1` route — the cabinet, the docked strip, three finds in the sleeve — and adds the one thing the pen cannot work without: **six entries already filed, from two grounds you are not standing on**. It opens the pocket straight onto 📓 NOTES in the 🧵 THE CASE reading. What a tester should see: the notes are now **title-first nodes**, collapsed, each with a caret and either a 🧵 count or the words *loose end*. Press one to fold it open into **bullets** (the full first sentence is the first bullet — the title's clip loses no word). Then press **🖊 THE RED PEN**: pressing a title now means *one end of a line*. Press a second and the line is drawn — a soft chime, a short red connector settling between the two rows, and **the list reorders so the two sit together**. The same two presses take it off again (the eraser end). **The rhyme to spot, and the game never once remarks on it:** the same door — **The Tilt** — is named in entries filed on *two different moons*, once as where a dead specialist's family is still waiting and once as where the phrase that fell out of their kit will open something. Every word of it is shipped dossier prose (#588/#774); nothing is highlighted, nothing is suggested, and **nothing congratulates you when you get it** — that is the register, by ruling. Off your feet the pen refuses out loud, and at the bar desk it refuses for the gumshoe rule, both on the same ladder the spread uses. Try `/map?threads=1`. *(Also a button in the front door's **⚙ DEV START SITES** list — 🖊🧵 "The red pen, and a case to draw on".)* |
| **`?roll=hi` / `?roll=lo`** | **FORCE THE ENCOUNTER BAND (#746). `hi` makes every rolled move land YES, `lo` makes it NO — AND THE SCENE MOVES; `mid` forces YES, BUT. Owner, in the issue: *"testing is a feature."* It overrides the BAND and never the roll — the dice still cast, the named modifier stack still reads truthfully on the panel, and the scene that plays out is the scene a captain would get, because a cheat that showed you a different scene would be worse than no cheat at all. The only rolled move today is THE HAND's ask about work, so `?tablescene=1&roll=lo` is how the refusal's three consequences (the table hardens, the fitter opens, the temp overheard it) get watched on demand. Pair with `&tablescene=1`.** |
| **`?tender=flash`** | **FORCE THE TENDER'S RARE ROLL (#1022).** The galley card is a pop-up now (press `6`, or `[E]` at the deck's CANTINA console) and **B-7V** leads it: his plate, his picture, and one line per beat off Core's authored pools — a greeting the first time you look, something else every time after, a pour line per **Pour a tot**, and at the third tot the one line the set is obliged to say. Rarely (1 in 12, and **never twice in one sitting**) the register changes and he announces the evening of a much grander room, with his own recovery under it. That is the beat worth watching and it is otherwise reachable only by luck, so this forces the **ROLL** and nothing else — `?roll=`'s own philosophy (#746): which line he reaches for is still his own salted pick, what follows it still follows it, and the once-a-sitting law still holds, so what plays out is the beat a captain would get. **What a tester should see:** open the card on `/map?tender=flash` — the announcement is drawn apart from his voice (indented, italic, brass) with the recovery beneath it; shut the card and open it again and he is himself, because the sitting has spent it. A **sitting** lapses on the same 90-second gap the rum ledger starts a fresh tot count on, so leave the counter for a while and he greets you afresh. **Broken looks like:** an announcement with nothing under it; a second one in the same sitting; the third tot answering with an ordinary pour line; or the same sentence twice in two looks. |
| **`?stool=1`** | **BOOT SITTING AT THE COUNTER (#756) — the high chairs the owner asked for.** Owner, live: *"Also there should be high chairs so sitting at the bar desk is also possible."* Implies the whole `?counter=1` route and then walks the last, last leg: the service card is open **and you are up on a stool**. What a tester should see: the plate reads **🪑 STOOL n · THE COUNTER**; the picture on the card is no longer the bar desk but the **window wall and the park behind it** (#759), because standing you look at the counter and seated you look over it; **the menu is still there, unmoved** — that is what "the keep serves you seated" means, and ordering from the stool debits and answers exactly as it does standing. The only new verb is **WAIT**. Getting down leaves you standing at the counter, not out of the bar. Which stool you land on is the room's answer off the frozen watch, never the cheat's — pair with `&watch=N` to sit at a heaving counter or an empty one. **#791 — and the row now stands at the BAR.** It used to be laid from the counter band's own start, so the first two seats sat in front of the goods hoist's shutter twelve du before the desk's photograph begins; the eight are spread across the **serving** desk now. The seat pick-or-default hands you is unchanged (Core's ordinal off the frozen watch) — what changed is where seat *n* is bolted down, so the stool you can see is free is the one you land on and it is at the bar. *(Also a button in the front door's **⚙ DEV START SITES** list.)* |
| **`?neighbour=1` / `?neighbour=0`** | **FORCE WHETHER THE ONE BESIDE YOU TURNS (#756).** `?approach=1`'s sibling at the bar, and it needs a lever even more than the tables do: whether anybody speaks sits behind a seeded roll **and** a seeded occupancy, so a stool with empty seats either side is a silence the dice cannot break however busy the watch is — **proximity IS the invitation** (owner). `1` turns her on the very next wait: she does **not** ask to sit, because she is already sitting, so the ladder opens at a remark thrown sideways — say something back, stand her one off a counter eighteen inches away, then ask what has been bothering her (the one rung the field book keeps). `0` means nobody ever turns, which is the other half of the feature: the counter answers, in words, and which of **three** silences you get depends on whether the seat beside you is empty, taken-and-quiet, or in a hall that has been emptied. `?neighbor=` is accepted too. Try `/map?stool=1&neighbour=1` and `/map?stool=1&neighbour=0`. *(Both are buttons in the front door's **⚙ DEV START SITES** list.)* |
| **`?tablescene=free`** | **BOOT STANDING AT A TABLE WITH NOBODY AT IT (#757) — the table the owner could not sit down at.** Owner, live in the hall: *"I have empty table but I cannot sit down"*, and, minutes later, *"the normal way to operate in a bar or restaurant is still not implemented."* Same route as `?tablescene=1` (it implies `?secretlab=deep&land=1&floor=1`) with a different last step: it sets the captain down at a **FREE top** — one of the room's own, taken off the same `CanteenRegulars.Tables` call the deck was drawn with, never a coordinate the cheat typed. The top is plated **🪑 A FREE TABLE — SIT DOWN**, and pressing `[E]` does exactly that: the panel's **first line confirms it** (*"You sit down. The table is yours."*) before it offers you a single verb. Then **SIT A WHILE — see who comes**, which is the seated state's whole verb — sitting down alone is a choice to be *findable*, and sitting a while asks the room whether it has anything for you. **Stand up** ends it. One press sits you down, the seat is where you stood, and there is no chair menu. <br><br>**#783 · The panel wears the table, in whichever of its two states you are in.** On a busy watch you get the wary line (back to the wall) over `art/b1-your-own-table.jpg` — the empty chair opposite, which *is* the wait beat. On a **quiet** watch, or with a **drink bought at the counter still in your hand**, the same sit is a SHORT REST: the relaxation line (boots up on the spare chair) over `art/b1-short-rest.jpg`, and standing up says something different too. Try `&watch=5` for the rest and `&watch=2` for the watch; buy a pour at the counter first (`?counter=1`) and even the heaving watch turns into a rest. Pair with `&approach=1` (below) to make somebody actually come, and with `&watch=N` to choose how full the hall is. |
| **`?approach=1` / `?approach=0`** | **FORCE WHETHER ANYBODY CROSSES THE ROOM (#757).** Whether a wait at a table you took alone brings somebody over is a seeded roll on (site, floor, top, watch, beat) scaled by how full the hall is that shift — so **both halves of the feature are otherwise reachable only by luck**, and #693's rule applies: *a scene nobody can reach on demand is a scene that ships broken.* `1` brings her over on the very next **SIT A WHILE**: she asks for the chair, offers to buy the round, and only then says what she came over for — the three-rung ladder (owner: *"think Gandalf knocking on Bilbo's door"*). `0` means **nobody ever comes**, which is not the absence of the feature but the other half of it: the hall answers, in words, that nothing is going to happen, and on the small watch an eighty-seat room that used to be loud saying nothing IS the beat. It forces **whether** and never **who** or **what** — her plate, her ladder and her ask are the ones a captain would get. Try `/map?tablescene=free&approach=1` and `/map?tablescene=free&watch=5&approach=0`. *(Both are buttons in the front door's **⚙ DEV START SITES** list — 🪑 “A table with nobody at it — sit down, and sit a while” and 🪑🕳 “…and the watch where nobody comes”, which on watch 5 is also the **short rest** state.)* |
| **`?tablescene=free&nerve=low&hurt=3`** | **THE SHORT REST, WATCHABLE (#784) — the same free table, with a captain who has something to get back.** Owner: *"Sitting down relaxes and heals"* / *"it is like short rest in TTRPG."* Take the table and press **WAIT**: the avatar on the deck is drawn SITTING (a chair back, folded body, arms on the table — no heading spoke), and each beat eases a whole nerve pip and, on the third, knits one of the five blows back. The panel says it after the room's own silence line, and the nerve ledger names it. Then keep pressing: the ceiling bites at `ShortRest.NervePipCapPerWatch` and the game tells you so — *a short rest is short*, and the rest of you comes back in a bunk. Add `&counter=1` first and buy a pour: the same rest lands in half the beats (the pour multiplies the RATE, never the ceiling). And press **W** while seated: the captain does not move — you are asked whether to stand up, `Esc` keeps your seat. |
| **`?patrol=1` / `?patrol=2`** | **BOOT ONTO A FLOOR WITH A ROUND ON IT (#804) — B2 of a deep site, which is the first floor under the bar and therefore the first with a security ROTA walking it. Owner: *"the rotating guards on the lower more restricted levels… ideally we could see them move and wait for them to pass before we pass them."* Implies the whole route (`?secretlab=deep&land=1&floor=2`); `2` forces the two-guard watch, which is otherwise a coin flip and is the harder scene to time. It forces nothing else — which stops the round walks, which direction it runs and who is on it are whatever the watch says, because a cheat that pinned the beat would be testing a floor that does not ship.**<br><br>**WHAT A TESTER SHOULD SEE, in this order, standing at the car and doing nothing.** First the **motion fan**: a smudged return with a bearing, moving, well before anything is on the deck — that is a guard heard through poured wall at #591's degraded reach, and it is the owner's *"we need our motion detector to warn us… before they spot us"* working with no new instrument code. Then, if they come closer without a clear line to you, one line: **👣 *Boots on shotcrete, out of sight and in no hurry…*** Then and only then a **green mark** with `PATROL 1` over it, drawn the instant your own line of sight reaches them and gone the instant a wall gets between you. **They stand five seconds at every stop and walk 3.2 du/s between them — that gap is the whole game.** Walk down a rib behind one, wait at a mouth for one to pass, and come back on the next watch to find the same stops walked in a different order.<br><br>**BROKEN LOOKS LIKE:** a mark that stays on the deck when you step behind a wall (the gate is off); a mark that appears at the far end of a floor before the fan has said anything (the eye's reach and the fan's have swapped); a guard who challenges you the moment you can see them (the two reaches have collapsed into one, and there is nothing to time); a round that walks into a wall and stops (a stop that is not on the A\* — the client sweep is the guard for that); or a guard drawn on a floor of the Hive wearing `SWEEP-1` (the sweep team's band leaking into the underground deck). Pair with `&watch=N` to change the round; the floor can be crossed by CLICKING it rather than with WASD, on this boot and every other (#875). *(Also a button in the front door's **⚙ DEV START SITES** list — 👮🚶 “B2 — somebody is walking the floor”.)* |
| **`?badge=1`** | **MINT THIS SITE'S OWN PASS AND PUT YOU IN FRONT OF SOMEBODY WHO READS IT (#804).** Implies `?patrol=1`'s whole route, because a pass with nobody to show it to is not a thing anybody can test. Minting is the ONLY thing it does: the guard still has to see you, the wallet is still read by Core, and what is said is what would have been said had the pass been earned. **Earned, it comes off the cage crew.** The Hand at a B1 table hands you a day-labour chit (#746), the chit opens the gate to the band below (#752), and the site does what a site does with a body that has arrived on somebody's account — it puts you on its books at the bottom of the cage. **The gig does not pay in coin; it pays in paper.**<br><br>**WHAT A TESTER SHOULD SEE.** 🎒 `I` shows **🪪 SITE PASS · GENERAL HANDS · <SITE> SITE** in the wallet. Walk straight at a round: the card **👮 THE ROUND STOPS AT YOU** goes up, and its amber row says he reads the face, the site code and the tier, hands it back, and mentions the wet floor round the corner. Close it and the round picks up where it left off. Now try the other three answers — boot `?patrol=1` with nothing (*"Nothing comes out of your wallet that this floor has ever heard of"*), with only the chit (*"That's for the cage. This isn't the cage."*), or take a pass to a different rock (it names the other SITE, the #679 ladder one building along). **All three end the same way: he walks you back to the car, nothing is taken, nobody is called, and a line goes into a book.** There is no chase in this feature and there is nowhere for one to start.<br><br>**BROKEN LOOKS LIKE:** a challenge that ends in anything but a walk back to the lift; a refusal that does not say WHY; the escort line pulsing to the HUD *behind* the card's own backdrop (#736's law — the sentence you act on rides the card); or a pass that works on a rock it was not issued for. *(Also a button in the front door's **⚙ DEV START SITES** list — 🪪 “…and the same floor with the site's own pass in your wallet”.)* |
| **`?watch=N`** | **PIN WHICH SHIFT THE HALL IS ON (#751). The B1 cantina hall holds eighty and how many of its twenty tables are taken varies BY WATCH — a heaving day watch, a small-hours watch of a dozen souls — and **nothing in the game announces which one you walked into**: that is the design, and it is exactly the kind of design a tester cannot see without waiting four sim-hours between looks. A watch is four sim-hours (`PatronRota.WatchSeconds`) and six of them are a day, so `?watch=2` is the middle of the day and `?watch=5` is the small hours; compare the two and the whole feature is on the screen. Owner, twice over: *"testing is a feature."* It pins the watch INDEX and nothing else — who is in the room and where they sat are still the rota's own answer for that shift (#709), so what you walk into is the room a captain would get, never a rigged one. Pair with `&tablescene=1`.** |
| **`?perf=1`** | **ARM THE DRAW-COST PROBE (#841, Lab 46) — the one measurement this repo has never had.** Lab 45 priced the SIM side of #841 to the microsecond and closed on the half it could not reach: *"if culling is worth doing it has to be justified on DRAW cost, and this lab could not measure draw cost."* There is no headless path to a canvas, and a timing taken from an MCP-driven tab is invalid here by standing law (the tab is `document.hidden`, so rAF is throttled and the timers are clamped). So the game carries its own stopwatch. `?perf=1` puts a clock on the walked view: **the whole `DrawWalkFrame`**, the **17 passes** of the pen's conductor by name (`PaintTheGround`, `FillTheFurniture`, `DrawTheWalls`, `DrawTheSeats`, `DrawTheConsoles`, …), and the **flush across to the canvas** (`CanvasRenderer.EndFrame`, the one line of the frame that reaches JavaScript — everything above it only fills an array). It keeps a rolling 120-frame window and reports it twice: a **line across the top of the deck** (mean / p95 / max, the furniture's share, the three dearest passes), refreshed four times a second, and the **same table printed to the browser console every 120 frames** in a fixed greppable shape — `[perf] pass=<name> mean=… p95=… max=…` — which is what you copy into `labs/46-what-a-draw-costs/README.md`. It changes nothing about the world and costs nothing when it is off (one null check per pass; the probe object does not exist). **Read it in a REAL FOREGROUND TAB, on the machine and at the window size you care about** — that is the whole reason it exists, and a number read anywhere else has to be disowned in the same breath it is quoted. Try `/map?secretlab=deep&land=1&floor=1&perf=1`, then walk from the park into a bare corridor and watch which row moves. *(Also a button in the front door's **⚙ DEV START SITES** list — ⏱ "What a draw costs — the furnished floor, timed".)* |

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

**And the disclosure clock: what a tester should see is NOTHING.** Crossing the seam starts `DisclosureClock`
for that ground (#677's second mechanic), and the mechanic's own law is that it is *never a progress bar and
never announced* — so there is no line, no counter, no glyph, no card and nothing on the HUD to look for, and
a build that grew one has a bug. The only place it is visible is the save file: open the vault JSON and the
`progress` section carries `hallsOpened`, one row per ground, each with the world-side window the seam was
first crossed in. Re-enter the same galleries and the row does not move — the FIRST crossing is the one kept,
because a clock you can restart by going back again is a farm. **#1063 is the first beat that reads it** (see
below); #1068 and #1074 are still to come, and each authors its own words when it is built.

### The ground that was filled in while you were away — `?buried=1` (#1063)

```
/map?buried=1&land=1                the same rock as ?found=1, one shift later
/map?found=1&land=1                 …and the same ground before the job, to compare
```

`?buried=1` is `?found=1` with the ground already **opened a whole world window ago**, so the burial fires on
the way down. Since #1074 it also picks WHICH window, because the window decides which of the two outcomes
this ground gets — see `?stopped=1` below for the other one and you land on a site whose galleries have been filled, floored and resurfaced. It seeds the
disclosure clock's register and nothing else — the fill itself runs through the ordinary `Burial.Fill` on the
ordinary descent, because a cheat that wrote a filled ground straight into the register would be testing a
code path the game does not have.

**What a tester should see, and every bit of it is deniable:**

- **the lift panel has no button past the listed bottom.** Ride down: the building stops where the building
  says it stops. There is no gap, no greyed row and no sentence about it, and `&floor=17` now sets you down
  on the nearest floor that exists.
- **on the listed bottom, a short recess off the main corridor** — five du deep, at the blind end of the
  spine — with a single door across the back of it. It is drawn in a **flat mid-grey that belongs to no
  palette**, heavier than any wall beside it (the found band's own idiom, §13.20), and it **does not open**
  and says nothing at all. That is the specimen, and it is the only segment on any listed floor in the game
  drawn that way.
- **in the upper canteen, a mason at a table.** Press E: *"pre-existing masonry, origin undetermined."* That
  is the whole testimony. On `?found=1` — the works are on and the job is not done — the board also carries
  **"Resurfacing of the lower galleries begins Monday. Please use the upper walks."**; on `?buried=1` that
  notice has come down, because the job is finished.
- **search the first room on that floor**: the maintenance ledger — *"Sub-level access no longer required.
  Filled and remediated per instruction."* Then count the instruction numbers in it. There are none.
- **the wire has one cheerful line about drainage**, once, and ✂ CLIP files it under the site's own operator.

**And what a tester must NOT see:** any card, pulse, beat, nerve shock, HUD marker, stat, sensor return or
sentence anywhere saying that a ground was buried, that anything was hidden, or who did it. A build that grew
one has a bug. The field book is the only witness, and **nothing the burial does may remove or change one
entry in it** — a note, a clipped story, a red thread or a satchel row taken out of those galleries before the
fill still reads exactly as it read. In the save file the `progress` section grows `hallsBuried` beside
`hallsOpened`, and that is the only place any of it is written down.

### The working that was closed while you were away — `?stopped=1` (#1074)

```
/map?stopped=1&land=1               the same rock as ?found=1, closed by order
/map?buried=1&land=1                …and the other thing that can happen to it
/map?found=1&land=1                 …and the same ground before either
```

`?stopped=1` is `?buried=1`'s twin. #1063's burial and #1074's stop order are **one trigger's two outcomes** —
an opened found band, one whole world window, the captain off the body — and a ground gets one of them,
decided by a coin seeded on the ground and the window it was opened in. So this cheat parks the ROCK *and* the
WINDOW: it walks back a shift at a time until the split hands this ground to the office, and then gets out of
the way. The closure runs through the ordinary `StopOrder.Note` on the ordinary descent.

**What a tester should see, and every bit of it is deniable:**

- **nothing is filled in.** The galleries are exactly where you left them: the site's true depth is unchanged,
  every gallery still reads as a gallery, and the field book still agrees with the ground. This is the whole
  difference from `?buried=1` — the town forgot; the office remembered on purpose.
- **the lift panel has no button past the listed bottom**, and no row refusing one either. There is no gap, no
  greyed row and no sentence about it. The building does not admit that band exists, so it does not name it
  even to say no.
- **on the listed bottom, a short recess off the main corridor** — five du deep, at the blind end of the spine,
  the same pocket `?buried=1` keeps its specimen in (a ground is stopped or buried and never both). Across
  the back of it is one leaf that does not open, wearing **`AUTHORITY — WORKING CLOSED`**. Press E:
  *"By order of the Authority this working is closed pending structural review. No schedule for the review is
  published."* A stamp and no signature. Try the satchel on it and there is no reader; point a sentry at it
  and there is no hasp.
- **search the SECOND room on that floor**: the plant's valve-book, three entries on one paper. Read the
  instruction numbers down the page — **2231**, then nothing, then **2233** — and the line between them says
  *per order* where both the others say *per instruction*. Nobody writes down that a number is missing.
- **in the upper canteen, the week's rota is still up**, listing the shift. The resurfacing notice is not: a
  notice about a job somebody was going to do comes down when nobody is going to do it.
- **and pinned beside the rota, the personnel register** (#1074 beat 4): `REGISTER — PERSONNEL` — a name, and
  then *"Reassigned where their skills are most needed."* No destination, no date, no signature. Press `[E]`
  on the board until it comes round; it is one of the four slots and never a fifth.
- **two of the people in that room are off that shift.** Stop at their tables:
  - one says *"Transferred, I think. Administration would know where."* — and means it, which is the whole
    of him. He is not covering and he is not frightened; that is what he was told and what he believes.
  - the other has **a mug on the shelf behind her chair** (🍺, the canteen's own glass, drawn and never
    pressable). Ask her and she says *"That stays where it is."* Ask again and you get her plate and nothing
    else, which is the room's ordinary once-per-person law doing the changing of the subject.
  - the mason from `?buried=1` is **not** in this room. A ground is stopped or buried and never both.

**And what a tester must NOT see:** any card, pulse, beat, nerve shock, HUD marker, stat, sensor return or
sentence anywhere saying that a working was stopped, that anything is being kept from anybody, or who
ordered it — and no name, anywhere, on anything except the register row's own hand. Nobody says *missing*,
nobody says *dead*, nobody names the working, and none of those three sentences mentions the Authority: the
enforcer is an office, it signs orders and fences, and it has no business in a canteen. Nothing explains the
mug, ever. In the save file the `progress` section grows `hallsStopped` beside `hallsBuried`, and that is the
only place any of it is written down — the register row, the two regulars and the mug are read straight off
it and keep no state of their own.

### The site that passed into official care — `?preserved=1` (#1074 beat 2)

```
/map?preserved=1&land=1             the same rock, one shift further along
/map?stopped=1&land=1               …and the same ground on the shift the order landed
/map?found=1&land=1                 …and the same ground before any of it
```

`?preserved=1` is `?stopped=1` one shift further along: the same rock, the same ground handed to the Authority
by the split, opened **two** whole world windows ago instead of one. Both stages of the office's paperwork
therefore fire on the way down — the working is closed, and then the closed working passes into care. The
order closed it *pending structural review*, with no schedule published; a window later there is still no
schedule, because the review that was never scheduled has become a study that never ends, and a study needs a
fence around it. Nobody decided anything in between.

**Everything new is on the SURFACE. Nothing below ground has changed at all** — the halls are still there,
the shaft under the listed bottom is still sealed by the order, the plate still reads
`AUTHORITY — WORKING CLOSED`, and every line of the `?stopped=1` walkthrough above still holds.

**What a tester should see:**

- **walk down the field to the survey shed the lift comes up in** (the camouflaged head with the two machined
  doors). It is inside a **rail**: a small closed ring of ordinary low wall, drawn in the same dim inner-line
  ink as every fallen span on the site — not the ship's bright hull stroke, and not a rectangle.
- **exactly ONE gap in it, and the gap faces the tube you walked out of.** Stand at the shed and look back
  toward the way home: the gate is on that side. This is a law and not a seed — ride the car up out of the
  halls and you are set down INSIDE the ring, and you must never have to walk round it to reach your own boat.
- **at the gap, on the regolith, one line:**
  *`AUTHORITY — THIS SITE IS PRESERVED. Its significance is under study.`* It stands a pace outside the rail,
  on the approach, so you read it on the way in.

**And what a tester must NOT see:** a second gap; a gap facing anywhere but the tube; a fence you have to walk
round to get home; a date, a department, a reference number or a signature anywhere on the notice; any card,
pulse, beat, nerve shock, HUD marker, stat or line on the wire about it; or any change at all below ground.
In the save file the `progress` section grows `hallsPreserved` beside `hallsStopped`, and nothing ever takes an
id back out of it — the study does not end.

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
/map?death=void                                    twenty days adrift ran out (#638) — her own deck only
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
character.

**#638 · `?death=void` takes a real lane now.** It reported "no lane at all yet" from #636 until 2026-09-01,
which is what issue #638 was filed about: a cause with a painting, three lines of prose and a headline that
nothing in the client could ever set. The lane is a CLOCK (`Core/VoidRule.cs`) — she is ADRIFT when reaction
mass is zero, no burn or arrival step of the plan can still fire, and the plotted course reaches no haven's
capture; twenty consecutive sim-days of that and the void has her. The dev door skips the twenty days and
calls the very method the watch calls, so what a tester sees is what a captain sees.

*What a tester should see* on `/map?death=void`: the sepia what-happened card carrying **`death-void.jpg`**
(a captain adrift, tether parted, the sail receding — the picture nobody had ever seen in play), the headline
`WHAT HAPPENED — lost to the void`, the caption *"…nothing hunted you. You simply could not get home."*, and
one of the three `VoidLines` under it. It is legal on **her own deck only** (`CanHappen`, #636), so adding
`&land=1` gets you the law substituting a ground death and saying so in the DEV pulse — that refusal is
correct, not a bug.

*Playing the whole clock* rather than staging its end: go dry with nowhere to go and decline the tow. The
three tellings are a banner + log line on day 0 (*"Reaction mass is spent. Nothing answers the helm."*), a
second banner on day 10 (*"Half the ledger gone…"*), and a story pop-up card on day 19 (*"The long dark has a
schedule now. One day remains on it."*). Refuelling, or the course falling into any haven's capture, cancels
the clock without ceremony — no banner, no card, it simply stops. The twenty is
`VoidRule.DaysAdrift`, flagged as the owner's dial; the two telling days are derived from it.

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

> **The house rule this cheat exists for, written beside the others in `Map.Sim.World.Query.cs`** (#870 split
> the boot out of `Map.Sim.cs`)**:** *"a scene nobody can
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
| `/map?oldcrew=1` | **#973 L5a · THE OLD CREW.** Ashore at the bar with the four shipmates this universe cast working THIS berth, and one captain already in the ground. Open the barkeep's counter: each of them carries their bond on a line above the buttons (*the best friend · now with Ilse*) — read it BEFORE you knock, which is the whole of the Fail Forward adoption. One of them is looking at your face instead of at the glass: press **🪞 … is looking at you** for the scene, answer with one of the three, and check the **Captain desk → Ledger → ⚖ Crossings** for the row it wrote. Then stand somebody a drink and read the dice line: *shared history* +1, and −1 apiece if the signer or the fling is at this berth. It grants nothing — every word is played. Override the berth with `&dock=<haven-id>`. |
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

Some hulls hide a space that is not on the deck plan. **Her plating is honest and her paperwork is not** — and
since slice 3 there are **three papers a lie can sit in, and a hull lies in exactly one**:

| read it at | what does not add up |
|---|---|
| 📦 **the cargo manifest** | one section of her shielding booked at a third of what every other section holds |
| 🪧 **the placard at the lock** | the builder's frame numbering steps over a run of frames it never writes down |
| ⚙ **the dead bridge panel** | the board is dead except one breaker, warm, on a bus to a section nobody uses |

Each is deniable on its own, and **every one dead-ends on a clean hull** — and on a lying hull the two she does
NOT lie in dead-end as well. So reading one document is not a search: read the manifest on a hull whose lie is in
her frame numbering and you are told, truthfully, that her shielding books out, and you learn nothing. A document
that only speaks up when there is something to find is a pointer rather than a clue.

Then **`K` to knock**, standing still. Two gears, chosen on the remote:

| gear | seconds | reach | heard |
|---|---|---|---|
| 📡 sounder | 5 | 4 du | 26 du — as loud as running a pump |
| ✊ knuckles | 12 | 2 du | 13 du — as loud as dogging a hatch by hand |

Moving abandons the reading and does **not** refund the noise. Three answers: `SOLID`, `ODD` (near, not here), and
`HOLLOW` — which puts a **FALSE PLATE** on the deck.

**Then you need the rig.** Forcing the plate costs a **🔥 HULL CUTTER** — 240 cr over the same back counter that
sells the SDR scanner, three cuts to a cell, bulky enough to cost a pocket space. `E` at the plate is a **9 s
channel** that dies if you step off it and is loud at the start, and it spends one cut; the last cut leaves the
rig in the hole. The satchel row prints what is left in the cell.

**And then you can get IN.** A void in the **shielding band** is walkable once cut: `E` again and the captain
folds in and pulls the plate to behind him. The deck draws the pocket as space (only that stretch of the band —
the rest stays hatched), the header says *A SPACE NOBODY DREW*, and `E` pushes the plate off again to get out. A
void inside a **bulkhead run** is a hand's width of pipework and refuses — what is in a void decides where it
can be, so a rack of keys fits a bulkhead and a folded gun mount needs the band.

**What a sweep team (#538) finds.** With the plate fitted, the pressure hull is a wall and they genuinely cannot
see you — walls are law for everyone (#324). Three things still give you away, all deterministic:

1. **You are not in it** — standing in a corridor is exactly as fatal as it always was.
2. **They watched you get in** — a lamp on the plate as it closes. Wait for the cone to pass.
3. **The cut is still warm** — for **40 s** after the rig goes through, a lamp landing on the plate opens it.
   The clock strip shows it counting down (`🔥 WARM CUT · the cut is still bright`). Cut early and let it cool.

Making a racket is deliberately not a fourth tell: noise already walks them to the place, which puts a lamp on
the plate, which asks the warm-cut question. The best outcome in the game is the one you hear from inside —
*boots on the deck plating, and then the sound goes forward, unhurried, and keeps going.*

**What to check.** The clock strip shows the knock, the cut, the warm-cut countdown and, once the right paper is
read, the band to search. About one hull in five hides something (Lab 44 probe F prints which of the ten seeded
causes lie, and where). The false plate must **survive a deck rebuild** — dog a hatch or run a pump after finding
it and it is still there.

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
dotnet run --project labs/48-a-lab-about-the-lab/Lab48.csproj -c Release
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
number was read out of the real generator with Lab 48 rather than assumed — and the test plan is
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
/map?park=1                            …and THROUGH the glass: the PARK (#759/#813) — the middle of the block, glass on all four sides
/map?frontdoor=1                       …the way IN (#775): out on the main corridor at the canteen's own front door
/map?freight=1                         …and where the food comes in: the GOODS HOIST, shut, and it says so
/map?designate=1                       …and the same shutter with a gun set down beside you: hand-load it, point it, fire
/map?patrol=2                          B2  — a ROUND on it (#804): watch the fan, then the boots, then the mark
/map?badge=1                           …the same round with the site's own pass already in your wallet
/map?parkback=1                        …and the FAR side of the green (#801): the horizon is a row of doors
/map?ringoffice=1                      …and the OTHER side of the glass (#813): inside a room that faces the green
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

---

## Appendix B — the pin ledgers, and the one sanctioned way to re-pin (#1055)

Four snapshot guards in `tests/SpaceSails.Client.Tests` hold the game still by pinning what it
measured on the old code. Three of them keep those pins in a **machine-written ledger** under
`tests/SpaceSails.Client.Tests/Ledgers/`:

| Ledger | Guard | What it pins |
| --- | --- | --- |
| `FrameHashes.ledger.txt` | `EveryFrameHashesTheSameTests` | 33 frames × (`calls`, `sha256`) — every mark `DeckView.Draw` lays, in order |
| `Fingerprints.ledger.txt` | `EveryFrameLeavesTheSameFingerprintTests` | 30 scenes (six worlds × five input sequences) × 44 probes, plus a `sweep roster` row per swept field of `Pages.Map` |
| `SeatFingerprints.ledger.txt` | `EverySeatTheCaptainTakesFingerprintsTheSameTests` | 16 sittings × (`chars`, `sha256`) |

**The format.** One row per (probe, scene):

```
<probe> | <scene> | <value>
```

A *probe* is one thing measured (`calls`, `sweep`, `walked-view pen`, `the accumulator`); a *scene*
is the world it was measured in (`ship · under way`, `TheRegolithOnFoot.AHeldKey`, `a park bench`).
Rows are **grouped by probe**, because a re-pin is almost never "one scene moved" — it is one probe
moving across many scenes. #1054 moved `sweep` on all thirty; #1040 moved `walked-view pen` on
fifteen. Probe-major puts each of those in one contiguous block, so two lanes moving two different
probes edit two blocks a hundred lines apart and git merges them without a word.

**Nobody edits a ledger by hand.** `ThePinsAreRewrittenOnlyWhenAskedTests.EveryLedgerIsTheFileTheWriterWouldHaveWritten`
re-renders each committed file from its own rows and demands it come back byte for byte.

### Re-pinning

When a change legitimately moves a pinned number, run the measurement — never a text editor:

```bash
SPACESAILS_REPIN=1 dotnet test tests/SpaceSails.Client.Tests -c Release \
  --filter FullyQualifiedName~ThePinsAreRewrittenOnlyWhenAsked \
  --logger "console;verbosity=detailed"
```

PowerShell:

```powershell
$env:SPACESAILS_REPIN = "1"
dotnet test tests/SpaceSails.Client.Tests -c Release `
  --filter FullyQualifiedName~ThePinsAreRewrittenOnlyWhenAsked `
  --logger "console;verbosity=detailed"
Remove-Item Env:\SPACESAILS_REPIN
```

The `--logger` is not decoration: `dotnet test` swallows the output of a *passing* test, and the
printed report **is** the deliverable. It names every row that moved, old → new, the delta per row,
and — for the field sweep — the NAME of the field that appeared or disappeared:

```
── Fingerprints ─────────────────────────────────────────────
  2035 row(s) measured, 2034 pinned; 55 moved, 1 new, 0 gone.
  probes touched: sweep, sweep roster, walked-view pen
  sweep | HerOwnDeckInFlight.SteadyFrames   Δ +1   ← sweep +1: _navHelpOpen
      was: 744 fields, sha256 …
      now: 745 fields, sha256 …
  + sweep roster | _navHelpOpen | Boolean
```

**Paste that report into the PR body.** A re-pin is reviewed by its report, not by squinting at a
table of hex.

### CI never re-pins

`PinLedger.Write` throws unless `SPACESAILS_REPIN` reads exactly `1`, and
`ThePinsAreRewrittenOnlyWhenAskedTests.TheOptInIsOffUntilSomebodyTurnsItOn` proves it throws — on
every CI build, with each ledger's bytes read before and after the refused call. A normal run only
ever **compares**: a guard goes red and stays red until a human has looked at what moved.

### When the sweep moves but the roster does not

A `sweep` row can move with no field joining or leaving the page — a field's *value* changed
(#953, #957). The red says so, and the way to name it is still the dump hook:

```bash
SPACESAILS_SWEEP_DUMP=<dir> dotnet test tests/SpaceSails.Client.Tests -c Release \
  --filter FullyQualifiedName~EveryFrameLeavesTheSameFingerprint
```

Run it once on the base and once on your lane, then diff the two directories.

> `EveryRoundFingerprintsTheSameTests` still keeps its pins in source. It is the fourth snapshot
> guard and the next candidate for a ledger; nothing about #1055 changes what it measures.

---

## Appendix C — the fast run and the full run (#251, item 4)

The suite is not slow. A small, nameable set of gates inside it is slow, and until #251 everybody
paid for them on every red-proof cycle. Measured over the whole solution on 2026-09-02 at
`e7c1915` — **5,759 tests, 3,552 s of test time, 551 test classes**:

| per-test wall time | tests | share of tests | share of the clock |
| --- | ---: | ---: | ---: |
| under 1 ms | 2,886 | 50.1% | 0.0% |
| 1–10 ms | 1,324 | 23.0% | 0.1% |
| 10–100 ms | 733 | 12.7% | 0.7% |
| 0.1–1 s | 524 | 9.1% | 5.0% |
| 1–5 s | 171 | 3.0% | 13.3% |
| 5–15 s | 79 | 1.4% | 19.0% |
| 15 s and up | **42** | **0.7%** | **61.9%** |

Half the suite finishes in under a millisecond and costs nothing at all. Forty-two tests hold
five-eighths of the clock.

### The cut: ten seconds of CLASS total

The unit is the **class**, not the test, because the class is the unit xUnit schedules — it
parallelises across test classes and serialises within one. That is not theory. In the baseline run
each assembly's wall clock *was* its single slowest class:

| assembly | wall clock | its slowest class | that class alone |
| --- | ---: | --- | ---: |
| `SpaceSails.Core.Tests` | 5 m 51 s | `ZubrinTrafficTests` | 349 s |
| `SpaceSails.Client.Tests` | 5 m 1 s | `EveryDeskBootsTests` | 300 s |

Tagging half of a slow class would leave the other half holding the floor, so a class carries the
mark or it does not. **64 classes** cost ten seconds or more: 21 in Core, 43 in the Client. Between
them they are **733 tests — 12.7% of the suite — and 93.0% of its measured seconds.** (63 of them
were measured in the 2026-09-02 baseline; the 64th, `TheWorldBuildersAreThreadSafeTests`, is #1108's
concurrency guard, measured at 15 s on 2026-09-04.)

Ten is a budget, not a discovered boundary: the class-total distribution is a continuum here, with
the nearest class above the line at 10.5 s and the nearest below it at 9.8 s. It is chosen because
it puts the fast run's own floor — the slowest class it still runs — at about ten seconds, which is
roughly where the test host's own start-up begins to dominate anyway.

### The invocations

```bash
# FAST — the inner loop. Everything except the slow gates.
dotnet test SpaceSails.slnx -c Release --filter "speed!=slow"

# FULL — the contract. Exactly what CI runs; nothing is filtered.
dotnet test SpaceSails.slnx -c Release

# ONLY the slow gates — for when you touched one of them.
dotnet test SpaceSails.slnx -c Release --filter "speed=slow"

# One suite at a time, and with your own filter ANDed on.
dotnet test tests/SpaceSails.Core.Tests -c Release --filter "speed!=slow"
dotnet test tests/SpaceSails.Core.Tests -c Release --filter "(speed!=slow)&(FullyQualifiedName~Airlock)"
```

PowerShell has a wrapper that prints the command it is about to run and what the fast run cannot
tell you:

```powershell
./test-fast.ps1              # the fast run
./test-fast.ps1 -Full        # the full run, same as CI
./test-fast.ps1 -Slow        # only the gates on the roster
./test-fast.ps1 -Core        # one suite; -Client for the other
./test-fast.ps1 -Trx         # also write .trx, so you can re-measure the class totals
```

Measured on the same box, same build, back to back:

| run | invocation | wall clock | tests |
| --- | --- | ---: | ---: |
| FULL, before #251 | (no filter) | **6 m 0 s** | 5,759, all green |
| FULL, after #251 | (no filter) | **5 m 27 s** | 5,767, all green |
| FAST | `--filter "speed!=slow"` | **38 s** | 5,035, all green |
| the gates alone | `--filter "speed=slow"` | **5 m 55 s** | 732, all green |

The two full runs are the zero-change proof: same tests, all green, the eight extra being this
lane's own roster guards and nothing else (Core 4,178 -> 4,182; Client 1,581 -> 1,585). The fast run
is the win — **9.5x**, six minutes down to thirty-eight seconds.

The fourth row is the cut's own receipt. The 732 tagged tests take 5 m 55 s *by themselves*, which
is the whole of the original six-minute run; the other 5,035 tests are very nearly free. That is the
finding in one line: the suite was never slow, sixty-three classes were.

### What the fast run does not tell you

This matters more than the number. A green fast run means **the rules still hold.** It does not mean
the ship still flies, the floors are still walkable, or the boot still builds the world it always
built — those are exactly the tests it skipped. What it leaves out, by family:

- **The N-body and long-flight gates** — `Lab20LongGoodbyeTests`, `SimulatorTests`, `LongHaulTests`,
  `TheCyclerArrivalIsAKeptCoOrbitalTests`, `TheParkedShipIsNotRunDownByTheMoonTests`,
  `TheAutopilotFliesAtATenthTests`, `EveryLaneItLaysHashesTheSameTests`.
- **The traffic and surface generators** — `ZubrinTrafficTests` (349 s on its own),
  `EncounterRuleTests`, `SurfaceReachabilityTests`, `SurfaceStructureTests`,
  `OneCounterAndOnlyOneTests`, `TrafficAndPredictionTests`, `OuterReachesTests`, `ArchiveNodeTests`,
  `TheRefugesUndergroundTests`.
- **The A-star walkability audits** — every square of a floor proved reachable:
  `TheParkTakesAClickTests`, `TheLandingPutsYouSomewhereYouCanWALKTests`, `TheHallIsWalkableTests`,
  `TheRoundIsWalkableTests`, `YouCanWalkTheHiveTests`, `TheExitIsTheFullStopTests`,
  `StationWreckTests`, and the rest.
- **The boot sweeps** — `EveryDeskBootsTests` (300 s), `EveryPopUpCanBeDismissedTests`,
  `TheBootBuildsTheSameWorldTests`, `TheBootStopsWhenYouLeaveTests`.
- **The snapshot fingerprints of Appendix B** — `EveryFrameLeavesTheSameFingerprintTests`,
  `EveryRoundFingerprintsTheSameTests`, `EverySeatTheCaptainTakesFingerprintsTheSameTests`. A
  re-pin is never done off a fast run.

**Run it full before you push**, and know that CI does regardless: `.github/workflows/ci.yml` runs
`dotnet test SpaceSails.slnx` with no filter and is deliberately untouched by #251. The fast run is
a convenience for the person typing; the merge gate is the whole contract, as it always was.

### The roster, and how to change it

The mark is an xUnit trait — `[SlowGate]` on the class, which a discoverer turns into
`speed=slow`. It is declared once per test assembly (`tests/*/SlowGate.cs`) because the two test
assemblies cannot see each other; what travels between them is the trait name, not the type.

Every tag is written down with the seconds that earned it, in
`tests/SpaceSails.Core.Tests/TheSlowGateRosterTests.cs` and its Client twin, and three laws hold the
two halves together:

1. **No unwritten tag.** A class carrying `[SlowGate]` with no row goes red — a tag nobody wrote
   down is a test the fast run silently stops running.
2. **No stale row.** A row naming a class that is gone, or that no longer carries the mark, goes red
   with "REMOVE ME".
3. **The mark reaches the runner.** The discoverer is asked directly what trait it emits, and the
   attribute's wiring is checked to point at *its own* assembly's discoverer. Without this, laws 1
   and 2 would stay green forever while `--filter "speed!=slow"` quietly ran everything.

Plus the anti-vacuous half: the sweep must find the assembly's test classes, the roster must be
non-empty and must match the tagged set exactly, the tagged set must stay a small minority (a mark
on everything would make the fast run empty and still pass), and no row may sit under the documented
cut. All four laws were **shown RED** before they were trusted — a planted tag, a stale row, a
reworded trait key and an under-cut number; the messages are quoted in the #251 PR body.

**The guard does not re-measure.** Asserting "this class really does take ten seconds" would be
asserting a property of the machine it happens to be running on — a loaded dev box, a cold runner, a
laptop on battery — and would redden for reasons that have nothing to do with the code. The numbers
in the roster are evidence, dated and quoted; only tag-versus-roster agreement is re-checked, because
that is a property of the source and cannot drift with the weather.

To re-measure the class totals yourself:

```bash
dotnet test SpaceSails.slnx -c Release --logger "trx" --results-directory TestResults
```

then sum each class's `UnitTestResult/@duration` from the `.trx`. Tag or untag the class, edit its
row in the same commit, and quote what you measured in the PR — the laws above will tell you, by
name, if you did only half of it.

---

## Appendix D — the process-wide registers, and why some suites run alone (#1108)

Five registers in Core are **ambient**: a static the whole process shares, installed once by whoever
owns the save and consulted at the one seam every reader already goes through.

| register | installed by | read by |
| --- | --- | --- |
| `PreservationZone` | `Map.Preserve.cs` | `MoonSurface.SurfaceDeck`, `UndergroundComplex.MoneyTrail` |
| `StopOrder` | `Map.Stop.cs` | `UndergroundComplex` (depth, bands, the money trail), `CanteenBoard`, `CanteenRegulars`, `Burial.NoticeIsUp` |
| `Burial` | `Map.Burial.cs` | `UndergroundComplex.HasFoundBand` and its neighbours, `CanteenBoard`, `CanteenRegulars` |
| `PoliteDecline` | `Map.Decline.cs` | `UndergroundComplex.Decline` |
| `QuietHands` | `Map.QuietHands.cs` | the owed-ground seam |

That is deliberate and it stays. A burial changes the *shape* of a site, and the shape of a site is
asked by about thirty callers — the lift panel, the remote, the sounder, the room carver, the sign
writer, the audits, the renderer — none of which has any business learning what a burial is. §13.15's
second cause is a caller reasoning about the shape of a building it does not own, and thirty callers
each taught a new idea is that bug thirty times. The game is single-threaded and reads them safely.

**The test runner is not single-threaded, and that is where the cost lands.** xUnit parallelises
across test classes. A guard that installs one of these registers on a *real* body id — `luna`,
`titan`, `phobos` — changes the world under every other class building that body at that instant.
Symptoms are never about the register: #1108 was `EveryFrameHashesTheSameTests` drawing 651 marks
where 649 were pinned, and `TheLiftHeadIsJustAnotherHutTests` measuring a lift head with a
preservation fence accidentally welded to it, about one run in four with Core and Client sharing a
machine. The register never appears in the message.

So:

* **Every test class that writes one of these registers carries
  `[Collection(StopRegisterCollection.Name)]`** — in both suites. The definition is one linked file
  (`tests/SpaceSails.Core.Tests/StopRegisterCollection.cs`, compiled into the Client suite as well),
  because a collection definition is per-assembly and two copies of "there is one of these" is how
  two halves come to disagree.
* **That collection is `DisableParallelization = true`.** Sharing a collection serialises the
  *writers* against each other and does nothing at all about the *readers*, which are the rest of
  the suite. Measured with an isolated xUnit 2.9.3 probe: four watcher classes polling a flag held
  by a plain `[CollectionDefinition]` class all saw the overlap (4 failed / 1 passed); with
  `DisableParallelization = true` on the same definition, none of them did (5 passed).
* **`TheProcessWideWritersAreSerialisedTests` enforces it** by reading both suites' sources for the
  six writes that replace process-wide state — the five `Install(` calls plus
  `Aerobrake.DiceEpisodeHook =`, which is the same animal — and failing any class that performs one
  without the attribute. It found four that had drifted outside, and two Aerobrake suites that had
  never been in.

**Writing a new guard that needs one of these registers?** Install it, restore it in a `finally`,
prefer an id family of your own (`care-ground-0`, `money-ground-1`) over a real body — and put
`[Collection(StopRegisterCollection.Name)]` on the class. The law will tell you if you forget.

**The other half — the generator caches.** `MoonSurface`'s layout memo and `HavenInterior`'s deck
memo are process-wide caches. Both were plain dictionaries once and both cost an afternoon (#585, a
shelter list that did not match the ground; #649, an `InvalidOperationException` out of the oracle
seat audit); both are `ConcurrentDictionary` now, and neither fix left a guard behind.
`TheWorldBuildersAreThreadSafeTests` is that guard: it fingerprints every mark the three world
builders lay, then has every core rebuild them fifty times over and asserts the fingerprints never
move. A cache keyed on a pure function of its inputs is fine; one whose value depends on call order
is not.

**"But the boot path writes them on every page build."** It does — a live `Pages.Map` is the game's one
writer and installs all five on every world build, so every Client guard that boots a page writes them too.
Those writes are `Install([])`: a fresh voyage has nothing stopped, fenced, filled or declined, and no test
boots a page with `?stopped=` / `?buried=` / `?preserved=`, nor loads a vault whose `Halls*` rows are
non-empty. An empty register replaced by an empty register moves nobody's world. What moves a world is a
**non-empty** install, and that only ever happens in the dozen suites the law names — which is why
serialising them costs a dozen classes and not the half of the Client suite that boots a page.
