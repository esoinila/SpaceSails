namespace SpaceSails.Core;

/// <summary>
/// The dock envelope for a station haven (μ = 0). A mass-less station can't be orbited, so the ship
/// clamps onto it (⚓): coast alongside, match its drift, throw the arm. This is the <b>one place</b>
/// the envelope numbers live — the client's ⚓ Dock button and its coaching lines read them
/// (<c>DockReachMeters</c>/<c>DockMatchSpeedMps</c> in Map.razor are these constants), and so does the
/// autopilot's station rendezvous: the arm-time <see cref="AutopilotRehearsal"/> that prices the last
/// mile and the live loop that stands the ship into the berth. Centralised (#155) so the rehearsal, the
/// live stand-down and the UI can never quote different numbers — the owner's coaching line is
/// "coast within 500,000 km, ≤8 km/s".
/// </summary>
public static class DockRule
{
    /// <summary>The outer radius of the dock envelope (metres): how close the ship must coast to a
    /// station before the clamp can reach — 5e8 m = 500,000 km.</summary>
    public const double EnvelopeMeters = 5e8;

    /// <summary>The relative speed (m/s) the ship must be matched to within before it can clamp on —
    /// 8000 m/s = 8 km/s. Any faster and the clamp would shear on contact.</summary>
    public const double MatchSpeed = 8000;

    /// <summary>
    /// #244 · HOW CLOSE IS ARRIVED AT SOMETHING YOU CANNOT CLAMP ONTO — 1e8 m, 100,000 km.
    ///
    /// <para>Owner, arrived at the roadster at 499,721 km: <i>"I think we dropped out of autopilot… did we
    /// miss the dock button press while warping?"</i> The autopilot had SUCCEEDED, and for a wreck that
    /// success was wrong: <b>the envelope is not the destination</b>. A fetch pickup is proximity at a
    /// three-metre object, and stopping half a million kilometres out is a car-park in the next county.</para>
    ///
    /// <para>This is not a new number. It is the range the fetch pickup has always worked at
    /// (<c>Map.Quests.Contracts</c> read it as a private literal), hoisted here so the arrival and the errand
    /// are ONE distance: the autopilot now delivers the ship to the range at which the thing it was sent to
    /// do actually happens. A clamp reaches <see cref="EnvelopeMeters"/> because that is how long the arm is;
    /// nothing reaches out to a dead hull, so what decides the arrival is the work.</para>
    /// </summary>
    public const double AlongsideMeters = 1e8;

    /// <summary>
    /// True when <paramref name="ship"/> is inside the dock envelope of a station at
    /// <paramref name="stationPosition"/> drifting at <paramref name="stationVelocity"/>: clear of the
    /// station's own <paramref name="bodyRadius"/>, within <see cref="EnvelopeMeters"/>, and matched to
    /// within <see cref="MatchSpeed"/>. For a μ=0 body this — not <see cref="OrbitRule.Insert"/> — is
    /// "captured": there is no orbit to enter, only a berth to coast alongside and clamp onto.
    /// </summary>
    public static bool InEnvelope(ShipState ship, Vector2d stationPosition, Vector2d stationVelocity, double bodyRadius)
    {
        double distance = (ship.Position - stationPosition).Length;
        double relSpeed = (ship.Velocity - stationVelocity).Length;
        return distance > bodyRadius && distance <= EnvelopeMeters && relSpeed <= MatchSpeed;
    }

    /// <summary>#244 · How close the armed arrival must get to <paramref name="station"/> before it has
    /// arrived: the clamp's reach where there is a clamp, the errand's own range where there is not.</summary>
    public static double ArrivalRangeMeters(CelestialBody station) =>
        DockableHavens.IsDockable(station) ? EnvelopeMeters : AlongsideMeters;

    /// <summary>
    /// #244 · HAS THE SHIP ARRIVED at this μ=0 station? The same matched-and-alongside test
    /// <see cref="InEnvelope"/> makes, against the range that class of body actually earns.
    ///
    /// <para>The fork is <see cref="DockableHavens.IsDockable"/> — the very predicate the ⚓ button obeys —
    /// and not <c>BodyKind</c> or μ, which is the distinction #938 D3a was about: μ=0 is why a body cannot be
    /// orbited (physics), while whether a clamp exists is why the arrival stops where it stops. One question
    /// asked in one place, so the rehearsal that PRICES the last mile and the loop that FLIES it can never
    /// disagree about where the trip ends.</para>
    /// </summary>
    public static bool Arrived(
        ShipState ship, Vector2d stationPosition, Vector2d stationVelocity, CelestialBody station)
    {
        ArgumentNullException.ThrowIfNull(station);
        double distance = (ship.Position - stationPosition).Length;
        double relSpeed = (ship.Velocity - stationVelocity).Length;
        return distance > station.BodyRadius
            && distance <= ArrivalRangeMeters(station)
            && relSpeed <= MatchSpeed;
    }
}
