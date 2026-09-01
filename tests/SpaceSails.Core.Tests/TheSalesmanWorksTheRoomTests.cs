using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1061 · <b>WHOSE TABLES HE STOPS AT, IN WHAT ORDER, AND FOR HOW LONG.</b>
///
/// <para><b>Owner, 2026-09-01:</b> <i>"let's at some point work on those A* walking insurance salesmen at
/// stations :-D"</i> The design's own sentence for what that means: <i>between his approaches to the captain
/// he works the room — A* to other patrons' tables, a beat of patter at each, then the next mark, and when the
/// room is worked, out through the egress like anyone whose shift ends.</i></para>
///
/// <para>The arithmetic behind that is <see cref="Egress.Marks"/>, and it is the shift's THIRD question after
/// who goes and who comes — asked of the same room, in the same order, off the same seeds. What is guarded
/// here is that it is frozen (the same tables in the same order on every machine and across a reload), that it
/// only ever names people the room actually seated, and that a pause is a pause rather than a full stop.</para>
/// </summary>
public sealed class TheSalesmanWorksTheRoomTests
{
    private const string Body = "the-space-bar";
    private const int Level = 0;
    private const string Who = NebulaRep.ContactId;

    private static IEnumerable<long> Watches() => Enumerable.Range(0, 64).Select(i => (long)i);

    /// <summary>Two leaves with plates on them, the shape a hall's staff doors are — so the departures the
    /// composition guard asks about have somewhere to be dealt through.</summary>
    private static readonly UndergroundComplex.LockedDoor[] Leaves =
    [
        new(-14, 49, -14, 53, "🔒 STAFF · M-B1"),
        new(14, 49, 14, 53, "🔒 STORES · M-B2"),
    ];

    /// <summary>A room of eight sitters, so a round of three is a real choice rather than "everybody".</summary>
    private static IReadOnlyList<Egress.Occupant> EightPeople() =>
        [.. Enumerable.Range(0, 8).Select(i => new Egress.Occupant(i, $"◈ REGULAR {i}"))];

    // ── Frozen ───────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SAME TABLES IN THE SAME ORDER, FOR EVER — AND NOT THE SAME ORDER EVERY WATCH.</b>
    ///
    /// <para>Both halves, because either alone passes over a broken world. A round asked twice must come back
    /// identical (the frozen-watch law: a captain who reloads a save watches the same man work the same tables
    /// in the same order), and across sixty-four watches the room must not be worked the same way twice —
    /// a "deterministic" round that simply handed back the room's own list in the room's own order would
    /// satisfy the first claim perfectly and would be a salesman on rails.</para>
    ///
    /// <para><b>Proven RED</b> by returning the pool unshuffled: <c>63 of 64 watches work this room in the
    /// identical order — the round is the room's own list with a hat on</c>.</para>
    /// </summary>
    [Fact]
    public void THE_ROUND_IsFrozenToTheWatchAndIsNotTheSameRoundEveryWatch()
    {
        var orders = new HashSet<string>(StringComparer.Ordinal);
        int repeated = 0;

        foreach (long watch in Watches())
        {
            IReadOnlyList<Egress.Patter> once = Egress.Marks(Body, Level, watch, Who, EightPeople());
            IReadOnlyList<Egress.Patter> twice = Egress.Marks(Body, Level, watch, Who, EightPeople());

            Assert.Equal(once.Select(m => m.Index).ToList(), twice.Select(m => m.Index).ToList());
            Assert.Equal(once.Select(m => m.BeatSeconds).ToList(), twice.Select(m => m.BeatSeconds).ToList());

            if (!orders.Add(string.Join(",", once.Select(m => m.Index))))
            {
                repeated++;
            }
        }

        Assert.True(orders.Count >= 20,
                    $"{repeated} of 64 watches work this room in an order already seen and only "
                    + $"{orders.Count} distinct rounds came out of a room of eight — the round is the room's "
                    + "own list with a hat on, and the captain can set a watch by it.");
    }

