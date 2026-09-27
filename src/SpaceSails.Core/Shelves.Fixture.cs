using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE FIXTURE ITSELF — the <c>Shelf</c> record, its spacing off the chamber's own fitting rules, and
/// <c>On</c>, which stands the shelves along a room's walls.
///
/// <para>Split out of <c>Shelves.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class Shelves
{
    // ── THE FIXTURE ITSELF ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #701 · ONE SHELF, ON ONE WALL — where the generator stood it, and the copy that never moves.
    /// </summary>
    /// <param name="X">Where it stands, in the surface's own coordinates: on the chamber's own measured wall
    /// line, in a stretch nothing else claimed.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Room">Which published room of the floor it is in, so a guard and a press can name it
    /// without measuring anything.</param>
    /// <param name="Of">The authored shelf on it.</param>
    /// <param name="IsFreetime">Which of the room's two this is — what they did, or who they were.</param>
    public readonly record struct Shelf(double X, double Y, int Room, Entry Of, bool IsFreetime)
    {
        /// <summary>What is drawn on the deck beside it, and what the look-card is titled. One string for
        /// both, exactly as the odd book's card wears its own shelf line: a captain reads the same words on
        /// the card that the room is showing them.</summary>
        public string Plate => $"{Glyph} {Of.Shelf}";

        /// <summary>What [E] puts on the card.</summary>
        public string Card => Of.Card;

        /// <summary>What the casebook keeps, once per thread.</summary>
        public string Gist => Of.Gist;

        /// <summary>What the read-list stores.</summary>
        public string Id => Of.Id;
    }

    /// <summary>How far apart a room's two shelves stand, at least. <see cref="ChamberFitting.MinFittingDu"/>
    /// — the shortest stretch of wall this building thinks is worth standing anything against, which is also
    /// the shortest gap at which two plates on one wall read as two objects.</summary>
    public static double ApartDu => ChamberFitting.MinFittingDu;

    /// <summary>How far out of a fitting's own face a shelf's reading spot stands.
    /// <see cref="ChamberFitting.SeatSetbackDu"/> — the number the furnisher already stands a STOOL off a
    /// worktop with, and therefore the one square in this room that is known to hold a body: the seam the
    /// bins' own stand-offs are measured the same way (<c>CarveBins</c>).</summary>
    public static double StandOffDu => ChamberFitting.SeatSetbackDu;

    /// <summary>How far in from the end of a free stretch a shelf stands, where a room falls back on bare
    /// wall. The incident board's own hand's breadth (<see cref="IncidentBoard.EndInsetDu"/>) and not a
    /// second number.</summary>
    public static double EndInsetDu => IncidentBoard.EndInsetDu;

    /// <summary>How much floor a shelf on BARE WALL keeps between itself and the nearest piece of furniture.
    /// The incident board's own clearance, for its reason: a board behind a fume hood is a board nobody ever
    /// reads.</summary>
    public static double ClearOfFurnitureDu => IncidentBoard.ClearOfFurnitureDu;

    /// <summary>
    /// #701 · EVERY SHELF ON THIS FLOOR — two per occupied room, none anywhere else.
    ///
    /// <para><b>A shelf is hung over the room's OWN FURNITURE.</b> That is the finding rather than the first
    /// guess: the first cut stood shelves on bare wall the way the incident board hangs, and <b>only three
    /// occupied rooms in five had bare wall left</b> to stand two things on — a chamber is 15 × 12 du, its
    /// two fire-code doorways claim four du either side of themselves, and #818's furniture is standing
    /// against what is left. The rooms that failed were not unusual rooms. They were the ordinary ones, with
    /// two ways out and two fittings.</para>
    ///
    /// <para>Which is the right answer anyway, and the catalog said so: a shelf belongs in <i>the room's
    /// existing furniture idiom</i>. Books live on the shelving over the bench, not on the one metre of
    /// plaster nobody put anything against. So the candidates are the faces of the room's own fittings —
    /// taken in the order the furnisher dealt them, at the middle of each face and then out toward its ends — and
    /// BARE WALL IS THE FALLBACK, for the handful of rooms whose furniture would not seat two.</para>
    ///
    /// <para>Every candidate is then CHECKED, and nothing here is a coordinate somebody liked: clear of
    /// every published opening, clear of the square the A* audit stands a body on, clear of every OTHER
    /// fitting in the room, clear of the incident board where the floor carries one, and
    /// <see cref="ApartDu"/> from the room's other shelf. §13.15's rule — a fixture that cannot find floor
    /// is not fitted, and the room says so by having none.</para>
    ///
    /// <para>Depth ZERO, like the board: a shelf is fixed to a wall or to the thing standing against it and
    /// claims none of the room's floor, so it lays <b>no solid</b>, moves no wall, and changes nothing the
    /// walkability audits or the sightline cost measure. The building's collision field is byte-for-byte
    /// what it was.</para>
    ///
    /// <para><b>BOTH OR NEITHER.</b> A room that cannot seat two gets none — a room with a work shelf and no
    /// freetime shelf would be the layer saying half a thing about somebody, which is worse than saying
    /// nothing. The guards measure how often that happens rather than assuming it never does.</para>
    /// </summary>
    /// <param name="bodyId">The moon this building is under.</param>
    /// <param name="level">Which floor.</param>
    /// <param name="rooms">The floor as carved AND FURNISHED — the published list, after
    /// <see cref="ChamberFitting.Fit"/> has run, because how much wall is spare is a fact about what is
    /// already standing against it.</param>
    /// <param name="openings">Every hole on the floor a body can pass. A caller that hands over more loses
    /// nothing.</param>
    /// <param name="board">The incident board, where the floor carries one, so a shelf is never stood in
    /// front of it.</param>
    public static IReadOnlyList<Shelf> On(
        string bodyId, int level,
        IReadOnlyList<UndergroundComplex.Room> rooms,
        IReadOnlyList<SurfaceLayout.Doorway> openings,
        IncidentBoard.Board? board = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(openings);

        var shelves = new List<Shelf>();

        // #677 · A SHELF IS A FACILITY OBJECT, and the band nobody dug is not a facility. One call, the odd
        // book's own, so the halls cannot be reached by either feature and there is one place to argue.
        if (!OddBooks.ShelvesStandHere(bodyId, level))
        {
            return shelves;
        }

        string? department = ChamberFitting.DepartmentOn(bodyId, level);
        UndergroundComplex.Kind kind = UndergroundComplex.KindOn(bodyId, level);

        // The ordinal is the room's place among the floor's OCCUPIED rooms — see FreetimeShelf.
        int ordinal = 0;
        for (int i = 0; i < rooms.Count; i++)
        {
            UndergroundComplex.Room room = rooms[i];
            if (!Occupied(in room, department, kind))
            {
                continue;
            }

            Post post = PostIn(room.Plate, department, kind);
            var pair = new List<Shelf>(2);
            foreach ((double x, double y) in Spots(room, openings, board))
            {
                if (pair.Count == 1 && Near(pair[0].X, pair[0].Y, x, y, ApartDu))
                {
                    continue;
                }

                pair.Add(new Shelf(
                    x, y, i,
                    pair.Count == 0 ? WorkShelf(post) : FreetimeShelf(bodyId, level, ordinal),
                    IsFreetime: pair.Count == 1));
                if (pair.Count == 2)
                {
                    break;
                }
            }

            // Both or neither. The ordinal only advances where a hand was actually dealt, so a room the
            // walls refused does not put a gap in the floor's deal.
            if (pair.Count == 2)
            {
                shelves.AddRange(pair);
                ordinal++;
            }
        }

        return shelves;
    }

    /// <summary>How finely a free stretch of BARE wall is walked, in the fallback. A hand's breadth
    /// (<see cref="EndInsetDu"/>): fine enough that the foot of wall beyond a bench's end is still offered,
    /// coarse enough that no room is walked more than a few dozen times.</summary>
    public static double StepDu => EndInsetDu;

    /// <summary>
    /// Every square in this room a shelf could be read at, in the order the room offers them.
    ///
    /// <para><b>The furniture first</b>, in the order the furnisher dealt it: out of each fitting's own face
    /// toward the room, at the middle of the face and then out toward its ends. That is where the books are, and
    /// it is the one square per fitting this building already proves a body fits on — it is the stool's own
    /// setback (<see cref="StandOffDu"/>).</para>
    ///
    /// <para><b>Then the bare wall</b>, longest free stretch first
    /// (<see cref="ChamberFitting.FreeWallRuns"/> sorts them), walked end to end at <see cref="StepDu"/>.
    /// For a room whose furniture cannot seat two, and for the rare room the furnisher left empty.</para>
    /// </summary>
    private static IEnumerable<(double X, double Y)> Spots(
        UndergroundComplex.Room room, IReadOnlyList<SurfaceLayout.Doorway> openings,
        IncidentBoard.Board? board)
    {
        var found = new List<(double X, double Y)>();
        bool boardIsHere = board is { } b && room.Contains(b.X, b.Y);

        // ── THE ROOM'S OWN FITTINGS ──────────────────────────────────────────────────────────────────
        foreach (RingOffice.Fixture kit in room.Furniture)
        {
            // Which way is OUT of this fitting into the room. A fitting is laid along a wall, so its short
            // axis is the one that points at the room — and a fitting with no depth at all (a run of
            // racking, a filing bank) is a LINE, whose short axis is zero long and whose direction is
            // therefore read off the room's own centre. Both cases, one sentence.
            double cx = (kit.X0 + kit.X1) / 2.0, cy = (kit.Y0 + kit.Y1) / 2.0;
            bool along = (kit.X1 - kit.X0) >= (kit.Y1 - kit.Y0);
            double away = along
                ? (room.Y >= cy ? +1.0 : -1.0)
                : (room.X >= cx ? +1.0 : -1.0);
            double face = along
                ? (away > 0 ? kit.Y1 : kit.Y0) + (away * StandOffDu)
                : (away > 0 ? kit.X1 : kit.X0) + (away * StandOffDu);

            double lo = along ? kit.X0 : kit.Y0, hi = along ? kit.X1 : kit.Y1;
            foreach (double t in new[] { 0.5, 1.0 / 6.0, 5.0 / 6.0, 1.0 / 3.0, 2.0 / 3.0 })
            {
                double at = lo + ((hi - lo) * t);
                Offer(along ? at : face, along ? face : at, kit);
            }
        }

        // ── …AND THEN WHATEVER WALL IS LEFT BARE ─────────────────────────────────────────────────────
        foreach (ChamberFitting.WallRun wall in ChamberFitting.FreeWallRuns(in room, openings))
        {
            double lo = wall.Lo + EndInsetDu, hi = wall.Hi - EndInsetDu;
            if (hi < lo)
            {
                continue;
            }

            int steps = (int)Math.Floor((hi - lo) / StepDu);
            for (int s = 0; s <= steps; s++)
            {
                (double x, double y) = wall.At(lo + (s * StepDu));
                Offer(x, y, null);
            }
        }

        return found;

        // One gate, so a spot at a fitting and a spot on bare wall are held to the same three tests and
        // neither list can quietly relax one of them.
        void Offer(double x, double y, RingOffice.Fixture? mine)
        {
            if (!room.Contains(x, y))
            {
                return;   // a face that points out of the room is not a face anybody stands at
            }
            foreach (SurfaceLayout.Doorway hole in openings)
            {
                if (ChamberFitting.BoxToPoint(
                        Math.Min(hole.X1, hole.X2), Math.Min(hole.Y1, hole.Y2),
                        Math.Max(hole.X1, hole.X2), Math.Max(hole.Y1, hole.Y2), x, y)
                    < ChamberFitting.OpeningClearDu)
                {
                    return;
                }
            }
            if (Near(x, y, room.X, room.Y, ChamberFitting.CentreClearDu))
            {
                return;   // the square the A* audit stands a body on stays standable
            }
            foreach (RingOffice.Fixture other in room.Furniture)
            {
                if (mine is { } own && other.X0 == own.X0 && other.Y0 == own.Y0
                    && other.X1 == own.X1 && other.Y1 == own.Y1)
                {
                    continue;   // the fitting this spot is the face OF
                }
                if (ChamberFitting.BoxToPoint(other.X0, other.Y0, other.X1, other.Y1, x, y)
                    < ClearOfFurnitureDu)
                {
                    return;
                }
            }
            if (boardIsHere && board is { } hung && Near(hung.X, hung.Y, x, y, ClearOfFurnitureDu))
            {
                return;   // never in front of the board: the room's library would hide its own gag
            }
            found.Add((x, y));
        }
    }

    private static bool Near(double ax, double ay, double bx, double by, double du)
    {
        double dx = ax - bx, dy = ay - by;
        return ((dx * dx) + (dy * dy)) < du * du;
    }
}
