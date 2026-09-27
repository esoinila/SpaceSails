using System;
using System.Collections.Generic;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #251 · WHO IS IN THE ROOM — the seated, the scheduled, and the marks a patter leaves.
///
/// <para>Split out of <c>Egress.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class Egress
{
    /// <summary>
    /// WHO A CANTEEN HALL ACTUALLY HAS IN IT, as the arithmetic needs them — the projection from the Hive's
    /// own furniture onto <see cref="Occupant"/>, written once.
    ///
    /// <para>Cabinets are skipped (nobody is in one) and so is any top the room did not seat. The
    /// <see cref="Occupant.Index"/> is the TOP'S OWN ORDINAL and never its position in this list: every seeded
    /// roll in a hall is keyed on it, and handing on a list position instead re-keys the whole room the moment
    /// one top is empty. That slip is silent, and <c>THE_PROJECTION_NamesTheTopsTheRoomActuallySeated</c> is
    /// what catches it.</para>
    ///
    /// <para>#1061 · Published rather than kept inside <see cref="Departures"/> because two questions are now
    /// asked of one reading of a room: who finishes and goes, and whose table a man working the room stops at.
    /// Two projections that agree today is this repository's oldest bug class with a salesman walking through
    /// it.</para>
    ///
    /// <para><b>This one is BODIES IN CHAIRS, and the crowd is in chairs.</b> The narrower reading the
    /// schedule wants is <see cref="OnTheSchedule"/>, one clause further down — see the note on it for why
    /// the difference is written as a second NAME rather than as a flag on this one.</para>
    /// </summary>
    public static IReadOnlyList<Occupant> Seated(IReadOnlyList<CanteenRegulars.TableSeat> tops)
    {
        ArgumentNullException.ThrowIfNull(tops);

        var seated = new List<Occupant>(tops.Count);
        foreach (CanteenRegulars.TableSeat top in tops)
        {
            // Cabinets are skipped — nobody is in one — and so is any top the room did not seat.
            if (top is { Taken: true, Quiet: false, Plate: { Length: > 0 } plate })
            {
                seated.Add(new Occupant(top.Index, plate));
            }
        }

        return seated;
    }

    /// <summary>
    /// #731 (B1 canteen) · …AND THE ONES THE SHIFT MAY GIVE LEGS TO. <see cref="Seated"/> less the crowd,
    /// <b>because the crowd is DATA.</b>
    ///
    /// <para>#751 wrote that law on the day it built them: <i>"A background patron is a plate, a bark and a
    /// chair. No pathing, no schedule, no per-frame anything… WASM perf is a law here and this is how it is
    /// kept: by there being nothing to run."</i> The projection was feeding them into the schedule anyway, and
    /// the cost was not only the frames: a hall holds a dozen background tops against the rota's three, so the
    /// two departures a shift deals were the crowd's four times out of five and the ROTA'S OWN TURNOVER — the
    /// thing this room's noticeboard is about, and the thing #731 names as this floor's whole customer — was
    /// invisible. Measured before the clause: 84 named departures against 332 the crowd took, and the agency
    /// temp got out of the room three times in two hundred and forty shifts.</para>
    ///
    /// <h3>#1061 · Why this is a second NAME and not a flag on <see cref="Seated"/></h3>
    ///
    /// <para>Because the two questions asked of this room want two different readings of it, and the
    /// difference between them is a LAW rather than a caller's preference — so it is said out loud in a name
    /// instead of hidden in a <c>bool</c> somebody has to remember to pass.</para>
    ///
    /// <para><b>A schedule</b> asks who may be given legs, and the crowd may not: standing one of them up
    /// would be pathing, a schedule and per-frame anything, which is exactly the three things #751 says they
    /// do not have. <b>A salesman's round</b> (<see cref="Marks"/>) asks who is sitting there to be stood
    /// beside, and a background patron plainly is — the crowd is precisely who an insurance man sells to, and
    /// a Fess who walked past a dozen people eating to reach the one named yard hand would be working a rota
    /// rather than a room. Stopping at their table gives them nothing to run: HE has the legs, they keep
    /// their plate, their bark and their chair, and nothing on their side gains a frame's work. #751's law is
    /// untouched by a man standing next to it.</para>
    /// </summary>
    public static IReadOnlyList<Occupant> OnTheSchedule(IReadOnlyList<CanteenRegulars.TableSeat> tops)
    {
        ArgumentNullException.ThrowIfNull(tops);

        // The crowd, by the room's own ordinals — collected first so this method is Seated's answer with one
        // clause over it, and never a second opinion about what "in a chair" means.
        var crowd = new HashSet<int>();
        foreach (CanteenRegulars.TableSeat top in tops)
        {
            if (top.Stranger)
            {
                crowd.Add(top.Index);
            }
        }

        if (crowd.Count == 0)
        {
            return Seated(tops);
        }

        var named = new List<Occupant>(tops.Count);
        foreach (Occupant who in Seated(tops))
        {
            if (!crowd.Contains(who.Index))
            {
                named.Add(who);
            }
        }

        return named;
    }

    /// <summary>
    /// #1061 · <b>WHOSE TABLES SOMEBODY WORKING THIS ROOM STOPS AT, IN WHAT ORDER, AND FOR HOW LONG.</b>
    ///
    /// <para>The third question asked of one shift, and deliberately the same arithmetic as the other two:
    /// pure and deterministic in (site, floor, watch, who), off <see cref="DiceRule.Seed"/> and
    /// <see cref="DeterministicRandom"/>, over the room's own people in the room's own order. No wall clock,
    /// no <c>System.Random</c> — the frozen-watch law (#709), so a captain who reloads a save watches the same
    /// man work the same tables in the same order for the same lengths of time.</para>
    ///
    /// <para><b>It is a shuffle and not a roll</b>, and that is the difference between this and
    /// <see cref="Departures"/>. Who LEAVES is a rarity: most people finish their drink and stay. Who a
    /// salesman stops at is not — he stops at everybody he has time for, and the only questions are which of
    /// them and in what order. A per-person roll here would produce empty rounds on a third of watches, and an
    /// empty round is a beat that mostly does not happen, which is the same as not having built it.</para>
    ///
    /// <para>At most <see cref="MostMarks"/> stops, so his shift ENDS. Nothing is said at any of them.</para>
    /// </summary>
    /// <param name="seated">Who is in the room, in the room's own order — a canteen hall's through
    /// <see cref="Seated"/>, a bar's off its own rota.</param>
    /// <param name="who">Whose round this is, folded into the seed so two people working one room do not
    /// walk the same line.</param>
    public static IReadOnlyList<Patter> Marks(
        string bodyId,
        int level,
        long watch,
        string who,
        IReadOnlyList<Occupant> seated)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(who);
        ArgumentNullException.ThrowIfNull(seated);

        var pool = new List<Occupant>(seated.Count);
        foreach (Occupant one in seated)
        {
            if (one.Plate is { Length: > 0 })
            {
                pool.Add(one);
            }
        }

        if (pool.Count == 0)
        {
            return [];
        }

        var roll = new DeterministicRandom(DiceRule.Seed($"hive:egress:works:{bodyId}:{level}:{who}", watch));

        // The room's own order, shuffled — so which tables he works, and the order he works them in, are one
        // frozen fact about this watch rather than "the first three the room happens to list".
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Math.Min(i, (int)(roll.NextDouble() * (i + 1)));
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int stops = Math.Min(MostMarks, pool.Count);
        var round = new List<Patter>(stops);
        for (int i = 0; i < stops; i++)
        {
            double beat = ShortestPatterSeconds
                          + (roll.NextDouble() * (LongestPatterSeconds - ShortestPatterSeconds));
            round.Add(new Patter(pool[i].Index, pool[i].Plate, beat));
        }

        return round;
    }
}
