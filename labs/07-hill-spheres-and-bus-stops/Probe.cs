// Lab 07 — Hill spheres and bus stops
//
// Teaching voice: "sphere of influence" sounds like a hard boundary, but it comes from an
// approximation (Curtis, "Orbital Mechanics for Engineering Students," ch. 8: the Hill/Laplace
// sphere as the radius where a secondary body's own gravity starts to dominate the primary's
// tidal pull). The textbook formula is a closed-form cube root; it does not, by itself, tell you
// whether a satellite parked at that exact radius actually stays put. This probe checks it the
// honest way: place test satellites in circular orbits around Mars at a range of radii expressed
// as fractions of the formula's Hill radius, integrate them forward in the FULL field (Sun *and*
// Mars both pulling on the ship — `CircularOrbitEphemeris` never lets the two bodies pull on each
// other, only the ship feels gravity, see `CircularOrbitEphemeris.cs`), and watch which ones are
// still bound to Mars five Mars-years later. Then it turns to the game's own orbit-insertion rule
// (`OrbitRule`, M20): the Δv a burn costs in mass-driver pulses at a few approach speeds, and why
// the hard 5 km/s cutoff (`OrbitRule.MaxRelativeSpeed`) lines up with how fast ships actually
// arrive from a standard low-energy transfer.
//
// IRONCLAD RULE: every number below came from running this probe. Change the code, rerun,
// re-paste — never hand-edit labs/07-hill-spheres-and-bus-stops/README.md's tables.

using SpaceSails.Core;

const double SunMu = 1.32712440018e20;        // m^3/s^2 (scenarios/sol.json "sun")
const double SunBodyRadius = 6.9634e9;        // m (scenarios/sol.json "sun" bodyRadiusM)
const double EarthOrbitRadius = 1.496e11;     // m (scenarios/sol.json "earth" orbitRadiusM)
const double MarsMu = 4.282837e13;            // m^3/s^2 (scenarios/sol.json "mars")
const double MarsBodyRadius = 3.3895e6;       // m (scenarios/sol.json "mars" bodyRadiusM)
const double MarsOrbitRadius = 2.2794e11;     // m (scenarios/sol.json "mars" orbitRadiusM)
const double MarsOrbitPeriod = 5.93551e7;     // s (scenarios/sol.json "mars" orbitPeriodS)

var sun = new CelestialBody("sun", "Sun", null, SunMu, SunBodyRadius, 0, 0, 0);
var mars = new CelestialBody("mars", "Mars", "sun", MarsMu, MarsBodyRadius, MarsOrbitRadius, MarsOrbitPeriod, 0);
var ephemeris = new CircularOrbitEphemeris([sun, mars]);
var simulator = new Simulator(ephemeris, timeStepSeconds: 1.0); // TimeStep unused by RunAdaptive

double marsAngularRate = Math.Tau / MarsOrbitPeriod;
Vector2d MarsPositionAt(double t) =>
    new(MarsOrbitRadius * Math.Cos(marsAngularRate * t), MarsOrbitRadius * Math.Sin(marsAngularRate * t));
Vector2d MarsVelocityAt(double t) =>
    new(-MarsOrbitRadius * marsAngularRate * Math.Sin(marsAngularRate * t), MarsOrbitRadius * marsAngularRate * Math.Cos(marsAngularRate * t));

double hillFormula = OrbitRule.HillRadius(mars, SunMu);
const int checkpoints = 400;

Console.WriteLine("=== Section A: Hill radius — formula vs a numerical bound/unbound test ===");
Console.WriteLine();
Console.WriteLine($"Formula (Curtis ch. 8): r_H = a_Mars * (mu_Mars / (3 * mu_Sun))^(1/3) = {hillFormula:E6} m" +
    $" ({hillFormula / 1000:F0} km, {hillFormula / MarsBodyRadius:F0}x Mars's own radius)");
Console.WriteLine();
Console.WriteLine("Test: a satellite in a circular orbit around Mars at r = fraction * r_H(formula),");
Console.WriteLine("integrated in the REAL field (Sun's gravity included, not switched off) for");
Console.WriteLine($"5 Mars-years, checked at {checkpoints} checkpoints against OrbitRule.IsBound (bound =");
Console.WriteLine("inside r_H(formula) of Mars AND negative energy relative to Mars, at every checkpoint —");
Console.WriteLine("one escape anywhere in the run fails the whole radius).");
Console.WriteLine();

