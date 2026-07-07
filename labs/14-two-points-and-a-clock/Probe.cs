// Lab 14 — Two points and a clock
//
// Teaching voice: lesson 13 solved the gunner's boundary value problem by shooting. This
// lesson graduates the same method to the navigator's BVP — LAMBERT'S PROBLEM (Curtis ch. 5):
// given where you are, where you must be, and the clock ("be at Mars on day 310"), find the
// departure velocity. Curtis solves it semi-analytically with universal variables — and that
// solution is EXACT, in a universe with exactly one attracting body. Ours has nine. So we do
// what real mission designers do: use Lambert as the seed, then let the shooting method
// (Newton on the real integrator, lesson 13's tool) finish the job in the world that actually
// exists. Lambert proposes; the integrator disposes.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code,
// the printed numbers in labs/14-two-points-and-a-clock/README.md go stale — rerun and
// re-paste, never hand-edit a table.

using SpaceSails.Core;

const double Day = 86400.0;
const double SunMu = 1.32712440018e20;

// Real sol.json rows, sun through neptune (moons/stations dropped; body radii set to 1 m so
// no lesson arc can "collide" — radius has no effect on gravity, exactly as in lesson 09).
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

CircularOrbitEphemeris BuildEphemeris(Func<string, bool> keep) => new(
    [.. specs.Where(s => keep(s.Id)).Select(s => new CelestialBody(
        s.Id, s.Id, s.Id == "sun" ? null : "sun", s.Mu, 1.0, s.OrbitRadius, s.OrbitPeriod, s.InitialPhase))]);

var solEphemeris = BuildEphemeris(_ => true);
var sunOnly = BuildEphemeris(id => id == "sun");
var sunEarthMars = BuildEphemeris(id => id is "sun" or "earth" or "mars");

