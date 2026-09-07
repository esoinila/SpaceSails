using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// WHICH STRETCHES OF WALL ARE FREE — one piece of kit before it has been given a wall to stand against,
/// the run of wall it could stand on, and the subtraction that takes the doorways, the corners and the
/// room's own centre out of it.
///
/// <para>#869 · The depth is a PARAMETER and not an assumption, because it changes the answer: the
/// clearance to an opening in THIS wall is unaffected (the box grows away from it) and the clearance to
/// an opening in a NEIGHBOURING wall shrinks by exactly the depth. Measured box-to-opening rather than
/// line-to-opening, so the number the guard asks for and the number the placer honoured are one number.</para>
///
/// <para>Split out of <c>ChamberFitting.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class ChamberFitting
{
    // ── THE LAYOUT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One piece of the kit, before it has been given a wall to stand against.</summary>
    /// <param name="DepthDu">#869 · How far it reaches INTO the room from the wall-clear line, for the pen.
    /// Zero is the segment this file has laid since #818 — see <see cref="FittingDepthDu"/> for why a depth
    /// is a picture and never a wall.</param>
    private readonly record struct Piece(
        RingOffice.Fitting Kind, double RunDu, string Plate, bool Seats, double DepthDu = 0.0);

    /// <summary>
    /// #864 · ONE FREE STRETCH OF ONE WALL, in the room's own reading — what is left of an inset line once
    /// every published opening and the audit's own square have taken their clearance out of it.
    ///
    /// <para>Published (rather than kept private to <see cref="Fit"/>) because the incident board
    /// (<see cref="IncidentBoard"/>) has to hang on a chamber wall under exactly the law the furniture is
    /// laid under, and a second author for "where is there wall left" is the shape of bug this house has
    /// paid for repeatedly: two placers that agree today and a doorway that moves tomorrow.</para>
    /// </summary>
    /// <param name="Edge">0 the bottom wall, 1 the top, 2 the left, 3 the right.</param>
    /// <param name="Lo">Where the free stretch starts, on the wall's own axis.</param>
    /// <param name="Hi">…and where it ends.</param>
    /// <param name="Fixed">The inset line's across-coordinate — <see cref="WallClearDu"/> inside the wall.</param>
    /// <param name="Inward">+1 or −1: which way is into the room from that line.</param>
    public readonly record struct WallRun(int Edge, double Lo, double Hi, double Fixed, double Inward)
    {
        /// <summary>Does this wall run along X? True of the bottom and top walls.</summary>
        public bool Horizontal => Edge < 2;

        /// <summary>How much wall there is.</summary>
        public double Length => Hi - Lo;

        /// <summary>One point on the line, in the surface's own coordinates.</summary>
        public (double X, double Y) At(double along) =>
            Horizontal ? (along, Fixed) : (Fixed, along);

        /// <summary>The middle of it — the furthest point of this stretch from whatever took the ends.</summary>
        public (double X, double Y) Middle => At((Lo + Hi) / 2.0);
    }

    /// <summary>
    /// #818/#864 · EVERY STRETCH OF THIS ROOM'S OWN WALLS LONG ENOUGH TO STAND SOMETHING AGAINST, longest
    /// first.
    ///
    /// <para>The measured-wall law of the class summary, as one function: every fitting stands
    /// <see cref="WallClearDu"/> inside a wall, and the free stretches of that inset line are what is left
    /// after every published opening has claimed <see cref="OpeningClearDu"/> either side of itself and the
    /// audit's own square (<see cref="CentreClearDu"/>) has claimed its. A wall whose openings eat all of it
    /// simply carries nothing, which is why nothing in this file has to know which side a chamber's door is
    /// on.</para>
    /// </summary>
    /// <param name="room">The room as published.</param>
    /// <param name="openings">Every hole a body can pass, including the ones that are not ways out. A caller
    /// that hands over more than this room's own loses nothing: an opening in somebody else's wall is too far
    /// away to claim any of this line.</param>
    /// <param name="depthDu">#869 · How far whatever stands here will reach INTO the room. Zero for a thing
    /// bolted flat to the wall (the incident board); <see cref="FittingDepthDu"/> for the furniture.
    ///
    /// <para>It is a parameter and not an assumption because it changes the answer, and getting that wrong
    /// is the one way this issue could have moved a bench into a doorway: the clearance to an opening in
    /// THIS wall is unaffected (the box grows away from it), and the clearance to an opening in a
    /// NEIGHBOURING wall shrinks by exactly the depth. Measured box-to-opening rather than line-to-opening,
    /// so the number the guard asks for and the number the placer honoured are the same number.</para></param>
    public static IReadOnlyList<WallRun> FreeWallRuns(
        in UndergroundComplex.Room room, IReadOnlyList<SurfaceLayout.Doorway> openings,
        double depthDu = 0.0)
    {
        ArgumentNullException.ThrowIfNull(openings);

        double x0 = room.X0 + WallClearDu, x1 = room.X1 - WallClearDu;
        double y0 = room.Y0 + WallClearDu, y1 = room.Y1 - WallClearDu;
        if (x1 - x0 < MinFittingDu || y1 - y0 < MinFittingDu)
        {
            return [];
        }

        // Edge 0 is the bottom wall's line and points INTO the room (+y); 1 the top (−y); 2 the left (+x);
        // 3 the right (−x). Everything is written on (along, fixed, inward) so no layout in this file ever
        // asks which wall it is standing against — the same trick RingOffice.Frame plays, at the one scale
        // where a chamber has no privileged side to play it about.
        var free = new List<WallRun>();
        for (int edge = 0; edge < 4; edge++)
        {
            bool horizontal = edge < 2;
            double fixedAt = edge switch { 0 => y0, 1 => y1, 2 => x0, _ => x1 };
            double inward = edge is 0 or 2 ? +1.0 : -1.0;
            double lo = horizontal ? x0 : y0, hi = horizontal ? x1 : y1;

            // #869 · The ACROSS-RANGE of whatever will stand here: the line itself where the thing is flat
            // against the wall, and the line plus its depth INTO the room where it is furniture.
            double frontAt = fixedAt + (inward * depthDu);
            double boxLo = Math.Min(fixedAt, frontAt), boxHi = Math.Max(fixedAt, frontAt);

            var blocked = new List<(double Lo, double Hi)>();
            foreach (SurfaceLayout.Doorway hole in openings)
            {
                double hx0 = Math.Min(hole.X1, hole.X2), hx1 = Math.Max(hole.X1, hole.X2);
                double hy0 = Math.Min(hole.Y1, hole.Y2), hy1 = Math.Max(hole.Y1, hole.Y2);

                // How far the DEEPEST part of that box is from the opening, ACROSS the line, and how much of
                // the line that leaves inside the clearance circle. Exact for an axis-aligned opening against
                // an axis-aligned box, which is every opening and every fitting in this building.
                double across = horizontal
                    ? Math.Max(Math.Max(hy0 - boxHi, boxLo - hy1), 0.0)
                    : Math.Max(Math.Max(hx0 - boxHi, boxLo - hx1), 0.0);
                if (across >= OpeningClearDu)
                {
                    continue;
                }
                double reach = Math.Sqrt((OpeningClearDu * OpeningClearDu) - (across * across));
                blocked.Add(horizontal ? (hx0 - reach, hx1 + reach) : (hy0 - reach, hy1 + reach));
            }

            // …and the square the audit stands on, which is an obstacle of exactly the same shape.
            double centre = horizontal ? room.Y : room.X;
            double centreAcross = Math.Max(Math.Max(centre - boxHi, boxLo - centre), 0.0);
            if (centreAcross < CentreClearDu)
            {
                double reach = Math.Sqrt((CentreClearDu * CentreClearDu) - (centreAcross * centreAcross));
                double at = horizontal ? room.X : room.Y;
                blocked.Add((at - reach, at + reach));
            }

            foreach ((double Lo, double Hi) span in Remaining(lo, hi, blocked))
            {
                if (span.Hi - span.Lo >= MinFittingDu)
                {
                    free.Add(new WallRun(edge, span.Lo, span.Hi, fixedAt, inward));
                }
            }
        }

        // The longest stretch first, so the piece that wants a wall gets the best one there is. Ties break on
        // the edge's own ordinal, which is deterministic and therefore reproducible on every visit.
        free.Sort((a, b) =>
        {
            int byLength = b.Length.CompareTo(a.Length);
            return byLength != 0 ? byLength : a.Edge.CompareTo(b.Edge);
        });
        return free;
    }
}
