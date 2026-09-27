using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE MASS-DRIVER PODS — the public wave, the fixed Luna launcher and a scenario's launchers, and
/// the scheduled and mid-flight shapes a pod is built in.
///
/// <para>Split out of <c>TrafficSchedule.cs</c> under #251 as a pure move: two runs of the base file, with
/// the tutorial's two hand-placed prey (<c>TrafficSchedule.Tutorial.cs</c>) lifted out from between them.
/// Every <c>static readonly</c> table they read stays in the opening file, in its original order
/// (#1163).</para>
/// </summary>
public static partial class TrafficSchedule
{
    /// <summary>
    /// Mass-driver launches: ballistic compute-core pods (worldbuilding notes §1). The driver
    /// imparts all Δv at launch, so the "burn" is folded into <c>InitialState</c> and the plan is
    /// empty — no engine, no future maneuvers, <c>ManeuverBudget = 0</c>: a pod's prediction cone
    /// never opens. The tutorial prey and the pirate's milk run. Reads a scenario's pod launchers
    /// when supplied (moon and station launch sites both — worldbuilding §3), else falls back to
    /// the original Luna-only table, byte-identical to pre-PR-3 behavior.
    /// </summary>
    public static IReadOnlyList<NpcShip> GeneratePods(ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition? traffic = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        TrafficDefinition? effective = traffic ?? ephemeris.Traffic;
        return effective is { PodLaunchers.Count: > 0 }
            ? GeneratePodsFromScenario(ephemeris, seed, count, effective)
            : GeneratePodsFromFixedLauncher(ephemeris, seed, count);
    }

    private static IReadOnlyList<NpcShip> GeneratePodsFromFixedLauncher(
        ICelestialEphemeris ephemeris, ulong seed, int count, double baseSimTime = 0, int wave = 0)
    {
        var rng = new DeterministicRandom(seed);
        var launchSim = new Simulator(ephemeris, CatchUpTimeStep);
        var pods = new List<NpcShip>(count);
        int midFlight = count / 2;

        for (int i = 0; i < count; i++)
        {
            string destination = rng.NextInt(0, 2) == 0 ? "mars" : "venus";
            string id = NpcId("pod", wave, i);
            string callsign = PodCallsigns[i % PodCallsigns.Length] + WaveTag(wave);
            pods.Add(i < midFlight
                ? MidFlightPod(ephemeris, launchSim, rng, "earth", destination, "luna", "Compute cores", baseSimTime, id, callsign)
                : ScheduledPod(ephemeris, launchSim, rng, "earth", destination, "luna", "Compute cores", baseSimTime, id, callsign));
        }

        return pods;
    }

    /// <summary>A pod scheduled to fire in the days after <paramref name="baseSimTime"/>: its
    /// <c>InitialState</c> is declared just past the mass driver's single launch burn (everything
    /// the driver gave it, nothing it can change — plan stays empty).</summary>
    private static NpcShip ScheduledPod(
        ICelestialEphemeris ephemeris, Simulator launchSim, DeterministicRandom rng,
        string planningOrigin, string destination, string originId, string cargo,
        double baseSimTime, string id, string callsign)
    {
        double departure = baseSimTime + Math.Floor(rng.NextDouble(0.5 * Day, 10 * Day));

        // Plan the route like a ship (one burst + coast; the arrival brake is dropped — the
        // customer catches the pod), then run just past the burst and declare that state the launch.
        NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planningOrigin, destination, departure, RoutePersonality.Economical, rng);
        ManeuverNode burn = route.Plan.Nodes[0];
        var launchPlan = new ManeuverPlan([burn]);
        ShipState launched = launchSim.Run(route.DepartureState, (burn.SimTime - departure) + CatchUpTimeStep, launchPlan);

