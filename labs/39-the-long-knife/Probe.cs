// Lab 39 — The long knife (laser / standoff ablation)
//
// Teaching voice: every other deflection touches the rock — the drill lands, the cannonball hits, the driver
// sits on it, the tractor hovers close. The long knife stands OFF and boils the rock with a beam. The ablated
// jet is the thrust; no landing at all. Two honest limits fall out, and they set the whole gameplay shape.
//   A — the STANDOFF budget: a diffraction spot spreads as d·λ/D, so spot intensity falls as 1/d² and the
//       minimum power to keep boiling rises as d² — how far can the knife reach, per platform power?
//   B — THRUST per rock: F = C_m·P (the standard momentum-coupling coefficient), a = F/M — a nanometre-per-
//       second-squared at MW class, same feeble scale as the tractor.
//   C — the HEADLINE: how long / how much power to clear a 140 m rock — continuous ablation, miss = 1.5·a·T².
//       MW-class buys YEARS; a 100 MW late-game upgrade buys months. The technique is POWER-gated.
//   D — cumulative Δv over a fixed burn, and the honest reconciliation (why it's the late-game variant).
//
// NOT a shipped gig — this certifies the physics and names the upgrade-gated "standoff laser" variant it would
// enable. IRONCLAD RULE: every number in the README came from running this probe.

using System.Globalization;
using SpaceSails.Core;

static string F(double v, int d) => v.ToString("F" + d, CultureInfo.InvariantCulture);
static string Sci(double v) => v.ToString("E2", CultureInfo.InvariantCulture);
const double Day = 86400.0;
const double Year = 365.25 * Day;
static string Years(double s) => double.IsInfinity(s) ? "∞" : F(s / (365.25 * 86400.0), 2);

const double SafeMiss = DeflectionGig.SafeMissMeters;   // 3e7 m — clear Ringside on the map
const double Pref = LaserAblation.ReferencePlatformPowerWatts; // 1 MW baseline

RockType[] types = [new(RockComposition.CType), new(RockComposition.SType), new(RockComposition.MType)];

Console.WriteLine("=== Lab 39 — The long knife (laser / standoff ablation) ===");
Console.WriteLine($"Aperture D = {F(LaserAblation.ApertureMeters, 0)} m, λ = {Sci(LaserAblation.WavelengthMeters)} m, ablation flux ≥ {Sci(LaserAblation.AblationThresholdWattsPerM2)} W/m².");
Console.WriteLine($"Momentum coupling C_m = {Sci(LaserAblation.MomentumCouplingNewtonsPerWatt)} N/W. Thrust = C_m·P. Miss = 1.5·a·T² (continuous, lab 37).");
Console.WriteLine($"Target miss to clear Ringside: SafeMiss = {F(SafeMiss, 0)} m.");
Console.WriteLine();

// ---- Section A: the standoff budget ---------------------------------------------------------------
Console.WriteLine("=== Section A: the STANDOFF budget — spot ∝ d, intensity ∝ 1/d², min-power ∝ d² ===");
Console.WriteLine("A diffraction-limited spot spreads to r ≈ d·λ/D. To keep it above the ablation flux you need power ∝ d².");
Console.WriteLine();
Console.WriteLine($"{"standoff",12}{"spot radius (m)",16}{"min power to ablate",20}");
foreach (double d in new[] { 1e3, 1e4, 1e5, 1e6 })
{
    double r = LaserAblation.SpotRadius(d);
    double pmin = LaserAblation.MinPowerToAblate(d);
    string pstr = pmin < 1e3 ? F(pmin, 2) + " W" : pmin < 1e6 ? F(pmin / 1e3, 2) + " kW" : F(pmin / 1e6, 2) + " MW";
    Console.WriteLine($"{Sci(d) + " m",12}{Sci(r),16}{pstr,20}");
}
Console.WriteLine();
Console.WriteLine($"Max reach of a {F(Pref / 1e6, 0)} MW platform: {Sci(LaserAblation.MaxStandoff(Pref))} m ({F(LaserAblation.MaxStandoff(Pref) / 1000.0, 0)} km).");
Console.WriteLine($"Max reach of a 100 MW platform:  {Sci(LaserAblation.MaxStandoff(100 * Pref))} m ({F(LaserAblation.MaxStandoff(100 * Pref) / 1000.0, 0)} km).");
Console.WriteLine();

