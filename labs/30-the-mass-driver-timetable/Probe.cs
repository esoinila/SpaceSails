// Lab 30 — The mass-driver timetable
//
// Teaching voice: the owner's canon (docs/worldbuilding-notes.md §1) says Luna's factories lob
// standardized compute-core packages by MASS DRIVER into transfer orbits — the pod has zero
// maneuver budget, the driver gives it everything at launch, and then it is a rock on a conic. The
// Sol scenario's own description already says "Luna's mass drivers lobbing compute-core pods"; this
// lab is where that stops being flavor text. We model the launch honestly (a driver-imparted speed
// off the lunar surface, no propulsion after), sweep speed x direction to find the family of useful
// trajectories, build the repeating TIMETABLE off the analytic Kepler rail, and price what it costs
// a parked player ship to catch and match a pod IN FLIGHT with the same rendezvous kernel Lab 24
// flies (TransferMath.Lambert + OrbitRule.PulsesFor). The punchline: you catch the milk run in your
// own backyard, right after it's flung, before it builds transfer speed.
//
// Every number below is the SAME Core code the game spends with: MassDriverSchedule.LaunchState /
// PodRailState / Timetable, TransferMath.PropagateKepler / Lambert, OrbitRule.PulsesFor.
//
// IRONCLAD RULE: every number in labs/30-the-mass-driver-timetable/README.md came from running this
// probe. Change the code and the README goes stale — rerun and re-paste, never hand-edit a table.

using SpaceSails.Core;
using SpaceSails.LabViz;

const double Day = 86400.0;
const double AU = 1.495978707e11;
const double SunMu = 1.32712440018e20;
const double EarthMu = 3.986004418e14;

// Inner-system slice of sol.json (verbatim radii/periods/phases), plus Luna and the Mercury compute
// yards — every body the launch family and the intercept touch, priced in the game's heliocentric
// frame (the frame pulse pricing reads, so it is the one that matters).
(string Id, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase, BodyKind Kind)[] specs =
[
    ("sun", "", SunMu, 6.9634e8, 0, 0, 0, BodyKind.Planet),
    ("mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0, BodyKind.Planet),
    ("mercury-compute", "mercury", 0, 500, 2.84e6, 6405, 0.2, BodyKind.Station),
    ("venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9, BodyKind.Planet),
    ("earth", "sun", EarthMu, 6.371e6, 1.496e11, 3.1558149e7, 1.8, BodyKind.Planet),
    ("luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0, BodyKind.Moon),
    ("mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7, BodyKind.Planet),
];

var field = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Id, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase, s.Kind))]);
var sim = new Simulator(field, timeStepSeconds: 60);

CelestialBody luna = field.Bodies.First(b => b.Id == "luna");
double venusR = field.Bodies.First(b => b.Id == "venus").OrbitRadius;
double mercuryR = field.Bodies.First(b => b.Id == "mercury").OrbitRadius;
double earthR = field.Bodies.First(b => b.Id == "earth").OrbitRadius;
double marsR = field.Bodies.First(b => b.Id == "mars").OrbitRadius;

// The heliocentric conic a launch state rides (sun at the origin, so the launch position IS the
// sun-relative radius). Returns the orbit shape the timetable and the family are read off.
(double A, double E, double Perihelion, double Aphelion, bool Bound) Conic(ShipState s)
{
    double r = s.Position.Length;
    double v2 = s.Velocity.LengthSquared;
    double energy = v2 / 2 - SunMu / r;
    double h = s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X;
    double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (SunMu * SunMu)));
    double a = -SunMu / (2 * energy);
    bool bound = energy < 0;
    double peri = a * (1 - e);                       // valid for both ellipse and hyperbola (a<0,e>1)
    double apo = bound ? a * (1 + e) : double.PositiveInfinity;
    return (a, e, peri, apo, bound);
}

