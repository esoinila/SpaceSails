namespace SpaceSails.Core;

/// <summary>
/// #528 · Which bodies have a painted treasure-map card, and what the file is called.
///
/// <para>This lived as a private <c>HashSet</c> in <c>Map.Quests</c> with a comment that said, in so many
/// words, <i>"bodies absent from this set (e.g. miranda) still fall back to the gradient"</i> — and
/// <c>art/treasure-miranda.jpg</c> was sitting finished in <c>wwwroot/art</c> the whole time. A painting
/// nobody had switched on, named in a comment as the example of the thing not switched on.</para>
///
/// <para>That is a whole bug class the #528 audit found three more of at once (the KAAMOS plates were
/// delivered before the cards that show them). The card's image is a CSS <c>url()</c> layered over a
/// deterministic gradient, so a missing file degrades silently to the tint — which is what made this
/// invisible, exactly as the <c>onerror</c>-hide law does elsewhere. The list therefore has to live
/// somewhere a test can read it, and <c>TreasureMapArtIsWiredTests</c> now asserts BOTH directions: every
/// body listed here has a file, and every <c>treasure-*.jpg</c> on disk is listed here.</para>
/// </summary>
public static class TreasureMapArt
{
    /// <summary>The bodies whose treasure-map card art has been delivered
    /// (<c>docs/FridaySecondPlan/hoard-image-manifest.md</c>). A body absent from this set still reads as
    /// its deterministic per-body gradient — the card is never broken, only plainer.</summary>
    public static readonly IReadOnlySet<string> PaintedBodies =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "phobos", "luna", "europa", "ganymede", "callisto", "titan", "enceladus", "miranda",
        };

    /// <summary>The card art for a body, or empty when the body has none and the gradient stands alone.</summary>
    public static string ArtFile(string bodyId) =>
        bodyId is not null && PaintedBodies.Contains(bodyId)
            ? $"art/treasure-{bodyId.ToLowerInvariant()}.jpg"
            : string.Empty;
}
