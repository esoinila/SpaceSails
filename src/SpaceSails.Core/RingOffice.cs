using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #817 · AN OFFICE WITHOUT FURNITURE IS NOT AN OFFICE — what is actually ON the floor of a room with the
/// view, laid off the room's own walls.
///
/// <para>Owner, live in a park-view suite on the evening of 2026-08-11, standing in a 40 x 50 du landscape
/// office of bare deck: <i>"It really needs tables … the cubicles etc chairs maybe tables etc. It is way too
/// empty"</i> · <i>"I mean in office people sit down … what on the floor there?"</i> · <i>"Let's make some
/// cubicles / desks / chairs we can sit in"</i>. And, for the big ones: <i>"Such a big premium office would
/// have little kitchen and couple toilets also"</i> · <i>"maybe some privacy cabinets also"</i>.</para>
///
/// <h3>Why this is a file and not forty lines inside the carve</h3>
///
/// <para>#813 carved the ring — walls, glass, a street door, a plate — and stopped at the threshold. The
/// obvious repair is to drop a few rectangles into <c>RingBox</c> at coordinates that look right on the one
/// floor somebody was standing on, and that is this repo's oldest named bug class: <b>unaudited client
/// geometry literals</b>. Not one number below is a position. Every one of them is a CLEARANCE or a PITCH,
/// and the room is handed in — so a suite 48.8 du wide and one 19 du wide are furnished by the same sentence
/// and neither of them was measured off a screenshot.</para>
///
/// <h3>The one law that makes the guards cheap</h3>
///
/// <para>Nothing is laid within <see cref="StreetClearDu"/> of the wall the doors are cut in, or within
/// <see cref="GlassClearDu"/> of the park-facing wall — and those are the only two walls of a ring room that
/// ever carry a door. So <i>"no fitting stands in a doorway's clearance"</i> is not a per-door search with a
/// per-door bug in it: it falls out of two aisles, both of which are wider than
/// <see cref="DoorClearDu"/>. The piers between neighbouring suites carry no door at all and get the much
/// tighter <see cref="PierClearDu"/>, which is how a 19 du room has room for anything.</para>
///
/// <h3>…and the one law that is easy to forget</h3>
///
/// <para><b>The room's own centre stays standable.</b> The A* audit that has walked every ring room since
/// #813 walks to <c>room.X, room.Y</c> — the middle of the box — so a boardroom table laid where a boardroom
/// table goes would report fourteen rooms a floor as "something was built through it". Every fitting is
/// pushed clear of <see cref="RoomCentreClearDu"/> around that square, which is why the negotiation table
/// runs down the room OFF the centre line rather than through it.</para>
///
/// <para>Pure, in the shape <see cref="ParkBenches"/> is pure: it is handed a room the generator carved and
/// answers what is in it. It has no clock, no dice, and no opinion about where a room goes.</para>
/// </summary>
public static class RingOffice
{
    // ── THE CLEARANCES ────────────────────────────────────────────────────────────────────────────────

    /// <summary>How much floor a doorway keeps in front of it. The door's own half-width
    /// (<see cref="UndergroundComplex.DoorHalf"/>) plus a body, rounded up: the square a leaf swings into and
    /// a captain stands in while it does. Every guard about furniture and doors is written against this
    /// number, and every aisle below is deliberately WIDER than it.</summary>
    public const double DoorClearDu = UndergroundComplex.DoorHalf + 0.8;

    /// <summary>The aisle kept inside the wall the street doors are cut in. Wider than
    /// <see cref="DoorClearDu"/> on purpose — see the class summary: it is what turns "no desk blocks a door"
    /// from a search into an arithmetic fact.</summary>
    public const double StreetClearDu = 5.0;

    /// <summary>The same at the park-facing wall. It is the walkway at the window — and it is the same number
    /// as the street's because that wall carries doors too now: every view suite has a gate onto the green
    /// (#817's second ruling), not only the far band's back of house.</summary>
    public const double GlassClearDu = 5.0;

    /// <summary>…and the gap left at the PIERS, which are the party walls between neighbouring suites and are
    /// the two walls of a ring room that can never carry a door. A hand's breadth, because a 19 du corner
    /// office furnished with five-du aisles on all four sides is a room with a rug in it.</summary>
    public const double PierClearDu = 1.5;

    /// <summary>How much clear floor is kept around a room's own centre — the square the ring's A* audit
    /// (<c>TheRingIsWalkableTests</c>) stands a body on to say the room was reached at all. A body's radius
    /// and then some. A fitting laid across it does not read as a bug in the furniture; it reports as
    /// <i>"ring room 3 has no floor at its own centre — something was built through it"</i> on every floor in
    /// the game, which is a long way from the table somebody actually moved.</summary>
    public const double RoomCentreClearDu = 1.6;

    // ── THE PITCHES ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Half the depth of a desk bank — the run of worktop a row of people sit at. Thin, because it
    /// is drawn on a plan and read at plate scale: the en-suite's whole pan is one 2.2 du segment (#707) and
    /// the owner named that fixture as the one interior in the building that reads right.</summary>
    public const double DeskHalfDepthDu = 1.0;

    /// <summary>How far a chair stands off the bank it belongs to. Clear of the worktop by more than a body's
    /// radius, so sitting down (#820) can put the captain ON the published seat without landing them inside
    /// the desk.</summary>
    public const double ChairSetbackDu = 2.4;

    /// <summary>How far back from the worktop a cubicle screen reaches — past the chair, which is the whole
    /// difference between a cubicle and a table.</summary>
    public const double CubicleReachDu = 4.0;

    /// <summary>
    /// #817 · HOW MUCH MORE WORKTOP A DESK GIVES ONE PERSON THAN A CANTEEN TOP DOES.
    ///
    /// <para>Owner's ruling on how to build these at all: <i>"office tables … [are] just more rectangular and
    /// have more table area / person. The functionality is about the same otherwise."</i> This is the second
    /// clause of that sentence as a number, and it is the ONLY thing this file says about seat density —
    /// the base is the hall's own published pitch (<see cref="UndergroundComplex.HallTopPitchDu"/>), so a
    /// canteen that gets roomier makes the offices roomier with it rather than the two quietly drifting
    /// apart. The first clause (rectangular) is the shape of <see cref="Fitting.DeskBank"/>: a run of
    /// worktop rather than a ring.</para>
    /// </summary>
    public const double DeskAreaPerSeatFactor = 1.25;

