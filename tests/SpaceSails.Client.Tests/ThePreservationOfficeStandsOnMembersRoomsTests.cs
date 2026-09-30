using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1332 C · <b>THE PRESERVATION OFFICE, AS A PLACE.</b> One door on Ringside Exchange's hotel level and nowhere
/// else; its plate verbatim over the middle cabin and <c>CABIN n</c> over the other four; the floor still carrying
/// its one label; shut, a cabin like its neighbours that nobody can walk into; ajar, one unlocked leaf in a doorway
/// a body can walk through, a desk and the sheet on it; and every other haven's hotel level the plan it always was,
/// whatever is asked of the office.
/// </summary>
public sealed class ThePreservationOfficeStandsOnMembersRoomsTests
{
    private const string Ringside = PreservationOffice.HavenId;

    private static double Hypot(double dx, double dy) => Math.Sqrt((dx * dx) + (dy * dy));

    private const double Step = 0.5;

    private static DeckPlan Below(string berth, HavenInterior.OfficeDoor office = HavenInterior.OfficeDoor.Shut) =>
        HavenInterior.DockedDeck(berth, level: HavenLevels.ServiceLevel, office: office)!;

    /// <summary>Every lobby that is not the office's.</summary>
    public static TheoryData<string> OtherLobbies
    {
        get
        {
            var rows = new TheoryData<string>();
            foreach (string id in HavenInterior.InteriorBodyIds.Where(HavenInterior.HasLowerLevel))
            {
                if (id != Ringside)
                {
                    rows.Add(id);
                }
            }

            return rows;
        }
    }

    /// <summary>A plan, as one string — walls, doors, consoles, labels, backdrops and furniture in build order.</summary>
    private static string Fingerprint(DeckPlan plan)
    {
        var sb = new StringBuilder();
        foreach (DeckPlan.Wall w in plan.Walls)
        {
            sb.Append(CultureInfo.InvariantCulture, $"W {w.X1:F4} {w.Y1:F4} {w.X2:F4} {w.Y2:F4} {w.IsWindow} {w.IsHull}\n");
        }

        for (int i = 0; i < plan.Doors.Length; i++)
        {
            DeckPlan.Door d = plan.Doors[i];
            sb.Append(CultureInfo.InvariantCulture,
                $"D {d.X1:F4} {d.Y1:F4} {d.X2:F4} {d.Y2:F4} {d.Locked} {plan.LeafOpening(i):F4}\n");
        }

        foreach (DeckPlan.ConsoleSpot c in plan.Consoles)
        {
            sb.Append(CultureInfo.InvariantCulture, $"C {c.Kind} {c.X:F4} {c.Y:F4} {c.Label}\n");
        }

        foreach ((float x, float y, string text) in plan.RoomLabels)
        {
            sb.Append(CultureInfo.InvariantCulture, $"L {x:F4} {y:F4} {text}\n");
        }

        foreach (DeckPlan.Backdrop b in plan.Backdrops)
        {
            sb.Append(CultureInfo.InvariantCulture, $"B {b.Url} {b.X:F4} {b.Y:F4} {b.W:F4} {b.H:F4} {b.Alpha:F4}\n");
        }

        foreach (DeckPlan.FurnitureSpot f in plan.Furniture)
        {
            sb.Append(CultureInfo.InvariantCulture, $"F {f.X0:F4} {f.Y0:F4} {f.X1:F4} {f.Y1:F4} {f.Tone}\n");
        }

        return sb.ToString();
    }

    /// <summary>
    /// <b>THE OFFICE IS AT RINGSIDE EXCHANGE AND NOWHERE ELSE.</b> Swept over the whole catalogue, with the
    /// anti-vacuous half: seven havens asked, exactly one answers.
    ///
    /// <para><b>Proven RED</b> by giving The Deep's <c>LowerSpec</c> an office too.</para>
    /// </summary>
    [Fact]
    public void TheOfficeExistsOnlyAtRingsideExchange()
    {
        IReadOnlyList<string> havens = HavenInterior.InteriorBodyIds;
        Assert.Equal(7, havens.Count);
        Assert.Equal([Ringside], havens.Where(HavenInterior.HasTheOffice).ToArray());
        Assert.True(HavenInterior.HasLowerLevel(Ringside));
        Assert.All(havens.Where(h => h != Ringside), h => Assert.Null(HavenInterior.TheOfficeBox(h)));
    }