// ---- Section B: thrust per rock -------------------------------------------------------------------
Console.WriteLine("=== Section B: THRUST and acceleration per rock — F = C_m·P, a = F/M ===");
Console.WriteLine($"At the {F(Pref / 1e6, 0)} MW reference: thrust F = {F(LaserAblation.Thrust(Pref), 1)} N (the same for every rock — it's the jet).");
Console.WriteLine("The acceleration is what differs, and it's feeble — a fraction of a nanometre per second squared.");
Console.WriteLine();
Console.WriteLine($"{"type",-20}{"radius",10}{"rock mass (kg)",16}{"accel a (m/s²)",18}");
foreach (RockType t in types)
{
    foreach (double r in new[] { 140.0, 1000.0 })
    {
        double m = KineticImpactor.AsteroidMass(t, r);
        double a = LaserAblation.Acceleration(Pref, m);
        Console.WriteLine($"{t.Label,-20}{F(r, 0) + " m",10}{Sci(m),16}{Sci(a),18}");
    }
}
Console.WriteLine();

// ---- Section C: the headline ----------------------------------------------------------------------
Console.WriteLine("=== Section C: the HEADLINE — how long / how much power to clear a 140 m rock ===");
Console.WriteLine("Continuous ablation held over the warning: miss = 1.5·a·T². Solve for T at each platform power.");
Console.WriteLine("The knife is POWER-gated — MW-class buys years, a 100 MW upgrade buys months.");
Console.WriteLine();
var sRock = new RockType(RockComposition.SType);
Console.WriteLine($"{"platform power",16}{"thrust (N)",12}{"accel (m/s²)",16}{"clear time (yr)",18}");
foreach (double p in new[] { Pref, 10 * Pref, 100 * Pref, 1000 * Pref })
{
    double a = LaserAblation.Acceleration(p, KineticImpactor.AsteroidMass(sRock, 140.0));
    double t = LaserAblation.RequiredBurnSeconds(sRock, 140.0, p, SafeMiss);
    string pstr = p < 1e9 ? F(p / 1e6, 0) + " MW" : F(p / 1e9, 1) + " GW";
    Console.WriteLine($"{pstr,16}{F(LaserAblation.Thrust(p), 0),12}{Sci(a),16}{Years(t),18}");
}
Console.WriteLine();

// ---- Section D: cumulative Δv over a fixed burn, and reconciliation --------------------------------
Console.WriteLine("=== Section D: cumulative Δv over a fixed 1-year burn (140 m S-type, per platform power) ===");
Console.WriteLine("Δv = a·t. Whether that clears Ringside depends on when it lands — here run the whole year continuously.");
Console.WriteLine();
Console.WriteLine($"{"platform power",16}{"Δv in 1 yr (m/s)",18}{"miss opened (m)",16}{"clears?",10}");
foreach (double p in new[] { Pref, 10 * Pref, 100 * Pref, 1000 * Pref })
{
    double m = KineticImpactor.AsteroidMass(sRock, 140.0);
    double dv = LaserAblation.CumulativeDeltaV(p, m, Year);
    double miss = GravityTractor.Miss(LaserAblation.Acceleration(p, m), Year);
    string pstr = p < 1e9 ? F(p / 1e6, 0) + " MW" : F(p / 1e9, 1) + " GW";
    Console.WriteLine($"{pstr,16}{F(dv, 4),18}{Sci(miss),16}{(miss >= SafeMiss ? "yes" : "no"),10}");
}
Console.WriteLine();
Console.WriteLine("GAMEPLAY HOOK enabled: an UPGRADE-GATED late-game \"standoff laser\" deflection variant — no landing, no");
Console.WriteLine("drilling, works at range, but gated on the power plant you can bring: a starter MW rig takes years, the");
Console.WriteLine("100 MW upgrade takes months. Honest reconciliation: diffraction lets a modest platform reach hundreds to");
Console.WriteLine("thousands of km, so STANDOFF is cheap; the real cost is POWER for THRUST — the long knife is the reward for");
Console.WriteLine("investing in a reactor, and (like the tractor and the driver) it still rewards early detection.");
