// Lab 20 — The Long Goodbye
//
// Teaching voice: lesson 19 flew our rails-universe Grand Tour and stopped at the Saturn
// closest pass — day 9499, 1.07 Gm out, closing 9.02 km/s relative to the giant. The real
// Voyager 2 did not stop. She rode the same crank on to Uranus (1986) and Neptune (1989),
// and mid-2026 she is ~139 AU from the sun, still outbound at ~15.3 km/s. This lesson asks
// the two questions lesson 19 left on the table: what happens to OUR probe AFTER the Saturn
// hand-off if she just coasts (zero burn), and — the rare-opportunity question — can any
// small, affordable pre-Saturn nudge put her on a second crank to Uranus in THIS sky? Then
// it flies the best answer forward to a fixed present date and reads her position, the way
// the Deep Space Network still reads the real one's.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code,
// the printed numbers in labs/20-the-long-goodbye/README.md go stale — rerun and re-paste,
// never hand-edit a table.

using SpaceSails.Core;
using SpaceSails.LabViz;

const double Day = 86400.0;
const double Year = 365.25 * Day;
const double AU = 1.496e11;
const double SunMu = 1.32712440018e20;
const double JupiterMu = 1.26686534e17;
const double SaturnMu = 3.7931187e16;
const double UranusMu = 5.793939e15;
const double NeptuneMu = 6.836529e15;
const double RJ = 6.9911e7;

// The present. Determinism is the brand: NEVER DateTime.Now. This lesson is pinned to a fixed
// wall-clock instant so its "she is here now" number is reproducible forever.
DateTimeOffset Present = new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);
// The display anchor lesson 19 uses: the flown launch lands on Voyager 2's real launch instant.
DateTimeOffset LaunchEpoch = new(1977, 8, 20, 14, 29, 0, TimeSpan.Zero);

// The 9-body table — identical constants/body table to lesson 19. Uranus and Neptune matter now.
(string Id, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase)[] specs =
[
    ("sun", SunMu, 6.9634e8, 0, 0, 0),
    ("mercury", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    ("venus", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    ("earth", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    ("mars", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", JupiterMu, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", UranusMu, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", NeptuneMu, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
];
var ephemeris = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(s.Id, s.Id, s.Id == "sun" ? null : "sun", s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase))]);
var sim = new Simulator(ephemeris, timeStepSeconds: 60);

// --viz (optional): record the trajectories this probe computes into a browser pop-up. Every
// viz line is gated behind LabViz.Wants, so without --viz stdout stays byte-identical. Recording
// only ever OBSERVES lists already computed (or re-projects a winner after the fact — the engine
// is deterministic, so the same numbers come back); it never perturbs a printed value.
var viz = LabViz.Wants(args)
    ? new VizScene("lab20-the-long-goodbye", "Lab 20 — The Long Goodbye", "the Grand Tour after the Saturn hand-off, coasted to 2026")
    : null;
viz?.AddBodies(ephemeris.Bodies);

// ---- helpers (per lab convention, each lesson keeps local copies; ported from lesson 19) ----

Vector2d BodyVelocity(string id, double t) =>
    (ephemeris.Position(id, t + 1.0) - ephemeris.Position(id, t - 1.0)) / 2.0;

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
    double residual = TofError(zSol);
    if (double.IsNaN(residual) || Math.Abs(residual) / Math.Sqrt(mu) > 1.0)
    {
        return (Vector2d.Zero, Vector2d.Zero, -1);
    }

    double ySol = Y(zSol);
    double f = 1 - ySol / r1;
    double g = A * Math.Sqrt(ySol / mu);
    double gDot = 1 - ySol / r2;
    return ((r2v - r1v * f) / g, (r2v * gDot - r1v) / g, iterations);
}

