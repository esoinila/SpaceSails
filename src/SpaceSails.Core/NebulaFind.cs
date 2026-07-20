namespace SpaceSails.Core;

/// <summary>
/// The seeded WHERE for the NEBULA fragment that needs a deterministic "is it here?" — the rare Nebula
/// Mutual adjuster who works a bar some watches and, deep in a drink, lets the tell slip
/// (<c>adjuster-tell</c>). Pure and seedable (issue #422's delivery lane), the exact mirror of
/// <see cref="KaamosFind.HolderAtBar"/> so the two arcs' bar seams are shaped alike and never collide. No
/// RNG, no wall clock — the same bar/watch always answers the same, so the adjuster "appears rarely" yet
/// stays put while present (asking twice the same watch answers the same).
/// </summary>
public static class NebulaFind
{
    /// <summary>Rarity of the adjuster: on roughly one bar-watch in this many, the Nebula Mutual adjuster
    /// is drinking in the room. Keyed on (bar, watch-day) so they are stable across a single watch yet only
    /// show up now and then — the roving-contacts rhythm (#414), the same cadence as the KAAMOS holder.</summary>
    public const int AdjusterOneInWatches = 5;

    /// <summary>True if the rare Nebula Mutual adjuster is drinking at THIS bar on THIS watch-day (sim-day).
    /// Deterministic per (bar, day). The client offers the "ask about NEBULA" seam when this holds and the
    /// <c>adjuster-tell</c> shard is not yet in hand. Salted distinctly from the KAAMOS holder so the two
    /// contacts never share a watch by coincidence.</summary>
    public static bool AdjusterAtBar(string bodyId, int watchDay)
    {
        if (string.IsNullOrEmpty(bodyId))
        {
            return false;
        }

        ulong h = DiceRule.Seed($"nebula:adjuster:{bodyId}", watchDay);
        return h % (ulong)AdjusterOneInWatches == 0;
    }
}
