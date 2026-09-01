using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #731 · <b>ONE ARITHMETIC, TWO DIRECTIONS, TWO ROOMS.</b>
///
/// <para><b>Owner, 2026-09-01:</b> <i>"also just other customers arriving and leaving in the bars already does
/// a lot… they can go behind doors that are locked to us."</i> A room whose schedule only ever DRAINS is not a
/// room with a metabolism, so the deal that decides who finishes and goes now also decides who turns up — and
/// it is the SAME deal, because two schedules that agree today is this repository's oldest bug class.</para>
///
/// <para>And the same seam is what lets a docked station's bar ask the question at all. The Hive's hall deals
/// its shift over <c>CanteenRegulars.TableSeat</c>s; a bar deals its own over names in numbered chairs. The
/// canteen's overload is now a PROJECTION onto <see cref="Egress.Occupant"/> and not a second copy of the
/// arithmetic, and the first guard below is that sentence made checkable.</para>
/// </summary>
public sealed class TheRoomFillsAsWellAsEmptiesTests
{
    private const string Body = "the-space-bar";
    private const int Level = 0;

    /// <summary>Two leaves with plates on them, the shape a bar's back rooms are.</summary>
    private static readonly UndergroundComplex.LockedDoor[] Leaves =
    [
        new(-14, 49, -14, 53, "🔒 CELLAR · M-B1"),
        new(14, 49, 14, 53, "🔒 STOREROOM · M-B2"),
    ];

    private static IEnumerable<long> Watches() => Enumerable.Range(0, 64).Select(i => (long)i);

    /// <summary>A room of eight sitters, so a third of them clearing the roll is a real number rather than a
    /// coin that is nearly always tails.</summary>
    private static IReadOnlyList<Egress.Occupant> EightPeople() =>
        Enumerable.Range(0, 8).Select(i => new Egress.Occupant(i, $"◈ REGULAR {i}")).ToList();

