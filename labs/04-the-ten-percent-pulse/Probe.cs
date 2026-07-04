// Lab 04 — The ten percent pulse
//
// Teaching voice: this game has exactly one propulsion primitive — scale the ship's velocity
// vector by 1.1 or by 0.9 (ManeuverPlan.AccelerateFactor / DecelerateFactor), plus a "Fine"
// variant at 1.01 / 0.99, plus (M16c) a free-double Percent per node. There is no "burn this
// many m/s" API anywhere in SpaceSails.Core — every speed change is a MULTIPLICATIVE pulse.
// That single fact has consequences worth computing rather than hand-waving:
//   (a) from one starting speed, only a discrete LATTICE of speeds is reachable, not a
//       continuum — quantization, straight up;
//   (b) the same pulse count spent at periapsis of an eccentric orbit buys a different final
//       orbit than spent at apoapsis — the Oberth effect (Curtis ch. 6), measured in real
//       specific-energy numbers, not asserted;
//   (c) circularizing after a transfer costs a real, countable number of pulses, and the
//       leftover mismatch after the last pulse is exactly the quantization residual from (a) —
//       this repurposes the repo's own M16 finding (commit cef2ac0: arriving at Mercury from
//       Earth crosses at 57.5 km/s against a 47.9 km/s circular speed there; 2 decelerate pulses
///      + a fine trim held the ship in [0.93, 1.00] x Mercury's orbit radius for 120 days with
//       zero further thrust) and reproduces the same style of check for Saturn.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code, the
// printed numbers in labs/04-the-ten-percent-pulse/README.md go stale — rerun and re-paste.

using SpaceSails.Core;

const double Day = 86400.0;
const double AU = 1.496e11; // m (scenarios/sol.json "earth" orbitRadiusM)

// Real sol.json numbers (mu in m^3/s^2, orbit radius in m) — same Sun/body constants every
// other lesson uses, plus the destination bodies this lesson needs.
const double SunMu = 1.32712440018e20;
const double SunBodyRadius = 6.9634e8;
const double MercuryMu = 2.2032e13;
const double MercuryOrbitRadius = 5.791e10;
const double MercuryBodyRadius = 2.4397e6;
const double SaturnMu = 3.7931187e16;
const double SaturnOrbitRadius = 1.43353e12;
const double SaturnBodyRadius = 5.8232e7;
const double EarthOrbitRadius = AU;
const double MarsOrbitRadius = 2.2794e11;

var sun = new CelestialBody("sun", "Sun", null, SunMu, SunBodyRadius, 0, 0, 0);
var mercury = new CelestialBody("mercury", "Mercury", "sun", MercuryMu, MercuryBodyRadius, MercuryOrbitRadius, 7.60052e6, 0.0);
var earth = new CelestialBody("earth", "Earth", "sun", 3.986004418e14, 6.371e6, EarthOrbitRadius, 3.1558149e7, 1.8);
var saturn = new CelestialBody("saturn", "Saturn", "sun", SaturnMu, SaturnBodyRadius, SaturnOrbitRadius, 9.29596e8, 4.5);
var ephemeris = new CircularOrbitEphemeris([sun, mercury, earth, saturn]);

double CircSpeed(double mu, double r) => Math.Sqrt(mu / r);

// ===================================================================================
// Section A — the reachable speed lattice
// ===================================================================================
Console.WriteLine("=== Section A: the reachable speed lattice ===");
Console.WriteLine("Starting speed v0 = Earth's circular solar speed (real sol.json Sun mu, Earth orbit radius):");
double v0 = CircSpeed(SunMu, EarthOrbitRadius);
Console.WriteLine($"v0 = {v0:F3} m/s");
Console.WriteLine();

// Every reachable multiplier using AT MOST maxTotal coarse (+-10%) pulses, any mix of
// accelerate (j) and decelerate (k) pulses. Order doesn't matter — scalar multiplication
// commutes — so the multiplier only depends on the COUNTS (j, k), not the firing order.
const int maxTotal = 6;
var lattice = new SortedSet<double>();
var byPulseCount = new List<(int j, int k, double mult)>();
for (int j = 0; j <= maxTotal; j++)
{
    for (int k = 0; k <= maxTotal - j; k++)
    {
        double mult = Math.Pow(1.1, j) * Math.Pow(0.9, k);
        lattice.Add(Math.Round(mult, 9));
        byPulseCount.Add((j, k, mult));
    }
}

