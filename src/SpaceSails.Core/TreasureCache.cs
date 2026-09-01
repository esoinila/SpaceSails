namespace SpaceSails.Core;

/// <summary>One buried cargo line inside a <see cref="TreasureCache"/> — a class, a count, and
/// whether it is hot (stolen-flagged at theft time). Burying hot cargo takes it off the ship's
/// books entirely (#223): the law confiscates what it can SEE aboard, and buried goods are, by
/// construction, not aboard.</summary>
/// <param name="CargoClass">What was buried (He3, Ice, Alloys, …).</param>
/// <param name="Units">How many units are in the chest.</param>
/// <param name="Hot">True if stolen-flagged — evidence taken off the books while it stays buried.</param>
public readonly record struct CacheCargo(string CargoClass, int Units, bool Hot);

/// <summary>
/// A buried chest (#223, the owner's dream): coin and/or cargo taken OFF the ship and hidden on a
/// landable body, plus the map text that leads back to it. Pure saved data — a future persistence
/// layer serializes a list of these. Deliberately one record, not a system.
///
/// <para><b>The confiscation seam.</b> Buried contents are invisible to a boarding confiscation by
/// construction: they live in the <see cref="CacheLedger"/>, never in the ship's carried coin or
/// hold. The BUSTED lane's confiscation reads only carried goods, so it CANNOT see a cache — it does
/// not (and must not) consult the ledger. Hot cargo buried therefore also stops counting as visible
/// evidence while it is underground; see <see cref="CacheLedger.BuriedHotUnits"/> for the clean
/// read the heat/evidence lane uses.</para>
///
/// <para><b>X always marks the spot.</b> The stored <see cref="Bearing"/> and <see cref="Paces"/>
/// are the honest truth: digging needs only that the cache is here. The barflies swear "X never
/// marks the spot" — in this game the professor is wrong every single time.</para>
/// </summary>
/// <param name="Id">Stable cache id (mint order / owner-scoped).</param>
/// <param name="BodyId">The body it is buried on (e.g. "phobos").</param>
/// <param name="LandmarkName">The site it is paced from, article included ("the monolith").</param>
/// <param name="Bearing">The in-world bearing from the landmark ("anti-spinward").</param>
/// <param name="Paces">How many paces along that bearing.</param>
/// <param name="Coin">Buried credits.</param>
/// <param name="Cargo">Buried cargo lines (may be empty when only coin is buried).</param>
/// <param name="BuriedSimTime">Sim time the chest went into the ground.</param>
/// <param name="Owner">Whose hoard this is — "you" for the player, else the NPC/contact name.</param>
/// <param name="PlayerOwned">True when the player buried it (the discovery roll only threatens ours;
/// an NPC cache is only ours to take once we hold its map).</param>
/// <param name="ReeverLevel">The stash's standing Reever presence (#295): 0..<see cref="ReeverRaid.MaxReevers"/>
/// watchdogs haunt this ground, left by whatever pack turned out at burial. It re-arms the 2D6 on every
/// return (our dig or a rival's search) and hardens the stash against a rival's slow discovery roll — a
/// Reever-haunted moon is the best vault with the most dangerous key. Defaults to 0 so every older saved
/// cache (and every rumour/NPC chest) round-trips unchanged.</param>
/// <param name="DigX">The REAL surface x the chest went into the ground (beach-comber kit, owner Evening
/// wind 2026-07-18: "bury anywhere"). Playtest bug #5: the ✗ used to render at a hash-scattered spot, not
/// where you dug — free-form burying records the actual dug position so the mark, and "dig at the X",
/// land where the shovel did. Null for an older save, a rumour/NPC chest, or any cache with no recorded
/// spot; the client then falls back to the deterministic hash-scatter (<c>MoonSurface.CachePosition</c>),
/// so every legacy cache round-trips unchanged.</param>
/// <param name="DigY">The REAL surface y the chest went into the ground (see <paramref name="DigX"/>).</param>
/// <param name="SiteIndex">#650 · WHICH GROUND the chest is under — the ordinal of the landing site
/// (<see cref="LandingSite.Index"/>) the shovel went in at. Since #320 a body offers 2–4 seeded sites and
/// every one of them rebuilds the SAME local surface frame, so a cache filtered by body alone drew its ✗ —
/// and dug out — at the same local x/y on all of them, on ground the captain had never walked.
///
/// <para><b>Null means body-wide</b>, which is exactly what every chest buried before this field existed
/// was: a legacy save loads with no site, keeps today's behaviour on every site of its body, and writes
/// back byte-for-byte (the vault omits the key entirely when it is null). New burials record their site,
/// so the ✗ is drawn only on the one ground it is actually under, and the map card names that ground.</para></param>
public readonly record struct TreasureCache(
    string Id,
    string BodyId,
    string LandmarkName,
    string Bearing,
    int Paces,
    int Coin,
    IReadOnlyList<CacheCargo> Cargo,
    double BuriedSimTime,
    string Owner,
    bool PlayerOwned,
    int ReeverLevel = 0,
    double? DigX = null,
    double? DigY = null,
    int? SiteIndex = null)
{
    /// <summary>True when this cache recorded the actual dug spot (free-form bury) rather than leaning on
    /// the hash-scatter — both coords present. A legacy or rumour cache has neither and falls back.</summary>
    public bool HasDigSpot => DigX is not null && DigY is not null;

    /// <summary>Total cargo units in the chest (0 when it holds only coin).</summary>
    public int TotalCargoUnits => Cargo?.Sum(c => c.Units) ?? 0;

    /// <summary>Hot (stolen-flagged) cargo units in the chest — the evidence buried here.</summary>
    public int HotCargoUnits => Cargo?.Where(c => c.Hot).Sum(c => c.Units) ?? 0;

    /// <summary>True when the chest holds anything at all worth digging up.</summary>
    public bool HasContents => Coin > 0 || TotalCargoUnits > 0;

    /// <summary>#650 · True when this chest knows which of the body's landing sites it is under. False for
    /// every legacy save and every rumour/NPC chest, which stay body-wide exactly as they are today.</summary>
    public bool HasSite => SiteIndex is not null;

    /// <summary>#650 · Is this chest under the ground the captain is standing on? A sited cache answers only
    /// for its own site; a null-site (legacy) cache answers yes anywhere on its body — the old behaviour,
    /// kept deliberately so no saved chest becomes undiggable.</summary>
    public bool IsOnSite(int siteIndex) => SiteIndex is not { } mine || mine == siteIndex;

    /// <summary>#650 · The display name of the ground this chest is under ("The Ridge Camp"), resolved off
    /// the body's seeded site board (<see cref="LandingSites"/>, deterministic per body id, so the name is
    /// stable forever without being copied into the save). Null when the cache is body-wide.</summary>
    public string? SiteName =>
        SiteIndex is { } i ? LandingSites.At(BodyId, i).Name : null;

    /// <summary>The big caption the map card shouts. A sited chest names the ground it is actually under —
    /// "PHOBOS · THE RIDGE CAMP — from the monolith, 40 paces anti-spinward" (#650) — so the card, and the
    /// ledger row that reads the same text, tell a captain standing on the wrong site where to go back to.
    /// A body-wide (legacy/rumour) chest keeps the original "PHOBOS — from the monolith, …". Body name is
    /// the caller's display string (title-cased off the ephemeris).</summary>
    public string Caption(string bodyDisplayName) =>
        SiteName is { } site
            ? $"{bodyDisplayName.ToUpperInvariant()} · {site.ToUpperInvariant()} — from {LandmarkName}, {Paces} paces {Bearing}"
            : $"{bodyDisplayName.ToUpperInvariant()} — from {LandmarkName}, {Paces} paces {Bearing}";

    /// <summary>The bearing+paces line on its own ("40 paces anti-spinward of the monolith").</summary>
    public string BearingLine =>
        $"{Paces} paces {Bearing} of {LandmarkName}";

    /// <summary>A one-line contents summary for the ledger and dig fanfare: "1,200 cr + 4 units
    /// (2 hot)". Reads "empty" only for a spent record.</summary>
    public string ContentsLine()
    {
        var parts = new List<string>();
        if (Coin > 0)
        {
            parts.Add($"{Coin:N0} cr");
        }
        if (TotalCargoUnits > 0)
        {
            int hot = HotCargoUnits;
            parts.Add(hot > 0 ? $"{TotalCargoUnits} units ({hot} hot)" : $"{TotalCargoUnits} units");
        }
        return parts.Count == 0 ? "empty" : string.Join(" + ", parts);
    }
}