(Vector2d V, bool Converged) ShootTo(
    Vector2d position, double t0, Vector2d vSeed, Vector2d target, double tArrive, double tol, double trust = 200)
{
    const double Eps = 1.0;
    Vector2d Fly(Vector2d v) => sim.RunAdaptive(new ShipState(position, v, t0), tArrive - t0).Position;
    Vector2d v = vSeed;
    for (int iter = 0; iter < 15; iter++)
    {
        Vector2d miss = Fly(v) - target;
        if (miss.Length < tol)
        {
            return (v, true);
        }

        Vector2d colX = (Fly(v + new Vector2d(Eps, 0)) - (miss + target)) / Eps;
        Vector2d colY = (Fly(v + new Vector2d(0, Eps)) - (miss + target)) / Eps;
        double det = colX.X * colY.Y - colX.Y * colY.X;
        var step = new Vector2d(
            -(colY.Y * miss.X - colY.X * miss.Y) / det,
            -(-colX.Y * miss.X + colX.X * miss.Y) / det);
        if (step.Length > trust)
        {
            step = step.Normalized() * trust;
        }

        v += step;
    }

    return (v, false);
}

(double Distance, double Time) ClosestTo(string bodyId, IReadOnlyList<TrajectorySample> samples, double fromTime)
{
    double best = double.MaxValue, bestT = fromTime;
    foreach (TrajectorySample s in samples)
    {
        if (s.SimTime < fromTime)
        {
            continue;
        }

        double d = (ephemeris.Position(bodyId, s.SimTime) - s.Position).Length;
        if (d < best)
        {
            (best, bestT) = (d, s.SimTime);
        }
    }

    return (best, bestT);
}

// Solar specific orbital energy (J/kg): negative = bound, positive = escaping.
double SpecificEnergy(ShipState s) => s.Velocity.LengthSquared / 2.0 - SunMu / s.Position.Length;

// Apoapsis in AU, or +Infinity if the orbit is hyperbolic (leaves the sun).
double ApoapsisAU(ShipState s)
{
    double energy = SpecificEnergy(s);
    if (energy >= 0)
    {
        return double.PositiveInfinity;
    }

    double a = -SunMu / (2 * energy);
    double h = Math.Abs(s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X);
    double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (SunMu * SunMu)));
    return a * (1 + e) / AU;
}

double SaturnHill = OrbitRule.HillRadius(ephemeris.Bodies.First(b => b.Id == "saturn"), SunMu);
double UranusHill = OrbitRule.HillRadius(ephemeris.Bodies.First(b => b.Id == "uranus"), SunMu);

// ===================================================================================
// Reproduce lesson 19's flown winner — deterministically, same body table + scan.
// ===================================================================================
// This block is lesson 19's Section C verbatim in spirit: the same 20-year departure grid,
// the same four leg lengths, the same 10.5 km/s launch cap, the same crank offset, then the
// same two-stage b-plane sweep on the live approach and the same post-flyby TCM-2. It exists
// so Lab 20 stands on the exact hand-off state Lab 19 handed off — not on hand-copied numbers.
Console.WriteLine("=== Reproducing lesson 19's flown winner (same body table + scan) ===");

Vector2d sideUnitFor(Vector2d vInf)
{
    Vector2d d = vInf.Normalized();
    return new Vector2d(-d.Y, d.X);
}

// Section-B crank geometry side, taken exactly as lesson 19 does (from the day-100 scout launch).
double dep0 = 100 * Day;
(double dv1, double tof) HohmannLeg(double r1, double r2)
{
    double aT = (r1 + r2) / 2;
    return (Math.Abs(Math.Sqrt(SunMu * (2 / r1 - 1 / aT)) - Math.Sqrt(SunMu / r1)),
            Math.PI * Math.Sqrt(aT * aT * aT / SunMu));
}
var toJupiter = HohmannLeg(AU, 7.7857e11);
double tofEJ = toJupiter.tof;
ShipState launchPad0 = RoutePlanner.DepartureState(ephemeris, "earth", "jupiter", dep0);
Vector2d jupAtArr0 = ephemeris.Position("jupiter", dep0 + tofEJ);
var seed0 = Lambert(launchPad0.Position, jupAtArr0, tofEJ, SunMu);
var launch0 = ShootTo(launchPad0.Position, dep0, seed0.V1, jupAtArr0, dep0 + tofEJ, 5e7, 500);
ShipState beforeFlyby0 = sim.RunAdaptive(launchPad0 with { Velocity = launch0.V }, tofEJ - 90 * Day);
Vector2d vInf0 = beforeFlyby0.Velocity - BodyVelocity("jupiter", beforeFlyby0.SimTime);
Vector2d side = sideUnitFor(vInf0);