string Reach(double periAu, double apoAu, bool bound)
{
    if (!bound) return "escape (unbound)";
    if (periAu <= mercuryR / AU * 1.03) return "reaches Mercury";
    if (periAu <= venusR / AU * 1.03) return "reaches Venus";
    if (apoAu >= marsR / AU * 0.97) return "reaches Mars";
    return "inner cislunar band";
}

// ===================================================================================
// Section A — the launch budget: what the driver has to give
// ===================================================================================
Console.WriteLine("=== Section A: the launch budget ===");
double vEsc = MassDriverSchedule.SurfaceEscapeSpeed(luna);
Vector2d lunaVel0 = TransferMath.BodyVelocity(field, "luna", 0);
Vector2d earthVel0 = TransferMath.BodyVelocity(field, "earth", 0);
double vEarthEscAtLuna = Math.Sqrt(2 * EarthMu / luna.OrbitRadius);
Console.WriteLine($"Luna surface escape speed sqrt(2mu/R): {vEsc:F1} m/s ({vEsc / 1000:F3} km/s) — the driver floor.");
Console.WriteLine($"Luna heliocentric speed at t=0: {lunaVel0.Length / 1000:F2} km/s (Earth rides at {earthVel0.Length / 1000:F2} km/s).");
Console.WriteLine($"Earth-escape speed at Luna's distance sqrt(2mu_E/r): {vEarthEscAtLuna / 1000:F3} km/s — clear this,");
Console.WriteLine($"relative to Earth, and the pod leaves cislunar space for a heliocentric conic.");
Console.WriteLine();

// ===================================================================================
// Section B — the family: sweep launch speed x direction
// ===================================================================================
Console.WriteLine("=== Section B: the launch family (heliocentric conic per launch) ===");
Console.WriteLine("Fire from Luna at t=0. Azimuth is measured off Luna's heliocentric prograde: pi is dead");
Console.WriteLine("retrograde (bleed heliocentric speed -> dive toward the sun), 0 is prograde (add speed ->");
Console.WriteLine("climb outward). Perihelion/aphelion are the conic's reach.");
Console.WriteLine();
Console.WriteLine($"{"v_launch km/s",-14}{"azimuth",-10}{"peri AU",10}{"apo AU",10}{"e",8}{"  reach",-22}");
Console.WriteLine(new string('-', 74));

(string Label, double Az)[] azimuths = [("retro (pi)", Math.PI), ("3pi/4", 3 * Math.PI / 4), ("prograde (0)", 0.0)];
double[] speeds = [2.6e3, 3.2e3, 4.0e3, 5.0e3, 6.0e3, 7.0e3, 7.6e3];
double venusSpeed = double.NaN, mercurySpeed = double.NaN;
foreach ((string azLabel, double az) in azimuths)
{
    foreach (double v in speeds)
    {
        ShipState launch = MassDriverSchedule.LaunchState(field, "luna", v, az, 0);
        var c = Conic(launch);
        double periAu = c.Perihelion / AU, apoAu = c.Bound ? c.Aphelion / AU : double.PositiveInfinity;
        string reach = Reach(periAu, apoAu, c.Bound);
        Console.WriteLine($"{v / 1000,-14:F2}{azLabel,-10}{periAu,10:F3}{(c.Bound ? apoAu.ToString("F3") : "inf"),10}{c.E,8:F3}  {reach,-22}");

        // Track the retrograde driver speed that first reaches Venus / Mercury (the useful long shots).
        if (az == Math.PI)
        {
            if (double.IsNaN(venusSpeed) && periAu <= venusR / AU * 1.03) venusSpeed = v;
            if (double.IsNaN(mercurySpeed) && periAu <= mercuryR / AU * 1.03) mercurySpeed = v;
        }
    }

    Console.WriteLine();
}

Console.WriteLine($"Useful family (retrograde lobs): ~{venusSpeed / 1000:F1} km/s drops perihelion to Venus's lane;");
Console.WriteLine($"~{mercurySpeed / 1000:F1} km/s is the showpiece long shot that reaches the Mercury compute yards.");
Console.WriteLine($"Prograde lobs climb the other way (toward Mars); just above the {vEsc / 1000:F1} km/s floor the pod barely");
Console.WriteLine($"escapes and loiters in the inner cislunar band — the low-energy 'to Earth's neighbourhood' case.");
Console.WriteLine();

