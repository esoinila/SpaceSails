namespace SpaceSails.Core;

/// <summary>
/// The active-sensor consequence of a laser-ranging ping (vision ¶14/¶16): the pinged target now
/// knows roughly where the shot came from. Pure data — Core doesn't decide what "aware" means for
/// behavior; the UI applies it now (a warning flag), and PR-7 (weapons/heat) is expected to
/// consume it for real later.
/// </summary>
public readonly record struct PingEvent(string TargetId, Vector2d SourcePosition, double SimTime);

/// <summary>
/// Active instruments (vision ¶14/¶16), as opposed to the tracking post's passive telescope:
/// laser ranging trades a perfect fix for lighting yourself up, and tight-beam comms let you hail
/// one specific contact without broadcasting to the whole sky. Both are pure geometry/state
/// construction — no clock, no randomness, no mutation. Passive scanning stays the pirate's way;
/// these are the two ways to give yourself away on purpose.
/// </summary>
public static class ActiveSensors
{
    /// <summary>Tight-beam range: a directed point-to-point link, not a broadcast — far shorter
    /// than the telescope's passive reach.</summary>
    public const double TightBeamMaxRangeMeters = 5e10;

    /// <summary>
    /// An active laser ping against a known target's true state: exact position and velocity,
    /// zero uncertainty age as of <paramref name="simTime"/> — no range limit of its own, since a
    /// laser only gets fired at something already found (the UI restricts this to tracked
    /// targets). The returned <see cref="PingEvent"/> is the price: the caller (UI) must apply
    /// it — the target becomes "aware", and your position at the moment of the ping is knowable
    /// to it and anyone else watching.
    /// </summary>
    public static (Observation Observation, PingEvent Ping) LaserRange(
        string targetId, Vector2d playerPosition, Vector2d targetPosition, Vector2d targetVelocity, double simTime)
    {
        var observation = new Observation(targetId, simTime, targetPosition, targetVelocity);
        var ping = new PingEvent(targetId, playerPosition, simTime);
        return (observation, ping);
    }

    /// <summary>True if a tight-beam link can reach the target from here.</summary>
    public static bool CanTightBeam(
        Vector2d playerPosition, Vector2d targetPosition, double maxRangeMeters = TightBeamMaxRangeMeters) =>
        (targetPosition - playerPosition).Length <= maxRangeMeters;
}
