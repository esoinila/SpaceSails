using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #320 · Miranda is a world, not a level. A destination body is no longer ONE spot you fall onto — it
/// offers a SEEDED SET of distinct landing sites (2–4 per body), each with its own name, character line,
/// and — the mechanical hook — its own ground. Where you set down matters, and a revisit can vary.
///
/// <para>The kind names the mood; the flavor is the one-line house voice the destination board reads
/// ("the Wild Plain — nobody out here will hear you"). The <see cref="LandingSite.LayoutSalt"/> is what
/// makes the choice bite: it parameterizes the surface deck-plan seed
/// (<see cref="SurfaceLayout.For(string, in SurfaceLayout.Field, string?)"/>), so a different site grows
/// a visibly different wing/feature layout on the same body — reusing the existing generation, inventing
/// no new subsystem.</para>
/// </summary>
public enum LandingSiteKind
{
    /// <summary>The established outback landing — today's ground (the kiosk, the dig fields, the body's
    /// signature deep landmark). Always site 0, always the canon ground (empty salt).</summary>
    WildPlain,
    DepotApron,
    CraterShelf,
    IceFissure,
    DerelictPad,
    ShadowedRille,
    QuietBasin,
    RidgeCamp,
}

/// <summary>One landing site on a body's destination board: its ordinal, kind, display name, the terse
/// house-voice character line, and the layout salt that seeds its ground. Pure data — the client shows
/// the set, the player picks one, and the picked salt flows into the surface deck-plan build.</summary>
public readonly record struct LandingSite(
    int Index, LandingSiteKind Kind, string Name, string Flavor, string LayoutSalt);

/// <summary>
/// #320 · The seeded landing-site generator. Pure and fully deterministic (splitmix/FNV via the shared
/// <see cref="DiceRule"/>, never <see cref="System.Random"/> or the clock), keyed off the body id — so a
/// given body ALWAYS offers the same site set, and re-landing re-offers it. Site 0 is always the Wild
/// Plain on the body's canon ground (empty salt), preserving every authored signature (Miranda's monolith
/// maze, Luna's mass-driver ruins) exactly as before; the remaining seeded sites draw distinct kinds from
/// the pool, each salting a different seeded ground.
/// </summary>
public static class LandingSites
{
    /// <summary>The count bounds (owner: "2–4 per landed body"). Site 0 is always present; the seeded
    /// roll adds one to three more.</summary>
    public const int MinSites = 2;
    public const int MaxSites = 4;

    // The secondary pool (site 0 is always WildPlain). Each entry is a kind + its display name + the terse
    // character line the board reads in the house voice. Verbatim and original — the owner reads these.
    private static readonly (LandingSiteKind Kind, string Name, string Flavor)[] Pool =
    [
        (LandingSiteKind.DepotApron, "The Depot Apron",
            "Fused rockcrete and dead floodlights. Someone kept this pad, before the war."),
        (LandingSiteKind.CraterShelf, "The Crater Shelf",
            "A broad ledge under a blown rim. Long sightlines, and nowhere to hide."),
        (LandingSiteKind.IceFissure, "The Ice Fissure",
            "A crack in the crust breathing old cold. Mind your footing near the dark."),
        (LandingSiteKind.DerelictPad, "The Derelict Pad",
            "A landing stage gone to rust and silence. Something set down here and stayed."),
        (LandingSiteKind.ShadowedRille, "The Shadowed Rille",
            "A gully of permanent night. Down here the tracker is the only eye you trust."),
        (LandingSiteKind.QuietBasin, "The Quiet Basin",
            "A low bowl of settled dust. Too quiet, if you stand still long enough to notice."),
        (LandingSiteKind.RidgeCamp, "The Ridge Camp",
            "A wind-scoured shelf and the bones of an old survey camp. Someone left in a hurry."),
    ];

    // Site 0's fixed identity — the established landing, on the canon ground (empty salt).
    private const string WildPlainName = "The Wild Plain";
    private const string WildPlainFlavor = "Open regolith to the dead horizon. Nobody out here will hear you.";

    /// <summary>The full seeded site set for a body, in stable order (site 0 first). Deterministic per
    /// body id: the same body always yields the same kinds, names, and salts, so a revisit offers the
    /// identical board and the chosen site is reproducible.</summary>
    public static IReadOnlyList<LandingSite> For(string? bodyId)
    {
        string id = bodyId ?? "";
        var sites = new List<LandingSite>(MaxSites)
        {
            // Site 0 — the Wild Plain, always on the body's canon ground (empty salt → SurfaceLayout.For
            // routes to the authored/seeded signature, unchanged from today).
            new(0, LandingSiteKind.WildPlain, WildPlainName, WildPlainFlavor, ""),
        };

        int total = Count(id);

        // Deterministically shuffle the secondary pool for this body, then take the first (total-1) — a
        // stable, distinct subset. Fisher–Yates driven by seeded dice, so no two sites share a kind and the
        // pick never moves between visits.
        int[] order = new int[Pool.Length];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = DiceRule.Roll(DiceRule.Seed($"landingsites:{id}:shuffle:{i}"), i + 1).Face - 1; // 0..i
            (order[i], order[j]) = (order[j], order[i]);
        }

        for (int n = 1; n < total; n++)
        {
            (LandingSiteKind kind, string name, string flavor) = Pool[order[n - 1]];
            // Salt is the kind name — unique within a body (the picks are distinct) and stable per site, so
            // SurfaceLayout seeds a different ground for each and the same ground on every revisit.
            sites.Add(new LandingSite(n, kind, name, flavor, kind.ToString()));
        }

        return sites;
    }

    /// <summary>The site at <paramref name="index"/> on a body's board, clamped into range — the client's
    /// safe accessor for a persisted or cheat-forced choice (a stale index never throws; it lands somewhere
    /// real). Site 0 (the Wild Plain) is the floor.</summary>
    public static LandingSite At(string? bodyId, int index)
    {
        IReadOnlyList<LandingSite> set = For(bodyId);
        if (index < 0)
        {
            index = 0;
        }
        if (index >= set.Count)
        {
            index = set.Count - 1;
        }
        return set[index];
    }

    /// <summary>How many sites this body offers (2–4), seeded off its id. Exposed for tests and the board.</summary>
    public static int Count(string? bodyId)
    {
        // 0..(MaxSites-MinSites) added to MinSites → MinSites..MaxSites inclusive.
        int span = MaxSites - MinSites + 1;
        int roll = DiceRule.Roll(DiceRule.Seed($"landingsites:{bodyId ?? ""}:count"), span).Face - 1; // 0..span-1
        return MinSites + roll;
    }
}
