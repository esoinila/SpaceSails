// Lab 05 — Transfers without formulas
//
// Teaching voice: Curtis ch. 6 solves Earth-Mars with three closed-form numbers (two burns, one
// flight time) because it assumes coplanar circular orbits and a free choice of departure time
// — and `scenarios/sol.json` literally IS that idealization (real radii and periods, circular,
// coplanar). So the formula should be dead-on here. What does the game's own planner
// (`RoutePlanner`, a grid search over the game's own `Simulator`) buy on top of a formula that's
// already exact for this solar system? This lesson computes both on the same Earth-Mars pair,
// then finds one case — a departure day you don't get to choose — where the search visibly
// earns its keep, and one break-it where shrinking the search grid makes the plan visibly worse.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code, the
// printed numbers in labs/05-transfers-without-formulas/README.md go stale — rerun and re-paste.

using SpaceSails.Core;

const double Day = 86400.0;
const double AU = 1.496e11;

// Real sol.json numbers (mu in m^3/s^2, orbit radius/period in m/s).
const double SunMu = 1.32712440018e20;
const double SunBodyRadius = 6.9634e8;
const double EarthMu = 3.986004418e14, EarthOrbitRadius = AU, EarthPeriod = 3.1558149e7, EarthPhase = 1.8, EarthBodyRadius = 6.371e6;
const double VenusMu = 3.24859e14, VenusOrbitRadius = 1.0821e11, VenusPeriod = 1.94142e7, VenusPhase = 0.9, VenusBodyRadius = 6.0518e6;
const double MarsMu = 4.282837e13, MarsOrbitRadius = 2.2794e11, MarsPeriod = 5.93551e7, MarsPhase = 2.7, MarsBodyRadius = 3.3895e6;

var sun = new CelestialBody("sun", "Sun", null, SunMu, SunBodyRadius, 0, 0, 0);
var earth = new CelestialBody("earth", "Earth", "sun", EarthMu, EarthBodyRadius, EarthOrbitRadius, EarthPeriod, EarthPhase);
var venus = new CelestialBody("venus", "Venus", "sun", VenusMu, VenusBodyRadius, VenusOrbitRadius, VenusPeriod, VenusPhase);
var mars = new CelestialBody("mars", "Mars", "sun", MarsMu, MarsBodyRadius, MarsOrbitRadius, MarsPeriod, MarsPhase);
var ephemeris = new CircularOrbitEphemeris([sun, earth, venus, mars]);

// ===================================================================================
// Section A — the Hohmann analytic transfer (Curtis ch. 6)
// ===================================================================================
Console.WriteLine("=== Section A: Earth -> Mars, the textbook Hohmann transfer ===");

(double dv1, double dv2, double tof, double v1, double v2, double vt1, double vt2) Hohmann(double r1, double r2, double mu)
{
    double aT = (r1 + r2) / 2.0;
    double v1c = Math.Sqrt(mu / r1);
    double v2c = Math.Sqrt(mu / r2);
    double vt1x = Math.Sqrt(mu * (2.0 / r1 - 1.0 / aT));
    double vt2x = Math.Sqrt(mu * (2.0 / r2 - 1.0 / aT));
    double dv1x = Math.Abs(vt1x - v1c);
    double dv2x = Math.Abs(v2c - vt2x);
    double tofx = Math.PI * Math.Sqrt(aT * aT * aT / mu);
    return (dv1x, dv2x, tofx, v1c, v2c, vt1x, vt2x);
}

var hohmannEM = Hohmann(EarthOrbitRadius, MarsOrbitRadius, SunMu);
Console.WriteLine($"r1 (Earth) = {EarthOrbitRadius / AU:F4} AU, r2 (Mars) = {MarsOrbitRadius / AU:F4} AU");
Console.WriteLine($"v_circ(Earth) = {hohmannEM.v1:F3} m/s, v_circ(Mars) = {hohmannEM.v2:F3} m/s");
Console.WriteLine($"transfer ellipse speed at departure = {hohmannEM.vt1:F3} m/s, at arrival = {hohmannEM.vt2:F3} m/s");
Console.WriteLine($"dv1 (departure burn)  = {hohmannEM.dv1:F3} m/s");
Console.WriteLine($"dv2 (arrival burn)    = {hohmannEM.dv2:F3} m/s");
Console.WriteLine($"total analytic dv     = {hohmannEM.dv1 + hohmannEM.dv2:F3} m/s");
Console.WriteLine($"transfer time         = {hohmannEM.tof:F0} s ({hohmannEM.tof / Day:F2} days)");
Console.WriteLine();
Console.WriteLine("This is exact for the two-body, coplanar, circular idealization — and sol.json literally IS");
Console.WriteLine("that idealization (real radii/periods, circular, coplanar orbits). No integration, no search:");
Console.WriteLine("three numbers from algebra. What it does NOT know about: which day you're allowed to leave,");
Console.WriteLine("that real thrust comes in +-10% pulses (lesson 4), or that NPC captains fly differently.");
Console.WriteLine();

