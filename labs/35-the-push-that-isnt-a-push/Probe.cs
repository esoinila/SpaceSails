// Lab 35 — The push that isn't a push (ablation)
//
// Teaching voice: the shipped #399/#401 deflection gig ablates an inbound rock with a drilled charge —
// the charge doesn't shove like a rocket, it boils off a jet of rock whose recoil lifts the orbit's low
// point (the periapsis) off the station's lane. This lab CERTIFIES that shipped Core (DeflectionGig): it
// prints the very numbers the gig's constants encode, so a change to the gig that breaks the physics
// shows up here and in the pinned tests.
//   A — the cos law of rotation: the bore only shoves the right way when it faces the required heading;
//       fire off-window and the raised-cosine alignment wastes the charge.
//   B — asteroid TYPE (Zubrin C/S/M): drill hardness and ablation efficiency, honestly costed.
//   C — periapsis-raise = honest MISS: the deflection raises the low point, and the closest approach on
//       the real Ringside Kepler rail equals the raise (no cheating the geometry).
//   D — the success BANDS and the heroic PAYOUT the miss reads into.
//   E — the DART/Dimorphos reality check: ejecta can AMPLIFY the push (β>1) — reconciled honestly.
//   F — the Electric-Universe impact-flash model (ImpactArcFlash), FLAGGED non-mainstream game canon.
//
// IRONCLAD RULE: every number in labs/35-the-push-that-isnt-a-push/README.md came from running this
// probe. Change the code, rerun, re-paste — never hand-edit a table.

using System.Globalization;
using SpaceSails.Core;

static string F(double v, int d) => v.ToString("F" + d, CultureInfo.InvariantCulture);

// The canonical target: the Ringside Exchange on its real circular orbit around Saturn (scenarios/sol.json,
// matching DeflectionGigTests).
const double RingRadius = 1.35e9;
const double RingPeriod = 1600600.0;
const double RingPhase = 5.0;
const double Impact = DeflectionGig.RailLeadSeconds;

DeflectionGig.RockRail Rail() => DeflectionGig.BuildRail(RingRadius, RingPeriod, RingPhase, Impact);
double Miss(DeflectionGig.RockRail r) => DeflectionGig.MissDistanceMeters(r, RingRadius, RingPeriod, RingPhase, Impact);

RockType[] types = [new(RockComposition.CType), new(RockComposition.SType), new(RockComposition.MType)];

Console.WriteLine("=== Lab 35 — The push that isn't a push (ablation): CERTIFYING the shipped DeflectionGig ===");
Console.WriteLine();

// ---- Section A: the cos law of rotation ----------------------------------------------------------
Console.WriteLine("=== Section A: the rotation cos law — the bore only shoves when it faces the heading ===");
Console.WriteLine("RotationAlignment = 0.5·(1+cos θ): 1 aligned, 0 on the far side. The client holds the charge");
Console.WriteLine($"until alignment ≥ FiringWindowAlignment = {F(DeflectionGig.FiringWindowAlignment, 2)}, so a clean run fires ~1.0.");
Console.WriteLine();
Console.WriteLine($"{"spin fraction",16}{"alignment",12}{"in window?",12}");
double spin = 120.0; // an arbitrary spin period (s); only the fraction matters
foreach (double frac in new[] { 0.00, 0.10, 0.20, 0.25, 0.40, 0.50 })
{
    double a = DeflectionGig.RotationAlignment(spin, 0.0, frac * spin);
    Console.WriteLine($"{F(frac, 2),16}{F(a, 3),12}{(a >= DeflectionGig.FiringWindowAlignment ? "yes" : "no"),12}");
}
Console.WriteLine();

// ---- Section B: asteroid TYPE — drill + ablation -------------------------------------------------
Console.WriteLine("=== Section B: asteroid TYPE (Zubrin C/S/M) — drill hardness and ablation efficiency ===");
Console.WriteLine($"Drill base = {F(DeflectionGig.DrillBaseSeconds, 1)}s (S-type). Ablation ceiling = {F(DeflectionGig.MaxPeriapsisRaiseMeters, 0)} m periapsis raise.");
Console.WriteLine();
Console.WriteLine($"{"type",-22}{"drill (s)",12}{"ablation eff",14}{"full-charge raise (m)",24}{"band",16}");
foreach (RockType t in types)
{
    double drill = DeflectionGig.RockProfile.DrillSeconds(t);
    double eff = DeflectionGig.RockProfile.AblationEfficiency(t);
    double flawless = DeflectionGig.PeriapsisRaiseForBurn(t, chargeFraction: 1.0, rotationAlignment: 1.0);
    DeflectionOutcome band = DeflectionGig.Classify(flawless);
    Console.WriteLine($"{t.Label,-22}{F(drill, 1),12}{F(eff, 2),14}{F(flawless, 0),24}{band,16}");
}
Console.WriteLine();
Console.WriteLine("The M-type is the stubborn worst case: a FULL, perfectly-aligned charge JUST clears; anything");
Console.WriteLine("short only grazes. A C-type clears with wide margin. (Owner: \"bring a bigger charge\" for metal.)");
Console.WriteLine();

