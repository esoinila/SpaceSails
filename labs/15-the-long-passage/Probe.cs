// Lab 15 — The long passage
//
// Teaching voice: everything this lab series has taught so far happened on inner-system
// clocks — hours for a slug (lesson 13), months for a Mars run (lessons 5, 14). This lesson
// sails for SATURN: a six-year passage, and six years is where small numbers stop being
// small. A 1 m/s departure error is invisible on a Mars run; over a Saturn passage it
// compounds into planetary-scale misses. The lesson prices four things honestly: what the
// outer system costs at all (Hohmann's tyranny), what one passage costs to solve (lesson
// 14's Lambert seed + shooting, at six-year range), what tiny sins compound into, and the
// navigator's oldest law — a correction's price is set by WHEN you pay it, not how big the
// error was.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code,
// the printed numbers in labs/15-the-long-passage/README.md go stale — rerun and re-paste,
// never hand-edit a table.

using System.Diagnostics;
using SpaceSails.Core;

const double Day = 86400.0;
const double Year = 365.25 * Day;
const double AU = 1.496e11;
const double SunMu = 1.32712440018e20;

// Real sol.json rows, sun through neptune (body radii 1 m: no arc can "collide"; radius has
// no effect on gravity — lesson 09's trick).
(string Id, double Mu, double OrbitRadius, double OrbitPeriod, double InitialPhase)[] specs =
[
    ("sun", SunMu, 0, 0, 0),
    ("mercury", 2.2032e13, 5.791e10, 7.60052e6, 0.0),
    ("venus", 3.24859e14, 1.0821e11, 1.94142e7, 0.9),
    ("earth", 3.986004418e14, 1.496e11, 3.1558149e7, 1.8),
    ("mars", 4.282837e13, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", 1.26686534e17, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", 3.7931187e16, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", 5.793939e15, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", 6.836529e15, 4.49506e12, 5.2004e9, 0.4),
];
var ephemeris = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Id, s.Id == "sun" ? null : "sun", s.Mu, 1.0, s.OrbitRadius, s.OrbitPeriod, s.InitialPhase))]);
var sunOnly = new CircularOrbitEphemeris(
    [new CelestialBody("sun", "sun", null, SunMu, 1.0, 0, 0, 0)]);

double EarthPeriod = specs[3].OrbitPeriod;
Vector2d BodyVelocity(string id, double t) =>
    (ephemeris.Position(id, t + 1.0) - ephemeris.Position(id, t - 1.0)) / 2.0;

// Lesson 14's tools, verbatim: two-body Lambert as the seed, shooting as the finisher.
(Vector2d V1, Vector2d V2, int Iterations) Lambert(Vector2d r1v, Vector2d r2v, double tof, double mu)
{
    double r1 = r1v.Length, r2 = r2v.Length;
    double cross = r1v.X * r2v.Y - r1v.Y * r2v.X;
    double dTheta = Math.Acos(Math.Clamp(r1v.Dot(r2v) / (r1 * r2), -1.0, 1.0));
    if (cross < 0)
    {
        dTheta = Math.Tau - dTheta;
    }

    double A = Math.Sin(dTheta) * Math.Sqrt(r1 * r2 / (1 - Math.Cos(dTheta)));

    static double StumpffC(double z) => z > 1e-8 ? (1 - Math.Cos(Math.Sqrt(z))) / z
        : z < -1e-8 ? (Math.Cosh(Math.Sqrt(-z)) - 1) / -z
        : 0.5 - z / 24 + z * z / 720;
    static double StumpffS(double z) => z > 1e-8 ? (Math.Sqrt(z) - Math.Sin(Math.Sqrt(z))) / Math.Pow(z, 1.5)
        : z < -1e-8 ? (Math.Sinh(Math.Sqrt(-z)) - Math.Sqrt(-z)) / Math.Pow(-z, 1.5)
        : 1.0 / 6 - z / 120 + z * z / 5040;

    double Y(double z) => r1 + r2 + A * (z * StumpffS(z) - 1) / Math.Sqrt(StumpffC(z));
    double TofError(double z)
    {
        double y = Y(z);
        return y < 0
            ? double.NegativeInfinity
            : Math.Pow(y / StumpffC(z), 1.5) * StumpffS(z) + A * Math.Sqrt(y) - Math.Sqrt(mu) * tof;
    }

    double zLo = -100.0, zHi = Math.Tau * Math.Tau - 1e-9;
    int iterations = 0;
    while (zHi - zLo > 1e-12 && iterations < 200)
    {
        double zMid = 0.5 * (zLo + zHi);
        if (TofError(zMid) > 0) { zHi = zMid; } else { zLo = zMid; }
        iterations++;
    }

    double zSol = 0.5 * (zLo + zHi);
    double achievedTof = (TofError(zSol) + Math.Sqrt(mu) * tof) / Math.Sqrt(mu);
    if (double.IsNaN(achievedTof) || Math.Abs(achievedTof - tof) > 1.0)
    {
        return (Vector2d.Zero, Vector2d.Zero, -1);
    }

    double ySol = Y(zSol);
    double f = 1 - ySol / r1;
    double g = A * Math.Sqrt(ySol / mu);
    double gDot = 1 - ySol / r2;
    return ((r2v - r1v * f) / g, (r2v * gDot - r1v) / g, iterations);
}

