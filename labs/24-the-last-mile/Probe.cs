// Lab 24 — The last mile
//
// Teaching voice: lesson 23 taught the ship to cross a giant's well cheap. Then the owner tried
// to reach Ringside Exchange 92,640 km away on the SAME Saturn lane — practically next door — and
// the armed autopilot declined at ~229 pulses. The last mile was the expensive one. Lambert can't
// price it: a phasing loop returns to its own start after one revolution, which is exactly the 2pi
// geometry the single-rev solver refuses (and should). The closed form doesn't need it — change
// your PERIOD, not your path. Dip a hair inside, coast k laps, and the target arrives at your
// doorstep as you return to it. Two small burns; the well does the chasing.
//
// This probe reproduces the hemorrhage honestly (Section A: the legacy point-and-throttle loop
// flown from 92,640 km), checks the phasing bus math against the rails and tells the honest
// Kepler-vs-authored-period story (Section B), flies the planner's two-burn schedule end to end
// through the real N-body sim into the dock envelope (Section C), and reads the cheaper-vs-sooner
// trade table the planner now returns (Section D). Every number is the SAME Core code the autopilot
// spends with: TransferMath.PhasingOrbit/PhaseGap, TransferPlanner.Solve, OrbitRule — Curtis ch. 6.5.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code, the
// printed numbers in labs/24-the-last-mile/README.md go stale — rerun and re-paste, never
// hand-edit a table.

using SpaceSails.Core;
using SpaceSails.LabViz;

const double Day = 86400.0;
const double SunMu = 1.32712440018e20;
const double SaturnMu = 3.7931187e16;

// Dock coaching envelope — the game's own numbers, the ones Map.razor prints when you fly a station:
// coast within 500,000 km, closing under 8 km/s. Cited here as the arrival yardstick for a mu=0 body
// (no OrbitRule.Insert exists for a mass-less station — "captured" is being inside this envelope).
const double DockEnvelopeMeters = 5e8;
const double DockMatchSpeed = 8000.0;

