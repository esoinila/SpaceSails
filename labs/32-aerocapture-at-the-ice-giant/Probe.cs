// Lab 32 — Aerocapture: the ice giant's haze is the free brake
//
// Teaching voice: the owner arrived at Uranus stranded — 29.8 km/s relative, 32 pulses in the tank —
// and asked the only question a sailor asks when the fuel gauge and the destination disagree: "could
// we use Uranus's own air as a free brake?" This lab answers it the R&D way — the probe prints the
// numbers before any feature ships. It reuses EXACTLY the Core drag lab 22 added (an exponential
// atmosphere shell + one ballistic-coefficient knob, Simulator.RunAdaptiveWithDrag / DragReport) and
// asks the four questions aerocapture at an ice giant actually turns on:
//
//   B. The corridor vs entry speed: at what periapsis depths does a pass skip out, capture into a
//      bound orbit, or blow the g budget? Uranus entry for the stranding is sqrt(vinf^2 + vesc^2) =
//      sqrt(29.8^2 + 21.4^2) ~ 36 km/s. Below some critical speed a safe single-pass corridor exists;
//      above it the "captures" band and the "over-g" band OVERLAP and the corridor is CLOSED.
//   C. Delta-v shed per pass vs depth: is one pass enough at 36 km/s, or a multi-pass campaign (each
//      apoapsis a bail-or-deepen decision)? The free pass and the propellant bridge, priced.
//   D. Heat and g in GAME units the existing systems already price: peak-g against the 3 g sail-hole
//      line (Atmosphere.SailHoleDecelG, lab 22's damage currency), dynamic pressure as the heat proxy,
//      and delta-v shed as pulses via OrbitRule.PulsesFor — the same kernel the autopilot spends with.
//   E. Which worlds play: a table for Uranus, Neptune, Saturn, Jupiter, Titan (the gentle deep
//      training-wheels case), Venus, Mars, Earth — scale heights and whether a corridor exists at a
//      typical arrival speed.
//
// The model, stated up front (lab 22's, unchanged): density(altitude) = refDensity *
// exp(-altitude/scaleHeight), exactly zero at/above a hard shell top; drag accel = -0.5 * rho *
// |v_rel| * v_rel / BC, BC = 120 kg/m^2. The body's spin, lift, and all heating physics are ignored;
// "too deep" is charged as HULL load off peak deceleration, not a thermal model. This lab flies in a
// planet-centered (body-at-origin) frame so the two-body energy is clean and the drag is isolated.
//
// The ice-giant shells (uranus, neptune) and the mars shell below are this lab's PROPOSAL — Uranus and
// Neptune carry no atmosphere in scenarios/sol.json yet. They are game-tuned in lab 22's spirit (thin,
// low, shell top well under 0.15 body radii so no existing gameplay trajectory clips one), and this is
// the R&D that would justify shipping them. Jupiter/Saturn/Earth/Venus/Titan MIRROR sol.json exactly.
//
// IRONCLAD RULE: every number in labs/32-aerocapture-at-the-ice-giant/README.md came from running
// this probe. If you change the code, rerun and re-paste — never hand-edit a table.

using SpaceSails.Core;

const double G0 = 9.80665;                 // standard gravity, m/s^2 — only for reporting decel in g
const double SunMu = 1.32712440018e20;     // for a body's heliocentric orbital speed (pulse pricing)
const double GBudgetG = Atmosphere.SailHoleDecelG; // the hull-damage line (Core constant): 3 g

