namespace SpaceSails.Core;

/// <summary>
/// One bought route tip: what ship, when it was bought, how long it stays good, and what it
/// cost. Purely a value the player's <see cref="IntelLedger"/> holds — buying it is what makes an
/// unpublished ship (worldbuilding notes §4: the secretive He3 haulers) show up on your own
/// departures board, stale-marked once <see cref="ValidForSeconds"/> runs out.
/// </summary>
public readonly record struct RouteIntel(string ShipId, double PurchasedAtSimTime, double ValidForSeconds, int Price)
{
    /// <summary>Default validity: a month before a route tip goes stale — ships reroute, crews get suspicious.</summary>
    public const double DefaultValiditySeconds = 30 * 86400;

    public bool IsFresh(double simTime) => simTime <= PurchasedAtSimTime + ValidForSeconds;

    /// <summary>Sim seconds until this entry goes stale (negative once it already has).</summary>
    public double SecondsUntilStale(double simTime) => PurchasedAtSimTime + ValidForSeconds - simTime;
}

/// <summary>
/// A player's bought intel, keyed by ship so a repurchase simply refreshes the entry rather than
/// piling up duplicates. Pure data + pure queries — nothing here reads a clock of its own.
/// </summary>
public sealed class IntelLedger
{
    private readonly Dictionary<string, RouteIntel> _entries = new();

    public IReadOnlyCollection<RouteIntel> Entries => _entries.Values;

    /// <summary>True if a fresh (unexpired) tip is on file for this ship at <paramref name="simTime"/>.</summary>
    public bool Knows(string shipId, double simTime) =>
        _entries.TryGetValue(shipId, out RouteIntel intel) && intel.IsFresh(simTime);

    public bool TryGet(string shipId, out RouteIntel intel) => _entries.TryGetValue(shipId, out intel);

    public void Add(RouteIntel intel) => _entries[intel.ShipId] = intel;

    /// <summary>Drops entries that have gone stale. Bookkeeping only — <see cref="Knows"/> already
    /// ignores stale entries — but keeps a UI list from growing forever across a long play session.</summary>
    public void PruneStale(double simTime)
    {
        List<string>? stale = null;
        foreach (KeyValuePair<string, RouteIntel> kv in _entries)
        {
            if (!kv.Value.IsFresh(simTime))
            {
                (stale ??= []).Add(kv.Key);
            }
        }

        if (stale is not null)
        {
            foreach (string id in stale)
            {
                _entries.Remove(id);
            }
        }
    }
}

/// <summary>
/// The dark space web (vision ¶14/¶16): pricing and where-you-can-trade rules for buying and
/// selling route intel. Every function here is pure — callers (the UI) own all state: credits,
/// the buyer's <see cref="IntelLedger"/>, the seller's <see cref="TrackedTargetLedger"/>. Same
/// inputs, same price, always; determinism is law.
/// </summary>
public static class IntelMarket
{
    /// <summary>Heliocentric distance past which a station counts as a "far trading post" — the
    /// same central-space/outer-reaches split <c>TrafficSchedule</c> uses for long-haul routes,
    /// sitting between Mars's orbit (2.28e11 m) and Jupiter's (7.79e11 m).</summary>
    public const double FarTradingPostThresholdMeters = 4e11;

    /// <summary>Baseline price for a route tip before cargo-value/distance scaling.</summary>
    public const int BasePrice = 300;

    /// <summary>Fraction of a ship's total cargo value folded into the buy price — a He3 hauler's
    /// route is worth more to know about than a milk-run pod's.</summary>
    public const double CargoValueWeight = 0.4;

    /// <summary>Fraction of quality × cargo value a sold track fetches at market.</summary>
    public const double SellValueFraction = 0.3;

    /// <summary>Below this quality a tracked target is too shaky to sell — nobody pays for a maybe.</summary>
    public const double MinSellableQuality = 0.5;

    /// <summary>
    /// Distance-from-Earth price multiplier: farther out, intel is cheaper — the outer reaches
    /// trade in secrets as a matter of course, so a route tip is common currency there, not
    /// exotic gossip. Near Earth the same tip is rare and commands a premium. Clamped to
    /// [0.3, 3] so neither extreme breaks the economy.
    /// </summary>
    public static double DistanceFactor(double distanceFromEarthMeters) =>
        Math.Clamp(1.5e12 / (1e11 + distanceFromEarthMeters), 0.3, 3.0);

    /// <summary>
    /// Buy price for a route tip about a ship worth <paramref name="totalCargoValue"/> credits of
    /// cargo, purchased at a point of sale <paramref name="distanceFromEarthMeters"/> from Earth.
    /// </summary>
    public static int BuyPrice(int totalCargoValue, double distanceFromEarthMeters)
    {
        double price = (BasePrice + totalCargoValue * CargoValueWeight) * DistanceFactor(distanceFromEarthMeters);
        return Math.Max(1, (int)Math.Round(price));
    }

    /// <summary>
    /// Sell price for one of your own tracking-post finds: scales with how good the fix is and
    /// what the target hauls. Selling never removes the track — information copies, it isn't
    /// spent (vision ¶16: scanning becomes an income loop, not a one-shot cash-out).
    /// </summary>
    public static int SellPrice(double quality, int totalCargoValue) =>
        Math.Max(0, (int)Math.Round(quality * totalCargoValue * SellValueFraction));

    /// <summary>True once a tracked target's quality is good enough to fence at a haven.</summary>
    public static bool CanSellTrack(double quality) => quality >= MinSellableQuality;

    /// <summary>
    /// Where the dark web operates (vision ¶16): pirate havens anywhere, or any station farther
    /// from the sun than <see cref="FarTradingPostThresholdMeters"/>. Central-space stations
    /// (compute farms and factories close to the inner planets) don't deal in stolen timetables,
    /// and ordinary planets never do either — the trade needs the outer reaches' anonymity, not
    /// a haven's disposition alone paired with a planet's crowds.
    /// </summary>
    public static bool CanTradeIntelAt(CelestialBody body, double distanceFromSunMeters) =>
        body.IsHaven || (body.Kind == BodyKind.Station && distanceFromSunMeters > FarTradingPostThresholdMeters);
}