Console.WriteLine($"With <= {maxTotal} total coarse (+-10%) pulses (any mix of accelerate/decelerate):");
Console.WriteLine($"  distinct combinations (j,k) tried: {byPulseCount.Count}");
Console.WriteLine($"  distinct reachable multipliers:    {lattice.Count}");
double[] sortedLattice = [.. lattice];
double minGap = double.MaxValue;
for (int i = 1; i < sortedLattice.Length; i++)
{
    minGap = Math.Min(minGap, sortedLattice[i] - sortedLattice[i - 1]);
}
Console.WriteLine($"  tightest neighboring gap in the lattice: {minGap:E6} (multiplier units)");
Console.WriteLine();

Console.WriteLine("Pure accelerate-only chain (j pulses, k=0) — the speeds after j consecutive +10% pulses:");
Console.WriteLine($"{"j pulses",-10}{"multiplier",-14}{"speed (m/s)",-16}");
for (int j = 0; j <= maxTotal; j++)
{
    double mult = Math.Pow(1.1, j);
    Console.WriteLine($"{j,-10}{mult,-14:F6}{v0 * mult,-16:F3}");
}
Console.WriteLine();

// A physically meaningful target: the ratio of Mars's circular solar speed to Earth's. No
// combination of whole +-10% pulses should be expected to land on it exactly.
double targetRatio = CircSpeed(SunMu, MarsOrbitRadius) / CircSpeed(SunMu, EarthOrbitRadius);
Console.WriteLine($"Target: v_circ(Mars)/v_circ(Earth) = {targetRatio:F6} (a real orbital-speed ratio, not a round number)");

(int bestJ, int bestK, double bestMult) = (0, 0, 1.0);
double bestResidual = double.MaxValue;
foreach ((int j, int k, double mult) in byPulseCount)
{
    double residual = Math.Abs(mult - targetRatio);
    if (residual < bestResidual)
    {
        (bestJ, bestK, bestMult, bestResidual) = (j, k, mult, residual);
    }
}

Console.WriteLine($"Best <= {maxTotal}-pulse coarse-only combo: {bestJ} accelerate + {bestK} decelerate " +
    $"= x{bestMult:F6} (target x{targetRatio:F6}), residual {bestResidual / targetRatio:P4}");

// Now add fine (+-1%) trim on top of the best coarse combo found above.
(int bestM, double bestFineMult) = (0, bestMult);
double bestFineResidual = bestResidual;
for (int m = -30; m <= 30; m++)
{
    double fineFactor = m >= 0 ? Math.Pow(1.01, m) : Math.Pow(0.99, -m);
    double mult = bestMult * fineFactor;
    double residual = Math.Abs(mult - targetRatio);
    if (residual < bestFineResidual)
    {
        (bestM, bestFineMult, bestFineResidual) = (m, mult, residual);
    }
}

Console.WriteLine($"Same combo + {bestM} fine (+-1%) trim pulses: x{bestFineMult:F6}, " +
    $"residual {bestFineResidual / targetRatio:P6}");
Console.WriteLine("Fine trim shrinks the residual by roughly the coarse-to-fine step-size ratio (~10x) but never");
Console.WriteLine("zeroes it: see the break-it at the end of this probe for why.");
Console.WriteLine();

// ===================================================================================
// Section B — the Oberth effect, measured with real pulses
// ===================================================================================
Console.WriteLine("=== Section B: the Oberth effect (Curtis ch. 6) ===");
double rp = 0.5 * AU, ra = 1.5 * AU;
double a0 = (rp + ra) / 2.0;
double e0 = (ra - rp) / (ra + rp);
double energy0 = -SunMu / (2 * a0);
double vp = Math.Sqrt(SunMu * (2.0 / rp - 1.0 / a0));
double va = Math.Sqrt(SunMu * (2.0 / ra - 1.0 / a0));
double period0 = Math.Tau * Math.Sqrt(a0 * a0 * a0 / SunMu);

Console.WriteLine($"Starting solar orbit: periapsis {rp / AU:F3} AU, apoapsis {ra / AU:F3} AU, " +
    $"e = {e0:F6}, a = {a0 / AU:F3} AU, period = {period0 / Day:F3} days");
Console.WriteLine($"vis-viva: v_periapsis = {vp:F3} m/s, v_apoapsis = {va:F3} m/s, specific energy = {energy0:E6} J/kg");
Console.WriteLine();

var oberthEphemeris = new CircularOrbitEphemeris([sun]);
var coastSim = new Simulator(oberthEphemeris, timeStepSeconds: 60.0);
var burnSim = new Simulator(oberthEphemeris, timeStepSeconds: 1.0); // fine dt so the burn window is essentially instantaneous

var atPeriapsis = new ShipState(new Vector2d(rp, 0), new Vector2d(0, vp), 0);

