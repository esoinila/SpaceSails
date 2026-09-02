namespace SpaceSails.Core;

/// <summary>Deterministic string hashing for the hoard rules (FNV-1a, 64-bit). NOT
/// <see cref="object.GetHashCode"/> — that is randomized per process and would make a treasure
/// map's bearing drift between sessions. This is stable forever, so a saved cache re-mints the same
/// map text and a seeded discovery roll replays identically in a test.</summary>
internal static class StableHash
{
    public static ulong Of(string s)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in s)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h;
    }

    /// <summary>A stable hash folded with a numeric salt — for per-period rolls.</summary>
    public static ulong Of(string s, long salt) => Of($"{s}#{salt}");
}

/// <summary>
/// Mints the map text for a fresh cache (#223): the honest bearing + paces from the body's best
/// landmark, derived deterministically from the burial so a saved chest always re-reads the same
/// map. "X always marks the spot" — this IS the X, and it never lies.
/// </summary>
public static class CacheMint
{
    /// <summary>The in-world bearings a map paces along — a spun rock's own compass, no true north
    /// out here. "anti-spinward" is the owner's canonical example.</summary>
    public static readonly IReadOnlyList<string> Bearings =
    [
        "spinward", "anti-spinward", "sunward", "shadeward",
        "rimward", "craterward", "toward the long shadow", "up the ridgeline",
    ];

    /// <summary>Fewest / most paces a map ever calls for — a short honest walk from the landmark.</summary>
    public const int MinPaces = 12;
    public const int MaxPaces = 88;

    /// <summary>The stable seed key for a burial: body, owner and the (integer-second) burial time.
    /// Two burials on the same body at the same instant by the same owner would collide — the caller
    /// disambiguates with a mint index folded into the owner tag or time, which it always has (a
    /// monotonic cache counter).</summary>
    public static string SeedKey(string bodyId, string owner, double buriedSimTime, int mintIndex) =>
        $"{bodyId}|{owner}|{(long)buriedSimTime}|{mintIndex}";

    /// <summary>The bearing for a seed.</summary>
    public static string Bearing(string seedKey) =>
        Bearings[(int)(StableHash.Of(seedKey, 1) % (ulong)Bearings.Count)];

    /// <summary>The pace count for a seed, in [<see cref="MinPaces"/>, <see cref="MaxPaces"/>].</summary>
    public static int Paces(string seedKey) =>
        MinPaces + (int)(StableHash.Of(seedKey, 2) % (ulong)(MaxPaces - MinPaces + 1));

    /// <summary>Mint a complete cache from its contents and place. The landmark is resolved off
    /// <see cref="Landmarks"/>; bearing and paces are derived from the seed. Pure — no side effects,
    /// no clock read (the caller passes the burial time).
    ///
    /// <para>#650 · <paramref name="siteIndex"/> is WHICH GROUND on that body the shovel went in at
    /// (<see cref="LandingSite.Index"/>). Left null the chest is body-wide, exactly as every cache minted
    /// before the field existed — which is why a rumour map, minted with no ground under it, still reads
    /// and digs the way it always has.</para>
    ///
    /// <para>#455 · <paramref name="buried"/> and <paramref name="padDistance"/> are the other two terms of
    /// the safety oracle (<see cref="CacheSafety"/>): whether the shovel went in, and how far from the pad
    /// the captain carried it. Left null — a rumour map, a job's chest — the cache reads under exactly the
    /// odds it always has.</para></summary>
    public static TreasureCache Bury(
        string id, string bodyId, int mintIndex, int coin, IReadOnlyList<CacheCargo> cargo,
        double buriedSimTime, string owner, bool playerOwned, int reeverLevel = 0,
        double? digX = null, double? digY = null, int? siteIndex = null,
        bool? buried = null, double? padDistance = null)
    {
        Landmark site = Landmarks.For(bodyId);
        string seed = SeedKey(bodyId, owner, buriedSimTime, mintIndex);
        return new TreasureCache(
            id, bodyId, site.Name, Bearing(seed), Paces(seed),
            coin, cargo ?? [], buriedSimTime, owner, playerOwned, reeverLevel, digX, digY, siteIndex,
            buried, padDistance);
    }
}

