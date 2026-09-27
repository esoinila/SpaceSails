using System;
using System.Collections.Generic;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #251 · WHERE A BODY STANDS AND WHICH DOOR IT USES — the standing place, the door for a table, and the
/// first <c>Departures</c> overload.
///
/// <para>Split out of <c>Egress.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class Egress
{
    /// <summary>
    /// WHERE A BODY STANDS TO USE A DOOR — the walkable spot in front of a leaf, or null if there is not one.
    ///
    /// <para>A door's leaf is stone (a locked door is drawn shut with a wall poured behind it), so nothing
    /// can path ONTO one. What a walk can end on is the floor in front of it, and which side is "in front"
    /// is a fact about the building rather than about the door: both normals are sounded at
    /// <see cref="DoorStandoffDu"/> and the one a body of this radius can stand on wins. Both standable
    /// (a door in the middle of open floor) is answered on the near side, which is the side the room the
    /// walker is in must be on.</para>
    ///
    /// <para>Sounded with <see cref="SurfaceCollision.Blocked"/> — the captain's own predicate, and the same
    /// one the A* lattice asks — so a spot this returns is a spot the walk can reach by construction, and a
    /// null is the honest answer rather than a body parked inside a wall.</para>
    /// </summary>
    /// <param name="door">The leaf, as the floor plan published it.</param>
    /// <param name="radius">The walker's body — the captain's body; one law, one width.</param>
    /// <param name="walls">The floor's own stone.</param>
    /// <param name="nearX">Where the walker is standing, for the tie-break.</param>
    public static DeckReachability.Point? StandingPlaceAt(
        in UndergroundComplex.LockedDoor door,
        double radius,
        IReadOnlyList<SurfaceCollision.Segment> walls,
        double nearX = double.NaN,
        double nearY = double.NaN)
    {
        ArgumentNullException.ThrowIfNull(walls);

        double mx = (door.X1 + door.X2) / 2, my = (door.Y1 + door.Y2) / 2;
        double ax = door.X2 - door.X1, ay = door.Y2 - door.Y1;
        double len = Math.Sqrt((ax * ax) + (ay * ay));
        if (len < 1e-9)
        {
            return null;
        }

        // The two ways off the leaf, perpendicular to it.
        double hx = -ay / len * DoorStandoffDu, hy = ax / len * DoorStandoffDu;
        (double X, double Y) one = (mx + hx, my + hy);
        (double X, double Y) other = (mx - hx, my - hy);
        bool oneFree = !SurfaceCollision.Blocked(one.X, one.Y, radius, walls);
        bool otherFree = !SurfaceCollision.Blocked(other.X, other.Y, radius, walls);

        if (oneFree && otherFree)
        {
            // Open on both hands: the side the walker is already on is the side of it they use.
            if (double.IsNaN(nearX) || double.IsNaN(nearY))
            {
                return new DeckReachability.Point(one.X, one.Y);
            }
            double d1 = ((one.X - nearX) * (one.X - nearX)) + ((one.Y - nearY) * (one.Y - nearY));
            double d2 = ((other.X - nearX) * (other.X - nearX)) + ((other.Y - nearY) * (other.Y - nearY));
            return new DeckReachability.Point(
                d1 <= d2 ? one.X : other.X, d1 <= d2 ? one.Y : other.Y);
        }
        if (oneFree)
        {
            return new DeckReachability.Point(one.X, one.Y);
        }
        return otherFree ? new DeckReachability.Point(other.X, other.Y) : null;
    }

    /// <summary>
    /// WHICH DOOR SOMEBODY USES. Deterministic in (site, floor, watch, who), and drawn only from the
    /// building's locked list — see the class docs for why that is a type and not a flag.
    /// </summary>
    /// <returns>An index into <paramref name="locked"/>, or −1 when the floor has no locked door at all
    /// (which is a true statement about some floors, and never a reason to use a public one).</returns>
    public static int DoorFor(
        string bodyId, int level, long watch, string who, IReadOnlyList<UndergroundComplex.LockedDoor> locked)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(who);
        ArgumentNullException.ThrowIfNull(locked);

        return locked.Count == 0
            ? -1
            : DiceRule.Roll(DiceRule.Seed($"hive:egress:door:{bodyId}:{level}:{who}", watch), locked.Count)
                .Face - 1;
    }

    /// <summary>
    /// WHO FINISHES AND GOES, THIS WATCH — the ambience half of the owner's proposal, and the answer to
    /// <i>"now they have to wait for us to leave before they can sit up."</i>
    ///
    /// <para>One pass over the room's own tops, in the room's own order. A top with somebody at it gets one
    /// seeded roll against <see cref="LeaversPerWatch"/>; the ones that clear it are dealt a moment inside
    /// the first <see cref="LastCallFraction"/> of the shift and a door out of the locked list. Cabinets are
    /// skipped — nobody is in one — and so is any top the room did not seat.</para>
    ///
    /// <para><b>#731 (B1 canteen) · AND THE CROWD IS SKIPPED, because the crowd is DATA</b> — the reading this
    /// overload spends is <see cref="OnTheSchedule"/> and not <see cref="Seated"/>, and #751's law, the
    /// measurement behind it and the reason the two readings have two names are all written on it.</para>
    ///
    /// <para>Returned in the order they LEAVE rather than in table order, because a caller stepping down the
    /// list as the watch runs wants the next one at the front, and sorting it here means nobody sorts it
    /// twice. At most <see cref="MostAtOnce"/> survive the cut, for the reason written on that
    /// constant.</para>
    /// </summary>
    public static IReadOnlyList<Move> Departures(
        string bodyId,
        int level,
        long watch,
        IReadOnlyList<CanteenRegulars.TableSeat> tops,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(tops);
        ArgumentNullException.ThrowIfNull(locked);

        return locked.Count == 0 ? [] : Departures(bodyId, level, watch, OnTheSchedule(tops), locked);
    }
}