Vector2d BodyVelocity(ICelestialEphemeris eph, string id, double t) =>
    (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

// ===================================================================================
// The Lambert solver — Curtis Algorithm 5.2 (universal variables), 2-D prograde
// ===================================================================================
// One honest disclosure up front: the "analytic" solution is ITSELF an iteration — a
// one-dimensional root-find on the universal variable z. The textbook method and the
// shooting method differ not in kind (both iterate) but in what they iterate THROUGH:
// Lambert iterates through two-body algebra; shooting iterates through the world.
(Vector2d V1, Vector2d V2, int Iterations) Lambert(Vector2d r1v, Vector2d r2v, double tof, double mu)
{
    double r1 = r1v.Length, r2 = r2v.Length;
    double cross = r1v.X * r2v.Y - r1v.Y * r2v.X;
    double dTheta = Math.Acos(Math.Clamp(r1v.Dot(r2v) / (r1 * r2), -1.0, 1.0));
    if (cross < 0)
    {
        dTheta = Math.Tau - dTheta; // prograde transfer
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
            ? double.NegativeInfinity // arc not geometrically possible this far hyperbolic
            : Math.Pow(y / StumpffC(z), 1.5) * StumpffS(z) + A * Math.Sqrt(y) - Math.Sqrt(mu) * tof;
    }

    // Bisection on z: below the root the arc flies too fast (error < 0), above too slow.
    // zHi just under (2pi)^2 = one full revolution; multi-rev arcs are out of scope on purpose.
    double zLo = -100.0, zHi = Math.Tau * Math.Tau - 1e-9;
    int iterations = 0;
    while (zHi - zLo > 1e-12 && iterations < 200)
    {
        double zMid = 0.5 * (zLo + zHi);
        if (TofError(zMid) > 0) { zHi = zMid; } else { zLo = zMid; }
        iterations++;
    }

    // Honesty check: bisection always RETURNS a z — verify it actually solved the equation.
    // (A sign-change bracket can be a lie when the requested arc has no single-rev solution.)
    double zSol2 = 0.5 * (zLo + zHi);
    double achievedTofSeconds = (TofError(zSol2) + Math.Sqrt(mu) * tof) / Math.Sqrt(mu);
    if (double.IsNaN(achievedTofSeconds) || Math.Abs(achievedTofSeconds - tof) > 1.0)
    {
        return (Vector2d.Zero, Vector2d.Zero, -1); // no honest solution
    }

    double ySol = Y(0.5 * (zLo + zHi));
    double f = 1 - ySol / r1;
    double g = A * Math.Sqrt(ySol / mu);
    double gDot = 1 - ySol / r2;
    return ((r2v - r1v * f) / g, (r2v * gDot - r1v) / g, iterations);
}

// ===================================================================================
// The shooting method — lesson 13's Newton, unknowns now the DEPARTURE VELOCITY
// ===================================================================================
(Vector2d V, List<(int Iter, double MissKm)> Trace, bool Converged) Shoot(
    Simulator sim, Vector2d position, double t0, Vector2d vSeed, Vector2d target, double tArrive,
    double tolMeters, int maxIterations)
{
    const double Eps = 1.0; // m/s finite-difference probe: big enough to average over the
                            // adaptive stepper's quantization, small enough to stay linear
    Vector2d Fly(Vector2d v) => sim.RunAdaptive(new ShipState(position, v, t0), tArrive - t0).Position;

    Vector2d v = vSeed;
    var trace = new List<(int, double)>();
    for (int iter = 0; iter < maxIterations; iter++)
    {
        Vector2d miss = Fly(v) - target;
        trace.Add((iter, miss.Length / 1000));
        if (miss.Length < tolMeters)
        {
            return (v, trace, true);
        }

        Vector2d colX = (Fly(v + new Vector2d(Eps, 0)) - (miss + target)) / Eps;
        Vector2d colY = (Fly(v + new Vector2d(0, Eps)) - (miss + target)) / Eps;
        double det = colX.X * colY.Y - colX.Y * colY.X;
        var step = new Vector2d(
            -(colY.Y * miss.X - colY.X * miss.Y) / det,
            -(-colX.Y * miss.X + colX.X * miss.Y) / det);
        if (step.Length > 500) // trust region, lesson 13's lesson
        {
            step = step.Normalized() * 500;
        }

        v += step;
    }

    return (v, trace, false);
}

// ===================================================================================
// The contract: depart Earth on day 100, BE AT MARS on day 310. No choosing.
// ===================================================================================
double t0 = 100 * Day;
double tArrive = 310 * Day;
double tof = tArrive - t0;
ShipState departure = RoutePlanner.DepartureState(solEphemeris, "earth", "mars", t0);
// The aim point is the capture envelope's DOORSTEP, 500,000 km sunward-out from Mars
// (lesson 7's window scale) — no navigator aims for the core of a planet, and a shooter
// that did would be skating on Mars's own steep near-field gravity at the endpoint.
Vector2d marsAtArrival = solEphemeris.Position("mars", tArrive);
Vector2d target = marsAtArrival + marsAtArrival.Normalized() * 5e8;
Vector2d earthV = BodyVelocity(solEphemeris, "earth", t0);
Vector2d marsV = BodyVelocity(solEphemeris, "mars", tArrive);

Console.WriteLine("=== Section A: Lambert's answer, checked in Lambert's own universe ===");
Console.WriteLine($"contract: depart day {t0 / Day:F0}, be at Mars's capture doorstep " +
    $"(500,000 km out) on day {tArrive / Day:F0} (TOF {tof / Day:F0} days)");
var (v1, v2, zIters) = Lambert(departure.Position, target, tof, SunMu);
Console.WriteLine($"universal-variable root-find: {zIters} bisection iterations on z (yes, the 'analytic' method iterates too)");
Console.WriteLine($"Lambert departure velocity: {v1.Length:F1} m/s heliocentric");
Console.WriteLine($"  dv1 vs Earth = {(v1 - earthV).Length:F1} m/s, dv2 vs Mars at arrival = {(marsV - v2).Length:F1} m/s");
var flySunOnly = new Simulator(sunOnly, timeStepSeconds: 60);
Console.WriteLine();
Console.WriteLine("Flown in a SUN-ONLY world — Lambert's own universe — at successively finer cruise steps:");
Console.WriteLine($"{"max dt (s)",10}  {"miss at day 310 (km)",22}  {"endpoint vel vs Lambert v2 (m/s)",34}");
foreach (double maxDt in new[] { 3600.0, 600.0, 60.0 })
{
    ShipState arr = flySunOnly.RunAdaptive(new ShipState(departure.Position, v1, t0), tof, maxTimeStep: maxDt);
    Console.WriteLine($"{maxDt,10:F0}  {(arr.Position - target).Length / 1000,22:N0}  {(arr.Velocity - v2).Length,34:F2}");
}

Console.WriteLine("-> the residual shrinks roughly linearly with dt: it is the INTEGRATOR's first-order");
Console.WriteLine("   truncation, not Lambert's error. In the one-attractor universe Curtis ch. 5 assumes,");
Console.WriteLine("   the formula is exactly right — the disagreement above is the game's cruise dt, priced.");
Console.WriteLine();

Console.WriteLine("=== Section B: the same velocity, flown in universes that actually have planets ===");
Console.WriteLine($"{"world",-28}{"miss at day 310 (km)",22}");
foreach ((string label, ICelestialEphemeris eph) in new (string, ICelestialEphemeris)[]
{
    ("sun only", sunOnly),
    ("sun + Earth + Mars", sunEarthMars),
    ("all nine bodies", solEphemeris),
})
{
    var sim = new Simulator(eph, timeStepSeconds: 60);
    ShipState arrived = sim.RunAdaptive(new ShipState(departure.Position, v1, t0), tof);
    Console.WriteLine($"{label,-28}{(arrived.Position - target).Length / 1000,22:N0}");
}

Console.WriteLine("-> the ship departs 5,000,000 km from Earth and still feels her; Mars pulls at the far end.");
Console.WriteLine("   Two-body Lambert cannot know any of that — the error is structural, not numerical.");
Console.WriteLine();

Console.WriteLine("=== Section C: shooting through the real world, seeded by Lambert ===");
var flySol = new Simulator(solEphemeris, timeStepSeconds: 60);
const double Tol = 5_000_000; // 5,000 km — arrival-envelope work, lesson 7's scale
var lambertSeeded = Shoot(flySol, departure.Position, t0, v1, target, tArrive, Tol, 12);
Console.WriteLine($"{"iter",4}  {"miss (km)",14}");
foreach ((int iter, double missKm) in lambertSeeded.Trace)
{
    Console.WriteLine($"{iter,4}  {missKm,14:N1}");
}

Console.WriteLine($"converged: {lambertSeeded.Converged} (tolerance {Tol / 1000:N0} km)");
Console.WriteLine($"correction the real world demanded on top of Lambert: {(lambertSeeded.V - v1).Length:F2} m/s");
Console.WriteLine();

Console.WriteLine("--- C2: the seed is not a nicety — Newton from a NAIVE seed ---");
Vector2d naiveSeed = (target - departure.Position) / tof; // constant-velocity gunnery guess
Console.WriteLine($"naive straight-line seed: {naiveSeed.Length:F1} m/s heliocentric, " +
    $"{(naiveSeed - v1).Length:F1} m/s away from Lambert's answer");
var naiveSeeded = Shoot(flySol, departure.Position, t0, naiveSeed, target, tArrive, Tol, 150);
Console.WriteLine($"{"iter",4}  {"miss (km)",14}");
foreach ((int iter, double missKm) in naiveSeeded.Trace)
{
    if (iter % 15 == 0 || iter == naiveSeeded.Trace.Count - 1)
    {
        Console.WriteLine($"{iter,4}  {missKm,14:N1}");
    }
}

int naiveFlights = naiveSeeded.Trace.Count * 3, lambertFlights = lambertSeeded.Trace.Count * 3;
Console.WriteLine($"converged: {naiveSeeded.Converged} — {naiveSeeded.Trace.Count} Newton iterations " +
    $"(~{naiveFlights} simulator flights) vs {lambertSeeded.Trace.Count} (~{lambertFlights} flights) from the Lambert seed");
Console.WriteLine("-> this is how real mission design works: an analytic two-body answer as the seed, a");
Console.WriteLine("   differential corrector through the full force model to finish. Lambert proposes,");
Console.WriteLine("   the integrator disposes — and a good seed is most of the meal.");
Console.WriteLine();

// ===================================================================================
// Section D — the porkchop plate: every contract you COULD have signed
// ===================================================================================
Console.WriteLine("=== Section D: the porkchop plate (total dv, km/s, by departure day and TOF) ===");
Console.WriteLine("Lambert per cell (the seed's whole value is being this cheap: no integration, ~50");
Console.WriteLine("bisection steps of algebra per cell). Total dv = leave Earth's orbit + match Mars's.");
Console.WriteLine();
double[] tofDays = [120, 160, 200, 240, 280, 320, 360, 400];
Console.Write("dep day |");
foreach (double td in tofDays)
{
    Console.Write($"{td,7:F0}");
}

Console.WriteLine("  <- TOF (days)");
Console.WriteLine(new string('-', 8 + 7 * tofDays.Length + 2));
(double dep, double tofD, double dv, Vector2d v1, Vector2d v2) best = (0, 0, double.MaxValue, Vector2d.Zero, Vector2d.Zero);
for (double depDay = 0; depDay <= 760; depDay += 76)
{
    Console.Write($"{depDay,7:F0} |");
    foreach (double td in tofDays)
    {
        double dep = depDay * Day;
        ShipState from = RoutePlanner.DepartureState(solEphemeris, "earth", "mars", dep);
        Vector2d to = solEphemeris.Position("mars", dep + td * Day);
        var (lv1, lv2, cellIters) = Lambert(from.Position, to, td * Day, SunMu);
        if (cellIters < 0)
        {
            Console.Write("      -"); // no honest single-rev solution for this cell
            continue;
        }

        double dv = (lv1 - BodyVelocity(solEphemeris, "earth", dep)).Length
                  + (BodyVelocity(solEphemeris, "mars", dep + td * Day) - lv2).Length;
        if (dv < best.dv)
        {
            best = (depDay, td, dv, lv1, lv2);
        }

        Console.Write($"{dv / 1000,7:F1}");
    }

    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine($"cheapest cell: depart day {best.dep:F0}, TOF {best.tofD:F0} days, total dv {best.dv / 1000:F2} km/s");
// Verify the cheapest cell by FLYING it (sun-only, fine dt): the plate is only as honest
// as its cells. A cell that doesn't land on Mars when flown is a solver bug, not a bargain.
{
    ShipState from = RoutePlanner.DepartureState(solEphemeris, "earth", "mars", best.dep * Day);
    Vector2d to = solEphemeris.Position("mars", (best.dep + best.tofD) * Day);
    ShipState flown = flySunOnly.RunAdaptive(
        new ShipState(from.Position, best.v1, best.dep * Day), best.tofD * Day, maxTimeStep: 60);
    Console.WriteLine($"cheapest cell VERIFIED by flying it: lands {(flown.Position - to).Length / 1000:N0} km " +
        $"from Mars, endpoint velocity {(flown.Velocity - best.v2).Length:F2} m/s from Lambert's v2");
    Console.WriteLine($"  its split: dv1 = {(best.v1 - BodyVelocity(solEphemeris, "earth", best.dep * Day)).Length:F1} m/s, " +
        $"dv2 = {(BodyVelocity(solEphemeris, "mars", (best.dep + best.tofD) * Day) - best.v2).Length:F1} m/s");
}
double rE = 1.496e11, rM = 2.2794e11, aT = (rE + rM) / 2;
double hohmannTotal = Math.Abs(Math.Sqrt(SunMu * (2 / rE - 1 / aT)) - Math.Sqrt(SunMu / rE))
                    + Math.Abs(Math.Sqrt(SunMu / rM) - Math.Sqrt(SunMu * (2 / rM - 1 / aT)));
Console.WriteLine($"The Hohmann analytic total for this pair (lesson 5) is {hohmannTotal / 1000:F2} km/s. The plate's floor");
Console.WriteLine("sits BELOW it — not because Lambert out-optimized Hohmann (it can't; Hohmann is the two-impulse");
Console.WriteLine("minimum between these orbits) but because the plate prices the GAME's actual departure state:");
Console.WriteLine("the spawn point rides 5,000,000 km up the sun's hill from Earth's orbit while keeping Earth's");
Console.WriteLine("full speed — a head start the textbook geometry doesn't include. The formula answers the");
Console.WriteLine("question it was asked. The plate answers the question the CAPTAIN asked — and the plate,");
Console.WriteLine("scanned over both axes, is what 'transfer window' actually MEANS, computed.");
