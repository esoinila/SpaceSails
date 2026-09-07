using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// Part of <see cref="HiveInterior"/> (the header note lives in HiveInterior.cs) - THE PASSES THAT DRESS
/// THE ROOMS: the indoor park and its benches, the ring's suites with their cubicles and basins, the
/// glass-box meeting rooms in the core, the bins, and every other chamber in the building.
///
/// <para>#1164 - Each was a <c>// -- banner --</c> section inside <see cref="FloorDeck"/> and is now a
/// named pass, called in the order the banners stood in. EVERY SOLID THING IN THESE ROOMS IS ALREADY IN
/// <c>floor.Walls</c>: Core laid the beds, the desks, the pans, the racking and the bin boxes as
/// segments, and <see cref="PourTheStructure"/> drew and collided with them at the top of the deck. What
/// is left here is the STENCIL, the FILL and the VERB, which is the whole of a renderer's business in a
/// room somebody else furnished (13.15).
/// </para>
///
/// <para>The two loops that carry a <c>continue</c> keep it: <see cref="FurnishTheSuites"/> cuts a shut
/// suite off between the pictures and the verbs, and <see cref="FurnishTheChambers"/> skips the rooms
/// drawn by their own passes above. The cut stays in the conductor, beside the comment that explains it,
/// rather than becoming an early <c>return</c> inside a pass that could not say why.</para>
/// </summary>
public static partial class HiveInterior
{
    /// <summary>
    /// #759 - DRAWS THE PARK: the floor art in panels Core cut, the plate at the gate, what is in each bed
    /// and where it goes when it is picked, the benches, and the one figure on the far one. Every solid thing
    /// in it is already poured. Lays down: consoles, labels, backdrops, benchSeats.
    /// </summary>
    private static void DrawThePark(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.Backdrop> backdrops, List<DeckPlan.BenchSpot> benchSeats,
        in UndergroundComplex.FloorPlan floor)
    {
        // ── #759 · THE PARK, DRAWN ─────────────────────────────────────────────────────────────────────
        //
        // Owner: "on map the park needs to Exist there next to the bar. It is an indoor park where the fresh
        // stuff is grown that is served here on the plate."
        //
        // Every solid thing in it — the walls, the raised beds, the benches, the floodlight masts — is
        // already in floor.Walls and was therefore drawn and collided with by the loop at the top of this
        // method. Nothing here lays out a bed or decides where a bench goes; this is the SIGNAGE and the
        // floor art, which is the whole of a renderer's business in a room Core carved.
        if (floor.Park is { } green)
        {
            // The floor it wears, in panels. One 16:9 frame stretched over a room six times wider than it is
            // deep would be a smear; Core cuts the box (ParkArtPanels) because that is geometry about a room
            // this file does not own.
            if (green.ArtUrl is { } parkArt)
            {
                foreach ((double px0, double _, double px1, double py1) in
                    UndergroundComplex.ParkArtPanels(green))
                {
                    backdrops.Add(new(
                        parkArt, (float)px0, (float)py1,
                        (float)(px1 - px0), (float)(green.Y1 - green.Y0),
                        UndergroundComplex.HallArtAlpha));
                }
            }

            // The plate at the gate, in the inspectorate voice the room is stencilled in.
            labels.Add(((float)green.X, (float)green.Y, UndergroundComplex.ParkPlate));

            // What is in each bed, and where it goes when it is picked. THE FOOD CONNECTION IS THIS LINE OF
            // STENCIL and nothing else: the bed says CANTEEN 1 and so does the counter, and no card ever
            // points that out.
            foreach (UndergroundComplex.GrowingBed bed in green.Beds)
            {
                labels.Add(((float)bed.X, (float)bed.Y, bed.Plate));
            }

            SeatTheParkBenches(consoles, labels, benchSeats, in green);

            // …and somebody on the far bench, who is scenery. Owner: "benches, the lone figure, the curve
            // that hides the far end." A plate at a coordinate, with nothing to press: a park that started
            // offering things would be a park that had noticed you.
            labels.Add((
                (float)green.FigureX, (float)(green.FigureY + 2.2), green.FigurePlate));
        }
    }

