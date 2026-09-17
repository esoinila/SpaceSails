using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #592 · A SECRET LAB'S OWN SECRET LAB — the band nobody listed. A facility whose BOTTOM BAND IS NOT
/// ON ITS OWN PLAN: a shaft not in the directory, a floor the panel does not list, and underneath the
/// expensive documented clandestine operation, the thing it was hiding from its own staff.
///
/// <para>The building lies by omission, which is in register with everything else down here: the
/// panel says what it has always said, there is no button below this one, and that is true. The way
/// down is a card somebody left in a room (#590) — a piece of paper telling the truth about a
/// building that is not.</para>
///
/// <para>Canon holds hardest here. The deepest floor of the deepest facility may be full of evidence
/// of an enormous, decades-long operation and may never once name what it produced.</para>
///
/// <para>Split out of <c>UndergroundComplex.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered; the family declares no <c>static readonly</c> field and the two <c>const</c>s here are
/// compile-time folded, so no initialiser order can be wrong.</para>
/// </summary>
public static partial class UndergroundComplex
{
    // ── #592 · A SECRET LAB'S OWN SECRET LAB ────────────────────────────────────────────────────────────
    //
    // Owner: "we could even have a secret lab lab :-D"
    //
    // The joke is good and the mechanic under it is better. A facility whose BOTTOM BAND IS NOT ON ITS OWN
    // PLAN: a shaft not in the directory, a floor the panel does not list. Everything above it is a real,
    // expensive, thoroughly documented clandestine operation — and underneath THAT is the thing the
    // clandestine operation was hiding from its own staff.
    //
    // It costs almost nothing to build because three things were already right:
    //
    //   * depth is free — a floor reuses the surface's own envelope, so a hidden band takes no space;
    //   * bands already gate descent, and a hidden band is that mechanism with the next shaft simply not
    //     advertised;
    //   * Kind already varies the building, so the deepest band can be a DIFFERENT KIND from the floors
    //     above it — a records annex whose bottom is a clinic tells a story nobody has to narrate.
    //
    // THE BUILDING LIES BY OMISSION, which is exactly in register with everything else down here. The panel
    // on the floor above says what it has always said: there is no button below this one. It does not hedge,
    // it does not hint, and it is not lying about a door — the button really is not there. The way down is a
    // card somebody left in a room (#590), which is a piece of paper telling the truth about a building that
    // is not.
    //
    // Canon holds hardest here, because this is the single most tempting place in the game to explain the
    // Old Ones. It does not. The deepest floor of the deepest facility may be full of evidence of an
    // enormous, well-funded, decades-long operation, and may never once name what the operation produced.

    /// <summary>How many sites in this many have something under the floor they admit to. Rare on purpose:
    /// the moment it is common it stops being a secret and becomes a level.</summary>
    public const int UnlistedOneInN = 4;

    /// <summary>Does this site have a band nobody listed? Seeded off its own id, so it is a fact about the
    /// world and not about the visit.</summary>
    public static bool HasUnlistedBand(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #411 · THE HEAD OFFICE HAS NOTHING TO HIDE FROM ITSELF, and that absence is the rank difference.
        // A branch office lies by omission to its own staff — a shaft not in the directory, a floor the
        // panel does not list. Here the directory is complete, every button is on the panel, and the whole
        // building is on the plan in the lobby. It does not need to keep a secret from the people who work
        // here, because the people who work here are the ones keeping it.
        //
        // BELT AND BRACES, said out loud rather than left to be discovered: this is currently REDUNDANT.
        // The head office already takes the whole allowance (DepthOf == DeepestPossibleFloor), so the second
        // guard below — "a band whose own shaft head would be clamped to nothing is not a band" — already
        // says no. It stays because the redundancy is the point: the day somebody raises the performance
        // bound, HQ would silently grow a band it does not admit to and the whole rank difference this file
        // is built on turns inside out. Its guard was proven RED by forcing this TRUE rather than by
        // deleting it, which is the honest way to say "dead today, load-bearing tomorrow".
        if (IsHeadOffice(bodyId))
        {
            return false;
        }

        // Only somewhere that already had room to hide something. A three-floor annex with a secret basement
        // is a bungalow with a dungeon; the lie needs a building big enough to keep a secret from its staff.
        int listed = DepthOf(bodyId);
        if (listed > -FloorsPerShaft)
        {
            return false;
        }

        // And only where the hidden band's own shaft head still fits inside the performance guard. That
        // bound is a guard and not a design bottom (#585), but a band that would be clamped to nothing is
        // not a band.
        if (BandTop(BandOf(listed) + 1) <= DeepestPossibleFloor)
        {
            return false;
        }

        return DiceRule.Roll(DiceRule.Seed($"hive:unlisted:{bodyId}"), UnlistedOneInN).Face == 1;
    }

    /// <summary>How far down a captain can ACTUALLY walk — the listed depth, plus the band nobody listed,
    /// plus (#677) the band nobody dug.
    ///
    /// <para>Every audit, every renderer and every lab wants this one: an unlisted floor is still a floor,
    /// and a topology nothing walks is a topology nobody has checked. Only the things that speak FOR the
    /// building — the lift panel, the directory — get to use <see cref="DepthOf"/>.</para></summary>
    public static int TrueDepthOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (HasFoundBand(bodyId))
        {
            return BandBottom(FoundBandOf(bodyId));
        }
        return HasUnlistedBand(bodyId) ? UnlistedBottomOf(bodyId) : DepthOf(bodyId);
    }

    /// <summary>#592/#677 · The deepest floor of the band nobody listed — the bottom of the BUILDING, which
    /// stopped being the same number as <see cref="TrueDepthOf"/> the day something under it turned out not
    /// to be a building at all.
    ///
    /// <para>Written down rather than inlined because two callers need exactly this and would otherwise each
    /// reach for <c>TrueDepthOf</c>: the thing on the pallet (<see cref="RelicRoomFor"/>, which belongs to
    /// the operation and not to the halls) and the guards. When #677 moved the true bottom two bands deeper,
    /// a <c>TrueDepthOf</c> here would have quietly relocated the one designated relic in the game into a
    /// gallery nobody built.</para></summary>
    public static int UnlistedBottomOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return BandBottom(UnlistedBandOf(bodyId));
    }

    /// <summary>#592 · WHICH band is the one nobody listed.
    ///
    /// <para>It is the next WHOLE band under the one the listed bottom falls in — not "four floors below the
    /// listed bottom", which sounds the same and is not. Bands are fixed slices of four counted from the
    /// surface, because that is what a shaft is; a hidden band that started at an arbitrary depth would
    /// share a car with the floors above it and the secret would be reachable by pressing DOWN. There is a
    /// GAP between the two, and nothing is generated in it: the listed building stops where it stops, and
    /// the unlisted one starts at its own shaft head.</para></summary>
    public static int UnlistedBandOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return BandOf(DepthOf(bodyId)) + 1;
    }

    /// <summary>The top floor a shaft band serves — where its car opens.</summary>
    public static int BandTop(int band) => -(band * FloorsPerShaft) - 1;

    /// <summary>The deepest floor a shaft band could serve if nothing stopped it.</summary>
    private static int BandBottom(int band) =>
        Math.Max(DeepestPossibleFloor, -((band + 1) * FloorsPerShaft));

    /// <summary>Is this floor one of the ones the building does not admit to?</summary>
    public static bool IsUnlisted(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasUnlistedBand(bodyId) && level < 0 && BandOf(level) == UnlistedBandOf(bodyId);
    }
}
