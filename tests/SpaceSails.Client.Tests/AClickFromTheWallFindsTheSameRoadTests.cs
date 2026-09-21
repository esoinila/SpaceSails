using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1290 · <b>A CAPTAIN WITH HIS BACK TO A WALL CAN STILL POINT AT THE FLOOR.</b>
///
/// <para>Owner, four separate one-shot runs of the two-leg night: <i>"walk west along the gallery until the
/// captain fetches up against the rail wall, and click-to-walk stops working entirely. Clicking any square —
/// the table two paces away, the other table, an open stretch of the tube floor behind him — does nothing at
/// all… From the same spot WASD still moves him, and one step off the wall clicks work again."</i></para>
///
/// <para><b>What was wrong.</b> Not the rail, not the fixture, not a missing square. The A* starts from the
/// captain's position ROUNDED to the nearest lattice node (<c>DeckReachability.Lattice.Cell</c>), and a
/// rounding is worth up to half a lattice step in each axis — 0.35 du on the diagonal. A held key walks the
/// captain up to the wall until the very next sub-step is refused, which parks him somewhere between
/// <c>AvatarRadius</c> and <c>AvatarRadius + one sub-step</c> off the stone. The two numbers overlap: the
/// node his own square rounds to is then INSIDE the wall's collision skin, the search reported a caller
/// error ("an unwalkable spawn") and came back with no route at all. His feet were on clear floor the whole
/// time, which is why the keys — which never touch the lattice — kept working.</para>
///
/// <para><b>The law this file states.</b> #875's ruling is that the two grips are one walk (<i>"click to walk
/// should always be on when the arrows for walking are active also"</i>), so PRESSED AGAINST A WALL IS NOT A
/// PLACE WITH NO CLICK ROAD. The guard is a comparison rather than a claim about any one room: whatever a
/// captain can click his way to from one body-width off a wall, he can click his way to from against it. A
/// world where nothing is reachable from either spot proves nothing and is skipped, so this cannot pass by
/// being asked in an empty room.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AClickFromTheWallFindsTheSameRoadTests
{
    // Map.Deck's own numbers: the captain's speed and the frame a browser most often hands out. A held key
    // is spent in exactly these steps, so the spot this file presses him into is the spot play parks him on.
    private const double WalkSpeedDu = 9.0;
    private const double OneFrameS = 1.0 / 60.0;

    /// <summary>How far back off the wall the CONTROL stands — one body-width, which is the owner's own
    /// "one step off the wall" and comfortably more than the rounding that does the damage.</summary>
    private const double OneStepOffDu = 2 * DeckPlan.AvatarRadius;

    /// <summary>Press the captain into whatever is that way, exactly the way a held key does it: sub-steps
    /// of speed × frame through <see cref="DeckPlan.Move"/> — the shipping stepper, called as-is — until the
    /// ground stops giving. Nothing here re-implements collision; if it did the file would be worthless.</summary>
    private static (double X, double Y) PressIntoTheWall(DeckPlan deck, double x, double y, double ux, double uy)
    {
        const double step = WalkSpeedDu * OneFrameS;
        for (int frame = 0; frame < 600; frame++)
        {
            (double nx, double ny) = deck.Move(x, y, ux * step, uy * step);
            if (Math.Abs(nx - x) + Math.Abs(ny - y) <= 1e-9)
            {
                break;   // fetched up: the next sub-step is the one the stone refused
            }
            (x, y) = (nx, ny);
        }
        return (x, y);
    }

    private static AutoWalk.Attempt Click(DeckPlan deck, double fromX, double fromY, DeckReachability.Point to)
    {
        var from = new DeckReachability.Point(fromX, fromY);
        return AutoWalk.Plan(
            enabled: true, from, to, deck.CollisionField, DeckPlan.AvatarRadius,
            AutoWalk.BoundsFor(deck.CollisionSegments, from, to),
            DeckReachability.DefaultStep, AutoWalk.PointingReachDu);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD 1 · THE ROOM THE ISSUE WAS FILED FROM, through the page's own click.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1290 · The reported run, driven end to end on a live page: walk west along the gallery on a HELD KEY
    /// until the rail wall stops him, then point at the table through <c>Map.ClickToWalkAt</c> — the very
    /// method the pointer calls, given the pixel the renderer's own projection puts that table on.
    ///
    /// <para><b>Every station along that rail, and not one.</b> The bug is a ROUNDING, so whether it bites
    /// depends on where along the glass he fetched up — the owner's own tell (<i>"the two frames are
    /// indistinguishable by eye and differ by a few pixels of parallax"</i>). A guard that walked to one
    /// spot would be a coin toss, and it passed on the shipped tree at the first spot tried.</para>
    ///
    /// <para>The control is the other half of the owner's report: from one body-width back, the identical
    /// click plans a route. Every station is calibrated that way, because a guard that only checked the
    /// pressed spot would pass in a gallery where no table can be reached at all.</para>
    /// </summary>
    [Fact]
    public void AgainstTheGalleryRailTheClickStillFindsTheTable()
    {
        Pages.Map map = AshoreAtTheGallery();
        var deck = (DeckPlan)Read(map, "_deckPlan")!;

        IReadOnlyList<DeckReachability.Point> tables = HavenInterior.GalleryTops(Port);
        Assert.True(tables.Count > 0, "the gallery has no tables to click — this guard would prove nothing.");

        DeckReachability.Point rail = HavenInterior.TheRailAt(Port)!.Value;
        (double _, double southY, double _, double northY) = HavenInterior.TheGalleryBox(Port)!.Value;

        var bad = new List<string>();
        int stations = 0;
        for (int i = 1; i < 40; i++)
        {
            double y = southY + ((northY - southY) * i / 40.0);

            // …and at a different FRAME each time. That is the owner's own tell — <i>"the two frames are
            // indistinguishable by eye and differ by a few pixels of parallax… it is the captain's exact
            // distance from the wall that decides it"</i> — and it is the honest world: a browser hands out
            // whatever frame it feels like, the last sub-step before the stone is that frame's own budget
            // long, and so the spot a held key parks him on is anywhere in a sub-step's width of the wall.
            // Pin the frame at a tidy 60 and this whole room lands on ONE x and the guard becomes a coin
            // toss that came up heads.
            double dt = 1.0 / (24 + ((i % 17) * 6));

            // Into the gallery at this station, then WEST on a held key until the rail wall has him. The key
            // is pressed and the frames are spent through the page's own handlers, so what parks him is the
            // shipping walk and not a coordinate this file chose.
            Set(map, "_autoWalk", null);
            Set(map, "_autoWalkDeck", null);
            Set(map, "_avatarX", rail.X);
            Set(map, "_avatarY", y);
            Invoke(map, "HandleDeckKey", "a", false);
            for (int frame = 0; frame < 400; frame++)
            {
                Invoke(map, "MoveAvatar", dt);
            }

            double px = (double)Read(map, "_avatarX")!, py = (double)Read(map, "_avatarY")!;
            if (!deck.Collides(px - DeckPlan.AvatarRadius, py))
            {
                continue;   // nothing to his west at this station: he is not against the glass
            }

            DeckReachability.Point table = tables
                .OrderBy(t => ((t.X - px) * (t.X - px)) + ((t.Y - py) * (t.Y - py)))
                .First();

            // ── THE CONTROL ── one body-width back off the glass, the same click plans a route.
            if (Click(deck, px + OneStepOffDu, py, table).Route is null)
            {
                continue;
            }
            stations++;

            // ── THE LAW ── and so it does from against it. Through the PAGE, pixel and all: the table's own
            // deck units run back through the projection the renderer is drawing with RIGHT NOW, so this is
            // the pixel a finger would have landed on, and ClickToWalkAt is what the pointer calls.
            Set(map, "_autoWalk", null);
            Set(map, "_autoWalkDeck", null);
            DeckView.Placement glass = DeckView.PlacementFor(
                deck, (int)Read(map, "_viewportWidth")!, (int)Read(map, "_viewportHeight")!,
                px, py, (double)Read(map, "_deckPanX")!, (double)Read(map, "_deckPanY")!);
            Invoke(map, "ClickToWalkAt", glass.Ox + (table.X * glass.Scale), glass.Oy - (table.Y * glass.Scale));

            if ((AutoWalk?)Read(map, "_autoWalk") is not { Active: true })
            {
                bad.Add($"  pressed to ({px:0.00}, {py:0.00}): the click on the table at "
                    + $"({table.X:0.0}, {table.Y:0.0}) planned nothing.");
            }
        }

        Assert.True(stations > 20,
            $"only {stations} stations along the rail had a table to lose — the control is doing the work.");
        if (bad.Count > 0)
        {
            Assert.Fail(
                $"{bad.Count} of {stations} stations along the gallery's west rail take the click road away "
                + "— and from every one of them the identical click one body-width back plans a route, and "
                + "WASD still walks him. #875's law is that the two grips are one walk:\n"
                + string.Join("\n", bad.Take(20)));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD 2 · …AND IT IS A LAW, NOT A GALLERY PATCH — every wall of five real rooms.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1290 · Press the captain into EVERY wall of five real floors — the haven concourse (which is the
    /// gallery, the tube and the bar in one plan), the service level under it, a Hive corridor and the
    /// ship's own deck — from both sides, and click somewhere he could plainly have clicked a body-width
    /// back. Every one of those clicks must still find a road.
    /// </summary>
    [Fact]
    public void EveryWallOfEveryRoomStillHasAClickRoadOffIt()
    {
        var bad = new List<string>();
        int pressed = 0, calibrated = 0;

        foreach ((string room, DeckPlan deck, DeckReachability.Point target) in TheRooms())
        {
            foreach ((double x, double y, double ux, double uy) in FacesOf(deck))
            {
                (double wallX, double wallY) = PressIntoTheWall(deck, x, y, ux, uy);
                pressed++;

                // The control: a body-width back along the way he came. If THAT cannot reach the target the
                // pair says nothing about the wall, so the face is skipped rather than counted as a pass.
                if (Click(deck, wallX - (ux * OneStepOffDu), wallY - (uy * OneStepOffDu), target).Route is null)
                {
                    continue;
                }
                calibrated++;

                if (Click(deck, wallX, wallY, target).Route is null)
                {
                    bad.Add($"  {room}: pressed to ({wallX:0.00}, {wallY:0.00}) — the click road to "
                        + $"({target.X:0.0}, {target.Y:0.0}) is gone, and it is there a body-width back.");
                }
            }
        }

        Assert.True(pressed > 400, $"only {pressed} faces pressed — this guard is not looking at a building.");
        Assert.True(calibrated > 200,
            $"only {calibrated} of {pressed} presses had a road to lose — the control is doing the work.");

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"{bad.Count} of {calibrated} walls take the captain's click road away when he stands "
                + "against them (the keys still move him from every one of these squares):");
            foreach (string line in bad.Take(25))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD 3 · …and a click that truly has no road still SAYS so.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1290 · The nudge must not buy silence. A captain jammed against a wall who points at somewhere there
    /// is genuinely no way to gets Core's own refusal — <see cref="AutoWalk.RefusalLine"/>, the shipped
    /// idiom — and never a shrug. A silent control is indistinguishable from a broken one, which is exactly
    /// what four one-shot runs read this bug as.
    /// </summary>
    [Fact]
    public void AClickWithNoRoadAtAllStillRefusesOutLoud()
    {
        DeckPlan deck = HavenInterior.DockedDeck(Port)!;
        DeckReachability.Point rail = HavenInterior.TheRailAt(Port)!.Value;
        (double x, double y) = PressIntoTheWall(deck, rail.X, rail.Y, -1, 0);

        (double x0, double y0, double x1, double y1) = HavenInterior.TheGalleryBox(Port)!.Value;
        var outside = new DeckReachability.Point(x0 - 120, y0 - 120);
        _ = (x1, y1);

        AutoWalk.Attempt attempt = Click(deck, x, y, outside);
        Assert.Null(attempt.Route);
        Assert.Equal(AutoWalk.RefusalLine, attempt.Refusal);
    }

    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The five floors, each with one place on it the game invites the captain to walk to.</summary>
    private static IEnumerable<(string Room, DeckPlan Deck, DeckReachability.Point Target)> TheRooms()
    {
        DeckPlan concourse = HavenInterior.DockedDeck(Port)!;
        yield return ("selene-gate concourse (bar, tube, gallery)", concourse,
            HavenInterior.GalleryTops(Port)[0]);

        DeckPlan lower = HavenInterior.DockedDeck(Port, level: HavenLevels.ServiceLevel)!;
        yield return ("selene-gate service level (the ring)", lower, AnAnchorOn(lower));

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        DeckPlan hive = HiveInterior.FloorDeck("miranda", -1, field, 0, (_, _) => { }, []);
        (double sx, double sy) = HiveInterior.SpawnOn(field);
        yield return ("miranda B1 (a Hive corridor)", hive, new DeckReachability.Point(sx, sy));

        DeckPlan hive3 = HiveInterior.FloorDeck("europa", -3, field, 0, (_, _) => { }, []);
        yield return ("europa B3 (a Hive corridor)", hive3, new DeckReachability.Point(sx, sy));

        yield return ("the ship's own deck", DeckPlan.Ship, AnAnchorOn(DeckPlan.Ship));
    }

    /// <summary>Somewhere on this floor the game already promises is walkable: its first console spot, which
    /// is a fixture the deck itself planted rather than a coordinate this file guessed at.</summary>
    private static DeckReachability.Point AnAnchorOn(DeckPlan deck)
    {
        Assert.True(deck.Consoles.Length > 0, "a floor with nothing on it cannot calibrate a click.");
        DeckPlan.ConsoleSpot spot = deck.Consoles[0];
        return new DeckReachability.Point(spot.X, spot.Y);
    }

    /// <summary>Every FACE of every wall on a floor, as a place to stand and a way to press: a point out
    /// along the segment's own normal, both hands, at three stations down its length. Read off the deck's
    /// own collision segments, so a floor that grows a wall grows a case here without anybody typing one.</summary>
    private static IEnumerable<(double X, double Y, double Ux, double Uy)> FacesOf(DeckPlan deck)
    {
        foreach (SurfaceCollision.Segment w in deck.CollisionSegments)
        {
            double dx = w.X2 - w.X1, dy = w.Y2 - w.Y1;
            double len = Math.Sqrt((dx * dx) + (dy * dy));
            if (len < 2 * DeckPlan.AvatarRadius)
            {
                continue;   // a stub shorter than a body: there is no face to stand against
            }

            double nx = -dy / len, ny = dx / len;   // the wall's own normal
            foreach (double t in new[] { 0.3, 0.5, 0.7 })
            {
                double mx = w.X1 + (dx * t), my = w.Y1 + (dy * t);
                foreach (int hand in new[] { 1, -1 })
                {
                    // Stand a comfortable pace off the face, and press back into it.
                    double standOff = 3 * DeckPlan.AvatarRadius;
                    double sx = mx + (nx * hand * standOff), sy = my + (ny * hand * standOff);
                    if (deck.Collides(sx, sy))
                    {
                        continue;   // that side of this wall is solid — nowhere to stand, nothing to press
                    }
                    yield return (sx, sy, -nx * hand, -ny * hand);
                }
            }
        }
    }

    /// <summary>A live page, clamped on at the one berth with an observation walk, with the captain ashore
    /// on its concourse — the shipping road onto that floor, never a deck handed to a page by a test.</summary>
    private static Pages.Map AshoreAtTheGallery()
    {
        Pages.Map map = Boot("a-click-from-the-wall");
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
        Assert.True(HavenInterior.HasObservationWalk(Port), $"{Port} has no gallery to be pressed against.");
        return map;
    }
}