    /// <summary>
    /// #793 - THE BENCHES TAKE THE SIT VERB. A bench is a console of its own kind now, and its two ends go
    /// onto the plan as SEATS in the counter's own idiom, so the deck can answer whether the WHOLE bench is
    /// free from across the room. Lays down: consoles, labels, benchSeats.
    /// </summary>
    private static void SeatTheParkBenches(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.BenchSpot> benchSeats, in UndergroundComplex.Park green)
    {
        // ── #793 · THE BENCHES TAKE THE SIT VERB ───────────────────────────────────────────────────
        //
        // #790 shipped them as plates over solid furniture and said so in this very comment: "sitting
        // down is #778's verb and arrives with it." It has arrived. A bench is a CONSOLE now, of its own
        // kind, and the plate says the verb the way a free table's does (#783: "why not use words like
        // SIT DOWN here if it means sitting down?").
        //
        // WHO IS ON WHICH BENCH IS CORE'S — ParkBenches.On reads #790's own lone figure and nothing was
        // populated to make the answer interesting. The two ends go onto the plan as SEATS, in the
        // counter's own idiom (#792/#795), so the deck can answer "is the whole bench free" from across
        // the room: that is the question the privacy predicate is written on.
        foreach (ParkBenches.Bench bench in ParkBenches.On(in green))
        {
            labels.Add(((float)bench.X, (float)(bench.Y - 2.2), UndergroundComplex.ParkBenchPlate));
            consoles.Add(new(
                DeckPlan.ConsoleKind.HiveBench, (float)bench.X, (float)bench.Y, bench.DeckPlate));

            for (int end = 0; end < ParkBenches.Ends; end++)
            {
                (double ex, double ey) = bench.End(end);
                benchSeats.Add(new(
                    (float)ex, (float)ey,
                    Taken: bench.Taken && end == ParkBenches.TakenEnd,
                    BenchHasSomebody: bench.Taken));
            }
        }
    }

    /// <summary>
    /// #817/#775 - FURNISHES THE SUITES on the floor's own ring, asked of the FLOOR and not of the garden,
    /// because a landscape floor has a ring and no park. Calls the passes below in the order their banners
    /// stood in, with the shut-suite cut between the pictures and the verbs.
    /// </summary>
    private static void FurnishTheSuites(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.FurnitureSpot> furniture,
        in UndergroundComplex.FloorPlan floor, int level, long canteenWatch, RoomBooking.Booking? booked,
        IReadOnlyCollection<string>? cubiclesShut)
    {
        // ── #817/#775 · THE SUITES, FURNISHED ─────────────────────────────────────────────────────
        //
        // Owner, standing in one of these on a bare deck: "It really needs tables … the cubicles etc
        // chairs maybe tables etc. It is way too empty" / "in office people sit down".
        //
        // EVERY SOLID THING IN HERE IS ALREADY IN floor.Walls — Core laid the desks, the screens, the
        // kitchenette and the cubicles as segments, so they were drawn and collided with by the loop at
        // the top of this method, exactly as the park's raised beds and the en-suite's own pan are (the
        // fixture idiom the owner named as the one interior in the building that reads right). What is
        // left for a renderer is the STENCIL and the VERB, which is the whole of a renderer's business
        // in a room somebody else furnished.
        // #775 · …ASKED OF THE FLOOR AND NOT OF THE GARDEN. This walked green.Frontage, which was
        // the ring's only publisher while a ring only ever stood round a park — and on a landscape
        // floor (#775) that would have drawn a whole block of offices with no plate, no fill and no
        // seat to press, silently, because the `if` above simply never opened. FloorPlan.TheRing is
        // the building's own list and it is the same list on both kinds of floor.
        foreach (UndergroundComplex.RingRoom suite in floor.TheRing)
        {
            NameTheBookedRoom(labels, in suite, level, canteenWatch, booked);
            FurnishTheSuite(labels, furniture, in suite);

            // #775 · A SHUT SUITE IS DRAWN AND NEVER PRESSED. Its wall onto the core is glass, so its
            // desks are visible from the promenade and the fill above is the point of it — and its leaf
            // does not open, so hanging a seat console inside would put an [E] on a chair no body can
            // reach. Everything below this line is a verb; everything above it is a picture.
            if (suite.Shut)
            {
                continue;
            }

            SeatTheSuiteChairs(consoles, in suite);
            FitTheCubicles(consoles, in suite, level, cubiclesShut);
            PlumbTheBasinRun(consoles, in suite);
        }
    }

