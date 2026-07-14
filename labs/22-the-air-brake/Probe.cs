// Lab 22 — The air brake
//
// Teaching voice: two of the flight assists in docs/TuesdayPlan/FlightAssists.md need no new
// physics — a slingshot is lesson 19's crank with a button on it. The third does: to brake
// against a planet's air (Stargate Universe's gas-giant dip) or bounce off it (the Apollo return
// corridor Kranz's people sweated), the simulator has to know a body can have an ATMOSPHERE. This
// lesson adds exactly one honest ingredient — an exponential density shell with a single ballistic
// coefficient — and then computes the three things a pilot actually needs: how much speed a skim
// sheds vs how deep you dip (the corridor), how narrow the Apollo skip corridor really is (in km of
// periapsis altitude), and whether you can capture into orbit on drag alone with the tank dry (the
// SGU move), flown pass by pass. The SAME Core drag the game ships draws every number below.
//
// The model, stated up front and again in Section A: density(altitude) = refDensity ·
// exp(−altitude/scaleHeight), zero at/above a hard shell top; drag accel = −0.5 · ρ · |v_rel| ·
// v_rel / BC, with v_rel the ship's velocity minus the body's rail velocity (the air moves with the
// planet; its spin, lift, and all heating physics are deliberately ignored — "too deep" is charged
// as hull load off the peak deceleration, not a thermal model). BC is the game's one knob.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code, the
// printed numbers in labs/22-the-air-brake/README.md go stale — rerun and re-paste, never hand-edit.

using SpaceSails.Core;
using SpaceSails.LabViz;

const double G0 = 9.80665; // standard gravity, m/s^2 — only for reporting deceleration in g

// Atmosphere constants MIRROR scenarios/sol.json exactly, so the corridor this lab draws IS the
// corridor the game node will price. (The lab flies in a planet-centered frame — one body at the
// origin — to isolate the drag physics from solar perturbation; the drag code is identical.)
const double JupiterMu = 1.26686534e17;
const double JupiterR = 6.9911e7;
var jupiterAtm = new Atmosphere(RefDensity: 4.0e-6, ScaleHeight: 3.0e4, TopAltitude: 4.0e5);

const double EarthMu = 3.986004418e14;
const double EarthR = 6.371e6;
var earthAtm = new Atmosphere(RefDensity: 1.2, ScaleHeight: 8.0e3, TopAltitude: 1.4e5);

const double MercuryMu = 2.2032e13;
const double MercuryR = 2.4397e6; // no atmosphere — the null-shell control in Break-it #3

// A single-body, body-at-origin ephemeris: parentId null + orbitPeriod 0 pins the body at (0,0)
// with zero rail velocity, so v_rel is just the ship's velocity and the two-body energy is clean.
Simulator MakeSim(string id, double mu, double radius, Atmosphere? atm) =>
    new(new CircularOrbitEphemeris([new CelestialBody(id, id, null, mu, radius, 0, 0, 0, Atmosphere: atm)]),
        timeStepSeconds: 1.0);

var jupiter = MakeSim("jupiter", JupiterMu, JupiterR, jupiterAtm);
var earth = MakeSim("earth", EarthMu, EarthR, earthAtm);

// --viz (optional): everything behind LabViz.Wants, so no-flag stdout is byte-identical.
var viz = LabViz.Wants(args)
    ? new VizScene("lab22-the-air-brake", "Lab 22 — The air brake",
        "aerobraking at Jupiter: the corridor, the Apollo skip, and a fuel-out capture")
    : null;

// ---- helpers ---------------------------------------------------------------------------------

// Incoming hyperbolic state on the +x axis at radius rStart, whose vacuum periapsis is rPeri:
// vis-viva sets the speed from the hyperbolic excess vInf, angular momentum h = rPeri·vPeri sets
// the tangential share, the rest is inbound radial. (The FLOWN periapsis, with drag, is measured
// from the DragReport, not assumed from this construction.)
ShipState Arrival(double mu, double rStart, double rPeri, double vInf, double t0 = 0)
{
    double vPeri = Math.Sqrt(vInf * vInf + 2 * mu / rPeri);
    double h = rPeri * vPeri;
    double v = Math.Sqrt(vInf * vInf + 2 * mu / rStart);
    double vt = h / rStart;
    double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
    return new ShipState(new Vector2d(rStart, 0), new Vector2d(vr, vt), t0);
}

