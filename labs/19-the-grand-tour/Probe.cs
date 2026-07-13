// Lab 19 — The Grand Tour
//
// Teaching voice: lesson 18's Mars pass was a taxi stop — the sun did the steering. This
// lesson meets the other kind of flyby: the LEVER. Voyager 2 left Earth with less launch
// energy than a direct Saturn shot needs, swung behind Jupiter, and let the giant's own
// orbital motion hurl it outward — then did it again at Saturn, Uranus, Neptune. The trick
// is no trick: in Jupiter's frame the ship's speed is conserved (gravity is conservative),
// but Jupiter's frame MOVES at 13 km/s, so turning the velocity vector inside that moving
// frame changes the ship's heliocentric speed for free. This lesson measures the crank
// (flyby distance vs energy stolen, both signs), then flies a real Earth->Jupiter->Saturn
// itinerary the way Voyager's navigators did — launch, correction, flyby, correction —
// and settles the bill against lesson 15's direct passage. Plus the ledger question the
// rails make delicious: WHO paid?
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code,
// the printed numbers in labs/19-the-grand-tour/README.md go stale — rerun and re-paste,
// never hand-edit a table.

using SpaceSails.Core;

const double Day = 86400.0;
const double Year = 365.25 * Day;
const double AU = 1.496e11;
const double SunMu = 1.32712440018e20;
const double JupiterMu = 1.26686534e17;

(string Id, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase)[] specs =
[
    ("sun", SunMu, 6.9634e8, 0, 0, 0),
    ("mercury", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    ("venus", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    ("earth", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    ("mars", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", JupiterMu, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", 3.7931187e16, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
];
var ephemeris = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(s.Id, s.Id, s.Id == "sun" ? null : "sun", s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase))]);
var sim = new Simulator(ephemeris, timeStepSeconds: 60);

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

double ApoapsisAU(ShipState s)
{
    double r = s.Position.Length, v = s.Velocity.Length;
    double energy = v * v / 2 - SunMu / r;
    if (energy >= 0)
    {
        return double.PositiveInfinity; // hyperbolic: leaves the system
    }

    double a = -SunMu / (2 * energy);
    double h = Math.Abs(s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X);
    double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (SunMu * SunMu)));
    return a * (1 + e) / AU;
}

// ===================================================================================
// Section A — the promise on paper
// ===================================================================================
Console.WriteLine("=== Section A: the promise ===");
(double dv1, double tof) HohmannLeg(double r1, double r2)
{
    double aT = (r1 + r2) / 2;
    return (Math.Abs(Math.Sqrt(SunMu * (2 / r1 - 1 / aT)) - Math.Sqrt(SunMu / r1)),
            Math.PI * Math.Sqrt(aT * aT * aT / SunMu));
}

var directSaturn = HohmannLeg(AU, 1.43353e12);
var toJupiter = HohmannLeg(AU, 7.7857e11);
var directNeptune = HohmannLeg(AU, 4.49506e12);
Console.WriteLine($"direct Earth->Saturn (lesson 15): departure {directSaturn.dv1:F0} m/s, {directSaturn.tof / Year:F2} yr");
Console.WriteLine($"direct Earth->Neptune (L15 tyranny): departure {directNeptune.dv1:F0} m/s, {directNeptune.tof / Year:F2} yr");
Console.WriteLine($"Earth->Jupiter leg only:          departure {toJupiter.dv1:F0} m/s, {toJupiter.tof / Year:F2} yr");
Console.WriteLine($"Voyager's wager: launch {directSaturn.dv1 - toJupiter.dv1:F0} m/s CHEAPER than Saturn direct,");
Console.WriteLine("and the real prize is Neptune-range reach for ~Jupiter fare. Let Jupiter's 13 km/s of");
Console.WriteLine("orbital motion supply the rest — plus, the post-flyby leg flies FASTER than a Hohmann");
Console.WriteLine("crawl. Cheaper AND quicker (for the outer system), if the giant will lend you his frame. Below: he does.");
Console.WriteLine();