var flightSim = new Simulator(ephemeris, timeStepSeconds: 60);

(Vector2d V, int Iterations, double MissMeters, bool Converged) Shoot(
    Vector2d position, double t0, Vector2d vSeed, Vector2d target, double tArrive,
    double tolMeters, int maxIterations)
{
    const double Eps = 1.0;
    Vector2d Fly(Vector2d v) => flightSim.RunAdaptive(new ShipState(position, v, t0), tArrive - t0).Position;

    Vector2d v = vSeed;
    double lastMiss = double.MaxValue;
    for (int iter = 0; iter < maxIterations; iter++)
    {
        Vector2d miss = Fly(v) - target;
        lastMiss = miss.Length;
        if (lastMiss < tolMeters)
        {
            return (v, iter, lastMiss, true);
        }

        Vector2d colX = (Fly(v + new Vector2d(Eps, 0)) - (miss + target)) / Eps;
        Vector2d colY = (Fly(v + new Vector2d(0, Eps)) - (miss + target)) / Eps;
        double det = colX.X * colY.Y - colX.Y * colY.X;
        var step = new Vector2d(
            -(colY.Y * miss.X - colY.X * miss.Y) / det,
            -(-colX.Y * miss.X + colX.X * miss.Y) / det);
        if (step.Length > 500)
        {
            step = step.Normalized() * 500;
        }

        v += step;
    }

    return (v, maxIterations, lastMiss, false);
}

// ===================================================================================
// Section A — the tyranny of the outer system, priced by Hohmann
// ===================================================================================
Console.WriteLine("=== Section A: what the outer system costs (Hohmann analytic, Curtis ch. 6) ===");
Console.WriteLine($"{"target",-10}{"dv1 (m/s)",11}{"dv2 (m/s)",11}{"total",9}{"TOF (years)",13}{"window every (days)",21}");
foreach (string id in new[] { "mars", "jupiter", "saturn", "uranus", "neptune" })
{
    var spec = specs.First(s => s.Id == id);
    double r1 = AU, r2 = spec.OrbitRadius, aT = (r1 + r2) / 2;
    double dv1 = Math.Abs(Math.Sqrt(SunMu * (2 / r1 - 1 / aT)) - Math.Sqrt(SunMu / r1));
    double dv2 = Math.Abs(Math.Sqrt(SunMu / r2) - Math.Sqrt(SunMu * (2 / r2 - 1 / aT)));
    double tofYears = Math.PI * Math.Sqrt(aT * aT * aT / SunMu) / Year;
    double synodicDays = 1.0 / Math.Abs(1.0 / EarthPeriod - 1.0 / spec.OrbitPeriod) / Day;
    Console.WriteLine($"{id,-10}{dv1,11:F0}{dv2,11:F0}{dv1 + dv2,9:F0}{tofYears,13:F2}{synodicDays,21:F1}");
}

