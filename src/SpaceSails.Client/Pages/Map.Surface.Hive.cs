using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the shaft down and back up, the detector, the dossier, and the foot of the monolith.
public partial class Map
{
    // ── #585 · THE HIVE: down the shaft, and back up ───────────────────────────────
    //
    // Owner: "I just don't want the secret lab to be puny 2 door apartment, but look like it could facilitate
    // a large operation with serious funding."
    //
    // Three calls were made on his behalf when he said "go forward" (all pinned by TheHiveTests, so each is a
    // one-line overrule): you find it by LOOKING (the lift head's door is the one imported thing on the moon),
    // it goes three floors and the bottom is not a bottom, and B1 still holds pressure while everything below
    // it does not. That last one is the whole feel of the place - the top floor lulls you and the rest costs
    // you air.
    private void HiveLiftInteract()
    {
        if (_surface is null)
        {
            return;
        }

        // #801 · WHICH CAR THE CAPTAIN IS STANDING AT. The press used to throw the pressed console away and
        // ask Core about "the lift", which was harmless while there was one and is a bug the moment there
        // are two: the goods car at the blind end would have opened the cage's panel and set the captain
        // down a hundred and seventy du away. The spot decides; nothing else in this method knows.
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { } at
            || at.Kind is not (DeckPlan.ConsoleKind.HiveLift or DeckPlan.ConsoleKind.HiveHead
                or DeckPlan.ConsoleKind.HiveServiceLift))
        {
            return;
        }
        _liftCar = at.Kind == DeckPlan.ConsoleKind.HiveServiceLift
            ? UndergroundComplex.ShaftKind.Service
            : UndergroundComplex.ShaftKind.Cage;

