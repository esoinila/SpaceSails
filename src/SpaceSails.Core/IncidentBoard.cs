using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #864 · THIS LABORATORY HAS OPERATED 0 DAYS WITHOUT SARCASM — the incident board on a lab chamber wall.
///
/// <para>Owner, 2026-08-13, blessing the poster set (#853) and adding one more from life: <i>"in one place
/// where I used AFM and transmission electron microscope (to see atom resolution face to face) had this
/// calendar. This lab has survived X days without sarcasm on the wall. That kind gags are such lab humor.
/// :-D"</i></para>
///
/// <h3>The register, which is #853's register</h3>
///
/// <para>Deadpan, and <b>nobody in-world finds it funny</b>. This is a real safety board, of the kind a real
/// safety inspectorate made somebody hang, with the word on the card changed by people who meant it. The
/// building has gone on standing round it for a very long time. Nothing anywhere in the game remarks on it,
/// and the fine print does the whole of the work: the zero is a CARD, there are spares, and every spare is
/// also a zero — which is a fact about a stationery order and about a workplace, and never a joke anybody
/// tells.</para>
///
/// <h3>A poster with no picture</h3>
///
/// <para>It is hung the way <see cref="LabPosters"/> are pressed and drawn — a <c>ViewObject</c> spot, a
/// plate for the eye and a card for the [E] — and it has NO art slot, because it is a text board and the
/// renderer's plate idiom carries it whole. There is nothing to degrade.</para>
///
/// <h3>Where it hangs, and why that is not the poster's answer</h3>
///
/// <para>A poster hangs beside a chamber's own door, on the CORRIDOR side, because a poster is signage for
/// somebody walking past. A board like this hangs INSIDE the room it is about, on the room's own wall — so
/// its placement is the furnishing idiom (#862/#818) rather than the signage one: it takes a free stretch of
/// a chamber's measured wall (<see cref="ChamberFitting.FreeWallRuns"/>), clear of every published opening
/// and clear of the square the A* audit stands a body on. Not one coordinate is typed here, and it is
/// hung under the very law the benches are laid under rather than a second author's copy of it.</para>
///
/// <para>ONE PER LAB FLOOR AT MOST. The owner said <i>"in one place"</i>, and a gag that is in every room is
/// wallpaper.</para>
/// </summary>
public static class IncidentBoard
{
    /// <summary>#864 · What is printed across it, in the board's own shout. Canonical, owner-authored;
    /// changing a character of it changes canon, and the guard hashes it for that reason.</summary>
    public const string Title = "🗓 THIS LABORATORY HAS OPERATED 0 DAYS WITHOUT SARCASM";

    /// <summary>#864 · …and the line you have to stop and read, which is the whole of the gag.</summary>
    public const string FinePrint =
        "The zero is not printed. It is a card in a bracket, and there is a stack of spare cards on the "
        + "shelf below. Every spare is also a zero.";

    /// <summary>
    /// #864 · ONE BOARD ON ONE WALL — the spot the generator hung it on, and the copy that never moves.
    /// </summary>
    /// <param name="X">Where it hangs, in the surface's own coordinates: on the chamber's own measured wall
    /// line, in the middle of a stretch nothing else claimed.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Room">Which published room of the floor it is in, so a guard and a press can name it
    /// without measuring anything.</param>
    public readonly record struct Board(double X, double Y, int Room)
    {
        /// <summary>What is drawn on the deck beside it.</summary>
        public string Plate => Title;

        /// <summary>What [E] puts on the card.</summary>
        public string Card => FinePrint;
    }

    /// <summary>
    /// #864 · WHICH WALL OF THIS FLOOR CARRIES THE BOARD, or none.
    ///
    /// <para>LABORATORIES floors and nowhere else — the department the gag is about, asked of
    /// <see cref="ChamberFitting.LabsOn"/> so one sentence decides it for the furniture, the posters and this
    /// board alike. Null everywhere else, which is a true statement about those floors rather than a missing
    /// one.</para>
    ///
    /// <para>The chamber NEAREST THE LIFT that has a spare wall takes it — the first laboratory a captain
    /// walks into off the car, which is where a building hangs the board it wants read, and the exact
    /// opposite end of the floor from #853's kicker so the two dressings do not pile into one corridor.
    /// "Spare" is measured and not assumed: the furnisher deals the longest stretches to its own kit first
    /// (<see cref="ChamberFitting.Fit"/> takes them in order), so the run at index
    /// <c>room.Furniture.Count</c> is the first one nothing is standing against, and a room with none is
    /// passed over for the next one out.</para>
    /// </summary>
    /// <param name="bodyId">The moon this building is under.</param>
    /// <param name="level">Which floor.</param>
    /// <param name="rooms">The floor as carved AND FURNISHED — the published list, after
    /// <see cref="ChamberFitting.Fit"/> has run, because how much wall is spare is a fact about what is
    /// already standing against it.</param>
    /// <param name="openings">Every hole on the floor a body can pass — the room's own ways and the
    /// en-suites' leaves, which is the same set the furnisher was handed. A caller that hands over more
    /// loses nothing.</param>
    /// <param name="shaftX">The cage's x, so "nearest the lift" is measured off the way home itself.</param>
    /// <param name="shaftY">The same.</param>
    public static Board? On(
        string bodyId, int level, IReadOnlyList<UndergroundComplex.Room> rooms,
        IReadOnlyList<SurfaceLayout.Doorway> openings, double shaftX, double shaftY)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(openings);

