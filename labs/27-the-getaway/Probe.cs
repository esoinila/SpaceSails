// Lab 27 — The getaway
//
// Teaching voice: the game already computes CaughtPlayer honestly — EncounterRule flies a
// thrust-only wolf (owner's standing ruling: hunters chase with fixed thrust, NO gravity or
// autopilot smarts) and marks a catch when it comes inside CatchRadius under the catch-speed cap.
// PR-BUSTED turns that flag into a boarding scene, and its RESIST/RUN dice want to quote HONEST
// odds, not vibes. So this lab measures the chase from first principles: where is a catch physically
// EARNABLE (the wolves' honesty contract — no rubber-banding, ever), and what are the player's three
// escapes worth when flown through the real machinery — the SLING (lesson 19's crank, a bend the
// thrust-only wolf cannot follow), the SKIM heat-bleed (lesson 22's atmosphere drag, so the pursuer
// overshoots), and the PHASING JUKE (lesson 24's k-table read as evasion: change your clock so the
// intercept solution goes stale — "the cheaper-sooner tradeoff comes in handy when there is heat on
// us", owner). Section F prints the small PursuitOdds Core table these numbers become — the seam
// the BUSTED pop-up's dice read.
//
// Every number is the SAME Core the game flies with: EncounterRule.AdvanceHunter (the pursuit law),
// SlingPlanner.Solve (the crank), the Simulator's atmosphere drag (lesson 22), TransferMath.
// PhasingOrbit (lesson 24), OrbitRule.PulsesFor (the pulse kernel), and PursuitOdds (this lab's seam).
//
// IRONCLAD RULE: every number in labs/27-the-getaway/README.md came from running this probe. Change
// the code and the printed tables go stale — rerun and re-paste, never hand-edit a table.

using SpaceSails.Core;
using SpaceSails.LabViz;

const double Day = 86400.0;
const double Year = 365.25 * Day;
const double SunMu = 1.32712440018e20;
const double JupiterMu = 1.26686534e17;
const double RJ = 6.9911e7;
const double SaturnMu = 3.7931187e16;
const double TitanMu = 8.9781e12; // Section E's Saturn system (lesson 24)
const double G0 = 9.80665;

// ---------------------------------------------------------------------------------------------
// The pursuit driver: fly the REAL EncounterRule.AdvanceHunter law against a player whose state at
// any time is given by a function. The wolf is thrust-only by design — it feels no gravity, only
// HunterAccelMps2 toward the player's LIVE position — so the honest envelope is a gravity-free
// kinematics problem, flown in the very quanta (HunterStepSeconds) Map.razor drives the chase with.
// Returns caught?/time-to-catch/closest pass and the relative speed there (the overshoot tell).
// ---------------------------------------------------------------------------------------------
ChaseResult Chase(HunterState wolf, Func<double, ShipState> playerAt, double startTime, double horizonSeconds)
{
    double end = startTime + horizonSeconds;
    double minSep = double.MaxValue, relAtMin = 0, catchDays = double.NaN;
    for (double t = startTime + EncounterRule.HunterStepSeconds; t <= end; t += EncounterRule.HunterStepSeconds)
    {
        ShipState p = playerAt(t);
        wolf = EncounterRule.AdvanceHunter(wolf, p, t);
        double sep = (wolf.State.Position - p.Position).Length;
        double rel = (wolf.State.Velocity - p.Velocity).Length;
        if (sep < minSep) { minSep = sep; relAtMin = rel; }
        if (wolf.CaughtPlayer) { catchDays = (t - startTime) / Day; break; }
    }
    ShipState pf = playerAt(wolf.State.SimTime);
    return new ChaseResult(wolf.CaughtPlayer, catchDays, minSep, relAtMin, (wolf.State.Position - pf.Position).Length);
}

// A cursor-based interpolator over a flown path: position linearly, velocity from the local segment
// slope (the same read EncounterRule uses internally). Query times only grow through a chase, so the
// cursor makes the whole replay O(path) instead of rescanning every quantum.
Func<double, ShipState> PathReader(IReadOnlyList<TrajectorySample> path)
{
    int cursor = 0;
    return t =>
    {
        if (path.Count == 1) return new ShipState(path[0].Position, Vector2d.Zero, t);
        while (cursor < path.Count - 2 && path[cursor + 1].SimTime < t) cursor++;
        TrajectorySample a = path[cursor], b = path[cursor + 1];
        double span = b.SimTime - a.SimTime;
        if (span <= 0) return new ShipState(b.Position, Vector2d.Zero, t);
        Vector2d vel = (b.Position - a.Position) / span;
        double f = (t - a.SimTime) / span;
        return new ShipState(a.Position + (b.Position - a.Position) * f, vel, t);
    };
}

