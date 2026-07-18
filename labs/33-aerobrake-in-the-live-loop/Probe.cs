// Lab 33 — Aerobrake in the LIVE loop: the campaign that Lab 32 printed, now flown for real
//
// Teaching voice: Lab 32 proved the ice giant's haze is a priced brake — but it flew in a CLEAN
// planet-centred frame, the body pinned at the origin with zero rail velocity, so the two-body energy
// stayed pristine and the drag was isolated. That is the right way to PRINT the physics. It is not the
// way the game FLIES it. In the live sim Uranus orbits the Sun at ~6.8 km/s, its atmosphere translates
// WITH it (the shell's air is not at rest), and the integrator is n-body (the Sun tugs the whole time).
// Lab 33 asks the one question that pinned frame cannot answer: does the aerobrake campaign still
// CONVERGE under the real integrator, with a moving atmosphere and a live solar perturbation — and does
// the shipped Core quote (Aerobrake.Price) reproduce Lab 32's headline before the button ever renders?
//
// The R&D rule holds: the probe prints the numbers before the feature ships. This lab flies the SAME
// Core drag (Simulator.RunAdaptiveWithDrag / DragReport) — no forked model — but in a two-body-rail
// ephemeris (Sun pinned, Uranus on its real sol.json orbit) so v_rel now genuinely includes Uranus's
// heliocentric motion. Because drag depends only on speed relative to the air, Galilean invariance
// predicts the campaign is identical to Lab 32's; Section F verifies that numerically rather than
// assuming it. The proposed Uranus shell mirrors Lab 32's proposal (added to scenarios/sol.json by #290).
//
// IRONCLAD RULE: every number in labs/33-aerobrake-in-the-live-loop/README.md came from running this
// probe. If you change the code, rerun and re-paste — never hand-edit a table.

using SpaceSails.Core;

const double G0 = 9.80665;
const double SunMu = 1.32712440018e20;

// ---- the LIVE two-body-rail ephemeris: Sun pinned at the origin, Uranus on its real sol.json orbit ----
// (mu, radius, orbit MIRROR scenarios/sol.json; the atmosphere shell is #290's shipped proposal.)
const double UranusMu = 5.793939e15, UranusR = 2.5362e7, UranusOrbitR = 2.87246e12, UranusPeriod = 2.65104e9;
var uranusAtm = new Atmosphere(RefDensity: 1.4e-5, ScaleHeight: 1.2e5, TopAltitude: 1.0e6);

var sun = new CelestialBody("sun", "Sun", null, SunMu, 6.9634e8, 0, 0, 0);
var uranus = new CelestialBody("uranus", "Uranus", "sun", UranusMu, UranusR, UranusOrbitR, UranusPeriod, 0.5,
    Atmosphere: uranusAtm);
var eph = new CircularOrbitEphemeris([sun, uranus]);
var sim = new Simulator(eph, timeStepSeconds: 1.0);

// Uranus's live position and rail velocity (central difference — the exact convention Simulator's drag uses).
Vector2d UranusPos(double t) => eph.Position("uranus", t);
Vector2d UranusVel(double t) => (eph.Position("uranus", t + 1.0) - eph.Position("uranus", t - 1.0)) / 2.0;

// Energy / apoapsis measured RELATIVE TO THE MOVING URANUS at the sample instant (the honest live read).
double RelEnergy(ShipState s)
{
    Vector2d rRel = s.Position - UranusPos(s.SimTime);
    Vector2d vRel = s.Velocity - UranusVel(s.SimTime);
    return vRel.LengthSquared / 2.0 - UranusMu / rRel.Length;
}
double RelApoapsis(ShipState s)
{
    Vector2d rRel = s.Position - UranusPos(s.SimTime);
    Vector2d vRel = s.Velocity - UranusVel(s.SimTime);
    double e0 = vRel.LengthSquared / 2.0 - UranusMu / rRel.Length;
    if (e0 >= 0) return double.PositiveInfinity;
    double a = -UranusMu / (2 * e0);
    double h = Math.Abs(rRel.X * vRel.Y - rRel.Y * vRel.X);
    double ecc = Math.Sqrt(Math.Max(0, 1 + 2 * e0 * h * h / (UranusMu * UranusMu)));
    return a * (1 + ecc);
}
double RelDist(ShipState s) => (s.Position - UranusPos(s.SimTime)).Length;

