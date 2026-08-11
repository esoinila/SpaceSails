using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #801 · THE PARK IS NOT THE EDGE OF THE MAP.
///
/// <para>Owner, 2026-08-09: <i>"we could have rooms to explore below the park also (on the map). Walking
/// through the park is fun, it should not be the edge."</i></para>
///
/// <para>#775 made the green a thoroughfare between corridors; it was still a room with a painted wall down
/// one whole side, and a crossing that ends at a horizon is a crossing you do once. The far wall is a row
/// of doors now, and behind them is the back of house a park of this size actually has: potting, soil,
/// feed, a cold room the counter draws on.</para>
///
/// <para>Every guard below walks the REAL generator over the REAL field. The Client half
/// (<c>TheFarGatesLeadSomewhereTests</c>) proves the other half of the same law — that the doors drawn here
/// are doors the boots can walk through — because a room that is on the plan and sealed in the collision
/// field is this ground's most expensive recurring bug and a park is exactly its shape.</para>
/// </summary>
public sealed class TheParkIsNotTheEdgeTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park,
        UndergroundComplex.FloorPlan Floor)> EveryPark()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level
                || UndergroundComplex.IsHeadOffice(body))
            {
                continue;
            }
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            if (floor.Park is { } park)
            {
                yield return (body, level, park, floor);
            }
        }
    }

    /// <summary>
    /// EVERY PARK HAS ROOMS BEHIND IT, EACH WITH A DOOR CUT IN THE FAR WALL, AND THE WALL IS OPEN ACROSS
    /// EVERY ONE OF THEM.
    ///
    /// <para><b>Proven RED</b> by returning an empty list from the back-of-house carve — i.e. the game as
    /// it shipped before this PR:</para>
    /// <code>
    /// 52 park(s) are still the edge of the map:
    ///   luna B1: the far wall has nothing behind it — the park is where the building stops.
    ///   phobos B1: the far wall has nothing behind it — the park is where the building stops.
    ///   …
    /// </code>
    ///
    /// <para>And <b>proven RED the other way</b>, which is the half that matters: with the rooms carved and
    /// the far wall left poured as one segment (the wall builder's old single <c>walls.Add</c>),
    /// <c>208 door(s) in the far wall have a wall lying across them</c> — the drawn-versus-walked bug class,
    /// caught in Core before the client ever sees it.</para>
    /// </summary>
    [Fact]
    public void EveryParkHasRoomsBehindItAndTheFarWallIsOpenAcrossTheirDoors()
    {
        var wrong = new List<string>();
        int parks = 0, rooms = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.FloorPlan floor)
            in EveryPark())
        {
            parks++;
            if (park.Rooms.Count == 0)
            {
                wrong.Add($"  {body} B{-level}: the far wall has nothing behind it — the park is where the "
                    + "building stops.");
                continue;
            }

            // Which line the far wall is: the park's own edge AWAY from the hall it stands behind.
            double near = Math.Abs(park.Y1 - park.Window.Y1) < 0.001 ? park.Y1 : park.Y0;
            double far = Math.Abs(near - park.Y1) < 0.001 ? park.Y0 : park.Y1;

            foreach (UndergroundComplex.BackRoom br in park.Rooms)
            {
                rooms++;

                // (a) It is on the FAR side. A back room laid on the near side would be a room in the
                //     corridor the park is entered from.
                if (Math.Abs(br.Door.Y1 - far) > 0.001 || Math.Abs(br.Door.Y2 - far) > 0.001)
                {
                    wrong.Add($"  {body} B{-level}: a back door is on {br.Door.Y1:F1} and the far wall is "
                        + $"on {far:F1}.");
                }
                if (park.Contains(br.X, br.Y))
                {
                    wrong.Add($"  {body} B{-level}: a back room's middle is INSIDE the park.");
                }

                // (b) The door is a real door, published like every other door down here.
                if (Math.Abs((br.Door.X2 - br.Door.X1) - (2 * UndergroundComplex.DoorHalf)) > 0.001)
                {
                    wrong.Add($"  {body} B{-level}: a back door is {br.Door.X2 - br.Door.X1:F1} du wide.");
                }
                if (!floor.Doorways.Any(d =>
                        Math.Abs(d.X1 - br.Door.X1) < 0.001 && Math.Abs(d.Y1 - br.Door.Y1) < 0.001
                        && Math.Abs(d.X2 - br.Door.X2) < 0.001))
                {
                    wrong.Add($"  {body} B{-level}: a back door is not on the floor's doorway list.");
                }

                // (c) NOTHING IS LYING ACROSS IT. §13.2's own law, said about the one wall this PR taught
                //     to have holes in it.
                foreach (SurfaceLayout.Wall w in floor.Walls)
                {
                    if (Math.Abs(w.Y1 - far) > 0.001 || Math.Abs(w.Y2 - far) > 0.001)
                    {
                        continue;
                    }
                    double lo = Math.Min(w.X1, w.X2), hi = Math.Max(w.X1, w.X2);
                    if (lo < br.Door.X2 - 0.01 && hi > br.Door.X1 + 0.01)
                    {
                        wrong.Add($"  {body} B{-level}: a wall ({lo:F1}…{hi:F1}) lies across the back door "
                            + $"at {br.Door.X1:F1}…{br.Door.X2:F1}.");
                    }
                }

                // (d) It is a room, in the list every room in the building is in — so the A* audit that
                //     walks every room from the car walks this one too, without being told about parks.
                if (!floor.RoomCentres.Any(c =>
                        Math.Abs(c.X - br.X) < 0.001 && Math.Abs(c.Y - br.Y) < 0.001))
                {
                    wrong.Add($"  {body} B{-level}: a back room is not on the room list — nothing audits it.");
                }

                // (e) Its plate is one of the authored ones, and it is signed on the plan.
                if (!UndergroundComplex.ParkBackPlates.Contains(br.Plate))
                {
                    wrong.Add($"  {body} B{-level}: '{br.Plate}' is not one of the authored plates.");
                }
                if (!floor.Labels.Any(l => string.Equals(l.Label, br.Plate, StringComparison.Ordinal)))
                {
                    wrong.Add($"  {body} B{-level}: '{br.Plate}' is on no wall.");
                }
            }

            // Two neighbours never share a jamb, and every one of them clears the floodlight masts — the
            // bays BETWEEN the masts are where they are laid, and a door in front of a lamp post would be
            // the one obstruction the park's own furniture could put in this feature's way.
            foreach (UndergroundComplex.BackRoom a in park.Rooms)
            {
                foreach (UndergroundComplex.BackRoom b in park.Rooms)
                {
                    if (!a.Equals(b) && a.X0 < b.X1 - 0.001 && a.X1 > b.X0 + 0.001)
                    {
                        wrong.Add($"  {body} B{-level}: two back rooms overlap.");
                    }
                }
                foreach ((double mx, double _) in park.Masts)
                {
                    if (mx > a.Door.X1 - 1.0 && mx < a.Door.X2 + 1.0)
                    {
                        wrong.Add($"  {body} B{-level}: a floodlight mast stands in a back door.");
                    }
                }
            }
        }

        Assert.True(parks > 40, $"only {parks} parks were measured — this proved little.");

        // The substantive report first, then the anti-vacuous count. Ordered that way on purpose: with the
        // carve gone it is the BUILDING that has gone wrong, and a guard that leads with "only 0 back rooms
        // were measured" is telling the truth about itself instead of about the building.
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} park(s) are still the edge of the map:\n" + string.Join("\n", wrong.Take(20)));
        Assert.True(rooms > 100, $"only {rooms} back rooms were measured — this proved little.");
    }

    /// <summary>
    /// THE BAND CAME OUT OF THE FIELD AND NOT OUT OF THE PARK.
    ///
    /// <para>The park's own size law (#759) is the constraint this feature had to be built around: it must
    /// stand <see cref="UndergroundComplex.ParkDepthDu"/> deep and hold half again the floor of the hall
    /// behind it, and on the shipped field the second of those binds at 38.3 of its 42 du. So the rooms
    /// behind it may not have been bought by making it smaller, and this is the guard that says so — the
    /// park is exactly the room it was before this PR, to three decimal places.</para>
    ///
    /// <para>The other half: nothing left the surface's own envelope. That law already exists
    /// (<c>TheHiveTests.ItNEVERLeavesTheSurfacesOwnEnvelope</c>) and is restated here on the sweep this
    /// feature is about, with the rock that is left over reported, because "the last strip of the field"
    /// is exactly the kind of claim that is true until somebody adds two du to a module.</para>
    /// </summary>
    [Fact]
    public void TheBandCameOutOfTheFieldAndNotOutOfThePark()
    {
        var wrong = new List<string>();
        int parks = 0;
        double thinnestRock = double.MaxValue;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.FloorPlan floor)
            in EveryPark())
        {
            parks++;

            // The park is untouched.
            if (park.Y1 - park.Y0 < UndergroundComplex.ParkDepthDu - 0.001)
            {
                wrong.Add($"  {body} B{-level}: the park is only {park.Y1 - park.Y0:F1} du deep — the back "
                    + "of house was paid for out of the green.");
            }

            foreach (UndergroundComplex.BackRoom br in park.Rooms)
            {
                if (Math.Abs((br.Y1 - br.Y0) - UndergroundComplex.ParkBackDepthDu) > 0.001)
                {
                    wrong.Add($"  {body} B{-level}: a back room is {br.Y1 - br.Y0:F1} du deep and the module "
                        + $"is {UndergroundComplex.ParkBackDepthDu:F1}.");
                }
                thinnestRock = Math.Min(
                    thinnestRock,
                    Math.Min(br.Y0 - Field.BottomY, Field.LandingBandY - br.Y1));
            }

            foreach (SurfaceLayout.Wall w in floor.Walls)
            {
                if (Math.Min(w.Y1, w.Y2) < Field.BottomY - 0.001
                    || Math.Max(w.Y1, w.Y2) > Field.LandingBandY + 0.001)
                {
                    wrong.Add($"  {body} B{-level}: a wall left the field at "
                        + $"({w.X1:F1},{w.Y1:F1})–({w.X2:F1},{w.Y2:F1}).");
                    break;
                }
            }
        }

        Assert.True(parks > 40, $"only {parks} parks were measured — this proved little.");
        Assert.True(thinnestRock >= UndergroundComplex.ParkBackRockDu - 0.001,
            $"only {thinnestRock:F1} du of rock left between the back wall and the end of the field, and "
            + $"the law wants {UndergroundComplex.ParkBackRockDu:F1}.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} problem(s) with where the band came from:\n" + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>
    /// THE BACK DOORS ARE NOT WAYS THROUGH THE PARK, AND THE AMENITIES' CONSERVATION SUM STILL BALANCES.
    ///
    /// <para><see cref="UndergroundComplex.Park.Ways"/> is the list of ways ACROSS the green, corridor to
    /// corridor (#775), and <c>TheHiveAmenitiesTests.CarvingTheAmenitiesNeverEMPTIESAFloor</c> counts each
    /// one as a place with a doorway. A back door is a way OUT of the park into a room, and the room is
    /// already counted — publishing it on <c>Ways</c> would have made the sum count it twice, which is a
    /// guard going red for a reason that has nothing to do with the bug it was written for.</para>
    ///
    /// <para>#817 · THE COUNT OF BACK DOORS IS NO LONGER THE COUNT OF BACK ROOMS, and the change is
    /// deliberate rather than a loosening. The owner ruled that a premium suite on a garden gets a door onto
    /// the garden, so the NEAR band's rooms now have one too — and <c>BackDoors</c> means "the doors off the
    /// gravel", which is what the sealing experiment in <c>TheParkIsWalkableTests</c> plugs. What is asserted
    /// instead is the containment that actually carries the law: every back room's own door is still in the
    /// list, and the list is still disjoint from <c>Ways</c>.</para>
    /// </summary>
    [Fact]
    public void TheBackDoorsAreNotWaysThroughAndTheSumStillBalances()
    {
        int parks = 0, ways = 0, backs = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _) in EveryPark())
        {
            parks++;
            ways += park.Ways.Count;
            backs += park.BackDoors.Count;

            Assert.True(park.BackDoors.Count >= park.Rooms.Count,
                $"{body} B{-level}: {park.Rooms.Count} rooms open off the gravel and only "
                + $"{park.BackDoors.Count} doors are published in the wall they open through.");
            foreach (UndergroundComplex.BackRoom room in park.Rooms)
            {
                Assert.Contains(room.Door, park.BackDoors);
            }

            foreach (SurfaceLayout.Doorway back in park.BackDoors)
            {
                Assert.DoesNotContain(park.Ways, w =>
                    Math.Abs(w.X1 - back.X1) < 0.001 && Math.Abs(w.Y1 - back.Y1) < 0.001);
            }
            Assert.False(park.Ways.Count == 0, $"{body} B{-level}: the park has no way through it at all.");
        }

        Assert.True(parks > 40 && ways > 80 && backs > 100,
            $"{parks} parks, {ways} ways, {backs} back doors — this proved little.");
    }
}
