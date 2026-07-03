namespace SpaceSails.Contracts;

/// <summary>Result of joining the shared session.</summary>
public sealed record JoinResultDto(
    string PlayerId,
    string Scenario,
    double SimTime,
    double PositionX,
    double PositionY,
    double VelocityX,
    double VelocityY,
    int ReactionMass);

/// <summary>A maneuver node as sent over the wire (client plan → server execution).</summary>
public sealed record PlanNodeDto(double SimTime, bool Accelerate, int Pulses, bool Fine = false);

/// <summary>
/// One ship the receiving player can currently see. Contacts outside sensor range are not
/// degraded or flagged — they are omitted entirely: hidden information never leaves the server.
/// </summary>
public sealed record ContactDto(
    string Id,
    string Callsign,
    string Kind, // "player" | "npc" | "pod"
    double PositionX,
    double PositionY,
    double VelocityX,
    double VelocityY,
    double Charge,
    string? CargoClass);

/// <summary>Per-player world snapshot, tailored by the server's observation filtering.</summary>
public sealed record WorldUpdateDto(
    double SimTime,
    int EffectiveWarp,
    int PlayerCount,
    double PositionX,
    double PositionY,
    double VelocityX,
    double VelocityY,
    double Charge,
    int ReactionMass,
    IReadOnlyList<ContactDto> Contacts);