// ===================================================================================
// Section B — RoutePlanner's grid search, same pair, real departure scan
// ===================================================================================
Console.WriteLine("=== Section B: RoutePlanner's grid search, Earth -> Mars, scanned over departure day ===");

double PlanDeltaV(Simulator sim, ShipState departure, ManeuverPlan plan)
{
    double total = 0;
    foreach (ManeuverNode node in plan.Nodes)
    {
        ShipState pre = sim.RunAdaptive(departure, node.SimTime - departure.SimTime, plan);
        double speedBefore = pre.Velocity.Length;
        double perPulse = node.Action == ManeuverAction.Accelerate
            ? 1.0 + node.EffectivePercent / 100.0
            : 1.0 - node.EffectivePercent / 100.0;
        double scale = Math.Pow(perPulse, node.Pulses);
        total += speedBefore * Math.Abs(scale - 1.0);
    }

    return total;
}

var flightSim = new Simulator(ephemeris, timeStepSeconds: 3600.0);
double synodicEM = 1.0 / Math.Abs(1.0 / EarthPeriod - 1.0 / MarsPeriod); // ~779.9 days
Console.WriteLine($"Earth-Mars synodic period = {synodicEM / Day:F1} days — scanning one full synodic cycle of departure days:");
Console.WriteLine();

var scanResults = new List<(double departureDay, int pulses, double missKm, double flightDays)>();
Console.WriteLine($"{"departure (day)",-18}{"pulses",-9}{"miss (km)",-16}{"flight (days)",-14}");
for (double depDay = 0; depDay <= synodicEM / Day; depDay += 40)
{
    double departureTime = depDay * Day;
    var rng = new DeterministicRandom(7);
    NpcRoute route = RoutePlanner.PlanRoute(ephemeris, "earth", "mars", departureTime, RoutePersonality.Economical, rng);
    int pulses = route.Plan.Nodes[0].Pulses;
    double flightDays = (route.EstimatedArrivalTime - departureTime) / Day;
    scanResults.Add((depDay, pulses, route.EstimatedMissDistance / 1000.0, flightDays));
    Console.WriteLine($"{depDay,-18:F0}{pulses,-9}{route.EstimatedMissDistance / 1000.0,-16:F1}{flightDays,-14:F1}");
}
Console.WriteLine();
Console.WriteLine("Rows with flight = 0.0 and a miss in the hundred-million-km range are real, not a display bug:");
Console.WriteLine("on those departure days, none of the Economical grid's burn sizes (4/6/8/10 pulses) ever get the");
Console.WriteLine("ship closer to Mars than its OWN starting distance within the search horizon — the distance climbs");
Console.WriteLine("monotonically from the first sample, so the search's own 'closest point so far' logic reports the");
Console.WriteLine("departure instant itself. Departure timing matters even for a formula-friendly pair like this one.");
Console.WriteLine();

var best = scanResults.OrderBy(r => r.missKm).ThenBy(r => r.flightDays).First();
Console.WriteLine($"Best departure day in the scan: day {best.departureDay:F0} " +
    $"({best.pulses} pulses, miss {best.missKm:F1} km, flight {best.flightDays:F1} days)");