        // ── #600 · THE PANEL, BECAUSE THE CAR ONLY WENT DOWN ──
        //
        // Owner, on B1: "looks like the elevator only takes me down... how do I get back to the surface with
        // it :-D Am I marooned in a secret lab underground now :-D ?" — then, deciding it: "we should have
        // elevator panel with UI then".
        //
        // This function USED to be the ride: one keypress, always one floor deeper, and the only way out was
        // to reach the bottom of the band first. On a twenty-floor site that meant riding eighteen floors
        // further from the surface, through dead air, to go up. The file's own comment two screens down says
        // a captain trapped on a dead floor is a death; the lift was the thing doing the trapping.
        //
        // It survived #590, #591 and #592 all editing this function, because none of them asked what the UP
        // case did — and the A* audit cannot see a state machine. It proves the captain can REACH the lift,
        // never that the lift is a way HOME.
        //
        // Core owns which buttons exist (UndergroundComplex.LiftPanel) so that the #590 card gate and the
        // #592 silence are ONE pure, tested rule instead of something re-derived in a razor file.
        _liftOutcome = null;
        _showLiftPanel = true;
        RendererInterop.PlayCue("board");
    }

    /// <summary>#600 · The buttons on this car's panel, from where the captain is standing.
    ///
    /// <para>#752 · The whole satchel goes in, and not a second list of ids: the cage's gate reads the
    /// day-labour chit, and whether the captain has cover is a fact about what they are CARRYING
    /// (<c>CanteenTable.Cover</c>) — asked of the same pocket the player can open and look in.</para></summary>
    private IReadOnlyList<UndergroundComplex.LiftStop> LiftStops() =>
        _surface is { } ex
            ? UndergroundComplex.LiftPanel(ex.Stop.Body.Id, ex.Floor, _liftCar, AuthorityCardIds(), _satchel)
            : [];

    /// <summary>#801 · Which of the two cars the open panel belongs to — set by the press that opened it,
    /// and read by the panel, the ride and the placement so that all three are talking about one machine.
    /// The cage until somebody walks to the other end of the corridor.</summary>
    private UndergroundComplex.ShaftKind _liftCar = UndergroundComplex.ShaftKind.Cage;

    /// <summary>#801 · What the open panel says under its title. The cage's line is the one it has always
    /// had; the goods car's names where the cage is, which is the anti-choke feature said in a sentence.</summary>
    private string LiftPanelLine() =>
        _liftCar == UndergroundComplex.ShaftKind.Cage
            ? "This car serves its own band and no further."
            : UndergroundComplex.ServiceCarPanelLine;

    /// <summary>#600 · A button was pressed. A refusing button says why and the car does not move — a button
    /// that is present and explains itself is the entire reason it is not simply absent.</summary>
    private void PressLiftButton(UndergroundComplex.LiftStop stop)
    {
        if (_surface is not { } ex || stop.IsCurrent)
        {
            return;
        }
        if (stop.Refusal is { } refused)
        {
            // Said every time — a refusal that goes quiet on the second press reads as a broken button.
            // Filed once, because pressing one gate eleven times is not eleven findings.
            //
            // #686 · And said INSIDE the panel. Owner, at this exact gate: "result text is not displayed
            // so it can be read. It is the same kind of bug as we had with the inventory item use." The
            // panel stays open on a refusal, and a line pulsed from here plays under the backdrop's blur —
            // in the DOM and not on the screen (#680's disease, second organ). The book still gets the
            // record; the saying happens where the player is looking.
            //
            // #684 · And it is the MATRIX that answers now. The panel used to keep a second set of sentences
            // of its own (UndergroundComplex.WrongCardLine, deleted), so the sharpest refusals in the game —
            // another shaft of THIS site, named, versus somebody else's building, named (#679/#683) — had no
            // client caller and nobody ever read them. One source, and this is a caller of it.
            UndergroundComplex.GateRead read =
                UndergroundComplex.TheGateReads(ex.Stop.Body.Id, ex.Floor, _satchel);
            int refusedBand = UndergroundComplex.BandOf(stop.Level);
            if (ex.HiveShaftsRefused.Add(refusedBand))
            {
                _liftOutcome = read.Line;
                FileNote(read.Line, "🔒");

                // #684 · …and it is TOLD. Owner: the read's outcome must be raised as a story card in the
                // house idiom, carrying the matrix's own line. It goes up OVER the panel — the view-object
                // block is the last modal in Map.razor and shares the same backdrop band, so the card is the
                // pop-up that is up, and #736's law is met by the line being ON it rather than only in the
                // panel row underneath. Once per shaft per excursion, off the same latch as the field note:
                // one event, one memory, and two latches would eventually disagree (#751's rule).
                _viewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                    read.Label, read.ArtUrl, read.Line);
            }
            else
            {
                _liftOutcome = $"🔒 {refused}";
            }
            return;
        }

        _showLiftPanel = false;

        // #689 · Going below this car's own band is the OTHER shaft, and #590's card is what opened it — but
        // the saying of that belongs to the ARRIVAL and not to this line. It used to be said right here, on
        // the exact frame the panel closes and the floor is rebuilt under the captain's feet, and the owner
        // played the whole loop without ever seeing it. The blur disease's third organ: not under a modal
        // this time, under a scene change. The ride carries the stop with it, and the doors say it.
        RideTheLiftTo(ex, stop.Level, stop);
    }

    private void CloseLiftPanel()
    {
        _showLiftPanel = false;
        _liftOutcome = null;
    }

    /// <summary>#600 · Whether the lift panel is up. The sim keeps running behind it, exactly as it does
    /// behind the valve board.</summary>
    private bool _showLiftPanel;

    /// <summary>#686 · What the last refused button answered, said inside the panel itself. The pulse HUD
    /// renders under the modal backdrop's blur, so a refusal routed there is in the DOM and not on the
    /// screen. Cleared whenever the panel opens or closes — the line belongs to one stand at one gate.</summary>
    private string? _liftOutcome;

    /// <param name="via">#689 · The button that was pressed, when a button was pressed. A ride that goes
    /// through the card gate has a story to tell on arrival, and only the panel knows which trip that was —
    /// the dev floor cheat rides the same car and has no gate to cross.</param>
    private void RideTheLiftTo(
        SurfaceExcursion ex, int level, UndergroundComplex.LiftStop? via = null)
    {
        int fromLevel = ex.Floor;
        bool wasUnderground = ex.Floor < 0;
        ex.Floor = level;

        if (level == 0)
        {
            // #602 · Back out INSIDE THE SHED — the box the car came up into. Owner: "I would expect to spawn
            // into the elevator box where we went down with", which is also what the line below has always
            // said out loud ("lets you out into somebody's idea of a maintenance shed").
            //
            // Taken from the shed itself rather than from a magic offset off its centre, so the spot the
            // captain lands on cannot drift from the walls that are drawn around it. That drift is exactly
            // what put him in a wall: this used to compute its own position from the RAW seeded head spot
            // while the shed was built at the nudged one.
            // #681 · …through the one door the captain is ever placed through, so the car has the same net
            // under it that the landing does. The audit still pins the un-nudged square (#602's own guard).
            (double carX, double carY) = MoonSurface.LiftHead(
                ex.Stop.Body.Id, ex.Site.LayoutSalt, MoonSurface.ExpeditionField()).CarFloor;
            // #603 \u00b7 And what you came out WITH. Owner: "Also in the brief pop-up of going to the surface...
            // inventory key needs to be advertised." Surfacing is the moment a captain takes stock \u2014 they
            // have just stopped spending air and started counting what it bought \u2014 so it is the one place
            // the pocket should announce itself without being asked.
            string carried = _satchel.Count > 0
                ? $" You are carrying {_satchel.Count} thing{(_satchel.Count == 1 ? "" : "s")} out of it \u2014 \ud83c\udf92 I to look."
                : "";
            ShowPulseMessage("\ud83d\udec3 The car climbs for a long time and lets you out into somebody's idea of a " +
                "maintenance shed. The moon is exactly as indifferent as you left it." + carried);
            // #681: and the placement LAST, so if the net has to catch anybody its line is the one left on
            // screen. A rescue nobody sees is a bug nobody reports.
            StandCaptainAt(carX, carY, "the car lets you out into the shed on the surface");
            // #804 · Nobody walks a round on the regolith. Cleared on the way out so a captain who surfaces
            // mid-challenge does not leave two people standing in a corridor that is no longer drawn.
            SpawnPatrolFor(ex);
            RequestVaultSave();
            return;
        }

        // #801 · …and it opens where the car the captain PRESSED is, which used to be a constant. The
        // doorstep is the shaft's own (UndergroundComplex.Shaft.Landing) and the two alcoves hang off
        // opposite faces of the spine, so a placement that kept its own +1.0 would have set a captain who
        // rode the goods car down inside the goods car's own wall.
        (double sx, double sy) = HiveInterior.SpawnOn(MoonSurface.ExpeditionField(), _liftCar);
        StandCaptainAt(sx, sy, "the car opens onto the floor");   // #681: the same net, underground
        RendererInterop.PlayCue("board");

        // #804 · WHO IS WALKING THIS ONE. Built here — at the one place a floor changes — and never in the
        // deck rebuild, which runs every time a room is searched and would restart a round under a captain
        // who was halfway through timing it. The watch is the one the arrival just froze, so the round and
        // the canteen upstairs turn over on the same beat.
        SpawnPatrolFor(ex);

        if (!wasUnderground)
        {
            // #585 \u00b7 THE CARD, on the first descent only. Owner: "I think we need to gen AI pop-up about
            // finding the elevator." It is the beat the whole feature turns on \u2014 the moment a moon stops
            // being a field with things scattered on it and becomes a LID.
            if (ex.HiveFloorsSeen.Count == 0)
            {
                // #411 · …and which card, because the head office's first descent is not a branch office's.
                // Core decides, so the two can never be shown for the wrong building.
                (string dLabel, string dArt, string dCard) =
                    UndergroundComplex.FirstDescentCard(ex.Stop.Body.Id);
                _viewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY, dLabel, dArt, dCard);
            }
        }

        // #411 · THE TWO FLOORS THE WHOLE ARC WAS WRITTEN FOR. Raised after the first-descent block on
        // purpose: those floors are reached from underground, so `wasUnderground` is true by the time a
        // captain gets to either of them and the establishing card above has long since been spent.
        MaybeRaiseHeadOfficeBeat(ex);

        // ── #725 · …AND THE SIGN THAT SAYS IT, WHICH HAD NO FRAME ──────────────────────────────────────
        //
        // Owner's audit: "are we giving enough attention to plot-significant finds? They should have a
        // Gen-AI image and their own dialog by our standards." The corrected plate is the whole arc's
        // arithmetic in one object and it was a wall stencil — missable at deck-plan zoom by a player who
        // has just walked past the reveal, with the game none the wiser.
        //
        // WHICH FLOOR IS CORE'S ANSWER (IsUnlistedLobby, off #694's own plate law) rather than a band sum
        // done again in a client. The card is per-excursion once, exactly like DEAD AIR above it, and it
        // takes the _viewObject slot uncontested: the top of every band holds pressure, so the air card can
        // never want the same frame, and the establishing card was spent floors ago.
        if (UndergroundComplex.IsUnlistedLobby(ex.Stop.Body.Id, level) && !ex.HiveUnlistedPlateShown)
        {
            ex.HiveUnlistedPlateShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.UnlistedLobbyLabel,
                UndergroundComplex.UnlistedLobbyArtUrl,
                UndergroundComplex.UnlistedLobbyCard);
            RendererInterop.PlayCue("reveal");
        }

        // ── #693 · EVERYTHING THE DOORS OPENING HAS TO SAY, AND THE LAW THAT DECIDES WHAT YOU HEAR ──
        //
        // This was five blocks in a row, three of them carrying a comment explaining that they were
        // deliberately LAST — because the pulse has one slot, the last write won, and the order these lines
        // happened to be written in was the entire contract. #592's climax was not one of the three. The
        // first words on a floor that does not exist, the biggest sentence in that feature, had been losing
        // the slot to the routine pressurisation line since the day it shipped: eaten by the weather.
        //
        // The arrival is COMPOSED now (Core, ArrivalSayings) with a RANK on each saying, and PulseSlot's law
        // picks the winner — a lower rank may not displace a higher one that is still held. So this loop
        // says all of them, the book keeps every one in the order they were said, and the screen keeps the
        // biggest. Shuffle the list and the same line is on screen; that is the law, and it is swept over
        // every arrival the generator admits (ThePulseKeepsTheBiggestSentenceTests).
        //
        // What stays here is what Core does not have: the cards, the nerve, the flags and the save.
        bool firstSight = ex.HiveFloorsSeen.Add(level);

        // #804 · Whether THIS ride was the one the day-labour chit opened. Banked rather than acted on where
        // it is noticed, so the pass it earns is granted after the arrival has said everything it has to say.
        bool chitGateThisRide = false;

        foreach (UndergroundComplex.Saying saying in UndergroundComplex.ArrivalSayings(
                     ex.Stop.Body.Id, fromLevel, level,
                     new UndergroundComplex.ArrivalMemory(
                         WasUnderground: wasUnderground,
                         FirstSightOfThisFloor: firstSight,
                         VacuumWarned: ex.HiveVacuumWarned,
                         UnlistedSeen: ex.HiveUnlistedSeen,
                         ChitBeatSpent: ex.ChitGateBeatShown,
                         SeamCrossed: ex.HiveSeamCrossed,
                         FoundSeen: ex.HiveFoundSeen,
                         ShaftsNarrated: ex.HiveShaftsOpened),
                     via, AuthorityCardIds(), _satchel))
        {
            // #768 · HELD, NOT SAID — because this same loop raises cards (the dead-air warning below, the
            // gate's own face) and the block above it may already have raised the first-descent card. Every
            // one of them lands a backdrop on top of whatever is on the pulse. The book gets the line now,
            // in the order it was said; the screen gets the winner when the captain closes the card.
            HoldAndFile(saying.Text, saying.Glyph, saying.Rank);

            switch (saying.Beat)
            {
                // #592 · THE FLOOR THAT IS NOT ON THE PLAN. The whole beat of the feature, said once, on the
                // first step out onto the band nobody listed.
                case UndergroundComplex.ArrivalBeat.Unlisted:
                    ex.HiveUnlistedSeen = true;
                    ApplyNerveShock(9.0, "a building with floors it does not count");
                    break;

                // ── #609 · WHETHER YOU CAN BREATHE HERE IS A CARD, NOT A TOAST ──
                //
                // Owner, having suffocated on B2: "I thought there is air in the base?" / "there should be a
                // warning or something :-D" / "maybe pop-up about you have air or you are in vacuum type ...
                // it is vital info" / "like the basement is more dangerous than the surface now :-D".
                //
                // The last one is exactly right and it is the DESIGN — depth is paid for in air (#585) — but
                // the game was announcing the single most important fact about a floor in a pulse that fades
                // in eight seconds, alongside pulses about hardware and dust. So the FIRST time each
                // excursion meets dead air it stops the world with a card; every later dead floor is the
                // pulse line again, because by then the captain has been told and a card per floor would be
                // a card nobody reads.
                case UndergroundComplex.ArrivalBeat.DeadAirFirst:
                    ex.HiveVacuumWarned = true;
                    _viewObject = new DeckPlan.ConsoleSpot(
                        DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                        UndergroundComplex.VacuumCardLabel,
                        UndergroundComplex.VacuumArtUrl,
                        UndergroundComplex.VacuumCard(ex.Stop.Body.Id, level, ex.AirSeconds));
                    break;

                // #689 · THE CARD'S FINEST HOUR. Owner, having found the card, fed the gate and ridden past
                // the listed bottom: "It was locked until I got it ... there was no story point about it
                // being needed or used." The line existed; it was said on the frame the panel closed and the
                // floor was torn down and rebuilt. Here the doors are open, the captain is standing still,
                // and the car is not going anywhere. Once per shaft per excursion — and the band comes off
                // the saying, because the ride that opened it already worked out which one that was.
                //
                // #684 · …and it is TOLD as a CARD, which is the other end of the read the panel makes. A
                // gate that refuses raises one at the panel; a gate that AGREES raises one here — same
                // title, same idiom, the face of the card the gate actually read. It belongs in this arm
                // and nowhere else: the beat already carries WHICH card opened the gate, so nothing here
                // re-derives a fact about a building it does not own (§13.15), and the picture can never
                // drift from the sentence #693 just said beside it. The pulse is that law's business and is
                // untouched — this is the saying a later tick cannot overwrite.
                case UndergroundComplex.ArrivalBeat.CardAccepted when saying.Gate is { } opened:
                    ex.HiveShaftsOpened.Add(opened.Band);
                    UndergroundComplex.GateRead told = UndergroundComplex.TheGateAccepted(opened);
                    _viewObject = new DeckPlan.ConsoleSpot(
                        DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                        told.Label, told.ArtUrl, told.Line);
                    ApplyNerveShock(3.0, "a gate that still obeys an office nobody can find");
                    break;

                // #752 · …AND THE OTHER PAPER'S ARRIVAL, WHICH IS THE JOB FINISHING. The Hand's line was
                // "take this to the lift and don't be clever near the counter"; this is the lift having heard
                // of it. No nerve shock: the card's gate is a dead office still saluting, which is
                // frightening, and a gate that reads a timesheet and waves you through is the least
                // frightening thing this building has done. The GIST is filed with the beat — the beat is
                // what happened, the gist is what the paper turned out to be worth.
                case UndergroundComplex.ArrivalBeat.ChitGate:
                    ex.ChitGateBeatShown = true;
                    FileNote(CanteenTable.ChitGateGist, CanteenTable.ChitGlyph);
                    // #804 · AND THE JOB PAYS IN PAPER — but the grant is made BELOW, after the arrival has
                    // finished speaking. Doing it here would put the pass's own sentence into the middle of
                    // a composed arrival, where a later saying (or the held card's release) simply takes the
                    // slot back off it (#693/#768). The gig completing is the loudest thing about this
                    // ride, and it is said last.
                    chitGateThisRide = true;
                    RequestVaultSave();
                    break;

                // #677 · The pour stopping, said in the shaft on the way.
                case UndergroundComplex.ArrivalBeat.Seam:
                    ex.HiveSeamCrossed = true;
                    break;

                // #677 · The first gallery. The same price as the floor nobody listed, and not bigger,
                // deliberately: nothing down here threatens, and a site that bills a captain for standing in
                // a comfortable room is a predator whatever the prose says (§10.4c's ruling).
                case UndergroundComplex.ArrivalBeat.Found:
                    ex.HiveFoundSeen = true;
                    ApplyNerveShock(9.0, "a room that was ready before anybody thought to build one");
                    break;

                default:
                    break;   // the descent's own line and every later air line are prose and nothing else
            }
        }

        ApplyNerveShock(UndergroundComplex.HoldsPressure(ex.Stop.Body.Id, level) ? 2.0 : 5.0,
            "a building this expensive, this far down, and this empty");

        // #768 · …AND NOW THE ARRIVAL KNOWS WHETHER IT PUT A CARD IN FRONT OF ITSELF. Every card this
        // arrival can raise has been raised by here, so this is the only place that can honestly ask. Doors
        // that opened on nothing but prose pulse the winner immediately — the shipped behaviour, unchanged.
        // Doors that also raised a card keep it, and the ✕ on that card is what finally says it.
        ReleaseHeldSayingsUnlessACardStopsTheWorld();

        // #804 · AND THE JOB PAYS IN PAPER, said LAST. This is the gig completing, not the gig being
        // offered: the Hand hands you a chit, the chit is a promise, and going down on it is the shift you
        // actually turned up for. The site does the one thing a site does about a body that has arrived on
        // somebody's account — it puts you on its books.
        //
        // Hung on the CHIT'S ride rather than on the table because the table's own gist already says what
        // the paper is worth ("Downstairs is a place you are now paid to be"), and this makes that sentence
        // literally true. After the release, because the pass's line is the loudest thing about this
        // particular ride and the arrival's own composition would otherwise take the slot back off it.
        if (chitGateThisRide)
        {
            IssueTheSitePass(ex);
        }

        RequestVaultSave();
    }

    /// <summary>#585 - Where the camouflaged lift head stands. Reuses the seeded secret-lab door spot, so the
    /// ground already kept clear for the old chamber is exactly the ground the shed stands on - one seeded
    /// fact, two uses, nothing to keep in sync.</summary>
    private (double X, double Y) SecretLabHeadSpot(SurfaceExcursion ex)
    {
        // #602 · THE SAME FUNCTION THE SHED IS DRAWN BY, and for once the comment above this one was not
        // describing the code. Owner, stepping out of the car: "Oh I emerged into the wall on the surface...
        // I cannot move :-D"
        //
        // This used to return SecretLab.For(...).DoorX/DoorY — the RAW seeded spot. But MoonSurface builds
        // the shed at SecretLab.HeadSpot(...), which starts from that raw spot and then MOVES it clear of
        // the shelters and huts already standing there. So on any site where the nudge did something, the
        // lift returned the captain to the un-nudged spot: inside the very structure the nudge exists to
        // avoid, in a wall, unable to move.
        //
        // The old comment claimed "one seeded fact, two uses, nothing to keep in sync" — which was the
        // intention and not the code. It is one function now, so it is true.
        return SecretLab.HeadSpot(
            ex.Stop.Body.Id, ex.Site.LayoutSalt, MoonSurface.ExpeditionField());
    }

    /// <summary>#585 - Turning over one room of the facility. About a third are stripped; the rarest thing in
    /// the building is a FILE ON SOMEBODY, because it is the only haul you spend on a person.</summary>
    private void HiveHaulInteract()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveHaul } spot)
        {
            return;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        int which = -1;
        for (int i = 0; i < floor.RoomCentres.Count; i++)
        {
            if (Math.Abs(floor.RoomCentres[i].X - spot.X) < 0.5
                && Math.Abs(floor.RoomCentres[i].Y - spot.Y) < 0.5)
            {
                which = i;
                break;
            }
        }
        if (which < 0)
        {
            return;
        }

        // #678 · THE ROOM IS NOT CONSUMED UNTIL THE FIND IS. This used to be one line — test the key and mark
        // it emptied in the same breath — which is exactly why a find the pocket could not take was destroyed
        // by the act of looking at it. Now the room is only struck off once the pickup has actually happened.
        int roomKey = HiveInterior.RoomKey(ex.Floor, which);
        if (ex.HiveRoomsEmptied.Contains(roomKey))
        {
            return;
        }

        UndergroundComplex.Haul haul = UndergroundComplex.InRoom(ex.Stop.Body.Id, ex.Floor, which);

        // ── #701 · THE ODD BOOK ─────────────────────────────────────────────────────────────────────────
        //
        // Owner: "a better alternative to finding an empty room. You look around but only one book catches
        // your attention." Searching a room and finding nothing is the most common outcome in this building
        // and the least written; one would-be-empty room in six now says something instead.
        //
        // It happens BEFORE the pocket, and it returns without ever reaching it, because a book is not a
        // haul: nothing goes in the satchel, no credits change hands, and — the load-bearing part — THE ROOM
        // IS NOT STRUCK OFF. The book is still on the shelf, so the console is still standing there and [E]
        // opens the card again. Re-reading is free; the casebook learns the gist once per book per thread
        // (#603's law: looking is free, knowledge is one-shot), which Core decides, not this method.
        //
        // The shelf line is the ROOM's line; the GIST is what the book is worth and is the thing the casebook
        // keeps. Filing both would put the same shelf in the book twice in two registers, and the second
        // search would file it a third time.
        if (OddBooks.Search(ex.Stop.Body.Id, ex.Floor, which, _oddBooksRead, _bookCheat) is { } shelf)
        {
            // #528's idiom, caption-only: there is no art file for these yet and one that is wired but
            // unpainted would render an img the browser hides — the lifeboat-muster precedent is a card that
            // never claims a picture at all.
            //
            // #736 · THE SHELF LINE IS ON THE CARD, not under it. The comment three lines down has said since
            // #701 that "the pulse would play under the card's own blur" — and the shelf line was pulsed on
            // the frame this card goes up, so the sentence that tells you WHY one book caught your eye was
            // the one sentence of the beat behind the frosted glass. Composed into the card's own story, the
            // way #603's document card already carries its try's answer: one object card, one text.
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                shelf.Title, null, shelf.Line + "\n\n" + shelf.Card);

            if (shelf.Gist is { } gist)
            {
                // FileNote and not ShowAndFile: the saying is on the card above, and a pulse would play under
                // the card's own blur (#686/#736).
                FileNote(gist, OddBooks.Glyph);
                _oddBooksRead = [.. shelf.Filed];
            }

            RequestVaultSave();
            return;
        }

        // Everything found down here is FILED (#587) - this is the place in the game most worth being able to
        // re-read, and a file on a harbourmaster that faded after eight seconds would be a joke.
        // #613 \u00b7 AND SAY WHETHER IT WENT INTO YOUR POCKET. Owner, after clearing a corridor: "now I used e
        // to search and then checked inventory on many rooms" / "we should tell the users if stuff is picked
        // up into inventory or not."
        //
        // The haul line describes what is in the room and stops, which was right while everything either
        // paid out in credits or was flavour. Some of it is now a THING YOU CARRY and some of it is not, and
        // the captain was opening the satchel after every single room to find out which \u2014 the game asking
        // the player to audit it.
        // \u2500\u2500 #613 \u00b7 THE CARD YOU FOUND IS ALWAYS A CARD YOU CARRY \u2500\u2500
        //
        // Owner, in a four-floor site: "now I picked authority card but it did not go to inventory."
        //
        // He was right and the cause was mine. CardInRoom returns the card for the band BELOW, and correctly
        // returns null on the bottom band \u2014 a card for a hole nobody dug would be a lie. But the client then
        // handed out a lead and put NOTHING in the pocket, while the prose went on describing a countersigned
        // card in the captain's hand. The sim did one thing and the sentence said another: this repo's third
        // named bug class, and I shipped it again.
        //
        // A card is an OBJECT. You picked it up, so you have it. When the shaft it runs is not in this
        // building it is a card for another one \u2014 which is exactly the wallet the refusal line has been
        // describing all along ("every one of them countersigned, current, and for another shaft"). Until now
        // that line was describing a thing the game could not give you. Now the deepest floor of one facility
        // hands you the way into the next, which is the best thing a bottom floor could possibly hold.
        //
        // ── #684 · AND THE LEAD IS NOT SPENT UNTIL THE CARD IS IN THE POCKET ──
        //
        // This used to call GrantLabLead here, which BANKS the lead and says it out loud — one step ahead of
        // the capacity check below. On a bottom-band Key with a full pocket the find is refused, the room is
        // not emptied and the same card is offered again on the next search (#678's law) — but the lead had
        // already been heard, and news can only be heard once. The captain kept the knowledge without ever
        // carrying the card that was supposed to be how they got it.
        //
        // The moon is only NAMED here now. It is announced further down, after the pocket has agreed.
        UndergroundComplex.AuthorityCard? found = null;
        string? farLead = null;
        if (haul == UndergroundComplex.Haul.Key)
        {
            found = UndergroundComplex.CardInRoom(ex.Stop.Body.Id, ex.Floor);
            if (found is null
                && NameAMoonWorthLookingAt(
                    DiceRule.Seed($"lead:hive-key:{ex.Stop.Body.Id}:{ex.Floor}:{which}")) is { } far)
            {
                farLead = far;
                if (UndergroundComplex.SiteHasBand(far, 0))
                {
                    found = new UndergroundComplex.AuthorityCard(far, 0);
                }
            }
        }

        // ── #678 · THE POCKET NEVER LIES ──
        //
        // Owner, after a card he had read a pickup line for turned out not to be in the satchel: "we should
        // have CI test that makes sure all picked items that sound useful are put into the inventory ... If
        // refused the item should stay where it was investigated last — not disappear like they do now, or
        // seem to."
        //
        // The composition used to live here, in the wrong order: the "Into your pocket" line was built and
        // shown, the room was already struck off, and only THEN did Satchel.Add get a chance to refuse. At
        // capacity the find was destroyed and the sentence had already claimed it. It is one pure call now
        // (UndergroundComplex.WhatGoesInThePocket), which is the only way a test can walk every haul against
        // every pocket — and it answers all three parts at once: what goes in, what is said, and whether the
        // room has been emptied at all.
        // #677 · Core mints the id, because the id says which KIND of place the thing came out of and three
        // separate seams read that later — the pocket line, the satchel row and the look-card. Composed here
        // as a string literal it was one place; the moment a second class of relic existed it would have
        // been the client teaching itself a fact about a band it does not own.
        string findId = UndergroundComplex.FindId(ex.Stop.Body.Id, ex.Floor, which);
        UndergroundComplex.Pickup pick = UndergroundComplex.WhatGoesInThePocket(
            haul, ex.Stop.Body.Id, found, findId, _satchel);

        string room = UndergroundComplex.HaulLine(haul, ex.Stop.Body.Id, ex.Floor, which, found);

        if (!pick.RoomEmptied)
        {
            // Nothing changes but the sentence. The room is not struck off, so searching it again offers the
            // same find — which is the enforcement side of #615 (leaving a thing must never destroy it).
            // The deck is deliberately NOT rebuilt: the console has to still be standing there.
            ShowAndFile(room + pick.Line, "\ud83d\udd26");
            RequestVaultSave();   // the field note is a possession too (#587)
            return;
        }

        ex.HiveRoomsEmptied.Add(roomKey);

        // #684 · NOW the lead is spent — the room has been turned over for good and whatever it held is in
        // the pocket. Said BEFORE the room's own line for the reason everything in this method is ordered:
        // the pulse keeps one slot and the last write wins, and the haul is the sentence worth surviving.
        if (farLead is { } lead)
        {
            AnnounceLabLead(lead);
        }

        if (haul == UndergroundComplex.Haul.Equipment)
        {
            _credits += 900;
            RendererInterop.PlayCue("board");
        }

        // #677/#603 \u00b7 WHAT THE BOOK KEEPS. Everywhere in the game the pulse line and the casebook line are
        // the same sentence, because what happened IS what is worth remembering. A record out of the halls
        // is the exception #603's law was written for: looking is free, knowledge is one-shot, so the screen
        // gets the event (a rubbing went into a pocket) and the book gets what the captain now KNOWS about a
        // wall. Filing both would put one wall in the book twice in two registers \u2014 #701's rule, learned on
        // the shelves \u2014 and there are several of these records in a band.
        if (UndergroundComplex.CasebookGistOf(haul, ex.Stop.Body.Id, ex.Floor) is { } hallGist)
        {
            ShowPulseMessage(room + pick.Line);
            if (!ex.HiveHallRecordShown)
            {
                FileNote(hallGist, "\u2b55");
            }
        }
        else
        {
            ShowAndFile(room + pick.Line, haul == UndergroundComplex.Haul.Dirt ? "\ud83d\uddc3" : "\ud83d\udd26");
        }

        if (haul == UndergroundComplex.Haul.Dirt)
        {
            ApplyNerveShock(4.0, "reading somebody's file in a building that should not exist");
        }

        // #585 · A facility keeps records of the OTHER facilities. Operational paper and files are the
        // strongest leads in the game, which is the right shape: the deeper into one of these you go, the
        // more of the map opens up.
        // ── #603 · PAPER IS NOW A THING YOU CARRY, NOT A LEAD YOU ARE GIVEN ──
        //
        // Operational paper used to grant a lead the moment you picked it up — the game did the thinking and
        // handed you the answer. The owner's version is better: the decision to read a document AS A CLUE is
        // the player's, and making it is what lights the tracker. A paper you have not connected to anything
        // is just paper.
        //
        // A file on somebody is carried too, but it is never offered to a door: it is leverage on a PERSON,
        // which is the only currency down here you spend on somebody you can go and meet.
        //
        // #678 · ONE ADD, and it is the one the sentence was written about. There used to be four of these
        // scattered down the method, each deciding for itself what this room hands over — which is how the
        // authority card came to be added AFTER the full-pocket check and to slip through it entirely.
        if (pick.Take is { } took)
        {
            _satchel = [.. Core.Satchel.Add(_satchel, took)];
        }

        if (haul == UndergroundComplex.Haul.Relic)
        {
            // The card is raised on the spot. For the thing on the pallet that is unconditional and every
            // time: it is the one object in the game a captain will want to look at again the moment they
            // find it, and #528's once-per-excursion gate is for things that RECUR — a rib mouth, a card, a
            // dead floor. There is one of those in a facility and most facilities do not have one.
            //
            // #677 · A HALL RECORD DOES RECUR, so it takes the gate. Several galleries in a band hold one
            // and they are all the same wall; the fifth full-screen card about it would be a slideshow, and
            // it is the same call the authority card makes twenty lines below. WHICH card is Core's — the
            // find's own id knows what it came out of, so this seam can never show a photograph of a pallet
            // to a captain standing in an empty gallery.
            CarriedObject.Reveal shown = CarriedObject.RelicReveal(findId);
            bool recurs = UndergroundComplex.IsHallRecord(findId);
            if (!recurs || !ex.HiveHallRecordShown)
            {
                _viewObject = new DeckPlan.ConsoleSpot(
                    DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                    shown.Label, shown.ArtUrl, shown.Story);

                // The fright is on the FIRST one for the same reason the card is. It is also the same size
                // as the pallet's and not larger: nothing down here threatens, everything accommodates, and
                // a place that bills a captain by the room for standing in it is a predator whatever the
                // prose says. What is frightening about a hall is arithmetic, not attention.
                ApplyNerveShock(9.0, recurs
                    ? "putting a tape measure against something that does not answer to one"
                    : "standing next to something that was measured for a neck");
            }

            ex.HiveHallRecordShown |= recurs;
        }
        else if (haul == UndergroundComplex.Haul.Dirt)
        {
            // A file still names a moon on its own — it is about a PERSON and the person is somewhere. Only
            // the operational paper became a thing you have to decide about.
            GrantLabLead(DiceRule.Seed($"lead:hive:{ex.Stop.Body.Id}:{ex.Floor}:{which}"));
        }

        // #678 · Said only when something actually went in — "that was the last space" is a fact about a
        // pickup, and on a room that handed over nothing it would be a warning attached to nothing.
        //
        // #688 · And it is a fact about ONE COMPARTMENT, because after the restructure "the satchel is full"
        // has no meaning: a sleeve stuffed with manifests says nothing about the pockets, and neither of them
        // says anything about the wallet, which never fills at all. The warning names what ran out.
        if (pick.Take is { } went && Core.Satchel.IsFull(_satchel, went.Kind))
        {
            ShowPulseMessage(Core.Satchel.CompartmentOf(went.Kind) == Core.Satchel.Compartment.Sleeve
                ? "🎒 That was the last sheet the document sleeve will hold. Something has to be read or " +
                  "left behind before you can carry any more paper out of here."
                : "🎒 Your hands and pockets are full. Something has to be spent or left behind before you " +
                  "can carry anything else out of here.");
        }

        // #590 · THE CARD IS NOW A THING YOU HOLD. It runs the shaft below the band it was found in, so the
        // way deeper into a facility is earned by working the floors you are on — and it is durable, so the
        // gate still reads it a month and a moon later.
        //
        // On the bottom band there is no shaft below to authorise, and rather than hand out an authority for
        // a hole nobody dug, that Key names another moon: the same payoff Records and Dirt give, which keeps
        // the deepest floor of a site pointing outward instead of at itself.
        //
        // #528 · THE COUNTERSIGNATURE. Owner: "the authority card could also have a gen ai image to really
        // tell the story here :-D" — the right pair with the sealed way, because the Hive has exactly two
        // objects about the idea of passage and this is the one that works. It is raised for a card that is
        // IN THE POCKET and never for one the world declined to mint (#678).
        if (found is { } card && pick.Take is not null && !ex.HiveAuthorityShown)
        {
            ex.HiveAuthorityShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.AuthorityCardLabel,
                UndergroundComplex.AuthorityCardArtUrl(card),
                UndergroundComplex.AuthorityCardStory(card));
        }

        RebuildSurfaceDeck();
        RequestVaultSave();
    }

    /// <summary>#585 - A door that never opens, read out loud. It is a WALL with a world behind it, and the
    /// game never once pretends otherwise - a door that teases would turn scale into a puzzle.</summary>
    private void HiveSignInteract()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveSign } spot)
        {
            return;
        }
        // #803 \u00b7 A door somebody took the hasp off is not a locked door any more, and the sign it wears says
        // which of the two it is. Reading it is still worth a press \u2014 the plate is the only thing in the
        // corridor that names the room \u2014 but it offers no pockets and tells no locked-door story, because
        // there is nothing left on it to try.
        if (spot.Label.StartsWith(HiveInterior.ShotOpenGlyph, StringComparison.Ordinal))
        {
            ShowPulseMessage(ShootTheLock.BehindItLine(
                spot.Label.Replace($"{HiveInterior.ShotOpenGlyph} ", "", StringComparison.Ordinal)));
            return;
        }

        string sign = spot.Label.Replace("\ud83d\udd12 ", "");

        // #528 \u00b7 THE WAY ON, CLOSED. Owner, standing at a rib's far end: "I see there is a nice lock here at
        // the end of the corridor.... maybe we could have a gen-AI image for it and a pop-up to tell the
        // story?"
        //
        // It fires at the moment it explains the most (#528's fourth rule): the captain has just walked the
        // length of a rib and met a plate with a distance painted on it. ONLY the sealed way earns a card \u2014 a
        // room door that will not open is a sign to read, not a scene, and forty of them would be a
        // slideshow. The pulse line still says its piece underneath, here and every time after.
        if (UndergroundComplex.IsSealedWay(sign) && _surface is { } sealedEx && !sealedEx.HiveSealedWayShown)
        {
            sealedEx.HiveSealedWayShown = true;
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                UndergroundComplex.SealedWayCardLabel,
                UndergroundComplex.SealedWayArtUrl,
                UndergroundComplex.SealedWayCard(sign));
            ApplyNerveShock(3.0, "a corridor somebody dug for a year and then closed");
        }

        // ── #603 · AND THE DOOR TELLS YOU YOU HAVE POCKETS ──
        //
        // Owner: "we should advertise the items list on closed locked door pop-up... something like check
        // your items button there that opens inventory."
        //
        // This is the #212 law applied to the satchel: the refusal already said WHY, and a captain standing
        // in front of it with an authority in their pocket had no way to find out whether it was the one.
        // The door now offers the verb. It is also how the satchel gets discovered at all — nobody reads a
        // keybind, and everybody presses the button on the thing that just refused them.
        _lockedDoor = new LockedDoorLook(
            sign,
            UndergroundComplex.LockedLine(sign),
            UndergroundComplex.IsSealedWay(sign)
                ? SatchelTry.Target.SealedWay
                : SatchelTry.Target.RoomDoor);
    }

    /// <summary>
    /// #803 · THE HASP COMES OFF, IN THE BUILDING'S OWN GRAMMAR.
    ///
    /// <para>The floor plan is pure and deterministic per (body, level), so the door that was shot is found
    /// where it has always been rather than remembered as a shape: the plan is rebuilt, the lock nearest the
    /// console the captain fired at is identified, and its key is written down. Every later rebuild replays
    /// it — a floor change, a searched room, a save and a load — which is the same seam an emptied room
    /// rides and the reason a shot door cannot grow its wall back behind the captain's shoulder.</para>
    ///
    /// <para>The MATCH is by geometry and by nothing else. A branch office reuses its door vocabulary, so a
    /// floor can carry two doors reading LONG STORAGE and shooting one of them is not shooting the other —
    /// keying on the sign would open both, quietly, forever.</para>
    /// </summary>
    private void TheLockComesOff(SurfaceExcursion ex, DesignateTarget target)
    {
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(
            ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());

        string? key = null;
        double bestSq = double.MaxValue;
        foreach (UndergroundComplex.LockedDoor l in floor.Locked)
        {
            double mx = (l.X1 + l.X2) / 2, my = (l.Y1 + l.Y2) / 2;
            double dx = mx - target.X, dy = my - target.Y;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < bestSq)
            {
                bestSq = d2;
                key = HiveInterior.LockKey(ex.Floor, l);
            }
        }

        // A console spot IS a lock's midpoint, so the nearest one is the one that was shot; the tolerance
        // exists only so a future renderer nudging a plate by a hair cannot silently open a different door.
        if (key is null || bestSq > 1.0)
        {
            return;
        }

        ex.LocksShotOpen.Add(key);
        RebuildSurfaceDeck();
    }

    /// <summary>#603 · The door the captain is standing at, while its pop-up is up.</summary>
    private sealed record LockedDoorLook(string Sign, string Line, SatchelTry.Target Target);

    private LockedDoorLook? _lockedDoor;

    private void CloseLockedDoor() => _lockedDoor = null;

    // ── #588 · A PERSON, OUT OF THE PIECES ─────────────────────────────────────────────────────────────
    //
    // Owner: "when we find somebody's kit maybe we get gen ai compilation of what we discover about them...
    // nice place for world building and dropping bread crumbs about our big plot", and then the part that
    // makes it a MECHANIC rather than a lore drop — "if we know what happened to someone we get contacts
    // easily by contacting their loved ones, in some cases that might lead our gum-shoe-efforts forward."
    //
    // Three pieces of kit make a person (one is litter, two is a coincidence). The payoff is not loot: it is
    // an errand, and a name to drop, and — sometimes — somebody who has been waiting nine years for news and
    // knows something nobody would ever tell a pirate.
    //
    // ── #774 · AND THE SENTENCES ARE READ ON THE CARD, NOT UNDER IT ────────────────────────────────────
    //
    // This method used to raise the card and then fire two to four ShowAndFile lines beneath it: the person,
    // the next of kin, what the family knows, the in that fell out of the kit — every one of them pulsed
    // into the HUD while a full-screen backdrop stood in front of the HUD. All of them were filed, none of
    // them was readable, and the captain closed the card onto silence.
    //
    // #768's hold cannot settle it and was refused for exactly this case: it releases ONE winner, and these
    // are a same-rank SEQUENCE whose survivor would have been decided by which line was appended last —
    // the ordering-as-contract bug #693 killed. So the remedy is #736's law instead. The card carries them,
    // in Core's own reading order (FieldDossier.Beat), and the book keeps every one of them exactly as it
    // always did: what changed is where a sentence is READ, never what is recorded.
    private void AssembleSomebody(SurfaceExcursion ex, string body, string salt, int roomIndex)
    {
        ex.KitPieces.Add(roomIndex);

        // ?kit=1 — the picture comes together on the FIRST piece. Three papers rooms at one room in eight,
        // inside a single excursion, is the rarest thing on the regolith; the cheat moves the gate and
        // nothing else, so what a tester reads is a dossier a captain can genuinely be handed.
        int enough = _kitCheat ? 1 : FieldDossier.FragmentsToAssemble;
        if (ex.KitPieces.Count < enough || ex.DossierShown)
        {
            return;
        }
        ex.DossierShown = true;

        // The stranger is keyed on the room whose papers COMPLETED the picture, so which body you are
        // holding is a fact about where you searched — not a global roll that would have happened anyway.
        FieldDossier.Person who = FieldDossier.Who(body, salt, roomIndex);
        string place = Core.FieldNotes.PlaceLabel(ex.Stop.Body.Name, ex.Site.Name);

        // Everything this kit has to say, already in the order it is read — composed in Core beside the
        // rolls that decide whether each sentence exists at all, so the client never picks an order.
        IReadOnlyList<FieldDossier.Saying> debrief =
            FieldDossier.Debrief(body, salt, roomIndex, everySaying: _kitCheat);

        // The card, with the compiled effects AND the debrief on it. Reuses the ViewObject pop-up the
        // builder's plate and the souvenirs already use — one image surface, not a second one to keep in
        // step. The caption is the fiction; the outcome region is what you are now holding.
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            FieldDossier.ConsoleLabel, FieldDossier.ArtUrl, FieldDossier.Compiled(who, place),
            FieldDossier.DebriefBlock(debrief));

        RendererInterop.PlayCue("board");

        // The book, unchanged: the same sentences under the same glyphs in the same order they were said.
        foreach (FieldDossier.Saying said in debrief)
        {
            FileNote(said.Text, said.Glyph);

            // #585: and what the family knows is a PLACE. This is the owner's own chain closing — "if we
            // know what happened to someone we get contacts easily by contacting their loved ones, in some
            // cases that might lead our gum-shoe-efforts forward" — arriving, eventually, at a moon on the
            // tracker. Banked at the same point in the sequence it has always stood, between the hint and
            // the in, because the field book's order is a record of when things were said.
            if (said.Beat is FieldDossier.Beat.WhatTheFamilyKnows)
            {
                GrantLabLead(DiceRule.Seed($"lead:kin:{body}:{salt}:{roomIndex}"));
            }
        }

        ApplyNerveShock(4.0, "a stranger's whole life, laid out on a rock");
    }

    // ── #585 · THE DETECTOR, SWEEPING ─────────────────────────────────────────────────────────────────
    //
    // Owner: "the detector should also give detecting readings near it."
    //
    // The probe was all-or-nothing — the exact square pings, its eight neighbours shriek, and the other four
    // thousand squares say nothing — so finding a lab was a lottery rather than a search. This is the
    // hot-and-cold every treasure hunt has run on forever: a reading that climbs as you close and falls away
    // as you drift, so a captain can pick a bearing, walk it, and TURN when it cools.
    //
    // It only wakes on a moon somebody has named (#585's leads). A detector that hummed everywhere would hand
    // over every lab in the system for free and make the whole clue chain pointless.
    private SecretLab.Reading _lastDetectorReading = SecretLab.Reading.Silent;

    private void StepSecretLabDetector()
    {
        if (_surface is not { } ex
            || ex.Lab is not { HasLab: true } lab
            || ex.SecretLabDoorRevealed
            || !_labLeads.Contains(ex.Stop.Body.Id))
        {
            _lastDetectorReading = SecretLab.Reading.Silent;
            return;
        }

        double dx = lab.DoorX - _avatarX, dy = lab.DoorY - _avatarY;
        SecretLab.Reading now = SecretLab.ReadingAt(Math.Sqrt((dx * dx) + (dy * dy)));
        if (now == _lastDetectorReading)
        {
            return;   // speak on the CHANGE only; a needle that narrates every frame is noise
        }

        bool warmer = now > _lastDetectorReading;
        _lastDetectorReading = now;

        string line = SecretLab.ReadingLine(now, warmer);
        if (line.Length > 0)
        {
            ShowPulseMessage(line);
            if (warmer && now >= SecretLab.Reading.Strong)
            {
                RendererInterop.PlayCue("pulse");
            }
        }
    }

    /// <summary>#585 · A clue names a moon. Called from every find in the gumshoe chain — a file in a
    /// facility, papers in a ruin, what a dead specialist's family turns out to know.</summary>
    /// <summary>Names a moon worth searching, and returns WHICH — #613, so a Key found on a bottom floor can
    /// mint the card for the shaft it points at. Returns the body even when the lead was already known: the
    /// lead is news you can only hear once, the card is an object that exists regardless.</summary>
    private string? GrantLabLead(ulong seed)
    {
        if (NameAMoonWorthLookingAt(seed) is not { } named)
        {
            return null;
        }
        AnnounceLabLead(named);
        return named;
    }

    /// <summary>#684 · WHICH moon, and NOTHING else — no lead written down, no line said, no save asked for.
    ///
    /// <para>Split out because one caller has to know the answer BEFORE it is allowed to keep it. A Key found
    /// on a bottom band mints its card for the site a lead names (#613), and that mint can be refused by a
    /// full pocket (#678) — at which point the room is not emptied and searching it again offers the same
    /// find. The lead was being banked and SAID during the naming, one step ahead of the capacity check, so a
    /// captain with no room left walked away holding the knowledge and none of the card. The sentence was
    /// composed before the act it describes, which is the exact fault #678 was filed about, surviving in the
    /// one branch of it that reached outside the pocket.</para></summary>
    private string? NameAMoonWorthLookingAt(ulong seed)
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        var candidates = new List<string>();
        foreach (ShuttleStop stop in ShuttleDestinationsInRange())
        {
            if (stop.IsLandable && !Derelict.TryParseWreckId(stop.Body.Id, out _))
            {
                candidates.Add(stop.Body.Id);
            }
        }
        if (!candidates.Contains(ex.Stop.Body.Id))
        {
            candidates.Add(ex.Stop.Body.Id);
        }

        return SecretLab.MoonWorthLookingAt(candidates, seed);
    }

    /// <summary>#684 · Bank the lead and say it — once. News you can only hear the first time, which is why
    /// it must not be spent on a find the pocket then refuses.</summary>
    private void AnnounceLabLead(string named)
    {
        if (!_labLeads.Add(named))
        {
            return;
        }

        string display = ShuttleDestinationsInRange()
            .FirstOrDefault(s => s.Body.Id == named)?.Body.Name ?? named;

        // #774 · Where the captain is looking, because one of this method's callers is the dossier assembly
        // and the dossier's own card is up when it calls — a moon named into a pulse behind that backdrop is
        // the fifth sentence of the same bug. Every other caller reaches this with nothing in front of them,
        // where the seam is an ordinary pulse and indistinguishable from one.
        SayWhereTheyAreLookingAndFile(SecretLab.LeadLine(display), "🔎");
        RequestVaultSave();
    }

    // ── #586 · WHAT SOMEBODY LEFT AT THE FOOT OF THE MONOLITH [E] ──────────────────────────────────────
    //
    // Owner: "let's have gen AI image at the monolith and some items appearing there now and then ... it is
    // supposed to be impressive... now it looks like a box in closet."
    //
    // The picture is the [E] on the slab itself. THIS is the other half, and it is the half that makes the
    // place alive: a landmark that never changes is scenery you visit once. Every line here is somebody
    // ELSE's visit — a cutting rig laid down neatly, a scoured plate, bootprints that all face the slab and
    // none lead away. The monolith itself never speaks, never reacts, and is never confirmed to have noticed
    // anything, which is the whole register (see reever-origin canon: the game never explains this).
    private void MonolithFootInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.MonolithFoot })
        {
            return;
        }

        long epoch = Monolith.EpochAt(SimTime);
        Monolith.Offering left = Monolith.AtTheFoot(ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch);
        if (left == Monolith.Offering.Nothing)
        {
            return;
        }

        string key = $"monolith:{epoch}";
        if (!ex.RuinsSearched.Add(key))
        {
            ShowPulseMessage("You have already looked at it. It has not changed.");
            return;
        }

        ShowAndFile(Monolith.FootLine(left, ex.Stop.Body.Id, ex.Site.LayoutSalt, epoch), "▮");

        // It costs nerve to stand here reading somebody else's last afternoon. Remains cost more.
        ApplyNerveShock(left == Monolith.Offering.Remains ? 5.0 : 2.0,
            "somebody else got this far, and this is what is left of their visit");
        RequestVaultSave();
    }
}