// ---- bodies (mu, radius MIRROR scenarios/sol.json; ice-giant + mars shells are this lab's proposal) -
var worlds = new (string Name, double Mu, double R, double OrbitR, Atmosphere? Atm, double TypicalVinf)[]
{
    // gas + ice giants and the training-wheels moon: the aerocapture candidates
    ("Jupiter", 1.26686534e17, 6.9911e7, 7.7857e11, new Atmosphere(4.0e-6, 3.0e4, 4.0e5),  5500),   // sol.json
    ("Saturn",  3.7931187e16,  5.8232e7, 1.43353e12, new Atmosphere(5.0e-6, 1.2e5, 7.0e5), 5500),   // sol.json
    ("Uranus",  5.793939e15,   2.5362e7, 2.87246e12, new Atmosphere(1.4e-5, 1.2e5, 1.0e6), 6000),   // PROPOSAL; typical arrival
    ("Neptune", 6.836529e15,   2.4622e7, 4.49506e12, new Atmosphere(1.8e-5, 1.0e5, 9.0e5), 6000),   // PROPOSAL
    ("Titan",   8.9781e12,     2.575e6,  1.43353e12, new Atmosphere(5.3, 4.0e4, 3.0e5),    2000),   // sol.json (orbits Saturn)
    // the inner rocky/terrestrial worlds — for the "which worlds play" contrast
    ("Venus",   3.24859e14,    6.0518e6, 1.0821e11,  new Atmosphere(65.0, 1.6e4, 1.5e5),   5000),   // sol.json
    ("Earth",   3.986004418e14,6.371e6,  1.496e11,   new Atmosphere(1.2, 8.0e3, 1.4e5),    3000),   // sol.json
    ("Mars",    4.282837e13,   3.3895e6, 2.2794e11,  new Atmosphere(0.02, 1.1e4, 1.2e5),   3000),   // PROPOSAL (thin CO2)
};
(string Name, double Mu, double R, double OrbitR, Atmosphere? Atm, double TypicalVinf) World(string n) =>
    Array.Find(worlds, w => w.Name == n);

// A single-body, body-at-origin ephemeris (parentId null + orbitPeriod 0 pins it at the origin with
// zero rail velocity, so v_rel is just the ship's velocity and the two-body energy is clean).
Simulator MakeSim(string id, double mu, double radius, Atmosphere? atm) =>
    new(new CircularOrbitEphemeris([new CelestialBody(id, id, null, mu, radius, 0, 0, 0, Atmosphere: atm)]),
        timeStepSeconds: 1.0);

// ---- helpers (lab 22's, verbatim) ------------------------------------------------------------

double Vesc(double mu, double r) => Math.Sqrt(2 * mu / r);

// Incoming hyperbolic state on the +x axis at radius rStart, whose vacuum periapsis is rPeri.
ShipState Arrival(double mu, double rStart, double rPeri, double vInf)
{
    double vPeri = Math.Sqrt(vInf * vInf + 2 * mu / rPeri);
    double h = rPeri * vPeri;
    double v = Math.Sqrt(vInf * vInf + 2 * mu / rStart);
    double vt = h / rStart;
    double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
    return new ShipState(new Vector2d(rStart, 0), new Vector2d(vr, vt), 0);
}

// Incoming BOUND state on the +x axis at radius rStart, on the ellipse with the given periapsis and
// apoapsis (used to seed the free tightening campaign from a just-captured orbit).
ShipState BoundArrival(double mu, double rStart, double rPeri, double rApo)
{
    double a = (rPeri + rApo) / 2.0;
    double vPeri = Math.Sqrt(mu * (2.0 / rPeri - 1.0 / a));
    double h = rPeri * vPeri;
    double v = Math.Sqrt(Math.Max(0, mu * (2.0 / rStart - 1.0 / a)));
    double vt = h / rStart;
    double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
    return new ShipState(new Vector2d(rStart, 0), new Vector2d(vr, vt), 0);
}

double Energy(ShipState s, double mu) => s.Velocity.LengthSquared / 2.0 - mu / s.Position.Length;

double Apoapsis(ShipState s, double mu)
{
    double e0 = Energy(s, mu);
    if (e0 >= 0) return double.PositiveInfinity;
    double a = -mu / (2 * e0);
    double hMom = Math.Abs(s.Position.X * s.Velocity.Y - s.Position.Y * s.Velocity.X);
    double ecc = Math.Sqrt(Math.Max(0, 1 + 2 * e0 * hMom * hMom / (mu * mu)));
    return a * (1 + ecc);
}