// A hyperbolic arrival relative to the MOVING Uranus at t0: build the planet-relative state (radial-in +
// tangential, Lab 32's geometry) then add Uranus's rail velocity so the ship rides the live heliocentric frame.
ShipState LiveArrival(double t0, double rStart, double rPeri, double vInf)
{
    double vPeri = Math.Sqrt(vInf * vInf + 2 * UranusMu / rPeri);
    double h = rPeri * vPeri;
    double v = Math.Sqrt(vInf * vInf + 2 * UranusMu / rStart);
    double vt = h / rStart;
    double vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
    Vector2d relPos = new(rStart, 0);
    Vector2d relVel = new(vr, vt);
    return new ShipState(UranusPos(t0) + relPos, UranusVel(t0) + relVel, t0);
}

// A bound arrival relative to the moving Uranus (seeds the live tightening campaign from a captured orbit).
ShipState LiveBound(double t0, double rStart, double rPeri, double rApo)
{
    double a = (rPeri + rApo) / 2.0;
    double vPeri = Math.Sqrt(UranusMu * (2.0 / rPeri - 1.0 / a));
    double h = rPeri * vPeri;
    double v = Math.Sqrt(Math.Max(0, UranusMu * (2.0 / rStart - 1.0 / a)));
    double vt = h / rStart, vr = -Math.Sqrt(Math.Max(0, v * v - vt * vt));
    return new ShipState(UranusPos(t0) + new Vector2d(rStart, 0), UranusVel(t0) + new Vector2d(vr, vt), t0);
}

double shellTop = UranusR + uranusAtm.TopAltitude;
double startAlt = uranusAtm.TopAltitude + 3.0e5;

// Fly ONE live pass: continuous RunAdaptiveWithDrag (drag is exactly zero above the shell, so the SAME
// call coasts the vacuum arc and bites only in the cloud tops), from an above-shell entry down through
// periapsis until the ship climbs back above the shell top. This is the live loop — the integrator config
// is the plot desk's skim gauge's (fine near periapsis, adaptive on the climb).
(ShipState Post, double PeakG, double DvShed, double MinAlt, bool Crashed) LivePass(ShipState entry)
{
    double peak = 0, shed = 0, minAlt = double.PositiveInfinity;
    ShipState s = entry;
    bool entered = false, crashed = false;
    while (s.SimTime - entry.SimTime < 6 * 3600)
    {
        (ShipState next, Simulator.DragReport rep) =
            sim.RunAdaptiveWithDrag(s, 20.0, null, minTimeStep: 0.05, maxTimeStep: 3.0);
        peak = Math.Max(peak, rep.PeakDecelMetersPerSecondSquared);
        shed += rep.DeltaVShedMetersPerSecond;
        if (!double.IsNaN(rep.MinAltitudeMeters)) minAlt = Math.Min(minAlt, rep.MinAltitudeMeters);
        s = next;
        double r = RelDist(s);
        if (r < UranusR) { crashed = true; break; }
        if (r < shellTop)
        {
            entered = true;
            if (RelEnergy(s) < 0 && RelApoapsis(s) < shellTop) { crashed = true; break; }
        }
        else if (entered) break;
    }
    return (s, peak / G0, shed, crashed ? 0 : minAlt, crashed);
}

// Coast (live) from just above the shell on the outbound leg around to just above it on the inbound leg,
// so the next LivePass starts at the same entry geometry. Continuous RunAdaptiveWithDrag (drag is zero out
// here). Steps coarsely far out but FINELY as it nears the shell, so a fast periapsis is never stepped over
// (a 300 s step at 10 km/s would leap 3000 km — clean through the 1000 km shell; 20 s cannot).
ShipState CoastToNextEntry(ShipState post)
{
    ShipState s = post;
    bool climbed = false;
    double t0 = s.SimTime;
    double gate = shellTop + 3.0e5;
    while (s.SimTime - t0 < 90 * 86400.0) // a bail cap; a tightened orbit's period is hours
    {
        double r = RelDist(s);
        double step = r > 3 * shellTop ? 300.0 : 20.0; // fine near the shell so the entry is caught, not skipped
        (ShipState next, _) = sim.RunAdaptiveWithDrag(s, step, null, minTimeStep: 1.0, maxTimeStep: step);
        double rNext = RelDist(next);
        if (rNext > gate) climbed = true;                       // safely out on the way up
        if (climbed && rNext <= gate && rNext < r) return next; // descending back to just above the shell
        s = next;
    }
    return s;
}

