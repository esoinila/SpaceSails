namespace SpaceSails.Core;

/// <summary>
/// The service history of an NPC hull — every ship the player scans carries a Victoria-I story
/// (owner ruling, 2026-07-19, aboard the ex-premium ferry Victoria I, approving Fable's proposal):
/// <i>"This Victoria I once sailed as premium roro ship between Finland and Sweden, then it was
/// bought by Tallink and now shows its age, though the history is still there of its glory days."</i>
/// Approved beat: NPC ships carry FORMER NAMES in their dossiers — <i>"ex-Aurora Queen, ex-mail
/// packet, three owners deep"</i> — so false colors becomes one more name in a long ledger of names.
///
/// <para>Pure authored Core data (repo agreement §9): the history is <b>seeded off the ship id</b>,
/// so the same hull reads the same story every session on both client and server — canon, not
/// per-run noise. The client just reads it and paints a compact HISTORY block on the dossier and a
/// one-line teaser on a scanned contact; the copy is one tested truth, not hand-rolled UI strings.</para>
///
/// <para>The PLAYER's own hull already carries its builder's-plate history (#392,
/// <see cref="Interior.Plaques.Ship"/>); this is the NPC-hull twin of that plate — the same
/// Koski &amp; Daughters yard canon, extended with the other house yards, worn onto every trader,
/// pod and hunter the scope resolves.</para>
/// </summary>
/// <param name="LaidDown">The service-life line — "Laid down at &lt;yard&gt; (&lt;berth&gt;), &lt;year&gt;."</param>
/// <param name="FormerNames">0–3 former names, each with a brief fate — "ex-AURORA QUEEN (mail packet,
/// Luna run)". Distinct within a hull; empty for a ship still under her maiden name.</param>
/// <param name="OwnersDeep">How many owners deep she is — previous owners before the present one
/// (0 = still her first owner). Always at least <see cref="FormerNames"/> count: a rename implies a
/// re-registration.</param>
/// <param name="Condition">One condition line in the Victoria-I key — "shows her age; the history is
/// still in her frames."</param>
public sealed record ShipHistory(
    string LaidDown,
    IReadOnlyList<string> FormerNames,
    int OwnersDeep,
    string Condition)
{
    /// <summary>True once there is any former name on the book — she's carried another name before this.</summary>
    public bool HasFormerNames => FormerNames.Count > 0;

    /// <summary>The former names joined the way a dossier lists them —
    /// "ex-AURORA QUEEN (mail packet, Luna run) · ex-KESTREL (impounded, renamed)"; empty when none.</summary>
    public string FormerNamesLine => string.Join(" · ", FormerNames);

    /// <summary>The one-line teaser a scanned/viewed contact shows — the headline of the history:
    /// her first former name and how many owners deep she runs, or (no rename) just the owners-deep
    /// count, or (a maiden hull) that she still flies her first name.</summary>
    public string Teaser
    {
        get
        {
            if (HasFormerNames)
            {
                // The bare former name (drop the "ex-" prefix and the "(fate)" tail) for a tight teaser.
                string first = FormerNames[0];
                int paren = first.IndexOf(" (", StringComparison.Ordinal);
                string bare = paren >= 0 ? first[..paren] : first;
                return $"{bare} · {OwnersDeepPhrase}";
            }

            return OwnersDeep > 0
                ? $"{OwnersDeepPhrase}, her maiden name still"
                : "maiden hull — her first owner, her first name";
        }
    }

    /// <summary>The FALSE COLORS idiom (copy-level only, no mechanic): frame the ship's CURRENT name as
    /// merely the LATEST name in a long ledger of names — so a hull flying false colors reads as one more
    /// alias, and even an honestly-named hull with a past is "currently answering to" her latest name.
    /// Returns null for a maiden hull flying true (there is no ledger to be the latest of).</summary>
    public string? CurrentNameLine(string currentName, bool flyingFalseColors)
    {
        if (flyingFalseColors)
        {
            return $"Currently answering to {currentName} — the latest false name in a long ledger of names.";
        }

        return HasFormerNames
            ? $"Currently answering to {currentName} — the latest name she's carried."
            : null;
    }

    /// <summary>The owners-deep phrase, pluralized — "one owner deep", "three owners deep".</summary>
    public string OwnersDeepPhrase => $"{OwnersWord(OwnersDeep)} owner{(OwnersDeep == 1 ? "" : "s")} deep";

    /// <summary>Small number word for the owners-deep count — "one", "two", "three"… falling back to the
    /// figure past the pool (a hull that deep in owners has earned the digits).</summary>
    private static string OwnersWord(int n) => n switch
    {
        1 => "one",
        2 => "two",
        3 => "three",
        4 => "four",
        5 => "five",
        6 => "six",
        _ => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
}

/// <summary>
/// The seeded service-history generator (owner ruling, 2026-07-19). One call — <see cref="For"/> —
/// turns a ship id into a whole Victoria-I story, deterministically: same id, same history, every
/// session, client and server alike. The pools are authored in the house voice — proud old names that
/// outlived their shine, the house yards that laid the hulls down, and fates that read like a long
/// service record.
/// </summary>
public static class ShipHistories
{
    // The house yards (owner canon extends here). Koski & Daughters is the player's own builder's-plate
    // yard (#392, Plaques.Ship); the other three are the sister houses that fit out the rest of the
    // system's hulls, each named in the house voice and berthed at a place the world already knows —
    // and Louhi Sunside is the "Mercury yards" The Deep's dedication plaque already names.
    private static readonly string[] Yards =
    [
        "Koski & Daughters Orbital Yards (Rauma Crater, Luna)",
        "Ahti & Sons Shipwrights (the Rusty Roadstead, Mars orbit)",
        "Vellamo Drydocks (the cloud-yards, Cinder Roost)",
        "Louhi Sunside Yards (Mercury, sunside)",
    ];

    // Proud old names that outlived their shine — a ship's glory-days name, the one lit on every
    // departures board before the shine wore off. Kept distinct from the live NPC callsigns' feel
    // (grander, a liner's name) so a former name reads as a former GLORY, not just an old handle.
    private static readonly string[] FormerNamePool =
    [
        "AURORA QUEEN", "MERIDIAN CROWN", "PRIDE OF SELENE", "SABLE DUCHESS",
        "GILDED WAKE", "MORNING PACKET", "EMPRESS OF ICE", "HALCYON",
        "LADY MIRANDA", "GOLDEN ROADSTEAD", "KESTREL ROYAL", "WINDLASS QUEEN",
        "CORONET", "SILVER PELICAN", "DUCHESS OF LUNA", "TYCHO'S PRIDE",
        "THE FAIR WIND", "NORTHERN LIGHT", "VELLAMO'S DAUGHTER", "ROADSTEAD BELLE",
        "SUNWARD EMPRESS", "HANSEATIC", "STARLING", "HALYARD",
    ];

    // The fates that hang off a former name — a line's whole rise and fall in a parenthesis. Read like
    // a service record: glory, misfortune, the auction block, the flag change.
    private static readonly string[] Fates =
    [
        "mail packet, Luna run",
        "premium ferry, Selene–Roadstead",
        "impounded, renamed",
        "requisitioned in the war, released",
        "mothballed a decade, recommissioned",
        "sold for debt, name struck",
        "flagged out, re-flagged twice",
        "ran the He3 lanes, retired",
        "a revenue cutter, paid off",
        "a fire off Ceres, rebuilt",
        "the pride of a line that folded",
        "seized for smuggling, auctioned",
    ];

    // One condition line, in the Victoria-I key — worn now, but the history is still in her frames.
    private static readonly string[] Conditions =
    [
        "She shows her age; the history is still in her frames.",
        "Worn now, but her old glory is still in the lines of her.",
        "The shine's long gone; the pedigree hasn't.",
        "Tired plating, proud bones — the history is still in her frames.",
        "She's carried better names in better days, and she remembers them.",
    ];

    /// <summary>The seeded service history for a hull, by its stable ship id. Deterministic — the same
    /// id yields the same story on every call, session and machine (repo agreement §9). A null/blank id
    /// gets a stable maiden-hull default rather than throwing.</summary>
    public static ShipHistory For(string shipId)
    {
        uint state = Seed(shipId);

        // Laid down: a house yard and a comfortably-old year. "Now" is ~2341 (the player's plate, the
        // stale lifeboat cards); NPC hulls fall in 2270..2319, so every one reads as an aged hull with
        // a past — none newer than the player's own 2341.
        string yard = Yards[(int)(Next(ref state) % (uint)Yards.Length)];
        int year = 2270 + (int)(Next(ref state) % 50u);
        string laidDown = $"Laid down at {yard}, {year}.";

        // 0..3 former names, distinct within the hull (a rename to a name she already wore is no rename).
        int formerCount = (int)(Next(ref state) % 4u);
        var formerNames = new List<string>(formerCount);
        var usedNames = new HashSet<int>();
        for (int i = 0; i < formerCount; i++)
        {
            int nameIdx = PickDistinct(ref state, FormerNamePool.Length, usedNames);
            int fateIdx = (int)(Next(ref state) % (uint)Fates.Length);
            formerNames.Add($"ex-{FormerNamePool[nameIdx]} ({Fates[fateIdx]})");
        }

        // Owners deep: at least as many as the renames (each rename is a re-registration), plus 0..2 more
        // (sold on without a new name — the player's plate's "sold on, and sold on again").
        int ownersDeep = formerCount + (int)(Next(ref state) % 3u);

        string condition = Conditions[(int)(Next(ref state) % (uint)Conditions.Length)];

        return new ShipHistory(laidDown, formerNames, ownersDeep, condition);
    }

    // A stable, process-independent seed from the ship id — FNV-1a, the same hash the plaques' seeded
    // lifeboat dates use — OR'd to non-zero so the xorshift generator never sticks at zero.
    private static uint Seed(string? shipId)
    {
        uint h = 2166136261u;
        foreach (char c in shipId ?? string.Empty)
        {
            h = (h ^ c) * 16777619u;
        }

        return h | 1u;
    }

    // A tiny deterministic xorshift step — draws a sequence of independent values off the seed so each
    // roll (yard, year, count, names, fates, owners, condition) is decorrelated but reproducible.
    private static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    // Draw an index into a pool not already used by this hull, so a ship never lists the same former
    // name twice. Bounded: caps at the pool size, which every caller stays well under (≤3 of many).
    private static int PickDistinct(ref uint state, int poolSize, HashSet<int> used)
    {
        int idx;
        do
        {
            idx = (int)(Next(ref state) % (uint)poolSize);
        }
        while (!used.Add(idx));

        return idx;
    }
}
