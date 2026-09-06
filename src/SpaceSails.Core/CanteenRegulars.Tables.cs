using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #746 · HOW MANY TOPS A ROOM GETS, AND WHERE THEY STAND — the seat bill a body's amenity is dealt, how
/// many chairs the nth top of it carries, and the laying-out itself: every top in an amenity, in order,
/// with the cabinets accounted for. The room's furniture plan, decided once so the floor the renderer
/// draws and the floor the walkers cross are the same floor. Split out of <c>CanteenRegulars.cs</c> under
/// #251 with no member renamed, re-scoped or re-ordered.
/// </summary>
public static partial class CanteenRegulars
{
    /// <summary>
    /// #746/#751 · HOW MANY EACH ROUND TOP IN THIS ROOM SEATS — the one place the question is answered.
    ///
    /// <para>Two rooms, two laws, and they meet here so that nothing downstream has to know which it is
    /// looking at:</para>
    ///
    /// <list type="bullet">
    /// <item><b>An ordinary three-top canteen</b> rolls each top, seeded off the SITE, the room's use and
    /// the top's ordinal — and deliberately NOT off the watch. A canteen does not re-furnish itself every
    /// shift, and a table that seated six at breakfast and two at supper would be the picture and the sim
    /// disagreeing about a thing the player can count.</item>
    /// <item><b>A hall</b> reads <see cref="UndergroundComplex.HallSeatBill"/>, because a hall has a SEAT
    /// TARGET to hit and twenty independent rolls miss it by seven on average (see that method's docs). The
    /// caterer's stock is designed; only its arrangement is seeded.</item>
    /// </list>
    /// </summary>
    public static IReadOnlyList<int> SeatBill(string bodyId, UndergroundComplex.Amenity amenity)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (amenity.Hall is { } hall)
        {
            return UndergroundComplex.HallSeatBill(bodyId, amenity.Use, hall.SeatTarget);
        }

