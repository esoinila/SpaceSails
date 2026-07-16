using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// Luna's mass drivers, lobbing compute-core pods (worldbuilding notes §1; Lab 30 "The mass-driver
/// timetable"). A driver imparts its whole Δv at the instant of launch — the pod leaves the surface
/// with a fixed speed in a fixed direction and NEVER thrusts again (<c>ManeuverBudget = 0</c>, empty
/// plan). From that launch state it rides a pure two-body conic, so <see cref="TransferMath.PropagateKepler"/>
/// names where any pod is at any time: a Kepler rail, not a stepped integration. This is the bridge
/// from the physics bench (the lab) into the world — a modest cadence off a site body becomes tiny
/// moving objects on the map, the milk run the pirate lane will later point contracts at.
///
/// <para>Everything here is a pure, deterministic function of the ephemeris and the run parameters:
/// the same site, speed, azimuth and cadence give a byte-identical timetable, exactly as the rails
/// themselves do. No randomness — a mass driver fires on a clock, not a coin.</para>
/// </summary>
public static class MassDriverSchedule
{
    private const double Day = 86400.0;

    /// <summary>
    /// One repeating mass-driver program: a launch <see cref="SiteBodyId"/> that fires a
    /// <see cref="Cargo"/> pod every <see cref="CadenceSeconds"/> with the driver-imparted
    /// <see cref="LaunchSpeed"/> (m/s, relative to the site body) aimed at <see cref="AzimuthRad"/>
    /// off the site's heliocentric prograde direction (0 = along its motion → outward transfers;
    /// π = against it → inner-system lobs toward Venus and the Mercury compute yards). Each pod is
    /// declared arrived — and swept off the rails — <see cref="LifespanSeconds"/> after its launch.
    /// </summary>
    public sealed record MassDriverRun(
        string SiteBodyId,
        string Cargo,
        string DestinationId,
        double LaunchSpeed,
        double AzimuthRad,
        double CadenceSeconds,
        double LifespanSeconds)
    {
        /// <summary>A gentle inner-system Luna lob: fires twice a day, each pod good for 200 days on
        /// the rails — the visible "milk run" cadence the map shows and Lab 30 measures.</summary>
        public static MassDriverRun LunaMilkRun(string cargo = "Compute cores", string destinationId = "venus") =>
            new("luna", cargo, destinationId, LaunchSpeed: 3200.0, AzimuthRad: Math.PI,
                CadenceSeconds: 0.5 * Day, LifespanSeconds: 200 * Day);
    }

    /// <summary>One scheduled firing: when it leaves the driver, when it is swept off the rails, and
    /// the heliocentric launch state (the seed of its conic). <see cref="Launch"/> is truth at
    /// <see cref="LaunchTime"/>; the pod's state at any later instant is
    /// <see cref="MassDriverSchedule.PodRailState"/> of it.</summary>
    public sealed record LaunchEntry(int Index, double LaunchTime, double ExpiryTime, ShipState Launch)
    {
        /// <summary>Is a pod fired on this entry on the rails at <paramref name="simTime"/> — i.e.
        /// launched already and not yet expired (arrived/decayed)?</summary>
        public bool IsLive(double simTime) => simTime >= LaunchTime && simTime < ExpiryTime;
    }