/// <summary>
/// The symmetric discovery risk (ruling 4): a slow periodic dice roll per PLAYER cache — rivals
/// stumble on our hoards, which is exactly the reason to split loot across many small caches. The
/// mirror direction (we find THEIRS) is played through rumour maps, not this roll.
///
/// <para><b>TODO — converge on the shared engine.</b> This is a tiny LOCAL d1000 seeded from the
/// cache id and the period index, standing in until the BUSTED lane's <c>DiceRule</c> lands; when it
/// does, swap <see cref="Roll"/> for a call into it (same seed inputs) so every consequence system
/// rolls on one engine (ruling 0, "the dice are the engine").</para>
///
/// <para>#455 · <b>The threshold is not computed here.</b> Every number this class compares a roll against
/// comes out of <see cref="CacheSafety"/>, which is also what the bury line reads — one oracle, so the
/// promise a captain was given and the dice thrown behind his back are the same arithmetic. This class owns
/// the CLOCK and the DIE; it does not own the odds.</para>
/// </summary>
public static class DiscoveryRule
{
    /// <summary>How often a cache is checked against discovery — one roll per in-game day. Slow on
    /// purpose: a hoard is meant to survive a while, and splitting it is the hedge.</summary>
    public const double PeriodSeconds = 86400.0;

    /// <summary>The percent view of <see cref="CacheSafety.BaseChancePerMille"/> — an unhidden chest's
    /// per-day odds. Modest: a single well-placed chest is likely to survive many days, but a big undivided
    /// hoard is a standing bet.</summary>
    public const int DiscoveryChancePercent = CacheSafety.BaseChancePerMille / 10;

    /// <summary>The floor the watchdog discount can never breach (#295): even a fully Reever-haunted
    /// stash carries a whisper of discovery risk, so a hoard is never truly immortal.</summary>
    public const int MinDiscoveryChancePercent = CacheSafety.MinChancePerMille / 10;

    /// <summary>The face of the die the threshold is compared against. #455 widened it from a d100: with
    /// three terms pushing on one number, whole percents were too coarse to let more than one of them
    /// matter (any single term hit the floor on its own and silently ate the other two).</summary>
    public const int DieFaces = 1000;

    /// <summary>The effective discovery chance, in whole percent, for a stash whose only recorded term is
    /// its standing Reever presence (#295) — the legacy read, and the one every chest buried before #455
    /// still lives under: 4%, 3%, 2%, 1%. It is the oracle's own answer, divided by ten, not a second
    /// ladder kept beside it.</summary>
    public static int DiscoveryChanceFor(int reeverLevel) =>
        CacheSafety.Read(padDistanceDu: null, buried: null, reeverLevel).ChancePerMille / 10;

    /// <summary>The discovery period index containing a sim time (whole days since epoch).</summary>
    public static long PeriodIndex(double simTime) => (long)Math.Floor(simTime / PeriodSeconds);

    /// <summary>The deterministic d1000 [1..1000] for one cache in one period. Same inputs → same roll,
    /// forever (the test gate). Replace the body with the shared DiceRule when it lands.</summary>
    public static int Roll(string cacheId, long periodIndex) =>
        1 + (int)(StableHash.Of(cacheId, periodIndex) % (ulong)DieFaces);

    /// <summary>True when a cache is found in a given period, under the legacy read (watchdogs only).</summary>
    public static bool IsDiscovered(string cacheId, long periodIndex, int reeverLevel = 0) =>
        Roll(cacheId, periodIndex) <= CacheSafety.Read(null, null, reeverLevel).ChancePerMille;

    /// <summary>#455 · True when THIS chest is found in a given period — the full three-term read (carry,
    /// shovel, watchdogs). The overload above is the same call with the other two terms unrecorded.</summary>
    public static bool IsDiscovered(TreasureCache cache, long periodIndex) =>
        Roll(cache.Id, periodIndex) <= CacheSafety.Read(cache).ChancePerMille;