    /// <summary>
    /// <b>THE PLATE, VERBATIM, OVER ONE DOOR — THE OTHERS STAY <c>CABIN n</c> — AND THE FLOOR KEEPS ITS ONE
    /// LABEL.</b> The office's door carries one name, painted and registered alike.
    ///
    /// <para><b>Proven RED</b> by painting the plate with a hyphen for its middle dot, and by numbering the office's
    /// neighbours from one again after it (<c>CABIN 3</c> twice).</para>
    /// </summary>
    [Fact]
    public void ThePlateIsVerbatimOverOneDoorAndTheFloorKeepsItsOneLabel()
    {
        Assert.Equal(
            ["CABIN 1", "CABIN 2", "PRESERVATION · BY APPOINTMENT", "CABIN 4", "CABIN 5"],
            HavenInterior.CabinDoorPlatesAt(Ringside));

        foreach (HavenInterior.OfficeDoor office in Enum.GetValues<HavenInterior.OfficeDoor>())
        {
            DeckPlan below = Below(Ringside, office);
            string[] hatches = [.. below.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.Hatch).Select(c => c.Label)];
            Assert.Equal(HavenInterior.CabinDoorPlatesAt(Ringside), hatches);
            (float _, float _, string label) = Assert.Single(below.RoomLabels);
            Assert.Equal(HavenLevels.RingsidePlate, label);
        }

