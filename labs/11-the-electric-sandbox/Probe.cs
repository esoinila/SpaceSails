// Lab 11 — The Electric Sandbox ⚡
//
// LABEL: this whole lesson is the game's fictional cosmology — the Electric Universe layer
// (PlasmaEnvironment) has no standard-physics counterpart; real solar wind doesn't hand a ship
// free momentum, and gravity is not coupled to charge in any theory anyone measures against.
// "In this house we compute both, and we label which is which" (docs/SaturdayPlan/GravityLab.md):
// everything below is computed with the same numerical honesty as the standard lessons, using
// the real PlasmaEnvironment class the live game ships with — it is simply computing a fiction,
// on purpose, and says so.
//
// Sections A and B compute the game's ACTUAL, shipped EU mechanic (the halo formula and the
// stream force really are in src/SpaceSails.Core/PlasmaEnvironment.cs, unchanged). Section C is
// explicitly a MADE-UP speculative extension layered on top for this lesson only — gravity
// itself does not depend on charge anywhere else in this codebase.
//
// IRONCLAD RULE: every number below came from running this probe.

using SpaceSails.Core;

const double Day = 86400;
const double SunMu = 1.32712440018e20;

// Real orbit radii + periods from scenarios/sol.json (used by both sol-eu.json and wheel.json).
const double MercuryOrbitRadius = 5.791e10;
const double VenusOrbitRadius = 1.0821e11;
const double EarthOrbitRadius = 1.496e11;
const double MarsOrbitRadius = 2.2794e11;
const double JupiterOrbitRadius = 7.7857e11;
const double SaturnOrbitRadius = 1.43353e12;
const double MercuryPeriod = 7.60052e6;
const double SaturnBodyRadius = 5.8232e7;

var sun = new CelestialBody("sun", "Sun", null, SunMu, 6.9634e9, 0, 0, 0);
var mercury = new CelestialBody("mercury", "Mercury", "sun", 2.2032e13, 2.4397e6, MercuryOrbitRadius, MercuryPeriod, 0.0);
var venus = new CelestialBody("venus", "Venus", "sun", 3.24859e14, 6.0518e6, VenusOrbitRadius, 1.94142e7, 0.9);
var earth = new CelestialBody("earth", "Earth", "sun", 3.986004418e14, 6.371e6, EarthOrbitRadius, 3.1558149e7, 1.8);
var mars = new CelestialBody("mars", "Mars", "sun", 4.282837e13, 3.3895e6, MarsOrbitRadius, 5.93551e7, 2.7);
var jupiter = new CelestialBody("jupiter", "Jupiter", "sun", 1.26686534e17, 6.9911e7, JupiterOrbitRadius, 3.74336e8, 3.6);
var saturn = new CelestialBody("saturn", "Saturn", "sun", 3.7931187e16, SaturnBodyRadius, SaturnOrbitRadius, 9.29596e8, 4.5);

var ephemeris = new CircularOrbitEphemeris([sun, mercury, venus, earth, mars, jupiter, saturn]);

// The real sol-eu.json streams: saturn<->jupiter (half-width 3e10 m), venus<->mercury (1.5e10 m).
var environment = new PlasmaEnvironment(ephemeris, [("saturn", "jupiter", 3e10), ("venus", "mercury", 1.5e10)]);

// ---- Section A: charge equilibration vs distance -------------------------------------------

Console.WriteLine("=== Section A: the solar halo — ambient charge = min(1, (5e10/r)^2) ===");
Console.WriteLine("[LABEL: fictional — the Electric Universe layer, not real solar physics]");
Console.WriteLine();

// Halo in isolation (no streams): sol-eu.json's real streams happen to run venus<->mercury and
// saturn<->jupiter, so measuring "ambient charge at Mercury's/Jupiter's orbit radius" with the
// full environment would accidentally sample a point sitting exactly on a stream endpoint
// (distance 0 <= half-width -> saturated to 100%) — a measurement artifact, not the halo. A
// stream-free environment isolates the pure r^-2 halo the doc claims are about.
var haloOnly = new PlasmaEnvironment(ephemeris, []);

