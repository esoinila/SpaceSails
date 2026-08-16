using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: the layout — one room furnished, and the kits it is furnished from.
public static partial class RingOffice
{
    // ── THE LAYOUT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #817 · FURNISH ONE ROOM. Handed the box the ring carved, it answers what is standing in it.
    ///
    /// <para>Everything is laid in the room's own u/v grid and mapped out at the end, so nothing below knows
    /// or cares which of the park's four walls this room is one of.</para>
    /// </summary>
    public static Furnishing Fit(in UndergroundComplex.RingRoom room)
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

        switch (DressingFor(in room))
        {
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

    /// <summary>
    /// THE BANKS — rows of workstations down the room, every one of them facing the glass.
    ///
    /// <para>A bank stops short of one pier and the next stops short of the other, so the way through the
    /// room is a weave rather than a corridor and a row is four segments rather than eight. The screens
    /// between the workstations reach BACK past the chairs, which is the whole difference between a cubicle
    /// and a table, and is the word the owner used.</para>
    /// </summary>
    private static void Banks(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        double width = uB - uA;
        if (width < 4.0 || vB - vA < DeskHalfDepthDu)
        {
            return;
        }

        // The aisle is a QUARTER of the run rather than a fixed six du wherever the run is short: a 19 du
        // end block's bank measured against the big suites' gangway is a bank with one chair at it, which
        // is the room the owner complained about with a desk added to it.
        double aisle = Math.Min(BankAisleDu, width * 0.3);

        for (int r = 0; r < MaxDeskRows; r++)
        {
            double vRow = vA + CubicleReachDu + (r * RowPitchDu);
            if (vRow + DeskHalfDepthDu > vB)
            {
                break;
            }
            vRow = ClearOfCentre(vRow, lay.VCentre, DeskHalfDepthDu);

            // What has to fit after the shift is the CHAIR, not the screen behind it: a screen that would
            // reach into the street aisle is clamped to the aisle's edge below and is simply a shorter
            // screen. A guard that demanded the screen's whole reach instead dropped every bank in the
            // block's two end suites — watched happen, and the symptom was four furnished rooms and four
            // empty ones on the same floor.
            if (vRow - ChairSetbackDu < vA - 0.001 || vRow + DeskHalfDepthDu > vB + 0.001)
            {
                continue;
            }

            // The aisle changes ends every row: the weave.
            double bankLo = (r % 2 == 0) ? uA : uA + aisle;
            double bankHi = (r % 2 == 0) ? uB - aisle : uB;

            if (!lay.Box(
                    Fitting.DeskBank, bankLo, vRow - DeskHalfDepthDu, bankHi, vRow + DeskHalfDepthDu, "",
                    Seating.OneSide))
            {
                continue;
            }

            int seats = Math.Max(MinSeatsPerBank, (int)((bankHi - bankLo) / SeatPitchDu));
            double chairV = vRow - ChairSetbackDu;
            for (int s = 0; s < seats; s++)
            {
                double u = bankLo + ((bankHi - bankLo) * (s + 0.5) / seats);
                lay.Chair(in room, u, chairV, lay.Frame.TowardTheGlass);

                // …and the screen between this workstation and the last one.
                if (s == 0)
                {
                    continue;
                }
                double screenU = bankLo + ((bankHi - bankLo) * s / seats);
                lay.Box(
                    Fitting.Partition,
                    screenU, Math.Max(vA, vRow - CubicleReachDu), screenU, vRow - DeskHalfDepthDu, "");
            }
        }
    }

    /// <summary>
    /// THE NEGOTIATION ROOM — one long table, chairs down both sides of it.
    ///
    /// <para>It runs the DEPTH of the room rather than its width, and it is deliberately off the room's own
    /// centre line: a table laid where a boardroom table goes stands on the exact square the ring's audit
    /// walks to. Off-centre is also the truer picture — the head of the table is the end at the window.</para>
    /// </summary>
    private static void LongTable(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA, double vB)
    {
        double reach = TableHalfWidthDu + TableChairGapDu;
        if (uB - uA < 2 * (reach + 1.0) || vB - vA < TableSeatPitchDu)
        {
            Banks(lay, in room, uA, uB, vA, vB);
            return;
        }

        double uT = Math.Clamp(
            ClearOfCentre(lay.UCentre - reach, lay.UCentre, TableHalfWidthDu),
            uA + reach, uB - reach);

        if (!lay.Box(
                Fitting.Table, uT - TableHalfWidthDu, vA, uT + TableHalfWidthDu, vB, TablePlate,
                Seating.BothSides))
        {
            Banks(lay, in room, uA, uB, vA, vB);
            return;
        }

        int seats = Math.Max(1, (int)((vB - vA) / TableSeatPitchDu));

        // Across the table from each other, which is what a negotiation is. They face ALONG the room's own
        // frontage rather than at the glass, off the axis the grid is laid in
        // (<see cref="Frame.AlongTheFrontage"/>).
        //
        // #868 · IT WAS A ROTATION OF THE GLASS NORMAL AND IT WAS WRONG ON ONE BAND IN FOUR. The rotation
        // `(-gy, gx)` gives +u on the near, far and west sides and −u on the EAST, because that side's grid
        // is the one whose depth axis runs backwards (<c>Frame.At</c>: <c>_x1 - v</c>). So every negotiation
        // room on the east band seated four people facing away from their own table, and nothing said so
        // until #868's facing guard asked the question — 48 seats across the sweep, on a floor plan where
        // both chairs and table are drawn and neither is drawn with a front.
        (double ax, double ay) = lay.Frame.AlongTheFrontage;
        for (int s = 0; s < seats; s++)
        {
            double v = vA + ((vB - vA) * (s + 0.5) / seats);
            lay.Chair(in room, uT - reach, v, (ax, ay));
            lay.Chair(in room, uT + reach, v, (-ax, -ay));
        }
    }

    /// <summary>
    /// THE RECEPTION COUNTER — across the room a pace inside the door, with the staff behind it looking at
    /// whoever just came through. One end left open, because a counter that reaches both piers is a wall
    /// with a story about it.
    ///
    /// <para>#827 · Seated ON ONE SIDE, which is the owner's own reading of what a counter IS — <i>the
    /// biggest table, with seats on one side only</i>. So this seats a ROW rather than a person: the same
    /// arithmetic a desk bank uses, on the same pitch, facing the other way. It is also what keeps a
    /// reception suite over <see cref="SeatsPerViewSuite"/> in a 19 du end block, where the counter and its
    /// gangway leave no depth for a bank behind it.</para>
    ///
    /// <para>Returns the far edge of what it laid, so the caller knows where the floor starts again.</para>
    /// </summary>
    private static double Counter(
        Lay lay, in UndergroundComplex.RingRoom room, double uA, double uB, double vA)
    {
        if (uB - uA < 4.0)
        {
            return vA;
        }

        double v = ClearOfCentre(vA + CounterStandoffDu, lay.VCentre, CounterHalfDepthDu);
        double open = Math.Min(BankAisleDu, (uB - uA) * 0.3);
        double runHi = uB - open;
        if (!lay.Box(
                Fitting.Counter, uA, v - CounterHalfDepthDu, runHi, v + CounterHalfDepthDu,
                ReceptionPlate, Seating.OneSide))
        {
            return vA;
        }

        // The seats that face the DOOR rather than the view: the whole of what a reception is.
        (double gx, double gy) = lay.Frame.TowardTheGlass;
        double seatV = v + CounterHalfDepthDu + ChairSetbackDu;
        int seats = Math.Max(MinSeatsPerBank, (int)((runHi - uA) / SeatPitchDu));
        for (int s = 0; s < seats; s++)
        {
            lay.Chair(in room, uA + ((runHi - uA) * (s + 0.5) / seats), seatV, (-gx, -gy));
        }

        return seatV;
    }

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
