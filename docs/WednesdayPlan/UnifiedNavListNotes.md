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

**Step states:** `planned → armed → active → done`, plus `stale` (exists today) and
**`waiting-on-open-ended`** (new): a step after an open-ended step keeps its parameters but its
SimTime is meaningless — render grayed with a marker: *"⏳ waits on: Ambush — timing recomputes
when it resolves."* Never silently execute a waiting step; when the open-ended step resolves,
re-solve times (or mark stale where physics changed).

**Interaction:**

- Click a step → it expands IN PLACE to its options (burn direction/pulses, sling side/radii,
  insertion body/rehearsal, skim depth). The floating sling/skim panels become these editors.
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

## Build order proposal

1. **PR-D1 — steps for insertions:** model armed auto-orbit as a flight-plan step (read-only
   list entry first — no behavior change, just presence + states). Cheap, kills most of #124's
   "loose buttons" complaint together with the shipped ghost gate.
2. **PR-D2 — step editors:** sling + skim panels open from their steps (accordion).
3. **PR-D3 — waiting-on-open-ended** state, shipped WITH the first open-ended step (PR-E's
   ambush), not before — no speculative machinery.
4. Dragging last, if at all.
