using System;
using System.Collections.Generic;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #731 · WHO LEAVES, WHEN, AND THROUGH WHICH DOOR — and the same three answers for who arrives.
///
/// <para><b>Owner, 2026-08-06:</b> <i>"If they go behind a door that is locked to us, we use that as 'I guess
/// that concludes the conversation' point in the plot / situation."</i> And, on the room this is first
/// wired into: <i>"Like on the bar now they have to wait for us to leave before they can sit up… or leave
/// the bar."</i></para>
///
/// <h3>The door is a TYPE, not a flag</h3>
///
/// <para>The load-bearing law on #731 is that the door somebody leaves through is one the captain's own TRY
/// would be refused at — <i>"An NPC exiting through a door and that door refusing the captain ten seconds
/// later is the whole beat, and no line of dialog may explain it."</i> A walker carrying a <c>bool</c> saying
/// so is this repo's fifth named bug class with a door on it: a guard would be asking a field somebody typed
/// in, and it would pass over a world in which every door was public.</para>
///
/// <para>So nothing here accepts a doorway. Every function that picks a door takes
/// <see cref="UndergroundComplex.LockedDoor"/> — the building's own list of the things it hung a plate and a
/// poured wall on — and <b>every member of that list is refused by construction</b>: the captain's offer at
/// one is <c>SatchelTry.AtRoomDoor</c>, which is unconditionally <c>Worked: false</c> with no branch in it.
/// A public exit cannot be passed to these functions at all, because a public exit is not one of these.</para>
///
/// <h3>Scheduled, and therefore frozen</h3>
///
/// <para>The issue asks the question and this is the answer it proposes: <i>scheduled for ambience,
/// triggered for plot beats; both through one walker.</i> Scheduled means a function of the WATCH INDEX —
/// <see cref="PatronRota.WatchIndex"/>, the same four-hour shift the bar upstairs and the canteen downstairs
/// already turn over on — and never of a wall clock. That is the frozen-watch law (#709): the room is drawn
/// at one instant and the walk is stepped at another, and a schedule that read the clock twice would have
/// the figure on screen and the person the game answers about be two different people. A watch chosen once
/// cannot drift into that.</para>
///
/// <para>Pure and deterministic in (site, floor, watch): the same shift produces the same departures in the
/// same order through the same doors, on every machine, forever. Nothing in here allocates per frame — it is
/// asked once when a floor is built.</para>
/// </summary>
public static class Egress
{
    /// <summary>How far in front of a door's leaf a body stands when it walks up to one, in deck units.
    ///
    /// <para>A locked door has a wall poured behind it, so the leaf itself is stone and no route can end on
    /// it. What a walker can reach is the floor in front, and this is how much of it: one body radius
    /// (<c>DeckPlan.AvatarRadius</c> is 0.7, and it is a body's own half-width off any wall) plus a little,
    /// rounded to a number a lattice at <see cref="DeckReachability.DefaultStep"/> can actually land on. Any
    /// less and the standing place is inside the wall the door is cut into; much more and the figure clicks
    /// out of existence while visibly still in the middle of the room.</para></summary>
    public const double DoorStandoffDu = 1.0;

    /// <summary>How many walkers one room may have afoot at once.
    ///
    /// <para>Two, and the number is about the READ rather than about the frame cost. One person standing up
    /// and crossing a hall is an event the captain looks at; four at once is a fire drill, and a room that
    /// empties itself every watch is not a room with a metabolism, it is a room with a bug. Two also leaves
    /// the arrival its own slot beside a departure, which is the pair the owner asked for — somebody going,
    /// somebody coming.</para></summary>
    public const int MostAtOnce = 2;

    /// <summary>The share of a watch that a scheduled departure may fall in, as a fraction from the start.
    ///
    /// <para>Departures are dealt into the FIRST part of the shift and never the last, for one reason: a
    /// walk scheduled at 0.99 of a watch is a walk the watch turns over in the middle of, and the room would
    /// rebuild under a body halfway across it. Three quarters leaves every scheduled leg a quarter of a
    /// four-hour shift to finish in, which is longer than the longest hall by three orders of
    /// magnitude.</para></summary>
    public const double LastCallFraction = 0.75;

    /// <summary>What fraction of the occupied tops in a room see somebody stand up and go, per watch.
    ///
    /// <para>A third. Low on purpose: the beat is worth having because it is not constant. A room in which
    /// everybody leaves every shift reads as a station being evacuated, and a room in which one person in
    /// three does reads as a canteen.</para></summary>
    public const double LeaversPerWatch = 1.0 / 3.0;

    /// <summary>
    /// One movement the shift has already decided on: who, how far into the watch, and which door.
    /// </summary>
    /// <param name="Plate">Their plate, as the room already knows them — never a second name.</param>
    /// <param name="TableIndex">The top they get up from, so a caller can key state off it and take their
    /// seat back. −1 for an arrival, who is not sitting anywhere yet.</param>
    /// <param name="AtSecondsIntoWatch">When in the shift it happens. A fraction of
    /// <see cref="PatronRota.WatchSeconds"/>, and never a clock reading.</param>
    /// <param name="Door">Which of the floor's locked doors, by index into the list this was resolved
    /// against.</param>
    public readonly record struct Move(string Plate, int TableIndex, double AtSecondsIntoWatch, int Door);

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

        var leaving = new List<Move>();
        if (locked.Count == 0)
        {
            return leaving;
        }

        foreach (CanteenRegulars.TableSeat top in tops)
        {
            if (top is not { Taken: true, Quiet: false, Plate: { Length: > 0 } plate })
            {
                continue;
            }

            ulong seed = DiceRule.Seed($"hive:egress:goes:{bodyId}:{level}:{top.Index}", watch);
            var roll = new DeterministicRandom(seed);
            if (roll.NextDouble() >= LeaversPerWatch)
            {
                continue;
            }

            double at = roll.NextDouble() * LastCallFraction * PatronRota.WatchSeconds;
            leaving.Add(new Move(plate, top.Index, at, DoorFor(bodyId, level, watch, plate, locked)));
        }

        leaving.Sort(static (a, b) => a.AtSecondsIntoWatch.CompareTo(b.AtSecondsIntoWatch));
        if (leaving.Count > MostAtOnce)
        {
            leaving.RemoveRange(MostAtOnce, leaving.Count - MostAtOnce);
        }
        return leaving;
    }

    /// <summary>
    /// …AND WHICH DOOR THE ONE WHO COMES TO YOUR TABLE COMES OUT OF.
    ///
    /// <para><b>Owner:</b> <i>"we can have npcs arrive at bar from locked place… Now it is possible to have
    /// NPC ask to sit down at our table and offer a quest! This is the classic TTRPG event."</i></para>
    ///
    /// <para>The arrival is TRIGGERED rather than scheduled — <c>SittingAlone.SomebodyComes</c> already owns
    /// whether anybody comes, and moving that decision would be a second opinion about one fact. What is
    /// scheduled is the PROVENANCE: which door in the building she was behind before she was at your elbow,
    /// frozen per (site, floor, watch, top) so the same shift always produces the same one. That is the whole
    /// cold open — a door that has never opened for the captain opens for somebody, and no line explains
    /// it.</para>
    /// </summary>
    /// <returns>An index into <paramref name="locked"/>, or −1 on a floor with no such door — in which case
    /// the caller has no provenance to offer and must not invent one.</returns>
    public static int ArrivalDoor(
        string bodyId, int level, long watch, int tableIndex,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked) =>
        DoorFor(bodyId, level, watch, $"arrival:{tableIndex}", locked);
}
