using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #793 · THE PARK BENCH IS A GUMSHOE MOVE — sit, see who else stops, and work the case if the bench is all
/// yours.
///
/// <para>Owner, from play in the new park: <i>"the benches take the sit verb"</i> · <i>"it is a good gumshoe
/// move to see if anyone is following us by foot, as they would need to stop moving also"</i> · <i>"the park
/// bench might be used to go through inventory info loot, if we get the whole bench to ourselves to have
/// some privacy."</i></para>
///
/// <h3>The one thing this file is careful about</h3>
///
/// <para><b>Nothing in the game tails the captain yet</b>, and the honest thing to guard is therefore a
/// SEAM, not a feature: the question exists, every shipped mover answers it <i>no</i> by construction, and
/// the hold law is proved in both directions with a mover this file makes up. A guard that only ever met
/// movers which cannot tail would pass a hold predicate that returned <c>false</c> unconditionally — so the
/// test-only tail is not a convenience, it is the anti-vacuity half.</para>
/// </summary>
public sealed class TheBenchAsksWhoElseStoppedTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 20).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Six watches is a day (<c>CanteenRegulars.WatchFill</c>).</summary>
    private const int Watches = 6;

    private static IEnumerable<(string Body, int Level, UndergroundComplex.FloorPlan Floor,
        UndergroundComplex.Park Park)> EveryPark()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || UndergroundComplex.IsHeadOffice(body))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            if (floor.Park is not { } park)
            {
                continue;
            }
            yield return (body, level, floor, park);
        }
    }

    // ── (a) A BENCH IS A SEAT WITH TWO ENDS, AND ONE OF THEM IS SPOKEN FOR ────────────────────────────

    /// <summary>
    /// EVERY PARK PUBLISHES ITS BENCHES AS SEATS, AND EXACTLY ONE OF THEM IS TAKEN — by #790's own lone
    /// figure, matched rather than re-rolled.
    ///
    /// <para>The occupancy is the whole of the privacy predicate, so it must be a fact about the room and
    /// not a second opinion about it. <c>Park.FigureX/FigureY/FigurePlate</c> have said who is sitting where
    /// since #790; this asserts <see cref="ParkBenches.On"/> reads exactly that, and that both states —
    /// taken and free — are in front of the captain in the same room at the same time.</para>
    ///
    /// <para><b>Proven RED</b> by seeding the occupancy instead of matching it (a second roll for "is
    /// anybody on this plank", which is the tempting shape):</para>
    /// <code>
    /// 30 park(s) disagree with themselves about who is sitting where:
    ///   luna B1: 3 bench(es) read TAKEN, and a park has one lone figure.
    ///   luna B1: the figure at (-171, -238) is on no bench this file can find.
    ///   phobos B1: 2 bench(es) read TAKEN, and a park has one lone figure.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void EVERY_PARK_PublishesItsBenchesAndExactlyOneIsTaken()
    {
        var wrong = new List<string>();
        int parks = 0, benches = 0, taken = 0, free = 0;

        foreach ((string body, int level, _, UndergroundComplex.Park park) in EveryPark())
        {
            parks++;
            IReadOnlyList<ParkBenches.Bench> seats = ParkBenches.On(in park);
            Assert.Equal(park.Benches.Count, seats.Count);

            int here = 0;
            foreach (ParkBenches.Bench b in seats)
            {
                benches++;
                Assert.Equal(park.Benches[b.Index].X, b.X, 6);
                Assert.Equal(park.Benches[b.Index].Y, b.Y, 6);

                if (b.Taken)
                {
                    here++;
                    taken++;
                    Assert.Equal(park.FigurePlate, b.Plate);
                    Assert.True(Math.Abs(b.X - park.FigureX) <= ParkBenches.SameBenchDu,
                        $"{body} B{-level}: bench {b.Index} reads taken and the figure is somewhere else.");
                }
                else
                {
                    free++;
                    Assert.Null(b.Plate);
                }
            }

            if (here != 1)
            {
                wrong.Add($"  {body} B{-level}: {here} bench(es) read TAKEN, and a park has one lone figure.");
            }
            if (seats.Count < 2)
            {
                wrong.Add($"  {body} B{-level}: {seats.Count} bench(es) — a privacy predicate about WHICH "
                    + "bench needs more than one to choose between.");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) disagree with themselves about who is sitting where:\n"
            + string.Join("\n", wrong.Take(8)));

        // Anti-vacuity: both states, really met, in real rooms.
        Assert.True(parks >= 10, $"only {parks} parks were read — this proved little.");
        Assert.True(taken >= parks, $"{taken} taken bench(es) over {parks} parks — the figure never landed.");
        Assert.True(free >= parks, $"{free} free bench(es) over {parks} parks — every plank was occupied.");
        Assert.True(benches == taken + free);
    }

    /// <summary>
    /// THE TWO ENDS ARE ON THE PLANK, THEY ARE APART, AND THE SITTER IS ON THE FIRST ONE.
    ///
    /// <para>Ends that landed on top of each other, or out past the arm-rests, would make "the whole bench"
    /// a phrase with no geometry under it — and the drawn free/taken marks are laid on exactly these
    /// coordinates.</para>
    ///
    /// <para><b>Proven RED</b> twice: with <c>EndOffsetDu = 0</c> (both ends at the centre) and with
    /// <c>EndOffsetDu = UndergroundComplex.ParkBenchHalfDu</c> (the ends at the very tips):</para>
    /// <code>
    /// bench ends at (-171.0, -238.4) and (-171.0, -238.4) are 0.00 du apart — one seat, not two.
    /// bench end at (-172.8, -238.4) is 1.80 du from the centre of a plank 1.80 du long each way —
    ///   that is the arm-rest, not a seat.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_TWO_ENDS_AreOnThePlankAndFarEnoughApartToBeTwoSeats()
    {
        double half = UndergroundComplex.ParkBenchHalfDu;
        Assert.True(ParkBenches.EndOffsetDu > 0 && ParkBenches.EndOffsetDu < half,
            $"a bench end at {ParkBenches.EndOffsetDu:F2} du from the centre of a plank {half:F2} du long "
            + "each way is either the arm-rest or the same seat twice.");

        foreach ((_, _, _, UndergroundComplex.Park park) in EveryPark())
        {
            foreach (ParkBenches.Bench b in ParkBenches.On(in park))
            {
                (double ax, double ay) = b.End(0);
                (double bx, double by) = b.End(1);

                double apart = Math.Sqrt(((ax - bx) * (ax - bx)) + ((ay - by) * (ay - by)));
                Assert.True(apart >= (2 * ParkBenches.EndOffsetDu) - 1e-9,
                    $"bench ends at ({ax:F1}, {ay:F1}) and ({bx:F1}, {by:F1}) are {apart:F2} du apart — "
                    + "one seat, not two.");

                // Both ends are on the plank the carve bolted down.
                Assert.True(Math.Abs(ax - b.X) <= half && Math.Abs(bx - b.X) <= half);
                Assert.Equal(b.Y, ay, 6);
                Assert.Equal(b.Y, by, 6);

                // …and the captain always gets the end the sitter is not on.
                (double yx, double yy) = b.YourEnd;
                if (b.Taken)
                {
                    Assert.NotEqual(b.End(ParkBenches.TakenEnd), (yx, yy));
                }
                else
                {
                    Assert.Equal(b.End(ParkBenches.TakenEnd), (yx, yy));
                }
            }
        }
    }

    // ── (b) THE PRIVACY PREDICATE, WITH A REAL FACT UNDER IT ─────────────────────────────────────────

    /// <summary>
    /// THE WHOLE BENCH, OR NOTHING — and the fact that decides it is the room's.
    ///
    /// <para>#799 shipped the rung and its refusal; what it could not ship was an OCCUPANCY. This walks
    /// every bench in every park and asserts the spread follows the room: on a bench nobody is on, the case
    /// may come out; on the one the figure is on, it may not, and the refusal names the bench.</para>
    ///
    /// <para><b>Proven RED</b> by giving the bench the cabinet's answer
    /// (<c>SeatedHud.Seat.ParkBench =&gt; true</c>):</para>
    /// <code>
    /// 30 park(s) let the case out on a bench somebody else is on:
    ///   luna B1: bench 5 has the lone figure on it and the spread is allowed.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_WHOLE_BENCH_OrNothing_AndTheRoomOwnsTheFact()
    {
        var wrong = new List<string>();
        int allowed = 0, refused = 0;

        foreach ((string body, int level, _, UndergroundComplex.Park park) in EveryPark())
        {
            foreach (ParkBenches.Bench b in ParkBenches.On(in park))
            {
                // "Alone" on a bench is the whole plank being yours — the room's fact, not a mood.
                bool alone = !b.Taken;
                bool may = SeatedSpread.CanSpreadTheCase(SeatedHud.Seat.ParkBench, alone);

                if (may != alone)
                {
                    wrong.Add(b.Taken
                        ? $"  {body} B{-level}: bench {b.Index} has the lone figure on it and the spread is allowed."
                        : $"  {body} B{-level}: bench {b.Index} is empty and the spread is refused anyway.");
                    continue;
                }

                if (may)
                {
                    allowed++;
                    Assert.Null(SeatedSpread.RefusalAt(SeatedHud.Seat.ParkBench, alone));
                }
                else
                {
                    refused++;
                    Assert.Equal(
                        SeatedSpread.NotOnASharedBenchLine,
                        SeatedSpread.RefusalAt(SeatedHud.Seat.ParkBench, alone));
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} bench(es) let the case out where the room says somebody can read it:\n"
            + string.Join("\n", wrong.Take(8)));
        Assert.True(allowed > 0 && refused > 0,
            $"the sweep met {allowed} allowed and {refused} refused — a predicate only ever asked one way "
            + "is a predicate that asserts nothing.");
    }

    /// <summary>
    /// A BENCH AND A CANTEEN TOP NEVER SHARE AN APPROACH ORDINAL.
    ///
    /// <para><see cref="SittingAlone.SomebodyComes"/> is seeded on (site, floor, ORDINAL, watch, beat).
    /// Bench 0 and table 0 are two seats in two rooms; handed the same ordinal they would be dealt the same
    /// answer on the same shift — one source consumed as though it were two.</para>
    ///
    /// <para><b>Proven RED</b> by <c>ApproachOrdinal(i) =&gt; i</c>:</para>
    /// <code>
    /// 30 room(s) deal a bench and a table the same approach: luna B1 w0: bench 0 and top 0 share ordinal 0,
    /// and the die agrees with itself on 6 of 6 watches.
    /// </code>
    /// </summary>
    [Fact]
    public void A_BENCH_AndATop_NeverShareAnApproachOrdinal()
    {
        var wrong = new List<string>();
        int compared = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor,
            UndergroundComplex.Park park) in EveryPark())
        {
            var tops = new HashSet<int>();
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (a.Use != UndergroundComplex.Comfort.UpperCanteen)
                {
                    continue;
                }
                foreach (CanteenRegulars.TableSeat top in CanteenRegulars.Tables(body, level, a, 0))
                {
                    tops.Add(top.Index);
                }
            }

            Assert.True(tops.Count > 0, $"{body} B{-level}: no tops to compare a bench against.");

            foreach (ParkBenches.Bench b in ParkBenches.On(in park))
            {
                int ordinal = ParkBenches.ApproachOrdinal(b.Index);
                compared++;
                if (tops.Contains(ordinal))
                {
                    wrong.Add($"  {body} B{-level}: bench {b.Index} and top {ordinal} share an ordinal.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} bench(es) are rolled on a canteen top's ordinal:\n"
            + string.Join("\n", wrong.Take(8)));
        Assert.True(compared >= 20, $"only {compared} benches were compared — this proved little.");

        // …and the roll really does part company. Same site, same floor, same watch, same beat: a bench and
        // the top with its own index must be able to disagree, or the offset is decorative.
        int disagreements = 0;
        for (long watch = 0; watch < Watches; watch++)
        {
            for (int beat = 0; beat < 6; beat++)
            {
                bool top = SittingAlone.SomebodyComes("luna", -1, 0, watch, beat);
                bool bench = SittingAlone.SomebodyComes(
                    "luna", -1, ParkBenches.ApproachOrdinal(0), watch, beat);
                if (top != bench)
                {
                    disagreements++;
                }
            }
        }
        Assert.True(disagreements > 0,
            "a bench and the top with the same index answered identically on every watch and beat — the "
            + "ordinal offset is not separating the two dice.");
    }

    /// <summary>A bench you are already sharing has nowhere to put a third person, and the wait beat must
    /// not ask. <b>Proven RED</b> by <c>TheOtherEndIsFree(bool) =&gt; true</c>, which lets a plank seat
    /// three.</summary>
    [Fact]
    public void A_SHARED_BENCH_HasNoRoomForAThirdPerson()
    {
        Assert.True(ParkBenches.TheOtherEndIsFree(shared: false));
        Assert.False(ParkBenches.TheOtherEndIsFree(shared: true));
        Assert.Equal(2, ParkBenches.Ends);
    }

    // ── (c) THE TAIL-CHECK, BOTH DIRECTIONS ──────────────────────────────────────────────────────────

    /// <summary>A test-only tail: the mover this game does not have yet, made here so the hold can be proved
    /// to FIRE as well as to hold its tongue. It is deliberately built by hand rather than through
    /// <see cref="FootTail.OnARound"/>, because the whole point of that factory is that it cannot produce
    /// one.</summary>
    private static FootTail.Mover ATestOnlyTailAt(double x, double y) =>
        new("THE ONE BEHIND YOU", x, y, OnAPublishedRound: false, Tailing: true);

    /// <summary>A wall straight across the line between two points — for proving the reading obeys sight.</summary>
    private static IReadOnlyList<SurfaceCollision.Segment> AWallAcross(
        double ax, double ay, double bx, double by)
    {
        double mx = (ax + bx) / 2.0, my = (ay + by) / 2.0;
        double dx = bx - ax, dy = by - ay;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        double px = -dy / len, py = dx / len;
        return [new SurfaceCollision.Segment(mx - (px * 40), my - (py * 40), mx + (px * 40), my + (py * 40))];
    }

    /// <summary>
    /// NOTHING THIS GAME SHIPS IS A TAIL — and it is a law rather than an omission.
    ///
    /// <para>The only movers with routes on these floors are #804's rounds, and a round is a pure function
    /// of (site, floor, watch) laid before the captain steps off the car. <see cref="PatrolBeat.OnTheRound"/>
    /// stamps that, and this asserts the stamp survives every position, every index, and a captain sitting
    /// right next to them with a clear view.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the disqualifying clause
    /// (<c>IsTailing =&gt; mover.Tailing</c>) and letting the round declare itself
    /// (<c>OnARound(..., Tailing: true)</c>):</para>
    /// <code>
    /// PATROL 1 at (4, 4) is reported as FOLLOWING the captain — a round is a route the building published
    /// before the captain arrived, and it cannot be about them.
    /// </code>
    /// </summary>
    [Fact]
    public void NOTHING_THIS_GAME_SHIPS_IsATail()
    {
        int asked = 0;
        for (int index = 0; index < PatrolBeat.MostOnAFloor; index++)
        {
            for (double x = -12; x <= 12; x += 4)
            {
                for (double y = -12; y <= 12; y += 4)
                {
                    FootTail.Mover m = PatrolBeat.OnTheRound(index, x, y);
                    asked++;

                    Assert.True(m.OnAPublishedRound);
                    Assert.False(m.Tailing);
                    Assert.False(FootTail.IsTailing(in m),
                        $"{m.Name} at ({x}, {y}) is reported as FOLLOWING the captain — a round is a route "
                        + "the building published before the captain arrived, and it cannot be about them.");

                    // …and no bench anywhere can make one hold, however close the captain sits or how
                    // clear the line is.
                    Assert.False(FootTail.MustHold(true, 0, 0, in m, []));
                    Assert.False(FootTail.AnythingTailing(0, 0, [m], []));
                    Assert.Empty(FootTail.Holding(true, 0, 0, [m], []));
                }
            }
        }
        Assert.True(asked >= 40, $"only {asked} movers were asked — this proved little.");
    }

    /// <summary>
    /// THE HOLD, PROVED BOTH WAYS, WITH A TAIL THIS FILE MAKES UP.
    ///
    /// <para>Owner: <i>"they would need to stop moving also."</i> A tail whose subject has sat down in the
    /// open cannot walk on past them — so it holds, and the hold is the tell. Five cases, and the sweep
    /// insists it met BOTH answers: a hold that fired for everybody would be as useless as one that never
    /// fired.</para>
    ///
    /// <para><b>Proven RED</b> three times, each by deleting one clause of
    /// <see cref="FootTail.MustHold"/>:</para>
    /// <code>
    /// (no seated clause)  a tail held while the captain was on their feet — walking proves nothing, because
    ///                     everybody on a walk is walking.
    /// (no sight clause)   a tail behind a wall was reported as having stopped when the captain sat down.
    /// (no range clause)   a tail 60.0 du away — twice the reach this game calls seeing — was read off a bench.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_HOLD_FiresForATailAndForNothingElse()
    {
        IReadOnlyList<SurfaceCollision.Segment> clear = [];
        FootTail.Mover tail = ATestOnlyTailAt(6, 0);
        FootTail.Mover ordinary = PatrolBeat.OnTheRound(0, 6, 0);

        bool metTrue = false, metFalse = false;

        // 1 · SEATED, in sight, a real tail → it must hold, and the bench must say so.
        Assert.True(FootTail.IsTailing(in tail));
        Assert.True(FootTail.MustHold(true, 0, 0, in tail, clear));
        Assert.True(FootTail.AnythingTailing(0, 0, [tail, ordinary], clear));
        Assert.Single(FootTail.Holding(true, 0, 0, [tail, ordinary], clear));
        metTrue = true;

        // 2 · ON YOUR FEET → nothing holds. Walking proves nothing: everybody on a walk is walking.
        Assert.False(FootTail.MustHold(false, 0, 0, in tail, clear));
        Assert.Empty(FootTail.Holding(false, 0, 0, [tail], clear));
        metFalse = true;

        // 3 · BEHIND A WALL → not in plain sight, so nothing was revealed by sitting still. This is the
        //     owner's own reason the move belongs in a park: "the same move at a hall table proves nothing."
        IReadOnlyList<SurfaceCollision.Segment> blocked = AWallAcross(0, 0, 6, 0);
        Assert.False(FootTail.InPlainSight(0, 0, in tail, blocked));
        Assert.False(FootTail.MustHold(true, 0, 0, in tail, blocked));
        Assert.False(FootTail.AnythingTailing(0, 0, [tail], blocked));

        // 4 · TOO FAR → past the one reach this game calls seeing.
        FootTail.Mover distant = ATestOnlyTailAt(FootTail.LegibleDu * 2, 0);
        Assert.False(FootTail.MustHold(true, 0, 0, in distant, clear));
        FootTail.Mover justInside = ATestOnlyTailAt(FootTail.LegibleDu - 1, 0);
        Assert.True(FootTail.MustHold(true, 0, 0, in justInside, clear));

        // 5 · A MOVER THAT SIMPLY IS NOT FOLLOWING YOU → never held, however close it stands.
        Assert.False(FootTail.MustHold(true, 0, 0, in ordinary, clear));
        Assert.False(FootTail.AnythingTailing(0, 0, [ordinary], clear));

        // …and the empty world is quiet, which is the shipped answer.
        Assert.False(FootTail.AnythingTailing(0, 0, null, clear));
        Assert.False(FootTail.AnythingTailing(0, 0, [], clear));

        Assert.True(metTrue && metFalse,
            "the hold never met both answers — a predicate asked only one way asserts nothing.");
    }

    /// <summary>
    /// THE READING IS A SENTENCE, NOT A METER — #618's law one instrument along.
    ///
    /// <para><b>Proven RED</b> by <c>Reading(bool) =&gt; NobodyStoppedLine</c>, which is what a seam with
    /// nothing behind it looks like:</para>
    /// <code>
    /// the bench says the same thing whether or not something stopped when the captain did — a reading
    /// that cannot change is not a reading.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_READING_IsASentenceAndNeverAMeter()
    {
        string clean = FootTail.Reading(false);
        string caught = FootTail.Reading(true);

        Assert.NotEqual(clean, caught);
        Assert.Equal(FootTail.NobodyStoppedLine, clean);
        Assert.Equal(FootTail.SomebodyStoppedLine, caught);

        foreach (string line in new[] { clean, caught })
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain('%', line);
            Assert.False(line.Any(char.IsDigit),
                $"the bench's reading carries a figure — it is a sentence, not a detection meter: {line}");
        }

        // Neither sentence draws the conclusion. The stop is what was SEEN; what it means is the player's.
        foreach (string word in new[] { "following", "tail", "tailing", "suspicious" })
        {
            Assert.DoesNotContain(word, caught, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── (d) THE ROOM SAYS THE ROOM IT IS IN ──────────────────────────────────────────────────────────

    /// <summary>
    /// THE STRIP NEVER QUOTES THE HALL AT SOMEBODY SITTING IN THE PARK.
    ///
    /// <para>The customer line's room clause is <see cref="SittingAlone.Fill"/> — a crowd figure for a room
    /// a captain on a bench cannot see, on the far side of a window wall. #740's fault exactly, and the fix
    /// is the same shape as the cabinet's: say the thing that is true about the room you are IN.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>ParkBench</c> arm from
    /// <see cref="SeatedHud.RoomClause"/>:</para>
    /// <code>
    /// a captain sitting on gravel behind a window wall is told "the hall is heaving" — a crowd figure for
    /// a room they cannot see.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_ROOM_CLAUSE_OnABench_NeverQuotesTheHall()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (long watch = 0; watch < Watches; watch++)
        {
            string clause = SeatedHud.RoomClause(SeatedHud.Seat.ParkBench, SittingAlone.Fill(watch));
            seen.Add(clause);
            Assert.DoesNotContain("hall", clause, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(SeatedHud.OpenWalkClause, clause);
        }

        // One sentence on every watch: a bench's clause is about SIGHT LINES, which do not turn over with
        // the shift. The hall's own clause still moves — the two are not the same instrument.
        Assert.Single(seen);
        Assert.True(
            SeatedHud.RoomClause(SeatedHud.Seat.HallTable, 0)
                != SeatedHud.RoomClause(SeatedHud.Seat.HallTable, 1),
            "the hall's clause stopped moving with the room — the bench arm has swallowed everything.");

        // …and the whole customer line still reads as a bench.
        string line = SeatedHud.CustomerLine(SeatedHud.Seat.ParkBench, null, 0, 3, 0.9);
        Assert.Contains("A PARK BENCH", line, StringComparison.Ordinal);
        Assert.Contains(SeatedHud.OpenWalkClause, line, StringComparison.Ordinal);
        Assert.DoesNotContain("hall", line, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AND THE SILENCE IS THE PARK'S. <b>Proven RED</b> by handing a bench
    /// <see cref="SittingAlone.NobodyCame"/>: <c>"A tray goes past. A chair scrapes. Your table stays your
    /// table."</c> — read on gravel under grow-lamps.
    /// </summary>
    [Fact]
    public void NOBODY_CAME_OnABench_IsTheParksOwnSilence()
    {
        var hall = new HashSet<string>(StringComparer.Ordinal);
        for (long watch = 0; watch < Watches; watch++)
        {
            for (int beat = 0; beat < 8; beat++)
            {
                hall.Add(SittingAlone.NobodyCame(watch, beat));
                hall.Add(SittingAlone.NobodyCame(watch, beat, quiet: true));
            }
        }

        var park = new HashSet<string>(StringComparer.Ordinal);
        for (int beat = 0; beat < 12; beat++)
        {
            string line = ParkBenches.NobodyCame(beat);
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain(line, hall);
            park.Add(line);
        }

        // The pool really cycles: four beats on a bench are four different sentences, so the room does not
        // loop while a captain sits through a watch of it.
        Assert.Equal(ParkBenches.NobodyCameLines.Count, park.Count);
        Assert.True(park.Count >= 3, $"a bench has {park.Count} thing(s) to say — that is a loop.");
    }

    // ── (e) THE CANON SWEEP ──────────────────────────────────────────────────────────────────────────

    /// <summary>Every new sentence is listed for canon review, and none of them is empty or a placeholder.
    /// The house rule that makes a prose pass possible at all.</summary>
    [Fact]
    public void EVERY_NEW_SENTENCE_IsListedForTheCanonSweep()
    {
        List<string> sweep = [.. ParkBenches.AllProse(), .. FootTail.AllProse()];

        foreach (string line in sweep)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain("TODO", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lorem", line, StringComparison.OrdinalIgnoreCase);
        }

        // The lines a player actually meets are IN the sweep, rather than a sample of them being in it.
        Assert.Contains(ParkBenches.TookTheBenchLine, sweep);
        Assert.Contains(ParkBenches.SharedTheBenchLine, sweep);
        Assert.Contains(ParkBenches.SomebodyTookTheOtherEndLine, sweep);
        Assert.Contains(FootTail.NobodyStoppedLine, sweep);
        Assert.Contains(FootTail.SomebodyStoppedLine, sweep);
        foreach (string line in ParkBenches.NobodyCameLines)
        {
            Assert.Contains(line, sweep);
        }
    }

    /// <summary>The bench scene is the same machine every other seat runs on: two moves, the framework's own
    /// ids, and the free exit the whole encounter system insists on.</summary>
    [Fact]
    public void THE_BENCH_SCENE_IsOneMoreSceneAndNotASecondEngine()
    {
        foreach (bool shared in new[] { false, true })
        {
            Encounter.Scene scene = ParkBenches.TheBench(shared);

            Assert.True(Encounter.CanAlwaysLeave(scene),
                "you cannot get up off the bench for free — every scene in this game can be left.");
            Assert.Equal(2, scene.Moves.Count);
            Assert.Contains(scene.Moves, m => m.Id == SittingAlone.Wait);
            Assert.Contains(scene.Moves, m => m.Id == SittingAlone.Stand);

            // The opening confirms the state change first (#783's law), and says which half you got.
            Assert.Equal(
                shared ? ParkBenches.SharedTheBenchLine : ParkBenches.TookTheBenchLine, scene.Opening);
            Assert.Equal(shared ? ParkBenches.SharedPlate : ParkBenches.OwnBenchPlate, scene.Counterpart);

            // …and nothing on it costs anything or is rolled. A bench is a posture, not a transaction.
            foreach (Encounter.Move m in scene.Moves)
            {
                Assert.Equal(0, m.Credits);
                Assert.False(m.Rolled);
                Assert.Equal(Encounter.Requirement.Free, m.Needs);
            }
        }
    }
}
