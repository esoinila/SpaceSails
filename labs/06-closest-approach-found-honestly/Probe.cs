// Lab 06 — Closest approach, found honestly
//
// Teaching voice: the planner's closest-pass warning (M18, `ClosestApproach.cs`) has to answer
// "how close does this ribbon get to that planet" from a trajectory that's only ever known at a
// finite set of sampled points — a flyby's true minimum distance almost never lands exactly on
// one. `ClosestApproach.Passes` handles this with a coarse stride, a stride-1 refine around the
// coarse minimum, then a parabola fit on d² between the three bracketing samples. This lesson
// measures what that buys on a genuine Earth->Mars near-miss: naive per-step minimum vs. the
// refine, at several sample densities, judged against a much denser scan as ground truth. Along
// the way it finds a real surprise — the client's own adaptive stepping (lesson 3's dt =
// dynamical-time/64, clamped) does NOT automatically rescue a fast flyby either, for exactly the
// reason lesson 3 already found. Then it reproduces the M18 commit's own accept-style claim
// (predicted vs. actual within about 0.1%) at whatever density actually earns it, and breaks the
// refine two ways on purpose.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code, the
// printed numbers in labs/06-closest-approach-found-honestly/README.md go stale — rerun and
// re-paste.

using SpaceSails.Core;

const double Day = 86400.0;
const double AU = 1.496e11;

const double SunMu = 1.32712440018e20, SunBodyRadius = 6.9634e8;
const double EarthMu = 3.986004418e14, EarthOrbitRadius = AU, EarthPeriod = 3.1558149e7, EarthPhase = 1.8, EarthBodyRadius = 6.371e6;
const double MarsMu = 4.282837e13, MarsOrbitRadius = 2.2794e11, MarsPeriod = 5.93551e7, MarsPhase = 2.7, MarsBodyRadius = 3.3895e6;

var sun = new CelestialBody("sun", "Sun", null, SunMu, SunBodyRadius, 0, 0, 0);
var earth = new CelestialBody("earth", "Earth", "sun", EarthMu, EarthBodyRadius, EarthOrbitRadius, EarthPeriod, EarthPhase);
var mars = new CelestialBody("mars", "Mars", "sun", MarsMu, MarsBodyRadius, MarsOrbitRadius, MarsPeriod, MarsPhase);
var ephemeris = new CircularOrbitEphemeris([sun, earth, mars]);

// A genuine Earth->Mars near-miss: departure day 40 (lesson 5's own scan window), 3 accelerate
// pulses, flown as a COAST with no brake, so the ship actually flies past Mars. Mars's own
// (small but nonzero) mu is in this ephemeris, so the flyby genuinely perturbs the ship.
const double departureTime = 40 * Day;
ShipState departure = RoutePlanner.DepartureState(ephemeris, "earth", "mars", departureTime);
var plan = new ManeuverPlan([new ManeuverNode(departureTime + 3600, ManeuverAction.Accelerate, 3)]);
const double horizon = 90 * Day;

var sim = new Simulator(ephemeris, timeStepSeconds: 1.0); // TimeStep unused by ProjectAdaptive

IReadOnlyList<TrajectorySample> SampleUniform(double dt, ManeuverPlan p) =>
    sim.ProjectAdaptive(departure, p, horizon, minTimeStep: dt, maxTimeStep: dt, maxSamples: 2_000_000);

double MarsDistance(TrajectorySample s) => (s.Position - ephemeris.Position("mars", s.SimTime)).Length;

(double dist, double time) NaiveMin(IReadOnlyList<TrajectorySample> samples)
{
    double best = double.MaxValue, bestT = 0;
    foreach (TrajectorySample s in samples)
    {
        double d = MarsDistance(s);
        if (d < best) { (best, bestT) = (d, s.SimTime); }
    }

    return (best, bestT);
}

double RefineMin(IReadOnlyList<TrajectorySample> samples) =>
    ClosestApproach.Passes(samples, ephemeris, maxEvaluationsPerBody: samples.Count).First(p => p.BodyId == "mars").Distance;

// ===================================================================================
// Section A — ground truth: a very dense uniform scan
// ===================================================================================
Console.WriteLine("=== Section A: dense-scan ground truth ===");
IReadOnlyList<TrajectorySample> truth = SampleUniform(5.0, plan); // 5-second stride
(double truthDist, double truthTime) = NaiveMin(truth);
Console.WriteLine($"dt = 5 s, {truth.Count} samples: minimum distance = {truthDist / 1000.0:F3} km at t = {truthTime / Day:F5} days");
Console.WriteLine("This is this probe's stand-in for 'the true minimum' — dense enough that a coarser scan's");
Console.WriteLine("job is to approximate it cheaply, not to find something more accurate than it.");
Console.WriteLine();