// Two-body specific orbital energy (J/kg) about a body at the origin: <0 bound, >0 escaping.
double Energy(ShipState s, double mu) => s.Velocity.LengthSquared / 2.0 - mu / s.Position.Length;

// One full revolution of the current bound two-body orbit as a polyline, starting at periapsis —
// the analytic ellipse (no integration, so no drift). Successive captured orbits share a periapsis
// region, so concatenating them draws the honest capture spiral: apoapsis dropping pass by pass.
(IReadOnlyList<Vector2d> Points, Vector2d Periapsis, double Period) EllipseArc(ShipState s, double mu, int samples = 240)
{
    double e0 = Energy(s, mu);
    double a = -mu / (2 * e0);
    double hSigned = s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X;
    double p = hSigned * hSigned / mu;
    double ecc = Math.Sqrt(Math.Max(0, 1 - p / a));
    // Eccentricity vector points at periapsis: e_vec = (v x h)/mu - r_hat  (h out of plane = hSigned).
    double evx = s.Velocity.Y * hSigned / mu - s.Position.X / s.Position.Length;
    double evy = -s.Velocity.X * hSigned / mu - s.Position.Y / s.Position.Length;
    double omega = Math.Atan2(evy, evx);
    int dir = hSigned >= 0 ? 1 : -1;
    var pts = new List<Vector2d>(samples + 1);
    for (int i = 0; i <= samples; i++)
    {
        double nu = dir * Math.Tau * i / samples;
        double rr = p / (1 + ecc * Math.Cos(nu));
        double ang = omega + nu;
        pts.Add(new Vector2d(rr * Math.Cos(ang), rr * Math.Sin(ang)));
    }
    double periR = p / (1 + ecc);
    return (pts, new Vector2d(periR * Math.Cos(omega), periR * Math.Sin(omega)), Math.Tau * Math.Sqrt(a * a * a / mu));
}

// Half a conic arc (works for ellipse or hyperbola): from the state's periapsis out to toRadius,
// on the incoming half (side = -1) or the outgoing half (side = +1). Pure analytic conic — used to
// draw each corridor depth's full flyby as pre-drag-in + post-drag-out, meeting at periapsis.
IReadOnlyList<Vector2d> ConicHalf(ShipState s, double mu, double toRadius, int side, int samples = 120)
{
    double r = s.Position.Length;
    double e0 = s.Velocity.LengthSquared / 2 - mu / r;
    double h = s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X; // signed (prograde: >0)
    double p = h * h / mu;
    double ecc = Math.Sqrt(Math.Max(0, 1 + 2 * e0 * h * h / (mu * mu)));
    double evx = s.Velocity.Y * h / mu - s.Position.X / r;
    double evy = -s.Velocity.X * h / mu - s.Position.Y / r;
    double omega = Math.Atan2(evy, evx);
    double cnu = Math.Clamp((p / toRadius - 1) / Math.Max(1e-12, ecc), -1, 1);
    double nuMax = Math.Acos(cnu);
    var pts = new List<Vector2d>(samples + 1);
    for (int i = 0; i <= samples; i++)
    {
        double frac = (double)i / samples;
        double nu = side < 0 ? -nuMax * (1 - frac) : nuMax * frac;
        double rr = p / (1 + ecc * Math.Cos(nu));
        double ang = omega + nu; // prograde
        pts.Add(new Vector2d(rr * Math.Cos(ang), rr * Math.Sin(ang)));
    }
    return pts;
}

// Apoapsis radius (m) of the current two-body orbit, or +Infinity if hyperbolic.
double Apoapsis(ShipState s, double mu)
{
    double e0 = Energy(s, mu);
    if (e0 >= 0) return double.PositiveInfinity;
    double a = -mu / (2 * e0);
    double hMom = Math.Abs(s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X);
    double ecc = Math.Sqrt(Math.Max(0, 1 + 2 * e0 * hMom * hMom / (mu * mu)));
    return a * (1 + ecc);
}