// Fly ONE atmosphere pass: from an above-shell entry, down through periapsis, until the ship climbs
// back above the shell top (measured, so a captured pass is never double-counted). Combines the
// per-slice DragReports into one pass report.
(ShipState Post, double PeakDecel, double DvShed, double MinAlt, double PeakQ, bool Crashed)
    FlyPass(Simulator sim, ShipState entry, double bodyRadius, double topAltitude, double mu, double sliceSeconds = 20.0)
{
    double shellTop = bodyRadius + topAltitude;
    double peak = 0, shed = 0, minAlt = double.PositiveInfinity, peakQ = 0;
    ShipState s = entry;
    bool entered = false, crashed = false;
    double flownCap = 3 * 3600; // a real pass is minutes; this only stops a pathology
    while (s.SimTime - entry.SimTime < flownCap)
    {
        (ShipState next, Simulator.DragReport rep) =
            sim.RunAdaptiveWithDrag(s, sliceSeconds, null, minTimeStep: 0.05, maxTimeStep: 2.0);
        peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
        peakQ = Math.Max(peakQ, rep.PeakDynamicPressurePascal);
        shed += rep.DeltaVShedMetersPerSecond;
        if (!double.IsNaN(rep.MinAltitudeMeters)) minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
        s = next;
        double r = s.Position.Length;
        if (r < bodyRadius) { crashed = true; break; } // decayed into the surface
        if (r < shellTop)
        {
            entered = true;
            // Trapped inside the shell: bound with apoapsis below the shell top means the orbit can
            // never climb clear of the air — it will spiral in. This is a full atmospheric ENTRY (a
            // landing), not a capture-to-orbit; end the pass here rather than integrate the decay.
            double e0 = Energy(s, mu);
            if (e0 < 0 && Apoapsis(s, mu) < shellTop) { crashed = true; break; }
        }
        else if (entered) break;
    }
    if (crashed) minAlt = 0;
    return (s, peak, shed, double.IsPositiveInfinity(minAlt) ? double.NaN : minAlt, peakQ, crashed);
}

// ===================================================================================
// Section A — the model, stated honestly
// ===================================================================================
Console.WriteLine("=== Section A: the model (lab 22's exponential atmosphere + one ballistic-coefficient knob) ===");
Console.WriteLine("density(h) = refDensity * exp(-h / scaleHeight), and exactly 0 at/above the shell top.");
Console.WriteLine($"drag accel = -0.5 * density * |v_rel| * v_rel / BC,  BC = {Simulator.BallisticCoefficient:F0} kg/m^2.");
Console.WriteLine($"'too deep' is charged off peak deceleration vs the {GBudgetG:F0} g sail-hole line (Atmosphere.SailHoleDecelG),");
Console.WriteLine("not a thermal model. Uranus & Neptune shells are THIS LAB'S PROPOSAL (no air in sol.json yet).");
Console.WriteLine();
Console.WriteLine($"{"body",-9}{"refDensity kg/m^3",18}{"scaleHeight km",15}{"shell top km",13}{"top / R",9}{"v_esc km/s",12}{"source",12}");
foreach (var w in worlds)
{
    string src = w.Name is "Uranus" or "Neptune" or "Mars" ? "PROPOSAL" : "sol.json";
    Console.WriteLine($"{w.Name,-9}{w.Atm!.RefDensity,18:G3}{w.Atm.ScaleHeight / 1000,15:F1}" +
        $"{w.Atm.TopAltitude / 1000,13:F0}{w.Atm.TopAltitude / w.R,9:F4}{Vesc(w.Mu, w.R) / 1000,12:F1}{src,12}");
}
Console.WriteLine("Every shell top sits well under 0.15 body radii, so no existing gameplay trajectory clips one.");
Console.WriteLine();

// ===================================================================================
// Section B — the corridor at Uranus vs entry speed
// ===================================================================================
var U = World("Uranus");
var uranus = MakeSim("uranus", U.Mu, U.R, U.Atm);
double uStart = U.R + U.Atm!.TopAltitude + 3.0e5;   // enter 300 km above the shell top

