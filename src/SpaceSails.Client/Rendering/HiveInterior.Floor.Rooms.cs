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
    ///
    /// <para>#759 - ...and RETURNS THE LAMP RIG, null on every floor with no park in it. The masts stop
    /// being scenery here: the gravel's panels are flagged as grow-lit and the posts are handed up as a
    /// <see cref="DeckPlan.GrowLight"/>, so the frame can ask <see cref="ParkDay"/> what the room looks
    /// like at the sim-time it is DRAWING rather than at the sim-time the captain walked in. Nothing about
    /// the level is decided in this file - see that record's docs for why it cannot be.</para>
    /// </summary>
    private static DeckPlan.GrowLight? DrawThePark(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.Backdrop> backdrops, List<DeckPlan.BenchSpot> benchSeats,
        in UndergroundComplex.FloorPlan floor, string bodyId)
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
                        UndergroundComplex.HallArtAlpha,
                        // #759 · …and it is the one floor in the game that is lit by something other than
                        // the building. The flag, not the number: see DeckPlan.Backdrop.
                        GrowLight: true));
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

            // #759 · THE MASTS STOP BEING POSTS. Core published them as points and poured them as four
            // segments apiece the day the park was carved (UndergroundComplex.ParkMastXs); until now that
            // was the whole of them, and the room's "artificial day" was a word in a comment. The rig goes
            // up to the plan as WHERE and WHOSE — the site id, because the cycle's phase offset is seeded
            // off it, and nothing else, because nothing else about the light is a fact about this floor.
            var rig = new List<(float X, float Y)>(green.Masts.Count);
            foreach ((double mx, double my) in green.Masts)
            {
                rig.Add(((float)mx, (float)my));
            }

            return new DeckPlan.GrowLight(bodyId, rig);
        }

        return null;
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
}
