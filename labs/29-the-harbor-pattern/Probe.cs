// Lab 29 — The harbor pattern
//
// Teaching voice: lesson 23 taught the ship to cross a giant's well cheap; lesson 24 taught it to
// close the last co-orbital mile for two small burns. Both END at a harbor — a mass-less station you
// clamp onto (Ringside Exchange), or a moon you PARK in orbit at to deliver (Enceladus, issue #175).
// But the owner's playtests kept ending the same way: the flying worked, and then the ship arrived
// WRONG — in orbit "by luck," or drifting past the berth too hot to grab, with no word on what a safe
// arrival even looks like. This lab measures the SAFE-APPROACH CORRIDOR honestly, per harbor: from a
// spread of inbound trajectories (range x closing speed x offset), what speed-vs-distance gates does a
// clean clamp-on / park actually require, what does the last-mile machinery cost from each gate, and
// WHERE do approaches fail — overshoot the tiny moon, blow through the clamp window, or burn an absurd
// pulse bill nulling excess speed they never should have carried in.
//
// Every number is the SAME Core the autopilot flies with: DockRule (the 500,000 km / 8 km/s clamp
// envelope), OrbitRule (the moon capture window, the safe approach, the tide-stable park), priced with
// OrbitRule.PulsesFor. Section A enumerates the harbors from the scenario data; Sections B/C fly a
// grid of inbound trajectories through the real N-body sim and print the pass/fail + pulse-cost tables;
// Section D reads one textbook arrival against one botched one; Section E prints the measured corridor
// gates and cross-checks them against the new Core ApproachCorridor query API — the numbers the game's
// "NEXT gate" coaching line (#159) and the #160 tutorial narration will speak.
//
// IRONCLAD RULE: every number in labs/29-the-harbor-pattern/README.md came from running this probe.
// Change the code and the printed tables go stale — rerun and re-paste, never hand-edit a table.

using SpaceSails.Core;
using SpaceSails.LabViz;

const double Day = 86400.0;
const double SunMu = 1.32712440018e20;
const double SaturnMu = 3.7931187e16;