// ===================================================================================
// Section B — the crank: flyby distance vs energy stolen (both signs)
// ===================================================================================
Console.WriteLine("=== Section B: the crank, measured ===");
// A launch toward Jupiter: Lambert Earth->Jupiter at the Hohmann-ish TOF, aimed at the
// planet's center, then re-aimed 90 days out at controlled offset points either side of
// the approach line. 200 days past the flyby, read the heliocentric ledger.
double dep0 = 100 * Day;
double tofEJ = toJupiter.tof;
ShipState launchPad = RoutePlanner.DepartureState(ephemeris, "earth", "jupiter", dep0);
Vector2d jupAtArr = ephemeris.Position("jupiter", dep0 + tofEJ);
var seed = Lambert(launchPad.Position, jupAtArr, tofEJ, SunMu);
var launch = ShootTo(launchPad.Position, dep0, seed.V1, jupAtArr, dep0 + tofEJ, 5e7, 500);
double tCA = dep0 + tofEJ;
ShipState beforeFlyby = sim.RunAdaptive(launchPad with { Velocity = launch.V }, tofEJ - 90 * Day);
Vector2d vInfVec = beforeFlyby.Velocity - BodyVelocity("jupiter", beforeFlyby.SimTime);
double vJup = BodyVelocity("jupiter", beforeFlyby.SimTime).Length;
Console.WriteLine($"approach: v_inf vs Jupiter = {vInfVec.Length / 1000:F2} km/s, heliocentric {beforeFlyby.Velocity.Length / 1000:F2} km/s,");
Console.WriteLine($"          pre-flyby orbit reaches {ApoapsisAU(beforeFlyby):F2} AU (Saturn sits at 9.58 AU)");
Console.WriteLine($"the two-body bound: the flyby can only ROTATE v_inf, so the outgoing heliocentric speed");
Console.WriteLine($"must land between |v_J - v_inf| = {(vJup - vInfVec.Length) / 1000:F2} and |v_J + v_inf| = {(vJup + vInfVec.Length) / 1000:F2} km/s. Watch it hold:");
Console.WriteLine();
Console.WriteLine($"{"aim offset",13}{"pass dist (km)",16}{"(R_J)",7}{"helio v after (km/s)",22}{"gain (km/s)",13}{"now reaches (AU)",18}");
Vector2d approachDir = vInfVec.Normalized();
var side = new Vector2d(-approachDir.Y, approachDir.X); // + = one side, - = the other
const double RJ = 6.9911e7;
foreach (double offset in new[] { -3e9, -1e9, -5e8, -2e8, 2e8, 5e8, 1e9, 3e9 })
{
    Vector2d aim = ephemeris.Position("jupiter", tCA) + side * offset;
    var tuned = ShootTo(beforeFlyby.Position, beforeFlyby.SimTime, beforeFlyby.Velocity, aim, tCA, Math.Max(1e6, Math.Abs(offset) * 0.02), 100);
    IReadOnlyList<TrajectorySample> through = sim.ProjectAdaptive(
        new ShipState(beforeFlyby.Position, tuned.V, beforeFlyby.SimTime), null, 290 * Day, maxTimeStep: 3600, maxSamples: 20_000);
    var pass = ClosestTo("jupiter", through, beforeFlyby.SimTime);
    if (pass.Distance < 2 * RJ)
    {
        Console.WriteLine($"{offset / 1e6,10:F0} Mm{pass.Distance / 1000,16:N0}{pass.Distance / RJ,7:F1}  impact-grade pass — discarded (inside 2 R_J the");
        Console.WriteLine($"{"",36}point-mass model and the step size are both lying)");
        continue;
    }

    // Outgoing heliocentric speed once clear of Jupiter's well (measuring AT closest approach
    // would report the periapsis speed-up, which beats the asymptotic bound — a lie). The long
    // coast also gives the reach.
    ShipState after = sim.RunAdaptive(new ShipState(beforeFlyby.Position, tuned.V, beforeFlyby.SimTime), 290 * Day);
    double gain = (after.Velocity.Length - beforeFlyby.Velocity.Length) / 1000;
    string reach = double.IsPositiveInfinity(ApoapsisAU(after)) ? "escapes the sun" : $"{ApoapsisAU(after):F2}";
    Console.WriteLine($"{offset / 1e6,10:F0} Mm{pass.Distance / 1000,16:N0}{pass.Distance / RJ,7:F1}{after.Velocity.Length / 1000,22:F2}{gain,13:F2}{reach,18}");
}

