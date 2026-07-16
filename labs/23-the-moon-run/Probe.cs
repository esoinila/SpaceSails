// Lab 23 — The moon run
//
// Teaching voice: Wednesday night the armed autopilot approached Titan the brute way — point
// at the aim, re-SET the whole velocity vector to moonVelocity + 4 km/s toward target, and pay
// again every time Saturn's pull dragged the relative speed back over the cap. Each re-set
// throws away the velocity the well GAVE us and buys it back at full pulse price. This lesson
// reproduces that hemorrhage honestly (Section A), then teaches the fix the game now flies:
// inside a giant's Hill sphere, plan in the giant's frame. The window is a bus timetable
// (Section B); Lambert rides the well as a porkchop whose floor IS Hohmann (Section C); and the
// planner's cheap plan, flown end to end in the real N-body sim, settles the bill against
// Wednesday (Section D). Every number here is the SAME Core code the autopilot spends with:
// TransferMath, TransferPlanner, OrbitRule — Curtis chs. 2, 5, 6.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code, the
// printed numbers in labs/23-the-moon-run/README.md go stale — rerun and re-paste, never
// hand-edit a table.

using SpaceSails.Core;
using SpaceSails.LabViz;

const double Day = 86400.0;
const double SunMu = 1.32712440018e20;
const double SaturnMu = 3.7931187e16;