    /// <summary>…and it is HIS round. Two people working one room do not walk the same line, which is the
    /// whole reason the seed takes a name at all.</summary>
    [Fact]
    public void AND_ItIsHisRoundAndNotTheRoomsOnly()
    {
        int differ = 0;
        foreach (long watch in Watches())
        {
            IReadOnlyList<Egress.Patter> his = Egress.Marks(Body, Level, watch, Who, EightPeople());
            IReadOnlyList<Egress.Patter> hers = Egress.Marks(Body, Level, watch, "somebody-else", EightPeople());
            if (!his.Select(m => m.Index).SequenceEqual(hers.Select(m => m.Index)))
            {
                differ++;
            }
        }

        Assert.True(differ >= 40,
                    $"only {differ} of 64 watches deal two people different rounds — whose round it is makes "
                    + "no difference, so the room has one line and everybody in it walks that line.");
    }

    // ── Real people, at real tables ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>HE ONLY EVER STOPS AT SOMEBODY THE ROOM ACTUALLY SEATED.</b>
    ///
    /// <para>Asked through the canteen's own projection (<see cref="Egress.Seated"/>) over a bench that holds
    /// an EMPTY top and a CABINET before the ones that get dealt — so a position-for-ordinal slip cannot come
    /// out right by accident, and a round that stopped at a cabinet nobody is in cannot pass. Every mark is
    /// matched back against the tops themselves, never against a second call.</para>
    ///
    /// <para><b>Proven RED</b> by projecting the list position instead of <c>top.Index</c> in
    /// <c>Egress.Seated</c> (<c>watch 0: the round stops at top 7, which the room did not seat</c>) and again
    /// by dropping the <c>Quiet</c> skip (<c>watch 0: the round stops at cabinet 1</c>).</para>
    /// </summary>
    [Fact]
    public void EVERY_MarkIsSomebodyTheRoomActuallySeated()
    {
        IReadOnlyList<CanteenRegulars.TableSeat> tops = SomeTops();
        IReadOnlyList<Egress.Occupant> seated = Egress.Seated(tops);
        int stopped = 0;
        var wrong = new List<string>();

        foreach (long watch in Watches())
        {
            IReadOnlyList<Egress.Patter> round = Egress.Marks(Body, Level, watch, Who, seated);

            Assert.Equal(Math.Min(Egress.MostMarks, seated.Count), round.Count);
            Assert.Equal(round.Count, round.Select(m => m.Index).Distinct().Count());

            foreach (Egress.Patter mark in round)
            {
                stopped++;
                CanteenRegulars.TableSeat? found =
                    tops.Cast<CanteenRegulars.TableSeat?>().FirstOrDefault(t => t!.Value.Index == mark.Index);

                if (found is not { } top)
                {
                    wrong.Add($"watch {watch}: the round stops at top {mark.Index}, which this room has no "
                              + "top with that ordinal at all.");
                    continue;
                }

                if (!top.Taken)
                {
                    wrong.Add($"watch {watch}: the round stops at top {mark.Index}, which the room did not "
                              + "seat — he is selling a policy to an empty chair.");
                }

                if (top.Quiet)
                {
                    wrong.Add($"watch {watch}: the round stops at cabinet {top.Cabinet}.");
                }

                if (!string.Equals(top.Plate, mark.Plate, StringComparison.Ordinal))
                {
                    wrong.Add($"watch {watch}: the round stops at `{mark.Plate}` at a top the room seated "
                              + $"`{top.Plate}` at.");
                }
            }
        }

        Assert.True(stopped >= 60,
                    $"only {stopped} stop(s) were dealt across sixty-four watches — this guard would be a "
                    + "green number never asked of the world.");
        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong.Take(6)));
    }

    /// <summary>A ROOM WITH NOBODY IN IT IS ALREADY WORKED. The vacuity half of the client's coverage floor:
    /// no sitters, no marks — and never a man pausing beside an empty chair.</summary>
    [Fact]
    public void A_ROOM_WithNobodySittingInItHasNoRoundToWork()
    {
        foreach (long watch in Watches())
        {
            Assert.Empty(Egress.Marks(Body, Level, watch, Who, []));
            Assert.Empty(Egress.Marks(Body, Level, watch, Who, [new Egress.Occupant(0, "")]));
        }
    }

    /// <summary>…and a room smaller than a round is worked in one pass. The floor the client keeps
    /// (<see cref="Egress.MarksBeforeTheTable"/>) is capped by this, which is why it is a floor and not a
    /// quota: a hall with one sitter must not leave the captain waiting for a second who is not there.</summary>
    [Fact]
    public void AND_ASmallRoomIsWorkedInOnePass()
    {
        Assert.True(Egress.MostMarks >= Egress.MarksBeforeTheTable,
                    $"a round is at most {Egress.MostMarks} stop(s) and the captain is made to watch "
                    + $"{Egress.MarksBeforeTheTable} of them — he would never come to the table at all.");

        foreach (long watch in Watches())
        {
            Assert.Single(Egress.Marks(Body, Level, watch, Who, [new Egress.Occupant(4, "◈ ONE-EYE SILAS")]));
        }
    }

    // ── A pause, not a full stop ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY BEAT IS A PAUSE, AND THEY ARE NOT ALL THE SAME PAUSE.</b>
    ///
    /// <para>In range, because a beat of nought is a man who does not stop and a beat of a whole watch is a
    /// man who never leaves. And varying, because a constant pause is a metronome: the sixty-four watches must
    /// not deal one length over and over, or the round has a rhythm the player learns in a minute.</para>
    ///
    /// <para><b>Proven RED</b> by returning <c>ShortestPatterSeconds</c> for every beat: <c>every one of the
    /// 192 pause(s) dealt is 5.0s long — the round is a metronome</c>.</para>
    /// </summary>
    [Fact]
    public void EVERY_PauseIsABeatAndNotAMetronome()
    {
        var lengths = new List<double>();

        foreach (long watch in Watches())
        {
            foreach (Egress.Patter mark in Egress.Marks(Body, Level, watch, Who, EightPeople()))
            {
                Assert.InRange(mark.BeatSeconds, Egress.ShortestPatterSeconds, Egress.LongestPatterSeconds);
                lengths.Add(mark.BeatSeconds);
            }
        }

        Assert.True(lengths.Count >= 60, $"only {lengths.Count} pause(s) were dealt in the whole sweep.");
        Assert.True(lengths.Distinct().Count() >= 20,
                    $"the {lengths.Count} pause(s) dealt across sixty-four watches come in only "
                    + $"{lengths.Distinct().Count()} length(s) — the round is a metronome, and a player can "
                    + "time him.");
    }

    // ── #1070 · TWO READINGS OF ONE ROOM, AND THE CROWD IS THE DIFFERENCE ────────────────────────────────

    /// <summary>
    /// <b>THE SALESMAN WORKS THE CROWD; THE SCHEDULE NEVER GIVES IT LEGS.</b> The one place this lane and
    /// #1070's rota-turnover lane meet, stated so it cannot drift back together by accident.
    ///
    /// <para>#751's law is <i>"a background patron is a plate, a bark and a chair. No pathing, no schedule, no
    /// per-frame anything"</i>, and #1070 spent it: the crowd is off <see cref="Egress.OnTheSchedule"/>,
    /// because standing one of them up would be all three of those things and because a dozen background tops
    /// against the rota's three made the room's own turnover invisible. <b>A salesman's round is the other
    /// question.</b> He asks who is sitting there to be stood beside — and stopping at a crowd top gives them
    /// nothing to run: he has the legs, they keep their plate, their bark and their chair. A Fess who walked
    /// past a dozen people eating to reach the one named yard hand would be working a rota, not a room.</para>
    ///
    /// <para>All four claims are asserted, because each alone passes over a broken world: the two readings
    /// must differ, the round must really stop at the crowd (not merely be allowed to), no departure may ever
    /// name one, and the named people must still be in both.</para>
    ///
    /// <para><b>Proven RED</b> both ways — by handing <c>Marks</c> the schedule's reading (<c>only 0 of 64
    /// watches stop at anybody in the crowd</c>) and by neutralising the <c>Stranger</c> clause in
    /// <c>Egress.OnTheSchedule</c> (<c>4 out of 4 items in the collection did not pass … Occupant { Index = 1,
    /// Plate = ◈ TWO HAULIERS, EATING (1) }</c>).</para>
    /// </summary>
    [Fact]
    public void THE_CrowdIsWorkedByTheSalesmanAndNeverPutOnTheSchedule()
    {
        IReadOnlyList<CanteenRegulars.TableSeat> tops = AHallWithACrowdInIt();
        var crowd = new HashSet<int>();
        var named = new HashSet<int>();
        foreach (CanteenRegulars.TableSeat top in tops)
        {
            if (top.Taken && !top.Quiet)
            {
                _ = top.Stranger ? crowd.Add(top.Index) : named.Add(top.Index);
            }
        }

        Assert.True(crowd.Count > 0 && named.Count > 0, "this bench has only one kind of person in it.");

        // 1 · The two readings of the room are not the same reading.
        IReadOnlyList<Egress.Occupant> seated = Egress.Seated(tops);
        IReadOnlyList<Egress.Occupant> onShift = Egress.OnTheSchedule(tops);
        Assert.All(crowd, i => Assert.Contains(seated, o => o.Index == i));
        Assert.All(crowd, i => Assert.DoesNotContain(onShift, o => o.Index == i));
        Assert.All(named, i => Assert.Contains(seated, o => o.Index == i));
        Assert.All(named, i => Assert.Contains(onShift, o => o.Index == i));

        // 2 · …and the round really does stop at the crowd, rather than merely being allowed to.
        int watchesWorkingTheCrowd = 0;
        var stoodUp = new List<string>();
        foreach (long watch in Watches())
        {
            if (Egress.Marks(Body, Level, watch, Who, seated).Any(m => crowd.Contains(m.Index)))
            {
                watchesWorkingTheCrowd++;
            }

            // 3 · …and no schedule ever gives one of them legs.
            foreach (Egress.Move move in Egress.Departures(Body, Level, watch, tops, Leaves))
            {
                if (crowd.Contains(move.TableIndex))
                {
                    stoodUp.Add($"watch {watch}: the schedule stands up `{move.Plate}`, who is one of the "
                                + "crowd and has no schedule at all (#751).");
                }
            }
        }

        Assert.True(watchesWorkingTheCrowd >= 40,
                    $"only {watchesWorkingTheCrowd} of 64 watches stop at anybody in the crowd — the salesman "
                    + "is working the rota rather than the room, and he walks past a hall of people eating to "
                    + "reach the one named yard hand.");
        Assert.True(stoodUp.Count == 0, string.Join(Environment.NewLine, stoodUp.Take(6)));
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Eight tops, six of them taken, one of them a cabinet nobody is in — the same bench the
    /// departures' own projection guard stands on, so the two questions are asked of one room.</summary>
    private static IReadOnlyList<CanteenRegulars.TableSeat> SomeTops()
    {
        var tops = new List<CanteenRegulars.TableSeat>();
        for (int i = 0; i < 8; i++)
        {
            tops.Add(new CanteenRegulars.TableSeat(
                i, i * 3.0, 20.0, 4,
                Plate: i == 3 ? null : $"◈ REGULAR {i}",
                Line: null,
                Cabinet: i == 6 ? 1 : 0));
        }

        return tops;
    }

    /// <summary>A HALL AS THE HIVE ACTUALLY BUILDS ONE — the rota's few named people, and #751's crowd as
    /// data around them. Four of the crowd against two named, one top nobody is at and one cabinet, so both
    /// readings of the room have something to be right or wrong about.</summary>
    private static IReadOnlyList<CanteenRegulars.TableSeat> AHallWithACrowdInIt()
    {
        var tops = new List<CanteenRegulars.TableSeat>();
        for (int i = 0; i < 8; i++)
        {
            bool crowd = i is 1 or 2 or 4 or 7;
            tops.Add(new CanteenRegulars.TableSeat(
                i, i * 3.0, 20.0, 4,
                Plate: i == 3 ? null : crowd ? $"◈ TWO HAULIERS, EATING ({i})" : $"◈ THE YARD HAND {i}",
                Line: null,
                Stranger: crowd,
                Cabinet: i == 6 ? 1 : 0));
        }

        return tops;
    }
}
