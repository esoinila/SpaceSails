namespace SpaceSails.Core;

/// <summary>
/// The shuttle-bay ferry (#163, "the door you understand as a flight"). The small craft that crosses
/// the gap to board a prey (the boarding run, <c>ShuttleFlightView</c>) doubles as the captain's
/// short-hop taxi to a nearby berth: walk to the shuttle-bay airlock, pick a place in shuttle range,
/// and walking through the door IS the trip — no separate flight minigame, the sim clock simply
/// advances by the crossing time.
///
/// <para>Both numbers are honest derivations of constants already in Core, so there is no parallel
/// model to drift:</para>
/// <list type="bullet">
/// <item><b>Reach</b> is <see cref="CaptureRule.CaptureRadiusMeters"/> (5e8 m = 500,000 km) — the same
/// craft demonstrably crosses that gap to board a ship, so ferrying the captain the same distance is
/// its proven range. It reaches a neighbour in the same planetary neighbourhood (a moon from its
/// planet's station, a co-orbiting berth) but never the next planet, which sits ~1e11 m off, far past
/// this.</item>
/// <item><b>Cruise speed</b> is <see cref="DockRule.MatchSpeed"/> (8 km/s) — the shuttle coasts the gap
/// at the same speed the mothership eases into a clamp. The trip cost is a documented flat-physics
/// straight-line crossing time for this first version.</item>
/// </list>
/// </summary>
public static class ShuttleRange
{
    /// <summary>The shuttle's one-hop reach (metres): a body farther than this is out of shuttle range.
    /// The boarding shuttle's own demonstrated crossing (<see cref="CaptureRule.CaptureRadiusMeters"/>).</summary>
    public const double RangeMeters = CaptureRule.CaptureRadiusMeters; // 5e8 m = 500,000 km

    /// <summary>The shuttle's cruise speed (m/s) for the door-is-a-flight time cost — anchored to the
    /// clamp match speed (<see cref="DockRule.MatchSpeed"/>, 8 km/s).</summary>
    public const double CruiseSpeedMps = DockRule.MatchSpeed; // 8000 m/s

    /// <summary>True when a destination <paramref name="distanceMeters"/> away is within one shuttle hop.
    /// Pure distance classification (the caller excludes the body it is already berthed at); a negative
    /// distance is never in range.</summary>
    public static bool InRange(double distanceMeters) =>
        distanceMeters >= 0 && distanceMeters <= RangeMeters;

    /// <summary>The wall-clock seconds the hop costs: the straight-line gap flown at
    /// <see cref="CruiseSpeedMps"/>. The sim clock advances by this — heat bleeds, traffic drifts — so a
    /// farther berth is a longer ride. A non-positive gap costs no time.</summary>
    public static double TravelSeconds(double distanceMeters) =>
        Math.Max(0.0, distanceMeters) / CruiseSpeedMps;
}
