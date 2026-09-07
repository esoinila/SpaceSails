using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #818 · NEVER AN EMPTY FLOOR — what is standing in an ORDINARY room, laid off that room's own walls.
///
/// <para>Owner ruling, 2026-08-11 evening, generalising #817 past the ring: <i>"Same for labs etc spaces…
/// they have chairs and desks and equipment … never ever empty floor"</i>. And then, with the kit spelled
/// out by somebody who has run these rooms for a living: <i>"chairs / tables / vacuum chambers (cough !!),
/// chemical test ventilation boxes [fume hoods], etc where do I put my test tube? Etc furnaces … let's not
/// have any empty storage space unless the space is actually an empty storage."</i></para>
///
/// <h3>The law, and its one stated escape hatch</h3>
///
/// <para><b>A room's floor is evidence of what the room is for.</b> Every carved room a captain can stand in
/// publishes at least one piece of furnishing appropriate to its plate. Bare deck is legal only where the
/// plate SAYS bare — a corridor, a street, the landing pad, a fire recess, and the one room the owner carved
/// the exception for himself: a store that is actually an empty store, which says so on its own plate
/// (<see cref="EmptyStorePlate"/>). An empty floor is a FACT about a room down here, never a default.</para>
///
/// <h3>Why this is a file and not forty lines inside the carve</h3>
///
/// <para>The same reason <see cref="RingOffice"/> is one: the obvious repair is to drop rectangles into
/// <c>AddRoomsAlong</c> at coordinates that look right on the one floor somebody was standing on, and that is
/// this repo's oldest named bug class — <b>unaudited geometry literals</b>. Not one number below is a
/// position. Every one of them is a CLEARANCE or a LENGTH, and the room is handed in, so a chamber and a
/// gallery are furnished by the same sentence and neither was measured off a screenshot.</para>
///
/// <h3>The one law that makes the guards cheap</h3>
///
/// <para>A chamber is not a ring suite: <b>any</b> of its four walls can carry an opening — the corridor door
/// in its face, a fire recess (#822) through either end wall, an en-suite's own leaf (#707) through its back.
/// So this file cannot buy safety with two fixed aisles the way the ring does. It buys it by MEASURING: every
/// fitting stands <see cref="WallClearDu"/> inside a wall, and the free stretches of that inset line are what
/// is left after every published opening has claimed <see cref="OpeningClearDu"/> either side of itself. A
/// wall whose openings eat all of it simply carries nothing, and the next wall round is tried. That is why
/// nothing here has to know which side a chamber's door is on.</para>
///
/// <para>…and <b>the room's own centre stays standable</b>, which is the law #817 learned the expensive way:
/// the A* audit walks to <c>room.X, room.Y</c>, so a fitting laid across that square does not report as
/// furniture, it reports as <i>"something was built through this room"</i> on every floor in the game.</para>
///
/// <h3>Every SOLID is a segment, and that is deliberate</h3>
///
/// <para>Each fitting puts exactly ONE wall segment on the floor, laid on the wall-clear line, collided with
/// exactly as the en-suite's pan and the ring's bench are. That is what keeps this feature affordable: a
/// facility floor carries a few hundred segments and Lab 45 says the sightline is O(walls). Four segments per
/// fitting in fifteen hundred rooms would be a frame budget spent on drawing cupboards.</para>
///
/// <para>#869 · <b>What is DRAWN is a box.</b> The desks and the benches grew a depth
/// (<see cref="FittingDepthDu"/>) because #868 found out the hard way what a fixture without one looks like:
/// the owner stood in a room whose bench was a single stroke and said <i>"the bench is a line"</i>, three
/// paces from a run of shelving he called clear as furniture goes. So a fitting's box reaches into the room
/// for the pen and its solid stays on the line for the body, and the two are published separately for that
/// reason. Nothing about the collision field, the walkability audits or the sightline cost changed when the
/// desks got their 90 cm.</para>
///
/// <para>Pure, in the shape <see cref="RingOffice.Fit"/> is pure: handed a room the generator carved and the
/// holes it cut, it answers what is in it. It has no clock and no opinion about where a room goes.</para>
///
/// <para>#251 · This is the opening file of a five-part family, and the other four are named for the
/// question they answer: <c>.Kit</c> (what kind of room this is — the plate first, then the floor's
/// department, then the site's register), <c>.Prose</c> (what every fitting SAYS, and the stool the
/// captain sits down on), <c>.Walls</c> (the piece, the wall run, and which stretches of wall are free),
/// and <c>.Fit</c> (the placement itself and the kit each trade gets).</para>
///
/// <para>The family declares no static field: every number here is a <c>const</c> and every derived
/// length a property, so no initializer chain exists to be broken by the cut. See
/// <c>NoPartialClassSpreadsItsStaticFieldsTests</c>. No member is renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class ChamberFitting
{
    // ── THE CLEARANCES ────────────────────────────────────────────────────────────────────────────────

    /// <summary>How far inside a wall a fitting stands. The ring's own pier gap
    /// (<see cref="RingOffice.PierClearDu"/>) and not a second number: a bench against a wall is a bench
    /// against a wall, whichever room it is in.</summary>
    public const double WallClearDu = RingOffice.PierClearDu;

    /// <summary>How much of that line an opening keeps clear either side of itself — the ring's own door
    /// clearance (<see cref="RingOffice.DoorClearDu"/>), which every guard in this house about furniture and
    /// doors is already written against. A doorway, a fire recess and an en-suite leaf are all measured with
    /// it, because a body coming through any of them needs the same square.</summary>
    public const double OpeningClearDu = RingOffice.DoorClearDu;

    /// <summary>How much clear floor is kept round the room's own centre — the square the A* audit stands a
    /// body on to say the room was reached at all. <see cref="RingOffice.RoomCentreClearDu"/>, for the reason
    /// that constant exists.</summary>
    public const double CentreClearDu = RingOffice.RoomCentreClearDu;

    /// <summary>The shortest stretch of wall worth standing anything against. Below this a fitting is a
    /// stub, and a stub is worse than an honest bare wall.</summary>
    public const double MinFittingDu = 2.0;

    /// <summary>How far a seat stands off the worktop it belongs to. <see cref="RingOffice.ChairSetbackDu"/>
    /// — one number for every chair in the building.</summary>
    public const double SeatSetbackDu = RingOffice.ChairSetbackDu;

    /// <summary>
    /// #869 · HOW DEEP A DESK IS DOWN HERE — and why the depth grows INWARD and nothing else moves.
    ///
    /// <para>Owner, 2026-08-13, from his own electric desk: <i>"my table (electric) is about 160 cm wide,
    /// about 90 cm deep"</i>. Ninety centimetres is <see cref="RingOffice.WorktopDepthDu"/>, the number #868
    /// gave the ring's own worktop, and it is the same number here because a desk is a fact about a BODY
    /// rather than about a module.</para>
    ///
    /// <para>The box is grown from the wall-clear line <b>into the room</b>, so the fitting's BACK stays
    /// exactly where the segment always stood. That is not tidiness, it is the whole safety of this change:
    /// every clearance in this file was measured to that line, and a depth grown the other way would push a
    /// bench 0.65 du nearer the very doorway it was laid clear of. The SOLID stays one segment on that same
    /// line, so not one wall in the building moves and the collision field, the sightline cost and every
    /// walkability audit are untouched. The depth is a PICTURE (<c>DeckPlan.FurnitureSpot</c>), and #868's
    /// finding was that a picture is the thing furniture down here never had.</para>
    /// </summary>
    public const double FittingDepthDu = RingOffice.WorktopDepthDu;

    // ── THE LENGTHS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>#869 · How much frontage ONE PERSON'S DESK gets — <see cref="RingOffice.WorktopRunDu"/>, the
    /// owner's own 160 cm. The one length in this file not derived from the room module, because a desk is
    /// not a fact about the room it is standing in.</summary>
    public static double DeskRunDu => RingOffice.WorktopRunDu;

    /// <summary>How long a laboratory bench runs, at most. The room module's own width
    /// (<see cref="UndergroundComplex.RoomWidthDu"/>) less the two clearances it stands between, so a bench
    /// grows with the chamber and is never measured off one floor's screenshot.</summary>
    public static double BenchRunDu => UndergroundComplex.RoomWidthDu - (2 * WallClearDu) - MinFittingDu;

    /// <summary>How long a run of racking is. The same, because a bay of shelving and a bench are the same
    /// object at this scale: a run of solid against a wall.</summary>
    public static double RackRunDu => BenchRunDu;

    /// <summary>A machine, a fume hood, a furnace, a vacuum vessel, a filing bank — the SHORT fittings, the
    /// ones you stand at rather than sit along. Half a bench, so the two read as different objects on a plan
    /// without a second number being invented.</summary>
    public static double UnitRunDu => BenchRunDu / 2.0;

    /// <summary>How many fittings one chamber gets, at most. A cap and not a target, and the number is
    /// small on purpose — see the class summary on segments. Three is enough for a room to answer <i>what
    /// is this for</i> at a glance and cheap enough to do it in fifteen hundred rooms.</summary>
    public const int MaxFittingsPerRoom = 3;
}
