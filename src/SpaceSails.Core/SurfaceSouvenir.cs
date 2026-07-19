namespace SpaceSails.Core;

/// <summary>
/// The surface souvenir kiosk's tee, keyed to the moon actually underfoot (#379). The owner walked up
/// to the Ganymede kiosk and it "sold me a Miranda souvenir T :-D" — the walk-up console on the walked
/// surface (distinct from the haven gift shops, #367) had the T-shirt copy hardcoded to Miranda from
/// the days when Miranda was the only ground you could land on. Since #366/#377 you land on Luna,
/// Ganymede, Titan, Phobos… so the kiosk must print THE LOCAL MOON's shirt.
///
/// <para>The gag is <b>generated, not hand-listed</b> — the house "I walked X and all I got was this
/// T-shirt" idiom (the same joke the haven gift shops tell, <c>HavenInterior</c>) with a handful of
/// seeded variants and the body's name dropped in, so every future landable moon sells its own shirt
/// by construction with no per-body table to keep. The one exception is <b>Miranda</b>, the canon story
/// body: it keeps its original hand-written line as its own (the maroon-story ground earned it).</para>
///
/// <para>Pure and deterministic — the per-body variant is chosen by a stable FNV hash of the body id
/// (never <see cref="object.GetHashCode"/>, which is process-randomized), so a given moon always sells
/// the same shirt and a test pins every line. The only substitution slot in a gag is the body name, so
/// the printed copy can never leak a stray brace.</para>
/// </summary>
public static class SurfaceSouvenir
{
    /// <summary>Miranda's canon body id — the one ground that keeps its original hand-written gag.</summary>
    public const string MirandaId = "miranda";

    /// <summary>Miranda's original tee line (#313), preserved verbatim as Miranda's own so the canon
    /// story body never regresses to a generated slogan.</summary>
    public const string MirandaGag = "The print's cracked; the sizing is 'optimistic pre-war human'.";

    // The seeded gag pool — the house "I walked {BodyName} …" idiom. Exactly one substitution slot
    // ({0} = the walked body's name) per line, so nothing else can leak into the printed copy.
    private static readonly string[] TeeGags =
    [
        "\"I walked {0} and all I got was this dusty T-shirt.\"",
        "\"I crossed {0} on foot and all I got was this lousy T-shirt.\"",
        "\"My boots wore out on {0} and all I got was this pre-war T-shirt.\"",
        "\"I dug half of {0} and all I got was this cracked-print T-shirt.\"",
    ];

    /// <summary>How many seeded gag variants exist (for tests that sweep the pool).</summary>
    public static int VariantCount => TeeGags.Length;

    /// <summary>The tee's shelf name for the moon underfoot, e.g. <c>"a GANYMEDE souvenir tee"</c> —
    /// the same shape as the original Miranda entry, only no longer hardcoded.</summary>
    public static string TeeItem(string bodyName) =>
        $"a {(bodyName ?? string.Empty).ToUpperInvariant()} souvenir tee";

    /// <summary>The tee's printed gag for the walked body. Miranda keeps its own hand-written line;
    /// every other moon gets a seeded variant of the house idiom, chosen deterministically from the
    /// body id so the same ground always sells the same shirt.</summary>
    public static string TeeGag(string bodyId, string bodyName)
    {
        if (IsMiranda(bodyId))
        {
            return MirandaGag;
        }
        int variant = (int)(StableHash.Of(bodyId ?? string.Empty) % (ulong)TeeGags.Length);
        return GagVariant(variant, bodyName);
    }

    /// <summary>A specific seeded gag variant with the body name dropped in — the pure spine
    /// <see cref="TeeGag(string,string)"/> selects from, exposed for tests. The index wraps, so any
    /// integer is a valid pick and the substitution is the body name and nothing else.</summary>
    public static string GagVariant(int variant, string bodyName)
    {
        int idx = ((variant % TeeGags.Length) + TeeGags.Length) % TeeGags.Length;
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture, TeeGags[idx], bodyName ?? string.Empty);
    }

    /// <summary>Whether this ground is Miranda (case-insensitive), so it keeps its canon gag.</summary>
    public static bool IsMiranda(string? bodyId) =>
        string.Equals(bodyId, MirandaId, System.StringComparison.OrdinalIgnoreCase);
}
