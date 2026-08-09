using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #801 · THE FAR GATES LEAD SOMEWHERE, AND THE SECOND CAR IS A WAY HOME — both halves proved on the deck
/// the boots actually collide with.
///
/// <para>Two features, one guard file, because they are the same bug class and this ground has paid for it
/// twice already: <b>a thing that is drawn and cannot be walked to</b>. #587 drew a facility and shipped a
/// sealed tube; #600 proved you could REACH the lift and never that it was a way HOME. A row of rooms
/// behind a garden and a car at the blind end of a corridor are exactly the two shapes that fail that way
/// while every Core test stays green.</para>
///
/// <para>So: the flood starts at the car, over <c>DeckPlan.CollisionField</c>, at
/// <c>DeckPlan.AvatarRadius</c> — the same predicate live movement uses — and then the doors are POURED
/// SHUT and the same flood is demanded to go dark. A sealing experiment is the only way to tell a door from
/// a hole in a wall nobody built.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFarGatesLeadSomewhereTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static (double, double, double, double) Bounds =>
        (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    private static bool CanWalk(DeckPlan deck, DeckReachability.Point from, double x, double y) =>
        DeckReachability.CanReach(
            from, new DeckReachability.Point(x, y), deck.CollisionField, DeckPlan.AvatarRadius, Bounds);

    private static IEnumerable<(string Body, int Level, UndergroundComplex.Park Park, DeckPlan Deck)>
        EveryPark()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.Build(body, level, Field).Park is { } park)
                {
                    yield return (body, level, park, DeckFor(body, level));
                }
            }
        }
    }

    // ── (a) THE ROOMS BEYOND THE GREEN ────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY ROOM BEHIND THE PARK CAN BE WALKED TO FROM THE CAR, AND EVERY ONE OF THEM GOES DARK WHEN ITS
    /// DOOR IS POURED SHUT.
    ///
    /// <para><b>Proven RED, first direction</b>, with the far wall left poured as one segment and the rooms
    /// carved behind it (the drawn-versus-walked bug, exactly):</para>
    /// <code>
    /// 48 room(s) behind the green are drawn and cannot be entered:
    ///   luna B1: 📋 GROUNDS OFFICE · ROTA POSTED at (-85, -270) — the door is a picture.
    ///   luna B1: 🌱 POTTING · SOIL, TRAYS, GRIT at (-32, -270) — the door is a picture.
    ///   …
    /// </code>
    ///
    /// <para><b>Proven RED, second direction</b> — the one that stops the guard passing on a park with no
    /// far wall at all — by deleting the far wall entirely: <c>48 room(s) are still reachable with every
    /// back door poured shut; the far wall is a picture, not a wall.</c></para>
    /// </summary>
    [Fact]
    public void EveryRoomBehindTheGreenIsEnteredThroughItsOwnDoorAndOnlyThroughIt()
    {
        var wrong = new List<string>();
        int parks = 0, rooms = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, DeckPlan deck) in EveryPark())
        {
            parks++;
            Assert.True(park.Rooms.Count > 0, $"{body} B{-level}: the park has nothing behind it.");

            var from = new DeckReachability.Point(
                HiveInterior.SpawnOn(Field).X, HiveInterior.SpawnOn(Field).Y);
            Assert.True(
                DeckReachability.Standable(from.X, from.Y, DeckPlan.AvatarRadius, deck.CollisionField),
                $"{body} B{-level}: the car's own doorstep is not standable.");

            // ── THE HONEST WORLD FIRST.
            foreach (UndergroundComplex.BackRoom br in park.Rooms)
            {
                rooms++;
                if (!CanWalk(deck, from, br.X, br.Y))
                {
                    wrong.Add($"  {body} B{-level}: {br.Plate} at ({br.X:F0}, {br.Y:F0}) — the door is a "
                        + "picture.");
                }
            }

            // ── AND THE SEALED ONE. Every back door plugged, grown across its own axis (the doors are
            //    horizontal spans, so the plug is padded in y) — the #775 lesson about plugging a front
            //    door with a wall that lies along it instead of across it.
            var plugged = deck.Walls.ToList();
            foreach (UndergroundComplex.BackRoom br in park.Rooms)
            {
                plugged.Add(new DeckPlan.Wall(
                    (float)br.Door.X1, (float)(br.Door.Y1 - 0.5),
                    (float)br.Door.X2, (float)(br.Door.Y2 + 0.5)));
            }
            var sealedDeck = new DeckPlan(
                [.. plugged], deck.Consoles, from.X, from.Y, (_, _) => "sealed");

            foreach (UndergroundComplex.BackRoom br in park.Rooms)
            {
                if (CanWalk(sealedDeck, from, br.X, br.Y))
                {
                    wrong.Add($"  {body} B{-level}: {br.Plate} is STILL reachable with its door poured "
                        + "shut — the far wall is a picture, not a wall.");
                }
            }

            // …and the sealing measured the DOOR and not the floor: the green itself is untouched.
            Assert.True(CanWalk(sealedDeck, from, park.X, park.Y),
                $"{body} B{-level}: plugging the back doors took the park with it — the plug is in the "
                + "wrong place and this experiment proves nothing.");
        }

        Assert.True(parks >= 10, $"only {parks} parks walked — this proved little.");
        Assert.True(rooms >= 40, $"only {rooms} back rooms walked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} room(s) behind the green are drawn and cannot be entered:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>THE SEARCH CONSOLE FOR A BACK ROOM STANDS IN THE ROOM AND NOT IN THE PARK. #759's own law
    /// — nothing in the green offers a verb — said about the rooms this PR put behind it.</summary>
    [Fact]
    public void NothingTheBackOfHouseOffersStandsOnTheGravel()
    {
        int consoles = 0;
        foreach ((string body, int level, UndergroundComplex.Park park, DeckPlan deck) in EveryPark())
        {
            foreach (UndergroundComplex.BackRoom br in park.Rooms)
            {
                DeckPlan.ConsoleSpot spot = Assert.Single(
                    deck.Consoles,
                    c => c.Kind == DeckPlan.ConsoleKind.HiveHaul
                        && br.Contains(c.X, c.Y));
                Assert.False(park.Contains(spot.X, spot.Y),
                    $"{body} B{-level}: the back room's search console is standing on the gravel.");
                consoles++;
            }
        }
        Assert.True(consoles >= 40, $"only {consoles} consoles checked — this proved little.");
    }

    // ── (b) THE SECOND CAR ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// BOTH CARS ARE ON EVERY FLOOR, BOTH ARE WALKABLE TO FROM THE OTHER, AND A CAPTAIN SET DOWN BY EITHER
    /// CAN STAND WHERE IT PUT THEM.
    ///
    /// <para>§13.3 is the hardest law down here and it was written about one car. <b>Proven RED</b> by
    /// having <c>RideTheLiftTo</c>'s placement keep its old constant — i.e. spawning at the cage's doorstep
    /// after riding the goods car — no: that one is proved by the arithmetic below, which is the point.
    /// The sealing proof for this half is <c>HiveInterior.SpawnOn(field, Service)</c> returning the cage's
    /// spot: <c>132 floor(s) put a captain who rode the goods car 170 du from the car they rode.</c></para>
    /// </summary>
    [Fact]
    public void BothCarsAreOnEveryFloorAndEachIsAWalkFromTheOther()
    {
        IReadOnlyList<UndergroundComplex.Shaft> cars = UndergroundComplex.ShaftsOn(Field);
        Assert.True(cars.Count >= 2, "the shipped field takes only one car — this guard is about nothing.");

        var wrong = new List<string>();
        int floors = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                DeckPlan deck = DeckFor(body, level);

                // One console per car, of the kind that car's [E] press dispatches on.
                DeckPlan.ConsoleSpot cage = Assert.Single(
                    deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveLift);
                DeckPlan.ConsoleSpot goods = Assert.Single(
                    deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.HiveServiceLift);

                foreach (UndergroundComplex.Shaft car in cars)
                {
                    (double sx, double sy) = HiveInterior.SpawnOn(Field, car.Kind);

                    // The doorstep is THIS car's, not the other one's.
                    if (Math.Abs(sx - car.X) > 0.001)
                    {
                        wrong.Add($"  {body} B{-level}: riding the {car.Kind} car puts a captain "
                            + $"{Math.Abs(sx - car.X):F0} du from the car they rode.");
                        continue;
                    }
                    if (!DeckReachability.Standable(sx, sy, DeckPlan.AvatarRadius, deck.CollisionField))
                    {
                        wrong.Add($"  {body} B{-level}: the {car.Kind} car's doors open into wall.");
                        continue;
                    }

                    // …and from it you can reach the OTHER car. That is the whole anti-choke claim: a guard
                    // on one of them has not sealed the floor.
                    var from = new DeckReachability.Point(sx, sy);
                    DeckPlan.ConsoleSpot other =
                        car.Kind == UndergroundComplex.ShaftKind.Cage ? goods : cage;
                    if (!CanWalk(deck, from, other.X, other.Y))
                    {
                        wrong.Add($"  {body} B{-level}: from the {car.Kind} car there is no walk to the "
                            + "other one.");
                    }
                }
            }
        }

        Assert.True(floors > 100, $"only {floors} floors walked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) fail the two-car law:\n" + string.Join("\n", wrong.Take(20)));
    }
}