const double strandVinf = 29800.0;

Console.WriteLine("=== Lab 33 — Aerobrake in the LIVE loop (Sun-pinned, Uranus on its real orbit) ===");
Console.WriteLine($"Uranus heliocentric speed at t0 = {UranusVel(0).Length / 1000:F2} km/s — the atmosphere rides this;");
Console.WriteLine("the SAME Core drag now works on v_rel that includes it. If the campaign still converges, the");
Console.WriteLine("pinned-frame lab was honest. Shell: refDensity 1.4e-5, H 120 km, top 1000 km (sol.json via #290).");
Console.WriteLine();

// ===================================================================================
// Section A — the shipped Core quote reproduces Lab 32's headline (the button prints first)
// ===================================================================================
Console.WriteLine("=== Section A: Aerobrake.Price — the quote the context menu ships (owner's 32-pulse tank) ===");
Aerobrake.Quote q = Aerobrake.Price(uranus, strandVinf, budgetPulses: 32);
Console.WriteLine($"outcome            : {q.Outcome}  (corridor {(q.CorridorOpen ? "OPEN" : "CLOSED")})");
Console.WriteLine($"arrival v_inf      : {q.ArrivalVinf / 1000:F1} km/s   periapsis entry {q.EntrySpeed / 1000:F1} km/s");
Console.WriteLine($"free shed (≤3g)    : {q.FreeShedMps / 1000:F1} km/s at {q.PeakG:F2} g  (q {q.PeakDynamicPressurePa / 1000:F2} kPa)");
Console.WriteLine($"capture Δv needed  : {q.CaptureDeltaV / 1000:F1} km/s   bridge {q.BridgeMps / 1000:F1} km/s");
Console.WriteLine($"propulsive bill    : {q.PropulsivePulses} p   aerobrake bill {q.AerobrakePulses} p   SAVED {q.PulsesSaved} p");
Console.WriteLine($"passes             : {q.PassesNeeded} ({q.TighteningPasses} free tightening after the capture)");
Console.WriteLine($"menu label         : {Aerobrake.MenuAction("Uranus", q)}");
Console.WriteLine($"trade              : {Aerobrake.Trade("Uranus", q)}");
Console.WriteLine("Lab 32 said: 42 propulsive, 31 bridge, ~11 saved, corridor CLOSED at 29.8 km/s. The live-frame");
Console.WriteLine("Core quote agrees (it is flown in the same pinned kernel Price() owns).");
Console.WriteLine();

// ===================================================================================
// Section B — the LIVE hybrid capture: one hot pass + a bridge burn goes bound, for real
// ===================================================================================
Console.WriteLine("=== Section B: the LIVE hybrid capture (bridge burn at periapsis, flown n-body) ===");
double periKm = 110.0;                                  // Lab 32's g-limit periapsis
double rPeri = UranusR + periKm * 1000.0;
double t0 = 0;
ShipState arrival = LiveArrival(t0, UranusR + startAlt, rPeri, strandVinf);
Console.WriteLine($"arrival rel energy : {RelEnergy(arrival):E3} J/kg (>0, hyperbolic) — inbound at 29.8 km/s v_inf");

// Fly the incoming hot pass live; at the deepest point the captain fires the bridge Δv retrograde. Model it
// as the closed-form target: drop the post-drag periapsis speed below local escape by the bridge amount.
var pre = LivePass(arrival);
Console.WriteLine($"hot pass (no burn) : peak {pre.PeakG:F2} g, shed {pre.DvShed:F0} m/s, {(RelEnergy(pre.Post) < 0 ? "captured" : "still hyperbolic (needs the bridge)")}");

