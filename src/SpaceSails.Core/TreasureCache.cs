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
    bool PlayerOwned)
{
    /// <summary>Total cargo units in the chest (0 when it holds only coin).</summary>
    public int TotalCargoUnits => Cargo?.Sum(c => c.Units) ?? 0;

    /// <summary>Hot (stolen-flagged) cargo units in the chest — the evidence buried here.</summary>
    public int HotCargoUnits => Cargo?.Where(c => c.Hot).Sum(c => c.Units) ?? 0;

    /// <summary>True when the chest holds anything at all worth digging up.</summary>
    public bool HasContents => Coin > 0 || TotalCargoUnits > 0;

    /// <summary>The big caption the map card shouts: "PHOBOS — from the monolith, 40 paces
    /// anti-spinward". Body name is the caller's display string (title-cased off the ephemeris).</summary>
    public string Caption(string bodyDisplayName) =>
        $"{bodyDisplayName.ToUpperInvariant()} — from {LandmarkName}, {Paces} paces {Bearing}";

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