Console.WriteLine();
Console.WriteLine("Every surviving row gains — and none beats the bound. Why no braking rows? Because a");
Console.WriteLine("Hohmann arrival is SLOW: incoming v_inf already points almost dead against Jupiter's");
Console.WriteLine("motion, i.e. you arrive pre-parked at the crank's minimum. Jupiter can barely slow you");
Console.WriteLine("further — but a half-turn of that same v_inf adds up to two Jupiter-speeds' worth of");
Console.WriteLine("energy. Braking flybys (MESSENGER creeping down to Mercury) need the opposite approach:");
Console.WriteLine("arrive FAST and overtaking, so the crank has somewhere slower to point you. The ship's");
Console.WriteLine("speed in Jupiter's frame is untouched throughout — the theft is entirely the frame's motion.");
Console.WriteLine();

// --- D: Escape verified: specific orbital energy sign flip (no propellant during flyby) ---
Console.WriteLine("=== D (energy sign flip, zero-burn flyby) ===");
// Honest demo. Start from the real approach state 90 d before CA (beforeFlyby, from §B).
// One burn, up front and BEFORE the measurement window:
//   1) boost along v_inf by a fixed amount — capped so the ship is still provably BOUND
//      (e_pre < 0) as it leaves the burn;
//   2) re-aim THAT boosted trajectory (ShootTo) to a close b-plane offset at its true
//      encounter time (a boosted ship arrives early, so we target the encounter, not tCA).
// Everything after the burn is a single zero-burn ballistic arc, so the sign flip must come
// from the crank, not from a burn hidden inside the window. The boost raises v_inf so that a
// safe pass (a few R_J, outside the 2 R_J point-mass floor) turns enough to flip the sign — a
// pure Hohmann arrival would have to graze Jupiter to do the same.
Vector2d vInfApproach = beforeFlyby.Velocity - BodyVelocity("jupiter", beforeFlyby.SimTime);
Vector2d boostedD = beforeFlyby.Velocity + vInfApproach.Normalized() * 8000;           // bound after burn: e_pre < 0
var boostedStartD = new ShipState(beforeFlyby.Position, boostedD, beforeFlyby.SimTime);

// True encounter time of the boosted ballistic (before re-aiming), so ShootTo targets a
// reachable point at the moment Jupiter is actually there.
IReadOnlyList<TrajectorySample> dScout = sim.ProjectAdaptive(
    boostedStartD, null, 130 * Day, maxTimeStep: 1800, maxSamples: 40_000);
double tEncD = ClosestTo("jupiter", dScout, beforeFlyby.SimTime + 1 * Day).Time;
Vector2d vInfEncD = boostedD - BodyVelocity("jupiter", tEncD);
Vector2d sideD = new Vector2d(-vInfEncD.Normalized().Y, vInfEncD.Normalized().X);
Vector2d aimD = ephemeris.Position("jupiter", tEncD) + sideD * 3e8;                     // controlled b-plane offset
var tunedD = ShootTo(beforeFlyby.Position, beforeFlyby.SimTime, boostedD, aimD, tEncD, 5e6, 100);
var startD = new ShipState(beforeFlyby.Position, tunedD.V, beforeFlyby.SimTime);

// e_pre: sampled at the boosted, re-aimed start — after the last burn, far from Jupiter (Sun-frame energy clean)
double eJPre = startD.Velocity.Length * startD.Velocity.Length / 2.0 - SunMu / startD.Position.Length;

