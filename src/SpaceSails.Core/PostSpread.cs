namespace SpaceSails.Core;

/// <summary>
/// U02-F24 (#1416): every trading post has its own price for a cargo class — one deterministic factor per
/// (post, class), band 0.80–1.25, applied to the class's face value BOTH ways (a post has one price for a
/// thing; the ferry fees still stop churn). Owner 2026-11-09: "price differences are allowed; without price
/// differences there would be no trade runs at all." Design by Fable. No board, no history (KOSH): the trade row
/// carries one told <see cref="Tail"/> at a non-par post, and par posts say nothing. Fence pricing is untouched.
/// </summary>
public static class PostSpread
{
    public const double MinFactor = 0.80;
    public const double MaxFactor = 1.25;

    private sealed record Row(double Default, string? DefaultWords, (string Class, double Factor, string Words)[] Exceptions);

    private static readonly Dictionary<string, Row> Posts = new()
    {
        ["earth"] = new(0.85, "Earth is short of nothing.", []),
        ["satellite-factory"] = new(0.95, "not on the works' list.",
            [("Alloys", 1.15, "the works builds with it."), ("Compute cores", 1.15, "the works builds with it.")]),
        ["selene-gate"] = new(1.00, null, []),
        ["mercury"] = new(0.90, "the Farms are next door.", [("Compute cores", 0.80, "they make these.")]),
        ["mercury-compute"] = new(1.00, null,
            [("He3", 1.20, "the Farms burn it."), ("Compute cores", 0.80, "they make these.")]),
        ["venus"] = new(0.95, "the inner run.", [("Alloys", 0.80, "Venus pours it.")]),
        ["cinder-roost"] = new(1.05, "a roost, not a market.", [("Ice", 1.15, "a hot roost is short of water.")]),
        ["mars"] = new(0.95, "the inner run.", [("Ice", 0.80, "Mars sells ice.")]),
        ["the-space-bar"] = new(1.00, null, []),
        ["jupiter"] = new(1.05, "the long haul.", [("He3", 0.90, "the moons make it.")]),
        ["red-eye"] = new(1.10, "the long haul.", [("Machinery", 1.20, "nothing out here makes it.")]),
        ["saturn"] = new(1.10, "the long haul.", [("He3", 0.90, "the moons make it.")]),
        ["enceladus"] = new(1.10, "the long haul.", [("Ice", 0.80, "an ice moon.")]),
        ["ringside-exchange"] = new(1.00, null, []),
        ["uranus"] = new(1.15, "the long haul.", [("He3", 0.90, "the moons make it.")]),
        ["the-tilt"] = new(1.20, "the long haul.",
            [("He3", 0.90, "the moons make it."), ("Machinery", 1.25, "nothing out here makes it.")]),
        ["neptune"] = new(1.20, "the long haul.", [("He3", 0.90, "the moons make it.")]),
        ["the-deep"] = new(1.25, "the end of the line.", [("He3", 0.90, "the moons make it.")]),
    };

    /// <summary>The (post, class) price factor; 1.00 for an unlisted post.</summary>
    public static double Factor(string? bodyId, string cargoClass)
    {
        if (bodyId is null || !Posts.TryGetValue(bodyId, out Row? row))
        {
            return 1.0;
        }

        foreach ((string cls, double factor, _) in row.Exceptions)
        {
            if (cls == cargoClass) { return factor; }
        }

        return row.Default;
    }

    /// <summary>The post a contact prices as: a station or haven node is its own post (not its parent planet's),
    /// a depot is its host body's. One price whether you reach the node or the depot.</summary>
    public static string PostIdOf(CommerceRule.LocalContact c) =>
        c.Kind is CommerceRule.LocalContactKind.Station or CommerceRule.LocalContactKind.Haven ? c.Id : c.HostBodyId ?? c.Id;

    /// <summary>A class's per-unit value at this post: the face value times the post's factor.</summary>
    public static int UnitValueCr(string? bodyId, string cargoClass) =>
        (int)Math.Round(CargoMarket.UnitValue(cargoClass) * Factor(bodyId, cargoClass));

    /// <summary>What a hold (units per class) is worth at this post. <paramref name="totalValueCr"/> is the
    /// caller's whole-hold face value; whatever it holds beyond the listed classes passes through unscaled.</summary>
    public static int HoldValueCr(string? bodyId, IReadOnlyDictionary<string, int>? byClass, int totalValueCr)
    {
        if (byClass is null) { return totalValueCr; }

        int faceListed = 0;
        int atPost = 0;
        foreach ((string cls, int units) in byClass)
        {
            faceListed += units * CargoMarket.UnitValue(cls);
            atPost += units * UnitValueCr(bodyId, cls);
        }

        return atPost + (totalValueCr - faceListed);
    }

    /// <summary>The told tail for the trade row — null at par. E.g. "−15% here — Earth is short of nothing."</summary>
    public static string? Tail(string? bodyId, string cargoClass)
    {
        double factor = Factor(bodyId, cargoClass);
        if (bodyId is null || factor == 1.0 || !Posts.TryGetValue(bodyId, out Row? row))
        {
            return null;
        }

        string? words = row.DefaultWords;
        foreach ((string cls, _, string w) in row.Exceptions)
        {
            if (cls == cargoClass) { words = w; break; }
        }

        if (words is null) { return null; }

        int pct = (int)Math.Round(Math.Abs(factor - 1.0) * 100);
        string sign = factor < 1.0 ? "\u2212" : "+";
        return $"{sign}{pct}% here \u2014 {words}";
    }
}
