using System;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #615 · <b>A FIND IS A DECISION, NOT AN AUTOMATIC PICKUP.</b>
///
/// <para>Owner: <i>"should we have like keep / leave option when we find stuff?"</i></para>
///
/// <para>Searching a room used to transfer whatever was in it, silently, on the frame the sentence printed.
/// With a twelve-slot sleeve that is the wrong default in both directions: it fills the captain's pockets
/// with papers they have already decided are worthless, and it takes the DECISION away from the one system
/// in this game built entirely out of decisions (#603 — the choice to read a document AS A CLUE is what
/// lights the tracker). The room hands nothing over now until somebody says so.</para>
///
/// <h3>The three laws, and each of them is somebody else's law already</h3>
/// <list type="number">
/// <item><b>The find offers both verbs.</b> Two words, no prose: the room's own line has already said what
/// is in front of the captain, and a decision card that argued for one answer would be the game doing the
/// deciding again one layer up.</item>
/// <item><b>LEAVE never destroys — THE ROOM REMEMBERS.</b> This is #678's law
/// (<c>UndergroundComplex.Pickup.RoomEmptied</c>) reached by a choice instead of by a refusal: a room is not
/// struck off until its find has actually gone into a pocket, so searching it again offers the same find.
/// And because the register of turned-over rooms rides the VAULT (#573 — a place you have been stays a place
/// you have been), the captain can fly away, come back a month later and walk to the paper they stepped over.
/// </item>
/// <item><b>KEEP with a full satchel is the moment the capacity means something.</b> It opens the pockets
/// (#691's compartments) with Core's own refusal written on them, and the find stays in the room while the
/// captain makes room. Never a silent drop, never a silent refusal — #678's founding sin, which is what
/// <see cref="UndergroundComplex.WhatGoesInThePocket"/> exists to make impossible.</item>
/// </list>
///
/// <h3>What is deliberately NOT a decision</h3>
/// <para><b>The key</b> (#585's authority card on a lanyard) is exempt, and that is #1069's ruling rather
/// than a convenience: it is the WAY DOWN and not a paper. It costs nothing to carry — the wallet has no
/// capacity at all (<see cref="Satchel.Compartment.Wallet"/>) — so there is no pressure for a decision to
/// resolve, and asking a captain whether they would like to be able to open the next door is not a choice,
/// it is a checkpoint. Taking it is not a decision.</para>
///
/// <para><b>A crate and an empty room.</b> Equipment is carried out and sold on the spot; it never enters a
/// compartment, so the capacity this whole issue is about never touches it. A stripped room hands over
/// nothing, and offering two verbs over nothing would be a prompt attached to an absence.</para>
///
/// <para><b>And the ground store is not touched.</b> <see cref="LeftBehind"/> is where a thing the captain
/// PICKED UP and then set down lies, and every sentence it prints says <i>"where you left it"</i> — that
/// wording is a lie about a find that was never in anybody's hand, which is this repository's third named
/// bug class (the sim doing one thing while the sentence reports another) in the exact register this issue
/// is about. Its own docblock also records the v1 ruling that it is excursion-scoped on purpose and that
/// hardening it into the vault is a decision for the owner. So the thing a captain declines stays where it
/// physically is — in the room, on the room's own console, drawn by the building's own signage — and the
/// register that rides the vault is the register of rooms already turned over.</para>
/// </summary>
public static class KeepOrLeave
{
    /// <summary>The two verbs, and the only two words this feature adds to the game. Plate-idiom nouns: the
    /// room's line has already said what the thing IS, so the buttons say only what the hand does.</summary>
    public const string KeepLabel = "Keep";

    /// <summary>…and its other half. See <see cref="KeepLabel"/>.</summary>
    public const string LeaveLabel = "Leave";

    /// <summary>#615 · WHAT THE CAPTAIN IS TOLD WHEN THEY LOOK AT A FIND AND WALK AWAY FROM IT. Authored by
    /// Fable on the issue's closing pass (2026-09-03) and shipped verbatim.
    ///
    /// <para>Two flat clauses, because LEAVE has no consequence to report: nothing moved, nothing was spent,
    /// nothing was destroyed. The first says what the world did (it kept the thing); the second is the
    /// promise the whole verb rests on — #678's law and #573's register, said in the register a captain
    /// actually reads. <b>The room is not struck off</b>, so its console is still standing there and
    /// searching it again offers the same two verbs, a month and a lift-off later.</para>
    ///
    /// <para>It says <i>the room</i> and not <i>where you left it</i> on purpose: <see cref="LeftBehind"/>'s
    /// wording is about a thing that was in a hand and put down, and this one never left the shelf. That
    /// distinction is the class docblock's third paragraph, and it is the sentence's job to keep it.</para>
    ///
    /// <para>Said on the PULSE and filed nowhere. Everywhere else the pulse line and the casebook line are
    /// the same sentence, because what happened is what is worth remembering — and here nothing happened. A
    /// casebook that recorded every paper a captain declined would be a book of things they did not do.</para></summary>
    public const string LeftWhereItLies = "Left where it lies. The room will still have it.";

