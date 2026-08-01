using System;

namespace SpaceSails.Core;

/// <summary>
/// #573 · WHAT IS STILL IN THE BUILDINGS — the "services" half of the owner's report.
///
/// <para>Owner, walking past them: <i>"there seemed to be shelter like spaces that were just missing the
/// services and the doors.... let's fix those."</i> The doors were the easy half. This is the other: a
/// thick-walled room you can enter and find nothing in is worse than no room at all, because the walk in
/// cost air and taught you not to bother next time.</para>
///
/// <para><b>But not every ruin is a prize, and that is the point.</b> If every building paid out, walking
/// into them would stop being a decision and become a chore you perform on all of them. Roughly half hold
/// something; the rest are somebody's empty house, and finding those is what makes the others worth the
/// suit-air. The emergency shelters stay categorically different — they are the only places that make AIR,
/// and nothing here competes with that.</para>
/// </summary>
public static class SurfaceSalvage
{
    /// <summary>What a building still had in it when everybody left.</summary>
    public enum Find
    {
        /// <summary>Stripped, or never held anything. Most of them.</summary>
        Nothing,

        /// <summary>A few rounds in a drawer — never a magazine's worth (#563's partial-cache law).</summary>
        Rounds,

        /// <summary>Somebody's kit, worth something to a fence.</summary>
        Goods,

        /// <summary>Paperwork nobody meant to leave — the breadcrumb material.</summary>
        Papers,
    }

    /// <summary>Rounds a drawer holds. Small on purpose: this is a top-up, not a resupply, and the shelters'
    /// lockers are the real thing.</summary>
    public const int RoundsMin = 8;
    public const int RoundsMax = 22;

    /// <summary>Credits a bit of sellable kit fetches.</summary>
    public const int GoodsMin = 40;
    public const int GoodsMax = 260;

    /// <summary>What is in this particular building. Seeded per site and index, so the same ruin always
    /// holds the same thing and a captain who leaves and comes back is not re-rolling it.</summary>
    public static Find WhatIsInside(string bodyId, string siteSalt, int index)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        // 8-sided: four empty, two rounds, one goods, one papers. Half hold something, and the good stuff
        // is scarce enough to be worth remembering where it was.
        return DiceRule.Roll(DiceRule.Seed($"salvage:{bodyId}:{siteSalt}:{index}"), 8).Face switch
        {
            1 or 2 => Find.Rounds,
            3 => Find.Goods,
            4 => Find.Papers,
            _ => Find.Nothing,
        };
    }

    /// <summary>How many rounds this drawer holds.</summary>
    public static int RoundsIn(string bodyId, string siteSalt, int index) =>
        RoundsMin + DiceRule.Roll(DiceRule.Seed($"salvage:{bodyId}:{siteSalt}:{index}:rounds"),
            RoundsMax - RoundsMin + 1).Face - 1;

    /// <summary>What the kit fetches.</summary>
    public static int GoodsIn(string bodyId, string siteSalt, int index) =>
        GoodsMin + DiceRule.Roll(DiceRule.Seed($"salvage:{bodyId}:{siteSalt}:{index}:cr"),
            GoodsMax - GoodsMin + 1).Face - 1;

    /// <summary>The label on the thing worth pressing E on.</summary>
    public static string LabelFor(Find find) => find switch
    {
        Find.Rounds => "🔫 A DRAWER, HALF SHUT",
        Find.Goods => "📦 SOMEBODY'S KIT",
        Find.Papers => "🗂 PAPERS ON THE FLOOR",
        _ => "",
    };

    /// <summary>What an empty room says when you have crossed it for nothing. It should sting slightly and
    /// be over quickly — the walk was the cost, and pretending otherwise would be worse.</summary>
    public static string EmptyRoomLine(string bodyId, string siteSalt, int index) =>
        DiceRule.Roll(DiceRule.Seed($"salvage:{bodyId}:{siteSalt}:{index}:empty"), 3).Face switch
        {
            1 => "Dust, a bench bolted to the floor, and the clean rectangles where things used to stand. " +
                 "Somebody packed properly.",
            2 => "Empty. Whoever left took the door seals with them, which tells you how long they had.",
            _ => "Nothing worth the air it cost you to walk in. It happens.",
        };

    /// <summary>The receipt for a drawer of rounds.</summary>
    public static string RoundsLine(int rounds) =>
        $"🔫 {rounds} loose rounds in a drawer somebody shut in a hurry. Not a resupply. Enough to matter " +
        "if it happens in the next ten minutes.";

    /// <summary>The receipt for sellable kit.</summary>
    public static string GoodsLine(int credits) =>
        $"📦 Tools, a sealed kit, a spare regulator — {credits:N0} cr of it once you find somebody who is " +
        "not fussy about provenance.";

    /// <summary>The papers. Texture, never testimony: the same rule the outposts live under (#563) — a badge,
    /// a docket, a roster. Nothing here explains what is walking around outside, and nothing ever will.</summary>
    public static string PapersLine(string bodyId, string siteSalt, int index) =>
        DiceRule.Roll(DiceRule.Seed($"salvage:{bodyId}:{siteSalt}:{index}:papers"), 3).Face switch
        {
            1 => "🗂 A shift roster, curling at the edges. Fourteen names, and somebody has drawn a line " +
                 "through eleven of them in a different pen. No dates, no reason given.",
            2 => "🗂 A delivery docket for four hundred litres of sealant, signed for by a company that has " +
                 "no address on it. The counter-signature is a number rather than a name.",
            _ => "🗂 A hand-written note taped inside a locker door: DO NOT SIGN THE EXTENSION. WHATEVER " +
                 "THEY OFFER. It is the only thing in the room somebody bothered to leave for a stranger.",
        };
}
