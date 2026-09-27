using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · WHOSE ROOM IS THIS, AND WHICH FREETIME SHELF IS IN IT — the post a plate names, whether a room is
/// occupied at all, and the per-floor deal that hands each room its freetime shelf.
///
/// <para>Split out of <c>Shelves.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered. <c>Work</c> and <c>Freetime</c> — the class's two initialised statics — stay
/// in the opening file in their original order (#1163).</para>
/// </summary>
public static partial class Shelves
{
    // ── WHOSE ROOM IS THIS ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #701 · IS THIS A ROOM WHOSE PLATE IS ABOUT WHO COMES THROUGH THE DOOR?
    ///
    /// <para>The audit's own finding, said in a predicate. <b>The Hive has no SECURITY department and no
    /// guard room</b> — the departments are the eight in <see cref="UndergroundComplex.Departments"/> and
    /// none of them is one, and the twenty-four head-office plates name no post either. What the building
    /// does have, in four of its six door registers, is the plate that ADMITS: <c>DO NOT ADMIT
    /// UNESCORTED</c>, <c>AUDIT — NO ADMITTANCE</c>, <c>CONTINUITY — AUTHORISED ONLY</c>, <c>OUTBOUND —
    /// AUTHORISED ONLY</c>. Those are the only rooms in this building whose sign is about a PERSON standing
    /// at the door rather than about the work behind it, and the guard's shelf is the one that belongs in
    /// them. A patrol manual nobody opened, in the room whose whole plate is a refusal.</para>
    ///
    /// <para>Asked of the plate and never of the department, so it survives the band nobody listed — where
    /// there is no department at all and the plate is the only thing in the building still talking.</para>
    /// </summary>
    public static bool IsAPost(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return plate.Contains("ADMIT", StringComparison.Ordinal)
            || plate.Contains("ADMITTANCE", StringComparison.Ordinal)
            || plate.Contains("AUTHORISED ONLY", StringComparison.Ordinal);
    }

    /// <summary>#701 · Is this the one room the department that reads everything was actually given? The
    /// block's own park-view register carries it (<c>PRIVILEGED RECORDS · READING ROOM</c>,
    /// <see cref="UndergroundComplex.ParkViewPlates"/>) and nothing else in the game does — so the reading
    /// room is a rare room with a window, which is exactly the rank #813's gradient gives it.</summary>
    public static bool IsAReadingRoom(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return plate.Contains("READING ROOM", StringComparison.Ordinal);
    }

    /// <summary>
    /// #701 · WHOSE ROOM THIS IS — the plate first, then the trade the furniture is already dealt by.
    ///
    /// <para>The same ladder <see cref="ChamberFitting.KitFor"/> walks, asked one question further along,
    /// and it asks THROUGH that method rather than beside it: a room's shelves and a room's benches read one
    /// sentence about what is done in it, so a floor cannot grow a clinic's books over a laboratory's fume
    /// hood. The two rungs this file adds above it are the two the kit ladder has no opinion about, because
    /// neither changes what furniture a room gets: a reading room is an office with a different library, and
    /// a door post is whatever room it is guarding the door of.</para>
    ///
    /// <para><see cref="ChamberFitting.Kit.Trade"/> — the UNMARKED floors — falls through to the SITE's own
    /// register, because UNMARKED is a floor whose department nobody wrote down and not a floor whose work
    /// nobody does. A laboratory's unmarked floor is still full of laboratory people.</para>
    /// </summary>
    public static Post PostIn(string plate, string? department, UndergroundComplex.Kind kind)
    {
        ArgumentNullException.ThrowIfNull(plate);

        if (plate.Length == 0 || ChamberFitting.IsEmptyStore(plate))
        {
            return Post.None;
        }
        if (IsAReadingRoom(plate))
        {
            return Post.ReadingRoom;
        }
        if (IsAPost(plate))
        {
            return Post.Security;
        }

        return ChamberFitting.KitFor(plate, department, kind) switch
        {
            ChamberFitting.Kit.Laboratory => Post.Lab,
            ChamberFitting.Kit.Clinic => Post.Clinic,
            ChamberFitting.Kit.Office => Post.Records,
            ChamberFitting.Kit.Plant => Post.Engineering,
            ChamberFitting.Kit.Trade => TradeOf(kind),
            // Store and None: stock and nothing. Nobody was given this room.
            _ => Post.None,
        };
    }

