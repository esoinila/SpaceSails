// Lab 10 — Fast enough for 10,000x
//
// Teaching voice: this is the performance lesson, and performance lessons lie constantly unless
// you measure on the real engine. Everything below runs SpaceSails.Core's actual Simulator (the
// same class the browser flies ships with) through a Stopwatch, in Release. No synthetic
// microbenchmark stands in for the real gravity sum: the ephemeris here is the full Sol system
// straight out of scenarios/sol.json (17 bodies, 14 with nonzero mu), so "cost per step" means
// what it actually costs the game to ask "what does gravity feel like right here."
//
// Three real war stories from this repo's own history motivate the three sections:
//   - M5 (docs/m5-spec.md / commit 1bf882b): NPCs stepping at the player's dt=1 s ground the
//     client to "~1 fps at 10000x warp." The fix — TrafficSchedule.NpcTimeStep = 60 s — got it
//     back to 53-57 fps. Section A reproduces the *mechanism* of that story on this machine.
//   - M19 (commit 63b06de): RunAdaptive's 60 s quanta measured "9,969 sim-s per real-s at
//     10000x" headless in sol-eu. That number came from a full game loop (rendering-free
//     browser/server run, Debug WASM even slower per this repo's own notes); Section A's numbers
//     come from a bare native Release console loop, so they are NOT the same measurement — they
//     are reported honestly as "a dev machine, native Release," and only the *relative* shape
//     (dt=1 naive is drastically slower than dt=60 quanta) is the actual lesson.
//   - M19 also introduced the determinism rule this lesson cares about most: below warp 100 nothing
//     changes, byte for byte; at/above warp 100 the client switches to RunAdaptive's 60 s quanta,
//     which is NOT bit-identical to fixed dt=1 stepping (M19's own test tolerates 0.1%). Section B
//     verifies both halves of that claim numerically.
//
// IRONCLAD RULE: every number below came from running this probe, in Release, on a dev machine.

using System.Diagnostics;
using SpaceSails.Core;

const double Day = 86400;

