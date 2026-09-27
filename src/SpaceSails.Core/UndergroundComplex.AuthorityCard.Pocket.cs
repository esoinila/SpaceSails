using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE POCKET NEVER LIES (#678) — what a room hands over, what goes in the pocket, and the lines
/// that say so.
///
/// <para>Split out of <c>UndergroundComplex.AuthorityCard.cs</c> under #251 as a pure move: one contiguous
/// run, no member renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class UndergroundComplex
{
    // ── #678 · THE POCKET NEVER LIES ────────────────────────────────────────────────────────────────────
    //
    // Owner, after a live playtest: "we should have CI test that makes sure all picked items that sound
    // useful are put into the inventory ... If refused the item should stay where it was investigated last —
    // not disappear like they do now, or seem to."
    //
    // Two silent drops, one law. The pickup sentence and the pickup were composed in the client in the wrong
    // order — the line was printed, the room was marked emptied, and only then did Satchel.Add get a chance
    // to refuse — so a full pocket ate a find while announcing it, and a Key room whose card could not be
    // minted narrated a countersigned card into a hand that was empty. Both are this repo's third named bug
    // class: the sim doing one thing while the sentence reports another.
    //
    // The composition lives here now, pure, where a test can walk every haul against every pocket. The rule
    // it enforces, in one line:
    //
    //     A PICKUP LINE MAY ONLY BE PRINTED FOR SOMETHING THAT ACTUALLY WENT IN.
    //
    // And its other half: what the pocket cannot take is NOT consumed. The room keeps it, and searching
    // again offers it again — which is the enforcement side of #615 (leave must not destroy).

    /// <summary>#678 · What turning over one room actually yields: the thing that goes in the pocket (null if
    /// nothing does), the sentence that says so, and whether the room has been emptied at all.</summary>
    /// <param name="Take">The item to add, or null — nothing to add is not a failure, it is most rooms.</param>
    /// <param name="Line">The pocket line appended to the haul line. Empty where there is nothing to say.</param>
    /// <param name="RoomEmptied">False ONLY when the pocket refused the find. The caller must not mark the
    /// room searched — the find is still lying there.</param>
    public readonly record struct Pickup(Satchel.Item? Take, string Line, bool RoomEmptied);

    /// <summary>
    /// #615 · <b>WHAT THIS ROOM WOULD HAND OVER, ASKED WITHOUT A POCKET IN THE ROOM.</b>
    ///
    /// <para>Lifted verbatim out of <see cref="WhatGoesInThePocket"/>, which still asks it and nothing else,
    /// the day a find became a DECISION. The offer of KEEP or LEAVE has to name the thing being decided
    /// about, and it has to do so for a captain whose sleeve is already full — that captain is the whole
    /// point of the question — so the identity of a find and the capacity for it are two questions now
    /// instead of one answer that goes null when the answer to the other is no.</para>
    ///
    /// <para>Null is most rooms: a stripped room hands over nothing, a crate is carried out and sold rather
    /// than pocketed, and a Key room with no card left to mint describes no card.</para>
    /// </summary>
    public static Satchel.Item? WhatTheRoomHandsOver(Haul haul, AuthorityCard? minted, string findId)
    {
        ArgumentNullException.ThrowIfNull(findId);
        return haul switch
        {
            Haul.Records => new Satchel.Item(Satchel.Kind.Paper, findId),
            Haul.Dirt => new Satchel.Item(Satchel.Kind.Dirt, findId),

            // #614 · What goes in the pocket is the RECORD of the thing on the pallet. You cannot lift it,
            // and a satchel claiming to hold a three-metre alloy band would be the same lie one size up.
            Haul.Relic => new Satchel.Item(Satchel.Kind.Relic, findId),
            Haul.Key when minted is { } card => new Satchel.Item(Satchel.Kind.Authority, card.Id),
            _ => null,
        };
    }

    /// <summary>#678 · What goes in the pocket, said in the same breath as the decision to put it there.</summary>
    /// <param name="haul">What the room holds.</param>
    /// <param name="hereBodyId">The site being searched — used only to tell a card for THIS building from a
    /// card for another one, which is the one thing worth saying about an authority as it goes in.</param>
    /// <param name="minted">For a <see cref="Haul.Key"/>, the card the caller actually minted. Null means no
    /// card exists to hand over, and then the room says so rather than describing one.</param>
    /// <param name="findId">The durable id of this find — the seed tag the prose is rebuilt from.</param>
    /// <param name="carried">What is already in the pocket.</param>
    /// <remarks>#615 · The identity of the find is <see cref="WhatTheRoomHandsOver"/>'s and no longer this
    /// method's own switch — one source of truth, because the KEEP/LEAVE offer has to know WHAT is being
    /// decided about at a moment when the pocket may well refuse it, and a second transcription of this
    /// table is how a room would come to offer a decision over one object and hand over another.</remarks>
    public static Pickup WhatGoesInThePocket(
        Haul haul, string hereBodyId, AuthorityCard? minted, string findId,
        IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(hereBodyId);
        ArgumentNullException.ThrowIfNull(findId);

        Satchel.Item? take = WhatTheRoomHandsOver(haul, minted, findId);

        if (take is { } wanted && !Satchel.CanTake(carried, wanted))
        {
            return new Pickup(null, PocketFullLine, RoomEmptied: false);
        }

        string line = haul switch
        {
            Haul.Records => PaperPocketLine,
            Haul.Dirt => "  🎒 Into your pocket: a file on somebody.",

            // #677 · A record out of the halls is the SAME law as the pallet — what goes in the pocket is the
            // record of a thing that stays — said in the owner's own words, and it carries no leading indent
            // because the room it came out of has nothing of its own to say first (HaulLine returns empty
            // there, deliberately). Told apart by the find's own id, minted once by FindId.
            Haul.Relic when IsHallRecord(findId) => FoundRecordFindLine,
            Haul.Relic => "  🎒 Into your pocket: measurements, a photograph, a scraping. The thing itself " +
                "stays where it is.",
            Haul.Key when minted is { } c && !string.Equals(c.BodyId, hereBodyId, StringComparison.Ordinal)
                => "  🎒 Into your pocket: an authority card — and it is not for this building.",
            Haul.Key when minted is not null => "  🎒 Into your pocket: an authority card.",
            Haul.Equipment => "  💳 Crated and carried out — it sells, it does not fit a pocket.",

            // A stripped room, and a Key room that had no card left to give. Neither has anything to say
            // about a pocket, and saying nothing is the honest answer for both.
            _ => "",
        };

        return new Pickup(take, line, RoomEmptied: true);
    }

    /// <summary>#678 · What the game says when a sheet of operational paper goes into the sleeve.
    ///
    /// <para>#1061 · Lifted out of the switch above, where it was a literal, the day a sheet that came off a
    /// MOON rather than out of a room needed the same sentence. A pickup line may only be printed for
    /// something that actually went in (#678's own law), and that law is much easier to keep when there is
    /// one sentence to print rather than one per place a paper can be found.</para></summary>
    public const string PaperPocketLine = "  🎒 Into your pocket: operational paper.";

    /// <summary>#678 · What a full pocket says. It is the only refusal in the game that leaves the world
    /// unchanged, and it has to be unmistakable about that: the find is still there.</summary>
    public const string PocketFullLine =
        "  🎒 Your hands and pockets are full, so you put it back exactly where it was lying. It will still " +
        "be here when you have read, spent or left something behind.";

    /// <summary>What the gate says when the card works. Said once, at the moment the car goes deeper than
    /// this shaft was ever dug to.
    ///
    /// <para>#592: worded so it is true of BOTH shafts it can open. It used to say "where the plan said a
    /// shaft would be" — right about the listed building, and a lie about the band the plan denies having.
    /// A card that announces the secret is a card that has given it away.</para></summary>
    public static string CardAcceptedLine(AuthorityCard card) =>
        $"🎫 You find the other shaft. It is not marked and it is not beside the first one, and its gate " +
        $"reads the card without hesitating — {CardTitle(card)}, countersigned by an office that stopped " +
        "answering its own post decades ago and never once revoked a thing. The car below is colder than " +
        "the one above.";

    /// <summary>#760 · What the gate says when it honours a card that was issued somewhere else.
    ///
    /// <para>Its own sentence, because it is its own fact. "The gate reads it without hesitating" is what a
    /// card's own gate says; this is a gate deciding that an office two moons away is an office it answers
    /// to, and a captain let through on that basis has learned something real about the world — which is
    /// exactly the payoff #715 names for making entities legible.</para>
    ///
    /// <para>It names the SITE the card came from and not the outfit. The site code is printed on the card
    /// in the captain's hand (#679); the company is a thing they can look up in their own pocket, where the
    /// satchel groups the wallet under it. A gate that read a company name out loud would be doing the
    /// player's inference for them.</para></summary>
    public static string StandingHonouredLine(AuthorityCard held) =>
        $"🎫 The gate reads it, and it is not this building's card — it was issued for " +
        $"{BodyNames.Designation(held.BodyId)} SITE, and the gate opens anyway. Whoever the " +
        "countersignatures answer to, they answer for this hole too. Nobody down here was ever working for " +
        "the moon.";
}
