using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #813 · WHAT A ROOM IN THE BLOCK IS — <see cref="RoomKind"/>, the six things a carved space can be, and
/// <see cref="Room"/>, the record every placer in the family hands back. It is the block's vocabulary and
/// not its arithmetic: nothing here measures anything, and the kind is carried as a FACT about the room
/// rather than inferred from its dimensions, so a guard telling a chamber from a suite is reading a
/// decision instead of a coincidence. Split out of <c>UndergroundComplex.Block.cs</c> under #251 with no
/// member renamed, re-scoped or re-ordered.
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>#822 · What KIND of carved space this is. Not decoration: a law about how you leave a room
    /// is often a law about one kind of room, and a guard that had to tell a chamber from a cabinet by its
    /// dimensions would be reading a coincidence.</summary>
    public enum RoomKind
    {
        /// <summary>A room off a rib — the module the whole building is made of.</summary>
        Chamber,

        /// <summary>A suite in the ring round the park, back of house included (#813/#817).</summary>
        RingSuite,

        /// <summary>The cantina hall (#751).</summary>
        Hall,

        /// <summary>A negotiating cabinet down the hall's outer wall (#751).</summary>
        Cabinet,

        /// <summary>A WC cubicle in the block's washroom (#821). Bedroom-small, by design.</summary>
        Cubicle,

        /// <summary>An en-suite cell hung off a principal chamber (#707). Bedroom-small, by design.</summary>
        Cell,

        /// <summary>#775 · A glass box in the middle of a landscape floor — one of the meeting rooms off the
        /// open core. Its own kind rather than a <see cref="Cabinet"/>, because a cabinet is a booking made
        /// at a bar's counter down a hall's outer wall and this is the room a department holds its
        /// meetings in.</summary>
        MeetingRoom,
    }

    /// <summary>
    /// #822 · ONE CARVED ROOM, WITH ITS OWN WAYS OUT — the list the fire code is swept over.
    ///
    /// <para>The building has published a room's CENTRE since #707 and its walls since the beginning, and
    /// neither of those can answer the owner's question. "How do you leave this room" is a fact about a
    /// BOX and the holes in it, and until this record existed the only way to ask it was to guess a room's
    /// extent by firing rays at the wall list — which walks straight out through the very doorway it is
    /// trying to count. So every placer down here hands over the box it laid and the gaps it cut, exactly
    /// as <see cref="Hall.Openings"/> and <see cref="RingRoom.Doors"/> already do, and the sweep reads one
    /// list nobody has to reconstruct.</para>
    ///
    /// <para><b>A way is a GAP, not a leaf.</b> The galleries of the found band hang no doors at all
    /// (#677 — an imported leaf down there would say somebody fitted it), so a room's ways are counted off
    /// the holes its own carver left in its own walls and never off <see cref="FloorPlan.Doorways"/>. A
    /// locked chamber is not in this list at all: it is not a space a captain can stand in, and the fire
    /// code is about the ones who are standing in them.</para>
    /// </summary>
    /// <param name="X0">Left edge of the box the walls were laid on.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Plate">What is stencilled on it, empty in the found band and on the plainer fittings.</param>
    /// <param name="Ways">Every hole in its walls a body can pass — street doors, corridor mouths, the gate
    /// onto the green, the door through to the recess next door. Never the locked ones.</param>
    /// <param name="Fittings">#818 · What is standing ON THE FLOOR of it. The owner's law — <i>"never ever
    /// empty floor"</i> — is a law about a list, and a law about a list nobody keeps is a law nobody can
    /// fail. Appended, so every caller that builds one positionally still means the same room. See
    /// <see cref="ChamberFitting"/> for the chambers and <see cref="RingOffice"/> for the suites: the two
    /// placers are different and the LIST is one, which is what lets a single sweep walk the building.</param>
    /// <param name="Chairs">#818 · Every seat in it, in the room's own order. Empty in a room nobody sits
    /// down in, which is a true statement about a store rather than a missing one.</param>
    public readonly record struct Room(
        double X0, double Y0, double X1, double Y1, string Plate,
        IReadOnlyList<SurfaceLayout.Doorway> Ways,
        RoomKind Kind = RoomKind.Chamber,
        IReadOnlyList<RingOffice.Fixture>? Fittings = null,
        IReadOnlyList<RingOffice.Chair>? Chairs = null)
    {
        /// <summary>#818 · The furniture, never null — a caller asking "is this floor bare" must not have to
        /// tell an empty list from a missing one.</summary>
        public IReadOnlyList<RingOffice.Fixture> Furniture => Fittings ?? [];

        /// <summary>#818 · The seats, never null. Same reason.</summary>
        public IReadOnlyList<RingOffice.Chair> Seats => Chairs ?? [];

        /// <summary>#818 · Does this room show what it is for? The owner's law asked of the room itself, so
        /// the carve, the renderer and the guard read one sentence.</summary>
        public bool IsFurnished => Furniture.Count > 0;

        /// <summary>The middle of it — where its plate is read from and where the audit walks to.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>How wide it is.</summary>
        public double WidthDu => X1 - X0;

        /// <summary>How deep it is.</summary>
        public double DepthDu => Y1 - Y0;

        /// <summary>Its longest wall — the number <see cref="FireCodeSmallRoomDu"/> is stated in.</summary>
        public double LongestSideDu => Math.Max(WidthDu, DepthDu);

        /// <summary>How much floor it has.</summary>
        public double FloorDu2 => WidthDu * DepthDu;

        /// <summary>#822 · Is it let off with one door? A space you can cross in two paces and whose one
        /// door you are never out of reach of — a WC cubicle, a privacy booth, an en-suite cell. The
        /// exemption #821's lock mechanic stands on.</summary>
        public bool BedroomSmall => LongestSideDu <= FireCodeSmallRoomDu;

        /// <summary>How many ways out of it there are — the number the fire code is stated in.</summary>
        public int Exits => Ways.Count;

        /// <summary>#822 · Does this room satisfy the standing law? Asked here rather than in the guard, so
        /// the carve and the sweep are reading one sentence.</summary>
        public bool MeetsFireCode => BedroomSmall || Exits >= FireCodeMinExits;

        /// <summary>Is the captain in it? The box the walls were laid on, and nothing else.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }
}