var bestRng = new DeterministicRandom(7);
NpcRoute bestRoute = RoutePlanner.PlanRoute(ephemeris, "earth", "mars", best.departureDay * Day, RoutePersonality.Economical, bestRng);
double searchDv = PlanDeltaV(flightSim, bestRoute.DepartureState, bestRoute.Plan);
Console.WriteLine();
Console.WriteLine("Comparing the SAME pair, side by side:");
Console.WriteLine($"{"",-24}{"dv-equivalent (m/s)",-22}{"flight time (days)",-20}{"arrival miss (km)",-18}");
Console.WriteLine($"{"Hohmann analytic",-24}{hohmannEM.dv1 + hohmannEM.dv2,-22:F3}{hohmannEM.tof / Day,-20:F2}{"(by definition) 0",-18}");
Console.WriteLine($"{"RoutePlanner search",-24}{searchDv,-22:F3}{best.flightDays,-20:F1}{best.missKm,-18:F1}");
Console.WriteLine();
Console.WriteLine("The formula and the search land in the same ballpark on dv, because sol.json's Earth-Mars pair");
Console.WriteLine("really is the idealization Curtis assumes. The search's real value is everything the formula");
Console.WriteLine("doesn't model: it only offers a HANDFUL of discrete +-10%-pulse burn sizes (candidates 4/6/8/10),");
Console.WriteLine("it only trusts whichever departure day you actually give it, and it reports a real arrival MISS");
Console.WriteLine("distance (never exactly 0) because the burn sizes are quantized (lesson 4) — the formula's dv1/");
Console.WriteLine("dv2 are numbers no pulse combination can fire exactly.");
Console.WriteLine();

// ===================================================================================
// Section C — where search shines: Earth -> Venus on a departure day you don't get to pick
// ===================================================================================
Console.WriteLine("=== Section C: Earth -> Venus, a departure day the captain doesn't get to choose ===");

var hohmannEV = Hohmann(EarthOrbitRadius, VenusOrbitRadius, SunMu);
Console.WriteLine($"Hohmann analytic (idealized, any departure day): dv1 = {hohmannEV.dv1:F3} m/s, " +
    $"dv2 = {hohmannEV.dv2:F3} m/s, total = {hohmannEV.dv1 + hohmannEV.dv2:F3} m/s, " +
    $"transfer time = {hohmannEV.tof / Day:F2} days");
Console.WriteLine("This number is the same regardless of which day you leave — the formula has no notion of a");
Console.WriteLine("'window.' A real captain is handed one day (a cargo contract, a duty roster) and has to fly");
Console.WriteLine("from wherever Venus actually is that day, not from the idealized alignment Curtis assumes.");
Console.WriteLine();

const double constrainedDay = 0.0; // "the captain's actual orders": leave at t=0, no choosing
var constrainedRng = new DeterministicRandom(7);
NpcRoute constrained = RoutePlanner.PlanRoute(ephemeris, "earth", "venus", constrainedDay, RoutePersonality.Economical, constrainedRng);
double constrainedDv = PlanDeltaV(flightSim, constrained.DepartureState, constrained.Plan);
Console.WriteLine($"Constrained to day {constrainedDay:F0}: RoutePlanner still finds a route — " +
    $"{constrained.Plan.Nodes[0].Pulses} pulses, dv-equivalent {constrainedDv:F3} m/s, " +
    $"flight {((constrained.EstimatedArrivalTime - constrainedDay * Day) / Day):F1} days, " +
    $"miss {constrained.EstimatedMissDistance / 1000.0:F1} km");

// Now let the search choose its own day too, scanning the Earth-Venus synodic period.
double synodicEV = 1.0 / Math.Abs(1.0 / EarthPeriod - 1.0 / VenusPeriod);
var venusScan = new List<(double departureDay, int pulses, double missKm, double flightDays)>();
for (double depDay = 0; depDay <= synodicEV / Day; depDay += 20)
{
    var rng = new DeterministicRandom(7);
    NpcRoute route = RoutePlanner.PlanRoute(ephemeris, "earth", "venus", depDay * Day, RoutePersonality.Economical, rng);
    venusScan.Add((depDay, route.Plan.Nodes[0].Pulses, route.EstimatedMissDistance / 1000.0, (route.EstimatedArrivalTime - depDay * Day) / Day));
}

var bestVenus = venusScan.OrderBy(r => r.missKm).ThenBy(r => r.flightDays).First();
Console.WriteLine($"Search allowed to pick its own day (scanning a full {synodicEV / Day:F0}-day synodic cycle): " +
    $"best is day {bestVenus.departureDay:F0} ({bestVenus.pulses} pulses, miss {bestVenus.missKm:F1} km, " +
    $"flight {bestVenus.flightDays:F1} days)");