// Full sol.json field: sun, planets, moons, real radii (lab 17's specs, verbatim) — so every
// bill below is priced in the live game's heliocentric frame. Pulse pricing reads WORLD speeds,
// so the frame matters: deep in Saturn's well the ship rides at ~22 km/s heliocentric and a
// pulse there is a BIG pulse.
(string Id, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase)[] specs =
[
    ("sun", "", SunMu, 6.9634e8, 0, 0, 0),
    ("mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    ("venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    ("earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    ("mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", "sun", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
    ("luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0),
    ("europa", "jupiter", 3.2038e12, 1.5608e6, 6.709e8, 3.068226e5, 0.5),
    ("ganymede", "jupiter", 9.8907e12, 2.6341e6, 1.0704e9, 6.181531e5, 1.5),
    ("callisto", "jupiter", 7.1808e12, 2.4103e6, 1.8827e9, 1.4419307e6, 3.0),
    ("titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0),
    ("enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0),
];

var fullField = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Id, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase))]);

// The pocket alone (lab 17's pocketOnly): Saturn nailed to the origin, its two big moons riding
// it — the Saturn-centric frame the --viz scene draws in.
var pocketOnly = new CircularOrbitEphemeris(
[
    new CelestialBody("saturn", "saturn", null, SaturnMu, 5.8232e7, 0, 0, 0),
    new CelestialBody("titan", "titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0),
    new CelestialBody("enceladus", "enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0),
]);

var sim = new Simulator(fullField, timeStepSeconds: 60);

CelestialBody titan = fullField.Bodies.First(b => b.Id == "titan");
double titanHill = OrbitRule.HillRadius(titan, SaturnMu);
double titanCaptureRange = OrbitRule.CaptureRange(titanHill);

// Ship departure state: the Enceladus doorstep (lab-17 style). Riding Enceladus's rail velocity,
// 3,000 km outward off her surface line — just clear of her Hill sphere, free-flying in Saturn's
// well. This is the state the arm-click hands the planner in the game.
const double DoorstepOffset = 3e6;
double t0 = 0.0;
Vector2d satPos0 = fullField.Position("saturn", t0);
Vector2d encPos0 = fullField.Position("enceladus", t0);
Vector2d outward0 = (encPos0 - satPos0).Normalized();
Vector2d shipPos0 = encPos0 + outward0 * DoorstepOffset;
Vector2d shipVel0 = TransferMath.BodyVelocity(fullField, "enceladus", t0);
var doorstep = new ShipState(shipPos0, shipVel0, t0);

double r1 = (shipPos0 - satPos0).Length;     // doorstep's Saturn-distance
double r2 = titan.OrbitRadius;               // Titan's orbit radius

// The old approach loop, flown honestly — the exact AutopilotRehearsal switch, reproduced so the
// Wednesday hemorrhage is measured, not asserted. Returns the pulse bill, the summed burn Δv, the
// sim days, the flown path, and whether it ever inserted. Sums Δv from the actual burn magnitudes.
const double FineStep = 60.0, CoastStep = 1800.0, CoastFactor = 1.25;
(int Pulses, double DeltaV, double Days, List<TrajectorySample> Path, bool Inserted) FlyOldLoop(
    ShipState ship, string targetId, double horizonDays)
{
    CelestialBody body = fullField.Bodies.First(b => b.Id == targetId);
    CelestialBody parent = fullField.Bodies.First(b => b.Id == body.ParentId);
    double hill = OrbitRule.HillRadius(body, parent.Mu);
    double captureRange = OrbitRule.CaptureRange(hill);
    double startTime = ship.SimTime;
    double horizon = startTime + horizonDays * Day;
    var path = new List<TrajectorySample> { new(ship.SimTime, ship.Position) };
    int pulses = 0;
    double totalDv = 0;
    int iter = 0;
    while (ship.SimTime < horizon && iter++ < 40_000)
    {
        Vector2d bodyPos = fullField.Position(body.Id, ship.SimTime);
        Vector2d bodyVel = TransferMath.BodyVelocity(fullField, body.Id, ship.SimTime);
        // A moon's parent planet is a solid body the approach chord must round (matches the live loop).
        OrbitRule.ApproachObstacle? obstacle = parent.ParentId is null
            ? null
            : new OrbitRule.ApproachObstacle(
                fullField.Position(parent.Id, ship.SimTime), parent.BodyRadius * OrbitRule.ParentSafeBodyRadii);

        switch (OrbitRule.AutopilotDecision(ship, bodyPos, bodyVel, body, hill))
        {
            case OrbitRule.AutopilotAction.Approach:
                pulses += OrbitRule.ApproachPulseCost(ship, bodyPos, bodyVel, body, obstacle, hill);
                ShipState beforeApproach = ship;
                ship = OrbitRule.Approach(ship, bodyPos, bodyVel, body, obstacle, hill);
                totalDv += (ship.Velocity - beforeApproach.Velocity).Length;
                path.Add(new(ship.SimTime, ship.Position));
                ship = sim.RunAdaptive(ship, FineStep);
                path.Add(new(ship.SimTime, ship.Position));
                break;

            case OrbitRule.AutopilotAction.Insert:
                pulses += OrbitRule.PulseCost(ship, bodyPos, bodyVel, body);
                ShipState beforeInsert = ship;
                ship = OrbitRule.Insert(ship, bodyPos, bodyVel, body);
                totalDv += (ship.Velocity - beforeInsert.Velocity).Length;
                path.Add(new(ship.SimTime, ship.Position));
                return (pulses, totalDv, (ship.SimTime - startTime) / Day, path, true);

            default: // None — coast. Coarse far out (no burn possible), fine when near.
                double distance = (ship.Position - bodyPos).Length;
                double dt = distance > captureRange * CoastFactor ? CoastStep : FineStep;
                ship = sim.RunAdaptive(ship, dt);
                path.Add(new(ship.SimTime, ship.Position));
                break;
        }
    }

    return (pulses, totalDv, (ship.SimTime - startTime) / Day, path, false);
}

// ===================================================================================
// Section A — the bill as flown vs the bill as priced
// ===================================================================================
Console.WriteLine("=== Section A: the bill as flown vs the bill as priced ===");
Console.WriteLine("Fly the OLD approach loop (velocity re-sets) from the Enceladus doorstep to Titan");
Console.WriteLine("capture in the real simulator, and count. Then price the same trip with vis-viva +");
Console.WriteLine("Hohmann's closed form, in Saturn's frame:");
Console.WriteLine();

var old = FlyOldLoop(doorstep, "titan", 40.0);
var hohmann = TransferMath.Hohmann(r1, r2, SaturnMu);
double vEnc = Math.Sqrt(SaturnMu / r1);   // vis-viva circular speed at the doorstep radius
double vTitan = Math.Sqrt(SaturnMu / r2); // vis-viva circular speed at Titan's radius

Console.WriteLine($"{"old autopilot (flown)",-30}{"pulses",9}{"total dv (km/s)",18}{"sim days",11}{"captured",11}");
Console.WriteLine($"{"Enceladus doorstep -> Titan",-30}{old.Pulses,9}{old.DeltaV / 1000,18:F1}{old.Days,11:F1}{old.Inserted,11}");
Console.WriteLine();
Console.WriteLine($"{"Hohmann closed form (priced)",-30}{"dv1 (km/s)",13}{"dv2 (km/s)",13}{"total",9}{"TOF (d)",11}");
Console.WriteLine($"{"Enceladus -> Titan",-30}{hohmann.DepartDeltaV / 1000,13:F2}{hohmann.ArriveDeltaV / 1000,13:F2}" +
    $"{hohmann.TotalDeltaV / 1000,9:F2}{hohmann.TransferSeconds / Day,11:F2}");
Console.WriteLine();
Console.WriteLine($"vis-viva lane speeds: v_Enceladus {vEnc / 1000:F2} km/s (inner), v_Titan {vTitan / 1000:F2} km/s (outer).");
Console.WriteLine($"The flown loop spent {old.DeltaV / 1000:F1} km/s ({old.Pulses} pulses) — " +
    $"{old.DeltaV / hohmann.TotalDeltaV:F1}x the {hohmann.TotalDeltaV / 1000:F2} km/s the geometry actually costs.");
Console.WriteLine();

// ===================================================================================
// Section B — the window
// ===================================================================================
Console.WriteLine("=== Section B: the window ===");
double leadAngle = TransferMath.HohmannLeadAngle(r1, r2, SaturnMu);        // required Titan lead, rad
double shipPeriod = OrbitRule.LocalOrbitPeriod(r1, SaturnMu);             // ship's local period at the doorstep
double synodic = TransferMath.SynodicPeriod(shipPeriod, titan.OrbitPeriod);

// Current lead of Titan over the doorstep longitude, Saturn-relative, from the rails.
Vector2d shipRel0 = shipPos0 - satPos0;
Vector2d titanRel0 = fullField.Position("titan", t0) - satPos0;
double thShip0 = Math.Atan2(shipRel0.Y, shipRel0.X);
double thTitan0 = Math.Atan2(titanRel0.Y, titanRel0.X);
double Norm2Pi(double a) { a %= Math.Tau; return a < 0 ? a + Math.Tau : a; }
double lead0 = Norm2Pi(thTitan0 - thShip0);

// The ship (inner, fast) gains on Titan (outer, slow) at omega = n_ship - n_titan. The lead
// decreases through the window value alpha once per synodic period.
double omega = Math.Tau / shipPeriod - Math.Tau / titan.OrbitPeriod;
double firstWait = Norm2Pi(lead0 - leadAngle) / omega;

Console.WriteLine($"required Titan lead at departure (alpha = pi - n_Titan*TOF): {leadAngle * 180 / Math.PI:F1} deg");
Console.WriteLine($"synodic period (the bus interval): {synodic / 3600:F1} h");
Console.WriteLine($"current Titan lead over the doorstep at t=0: {lead0 * 180 / Math.PI:F1} deg");
Console.WriteLine();
Console.WriteLine($"{"window opening",-18}{"t (h from now)",18}{"t (days)",12}");
for (int k = 0; k < 3; k++)
{
    double t = firstWait + k * synodic;
    Console.WriteLine($"{"#" + (k + 1),-18}{t / 3600,18:F1}{t / Day,12:F2}");
}

Console.WriteLine();

// ===================================================================================
// Section C — the porkchop plate that rides the well
// ===================================================================================
Console.WriteLine("=== Section C: the porkchop, Lambert riding the well ===");
Console.WriteLine("Total dv (km/s) by departure hour x TOF, each cell one certified TransferMath.Lambert");
Console.WriteLine("solve in Saturn's frame from the doorstep radius (departure dv + arrival matching dv).");
Console.WriteLine("A '-' would be Lambert's honest refusal — no single-rev arc (Curtis 5.3):");
Console.WriteLine();

double[] tofDays = [2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5];
Console.Write("dep hr  |");
foreach (double td in tofDays)
{
    Console.Write($"{td,7:F1}");
}

Console.WriteLine("  <- TOF (days)");
Console.WriteLine(new string('-', 9 + 7 * tofDays.Length + 2));
(double dep, double tofD, double dv) bestCell = (0, 0, double.MaxValue);
for (double depHour = 0; depHour <= 36; depHour += 4)
{
    Console.Write($"{depHour,7:F0} |");
    double t = depHour * 3600;
    Vector2d satPos = fullField.Position("saturn", t);
    Vector2d encPos = fullField.Position("enceladus", t);
    Vector2d from = encPos + (encPos - satPos).Normalized() * DoorstepOffset;
    Vector2d shipRelVel = TransferMath.BodyVelocity(fullField, "enceladus", t) - TransferMath.BodyVelocity(fullField, "saturn", t);
    foreach (double td in tofDays)
    {
        double tofC = td * Day;
        Vector2d satArr = fullField.Position("saturn", t + tofC);
        Vector2d titanArr = fullField.Position("titan", t + tofC);
        if (TransferMath.Lambert(from - satPos, titanArr - satArr, tofC, SaturnMu) is not { } sol)
        {
            Console.Write("      -");
            continue;
        }

        Vector2d titanRelVel = TransferMath.BodyVelocity(fullField, "titan", t + tofC) - TransferMath.BodyVelocity(fullField, "saturn", t + tofC);
        double dv = (sol.V1 - shipRelVel).Length + (titanRelVel - sol.V2).Length;
        if (dv < bestCell.dv)
        {
            bestCell = (depHour, td, dv);
        }

        Console.Write($"{dv / 1000,7:F1}");
    }

    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine($"cheapest hand-scanned cell: depart hour {bestCell.dep:F0}, TOF {bestCell.tofD:F1} d, " +
    $"total dv {bestCell.dv / 1000:F2} km/s (Hohmann floor: {hohmann.TotalDeltaV / 1000:F2} km/s at {hohmann.TransferSeconds / Day:F2} d)");
Console.WriteLine("No '-' cells: the 180-deg blind spot is measure-zero and this coarse grid steps over it.");

// The blind spot itself, demonstrated on purpose: a transfer swept through exactly 180 deg has
// r2 anti-parallel to r1, sin(dTheta) = 0, and Lambert's f-and-g construction divides by ~0. The
// solver refuses (null) rather than certify garbage; nudge the geometry a hair off 180 and it solves.
Vector2d r1dir = (shipPos0 - satPos0).Normalized();
Vector2d r2at180 = -r1dir * r2;                                    // dead anti-parallel: exactly 180 deg
double smallTurn = 1.0 * Math.PI / 180;                           // 1 deg off
Vector2d r2at179 = new Vector2d(-r1dir.X * Math.Cos(smallTurn) + r1dir.Y * Math.Sin(smallTurn),
                                -r1dir.X * Math.Sin(smallTurn) - r1dir.Y * Math.Cos(smallTurn)) * r2;
double demoTof = hohmann.TransferSeconds;
string at180 = TransferMath.Lambert(r1dir * r1, r2at180, demoTof, SaturnMu) is { } ? "SOLVED" : "null (refused)";
string at179 = TransferMath.Lambert(r1dir * r1, r2at179, demoTof, SaturnMu) is { } ? "SOLVED" : "null (refused)";
Console.WriteLine($"  Lambert at exactly 180 deg: {at180};  at 179 deg: {at179}.");
Console.WriteLine();

// The planner's own scan (24x12 + a 5x5 refine), scored and priced with the game's own kernels.
var plan = TransferPlanner.Solve(sim, fullField, new TransferPlanner.Request(doorstep, "saturn", "titan", MaxWaitSeconds: 0));
Console.WriteLine("TransferPlanner.Solve (the engine the autopilot arms with):");
if (!plan.Ok)
{
    Console.WriteLine($"  refused: {plan.Failure}");
}
else
{
    Console.WriteLine($"  {plan.Summary}");
    Console.WriteLine($"  depart t+{(plan.DepartTime - t0) / 3600:F1} h, TOF {plan.TimeOfFlightSeconds / Day:F2} d, " +
        $"planned dv {plan.PlannedDeltaVTotal / 1000:F2} km/s, arrival {plan.ArrivalRelativeSpeed / 1000:F2} km/s rel, est. {plan.EstimatedPulses} pulses");
    Console.WriteLine($"  the floor sits at {plan.PlannedDeltaVTotal / 1000:F2} km/s — Hohmann's {hohmann.TotalDeltaV / 1000:F2} km/s, " +
        "rediscovered by a solver that never heard of apsides.");
}

Console.WriteLine();

// ===================================================================================
// Section D — flown, end to end (the headline)
// ===================================================================================
Console.WriteLine("=== Section D: the moon run, flown end to end ===");
List<TrajectorySample> transferPath = [];
if (!plan.Ok)
{
    Console.WriteLine($"planner refused ({plan.Failure}) — nothing to fly.");
}
else
{
    TransferPlanner.BurnStep burn = plan.Burns[0];

    // 1) Coast the doorstep to the departure instant, then apply the ONE planner burn — priced in
    //    pulses at the ship's world speed with the SAME OrbitRule.PulsesFor the live loop spends.
    ShipState atDepart = burn.SimTime > doorstep.SimTime
        ? sim.RunAdaptive(doorstep, burn.SimTime - doorstep.SimTime, maxTimeStep: 900)
        : doorstep;
    int departPulses = OrbitRule.PulsesFor(burn.DeltaV.Length, atDepart.Velocity.Length);
    var departed = atDepart with { Velocity = atDepart.Velocity + burn.DeltaV };

    // 2) Coast the transfer arc ballistically, sampling the closest approach to Titan (lab 06 style).
    transferPath = [.. sim.ProjectAdaptive(departed, null, plan.TimeOfFlightSeconds, maxTimeStep: 1800, maxSamples: 20_000)];
    double closest = double.MaxValue, tClose = departed.SimTime;
    foreach (TrajectorySample s in transferPath)
    {
        double d = (fullField.Position("titan", s.SimTime) - s.Position).Length;
        if (d < closest)
        {
            (closest, tClose) = (d, s.SimTime);
        }
    }

    // 3) From near arrival (the state at closest approach), run the SAME old-style capture loop —
    //    short-range now, so it inserts cheaply instead of hemorrhaging.
    ShipState nearArrival = tClose > departed.SimTime
        ? sim.RunAdaptive(departed, tClose - departed.SimTime, maxTimeStep: 900)
        : departed;
    var capture = FlyOldLoop(nearArrival, "titan", 20.0);

    int endToEndPulses = departPulses + capture.Pulses;

    Console.WriteLine($"{"leg",-34}{"event",34}");
    Console.WriteLine($"{"departure burn",-34}{$"{burn.DeltaV.Length / 1000:F2} km/s at t+{(burn.SimTime - t0) / 3600:F1} h, {departPulses} pulses",34}");
    Console.WriteLine($"{"ballistic transfer",-34}{$"{plan.TimeOfFlightSeconds / Day:F2} d coast, closest {closest / 1e6:F0} Mm from Titan",34}");
    Console.WriteLine($"{"capture handover (old loop)",-34}{$"{capture.Pulses} pulses, {capture.DeltaV / 1000:F2} km/s, inserted {capture.Inserted}",34}");
    Console.WriteLine($"{"capture range for reference",-34}{$"{titanCaptureRange / 1e6:F0} Mm ({titanCaptureRange / titanHill:F1} Titan Hill radii)",34}");
    Console.WriteLine();
    Console.WriteLine($"END-TO-END pulse bill:  new {endToEndPulses} pulses  vs  old {old.Pulses} pulses  " +
        $"(a {(double)old.Pulses / Math.Max(1, endToEndPulses):F1}x saving)");
    Console.WriteLine($"                        new {(burn.DeltaV.Length + capture.DeltaV) / 1000:F2} km/s dv  vs  old {old.DeltaV / 1000:F1} km/s dv");
    Console.WriteLine();
    Console.WriteLine("Titan even has an atmosphere (lab 22's aerobrake) to shave the arrival leg later — out");
    Console.WriteLine("of scope today. The lesson stands: inside a giant's well, the straight line is the most");
    Console.WriteLine("expensive path there is. Plan in the giant's frame and the well pays half the fare.");
}

// ===================================================================================
// --viz (optional): a Saturn-centric scene. Gated behind LabViz.Wants, so no-flag stdout is
// byte-identical. The flown arc and the old spiral are converted to Saturn-relative coordinates
// (worldPos - saturnPos at each sample time) so they sit in the pocketOnly frame.
// ===================================================================================
if (LabViz.Wants(args))
{
    var viz = new VizScene("lab23-the-moon-run", "Lab 23 — The moon run", "Enceladus -> Titan, flown in Saturn's frame");
    viz.AddBodies(pocketOnly.Bodies);

    List<TrajectorySample> ToSaturnRelative(IEnumerable<TrajectorySample> samples) =>
        [.. samples.Select(s => new TrajectorySample(s.SimTime, s.Position - fullField.Position("saturn", s.SimTime)))];

    // The old loop's spiral-of-resets as a toggleable comparison group.
    viz.AddPath("Wednesday's old approach (resets)", ToSaturnRelative(old.Path), VizColors.Sweep, "old", 1.2, 0.55);

    if (plan.Ok && transferPath.Count > 0)
    {
        TransferPlanner.BurnStep burn = plan.Burns[0];
        List<TrajectorySample> relTransfer = ToSaturnRelative(transferPath);
        viz.AddPath("planned transfer (Lambert)", relTransfer, VizColors.Trajectory, "main", 1.8, 1.0, ghost: true);

        // Markers: the departure burn, the closest pass to Titan, and the insertion event.
        double closest = double.MaxValue, tClose = burn.SimTime;
        foreach (TrajectorySample s in transferPath)
        {
            double d = (fullField.Position("titan", s.SimTime) - s.Position).Length;
            if (d < closest)
            {
                (closest, tClose) = (d, s.SimTime);
            }
        }

        Vector2d burnRel = relTransfer[0].Position;
        Vector2d closeRel = fullField.Position("titan", tClose) - fullField.Position("saturn", tClose);
        viz.AddMarker(burn.SimTime, burnRel, $"departure burn ({burn.DeltaV.Length / 1000:F2} km/s)", MarkerKinds.Burn);
        viz.AddMarker(tClose, closeRel, $"closest pass ({closest / 1e6:F0} Mm)", MarkerKinds.Closest);
        viz.AddMarker(relTransfer[^1].SimTime, relTransfer[^1].Position, "capture handover", MarkerKinds.Event);
    }

    LabViz.Show(viz, args);
}
