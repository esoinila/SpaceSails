# The moon run — in-well transfer math (#146) and Lab 23

*Thursday's main lane, spec'd. Lab-first, house pattern: the math lands as `SpaceSails.Core`
code, Lab 23 "The moon run" teaches it and prints the honest bill, and the same code becomes
the autopilot's in-well planner so the rehearsal (#151) starts quoting the CHEAP plan.*

## 0. The problem, priced

Wednesday night's Titan approach spent ~33 km/s (167 pulses) because `OrbitRule.Approach`
re-SETS the whole velocity vector (`bodyVelocity + 4 km/s toward aim`) every time Saturn's
pull drives the relative speed back over the 5 km/s cap. The well's work is thrown away and
re-bought every reset. The geometry, priced honestly in Saturn's frame (sol.json numbers,
μ_Saturn = 3.7931187e16 m³/s², r_Enceladus = 2.38037e8 m, r_Titan = 1.22183e9 m):

- circular speeds: v_Enc ≈ 12.62 km/s, v_Titan ≈ 5.57 km/s
- Hohmann Enceladus→Titan: **Δv₁ ≈ 3.71 km/s, Δv₂ ≈ 2.39 km/s, TOF ≈ 3.68 d**
- synodic window: every ≈ 36.0 h

Total ≈ 6.1 km/s — a ~5× saving before any refinement, and the arrival leg is what the
existing capture machinery (`OrbitRule.Insert`) already prices. Titan even has an atmosphere
(Lab 22's aerobrake) to shave Δv₂ later — out of scope today, noted in the lab.

## 1. Transfer flavor ruling: Lambert engine, Hohmann teacher

The morning decision (patched-Hohmann vs Lambert) is resolved by using each where it is
strongest, matching the lab ladder (05 → 14 → 17):

- **Hohmann** (closed form) supplies intuition, the TOF scale, the phase window, and the
  scan seed. It is exact only for circular, coplanar, apsis-to-apsis hops.
- **Lambert** (universal variables, Curtis Algorithm 5.2 — lab 14's solver, hardened) is the
  engine: it solves from the ship's ACTUAL state at any departure time to the moon's ACTUAL
  rail position at arrival, elliptical rails included, no apsis assumption.

Lambert proposes; the rehearsal (the real N-body integrator) disposes — same honesty split as
labs 14/15/17/19.

## 2. New Core surface

### 2.1 `TransferMath` (static, pure two-body kernels) — WRITTEN BY THE LEAD

File `src/SpaceSails.Core/TransferMath.cs`. Deterministic (fixed iteration caps, no
randomness, no wall clock). SI units, μ in m³/s². All 2-D ecliptic (`Vector2d`).

```csharp
public readonly record struct LambertSolution(Vector2d V1, Vector2d V2, int Iterations);

// null = no honest single-revolution solution (geometry degenerate, TOF unreachable,
// or the verification re-check failed). Callers SKIP, never guess.
public static LambertSolution? Lambert(Vector2d r1, Vector2d r2, double tofSeconds,
                                       double mu, bool prograde = true);

public readonly record struct HohmannPlan(
    double DepartDeltaV, double ArriveDeltaV, double TransferSeconds, double SemiMajorAxis);
public static HohmannPlan Hohmann(double r1, double r2, double mu);

public static double SynodicPeriod(double period1, double period2);

// Target's lead angle over the ship at departure for a Hohmann arrival: α = π − n_target·TOF,
// normalized to (−π, π]. The wait until the next window follows from the current relative
// phase and the synodic rate.
public static double HohmannLeadAngle(double r1, double r2, double mu);

// The standardized rails velocity: central difference, h = 1 s (identical to the private
// copies in AutopilotRehearsal / labs; those can migrate to this later).
public static Vector2d BodyVelocity(ICelestialEphemeris ephemeris, string bodyId, double simTime);
```

**Lambert hardening over the lab-14 solver** (why the lead writes it):

1. **Adaptive lower bracket.** Lab 14 fixes z ∈ [−100, (2π)²). Short-TOF/hyperbolic cells in
   a porkchop scan can root below −100 and would falsely return "no solution". Expand the
   lower bracket by doubling from −1 until F(z) < 0 (cap ~−1e6, else null).
2. **Degenerate-geometry guards.** |sin Δθ| < 1e-9 (0° or 180° transfer) → null. 180° is
   Lambert's classic singularity (A = 0 divides f-and-g by zero); scans step over it. The
   lab documents this as "the 180° blind spot" — approach it with 179°, never hit it.
3. **Verification re-check kept and tightened**: achieved TOF must match the request within
   max(1 s, 1e-6·TOF); y(z) must be positive; |g| must be non-tiny. Fail → null, no sentinel
   values.
4. **Both arcs.** `prograde: false` selects the retrograde (long-way) branch explicitly
   instead of inferring from the cross product only.

### 2.2 `TransferPlanner` (static, `SlingPlanner`-shaped) — OPUS LANE 1

File `src/SpaceSails.Core/TransferPlanner.cs`.

```csharp
public readonly record struct Request(
    ShipState Ship,                  // state at solve time (arm click / lab departure)
    string ParentBodyId,             // the well being ridden ("saturn")
    string TargetBodyId,             // the moon to reach ("titan")
    double MaxWaitSeconds,           // how long we may coast for a window; default 1.25 × synodic
    double MaxDeltaV = 25_000);      // refuse anything worse (sanity ceiling)

public readonly record struct BurnStep(double SimTime, Vector2d DeltaV);

public readonly record struct Result(
    bool Ok,
    string? Failure,                 // shown verbatim, SlingPlanner style
    double DepartTime,
    double TimeOfFlightSeconds,
    IReadOnlyList<BurnStep> Burns,   // today: ONE departure burn; arrival is the existing
                                     // capture machinery's job once inside CaptureRange
    double ArrivalRelativeSpeed,     // |v_lambert2 − v_moon| — must be < OrbitRule.MaxRelativeSpeed
    double PlannedDeltaVTotal,       // departure burn + arrival matching, the honest quote
    int EstimatedPulses,             // priced with OrbitRule.PulsesFor at the burn states
    string Summary);                 // one human line for the flight-plan status

public static Result Solve(Simulator simulator, ICelestialEphemeris ephemeris, Request request);
```

**Solve algorithm (deterministic, WASM-cheap):**

1. Resolve parent + target; fail with a verbatim reason if the target does not orbit the
   parent, or the ship sits inside ANOTHER moon's Hill sphere (today's planner solves the
   free-flight-in-the-well case; parked-at-moon departure is a documented follow-up).
2. Coast-project the ship over the wait window once:
   `simulator.ProjectAdaptive(ship, null, MaxWaitSeconds, maxTimeStep: 900)` — candidate
   departure states are read off these samples (interpolate position/velocity at grid times
   by nearest sample; the grid rides real N-body coasting, not a circular assumption).
3. Porkchop scan in the PARENT's frame: departure time over the wait window (24 cells),
   TOF over [0.4, 1.6] × Hohmann TOF (12 cells). Each cell: Lambert from
   (shipPos − parentPos) to (targetPosAtArrival − parentPosAtArrival);
   cost = |V1 − (shipVel − parentVel)| + |targetRelVel(arrival) − V2|. Skip null cells and
   cells whose arrival relative speed ≥ OrbitRule.MaxRelativeSpeed (uncapturable) — and cells
   whose transfer-ellipse periapsis dips inside ParentSafeBodyRadii × parent radius
   (never thread Saturn; compute periapsis from the two-body elements of (r1, V1)).
4. Refine the best cell with one finer 5×5 pass (½-cell spacing), same filters.
5. Emit ONE departure `BurnStep`: ΔV = V1_parentFrame − shipRelVel, applied at DepartTime
   (world-frame Δv is identical — frame offsets cancel in deltas).
   `PlannedDeltaVTotal` = departure + arrival matching; `EstimatedPulses` prices departure at
   the ship's heliocentric speed at burn time and arrival at the target's heliocentric speed
   (both via `OrbitRule.PulsesFor`, made public — single pricing source with the live loop).
6. `Summary` e.g. `"transfer Enceladus→Titan: burn 3.71 km/s in 14.2 h, arrive in 3.7 d at
   2.39 km/s rel (est. 34 pulses)"`.

**Tests (`tests/SpaceSails.Core.Tests/TransferMathTests.cs`, `TransferPlannerTests.cs`):**

- Lambert circular-consistency: point on a circular orbit, r2 at Δθ ∈ {30°, 170°, 190°,
  350°}, TOF = Δθ/n → V1 and V2 both circular speed, tangential, rel tol 1e-6.
- Lambert two-body invariants: specific energy and angular momentum identical at both ends
  (rel tol 1e-9) for a spread of (Δθ, TOF) cells, elliptic AND hyperbolic (short TOF).
- Lambert↔Hohmann limit at 179°: Δv within 2% of closed form.
- Degenerate/unreachable cells return null (0°, 180°, absurd TOFs) — never NaN.
- Planner on the sol.json Saturn subset: finds a plan with PlannedDeltaVTotal < 8 km/s
  (vs ~33 km/s status quo) and arrival rel speed < 5 km/s; determinism (two identical calls,
  byte-identical results); refusal paths give verbatim reasons.
- House gate style: fly the planner's burn through the real `Simulator` and assert closest
  approach to Titan < Titan's CaptureRange — Lambert proposes, integrator disposes, within
  the tolerance a mid-course of < 200 m/s would close (lab 17 measured the pocket clean).

### 2.3 Autopilot + rehearsal wiring — OPUS LANE 3

- `AutopilotRehearsal.Rehearse` gains an optional `IReadOnlyList<TransferPlanner.BurnStep>?
  schedule = null`. Before the existing decision loop, it coasts to each burn time
  (`RunAdaptive` already lands exactly on node times via a temporary `ManeuverPlan`, or
  simply coast-to-time), applies the impulse, prices it with the same public
  `OrbitRule.PulsesFor`, then falls into the unchanged loop for the terminal capture.
- `Map.razor ToggleArmedInsertion`: when the target's parent owns moons and the ship is
  inside the parent's Hill sphere (`ShipInsideHill`, the #135 predicate) and outside the
  target's CaptureRange, call `TransferPlanner.Solve` first; if Ok, rehearse WITH the
  schedule and cache it beside `_autopilotPlanPath`. If the planner refuses, fall back to
  rehearsing the legacy behavior (never lose the old capability); the rehearsal still
  refuses unaffordable jobs exactly as #151 shipped.
- `CheckArmedInsertion`: with a cached schedule, execute due burns at their sim times
  (impulse + pulse spend, same reserve guard), and consult `AutopilotDecision` only inside
  the target's CaptureRange or once the schedule is exhausted. External handbacks keep the
  #147 loud path untouched.
- The #148 intended path now draws the REHEARSED transfer arc — no extra work, it rides
  `RehearsalResult.Path`.

## 3. Lab 23 "The moon run" — OPUS LANE 2

`labs/23-the-moon-run/` (Probe.cs, Lab23.csproj, README.md), viz-wired. The lesson is the
edutainment writeup of exactly the Core math above, in the house voice, every printed number
from a real run:

- **Section A — the bill as flown vs the bill as priced.** Reproduce the Wednesday
  hemorrhage honestly: fly the OLD `OrbitRule` approach loop Enceladus-doorstep→Titan in the
  real sim, count its pulses/Δv; print next to vis-viva + Hohmann's closed form. (Curtis
  ch. 6; lab 01's vis-viva, lab 07's pulse pricing.)