    /// <summary>Between two people at the same bank. Half a hall's table pitch — two people abreast in the
    /// ground one round top stands on — widened by <see cref="DeskAreaPerSeatFactor"/>.</summary>
    public static double SeatPitchDu => UndergroundComplex.HallTopPitchDu / 2.0 * DeskAreaPerSeatFactor;

    /// <summary>…and between two banks, back to back down the room. A hall's whole table pitch less the
    /// reach of the screens either side of the gangway between them.</summary>
    public static double RowPitchDu => UndergroundComplex.HallTopPitchDu - (2 * DeskHalfDepthDu);

    /// <summary>The fewest a bank seats. A bank is a run of worktop several people sit at; a bank with one
    /// chair at it is a desk, and this file lays banks. It is also what makes
    /// <see cref="SeatsPerViewSuite"/> true in the narrowest suite on the ring — a 19 du end block whose
    /// single bank would otherwise round down to one seat and hand the owner back the room he
    /// complained about.</summary>
    public const int MinSeatsPerBank = 2;

    /// <summary>How many banks a room gets, however deep it is. A cap and not a target: every segment here is
    /// drawn and collided with on a floor that carried 270 wall segments in total before this file existed,
    /// and a room with six rows in it is a room nobody can walk across anyway.</summary>
    public const int MaxDeskRows = 3;

    /// <summary>The gap a bank stops short of the far pier by, so a row is a row and not a wall across the
    /// room. Alternating ends (see <see cref="Bank"/>), which makes the circulation a weave rather than a
    /// corridor — and costs four segments a row rather than eight.</summary>
    public const double BankAisleDu = 6.0;

    /// <summary>Between two people across a table, and down its length. Tighter than
    /// <see cref="SeatPitchDu"/> because a negotiation table seats people facing each other rather than
    /// side by side at a worktop — a quarter of a hall's top pitch, at the same
    /// <see cref="DeskAreaPerSeatFactor"/>.</summary>
    public static double TableSeatPitchDu =>
        UndergroundComplex.HallTopPitchDu / 4.0 * DeskAreaPerSeatFactor;

    /// <summary>Half the width of the long table.</summary>
    public const double TableHalfWidthDu = 2.0;

    /// <summary>How far a chair sits off the table's edge.</summary>
    public const double TableChairGapDu = 1.4;

    /// <summary>Half the depth of a reception counter, and how thick a run of shelving is drawn.</summary>
    public const double CounterHalfDepthDu = 1.0;

    /// <summary>How far in from the street aisle the reception counter stands — the pace of floor between the
    /// door and the person who decides whether you get past it.</summary>
    public const double CounterStandoffDu = 2.0;

    /// <summary>How long a plain room's bench is.</summary>
    public const double BenchDu = 6.0;

    // ── THE SIZE GATES ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #817 · How much street frontage a suite needs before it earns the SERVICE STRIP — the kitchenette, the
    /// two WC cubicles and the privacy booths.
    ///
    /// <para>Owner: <i>"Such a big premium office would have little kitchen and couple toilets also"</i>, and
    /// the operative word is <b>big</b>. This is the number that word is cashed out as. Thirty du is a shade
    /// under the ring's own <see cref="UndergroundComplex.RingRoomTargetDu"/> and comfortably over its
    /// <see cref="UndergroundComplex.RingRoomMinDu"/>, so the band's full-width suites take the tier and its
    /// corner offices and its 19 du end blocks do not — which is #775's amenity gradient arriving one more
    /// time, as plumbing.</para>
    /// </summary>
    public const double BigSuiteDu = 30.0;

    /// <summary>How wide the service strip is, measured off the pier it stands against.</summary>
    public const double StripDu = 6.0;

    /// <summary>How long a WC cubicle is down the strip.</summary>
    public const double CubicleDu = 5.0;

    /// <summary>…and a privacy booth, which is one seat and a door and nothing else.</summary>
    public const double BoothDu = 4.5;

    /// <summary>The gap between two boxes down the strip, so the strip reads as a row of cells rather than as
    /// one long cupboard. Nobody walks between two cubicles, so it is a joint and not a passage.</summary>
    public const double StripGapDu = 1.5;

    /// <summary>
    /// How much floor is left BETWEEN two fittings a body has to walk between. Wide enough for a captain and
    /// then some, and it is the number that turns "the room is furnished" into "the room is walkable".
    ///
    /// <para>Learned the expensive way inside this very issue and worth the paragraph: a reception counter
    /// laid two paces inside the door and a cubicle bank laid behind it left FOUR du between them — plenty —
    /// and then the bank's own screens reached back three of those four, pinching the only route across the
    /// room to one du. The A* audit found it on three sites (<c>RECEPTION · APPOINTMENTS HELD … cannot be
    /// reached from the car at all</c>) and nothing about the plan looked wrong: every fitting was inside its
    /// own band, and the bands did not overlap. What overlapped was a band and a REACH. So a caller that lays
    /// something in front of a bank hands the bank a start line this far past its own edge, and the screens
    /// are measured from there.</para>
    /// </summary>
    public const double GangwayDu = 3.0;

    /// <summary>Half the width of the leaf on a cubicle or a booth. A bedroom-small room takes a
    /// bedroom-small door (#822): the building's own <see cref="UndergroundComplex.DoorHalf"/> is 3.2, and a
    /// 6.4 du opening in a 5 du wall is not a door, it is a missing wall.</summary>
    public const double CubicleDoorHalfDu = 1.6;

    /// <summary>How many WC cubicles a big suite gets. The owner said <i>"couple toilets"</i> and a couple is
    /// two.</summary>
    public const int Cubicles = 2;

    /// <summary>…and how many privacy booths. <i>"maybe some privacy cabinets also"</i> — enough that
    /// somebody else being in one still leaves you one, which is the smallest number that makes the fixture
    /// a fact about the room rather than a prop.</summary>
    public const int Booths = 2;

    /// <summary>#817 · The fewest working seats a room with the view may publish. Owner's believability bar,
    /// live: you can sit down at MORE THAN ONE place in an office. One chair in a landscape suite is a prop;
    /// two is a room people work in.</summary>
    public const int SeatsPerViewSuite = 2;

    // ── WHAT A PIECE OF FURNITURE IS ──────────────────────────────────────────────────────────────────

