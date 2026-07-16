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
}
