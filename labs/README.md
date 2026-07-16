# The Gravity Lab

A gravity-calculations tutorial series that does not simplify the way the textbooks do. Every
lesson computes numerically, in the messy situations a real integrator actually runs into —
using `SpaceSails.Core`, the exact deterministic engine the game itself flies ships with. This
is the magazine type-in listing, brought back: fork a lesson's probe, run it, break it on
purpose, and learn the programming and the physics at the same time. The game engine is the
lab equipment; you are not reading about orbital mechanics, you are running it.

Standard physics is presented as standard, with Curtis, *Orbital Mechanics for Engineering
Students*, as the reference the lab talks to by chapter. Wherever a lesson strays into the
game's fictional cosmology (mass drivers, ancients' pyramid satellites, the Electric Universe
layer), it says so plainly — **in this house we compute both, and we label which is which.**
The lab's honesty is the whole brand: every number printed in every lesson's `README.md` came
from actually running that lesson's probe. If you change a probe's code, its numbers go stale
— rerun it and re-paste, never hand-edit a table.

## How to run a lesson

Each lesson is a tiny console app that prints its own tables straight to stdout:

```bash
dotnet run --project labs/01-falling-is-orbiting -c Release
dotnet run --project labs/02-the-integrator-zoo -c Release
dotnet run --project labs/03-time-step-is-a-lie-you-choose -c Release
dotnet run --project labs/04-the-ten-percent-pulse -c Release
dotnet run --project labs/05-transfers-without-formulas -c Release
dotnet run --project labs/06-closest-approach-found-honestly -c Release
dotnet run --project labs/07-hill-spheres-and-bus-stops -c Release
dotnet run --project labs/08-seeing-through-uncertainty -c Release
dotnet run --project labs/09-what-the-rails-hide -c Release
dotnet run --project labs/10-fast-enough-for-ten-thousand-x -c Release
dotnet run --project labs/11-the-electric-sandbox -c Release
dotnet run --project labs/12-oops-at-the-moon -c Release
dotnet run --project labs/13-shooting-literally -c Release
dotnet run --project labs/14-two-points-and-a-clock -c Release
dotnet run --project labs/15-the-long-passage -c Release
dotnet run --project labs/16-going-ashore -c Release
dotnet run --project labs/17-the-pocket-solar-system -c Release
dotnet run --project labs/18-the-free-return -c Release
dotnet run --project labs/19-the-grand-tour -c Release
dotnet run --project labs/20-the-long-goodbye -c Release
dotnet run --project labs/22-the-air-brake -c Release
dotnet run --project labs/23-the-moon-run -c Release
dotnet run --project labs/24-the-last-mile -c Release
dotnet run --project labs/28-the-pump-crawl -c Release
```

Each lesson folder holds:

- `README.md` — the lesson: motivation, the standard-textbook take (with Curtis chapter
  pointers), what the textbook simplifies away, the numerical experiment with real printed
  output, and 2-3 "break it" exercises that intentionally damage the computation.
- `Probe.cs` + a small `LabNN.csproj` referencing `src/SpaceSails.Core` directly — determinism
  means a probe's numbers transfer 1:1 into the live game.

## Seeing a lesson: `--viz`

Some lessons can draw what they compute. Append `-- --viz` to any supporting lesson:

```bash
dotnet run --project labs/19-the-grand-tour -c Release -- --viz
```

It prints exactly the same tables as always, then writes a single self-contained
`labviz/<slug>.html` (no external requests, no CDN, no fonts) and opens it in your default
browser. The pop-up is a small SpaceSails instrument: pan (drag) and zoom (wheel) a dark canvas
of bodies on their orbit rings, scrub or play a timeline that walks a ghost ship dot along the
trajectory, toggle legend groups, and read markers (burns, flybys, closest passes). The drawing
comes straight from the numbers the probe just computed — without `--viz`, stdout is
byte-identical to before. Add `--viz-no-open` to write the file without launching a browser, or
`--viz-out=<path>` to choose the destination.

