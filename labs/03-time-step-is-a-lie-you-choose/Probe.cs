// Lab 03 — Time step is a lie you choose
//
// Teaching voice: "dt" isn't a physical constant. It's a knob you turn, and the honest
// answer to "what should it be" is "small enough to resolve whatever is happening right now" —
// which is a DIFFERENT number in deep space than during a sun-grazing flyby. This probe builds
// a fast hyperbolic flyby past the Sun (real mu from scenarios/sol.json) and asks three fixed
// timesteps, and the game's own Simulator.ProjectAdaptive, to find its closest approach. The
// judge is a closed-form periapsis from orbital energy + angular momentum — quantities the
// continuous two-body problem gets exactly, no numerical integration involved.
//
// Simulator.cs (ProjectAdaptive's doc comment) is blunt about why the game doesn't use a
// closed-form shortcut here at all: "The classic closed-form alternative (universal-variable
// Kepler / patched conics) is rejected on purpose — it assumes one attracting body per arc and
// would disagree with the integrator exactly where the game happens: flybys." This probe
// measures exactly how much fixed-dt integration ALSO disagrees with the truth at a flyby,
// and how the adaptive scheme (dt = dynamical time / 64, clamped [1 s, 1 h]) buys most of the
// fine-dt accuracy for a fraction of the fine-dt cost.
//
// IRONCLAD RULE: every number below came from running this probe.

using SpaceSails.Core;

const double SunMu = 1.32712440018e20; // m^3/s^2
const double SunBodyRadius = 6.9634e8; // m
const double AU = 1.496e11;            // m

var sun = new CelestialBody("sun", "Sun", null, SunMu, SunBodyRadius, 0, 0, 0);
var ephemeris = new CircularOrbitEphemeris([sun]);

// A fast, deep sun-grazing hyperbolic flyby: start 3 AU out, moving straight at the Sun with
// a small sideways offset (impact parameter) and an asymptotic ("interstellar visitor") speed
// of 40 km/s — comet/interstellar-object territory, well above anything bound to the Sun.
double startDistance = 3.0 * AU;
double impactParameter = 0.16 * AU;
double vInfinity = 40000.0;

var start = new ShipState(new Vector2d(-startDistance, impactParameter), new Vector2d(vInfinity, 0), 0);

// Closed-form orbital elements from the actual starting state — valid for ANY conic, bound or
// not (Curtis ch. 2's energy/angular-momentum relations don't care whether e is above or below
// 1). This is the ground truth the integrators are judged against.
double r0 = start.Position.Length;
double v0 = start.Velocity.Length;
double energy = v0 * v0 / 2.0 - SunMu / r0;
double h = start.Position.X * start.Velocity.Y - start.Position.Y * start.Velocity.X; // r x v (z)
double a = -SunMu / (2.0 * energy);
double e = Math.Sqrt(1.0 + 2.0 * energy * h * h / (SunMu * SunMu));
double periapsisTruth = a * (1 - e);

Console.WriteLine("=== The flyby ===");
Console.WriteLine($"v_infinity = {vInfinity:F0} m/s, impact parameter = {impactParameter / AU:F3} AU");
Console.WriteLine($"eccentricity e = {e:F6} (hyperbolic, e > 1), semi-major axis a = {a / AU:F6} AU");
Console.WriteLine($"closed-form periapsis = {periapsisTruth / AU:F6} AU ({periapsisTruth:E6} m," +
    $" {periapsisTruth / SunBodyRadius:F2}x the Sun's radius)");
Console.WriteLine();

// Horizon: roughly the time to cross from -3 AU to +3 AU at the asymptotic speed (the bend
// near periapsis eats a little of this, but the ship is well past the Sun again by then).
double horizon = 2.0 * startDistance / vInfinity;
Console.WriteLine($"Integration horizon: {horizon:F0} s ({horizon / 86400.0:F1} days)");
Console.WriteLine();

(double minRadius, int steps) TrackFixed(double dt)
{
    var simulator = new Simulator(ephemeris, dt);
    IReadOnlyList<Vector2d> points = simulator.Project(start, null, horizon, sampleEverySteps: 1);
    return (points.Min(p => p.Length), points.Count - 1);
}

(double minRadius, int steps) TrackAdaptive(double minDt, double maxDt, double fraction)
{
    var simulator = new Simulator(ephemeris, timeStepSeconds: 1.0); // TimeStep unused by ProjectAdaptive
    IReadOnlyList<TrajectorySample> samples =
        simulator.ProjectAdaptive(start, null, horizon, minDt, maxDt, fraction, maxSamples: 200_000);
    return (samples.Min(s => s.Position.Length), samples.Count - 1);
}

void PrintRow(string label, double minRadius, int steps)
{
    double error = Math.Abs(minRadius - periapsisTruth);
    double relError = error / periapsisTruth;
    Console.WriteLine($"{label,-24}{steps,-12}{minRadius / AU,-16:F6}{error,-14:E3}{relError,-14:E3}");
}

Console.WriteLine("=== Section A: fixed dt vs the game's adaptive stepping ===");
Console.WriteLine($"{"method",-24}{"steps (cost)",-12}{"min radius (AU)",-16}{"abs. error (m)",-14}{"rel. error",-14}");
foreach (double dt in new double[] { 3600, 600, 60 })
{
    var (minR, steps) = TrackFixed(dt);
    PrintRow($"fixed dt = {dt:F0} s", minR, steps);
}

var (adaptR, adaptSteps) = TrackAdaptive(1.0, 3600.0, 1.0 / 64);
PrintRow("adaptive (game default)", adaptR, adaptSteps);

Console.WriteLine();
Console.WriteLine("=== BREAK IT: widen the clamp — let adaptive dt run coarser near the Sun ===");
Console.WriteLine($"{"clamp",-24}{"steps (cost)",-12}{"min radius (AU)",-16}{"abs. error (m)",-14}{"rel. error",-14}");
foreach (double maxDt in new double[] { 3600, 21600, 86400 })
{
    var (minR, steps) = TrackAdaptive(1.0, maxDt, 1.0 / 64);
    PrintRow($"max dt = {maxDt:F0} s", minR, steps);
}

Console.WriteLine();
Console.WriteLine("=== BREAK IT: loosen the /64 — coarser or finer dynamical-time fraction ===");
Console.WriteLine($"{"fraction",-24}{"steps (cost)",-12}{"min radius (AU)",-16}{"abs. error (m)",-14}{"rel. error",-14}");
foreach (double fraction in new double[] { 1.0 / 8, 1.0 / 64, 1.0 / 512 })
{
    var (minR, steps) = TrackAdaptive(1.0, 3600.0, fraction);
    PrintRow($"fraction = 1/{1.0 / fraction:F0}", minR, steps);
}