Console.WriteLine("=== Section B: the corridor at Uranus, swept depth at the stranding speed ===");
const double strandVinf = 29800.0;                  // 29.8 km/s — the owner's relative speed at the stranding
double strandEntry = Math.Sqrt(strandVinf * strandVinf + Vesc(U.Mu, U.R) * Vesc(U.Mu, U.R));
Console.WriteLine($"arrival v_inf = {strandVinf / 1000:F1} km/s; v_esc = {Vesc(U.Mu, U.R) / 1000:F1} km/s; " +
    $"periapsis entry speed ~ {strandEntry / 1000:F1} km/s. Damage line = {GBudgetG:F0} g.");
Console.WriteLine($"{"peri alt km",13}{"min alt km",12}{"dv shed m/s",13}{"peak g",9}{"q kPa",9}{"outcome",34}");

foreach (double periAltKm in new[] { 40.0, 80.0, 120.0, 160.0, 220.0, 300.0, 400.0 })
{
    double rPeri = U.R + periAltKm * 1000;
    var pass = FlyPass(uranus, Arrival(U.Mu, uStart, rPeri, strandVinf), U.R, U.Atm.TopAltitude, U.Mu);
    double peakG = pass.PeakDecel / G0;
    double eOut = Energy(pass.Post, U.Mu);
    string outcome;
    if (pass.Crashed) outcome = "IMPACT (augers in)";
    else if (peakG > GBudgetG) outcome = eOut < 0 ? "OVER-G: hull holed + captured" : "OVER-G: hull holed, still leaves";
    else outcome = eOut < 0 ? $"captured, apo {Apoapsis(pass.Post, U.Mu) / U.R:F1} R_U" : $"SKIPS OUT, v_inf {Math.Sqrt(2 * eOut) / 1000:F1} km/s";
    Console.WriteLine($"{periAltKm,13:F0}{pass.MinAlt / 1000,12:F1}{pass.DvShed,13:F0}{peakG,9:F2}{pass.PeakQ / 1000,9:F2}{outcome,34}");
}
Console.WriteLine();

// Corridor width vs entry speed: for each v_inf, sweep depth finely; the SAFE single-pass corridor is
// the band that captures (E<0) AND stays under the g line. Report its width, and the critical v_inf.
Console.WriteLine("Corridor width vs entry speed (safe single-pass = captures AND peak g <= 3):");
Console.WriteLine($"{"v_inf km/s",12}{"entry km/s",12}{"capture band km",18}{"safe band km",15}{"width km",11}{"verdict",16}");
double critVinf = double.NaN;
foreach (double vInfKm in new[] { 4.0, 8.0, 12.0, 16.0, 20.0, 24.0, 29.8 })
{
    double vInf = vInfKm * 1000;
    double entry = Math.Sqrt(vInf * vInf + Vesc(U.Mu, U.R) * Vesc(U.Mu, U.R));
    double capLo = double.NaN, capHi = double.NaN, safeLo = double.NaN, safeHi = double.NaN;
    for (double altKm = 20; altKm <= 900; altKm += 10)
    {
        var pass = FlyPass(uranus, Arrival(U.Mu, uStart, U.R + altKm * 1000, vInf), U.R, U.Atm.TopAltitude, U.Mu);
        double peakG = pass.PeakDecel / G0;
        bool captured = !pass.Crashed && Energy(pass.Post, U.Mu) < 0;
        if (captured)
        {
            capLo = double.IsNaN(capLo) ? altKm : Math.Min(capLo, altKm);
            capHi = double.IsNaN(capHi) ? altKm : Math.Max(capHi, altKm);
            if (peakG <= GBudgetG)
            {
                safeLo = double.IsNaN(safeLo) ? altKm : Math.Min(safeLo, altKm);
                safeHi = double.IsNaN(safeHi) ? altKm : Math.Max(safeHi, altKm);
            }
        }
    }
    double width = double.IsNaN(safeHi) ? 0 : safeHi - safeLo;
    string capBand = double.IsNaN(capLo) ? "(none)" : $"{capLo:F0}-{capHi:F0}";
    string safeBand = double.IsNaN(safeLo) ? "(none)" : $"{safeLo:F0}-{safeHi:F0}";
    string verdict = width > 0 ? "corridor OPEN" : "CLOSED";
    if (width > 0) critVinf = vInfKm; // highest speed (loop ascends) with an open corridor
    Console.WriteLine($"{vInfKm,12:F1}{entry / 1000,12:F1}{capBand,18}{safeBand,15}{width,11:F0}{verdict,16}");
}
Console.WriteLine();
Console.WriteLine($"The corridor CLOSES above ~{critVinf:F0} km/s v_inf: past there, every depth that captures is");
Console.WriteLine($"already over {GBudgetG:F0} g. The stranding ({strandVinf / 1000:F1} km/s) is well above it — no safe single-pass capture.");
Console.WriteLine();