// ===================================================================================
// Section A — the wolf's contract (EncounterRule constants, and why a long chase overshoots)
// ===================================================================================
Console.WriteLine("=== Section A: the wolf's contract (thrust-only, by design) ===");
Console.WriteLine($"thrust accel     a  = {EncounterRule.HunterAccelMps2:F2} m/s^2 (toward the player's LIVE position — no gravity, no autopilot)");
Console.WriteLine($"catch radius     R  = {EncounterRule.CatchRadiusMeters / 1e3:N0} km");
Console.WriteLine($"catch speed cap  u  = {EncounterRule.CatchRelativeSpeedMetersPerSecond:N0} m/s (a wolf roaring past faster than this does NOT catch)");
Console.WriteLine($"integration quantum {EncounterRule.HunterStepSeconds:F0} s (same cadence as NPC traffic)");
Console.WriteLine();
double reach = EncounterRule.CatchRelativeSpeedMetersPerSecond * EncounterRule.CatchRelativeSpeedMetersPerSecond
    / (2 * EncounterRule.HunterAccelMps2);
Console.WriteLine($"The killer identity: from a standing start the wolf is still UNDER its {EncounterRule.CatchRelativeSpeedMetersPerSecond:N0} m/s catch cap only");
Console.WriteLine($"after accelerating across  u^2/(2a) = {reach / 1e3:N0} km. Chase a runner across more open space than");
Console.WriteLine($"that and it arrives too hot to grab — it overshoots. That single number is why the getaway is");
Console.WriteLine($"earnable at all: the thrust-only wolf is fast, but it cannot be fast AND gentle.");
Console.WriteLine();

// ===================================================================================
// Section B — the pursuit envelope: head start x flee speed, flown through the real law
// ===================================================================================
Console.WriteLine("=== Section B: the pursuit envelope (where a catch is physically earnable) ===");
Console.WriteLine("Fly the REAL AdvanceHunter law from a spread of head starts (initial separation) against a");
Console.WriteLine("player fleeing in a straight line at a spread of speeds. Cell = time-to-catch in days, or");
Console.WriteLine("'runs' when the wolf never closes to a <=cap grab inside the horizon. This is the wolves'");
Console.WriteLine("honesty contract: no rubber-banding, ever — a catch has to be geometrically earned.");
Console.WriteLine();

// The scene sits far out on the +x axis; the player flees radially OUTWARD (+x), so it is always
// farther from the world origin (the sun) than the wolf and EncounterRule.SunBlinded stays false by
// its first guard — the sun-glare assist is kept out of the clean envelope.
const double SceneX = 1e12; // ~Saturn's distance from the sun; only the sun-glare guard reads it
ChaseResult FleeChase(double headStartMeters, double fleeSpeedMps, double horizonDays)
{
    var playerPos0 = new Vector2d(SceneX, 0);
    var playerVel = new Vector2d(fleeSpeedMps, 0);
    var wolf = new HunterState("wolf", "WOLF", "policed", 0, 0,
        new ShipState(new Vector2d(SceneX - headStartMeters, 0), Vector2d.Zero, 0), false, false);
    return Chase(wolf, t => new ShipState(playerPos0 + playerVel * t, playerVel, t), 0, horizonDays * Day);
}

double[] headStarts = [1e8, 2e8, 3e8, 4e8, 6e8, 1e9, 3e9];
double[] fleeSpeeds = [0, 500, 1000, 2000, 3000, 5000];
const double EnvHorizonDays = 120.0;

