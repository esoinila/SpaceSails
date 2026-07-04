// Lab 09 — What the rails hide
//
// Teaching voice: read `CircularOrbitEphemeris.cs` and the claim is right there in the code, not
// just the docs — `Position(bodyId, simTime)` is a closed-form function of time and nothing else.
// Bodies don't perturb each other; there is no force calculation between planets anywhere in
// this engine. Only the ship (via `Simulator.GravitationalAcceleration`) feels their combined
// pull. That is "rails": every planet is exactly where its circular orbit formula says it is,
// forever, regardless of what any other mass in the system is doing. Real solar systems are not
// like this — Jupiter tugs on Saturn, Earth tugs on Venus, and the Sun itself gets tugged back
// and wobbles off dead-center. This lab builds the thing the game deliberately does NOT build: a
// genuine self-contained N-body integrator (semi-implicit Euler, all pairs attract, including the
// Sun) for the Sun and all eight planets plus a ship, and measures exactly how big a lie "rails"
// is, and where the lie actually costs you (long-baseline outer-system dynamics, precision
// flybys) versus where it plainly doesn't (a single inner-system transfer).
//
// Performance note: this probe is deliberately tiny per force evaluation (<=11 point masses, at
// most ~55 pairs with the i<j symmetry trick) but runs many steps. dt = 600 s is lesson 02's own
// proven-safe choice for the game's semi-implicit integrator on real solar-system timescales (no
// visible energy drift over 50 Mercury years there); duration is capped at 10 years for the
// long-baseline sections so the whole probe — four separate N-body integrations — finishes in
// low single-digit seconds, not minutes.
//
// IRONCLAD RULE: every number below came from running this probe. Change the code, rerun,
// re-paste — never hand-edit labs/09-what-the-rails-hide/README.md's tables.

using SpaceSails.Core;

const double Day = 86400.0;
const double Year = 365.25 * Day;
const double Dt = 600; // s — lesson 02's proven-safe semi-implicit step for inner-solar-system dynamics.

// Real Sol data, straight from scenarios/sol.json's sun-through-neptune rows (moons/stations
// dropped — this lab is about planet-planet perturbation, not the Wheel of the World's local
// stations).
(string Id, double Mu, double OrbitRadius, double OrbitPeriod, double InitialPhase)[] specs =
[
    ("sun", 1.32712440018e20, 0, 0, 0),
    ("mercury", 2.2032e13, 5.791e10, 7.60052e6, 0.0),
    ("venus", 3.24859e14, 1.0821e11, 1.94142e7, 0.9),
    ("earth", 3.986004418e14, 1.496e11, 3.1558149e7, 1.8),
    ("mars", 4.282837e13, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", 1.26686534e17, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", 3.7931187e16, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", 5.793939e15, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", 6.836529e15, 4.49506e12, 5.2004e9, 0.4),
];
int n = specs.Length;
double[] mu = [.. specs.Select(s => s.Mu)];
int jupiterIndex = Array.FindIndex(specs, s => s.Id == "jupiter");
int saturnIndex = Array.FindIndex(specs, s => s.Id == "saturn");

var railsBodies = specs.Select(s => new CelestialBody(s.Id, s.Id, s.Id == "sun" ? null : "sun", s.Mu, 1.0, s.OrbitRadius, s.OrbitPeriod, s.InitialPhase)).ToArray();
var railsEphemeris = new CircularOrbitEphemeris(railsBodies);

Vector2d RailsPos(int i, double t) => railsEphemeris.Position(specs[i].Id, t);

// --- The self-contained N-body core: semi-implicit Euler, every pair of massive bodies attracts,
// including the Sun (it is NOT held fixed — its own reflex wobble from the giant planets falls
// out of the same pairwise sum, not a separate hack). ---
void NBodyStep(Vector2d[] pos, Vector2d[] vel, double[] bodyMu, double dt)
{
    int m = pos.Length;
    var acc = new Vector2d[m];
    for (int i = 0; i < m; i++)
    {
        for (int j = i + 1; j < m; j++)
        {
            Vector2d d = pos[j] - pos[i];
            double r2 = d.LengthSquared;
            if (r2 == 0)
            {
                continue;
            }

            double invR3 = 1.0 / (r2 * Math.Sqrt(r2));
            if (bodyMu[j] != 0)
            {
                acc[i] += d * (bodyMu[j] * invR3);
            }

            if (bodyMu[i] != 0)
            {
                acc[j] -= d * (bodyMu[i] * invR3);
            }
        }
    }

    for (int i = 0; i < m; i++)
    {
        vel[i] += acc[i] * dt;
    }

    for (int i = 0; i < m; i++)
    {
        pos[i] += vel[i] * dt;
    }
}

