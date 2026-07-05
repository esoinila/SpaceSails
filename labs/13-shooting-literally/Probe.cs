// Lab 13 — Shooting, literally
//
// Teaching voice: every transfer you plotted in lessons 4-5 was secretly a BOUNDARY VALUE
// PROBLEM — "be at that place at that time" — and you solved it by scanning candidates. The
// gun deck (M28, `FireControl.cs`) can't afford a scan per shot; it uses the numericist's
// classic instead: the SHOOTING METHOD. Guess the launch bearing and charge, FLY the slug
// through the real integrator, measure the miss, and let Newton turn the miss into a better
// guess. Two unknowns (bearing, charge) for two constraints (the aim point's x and y): with
// a fixed muzzle speed exact hits would be measure-zero luck, which is why the mass driver's
// charge is adjustable. This lesson states the BVP, watches Newton converge iteration by
// iteration (the same trace the war room animates), prices what honesty costs — dispersion
// is the target TRACK's uncertainty, not the solver's — and probes the validity window.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code,
// the printed numbers in labs/13-shooting-literally/README.md go stale — rerun and re-paste.

using SpaceSails.Core;

const double Hour = 3600.0;
const double AU = 1.496e11;
const double SunMu = 1.32712440018e20;

var sun = new CelestialBody("sun", "Sun", null, SunMu, 6.9634e8, 0, 0, 0);
var ephemeris = new CircularOrbitEphemeris([sun]);
var sim = new Simulator(ephemeris, timeStepSeconds: 60);

// The pirate's geometry: we ride a circular 1 AU orbit; the prey drifts 200,000 km ahead
// on nearly the same orbit, slowly opening. Fire control is close work (lesson 7's capture
// envelope is 5e8 m) — this is exactly the range where boarding-or-shooting gets decided.
double circular = Math.Sqrt(SunMu / AU);
var shooter = new ShipState(new Vector2d(AU, 0), new Vector2d(0, circular), 0);
var prey = new ShipState(
    shooter.Position + new Vector2d(-3e7, 2e8),
    shooter.Velocity + new Vector2d(-900, 350), 0);

// ===================================================================================
// Section A — the boundary value problem, and why "just aim at it" fails
// ===================================================================================
Console.WriteLine("=== Section A: the BVP — aim where they WILL be, and even that isn't enough ===");
const double tHit = 12 * Hour;
var observation = new Observation("prey", 0, prey.Position, prey.Velocity);
PredictedPath track = PathPredictor.Predict(ephemeris, observation, null, tHit);
Vector2d aimPoint = track.Samples[^1].Position;
Console.WriteLine($"prey now:            {(prey.Position - shooter.Position).Length / 1000:N0} km away");
Console.WriteLine($"aim point (t+12 h):  {(aimPoint - shooter.Position).Length / 1000:N0} km away, on the prey's PREDICTED track");

// The naive shot: point the barrel straight at the aim point, charge from flat geometry.
Vector2d requiredRel = (aimPoint - shooter.Position) / tHit - shooter.Velocity;
double naiveBearing = Math.Atan2(requiredRel.Y, requiredRel.X);
double naiveCharge = requiredRel.Length;
var naiveSlug = new ShipState(
    shooter.Position,
    shooter.Velocity + new Vector2d(Math.Cos(naiveBearing), Math.Sin(naiveBearing)) * naiveCharge,
    0);
ShipState naiveArrived = sim.RunAdaptive(naiveSlug, tHit);
Console.WriteLine($"straight-line charge: {naiveCharge:F1} m/s at bearing {naiveBearing * 180 / Math.PI:F3} deg");
Console.WriteLine($"straight-line miss:   {(naiveArrived.Position - aimPoint).Length / 1000:F1} km  <- gravity bent the shot");
Console.WriteLine();

// ===================================================================================
// Section B — the shooting method: Newton on the integrator, trace and all
// ===================================================================================
Console.WriteLine("=== Section B: Newton turns the miss into the next guess ===");
FireControl.Solution solution = FireControl.Solve(sim, shooter, maxMuzzleSpeed: 8000, aimPoint, tHit);
Console.WriteLine($"{"iter",4}  {"bearing (deg)",14}  {"charge (m/s)",13}  {"miss (km)",12}");
foreach (FireControl.IterationStep step in solution.Trace)
{
    Console.WriteLine($"{step.Iteration,4}  {step.BearingRad * 180 / Math.PI,14:F6}  {step.MuzzleSpeed,13:F2}  {step.MissMeters / 1000,12:F3}");
}