    /// <summary>
    /// #770 - NAMES THE ONE ROOM ON THE FRONTAGE THE CAPTAIN HAS PAID FOR. The ring's plates are not
    /// otherwise stencilled - a caption on every forty-du suite is a wall of text over a garden - but a
    /// BOOKED room is a state, and states go on the plan. Lays down: labels.
    /// </summary>
    private static void NameTheBookedRoom(
        List<(float X, float Y, string Text)> labels, in UndergroundComplex.RingRoom suite,
        int level, long canteenWatch, RoomBooking.Booking? booked)
    {
        // ── #770 · THE ONE ROOM ON THE FRONTAGE WITH A NAME AGAINST IT ────────────────────────
        //
        // The ring's plates are not otherwise stencilled on the plan — a block of forty-du suites
        // with a caption on every one of them is a wall of text over a garden, and the plate is
        // already told where it matters (the sit line names the room you sat down in). What IS drawn
        // is the room somebody has PAID FOR, because that is a state and states go on the plan: the
        // door reads BOOKED and which shift it is booked for, exactly as a cubicle's leaf reads
        // OCCUPIED. Core owns the words (RoomBooking.Booking.Plate) and this composes nothing.
        if (booked is { } mine
            && RoomBooking.HoldsThisRoom(in mine, level, suite.Number, canteenWatch))
        {
            labels.Add((
                (float)suite.Door.X1, (float)suite.Door.Y1, mine.Plate));
        }
    }

    /// <summary>
    /// #817/#868 - STENCILS AND FILLS ONE SUITE'S FURNITURE: the plate where Core put one, and the published
    /// BOX filled, because an outline reads as a space you could stand in. Lays down: labels, furniture.
    /// </summary>
    private static void FurnishTheSuite(
        List<(float X, float Y, string Text)> labels, List<DeckPlan.FurnitureSpot> furniture,
        in UndergroundComplex.RingRoom suite)
    {
        foreach (RingOffice.Fixture fitting in suite.Furniture)
        {
            if (fitting.Plate.Length > 0)
            {
                labels.Add(((float)fitting.X, (float)fitting.Y, fitting.Plate));
            }

            // #868 · …AND THE BOX ITSELF, FILLED. Owner, sitting in one of these on the back street:
            // "The graphics kind of does not show there being a table" / "The bench is a line" —
            // and, three paces away, "The Shelving is clear as furniture goes."
            //
            // The sentence above ("what is left for a renderer is the STENCIL and the VERB") was
            // true of a floor whose only fixtures were rectangles: four wall segments round a dark
            // gap DO read as a cupboard. It was never true of the back of house, whose bench was one
            // degenerate box and therefore one stroke, and it was never true of anything at all,
            // because an outline reads as a space you could stand in. So the deck is handed the
            // published BOX and fills it — Core's own rectangle, never one measured here (§13.15).
            Furnish(furniture, in fitting);
        }
    }

    /// <summary>
    /// HANGS THE SIT VERB ON A SUITE'S CHAIRS, on the seam the stools and the park benches already use: Core
    /// says where a seat is and this hangs a console on it. Lays down: consoles.
    /// </summary>
    private static void SeatTheSuiteChairs(List<DeckPlan.ConsoleSpot> consoles,
        in UndergroundComplex.RingRoom suite)
    {
        // The chairs take the SIT verb, on the seam the stools and the park benches already use:
        // Core says where a seat is and this hangs a console on it. The plate carries the VERB
        // (#783: "why not use words like SIT DOWN here if it means sitting down?").
        foreach (RingOffice.Chair chair in suite.Seats)
        {
            consoles.Add(new(
                DeckPlan.ConsoleKind.HiveOfficeChair,
                (float)chair.X, (float)chair.Y, chair.DeckPlate));
        }
    }

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