Console.Write($"{"head start \\ flee",-18}");
foreach (double v in fleeSpeeds) Console.Write($"{v / 1000,9:F1} km/s");
Console.WriteLine();
Console.WriteLine(new string('-', 18 + fleeSpeeds.Length * 14));
foreach (double d in headStarts)
{
    Console.Write($"{d / 1e3,13:N0} km ");
    foreach (double v in fleeSpeeds)
    {
        ChaseResult r = FleeChase(d, v, EnvHorizonDays);
        Console.Write(r.Caught ? $"{r.CatchDays,10:F1} d " : $"{"runs",12} ");
    }
    Console.WriteLine();
}
Console.WriteLine();
Console.WriteLine("The overshoot, made concrete — three 'runs' cells, their CLOSEST pass and the relative speed there:");
foreach ((double d, double v) in new[] { (6e8, 1000.0), (1e9, 2000.0), (3e9, 0.0) })
{
    ChaseResult r = FleeChase(d, v, EnvHorizonDays);
    Console.WriteLine($"  head start {d / 1e3,10:N0} km, flee {v / 1000:F1} km/s -> closest {r.MinSepMeters / 1e3,10:N0} km at " +
        $"{r.RelAtMinSep,8:N0} m/s ({(r.RelAtMinSep > EncounterRule.CatchRelativeSpeedMetersPerSecond ? "HOT — roars past" : "under cap")})");
}
Console.WriteLine();
Console.WriteLine($"Read the cliff: inside ~R ({EncounterRule.CatchRadiusMeters / 1e3:N0} km) at a modest flee speed the wolf grabs you; past it");
Console.WriteLine("the closest pass is fast — it screams through the catch radius above the cap and cannot grab. The");
Console.WriteLine("boundary tightens as you run harder. A wolf that 'catches up' from far away always arrives too hot.");
Console.WriteLine();