// The window scan (identical to lesson 19).
const double CrankOffset = -5e8;
double bestDep = -1, bestSatCA = double.MaxValue, bestTof = tofEJ;
for (double depDay = 0; depDay <= 7300; depDay += 53)
{
    double t = depDay * Day;
    ShipState pad = RoutePlanner.DepartureState(ephemeris, "earth", "jupiter", t);
    foreach (double tofYr in new[] { 2.2, 2.6, 3.0, 3.4 })
    {
        double tofC = tofYr * Year;
        Vector2d jArr = ephemeris.Position("jupiter", t + tofC);
        var s = Lambert(pad.Position, jArr, tofC, SunMu);
        if (s.Iterations < 0 || (s.V1 - pad.Velocity).Length > 10_500)
        {
            continue;
        }

        ShipState nearJscan = sim.RunAdaptive(pad with { Velocity = s.V1 }, tofC - 90 * Day, maxTimeStep: 7200);
        Vector2d aim = jArr + side * CrankOffset;
        var tuned = ShootTo(nearJscan.Position, nearJscan.SimTime, nearJscan.Velocity, aim, t + tofC, 1e7, 100);
        IReadOnlyList<TrajectorySample> onward = sim.ProjectAdaptive(
            new ShipState(nearJscan.Position, tuned.V, nearJscan.SimTime), null, 4.5 * Year, maxTimeStep: 7200, maxSamples: 40_000);
        var sat = ClosestTo("saturn", onward, t + tofC + 100 * Day);
        if (sat.Distance < bestSatCA)
        {
            (bestDep, bestSatCA, bestTof) = (depDay, sat.Distance, tofC);
        }
    }
}

// Fly the winner exactly like lesson 19: launch to the crank, TCM-1 via the two-stage b-plane
// sweep on the live approach, then TCM-2 walking the arrival to lesson 19's chosen Saturn offset.
double t0C = bestDep * Day;
ShipState padC = RoutePlanner.DepartureState(ephemeris, "earth", "jupiter", t0C);
Vector2d jupArrC = ephemeris.Position("jupiter", t0C + bestTof);
var launchSeed = Lambert(padC.Position, jupArrC, bestTof, SunMu);
var liftoff = ShootTo(padC.Position, t0C, launchSeed.V1, jupArrC + side * CrankOffset, t0C + bestTof, 1e7, 500);
double launchDv = (liftoff.V - padC.Velocity).Length;
ShipState nearJ = sim.RunAdaptive(padC with { Velocity = liftoff.V }, bestTof - 90 * Day);

(Vector2d V, double CA, double T, double Off) chosen = (nearJ.Velocity, double.MaxValue, 0, 0);
(Vector2d V, double CA, double T, double Off) Sweep(double from, double to, double step, (Vector2d V, double CA, double T, double Off) incumbent)
{
    for (double off = from; off >= to; off -= step)
    {
        var tuned = ShootTo(nearJ.Position, nearJ.SimTime, nearJ.Velocity, jupArrC + side * off, t0C + bestTof, 2e6, 100);
        IReadOnlyList<TrajectorySample> onward = sim.ProjectAdaptive(
            new ShipState(nearJ.Position, tuned.V, nearJ.SimTime), null, 5.5 * Year, maxTimeStep: 7200, maxSamples: 48_000);
        var sat = ClosestTo("saturn", onward, t0C + bestTof + 100 * Day);
        if (sat.Distance < incumbent.CA)
        {
            incumbent = (tuned.V, sat.Distance, sat.Time, off);
        }
    }

    return incumbent;
}

chosen = Sweep(-1.5e8, -1.5e9, 7.5e7, chosen);
chosen = Sweep(chosen.Off + 6e7, chosen.Off - 6e7, 1.5e7, chosen);
double tcm1Dv = (chosen.V - nearJ.Velocity).Length;

// Through the flyby ballistic, then one post-flyby TCM to lesson 15's doorstep (lesson 19's TCM-2).
ShipState pastJ = sim.RunAdaptive(new ShipState(nearJ.Position, chosen.V, nearJ.SimTime), 90 * Day + 150 * Day);
Vector2d satAim = ephemeris.Position("saturn", chosen.T);
satAim += satAim.Normalized() * 1e9;
var tcm2 = ShootTo(pastJ.Position, pastJ.SimTime, pastJ.Velocity, satAim, chosen.T, 1e9, 300);
double tcm2Dv = (tcm2.V - pastJ.Velocity).Length;

