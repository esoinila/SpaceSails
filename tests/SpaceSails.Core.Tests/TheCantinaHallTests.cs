using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #751 · THE HALL RULE — eighty seats, a crowd that is the cover, and the room that stayed empty.
///
/// <para>Owner, 2026-08-06: <i>"The Canteen is way too small… It needs to house like 80 customers… I am
/// thinking like Mos Eisley Space port size bar,"</i> then <i>"Definitely want to make the B1 bar be fancy
/// ... and have cabinet-spaces for sensitive negotiations,"</i> and then the second customer of the same
/// carve: <i>"The canteen for only staff can also be a lot bigger ... usually people eat lunch at same time
/// so the whole staff using it should about fit in."</i></para>
///
/// <para>Every guard below walks the REAL generator over the REAL field. Nothing here builds a synthetic
/// room to measure — that is the fifth bug class, and the whole point of a hall is that it is a shape the
/// ground had to agree to.</para>
/// </summary>
public sealed class TheCantinaHallTests
{
    /// <summary>A spread wide enough that the laws are measured rather than sampled: shallow annexes, deep
    /// clinics, both cheat rocks, and the head office (whose hall reads in its own vocabulary).</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    private static UndergroundComplex.Amenity? HallOn(string body, int level, UndergroundComplex.Comfort use)
    {
        foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities)
        {
            if (a.Use == use && a.Hall is not null)
            {
                return a;
            }
        }
        return null;
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    // ── (a) THE SEAT COUNT ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The B1 hall seats EIGHTY, in the owner's own 2/4/6 mix — measured off the tops the generator laid,
    /// never off the target it was handed.
    ///
    /// <para><b>Proven RED</b> by the tempting implementation: seed each top's size the way #746's ordinary
    /// canteen does (<c>SeatCounts[Roll(...)]</c>) instead of reading the caterer's bill. Twenty independent
    /// rolls over {2,4,6} have a standard deviation of seven, and the sweep says so at once:</para>
    /// <code>
    /// 27 hall(s) miss the owner's eighty:
    ///   europa B1: 70 seats over 20 tops
    ///   ganymede B1: 92 seats over 20 tops
    ///   callisto B1: 86 seats over 20 tops
    ///   enceladus B1: 62 seats over 20 tops
    ///   miranda B1: 90 seats over 20 tops
    /// </code>
    /// </summary>
    [Fact]
    public void TheHallSeatsEightyInTwosFoursAndSixes()
    {
        var wrong = new List<string>();
        int halls = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }
            if (HallOn(body, level, UndergroundComplex.Comfort.UpperCanteen) is not { } hall)
            {
                wrong.Add($"  {body} B{-level}: no hall was carved at all — the bar is still a three-top nook.");
                continue;
            }

            halls++;

            int seats = 0, tops = 0;
            var sizes = new HashSet<int>();
            foreach (CanteenRegulars.TableSeat top in CanteenRegulars.Tables(body, level, hall))
            {
                if (top.Quiet)
                {
                    continue;   // a cabinet's chairs are EXTRA and are counted on their own below
                }
                seats += top.Seats;
                sizes.Add(top.Seats);
                tops++;
            }

