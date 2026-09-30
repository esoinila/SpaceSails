namespace SpaceSails.Core;

/// <summary>
/// Deterministic string hashing (FNV-1a, 64-bit) — the one shared seeded hash for ids that decide something
/// about the world. NOT <see cref="object.GetHashCode"/>: .NET randomises string hashing per process, so a
/// world decision seeded on it is decided per RUN rather than per seed — the archive node that was aboard a
/// wreck in one boot and gone in the next (#1340), a treasure map's bearing drifting between sessions. This is
/// stable forever and on every machine (worldbuilding §9: the same scenario and seed produce the same world).
///
/// <para>Born inside the hoard rules (it was <c>internal</c> in <c>Hoard.cs</c>) and moved here, public, by
/// #1340 so the client's wreck rolls can seed off the same function Core's do.</para>
/// </summary>
public static class StableHash
{
    /// <summary>The 64-bit FNV-1a of every UTF-16 unit of <paramref name="s"/>.</summary>
    public static ulong Of(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
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

    /// <summary>#1340 · <see cref="Of(string)"/> as the <c>long</c> a <see cref="DiceRule.Seed(string, long[])"/>
    /// state takes — the drop-in for <c>(long)id.GetHashCode(StringComparison.Ordinal)</c>, which is the
    /// per-process hash this exists to replace.</summary>
    public static long Id(string s) => unchecked((long)Of(s));
}