![Lesson 19's pop-up: the flown Grand Tour, ghost ship at the Jupiter flyby](../docs/features/pics/lab-viz-grand-tour.png)
*Lesson 19's pop-up, scrubbed to the Jupiter flyby: the flown Earth→Jupiter→Saturn itinerary in
orange with its burn/flyby markers, the aim-offset sweep group at lower right, and the live
readout (5.204 AU, 28.98 km/s) interpolated from the integrator's own samples.*

**Supported so far:** lesson [01](01-falling-is-orbiting/README.md) (the minimal example),
lesson [19](19-the-grand-tour/README.md) (the showcase — the flown Grand Tour with its aim-offset
sweep as a toggleable group), lesson [20](20-the-long-goodbye/README.md) (the same tour coasted
to a fixed 2026 present, with calendar dates from 1977 to now and a "she is here now" finale marker),
lesson [23](23-the-moon-run/README.md) (the Saturn-centric moon run — the planned Lambert
transfer arc with its ghost ship and burn/closest markers, and Wednesday's spiral-of-resets as a
toggleable comparison group), and lesson [24](24-the-last-mile/README.md) (the last mile — the
co-orbital phasing loop with its enter/re-match markers, Ringside and the moons on their rings, and
the near-co-orbital direct hop and the legacy point-and-throttle chase as toggleable groups).

**Wiring a new lesson** takes about six lines around the sample lists the probe already has —
everything behind `LabViz.Wants(args)` so the no-flag output never changes:

```csharp
using SpaceSails.LabViz;
// ... after the probe's existing computation ...
if (LabViz.Wants(args))
{
    var viz = new VizScene("labNN-slug", "Lab NN — Title", "one-line subtitle");
    viz.AddBodies(ephemeris.Bodies);
    viz.AddPath("trajectory", samples, VizColors.Trajectory, ghost: true); // samples: IReadOnlyList<TrajectorySample>
    viz.AddMarker(t, position, "label", MarkerKinds.Burn);
    LabViz.Show(viz, args);
}
```

Add a `ProjectReference` to `labs/SpaceSails.LabViz/SpaceSails.LabViz.csproj` in the lesson's
`.csproj`. See [docs/features/lab-viz.md](../docs/features/lab-viz.md) for the viewer tour and
`docs/lab-viz-spec.md` for the internals.

## The ladder

1. [**Falling is orbiting**](01-falling-is-orbiting/README.md) — two-body freefall computed
   step by step; vis-viva checked numerically against the integrator, including the game's own
   ±10% pulse turning a circle into an ellipse.
2. [**The integrator zoo**](02-the-integrator-zoo/README.md) — explicit Euler vs. the game's
   semi-implicit Euler vs. RK4 on one Mercury year and fifty: energy drift measured and
   tabulated, and why "smaller error" and "bounded error" are different guarantees.
3. [**Time step is a lie you choose**](03-time-step-is-a-lie-you-choose/README.md) — fixed dt
   vs. the game's adaptive `ProjectAdaptive` on a sun-grazing hyperbolic flyby: cost vs. accuracy,
   and a case where the adaptive default doesn't automatically win.
4. [**The ±10% pulse**](04-the-ten-percent-pulse/README.md) — impulse quantization and the Oberth
   effect, measured from the game's own numbers.
5. [**Transfers without formulas**](05-transfers-without-formulas/README.md) — Hohmann analytic
   (Curtis ch. 6) vs. `RoutePlanner`'s grid search on Earth→Mars.
6. [**Closest approach, found honestly**](06-closest-approach-found-honestly/README.md) —
   scanning vs. parabola-on-d² refinement, the same technique the planner and the closest-pass
   warning use.
7. [**Hill spheres and bus stops**](07-hill-spheres-and-bus-stops/README.md) — sphere-of-influence
   checked numerically against `OrbitRule`'s formula (a jagged stability structure, not a clean
   line), plus orbit-insertion Δv priced in pulses and why the 5 km/s window exists.
8. [**Seeing through uncertainty**](08-seeing-through-uncertainty/README.md) — a real NPC's
   observation → prediction cone (`PathPredictor`) checked against its true hidden flight, then
   telescope track quality (`TrackingStation`) converted directly into boarding-envelope odds.