    /// <summary>What a fitting IS — so a renderer can stencil it without measuring it, and a guard can say
    /// "every view suite has a desk" without pattern-matching on a string.</summary>
    public enum Fitting
    {
        /// <summary>A run of worktop several people sit at.</summary>
        DeskBank,

        /// <summary>The screen between two workstations at one bank. The word the owner used was
        /// "cubicles", and this is the du of it that makes one.</summary>
        Partition,

        /// <summary>The negotiation room's one long table.</summary>
        Table,

        /// <summary>A reception counter, or the kitchenette's own worktop.</summary>
        Counter,

        /// <summary>A run of shelving against a pier — the reading room's, and every plain room's.</summary>
        Shelving,

        /// <summary>A bench. What a back-of-house room has instead of a desk.</summary>
        Bench,

        /// <summary>The kitchenette block in a big suite's service strip.</summary>
        Kitchenette,

        /// <summary>A WC cubicle: a walled box with its own published door (#821 will lock it from the
        /// inside).</summary>
        Cubicle,

        /// <summary>A privacy booth — one seat, open-fronted, phone-box shaped.</summary>
        Booth,
    }

    /// <summary>
    /// #827 · WHICH SIDES OF A PIECE OF FURNITURE CARRY SEATS.
    ///
    /// <para>Owner's reading of the whole family, arriving while this issue was in flight: a canteen counter
    /// is <i>the biggest table, with seats on ONE side only</i> and the round tops are the same object with
    /// seats all the way round. So "how many sides" is a fact a table HAS rather than something each
    /// renderer works out from where the chairs happened to land — and an office desk is one-sided in
    /// exactly the sense a bar is, which is why the two can eventually be one record.</para>
    ///
    /// <para>The continuity of the row (a bar's seats have gaps in them where customers walk up to be
    /// served) is #827's own sweep and is deliberately not modelled here: nothing in a ring office has a
    /// broken seat row today, and inventing the field before the case exists would be a second opinion
    /// waiting to disagree with the first.</para>
    /// </summary>
    public enum Seating
    {
        /// <summary>Nobody sits at it. Shelving, a kitchenette, a screen.</summary>
        None,

        /// <summary>One side. A desk bank (the chairs are on the street side, looking at the glass over the
        /// worktop) and a reception counter (the staff side, looking at the door) — the bar's own shape.</summary>
        OneSide,

        /// <summary>Both long sides, facing each other across it. The negotiation table.</summary>
        BothSides,

        /// <summary>All the way round. What a canteen's round top is, and what nothing in an office
        /// is.</summary>
        AllRound,
    }

    /// <summary>One piece of furniture, as the box it occupies on the plan.</summary>
    /// <param name="Kind">What it is.</param>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Plate">What is stencilled on it, where it is the kind of thing that gets a stencil.
    /// Empty on a desk bank: a plate over every worktop in a landscape office is a room nobody can read.</param>
    /// <param name="Sides">#827 · Which sides of it carry chairs. See <see cref="Seating"/>.</param>
    public readonly record struct Fixture(
        Fitting Kind, double X0, double Y0, double X1, double Y1, string Plate,
        Seating Sides = Seating.None)
    {
        /// <summary>The middle of it — where its stencil is read from.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;
    }

    /// <summary>
    /// One chair, as a seat with a facing.
    /// </summary>
    /// <param name="Index">Its ordinal in the room, so a watch-scoped state key can name it.</param>
    /// <param name="X">Where a body goes when it sits down. #820 · sitting SNAPS the captain onto this
    /// coordinate, so it is the seat and not a hint at one — it is laid clear of the worktop it belongs to by
    /// more than a body's radius for exactly that reason.</param>
    /// <param name="Y">The same.</param>
    /// <param name="FaceX">Which way the person in it is looking, as a unit vector. Desk chairs face the
    /// GLASS: the view is what the room rents for and the furniture should agree.</param>
    /// <param name="FaceY">The same.</param>
    /// <param name="Room">The plate of the room it stands in, so sitting down can name where you are without
    /// the seat having to be looked up in anything.</param>
    public readonly record struct Chair(
        int Index, double X, double Y, double FaceX, double FaceY, string Room)
    {
        /// <summary>What is drawn over it, with the verb on it — #783's ruling, unchanged: <i>"why not use
        /// words like SIT DOWN here if it means sitting down?"</i></summary>
        public string DeckPlate => FreeChairPlate;

        /// <summary>#820 · Where the captain is put down when they stand up again. A ring chair is not a
        /// solid — the seat is a square you were already standing on to press [E] — so standing up leaves you
        /// exactly where sitting down put you, and nothing can trap the dot. Published rather than assumed,
        /// so the sweep that gives the park bench the same law has one seam to change.</summary>
        public (double X, double Y) StandAt => (X, Y);
    }

    /// <summary>Everything one room is furnished with, in one answer: what the deck draws and collides with,
    /// what a captain can sit on, and the doors the cubicles brought with them.</summary>
    /// <param name="Fixtures">The furniture, as boxes, for the stencils and for the guards.</param>
    /// <param name="Chairs">Every seat, in the room's own order.</param>
    /// <param name="Solids">The same furniture as WALL SEGMENTS — what actually goes into the floor's wall
    /// list, so it is drawn and walked into exactly the way the en-suite's pan and the park's raised beds
    /// are. One list, both jobs, and no second opinion about where a desk is.</param>
    /// <param name="Doors">The cubicles' own leaves, as published doorways. #821 · A real door and never a
    /// decorative gap: the lock-from-inside is coming and it must land on a door the building already
    /// knows about.</param>
    public readonly record struct Furnishing(
        IReadOnlyList<Fixture> Fixtures,
        IReadOnlyList<Chair> Chairs,
        IReadOnlyList<SurfaceLayout.Wall> Solids,
        IReadOnlyList<SurfaceLayout.Doorway> Doors)
    {
        /// <summary>Nothing at all — what a room too small to stand a desk in gets, and what nothing in the
        /// ring actually gets today. Kept so a degenerate box returns an empty answer rather than a
        /// half-built one.</summary>
        public static Furnishing Empty { get; } = new([], [], [], []);
    }

    // ── HOW THE ROOM IS READ ──────────────────────────────────────────────────────────────────────────

