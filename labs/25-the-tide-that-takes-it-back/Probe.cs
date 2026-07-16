// Lab 25 — The tide that takes it back
//
// Teaching voice: the owner flew the first full mission, auto-parked at Enceladus, and the orbit
// quietly died — "it troubles me greatly that the ship is left to be kind of in orbit by luck
// there." The autopilot ACHIEVED an orbit; it did not KEEP one. This lab measures, honestly, the
// numbers that turn "achieved" into "kept":
//   A — WHERE a parked orbit around a small moon goes chaotic and HOW FAST it dies, swept across
//       radius × eccentricity in the live N-body sim. The headline: at a deep well like Enceladus
//       the parent's tide FORCES a large eccentricity even on a circle, and the ballistic park
//       crashes on its own within a day — the owner's stranded ship, reproduced.
//   B — what it COSTS to HOLD the park: periodic trim burns (re-circularize when the tide has
//       pumped the eccentricity past a tolerance) priced in pulses per day, versus losing the orbit.
//       A tolerance sweep picks the tolerance honestly: tight enough to keep periapsis clear of the
//       real surface at Enceladus, loose enough not to fight Luna/Titan's harmless oscillation.
//   C — the per-body keeping table Core consumes, and the tide-acceleration estimate that prices
//       every OTHER moon against the three measured here.
//
// Every number is the SAME Core code the autopilot spends with: OrbitRule (park band, insert, pulse
// pricing) and OrbitKeeping (trim tolerance, trim burn), flown through the real Simulator.
//
// IRONCLAD RULE: every number in labs/25-the-tide-that-takes-it-back/README.md came from running
// this probe. If you change the code, rerun and re-paste — never hand-edit a table.

using System.Globalization;
using SpaceSails.Core;

const double Day = 86400.0;
const double SunMu = 1.32712440018e20;
const double SaturnMu = 3.7931187e16;
const double EarthMu = 3.986004418e14;

