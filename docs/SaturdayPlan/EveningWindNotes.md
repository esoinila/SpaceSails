# Evening wind — the Miranda surface: treasure, tide & tools (2026-07-18)

*Provenance: owner rulings streamed live during the Saturday-evening localhost playtest (release build,
`710e530`), worked into a spec draft by Fable. The playtest ran the full bury loop end-to-end:
board with a loaded chest → maze walk → channeled dig under a 3-Reever turnout → nerves shot →
sprint → lift-off → map card → ledger. **The bury→ledger path works**; everything below is what
the ground should become next.*

## The one law (owner, verbatim intent)

> "The idea is that even with bots there is only so long time to stay there."

**The tide owns the ground; bots rent minutes.** No loadout makes a surface stay safe — only
longer. Every mechanic below serves that clock.

## What the owner asked for (this stream)

### The tide (Reever spawning)
1. Reevers come **from the bottom of the screen** (the deep edge) — **no cap**, at **random
   intervals**. Replaces the fixed roll-plus-linger-ticks pack.
2. They should paint on the **motion tracker long before** they reach the visible map — contacts
   born beyond the viewport, heard before seen.
3. **More than 2 Reevers total** in the world; their job is *loitering prevention*, not ambush.
4. Reevers **don't venture too far** (toward the landing band) — they have a home range; the
   corollary is that bots can't protect the deep field, only the middle ground.

### Treasure hunting (the beach-comber kit)
5. **Bury anywhere** (or at least choose among several spots) — not one fixed ⛏ field.
6. Bringing a **shovel + metal detector** = minesweeper-like play: probe **any square** of the
   surface for shallow-buried treasure. A **per-visit grid** marks the squares already checked.
7. A **D100 throw** decides whether any spot holds treasure. Unlucky to find anything — but never
   impossible. Some ground is **too hard to dig**; the die handles that too.
8. Burying stores the location **onto the map and into the ledger** *(confirmed working today —
   the map card files under 🗺 Treasure maps)*.