Console.WriteLine("The formula can't tell you this at all — it has no departure-day axis. The search can be handed");
Console.WriteLine("EITHER a free choice (and finds the cheap window) OR a fixed order (and still returns a flyable,");
Console.WriteLine("if worse, plan) — that flexibility, not raw accuracy, is what a fixed-form Hohmann formula can't do.");
Console.WriteLine();

// ===================================================================================
// Break it — shrink the search grid
// ===================================================================================
Console.WriteLine("=== BREAK IT: shrink the search grid ===");

(int bestPulses, double bestMissKm, double bestArrivalDays) SearchWithGrid(
    string originId, string destId, double departureTime, int[] candidates, double horizon)
{
    ShipState departure = RoutePlanner.DepartureState(ephemeris, originId, destId, departureTime);
    double originR = ephemeris.Bodies.First(b => b.Id == originId).OrbitRadius;
    double destR = ephemeris.Bodies.First(b => b.Id == destId).OrbitRadius;
    bool inward = destR < originR;
    ManeuverAction burn = inward ? ManeuverAction.Decelerate : ManeuverAction.Accelerate;
    var sim = new Simulator(ephemeris, timeStepSeconds: 86400); // same coarse dt RoutePlanner's own search uses

    double bestMiss = double.MaxValue;
    int bestPulses = candidates[0];
    double bestArrival = departureTime + horizon;
    foreach (int pulses in candidates)
    {
        var plan = new ManeuverPlan([new ManeuverNode(departureTime + 3600, burn, pulses)]);
        IReadOnlyList<TrajectorySample> samples = sim.ProjectAdaptive(
            departure, plan, horizon, minTimeStep: 86400, maxTimeStep: 86400, maxSamples: 100_000);
        double miss = double.MaxValue, arrival = departureTime + horizon;
        foreach (TrajectorySample sample in samples)
        {
            double d = (ephemeris.Position(destId, sample.SimTime) - sample.Position).Length;
            if (d < miss) { (miss, arrival) = (d, sample.SimTime); }
        }

        if (miss < bestMiss)
        {
            (bestMiss, bestPulses, bestArrival) = (miss, pulses, arrival);
        }
    }

    return (bestPulses, bestMiss / 1000.0, (bestArrival - departureTime) / Day);
}

double horizonEM = 2 * Math.Tau * Math.Sqrt(EarthOrbitRadius * EarthOrbitRadius * EarthOrbitRadius / SunMu);
Console.WriteLine("Same Earth -> Mars departure (day 0) with progressively smaller candidate grids:");
Console.WriteLine($"{"grid",-28}{"pulses picked",-16}{"miss (km)",-14}{"flight (days)",-14}");
int[][] grids = [[4, 6, 8, 10], [4], [6], [8], [10], [12, 16, 20]];
var gridResults = new List<(string label, int pulses, double missKm, double flightDays)>();
foreach (int[] grid in grids)
{
    var (pulses, missKm, flightDays) = SearchWithGrid("earth", "mars", 0, grid, horizonEM);
    string label = string.Join(",", grid);
    gridResults.Add((label, pulses, missKm, flightDays));
    Console.WriteLine($"{label,-28}{pulses,-16}{missKm,-14:F1}{flightDays,-14:F1}");
}
Console.WriteLine();
double fullGridMiss = gridResults[0].missKm;
var worstSingle = gridResults.Skip(1).Take(4).OrderByDescending(r => r.missKm).First();
Console.WriteLine($"The real Economical grid [4,6,8,10] picks the best of its four candidates: {fullGridMiss:F1} km miss.");
Console.WriteLine($"Reduce it to a SINGLE candidate and the result depends entirely on whether that one guess suits");
Console.WriteLine($"this transfer — grid [{worstSingle.label}] alone misses by {worstSingle.missKm:F1} km, " +
    $"{worstSingle.missKm / fullGridMiss:F1}x worse than trusting all four. A single-candidate grid isn't a smaller");
Console.WriteLine($"version of the search — it's a guess with no fallback if that guess is wrong for this transfer.");
