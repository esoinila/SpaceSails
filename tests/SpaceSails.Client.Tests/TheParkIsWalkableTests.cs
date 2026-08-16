using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #759 · CAN YOU ACTUALLY WALK THE PARK? — the A* flood, pointed at a curved gravel path through a room
/// the width of the base, with a wall of glass along one side of it.
///
/// <para>Owner: <i>"It must be WALKABLE, with curved paths, just like the ship park — the deck-plan park is
/// a place you stroll through, not a backdrop you look at."</i> That sentence is a reachability claim, and
/// #585's law is the one that answers it: <b>a room that exists on screen and cannot be reached is the same
/// bug class as a beacon over nothing.</b></para>
///
/// <para>And this room has a second, sharper way to be wrong, which is the one the whole feature turns on:
/// the wall it shares with the bar is GLASS. Sight crosses it; bodies do not. A guard that only checked the
/// park was reachable would pass just as happily on a park with no wall between at all — so the flood below
/// is run twice, and the second run plugs the ONE gate and demands the park go dark.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheParkIsWalkableTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    private static (DeckReachability.Point Spawn, (double, double, double, double) Bounds) Walk()
    {
        (double sx, double sy) = HiveInterior.SpawnOn(Field);
        return (new DeckReachability.Point(sx, sy),
            (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY));
    }

    /// <summary>
    /// Everywhere the captain can get to on this deck, as a lookup — ONE flood per deck rather than one A*
    /// per sample.
    ///
    /// <para>The park publishes a couple of hundred metres of gravel per site and there are eleven sites, so
    /// running <c>CanReach</c> per sample would be four thousand separate A* searches over a field this
    /// size. <see cref="DeckReachability.Reachable"/> is the same lattice, the same standability predicate
    /// and the same no-corner-cutting rule as the walk — its own summary says so — so a point in this set is
    /// exactly a point <c>CanReach</c> would have reached, and the audit loses nothing but the wait.</para>
    /// </summary>
    private sealed class Flood
    {
        private const double Step = DeckReachability.DefaultStep;
        private readonly Dictionary<(int, int), List<DeckReachability.Point>> _cells = [];

        public int Count { get; }

        public Flood(DeckPlan deck, DeckReachability.Point from, (double, double, double, double) bounds)
        {
            var reached = DeckReachability.Reachable(from, deck.CollisionField, DeckPlan.AvatarRadius, bounds);
            Count = reached.Count;
            foreach (DeckReachability.Point p in reached)
            {
                (int, int) key = ((int)Math.Floor(p.X / Step), (int)Math.Floor(p.Y / Step));
                if (!_cells.TryGetValue(key, out var bucket))
                {
                    _cells[key] = bucket = [];
                }
                bucket.Add(p);
            }
        }

        /// <summary>Is this spot on the flood? True when a walked lattice point lands within one grid step
        /// of it — the same tolerance <c>CanReach</c> itself reaches a goal with.</summary>
        public bool Holds(double x, double y)
        {
            int cx = (int)Math.Floor(x / Step), cy = (int)Math.Floor(y / Step);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!_cells.TryGetValue((cx + dx, cy + dy), out var bucket))
                    {
                        continue;
                    }
                    foreach (DeckReachability.Point p in bucket)
                    {
                        if (Math.Abs(p.X - x) <= Step && Math.Abs(p.Y - y) <= Step)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park,
        UndergroundComplex.Amenity Bar, UndergroundComplex.FloorPlan Floor)> EveryPark()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                if (floor.Park is not { } park)
                {
                    continue;
                }
                UndergroundComplex.Amenity bar = floor.Amenities.First(a => a.Hall is not null);
                yield return (body, level, park, bar, floor);
            }
        }
    }

    // ── (b) EVERY METRE OF GRAVEL ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY SAMPLE OF EVERY GRAVEL WALK CAN BE STOOD ON AND WALKED TO FROM THE LIFT — through the corridor,
    /// out of the gate at the end of it, and round the bends.
    ///
    /// <para>Both halves are asserted, and in this order, because they are different findings (#600's
    /// lesson): a sample that is not STANDABLE means a raised bed was laid on the path, and one that is
    /// standable but not REACHABLE means the gate is not a gate.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>onTheWalk</c> rejection in Core's <c>CarvePark</c> — i.e.
    /// by planting the beds on the grid and letting the gravel fall where it may:</para>
    /// <code>
    /// 767 place(s) in a park cannot be stood in or reached:
    ///   luna B1: the gravel at (-139, -242) is solid — something was planted on the path.
    ///   luna B1: the gravel at (-138, -244) cannot be reached from the lift — the gate is not a gate.
    ///   luna B1: the gravel at (-136, -245) cannot be reached from the lift — the gate is not a gate.
    ///   luna B1: the gravel at (-134, -246) is solid — something was planted on the path.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryStepOfEveryWalkIsReachableFromTheLift()
    {
        var bad = new List<string>();
        int parks = 0, steps = 0;

        (DeckReachability.Point spawn, var bounds) = Walk();

        foreach ((string body, int level, UndergroundComplex.Park park, _, _) in EveryPark())
        {
            DeckPlan deck = DeckFor(body, level);
            parks++;

            Assert.True(DeckReachability.Standable(spawn.X, spawn.Y, DeckPlan.AvatarRadius, deck.CollisionField),
                $"{body} B{-level}: the walk starts inside a wall — the verdict would mean nothing.");

            var flood = new Flood(deck, spawn, bounds);
            Assert.True(flood.Count > 2000,
                $"{body} B{-level}: the flood only found {flood.Count} standable squares on a whole "
                + "floor — it never left the lift, and every verdict below would be vacuous.");

            foreach ((double px, double py) in park.Walk.Append((park.X, park.Y)))
            {
                steps++;
                if (!DeckReachability.Standable(px, py, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    bad.Add($"  {body} B{-level}: the gravel at ({px:F0}, {py:F0}) is solid — something was "
                        + "planted on the path.");
                }
                else if (!flood.Holds(px, py))
                {
                    bad.Add($"  {body} B{-level}: the gravel at ({px:F0}, {py:F0}) cannot be reached from "
                        + "the lift — the gate is not a gate.");
                }
            }

            // …and every bench is a thing you can walk UP to, which is what a bench is for.
            foreach ((double bx, double by) in park.Benches)
            {
                double toward = Math.Sign(((park.Y0 + park.Y1) / 2.0) - by) * 2.6;
                if (!flood.Holds(bx, by + toward))
                {
                    bad.Add($"  {body} B{-level}: nobody can get to the bench at ({bx:F0}, {by:F0}).");
                }
            }
        }

        Assert.True(parks >= 10, $"only {parks} parks were flooded — this proved little.");
        Assert.True(steps > 1500, $"only {steps} places in a park were walked to — this proved little.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} place(s) in a park cannot be stood in or reached:\n"
            + string.Join("\n", bad.Take(20)));
    }

    // ── (c) THE GLASS: DRAWN AS GLASS, AND NOT A WAY IN ───────────────────────────────────────────────

    /// <summary>
    /// EVERY WALL BETWEEN THE PARK AND A ROOM IS DRAWN AS GLASS — the hall's, and every ring room's, all
    /// the way round the block. On exactly the line Core published, carrying <c>IsWindow</c>, with no poured
    /// wall doubling any of it.
    ///
    /// <para>#813 · This guard used to say <i>ONE wall</i>, and it was right to: the park had a single
    /// glazed side and painted rock on the other three, so <c>Assert.Single</c> was the whole truth. The
    /// Manhattan ruling makes the green the middle of a city block, and its second law is that <b>every du
    /// of the park's perimeter that is not a gate is a room's park-facing wall, and that wall is glass</b>.
    /// So the guard is pointed at the same claim over a bigger world rather than relaxed: not "the window
    /// wall is one segment" but "every window wall is drawn as one, on both axes".</para>
    ///
    /// <para>BOTH AXES is not a detail. The west and east ring rooms' view walls are VERTICAL — the first
    /// vertical glazing in the building — and a check that reads a segment's <c>Y1</c> to find it would sail
    /// past every one of them and report that it had proved something.</para>
    ///
    /// <para>AND THE HOLE IS ACCOUNTED FOR. A far-band room keeps #801's own door off the gravel, so its
    /// glass reaches the deck as the two panes either side of that door. The covered length is measured
    /// against the room's OWN published gate rather than against a tolerance: a pane short by a doorway is
    /// the room's door, and a pane short by anything else is a hole nobody built.</para>
    ///
    /// <para><b>Proven RED</b> by flipping ONE flag in <c>HiveInterior.FloorDeck</c> — the glazing still
    /// reaches the deck, still stands there, still stops a body, and simply draws in the poured-hull ink:</para>
    /// <code>
    /// luna B1: the wall between the bar and the park draws as poured concrete. Both of this room's own
    /// pictures are shot THROUGH it at the green.
    /// </code>
    ///
    /// <para>That edit leaves <see cref="PouringTheGateShutTakesTheWholeParkDARK"/> GREEN, which is the
    /// point of having two guards: this one owns the DRAWN half and that one owns the SIMULATED half, and
    /// neither can cover for the other.</para>
    /// </summary>
    [Fact]
    public void TheWindowWallIsDrawnAsGlass()
    {
        var bad = new List<string>();
        int walls = 0, vertical = 0, withADoorInThem = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _, _) in EveryPark())
        {
            DeckPlan deck = DeckFor(body, level);

            // EVERY PANE THE ROOM PUBLISHES: the hall's own glass, and then the whole ring. Off the two
            // published lists, so a floor that stopped glazing a side of its block cannot pass by being
            // asked about the side it still glazes.
            var panes = new List<(string What, SurfaceLayout.Wall Glass, double DoorWidth)>
            {
                ("the wall between the bar and the park", park.Window, 0.0),
            };
            foreach (UndergroundComplex.RingRoom room in park.Frontage)
            {
                if (room.View is not { } view)
                {
                    continue;   // a corner room stands past the end of the park's wall: no park, no pane
                }
                double doorWidth = room.Gate is { } gate
                    ? Math.Max(Math.Abs(gate.X2 - gate.X1), Math.Abs(gate.Y2 - gate.Y1))
                    : 0.0;
                panes.Add(($"ring room {room.Number} ({room.Side})'s window wall", view, doorWidth));
            }

            foreach ((string what, SurfaceLayout.Wall glass, double doorWidth) in panes)
            {
                bool horizontal = Math.Abs(glass.Y1 - glass.Y2) < 0.01;
                double lo = horizontal ? Math.Min(glass.X1, glass.X2) : Math.Min(glass.Y1, glass.Y2);
                double hi = horizontal ? Math.Max(glass.X1, glass.X2) : Math.Max(glass.Y1, glass.Y2);
                double on = horizontal ? glass.Y1 : glass.X1;

                // On the same LINE and overlapping the same SPAN, asked on the pane's own axis. A drawn
                // segment that merely touches an end is a neighbour's pane and not this one.
                var onTheLine = deck.Walls.Where(w =>
                {
                    bool wh = Math.Abs(w.Y1 - w.Y2) < 0.01;
                    if (wh != horizontal)
                    {
                        return false;
                    }
                    if (Math.Abs((horizontal ? w.Y1 : w.X1) - on) > 0.01)
                    {
                        return false;
                    }
                    double wLo = horizontal ? Math.Min(w.X1, w.X2) : Math.Min(w.Y1, w.Y2);
                    double wHi = horizontal ? Math.Max(w.X1, w.X2) : Math.Max(w.Y1, w.Y2);
                    return wLo < hi - 0.01 && wHi > lo + 0.01;
                }).ToList();

                if (onTheLine.Count == 0)
                {
                    bad.Add($"  {body} B{-level}: {what} ({lo:F1}…{hi:F1} at {(horizontal ? "y" : "x")} "
                        + $"{on:F1}) is not on the deck in ANY form — the room looks out at a park with no "
                        + "wall between, and walks straight into it.");
                    continue;
                }

                walls++;
                if (!horizontal)
                {
                    vertical++;
                }
                if (doorWidth > 0.01)
                {
                    withADoorInThem++;
                }

                foreach (DeckPlan.Wall drawn in onTheLine)
                {
                    Assert.True(drawn.IsWindow,
                        $"{body} B{-level}: {what} draws as poured concrete. Both of this room's own "
                        + "pictures are shot THROUGH it at the green.");
                }

                // The panes together span the whole wall, less exactly the doorway the room published in
                // it. Endpoints first, because a pane that stops short at one end is a different finding
                // from a pane with a hole in the middle of it.
                Assert.Equal(lo, onTheLine.Min(w => horizontal
                    ? Math.Min(w.X1, w.X2) : Math.Min(w.Y1, w.Y2)), 1);
                Assert.Equal(hi, onTheLine.Max(w => horizontal
                    ? Math.Max(w.X1, w.X2) : Math.Max(w.Y1, w.Y2)), 1);

                double covered = onTheLine.Sum(w => horizontal
                    ? Math.Abs(w.X2 - w.X1) : Math.Abs(w.Y2 - w.Y1));
                Assert.True(Math.Abs(covered - ((hi - lo) - doorWidth)) < 0.05,
                    $"{body} B{-level}: {what} is {hi - lo:F1} du long and only {covered:F1} du of it is "
                    + $"glazed, with a published door {doorWidth:F1} du wide in it. The rest is a hole "
                    + "nobody built.");
            }
        }

        Assert.True(walls >= 100, $"only {walls} window walls were checked — this proved little.");
        Assert.True(vertical >= 20,
            $"only {vertical} of them ran VERTICALLY — the block's two ends are not being read at all, and "
            + "a check that only ever sees horizontal glass is the one that would miss them.");
        Assert.True(withADoorInThem >= 20,
            $"only {withADoorInThem} panes had a door cut in them — the split-pane case is not being "
            + "exercised and the arithmetic above proves nothing about it.");
        Assert.True(bad.Count == 0, $"{bad.Count} pane(s) draw no glass at all:\n" + string.Join("\n", bad));
    }

    /// <summary>
    /// …AND IT IS STILL A WALL. Nobody stands on it, and — the half that matters — <b>the published
    /// openings are the only ways in</b>: plug every one of them and the whole park goes unreachable.
    ///
    /// <para>This is the #488/#600 experiment in its sharpest form. A park behind glass and a park behind a
    /// hole in the wall are the same picture from the bar; the only thing that tells them apart is what
    /// happens when the legitimate doors are shut. If any gravel is still reachable with them poured shut,
    /// then something crosses that glass — and the room's whole design ("you always meet the park through
    /// glass before you walk it") is decoration.</para>
    ///
    /// <para>#813 · THE LIST OF LEGITIMATE DOORS GREW AGAIN, and the guard grows with it rather than
    /// loosening. #775 took it from one gate to every gate; the Manhattan carve rings the green with rooms,
    /// and the far band keeps #801's own doors onto the gravel — which are ways OUT of the park into a room
    /// and are deliberately not <see cref="UndergroundComplex.Park.Ways"/>. Both lists are plugged here.
    /// Sealing only the Ways and finding the gravel still reachable would have been a true report of a
    /// perfectly correct building, which is how a sealing experiment starts lying: not by missing a bug, but
    /// by counting a door it forgot as a hole in the glass.</para>
    ///
    /// <para><b>Proven RED</b> twice, at two depths.</para>
    ///
    /// <para>By publishing NO glazing at all (<c>if (!glazed) walls.Add(farWall);</c> in <c>CarveHall</c>) —
    /// i.e. by letting the bar open straight into the park. The first assertion catches it:</para>
    /// <code>
    /// luna B1: the window wall can be stood on — it is a picture, not a wall.
    /// </code>
    ///
    /// <para>And then, sharper, by leaving the glass standing with one doorway-sized hole cut in it
    /// (<c>double gapU = width * 0.75;</c>, two glass segments either side of it) — a wall that is still
    /// there, still drawn as glass, and still solid everywhere this test's first assertions look:</para>
    /// <code>
    /// titan B1: 188 of 189 gravel samples are STILL reachable with the park's only gate poured shut.
    /// Something crosses the window wall.
    /// </code>
    /// </summary>
    [Fact]
    public void PouringTheGateShutTakesTheWholeParkDARK()
    {
        (DeckReachability.Point spawn, var bounds) = Walk();
        int proved = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.Amenity bar, _)
            in EveryPark())
        {
            DeckPlan honest = DeckFor(body, level);

            // NOBODY STANDS ON THE GLASS. The midpoint of the segment, which is a metre of wall in the
            // middle of a room a captain will walk the length of.
            double mx = (park.Window.X1 + park.Window.X2) / 2.0;
            Assert.False(
                DeckReachability.Standable(mx, park.Window.Y1, DeckPlan.AvatarRadius, honest.CollisionField),
                $"{body} B{-level}: the window wall can be stood on — it is a picture, not a wall.");

            // THE WORLD IS HONEST FIRST: there is real floor a step behind the glass, and the bar in front
            // of it is a room the captain can walk to — so the red below is about the WALL and not about a
            // coordinate nobody could ever have stood on. The bar's side is taken at THE COUNTER, the
            // amenity's own published spot, rather than at a du off the glass: the strip immediately behind
            // the pane is the service side of the counter, which is walled off from the room on purpose
            // (#751) and is not where a customer stands.
            double into = Math.Sign(((park.Y0 + park.Y1) / 2.0) - park.Window.Y1) * 2.5;
            var inPark = new DeckReachability.Point(mx, park.Window.Y1 + into);
            var inHall = new DeckReachability.Point(bar.X, bar.Y);
            Assert.True(DeckReachability.Standable(inPark.X, inPark.Y, DeckPlan.AvatarRadius, honest.CollisionField),
                $"{body} B{-level}: there is no floor behind the glass.");
            Assert.True(DeckReachability.Standable(inHall.X, inHall.Y, DeckPlan.AvatarRadius, honest.CollisionField),
                $"{body} B{-level}: there is no floor at the counter.");

            var open = new Flood(honest, spawn, bounds);
            int reachable = park.Walk.Count(p => open.Holds(p.X, p.Y));
            Assert.True(reachable == park.Walk.Count,
                $"{body} B{-level}: the park is already unwalkable ({reachable} of {park.Walk.Count}) — "
                + "this experiment would prove nothing.");

            // …and now EVERY OPENING IN THE PARK'S BOUNDARY is poured shut. Taken off the room's own two
            // published lists rather than re-derived, so this cannot plug a hole that is not actually a
            // door — and cannot miss one.
            //
            // #775 · It used to plug exactly one, because there was exactly one. The park is a THOROUGHFARE
            // now (owner: "it is a kind of place people like to walk through on their way"), and a sealing
            // experiment that misses a door is an experiment that proves nothing while reporting that it
            // proved something. The spirit is unchanged and is the whole point: with every way in bricked
            // up, not one gravel sample may be reachable — anything that still is, is crossing the glass.
            //
            // #813 · …and BackDoors with them. A Way is a way THROUGH the green, corridor to corridor; a
            // BackDoor is a way OUT of it into one of the far band's rooms, and Core keeps the two lists
            // apart on purpose (the amenities' conservation sum counts a Way as a place). Both are doors a
            // body walks through, so both are plugged — a sealing experiment does not get to inherit a
            // bookkeeping distinction.
            //
            // AND IF A THIRD KIND EVER ARRIVES, IT BELONGS HERE TOO. This experiment is only as strong as
            // its list of openings, and the list is enumerated by hand from the two Core publishes today.
            // A future opening onto the gravel that neither Ways nor BackDoors carries would leave this
            // guard passing while proving nothing — which is exactly what it did between #813's carve and
            // the moment BackDoors was added to the line below: it reported a bug in a correct building,
            // and the fix is the same either way. Anything that adds a way onto the green adds it here.
            UndergroundComplex.FloorPlan drawn = UndergroundComplex.Build(body, level, Field);
            var plugged = new List<DeckPlan.Wall>(honest.Walls);
            var openings = new List<SurfaceLayout.Doorway>(park.Ways);
            openings.AddRange(park.BackDoors);
            foreach (SurfaceLayout.Doorway gate in openings)
            {
                Assert.Contains(gate, drawn.Doorways);

                // Grown ACROSS the opening, whichever way the opening runs — the block's west and east
                // gates are VERTICAL, and a plug padded on a fixed axis would lie along one of them and
                // seal nothing at all while reporting that it had.
                bool horizontal = Math.Abs(gate.Y1 - gate.Y2) < 0.001;
                float px = horizontal ? 0f : 0.5f, py = horizontal ? 0.5f : 0f;
                plugged.Add(new DeckPlan.Wall(
                    (float)gate.X1 - px, (float)gate.Y1 - py,
                    (float)gate.X2 + px, (float)gate.Y2 + py, false, true));
            }
            Assert.True(park.Ways.Count > 1,
                $"{body} B{-level}: the park has one way in — #775's thoroughfare never happened, and this "
                + "experiment is the one that would not notice.");
            Assert.True(park.BackDoors.Count > 0,
                $"{body} B{-level}: the park has nothing opening off its gravel — #801's back of house "
                + "never happened, and the second half of this plug is plugging an empty list.");

            var sealedDeck = new DeckPlan(
                [.. plugged], honest.Consoles, honest.RoomLabels, honest.Backdrops,
                honest.SpawnX, honest.SpawnY, 0, (_, _) => { }, (_, _) => "sealed");

            var shut = new Flood(sealedDeck, spawn, bounds);
            int still = park.Walk.Count(p => shut.Holds(p.X, p.Y));

            Assert.True(still == 0,
                $"{body} B{-level}: {still} of {park.Walk.Count} gravel samples are STILL reachable with "
                + $"every one of the park's {openings.Count} published openings poured shut. Something "
                + "crosses the window wall.");

            // …and the hall on the other side of that glass is untouched by the plug, which is what makes
            // the result above a statement about the WALL rather than about a floor that fell apart.
            Assert.True(shut.Holds(inHall.X, inHall.Y),
                $"{body} B{-level}: plugging the park's gate also sealed the bar — the experiment measured "
                + "the wrong thing.");

            proved++;
        }

        Assert.True(proved >= 10, $"only {proved} parks could be deliberately sealed — the experiment never ran.");
    }

    // ── (d) THE ART SEAM, ON THE DECK ─────────────────────────────────────────────────────────────────

    /// <summary>The park's floor wears its picture through the seam the hall's floor already used, in the
    /// panels Core cut — and the hall beside it now wears the frame with the window in it.</summary>
    [Fact]
    public void TheParkWearsItsArtAndTheHallWearsTheParkView()
    {
        int parks = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, UndergroundComplex.Amenity bar, _)
            in EveryPark())
        {
            DeckPlan deck = DeckFor(body, level);
            UndergroundComplex.Hall hall = bar.Hall!.Value;
            parks++;

            var panels = deck.Backdrops.Where(b => b.Url == UndergroundComplex.ParkArtUrl).ToList();
            Assert.Equal(UndergroundComplex.ParkArtPanels(park).Count, panels.Count);
            foreach (DeckPlan.Backdrop b in panels)
            {
                // Top-left, W, H — the ship's own convention, and the panels together span the park exactly.
                Assert.Equal((float)park.Y1, b.Y, 1);
                Assert.Equal((float)(park.Y1 - park.Y0), b.H, 1);
                Assert.InRange(b.X, (float)park.X0 - 0.1f, (float)park.X1 + 0.1f);
            }
            Assert.Equal((float)park.X0, panels.Min(b => b.X), 1);
            Assert.Equal((float)park.X1, panels.Max(b => b.X + b.W), 1);

            // …and the room on the other side of the glass wears the picture that HAS the glass in it.
            Assert.Equal(UndergroundComplex.CantinaHallArtUrl, hall.ArtUrl);
            Assert.Contains(deck.Backdrops, b =>
                b.Url == UndergroundComplex.CantinaHallArtUrl && Math.Abs(b.X - (float)hall.X0) < 0.1);
        }

        Assert.True(parks >= 10, $"only {parks} parks were painted — this proved little.");
    }

    /// <summary>The park's own signage is on the deck: the plate at the gate, a stencil on every bed, a plate
    /// on every bench, and one figure.
    ///
    /// <para>#793 · AND EXACTLY ONE VERB, WHICH IT CAN HONOUR. This guard shipped as <i>"nothing in a park
    /// answers [E] yet, and a console that answers nothing is #757's complaint restated in a garden"</i>, and
    /// it was right on both halves — which is why it is pointed the other way here rather than weakened. The
    /// benches take the sit verb now, so the park has ONE console kind and this says exactly which: one
    /// <c>HiveBench</c> per bench, on the bench's own spot, wearing <see cref="ParkBenches"/>' plate — and
    /// nothing else in the room pressable at all. The beds, the masts, the gate plate and the lone figure
    /// are still signage. A second verb turning up in a park fails this as loudly as the first one did.</para>
    /// </summary>
    [Fact]
    public void TheParkIsSignedAndOffersNoVerbItCannotHonour()
    {
        int parks = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, _, _) in EveryPark())
        {
            DeckPlan deck = DeckFor(body, level);
            parks++;

            Assert.Contains(deck.RoomLabels, l => l.Text == UndergroundComplex.ParkPlate);
            foreach (UndergroundComplex.GrowingBed bed in park.Beds)
            {
                Assert.Contains(deck.RoomLabels, l => l.Text == bed.Plate);
            }
            Assert.Equal(
                park.Benches.Count,
                deck.RoomLabels.Count(l => l.Text == UndergroundComplex.ParkBenchPlate));
            Assert.Contains(deck.RoomLabels, l => l.Text == park.FigurePlate);
            Assert.Contains(park.FigurePlate, CanteenRegulars.StrangerPlates);

            // #793 · ONE verb in the room, and it is the bench's. Everything else — the beds, the masts, the
            // gate's own plate, and the lone figure on the far bench — is still signage with nothing to
            // press: you sit down NEXT to the figure, you do not talk to them.
            var pressable = deck.Consoles.Where(c => park.Contains(c.X, c.Y)).ToList();
            foreach (DeckPlan.ConsoleSpot c in pressable)
            {
                Assert.True(c.Kind == DeckPlan.ConsoleKind.HiveBench,
                    $"{body} B{-level}: a console ({c.Label}) stands in the park with nothing to answer.");
            }

            // …and there is exactly one of them per bench, on the bench's own spot, wearing Core's plate.
            Assert.Equal(park.Benches.Count, pressable.Count);
            foreach (ParkBenches.Bench bench in ParkBenches.On(in park))
            {
                ParkBenches.Bench b = bench;
                DeckPlan.ConsoleSpot on = Assert.Single(
                    pressable, c => Math.Abs(c.X - b.X) < 0.01 && Math.Abs(c.Y - b.Y) < 0.01);
                Assert.Equal(b.DeckPlate, on.Label);
            }
        }

        Assert.True(parks >= 10, $"only {parks} parks were read — this proved little.");
    }

    // ── (e) ATTENDANCE IS RECORDED, ONCE ──────────────────────────────────────────────────────────────

    /// <summary>
    /// THE ATTENDANCE LINE FILES ONCE PER EXCURSION AND FROM ONE PLACE — #751's own latch discipline, on a
    /// note with no card behind it.
    ///
    /// <para>Same scan as <see cref="TheHallCardsAreRaisedOnceTests"/>, and for the same reason: the wiring
    /// is what is left to get wrong once Core owns the room. A note that is never filed is a plate nobody is
    /// told anything by; one filed on every frame is a field book that fills up with one sentence while the
    /// captain stands on the gravel.</para>
    ///
    /// <para><b>Proven RED</b> by deleting <c>ex.HiveParkNoteFiled</c> from the poll's guard:</para>
    /// <code>
    /// the filing of ParkNote never READS ex.HiveParkNoteFiled — an unread latch fences nothing, and this
    /// line would file on every frame the captain stands in the park.
    /// </code>
    /// </summary>
    [Fact]
    public void TheAttendanceNoteFilesOnceFromOnePlace()
    {
        string root = RepoRoot();
        var raises = new List<(string File, int At)>();
        foreach (string file in Directory.EnumerateFiles(
            Path.Combine(root, "src", "SpaceSails.Client"), "*.*", SearchOption.AllDirectories))
        {
            char s = Path.DirectorySeparatorChar;
            if (Path.GetExtension(file) is not (".cs" or ".razor")
                || file.Contains($"{s}obj{s}", StringComparison.Ordinal)
                || file.Contains($"{s}bin{s}", StringComparison.Ordinal))
            {
                continue;
            }
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"UndergroundComplex\.ParkNote\b"))
            {
                raises.Add((file, m.Index));
            }
        }

        (string source, int at) = Assert.Single(raises);
        string text = File.ReadAllText(source);
        int from = text.LastIndexOf("\n    private ", at, StringComparison.Ordinal);
        int to = text.IndexOf("\n    private ", at, StringComparison.Ordinal);
        Assert.True(from >= 0, "could not find the member that files the park's note.");
        string block = text[from..(to < 0 ? text.Length : to)];

        Assert.True(block.Contains("ex.HiveParkNoteFiled = true", StringComparison.Ordinal),
            "the filing of ParkNote never SPENDS ex.HiveParkNoteFiled — a latch that is written nowhere "
            + "never closes.");
        Assert.True(Regex.Matches(block, @"\bex\.HiveParkNoteFiled\b").Count >= 2,
            "the filing of ParkNote never READS ex.HiveParkNoteFiled — an unread latch fences nothing, and "
            + "this line would file on every frame the captain stands in the park.");

        // It asks CORE whether the room holds you — the park's own published box — rather than measuring a
        // rectangle here, and it lays the floor out once per floor rather than once per tick (#751's own
        // finding about a poll that calls Build).
        Assert.Contains("UndergroundComplex.Build(", block, StringComparison.Ordinal);
        Assert.Contains("_parkFor != (ex.Stop.Body.Id, ex.Floor)", block, StringComparison.Ordinal);
        Assert.Contains(".Contains(_avatarX, _avatarY)", block, StringComparison.Ordinal);

        // And it is filed as a PULSE + a book line (ShowAndFile), because there is no card over the top of
        // it here — which is the one difference from the cabinet's note and is deliberate.
        Assert.Contains("ShowAndFile(", block, StringComparison.Ordinal);
    }

    /// <summary>#693's rule — <i>a scene nobody can reach on demand is a scene that ships broken</i> — for a
    /// room that is behind the largest room in the game and through the one gate on the floor. The URL is
    /// parsed, the last leg is walked to the park's OWN published spot, and the front door lists it.</summary>
    [Fact]
    public void ThereIsAOneUrlRouteIntoThePark()
    {
        string root = RepoRoot();
        // #870 · the sim page is nine partials by subject now; "the sim" here is all of them.
        string sim = string.Concat(
            Directory.EnumerateFiles(
                    Path.Combine(root, "src", "SpaceSails.Client", "Pages"), "Map.Sim*.cs")
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(File.ReadAllText));
        string surface = File.ReadAllText(
            Path.Combine(root, "src", "SpaceSails.Client", "Pages", "Map.Surface.Cheats.cs"));

        Assert.Contains("pair.StartsWith(\"park=\"", sim, StringComparison.Ordinal);
        Assert.Contains("_parkCheat = true", sim, StringComparison.Ordinal);

        int at = surface.IndexOf("StandInTheParkIfAsked(SurfaceExcursion", StringComparison.Ordinal);
        Assert.True(at > 0, "?park=1 parses and nothing ever walks the last leg.");
        string stand = surface[at..(at + 1400)];
        Assert.Contains("UndergroundComplex.Build(", stand, StringComparison.Ordinal);
        Assert.Contains("StandCaptainAt(green.X, green.Y", stand, StringComparison.Ordinal);

        Assert.Contains(DevStarts.All, e => e.Url == "/map?park=1");
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }
}