Console.WriteLine($"{"body",-12}{"orbit radius (AU)",-20}{"ambient charge",-18}");
foreach ((string name, double r) in new (string, double)[]
         {
             ("Mercury", MercuryOrbitRadius), ("Venus", VenusOrbitRadius), ("Earth", EarthOrbitRadius),
             ("Mars", MarsOrbitRadius), ("Jupiter", JupiterOrbitRadius),
         })
{
    double ambient = haloOnly.AmbientCharge(new Vector2d(r, 0), 0);
    Console.WriteLine($"{name,-12}{r / 1.496e11,-20:F4}{ambient,-18:P1}");
}

Console.WriteLine();
Console.WriteLine("docs/features/electric-sky.md claims 'roughly 75% ambient at Mercury, down to about");
Console.WriteLine("11% at Earth' — the table above is that claim, actually computed against the live");
Console.WriteLine("PlasmaEnvironment class rather than taken on faith.");
Console.WriteLine();

Console.WriteLine("=== Charge equilibration over time: relaxing toward ambient, tau = 3600 s ===");
Console.WriteLine("A ship parked at Earth's orbit (ambient ~ 11.2%), charge starting at 0, stepped with");
Console.WriteLine("the real Simulator + environment at dt = 60 s (Simulator.Step's own exponential-blend");
Console.WriteLine("formula: charge += (ambient - charge) * min(1, dt/tau)).");
Console.WriteLine();

var chargeSim = new Simulator(ephemeris, timeStepSeconds: 60.0, environment: haloOnly);
var chargeShip = new ShipState(new Vector2d(EarthOrbitRadius, 0), new Vector2d(0, Math.Sqrt(SunMu / EarthOrbitRadius)), 0, Charge: 0);
double earthAmbient = haloOnly.AmbientCharge(chargeShip.Position, 0);

Console.WriteLine($"{"tau multiples",-16}{"sim time (s)",-14}{"charge",-14}{"fraction of ambient",-20}");
double[] tauMultiples = [0, 1, 2, 3, 5, 10];
int stepIndex = 0;
foreach (double tauMult in tauMultiples)
{
    double targetTime = tauMult * PlasmaEnvironment.EquilibrationTau;
    while (chargeShip.SimTime < targetTime)
    {
        chargeShip = chargeSim.Step(chargeShip);
        stepIndex++;
    }

    Console.WriteLine($"{tauMult,-16:F0}{chargeShip.SimTime,-14:F0}{chargeShip.Charge,-14:P2}{chargeShip.Charge / earthAmbient,-20:P1}");
}

// ---- Section B: stream-riding, Saturn -> Jupiter --------------------------------------------

Console.WriteLine();
Console.WriteLine("=== Section B: riding the Saturn->Jupiter stream vs a standard ballistic Hohmann transfer ===");
Console.WriteLine("[Standard half: the Hohmann transfer time is real orbital mechanics, Curtis ch. 6.]");
Console.WriteLine("[Fictional half: the stream ride uses PlasmaEnvironment, the game's EU layer.]");
Console.WriteLine();

double aTransfer = (SaturnOrbitRadius + JupiterOrbitRadius) / 2.0;
double vCircSaturn = Math.Sqrt(SunMu / SaturnOrbitRadius);
double vTransferAtSaturn = Math.Sqrt(SunMu * (2.0 / SaturnOrbitRadius - 1.0 / aTransfer));
double hohmannDeltaV = vTransferAtSaturn - vCircSaturn;
double hohmannTime = Math.PI * Math.Sqrt(aTransfer * aTransfer * aTransfer / SunMu);

Console.WriteLine($"Standard Hohmann Saturn (1.43353e12 m) -> Jupiter (7.7857e11 m): retrograde burn " +
    $"{hohmannDeltaV:F1} m/s, transfer time = {hohmannTime:F0} s ({hohmannTime / Day:F1} days, " +
    $"{hohmannTime / Day / 365.25:F2} years).");
