namespace SpaceSails.Core;

/// <summary>
/// The beach-comber kit (owner rulings, Evening-wind playtest 2026-07-18). The shovel-and-metal-detector
/// play: <b>"I bring a shovel, I should have a minesweeper-like capability to check any square of area
/// for shallow buried treasure."</b> An empty-sling excursion is a legitimate fishing expedition, not a
/// dead end — pressing E digs a probe hole at your feet, and this rule says what the ground gives up.
///
/// <para>The owner's die (verbatim): <b>"Could be done with a D100 throw to see if there is treasure at
/// any spot on Miranda. Some surfaces may be too hard to dig though. But the die could handle those."</b>
/// So one D100 per surface square decides among three bands — most rolls turn up <see cref="Outcome.Nothing"/>
/// ("unlucky to find anything but still possible"), a band is <see cref="Outcome.TooHard"/> (bedrock, the
/// dig refused with flavor), and a rare band is a <see cref="Outcome.Shallow"/> find (a modest coin/scrap
/// — luck, deliberately NOT economy-breaking).</para>
///
/// <para>Pure and fully deterministic, keyed on the body id and the integer surface SQUARE — the same
/// seeded idiom as <see cref="ReeverRaid"/> and <see cref="ReeverTide"/>, salted off the ONE shared
/// <see cref="DiceRule"/> engine (never <see cref="System.Random"/> or the clock — determinism is law in
/// Core). A given square on a given body always answers the same throw, so a swept grid never lies and a
/// test pins every band. Squares are the client's world-grid quantised through
/// <see cref="SquareOf"/>; the live per-visit swept marks are the client's thin real-time layer.</para>
/// </summary>
public static class BeachComber
{
    /// <summary>The side of one probe square in deck-units — a hole is this coarse, so a metal-detector
    /// sweep walks square to square rather than pixel to pixel. Small enough that the deep field holds
    /// many distinct throws, big enough that a checked mark reads on the deck-plan grid.</summary>
    public const double SquareSize = 3.0;

    /// <summary>What a probe of a square turns up. Ordered from the common case out to the rare one.</summary>
    public enum Outcome
    {
        /// <summary>The overwhelming common case — you dig and find nothing but regolith. Unlucky, but
        /// (owner) "still possible" to have found something, so a fishing expedition is never hopeless.</summary>
        Nothing,

        /// <summary>Bedrock a foot down — the shovel rings and won't bite. The die handling "some surfaces
        /// … too hard to dig" (owner): the dig is refused, no hole opened, but the square is now KNOWN.</summary>
        TooHard,

        /// <summary>A rare shallow find — a stray coin or a scrap of pre-war salvage a few inches down.
        /// Modest by design: this is luck, not an economy, so it never rivals a buried chest.</summary>
        Shallow,
    }

    // The D100 bands (owner's throw). Most of the die is Nothing; a modest low band is bedrock; a thin
    // high band is a find. Tuned so a fishing expedition mostly comes up empty, sometimes hits rock, and
    // once in a rare while pays a little — luck, never the loot loop.
    /// <summary>A roll at or under this is bedrock — the ground too hard to dig (≈12%).</summary>
    public const int TooHardMax = 12;

    /// <summary>A roll at or above this is a shallow find (≈4%); everything between is plain
    /// <see cref="Outcome.Nothing"/> (≈84%).</summary>
    public const int ShallowMin = 97;

    // A shallow find is deliberately small — a handful of coin, and only sometimes a single scrap unit.
    /// <summary>The fewest / most credits a shallow find ever coughs up — pocket change, not a payday.</summary>
    public const int MinFindCoin = 15;
    public const int MaxFindCoin = 90;

    /// <summary>The cargo class a shallow scrap find yields — a scrap of pre-war salvage.</summary>
    public const string FindCargoClass = "Salvage";

    /// <summary>The surface square (integer grid cell) a world position falls in — the key a probe throws
    /// against. Quantises through <see cref="SquareSize"/> with a floor so negative coordinates (the deep
    /// field runs into negative y) bucket cleanly and stably.</summary>
    public static (int X, int Y) SquareOf(double worldX, double worldY) =>
        ((int)System.Math.Floor(worldX / SquareSize), (int)System.Math.Floor(worldY / SquareSize));

    /// <summary>The world centre of a surface square — where a checked mark or a refusal glyph is drawn.</summary>
    public static (double X, double Y) SquareCenter(int squareX, int squareY) =>
        ((squareX + 0.5) * SquareSize, (squareY + 0.5) * SquareSize);

    /// <summary>Throw the D100 for one square on one body (the owner's "D100 to see if there is treasure
    /// at any spot"). Fully deterministic in (<paramref name="bodyId"/>, square) — the same spot always
    /// answers the same throw, so a swept grid is stable across a visit and a test pins each band. A
    /// <see cref="Outcome.Shallow"/> result carries a modest coin + maybe one scrap unit; the other
    /// outcomes carry nothing.</summary>
    public static Probe Roll(string bodyId, int squareX, int squareY)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ulong seed = DiceRule.Seed($"beachcomber:{bodyId}", squareX, squareY);
        int d100 = DiceRule.Roll(seed, 100).Face; // 1..100

        if (d100 <= TooHardMax)
        {
            return new Probe(Outcome.TooHard, d100, 0, 0);
        }
        if (d100 >= ShallowMin)
        {
            // A modest find: a little coin off a salted stream, and a ~1-in-3 chance of a single scrap.
            int coin = DiceRule.RollAmount(DiceRule.Seed(seed, "find-coin"), MinFindCoin, MaxFindCoin).Face;
            int scrap = DiceRule.Roll(DiceRule.Seed(seed, "find-scrap"), 3).Face == 3 ? 1 : 0;
            return new Probe(Outcome.Shallow, d100, coin, scrap);
        }
        return new Probe(Outcome.Nothing, d100, 0, 0);
    }
}

/// <summary>
/// A settled beach-comber probe of one square: the band the D100 landed in, the raw face (so the reveal
/// can show the throw), and what a shallow find yields. Pure data — the hole, the swept mark and the
/// fanfare live client-side.
/// </summary>
/// <param name="Outcome">Which band the throw landed in.</param>
/// <param name="Roll">The raw D100 face (1..100), for the on-screen reveal.</param>
/// <param name="FindCoin">Credits a shallow find pays (0 unless <see cref="BeachComber.Outcome.Shallow"/>).</param>
/// <param name="FindScrapUnits">Scrap-salvage units a shallow find yields (0 or 1).</param>
public readonly record struct Probe(BeachComber.Outcome Outcome, int Roll, int FindCoin, int FindScrapUnits)
{
    /// <summary>True when the probe turned up a shallow find worth pocketing.</summary>
    public bool IsFind => Outcome == BeachComber.Outcome.Shallow;

    /// <summary>True when the shovel rang off bedrock — the dig is refused.</summary>
    public bool IsTooHard => Outcome == BeachComber.Outcome.TooHard;
}
