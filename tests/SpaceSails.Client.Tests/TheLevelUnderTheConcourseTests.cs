using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1253 · <b>DOWN BELOW — THE LEVEL, THE CAGES, AND THE WAY BACK UP.</b>
///
/// <para>Owner, 2026-09-20: <i>"could we add a basement level to the observation deck station… The main hall
/// could have multiple elevators… good for tailing."</i></para>
///
/// <para>The #1253 audit is a read-only inventory of what a haven IS and it names, in advance, every way a
/// second floor goes wrong: the deck memo serving whichever floor was built first; the T's rectangles leaking
/// into the lower level; one people-list for two rooms; a <c>static readonly</c> in a partial that sorts
/// early building a station stacked on the origin; and — worst — an A* audit that proves you can REACH a car
/// and never that the car is a way HOME (#600/#719). This file is that list, turned round, one law at a
/// time.</para>
/// </summary>
public sealed class TheLevelUnderTheConcourseTests
{
    private static string Berth => HavenInterior.TheHavenWithFloors
        ?? throw new InvalidOperationException("no haven in the catalogue has a floor under it at all.");

    private static DeckPlan Concourse => HavenInterior.DockedDeck(Berth)!;

    private static DeckPlan Below =>
        HavenInterior.DockedDeck(Berth, level: HavenLevels.ServiceLevel)!;

