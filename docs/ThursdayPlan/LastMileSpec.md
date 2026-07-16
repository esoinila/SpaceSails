# The last mile — co-orbital rendezvous (#155) and Lab 24

*Second Thursday lane, same playbook as [MoonRunTransferSpec.md](MoonRunTransferSpec.md): the
kernel is in Core and lead-verified (`TransferMath.PhasingOrbit`/`PhaseGap`, commit ab51851);
lanes build the planner mode, the lab, and the wiring. Read the moon-run spec first — every
house rule there (determinism, SlingPlanner-shaped refusals, one pricing kernel, rehearsal as
the honest verdict) applies verbatim.*

## 0. The problem, priced

The owner, 92,640 km from Ringside Exchange on nearly the same Saturn orbit (issue #155): the
armed autopilot DECLINED at ≈229 p — correct under the legacy loop, absurd against the
geometry. Near-co-orbital is Lambert's structural blind spot (a phasing loop returns to its
own start = the 2π singularity), so #152's porkchop finds nothing and falls back. The closed
form prices the same catch-up at **39 m/s ≈ 1 pulse** (k=1 dip) — with an 18-day wait,
because Ringside's lane period is 18.5 d. At Enceladus radii (33 h lane) the same math waits
hours. Hence the REAL product: a **cheaper-vs-sooner trade table** (owner: "comes in handy
when there is heat on us"), never a single silent answer.

## 1. Planner — rendezvous mode (OPUS LANE 1)

Extend `TransferPlanner.Solve` (do NOT touch `TransferMath`):

- **When:** same parent, and |r_ship − r_target| ≤ 7.5% of r_target (parent-relative radii at
  solve time). Run IN ADDITION to the existing porkchop; merge results.
- **Candidates:** k = 1..6 × both families via `TransferMath.PhasingOrbit` at the SHIP's
  current parent-relative radius, gap from `TransferMath.PhaseGap(shipRel, targetRel)`
  evaluated at a departure epoch t_dep = now + 600 s (fixed prep offset, deterministic).
  Feasibility per candidate: `Periapsis > OrbitRule.ParentSafeBodyRadii × parent.BodyRadius`;
  `Apoapsis < 0.9 × HillRadius(parent vs its own parent)` (skip the Hill check when the parent
  is the sun-orbiting root's… parent is a planet here — compute parent's Hill from the sun);
  `WaitSeconds ≤ MaxWaitSeconds` when the caller set one (>0). NOTE the default-wait rule from
  the moon-run spec (1.25 × synodic) is the WRONG scale here — when MaxWaitSeconds ≤ 0 the
  rendezvous mode considers all k ≤ 6.
- **Schedule per candidate — exactly two burns, both computable at solve time:**
  1. t_dep: Δv₁ = v_phasing_vector − shipRelVel, where v_phasing_vector = local prograde unit
     (radial unit rotated +90°, CCW rails) × the plan's phasing speed
     `√(μ(2/r − 1/SemiMajorAxis))`. (This also cleans up any small radial drift the ship
     carries — priced honestly since Δv is a vector difference.)
  2. t_rdv = t_dep + WaitSeconds: Δv₂ = targetRelVel(t_rdv) − v_phasing_vector (apsis-to-apsis
     integer revolutions return the ship to the burn point with the same velocity vector, so
     the exit burn is known at solve time; the rehearsal absorbs the two-body lie).
  ArrivalTime = t_rdv. Refuse a ship whose parent-frame angular momentum is retrograde
  (cross(r, v_rel) < 0) — the rails are CCW; verbatim reason.
- **Result:** winner = cheapest feasible across {porkchop cells} ∪ {phasing candidates}. Add
  to `Result`: `IReadOnlyList<Alternative> Alternatives` where
  `readonly record struct Alternative(string Label, double DeltaVTotal, int EstimatedPulses,
  double WaitSeconds, double ArrivalTime)` — the sorted trade table (winner first, then by
  arrival time), INCLUDING the direct-hop row when the porkchop found one. The UI's
  sooner-vs-cheaper choice reads this; today only the winner is flown.
- **Pulses:** same public `OrbitRule.PulsesFor` at world speeds (burn 1 at ship world speed at
  t_dep; burn 2 at target world speed at t_rdv).
- **Tests** (`TransferPlannerRendezvousTests.cs`): Ringside case (ship on the exchange's rail,
  92,640 km behind) → Ok, winner TotalΔv < 100 m/s, 2 burns, Alternatives ≥ 3 with strictly
  increasing wait for the dip family; flown gate — apply both burns through the real
  `Simulator` at their epochs, assert at ArrivalTime the ship is within 5e8 m of the target
  and < 100 m/s relative (two-body lie margin); determinism; retrograde-ship refusal;
  moon-to-moon unaffected (Enceladus→Titan still picks the porkchop hop; assert winner ≈
  the #152 numbers).

## 2. Rehearsal + wiring (OPUS LANE 3)

- `Rehearse` already executes multi-burn schedules and gates decisions until ArrivalTime —
  unchanged for moons. Add the STATION arrival criterion: when the target's `Kind ==
  BodyKind.Station`, "captured" = within the dock envelope at the gate (read the envelope
  constants from the one place Map.razor's dock coaching reads them — move them into Core if
  they're client-side literals, e.g. `DockRule.EnvelopeMeters/MatchSpeed`; the screenshot
  says "coast within 500,000 km, ≤8 km/s") with pulses as spent. No `OrbitRule.Insert` for a
  μ=0 body.
- Live loop: when the armed target is a station and the schedule completes, the autopilot
  stands the ship into the envelope, matches, and the status line says the dock is ready
  ("in the envelope — hit ⚓ Dock"); do NOT auto-clamp (docking stays the captain's click
  today; #160's tutorial may revisit).
- Nav-target box: the arm button works for stations via the same `ToggleArmedInsertion` path;
  label it "🛰 Autopilot to here" for a station target. The banner/summary shows the winner's
  one-line quote; if `Alternatives.Count > 1`, append "(+N other windows)" — the full table
  UI is #159/D2 territory, not this lane.
- Tests: rehearse-with-schedule on the Ringside case captures within the envelope under
  30 pulses; legacy comparison (the 229-p decline geometry) documented in the test name.

## 3. Lab 24 "The last mile" (OPUS LANE 2)

`labs/24-the-last-mile/`, README from the lead's draft (Lab23 pattern — ⟨RUN⟩ placeholders,
IRONCLAD rule). Sections: A the last mile is the expensive mile (legacy loop flown from
92,640 km vs the closed form); B the bus math (families, closure check against the rails,
k-table); C flown end-to-end through the real N-body sim (enter → k laps → exit → inside the
dock envelope), planner + rehearsal composed; D the tactical table (cheaper-vs-sooner, direct
hop row included — the heat-on-us reading). `--viz`: Saturn-centric, the phasing loop and the
direct hop as toggleable groups. Gate `Lab24LastMileTests.cs`, lab-19 style bands.

## 4. Order

1. Kernel ✔ (lead, ab51851). 2. Lane 1 → lead inspection. 3. Lanes 2 + 3 (lane 3 in a
worktree). 4. Owner playtest: `/map?start=saturn`, arm the exchange, watch it QUOTE the bus
table instead of declining.