// ===================================================================================
// Section B — naive scan vs. parabola refine, at three sample densities
// ===================================================================================
Console.WriteLine("=== Section B: naive per-step minimum vs. ClosestApproach's refine ===");
Console.WriteLine($"{"density",-14}{"samples",-10}{"naive (km)",-16}{"naive err %",-16}{"refine (km)",-16}{"refine err %",-16}");

(string label, double dt)[] densities = [("1 day", 86400.0), ("3 hours", 10800.0), ("10 minutes", 600.0)];
foreach ((string label, double dt) in densities)
{
    IReadOnlyList<TrajectorySample> samples = SampleUniform(dt, plan);
    (double naive, double _) = NaiveMin(samples);
    double refine = RefineMin(samples);
    double naiveErr = Math.Abs(naive - truthDist) / truthDist * 100.0;
    double refineErr = Math.Abs(refine - truthDist) / truthDist * 100.0;
    Console.WriteLine($"{label,-14}{samples.Count,-10}{naive / 1000.0,-16:F3}{naiveErr,-16:E3}{refine / 1000.0,-16:F3}{refineErr,-16:E3}");
}
Console.WriteLine();
Console.WriteLine("The refine should track the naive minimum from a MUCH denser scan more closely than the same-");
Console.WriteLine("density naive number does, for the cost of the coarse scan plus a handful of extra evaluations.");
Console.WriteLine();

// ===================================================================================
// Section C — the client's REAL setting is adaptive, and it does NOT automatically save this
// ===================================================================================
Console.WriteLine("=== Section C: the client's real (adaptive) stepping, and why it doesn't automatically win ===");
Console.WriteLine("Map.razor calls `_simulator.ProjectAdaptive(_ship, _plan, horizon, maxTimeStep: 3 * 3600, ...)` —");
Console.WriteLine("maxTimeStep is only a CEILING; the real step is lesson 3's dt = dynamical-time/64, clamped to it.");
Console.WriteLine();

IReadOnlyList<TrajectorySample> adaptiveSamples = sim.ProjectAdaptive(departure, plan, horizon, maxTimeStep: 3 * 3600, maxSamples: 500_000);
(double adaptiveNaive, double _) = NaiveMin(adaptiveSamples);
double adaptiveRefine = RefineMin(adaptiveSamples);
double adaptiveNaiveErr = Math.Abs(adaptiveNaive - truthDist) / truthDist * 100.0;
double adaptiveRefineErr = Math.Abs(adaptiveRefine - truthDist) / truthDist * 100.0;
Console.WriteLine($"adaptive (maxTimeStep = 3 h ceiling, same as the client), {adaptiveSamples.Count} samples: " +
    $"naive = {adaptiveNaive / 1000.0:F3} km (err {adaptiveNaiveErr:E3} %), refine = {adaptiveRefine / 1000.0:F3} km " +
    $"(err {adaptiveRefineErr:E3} %)");
Console.WriteLine();
Console.WriteLine("Samples straddling the true minimum (t = " + (truthTime / Day).ToString("F4") + " days):");
foreach (TrajectorySample s in adaptiveSamples.Where(s => Math.Abs(s.SimTime - truthTime) < 1.5 * Day))
{
    Console.WriteLine($"  t = {s.SimTime / Day:F4} days, dist = {MarsDistance(s) / 1000.0:F1} km");
}
Console.WriteLine();
Console.WriteLine("The surprise: adaptive stepping sizes the NEXT step from the CURRENT distance. Approaching this");
Console.WriteLine("planet, the ship is still outside the radius where dynamical-time/64 drops below the 3-hour");
Console.WriteLine("ceiling, so it takes one more full 3-hour step — and that single step carries it clean through");
Console.WriteLine("periapsis to the other side, never sampling anywhere near the true minimum. This is lesson 3's");
Console.WriteLine("own finding (\"adaptive doesn't automatically win\") showing up again here: it helps enormously");
Console.WriteLine("for a graze that lasts many steps, and does nothing for one fast enough to fit inside a single one.");
Console.WriteLine();

// ===================================================================================
// Section D — the M18-style accept check: how fine actually earns "within ~0.1%" here
// ===================================================================================
Console.WriteLine("=== Section D: M18-style accept check (predicted vs actual within ~0.1%) ===");
Console.WriteLine($"{"density",-14}{"samples",-10}{"refine (km)",-16}{"refine err %",-16}{"verdict",-10}");
(string label, double dt)[] fineDensities = [("10 minutes", 600.0), ("1 minute", 60.0), ("10 seconds", 10.0)];
double passDt = 0;
foreach ((string label, double dt) in fineDensities)
{
    IReadOnlyList<TrajectorySample> samples = SampleUniform(dt, plan);
    double refine = RefineMin(samples);
    double refineErr = Math.Abs(refine - truthDist) / truthDist * 100.0;
    string verdict = refineErr < 0.1 ? "PASS" : "fail";
    Console.WriteLine($"{label,-14}{samples.Count,-10}{refine / 1000.0,-16:F3}{refineErr,-16:E3}{verdict,-10}");
    if (refineErr < 0.1 && passDt == 0) { passDt = dt; }
}
Console.WriteLine();
Console.WriteLine(passDt > 0
    ? $"The ~0.1% M18-style tolerance is real and reachable here — it just needs samples every {passDt:F0} s"
      + " through this particular (fast, deep) encounter, far finer than the client's 3-hour ribbon ceiling."
    : "None of these densities reach 0.1% for this encounter — see the break-its below for why fast, deep grazes are the hard case.");