        return new NpcShip(
            id, callsign, cargo, originId, destination,
            RoutePersonality.Economical, departure, launched.SimTime, launched,
            // 5 units × 400 cr = exactly the first upgrade (2000 cr): one clean milk run finishes
            // the tutorial. 4 units would dead-end it 400 credits short.
            ManeuverPlan.Empty, route.EstimatedArrivalTime,
            CargoUnits: 5, ManeuverBudget: 0, IsPod: true);
    }

    /// <summary>
    /// A pod that already fired 0.5–6 days before <paramref name="baseSimTime"/> and is still
    /// coasting out through the inner system as of NOW (owner, 2026-07-06 empty-sky fix): a lit,
    /// catchable contact the instant the board opens, instead of a sky that waits days for the
    /// next mass-driver firing. Ballistic after the single launch burn, exactly like a scheduled
    /// pod — only its clock is wound back.
    /// </summary>
    private static NpcShip MidFlightPod(
        ICelestialEphemeris ephemeris, Simulator launchSim, DeterministicRandom rng,
        string planningOrigin, string destination, string originId, string cargo,
        double baseSimTime, string id, string callsign)
    {
        double timeSinceLaunch = rng.NextDouble(0.5 * Day, 6 * Day);
        double virtualDeparture = baseSimTime - timeSinceLaunch;

        NpcRoute route = RoutePlanner.PlanRoute(ephemeris, planningOrigin, destination, virtualDeparture, RoutePersonality.Economical, rng);
        ManeuverNode burn = route.Plan.Nodes[0];
        var launchPlan = new ManeuverPlan([burn]);
        ShipState now = launchSim.Run(route.DepartureState, baseSimTime - virtualDeparture, launchPlan);

        return new NpcShip(
            id, callsign, cargo, originId, destination,
            RoutePersonality.Economical, virtualDeparture, now.SimTime, now,
            ManeuverPlan.Empty, route.EstimatedArrivalTime,
            CargoUnits: 5, ManeuverBudget: 0, IsPod: true);
    }

    private static IReadOnlyList<NpcShip> GeneratePodsFromScenario(
        ICelestialEphemeris ephemeris, ulong seed, int count, TrafficDefinition traffic,
        double baseSimTime = 0, int wave = 0)
    {
        var rng = new DeterministicRandom(seed);
        var launchSim = new Simulator(ephemeris, CatchUpTimeStep);
        var pods = new List<NpcShip>(count);
        IReadOnlyList<PodLauncherDefinition> launchers = traffic.PodLaunchers;

        // At least one pod is already coasting as of baseSimTime (owner, 2026-07-06 empty-sky fix):
        // the tutorial's named "Luna pod" prey must exist the instant the board opens, not fire
        // days later. The rest are scheduled firings so the milk run keeps replenishing.
        int midFlight = count / 2;
        PodLauncherDefinition? lunaLauncher = launchers.FirstOrDefault(l => l.Body == "luna");

        for (int i = 0; i < count; i++)
        {
            bool isMidFlight = i < midFlight;
            // Draw the launcher every iteration so the rng stream (and determinism) is identical
            // whether or not the Luna override below fires.
            PodLauncherDefinition picked = launchers[rng.NextInt(0, launchers.Count)];
            PodLauncherDefinition launcher = isMidFlight && lunaLauncher is not null ? lunaLauncher : picked;
            string planningOrigin = PlanningBodyId(ephemeris, launcher.Body);
            string destination = PickPodDestination(ephemeris, planningOrigin, rng);
            string id = NpcId("pod", wave, i);
            string callsign = PodCallsigns[i % PodCallsigns.Length] + WaveTag(wave);

            pods.Add(isMidFlight
                ? MidFlightPod(ephemeris, launchSim, rng, planningOrigin, destination, launcher.Body, launcher.Cargo, baseSimTime, id, callsign)
                : ScheduledPod(ephemeris, launchSim, rng, planningOrigin, destination, launcher.Body, launcher.Cargo, baseSimTime, id, callsign));
        }

        return pods;
    }

    private static string PickPodDestination(ICelestialEphemeris ephemeris, string planningOrigin, DeterministicRandom rng)
    {
        List<string> candidates = FixedPodDestinations.Concat(["earth"])
            .Where(id => id != planningOrigin && ephemeris.Bodies.Any(b => b.Id == id))
            .Distinct()
            .ToList();
        if (candidates.Count == 0)
        {
            candidates = [.. FixedPodDestinations];
        }

        return candidates[rng.NextInt(0, candidates.Count)];
    }
}