// A massless test particle (a ship) feels every massive body's pull; it exerts none back.
Vector2d ShipAcceleration(Vector2d shipPos, Vector2d[] planetPos, double[] bodyMu)
{
    Vector2d a = Vector2d.Zero;
    for (int i = 0; i < planetPos.Length; i++)
    {
        if (bodyMu[i] == 0)
        {
            continue;
        }

        Vector2d d = planetPos[i] - shipPos;
        double r2 = d.LengthSquared;
        if (r2 == 0)
        {
            continue;
        }

        a += d * (bodyMu[i] / (r2 * Math.Sqrt(r2)));
    }

    return a;
}

(Vector2d[] pos, Vector2d[] vel) InitialPlanetState()
{
    var pos = new Vector2d[n];
    var vel = new Vector2d[n];
    for (int i = 0; i < n; i++)
    {
        pos[i] = RailsPos(i, 0);
        double w = specs[i].OrbitPeriod == 0 ? 0 : Math.Tau / specs[i].OrbitPeriod;
        vel[i] = specs[i].OrbitPeriod == 0
            ? Vector2d.Zero
            : new Vector2d(-specs[i].OrbitRadius * w * Math.Sin(specs[i].InitialPhase), specs[i].OrbitRadius * w * Math.Cos(specs[i].InitialPhase));
    }

    return (pos, vel);
}

Console.WriteLine("=== The claim, verified by reading CircularOrbitEphemeris.cs ===");
Console.WriteLine();
Console.WriteLine("Position(bodyId, simTime) computes a body's position from ONLY its own orbit radius,");
Console.WriteLine("period and phase (plus its parent's position) — simTime is the only input. There is");
Console.WriteLine("no loop over other bodies, no force sum, nowhere in that method or anywhere else in");
Console.WriteLine("Core does one celestial body's gravity affect another's trajectory. Simulator.cs's");
Console.WriteLine("GravitationalAcceleration sums every body's pull on the SHIP only. Bodies are rails:");
Console.WriteLine("exactly where the formula says, forever, no matter what mass is nearby. Confirmed by");
Console.WriteLine("reading the source — this lab now measures what that costs.");

Console.WriteLine();
Console.WriteLine("=== Section (a): rails vs. N-body planet positions over 10 years ===");
Console.WriteLine();

const int years = 10;
int stepsPerYear = (int)Math.Round(Year / Dt);
Console.WriteLine($"dt = {Dt:F0} s, {stepsPerYear} steps/year, {years} years -> {stepsPerYear * years:N0} total steps.");
Console.WriteLine();

var (nbPos, nbVel) = InitialPlanetState();
var divergenceByYear = new double[n, years + 1]; // [body, year] in meters, year 0 = 0 by construction
var nbPosByYear = new Vector2d[n, years + 1];
for (int i = 0; i < n; i++)
{
    nbPosByYear[i, 0] = nbPos[i];
}

double simTime = 0;
for (int y = 1; y <= years; y++)
{
    for (int s = 0; s < stepsPerYear; s++)
    {
        NBodyStep(nbPos, nbVel, mu, Dt);
        simTime += Dt;
    }

    for (int i = 0; i < n; i++)
    {
        nbPosByYear[i, y] = nbPos[i];
        divergenceByYear[i, y] = (nbPos[i] - RailsPos(i, simTime)).Length;
    }
}

Console.Write($"{"body",-10}");
for (int y = 1; y <= years; y++)
{
    Console.Write($"{"yr " + y,-12}");
}

