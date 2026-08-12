using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #822 · THE FIRE CODE, SWEPT OVER THE WHOLE BUILDING.
///
/// <para>Owner, evening playtest 2026-08-11, standing in a room with one leaf in it:</para>
///
/// <list type="bullet">
/// <item><i>"Let's have a standing rule that no space except small bedroom can only have one door… it's a
/// fire hazard otherwise."</i></item>
/// <item>and on the labs, the following afternoon: <i>"the second exit is itself hidden — a service crawl or
/// second hidden door, found the way the first one is. The lab obeys the code the building pretends to
/// follow: two doors nobody can see."</i></item>
/// </list>
///
/// <para>One-door rooms read as traps because they ARE traps, and a second way out is also the better level:
/// a route, a flank, somewhere to be that is not the doorway a guard is standing in. #817 put the law into
/// the ring's own carve; this walks EVERYTHING ELSE — the chambers off every rib, the halls, the negotiating
/// cabinets, the back of house, the washroom, the cells — on every floor of every named site and every
/// generated moon in the sweep.</para>
///
/// <para><b>Stated against a list the building keeps.</b> Every room hands over the box its walls were laid
/// on and the gaps its own carver cut in them (<see cref="UndergroundComplex.Room"/>), the way the hall and
/// the ring have published theirs since #775/#817. A sweep that reconstructed rooms by firing rays at the
/// wall list would walk out through the very doorway it was trying to count, and a sweep stated against
/// coordinates typed in here would be the house's fifth bug class. The threshold and the count are asked of
/// Core (<see cref="UndergroundComplex.FireCodeSmallRoomDu"/>,
/// <see cref="UndergroundComplex.FireCodeMinExits"/>) rather than restated.</para>
///
/// <para><b>Proven RED on the world of 2026-08-12</b> by taking the fire recesses and the cabinets' second
/// leaf back out of the carve (<c>continue;</c> at the top of the recess pass, and <c>atV = [vMid]</c>):</para>
/// <code>
/// 2158 room(s) have only one way out:
///   luna B4 · 15.0 x 12.0 du (CREW MUSTER) at (163.6,-33.0) — 1 way out, the code wants 2.
///   luna B4 · 15.0 x 12.0 du (BONDED HOLD) at (163.6,-65.0) — 1 way out, the code wants 2.
///   ...
///   luna B1 · 27.9 x 10.0 du (CABINET 1 · ENGAGED WHEN LIT) at (238.2,-14.5) — 1 way out, the code wants 2.
/// </code>
/// </summary>
public sealed class NoRoomHasOnlyOneWayOutTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every floor of every site in the sweep, built by the real generator over the real field.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.FloorPlan Floor)> EveryFloor()
    {
        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                yield return (body, level, UndergroundComplex.Build(body, level, Field));
            }
        }
    }

    private static string Say(string body, int level, UndergroundComplex.Room room) =>
        string.Create(CultureInfo.InvariantCulture,
            $"  {body} B{-level} · {room.WidthDu:F1} x {room.DepthDu:F1} du"
            + $"{(room.Plate.Length > 0 ? $" ({room.Plate})" : "")} at ({room.X:F1},{room.Y:F1})"
            + $" — {room.Exits} way(s) out, the code wants {UndergroundComplex.FireCodeMinExits}.");

    /// <summary>
    /// THE LAW ITSELF. Every published room on every floor plan of every site, and not one of them is a box
    /// with a single hole in it unless it is bedroom-small.
    ///
    /// <para>Anti-vacuity is two counts and both have to move: the sweep must have measured thousands of
    /// rooms it holds to the law, AND it must have let some off — a threshold that exempts everything is
    /// the guard that asserts nothing (the house's fifth bug class), and so is one that exempts nothing,
    /// because then the exemption #821's lock stands on is not being exercised at all.</para>
    /// </summary>
    [Fact]
    public void NoSpaceACaptainCanStandInHasOnlyOneWayOut()
    {
        var wrong = new List<(double Area, string Line)>();
        int held = 0, exempt = 0, floors = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            floors++;
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (room.BedroomSmall)
                {
                    exempt++;
                    continue;
                }
                held++;
                if (!room.MeetsFireCode)
                {
                    wrong.Add((room.FloorDu2, Say(body, level, room)));
                }
            }
        }

        Assert.True(floors > 200, $"only {floors} floors were built — this proved little.");
        Assert.True(held > 1000, $"only {held} room(s) were held to the code — this proved little.");
        Assert.True(
            exempt > 0,
            "not one room was let off as bedroom-small, so the exemption #821's locked cubicle stands on "
            + "was never exercised — a threshold nothing falls under is a threshold nobody is testing.");

        // Worst first: a big room with one door is the one the owner walked into.
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} room(s) have only one way out:\n"
            + string.Join("\n", wrong.OrderByDescending(w => w.Area).Take(20).Select(w => w.Line)));
    }

    /// <summary>
    /// …AND THE EXEMPTION IS THE ONE THAT WAS RULED ON, not a hole big enough to drive the law through. A
    /// room let off with one door is a cell, a booth or a cubicle: nothing wider than
    /// <see cref="UndergroundComplex.FireCodeSmallRoomDu"/> on its longest wall, which is the en-suite's own
    /// eight du and the width of #821's cubicle.
    /// </summary>
    [Fact]
    public void OnlyTheBedroomSmallRoomsAreLetOff()
    {
        var wrong = new List<string>();
        int exempt = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                if (!room.BedroomSmall)
                {
                    continue;
                }
                exempt++;
                if (room.LongestSideDu > UndergroundComplex.FireCodeSmallRoomDu)
                {
                    wrong.Add(Say(body, level, room));
                }
                if (room.Exits == 0)
                {
                    wrong.Add($"  {body} B{-level}: a {room.LongestSideDu:F1} du room with NO way out at "
                        + $"({room.X:F1},{room.Y:F1}) — the exemption is one door, not none.");
                }
            }
        }

        Assert.True(exempt > 100, $"only {exempt} room(s) were let off — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} exempted room(s) are not bedroom-small:\n" + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>
    /// A WAY OUT IS A HOLE IN THE ROOM'S OWN WALL — the guard that stops the law above from being satisfied
    /// by a list.
    ///
    /// <para>This is #585's lesson turned on the fire code: a room could publish two ways, pass the count,
    /// and have both of them opening onto a wall somebody else laid on the same line. So every published way
    /// is checked twice — that it lies ON the room's own perimeter, and that no wall on the floor covers the
    /// span it claims. A second exit that is walled over is the drawn world and the walked world disagreeing,
    /// which is the bug class this ground keeps paying for.</para>
    /// </summary>
    [Fact]
    public void EveryPublishedWayIsAnActualGapInAnActualWall()
    {
        var wrong = new List<string>();
        int checkedWays = 0;

        foreach ((string body, int level, UndergroundComplex.FloorPlan floor) in EveryFloor())
        {
            foreach (UndergroundComplex.Room room in floor.TheRooms)
            {
                foreach (SurfaceLayout.Doorway way in room.Ways)
                {
                    checkedWays++;
                    double mx = (way.X1 + way.X2) / 2.0, my = (way.Y1 + way.Y2) / 2.0;
                    bool onEdge =
                        (Near(mx, room.X0) || Near(mx, room.X1)) && my >= room.Y0 - 0.01 && my <= room.Y1 + 0.01;
                    onEdge |=
                        (Near(my, room.Y0) || Near(my, room.Y1)) && mx >= room.X0 - 0.01 && mx <= room.X1 + 0.01;
                    if (!onEdge)
                    {
                        wrong.Add($"  {body} B{-level}: a way at ({mx:F1},{my:F1}) is not on the perimeter of "
                            + $"the room it belongs to ({room.X0:F1},{room.Y0:F1})-({room.X1:F1},{room.Y1:F1}).");
                        continue;
                    }

                    if (Walled(floor.Walls, way))
                    {
                        wrong.Add($"  {body} B{-level}: the way at ({mx:F1},{my:F1}) is walled over — "
                            + "the room publishes an exit nothing can pass.");
                    }
                }
            }
        }

        Assert.True(checkedWays > 2000, $"only {checkedWays} way(s) were checked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} published way(s) are not gaps:\n" + string.Join("\n", wrong.Take(20)));

        static bool Near(double a, double b) => Math.Abs(a - b) < 0.01;

        // Does any wall lying on this way's own line cover the middle of it? Only walls collinear with the
        // gap can shut it — a wall crossing it is furniture standing in a doorway, which is a different law
        // (#585's clearances) and has its own guards.
        static bool Walled(IReadOnlyList<SurfaceLayout.Wall> walls, SurfaceLayout.Doorway way)
        {
            double mx = (way.X1 + way.X2) / 2.0, my = (way.Y1 + way.Y2) / 2.0;
            bool vertical = Math.Abs(way.X1 - way.X2) < 0.01;
            foreach (SurfaceLayout.Wall w in walls)
            {
                if (vertical)
                {
                    if (Math.Abs(w.X1 - mx) > 0.01 || Math.Abs(w.X2 - mx) > 0.01)
                    {
                        continue;
                    }
                    if (my > Math.Min(w.Y1, w.Y2) + 0.01 && my < Math.Max(w.Y1, w.Y2) - 0.01)
                    {
                        return true;
                    }
                }
                else
                {
                    if (Math.Abs(w.Y1 - my) > 0.01 || Math.Abs(w.Y2 - my) > 0.01)
                    {
                        continue;
                    }
                    if (mx > Math.Min(w.X1, w.X2) + 0.01 && mx < Math.Max(w.X1, w.X2) - 0.01)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// THE LABS OBEY THE CODE THE BUILDING PRETENDS TO FOLLOW — two ways out of every chamber in the
    /// mountain, and the second one out of the deepest is itself hidden.
    ///
    /// <para>Owner's ruling, 2026-08-11: <i>"the second exit is itself hidden — a service crawl or second
    /// hidden door, found the way the first one is. The lab obeys the code the building pretends to follow:
    /// two doors nobody can see."</i> There is no exemption clause here — the labs are swept exactly as the
    /// facility is, and the only difference is that neither of the heart's doors is on anything.</para>
    ///
    /// <para><b>Proven RED</b> by taking the crawl back out (returning the heart's back wall to the single
    /// solid segment it was): <c>THE HEART has 1 way(s) out and the fire code wants 2 — and it is the one
    /// room in the game a lockdown can seal a captain into.</c></para>
    /// </summary>
    [Fact]
    public void EveryLabChamberHasTwoWaysOutAndTheHeartsSecondIsHidden()
    {
        var wrong = new List<string>();
        int labs = 0;

        foreach (string body in Sweep())
        {
            SecretLab.Placement placement = SecretLab.For(body, Field, forcePresent: true);
            SecretLab.Region region = SecretLab.Build(body, Field, placement.DoorX, placement.DoorY);
            labs++;

            // The chambers, shallow to deep. A chamber's ways are the door INTO it, the door OUT of it into
            // the next one, and whatever it has that is on no board at all — asked of the region's own
            // published lists, never counted off the wall segments.
            for (int c = 0; c < SecretLab.ChamberNames.Count; c++)
            {
                string name = SecretLab.ChamberNames[c];
                int ways = c == 0
                    ? 1                                     // the hidden front door — the way you came in
                    : region.Doors.Count(d => d.Deeper == name);
                if (c + 1 < SecretLab.ChamberNames.Count)
                {
                    ways += region.Doors.Count(d => d.Deeper == SecretLab.ChamberNames[c + 1]);
                }
                ways += region.TheHidden.Count(h => h.Chamber == name);

                if (ways < UndergroundComplex.FireCodeMinExits)
                {
                    wrong.Add($"  {body}: {name} has {ways} way(s) out and the fire code wants "
                        + $"{UndergroundComplex.FireCodeMinExits} — and it is the one room in the game a "
                        + "lockdown can seal a captain into.");
                }
            }

            // …and the deepest one's second way is HIDDEN: never a lab door, so the board cannot list it and
            // the lockdown cannot throw it.
            SecretLab.HiddenWay[] deep = [.. region.TheHidden.Where(h => h.Chamber == SecretLab.ChamberNames[^1])];
            if (deep.Length == 0)
            {
                wrong.Add($"  {body}: the heart has no hidden way out at all.");
                continue;
            }
            foreach (SecretLab.HiddenWay way in deep)
            {
                if (region.Doors.Any(d => d.Id == way.Id))
                {
                    wrong.Add($"  {body}: the crawl {way.Id} is on the door board — a hidden way that a "
                        + "lockdown can throw is not hidden and is not a way out.");
                }

                // IT IS A REAL GAP: the region's own wall list leaves it open…
                if (Covers(region.Walls, way.X, way.Y))
                {
                    wrong.Add($"  {body}: the crawl at ({way.X:F1},{way.Y:F1}) is walled over in the "
                        + "region's own geometry — nothing could ever pass it.");
                }

                // …and the PLUG shuts it, so nothing is walkable that has not been found. Both halves
                // matter: a gap with no plug is a mountain with an open back door.
                if (!Covers([way.Plug], way.X, way.Y))
                {
                    wrong.Add($"  {body}: the crawl's plug does not stand in its own gap at "
                        + $"({way.X:F1},{way.Y:F1}) — the lab is open before anybody found anything.");
                }
            }
        }

        Assert.True(labs > 40, $"only {labs} lab(s) were built — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} lab chamber(s) break the code:\n" + string.Join("\n", wrong.Take(20)));

        // Does any wall lie across this point?
        static bool Covers(IReadOnlyList<SurfaceLayout.Wall> walls, double x, double y)
        {
            foreach (SurfaceLayout.Wall w in walls)
            {
                bool vertical = Math.Abs(w.X1 - w.X2) < 0.01;
                if (vertical
                    && Math.Abs(w.X1 - x) < 0.01
                    && y > Math.Min(w.Y1, w.Y2) - 0.01 && y < Math.Max(w.Y1, w.Y2) + 0.01)
                {
                    return true;
                }
                if (!vertical && Math.Abs(w.Y1 - w.Y2) < 0.01
                    && Math.Abs(w.Y1 - y) < 0.01
                    && x > Math.Min(w.X1, w.X2) - 0.01 && x < Math.Max(w.X1, w.X2) + 0.01)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