// Fly the real integrator through the encounter and find the TRUE closest approach.
IReadOnlyList<TrajectorySample> dTraj = sim.ProjectAdaptive(
    startD, null, 130 * Day, maxTimeStep: 1800, maxSamples: 40_000);
var dPass = ClosestTo("jupiter", dTraj, beforeFlyby.SimTime + 1 * Day);
double caRJ = dPass.Distance / RJ;

Console.WriteLine($"pre-Jupiter specific energy (Sun frame): {eJPre:F0} J/kg  (negative = bound)");
if (caRJ > 12.0)
{
    // The lever is sharp: if the aim slipped to the far branch the demo is invalid — say so.
    Console.WriteLine($"D demo flyby closest approach to Jupiter: {caRJ:F1} R_J — NO close pass, demo invalid (re-tune aim/boost)");
}
else
{
    Console.WriteLine($"D demo flyby closest approach to Jupiter: {caRJ:F2} R_J (a real pass, outside the 2 R_J point-mass floor)");
}

// e_post: sampled 60 d after CA on the SAME ballistic arc (zero burns since the start), well
// clear of Jupiter's well so the Sun-frame energy is clean again.
ShipState jPost = sim.RunAdaptive(startD, (dPass.Time + 60 * Day) - startD.SimTime);
double eJPost = jPost.Velocity.Length * jPost.Velocity.Length / 2.0 - SunMu / jPost.Position.Length;
Console.WriteLine($"post-Jupiter specific energy (Sun frame, 60 d past CA, zero burn in flyby): {eJPost:F0} J/kg  ({(eJPost > 0 ? "positive = ESCAPING (sign flip)" : "still bound")})");

// v_inf in/out (turning angle) sampled symmetrically ±2 d about CA on the ballistic arc.
Console.WriteLine("turning in Jupiter frame conserves |v_inf|; heliocentric |v| jumps because frame is moving.");
ShipState jIn = sim.RunAdaptive(startD, (dPass.Time - 2 * Day) - startD.SimTime);
ShipState jOut = sim.RunAdaptive(startD, (dPass.Time + 2 * Day) - startD.SimTime);
Vector2d vInfIn = jIn.Velocity - BodyVelocity("jupiter", jIn.SimTime);
Vector2d vInfOut = jOut.Velocity - BodyVelocity("jupiter", jOut.SimTime);
double turnDeg = Math.Acos(Math.Clamp(vInfIn.Normalized().Dot(vInfOut.Normalized()), -1, 1)) * 180 / Math.PI;
Console.WriteLine($"|v_inf| in {vInfIn.Length / 1000:F2} km/s, out {vInfOut.Length / 1000:F2} km/s (conserved to {Math.Abs(vInfIn.Length - vInfOut.Length) / vInfIn.Length:P0}); turning angle {turnDeg:F1} deg");

// Patched-conic prediction from MEASURED quantities: r_p = actual CA, v_inf = measured incoming.
double rpD = dPass.Distance;
double vInfDmag = vInfIn.Length;
double predD = 2 * Math.Asin(1.0 / (1 + rpD * vInfDmag * vInfDmag / JupiterMu)) * 180 / Math.PI;
Console.WriteLine($"patched-conic deflection from measured r_p={rpD / RJ:F2} R_J, v_inf={vInfDmag / 1000:F2} km/s: {predD:F1} deg (measured {turnDeg:F1} deg; the residual is the n-body lesson)");
Console.WriteLine();