// ===================================================================================
// Section C — the timetable: a repeating Luna->Venus milk run on the rails
// ===================================================================================
Console.WriteLine("=== Section C: the timetable (Luna->Venus milk run, on the Kepler rail) ===");
var run = MassDriverSchedule.MassDriverRun.LunaMilkRun(destinationId: "venus")
    with { LaunchSpeed = 3.2e3 };
Console.WriteLine($"Run: fire {run.LaunchSpeed / 1000:F1} km/s retrograde every {run.CadenceSeconds / 3600:F0} h, each pod live for {run.LifespanSeconds / Day:F0} d.");
Console.WriteLine();

int cadenceCount = 8;
IReadOnlyList<MassDriverSchedule.LaunchEntry> timetable = MassDriverSchedule.Timetable(field, run, baseSimTime: 0, cadenceCount);

// A neighbourhood pass = the pod's rail crossing a target orbit radius. Scan each pod's conic for
// the first time |r_pod - r_target| is minimized after launch; print the pass time, radius and speed.
(double Time, double Radius, double Speed)? FirstReach(ShipState launch, double targetRadius, double horizonDays)
{
    double best = double.MaxValue; double bestT = 0, bestR = 0, bestS = 0;
    for (double t = launch.SimTime + Day; t <= launch.SimTime + horizonDays * Day; t += Day)
    {
        if (MassDriverSchedule.PodRailState(launch, t, SunMu) is not { } s) continue;
        double r = s.Position.Length;
        double d = Math.Abs(r - targetRadius);
        if (d < best) { best = d; bestT = t; bestR = r; bestS = s.Velocity.Length; }
    }

    return best < 0.05 * targetRadius ? (bestT, bestR, bestS) : null;
}

Console.WriteLine($"{"pod",-14}{"launch (d)",12}{"Venus pass (d)",16}{"pass r AU",12}{"pass v km/s",14}");
Console.WriteLine(new string('-', 68));
foreach (MassDriverSchedule.LaunchEntry e in timetable)
{
    string when = FirstReach(e.Launch, venusR, 300) is { } p
        ? $"{(p.Time - 0) / Day,16:F1}{p.Radius / AU,12:F3}{p.Speed / 1000,14:F2}"
        : $"{"(no Venus pass in 300 d)",42}";
    Console.WriteLine($"{Callsign(e.Index),-14}{e.LaunchTime / Day,12:F2}{when}");
}

Console.WriteLine();
Console.WriteLine($"The cadence is a bus schedule: a fresh pod every {run.CadenceSeconds / 3600:F0} h, each arriving Venus's lane about");
Console.WriteLine($"the same transfer time later. Half the board is already in flight at t=0, half still to fire.");
Console.WriteLine();

// ===================================================================================
// Section D — the intercept: catch a pod IN FLIGHT and MATCH it (the Lab 24 pricing)
// ===================================================================================
Console.WriteLine("=== Section D: the intercept — loiter-and-match vs chase-it-down ===");
Console.WriteLine("A pod off the driver is nearly co-orbital with Earth for the first hours, then it commits to a");
Console.WriteLine("heliocentric dive and speeds away. Two ways to take it, both priced with OrbitRule.PulsesFor:");
Console.WriteLine();

ShipState freshPod = timetable.First(e => Math.Abs(e.LaunchTime) < 1).Launch;
Vector2d earthPos0 = field.Position("earth", 0);
Vector2d playerVel0 = earthVel0;                          // parked co-orbital with Earth
double playerSpeed0 = playerVel0.Length;