Console.WriteLine("-> read the shape: past Jupiter the dv bill nearly stops growing (you are escaping the");
Console.WriteLine("   sun either way) but the CLOCK explodes. The outer system is not expensive — it is LONG.");
Console.WriteLine("   Neptune's window repeats every ~367 days; its passage lasts ~30.6 YEARS. The window is");
Console.WriteLine("   not the scarce resource out here. Lifetime is.");
Console.WriteLine();

// ===================================================================================
// Section B — one real passage, solved: Earth -> Saturn, six years
// ===================================================================================
Console.WriteLine("=== Section B: the passage, solved (Earth -> Saturn's doorstep) ===");
double tof = 6.10 * Year;
double synodicSaturn = 1.0 / Math.Abs(1.0 / EarthPeriod - 1.0 / specs[6].OrbitPeriod);

// Scan one Earth-Saturn synodic cycle of departure days with the CHEAP tool (Lambert),
// exactly how lesson 14's porkchop found its valley.
(double dep, double dv, Vector2d v1) window = (0, double.MaxValue, Vector2d.Zero);
for (double depDay = 0; depDay <= synodicSaturn / Day; depDay += 7)
{
    double t = depDay * Day;
    ShipState from = RoutePlanner.DepartureState(ephemeris, "earth", "saturn", t);
    Vector2d saturnArr = ephemeris.Position("saturn", t + tof);
    Vector2d aim = saturnArr + saturnArr.Normalized() * 1e9;
    var (lv1, lv2, ok) = Lambert(from.Position, aim, tof, SunMu);
    if (ok < 0)
    {
        continue;
    }

    double dv = (lv1 - BodyVelocity("earth", t)).Length + (BodyVelocity("saturn", t + tof) - lv2).Length;
    if (dv < window.dv)
    {
        window = (depDay, dv, lv1);
    }
}

Console.WriteLine($"Lambert scan of one synodic cycle ({synodicSaturn / Day:F1} days, 7-day grid): " +
    $"best departure day {window.dep:F0}, total dv {window.dv / 1000:F2} km/s (TOF fixed at {tof / Year:F2} years)");

double t0 = window.dep * Day;
double tArrive = t0 + tof;
ShipState departure = RoutePlanner.DepartureState(ephemeris, "earth", "saturn", t0);
Vector2d saturnAtArrival = ephemeris.Position("saturn", tArrive);
Vector2d target = saturnAtArrival + saturnAtArrival.Normalized() * 1e9; // doorstep: 1,000,000 km out
const double Tol = 1e7; // 10,000 km on a 1.4e9 km passage

var solved = Shoot(departure.Position, t0, window.v1, target, tArrive, Tol, 15);
Console.WriteLine($"shooting through all nine bodies: converged {solved.Converged} in {solved.Iterations} Newton " +
    $"iterations, final miss {solved.MissMeters / 1000:N0} km (tolerance {Tol / 1000:N0} km)");
Console.WriteLine($"correction the real world demanded on top of Lambert: {(solved.V - window.v1).Length:F2} m/s over a " +
    $"{tof / Year:F1}-year arc");

var clock = Stopwatch.StartNew();
ShipState arrived = flightSim.RunAdaptive(new ShipState(departure.Position, solved.V, t0), tof);
clock.Stop();
Console.WriteLine($"arrival speed relative to Saturn: {(arrived.Velocity - BodyVelocity("saturn", tArrive)).Length / 1000:F2} km/s " +
    "(the braking bill, lesson 4's pulses will have to pay it)");
Console.WriteLine($"cost of one six-year flight, adaptive integrator: {clock.ElapsedMilliseconds} ms on this dev machine");
Console.WriteLine($"   (lesson 10's point at passage scale: the Newton solve above burned {solved.Iterations * 3 + 1} such flights)");
Console.WriteLine();

// ===================================================================================
// Section C — six years is where small numbers stop being small
// ===================================================================================
Console.WriteLine("=== Section C: what a tiny departure error compounds into ===");
Vector2d baseline = arrived.Position;
Vector2d alongTrack = solved.V.Normalized();
Console.WriteLine($"{"error (m/s)",12}{"naive error*TOF (km)",22}{"actual miss (km)",18}{"amplification",15}");
foreach (double err in new[] { 0.01, 0.1, 1.0, 10.0 })
{
    ShipState end = flightSim.RunAdaptive(new ShipState(departure.Position, solved.V + alongTrack * err, t0), tof);
    double actual = (end.Position - baseline).Length;
    double naive = err * tof;
    Console.WriteLine($"{err,12:F2}{naive / 1000,22:N0}{actual / 1000,18:N0}{actual / naive,15:F1}x");
}

