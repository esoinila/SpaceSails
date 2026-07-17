namespace SpaceSails.Core;

/// <summary>
/// Builds the co-moving berth state: the ship pinned onto a body's rail — a small radial offset out
/// from the body (in the Sun's frame), riding at the body's own orbital velocity. This is the ONE
/// construction behind every "the ship is at this berth" moment: a docked start, a shuttle arrival, a
/// vault-resume boot, and — since #269 — completing a manual clamp. Centralised so those four paths
/// can never build a berth four subtly different ways.
///
/// #269: before this, the ⚓ Dock button only set the docked flag and froze the arm at wherever the
/// ship happened to float — and the 500,000 km approach envelope meant "float" could be 100,000 km out
/// on a conic diving at the planet (the owner's Tilt arrival: "docked and about to slam into the
/// planet"). Completing a clamp now snaps the ship HERE: one body, one rail.
/// </summary>
public static class BerthState
{
    /// <summary>The radial gap (metres) the berth sits off the body — just clear of the ~1 km station,
    /// well inside <see cref="DockRule.EnvelopeMeters"/>. Was a bare <c>3_000</c> repeated at every
    /// berth-building call site; named here so they all quote the same reach.</summary>
    public const double BerthOffsetMeters = 3_000;

    /// <summary>
    /// A ship state co-moving with <paramref name="bodyId"/> at <paramref name="simTime"/>:
    /// <paramref name="offsetMeters"/> radially outward from the body (from the Sun's frame), velocity
    /// equal to the body's orbital velocity by central difference. Bodies are deterministic from time,
    /// so this reconstructs the exact berth at any epoch — no stored orbit need cross a save boundary.
    /// </summary>
    public static ShipState CoMoving(
        ICelestialEphemeris ephemeris, string bodyId, double simTime, double offsetMeters, double charge = 0)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);

        const double h = 1.0;
        Vector2d pos = ephemeris.Position(bodyId, simTime);
        Vector2d vel = (ephemeris.Position(bodyId, simTime + h) - ephemeris.Position(bodyId, simTime - h)) / (2 * h);
        Vector2d outward = pos == Vector2d.Zero ? new Vector2d(1, 0) : pos.Normalized();
        return new ShipState(pos + outward * offsetMeters, vel, simTime, charge);
    }
}