Console.WriteLine();

Vector2d saturnPos0 = ephemeris.Position("saturn", 0);
Vector2d jupiterPos0 = ephemeris.Position("jupiter", 0);
const double H = 1.0;
Vector2d saturnVel0 = (ephemeris.Position("saturn", H) - ephemeris.Position("saturn", -H)) / (2 * H);
Vector2d streamDir0 = (jupiterPos0 - saturnPos0).Normalized();

// Depart just clear of Saturn's body, already inside the stream ribbon, co-moving with Saturn
// (like a cargo pod that just undocked) and already fully charged (a smuggler running hot).
var rideStart = new ShipState(
    saturnPos0 + streamDir0 * (SaturnBodyRadius * 3),
    saturnVel0,
    0,
    Charge: 1.0);

// "Arrival" = inside Jupiter's own Hill sphere (real orbital mechanics: the radius within which
// Jupiter's own gravity, not the Sun's, dominates) rather than an arbitrary round number.
double jupiterHillRadius = JupiterOrbitRadius * Math.Cbrt(jupiter.Mu / (3 * SunMu));
double ArrivalRadius = jupiterHillRadius;
const double RideHorizon = 400 * Day;
const double RideDt = 600.0;

(bool arrived, double simTime, double minDistanceToJupiter, bool leftStream) RunTransfer(ShipState start, PlasmaEnvironment? env)
{
    var sim = new Simulator(ephemeris, RideDt, env);
    ShipState state = start;
    double minDist = (state.Position - jupiterPos0).Length;
    bool leftStream = false;
    while (state.SimTime < RideHorizon)
    {
        state = sim.Step(state);
        Vector2d jupiterNow = ephemeris.Position("jupiter", state.SimTime);
        double dist = (state.Position - jupiterNow).Length;
        minDist = Math.Min(minDist, dist);
        if (dist <= ArrivalRadius)
        {
            return (true, state.SimTime, minDist, leftStream);
        }
    }

    return (false, state.SimTime, minDist, leftStream);
}

Console.WriteLine($"'Arrival' = inside Jupiter's own Hill sphere ({jupiterHillRadius:E3} m — real orbital " +
    "mechanics, not an arbitrary round number: the radius where Jupiter's own gravity starts to dominate " +
    "over the Sun's).");
Console.WriteLine();

(bool ridArrived, double ridTime, double ridMinDist, _) = RunTransfer(rideStart, environment);
var ballisticStart = rideStart with { Charge = 0 };
(bool balArrived, double balTime, double balMinDist, _) = RunTransfer(ballisticStart, null);

Console.WriteLine($"{"case",-38}{"outcome",-14}{"sim time (days)",-18}{"closest approach to Jupiter (m)",-30}");
Console.WriteLine($"{"charged, riding the stream",-38}{(ridArrived ? "ARRIVED" : "no arrival"),-14}{ridTime / Day,-18:F2}{ridMinDist,-30:E3}");
Console.WriteLine($"{"uncharged, same departure, no stream",-38}{(balArrived ? "ARRIVED" : "no arrival"),-14}{balTime / Day,-18:F2}{balMinDist,-30:E3}");
Console.WriteLine();
Console.WriteLine($"Same departure position and velocity in both rows (just undocked from Saturn, co-moving");
Console.WriteLine($"with it) — the ONLY difference is charge=1 + the stream force vs charge=0/no environment.");
if (ridArrived)
{
    Console.WriteLine($"Riding the stream: {ridTime / Day:F1} days, vs. the standard ballistic Hohmann's " +
        $"{hohmannTime / Day:F0} days for the same trip done the textbook way (a real transfer burn instead " +
        "of freeloading on the current) — riding the river is a stream-ribbon shortcut, not a substitute for" +
        " actually planning a transfer.");
}

// ---- Section C: speculative mu(charge) playground -------------------------------------------

