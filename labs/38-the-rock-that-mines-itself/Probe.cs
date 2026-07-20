// Lab 38 — The rock that mines itself (mass driver on the rock)
//
// Teaching voice: the cannonball (lab 36) throws mass AT the rock; the tractor (lab 37) tows it with gravity.
// This one lands a driver rig and throws the rock's OWN mass off the far side — the rock is both ship and fuel.
// The Luna mass-driver canon (lab 30) turned on the threat. The whole lesson is the rocket equation with the
// rock as tankage, and the honest engineering cost that falls out of it.
//   A — the rocket equation Δv = v_ex·ln(M0/M_final): how big a Δv each fling FRACTION of the rock buys.
//   B — the SELF-MINE table: to clear Ringside (SafeMiss = 3e7 m) with a year of lead, how much of each rock
//       you fling (fraction AND absolute tonnes) — the fraction is tiny, the tonnage is not.
//   C — the RUN CLOCK: at a believable rig throughput, how long the reactor throws — days for a small rock,
//       YEARS for a big one (longer than the warning: the honest negative — a slow, multi-visit gig).
//   D — the REACTOR bill: the power to fling regolith at the driver's muzzle speed (tens of MW).
//
// NOT a shipped gig — this certifies the physics and names the "land a driver and eat the rock" engineering
// variant it would enable. IRONCLAD RULE: every number in the README came from running this probe.

using System.Globalization;
using SpaceSails.Core;

static string F(double v, int d) => v.ToString("F" + d, CultureInfo.InvariantCulture);
static string Sci(double v) => v.ToString("E2", CultureInfo.InvariantCulture);
const double Day = 86400.0;
const double Year = 365.25 * Day;
static string Years(double s) => double.IsInfinity(s) ? "∞" : F(s / (365.25 * 86400.0), 2);
static string Days(double s) => double.IsInfinity(s) ? "∞" : F(s / 86400.0, 2);

const double SafeMiss = DeflectionGig.SafeMissMeters;            // 3e7 m — clear Ringside on the map
const double vEx = RockMassDriver.ExhaustVelocityMetersPerSecond; // 2500 m/s rock-rig muzzle
const double thru = RockMassDriver.ReferenceThroughputKgPerSecond; // 20 kg/s

RockType[] types = [new(RockComposition.CType), new(RockComposition.SType), new(RockComposition.MType)];

Console.WriteLine("=== Lab 38 — The rock that mines itself (mass driver on the rock) ===");
Console.WriteLine($"The rock is both ship and fuel: Δv = v_ex·ln(M0/M_final). Muzzle v_ex = {F(vEx, 0)} m/s");
Console.WriteLine($"(Luna's compute-pod driver, lab 30, throws at ~3200 m/s). Reference rig throughput = {F(thru, 0)} kg/s.");
Console.WriteLine($"Target miss to clear Ringside: SafeMiss = {F(SafeMiss, 0)} m. Deflection Δv is along-track (miss = 3·Δv·t).");
Console.WriteLine();

// ---- Section A: the rocket equation ---------------------------------------------------------------
Console.WriteLine("=== Section A: the rocket equation — Δv each fling FRACTION of the rock buys ===");
Console.WriteLine("Δv = v_ex·ln(M0/M_final). Throw off a fraction f and the rest gains v_ex·ln(1/(1−f)). Tiny f, tiny Δv.");
Console.WriteLine();
Console.WriteLine($"{"fraction flung",16}{"Δv gained (m/s)",18}");
foreach (double f in new[] { 1e-5, 1e-4, 1e-3, 1e-2, 0.1 })
{
    double dv = RockMassDriver.DeltaV(vEx, 1.0, 1.0 - f);
    Console.WriteLine($"{F(f * 100.0, 3) + " %",16}{F(dv, 4),18}");
}
Console.WriteLine();