// Fly ONE atmosphere pass: from an above-shell entry state, down through periapsis, until the ship
// climbs back above the shell top — measured, so a captured (bound) pass is never double-counted by
// a second dip. Combines the per-slice DragReports into one pass report. Returns the post-pass
// state (above the shell, drag already zero, so its energy is the clean post-pass energy) plus the
// pass's peak deceleration, Δv shed, and deepest altitude.
(ShipState Post, double PeakDecel, double DvShed, double MinAlt, bool Crashed, IReadOnlyList<TrajectorySample> Track)
    FlyPass(Simulator sim, ShipState entry, double bodyRadius, double topAltitude, double sliceSeconds = 30.0)
{
    double shellTop = bodyRadius + topAltitude;
    double peak = 0, shed = 0, minAlt = double.PositiveInfinity;
    var track = new List<TrajectorySample> { new(entry.SimTime, entry.Position) };
    ShipState s = entry;
    bool entered = false, crashed = false;
    double flownCap = 12 * 3600; // 12 h safety cap (a pass is minutes; this only stops a pathology)
    while (s.SimTime - entry.SimTime < flownCap)
    {
        (ShipState next, Simulator.DragReport rep) =
            sim.RunAdaptiveWithDrag(s, sliceSeconds, null, minTimeStep: 0.1, maxTimeStep: 2.0);
        peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
        shed += rep.DeltaVShedMetersPerSecond;
        if (!double.IsNaN(rep.MinAltitudeMeters)) minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
        s = next;
        track.Add(new TrajectorySample(s.SimTime, s.Position));
        double r = s.Position.Length;
        if (r < bodyRadius) { crashed = true; break; } // decayed into the surface — the pass ends here
        if (r < shellTop) entered = true;
        else if (entered) break;
    }
    if (crashed) minAlt = 0;
    return (s, peak, shed, double.IsPositiveInfinity(minAlt) ? double.NaN : minAlt, crashed, track);
}

// ===================================================================================
// Section A — the model, stated honestly
// ===================================================================================
Console.WriteLine("=== Section A: the model (exponential atmosphere + one ballistic-coefficient knob) ===");
Console.WriteLine("density(h) = refDensity * exp(-h / scaleHeight), and exactly 0 at/above the shell top.");
Console.WriteLine($"drag accel = -0.5 * density * |v_rel| * v_rel / BC,  BC = {Simulator.BallisticCoefficient:F0} kg/m^2 (the game's knob).");
Console.WriteLine("v_rel = ship velocity - body rail velocity (the air translates with the planet).");
Console.WriteLine("IGNORED on purpose: the planet's spin, aerodynamic lift, and all heating physics.");
Console.WriteLine("'too deep' is charged off peak deceleration (below), not a thermal model.");
Console.WriteLine();
Console.WriteLine("sol.json atmosphere shells used here (justified in the README table):");
Console.WriteLine($"{"body",-9}{"refDensity kg/m^3",20}{"scaleHeight km",16}{"shell top km",14}{"top / body radius",18}");
void ModelRow(string name, Atmosphere a, double radius) =>
    Console.WriteLine($"{name,-9}{a.RefDensity,20:G3}{a.ScaleHeight / 1000,16:F1}{a.TopAltitude / 1000,14:F0}{a.TopAltitude / radius,18:F4}");
ModelRow("Jupiter", jupiterAtm, JupiterR);
ModelRow("Earth", earthAtm, EarthR);
ModelRow("Venus", new Atmosphere(65.0, 1.6e4, 1.5e5), 6.0518e6);
ModelRow("Saturn", new Atmosphere(5.0e-6, 1.2e5, 7.0e5), 5.8232e7);
ModelRow("Titan", new Atmosphere(5.3, 4.0e4, 3.0e5), 2.575e6);
Console.WriteLine("Every shell top sits well under 0.15 body radii, so no existing gameplay trajectory");
Console.WriteLine("(orbit insertion parks at ~0.5 Hill radii) ever clips one by accident.");
Console.WriteLine();

