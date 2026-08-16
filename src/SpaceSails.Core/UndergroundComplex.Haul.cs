using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── WHAT YOU CARRY OUT ──────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "those sites should have good loot of stuff and information also... like dirt on potential
    // contacts ... the works."
    //
    // The second half is the interesting one and it is the reason these places belong in this game rather
    // than in a shooter. A crate of credits is a number going up. A FILE ON SOMEBODY is a thing you can spend
    // on a person — and this game already has the people: the bar contacts, the barkeeps, the harbourmasters'
    // seconds, the families in #588's kits. A records annex under a moon is where you learn that the man who
    // sets the docking fees at The Tilt has a name in a payroll he should not be in.
    //
    // It is left entirely open whether the captain USES it. That is the whole point of leverage.

    /// <summary>#592 · Which room is GUARANTEED to hold the way down, on a site that has something to hide.
    ///
    /// <para>Null on an ordinary site: there is nothing under it, so nothing has to be findable and every
    /// Key stays a roll. On a site with an unlisted band it is a room on the last floor the building admits
    /// to — the floor a captain is standing on when the panel goes quiet, which is exactly where somebody
    /// would have been carrying one.</para>
    ///
    /// <para><b>Room 0, not a seeded index.</b> This function is pure of the field, so it cannot know how
    /// many rooms that floor actually has — and the count varies: the four-room floor law is asserted for
    /// the scenario's own bodies, and a generated site can produce a floor with three. A seeded 0..3 index
    /// therefore misses sometimes, which puts the guarantee back exactly where it started. Room 0 always
    /// exists on any floor worth riding to.</para>
    ///
    /// <para>Nobody can see the index, so nothing is lost by it being fixed — a player finds a room, not a
    /// number — and the alternative costs a floor plan on every haul lookup.</para></summary>
    public static (int Level, int RoomIndex)? KeyRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasUnlistedBand(bodyId) ? (DepthOf(bodyId), 0) : null;
    }

    /// <summary>What a room in one of these places holds.</summary>
    public enum Haul
    {
        /// <summary>Stripped. Load-bearing, as everywhere else on this ground.</summary>
        Nothing,
        /// <summary>Hardware worth money — the "good loot of stuff".</summary>
        Equipment,
        /// <summary>Somebody's file. Leverage on a person the captain can actually go and meet.</summary>
        Dirt,
        /// <summary>Operational paper: a manifest, a route, a schedule. Points somewhere else.</summary>
        Records,
        /// <summary>A way through a door somewhere — a code, a card, a countersigned authority.</summary>
        Key,

        /// <summary>#614 · The thing on the pallet. Exactly one room in a whole facility, and only in the
        /// band nobody listed.</summary>
        Relic,
    }

    /// <summary>#614 · WHERE THE THING ON THE PALLET IS, and why it is not a roll.
    ///
    /// <para>Same reasoning as <see cref="KeyRoomFor"/>, for the same reason: a one-in-N object placed by
    /// seeded dice is an object that is silently absent on some worlds FOREVER, and nothing on screen ever
    /// says so. Every test still passes and the best thing in the game is simply missing from a third of the
    /// universe.</para>
    ///
    /// <para>So it is designated: the deepest floor of the band nobody listed. Sites without an unlisted band
    /// have no relic at all, which is correct — it is the payoff for getting somewhere you were not supposed
    /// to be able to reach, and a facility that admits to its own depth has nowhere to put it.</para>
    ///
    /// <para><b>Room 0.</b> A floor's room count depends on the site's field, so the only index a
    /// field-free designation may safely name is the one every floor has. Room 0 cannot collide with
    /// <see cref="KeyRoomFor"/> either: that one sits on the LISTED bottom, and a site only has a relic when
    /// its true depth runs deeper than the depth it admits to.</para></summary>
    /// <remarks>#677 · <see cref="UnlistedBottomOf"/> and not <c>TrueDepthOf</c>. The thing on the pallet
    /// belongs to the OPERATION — somebody crated it, somebody left the lights on over it — so it sits on the
    /// deepest floor the operation dug. The day something deeper turned out not to have been dug by anybody,
    /// a <c>TrueDepthOf</c> here would have moved the one designated relic in the game two bands down into a
    /// gallery with no lights and no pallets in it, and every test would still have passed.</remarks>
    public static (int Level, int RoomIndex)? RelicRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasUnlistedBand(bodyId) ? (UnlistedBottomOf(bodyId), 0) : null;
    }

    /// <summary>#677 · WHERE THE WAY DOWN TO THE HALLS IS, designated for exactly the reason
    /// <see cref="KeyRoomFor"/> is: a Key is one face in nine, and a seeded band that happened to roll none
    /// would leave a site's halls unreachable not for that visit but forever, with nothing on screen ever
    /// saying so and every test still green.
    ///
    /// <para>It is room 0 of the band nobody listed's own SHAFT HEAD — the floor a captain steps out onto
    /// when the plate finally names a different building (#694). Not its bottom floor, which is already
    /// spoken for by <see cref="RelicRoomFor"/>, and not the listed bottom, which is already
    /// <see cref="KeyRoomFor"/>. Three designations, three floors, no collision.</para>
    ///
    /// <para>Every other Key in that band mints the same card anyway (<see cref="CardInRoom"/> asks
    /// <see cref="NextShaftBelow"/>, which steps over the band of nothing) — this one only guarantees that at
    /// least one exists. The paper telling the truth about a building that is not, one rung further.</para></summary>
    public static (int Level, int RoomIndex)? FoundKeyRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasFoundBand(bodyId) ? (BandTop(UnlistedBandOf(bodyId)), 0) : null;
    }

    /// <summary>
    /// #411 · THE ONE PIECE OF PAPER WORTH CARRYING OUT OF THE HEAD OFFICE, designated for exactly the
    /// reason <see cref="KeyRoomFor"/> and <see cref="RelicRoomFor"/> are: a seeded roll would leave it
    /// silently absent on some threads forever, and nothing on screen would ever say so.
    ///
    /// <para>It is a <see cref="Haul.Records"/> room and not a <see cref="Haul.Relic"/> one, and that is the
    /// honest call rather than the convenient one — the relic's own prose describes a band of alloy on a
    /// pallet, and dressing a sheet of paper in it would be the sim doing one thing while the sentence said
    /// another. Records already goes into the satchel as paper, which is all this needs.</para></summary>
    public static (int Level, int RoomIndex)? StandingOrderRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsHeadOffice(bodyId) ? (StandingOrderLevel, 0) : null;
    }

    /// <summary>What is in this room. Weighted so the place feels stripped but worth walking: about a third
    /// empty, and DIRT is the rarest thing in the building because it is the most valuable.</summary>
    public static Haul InRoom(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #592 · THE ONE ROOM THAT IS NOT A ROLL.
        //
        // The way into the band nobody listed is a card, and a card comes out of a Key room on the last
        // floor the building admits to. Key is one face in nine, and a last band holds thirty-odd rooms, so
        // about one site in thirty would roll no Key at all — and because the rolls are seeded, that site's
        // hidden band would be unreachable NOT for that visit but FOREVER.
        //
        // Nothing on screen would ever say so, which is the only reason this is not the "map lies" bug; it
        // is the quieter one where a feature is silently dead on some worlds and every test still passes.
        // So one room on the last listed floor is designated, deterministically, and holds the way down.
        if (KeyRoomFor(bodyId) is { } wayDown && level == wayDown.Level && roomIndex == wayDown.RoomIndex)
        {
            return Haul.Key;
        }

        // #614 · And the one room that holds the thing nobody signed for. Designated for the same reason as
        // the Key room above — see RelicRoomFor.
        if (RelicRoomFor(bodyId) is { } pallet && level == pallet.Level && roomIndex == pallet.RoomIndex)
        {
            return Haul.Relic;
        }

        // #411 · And the head office's one designated room — the sheet that says the runs were to continue
        // until countermanded, in a folder with nothing else in it.
        if (StandingOrderRoomFor(bodyId) is { } order && level == order.Level && roomIndex == order.RoomIndex)
        {
            return Haul.Records;
        }

        // #677 · And the one room that holds the way down to the halls.
        if (FoundKeyRoomFor(bodyId) is { } hall && level == hall.Level && roomIndex == hall.RoomIndex)
        {
            return Haul.Key;
        }

        // ── #677 · AND THE HALLS, WHERE ALMOST NOTHING IS IN ALMOST EVERY ROOM ────────────────────────────
        //
        // The emptiness is load-bearing everywhere on this ground (§10.3) and down here it is the whole
        // sensation: a place kept ready, and nobody in it. So the roll is not a weighting of the facility's
        // roll — it is a different roll with almost nothing in it.
        //
        // What is deliberately ABSENT and why, because each absence is a canon law rather than a balance
        // call: no EQUIPMENT (nobody procured anything down here, and a crate would name a supplier); no
        // RECORDS and no DIRT (both are paperwork, and paperwork is an institution — a file on somebody in a
        // hall would say who kept it); no KEY (the entry card is the last card, and a second one would make
        // the halls a building with a directory). What is left is what a surveyor could actually carry out
        // of a place like this, which is a MEASUREMENT.
        if (IsFound(bodyId, level))
        {
            return DiceRule.Roll(DiceRule.Seed($"hive:hall-haul:{bodyId}:{level}:{roomIndex}"),
                    FoundRecordOneInN).Face == 1
                ? Haul.Relic
                : Haul.Nothing;
        }

        int face = DiceRule.Roll(DiceRule.Seed($"hive:haul:{bodyId}:{level}:{roomIndex}"), 9).Face;

        // #592 · THE PAYOFF FOR REACHING THE FLOOR NOBODY LISTED IS INFORMATION, NOT A BIGGER NUMBER.
        //
        // The issue is explicit about this and it is the right call: a crate of credits is a number going
        // up, and this game already has the better currency. Down here the rooms are heavy with paper —
        // FILES ON PEOPLE, and the operational record of what was moved and how often — because that is the
        // shape of a secret worth digging a shaft nobody wrote down for.
        //
        // Deliberately NOT more Equipment. If the hidden floor paid in hardware it would be a loot room with
        // a story painted on it, and every captain would end up describing it as "the good level".
        if (IsUnlisted(bodyId, level))
        {
            return face switch
            {
                1 or 2 => Haul.Nothing,       // still stripped. Somebody cleared this too, and in a hurry.
                3 => Haul.Equipment,
                4 or 5 => Haul.Records,
                6 => Haul.Key,
                _ => Haul.Dirt,               // a third of the floor is a file on somebody
            };
        }

        return face switch
        {
            1 or 2 or 3 => Haul.Nothing,
            4 or 5 => Haul.Equipment,
            6 or 7 => Haul.Records,
            8 => Haul.Key,
            _ => Haul.Dirt,
        };
    }

    /// <summary>Whose file it is, and what is in it. The subject is one of the standing roles a captain
    /// actually deals with, so the leverage has somewhere to be spent.</summary>
    public static string DirtOn(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        string[] subjects =
        [
            "the harbourmaster's second at The Tilt",
            "the man who sets the docking fees at Selene Gate",
            "the yard foreman at Highport Satellite Works",
            "the quiet one who drinks alone at The Rusty Roadstead",
            "the clerk who signs the bonded holds at Ringside Exchange",
            "the duty officer at The Deep",
        ];
        string[] findings =
        [
            "is in a payroll here they have no business being in, at a grade they were never qualified for",
            "signed for eleven consignments that the manifest office says never arrived",
            "was paid a settlement by an office that denies existing, and cashed it",
            "appears in the visitor book four times, always after midnight, always alone",
            "countersigned a transfer order for a person whose file is three rooms from here",
            "is listed as next of kin for somebody they have never once mentioned",
        ];

        ulong seed = DiceRule.Seed($"hive:dirt:{bodyId}:{level}:{roomIndex}");
        string who = subjects[(int)(seed % (ulong)subjects.Length)];
        string what = findings[(int)((seed / 7) % (ulong)findings.Length)];
        return $"🗃 A file, and it is not the file you were expecting: {who} {what}. " +
            "Nobody buried this here by accident. You can hold on to it, or you can never mention it. " +
            "Both of those are decisions.";
    }

    /// <summary>The line for the rest of the hauls.
    ///
    /// <para>#678 · <paramref name="minted"/> is the card the caller ACTUALLY handed over for a
    /// <see cref="Haul.Key"/> — null when none was, which happens on the bottom band whenever the client's
    /// far-site fallback comes up empty. It is a required parameter rather than an optional one for the
    /// reason <see cref="NameOf"/> has no site-blind overload: a defaulted "no card" would be a second answer
    /// to "what does this room say", silently wrong at exactly the one call site that matters.</para></summary>
    public static string HaulLine(Haul haul, string bodyId, int level, int roomIndex, AuthorityCard? minted)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // ── #677 · THE HALLS HAVE THEIR OWN TWO ANSWERS AND NO OTHERS ────────────────────────────────────
        //
        // Taken before the switch below rather than as two more arms inside it, because the thing that must
        // never happen is a haul reaching the facility's DEFAULT arm down here: "stripped to the fittings…
        // whoever cleared this room did it carefully and did it in a hurry" is a sentence about STAFF, and
        // there was no staff. A default arm is how that sentence would arrive — silently, on the day
        // somebody adds a Haul value and does not think about a floor nobody built.
        if (IsFound(bodyId, level))
        {
            // A record find says nothing about its room. Everything there is to say is the pocket line and
            // the card, both authored; a sentence invented here to fill the gap would be the one thing this
            // feature forbids.
            return haul == Haul.Relic ? "" : FoundEmptyRoomLine;
        }

        return haul switch
        {
        Haul.Equipment =>
            "🧪 Bench hardware, crated and never unpacked — the good stuff, bought with somebody's grant and " +
            "abandoned with the lights on. It will fetch a great deal from people who will not ask.",
        // #411 · The head office's designated sheet reads as itself; everywhere else, operational paper.
        Haul.Records when StandingOrderRoomFor(bodyId) is { } o && level == o.Level && roomIndex == o.RoomIndex
            => StandingOrderLine,
        Haul.Records =>
            "📋 Operational paper: rosters, routes, a shipping schedule with a column nobody has labelled. It " +
            "does not say what was moved. It says exactly how often, and to where.",
        Haul.Key => KeyLine(bodyId, level, minted),
        Haul.Dirt => DirtOn(bodyId, level, roomIndex),

        // #614 · The room is described. The thing is NOT explained, here or anywhere: the pulse says what is
        // in front of you and the card (CarriedObject.CollarStory) says what it measures, and between them
        // they never once say what it was for. Canon holds hardest exactly here.
        Haul.Relic =>
            "⭕ The room is a bay, and there is one thing in it: a band of dark alloy on a pallet, taller " +
            "than you are and machined inside and out. Nobody stripped this room. They left it, and they " +
            "left the lights on over it.",
        _ =>
            "🚪 Stripped to the fittings. Whoever cleared this room did it carefully and did it in a hurry, " +
            "which are two different things and both of them are here.",
        };
    }

    /// <summary>#677/#603 · What the CASEBOOK keeps out of one room, or null where the pulse line is already
    /// the whole of the record.
    ///
    /// <para>Only the halls answer, and only for a record: looking is free and knowledge is one-shot, so the
    /// book keeps what the captain now KNOWS about a wall rather than the sentence about putting a rubbing in
    /// a pocket. Every other room in the game files its own line and always has.</para></summary>
    public static string? CasebookGistOf(Haul haul, string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return haul == Haul.Relic && IsFound(bodyId, level) ? FoundRecordGist : null;
    }

    /// <summary>What the panel says when this car has gone as deep as it goes. It does not hint, it does not
    /// unlock, and there is no button that was hiding: the building simply continues past what this shaft was
    /// dug to reach, which is the honest reason a facility has more than one lift.</summary>
    public static string EndOfTheLineLine(int floorsDown) =>
        $"🛗 The panel has no button below B{floorsDown}. This car was dug to serve the top of the building " +
        "and nothing else — whatever is under you was reached another way, by somebody with their own shaft " +
        "and their own reasons. It is down here somewhere.";
}