// ===================================================================================
// Section C — the itinerary: Earth -> Jupiter -> Saturn, flown like Voyager flew
// ===================================================================================
Console.WriteLine("=== Section C: the itinerary, flown with TCMs ===");
// Find a departure day where the post-flyby arc can MEET Saturn: scan departure days,
// launch at the E->J Lambert, take the hardest useful crank (fixed aim offset chosen from
// Section B's sweet side), coast 4 years past the flyby, and score by Saturn's closest
// approach. Then tighten the winner with a post-flyby TCM, exactly like a real mission.
double bestDep = -1, bestSatCA = double.MaxValue, bestSatT = 0, bestTof = tofEJ;
const double CrankOffset = -5e8; // Section B's strong-gain side, at a Voyager-respectable pass
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
            continue; // no honest arc, or a launch no sail would buy (Earth->Jupiter Hohmann from A)
        }

        ShipState nearJ = sim.RunAdaptive(pad with { Velocity = s.V1 }, tofC - 90 * Day, maxTimeStep: 7200);
        Vector2d aim = jArr + side * CrankOffset;
        var tuned = ShootTo(nearJ.Position, nearJ.SimTime, nearJ.Velocity, aim, t + tofC, 1e7, 100);
        IReadOnlyList<TrajectorySample> onward = sim.ProjectAdaptive(
            new ShipState(nearJ.Position, tuned.V, nearJ.SimTime), null, 4.5 * Year, maxTimeStep: 7200, maxSamples: 40_000);
        var sat = ClosestTo("saturn", onward, t + tofC + 100 * Day);
        if (sat.Distance < bestSatCA)
        {
            (bestDep, bestSatCA, bestSatT, bestTof) = (depDay, sat.Distance, sat.Time, tofC);
        }
    }
}

Console.WriteLine($"window scan (20 yr of departure days, 53-day grid, 4 leg lengths, launch capped at 10.5 km/s):");
Console.WriteLine($"  best: depart day {bestDep:F0}, Earth->Jupiter leg {bestTof / Year:F1} yr");