    /// <summary>#615 · Is THIS find a decision?
    ///
    /// <para>Asked of what the room would hand over <b>ignoring capacity</b>
    /// (<see cref="UndergroundComplex.WhatTheRoomHandsOver"/>) and never of what a full pocket would accept:
    /// a captain with no room left is exactly the captain who most needs to be asked, and a gate that read
    /// the capacity here would go quiet at the one moment the question is worth anything.</para>
    ///
    /// <para>True for the things that cost a compartment — operational paper, a file on somebody, the record
    /// of the thing on the pallet. False for the key, for a crate, and for a room with nothing in it; the
    /// class docblock argues each of those three.</para></summary>
    public static bool IsADecision(UndergroundComplex.Haul haul, Satchel.Item? offered) =>
        offered is not null && haul != UndergroundComplex.Haul.Key;

    /// <summary>#573 · How one turned-over room is written down so it survives the shuttle.
    ///
    /// <para>A <b>site-qualified</b> key, which the excursion's own <c>HiveRoomsEmptied</c> is not: that set
    /// holds <c>HiveInterior.RoomKey(level, roomIndex)</c>, which says nothing at all about which moon it was
    /// under, and it is thrown away with the walk. Carried to the vault as-is, B3's fourth room on one rock
    /// would strike off B3's fourth room on every other one.</para>
    ///
    /// <para>A pipe, and not a colon, because a body id is a foreign string and a colon is what
    /// <see cref="LeftBehind.SpotKey"/> already spends. Nothing parses this back — it is compared, never
    /// read — so the only property it needs is that two different rooms never write the same line.</para>
    /// </summary>
    public static string RoomKey(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return $"{bodyId}|{level}|{roomIndex}";
    }

    /// <summary>#615 · Read one stored key back, but ONLY if it belongs to the site the captain is standing
    /// on. The one question the seeding asks of the register — which room on which floor of THIS building has
    /// already been gone through — so the excursion's live set and the vault's durable one can never come to
    /// two views of one room.
    ///
    /// <para>Here rather than at the seeding, because a key written by
    /// <see cref="RoomKey"/> and read by a transcription of it in the client is two functions that agree
    /// until the day one of them is edited. False for anything this build cannot read, which costs one
    /// unrecognised room its strike-off and nothing else.</para></summary>
    public static bool TryReadKey(string key, string bodyId, out int level, out int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(bodyId);

        level = 0;
        roomIndex = 0;
        if (!key.StartsWith(bodyId + "|", StringComparison.Ordinal))
        {
            return false;
        }

        string[] parts = key.Split('|');
        return parts.Length == 3
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out level)
            && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out roomIndex);
    }

    /// <summary>#615 · A find waiting on an answer: everything the KEEP branch needs to finish the pickup the
    /// search verb stopped half way through, and nothing else.
    ///
    /// <para>It is a RECORD OF THE ROOM rather than of the item, deliberately. What goes in the pocket is
    /// re-asked of Core at the moment KEEP is pressed, because between the card going up and the button being
    /// pressed the captain may have opened their satchel and made room — and a <c>Take</c> frozen at find
    /// time would be a capacity answered before the question that changed it.</para></summary>
    /// <param name="BodyId">The site. Part of the vault key, and the thing that tells a card for this
    /// building from a card for another one.</param>
    /// <param name="Level">Which floor, 0 being the surface and negative underground.</param>
    /// <param name="RoomIndex">Which room on it.</param>
    /// <param name="Haul">What the room holds.</param>
    /// <param name="FindId">The durable id the prose is rebuilt from.</param>
    /// <param name="Minted">The authority card the search actually minted, or null. Carried because minting
    /// is seeded off a far-site fallback the second call would not reproduce.</param>
    /// <param name="FarLead">The moon the Key's fallback named, or null — spent only once the pocket agrees
    /// (#684: news can only be heard once).</param>
    /// <param name="RoomLine">The room's own sentence, already composed. Shown on the card so the decision is
    /// made looking at the thing rather than at a menu.</param>
    public readonly record struct Pending(
        string BodyId,
        int Level,
        int RoomIndex,
        UndergroundComplex.Haul Haul,
        string FindId,
        UndergroundComplex.AuthorityCard? Minted,
        string? FarLead,
        string RoomLine);
}