9. [**What the rails hide**](09-what-the-rails-hide/README.md) — a true from-scratch n-body
   integrator vs. the rails ephemeris: per-planet drift, transfer-plan divergence, sensitivity to a
   1-meter nudge, and why patched approximations are used on purpose (and where they aren't safe).
10. [**Fast enough for 10,000×**](10-fast-enough-for-ten-thousand-x/README.md) — the M5/M19
    performance war stories, reproduced honestly on a dev machine: `RunAdaptive`'s real per-call
    cost, one ship vs. the game's actual 23-NPC roster, and the determinism constraint
    (byte-identical below warp 100) verified rather than asserted.
11. [**The Electric Sandbox**](11-the-electric-sandbox/README.md) ⚡ — `PlasmaEnvironment`'s real
    halo and stream mechanics computed straight, plus a clearly labeled speculative playground:
    "what if effective μ depended on the electrical environment?"
12. [**Oops at the Moon**](12-oops-at-the-moon/README.md) 🌙 — the finale: the one lesson that
    un-rails a body. A velocity kick to Luna's orbit (the game's own ±% pulse mechanic, by
    accident), integrated as a genuine free body — departs, degrades, or spirals in, computed
    honestly, plus a playable `scenarios/oops.json` aftermath.
13. [**Shooting, literally**](13-shooting-literally/README.md) 🎖 — the encore: the firing
    solution as a boundary value problem, solved by the shooting method — Newton over launch
    bearing and charge, every residual flown through the real integrator (the war room's
    CALCULATING FIRING SOLUTION trace, reproduced). Straight-line gunnery misses by 5,996 km;
    one Newton step lands 85 km out. Then the honest part: dispersion is the target *track's*
    cone, not the solver's residual — fire-control quality IS track quality.
