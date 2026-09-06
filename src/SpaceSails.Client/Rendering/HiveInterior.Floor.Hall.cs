using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// Part of <see cref="HiveInterior"/> (the header note lives in HiveInterior.cs) — THE PASSES THAT DRESS
/// THE ONE ROOM ON THE FLOOR PEOPLE ARE IN: the bar's whole desk front as a single fixture, the pictures
/// on the floor and on the furniture, the tall seats and the gaps between them, the tops and whoever is
/// at them, the cabinets down the back wall and the cork board beside them.
///
/// <para>#1164 · Each was a <c>// ── banner ──</c> section inside <see cref="FloorDeck"/>'s amenity loop
/// and is now a named pass, called by <see cref="FitOutTheAmenities"/> in the order the banners stood in.
/// EVERY ONE OF THEM ASKS AND DECIDES NOTHING — where a stool is, who is on it, which top is talking and
/// what the mug behind her says are all Core's, off the frozen watch handed down from the page, because a
/// renderer that worked one of them out itself would be the drawn room and the pressed room coming out of
/// two authors.</para>
/// </summary>
public static partial class HiveInterior
{
    /// <summary>
    /// #707 · FITS OUT EVERY AMENITY Core carved out of the floor's own rooms — the counter, the art, the
    /// seats, the tops, the cabinets and the board — by calling the passes below in the order the banners
    /// stood in. The fixtures themselves are already poured; this is the console, the plate and the picture,
    /// which is the whole of a renderer's business in a room somebody else carved.
    /// </summary>
    private static void FitOutTheAmenities(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.Backdrop> backdrops, List<DeckPlan.TableTop> tables,
        List<DeckPlan.StoolSpot> stools, in UndergroundComplex.FloorPlan floor,
        string bodyId, int level, long canteenWatch,
        IReadOnlyCollection<string>? cabinetsDogged,
        IReadOnlySet<int>? stoodUp, IReadOnlyDictionary<int, string>? cameIn)
    {
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            ServeTheCounter(consoles, backdrops, in a);
            PaintTheFittedArt(backdrops, in a);
            SeatTheStools(labels, stools, in a, bodyId, level, canteenWatch);
            PlateTheHall(labels, in a);
            SeatTheTops(
                consoles, labels, tables, in a, bodyId, level, canteenWatch, stoodUp, cameIn);
            PlateTheCabinets(labels, in a, level, cabinetsDogged);
            HangTheCorkBoard(consoles, in a, bodyId, level);
        }
    }

    /// <summary>
    /// #791/#827 · SERVES THE WHOLE DESK FRONT off ONE console with a LENGTH on it — a row of [E] dots along
    /// a bar would be eleven pieces of furniture pretending to be a choice — and hangs the hall's own picture
    /// under it. Lays down: consoles, backdrops.
    /// </summary>
    private static void ServeTheCounter(
        List<DeckPlan.ConsoleSpot> consoles, List<DeckPlan.Backdrop> backdrops,
        in UndergroundComplex.Amenity a)
    {
        // ── #791 · ONE FIXTURE, ONE CARD, AND THE WHOLE DESK-FRONT TO PRESS IT FROM ────────────────
        //
        // Owner, live at the B1 bar: "The Bar desk is really long now, but there is only one spot to get
        // service on it… we would need an E-bus of the bar desk length instead of one bar keep cashier
        // at a single spot."
        //
        // It is still ONE console — one dot, one plate, one card — and that is the point of doing it
        // this way rather than bolting a row of consoles along the bar. A row would be a dozen [E]
        // targets in a room already dotted with table consoles, which is the very crowding #212 and
        // both "there two e's are too close to each others" reports were about; and every one of them
        // would open the same card, so eleven of the twelve would be furniture pretending to be a
        // choice. A fixture that IS eighty du long says the true thing once.
        //
        // THE LENGTH IS CORE'S. Hall.Service is the run the carve laid, off the same (u, v) as the
        // counter's own wall segments, its photograph and its stools. Nothing here measures a bar.
        //
        // #827 · …and the run is the DESK'S FRONT FACE, which is not concentric with the plate: the
        // console dot stands on the square a body stands on, and the rail is drawn on the counter a step
        // behind it. Both coordinates are Core's; the only thing that happens here is a cast.
        UndergroundComplex.ServiceRun? run = a.Hall?.Service;
        consoles.Add(new(
            DeckPlan.ConsoleKind.HiveAmenity, (float)a.X, (float)a.Y, a.Fixture,
            Run: run is { } bus
                ? ((float)bus.X0, (float)bus.Y0, (float)bus.X1, (float)bus.Y1)
                : null));
        if (a.Hall is { ArtUrl: { } floorArt } painted)
        {
            // Top-left, W, H — the ship's own convention, and Y is the box's TOP edge because deck +y
            // is up. Hall.X0/Y0/X1/Y1 are already min/max normalised where they are carved, so there is
            // no orientation to work out here: a rib that runs down the field and one that runs up it
            // hand this the same rectangle.
            backdrops.Add(new(
                floorArt,
                (float)painted.X0, (float)painted.Y1,
                (float)(painted.X1 - painted.X0), (float)(painted.Y1 - painted.Y0),
                UndergroundComplex.HallArtAlpha));
        }
    }

    /// <summary>
    /// #780 · PAINTS THE FURNITURE ART on top of the floor it stands on, AFTER the hall's own wallpaper in
    /// this list and never before it: DeckView walks Backdrops in order. Lays down: backdrops.
    /// </summary>
    private static void PaintTheFittedArt(List<DeckPlan.Backdrop> backdrops, in UndergroundComplex.Amenity a)
    {
        // ── #780 · AND THE FURNITURE, ON TOP OF THE FLOOR IT STANDS ON ────────────────────────────
        //
        // Owner, live: "see how in the space bars we have the image of bar desk at the spot where the
        // bar desk is." AFTER the hall's own art in this list and never before it, because DeckView
        // walks Backdrops in order — a counter painted first would have the room's wallpaper laid back
        // over it, which is one picture drawn twice and neither of them seen. Still under every vector
        // mark, which is the one law this whole seam exists to keep.
        //
        // The rectangle is Core's and the alpha is Core's; this loop measures nothing. It converts a box
        // (X0,Y0,X1,Y1) into the ship's own top-left+W+H convention, and that is the entire contribution
        // a renderer is allowed to make to a room somebody else carved.
        foreach (UndergroundComplex.SpotArt spot in a.Hall is { } furnished ? furnished.Painted : [])
        {
            backdrops.Add(new(
                spot.Url,
                (float)spot.X0, (float)spot.Y1,
                (float)(spot.X1 - spot.X0), (float)(spot.Y1 - spot.Y0),
                UndergroundComplex.SpotArtAlpha));
        }
    }

    /// <summary>
    /// #792 · SEATS THE TALL CHAIRS at the counter, free and taken, off Core's own row and Core's own
    /// occupancy — and plates the gaps in it. Lays down: labels, stools.
    /// </summary>
    private static void SeatTheStools(
        List<(float X, float Y, string Text)> labels, List<DeckPlan.StoolSpot> stools,
        in UndergroundComplex.Amenity a, string bodyId, int level, long canteenWatch)
    {
        // ── #792 · THE TALL SEATS, FREE AND TAKEN ──────────────────────────────────────────────────
        //
        // Owner: "there should be high chairs so sitting at the bar desk is also possible" (#756, built)
        // and then, on the 8th: "Now I have trouble finding a free table… lol story of my travelling
        // life right here."
        //
        // WHERE is the hall's (Core carved the row out of the counter's own segments); WHO IS ON THEM is
        // TheStools' (the same call the [E] press asks, off the same frozen watch, so the seat drawn
        // free is the seat pick-or-default will hand over). Both halves come from somewhere else on
        // purpose — a renderer that laid out eight stools would be doing geometry about a bar it did not
        // carve, and one that decided which were busy would be the drawn room and the pressed room
        // disagreeing, which is the bug class this project has paid for most often.
        if (a.Hall is { } counter)
        {
            // Asked over the whole row before any of it is drawn, because "is there anybody at this
            // counter" is a fact about the ROW and not about a seat: the free stool beside somebody and
            // the free stool in an empty bar are two different offers, and they are the same two offers
            // the tops make. One language, one question.
            bool anybody = false;
            for (int s = 0; s < counter.StoolRow.Count; s++)
            {
                anybody |= TheStools.Taken(bodyId, level, s, canteenWatch);
            }
            for (int s = 0; s < counter.StoolRow.Count; s++)
            {
                (double sx, double sy) = counter.StoolRow[s];
                stools.Add(new(
                    (float)sx, (float)sy, TheStools.Taken(bodyId, level, s, canteenWatch), anybody));
            }

            PlateTheGapsInTheRow(labels, in counter);
        }
    }

    /// <summary>
    /// #827 · PLATES THE HOLES IN THE ROW, WHICH ARE FIXTURES TOO. A gap with nothing painted on it is a seat
    /// somebody unbolted; a gap with its own stencil is a place to stand. Lays down: labels.
    /// </summary>
    private static void PlateTheGapsInTheRow(
        List<(float X, float Y, string Text)> labels, in UndergroundComplex.Hall counter)
    {
        // ── #827 · AND THE HOLES IN THE ROW, WHICH ARE FIXTURES TOO ──────────────────────────
        //
        // Owner, completing the counter model: "there are gaps for people to walk to the cashier
        // etc." A gap with nothing painted on it is a seat somebody unbolted; a gap with its own
        // stencil is a place to stand. Both the position and the words are Core's — this loop reads
        // the same one published row the stools above came out of, so a gap cannot end up somewhere
        // the seats do not agree with.
        foreach (UndergroundComplex.CounterPlace place in counter.CounterRow)
        {
            if (!place.Seated)
            {
                labels.Add(((float)place.X, (float)place.Y, place.Plate));
            }
        }
    }

    /// <summary>
    /// #751 · STENCILS THE HALL'S OWN PLATE beside its door, on whichever face the rib put it. Lays down:
    /// labels.
    /// </summary>
    private static void PlateTheHall(List<(float X, float Y, string Text)> labels,
        in UndergroundComplex.Amenity a)
    {
        // #751 · A hall's plate is stencilled beside its DOOR, which is on whichever face the rib is
        // on — so Core says where, exactly as it says where the board hangs. The 7.6 du offset below is
        // measured off a 15 x 12 room and would hang the sign in mid-floor in a room fifty du deep.
        labels.Add(a.Hall is { } signed
            ? ((float)signed.PlateX, (float)signed.PlateY, a.Plate)
            : ((float)a.X, (float)(a.Y - 7.6), a.Plate));
    }

    /// <summary>
    /// #709/#746 · SETS THE TOPS OUT and — where Core says people sit here — puts somebody at them, off the
    /// ONE call that answers the tops, their seat counts, their chairs and their occupancy together, so the
    /// room that is drawn and the room [E] presses are one room. Lays down: consoles, labels, tables.
    /// </summary>
    private static void SeatTheTops(
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.TableTop> tables, in UndergroundComplex.Amenity a, string bodyId, int level,
        long canteenWatch, IReadOnlySet<int>? stoodUp,
        IReadOnlyDictionary<int, string>? cameIn)
    {
        // ── #709/#746 · THE TOPS, AND — ON B1 ONLY — SOMEBODY SITTING AT THEM ─────────────────────
        //
        // Owner: "we should have people in the bar... we have cover story" and, in the same breath,
        // "for now let's keep the people in B1." Then, #746: "tables should seat 2/4/more, not all
        // pairs" — which is only worth having if somebody can ask whether a seat is FREE.
        //
        // ONE CALL FOR BOTH. This used to walk a.Tables for the round tops and then separately ask
        // CanteenRegulars.Sitting who was at them, which is two sources for one fact — the exact shape
        // #709's own docs warn about (the drawn room and the pressed room disagreeing). Core now
        // answers with the tops, their seat counts and their occupancy in one list, off the same
        // frozen watch, and the [E] press asks that same function. The renderer decides nothing.
        //
        // They stand ON the table's own spot rather than beside it, because the table IS the seat as
        // far as the deck is concerned: Core placed those round tops (#707) and a console offset by a
        // hand-typed du would be one more caller doing geometry about furniture it does not own.
        //
        // #751 · …and it is the same one call now that the room holds eighty. THREE TIERS COME OUT OF
        // IT — the named regulars, the background patrons that fill the hall by the watch, and the
        // cabinets' empty tops — and this loop cannot tell them apart, which is the point: a patron is
        // a plate at a coordinate, drawn like any console dot, with nothing to run per frame.
        //
        // #792 · THE SAME ONE CALL ANSWERS THE THREE GLANCES, TOO. A top now goes onto the plan with
        // its seat count, whether anybody is at it and whether they are TALKING — all three off this
        // list, off this frozen watch, so the chairs drawn free are the chairs [E] will offer. The
        // renderer downstream is handed the answers and never works one out: DeckView has no idea what
        // a canteen is, which is #788's own discipline for the seated captain applied to everybody else
        // in the room.
        foreach (CanteenRegulars.TableSeat top in
            CanteenRegulars.Tables(bodyId, level, a, canteenWatch, stoodUp, cameIn))
        {
            // #823 · …and HOW MANY of them are at it, which the pen used to have to guess at and
            // therefore drew as one. Handed down like every other field on this record, off the same
            // frozen watch, so the bodies on the plan are the bodies Core seated.
            // #820 · …and WHERE ITS CHAIRS ARE, off Core's own ring, with Core's own answer about which
            // of them the party is in. Handed down for the reason every other field on this record is:
            // the captain is seated in one of these chairs now, and a pen that placed them itself would
            // be the drawn chair and the sat chair coming out of two authors.
            var chairs = new List<DeckPlan.TableChair>(Math.Max(0, top.Seats));
            for (int c = 0; c < top.Seats; c++)
            {
                (double chx, double chy) = top.Chair(c);
                chairs.Add(new((float)chx, (float)chy, top.PartyIn(c)));
            }

            tables.Add(new(
                (float)top.X, (float)top.Y, top.Seats, top.Taken, top.Talking, top.Heads, chairs));
            // #757 · EVERY TOP IS NOW A CONSOLE, and which kind it is is the one fact the room already
            // knows: somebody at it, or nobody. Owner, live in the hall: "I have empty table but I
            // cannot sit down." An empty top used to be drawn as a ring on the floor and nothing else,
            // so [E] there had literally nothing to answer — the refusal the issue is titled after was
            // an ABSENCE, which is the one kind of refusal a player cannot read.
            //
            // Still one call, still the same frozen watch. The renderer does not decide who may be sat
            // with; it labels what Core says is there.
            //
            // …and TAKING one is offered in the room outsiders are admitted to and NOWHERE ELSE, which is
            // Core's own B1 ruling (CanteenRegulars.PeopleSitHere) rather than a clause typed here.
            // B17's staff mess is hall-class as well and just as full of tops, and its whole identity is
            // that the shift has not come — twenty FREE TABLE plates in it would be this renderer handing
            // that room a verb its own design refuses.
            if (top.Plate is { } plate)
            {
                consoles.Add(new(
                    DeckPlan.ConsoleKind.HiveRegular, (float)top.X, (float)top.Y, plate));

                    PutOneMugOnTheShelf(labels, plate, in top, in a);
            }
            else if (CanteenRegulars.PeopleSitHere(bodyId, level, a))
            {
                consoles.Add(new(
                    DeckPlan.ConsoleKind.HiveTable, (float)top.X, (float)top.Y,
                    SittingAlone.FreeTablePlate));
            }
        }
    }

    /// <summary>
    /// #1074 beat 4 · PUTS ONE MUG ON THE SHELF behind one regular's seat. A LABEL and not a console: there
    /// is nothing to work, nothing to press and nothing to take. Lays down: labels.
    /// </summary>
    private static void PutOneMugOnTheShelf(
        List<(float X, float Y, string Text)> labels, string plate, in CanteenRegulars.TableSeat top,
        in UndergroundComplex.Amenity a)
    {
        // ── #1074 beat 4 · AND ONE MUG ON THE SHELF BEHIND ONE OF THEM ──────────────────────
        //
        // "A mug on the shelf behind one regular's seat… The mug is the whole testimony." It is a
        // LABEL and not a console: there is nothing to work, nothing to press and nothing to
        // take. A captain sees a glass on a shelf behind a woman eating, and if he asks her about
        // it she says the one sentence she has and the room offers nothing further.
        //
        // No new art — the canteen's own glass (CareerCost.MugGlyph) — and no geometry decided
        // here: Core says where the shelf is off the top's own ring and clamps it into the room
        // (CareerCost.MugAt), because a pen placing a prop against a wall it does not own is
        // §13.15's own warning and this one has a chair to miss.
        if (string.Equals(plate, CareerCost.MugPlate, StringComparison.Ordinal))
        {
            (double mugX, double mugY) = CareerCost.MugAt(top, a);
            labels.Add(((float)mugX, (float)mugY, CareerCost.MugGlyph));
        }
    }

    /// <summary>
    /// #751/#758 · PLATES THE CABINETS down the back wall of the hall, each with what it is and which stage
    /// its privacy is standing at — read from the hall side, off the hall's own box. Lays down: labels.
    /// </summary>
    private static void PlateTheCabinets(
        List<(float X, float Y, string Text)> labels, in UndergroundComplex.Amenity a, int level,
        IReadOnlyCollection<string>? cabinetsDogged)
    {
        // ── #751 · THE CABINETS, PLATED ────────────────────────────────────────────────────────────
        //
        // Owner: "have cabinet-spaces for sensitive negotiations." A row of doors down the back wall of
        // a hall, each with what it is stencilled beside it — and the plate is the whole of how you get
        // one: BY ARRANGEMENT · ASK AT THE COUNTER. No console: there is nothing in a cabinet to work,
        // and the card fires by standing in it (the refuge idiom, same as the staff mess).
        if (a.Hall is { } theHall)
        {
            // Read from the hall side, in front of the door — never from inside the cabinet, and never
            // from the far side of the outer wall. Which side that is comes off the hall's own box
            // rather than a sign the renderer guessed at: the cabinets stand against whichever wall the
            // rib put them on, and this file does not know which one that was.
            double hallMidX = (theHall.X0 + theHall.X1) / 2.0;
            foreach (UndergroundComplex.Cabinet cabinet in theHall.Cabinets)
            {
                double inward = cabinet.X > hallMidX ? -1.0 : 1.0;

                // #758 · …AND WHICH STAGE IT IS STANDING AT, as one glyph on the end of that plate. The
                // whole of the deck's contribution to the curtain: a captain crossing the hall can see
                // which of three doors is cloth and which is dogged, and nothing is spelled out. Absent
                // from the set is CURTAIN — the state the building keeps them in — so a floor built with
                // nothing to say draws the row as it stands rather than as three shut doors.
                CabinetPrivacy.Stage stage =
                    cabinetsDogged is not null
                    && cabinetsDogged.Contains(CabinetPrivacy.Key(level, cabinet.Number))
                        ? CabinetPrivacy.Stage.Door
                        : CabinetPrivacy.Stage.Curtain;

                labels.Add((
                    (float)(cabinet.X + (inward * (cabinet.HalfW + 2.0))),
                    (float)cabinet.Y, CabinetPrivacy.PlateFor(cabinet.Plate, stage)));
            }
        }
    }

    /// <summary>
    /// #709 · HANGS THE CORK BOARD ON THE WALL, where the cast's own working day is written down. Core owns
    /// where, exactly as it owns who sits down. Lays down: consoles.
    /// </summary>
    private static void HangTheCorkBoard(
        List<DeckPlan.ConsoleSpot> consoles, in UndergroundComplex.Amenity a, string bodyId, int level)
    {
        // ── #709 · AND THE CORK BOARD ON THE WALL ─────────────────────────────────────────────────
        //
        // Owner: "let's add a bulletin board to the bar." It is where the cast's own working day is
        // written down — every notice on it belongs to somebody sitting in this room, and nothing
        // anywhere says so.
        //
        // CORE OWNS WHERE, exactly as it owns who sits down. A renderer choosing a spot on a wall would
        // be doing geometry about a room it does not own (§13.15), and the offsets are picked against the
        // counter's line and the tables' so the board owns its own patch of floor to be pressed from.
        if (CanteenBoard.At(bodyId, level, a) is { } board)
        {
            consoles.Add(new(
                DeckPlan.ConsoleKind.HiveBoard, (float)board.X, (float)board.Y, CanteenBoard.Plate));
        }
    }
}