    /// <summary>Which dressing a room takes. Off its PLATE where it has one of the block's own
    /// (<see cref="UndergroundComplex.ParkViewPlates"/>), because the plate is the only thing that says what
    /// the room is for — and a room whose plate says NEGOTIATION ROOM and whose floor says twelve
    /// workstations is the sim and the sentence disagreeing, which is a bug class this house has a name
    /// for.</summary>
    public enum Dressing
    {
        /// <summary>Banks of workstations facing the glass. The default, and what most of the register
        /// means.</summary>
        Cubicles,

        /// <summary>One long table with chairs down both sides.</summary>
        LongTable,

        /// <summary>Shelving down both piers and a row of reading desks.</summary>
        ReadingRoom,

        /// <summary>A counter facing the door, with the desks behind it.</summary>
        Reception,

        /// <summary>Shelving and a bench. What a corner office with no view and the back of house get:
        /// cheap is fine, empty is not.</summary>
        Plain,
    }

    /// <summary>What this room is dressed as. The back of house keeps #801's plates and its own character —
    /// a potting shed is not an office — and a corner room has no view to sit facing, so both take the plain
    /// version.</summary>
    public static Dressing DressingFor(in UndergroundComplex.RingRoom room)
    {
        if (!room.HasView || room.Side == UndergroundComplex.RingSide.Far)
        {
            return Dressing.Plain;
        }
        if (room.Plate.Contains("NEGOTIATION", StringComparison.Ordinal))
        {
            return Dressing.LongTable;
        }
        if (room.Plate.Contains("READING ROOM", StringComparison.Ordinal))
        {
            return Dressing.ReadingRoom;
        }
        if (room.Plate.Contains("RECEPTION", StringComparison.Ordinal))
        {
            return Dressing.Reception;
        }
        return Dressing.Cubicles;
    }

    /// <summary>Does this room earn the service strip? Measured on the STREET FRONTAGE, which is the
    /// dimension the owner was looking down when he called the room big — and the same dimension the door
    /// count is scaled on, so "big" means one thing in this file.</summary>
    public static bool IsBigSuite(in UndergroundComplex.RingRoom room) =>
        room.HasView && room.Side != UndergroundComplex.RingSide.Far && FrontageOf(room) >= BigSuiteDu;

    /// <summary>How much street face a room presents — X on the two bands, Y on the two ends.</summary>
    public static double FrontageOf(in UndergroundComplex.RingRoom room) =>
        room.Side is UndergroundComplex.RingSide.Near or UndergroundComplex.RingSide.Far
            ? room.X1 - room.X0
            : room.Y1 - room.Y0;

    // ── THE FRAME ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A room read in its OWN axes — u along the street face, v inward from the street toward the glass.
    ///
    /// <para>The same trick <c>RingBox</c> plays with its from/to corners, and for the same reason: one
    /// layout sentence furnishes a room on any of the four sides without ever asking which side it is on.
    /// Get this wrong and every desk on the block's two ends is laid at ninety degrees to the room it is
    /// in — which is the shape of bug that is invisible in a diff and obvious on the first boot.</para>
    /// </summary>
    private readonly struct Frame
    {
        private readonly UndergroundComplex.RingSide _side;
        private readonly double _x0, _y0, _x1, _y1;

        internal Frame(in UndergroundComplex.RingRoom room)
        {
            _side = room.Side;
            (_x0, _y0, _x1, _y1) = (room.X0, room.Y0, room.X1, room.Y1);
            Horizontal = room.Side is UndergroundComplex.RingSide.Near or UndergroundComplex.RingSide.Far;
            ULo = Horizontal ? room.X0 : room.Y0;
            UHi = Horizontal ? room.X1 : room.Y1;
            Depth = Horizontal ? room.Y1 - room.Y0 : room.X1 - room.X0;
        }

        /// <summary>Does the street face run along X? True on the two bands, false on the two ends.</summary>
        internal bool Horizontal { get; }

        /// <summary>The frontage, in world coordinates on its own axis.</summary>
        internal double ULo { get; }

        /// <summary>The same, far end.</summary>
        internal double UHi { get; }

        /// <summary>How far it is from the street wall to the park wall.</summary>
        internal double Depth { get; }

        /// <summary>One point of the room's own grid, in the surface's coordinates.</summary>
        internal (double X, double Y) At(double u, double v) => _side switch
        {
            UndergroundComplex.RingSide.Near => (u, _y1 - v),
            UndergroundComplex.RingSide.Far => (u, _y0 + v),
            UndergroundComplex.RingSide.West => (_x0 + v, u),
            _ => (_x1 - v, u),
        };

        /// <summary>One box of the room's own grid, as a world rectangle with its corners in order.</summary>
        internal (double X0, double Y0, double X1, double Y1) Box(
            double uA, double vA, double uB, double vB)
        {
            (double ax, double ay) = At(uA, vA);
            (double bx, double by) = At(uB, vB);
            return (Math.Min(ax, bx), Math.Min(ay, by), Math.Max(ax, bx), Math.Max(ay, by));
        }

        /// <summary>Which way the glass is, as a unit vector — what a chair at a desk is looking at.</summary>
        internal (double X, double Y) TowardTheGlass => _side switch
        {
            UndergroundComplex.RingSide.Near => (0.0, -1.0),
            UndergroundComplex.RingSide.Far => (0.0, 1.0),
            UndergroundComplex.RingSide.West => (1.0, 0.0),
            _ => (-1.0, 0.0),
        };
    }