    /// <summary>The site's own trade, for a room on a floor whose department nobody wrote down. A transit
    /// station's people file manifests, a depot's grade people on paper and the head office is paper all the
    /// way down — all three are clerks, and saying so is more honest than inventing a fourth answer.</summary>
    private static Post TradeOf(UndergroundComplex.Kind kind) => kind switch
    {
        UndergroundComplex.Kind.Laboratory => Post.Lab,
        UndergroundComplex.Kind.BlackClinic => Post.Clinic,
        _ => Post.Records,
    };

    /// <summary>
    /// #701 · IS THIS ROOM SOMEBODY'S? The occupancy rule, in one call, so the placer, the renderer and
    /// every guard read one sentence. See the class summary for the four clauses and why each is there.
    /// </summary>
    public static bool Occupied(
        in UndergroundComplex.Room room, string? department, UndergroundComplex.Kind kind) =>
        (room.Kind == UndergroundComplex.RoomKind.Chamber || IsAReadingRoom(room.Plate))
        && PostIn(room.Plate, department, kind) != Post.None;

    // ── WHICH FREETIME SHELF · dealt per floor, never rolled per room ─────────────────────────────────

    /// <summary>
    /// #701 · THE DEAL — which persona each occupied room on this floor gets.
    ///
    /// <para>The catalog asks for <i>never two alike on one floor</i>, and a floor has far more than seven
    /// occupied rooms in it, so the literal form of that is arithmetically impossible. The honest form is a
    /// DEAL: the seven are shuffled and handed out in order, and only when all seven are gone are they
    /// shuffled again. So no persona is ever repeated until every one of them has been seen, and no two
    /// rooms in a row ever share one — which is the thing the ask was protecting, because two identical
    /// shelves you can see from one another is the moment the layer stops saying anything about people.</para>
    ///
    /// <para>Seeded per (site, floor, cycle), so a floor deals the same hand on every visit. The first card
    /// of a cycle is swapped with the second where it would repeat the last card of the cycle before it —
    /// deterministically, and it is the only fixup in this file.</para>
    /// </summary>
    /// <param name="bodyId">The moon this building is under.</param>
    /// <param name="level">Which floor.</param>
    /// <param name="cycle">Which pass through the seven this is — the occupied room's ordinal divided by
    /// <see cref="Personas"/>.</param>
    public static IReadOnlyList<int> DealOn(string bodyId, int level, int cycle)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        int[] order = Shuffle(bodyId, level, cycle);
        if (cycle > 0 && order.Length > 1)
        {
            int last = Shuffle(bodyId, level, cycle - 1)[^1];
            if (order[0] == last)
            {
                (order[0], order[1]) = (order[1], order[0]);
            }
        }
        return order;
    }

    private static int[] Shuffle(string bodyId, int level, int cycle)
    {
        var order = new int[Personas];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = DiceRule.Roll(
                DiceRule.Seed($"hive:shelf-deal:{bodyId}:{level}:{cycle}:{i}"), i + 1).Face - 1;
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    /// <summary>#701 · Which persona the occupied room at this ORDINAL gets — the deal, read at one
    /// position. The ordinal is the room's place among the floor's occupied rooms and never its published
    /// index: a floor whose third chamber is a storeroom must not leave a gap in the hand.</summary>
    public static Entry FreetimeShelf(string bodyId, int level, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        return Freetime[DealOn(bodyId, level, ordinal / Personas)[ordinal % Personas]];
    }
}
