using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #813 · CUTTING A SIDE OF THE BLOCK INTO ROOMS — the arithmetic behind <see cref="RingRoom"/>: which
/// spans of a side are actually available once the cage, the gates and the hall have taken their bites,
/// how many rooms a span of that length wants, and the carve itself. The only members of the block family
/// that decide anything by measurement rather than by register. Split out of
/// <c>UndergroundComplex.Block.cs</c> under #251 with no member renamed, re-scoped or re-ordered.
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>#813 · How a band of ring frontage is cut into rooms — one answer, asked by the near band,
    /// the far band, the two ends, and by the chooser that decides which sub-segment the hall stands in.
    ///
    /// <para>The gates cut the band into segments; the park's own two ends cut it again, because a room that
    /// straddled the corner would have glass along part of its front wall and rock along the rest, and a
    /// wall that is two materials is a wall two placers will one day disagree about.</para></summary>
    private static List<(double Lo, double Hi)> RingSegments(
        double lo, double hi, IReadOnlyList<double> gapCentres, double half, params double[] splits)
    {
        var cuts = new List<(double Lo, double Hi)>(gapCentres.Count);
        foreach (double c in gapCentres)
        {
            cuts.Add((c - half, c + half));
        }
        cuts.Sort((a, b) => a.Lo.CompareTo(b.Lo));

        var spans = new List<(double Lo, double Hi)>();
        double cursor = lo;
        foreach ((double glo, double ghi) in cuts)
        {
            if (glo > cursor)
            {
                spans.Add((cursor, glo));
            }
            cursor = Math.Max(cursor, ghi);
        }
        if (cursor < hi)
        {
            spans.Add((cursor, hi));
        }

        var cut = new List<(double Lo, double Hi)>(spans.Count + splits.Length);
        foreach ((double slo, double shi) in spans)
        {
            double at = slo;
            foreach (double s in splits)
            {
                if (s > at + 0.001 && s < shi - 0.001)
                {
                    cut.Add((at, s));
                    at = s;
                }
            }
            cut.Add((at, shi));
        }
        return cut;
    }

    /// <summary>#813 · The near band's sub-segments — the run of ground between the spine and the park's own
    /// near wall, once the gates and the park's two ends have been taken out of it. Field-pure, and lifted
    /// out whole so the chooser that puts the hall in one of them and the carve that fills the rest are
    /// reading the SAME list rather than two copies of the same arithmetic (§13.15).</summary>
    public static IReadOnlyList<(double Lo, double Hi)> RingNearSegments(in ParkBlock block) =>
        RingSegments(
            block.WestInnerX, block.EastInnerX, block.SpurXs, CorridorHalf, block.X0, block.X1);

    /// <summary>#813 · How many rooms a run of frontage this long is cut into, and each of them the same
    /// width. Never fewer than one and never so many that one of them is under
    /// <see cref="RingRoomMinDu"/>.</summary>
    private static int RingRoomsIn(double span)
    {
        int n = Math.Max(1, (int)Math.Round(span / RingRoomTargetDu, MidpointRounding.AwayFromZero));
        while (n > 1 && span / n < RingRoomMinDu)
        {
            n--;
        }
        return n;
    }

    /// <summary>
    /// #813 · THE RING, CARVED — every room that faces the park, the four streets that serve them, and the
    /// six gates through them.
    ///
    /// <para>It owns the park's whole boundary, which is the one thing that makes the Manhattan ruling
    /// provable rather than decorative: every du of the park's four walls is laid HERE, either as a room's
    /// glass or as a gate's stub, so "no side of the park is wasted" is true by construction and not by an
    /// arrangement two placers happen to agree on. The single exception is the hall's own glass, which
    /// <see cref="CarveHall"/> laid before this ran, and which this therefore steps over — #585's one-gap
    /// law, said about a wall with two authors.</para>
    ///
    /// <para>Laid in one order, near then far then the two ends, so a room's <see cref="RingRoom.Number"/>
    /// is a fact about where it is rather than about when the loop got to it.</para>
    /// </summary>
    private static List<RingRoom> CarveRing(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<(double Lo, double Hi, double PlateX, string Plate)> spineDoors,
        List<(double Lo, double Hi)> spineMouths,
        List<SurfaceLayout.Doorway> gates, List<LockedDoor> locked,
        string bodyId, int level, in ParkBlock block, Hall? hall)
    {
        bool found = IsFound(bodyId, level);

        // #775 · IS THIS A LANDSCAPE FLOOR — a block with no garden in the middle of it? Two things follow
        // from the answer, and both of them are about not telling a lie the plan can be read for:
        //
        //   · THE PLATES. ParkViewPlates says GARDEN ASPECT and GREEN SIDE and ParkBackPlates is a potting
        //     shed's register. Every one of those is true of the one floor with a park behind the glass and
        //     false of every floor without one, so down here the rooms take the floor's own department
        //     register — SignFor, which is exactly what the block's corner offices have always taken.
        //   · THE DRESSING. A LABORATORIES floor's back band is a lab and not a store: benches with the
        //     glassware racked along them, and desks behind (#775 beat 2).
        bool landscape = !HasParkBlock(bodyId, level);
        bool labs = landscape && ChamberFitting.LabsOn(ChamberFitting.DepartmentOn(bodyId, level));

        var ring = new List<RingRoom>();
        double sf = block.SpineFaceY;

        // ── THE SHELL · three walls, and the fourth side is the spine's own face.
        walls.Add(new(block.WestOuterX, sf, block.WestOuterX, block.BackStreetY0, true));
        walls.Add(new(block.EastOuterX, sf, block.EastOuterX, block.BackStreetY0, true));
        walls.Add(new(block.WestOuterX, block.BackStreetY0, block.EastOuterX, block.BackStreetY0, true));
        claimed.Add((block.WestOuterX - 1.5, block.BackStreetY0 - 1.5, block.EastOuterX + 1.5, sf + 1.5));

        // ── THE MOUTHS ON THE SPINE · the two streets and every gate down the near band. Corridor-width,
        //    and in the mouths list rather than the doors list, because a corridor mouth gets no leaf and no
        //    plate — exactly as a rib's mouth never has.
        spineMouths.Add((block.WestOuterX, block.WestInnerX));
        spineMouths.Add((block.EastInnerX, block.EastOuterX));
        foreach (double sx in block.SpurXs)
        {
            spineMouths.Add((sx - CorridorHalf, sx + CorridorHalf));
        }

        // ── #821 · WHICH OF THE NEAR SUITES IS THE BLOCK'S PUBLIC WASHROOM.
        //
        // Decided BEFORE anything is laid, because the plate is what the furnishing reads (RingOffice.
        // DressingFor) and a room cannot be re-plated after it has been furnished as an office.
        //
        // Off the ground and never off a die: the one nearest the middle of the block, of those wide enough
        // to hold a terrace and a basin run. That is a REASON — the middle of the near band is the busiest
        // frontage in the building, with the hall on one side of it and a gate onto the green on the other,
        // which is where a public washroom goes — and it makes the room a fact a captain can learn rather
        // than a shift's roll.
        double washroomAt = WashroomFrontageOn(block, hall);

        // ── (1) THE NEAR BAND · the premium suites, doors on the spine, glass on the green.
        foreach ((double lo, double hi) in RingNearSegments(block))
        {
            // The hall stands in one of these and it stands in the whole of it (see Build). Nothing else is
            // laid on that ground: the hall published its own glass and its own front doors already.
            //
            // #775 · …on the ONE floor that has a hall. A landscape floor's near band is offices end to end,
            // and the null here is a room that was never carved rather than a room this loop has to guess
            // the extent of — which is why the hall arrives as a Hall? and not as a box of NaNs.
            if (hall is { } venue && venue.X1 > lo + 0.001 && venue.X0 < hi - 0.001)
            {
                continue;
            }
            int n = RingRoomsIn(hi - lo);
            for (int k = 0; k < n; k++)
            {
                double rx0 = lo + ((hi - lo) * k / n), rx1 = lo + ((hi - lo) * (k + 1) / n);
                bool washroom = !double.IsNaN(washroomAt)
                    && rx0 <= washroomAt + 0.001 && rx1 >= washroomAt - 0.001;

                // #817 · AND A DOOR ONTO THE GREEN. Owner's ruling, live in one of these: a premium office
                // that sold on the aspect gets a way OUT into the garden, not only a window at it. The far
                // band's back of house has had exactly this door since #801 and it is the same door.
                //
                // The hall's glass is untouched and always was: the segment the hall stands in is stepped
                // over above, and #751's "the glass between the bar and the green is never a door" was
                // always a rule about the BAR's window wall — a public room with eighty seats in it, whose
                // egress is its own front doors on the spine. RingBox cuts one only where there is park in
                // front of the room to cut it into, so the block's corner offices still have none.
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, RingSide.Near, rx0, block.Y1, rx1, sf, block, gate: true,
                    plateOverride: washroom ? ParkWashroomPlate : null,
                    landscape: landscape, labs: labs));
            }
        }

        // ── (2) THE FAR BAND · #801's back of house, re-anchored as ring fabric. It keeps the door onto the
        //    gravel that made it worth walking across a garden for, and it GAINS the street door the
        //    Manhattan ruling requires — the owner's "nobody walks through an office to reach an office"
        //    said about the one row that used to have no other way in.
        int back = 0;
        foreach ((double lo, double hi) in RingSegments(
            block.WestInnerX, block.EastInnerX, block.SpurXs, CorridorHalf, block.X0, block.X1))
        {
            int n = RingRoomsIn(hi - lo);
            for (int k = 0; k < n; k++)
            {
                double rx0 = lo + ((hi - lo) * k / n), rx1 = lo + ((hi - lo) * (k + 1) / n);
                // ── #775 · AND ONE IN THREE OF THEM DOES NOT OPEN.
                //
                // Owner's oldest note about this building, and the cheapest thing in it: <i>"we can again
                // use the locked doors to give the illusion of much larger space."</i> The block round the
                // park is the PUBLIC floor — a bar, a garden, a washroom anybody may use — and every room on
                // it opens. A department's own floor does not work like that, and a landscape floor cut
                // entirely of open rooms reads as a showroom: it lost the closed doors the ordinary grid
                // gets for free (AddRoomsAlong shuts half its chambers) and the floor stopped implying
                // anything past itself. Watched go red on the guard that says so, at
                // `callisto B10 · NO PLATE: nothing implies the rest of it` — two locked doors on a whole
                // floor.
                //
                // THE BACK BAND, because that is where a department's closed rooms are: off the service
                // street, behind the core, away from the desks. EVERY OTHER one, off the ground rather than
                // off a die, because a seeded share can roll none and the illusion would then be missing on
                // some worlds forever with every test still green — the same reasoning KeyRoomFor is
                // designated for. Alternating rather than clustered, so the row reads as a department's own
                // doors down its own street rather than as one sealed corner.
                //
                // EVERY OTHER and not every third, and the difference is a law: the band is six rooms on the
                // thinnest floor the generator makes, so alternating guarantees THREE closed doors and a
                // third would have guaranteed two. Three is the number the unlisted band's own guard has
                // demanded of every floor in this building since #592 — watched go red at exactly two on
                // probe-moon-36 B10 with a third, which is the arithmetic saying so rather than a taste.
                bool closed = landscape && back % 2 == 1;
                back++;
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, RingSide.Far, rx0, block.BackStreetY1, rx1, block.Y0, block,
                    gate: true, plateOverride: null, landscape: landscape, labs: labs,
                    locked: locked, shut: closed));
            }
        }

        // ── (3) AND THE TWO ENDS · a gate through the middle of each, and a room above and below it.
        foreach (RingSide side in new[] { RingSide.West, RingSide.East })
        {
            bool west = side == RingSide.West;
            double inner = west ? block.WestInnerX : block.X1;
            double outer = west ? block.X0 : block.EastInnerX;
            double mid = (block.Y0 + block.Y1) / 2.0;
            foreach ((double lo, double hi) in RingSegments(
                block.Y0, block.Y1, [mid], CorridorHalf))
            {
                // #817 · …and the two ends are premium suites too: they look at the green out of one whole
                // wall, so they get the same door onto it the near band's do. It is also what keeps their
                // street face sane — see DoorsForFrontage: a 19 du end block with two leaves in it is a
                // face with nowhere to stand.
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, side, inner, lo, outer, hi, block, gate: true,
                    plateOverride: null, landscape: landscape, labs: labs));
            }
        }

        // ── (4) THE GATES · one spur per crossing, and the park's wall opened where it arrives.
        //
        //    THE HALL OPENS ONTO ONE OF THEM. #751's law is that the hall never cuts a door: the gaps in
        //    the corridor beside it ARE its doors, and it publishes the very slots the corridor was cut at.
        //    That corridor used to be a rib and is a gate now — so the gate's side wall is swept with the
        //    hall's own published openings taken out of it, and the room keeps every door it had. Watched go
        //    red without this: "the door at (-1.5,-177.4) is one the hall knows about and the deck plan does
        //    not", 208 of them, on every floor with a block on it.
        var abutting = new List<SurfaceLayout.Doorway>();
        foreach (SurfaceLayout.Doorway o in hall is { } withDoors ? withDoors.Openings : [])
        {
            if (Math.Abs(o.X1 - o.X2) < 0.001)
            {
                abutting.Add(o);

                // …and PUBLISHED, in the same list every other door down here is in, so the deck hangs its
                // imported leaf on it and an audit can find it without knowing anything about halls. This is
                // the job AddRoomsAlong used to do for the hall's own column (#751) and the column is a gate
                // now, so the gate does it. Watched go red without this: "the door at (-1.5,-177.4) is one
                // the hall knows about and the deck plan does not — nothing would be drawn there."
                if (!found)
                {
                    doorways.Add(o);
                }
            }
        }

        foreach (double sx in block.SpurXs)
        {
            RingGate(walls, doorways, claimed, gates, sx, sf, block.Y1, vertical: true, abutting);
            RingGate(walls, doorways, claimed, gates, sx, block.BackStreetY1, block.Y0, vertical: true);
        }
        double midY = (block.Y0 + block.Y1) / 2.0;
        RingGate(walls, doorways, claimed, gates, midY, block.WestInnerX, block.X0, vertical: false);
        RingGate(walls, doorways, claimed, gates, midY, block.EastInnerX, block.X1, vertical: false);

        return ring;
    }
}
