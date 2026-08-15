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

    /// <summary>#868 · The least floor a seat keeps between itself and the solid it is a seat AT, where the
    /// room is too tight for the whole <see cref="ChairSetbackDu"/>. A body's radius and a hand — the setback
    /// is what a chair at a desk WANTS; this is what a seat must have before it stops being a seat and starts
    /// being a coordinate inside a bench, which is the trap #820's snap would put the captain in.</summary>
    public const double SeatClearDu = 1.2;

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

    /// <summary>
    /// #868 · HOW DEEP A BENCH IS — and the whole of why this number had to exist.
    ///
    /// <para>Owner, standing in a cold room on the far band, 2026-08-13: <i>"The bench is a line."</i> It
    /// was, literally: <see cref="Plain"/> laid it as a DEGENERATE box, which <see cref="Lay.Box"/> honestly
    /// turns into one segment, and one segment is one stroke on a plan. Beside it the SHELVING — a real
    /// rectangle — read as furniture at a glance, and the owner said so in the same breath:
    /// <i>"The Shelving is clear as furniture goes."</i> One room, the positive control and the negative
    /// control, three paces apart.</para>
    ///
    /// <para>So a bench is as thick as a run of shelving is (<see cref="CounterHalfDepthDu"/>, doubled), for
    /// the reason the two are one object at this scale: a length of solid you put things on or sit on. There
    /// is no second opinion about how deep a plan fixture is drawn.</para>
    /// </summary>
    public static double BenchDepthDu => 2 * CounterHalfDepthDu;

    /// <summary>
    /// #868/#869 · HOW DEEP A WORKTOP IS, at the size a person actually is.
    ///
    /// <para>Owner's own fix for the desk that was narrated and never drawn: <i>"could the table just be a
    /// different color rectangle in front of the chair, so arms (and papers) could rest on it?"</i> — and
    /// #869's sizing, which is the human-true desk this building has never had: 2.3 du of frontage per
    /// person, 1.3 du deep. A desk is the one fixture down here whose size is a fact about a BODY rather
    /// than about the module, so it is the one fixture measured in bodies.</para>
    /// </summary>
    public const double WorktopDepthDu = 1.3;

    /// <summary>#869 · …and how much of it one person gets. See <see cref="WorktopDepthDu"/>.</summary>
    public const double WorktopRunDu = 2.3;

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

    /// <summary>The stub of wall left either side of a cell's opening. A jamb and not a wall: a cell is a
    /// box you step into, and the whole of it is the door.</summary>
    public const double CellJambDu = 0.4;

    /// <summary>
    /// How long a cell of the strip is — a WC cubicle or a privacy booth.
    ///
    /// <para>DERIVED FROM THE BUILDING'S OWN DOOR (<see cref="UndergroundComplex.DoorHalf"/>) plus a jamb
    /// either side, and that is not tidiness. The first cut of this feature gave the cubicles a leaf half the
    /// building's width, on the reasoning that a bedroom-small room takes a bedroom-small door — and #724's
    /// jamb law went red on 612 approaches across every site in the game. That law is <i>a captain one
    /// body-width off a doorway cut, pressing only the axis through it, must pass</i>, and it is measured out
    /// to <c>DoorHalf + a radius</c> because that is where the body stops overlapping the opening. A narrower
    /// leaf makes that sentence false about itself: the guard walks at a hole that is not there. One door
    /// width in this building; the cell is sized to it rather than the other way round.</para>
    /// </summary>
    public static double CellDu => (2 * UndergroundComplex.DoorHalf) + (2 * CellJambDu);

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
    /// <para>It is FIVE and not three for a second reason found the same way: the strip's cells open onto
    /// this gangway, and a captain walking at a cubicle door from a pace or two out was starting behind the
    /// end of a desk bank and pinning there. It is a corridor, so it is corridor-width — the same aisle the
    /// street and the glass keep.</para>
    public const double GangwayDu = 5.0;

    /// <summary>How many WC cubicles a big suite gets. The owner said <i>"couple toilets"</i> and a couple is
    /// two.</summary>
    public const int Cubicles = 2;

    /// <summary>#821 · How many cubicles the block's PUBLIC washroom gets. Owner, standing in the park:
    /// <i>"let's add toilets there.. we might want to hide from guards in one toilet cubicle we lock from
    /// inside"</i> — and a row you can choose from is what makes the hide a decision rather than a
    /// corridor. Three, because two is a pair and a public washroom is a row.</summary>
    public const int PublicCubicles = 3;

    /// <summary>#821 · The narrowest frontage that can hold a public washroom: the two piers, the cubicle
    /// terrace against one of them, a corridor between, and the basin run against the other. Derived rather
    /// than typed, so a terrace that grows a du takes the gate with it.</summary>
    public static double WashroomMinFrontageDu =>
        (2 * PierClearDu) + StripDu + GangwayDu + (2 * CounterHalfDepthDu);

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

        // ── #818 · THE REST OF THE BUILDING'S FITTINGS ────────────────────────────────────────────────
        //
        // Owner, generalising #817 past the ring: "Same for labs etc spaces… they have chairs and desks and
        // equipment … never ever empty floor", and then the kit itself, from somebody who has run these
        // rooms: "chairs / tables / vacuum chambers (cough !!), chemical test ventilation boxes [fume
        // hoods], etc where do I put my test tube? Etc furnaces".
        //
        // They live HERE and not in a second enum beside ChamberFitting for the reason a Fixture is one
        // record: a renderer that draws a piece of furniture, a guard that counts one, and a plate that
        // names one should read ONE vocabulary. This list stopped being the ring's the moment the law went
        // building-wide; the ring is only where it was first written down.

        /// <summary>A laboratory bench, with the glassware racked along it. The answer to <i>where do I put
        /// my test tube?</i></summary>
        LabBench,

        /// <summary>A chemical test ventilation box, against a wall. The owner's own phrase for it.</summary>
        FumeHood,

        /// <summary>A vacuum vessel. Asked for by name, with a cough.</summary>
        VacuumChamber,

        /// <summary>A furnace.</summary>
        Furnace,

        /// <summary>A run of bays in a long store. Shelving's heavy cousin, and its own kind because a store
        /// that reported <see cref="Shelving"/> would be a warehouse pretending to be a reading room.</summary>
        Racking,

        /// <summary>A machine bolted to a plant floor.</summary>
        Machinery,

        /// <summary>A bank of filing in an administration chamber.</summary>
        FilingCabinet,

        /// <summary>#828 · THE SECURE DISPOSAL — the shredder-class machine at the far end of a premium
        /// suite's service strip. Owner: <i>"One thing the good offices would also have is a safe paper
        /// disposal trashes… that visually destroy the notes as we watch… a more secure disposal than
        /// restaurant trash."</i>
        ///
        /// <para>A FITTING here and a <see cref="RipAndBin.Tier.SecureDisposal"/> to the verb: one box on
        /// the plan, plated once, and the third rung of a ladder that already existed. It is the tier the
        /// service strip is — the kitchenette, the staff WCs, the privacy booths — which is why it stands
        /// here and not by every lift.</para></summary>
        SecureDisposal,
    }

    /// <summary>
    /// #821 · ONE CUBICLE, PUBLISHED AS THE THING THE LOCK LANDS ON.
    ///
    /// <para>A <see cref="Fitting.Cubicle"/> fixture is the BOX and the floor's doorway list holds the LEAF,
    /// and until this record existed nothing said which leaf belonged to which box. That pairing is the
    /// whole of #821 — a lock is a fact about one door of one cell — so it is published by the placer that
    /// laid both rather than recovered afterwards by a client measuring midpoints, which is §13.15 and the
    /// shape of bug this ground has twice paid for.</para>
    /// </summary>
    /// <param name="Index">Its ordinal in the room, so a watch-scoped key can name it.</param>
    /// <param name="X0">Left edge of the box, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Door">Its own leaf — the very doorway the floor published, not a copy of one.</param>
    /// <param name="SeatX">Where a body goes when it sits down in here. The middle of the box: clear of the
    /// pan against the back wall and clear of the leaf, so the snap (#820's law, kept) can put the captain
    /// ON this coordinate and standing up leaves them exactly there.</param>
    /// <param name="SeatY">The same.</param>
    /// <param name="StepX">Where somebody stands OUTSIDE it — a door's clearance off the leaf, on the room's
    /// side. Where a guard knocks from, and published rather than derived for that reason: a man who walked
    /// to a coordinate this file did not choose would be standing somewhere nobody laid out.</param>
    /// <param name="StepY">The same.</param>
    /// <param name="Plate">What is stencilled on it while it is free.</param>
    public readonly record struct Stall(
        int Index, double X0, double Y0, double X1, double Y1,
        SurfaceLayout.Doorway Door, double SeatX, double SeatY, double StepX, double StepY, string Plate)
    {
        /// <summary>The middle of it.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>Where the leaf's own console hangs — the middle of the doorway, which is the square a
        /// captain is standing on when they reach behind them and turn the catch.</summary>
        public double DoorX => (Door.X1 + Door.X2) / 2.0;

        /// <summary>The same.</summary>
        public double DoorY => (Door.Y1 + Door.Y2) / 2.0;

        /// <summary>Is the captain in it? The box the three walls were laid on, and nothing else — the same
        /// arithmetic <see cref="UndergroundComplex.RingRoom.Contains"/> uses one scale up.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>
        /// #821 · Is a body of <paramref name="bodyRadius"/> standing in here AND CLEAR OF THE LEAF — which
        /// is what "inside" has to mean for the catch, as opposed to for the hide.
        ///
        /// <para>A shut cubicle is a WALL SEGMENT laid on the leaf, so a captain who turned the catch while
        /// standing in the opening would be standing inside the thing they just made. Nothing traps the dot
        /// on this ground (the ring chair's own law, and the oldest bug this project has), so the catch asks
        /// for a body's clearance and the refusal that follows is the ordinary one: step in first.</para>
        /// </summary>
        public bool ClearOfTheLeaf(double x, double y, double bodyRadius)
        {
            if (!Contains(x, y))
            {
                return false;
            }
            double dx = x - DoorX, dy = y - DoorY;
            return (dx * dx) + (dy * dy) >= bodyRadius * bodyRadius;
        }

        /// <summary>#821 · Where the captain is put down when they stand up again — the seat, because a
        /// cubicle's pan is not a solid on the plan and the seat is the square you were already standing on
        /// to press [E]. The ring chair's own law (<see cref="Chair.StandAt"/>), said about a smaller
        /// room.</summary>
        public (double X, double Y) StandAt => (SeatX, SeatY);
    }

    /// <summary>
    /// #821 · ONE WASHBASIN. Owner: <i>"also a function to wash hands with some film noir comment at the
    /// end."</i>
    ///
    /// <para>The RUN is one <see cref="Fitting.Counter"/> — a basin run is a worktop with holes in it, and
    /// calling it what it is keeps every guard about worktops true about this room without a second kind to
    /// teach them. These are the taps along it, and they exist so the press has somewhere to be that is not
    /// the middle of a six-du counter.</para>
    /// </summary>
    /// <param name="Index">Its ordinal along the run.</param>
    /// <param name="X">Where a body stands to use it.</param>
    /// <param name="Y">The same.</param>
    public readonly record struct Basin(int Index, double X, double Y);

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
    /// <param name="Stalls">#821 · The cubicles, each paired with its OWN leaf. See <see cref="Stall"/>: the
    /// lock is a fact about one door of one cell, and the placer that laid both is the only thing that can
    /// say which is which.</param>
    /// <param name="Basins">#821 · The taps along a public washroom's basin run.</param>
    public readonly record struct Furnishing(
        IReadOnlyList<Fixture> Fixtures,
        IReadOnlyList<Chair> Chairs,
        IReadOnlyList<SurfaceLayout.Wall> Solids,
        IReadOnlyList<SurfaceLayout.Doorway> Doors,
        IReadOnlyList<Stall>? Stalls = null,
        IReadOnlyList<Basin>? Basins = null)
    {
        /// <summary>Nothing at all — what a room too small to stand a desk in gets, and what nothing in the
        /// ring actually gets today. Kept so a degenerate box returns an empty answer rather than a
        /// half-built one.</summary>
        public static Furnishing Empty { get; } = new([], [], [], []);

        /// <summary>The cubicles, never null.</summary>
        public IReadOnlyList<Stall> Cells => Stalls ?? [];

        /// <summary>The taps, never null.</summary>
        public IReadOnlyList<Basin> Taps => Basins ?? [];
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

        /// <summary>#821 · A terrace of cubicles down one pier, a basin run down the other, and a bench to
        /// wait on. The block's own public washroom.</summary>
        Washroom,
    }

    /// <summary>#821 · Is this the block's public washroom? Off the PLATE, which is the only thing that says
    /// what a room is for — the same one question <see cref="DressingFor"/> asks, published so a guard, a
    /// renderer and the size gate cannot each answer it their own way.</summary>
    public static bool IsWashroom(in UndergroundComplex.RingRoom room) =>
        room.Plate.Contains("WASHROOM", StringComparison.Ordinal);

    /// <summary>What this room is dressed as. The back of house keeps #801's plates and its own character —
    /// a potting shed is not an office — and a corner room has no view to sit facing, so both take the plain
    /// version.</summary>
    public static Dressing DressingFor(in UndergroundComplex.RingRoom room)
    {
        // #821 · THE WASHROOM IS ASKED FIRST, and before the view clause, because it is the one dressing
        // that is not an office at all: a room of cubicles with desks in it would be the plate and the floor
        // disagreeing, which is the bug class this switch was written to be incapable of.
        if (IsWashroom(in room))
        {
            return Dressing.Washroom;
        }
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
    /// <para>#821 · …and never the block's own washroom, however wide it is. The service strip is the tier a
    /// PREMIUM OFFICE earns — a kitchenette, two staff WCs and the privacy booths — and a public washroom
    /// that grew a kitchenette would be the amenity ladder handing the same room both rungs.</para>
    public static bool IsBigSuite(in UndergroundComplex.RingRoom room) =>
        room.HasView && room.Side != UndergroundComplex.RingSide.Far && !IsWashroom(in room)
        && FrontageOf(room) >= BigSuiteDu;

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

        /// <summary>#868 · Which way the room's FRONTAGE runs, as a unit vector — the +u axis of the grid
        /// everything in this file is laid in, in the surface's own coordinates.
        ///
        /// <para>Published for the same reason <see cref="TowardTheGlass"/> is: a layout that wants to face
        /// somebody ACROSS the room rather than at the window would otherwise write its own pair of numbers
        /// per side, and four hand-written pairs is four chances to get a sign wrong on one of the ring's
        /// four bands. The back-of-house set (<see cref="Plain"/>) is laid entirely along this axis, because
        /// that band is one chamber module deep and has no other axis to be laid along.</para></summary>
        internal (double X, double Y) AlongTheFrontage => Horizontal ? (1.0, 0.0) : (0.0, 1.0);

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
        var stalls = new List<Stall>();
        var basins = new List<Basin>();

        // Where the room's own centre falls in the room's own grid — the square the A* audit stands on.
        double uCentre = (frame.ULo + frame.UHi) / 2.0, vCentre = frame.Depth / 2.0;

        var lay = new Lay(frame, uCentre, vCentre, fixtures, chairs, solids, doors, stalls, basins);

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
            case Dressing.Washroom:
                Washroom(lay, in room, uA, uB, vA, vB);
                break;

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

        return new Furnishing(fixtures, chairs, solids, doors, stalls, basins);
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
        List<SurfaceLayout.Doorway> doors, List<Stall> stalls, List<Basin> basins)
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

            // ── #883 · AND IT IS SOLID, not four rails round a hollow ─────────────────────────────────
            //
            // This laid the four sides and stopped, which is the #874 fault said one building along: a desk
            // bank is 28.8 × 2.0 du and a kitchenette is 6 × 6, and the inside of one is standable floor no
            // route on the floor can ever end in. Measured before it was touched — every square inside every
            // ring fitting in the game, and the flood from the suite's own centre reached NOT ONE of them:
            // 748 sealed squares on a single luna B1, in six kinds of furniture, on every ring in the game.
            //
            // AddSolidMass is #586's own answer to exactly this and has been the furniture's answer since
            // #874 made it public. The OUTLINE IS UNCHANGED — same four segments, same order, same IsHull —
            // so nothing on any plan moves and the sightline keeps exactly what it stopped before; the
            // inside simply stops being a place.
            SurfaceLayout.AddSolidMass(solids, x0, y0, x1, y1, hull: true);
            return true;
        }

        /// <summary>
        /// #828 · Lay one fitting whose inside is not a place — the same box as <see cref="Box"/>, filled.
        ///
        /// <para><see cref="SurfaceLayout.AddSolidMass"/> is this codebase's one word for SOLID (#586's
        /// monolith, #874's growing beds, #798's own bins), and a fitting that is also a
        /// <see cref="RipAndBin.Bin"/> is held to the bin law: the box the eye sees is the box the boots
        /// meet, and there is no sealed square in the middle of it. Returns false, having laid nothing, on
        /// the room's own centre — <see cref="Box"/>'s rule, unchanged, because a fitting that bricked up
        /// the square the A* audit stands on would be worse for being solid.</para>
        /// </summary>
        internal bool SolidBox(
            Fitting kind, double uLo, double vLo, double uHi, double vHi, string plate)
        {
            if (CoversTheCentre(uLo, vLo, uHi, vHi))
            {
                return false;
            }

            (double x0, double y0, double x1, double y1) = _frame.Box(uLo, vLo, uHi, vHi);
            fixtures.Add(new Fixture(kind, x0, y0, x1, y1, plate));

            // hatchDrawn: false — the strokes through the middle are COLLISION and nothing else here. #868
            // hands the deck this fitting's published box and FILLS it (HiveInterior.Furnish), so a mass
            // drawn as masonry as well would be one machine wearing two textures. "Same collision, no
            // joins" — SurfaceLayout's own words for exactly this case.
            SurfaceLayout.AddSolidMass(solids, x0, y0, x1, y1, true, hatchDrawn: false);
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

        /// <summary>Publish one door. Cubicles only (#821): a real leaf the lock can land on later. Returns
        /// the leaf, so the cell that cut it can keep it — the pairing of box to door is #821's whole
        /// question and it is answered where both were laid.</summary>
        internal SurfaceLayout.Doorway Door(double uLo, double vLo, double uHi, double vHi)
        {
            (double x0, double y0, double x1, double y1) = _frame.Box(uLo, vLo, uHi, vHi);
            var leaf = new SurfaceLayout.Doorway(x0, y0, x1, y1);
            doors.Add(leaf);
            return leaf;
        }

        /// <summary>#821 · Record one cubicle: its box, its own leaf, the square you sit on inside it and the
        /// square somebody knocks from outside it. All four in the room's own grid, mapped here.</summary>
        internal void Cubicle(
            in Fixture box, in SurfaceLayout.Doorway leaf, double seatU, double seatV,
            double stepU, double stepV, string plate)
        {
            (double sx, double sy) = _frame.At(seatU, seatV);
            (double kx, double ky) = _frame.At(stepU, stepV);
            stalls.Add(new Stall(
                stalls.Count, box.X0, box.Y0, box.X1, box.Y1, leaf, sx, sy, kx, ky, plate));
        }

        /// <summary>#821 · Record one tap along a basin run, in the room's own grid.</summary>
        internal void Tap(double u, double v)
        {
            (double x, double y) = _frame.At(u, v);
            basins.Add(new Basin(basins.Count, x, y));
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

        int seats = Math.Max(1, (int)((vB - vA) / TableSeatPitchDu));

        // Across the table from each other, which is what a negotiation is. They face ALONG the room's own
        // frontage rather than at the glass, off the axis the grid is laid in
        // (<see cref="Frame.AlongTheFrontage"/>).
        //
        // #868 · IT WAS A ROTATION OF THE GLASS NORMAL AND IT WAS WRONG ON ONE BAND IN FOUR. The rotation
        // `(-gy, gx)` gives +u on the near, far and west sides and −u on the EAST, because that side's grid
        // is the one whose depth axis runs backwards (<c>Frame.At</c>: <c>_x1 - v</c>). So every negotiation
        // room on the east band seated four people facing away from their own table, and nothing said so
        // until #868's facing guard asked the question — 48 seats across the sweep, on a floor plan where
        // both chairs and table are drawn and neither is drawn with a front.
        (double ax, double ay) = lay.Frame.AlongTheFrontage;
        for (int s = 0; s < seats; s++)
        {
            double v = vA + ((vB - vA) * (s + 0.5) / seats);
            lay.Chair(in room, uT - reach, v, (ax, ay));
            lay.Chair(in room, uT + reach, v, (-ax, -ay));
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
    /// THE PLAIN VERSION — shelving, a bench, and the worktop the bench is a bench AT.
    ///
    /// <para>What a corner office with no view and the back of house (#801) get. Owner's law is that no floor
    /// is bare, not that every floor is furnished the same: a potting shed with cubicles in it would be the
    /// plate and the room disagreeing. The bench still SEATS you, because people sit down in these rooms
    /// too.</para>
    ///
    /// <h3>#868 · What the owner found in one of these, and the three things wrong with it</h3>
    ///
    /// <para>He sat down at a chair in <c>❄ COLD ROOM · TO CANTEEN 1</c> on the back street of B1 and read
    /// the room off the plan: <i>"The graphics kind of does not show there being a table"</i> ·
    /// <i>"The bench is a line"</i> · <i>"and too far to use as a bench"</i> · and, as the positive control,
    /// <i>"The Shelving is clear as furniture goes."</i></para>
    ///
    /// <para>Every one of those was TRUE, and all three were this method's:</para>
    ///
    /// <list type="number">
    /// <item><b>There was no table.</b> Not undrawn — ABSENT. A plain room published two chairs and not one
    /// worktop, while the sit line told the captain the worktop in front of them was clear. The renderer had
    /// nothing to draw because Core had stood nothing there.</item>
    /// <item><b>The bench was a degenerate box</b>, so it was one solid segment, so it was one stroke. See
    /// <see cref="BenchDepthDu"/>.</item>
    /// <item><b>The chairs faced the glass with the bench BEHIND them</b> and nothing in front, a pace out
    /// into the middle of the floor. A seat with its back to the only fixture in reach is the "too far to use
    /// as a bench" the owner was looking at.</item>
    /// </list>
    ///
    /// <para>The fix is his own, quoted: <i>"could the table just be a different color rectangle in front of
    /// the chair, so arms (and papers) could rest on it?"</i> — a SET, laid so the three pieces explain each
    /// other. The bench stands against the far pier, a body's setback out from it is the seat, and a setback
    /// past that is the worktop, so the dot at the chair has the bench behind it and the slab in front of it
    /// and neither is a stroke.</para>
    ///
    /// <h3>Why it is all laid across the FRONTAGE</h3>
    ///
    /// <para>Measured, not preferred. The back-of-house band is one chamber module deep
    /// (<see cref="UndergroundComplex.RoomHeightDu"/> = 12 du) and gives <see cref="StreetClearDu"/> to the
    /// street aisle and <see cref="GlassClearDu"/> to the walkway at the glass, which leaves TWO du of depth
    /// to furnish. A set stacked across the depth does not fit in the very room the owner was sitting in —
    /// the frontage is where the sixteen to forty du are, so the set is laid along it and grows into it.</para>
    /// </summary>
    private static void Plain(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        double shelfHi = uA + (2 * CounterHalfDepthDu);
        lay.Box(Fitting.Shelving, uA, vA, shelfHi, vB, StorePlate);

        if (vB - vA < 0.001)
        {
            return;
        }

        // How LONG the set is. A bench's own published length, or the whole band where the band is shallower
        // than that — which is the far ring, where it is two du. It is anchored at the street aisle so that a
        // deep corner office gets a bench somebody could walk in and sit on rather than a forty-du plank.
        double setV1 = Math.Min(vB, vA + BenchDu);

        // ── THE SET, measured back from the far pier: bench, a body, worktop.
        double benchLo = uB - BenchDepthDu;
        double seatU = benchLo - ChairSetbackDu;
        double workHi = seatU - ChairSetbackDu;
        double workLo = workHi - WorktopDepthDu;

        // Where the set may start at all: past the shelving and the gangway a body walks between two
        // fittings, and clear of the square the ring's A* audit stands on. Both are published numbers of this
        // file's own, and neither is a coordinate.
        double floorAt = Math.Max(shelfHi + GangwayDu, lay.UCentre + RoomCentreClearDu);
        bool bench = workLo >= floorAt;

        if (!bench)
        {
            // A frontage too short to hold the bench as well. The WORKTOP is the piece that stays, because a
            // chair without one is the very lie this issue was opened about: the set loses its bench rather
            // than its reason to have a chair at all.
            workLo = floorAt;
            workHi = workLo + WorktopDepthDu;
            seatU = workHi + ChairSetbackDu;
        }

        if (seatU > uB + 0.001)
        {
            return;   // nowhere in this room to sit that is not a pier. The shelving, and honest bare floor.
        }

        if (!lay.Box(Fitting.Counter, workLo, vA, workHi, setV1, WorktopPlate, Seating.OneSide))
        {
            return;
        }

        if (bench)
        {
            lay.Box(Fitting.Bench, benchLo, vA, uB, setV1, BenchPlate, Seating.OneSide);
        }

        // …and the seat, FACING THE WORKTOP — back down the frontage, off the one published axis
        // (<see cref="Frame.AlongTheFrontage"/>) rather than a fifth hand-written pair of numbers. It is the
        // whole of the third finding: a chair in a back room looks at the thing it works at, and a chair
        // that looked at a window it does not have is what put the garden in a cold store's narration.
        (double ax, double ay) = lay.Frame.AlongTheFrontage;
        lay.Chair(in room, seatU, (vA + setV1) / 2.0, (-ax, -ay));
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
    ///
    /// <h3>The cells ABUT, and that is load-bearing</h3>
    ///
    /// <para>They were laid with a joint between them first, so the strip would read as a row of separate
    /// boxes, and #724's jamb law went red on every site: a captain walking at the edge of a cubicle door was
    /// funnelled AWAY from it into the du and a half between two cells — an opening as far as the sidestep
    /// can see, and a dead slot as far as a body is concerned. A terrace has no such slot. Past a cell's end
    /// wall is the next cell's door, which is a place a captain can actually go, and the pier between two
    /// leaves is two jambs wide — narrower than a body, so there is nowhere on this face to stand that is
    /// not in front of a door.</para>
    /// </summary>
    private static void ServiceStrip(
        Lay lay, in UndergroundComplex.RingRoom room, double uLo, double uHi, double vA, double vB)
    {
        double v = vA;

        // ── THE KITCHENETTE, in the corner nearest the door: a counter block against the pier. Its far
        //    edge is the first cell's own end wall, which is why no cell below lays one at its near end.
        double kitchenTo = v + StripDu;
        if (kitchenTo > vB)
        {
            return;
        }
        lay.Box(Fitting.Kitchenette, uLo, v, uHi, kitchenTo, KitchenettePlate);
        v = kitchenTo;

        // ── THE TWO WCs. A box, a door in the face it opens off, and a pan against the far wall.
        for (int c = 0; c < Cubicles; c++)
        {
            double to = v + CellDu;
            if (to > vB)
            {
                break;
            }
            Cell(lay, uLo, uHi, v, to, Fitting.Cubicle, WcPlate(c + 1), publish: true);
            v = to;
        }

        // ── AND THE PRIVACY BOOTHS. One seat each, facing out of the open front the way somebody on a call
        //    sits — the point of the box is what is BEHIND you.
        (double gx, double gy) = lay.Frame.TowardTheGlass;
        for (int b = 0; b < Booths; b++)
        {
            double to = v + CellDu;
            if (to > vB)
            {
                break;
            }
            Cell(lay, uLo, uHi, v, to, Fitting.Booth, BoothPlate(b + 1), publish: false);
            lay.Chair(in room, (uLo + uHi) / 2.0, (v + to) / 2.0, (-gx, -gy));
            v = to;
        }

        // ── #828 · AND THE SECURE DISPOSAL, at the end of the strip ──────────────────────────────────────
        //
        // Owner: "One thing the good offices would also have is a safe paper disposal trashes… that visually
        // destroy the notes as we watch… a more secure disposal than restaurant trash."
        //
        // LAST, and that is the whole of the placement rule: the strip is laid in the order the tier was
        // asked for, and a fitting added to the head of it would silently push a WC or a booth off the end
        // of a room that has fitted both since #817. It takes the depth that was already spare, against the
        // same pier, in the same terrace — so a captain stepping out of the last booth is standing at it.
        //
        // No door, no seat: it is a machine you stand over. What makes it a BIN is CarveBins reading this
        // fixture back off the finished room (§13.15's rule — the placer that needs to see the whole floor
        // runs last), so the box a body collides with, the plate a captain reads and the bucket the verb
        // feeds are one rectangle.
        //
        // …and SOLID, which is the one way this box differs from every other fitting in the file. #798's own
        // law for a bin is that the drawn box is the walked box and the inside of it is not a place (#874,
        // #586): four rails round a six-by-three machine leave a standable pocket with no way into it, and
        // the ONE fixture on this ring that is also a published RipAndBin.Bin is the one whose guard says so
        // out loud. The rest of the ring's fittings are still four rails apiece, which is a real fault and a
        // whole-ring one — it belongs to an issue of its own, not to a lane that is fitting one machine.
        double disposalTo = v + SecureDisposalDu;
        if (disposalTo <= vB)
        {
            lay.SolidBox(Fitting.SecureDisposal, uLo, v, uHi, disposalTo, SecureDisposalPlate);
        }
    }

    /// <summary>One cell of the service strip: three solid sides, and the fourth split either side of the
    /// way in. The opening is the BUILDING'S own door, centred, with <see cref="CellJambDu"/> either side —
    /// see <see cref="CellDu"/> for why it may not be anything narrower.</summary>
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

        // The FAR end and the back — the back being the pier side, which is the strip's outer face. The
        // near end is the previous cell's far end and is never laid twice: two segments where the eye reads
        // one is this repo's own named way of ending up with two answers about one wall.
        lay.Wall(uLo, vHi, uHi, vHi);
        lay.Wall(uHi, vLo, uHi, vHi);

        // …and the face it opens off, in two segments with the leaf between them.
        double mid = (vLo + vHi) / 2.0;
        lay.Wall(uLo, vLo, uLo, mid - UndergroundComplex.DoorHalf);
        lay.Wall(uLo, mid + UndergroundComplex.DoorHalf, uLo, vHi);

        var box = new Fixture(
            kind, x0, y0, x1, y1, plate,
            kind == Fitting.Booth ? Seating.OneSide : Seating.None);

        if (publish)
        {
            SurfaceLayout.Doorway leaf = lay.Door(
                uLo, mid - UndergroundComplex.DoorHalf, uLo, mid + UndergroundComplex.DoorHalf);

            // The fixture, against the back wall — the en-suite's own one-segment pan, which is the whole of
            // what makes a 5 du box read as a WC on a plan.
            lay.Wall(uHi - PanInsetDu, mid - PanHalfDu, uHi - PanInsetDu, mid + PanHalfDu);

            // #821 · …and the cell as the thing a LOCK lands on: the box, its own leaf, the square you sit
            // on in the middle of it and the square somebody knocks from a door's clearance outside it.
            lay.Cubicle(
                in box, in leaf, (uLo + uHi) / 2.0, mid, uLo - DoorClearDu, mid, plate);
        }

        lay.Note(box);
    }

    /// <summary>How far in from a cell's back wall the pan stands. The en-suite's own inset (#707), named
    /// now that a second room lays one — a number typed twice is two answers waiting to disagree.</summary>
    public const double PanInsetDu = 1.4;

    /// <summary>…and half the length of it.</summary>
    public const double PanHalfDu = 1.1;

    /// <summary>
    /// #821 · THE BLOCK'S PUBLIC WASHROOM — a terrace of cubicles down one pier, a basin run down the other,
    /// and a bench between them.
    ///
    /// <para>Owner, standing in the park on the evening of 2026-08-11: <i>"let's add toilets there.. we
    /// might want to hide from guards in one toilet cubicle we lock from inside :-D"</i>, and then
    /// <i>"also a function to wash hands with some film noir comment at the end."</i></para>
    ///
    /// <para>It is the SERVICE STRIP'S OWN IDIOM and deliberately not a second one: the same
    /// <see cref="Cell"/> lays a public cubicle and a big suite's staff WC, so the door the lock lands on is
    /// one kind of door and there is exactly one place its geometry can be got wrong. What is different is
    /// only what stands opposite — a run of basins rather than a kitchenette, because this is a room the
    /// whole floor walks into rather than a corner of somebody's office.</para>
    ///
    /// <para>The terrace stands against a PIER, which is the one pair of walls in a ring room that can never
    /// carry a door, so a row of little boxes runs down the suite without ever coming near a doorway's
    /// clearance — <see cref="ServiceStrip"/>'s reason, unchanged, and the reason the cells still ABUT.</para>
    /// </summary>
    private static void Washroom(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        // ── THE TERRACE, down the FAR pier — the same face of the room the service strip stands against,
        //    and that is not a preference. A cell opens off its uLo face (see Cell), so a terrace laid at
        //    the NEAR pier opens INTO the pier: the leaves are a hand's breadth off a solid wall and #724's
        //    jamb law goes red on 1512 approaches across every clandestine site in the game — "400 presses
        //    left the captain at -127.35, still on the near side of the wall at -125.00", which is a captain
        //    in the room next door walking at a door that is not there. Watched happen inside this issue.
        //
        //    It stops when the room runs out of DEPTH rather than when the count does: a washroom with two
        //    cubicles in it is a washroom, and a cell laid past the glass aisle would be a cubicle in the
        //    window.
        double uStripLo = Math.Max(uA, uB - StripDu);
        double v = vA;
        for (int c = 0; c < PublicCubicles; c++)
        {
            double to = v + CellDu;
            if (to > vB)
            {
                break;
            }
            Cell(lay, uStripLo, uB, v, to, Fitting.Cubicle, PublicWcPlate(c + 1), publish: true);
            v = to;
        }

        // ── THE BASIN RUN, against the NEAR pier and a gangway clear of the terrace's own leaves. One
        //    counter and not one fixture per tap: a basin run IS a worktop with holes in it (see Basin), and
        //    four boxes where the eye reads one length of porcelain is four things for the collision field
        //    to sweep and three more for a degenerate-wall scan to complain about.
        double runHi = Math.Min(uA + (2 * CounterHalfDepthDu), uStripLo - GangwayDu);
        if (runHi <= uA + 0.001 || vB - vA < BasinPitchDu)
        {
            return;
        }

        double runV1 = Math.Min(vB, vA + (BasinsPerRun * BasinPitchDu));
        if (!lay.Box(Fitting.Counter, uA, vA, runHi, runV1, BasinRunPlate, Seating.OneSide))
        {
            return;
        }

        // The taps, spaced along it, standing a body clear of the porcelain on the room's side.
        int taps = Math.Max(1, (int)((runV1 - vA) / BasinPitchDu));
        for (int t = 0; t < taps; t++)
        {
            lay.Tap(runHi + CounterHalfDepthDu, vA + ((runV1 - vA) * (t + 0.5) / taps));
        }

        // ── AND SOMEWHERE TO WAIT. A bench against the SAME PIER, past the end of the run — the plain
        //    room's own plank (see Plain), stood on its side because this pier already has porcelain on the
        //    first half of it. Two places on it, because the ring's furnishing law is that a captain can sit
        //    down in more than one place in a room, and a public washroom with nowhere to wait is a corridor
        //    with taps in it.
        //
        //    #868 · …and it is a BOX and no longer a line, for the reason the plain room's is
        //    (BenchDepthDu): the owner read one of these off a plan and said "the bench is a line", and it
        //    was — one degenerate box, one segment, one stroke. A bench is as deep as a run of shelving is.
        double benchV0 = runV1 + GangwayDu, benchV1 = Math.Min(vB, benchV0 + BenchDu);

        // How far this pier's band reaches before the terrace's own gangway starts — the same line the basin
        // run stops at, asked once. The bench is as deep as that band can spare after a seat has taken its
        // clearance out of it, so the narrowest washroom on the ring gets a thin slab rather than a stroke.
        double column = uStripLo - GangwayDu;
        double benchHi = Math.Min(uA + BenchDepthDu, column - SeatClearDu);
        if (benchV1 - benchV0 < 2.0 || benchHi - uA < CounterHalfDepthDu / 2.0)
        {
            return;
        }
        if (!lay.Box(Fitting.Bench, uA, benchV0, benchHi, benchV1, BenchPlate, Seating.OneSide))
        {
            return;
        }

        // A pace off the plank on the room's side of it — away from the pier, which is the direction the
        // basin run's own taps face, taken off ONE published vector rather than a second pair of numbers.
        // Measured off the bench's own FAR face now that it has one, and never past the porcelain's line.
        //
        // #868 · …and it faces ALONG THE FRONTAGE, off the published axis rather than a rotation of the
        // glass normal — the same one-line sign fault the negotiation table had, in the same shape, on the
        // same band: `(-gy, gx)` is +u on three sides of the ring and −u on the east, so a washroom on that
        // band sat people facing into the pier their own bench is bolted to.
        double seatU = Math.Min(benchHi + ChairSetbackDu, column);
        (double ax, double ay) = lay.Frame.AlongTheFrontage;
        for (int e = 0; e < 2; e++)
        {
            lay.Chair(in room, seatU, benchV0 + ((benchV1 - benchV0) * (e + 0.5) / 2.0), (ax, ay));
        }
    }

    /// <summary>#821 · How many basins a run holds. Enough that a queue is a queue and not a line — and it
    /// is a CAP, because the run is cut to whatever depth the room has left after the terrace.</summary>
    public const int BasinsPerRun = 4;

    /// <summary>#821 · Between two people at the basin run. The EN-SUITE'S OWN PAN, end to end
    /// (<see cref="PanHalfDu"/>), and a jamb between them: a basin is a fixture of the same scale as the
    /// thing in a cubicle, because on this plan it is one. Deliberately not <see cref="SeatPitchDu"/> —
    /// that is the width of a person SITTING at a worktop, which at eight du would have laid four taps down
    /// thirty-three du of porcelain and read as a swimming bath.</summary>
    public static double BasinPitchDu => (2 * PanHalfDu) + CellJambDu;

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

    /// <summary>
    /// #868 · …and the surface the bench is a bench AT.
    ///
    /// <para>Owner, sealing the fix: <i>"I think table should be similar just say table."</i> — one idiom for
    /// all plan furniture, the shelving's own recipe (a filled rectangle with a plate on it), and the
    /// plainest possible noun on the plate.</para>
    ///
    /// <para>It was written WORKTOP for one round, off a parenthetical in the issue's draft text
    /// (<i>"or the kit's honest noun — WORKTOP where the narration says worktop"</i>) — which was the crew
    /// reading a DRAFT over a RULING. The seal is the last word. Nothing is left disagreeing: the sit line
    /// keeps <i>the worktop is clear</i>, because a table's top is its worktop.</para>
    /// </summary>
    public const string WorktopPlate = "🪑 TABLE";

    /// <summary>The counter a reception is.</summary>
    public const string ReceptionPlate = "📇 THE COUNTER · APPOINTMENTS HELD";

    /// <summary>The kitchenette in a big suite. The building's own register: it says what the fixture is and
    /// nothing about what the facility is for (§13.8).</summary>
    public const string KitchenettePlate = "🍵 KITCHENETTE · STAFF ONLY";

    /// <summary>One of the two WCs.</summary>
    public static string WcPlate(int n) => $"🚻 WC {n}";

    /// <summary>One of the privacy booths.</summary>
    public static string BoothPlate(int n) => $"🕻 PRIVACY BOOTH {n}";

    /// <summary>#828 · How deep the secure disposal stands down the service strip. A machine and not a room:
    /// deliberately shallower than a cell (<see cref="CellDu"/>), so it fits in the depth every big suite on
    /// every site already had spare after the kitchenette, the two WCs and both booths.</summary>
    public const double SecureDisposalDu = 3.0;

    /// <summary>#828 · What is stencilled on it — <see cref="RipAndBin.PlateFor"/>'s own word for the rung,
    /// never a second one. The plan's plate and the verb's plate are the same string because they are the
    /// same object seen from two ends of the building.</summary>
    public static string SecureDisposalPlate => RipAndBin.PlateFor(RipAndBin.Tier.SecureDisposal);

    /// <summary>#821 · One of the public washroom's cubicles, with the verb on it (#783) — a door you can
    /// shut is the whole of what this room offers and the plate should say so.</summary>
    public static string PublicWcPlate(int n) => $"🚻 CUBICLE {n} · STEP IN";

    /// <summary>#821 · The basin run. The building's own register — it names the fixture and nothing about
    /// what the facility is for (§13.8) — and it carries the verb, because [E] here washes your hands.</summary>
    public const string BasinRunPlate = "🚰 THE BASIN RUN · WASH YOUR HANDS";

    /// <summary>Where you are, when you are sitting in one of these.</summary>
    public const string Setting = "A chair in one of the park-view suites";

    /// <summary>Sitting down. #783's law kept: the FIRST clause confirms the state change, and the room names
    /// itself — the plate beside the door is the only thing that tells one poured box from another, and a
    /// captain who has walked in off a corridor deserves to be told which one they are sitting in.</summary>
    /// <param name="plate">The room's own stencil.</param>
    /// <param name="garden">#868 · Is this a room that LOOKS AT THE GREEN — see
    /// <see cref="LooksAtTheGarden"/>. The clause about the wall of glass is reachable through this
    /// parameter and nowhere else, which is the whole of the third finding on this issue.</param>
    /// <param name="desk">#868 · Is there actually a worktop in front of this seat — see
    /// <see cref="WorksAtASurface"/>. Every sentence that says <i>the worktop is clear</i> is reachable
    /// through this parameter and nowhere else, so a bench outside a row of cubicles cannot be narrated as
    /// somebody's desk.</param>
    public static string TookAChairLine(string plate, bool garden, bool desk)
    {
        if (!desk)
        {
            return WaitingSeatLine(plate);
        }
        if (string.IsNullOrEmpty(plate))
        {
            return "You pull out a chair and sit down. Nothing on this floor is signed, and nobody has "
                + "been at this desk in a very long time.";
        }
        if (garden)
        {
            return $"You pull out a chair and sit down at somebody's desk in {plate}. The worktop is "
                + "clear, the screen is dark, and the whole wall in front of you is the garden.";
        }
        return IsColdStore(plate) ? ColdStoreChairLine : BackRoomChairLine(plate);
    }

    /// <summary>
    /// #868 · SITTING DOWN IN THE COLD ROOM — the owner's own sentence, kept verbatim.
    ///
    /// <para>He wrote it standing in <c>❄ COLD ROOM · TO CANTEEN 1</c>, having just been told by the game
    /// that the whole wall in front of him was the garden: <i>"You pull out a chair and sit down at
    /// somebody's desk in the cold store. The worktop is clear, the air bites, and the door is the only
    /// view."</i> Not a word of it is the crew's, and it is a const rather than a branch inside the general
    /// line so that it cannot be reworded by somebody tidying a format string.</para>
    /// </summary>
    public const string ColdStoreChairLine =
        "You pull out a chair and sit down at somebody's desk in the cold store. The worktop is clear, the "
        + "air bites, and the door is the only view.";

    /// <summary>#868 · Is this room the one the owner sat in? Off the PLATE, which is the only thing that
    /// says what a room is — the same one question <see cref="IsWashroom"/> asks, published so the sit line
    /// and any guard about it read one sentence.</summary>
    public static bool IsColdStore(string plate) =>
        plate.Contains("COLD ROOM", StringComparison.Ordinal)
        || plate.Contains("COLD STORE", StringComparison.Ordinal);

    /// <summary>
    /// #868 · …and sitting down in every OTHER room with no garden in front of it — the rest of the back of
    /// house, the corner offices, the block's own washroom.
    ///
    /// <para><b>NEW PROSE, and flagged as such for the owner to bless.</b> The ruling asked for <i>"one
    /// neutral non-view line for other windowless rooms — in the same voice"</i>: the cold room's own line
    /// is his and this one is the crew's, written to the same three-clause shape and naming no garden. It
    /// keeps <i>the worktop is clear</i>, because that clause is now TRUE — there is a worktop in front of
    /// the chair (see <see cref="Plain"/>), which there was not when this issue was opened.</para>
    /// </summary>
    /// <param name="plate">The room's own stencil.</param>
    public static string BackRoomChairLine(string plate) =>
        $"You pull out a chair and sit down at somebody's desk in {plate}. The worktop is clear, the room "
        + "is quiet, and there is nothing in front of you but the wall it was poured against.";

    /// <summary>
    /// #868 · DOES A CHAIR IN THIS ROOM LOOK AT THE GARDEN?
    ///
    /// <para>Owner, on being told it did while sitting in a cold store: the view-suite line was firing in the
    /// back of house. The clause is a fact about a DRESSING and not about a wall — the far band's park face
    /// is glass with the gravel gate cut through it, so a test on the geometry alone would keep saying yes in
    /// the very room the complaint came from. What is actually being asked is <i>is this a room somebody
    /// rented for the aspect</i>, and <see cref="DressingFor"/> is the one place in the building that
    /// answers it: everything it dresses PLAIN is a corner office or a back room, and the washroom is not a
    /// room anybody sits in to look out of.</para>
    /// </summary>
    public static bool LooksAtTheGarden(in UndergroundComplex.RingRoom room) =>
        DressingFor(in room) is not (Dressing.Plain or Dressing.Washroom);

    /// <summary>
    /// #868 · …AND SITTING SOMEWHERE THAT IS NOT A DESK AT ALL — the bench outside the block's cubicles, the
    /// seat inside a privacy booth.
    ///
    /// <para><b>NEW PROSE, flagged for the owner to bless.</b> It exists because fixing the garden clause
    /// exposed the same fault one room along: the ring publishes seats that are places to WAIT rather than
    /// places to work, and every one of them was being told <i>the worktop is clear</i> in a room with no
    /// worktop in it. The clause is now reachable only where a surface is actually within reach of the seat
    /// (<see cref="WorksAtASurface"/>), so this is what is left to say — and what is left to say about
    /// waiting in somebody else's building is #603's own note: being uninteresting is the cover.</para>
    /// </summary>
    /// <param name="plate">The room's own stencil.</param>
    public static string WaitingSeatLine(string plate) =>
        string.IsNullOrEmpty(plate)
            ? "You sit down and wait. Nobody has been through here in a long time, and nobody comes through "
                + "now."
            : $"You sit down in {plate} with nothing in front of you, and wait. Nobody looks twice at "
                + "somebody who is only waiting, which is most of what this seat is for.";

    /// <summary>#868 · Is this the kind of fitting somebody WORKS AT — the thing a chair is a lie without?
    /// Asked of the published KIND and never of a plate's wording, so a fixture renamed tomorrow keeps its
    /// answer.</summary>
    public static bool IsWorkSurface(Fitting kind) =>
        kind is Fitting.DeskBank or Fitting.Table or Fitting.Counter or Fitting.LabBench;

    /// <summary>
    /// #868 · HAS THIS SEAT GOT A WORKTOP IN FRONT OF IT?
    ///
    /// <para>Measured off the published boxes, because that is the only honest way to ask it. The owner sat
    /// in a chair that told him the worktop in front of him was clear, in a room that published no worktop at
    /// all — the sentence was reading the PLATE and the plate says nothing about what is standing on a floor.
    /// A seat is at a desk when a desk is within the setback a chair is laid at
    /// (<see cref="ChairSetbackDu"/>), which is the same number the placer used to lay it there.</para>
    /// </summary>
    public static bool WorksAtASurface(in UndergroundComplex.RingRoom room, in Chair chair)
    {
        foreach (Fixture f in room.Furniture)
        {
            if (!IsWorkSurface(f.Kind))
            {
                continue;
            }
            double dx = Math.Max(Math.Max(f.X0 - chair.X, chair.X - f.X1), 0.0);
            double dy = Math.Max(Math.Max(f.Y0 - chair.Y, chair.Y - f.Y1), 0.0);
            if (Math.Sqrt((dx * dx) + (dy * dy)) <= ChairSetbackDu + 0.01)
            {
                return true;
            }
        }
        return false;
    }

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
    /// <param name="garden">#868 · See <see cref="LooksAtTheGarden"/>.</param>
    /// <param name="desk">#868 · See <see cref="WorksAtASurface"/>.</param>
    public static Encounter.Scene TheChair(string plate, bool garden, bool desk) => new(
        "ring:chair",
        SeatPlate,
        Setting,
        TookAChairLine(plate, garden, desk),
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
        yield return WcPlate(1);
        yield return BoothPlate(1);
        yield return PublicWcPlate(1);
        yield return BasinRunPlate;
        yield return Setting;
        yield return WorktopPlate;
        yield return TookAChairLine("", true, true);
        yield return TookAChairLine("A BACK ROOM", true, true);
        yield return ColdStoreChairLine;
        yield return BackRoomChairLine("A BACK ROOM");
        yield return WaitingSeatLine("");
        yield return WaitingSeatLine("A BACK ROOM");
        yield return StoodUpLine;
        yield return WaitLabel;
        yield return StandLabel;
        yield return SeatPlate;
    }
}
