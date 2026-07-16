// Lab 28 — The pump crawl
//
// Teaching voice: lesson 23 stopped the Titan-approach hemorrhage (#146: 167 pulses fighting
// Saturn, then the autopilot starved) by planning in the giant's frame. But the deeper lesson of
// that night is not "the approach was expensive" — it is "the ship let itself get expensive-far
// from a fuel pump and only found out when the tank hit 8." A pump is not everywhere. Depots ride
// rails at the PLANETS, the STATIONS and the HAVENS (TrafficSchedule.GenerateDepots) — but NOT at
// an ordinary moon. Titan has no pump; Luna has no pump. So "can I still refuel?" is a real,
// measurable question with a real red line, and this lesson measures it honestly with the game's
// own planners (TransferPlanner — the moon-run + last-mile machinery) and its own pulse kernel
// (OrbitRule.PulsesFor). Section A draws the pump map. Section B prices the reach from every
// representative ship state to every in-well pump. Section C sweeps remaining pulses to find the
// RED LINE per region — the crossing from "can refuel somewhere" to "stranded". Section D answers
// #146 directly: the reserve a ship must refuse to dip below during a moon approach deep in a
// giant's well, and why the flat 18% autopilot reserve is not enough out there. Section E runs the
// new Core service (FuelReachability) and shows its verdicts agree with the measured tables.
//
// IRONCLAD RULE: every number below came from running this probe. If you change the code, the
// printed numbers in labs/28-the-pump-crawl/README.md go stale — rerun and re-paste, never
// hand-edit a table.

using SpaceSails.Core;

const double SunMu = 1.32712440018e20;
const double SaturnMu = 3.7931187e16;