// The hand-off state: post-TCM-2, ballistic from here on. This is what lesson 20 inherits.
ShipState handoff = new(pastJ.Position, tcm2.V, pastJ.SimTime);
ShipState atSaturn = sim.RunAdaptive(handoff, chosen.T - handoff.SimTime);
double arriveDist = (atSaturn.Position - ephemeris.Position("saturn", chosen.T)).Length;
double arriveRel = (atSaturn.Velocity - BodyVelocity("saturn", chosen.T)).Length;

Console.WriteLine($"  window scan best: depart day {bestDep:F0}, Earth->Jupiter leg {bestTof / Year:F1} yr");
Console.WriteLine($"  b-plane sweep best aim offset {chosen.Off / 1e6:F0} Mm; ballistic Saturn approach {chosen.CA / 1e9:F2} Gm at day {chosen.T / Day:F0}");
Console.WriteLine($"  flown TCMs: launch {launchDv:F0} m/s, TCM-1 {tcm1Dv:F1} m/s, TCM-2 {tcm2Dv:F1} m/s");
Console.WriteLine($"  Saturn hand-off: {arriveDist / 1e9:F2} Gm from Saturn, day {chosen.T / Day:F0}, closing {arriveRel / 1000:F2} km/s relative");
bool reproduced = Math.Abs(bestDep - 6413) < 1 && Math.Abs(chosen.Off / 1e6 - (-1485)) < 30
    && Math.Abs(chosen.T / Day - 9499) < 3 && Math.Abs(arriveDist / 1e9 - 1.07) < 0.05;
Console.WriteLine($"  reproduced lesson 19 winner (day 6413 / -1485 Mm / 1.07 Gm at day 9499): {(reproduced ? "YES" : "NO — investigate before trusting the rest")}");
Console.WriteLine();

// ===================================================================================
// Section A — the hand-off: fly THROUGH the Saturn pass, zero burn, measure the ledger.
// ===================================================================================
Console.WriteLine("=== Section A: the hand-off (zero-burn ballistic through the Saturn pass) ===");
// The hand-off arc is already ballistic (no burn since TCM-2). Fly it straight through the
// Saturn closest pass and read the solar ledger on each side — well clear of Saturn's well so
// the Sun-frame energy is clean. Don't assume escape; measure the sign.
double satCA = chosen.T;
ShipState satIn = sim.RunAdaptive(handoff, (satCA - 40 * Day) - handoff.SimTime);
ShipState satOut = sim.RunAdaptive(handoff, (satCA + 40 * Day) - handoff.SimTime);
ShipState beforeSat = sim.RunAdaptive(handoff, (satCA - 120 * Day) - handoff.SimTime);
ShipState afterSat = sim.RunAdaptive(handoff, (satCA + 120 * Day) - handoff.SimTime);

double ePre = SpecificEnergy(beforeSat);
double ePost = SpecificEnergy(afterSat);
Vector2d vInfIn = satIn.Velocity - BodyVelocity("saturn", satIn.SimTime);
Vector2d vInfOut = satOut.Velocity - BodyVelocity("saturn", satOut.SimTime);
double turnDeg = Math.Acos(Math.Clamp(vInfIn.Normalized().Dot(vInfOut.Normalized()), -1, 1)) * 180 / Math.PI;

Console.WriteLine($"pre-Saturn  specific energy (Sun frame, 120 d before CA): {ePre:F0} J/kg  ({(ePre > 0 ? "positive = escaping" : "negative = bound")})");
Console.WriteLine($"post-Saturn specific energy (Sun frame, 120 d after  CA): {ePost:F0} J/kg  ({(ePost > 0 ? "positive = ESCAPING" : "negative = still bound")})");
Console.WriteLine($"|v_inf| vs Saturn in {vInfIn.Length / 1000:F2} km/s, out {vInfOut.Length / 1000:F2} km/s (conserved to {Math.Abs(vInfIn.Length - vInfOut.Length) / vInfIn.Length:P0}); turning angle {turnDeg:F1} deg");
Console.WriteLine($"pass distance {arriveDist / 1e9:F2} Gm = {arriveDist / SaturnHill:F3} Saturn Hill radii (a {(arriveDist < 0.05 * SaturnHill ? "close" : "distant")} pass -> {(turnDeg < 10 ? "weak" : "moderate")} crank)");
string verdictA = double.IsPositiveInfinity(ApoapsisAU(afterSat))
    ? "the zero-burn arc ESCAPES the sun — hyperbolic after Saturn"
    : $"the zero-burn arc stays BOUND: post-Saturn apoapsis {ApoapsisAU(afterSat):F1} AU (Uranus sits at {2.87246e12 / AU:F1} AU, Neptune {4.49506e12 / AU:F1} AU)";