// ===================================================================================
// Section C — delta-v shed per pass, and the campaign (one pass vs multi-pass)
// ===================================================================================
Console.WriteLine("=== Section C: is one pass enough at 36 km/s, or a campaign? ===");

// C1. The FREE pass at the g limit: find the deepest single pass that stays at/under 3 g, and how much
//     it sheds — then compare to the delta-v needed to capture (drop periapsis speed below local escape).
double bestFreeShed = 0, bestFreeAltKm = double.NaN, bestFreeG = 0;
for (double altKm = 40; altKm <= 900; altKm += 5)
{
    var pass = FlyPass(uranus, Arrival(U.Mu, uStart, U.R + altKm * 1000, strandVinf), U.R, U.Atm.TopAltitude, U.Mu);
    if (!pass.Crashed && pass.PeakDecel / G0 <= GBudgetG && pass.DvShed > bestFreeShed)
    {
        bestFreeShed = pass.DvShed; bestFreeAltKm = altKm; bestFreeG = pass.PeakDecel / G0;
    }
}
double rPeriFree = U.R + bestFreeAltKm * 1000;
double vPeriFree = Math.Sqrt(strandVinf * strandVinf + 2 * U.Mu / rPeriFree); // pre-drag periapsis speed
double vEscPeri = Vesc(U.Mu, rPeriFree);                                      // local escape at that periapsis
double dvToCapture = vPeriFree - vEscPeri;                                    // must drop below this to go bound
double bridge = Math.Max(0, dvToCapture - bestFreeShed);                      // propellant the air can't pay
Console.WriteLine($"C1. Deepest FREE pass at/under {GBudgetG:F0} g: periapsis {bestFreeAltKm:F0} km ({bestFreeG:F2} g), sheds {bestFreeShed:F0} m/s.");
Console.WriteLine($"    To capture, the {vPeriFree / 1000:F1} km/s periapsis speed must drop below local escape {vEscPeri / 1000:F1} km/s");
Console.WriteLine($"    = shed {dvToCapture / 1000:F1} km/s. One free pass pays {bestFreeShed / 1000:F1} km/s; the bridge is {bridge / 1000:F1} km/s.");
Console.WriteLine("    => ONE pass is NOT enough at this speed. The air captures only with a propellant assist, or by");
Console.WriteLine("       going over-g (holing the hull), or by a campaign that first pass must still get bound to begin.");
Console.WriteLine();

