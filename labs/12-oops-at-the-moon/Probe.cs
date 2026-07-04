// Lab 12 — Oops at the Moon 🌙
//
// LABEL: the STORY is fiction (miners, a shorted capacitor) — nothing in scenarios/sol.json
// changes; no such incident is canon. The NUMERICS are entirely real: this probe un-rails Luna
// — the one place in this whole lab series a body leaves its closed-form circular orbit — and
// integrates it as a genuine free body through SpaceSails.Core's actual Simulator, subject to
// real Newtonian gravity from Sun + Earth. Every periapsis, escape speed, and day-count below is
// standard two-body/perturbed orbital mechanics (Curtis ch. 2-3), just aimed at a scenario this
// repo made up.
//
// The mechanism: the game's ONLY propulsion is a velocity-scaling pulse (ManeuverAction, +/-10%
// etc. — see ManeuverPlan.cs). This lesson's "capacitor" doesn't touch Earth's mass; it kicks
// Luna's Earth-relative velocity the same way the ship's own engine kicks its velocity — an
// accident inflicting the game's own mechanic on a moon instead of a ship. Scaling Luna's
// tangential orbital velocity is exactly a change in specific orbital energy, which is exactly
// what "how tightly bound to Earth" means in the two-body sense — the fictional "capacitor"
// framing and the real "binding changed" physics are the same number.
//
// IRONCLAD RULE: every number below came from running this probe.

using SpaceSails.Core;

const double Day = 86400;
const double SunMu = 1.32712440018e20;
const double EarthMu = 3.986004418e14;
const double EarthOrbitRadius = 1.496e11;
const double EarthPeriod = 3.1558149e7;
const double EarthPhase = 1.8;
const double EarthBodyRadius = 6.371e6;
const double LunaOrbitRadius = 3.844e8;
const double LunaPeriod = 2.3606e6;

// Grazing threshold: Earth's body radius plus a loose 100 km "starts hitting real atmosphere"
// margin -- not a precise reentry-interface number, just a documented, simple choice.
const double GrazeThreshold = EarthBodyRadius + 100_000;

var sun = new CelestialBody("sun", "Sun", null, SunMu, 6.9634e9, 0, 0, 0);
var earth = new CelestialBody("earth", "Earth", "sun", EarthMu, EarthBodyRadius, EarthOrbitRadius, EarthPeriod, EarthPhase);
var luna = new CelestialBody("luna", "Luna", "earth", 4.9048695e12, 1.7374e6, LunaOrbitRadius, LunaPeriod, 0.0, BodyKind.Moon);

// The rail ephemeris — exactly scenarios/sol.json's sun/earth/luna — used only to read off
// Luna's real rail state (position + velocity) at t0.
var railEphemeris = new CircularOrbitEphemeris([sun, earth, luna]);

// The free-body ephemeris: Sun + Earth only. Luna leaves this list's control the moment it's
// un-railed; from here on its position is Simulator.Step output, not CircularOrbitEphemeris.
var freeBodyEphemeris = new CircularOrbitEphemeris([sun, earth]);

const double H = 1.0;
Vector2d earthPos0 = railEphemeris.Position("earth", 0);
Vector2d earthVel0 = (railEphemeris.Position("earth", H) - railEphemeris.Position("earth", -H)) / (2 * H);
Vector2d lunaPos0 = railEphemeris.Position("luna", 0);
Vector2d lunaVel0 = (railEphemeris.Position("luna", H) - railEphemeris.Position("luna", -H)) / (2 * H);

Vector2d relPos0 = lunaPos0 - earthPos0;
Vector2d relVel0 = lunaVel0 - earthVel0;
double r0 = relPos0.Length;
double vCirc = relVel0.Length;
double vCircAnalytic = Math.Sqrt(EarthMu / r0);

Console.WriteLine("=== Setup: Luna's real rail state at t0, read straight from scenarios/sol.json's numbers ===");
Console.WriteLine($"Earth-Luna distance r0 = {r0:E6} m ({r0 / 1000:F0} km). Luna's Earth-relative orbital speed:");
Console.WriteLine($"  numeric (finite-difference off the rail): {vCirc:F3} m/s");
Console.WriteLine($"  analytic circular speed sqrt(mu_earth/r0): {vCircAnalytic:F3} m/s");
Console.WriteLine($"  agreement: {Math.Abs(vCirc - vCircAnalytic) / vCircAnalytic:E3} relative error (the rail really is circular).");
Console.WriteLine();

// ---- Analytic + numeric machinery -----------------------------------------------------------

(double energy, double a, double h, double e, double periapsis, double apoapsis, bool bound) OrbitElements(Vector2d rRel, Vector2d vRel)
{
    double r = rRel.Length;
    double v = vRel.Length;
    double energy = v * v / 2.0 - EarthMu / r;
    double hAngMom = rRel.X * vRel.Y - rRel.Y * vRel.X;
    bool bound = energy < 0;
    double a = bound ? -EarthMu / (2.0 * energy) : double.PositiveInfinity;
    double e = bound ? Math.Sqrt(Math.Max(0.0, 1.0 - (hAngMom * hAngMom) / (EarthMu * a))) : double.NaN;
    double periapsis = bound ? a * (1 - e) : double.NaN;
    double apoapsis = bound ? a * (1 + e) : double.NaN;
    return (energy, a, hAngMom, e, periapsis, apoapsis, bound);
}