    /// <summary>Whether a cache buried at <paramref name="buriedSimTime"/> gets discovered somewhere
    /// in the span (<paramref name="lastCheckedPeriod"/>, nowPeriod] — the client passes the last
    /// period it already resolved so a warp jump that skips days can't skip a roll. Returns the
    /// discovering period (so the caller can stamp WHEN) or null if it survived the span.</summary>
    public static long? DiscoveredWithin(string cacheId, long lastCheckedPeriod, double nowSimTime, int reeverLevel = 0)
    {
        long nowPeriod = PeriodIndex(nowSimTime);
        for (long p = lastCheckedPeriod + 1; p <= nowPeriod; p++)
        {
            if (IsDiscovered(cacheId, p, reeverLevel))
            {
                return p;
            }
        }
        return null;
    }

    /// <summary>#455 · The same skipped-span scan for a WHOLE chest, so the return-trip roll prices the
    /// carry and the shovel as well as the ground. This is the one the discovery watch calls.</summary>
    public static long? DiscoveredWithin(TreasureCache cache, long lastCheckedPeriod, double nowSimTime)
    {
        long nowPeriod = PeriodIndex(nowSimTime);
        for (long p = lastCheckedPeriod + 1; p <= nowPeriod; p++)
        {
            if (IsDiscovered(cache, p))
            {
                return p;
            }
        }
        return null;
    }
}

/// <summary>
/// The other direction of the symmetry (ruling 4): a bar occasionally sells a RUMOUR MAP to some
/// NPC's forgotten hoard — a purchasable map artifact that flows straight into the same dig path as
/// our own. One generator, modest contents, dice-priced. The price is always a fraction of the
/// haul, so a rumour is a bet that usually pays — but a barfly's map is a barfly's map.
/// </summary>
public static class RumorMaps
{
    /// <summary>A purchasable rumour: the NPC cache it points at, the asking price, and the flavour
    /// the barfly slings with it.</summary>
    public readonly record struct Rumor(TreasureCache Cache, int PriceCredits, string Patter);

    private static readonly string[] Sellers = ["a one-eyed dockhand", "a nervous fuel jockey", "an off-books surveyor", "a drunk ex-collector"];
    private static readonly string[] Ghosts = ["Old Vane", "the Cassini brothers", "Dust-Mary", "a dead fixer called Kell"];

    /// <summary>Generate a rumour deterministically from a seed (the client passes a stable draw key —
    /// station id + day, so a given bar sells a stable rumour that day, not a new one each frame).
    /// The cache it points at is buried on a body with a NAMED landmark when possible (the monolith
    /// is the storied one), holds modest coin + maybe a couple of units, and is priced at a dice-
    /// rolled fraction of the coin — a wager that usually clears.</summary>
    public static Rumor Generate(string drawKey, string preferredBodyId = "phobos")
    {
        ulong h = StableHash.Of(drawKey);
        string ghost = Ghosts[(int)(StableHash.Of(drawKey, 7) % (ulong)Ghosts.Length)];
        string seller = Sellers[(int)(StableHash.Of(drawKey, 8) % (ulong)Sellers.Length)];

        int coin = 300 + (int)(StableHash.Of(drawKey, 3) % 1201UL);   // 300..1500 cr
        int units = (int)(StableHash.Of(drawKey, 4) % 4UL);           // 0..3 units of salvage
        var cargo = units > 0 ? new List<CacheCargo> { new("Salvage", units, Hot: false) } : new List<CacheCargo>();

        // A stale burial time (days back) so the map text is stably minted and the chest reads "old".
        double buriedSimTime = -(double)(StableHash.Of(drawKey, 5) % 40UL + 3UL) * DiscoveryRule.PeriodSeconds;
        int mintIndex = (int)(h % 997UL);

        TreasureCache cache = CacheMint.Bury(
            $"rumor-{drawKey}", preferredBodyId, mintIndex, coin, cargo, buriedSimTime, ghost, playerOwned: false);

        // Dice-priced: 25%..55% of the buried coin, a wager that usually clears.
        int pricePct = 25 + (int)(StableHash.Of(drawKey, 6) % 31UL);
        int price = Math.Max(100, coin * pricePct / 100);

        string patter = $"\"{seller} swears {ghost} buried a chest out on {preferredBodyId} and never came back for it. Map's real. Fifty says it isn't.\"";
        return new Rumor(cache, price, patter);
    }
}
