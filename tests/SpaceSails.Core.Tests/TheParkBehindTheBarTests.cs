using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #759 · THE PARK BEHIND THE BAR — a room that has to be BIG, has to be ADJACENT, and has to be shut off
/// from the room it is adjacent to by a wall you can see through.
///
/// <para>Owner, 2026-08-06 night: <i>"Let's go Vault Tech fancy and have a view to an underground park
/// behind the bar, windows between."</i> Then, from a cruise ship: <i>"the reference is the ship's Central
/// Park — the meeting place the cafeterias and restaurants ring… Do not make the park a puny small
/// closet."</i> Then, on the map: <i>"the park needs to Exist there next to the bar. It is an indoor park
/// where the fresh stuff is grown that is served here on the plate."</i> And, on the hall beside it:
/// <i>"it is like cramped… At least double or triple it."</i></para>
///
/// <para>Every guard below walks the REAL generator over the REAL field. Nothing here builds a synthetic
/// room to measure — that is the house's fifth bug class, and a park is exactly the shape of feature it
/// eats: a room whose whole point is its SIZE, measured by a test that made up the size it measured.</para>
/// </summary>
public sealed class TheParkBehindTheBarTests
{
    /// <summary>The shipped sites, both cheat rocks, and the head office — which must NOT have one.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every B1 that is supposed to have one, with the hall it stands behind.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Hall Hall,
        UndergroundComplex.Park Park, UndergroundComplex.FloorPlan Floor)> EveryPark()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || UndergroundComplex.IsHeadOffice(body))
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            UndergroundComplex.Hall? hall = null;
            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (a.Use == UndergroundComplex.Comfort.UpperCanteen && a.Hall is { } h)
                {
                    hall = h;
                }
            }

            Assert.True(hall is not null, $"{body} B{-level}: no hall was carved — nothing to put a park behind.");
            Assert.True(floor.Park is not null,
                $"{body} B{-level}: the bar has its window art and there is NOTHING behind the glass.");
            yield return (body, level, hall!.Value, floor.Park!.Value, floor);
        }
    }

    // ── (a) IT EXISTS, IT IS NEXT TO THE BAR, AND IT IS NOT A CLOSET ──────────────────────────────────

    /// <summary>
    /// THE PARK EXISTS ON B1, SHARES A WALL WITH THE HALL, AND DWARFS IT.
    ///
    /// <para><b>Proven RED</b> by carving it at the size the first sketch had — the band left over behind
    /// the hall, kept to the hall's own width instead of the spine's whole length
    /// (<c>double x0 = hall.X0, x1 = hall.X1;</c> in <c>CarvePark</c>):</para>
    /// <code>
    /// 104 park(s) are not the room the owner asked for:
    ///   luna B1: 4616 du² of park behind 7090 du² of hall — the park is not the biggest room in the building.
    ///   luna B1: the park is narrower than the hall plus its own corridor.
    ///   phobos B1: 4616 du² of park behind 7090 du² of hall — the park is not the biggest room in the building.
    ///   phobos B1: the park is narrower than the hall plus its own corridor.
    ///   …
    ///   titan B1: 2159 du² of park behind 3315 du² of hall — the park is not the biggest room in the building.
    /// </code>
    ///
    /// <para>The area law is stated against the hall rather than as a magic number, because "big" is a
    /// comparison and the thing it is being compared to is standing right there behind the glass.</para>
    /// </summary>
    [Fact]
    public void TheParkIsNextToTheBarAndDwarfsIt()
    {
        var wrong = new List<string>();
        int parks = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park, _)
            in EveryPark())
        {
            parks++;
            double hallFloor = (hall.X1 - hall.X0) * (hall.Y1 - hall.Y0);

            // NOT A CLOSET. Three ways of saying it, and a one-tile park fails all three.
            if (park.FloorDu2 < 1.5 * hallFloor)
            {
                wrong.Add($"  {body} B{-level}: {park.FloorDu2:F0} du² of park behind {hallFloor:F0} du² of "
                    + "hall — the park is not the biggest room in the building.");
            }
            if (park.Y1 - park.Y0 < UndergroundComplex.ParkDepthDu - 0.001)
            {
                wrong.Add($"  {body} B{-level}: the park is only {park.Y1 - park.Y0:F1} du deep.");
            }
            if (park.X1 - park.X0 < (hall.X1 - hall.X0) + (2 * UndergroundComplex.CorridorHalf))
            {
                wrong.Add($"  {body} B{-level}: the park is narrower than the hall plus its own corridor.");
            }

            // ADJACENT, and adjacent the only way a window wall can be: they share a LINE. The park is on
            // the far side of the hall's far wall, so exactly one of these two edges is the same number.
            bool below = Math.Abs(park.Y1 - hall.Y0) < 0.001;
            bool above = Math.Abs(park.Y0 - hall.Y1) < 0.001;
            if (below == above)
            {
                wrong.Add($"  {body} B{-level}: the park's box ({park.Y0:F1}…{park.Y1:F1}) does not share an "
                    + $"edge with the hall's ({hall.Y0:F1}…{hall.Y1:F1}) — there is rock between them.");
            }

            // …and it stands behind the WHOLE hall, not beside a corner of it.
            if (park.X0 > hall.X0 + 0.001 || park.X1 < hall.X1 - 0.001)
            {
                wrong.Add($"  {body} B{-level}: the hall ({hall.X0:F1}…{hall.X1:F1}) sticks out past the "
                    + $"park ({park.X0:F1}…{park.X1:F1}).");
            }
        }

        Assert.True(parks > 40, $"only {parks} parks were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) are not the room the owner asked for:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>The head office has no park, for the same reason its hall wears no bar art and raises no
    /// carriers' canteen card: it is a different building with its own everything (#411). Without this the
    /// sweep above would pass on a game where the park was carved on every floor of every site.</summary>
    [Fact]
    public void TheHeadOfficeHasNoParkAndNoOtherFloorHasOneEither()
    {
        Assert.True(UndergroundComplex.IsHeadOffice("enceladus"), "the sweep no longer holds a head office.");

        int hq = UndergroundComplex.TopPressurisedFloor("enceladus")!.Value;
        Assert.Null(UndergroundComplex.Build("enceladus", hq, Field).Park);
        Assert.Null(UndergroundComplex.ParkArtFor("enceladus", UndergroundComplex.Comfort.UpperCanteen));

        // …and it is the TOP PRESSURISED floor's room and nobody else's — the staff mess is hall-class too
        // and gets no green, because the picture with the window in it is the bar's.
        int floors = 0, parked = 0;
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                if (UndergroundComplex.Build(body, level, Field).Park is not null)
                {
                    parked++;
                    Assert.Equal(UndergroundComplex.TopPressurisedFloor(body), level);
                    Assert.False(UndergroundComplex.IsHeadOffice(body));
                }
            }
        }

        Assert.True(floors > 100, $"only {floors} floors walked — this proved little.");
        Assert.True(parked >= 10, $"only {parked} floors have a park — this proved little.");
    }

    // ── (c) THE WINDOW WALL, AT CORE'S END ────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WALL BETWEEN THE BAR AND THE PARK IS GLASS — published as glazing, NOT as a poured wall, and
    /// standing on exactly the line the two rooms share.
    ///
    /// <para>The other half of the law — that it still STOPS somebody — is the client's, because collision
    /// is the client's. <c>TheParkIsWalkableTests</c> owns it and proves it from both ends: with the glass
    /// gone entirely (<i>"the window wall can be stood on — it is a picture, not a wall"</i>) and with the
    /// glass standing and one doorway-sized hole cut in it (<i>"titan B1: 188 of 189 gravel samples are
    /// STILL reachable with the park's only gate poured shut"</i>).</para>
    /// </summary>
    [Fact]
    public void TheWallBetweenIsGlassAndIsNotAlsoAPouredWall()
    {
        int checkedParks = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryPark())
        {
            checkedParks++;

            SurfaceLayout.Wall glass = Assert.Single(floor.Windows!);
            Assert.Equal(park.Window, glass);

            // It IS the hall's far wall: the shared line, the hall's full width, and nothing more.
            double shared = Math.Abs(park.Y1 - hall.Y0) < 0.001 ? hall.Y0 : hall.Y1;
            Assert.Equal(shared, glass.Y1, 3);
            Assert.Equal(shared, glass.Y2, 3);
            Assert.Equal(hall.X0, Math.Min(glass.X1, glass.X2), 3);
            Assert.Equal(hall.X1, Math.Max(glass.X1, glass.X2), 3);

            // …and it is not ALSO in the poured walls, which would be two segments on one line: one to
            // draw, one to collide, and a day later they would disagree.
            foreach (SurfaceLayout.Wall w in floor.Walls)
            {
                bool sameLine = Math.Abs(w.Y1 - shared) < 0.001 && Math.Abs(w.Y2 - shared) < 0.001;
                bool overlaps = Math.Min(w.X1, w.X2) < hall.X1 - 0.001
                    && Math.Max(w.X1, w.X2) > hall.X0 + 0.001;
                Assert.False(sameLine && overlaps,
                    $"{body} B{-level}: a poured wall ({w.X1:F1}…{w.X2:F1}) lies on the glass line.");
            }

            // THE WAY IN IS SOMEWHERE ELSE. The gate is on the same shared line, it is a doorway the floor
            // publishes (so the deck draws it as a door and the audit can plug it), and it does not overlap
            // the glass by a du.
            Assert.Equal(shared, park.Gate.Y1, 3);
            Assert.Contains(floor.Doorways, d => d == park.Gate);
            Assert.True(park.Gate.X2 <= hall.X0 + 0.001 || park.Gate.X1 >= hall.X1 - 0.001,
                $"{body} B{-level}: the gate is cut THROUGH the window wall.");
            Assert.Equal(2 * UndergroundComplex.DoorHalf, park.Gate.X2 - park.Gate.X1, 3);
        }

        Assert.True(checkedParks > 40, $"only {checkedParks} parks were checked — this proved little.");
    }

    // ── THE PLANTING, AND WHAT IT IS FOR ──────────────────────────────────────────────────────────────

    /// <summary>
    /// SEVERAL RAISED BEDS, ALL OF THEM INSIDE THE PARK, NONE OF THEM ON THE GRAVEL — and every one of them
    /// stencilled with a crop and the room it goes to.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>onTheWalk</c> rejection in <c>CarvePark</c>, which is the
    /// one line that makes a curved path possible on a square grid:</para>
    /// <code>
    /// 846 park(s) have planting laid on the walk:
    ///   luna B1: bed 3 at (-86, -230) stands on the gravel — 1 walk samples are inside it.
    ///   luna B1: bed 4 at (-63, -230) stands on the gravel — 3 walk samples are inside it.
    ///   luna B1: bed 7 at (7, -230) stands on the gravel — 2 walk samples are inside it.
    ///   …
    /// </code>
    ///
    /// <para>…and the client's own flood said the same thing in the language that matters —
    /// <c>767 place(s) in a park cannot be stood in or reached: luna B1: the gravel at (-139, -242) is
    /// solid — something was planted on the path.</c></para>
    /// </summary>
    [Fact]
    public void TheBedsAreInTheParkAndNeverOnTheWalk()
    {
        var wrong = new List<string>();
        int beds = 0, parks = 0;

        foreach ((string body, int level, _, UndergroundComplex.Park park, _) in EveryPark())
        {
            parks++;

            if (park.Beds.Count < 6)
            {
                wrong.Add($"  {body} B{-level}: {park.Beds.Count} raised bed(s) — the owner asked for a park "
                    + "that grows the food, not a lawn with a pot on it.");
            }

            foreach (UndergroundComplex.GrowingBed bed in park.Beds)
            {
                beds++;

                if (!park.Contains(bed.X, bed.Y))
                {
                    wrong.Add($"  {body} B{-level}: bed {bed.Number} at ({bed.X:F0}, {bed.Y:F0}) is outside "
                        + "the park.");
                }

                int on = park.Walk.Count(p => bed.Contains(p.X, p.Y));
                if (on > 0)
                {
                    wrong.Add($"  {body} B{-level}: bed {bed.Number} at ({bed.X:F0}, {bed.Y:F0}) stands on "
                        + $"the gravel — {on} walk samples are inside it.");
                }

                // The whole of the food connection, and it is a stencil: the bed names the room the counter
                // serves in, and the counter's own sign is that room.
                Assert.Contains(bed.Crop, UndergroundComplex.ParkCrops);
                Assert.Contains(UndergroundComplex.ParkBedDestination, bed.Plate, StringComparison.Ordinal);
                Assert.Contains(bed.Crop, bed.Plate, StringComparison.Ordinal);
            }

            // Every crop the list holds is actually planted somewhere in the game.
            Assert.True(park.Beds.Select(b => b.Crop).Distinct().Count() >= 5,
                $"{body} B{-level}: only "
                + $"{park.Beds.Select(b => b.Crop).Distinct().Count()} different things are growing.");

            // …and the benches and the masts stand in the park too, and off the gravel.
            foreach ((double bx, double by) in park.Benches.Concat(park.Masts))
            {
                if (!park.Contains(bx, by))
                {
                    wrong.Add($"  {body} B{-level}: a bench or a mast at ({bx:F0}, {by:F0}) is outside the park.");
                }
            }
            Assert.True(park.Benches.Count >= UndergroundComplex.ParkWalkBends,
                $"{body} B{-level}: {park.Benches.Count} benches for "
                + $"{UndergroundComplex.ParkWalkBends} bends.");
            Assert.NotEmpty(park.Masts);
        }

        Assert.True(parks > 40, $"only {parks} parks were measured — this proved little.");
        Assert.True(beds > 300, $"only {beds} beds were walked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) have planting laid on the walk:\n" + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>THE WALK IS A CURVE AND NOT A CORRIDOR — it leaves the gate, it bends, and it reaches both
    /// ends of a room the eye cannot see the end of. Measured off the published centre-line, which is the
    /// same list the beds were laid around and the same list the A* flood walks.</summary>
    [Fact]
    public void TheWalkCurvesAllTheWayDownTheRoom()
    {
        int parks = 0;

        foreach ((string body, int level, _, UndergroundComplex.Park park, _) in EveryPark())
        {
            parks++;

            Assert.True(park.Walk.Count > 100, $"{body} B{-level}: {park.Walk.Count} walk samples.");
            Assert.All(park.Walk, p => Assert.True(park.Contains(p.X, p.Y),
                $"{body} B{-level}: the walk leaves the park at ({p.X:F0}, {p.Y:F0})."));

            // It starts at the gate.
            (double gx, double gy) = park.Walk[0];
            Assert.Equal((park.Gate.X1 + park.Gate.X2) / 2.0, gx, 1);
            Assert.True(Math.Abs(gy - park.Gate.Y1) < 2.0,
                $"{body} B{-level}: the walk does not start at the gate.");

            // It is a CURVE: the depth it runs at is not one number. A straight path down the middle of the
            // room would have a spread of zero here, which is the shape this guard exists to fail.
            double lo = park.Walk.Min(p => p.Y), hi = park.Walk.Max(p => p.Y);
            Assert.True(hi - lo > 0.35 * (park.Y1 - park.Y0),
                $"{body} B{-level}: the walk wanders {hi - lo:F1} du across a "
                + $"{park.Y1 - park.Y0:F1} du room — that is a corridor, not a park walk.");

            // …and it goes all the way to both ends, which is what makes the far end worth hiding.
            Assert.True(park.Walk.Min(p => p.X) < park.X0 + 10.0);
            Assert.True(park.Walk.Max(p => p.X) > park.X1 - 10.0);
        }

        Assert.True(parks > 40, $"only {parks} walks were measured — this proved little.");
    }

    // ── (d) THE ART SEAM ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The park wears its own picture, the hall wears the one with the window in it, and the panels
    /// the park's floor is cut into tile the box exactly and are shaped like the frames they hold.</summary>
    [Fact]
    public void ThePaintingsAreTheOnesTheOwnerPinned()
    {
        Assert.Equal("art/b1-park-walk.jpg", UndergroundComplex.ParkArtUrl);
        Assert.Equal("art/b1-restaurant-park-view.jpg", UndergroundComplex.CantinaHallArtUrl);
        Assert.Equal(
            UndergroundComplex.CantinaHallArtUrl,
            UndergroundComplex.HallArtFor("luna", UndergroundComplex.Comfort.UpperCanteen));
        Assert.NotEqual(UndergroundComplex.ParkArtUrl, UndergroundComplex.CantinaHallArtUrl);

        int parks = 0;
        foreach ((string body, int level, _, UndergroundComplex.Park park, _) in EveryPark())
        {
            parks++;
            Assert.Equal(UndergroundComplex.ParkArtUrl, park.ArtUrl);

            var panels = UndergroundComplex.ParkArtPanels(park);
            Assert.True(panels.Count >= 2,
                $"{body} B{-level}: one frame stretched over a room "
                + $"{(park.X1 - park.X0) / (park.Y1 - park.Y0):F1} times wider than it is deep.");

            // They tile the box: no gap, no overlap, no overhang.
            Assert.Equal(park.X0, panels[0].X0, 3);
            Assert.Equal(park.X1, panels[^1].X1, 3);
            for (int i = 1; i < panels.Count; i++)
            {
                Assert.Equal(panels[i - 1].X1, panels[i].X0, 3);
            }
            foreach ((double px0, double py0, double px1, double py1) in panels)
            {
                Assert.Equal(park.Y0, py0, 3);
                Assert.Equal(park.Y1, py1, 3);

                // …and each one is roughly the shape of the frame it holds, which is the whole reason the
                // room is cut up at all.
                double aspect = (px1 - px0) / (py1 - py0);
                Assert.InRange(aspect, UndergroundComplex.ParkArtAspect * 0.7,
                    UndergroundComplex.ParkArtAspect * 1.3);
            }
        }

        Assert.True(parks > 40, $"only {parks} parks were painted — this proved little.");
    }

    // ── (f) THE HALL THE OWNER CALLED CRAMPED ─────────────────────────────────────────────────────────

    /// <summary>
    /// THE HALL IS TWO TO FOUR TIMES THE ROOM IT WAS. Owner, standing in the first one: <i>"it is like
    /// cramped… At least double or triple it."</i>
    ///
    /// <para><b>Proven RED</b> by putting the two numbers that grew it back where they were —
    /// <c>HallSpreadFactor = 1.0</c> (the ordinary canteen's density) and <c>HallRibExtraDu = 0</c> (a hall's
    /// rib no longer than anybody else's):</para>
    /// <code>
    /// 53 hall(s) are still the cramped room:
    ///   luna B1: 2654 du² over 20 tops — 133 du² a table
    ///   phobos B1: 2654 du² over 20 tops — 133 du² a table
    ///   …
    ///   titan B1: 2493 du² over 20 tops — 125 du² a table
    /// </code>
    ///
    /// <para>Two laws, because the two failures are different rooms: a hall can be big and still packed
    /// (the owner's actual complaint was the PITCH), and it can be roomy per table and still be a corridor.
    /// Both numbers are measured off the carved box and the laid tops, never off the constants above.</para>
    /// </summary>
    [Fact]
    public void TheHallIsNoLongerTheCrampedRoom()
    {
        var wrong = new List<string>();
        int halls = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities)
            {
                if (a.Use != UndergroundComplex.Comfort.UpperCanteen || a.Hall is not { } hall)
                {
                    continue;
                }

                halls++;
                double floor = (hall.X1 - hall.X0) * (hall.Y1 - hall.Y0);
                double perTop = floor / Math.Max(1, a.Tables.Count);

                if (floor < 3000)
                {
                    wrong.Add($"  {body} B{-level}: {floor:F0} du² over {a.Tables.Count} tops — "
                        + $"{perTop:F0} du² a table");
                }
                else if (perTop < 150)
                {
                    wrong.Add($"  {body} B{-level}: {floor:F0} du² over {a.Tables.Count} tops — "
                        + $"{perTop:F0} du² a table, which is the pitch he called cramped");
                }
            }
        }

        Assert.True(halls > 40, $"only {halls} halls were measured — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} hall(s) are still the cramped room:\n" + string.Join("\n", wrong.Take(20)));

        // The two numbers the growth is made of, so a silent revert of either reads here as well as in the
        // measurements above.
        Assert.True(UndergroundComplex.HallSpreadFactor >= 3.0);
        Assert.True(UndergroundComplex.HallRibExtraDu > 0);
    }

    // ── THE PROSE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every sentence the park adds, verbatim — and not one of them says what the FACILITY is for.
    /// §13.8: the park is allowed to say what the KITCHEN is for, and that is the whole of what it says.</summary>
    [Fact]
    public void TheParksProseIsWiredVerbatimAndEXPLAINSNothing()
    {
        Assert.Equal(
            "🌳 THE PARK · RECREATION SCHEDULE POSTED · ATTENDANCE IS RECORDED",
            UndergroundComplex.ParkPlate);

        Assert.Equal(
            "An indoor park behind the canteen's glass: gravel walks, raised beds under grow-lamps, and a "
            + "plate at the gate that says attendance is recorded. The beds are stencilled for the counter — "
            + "including the stew the card sources from as far down as they are willing to say, which is "
            + "growing ten metres from the table it is served at.",
            UndergroundComplex.ParkNote);

        Assert.Equal("🪑 A STEEL BENCH", UndergroundComplex.ParkBenchPlate);

        Assert.Equal(
            ["TABLE GREENS", "STEW ROOT", "BREAKFAST TOMATO", "SOFT HERBS", "SALAD STOCK",
             "COFFEE · SIX TREES · TRIAL"],
            UndergroundComplex.ParkCrops);

        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];
        var everything = new List<string>
        {
            UndergroundComplex.ParkPlate, UndergroundComplex.ParkNote,
            UndergroundComplex.ParkBenchPlate, UndergroundComplex.ParkBedDestination,
        };
        everything.AddRange(UndergroundComplex.ParkCrops);
        foreach (string text in everything)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        // THE FOOD CONNECTION, AS ONE FACT AND NOT AS A SENTENCE: the bed's destination is the counter's own
        // sign, and the counter really does sell the thing the note names. If either side is reworded this
        // goes red, which is the only way a loop closed by a stencil can be defended.
        Assert.Contains(
            UndergroundComplex.ParkBedDestination,
            UndergroundComplex.AmenitySigns("luna", UndergroundComplex.Comfort.UpperCanteen).Plate,
            StringComparison.Ordinal);
        Assert.Contains(
            Interior.CounterService.Card,
            d => d.Flavor.Contains("as far down as we are willing to say", StringComparison.Ordinal));
    }

    // ---- #775 THE THOROUGHFARE ----------------------------------------------------------------------

    /// <summary>
    /// THE PARK IS A WAY THROUGH, NOT A CUL-DE-SAC — several gates, on the corridors that actually reach
    /// it, and never one cut through the glass.
    ///
    /// <para>Owner, 2026-08-09: <i>"let's have multiple doors to the park... it is a kind of place people
    /// like to walk through on their way."</i> #790 shipped it with ONE gate at the end of one corridor,
    /// which makes the largest room in the game a destination and a dead end.</para>
    ///
    /// <para>The law is stated against the building rather than as a number: EVERY rib pointing the park's
    /// way opens into it (so a route down one rib and up another crosses the green), and at least one gate
    /// is not a rib at all -- the garden walk, which exists because the ribs' directions are seeded and on
    /// a quarter of the shipped sites exactly one of them falls this way.</para>
    ///
    /// <para><b>Proven RED</b> against the world as #790 shipped it -- the gate list handed to CarvePark cut
    /// back to the hall's own rib (<c>parkGateXs.Clear(); parkGateXs.Add(ribList[slot.Rib].X);</c>):</para>
    /// <code>
    /// 52 park(s) are a cul-de-sac rather than a way through:
    ///   luna B1: 1 way(s) in -- the owner asked for more than one.
    ///   luna B1: the rib at -103.4 points at the park and does not open into it.
    ///   luna B1: the rib at -54.2 points at the park and does not open into it.
    ///   luna B1: no gate that is not a rib -- the garden walk was never cut.
    ///   ...
    /// </code>
    /// </summary>
    [Fact]
    public void TheParkIsAThoroughfare()
    {
        var wrong = new List<string>();
        int parks = 0, ways = 0;
        var seen = new HashSet<int>();
        (double _, double shaftY) = UndergroundComplex.ShaftAt(Field);

        foreach ((string body, int level, UndergroundComplex.Hall hall, UndergroundComplex.Park park,
            UndergroundComplex.FloorPlan floor) in EveryPark())
        {
            parks++;
            ways += park.Ways.Count;
            seen.Add(park.Ways.Count);

            if (park.Ways.Count < 2)
            {
                wrong.Add($"  {body} B{-level}: {park.Ways.Count} way(s) in - the owner asked for more "
                    + "than one.");
            }

            // The line every gate is cut in: the park's own near wall, which is whichever of its two long
            // edges faces the spine.
            double near = park.Y0 > shaftY ? park.Y0 : park.Y1;

            foreach (SurfaceLayout.Doorway g in park.Ways)
            {
                double gx = (g.X1 + g.X2) / 2.0;
                if (Math.Abs(g.Y1 - near) > 0.001 || Math.Abs(g.Y2 - near) > 0.001)
                {
                    wrong.Add($"  {body} B{-level}: a gate at y={g.Y1:F1} is not in the park's near wall "
                        + $"(y={near:F1}).");
                }
                if (Math.Abs((g.X2 - g.X1) - (2 * UndergroundComplex.DoorHalf)) > 0.001)
                {
                    wrong.Add($"  {body} B{-level}: a gate is {g.X2 - g.X1:F1} du wide.");
                }
                if (gx > hall.X0 - 0.001 && gx < hall.X1 + 0.001)
                {
                    wrong.Add($"  {body} B{-level}: the gate at x={gx:F1} is cut THROUGH the window wall.");
                }
                if (!floor.Doorways.Any(d => d == g))
                {
                    wrong.Add($"  {body} B{-level}: the gate at x={gx:F1} is one the park knows about and "
                        + "the plan does not draw.");
                }

                // AND IT LEADS SOMEWHERE: nothing poured across it, on its own line.
                foreach (SurfaceLayout.Wall w in floor.Walls)
                {
                    bool onTheLine = Math.Abs(w.Y1 - near) < 0.05 && Math.Abs(w.Y2 - near) < 0.05;
                    bool across = Math.Min(w.X1, w.X2) < g.X2 - 0.2 && Math.Max(w.X1, w.X2) > g.X1 + 0.2;
                    if (onTheLine && across)
                    {
                        wrong.Add($"  {body} B{-level}: the gate at x={gx:F1} has a wall across it.");
                        break;
                    }
                }
            }

            // EVERY CORRIDOR THAT REACHES IT OPENS INTO IT. That is the whole of "people walk through on
            // their way": a rib that stops in rock beside the green is a route that goes round.
            bool side = park.Y1 < shaftY;
            foreach (UndergroundComplex.Rib r in floor.Ribs)
            {
                if (r.Down == side && !park.Ways.Any(g => Math.Abs(((g.X1 + g.X2) / 2.0) - r.X) < 0.001))
                {
                    wrong.Add($"  {body} B{-level}: the rib at {r.X:F1} points at the park and does not "
                        + "open into it.");
                }
            }

            // ...and one of them is NOT a rib. The floor's dice decide how many ribs fall this way; the
            // garden walk is why "multiple" is a law rather than a hope.
            if (!park.Ways.Any(g => !floor.Ribs.Any(
                    r => Math.Abs(((g.X1 + g.X2) / 2.0) - r.X) < 0.001)))
            {
                wrong.Add($"  {body} B{-level}: no gate that is not a rib - the garden walk was never cut.");
            }

            // No two gates are the same gate.
            for (int a = 0; a < park.Ways.Count; a++)
            {
                for (int b = a + 1; b < park.Ways.Count; b++)
                {
                    if (Math.Abs(park.Ways[a].X1 - park.Ways[b].X1) < 2 * UndergroundComplex.DoorHalf)
                    {
                        wrong.Add($"  {body} B{-level}: two gates share a jamb at x={park.Ways[a].X1:F1}.");
                    }
                }
            }

            // The hall's own gate is still the FIRST one, which is what the room's plate and its dev route
            // are pinned to.
            Assert.Equal(park.Gate, park.Ways[0]);
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) are a cul-de-sac rather than a way through:\n"
            + string.Join("\n", wrong.Take(20)));
        Assert.True(parks > 40, $"only {parks} parks were checked - this proved little.");
        Assert.True(ways > 100, $"only {ways} gates were checked - this proved little.");
        Assert.True(seen.Count > 1,
            "every park in the game has the same number of gates - the ribs are doing nothing: "
            + string.Join(", ", seen.Order()));
    }
}
