// Lab 02 — The integrator zoo
//
// Teaching voice: there is more than one way to turn "acceleration right now" into "position
// a moment from now," and the choice matters over a long simulated run even though every
// method gets the *instant* acceleration exactly right (Newton's law of gravitation isn't in
// dispute — only how you march it forward in time). This probe implements three integrators
// from scratch — explicit (forward) Euler, RK4, and the game's own semi-implicit Euler (called
// through the real `SpaceSails.Core.Simulator`, not reimplemented) — and races them around
// Mercury's real orbit (mu and orbit radius straight from `scenarios/sol.json`) for one
// Mercury year, at three fixed dt values. The scoreboard is specific orbital energy
// (`epsilon = v^2/2 - mu/r`, conserved exactly by the real continuous two-body problem) —
// whatever an integrator does to that number over a year is entirely its own numerical error.
//
// IRONCLAD RULE: every number below came from running this probe. Change the code, rerun,
// re-paste — never hand-edit labs/02-the-integrator-zoo/README.md's tables.

using SpaceSails.Core;

const double SunMu = 1.32712440018e20;        // m^3/s^2 (scenarios/sol.json "sun")
const double SunBodyRadius = 6.9634e8;        // m
const double MercuryOrbitRadius = 5.791e10;   // m (scenarios/sol.json "mercury" orbitRadiusM)
const double MercuryPeriod = 7.60052e6;       // s (scenarios/sol.json "mercury" orbitPeriodS)

var sun = new CelestialBody("sun", "Sun", null, SunMu, SunBodyRadius, 0, 0, 0);
var ephemeris = new CircularOrbitEphemeris([sun]);

double CircularSpeed(double r) => Math.Sqrt(SunMu / r);
double SpecificEnergy(Vector2d r, Vector2d v) => v.LengthSquared / 2.0 - SunMu / r.Length;

Vector2d Acceleration(Vector2d r)
{
    double d2 = r.LengthSquared;
    double d = Math.Sqrt(d2);
    return r * (-SunMu / (d2 * d));
}

// Explicit (forward) Euler: both r and v advance using the OLD velocity/acceleration. This is
// the textbook-simplest integrator and the one every intro numerical-methods course starts
// with — and the one nobody should ship, as this probe demonstrates.
(Vector2d r, Vector2d v) ExplicitEulerStep(Vector2d r, Vector2d v, double dt)
{
    Vector2d a = Acceleration(r);
    return (r + v * dt, v + a * dt);
}

// Classical 4th-order Runge-Kutta on the state (r, v) with dr/dt = v, dv/dt = a(r). Four
// acceleration evaluations per step buys fourth-order-in-dt local accuracy — dramatically
// better per step than either Euler variant.
(Vector2d r, Vector2d v) Rk4Step(Vector2d r, Vector2d v, double dt)
{
    Vector2d k1r = v, k1v = Acceleration(r);
    Vector2d k2r = v + k1v * (dt / 2), k2v = Acceleration(r + k1r * (dt / 2));
    Vector2d k3r = v + k2v * (dt / 2), k3v = Acceleration(r + k2r * (dt / 2));
    Vector2d k4r = v + k3v * dt, k4v = Acceleration(r + k3r * dt);
    Vector2d rNew = r + (k1r + 2 * k2r + 2 * k3r + k4r) * (dt / 6.0);
    Vector2d vNew = v + (k1v + 2 * k2v + 2 * k3v + k4v) * (dt / 6.0);
    return (rNew, vNew);
}

// The game's own semi-implicit ("symplectic") Euler, called through the real Simulator — not
// reimplemented. Simulator.Step advances v with the OLD acceleration, then advances r with the
// NEW v (see Simulator.cs StepBy). That one-line reordering from explicit Euler is the whole
// difference this lesson is about.
double RunSemiImplicit(double dt, double durationSeconds, out double energy0, out double energyN)
{
    var simulator = new Simulator(ephemeris, dt);
    var start = new ShipState(new Vector2d(MercuryOrbitRadius, 0), new Vector2d(0, CircularSpeed(MercuryOrbitRadius)), 0);
    energy0 = SpecificEnergy(start.Position, start.Velocity);
    ShipState end = simulator.Run(start, durationSeconds);
    energyN = SpecificEnergy(end.Position, end.Velocity);
    return (energyN - energy0) / Math.Abs(energy0);
}

double RunHomebrew(Func<Vector2d, Vector2d, double, (Vector2d, Vector2d)> step, double dt, double durationSeconds,
    out double energy0, out double energyN)
{
    Vector2d r = new(MercuryOrbitRadius, 0);
    Vector2d v = new(0, CircularSpeed(MercuryOrbitRadius));
    energy0 = SpecificEnergy(r, v);

    int steps = (int)Math.Round(durationSeconds / dt);
    for (int i = 0; i < steps; i++)
    {
        (r, v) = step(r, v, dt);
    }

    energyN = SpecificEnergy(r, v);
    return (energyN - energy0) / Math.Abs(energy0);
}

