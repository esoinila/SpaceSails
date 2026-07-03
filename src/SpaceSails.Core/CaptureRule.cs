namespace SpaceSails.Core;

/// <summary>
/// The piracy capture rule (plan §M6, revised by the owner's boarding-shuttle design): the
/// mothership doesn't dock — it opens a *window of opportunity* for small boarding craft.
/// Inside <see cref="CaptureRadiusMeters"/> at under <see cref="MaxRelativeSpeed"/> the
/// shuttles can fly, but their transit gets harder the sloppier the pass:
/// <see cref="RequiredSecondsFor"/> scales boarding time with distance and relative speed.
/// A tight rendezvous boards in ~<see cref="BaseBoardingSeconds"/>; a fast drive-by needs a
/// long window that its own geometry rarely grants. Core owns the pure math; callers
/// accumulate progress (client now, server post-M9).
/// </summary>
public static class CaptureRule
{
    /// <summary>Shuttle operating range. Wider than a boarding tube — the mothership can
    /// stand off ~a lunar distance and still fly craft across.</summary>
    public const double CaptureRadiusMeters = 5e8;

    /// <summary>Max relative speed at which shuttles can chase the target down. One ±10%
    /// pulse at interplanetary speed is a ~3 km/s quantum; shuttles forgive a bit more than
    /// one quantum of mismatch — but they pay for it in transit time.</summary>
    public const double MaxRelativeSpeed = 5000;

    /// <summary>Boarding time for a perfect match at point-blank range.</summary>
    public const double BaseBoardingSeconds = 30;

    /// <summary>Relative speed that doubles shuttle transit time.</summary>
    public const double RelativeSpeedPenalty = 1500;

    /// <summary>Stand-off distance that doubles shuttle transit time.</summary>
    public const double DistancePenalty = 2e8;

    /// <summary>Kept for HUD scale/legacy callers: the nominal window length.</summary>
    public const double RequiredSeconds = 60;

    public static bool IsInWindow(ShipState player, ShipState target) =>
        (player.Position - target.Position).LengthSquared <= CaptureRadiusMeters * CaptureRadiusMeters
        && (player.Velocity - target.Velocity).LengthSquared <= MaxRelativeSpeed * MaxRelativeSpeed;

    /// <summary>
    /// Sim seconds of continuous window the shuttles need at this instant's geometry.
    /// Tight+slow ≈ 30 s; at the envelope's sloppy corner (5 km/s, 5e8 m) ≈ 455 s — more
    /// window than a straight-line drive-by through the envelope can provide, so genuine
    /// flybys still fail; a rough-but-honest pass succeeds where docking never would.
    /// </summary>
    public static double RequiredSecondsFor(ShipState player, ShipState target)
    {
        double distance = (player.Position - target.Position).Length;
        double relativeSpeed = (player.Velocity - target.Velocity).Length;
        return BaseBoardingSeconds
            * (1 + relativeSpeed / RelativeSpeedPenalty)
            * (1 + distance / DistancePenalty);
    }
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
