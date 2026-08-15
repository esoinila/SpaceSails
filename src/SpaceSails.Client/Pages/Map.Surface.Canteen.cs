using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the counter, the mess, the hall, attendance, the park and the board.
public partial class Map
{
    // ── #707 · THE COUNTER, THE BASINS AND THE MACHINES ─────────────────────────────────────────────────
    //
    // Owner: "all the secret labs dont have any cantina / bar nor any toilets."
    //
    // ONE verb for all three rooms, because it is one verb: stand in a room somebody ate or washed in and
    // read what is left of it. There is deliberately nothing to spend and nothing to pick up — this issue
    // builds the ROOMS, and the social layer that belongs in them (overhearing the next table, the exposure
    // a stranger's face costs in a room where every face is known, the meal-line ID check) is filed
    // separately by the owner's own scope split.
    //
    // WHICH ROOM IT IS COMES FROM CORE, matched by position exactly the way HiveHaulInteract matches a room
    // index. Reading it off the console's own label would have been closer to hand and would have been a
    // second copy of the fixture's name, going quietly wrong the day somebody edits one of the two.
    private void HiveAmenityInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveAmenity } spot)
        {
            return;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        for (int i = 0; i < floor.Amenities.Count; i++)
        {
            UndergroundComplex.Amenity a = floor.Amenities[i];
            if (Math.Abs(a.X - spot.X) >= 0.5 || Math.Abs(a.Y - spot.Y) >= 0.5)
            {
                continue;
            }

            // ── #756 · A COUNTER THAT TAKES ORDERS ANSWERS THE PRESS ITSELF ───────────────────────────
            //
            // Owner, live at this exact fixture: "HOW DO I ORDER A DRINK FROM THE BAR?????? I walk to the
            // bar to buy a drink... not possible... WHY?" What is below was the WHOLE of what pressing E
            // here ever did — a paragraph ABOUT a counter, read standing at the counter, with a purse in
            // your pocket and nothing in the building to spend it on.
            //
            // CORE SAYS WHETHER THIS FIXTURE SERVES, and the card that opens is the very one the Tilt bar
            // has opened since #247. The room's first-entry paragraph still lands — through FileNote rather
            // than ShowAndFile, because the pulse half would now play UNDER the card's own blur (#686/#736)
            // and a line said where nobody is looking is a line not said.
            if (Core.Interior.CounterService.For(ex.Stop.Body.Id, a.Use) is { } counter)
            {
                if (ex.HiveAmenitiesRead.Add(HiveInterior.RoomKey(ex.Floor, i)))
                {
                    FileNote(UndergroundComplex.AmenityLine(ex.Stop.Body.Id, a.Use), "🍽");
                    RequestVaultSave();   // the field note is a possession too (#587)
                }
                OpenCounterService(counter);
                return;
            }

            // FILED, not flashed. The mess's manifest is a lead about somewhere else entirely and the bar's
            // paint is the only warm thing in the building; both are worth being able to re-read, and a line
            // that faded in eight seconds would be #587's lesson unlearned. Once per room per excursion.
            if (ex.HiveAmenitiesRead.Add(HiveInterior.RoomKey(ex.Floor, i)))
            {
                ShowAndFile(UndergroundComplex.AmenityLine(ex.Stop.Body.Id, a.Use), "🍽");
                RequestVaultSave();   // the field note is a possession too (#587)
            }
            else
            {
                ShowPulseMessage(a.Plate);
            }
            return;
        }
    }

    // ── #725 · THE ROOM NOBODY EATS IN, NOTICED ON THE WAY IN ────────────────────────────────────────────
    //
    // Owner's audit: "are we giving enough attention to plot-significant finds? They should have a Gen-AI
    // image and their own dialog by our standards." The staff mess was map furniture — a plate, four machines
    // and three tables — and a captain who walked through it without pressing E got nothing at all.
    //
    // A ROOM BEAT AND NOT A FLOOR BEAT, which is the one thing that makes it different from every other card
    // down here. The floor it sits on is an ordinary floor; the find is a door and what is behind it. So this
    // is the refuge idiom (poll the position, ask Core whether the room holds you) rather than the lift
    // idiom, and the containment law is UndergroundComplex's own — Amenity.Contains delegates to RefugeHolds,
    // so the box the card fires in is the box the walls are drawn on.
    //
    // WHICH ROOM COMES FROM CORE: the floor plan's own StaffCanteen amenity, which CarveAmenities only ever
    // makes on StaffCanteenFloor. Nothing here re-asks which floor that is — a second answer to a question
    // Core already owns is the table at the top of UndergroundComplex.cs.
    //
    // Once per excursion, like DEAD AIR: the first time is the find, and after that it is a canteen.
    //
    // AND IT IS ASKED ONCE PER FLOOR, not once per frame. UndergroundComplex.Build lays a whole floor out;
    // running it inside the step loop would be a generator call every tick for as long as the captain is
    // underground, which is the cost the renderer already pays exactly once. Cached against (body, floor)
    // because Build is pure and deterministic per that pair — the same reason a floor is the same floor
    // every visit.
    private (string Body, int Floor)? _messRoomFor;
    private UndergroundComplex.Amenity? _messRoom;

    private void CheckStaffMessUnderfoot()
    {
        // #746 · TWO beats live in this room now, and they are not the same beat. The room's own card is the
        // FIND (once, #743); showing the chit to it is what somebody with a reason to be here does, and a
        // captain who walked through this room on an earlier floor-crawl and only later talked their way onto
        // the cage crew must still get it. So the poll runs until BOTH have had their turn.
        if (_surface is not { } ex || ex.Floor >= 0 || _viewObject is not null
            || (ex.HiveStaffMessShown && ex.MessChitBeatShown))
        {
            return;
        }

        if (_messRoomFor != (ex.Stop.Body.Id, ex.Floor))
        {
            _messRoomFor = (ex.Stop.Body.Id, ex.Floor);
            _messRoom = null;
            foreach (UndergroundComplex.Amenity a in
                UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
            {
                if (a.Use == UndergroundComplex.Comfort.StaffCanteen)
                {
                    _messRoom = a;
                    break;
                }
            }
        }

        if (_messRoom is { } mess && mess.Contains(_avatarX, _avatarY))
        {
            if (!ex.HiveStaffMessShown)
            {
                ex.HiveStaffMessShown = true;
                _viewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                    UndergroundComplex.StaffMessLabel,
                    UndergroundComplex.StaffMessArtUrl,
                    UndergroundComplex.StaffMessCard);
                RendererInterop.PlayCue("reveal");
                return;   // one beat at a time — see below
            }

            // #746 · …and if you are carrying the cage crew's chit, you eat. ADDITIVE, and deliberately
            // second: #743's card is the find, and this is what somebody with a reason to be here does next.
            //
            // NEVER IN THE SAME FRAME AS THE CARD. This line is PULSED, and a pulse played while a card is up
            // renders under that card's backdrop and its blur — #680, exactly. The poll survives the card
            // (see the guard at the top), so the beat lands on the tick after the captain closes it, still
            // standing in the room, with nothing over the top of it.
            ShowMessChitBeat(ex);
        }
    }

    // ── #751 · THE HALL, AND THE DOORS ALONG THE BACK OF IT ──────────────────────────────────────────────
    //
    // Owner: these rooms are story-grade — they get first-entry CARDS with gen-AI art, the same one-shot
    // pattern as THE PLATE and THE STAFF MESS. So this is #725's idiom, verbatim: poll the position, ask
    // CORE whether a room holds you, raise the card once and never again this excursion.
    //
    // TWO ROOMS ON ONE POLL, and the order is the order a captain meets them: you cannot be in a cabinet
    // without having been in the hall, so the hall's card is raised first and the cabinet's on a later tick.
    // One card at a time — #680: a second card raised in the same frame renders under the first one's
    // backdrop and its blur.
    //
    // AND THE CABINET FILES ITS NOTE OFF THE SAME LATCH. The card is the moment and the note is the book's
    // compressed record of it; two latches would be two ways for one event to be remembered, and a save in
    // between would eventually show one without the other.
    private (string Body, int Floor)? _hallFor;
    private UndergroundComplex.Amenity? _hallRoom;

    private void CheckCantinaHallUnderfoot()
    {
        if (_surface is not { } ex || ex.Floor >= 0 || _viewObject is not null)
        {
            return;
        }

        // Both beats spent, and the poll stands down for the rest of the excursion. The hall's own card is
        // asked of CORE rather than of the flag alone, because there is one building where it never fires
        // at all (the head office's dining room, #411) — and a poll that waited for a card that is never
        // coming would run for the whole walk.
        bool hallDue = !ex.HiveCantinaHallShown
            && UndergroundComplex.ShowsCantinaHallCard(ex.Stop.Body.Id, ex.Floor);
        if (!hallDue && ex.HiveCabinetShown)
        {
            return;
        }

        // Cached against (body, floor) for the reason #725's own comment gives: Build lays a whole floor
        // out, and running it inside the step loop would be a generator call every tick for as long as the
        // captain is underground.
        if (_hallFor != (ex.Stop.Body.Id, ex.Floor))
        {
            _hallFor = (ex.Stop.Body.Id, ex.Floor);
            _hallRoom = null;
            foreach (UndergroundComplex.Amenity a in
                UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
            {
                if (a.Use == UndergroundComplex.Comfort.UpperCanteen && a.Hall is not null)
                {
                    _hallRoom = a;
                    break;
                }
            }
        }

        if (_hallRoom is not { } hallRoom || hallRoom.Hall is not { } hall
            || !hallRoom.Contains(_avatarX, _avatarY))
        {
            return;
        }

        // WHICH BUILDING'S HALL EARNS THE CARD IS CORE'S CALL (see hallDue above). The card names the sign
        // on the door — CANTEEN 1 · CARRIERS & CONTRACTORS — and the head office's dining room is a
        // different room with a different register and an arrival card of its own (#411).
        if (hallDue)
        {
            ex.HiveCantinaHallShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.CantinaHallLabel,
                UndergroundComplex.CantinaHallArtUrl,
                UndergroundComplex.CantinaHallCard);
            RendererInterop.PlayCue("reveal");
            // No save: nothing durable changed. The latch is excursion-scoped like every one of its
            // siblings (#725) — a captain who lands again is walking in for the first time again.
            return;   // one beat at a time
        }

        if (!ex.HiveCabinetShown && hall.CabinetAt(_avatarX, _avatarY) is not null)
        {
            ex.HiveCabinetShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.CabinetLabel,
                UndergroundComplex.CabinetArtUrl,
                UndergroundComplex.CabinetCard);
            // FileNote and NOT ShowAndFile: the card is up, and a pulse under an open card's backdrop is
            // the exact bug #680 was filed on. The book still remembers.
            FileNote(UndergroundComplex.CabinetNote, UndergroundComplex.CabinetGlyph);
            RendererInterop.PlayCue("reveal");
            // …and THIS one saves, because the field book is durable and the note is now in it.
            RequestVaultSave();
        }
    }

    // ── #759 · ATTENDANCE IS RECORDED ────────────────────────────────────────────────────────────────────
    //
    // The plate at the gate says the park keeps a register. So the game keeps exactly one: a line in the
    // captain's own field book, filed the first time they walk in and never again this excursion. That is
    // the whole feature — the issue's title phrase delivered as a SENTENCE rather than as a system, because
    // a park that actually counted your visits would be a park that had noticed you, and #759's whole
    // register is that nothing down here ever admits to noticing anything.
    //
    // NO CARD. The hall gets one and the cabinet gets one because they are finds; the park is a place you
    // walk, and its own art is already under your boots. A third card in the same room would spend the
    // beat.
    private (string Body, int Floor)? _parkFor;
    private UndergroundComplex.Park? _park;

    private void CheckTheParkUnderfoot()
    {
        // The note is durable but the LATCH is the excursion's, like every one of its siblings (#725) — and
        // the poll stands down for the rest of the walk the moment it has fired. `_viewObject` is checked
        // for #680's reason: a pulse played under an open card renders beneath its backdrop and its blur.
        if (_surface is not { } ex || ex.Floor >= 0 || ex.HiveParkNoteFiled || _viewObject is not null)
        {
            return;
        }

        // Cached against (body, floor), the hall poll's own reason: Build lays a whole floor out, and
        // running it inside the step loop would be a generator call every tick for the whole excursion.
        if (_parkFor != (ex.Stop.Body.Id, ex.Floor))
        {
            _parkFor = (ex.Stop.Body.Id, ex.Floor);
            _park = UndergroundComplex.Build(
                ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park;
        }

        if (_park is not { } green || !green.Contains(_avatarX, _avatarY))
        {
            return;
        }

        ex.HiveParkNoteFiled = true;
        ShowAndFile(UndergroundComplex.ParkNote, UndergroundComplex.ParkGlyph);
        RequestVaultSave();
    }

    // ── #709 · STOPPING AT SOMEBODY'S TABLE ──────────────────────────────────────────────────────────────
    //
    // Owner: "we should have people in the bar... we have cover story."
    //
    // The Hive's first conversation, and deliberately the smallest one the game has. A regular gives you ONE
    // breath of their working day and nothing else: no menu, no trade, no round to buy, no contact seam. That
    // restraint is the feature — the upper canteen is where a captain is UNREMARKABLE, and a room that starts
    // offering things is a room that is paying attention to you.
    //
    // NO RISK HERE, ON PURPOSE. #618 rules that talking is what blows a cover — but that is the GUARDS on the
    // bottom floors, who are not built yet, and whose whole point is that probing them probes back. Band 0 is
    // the floor whose own sign says NO PASS REQUIRED. Wiring an exposure cost into this conversation now would
    // pre-empt a ruling that has not been made and would make the one safe room in the building unsafe.
    //
    // Filed once per person per excursion, pulsed every time after — the same law the amenity write-up above
    // uses, and for the same reason: leaning on a table eleven times is not eleven findings, and a console
    // that goes silent reads as broken (#212).
    private void HiveRegularInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveRegular } spot)
        {
            return;
        }

        // WHICH PERSON COMES FROM CORE, matched by position — the same discipline HiveAmenityInteract uses
        // one method up. Reading the line off the console's own label would have been closer to hand and
        // would have been a second copy of the cast, going quietly wrong the day somebody edits one of them.
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        int seat = 0;
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            foreach (CanteenRegulars.Seated who in
                CanteenRegulars.Sitting(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
            {
                if (Math.Abs(who.X - spot.X) < 0.5 && Math.Abs(who.Y - spot.Y) < 0.5)
                {
                    if (ex.HiveRegularsHeard.Add(HiveInterior.RoomKey(ex.Floor, 900 + seat)))
                    {
                        ShowAndFile(who.Line, CanteenRegulars.Glyph);
                        RequestVaultSave();
                    }
                    else
                    {
                        ShowPulseMessage(who.Plate);
                    }
                    return;
                }
                seat++;
            }
        }
    }

    // ── #709 · READING THE BOARD ─────────────────────────────────────────────────────────────────────────
    //
    // Owner: "let's add a bulletin board to the bar" · "maybe spot the person notifying in the bar."
    //
    // ONE NOTICE PER PRESS, in the order they are pinned, then round again. Deliberate: a board that dumped
    // four notices into one card would be read once and never looked at twice, and the whole point of this
    // object is that a captain comes BACK to it after meeting the people. The pump notice means nothing until
    // you have heard the fitter, and everything afterwards.
    //
    // NOTHING HERE READS Notice.Pairs. Which regular pinned which notice is the player's to make or to miss,
    // and a client that hinted would spend the only thing the board has.
    private void HiveBoardInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveBoard })
        {
            return;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            var pinned = CanteenBoard.Pinned(ex.Stop.Body.Id, ex.Floor, a);
            if (pinned.Count == 0)
            {
                continue;
            }

            CanteenBoard.Notice n = pinned[ex.HiveBoardNext % pinned.Count];
            ex.HiveBoardNext++;
            ShowAndFile($"{n.Head} — {n.Body}", CanteenBoard.Glyph);
            RequestVaultSave();   // the field note is a possession too (#587)
            return;
        }
    }
}
