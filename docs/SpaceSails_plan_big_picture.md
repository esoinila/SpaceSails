I want a sailing ship pace game that takes place in the solar system level.

The spaceships either accelerate on their current trajectory or slow down as the basic mode of their movement control.

Like in sailing ships the navigation is plotted and planned in advance with taking into account the movement of the celestial bodies. 

I have tested this kind of game mechanic with real-time game-play and it is surprisingly controllable just with plus and minus to velocity vector with 10% increments / decerement.

Now the gameplay there is easy if can "fly" large circle around the bodies, it get's tricky once you in between the planets. This in-between the planets is the most exiting place to fly but it really needs the planning stage to be fun. Maybe the player could be a space pirate that robs Helium3 ships coming from Saturn to the inner planets.

Besides the current solar system, I kind of like the Velikovskian Wheel of the World topography where Earth-Mars-Venus are aligned and rotate Saturn which rotates around the sun. 

Maybe the sails could be large banks of solar cells on one side and thermal evaporators on the darker side. 

Approaching from the Sun, would help sneak up on He3 cargo ships. Electric Universe style assumed gravity being controlled by hollow spehere charge capacitors hypothesis could be fun. So Let's assume the charge is different on different distances of the Sun and the charge must be managed when changing the distance from the sun to avoid massive Thunderbotls of the Gods style arcing. Also I love all kinds of massive plasma phenemons in the sky, and somehow using these streams as ways to move around in some fashion, say some kind or MRI magnet or small ship that intentionally is set to have high charge to gain momentum from magnetic fields or electric force to gt free momentum. Besides that I kind of love the the Expanse style physics, which Robert Zubrin advertised in his book the Case for Space also.

But overall I want the sail-boat game with map-plotting mode where the massive ships move similar speeds to the planets, to make it difficult. Also the assumed trajectories / plans of other ships should be "simulated". The plans would include the control movements and trajectory changes that only the ship captains would really know. But there should be some way for the pirate captain to guess the control movements of his prey and it should show in the UI as the predicted path.


First write a markdown with the plan. I like dotnet 10, which is installed here and just plain Razer + Bootstrap on browser. Feel free to use tech of your choise for the graphics screen but I think it should run in browser (web-assembly maybe or some other accelerated UI?), so that multigame with Azure container service hosting could be possible. As Senior AI Engineer with MS certs I am partial to Microsoft Tech so use it when possible.

Just first do the detailed plan. Then we can have cheaper models implement it and you can check the work now and then. I have github account so let's have a repo there.

---

## Addendum: where it actually went (2026-08-03)

*Everything above is the owner's original brief, 2026-07-02, kept verbatim. It is still the
constitution — the plotting table is still the heart of the game, and the ±10% pulse is still
the only control. This addendum records what grew on top of it, so a reader does not mistake a
one-month-old brief for the current shape of the game.*

**What the brief predicted correctly.** The plotting-first sailing pace, the pirate premise,
the He3 runs from Saturn, the Electric Universe layer with hull charge and arcing, the Wheel
of the World as a second scenario, and predicted-path cones over other ships' plans — all
shipped, all substantially as described.

**What it did not predict, in the order it happened.**

1. **The ship learned to speak.** The recurring playtest note was never "the flying is wrong",
   it was "the ship never said what it was doing". A single plan-status voice now drives the
   NOW/NEXT banner, the desk chips, the alert channel and the parrot.
2. **The bridge became a crew.** One simulation, eight duty desks, each owning ~70% of its
   screen with every other station riding along as a one-line chip.
3. **The game left the cockpit.** Landable bodies, a shuttle you fly down yourself, and a
   ground you walk on foot in a suit, with nerve and condition as two separate meters.
4. **The ground grew a career, in three stages.** It started as one verb — *bury a chest where
   the law cannot see it*, with a generated map card pacing the hoard off a landmark. Then it
   grew **detective work**: fetch runs with legs, hatch-crack jobs with real codes, and route
   tips carried with their provenance (who told you, where, when), including tips filed as
   *background — may matter later*. Then it grew **secret labs**: a walkable underground
   complex with departments, sealed sectors, authority cards that open exactly one shaft band,
   a suit-air budget instead of a health bar, and — rarely — a band nobody listed, which pays
   in information rather than hardware.
5. **Death became a grammar instead of a reload.** The collector's BUSTED encounter resolves
   through four stages on open dice; rebirth is a brain-backup, a clinic bill and a rustbucket.
   Anything buried or banked was never aboard, which is what makes stage one of the ground game
   a *strategy* rather than a chore.
6. **Two long arcs, and their convergence.** The world now carries two slow mysteries that are
   never explained, only assembled out of fragments earned in ordinary play — one of which
   rides the player's own deaths as its evidence channel. When both are half-assembled, they
   meet. (Owner's framing: *"kind of how in the Expanse the various characters notice their
   rabbit holes converge."*)
7. **The lab course outgrew physics.** The "measure it in a probe, then ship the measured
   number" method got pointed at the *software*: whether a rescue button is clickable when you
   need it, whether A\* can reach the back of the ship, and finally a lab about the lab.

**The through-line, for anyone planning the next stretch:** the brief's instinct that
*planning is the fun* held, and the honesty law generalized — first to instruments, then to
the curriculum, and now to content nobody's integrator can check.