using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #820 · SITTING SNAPS YOU ONTO THE SEAT — the published half.
///
/// <para>Owner, evening playtest 2026-08-11, on a park bench and liking it: <i>"I like the sit down symbol.
/// The bench is nice also. I would move the avatar on top of the bench when I sit… just snap it into the
/// correct position."</i> The dot sat a full step to the left of the steel, because every seat verb in the
/// game left the captain standing wherever they had pressed [E].</para>
///
/// <h3>What is guarded here, and what is guarded next door</h3>
///
/// <para>A snap needs a COORDINATE to snap to, and the ruling was that it comes off Core's own geometry and
/// is never re-derived by the client. So this file asks the published furniture the two questions the press
/// now asks it — <i>which end of this plank do I get</i> and <i>which chair round this top is free</i> — and
/// asks them of the REAL generator over the REAL field, on every site the sweep can reach. Whether the
/// answers are ground a body can stand on is the client's collision field to answer, and
/// <c>EverySeatIsSomewhereYouCanSitTests</c> answers it there.</para>
///
/// <para>Not one coordinate below is typed into this file. A law measured against a number the test brought
/// with it is the house's fifth bug class, and a seat is exactly the kind of thing somebody would be tempted
/// to write 1.55 about.</para>
/// </summary>
public sealed class SittingSnapsYouOntoTheSeatTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 20).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every pressurised floor the sweep can reach, built once.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.FloorPlan Floor)> EveryFloor()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }
            yield return (body, level, UndergroundComplex.Build(body, level, Field));
        }
    }

    // ── (a) THE BENCH · WHICH END ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #820 · THE END YOU GET IS AN END, IT IS FREE, AND IT IS THE ONE YOU WALKED UP TO.
    ///
    /// <para>Three claims about one plank, and they are three because a bench can be wrong in three ways.
    /// The end has to be an END (a snap to the CENTRE is a captain sitting on the middle of a two-seat
    /// bench, which is the picture the owner was complaining about with a different offset); it has to be
    /// the FREE one when somebody is already there (#790's lone figure, and sitting in their lap is #823's
    /// complaint in a garden); and on a bench that is all yours it has to be the end the captain's own feet
    /// chose, or the sit slides them a plank's length sideways for no reason a player could read.</para>
    ///
    /// <para>Asked from BOTH sides of every real bench, so "the nearest end" cannot be satisfied by an
    /// implementation that always returns the same one — the failure a single-sided test would have waved
    /// through.</para>
    ///
    /// <para><b>Proven RED</b> against the behaviour this issue is about: <c>EndYouTake</c> returning where
    /// the captain was standing (which is what every seat verb did before the snap) —</para>
    /// <code>
    /// 372 bench sitting(s) land somewhere that is not a seat:
    ///   luna B1 bench 0: sat down at (-86.4,-246.3), which is neither end — the ends are (-86.3,-248.1)
    ///     and (-84.5,-248.1).
    ///   luna B1 bench 1: sat down at (-53.9,-215.7), which is neither end — the ends are (-54.1,-213.9)
    ///     and (-52.4,-213.9).
    /// </code>
    ///
    /// <para>That red is also why the two counters above are incremented before anything is checked: the
    /// first run of it reported <i>"no bench in the whole sweep had anybody on it"</i> — the anti-vacuity
    /// clause firing on a broken snap and hiding what actually broke.</para>
    /// </summary>
    [Fact]
    public void THE_BENCH_SeatsYouOnAFreeEndAndItIsTheEndYouWalkedUpTo()
    {
        var wrong = new List<string>();
        int sittings = 0, shared = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            if (floor.Park is not { } park)
            {
                continue;
            }

            foreach (ParkBenches.Bench bench in ParkBenches.On(in park))
            {
                (double lx, double ly) = bench.End(ParkBenches.TakenEnd);
                (double rx, double ry) = bench.End(1);
                string what = $"  {body} B{-level} bench {bench.Index}";

                // Walked up to from beside each end in turn — one standoff off the plank, on the side the
                // park's own gravel runs, which is where a captain pressing [E] actually stands.
                foreach ((double fx, double fy, bool fromLeft) in new[]
                {
                    (lx, ly, true), (rx, ry, false),
                })
                {
                    (double sx, double sy) =
                        ParkBenches.TowardTheWalk(in park, fx, fy, UndergroundComplex.ParkBenchHalfDu);
                    (double gx, double gy) = bench.EndYouTake(sx, sy);

                    // Counted BEFORE anything is checked, so that breaking the snap makes this guard report
                    // the SNAP rather than report that it never reached a shared bench. An anti-vacuity
                    // clause that fires first turns a red into a shrug — found by proving this one red.
                    sittings++;
                    if (bench.Taken)
                    {
                        shared++;
                    }

                    bool onLeft = Math.Abs(gx - lx) < 1e-9 && Math.Abs(gy - ly) < 1e-9;
                    bool onRight = Math.Abs(gx - rx) < 1e-9 && Math.Abs(gy - ry) < 1e-9;

                    if (!onLeft && !onRight)
                    {
                        wrong.Add($"{what}: sat down at ({gx:F1},{gy:F1}), which is neither end — the ends "
                            + $"are ({lx:F1},{ly:F1}) and ({rx:F1},{ry:F1}).");
                        continue;
                    }

                    if (bench.Taken)
                    {
                        if (onLeft)
                        {
                            wrong.Add($"{what}: somebody is already on end {ParkBenches.TakenEnd} and the "
                                + "sit put the captain in their lap.");
                        }
                        continue;   // no choice on a shared plank: which end is not the captain's to pick.
                    }

                    if (onLeft != fromLeft)
                    {
                        wrong.Add($"{what}: the captain walked up to the "
                            + (fromLeft ? "LEFT" : "RIGHT") + " end and was seated on the "
                            + (onLeft ? "LEFT" : "RIGHT") + " one.");
                    }
                }
            }
        }

        Assert.True(sittings > 200, $"only {sittings} bench sitting(s) were tried — this proved little.");
        Assert.True(shared > 0, "no bench in the whole sweep had anybody on it — the shared-end clause "
            + "above never fired, and a guard that cannot reach its own hard case is not a guard.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} bench sitting(s) land somewhere that is not a seat:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    // ── (b) THE CANTEEN TOP · WHICH CHAIR ────────────────────────────────────────────────────────────

    /// <summary>
    /// #820 · THE CHAIR YOU GET IS ON THE RING, IS ONE NOBODY IS IN, AND IS THE NEAREST SUCH.
    ///
    /// <para>The tops are the seat kind that had no published seats at all: the deck drew chairs out of a
    /// radius and an angle it kept to itself, and the [E] press knew only where the TABLE was. So the ring
    /// moved into Core (<see cref="CanteenRegulars.ChairAt"/>) and this states the three things the press
    /// now depends on. The occupancy clause is the sharp one — it is #823's <i>"does the other try sit in
    /// the others lap?"</i> asked about the captain, and it is answered by the very walk the deck draws the
    /// party's bodies on, so a chair the press hands over is a chair the player can SEE is empty.</para>
    ///
    /// <para>Asked from four bearings round each top, because "nearest" is the claim a fixed answer would
    /// satisfy from one bearing and fail from the others.</para>
    ///
    /// <para><b>Proven RED</b> twice, because three clauses need more than one way of being wrong. First
    /// against the very behaviour this issue is about — <c>ChairYouTake</c> answering the position it was
    /// asked from, which is a captain sitting where they stood:</para>
    /// <code>
    /// 2868 sitting(s) at a top land somewhere that is not a chair:
    ///   luna B1 top 0: sat down at (15.5,-164.1), which is 6.2 du from the top — the chairs stand at
    ///     1.55 du.
    ///   luna B1 top 0: sat down at (3.1,-164.1), which is 6.2 du from the top — the chairs stand at
    ///     1.55 du.
    /// </code>
    ///
    /// <para>…and then against the shortcut a snap invites, <c>Chair(0)</c> — which lands ON the ring and
    /// still fails the two clauses that matter, one of them in somebody's lap:</para>
    /// <code>
    /// 2044 sitting(s) at a top land somewhere that is not a chair:
    ///   luna B1 top 0: approached from (3.1,-164.1) and seated in chair 0, walking the captain past free
    ///     chair 1.
    ///   luna B1 top 4: 2 at it, and the captain was seated in chair 0, which one of them is in.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_CANTEEN_TOP_SeatsYouInTheNearestChairNobodyIsIn()
    {
        var wrong = new List<string>();
        int sittings = 0, occupied = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                foreach (CanteenRegulars.TableSeat top in
                    CanteenRegulars.Tables(body, level, a, watch: 0))
                {
                    if (top.Seats <= 0 || top.Free <= 0)
                    {
                        continue;
                    }
                    if (top.Taken)
                    {
                        occupied++;
                    }

                    // Stood off the top at arm's length, N/E/S/W — four different "nearest" answers, and a
                    // press that always handed over chair 0 would fail three of them.
                    double reach = CanteenRegulars.ChairRingDu * 4;
                    foreach ((double fx, double fy) in new[]
                    {
                        (top.X + reach, top.Y), (top.X - reach, top.Y),
                        (top.X, top.Y + reach), (top.X, top.Y - reach),
                    })
                    {
                        sittings++;
                        string what = $"  {body} B{-level} top {top.Index}";

                        if (top.ChairYouTake(fx, fy) is not { } got)
                        {
                            wrong.Add($"{what}: seats {top.Seats} with {top.Free} free and handed over no "
                                + "chair at all.");
                            continue;
                        }

                        double outward = Math.Sqrt(((got.X - top.X) * (got.X - top.X))
                                                 + ((got.Y - top.Y) * (got.Y - top.Y)));
                        if (Math.Abs(outward - CanteenRegulars.ChairRingDu) > 1e-9)
                        {
                            wrong.Add($"{what}: sat down at ({got.X:F1},{got.Y:F1}), which is {outward:F1} du "
                                + $"from the top — the chairs stand at {CanteenRegulars.ChairRingDu} du.");
                            continue;
                        }

                        // WHICH chair it is, and everything below is stated about that ordinal rather than
                        // about a pair of doubles.
                        int seat = -1;
                        for (int c = 0; c < top.Seats; c++)
                        {
                            (double cx, double cy) = top.Chair(c);
                            if (Math.Abs(cx - got.X) < 1e-9 && Math.Abs(cy - got.Y) < 1e-9)
                            {
                                seat = c;
                                break;
                            }
                        }
                        if (seat < 0)
                        {
                            wrong.Add($"{what}: sat down on the ring but in none of its {top.Seats} chairs.");
                            continue;
                        }

                        if (top.PartyIn(seat))
                        {
                            wrong.Add($"{what}: {top.Heads} at it, and the captain was seated in chair "
                                + $"{seat}, which one of them is in.");
                            continue;
                        }

                        for (int c = 0; c < top.Seats; c++)
                        {
                            if (top.PartyIn(c))
                            {
                                continue;
                            }
                            (double cx, double cy) = top.Chair(c);
                            double mine = ((got.X - fx) * (got.X - fx)) + ((got.Y - fy) * (got.Y - fy));
                            double theirs = ((cx - fx) * (cx - fx)) + ((cy - fy) * (cy - fy));
                            if (theirs < mine - 1e-9)
                            {
                                wrong.Add($"{what}: approached from ({fx:F1},{fy:F1}) and seated in chair "
                                    + $"{seat}, walking the captain past free chair {c}.");
                                break;
                            }
                        }
                    }
                }
            }
        }

        Assert.True(sittings > 1000, $"only {sittings} sitting(s) were tried — this proved little.");
        Assert.True(occupied > 20, $"only {occupied} top(s) in the whole sweep had anybody at them — the "
            + "lap clause never really fired.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} sitting(s) at a top land somewhere that is not a chair:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>
    /// #823/#820 · THE PARTY IS IN EXACTLY AS MANY CHAIRS AS IT HAS HEADS — the walk itself, over every seat
    /// count the building can lay and every headcount that can sit at one.
    ///
    /// <para>It was the deck's own loop and is Core's now, because the press had to read it to find a chair
    /// nobody is in. The property that makes it safe to read is the one below: it selects <c>heads</c>
    /// chairs, never <c>heads ± 1</c>, so "free chairs are seats less heads" (<c>TableSeat.Free</c>) and
    /// "these particular chairs are taken" are two statements of one fact rather than two facts.</para>
    ///
    /// <para><b>Proven RED</b> by the off-by-one a spread walk invites — <c>&lt;= heads</c> in place of
    /// <c>&lt; heads</c>:</para>
    /// <code>
    /// Assert.Empty() Failure: Collection was not empty
    /// Collection: ["a 2-seat top with 1 head fills 2 chairs", "a 4-seat top with 1 head fills 2 chairs", ...]
    /// </code>
    /// </summary>
    [Fact]
    public void THE_PARTY_FillsExactlyItsOwnHeadcountOfChairs()
    {
        var wrong = new List<string>();

        foreach (int seats in CanteenRegulars.SeatCounts)
        {
            for (int heads = 0; heads <= seats; heads++)
            {
                int filled = 0;
                for (int c = 0; c < seats; c++)
                {
                    if (CanteenRegulars.PartyInChair(c, seats, heads))
                    {
                        filled++;
                    }
                }
                if (filled != heads)
                {
                    wrong.Add($"a {seats}-seat top with {heads} head(s) fills {filled} chairs");
                }
            }
        }

        Assert.Empty(wrong);
    }

    // ── (c) THE COUNTER · THE ROW THE VERB IS DEALT FROM ─────────────────────────────────────────────

    /// <summary>
    /// #820 · A SERVING COUNTER PUBLISHES A SEAT FOR EVERY STOOL THE VERB CAN HAND OUT.
    ///
    /// <para>The stool sitting reads <see cref="UndergroundComplex.FloorPlan.TheStoolRow"/> by the ordinal
    /// <see cref="TheStools.FirstFreeStool"/> handed it, so the row and the row's own occupancy have to be
    /// the same length. They come from two files that have never spoken (the carve lays the seats beside the
    /// counter's segments; <c>TheStools</c> decides who is on them off the watch), which is exactly the
    /// shape of a mismatch nobody would notice until a captain pressed the verb on the last stool.</para>
    ///
    /// <para>And the row is only there where the counter SERVES — a hall whose desk takes no orders (the
    /// staff mess, the head office's sideboard) has no stools, which is a true statement about those rooms
    /// and the reason the client's placement is written as a bound rather than an assumption.</para>
    ///
    /// <para><b>Proven RED</b> by laying the row over the counter's whole band instead of its serving
    /// length, one stool short:</para>
    /// <code>
    /// 12 counter(s) deal a stool they have not bolted down:
    ///   luna B1: the row is 7 seat(s) long and the verb deals 8 of them.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_COUNTER_PublishesEverySeatTheStoolVerbCanDeal()
    {
        var wrong = new List<string>();
        int serving = 0, bare = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            IReadOnlyList<(double X, double Y)> row = floor.TheStoolRow;
            bool serves = false;
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                serves |= a.Hall is not null && CounterService.For(body, a.Use) is not null;
            }

            if (!serves)
            {
                bare++;
                if (row.Count > 0)
                {
                    wrong.Add($"  {body} B{-level}: {row.Count} stool(s) at a counter nobody serves at.");
                }
                continue;
            }

            serving++;
            if (row.Count != TheStools.Count)
            {
                wrong.Add($"  {body} B{-level}: the row is {row.Count} seat(s) long and the verb deals "
                    + $"{TheStools.Count} of them.");
            }
        }

        Assert.True(serving > 8, $"only {serving} serving counter(s) were read — this proved little.");
        Assert.True(bare > 0, "no floor in the sweep had a counter that does not serve — the other half of "
            + "the claim never fired.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} counter(s) deal a stool they have not bolted down:\n"
            + string.Join("\n", wrong.Take(20)));
    }
}
