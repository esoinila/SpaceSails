# UI usability notes — captured while flying a piracy run

Running list of friction points found while actually *using* the game to hunt a
ship. Each is a candidate for a UI improvement, roughly in the order hit.

## Finding a target
- **"Traffic board" wording vs. the actual desk.** The First-hunt tutorial says
  "Open the traffic board and select the Luna pod," but there is no control
  labelled *traffic board* — it's the **Comms** desk (Contacts & intel). The
  tutorial's noun and the desk's name should match, or the tutorial should say
  "Comms desk (5)".
- **The en-route freighter list is a flat name list.** Comms → Contacts shows
  ships by name + status chip only. To learn each ship's route/cargo/distance I
  had to click every row and read its dossier one at a time. A hunter wants to
  compare prey at a glance — show route, cargo (and value), and distance/closing
  rate as columns, ideally sortable by "closest" or "richest".
- **No catchability cue in the list.** Whether a ship is *closing* or *opening*
  only appears after selecting it (map popup: "opening 6.0 km/s"). Surface a
  closing/opening arrow + rel-speed on the contact row so you can pick a
  reachable target without opening each one.
- **"Not yet inside sensor reach" targets can't be acted on.** All four en-route
  freighters read "last seen: never — not yet inside sensor reach," so the Comms
  dossier is informative but every action points elsewhere ("track her at the
  Sensors desk"). A one-click "hand this contact to Sensors / plot toward its
  route" would save the desk-hopping.

## The catchability gap (biggest one)
- **Nothing tells you *when/where* a target is actually boardable.** The window
  is rel-speed < 5 km/s within 5e8 m, but interplanetary traffic runs at
  **80–160 km/s relative** mid-transit (Tycho's Due showed 161 km/s inbound).
  A ship is only matchable when it shares your orbit — e.g. in the first hours
  after it departs a body you're near, or as it settles into a destination orbit
  you also hold. Nothing in the UI flags "this one is matchable" vs "this is a
  160 km/s fly-through you can never catch." A hunter currently learns this only
  by selecting a target and reading a scary rel-speed. Ideas: colour the contact
  by catchability (green if a plotted match is within your Δv budget), or show a
  "match cost: N pulses / impossible" estimate next to rel-speed.
- **No "ambush a fresh departure" affordance.** The natural catch — pounce on a
  ship just leaving Earth/Luna while it's still near your velocity — isn't
  surfaced anywhere. A "departing soon from <body>" flag on scheduled ships (with
  the bodies you're co-orbiting highlighted) would point new pirates at the one
  catch that actually works from a standing start.

## Scope
- **Manual target stepping is unclear.** With the scope in AUTO, clicking the
  ◀ / ▶ arrows didn't visibly cycle the locked target. It's not obvious you must
  leave AUTO first, or the arrows need a more obvious active/disabled state.

## Tutorial vs. reality
- **"First hunt" promises a catch the opening state may not offer.** The tutorial
  says to select the Luna pod and board it, but at scenario start I found no pod
  or freighter inside the boarding envelope — the nearest ship was receding and
  the rest were 160 km/s fly-throughs. Either seed a genuinely catchable target
  near the player at t=0, or have the tutorial say "wait for / ambush a fresh
  departure" so the promise matches the physics.
- **Warp + boarding interaction is unexplained.** Boarding progress accrues at
  wall-clock rate (so you can't warp through a board) — correct and fair, but the
  UI never says so; a first-timer will crank warp during a board and wonder why
  the bar crawls. A one-line hint when a window opens would help.

## Selection / picker
- **The map "Which one?" chooser lists bodies and depots but not the nearby
  ship you were aiming at.** Clicking the ship cluster near Earth offered
  Earth/Luna/depots; the freighter itself came via a second path. Prey should be
  first-class (and top) in that chooser.
