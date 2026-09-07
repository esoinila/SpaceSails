using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #731 · THE ROTA TURNS OVER, AND THE ROOM SHOWS IT — who is coming on this watch, who shows a pass on
/// the way out and how long they hold it up, whether anybody sits in this room at all, and the seating
/// that deals a watch's bodies onto its tops. All of it off the frozen watch and none of it off a
/// register kept anywhere else. Split out of <c>CanteenRegulars.cs</c> under #251 with no member renamed,
/// re-scoped or re-ordered.
/// </summary>
public static partial class CanteenRegulars
{
    // ── #731 · THE ROTA TURNS OVER, AND THE ROOM SHOWS IT ─────────────────────────────────────────────────
    //
    // Issue #731's second customer, in the issue's own words: "The B1 canteen: rota turnover made visible —
    // the agency temp leaving at watch change through the staff door, showing the pass nobody inside asks
    // for."
    //
    // The room has emptied on a schedule since #731 v1 and has never once FILLED. That is half a metabolism,
    // and the half it is missing is the one the board on this room's own wall is about: ROTA — WEEK 31. This
    // class has always known who the next shift puts in here; it simply produced them by rebuilding the room
    // at the stroke of the watch, so the turnover happened in the one frame nobody was looking at.

    /// <summary>
    /// #731 · WHO THE ONCOMING ROTA PUTS IN THIS ROOM THAT THIS WATCH DOES NOT — the people who WALK IN.
    ///
    /// <para>The next watch's own seating, less everybody this watch already seated. Nothing new is rolled:
    /// this is the rota reading one line further down the sheet, which is exactly what a rota is for and what
    /// the noticeboard in this room says it does. A person who is on both shifts is not "arriving" — they are
    /// sitting there — and a person on neither is not in the building.</para>
    ///
    /// <para><b>Deterministic in (site, floor, watch) and nothing else.</b> <see cref="Seating"/> is seeded on
    /// the site and the shift, so this is too, and the same watch names the same oncoming faces in the same
    /// order on every machine forever. It reads no clock: the caller freezes the watch when the floor is drawn
    /// (#709) and hands that number here, exactly as it does to <see cref="Tables"/>.</para>
    ///
    /// <para>Plates, and not cast indices, because a plate is what the walker carries and what the room draws
    /// over their head. Empty for every room that is not the one people sit in — which is the owner's B1
    /// ruling asked of <see cref="PeopleSitHere"/> rather than re-derived.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="amenity">The room, as Core carved it.</param>
    /// <param name="watch">The shift, frozen when the floor was drawn.</param>
    public static IReadOnlyList<string> ComingOnShift(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var accountedFor = new HashSet<int>();
        foreach ((int _, int cast) in Seating(bodyId, level, amenity, watch))
        {
            accountedFor.Add(cast);
        }

        var coming = new List<string>();
        foreach ((int _, int cast) in Seating(bodyId, level, amenity, watch + 1))
        {
            // Add() answers false for somebody this watch already has AND for a face the oncoming sheet
            // happens to name twice, so nobody walks into a room they are already sitting in.
            if (accountedFor.Add(cast))
            {
                coming.Add(Cast[cast].Plate);
            }
        }

        return coming;
    }

    /// <summary>
    /// #731 · <b>THE ONE EXIT THAT IS A GESTURE.</b>
    ///
    /// <para>The issue names the beat and names the person: <i>"the agency temp leaving at watch change
    /// through the staff door, showing the pass nobody inside asks for."</i> They are the one in this cast who
    /// has been here a week, whose name on the rota is not the name they gave at the door
    /// (<c>CanteenBoard</c>'s nights-slot-four notice, and their own single breath), and who therefore still
    /// believes a staff door is a thing you are asked to justify. Everybody else in the room has learned that
    /// nobody is asking.</para>
    ///
    /// <para><b>Nothing here says any of that.</b> The gesture is the whole content: they stop at a leaf the
    /// captain's own TRY is refused at, turn back to a room that does not look up, hold, and go. No line of
    /// dialog explains it, which is law three on this issue and the reason this is a predicate about WHO
    /// rather than a string about why.</para>
    ///
    /// <para>Asked of <see cref="CanteenTable.WhoIs"/> — the one place in this codebase that turns a plate
    /// into one of the named three — so the day the temp's plate is re-worded there is one author of it.</para>
    /// </summary>
    public static bool ShowsThePassOnTheWayOut(string? plate) =>
        CanteenTable.WhoIs(plate) == CanteenTable.Who.Temp;

    /// <summary>#731 · How long the pass is held up to a room that does not answer, in seconds of the same
    /// clock the walk itself is stepped on.
    ///
    /// <para>Three. Long enough that a captain anywhere in the hall can see somebody has stopped at the staff
    /// door and turned round; short enough that it reads as a formality being observed rather than as a body
    /// that has got stuck in a doorway. It is counted in the walker's own <c>dt</c> and never against a wall
    /// clock, so the walk and the pause are on one clock and a frame cannot fall between them.</para></summary>
    public const double PassHeldSeconds = 3.0;

