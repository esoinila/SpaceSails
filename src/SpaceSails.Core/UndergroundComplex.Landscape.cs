using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #775 · LANDSCAPE OFFICES, NOT A DORMITORY ───────────────────────────────────────────────────────
    //
    // Owner, streamed during the B1 playtest of 2026-08-08, two beats:
    //
    //   1. "All the office desks — we should have landscape-style offices on B1 and below floors, with lots
    //      of meeting rooms. The current layout is like a dormitory of midship windowless cabins."
    //   2. "The work done there is lab work and office work related to that lab work, so the space should
    //      look like that."
    //
    // …and the umbrella principle he filed three addenda later: THE LAYOUT MATCHES ITS FUNCTION.
    //
    // WHAT WAS WRONG. Every floor but one is cut the same way: a spine, four cross corridors, and a column
    // of 15 x 12 du cells down each face of each of them. That module is the whole building, and it is the
    // right module for a store, a plant room and a sleeping cabin — the owner's own correction, filed the
    // same evening, is that THE CABINS STAY. It is the wrong module for the floor an ADMINISTRATION or a
    // LABORATORIES plate hangs over, because the work on those floors is forty people at desks who can see
    // each other, and a row of identical shut boxes says the opposite of that.
    //
    // WHAT THIS IS. The building already knows how to cut a landscape floor: #813's block — a ring of large
    // rooms with their own streets round a middle, furnished by RingOffice into banks of desks with sight
    // lines and a gangway past everybody. It was cut on exactly ONE floor of each site because the predicate
    // that decides where it goes is a question about a PARK (HasParkBlock), and B1 is the only floor with a
    // garden in it. So the ring never had anything to do with the garden: the garden had to do with the
    // ring, and the audit on #938 said so — "a single predicate limits ring-cut large rooms to one floor;
    // the ring cutter and the furnishers already exist."
    //
    // The predicate is split in two here. HasBlockOn asks the question the CARVE cares about — does this
    // floor take the block — and HasParkBlock goes on asking the question the PARK cares about, unchanged,
    // on the one floor that has one. What stands in the middle of the block on the other floors is the thing
    // an office puts in its own core: a row of glass meeting rooms with a promenade all round them
    // (CarveMeetingCore).
    //
    // WHY THE DEPARTMENT AND NOT THE AIR. #608's fine-motor law — "any room that would house like office
    // work would be pressurized by that constraint" — is enforced today by Haul.NeedsAir, and it is a law
    // about what a captain FINDS down here: no roster, no file on anybody, out of a floor that does not
    // breathe. It is not a law about how a floor was CUT, because the cutting happened while the whole
    // building breathed and a compressor that failed forty years later did not move a wall. The shipped
    // building already agrees with that reading: luna B2 is a LABORATORIES floor that holds no pressure and
    // has stools bolted to its benches. Reading pressure here would also have deleted the owner's second
    // beat outright — a branch office's department cycle is eight long and its pressurised floors are every
    // fourth, so LABORATORIES never once lands on one.

    /// <summary>
    /// #775 · THE DEPARTMENTS WHOSE WORK IS DESK WORK — the floors that get a landscape office.
    ///
    /// <para>Written as a list of department plates taken verbatim out of <see cref="DepartmentsFor"/>
    /// rather than as a keyword match, for the reason <see cref="PrincipalPlates"/> is: a match on "OFFICE"
    /// would silently collect THE WINTER OFFICE and THE BERTH OFFICE and then, the day somebody writes a
    /// department called THE MORTUARY OFFICE, that too. A rule that selects by accident is this repo's fifth
    /// bug class wearing a clever hat. Every entry is proved to be a department this building actually hangs
    /// by <c>EveryDeskWorkDepartmentIsOneThisBuildingActuallyHangs</c>.</para>
    ///
    /// <para><b>What is deliberately NOT here.</b> LONG STORAGE, PLANT, DEEP STORAGE and THE COLD ROOMS are
    /// the module's own floors and keep it — a store is a store, and the owner's correction was that the
    /// cabins stay. ISOLATION and the head office's residential bands (RESIDENCY, THE QUIET ROOMS, DEEP
    /// RESIDENCY, THE WINTERING HALL) are rooms where work is done TO somebody or where somebody sleeps,
    /// which is the cell's own idiom. ARCHIVE is racking. RECEPTION is B1's lobby, and CONTINUITY and THE
    /// STANDING ORDER are vaults with an arc bolted to them.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> DeskWorkDepartments =
    [
        // The branch office's own two, and they are the two the owner named.
        "ADMINISTRATION",
        "LABORATORIES",

        // #411 · …and the head office, which is a building made almost entirely of people who sign things.
        "ESTABLISHMENT",
        "THE REGISTRY",
        "SCHEDULING & WINDOWS",
        "PROCUREMENT",
        "CONTRACTS",
        "BRANCH LIAISON",
        "AUDIT",
        "PAYROLL — CLOSED ACCOUNTS",
        "LONG CONTRACTS",
        "SITE ESTABLISHMENT",
        "DISPATCH",
        "OCCUPANCY",
        "WELFARE",
        "THE WINTER OFFICE",
        "THE BERTH OFFICE",
    ];

    /// <summary>#775 · Is this a department whose people sit at desks all day? See
    /// <see cref="DeskWorkDepartments"/>.</summary>
    public static bool IsDeskWork(string department)
    {
        ArgumentNullException.ThrowIfNull(department);
        foreach (string d in DeskWorkDepartments)
        {
            if (string.Equals(d, department, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// #775 · DOES THIS FLOOR GET A LANDSCAPE OFFICE — the block, with a core of meeting rooms in the middle
    /// of it rather than a garden.
    ///
    /// <para>Three refusals, each with a reason:</para>
    /// <list type="bullet">
    /// <item>the surface and the band nobody dug (<see cref="IsFound"/>): a gallery has no departments, no
    /// plates and nobody who ever filed anything, so cutting it an open-plan office would be this file
    /// telling on the one thing the building must never explain (§13.20);</item>
    /// <item><see cref="IsHallFloor"/>, the two canteen floors. The top one already IS the block and its
    /// middle is the park; the staff mess two hundred metres down stands on a rib's own room column, and
    /// taking that ground away would delete a canteen rather than re-cut an office;</item>
    /// <item>a department that is not desk work — see <see cref="DeskWorkDepartments"/>.</item>
    /// </list>
    /// </summary>
    public static bool IsLandscapeFloor(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return level < 0
            && !IsFound(bodyId, level)
            && !IsHallFloor(bodyId, level)
            && IsDeskWork(DepartmentOf(bodyId, level));
    }

    /// <summary>
    /// #775 · DOES THIS FLOOR TAKE THE BLOCK AT ALL — the question the carve asks, and the one that used to
    /// be spelled <see cref="HasParkBlock"/>.
    ///
    /// <para>The two are not the same question, and the day they stopped being the same question is this
    /// issue. A block is a ring of large rooms round a middle with a street on every side of it; a PARK is
    /// what stands in that middle on the one floor of a branch office that has a garden. Everything that
    /// asks "is there a ring, glass, a back street" asks this; everything that asks "is there grass" goes on
    /// asking <see cref="HasParkBlock"/>, and it goes on getting the answer it always did.</para>
    /// </summary>
    public static bool HasBlockOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasParkBlock(bodyId, level) || IsLandscapeFloor(bodyId, level);
    }

    // ── THE MEETING CORE ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #775 · The promenade kept clear all round the core, so every gate through the ring and every suite's
    /// own door onto it arrives on walkable floor and can be walked PAST as well as through.
    ///
    /// <para>The block's own street width (twice <see cref="CorridorHalf"/>) and not a number of its own:
    /// the two end gates arrive at the middle of the core's west and east walls and the crossings arrive at
    /// its near and far ones, and a promenade narrower than the corridor feeding it is a corridor that ends
    /// in a pinch. It is also what makes the owner's <i>walk past everybody</i> true of this floor — every
    /// route from one gate to another runs along the glass of the meeting rooms.</para></summary>
    public static double CoreEdgeClearDu => 2 * CorridorHalf;

    /// <summary>#775 · How wide a meeting room WANTS to be. A table with people down both sides of it and
    /// the floor to get past their chairs — comfortably over <see cref="FireCodeSmallRoomDu"/>, so one of
    /// these is never a booth that gets let off with a single door.</summary>
    public const double MeetingRoomTargetDu = 24.0;

    /// <summary>#775 · The narrowest one may be. Under this the run is left as core floor rather than cut
    /// into a cupboard with two doors in it.</summary>
    public const double MeetingRoomMinDu = 15.0;

    /// <summary>#775 · The fewest meeting rooms a landscape floor may publish. Owner: <i>"lots of meeting
    /// rooms"</i>, and this is the floor under that — the smallest number that makes a row read as a
    /// FACILITY for the thing rather than as one odd room, which is the same shape of number, for the same
    /// reason, as <see cref="CabinetsPerHall"/>.</summary>
    public const int MeetingRoomsPerFloor = 3;

    /// <summary>#775 · The least depth a meeting room may have and still be furnished: the aisle
    /// <see cref="RingOffice.Fit(in RingRoom)"/> keeps at each of its two door-carrying walls, and one place
    /// at the table between them. Derived rather than typed, so an aisle that widens or a table that spreads
    /// takes this with it — and it is what keeps #818's law true by REFUSING to cut a room the furnisher
    /// would hand back empty, rather than by hoping.</summary>
    public static double MinMeetingRoomDepthDu =>
        RingOffice.StreetClearDu + RingOffice.GlassClearDu + RingOffice.TableSeatPitchDu;

    /// <summary>
    /// #775 · ONE MEETING ROOM in the core — a glass box off the open floor.
    ///
    /// <para>Published with its own box, its own ways out and its own furniture for the reason
    /// <see cref="RingRoom"/> is: "every meeting room has two ways out and a table you can sit at" is a law
    /// about a list, and a law about a list nobody keeps is a law nobody can fail.</para>
    /// </summary>
    /// <param name="Number">1-based, west to east, in the order the core was laid.</param>
    /// <param name="X0">Left edge of the box, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge — the far promenade's side.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge — the spine's side.</param>
    /// <param name="Plate">What is stencilled beside its near leaf.</param>
    /// <param name="Ways">Its two leaves: one in the near wall, one in the far wall. A box you can walk
    /// THROUGH, which is what a glass room in an office core actually is — and #822's fire code satisfied by
    /// two holes in two different walls rather than two in one.</param>
    /// <param name="Panes">The rest of those two walls, in the window idiom the park's own glazing uses: an
    /// eye crosses them and a body does not. It is what makes this a glass box rather than a cell, and it is
    /// the sight line the owner asked for — from a desk on the near band you see across the core, into the
    /// meetings, and out the other side.</param>
    /// <param name="Fittings">The table.</param>
    /// <param name="Chairs">The seats round it, which the universal seat verb (#757/#820) reaches exactly
    /// the way it reaches a suite's.</param>
    public readonly record struct MeetingRoom(
        int Number, double X0, double Y0, double X1, double Y1, string Plate,
        IReadOnlyList<SurfaceLayout.Doorway> Ways,
        IReadOnlyList<SurfaceLayout.Wall> Panes,
        IReadOnlyList<RingOffice.Fixture>? Fittings = null,
        IReadOnlyList<RingOffice.Chair>? Chairs = null)
    {
        /// <summary>The furniture, never null.</summary>
        public IReadOnlyList<RingOffice.Fixture> Furniture => Fittings ?? [];

        /// <summary>The seats, never null.</summary>
        public IReadOnlyList<RingOffice.Chair> Seats => Chairs ?? [];

        /// <summary>The middle of it.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>How much floor it has.</summary>
        public double FloorDu2 => (X1 - X0) * (Y1 - Y0);
    }

    /// <summary>#775 · How many meeting rooms a run of core frontage this long is cut into, each of them the
    /// same width — <c>RingRoomsIn</c>'s own sentence said at the core's scale. Zero where the run will not
    /// hold one, which is an honest answer: what is left is core floor.</summary>
    private static int MeetingRoomsIn(double span)
    {
        if (span < MeetingRoomMinDu)
        {
            return 0;
        }
        int n = Math.Max(1, (int)Math.Round(span / MeetingRoomTargetDu, MidpointRounding.AwayFromZero));
        while (n > 1 && span / n < MeetingRoomMinDu)
        {
            n--;
        }
        return n;
    }

    /// <summary>
    /// #775 · THE CORE, CARVED — what an office block puts in the middle of itself when it has no garden.
    ///
    /// <para>Owner's second ask is what decides the shape: <i>"lots of meeting rooms … glass-box rooms off
    /// the open floor"</i>. So the core is neither one room nor solid: it is a ROW of glass boxes down the
    /// middle of the block with a promenade all round them, each box open at BOTH ends.</para>
    ///
    /// <para><b>Nothing here pours a boundary segment.</b> The core's four walls belong to the ring's rooms
    /// and the ring's gates exactly as the park's do (<c>CarveRing</c>), so this lays only what stands INSIDE
    /// the box — the same narrowing <c>CarvePark</c> works under, for the same reason.</para>
    ///
    /// <para><b>And nothing stands in front of a door.</b> The row is cut by the very <c>RingSegments</c>
    /// the ring's own bands are cut by, at the same gate columns, so a crossing through the block arrives in
    /// a cross aisle rather than in the middle of somebody's meeting.</para>
    /// </summary>
    private static List<MeetingRoom> CarveMeetingCore(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        string bodyId, int level, in ParkBlock block)
    {
        var core = new List<MeetingRoom>();

        double y0 = block.Y0 + CoreEdgeClearDu, y1 = block.Y1 - CoreEdgeClearDu;
        if (y1 - y0 < MinMeetingRoomDepthDu)
        {
            return core;   // a core too shallow to hold a table. Open floor, and that is the honest answer.
        }

        // The same segments the ring's own bands are cut into, at the same crossings — so every gate through
        // the block arrives in a cross aisle. The core's two ends are taken in as promenade for the two end
        // gates, which arrive at the middle of its west and east walls.
        List<(double Lo, double Hi)> runs = RingSegments(
            block.X0 + CoreEdgeClearDu, block.X1 - CoreEdgeClearDu, block.SpurXs, CorridorHalf);

        foreach ((double lo, double hi) in runs)
        {
            int n = MeetingRoomsIn(hi - lo);
            for (int k = 0; k < n; k++)
            {
                core.Add(MeetingBox(
                    walls, glass, doorways, labels, claimed, bodyId, level, core.Count + 1,
                    lo + ((hi - lo) * k / n), y0, lo + ((hi - lo) * (k + 1) / n), y1));
            }
        }

        return core;
    }

    /// <summary>#775 · One meeting room: two solid piers, a leaf and a wall of glass at each end, and a
    /// table with chairs down both sides of it.
    ///
    /// <para>Furnished by <see cref="RingOffice.Fit(in RingRoom, RingOffice.Dressing)"/> — the very
    /// furnisher #770's negotiation rooms use, handed the dressing rather than left to read it off a plate.
    /// That is the whole of why this room is believable without one new number: the table's width, the gap a
    /// chair sits off it, how many people fit down a side and how far each of them stands clear of the
    /// square the audit walks to are all already decided, once, in the file that decides them for the block
    /// upstairs.</para></summary>
    private static MeetingRoom MeetingBox(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        string bodyId, int level, int number, double x0, double y0, double x1, double y1)
    {
        // ── THE TWO PIERS. The other two walls are glass with a leaf in the middle of each.
        walls.Add(new(x0, y0, x0, y1, true));
        walls.Add(new(x1, y0, x1, y1, true));

        // ── THE LEAVES · one at each end, which is #822's fire code met by two holes in two DIFFERENT
        //    walls. That is not a saving over two in one face, it is the better answer to the law: a captain
        //    leaves by a wall nobody is standing in, which is the sentence the whole fire-recess idiom
        //    (#822) was built on one floor up.
        var ways = new List<SurfaceLayout.Doorway>(2);
        var panes = new List<SurfaceLayout.Wall>(4);
        double at = (x0 + x1) / 2.0;
        foreach (double face in (double[])[y1, y0])
        {
            ways.Add(new SurfaceLayout.Doorway(at - DoorHalf, face, at + DoorHalf, face));
            panes.Add(new(x0, face, at - DoorHalf, face, true));
            panes.Add(new(at + DoorHalf, face, x1, face, true));
        }
        foreach (SurfaceLayout.Wall pane in panes)
        {
            glass.Add(pane);
        }

        // ── THE PLATE. The building's existing one for exactly this room, and no new word is invented down
        //    here (§13.8): the block upstairs has stencilled NEGOTIATION ROOM · BOOK AT THE COUNTER on the
        //    rooms you sit round a table in since #816, and a bureaucracy that makes a branch book its
        //    meeting room at the one counter in the building is the least surprising thing on this ground.
        bool found = IsFound(bodyId, level);
        string plate = found ? "" : ParkViewPlates[1];

        if (!found)
        {
            foreach (SurfaceLayout.Doorway leaf in ways)
            {
                doorways.Add(leaf);
            }

            // BESIDE the near leaf and never over it — #775's own lesson on the hall's front doors, and the
            // ring's, said a third time: a plate centred on its own doorway is a plate with the captain
            // standing on top of it the moment they arrive.
            labels.Add(new(Math.Clamp(at + DoorHalf + 3.0, x0 + 1.5, x1 - 1.5), y1 - 2.5, plate));
        }

        // ── AND WHAT IS ON THE FLOOR OF IT. Handed to the ring's own furnisher in the frame that puts its
        //    street wall on the NEAR promenade — the grid RingSide.Near is laid in — so the table runs the
        //    depth of the room from one leaf to the other and the chairs face each other across it.
        //
        //    View and Gate are null on purpose. A meeting room has no window onto anything but the two
        //    promenades it opens on, and IsBigSuite — the gate that would hang a kitchenette and two WCs off
        //    it — asks exactly that question. A boardroom with an en-suite would be the amenity ladder
        //    handing one room two rungs, which is #821's own reason for asking the washroom first.
        var box = new RingRoom(
            number, x0, y0, x1, y1, RingSide.Near, ways[0], View: null, Gate: null, plate, ways);
        RingOffice.Furnishing fit = RingOffice.Fit(in box, RingOffice.Dressing.LongTable);
        foreach (SurfaceLayout.Wall solid in fit.Solids)
        {
            walls.Add(solid);
        }

        claimed.Add((x0 - 1.5, y0 - 1.5, x1 + 1.5, y1 + 1.5));

        return new MeetingRoom(number, x0, y0, x1, y1, plate, ways, panes, fit.Fixtures, fit.Chairs);
    }
}
