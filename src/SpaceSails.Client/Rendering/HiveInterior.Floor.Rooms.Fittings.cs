using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #251 · THE ROOMS' FITTINGS — the cubicles, the basin run, the meeting rooms, the bin plates, the
/// chambers and the desk controls.
///
/// <para>Split out of <c>HiveInterior.Floor.Rooms.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no static field.</para>
/// </summary>
public static partial class HiveInterior
{
    /// <summary>
    /// #821 - FITS THE CUBICLES: TWO consoles per cell and deliberately not one, because they are two verbs
    /// three du apart - the LEAF at the opening, whose plate is the state a guard walking past can read, and
    /// the SEAT at the pan. Lays down: consoles.
    /// </summary>
    private static void FitTheCubicles(
        List<DeckPlan.ConsoleSpot> consoles, in UndergroundComplex.RingRoom suite, int level,
        IReadOnlyCollection<string>? cubiclesShut)
    {
        // ── #821 · THE CUBICLES: A LEAF YOU PRESS, AND A SEAT INSIDE ──────────────────────────
        //
        // Owner, standing in the park: "we might want to hide from guards in one toilet cubicle we
        // lock from inside :-D"
        //
        // TWO consoles per cell and deliberately not one, because they are two verbs and the room
        // separates them: the LEAF is at the opening and the SEAT is at the pan, three du apart —
        // which is exactly DeckPlan.InteractRadius, so a captain at the door presses the door and a
        // captain at the pan presses the pan. A single console that meant "lock" or "sit down"
        // depending on which half of a six-du box you stood in would be one key doing two things
        // with no way for the plate to say which.
        //
        // The leaf's plate is the STATE (CubicleLock.PlateFor): a washroom door says VACANT or
        // OCCUPIED, and that is the whole of what a guard walking past this room can read.
        foreach (RingOffice.Stall cell in suite.Cubicles)
        {
            bool over = cubiclesShut is not null
                && cubiclesShut.Contains(CubicleKey(level, in cell));

            consoles.Add(new(
                DeckPlan.ConsoleKind.HiveCubicle,
                (float)cell.DoorX, (float)cell.DoorY, CubicleLock.PlateFor(over)));

            // The seat takes the OFFICE CHAIR's verb, on the same seam and with the same snap
            // (#820): Core says where the seat is and this hangs the press on it. The plate is the
            // cell's own, so a captain reads which cubicle they are stepping into rather than
            // "AN OFFICE CHAIR" in a WC.
            consoles.Add(new(
                DeckPlan.ConsoleKind.HiveOfficeChair,
                (float)cell.SeatX, (float)cell.SeatY, cell.Plate));
        }
    }

    /// <summary>
    /// #821/#827 - PLUMBS THE BASIN RUN as ONE fixture the length of itself - #791's E-bus borrowed whole,
    /// taken off the two end taps so nothing here measures a length of porcelain. Lays down: consoles.
    /// </summary>
    private static void PlumbTheBasinRun(List<DeckPlan.ConsoleSpot> consoles,
        in UndergroundComplex.RingRoom suite)
    {
        // ── #821 · AND THE BASIN RUN, AS ONE FIXTURE THE LENGTH OF ITSELF ─────────────────────
        //
        // #791's E-bus, borrowed whole: the run is one console with a LENGTH on it rather than a tap
        // per basin, for the reason the bar desk is — four [E] dots in a row all opening the same
        // beat would be three pieces of furniture pretending to be a choice. The run is taken off
        // the published taps, so nothing here measures a length of porcelain.
        //
        // #827 · …as the two END TAPS rather than a half-span about their midpoint, which is what a
        // run became when the counter's plate stopped standing at the middle of its own desk. Here
        // the two are the same segment either way; there it is the whole issue.
        if (suite.Basins.Count > 0)
        {
            RingOffice.Basin first = suite.Basins[0], last = suite.Basins[^1];
            consoles.Add(new(
                DeckPlan.ConsoleKind.HiveBasin,
                (float)((first.X + last.X) / 2.0), (float)((first.Y + last.Y) / 2.0),
                RingOffice.BasinRunPlate,
                Run: ((float)first.X, (float)first.Y, (float)last.X, (float)last.Y)));
        }
    }