        Assert.Equal(PreservationOffice.DoorPlate, HavenInterior.CabinPlatesAt(Ringside)[PreservationOffice.Cabin - 1]);
    }

    /// <summary>
    /// <b>SHUT, IT IS A CABIN; AJAR, IT IS A DOORWAY.</b> Shut: five locked leaves on an unbroken corridor face, and
    /// not one square of the office reachable from the car. Ajar: exactly one unlocked leaf — the office's, hung part
    /// way over — and every square of the office a body can stand on reachable from the car, the desk's sheet within
    /// an [E] of one of them. Ajar with the desk bare: the same room without the sheet.
    ///
    /// <para><b>Proven RED</b> by drawing the ajar leaf without cutting the wall (the office unreachable).</para>
    /// </summary>
    [Fact]
    public void ShutItIsACabinAndAjarItIsADoorwayABodyWalksThrough()
    {
        (double x0, double y0, double x1, double y1) = HavenInterior.TheOfficeBox(Ringside)!.Value;
        DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Ringside, 0)!.Value;
        (double, double, double, double) bounds = (-20, 18, 25, 60);

        int InOffice(DeckPlan plan, out int reached, out bool sheetReached)
        {
            IReadOnlyList<SurfaceCollision.Segment> walls = plan.CollisionField;
            var got = new HashSet<(int, int)>(
                DeckReachability.Reachable(landing, walls, DeckPlan.AvatarRadius, bounds, Step)
                    .Select(p => ((int)Math.Round(p.X / Step), (int)Math.Round(p.Y / Step))));
            DeckReachability.Point desk = HavenInterior.TheOfficeDeskAt(Ringside)!.Value;
            int standable = 0;
            reached = 0;
            sheetReached = false;
            // On the flood's own lattice (anchored at the bounds), so a square swept is a square the flood can name.
            for (double x = bounds.Item1; x <= bounds.Item3; x += Step)
            {
                for (double y = bounds.Item2; y <= bounds.Item4; y += Step)
                {
                    if (x <= x0 || x >= x1 || y <= y0 || y >= y1
                        || !DeckReachability.Standable(x, y, DeckPlan.AvatarRadius, walls))
                    {
                        continue;
                    }

                    standable++;
                    if (got.Contains(((int)Math.Round(x / Step), (int)Math.Round(y / Step))))
                    {
                        reached++;
                        sheetReached |= Hypot(x - desk.X, y - desk.Y) <= DeckPlan.InteractRadius;
                    }
                }
            }

            return standable;
        }

        DeckPlan shut = Below(Ringside);
        Assert.Equal(HavenLevels.Cabins, shut.Doors.Count(d => d.Locked));
        Assert.True(InOffice(shut, out int shutReached, out _) > 20, "the office box has no floor in it.");
        Assert.Equal(0, shutReached);
        Assert.False(HavenInterior.TheSheetLiesIn(shut));
        Assert.Empty(shut.Furniture);

        DeckPlan ajar = Below(Ringside, HavenInterior.OfficeDoor.Ajar);
        DeckPlan.Door leaf = Assert.Single(ajar.Doors, d => !d.Locked);
        int index = Array.IndexOf(ajar.Doors, leaf);
        Assert.Equal(PreservationOffice.Cabin - 1, index);
        Assert.True(ajar.LeafOpening(index) is > 0 and < 1, "the office's leaf is not hung ajar.");
        int standable = InOffice(ajar, out int reachedAjar, out bool sheet);
        Assert.Equal(standable, reachedAjar);
        Assert.True(sheet, "the sheet is out of reach of every square of the office.");
        Assert.True(HavenInterior.TheSheetLiesIn(ajar));
        Assert.Single(ajar.Furniture);

        DeckPlan bare = Below(Ringside, HavenInterior.OfficeDoor.AjarDeskBare);
        Assert.True(HavenInterior.TheOfficeStandsOpenIn(bare));
        Assert.False(HavenInterior.TheSheetLiesIn(bare));
        Assert.Single(bare.Furniture);
    }

    /// <summary>
    /// <b>NO OFFICE, NO DIFFERENCE.</b> Every other haven's hotel level, asked for with the office ajar or bare, is
    /// byte for byte the plan asked for with it shut — and that is the plan every older caller gets.
    ///
    /// <para><b>Proven RED</b> by giving every station with a hotel level the office's cabin (its plate over their
    /// cabin 3, and their corridor face cut there when asked for it ajar).</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(OtherLobbies))]
    public void EveryOtherHavensHotelLevelIsByteIdenticalWhateverTheOfficeIsAsked(string berth)
    {
        string asAlways = Fingerprint(HavenInterior.DockedDeck(berth, level: HavenLevels.ServiceLevel)!);
        foreach (HavenInterior.OfficeDoor office in Enum.GetValues<HavenInterior.OfficeDoor>())
        {
            Assert.Equal(asAlways, Fingerprint(Below(berth, office)));
        }

        Assert.DoesNotContain(PreservationOffice.DoorPlate, asAlways, StringComparison.Ordinal);
        Assert.Equal(
            Enumerable.Range(1, HavenLevels.Cabins).Select(HavenLevels.CabinDoorPlate),
            HavenInterior.CabinDoorPlatesAt(berth));
    }

    /// <summary>
    /// <b>THE CLERK HAS A WAY TO HIS CAR.</b> The car the room names for him is the one whose landing is nearest his
    /// doorstep, and a body can walk from the one to the other on the plan that is drawn while the door stands
    /// open.
    ///
    /// <para><b>Proven RED</b> by <c>TheClerksCarAt</c> answering the farthest car.</para>
    /// </summary>
    [Fact]
    public void TheClerkHasAWayFromHisDoorToTheNearestCar()
    {
        DeckReachability.Point door = HavenInterior.TheOfficeDoorstepAt(Ringside)!.Value;
        int car = HavenInterior.TheClerksCarAt(Ringside)!.Value;
        double to(int cage)
        {
            DeckReachability.Point l = HavenInterior.TheCageLandingAt(Ringside, cage)!.Value;
            return Hypot(l.X - door.X, l.Y - door.Y);
        }

        for (int cage = 0; cage < HavenLevels.Cages; cage++)
        {
            Assert.True(to(car) <= to(cage));
        }

        IReadOnlyList<SurfaceCollision.Segment> walls = Below(Ringside, HavenInterior.OfficeDoor.Ajar).CollisionField;
        DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Ringside, car)!.Value;
        IReadOnlyCollection<DeckReachability.Point> reached =
            DeckReachability.Reachable(door, walls, DeckPlan.AvatarRadius, (-20, 18, 25, 60), Step);
        Assert.Contains(reached, p => Hypot(p.X - landing.X, p.Y - landing.Y) < Step);
        Assert.Null(HavenInterior.TheClerksCarAt("the-space-bar"));
    }
}