14. [**Two points and a clock**](14-two-points-and-a-clock/README.md) — Lambert's problem
    (Curtis ch. 5, universal variables, implemented in the probe) meets the shooting method:
    Lambert is exactly right about a universe with one attractor, misses by 443,569 km in the
    one with nine, and Newton through the real integrator fixes it in ONE step for 14 m/s.
    Plus a verified porkchop plate — and the plot twist of a floor *below* Hohmann, explained
    honestly (the spawn point's 5,000,000 km head start, not a broken theorem).
15. [**The long passage**](15-the-long-passage/README.md) — six years to Saturn, where small
    numbers stop being small: Hohmann's tyranny table (Neptune: ~30.6 YEARS), a shooting-solved
    passage through Jupiter country (Lambert's two-body lie now costs 150 m/s, not 14), 1 m/s of
    departure error compounding ~4× into 718,250 km, and the navigator's oldest law computed —
    the same sin absolved at day 30 costs 1.26 m/s, on the deathbed 348 m/s.
16. [**Going ashore**](16-going-ashore/README.md) — moons: bus stops nested inside bus stops.
    The Enceladus insertion window is a 444 km shell (the haven that barely exists); Luna
    parking orbits stress-tested prograde AND retrograde at two time steps — the game's cruise
    ceiling doesn't blur the classical stability map (prograde ~0.5 Hill, retrograde ~0.9), it
    nearly INVERTS it; and the series' first landing: de-orbit dv, fall time, and touchdown
    speed per moon, the Luna row flown to verify (759 min analytic, 760 flown).
17. [**The pocket solar system**](17-the-pocket-solar-system/README.md) — Saturn's moons are
    the whole course at 1/1000 scale: Enceladus→Titan has Earth→Jupiter's radius ratio with a
    3.7-DAY Hohmann and a window every 36 hours; lessons 14-15's toolchain transfers verbatim;
    and the sun's share of the correction, isolated by construction, is 0.04 m/s — pocket
    systems aren't just fast, they're CLEAN.
18. [**The free return**](18-the-free-return/README.md) — the trajectory from *The Martian*:
    leave Earth, swing past Mars ballistically, come home, never burning since departure. No
    formula produces it — a grid search finds it (depart day 30, 4250 m/s → Mars at 4.27M km,
    Earth again at 4.79M km). The cycler's fare is lesson 4's unavoidable quantization sin, and
    the honest surprise is that pinning *less* costs less: this figure's Mars pass is sun-steered
    timetabling, not gravity theft, so the appointment tolerates a floating pass. Round-trip
    corrections run 358–521 m/s vs ~11,200 m/s for two Hohmann tickets — Rich Purnell, computed.

19. [**The Grand Tour (Voyager)**](19-the-grand-tour/README.md) — gravity theft for real: Earth→Jupiter→Saturn
    (stretch Uranus/Neptune) using the crank. Launch cheaper than direct Saturn; measure one flyby
    (speeds, turning angle, patched-conic comparison); chain legs with explicit TCMs; verify energy
    sign flip and symmetry; grid-scan the window. Every number from the integrator; rails create
    the energy. Break the assists on purpose.

20. [**The Long Goodbye**](20-the-long-goodbye/README.md) — the sequel lesson 19 left unwritten: the
    real Voyager 2 rode the crank on to Uranus and Neptune; ours stopped at Saturn. Reproduce lesson
    19's hand-off, fly ballistically THROUGH the Saturn pass (it *brakes* — the arc stays bound at 10.4
    AU, measured not assumed), then sweep affordable pre-Saturn TCM-3s for a second crank to Uranus and
    find none — our rails carry no 1977 alignment. Coast the winner to a fixed 2026-07-14 present and
    read her position (6.6 AU, falling back) beside the real Voyager 2's ~139 AU. Alignment was the
    mission; the crank was only the vehicle.

21. **The Commuter** — *reserved* (see [docs/TuesdayPlan/TuesdayPlanVision.md](../docs/TuesdayPlan/TuesdayPlanVision.md)):
    an Earth↔Mars cycler computed honestly in our rails, landing next to lesson 18's free return.

22. [**The air brake**](22-the-air-brake/README.md) — the one new Core ingredient the flight assists
    needed: an exponential atmosphere with a single ballistic-coefficient knob. Compute the aerobrake
    corridor at Jupiter (Δv shed vs. depth, the 3 g damage line), the Apollo skip at Earth-return
    speed (the capture-without-burn-up corridor is ~20 km of periapsis altitude wide), and a fuel-out
    capture flown pass by pass — a hyperbolic arrival turned bound (114 → 7 R_J) on drag alone, zero
    burns after the aim. The same drag powers the game's skim/skip node; `--viz` draws the corridor
    fan and the capture spiral.

23. [**The moon run**](23-the-moon-run/README.md) — the lesson where the game's autopilot learns
    orbital mechanics. Wednesday's reset loop hemorrhaged fuel fighting Saturn (#146) — flown honestly
    from the Enceladus doorstep it burns 677 pulses (54.5 km/s) to reach Titan, a hop the geometry
    prices at 6.04 km/s. Reproduce the hemorrhage honestly, then teach the
    fix the game now flies (`TransferMath` + `TransferPlanner`, Curtis chs. 2/5/6): plan in the giant's
    frame — the window is a 36.8 h bus timetable, the porkchop's floor IS Hohmann (6.02 km/s), and the
    planner's one prograde departure burn, flown end to end in the real N-body sim, passes 73 Mm from
    Titan and captures for **96 pulses total — a 7.1× cut**. `--viz` draws the Saturn-centric arc with
    the old spiral as a toggleable comparison.

24. [**The last mile**](24-the-last-mile/README.md) — the lesson where "almost there" is its own
    orbital problem. The owner, 92,640 km behind Ringside Exchange on the SAME Saturn lane, watched
    the autopilot decline at 229 pulses: Lambert can't price a phasing loop (it sweeps 2π, the blind
    spot), and pointing-and-throttling costs **4.0 km/s** for one brute approach that arrives at 4 km/s
    it can't shed. The fix is 1960s rendezvous doctrine — change your PERIOD, not your path: dip inside,
    coast k laps, re-match. The bus math closes to machine epsilon against the Kepler rate, but Ringside's
    authored rail is 0.024% off Kepler, so per-lap drift bends the cheapest bus to **k=2 dip at 27.5 m/s**
    (not the k=6 the Δv formula alone would pick). Flown two-burn through the real N-body sim, it closes
    the 92,640 km along-track gap and coasts to **4.9 Mm at 2 m/s matched** — inside the 500,000 km / ≤8 km/s
    dock envelope with 100× margin, for **2 pulses**. `TransferPlanner.Solve` returns the whole
    cheaper-vs-sooner trade table (the "heat on us" tactic); `--viz` draws the phasing loop with the
    direct hop and the legacy chase as toggleable groups.

