// Lab 37 — The slow hand (gravity tractor)
//
// Teaching voice: no charge, no collision. Park a heavy tug a short way off the rock and STATION-KEEP there
// for days, weeks, years — the tug's own gravity tows the orbit over. It is the gentlest technique and the
// most demanding of TIME, and the number that falls out is the whole lesson: the pull is so feeble that the
// slow hand only works with YEARS of warning. Early detection is not a nicety here, it is the entire method.
//   A — the tug acceleration a = G·m_ship/d², and the continuous-tow miss = 1.5·a·T².
//   B — how EARLY you must arrive (lead time) to clear each rock past Ringside — the headline.
//   C — ship-mass sensitivity: a heavier tug (or ballast) shortens the wait, but only as √mass.
//   D — the HOVER bill: to not fall in, the tug thrusts against the rock's pull continuously — the Lab-25
//       station-keeping discipline, here priced as a tiny force sustained for a very long time.
//
// NOT a shipped gig — this certifies the physics and names the early-detection lesson it teaches. IRONCLAD
// RULE: every number in the README came from running this probe.

using System.Globalization;
using SpaceSails.Core;

static string F(double v, int d) => v.ToString("F" + d, CultureInfo.InvariantCulture);
static string Sci(double v) => v.ToString("E2", CultureInfo.InvariantCulture);
static string Years(double s) => double.IsInfinity(s) ? "∞" : F(s / (365.25 * 86400.0), 2);

const double SafeMiss = DeflectionGig.SafeMissMeters;   // 3e7 m — clear Ringside on the map
const double mShip = GravityTractor.ReferenceShipMassKg;

RockType[] types = [new(RockComposition.CType), new(RockComposition.SType), new(RockComposition.MType)];

Console.WriteLine("=== Lab 37 — The slow hand (gravity tractor) ===");
Console.WriteLine($"Reference tug mass = {F(mShip / 1000.0, 0)} t, standoff = {F(GravityTractor.StandoffFactor, 1)}·radius, G = {Sci(GravityTractor.G)}.");
Console.WriteLine($"Target miss to clear Ringside: SafeMiss = {F(SafeMiss, 0)} m. Continuous-tow miss = 1.5·a·T².");
Console.WriteLine();

// ---- Section A: the tug acceleration --------------------------------------------------------------
Console.WriteLine("=== Section A: the feeble pull — a = G·m_ship/d² ===");
Console.WriteLine("The 100 t tug's gravity, at 1.5 radii off each real rock. This is the entire thrust of the technique.");
Console.WriteLine();
Console.WriteLine($"{"radius",10}{"standoff (m)",14}{"accel a (m/s²)",18}");
foreach (double r in KineticImpactor.RealisticRadiiMeters)
{
    double d = GravityTractor.Standoff(r);
    double a = GravityTractor.TugAcceleration(mShip, d);
    Console.WriteLine($"{F(r, 0) + " m",10}{F(d, 1),14}{Sci(a),18}");
}
Console.WriteLine();

// ---- Section B: how early you must arrive ---------------------------------------------------------
Console.WriteLine("=== Section B: how EARLY you must arrive — lead time to clear each rock (YEARS) ===");
Console.WriteLine("Solve SafeMiss = 1.5·a·T² for T. Note: the tug's pull is the same regardless of the rock's mass");
Console.WriteLine("(gravity acts on the ship's mass, not the rock's) — so the ONLY thing that changes the wait is the");
Console.WriteLine("standoff, i.e. the rock's SIZE. A bigger rock means a bigger standoff, a weaker pull, a longer wait.");
Console.WriteLine();
Console.WriteLine($"{"radius",10}{"accel (m/s²)",16}{"lead required (yr)",20}");
foreach (double r in KineticImpactor.RealisticRadiiMeters)
{
    double a = GravityTractor.TugAcceleration(mShip, GravityTractor.Standoff(r));
    double lead = GravityTractor.RequiredLeadSeconds(a, SafeMiss);
    Console.WriteLine($"{F(r, 0) + " m",10}{Sci(a),16}{Years(lead),20}");
}
Console.WriteLine();

// ---- Section C: ship-mass sensitivity -------------------------------------------------------------
Console.WriteLine("=== Section C: ship-mass sensitivity — a heavier tug (or ballast) helps only as √mass ===");
Console.WriteLine("Lead required for an S-type r=140 m rock, swept over tug mass. Doubling the tug cuts the wait by √2,");
Console.WriteLine("not by half — the slow hand cannot be muscled, only started early.");
Console.WriteLine();
Console.WriteLine($"{"tug mass (t)",14}{"accel (m/s²)",16}{"lead required (yr)",20}");
double r140 = 140.0;
foreach (double m in new[] { 1.0e4, 5.0e4, 1.0e5, 5.0e5, 1.0e6 })
{
    double a = GravityTractor.TugAcceleration(m, GravityTractor.Standoff(r140));
    double lead = GravityTractor.RequiredLeadSeconds(a, SafeMiss);
    Console.WriteLine($"{F(m / 1000.0, 0),14}{Sci(a),16}{Years(lead),20}");
}
Console.WriteLine();

// ---- Section D: the HOVER bill (Lab 25 tie) ------------------------------------------------------
Console.WriteLine("=== Section D: the HOVER bill — station-keeping so the tug doesn't fall in (the Lab-25 tie) ===");
Console.WriteLine("To hover, the tug must thrust to cancel the ROCK's pull on IT: F = m_ship·G·M_rock/d². Tiny force,");
Console.WriteLine("but sustained for the whole multi-year tow — the slow hand's cost is DURATION, not Δv.");
Console.WriteLine();
Console.WriteLine($"{"type",-20}{"radius",10}{"rock mass (kg)",16}{"hover thrust (N)",18}");
foreach (RockType t in types)
{
    foreach (double r in new[] { 140.0, 1000.0 })
    {
        double mRock = KineticImpactor.AsteroidMass(t, r);
        double thrust = GravityTractor.HoverThrust(mShip, mRock, GravityTractor.Standoff(r));
        Console.WriteLine($"{t.Label,-20}{F(r, 0) + " m",10}{Sci(mRock),16}{F(thrust, 2),18}");
    }
}
Console.WriteLine();
Console.WriteLine("GAMEPLAY HOOK enabled: a gravity-tractor gig variant that REWARDS EARLY DETECTION — arrive years");
Console.WriteLine("ahead and a gentle hover saves the port for zero collision risk; arrive late and the slow hand is");
Console.WriteLine("useless (the cannonball or the drilled charge is your only option). It also composes with Lab 25:");
Console.WriteLine("the hover IS station-keeping, a standoff held instead of a park.");
