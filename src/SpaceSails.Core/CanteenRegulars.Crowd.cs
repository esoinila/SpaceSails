using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #751 · DEALING THE CROWD THAT IS THE COVER — which bark a patron has this watch, how many tops are
/// occupied, and the deal itself: a face that fits the top it is put at, never used twice in one room.
/// The faces, the barks and the watch's fill are REGISTERS and live in <c>CanteenRegulars.cs</c>; this
/// file only chooses from them. Split out of that file under #251 with no member renamed, re-scoped or
/// re-ordered.
/// </summary>
public static partial class CanteenRegulars
{
    /// <summary>#751 · Which bark this patron has this watch. Seeded on (site, top, watch) and nothing else,
    /// so it is stable while you stand there, different next shift, and a guard can measure that every one
    /// of the fourteen is actually reachable rather than assuming it.</summary>
    public static int BarkIndex(string bodyId, int tableIndex, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return DiceRule.Roll(
            DiceRule.Seed($"hive:hall:bark:{bodyId}:{tableIndex}", watch), Barks.Count).Face - 1;
    }

    /// <summary>#751 · How many of the hall's tops have somebody at them this watch — the named regulars
    /// included. The number the room's whole mood comes out of.</summary>
    public static int OccupiedTops(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch = 0)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int taken = 0;
        foreach (TableSeat top in Tables(bodyId, level, amenity, watch))
        {
            if (top.Taken)
            {
                taken++;
            }
        }
        return taken;
    }

    /// <summary>WHICH TOPS THE CROWD IS AT, and which face each of them wears. Keyed by top ordinal, exactly
    /// like <see cref="Seating"/>, and it skips whatever the named regulars already have.</summary>
    /// <param name="bill">#823 · What each top SEATS, so the deal can respect the furniture. A whole crew of
    /// four cannot be dealt to a two-top: the party would be sitting in each other's laps, which is the exact
    /// picture the owner sent back from the canteen.</param>
    private static Dictionary<int, int> Crowd(
        string bodyId, int level, UndergroundComplex.Amenity amenity, long watch,
        IEnumerable<int> takenByTheCast, IReadOnlyList<int> bill)
    {
        var crowd = new Dictionary<int, int>();

        // Halls only, upper canteen only, top floor only. The middle clause is the one with teeth: the
        // STAFF MESS is hall-class too (#751's second customer) and it must stay empty on every watch
        // forever — its whole identity is #743's sentence at architectural scale, "the shift has not come".
        if (amenity.Hall is null || !PeopleSitHere(bodyId, level, amenity))
        {
            return crowd;
        }

        int tops = amenity.Tables.Count;
        if (tops <= 0)
        {
            return crowd;
        }

        double fill = WatchFill[(int)(((watch % WatchFill.Count) + WatchFill.Count) % WatchFill.Count)];
        int want = (int)Math.Round(tops * fill, MidpointRounding.AwayFromZero);

        var used = new List<int>(takenByTheCast);
        int seatedByCast = used.Count;
        want = Math.Clamp(want - seatedByCast, 0, tops - seatedByCast);

        for (int i = 0; i < want; i++)
        {
            int table = PickUnused(
                DiceRule.Roll(DiceRule.Seed($"hive:hall:top:{bodyId}:{i}", watch), tops).Face - 1,
                tops, used);
            int face = DiceRule.Roll(
                DiceRule.Seed($"hive:hall:face:{bodyId}:{table}", watch), StrangerPlates.Count).Face - 1;

            // #823 · THE FURNITURE HAS A VETO. The die names a face; the top says whether it will take it.
            if (FaceThatFits(face, table < bill.Count ? bill[table] : SeatCounts[0]) is { } fits)
            {
                crowd[table] = fits;
            }
        }

        return crowd;
    }

    /// <summary>
    /// #823 · THE FIRST FACE AT OR AFTER THE ROLLED ONE THAT THIS TOP CAN SEAT, or null if none of them can.
    ///
    /// <para>Same skip-forward discipline as <see cref="PickUnused"/> and the hall's seat bill, and for the
    /// same reason: a re-roll loop on a seeded die is how a generator stops being deterministic. A hall's
    /// twos take the ones and the pairs; the fours and sixes take the crews. Null is the honest answer for a
    /// top nobody could sit at — the caller leaves it empty rather than seating four people on two chairs —
    /// and no top the game builds is that small today (SeatCounts starts at 2 and half the faces are one
    /// person), which is a thing the guard measures rather than a thing this comment promises.</para>
    /// </summary>
    private static int? FaceThatFits(int wanted, int seats)
    {
        for (int step = 0; step < Faces.Length; step++)
        {
            int candidate = ((wanted + step) % Faces.Length + Faces.Length) % Faces.Length;
            if (Faces[candidate].Heads <= seats)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Take the rolled index, or the next free one after it. Two people on one chair and one person
    /// said twice are the same bug wearing different clothes, and a re-roll loop on a seeded die is how a
    /// generator stops being deterministic.</summary>
    private static int PickUnused(int wanted, int count, List<int> used)
    {
        for (int step = 0; step < count; step++)
        {
            int candidate = (wanted + step) % count;
            if (!used.Contains(candidate))
            {
                used.Add(candidate);
                return candidate;
            }
        }

        used.Add(wanted);
        return wanted;
    }
}
