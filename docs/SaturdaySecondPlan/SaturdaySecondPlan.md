# Saturday Second Plan — the arrival earns its bill (2026-07-18)

## Where Friday night left the ship

The first dock-to-dock long haul in the game's history flew last night: Rusty Roadstead → the
void → The Tilt, one roadster wallet delivered eight game-years late, 4,200 cr paid under the
table. The trip also wrote the night's ledger in bruises — a stranding at 29.8 km/s with 32
pulses, a rescue tug that ate the hold, an accidental capture on integrator error, and a dock
that clamped on from 104,000 km out. Ten PRs merged before the watch ended (#259, #270–#278):
the void crossing and its resume, the 500-pulse tank, the plot-ribbon cap, the rescue pop-up,
computed skips, the dock that attaches, the impact that finally arrives, pay-at-the-pump
billing, the surface-clearance gate — and #275, the Map.razor split into thirteen subsystem
files, so today's lanes stop queueing for one door.

Every symptom got its fix. The DISEASE is still on the books: **the long haul quotes the
departure and hands you the arrival as a surprise**. That is Saturday's work.

## The main lane: PR-D1 — insertions as steps, and the whole trip quoted (#262 structural)

North star unchanged: `docs/WednesdayPlan/UnifiedNavListNotes.md` — ONE flight-plan list,
dock-to-dock, each step's sub-panel owning its buttons. The odd ones out are still orbit
insertion and dock: they execute immediately and mutate `_ship` instead of being steps.

1. **Insertion becomes a step.** The armed auto-orbit / capture burn takes its place in the
   plan list with an epoch, a quoted pulse bill, and a sub-panel — the same grammar every
   other burn already speaks. It bills as it fires (#268's law, already plumbed via #277).
2. **The long-haul gate quotes the ROUND bill.** Departure + arrival insertion, checked
   against the tank at the offer (#249's refuse-with-reason machinery; #278's clearance gate
   already sits in the same doorway). The bus includes BOTH burns — or says plainly that it
   doesn't. Never again a captain discovering the brake costs 201 p with 32 in the tank.
3. **Dock joins the list last** (stretch): clamp-on as the plan's final step, so a milk run
   reads top-to-bottom as the trip it is.

Deliverable: fly a milk run where the whole journey is visible as steps before the first burn,
priced end-to-end, and the arrival is just another line that executes on schedule.

Builds on (all merged last night): `BerthState.CoMoving` (#274), `MatchClampLedger` (#277),
`SurfaceClearance` (#278), the partial-class layout (#275 — this lane lives in
`Map.Autopilot.cs` / `Map.LongHaul.cs` / `Map.Plot.cs`).

## The lab menu: Lab 32 — the air brake earns a licence (#263)

The accidental Uranus aerocapture was retired by #276; the SANCTIONED version starts as a lab,
per the R&D method — the probe prints numbers before any button exists:

- Corridor width vs entry speed: at what periapsis depths does a pass skip out / capture /
  exceed a heat-and-g budget (simple exponential atmosphere + the existing drag machinery).
- Δv shed per pass vs depth — one pass or a multi-pass campaign at 36 km/s.
- Cost in GAME units: what a pass charges in hull/heat/sanity terms the existing systems price.
- Which worlds play: Uranus, Neptune, Saturn, Jupiter (fierce), Titan (the training wheels),
  Venus, Mars, Earth.

The lab feeds PR-D1 an **aerobrake step variant** later: the plan quotes "2 passes, periapsis
180 km, heat moderate, 0 pulses" beside the honest burn's pulse bill, and the captain picks
their poison. Seams are already marked in `SurfaceImpact` and `SurfaceClearance`.

## The shore lane: every bar needs a barkeep (#247)

Buying a drink ashore — The Tilt stocks for the occasion (no captain in the setting has earned
a tot harder). Composes with the sanity meter (#226): shore leave finally has a counter to
drain. Small lane, high flavor, and the Galley's "0 tots poured" stops being a lie of omission.

## The tidy queue (cheap, batchable, one lane)

- **#258** — archive the non-Sol scenario cards (it is always Sol; the rest move off the front
  page, definitions kept).
- **#254** — version stamp (git SHA + build time) on the start picker: build-ghosts die at a
  glance.
- **#253** — context menus clamp to the viewport (verify what #272's z-order work already
  covered; finish the rest).
- **Map.razor.css split** along the #275 seams, plus the refactor's "spotted for later" list
  (Plot/Combat subdivision, draw-primitive extraction) — only if the day has room.

## Open questions for the owner (answer over morning coffee)

1. **Insertion-step consent**: when the plan reaches an insertion step, does it fire like any
   scheduled burn (navigation = flows), or ask the captain first? Existing law reserves
   are-you-sure for deliberate acts (weapons, felonies) — proposal: insertions fire, docks ask.
2. **Round-bill refusal**: when departure + insertion exceeds the tank, does the long haul
   refuse outright, or offer the one-way with a plain "the far side is a stranding" warning?
   (Stranding is now survivable-but-expensive: the rescue tug and insurance both exist.)
3. **Aerocapture risk currency**: dice-scripted episodes (the BUSTED engine, house style) or a
   deterministic heat/hull price for the first pass?
4. **Drinks**: pure sanity-drain purchases, or can a round buy dice items / contact warmth too?

## The testing protocol (unchanged — it caught everything again last night)

Owner flies fresh **Release** builds hands-on and streams findings; the lead triages live from
the tab with code-level root causes, files issues mid-flight, dispatches Opus lanes
same-session; merges under explicit or standing approval, CI-gated; every lab README pastes
only numbers a probe actually printed. The bench flight is not optional — it is the only
compiler some bugs will ever meet.