// Sanity check: coast the game's own integrator from periapsis to apoapsis (half period) and
// confirm it lands on the closed-form ra/va — this is the SAME integrator the burns below use.
ShipState atApoapsisCoasted = coastSim.Run(atPeriapsis, period0 / 2.0);
Console.WriteLine("Sanity check — coasting from periapsis to apoapsis with the game's own Simulator:");
Console.WriteLine($"  computed apoapsis radius = {atApoapsisCoasted.Position.Length / AU:F6} AU (closed form {ra / AU:F6})");
Console.WriteLine($"  computed apoapsis speed  = {atApoapsisCoasted.Velocity.Length:F3} m/s (closed form {va:F3})");
Console.WriteLine();

const int oberthPulses = 2;
double scale = Math.Pow(ManeuverPlan.AccelerateFactor, oberthPulses);
Console.WriteLine($"Same maneuver spent at each extreme: {oberthPulses} accelerate pulses (x{scale:F4} total)");
Console.WriteLine();

(double energyAfter, double aNew, double eNew, double periNew, double apoNew, bool boundOrbit) MeasureBurn(ShipState before)
{
    var burnPlan = new ManeuverPlan([new ManeuverNode(before.SimTime, ManeuverAction.Accelerate, oberthPulses)]);
    ShipState after = burnSim.Step(before, burnPlan);
    double r = after.Position.Length;
    double v = after.Velocity.Length;
    double h = after.Position.X * after.Velocity.Y - after.Position.Y * after.Velocity.X;
    double energy = v * v / 2.0 - SunMu / r;
    bool bound = energy < 0;
    double a = bound ? -SunMu / (2 * energy) : double.NaN;
    double e = bound ? Math.Sqrt(1.0 + 2.0 * energy * h * h / (SunMu * SunMu)) : double.NaN;
    double peri = bound ? a * (1 - e) : double.NaN;
    double apo = bound ? a * (1 + e) : double.NaN;
    return (energy, a, e, peri, apo, bound);
}

var periBurn = MeasureBurn(atPeriapsis);
var apoBurn = MeasureBurn(atApoapsisCoasted);

Console.WriteLine($"{"burn location",-16}{"new energy (J/kg)",-20}{"new a (AU)",-14}{"new e",-12}{"new peri (AU)",-16}{"new apo (AU)",-14}");
void PrintBurn(string label, (double energyAfter, double aNew, double eNew, double periNew, double apoNew, bool boundOrbit) r)
{
    string aStr = r.boundOrbit ? (r.aNew / AU).ToString("F6") : "unbound";
    string eStr = r.boundOrbit ? r.eNew.ToString("F6") : "-";
    string periStr = r.boundOrbit ? (r.periNew / AU).ToString("F6") : "-";
    string apoStr = r.boundOrbit ? (r.apoNew / AU).ToString("F6") : "-";
    Console.WriteLine($"{label,-16}{r.energyAfter,-20:E6}{aStr,-14}{eStr,-12}{periStr,-16}{apoStr,-14}");
}
PrintBurn("periapsis", periBurn);
PrintBurn("apoapsis", apoBurn);
Console.WriteLine();

double energyGainPeri = periBurn.energyAfter - energy0;
double energyGainApo = apoBurn.energyAfter - energy0;
Console.WriteLine($"specific energy gain (J/kg): periapsis burn = {energyGainPeri:E6}, apoapsis burn = {energyGainApo:E6}");
Console.WriteLine($"periapsis burn gains {energyGainPeri / energyGainApo:F3}x the specific energy of the identical " +
    $"burn at apoapsis — same fuel (same pulse count), very different orbit. That ratio is v_p^2/v_a^2 to first " +
    $"order ({(vp * vp) / (va * va):F3}) because Delta-E ~ v * Delta-v, and Delta-v is the same fraction of v either way.");
Console.WriteLine();

// ===================================================================================
// Section C — pulse cost of circularizing at Mercury vs Saturn (echoes the repo's M16 finding:
// commit cef2ac0, "shed the difference once (2x -10% + a trim) and the ship holds
// [0.93, 1.00] x rM for 120 days with ZERO further thrust")
// ===================================================================================
Console.WriteLine("=== Section C: circularizing at Mercury vs Saturn, from the same transfer shape ===");

var flightEphemeris = new CircularOrbitEphemeris([sun]);
var flightSim = new Simulator(flightEphemeris, timeStepSeconds: 3600.0);
var fineBurnSim = new Simulator(flightEphemeris, timeStepSeconds: 1.0);

