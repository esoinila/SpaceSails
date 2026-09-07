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
public sealed partial class TheCantinaHallTests
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
}