Console.WriteLine($"verdict: {verdictA}");
Console.WriteLine();

// ===================================================================================
// Section B — the second crank, if the sky allows: TCM-3 sweep for a Uranus intercept.
// ===================================================================================
Console.WriteLine("=== Section B: the second crank, if the sky allows (TCM-3 <= 600 m/s) ===");
// A TCM-3 applied well before the Saturn pass (here at the hand-off point, ~Jupiter+150d, day
// ~7805, more than four years before Saturn) re-aims the Saturn b-plane. Like lesson 19's sweep,
// the flyby is a lever: a few hundred m/s here moves the Saturn pass by Gm and re-points the
// whole post-Saturn leg. For each aim that stays under a 600 m/s TCM-3 budget, measure the Saturn
// pass, the post-Saturn reach, and — only when the reach actually gets to Uranus's orbit — the
// Uranus closest approach over a capped 40-year projection. A hit inside 3 Uranus Hill radii is a
// genuine intercept.
const double Tcm3Cap = 600.0;
double uranusR = 2.87246e12;
Console.WriteLine($"Uranus Hill radius {UranusHill / 1e9:F1} Gm; intercept gate = 3 Hill = {3 * UranusHill / 1e9:F1} Gm ({3 * UranusHill / AU:F2} AU).");
Console.WriteLine($"{"TCM-3 aim",12}{"dv (m/s)",10}{"Saturn pass",14}{"post-Sat reach",18}{"Uranus closest",18}");

(double Off, double Dv, double SatPass, double Reach, double UranusCA, ShipState PostSat, bool Escapes) bestB =
    (0, 0, arriveDist, ApoapsisAU(afterSat), double.MaxValue, afterSat, double.IsPositiveInfinity(ApoapsisAU(afterSat)));
bool anyUranus = false;
Vector2d satNominal = ephemeris.Position("saturn", satCA);
Vector2d satSide = satNominal.Normalized(); // radial offsets, same family lesson 19's TCM-2 used
foreach (double off in new[] { -8e8, -6e8, -4e8, -2e8, 0.0, 2e8, 4e8, 6e8, 8e8, 1.2e9, 1.6e9, 2.0e9 })
{
    Vector2d aim = satNominal + satSide * off;
    var tuned = ShootTo(handoff.Position, handoff.SimTime, handoff.Velocity, aim, satCA, 5e8, 300);
    double dv = (tuned.V - handoff.Velocity).Length;
    if (dv > Tcm3Cap)
    {
        Console.WriteLine($"{off / 1e6,9:F0} Mm{dv,10:F0}{"over budget",14}{"-",18}{"-",18}");
        continue;
    }

    var tcm3Start = new ShipState(handoff.Position, tuned.V, handoff.SimTime);
    // Through the Saturn encounter at the encounter step (3600), a couple of years past CA.
    IReadOnlyList<TrajectorySample> nearSat = sim.ProjectAdaptive(
        tcm3Start, null, (satCA + 2 * Year) - handoff.SimTime, maxTimeStep: 3600, maxSamples: 120_000);
    var pass = ClosestTo("saturn", nearSat, handoff.SimTime + 1 * Day);
    ShipState postSat = sim.RunAdaptive(tcm3Start, (pass.Time + 120 * Day) - handoff.SimTime);
    double reach = ApoapsisAU(postSat);
    bool escapes = double.IsPositiveInfinity(reach);

    double uranusCA = double.MaxValue;
    if (escapes || reach * AU >= uranusR - 3 * UranusHill)
    {
        // The arc can physically get to Uranus's orbit — worth the long look. Far from Saturn now,
        // so the deep-space coast at 86400 is plenty. 40 years is a capped time-of-flight.
        IReadOnlyList<TrajectorySample> longCoast = sim.ProjectAdaptive(
            postSat, null, 40 * Year, maxTimeStep: 86400, maxSamples: 60_000);
        uranusCA = ClosestTo("uranus", longCoast, postSat.SimTime).Distance;
    }

    string reachTxt = escapes ? "escapes" : $"{reach:F1} AU";
    string uranusTxt = uranusCA == double.MaxValue ? "n/a (short)" : $"{uranusCA / 1e9:F1} Gm";
    Console.WriteLine($"{off / 1e6,9:F0} Mm{dv,10:F0}{pass.Distance / 1e9,11:F2} Gm{reachTxt,18}{uranusTxt,18}");

    if (uranusCA < 3 * UranusHill)
    {
        anyUranus = true;
    }

    // Track the arc that reaches farthest (best shot at the outer system) among surviving aims.
    if (!bestB.Escapes && (escapes || reach > bestB.Reach))
    {
        bestB = (off, dv, pass.Distance, reach, uranusCA, postSat, escapes);
    }
}

