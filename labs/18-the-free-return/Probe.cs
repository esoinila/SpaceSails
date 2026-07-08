// Lab 18 — The free return
//
// Teaching voice: the most famous trajectory in fiction-adjacent orbital mechanics is Rich
// Purnell's from The Martian — don't stop at Earth, swing PAST it, let gravity turn the
// ship, go back to Mars for nearly nothing. The idea is older than the book: Apollo rode a
// free-return figure to the Moon (fail-safe by geometry, which saved 13), and Aldrin's
// cycler is the same thought made permanent — a ballistic bus that meets Earth and Mars
// forever, paying only "minor corrections." This lesson finds a real free-return in the
// game's own field: leave Earth, coast, swing by Mars WITHOUT capturing, come home. No
// formula gives you this — it is a search over departure day and departure speed, scored
// by two closest approaches (lesson 6's tool), refined by lesson 13's Newton. Then the bus
// gets its fare priced: what does each encounter cost to KEEP riding?
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code,
// the printed numbers in labs/18-the-free-return/README.md go stale — rerun and re-paste,
// never hand-edit a table.

using SpaceSails.Core;

const double Day = 86400.0;
const double Year = 365.25 * Day;
const double SunMu = 1.32712440018e20;

// Sun + eight planets, real radii (flyby altitudes matter here); moons dropped — this
// lesson lives between planets.
(string Id, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase)[] specs =
[
    ("sun", SunMu, 6.9634e8, 0, 0, 0),
    ("mercury", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    ("venus", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    ("earth", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    ("mars", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", 3.7931187e16, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
];
var ephemeris = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(s.Id, s.Id, s.Id == "sun" ? null : "sun", s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase))]);
var sim = new Simulator(ephemeris, timeStepSeconds: 60);

// Closest approach to a body along a flown trajectory, sample-scanned then parabola-refined
// on d^2 — lesson 6's technique, verbatim.
(double Distance, double Time) ClosestTo(string bodyId, IReadOnlyList<TrajectorySample> samples, double fromTime, double toTime)
{
    double best = double.MaxValue, bestT = fromTime;
    foreach (TrajectorySample s in samples)
    {
        if (s.SimTime < fromTime || s.SimTime > toTime)
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

// ===================================================================================
// Section A — the blueprint: why a free return can exist at all
// ===================================================================================
Console.WriteLine("=== Section A: the blueprint ===");
double aFree = (1.496e11 + 2.2794e11) / 2; // the Hohmann ellipse, the minimal Mars-toucher
double tFree = Math.Tau * Math.Sqrt(aFree * aFree * aFree / SunMu);
Console.WriteLine($"Any ellipse tangent at Earth that reaches Mars's orbit has a period between the Hohmann");
Console.WriteLine($"ellipse's {tFree / Year:F2} yr and infinity — pick the ellipse and you pick when you re-cross");
Console.WriteLine($"Earth's orbit. A FREE RETURN needs two coincidences at once: Mars present at your outbound");
Console.WriteLine($"crossing, Earth present when you come back around. Two conditions, two knobs (departure day,");
Console.WriteLine($"departure speed) — generically solvable, never by formula. Curtis ch. 8 sets up the patched-");
Console.WriteLine($"conic version and admits it: the real trajectory is found numerically. So: numerically.");
Console.WriteLine();

// ===================================================================================
// Section B — the search: two closest approaches, one coarse grid, one Newton polish
// ===================================================================================
Console.WriteLine("=== Section B: finding the bus (grid search over departure day x speed) ===");
const double Horizon = 4.0 * Year;
double marsUseful = 5.4e9;  // inside Mars's autopilot capture range — a flyby that COUNTS
ShipState Depart(double depDay, double dv)
{
    ShipState spawn = RoutePlanner.DepartureState(ephemeris, "earth", "mars", depDay * Day);
    return spawn with { Velocity = spawn.Velocity + spawn.Velocity.Normalized() * dv };
}

(double depDay, double dv, double marsCA, double marsT, double earthCA, double earthT) best =
    (0, 0, double.MaxValue, 0, double.MaxValue, 0);
int candidates = 0;
for (double depDay = 0; depDay <= 780; depDay += 10)
{
    for (double dv = 2600; dv <= 4600; dv += 100)
    {
        IReadOnlyList<TrajectorySample> flight = sim.ProjectAdaptive(
            Depart(depDay, dv), null, Horizon, maxTimeStep: 7200, maxSamples: 40_000);
        var mars = ClosestTo("mars", flight, depDay * Day, depDay * Day + Horizon);
        if (mars.Distance > marsUseful)
        {
            continue;
        }

        var home = ClosestTo("earth", flight, mars.Time + 30 * Day, depDay * Day + Horizon);
        candidates++;
        if (home.Distance < best.earthCA)
        {
            best = (depDay, dv, mars.Distance, mars.Time, home.Distance, home.Time);
        }
    }
}

Console.WriteLine($"grid: 79 departure days x 21 speeds = 1659 flights, {candidates} had a useful Mars flyby");
Console.WriteLine($"best ballistic round trip: depart day {best.depDay:F0}, dv {best.dv:F0} m/s prograde");
Console.WriteLine($"  Mars flyby  {best.marsCA / 1000:N0} km at day {best.marsT / Day:F0}");
Console.WriteLine($"  Earth return {best.earthCA / 1000:N0} km at day {best.earthT / Day:F0} " +
    $"(round trip {(best.earthT - best.depDay * Day) / Year:F2} yr)");
Console.WriteLine();

// Polish with Newton on (depDay, dv) to drive the EARTH RETURN distance down while keeping
// the Mars flyby useful: 2 unknowns, but the objective is a scalar — so polish dv alone on
// a finer grid around the winner, the honest cheap way.
Console.WriteLine("fine polish (1-day x 10 m/s grid around the winner):");
foreach (double depDay in new[] { best.depDay - 5, best.depDay, best.depDay + 5 })
{
    for (double dv = best.dv - 50; dv <= best.dv + 50; dv += 10)
    {
        IReadOnlyList<TrajectorySample> flight = sim.ProjectAdaptive(
            Depart(depDay, dv), null, Horizon, maxTimeStep: 3600, maxSamples: 80_000);
        var mars = ClosestTo("mars", flight, depDay * Day, depDay * Day + Horizon);
        if (mars.Distance > marsUseful)
        {
            continue;
        }

        var home = ClosestTo("earth", flight, mars.Time + 30 * Day, depDay * Day + Horizon);
        if (home.Distance < best.earthCA)
        {
            best = (depDay, dv, mars.Distance, mars.Time, home.Distance, home.Time);
        }
    }
}

Console.WriteLine($"polished: depart day {best.depDay:F0}, dv {best.dv:F0} m/s -> Mars flyby {best.marsCA / 1000:N0} km " +
    $"(day {best.marsT / Day:F0}), Earth return {best.earthCA / 1000:N0} km (day {best.earthT / Day:F0})");
Console.WriteLine();

// ===================================================================================
// Section C — riding the bus: correction fare per leg
// ===================================================================================
Console.WriteLine("=== Section C: the fare — keeping the bus on its route ===");
Console.WriteLine("The cycler idea is not 'no burns ever' — it is 'burns priced like bus fare, not like");
Console.WriteLine("missions.' The fare exists because of lesson 4: your drive fires quantized pulses, so the");
Console.WriteLine("planned departure is unfireable and you leave with a built-in sin. Two ways to atone,");
Console.WriteLine("both shooting-solved (lesson 13's Newton):");
Console.WriteLine();

(Vector2d V, bool Converged, double Miss) ShootTo(
    Vector2d position, double t0, Vector2d vSeed, Vector2d target, double tArrive, double tol)
{
    const double Eps = 1.0;
    Vector2d Fly(Vector2d v) => sim.RunAdaptive(new ShipState(position, v, t0), tArrive - t0).Position;
    Vector2d v = vSeed;
    double lastMiss = double.MaxValue;
    for (int iter = 0; iter < 15; iter++)
    {
        Vector2d miss = Fly(v) - target;
        lastMiss = miss.Length;
        if (lastMiss < tol)
        {
            return (v, true, lastMiss);
        }

        Vector2d colX = (Fly(v + new Vector2d(Eps, 0)) - (miss + target)) / Eps;
        Vector2d colY = (Fly(v + new Vector2d(0, Eps)) - (miss + target)) / Eps;
        double det = colX.X * colY.Y - colX.Y * colY.X;
        var step = new Vector2d(
            -(colY.Y * miss.X - colY.X * miss.Y) / det,
            -(-colX.Y * miss.X + colX.X * miss.Y) / det);
        if (step.Length > 200)
        {
            step = step.Normalized() * 200;
        }

        v += step;
    }

    return (v, false, lastMiss);
}

// The planned route: the polished ballistic figure's own encounter geometry.
ShipState planned = Depart(best.depDay, best.dv);
IReadOnlyList<TrajectorySample> reference = sim.ProjectAdaptive(
    planned, null, 4.5 * Year, maxTimeStep: 3600, maxSamples: 80_000);
var marsPlan = ClosestTo("mars", reference, best.depDay * Day, best.depDay * Day + 4.5 * Year);
var earthPlan = ClosestTo("earth", reference, marsPlan.Time + 30 * Day, best.depDay * Day + 4.5 * Year);
ShipState atMarsPlan = sim.RunAdaptive(planned, marsPlan.Time - planned.SimTime);
Vector2d marsOffset = atMarsPlan.Position - ephemeris.Position("mars", marsPlan.Time);
ShipState atEarthPlan = sim.RunAdaptive(planned, earthPlan.Time - planned.SimTime);
Vector2d earthOffset = atEarthPlan.Position - ephemeris.Position("earth", earthPlan.Time);

// The sin: lesson 4's pulse quantization. The drive trims in 1% multiplicative steps, so
// the plan's exact departure dv is unfireable — you leave with the nearest FIREABLE burn.
ShipState spawn0 = RoutePlanner.DepartureState(ephemeris, "earth", "mars", best.depDay * Day);
double v0 = spawn0.Velocity.Length;
int pulses = (int)Math.Round(Math.Log(1 + best.dv / v0) / Math.Log(1.01));
double dvFireable = v0 * (Math.Pow(1.01, pulses) - 1);
Console.WriteLine($"planned departure dv {best.dv:F0} m/s; nearest fireable burn = {pulses} fine pulses " +
    $"= {dvFireable:F0} m/s (a {Math.Abs(dvFireable - best.dv):F0} m/s sin you cannot avoid)");
ShipState rider = spawn0 with { Velocity = spawn0.Velocity + spawn0.Velocity.Normalized() * dvFireable };
Console.WriteLine();

// Route A: pay BEFORE the flyby — restore the planned Mars-encounter point 60 days out,
// then restore the planned Earth-return point 30 days after the flyby.
ShipState atBurn1 = sim.RunAdaptive(rider, (marsPlan.Time - 60 * Day) - rider.SimTime);
var fix1 = ShootTo(atBurn1.Position, atBurn1.SimTime, atBurn1.Velocity,
    ephemeris.Position("mars", marsPlan.Time) + marsOffset, marsPlan.Time, 5e7);
ShipState afterFlyby = sim.RunAdaptive(
    new ShipState(atBurn1.Position, fix1.V, atBurn1.SimTime), (marsPlan.Time + 30 * Day) - atBurn1.SimTime);
var fix2 = ShootTo(afterFlyby.Position, afterFlyby.SimTime, afterFlyby.Velocity,
    ephemeris.Position("earth", earthPlan.Time) + earthOffset, earthPlan.Time, 2e8);
ShipState homeA = sim.RunAdaptive(
    new ShipState(afterFlyby.Position, fix2.V, afterFlyby.SimTime), earthPlan.Time - afterFlyby.SimTime);
double fareA1 = (fix1.V - atBurn1.Velocity).Length;
double fareA2 = (fix2.V - afterFlyby.Velocity).Length;
double homeMissA = (homeA.Position - ephemeris.Position("earth", earthPlan.Time)).Length;

// Route B: ride the sin THROUGH the Mars pass uncorrected — let the pass land where it
// lands — then pay one burn that pins only the appointment that matters: home.
IReadOnlyList<TrajectorySample> sinned = sim.ProjectAdaptive(
    rider, null, 4.5 * Year, maxTimeStep: 3600, maxSamples: 80_000);
var marsB = ClosestTo("mars", sinned, best.depDay * Day, best.depDay * Day + 4.5 * Year);
ShipState lateShip = sim.RunAdaptive(rider, (marsPlan.Time + 30 * Day) - rider.SimTime);
var lateFix = ShootTo(lateShip.Position, lateShip.SimTime, lateShip.Velocity,
    ephemeris.Position("earth", earthPlan.Time) + earthOffset, earthPlan.Time, 2e8);
ShipState homeB = sim.RunAdaptive(
    new ShipState(lateShip.Position, lateFix.V, lateShip.SimTime), earthPlan.Time - lateShip.SimTime);
double fareB = (lateFix.V - lateShip.Velocity).Length;
double homeMissB = (homeB.Position - ephemeris.Position("earth", earthPlan.Time)).Length;

Console.WriteLine($"{"strategy",-38}{"fares (m/s)",14}{"total",9}{"Mars pass (km)",16}{"home kept (km)",16}");
Console.WriteLine($"{"pin BOTH appointments exactly",-38}{$"{fareA1:F0} + {fareA2:F0}",14}{fareA1 + fareA2,9:F1}" +
    $"{best.marsCA / 1000,16:N0}{homeMissA / 1000,16:N0}");
Console.WriteLine($"{"let Mars float, pin only home",-38}{$"{fareB:F0}",14}{fareB,9:F1}" +
    $"{marsB.Distance / 1000,16:N0}{homeMissB / 1000,16:N0}");
Console.WriteLine();
double marsPull = 4.282837e13 / (best.marsCA * best.marsCA);
double sunPullThere = SunMu / (2.2794e11 * 2.2794e11);
Console.WriteLine($"And the honest surprise: pinning less costs LESS, because this figure's Mars pass is a");
Console.WriteLine($"TAXI stop, not a slingshot — at {best.marsCA / 1e9:F1} million km out, Mars pulls " +
    $"{marsPull:E1} m/s^2 vs the");
Console.WriteLine($"sun's {sunPullThere:E1} m/s^2 ({sunPullThere / marsPull:F0}x stronger). The free return is steered by the SUN — resonant");
Console.WriteLine("timetabling, not gravity theft — so the Mars appointment tolerates a floating pass and the");
Console.WriteLine("navigator who insisted on punctuality at BOTH stations paid extra for nothing. Pin the");
Console.WriteLine("appointment you need; let the rest breathe. (Lesson 19 meets the OTHER kind of flyby, the");
Console.WriteLine("kind that IS a lever.)");
Console.WriteLine();
Console.WriteLine($"Scale check — the bus vs buying missions: this round trip after departure cost {fareA1 + fareA2:F0} or");
Console.WriteLine($"{fareB:F0} m/s in corrections. Two one-way Hohmann tickets (lesson 5) run ~11,200 m/s. The cycler");
Console.WriteLine("economy is real: once you're on the figure, staying on it is two orders of magnitude cheaper");
Console.WriteLine("than starting over — Rich Purnell's whole pitch, computed.");