// ===================================================================================
// Section B — the corridor at Jupiter: sweep periapsis depth on a fixed hyperbolic arrival
// ===================================================================================
Console.WriteLine("=== Section B: the corridor at Jupiter (fixed hyperbolic arrival, swept depth) ===");
const double BvInf = 5500.0;          // a workaday Jupiter arrival excess speed, m/s
const double DamageLineG = 3.0;       // the hull-damage line: peak deceleration above this holes the sail
double bStart = JupiterR + jupiterAtm.TopAltitude + 3.0e5; // enter 300 km above the shell top
Console.WriteLine($"arrival v_inf = {BvInf / 1000:F1} km/s; damage line = {DamageLineG:F0} g peak deceleration.");
Console.WriteLine($"{"peri alt km",13}{"min alt km",12}{"dv shed m/s",14}{"peak g",10}{"outcome",30}");

double corridorLoKm = double.NaN, corridorHiKm = double.NaN; // gentle-braking corridor edges (alt km)
var bSweep = new List<(double AltKm, ShipState Entry, ShipState Post)>();
foreach (double periAltKm in new[] { 5.0, 20.0, 40.0, 60.0, 80.0, 100.0, 130.0, 170.0, 220.0, 300.0 })
{
    double rPeri = JupiterR + periAltKm * 1000;
    ShipState entry = Arrival(JupiterMu, bStart, rPeri, BvInf);
    var pass = FlyPass(jupiter, entry, JupiterR, jupiterAtm.TopAltitude);
    double peakG = pass.PeakDecel / G0;
    double eOut = Energy(pass.Post, JupiterMu);
    string outcome;
    if (peakG > DamageLineG)
        outcome = eOut < 0 ? "TOO DEEP: hull holed + captured" : "TOO DEEP: hull holed";
    else if (eOut < 0)
        outcome = $"captured, apoapsis {Apoapsis(pass.Post, JupiterMu) / JupiterR:F1} R_J";
    else
        outcome = $"exits, v_inf {Math.Sqrt(2 * eOut) / 1000:F2} km/s";
    Console.WriteLine($"{periAltKm,13:F0}{pass.MinAlt / 1000,12:F1}{pass.DvShed,14:F0}{peakG,10:F2}{outcome,30}");

    // Gentle-braking corridor = sheds a useful >50 m/s without crossing the damage line.
    if (peakG <= DamageLineG && pass.DvShed >= 50)
    {
        corridorHiKm = double.IsNaN(corridorHiKm) ? periAltKm : Math.Max(corridorHiKm, periAltKm);
        corridorLoKm = double.IsNaN(corridorLoKm) ? periAltKm : Math.Min(corridorLoKm, periAltKm);
    }
    bSweep.Add((periAltKm, entry, pass.Post));
}
Console.WriteLine();
Console.WriteLine($"THREE ZONES: too shallow (>~220 km: <50 m/s shed, nothing happens) | the corridor");
Console.WriteLine($"(~{corridorLoKm:F0}-{corridorHiKm:F0} km: useful braking, under the {DamageLineG:F0} g line) | too deep (the sail holes).");
Console.WriteLine();

// ===================================================================================
// Section C — the skip at Earth: Apollo-return-grade arrival, sweep entry depth
// ===================================================================================
Console.WriteLine("=== Section C: the skip at Earth (Apollo-return speed, swept entry depth) ===");
const double CvInf = 1500.0;          // lunar-return-grade hyperbolic excess, m/s (~11 km/s at entry)
const double SkipDamageG = 6.5;       // Apollo crews held ~6-7 g; above this the capsule burns up
double cStart = EarthR + earthAtm.TopAltitude + 6.0e4;
double entrySpeed = Math.Sqrt(CvInf * CvInf + 2 * EarthMu / (EarthR + 122e3));
Console.WriteLine($"arrival v_inf = {CvInf / 1000:F1} km/s ({entrySpeed / 1000:F2} km/s at the 122 km interface); burn-up line = {SkipDamageG:F1} g.");
Console.WriteLine($"{"peri alt km",13}{"min alt km",12}{"dv shed m/s",14}{"peak g",10}{"outcome",26}");