- **Section B — the window.** Phase angles, `HohmannLeadAngle`, synodic wait table over a
  full cycle (36 h); "the bus timetable" voice from lab 07.
- **Section C — Lambert rides the well.** `TransferPlanner.Solve` porkchop over one synodic
  cycle × TOF sweep; print the plate (lab 14's plate, pocket edition), mark the floor, and
  the 180° blind-spot column of nulls with the honest explanation (Curtis ch. 5).
- **Section D — flown.** Apply the planner's burn schedule in the real N-body `Simulator`,
  measure closest approach to Titan, hand over to `OrbitRule` capture, print the END-TO-END
  pulse bill old vs new (the headline: ~167 pulses → ~35). `--viz`: Saturn-centric scene,
  transfer arc ghost-shipped, burn/closest markers, the old spiral as a toggleable
  comparison group.
- **Break-it exercises:** hit 180° exactly; demand the Hohmann TOF at the WRONG phase;
  shrink MaxWait below the window and watch the refusal reason.
- Gate: `tests/SpaceSails.Core.Tests/Lab23MoonRunTests.cs`, lab-19-style (independent
  helpers, invariant bands, no exact-trajectory asserts).

## 4. Order of work

1. Lead: `TransferMath.cs` + `OrbitRule.PulsesFor` made public + scratch verification. ✔ this spec
2. Lane 1 (Opus): `TransferPlanner` + both test files. Inspection by lead.
3. Lane 2 (Opus): Lab 23 + README + viz + gate. Inspection by lead; numbers pasted from a real run only.
4. Lane 3 (Opus): rehearsal schedule + Map.razor wiring + Chrome verification on 5073.
5. Owner playtest: arm auto-orbit Titan from the well — watch it QUOTE CHEAP and fly a real
   transfer arc; the Enceladus→Titan milk run end to end.

Open owner decision preserved: the manual "transfer to <moon>" SOLVE button in the flight
plan (§3 of the day plan). Everything above is engine-side; the button is one
`AddSlingBurn`-shaped editor away whenever the ruling lands.