    /// <summary>The lattice every sweep in this file walks, and the step it walks it at. The ring's own
    /// footprint with a margin, measured off the hall the level is laid under rather than typed — a basement
    /// swept over a box somebody guessed would be a sweep that proved whatever it happened to cover.</summary>
    private const double Step = 0.5;

    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds
    {
        get
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (DeckPlan.Wall w in Below.Walls)
            {
                minX = Math.Min(minX, Math.Min(w.X1, w.X2));
                maxX = Math.Max(maxX, Math.Max(w.X1, w.X2));
                minY = Math.Min(minY, Math.Min(w.Y1, w.Y2));
                maxY = Math.Max(maxY, Math.Max(w.Y1, w.Y2));
            }

            return (minX - 2, minY - 2, maxX + 2, maxY + 2);
        }
    }

    /// <summary>What a plan IS, as one string — every wall, door, console, label and backdrop in the order
    /// the build emitted them. Used for the "level 0 has not moved" laws, so a claim about bytes is about
    /// bytes and not about a count that two different decks could agree on.</summary>
    private static string Fingerprint(DeckPlan plan)
    {
        var sb = new StringBuilder();
        foreach (DeckPlan.Wall w in plan.Walls)
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"W {w.X1:F4} {w.Y1:F4} {w.X2:F4} {w.Y2:F4} {w.IsWindow} {w.IsHull}\n");
        }

        foreach (DeckPlan.Door d in plan.Doors)
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"D {d.X1:F4} {d.Y1:F4} {d.X2:F4} {d.Y2:F4} {d.Locked}\n");
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
            sb.Append(CultureInfo.InvariantCulture,
                $"B {b.Url} {b.X:F4} {b.Y:F4} {b.W:F4} {b.H:F4} {b.Alpha:F4}\n");
        }

        return sb.ToString();
    }

    // ── (a) THE LEVEL TERM IS TRANSPARENT ────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>LEVEL 0 IS THE DECK THIS GAME HAS ALWAYS BUILT, AT EVERY HAVEN.</b> Asking for the concourse by
    /// name and asking for nothing at all must be one plan, wall for wall and label for label.
    ///
    /// <para>This is the audit's first prediction turned round — <i>"the deck memo has no floor term, so it
    /// serves whichever floor was built first"</i> — and it also holds the default in place: every caller in
    /// the game that has ever asked for a docked deck passes no level, and the day that quietly started
    /// meaning something else is the day seven pinned haven frames move for no reason anybody wrote down.
    /// The memo is asked TWICE per haven on purpose, in both orders, because a cache that keyed the floor
    /// badly would hand the second ask the first ask's answer.</para>
    ///
    /// <para><b>Proven RED</b> by dropping <c>level</c> out of the lower deck's cache key (both floors share
    /// <c>bodyId</c>): <c>selene-gate: the concourse and level 0 are two different plans.</c></para>
    /// </summary>
    [Fact]
    public void AskingForTheConcourseAndAskingForNothingAreOnePlanAtEveryHaven()
    {
        foreach (string id in HavenInterior.InteriorBodyIds)
        {
            string bare = Fingerprint(HavenInterior.DockedDeck(id)!);
            string named = Fingerprint(HavenInterior.DockedDeck(id, level: HavenLevels.Concourse)!);
            Assert.True(
                string.Equals(bare, named, StringComparison.Ordinal),
                $"{id}: the concourse and level 0 are two different plans.");

            // …and the other order, so a memo that keyed the floor badly cannot pass by luck.
            Assert.Equal(
                Fingerprint(HavenInterior.DockedDeck(id, level: HavenLevels.Concourse)!),
                Fingerprint(HavenInterior.DockedDeck(id)!));
        }
    }

    /// <summary>
    /// <b>SIX OF THE SEVEN HAVENS HAVE NO FLOOR UNDER THEM, AND ASKING FOR ONE GIVES THEM THEIR CONCOURSE.</b>
    ///
    /// <para>A station with no basement answers the concourse for any level at all — which is what keeps the
    /// other six byte-identical whatever a caller passes, and what makes the whole feature additive rather
    /// than a level term threaded through seven stations' geometry. It also holds the CAGES to one station:
    /// three lift consoles appearing on a ring that has nothing under it would be an affordance with nothing
    /// behind it (#212), pressed once and never again.</para>
    ///
    /// <para><b>Proven RED</b> by giving a second spec a <c>LowerSpec</c>: <c>red-eye: 3 lift console(s) on a
    /// station with no floor under it.</c></para>
    /// </summary>
    [Fact]
    public void OnlyOneStationHasAFloorUnderItAndTheRestAreUntouched()
    {
        int withFloors = 0;
        foreach (string id in HavenInterior.InteriorBodyIds)
        {
            int cages = HavenInterior.DockedDeck(id)!.Consoles
                .Count(c => c.Kind == DeckPlan.ConsoleKind.HavenLift);

            if (HavenInterior.HasLowerLevel(id))
            {
                withFloors++;
                Assert.Equal(HavenLevels.Levels.Count, HavenInterior.LevelsOf(id).Count);
                Assert.Equal(HavenLevels.Cages, cages);
                continue;
            }

            Assert.Single(HavenInterior.LevelsOf(id));
            Assert.Empty(HavenInterior.TheCagesAt(id));
            Assert.True(cages == 0, $"{id}: {cages} lift console(s) on a station with no floor under it.");
            Assert.Equal(
                Fingerprint(HavenInterior.DockedDeck(id)!),
                Fingerprint(HavenInterior.DockedDeck(id, level: HavenLevels.ServiceLevel)!));
        }

        Assert.Equal(1, withFloors);
    }

    // ── (b) THE CAGES ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THREE CARS, ON THREE DIFFERENT EDGES, ON THE SAME SQUARES ON BOTH FLOORS.</b>
    ///
    /// <para>Three laws in one case, and each is the owner's ask said in geometry:</para>
    /// <list type="number">
    /// <item>there are <see cref="HavenLevels.Cages"/> of them and no two are within an interact radius of
    /// each other — three cars a captain can watch at once are one car drawn three times;</item>
    /// <item>each stands on a DIFFERENT edge of the twelve-gon, and none of them on the three this station
    /// already spent (the tube, the bar door, and #1199's observation walk, which holds edge 5);</item>
    /// <item>a car is in the same place on both floors, which is the Hive's own law about a shaft said about
    /// a berth: going down is legible and coming back up is never a search.</item>
    /// </list>
    ///
    /// <para><b>Proven RED</b> by moving one cage onto the walk's edge: <c>a cage stands on edge 5, which is
    /// the observation walk's.</c></para>
    /// </summary>
    [Fact]
    public void TheThreeCarsStandOnThreeFreeEdgesAndOnTheSameSquareOnBothFloors()
    {
        IReadOnlyList<DeckReachability.Point> cars = HavenInterior.TheCagesAt(Berth);
        Assert.Equal(HavenLevels.Cages, cars.Count);

        for (int i = 0; i < cars.Count; i++)
        {
            for (int j = i + 1; j < cars.Count; j++)
            {
                double dx = cars[i].X - cars[j].X, dy = cars[i].Y - cars[j].Y;
                Assert.True(
                    Math.Sqrt((dx * dx) + (dy * dy)) > DeckPlan.InteractRadius * 2,
                    $"cars {i} and {j} are on top of each other.");
            }
        }

        // The same three squares carry a HavenLift console on the concourse and on the level below.
        List<(double X, double Y)> above = Concourse.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.HavenLift)
            .Select(c => ((double)c.X, (double)c.Y)).OrderBy(p => p.Item1).ThenBy(p => p.Item2).ToList();
        List<(double X, double Y)> below = Below.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.HavenLift)
            .Select(c => ((double)c.X, (double)c.Y)).OrderBy(p => p.Item1).ThenBy(p => p.Item2).ToList();

        Assert.Equal(HavenLevels.Cages, above.Count);
        Assert.Equal(above.Count, below.Count);
        for (int i = 0; i < above.Count; i++)
        {
            Assert.Equal(above[i].X, below[i].X, 3);
            Assert.Equal(above[i].Y, below[i].Y, 3);
        }

        // …and none of them on one of the three edges this ring already spent. The walk's own edge is asked
        // of the room rather than restated: #1199 owns which one it took.
        DeckReachability.Point? mouth = HavenInterior.TheWalksMouthAt(Berth);
        Assert.NotNull(mouth);
        foreach (DeckReachability.Point car in cars)
        {
            Assert.True(
                Math.Abs(car.X - mouth!.Value.X) > DeckPlan.InteractRadius
                || Math.Abs(car.Y - mouth.Value.Y) > DeckPlan.InteractRadius,
                "a cage stands on the observation walk's own edge.");
        }
    }

    /// <summary>
    /// <b>THE RING GAVE UP THREE DEPARTMENT PLATES AND NOTHING ELSE.</b> The concourse's WALLS, DOORS,
    /// LABELS and BACKDROPS are unchanged by the cages — a car is a door in a wall, not a gap in it — and the
    /// only difference on level 0 is three consoles that used to be knockable hatches and now say what is
    /// behind them.
    ///
    /// <para>Measured against the ring rather than against a remembered number: every one of the twelve
    /// edges still carries exactly one console, the tube's and the bar's and the walk's excepted, and the
    /// hatch ids on the ones that are still departments are the ones the sealed-edge counter has always
    /// dealt them. That is what <c>sealedIdx++</c> over a cage BUYS, and dropping it would renumber this
    /// station's whole ring.</para>
    ///
    /// <para><b>Proven RED</b> by removing the <c>sealedIdx</c> step over a cage — nine of the ring's own
    /// panels change their department and this case names them.</para>
    /// </summary>
    [Fact]
    public void TheCagesCostTheRingThreePlatesAndNotOneWall()
    {
        // The concourse still hangs exactly one console on each of its sealed/car edges: the ring deals
        // twelve faces, three of them are the tube, the bar door and the walk, and the other nine carry a
        // panel. Three of those nine are cars now.
        int hatches = Concourse.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.Hatch
            && c.Label.Contains('-', StringComparison.Ordinal));
        int cages = Concourse.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.HavenLift);
        Assert.Equal(HavenLevels.Cages, cages);

        // The ring's nine panels, plus the bar's two back-room leaves, are the Hatch consoles on this deck.
        // Three of the nine became cars, so the ring's own panels are six.
        Assert.True(hatches >= 6, $"the ring carries only {hatches} numbered panels — the cages ate more than three.");

        // Every department id on the ring is still DISTINCT: the counter stepped over the cars rather than
        // being reset by them, which is what stops two edges claiming one designation.
        List<string> ids = Concourse.Consoles
            .Where(c => c.Kind is DeckPlan.ConsoleKind.Hatch or DeckPlan.ConsoleKind.HavenLift)
            .Select(c => c.Label[(c.Label.LastIndexOf('·') + 1)..].Trim())
            .ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    // ── (c) THE FLOOR IS WALKABLE ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY TILE OF THE LOWER CONCOURSE IS REACHABLE FROM THE FIRST CAR'S LANDING.</b> Swept rather than
    /// sampled: every lattice point down there a body could stand on at all must be in the set grown from
    /// where the doors open. A square you can see and cannot walk to is the #600 lift bug with worklight on
    /// it.
    ///
    /// <para><b>Proven RED</b> by widening the cabin row until its corners push through the ring — the whole
    /// north-west pocket drops out of the set at once.</para>
    /// </summary>
    [Fact]
    public void EveryStandableTileOfTheLowerConcourseIsReachable()
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = Below.CollisionField;
        DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Berth, 0)!.Value;

        IReadOnlyCollection<DeckReachability.Point> reached =
            DeckReachability.Reachable(landing, walls, DeckPlan.AvatarRadius, Bounds, Step);
        var got = new HashSet<(int, int)>(
            reached.Select(p => ((int)Math.Round(p.X / Step), (int)Math.Round(p.Y / Step))));

        (double minX, double minY, double maxX, double maxY) = Bounds;
        int standable = 0, missed = 0;
        var worst = new List<string>();
        for (double x = minX; x <= maxX + 1e-9; x += Step)
        {
            for (double y = minY; y <= maxY + 1e-9; y += Step)
            {
                if (!DeckReachability.Standable(x, y, DeckPlan.AvatarRadius, walls)
                    || !OnTheCorridorFloor(x, y))
                {
                    continue;
                }

                standable++;
                if (got.Contains(((int)Math.Round(x / Step), (int)Math.Round(y / Step))))
                {
                    continue;
                }

                missed++;
                if (worst.Count < 10)
                {
                    worst.Add(string.Create(CultureInfo.InvariantCulture, $"  ({x:F1}, {y:F1})"));
                }
            }
        }

        Assert.True(standable > 200,
            $"only {standable} tile(s) of the lower concourse are standable — this proved nothing.");
        Assert.True(missed == 0,
            $"{missed} of {standable} standable tile(s) down below cannot be walked to from the car:\n"
            + string.Join("\n", worst));
    }

    /// <summary>Is this point part of the FLOOR — inside the ring the level is poured in, and not inside one
    /// of the sealed cabins?
    ///
    /// <para>Both clauses are load-bearing. Ground outside the building is not a tile and calling it
    /// unreachable would be a sweep failing on vacuum. And the inside of a cabin is <i>standable</i> to a
    /// collision test — it is a poured box with nothing in it — while being, by construction, the one place
    /// on this station a captain may never be: the leaf does not open, which is #563's ruling and the whole
    /// content of the row. A sweep that demanded a route into one would be demanding the feature be
    /// broken.</para>
    ///
    /// <para>The cabin box is read off the DRAWN walls and leaves rather than off the geometry that placed
    /// them, so the exclusion cannot quietly widen with a row that moved.</para></summary>
    private static bool OnTheCorridorFloor(double x, double y)
    {
        DeckReachability.Point centre = TheMiddleOfTheRing;
        double dx = x - centre.X, dy = y - centre.Y;
        if (Math.Sqrt((dx * dx) + (dy * dy)) > RingApothem - DeckPlan.AvatarRadius)
        {
            return false;
        }

        (double cx0, double cy0, double cx1, double cy1) = TheCabinRowBox;
        return x < cx0 - DeckPlan.AvatarRadius || x > cx1 + DeckPlan.AvatarRadius
            || y < cy0 - DeckPlan.AvatarRadius || y > cy1 + DeckPlan.AvatarRadius;
    }

    /// <summary>The block of cabins, as the plan drew it — <c>(west, front, east, back)</c>. Off the leaves
    /// and the party walls, never off the placer.</summary>
    private static (double X0, double Y0, double X1, double Y1) TheCabinRowBox
    {
        get
        {
            List<DeckPlan.Door> leaves = Below.Doors.Where(d => d.Locked).ToList();
            Assert.Equal(HavenLevels.Cabins, leaves.Count);
            double face = leaves.Min(d => (double)d.Y1);

            List<DeckPlan.Wall> inner = Below.Walls.Where(w => !w.IsHull).ToList();
            double back = inner
                .Where(w => Math.Abs(w.Y1 - w.Y2) < 1e-3 && w.Y1 > face + 0.5)
                .Min(w => (double)w.Y1);
            List<DeckPlan.Wall> uprights = inner.Where(w => Math.Abs(w.X1 - w.X2) < 1e-3).ToList();
            return (uprights.Min(w => (double)w.X1), face, uprights.Max(w => (double)w.X1), back);
        }
    }

    private static DeckReachability.Point TheMiddleOfTheRing
    {
        get
        {
            IReadOnlyList<DeckReachability.Point> cars = HavenInterior.TheCagesAt(Berth);
            IReadOnlyList<DeckReachability.Point> landings =
                [.. Enumerable.Range(0, cars.Count).Select(i => HavenInterior.TheCageLandingAt(Berth, i)!.Value)];

            // The cars sit nine tenths of the way out and their landings a fixed pace further in, both along
            // the line to the middle — so the middle is where those three lines meet, and it is recovered
            // from them rather than typed. Two cars are enough; the third is the check.
            double cx = 0, cy = 0;
            for (int i = 0; i < cars.Count; i++)
            {
                double ux = landings[i].X - cars[i].X, uy = landings[i].Y - cars[i].Y;
                double len = Math.Sqrt((ux * ux) + (uy * uy));
                cx += cars[i].X + (ux / len * OutToTheMiddle(i));
                cy += cars[i].Y + (uy / len * OutToTheMiddle(i));
            }

            return new DeckReachability.Point(cx / cars.Count, cy / cars.Count);
        }
    }

    /// <summary>How far the middle is from car <paramref name="i"/> — solved off the pair of cars opposite it
    /// rather than assumed, so nothing in this file holds a copy of the ring's radius.</summary>
    private static double OutToTheMiddle(int i)
    {
        IReadOnlyList<DeckReachability.Point> cars = HavenInterior.TheCagesAt(Berth);
        double best = 0;
        for (int j = 0; j < cars.Count; j++)
        {
            if (j == i)
            {
                continue;
            }

            double dx = cars[i].X - cars[j].X, dy = cars[i].Y - cars[j].Y;
            best = Math.Max(best, Math.Sqrt((dx * dx) + (dy * dy)));
        }

        return best / 2.0;
    }

    private static double RingApothem
    {
        get
        {
            DeckReachability.Point centre = TheMiddleOfTheRing;
            double best = double.MaxValue;
            foreach (DeckPlan.Wall w in Below.Walls.Where(w => w.IsHull))
            {
                double mx = ((double)w.X1 + w.X2) / 2, my = ((double)w.Y1 + w.Y2) / 2;
                double dx = mx - centre.X, dy = my - centre.Y;
                best = Math.Min(best, Math.Sqrt((dx * dx) + (dy * dy)));
            }

            return best;
        }
    }

    // ── (d) THE FIRE CODE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE LOWER FLOOR PASSES THE FIRE CODE, AND NOTHING NEW WAS EXEMPTED FOR IT.</b>
    ///
    /// <para>Two rooms down there and two different answers out of the SAME law
    /// (<see cref="UndergroundComplex.MeetsFireCode"/>), which is the point:</para>
    /// <list type="bullet">
    /// <item>the CORRIDOR has <see cref="HavenLevels.Cages"/> ways out and takes no exemption at all — three
    /// cars count the way the Hive's two do, because a car that reaches another floor is an exit and the
    /// building's own <c>Shaft.ReachesTheSurface</c> is that same sentence one building over;</item>
    /// <item>a CABIN has one leaf and is let off under the exemption that already exists — it is
    /// bedroom-small BY MEASUREMENT (<see cref="UndergroundComplex.FireCodeSmallRoomDu"/>), which is
    /// #822's original and only dimensional member.</item>
    /// </list>
    ///
    /// <para>No member is added to <see cref="UndergroundComplex.FireCodeExemption"/> for this level, and
    /// that is asserted rather than remembered: a floor that had to argue for its own exemption would be the
    /// list growing by arithmetic, which is the drift <c>ReasonFor</c> exists to stop.</para>
    ///
    /// <para><b>Proven RED</b> by deepening a cabin past the threshold: <c>a cabin is 8.5 du on its longest
    /// side and the code lets a room off at 8.0 — it has one leaf and no exemption to stand on.</c></para>
    /// </summary>
    [Fact]
    public void TheLowerFloorMeetsTheFireCodeWithNoNewExemption()
    {
        Assert.True(
            UndergroundComplex.MeetsFireCode(HavenLevels.Cages, UndergroundComplex.FireCodeExemption.None),
            $"{HavenLevels.Cages} car(s) off the lower concourse and the code wants "
            + $"{UndergroundComplex.FireCodeMinExits}.");

        // …and the three are REAL exits: each is a console on this floor's own plan.
        Assert.Equal(
            HavenLevels.Cages,
            Below.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.HavenLift));

        // The cabins. Longest side against the law's own threshold, measured off the drawn walls.
        (double width, double depth) = TheCabinBox;
        double longest = Math.Max(width, depth);
        Assert.True(
            longest <= UndergroundComplex.FireCodeSmallRoomDu,
            $"a cabin is {longest:F1} du on its longest side and the code lets a room off at "
            + $"{UndergroundComplex.FireCodeSmallRoomDu:F1} — it has one leaf and no exemption to stand on.");
        Assert.True(
            UndergroundComplex.MeetsFireCode(1, UndergroundComplex.FireCodeExemption.BedroomSmall),
            "the bedroom-small exemption no longer lets a one-leaf room off, and the cabins rode on it.");

        // …and the exemption list did not grow a member for this level.
        Assert.Equal(3, Enum.GetValues<UndergroundComplex.FireCodeExemption>().Length);
    }

    /// <summary>One cabin's box, measured off the drawn party walls rather than off the geometry that drew
    /// them — a guard that asked the same expression the build asked would agree with whatever the build
    /// did.</summary>
    private static (double Width, double Depth) TheCabinBox
    {
        get
        {
            IReadOnlyList<string> plates = HavenInterior.CabinPlatesAt(Berth);
            Assert.Equal(HavenLevels.Cabins, plates.Count);

            List<DeckPlan.Door> leaves = Below.Doors.Where(d => d.Locked).ToList();
            Assert.Equal(HavenLevels.Cabins, leaves.Count);

            // The row's own front: every leaf is on it, so its y is the corridor face.
            double face = leaves[0].Y1;
            Assert.All(leaves, d => Assert.Equal(face, d.Y1, 3));

            // The back of the row is the nearest wall running parallel to that face, north of it.
            double back = Below.Walls
                .Where(w => Math.Abs(w.Y1 - w.Y2) < 1e-3 && w.Y1 > face + 0.5 && !w.IsHull)
                .Select(w => (double)w.Y1)
                .DefaultIfEmpty(double.NaN)
                .Min();
            Assert.False(double.IsNaN(back), "the cabin row has no back wall.");

            // …and the frontage is the spacing between two neighbouring leaves.
            List<double> xs = leaves.Select(d => ((double)d.X1 + d.X2) / 2).OrderBy(v => v).ToList();
            double frontage = xs[1] - xs[0];
            return (frontage, back - face);
        }
    }

    // ── (e) THE BERTH COLUMN IN THE WAY-HOME LAW ─────────────────────────────────────────────────────────

    /// <summary>
    /// <b>FROM EVERY SQUARE DOWN THERE, A CAR IS REACHABLE — AND THE CAR RIDES BACK — AND THE GANGWAY IS
    /// REACHABLE FROM WHERE IT PUTS YOU.</b>
    ///
    /// <para>This is <see cref="TheStairIsAWayHomeTests"/>' second column, said about a berth. #600 is the
    /// scar it respects: <i>an A* audit proves you can REACH the lift, never that it is a way HOME</i> — and
    /// it survived three PRs editing the same function because none of them asked what the UP case did. So
    /// all three legs are asked of every tile, and the middle one is asked of the PANEL the captain actually
    /// presses (<see cref="HavenLevels.Panel"/>) rather than of a rule this file keeps.</para>
    ///
    /// <para>The last leg matters most and is the one a moon does not have: a service level carries no
    /// gangway and no tube, so a captain who came down and could not get back to his own airlock would be
    /// marooned in a station rather than in a lab.</para>
    ///
    /// <para><b>Proven RED</b> by taking the CONCOURSE row off the lower panel (the car only goes down):
    /// <c>the panel on the service level offers no way back to the concourse — #600 in a new coat.</c></para>
    /// </summary>
    [Fact]
    public void EverySquareDownBelowReachesACarThatRidesBackToAGangway()
    {
        IReadOnlyList<SurfaceCollision.Segment> below = Below.CollisionField;
        var cars = new List<DeckReachability.Point>(HavenInterior.TheCagesAt(Berth));
        Assert.NotEmpty(cars);

        // LEG TWO, once and for all three cars: the panel on this floor offers the concourse.
        IReadOnlyList<UndergroundComplex.LiftStop> panel = HavenLevels.Panel(HavenLevels.ServiceLevel);
        Assert.True(
            panel.Any(s => s.Level == HavenLevels.Concourse && s.Refusal is null && !s.IsCurrent),
            "the panel on the service level offers no way back to the concourse — #600 in a new coat.");

        // LEG THREE, once per car: the gangway is reachable from the square its doors open on upstairs.
        IReadOnlyList<SurfaceCollision.Segment> above = Concourse.CollisionField;
        (double gx, double gy, _) = HavenInterior.BarThreshold;
        var gangway = new DeckReachability.Point(2.5, 8);   // the airlock corridor PullAvatarAboard uses
        for (int cage = 0; cage < cars.Count; cage++)
        {
            DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Berth, cage)!.Value;
            Assert.True(
                DeckReachability.Standable(landing.X, landing.Y, DeckPlan.AvatarRadius, above),
                $"car {cage}'s landing on the concourse is solid ground.");
            Assert.True(
                DeckReachability.CanReach(landing, gangway, above, DeckPlan.AvatarRadius, WholeComplex),
                $"car {cage} puts the captain somewhere the gangway cannot be walked to — he is marooned "
                + "in a station, which is #600 with a view of Earth.");
            Assert.True(
                DeckReachability.CanReach(
                    landing, new DeckReachability.Point(gx, gy), above, DeckPlan.AvatarRadius, WholeComplex),
                $"car {cage} puts the captain somewhere the bar cannot be walked to.");
        }

        // LEG ONE, swept: every standable tile down below reaches a car.
        (double minX, double minY, double maxX, double maxY) = Bounds;
        int seen = 0, stranded = 0;
        var worst = new List<string>();
        for (double x = minX; x <= maxX + 1e-9; x += Step)
        {
            for (double y = minY; y <= maxY + 1e-9; y += Step)
            {
                if (!DeckReachability.Standable(x, y, DeckPlan.AvatarRadius, below) || !OnTheCorridorFloor(x, y))
                {
                    continue;
                }

                seen++;
                var from = new DeckReachability.Point(x, y);
                if (cars.Any(car =>
                        DeckReachability.CanReach(from, car, below, DeckPlan.AvatarRadius, Bounds)))
                {
                    continue;
                }

                stranded++;
                if (worst.Count < 10)
                {
                    worst.Add(string.Create(CultureInfo.InvariantCulture, $"  ({x:F1}, {y:F1})"));
                }
            }
        }

        Assert.True(seen > 200, $"only {seen} tile(s) were walked — this proved nothing about a way home.");
        Assert.True(stranded == 0,
            $"{stranded} of {seen} square(s) on the service level cannot reach any car:\n"
            + string.Join("\n", worst));
    }

    private static (double MinX, double MinY, double MaxX, double MaxY) WholeComplex => (-40, -10, 40, 90);

    // ── (f) THE T STAYS UPSTAIRS ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE OBSERVATION WALK IS THE CONCOURSE'S, AND ITS RECTANGLES DO NOT LEAK DOWN A FLOOR.</b> The
    /// audit named this one exactly: <i>"the T's rectangle tests leak into the lower floor."</i> The lower
    /// level is laid in the SAME coordinate space as the hall — it is the ring's own footprint, one storey
    /// down — so every point of the gallery and the tube has a twin down there, and every one of them is a
    /// service corridor.
    ///
    /// <para><b>Proven RED</b> by dropping the level clause out of <c>InTheObservationWalk</c>: the rail's
    /// own coordinates answer OBSERVATION WALK on a floor with no window in it.</para>
    /// </summary>
    [Fact]
    public void TheWalkAndTheGalleryAnswerNothingOnTheFloorBelow()
    {
        DeckReachability.Point rail = HavenInterior.TheRailAt(Berth)!.Value;
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Berth)!.Value;

        // The MOUTH is deliberately not in this list: it is one step OUTSIDE the doorway, on the concourse,
        // which is where a captain stands to watch somebody walk out over the drop. It is in neither room on
        // either floor, and a guard that claimed otherwise would be describing a different doorway.
        foreach (DeckReachability.Point p in new[] { rail, throat })
        {
            Assert.True(HavenInterior.InTheObservationWalk(Berth, p.X, p.Y));
            Assert.False(
                HavenInterior.InTheObservationWalk(Berth, p.X, p.Y, HavenLevels.ServiceLevel),
                "a point of the T answers OBSERVATION WALK on the service level.");
        }

        Assert.True(HavenInterior.InTheGallery(Berth, rail.X, rail.Y));
        Assert.False(HavenInterior.InTheGallery(Berth, rail.X, rail.Y, HavenLevels.ServiceLevel));

        // …and the floor below says what it is, everywhere on it, through the plan's own location clause.
        Assert.Equal(HavenLevels.LowerConcoursePlate, Below.Location(rail.X, rail.Y));
        Assert.Equal(HavenLevels.LowerConcoursePlate, Below.Location(2.5, 40));
    }

    // ── (g) THE CABIN LEAVES ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>FIVE NUMBERED DOORS THAT DO NOT OPEN, AND NOT ONE NAME ON ANY OF THEM.</b>
    ///
    /// <para>Every cabin leaf is drawn LOCKED, wears the plate Core paints, and carries a knockable panel on
    /// the corridor side — the ring's own <c>Hatch</c> kind, which is the same verb and therefore not one
    /// line of new dispatch. #563's ruling is the whole of the lock: it is TIME, not a key, so there is
    /// nothing anywhere in the plan that could ever open one.</para>
    ///
    /// <para>And no names. The owner's standing rule about this station's people is that nothing confirms
    /// anything about anyone, so a plate that said whose cabin it was would be the building doing the
    /// confirming.</para>
    ///
    /// <para><b>Proven RED</b> by drawing one leaf unlocked: <c>1 cabin leaf/leaves are not locked.</c></para>
    /// </summary>
    [Fact]
    public void TheCabinsAreNumberedLockedAndNobodys()
    {
        List<DeckPlan.ConsoleSpot> plates = Below.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.Hatch)
            .ToList();
        Assert.Equal(HavenLevels.Cabins, plates.Count);

        for (int i = 0; i < HavenLevels.Cabins; i++)
        {
            string wanted = HavenLevels.CabinPlate(i + 1);
            Assert.Contains(plates, p => string.Equals(p.Label, wanted, StringComparison.Ordinal));
        }

        Assert.Equal(HavenLevels.Cabins, Below.Doors.Count(d => d.Locked));
        Assert.Equal(HavenLevels.Cabins, Below.Doors.Length);

        // Nobody is named on this floor. Every string the plan carries is checked against the game's own
        // roster of people, because a plate that learnt a name is the one thing this level may not do.
        var everyString = new List<string>();
        everyString.AddRange(Below.Consoles.Select(c => c.Label));
        everyString.AddRange(Below.RoomLabels.Select(l => l.Text));
        foreach (string regular in PatronRota.Roster)
        {
            Assert.DoesNotContain(everyString, s => s.Contains(regular, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// <b>THE DOOR PLATE IS THE ONLY SENTENCE THIS LEVEL SAYS ABOUT ITSELF.</b> The concourse side of each
    /// car wears <see cref="HavenLevels.NoPublicAccessPlate"/> — the inspectorate register every maintenance
    /// sign in this game is stencilled in — and the floor below carries exactly one label, its own name.
    /// Nothing explains anything: there is no welcome poster, no plaque, no muster point and no advertising
    /// down there, because nobody is being welcomed.
    ///
    /// <para><b>Proven RED</b> by adding any second label to the lower build.</para>
    /// </summary>
    [Fact]
    public void ThePlatesAreTheOnlyProseAndTheFloorCarriesOneLabel()
    {
        List<DeckPlan.ConsoleSpot> cars = Concourse.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.HavenLift).ToList();
        Assert.Equal(HavenLevels.Cages, cars.Count);
        Assert.All(cars, c => Assert.Contains(HavenLevels.NoPublicAccessPlate, c.Label, StringComparison.Ordinal));

        Assert.Single(Below.RoomLabels);
        Assert.Equal(HavenLevels.LowerConcoursePlate, Below.RoomLabels[0].Text);

        // Every console down there is a cabin plate or a car. Nothing else is planted on this floor.
        Assert.All(
            Below.Consoles,
            c => Assert.True(
                c.Kind is DeckPlan.ConsoleKind.Hatch or DeckPlan.ConsoleKind.HavenLift,
                $"a {c.Kind} console stands on the service level: '{c.Label}'."));
    }

    // ── (h) THE STOP LIST ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PANEL OFFERS BOTH FLOORS FROM EITHER FLOOR, AND MARKS THE ONE YOU ARE ON.</b> #600's whole bug
    /// fix, said about a berth: from any floor, the way out must never require travelling further in.
    ///
    /// <para>And it is <see cref="UndergroundComplex.LiftStop"/> that is being handed back — the same row
    /// <c>LiftPanel.razor</c> has drawn since #600 — so this feature's surface is the one the player already
    /// knows how to read, and a second lift panel is not a thing this game now has.</para>
    /// </summary>
    [Theory]
    [InlineData(HavenLevels.Concourse)]
    [InlineData(HavenLevels.ServiceLevel)]
    public void ThePanelOffersBothFloorsFromEitherOne(int level)
    {
        IReadOnlyList<UndergroundComplex.LiftStop> stops = HavenLevels.Panel(level);
        Assert.Equal(HavenLevels.Levels.Count, stops.Count);
        Assert.Single(stops, s => s.IsCurrent);
        Assert.Equal(level, stops.Single(s => s.IsCurrent).Level);
        Assert.All(stops, s => Assert.Null(s.Refusal));
        Assert.All(stops, s => Assert.True(s.Pressurised, "a haven's floor does not hold air."));
        Assert.All(stops, s => Assert.Equal(HavenLevels.NameOf(s.Level), s.Name));

        // …and the ride the press asks for is the OTHER floor, never the one under the captain's feet.
        foreach (UndergroundComplex.LiftStop stop in stops)
        {
            int? asked = HavenLevels.RideFrom(level, in stop);
            Assert.Equal(stop.IsCurrent ? null : stop.Level, asked);
        }
    }
}