double captureLoKm = double.NaN, captureHiKm = double.NaN; // capture-without-burnup band edges
foreach (double periAltKm in new[] { 110.0, 100.0, 95.0, 90.0, 85.0, 80.0, 75.0, 70.0, 65.0, 60.0 })
{
    double rPeri = EarthR + periAltKm * 1000;
    ShipState entry = Arrival(EarthMu, cStart, rPeri, CvInf);
    var pass = FlyPass(earth, entry, EarthR, earthAtm.TopAltitude, sliceSeconds: 20.0);
    double peakG = pass.PeakDecel / G0;
    double eOut = Energy(pass.Post, EarthMu);
    bool augersIn = pass.Crashed || pass.MinAlt < 2000; // reached the surface — the capsule is gone regardless of g
    string outcome;
    if (augersIn) outcome = "BURN-UP (augers in)";
    else if (peakG > SkipDamageG) outcome = "BURN-UP (too steep)";
    else if (eOut >= 0) outcome = $"skips out, v_inf {Math.Sqrt(2 * eOut) / 1000:F2} km/s";
    else outcome = $"CAPTURED, apo {Apoapsis(pass.Post, EarthMu) / 1000:F0} km";
    Console.WriteLine($"{periAltKm,13:F0}{pass.MinAlt / 1000,12:F1}{pass.DvShed,14:F0}{peakG,10:F2}{outcome,26}");

    if (!augersIn && peakG <= SkipDamageG && eOut < 0)
    {
        captureHiKm = double.IsNaN(captureHiKm) ? periAltKm : Math.Max(captureHiKm, periAltKm);
        captureLoKm = double.IsNaN(captureLoKm) ? periAltKm : Math.Min(captureLoKm, periAltKm);
    }
}
double corridorWidthKm = double.IsNaN(captureHiKm) ? double.NaN : captureHiKm - captureLoKm;
Console.WriteLine();
Console.WriteLine($"Apollo's honest corridor: capture-without-burn-up spans periapsis altitude " +
    $"{captureLoKm:F0}-{captureHiKm:F0} km — about {corridorWidthKm:F0} km wide. Shallower and you skip");
Console.WriteLine("back out to space; deeper and the g-load holes you. That whole margin is the corridor");
Console.WriteLine("the return crews had to hit blind, on a slide-rule reentry angle.");
Console.WriteLine();

// ===================================================================================
// Section D — the fuel-out capture (the Stargate Universe move): capture on drag alone
// ===================================================================================
Console.WriteLine("=== Section D: the fuel-out capture at Jupiter (zero burns after the aim) ===");
const double DvInf = 6000.0;          // arrive fast and hyperbolic, tank dry
double dPeriAltKm = 72.0;             // aim shallow: pass 1 just captures, periapsis stays clear of the surface
double dStart = JupiterR + jupiterAtm.TopAltitude + 3.0e5;
Console.WriteLine($"arrival v_inf = {DvInf / 1000:F1} km/s (hyperbolic, E > 0); aim periapsis {dPeriAltKm:F0} km; no burns after this.");
Console.WriteLine($"{"pass",6}{"peri alt km",13}{"dv shed m/s",14}{"peak g",10}{"energy J/kg",16}{"apoapsis",16}");

