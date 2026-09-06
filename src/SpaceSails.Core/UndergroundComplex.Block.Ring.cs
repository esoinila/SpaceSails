using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #813 · WHAT A RING ROOM IS — <see cref="RingSide"/>, the four faces of the block, and
/// <see cref="RingRoom"/>, one room's span on one of them together with which way its doors and its glass
/// point. The shapes only; the arithmetic that cuts a side into these lives in
/// <c>UndergroundComplex.Block.Carve.cs</c>. Split out of <c>UndergroundComplex.Block.cs</c> under #251
/// with no member renamed, re-scoped or re-ordered.
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>#813 · Which side of the park a ring room faces it from. Near is the spine's side.</summary>
    public enum RingSide
    {
        /// <summary>The spine's side — the premium band, and the hall's.</summary>
        Near,

        /// <summary>The back street's side — the back of house.</summary>
        Far,

        /// <summary>The block's west end.</summary>
        West,

        /// <summary>The block's east end.</summary>
        East,
    }

    /// <summary>
    /// #813 · ONE ROOM ON THE RING — the room with the view, published with both of its walls.
    ///
    /// <para>Two walls are what make it one of these rather than an ordinary chamber, and they are on
    /// opposite sides of it: <see cref="View"/> is the park-facing wall, which is glass; <see cref="Door"/>
    /// is the way in, which is on a street. That is the owner's <i>"view side to the park, service side to
    /// the corridor"</i> as a record rather than as an arrangement two placers happen to agree on.</para>
    ///
    /// <para>Published for the reason <see cref="Hall.Openings"/> and <see cref="Park.Ways"/> are: "every
    /// park-facing wall is used" is a law about a list, and a law about a list nobody keeps is a law nobody
    /// can fail.</para>
    /// </summary>
    /// <param name="Number">1-based, in the order the ring was laid: near, far, west, east.</param>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Side">Which of the park's four walls this room is one of.</param>
    /// <param name="Door">The FIRST way in, cut in the street's face. #817 · It used to be the only one and
    /// this parameter used to say so; the owner overrode that from inside a landscape office with one leaf
    /// in it. Every caller that means "the way in" still means this one, and every caller that means "all of
    /// them" says <see cref="RingRoom.Doors"/>.</param>
    /// <param name="View">The park-facing wall, as the segment it was built as — null on a CORNER room,
    /// which stands past the end of the park's own wall and therefore has nothing to look at. A corner
    /// office with no view is the amenity gradient (#775) drawn on the plan.</param>
    /// <param name="Gate">The door in the park-facing wall, where this room has one. #817 · EVERY ROOM WITH
    /// A VIEW HAS ONE — owner's ruling, live: a premium office on a garden gets a door to the garden and not
    /// only a window at it. The far band's back of house has kept #801's own way in off the gravel since it
    /// was carved, and this is the same door, granted to the rooms that were paying for the aspect. Null on
    /// a corner room, which has no park in front of it — and null on the HALL, whose glass is still never a
    /// door: that rule was always about the bar's window wall and never about the ring's.</param>
    /// <param name="Plate">What is stencilled beside the FIRST street door.</param>
    /// <param name="Ways">#817 · Every street door, in the order they were cut along the frontage. Null on a
    /// room built before the count scaled, which is why <see cref="Doors"/> falls back to
    /// <paramref name="Door"/> rather than to an empty list — an empty list would make "every ring room
    /// opens onto a street" vacuously true, which is the one way a law about a list can fail silently.</param>
    /// <param name="Fittings">#817 · What is standing on the floor of it. See <see cref="RingOffice"/>.</param>
    /// <param name="Chairs">#817 · Every seat in it, in the room's own order. Chairs face the GLASS, because
    /// the view is what the room rents for and the furniture should agree.</param>
    /// <param name="Cells">#821 · Every WC cubicle in it, each paired with its OWN published leaf — the
    /// staff pair in a big suite's service strip and the row in the block's public washroom, off one list
    /// because they are one kind of door. See <see cref="RingOffice.Stall"/>: the lock is a fact about one
    /// door of one cell, and nothing outside the placer that laid both can say which is which.</param>
    /// <param name="Taps">#821 · The basins along a public washroom's run.</param>
    public readonly record struct RingRoom(
        int Number, double X0, double Y0, double X1, double Y1, RingSide Side,
        SurfaceLayout.Doorway Door, SurfaceLayout.Wall? View, SurfaceLayout.Doorway? Gate,
        string Plate,
        IReadOnlyList<SurfaceLayout.Doorway>? Ways = null,
        IReadOnlyList<RingOffice.Fixture>? Fittings = null,
        IReadOnlyList<RingOffice.Chair>? Chairs = null,
        IReadOnlyList<RingOffice.Stall>? Cells = null,
        IReadOnlyList<RingOffice.Basin>? Taps = null,
        // #775 · Whether its street door is a leaf that will not open. FALSE on every room of the block
        // round the park, which is the public floor and has no closed rooms on it; true on a share of a
        // landscape floor's back band, which is the department's own — see CarveRing. Appended and never
        // inserted, for the reason every optional on this record is appended.
        bool Shut = false)
    {
        /// <summary>#821 · The cubicles, never null.</summary>
        public IReadOnlyList<RingOffice.Stall> Cubicles => Cells ?? [];

        /// <summary>#821 · The basins, never null.</summary>
        public IReadOnlyList<RingOffice.Basin> Basins => Taps ?? [];

        /// <summary>#817 · EVERY STREET DOOR. Owner: <i>"bigger spaces must have much more doors."</i>
        /// Published for the reason <see cref="Hall.Openings"/> is: a law about how a room is entered cannot
        /// be written against a list nobody keeps.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Doors => Ways ?? [Door];

        /// <summary>#817 · The furniture, never null.</summary>
        public IReadOnlyList<RingOffice.Fixture> Furniture => Fittings ?? [];

        /// <summary>#817 · The seats, never null.</summary>
        public IReadOnlyList<RingOffice.Chair> Seats => Chairs ?? [];

        /// <summary>#822 · EVERY WAY OUT OF IT — the street doors and, where it has one, the gate onto the
        /// green. The list <see cref="Room.Ways"/> is filled from, so a suite's egress is one fact told
        /// once rather than a count here and a list somewhere else.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> WaysOut =>
            Shut ? [] : Gate is { } onto ? [.. Doors, onto] : Doors;

        /// <summary>#822 · How many ways out of it there are altogether — the street doors and the gate onto
        /// the green. The number the fire code is stated in, asked of the room's own published lists.</summary>
        public int Exits => WaysOut.Count;

        /// <summary>The middle of it — where its plate is read from and where the audit walks to.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>Is the captain in it? The box the walls were laid on, and nothing else.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>How much floor it has.</summary>
        public double FloorDu2 => (X1 - X0) * (Y1 - Y0);

        /// <summary>Does it look at the green? False on the four corner rooms, and that is a true statement
        /// about them rather than a missing one.</summary>
        public bool HasView => View is not null;
    }
}