    /// <summary>
    /// #709/#757 · IS THIS THE ROOM PEOPLE ARE IN? The owner's B1 ruling, as a question anybody may ask.
    ///
    /// <para>The upper canteen, on the top pressurised floor, and nowhere else. It was already the first two
    /// clauses of <see cref="Seating"/> and of <see cref="Crowd"/> in longhand; #757 needed a THIRD caller,
    /// because an empty top may only offer <i>take this table</i> in a room outsiders are admitted to — the
    /// pass-only staff mess is hall-class as well, full of tops, and its whole identity is that the shift
    /// has not come and nobody is ever in it. A third private copy of two clauses is how one rule stops
    /// being one rule, so the copies became this.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="amenity">The room, as Core carved it (#707).</param>
    public static bool PeopleSitHere(string bodyId, int level, UndergroundComplex.Amenity amenity)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return amenity.Use == UndergroundComplex.Comfort.UpperCanteen
            && UndergroundComplex.TopPressurisedFloor(bodyId) == level;
    }

    /// <summary>WHO IS AT WHICH TABLE, as indices — the one rota, called by <see cref="Sitting"/> and by
    /// <see cref="Tables"/>. It was inlined in Sitting until #746 needed the table's ORDINAL as well as its
    /// coordinates; matching a person back to a top by comparing two doubles would have been a second answer
    /// to "who is sitting where", which is the thing this class's own docs warn about hardest.</summary>
    private static List<(int Table, int Who)> Seating(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var seating = new List<(int Table, int Who)>();

        // The washroom and the deep staff mess get nobody. The mess is pass-only and the people who would be
        // in it are #618's question, not this one; the washroom is the one amenity nobody sits down in. And
        // only on the floor the owner put them on — #757 lifted both clauses into PeopleSitHere so that the
        // third caller could not become a second opinion.
        if (!PeopleSitHere(bodyId, level, amenity))
        {
            return seating;
        }

        int seats = Math.Min(amenity.Tables.Count, MostAtOnce);
        if (seats <= 0)
        {
            return seating;
        }

        // How many turned up THIS SHIFT. At least one — the owner asked for people in the bar, and an empty
        // canteen is a thing this building already has twenty floors of.
        ulong seed = DiceRule.Seed($"hive:canteen:{bodyId}", watch);
        int here = DiceRule.Roll(seed, seats).Face;

        // #1074 beat 4 · …AND A CLOSED WORKING PUTS ITS SHIFT IN HERE. On a ground the Authority has stopped,
        // at least two of the tops are taken — the plainest reading of what an order does to a rota, and the
        // room's own answer to it: the working is shut, the shift is still rostered, and the canteen is the
        // room they are in. It is the SAME dice in the SAME order (the i = 1 draw a busier watch would have
        // made anyway), raised and never re-rolled, and it is asked ONLY of a stopped ground — so no world
        // anybody has not stopped seats one extra person, and none of them changes by one character.
        //
        // It is here rather than in the deal below because the beat needs BOTH of its people at once: the
        // colleague to be asked and, behind the next chair, the mug. One of the two would be half a beat, and
        // which half would depend on a die.
        if (StopOrder.On(bodyId))
        {
            here = Math.Max(here, Math.Min(2, seats));
        }

        var usedTables = new List<int>(here);
        var usedCast = new List<int>(here);

        for (int i = 0; i < here; i++)
        {
            int table = PickUnused(
                DiceRule.Roll(DiceRule.Seed($"hive:canteen:table:{bodyId}:{i}", watch), amenity.Tables.Count).Face - 1,
                amenity.Tables.Count, usedTables);
            int cast = PickUnused(
                DiceRule.Roll(DiceRule.Seed($"hive:canteen:who:{bodyId}:{i}", watch), OrdinaryCast).Face - 1,
                OrdinaryCast, usedCast);

            seating.Add((table, cast));
        }

        // #1063 · …and where the works are on, the mason has the first of those chairs. He REPLACES whoever
        // the shift dealt into it rather than being added beside them, for the reason the board's own notice
        // takes a slot rather than making a fifth: a room that grew a person while a captain was away is the
        // room saying that something happened, and the whole beat is that nothing did. One mason, one chair,
        // every watch, deterministically — and on every ground nobody has opened, this does nothing at all.
        //
        // #1074 beat 4 · …UNLESS THE OFFICE GOT HERE FIRST, in which case those chairs are the career-cost
        // pair's and the mason is not in the room at all. The two beats are one trigger's two outcomes and a
        // ground gets exactly one of them (StopOrder.TheOfficeGetsThisOne), so a stopped ground is never a
        // buried one — and the man whose whole testimony is a resurfacing job has no job here: beat 1 already
        // took his notice down off the board for that reason ("services isolated per order", so nobody is
        // going to do it). One arm or the other, never both, and on an ordinary ground neither.
        if (seating.Count > 0)
        {
            if (StopOrder.On(bodyId))
            {
                seating[0] = (seating[0].Table, Colleague);
                if (seating.Count > 1)
                {
                    seating[1] = (seating[1].Table, MugKeeper);
                }
            }
            else if (Burial.WorksAreOn(bodyId))
            {
                seating[0] = (seating[0].Table, Mason);
            }
        }

        return seating;
    }
}
