# The unified navigation list & UI depth — design notes (2026-07-15)

*Provenance: owner rulings from the Wednesday playtest stream, worked into a spec by Fable as a
stand-in — the Gemini CLI consult is queued behind its interactive login (it was hung on the
auth prompt all morning). When Gemini is back, run it over THIS doc as a second opinion, the
way Tuesday's blind audit compared stand-ins to the real thing.*

## What the owner asked for (issues #123, #124, #127 + stream)

1. All the flight steps organized — the navigation steps need to be in ONE list.
2. WHO IS FLYING THE SHIP needs to be BIG in the UI *(shipped in PR #129 — the pilot banner)*.
3. The current burn list should gather the flying UI steps into it; the loose slingshot buttons
   should fold into the same list the way burns already do (#124).
4. Maybe draggable re-ordering; clicking a step opens its options and settings.
5. Open-ended steps (an ambush of another ship — unknown completion time) make later planned
   burns non-computable: they should be grayed out / marked, not silently wrong.
6. The UI needs depth — subviews instead of everything crammed on top of each other (#123).

## What the code already gives us

- `Map.razor` `_planNodes` (≈line 2065) is ALREADY an ordered, time-sorted step list
  (`PlanNode`: SimTime, Action, Pulses, Percent, Stale, Executed, Mode, HeadingDegrees), and
  manual burns, sling burns (`AddSlingBurn`), and skim burns (`AddSkimBurn`) all flow into it.
- Orbit insertion and dock/undock are the odd ones out: they execute IMMEDIATELY and mutate
  `_ship` (`EnterOrbit`, `CheckArmedInsertion`, `ToggleDock`) instead of being steps. Armed
  auto-orbit (`_armedOrbitBodyId`) is a step in spirit with no list presence.
- `StaleFutureNodes()` already exists — the invalidation mechanic just needs a second flavor.

## The flight plan (spec)

**One list, called the flight plan, replacing the burn list.** Every entry is a step:

| Step kind | Today | Becomes |
|---|---|---|
| Burn (both modes) | `PlanNode` | unchanged — the seed of the list |
| Sling burn | `PlanNode` via SOLVE | unchanged, but its editor opens FROM the list |
| Skim burn | `PlanNode` | unchanged, same |
| Orbit insertion | immediate `EnterOrbit` / armed flag | a step: "insert at Titan (armed)" |
| Dock / undock | immediate toggle | a step at the end of a plan, or immediate as now — step form is for planning ahead |
| Ambush / escort event (future, PR-E) | — | an OPEN-ENDED step |

**The unit of planning is the TRIP (owner, mid-stream, excited):** the flight plan should be
able to hold the whole journey — *"from docked position to docked position, or orbit on
another place."* Undock → burns → sling/skim → insertion → dock, one list, readable top to
bottom. Something can always intercept the plan — that's the game — but a **milk run** should
be plannable end to end and then just flown. This is the usability north star the list is
built toward; the step kinds above are exactly the vocabulary a dock-to-dock trip needs.

**Step states:** `planned → armed → active → done`, plus `stale` (exists today) and
**`waiting-on-open-ended`** (new): a step after an open-ended step keeps its parameters but its
SimTime is meaningless — render grayed with a marker: *"⏳ waits on: Ambush — timing recomputes
when it resolves."* Never silently execute a waiting step; when the open-ended step resolves,
re-solve times (or mark stale where physics changed).

**Interaction:**

- Click a step → it expands IN PLACE to its options (burn direction/pulses, sling side/radii,
  insertion body/rehearsal, skim depth). The floating sling/skim panels become these editors.
  **Owner endorsement (verbatim intent): the sub-panel of a step is WHERE the related buttons
  live** — SOLVE, arm/disarm, add-the-burn, dock controls all belong inside their step's
  expanded panel, not loose on the HUD. The step list IS the button organization (#123/#124's
  cure in one move).
- **A burn step's direction is four words, in the ghost's frame (#838, owner ruling A,
  2026-08-17).** Owner, at the end of a playtest: *"The actual scrub flying should probably only use
  the vector view but have like quick selects for forward, backward, up and down directions. I think
  the plus- and minus flying is something that works for reflex flying but our planning sailship
  flying almost always use the vector view. I almost always use those four directions in respect to
  our trajectory... the intermediate angles are the exception. Also that panel could be bigger to fit
  all the buttons properly."* So the burn step's sub-panel offers **▶ FORWARD · ◀ BACK · ▲ UP ·
  ▼ DOWN** — prograde, retrograde and the two radials in the game's plain words — and nothing else
  by default; free aim (any typed angle) stays as the documented exception. The ± idiom leaves the
  planner entirely and stays where it works, in reflex flying (the live `+`/`−`/arrow keys). **The
  frame law:** all four are solved from the ghost's own position and velocity **at that node's
  epoch**, against the primary at that same instant — never off the ship's live heading, because the
  planner plans the future and the two frames differ exactly when planning matters most. Nothing is
  cached: re-time the step and press again and the direction is re-solved off the re-projected
  ribbon. The words and the arithmetic both live in Core (`NodeFrame`), so the panel, the glance
  line and any future desk can never disagree about what "up" means. The plotting panel was widened
  (32 → 38 rem) so the four buttons and the free-aim row each fit on one line.
- Drag-to-reorder (stretch goal): only meaningful between steps whose order is a free choice;
  a physics-ordered step (a sling solved for a specific pass epoch) snaps back with a one-line
  why. V1 can ship without dragging — click-to-edit matters more.
- The pilot banner (PR #129) names the active step when the autopilot is flying it:
  "AUTOPILOT HAS THE SHIP — step 3/7: insert at Titan."

## UI depth (#123) — progressive disclosure rules

- **Layer tokens, not ad hoc z-indexes:** map < map overlays < side rails < top strip (desk
  tabs + pilot banner) < modal desk. PR #129 already anchors modal desks below the top strip.
- **The main flight view shows only:** the pilot banner, the flight plan (collapsed: next step
  + "…4 more"), alerts/toasts, and the map. Everything else lives in a desk or opens from a
  step. The loose slingshot/orbit/dock buttons on the HUD (#124's complaint) fold into the
  flight plan or the target's context card.
- **One editor open at a time:** expanding a step collapses the previous one (accordion), which
  is what kills the crammed-on-top look on small screens.
- **Context card** (right side, replaces stacked pop-ups): whatever is SELECTED — a body, a
  step, a contact — gets the one card; selecting something else replaces it.

## Frame awareness (owner, backlog — see UIUsabilityNotes.md "Planet-centric frames")

Inside a gas giant's Hill sphere the plan and its numbers should speak the LOCAL frame:
step deltas, relative speeds, and vectors quoted Saturn-centric at Saturn (Jupiter-centric
at Jupiter), with a visible frame chip. Heliocentric numbers made the Saturn moon tour
unreadable — the primary's ~10 km/s solar orbit drowned the moon-to-moon deltas.

**Shipped, and the other half of it (#926, 2026-08-17).** The frame chip landed with #135/#143/#206;
what it never did was SAY which frame the step editor was reading a plan in, and the owner hit the
mirror image of the Saturn problem from the other side — *"the real thrust amounts are dependent on
the coordinate origin. I had to remember to switch to Sun to get the ship to really start moving from
Earth towards Mars."* A planet-centric frame drowns an interplanetary trip exactly as the solar frame
drowned the moon tour. So the step editor now names its reading frame always, and when the plan has a
destination it offers the **trip's** frame — the common parent of both ends (`TripFrame.Of`: the Sun
for Earth→Mars, Earth for Earth→Luna, Jupiter for Europa→Ganymede) — in one press. It offers; it never
switches by itself, and the press moves the ribbon and the numbers, never the plan.

## The trip starts at the berth (#955 NAV-1, 2026-08-23) — SHIPPED

Owner's test story for this list, filed on #955: *"plan while docked, then the plan starts with an undock step
recorded topmost in the nav-burn list, then safe-harbour out-thrust to clear the vicinity of the station, then
the actual burns, then the autopilot approach step, then the dock step."* #965 built the arrival and #969 made
arming it a plan-time promise; both of them refused to be made from a berth, which is the one place a captain
actually plans a voyage from. So the table above gains its first two rows for real:

| Step kind | Row | Executor |
|---|---|---|
| **⚓ Undock** (`PlanStepKind.Undock`) | topmost, laid by **⚓ + Cast off** | the frame loop, landed on exactly (`ConsumeTheAccumulator` → `RunTheCastOffStep`) — it is the one step that changes which BRANCH the loop takes, because a clamped ship's frame never applies a maneuver plan |
| **🚀 Clear the harbour** (`PlanStepKind.ClearHarbour`) | row 2, laid by the same press | the ordinary plan executor — it is a real `BurnMode.Vector` node, which is why the ribbon already draws it and the tank already bills it |

**One press lays both.** A cast-off that leaves the ship drifting in the harbour's traffic is not a cast-off.

**The clearance is sized by the harbour, not typed** (`Core/CastOffRule`): it thrusts straight out along the
berthing arm to a share of `DockRule.MatchSpeed` — the fastest relative speed the envelope law itself calls
*matched*, so she never leaves faster than the clamp would have taken her coming in — and "clear" is
`DockRule.EnvelopeMeters` with a margin. At a Sol berth that is **two pulses**; the berth's own shove
(`UndockPushMps`) counts toward it and is not paid for twice. The two judgement numbers — the departure's
share of the matched speed, and the margin — are named constants in `CastOffRule` and flagged there as owner
knobs.

**The plotted path starts at the berth.** `ReprojectTrajectory` projects from `PlanStartState()`, which for a
clamped ship with a pending cast-off is the state the clamp is about to hand over (berth + shove). So the
passes, the arrival's ✓/✗ and #969's arm-time rehearsal are all computed from the berth onward.

**The one carve-out in the nav lock, and it is not a live act.** A clamped ship may ARM a *then* — a promise
about a pass months away that moves nothing when it is pressed — provided the plan begins by casting her off.
Every other nav act (the NOW arm, `EnterOrbit`, the match-and-clamp) is still refused by `RejectNavWhileDocked`
in the same ⚓ sentence. And a plotted burn whose epoch slides past under the clamp is **struck, not billed**:
the clamped branch never fired it, and charging for it would be a green number never asked of the world.

## Build order proposal

1. **PR-D1 — steps for insertions:** model armed auto-orbit as a flight-plan step (read-only
   list entry first — no behavior change, just presence + states). Cheap, kills most of #124's
   "loose buttons" complaint together with the shipped ghost gate.
2. **PR-D2 — step editors:** sling + skim panels open from their steps (accordion).
3. **PR-D3 — waiting-on-open-ended** state, shipped WITH the first open-ended step (PR-E's
   ambush), not before — no speculative machinery.
4. Dragging last, if at all.