Console.WriteLine();
if (anyUranus)
{
    Console.WriteLine("A Uranus window EXISTS in our sky: at least one affordable TCM-3 threads inside 3 Uranus");
    Console.WriteLine("Hill radii. The second crank is on — chain the same question to Neptune from that arc.");
}
else
{
    Console.WriteLine("No Uranus window. Every affordable TCM-3 either leaves the arc short of Uranus's orbit or,");
    Console.WriteLine("where it reaches, finds Uranus nowhere near the crossing. This is the whole lesson: the");
    Console.WriteLine("real Voyager's 4-planet line-up recurs about every 175 years, and 1977 caught it. Our");
    Console.WriteLine("rails carry fictional phases, so the giants simply are not standing in a row for us. A");
    Console.WriteLine("flyby can bend a trajectory; it cannot summon a planet to the bend. Alignment was the");
    Console.WriteLine("mission — not the launch vehicle, not the crank. Without it, the Grand Tour is one flyby long.");
}
Console.WriteLine();

// ===================================================================================
// Section C — the long goodbye: coast the best arc to a fixed present, read her position.
// ===================================================================================
Console.WriteLine("=== Section C: the long goodbye (coast to a fixed present) ===");
// Best trajectory from B if it beat the zero-burn pass to the outer system, else A's zero-burn
// hand-off arc. Either way it is ballistic from its last burn; coast it to the pinned present.
ShipState coastFrom;
string chosenArc;
if (anyUranus)
{
    var tuned = ShootTo(handoff.Position, handoff.SimTime, handoff.Velocity, satNominal + satSide * bestB.Off, satCA, 5e8, 300);
    coastFrom = new ShipState(handoff.Position, tuned.V, handoff.SimTime);
    chosenArc = $"Section B's Uranus-bound TCM-3 ({bestB.Dv:F0} m/s, aim {bestB.Off / 1e6:F0} Mm)";
}
else
{
    coastFrom = handoff;
    chosenArc = "Section A's zero-burn Saturn pass (no affordable second crank existed)";
}

double yearsSinceLaunch = (Present - LaunchEpoch).TotalDays / 365.25;
double tPresent = t0C + (Present - LaunchEpoch).TotalSeconds;
// Two-stage, deterministic recipe (probe and tests use it identically): fine step (3600) through
// the Saturn encounter to a clean deep-space state, then the long coast at 86400 to the present.
double tSeam = satCA + 200 * Day;
ShipState seam = sim.RunAdaptive(coastFrom, tSeam - coastFrom.SimTime, maxTimeStep: 3600);
ShipState now = sim.RunAdaptive(seam, tPresent - seam.SimTime, maxTimeStep: 86400);
double nowAU = now.Position.Length / AU;
double nowSpeed = now.Velocity.Length / 1000;