9. **The empty sling is a fishing expedition, not a mistake** (owner ruling, later the same
   evening). Going down with nothing to bury is a first-class trip with its own goals:
   - **Lift somebody else's cache** — by a **tip** (the information about a spot to check is the
     only cargo you need) or by plain **luck** (the D100 kit above).
   - **Dead drop** — a known spot works as a drop point: one party buries, the other party's tip
     says where to dig. (Natural hook for the masked-contracts lane, PR-E.)
   - **Lift your own earlier hoard** — walk back to your ✗ *(the own-cache path exists today via
     `LiftChestHere`; rumor-bought maps of rivals' hoards already file in the ledger as "a map
     you hold" — what's missing is digging at a tipped spot you don't own, and the drop flow)*.
   **Both action paths must work**: chest aboard → bury; no chest → hunt/lift.
   - **Verbal tips paint candidate spots** (owner: "we could receive verbal instructions about
     where a treasure is... then we could show possible places to try our luck on the map. Then
     we are in Reever Town :-D"). A spoken tip is deliberately FUZZY — "86 paces anti-spinward"
     resolves to SEVERAL candidate dig spots on the surface map, and you try your luck spot by
     spot. Every candidate sits deep in the tide's ground, so the treasure hunt and the eviction
     clock are the same mechanic: the longer the tip takes to run down, the more of Reever Town
     you meet.

### The ground itself
10. The walk area should feel like a **planet — big and round**, not a fenced square. **Natural
    barriers** (crater rims, crevasses, boulder fields) instead of box-maze walls.

### Bots (the sentry crew)
11. Bots **auto-exit the shuttle** on landing and *offer their services* — no memorized key.
    The **T key is too hard** for new players; guard positions should be a **mouse click**.
12. **Burst-fire sound** when they engage — *like in the Aliens movie*.
13. Bots **report in** as their magazine drops **below 50**.

### The instrument column
14. **Advertise the dig and bot actions in text under the motion tracker** — the left column
    teaches the ground game.
15. Reever blips on the tracker: **red**, **smaller**, **pulsing like a heartbeat**.

### The bar-keep's post (haven interiors)
16. Every bar whose art shows a **bar desk** must put the barkeep **service position** — the
    console you press E at to get a drink — **at that desk in the picture**, never in the middle
    of the empty floor. The counter overlay sits **on top of the bar in the image**, never over a
    window. Table positions are low-priority; we don't sweat those.
17. **Audit ALL bars per image** (Roadstead/Mars, Cinder Roost, Ringside, The Tilt). The #247 fix
    assumed one shared geometry ("all four arts draw the counter down the left",
    `HavenInterior.cs`) — that assumption yields to per-image coordinates wherever it's wrong.
    Runs as its **own Opus branch**: check every bar, reposition each.

### The nerve gauge (sanity — #226 Fail Forward, on the #317 first slice)
18. **Don't drain the nerve too fast on sightings** — diminishing returns (owner: "seeing one
    reever after already seeing one more does not make you that much faster more nuts"). The
    first contact of a watch is the shock; the rest of the pack stacks only a sliver each. *(In
    today's playtest one dig took the captain from steady hands to nerves shot — too steep.)*
19. **"If they get to skin, that is a different thing"** — actual Reever contact is the big,
    undiminished hit. Sightings fray; touch breaks.
20. **The overdraw rule**: nerve already empty + more sanity damage → the captain **goes crazy
    and dramatically exits the scene**, and **the piracy insurance issues a new captain** :-D.
    Fail Forward, not game over — the run continues under a fresh name; the ledger, the ship and
    the hoards persist. (Ropecon Fail Forward/sanity system is the canonical source for the
    shape of this.)
21. **Recovery is quick where it's earned** — a drink at the bar or **resting** heals the nerve
    fast (the #339 drink-steadies-the-nerve seam is the start of this lane; rest joins it).
22. **One cabin becomes the MED BAY** — calming pills retrieved there restore the captain's
    sanity. The ship deck's three cabins donate one; the med bay is the shipboard answer when
    there's no bar in reach.

## Implementation queue (owner: "let's get coding", 2026-07-18)

- **Lane 1 — the tide + the instrument column** (#1–#4, #14–#15): endless deep-edge spawner,
  home range, red heartbeat blips, column captions. Opus branch.
- **Lane 2 — the bar-keep audit** (#16–#17): per-image service positions across all four bars.
  Separate Opus branch.
- Later lanes (beach-comber kit + fishing expeditions #5–#9, the round ground #10, bot crew
  #11–#13, nerve tuning + the insurance captain #18–#20) queue behind these.
- **Miranda is the prototype ground** (owner: "main focus on Miranda and shuttle treasure
  fixings … let's prototype on Miranda, because those reevers :-D"). More shuttle destinations
  come AFTER the loop sings here — one haunted moon teaches us faster than five quiet ones.

## What the code already gives us

- `MotionTracker.Sweep` has **no range cap** — it already paints movers "well beyond the visible
  grid edge." A spawner past the viewport gets ask #2 for free; only the blip styling (#14) and
  the caption text (#13) are client work in `DeckView`/`Map.Surface`.
- The tide replaces `ReeverRaid.Roll` at dig start + `WakesOnLingerTick`/`MaxSurfaceReevers`
  (`Map.Surface.cs`). The "pinned by sentry → velocity zero" behavior (bots grinding a Reever
  still) already embodies "bots buy time, not safety" — keep it; the tide just guarantees the
  magazine runs out before the ground does.
- `MoonSurface.DigFieldX/Y` is the single fixed bury spot (#313's commitment walk). Free-form
  burying retires it; the D100 + hardness throw slots into the existing channeled-dig
  (`BeginDig`/`CompleteDig`) and dice idioms (2D6 turnout roll rides the same seam).
- `MoonSurface.CachePosition` scatters the ✗ by hash instead of recording the dug spot — fine
  while the dig field was fixed; free-form burying must store the REAL spot (see bugs, below).

## Bugs & rough edges from the playtest (separate from the design lanes)

1. **Empty-sling excursion gives no ground feedback** — with nothing to bury the ⛏ site never
   spawns and nothing on the ground says why; the boarding chooser still advertises "find a spot
   to dig." This caused a false "the ledger lost my map" report today. *Superseded in part by
   ruling #9: the empty sling becomes a legitimate fishing expedition (lift by tip/luck, dead
   drop, lift your own) — the remaining fix is that whichever path you're on, the ground must
   SAY what's possible.*
2. **`logged 0d 16h 13m`** on just-created ledger entries — reads as an age, shows a timestamp.
3. **Keyboard focus loss** after closing the map card (and after some clicks): desk hotkeys 0–7
   and E go dead until a click refocuses the map div.
4. **Main-thread stalls**: scenario boot ~25–30 s; shuttle transitions still freeze long enough
   for "page unresponsive" flickers (#333 reduced, not eliminated).
5. **The ✗ renders at a hashed position, not where you dug** — by design today, but it reads as
   a bug on the ground; free-form burying (#5 above) forces the real fix.
