using System;
using System.Globalization;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #1074/#1063 · WHICH AUTHORED PAPER A FIND IS ─────────────────────────────────────────────────────
    //
    // Five rooms in this building hand over a paper somebody WROTE, and the pocket has never known it. A
    // find goes into the satchel as an ordinary Satchel.Kind.Paper carrying nothing but the id FindId
    // minted, and every seam that names a paper away from its room — the sleeve's row, the free glance,
    // the tracker press, the book's line about what was left behind — rebuilds a seeded generic document
    // off that id. So the rail's invoice opened as a torn shipping manifest.
    //
    // THE BUILDING IS ASKED, BECAUSE THE BUILDING IS WHAT KNOWS. The five are DESIGNATED rooms, not rolls,
    // and the functions that designate them (MaintenanceLedgerRoomFor, ValveBookRoomFor, MoneyTrailPaperIn)
    // are already the single answer to "what is in this room". This asks those same three and never a
    // fourth: a table of ids kept beside them would be the same fact computed twice, which is the bug this
    // file's own spec opens with a table of. What each paper is CALLED is not the building's question and
    // lives in PaperHeads.
    //
    // THE REGISTERS GATE IT AND THEY GATE IT FIRST. Both of the three-way lookups below open on a list check
    // that is empty on almost every world (Burial.IsFilled, StopOrder.On), so on a ground nobody has filled
    // in and nobody has closed — which is every ground in almost every game — the whole of this is a string
    // split and three empty-list walks.

    /// <summary>#1074/#1063 · <b>WHICH AUTHORED PAPER THIS FIND IS</b>, or null for every other paper in the
    /// game — which is almost all of them, and which is what keeps the seeded titles #613 was written for.
    ///
    /// <para>Asked of the ID rather than of a body and a room, for <see cref="IsHallRecord"/>'s reason: the
    /// satchel keeps the id and nothing else, and a row that re-derived a floor's designations from a parsed
    /// level would be a second answer to a question <see cref="FindId"/> already settled the moment the
    /// thing went in the pocket.</para>
    ///
    /// <para><b>The id has to be one this building would actually mint.</b> A designation match alone is not
    /// enough: a record out of the halls wears a different prefix (<see cref="HallFindPrefix"/>) and could
    /// otherwise collide with a listed floor's level and room index, which would title a section of wall
    /// with a plant book. So a match is confirmed by re-minting the id and comparing it, character for
    /// character, with the one that came in — the round trip is the guarantee, and it is only paid on the
    /// five ids in the game that get that far.</para></summary>
    public static PaperHeads.Paper? AuthoredPaperOf(string? findId)
    {
        if (findId is null || RoomOfFind(findId) is not { } at)
        {
            return null;
        }

        PaperHeads.Paper? which = WhatIsKeptIn(at.BodyId, at.Level, at.RoomIndex);
        return which is not null
            && string.Equals(FindId(at.BodyId, at.Level, at.RoomIndex), findId, StringComparison.Ordinal)
                ? which
                : null;
    }

    /// <summary>#1074/#1063 · Which of the five, by room. The three designations are asked in the order the
    /// arc dealt them, and they cannot answer twice: the ledger only ever exists on a ground that was filled
    /// in and the other four only on a ground that was stopped, and a ground is one or the other and never
    /// both (<see cref="StopOrder.TheOfficeGetsThisOne"/>).</summary>
    private static PaperHeads.Paper? WhatIsKeptIn(string bodyId, int level, int roomIndex)
    {
        if (MaintenanceLedgerRoomFor(bodyId) is { } ledger
            && ledger.Level == level && ledger.RoomIndex == roomIndex)
        {
            return PaperHeads.Paper.MaintenanceLedger;
        }

        if (ValveBookRoomFor(bodyId) is { } valves
            && valves.Level == level && valves.RoomIndex == roomIndex)
        {
            return PaperHeads.Paper.ValveBook;
        }

        return MoneyTrailPaperIn(bodyId, level, roomIndex) switch
        {
            MoneyTrail.Item.Pour => PaperHeads.Paper.Pour,
            MoneyTrail.Item.Rail => PaperHeads.Paper.Rail,
            MoneyTrail.Item.Rota => PaperHeads.Paper.Rota,
            _ => null,
        };
    }

    /// <summary>#1074 · The room a find id names, or null where the string is not one of this building's
    /// find ids at all — which includes every id the rest of the game mints, #1061's dropped schedule among
    /// them.
    ///
    /// <para>Split from the RIGHT, because the two numbers are the fixed part: a body id is an arbitrary
    /// string and has never promised not to contain a colon, while the level and the room index are always
    /// the last two fields <see cref="FindId"/> writes. Nothing here trusts its own split — the caller
    /// confirms the answer by re-minting the id — so this only has to be right about where the numbers
    /// are.</para></summary>
    private static (string BodyId, int Level, int RoomIndex)? RoomOfFind(string findId)
    {
        int lastColon = findId.LastIndexOf(':');
        if (lastColon < 0)
        {
            return null;
        }

        int levelColon = findId.LastIndexOf(':', lastColon - 1);
        if (levelColon < 0)
        {
            return null;
        }

        int prefixColon = findId.IndexOf(':', StringComparison.Ordinal);
        if (prefixColon >= levelColon)
        {
            return null;   // nothing between the prefix and the level: no body id, so no room
        }

        if (!int.TryParse(
                findId.AsSpan(levelColon + 1, lastColon - levelColon - 1),
                NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int level)
            || !int.TryParse(
                findId.AsSpan(lastColon + 1),
                NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int roomIndex))
        {
            return null;
        }

        return (findId[(prefixColon + 1)..levelColon], level, roomIndex);
    }
}
