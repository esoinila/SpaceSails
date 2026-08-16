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
public static partial class RingOffice
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
}