Console.WriteLine();

// ===================================================================================
// Break it #1 — put the true minimum exactly on a sample point
// ===================================================================================
Console.WriteLine("=== BREAK IT #1: land a sample exactly on the true minimum ===");
double elapsed = truthTime - departure.SimTime;
int n = (int)Math.Round(elapsed / 60.0); // target ~60 s stride — fine enough to integrate accurately (Section D)
double dtExact = elapsed / n;
IReadOnlyList<TrajectorySample> exactSamples = SampleUniform(dtExact, plan);
(double exactNaive, double exactNaiveTime) = NaiveMin(exactSamples);
double exactRefine = RefineMin(exactSamples);
Console.WriteLine($"dt chosen so departure + {n} steps lands exactly on the ground-truth minimum time: dt = {dtExact:F6} s");
Console.WriteLine($"sample landed at t = {exactNaiveTime / Day:F6} days (ground truth {truthTime / Day:F6} days)");
Console.WriteLine($"naive minimum = {exactNaive / 1000.0:F6} km, refine = {exactRefine / 1000.0:F6} km, " +
    $"ground truth = {truthDist / 1000.0:F6} km");
Console.WriteLine("When a sample already sits on the true minimum, naive and refine agree almost exactly — the");
Console.WriteLine("parabola fit has nothing left to correct for, because there's no interpolation error to remove.");
Console.WriteLine("The refine doesn't do worse here; it just stops mattering. Its whole value is for the (usual)");
Console.WriteLine("case where the minimum falls BETWEEN samples, which is every density in Section B/D above.");
Console.WriteLine();

// ===================================================================================
// Break it #2 — a mid-path burn breaks the parabola's smoothness assumption
// ===================================================================================
Console.WriteLine("=== BREAK IT #2: a mid-path burn near closest approach ===");
const int burnPulses = 3; // a real coarse +-10% burst, not a 1% trim — a genuine velocity kink
const double coarseDt = 600.0; // the "10 minutes" density from Section B/D, already under strain

IReadOnlyList<TrajectorySample> unburnedFine = SampleUniform(coarseDt, plan);
(double unburnedCoarseMin, double unburnedCoarseMinTime) = NaiveMin(unburnedFine);
double unburnedRefine = RefineMin(unburnedFine);
double unburnedRefineErr = Math.Abs(unburnedRefine - truthDist) / truthDist * 100.0;

// Place the burn exactly on the coarse scan's own minimum sample — the parabola's CENTER
// bracket point — so it directly corrupts the vertex the fit trusts most.
double burnTime = unburnedCoarseMinTime;
var burnedPlan = new ManeuverPlan(
[
    new ManeuverNode(departureTime + 3600, ManeuverAction.Accelerate, 3),
    new ManeuverNode(burnTime, ManeuverAction.Accelerate, burnPulses),
]);

IReadOnlyList<TrajectorySample> burnedTruth = SampleUniform(5.0, burnedPlan);
(double burnedTruthDist, double burnedTruthTime) = NaiveMin(burnedTruth);
IReadOnlyList<TrajectorySample> burnedFine = SampleUniform(coarseDt, burnedPlan);
(double burnedNaive, double _) = NaiveMin(burnedFine);
double burnedRefine = RefineMin(burnedFine);
double burnedNaiveErr = Math.Abs(burnedNaive - burnedTruthDist) / burnedTruthDist * 100.0;
double burnedRefineErr = Math.Abs(burnedRefine - burnedTruthDist) / burnedTruthDist * 100.0;

Console.WriteLine($"Extra {burnPulses}-pulse accelerate burn inserted at t = {burnTime / Day:F4} days " +
    $"(inside the {coarseDt:F0}-second bracket straddling the original minimum near t = {truthTime / Day:F4} days)");
Console.WriteLine($"new dense-scan ground truth: {burnedTruthDist / 1000.0:F3} km at t = {burnedTruthTime / Day:F4} days");
Console.WriteLine($"at dt = {coarseDt:F0} s: unburned refine err = {unburnedRefineErr:E3} %, burned refine err = {burnedRefineErr:E3} % " +
    $"(burned naive err = {burnedNaiveErr:E3} %)");
Console.WriteLine("The parabola-on-d^2 fit assumes smooth motion across the three bracketing samples. A burn");
Console.WriteLine("dropped inside that bracket puts a velocity DISCONTINUITY between two of them, which the fit");
Console.WriteLine("has no way to model — it can only ever see the two positions, not the kink in between.");