        if (!ChamberFitting.LabsOn(ChamberFitting.DepartmentOn(bodyId, level)))
        {
            return null;
        }

        // Every chamber with a door in it and a name on it. A gallery has neither (#677), and a room nobody
        // ever labelled is not a room a safety board was ever hung in.
        var candidates = new List<(double Gap, int Index)>();
        for (int i = 0; i < rooms.Count; i++)
        {
            UndergroundComplex.Room room = rooms[i];
            if (room.Kind != UndergroundComplex.RoomKind.Chamber
                || room.Plate.Length == 0 || room.Ways.Count == 0)
            {
                continue;
            }
            double dx = room.X - shaftX, dy = room.Y - shaftY;
            candidates.Add(((dx * dx) + (dy * dy), i));
        }

        candidates.Sort((a, b) =>
        {
            int byGap = a.Gap.CompareTo(b.Gap);
            return byGap != 0 ? byGap : a.Index.CompareTo(b.Index);
        });

        foreach ((double _, int index) in candidates)
        {
            UndergroundComplex.Room room = rooms[index];

            // Depth ZERO: a board is bolted flat to the wall, so it claims none of the room's floor and
            // takes the clearances the furniture's own line was measured with (#869 gave the runs a depth
            // for the things that stand out into a room; this is not one of them).
            foreach (ChamberFitting.WallRun wall in ChamberFitting.FreeWallRuns(in room, openings))
            {
                // The middle of the stretch first — where the furnisher stands its own piece, which is why
                // the middle of a stretch that is CARRYING one is rejected below — then a hand's breadth in
                // from each end, which is the wall left over beside a bench that does not fill its run.
                foreach (double along in new[] { (wall.Lo + wall.Hi) / 2.0, wall.Lo + EndInsetDu, wall.Hi - EndInsetDu })
                {
                    if (along < wall.Lo - 1e-9 || along > wall.Hi + 1e-9)
                    {
                        continue;
                    }
                    (double x, double y) = wall.At(along);
                    if (Clear(x, y, in room, openings))
                    {
                        return new Board(x, y, index);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>How far in from the end of a free stretch a board hangs, where the middle of that stretch is
    /// already carrying a bench. A hand's breadth — enough that the arithmetic the free-span pass did on the
    /// endpoint (which lands EXACTLY on a doorway's clearance) is not being trusted to the last decimal, and
    /// short enough that a board is still on the wall it was hung on.</summary>
    public const double EndInsetDu = 0.5;

    /// <summary>How much floor a board keeps between itself and the nearest piece of furniture. A board
    /// behind a fume hood is a board nobody ever reads.</summary>
    public static double ClearOfFurnitureDu => ChamberFitting.MinFittingDu / 2.0;

    /// <summary>Is this a square a board can hang on and a captain can stand at to read it? The three
    /// questions the guard asks, asked here first — every published opening, the square the A* audit stands
    /// a body on, and whatever the furnisher already stood against this wall.</summary>
    private static bool Clear(
        double x, double y, in UndergroundComplex.Room room,
        IReadOnlyList<SurfaceLayout.Doorway> openings)
    {
        foreach (SurfaceLayout.Doorway hole in openings)
        {
            if (ChamberFitting.BoxToPoint(
                    Math.Min(hole.X1, hole.X2), Math.Min(hole.Y1, hole.Y2),
                    Math.Max(hole.X1, hole.X2), Math.Max(hole.Y1, hole.Y2), x, y)
                < ChamberFitting.OpeningClearDu)
            {
                return false;
            }
        }

        double dx = x - room.X, dy = y - room.Y;
        if (Math.Sqrt((dx * dx) + (dy * dy)) < ChamberFitting.CentreClearDu)
        {
            return false;
        }

        foreach (RingOffice.Fixture fit in room.Furniture)
        {
            if (ChamberFitting.BoxToPoint(fit.X0, fit.Y0, fit.X1, fit.Y1, x, y) < ClearOfFurnitureDu)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Title;
        yield return FinePrint;
    }
}