(int coarse, int fine, double achieved) FindCircularizingCombo(double ratio, bool ratioAboveOne)
{
    double coarseFactor = ratioAboveOne ? ManeuverPlan.DecelerateFactor : ManeuverPlan.AccelerateFactor;
    double fineFactor = ratioAboveOne ? ManeuverPlan.FineDecelerateFactor : ManeuverPlan.FineAccelerateFactor;
    (int bestC, int bestF, double bestAchieved) = (0, 0, ratio);
    double bestErr = Math.Abs(ratio - 1.0);
    for (int c = 0; c <= 10; c++)
    {
        double afterCoarse = ratio * Math.Pow(coarseFactor, c);
        for (int f = 0; f <= 30; f++)
        {
            double achieved = afterCoarse * Math.Pow(fineFactor, f);
            double err = Math.Abs(achieved - 1.0);
            if (err < bestErr)
            {
                (bestC, bestF, bestAchieved, bestErr) = (c, f, achieved, err);
            }
        }
    }
    return (bestC, bestF, bestAchieved);
}

// Same transfer shape as M16b: one whole-pulse burn at Earth's orbit (decelerate to fall
// inward, accelerate to climb outward). Earth's departure radius becomes the OTHER apsis of
// the new ellipse, so the burn count that lands the transfer's periapsis (inward) or apoapsis
// (outward) closest to the destination's orbit radius is a small integer search — no RNG, no
// planner miss-tolerance, just the same pulse primitive lesson 1's break-it #2 already used.
(int pulses, double apsisRadius, ManeuverAction action) BestTransferBurn(double destOrbitRadius, bool inward)
{
    ManeuverAction action = inward ? ManeuverAction.Decelerate : ManeuverAction.Accelerate;
    double factor = inward ? ManeuverPlan.DecelerateFactor : ManeuverPlan.AccelerateFactor;
    (int bestN, double bestApsis) = (1, double.MaxValue);
    double bestErr = double.MaxValue;
    for (int n = 1; n <= 15; n++)
    {
        double vAfter = v0 * Math.Pow(factor, n);
        double aNew = 1.0 / (2.0 / EarthOrbitRadius - vAfter * vAfter / SunMu);
        double otherApsis = 2 * aNew - EarthOrbitRadius;
        double err = Math.Abs(otherApsis - destOrbitRadius);
        if (otherApsis > 0 && err < bestErr)
        {
            (bestN, bestApsis, bestErr) = (n, otherApsis, err);
        }
    }
    return (bestN, bestApsis, action);
}

void ReportCircularization(string name, double destOrbitRadius, bool inward)
{
    (int pulses, double apsisRadius, ManeuverAction action) = BestTransferBurn(destOrbitRadius, inward);
    Console.WriteLine($"--- {name} ---");
    Console.WriteLine($"transfer burn: {pulses} {action} pulses at Earth's orbit -> new " +
        $"{(inward ? "periapsis" : "apoapsis")} = {apsisRadius / AU:F4} AU (target {destOrbitRadius / AU:F4} AU)");

    var departure = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, v0), 0);
    var burnPlan = new ManeuverPlan([new ManeuverNode(0, action, pulses)]);
    ShipState justAfterBurn = fineBurnSim.Step(departure, burnPlan); // 1 s, essentially instantaneous

    double vAfterBurn = v0 * Math.Pow(inward ? ManeuverPlan.DecelerateFactor : ManeuverPlan.AccelerateFactor, pulses);
    double aNew = 1.0 / (2.0 / EarthOrbitRadius - vAfterBurn * vAfterBurn / SunMu);
    double periodNew = Math.Tau * Math.Sqrt(aNew * aNew * aNew / SunMu);
    double halfPeriod = periodNew / 2.0;

    // The burn point IS the other apsis of the new ellipse, so exactly half its period later the
    // ship is at the periapsis/apoapsis we solved for above — coast there with the real integrator.
    ShipState crossing = flightSim.RunAdaptive(justAfterBurn, halfPeriod - justAfterBurn.SimTime, null);
    double crossingSpeed = crossing.Velocity.Length;
    double crossingRadius = crossing.Position.Length;
    double circularThere = CircSpeed(SunMu, crossingRadius);
    double ratio = crossingSpeed / circularThere;
    bool tooFast = ratio > 1.0;

    Console.WriteLine($"coasted (game's own adaptive integrator) to the apsis at sim time {crossing.SimTime / Day:F2} days: " +
        $"radius {crossingRadius / AU:F4} AU (closed form {apsisRadius / AU:F4} AU)");
    Console.WriteLine($"crossing speed = {crossingSpeed / 1000.0:F3} km/s (circular there = {circularThere / 1000.0:F3} km/s, " +
        $"ratio = {ratio:F6})");

    (int coarse, int fine, double achieved) = FindCircularizingCombo(ratio, tooFast);
    ManeuverAction trimAction = tooFast ? ManeuverAction.Decelerate : ManeuverAction.Accelerate;
    Console.WriteLine($"circularizing combo: {coarse} {trimAction} pulses + {fine} fine trim pulses -> ratio {achieved:F6} " +
        $"(residual {Math.Abs(achieved - 1.0):E6})");

    var trimNodes = new List<ManeuverNode>();
    if (coarse > 0) { trimNodes.Add(new ManeuverNode(crossing.SimTime, trimAction, coarse)); }
    if (fine > 0) { trimNodes.Add(new ManeuverNode(crossing.SimTime, trimAction, fine, Fine: true)); }
    var trimPlan = new ManeuverPlan(trimNodes);

    IReadOnlyList<TrajectorySample> holdSamples = flightSim.ProjectAdaptive(crossing, trimPlan, 120 * Day, maxSamples: 20_000);
    double minR = holdSamples.Min(s => s.Position.Length) / crossingRadius;
    double maxR = holdSamples.Max(s => s.Position.Length) / crossingRadius;
    Console.WriteLine($"120-day hold after trim, zero further thrust: r in [{minR:F4}, {maxR:F4}] x crossing radius");

    // Reference: what an EXACT (non-pulse-quantized) circularization would hold at.
    var exactState = new ShipState(crossing.Position, crossing.Velocity.Normalized() * circularThere, crossing.SimTime);
    IReadOnlyList<TrajectorySample> exactSamples = flightSim.ProjectAdaptive(exactState, null, 120 * Day, maxSamples: 20_000);
    double exactMinR = exactSamples.Min(s => s.Position.Length) / crossingRadius;
    double exactMaxR = exactSamples.Max(s => s.Position.Length) / crossingRadius;
    Console.WriteLine($"reference — exact circular speed there (not pulse-reachable): r in [{exactMinR:F4}, {exactMaxR:F4}] x crossing radius");
    Console.WriteLine();
}