// (1) LOITER-AND-MATCH: the pirate is already parked in a circular heliocentric orbit at the pod's
// current radius (loitering where the pod passes). The catch costs only the velocity MATCH —
// |V_pod - v_circular(r_pod)| — no transfer to fly. Sweep the pod's flight time to find the window
// where that match is cheapest (it is smallest right after launch and grows as the pod dives).
Console.WriteLine("(1) loiter-and-match (parked where the pod passes; pay only to match its velocity):");
Console.WriteLine($"{"flight age",-12}{"pod r AU",10}{"match m/s",12}{"pulses",8}");
Console.WriteLine(new string('-', 42));
(double AgeD, double Match, int Pulses)? cheapMatch = null;
foreach (double ageD in new[] { 0.02, 0.1, 0.25, 0.5, 1.0, 2.0, 4.0, 8.0, 16.0, 32.0 })
{
    if (MassDriverSchedule.PodRailState(freshPod, ageD * Day, SunMu) is not { } pod) continue;
    double r = pod.Position.Length;
    Vector2d tHat = new Vector2d(-pod.Position.Y, pod.Position.X) / r;      // CCW circular direction
    Vector2d vCirc = tHat * Math.Sqrt(SunMu / r);
    double match = (pod.Velocity - vCirc).Length;
    int pulses = OrbitRule.PulsesFor(match, Math.Sqrt(SunMu / r));
    Console.WriteLine($"{ageD,-12:F2}{r / AU,10:F3}{match,12:F1}{pulses,8}");
    if (cheapMatch is null || match < cheapMatch.Value.Match) cheapMatch = (ageD, match, pulses);
}

Console.WriteLine();

// (2) CHASE IT DOWN: not pre-positioned — parked at Earth, the player flies a Lambert arc to the
// pod's rail position at t0+TOF and matches on arrival (Lab 23/24's kernel: TransferMath.Lambert +
// two priced burns). The cheaper end is a long, gentle arc — but it is a real transfer either way.
Console.WriteLine("(2) chase it down (parked at Earth; Lambert to the pod + match on arrival):");
Console.WriteLine($"{"TOF (d)",-9}{"pod dist Gm",13}{"depart m/s",12}{"match m/s",11}{"total m/s",11}{"pulses",8}");
Console.WriteLine(new string('-', 64));
(double Tof, double Total, int Pulses, double Depart, double Match)? cheapChase = null;
for (double tofD = 4; tofD <= 60; tofD += 8)
{
    double tof = tofD * Day;
    if (MassDriverSchedule.PodRailState(freshPod, tof, SunMu) is not { } podAtArr) continue;
    if (TransferMath.Lambert(earthPos0, podAtArr.Position, tof, SunMu) is not { } arc) continue;

    double depart = (arc.V1 - playerVel0).Length;
    double match = (podAtArr.Velocity - arc.V2).Length;
    double total = depart + match;
    int pulses = OrbitRule.PulsesFor(depart, playerSpeed0) + OrbitRule.PulsesFor(match, podAtArr.Velocity.Length);
    double dist = (podAtArr.Position - earthPos0).Length;
    Console.WriteLine($"{tofD,-9:F0}{dist / 1e9,13:F1}{depart,12:F1}{match,11:F1}{total,11:F1}{pulses,8}");
    if (cheapChase is null || total < cheapChase.Value.Total) cheapChase = (tofD, total, pulses, depart, match);
}

Console.WriteLine();
if (cheapMatch is { } cm)
{
    Console.WriteLine($"Cheap window: loiter-and-match at {cm.AgeD:F2} d old costs {cm.Match:F0} m/s = {cm.Pulses} pulses — you take");
    Console.WriteLine($"the pod for the driver's own speed, without flying anywhere.");
}

if (cheapChase is { } cc)
{
    Console.WriteLine($"Chasing it down instead: cheapest arc is {cc.Total:F0} m/s = {cc.Pulses} pulses — a real transfer, dearer.");
}

Console.WriteLine();

// ===================================================================================
// Section E — the trade: catch in flight vs pick up at either end
// ===================================================================================
Console.WriteLine("=== Section E: the trade — catch in flight vs pick up at either end ===");

