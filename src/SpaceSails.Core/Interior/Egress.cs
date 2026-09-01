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

    /// <summary>
    /// WHO A CANTEEN HALL ACTUALLY HAS IN IT, as the arithmetic needs them — the projection from the Hive's
    /// own furniture onto <see cref="Occupant"/>, written once.
    ///
    /// <para>Cabinets are skipped (nobody is in one) and so is any top the room did not seat. The
    /// <see cref="Occupant.Index"/> is the TOP'S OWN ORDINAL and never its position in this list: every seeded
    /// roll in a hall is keyed on it, and handing on a list position instead re-keys the whole room the moment
    /// one top is empty. That slip is silent, and <c>THE_PROJECTION_NamesTheTopsTheRoomActuallySeated</c> is
    /// what catches it.</para>
    ///
    /// <para>#1061 · Published rather than kept inside <see cref="Departures"/> because two questions are now
    /// asked of one reading of a room: who finishes and goes, and whose table a man working the room stops at.
    /// Two projections that agree today is this repository's oldest bug class with a salesman walking through
    /// it.</para>
    ///
    /// <para><b>This one is BODIES IN CHAIRS, and the crowd is in chairs.</b> The narrower reading the
    /// schedule wants is <see cref="OnTheSchedule"/>, one clause further down — see the note on it for why
    /// the difference is written as a second NAME rather than as a flag on this one.</para>
    /// </summary>
    public static IReadOnlyList<Occupant> Seated(IReadOnlyList<CanteenRegulars.TableSeat> tops)
    {
        ArgumentNullException.ThrowIfNull(tops);

        var seated = new List<Occupant>(tops.Count);
        foreach (CanteenRegulars.TableSeat top in tops)
        {
            // Cabinets are skipped — nobody is in one — and so is any top the room did not seat.
            if (top is { Taken: true, Quiet: false, Plate: { Length: > 0 } plate })
            {
                seated.Add(new Occupant(top.Index, plate));
            }
        }

        return seated;
    }

    /// <summary>
    /// #731 (B1 canteen) · …AND THE ONES THE SHIFT MAY GIVE LEGS TO. <see cref="Seated"/> less the crowd,
    /// <b>because the crowd is DATA.</b>
    ///
    /// <para>#751 wrote that law on the day it built them: <i>"A background patron is a plate, a bark and a
    /// chair. No pathing, no schedule, no per-frame anything… WASM perf is a law here and this is how it is
    /// kept: by there being nothing to run."</i> The projection was feeding them into the schedule anyway, and
    /// the cost was not only the frames: a hall holds a dozen background tops against the rota's three, so the
    /// two departures a shift deals were the crowd's four times out of five and the ROTA'S OWN TURNOVER — the
    /// thing this room's noticeboard is about, and the thing #731 names as this floor's whole customer — was
    /// invisible. Measured before the clause: 84 named departures against 332 the crowd took, and the agency
    /// temp got out of the room three times in two hundred and forty shifts.</para>
    ///
    /// <h3>#1061 · Why this is a second NAME and not a flag on <see cref="Seated"/></h3>
    ///
    /// <para>Because the two questions asked of this room want two different readings of it, and the
    /// difference between them is a LAW rather than a caller's preference — so it is said out loud in a name
    /// instead of hidden in a <c>bool</c> somebody has to remember to pass.</para>
    ///
    /// <para><b>A schedule</b> asks who may be given legs, and the crowd may not: standing one of them up
    /// would be pathing, a schedule and per-frame anything, which is exactly the three things #751 says they
    /// do not have. <b>A salesman's round</b> (<see cref="Marks"/>) asks who is sitting there to be stood
    /// beside, and a background patron plainly is — the crowd is precisely who an insurance man sells to, and
    /// a Fess who walked past a dozen people eating to reach the one named yard hand would be working a rota
    /// rather than a room. Stopping at their table gives them nothing to run: HE has the legs, they keep
    /// their plate, their bark and their chair, and nothing on their side gains a frame's work. #751's law is
    /// untouched by a man standing next to it.</para>
    /// </summary>
    public static IReadOnlyList<Occupant> OnTheSchedule(IReadOnlyList<CanteenRegulars.TableSeat> tops)
    {
        ArgumentNullException.ThrowIfNull(tops);

        // The crowd, by the room's own ordinals — collected first so this method is Seated's answer with one
        // clause over it, and never a second opinion about what "in a chair" means.
        var crowd = new HashSet<int>();
        foreach (CanteenRegulars.TableSeat top in tops)
        {
            if (top.Stranger)
            {
                crowd.Add(top.Index);
            }
        }

        if (crowd.Count == 0)
        {
            return Seated(tops);
        }

        var named = new List<Occupant>(tops.Count);
        foreach (Occupant who in Seated(tops))
        {
            if (!crowd.Contains(who.Index))
            {
                named.Add(who);
            }
        }

        return named;
    }

    /// <summary>
    /// #1061 · <b>WHOSE TABLES SOMEBODY WORKING THIS ROOM STOPS AT, IN WHAT ORDER, AND FOR HOW LONG.</b>
    ///
    /// <para>The third question asked of one shift, and deliberately the same arithmetic as the other two:
    /// pure and deterministic in (site, floor, watch, who), off <see cref="DiceRule.Seed"/> and
    /// <see cref="DeterministicRandom"/>, over the room's own people in the room's own order. No wall clock,
    /// no <c>System.Random</c> — the frozen-watch law (#709), so a captain who reloads a save watches the same
    /// man work the same tables in the same order for the same lengths of time.</para>
    ///
    /// <para><b>It is a shuffle and not a roll</b>, and that is the difference between this and
    /// <see cref="Departures"/>. Who LEAVES is a rarity: most people finish their drink and stay. Who a
    /// salesman stops at is not — he stops at everybody he has time for, and the only questions are which of
    /// them and in what order. A per-person roll here would produce empty rounds on a third of watches, and an
    /// empty round is a beat that mostly does not happen, which is the same as not having built it.</para>
    ///
    /// <para>At most <see cref="MostMarks"/> stops, so his shift ENDS. Nothing is said at any of them.</para>
    /// </summary>
    /// <param name="seated">Who is in the room, in the room's own order — a canteen hall's through
    /// <see cref="Seated"/>, a bar's off its own rota.</param>
    /// <param name="who">Whose round this is, folded into the seed so two people working one room do not
    /// walk the same line.</param>
    public static IReadOnlyList<Patter> Marks(
        string bodyId,
        int level,
        long watch,
        string who,
        IReadOnlyList<Occupant> seated)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(who);
        ArgumentNullException.ThrowIfNull(seated);

        var pool = new List<Occupant>(seated.Count);
        foreach (Occupant one in seated)
        {
            if (one.Plate is { Length: > 0 })
            {
                pool.Add(one);
            }
        }

        if (pool.Count == 0)
        {
            return [];
        }

        var roll = new DeterministicRandom(DiceRule.Seed($"hive:egress:works:{bodyId}:{level}:{who}", watch));

        // The room's own order, shuffled — so which tables he works, and the order he works them in, are one
        // frozen fact about this watch rather than "the first three the room happens to list".
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Math.Min(i, (int)(roll.NextDouble() * (i + 1)));
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int stops = Math.Min(MostMarks, pool.Count);
        var round = new List<Patter>(stops);
        for (int i = 0; i < stops; i++)
        {
            double beat = ShortestPatterSeconds
                          + (roll.NextDouble() * (LongestPatterSeconds - ShortestPatterSeconds));
            round.Add(new Patter(pool[i].Index, pool[i].Plate, beat));
        }

        return round;
    }

    /// <summary>
    /// …AND THE SAME QUESTION ASKED OF A ROOM THAT IS NOT A CANTEEN. The overload above is a projection onto
    /// this one, so the Hive's hall and a docked station's bar deal their shifts with one arithmetic and one
    /// set of seeds rather than with two that agree today.
    /// </summary>
    /// <param name="seated">Who is in the room, in the room's own order.</param>
    public static IReadOnlyList<Move> Departures(
        string bodyId,
        int level,
        long watch,
        IReadOnlyList<Occupant> seated,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked) =>
        Deal(bodyId, level, watch, seated, locked, "goes", LeaversPerWatch, "");

    /// <summary>
    /// #731 · WHO TURNS UP, THIS WATCH — the other half of the same schedule, and the owner's own words for
    /// why it exists: <i>"also just other customers arriving and leaving in the bars already does a lot… they
    /// can go behind doors that are locked to us."</i>
    ///
    /// <para>Identical machinery to <see cref="Departures"/> — one seeded roll against
    /// <see cref="ComersPerWatch"/>, a moment inside the first <see cref="LastCallFraction"/> of the shift, and
    /// a door out of the locked list — run over the people the room does NOT currently have. The door is the
    /// point: somebody comes OUT of a leaf the captain's own TRY is refused at, crosses the floor on real legs
    /// and takes a chair, and no line explains how they were behind it. That is the cold open the full stop is
    /// the mirror of, and both are the same walker.</para>
    ///
    /// <para>The salt is different from the departure's, so a room does not send the same person out and bring
    /// them in on one roll; and the door is dealt off <c>in:</c> plus their plate, so the leaf somebody comes
    /// out of and the leaf they would leave by are two independent facts about one evening.</para>
    /// </summary>
    /// <param name="expected">Who is not in the room, each carrying the place they would take if they came —
    /// the caller's to allot, because a free chair is a fact about a room and not about a schedule.</param>
    public static IReadOnlyList<Move> Arrivals(
        string bodyId,
        int level,
        long watch,
        IReadOnlyList<Occupant> expected,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked) =>
        Deal(bodyId, level, watch, expected, locked, "comes", ComersPerWatch, "in:");

    /// <summary>
    /// THE DEAL ITSELF, ONCE — the arithmetic both directions and both rooms spend.
    ///
    /// <para>One pass over the room's own people in the room's own order. Each gets one seeded roll against
    /// <paramref name="share"/>; the ones that clear it are dealt a moment inside the first
    /// <see cref="LastCallFraction"/> of the shift and a door out of the locked list. Returned in the order
    /// they HAPPEN rather than in the room's order, because a caller stepping down the list as the watch runs
    /// wants the next one at the front, and sorting it here means nobody sorts it twice. At most
    /// <see cref="MostAtOnce"/> survive the cut, for the reason written on that constant.</para>
    /// </summary>
    /// <param name="salt">Which half of the schedule this is — folded into the seed so the two halves are
    /// independent rolls about one person on one watch.</param>
    /// <param name="doorPrefix">…and the same for the door, so somebody's way in and their way out are not
    /// forced to be the same leaf by an accident of seeding.</param>
    private static IReadOnlyList<Move> Deal(
        string bodyId,
        int level,
        long watch,
        IReadOnlyList<Occupant> people,
        IReadOnlyList<UndergroundComplex.LockedDoor> locked,
        string salt,
        double share,
        string doorPrefix)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(locked);

        var dealt = new List<Move>();
        if (locked.Count == 0)
        {
            return dealt;
        }

        foreach (Occupant who in people)
        {
            if (who.Plate is not { Length: > 0 } plate)
            {
                continue;
            }

            ulong seed = DiceRule.Seed($"hive:egress:{salt}:{bodyId}:{level}:{who.Index}", watch);
            var roll = new DeterministicRandom(seed);
            if (roll.NextDouble() >= share)
            {
                continue;
            }

            double at = roll.NextDouble() * LastCallFraction * PatronRota.WatchSeconds;
            dealt.Add(new Move(
                plate, who.Index, at, DoorFor(bodyId, level, watch, doorPrefix + plate, locked)));
        }

        dealt.Sort(static (a, b) => a.AtSecondsIntoWatch.CompareTo(b.AtSecondsIntoWatch));
        if (dealt.Count > MostAtOnce)
        {
            dealt.RemoveRange(MostAtOnce, dealt.Count - MostAtOnce);
        }
        return dealt;
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
