using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Labs.Lab44;

/// <summary>
/// #589 · What one floor of the Hive is, and how much of it the captain can actually get to.
///
/// <para>The verdict and the picture come from the SAME object — this one — so a floor that prints
/// <c>9/11 rooms</c> in the table is a floor whose picture has two crosses in it. There is no second
/// pathfinder, no second collision field, and no second idea of where the rooms are.</para>
///
/// <para>It walks the CLIENT's deck on purpose. #587 was a difference between the walls Core draws and the
/// collision field the boots hit; a report built from Core's geometry alone would have called that floor
/// perfect.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal sealed class FloorReport
{
    internal double Step { get; }

    /// <summary>Everywhere the captain can stand, flooded from where the lift doors open.</summary>
    internal IReadOnlyCollection<DeckReachability.Point> Wash { get; }

    /// <summary>One verdict per entry in <c>FloorPlan.RoomCentres</c>, in that order.</summary>
    internal IReadOnlyList<bool> RoomReached { get; }

    internal bool LiftReached { get; }

    internal int Rooms => RoomReached.Count;

    internal int RoomsReached { get; }

    internal bool AllReached => LiftReached && RoomsReached == Rooms;

    private FloorReport(
        double step, IReadOnlyCollection<DeckReachability.Point> wash,
        IReadOnlyList<bool> roomReached, bool liftReached, int roomsReached)
    {
        Step = step;
        Wash = wash;
        RoomReached = roomReached;
        LiftReached = liftReached;
        RoomsReached = roomsReached;
    }

    internal static FloorReport For(
        string body, int level, in SurfaceLayout.Field field, double step = DeckReachability.DefaultStep)
    {
        DeckPlan deck = HiveInterior.FloorDeck(body, level, field, 0, (_, _) => { }, []);
        (double sx, double sy) = HiveInterior.SpawnOn(field);
        var spawn = new DeckReachability.Point(sx, sy);
        var bounds = (field.LeftX, field.BottomY, field.RightX, field.LandingBandY);

        IReadOnlyCollection<DeckReachability.Point> wash =
            DeckReachability.Reachable(spawn, deck.CollisionField, DeckPlan.AvatarRadius, bounds, step);

        // The verdict per target is asked of the WALK, not read off the wash, because the walk is what the
        // CI audit asks (YouCanWalkTheHiveTests) and the two must agree about every room or one of them is
        // lying. They share a lattice, so they do — but the report is only worth trusting if it is the same
        // question, put the same way.
        bool Reached(double x, double y) =>
            DeckReachability.CanReach(spawn, new DeckReachability.Point(x, y),
                deck.CollisionField, DeckPlan.AvatarRadius, bounds, step);

        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, field);
        var roomReached = new List<bool>(floor.RoomCentres.Count);
        int hits = 0;
        foreach ((double rx, double ry) in floor.RoomCentres)
        {
            bool ok = Reached(rx, ry);
            roomReached.Add(ok);
            if (ok)
            {
                hits++;
            }
        }

        // The lift consoles, exactly where HiveInterior puts them — the hardest law down here, and #801
        // made it plural: a floor where one car is reachable and the other is not is a floor with a choke
        // point back in it, and the report has to be able to say so.
        bool lift = true;
        foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(field))
        {
            (double cx, double cy) = car.Landing;
            lift &= Reached(
                cx,
                cy + ((car.Kind == UndergroundComplex.ShaftKind.Cage ? 1 : -1)
                    * (UndergroundComplex.CorridorHalf + 1.5)));
        }

        return new FloorReport(step, wash, roomReached, lift, hits);
    }
}
