using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #585 · THE SPINE AND EVERYTHING CUT INTO IT — the four passes of <see cref="Build"/> that own the one
/// corridor every floor down here is built around: where the ribs are, every mouth in the spine's two
/// faces that is not a rib, the caps and cage boxes that close it, and the sweep that pours the faces.
///
/// <para>A corridor is defined by where it does NOT have walls, which is why these four are a subject and
/// not a coincidence: three of them fill lists of spans and the fourth is the only thing that reads them.
/// The wall is poured LAST of the three for the same reason — it used to stand above the hall carve, which
/// is why the hall could not have a front door.</para>
///
/// <para>#251 · Extracted from the 837-line <c>Build</c> as named passes, #1167's method. Every line of
/// every body is the line that was inline, in the order it was in; what is new is the name, the docblock,
/// and the fact that each pass's inputs have to be said out loud. See
/// <c>UndergroundComplex.FloorPlan.cs</c>.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>#801/#813 · WHERE THE CROSS CORRIDORS ARE, AND WHICH WAY EACH ONE RUNS — the x's come from
    /// <see cref="RibColumnsOn"/>, the same list the second car is placed against, so a car and a corridor
    /// can never disagree about where the corridors are.
    ///
    /// <para>Returns the ribs twice, deliberately: the working list the wall sweeps and the room loop read,
    /// and the <see cref="Rib"/> list the plan is published in. They are the same ribs said twice and have
    /// been since #819 stopped the alcoves being appended into the first of them.</para></summary>
    private static (List<(double X, bool Down)> Xs, List<Rib> Published) PlanTheRibs(
        string bodyId, int level, in SurfaceLayout.Field field, ParkBlock? blockOn)
    {
        var ribXs = new System.Collections.Generic.List<(double X, bool Down)>();
        // #801 · The x's come from RibColumnsOn now — the same list the second car is placed against, so a
        // car and a corridor can never disagree about where the corridors are. Which WAY each one runs is
        // still this floor's own seeded business.
        //
        // #813 · …on every floor but ONE. On the block's floor the lower half of the field IS the block, all
        // of it, so a column runs DOWN if and only if the block uses it as a gate through the ring, and
        // every other column runs UP into the ordinary grid. That is not the seed being overruled for
        // convenience: a rib running down anywhere else would arrive in the middle of somebody's office,
        // and the seeded direction is a fact about a floor with two open halves.
        foreach ((int ordinal, double rx) in RibColumnsOn(field))
        {
            bool ribDown = Frac(bodyId, $"hive:{level}:rib-dir:{ordinal}") < 0.62;
            if (blockOn is { } gated)
            {
                ribDown = false;
                foreach (double sx in gated.SpurXs)
                {
                    ribDown |= Math.Abs(sx - rx) < 0.001;
                }
            }
            ribXs.Add((rx, ribDown));
        }

        // #587 · The ribs, exactly as built, published on the plan. This used to be taken HERE, ahead of the
        // two alcoves being appended to the list below, because an alcove is a mouth in a wall and not a
        // corridor anybody walks down. #819 · Nothing is appended any more — the alcoves cut their own spans
        // — so the two lists are the same ribs said twice, and this one is the one the plan is published in.
        var ribList = new List<Rib>(ribXs.Count);
        foreach ((double rx, bool rdown) in ribXs)
        {
            ribList.Add(new Rib(rx, rdown));
        }
        return (ribXs, ribList);
    }

    /// <summary>#819 · EVERY MOUTH CUT IN THE SPINE'S FACES THAT IS NOT A RIB — the lift's alcove, the goods
    /// car's, and the three pockets a site's own history cuts: a preserved doorway, an Authority's seal, and
    /// the second way out.
    ///
    /// <para>They are cut HERE, before any room placer runs, for two reasons that are one reason: each is a
    /// mouth in the spine's own face and the sweep below is what carries it, and each must claim its ground
    /// before anything can be laid on top of it. On a site nobody has buried, stopped, or drawn a
    /// means-of-escape plan for — which is almost every site in almost every world — the three carvers
    /// return having done nothing at all.</para></summary>
    private static (List<(double Y, double Lo, double Hi)> Mouths, double? ServiceX, Specimen? Specimen)
        CutTheAlcoveMouths(
            string bodyId, int level, in SurfaceLayout.Field field, double shaftX, double shaftY,
            List<SurfaceLayout.Wall> walls, List<(double X0, double Y0, double X1, double Y1)> claimed,
            List<LockedDoor> locked, List<SurfaceLayout.Doorway> doorways,
            List<SurfaceLayout.Landmark> labels)
    {
        // ── #819 · AN ALCOVE'S MOUTH IS CUT TO THE ALCOVE'S OWN WIDTH ────────────────────────────────────
        //
        // Owner, on B1 at GOODS CAR 2: "the elevator here has little gaps to the wall." He was reading a
        // seam with two authors on it. Both alcoves used to be APPENDED INTO `ribXs` — they were mouths in a
        // face, so the rib list looked like the place to say so — and everything that comes in by that door
        // is cut at rx ± CorridorHalf, 3.5 du, because that is what a rib is. But the alcove BOX either side
        // of the car stands at ± ShaftHalf, 3.0 du. The face therefore ended half a du outboard of the wall
        // it was supposed to meet, on each side of each car, on both faces, on every floor: ~11 px of
        // daylight between a lift and its own wall at playtest zoom, which is exactly what he saw.
        //
        // A CORRIDOR's width governing a SHAFT's mouth — the "one constant governing the wrong thing" class
        // this file keeps a table of, and the fix is the one #775 already found for the hall's front door:
        // the alcove hands the sweep a SPAN at its own width, the way the hall's and the ring's doors hand
        // theirs at DoorHalf. One sorted list, one cursor, #587's law untouched — and the rib list goes back
        // to holding nothing but ribs, so nothing downstream has to do arithmetic to recognise an alcove.
        var alcoveMouths = new List<(double Y, double Lo, double Hi)>
        {
            (shaftY + CorridorHalf, shaftX - ShaftHalf, shaftX + ShaftHalf),
        };

        // #801 · …and the GOODS CAR's alcove, as a mouth in the LOWER face at the blind end of the corridor.
        // Two cars on one face would read as one machine room; on opposite faces, at opposite ends, they read
        // as two ways out, which is the whole of the feature.
        double? serviceX = ServiceShaftAt(field) is { } car ? car.X : null;
        if (serviceX is { } sx2)
        {
            alcoveMouths.Add((shaftY - CorridorHalf, sx2 - ShaftHalf, sx2 + ShaftHalf));
        }

        // #1063 · …and, on a ground the neighbours have filled in, the recess the old door is kept in. Cut
        // HERE because it is a mouth in the spine's own face and the sweep below is what carries it, and
        // because it must claim its ground before any room placer runs. On every site nobody has buried —
        // which is every site in almost every world — this returns having done nothing at all.
        CarveSpecimen(bodyId, level, field, walls, alcoveMouths, claimed);
        Specimen? specimen = SpecimenOn(bodyId, level, field);

        // #1074 · …and, on a ground whose deep working the Authority has closed, the recess the SEAL stands
        // in. The same pocket, cut here for the same two reasons, and it may be the same pocket because a
        // ground is stopped or buried and never both (StopOrder.TheOfficeGetsThisOne). What differs is what
        // is at the back of it: #1063 keeps a preserved doorway with nothing written on it, and this hangs a
        // leaf that will not open with an office's stamp on it. On every site nobody has stopped — which is
        // every site in almost every world — this returns having done nothing at all.
        CarveStopSeal(bodyId, level, field, walls, alcoveMouths, claimed, locked);

        // #719 · …and THE SECOND WAY OUT, at the other blind end of the same corridor. Cut here with its two
        // neighbours for the same two reasons — it is a mouth in the spine's own face, and it must claim its
        // ground before any room placer runs — and its own placer is written to refuse the ends those two
        // stand in, so a stair can never be cut through a preserved doorway or a stop order's seal. On every
        // floor below the listed bottom this returns having done nothing at all: the building files a
        // means-of-escape drawing for the floors it admits to and for no others.
        CarveStair(bodyId, level, field, walls, alcoveMouths, claimed, doorways, labels);
        return (alcoveMouths, serviceX, specimen);
    }

    /// <summary>#585/#801 · BOTH ENDS SHUT, AND THE TWO CAGE BOXES — the caps that close the corridor (the
    /// missing right-hand one WAS the "open end" the owner walked out of), the lift's box on the upper face,
    /// and the goods car's mirrored onto the lower.
    ///
    /// <para>The second box is CLAIMED as well as built: the ground it stands on is past the last rib's
    /// chambers so nothing was ever going to be laid there, and the ledger says so rather than leaving it to
    /// arithmetic.</para></summary>
    private static void PourTheSpineEnds(
        List<SurfaceLayout.Wall> walls, List<(double X0, double Y0, double X1, double Y1)> claimed,
        double left, double right, double shaftX, double shaftY, double? serviceX)
    {
        // BOTH ends shut. The missing right-hand cap is the "open end" itself.
        walls.Add(new(left, shaftY - CorridorHalf, left, shaftY + CorridorHalf, true));
        walls.Add(new(right, shaftY - CorridorHalf, right, shaftY + CorridorHalf, true));
        // #605 · The floor's name used to be pinned 26 du off down the spine, which is most of a screen
        // from the only thing that tells you which floor you are on. It is painted at the LIFT now
        // (HiveInterior), stacked under the depth, so the plate and the number are read together.

        // ── THE SHAFT. Same spot on every floor.
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf, shaftX - ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX + ShaftHalf, shaftY + CorridorHalf, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf + 5, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));

        // ── #801 · AND THE SECOND ONE, the same box mirrored onto the lower face. Same spot on every floor,
        //    for the cage's own reason: a car a captain has to look for twice is a car they will not use.
        //    Claimed as well as built — the ground it stands on is past the last rib's chambers, so nothing
        //    was ever going to be laid here, and the ledger says so rather than leaving it to arithmetic.
        if (serviceX is { } carX)
        {
            walls.Add(new(carX - ShaftHalf, shaftY - CorridorHalf, carX - ShaftHalf, shaftY - CorridorHalf - 5, true));
            walls.Add(new(carX + ShaftHalf, shaftY - CorridorHalf, carX + ShaftHalf, shaftY - CorridorHalf - 5, true));
            walls.Add(new(carX - ShaftHalf, shaftY - CorridorHalf - 5, carX + ShaftHalf, shaftY - CorridorHalf - 5, true));
            claimed.Add((
                carX - ShaftHalf - 1.5, shaftY - CorridorHalf - 6.5,
                carX + ShaftHalf + 1.5, shaftY - CorridorHalf));
        }
        // #605 · The "LIFT" plate is gone from here. The console at the car mouth is already labelled LIFT,
        // and the signage stack above it (HiveInterior) now answers the bigger question in the same wall
        // space. Three plates on one wall is a wall nobody reads.
    }

    /// <summary>#585/#775/#819 · ONE FACE OF THE SPINE, built as segments that stop either side of every
    /// mouth cut into it. Called twice, once per face, and only after every carve above has filled the four
    /// lists of spans it sweeps — walls are a set, not a sequence, so the wall may be poured last.
    ///
    /// <para>It was a local function closing over those four lists until #251; every value it used to
    /// capture is handed to it now, which is the only change. See its own comments for the three bugs that
    /// shaped it.</para></summary>
    private static void SpineFace(
        double y, Func<double, bool, bool> cutHere,
        List<SurfaceLayout.Wall> walls,
        List<(double X, bool Down)> ribXs,
        List<(double Y, double Lo, double Hi)> alcoveMouths,
        double hallSpineFaceY,
        List<(double Lo, double Hi)> hallSpineCuts,
        List<(double Lo, double Hi)> spineMouths,
        List<(double Lo, double Hi, double PlateX, string Plate)> ringSpineCuts,
        double left, double right)
    {
        // #587 · A CURSOR THAT WALKS A LINE MUST BE GIVEN THE LINE IN ORDER.
        //
        // This is the third bug on this wall and the first one that was invisible from the plan: the
        // geometry was right, the mouths were right, and the WALLS BETWEEN THEM were built by a cursor
        // sweeping left to right over a list that was not sorted left to right. `ribXs` holds the ribs in
        // ascending x (they are Lerped in order) and then the lift alcove APPENDED at the end, at the
        // shaft's own x — which on this field sits left of the right-most rib.
        //
        // So the sweep ran out to the far rib, advanced the cursor past it, then met the alcove behind it
        // and emitted a segment from cursor BACK to the alcove's near edge: one long wall lying across
        // everything between the two, re-sealing both mouths it had just been asked to open. The A*
        // audit reported it as the two room columns beside the right-most rib plus the lift itself —
        // and it only ever happened when that rib pointed UP, because the alcove is only cut into the
        // top face, which is exactly the pattern #587 recorded and could not explain.
        //
        // RibFace already sorts its cuts for precisely this reason. Both faces sort now, and the cursor
        // can only ever move forward — so an overlapping pair of mouths degrades to one wide mouth
        // rather than to a wall.
        //
        // #775 · …and a mouth is a SPAN now rather than a centre, because the two things cut into this
        // wall are no longer the same width: a rib mouth is the corridor's own CorridorHalf and a hall's
        // front door is DoorHalf, the number every other door in the building is cut to. One sweep, one
        // sorted list of spans, and the cursor still only ever moves forward.
        //
        // #819 · …and the alcoves come in as spans of their own now rather than as entries in the rib
        // list, so `ribXs` is ribs and nothing else and this loop cuts corridors only. The sort stays
        // where #587 put it for the reason #587 gave: a door span may arrive from any of four lists and
        // none of them owes this cursor an x order.
        var mouths = new List<(double Lo, double Hi)>();
        foreach ((double rx, bool down) in ribXs)
        {
            if (cutHere(rx, down))
            {
                mouths.Add((rx - CorridorHalf, rx + CorridorHalf));
            }
        }
        foreach ((double ay, double lo, double hi) in alcoveMouths)
        {
            if (Math.Abs(y - ay) < 0.001)
            {
                mouths.Add((lo, hi));
            }
        }
        if (Math.Abs(y - hallSpineFaceY) < 0.001)
        {
            mouths.AddRange(hallSpineCuts);
            mouths.AddRange(spineMouths);
            foreach ((double lo, double hi, double _, string _) in ringSpineCuts)
            {
                mouths.Add((lo, hi));
            }
        }
        mouths.Sort((a, b) => a.Lo.CompareTo(b.Lo));

        double cursor = left;
        foreach ((double lo, double hi) in mouths)
        {
            double near = Math.Max(cursor, lo);
            if (near > cursor)
            {
                walls.Add(new(cursor, y, near, y, true));
            }
            cursor = Math.Max(cursor, hi);
        }
        walls.Add(new(cursor, y, right, y, true));
    }
}