Console.WriteLine("=== One Mercury year (7.60052e6 s), three integrators, three timesteps ===");
Console.WriteLine();
Console.WriteLine("Relative drift in specific orbital energy: (E_end - E_start) / |E_start|.");
Console.WriteLine("The continuous two-body problem conserves E exactly — every number below is");
Console.WriteLine("pure integrator error, nothing else.");
Console.WriteLine();

double[] dtValues = [3600, 600, 60];
Console.WriteLine($"{"dt (s)",-10}{"explicit Euler",-22}{"semi-implicit (game)",-24}{"RK4",-18}");
foreach (double dt in dtValues)
{
    double eulerDrift = RunHomebrew(ExplicitEulerStep, dt, MercuryPeriod, out _, out _);
    double semiDrift = RunSemiImplicit(dt, MercuryPeriod, out _, out _);
    double rk4Drift = RunHomebrew(Rk4Step, dt, MercuryPeriod, out _, out _);
    Console.WriteLine($"{dt,-10:F0}{eulerDrift,-22:E3}{semiDrift,-24:E3}{rk4Drift,-18:E3}");
}

Console.WriteLine();
Console.WriteLine("=== BREAK IT: the long haul — 50 Mercury years at dt = 600 s ===");
Console.WriteLine();
Console.WriteLine("A single year barely stresses any of these. Run 50 years (about 3800 real");
Console.WriteLine("days of Mercury's clock) at one fixed dt and watch what 'secular' means:");
Console.WriteLine("does the energy error grow steadily in one direction, or oscillate and stay");
Console.WriteLine("bounded?");
Console.WriteLine();

double longRun = 50 * MercuryPeriod;
double eulerLong = RunHomebrew(ExplicitEulerStep, 600, longRun, out double eu0, out double euN);
double semiLong = RunSemiImplicit(600, longRun, out double se0, out double seN);
double rk4Long = RunHomebrew(Rk4Step, 600, longRun, out double rk0, out double rkN);

Console.WriteLine($"{"integrator",-20}{"E_start (J/kg)",-20}{"E_end (J/kg)",-20}{"rel. drift",-14}");
Console.WriteLine($"{"explicit Euler",-20}{eu0,-20:E6}{euN,-20:E6}{eulerLong,-14:E3}");
Console.WriteLine($"{"semi-implicit",-20}{se0,-20:E6}{seN,-20:E6}{semiLong,-14:E3}");
Console.WriteLine($"{"RK4",-20}{rk0,-20:E6}{rkN,-20:E6}{rk4Long,-14:E3}");

Console.WriteLine();
Console.WriteLine("=== Section C: does RK4 ever drift? (it does — just very, very slowly) ===");
Console.WriteLine();
Console.WriteLine("RK4 is not symplectic: it has no proof of bounded energy error, only a small");
Console.WriteLine("LOCAL truncation order (dt^5 per step). Whether that adds up to something");
Console.WriteLine("secular is an empirical question at these orbital timescales. First: hold dt");
Console.WriteLine("fixed and stretch the duration — a real secular term grows with time, an");
Console.WriteLine("oscillating bounded error does not.");
Console.WriteLine();
Console.WriteLine($"{"years",-10}{"dt (s)",-10}{"RK4 drift",-14}");
foreach (double years in new double[] { 500, 2000, 8000 })
{
    double drift = RunHomebrew(Rk4Step, 3600, years * MercuryPeriod, out _, out _);
    Console.WriteLine($"{years,-10:F0}{3600,-10:F0}{drift,-14:E3}");
}

Console.WriteLine();
Console.WriteLine("Second: hold duration fixed at 50 years and coarsen dt — RK4 vs the game's");
Console.WriteLine("semi-implicit Euler, side by side:");
Console.WriteLine();
Console.WriteLine($"{"dt (s)",-10}{"RK4 drift (50 yr)",-22}{"semi-implicit drift (50 yr)",-28}");
foreach (double dt2 in new double[] { 3600, 7200, 14400, 28800 })
{
    double rk4d = RunHomebrew(Rk4Step, dt2, 50 * MercuryPeriod, out _, out _);
    double semid = RunSemiImplicit(dt2, 50 * MercuryPeriod, out _, out _);
    Console.WriteLine($"{dt2,-10:F0}{rk4d,-22:E3}{semid,-28:E3}");
}

Console.WriteLine();
Console.WriteLine("Third: at that same coarse dt = 28800 s, does semi-implicit's error grow");
Console.WriteLine("with duration the way RK4's did above, or does it just wobble in place?");
Console.WriteLine("This is the actual test of 'bounded' vs 'secular,' not the small-number");
Console.WriteLine("table above.");
Console.WriteLine();
Console.WriteLine($"{"years",-10}{"semi-implicit drift",-20}");
foreach (double years in new double[] { 50, 200, 800, 3200 })
{
    double drift = RunSemiImplicit(28800, years * MercuryPeriod, out _, out _);
    Console.WriteLine($"{years,-10:F0}{drift,-20:E3}");
}