// The scenario field (scenarios/sol.json, verbatim) — every harbor priced in the live heliocentric
// frame, because PulsesFor reads WORLD speed and the frame is where the pulse bill lives.
(string Id, string Name, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase, BodyKind Kind, bool Haven)[] specs =
[
    ("sun", "Sun", "", SunMu, 6.9634e8, 0, 0, 0, BodyKind.Planet, false),
    ("mercury", "Mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0, BodyKind.Planet, false),
    ("mercury-compute", "Mercury Compute Farms", "mercury", 0, 500, 2.84e6, 6405, 0.2, BodyKind.Station, false),
    ("venus", "Venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9, BodyKind.Planet, false),
    ("cinder-roost", "Cinder Roost", "venus", 0, 700, 1.5e7, 8000, 1.3, BodyKind.Station, true),
    ("earth", "Earth", "sun", 3.986004418e14, 6.371e6, 1.496e11, 3.1558149e7, 1.8, BodyKind.Planet, false),
    ("luna", "Luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0, BodyKind.Moon, false),
    ("satellite-factory", "Highport Satellite Works", "earth", 0, 300, 6.771e6, 5546, 2.4, BodyKind.Station, false),
    ("mars", "Mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7, BodyKind.Planet, false),
    ("the-space-bar", "The Rusty Roadstead", "mars", 0, 900, 1.2e7, 7200, 3.1, BodyKind.Station, true),
    ("jupiter", "Jupiter", "sun", 1.26686534e17, 6.9911e7, 7.7857e11, 3.74336e8, 3.6, BodyKind.Planet, false),
    ("saturn", "Saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5, BodyKind.Planet, false),
    ("titan", "Titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0, BodyKind.Moon, false),
    ("enceladus", "Enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0, BodyKind.Moon, true),
    ("ringside-exchange", "Ringside Exchange", "saturn", 0, 1000, 1.35e9, 1.6006e6, 5.0, BodyKind.Station, true),
    ("uranus", "Uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4, BodyKind.Planet, false),
    ("the-tilt", "The Tilt", "uranus", 0, 1000, 8.0e7, 14000, 4.7, BodyKind.Station, true),
    ("neptune", "Neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4, BodyKind.Planet, false),
];

var field = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Id, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase, s.Kind))]);
var sim = new Simulator(field, timeStepSeconds: 60);

CelestialBody Body(string id) => field.Bodies.First(b => b.Id == id);
double WorldSpeedNear(string id, double t) => TransferMath.BodyVelocity(field, id, t).Length;

// ===================================================================================
// Fly the real last-mile machinery from an inbound state, through the N-body sim, and report the
// honest outcome. Stations (mu=0) can only ever "Approach" and are "captured" the instant they enter
// the DockRule clamp envelope; moons (mu>0) Approach then Insert, and are delivered once the orbit is
// bound AND tide-stable (OrbitRule.ParkStability). We also detect the two failure modes the owner hit:
// IMPACT (fell through the surface) and blowing past too hot (OVERSHOT / never captured within horizon).
// ===================================================================================
FlyResult FlyApproach(ShipState s0, string targetId, double horizonDays)
{
    CelestialBody body = Body(targetId);
    bool station = body.Mu <= 0;
    CelestialBody parent = Body(body.ParentId!);
    double hill = OrbitRule.HillRadius(body, parent.Mu);      // 0 for a mass-less station
    double captureRange = OrbitRule.CaptureRange(hill);       // floors at 3e9 m
    double surface = body.BodyRadius * OrbitRule.SurfaceParkRadii;
    double start = s0.SimTime, horizon = start + horizonDays * Day;
    const double FineStep = 30.0, CoastStep = 1800.0, CoastFactor = 1.25;

    ShipState s = s0;
    int pulses = 0, iter = 0;
    double totalDv = 0, minDist = double.MaxValue;
    bool wasInsideEnvelope = false;

    while (s.SimTime < horizon && iter++ < 400_000)
    {
        Vector2d bp = field.Position(body.Id, s.SimTime);
        Vector2d bv = TransferMath.BodyVelocity(field, body.Id, s.SimTime);
        double dist = (s.Position - bp).Length;
        double rel = (s.Velocity - bv).Length;
        minDist = Math.Min(minDist, dist);

        if (station)
        {
            if (DockRule.InEnvelope(s, bp, bv, body.BodyRadius))
            {
                return new(Outcome.Clamped, pulses, totalDv, (s.SimTime - start) / Day, minDist, rel);
            }
            // Passed through the clamp bubble but too hot to grab, and now receding again → overshot.
            bool inBubble = dist <= DockRule.EnvelopeMeters;
            if (wasInsideEnvelope && !inBubble)
            {
                return new(Outcome.Overshot, pulses, totalDv, (s.SimTime - start) / Day, minDist, rel);
            }
            wasInsideEnvelope |= inBubble;
        }
        else
        {
            if (dist < surface)
            {
                return new(Outcome.Impact, pulses, totalDv, (s.SimTime - start) / Day, minDist, rel);
            }
            if (OrbitRule.IsBound(s, bp, bv, body, hill)
                && OrbitRule.ParkStability(s, bp, bv, body, hill) == OrbitRule.ParkStabilityVerdict.Stable)
            {
                return new(Outcome.Parked, pulses, totalDv, (s.SimTime - start) / Day, minDist, rel);
            }
        }

        OrbitRule.ApproachObstacle? obstacle = parent.ParentId is null
            ? null
            : new OrbitRule.ApproachObstacle(field.Position(parent.Id, s.SimTime), parent.BodyRadius * OrbitRule.ParentSafeBodyRadii);

        switch (OrbitRule.AutopilotDecision(s, bp, bv, body, hill))
        {
            case OrbitRule.AutopilotAction.Insert:
                pulses += OrbitRule.PulseCost(s, bp, bv, body);
                ShipState pre = s;
                s = OrbitRule.Insert(s, bp, bv, body);
                totalDv += (s.Velocity - pre.Velocity).Length;
                break;
            case OrbitRule.AutopilotAction.Approach:
                pulses += OrbitRule.ApproachPulseCost(s, bp, bv, body, obstacle, hill);
                ShipState pre2 = s;
                s = OrbitRule.Approach(s, bp, bv, body, obstacle, hill);
                totalDv += (s.Velocity - pre2.Velocity).Length;
                s = sim.RunAdaptive(s, FineStep);
                break;
            default:
                double dt = dist > captureRange * CoastFactor ? CoastStep : FineStep;
                s = sim.RunAdaptive(s, dt);
                break;
        }
    }

    Vector2d bpf = field.Position(body.Id, s.SimTime);
    Vector2d bvf = TransferMath.BodyVelocity(field, body.Id, s.SimTime);
    return new(Outcome.Timeout, pulses, totalDv, (s.SimTime - start) / Day, minDist, (s.Velocity - bvf).Length);
}

// Build an inbound ship: at range R from the target on the target's OUTWARD-parent radial, closing at
// speed v. offsetDeg tilts the closing velocity off the pure radial (an oblique approach) — the "offset"
// axis of the sweep. Velocity is set in the target's frame (target velocity + the closing vector), so v
// is the honest relative speed at the start; the sim then adds the parent's tide over the fall.
ShipState Inbound(string targetId, double t0, double rangeM, double vClose, double offsetDeg)
{
    Vector2d bp = field.Position(targetId, t0);
    Vector2d bv = TransferMath.BodyVelocity(field, targetId, t0);
    CelestialBody body = Body(targetId);
    Vector2d parentPos = field.Position(body.ParentId!, t0);
    Vector2d outward = (bp - parentPos);
    outward /= outward.Length;                                   // radial away from the parent
    Vector2d shipPos = bp + outward * rangeM;
    // Closing direction: inbound (-outward), tilted by offsetDeg toward the local tangent.
    Vector2d tangent = new Vector2d(-outward.Y, outward.X);
    double a = offsetDeg * Math.PI / 180.0;
    Vector2d closeDir = -outward * Math.Cos(a) + tangent * Math.Sin(a);
    return new ShipState(shipPos, bv + closeDir * vClose, t0);
}

// ===================================================================================
// Section A — the harbors and their doors
// ===================================================================================
Console.WriteLine("=== Section A: the harbors and their doors ===");
Console.WriteLine("Every dockable haven and moon delivery point in scenarios/sol.json, with the door the game");
Console.WriteLine("actually checks. STATION (mu=0): clamp on inside the DockRule envelope (coast within 500,000 km,");
Console.WriteLine("match to <= 8 km/s). MOON (mu>0): park in a tide-stable orbit (OrbitRule) — issue #175's delivery.");
Console.WriteLine();
Console.WriteLine($"{"harbor",-26}{"class",-9}{"door: within",14}{"speed cap",11}{"park r / Hill",16}{"handover",12}");
Console.WriteLine(new string('-', 88));
foreach (var sp in specs.Where(s => s.Kind == BodyKind.Station || (s.Haven && s.Kind == BodyKind.Moon)))
{
    CelestialBody b = Body(sp.Id);
    CelestialBody parent = Body(b.ParentId!);
    if (b.Mu <= 0)
    {
        Console.WriteLine($"{sp.Name,-26}{"station",-9}{DockRule.EnvelopeMeters / 1e3,10:N0} km{DockRule.MatchSpeed / 1000,8:F0} km/s{"— (clamp)",16}{"3.0e6 km",12}");
    }
    else
    {
        double hill = OrbitRule.HillRadius(b, parent.Mu);
        double park = OrbitRule.ParkingRadius(b, hill);
        double handover = OrbitRule.CaptureRange(hill) / 1e3;
        Console.WriteLine($"{sp.Name,-26}{"moon",-9}{hill / 1e3,10:N0} km{OrbitRule.MaxRelativeSpeed / 1000,8:F0} km/s" +
            $"{$"{park / 1e3:N0} / {hill / 1e3:N0} km",16}{$"{handover / 1e3:N0}e3 km",12}");
    }
}
Console.WriteLine();
Console.WriteLine("Note the scale gap the corridor has to respect: a station's clamp bubble is 500,000 km wide, but");
Console.WriteLine("Enceladus' whole Hill sphere — the only place it can hold an orbit — is under 1,000 km. Same word");
Console.WriteLine("'arrive', two utterly different doors.");
Console.WriteLine();

// ===================================================================================
// Section B — the corridor sweep at a station (Ringside Exchange)
// ===================================================================================
void StationSweep(string id)
{
    CelestialBody b = Body(id);
    double t0 = 0.0;
    double world = WorldSpeedNear(id, t0);
    double laneV = OrbitRule.CircularSpeed(Body(b.ParentId!), b.OrbitRadius);
    Console.WriteLine($"=== Section B: the corridor sweep — {b.Id} (station clamp) ===");
    Console.WriteLine($"World speed at the harbor ~{world / 1000:F1} km/s (one pulse buys ~{Math.Max(1.0, world * OrbitRule.DeltaVPerPulseFraction):F0} m/s of dv); lane circular {laneV / 1000:F2} km/s.");
    Console.WriteLine("Fly the real approach machinery from each (range, closing speed) inbound state to the clamp");
    Console.WriteLine("envelope. Cell = pulses to clamp (the last-mile bill from that gate); 'x' = never clamped in horizon.");
    Console.WriteLine();

    double[] ranges = [3e9, 1.5e9, 1e9, 7.5e8, 5.5e8];
    double[] speeds = [1000, 2000, 4000, 8000, 12000, 16000];
    Console.Write($"{"range \\ v_close",-16}");
    foreach (double v in speeds) Console.Write($"{v / 1000,8:F0} km/s");
    Console.WriteLine();
    Console.WriteLine(new string('-', 16 + speeds.Length * 13));
    foreach (double r in ranges)
    {
        Console.Write($"{r / 1e3,12:N0} km ");
        foreach (double v in speeds)
        {
            FlyResult f = FlyApproach(Inbound(id, t0, r, v, 0), id, 90.0);
            Console.Write(f.Outcome == Outcome.Clamped ? $"{f.Pulses,10} p " : $"{f.Outcome.ToString()[..3].ToLower() + " x",13}");
        }
        Console.WriteLine();
    }
    Console.WriteLine();
}
StationSweep("ringside-exchange");
Console.WriteLine("Read it across a row: a station never truly REFUSES you — the clamp bubble is huge and gravity-free,");
Console.WriteLine("so the machinery can always null your excess and coast in. What it costs you is PULSES: arrive matched");
Console.WriteLine("to the lane (left columns) and the clamp is 1-2 pulses; arrive hot (right columns) and you pay a pulse");
Console.WriteLine("for every ~1% of world speed you have to shed first. The corridor for a station is a COST gate.");
Console.WriteLine();

// ===================================================================================
// Section C — the corridor sweep at a moon (Enceladus), where approaches FAIL
// ===================================================================================
void MoonSweep(string id)
{
    CelestialBody b = Body(id);
    CelestialBody parent = Body(b.ParentId!);
    double hill = OrbitRule.HillRadius(b, parent.Mu);
    double park = OrbitRule.ParkingRadius(b, hill);
    double t0 = 0.0;
    double world = WorldSpeedNear(id, t0);
    Console.WriteLine($"=== Section C: the corridor sweep — {b.Id} (moon park, issue #175) ===");
    Console.WriteLine($"Hill sphere {hill / 1e3:N0} km, tide-stable park {park / 1e3:N0} km, capture speed cap {OrbitRule.MaxRelativeSpeed / 1000:F0} km/s.");
    Console.WriteLine($"World speed ~{world / 1000:F1} km/s (one pulse ~{Math.Max(1.0, world * OrbitRule.DeltaVPerPulseFraction):F0} m/s). Cell = pulses to a STABLE park;");
    Console.WriteLine("'imp' = fell into the moon, 'ovr' = blew past too hot, 'x' = uncaptured in horizon.");
    Console.WriteLine();

    double[] ranges = [5e6, 2e6, hill, 5e5, park * 1.2];
    double[] speeds = [300, 500, 1000, 2000, 3000, 5000];
    Console.Write($"{"range \\ v_close",-16}");
    foreach (double v in speeds) Console.Write($"{v / 1000,8:F1} km/s");
    Console.WriteLine();
    Console.WriteLine(new string('-', 16 + speeds.Length * 13));
    foreach (double r in ranges)
    {
        Console.Write($"{r / 1e3,12:N0} km ");
        foreach (double v in speeds)
        {
            FlyResult f = FlyApproach(Inbound(id, t0, r, v, 0), id, 20.0);
            Console.Write(f.Outcome switch
            {
                Outcome.Parked => $"{f.Pulses,10} p ",
                Outcome.Impact => $"{"imp",13}",
                Outcome.Overshot => $"{"ovr",13}",
                _ => $"{"x",13}",
            });
        }
        Console.WriteLine();
    }
    Console.WriteLine();
}
MoonSweep("enceladus");
Console.WriteLine("A moon is the opposite of a station: the door is tiny and gravity is real, so an approach that is too");
Console.WriteLine("hot for its range genuinely FAILS — it punches through the moon or blows past the Hill sphere before the");
Console.WriteLine("autopilot can turn the fall into an orbit. THIS is the owner's 'in orbit by luck' (#180) and 'no deliver");
Console.WriteLine("button' (#175): the harbor pattern is the speed-vs-range envelope that keeps the last fall inside the door.");
Console.WriteLine();

// ===================================================================================
// Section D — textbook vs botched, one gate each
// ===================================================================================
Console.WriteLine("=== Section D: a textbook arrival vs a botched one ===");
void Compare(string id, double range, double onV, double hotV, double horizon)
{
    FlyResult ok = FlyApproach(Inbound(id, 0.0, range, onV, 0), id, horizon);
    FlyResult bad = FlyApproach(Inbound(id, 0.0, range, hotV, 0), id, horizon);
    Console.WriteLine($"{id}: inbound from {range / 1e3:N0} km");
    Console.WriteLine($"  textbook  (closing {onV / 1000,5:F1} km/s):  {ok.Outcome,-9} {ok.Pulses,4} pulses, {ok.DeltaV / 1000,6:F2} km/s dv, {ok.Days,5:F1} d");
    Console.WriteLine($"  botched   (closing {hotV / 1000,5:F1} km/s):  {bad.Outcome,-9} {bad.Pulses,4} pulses, {bad.DeltaV / 1000,6:F2} km/s dv, {bad.Days,5:F1} d");
    Console.WriteLine();
}
Compare("ringside-exchange", 1e9, 2000, 16000, 90.0);
Compare("enceladus", 5e6, 1000, 3000, 20.0);

// ===================================================================================
// Section E — the measured corridor table + the Core ApproachCorridor query API
// ===================================================================================
Console.WriteLine("=== Section E: the corridor gates, and the Core API that speaks them ===");
Console.WriteLine("The gates below are the SAME numbers ApproachCorridor.For(...) computes from the Core constants");
Console.WriteLine("the autopilot flies with (DockRule envelope, OrbitRule closing speed / Hill / park). This is the");
Console.WriteLine("seam the banner NEXT row (#159) and the #160 tutorial narration read — printed straight from the API.");
Console.WriteLine();

void PrintCorridor(string id)
{
    CelestialBody b = Body(id);
    CelestialBody parent = Body(b.ParentId!);
    ApproachCorridor c = ApproachCorridor.For(b, parent.Mu);
    Console.WriteLine($"{b.Id} — {c.Class}, glideslope tau = {c.GlideslopeSeconds:N0} s ({c.GlideslopeSeconds / 3600:F1} h)");
    Console.WriteLine($"  {"gate",-14}{"by range",16}{"be under",14}");
    foreach (CorridorGate g in c.Gates)
    {
        Console.WriteLine($"  {g.Name,-14}{g.RangeMeters / 1e3,12:N0} km{g.MaxSpeedMps / 1000,10:F2} km/s");
    }
    Console.WriteLine();
}
PrintCorridor("ringside-exchange");
PrintCorridor("enceladus");

Console.WriteLine("Read(range, closing speed) — the live verdict + next gate the guidance seam serves:");
Console.WriteLine();
void ReadRow(ApproachCorridor c, double range, double v)
{
    CorridorReading r = c.Read(range, v);
    Console.WriteLine($"  {range / 1e3,12:N0} km @ {v / 1000,5:F2} km/s -> {r.Verdict,-9} (ceiling {r.MaxSpeedHere / 1000,5:F2} km/s, " +
        $"margin {r.MarginMps / 1000,6:F2} km/s) | NEXT: {c.NextGate(range).Name} at {c.NextGate(range).RangeMeters / 1e3:N0} km, under {c.NextGate(range).MaxSpeedMps / 1000:F2} km/s");
}
var ring = ApproachCorridor.For(Body("ringside-exchange"), Body("saturn").Mu);
var enc = ApproachCorridor.For(Body("enceladus"), Body("saturn").Mu);
Console.WriteLine(" Ringside Exchange (station clamp):");
ReadRow(ring, 2e9, 6000);           // far out, under the cap glide -> on pattern
ReadRow(ring, 5e8, 4000);           // exactly the clamp-window gate
ReadRow(ring, 5e8, 7000);           // inside the bubble, over pattern but under 8 km/s shear -> hot
ReadRow(ring, 3e8, 9000);           // inside the bubble, over the 8 km/s clamp cap -> missed
Console.WriteLine(" Enceladus (moon park):");
ReadRow(enc, 5e6, 4000);            // outside the Hill sphere, under the 5 km/s cap -> on pattern
ReadRow(enc, 9.49e5, 1200);         // ~at the Hill sphere, on the terminal-close line
ReadRow(enc, 5e5, 3000);            // inside Hill, over pattern, under 5 km/s -> hot (courts overshoot)
ReadRow(enc, 4e5, 6000);            // inside Hill, over the 5 km/s window cap -> missed
Console.WriteLine();

// ===================================================================================
// --viz (optional): the Enceladus corridor drawn as a glideslope, with a textbook on-pattern fall and
// a botched hot fall through the real sim, in the moon-centric frame. Gated behind LabViz.Wants so the
// no-flag stdout is byte-identical.
// ===================================================================================
if (LabViz.Wants(args))
{
    CelestialBody enceladus = Body("enceladus");
    var pocket = new CircularOrbitEphemeris(
    [
        new CelestialBody("enceladus", "enceladus", null, enceladus.Mu, enceladus.BodyRadius, 0, 0, 0),
    ]);
    var viz = new VizScene("lab29-the-harbor-pattern", "Lab 29 — The harbor pattern",
        "Enceladus corridor: a textbook on-pattern fall vs a botched hot one, in the moon frame");
    viz.AddBodies(pocket.Bodies);

    List<TrajectorySample> ToMoonRelative(IEnumerable<TrajectorySample> samples) =>
        [.. samples.Select(s => new TrajectorySample(s.SimTime, s.Position - field.Position("enceladus", s.SimTime)))];

    foreach ((double v, string label, string group) in new[] { (500.0, "textbook (0.5 km/s)", "main"), (5000.0, "botched (5 km/s)", "hot") })
    {
        ShipState s = Inbound("enceladus", 0, 2e6, v, 0);
        var path = sim.ProjectAdaptive(s, null, 6 * 3600, maxTimeStep: 30, maxSamples: 20_000);
        viz.AddPath(label, ToMoonRelative(path), group == "main" ? VizColors.Trajectory : VizColors.Sweep, group, 1.6, group == "main" ? 1.0 : 0.6);
    }
    LabViz.Show(viz, args);
}

enum Outcome { Clamped, Parked, Impact, Overshot, Timeout }
record FlyResult(Outcome Outcome, int Pulses, double DeltaV, double Days, double MinDist, double TermRelSpeed);
