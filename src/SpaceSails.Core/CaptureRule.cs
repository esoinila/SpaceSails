namespace SpaceSails.Core;

/// <summary>
/// The piracy capture rule (plan §M6): to board a target you must hold station — inside
/// <see cref="CaptureRadiusMeters"/> at under <see cref="MaxRelativeSpeed"/> — for
/// <see cref="RequiredSeconds"/> of continuous sim time. Core owns the constants and the pure
/// per-instant predicate; callers accumulate the window (client now, server from M9).
/// </summary>
public static class CaptureRule
{
    /// <summary>Boarding range. 1e8 m ≈ a quarter of the Earth–Luna distance — generous on
    /// purpose: closing tighter with ±10% pulses is the skill, not pixel-hunting.</summary>
    public const double CaptureRadiusMeters = 1e8;

    /// <summary>Max closing speed to hold a boarding tube. One pulse at interplanetary speed
    /// is ~3 km/s, so 2 km/s demands a genuine rendezvous orbit, not a flyby.</summary>
    public const double MaxRelativeSpeed = 2000;

    /// <summary>Continuous sim seconds the window must hold.</summary>
    public const double RequiredSeconds = 60;

    public static bool IsInWindow(ShipState player, ShipState target) =>
        (player.Position - target.Position).LengthSquared <= CaptureRadiusMeters * CaptureRadiusMeters
        && (player.Velocity - target.Velocity).LengthSquared <= MaxRelativeSpeed * MaxRelativeSpeed;
}

/// <summary>What a fence pays per unit, by cargo class. He3 is the prize; pods are milk runs.</summary>
public static class CargoMarket
{
    public static int UnitValue(string cargoClass) => cargoClass switch
    {
        "He3" => 1200,
        "Compute cores" => 400,
        "Alloys" => 300,
        "Machinery" => 250,
        "Ice" => 100,
        _ => 50,
    };
}
