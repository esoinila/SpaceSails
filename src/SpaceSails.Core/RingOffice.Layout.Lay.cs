using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// THE PLACER — the room's frame, its keep-clear square, and the four lists everything ends up in.
///
/// <para>One object rather than six parameters threaded through every layout, and one place where the
/// room's own centre is defended: <c>Box</c> refuses to lay anything across it. Every fitting in this
/// family goes through here, so <i>"nothing is built through the square the audit stands on"</i> is a
/// property of the placer and not of five layouts each remembering to check.</para>
///
/// <para>Split out of <c>RingOffice.Layout.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class RingOffice
{
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
}