            if (seats is < 76 or > 84)
            {
                wrong.Add($"  {body} B{-level}: {seats} seats over {tops} tops");
            }
            if (!sizes.SetEquals(UndergroundComplex.HallTopSizes))
            {
                wrong.Add(
                    $"  {body} B{-level}: the tops are {string.Join("/", sizes.OrderBy(s => s))} — not the "
                    + "owner's 2/4/6");
            }
        }

        Assert.True(halls > 40, $"only {halls} halls were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} hall(s) miss the owner's eighty:\n{string.Join("\n", wrong.Take(12))}");
    }

    // ── (h) THE CABINETS ARE EXTRA ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Three cabinets, six chairs each, and not one of those chairs is in the hall's eighty.
    ///
    /// <para><b>Proven RED</b> by letting the cabinet tops read as ordinary hall tops (<c>Cabinet: 0</c> on
    /// the <c>TableSeat</c>, which is what an author who had not thought about the eighty would write). It
    /// takes this guard and the one above red together, which is the point — the two halves state one
    /// law:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ        ← the cabinets stopped being cabinets
    /// Expected: 3
    /// Actual:   0
    ///
    /// 53 hall(s) miss the owner's eighty:
    ///   luna B1: 98 seats over 23 tops
    ///   phobos B1: 98 seats over 23 tops
    ///   europa B1: 98 seats over 23 tops
    /// </code>
    /// </summary>
    [Fact]
    public void EveryCabinetSeatsSixAndNoneOfThoseChairsIsInTheHallsEighty()
    {
        int cabinets = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.UpperCanteen) is not { } amenity
                || amenity.Hall is not { } hall)
            {
                continue;
            }

            Assert.Equal(UndergroundComplex.CabinetsPerHall, hall.Cabinets.Count);

            var numbers = new List<int>();
            foreach (UndergroundComplex.Cabinet cabinet in hall.Cabinets)
            {
                cabinets++;
                numbers.Add(cabinet.Number);

                // Its own box, inside the hall, holding its own table and nothing of the hall's.
                Assert.True(hall.Contains(cabinet.X, cabinet.Y),
                    $"{body} B{-level}: cabinet {cabinet.Number} is not inside the hall it is off.");
                Assert.True(cabinet.Contains(cabinet.Table.X, cabinet.Table.Y),
                    $"{body} B{-level}: cabinet {cabinet.Number}'s table is not in the cabinet.");
                foreach ((double tx, double ty) in amenity.Tables)
                {
                    Assert.False(cabinet.Contains(tx, ty),
                        $"{body} B{-level}: a hall top at ({tx:F1}, {ty:F1}) is inside cabinet "
                        + $"{cabinet.Number} — those chairs would be counted twice.");
                }

                // The plate, verbatim, numbered.
                Assert.Equal(
                    $"CABINET {cabinet.Number} · BY ARRANGEMENT · ASK AT THE COUNTER", cabinet.Plate);
            }

            Assert.Equal([1, 2, 3], numbers);

            // And the seat count the room offers for one of them is the six the card counts out loud.
            var quiet = CanteenRegulars.Tables(body, level, amenity).Where(t => t.Quiet).ToList();
            Assert.Equal(UndergroundComplex.CabinetsPerHall, quiet.Count);
            Assert.All(quiet, t => Assert.Equal(UndergroundComplex.CabinetSeats, t.Seats));
            Assert.Equal(
                UndergroundComplex.CabinetsPerHall * UndergroundComplex.CabinetSeats,
                quiet.Sum(t => t.Seats));
        }

        Assert.True(cabinets > 120, $"only {cabinets} cabinets swept — this proved little.");
    }

    // ── (k) THE MESS IS SIZED BY A LAW, NOT BY A NUMBER ───────────────────────────────────────────────

    /// <summary>
    /// The staff mess seats the building's whole complement at one sitting, and the complement is derived
    /// from the building's own stock.
    ///
    /// <para><b>Proven RED</b> by hand-typing the figure that happens to be right for the site the feature
    /// was built on — <c>HallSeatsFor(...) =&gt; 80</c> for both rooms — which is wrong everywhere else at
    /// once:</para>
    /// <code>
    /// 37 mess hall(s) do not seat the shift:
    ///   luna B13: 80 seats for a complement of 52
    ///   ganymede B5: 80 seats for a complement of 20
    ///   callisto B5: 80 seats for a complement of 32
    ///   enceladus B21: 80 seats for a complement of 96
    ///   the-clinker B13: 80 seats for a complement of 56
    /// </code>
    /// </summary>
    [Fact]
    public void TheMessSeatsTheWholeComplementAtOneSitting()
    {
        var wrong = new List<string>();
        var complements = new HashSet<int>();
        int messes = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.StaffCanteenFloor(body) is not { } level)
            {
                continue;   // a shallow annex has one canteen, which is its entire catering budget
            }
            if (HallOn(body, level, UndergroundComplex.Comfort.StaffCanteen) is not { } mess)
            {
                wrong.Add($"  {body} B{-level}: the mess was not carved as a hall at all.");
                continue;
            }

            messes++;
            int complement = UndergroundComplex.ImpliedComplement(body);
            complements.Add(complement);

            int seats = CanteenRegulars.Tables(body, level, mess).Sum(t => t.Seats);
            if (seats != complement)
            {
                wrong.Add($"  {body} B{-level}: {seats} seats for a complement of {complement}");
            }

            // …and the law is the building's own arithmetic rather than a second sum written beside it.
            Assert.Equal(
                -UndergroundComplex.DepthOf(body) * UndergroundComplex.HeadsPerDepartment, complement);

            // A mess has no cabinets: nobody negotiates anything in a room the shift stopped coming to.
            Assert.Empty(mess.Hall!.Value.Cabinets);
        }

        Assert.True(messes > 30, $"only {messes} staff messes were measured — this proved little.");

        // THE GUARD IS NOT MEASURING ONE NUMBER. If every site in the sweep implied the same complement, a
        // typed constant would pass this file happily — the fifth bug class exactly.
        Assert.True(complements.Count >= 5,
            $"only {complements.Count} distinct complement(s) across the sweep — a typed 80 would pass this.");

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} mess hall(s) do not seat the shift:\n{string.Join("\n", wrong.Take(12))}");
    }

    /// <summary>The complement counts the floors the building ADMITS to and never the band nobody listed.
    /// That is #592's tell said in the catering budget: whoever is down there is not on this payroll.</summary>
    [Fact]
    public void TheComplementCountsOnlyTheFloorsTheDirectoryAdmitsTo()
    {
        const string hidden = "secret-lab-site-unlisted";
        Assert.True(UndergroundComplex.HasUnlistedBand(hidden));

        int listed = -UndergroundComplex.DepthOf(hidden);
        int everything = -UndergroundComplex.TrueDepthOf(hidden);
        Assert.True(everything > listed, "the cheat rock no longer hides a band — this proves nothing.");

        Assert.Equal(listed * UndergroundComplex.HeadsPerDepartment,
            UndergroundComplex.ImpliedComplement(hidden));
        Assert.NotEqual(everything * UndergroundComplex.HeadsPerDepartment,
            UndergroundComplex.ImpliedComplement(hidden));
    }

    // ── (l) THE MESS STAYS EMPTY. FOREVER. ────────────────────────────────────────────────────────────

    /// <summary>
    /// Nobody is ever in the staff mess, on any watch, on any site.
    ///
    /// <para><b>Proven RED</b> by letting the crowd seeder see it — dropping the <c>Use ==
    /// UpperCanteen</c> clause from <c>CanteenRegulars.Crowd</c>, which is the one-line change that turns
    /// #743's whole room into a lunch rush:</para>
    /// <code>
    /// 480 watch(es) put somebody in a room nobody has come to:
    ///   luna B13 watch 0: 4 of 13 tops taken
    ///   luna B13 watch 1: 11 of 13 tops taken
    ///   luna B13 watch 2: 12 of 13 tops taken
    ///   luna B13 watch 5: 2 of 13 tops taken
    /// </code>
    /// </summary>
    [Fact]
    public void TheMessIsEmptyOnEveryWatchForever()
    {
        var wrong = new List<string>();
        int watches = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.StaffCanteenFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.StaffCanteen) is not { } mess)
            {
                continue;
            }

            for (long watch = 0; watch < 12; watch++)
            {
                watches++;
                int taken = CanteenRegulars.OccupiedTops(body, level, mess, watch);
                if (taken > 0)
                {
                    wrong.Add(
                        $"  {body} B{-level} watch {watch}: {taken} of {mess.Tables.Count} tops taken");
                }
                Assert.Empty(CanteenRegulars.Sitting(body, level, mess, watch));
            }
        }

        Assert.True(watches > 300, $"only {watches} watches were walked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} watch(es) put somebody in a room nobody has come to:\n"
            + string.Join("\n", wrong.Take(12)));
    }

    // ── (d) THE WATCH IS THE MOOD ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The hall heaves on one watch and echoes on another, and nothing anywhere says so.
    ///
    /// <para><b>Proven RED</b> by pinning the fill to one watch's figure — the shape #709 had to fix once
    /// already, a room seeded off the site alone and therefore the same room forever:</para>
    /// <code>
    /// luna B1: the busy watch holds 19 tables and the small one 19 — walking in at two different hours
    ///          must be walking into two different rooms.
    /// </code>
    /// </summary>
    [Fact]
    public void TheHallIsBusierOnSomeWatchesThanOnOthers()
    {
        var thin = new List<string>();
        int halls = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.UpperCanteen) is not { } hall)
            {
                continue;
            }

            halls++;
            var counts = new List<int>();
            for (long watch = 0; watch < UndergroundComplex.CabinetsPerHall * 4; watch++)
            {
                counts.Add(CanteenRegulars.OccupiedTops(body, level, hall, watch));
            }

            int distinct = counts.Distinct().Count();
            if (distinct < 4)
            {
                thin.Add(
                    $"  {body} B{-level}: {distinct} distinct occupancy(ies) across {counts.Count} watches "
                    + $"({string.Join(", ", counts.Take(6))}, …)");
            }

            // TWO WATCHES, MEASURED DIFFERENT — the owner's heaving day and dozen-soul night, named.
            int heaving = CanteenRegulars.OccupiedTops(body, level, hall, 2);
            int quiet = CanteenRegulars.OccupiedTops(body, level, hall, 5);
            Assert.True(heaving > quiet + 5,
                $"{body} B{-level}: the busy watch holds {heaving} tables and the small one {quiet} — "
                + "walking in at two different hours must be walking into two different rooms.");

            // …and both of them are the same room every time you look at them, which is #709's own law.
            Assert.Equal(heaving, CanteenRegulars.OccupiedTops(body, level, hall, 2));
            Assert.Equal(
                CanteenRegulars.Tables(body, level, hall, 2),
                CanteenRegulars.Tables(body, level, hall, 2));
        }

        Assert.True(halls > 40, $"only {halls} halls were measured — this proved little.");
        Assert.True(thin.Count == 0,
            $"the hall does not change with the shift:\n{string.Join("\n", thin.Take(12))}");
    }

    /// <summary>The named regulars are still the named regulars: never doubled up with the crowd, never
    /// wearing a stranger's plate, and still never more than #709's three at once.</summary>
    [Fact]
    public void TheNamedRegularsKeepTheirTablesInsideTheCrowd()
    {
        int seenNamed = 0, seenStrangers = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.UpperCanteen) is not { } hall)
            {
                continue;
            }

            for (long watch = 0; watch < 6; watch++)
            {
                var tops = CanteenRegulars.Tables(body, level, hall, watch);
                var named = tops.Where(t => t.Taken && !t.Stranger).ToList();
                var crowd = tops.Where(t => t.Stranger).ToList();

                seenNamed += named.Count;
                seenStrangers += crowd.Count;

                Assert.InRange(named.Count, 1, CanteenRegulars.MostAtOnce);

                // Nobody is at two tables, and no top holds two people.
                Assert.Equal(tops.Count, tops.Select(t => t.Index).Distinct().Count());
                Assert.Equal(named.Count, named.Select(t => t.Plate).Distinct().Count());

                // A stranger never wears a name the #746 machine would read as one of the three.
                foreach (CanteenRegulars.TableSeat t in crowd)
                {
                    Assert.Equal(CanteenTable.Who.None, CanteenTable.WhoIs(t.Plate));
                    Assert.Contains(t.Plate!, CanteenRegulars.StrangerPlates);
                    Assert.Equal(0, t.Cabinet);
                }

                // …and the ones the room seats against #709's rota are exactly the ones Sitting names.
                var sitting = CanteenRegulars.Sitting(body, level, hall, watch);
                Assert.Equal(sitting.Count, named.Count);
                foreach (CanteenRegulars.Seated s in sitting)
                {
                    Assert.Contains(named, t => t.Plate == s.Plate && t.X == s.X && t.Y == s.Y);
                }
            }
        }

        Assert.True(seenNamed > 100, $"only {seenNamed} regulars seated — this proved little.");
        Assert.True(seenStrangers > 1000, $"only {seenStrangers} strangers seated — the hall is not full.");
    }

    // ── (g) THE BARKS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Every one of the fourteen barks is actually reachable, and each is stable per (patron, watch).
    ///
    /// <para><b>Proven RED</b> by the exact mistake the second addendum invited: the two FANCY barks were
    /// APPENDED to a pool of twelve, and the draw was left rolling over the old width
    /// (<c>Roll(seed, Barks.Count - 2)</c>). Nothing breaks, nothing looks wrong, and two authored lines
    /// are simply never said by anybody:</para>
    /// <code>
    /// 2 bark(s) nobody ever says:
    ///   Real coffee. On a rock like this. Somebody's writing it off against something.
    ///   Table linen on a freight stop. I stopped asking the questions I like the answers to.
    /// </code>
    /// </summary>
    [Fact]
    public void AllFourteenBarksAreReachableAndEachIsStableForItsWatch()
    {
        Assert.Equal(14, CanteenRegulars.Barks.Count);

        var heard = new HashSet<string>(StringComparer.Ordinal);
        int drawn = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || HallOn(body, level, UndergroundComplex.Comfort.UpperCanteen) is not { } hall)
            {
                continue;
            }

            for (long watch = 0; watch < 6; watch++)
            {
                foreach (CanteenRegulars.TableSeat t in CanteenRegulars.Tables(body, level, hall, watch))
                {
                    if (!t.Stranger)
                    {
                        continue;
                    }

                    drawn++;
                    Assert.Contains(t.Line!, CanteenRegulars.Barks);
                    heard.Add(t.Line!);

                    // Deterministic per (patron, watch): the same table on the same shift says the same
                    // thing, and the index the room drew is the index the law hands out.
                    Assert.Equal(
                        CanteenRegulars.Barks[CanteenRegulars.BarkIndex(body, t.Index, watch)], t.Line);
                    Assert.Equal(
                        CanteenRegulars.BarkIndex(body, t.Index, watch),
                        CanteenRegulars.BarkIndex(body, t.Index, watch));
                }
            }
        }

        Assert.True(drawn > 1000, $"only {drawn} barks drawn — this proved little.");

        var unsaid = CanteenRegulars.Barks.Where(b => !heard.Contains(b)).ToList();
        Assert.True(unsaid.Count == 0,
            $"{unsaid.Count} bark(s) nobody ever says:\n  {string.Join("\n  ", unsaid)}");
    }

    /// <summary>The pool is the owner's twelve plus the two the FANCY register added, verbatim and in
    /// order. Pinned literally, because "wired verbatim, change none of them" is the brief.</summary>
    [Fact]
    public void TheBarkPoolIsWiredVerbatim()
    {
        Assert.Equal(
            "Two more runs and I'm wintering somewhere with weather.", CanteenRegulars.Barks[0]);
        Assert.Equal(
            "They pay on the nail here. You don't ask what the nail's in.", CanteenRegulars.Barks[1]);
        Assert.Equal("Cage crew again? Wear the good gloves.", CanteenRegulars.Barks[2]);
        Assert.Equal(
            "I had a mate went down-contract. Sends money home regular. Never writes.",
            CanteenRegulars.Barks[3]);
        Assert.Equal(
            "The coffee's the same on every rock. That's either comforting or it isn't.",
            CanteenRegulars.Barks[4]);
        Assert.Equal(
            "Don't sit near the board on rota day unless you want work.", CanteenRegulars.Barks[5]);
        Assert.Equal(
            "Somebody asked the counter what's below. Funniest thing — nobody remembers who.",
            CanteenRegulars.Barks[6]);
        Assert.Equal(
            "Freight doesn't weigh what the manifest says. Freight never weighs what the manifest says.",
            CanteenRegulars.Barks[7]);
        Assert.Equal("First week? It shows. Sit down, it wears off.", CanteenRegulars.Barks[8]);
        Assert.Equal(
            "The lift's polite. That's more than I can say for the last three sites.",
            CanteenRegulars.Barks[9]);
        Assert.Equal(
            "You get used to the hum. Then one day it stops and you find out you liked it.",
            CanteenRegulars.Barks[10]);
        Assert.Equal("Keep your name simple here. They'll shorten it anyway.", CanteenRegulars.Barks[11]);

        // …and the fancy register (#601's funding trail, overheard rather than stated).
        Assert.Equal(
            "Real coffee. On a rock like this. Somebody's writing it off against something.",
            CanteenRegulars.Barks[12]);
        Assert.Equal(
            "Table linen on a freight stop. I stopped asking the questions I like the answers to.",
            CanteenRegulars.Barks[13]);
    }

    /// <summary>§13.8, over the crowd. Eighty strangers is eighty chances to explain something, which makes
    /// this the largest single body of prose in the building and the most dangerous.</summary>
    [Fact]
    public void NotOneStrangerExplainsANYTHING()
    {
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var prose = CanteenRegulars.AllStrangerProse().ToList();
        Assert.Equal(CanteenRegulars.StrangerPlates.Count + CanteenRegulars.Barks.Count, prose.Count);

        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // The register test, one door along from #701's: every plate reads as a person at a glance and
        // every one of them is boring.
        foreach (string plate in CanteenRegulars.StrangerPlates)
        {
            Assert.StartsWith(CanteenRegulars.Glyph, plate, StringComparison.Ordinal);
            Assert.Equal(plate.ToUpperInvariant(), plate);
            Assert.DoesNotContain(plate, CanteenRegulars.AllProse());
        }
    }

    // ── (e) THE THIN SCENE ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A stranger's table offers exactly three moves and Ask-about-work is not one of them.
    ///
    /// <para><b>Proven RED</b> by handing the crowd the named cast's scene — <c>SceneFor(Who.Hand)</c> for a
    /// stranger, the obvious shortcut when a second counterpart kind arrives:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: ["smalltalk", "round", "leave"]
    /// Actual:   ["smalltalk", "smalltalk2", "round", "show", "work", "leave"]
    /// </code>
    /// </summary>
    [Fact]
    public void AStrangersTableIsSmallTalkTheRoundAndYourLeaveAndNothingElse()
    {
        Encounter.Scene scene = CanteenTable.StrangerScene("◈ CAGE CREW, OFF SHIFT");

        Assert.Equal(
            [CanteenTable.SmallTalk, CanteenTable.Round, CanteenTable.Leave],
            scene.Moves.Select(m => m.Id).ToList());

        Assert.DoesNotContain(scene.Moves, m => m.Id == CanteenTable.Work);
        Assert.DoesNotContain(scene.Moves, m => m.Rolled);

        // The framework's own free-exit law applies to it without this file restating it.
        Assert.True(Encounter.CanAlwaysLeave(scene));

        // The round is the ordinary round at the ordinary price, and it buys the ordinary +1: a stranger's
        // table is where you warm up cheap, which is the whole reason it is on the panel at all.
        Encounter.Move round = scene.Moves.Single(m => m.Id == CanteenTable.Round);
        Assert.Equal(Encounter.Requirement.Credits, round.Needs);
        Assert.Equal(CanteenTable.RoundPrice, round.Credits);
        Assert.Equal(
            Encounter.ModifierStep,
            Encounter.Modifiers(new Encounter.Situation(RoundBought: true)).Single().Value);

        // …and it reads as the person the deck drew, in the room the deck drew them in.
        Assert.Equal("◈ CAGE CREW, OFF SHIFT", scene.Counterpart);
        Assert.Equal(CanteenTable.Setting, scene.Setting);
        Assert.Equal(CanteenTable.WaveIn(CanteenTable.Who.Stranger), scene.Opening);

        // Small talk says the bark they were dealt and changes nothing at all.
        CanteenTable.Answer said = CanteenTable.StrangerSaid(CanteenRegulars.Barks[3]);
        Assert.Equal(CanteenRegulars.Barks[3], said.Line);
        Assert.False(said.GrantsChit || said.ClosesTheAsk || said.HardensTable || said.TeachesTheHouse);
        Assert.Equal(0, said.NervePips);
        Assert.Null(said.Note);
    }

    // ── (i) THE QUIET RULE ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A file on the table is LOUD in the hall and is not loud in a cabinet — the same slip, the same
    /// person, a different room.
    ///
    /// <para><b>Proven RED</b> by removing the location check (<c>PutOnTheTable</c> ignoring
    /// <c>quiet</c>), which is exactly what the code did before #751 and is the only thing that makes a
    /// cabinet a cabinet:</para>
    /// <code>
    /// the ask closed at a table the counter cannot see.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_QUIET_RULE_AFileIsLoudInTheHallAndNotInACabinet()
    {
        var file = new Satchel.Item(Satchel.Kind.Dirt, "dirt:somebody");

        CanteenTable.Answer loud = CanteenTable.PutOnTheTable(file, CanteenTable.Who.Hand, quiet: false);
        Assert.True(loud.ClosesTheAsk, "the counter has eyes in the hall — the ask must close.");
        Assert.Equal(CanteenTable.DirtLine, loud.Line);

        CanteenTable.Answer quiet = CanteenTable.PutOnTheTable(file, CanteenTable.Who.Hand, quiet: true);
        Assert.False(quiet.ClosesTheAsk, "the ask closed at a table the counter cannot see.");
        Assert.Equal(CanteenTable.QuietLine, quiet.Line);
        Assert.Equal(CanteenTable.CabinetLeverageNote, quiet.Note);

        // The two answers do not merely differ in a flag — the counterpart SAYS something different, which
        // is how the mechanic is taught: by observation, never by tooltip.
        Assert.NotEqual(loud.Line, quiet.Line);

        // …and the rule is about the ROOM and never about the paper: everything else on the table reads the
        // same in both rooms.
        var manifest = new Satchel.Item(Satchel.Kind.Paper, "paper:manifest");
        Assert.Equal(
            CanteenTable.PutOnTheTable(manifest, CanteenTable.Who.Fitter, quiet: false),
            CanteenTable.PutOnTheTable(manifest, CanteenTable.Who.Fitter, quiet: true));
    }

    /// <summary>And a cabinet's own top is the one Core marks quiet — nothing else in the building is. The
    /// fact the rule reads has exactly one source.</summary>
    [Fact]
    public void ONESOURCE_OnlyACabinetsTopIsQuiet()
    {
        int quiet = 0, loud = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities)
                {
                    foreach (CanteenRegulars.TableSeat t in CanteenRegulars.Tables(body, level, a))
                    {
                        bool inCabinet = a.Hall is { } h && h.CabinetAt(t.X, t.Y) is not null;
                        Assert.Equal(inCabinet, t.Quiet);
                        Assert.Equal(inCabinet, t.Cabinet > 0);
                        if (t.Quiet)
                        {
                            quiet++;
                        }
                        else
                        {
                            loud++;
                        }
                    }
                }
            }
        }

        Assert.True(quiet > 30, $"only {quiet} cabinet tops swept — this proved little.");
        Assert.True(loud > 300, $"only {loud} ordinary tops swept — this proved little.");
    }

    // ── (n) ONE CARVE, TWO CUSTOMERS ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// The B1 hall and the B17 mess come out of ONE implementation, and the only line where they differ is
    /// the seat target.
    ///
    /// <para>Source-shape, and it has to be: a second copy of the carve would agree with the first for
    /// exactly as long as nobody edited either, which is the whole table at the top of
    /// <c>UndergroundComplex.cs</c>. What is under test is that there is only one carve left to change.</para>
    ///
    /// <para><b>Proven RED</b> by adding the overload a "the mess needs its own version" change would
    /// naturally reach for:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 1
    /// Actual:   2
    /// </code>
    /// </summary>
    [Fact]
    public void ONESOURCE_BothHallsAreCarvedByTheOneCarver()
    {
        string core = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "SpaceSails.Core", "UndergroundComplex.cs"));

        Assert.Equal(1, Occurrences(core, "private static HallSite? CarveHall("));
        Assert.Equal(1, Occurrences(core, "hallSite = CarveHall("));

        // The bill, the pitch and the box are asked for once each — no "if it is the mess" fork anywhere in
        // the geometry. The ONE place the two rooms differ is the seat target they are handed.
        Assert.Equal(1, Occurrences(core, "public static int HallSeatsFor("));
        Assert.Contains("use == Comfort.StaffCanteen ? ImpliedComplement(bodyId) : CantinaHallSeats",
            core, StringComparison.Ordinal);

        // …and behaviourally: both rooms produce the same KIND of object off the same field, so a caller
        // that can read one can read the other without knowing which it has.
        var kinds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string body in Bodies)
        {
            foreach ((int? level, UndergroundComplex.Comfort use) in new (int?, UndergroundComplex.Comfort)[]
            {
                (UndergroundComplex.TopPressurisedFloor(body), UndergroundComplex.Comfort.UpperCanteen),
                (UndergroundComplex.StaffCanteenFloor(body), UndergroundComplex.Comfort.StaffCanteen),
            })
            {
                if (level is not { } l || HallOn(body, l, use) is not { } a || a.Hall is not { } hall)
                {
                    continue;
                }

                kinds.Add(use.ToString());
                Assert.True(hall.X1 > hall.X0 && hall.Y1 > hall.Y0);
                Assert.True(hall.Contains(a.X, a.Y),
                    $"{body} B{-l}: the {use}'s own fixture console is outside its box.");
                Assert.True(hall.Contains(hall.BoardX, hall.BoardY));
                Assert.True(hall.Contains(hall.PlateX, hall.PlateY));
                Assert.All(a.Tables, t => Assert.True(hall.Contains(t.X, t.Y)));
            }
        }

        Assert.Equal(2, kinds.Count);
    }

    private static int Occurrences(string haystack, string needle)
    {
        int found = 0, at = 0;
        while ((at = haystack.IndexOf(needle, at, StringComparison.Ordinal)) >= 0)
        {
            found++;
            at += needle.Length;
        }
        return found;
    }

    // ── THE CLAIM LEDGER ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A hall costs the floor exactly the column it stands on — and never a room the floor was going to
    /// build somewhere else.
    ///
    /// <para>The fact the PR body states, measured rather than asserted: on a hall floor the room stock is
    /// down by the two slots of one rib column against the same floor built without one, no room centre is
    /// inside the hall's box, and the floor is still worth the lift ride.</para>
    /// </summary>
    [Fact]
    public void AHallCostsTheFloorItsOwnColumnAndNothingElse()
    {
        int floors = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                UndergroundComplex.Amenity? hallAmenity = null;
                foreach (UndergroundComplex.Amenity a in floor.Amenities)
                {
                    if (a.Hall is not null)
                    {
                        hallAmenity = a;
                    }
                }
                if (hallAmenity is not { } amenity || amenity.Hall is not { } hall)
                {
                    Assert.False(UndergroundComplex.IsHallFloor(body, level),
                        $"{body} B{-level} is a hall floor and no hall was carved on it.");
                    continue;
                }

                floors++;

                // NOTHING ELSE IS STANDING ON IT. A room drawn inside the hall's walls is #585's stranded
                // room with better furniture.
                foreach ((double rx, double ry) in floor.RoomCentres)
                {
                    Assert.False(hall.Contains(rx, ry),
                        $"{body} B{-level}: a room at ({rx:F1}, {ry:F1}) is inside the hall.");
                }
                foreach (UndergroundComplex.EnSuite cell in floor.EnSuites)
                {
                    Assert.False(hall.Contains(cell.X, cell.Y),
                        $"{body} B{-level}: an en-suite is inside the hall.");
                }
                foreach (UndergroundComplex.LockedDoor door in floor.Locked)
                {
                    // #775 · …EXCEPT THE ONE THE ROOM OWNS. The goods hoist's shutter is a sealed door
                    // standing inside the hall on purpose: freight access is a fixture in the counter's own
                    // service band, and this building's grammar for "that will not open for you" is a locked
                    // door with a plate on it. The exemption is by IDENTITY — the very segment the hall
                    // published — so the law still catches a chamber's door swallowed by the room, which is
                    // the thing it was written about.
                    bool isTheHoist = hall.Freight is { } hoist
                        && Math.Abs(door.X1 - hoist.Shutter.X1) < 0.001
                        && Math.Abs(door.Y1 - hoist.Shutter.Y1) < 0.001
                        && Math.Abs(door.X2 - hoist.Shutter.X2) < 0.001
                        && Math.Abs(door.Y2 - hoist.Shutter.Y2) < 0.001;
                    Assert.True(
                        isTheHoist || !hall.Contains((door.X1 + door.X2) / 2, (door.Y1 + door.Y2) / 2),
                        $"{body} B{-level}: a sealed door is inside the hall.");
                }

                // …and neither car is, which are the two spots every excursion stands on (#801).
                foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
                {
                    Assert.False(hall.Contains(car.X, car.Y),
                        $"{body} B{-level}: the hall swallowed the {car.Kind} car.");
                    Assert.False(hall.Contains(car.Landing.X, car.Landing.Y),
                        $"{body} B{-level}: the hall swallowed the {car.Kind} car's doorstep.");
                }

                // The floor is still a facility rather than a flat — over the sites the game actually
                // ships. Scoped exactly the way TheHiveAmenitiesTests scopes the same law and for the same
                // stated reason: "at least four places" fails on a handful of GENERATED moons that were
                // tight long before any of this, and a guard that ships red teaches everybody to scroll
                // past it. The client's own AFloorIsWorthTheLiftRide is the shipped-sites law.
                if (Bodies.Contains(body))
                {
                    Assert.True(floor.RoomCentres.Count + floor.Amenities.Count >= 4,
                        $"{body} B{-level}: {floor.RoomCentres.Count} rooms and {floor.Amenities.Count} "
                        + "amenities — the hall ate the floor.");
                }

                // And the top floor keeps its washroom beside the wet stack it was always beside.
                if (UndergroundComplex.TopPressurisedFloor(body) == level)
                {
                    Assert.Contains(floor.Amenities, a => a.Use == UndergroundComplex.Comfort.Washroom);
                }
            }
        }

        Assert.True(floors > 70, $"only {floors} hall floors were walked — this proved little.");
    }

    // ── THE CARDS ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The hall's card belongs to the room whose sign it quotes, and to no other floor of any
    /// building. It is the branch office's CANTEEN 1 — the head office's dining room has its own register
    /// and its own arrival card (#411).</summary>
    [Fact]
    public void TheHallCardBelongsToTheCarriersCanteenAndToNoOtherFloor()
    {
        var wrong = new List<string>();
        int fired = 0, floors = 0;

        foreach (string body in Sweep())
        {
            int? top = UndergroundComplex.TopPressurisedFloor(body);
            bool branch = !UndergroundComplex.IsHeadOffice(body);

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                bool fires = UndergroundComplex.ShowsCantinaHallCard(body, level);
                bool want = branch && top == level;
                if (fires != want)
                {
                    wrong.Add($"  {body} B{-level}: {(fires ? "fired" : "silent")}, wanted {(want ? "yes" : "no")}");
                }
                if (fires)
                {
                    fired++;
                }
            }
        }

        Assert.True(floors > 300, $"only {floors} floors walked — this proved little.");
        Assert.True(fired > 40, $"the card fires on only {fired} floors — this proved little.");
        Assert.True(wrong.Count == 0, "the hall card fires on the wrong floors:\n" + string.Join("\n", wrong));

        // The head office is in the sweep and is the reason the clause exists.
        Assert.True(UndergroundComplex.IsHeadOffice("enceladus"), "the sweep no longer holds a head office.");
        Assert.False(UndergroundComplex.ShowsCantinaHallCard(
            "enceladus", UndergroundComplex.TopPressurisedFloor("enceladus")!.Value));
    }

    /// <summary>Both cards, verbatim, and neither explains a thing. The prose is the owner-approved text and
    /// this is where a reword goes red.</summary>
    [Fact]
    public void BothCardsAreWiredVerbatimAndNeitherEXPLAINSAnything()
    {
        Assert.Equal(
            "Carriers' canteen, the sign says, and the room says something else: linen on the tables, "
            + "brass on the pillars, light somebody chose. On a rock with no name on any chart, the company "
            + "feeds its contractors like a hotel feeds guests it wants to keep — and nobody at the tables "
            + "finds that strange, because the pay is on the nail, the coffee is real, and questions are the "
            + "one thing on the menu that costs. Along the back wall, a row of doors. Cabinets, by "
            + "arrangement. The hall is loud. The doors are why.",
            UndergroundComplex.CantinaHallCard);

        Assert.Equal(
            "Six chairs, a table wiped past clean, and a door padded like a vault that dogs shut from "
            + "inside. The hall outside is loud the way a sea is loud — a noise you can hide a sentence in, "
            + "but every face out there sits in the counter's long memory. In here there is no memory: "
            + "whatever crosses this table crosses it once and leaves in the pockets it came in. There is a "
            + "telephone on the wall. It has no dial. Rooms like this are not on the menu — you arrange "
            + "them, or you are brought.",
            UndergroundComplex.CabinetCard);

        Assert.Equal(
            "A cabinet off the hall: six chairs, one door, and no line of sight to the counter. Rooms like "
            + "this are why the hall is loud.",
            UndergroundComplex.CabinetNote);

        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];
        foreach (string text in new[]
        {
            UndergroundComplex.CantinaHallCard, UndergroundComplex.CabinetCard,
            UndergroundComplex.CabinetNote, UndergroundComplex.CantinaHallLabel,
            UndergroundComplex.CabinetLabel,
        })
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        // The cabinet's card counts the chairs out loud, so the room had better have six of them.
        Assert.Equal(6, UndergroundComplex.CabinetSeats);
        Assert.Contains("Six chairs", UndergroundComplex.CabinetCard, StringComparison.Ordinal);
        Assert.Contains("six chairs", UndergroundComplex.CabinetNote, StringComparison.Ordinal);

        // Both name a slot that can actually be painted, and they are not the same picture.
        foreach (string url in new[]
            { UndergroundComplex.CantinaHallArtUrl, UndergroundComplex.CabinetArtUrl })
        {
            Assert.StartsWith("art/", url, StringComparison.Ordinal);
            Assert.EndsWith(".jpg", url, StringComparison.Ordinal);
        }
        Assert.NotEqual(UndergroundComplex.CantinaHallArtUrl, UndergroundComplex.CabinetArtUrl);
        Assert.NotEqual(UndergroundComplex.StaffMessArtUrl, UndergroundComplex.CantinaHallArtUrl);
    }
}