// Pick up at Luna: match the pod at launch (you are at Luna, moving with Luna; the pod leaves at the
// driver speed relative to Luna, so matching it costs exactly that speed — plus you had to get to Luna).
double atLunaMatch = run.LaunchSpeed;                                   // v relative to Luna, by construction
int atLunaPulses = OrbitRule.PulsesFor(atLunaMatch, lunaVel0.Length);

// Pick up at Venus (the far end): a textbook Hohmann Earth->Venus, both burns — the cost of chasing
// the cargo all the way to its delivery lane instead of catching it as it leaves home.
TransferMath.HohmannPlan hoh = TransferMath.Hohmann(earthR, venusR, SunMu);
int venusPulses = OrbitRule.PulsesFor(hoh.DepartDeltaV, playerSpeed0)
                  + OrbitRule.PulsesFor(hoh.ArriveDeltaV, Math.Sqrt(SunMu / venusR));

Console.WriteLine($"{"option",-38}{"total m/s",12}{"pulses",9}");
Console.WriteLine(new string('-', 59));
if (cheapMatch is { } m)
{
    Console.WriteLine($"{"catch in flight (loiter-and-match)",-38}{m.Match,12:F0}{m.Pulses,9}");
}

if (cheapChase is { } ch)
{
    Console.WriteLine($"{"catch in flight (chase from Earth)",-38}{ch.Total,12:F0}{ch.Pulses,9}");
}

Console.WriteLine($"{"pick up at Luna (match the driver)",-38}{atLunaMatch,12:F0}{atLunaPulses,9}");
Console.WriteLine($"{"pick up at Venus (Hohmann chase)",-38}{hoh.TotalDeltaV,12:F0}{venusPulses,9}");
Console.WriteLine();
Console.WriteLine("The pod is cheapest to catch in your own backyard, the moment it's flung and still slow relative");
Console.WriteLine("to Earth — before it has built transfer speed: loiter where the next pod appears and take it for");
Console.WriteLine("the driver's own speed, no trip required. Let it commit to the dive and you must fly a real arc to");
Console.WriteLine("it; chase it all the way to Venus and you pay the whole transfer yourself. The milk run rewards the");
Console.WriteLine("pirate who reads the timetable and is already parked where the next pod passes.");

// ===================================================================================
// --viz (optional): heliocentric inner-system scene — the launch fan off Luna and one pod's conic.
// Gated behind LabViz.Wants so no-flag stdout is byte-identical.
// ===================================================================================
if (LabViz.Wants(args))
{
    var viz = new VizScene("lab30-mass-driver-timetable", "Lab 30 — The mass-driver timetable",
        "Luna lobs compute-core pods: the launch fan and a Luna->Venus rail, heliocentric");
    viz.AddBodies(field.Bodies);

    // The launch fan: several azimuths at the 3.2 km/s milk-run speed, each conic sampled on the rail.
    foreach ((string azLabel, double az) in azimuths)
    {
        ShipState launch = MassDriverSchedule.LaunchState(field, "luna", 3.2e3, az, 0);
        var samples = new List<TrajectorySample>();
        for (double t = 0; t <= 260 * Day; t += Day)
        {
            if (MassDriverSchedule.PodRailState(launch, t, SunMu) is { } s)
            {
                samples.Add(new TrajectorySample(t, s.Position));
            }
        }

        viz.AddPath($"lob {azLabel}", samples, az == Math.PI ? VizColors.Trajectory : VizColors.Sweep,
            az == Math.PI ? "main" : "fan", az == Math.PI ? 1.8 : 1.0, az == Math.PI ? 1.0 : 0.5);
    }

    viz.AddMarker(0, field.Position("luna", 0), "Luna driver", MarkerKinds.Burn);
    LabViz.Show(viz, args);
}

static string Callsign(int i)
{
    string[] names = ["Milk Run", "Windfall", "Ripe Plum", "Fat Goose", "Easy Keeping", "Tin Kettle", "Slow Coach", "Ferryman's Due"];
    return names[i % names.Length];
}
