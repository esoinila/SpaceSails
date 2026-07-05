namespace SpaceSails.Core;

/// <summary>A timestamped sensor contact: what the observer knew about the target, and when.</summary>
public readonly record struct Observation(string TargetId, double SimTime, Vector2d Position, Vector2d Velocity);

/// <summary>
/// What a sensor can see. Pure and deterministic: the same geometry always yields the same
/// answer, so client and server agree about who saw what (the server enforces it from M9).
/// </summary>
public interface IObservationModel
{
    bool TryObserve(Vector2d observerPosition, string targetId, ShipState target, double simTime, out Observation observation);
}

/// <summary>
/// Range-limited sensor with a sun-glare penalty: a target that sits within the glare cone
/// (looking from the observer toward the Sun at the system origin) is only detectable at
/// <see cref="GlareRangeFactor"/> × range. Low solar orbits are where ambushes live (plan §M7).
/// </summary>
public sealed class SensorModel(double rangeMeters, double glareHalfAngleRad, double glareRangeFactor) : IObservationModel
{
    /// <summary>Defaults tuned for the Sol scenario: 1e11 m ≈ 0.67 AU, 20° glare cone at ×0.25 range.</summary>
    public static SensorModel Default { get; } = new(1.0e11, 20.0 * Math.PI / 180.0, 0.25);

    /// <summary>A fully charged hull glows: it is seen (1 + this) × farther. M7.</summary>
    public const double ChargeGlowFactor = 2.0;

    public double RangeMeters { get; } = rangeMeters;
    public double GlareHalfAngleRad { get; } = glareHalfAngleRad;
    public double GlareRangeFactor { get; } = glareRangeFactor;

    public bool TryObserve(Vector2d observerPosition, string targetId, ShipState target, double simTime, out Observation observation)
    {
        double distance = (target.Position - observerPosition).Length;
        if (distance > EffectiveRange(observerPosition, target.Position, target.Charge))
        {
            observation = default;
            return false;
        }

        observation = new Observation(targetId, simTime, target.Position, target.Velocity);
        return true;
    }

    /// <summary>
    /// How far this sensor sees toward a specific target: sun glare dims passive detection,
    /// while a charged hull *radiates* and that glow pierces glare untouched — the sun-side
    /// ambush tradeoff (plan §M7). Exposed for the M27 mutual-visibility readout.
    /// </summary>
    public double EffectiveRange(Vector2d observerPosition, Vector2d targetPosition, double targetCharge)
    {
        Vector2d toTarget = targetPosition - observerPosition;
        double distance = toTarget.Length;
        double baseRange = RangeMeters;
        Vector2d toSun = Vector2d.Zero - observerPosition;
        double sunDistance = toSun.Length;
        if (distance > 0 && sunDistance > 0)
        {
            double cosAngle = toTarget.Dot(toSun) / (distance * sunDistance);
            if (cosAngle > Math.Cos(GlareHalfAngleRad))
            {
                baseRange *= GlareRangeFactor;
            }
        }

        return baseRange + RangeMeters * ChargeGlowFactor * targetCharge;
    }

    /// <summary>
    /// M27: who spots whom first, assuming the other side carries the same sensor fit. The
    /// telescope's double duty — watching targets AND minding our own signature toward them
    /// (our hull charge glows in THEIR range term; their glare cone is computed from THEIR sky).
    /// </summary>
    public SightAdvantage Advantage(ShipState us, ShipState them)
    {
        double distance = (them.Position - us.Position).Length;
        double ourRange = EffectiveRange(us.Position, them.Position, them.Charge);
        double theirRange = EffectiveRange(them.Position, us.Position, us.Charge);
        return new SightAdvantage(distance, ourRange, theirRange,
            distance <= ourRange, distance <= theirRange);
    }
}

/// <summary>M27: the two detection bubbles at a glance — ours on them, theirs on us.</summary>
public readonly record struct SightAdvantage(
    double Distance, double OurRange, double TheirRange, bool WeSeeThem, bool TheySeeUs)
{
    /// <summary>Positive: we'd spot them before they spot us; negative: they win the eyes race.</summary>
    public double Edge => OurRange - TheirRange;
}

