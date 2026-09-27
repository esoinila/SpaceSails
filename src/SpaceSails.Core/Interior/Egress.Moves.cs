using System;
using System.Collections.Generic;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #251 · THE COMINGS AND GOINGS — the second <c>Departures</c> overload, arrivals, the deal under both, and
/// the arrival door.
///
/// <para>Split out of <c>Egress.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class Egress
{
    /// <summary>
    /// …AND THE SAME QUESTION ASKED OF A ROOM THAT IS NOT A CANTEEN. The overload above is a projection onto
    /// this one, so the Hive's hall and a docked station's bar deal their shifts with one arithmetic and one
    /// set of seeds rather than with two that agree today.
    /// </summary>
    /// <param name="seated">Who is in the room, in the room's own order.</param>
    public static IReadOnlyList<Move> Departures(
        string bodyId,
        int level,
        long watch,
        IReadOnlyList<Occupant> seated,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked) =>
        Deal(bodyId, level, watch, seated, locked, "goes", LeaversPerWatch, "");

    /// <summary>
    /// #731 · WHO TURNS UP, THIS WATCH — the other half of the same schedule, and the owner's own words for
    /// why it exists: <i>"also just other customers arriving and leaving in the bars already does a lot… they
    /// can go behind doors that are locked to us."</i>
    ///
    /// <para>Identical machinery to <see cref="Departures"/> — one seeded roll against
    /// <see cref="ComersPerWatch"/>, a moment inside the first <see cref="LastCallFraction"/> of the shift, and
    /// a door out of the locked list — run over the people the room does NOT currently have. The door is the
    /// point: somebody comes OUT of a leaf the captain's own TRY is refused at, crosses the floor on real legs
    /// and takes a chair, and no line explains how they were behind it. That is the cold open the full stop is
    /// the mirror of, and both are the same walker.</para>
    ///
    /// <para>The salt is different from the departure's, so a room does not send the same person out and bring
    /// them in on one roll; and the door is dealt off <c>in:</c> plus their plate, so the leaf somebody comes
    /// out of and the leaf they would leave by are two independent facts about one evening.</para>
    /// </summary>
    /// <param name="expected">Who is not in the room, each carrying the place they would take if they came —
    /// the caller's to allot, because a free chair is a fact about a room and not about a schedule.</param>
    public static IReadOnlyList<Move> Arrivals(
        string bodyId,
        int level,
        long watch,
        IReadOnlyList<Occupant> expected,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked) =>
        Deal(bodyId, level, watch, expected, locked, "comes", ComersPerWatch, "in:");

    /// <summary>
    /// THE DEAL ITSELF, ONCE — the arithmetic both directions and both rooms spend.
    ///
    /// <para>One pass over the room's own people in the room's own order. Each gets one seeded roll against
    /// <paramref name="share"/>; the ones that clear it are dealt a moment inside the first
    /// <see cref="LastCallFraction"/> of the shift and a door out of the locked list. Returned in the order
    /// they HAPPEN rather than in the room's order, because a caller stepping down the list as the watch runs
    /// wants the next one at the front, and sorting it here means nobody sorts it twice. At most
    /// <see cref="MostAtOnce"/> survive the cut, for the reason written on that constant.</para>
    /// </summary>
    /// <param name="salt">Which half of the schedule this is — folded into the seed so the two halves are
    /// independent rolls about one person on one watch.</param>
    /// <param name="doorPrefix">…and the same for the door, so somebody's way in and their way out are not
    /// forced to be the same leaf by an accident of seeding.</param>
    private static IReadOnlyList<Move> Deal(
        string bodyId,
        int level,
        long watch,
        IReadOnlyList<Occupant> people,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked,
        string salt,
        double share,
        string doorPrefix)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(locked);

        var dealt = new List<Move>();
        if (locked.Count == 0)
        {
            return dealt;
        }

        foreach (Occupant who in people)
        {
            if (who.Plate is not { Length: > 0 } plate)
            {
                continue;
            }

            ulong seed = DiceRule.Seed($"hive:egress:{salt}:{bodyId}:{level}:{who.Index}", watch);
            var roll = new DeterministicRandom(seed);
            if (roll.NextDouble() >= share)
            {
                continue;
            }

            double at = roll.NextDouble() * LastCallFraction * PatronRota.WatchSeconds;
            dealt.Add(new Move(
                plate, who.Index, at, DoorFor(bodyId, level, watch, doorPrefix + plate, locked)));
        }

        dealt.Sort(static (a, b) => a.AtSecondsIntoWatch.CompareTo(b.AtSecondsIntoWatch));
        if (dealt.Count > MostAtOnce)
        {
            dealt.RemoveRange(MostAtOnce, dealt.Count - MostAtOnce);
        }
        return dealt;
    }

    /// <summary>
    /// …AND WHICH DOOR THE ONE WHO COMES TO YOUR TABLE COMES OUT OF.
    ///
    /// <para><b>Owner:</b> <i>"we can have npcs arrive at bar from locked place… Now it is possible to have
    /// NPC ask to sit down at our table and offer a quest! This is the classic TTRPG event."</i></para>
    ///
    /// <para>The arrival is TRIGGERED rather than scheduled — <c>SittingAlone.SomebodyComes</c> already owns
    /// whether anybody comes, and moving that decision would be a second opinion about one fact. What is
    /// scheduled is the PROVENANCE: which door in the building she was behind before she was at your elbow,
    /// frozen per (site, floor, watch, top) so the same shift always produces the same one. That is the whole
    /// cold open — a door that has never opened for the captain opens for somebody, and no line explains
    /// it.</para>
    /// </summary>
    /// <returns>An index into <paramref name="locked"/>, or −1 on a floor with no such door — in which case
    /// the caller has no provenance to offer and must not invent one.</returns>
    public static int ArrivalDoor(
        string bodyId, int level, long watch, int tableIndex,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked) =>
        DoorFor(bodyId, level, watch, $"arrival:{tableIndex}", locked);
}