// ===================================================================================
// Section C — the SLING escape (lesson 19's crank, a bend the wolf cannot follow)
// ===================================================================================
Console.WriteLine("=== Section C: the sling escape (SlingPlanner, real Earth->Jupiter case) ===");
var jbodies = new[]
{
    new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
    new CelestialBody("earth", "earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    new CelestialBody("jupiter", "jupiter", "sun", JupiterMu, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    new CelestialBody("saturn", "saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
};
var jeph = new CircularOrbitEphemeris(jbodies);
var jsim = new Simulator(jeph, timeStepSeconds: 60);

// A burn state on a real Earth->Jupiter approach that already passes close to Jupiter, ~40 d before
// closest approach (SlingPlannerTests' ApproachBurnState, verbatim) — the "integrate the plan to the
// burn node" input the plotting desk hands the solver.
double jdep = 100 * Day, jtof = 2.73 * Year;
var pad = RoutePlanner.DepartureState(jeph, "earth", "jupiter", jdep);
var jAt = jeph.Position("jupiter", jdep + jtof);
var lam = TransferMath.Lambert(pad.Position, jAt, jtof, SunMu); // sun at origin: heliocentric == absolute
ShipState slingBurn = jsim.RunAdaptive(new ShipState(pad.Position, lam!.Value.V1, jdep), jtof - 40 * Day);
double passEpoch = jdep + jtof;

double requested = 12 * RJ;
var slingReq = new SlingPlanner.Request(slingBurn, "jupiter", passEpoch, requested, SlingPlanner.PassSide.Lead);
SlingPlanner.Result sling = SlingPlanner.Solve(jsim, jeph, slingReq);

if (!sling.Ok)
{
    Console.WriteLine($"sling refused: {sling.Failure}");
}
else
{
    Console.WriteLine($"burn {sling.DeltaVMagnitude:F1} m/s, {sling.Iterations} Newton iters -> pass {sling.AchievedPassDistance / RJ:F1} R_J (Lead side).");
    Console.WriteLine($"heliocentric speed  before {sling.SpeedBefore / 1000:F2} km/s  ->  after {sling.SpeedAfter / 1000:F2} km/s   " +
        $"(gain {sling.SpeedGain:F0} m/s, {(sling.Escapes ? "ESCAPES the sun" : $"apoapsis {sling.ApoapsisAU:F1} AU")})");
    Console.WriteLine($"lever: one pulse at the aim shifts the far end of the pass by {sling.LeverGm:N0} Gm (re-trim after the pass).");
    Console.WriteLine();

    // The flyby DONATED heliocentric speed for free — the crank's whole point (lesson 19), here read
    // as an escape budget. SpeedBefore -> SpeedAfter is measured off the flown ±90 d arc by the solver.
    double donated = sling.SpeedGain;
    double wolfBurnHours = donated / EncounterRule.HunterAccelMps2 / 3600;
    Console.WriteLine($"The flyby donated {donated / 1000:F1} km/s of heliocentric speed — FREE, from gravity, at a corner the");
    Console.WriteLine($"thrust-only wolf (no gravity) cannot thread. To match just that speed it would burn {donated:N0}/{EncounterRule.HunterAccelMps2:F1} =");
    Console.WriteLine($"{wolfBurnHours:F1} HOURS of continuous, perfectly-aimed thrust — and through the pass it was pointed at your OLD vector.");
    Console.WriteLine();

    // Tie it to Section B's envelope. The boost throws the player's speed relative to any reacquiring
    // wolf far past the catch cap AND onto a departing trajectory — both axes of the 'runs' region. So
    // the sling is the STRATEGIC getaway: it doesn't shake a wolf on your tail this hour, it rewrites
    // the trajectory so the wolf's whole pursuit restarts from a boost-sized deficit.
    PursuitOdds.GeometryClass afterClass = PursuitOdds.Classify(3 * EncounterRule.CatchRadiusMeters, donated);
    Console.WriteLine($"Post-sling the player recedes at {donated / 1000:F1} km/s — {donated / EncounterRule.CatchRelativeSpeedMetersPerSecond:F0}x the {EncounterRule.CatchRelativeSpeedMetersPerSecond:N0} m/s catch cap — on a");
    Console.WriteLine($"{(sling.Escapes ? "sun-escape" : $"{sling.ApoapsisAU:F0}-AU")} arc. Any wolf must first match that by thrust alone; classified against the flown");
    Console.WriteLine($"envelope this is '{afterClass}' (Run odds {PursuitOdds.OddsFor(PursuitOdds.Trick.Run, afterClass)}). The sling's tactical cousins — the skim and the juke — follow.");
    Console.WriteLine();
}

// ===================================================================================
// Section D — the SKIM heat-bleed (lesson 22's drag, so the pursuer overshoots)
// ===================================================================================
Console.WriteLine("=== Section D: the skim heat-bleed (atmosphere drag at Jupiter, so the wolf overshoots) ===");
var jupiterAtm = new Atmosphere(RefDensity: 4.0e-6, ScaleHeight: 3.0e4, TopAltitude: 4.0e5); // sol.json Jupiter shell (lesson 22)
var jupAtmEph = new CircularOrbitEphemeris([new CelestialBody("jupiter", "jupiter", null, JupiterMu, RJ, 0, 0, 0, Atmosphere: jupiterAtm)]);
var jupAtmSim = new Simulator(jupAtmEph, timeStepSeconds: 1.0);

// Incoming hyperbolic state on the +x axis at radius rStart with vacuum periapsis rPeri (lesson 22's
// Arrival): angular momentum sets the tangential share, the rest is inbound radial.
ShipState Arrival(double mu, double rStart, double rPeri, double vInf)
{
    double vPeri = Math.Sqrt(vInf * vInf + 2 * mu / rPeri);
    double h = rPeri * vPeri;
    double v = Math.Sqrt(vInf * vInf + 2 * mu / rStart);
    double vt = h / rStart;
    double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
    return new ShipState(new Vector2d(rStart, 0), new Vector2d(vr, vt), 0);
}

// Fly ONE atmosphere pass (lesson 22's FlyPass, trimmed to what this lab needs): from an above-shell
// entry, down through periapsis, until the ship climbs back above the shell top. Returns the flown
// track, the speed shed, the peak g (the 3 g sail-hole line), and the deepest altitude.
(IReadOnlyList<TrajectorySample> Track, double DvShed, double PeakG, double MinAltKm, ShipState Post)
    SkimPass(double vInf, double periAltKm)
{
    double shellTop = RJ + jupiterAtm.TopAltitude;
    double rStart = shellTop + 3.0e5;
    ShipState s = Arrival(JupiterMu, rStart, RJ + periAltKm * 1000, vInf);
    double vEntry = s.Velocity.Length;
    var track = new List<TrajectorySample> { new(s.SimTime, s.Position) };
    double peak = 0, minAlt = double.PositiveInfinity;
    bool entered = false;
    while (s.SimTime < 12 * 3600)
    {
        (ShipState next, Simulator.DragReport rep) = jupAtmSim.RunAdaptiveWithDrag(s, 30.0, null, minTimeStep: 0.1, maxTimeStep: 2.0);
        peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
        if (!double.IsNaN(rep.MinAltitudeMeters)) minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
        s = next;
        track.Add(new TrajectorySample(s.SimTime, s.Position));
        double r = s.Position.Length;
        if (r < RJ) break;
        if (r < shellTop) entered = true;
        else if (entered) break;
    }
    // Δv shed = the loss in body-relative speed across the pass (the pursuer never feels it).
    double dvShed = vEntry - s.Velocity.Length;
    return (track, dvShed, peak / G0, double.IsPositiveInfinity(minAlt) ? double.NaN : minAlt / 1000, s);
}

Console.WriteLine($"Jupiter shell: refDensity {jupiterAtm.RefDensity:G3} kg/m^3, scale height {jupiterAtm.ScaleHeight / 1000:F0} km, top {jupiterAtm.TopAltitude / 1000:F0} km; sail-hole line {Atmosphere.SailHoleDecelG:F0} g.");
Console.WriteLine("Sweep the skim depth on a fixed arrival; the corridor is the depth that bleeds real speed under the g-line.");
Console.WriteLine();
const double SkimVInf = 5500.0; // a workaday Jupiter arrival excess (lesson 22)
Console.WriteLine($"arrival v_inf = {SkimVInf / 1000:F1} km/s");
Console.WriteLine($"{"peri alt km",13}{"min alt km",12}{"dv shed m/s",14}{"peak g",10}{"outcome",22}");
double bestSkimDv = 0, bestSkimAlt = double.NaN;
foreach (double periAltKm in new[] { 20.0, 40.0, 60.0, 80.0, 100.0, 130.0, 170.0, 220.0, 300.0 })
{
    var p = SkimPass(SkimVInf, periAltKm);
    string outcome = p.MinAltKm <= 0 ? "IMPACT" : p.PeakG > Atmosphere.SailHoleDecelG ? "TOO DEEP: hull holed" : p.DvShed >= 50 ? "clean skim" : "too shallow";
    Console.WriteLine($"{periAltKm,13:F0}{p.MinAltKm,12:F1}{p.DvShed,14:F0}{p.PeakG,10:F2}{outcome,22}");
    if (p.PeakG <= Atmosphere.SailHoleDecelG && p.MinAltKm > 0 && p.DvShed > bestSkimDv) { bestSkimDv = p.DvShed; bestSkimAlt = periAltKm; }
}
Console.WriteLine();

// Take the deepest CLEAN skim and measure the overshoot: a wolf tailing 1.5 R astern, matched to the
// pre-skim body-relative velocity, chasing the player's LIVE position through the pass. The player
// bleeds bestSkimDv and turns hard at periapsis; the wolf feels no drag and no gravity — it cannot
// make the turn and keeps the speed the player just shed, so it blows by.
{
    var p = SkimPass(SkimVInf, bestSkimAlt);
    var reader = PathReader(p.Track);
    ShipState entry = reader(p.Track[0].SimTime);
    Vector2d astern = -entry.Velocity.Normalized() * (1.5 * EncounterRule.CatchRadiusMeters);
    double horizon = p.Track[^1].SimTime;
    var skimWolf = new HunterState("wolf", "WOLF", "policed", 0, 0,
        new ShipState(entry.Position + astern, entry.Velocity, 0), false, false);
    ChaseResult sk = Chase(skimWolf, reader, 0, horizon);
    double reNull = bestSkimDv / EncounterRule.HunterAccelMps2;
    Console.WriteLine($"Deepest CLEAN skim: periapsis {bestSkimAlt:F0} km, sheds {bestSkimDv:F0} m/s under the {Atmosphere.SailHoleDecelG:F0} g line, pass lasts {horizon / 60:F0} min.");
    Console.WriteLine($"Tailing wolf (1.5 R astern, matched) through the pass: closest {sk.MinSepMeters / 1e3:N0} km at {sk.RelAtMinSep:N0} m/s, ends {sk.FinalSepMeters / 1e3:N0} km astern, {(sk.Caught ? "caught" : "OVERSHOOTS")}.");
    Console.WriteLine($"Overshoot margin: the wolf carries the {bestSkimDv:F0} m/s the player just shed as excess closing speed; at {EncounterRule.HunterAccelMps2:F1} m/s^2");
    Console.WriteLine($"it needs {reNull:F0} s ({reNull / 60:F1} min) merely to null it — and a wolf over the {EncounterRule.CatchRelativeSpeedMetersPerSecond:N0} m/s cap cannot grab meanwhile.");
    Console.WriteLine();
}

// ===================================================================================
// Section E — the PHASING JUKE (lesson 24's k-table read as evasion: staling the intercept)
// ===================================================================================
Console.WriteLine("=== Section E: the phasing juke (lesson 24's k-table as evasion — stale the intercept) ===");
var sbodies = new[]
{
    new CelestialBody("sun", "sun", null, SunMu, 6.9634e8, 0, 0, 0),
    new CelestialBody("saturn", "saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    new CelestialBody("titan", "titan", "saturn", TitanMu, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
    new CelestialBody("enceladus", "enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon),
    new CelestialBody("ringside-exchange", "ringside-exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station),
};
var seph = new CircularOrbitEphemeris(sbodies);
var ssim = new Simulator(seph, timeStepSeconds: 60);
CelestialBody ringside = seph.Bodies.First(b => b.Id == "ringside-exchange");
double railRadius = ringside.OrbitRadius;
const double ArcBehind = 92.64e6; // lesson 24's exact geometry: the player is on Ringside's lane

double t0 = 0.0;
Vector2d satPos0 = seph.Position("saturn", t0);
Vector2d satVel0 = TransferMath.BodyVelocity(seph, "saturn", t0);
Vector2d ringRel0 = seph.Position("ringside-exchange", t0) - satPos0;
double ringAngle0 = Math.Atan2(ringRel0.Y, ringRel0.X);
double shipAngle0 = ringAngle0 - ArcBehind / railRadius;
Vector2d shipRelPos0 = new Vector2d(Math.Cos(shipAngle0), Math.Sin(shipAngle0)) * railRadius;
Vector2d tangent0 = new Vector2d(-Math.Sin(shipAngle0), Math.Cos(shipAngle0));
double vCirc0 = Math.Sqrt(SaturnMu / railRadius);
var laneShip = new ShipState(satPos0 + shipRelPos0, satVel0 + tangent0 * vCirc0, t0);

// Read the phase geometry at the planner's fixed prep offset (t0 + 600 s), exactly as lesson 24 does.
const double PrepOffset = 600.0;
double tDep = t0 + PrepOffset;
ShipState atDep = ssim.RunAdaptive(laneShip, PrepOffset);
Vector2d satPosDep = seph.Position("saturn", tDep);
Vector2d satVelDep = TransferMath.BodyVelocity(seph, "saturn", tDep);
Vector2d shipRelPosDep = atDep.Position - satPosDep;
Vector2d shipRelVelDep = atDep.Velocity - satVelDep;
Vector2d targetRelPosDep = seph.Position("ringside-exchange", tDep) - satPosDep;
double rDep = shipRelPosDep.Length;
double gap = TransferMath.PhaseGap(shipRelPosDep, targetRelPosDep);
Vector2d progradeUnit = new Vector2d(-shipRelPosDep.Y, shipRelPosDep.X) / rDep;
double shipWorldSpeed = atDep.Velocity.Length;

// The OLD plot the wolf's intercept solution is aimed at: the player coasting straight on the lane.
// Fresh-scan position lookup (not the monotonic PathReader — these point queries are out of order
// across the k loop, so each call scans from the top).
var coastPlot = ssim.ProjectAdaptive(atDep, null, 8 * Day, maxTimeStep: 1800, maxSamples: 20_000);
Vector2d PosAt(IReadOnlyList<TrajectorySample> path, double t)
{
    for (int i = 0; i < path.Count - 1; i++)
    {
        if (t >= path[i].SimTime && t <= path[i + 1].SimTime)
        {
            double span = path[i + 1].SimTime - path[i].SimTime;
            double f = span > 0 ? (t - path[i].SimTime) / span : 0;
            return path[i].Position + (path[i + 1].Position - path[i].Position) * f;
        }
    }
    return path[^1].Position;
}

Console.WriteLine("The player is on Ringside's lane (lesson 24's 92,640 km case). A wolf's FIRING solution is aimed");
Console.WriteLine("at the player's CURRENT plot: coasting the lane. A round hits within OrdnanceRule's 0.5 Mm radius.");
Console.WriteLine("Commit a phasing juke (dip inside, coast k laps) and the player's real track walks off that plot —");
Console.WriteLine("once it walks off by more than the hit radius, the solution is a LIE. Cheaper jukes (more laps, tiny");
Console.WriteLine("burn) rot the shot slowly; the dear one-lap juke voids it soonest. Pulses vs staleness:");
Console.WriteLine();
double hitR = OrdnanceRule.HitRadiusMeters;
Console.WriteLine($"{"k",-4}{"enter m/s",11}{"pulses",9}{"wait d",9}{"stale @1d Mm",15}{"stale @3d Mm",15}{"shot void after",18}");
Console.WriteLine(new string('-', 84));
for (int k = 1; k <= 6; k++)
{
    if (TransferMath.PhasingOrbit(rDep, gap, SaturnMu, k, dipInside: true) is not { } plan) { Console.WriteLine($"{k,-4}(no bound dip this k)"); continue; }
    double phasingSpeed = Math.Sqrt(SaturnMu * (2 / rDep - 1 / plan.SemiMajorAxis));
    Vector2d vPhasing = progradeUnit * phasingSpeed;
    Vector2d dv1 = vPhasing - shipRelVelDep;
    int pulses = OrbitRule.PulsesFor(dv1.Length, shipWorldSpeed);
    // Fly the juke and read how far it has walked off the old coast plot at fixed look-aheads, and the
    // first hour the divergence exceeds the gun's hit radius (the shot solution is void from there).
    var jukeStart = atDep with { Velocity = atDep.Velocity + dv1 };
    var jukePath = ssim.ProjectAdaptive(jukeStart, null, 4 * Day, maxTimeStep: 1800, maxSamples: 20_000);
    double stale1 = (PosAt(jukePath, tDep + 1 * Day) - PosAt(coastPlot, tDep + 1 * Day)).Length;
    double stale3 = (PosAt(jukePath, tDep + 3 * Day) - PosAt(coastPlot, tDep + 3 * Day)).Length;
    double voidHours = double.NaN;
    for (double h = 0.5; h <= 96; h += 0.5)
    {
        if ((PosAt(jukePath, tDep + h * 3600) - PosAt(coastPlot, tDep + h * 3600)).Length > hitR) { voidHours = h; break; }
    }
    string voidTxt = double.IsNaN(voidHours) ? ">96 h" : $"{voidHours:F1} h";
    Console.WriteLine($"{k,-4}{plan.EnterDeltaV,11:F2}{pulses,9}{plan.WaitSeconds / Day,9:F1}{stale1 / 1e6,15:F2}{stale3 / 1e6,15:F2}{voidTxt,18}");
}
Console.WriteLine();
Console.WriteLine("Read it as the owner does: no heat, ride the cheap high-k bus (small enter burn, slow to diverge)");
Console.WriteLine("and wait it out; heat on our tail, pay the k=1 fare to be GONE — the biggest immediate staleness a");
Console.WriteLine("single up-front burn can buy, so the wolf's shot goes void soonest and you re-emerge on a new clock");
Console.WriteLine("(the wait column: k=1 is back at the doorstep in ~18 d, k=6 in ~111). Same k-table lesson 24 reads");
Console.WriteLine("for economy, read here for evasion: change your clock and the solution the wolf computed is a lie.");
Console.WriteLine();

// ===================================================================================
// Section F — the PursuitOdds Core table (the seam the BUSTED dice read)
// ===================================================================================
Console.WriteLine("=== Section F: PursuitOdds — the honesty contract as a Core table ===");
Console.WriteLine("Section B's envelope and C/D/E's measured margins become a small Core query the BUSTED pop-up's");
Console.WriteLine("RESIST/RUN dice draw modifiers from. It is pure data — no gameplay wiring. Printed straight from");
Console.WriteLine("the API so the README cannot drift from the code:");
Console.WriteLine();
Console.WriteLine($"catch boundary head start = {PursuitOdds.JawsHeadStartMeters / 1e3:N0} km (= EncounterRule catch radius); runner cap = {PursuitOdds.RunnerCatchCapMps:N0} m/s.");
Console.WriteLine();
Console.WriteLine("Geometry classes, from the flown envelope:");
foreach (PursuitOdds.GeometryClass g in Enum.GetValues<PursuitOdds.GeometryClass>())
    Console.WriteLine($"  {g,-11} — {PursuitOdds.Describe(g)}");
Console.WriteLine();
Console.WriteLine($"{"trick \\ geometry",-16}");
Console.Write($"{"",-16}");
foreach (PursuitOdds.GeometryClass g in Enum.GetValues<PursuitOdds.GeometryClass>()) Console.Write($"{g,-13}");
Console.WriteLine();
Console.WriteLine(new string('-', 16 + 13 * 4));
foreach (PursuitOdds.Trick trick in Enum.GetValues<PursuitOdds.Trick>())
{
    Console.Write($"{trick,-16}");
    foreach (PursuitOdds.GeometryClass g in Enum.GetValues<PursuitOdds.GeometryClass>())
    {
        PursuitOdds.EscapeOdds odds = PursuitOdds.OddsFor(trick, g);
        Console.Write($"{$"{odds}{(PursuitOdds.DiceModifier(trick, g) >= 0 ? "+" : "")}{PursuitOdds.DiceModifier(trick, g)}",-13}");
    }
    Console.WriteLine();
}
Console.WriteLine();
Console.WriteLine("EscapeOdds -> dice modifier ladder (the number the RESIST/RUN roll adds):");
foreach (PursuitOdds.EscapeOdds o in Enum.GetValues<PursuitOdds.EscapeOdds>())
    Console.WriteLine($"  {o,-10} {(PursuitOdds.ModifierFor(o) >= 0 ? "+" : "")}{PursuitOdds.ModifierFor(o)}");
Console.WriteLine();
Console.WriteLine("Sample queries (the pop-up asks these live):");
foreach ((double d, double v, PursuitOdds.Trick tk) in new[]
{
    (2e8, 500.0, PursuitOdds.Trick.Run),
    (3e8, 4000.0, PursuitOdds.Trick.Sling),
    (7e8, 1000.0, PursuitOdds.Trick.Skim),
    (2e9, 2000.0, PursuitOdds.Trick.PhasingJuke),
})
{
    PursuitOdds.GeometryClass g = PursuitOdds.Classify(d, v);
    Console.WriteLine($"  head start {d / 1e3,10:N0} km, rel {v / 1000:F1} km/s -> {g,-11} | {tk,-11} odds {PursuitOdds.OddsFor(tk, g)} (dice {(PursuitOdds.DiceModifier(tk, g) >= 0 ? "+" : "")}{PursuitOdds.DiceModifier(tk, g)})");
}
Console.WriteLine();

// ===================================================================================
// --viz (optional): one sling escape and one juke, gated behind LabViz.Wants so no-flag stdout is
// byte-identical. The sling in the sun frame (the whip past Jupiter); the juke in Saturn's frame.
// ===================================================================================
if (LabViz.Wants(args))
{
    var viz = new VizScene("lab27-the-getaway", "Lab 27 — The getaway",
        "one sling escape (whip past Jupiter) and one phasing juke (stale the intercept)");

    // The sling, in the heliocentric frame: the slung getaway arc past Jupiter.
    if (sling.Ok)
    {
        viz.AddBodies(jbodies);
        var slungStart = slingBurn with { Velocity = slingBurn.Velocity + sling.DeltaV };
        double h = (passEpoch + 120 * Day) - slingBurn.SimTime;
        var path = jsim.ProjectAdaptive(slungStart, null, h, maxTimeStep: 43200, maxSamples: 20_000);
        viz.AddPath("sling getaway (whip Jupiter)", path, VizColors.Trajectory, "sling", 1.8, 1.0, ghost: true);
        viz.AddMarker(slingBurn.SimTime, slingBurn.Position, $"aim burn ({sling.DeltaVMagnitude:F0} m/s)", MarkerKinds.Burn);
        viz.AddMarker(sling.PassEpoch, jeph.Position("jupiter", sling.PassEpoch), $"Jupiter flyby ({sling.AchievedPassDistance / RJ:F0} R_J)", MarkerKinds.Flyby);
    }

    // The juke, in Saturn's frame: the k=1 dip walking off the old coast plot.
    List<TrajectorySample> ToSaturn(IEnumerable<TrajectorySample> s) =>
        [.. s.Select(x => new TrajectorySample(x.SimTime, x.Position - seph.Position("saturn", x.SimTime)))];
    if (TransferMath.PhasingOrbit(rDep, gap, SaturnMu, 1, dipInside: true) is { } k1)
    {
        double phasingSpeed = Math.Sqrt(SaturnMu * (2 / rDep - 1 / k1.SemiMajorAxis));
        Vector2d dv1 = progradeUnit * phasingSpeed - shipRelVelDep;
        var jukeStart = atDep with { Velocity = atDep.Velocity + dv1 };
        var jukePath = ssim.ProjectAdaptive(jukeStart, null, k1.WaitSeconds, maxTimeStep: 1800, maxSamples: 20_000);
        viz.AddPath("phasing juke (k=1 dip)", ToSaturn(jukePath), VizColors.Ship, "juke", 1.6, 1.0);
        viz.AddPath("stale plot (old coast)", ToSaturn(coastPlot), VizColors.Sweep, "coast", 1.2, 0.6);
        viz.AddMarker(tDep, shipRelPosDep, $"juke burn ({k1.EnterDeltaV:F0} m/s)", MarkerKinds.Burn);
    }
    LabViz.Show(viz, args);
}

record ChaseResult(bool Caught, double CatchDays, double MinSepMeters, double RelAtMinSep, double FinalSepMeters);