// Full sol.json field: sun, planets, moons (lab 17's specs, verbatim) PLUS ringside-exchange — so
// every bill below is priced in the live game's heliocentric frame. Pulse pricing reads WORLD
// speeds, so the frame matters: on Saturn's ring the ship rides at ~15 km/s heliocentric and a pulse
// there is a big pulse.
(string Id, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase, BodyKind Kind)[] specs =
[
    ("sun", "", SunMu, 6.9634e8, 0, 0, 0, BodyKind.Planet),
    ("mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0, BodyKind.Planet),
    ("venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9, BodyKind.Planet),
    ("earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8, BodyKind.Planet),
    ("mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7, BodyKind.Planet),
    ("jupiter", "sun", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6, BodyKind.Planet),
    ("saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5, BodyKind.Planet),
    ("uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4, BodyKind.Planet),
    ("neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4, BodyKind.Planet),
    ("luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0, BodyKind.Moon),
    ("europa", "jupiter", 3.2038e12, 1.5608e6, 6.709e8, 3.068226e5, 0.5, BodyKind.Moon),
    ("ganymede", "jupiter", 9.8907e12, 2.6341e6, 1.0704e9, 6.181531e5, 1.5, BodyKind.Moon),
    ("callisto", "jupiter", 7.1808e12, 2.4103e6, 1.8827e9, 1.4419307e6, 3.0, BodyKind.Moon),
    ("titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
    ("enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon),
    // The owner's #155 target: a mass-less trading station on Saturn's ring lane (scenarios/sol.json).
    ("ringside-exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station),
];

var fullField = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Id, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase, s.Kind))]);

// The pocket alone: Saturn nailed to the origin, its moons and the ring station riding it — the
// Saturn-centric frame the --viz scene draws in.
var pocketOnly = new CircularOrbitEphemeris(
[
    new CelestialBody("saturn", "saturn", null, SaturnMu, 5.8232e7, 0, 0, 0),
    new CelestialBody("titan", "titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon),
    new CelestialBody("enceladus", "enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon),
    new CelestialBody("ringside-exchange", "ringside-exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station),
]);

var sim = new Simulator(fullField, timeStepSeconds: 60);

CelestialBody ringside = fullField.Bodies.First(b => b.Id == "ringside-exchange");
double railRadius = ringside.OrbitRadius;              // 1.35e9 m
const double ArcBehind = 92.64e6;                      // the owner's exact geometry: 92,640 km of arc

// Ship departure state: on Ringside's lane, 92,640 km of arc BEHIND it (issue #155). Position = the
// rail point at (Ringside's angle - ArcBehind/railRadius). Velocity = the rail-tangent circular
// velocity in the world frame — Saturn's velocity plus the local CCW circular velocity sqrt(mu/r).
double t0 = 0.0;
Vector2d satPos0 = fullField.Position("saturn", t0);
Vector2d satVel0 = TransferMath.BodyVelocity(fullField, "saturn", t0);
Vector2d ringRel0 = fullField.Position("ringside-exchange", t0) - satPos0;
double ringAngle0 = Math.Atan2(ringRel0.Y, ringRel0.X);
double shipAngle0 = ringAngle0 - ArcBehind / railRadius;
Vector2d shipRelPos0 = new Vector2d(Math.Cos(shipAngle0), Math.Sin(shipAngle0)) * railRadius;
Vector2d tangent0 = new Vector2d(-Math.Sin(shipAngle0), Math.Cos(shipAngle0)); // CCW prograde unit
double vCirc0 = Math.Sqrt(SaturnMu / railRadius);
Vector2d shipPos0 = satPos0 + shipRelPos0;
Vector2d shipVel0 = satVel0 + tangent0 * vCirc0;
var ship = new ShipState(shipPos0, shipVel0, t0);

// The old approach loop, flown honestly — the exact AutopilotRehearsal switch, reproduced so the
// #155 decline is measured, not asserted. A mu=0 station has no capture/insertion, so the loop can
// only ever "Approach" (point at the station, throttle to 4 km/s closing) — every re-set leaves the
// lane and Saturn charges for it. Flies ONE honest brute-force approach: stop the instant the ship
// first forces itself to true docking range (1,000 km), so the pulse bill is the cost of a single
// attempt to reach Ringside the straight-line way (the trip the autopilot rehearsed and declined),
// not an unbounded thrash. Returns pulses, summed burn Δv, sim days, the flown path, reached flag.
const double FineStep = 60.0, CoastStep = 1800.0, CoastFactor = 1.25;
const double DockingRange = 1e6; // 1,000 km — "arrived at the station" for the brute-force attempt
(int Pulses, double DeltaV, double Days, List<TrajectorySample> Path, bool Reached) FlyOldLoop(
    ShipState s, string targetId, double horizonDays)
{
    CelestialBody body = fullField.Bodies.First(b => b.Id == targetId);
    CelestialBody parent = fullField.Bodies.First(b => b.Id == body.ParentId);
    double hill = OrbitRule.HillRadius(body, parent.Mu);            // 0 for a mass-less station
    double captureRange = OrbitRule.CaptureRange(hill);             // floors at 3e9 m
    double startTime = s.SimTime;
    double horizon = startTime + horizonDays * Day;
    var path = new List<TrajectorySample> { new(s.SimTime, s.Position) };
    int pulses = 0;
    double totalDv = 0;
    int iter = 0;
    while (s.SimTime < horizon && iter++ < 60_000)
    {
        Vector2d bodyPos = fullField.Position(body.Id, s.SimTime);
        Vector2d bodyVel = TransferMath.BodyVelocity(fullField, body.Id, s.SimTime);
        if ((s.Position - bodyPos).Length <= DockingRange)
        {
            return (pulses, totalDv, (s.SimTime - startTime) / Day, path, true);
        }

        // The parent planet is a solid body the approach chord must round (matches the live loop).
        OrbitRule.ApproachObstacle? obstacle = parent.ParentId is null
            ? null
            : new OrbitRule.ApproachObstacle(
                fullField.Position(parent.Id, s.SimTime), parent.BodyRadius * OrbitRule.ParentSafeBodyRadii);

        switch (OrbitRule.AutopilotDecision(s, bodyPos, bodyVel, body, hill))
        {
            case OrbitRule.AutopilotAction.Approach:
                pulses += OrbitRule.ApproachPulseCost(s, bodyPos, bodyVel, body, obstacle, hill);
                ShipState before = s;
                s = OrbitRule.Approach(s, bodyPos, bodyVel, body, obstacle, hill);
                totalDv += (s.Velocity - before.Velocity).Length;
                path.Add(new(s.SimTime, s.Position));
                s = sim.RunAdaptive(s, FineStep);
                path.Add(new(s.SimTime, s.Position));
                break;

            default: // None/Insert — coast (a mu=0 station never opens an insertion window).
                double distance = (s.Position - bodyPos).Length;
                double dt = distance > captureRange * CoastFactor ? CoastStep : FineStep;
                s = sim.RunAdaptive(s, dt);
                path.Add(new(s.SimTime, s.Position));
                break;
        }
    }

    return (pulses, totalDv, (s.SimTime - startTime) / Day, path, false);
}

// ===================================================================================
// Shared phasing pricing — the SAME arithmetic TransferPlanner runs per candidate, reproduced here
// (public TransferMath + BodyVelocity + OrbitRule.PulsesFor) so Section B's table is the planner's
// own numbers, not a parallel model. Read at the planner's fixed prep offset (t0 + 600 s).
// ===================================================================================
const double PrepOffset = 600.0;
double tDep = t0 + PrepOffset;
ShipState shipAtDep = sim.RunAdaptive(ship, PrepOffset);
Vector2d parentPosDep = fullField.Position("saturn", tDep);
Vector2d parentVelDep = TransferMath.BodyVelocity(fullField, "saturn", tDep);
Vector2d shipRelPosDep = shipAtDep.Position - parentPosDep;
Vector2d shipRelVelDep = shipAtDep.Velocity - parentVelDep;
Vector2d targetRelPosDep = fullField.Position("ringside-exchange", tDep) - parentPosDep;
double rDep = shipRelPosDep.Length;
double gap = TransferMath.PhaseGap(shipRelPosDep, targetRelPosDep);   // signed lead, target over ship
double gapNorm = gap < 0 ? gap + Math.Tau : gap;                      // [0, 2pi)
Vector2d progradeUnit = new Vector2d(-shipRelPosDep.Y, shipRelPosDep.X) / rDep;
double thBurn = Math.Atan2(shipRelPosDep.Y, shipRelPosDep.X);

double keplerPeriod = OrbitRule.LocalOrbitPeriod(rDep, SaturnMu);     // 2pi*sqrt(r^3/mu) — Kepler
double nKepler = Math.Tau / keplerPeriod;
double nAuthored = Math.Tau / ringside.OrbitPeriod;                   // the station's AUTHORED rail rate

double FoldPi(double a) { a = Math.IEEERemainder(a, Math.Tau); return a <= -Math.PI ? a + Math.Tau : a; }

(double EnterDv, double ExitDv, double Total, int Pulses, double WaitS, double PhasingPeriod,
 double ResKepler, double ResAuthored, double Periapsis, double Apoapsis)? PricePhasing(int k, bool dip)
{
    if (TransferMath.PhasingOrbit(rDep, gap, SaturnMu, k, dip) is not { } plan)
    {
        return null;
    }

    double tRdv = tDep + plan.WaitSeconds;
    double phasingSpeed = Math.Sqrt(SaturnMu * (2 / rDep - 1 / plan.SemiMajorAxis));
    Vector2d vPhasing = progradeUnit * phasingSpeed;
    Vector2d dv1 = vPhasing - shipRelVelDep;
    Vector2d targetRelVelRdv = TransferMath.BodyVelocity(fullField, "ringside-exchange", tRdv)
                               - TransferMath.BodyVelocity(fullField, "saturn", tRdv);
    Vector2d dv2 = targetRelVelRdv - vPhasing;
    double dv1m = dv1.Length, dv2m = dv2.Length;
    double shipWorldSpeed = shipAtDep.Velocity.Length;
    double targetWorldSpeed = TransferMath.BodyVelocity(fullField, "ringside-exchange", tRdv).Length;
    int pulses = OrbitRule.PulsesFor(dv1m, shipWorldSpeed) + OrbitRule.PulsesFor(dv2m, targetWorldSpeed);

    // Closure residual: the target's longitude relative to the burn apsis at t_rdv. The target leads
    // the ship by g at departure and advances at rate n over the k*T_ph coast; the ship returns to
    // thBurn. residual = fold(g + n*k*T_ph). With the KEPLER rate the phasing identity makes this ~0;
    // with the AUTHORED rail rate it drifts by (n_auth - n_kepler)*k*T_ph — the honest wrinkle.
    double resKepler = FoldPi(gapNorm + nKepler * plan.WaitSeconds);
    double resAuthored = FoldPi(gapNorm + nAuthored * plan.WaitSeconds);

    return (plan.EnterDeltaV, plan.ExitDeltaV, dv1m + dv2m, pulses, plan.WaitSeconds, plan.PhasingPeriod,
        resKepler, resAuthored, plan.Periapsis, plan.Apoapsis);
}

// ===================================================================================
// Section A — the last mile is the expensive mile
// ===================================================================================
Console.WriteLine("=== Section A: the last mile is the expensive mile ===");
Console.WriteLine("Fly the OLD approach loop (point at the station, throttle to 4 km/s closing) from");
Console.WriteLine("92,640 km behind Ringside on the same lane, in the real simulator, and count. A mu=0");
Console.WriteLine("station never opens an insertion window, so the loop can only re-set and re-buy — the");
Console.WriteLine("#155 geometry the armed autopilot declined at ~229 pulses. Price the phasing catch-up beside it:");
Console.WriteLine();

var legacy = FlyOldLoop(ship, "ringside-exchange", 5.0);
var k1 = PricePhasing(1, dip: true)!.Value;

Console.WriteLine($"{"legacy loop (flown to 1,000 km)",-32}{"pulses",9}{"total dv (km/s)",18}{"reached",11}");
Console.WriteLine($"{"92,640 km behind -> Ringside",-32}{legacy.Pulses,9}{legacy.DeltaV / 1000,18:F1}{legacy.Reached,11}");
Console.WriteLine();
Console.WriteLine($"{"phasing k=1 dip (priced)",-32}{"enter (m/s)",13}{"exit (m/s)",13}{"total",9}{"wait (d)",11}");
Console.WriteLine($"{"92,640 km behind -> Ringside",-32}{k1.EnterDv,13:F2}{k1.ExitDv,13:F2}{k1.Total,9:F1}{k1.WaitS / Day,11:F1}");
Console.WriteLine();
double approachClose = OrbitRule.MaxRelativeSpeed * OrbitRule.ApproachSpeedFraction; // 4 km/s
Console.WriteLine($"lane speed at the ring: v_circular {vCirc0 / 1000:F2} km/s; current gap to Ringside {gapNorm * 180 / Math.PI:F2} deg ({ArcBehind / 1000:F0} km of arc).");
Console.WriteLine($"One brute-force attempt to reach the station spent {legacy.DeltaV / 1000:F1} km/s ({legacy.Pulses} pulses) — pointing at a");
Console.WriteLine($"neighbour on its own lane and leaving the rail to force the gap shut — and it arrives at the");
Console.WriteLine($"{approachClose / 1000:F0} km/s approach speed it cannot shed, so it screams past and must re-buy the whole approach to");
Console.WriteLine($"hold: the endless re-set the ~229-pulse rehearsal projected before the armed autopilot declined");
Console.WriteLine($"(#155). That first {legacy.DeltaV / 1000:F1} km/s is already {legacy.DeltaV / k1.Total:F0}x the {k1.Total:F0} m/s the phasing closed form costs to close the same gap.");
Console.WriteLine();

// ===================================================================================
// Section B — the bus math, checked against the rails
// ===================================================================================
Console.WriteLine("=== Section B: the bus math, checked against the rails ===");
Console.WriteLine($"Kepler period at the ring radius {rDep / 1e9:F4}e9 m: {keplerPeriod:F1} s. Ringside's AUTHORED");
Console.WriteLine($"period: {ringside.OrbitPeriod:F1} s — {Math.Abs(ringside.OrbitPeriod - keplerPeriod) / keplerPeriod * 100:F3}% off Kepler. That tiny mismatch is the whole story below.");
Console.WriteLine();
Console.WriteLine("Closure check (dip family): the phasing identity is built on the KEPLER rate, so the");
Console.WriteLine("Kepler-closure residual is machine epsilon. The station rides the AUTHORED rail, so the");
Console.WriteLine("authored-closure residual drifts (n_auth - n_kepler)*k*T_ph per plan — growing with k.");
Console.WriteLine();
Console.WriteLine($"{"k",-4}{"family",-8}{"enter m/s",11}{"exit m/s",11}{"TOTAL m/s",11}{"wait d",9}{"res_Kepler",14}{"res_authored",14}");
Console.WriteLine(new string('-', 82));
(int k, bool dip, double total)? cheapest = null;
for (int fam = 0; fam < 2; fam++)
{
    bool dip = fam == 0;
    for (int k = 1; k <= 6; k++)
    {
        if (PricePhasing(k, dip) is not { } row)
        {
            Console.WriteLine($"{k,-4}{(dip ? "dip" : "swell"),-8}{"(no bound ellipse this way round)",47}");
            continue;
        }

        Console.WriteLine($"{k,-4}{(dip ? "dip" : "swell"),-8}{row.EnterDv,11:F2}{row.ExitDv,11:F2}{row.Total,11:F2}" +
            $"{row.WaitS / Day,9:F1}{row.ResKepler,14:E2}{row.ResAuthored,14:E2}");
        if (cheapest is null || row.Total < cheapest.Value.total)
        {
            cheapest = (k, dip, row.Total);
        }
    }
}

Console.WriteLine();
Console.WriteLine("Read the dip column top to bottom: the ENTER burn shrinks with k (a smaller period change");
Console.WriteLine("per lap), but the EXIT burn GROWS — every extra lap lets the authored-rail drift accumulate,");
Console.WriteLine($"so the total is a U: the cheapest bus is k={cheapest!.Value.k} ({(cheapest.Value.dip ? "dip" : "swell")}) at {cheapest.Value.total:F1} m/s, not k=1. If the rail");
Console.WriteLine("ran at its exact Kepler rate, exit would mirror enter and k=6 would win; the 0.025% authored");
Console.WriteLine("offset is what bends the sweet spot inward. That is the two-body lie showing up in algebra,");
Console.WriteLine("before a single step is flown.");
Console.WriteLine();

// ===================================================================================
// Section C — flown end to end through the real N-body sim
// ===================================================================================
Console.WriteLine("=== Section C: flown end to end, into the dock envelope ===");
var plan = TransferPlanner.Solve(sim, fullField,
    new TransferPlanner.Request(ship, "saturn", "ringside-exchange", MaxWaitSeconds: 0));

List<TrajectorySample> flownPath = [];
double flownMiss = double.NaN, flownRel = double.NaN;
if (!plan.Ok)
{
    Console.WriteLine($"planner refused: {plan.Failure}");
}
else
{
    Console.WriteLine($"planner winner: {plan.Summary}");
    Console.WriteLine($"  depart t+{(plan.DepartTime - t0) / 60:F0} min, {plan.Burns.Count} burns, wait {plan.TimeOfFlightSeconds / Day:F1} d, " +
        $"planned dv {plan.PlannedDeltaVTotal:F1} m/s, est. {plan.EstimatedPulses} pulses");
    Console.WriteLine();

    // Fly the two-burn schedule through the real integrator: coast to burn 1, apply it, coast the k
    // laps sampling the path, apply burn 2 at rendezvous, then read the miss and closing speed against
    // the target's true rail state — the honest verdict on the two-body plan.
    TransferPlanner.BurnStep b1 = plan.Burns[0];
    TransferPlanner.BurnStep b2 = plan.Burns[^1];
    ShipState atB1 = sim.RunAdaptive(ship, b1.SimTime - ship.SimTime);
    var afterB1 = atB1 with { Velocity = atB1.Velocity + b1.DeltaV };
    flownPath = [.. sim.ProjectAdaptive(afterB1, null, b2.SimTime - afterB1.SimTime, maxTimeStep: 1800, maxSamples: 20_000)];
    ShipState atB2 = sim.RunAdaptive(afterB1, b2.SimTime - afterB1.SimTime);
    var afterB2 = atB2 with { Velocity = atB2.Velocity + b2.DeltaV };

    Vector2d stationAtArrival = fullField.Position("ringside-exchange", afterB2.SimTime);
    Vector2d stationVelArrival = TransferMath.BodyVelocity(fullField, "ringside-exchange", afterB2.SimTime);
    flownMiss = (afterB2.Position - stationAtArrival).Length;
    flownRel = (afterB2.Velocity - stationVelArrival).Length;

    int p1 = OrbitRule.PulsesFor(b1.DeltaV.Length, atB1.Velocity.Length);
    int p2 = OrbitRule.PulsesFor(b2.DeltaV.Length, atB2.Velocity.Length);

    Console.WriteLine($"{"leg",-30}{"event",44}");
    Console.WriteLine($"{"burn 1 (enter phasing)",-30}{$"{b1.DeltaV.Length:F1} m/s at t+{(b1.SimTime - t0) / 60:F0} min, {p1} pulses",44}");
    Console.WriteLine($"{"coast k laps (ballistic)",-30}{$"{(b2.SimTime - b1.SimTime) / Day:F1} d in Saturn's well",44}");
    Console.WriteLine($"{"burn 2 (re-match at Ringside)",-30}{$"{b2.DeltaV.Length:F1} m/s at t+{(b2.SimTime - t0) / Day:F1} d, {p2} pulses",44}");
    Console.WriteLine();
    Console.WriteLine($"flown arrival vs dock envelope (coast within {DockEnvelopeMeters / 1e3:F0} km, close under {DockMatchSpeed / 1000:F0} km/s):");
    Console.WriteLine($"  miss distance   {flownMiss / 1e6,10:F2} Mm  ({(flownMiss <= DockEnvelopeMeters ? "INSIDE" : "outside")} the {DockEnvelopeMeters / 1e6:F0} Mm envelope, {DockEnvelopeMeters / flownMiss:F1}x margin)");
    Console.WriteLine($"  relative speed  {flownRel / 1000,10:F3} km/s ({(flownRel <= DockMatchSpeed ? "INSIDE" : "outside")} the {DockMatchSpeed / 1000:F0} km/s cap)");
    Console.WriteLine();
    Console.WriteLine($"The phasing closed the {ArcBehind / 1e3:F0} km ({ArcBehind / 1e6:F1} Mm) along-track gap that drifting alone never can — you");
    Console.WriteLine($"cannot catch a neighbour on your own lane by waiting. The two-body plan predicted a machine-");
    Console.WriteLine($"epsilon Kepler closure; flown through Saturn plus two moons and the sun's tide over {(b2.SimTime - b1.SimTime) / Day:F1} days it");
    Console.WriteLine($"lands {flownMiss / 1e6:F1} Mm out at {flownRel:F0} m/s matched — the lie is real but small, and the ship coasts into");
    Console.WriteLine($"the dock envelope. Docking stays the captain's click.");
}

Console.WriteLine();

// ===================================================================================
// Section D — the tactical table: cheaper vs sooner
// ===================================================================================
Console.WriteLine("=== Section D: the tactical table — cheaper vs sooner ===");
if (!plan.Ok)
{
    Console.WriteLine($"planner refused ({plan.Failure}) — no table to read.");
}
else
{
    Console.WriteLine($"{"row",-24}{"total dv (m/s)",16}{"pulses",9}{"wait (d)",11}{"arrival (d)",13}");
    Console.WriteLine(new string('-', 73));
    foreach (TransferPlanner.Alternative alt in plan.Alternatives)
    {
        Console.WriteLine($"{alt.Label,-24}{alt.DeltaVTotal,16:F1}{alt.EstimatedPulses,9}" +
            $"{alt.WaitSeconds / Day,11:F1}{(alt.ArrivalTime - t0) / Day,13:F1}");
    }

    Console.WriteLine();
    TransferPlanner.Alternative cheap = plan.Alternatives[0];
    TransferPlanner.Alternative fast = plan.Alternatives
        .OrderBy(a => a.ArrivalTime).ThenBy(a => a.DeltaVTotal).First();
    Console.WriteLine($"Cheapest: {cheap.Label} at {cheap.DeltaVTotal:F0} m/s, but you wait {cheap.WaitSeconds / Day:F1} days for it.");
    Console.WriteLine($"Soonest:  {fast.Label} arrives in {(fast.ArrivalTime - t0) / Day:F1} days for {fast.DeltaVTotal:F0} m/s ({fast.DeltaVTotal / cheap.DeltaVTotal:F1}x the fare, {(cheap.ArrivalTime - fast.ArrivalTime) / Day:F0} days sooner).");
    Console.WriteLine("Read it like a bus schedule with a wolf at the stop: no heat, ride the cheap bus and wait it");
    Console.WriteLine("out; heat on our tail, and the captain pays the fare to be gone sooner — same physics, different");
    Console.WriteLine("captain. The planner quotes the whole timetable instead of a single silent answer.");
}

// ===================================================================================
// --viz (optional): a Saturn-centric pocket scene. Gated behind LabViz.Wants, so no-flag stdout is
// byte-identical. The flown phasing loop is converted to Saturn-relative coordinates; a direct
// Lambert hop (when one solves) rides as a toggleable comparison group.
// ===================================================================================
if (LabViz.Wants(args))
{
    var viz = new VizScene("lab24-the-last-mile", "Lab 24 — The last mile",
        "92,640 km behind Ringside on the same lane — phasing, flown in Saturn's frame");
    viz.AddBodies(pocketOnly.Bodies);

    List<TrajectorySample> ToSaturnRelative(IEnumerable<TrajectorySample> samples) =>
        [.. samples.Select(s => new TrajectorySample(s.SimTime, s.Position - fullField.Position("saturn", s.SimTime)))];

    // The legacy point-and-throttle chase as a toggleable comparison group.
    viz.AddPath("legacy chase (point & throttle)", ToSaturnRelative(legacy.Path), VizColors.Sweep, "legacy", 1.2, 0.55);

    if (plan.Ok && flownPath.Count > 0)
    {
        TransferPlanner.BurnStep b1 = plan.Burns[0];
        TransferPlanner.BurnStep b2 = plan.Burns[^1];
        List<TrajectorySample> relPhasing = ToSaturnRelative(flownPath);
        viz.AddPath("phasing loop (k laps)", relPhasing, VizColors.Trajectory, "main", 1.8, 1.0, ghost: true);

        // A direct co-orbital Lambert hop, drawn as a toggleable "what the porkchop tries" group when
        // one solves — a modest TOF straight across the gap, the arc Lambert can only price off the
        // 2pi singularity.
        double directTof = 0.5 * ringside.OrbitPeriod;
        Vector2d satAtDep = fullField.Position("saturn", tDep);
        Vector2d satAtArr = fullField.Position("saturn", tDep + directTof);
        Vector2d fromRel = shipAtDep.Position - satAtDep;
        Vector2d toRel = fullField.Position("ringside-exchange", tDep + directTof) - satAtArr;
        if (TransferMath.Lambert(fromRel, toRel, directTof, SaturnMu) is { } direct)
        {
            var directShip = new ShipState(shipAtDep.Position, parentVelDep + direct.V1, tDep);
            var directPath = sim.ProjectAdaptive(directShip, null, directTof, maxTimeStep: 1800, maxSamples: 20_000);
            viz.AddPath("direct hop (Lambert)", ToSaturnRelative(directPath), VizColors.Ship, "direct", 1.4, 0.7);
        }

        Vector2d burn1Rel = relPhasing[0].Position;
        Vector2d rdvRel = fullField.Position("ringside-exchange", b2.SimTime) - fullField.Position("saturn", b2.SimTime);
        viz.AddMarker(b1.SimTime, burn1Rel, $"enter phasing ({b1.DeltaV.Length:F0} m/s)", MarkerKinds.Burn);
        viz.AddMarker(b2.SimTime, rdvRel, $"re-match at Ringside ({b2.DeltaV.Length:F0} m/s)", MarkerKinds.Event);
    }

    LabViz.Show(viz, args);
}
