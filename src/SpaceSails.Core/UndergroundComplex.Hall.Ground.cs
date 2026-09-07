using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #751/#759 · WHERE A HALL MAY STAND — which column it takes, whether this floor has one at all, what it
/// is for, and whether the ground on the chosen column will actually hold one.
///
/// <para>"Nearest the car, every time", but only among the slots that CAN take a hall: the ground is asked
/// FIRST (<see cref="HallGround"/>, the same arithmetic the carve itself uses, so the two can never
/// disagree), the best floor wins, and nearest-the-car breaks the tie.</para>
///
/// <para>Split out of <c>UndergroundComplex.Hall.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class UndergroundComplex
{
    /// <summary>
    /// #751 · WHICH COLUMN THE HALL STANDS ON — the same criterion #707 already uses for the canteen, asked
    /// one step earlier.
    ///
    /// <para>"Nearest the car, every time": a building puts its catering by the lift, and a bar you have to
    /// go looking for is not a bar anybody drank in on a shift. The old carve chose the nearest ROOM out of
    /// the rooms the floor had built; a hall has to be chosen before any room exists, so this asks the same
    /// question of the room SLOTS — the very positions <see cref="RoomCentresAlong"/> is about to place. Same
    /// answer, computed from the same arithmetic, one pass earlier.</para>
    ///
    /// <para>#759 · …<b>of the slots that can actually hold one.</b> Nearest-the-car on its own put every
    /// hall in the game on the rib beside the shaft, and on the floors whose rib runs UP that is the one
    /// column in the building with the lift alcove standing in front of it — so the room the owner called
    /// cramped was cramped by a fixture thirty du away, and no amount of spreading its tables could have
    /// answered him. The ground is asked FIRST now (<see cref="HallGround"/>, the same arithmetic the carve
    /// itself uses, so the two can never disagree), the best floor wins, and nearest-the-car breaks the tie
    /// — which it does on every floor where two slots would both take the hall whole, i.e. the rule is
    /// unchanged everywhere it was ever doing any work.</para>
    /// </summary>
    private static (int Rib, int Side)? HallSlotFor(
        string bodyId, int level, List<Rib> ribs, in SurfaceLayout.Field field,
        double shaftX, double? serviceX, double shaftY, double leftEnd, double rightEnd, double roomScale)
    {
        if (!IsHallFloor(bodyId, level) || ribs.Count == 0)
        {
            return null;
        }

        Comfort use = HallUseOn(bodyId, level);
        double roomW = RoomWidthDu * roomScale;
        (int Rib, int Side)? best = null;
        double bestFloor = -1, bestD2 = double.MaxValue;

        for (int i = 0; i < ribs.Count; i++)
        {
            (double mouth, double far) = RibReach(field, shaftY, ribs[i].Down, hall: true);
            List<double> ys = RoomCentresAlong(mouth, far, ribs[i].Down, roomScale);
            if (ys.Count == 0)
            {
                continue;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                if (HallGround(
                        bodyId, use, ribs, i, side, mouth, far, shaftX, serviceX, leftEnd, rightEnd,
                        roomScale)
                    is not { } ground)
                {
                    continue;   // the ground here would not take a hall at all
                }

                // The floor this slot would yield, and never more than the hall ASKED for — so two slots
                // that both take it whole are equal and the tie falls to the lift, which is the #751 rule.
                double floor = Math.Min(ground.Width, ground.Wanted) * ground.Length;

                double cx = ribs[i].X + (side * (CorridorHalf + (roomW / 2)));
                double d2 = double.MaxValue;
                foreach (double cy in ys)
                {
                    double dx = cx - shaftX, dy = cy - shaftY;
                    d2 = Math.Min(d2, (dx * dx) + (dy * dy));
                }

                if (floor > bestFloor + 0.5 || (floor > bestFloor - 0.5 && d2 < bestD2))
                {
                    (best, bestFloor, bestD2) = ((i, side), Math.Max(floor, bestFloor), d2);
                }
            }
        }

        return best;
    }

    /// <summary>#751 · Does this floor get a hall at all? The two customers of the carve, asked in one
    /// place: the floor the bar is on, and the floor the mess is on.</summary>
    public static bool IsHallFloor(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return TopPressurisedFloor(bodyId) == level || StaffCanteenFloor(bodyId) == level;
    }

    /// <summary>#751 · Which hall this floor's is. The mess wins where a site is shallow enough for the two
    /// to land on the same floor — but <see cref="StaffCanteenFloor"/> returns null in exactly that case, so
    /// this is belt and braces rather than a rule.</summary>
    private static Comfort HallUseOn(string bodyId, int level) =>
        TopPressurisedFloor(bodyId) == level ? Comfort.UpperCanteen : Comfort.StaffCanteen;

    /// <summary>
    /// #759 · HOW MUCH GROUND A SLOT HAS FOR A HALL, AND HOW MUCH THE HALL WANTS — the arithmetic
    /// <see cref="CarveHall"/> used to do inline, lifted out whole so <see cref="HallSlotFor"/> can ask the
    /// same question one pass earlier.
    ///
    /// <para>It is lifted rather than copied for the reason this file opens with a table of: a chooser that
    /// worked out available width on its own would be a second opinion about a room the carve owns, and the
    /// two would drift apart the first time either grew a clause. Null means this ground will not take a
    /// hall at all.</para>
    /// </summary>
    private static (double Width, double Wanted, double Length, double VSpan)? HallGround(
        string bodyId, Comfort use, List<Rib> ribs, int ribIndex, int side,
        double mouth, double far, double shaftX, double? serviceX, double leftEnd, double rightEnd,
        double roomScale)
    {
        double ribX = ribs[ribIndex].X;
        double roomW = RoomWidthDu * roomScale;

        // ── HOW FAR OUT THE GROUND GOES. Clamped against the things that are already spoken for rather
        //    than against a guess: the next rib's chambers, the lift alcove where the hall shares a spine
        //    face with it, and the spine's own end cap.
        //
        // #759 · …and a rib pointing the OTHER WAY off the spine is not in the way of anything. This used
        // to reserve a full room column beside EVERY neighbouring rib, which on half the floors in the game
        // was ground held back for chambers standing on the far side of the spine — sixty du of rock the
        // hall was refused because of rooms it could not have reached with a drill. The clamp asks which
        // way the neighbour runs now, and against a neighbour that shares this band it stops at the
        // CORRIDOR rather than at the far side of that corridor's rooms: those room slots are ground, the
        // claim ledger drops what stands on the hall, and a passage is the one thing that may never be
        // covered.
        double limit = side > 0 ? rightEnd - HallEdgePadDu : leftEnd + HallEdgePadDu;
        foreach (Rib other in ribs)
        {
            if (other.Down != ribs[ribIndex].Down)
            {
                continue;
            }
            if (side > 0 && other.X > ribX)
            {
                limit = Math.Min(limit, other.X - CorridorHalf - 1.5);
            }
            else if (side < 0 && other.X < ribX)
            {
                limit = Math.Max(limit, other.X + CorridorHalf + 1.5);
            }
        }
        // The lift alcove hangs off the TOP face, so only a rib that runs UP can meet it. #585 was a wall
        // lying across a mouth; a hall laid over the alcove would be the same mistake with the captain's own
        // way home inside it.
        //
        // #759 · …and only where the alcove is actually in the way, which is the clause this shipped
        // without. A hall growing LEFT off a rib that is already left of the shaft was being clamped to a
        // limit on the shaft's far side — a lower bound raised above the wall it was bounding, so
        // `available` came out as the distance to a point behind the hall and the room was laid seventy du
        // outside the field. It never fired while every hall in the game stood on the rib beside the car;
        // the moment #759 let the chooser look at the other seven slots, it laid a bar through the edge of
        // the world. A clamp that does not ask which side its obstacle is on is not a clamp.
        if (!ribs[ribIndex].Down)
        {
            if (side > 0 && shaftX > ribX)
            {
                limit = Math.Min(limit, shaftX - ShaftHalf - 1.5);
            }
            else if (side < 0 && shaftX < ribX)
            {
                limit = Math.Max(limit, shaftX + ShaftHalf + 1.5);
            }
        }

        // #801 · …and the GOODS CAR's alcove, which hangs off the LOWER face, so it is the ribs running DOWN
        // that can meet it. The same two lines the other way up — stated as its own clause rather than as a
        // loop over a list of cars, because the clause a car needs is WHICH FACE it is on, and a list that
        // had lost that would be a clamp that does not ask which side its obstacle is on.
        if (ribs[ribIndex].Down && serviceX is { } carX)
        {
            if (side > 0 && carX > ribX)
            {
                limit = Math.Min(limit, carX - ShaftHalf - 1.5);
            }
            else if (side < 0 && carX < ribX)
            {
                limit = Math.Max(limit, carX + ShaftHalf + 1.5);
            }
        }

        double faceX = ribX + (side * CorridorHalf);
        double available = Math.Abs(limit - faceX);
        double length = Math.Abs(far - mouth);

        // ── WHAT THE HALL NEEDS. The tops come first: the seat target decides the bill, the bill decides
        //    how many tops, and the tops decide how deep the room has to be at the pitch a hall lays its
        //    tables out at (HallSpreadFactor — the owner's "at least double or triple it").
        // …and the cabinets are the cantina's alone. The mess is the room the shift stopped coming to; a
        // door for sensitive negotiations in it would be furnishing a joke nobody is in the room to make.
        double cabBand = use == Comfort.UpperCanteen ? HallCabinetDepthDu : 0.0;

        int tops = HallSeatBill(bodyId, use, HallSeatsFor(bodyId, use)).Count;
        double pitch = HallTopPitchDu;
        double vSpan = length - (2 * HallEdgePadDu) - HallCounterBandDu - HallEdgePadDu;
        int rowsAtPitch = Math.Max(1, (int)(vSpan / pitch));
        int colsNeeded = (tops + rowsAtPitch - 1) / rowsAtPitch;
        double wanted = HallDoorAisleDu + (colsNeeded * pitch) + HallEdgePadDu + cabBand;

        double width = Math.Min(available, wanted);

        // A hall that cannot hold its own doorways, its aisle and a table strip is not a hall. Saying so
        // and standing down is the honest answer; a guard asserts it never actually happens.
        double minWidth = HallDoorAisleDu + cabBand + HallEdgePadDu + (2 * DoorHalf);
        return width < minWidth || vSpan < 4 * DoorHalf ? null : (available, wanted, length, vSpan);
    }
}
