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
public static partial class Egress
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

    /// <summary>
    /// #731 · <b>WHERE THE Nth BODY IN A QUEUE FOR ONE DOOR STANDS.</b> The arithmetic of a single-file
    /// exit, stated once because two crews now walk one.
    ///
    /// <para>#731 v2 gave the hull sweep team a file at a wreck's shuttle lock and worked this out inline
    /// (<c>Map.SweepTeam.cs</c>): the head of the queue stands <see cref="DoorStandoffDu"/> off the leaf, and
    /// everybody behind them one spacing further back along the way they came. The repo crew walking home to
    /// their own boat wants the SAME queue at a hatch that is not on a spine and not at <c>y = 0</c>, and a
    /// second copy of this sum is this repository's oldest bug class with a body standing in it — two files
    /// disagreeing by a body-width is two figures drawn inside one another.</para>
    ///
    /// <para>So the door is a point, the queue runs along a DIRECTION away from it (whichever side the people
    /// are on — a wreck's file runs back down the spine, a boat's runs off its hatch), and rank 0 is whoever
    /// is working the leaf. The direction is normalised here rather than trusted, because a caller handing a
    /// two-unit vector would silently double every spacing in the file.</para>
    /// </summary>
    /// <param name="doorX">The leaf, or the hatch, or whatever the queue is for.</param>
    /// <param name="doorY">…the same.</param>
    /// <param name="awayX">Which way the queue runs, as a direction from the door. Its length is ignored.</param>
    /// <param name="awayY">…the same.</param>
    /// <param name="rank">0 for whoever is working it, 1 for the next one back, and so on.</param>
    /// <param name="spacingDu">How far apart two bodies in the file stand.</param>
    /// <exception cref="ArgumentOutOfRangeException">A negative rank, a spacing that is not positive and
    /// finite, or a direction with no length — none of which is a queue.</exception>
    public static (double X, double Y) PlaceInTheFile(
        double doorX, double doorY, double awayX, double awayY, int rank, double spacingDu)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rank);
        if (!(spacingDu > 0) || double.IsInfinity(spacingDu))
        {
            throw new ArgumentOutOfRangeException(
                nameof(spacingDu), spacingDu, "Two bodies in a file stand a positive, finite distance apart.");
        }

        double length = Math.Sqrt((awayX * awayX) + (awayY * awayY));
        if (!(length > 0) || double.IsInfinity(length))
        {
            throw new ArgumentOutOfRangeException(
                nameof(awayX), (awayX, awayY), "A file runs in some direction away from the door it is for.");
        }

        double back = DoorStandoffDu + (rank * spacingDu);
        return (doorX + (awayX / length * back), doorY + (awayY / length * back));
    }

    /// <summary>How many walkers one room may have afoot at once.
    ///
    /// <para>Two, and the number is about the READ rather than about the frame cost. One person standing up
    /// and crossing a hall is an event the captain looks at; four at once is a fire drill, and a room that
    /// empties itself every watch is not a room with a metabolism, it is a room with a bug. Two also leaves
    /// the arrival its own slot beside a departure, which is the pair the owner asked for — somebody going,
    /// somebody coming.</para></summary>
    public const int MostAtOnce = 2;

    /// <summary>
    /// #973 L0 · HOW MANY FIGURE SLOTS A ROOM WITH A METABOLISM NEEDS — the room's own law
    /// (<see cref="MostAtOnce"/>) plus the visitors who are not the room's people.
    ///
    /// <para>Stated ONCE, here, because two rooms now ask it: the Hive's canteen floor and a docked station's
    /// bar. Two constants for one fact is this repository's oldest bug class, and it has already thrown twice
    /// on exactly this number — <c>DeckPlan.MaxDroids</c> lagged the walker band and fifteen frame fingerprints
    /// came back with an <c>IndexOutOfRangeException</c>, once on #731 and again on #973 L2. A third room may
    /// not be allowed to invent a third arithmetic.</para>
    ///
    /// <para>The visitor is <see cref="NebulaRep.OnTheFloorAtOnce"/> and not a number of its own for the same
    /// reason: <see cref="MostAtOnce"/> is a law about REGULARS, satisfied constantly on a heaving watch, and
    /// a salesman sharing that allowance is a salesman who never gets on the floor at all.</para>
    /// </summary>
    public const int BandSlots = MostAtOnce + NebulaRep.OnTheFloorAtOnce;

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

    /// <summary>What share of a room's ABSENT people come in off the street — or out of the back — per watch.
    ///
    /// <para><b>Owner, 2026-09-01:</b> <i>"also just other customers arriving and leaving in the bars already
    /// does a lot… they can go behind doors that are locked to us."</i> A room whose schedule only ever DRAINS
    /// is not a room with a metabolism, it is a room being evacuated slowly — so the same watch that decides
    /// who finishes decides who turns up, at the same rate and out of the same list of leaves. One arithmetic,
    /// run in both directions.</para>
    ///
    /// <para>The same third as <see cref="LeaversPerWatch"/>, and it is deliberately the same NUMBER rather
    /// than a second one to tune: over a long evening a room that loses a third of its sitters and gains a
    /// third of its absentees per shift is a room that stays about as full as it started, which is what a bar
    /// looks like.</para></summary>
    public const double ComersPerWatch = LeaversPerWatch;

    /// <summary>
    /// ONE BODY A ROOM HAS, as the shift's arithmetic needs them — and nothing else about them.
    ///
    /// <para>The two rooms that ask this question do not share a furniture type: underground it is a
    /// <see cref="CanteenRegulars.TableSeat"/> at a canteen top, and in a docked station's bar it is a name off
    /// <see cref="PatronRota.Roster"/> in one of the bar's own numbered chairs. What the arithmetic actually
    /// needs of either is TWO facts — where they sit in the room's own numbering, and what the room calls
    /// them — so that is what it takes. A second copy of the deal, one per room, is this repository's oldest
    /// bug class with a barman in it; the canteen's own overload below is a projection onto this and not a
    /// second opinion.</para>
    /// </summary>
    /// <param name="Index">Their place in the room's own numbering — the top's ordinal underground, the
    /// chair's index in a bar. For somebody who is not in the room yet, the place they will take.</param>
    /// <param name="Plate">What the room calls them, verbatim and never a second name.</param>
    public readonly record struct Occupant(int Index, string Plate);

    /// <summary>
    /// #1061 · ONE STOP ON A ROUND — whose place in the room somebody working it pauses at, what the room
    /// calls them, and how long the pause lasts.
    ///
    /// <para>Owner, 2026-09-01: <i>"let's at some point work on those A* walking insurance salesmen at
    /// stations."</i> A salesman crossing a bar to the captain's table is a card; a salesman crossing it to
    /// SOMEBODY ELSE'S table is nothing but a body, a pause and a body again — and that is the whole beat.
    /// The captain watching him work two tables before he reaches theirs is how you see the pitch
    /// coming.</para>
    ///
    /// <para>There is no line in this record and there is no line anywhere behind it. The pause IS the
    /// patter, and §13.8 is what keeps it that way: a room that captioned an NPC selling to an NPC would be
    /// the game saying what the room already said.</para>
    /// </summary>
    /// <param name="Index">Their place in the room's own numbering — the same ordinal
    /// <see cref="Occupant.Index"/> carries, so a caller looks the body up the one way it already does.</param>
    /// <param name="Plate">What the room calls them, verbatim and never a second name.</param>
    /// <param name="BeatSeconds">How long the pause at that place lasts, between
    /// <see cref="ShortestPatterSeconds"/> and <see cref="LongestPatterSeconds"/>.</param>
    public readonly record struct Patter(int Index, string Plate, double BeatSeconds);

    /// <summary>#1061 · The shortest a beat of patter lasts. Short enough to read as a man being brushed
    /// off.</summary>
    public const double ShortestPatterSeconds = 5.0;

    /// <summary>#1061 · …and the longest. Long enough to read as somebody who is actually listening, and
    /// short enough that a captain who sat down two tables away is not watching a monologue.</summary>
    public const double LongestPatterSeconds = 13.0;

    /// <summary>#1061 · How many of a room's own people one round stops at, at most.
    ///
    /// <para>Three, for <see cref="MostAtOnce"/>'s reason turned around: the beat is worth having because it
    /// ENDS. A salesman who works every top in the room forever is furniture that moves, and a room he never
    /// leaves has no shift in it.</para></summary>
    public const int MostMarks = 3;

    /// <summary>
    /// #1061 · HOW MANY TABLES THE CAPTAIN WATCHES HIM WORK BEFORE HE REACHES THEIRS. Two, and the number is
    /// the whole design: <i>the captain watching him work two tables before reaching yours is the point — you
    /// see the pitch coming.</i>
    ///
    /// <para>One would be an accident and three would be a wait. It is a floor and not a quota: a room with
    /// fewer marks than this in it is worked out sooner, and he comes to the table then rather than standing
    /// about waiting for people who are not in the room.</para>
    /// </summary>
    public const int MarksBeforeTheTable = 2;

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
}