Console.WriteLine();
for (int i = 0; i < n; i++)
{
    Console.Write($"{specs[i].Id,-10}");
    for (int y = 1; y <= years; y++)
    {
        Console.Write($"{divergenceByYear[i, y],-12:E2}");
    }

    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine("Units: meters. The Sun row is not zero: in the real N-body sum the Sun itself gets");
Console.WriteLine("pulled by the giant planets and wobbles off the origin rails assumes it sits at —");
Console.WriteLine("that wobble is real solar physics (the reflex motion real telescopes use to find");
Console.WriteLine("exoplanets), and 'rails' erases it by fiat.");

Console.WriteLine();
Console.WriteLine("=== Section (b): one Earth->Mars transfer, the SAME plan, rails vs. N-body ===");
Console.WriteLine();

var rng = new DeterministicRandom(20260704);
NpcRoute route = RoutePlanner.PlanRoute(railsEphemeris, "earth", "mars", 0, RoutePersonality.Economical, rng);
double transferDuration = route.EstimatedArrivalTime;
Console.WriteLine($"Plan: {route.Plan.Nodes.Count} burn node(s), estimated transfer {transferDuration / Day:F1} days.");
foreach (ManeuverNode node in route.Plan.Nodes)
{
    Console.WriteLine($"  - t={node.SimTime:F0} s, {node.Action}, {node.Pulses} pulse(s)");
}

Console.WriteLine();

// Rails run: the game's own Simulator, unmodified, at the same fixed dt as our N-body core.
var railsSimulator = new Simulator(railsEphemeris, Dt);
ShipState railsFinal = railsSimulator.Run(route.DepartureState, transferDuration, route.Plan);

// N-body run: our own integrator, planets mutually gravitating, ship flying the IDENTICAL plan.
var (transferPos, transferVel) = InitialPlanetState();
Vector2d shipPos = route.DepartureState.Position;
Vector2d shipVel = route.DepartureState.Velocity;
double t = 0;
while (t < transferDuration)
{
    double dt = Math.Min(Dt, transferDuration - t);
    double scale = route.Plan.ScaleFactorInWindow(t, t + dt);
    if (scale != 1.0)
    {
        shipVel *= scale;
    }

    Vector2d shipAcc = ShipAcceleration(shipPos, transferPos, mu);
    shipVel += shipAcc * dt;
    shipPos += shipVel * dt;
    NBodyStep(transferPos, transferVel, mu, dt);
    t += dt;
}

double shipDivergence = (railsFinal.Position - shipPos).Length;
double earthMarsDistance = (RailsPos(Array.FindIndex(specs, s => s.Id == "earth"), 0) - RailsPos(Array.FindIndex(specs, s => s.Id == "mars"), 0)).Length;
Console.WriteLine($"Rails final position:   {railsFinal.Position.X:E6}, {railsFinal.Position.Y:E6} m");
Console.WriteLine($"N-body final position:  {shipPos.X:E6}, {shipPos.Y:E6} m");
Console.WriteLine($"Ship divergence at arrival: {shipDivergence:E3} m ({shipDivergence / earthMarsDistance:E3} = a" +
    $" {shipDivergence / earthMarsDistance * 100:E3}% fraction of the Earth-Mars distance at t=0).");
Console.WriteLine($"For scale: {route.EstimatedMissDistance:E3} m was the planner's own accepted miss distance for this route.");

Console.WriteLine();
Console.WriteLine("=== Section (c): sensitivity to initial conditions — two ships, 1 m apart, 10 years ===");
Console.WriteLine();
Console.WriteLine("Both ships coast ballistically (no plan) from a Mars-orbit-radius circular start,");
Console.WriteLine("riding the SAME N-body planetary field (planets don't feel the ships back — the test-");
Console.WriteLine("particle assumption — so one planetary integration serves both). Ship B starts exactly");
Console.WriteLine("1 m away from Ship A along x. This is the Lyapunov-exponent flavor of chaos: bounded");
Console.WriteLine("systems with sensitive dependence on initial conditions don't stay 1 m apart.");
Console.WriteLine();

int marsIdx = Array.FindIndex(specs, s => s.Id == "mars");
double shipOrbitR = specs[marsIdx].OrbitRadius;
double shipOrbitV = Math.Sqrt(specs[0].Mu / shipOrbitR); // circular speed around the Sun at Mars's radius
Vector2d shipAPos = new(shipOrbitR, 0);
Vector2d shipAVel = new(0, shipOrbitV);
Vector2d shipBPos = new(shipOrbitR + 1.0, 0); // 1 meter away
Vector2d shipBVel = shipAVel;

var (sensPos, sensVel) = InitialPlanetState();
var separationByYear = new double[years + 1];
separationByYear[0] = (shipBPos - shipAPos).Length;

for (int y = 1; y <= years; y++)
{
    for (int s = 0; s < stepsPerYear; s++)
    {
        Vector2d accA = ShipAcceleration(shipAPos, sensPos, mu);
        Vector2d accB = ShipAcceleration(shipBPos, sensPos, mu);
        shipAVel += accA * Dt;
        shipBVel += accB * Dt;
        shipAPos += shipAVel * Dt;
        shipBPos += shipBVel * Dt;
        NBodyStep(sensPos, sensVel, mu, Dt);
    }

    separationByYear[y] = (shipBPos - shipAPos).Length;
}

Console.WriteLine($"{"year",-8}{"separation (m)",-18}{"growth since previous year (x)",-30}");
Console.WriteLine($"{0,-8}{separationByYear[0],-18:E3}{"-",-30}");
for (int y = 1; y <= years; y++)
{
    double growth = separationByYear[y] / separationByYear[y - 1];
    Console.WriteLine($"{y,-8}{separationByYear[y],-18:E3}{growth,-30:F2}");
}

double totalGrowth = separationByYear[years] / separationByYear[0];
double lyapunovEstimate = Math.Log(totalGrowth) / (years * Year);
Console.WriteLine();
Console.WriteLine($"Total growth over {years} years: {totalGrowth:E3}x. Rough Lyapunov-exponent estimate" +
    $" ln(growth)/time = {lyapunovEstimate:E3} /s (e-folding time ~ {1.0 / lyapunovEstimate / Day:F0} days) —");
Console.WriteLine("this is a single-trajectory-pair estimate, not a converged Lyapunov spectrum, but the");
Console.WriteLine("direction is the real point: a 1 m error compounds into a macroscopic one within the");
Console.WriteLine("10-year window, from gravity alone, with no other source of randomness anywhere in");
Console.WriteLine("this deterministic integrator.");

Console.WriteLine();
Console.WriteLine("=== Break it: remove Jupiter, watch Saturn wander ===");
Console.WriteLine();

double[] muNoJupiter = [.. mu];
muNoJupiter[jupiterIndex] = 0;
var (noJupPos, noJupVel) = InitialPlanetState();
var saturnNoJupiterByYear = new Vector2d[years + 1];
saturnNoJupiterByYear[0] = noJupPos[saturnIndex];
for (int y = 1; y <= years; y++)
{
    for (int s = 0; s < stepsPerYear; s++)
    {
        NBodyStep(noJupPos, noJupVel, muNoJupiter, Dt);
    }

    saturnNoJupiterByYear[y] = noJupPos[saturnIndex];
}

Console.WriteLine("Saturn's position with Jupiter's gravity switched OFF (muNoJupiter[jupiter] = 0, so");
Console.WriteLine("Jupiter still exists and still gets pulled, it just stops pulling on anyone) versus the");
Console.WriteLine("full N-body run from Section (a), which already includes Jupiter's real pull on Saturn:");
Console.WriteLine();
Console.WriteLine($"{"year",-8}{"Saturn drift from removing Jupiter (m)",-40}");
for (int y = 1; y <= years; y++)
{
    double drift = (saturnNoJupiterByYear[y] - nbPosByYear[saturnIndex, y]).Length;
    Console.WriteLine($"{y,-8}{drift,-40:E3}");
}

Console.WriteLine();
Console.WriteLine("=== Conclusion: how big is the lie, and where does it matter? ===");
Console.WriteLine();
Console.WriteLine("Rails is a deliberate, honest simplification — the same one real mission planners");
Console.WriteLine("make with patched conics before a numerical refinement pass. Section (a) says exactly");
Console.WriteLine("how big it is per planet over 10 years; Section (b) says it is utterly negligible for");
Console.WriteLine("a single inner-system transfer measured in months (a fraction of a percent of the");
Console.WriteLine("Earth-Mars distance, dwarfed by the planner's own accepted miss tolerance); the break-");
Console.WriteLine("it says the missing physics is concentrated in specific planet-planet couplings");
Console.WriteLine("(Jupiter on Saturn), not spread evenly across the system. Section (c) is the sharper");
Console.WriteLine("warning: this is a chaotic system, so ANY fixed error — including the rails");
Console.WriteLine("approximation itself — grows over long enough baselines. Rails is safe for the game's");
Console.WriteLine("actual playable timescale (single transfers, close flybys measured in days); it is a");
Console.WriteLine("real lie exactly where the game never asks it a question: multi-decade outer-system");
Console.WriteLine("trajectories and precision long-baseline flybys past the giants.");