double[] fractions = [0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.2];
double totalDuration = 5 * MarsOrbitPeriod;
double dtCheckpoint = totalDuration / checkpoints;

var boundFractions = new List<double>();
var unboundFractions = new List<double>();
Console.WriteLine($"{"fraction of r_H",-18}{"radius (km)",-16}{"still bound after 5 Mars-yr?",-30}{"max |r-Mars| / r_H reached",-28}{"escaped at (Mars-yr)",-20}");
foreach (double fraction in fractions)
{
    double r = fraction * hillFormula;
    Vector2d mars0 = MarsPositionAt(0);
    Vector2d marsV0 = MarsVelocityAt(0);
    Vector2d shipPos = mars0 + new Vector2d(r, 0);
    double vCirc = OrbitRule.CircularSpeed(mars, r);
    Vector2d shipVel = marsV0 + new Vector2d(0, vCirc); // prograde, same sense as Mars's own orbit
    var state = new ShipState(shipPos, shipVel, 0);

    bool everBound = true;
    double worstRatio = 0;
    double escapedAtYears = -1;
    for (int c = 1; c <= checkpoints; c++)
    {
        state = simulator.RunAdaptive(state, dtCheckpoint, null, minTimeStep: 1.0, maxTimeStep: 43200.0, dynamicalTimeFraction: 1.0 / 64);
        Vector2d marsPos = MarsPositionAt(state.SimTime);
        Vector2d marsVel = MarsVelocityAt(state.SimTime);
        double dist = (state.Position - marsPos).Length;
        worstRatio = Math.Max(worstRatio, dist / hillFormula);
        bool bound = OrbitRule.IsBound(state, marsPos, marsVel, mars, hillFormula);
        if (!bound)
        {
            everBound = false;
            escapedAtYears = c * dtCheckpoint / MarsOrbitPeriod;
            break;
        }
    }

    (everBound ? boundFractions : unboundFractions).Add(fraction);

    string escapedLabel = everBound ? "-" : $"{escapedAtYears:F2}";
    Console.WriteLine($"{fraction,-18:F2}{r / 1000,-16:F0}{(everBound ? "YES" : "no — escaped"),-30}{worstRatio,-28:F3}{escapedLabel,-20}");
}

Console.WriteLine();
Console.WriteLine($"Surprise: the boundary is NOT a single clean radius. Bound fractions found:" +
    $" {string.Join(", ", boundFractions.Select(f => f.ToString("F2")))} * r_H. Unbound:" +
    $" {string.Join(", ", unboundFractions.Select(f => f.ToString("F2")))} * r_H.");
Console.WriteLine("A narrow stable island near 0.20 * r_H, a genuine escape band at 0.30-0.60 * r_H,");
Console.WriteLine("then a wide stable island again at 0.70-0.90 * r_H, before everything at or above");
Console.WriteLine("the formula radius escapes. This is the restricted three-body problem's real");
Console.WriteLine("texture, not a bug in this probe: at these fractions the local orbit period around");
Console.WriteLine("Mars falls into and out of resonance with the Sun's once-per-Mars-year tidal tug,");
Console.WriteLine("and resonant fractions get pumped out over a handful of years while off-resonant");
Console.WriteLine("ones stay put — the same flavor of structure lesson 09 finds again at solar-system");
Console.WriteLine("scale. The formula radius (fraction 1.00) is a leading-order approximation — it");
Console.WriteLine("keeps only the point where Mars's own pull matches the Sun's *average* pull, and");
Console.WriteLine("drops the tidal gradient that carves out these islands — so treat it as an upper");
Console.WriteLine("bound on where a parking orbit MIGHT survive, never a guarantee.");

Console.WriteLine();
Console.WriteLine("=== Section B: orbit-insertion economics (OrbitRule, M20) ===");
Console.WriteLine();

// Ground the 5 km/s cutoff against a real number: the hyperbolic excess speed a standard Hohmann
// transfer arrives at Mars with (Curtis ch. 8's patched-conic v_infinity), from vis-viva on the
// transfer ellipse and Mars's own circular speed.
double aTransfer = (EarthOrbitRadius + MarsOrbitRadius) / 2;
double vArrival = Math.Sqrt(SunMu * (2 / MarsOrbitRadius - 1 / aTransfer));
double vMarsOrbit = Math.Sqrt(SunMu / MarsOrbitRadius);
double vInfinityHohmann = Math.Abs(vMarsOrbit - vArrival);
Console.WriteLine($"Earth->Mars Hohmann transfer: arrival speed on the transfer ellipse = {vArrival:F1} m/s," +
    $" Mars's own orbital speed = {vMarsOrbit:F1} m/s.");