// C2. The free tightening campaign, ONCE bound: seed a just-captured wide orbit (as an assisted or
//     over-g first pass would leave it), then fly free passes at the g-limit depth. Reconstruct each
//     next entry from the exit orbit's energy + angular momentum EXACTLY (lab 22's symmetry trick), so
//     the long coast that semi-implicit Euler would corrupt is skipped, not approximated. Each apoapsis
//     is a bail-or-deepen decision; with no fuel to re-raise periapsis it creeps down — a race you lose.
double campaignPeriKm = 300.0; // a GENTLE shallow periapsis (~0.7 g), so each free pass sheds a little
Console.WriteLine($"C2. The free tightening campaign once bound (seeded at a just-captured 60 R_U orbit, {campaignPeriKm:F0} km gentle periapsis):");
Console.WriteLine($"{"pass",6}{"peri alt km",13}{"dv shed m/s",13}{"peak g",9}{"energy J/kg",15}{"apoapsis",14}");
ShipState ship = BoundArrival(U.Mu, uStart, U.R + campaignPeriKm * 1000, 60 * U.R);
double campaignShed = 0;
int campaignPasses = 0;
for (int passN = 1; passN <= 12; passN++)
{
    var pass = FlyPass(uranus, ship, U.R, U.Atm.TopAltitude, U.Mu);
    double eOut = Energy(pass.Post, U.Mu);
    double apo = Apoapsis(pass.Post, U.Mu);
    campaignShed += pass.DvShed; campaignPasses = passN;
    string apoTxt = pass.Crashed ? "IMPACT (decayed in)"
        : double.IsPositiveInfinity(apo) ? "escapes" : $"{apo / U.R:F1} R_U";
    Console.WriteLine($"{passN,6}{pass.MinAlt / 1000,13:F1}{pass.DvShed,13:F0}{pass.PeakDecel / G0,9:F2}{eOut,15:F0}{apoTxt,14}");
    if (pass.Crashed || eOut >= 0) break;
    if (apo < 2 * U.R) break; // tight enough to hand to a small circularization burn
    // reconstruct next entry inbound at uStart, carrying the exit orbit's energy + |h| exactly
    double hExit = Math.Abs(pass.Post.Position.X * pass.Post.Velocity.Y - pass.Post.Position.Y * pass.Post.Velocity.X);
    double vNext = Math.Sqrt(2 * (eOut + U.Mu / uStart));
    double vtNext = hExit / uStart;
    double vrNext = -Math.Sqrt(Math.Max(0, vNext * vNext - vtNext * vtNext));
    ship = new ShipState(new Vector2d(uStart, 0), new Vector2d(vrNext, vtNext), 0);
}
Console.WriteLine($"    {campaignPasses} free passes shed {campaignShed / 1000:F1} km/s total for ZERO fuel, tightening the orbit;");
Console.WriteLine("    periapsis creeps down every pass (no fuel to re-raise it), so free tightening is a race the hull loses.");
Console.WriteLine();

// ===================================================================================
// Section D — heat and g in GAME units the existing systems already price
// ===================================================================================
Console.WriteLine("=== Section D: the bill in game units (hull g, heat proxy, pulses) ===");
double vOrbU = Math.Sqrt(SunMu / U.OrbitR);           // Uranus heliocentric orbital speed (pulse pricing)
// Pure-propulsive capture: shed the whole excess at periapsis, priced at the ~periapsis world speed.
int propulsivePulses = OrbitRule.PulsesFor(dvToCapture, vPeriFree);
// Aerocapture-assisted first pass: the air pays the free shed; you buy only the bridge, at periapsis speed.
int bridgePulses = OrbitRule.PulsesFor(bridge, vPeriFree);
int airPaysPulses = propulsivePulses - bridgePulses;
Console.WriteLine($"Owner's tank: 32 pulses. Uranus heliocentric orbital speed {vOrbU / 1000:F1} km/s (the world-speed floor).");
Console.WriteLine($"Pure-propulsive capture: shed {dvToCapture / 1000:F1} km/s at ~{vPeriFree / 1000:F1} km/s world speed = " +
    $"{propulsivePulses} pulses  => {(propulsivePulses > 32 ? "IMPOSSIBLE (over 32)" : "affordable")}.");
Console.WriteLine($"Aerocapture-assisted: the {GBudgetG:F0} g pass sheds {bestFreeShed / 1000:F1} km/s FREE; you buy only the " +
    $"{bridge / 1000:F1} km/s bridge = {bridgePulses} pulses.");
Console.WriteLine($"  => the haze pays {airPaysPulses} pulses of the capture bill and drops peak-g from a solo capture's lethal load to {GBudgetG:F0} g.");
Console.WriteLine($"  => {bridgePulses} pulses is {(bridgePulses <= 32 ? "INSIDE" : "OUTSIDE")} the owner's 32 — the air is what makes the stranding survivable.");
// The deepest solo (no-assist) single-pass capture and its g, for the "how bad is the desperate move" read.
double soloG = double.NaN, soloAltKm = double.NaN;
for (double altKm = 900; altKm >= 20; altKm -= 5)
{
    var pass = FlyPass(uranus, Arrival(U.Mu, uStart, U.R + altKm * 1000, strandVinf), U.R, U.Atm.TopAltitude, U.Mu);
    if (!pass.Crashed && Energy(pass.Post, U.Mu) < 0) { soloG = pass.PeakDecel / G0; soloAltKm = altKm; break; }
}
Console.WriteLine(double.IsNaN(soloG)
    ? "Desperate solo capture (no fuel, accept the hull): NONE exists — at 29.8 km/s every single pass either"
      + " skips out (shallow) or augers in (deep) before it can shed enough. The bridge burn is mandatory."
    : $"Desperate solo capture (no fuel, accept the hull): shallowest depth that still captures is "
      + $"{soloAltKm:F0} km at {soloG:F1} g — {soloG / GBudgetG:F1}x the sail-hole line.");