    // ── The projection is not a second opinion ───────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE CANTEEN'S OVERLOAD IS A FAITHFUL PROJECTION, NOT A RE-NUMBERING.</b>
    ///
    /// <para>The Hive's hall has dealt its shift over <c>CanteenRegulars.TableSeat</c>s since #731 v1, and
    /// that overload is now a projection onto <see cref="Egress.Occupant"/>. A projection has exactly two ways
    /// to be wrong and both are silent: it can hand on the LIST POSITION where the top's own ordinal was
    /// meant (which re-keys every seeded roll in a hall the moment one top is empty), and it can forget which
    /// tops the deal was never supposed to look at.</para>
    ///
    /// <para>So the claim is made against the TOPS, not against a second call: every move dealt names a top
    /// that is really taken, really not a cabinet, and whose plate is really the one on the move — and the
    /// bench deliberately holds an empty top and a cabinet BEFORE the ones that get dealt, so a position-for-
    /// ordinal slip cannot come out right by accident.</para>
    ///
    /// <para><b>Proven RED</b> by projecting the list position instead of <c>top.Index</c>
    /// (<c>watch 1: the deal names top 3, which the room did not seat</c>) and again by dropping the
    /// <c>Quiet</c> skip (<c>watch 7: the deal stood somebody up out of cabinet 1</c>).</para>
    /// </summary>
    [Fact]
    public void THE_PROJECTION_NamesTheTopsTheRoomActuallySeated()
    {
        int compared = 0;
        var wrong = new List<string>();
        IReadOnlyList<CanteenRegulars.TableSeat> tops = SomeTops();

        foreach (long watch in Watches())
        {
            foreach (Egress.Move move in Egress.Departures(Body, Level, watch, tops, Leaves))
            {
                compared++;
                CanteenRegulars.TableSeat? found =
                    tops.Cast<CanteenRegulars.TableSeat?>().FirstOrDefault(t => t!.Value.Index == move.TableIndex);

                if (found is not { } top)
                {
                    wrong.Add($"watch {watch}: the deal names top {move.TableIndex}, which this room has no "
                              + "top with that ordinal at all.");
                    continue;
                }

                if (!top.Taken)
                {
                    wrong.Add($"watch {watch}: the deal names top {move.TableIndex}, which the room did not "
                              + "seat.");
                }

                if (top.Quiet)
                {
                    wrong.Add($"watch {watch}: the deal stood somebody up out of cabinet {top.Cabinet}.");
                }

                if (!string.Equals(top.Plate, move.Plate, StringComparison.Ordinal))
                {
                    wrong.Add($"watch {watch}: the deal stands up `{move.Plate}` from a top the room seated "
                              + $"`{top.Plate}` at.");
                }
            }
        }

        Assert.True(compared >= 20,
                    $"only {compared} move(s) were dealt across sixty-four watches — this guard would be a "
                    + "green number never asked of the world.");
        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong.Take(6)));
    }

    // ── The other direction ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE ROOM FILLS AS WELL AS IT EMPTIES, AND THE TWO ARE NOT ONE ROLL.</b>
    ///
    /// <para>Over sixty-four watches of one room: somebody is dealt IN, somebody is dealt OUT, and the two
    /// halves disagree — a watch that sends a man home is not thereby a watch that brings him back, and the
    /// leaf he would come out of is not forced to be the leaf he would leave by. A schedule whose two
    /// directions were one roll would be a room where everybody who leaves is replaced by themselves.</para>
    ///
    /// <para>All three counts are asserted, because each one alone passes over a broken world: "somebody
    /// arrives" passes over a room that fills every watch, "the halves differ" passes over a room where
    /// nothing ever happens, and neither notices a door dealt by the wrong key.</para>
    ///
    /// <para><b>Proven RED</b> by giving <c>Arrivals</c> the departure's salt and door prefix: <c>64 of 64
    /// watches deal the identical people in and out through the identical leaves</c>.</para>
    /// </summary>
    [Fact]
    public void THE_ROOM_FillsAsWellAsItEmptiesAndTheTwoAreNotOneRoll()
    {
        int watchesThatFill = 0, watchesThatEmpty = 0, watchesThatDiffer = 0, doorsThatDiffer = 0;

        foreach (long watch in Watches())
        {
            IReadOnlyList<Egress.Move> going = Egress.Departures(Body, Level, watch, EightPeople(), Leaves);
            IReadOnlyList<Egress.Move> coming = Egress.Arrivals(Body, Level, watch, EightPeople(), Leaves);

            watchesThatEmpty += going.Count > 0 ? 1 : 0;
            watchesThatFill += coming.Count > 0 ? 1 : 0;
            if (!going.Select(m => m.Plate).SequenceEqual(coming.Select(m => m.Plate)))
            {
                watchesThatDiffer++;
            }

            foreach (Egress.Move a in coming)
            {
                if (going.Any(d => string.Equals(d.Plate, a.Plate, StringComparison.Ordinal)
                                   && d.Door != a.Door))
                {
                    doorsThatDiffer++;
                }
            }
        }

        Assert.True(watchesThatEmpty >= 20, $"only {watchesThatEmpty} of 64 watches sent anybody home.");
        Assert.True(watchesThatFill >= 20, $"only {watchesThatFill} of 64 watches brought anybody in.");
        Assert.True(watchesThatDiffer >= 20,
                    $"only {watchesThatDiffer} of 64 watches deal different people in and out — the two "
                    + "directions are one roll, and everybody who leaves is replaced by themselves.");
        Assert.True(doorsThatDiffer > 0,
                    "on no watch does anybody come in through one leaf and leave by the other — the way in "
                    + "and the way out are forced to be one door by an accident of seeding.");
    }

    /// <summary>Both directions obey the room's own law about how many bodies may be afoot, and both hand
    /// their moves back in the order they HAPPEN — a caller stepping down the list as the watch runs wants the
    /// next one at the front.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>MostAtOnce</c> trim: <c>watch 2 deals 4 arrivals into a
    /// room that allows 2 on its feet</c>.</para></summary>
    [Fact]
    public void BOTH_DirectionsAreCappedAndSorted()
    {
        int fullWatches = 0;

        foreach (long watch in Watches())
        {
            foreach (IReadOnlyList<Egress.Move> dealt in new[]
                     {
                         Egress.Departures(Body, Level, watch, EightPeople(), Leaves),
                         Egress.Arrivals(Body, Level, watch, EightPeople(), Leaves),
                     })
            {
                Assert.True(dealt.Count <= Egress.MostAtOnce,
                            $"watch {watch} deals {dealt.Count} move(s) into a room that allows "
                            + $"{Egress.MostAtOnce} on its feet.");
                Assert.Equal(
                    dealt.Select(m => m.AtSecondsIntoWatch).OrderBy(t => t).ToList(),
                    dealt.Select(m => m.AtSecondsIntoWatch).ToList());

                foreach (Egress.Move m in dealt)
                {
                    Assert.InRange(m.AtSecondsIntoWatch, 0, Egress.LastCallFraction * PatronRota.WatchSeconds);
                    Assert.InRange(m.Door, 0, Leaves.Length - 1);
                }

                fullWatches += dealt.Count == Egress.MostAtOnce ? 1 : 0;
            }
        }

        Assert.True(fullWatches > 0,
                    "no watch in the whole sweep ever filled the room — the cap was never actually reached, "
                    + "so this guard proves nothing about it.");
    }

    /// <summary>A room with no leaf the captain is refused at has nobody coming out of one. The honest answer
    /// about some floors, and never a reason to use a public exit — the same law the departures have kept
    /// since #731 v1, restated for the direction that did not exist then.</summary>
    [Fact]
    public void NOBODY_ComesOutOfADoorTheBuildingDoesNotHave()
    {
        foreach (long watch in Watches())
        {
            Assert.Empty(Egress.Arrivals(Body, Level, watch, EightPeople(), []));
            Assert.Empty(Egress.Departures(Body, Level, watch, EightPeople(), []));
        }
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Eight tops, six of them taken, one of them a cabinet nobody is in — the shape a canteen hall
    /// hands the deal, so the projection is exercised on skips as well as on sitters.</summary>
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
}