(List<(double day, double periapsisKm)> periapsisEvents, double? firstGrazeDay, double finalDistanceKm) TrackFreeBody(
    ShipState start, double horizonDays, double dt)
{
    var sim = new Simulator(freeBodyEphemeris, dt);
    ShipState state = start;
    var events = new List<(double, double)>();
    double? firstGraze = null;

    double DistToEarth(ShipState s) => (s.Position - freeBodyEphemeris.Position("earth", s.SimTime)).Length;

    double d0 = DistToEarth(state);
    double d1 = d0;
    double horizonSeconds = horizonDays * Day;
    while (state.SimTime < horizonSeconds)
    {
        state = sim.Step(state);
        double d2 = DistToEarth(state);

        if (d1 < d0 && d1 <= d2)
        {
            events.Add(((state.SimTime - dt) / Day, d1 / 1000.0));
        }

        if (firstGraze is null && d2 <= GrazeThreshold)
        {
            firstGraze = state.SimTime / Day;
        }

        d0 = d1;
        d1 = d2;
    }

    return (events, firstGraze, d1 / 1000.0);
}

void PrintCase(string label, double kickFactor, double horizonDays, double dt)
{
    Vector2d relVelNew = relVel0 * kickFactor;
    Vector2d lunaVelNew = earthVel0 + relVelNew;
    var start = new ShipState(lunaPos0, lunaVelNew, 0);

    (double energy, double a, double h, double e, double periapsis, double apoapsis, bool bound) = OrbitElements(relPos0, relVelNew);

    Console.WriteLine($"--- {label} (velocity x {kickFactor:F3}) ---");
    if (bound)
    {
        double period = 2 * Math.PI * Math.Sqrt(a * a * a / EarthMu);
        Console.WriteLine($"  analytic: bound, a={a / 1000:F0} km, e={e:F4}, periapsis={periapsis / 1000:F0} km, " +
            $"apoapsis={apoapsis / 1000:F0} km, period={period / Day:F2} days (half-period to first periapsis = {period / 2 / Day:F2} days)");
    }
    else
    {
        double vInf = Math.Sqrt(2 * energy);
        Console.WriteLine($"  analytic: UNBOUND relative to Earth alone. v_infinity = {vInf:F1} m/s (departs Earth's local field).");
    }

    (List<(double day, double periapsisKm)> events, double? firstGraze, double finalDistKm) = TrackFreeBody(start, horizonDays, dt);

    Console.WriteLine($"  numeric ({horizonDays:F0}-day free-body integration, dt={dt:F0}s, Sun+Earth gravity):");
    if (events.Count > 0)
    {
        Console.WriteLine($"    perigee timeline (day, distance to Earth in km): " +
            string.Join(", ", events.Take(6).Select(ev => $"({ev.day:F1}d, {ev.periapsisKm:F0}km)")));
    }
    else
    {
        Console.WriteLine("    no perigee passage detected in this horizon (still receding at the end -- consistent with departing).");
    }

    Console.WriteLine(firstGraze is { } g
        ? $"    ATMOSPHERE-GRAZING at day {g:F2} (distance <= {GrazeThreshold / 1000:F0} km)."
        : $"    never crosses the {GrazeThreshold / 1000:F0} km grazing threshold in {horizonDays:F0} days. Final distance to Earth: {finalDistKm:F0} km.");
    Console.WriteLine();
}

// ---- Section: three severities, one mechanism ------------------------------------------------

Console.WriteLine("=== Three severities of the same accident: a velocity kick to Luna's orbital speed ===");
Console.WriteLine("(the game's own +/-N% pulse mechanic, ManeuverPlan.cs, applied by the story's accident");
Console.WriteLine(" instead of the ship's engine)");
Console.WriteLine();

PrintCase("Mild: miners careless, +15% speedup (prograde)", 1.15, 400, 300);
PrintCase("Severe: miners reckless, -85% slowdown (retrograde)", 0.15, 10, 60);
PrintCase("Panic: miners flee, +50% speedup (prograde, past escape)", 1.50, 120, 300);

// ---- Break-it: find the X that ends the Moon in 30 days --------------------------------------

Console.WriteLine("=== BREAK IT: scanning retrograde severity to find the X that ends the Moon in 30 days ===");
Console.WriteLine($"{"slowdown X",-14}{"periapsis (km)",-18}{"half-period (days)",-20}{"ends within 30 days?",-22}");
foreach (double x in new double[] { 0.05, 0.10, 0.20, 0.30, 0.40, 0.50, 0.60, 0.70, 0.80, 0.85, 0.90, 0.95 })
{
    Vector2d relVelNew = relVel0 * (1 - x);
    (double energy, double a, double h, double e, double periapsis, double apoapsis, bool bound) = OrbitElements(relPos0, relVelNew);
    if (!bound)
    {
        Console.WriteLine($"{x,-14:P0}{"n/a (unbound)",-18}{"n/a",-20}{"no",-22}");
        continue;
    }

    double period = 2 * Math.PI * Math.Sqrt(a * a * a / EarthMu);
    double halfPeriodDays = period / 2 / Day;
    bool endsWithin30 = periapsis <= GrazeThreshold && halfPeriodDays <= 30;
    Console.WriteLine($"{x,-14:P0}{periapsis / 1000,-18:F0}{halfPeriodDays,-20:F2}{(endsWithin30 ? "YES" : "no"),-22}");
}

Console.WriteLine();
Console.WriteLine("=== Flavor: the real anecdote, then the riff ===");
Console.WriteLine("Factual: Apollo 12's ascent stage impact made Luna's seismometer network ring for");
Console.WriteLine("nearly an hour -- real evidence the Moon has essentially no internal damping compared");
Console.WriteLine("to Earth's wet, faulted crust.");
Console.WriteLine("[RIFF, labeled: if THAT is how long a spent rocket stage rings the Moon like a bell,");
Console.WriteLine(" what does a mining rig's shorted capacitor sound like -- to instruments that, in this");
Console.WriteLine(" story, are still listening.]");