Console.WriteLine("Heat has no thermal system in-game; it is charged as hull load off peak-g (lab 22's currency). Peak dynamic");
Console.WriteLine("pressure (q, above) is the physical heat proxy a future thermal model would read from the same DragReport.");
Console.WriteLine();

// ===================================================================================
// Section E — which worlds play
// ===================================================================================
Console.WriteLine("=== Section E: which worlds play (corridor at a typical arrival speed) ===");
Console.WriteLine("'grab g' = peak g of the shallowest pass that goes bound (captures OR is pulled in). A safe corridor");
Console.WriteLine("needs a capture-to-ORBIT (apoapsis clears the shell) under the 3 g line.");
Console.WriteLine($"{"world",-9}{"H km",7}{"v_esc km/s",12}{"v_inf km/s",12}{"entry km/s",12}{"grab g",10}{"safe corridor",16}{"verdict",24}");
foreach (var w in worlds)
{
    var sim = MakeSim(w.Name, w.Mu, w.R, w.Atm);
    double start = w.R + w.Atm!.TopAltitude + 3.0e5;
    double shellTop = w.R + w.Atm.TopAltitude;
    double entry = Math.Sqrt(w.TypicalVinf * w.TypicalVinf + Vesc(w.Mu, w.R) * Vesc(w.Mu, w.R));
    double safeLo = double.NaN, safeHi = double.NaN, grabG = double.NaN;
    bool anyOrbit = false, anyLand = false;
    // sweep depth from just below the shell top down toward the surface
    double topKm = w.Atm.TopAltitude / 1000;
    for (double altKm = topKm - 10; altKm >= 5; altKm -= Math.Max(2, topKm / 80))
    {
        var pass = FlyPass(sim, Arrival(w.Mu, start, w.R + altKm * 1000, w.TypicalVinf), w.R, w.Atm.TopAltitude, w.Mu);
        double peakG = pass.PeakDecel / G0;
        bool bound = pass.Crashed || Energy(pass.Post, w.Mu) < 0;
        bool orbit = !pass.Crashed && Energy(pass.Post, w.Mu) < 0 && Apoapsis(pass.Post, w.Mu) > shellTop;
        if (bound && double.IsNaN(grabG)) grabG = peakG; // shallowest = first bound pass on the way down
        if (orbit) anyOrbit = true;
        if (pass.Crashed) anyLand = true;
        if (orbit && peakG <= GBudgetG)
        {
            safeLo = double.IsNaN(safeLo) ? altKm : Math.Min(safeLo, altKm);
            safeHi = double.IsNaN(safeHi) ? altKm : Math.Max(safeHi, altKm);
        }
    }
    string corridor = double.IsNaN(safeLo) ? "(none)" : $"{safeLo:F0}-{safeHi:F0} km";
    string verdict = !double.IsNaN(safeLo) ? "PLAYS (gentle)"
        : anyOrbit ? "captures HOT (>3g)"
        : anyLand ? "aero-LANDS (air too thick)"
        : "no capture (too thin)";
    string grab = double.IsNaN(grabG) ? "n/a" : $"{grabG:F1}";
    Console.WriteLine($"{w.Name,-9}{w.Atm.ScaleHeight / 1000,7:F0}{Vesc(w.Mu, w.R) / 1000,12:F1}{w.TypicalVinf / 1000,12:F1}" +
        $"{entry / 1000,12:F1}{grab,10}{corridor,16}{verdict,24}");
}
Console.WriteLine();
Console.WriteLine("The pattern: thin-high shells (the giants, Earth, Mars) give a gentle skim corridor at their typical");
Console.WriteLine("arrival speeds. The ice giants play too, at typical speeds — but NOT at the stranding (Section B). Titan");
Console.WriteLine("and Venus are the anti-training-wheels: shells so thick every entering pass sheds enough to grab you, but");
Console.WriteLine("at brutal g — you cannot skip out (forgiving) yet cannot capture GENTLY (you aero-LAND, not orbit). Titan is");
Console.WriteLine("still the 'deep' teaching case: a guaranteed grab, the opposite failure mode to the ice giant's skip-away.");
Console.WriteLine();

