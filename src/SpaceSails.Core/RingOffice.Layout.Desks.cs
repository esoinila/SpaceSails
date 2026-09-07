using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// WHAT A ROOM WITH A VIEW GETS — the banks of workstations facing the glass, the negotiation room's one
/// long table, and the reception counter a pace inside the door.
///
/// <para>A bank stops short of one pier and the next stops short of the other, so the way through the
/// room is a weave rather than a corridor. The table runs the DEPTH of the room and is deliberately off
/// its centre line, which is both the audit's requirement and the truer picture: the head of the table is
/// the end at the window. And the counter seats a ROW rather than a person — the owner's own reading of
/// what a counter IS, <i>the biggest table, with seats on one side only</i>.</para>
///
/// <para>Split out of <c>RingOffice.Layout.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class RingOffice
{
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
}
