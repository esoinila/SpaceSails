using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: the layout — one room furnished, and the kits it is furnished from.

/// <summary>
/// #251 · ONE ROOM FURNISHED — the entry point, and the one rule every layout in the family obeys: a
/// fitting's centre line is pushed clear of the room's own centre, because the audit stands a body on
/// that square.
///
/// <para>This is the opening file of a five-part family, and the other four are named for what they lay:
/// <c>.Lay</c> (the placer — the room's frame, its keep-clear square, and the lists everything ends up
/// in), <c>.Desks</c> (the banks, the negotiation table and the reception counter), <c>.Rooms</c> (the
/// plain version, #817's service strip and the cell it is made of), and <c>.Washroom</c> (#821's suite:
/// a terrace of cubicles against a pier, and the basin run opposite).</para>
///
/// <para>The family declares no static field — <c>PanInsetDu</c>, <c>PanHalfDu</c> and
/// <c>BasinsPerRun</c> are <c>const</c>s and <c>BasinPitchDu</c> is a property — so the #1163 initializer
/// hazard is absent by construction. No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public static partial class RingOffice
{
    // ── THE LAYOUT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #817 · FURNISH ONE ROOM. Handed the box the ring carved, it answers what is standing in it.
    ///
    /// <para>Everything is laid in the room's own u/v grid and mapped out at the end, so nothing below knows
    /// or cares which of the park's four walls this room is one of.</para>
    /// </summary>
    public static Furnishing Fit(in UndergroundComplex.RingRoom room) =>
        Fit(in room, DressingFor(in room));

    /// <summary>
    /// #775 · FURNISH ONE ROOM AS SOMETHING ITS PLATE DOES NOT SAY — the same placer, handed the dressing.
    ///
    /// <para><see cref="DressingFor"/> reads a room's PLATE, because on the ring round the park the plate is
    /// the only thing that says what a room is for, and that stays the answer for every room the ring cuts.
    /// The landscape floors have two rooms it cannot answer for: a meeting room in the block's core, whose
    /// plate comes out of the floor's own department register and says nothing about tables, and a
    /// laboratory suite, which is a room of desks with a run of benches down one pier and has no plate
    /// anywhere in this building either. Both are decided by the CARVE — it knows it is laying a core, and
    /// it knows what department it is standing on — so the carve SAYS so, rather than encoding the answer in
    /// a string and reading it back out. One fact told twice is the table at the top of this repo's
    /// spec.</para>
    /// </summary>
    public static Furnishing Fit(in UndergroundComplex.RingRoom room, Dressing dressing)
    {
        var frame = new Frame(in room);
        double uA = frame.ULo + PierClearDu, uB = frame.UHi - PierClearDu;
        double vA = StreetClearDu, vB = frame.Depth - GlassClearDu;

        // A box with no room left in it after the aisles. Nothing on the ring is this small today; the
        // answer is an empty room rather than a half-built one, because a placer that squeezes furniture
        // into a space it does not fit is a placer that builds a desk through a wall.
        if (uB - uA < 4.0 || vB - vA < 2.0)
        {
            return Furnishing.Empty;
        }

        var fixtures = new List<Fixture>();
        var chairs = new List<Chair>();
        var solids = new List<SurfaceLayout.Wall>();
        var doors = new List<SurfaceLayout.Doorway>();
        var stalls = new List<Stall>();
        var basins = new List<Basin>();

        // Where the room's own centre falls in the room's own grid — the square the A* audit stands on.
        double uCentre = (frame.ULo + frame.UHi) / 2.0, vCentre = frame.Depth / 2.0;

        var lay = new Lay(frame, uCentre, vCentre, fixtures, chairs, solids, doors, stalls, basins);

        // THE SERVICE STRIP first, because it takes frontage away from everything else. A big suite's desks
        // stop short of it; a small one never had it and keeps its whole width.
        double workUB = uB;
        if (IsBigSuite(in room))
        {
            // …and the desks stop a GANGWAY short of it, not a joint: the cubicle doors open onto this gap
            // and a captain has to be able to step out of one.
            workUB = uB - StripDu - GangwayDu;
            ServiceStrip(lay, in room, uB - StripDu, uB, vA, vB);
        }

        switch (dressing)
        {
            // #775 · THE LABORATORY SUITE — the owner's second beat, drawn: <i>"benches with instruments
            // beside desk banks"</i>. It is the READING ROOM's own sentence with the shelving swapped for a
            // run of bench: a band down the near pier, a gangway, and then the desks. The bench is the
            // building's existing fixture and wears the building's existing plate — a laboratory floor's
            // chambers have carried both since #818, and a second vocabulary for the same length of worktop
            // would be two answers to what a bench is.
            case Dressing.LabHall:
                double run = uA + BenchDepthDu;
                if (lay.Box(
                        Fitting.LabBench, uA, vA, run, vB, ChamberFitting.BenchPlate, Seating.OneSide))
                {
                    // …and the stools at it, facing BACK ALONG THE FRONTAGE at the bench they belong to —
                    // off the published axis and never a rotation of the glass normal, which is the one-line
                    // sign fault #868 found on the east band twice already. A run of glassware nobody can
                    // sit at is the very lie #818 was opened about.
                    (double ax, double ay) = lay.Frame.AlongTheFrontage;
                    int stools = Math.Max(1, (int)((vB - vA) / SeatPitchDu));
                    for (int s = 0; s < stools; s++)
                    {
                        lay.Chair(
                            in room, run + SeatClearDu, vA + ((vB - vA) * (s + 0.5) / stools), (-ax, -ay));
                    }
                }
                Banks(lay, in room, run + GangwayDu, workUB, vA, vB);
                break;

            case Dressing.Washroom:
                Washroom(lay, in room, uA, uB, vA, vB);
                break;

            case Dressing.LongTable:
                LongTable(lay, in room, uA, workUB, vA, vB);
                break;

            case Dressing.ReadingRoom:
                double shelf = uA + (2 * CounterHalfDepthDu);
                lay.Box(Fitting.Shelving, uA, vA, shelf, vB, ReadingShelfPlate);
                Banks(lay, in room, shelf + GangwayDu, workUB, vA, vB);
                break;

            case Dressing.Reception:
                Banks(
                    lay, in room, uA, workUB,
                    Counter(lay, in room, uA, workUB, vA) + GangwayDu, vB);
                break;

            case Dressing.Plain:
                Plain(lay, in room, uA, workUB, vA, vB);
                break;

            default:
                Banks(lay, in room, uA, workUB, vA, vB);
                break;
        }

        return new Furnishing(fixtures, chairs, solids, doors, stalls, basins);
    }

    /// <summary>Push a fitting's centre line clear of the room's own centre, on whichever side of it the
    /// fitting already was. See the class summary: the audit stands a body on that square.</summary>
    private static double ClearOfCentre(double at, double centre, double half) =>
        Math.Abs(at - centre) >= half + RoomCentreClearDu
            ? at
            : at < centre ? centre - half - RoomCentreClearDu : centre + half + RoomCentreClearDu;
}