28. [**The pump crawl**](28-the-pump-crawl/README.md) — the lesson where the ship learns to ask *can I
    still refuel?* before the tank answers for it (#146/#157/#166). Depots ride the rails at planets,
    stations and havens but **never at an ordinary moon**, so Titan and Luna are dry. Priced with the
    game's own `TransferPlanner` and pulse kernel, the reach to the nearest pump is **29 pulses** from
    a parked-at-Titan doorstep (Ringside) or **77** to the always-there haven (Enceladus) — the reliable
    reach already **exceeds the flat 45-p / 18% autopilot reserve**, the #146 starve in one number. The
    red line per region: **27–29 p** in the Saturn well, **infinite at Luna** (its only depot host is a
    LEO station the 5 km/s capture cap can't match — a real gap for #157). Ships a Core service,
    `FuelReachability.Assess`, that returns a well-aware verdict — Comfortable / Thin / CannotReachAPump —
    for the #166 banner and 🦜 parrot squawk; the amber floor `reach + 18%` rides the crawl instead of a
    flat fraction the well outgrows.
29. [**The harbor pattern**](29-the-harbor-pattern/README.md) — the lesson where the ship learns what
    a safe arrival looks like. The owner kept landing WRONG: in orbit "by luck" at Enceladus (#180),
    stuck near a station with no way to deliver (#175). Two harbor doors, measured by flying a spread of
    inbound trajectories through the real sim: a **station** clamp bubble (500,000 km / ≤8 km/s) never
    refuses you but bills a pulse per ~1% of world speed you arrive hot with — a clean **4 km/s** coast
    clamps for **0 pulses**, a 16 km/s botch for **68**; a **moon** door is the **949 km** Hill sphere
    where a too-hot fall genuinely IMPACTS — 1 km/s parks for **9 pulses**, 3 km/s hits the moon. The
    corridor is a constant-time-to-go glideslope (`v ≤ range/τ`, τ anchored on the autopilot's own
    closing speed), embedded as `ApproachCorridor` in Core with a tested `Read(range, speed)` →
    OnPattern/Hot/Missed + next-gate query — the seam the banner NEXT row (#159) and the #160 tutorial
    narration speak. `--viz` draws the Enceladus corridor: a textbook fall coiling into a park beside a
    botched one punching through.
30. [**The mass-driver timetable**](30-the-mass-driver-timetable/README.md) — the lesson where the
    owner's canon (Luna's mass drivers lobbing compute-core pods) becomes measured physics. A pod has
    zero maneuver budget — the driver gives it everything at launch — so its whole future is a
    closed-form Kepler conic, a rail (`TransferMath.PropagateKepler`). Above the **2.376 km/s** lunar
    escape floor, a **retrograde** lob of ~2.6 km/s drops perihelion into Venus's lane, and ~**7.6 km/s**
    threads perihelion to **0.373 AU** — the showpiece long shot to the Mercury compute yards; prograde
    lobs climb toward Mars. The repeating cadence (`MassDriverSchedule`) is a bus timetable read off the
    rail — half the pods always already in flight. The tactical punchline, priced with the game's own
    `OrbitRule.PulsesFor`: a pod is cheapest at the launch end — **loiter-and-match near Earth for ~3.5
    km/s (12 pulses)** vs a full Venus chase at **5202 m/s (17 pulses)**; chase it mid-dive and you buy
    the transfer yourself. `--viz` draws the launch fan off Luna over the inner system.

## Framing rule

Standard physics is presented as standard; Curtis is the reference. The EU-flavored lessons
(11, 12) are explicitly labeled as the game's fictional cosmology / speculative playgrounds.
See `docs/SaturdayPlan/GravityLab.md` for the full plan and PR lanes.