Console.WriteLine();
Console.WriteLine("=== Section C: SPECULATIVE — what if effective mu depended on the electrical environment? ===");
Console.WriteLine("[LABEL: pure speculative fun. Nowhere else in this codebase does gravity depend on charge.");
Console.WriteLine(" mu_eff(charge_environment) = mu0 * (1 + k * q(r)) for a made-up k. Real Newtonian gravity");
Console.WriteLine(" (used everywhere else in this repo, including labs 1-3, 9) has mu independent of q.]");
Console.WriteLine();

double qMercury = haloOnly.AmbientCharge(new Vector2d(MercuryOrbitRadius, 0), 0);
double qEarth = haloOnly.AmbientCharge(new Vector2d(EarthOrbitRadius, 0), 0);
double mercuryYearReal = 2 * Math.PI * Math.Sqrt(MercuryOrbitRadius * MercuryOrbitRadius * MercuryOrbitRadius / SunMu);

double aMarsTransfer = (EarthOrbitRadius + MarsOrbitRadius) / 2.0;
double earthMarsHohmannReal = Math.PI * Math.Sqrt(aMarsTransfer * aMarsTransfer * aMarsTransfer / SunMu);

Console.WriteLine($"q(Mercury) = {qMercury:P1}, q(Earth) = {qEarth:P1} (from Section A).");
Console.WriteLine($"Real Mercury year: {mercuryYearReal / Day:F3} days. Real Earth->Mars Hohmann: {earthMarsHohmannReal / Day:F2} days.");
Console.WriteLine();
Console.WriteLine($"{"k",-10}{"mu_eff/mu0 @ Mercury",-22}{"Mercury year (days)",-22}{"mu_eff/mu0 @ Earth",-20}{"Earth->Mars transfer (days)",-28}");
foreach (double k in new double[] { 0.05, 0.20, 0.50 })
{
    double muScaleMercury = 1 + k * qMercury;
    double mercuryYearSpeculative = mercuryYearReal / Math.Sqrt(muScaleMercury);

    double muScaleEarth = 1 + k * qEarth;
    double transferSpeculative = earthMarsHohmannReal / Math.Sqrt(muScaleEarth);

    Console.WriteLine($"{k,-10:F2}{muScaleMercury,-22:F4}{mercuryYearSpeculative / Day,-22:F3}{muScaleEarth,-20:F4}{transferSpeculative / Day,-28:F2}");
}

Console.WriteLine();
Console.WriteLine("Mercury sits in a hot neighborhood (q ~ 75%), so even a modest k measurably shortens its");
Console.WriteLine("year; Earth's cold neighborhood (q ~ 11%) barely moves the Earth->Mars transfer time at");
Console.WriteLine("the same k. If gravity really did couple to the electrical environment this way, the");
Console.WriteLine("measurable signature would be inner planets running fast clocks relative to outer ones —");
Console.WriteLine("a clean, falsifiable 'what you would actually see' for a purely speculative toy.");

// ---- Break-it ---------------------------------------------------------------------------------

Console.WriteLine();
Console.WriteLine("=== Break it yourself ===");
Console.WriteLine("1. Section B's ride starts already at charge=1 (already hot). Start at charge=0 instead");
Console.WriteLine("   (a cold, sneaking departure) and see how many hours of equilibration (tau=3600s) it");
Console.WriteLine("   costs before the stream force is worth anything.");
Console.WriteLine("2. Section C picks k in {0.05, 0.20, 0.50} arbitrarily -- there is no in-repo constant to");
Console.WriteLine("   check against, because this mechanic doesn't exist outside this lesson. Find the k");
Console.WriteLine("   where Mercury's speculative year drops under half its real value.");
Console.WriteLine("3. Move Section B's departure point off the stream centerline by more than the ribbon's");
Console.WriteLine("   half-width (3e10 m) and watch the charge stop equilibrating to 1.0 -- the ship goes");
Console.WriteLine("   cold and the 'free momentum' disappears entirely, mid-flight.");