    /// <summary>
    /// #775 - FURNISHES THE MEETING ROOMS IN THE CORE. They take the ring's own two lines because they are
    /// the ring's own furniture - the FILL, the STENCIL and the VERB, with every solid thing already poured.
    /// Lays down: consoles, labels, furniture.
    /// </summary>
    private static void FurnishTheMeetingRooms(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.FurnitureSpot> furniture,
        in UndergroundComplex.FloorPlan floor)
    {
        // ── #775 · THE MEETING ROOMS IN THE CORE ──────────────────────────────────────────────────────
        //
        // Owner: "lots of meeting rooms — glass-box rooms off the open floor", and they take the ring's own
        // two lines because they are the ring's own furniture: every solid thing in one is already in
        // floor.Walls (Core laid the table), so what is left for a renderer is the FILL, the STENCIL and the
        // VERB. The chairs take the universal seat verb (#757/#820) on the same seam a suite's do.
        foreach (UndergroundComplex.MeetingRoom cell in floor.TheMeetingRooms)
        {
            foreach (RingOffice.Fixture fitting in cell.Furniture)
            {
                if (fitting.Plate.Length > 0)
                {
                    labels.Add(((float)fitting.X, (float)fitting.Y, fitting.Plate));
                }
                Furnish(furniture, in fitting);
            }
            foreach (RingOffice.Chair chair in cell.Seats)
            {
                consoles.Add(new(
                    DeckPlan.ConsoleKind.HiveOfficeChair,
                    (float)chair.X, (float)chair.Y, chair.DeckPlate));
            }
        }
    }

    /// <summary>
    /// #798 - PLATES THE BINS and nothing else: the box is already poured, and tearing a document up is the
    /// SATCHEL'S verb, so an [E] here would answer nothing. The secure rung is skipped, because it is a
    /// suite's own furnishing and is plated by the pass that stood it. Lays down: labels.
    /// </summary>
    private static void PlateTheBins(List<(float X, float Y, string Text)> labels,
        in UndergroundComplex.FloorPlan floor)
    {
        // ── #798 · THE BINS, PLATED ────────────────────────────────────────────────────────────────────
        //
        // Owner: "those trash cans are needed so we get rid of the processed materials without connecting
        // them to us too clearly, like leaving them to the table."
        //
        // The BOX is already in floor.Walls — Core stood it there, so it was drawn and collided with by the
        // loop at the top of this method, and one rectangle carries both halves. This is the stencil on it
        // and nothing else, which is the whole of a renderer's business in a room somebody else carved.
        //
        // Plated and NOT a console, the park bench's own idiom one room over: tearing a document up is the
        // SATCHEL'S verb (you have to be holding the thing), and an [E] on a bin that answered nothing would
        // be #757's complaint restated in a corridor.
        foreach (RipAndBin.Bin bin in floor.TheBins)
        {
            // #828 · …except the secure rung, whose box is a SUITE'S OWN FURNISHING
            // (RingOffice.SecureDisposal) and is plated by the pass that stood it. A stencil here as well
            // would draw one machine as two — the same plate twice, a couple of du apart.
            if (bin.Tier == RipAndBin.Tier.SecureDisposal)
            {
                continue;
            }

            labels.Add((
                (float)bin.X, (float)(bin.Y - RipAndBin.HalfDu - 1.4), bin.Plate));
        }
    }

