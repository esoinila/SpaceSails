namespace SpaceSails.Core;

/// <summary>
/// #406 — the pure name-matching + ranking behind the Nav search box (type-to-find a jump target
/// instead of zoom-hunting). Kept in Core so the contract — "what does 'ti' match, and in what order"
/// — is unit-tested without booting a browser. The client gathers the candidates (bodies, depots,
/// contacts) and reuses these two calls to filter + rank them; the select/jump action stays in the UI.
/// </summary>
public static class NavSearch
{
    /// <summary>Case-insensitive substring test on a trimmed query. An empty/whitespace query matches
    /// NOTHING — the box shows no dropdown until you type, because "search for nothing" is not "list
    /// everything" (that is what the frame picker and the Layers tree are for).</summary>
    public static bool Matches(string query, string name)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>How good a hit is — LOWER is better, <see cref="int.MaxValue"/> means "no match".
    /// An exact whole-name hit ranks first, then a prefix hit, then an interior hit (earlier position
    /// wins), so typing "ti" surfaces "Titan" ahead of "Mestitia".</summary>
    public static int Rank(string query, string name)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrEmpty(name))
        {
            return int.MaxValue;
        }

        string q = query.Trim();
        int idx = name.IndexOf(q, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return int.MaxValue;      // no match
        }

        if (name.Length == q.Length)
        {
            return 0;                 // exact whole-name hit
        }

        if (idx == 0)
        {
            return 1;                 // prefix hit
        }

        return 2 + idx;               // interior hit — the earlier the character, the better
    }

    /// <summary>Filter <paramref name="items"/> to those whose name matches <paramref name="query"/>,
    /// ordered best-match first. Ties (same rank) keep the caller's input order — so the client can feed
    /// candidates already in intent order (threats, then contacts, then bodies) and have that hold as the
    /// tiebreak. Empty query yields an empty list (see <see cref="Matches"/>).</summary>
    public static List<T> FilterAndRank<T>(string query, IEnumerable<T> items, Func<T, string> nameOf)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return items
            .Select((item, index) => (item, index, rank: Rank(query, nameOf(item))))
            .Where(t => t.rank != int.MaxValue)
            .OrderBy(t => t.rank)
            .ThenBy(t => t.index)
            .Select(t => t.item)
            .ToList();
    }
}