// Full sol.json field: sun, planets, moons, the two Saturn-ring commerce ports (Ringside Exchange,
// a station+haven) and Earth's Highport Satellite Works (a station), with the haven flags the live
// scenario carries — so GenerateDepots below picks EXACTLY the pumps the game spawns, and every
// reach is priced in the live heliocentric frame (pulse pricing reads WORLD speed).
(string Id, string Name, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase, BodyKind Kind, bool Haven)[] specs =
[
    ("sun", "Sun", "", SunMu, 6.9634e8, 0, 0, 0, BodyKind.Planet, false),
    ("mercury", "Mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0, BodyKind.Planet, false),
    ("venus", "Venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9, BodyKind.Planet, false),
    ("earth", "Earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8, BodyKind.Planet, false),
    ("luna", "Luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0, BodyKind.Moon, false),
    ("satellite-factory", "Highport Satellite Works", "earth", 0, 300, 6.771e6, 5546, 2.4, BodyKind.Station, false),
    ("mars", "Mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7, BodyKind.Planet, false),
    ("jupiter", "Jupiter", "sun", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6, BodyKind.Planet, false),
    ("saturn", "Saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5, BodyKind.Planet, false),
    ("titan", "Titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon, false),
    ("enceladus", "Enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon, true),
    ("ringside-exchange", "Ringside Exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station, true),
    ("uranus", "Uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4, BodyKind.Planet, false),
    ("neptune", "Neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4, BodyKind.Planet, false),
];

var field = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Name, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase, s.Kind, s.Haven))]);

var sim = new Simulator(field, timeStepSeconds: 60);
const double t0 = 0.0;

// The base tank the client hands the captain (Map.razor: _reactionMassPulses = 250). Every red
// line below is a raw pulse count independent of the tank; the tank only fixes where the flat 18%
// autopilot reserve (AutopilotRehearsal.ReserveFraction) sits, for the comparison in Sections C/D.
const int BaseTankCapacity = 250;
int flatReserve = AutopilotRehearsal.ReservePulses(BaseTankCapacity);

// A moon "doorstep": riding the moon's rail velocity, offset radially outward past its Hill sphere
// so it reads as free-flying in the PARENT's well (the state the arm-click hands the planner — the
// planner rightly refuses a departure from inside a moon's Hill sphere). Enceladus's Hill is tiny
// (~1e3 km) so lab 23's fixed 3,000 km clears it; Titan's is ~52,000 km, so we scale by the Hill.
ShipState Doorstep(string moonId)
{
    CelestialBody moon = field.Bodies.First(b => b.Id == moonId);
    double hill = OrbitRule.HillRadius(moon, SaturnMuOrEarthMu(moon.ParentId!));
    double offset = Math.Max(3e6, hill * 2.0);
    Vector2d pos = field.Position(moonId, t0);
    Vector2d parentPos = field.Position(moon.ParentId!, t0);
    Vector2d outward = (pos - parentPos).Normalized();
    Vector2d vel = TransferMath.BodyVelocity(field, moonId, t0);
    return new ShipState(pos + outward * offset, vel, t0);
}

double SaturnMuOrEarthMu(string parentId) => field.Bodies.First(b => b.Id == parentId).Mu;

// A ship genuinely PARKED at a moon: inside its Hill sphere, near-rest in the moon's frame — the
// state the fuel alarm actually reads while orbiting a dry moon. The service lifts this to the
// moon's doorstep to price the crawl (the planner refuses a departure from inside a moon's Hill).
ShipState ParkedAt(string moonId)
{
    CelestialBody moon = field.Bodies.First(b => b.Id == moonId);
    double hill = OrbitRule.HillRadius(moon, SaturnMuOrEarthMu(moon.ParentId!));
    Vector2d pos = field.Position(moonId, t0);
    Vector2d parentPos = field.Position(moon.ParentId!, t0);
    Vector2d outward = (pos - parentPos).Normalized();
    double altitude = Math.Max(moon.BodyRadius * 1.5, hill * 0.3);
    Vector2d vel = TransferMath.BodyVelocity(field, moonId, t0);
    return new ShipState(pos + outward * altitude, vel, t0);
}

// A ship DOCKED at a mass-less station: inside its dock envelope, matched to its rail velocity.
ShipState DockedAt(string stationId)
{
    Vector2d pos = field.Position(stationId, t0);
    Vector2d vel = TransferMath.BodyVelocity(field, stationId, t0);
    return new ShipState(pos + new Vector2d(1e6, 0), vel, t0);
}

// A free-flying cruise state on a circular orbit about a parent at the given radius and polar angle
// (parent-relative), riding the local circular velocity — a ship mid-transfer, deep in the well.
ShipState CruiseState(string parentId, double radius, double angle)
{
    Vector2d parentPos = field.Position(parentId, t0);
    Vector2d parentVel = TransferMath.BodyVelocity(field, parentId, t0);
    double mu = field.Bodies.First(b => b.Id == parentId).Mu;
    Vector2d rel = new Vector2d(Math.Cos(angle), Math.Sin(angle)) * radius;
    Vector2d tangent = new Vector2d(-Math.Sin(angle), Math.Cos(angle)); // CCW prograde
    double vCirc = Math.Sqrt(mu / radius);
    return new ShipState(parentPos + rel, parentVel + tangent * vCirc, t0);
}

// The reach to ONE pump: the planner's honest pulse estimate to ride the well onto that body, or an
// honest refusal. MaxWaitSeconds:0 lets the planner size its own wait window (one full synodic).
(bool Ok, int Pulses, string Note) Reach(ShipState ship, string parentId, string pumpId)
{
    var r = TransferPlanner.Solve(sim, field, new TransferPlanner.Request(ship, parentId, pumpId, MaxWaitSeconds: 0));
    return r.Ok ? (true, r.EstimatedPulses, r.Summary) : (false, int.MaxValue, r.Failure ?? "refused");
}

// The cheapest reachable pump in a well, skipping any pump the ship is already sitting at (reach 0).
(string PumpId, int Pulses) NearestPump(ShipState ship, string parentId, string[] pumps, string? atPump = null)
{
    string best = "(none)";
    int bestP = int.MaxValue;
    foreach (string p in pumps)
    {
        if (p == atPump)
        {
            return (p, 0); // already alongside the pump — refuel is a click, not a trip
        }

        var (ok, pulses, _) = Reach(ship, parentId, p);
        if (ok && pulses < bestP)
        {
            (best, bestP) = (p, pulses);
        }
    }

    return (best, bestP);
}

string[] saturnPumps = ["enceladus", "ringside-exchange"];
string[] earthPumps = ["satellite-factory"];

// ===================================================================================
// Section A — the pump map (which bodies carry a fuel depot, and which do not)
// ===================================================================================
Console.WriteLine("=== Section A: the pump map ===");
Console.WriteLine("TrafficSchedule.GenerateDepots spawns one depot at every planet, every station and every");
Console.WriteLine("pirate haven — and NONE at an ordinary moon. This is the game's own list, printed:");
Console.WriteLine();
var depots = TrafficSchedule.GenerateDepots(field, seed: 1);
var depotHosts = depots.Select(d => d.DepotBodyId).ToHashSet();
Console.WriteLine($"{"body",-26}{"kind",-10}{"haven",-8}{"has a pump?",-12}");
Console.WriteLine(new string('-', 56));
foreach (var s in specs)
{
    if (s.Id == "sun")
    {
        continue;
    }

    bool hasPump = depotHosts.Contains(s.Id);
    Console.WriteLine($"{s.Name,-26}{s.Kind,-10}{(s.Haven ? "yes" : "no"),-8}{(hasPump ? "PUMP" : "— dry —"),-12}");
}

Console.WriteLine();
Console.WriteLine("Read the two moons a captain actually parks at: Titan and Luna are DRY. To refuel from");
Console.WriteLine("either you must first cross back to a pump — the crawl this lesson prices.");
Console.WriteLine();

// ===================================================================================
// Section B — the reach: pulses from each representative state to each in-well pump
// ===================================================================================
Console.WriteLine("=== Section B: the reach (pulses to each pump, priced by TransferPlanner) ===");
Console.WriteLine("Each cell is the game's own estimate to ride the well onto that pump — or its honest");
Console.WriteLine("refusal. 'at pump' = the ship is already docked there (refuel is a click).");
Console.WriteLine();

var states = new (string Label, ShipState Ship, string Parent, string[] Pumps, string? AtPump)[]
{
    ("Enceladus doorstep (haven moon)", Doorstep("enceladus"), "saturn", saturnPumps, "enceladus"),
    ("Titan doorstep (DRY moon)",       Doorstep("titan"),     "saturn", saturnPumps, null),
    ("Saturn mid-well cruise",          CruiseState("saturn", 6.0e8, 0.5), "saturn", saturnPumps, null),
    ("Ringside dock (haven station)",   Doorstep("enceladus"), "saturn", saturnPumps, "ringside-exchange"),
    ("Luna doorstep (DRY moon)",        Doorstep("luna"),      "earth",  earthPumps, null),
};

Console.WriteLine($"{"ship state",-34}{"-> Enceladus",14}{"-> Ringside",14}{"-> Highport",14}{"cheapest",12}");
Console.WriteLine(new string('-', 88));
foreach (var st in states)
{
    string EncCell()
    {
        if (st.AtPump == "enceladus")
        {
            return "at pump";
        }

        if (!saturnPumps.Contains("enceladus") || st.Parent != "saturn")
        {
            return "-";
        }

        var (ok, p, _) = Reach(st.Ship, "saturn", "enceladus");
        return ok ? p.ToString() : "refused";
    }

    string RingCell()
    {
        if (st.AtPump == "ringside-exchange")
        {
            return "at pump";
        }

        if (st.Parent != "saturn")
        {
            return "-";
        }

        var (ok, p, _) = Reach(st.Ship, "saturn", "ringside-exchange");
        return ok ? p.ToString() : "refused";
    }

    string HighCell()
    {
        if (st.Parent != "earth")
        {
            return "-";
        }

        var (ok, p, _) = Reach(st.Ship, "earth", "satellite-factory");
        return ok ? p.ToString() : "refused";
    }

    var (pump, cheapest) = NearestPump(st.Ship, st.Parent, st.Pumps, st.AtPump);
    string cheapStr = cheapest == 0 ? "0 (at pump)" : cheapest == int.MaxValue ? "STRANDED" : $"{cheapest} ({pump.Replace("-exchange", "").Replace("satellite-factory", "highport")})";
    Console.WriteLine($"{st.Label,-34}{EncCell(),14}{RingCell(),14}{HighCell(),14}{cheapStr,12}");
}

Console.WriteLine();

// ===================================================================================
// Section C — the red line per region (sweep remaining pulses to the crossing)
// ===================================================================================
Console.WriteLine("=== Section C: the red line per region ===");
Console.WriteLine("The RED LINE is the cheapest-pump reach from the worst state a captain reaches in that");
Console.WriteLine("region: dip one pulse below it and no pump is reachable — the ship is stranded. Compared");
Console.WriteLine($"to the flat 18% autopilot reserve ({flatReserve} p on the base {BaseTankCapacity}-pulse tank):");
Console.WriteLine();

var regions = new (string Region, ShipState Worst, string Parent, string[] Pumps)[]
{
    ("inner Saturn moons (parked at Titan)", Doorstep("titan"), "saturn", saturnPumps),
    ("Saturn well cruise",                   CruiseState("saturn", 6.0e8, 0.5), "saturn", saturnPumps),
    ("Luna neighborhood (parked at Luna)",   Doorstep("luna"), "earth", earthPumps),
};

Console.WriteLine($"{"region",-40}{"red line (p)",14}{"nearest pump",22}{"flat 18% covers?",18}");
Console.WriteLine(new string('-', 94));
foreach (var rg in regions)
{
    var (pump, reach) = NearestPump(rg.Worst, rg.Parent, rg.Pumps);
    string covers = reach == int.MaxValue ? "n/a" : (flatReserve >= reach ? "yes" : $"NO (short {reach - flatReserve} p)");
    Console.WriteLine($"{rg.Region,-40}{reach,14}{pump,22}{covers,18}");
}

Console.WriteLine();
Console.WriteLine("A small margin sweep at the worst region (parked at Titan), verdict at each remaining level:");
Console.WriteLine();
{
    var worst = Doorstep("titan");
    var (pump, reach) = NearestPump(worst, "saturn", saturnPumps);
    int[] sweep = [reach + 40, reach + 10, reach + 1, reach, reach - 1, reach - 20, flatReserve, 10];
    Console.WriteLine($"(cheapest reach to {pump} = {reach} pulses)");
    Console.WriteLine($"{"remaining pulses",-20}{"margin",10}{"can reach a pump?",22}");
    Console.WriteLine(new string('-', 52));
    foreach (int rem in sweep.Distinct().OrderByDescending(x => x))
    {
        int margin = rem - reach;
        string verdict = margin >= 0 ? "yes" : "NO — STRANDED";
        Console.WriteLine($"{rem,-20}{margin,10}{verdict,22}");
    }
}

Console.WriteLine();

// ===================================================================================
// Section D — the #146 reserve (what to refuse to dip below in a giant's well)
// ===================================================================================
Console.WriteLine("=== Section D: the #146 reserve — a well-aware floor ===");
Console.WriteLine("#146: 'what reserve should the ship refuse to dip below during a moon approach deep in a");
Console.WriteLine("giant's well?' The answer is not a flat fraction of the tank — it is the fare back to a");
Console.WriteLine("pump from where the approach leaves you. Two pumps serve the Saturn well: Ringside (a ring");
Console.WriteLine("station, cheap only when its phase is favourable) and Enceladus (the inner haven — always");
Console.WriteLine("there on the inner lane). Prudence prices the reach to the ALWAYS-there haven:");
Console.WriteLine();
{
    var titan = Doorstep("titan");
    var (_, reachRing) = (0, Reach(titan, "saturn", "ringside-exchange").Pulses);
    int reachEnc = Reach(titan, "saturn", "enceladus").Pulses;
    int nearest = Math.Min(reachRing, reachEnc);
    int wellAwareReserve = nearest + flatReserve;

    Console.WriteLine($"reach parked-at-Titan -> Ringside (phase-dependent) : {reachRing} pulses");
    Console.WriteLine($"reach parked-at-Titan -> Enceladus (always-there)   : {reachEnc} pulses");
    Console.WriteLine($"flat 18% autopilot reserve on the base tank         : {flatReserve} pulses");
    Console.WriteLine($"RECOMMENDED well-aware reserve (nearest + cushion)  : {wellAwareReserve} pulses");
    Console.WriteLine();
    Console.WriteLine($"The reliable-haven reach ({reachEnc} p) already {(reachEnc > flatReserve ? $"EXCEEDS the flat 45-p reserve by {reachEnc - flatReserve} p" : "sits under the flat reserve")}: a ship");
    Console.WriteLine($"that held only the flat floor and then found Ringside out of phase would be a burn short of");
    Console.WriteLine($"the only pump it could count on — the #146 starve. The autopilot must quote and hold a reserve");
    Console.WriteLine($"that RIDES the reach ({wellAwareReserve} p here), not a fixed fraction the well outgrows.");
}

Console.WriteLine();

// ===================================================================================
// Section E — the Core service (FuelReachability) agrees with the tables
// ===================================================================================
Console.WriteLine("=== Section E: FuelReachability (the Core service the game will call) ===");
Console.WriteLine("Given (state, remaining pulses, tank), the service returns nearest-pump cost, margin and a");
Console.WriteLine("verdict. Here it is run on the representative states at the base 250-pulse tank, half full:");
Console.WriteLine();
Console.WriteLine($"{"ship state",-34}{"remaining",11}{"nearest",10}{"margin",9}{"verdict",22}");
Console.WriteLine(new string('-', 86));
var svcStates = new (string Label, ShipState Ship, string Parent, int Remaining)[]
{
    ("Parked at Enceladus (at pump)", ParkedAt("enceladus"), "saturn", 30),
    ("Docked at Ringside (at pump)",  DockedAt("ringside-exchange"), "saturn", 30),
    ("Parked at Titan, half tank",    ParkedAt("titan"),     "saturn", 125),
    ("Parked at Titan, thin",         ParkedAt("titan"),     "saturn", 60),
    ("Parked at Titan, stranded",     ParkedAt("titan"),     "saturn", 20),
    ("Saturn cruise, half tank",      CruiseState("saturn", 6.0e8, 0.5), "saturn", 125),
    ("Parked at Luna (dry, no pump)", ParkedAt("luna"),      "earth",  125),
};
foreach (var s in svcStates)
{
    var a = FuelReachability.Assess(sim, field, s.Ship, s.Remaining, BaseTankCapacity, s.Parent);
    string nearest = a.NearestDepotPulses == int.MaxValue ? "none" : a.NearestDepotPulses.ToString();
    string margin = a.NearestDepotPulses == int.MaxValue ? "-" : a.MarginPulses.ToString();
    Console.WriteLine($"{s.Label,-34}{s.Remaining,11}{nearest,10}{margin,9}{a.Verdict,22}");
}

Console.WriteLine();
Console.WriteLine("Thresholds embedded in the service are sourced from THIS lab's measured tables (Sections");
Console.WriteLine("C/D). The banner strip and parrot squawk (#166) read the verdict; the red line is the");
Console.WriteLine("'can't reach a pump' crossing, the amber line the well-aware reserve.");