ShipState ship = Arrival(JupiterMu, dStart, JupiterR + dPeriAltKm * 1000, DvInf);
var passPosts = new List<ShipState>();                        // post-pass states, for the viz spiral
IReadOnlyList<TrajectorySample> firstPassTrack = [];          // pass 1's real arc, the incoming leg
int captured = 0;
for (int passN = 1; passN <= 12; passN++)
{
    var pass = FlyPass(jupiter, ship, JupiterR, jupiterAtm.TopAltitude);
    if (passN == 1) firstPassTrack = pass.Track;

    double eOut = Energy(pass.Post, JupiterMu);
    double apo = Apoapsis(pass.Post, JupiterMu);
    string apoTxt = pass.Crashed ? "IMPACT (decayed in)"
        : double.IsPositiveInfinity(apo) ? "escapes (still hyperbolic)" : $"{apo / JupiterR:F1} R_J";
    Console.WriteLine($"{passN,6}{pass.MinAlt / 1000,13:F1}{pass.DvShed,14:F0}{pass.PeakDecel / G0,10:F2}{eOut,16:F0}{apoTxt,16}");

    if (pass.Crashed) break;                          // the orbit decayed into the planet — done
    if (eOut < 0)
    {
        passPosts.Add(pass.Post);
        if (captured == 0) captured = passN;          // first pass that turned the orbit bound
        if (apo < 8 * JupiterR) break;                // captured tight enough — stop
    }
    if (eOut >= 0 && passN == 12) break;

    // Advance to the next skim WITHOUT flying the long bound coast (semi-implicit Euler bleeds
    // energy through an e~0.98 ellipse — lesson 02's drift, live). The single-body field is
    // rotationally symmetric, so a drag pass depends only on entry radius + radial/tangential speed,
    // never on angular position: reconstruct the next entry on the +x axis, inbound, carrying the
    // exit orbit's energy and angular momentum EXACTLY. The ledger stays honest; the coast that would
    // corrupt it is skipped by symmetry, not by approximation.
    double hExit = Math.Abs(pass.Post.Position.X * pass.Post.Velocity.Y - pass.Post.Position.Y * pass.Post.Velocity.X);
    double vNext = Math.Sqrt(2 * (eOut + JupiterMu / dStart));
    double vtNext = hExit / dStart;
    double vrNext = -Math.Sqrt(Math.Max(0, vNext * vNext - vtNext * vtNext));
    ship = new ShipState(new Vector2d(dStart, 0), new Vector2d(vrNext, vtNext), 0);
}
Console.WriteLine();
Console.WriteLine(captured > 0
    ? $"Captured on pass {captured}: the atmosphere turned a hyperbolic arrival into a bound orbit with"
    : "Not captured within 12 passes at this aim — go deeper or arrive slower.");
if (captured > 0)
    Console.WriteLine("zero propellant spent after the aim. That is the Destiny's trick: brake on air, not fuel.");
Console.WriteLine();

// ===================================================================================
// Break it yourself
// ===================================================================================
Console.WriteLine("=== Break it yourself ===");

// 1) Double the BC. Only the ratio density/BC appears in the drag law, so doubling BC is IDENTICAL
//    to halving refDensity. Fly the same Section-B corridor pass through a half-density Jupiter.
{
    var halfJupiter = MakeSim("jupiter", JupiterMu, JupiterR,
        jupiterAtm with { RefDensity = jupiterAtm.RefDensity / 2 });
    double rPeri = JupiterR + 80 * 1000;
    var full = FlyPass(jupiter, Arrival(JupiterMu, bStart, rPeri, BvInf), JupiterR, jupiterAtm.TopAltitude);
    var half = FlyPass(halfJupiter, Arrival(JupiterMu, bStart, rPeri, BvInf), JupiterR, jupiterAtm.TopAltitude);
    Console.WriteLine($"1. Double BC (= half density): the 80 km pass sheds {full.DvShed:F0} m/s at stock BC, " +
        $"{half.DvShed:F0} m/s at 2x BC.");
    Console.WriteLine("   Only density/BC matters — the game's BC knob and a body's refDensity are the same dial.");
}

// 2) The corridor at double speed. Same 60 km dip, arrival speed doubled: the Δv shed barely moves
//    (Jupiter's gravity sets the periapsis speed, not v_inf), but the OUTCOME flips — the slow
//    arrival captures, the fast one keeps its excess energy and blows straight back out.
{
    double rPeri = JupiterR + 60 * 1000;
    var slow = FlyPass(jupiter, Arrival(JupiterMu, bStart, rPeri, BvInf), JupiterR, jupiterAtm.TopAltitude);
    var fast = FlyPass(jupiter, Arrival(JupiterMu, bStart, rPeri, 2 * BvInf), JupiterR, jupiterAtm.TopAltitude);
    double eSlow = Energy(slow.Post, JupiterMu), eFast = Energy(fast.Post, JupiterMu);
    string slowOut = eSlow < 0 ? $"CAPTURES (apo {Apoapsis(slow.Post, JupiterMu) / JupiterR:F0} R_J)" : $"exits {Math.Sqrt(2 * eSlow) / 1000:F1} km/s";
    string fastOut = eFast < 0 ? $"captures (apo {Apoapsis(fast.Post, JupiterMu) / JupiterR:F0} R_J)" : $"EXITS {Math.Sqrt(2 * eFast) / 1000:F1} km/s";
    Console.WriteLine($"2. Double speed: the same 60 km dip sheds {slow.DvShed:F0} m/s at {BvInf / 1000:F1} km/s -> {slowOut}, " +
        $"and {fast.DvShed:F0} m/s at {2 * BvInf / 1000:F1} km/s -> {fastOut}.");
    Console.WriteLine("   Nearly the same Δv, opposite result: a fast arrival must dip DEEPER (toward the damage");
    Console.WriteLine("   line) to shed the extra energy — which is exactly why hot aerocapture is dangerous.");
}