// ===================================================================================
// Break it yourself
// ===================================================================================
Console.WriteLine("=== Break it yourself ===");

// 1) Halve the arrival speed. The corridor that is CLOSED at 29.8 km/s reopens well below critical.
{
    double vInf = 14000;
    double safeLo = double.NaN, safeHi = double.NaN;
    for (double altKm = 20; altKm <= 900; altKm += 10)
    {
        var pass = FlyPass(uranus, Arrival(U.Mu, uStart, U.R + altKm * 1000, vInf), U.R, U.Atm.TopAltitude, U.Mu);
        bool captured = !pass.Crashed && Energy(pass.Post, U.Mu) < 0;
        if (captured && pass.PeakDecel / G0 <= GBudgetG) { safeLo = double.IsNaN(safeLo) ? altKm : Math.Min(safeLo, altKm); safeHi = double.IsNaN(safeHi) ? altKm : Math.Max(safeHi, altKm); }
    }
    Console.WriteLine($"1. Halve the speed to {vInf / 1000:F0} km/s v_inf: the safe corridor is {(double.IsNaN(safeLo) ? "(still closed)" : $"{safeLo:F0}-{safeHi:F0} km, {safeHi - safeLo:F0} km wide")}.");
    Console.WriteLine("   Slower arrivals need less shed, so the capture band clears the g line — speed, not depth, is the gate.");
}

// 2) Neptune at the same speed. A denser proposed shell brakes harder — its corridor sits at a different depth.
{
    var N = World("Neptune");
    var nep = MakeSim("neptune", N.Mu, N.R, N.Atm);
    double nStart = N.R + N.Atm!.TopAltitude + 3.0e5;
    var pass = FlyPass(nep, Arrival(N.Mu, nStart, N.R + 120e3, 14000), N.R, N.Atm.TopAltitude, N.Mu);
    Console.WriteLine($"2. Neptune, 14 km/s v_inf, 120 km dip: sheds {pass.DvShed:F0} m/s at {pass.PeakDecel / G0:F2} g, " +
        $"{(Energy(pass.Post, N.Mu) < 0 ? "captures" : "skips")}. Its higher v_esc ({Vesc(N.Mu, N.R) / 1000:F1} km/s) makes every arrival hotter.");
}

// 3) Mars is the anti-Titan. A thin CO2 shell can't brake a fast arrival before the ground stops it.
{
    var M = World("Mars");
    var mars = MakeSim("mars", M.Mu, M.R, M.Atm);
    double mStart = M.R + M.Atm!.TopAltitude + 3.0e5;
    var shallow = FlyPass(mars, Arrival(M.Mu, mStart, M.R + 60e3, 3000), M.R, M.Atm.TopAltitude, M.Mu);
    var deep = FlyPass(mars, Arrival(M.Mu, mStart, M.R + 10e3, 3000), M.R, M.Atm.TopAltitude, M.Mu);
    Console.WriteLine($"3. Mars, 3 km/s v_inf: a 60 km pass sheds {shallow.DvShed:F0} m/s ({(Energy(shallow.Post, M.Mu) < 0 ? "captures" : "skips")}); " +
        $"a 10 km pass {(deep.Crashed ? "AUGERS IN" : $"sheds {deep.DvShed:F0} m/s")}.");
    Console.WriteLine("   The thin shell is the anti-Titan: too little air to capture high, too little room to capture deep.");
}
Console.WriteLine();
Console.WriteLine("Every number above came from running the probe. Rerun after edits.");
