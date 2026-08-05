using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #585 · IS IT ACTUALLY WALKABLE? Owner, asking for a smoke test: <i>"feel free also to smoke test it in the
/// browser. Like can the basement be seen there. Is it navigable from display. Is there something
/// obstructed."</i>
///
/// <para>Two of those three questions do not need eyes, and answering them here is <b>stronger</b> than a
/// glance at one screenshot: a look tells you about the floor you happened to open, and a flood tells you
/// about every floor of every clandestine site in the system. (The third — can it be SEEN — still wants a
/// person, and an MCP-driven browser tab cannot answer it: such a tab is <c>document.hidden</c>, so rAF is
/// throttled and the game's boot does not complete.)</para>
///
/// <para>This walks the REAL deck the captain's boots collide with — <see cref="HiveInterior.FloorDeck"/> —
/// and asks the only question that matters about a facility built out of corridors: <b>can you get from the
/// lift to everything the floor is offering you, and back?</b></para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class YouCanWalkTheHiveTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    /// <summary>Every floor of every site, reported as one table rather than one failure at a time.</summary>
    private static void AuditEveryFloor(Func<string, int, DeckPlan, string?> check, string what)
    {
        var bad = new List<string>();
        foreach (string body in Bodies)
        {
            // #592 · FloorsOf, not "−1 down to the depth". A site with an unlisted band has a GAP in the
            // middle where nothing was dug, so counting from a depth would flood four floors that do not
            // exist and skip the four that do — an audit checking a topology nobody ships.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                string? complaint = check(body, level, DeckFor(body, level));
                if (complaint is not null)
                {
                    bad.Add($"  {body} B{-level}: {complaint}");
                }
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} floor(s) fail: {what}");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    [Fact]
    public void EveryRoomOnEveryFloorCanBeWalkedToFromTheLift()
    {
        // THE question about a building made of corridors. A room that exists on screen and cannot be reached
        // is the same bug class as a beacon over nothing — the map showing you something the ground will not
        // honour — and in a facility of twenty floors it would be invisible to reasoning forever.
        //
        // #587 · ABSOLUTE AGAIN. This shipped for a day as "the lift is a law, the rooms are a ratio", because
        // the generator was sealing a handful of rooms on 35 floors and nobody could say why, and a guard that
        // ships red is a guard everybody learns to scroll past. The cause is found and fixed (an unsorted
        // cursor in UndergroundComplex.SpineFace), so the ratio is gone: ONE sealed room anywhere in any
        // clandestine site on any body is a failure, and the message names the coordinates.
        AuditEveryFloor((body, level, deck) =>
        {
            (double sx, double sy) = HiveInterior.SpawnOn(Field);
            var spawn = new DeckReachability.Point(sx, sy);
            var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

            // #707 · AND THE AMENITIES, in the same flood rather than in a guard of their own. A canteen is
            // a room off a rib exactly like every other room down here, so the law that covers it is 13.1
            // and the audit that covers it is this one — the only thing that had to change is that the list
            // of targets is a list of PLACES THE FLOOR OFFERS YOU, not a list of one console kind.
            var targets = deck.Consoles
                .Where(c => c.Kind is DeckPlan.ConsoleKind.HiveHaul or DeckPlan.ConsoleKind.HiveLift
                    or DeckPlan.ConsoleKind.HiveAmenity)
                .Select(c => new DeckReachability.Point(c.X, c.Y))
                .ToList();

            if (targets.Count == 0)
            {
                return "nothing on this floor to walk to at all.";
            }

            var stranded = new List<string>();
            foreach (DeckReachability.Point t in targets)
            {
                if (!DeckReachability.CanReach(spawn, t, deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                {
                    stranded.Add($"({t.X:F0}, {t.Y:F0})");
                }
            }

            return stranded.Count == 0 ? null
                : $"{stranded.Count} of {targets.Count} places cannot be reached from the lift: "
                    + string.Join(", ", stranded.Take(4));
        }, "spec — every room is walkable from the lift");
    }

    [Fact]
    public void TheCaptainCanSTANDWhereTheLiftPutsThem()
    {
        // The most embarrassing possible failure: step out of the car into a wall. It would strand a captain
        // inside a building with no way back up, which on a dead floor is a death.
        AuditEveryFloor((_, _, deck) =>
        {
            (double sx, double sy) = HiveInterior.SpawnOn(Field);
            return DeckReachability.Standable(sx, sy, DeckPlan.AvatarRadius, deck.CollisionField)
                ? null
                : "the lift opens into solid wall.";
        }, "spec — you can stand where the doors open");
    }

    [Fact]
    public void EveryAirlessFloorHasARefugeYouCanWALKToFromTheLift()
    {
        // ── #608 · THE ONE THAT DECIDES WHETHER ANY OF THIS IS REAL ──
        //
        // Owner: "there should be like at least one air replenish station in each of the airless labs
        // underground... for pure safety". Core guarantees one exists on every airless floor
        // (TheRefugesUndergroundTests). This guarantees you can GET TO IT — and those are different claims,
        // which is the whole reason this project owns an A* audit. A refuge you cannot walk to is a refuge
        // that does not exist, and it is worse than none: the card, the plate and the tracker ring would all
        // be promising air that the collision field does not honour, and a captain would spend the last of a
        // tank believing them.
        //
        // A REACHABILITY TEST IS ONLY AS HONEST AS BOTH ITS ENDPOINTS. #600 is the lesson: the A* audit had
        // proved for months that you can REACH the lift and never once that the lift is a way HOME, and the
        // car only went down. So neither end of this walk is taken on trust — the spawn and the refuge are
        // each asserted STANDABLE first. A goal buried in wall would make CanReach return false and read as
        // "the corridors are broken"; a goal that is somehow standable inside a sealed box would pass a
        // sloppier test on a floor nobody could ever cross.
        AuditEveryFloor((_, level, deck) =>
        {
            if (UndergroundComplex.HoldsPressure(level))
            {
                return null;   // the floor is the refuge
            }

            var refuges = deck.Consoles
                .Where(c => c.Kind == DeckPlan.ConsoleKind.HiveRefuge)
                .Select(c => new DeckReachability.Point(c.X, c.Y))
                .ToList();
            if (refuges.Count == 0)
            {
                return "a vacuum floor with no refuge drawn on it at all.";
            }

            (double sx, double sy) = HiveInterior.SpawnOn(Field);
            if (!DeckReachability.Standable(sx, sy, DeckPlan.AvatarRadius, deck.CollisionField))
            {
                return "the walk starts inside a wall — this floor's verdict would mean nothing.";
            }

            var spawn = new DeckReachability.Point(sx, sy);
            var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

            foreach (DeckReachability.Point r in refuges)
            {
                if (!DeckReachability.Standable(r.X, r.Y, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    return $"the refuge at ({r.X:F0}, {r.Y:F0}) is solid wall — nobody could stand in it.";
                }
                if (!DeckReachability.CanReach(spawn, r, deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                {
                    return $"the refuge at ({r.X:F0}, {r.Y:F0}) cannot be walked to from the car.";
                }
            }
            return null;
        }, "spec — every airless floor has air you can actually reach");
    }

    [Fact]
    public void TheRefugeIsAWalkFromTheLiftAndNotAStepFromIt()
    {
        // #608 · "Never on the way. If it sits beside the lift it is decoration; it earns its existence by
        // being somewhere you have to decide to detour to." That is what keeps #585's central call alive —
        // depth is paid for in air, and every stair down is a decision about getting back up. Core places
        // for the detour; this measures the walk on the REAL deck, over the real corridors, because a
        // straight-line distance and a route through a facility are not the same number and only one of them
        // is what the captain pays.
        AuditEveryFloor((_, level, deck) =>
        {
            if (UndergroundComplex.HoldsPressure(level))
            {
                return null;
            }

            (double sx, double sy) = HiveInterior.SpawnOn(Field);
            var spawn = new DeckReachability.Point(sx, sy);
            var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

            foreach (DeckPlan.ConsoleSpot c in deck.Consoles)
            {
                if (c.Kind != DeckPlan.ConsoleKind.HiveRefuge)
                {
                    continue;
                }
                DeckReachability.Walk walk = DeckReachability.Path(
                    spawn, new DeckReachability.Point(c.X, c.Y),
                    deck.CollisionField, DeckPlan.AvatarRadius, bounds);

                double walked = walk.Steps * DeckReachability.DefaultStep;
                if (walked < UndergroundComplex.MinRefugeDetourDu)
                {
                    return $"the refuge is {walked:F0} du of walking from the car — that is decoration.";
                }
            }
            return null;
        }, "spec — reaching the air is a decision to detour");
    }

    [Fact]
    public void APRIVATEWashroomBehindAnOpenDoorIsAWashroomYouCanWALKInto()
    {
        // ── #707 · THE ONE THING THE FLOOD ABOVE CANNOT SEE ──
        //
        // An en-suite has no console in it, deliberately: a private cell carries no plate and there is
        // nothing in it to work, and inventing an [E] just to give the audit a handle would be the test
        // shaping the game. So it is not in 13.1's target list — which means it is EXACTLY the shape of
        // thing this project keeps shipping broken: geometry drawn on a plan that nothing walks.
        //
        // It is a room built out of a doorway cut in another room's back wall, which is a new kind of hole
        // in this generator, and the cell is the only place in the Hive a captain can be asked to pass
        // through two doors to reach. Both ends of the walk are asserted (#600's lesson: a reachability
        // test is only as honest as its endpoints), and only the cells whose PARENT opens are walked —
        // behind a locked plate the cell is a thing you read from the corridor, exactly like the room it
        // belongs to, and demanding a route into it would be demanding the locked door open.
        AuditEveryFloor((body, level, deck) =>
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            (double sx, double sy) = HiveInterior.SpawnOn(Field);
            var spawn = new DeckReachability.Point(sx, sy);
            var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

            foreach (UndergroundComplex.EnSuite cell in floor.EnSuites)
            {
                if (!cell.Open)
                {
                    continue;
                }
                if (!DeckReachability.Standable(cell.X, cell.Y, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    return $"the en-suite off '{cell.Of}' at ({cell.X:F0}, {cell.Y:F0}) is solid wall.";
                }
                if (!DeckReachability.CanReach(
                        spawn, new DeckReachability.Point(cell.X, cell.Y),
                        deck.CollisionField, DeckPlan.AvatarRadius, bounds))
                {
                    return $"the en-suite off '{cell.Of}' cannot be walked to from the lift.";
                }
            }
            return null;
        }, "spec — a private washroom off a room you can enter is a room you can enter");
    }

    [Fact]
    public void NothingIsOBSTRUCTEDByTheDoorsThatWillNeverOpen()
    {
        // The owner's third question, and the one with a real trap in it. A locked door is drawn AND backed by
        // a wall — that is what makes it honest — so if a locked door were ever hung on the only way into a
        // room, the room would still be listed and drawn and would simply be unreachable forever.
        //
        // The reachability flood above already proves it does not happen. This states the intent separately,
        // so a future change that starts locking doorways gets a message about WHY rather than a mystery.
        AuditEveryFloor((body, level, deck) =>
        {
            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);

            foreach (UndergroundComplex.LockedDoor l in floor.Locked)
            {
                double lx = (l.X1 + l.X2) / 2, ly = (l.Y1 + l.Y2) / 2;
                foreach ((double rx, double ry) in floor.RoomCentres)
                {
                    // A locked door sitting on an enterable room's own face would be the contradiction.
                    if (Math.Abs(lx - rx) < 9 && Math.Abs(ly - ry) < 2.5)
                    {
                        return $"a sealed door is hung on the face of the enterable room at ({rx:F0}, {ry:F0}).";
                    }
                }
            }
            return null;
        }, "spec — a locked door never seals a room you are told you can enter");
    }

    [Fact]
    public void ThereIsAlwaysAWayBackToTheLift()
    {
        // Coming back up must never be a search, and on a dead floor it must never be a maze either: the tank
        // is running the whole time you are looking.
        AuditEveryFloor((_, _, deck) =>
        {
            var lift = deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveLift).ToList();
            return lift.Count == 1 ? null : $"{lift.Count} lifts on this floor — there should be exactly one.";
        }, "spec — one lift, always findable");
    }

    [Fact]
    public void ADeepFloorIsAsWalkableAsAShallowOne()
    {
        // "Depth is free" only means anything if floor twenty holds together as well as floor one. Checked at
        // the performance guard itself, which is the worst case the generator can ever be asked for.
        foreach (int level in new[] { -12, -18, UndergroundComplex.DeepestPossibleFloor })
        {
            DeckPlan deck = DeckFor("miranda", level);
            (double sx, double sy) = HiveInterior.SpawnOn(Field);
            var spawn = new DeckReachability.Point(sx, sy);
            var bounds = (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY);

            var rooms = deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveHaul).ToList();
            int reached = rooms.Count(c => DeckReachability.CanReach(
                spawn, new DeckReachability.Point(c.X, c.Y),
                deck.CollisionField, DeckPlan.AvatarRadius, bounds));

            // #587 · ALL of them, not most of them. This was pinned at "more than half" while the sealed-room
            // tail was open; with that closed, "depth is free" gets to mean what it says — floor twenty-four
            // is exactly as walkable as floor one, and any drift shows up here first because this is the
            // worst case the generator can ever be asked for.
            Assert.True(reached == rooms.Count,
                $"B{-level}: only {reached} of {rooms.Count} rooms reachable — deep floors are not free after all.");
        }
    }

    [Fact]
    public void AFloorIsWorthTheLiftRide()
    {
        // The complaint that started all of this, stated as a floor-by-floor law: not a two-door apartment.
        AuditEveryFloor((_, _, deck) =>
        {
            // #608 · THE REFUGE COUNTS AS A ROOM, because it IS one. It is carved out of the floor's own
            // rooms (one poured box off a rib, with its doorway), so counting only HiveHaul would read a
            // safety regulation as a floor being made smaller — and on the tightest floors this law already
            // sits exactly on 4, so it would have gone red for a change that took nothing away. What the law
            // is about is whether stepping out of the car is worth the ride: a room you can breathe in is
            // very much somewhere to go.
            // #707 · An amenity room counts for the identical reason the refuge does: it IS one of the
            // floor's rooms, carved out of the same pool, and counting only HiveHaul would read three
            // rooms being FURNISHED as a floor being made smaller. Somewhere to eat is very much somewhere
            // to go.
            int rooms = deck.Consoles.Count(c =>
                c.Kind is DeckPlan.ConsoleKind.HiveHaul or DeckPlan.ConsoleKind.HiveRefuge
                    or DeckPlan.ConsoleKind.HiveAmenity);
            int sealedDoors = deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.HiveSign);

            if (rooms < 4)
            {
                return $"only {rooms} rooms to search.";
            }
            return sealedDoors < 3 ? $"only {sealedDoors} sealed doors — nothing implies the rest of it." : null;
        }, "spec — a facility, not a flat");
    }
}
