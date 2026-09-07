using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// THE FITTING-OUT ITSELF — the room handed its kit and its free walls, and what each trade's kit is.
///
/// <para>The ordinal is a COUNT and not a seed, which is worth the parameter: a seeded pick leaves a
/// whole kit's appearance on a floor to chance, and a laboratories floor of nine chambers with no furnace
/// anywhere on it is exactly the finding this work exists to prevent — watched happen, fifteen floors,
/// one piece missing each. Counting guarantees that any three furnished lab chambers show the whole kit
/// between them.</para>
///
/// <para><c>BoxToPoint</c> lives here, at the foot of the placer that uses it, and is shared with
/// <c>IncidentBoard</c> — which has to keep a board off the very furniture this file stood against the
/// wall.</para>
///
/// <para>Split out of <c>ChamberFitting.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class ChamberFitting
{
    /// <summary>
    /// #818 · FURNISH ONE ROOM. Handed the box the generator carved and every hole it cut in it, it answers
    /// what is standing in there.
    /// </summary>
    /// <param name="room">The room as published, with its own ways out.</param>
    /// <param name="kit">Which trade's kit — see <see cref="KitFor"/>.</param>
    /// <param name="openings">Every hole in this room's walls a body can pass, INCLUDING the ones that are
    /// not ways out: an en-suite's leaf is a door into a cell and a captain still has to be able to reach it.
    /// A caller that hands over more than this room's own openings loses nothing — an opening in somebody
    /// else's wall is too far away to claim any of this line.</param>
    /// <param name="ordinal">Which room of THIS KIT this is on the floor, counted up as the floor is
    /// furnished. It is what walks the laboratory's specialist rotation round (see <see cref="KitOf"/>), and
    /// it is a COUNT rather than a seed for one reason worth the parameter: a seeded pick leaves the whole
    /// kit's appearance on a floor to chance, and a laboratories floor of nine chambers with no furnace on it
    /// anywhere is exactly the finding this issue exists to prevent. Watched happen — fifteen floors, one
    /// piece missing each. Counting guarantees that any three furnished lab chambers show the whole kit
    /// between them.</param>
    public static RingOffice.Furnishing Fit(
        in UndergroundComplex.Room room, Kit kit,
        IReadOnlyList<SurfaceLayout.Doorway> openings, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(openings);

        if (kit == Kit.None)
        {
            return RingOffice.Furnishing.Empty;
        }

        // ── THE FOUR INSET LINES, AND WHAT IS LEFT OF THEM ────────────────────────────────────────────
        //
        // One function since #864 published it (so the incident board hangs under the very law the furniture
        // is laid under), longest stretch first, so the piece that wants a wall gets the best one there is.
        // An empty answer is a box with nothing left in it after the clearances, or a room whose every wall
        // is a doorway's clearance — reported by the sweep as the violation it is, rather than quietly
        // wedging a bench into a doorway.
        // …and measured for the DEEPEST piece any kit holds, so one span list serves every piece in it. A
        // shallow fitting laid on a span cut for a deep one loses a few du of wall it could have had; a deep
        // one laid on a span cut for a line would be standing in a doorway.
        IReadOnlyList<WallRun> free = FreeWallRuns(in room, openings, FittingDepthDu);
        if (free.Count == 0)
        {
            return RingOffice.Furnishing.Empty;
        }

        var fixtures = new List<RingOffice.Fixture>();
        var chairs = new List<RingOffice.Chair>();
        var solids = new List<SurfaceLayout.Wall>();

        IReadOnlyList<Piece> pieces = KitOf(kit, ordinal);
        int slot = 0;
        for (int p = 0; p < pieces.Count && slot < free.Count && fixtures.Count < MaxFittingsPerRoom; p++)
        {
            Piece piece = pieces[p];
            WallRun wall = free[slot++];
            (double spanLo, double spanHi, double fixedAt, double inward) =
                (wall.Lo, wall.Hi, wall.Fixed, wall.Inward);

            double run = Math.Min(piece.RunDu, spanHi - spanLo);
            double mid = (spanLo + spanHi) / 2.0;
            double a = mid - (run / 2.0), b = mid + (run / 2.0);
            bool horizontal = wall.Horizontal;

            // ── #869 · THE BOX IS A PICTURE ───────────────────────────────────────────────────────────
            //
            // A fitting with a DEPTH is drawn as the rectangle it is — #868's finding, said one building
            // along: an outline round a dark gap reads as somewhere you could stand, and only a filled box
            // reads as furniture. The depth grows INTO the room off the wall-clear line, so the fitting's
            // back stays exactly where the segment always stood and not one clearance this file already
            // measured moves (see FittingDepthDu), and it is dropped outright where the deeper box would
            // reach a doorway or the audit's own square.
            //
            // ── #883 · …AND THE SOLID IS THAT PICTURE, NOT THE LINE UNDER IT ──────────────────────────
            //
            // "The SOLID stays the single segment #818 has laid since this feature shipped" is what stood
            // here, with the frame budget as its reason (Lab 45: the sightline is O(walls)). It bought that
            // budget with the oldest fault in this building. A drawn box backed by one rail is not a fence
            // round a hollow like the ring's — it is WORSE, because the hollow is wide open: the front
            // 0.6 du strip of every lab bench and every chamber desk in the game is ordinary walkable floor
            // that the pen fills in as mass, and a captain who walks there is standing inside the furniture.
            // Measured before it was touched: 19,957 standable squares under 1,109 lab benches and a further
            // 30 under one floor's desks, and an A* from the room's own centre reached EVERY ONE of them.
            //
            // The four rails cost three segments apiece and no hatch, because a fitting is FittingDepthDu
            // (1.3 du) deep and nothing 1.3 du deep can hold a captain 1.4 du across — that is #883's own
            // early-out in AddSolidMass, and it is why the honest box is affordable in fifteen hundred rooms.
            double frontAt = fixedAt + (inward * piece.DepthDu);
            double acrossLo = Math.Min(fixedAt, frontAt), acrossHi = Math.Max(fixedAt, frontAt);

            (double fx0, double fy0, double fx1, double fy1) = horizontal
                ? (a, acrossLo, b, acrossHi)
                : (acrossLo, a, acrossHi, b);

            fixtures.Add(new RingOffice.Fixture(
                piece.Kind, fx0, fy0, fx1, fy1, piece.Plate,
                piece.Seats ? RingOffice.Seating.OneSide : RingOffice.Seating.None));

            // A piece with no depth IS a line — a run of racking, a furnace, a filing bank — and the pen
            // never fills one, so it keeps the single segment #818 gave it. Only the pieces the renderer
            // draws as a rectangle are laid as the rectangle they are drawn as.
            if (piece.DepthDu < 0.001)
            {
                solids.Add(horizontal
                    ? new SurfaceLayout.Wall(a, fixedAt, b, fixedAt, true)
                    : new SurfaceLayout.Wall(fixedAt, a, fixedAt, b, true));
            }
            else
            {
                SurfaceLayout.AddSolidMass(solids, fx0, fy0, fx1, fy1, hull: true);
            }

            if (!piece.Seats || chairs.Count > 0)
            {
                continue;
            }

            // ── THE STOOL, a pace off the worktop on the room's side of it.
            //
            // Owner's kit opens with the word "chairs", and a bench you cannot sit at is a bench in a
            // catalogue. It is laid clear of the segment by more than a body's radius so #820's snap can put
            // the captain ON it, and shifted along the run if the room's own centre is where it wanted to
            // stand — the audit's square is defended for a seat exactly as it is for a solid, because a body
            // parked on it is the same report.
            double seatAlong = mid;
            double seatAcross = fixedAt + (inward * SeatSetbackDu);
            (double sx, double sy) = horizontal ? (seatAlong, seatAcross) : (seatAcross, seatAlong);
            if (Near(sx, sy, room.X, room.Y, CentreClearDu))
            {
                seatAlong = mid + (((spanHi - mid) > (mid - spanLo) ? +1.0 : -1.0) * CentreClearDu * 2.0);
                seatAlong = Math.Clamp(seatAlong, spanLo, spanHi);
                (sx, sy) = horizontal ? (seatAlong, seatAcross) : (seatAcross, seatAlong);
            }
            if (Near(sx, sy, room.X, room.Y, CentreClearDu))
            {
                continue;   // nowhere in this room to sit that is not the square the audit walks to
            }

            chairs.Add(new RingOffice.Chair(
                chairs.Count, sx, sy,
                horizontal ? 0.0 : -inward, horizontal ? -inward : 0.0,
                room.Plate));
        }

        return new RingOffice.Furnishing(fixtures, chairs, solids, []);
    }

    /// <summary>How far a box is from a point. Shared with <see cref="IncidentBoard"/>, which has to keep a
    /// board off the very furniture this file stood against the wall.</summary>
    internal static double BoxToPoint(double x0, double y0, double x1, double y1, double px, double py)
    {
        double dx = Math.Max(Math.Max(x0 - px, px - x1), 0.0);
        double dy = Math.Max(Math.Max(y0 - py, py - y1), 0.0);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>Is this point inside a keep-out square's radius of that one?</summary>
    private static bool Near(double ax, double ay, double bx, double by, double radius) =>
        ((ax - bx) * (ax - bx)) + ((ay - by) * (ay - by)) < radius * radius;

    /// <summary>What is left of a line once every blocked stretch has been taken out of it. Sorted and
    /// swept with a cursor that only ever moves forward — #587's law, which this house has paid for once and
    /// does not intend to pay for again on a new list.</summary>
    private static List<(double Lo, double Hi)> Remaining(
        double lo, double hi, List<(double Lo, double Hi)> blocked)
    {
        blocked.Sort((a, b) => a.Lo.CompareTo(b.Lo));
        var open = new List<(double Lo, double Hi)>();
        double cursor = lo;
        foreach ((double bLo, double bHi) in blocked)
        {
            if (bLo > cursor)
            {
                open.Add((cursor, Math.Min(bLo, hi)));
            }
            cursor = Math.Max(cursor, bHi);
            if (cursor >= hi)
            {
                return open;
            }
        }
        open.Add((cursor, hi));
        return open;
    }

    /// <summary>
    /// #818 · THE KIT — what a room of this trade actually holds, in the order it wants a wall.
    ///
    /// <para>The laboratory's is a ROTATION and not a fixed three, and that is the owner's list being taken
    /// seriously: <i>"chairs / tables / vacuum chambers … fume hoods … where do I put my test tube? Etc
    /// furnaces"</i> is a description of a FLOOR, not of one room. Every lab chamber gets its bench and a
    /// stool; which two of the specialist pieces stand beside it is walked round the list ONE ROOM AT A TIME,
    /// so a laboratories floor shows the whole kit across its chambers and no single room is a showroom.</para>
    /// </summary>
    private static IReadOnlyList<Piece> KitOf(Kit kit, int ordinal)
    {
        switch (kit)
        {
            case Kit.Laboratory:
            {
                Piece[] specialists =
                [
                    new(RingOffice.Fitting.FumeHood, UnitRunDu, FumeHoodPlate, false),
                    new(RingOffice.Fitting.VacuumChamber, UnitRunDu, VacuumChamberPlate, false),
                    new(RingOffice.Fitting.Furnace, UnitRunDu, FurnacePlate, false),
                ];
                int start = ((ordinal % specialists.Length) + specialists.Length) % specialists.Length;
                return
                [
                    // #869 · A bench is 90 cm deep because a person is. It keeps its RUN — a bench runs the
                    // wall, and that is what makes it a bench rather than a desk — and takes the human-true
                    // depth, so what the pen fills is the surface somebody actually stands at.
                    new(RingOffice.Fitting.LabBench, BenchRunDu, BenchPlate, true, FittingDepthDu),
                    specialists[start],
                    specialists[(start + 1) % specialists.Length],
                ];
            }

            case Kit.Store:
                return
                [
                    new(RingOffice.Fitting.Racking, RackRunDu, RackingPlate, false),
                    new(RingOffice.Fitting.Racking, RackRunDu, "", false),
                    new(RingOffice.Fitting.Racking, UnitRunDu, "", false),
                ];

            case Kit.Plant:
                return
                [
                    new(RingOffice.Fitting.Machinery, UnitRunDu, MachineryPlate, false),
                    new(RingOffice.Fitting.Machinery, UnitRunDu, "", false),
                    new(RingOffice.Fitting.Shelving, UnitRunDu, "", false),
                ];

            case Kit.Office:
                return
                [
                    // #869 · ONE PERSON'S DESK, at the size the owner measured his own with a tape:
                    // 160 x 90 cm, which is DeskRunDu x FittingDepthDu. It ran the whole wall until this
                    // issue — ten du of worktop for one clerk — and a desk sized like a bench is exactly the
                    // half of "reads like furniture" that a plate cannot fix.
                    new(RingOffice.Fitting.DeskBank, DeskRunDu, "", true, FittingDepthDu),
                    new(RingOffice.Fitting.FilingCabinet, UnitRunDu, FilingPlate, false),
                    new(RingOffice.Fitting.Shelving, UnitRunDu, "", false),
                ];

            case Kit.Clinic:
                return
                [
                    new(RingOffice.Fitting.Bench, BenchRunDu, ExaminationPlate, true),
                    new(RingOffice.Fitting.Counter, UnitRunDu, WorktopPlate, false),
                    new(RingOffice.Fitting.Shelving, UnitRunDu, "", false),
                ];

            default:
                return
                [
                    new(RingOffice.Fitting.Shelving, UnitRunDu, RingOffice.StorePlate, false),
                    new(RingOffice.Fitting.Bench, BenchRunDu, RingOffice.BenchPlate, true),
                ];
        }
    }
}
