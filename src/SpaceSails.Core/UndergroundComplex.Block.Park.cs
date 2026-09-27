using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE BLOCK AROUND THE PARK (#813) — the park-block record, the block on a field, and the ring's
/// frontage.
///
/// <para>Split out of <c>UndergroundComplex.Block.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>
    /// #813 · THE BLOCK, AS A PURE FUNCTION OF THE GROUND — every line the ring is laid on, decided once.
    ///
    /// <para>Field-pure on purpose, and it is the same discipline <see cref="RibColumnsOn"/> is written
    /// with: the goods car is placed against these numbers (<see cref="ServiceShaftAt"/>) and a car stands
    /// in the same place on every floor of a site, so anything the car is measured against has to hold for
    /// the whole building and not for the one floor that has a park on it.</para>
    /// </summary>
    /// <param name="WestStreetX">The centre line of the block's west street.</param>
    /// <param name="EastStreetX">The same, east.</param>
    /// <param name="X0">The park's own left edge.</param>
    /// <param name="X1">The park's own right edge.</param>
    /// <param name="Y0">The park's far wall — the back street's side.</param>
    /// <param name="Y1">The park's near wall — the spine's side, and the hall's glass.</param>
    /// <param name="SpineFaceY">The spine's lower face, which is the near ring's own front wall.</param>
    /// <param name="BackStreetY0">The back street's far face — the block's outer wall.</param>
    /// <param name="BackStreetY1">Its near face, which is the far ring's back wall.</param>
    /// <param name="SpurXs">Where a gate cuts through the near and far bands. These are rib columns, so a
    /// corridor and a cross corridor can never disagree about where the crossings are (#801's own
    /// reason).</param>
    public readonly record struct ParkBlock(
        double WestStreetX, double EastStreetX,
        double X0, double X1, double Y0, double Y1,
        double SpineFaceY, double BackStreetY0, double BackStreetY1,
        IReadOnlyList<double> SpurXs)
    {
        /// <summary>The inside face of the west street — the wall the west ring's doors are cut in.</summary>
        public double WestInnerX => WestStreetX + CorridorHalf;

        /// <summary>The same, east.</summary>
        public double EastInnerX => EastStreetX - CorridorHalf;

        /// <summary>The block's own outer wall, west.</summary>
        public double WestOuterX => WestStreetX - CorridorHalf;

        /// <summary>The same, east.</summary>
        public double EastOuterX => EastStreetX + CorridorHalf;

        /// <summary>How much floor the park itself has — what the owner's "do not make it a puny small
        /// closet" is measured in.</summary>
        public double ParkDu2 => (X1 - X0) * (Y1 - Y0);
    }

    /// <summary>#813 · The block's lines, off the ground alone. See <see cref="ParkBlock"/>.</summary>
    public static ParkBlock BlockOn(in SurfaceLayout.Field field)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;
        (double _, double shaftY) = ShaftAt(field);

        double west = left + BlockStreetInsetDu, east = right - BlockStreetInsetDu;
        double spineFace = shaftY - CorridorHalf;
        double nearY = spineFace - RingNearDepthDu;
        double farY = nearY - ParkDepthDu;
        double streetY1 = farY - RingFarDepthDu;
        double streetY0 = streetY1 - (2 * CorridorHalf);

        double x0 = west + CorridorHalf + RingSideDepthDu;
        double x1 = east - CorridorHalf - RingSideDepthDu;

        // WHICH COLUMNS THE GATES GO DOWN. The rib columns, so a gate and a cross corridor are one line —
        // and only those that leave a whole room at each end of the band they cut. A gate against the corner
        // would buy a through-route by spending the frontage the through-route exists to show off.
        var spurs = new List<double>();
        foreach ((int _, double rx) in RibColumnsOn(field))
        {
            if (rx - CorridorHalf >= x0 + RingRoomMinDu && rx + CorridorHalf <= x1 - RingRoomMinDu)
            {
                spurs.Add(rx);
            }
        }

        return new ParkBlock(west, east, x0, x1, farY, nearY, spineFace, streetY0, streetY1, spurs);
    }

    /// <summary>
    /// #813 · HOW MUCH ROOM FRONTAGE THE BLOCK CARRIES ON EACH SIDE OF ITS OWN MIDDLE, in deck units.
    ///
    /// <para>The near and far bands run the length of the block and the gates cut them; what is left is
    /// room, and how much of it lies each side of the park's centre line is what "the less-built side"
    /// means. It is not symmetric, and the reason it is not is worth saying: the rib columns are laid at
    /// fifths of the spine and the one nearest the cage is DROPPED (<see cref="RibColumnsOn"/>), so the
    /// gates fall on one side of the middle and the long unbroken run of suites falls on the other.</para>
    ///
    /// <para>Field-pure, so the car it decides stands in the same place on every floor.</para>
    /// </summary>
    public static (double West, double East) RingFrontageOn(in SurfaceLayout.Field field)
    {
        ParkBlock block = BlockOn(field);
        double mid = (block.X0 + block.X1) / 2.0;
        double west = mid - block.X0, east = block.X1 - mid;
        foreach (double sx in block.SpurXs)
        {
            double lo = sx - CorridorHalf, hi = sx + CorridorHalf;
            west -= Math.Max(0, Math.Min(hi, mid) - Math.Max(lo, block.X0));
            east -= Math.Max(0, Math.Min(hi, block.X1) - Math.Max(lo, mid));
        }
        return (west, east);
    }
}