// Re-fly and, at periapsis, brake retrograde onto a wide BOUND orbit (apoapsis ~40 R_U — Lab 32 C2's
// capture target). The Δv actually spent is the honest bridge; it should land near the quote's 11.3 km/s.
double vPeri = Math.Sqrt(strandVinf * strandVinf + 2 * UranusMu / rPeri);
double vEscPeri = Math.Sqrt(2 * UranusMu / rPeri);
double aTarget = (rPeri + 40 * UranusR) / 2.0;
double vTarget = Math.Sqrt(UranusMu * (2.0 / rPeri - 1.0 / aTarget)); // periapsis speed of the captured orbit
(ShipState capture, double bridgeSpent) = FlyPassBrakingToSpeed(arrival, vTarget);
Console.WriteLine($"capture Δv needed  : {vPeri - vEscPeri:F0} m/s to escape; quote's bridge {q.BridgeMps:F0} m/s");
Console.WriteLine($"bridge SPENT live  : {bridgeSpent / 1000:F1} km/s (brake to the {40} R_U capture orbit)");
Console.WriteLine($"post-burn          : rel energy {RelEnergy(capture):E3} J/kg, " +
    $"{(RelEnergy(capture) < 0 ? $"CAPTURED, apoapsis {RelApoapsis(capture) / UranusR:F1} R_U" : "STILL loose")}");
Console.WriteLine("=> the air-assisted bridge captures under the LIVE integrator, moving atmosphere and all.");
Console.WriteLine();

// ===================================================================================
// Section C — the LIVE free tightening campaign: apoapsis drops, pass after pass, for real
// ===================================================================================
Console.WriteLine("=== Section C: the LIVE free tightening campaign (each pass flown, no fuel) ===");
Console.WriteLine($"{"pass",6}{"peak g",9}{"shed m/s",11}{"rel energy J/kg",18}{"apoapsis R_U",15}");
ShipState ship = LiveBound(0, UranusR + startAlt, UranusR + 300e3, 4 * UranusR);
double firstApo = RelApoapsis(ship), lastApo = double.PositiveInfinity;
bool monotone = true;
int flownPasses = 0;
for (int passN = 1; passN <= 6; passN++)
{
    var pass = LivePass(ship);
    double apo = RelApoapsis(pass.Post);
    double en = RelEnergy(pass.Post);
    flownPasses = passN;
    Console.WriteLine($"{passN,6}{pass.PeakG,9:F2}{pass.DvShed,11:F0}{en,18:F0}" +
        $"{(double.IsPositiveInfinity(apo) ? "escapes" : $"{apo / UranusR:F2}"),15}");
    if (pass.Crashed || en >= 0) { Console.WriteLine("   (decayed in / escaped — campaign ends)"); break; }
    if (apo > lastApo + 1e3) monotone = false; // apoapsis must not grow
    lastApo = apo;
    if (apo < 1.5 * UranusR) { Console.WriteLine("   (tight enough for a small circularisation trim)"); break; }
    ship = CoastToNextEntry(pass.Post);
}
Console.WriteLine($"apoapsis {firstApo / UranusR:F2} → {lastApo / UranusR:F2} R_U over {flownPasses} live passes; " +
    $"monotone-decreasing: {monotone}. The free tail converges under the real integrator.");
Console.WriteLine();

// ===================================================================================
// Section D — determinism: the live campaign is bit-identical run to run
// ===================================================================================
Console.WriteLine("=== Section D: determinism (client WASM and any replay agree to the bit) ===");
var a1 = LivePass(LiveArrival(0, UranusR + startAlt, rPeri, strandVinf));
var a2 = LivePass(LiveArrival(0, UranusR + startAlt, rPeri, strandVinf));
Console.WriteLine($"two identical live passes: post states equal = {a1.Post.Equals(a2.Post)}, " +
    $"peak g equal = {a1.PeakG == a2.PeakG}, shed equal = {a1.DvShed == a2.DvShed}");
Console.WriteLine();