// Lab 17's sol.json field, verbatim — every bill priced in the live game's heliocentric frame.
(string Id, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase)[] specs =
[
    ("sun", "", SunMu, 6.9634e8, 0, 0, 0),
    ("mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    ("venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    ("earth", "sun", EarthMu, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    ("mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", "sun", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
    ("luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0),
    ("titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0),
    ("enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0),
];

var field = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Id, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase,
        s.Parent is "" or "sun" ? BodyKind.Planet : BodyKind.Moon))]);

var sim = new Simulator(field, timeStepSeconds: 60);

Vector2d BodyVel(string id, double t) => (field.Position(id, t + 1.0) - field.Position(id, t - 1.0)) / 2.0;
CelestialBody Body(string id) => field.Bodies.First(b => b.Id == id);

// A parked-orbit start, body-relative, at APOAPSIS on +X: semi-major axis a, eccentricity e, so the
// outer reach is r_a = a(1+e) and periapsis a(1-e). Prograde. e=0 is a clean circle at r=a.
ShipState StartOrbit(string moonId, double a, double e, double t0)
{
    CelestialBody moon = Body(moonId);
    double ra = a * (1 + e);
    double va = Math.Sqrt(moon.Mu / a) * Math.Sqrt((1 - e) / (1 + e));
    return new ShipState(field.Position(moonId, t0) + new Vector2d(ra, 0), BodyVel(moonId, t0) + new Vector2d(0, va), t0);
}

// The measurement stride resolves the park orbit finely (period/64), so the osculating elements and
// the failure epochs are converged, not truncation artefacts (Lab 3 / Lab 16's warning).
double Stride(string moonId)
{
    CelestialBody moon = Body(moonId);
    double park = OrbitRule.ParkingRadius(moon, OrbitRule.HillRadius(moon, Body(moon.ParentId!).Mu));
    return OrbitRule.LocalOrbitPeriod(park, moon.Mu) / 64.0;
}

// ---- Section A: the drift sweep — where the band bites and how fast ------------------------------
// t_band  — first time the instantaneous two-body verdict leaves the tide-stable band.
// t_death — first REAL death: osculating periapsis below the physical surface (impact), or escape
//           past the Hill sphere. Reported in park-orbit periods. '—' = neither within the horizon.

(double bandP, double deathP, string end) SweepCell(string moonId, double a, double e, double periodsHorizon)
{
    CelestialBody moon = Body(moonId);
    CelestialBody parent = Body(moon.ParentId!);
    double hill = OrbitRule.HillRadius(moon, parent.Mu);
    double periodPark = OrbitRule.LocalOrbitPeriod(OrbitRule.ParkingRadius(moon, hill), moon.Mu);
    double stride = Stride(moonId);
    int strides = (int)(periodsHorizon * periodPark / stride);

    ShipState ship = StartOrbit(moonId, a, e, 0.0);
    double tBand = double.NaN, tDeath = double.NaN;
    string end = "survived";
    for (int i = 0; i < strides; i++)
    {
        double t = ship.SimTime;
        Vector2d mPos = field.Position(moonId, t);
        Vector2d mVel = BodyVel(moonId, t);
        OrbitRule.ParkStabilityVerdict verdict = OrbitRule.ParkStability(ship, mPos, mVel, moon, hill);
        OrbitKeeping.Elements el = OrbitKeeping.OrbitElements(ship, mPos, mVel, moon);
        double dist = (ship.Position - mPos).Length;

        if (double.IsNaN(tBand) && verdict != OrbitRule.ParkStabilityVerdict.Stable)
        {
            tBand = t;
        }
        bool impact = dist < moon.BodyRadius || (el.Bound && el.SemiMajorAxis * (1 - el.Eccentricity) < moon.BodyRadius);
        bool escape = dist >= hill;
        if (impact) { tDeath = t; end = "impact"; break; }
        if (escape) { tDeath = t; end = "escape"; break; }
        ship = sim.RunAdaptive(ship, stride, maxTimeStep: stride);
    }

    double P(double s) => double.IsNaN(s) ? double.NaN : s / periodPark;
    return (P(tBand), P(tDeath), end);
}

string F1(double p) => double.IsNaN(p) ? "—" : p.ToString("F1", CultureInfo.InvariantCulture);

void PrintSweep(string moonId)
{
    CelestialBody moon = Body(moonId);
    CelestialBody parent = Body(moon.ParentId!);
    double hill = OrbitRule.HillRadius(moon, parent.Mu);
    double park = OrbitRule.ParkingRadius(moon, hill);
    double periodPark = OrbitRule.LocalOrbitPeriod(park, moon.Mu);
    Console.WriteLine($"--- {moon.Id.ToUpperInvariant()} --- Hill {hill / 1e3:F0} km ({hill / moon.BodyRadius:F1} R), " +
        $"park {park / 1e3:F0} km ({park / hill:F2} Hill = {park / moon.BodyRadius:F2} R), " +
        $"band ceiling {OrbitRule.StableParkCeiling(moon, hill) / hill:F2} Hill, park period {periodPark / 3600:F1} h");
    Console.WriteLine($"  circular (e=0) parks swept OUTWARD by a/Hill:");
    Console.WriteLine($"    {"a/Hill",8}{"a (R)",9}{"t_leave_band",14}{"t_death",10}{"ending",10}   (orbits)");
    const double H = 40;
    foreach (double f in new[] { 0.20, 0.30, 0.33, 0.40, 0.45, 0.53, 0.65, 0.80 })
    {
        var (tb, td, end) = SweepCell(moonId, f * hill, 0.0, H);
        Console.WriteLine($"    {f,8:F2}{f * hill / moon.BodyRadius,9:F2}{F1(tb),14}{F1(td),10}{end,10}");
    }
    Console.WriteLine($"  eccentric parks at the 0.33-Hill radius, swept by e:");
    Console.WriteLine($"    {"e",8}{"apo/Hill",10}{"t_leave_band",14}{"t_death",10}{"ending",10}   (orbits)");
    foreach (double e in new[] { 0.00, 0.05, 0.10, 0.20, 0.30 })
    {
        double a = OrbitRule.ParkStableHillFraction * hill;
        var (tb, td, end) = SweepCell(moonId, a, e, H);
        Console.WriteLine($"    {e,8:F2}{a * (1 + e) / hill,10:F2}{F1(tb),14}{F1(td),10}{end,10}");
    }
    Console.WriteLine();
}

// ---- Section B: the cost of keeping — trims/day, Δv/day, pulses/day, safety ----------------------
// Park circular at 0.33 Hill and HOLD it: each stride, if the tide has pumped e past eccTol,
// re-circularize (OrbitKeeping.Trim), counting Δv and pulses at world speed. Report per-day cost, the
// closest the kept periapsis ever came to the surface (safety), and whether it held the whole horizon.

// checkEveryFrac = the trim cadence as a fraction of the park period. A trim is considered only at
// each cadence point; between them the bounded forced oscillation is allowed to reverse itself, so
// the ship pays only for the IRREVERSIBLE (secular) drift — the difference between fighting the tide
// and riding it. Integration always uses the fine measurement stride for accuracy.
(double trimsDay, double dvDay, double pulsesDay, double periMinR, bool held, double days) KeepCost(
    string moonId, double eccTol, double periodsHorizon, double checkEveryFrac)
{
    CelestialBody moon = Body(moonId);
    CelestialBody parent = Body(moon.ParentId!);
    double hill = OrbitRule.HillRadius(moon, parent.Mu);
    double park = OrbitRule.ParkingRadius(moon, hill);
    double periodPark = OrbitRule.LocalOrbitPeriod(park, moon.Mu);
    double stride = Stride(moonId);
    int strides = (int)(periodsHorizon * periodPark / stride);
    double nextCheck = checkEveryFrac * periodPark;

    ShipState ship = StartOrbit(moonId, park, 0.0, 0.0);
    int trims = 0, pulses = 0;
    double dv = 0, periMinR = double.MaxValue;
    bool held = true;
    for (int i = 0; i < strides; i++)
    {
        Vector2d mPos = field.Position(moonId, ship.SimTime);
        Vector2d mVel = BodyVel(moonId, ship.SimTime);
        OrbitKeeping.Elements el = OrbitKeeping.OrbitElements(ship, mPos, mVel, moon);
        if (el.Bound)
        {
            periMinR = Math.Min(periMinR, el.SemiMajorAxis * (1 - el.Eccentricity) / moon.BodyRadius);
        }
        if ((ship.Position - mPos).Length < moon.BodyRadius) { held = false; break; }
        if (ship.SimTime >= nextCheck)
        {
            nextCheck += checkEveryFrac * periodPark;
            if (OrbitKeeping.NeedsTrim(ship, mPos, mVel, moon, eccTol))
            {
                dv += OrbitKeeping.TrimDeltaV(ship, mPos, mVel, moon, park);
                pulses += OrbitKeeping.TrimPulseCost(ship, mPos, mVel, moon, park);
                ship = OrbitKeeping.Trim(ship, mPos, mVel, moon, park);
                trims++;
            }
        }
        ship = sim.RunAdaptive(ship, stride, maxTimeStep: stride);
    }
    double days = ship.SimTime / Day;
    return (trims / days, dv / days, pulses / days, periMinR, held, days);
}

// ---- run ----------------------------------------------------------------------------------------
Console.WriteLine("=== Section A: the drift sweep — where the tide-chaotic band bites, and how fast ===");
Console.WriteLine("Ballistic parks flown in the live N-body field. t_leave_band = OrbitRule.ParkStability");
Console.WriteLine("leaves Stable; t_death = REAL impact (periapsis under the surface) or escape past Hill.");
Console.WriteLine("'—' = neither within 40 orbits. Enceladus is the lesson: even a circle crashes on its own.");
Console.WriteLine();
PrintSweep("enceladus");
PrintSweep("luna");
PrintSweep("titan");

Console.WriteLine("=== Section B1: the trim CADENCE — riding the reversible oscillation vs fighting it ===");
Console.WriteLine("The tide FORCES a bounded eccentricity that reverses each orbit; correcting it every tick");
Console.WriteLine("pays for motion the tide would undo for free. Trimming only every f·period lets the");
Console.WriteLine("oscillation reverse, so the ship pays for the SECULAR drift alone. Enceladus, 40 orbits,");
Console.WriteLine($"eccTol {OrbitKeeping.TrimEccentricity:F2}:");
Console.WriteLine();
Console.WriteLine($"{"cadence f",11}{"trims/day",11}{"Δv/day m/s",12}{"pulses/day",12}{"peri min (R)",14}{"held?",8}");
foreach (double f in new[] { 0.02, 0.05, 0.10, 0.25, 0.50, 1.00 })
{
    var k = KeepCost("enceladus", OrbitKeeping.TrimEccentricity, 40, f);
    Console.WriteLine($"{f,11:F2}{k.trimsDay,11:F1}{k.dvDay,12:F2}{k.pulsesDay,12:F1}{k.periMinR,14:F3}{(k.held ? "yes" : "CRASH"),8}");
}
Console.WriteLine();
Console.WriteLine($"Chosen cadence: OrbitKeeping.TrimCadenceFraction = {OrbitKeeping.TrimCadenceFraction:F2} of the park period,");
Console.WriteLine($"tolerance OrbitKeeping.TrimEccentricity = {OrbitKeeping.TrimEccentricity:F2}.");
Console.WriteLine();

Console.WriteLine("=== Section B2: the keeping bill at the chosen cadence, per moon (60-orbit hold) ===");
Console.WriteLine($"{"moon",-12}{"days",8}{"trims/day",11}{"Δv/day m/s",12}{"pulses/day",12}{"peri min (R)",14}{"held?",8}");
var profiles = new List<OrbitKeeping.KeepProfile>();
foreach (string id in new[] { "enceladus", "luna", "titan" })
{
    var k = KeepCost(id, OrbitKeeping.TrimEccentricity, 60, OrbitKeeping.TrimCadenceFraction);
    Console.WriteLine($"{id,-12}{k.days,8:F1}{k.trimsDay,11:F2}{k.dvDay,12:F3}{k.pulsesDay,12:F2}{k.periMinR,14:F3}{(k.held ? "yes" : "CRASH"),8}");
    profiles.Add(new OrbitKeeping.KeepProfile(id, OrbitRule.ParkStableHillFraction, k.dvDay, k.trimsDay));
}
Console.WriteLine();

Console.WriteLine("=== Section C: the per-body keeping table (Core consumes this) + the physics estimate ===");
Console.WriteLine("Δv/day scales with the tide the parent raises across the park (a_tide = 2 μ_p r / D³).");
Console.WriteLine("Ktide = measured Δv/day ÷ (a_tide · 86400); its mean is the fallback constant for un-measured moons.");
Console.WriteLine();
Console.WriteLine($"{"moon",-12}{"a_tide m/s²",14}{"Δv/day",11}{"Ktide",10}{"world km/s",12}{"p/day@world",13}");
double ktideSum = 0; int n = 0;
foreach (OrbitKeeping.KeepProfile p in profiles)
{
    CelestialBody moon = Body(p.BodyId);
    CelestialBody parent = Body(moon.ParentId!);
    double hill = OrbitRule.HillRadius(moon, parent.Mu);
    double aTide = OrbitKeeping.TideAcceleration(OrbitRule.ParkingRadius(moon, hill), parent.Mu, moon.OrbitRadius);
    double ktide = p.TrimDvPerDay / (aTide * Day);
    ktideSum += ktide; n++;
    double world = BodyVel(p.BodyId, 0.0).Length;
    Console.WriteLine($"{p.BodyId,-12}{aTide,14:E3}{p.TrimDvPerDay,11:F3}{ktide,10:F3}{world / 1e3,12:F2}{OrbitKeeping.TrimPulsesPerDay(p, world),13}");
}
Console.WriteLine();
Console.WriteLine($"mean Ktide = {ktideSum / n:F3}");
Console.WriteLine();
Console.WriteLine("Generated KeepProfile rows for OrbitKeepingTable (bodyId, parkHillFraction, ΔvPerDay, trimsPerDay):");
foreach (OrbitKeeping.KeepProfile p in profiles)
{
    Console.WriteLine($"  [\"{p.BodyId}\"] = new(\"{p.BodyId}\", " +
        $"{p.ParkHillFraction.ToString("R", CultureInfo.InvariantCulture)}, " +
        $"{p.TrimDvPerDay.ToString("R", CultureInfo.InvariantCulture)}, " +
        $"{p.TrimsPerDay.ToString("R", CultureInfo.InvariantCulture)}),");
}
