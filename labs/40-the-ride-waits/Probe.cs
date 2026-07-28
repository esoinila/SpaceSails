// Lab 40 — The ride waits
//
// Teaching voice: the owner wants boardable places that are neither worlds nor stations — a derelict
// hulk you walk around inside and strip (issue #488). A landing works because the ground holds still
// under you; a station works because you clamp to it. A derelict does NEITHER: it is a free-floating
// object on its own trajectory, and the moment the away team leaves, the mothership is a second
// free-floating object on a slightly different one.
//
//   "our autopilot would have to have a new mode (that does not cost an arm and a leg) that just keeps
//    us at approximate relative shuttle-range position … that the ship does not leave them behind :-D"
//
// Before writing that mode, this lab asks the three questions whose answers decide its design:
//
//   A — DOES IT EVEN NEED TO EXIST? Park the ship alongside a derelict, thrust NOTHING, and time how
//       long until the gap opens past the hold band. If that is days everywhere, the mode is a luxury.
//       If it is minutes somewhere, that somewhere is where the fiction lives.
//   B — WHAT DOES THE HAND-OFF COST? A real drop is never perfectly matched. Sweep a residual relative
//       velocity and watch the endurance collapse — this prices "match before you launch the shuttle".
//   C — WHAT DOES HOLDING COST? Fly a dead-band keeper (trim only when it drifts out of the band, the
//       owner's "approximate") and price it in PULSES PER DAY through the same OrbitRule kernel the
//       autopilot spends with. That number is the one the arm-time quote has to show.
//
// The hypothesis under test (issue #488): holding station at shuttle range costs ~nothing outside a
// gravity well and scales with the local TIDAL GRADIENT — because two objects at the same place with
// the same velocity stay together for free, and what separates them is the difference in the pull they
// feel. If this lab refutes that, the feature's design changes before a line of it ships.
//
// Every number is the SAME Core code the game spends with: the live n-body Simulator, ShuttleRange for
// the band, and OrbitRule.PulsesFor for the bill.
//
// IRONCLAD RULE: every number in labs/40-the-ride-waits/README.md came from running this probe. If you
// change the code, rerun and re-paste — never hand-edit a table.

using System.Globalization;
using SpaceSails.Core;

const double Day = 86400.0;
const double Hour = 3600.0;
const double SunMu = 1.32712440018e20;
const double SaturnMu = 3.7931187e16;
const double EarthMu = 3.986004418e14;
const double JupiterMu = 1.26686534e17;

