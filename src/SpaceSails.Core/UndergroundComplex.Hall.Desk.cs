using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #827 · THE COUNTER, LAID OUT ONCE — the box, its customer face, and the row of seats and gaps along
/// that face, all out of the hall's own (u, v) and all in one place.
///
/// <para>Split out of <c>UndergroundComplex.Hall.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>
    /// #827 · THE COUNTER, LAID OUT ONCE — the box, its customer face, and the row of seats and gaps along
    /// that face, all out of the hall's own (u, v) and all in one place.
    ///
    /// <para>This exists because the bar used to be built four times: a collidable wall on the counter's
    /// line, a photograph over the band behind it, a row of stools 1.6 du in front of the line and an [E]
    /// run 2.0 du in front of it. Every one of those was correct arithmetic and they landed the counter at
    /// three different heights on the deck, which the owner walked into and read as <i>"the blue seats are
    /// like without the table that the counter always provides."</i> One carve, four readers, no drift.</para>
    ///
    /// <h3>The row, and why it has holes in it</h3>
    ///
    /// <para>Owner: <i>"the counter is the biggest table with customer seats only on one side … but not
    /// continuously … there are gaps for people to walk to the cashier etc."</i> So the face is cut into
    /// <c>TheStools.Count + <see cref="HallCounterGaps"/></c> even places and two of them are STANDING ones:
    /// the till a quarter of the way along — the first thing you meet after the door aisle, and where the
    /// keep stands on the other side — and the collection point at the far end, where what you ordered comes
    /// back over the desk. The seats keep their ordinals through the gaps: entry <c>s</c> of
    /// <see cref="CounterDesk.Stools"/> is still <c>Interior.TheStools</c>' stool <c>s</c>, which is what
    /// #820's snap and #792's occupancy both read the row by.</para>
    ///
    /// <para>Both the count and the two positions are DERIVED — a quarter of the row, and the end of it —
    /// so a bar that one day seats twelve keeps its cashier a quarter of the way along instead of at a
    /// literal index somebody typed when there were eight.</para>
    /// </summary>
    /// <param name="at">The hall's own (u, v) → field projection, handed in so this never has to know which
    /// way the rib points. <b>v</b> runs from the spine to the far wall, so the hall is at v below the
    /// face.</param>
    /// <param name="u0">Where the SERVING desk starts — past the goods hoist's divider (#775).</param>
    /// <param name="u1">Where it ends.</param>
    /// <param name="faceV">The counter's line: the customer edge of the band.</param>
    /// <param name="backV">The far wall behind it.</param>
    /// <param name="serves">Whether anybody is ever served over this desk. A counter that takes no orders
    /// publishes a box and an empty row, which is a true statement about the staff mess.</param>
    private static CounterDesk TheCounterDesk(
        Func<double, double, (double X, double Y)> at,
        double u0, double u1, double faceV, double backV, bool serves)
    {
        (double fx0, double fy0) = at(u0, faceV);
        (double fx1, double fy1) = at(u1, faceV);
        (double bx0, double by0) = at(u0, backV);
        (double bx1, double by1) = at(u1, backV);

        var places = new List<CounterPlace>();
        if (serves)
        {
            int count = Interior.TheStools.Count + HallCounterGaps;
            int till = Math.Clamp(count / 4, 0, count - 2);
            int collection = count - 1;
            int stool = 0;
            for (int i = 0; i < count; i++)
            {
                double u = u0 + ((u1 - u0) * ((i + 0.5) / count));
                CounterPost post =
                    i == till ? CounterPost.Till
                    : i == collection ? CounterPost.Collection
                    : CounterPost.Stool;

                // A SEAT is a body's radius off the face (elbows on the counter); a GAP is where a body
                // STANDS to be served, which is further out and is the same standoff #791 named. Two
                // quantities, both published, neither typed here.
                double standoff = post == CounterPost.Stool ? HallStoolStandoffDu : HallServiceStandoffDu;
                (double faceX, double faceY) = at(u, faceV);
                (double bodyX, double bodyY) = at(u, faceV - standoff);
                places.Add(new CounterPlace(
                    i, post, post == CounterPost.Stool ? stool++ : -1, faceX, faceY, bodyX, bodyY));
            }
        }

        return new CounterDesk(
            Math.Min(Math.Min(fx0, fx1), Math.Min(bx0, bx1)),
            Math.Min(Math.Min(fy0, fy1), Math.Min(by0, by1)),
            Math.Max(Math.Max(fx0, fx1), Math.Max(bx0, bx1)),
            Math.Max(Math.Max(fy0, fy1), Math.Max(by0, by1)),
            fx0, fy0, fx1, fy1,
            RingOffice.Seating.OneSide, places);
    }
}