/// <summary>
/// M27: the close-range active radar — exact returns inside its range regardless of sun glare
/// or a dark hull. The price is being LOUD: every ship in earshot of the ping learns exactly
/// where we are (it wakes their "aware" flag). For tight spots where stealth is already spent.
/// </summary>
public static class RadarRule
{
    /// <summary>Precise returns inside this range.</summary>
    public const double RangeMeters = 5e9;

    /// <summary>Ships this far out hear the ping and become aware of us.</summary>
    public const double LoudRangeMeters = 5e10;

    public static bool InRange(Vector2d us, Vector2d them) =>
        (them - us).LengthSquared <= RangeMeters * RangeMeters;

    public static bool HearsPing(Vector2d us, Vector2d them) =>
        (them - us).LengthSquared <= LoudRangeMeters * LoudRangeMeters;
}

/// <summary>
/// M29: the ship's transponder — the AIS of the solar lanes (owner: "like ships on ocean, we
/// can turn off our transponder just before we commit to the pirate run"). ON broadcasts our
/// true state on the beacon band: honest traffic runs lit, and anyone in beacon range has us
/// without winning any eyes race. DARK broadcasts nothing — detection falls back to the
/// optical race. FAKE broadcasts a GHOST: the ship we'd be if we'd stayed on the declared
/// course — the state snapshotted at the moment of the lie, flown ballistically ever since —
/// while the real hull runs the intercept.
/// </summary>
public enum TransponderMode
{
    On,
    Dark,
    Fake,
}

/// <summary>What one particular observer's picture of us is, beacon and optics combined.</summary>
public enum BeaconPicture
{
    /// <summary>They have nothing — no beacon signal, no optical contact.</summary>
    Nothing,

    /// <summary>They have our TRUE position (lit beacon in range, or their own eyes).</summary>
    TrueContact,

    /// <summary>They read the fake beacon and believe the ghost — we look on course.</summary>
    Ghost,

    /// <summary>Their own optics see the real us WHILE the beacon claims the ghost — the lie
    /// is blown to this observer: a hull provably off its declared course.</summary>
    LieBlown,
}

public static class TransponderRule
{
    /// <summary>Beacon band reach — the same "earshot" as a loud radar ping.</summary>
    public const double BeaconRangeMeters = RadarRule.LoudRangeMeters;

    /// <summary>Whatever the optics say, a lit (or lying) beacon inside range is heard.
    /// Dark ships are exactly as visible as their hull and no more.</summary>
    public static bool BeaconHeard(TransponderMode mode, double distance) =>
        mode != TransponderMode.Dark && distance <= BeaconRangeMeters;

    /// <summary>The eyes-race verdict corrected for our beacon: they have us if their optics
    /// win OR our beacon is talking to them (a fake beacon still hands them A contact — just
    /// not the true one; see <see cref="PictureFor"/> for what they believe).</summary>
    public static SightAdvantage WithBeacon(SightAdvantage optical, TransponderMode mode) =>
        BeaconHeard(mode, optical.Distance) && !optical.TheySeeUs
            ? optical with { TheySeeUs = true }
            : optical;

    /// <summary>
    /// One observer's picture of us: nothing, the truth, the ghost — or the blown lie, when
    /// their own eyes see the real hull while the beacon claims we're somewhere else. Pure
    /// function of the optical verdict and the mode; per observer, so a lie can hold across
    /// the system while being blown to the one hull close enough to look.
    /// </summary>
    public static BeaconPicture PictureFor(TransponderMode mode, SightAdvantage optical) => mode switch
    {
        TransponderMode.Dark => optical.TheySeeUs ? BeaconPicture.TrueContact : BeaconPicture.Nothing,
        TransponderMode.On => optical.TheySeeUs || optical.Distance <= BeaconRangeMeters
            ? BeaconPicture.TrueContact
            : BeaconPicture.Nothing,
        _ => optical.TheySeeUs ? BeaconPicture.LieBlown
            : optical.Distance <= BeaconRangeMeters ? BeaconPicture.Ghost
            : BeaconPicture.Nothing,
    };
}
