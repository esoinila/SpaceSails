using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// WHAT EVERYTHING ELSE GETS — the plain version (shelving, a bench, and the worktop the bench is a bench
/// AT), #817's service strip down a big suite's pier, and the cell both of them are made of.
///
/// <para>The plain room is the one the owner sat down in and read off the plan: <i>"The graphics kind of
/// does not show there being a table"</i> · <i>"The bench is a line"</i> · <i>"and too far to use as a
/// bench"</i>. All three were true and all three were this method's, which is why the pieces are laid as
/// a SET that explains itself.</para>
///
/// <para>The strip stands against a PIER — the one pair of walls in a ring room that can never carry a
/// door — so a run of little boxes goes the whole depth without ever coming near a doorway's clearance,
/// and the cells ABUT, which is load-bearing: past a cell's end wall is the next cell's door, and there
/// is nowhere on that face to stand that is not in front of one.</para>
///
/// <para>Split out of <c>RingOffice.Layout.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class RingOffice
{
    /// <summary>
    /// THE PLAIN VERSION — shelving, a bench, and the worktop the bench is a bench AT.
    ///
    /// <para>What a corner office with no view and the back of house (#801) get. Owner's law is that no floor
    /// is bare, not that every floor is furnished the same: a potting shed with cubicles in it would be the
    /// plate and the room disagreeing. The bench still SEATS you, because people sit down in these rooms
    /// too.</para>
    ///
    /// <h3>#868 · What the owner found in one of these, and the three things wrong with it</h3>
    ///
    /// <para>He sat down at a chair in <c>❄ COLD ROOM · TO CANTEEN 1</c> on the back street of B1 and read
    /// the room off the plan: <i>"The graphics kind of does not show there being a table"</i> ·
    /// <i>"The bench is a line"</i> · <i>"and too far to use as a bench"</i> · and, as the positive control,
    /// <i>"The Shelving is clear as furniture goes."</i></para>
    ///
    /// <para>Every one of those was TRUE, and all three were this method's:</para>
    ///
    /// <list type="number">
    /// <item><b>There was no table.</b> Not undrawn — ABSENT. A plain room published two chairs and not one
    /// worktop, while the sit line told the captain the worktop in front of them was clear. The renderer had
    /// nothing to draw because Core had stood nothing there.</item>
    /// <item><b>The bench was a degenerate box</b>, so it was one solid segment, so it was one stroke. See
    /// <see cref="BenchDepthDu"/>.</item>
    /// <item><b>The chairs faced the glass with the bench BEHIND them</b> and nothing in front, a pace out
    /// into the middle of the floor. A seat with its back to the only fixture in reach is the "too far to use
    /// as a bench" the owner was looking at.</item>
    /// </list>
    ///
    /// <para>The fix is his own, quoted: <i>"could the table just be a different color rectangle in front of
    /// the chair, so arms (and papers) could rest on it?"</i> — a SET, laid so the three pieces explain each
    /// other. The bench stands against the far pier, a body's setback out from it is the seat, and a setback
    /// past that is the worktop, so the dot at the chair has the bench behind it and the slab in front of it
    /// and neither is a stroke.</para>
    ///
    /// <h3>Why it is all laid across the FRONTAGE</h3>
    ///
    /// <para>Measured, not preferred. The back-of-house band is one chamber module deep
    /// (<see cref="UndergroundComplex.RoomHeightDu"/> = 12 du) and gives <see cref="StreetClearDu"/> to the
    /// street aisle and <see cref="GlassClearDu"/> to the walkway at the glass, which leaves TWO du of depth
    /// to furnish. A set stacked across the depth does not fit in the very room the owner was sitting in —
    /// the frontage is where the sixteen to forty du are, so the set is laid along it and grows into it.</para>
    /// </summary>
    private static void Plain(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        double shelfHi = uA + (2 * CounterHalfDepthDu);
        lay.Box(Fitting.Shelving, uA, vA, shelfHi, vB, StorePlate);

        if (vB - vA < 0.001)
        {
            return;
        }

        // How LONG the set is. A bench's own published length, or the whole band where the band is shallower
        // than that — which is the far ring, where it is two du. It is anchored at the street aisle so that a
        // deep corner office gets a bench somebody could walk in and sit on rather than a forty-du plank.
        double setV1 = Math.Min(vB, vA + BenchDu);

        // ── THE SET, measured back from the far pier: bench, a body, worktop.
        double benchLo = uB - BenchDepthDu;
        double seatU = benchLo - ChairSetbackDu;
        double workHi = seatU - ChairSetbackDu;
        double workLo = workHi - WorktopDepthDu;

        // Where the set may start at all: past the shelving and the gangway a body walks between two
        // fittings, and clear of the square the ring's A* audit stands on. Both are published numbers of this
        // file's own, and neither is a coordinate.
        double floorAt = Math.Max(shelfHi + GangwayDu, lay.UCentre + RoomCentreClearDu);
        bool bench = workLo >= floorAt;

        if (!bench)
        {
            // A frontage too short to hold the bench as well. The WORKTOP is the piece that stays, because a
            // chair without one is the very lie this issue was opened about: the set loses its bench rather
            // than its reason to have a chair at all.
            workLo = floorAt;
            workHi = workLo + WorktopDepthDu;
            seatU = workHi + ChairSetbackDu;
        }

        if (seatU > uB + 0.001)
        {
            return;   // nowhere in this room to sit that is not a pier. The shelving, and honest bare floor.
        }

        if (!lay.Box(Fitting.Counter, workLo, vA, workHi, setV1, WorktopPlate, Seating.OneSide))
        {
            return;
        }

        if (bench)
        {
            lay.Box(Fitting.Bench, benchLo, vA, uB, setV1, BenchPlate, Seating.OneSide);
        }

        // …and the seat, FACING THE WORKTOP — back down the frontage, off the one published axis
        // (<see cref="Frame.AlongTheFrontage"/>) rather than a fifth hand-written pair of numbers. It is the
        // whole of the third finding: a chair in a back room looks at the thing it works at, and a chair
        // that looked at a window it does not have is what put the garden in a cold store's narration.
        (double ax, double ay) = lay.Frame.AlongTheFrontage;
        lay.Chair(in room, seatU, (vA + setV1) / 2.0, (-ax, -ay));
    }

    /// <summary>
    /// #817 · THE SERVICE STRIP — the kitchenette, the two WC cubicles and the privacy booths, down the pier
    /// of a big suite.
    ///
    /// <para>Owner: <i>"Such a big premium office would have little kitchen and couple toilets also"</i> and
    /// <i>"maybe some privacy cabinets also"</i>. It stands against a PIER — the one pair of walls in a ring
    /// room that can never carry a door — so a strip of little boxes can run the whole depth of the suite
    /// without ever coming near a doorway's clearance. The desks give up that much frontage and keep the
    /// rest, which is why <c>Fit</c> lays this first.</para>
    ///
    /// <para>Every cell is the en-suite's own idiom (#707): a walled box with a gap in the face it opens off,
    /// and one stub of a fixture inside it so it reads as what it is at plate scale. The cubicles' gaps are
    /// PUBLISHED DOORS (#821 will lock them from the inside); the booths' are open fronts, because a phone
    /// box you can be shut into is a different feature and this issue is not it.</para>
    ///
    /// <h3>The cells ABUT, and that is load-bearing</h3>
    ///
    /// <para>They were laid with a joint between them first, so the strip would read as a row of separate
    /// boxes, and #724's jamb law went red on every site: a captain walking at the edge of a cubicle door was
    /// funnelled AWAY from it into the du and a half between two cells — an opening as far as the sidestep
    /// can see, and a dead slot as far as a body is concerned. A terrace has no such slot. Past a cell's end
    /// wall is the next cell's door, which is a place a captain can actually go, and the pier between two
    /// leaves is two jambs wide — narrower than a body, so there is nowhere on this face to stand that is
    /// not in front of a door.</para>
    /// </summary>
    private static void ServiceStrip(
        Lay lay, in UndergroundComplex.RingRoom room, double uLo, double uHi, double vA, double vB)
    {
        double v = vA;

        // ── THE KITCHENETTE, in the corner nearest the door: a counter block against the pier. Its far
        //    edge is the first cell's own end wall, which is why no cell below lays one at its near end.
        double kitchenTo = v + StripDu;
        if (kitchenTo > vB)
        {
            return;
        }
        lay.Box(Fitting.Kitchenette, uLo, v, uHi, kitchenTo, KitchenettePlate);
        v = kitchenTo;

        // ── THE TWO WCs. A box, a door in the face it opens off, and a pan against the far wall.
        for (int c = 0; c < Cubicles; c++)
        {
            double to = v + CellDu;
            if (to > vB)
            {
                break;
            }
            Cell(lay, uLo, uHi, v, to, Fitting.Cubicle, WcPlate(c + 1), publish: true);
            v = to;
        }

        // ── AND THE PRIVACY BOOTHS. One seat each, facing out of the open front the way somebody on a call
        //    sits — the point of the box is what is BEHIND you.
        (double gx, double gy) = lay.Frame.TowardTheGlass;
        for (int b = 0; b < Booths; b++)
        {
            double to = v + CellDu;
            if (to > vB)
            {
                break;
            }
            Cell(lay, uLo, uHi, v, to, Fitting.Booth, BoothPlate(b + 1), publish: false);
            lay.Chair(in room, (uLo + uHi) / 2.0, (v + to) / 2.0, (-gx, -gy));
            v = to;
        }

        // ── #828 · AND THE SECURE DISPOSAL, at the end of the strip ──────────────────────────────────────
        //
        // Owner: "One thing the good offices would also have is a safe paper disposal trashes… that visually
        // destroy the notes as we watch… a more secure disposal than restaurant trash."
        //
        // LAST, and that is the whole of the placement rule: the strip is laid in the order the tier was
        // asked for, and a fitting added to the head of it would silently push a WC or a booth off the end
        // of a room that has fitted both since #817. It takes the depth that was already spare, against the
        // same pier, in the same terrace — so a captain stepping out of the last booth is standing at it.
        //
        // No door, no seat: it is a machine you stand over. What makes it a BIN is CarveBins reading this
        // fixture back off the finished room (§13.15's rule — the placer that needs to see the whole floor
        // runs last), so the box a body collides with, the plate a captain reads and the bucket the verb
        // feeds are one rectangle.
        //
        // …and it is laid with lay.Box like every other fitting in the file, which is now the whole of what
        // needs saying: #883 taught THAT method to fill its box (SurfaceLayout.AddSolidMass, hatched across
        // the short side), so the law this fixture needed most — #798's, that a bin's drawn box is the
        // walked box and the inside of it is not a place — is the law every desk and kitchenette on the ring
        // already keeps. This lane briefly carried its own solid-box helper for the one fitting that is also
        // a published RipAndBin.Bin; two idioms for one law is this repo's first named bug class, so the
        // helper went and the machine takes the building's own.
        double disposalTo = v + SecureDisposalDu;
        if (disposalTo <= vB)
        {
            lay.Box(Fitting.SecureDisposal, uLo, v, uHi, disposalTo, SecureDisposalPlate);
        }
    }

    /// <summary>One cell of the service strip: three solid sides, and the fourth split either side of the
    /// way in. The opening is the BUILDING'S own door, centred, with <see cref="CellJambDu"/> either side —
    /// see <see cref="CellDu"/> for why it may not be anything narrower.</summary>
    private static void Cell(
        Lay lay, double uLo, double uHi, double vLo, double vHi,
        Fitting kind, string plate, bool publish)
    {
        // The BOX is the fitting — laid as a fixture with no solid of its own, because a cell's four walls
        // are not four sides of a rectangle: one of them has a hole in it.
        (double x0, double y0, double x1, double y1) = lay.Frame.Box(uLo, vLo, uHi, vHi);
        if (lay.CoversTheCentre(uLo, vLo, uHi, vHi))
        {
            return;
        }

        // The FAR end and the back — the back being the pier side, which is the strip's outer face. The
        // near end is the previous cell's far end and is never laid twice: two segments where the eye reads
        // one is this repo's own named way of ending up with two answers about one wall.
        lay.Wall(uLo, vHi, uHi, vHi);
        lay.Wall(uHi, vLo, uHi, vHi);

        // …and the face it opens off, in two segments with the leaf between them.
        double mid = (vLo + vHi) / 2.0;
        lay.Wall(uLo, vLo, uLo, mid - UndergroundComplex.DoorHalf);
        lay.Wall(uLo, mid + UndergroundComplex.DoorHalf, uLo, vHi);

        var box = new Fixture(
            kind, x0, y0, x1, y1, plate,
            kind == Fitting.Booth ? Seating.OneSide : Seating.None);

        if (publish)
        {
            SurfaceLayout.Doorway leaf = lay.Door(
                uLo, mid - UndergroundComplex.DoorHalf, uLo, mid + UndergroundComplex.DoorHalf);

            // The fixture, against the back wall — the en-suite's own one-segment pan, which is the whole of
            // what makes a 5 du box read as a WC on a plan.
            lay.Wall(uHi - PanInsetDu, mid - PanHalfDu, uHi - PanInsetDu, mid + PanHalfDu);

            // #821 · …and the cell as the thing a LOCK lands on: the box, its own leaf, the square you sit
            // on in the middle of it and the square somebody knocks from a door's clearance outside it.
            lay.Cubicle(
                in box, in leaf, (uLo + uHi) / 2.0, mid, uLo - DoorClearDu, mid, plate);
        }

        lay.Note(box);
    }
}
