using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #821 · THE WASHROOM SUITE — a terrace of cubicles against the pier, and a run of basins opposite.
///
/// <para>It is the service strip's own cell that lays a public cubicle and a big suite's staff WC, so the
/// door the lock lands on is ONE kind of door and there is exactly one place its geometry can be got
/// wrong. What differs is only what stands opposite: a run of basins rather than a kitchenette, because
/// this is a room the whole floor walks into rather than a corner of somebody's office.</para>
///
/// <para>Split out of <c>RingOffice.Layout.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class RingOffice
{
    /// <summary>How far in from a cell's back wall the pan stands. The en-suite's own inset (#707), named
    /// now that a second room lays one — a number typed twice is two answers waiting to disagree.</summary>
    public const double PanInsetDu = 1.4;

    /// <summary>…and half the length of it.</summary>
    public const double PanHalfDu = 1.1;

    /// <summary>
    /// #821 · THE BLOCK'S PUBLIC WASHROOM — a terrace of cubicles down one pier, a basin run down the other,
    /// and a bench between them.
    ///
    /// <para>Owner, standing in the park on the evening of 2026-08-11: <i>"let's add toilets there.. we
    /// might want to hide from guards in one toilet cubicle we lock from inside :-D"</i>, and then
    /// <i>"also a function to wash hands with some film noir comment at the end."</i></para>
    ///
    /// <para>It is the SERVICE STRIP'S OWN IDIOM and deliberately not a second one: the same
    /// <see cref="Cell"/> lays a public cubicle and a big suite's staff WC, so the door the lock lands on is
    /// one kind of door and there is exactly one place its geometry can be got wrong. What is different is
    /// only what stands opposite — a run of basins rather than a kitchenette, because this is a room the
    /// whole floor walks into rather than a corner of somebody's office.</para>
    ///
    /// <para>The terrace stands against a PIER, which is the one pair of walls in a ring room that can never
    /// carry a door, so a row of little boxes runs down the suite without ever coming near a doorway's
    /// clearance — <see cref="ServiceStrip"/>'s reason, unchanged, and the reason the cells still ABUT.</para>
    /// </summary>
    private static void Washroom(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        // ── THE TERRACE, down the FAR pier — the same face of the room the service strip stands against,
        //    and that is not a preference. A cell opens off its uLo face (see Cell), so a terrace laid at
        //    the NEAR pier opens INTO the pier: the leaves are a hand's breadth off a solid wall and #724's
        //    jamb law goes red on 1512 approaches across every clandestine site in the game — "400 presses
        //    left the captain at -127.35, still on the near side of the wall at -125.00", which is a captain
        //    in the room next door walking at a door that is not there. Watched happen inside this issue.
        //
        //    It stops when the room runs out of DEPTH rather than when the count does: a washroom with two
        //    cubicles in it is a washroom, and a cell laid past the glass aisle would be a cubicle in the
        //    window.
        double uStripLo = Math.Max(uA, uB - StripDu);
        double v = vA;
        for (int c = 0; c < PublicCubicles; c++)
        {
            double to = v + CellDu;
            if (to > vB)
            {
                break;
            }
            Cell(lay, uStripLo, uB, v, to, Fitting.Cubicle, PublicWcPlate(c + 1), publish: true);
            v = to;
        }

        // ── THE BASIN RUN, against the NEAR pier and a gangway clear of the terrace's own leaves. One
        //    counter and not one fixture per tap: a basin run IS a worktop with holes in it (see Basin), and
        //    four boxes where the eye reads one length of porcelain is four things for the collision field
        //    to sweep and three more for a degenerate-wall scan to complain about.
        double runHi = Math.Min(uA + (2 * CounterHalfDepthDu), uStripLo - GangwayDu);
        if (runHi <= uA + 0.001 || vB - vA < BasinPitchDu)
        {
            return;
        }

        double runV1 = Math.Min(vB, vA + (BasinsPerRun * BasinPitchDu));
        if (!lay.Box(Fitting.Counter, uA, vA, runHi, runV1, BasinRunPlate, Seating.OneSide))
        {
            return;
        }

        // The taps, spaced along it, standing a body clear of the porcelain on the room's side.
        int taps = Math.Max(1, (int)((runV1 - vA) / BasinPitchDu));
        for (int t = 0; t < taps; t++)
        {
            lay.Tap(runHi + CounterHalfDepthDu, vA + ((runV1 - vA) * (t + 0.5) / taps));
        }

        // ── AND SOMEWHERE TO WAIT. A bench against the SAME PIER, past the end of the run — the plain
        //    room's own plank (see Plain), stood on its side because this pier already has porcelain on the
        //    first half of it. Two places on it, because the ring's furnishing law is that a captain can sit
        //    down in more than one place in a room, and a public washroom with nowhere to wait is a corridor
        //    with taps in it.
        //
        //    #868 · …and it is a BOX and no longer a line, for the reason the plain room's is
        //    (BenchDepthDu): the owner read one of these off a plan and said "the bench is a line", and it
        //    was — one degenerate box, one segment, one stroke. A bench is as deep as a run of shelving is.
        double benchV0 = runV1 + GangwayDu, benchV1 = Math.Min(vB, benchV0 + BenchDu);

        // How far this pier's band reaches before the terrace's own gangway starts — the same line the basin
        // run stops at, asked once. The bench is as deep as that band can spare after a seat has taken its
        // clearance out of it, so the narrowest washroom on the ring gets a thin slab rather than a stroke.
        double column = uStripLo - GangwayDu;
        double benchHi = Math.Min(uA + BenchDepthDu, column - SeatClearDu);
        if (benchV1 - benchV0 < 2.0 || benchHi - uA < CounterHalfDepthDu / 2.0)
        {
            return;
        }
        if (!lay.Box(Fitting.Bench, uA, benchV0, benchHi, benchV1, BenchPlate, Seating.OneSide))
        {
            return;
        }

        // A pace off the plank on the room's side of it — away from the pier, which is the direction the
        // basin run's own taps face, taken off ONE published vector rather than a second pair of numbers.
        // Measured off the bench's own FAR face now that it has one, and never past the porcelain's line.
        //
        // #868 · …and it faces ALONG THE FRONTAGE, off the published axis rather than a rotation of the
        // glass normal — the same one-line sign fault the negotiation table had, in the same shape, on the
        // same band: `(-gy, gx)` is +u on three sides of the ring and −u on the east, so a washroom on that
        // band sat people facing into the pier their own bench is bolted to.
        double seatU = Math.Min(benchHi + ChairSetbackDu, column);
        (double ax, double ay) = lay.Frame.AlongTheFrontage;
        for (int e = 0; e < 2; e++)
        {
            lay.Chair(in room, seatU, benchV0 + ((benchV1 - benchV0) * (e + 0.5) / 2.0), (ax, ay));
        }
    }

    /// <summary>#821 · How many basins a run holds. Enough that a queue is a queue and not a line — and it
    /// is a CAP, because the run is cut to whatever depth the room has left after the terrace.</summary>
    public const int BasinsPerRun = 4;

    /// <summary>#821 · Between two people at the basin run. The EN-SUITE'S OWN PAN, end to end
    /// (<see cref="PanHalfDu"/>), and a jamb between them: a basin is a fixture of the same scale as the
    /// thing in a cubicle, because on this plan it is one. Deliberately not <see cref="SeatPitchDu"/> —
    /// that is the width of a person SITTING at a worktop, which at eight du would have laid four taps down
    /// thirty-three du of porcelain and read as a swimming bath.</summary>
    public static double BasinPitchDu => (2 * PanHalfDu) + CellJambDu;
}