ReportCircularization("Mercury (inward transfer)", MercuryOrbitRadius, inward: true);
ReportCircularization("Saturn (outward transfer)", SaturnOrbitRadius, inward: false);

// ===================================================================================
// Break it — try to reach EXACTLY circular speed with only +-10% pulses
// ===================================================================================
Console.WriteLine("=== BREAK IT: try to land exactly on ratio = 1.000000 with pulses only ===");
Console.WriteLine("Search up to 20 coarse + 60 fine pulses in either direction for the closest approach to 1.0:");
double bestOverall = double.MaxValue;
(int cj, int ck, int fm, int fn) bestCombo = (0, 0, 0, 0);
for (int cj = 0; cj <= 20; cj++)
{
    for (int ck = 0; ck <= 20; ck++)
    {
        double coarseMult = Math.Pow(1.1, cj) * Math.Pow(0.9, ck);
        for (int fm = 0; fm <= 60; fm++)
        {
            for (int fn = 0; fn <= 60; fn++)
            {
                double mult = coarseMult * Math.Pow(1.01, fm) * Math.Pow(0.99, fn);
                double residual = Math.Abs(mult - 1.0);
                if (residual < bestOverall)
                {
                    bestOverall = residual;
                    bestCombo = (cj, ck, fm, fn);
                }
            }
        }
    }
}
Console.WriteLine($"Best combo found: {bestCombo.cj} accelerate + {bestCombo.ck} decelerate coarse, " +
    $"{bestCombo.fm} fine-accelerate + {bestCombo.fn} fine-decelerate -> residual {bestOverall:E6}");
Console.WriteLine("Explanation: every pulse multiplies velocity by one of {1.1, 0.9, 1.01, 0.99}. Reaching EXACTLY");
Console.WriteLine("1.0 requires 1.1^j * 0.9^k * 1.01^m * 0.99^n = 1 for some integers j,k,m,n not all zero — i.e.");
Console.WriteLine("j*ln(1.1) + k*ln(0.9) + m*ln(1.01) + n*ln(0.99) = 0. These four logarithms are not rationally");
Console.WriteLine("related (they come from unrelated decimal ratios, not designed lattice generators), so the only");
Console.WriteLine("exact integer solution is the trivial one (0,0,0,0). The residual above is the search's floor,");
Console.WriteLine("not a bug: it shrinks as you allow more pulses, but a finite pulse budget can only ever get");
Console.WriteLine("CLOSE to circular, never land on it exactly — which is exactly why the cockpit's 'circular here'");
Console.WriteLine("readout (M16b) is phrased as something to trim TOWARD, not something you can zero out.");