    // ── THE LAYOUT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #817 · FURNISH ONE ROOM. Handed the box the ring carved, it answers what is standing in it.
    ///
    /// <para>Everything is laid in the room's own u/v grid and mapped out at the end, so nothing below knows
    /// or cares which of the park's four walls this room is one of.</para>
    /// </summary>
    public static Furnishing Fit(in UndergroundComplex.RingRoom room)
    {
        var frame = new Frame(in room);
        double uA = frame.ULo + PierClearDu, uB = frame.UHi - PierClearDu;
        double vA = StreetClearDu, vB = frame.Depth - GlassClearDu;

        // A box with no room left in it after the aisles. Nothing on the ring is this small today; the
        // answer is an empty room rather than a half-built one, because a placer that squeezes furniture
        // into a space it does not fit is a placer that builds a desk through a wall.
        if (uB - uA < 4.0 || vB - vA < 2.0)
        {
            return Furnishing.Empty;
        }

        var fixtures = new List<Fixture>();
        var chairs = new List<Chair>();
        var solids = new List<SurfaceLayout.Wall>();
        var doors = new List<SurfaceLayout.Doorway>();

        // Where the room's own centre falls in the room's own grid — the square the A* audit stands on.
        double uCentre = (frame.ULo + frame.UHi) / 2.0, vCentre = frame.Depth / 2.0;

        var lay = new Lay(frame, uCentre, vCentre, fixtures, chairs, solids, doors);

        // THE SERVICE STRIP first, because it takes frontage away from everything else. A big suite's desks
        // stop short of it; a small one never had it and keeps its whole width.
        double workUB = uB;
        if (IsBigSuite(in room))
        {
            // …and the desks stop a GANGWAY short of it, not a joint: the cubicle doors open onto this gap
            // and a captain has to be able to step out of one.
            workUB = uB - StripDu - GangwayDu;
            ServiceStrip(lay, in room, uB - StripDu, uB, vA, vB);
        }

        switch (DressingFor(in room))
        {
            case Dressing.LongTable:
                LongTable(lay, in room, uA, workUB, vA, vB);
                break;

            case Dressing.ReadingRoom:
                double shelf = uA + (2 * CounterHalfDepthDu);
                lay.Box(Fitting.Shelving, uA, vA, shelf, vB, ReadingShelfPlate);
                Banks(lay, in room, shelf + GangwayDu, workUB, vA, vB);
                break;

            case Dressing.Reception:
                Banks(
                    lay, in room, uA, workUB,
                    Counter(lay, in room, uA, workUB, vA) + GangwayDu, vB);
                break;

            case Dressing.Plain:
                Plain(lay, in room, uA, workUB, vA, vB);
                break;

            default:
                Banks(lay, in room, uA, workUB, vA, vB);
                break;
        }

        return new Furnishing(fixtures, chairs, solids, doors);
    }

    /// <summary>Push a fitting's centre line clear of the room's own centre, on whichever side of it the
    /// fitting already was. See the class summary: the audit stands a body on that square.</summary>
    private static double ClearOfCentre(double at, double centre, double half) =>
        Math.Abs(at - centre) >= half + RoomCentreClearDu
            ? at
            : at < centre ? centre - half - RoomCentreClearDu : centre + half + RoomCentreClearDu;

    /// <summary>
    /// THE PLACER — the room's frame, its keep-clear square, and the four lists everything ends up in.
    ///
    /// <para>One object rather than six parameters threaded through every layout, and one place where the
    /// room's own centre is defended: <see cref="Box"/> refuses to lay anything across it. Every fitting in
    /// this file goes through here, so <i>"nothing is built through the square the audit stands on"</i> is a
    /// property of the placer and not of five layouts each remembering to check.</para>
    /// </summary>
    private sealed class Lay(
        Frame frame, double uCentre, double vCentre,
        List<Fixture> fixtures, List<Chair> chairs, List<SurfaceLayout.Wall> solids,
        List<SurfaceLayout.Doorway> doors)
    {
        private readonly Frame _frame = frame;

        /// <summary>The room's own grid, for the callers that need to map a point themselves.</summary>
        internal Frame Frame => _frame;

        /// <summary>Where the room's centre falls on the frontage axis.</summary>
        internal double UCentre { get; } = uCentre;

        /// <summary>…and on the depth axis.</summary>
        internal double VCentre { get; } = vCentre;

        /// <summary>Would a box laid here stand on the square the ring's audit walks to? Rectangle-to-point,
        /// so a screen between two workstations is measured the same way a boardroom table is.</summary>
        internal bool CoversTheCentre(double uLo, double vLo, double uHi, double vHi)
        {
            double du = Math.Max(Math.Max(uLo - UCentre, UCentre - uHi), 0.0);
            double dv = Math.Max(Math.Max(vLo - VCentre, VCentre - vHi), 0.0);
            // The epsilon is not decoration: ClearOfCentre shifts a fitting to EXACTLY this distance, and a
            // strict comparison against a number arrived at by subtraction rejects the very placement that
            // was computed to satisfy it about half the time.
            return Math.Sqrt((du * du) + (dv * dv)) < RoomCentreClearDu - 1e-6;
        }

        /// <summary>Lay one solid box. Returns false, having laid nothing at all, when it would cover the
        /// room's centre — so a caller that cannot shift out of the way drops the fitting rather than
        /// bricking up the room.</summary>
        internal bool Box(
            Fitting kind, double uLo, double vLo, double uHi, double vHi, string plate,
            Seating sides = Seating.None)
        {
            if (CoversTheCentre(uLo, vLo, uHi, vHi))
            {
                return false;
            }

            (double x0, double y0, double x1, double y1) = _frame.Box(uLo, vLo, uHi, vHi);
            fixtures.Add(new Fixture(kind, x0, y0, x1, y1, plate, sides));

            // A degenerate box is a SEGMENT and is laid as one — a bench, a cubicle screen, an en-suite's
            // pan. Four coincident segments where one belongs is three more things for the collision field
            // to sweep and one more thing for a degenerate-wall scan to complain about.
            if (Math.Abs(x1 - x0) < 0.001 || Math.Abs(y1 - y0) < 0.001)
            {
                solids.Add(new(x0, y0, x1, y1, true));
                return true;
            }

            solids.Add(new(x0, y0, x1, y0, true));
            solids.Add(new(x0, y1, x1, y1, true));
            solids.Add(new(x0, y0, x0, y1, true));
            solids.Add(new(x1, y0, x1, y1, true));
            return true;
        }

        /// <summary>Lay one solid segment with no fitting of its own — the sides of a cell, which are three
        /// walls of one box rather than three pieces of furniture.</summary>
        internal void Wall(double uLo, double vLo, double uHi, double vHi)
        {
            if (CoversTheCentre(uLo, vLo, uHi, vHi))
            {
                return;
            }
            (double x0, double y0, double x1, double y1) = _frame.Box(uLo, vLo, uHi, vHi);
            solids.Add(new(x0, y0, x1, y1, true));
        }

        /// <summary>Record one fitting whose solids were laid a piece at a time — a cell, whose four walls
        /// are not four sides of a rectangle because one of them has a door in it.</summary>
        internal void Note(Fixture fixture) => fixtures.Add(fixture);

        /// <summary>Publish one door. Cubicles only (#821): a real leaf the lock can land on later.</summary>
        internal void Door(double uLo, double vLo, double uHi, double vHi)
        {
            (double x0, double y0, double x1, double y1) = _frame.Box(uLo, vLo, uHi, vHi);
            doors.Add(new SurfaceLayout.Doorway(x0, y0, x1, y1));
        }

        /// <summary>Seat one person, in the room's own grid, facing the way they were told.</summary>
        internal void Chair(
            in UndergroundComplex.RingRoom room, double u, double v, (double X, double Y) facing)
        {
            (double x, double y) = _frame.At(u, v);
            chairs.Add(new Chair(chairs.Count, x, y, facing.X, facing.Y, room.Plate));
        }
    }

