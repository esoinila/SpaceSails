// Lab 36 — The cannonball (kinetic impactor)
//
// Teaching voice: the ablation gig (lab 35) drills and boils. The cannonball is the other classic: hurl a
// mass at the rock and let momentum — amplified by the ejecta plume it blows off — nudge the orbit, then let
// the WARNING TIME multiply the nudge. This lab prices it honestly at REAL asteroid sizes (the gig's on-map
// rock is a 4000 km camera abstraction; a real Ringside-killer is 50 m – 1 km).
//   A — asteroid mass by Zubrin type and size, and the ejecta enhancement β (DART/Dimorphos: β≈3.6 on a
//       real S-type — the plume did most of the work; C over-delivers, M under).
//   B — the along-track LEVERAGE: an along-track Δv opens a miss of 3·Δv·t, so early detection is the whole
//       game — the same nudge four times as early misses four times as far.
//   C — the SACRIFICE table: how much WARNING a jettisoned cargo pod (~20 t) or an old hull (~200 t) buys
//       against each rock, to clear it past Ringside (SafeMiss = 3e7 m).
//   D — the required-mass view: at a fixed lead, how heavy a slug each rock demands.
//
// NOT a shipped gig — this certifies the physics and names the gig variant it would enable. IRONCLAD RULE:
// every number in the README came from running this probe.

using System.Globalization;
using SpaceSails.Core;

static string F(double v, int d) => v.ToString("F" + d, CultureInfo.InvariantCulture);
static string Sci(double v) => v.ToString("E2", CultureInfo.InvariantCulture);
const double Day = 86400.0;
const double Year = 365.25 * Day;
static string Years(double s) => double.IsInfinity(s) ? "∞" : F(s / (365.25 * 86400.0), 2);

const double SafeMiss = DeflectionGig.SafeMissMeters;   // 3e7 m — clear Ringside on the map
const double u = KineticImpactor.ReferenceClosingSpeed; // 6 km/s intercept, DART-scale

RockType[] types = [new(RockComposition.CType), new(RockComposition.SType), new(RockComposition.MType)];

Console.WriteLine("=== Lab 36 — The cannonball (kinetic impactor) ===");
Console.WriteLine($"Target miss to clear Ringside: SafeMiss = {F(SafeMiss, 0)} m. Intercept closing speed u = {F(u, 0)} m/s");
Console.WriteLine($"(DART struck at {F(KineticImpactor.DartImpactVelocity, 0)} m/s). The gig's on-map rock is 4000 km (the camera); these are REAL sizes.");
Console.WriteLine();

// ---- Section A: mass and β per type/size ---------------------------------------------------------
Console.WriteLine("=== Section A: asteroid mass by type + size, and the ejecta enhancement β ===");
Console.WriteLine("Mass = ρ·(4/3)πr³. β>1 is the ejecta plume adding thrust; DART measured β≈3.6 on S-type Dimorphos.");
Console.WriteLine();
Console.WriteLine($"{"type",-20}{"ρ kg/m³",10}{"β",8}{"r=50m",14}{"r=140m",14}{"r=370m",14}{"r=1000m",14}   (mass kg)");
foreach (RockType t in types)
{
    string row = $"{t.Label,-20}{F(KineticImpactor.BulkDensity(t.Composition), 0),10}{F(KineticImpactor.Beta(t.Composition), 1),8}";
    foreach (double r in KineticImpactor.RealisticRadiiMeters)
    {
        row += $"{Sci(KineticImpactor.AsteroidMass(t, r)),14}";
    }
    Console.WriteLine(row);
}
Console.WriteLine();

// ---- Section B: the along-track leverage ---------------------------------------------------------
Console.WriteLine("=== Section B: the along-track LEVERAGE — miss = 3·Δv·t, so warning is everything ===");
Console.WriteLine("An old hull (200 t) hits an S-type r=140m. The SAME Δv opens a wider miss the earlier it lands:");
Console.WriteLine();
var sRock = new RockType(RockComposition.SType);
double sMass = KineticImpactor.AsteroidMass(sRock, 140.0);
double dvHull = KineticImpactor.DeltaV(KineticImpactor.OldHullMassKg, u, sMass, KineticImpactor.Beta(RockComposition.SType));
Console.WriteLine($"  hull Δv imparted = {F(dvHull * 1000.0, 3)} mm/s (β={F(KineticImpactor.Beta(RockComposition.SType), 1)}, M={Sci(sMass)} kg)");
Console.WriteLine();
Console.WriteLine($"{"lead time",14}{"miss (m)",16}{"clears Ringside?",18}");
foreach (double lead in new[] { 30 * Day, 100 * Day, Year, 3 * Year, 10 * Year })
{
    double miss = KineticImpactor.AlongTrackMiss(dvHull, lead);
    string label = lead < Year ? $"{F(lead / Day, 0)} d" : $"{Years(lead)} yr";
    Console.WriteLine($"{label,14}{Sci(miss),16}{(miss >= SafeMiss ? "yes" : "no"),18}");
}
Console.WriteLine();

// ---- Section C: the SACRIFICE table — warning a slug buys ----------------------------------------
Console.WriteLine("=== Section C: the SACRIFICE — how much WARNING a pod or a hull buys to clear each rock ===");
Console.WriteLine($"Cargo pod = {F(KineticImpactor.CargoPodMassKg / 1000.0, 0)} t; old hull = {F(KineticImpactor.OldHullMassKg / 1000.0, 0)} t. Lead time (yr) required to open a {F(SafeMiss, 0)} m miss.");
Console.WriteLine("'∞' = the slug is too light to ever reach the miss at this intercept speed (it can't; throw more mass).");
Console.WriteLine();
Console.WriteLine($"{"type",-20}{"radius",10}{"pod (yr warn)",16}{"hull (yr warn)",16}");
foreach (RockType t in types)
{
    foreach (double r in KineticImpactor.RealisticRadiiMeters)
    {
        double podLead = KineticImpactor.RequiredLeadSeconds(t, r, KineticImpactor.CargoPodMassKg, SafeMiss, u);
        double hullLead = KineticImpactor.RequiredLeadSeconds(t, r, KineticImpactor.OldHullMassKg, SafeMiss, u);
        Console.WriteLine($"{t.Label,-20}{F(r, 0) + " m",10}{Years(podLead),16}{Years(hullLead),16}");
    }
}
Console.WriteLine();

// ---- Section D: the required-mass view -----------------------------------------------------------
Console.WriteLine("=== Section D: required slug MASS at a fixed 1-year warning ===");
Console.WriteLine($"How heavy a slug clears each rock past Ringside with {Years(Year)} yr of warning (t = tonnes).");
Console.WriteLine();
Console.WriteLine($"{"type",-20}{"radius",10}{"slug needed (t)",18}{"= how many hulls",18}");
foreach (RockType t in types)
{
    foreach (double r in KineticImpactor.RealisticRadiiMeters)
    {
        double mReq = KineticImpactor.RequiredImpactorMass(t, r, SafeMiss, Year, u);
        Console.WriteLine($"{t.Label,-20}{F(r, 0) + " m",10}{F(mReq / 1000.0, 1),18}{F(mReq / KineticImpactor.OldHullMassKg, 1),18}");
    }
}
Console.WriteLine();
Console.WriteLine("GAMEPLAY HOOK enabled: a \"sacrifice a cargo pod / an old hull as the slug\" deflection variant. The");
Console.WriteLine("lesson the numbers teach: the cannonball rewards WARNING (3× per unit lead) far more than brute mass —");
Console.WriteLine("a small pod thrown early beats a heavy hull thrown late. Early detection is the cheapest deflection.");