Console.WriteLine($"converged: {solution.Converged} (tolerance {FireControl.ConvergedMissMeters / 1000:F0} km)");
Console.WriteLine($"solution:  bearing {solution.BearingRad * 180 / Math.PI:F4} deg, charge {solution.MuzzleSpeed:F1} m/s, flight {solution.TimeOfFlightSeconds / Hour:F1} h");
Console.WriteLine();

// ===================================================================================
// Section C — dispersion is the TRACK's uncertainty, not the solver's
// ===================================================================================
Console.WriteLine("=== Section C: the solver is exact; the fix is not ===");
Console.WriteLine("The residual above is meters. The honest error budget is the target cone's");
Console.WriteLine("half-width at t_hit (lesson 8, incl. the M28 impulse term) — per fix age:");
Console.WriteLine($"{"fix age",10}  {"cone at t_hit (km)",20}");
foreach (double ageHours in new[] { 0.0, 6.0, 24.0 })
{
    var staleObs = new Observation("prey", -ageHours * Hour, prey.Position, prey.Velocity);
    PredictedPath staleTrack = PathPredictor.Predict(ephemeris, staleObs, null, tHit + ageHours * Hour);
    Console.WriteLine($"{ageHours,8:F0} h  {staleTrack.HalfWidthAt(tHit) / 1000,20:N0}");
}

Console.WriteLine();
Console.WriteLine("And per LEAD TIME with a fresh fix — why gunners shoot short leads:");
Console.WriteLine($"{"lead",8}  {"cone at t_hit (km)",20}");
foreach (double leadHours in new[] { 1.0, 3.0, 6.0, 12.0 })
{
    Console.WriteLine($"{leadHours,6:F0} h  {track.HalfWidthAt(leadHours * Hour) / 1000,20:N0}");
}

Console.WriteLine("-> telescope work at the Sensors desk literally buys tighter shots: fire-");
Console.WriteLine("   control quality IS track quality. One weapon system across two stations.");
Console.WriteLine();

// ===================================================================================
// Section D — the validity window: how long does a solution keep?
// ===================================================================================
Console.WriteLine("=== Section D: a locked solution goes stale — measured ===");
Console.WriteLine($"validity window reported by the solver: {solution.ValiditySeconds:F0} s");
Console.WriteLine($"{"fire delay (s)",14}  {"miss (km)",12}");
foreach (double delay in new[] { 0.0, 60.0, 300.0, 900.0, 3600.0 })
{
    ShipState coasted = delay > 0 ? sim.RunAdaptive(shooter, delay) : shooter;
    var slug = new ShipState(
        coasted.Position, coasted.Velocity + solution.LaunchDirection * solution.MuzzleSpeed, coasted.SimTime);
    ShipState arrived = sim.RunAdaptive(slug, tHit - delay);
    Console.WriteLine($"{delay,14:F0}  {(arrived.Position - aimPoint).Length / 1000,12:F1}");
}

Console.WriteLine();

// ===================================================================================
// Section E — break it: the driver's reach is a hard wall
// ===================================================================================
Console.WriteLine("=== Section E: beyond the reach, Newton can only report best effort ===");
Vector2d farAim = shooter.Position + new Vector2d(5e8, 8e8); // needs ~21 km/s of charge
FireControl.Solution far = FireControl.Solve(sim, shooter, maxMuzzleSpeed: 8000, farAim, tHit);
Console.WriteLine($"aim {(farAim - shooter.Position).Length / 1000:N0} km out at t+12 h with an 8 km/s driver:");
Console.WriteLine($"converged: {far.Converged}, best-effort miss {far.ExpectedMissMeters / 1000:N0} km, charge pinned at {far.MuzzleSpeed:F0} m/s");
Console.WriteLine("-> the war room's NO SOLUTION is this, verbatim: not a bug, a boundary.");