    /// <summary>
    /// THE BANKS — rows of workstations down the room, every one of them facing the glass.
    ///
    /// <para>A bank stops short of one pier and the next stops short of the other, so the way through the
    /// room is a weave rather than a corridor and a row is four segments rather than eight. The screens
    /// between the workstations reach BACK past the chairs, which is the whole difference between a cubicle
    /// and a table, and is the word the owner used.</para>
    /// </summary>
    private static void Banks(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        double width = uB - uA;
        if (width < 4.0 || vB - vA < DeskHalfDepthDu)
        {
            return;
        }

        // The aisle is a QUARTER of the run rather than a fixed six du wherever the run is short: a 19 du
        // end block's bank measured against the big suites' gangway is a bank with one chair at it, which
        // is the room the owner complained about with a desk added to it.
        double aisle = Math.Min(BankAisleDu, width * 0.3);

        for (int r = 0; r < MaxDeskRows; r++)
        {
            double vRow = vA + CubicleReachDu + (r * RowPitchDu);
            if (vRow + DeskHalfDepthDu > vB)
            {
                break;
            }
            vRow = ClearOfCentre(vRow, lay.VCentre, DeskHalfDepthDu);

            // What has to fit after the shift is the CHAIR, not the screen behind it: a screen that would
            // reach into the street aisle is clamped to the aisle's edge below and is simply a shorter
            // screen. A guard that demanded the screen's whole reach instead dropped every bank in the
            // block's two end suites — watched happen, and the symptom was four furnished rooms and four
            // empty ones on the same floor.
            if (vRow - ChairSetbackDu < vA - 0.001 || vRow + DeskHalfDepthDu > vB + 0.001)
            {
                continue;
            }

            // The aisle changes ends every row: the weave.
            double bankLo = (r % 2 == 0) ? uA : uA + aisle;
            double bankHi = (r % 2 == 0) ? uB - aisle : uB;

            if (!lay.Box(
                    Fitting.DeskBank, bankLo, vRow - DeskHalfDepthDu, bankHi, vRow + DeskHalfDepthDu, "",
                    Seating.OneSide))
            {
                continue;
            }

            int seats = Math.Max(MinSeatsPerBank, (int)((bankHi - bankLo) / SeatPitchDu));
            double chairV = vRow - ChairSetbackDu;
            for (int s = 0; s < seats; s++)
            {
                double u = bankLo + ((bankHi - bankLo) * (s + 0.5) / seats);
                lay.Chair(in room, u, chairV, lay.Frame.TowardTheGlass);

                // …and the screen between this workstation and the last one.
                if (s == 0)
                {
                    continue;
                }
                double screenU = bankLo + ((bankHi - bankLo) * s / seats);
                lay.Box(
                    Fitting.Partition,
                    screenU, Math.Max(vA, vRow - CubicleReachDu), screenU, vRow - DeskHalfDepthDu, "");
            }
        }
    }

    /// <summary>
    /// THE NEGOTIATION ROOM — one long table, chairs down both sides of it.
    ///
    /// <para>It runs the DEPTH of the room rather than its width, and it is deliberately off the room's own
    /// centre line: a table laid where a boardroom table goes stands on the exact square the ring's audit
    /// walks to. Off-centre is also the truer picture — the head of the table is the end at the window.</para>
    /// </summary>
    private static void LongTable(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        double reach = TableHalfWidthDu + TableChairGapDu;
        if (uB - uA < 2 * (reach + 1.0) || vB - vA < TableSeatPitchDu)
        {
            Banks(lay, in room, uA, uB, vA, vB);
            return;
        }

        double uT = Math.Clamp(
            ClearOfCentre(lay.UCentre - reach, lay.UCentre, TableHalfWidthDu),
            uA + reach, uB - reach);

        if (!lay.Box(
                Fitting.Table, uT - TableHalfWidthDu, vA, uT + TableHalfWidthDu, vB, TablePlate,
                Seating.BothSides))
        {
            Banks(lay, in room, uA, uB, vA, vB);
            return;
        }

        (double gx, double gy) = lay.Frame.TowardTheGlass;
        int seats = Math.Max(1, (int)((vB - vA) / TableSeatPitchDu));
        for (int s = 0; s < seats; s++)
        {
            double v = vA + ((vB - vA) * (s + 0.5) / seats);

            // Across the table from each other, which is what a negotiation is. They face ALONG the room's
            // own width rather than at the glass — the perpendicular of the glass normal, which is a
            // rotation of one published vector and never a fifth hand-written pair of numbers.
            lay.Chair(in room, uT - reach, v, (-gy, gx));
            lay.Chair(in room, uT + reach, v, (gy, -gx));
        }
    }

    /// <summary>
    /// THE RECEPTION COUNTER — across the room a pace inside the door, with the staff behind it looking at
    /// whoever just came through. One end left open, because a counter that reaches both piers is a wall
    /// with a story about it.
    ///
    /// <para>#827 · Seated ON ONE SIDE, which is the owner's own reading of what a counter IS — <i>the
    /// biggest table, with seats on one side only</i>. So this seats a ROW rather than a person: the same
    /// arithmetic a desk bank uses, on the same pitch, facing the other way. It is also what keeps a
    /// reception suite over <see cref="SeatsPerViewSuite"/> in a 19 du end block, where the counter and its
    /// gangway leave no depth for a bank behind it.</para>
    ///
    /// <para>Returns the far edge of what it laid, so the caller knows where the floor starts again.</para>
    /// </summary>
    private static double Counter(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA)
    {
        if (uB - uA < 4.0)
        {
            return vA;
        }

        double v = ClearOfCentre(vA + CounterStandoffDu, lay.VCentre, CounterHalfDepthDu);
        double open = Math.Min(BankAisleDu, (uB - uA) * 0.3);
        double runHi = uB - open;
        if (!lay.Box(
                Fitting.Counter, uA, v - CounterHalfDepthDu, runHi, v + CounterHalfDepthDu,
                ReceptionPlate, Seating.OneSide))
        {
            return vA;
        }

        // The seats that face the DOOR rather than the view: the whole of what a reception is.
        (double gx, double gy) = lay.Frame.TowardTheGlass;
        double seatV = v + CounterHalfDepthDu + ChairSetbackDu;
        int seats = Math.Max(MinSeatsPerBank, (int)((runHi - uA) / SeatPitchDu));
        for (int s = 0; s < seats; s++)
        {
            lay.Chair(in room, uA + ((runHi - uA) * (s + 0.5) / seats), seatV, (-gx, -gy));
        }

        return seatV;
    }