// 3) Skim Mercury — no atmosphere field, so the guard skips everything and NOTHING happens.
{
    var mercury = MakeSim("mercury", MercuryMu, MercuryR, atm: null);
    double rPeri = MercuryR + 50 * 1000;
    double mStart = MercuryR + 5e5;
    var (_, rep) = mercury.RunAdaptiveWithDrag(
        Arrival(MercuryMu, mStart, rPeri, 4000), 4000, null, minTimeStep: 0.1, maxTimeStep: 2.0);
    Console.WriteLine($"3. Skim Mercury: dv shed {rep.DeltaVShedMetersPerSecond:F1} m/s, peak {rep.PeakDecelMetersPerSecondSquared:F1} m/s^2, " +
        $"dominant body {(rep.DominantBodyId ?? "none")}.");
    Console.WriteLine("   No atmosphere field -> the drag term is skipped whole. An airless world is a vacuum flyby,");
    Console.WriteLine("   byte-for-byte the trajectory it always flew. Aerobraking needs air; Mercury has none.");
}
Console.WriteLine();
Console.WriteLine("Every number above came from running the probe. Rerun after edits.");

// ===================================================================================
// Viz — Jupiter, the corridor family (faded), the capture spiral (ghost), skim periapsis marks
// ===================================================================================
if (viz is not null)
{
    viz.AddBody(new CelestialBody("jupiter", "Jupiter", null, JupiterMu, JupiterR, 0, 0, 0, Atmosphere: jupiterAtm));

    // The Section-B corridor family: one faded flyby per swept depth (the sweep group, like lab 19).
    // Each arc is the pre-drag hyperbola inbound stitched to the post-drag orbit outbound — the kink
    // at periapsis IS the skim, and the family fans from "barely bent" (shallow) to "captured" (deep).
    foreach (var (altKm, entry, post) in bSweep)
    {
        var arc = new List<TrajectorySample>();
        double tc = 0;
        foreach (Vector2d pt in ConicHalf(entry, JupiterMu, 4 * JupiterR, side: -1)) arc.Add(new TrajectorySample(tc++, pt));
        foreach (Vector2d pt in ConicHalf(post, JupiterMu, 4 * JupiterR, side: +1)) arc.Add(new TrajectorySample(tc++, pt));
        viz.AddPath($"corridor {altKm:F0} km", arc, VizColors.Sweep, "corridor", 1.0, 0.4);
    }

    // The Section-D capture spiral: the real incoming pass 1, then each captured orbit as its exact
    // analytic ellipse (they nest and share a periapsis, so the concatenation is the honest inward
    // spiral). Assembled as one ghost path the scrubber walks from the hyperbolic arrival down into
    // the tightening bound orbit, with a skim marker at each pass's periapsis.
    var spiral = new List<TrajectorySample>();
    double tCursor = 0;
    if (firstPassTrack.Count > 0)
    {
        foreach (TrajectorySample smp in firstPassTrack) spiral.Add(smp);
        tCursor = firstPassTrack[^1].SimTime;
    }
    for (int k = 0; k < passPosts.Count; k++)
    {
        var (pts, peri, period) = EllipseArc(passPosts[k], JupiterMu);
        for (int i = 0; i < pts.Count; i++)
            spiral.Add(new TrajectorySample(tCursor + period * i / (pts.Count - 1), pts[i]));
        viz.AddMarker(tCursor, peri, $"skim {k + 1}", MarkerKinds.Flyby);
        tCursor += period;
    }
    viz.AddPath("fuel-out capture spiral", spiral, VizColors.Trajectory, "capture", 1.8, 1.0, ghost: true);

    LabViz.Show(viz, args);
}