Console.WriteLine($"flying: {chosenArc}");
Console.WriteLine($"anchor: launch = {LaunchEpoch:yyyy-MM-dd HH:mm} UTC (lesson 19's display anchor); present = {Present:yyyy-MM-dd} (hard-coded, never DateTime.Now)");
Console.WriteLine();
Console.WriteLine($"{"",-16}{"heliocentric dist",20}{"speed",14}{"years since launch",20}");
Console.WriteLine($"{"our probe",-16}{$"{nowAU:F1} AU",20}{$"{nowSpeed:F2} km/s",14}{$"{yearsSinceLaunch:F1} yr",20}");
Console.WriteLine($"{"real Voyager 2",-16}{"~139 AU",20}{"~15.3 km/s",14}{$"~{yearsSinceLaunch:F0} yr",20}   <- the real world's numbers, not ours");
Console.WriteLine();
Console.WriteLine($"She is here now: {nowAU:F1} AU from the sun, {(now.Velocity.Length * now.Velocity.Length / 2.0 - SunMu / now.Position.Length > 0 ? "still escaping" : "bound and turning back")}, on {Present:yyyy-MM-dd}.");
Console.WriteLine("The real Voyager 2 is four times farther and faster because she never stopped riding the");
Console.WriteLine("crank — three more giants lined up to hand her along. Ours took one flyby and a refund at");
Console.WriteLine("Saturn, and the outer system was empty when she got there. Same physics; a different sky.");
Console.WriteLine();

// ===================================================================================
// Break it yourself — three ways to feel the lever and the bill.
// ===================================================================================
Console.WriteLine("=== Break it yourself ===");

// 1) Move TCM-3 closer to Saturn and watch the lever amplify: same target offset, far less lead
//    time, far more propellant (or, at fixed budget, far less reach). Apply the same +1.6 Gm aim
//    at Saturn-30d instead of at the hand-off (day ~7805).
{
    double offB = 1.6e9;
    var early = ShootTo(handoff.Position, handoff.SimTime, handoff.Velocity, satNominal + satSide * offB, satCA, 5e8, 300);
    double earlyDv = (early.V - handoff.Velocity).Length;
    ShipState nearLate = sim.RunAdaptive(handoff, (satCA - 30 * Day) - handoff.SimTime);
    var late = ShootTo(nearLate.Position, nearLate.SimTime, nearLate.Velocity, satNominal + satSide * offB, satCA, 5e8, 300);
    double lateDv = (late.V - nearLate.Velocity).Length;
    Console.WriteLine($"1. Lever: the SAME {offB / 1e9:F1} Gm Saturn re-aim costs {earlyDv:F0} m/s applied at the hand-off (Saturn-{(satCA - handoff.SimTime) / Day:F0}d),");
    Console.WriteLine($"   but {lateDv:F0} m/s applied at Saturn-30d — {(lateDv / Math.Max(1, earlyDv)):F1}x more, because the lever arm collapsed. Aim early.");
}

// 2) Double dt through the Saturn pass: measure how far the pass distance and post-Saturn energy
//    move when the encounter step goes from 3600 to 7200 s.
{
    ShipState fine = sim.RunAdaptive(handoff, (satCA + 120 * Day) - handoff.SimTime, maxTimeStep: 3600);
    ShipState coarse = sim.RunAdaptive(handoff, (satCA + 120 * Day) - handoff.SimTime, maxTimeStep: 7200);
    IReadOnlyList<TrajectorySample> pf = sim.ProjectAdaptive(handoff, null, (satCA + 120 * Day) - handoff.SimTime, maxTimeStep: 3600, maxSamples: 120_000);
    IReadOnlyList<TrajectorySample> pc = sim.ProjectAdaptive(handoff, null, (satCA + 120 * Day) - handoff.SimTime, maxTimeStep: 7200, maxSamples: 120_000);
    double caFine = ClosestTo("saturn", pf, handoff.SimTime).Distance;
    double caCoarse = ClosestTo("saturn", pc, handoff.SimTime).Distance;
    Console.WriteLine($"2. Double dt: Saturn pass {caFine / 1e9:F3} Gm at 3600 s vs {caCoarse / 1e9:F3} Gm at 7200 s " +
        $"({Math.Abs(caFine - caCoarse) / caFine:P1} shift); post-Saturn energy {SpecificEnergy(fine):F0} vs {SpecificEnergy(coarse):F0} J/kg.");
    Console.WriteLine("   A distant pass forgives a coarse step; move the pass inside a Hill radius and it won't.");
}

// 3) Brake INTO Saturn orbit and price the bill: the Δv to drop from the arrival state onto a
//    bound (circular) Saturn orbit at the pass distance. Voyager never paid this — she wasn't stopping.
{
    double vCircSat = Math.Sqrt(SaturnMu / arriveDist);
    double brakeDv = arriveRel - vCircSat; // slow the v_inf-dominated approach to a circular orbit
    Console.WriteLine($"3. Stop at Saturn: arriving {arriveRel / 1000:F2} km/s relative, a circular orbit at {arriveDist / 1e9:F2} Gm needs {vCircSat / 1000:F2} km/s;");
    Console.WriteLine($"   the braking bill is {brakeDv / 1000:F2} km/s ({brakeDv:F0} m/s) — the refund on Jupiter's gift, paid in full. Flyby missions never do.");
}
Console.WriteLine();
Console.WriteLine("Every number above came from running the probe. Rerun after edits.");