    /// <summary>
    /// THE PLAIN VERSION — shelving and a bench, and that is the whole of it.
    ///
    /// <para>What a corner office with no view and the back of house (#801) get. Owner's law is that no floor
    /// is bare, not that every floor is furnished the same: a potting shed with cubicles in it would be the
    /// plate and the room disagreeing. The bench still SEATS you, because people sit down in these rooms
    /// too.</para>
    /// </summary>
    private static void Plain(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        lay.Box(Fitting.Shelving, uA, vA, uA + (2 * CounterHalfDepthDu), vB, StorePlate);

        // The bench goes against the OTHER pier, which is the half of the room the shelving is not in and —
        // in a 20 x 12 back-of-house box — the half the room's own centre is not in either.
        double benchHi = uB;
        double benchLo = Math.Max(uA + (2 * CounterHalfDepthDu) + GangwayDu, uB - BenchDu);
        if (benchHi - benchLo < 2.0 || vB < vA)
        {
            return;
        }

        if (!lay.Box(Fitting.Bench, benchLo, vA, benchHi, vA, BenchPlate, Seating.OneSide))
        {
            return;
        }

        // Two places to sit, a pace off the plank on the room's side of it — clear of the bench's own
        // segment, which is solid exactly as the park's planks are.
        (double gx, double gy) = lay.Frame.TowardTheGlass;
        for (int e = 0; e < 2; e++)
        {
            double u = benchLo + ((benchHi - benchLo) * (e + 0.5) / 2.0);
            lay.Chair(in room, u, Math.Min(vA + ChairSetbackDu, vB), (gx, gy));
        }
    }

    /// <summary>
    /// #817 · THE SERVICE STRIP — the kitchenette, the two WC cubicles and the privacy booths, down the pier
    /// of a big suite.
    ///
    /// <para>Owner: <i>"Such a big premium office would have little kitchen and couple toilets also"</i> and
    /// <i>"maybe some privacy cabinets also"</i>. It stands against a PIER — the one pair of walls in a ring
    /// room that can never carry a door — so a strip of little boxes can run the whole depth of the suite
    /// without ever coming near a doorway's clearance. The desks give up that much frontage and keep the
    /// rest, which is why <c>Fit</c> lays this first.</para>
    ///
    /// <para>Every cell is the en-suite's own idiom (#707): a walled box with a gap in the face it opens off,
    /// and one stub of a fixture inside it so it reads as what it is at plate scale. The cubicles' gaps are
    /// PUBLISHED DOORS (#821 will lock them from the inside); the booths' are open fronts, because a phone
    /// box you can be shut into is a different feature and this issue is not it.</para>
    /// </summary>
    private static void ServiceStrip(
        Lay lay, in UndergroundComplex.RingRoom room, double uLo, double uHi, double vA, double vB)
    {
        double v = vA;

        // ── THE KITCHENETTE, in the corner nearest the door: a counter block against the pier.
        double kitchenTo = v + StripDu;
        if (kitchenTo > vB)
        {
            return;
        }
        lay.Box(Fitting.Kitchenette, uLo, v, uHi, kitchenTo, KitchenettePlate);
        v = kitchenTo + StripGapDu;

        // ── THE TWO WCs. A box, a door in the face it opens off, and a pan against the far wall.
        for (int c = 0; c < Cubicles; c++)
        {
            double to = v + CubicleDu;
            if (to > vB)
            {
                break;
            }
            Cell(lay, uLo, uHi, v, to, Fitting.Cubicle, WcPlate(c + 1), publish: true);
            v = to + StripGapDu;
        }

        // ── AND THE PRIVACY BOOTHS. One seat each, facing out of the open front the way somebody on a call
        //    sits — the point of the box is what is BEHIND you.
        (double gx, double gy) = lay.Frame.TowardTheGlass;
        for (int b = 0; b < Booths; b++)
        {
            double to = v + BoothDu;
            if (to > vB)
            {
                break;
            }
            Cell(lay, uLo, uHi, v, to, Fitting.Booth, BoothPlate(b + 1), publish: false);
            lay.Chair(in room, (uLo + uHi) / 2.0, (v + to) / 2.0, (-gx, -gy));
            v = to + StripGapDu;
        }
    }

    /// <summary>One cell of the service strip: three solid sides, and the fourth split either side of the
    /// way in. The gap is <see cref="CubicleDoorHalfDu"/> wide and centred — a bedroom-small room takes a
    /// bedroom-small door (#822).</summary>
    private static void Cell(
        Lay lay, double uLo, double uHi, double vLo, double vHi,
        Fitting kind, string plate, bool publish)
    {
        // The BOX is the fitting — laid as a fixture with no solid of its own, because a cell's four walls
        // are not four sides of a rectangle: one of them has a hole in it.
        (double x0, double y0, double x1, double y1) = lay.Frame.Box(uLo, vLo, uHi, vHi);
        if (lay.CoversTheCentre(uLo, vLo, uHi, vHi))
        {
            return;
        }

        // The two ends and the back — the back being the pier side, which is the strip's outer face.
        lay.Wall(uLo, vLo, uHi, vLo);
        lay.Wall(uLo, vHi, uHi, vHi);
        lay.Wall(uHi, vLo, uHi, vHi);

        // …and the face it opens off, in two segments with the leaf between them.
        double mid = (vLo + vHi) / 2.0;
        lay.Wall(uLo, vLo, uLo, mid - CubicleDoorHalfDu);
        lay.Wall(uLo, mid + CubicleDoorHalfDu, uLo, vHi);

        if (publish)
        {
            lay.Door(uLo, mid - CubicleDoorHalfDu, uLo, mid + CubicleDoorHalfDu);

            // The fixture, against the back wall — the en-suite's own one-segment pan, which is the whole of
            // what makes a 5 du box read as a WC on a plan.
            lay.Wall(uHi - 1.4, mid - 1.1, uHi - 1.4, mid + 1.1);
        }

        lay.Note(new Fixture(
            kind, x0, y0, x1, y1, plate,
            kind == Fitting.Booth ? Seating.OneSide : Seating.None));
    }

