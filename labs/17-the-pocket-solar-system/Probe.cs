// Lab 17 — The pocket solar system
//
// Teaching voice: Saturn's moon family is the entire course, replayed at 1/1000 scale.
// Enceladus-to-Titan has the same radius ratio as an Earth-to-Jupiter run, the same Hohmann
// algebra, the same Lambert seeds and shooting finishers (lessons 14-15) — but the primary
// is Saturn, not the sun, so everything that took YEARS in the big system takes DAYS in the
// pocket one, and the transfer window that lesson 15 said comes once a year comes every 36
// hours. Same physics, different mu: the course's whole toolchain should transfer without
// edits. This lesson checks that it does — and measures the one thing the pocket system has
// that the textbook two-body picture doesn't: the sun outside, tugging the whole pocket.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code,
// the printed numbers in labs/17-the-pocket-solar-system/README.md go stale — rerun and
// re-paste, never hand-edit a table.

using SpaceSails.Core;

const double Day = 86400.0;
const double Year = 365.25 * Day;
const double SunMu = 1.32712440018e20;
const double SaturnMu = 3.7931187e16;

// Full sol.json field: sun, planets, moons, real radii (lesson 16's specs, verbatim).
(string Id, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase)[] specs =
[
    ("sun", "", SunMu, 6.9634e8, 0, 0, 0),
    ("mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    ("venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    ("earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    ("mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", "sun", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
    ("luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0),
    ("europa", "jupiter", 3.2038e12, 1.5608e6, 6.709e8, 3.068226e5, 0.5),
    ("ganymede", "jupiter", 9.8907e12, 2.6341e6, 1.0704e9, 6.181531e5, 1.5),
    ("callisto", "jupiter", 7.1808e12, 2.4103e6, 1.8827e9, 1.4419307e6, 3.0),
    ("titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0),
    ("enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0),
];

var fullField = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Id, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase))]);

// The pocket alone: Saturn nailed to the origin, its two big moons riding it, nothing else
// in the universe. Same Saturn-centric geometry at any sim time, by construction.
var pocketOnly = new CircularOrbitEphemeris(
[
    new CelestialBody("saturn", "saturn", null, SaturnMu, 5.8232e7, 0, 0, 0),
    new CelestialBody("titan", "titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0),
    new CelestialBody("enceladus", "enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0),
]);

Vector2d BodyVelocity(ICelestialEphemeris eph, string id, double t) =>
    (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

// Lessons 14-15's toolchain, verbatim — the point of this lab is that it transfers.
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

(Vector2d V, int Iterations, double MissMeters, bool Converged) Shoot(
    Simulator sim, Vector2d position, double t0, Vector2d vSeed, Vector2d target, double tArrive,
    double tolMeters, int maxIterations)
{
    const double Eps = 1.0;
    Vector2d Fly(Vector2d v) => sim.RunAdaptive(new ShipState(position, v, t0), tArrive - t0).Position;

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
// Section A — the scale model, side by side
// ===================================================================================
Console.WriteLine("=== Section A: the same course, two sizes ===");
(double dv1, double dv2, double tof) Hohmann(double r1, double r2, double mu)
{
    double aT = (r1 + r2) / 2;
    double dv1 = Math.Abs(Math.Sqrt(mu * (2 / r1 - 1 / aT)) - Math.Sqrt(mu / r1));
    double dv2 = Math.Abs(Math.Sqrt(mu / r2) - Math.Sqrt(mu * (2 / r2 - 1 / aT)));
    return (dv1, dv2, Math.PI * Math.Sqrt(aT * aT * aT / mu));
}

double Synodic(double t1, double t2) => 1.0 / Math.Abs(1.0 / t1 - 1.0 / t2);

var big = Hohmann(1.496e11, 7.7857e11, SunMu);
var pocket = Hohmann(2.38037e8, 1.22183e9, SaturnMu);
double bigSyn = Synodic(3.1558149e7, 3.74336e8);
double pocketSyn = Synodic(1.183868e5, 1.377648e6);
Console.WriteLine($"{"run",-22}{"r2/r1",7}{"total dv (km/s)",17}{"TOF",12}{"window every",15}");
Console.WriteLine($"{"Earth -> Jupiter",-22}{7.7857e11 / 1.496e11,7:F2}{(big.dv1 + big.dv2) / 1000,17:F2}{big.tof / Year,9:F2} yr{bigSyn / Day,12:F1} d");
Console.WriteLine($"{"Enceladus -> Titan",-22}{1.22183e9 / 2.38037e8,7:F2}{(pocket.dv1 + pocket.dv2) / 1000,17:F2}{pocket.tof / Day,9:F2} d {pocketSyn / 3600,11:F1} h");
Console.WriteLine();
Console.WriteLine("Same radius ratio, same algebra, same COURSE — but the pocket's clock runs ~270x faster.");
Console.WriteLine("Lesson 15 called the outer system LONG; its moon system is the opposite: interplanetary-");
Console.WriteLine("grade dv on a weekend timescale, with a transfer window every day and a half. (The dv is");
Console.WriteLine("no toy, though: Enceladus rides Saturn at 12.6 km/s. Deep wells spin fast bus routes.)");
Console.WriteLine();

// ===================================================================================
// Section B — fly one hop, both fields: how much is the sun's share?
// ===================================================================================
Console.WriteLine("=== Section B: one hop, flown honestly — and the sun's share, isolated ===");
double tof = pocket.tof; // the Hohmann-time hop
double bestDep = 0, bestDv = double.MaxValue;
for (double depHour = 0; depHour <= pocketSyn / 3600; depHour += 0.5)
{
    double t = depHour * 3600;
    Vector2d satPos = fullField.Position("saturn", t);
    Vector2d encPos = fullField.Position("enceladus", t);
    Vector2d from = encPos + (encPos - satPos).Normalized() * 3e6; // Enceladus doorstep: ~3 of its own Hill radii
    Vector2d titanArr = fullField.Position("titan", t + tof);
    Vector2d aim = titanArr + (titanArr - fullField.Position("saturn", t + tof)).Normalized() * 2.5e7;
    var (v1, v2, ok) = Lambert(from - satPos, aim - fullField.Position("saturn", t + tof), tof, SaturnMu);
    if (ok < 0)
    {
        continue;
    }

    double dv = (v1 - (BodyVelocity(fullField, "enceladus", t) - BodyVelocity(fullField, "saturn", t))).Length
              + ((BodyVelocity(fullField, "titan", t + tof) - BodyVelocity(fullField, "saturn", t + tof)) - v2).Length;
    if (dv < bestDv)
    {
        (bestDep, bestDv) = (depHour, dv);
    }
}

Console.WriteLine($"Lambert scan of one synodic cycle ({pocketSyn / 3600:F1} h, half-hour grid): best departure " +
    $"hour {bestDep:F1}, total dv {bestDv / 1000:F2} km/s (TOF fixed at the Hohmann {tof / Day:F2} d)");

double t0 = bestDep * 3600;
double tArrive = t0 + tof;
const double Tol = 1e6; // 1,000 km on a 1e9 m hop

(double corr, bool conv, int iters, double relArr) FlyHop(ICelestialEphemeris eph, string label)
{
    var sim = new Simulator(eph, timeStepSeconds: 60);
    Vector2d satPos = eph.Position("saturn", t0);
    Vector2d encPos = eph.Position("enceladus", t0);
    Vector2d from = encPos + (encPos - satPos).Normalized() * 3e6;
    Vector2d satArr = eph.Position("saturn", tArrive);
    Vector2d titanArr = eph.Position("titan", tArrive);
    Vector2d aim = titanArr + (titanArr - satArr).Normalized() * 2.5e7;
    var (v1, _, _) = Lambert(from - satPos, aim - satArr, tof, SaturnMu);
    Vector2d seed = v1 + BodyVelocity(eph, "saturn", t0);
    var solved = Shoot(sim, from, t0, seed, aim, tArrive, Tol, 15);
    ShipState arrived = sim.RunAdaptive(new ShipState(from, solved.V, t0), tof);
    double relArrival = (arrived.Velocity - BodyVelocity(eph, "titan", tArrive)).Length;
    Console.WriteLine($"{label,-34} converged {solved.Converged} in {solved.Iterations} iterations, " +
        $"correction on Lambert {(solved.V - seed).Length:F2} m/s, arrival {relArrival / 1000:F2} km/s vs Titan");
    return ((solved.V - seed).Length, solved.Converged, solved.Iterations, relArrival);
}

var inPocket = FlyHop(pocketOnly, "pocket only (Saturn + two moons):");
var inFull = FlyHop(fullField, "full field (sun + everything):");
Console.WriteLine($"-> the sun's share of the correction: ~{inFull.corr - inPocket.corr:F0} m/s on this {tof / Day:F1}-day arc.");
Console.WriteLine("   Titan sits 2% of the way to Saturn's Hill edge — the pocket is DEEP, and the sun's");
Console.WriteLine("   tide on a days-long arc is pocket change. Compare lesson 15: the same sun cost a");
Console.WriteLine("   six-year Saturn passage 150 m/s of Lambert lie. Pocket systems are not just fast,");
Console.WriteLine("   they are CLEAN — two-body intuition works better here than anywhere in the big system.");
Console.WriteLine();

// ===================================================================================
// Section C — the porkchop plate, pocket edition
// ===================================================================================
Console.WriteLine("=== Section C: the porkchop plate that fits in a weekend ===");
Console.WriteLine("Total dv (km/s) by departure hour and TOF — lesson 14's plate spanned 760 DAYS of");
Console.WriteLine("departures; this one spans 36 HOURS:");
Console.WriteLine();
double[] tofDays = [2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5];
Console.Write("dep hr  |");
foreach (double td in tofDays)
{
    Console.Write($"{td,7:F1}");
}

Console.WriteLine("  <- TOF (days)");
Console.WriteLine(new string('-', 9 + 7 * tofDays.Length + 2));
(double dep, double tofD, double dv) bestCell = (0, 0, double.MaxValue);
for (double depHour = 0; depHour <= 36; depHour += 4)
{
    Console.Write($"{depHour,7:F0} |");
    foreach (double td in tofDays)
    {
        double t = depHour * 3600;
        double tofC = td * Day;
        Vector2d satPos = fullField.Position("saturn", t);
        Vector2d encPos = fullField.Position("enceladus", t);
        Vector2d from = encPos + (encPos - satPos).Normalized() * 3e6;
        Vector2d satArr = fullField.Position("saturn", t + tofC);
        Vector2d titanArr = fullField.Position("titan", t + tofC);
        var (v1, v2, ok) = Lambert(from - satPos, titanArr - satArr, tofC, SaturnMu);
        if (ok < 0)
        {
            Console.Write("      -");
            continue;
        }

        double dv = (v1 - (BodyVelocity(fullField, "enceladus", t) - BodyVelocity(fullField, "saturn", t))).Length
                  + ((BodyVelocity(fullField, "titan", t + tofC) - BodyVelocity(fullField, "saturn", t + tofC)) - v2).Length;
        if (dv < bestCell.dv)
        {
            bestCell = (depHour, td, dv);
        }

        Console.Write($"{dv / 1000,7:F1}");
    }

    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine($"cheapest cell: depart hour {bestCell.dep:F0}, TOF {bestCell.tofD:F1} days, total dv {bestCell.dv / 1000:F2} km/s");
Console.WriteLine("Read the rows top to bottom: the valley drifts through the plate and comes BACK — that's");
Console.WriteLine($"the {pocketSyn / 3600:F0}-hour synodic beat. Lesson 15's navigator waits 378 days for this pattern to");
Console.WriteLine("repeat; the Saturn league's traders watch it repeat before the weekend is out. Miss the");
Console.WriteLine("bus in a pocket system and the honest answer is: there's another one tomorrow.");