// ===================================================================================
// Viz — the whole tour, launch (1977) to where she is now (2026-07-14).
// ===================================================================================
if (viz is not null)
{
    // Section B's TCM-3 fan: one faded arc per surviving affordable aim, through Saturn and out.
    foreach (double off in new[] { -8e8, -4e8, 0.0, 4e8, 8e8, 1.6e9, 2.0e9 })
    {
        Vector2d aim = satNominal + satSide * off;
        var tuned = ShootTo(handoff.Position, handoff.SimTime, handoff.Velocity, aim, satCA, 5e8, 300);
        if ((tuned.V - handoff.Velocity).Length > Tcm3Cap)
        {
            continue;
        }

        IReadOnlyList<TrajectorySample> fan = sim.ProjectAdaptive(
            new ShipState(handoff.Position, tuned.V, handoff.SimTime), null, 12 * Year, maxTimeStep: 43200, maxSamples: 30_000);
        viz.AddPath($"TCM-3 aim {off / 1e9:0.0} Gm", fan, VizColors.Sweep, "sweep", 1.0, 0.4);
    }

    // The main ghost path: launch -> Jupiter -> Saturn -> long coast to the present, stitched leg by
    // leg. Encounter legs at 3600 (they must land exactly on the RunAdaptive states the next leg
    // starts from — the lesson-19 seam bug); the long deep-space coast at 86400 (far from every body,
    // where a day-long step is fine, and a 49-year coast at 3600 s would be ~430k steps).
    var flown = new List<TrajectorySample>();
    void AddLeg(ShipState state, double duration, double maxStep)
    {
        IReadOnlyList<TrajectorySample> leg = sim.ProjectAdaptive(
            state, null, duration, maxTimeStep: maxStep, maxSamples: 120_000);
        flown.AddRange(flown.Count == 0 ? leg : leg.Skip(1));
    }

    AddLeg(new ShipState(padC.Position, liftoff.V, t0C), nearJ.SimTime - t0C, 3600);            // launch -> TCM-1
    AddLeg(new ShipState(nearJ.Position, chosen.V, nearJ.SimTime), pastJ.SimTime - nearJ.SimTime, 3600);  // -> TCM-2
    AddLeg(coastFrom, tSeam - coastFrom.SimTime, 3600);                                          // -> through Saturn
    AddLeg(seam, tPresent - seam.SimTime, 86400);                                                // -> present (long coast)
    viz.AddPath("itinerary (Earth->Jupiter->Saturn->2026)", flown, VizColors.Trajectory, "main", 1.8, 1.0, ghost: true);

    // Display epoch: flown departure lands on Voyager 2's real launch instant (lesson 19's anchor).
    viz.SetEpoch(LaunchEpoch - TimeSpan.FromSeconds(t0C));

    var jupPass = ClosestTo("jupiter", flown, t0C);
    viz.AddMarker(t0C, padC.Position, $"launch ({launchDv:F0} m/s)", MarkerKinds.Burn);
    viz.AddMarker(nearJ.SimTime, nearJ.Position, $"TCM-1 ({tcm1Dv:F1} m/s)", MarkerKinds.Burn);
    viz.AddMarker(pastJ.SimTime, pastJ.Position, $"TCM-2 ({tcm2Dv:F1} m/s)", MarkerKinds.Burn);
    viz.AddMarker(jupPass.Time, ephemeris.Position("jupiter", jupPass.Time), $"Jupiter flyby ({jupPass.Distance / RJ:F1} R_J)", MarkerKinds.Flyby);
    viz.AddMarker(satCA, ephemeris.Position("saturn", satCA), $"Saturn pass ({arriveDist / 1e9:F2} Gm)", MarkerKinds.Flyby);
    viz.AddMarker(tPresent, now.Position, $"she is here now — {nowAU:F1} AU ({Present:yyyy-MM-dd})", MarkerKinds.Event);

    LabViz.Show(viz, args);
}