    // ── WHAT IT SAYS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The chair, as every seat in this game wears it.</summary>
    public const string Glyph = SittingAlone.Glyph;

    /// <summary>What a free office chair is labelled on the deck, with the verb on it (#783).</summary>
    public const string FreeChairPlate = Glyph + " AN OFFICE CHAIR — SIT DOWN";

    /// <summary>The long table in a negotiation room.</summary>
    public const string TablePlate = "🪑 THE LONG TABLE";

    /// <summary>The reading room's shelving.</summary>
    public const string ReadingShelfPlate = "📚 BOUND COPIES · DO NOT REMOVE";

    /// <summary>A plain room's shelving — the back of house and the corner offices.</summary>
    public const string StorePlate = "🗄 SHELVING";

    /// <summary>…and its bench.</summary>
    public const string BenchPlate = "🪑 A BENCH";

    /// <summary>The counter a reception is.</summary>
    public const string ReceptionPlate = "📇 THE COUNTER · APPOINTMENTS HELD";

    /// <summary>The kitchenette in a big suite. The building's own register: it says what the fixture is and
    /// nothing about what the facility is for (§13.8).</summary>
    public const string KitchenettePlate = "🍵 KITCHENETTE · STAFF ONLY";

    /// <summary>One of the two WCs.</summary>
    public static string WcPlate(int n) => $"🚻 WC {n}";

    /// <summary>One of the privacy booths.</summary>
    public static string BoothPlate(int n) => $"🕻 PRIVACY BOOTH {n}";

    /// <summary>Where you are, when you are sitting in one of these.</summary>
    public const string Setting = "A chair in one of the park-view suites";

    /// <summary>Sitting down. #783's law kept: the FIRST clause confirms the state change, and the room names
    /// itself — the plate beside the door is the only thing that tells one poured box from another, and a
    /// captain who has walked in off a corridor deserves to be told which one they are sitting in.</summary>
    /// <param name="plate">The room's own stencil.</param>
    public static string TookAChairLine(string plate) =>
        string.IsNullOrEmpty(plate)
            ? "You pull out a chair and sit down. Nothing on this floor is signed, and nobody has been at "
                + "this desk in a very long time."
            : $"You pull out a chair and sit down at somebody's desk in {plate}. The worktop is clear, the "
                + "screen is dark, and the whole wall in front of you is the garden.";

    /// <summary>Standing up off it.</summary>
    public const string StoodUpLine = "You push the chair back and stand up.";

    /// <summary>What sitting a while at a desk is FOR. You are a person at a workstation in an office you do
    /// not work in, which is a cover and not a rest.</summary>
    public const string WaitLabel = "SIT A WHILE — look like you work here";

    /// <summary>…and the way off it.</summary>
    public const string StandLabel = "Stand up";

    /// <summary>Who you are sitting as, on the docked strip.</summary>
    public const string SeatPlate = Glyph + " AT A DESK";

    /// <summary>What nothing happening at a desk in somebody else's office sounds like.</summary>
    public static readonly IReadOnlyList<string> NobodyCameLines =
    [
        "A while goes by. Somewhere down the row a chair creaks and settles, and the lamps over the garden "
        + "carry on being four in the afternoon.",
        "Nobody comes. A ventilation register above the glass ticks over to its next setting and the whole "
        + "room gets half a degree cooler.",
        "A while goes by. Somebody walks past the door without slowing down, which is the whole of what you "
        + "came here to be.",
        "Nothing. The screen in front of you wakes at a movement somewhere behind you, shows a login field, "
        + "and goes dark again.",
    ];

    /// <summary>What nothing happening sounds like, told rather than left to be inferred from a control that
    /// went quiet (#603/#757).</summary>
    /// <param name="beat">Which wait this was.</param>
    public static string NobodyCame(int beat)
    {
        IReadOnlyList<string> pool = NobodyCameLines;
        return pool[((beat % pool.Count) + pool.Count) % pool.Count];
    }

    /// <summary>
    /// THE CHAIR, as an <see cref="Encounter.Scene"/> — two moves and no third, on the machine every other
    /// seat in this game already runs on.
    ///
    /// <para>The move IDS are <see cref="SittingAlone.Wait"/> and <see cref="SittingAlone.Stand"/> and
    /// deliberately not new ones: the park bench made the same choice for the same reason (a saved game and
    /// a guard both key on the id). Only the labels and the words are the office's.</para>
    /// </summary>
    /// <param name="plate">The room's own stencil, which the opening line names.</param>
    public static Encounter.Scene TheChair(string plate) => new(
        "ring:chair",
        SeatPlate,
        Setting,
        TookAChairLine(plate),
        [
            new(SittingAlone.Wait, WaitLabel),
            new(SittingAlone.Stand, StandLabel, Says: StoodUpLine),
        ]);

    /// <summary>Where a chair's watch-scoped state is keyed from, so an office chair and a canteen top with
    /// the same ordinal on the same floor can never share a wait counter. The park bench's own reason
    /// (<see cref="ParkBenches.ApproachOrdinalBase"/>), one room along, and far enough past it that no ring
    /// will ever grow into the gap.</summary>
    public const int ApproachOrdinalBase = 5000;

    /// <summary>This chair's ordinal for the approach roll.</summary>
    /// <param name="roomNumber">The ring room it stands in.</param>
    /// <param name="chairIndex">Its ordinal in that room.</param>
    public static int ApproachOrdinal(int roomNumber, int chairIndex) =>
        ApproachOrdinalBase + (roomNumber * 64) + chairIndex;

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string s in NobodyCameLines)
        {
            yield return s;
        }
        yield return FreeChairPlate;
        yield return TablePlate;
        yield return ReadingShelfPlate;
        yield return StorePlate;
        yield return BenchPlate;
        yield return ReceptionPlate;
        yield return KitchenettePlate;
        yield return Setting;
        yield return TookAChairLine("");
        yield return StoodUpLine;
        yield return WaitLabel;
        yield return StandLabel;
        yield return SeatPlate;
    }
}