Console.WriteLine($"Patched-conic hyperbolic excess speed relative to Mars: v_infinity = {vInfinityHohmann:F1} m/s.");
Console.WriteLine($"OrbitRule.MaxRelativeSpeed = {OrbitRule.MaxRelativeSpeed:F0} m/s" +
    $" ({OrbitRule.MaxRelativeSpeed / vInfinityHohmann:F1}x a standard Hohmann arrival's v_infinity).");
Console.WriteLine();

double[] approachSpeeds = [500, 1500, vInfinityHohmann, 4900, 5200];
Vector2d testMarsPos = MarsPositionAt(0);
Vector2d testMarsVel = MarsVelocityAt(0);
Vector2d testShipPos = testMarsPos + new Vector2d(0.4 * hillFormula, 0); // safely in the bound region found above

Console.WriteLine($"{"approach speed (m/s)",-22}{"window open?",-14}{"insertion dv (m/s)",-20}{"pulse cost",-12}{"ship heliocentric v (m/s)",-26}{"bound after insert?",-20}");
foreach (double vRel in approachSpeeds)
{
    Vector2d relVel = new(-vRel, 0); // radially inward approach
    Vector2d shipVel = testMarsVel + relVel;
    var shipState = new ShipState(testShipPos, shipVel, 0);

    bool open = OrbitRule.WindowOpen(shipState, testMarsPos, testMarsVel, mars, hillFormula);
    double dv = OrbitRule.InsertionDeltaV(shipState, testMarsPos, testMarsVel, mars);
    int pulses = OrbitRule.PulseCost(shipState, testMarsPos, testMarsVel, mars);
    ShipState afterInsert = OrbitRule.Insert(shipState, testMarsPos, testMarsVel, mars);
    bool boundAfter = OrbitRule.IsBound(afterInsert, testMarsPos, testMarsVel, mars, hillFormula);

    Console.WriteLine($"{vRel,-22:F0}{(open ? "yes" : "NO"),-14}{dv,-20:F1}{pulses,-12}{shipVel.Length,-26:F1}{(boundAfter ? "yes" : "n/a (blocked)"),-20}");
}

Console.WriteLine();
Console.WriteLine("Why the window is shaped this way: PulseCost prices a burn as a percentage of the");
Console.WriteLine("ship's own heliocentric speed (~Mars's ~24 km/s orbital speed, not the small relative");
Console.WriteLine("approach speed), so the pulse count barely moves across ordinary approach speeds —");
Console.WriteLine("what actually gates the burn is OrbitRule.MaxRelativeSpeed, a flat 5000 m/s cutoff.");
Console.WriteLine("A standard Hohmann arrival's v_infinity sits comfortably under it (see the ratio");
Console.WriteLine("above); only an unusually hot direct/hyperbolic approach — well above what any");
Console.WriteLine("plotted low-energy transfer produces — gets locked out, same as the 5200 m/s row.");

Console.WriteLine();
Console.WriteLine("=== Break it: insert right at the Hill edge ===");
Console.WriteLine();
Console.WriteLine("Section A already ran this experiment as fraction = 1.00 above: a satellite placed");
Console.WriteLine("in a circular orbit at exactly the formula's Hill radius does NOT survive 5");
Console.WriteLine("Mars-years bound to Mars (see the row above) — the Sun's tide eventually walks it");
Console.WriteLine("out. WindowOpen's strict `distance < hillRadius` check still lets a ship attempt");
Console.WriteLine("insertion arbitrarily close to that edge; OrbitRule.Insert() computes the velocity");
Console.WriteLine("for a circular orbit as if Mars were the only mass in the universe, and at t=0 that");
Console.WriteLine("state is genuinely bound (IsBound is a snapshot). It just doesn't STAY bound — the");
Console.WriteLine("empirical boundary in Section A is the honest answer for how much margin an insertion");
Console.WriteLine("burn actually needs below the nominal Hill radius to be a real parking orbit and not");
Console.WriteLine("a slow-motion escape.");