    /// <summary>
    /// #818 - FURNISHES EVERY OTHER ROOM IN THE BUILDING - never ever empty floor. One stencil per room
    /// (Core's doing, not a filter here), the fill, and the seat this floor's trade sits you on, which is
    /// asked ONCE per floor because a department does not change room to room. Lays down: consoles, labels,
    /// furniture.
    /// </summary>
    private static void FurnishTheChambers(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.FurnitureSpot> furniture,
        in UndergroundComplex.FloorPlan floor, string bodyId, int level, UndergroundComplex.Kind kind)
    {
        // ── #818 · WHAT IS STANDING IN EVERY OTHER ROOM IN THE BUILDING ────────────────────────────────
        //
        // Owner, generalising #817 past the ring: "Same for labs etc spaces… they have chairs and desks and
        // equipment … never ever empty floor."
        //
        // EVERY SOLID THING IN HERE IS ALREADY IN floor.Walls — Core laid the benches, the fume hoods, the
        // racking and the machines as segments, so they were drawn and collided with by the loop at the top
        // of this method, exactly as the ring's desks and the en-suite's pan are. What is left for a renderer
        // is the STENCIL and the VERB, which is the whole of a renderer's business in a room somebody else
        // furnished.
        //
        // ONE STENCIL PER ROOM, and that is Core's doing rather than a filter here: the placer plates the
        // signature fitting and hands the rest over blank, because a plate over every bay of racking in a
        // long store is a floor nobody can read. The `if` below is the ring's own (#817) and is what makes
        // that decision visible from this end.
        // #869 · …and WHICH SEAT this floor's trade sits you on. Core answers it (ChamberFitting.SeatPlateFor
        // off the room's own kit): a laboratory perches you on a saddle stool, an administration chamber
        // seats you in an office chair, and everything else keeps the stool it has had since #818. Asked once
        // per floor rather than once per room, because a floor's department does not change room to room.
        string? department = ChamberFitting.DepartmentOn(bodyId, level);

        for (int r = 0; r < floor.TheRooms.Count; r++)
        {
            UndergroundComplex.Room room = floor.TheRooms[r];
            if (room.Kind != UndergroundComplex.RoomKind.Chamber)
            {
                continue;   // the suites, the hall and its booths are drawn by their own passes above
            }

            foreach (RingOffice.Fixture fitting in room.Furniture)
            {
                if (fitting.Plate.Length > 0)
                {
                    labels.Add(((float)fitting.X, (float)fitting.Y, fitting.Plate));
                }

                // #868 · The same fill the ring's furniture takes. A chamber's kit is laid as SEGMENTS on
                // purpose (ChamberFitting's class summary: Lab 45 says the sightline is O(walls), and four
                // rectangles per room in fifteen hundred rooms is a frame budget spent drawing cupboards),
                // so most of these are degenerate and the pen skips them — the call is made anyway, so the
                // day a chamber's bench grows a depth it is drawn without anybody remembering this line.
                // #869 · …which is exactly the day that arrived: the desks and the benches have one now.
                Furnish(furniture, in fitting);
            }

            // The stools take the SIT verb on the seam the office chairs, the stools at the bar and the park
            // benches already use: Core says where a seat is and this hangs a console on it. The plate
            // carries the VERB (#783: "why not use words like SIT DOWN here if it means sitting down?").
            string seatPlate = ChamberFitting.SeatPlateFor(
                ChamberFitting.KitFor(room.Plate, department, kind));
            foreach (RingOffice.Chair seat in room.Seats)
            {
                consoles.Add(new(
                    DeckPlan.ConsoleKind.HiveOfficeChair,
                    (float)seat.X, (float)seat.Y, seatPlate));
            }
            WorkTheDeskControls(consoles, in room);
        }
    }

    /// <summary>
    /// #869 - HANGS THE DESK'S OWN TWO CONTROLS: ONE PAIR PER DESK and never one per seat, because a long
    /// bench with two stools at it is one desk with one motor. Which desk a seat belongs to and where the two
    /// paddles go are both Core's answers. Lays down: consoles.
    /// </summary>
    private static void WorkTheDeskControls(
        List<DeckPlan.ConsoleSpot> consoles, in UndergroundComplex.Room room)
    {
        // ── #869 · AND THE DESK'S OWN TWO CONTROLS ────────────────────────────────────────────────
        //
        // Owner, from his own electric desk: "it got up- and down buttons to move the table to work
        // either with office chair, Salli standing (lab) chair or by standing while using the table."
        //
        // ONE PAIR PER DESK and never one per seat: a long bench with two stools at it is one desk with
        // one motor, and a second paddle at the same end would be two consoles fighting over one press.
        // Which desk a seat belongs to is Core's answer (SitStandDesk.DeskFor, measured off the setback
        // the placer laid the chair at), and so are the two coordinates — this file hangs a console on
        // them and measures nothing (§13.15).
        for (int f = 0; f < room.Furniture.Count; f++)
        {
            RingOffice.Fixture desk = room.Furniture[f];
            if (!SitStandDesk.HasAMotor(desk.Kind))
            {
                continue;
            }
            foreach (RingOffice.Chair seat in room.Seats)
            {
                if (SitStandDesk.DeskFor(room.Furniture, in seat) != f)
                {
                    continue;
                }

                (double ex, double ey) = SitStandDesk.TheEdge(in desk, in seat);
                (double bx, double by) = SitStandDesk.TheButtons(in desk, in seat);
                consoles.Add(new(DeckPlan.ConsoleKind.HiveDeskEdge,
                    (float)ex, (float)ey, SitStandDesk.EdgePlate));
                consoles.Add(new(DeckPlan.ConsoleKind.HiveDeskPresets,
                    (float)bx, (float)by, SitStandDesk.ButtonsPlate));
                break;
            }
        }
    }
}