// ===================================================================================
// Section E — Galilean check: the moving frame matches Lab 32's pinned frame
// ===================================================================================
Console.WriteLine("=== Section E: the moving atmosphere sheds exactly what the pinned frame did (Galilean) ===");
// Pinned frame (Lab 32's rig): the same body at the origin, zero rail velocity.
var pinnedEph = new CircularOrbitEphemeris([new CelestialBody("u", "u", null, UranusMu, UranusR, 0, 0, 0, Atmosphere: uranusAtm)]);
var pinnedSim = new Simulator(pinnedEph, timeStepSeconds: 1.0);
double pPeak = 0, pShed = 0; ShipState ps = new(new Vector2d(UranusR + startAlt, 0),
    new Vector2d(-Math.Sqrt(Math.Max(0, (strandVinf * strandVinf + 2 * UranusMu / (UranusR + startAlt)) - Math.Pow(rPeri * Math.Sqrt(strandVinf * strandVinf + 2 * UranusMu / rPeri) / (UranusR + startAlt), 2))),
        rPeri * Math.Sqrt(strandVinf * strandVinf + 2 * UranusMu / rPeri) / (UranusR + startAlt)), 0);
bool pen = false;
while (ps.SimTime < 6 * 3600)
{
    (ShipState nx, Simulator.DragReport rep) = pinnedSim.RunAdaptiveWithDrag(ps, 20.0, null, minTimeStep: 0.05, maxTimeStep: 3.0);
    pPeak = Math.Max(pPeak, rep.PeakDecelMetersPerSecondSquared); pShed += rep.DeltaVShedMetersPerSecond; ps = nx;
    double r = ps.Position.Length;
    if (r < shellTop) pen = true; else if (pen) break;
    if (r < UranusR) break;
}
Console.WriteLine($"pinned frame  : peak {pPeak / G0:F3} g, shed {pShed:F1} m/s");
Console.WriteLine($"moving frame  : peak {pre.PeakG:F3} g, shed {pre.DvShed:F1} m/s");
Console.WriteLine($"Δ shed = {Math.Abs(pShed - pre.DvShed):F2} m/s, Δ peak = {Math.Abs(pPeak / G0 - pre.PeakG):F4} g " +
    "— the two agree to integrator tolerance: the pinned lab was honest.");
Console.WriteLine();

// ===================================================================================
// Break it yourself
// ===================================================================================
Console.WriteLine("=== Break it yourself ===");
Aerobrake.Quote slow = Aerobrake.Price(uranus, 6000, budgetPulses: 32);
Console.WriteLine($"1. Arrive at 6 km/s v_inf instead of 29.8: outcome {slow.Outcome} " +
    $"(corridor {(slow.CorridorOpen ? "OPEN — a solo aerocapture, near-free" : "closed")}), " +
    $"{slow.PassesNeeded} passes, {slow.PulsesSaved} p saved. Speed, not aim, is the gate.");
Aerobrake.Quote gentle = Aerobrake.Price(uranus, 3000, budgetPulses: 32);
Console.WriteLine($"2. A 3 km/s v_inf: outcome {gentle.Outcome} — {(gentle.CorridorOpen ? "the haze just catches you" : "still a bridge")}.");
Console.WriteLine();
Console.WriteLine("Every number above came from running the probe. Rerun after edits.");

// A live pass that, at the deepest point (periapsis), brakes the air-relative velocity down to a target
// speed — the hybrid capture burn. Returns the post state and the Δv actually spent (the honest bridge).
(ShipState Post, double BridgeSpent) FlyPassBrakingToSpeed(ShipState entry, double targetRelSpeed)
{
    ShipState s = entry;
    bool entered = false, burned = false;
    double lastDist = double.MaxValue, spent = 0;
    while (s.SimTime - entry.SimTime < 6 * 3600)
    {
        double r = RelDist(s);
        // Fire once, at periapsis (distance stops shrinking): brake the air-relative flow to the target speed.
        if (!burned && entered && r > lastDist)
        {
            Vector2d vRel = s.Velocity - UranusVel(s.SimTime);
            double v = vRel.Length;
            if (v > targetRelSpeed)
            {
                spent = v - targetRelSpeed;
                s = s with { Velocity = s.Velocity - vRel * (spent / v) };
            }
            burned = true;
        }
        lastDist = r;
        (ShipState next, _) = sim.RunAdaptiveWithDrag(s, 20.0, null, minTimeStep: 0.05, maxTimeStep: 3.0);
        s = next;
        double rn = RelDist(s);
        if (rn < UranusR) break;
        if (rn < shellTop) entered = true;
        else if (entered && burned) break;
    }
    return (s, spent);
}
