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
[SlowGate] // #251 · 20 s over 3 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
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
    /// EVERY ROOM BEHIND THE PARK CAN BE WALKED TO FROM THE CAR — BY EITHER OF ITS TWO DOORS, AND ONLY BY
    /// THEM. Plug one and the room is still a room; plug both and it goes dark.
    ///
    /// <para>#813 · THE ROOM GREW A SECOND DOOR AND THE GUARD GREW A SECOND HALF. #801 carved this row with
    /// exactly one way in — a gap in the park's far wall — and said so in the record's own doc comment:
    /// <i>"It is the room's ONLY door; the band behind the park is the last of the field, and there is no
    /// corridor back there for a second one."</i> The Manhattan ruling puts a corridor back there. The back
    /// street IS the block's far side now, so each of these rooms has a street door as well as its old door
    /// onto the gravel, and the owner's law for the whole block — <i>"nobody walks through an office to
    /// reach an office"</i> — is exactly the claim that both of them work.</para>
    ///
    /// <para>So the sealing experiment is run three times per park instead of once, and the two new runs are
    /// the ones that carry the ruling: <b>street door plugged, the room is still reached off the gravel</b>
    /// (#801's promise, unharmed); <b>gravel door plugged, the room is still reached off the back street</b>
    /// (#813's); <b>both plugged, dark</b> (the wall). A guard that kept only the third would go green on a
    /// room walled off from its own street, which is the shape the ruling exists to forbid.</para>
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
    ///
    /// <para><b>Proven RED, third direction (#813)</b>: this guard as it stood, run against the Manhattan
    /// carve, plugged only the gravel doors and reported that they were not doors at all —</para>
    /// <code>
    /// 48 room(s) behind the green are drawn and cannot be entered:
    ///   luna B1: 📋 GROUNDS OFFICE · ROTA POSTED is STILL reachable with its door poured shut — the far
    ///   wall is a picture, not a wall.
    /// </code>
    /// <para>— which was a true measurement of a perfectly correct building, and is how a sealing
    /// experiment starts lying: not by missing a bug, but by counting a door it forgot as a hole.</para>
    /// </summary>
    [Fact]
    public void EveryRoomBehindTheGreenIsEnteredThroughItsOwnDoorAndOnlyThroughIt()
    {
        var wrong = new List<string>();
        int parks = 0, rooms = 0, twoDoored = 0;

        foreach ((string body, int level, UndergroundComplex.Park park, DeckPlan deck) in EveryPark())
        {
            parks++;
            Assert.True(park.Rooms.Count > 0, $"{body} B{-level}: the park has nothing behind it.");

            var from = new DeckReachability.Point(
                HiveInterior.SpawnOn(Field).X, HiveInterior.SpawnOn(Field).Y);
            Assert.True(
                DeckReachability.Standable(from.X, from.Y, DeckPlan.AvatarRadius, deck.CollisionField),
                $"{body} B{-level}: the car's own doorstep is not standable.");

            // ── THE TWO DOORS, OFF THE RING. Park.Rooms is #801's own view of these rooms and carries the
            //    gravel door alone; Park.Frontage is the same rooms as ring fabric and carries both. They
            //    are ONE set of rooms carved once — Core says so where it builds them — so this PAIRS them
            //    by box rather than re-deriving either. A second derivation of the second door would be the
            //    mirrored-constant bug with a record's clothes on.
            //
            //    #817 · …and "the street door" is a LIST now. The owner overrode the ring's one-door record
            //    from inside a landscape office ("bigger spaces must have much more doors"), so a 48 du room
            //    on the back street carries three leaves, and a sealing experiment that plugged the first of
            //    them would report a room with two open doors as a sealed box.
            var back =
                new List<(UndergroundComplex.BackRoom Room, IReadOnlyList<SurfaceLayout.Doorway> Street)>();
            foreach (UndergroundComplex.BackRoom br in park.Rooms)
            {
                UndergroundComplex.BackRoom room = br;
                UndergroundComplex.RingRoom ring = Assert.Single(
                    park.Frontage,
                    r => Math.Abs(r.X - room.X) < 0.01 && Math.Abs(r.Y - room.Y) < 0.01);
                Assert.True(ring.Side == UndergroundComplex.RingSide.Far,
                    $"{body} B{-level}: {br.Plate} is published behind the green and on the {ring.Side} "
                    + "side of the ring — the two lists disagree about one room.");
                back.Add((br, ring.Doors));
            }

            // ── THE HONEST WORLD FIRST.
            foreach ((UndergroundComplex.BackRoom br, _) in back)
            {
                rooms++;
                if (!CanWalk(deck, from, br.X, br.Y))
                {
                    wrong.Add($"  {body} B{-level}: {br.Plate} at ({br.X:F0}, {br.Y:F0}) — the door is a "
                        + "picture.");
                }
            }

            // ── AND THE SEALED ONES. A plug is grown ACROSS its own opening — the #775 lesson about
            //    plugging a front door with a wall that lies along it instead of across it — and it is
            //    built three ways: the gravel doors alone, the street doors alone, and both.
            UndergroundComplex.FloorPlan drawn = UndergroundComplex.Build(body, level, Field);
            DeckPlan Seal(IEnumerable<SurfaceLayout.Doorway> doors)
            {
                var plugged = new List<DeckPlan.Wall>(deck.Walls);
                foreach (SurfaceLayout.Doorway d in doors)
                {
                    Assert.Contains(d, drawn.Doorways);
                    bool horizontal = Math.Abs(d.Y1 - d.Y2) < 0.001;
                    float px = horizontal ? 0f : 0.5f, py = horizontal ? 0.5f : 0f;
                    plugged.Add(new DeckPlan.Wall(
                        (float)d.X1 - px, (float)d.Y1 - py,
                        (float)d.X2 + px, (float)d.Y2 + py, false, true));
                }
                return new DeckPlan(
                    [.. plugged], deck.Consoles, deck.RoomLabels, deck.Backdrops,
                    deck.SpawnX, deck.SpawnY, 0, (_, _) => { }, (_, _) => "sealed");
            }

            DeckPlan gravelShut = Seal(back.Select(b => b.Room.Door));
            DeckPlan streetShut = Seal(back.SelectMany(b => b.Street));
            DeckPlan bothShut = Seal(back.SelectMany(b => b.Street.Append(b.Room.Door)));

            foreach ((UndergroundComplex.BackRoom br, IReadOnlyList<SurfaceLayout.Doorway> street) in back)
            {
                twoDoored++;

                // #813 · The street is a way in. EVERY gravel door on the floor is shut here, so a route
                // that arrives came off the back street and nowhere else.
                if (!CanWalk(gravelShut, from, br.X, br.Y))
                {
                    wrong.Add($"  {body} B{-level}: {br.Plate} cannot be reached with only its door onto "
                        + "the gravel plugged — the street door the Manhattan ruling requires is not a "
                        + "door.");
                }

                // #801 · …and the gravel still is. Every street door is shut, so a route that arrives here
                // walked across the garden, which is what made this row worth carving in the first place.
                if (!CanWalk(streetShut, from, br.X, br.Y))
                {
                    wrong.Add($"  {body} B{-level}: {br.Plate} cannot be reached with only its "
                        + $"{street.Count} street door(s) — the first at "
                        + $"({(street[0].X1 + street[0].X2) / 2:F0}, "
                        + $"{(street[0].Y1 + street[0].Y2) / 2:F0}) — plugged. Walking across the green "
                        + "stopped being a way in.");
                }

                // …and with BOTH shut it is a box. Anything still reachable is crossing a wall.
                if (CanWalk(bothShut, from, br.X, br.Y))
                {
                    wrong.Add($"  {body} B{-level}: {br.Plate} is STILL reachable with BOTH its doors "
                        + "poured shut — a wall of that room is a picture, not a wall.");
                }
            }

            // …and the sealing measured the DOORS and not the floor: the green itself is untouched by even
            // the worst of the three plugs.
            Assert.True(CanWalk(bothShut, from, park.X, park.Y),
                $"{body} B{-level}: plugging the back doors took the park with it — the plug is in the "
                + "wrong place and this experiment proves nothing.");
        }

        Assert.True(parks >= 10, $"only {parks} parks walked — this proved little.");
        Assert.True(rooms >= 40, $"only {rooms} back rooms walked — this proved little.");
        Assert.True(twoDoored >= 40,
            $"only {twoDoored} back rooms were sealed one door at a time — the half of this guard that "
            + "carries the Manhattan ruling never ran.");
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