// Lab 17's sol.json field, verbatim — every bill priced in the live game's heliocentric frame.
(string Id, string Parent, double Mu, double BodyRadius, double OrbitRadius, double OrbitPeriod, double Phase)[] specs =
[
    ("sun", "", SunMu, 6.9634e8, 0, 0, 0),
    ("mercury", "sun", 2.2032e13, 2.4397e6, 5.791e10, 7.60052e6, 0.0),
    ("venus", "sun", 3.24859e14, 6.0518e6, 1.0821e11, 1.94142e7, 0.9),
    ("earth", "sun", EarthMu, 6.371e6, 1.496e11, 3.1558149e7, 1.8),
    ("mars", "sun", 4.282837e13, 3.3895e6, 2.2794e11, 5.93551e7, 2.7),
    ("jupiter", "sun", JupiterMu, 6.9911e7, 7.7857e11, 3.74336e8, 3.6),
    ("saturn", "sun", SaturnMu, 5.8232e7, 1.43353e12, 9.29596e8, 4.5),
    ("uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4),
    ("neptune", "sun", 6.836529e15, 2.4622e7, 4.49506e12, 5.2004e9, 0.4),
    ("luna", "earth", 4.9048695e12, 1.7374e6, 3.844e8, 2.3606e6, 0.0),
    ("titan", "saturn", 8.9781e12, 2.575e6, 1.22183e9, 1.377648e6, 1.0),
    ("enceladus", "saturn", 7.211e9, 2.5226e5, 2.38037e8, 1.183868e5, 2.0),
];

var field = new CircularOrbitEphemeris(
    [.. specs.Select(s => new CelestialBody(
        s.Id, s.Id, s.Parent == "" ? null : s.Parent, s.Mu, s.BodyRadius, s.OrbitRadius, s.OrbitPeriod, s.Phase,
        s.Parent is "" or "sun" ? BodyKind.Planet : BodyKind.Moon))]);

var sim = new Simulator(field, timeStepSeconds: 60);

Vector2d BodyVel(string id, double t) => (field.Position(id, t + 1.0) - field.Position(id, t - 1.0)) / 2.0;
CelestialBody Body(string id) => field.Bodies.First(b => b.Id == id);

// ── The band ──────────────────────────────────────────────────────────────────────────────────────
//
// The shuttle's reach is ShuttleRange.RangeMeters (5e8 m = 500,000 km). Holding the ship AT that edge
// would be cruel — the away team's ride would be one drift away from unreachable — so the working hold
// band is a fraction of it. HoldBand is the dead-band radius the keeper defends; the owner's
// "approximate relative shuttle-range position" is exactly this: a band, not a point.
double shuttleReach = ShuttleRange.RangeMeters;
double holdBand = 0.5 * shuttleReach;      // 250,000 km — half the reach, a comfortable leash

// ── The regimes ───────────────────────────────────────────────────────────────────────────────────
//
// A derelict drifting in each of the places one could plausibly be found. `Parent` empty = deep space
// (heliocentric only); otherwise the derelict rides a circular orbit at `Radius` around that body.
(string Name, string Parent, double Radius)[] regimes =
[
    ("deep space (2.5 AU, no well)",        "",          0),
    ("high over Earth (60 Re)",             "earth",     60 * 6.371e6),
    ("low over Luna (3 Rl)",                "luna",      3 * 1.7374e6),
    ("low over Enceladus (3 Re)",           "enceladus", 3 * 2.5226e5),
    ("close over Jupiter (3 Rj)",           "jupiter",   3 * 6.9911e7),
];

// HOW the ship is asked to sit alongside the wreck. This distinction turned out to BE the lab's
// finding, so it is a first-class parameter rather than an implementation detail.
//
//   FixedOffset — park at a constant displacement from the wreck and match its velocity. The naive
//                 reading of "hold relative position", and the one a player would assume works.
//   CoOrbital   — where the wreck ORBITS something, share that orbit: identical radius and speed,
//                 displaced only ALONG the track. Same semi-major axis ⇒ same period ⇒ no phasing.
//
// In deep space the two are the same thing (there is no orbit to share), which is exactly why deep
// space is the easy case. (The enum itself lives at the foot of the file — top-level statements must
// precede type declarations.)

// The separation we ask for. A fixed offset near a small body is nonsense if the offset dwarfs the
// orbit itself — 25,000 km "alongside" a wreck circling Enceladus at 757 km is not alongside anything,
// it is a different trajectory entirely. So the ask is capped to a quarter of the orbital radius.
double OffsetFor(string parent, double radius) =>
    parent.Length == 0 ? 0.1 * holdBand : Math.Min(0.1 * holdBand, 0.25 * radius);

// Build the derelict's state in a regime, and the ship's state alongside it, perfectly matched
// (the honest hand-off: the mothership matched the derelict before dropping the shuttle).
(ShipState Wreck, ShipState Ship) Setup(string parent, double radius, double t0, double relVelError, Hold hold)
{
    double off = OffsetFor(parent, radius);

    if (parent.Length == 0)
    {
        // Deep space: a heliocentric drifter at 2.5 AU on a circular sun orbit. Nothing to orbit
        // locally, so both holds collapse to "match velocity and sit there".
        double r = 2.5 * 1.496e11;
        var p = new Vector2d(r, 0);
        var v = new Vector2d(0, Math.Sqrt(SunMu / r));
        return (new ShipState(p, v, t0),
                new ShipState(p + new Vector2d(0, off), v + new Vector2d(relVelError, 0), t0));
    }

    Vector2d bp = field.Position(parent, t0);
    Vector2d bv = BodyVel(parent, t0);
    double mu = Body(parent).Mu;
    double vCirc = Math.Sqrt(mu / radius);

    Vector2d wreckPos = bp + new Vector2d(radius, 0);
    Vector2d wreckVel = bv + new Vector2d(0, vCirc);
    var wreck = new ShipState(wreckPos, wreckVel, t0);

    if (hold == Hold.FixedOffset)
    {
        // Park off the wreck and match its velocity — at a DIFFERENT radius from the parent, which is
        // the whole problem: a different radius is a different orbit.
        return (wreck, new ShipState(
            wreckPos + new Vector2d(0, off), wreckVel + new Vector2d(relVelError, 0), t0));
    }

    // Co-orbital: same radius, displaced along the track by the arc `off`. Identical semi-major axis,
    // so in a clean two-body world they never phase apart at all.
    double theta = off / radius;
    var shipPos = bp + new Vector2d(radius * Math.Cos(theta), radius * Math.Sin(theta));
    var shipVel = bv + new Vector2d(-vCirc * Math.Sin(theta), vCirc * Math.Cos(theta));
    return (wreck, new ShipState(shipPos, shipVel + new Vector2d(relVelError, 0), t0));
}

double Gap(ShipState a, ShipState b) => (a.Position - b.Position).Length;

// A — free-drift endurance: no thrust at all, how long until the gap opens past the band.
double DriftEndurance(string parent, double radius, double relVelError, double capSeconds, Hold hold)
{
    (ShipState wreck, ShipState ship) = Setup(parent, radius, 0, relVelError, hold);
    for (double t = 0; t < capSeconds; t += 60)
    {
        wreck = sim.Step(wreck);
        ship = sim.Step(ship);
        if (Gap(ship, wreck) > holdBand)
        {
            return t;
        }
    }
    return double.PositiveInfinity;
}

// C — the dead-band keeper: fly for `days`, and whenever the gap leaves the band, trim. A trim nulls
// the relative velocity and aims back at the derelict — priced with the SAME OrbitRule kernel the
// autopilot's real burns use, so the lab's bill and the game's bill are one number.
(int Pulses, int Trims, double WorstDv) HoldCost(string parent, double radius, double days, Hold hold)
{
    (ShipState wreck, ShipState ship) = Setup(parent, radius, 0, 0, hold);
    int pulses = 0, trims = 0;
    double worstDv = 0;

    // The trim closes the standing gap GENTLY — over a return window, not in a panic. Closing a
    // quarter-million kilometres "in an hour" would be a 69 km/s burn, which is how the first cut of
    // this lab produced a nonsense bill of ~2,000 pulses/day against a 500-pulse tank. The keeper's
    // job is to stop the divergence, not to teleport.
    double returnWindow = 12 * Hour;

    for (double t = 0; t < days * Day; t += 60)
    {
        wreck = sim.Step(wreck);
        ship = sim.Step(ship);
        double gap = Gap(ship, wreck);

        if (gap > holdBand)
        {
            Vector2d toWreck = wreck.Position - ship.Position;
            Vector2d want = wreck.Velocity + (toWreck / returnWindow);
            double dv = (want - ship.Velocity).Length;
            worstDv = Math.Max(worstDv, dv);
            pulses += OrbitRule.PulsesFor(dv, ship.Velocity.Length);
            trims++;
            ship = ship with { Velocity = want };
        }
    }

    return (pulses, trims, worstDv);
}

string Dur(double s) => double.IsPositiveInfinity(s)
    ? "never"
    : s >= Day ? $"{s / Day:0.0} d" : s >= Hour ? $"{s / Hour:0.0} h" : $"{s / 60:0} min";

var ci = CultureInfo.InvariantCulture;
Console.WriteLine("LAB 40 — THE RIDE WAITS");
Console.WriteLine("Does the ship drift off while the away team is aboard the wreck, and what does holding cost?");
Console.WriteLine();
Console.WriteLine($"shuttle reach      : {shuttleReach / 1000:N0} km  (ShuttleRange.RangeMeters)");
Console.WriteLine($"hold band (leash)  : {holdBand / 1000:N0} km  (half the reach)");
Console.WriteLine($"start offset       : {0.1 * holdBand / 1000:N0} km");
Console.WriteLine();

Console.WriteLine("  regime                          | hold offset asked for");
Console.WriteLine("  --------------------------------|----------------------");
foreach ((string name, string parent, double radius) in regimes)
{
    Console.WriteLine($"  {name,-32}| {OffsetFor(parent, radius) / 1000:N0} km");
}
Console.WriteLine();

// ── A · free drift, both holds, perfectly matched ─────────────────────────────────────────────────
Console.WriteLine("A — FREE DRIFT, PERFECT HAND-OFF (no thrust; how long before the ride is gone)");
Console.WriteLine("  regime                          | fixed offset | co-orbital");
Console.WriteLine("  --------------------------------|--------------|-----------");
foreach ((string name, string parent, double radius) in regimes)
{
    double fixedE = DriftEndurance(parent, radius, 0, 30 * Day, Hold.FixedOffset);
    double coE = DriftEndurance(parent, radius, 0, 30 * Day, Hold.CoOrbital);
    Console.WriteLine($"  {name,-32}| {Dur(fixedE),12} | {Dur(coE)}");
}
Console.WriteLine();

// ── B · the hand-off error (against the GOOD hold) ────────────────────────────────────────────────
Console.WriteLine("B — THE HAND-OFF ERROR, co-orbital hold (residual relative velocity at shuttle launch)");
Console.WriteLine("  regime                          |    0 m/s |    1 m/s |   10 m/s |  100 m/s");
Console.WriteLine("  --------------------------------|----------|----------|----------|---------");
foreach ((string name, string parent, double radius) in regimes)
{
    string row = $"  {name,-32}|";
    foreach (double err in new[] { 0.0, 1.0, 10.0, 100.0 })
    {
        row += $" {Dur(DriftEndurance(parent, radius, err, 30 * Day, Hold.CoOrbital)),8} |";
    }
    Console.WriteLine(row.TrimEnd('|'));
}
Console.WriteLine();

// ── C · what holding costs, both holds ────────────────────────────────────────────────────────────
const double HoldDays = 5;
Console.WriteLine($"C — THE HOLD ({HoldDays} d dead-band keeper, perfect hand-off) — pulses/day is the quote");
Console.WriteLine("  regime                          | FIXED OFFSET            | CO-ORBITAL");
Console.WriteLine("                                  | trims  p/day  worst Δv  | trims  p/day  worst Δv");
Console.WriteLine("  --------------------------------|-------------------------|-----------------------");
foreach ((string name, string parent, double radius) in regimes)
{
    (int fp, int ft, double fdv) = HoldCost(parent, radius, HoldDays, Hold.FixedOffset);
    (int cp, int ct, double cdv) = HoldCost(parent, radius, HoldDays, Hold.CoOrbital);
    Console.WriteLine(
        $"  {name,-32}| {ft,5} {(fp / HoldDays).ToString("0.0", ci),6} {(fdv / 1000).ToString("0.00", ci),9} km/s |" +
        $" {ct,5} {(cp / HoldDays).ToString("0.0", ci),6} {(cdv / 1000).ToString("0.00", ci),9} km/s");
}
Console.WriteLine();

Console.WriteLine("Read it as: HOW you are asked to wait matters more than WHERE. Parking at a fixed");
Console.WriteLine("offset near a body is a different orbit, and different orbits leave. Share the wreck's");
Console.WriteLine("own orbit instead and the ride costs almost nothing, almost everywhere.");

/// <summary>How the ship is asked to sit alongside the wreck — see the note at the head of the file.
/// This distinction turned out to BE the lab's finding.</summary>
internal enum Hold
{
    /// <summary>Park at a constant displacement and match velocity — the naive reading of "hold
    /// relative position", and near a body a different orbit from the wreck's.</summary>
    FixedOffset,

    /// <summary>Share the wreck's own orbit: same radius and speed, displaced only along the track.</summary>
    CoOrbital,
}