        var rolled = new List<int>(amenity.Tables.Count);
        for (int i = 0; i < amenity.Tables.Count; i++)
        {
            ulong seed = DiceRule.Seed(
                $"hive:canteen:seats:{bodyId}:{(int)amenity.Use}:{i}", 0);
            rolled.Add(SeatCounts[DiceRule.Roll(seed, SeatCounts.Count).Face - 1]);
        }
        return rolled;
    }

    /// <summary>#746 · How many a given round top seats. <see cref="SeatBill"/>'s entry for it, and kept as
    /// its own call because that is how the rest of the game asks.</summary>
    public static int SeatsAt(string bodyId, UndergroundComplex.Amenity amenity, int tableIndex)
    {
        IReadOnlyList<int> bill = SeatBill(bodyId, amenity);
        return tableIndex >= 0 && tableIndex < bill.Count ? bill[tableIndex] : SeatCounts[0];
    }

    /// <summary>
    /// #746/#751 · EVERY ROUND TOP IN THE ROOM, with its seats and its occupancy — the one fact the
    /// renderer draws and the one fact the [E] press asks. Same frozen watch (#709), so the chair on the
    /// screen and the chair the game offers you are the same chair.
    ///
    /// <para>#751 · Three tiers come out of this one call, in one list, because they are one question:
    /// the ten NAMED REGULARS keep their tables and their whole scene; BACKGROUND PATRONS fill whatever the
    /// watch says the hall is holding and are pure data (a plate, a bark, a chair — no pathing, nothing per
    /// frame); and the CABINET tops come last, empty, because #731's walkers are the ones who will sit in
    /// them.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="amenity">The room, as Core carved it (#707/#751).</param>
    /// <param name="watch">The shift, frozen when the floor was drawn.</param>
    /// <param name="stoodUp">
    /// #731 · WHO HAS ALREADY GOT UP AND WALKED OFF, by table ordinal.
    ///
    /// <para>Owner, 2026-08-06: <i>"on the bar now they have to wait for us to leave before they can sit
    /// up… or leave the bar."</i> A walker (<see cref="Interior.NpcWalk"/>) crossing the room on real legs
    /// is one body, and a body cannot be in two places — so the moment somebody stands, their chair has to
    /// come back empty HERE, in the one function that answers who is sitting where, or the drawn room and
    /// the pressed room disagree about a person who is visibly walking past both of them. That is this
    /// repo's third named bug class and it would arrive wearing the feature that caused it.</para>
    ///
    /// <para>It is deliberately NOT a second rota and it decides nothing: the shift still deals who was
    /// there, and this only says which of them is no longer in the chair. Excursion-scoped and watch-scoped
    /// in the caller, like every other thing the Hive remembers about a shift.</para>
    /// </param>
    /// <param name="cameIn">
    /// #731 · …AND WHO HAS WALKED IN OFF THE ONCOMING ROTA, by the top they took.
    ///
    /// <para>The mirror of <paramref name="stoodUp"/>, and it exists for the mirror reason. A room whose
    /// schedule only ever DRAINS is not a room with a metabolism, it is a room being evacuated slowly; the
    /// same watch that decides who finishes decides who turns up (<see cref="ComingOnShift"/>), and somebody
    /// who has crossed the floor on real legs and sat down must be drawn in that chair by the one function
    /// that answers who is in which chair — or the room the player looks at and the room [E] asks about are
    /// two rooms again.</para>
    ///
    /// <para>It is not a second rota either: the shift still says who was dealt this watch, and this only
    /// says which of the ONCOMING shift got here early and where. Read BEFORE
    /// <paramref name="stoodUp"/> so that a top somebody vacated and somebody else took reads as taken —
    /// one chair, one person, whichever of the two moved last.</para>
    /// </param>
    public static IReadOnlyList<TableSeat> Tables(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch = 0,
        IReadOnlySet<int>? stoodUp = null,
        IReadOnlyDictionary<int, string>? cameIn = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var who = new Dictionary<int, int>();
        foreach ((int table, int cast) in Seating(bodyId, level, amenity, watch))
        {
            who[table] = cast;
        }

        IReadOnlyList<int> bill = SeatBill(bodyId, amenity);
        Dictionary<int, int> crowd = Crowd(bodyId, level, amenity, watch, who.Keys, bill);

        var tops = new List<TableSeat>(amenity.Tables.Count + CabinetRoom(amenity));
        for (int i = 0; i < amenity.Tables.Count; i++)
        {
            (double tx, double ty) = amenity.Tables[i];
            int seats = i < bill.Count ? bill[i] : SeatCounts[0];

            if (cameIn?.TryGetValue(i, out string? walkedIn) == true && ByPlate(walkedIn) is { } newcomer)
            {
                // #731 · Somebody came out of a leaf that does not open for the captain, crossed the floor and
                // sat here. They are one of the ten and they are one person, exactly like a regular the shift
                // dealt — the only thing different about them is that the player watched them arrive.
                tops.Add(new TableSeat(
                    i, tx, ty, seats, newcomer.Plate, newcomer.Line, Heads: RegularHeads));
            }
            else if (stoodUp?.Contains(i) == true)
            {
                // They stood up. The top is a top with nobody at it, which is exactly what it is.
                tops.Add(new TableSeat(i, tx, ty, seats, null, null));
            }
            else if (who.TryGetValue(i, out int cast))
            {
                // #792 · A named regular is ONE PERSON at a top, every one of the ten, which is the whole
                // premise of #757's ask-to-join: there is a chair, and somebody to ask. So they are never
                // Talking, and the deck may draw them as the approachable thing they are.
                tops.Add(new TableSeat(
                    i, tx, ty, seats, Cast[cast].Plate, Cast[cast].Line, Heads: RegularHeads));
            }
            else if (crowd.TryGetValue(i, out int face))
            {
                tops.Add(new TableSeat(
                    i, tx, ty, seats,
                    StrangerPlates[face % StrangerPlates.Count],
                    Barks[BarkIndex(bodyId, i, watch)],
                    Stranger: true,
                    Talking: StrangerTalks(face % StrangerPlates.Count),
                    // #823 · …and the party's size, off the same one authored row. Crowd() has already
                    // refused to deal a face this top cannot seat, so this arrives fitting.
                    Heads: StrangerHeads(face % StrangerPlates.Count)));
            }
            else
            {
                tops.Add(new TableSeat(i, tx, ty, seats, null, null));
            }
        }

        // …and the cabinets, which are tops in this room like any other and are simply nobody's this watch.
        if (amenity.Hall is { } hall)
        {
            foreach (UndergroundComplex.Cabinet cabinet in hall.Cabinets)
            {
                tops.Add(new TableSeat(
                    tops.Count, cabinet.Table.X, cabinet.Table.Y, UndergroundComplex.CabinetSeats,
                    null, null, Cabinet: cabinet.Number));
            }
        }

        return tops;
    }

    /// <summary>How many cabinet tops this room adds, for the list's capacity.</summary>
    private static int CabinetRoom(UndergroundComplex.Amenity amenity) =>
        amenity.Hall?.Cabinets.Count ?? 0;
}