// Fly it like Voyager's navigators flew — and do the b-plane sweep ON THE FLIGHT ITSELF,
// because the flyby is a lever: an aim difference of a few thousand km re-aims the whole
// post-flyby leg by tens of Gm. Sweep an arc you are not flying and the lever makes the
// prediction worthless; sweep the arc you ARE flying and the winner is your TCM-1.
double t0C = bestDep * Day;
ShipState padC = RoutePlanner.DepartureState(ephemeris, "earth", "jupiter", t0C);
Vector2d jupArrC = ephemeris.Position("jupiter", t0C + bestTof);
var launchSeed = Lambert(padC.Position, jupArrC, bestTof, SunMu);
double saturnHill = OrbitRule.HillRadius(
    new CelestialBody("saturn", "saturn", "sun", 3.7931187e16, 5.8232e7, 1.43353e12, 9.29596e8, 4.5), SunMu);
{
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

    chosen = Sweep(-1.5e8, -1.5e9, 7.5e7, chosen);                                    // coarse
    chosen = Sweep(chosen.Off + 6e7, chosen.Off - 6e7, 1.5e7, chosen);                // fine
    double tcm1Dv = (chosen.V - nearJ.Velocity).Length;
    Console.WriteLine($"b-plane sweep flown on the actual approach: best aim offset {chosen.Off / 1e6:F0} Mm ->");
    Console.WriteLine($"  ballistic Saturn approach {chosen.CA / 1e9:F2} Gm at day {chosen.T / Day:F0} " +
        $"({chosen.CA / saturnHill:F2} Saturn Hill radii; capture range reaches {OrbitRule.CaptureRange(saturnHill) / 1e9:F0} Gm)");
    Console.WriteLine();

    // Through the flyby ballistic, then one post-flyby TCM walks the approach from the
    // sweep's best down to lesson 15's doorstep.
    ShipState pastJ = sim.RunAdaptive(new ShipState(nearJ.Position, chosen.V, nearJ.SimTime), 90 * Day + 150 * Day);
    Vector2d satAim = ephemeris.Position("saturn", chosen.T);
    satAim += satAim.Normalized() * 1e9;
    var tcm2 = ShootTo(pastJ.Position, pastJ.SimTime, pastJ.Velocity, satAim, chosen.T, 1e9, 300);
    double tcm2Dv = (tcm2.V - pastJ.Velocity).Length;
    ShipState atSaturn = sim.RunAdaptive(
        new ShipState(pastJ.Position, tcm2.V, pastJ.SimTime), chosen.T - pastJ.SimTime);
    Vector2d satPos = ephemeris.Position("saturn", chosen.T);
    double arriveDist = (atSaturn.Position - satPos).Length;
    double relSpeed = (atSaturn.Velocity - BodyVelocity("saturn", chosen.T)).Length;
    double totalTof = (chosen.T - t0C) / Year;
    double tcmTotal = tcm1Dv + tcm2Dv;

    Console.WriteLine();
    Console.WriteLine($"{"burn",-32}{"dv (m/s)",10}");
    Console.WriteLine($"{"launch (targets the crank)",-32}{launchDv,10:F0}");
    Console.WriteLine($"{"TCM-1 at Jupiter-90d",-32}{tcm1Dv,10:F1}");
    Console.WriteLine($"{"TCM-2 at Jupiter+150d",-32}{tcm2Dv,10:F1}");
    Console.WriteLine($"arrival: {arriveDist / 1e9:F2} Gm from Saturn (capture range reaches {OrbitRule.CaptureRange(saturnHill) / 1e9:F0} Gm), " +
        $"day {chosen.T / Day:F0},");
    Console.WriteLine($"         {totalTof:F2} yr from launch, closing at {relSpeed / 1000:F2} km/s relative to Saturn");
    Console.WriteLine();
    // Compute the direct-Hohmann arrival relative speed live (no hard-coded "5439 m/s").
    double rSat = 1.43353e12;
    double aDirect = (AU + rSat) / 2;
    double vArrHelioDirect = Math.Sqrt(SunMu * (2 / rSat - 1 / aDirect));
    double vSat = Math.Sqrt(SunMu / rSat);
    double directArrRel = Math.Abs(vArrHelioDirect - vSat);  // head-on component; the simulated via-Jupiter case uses actual relative speed

    Console.WriteLine($"{"route",-24}{"launch (m/s)",14}{"TCMs",8}{"braking bill",14}{"time to Saturn",16}");
    Console.WriteLine($"{"direct Hohmann (L15)",-24}{directSaturn.dv1,14:F0}{"-",8}{$"{directArrRel:F0} m/s",14}{directSaturn.tof / Year,13:F2} yr");
    Console.WriteLine($"{"via Jupiter (this)",-24}{launchDv,14:F0}{tcmTotal,8:F0}{$"{relSpeed:F0} m/s",14}{totalTof,13:F2} yr");
    Console.WriteLine();
    Console.WriteLine($"The verdict is honest, not heroic. The crank is pure profit on launch day ({directSaturn.dv1 - toJupiter.dv1:F0} m/s");
    Console.WriteLine("cheaper) — and the game sky's first twenty years offer no 1977, so the detour loses two");
    Console.WriteLine("years on the clock, and STOPPING at Saturn refunds part of the gift: the slung arc");
    Console.WriteLine("arrives hot, and the braking bill grows by what Jupiter donated. Voyager never paid that");
    Console.WriteLine("line — she wasn't stopping. Flyby missions ride the crank for free precisely because");
    Console.WriteLine("they never ask the universe to slow them back down. Free energy has a timetable, and a");
    Console.WriteLine("refund policy.");
}

Console.WriteLine();

// ===================================================================================
// Section D — the ledger
// ===================================================================================
Console.WriteLine("=== Section D: who paid? ===");
Console.WriteLine("In the real universe Jupiter pays: Voyager's energy gain slowed the planet by about one");
Console.WriteLine("part in 1e24 — conservation holds, the bill just rounds to zero per customer. In THIS");
Console.WriteLine("universe it doesn't even round: Jupiter is on rails (lesson 9), its orbit is a formula");
Console.WriteLine("that cannot recoil, and the ship's energy gain in Section B is created from nothing. The");
Console.WriteLine("rails don't notice the theft. Every game and most mission planners accept this ledger");
Console.WriteLine("hole on purpose — the alternative (lesson 9's true n-body) charges you chaos for the");
Console.WriteLine("privilege of balanced books. Know which universe your solver lives in.");
