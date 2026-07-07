// Lab 16 — Going ashore
//
// Teaching voice: lesson 7 priced the bus stops — planetary Hill spheres, insertion pulses.
// But the places worth going ashore are mostly MOONS (the He3 is on moons; the Enceladus
// haven IS a moon), and a moon is a bus stop nested inside a bus stop: its Hill sphere is
// carved out of its parent's, the tide trying to strip your parking orbit is the PLANET's
// (with the sun's tide working on the whole arrangement from outside), and the game engine
// makes nothing simpler for you — a ship near Luna feels Luna, Earth, and the sun all at
// once, honestly summed. This lesson sizes the nested stops, stress-tests parking orbits
// around Luna both prograde and retrograde (the restricted three-body problem plays
// favorites), prices the arrival with the game's own OrbitRule meter, and then does the
// thing the series has never done: goes ASHORE — the de-orbit burn, the fall, and the
// touchdown speed a lander must kill, checked against the real integrator.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code,
// the printed numbers in labs/16-going-ashore/README.md go stale — rerun and re-paste,
// never hand-edit a table.

using SpaceSails.Core;

const double Day = 86400.0;
const double Year = 365.25 * Day;
const double SunMu = 1.32712440018e20;

// Real sol.json rows — sun, all eight planets, all six moons, REAL body radii this time:
// Section D needs actual ground to land on. (The integrator clamps gravity off inside a
// body — collision is a later milestone — so the probe watches for the surface itself.)
(string Id, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase)[] specs =
[
    ("sun", "", SunMu, 6.9634e8, 0, 0, 0),
    ("mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    ("venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    ("earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    ("mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", "sun", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", "sun", 3.7931187e16, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
    ("luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0),
    ("europa", "jupiter", 3.2038e12, 1.5608e6, 6.709e8, 3.068226e5, 0.5),
    ("ganymede", "jupiter", 9.8907e12, 2.6341e6, 1.0704e9, 6.181531e5, 1.5),
    ("callisto", "jupiter", 7.1808e12, 2.4103e6, 1.8827e9, 1.4419307e6, 3.0),
    ("titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0),
    ("enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0),
];

var bodies = specs.Select(s => new CelestialBody(
    s.Id, s.Id, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase)).ToArray();
var ephemeris = new CircularOrbitEphemeris(bodies);
var sim = new Simulator(ephemeris, timeStepSeconds: 60);

CelestialBody Body(string id) => bodies.First(b => b.Id == id);
double ParentMu(string id) => Body(specs.First(s => s.Id == id).Parent).Mu;
Vector2d BodyVelocity(string id, double t) =>
    (ephemeris.Position(id, t + 1.0) - ephemeris.Position(id, t - 1.0)) / 2.0;

string[] moons = ["luna", "europa", "ganymede", "callisto", "titan", "enceladus"];

// ===================================================================================
// Section A — bus stops inside bus stops: the nested ladder
// ===================================================================================
Console.WriteLine("=== Section A: the nested ladder (every moon, sized by the game's own OrbitRule) ===");
Console.WriteLine($"{"moon",-11}{"Hill (km)",11}{"in radii",10}{"% of parent Hill",18}{"v_circ @ .5 Hill",18}{"window shell (km)",19}");
foreach (string id in moons)
{
    CelestialBody moon = Body(id);
    CelestialBody parent = Body(specs.First(s => s.Id == id).Parent);
    double hill = OrbitRule.HillRadius(moon, parent.Mu);
    double parentHill = OrbitRule.HillRadius(parent, SunMu);
    double shell = hill - 2 * moon.BodyRadius; // OrbitRule.WindowOpen: 2 radii < d < Hill
    Console.WriteLine($"{id,-11}{hill / 1000,11:N0}{hill / moon.BodyRadius,10:F1}{100 * hill / parentHill,17:F2}%" +
        $"{OrbitRule.CircularSpeed(moon, hill * 0.5),16:F1} m/s{shell / 1000,17:N0}");
}

Console.WriteLine();
Console.WriteLine("Two readings. Enceladus's whole Hill sphere is ~3.8 of its own radii — the insertion window");
Console.WriteLine("(inside Hill, above 2 radii) is a shell a few hundred km thick around a 252 km snowball: the");
Console.WriteLine("haven that barely exists. And every moon's 5x-Hill capture range is smaller than the game's");
Console.WriteLine($"{OrbitRule.CaptureRangeFloorMeters / 1e9:F0},000,000 km floor (`CaptureRangeFloorMeters`) — that constant isn't cosmetic, it is what");
Console.WriteLine("makes moon stops FINDABLE at map zoom. The floor exists because of this table.");
Console.WriteLine();

// ===================================================================================
// Section B — parking at Luna: the doubly-nested tide, prograde vs retrograde
// ===================================================================================
Console.WriteLine("=== Section B: parking orbits at Luna, 6 years in the REAL field (sun + Earth + Luna...) ===");
CelestialBody lunaBody = Body("luna");
double lunaHill = OrbitRule.HillRadius(lunaBody, Body("earth").Mu);
Console.WriteLine($"Luna Hill radius = {lunaHill / 1000:N0} km. Lesson 7 found Mars's stability structure jagged;");
Console.WriteLine("the autopilot parks at 0.5 Hill (`AutopilotInsertHillFraction`). Does that hold at a MOON,");
Console.WriteLine("where the stripping tide is Earth's and the sun works the whole arrangement from outside?");
Console.WriteLine("'GONE' means the ship put 1.5 Hill radii between itself and Luna and stayed out. And the");
Console.WriteLine("table runs TWICE — once at the game's cruise step ceiling (3600 s), once at a fine 150 s —");
Console.WriteLine("because near a stability boundary, lesson 3's lie chooses sides.");

string RunLadder(double fraction, double sense, double maxDt)
{
    double r = fraction * lunaHill;
    Vector2d lunaPos = ephemeris.Position("luna", 0);
    Vector2d lunaVel = BodyVelocity("luna", 0);
    var state = new ShipState(
        lunaPos + new Vector2d(r, 0),
        lunaVel + new Vector2d(0, sense) * OrbitRule.CircularSpeed(lunaBody, r),
        0);

    const int Checkpoints = 300;
    double chunk = 6 * Year / Checkpoints;
    double maxRatio = fraction;
    for (int i = 0; i < Checkpoints; i++)
    {
        state = sim.RunAdaptive(state, chunk, maxTimeStep: maxDt);
        double ratio = (state.Position - ephemeris.Position("luna", state.SimTime)).Length / lunaHill;
        maxRatio = Math.Max(maxRatio, ratio);
        if (ratio > 1.5)
        {
            return $"GONE {state.SimTime / Year:F2} yr (max {maxRatio:F2})";
        }
    }

    return $"held 6 yr (max {maxRatio:F2})";
}

foreach (double maxDt in new[] { 3600.0, 150.0 })
{
    Console.WriteLine();
    Console.WriteLine($"--- max time step {maxDt:F0} s ---");
    Console.WriteLine($"{"fraction",9}  {"prograde",-28}  {"retrograde",-28}");
    foreach (double fraction in new[] { 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 })
    {
        Console.WriteLine($"{fraction,9:F1}  {RunLadder(fraction, +1, maxDt),-28}  {RunLadder(fraction, -1, maxDt),-28}");
    }
}

Console.WriteLine();
Console.WriteLine("Read the two tables against each other and be a little afraid. At the cruise ceiling the map");
Console.WriteLine("says prograde is the hardy sense (solid to 0.8) and retrograde fragile (dies from 0.6). At a");
Console.WriteLine("fine step the CLASSICAL picture emerges — prograde unravels past ~0.5 Hill, retrograde rides");
Console.WriteLine("to ~0.9 (the distant-retrograde island) — the coarse table isn't blurred, it is nearly");
Console.WriteLine("INVERTED. In the rotating frame a retrograde satellite meets the tidal bulge at a higher");
Console.WriteLine("beat frequency, so the same step ceiling under-resolves retrograde forcing first: truncation");
Console.WriteLine("error masquerading as dynamics, lesson 3's thesis biting lesson 7's method. Rows hugging the");
Console.WriteLine("boundary (prograde 0.8, retrograde 0.8) stay step-sensitive at ANY dt — that's lesson 9's");
Console.WriteLine("chaotic coastline, not sloppiness. The autopilot's 0.5 Hill parks on the last reliably-solid");
Console.WriteLine("prograde rung — the constant is right, with thinner margin than the game's own warp-speed");
Console.WriteLine("integration would have you believe.");
Console.WriteLine();

// ===================================================================================
// Section C — the arrival bill, read off the game's own meter
// ===================================================================================
Console.WriteLine("=== Section C: what walking up to Luna costs (OrbitRule's own arithmetic) ===");
Console.WriteLine("A ship coasting in from interplanetary space falls DOWN Earth's well before it ever");
Console.WriteLine("meets Luna. Speed at Luna's orbital radius for a given hyperbolic excess at Earth:");
Console.WriteLine($"{"v_inf at Earth",15}{"speed at Luna's r",19}{"rel speed vs Luna (band)",26}{"window can open?",18}");
double lunaOrbitSpeed = Math.Tau * 3.844e8 / 2.3606e6; // Luna's speed around Earth on her rails
foreach (double vInf in new[] { 500.0, 1500.0, 3000.0, 5000.0 })
{
    double atLuna = Math.Sqrt(vInf * vInf + 2 * Body("earth").Mu / 3.844e8);
    double lo = Math.Abs(atLuna - lunaOrbitSpeed), hi = atLuna + lunaOrbitSpeed;
    string verdict = lo < OrbitRule.MaxRelativeSpeed ? "yes, aim well" : "no - brake first";
    Console.WriteLine($"{vInf,12:F0}    {atLuna,15:F0}    {$"{lo:F0} .. {hi:F0}",24}  {verdict,16}");
}

Console.WriteLine($"(Luna's own orbital speed: {lunaOrbitSpeed:F0} m/s; the window needs rel speed < {OrbitRule.MaxRelativeSpeed:F0} m/s.)");
Console.WriteLine("Geometry decides: chase Luna along her orbit and the relative speed sits at the LOW end of");
Console.WriteLine("the band — a modest interplanetary arrival can capture at the Moon directly, no Earth-orbit");
Console.WriteLine("layover. Cross her path instead and the same v_inf slams the window shut.");
Console.WriteLine();
Console.WriteLine("And the insertion itself, at 0.5 Hill, priced in the game's assisted-burn pulses (1% of");
Console.WriteLine("your current speed each, `DeltaVPerPulseFraction` — note the unit is YOUR speed, so the");
Console.WriteLine("same physical burn costs different pulses on different headings):");
Console.WriteLine($"{"rel speed (m/s)",16}{"insertion dv (m/s)",20}{"pulses",8}");
{
    Vector2d lunaPos = ephemeris.Position("luna", 0);
    Vector2d lunaVel = BodyVelocity("luna", 0);
    var radial = new Vector2d(1, 0);
    foreach (double rel in new[] { 500.0, 2000.0, 4000.0 })
    {
        var ship = new ShipState(
            lunaPos + radial * (0.5 * lunaHill),
            lunaVel + new Vector2d(0, -rel), // falling past, window-legal
            0);
        double dv = OrbitRule.InsertionDeltaV(ship, lunaPos, lunaVel, lunaBody);
        int pulses = OrbitRule.PulseCost(ship, lunaPos, lunaVel, lunaBody);
        Console.WriteLine($"{rel,16:F0}{dv,20:F1}{pulses,8}");
    }
}

Console.WriteLine();

// ===================================================================================
// Section D — going ashore: de-orbit, the fall, and the touchdown bill
// ===================================================================================
Console.WriteLine("=== Section D: going ashore (and getting off again) ===");
Console.WriteLine("From a circular parking orbit, one retro burn drops your periapsis to the ground (Curtis");
Console.WriteLine("ch. 6 again — a Hohmann transfer whose lower stop is the SURFACE). The lander then has to");
Console.WriteLine("kill the touchdown speed; the game doesn't model landings (yet), so this is the mission");
Console.WriteLine("the deck crew would fly. Analytic, per moon:");
Console.WriteLine($"{"moon",-11}{"park at (km)",13}{"de-orbit dv",13}{"fall time",11}{"touchdown (m/s)",17}{"depart park (m/s)",19}");
foreach (string id in moons)
{
    CelestialBody moon = Body(id);
    double hill = OrbitRule.HillRadius(moon, ParentMu(id));
    double rPark = Math.Max(0.3 * hill, 3 * moon.BodyRadius);
    double rSurf = moon.BodyRadius;
    double a = (rPark + rSurf) / 2;
    double vCirc = OrbitRule.CircularSpeed(moon, rPark);
    double dvDeorbit = vCirc - Math.Sqrt(moon.Mu * (2 / rPark - 1 / a));
    double fallMinutes = Math.PI * Math.Sqrt(a * a * a / moon.Mu) / 60;
    double touchdown = Math.Sqrt(moon.Mu * (2 / rSurf - 1 / a));
    double dvDepart = (Math.Sqrt(2) - 1) * vCirc; // park -> escape, the ride home starts here
    Console.WriteLine($"{id,-11}{rPark / 1000,13:N0}{dvDeorbit,11:F1} m/s{fallMinutes,9:F0} min{touchdown,17:F1}{dvDepart,19:F1}");
}

Console.WriteLine();
Console.WriteLine("Now the honesty pass: fly Luna's row through the REAL integrator (sun + Earth + Luna all");
Console.WriteLine("pulling) and watch for the surface, because the two-body ellipse is a rails-grade idealization:");
{
    CelestialBody moon = lunaBody;
    double rPark = Math.Max(0.3 * lunaHill, 3 * moon.BodyRadius);
    Vector2d lunaPos = ephemeris.Position("luna", 0);
    Vector2d lunaVel = BodyVelocity("luna", 0);
    var radial = new Vector2d(1, 0);
    var tangent = new Vector2d(0, 1);
    double vCirc = OrbitRule.CircularSpeed(moon, rPark);
    double a = (rPark + moon.BodyRadius) / 2;
    double dvDeorbit = vCirc - Math.Sqrt(moon.Mu * (2 / rPark - 1 / a));
    var afterBurn = new ShipState(
        lunaPos + radial * rPark,
        lunaVel + tangent * (vCirc - dvDeorbit),
        0);

    IReadOnlyList<TrajectorySample> descent = sim.ProjectAdaptive(
        afterBurn, null, 4 * Math.PI * Math.Sqrt(a * a * a / moon.Mu),
        minTimeStep: 1, maxTimeStep: 10, maxSamples: 500_000);
    foreach (TrajectorySample s in descent)
    {
        double alt = (s.Position - ephemeris.Position("luna", s.SimTime)).Length;
        if (alt <= moon.BodyRadius)
        {
            // Speed at the crossing, from a fine re-run to just before this sample.
            ShipState atGround = sim.RunAdaptive(afterBurn, s.SimTime - afterBurn.SimTime, maxTimeStep: 10);
            double speed = (atGround.Velocity - BodyVelocity("luna", s.SimTime)).Length;
            Console.WriteLine($"flown: surface contact after {s.SimTime / 60:F0} min at {speed:F1} m/s relative to Luna");
            break;
        }
    }

    double fallMinutes = Math.PI * Math.Sqrt(a * a * a / moon.Mu) / 60;
    double touchdown = Math.Sqrt(moon.Mu * (2 / moon.BodyRadius - 1 / a));
    Console.WriteLine($"analytic said:        {fallMinutes:F0} min at {touchdown:F1} m/s");
}

Console.WriteLine();
Console.WriteLine("-> the two-body de-orbit survives contact with the real field at Luna: the fall is deep");
Console.WriteLine("   inside the Hill sphere, where Luna owns the dynamics and the tide is a spectator. Compare");
Console.WriteLine("   Enceladus's touchdown with Ganymede's: one is a hard bicycle crash, the other arrives");
Console.WriteLine("   like a train. The He3 is always on the moons with the cheap ground — the worldbuilding");
Console.WriteLine("   knew what it was doing.");
