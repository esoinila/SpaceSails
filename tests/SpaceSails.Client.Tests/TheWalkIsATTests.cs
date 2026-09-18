using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1199 (2026-09-18) · <b>THE WALK IS A T</b>, driven against the built deck.
///
/// <para>Owner, live on the walk: <i>"Let's have a wider place at the far end of the observation place… like
/// somewhere where 10 people can stand side to side and watch out — that is how these usually are built: a
/// tube, then an area to view… like the letter T — now we have the foot of the letter ready."</i></para>
///
/// <para>Every claim here is measured off <see cref="HavenInterior.DockedDeck"/> — the walls the renderer
/// draws and the captain collides with — rather than off the constants that built them. A guard that read
/// the same numbers the builder read would agree with whatever the builder did, which is this repository's
/// <i>green test that asserts nothing</i> class with a tape measure in its hand.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheWalkIsATTests
{
    private static string Berth => ObservationWalk.HavenId;

    private static DeckPlan Deck => HavenInterior.DockedDeck(Berth)!;

    /// <summary>The bounds every lattice sweep in this file runs over — the whole complex with the T's own
    /// reach added, so a room hung off the west side of the ring is inside the search rather than outside
    /// it. Measured off the room's own published box, never typed.</summary>
    private static (double MinX, double MinY, double MaxX, double MaxY) Bounds
    {
        get
        {
            (double gx0, double gy0, _, double gy1) = HavenInterior.TheGalleryBox(Berth)!.Value;
            return (gx0 - 4, Math.Min(gy0 - 4, -4), 40, Math.Max(gy1 + 4, 90));
        }
    }

    // ── THE CROSSBAR ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>TEN PEOPLE, SIDE BY SIDE, BY MEASUREMENT.</b> The owner's own number
    /// (<see cref="ObservationWalk.AbreastAtTheRail"/>) asked of the glass they would be standing at: the
    /// gallery's outer wall is found on the built deck, its length is measured, and ten bodies at one body
    /// width each have to fit along it.
    ///
    /// <para>And a second claim, which is the owner's addendum: the hat is WIDER than that minimum
    /// (<i>"by making the T-character wider hatted"</i>). A room built to exactly ten bodies is a corridor
    /// turned sideways with ten people wedged in it, and the slack is the floor the cafeteria stands on.</para>
    ///
    /// <para><b>Revert that reddens it:</b> <c>GalleryWidthDu</c> set back to the ten-abreast minimum — the
    /// wide-hat assert goes red; set to a tube's width and the abreast assert goes red with it.</para>
    /// </summary>
    [Fact]
    public void TheGalleryIsTenAbreastAtTheGlassAndWiderThanTheMinimum()
    {
        (double westX, double southY, _, double northY) = HavenInterior.TheGalleryBox(Berth)!.Value;

        // The outer wall, found on the DECK by where it is rather than by what built it: the westernmost
        // vertical run in the whole plan. Nothing else in this station reaches that far.
        DeckPlan.Wall outer = Deck.Walls
            .Where(s => Math.Abs(s.X1 - s.X2) < 1e-3)
            .OrderBy(s => s.X1)
            .First();

        Assert.Equal(westX, outer.X1, 3);
        Assert.True(outer.IsWindow, "the gallery's outer wall is glass, like the walk's own three.");

        double railDu = Math.Abs(outer.Y2 - outer.Y1);
        Assert.Equal(northY - southY, railDu, 3);

        double aBodyIs = 2 * DeckPlan.AvatarRadius;
        Assert.True(
            railDu >= ObservationWalk.AbreastAtTheRail * aBodyIs,
            $"the rail is {railDu:0.00} du — {ObservationWalk.AbreastAtTheRail} bodies want "
            + $"{ObservationWalk.AbreastAtTheRail * aBodyIs:0.00}.");

        // …and ten TILES, which is the other half of the ask and the weaker of the two.
        Assert.True(railDu >= ObservationWalk.AbreastAtTheRail);

        // The wide hat: strictly more than the minimum, and by a real margin rather than a rounding.
        Assert.True(
            railDu > ObservationWalk.AbreastAtTheRail * aBodyIs + aBodyIs,
            $"the hat is not wider than its own minimum ({railDu:0.00} du).");
    }

    /// <summary>#1199 · …AND IT IS A GALLERY RATHER THAN A LOUNGE. Two paces deep — the fire code's own
    /// bedroom-small number — measured on the deck between the outer glass and the back wall.
    ///
    /// <para><b>Revert that reddens it:</b> <c>GalleryDepthDu</c> doubled.</para></summary>
    [Fact]
    public void TheGalleryIsTwoPacesDeepAndTheCafeteriaHasTheInnerHalf()
    {
        (double westX, _, double eastX, _) = HavenInterior.TheGalleryBox(Berth)!.Value;
        double depth = eastX - westX;

        Assert.Equal(UndergroundComplex.FireCodeSmallRoomDu, depth, 3);
        Assert.Equal(ObservationWalk.CafeteriaBandDu, depth / 2.0, 3);

        // The band is the INNER half, and the rail's own half is the other one.
        Assert.True(HavenInterior.InTheCafeteriaBand(Berth, eastX - 0.5, 40));
        Assert.False(HavenInterior.InTheCafeteriaBand(Berth, westX + 0.5, 40));
    }

    /// <summary>
    /// #1199 · <b>THE TUBE IS THE ONLY WAY IN — TO THE WHOLE T.</b> Every door the plan hangs is measured
    /// against BOTH of the room's boxes, grown by a body's width so an opening cut in any face of either is
    /// inside the count, and exactly one of them is in there.
    ///
    /// <para>This is what keeps #822's named exemption honest now that the room has grown, and it is the
    /// half of the tell #1233 rides: the walk's tube is the SECOND doorway a coat can be counted through,
    /// and it stays exactly one doorway.</para>
    ///
    /// <para><b>Revert that reddens it:</b> an auto-door added across the throat where the stem meets the hat
    /// — <i>2 ways into the T</i>. And the opposite: the gallery walled off from the tube, which strands it
    /// at zero and fails the reachability guard below.</para>
    /// </summary>
    [Fact]
    public void TheTubeIsTheOnlyWayIntoTheWholeT()
    {
        const double Reach = DeckPlan.AvatarRadius;
        var boxes = new[]
        {
            HavenInterior.TheWalksBox(Berth)!.Value,
            HavenInterior.TheGalleryBox(Berth)!.Value,
        };

        int ways = 0;
        foreach (DeckPlan.Door door in Deck.Doors)
        {
            double mx = (door.X1 + door.X2) / 2.0, my = (door.Y1 + door.Y2) / 2.0;
            if (!boxes.Any(b => mx >= b.X0 - Reach && mx <= b.X1 + Reach
                                && my >= b.Y0 - Reach && my <= b.Y1 + Reach))
            {
                continue;
            }

            ways++;
            Assert.False(door.Locked, "the one way into the T is not a leaf anybody is refused at.");
        }

        Assert.Equal(ObservationWalk.Doorways, ways);
    }

    /// <summary>
    /// #1199 · <b>EVERY TILE OF THE GALLERY IS REACHABLE FROM THE CONCOURSE</b>, on the captain's own
    /// lattice, with no corner-cutting — so the crossbar is floor rather than a shape drawn beyond a wall.
    ///
    /// <para>Swept rather than sampled: every lattice point inside the gallery's box that a body could stand
    /// on at all must be in the reachable set grown from the bar's own threshold. A room you can see and not
    /// walk into is the #600 lift bug with a view.</para>
    ///
    /// <para><b>Revert that reddens it:</b> the throat welded shut (the old blind wall put back) — the whole
    /// crossbar drops out of the set at once.</para>
    /// </summary>
    [Fact]
    public void EveryStandableTileOfTheGalleryIsReachable()
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = Deck.CollisionField;
        (double x, double y, _) = HavenInterior.BarThreshold;
        const double Step = 0.5;

        IReadOnlyCollection<DeckReachability.Point> reached = DeckReachability.Reachable(
            new DeckReachability.Point(x, y), walls, DeckPlan.AvatarRadius, Bounds, Step);

        var got = new HashSet<(int, int)>(
            reached.Select(p => ((int)Math.Round(p.X / Step), (int)Math.Round(p.Y / Step))));

        (double gx0, double gy0, double gx1, double gy1) = HavenInterior.TheGalleryBox(Berth)!.Value;
        int standable = 0, missed = 0;
        for (double px = gx0; px <= gx1 + 1e-9; px += Step)
        {
            for (double py = gy0; py <= gy1 + 1e-9; py += Step)
            {
                if (SurfaceCollision.Blocked(px, py, DeckPlan.AvatarRadius, walls))
                {
                    continue;
                }

                standable++;
                if (!got.Contains(((int)Math.Round(px / Step), (int)Math.Round(py / Step))))
                {
                    missed++;
                }
            }
        }

        // Anti-vacuity: a crossbar with no standable floor in it would pass a "nothing was missed" assert.
        Assert.True(standable > 100, $"only {standable} standable tiles in the gallery — that is not a room.");
        Assert.Equal(0, missed);
    }

    // ── WHAT THE SHAPE IS FOR ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>FROM THE RAIL, THE WAY IN CANNOT SEE YOU — AND YOU CANNOT SEE IT.</b>
    ///
    /// <para>Owner, 2026-09-18: <i>"The T could even be curved — both leg and hat. For tailing it would make
    /// sense."</i> This is what the curve is FOR, and it is the one property the whole beat rests on: a
    /// straight tube with the rail dead centre keeps the person being followed in the captain's line down its
    /// whole length, so he holds at the mouth for ever and nothing happens. Here the line from the rail to the
    /// concourse doorway runs into the gallery's own back wall, in both directions, over the game's one
    /// sightline oracle and the deck's own stone.</para>
    ///
    /// <para><b>Revert that reddens it:</b> the rail put back on the T's axis (drop the quarter-width offset
    /// in <c>TheRailAt</c>) — the tube is a clear shot from end to end and both asserts go red.</para>
    /// </summary>
    [Fact]
    public void TheRailAndTheWayInCannotSeeEachOther()
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = Deck.CollisionField;
        DeckReachability.Point rail = HavenInterior.TheRailAt(Berth)!.Value;
        DeckReachability.Point mouth = HavenInterior.TheWalksMouthAt(Berth)!.Value;

        Assert.False(
            SurfaceCollision.HasLineOfSight(rail.X, rail.Y, mouth.X, mouth.Y, walls),
            "the tube's mouth can see the rail — the beat stalls there.");
        Assert.False(
            SurfaceCollision.HasLineOfSight(mouth.X, mouth.Y, rail.X, rail.Y, walls),
            "the rail can see the tube's mouth.");

        // Anti-vacuity: the oracle is not simply answering NO for this room. The throat — the opening where
        // the leg meets the hat — IS in line from the mouth, which is how a captain sees somebody go in.
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Berth)!.Value;
        Assert.True(SurfaceCollision.HasLineOfSight(mouth.X, mouth.Y, throat.X, throat.Y, walls),
            "the mouth cannot even see down its own tube.");
    }

    /// <summary>
    /// #1199 · <b>THE CAFETERIA'S TABLES ARE STAKEOUT SEATS.</b> Owner, 2026-09-18: <i>"the tables at the hat
    /// would be good stakeout positions to enjoy the vending machine / automated teller service while enjoying
    /// the view."</i>
    ///
    /// <para>A stakeout seat is a chair with a LINE to the way in — and it composes with no second oracle,
    /// which is the audit the ask called for: the chair is <c>HavenInterior.BesideATop</c>'s own sounding
    /// against the room's stone, and the line is <c>SurfaceCollision.HasLineOfSight</c>, the one the tail, the
    /// eye and the man-takes-a-post beat all already ask. Nothing new was taught to see.</para>
    ///
    /// <para><b>Revert that reddens it:</b> the tables pushed north and south into the ends of the hat, behind
    /// the back wall's stubs — the throat drops out of line from both chairs.</para>
    /// </summary>
    [Fact]
    public void EveryTableInTheGallerySeesTheThroat()
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = Deck.CollisionField;
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Berth)!.Value;
        IReadOnlyList<DeckReachability.Point> tops = HavenInterior.GalleryTops(Berth);

        Assert.Equal(2, tops.Count);
        foreach (DeckReachability.Point top in tops)
        {
            DeckReachability.Point chair =
                HavenInterior.BesideATop(top, DeckPlan.AvatarRadius, walls)
                ?? throw new Xunit.Sdk.XunitException($"no chair at the table at {top.X:0.0},{top.Y:0.0}.");

            Assert.True(
                SurfaceCollision.HasLineOfSight(chair.X, chair.Y, throat.X, throat.Y, walls),
                $"the chair at {chair.X:0.0},{chair.Y:0.0} cannot see the way in.");
        }
    }

    // ── THE ROOM'S OWN FURNITURE ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>EVERY FIXTURE IS INSIDE THE ROOM, OFF THE RAIL, AND REACHABLE</b> — and the one exception
    /// is the binoculars, which are the only thing allowed at the glass.
    ///
    /// <para><b>Revert that reddens it:</b> a vending machine moved out to the rail band — the off-the-rail
    /// assert names it.</para>
    /// </summary>
    [Fact]
    public void TheCafeteriasFixturesStandInsideTheGalleryAndOffTheRail()
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = Deck.CollisionField;
        (double x, double y, _) = HavenInterior.BarThreshold;

        var offTheRail = new List<DeckReachability.Point>(HavenInterior.TheVendorsAt(Berth));
        offTheRail.AddRange(HavenInterior.GalleryTops(Berth));
        Assert.Equal(4, offTheRail.Count);

        foreach (DeckReachability.Point spot in offTheRail)
        {
            Assert.True(HavenInterior.InTheGallery(Berth, spot.X, spot.Y),
                $"a fixture at {spot.X:0.0},{spot.Y:0.0} is outside the gallery.");
            Assert.True(HavenInterior.InTheCafeteriaBand(Berth, spot.X, spot.Y),
                $"a fixture at {spot.X:0.0},{spot.Y:0.0} is out on the rail line.");
        }

        // The binoculars ARE on the rail, and they are the only thing that is.
        DeckReachability.Point glasses = HavenInterior.TheBinocularsAt(Berth)!.Value;
        Assert.True(HavenInterior.InTheGallery(Berth, glasses.X, glasses.Y));
        Assert.False(HavenInterior.InTheCafeteriaBand(Berth, glasses.X, glasses.Y));

        // …and every one of the five is somewhere a body can get to and stand.
        offTheRail.Add(glasses);
        foreach (DeckReachability.Point spot in offTheRail)
        {
            Assert.True(
                DeckReachability.CanReach(
                    new DeckReachability.Point(x, y), spot, walls, DeckPlan.AvatarRadius, Bounds),
                $"nothing can walk up to the fixture at {spot.X:0.0},{spot.Y:0.0}.");
        }
    }

    /// <summary>#1199 · …AND THE CONSOLES ARE REALLY ON THE DECK, one press each, with the fixture's own
    /// plate on them. Asked of the built plan, so a fixture measured here and hung somewhere else would show
    /// up as a missing press rather than as a passing test.</summary>
    [Fact]
    public void TheDeckCarriesTheTwoCoinMachinesAndTwoTakeableTops()
    {
        DeckPlan deck = Deck;

        DeckPlan.ConsoleSpot[] glasses =
            deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.CoinBinoculars).ToArray();
        Assert.Single(glasses);
        Assert.Equal(GalleryFixtures.BinocularsPlate, glasses[0].Label);

        DeckPlan.ConsoleSpot[] vendors =
            deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.CoinVendor).ToArray();
        Assert.Equal(2, vendors.Length);
        Assert.All(vendors, v => Assert.Equal(GalleryFixtures.VendorPlate, v.Label));

        // A top the captain can take at each table, in the room's own label.
        foreach (DeckReachability.Point top in HavenInterior.GalleryTops(Berth))
        {
            Assert.Contains(deck.Consoles, c =>
                c.Kind == DeckPlan.ConsoleKind.BarTop
                && Math.Abs(c.X - top.X) < 0.5 && Math.Abs(c.Y - top.Y) < 0.5);
            Assert.Contains(deck.Tables, t =>
                Math.Abs(t.X - top.X) < 0.5 && Math.Abs(t.Y - top.Y) < 0.5);
        }

        // …and the machines are drawn as blocks, so the walked room and the drawn room are one room.
        Assert.Equal(2, HavenInterior.TheVendingMachineBlocks(Berth).Count);
        foreach ((double x0, double y0, double x1, double y1) in HavenInterior.TheVendingMachineBlocks(Berth))
        {
            Assert.Contains(deck.Furniture, f =>
                Math.Abs(f.X0 - x0) < 0.01 && Math.Abs(f.Y0 - y0) < 0.01
                && Math.Abs(f.X1 - x1) < 0.01 && Math.Abs(f.Y1 - y1) < 0.01);
        }
    }

    /// <summary>#1199 · THE GALLERY'S BACKDROP IS THE CAFETERIA PLATE, and the tube keeps the walk's own
    /// drop. Two canvases over two rectangles, because the two halves of the T are two shapes and one plate
    /// stretched across both would be the same picture at two visibly different stretches.</summary>
    [Fact]
    public void TheGalleryWearsTheCafeteriaPlateAndTheTubeKeepsTheDrop()
    {
        DeckPlan deck = Deck;
        (double gx0, double gy0, double gx1, double gy1) = HavenInterior.TheGalleryBox(Berth)!.Value;

        DeckPlan.Backdrop cafe = Assert.Single(deck.Backdrops, b => b.Url == GalleryFixtures.CafeteriaArtUrl);
        Assert.Equal(gx0, cafe.X, 3);
        Assert.Equal(gx1 - gx0, cafe.W, 3);
        Assert.Equal(gy1 - gy0, cafe.H, 3);

        Assert.Contains(deck.Backdrops, b => b.Url == ObservationWalk.ArtUrl);
    }

    // ── AND NOTHING ELSE MOVED ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>NO OTHER BERTH GREW ANYTHING.</b> The whole T, its cafeteria, its two coin machines and its
    /// two tops exist at Selene Gate and nowhere else — swept over every haven the game builds rather than
    /// over a list somebody wrote down, so a ninth berth added next month cannot quietly inherit a gallery.
    /// </summary>
    [Fact]
    public void NoOtherHavenGrewAGallery()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            bool here = body == Berth;
            Assert.Equal(here, HavenInterior.TheGalleryBox(body) is not null);
            Assert.Equal(here, HavenInterior.TheThroatAt(body) is not null);
            Assert.Equal(here ? 2 : 0, HavenInterior.GalleryTops(body).Count);
            Assert.Equal(here ? 2 : 0, HavenInterior.TheVendorsAt(body).Count);
            Assert.Equal(here ? 2 : 0, HavenInterior.TheVendingMachineBlocks(body).Count);

            DeckPlan deck = HavenInterior.DockedDeck(body)!;
            Assert.Equal(here ? 1 : 0,
                deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.CoinBinoculars));
            Assert.Equal(here ? 2 : 0,
                deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.CoinVendor));
            Assert.Equal(here ? 1 : 0,
                deck.Backdrops.Count(b => b.Url == GalleryFixtures.CafeteriaArtUrl));
        }
    }
}
