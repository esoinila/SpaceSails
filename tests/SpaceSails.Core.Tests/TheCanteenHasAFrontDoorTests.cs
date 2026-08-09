using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #775 · CIRCULATION — THE DOORS ON THE MAIN CORRIDOR, THE COUNT A CODE PUTS THERE, AND THE WAY THE FOOD
/// GETS IN.
///
/// <para>Owner, walking the new B1 the night #790 landed: <i>"The bar/canteen needs DOORS ON THE MAIN
/// CORRIDOR — today you have to really look for the way in; a venue's entrance should find YOU."</i> /
/// <i>"A canteen this size would have MORE THAN TWO DOORS just for safety reasons."</i> / <i>"The facility
/// needs FREIGHT ACCESS somewhere — eighty seats of food and twelve beds of produce do not arrive through a
/// personnel door."</i></para>
///
/// <para>Every guard below walks the REAL generator over the REAL field and measures the REAL published
/// lists. Nothing here builds a synthetic hall to measure — the fifth bug class is a guard handed a world
/// that cannot tell pass from fail, and a door count is exactly the shape of law it eats.</para>
/// </summary>
public sealed class TheCanteenHasAFrontDoorTests
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

    /// <summary>Every hall in the sweep, with the floor it stands on.</summary>
    private static IEnumerable<(string Body, int Level, UndergroundComplex.Hall Hall,
        UndergroundComplex.FloorPlan Floor)> EveryHall()
    {
        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                foreach (UndergroundComplex.Amenity a in floor.Amenities)
                {
                    if (a.Hall is { } hall)
                    {
                        yield return (body, level, hall, floor);
                    }
                }
            }
        }
    }

    /// <summary>Is anything poured ACROSS this opening, on the opening's own line? The one question "a door
    /// that leads somewhere" reduces to on a plan made of segments — and it is asked of the interior of the
    /// gap, so a wall that stops exactly at the jamb is a wall that stops at the jamb.</summary>
    private static bool BlockedByWall(SurfaceLayout.Doorway door, IReadOnlyList<SurfaceLayout.Wall> walls)
    {
        const double eps = 0.05, bite = 0.2;
        bool horizontal = Math.Abs(door.Y1 - door.Y2) < eps;
        double line = horizontal ? door.Y1 : door.X1;
        double lo = Math.Min(horizontal ? door.X1 : door.Y1, horizontal ? door.X2 : door.Y2) + bite;
        double hi = Math.Max(horizontal ? door.X1 : door.Y1, horizontal ? door.X2 : door.Y2) - bite;

        foreach (SurfaceLayout.Wall w in walls)
        {
            double a1 = horizontal ? w.Y1 : w.X1, a2 = horizontal ? w.Y2 : w.X2;
            if (Math.Abs(a1 - line) > eps || Math.Abs(a2 - line) > eps)
            {
                continue;   // not on this door's line at all
            }
            double b1 = horizontal ? w.X1 : w.Y1, b2 = horizontal ? w.X2 : w.Y2;
            if (Math.Min(b1, b2) < hi && Math.Max(b1, b2) > lo)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The doors cut in the SPINE'S own face — the ones this issue is about. Told apart by their
    /// orientation, which is not a convention but a fact: the spine runs in x, so a door in its face is a
    /// horizontal span, and every door in a rib's face is a vertical one.</summary>
    private static IEnumerable<SurfaceLayout.Doorway> OnTheCorridor(in UndergroundComplex.Hall hall) =>
        hall.Openings.Where(d => Math.Abs(d.Y1 - d.Y2) < 0.05).ToList();

    // ── (1) THE VENUE'S ENTRANCE FINDS YOU ───────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY HALL OPENS ONTO THE MAIN CORRIDOR, AND THE WALL BUILDER LEFT THE GAP.
    ///
    /// <para>Two halves, because either alone is the drawn-versus-simulated split: the room PUBLISHES a
    /// door on the spine's face, and the spine's face is not poured across it. A published door with
    /// concrete behind it is a plan that says a facility and a collision field that says a sealed box —
    /// #585, exactly.</para>
    ///
    /// <para><b>Proven RED</b> against the world as it stood before this issue — no front door is ever
    /// placed (<c>double duHi = -1e9;</c>), which is exactly what the two <c>SpineFace</c> calls standing
    /// ABOVE the hall carve amounted to: the wall was poured before anybody knew where the room was.</para>
    /// <code>
    /// 93 hall(s) have no honest way in off the main corridor:
    ///   luna B1: 3 door(s), NONE of them on the spine's own face — the way in is down a rib.
    ///   luna B13: 3 door(s), NONE of them on the spine's own face — the way in is down a rib.
    ///   phobos B1: 3 door(s), NONE of them on the spine's own face — the way in is down a rib.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void EveryHallOpensOntoTheMainCorridor()
    {
        var wrong = new List<string>();
        int halls = 0, fronts = 0;
        (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);

        foreach ((string body, int level, UndergroundComplex.Hall hall, var floor) in EveryHall())
        {
            halls++;
            List<SurfaceLayout.Doorway> corridorDoors = [.. OnTheCorridor(hall)];
            if (corridorDoors.Count == 0)
            {
                wrong.Add($"  {body} B{-level}: {hall.Openings.Count} door(s), NONE of them on the spine's "
                    + "own face — the way in is down a rib.");
                continue;
            }

            foreach (SurfaceLayout.Doorway d in corridorDoors)
            {
                fronts++;

                // It is ON the spine's face — one of the two lines the corridor's long walls run on — and
                // it is inside the room it belongs to.
                bool onAFace =
                    Math.Abs(Math.Abs(d.Y1 - shaftY) - UndergroundComplex.CorridorHalf) < 0.01;
                if (!onAFace)
                {
                    wrong.Add($"  {body} B{-level}: a 'corridor' door at y={d.Y1:F1} is not on either face "
                        + $"of the spine (y={shaftY:F1} ± {UndergroundComplex.CorridorHalf:F1}).");
                }
                if (Math.Min(d.X1, d.X2) < hall.X0 - 0.01 || Math.Max(d.X1, d.X2) > hall.X1 + 0.01)
                {
                    wrong.Add($"  {body} B{-level}: a corridor door at x∈[{d.X1:F1},{d.X2:F1}] hangs off "
                        + $"the end of its own hall (x∈[{hall.X0:F1},{hall.X1:F1}]).");
                }

                // …and the corridor's wall is not poured across it.
                if (BlockedByWall(d, floor.Walls))
                {
                    wrong.Add($"  {body} B{-level}: the corridor door at x∈[{d.X1:F1},{d.X2:F1}] has the "
                        + "spine's own wall across it — a door onto concrete.");
                }
            }

            // AND IT IS WHERE THE WALKER IS. The first one is placed at the lift (#751's own rule, one step
            // further in), so of everything on that wall it is the nearest thing to the car.
            double best = corridorDoors.Min(d => Math.Abs(((d.X1 + d.X2) / 2.0) - shaftX));
            double first = Math.Abs(((corridorDoors[0].X1 + corridorDoors[0].X2) / 2.0) - shaftX);
            if (first > best + 0.01)
            {
                wrong.Add($"  {body} B{-level}: the ENTRANCE is not the door nearest the lift "
                    + $"({first:F1} du away, when one of its siblings is {best:F1}).");
            }
        }

        // The FINDING first and the anti-vacuity after it: a red run should name the halls rather than
        // report that the sweep was small, and a sweep with nothing to say still has to answer for how
        // much world it looked at.
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} hall(s) have no honest way in off the main corridor:\n"
            + string.Join("\n", wrong.Take(20)));
        Assert.True(halls > 40, $"only {halls} halls were examined — this proved little.");
        Assert.True(fronts > 40, $"only {fronts} corridor doors were examined — this proved little.");
    }

    // ── (2) MORE THAN TWO DOORS, BECAUSE A CODE SAYS SO ──────────────────────────────────────────────

    /// <summary>
    /// EVERY HALL HAS AS MANY DOORS AS ITS OWN FLOOR AREA REQUIRES, AND NEVER FEWER THAN THREE.
    ///
    /// <para>The count is not typed anywhere: it is <see cref="UndergroundComplex.HallEgressDoors"/> of the
    /// room's own published box, asked here and asked by the carve, so this cannot drift into agreeing with
    /// a number the carve stopped using.</para>
    ///
    /// <para><b>Proven RED</b> by handing the carve a ONE-DOOR hall — <c>doors.Clear(); int wantSpine = 1;</c>
    /// in place of the shortfall arithmetic:</para>
    /// <code>
    /// 93 hall(s) do not have the doors a room that size is required to have:
    ///   luna B1: 7090 du² of hall with 1 door(s); the code wants 5.
    ///   luna B13: 4714 du² of hall with 1 door(s); the code wants 4.
    ///   ganymede B5: 2118 du² of hall with 1 door(s); the code wants 3.
    ///   …
    /// </code>
    ///
    /// <para>…and again against the pre-#775 world (no front doors at all), where the biggest halls fall
    /// short on the area term alone: <c>luna B1: 7090 du² of hall with 3 door(s); the code wants 5.</c></para>
    /// </summary>
    [Fact]
    public void TheDoorCountLawBinds()
    {
        var wrong = new List<string>();
        int halls = 0;
        var seenRequired = new HashSet<int>();
        var seenFronts = new HashSet<int>();

        foreach ((string body, int level, UndergroundComplex.Hall hall, _) in EveryHall())
        {
            halls++;
            int need = hall.EgressRequired;
            seenRequired.Add(need);
            seenFronts.Add(OnTheCorridor(hall).Count());

            if (need < UndergroundComplex.HallMinDoors)
            {
                wrong.Add($"  {body} B{-level}: the law asks for {need} doors, under the floor of "
                    + $"{UndergroundComplex.HallMinDoors} — the owner's 'more than two' is gone.");
            }
            if (hall.Openings.Count < need)
            {
                wrong.Add($"  {body} B{-level}: {hall.FloorDu2:F0} du² of hall with "
                    + $"{hall.Openings.Count} door(s); the code wants {need}.");
            }

            // Every published door is distinct ground. Two exits sharing a jamb are one exit.
            var seen = new List<SurfaceLayout.Doorway>();
            foreach (SurfaceLayout.Doorway d in hall.Openings)
            {
                foreach (SurfaceLayout.Doorway other in seen)
                {
                    bool sameLine = Math.Abs(d.X1 - other.X1) < 0.01 && Math.Abs(d.Y1 - other.Y1) < 0.01;
                    if (sameLine)
                    {
                        wrong.Add($"  {body} B{-level}: two of its doors are the same door "
                            + $"({d.X1:F1},{d.Y1:F1}).");
                    }
                }
                seen.Add(d);
            }
        }

        // ANTI-VACUITY, AND IT IS THE WHOLE POINT OF THE LAW: the required count is DERIVED, so the sweep
        // must actually produce more than one answer. A law that says "three" everywhere is a constant with
        // a formula painted on it.
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} hall(s) do not have the doors a room that size is required to have:\n"
            + string.Join("\n", wrong.Take(20)));
        Assert.True(halls > 40, $"only {halls} halls were examined — this proved little.");
        Assert.True(seenRequired.Count > 1,
            "every hall in the game demands the same number of doors — the area term is doing nothing: "
            + string.Join(", ", seenRequired.Order()));
        Assert.True(seenFronts.Count > 1,
            "every hall in the game got the same number of front doors — the shortfall term is doing "
            + "nothing: " + string.Join(", ", seenFronts.Order()));
    }

    /// <summary>The law itself, stated twice and asked once: a bigger room is never asked for fewer doors,
    /// and a room big enough is asked for more than the floor. A monotone law with a floor is the only
    /// shape "egress scales with occupancy" can honestly take.</summary>
    [Fact]
    public void TheEgressLawIsMonotoneAndHasAFloor()
    {
        int last = 0;
        for (double du2 = 0; du2 <= 20000; du2 += 25)
        {
            int doors = UndergroundComplex.HallEgressDoors(du2);
            Assert.True(doors >= UndergroundComplex.HallMinDoors,
                $"{du2:F0} du² of hall was allowed {doors} doors.");
            Assert.True(doors >= last, $"{du2:F0} du² asks for fewer doors than the room below it.");
            last = doors;
        }
        Assert.True(last > UndergroundComplex.HallMinDoors,
            "a twenty-thousand du² room is still only asked for the minimum — the law is a constant.");
    }

    // ── (3) EVERY DOOR LEADS SOMEWHERE ───────────────────────────────────────────────────────────────

    /// <summary>
    /// NOT ONE OF THESE DOORS OPENS ONTO ROCK — the #776-era law, said about a room that just grew four
    /// more ways out of it.
    ///
    /// <para>Every published opening is checked against every wall on its own line, and every one of them
    /// is also PUBLISHED on the plan's own doorway list where the floor has drawn doors at all — so a door
    /// the hall knows about and the deck does not is caught here rather than by a captain walking into it.</para>
    ///
    /// <para><b>Proven RED</b> by publishing the front doors without cutting them — the carve's
    /// <c>spineCuts.Add(...)</c> commented out, everything else untouched:</para>
    /// <code>
    /// 270 door(s) lead nowhere:
    ///   luna B1: the door at (30.8,-157.0)-(37.2,-157.0) has a wall across it.
    ///   luna B1: the door at (30.8,-157.0) is one the hall knows about and the deck plan does not —
    ///            nothing would be drawn there.
    ///   luna B1: the door at (90.0,-157.0)-(96.4,-157.0) has a wall across it.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void EveryHallDoorLeadsSomewhere()
    {
        var wrong = new List<string>();
        int doors = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, var floor) in EveryHall())
        {
            bool drawn = !UndergroundComplex.IsFound(body, level);
            foreach (SurfaceLayout.Doorway d in hall.Openings)
            {
                doors++;
                if (BlockedByWall(d, floor.Walls))
                {
                    wrong.Add($"  {body} B{-level}: the door at ({d.X1:F1},{d.Y1:F1})-({d.X2:F1},{d.Y2:F1})"
                        + " has a wall across it.");
                }
                if (BlockedByWall(d, floor.Windows ?? []))
                {
                    wrong.Add($"  {body} B{-level}: the door at ({d.X1:F1},{d.Y1:F1}) opens into the park's "
                        + "glass.");
                }
                foreach (UndergroundComplex.LockedDoor l in floor.Locked)
                {
                    if (BlockedByWall(d, [new SurfaceLayout.Wall(l.X1, l.Y1, l.X2, l.Y2, true)]))
                    {
                        wrong.Add($"  {body} B{-level}: the door at ({d.X1:F1},{d.Y1:F1}) is behind the "
                            + $"locked door '{l.Sign}'.");
                    }
                }
                if (drawn && !floor.Doorways.Any(p =>
                        Math.Abs(p.X1 - d.X1) < 0.01 && Math.Abs(p.Y1 - d.Y1) < 0.01
                        && Math.Abs(p.X2 - d.X2) < 0.01 && Math.Abs(p.Y2 - d.Y2) < 0.01))
                {
                    wrong.Add($"  {body} B{-level}: the door at ({d.X1:F1},{d.Y1:F1}) is one the hall knows "
                        + "about and the deck plan does not — nothing would be drawn there.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} door(s) lead nowhere:\n" + string.Join("\n", wrong.Take(20)));
        Assert.True(doors > 150, $"only {doors} hall doors were checked — this proved little.");
    }

    // ── (4) FREIGHT ACCESS EXISTS, AND IT IS A THING ON THE DECK ─────────────────────────────────────

    /// <summary>
    /// EVERY HALL HAS A GOODS HOIST, IT IS BUILT OUT OF WALLS THAT ARE ACTUALLY THERE, AND ITS SHUTTER IS
    /// A DOOR THAT REFUSES IN WORDS.
    ///
    /// <para>The drawn-versus-simulated guard for this feature: the fixture is PUBLISHED as a box, and the
    /// four sides of that box have to be findable in the floor's own wall list. A record describing a
    /// freight car nobody poured is a picture of freight access.</para>
    ///
    /// <para><b>Proven RED</b> by publishing the hoist and leaving out its one new wall — the
    /// <c>walls.Add(... X(hoistU1) ...)</c> divider commented out:</para>
    /// <code>
    /// 93 goods hoist(s) are not the fixture the plan says they are:
    ///   luna B1: the car's divider at x=14.5 was published and never poured.
    ///   luna B13: one side at x=-24.5 was published and never poured.
    ///   phobos B1: one side at x=73.9 was published and never poured.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void TheGoodsHoistIsBuiltAndItRefusesInWords()
    {
        var wrong = new List<string>();
        int hoists = 0;

        foreach ((string body, int level, UndergroundComplex.Hall hall, var floor) in EveryHall())
        {
            if (hall.Freight is not { } hoist)
            {
                wrong.Add($"  {body} B{-level}: {hall.FloorDu2:F0} du² of canteen and no freight access at "
                    + "all — the food arrives through a personnel door.");
                continue;
            }
            hoists++;

            // IT IS IN THE ROOM IT SERVES, in the band the customer never stands in.
            if (hoist.X0 < hall.X0 - 0.01 || hoist.X1 > hall.X1 + 0.01
                || hoist.Y0 < hall.Y0 - 0.01 || hoist.Y1 > hall.Y1 + 0.01)
            {
                wrong.Add($"  {body} B{-level}: the car is not inside its own hall.");
            }
            double car = (hoist.X1 - hoist.X0) * (hoist.Y1 - hoist.Y0);
            if (car < 2 * UndergroundComplex.DoorHalf * UndergroundComplex.HallCounterBandDu - 0.01)
            {
                wrong.Add($"  {body} B{-level}: the car is {car:F0} du² — a pallet does not go in that.");
            }

            // ITS FOUR SIDES ARE POURED. Two uprights and the counter line's own two stubs either side of
            // the shutter; the fourth is the room's far wall (poured, or the park's glass) and is asked for
            // in whichever list it lives in.
            foreach ((double x, string what) in new[]
                     { (hoist.X0, "one side"), (hoist.X1, "the car's divider") })
            {
                bool poured = floor.Walls.Any(w =>
                    Math.Abs(w.X1 - x) < 0.01 && Math.Abs(w.X2 - x) < 0.01
                    && Math.Min(w.Y1, w.Y2) <= hoist.Y0 + 0.01 && Math.Max(w.Y1, w.Y2) >= hoist.Y1 - 0.01);
                if (!poured)
                {
                    wrong.Add($"  {body} B{-level}: {what} at x={x:F1} was published and never poured.");
                }
            }
            bool backed =
                floor.Walls.Concat(floor.Windows ?? []).Any(w =>
                    (Math.Abs(w.Y1 - hoist.Y0) < 0.01 || Math.Abs(w.Y1 - hoist.Y1) < 0.01)
                    && Math.Abs(w.Y1 - w.Y2) < 0.01
                    && Math.Min(w.X1, w.X2) <= hoist.X0 + 0.01 && Math.Max(w.X1, w.X2) >= hoist.X1 - 0.01);
            if (!backed)
            {
                wrong.Add($"  {body} B{-level}: the car has no back — it opens into whatever is behind it.");
            }

            // AND THE SHUTTER IS A DOOR THAT WILL NOT OPEN, WITH THE REFUSAL ON IT. The building's own
            // grammar (#585's locked doors), so [E] reads the plate and nothing else ever happens.
            UndergroundComplex.LockedDoor? shut = floor.Locked.Cast<UndergroundComplex.LockedDoor?>()
                .FirstOrDefault(l => l is { } d
                    && Math.Abs(d.X1 - hoist.Shutter.X1) < 0.01 && Math.Abs(d.Y1 - hoist.Shutter.Y1) < 0.01);
            if (shut is not { } leaf)
            {
                wrong.Add($"  {body} B{-level}: the shutter is a gap, not a refusal — nothing is hung in "
                    + "it, so pressing it says nothing at all.");
            }
            else if (leaf.Sign != UndergroundComplex.FreightPlate)
            {
                wrong.Add($"  {body} B{-level}: the shutter says '{leaf.Sign}'.");
            }

            // …and the plate is read from the HALL floor, in front of it, not from inside the car.
            if (hoist.Contains(hoist.PlateX, hoist.PlateY))
            {
                wrong.Add($"  {body} B{-level}: the plate is read from INSIDE the car.");
            }
            if (!hall.Contains(hoist.PlateX, hoist.PlateY))
            {
                wrong.Add($"  {body} B{-level}: the plate is read from outside the hall entirely.");
            }
            if (!floor.Labels.Any(m => m.Label == UndergroundComplex.FreightSign
                    && Math.Abs(m.X - hoist.PlateX) < 0.01 && Math.Abs(m.Y - hoist.PlateY) < 0.01))
            {
                wrong.Add($"  {body} B{-level}: the hoist is drawn and never labelled.");
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} goods hoist(s) are not the fixture the plan says they are:\n"
            + string.Join("\n", wrong.Take(20)));
        Assert.True(hoists > 40, $"only {hoists} goods hoists were examined — this proved little.");
    }

    // ── (5) THE NEW SENTENCES ────────────────────────────────────────────────────────────────────────

    /// <summary>§13.8 · THE PLATES SAY WHAT THE ROOM IS FOR AND NEVER WHAT THE BUILDING IS FOR. Three new
    /// sentences ship with this issue and none of them may leak: no Old Ones, no KAAMOS, no restores, no
    /// explanation of anything. A goods hoist knows about pallets.</summary>
    [Fact]
    public void TheNewPlatesLeakNothing()
    {
        var sentences = new List<string>
        {
            UndergroundComplex.FreightPlate,
            UndergroundComplex.FreightSign,
            UndergroundComplex.HallEgressPlate(2),
            UndergroundComplex.HallEgressPlate(3),
        };
        foreach (string body in new[] { "luna", "enceladus", "the-clinker" })
        {
            foreach (UndergroundComplex.Comfort use in
                new[] { UndergroundComplex.Comfort.UpperCanteen, UndergroundComplex.Comfort.StaffCanteen })
            {
                string entrance = UndergroundComplex.HallEntrancePlate(body, use);
                sentences.Add(entrance);

                // …and it NAMES THE VENUE, which is the whole reason it is composed rather than typed: a
                // corridor plate that reads only ENTRANCE says nothing about what it is the way in to.
                Assert.EndsWith(" · ENTRANCE", entrance, StringComparison.Ordinal);
                Assert.True(entrance.Length > " · ENTRANCE".Length + 3,
                    $"'{entrance}' is a plate with no room on it.");
                Assert.StartsWith(entrance[..entrance.IndexOf(" · ENTRANCE", StringComparison.Ordinal)],
                    UndergroundComplex.AmenitySigns(body, use).Plate, StringComparison.Ordinal);
            }
        }
        string[] forbidden =
        [
            "kaamos", "old one", "reever", "restore", "backup", "minister", "vantar",
            "ancient", "monolith", "donor",
        ];

        foreach (string s in sentences)
        {
            Assert.False(string.IsNullOrWhiteSpace(s), "a plate is blank.");
            foreach (string word in forbidden)
            {
                Assert.False(s.Contains(word, StringComparison.OrdinalIgnoreCase),
                    $"'{s}' leaks '{word}'.");
            }
        }

        // The numbers are the whole of what an exit plate says, and they are DIFFERENT numbers — a corridor
        // of identical plates is a wall with holes in it.
        Assert.NotEqual(UndergroundComplex.HallEgressPlate(2), UndergroundComplex.HallEgressPlate(3));
        Assert.Contains("CREW", UndergroundComplex.FreightPlate, StringComparison.Ordinal);
    }
}