// ---- Section B: the self-mine table ---------------------------------------------------------------
Console.WriteLine("=== Section B: the SELF-MINE — fling to clear Ringside with 1 year of warning ===");
Console.WriteLine($"Required Δv at {Years(Year)} yr lead = {F(KineticImpactor.RequiredDeltaV(SafeMiss, Year), 4)} m/s. Mass flung = M0·(1−e^(−Δv/v_ex)).");
Console.WriteLine("The fraction is a rounding error; the absolute mass is kilotonnes to megatonnes.");
Console.WriteLine();
Console.WriteLine($"{"type",-20}{"radius",10}{"rock mass M0 (kg)",18}{"fraction flung",16}{"mass flung (t)",18}");
foreach (RockType t in types)
{
    foreach (double r in KineticImpactor.RealisticRadiiMeters)
    {
        double m0 = KineticImpactor.AsteroidMass(t, r);
        double dv = KineticImpactor.RequiredDeltaV(SafeMiss, Year);
        double frac = RockMassDriver.MassFractionFlung(dv, vEx);
        double flung = RockMassDriver.MassToDeflect(t, r, SafeMiss, Year, vEx);
        Console.WriteLine($"{t.Label,-20}{F(r, 0) + " m",10}{Sci(m0),18}{F(frac * 100.0, 4) + " %",16}{Sci(flung / 1000.0),18}");
    }
}
Console.WriteLine();

// ---- Section C: the run clock ---------------------------------------------------------------------
Console.WriteLine("=== Section C: the RUN CLOCK — how long the rig throws (at 20 kg/s), by warning time ===");
Console.WriteLine("Run time = mass flung / throughput. Compare it to the WARNING: if run > lead, the rig can't finish");
Console.WriteLine("in time (an honest negative) — the mass driver is a long-warning, multi-visit engineering gig.");
Console.WriteLine();
Console.WriteLine($"{"type",-20}{"radius",10}{"lead",10}{"run time",14}{"finishes in time?",18}");
foreach (RockType t in new[] { new RockType(RockComposition.SType), new RockType(RockComposition.MType) })
{
    foreach (double r in new[] { 140.0, 1000.0 })
    {
        foreach (double lead in new[] { Year, 5 * Year })
        {
            double run = RockMassDriver.RunSecondsToDeflect(t, r, SafeMiss, lead, thru, vEx);
            string runStr = run < Year ? Days(run) + " d" : Years(run) + " yr";
            Console.WriteLine($"{t.Label,-20}{F(r, 0) + " m",10}{Years(lead) + " yr",10}{runStr,14}{(run <= lead ? "yes" : "NO"),18}");
        }
    }
}
Console.WriteLine();

// ---- Section D: the reactor bill ------------------------------------------------------------------
Console.WriteLine("=== Section D: the REACTOR bill — power to fling regolith at the muzzle speed ===");
Console.WriteLine("P = ṁ·½·v_ex² — the kinetic-energy rate poured into the jet. This is the reactor the away-rig must haul.");
Console.WriteLine();
Console.WriteLine($"{"throughput (kg/s)",18}{"driver power (MW)",18}");
foreach (double m in new[] { 5.0, 20.0, 100.0, 500.0 })
{
    double p = RockMassDriver.DriverPowerWatts(m, vEx);
    Console.WriteLine($"{F(m, 0),18}{F(p / 1e6, 2),18}");
}
Console.WriteLine();
Console.WriteLine("GAMEPLAY HOOK enabled: a \"land a driver and eat the rock\" MULTI-VISIT engineering gig — no ammunition to");
Console.WriteLine("haul (the rock is the fuel), reusable across visits, ties to the Luna mass-driver canon (lab 30) and the");
Console.WriteLine("away-mission rig hauling. Its honest limit, priced above: throughput and warning gate it — big rocks on");
Console.WriteLine("short notice are out of reach, so like the tractor (lab 37) it REWARDS EARLY DETECTION.");