    /// <summary>
    /// Surface escape speed of a body, √(2μ/R): the floor the driver speed must clear or the pod
    /// falls back (or merely enters lunar orbit) instead of leaving for a heliocentric conic. A
    /// Luna number the probe prints and the launch family is measured against.
    /// </summary>
    public static double SurfaceEscapeSpeed(CelestialBody body)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(body.Mu);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(body.BodyRadius);
        return Math.Sqrt(2.0 * body.Mu / body.BodyRadius);
    }

    /// <summary>
    /// The heliocentric state of a pod the instant the driver releases it: the site body's own
    /// position and velocity, plus the driver's kick. The pod starts one body-radius off the surface
    /// along its aim, moving at the site's velocity plus <paramref name="launchSpeed"/> in that
    /// aim direction. <paramref name="azimuthRad"/> is measured off the site's heliocentric prograde
    /// (its velocity vector), CCW positive. No propulsion is modelled after this — the returned state
    /// is the whole flight plan.
    /// </summary>
    public static ShipState LaunchState(
        ICelestialEphemeris ephemeris, string siteBodyId, double launchSpeed, double azimuthRad, double launchTime)
    {
        CelestialBody site = ephemeris.Bodies.First(b => b.Id == siteBodyId);
        Vector2d center = ephemeris.Position(siteBodyId, launchTime);
        Vector2d siteVel = TransferMath.BodyVelocity(ephemeris, siteBodyId, launchTime);

        // Aim direction = the site's prograde rotated by the launch azimuth. If the site is somehow
        // at rest (a parentless origin body), fall back to +X so the direction is still well-defined.
        Vector2d prograde = siteVel.Length > 0 ? siteVel.Normalized() : new Vector2d(1, 0);
        Vector2d aim = Rotate(prograde, azimuthRad);

        Vector2d position = center + aim * site.BodyRadius;
        Vector2d velocity = siteVel + aim * launchSpeed;
        return new ShipState(position, velocity, launchTime);
    }

    /// <summary>
    /// Where a pod is at <paramref name="simTime"/>, on its rail: the analytic two-body conic from
    /// its <paramref name="launch"/> state about an attractor of parameter <paramref name="sunMu"/>.
    /// A pure function of time (no integration), so a timetable can name a pod's neighbourhood pass
    /// without stepping anything. This is the heliocentric conic — honest once the pod is clear of
    /// the launch body's well; near the site the site's gravity still bends it, the small "two-body
    /// lie" the probe measures against the full-field integrator.
    /// </summary>
    public static ShipState? PodRailState(ShipState launch, double simTime, double sunMu)
    {
        if (TransferMath.PropagateKepler(launch.Position, launch.Velocity, simTime - launch.SimTime, sunMu) is not { } k)
        {
            return null;
        }

        return new ShipState(k.Position, k.Velocity, simTime);
    }

    /// <summary>
    /// The timetable: <paramref name="count"/> consecutive firings of <paramref name="run"/> on its
    /// cadence, centred on <paramref name="baseSimTime"/> so that about half are already in flight
    /// (visible NOW) and the rest are still to fire — the sky is never empty and the driver never
    /// idle. Entry <c>i</c> fires at <c>baseSimTime + (i − count/2)·cadence</c>. Pure and
    /// deterministic: the same arguments give the same launch times and states every call.
    /// </summary>
    public static IReadOnlyList<LaunchEntry> Timetable(
        ICelestialEphemeris ephemeris, MassDriverRun run, double baseSimTime, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(run.CadenceSeconds);

        int inFlight = count / 2;
        var entries = new List<LaunchEntry>(count);
        for (int i = 0; i < count; i++)
        {
            double launchTime = baseSimTime + (i - inFlight) * run.CadenceSeconds;
            ShipState launch = LaunchState(ephemeris, run.SiteBodyId, run.LaunchSpeed, run.AzimuthRad, launchTime);
            entries.Add(new LaunchEntry(i, launchTime, launchTime + run.LifespanSeconds, launch));
        }

        return entries;
    }

    /// <summary>
    /// A modest cadence of visible pods for the live world: the <see cref="Timetable"/> turned into
    /// ballistic NPC pods (<c>IsPod</c>, zero maneuver budget, empty plan) that render for free with
    /// every other contact. A pod already in flight is coasted forward through the full field to
    /// <paramref name="baseSimTime"/> so its declared "now" state is continuous with the live sim
    /// (exactly how the other mid-flight traffic is seeded); a pod still to fire carries its launch
    /// state and activates when the driver releases it. Its <c>EstimatedArrivalTime</c> is the
    /// entry's expiry, so the world's existing arrived-pod sweep gives every pod its lifespan.
    /// </summary>
    public static IReadOnlyList<NpcShip> GenerateCadence(
        ICelestialEphemeris ephemeris, MassDriverRun run, double baseSimTime, int count, int wave = 0)
    {
        var sim = new Simulator(ephemeris, TrafficSchedule.NpcTimeStep);
        IReadOnlyList<LaunchEntry> timetable = Timetable(ephemeris, run, baseSimTime, count);
        var pods = new List<NpcShip>(count);

        foreach (LaunchEntry entry in timetable)
        {
            bool inFlight = entry.LaunchTime < baseSimTime;
            ShipState state = inFlight
                ? sim.RunAdaptive(entry.Launch, baseSimTime - entry.LaunchTime)
                : entry.Launch;
            double activation = inFlight ? baseSimTime : entry.LaunchTime;
            string tag = wave == 0 ? "" : $" ·{wave}";
            string id = wave == 0 ? $"mdriver-{entry.Index}" : $"mdriver-w{wave}-{entry.Index}";

            pods.Add(new NpcShip(
                id,
                Callsigns[entry.Index % Callsigns.Length] + tag,
                run.Cargo,
                run.SiteBodyId,
                run.DestinationId,
                RoutePersonality.Economical,
                DepartureTime: entry.LaunchTime,
                ActivationTime: activation,
                InitialState: state,
                Plan: ManeuverPlan.Empty,
                EstimatedArrivalTime: entry.ExpiryTime,
                CargoUnits: 5,
                ManeuverBudget: 0,
                IsPod: true));
        }

        return pods;
    }

    // Pods off the driver get prey-nicknames, kept distinct from the hauler and the random-pod names
    // so a callsign alone says "this came off Luna's driver".
    private static readonly string[] Callsigns =
        ["Milk Run", "Windfall", "Ripe Plum", "Fat Goose", "Easy Keeping", "Tin Kettle", "Slow Coach", "Ferryman's Due"];

    // Rotate a vector by angle (radians), CCW positive.
    private static Vector2d Rotate(Vector2d v, double angle)
    {
        double c = Math.Cos(angle), s = Math.Sin(angle);
        return new Vector2d(c * v.X - s * v.Y, s * v.X + c * v.Y);
    }
}