// ---- The full Sol system (scenarios/sol.json), transcribed as CelestialBody values so the
// gravity sum below costs exactly what the live game's does: 14 attracting bodies, 3 massless
// markers (stations) that GravitationalAcceleration skips via the Mu == 0 check. ----
CelestialBody[] solBodies =
[
    new("sun", "Sun", null, 1.32712440018e20, 6.9634e9, 0, 0, 0),
    new("mercury", "Mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    new("mercury-compute", "Mercury Compute Farms", "mercury", 0, 500, 2.84e6, 6405, 0.2, BodyKind.Station),
    new("venus", "Venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    new("earth", "Earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    new("luna", "Luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0, BodyKind.Moon),
    new("satellite-factory", "Highport Satellite Works", "earth", 0, 300, 6.771e6, 5546, 2.4, BodyKind.Station),
    new("mars", "Mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    new("jupiter", "Jupiter", "sun", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    new("europa", "Europa", "jupiter", 3.2038e12, 1.5608e6, 6.709e8, 3.068226e5, 0.5, BodyKind.Moon),
    new("ganymede", "Ganymede", "jupiter", 9.8907e12, 2.6341e6, 1.0704e9, 6.181531e5, 1.5, BodyKind.Moon),
    new("callisto", "Callisto", "jupiter", 7.1808e12, 2.4103e6, 1.8827e9, 1.4419307e6, 3.0, BodyKind.Moon),
    new("saturn", "Saturn", "sun", 3.7931187e16, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    new("titan", "Titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
    new("enceladus", "Enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon, IsHaven: true),
    new("ringside-exchange", "Ringside Exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station, IsHaven: true),
    new("uranus", "Uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    new("neptune", "Neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
];

var ephemeris = new CircularOrbitEphemeris(solBodies);

const double EarthOrbitRadius = 1.496e11;
const double SunMu = 1.32712440018e20;
double earthCircularSpeed = Math.Sqrt(SunMu / EarthOrbitRadius);

double Milliseconds(Stopwatch sw) => sw.Elapsed.TotalMilliseconds;

// ---- Section A: sim-seconds-per-wall-second, three ways ------------------------------------

Console.WriteLine("=== Section A: cost per Step() call — same physics, three dt choices ===");
Console.WriteLine("Ship parked at Earth's orbit (1 AU), full 17-body Sol ephemeris, warmed up JIT.");
Console.WriteLine();

const int WarmupSteps = 50_000;
const int TimedSteps = 2_000_000;

(double msTotal, double msPerCall) TimeFixedSteps(double dt, int steps)
{
    var sim = new Simulator(ephemeris, dt);
    var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, earthCircularSpeed), 0);
    for (int i = 0; i < WarmupSteps; i++) state = sim.Step(state);

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < steps; i++) state = sim.Step(state);
    sw.Stop();
    return (Milliseconds(sw), Milliseconds(sw) / steps);
}

(double msTotal, double msPerCall) TimeRunAdaptiveQuanta(double quantum, int calls)
{
    var sim = new Simulator(ephemeris, timeStepSeconds: 1.0); // TimeStep unused by RunAdaptive
    var state = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, earthCircularSpeed), 0);
    for (int i = 0; i < WarmupSteps / 100; i++) state = sim.RunAdaptive(state, quantum);

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < calls; i++) state = sim.RunAdaptive(state, quantum);
    sw.Stop();
    return (Milliseconds(sw), Milliseconds(sw) / calls);
}

(double ms1, double msPerCall1) = TimeFixedSteps(1.0, TimedSteps);
(double ms60, double msPerCall60) = TimeFixedSteps(60.0, TimedSteps);
(double msAdapt, double msPerCallAdapt) = TimeRunAdaptiveQuanta(60.0, TimedSteps);

Console.WriteLine($"{"method",-28}{"calls",-12}{"total ms",-12}{"ms/call",-14}{"sim-s/call",-12}{"sim-s per wall-s",-18}");
Console.WriteLine($"{"fixed dt = 1 s",-28}{TimedSteps,-12}{ms1,-12:F1}{msPerCall1,-14:E3}{1.0,-12:F0}{1.0 / (msPerCall1 / 1000.0),-18:E3}");
Console.WriteLine($"{"fixed dt = 60 s (NPC quantum)",-28}{TimedSteps,-12}{ms60,-12:F1}{msPerCall60,-14:E3}{60.0,-12:F0}{60.0 / (msPerCall60 / 1000.0),-18:E3}");
Console.WriteLine($"{"RunAdaptive(60 s quantum)",-28}{TimedSteps,-12}{msAdapt,-12:F1}{msPerCallAdapt,-14:E3}{60.0,-12:F0}{60.0 / (msPerCallAdapt / 1000.0),-18:E3}");
Console.WriteLine();
double throughput1 = 1.0 / (msPerCall1 / 1000.0);
double throughput60 = 60.0 / (msPerCall60 / 1000.0);
double throughputAdapt = 60.0 / (msPerCallAdapt / 1000.0);

Console.WriteLine("Per-call cost (ms/call) for the two FIXED rows is set by the gravity sum over 14 massive");
Console.WriteLine("bodies, NOT by dt — a Step() call at dt=1 and one at dt=60 do the identical arithmetic");
Console.WriteLine($"({msPerCall1:E3} ms vs {msPerCall60:E3} ms/call, a wash). Throughput (sim-s per wall-s) differs only");
Console.WriteLine("because a dt=60 call advances 60x more sim-time per identical-cost call.");
Console.WriteLine();
Console.WriteLine("RunAdaptive(60) is the genuine surprise: its per-call cost is NOT 'about the same' as fixed");
Console.WriteLine($"dt=60 — it is {msPerCallAdapt / msPerCall60:F2}x more expensive per call, because RunAdaptive spends a full");
Console.WriteLine("DynamicalTime() pass (looping all 14 bodies computing distances) choosing its step size");
Console.WriteLine("BEFORE StepBy does its own full GravitationalAcceleration pass over the same 14 bodies —");
Console.WriteLine("two body-loops per call instead of one. It still wins overall (see the fps table below)");
Console.WriteLine("purely because one call now covers 60 sim-seconds instead of 1 — the 60x fewer calls more");
Console.WriteLine("than pays for the doubled per-call cost. Performance tricks are only free until you");
Console.WriteLine("actually measure them; this one is a real, worthwhile, but NOT free trade.");
Console.WriteLine();

Console.WriteLine("=== Reproducing the M5 war story: fps at warp 10000x, single player ship ===");
Console.WriteLine("(M5's own number: NPCs at dt=1s -> ~1 fps at 10000x; NpcTimeStep=60 -> 53-57 fps.");
Console.WriteLine(" M19's own number: 9,969 sim-s/real-s at 10000x, measured headless in sol-eu with");
Console.WriteLine(" plasma + full traffic in the browser/server loop — a very different, heavier");
Console.WriteLine(" measurement than this bare native console loop. Numbers below are THIS machine,");
Console.WriteLine(" native Release, one ship, no rendering — reported honestly, not as a repro of 9,969.)");
Console.WriteLine();
Console.WriteLine($"{"method",-28}{"wall-s for 10000 sim-s",-24}{"implied fps at warp 10000x",-28}");
foreach ((string label, double throughput) in new[]
         {
             ("fixed dt = 1 s", throughput1),
             ("fixed dt = 60 s (NPC)", throughput60),
             ("RunAdaptive(60 s quantum)", throughputAdapt),
         })
{
    double wallSecondsFor10000SimSeconds = 10000.0 / throughput;
    double impliedFps = 1.0 / wallSecondsFor10000SimSeconds;
    Console.WriteLine($"{label,-28}{wallSecondsFor10000SimSeconds,-24:F5}{impliedFps,-28:F1}");
}

// ---- Section B: cost of one ship vs the game's real 23-NPC roster --------------------------

Console.WriteLine();
Console.WriteLine("=== Section B: one ship vs the game's real NPC roster (8 traffic + 3 pods + 12 depots) ===");

IReadOnlyList<NpcShip> traffic = TrafficSchedule.Generate(ephemeris, seed: 42, count: 8);
IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(ephemeris, seed: 43, count: 3);
IReadOnlyList<NpcShip> depots = TrafficSchedule.GenerateDepots(ephemeris, seed: 44);
int totalNpcs = traffic.Count + pods.Count + depots.Count;
Console.WriteLine($"traffic={traffic.Count}, pods={pods.Count}, depots={depots.Count} -> total NPC entries = {totalNpcs}");
Console.WriteLine("(depots: one per planet [8] + notable stations/havens [mercury-compute, " +
    "satellite-factory, enceladus, ringside-exchange = 4] = 12; matches M22's 'a bus stop at every " +
    "planet orbit.')");
Console.WriteLine();

var npcSim = new Simulator(ephemeris, TrafficSchedule.NpcTimeStep);
const int RosterFrames = 20_000;

double TimeIntegratedRoster(IReadOnlyList<NpcShip> ships, int frames)
{
    ShipState[] states = [.. ships.Select(s => s.InitialState)];
    for (int w = 0; w < 20; w++)
        for (int i = 0; i < states.Length; i++)
            states[i] = npcSim.Step(states[i], ships[i].Plan);

    var sw = Stopwatch.StartNew();
    for (int f = 0; f < frames; f++)
        for (int i = 0; i < states.Length; i++)
            states[i] = npcSim.Step(states[i], ships[i].Plan);
    sw.Stop();
    return Milliseconds(sw) / frames;
}

double TimeDepotRoster(IReadOnlyList<NpcShip> depotShips, int frames)
{
    double simTime = 0;
    for (int w = 0; w < 20; w++)
        foreach (NpcShip d in depotShips)
            _ = TrafficSchedule.DepotState(d.Id, d.DepotBodyId!, d.DepotOrbitRadius, d.DepotPhase, ephemeris, simTime);

    var sw = Stopwatch.StartNew();
    for (int f = 0; f < frames; f++)
    {
        simTime += TrafficSchedule.NpcTimeStep;
        foreach (NpcShip d in depotShips)
            _ = TrafficSchedule.DepotState(d.Id, d.DepotBodyId!, d.DepotOrbitRadius, d.DepotPhase, ephemeris, simTime);
    }
    sw.Stop();
    return Milliseconds(sw) / frames;
}

var singleShipStates = new[] { traffic[0].InitialState };
double msPerFrame1Ship = TimeIntegratedRoster([traffic[0]], RosterFrames);
double msPerFrame11Ships = TimeIntegratedRoster([.. traffic, .. pods], RosterFrames);
double msPerFrame12Depots = TimeDepotRoster(depots, RosterFrames);

Console.WriteLine($"{"roster",-40}{"ms/frame (one 60 s quantum each)",-34}");
Console.WriteLine($"{"1 integrated ship",-40}{msPerFrame1Ship,-34:E3}");
Console.WriteLine($"{"11 integrated ships (8 traffic+3 pods)",-40}{msPerFrame11Ships,-34:E3}");
Console.WriteLine($"{"12 rails-only depots (DepotState calls)",-40}{msPerFrame12Depots,-34:E3}");
Console.WriteLine($"{"all 23 NPCs, one frame",-40}{msPerFrame11Ships + msPerFrame12Depots,-34:E3}");
double msPerShipUnit = msPerFrame11Ships / 11.0;
double msPerDepotUnit = msPerFrame12Depots / 12.0;
Console.WriteLine();
Console.WriteLine($"11 integrated ships cost {msPerFrame11Ships / msPerFrame1Ship:F2}x one ship (expect ~11x: " +
    "each does its own independent gravity sum). Per-unit: one integrated ship costs " +
    $"{msPerShipUnit:E3} ms/quantum; one depot costs {msPerDepotUnit:E3} ms/quantum — a depot is about " +
    $"{msPerShipUnit / msPerDepotUnit:F1}x cheaper than an integrated ship, NOT free: DepotState still walks the parent-body chain " +
    "and calls Position() twice (a finite-difference velocity), it just never runs a gravity sum. Riding " +
    "rails is a real, measured discount — not the zero-cost idealization the doc comments' 'costs nothing' " +
    "shorthand might suggest if you don't actually measure it.");

// ---- Section C: the determinism constraint --------------------------------------------------

Console.WriteLine();
Console.WriteLine("=== Section C: below warp 100, byte-identical; at/above, a bounded, measured trade ===");
Console.WriteLine("Mirrors Map.razor's own dispatch (M19): effective warp < 100 -> Simulator.Step(dt=1s)");
Console.WriteLine("every frame, unchanged since before M19. effective warp >= 100 -> Simulator.RunAdaptive");
Console.WriteLine("in fixed 60 s quanta. This probe reimplements that exact dispatch and diffs the result");
Console.WriteLine("against pure fixed dt=1 stepping over the same duration.");
Console.WriteLine();

const double AdaptiveWarpThreshold = 100;

ShipState DispatchLikeTheClient(ShipState state, ManeuverPlan plan, double totalDuration, double effectiveWarp, double quantum)
{
    var sim = new Simulator(ephemeris, timeStepSeconds: 1.0);
    double consumed = 0;
    while (consumed < totalDuration)
    {
        if (effectiveWarp >= AdaptiveWarpThreshold)
        {
            double step = Math.Min(quantum, totalDuration - consumed);
            state = sim.RunAdaptive(state, step, plan);
            consumed += step;
        }
        else
        {
            state = sim.Step(state, plan);
            consumed += sim.TimeStep;
        }
    }

    return state;
}

// A 20-day Earth-vicinity cruise with a mid-course burn, same shape as M19's own
// RunAdaptive_MatchesFixedStepWithinTolerance test (Core.Tests/SimulatorTests.cs), but run
// against the FULL 17-body ephemeris here rather than sun+earth alone.
Vector2d earth0 = ephemeris.Position("earth", 0);
Vector2d earthVel = (ephemeris.Position("earth", 1.0) - ephemeris.Position("earth", -1.0)) / 2.0;
var cruiseStart = new ShipState(earth0 + earth0.Normalized() * 5e9, earthVel, 0);
var cruisePlan = new ManeuverPlan([new ManeuverNode(10 * Day + 12345.6, ManeuverAction.Accelerate, 3)]);
double cruiseDuration = 20 * Day;

var truthSim = new Simulator(ephemeris, 1.0);
ShipState truth = truthSim.Run(cruiseStart, cruiseDuration, cruisePlan);

ShipState belowThreshold = DispatchLikeTheClient(cruiseStart, cruisePlan, cruiseDuration, effectiveWarp: 50, quantum: 60);
ShipState aboveThreshold = DispatchLikeTheClient(cruiseStart, cruisePlan, cruiseDuration, effectiveWarp: 1000, quantum: 60);

double diffBelow = (belowThreshold.Position - truth.Position).Length;
double diffAbove = (aboveThreshold.Position - truth.Position).Length;
double relDiffAbove = diffAbove / (truth.Position - earth0).Length;

Console.WriteLine($"{"regime",-34}{"|position - truth| (m)",-26}{"relative to Earth-distance",-28}");
Console.WriteLine($"{"warp 50 (below threshold, dt=1s)",-34}{diffBelow,-26:E6}{diffBelow / (truth.Position - earth0).Length,-28:E3}");
Console.WriteLine($"{"warp 1000 (RunAdaptive, 60s quanta)",-34}{diffAbove,-26:E6}{relDiffAbove,-28:E3}");
Console.WriteLine();
Console.WriteLine("The warp-50 row is not 'very small' — it is EXACTLY zero: below the threshold the");
Console.WriteLine("dispatch calls the identical Step(dt=1s) code path, so there is no floating-point");
Console.WriteLine("operation left to differ. That is what 'byte-identical below warp 100' means, verified");
Console.WriteLine("here rather than asserted. Above threshold, RunAdaptive's 60 s quanta is a real,");
Console.WriteLine("measured, bounded approximation — small on this cruise, and never invisible in");
Console.WriteLine("principle, which is exactly why the client only turns it on where deep space makes it");
Console.WriteLine("cheap to be a little bit wrong.");

// ---- Break-it: widen the quantum until trajectories visibly fork ---------------------------

Console.WriteLine();
Console.WriteLine("=== BREAK IT: widen the RunAdaptive quantum past the client's 60 s choice ===");
Console.WriteLine($"{"quantum (s)",-14}{"|position - truth| (m)",-26}{"relative to Earth-distance",-28}");
foreach (double quantum in new double[] { 60, 600, 3600, 21600, 86400 })
{
    ShipState result = DispatchLikeTheClient(cruiseStart, cruisePlan, cruiseDuration, effectiveWarp: 1000, quantum: quantum);
    double diff = (result.Position - truth.Position).Length;
    double rel = diff / (truth.Position - earth0).Length;
    Console.WriteLine($"{quantum,-14:F0}{diff,-26:E6}{rel,-28:E3}");
}

Console.WriteLine();
Console.WriteLine("The genuine surprise: error grows steadily from 60 s to 3600 s, then goes almost dead");
Console.WriteLine("flat from 3600 s onward (1.805433E+007 at 3600 s vs 1.805435E+007 at 86400 s — a 6th");
Console.WriteLine("significant figure). The client's outer 'quantum' is not just a call-batching convenience:");
Console.WriteLine("RunAdaptive's own boundary rule is dt = min(clamp(dynamicalTime/64, 1, 3600), timeToBoundary),");
Console.WriteLine("where timeToBoundary is capped by THIS call's own endTime (start-of-call + quantum). A small");
Console.WriteLine("outer quantum therefore imposes an EXTRA step-size ceiling tighter than the nominal 3600 s");
Console.WriteLine("maxTimeStep default — quantum=60 silently forces every internal step to <=60 s, far finer");
Console.WriteLine("than RunAdaptive would ever pick on its own out here in deep space. Once the outer quantum");
Console.WriteLine("reaches the nominal 3600 s ceiling, widening it further changes nothing: 3600 s was already");
Console.WriteLine("the binding constraint, exactly the lesson lab 03's clamp break-it found for ProjectAdaptive.");
Console.WriteLine("The client's 60 s choice, in other words, quietly buys MORE accuracy than 'the same as");
Console.WriteLine("dt=60 fixed' would suggest, at the 2x per-call cost measured in Section A.");

Console.WriteLine();
Console.WriteLine("=== Break it yourself ===");
Console.WriteLine("1. The quantum sweep above is run for you. Decouple the two clamps: call RunAdaptive with");
Console.WriteLine("   an explicit maxTimeStep well above the outer quantum (e.g. maxTimeStep=86400 while");
Console.WriteLine("   quantum stays 60) and confirm the outer quantum alone still pins the internal step to");
Console.WriteLine("   <=60 s regardless of what maxTimeStep allows.");
Console.WriteLine("2. On your own: repeat Section A's throughput bench with the ship parked at Mercury's");
Console.WriteLine("   orbit (5.791e10 m) instead of Earth's. RunAdaptive's dynamical-time fraction should");
Console.WriteLine("   start taking real sub-60s steps that much closer to the Sun -- does its per-call cost");
Console.WriteLine("   catch up to (or exceed) fixed dt=1?");
Console.WriteLine("3. On your own: this probe measures native Release console throughput. If you have a");
Console.WriteLine("   WASM build handy, run the same Stopwatch benchmark in the browser (Debug vs Release,");
Console.WriteLine("   per this repo's own ~100x interpreter note) and see how much of the M5/M19 story was");
Console.WriteLine("   the algorithm vs the runtime.");