// ---- Section C: periapsis-raise = honest miss ----------------------------------------------------
Console.WriteLine("=== Section C: the deflection is HONEST — a periapsis raise of ΔR yields a miss of ΔR ===");
Console.WriteLine("Flown on the real Ringside Kepler rail: undeflected the rock hits; raising periapsis by ΔR opens");
Console.WriteLine("the closest approach to exactly ΔR — the map's lifted rail is the true miss, not a fudge.");
Console.WriteLine();
Console.WriteLine($"{"raise ΔR (m)",16}{"measured miss (m)",20}{"error",12}{"band",16}");
double undeflected = Miss(Rail());
Console.WriteLine($"{"0 (undeflected)",16}{F(undeflected, 0),20}{"—",12}{DeflectionGig.Classify(undeflected),16}");
foreach (double raise in new[] { 8.0e6, 3.0e7, 4.5e7 })
{
    double miss = Miss(DeflectionGig.RaisePeriapsis(Rail(), raise));
    Console.WriteLine($"{F(raise, 0),16}{F(miss, 0),20}{F(100.0 * (miss - raise) / raise, 2) + "%",12}{DeflectionGig.Classify(miss),16}");
}
Console.WriteLine();

// ---- Section D: success bands + heroic payout ----------------------------------------------------
Console.WriteLine("=== Section D: the success BANDS and the heroic PAYOUT ===");
Console.WriteLine($"Safe miss ≥ {F(DeflectionGig.SafeMissMeters, 0)} m (full); graze ≥ {F(DeflectionGig.GrazeMissMeters, 0)} m; below = impact (Ringside survives).");
Console.WriteLine();
int full = DeflectionGig.Total(DeflectionGig.BaseFee, RingRadius, RingRadius, DeflectionOutcome.FullDeflection, 0);
int graze = DeflectionGig.Total(DeflectionGig.BaseFee, RingRadius, RingRadius, DeflectionOutcome.GrazingMiss, 0);
int impact = DeflectionGig.Total(DeflectionGig.BaseFee, RingRadius, RingRadius, DeflectionOutcome.Impact, 0);
Console.WriteLine($"{"outcome",-18}{"payout (cr)",14}");
Console.WriteLine($"{"full deflection",-18}{full,14}");
Console.WriteLine($"{"grazing miss",-18}{graze,14}");
Console.WriteLine($"{"impact / abort",-18}{impact,14}  (floor {DeflectionGig.Floor}; per crew lost −{DeflectionGig.PerCrewLostPenalty})");
Console.WriteLine();

// ---- Section E: the DART/Dimorphos reality check -------------------------------------------------
Console.WriteLine("=== Section E: the DART reality check — ejecta can AMPLIFY, not absorb (honest science) ===");
Console.WriteLine("DART hit Dimorphos (a real S-type) in 2022 and deflected it ~3.6× bare momentum: the loose surface");
Console.WriteLine("threw an ejecta plume that ADDED thrust. So the honest result is the opposite of \"rubble absorbs");
Console.WriteLine("the push\" — a volatile-rich C-type can OVER-deliver. The gig encodes this as ablation efficiency:");
Console.WriteLine();
Console.WriteLine($"{"type",-22}{"ablation eff",-14}{"reading"}");
foreach (RockType t in types)
{
    double eff = DeflectionGig.RockProfile.AblationEfficiency(t);
    string reading = t.Composition switch
    {
        RockComposition.CType => "eager — volatiles flash, plume amplifies",
        RockComposition.MType => "stubborn — dense metal resists ablation",
        _ => "the firm middle (DART's measured class)",
    };
    Console.WriteLine($"{t.Label,-22}{F(eff, 2),-14}{reading}");
}
Console.WriteLine();

// ---- Section F: the Electric-Universe impact-flash model -----------------------------------------
Console.WriteLine("=== Section F: the EU impact-flash model (FLAGGED non-mainstream game canon) ===");
Console.WriteLine($"SpaceSails runs Electric-Universe rules (#369). Owner's arc-melter reference: {F(ImpactArcFlash.ReferenceArcAmps, 0)} A across a");
Console.WriteLine($"{F(ImpactArcFlash.ReferenceArcVolts / 1000.0, 0)} kV spark ⇒ {F(ImpactArcFlash.ReferenceArcPowerWatts / 1e6, 1)} MW of arc power. The flash scales with CONDUCTIVITY, so the");
Console.WriteLine("metallic M-type arcs hardest and flashes brightest. Mainstream attributes flash to vaporisation");
Console.WriteLine("alone — this is the game's licence to be electric, LABELLED as such.");
Console.WriteLine();
Console.WriteLine($"{"type",-22}{"conductivity",14}{"flash × (vs kinetic)",22}");
foreach (RockType t in types)
{
    Console.WriteLine($"{t.Label,-22}{F(ImpactArcFlash.ConductivityClass(t.Composition), 2),14}{F(ImpactArcFlash.ArcFlashMultiplier(t.Composition), 2),22}");
}
Console.WriteLine();
Console.WriteLine("GAMEPLAY HOOK certified: this IS the shipped #399/#401 ablation gig — drill, cos-law fire, per-type");
Console.WriteLine("cost, honest miss, heroic pay. The arc-flash multiplier is logged for a future EU visual/deflection");
Console.WriteLine("kick where an M-type's bigger arc lends a bigger shove (owner's charged-asteroid canon).");