Console.WriteLine("-> the amplification is the orbit dynamics themselves: an along-track error changes the");
Console.WriteLine("   orbit's ENERGY, so the arcs don't just drift apart linearly — they arrive at different");
Console.WriteLine("   times moving at km/s. Lesson 4's quantized pulses guarantee you never leave with the");
Console.WriteLine("   exact right velocity. On a Mars run that footnote is survivable. Here it is the story.");
Console.WriteLine();

// ===================================================================================
// Section D — the navigator's oldest law: a correction is priced by WHEN you pay it
// ===================================================================================
Console.WriteLine("=== Section D: the same 1 m/s sin, absolved at different confessionals ===");
Vector2d sinnedV = solved.V + alongTrack * 1.0;
Console.WriteLine($"{"corrected at",14}{"years remaining",17}{"correction dv (m/s)",21}{"final miss (km)",17}");
foreach (double corrDay in new[] { 30.0, 180.0, 365.0, 730.0, 1460.0, 2000.0, 2198.0 })
{
    double tc = t0 + corrDay * Day;
    ShipState atCorr = flightSim.RunAdaptive(new ShipState(departure.Position, sinnedV, t0), tc - t0);
    Vector2d lambertSeed = Lambert(atCorr.Position, target, tArrive - tc, SunMu).V1;
    var fix = Shoot(atCorr.Position, tc, lambertSeed, target, tArrive, Tol, 15);
    ShipState fixedEnd = flightSim.RunAdaptive(new ShipState(atCorr.Position, fix.V, tc), tArrive - tc);
    Console.WriteLine($"{$"day {corrDay:F0}",14}{(tArrive - tc) / Year,17:F2}{(fix.V - atCorr.Velocity).Length,21:F2}" +
        $"{(fixedEnd.Position - target).Length / 1000,17:N0}");
}

Console.WriteLine("-> same sin, same destination, price set only by the calendar. Fix it in the first month");
Console.WriteLine("   and the bill is barely more than the sin itself; sail on it for years and the correction");
Console.WriteLine("   grows toward a whole new departure burn. Mission control's daily trajectory meetings");
Console.WriteLine("   are not bureaucracy — they are compound interest management.");
Console.WriteLine();

// ===================================================================================
// Section E — nobody can watch you for six years
// ===================================================================================
Console.WriteLine("=== Section E: the cone at passage scale (lesson 8's honesty, compounded) ===");
var obs = new Observation("freighter", t0, departure.Position, solved.V);
PredictedPath crewedTrack = PathPredictor.Predict(ephemeris, obs, null, Day);
PredictedPath podTrack = PathPredictor.Predict(ephemeris, obs, null, Day, maneuverBudget: 0);
Console.WriteLine($"cone half-width at arrival, CREWED ship (can maneuver unobserved): " +
    $"{crewedTrack.HalfWidthAt(tArrive) / AU:N0} AU");
Console.WriteLine($"cone half-width at arrival, mass-driver POD (cannot burn at all):   " +
    $"{podTrack.HalfWidthAt(tArrive) / AU:F2} AU ({podTrack.HalfWidthAt(tArrive) / 1000:N0} km)");
double captureEnvelope = 5e8;
double staleSeconds = (captureEnvelope - PredictedPath.BaseHalfWidthMeters) / PredictedPath.VelocitySigma;
Console.WriteLine($"even the pod's cone crosses the {captureEnvelope / 1000:N0} km capture envelope " +
    $"{staleSeconds / Day:F0} days after the fix — measurement noise alone (100 m/s velocity sigma) does it");
Console.WriteLine("-> a six-year track does not exist. Long-haul traffic is not TRACKED, it is MET: you");
Console.WriteLine("   compute where the passage ends (Sections B-D) and re-acquire at the far end —");
Console.WriteLine("   lesson 8's telescope work, scheduled six years in advance.");
