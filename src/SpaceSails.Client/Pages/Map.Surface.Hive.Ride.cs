using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #585 · ARRIVING ON A FLOOR — <see cref="RideTheLiftTo"/>, the one member that turns a pressed button (or
/// a flight of stairs, or a dev cheat) into a floor the captain is standing on: the new deck, the spot they
/// land at, the patrol cleared, the save taken, and every saying the arrival has to make, held behind the
/// cards it raises rather than pulsed under them. Split out of <c>Map.Surface.Hive.cs</c> under #251 with no
/// member renamed, re-scoped or re-ordered.
/// </summary>
public partial class Map
{
    /// <param name="via">#689 · The button that was pressed, when a button was pressed. A ride that goes
    /// through the card gate has a story to tell on arrival, and only the panel knows which trip that was —
    /// the dev floor cheat rides the same car and has no gate to cross.</param>
    /// <param name="byStair">#719 · Whether this trip was made on the captain's own legs. Everything about
    /// the arrival is the same — the shed's own floor spot, the net under it, the patrol cleared, the save —
    /// except the one sentence that says a CAR did it. A stair narrated as a car would be the house bug class
    /// on the loudest line in the feature, so the line is not said and the marker beside it names the beat
    /// that is owed one.</param>
    private void RideTheLiftTo(
        SurfaceExcursion ex, int level, UndergroundComplex.LiftStop? via = null, bool byStair = false)
    {
        int fromLevel = ex.Floor;
        bool wasUnderground = ex.Floor < 0;
        ex.Floor = level;

        if (level == 0)
        {
            // #719 slice 2 · THE MAINTENANCE BREAK ENDS HERE AND NOWHERE ELSE. Owner's ruling: "the car stays
            // stopped until the captain is back on the surface; the next excursion finds it running (nobody
            // files a maintenance ticket against a man who left)." Both halves are this one line: it cannot
            // be reset by walking away, hiding, changing floor or waiting, because nothing else touches it —
            // and it cannot outlive the trip, because the flag is the excursion's own and a landing makes a
            // new one. Cleared on EVERY road to the regolith, the climb and the kick-out included: a man who
            // has been thrown out of the building is a man who left.
            ex.CarStopped = false;

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
            // #719 \u00b7 AND THE CLIMB OUT HAS ITS OWN SENTENCE NOW (Fable canon, 2026-09-04 \u2014 the line the FABLE
            // marker that stood here was left for). The two roads to the lid say different things because
            // they ARE different things: the car's line names a machine, and naming a machine that did not
            // carry you is the house bug class on the loudest line in the feature. Neither is ever said for
            // the other trip, and the stair's is said ONCE \u2014 a captain who climbs out twice in one excursion
            // has already been told what a climb costs, and the tank is the thing still saying it.
            if (byStair)
            {
                if (!ex.StairArrivalSaid)
                {
                    ex.StairArrivalSaid = true;
                    ShowAndFile(UndergroundComplex.StairArrivalLine + carried,
                        UndergroundComplex.StairArrivalGlyph, PulseRank.Beat);
                }
            }
            else
            {
                ShowPulseMessage("\ud83d\udec3 The car climbs for a long time and lets you out into somebody's idea of a " +
                    "maintenance shed. The moon is exactly as indifferent as you left it." + carried);
            }

            // #681: and the placement LAST, so if the net has to catch anybody its line is the one left on
            // screen. A rescue nobody sees is a bug nobody reports.
            StandCaptainAt(carX, carY, byStair
                ? "the stair lets you out into the shed on the surface"
                : "the car lets you out into the shed on the surface");
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

                // #677 · The pour stopping, said in the shaft on the way — and the moment the DISCLOSURE
                // CLOCK starts. Here rather than on the arrival beat below, because the seam is the crossing:
                // the clock is about the ground having been opened, and the ground is open the instant the
                // tunnel keeps going in a material the light does not grip.
                //
                // NOTHING IS SAID AND NOTHING IS SHOWN. That is the mechanic (#677: "never a progress bar and
                // never announced"), and it is why this arm looks like a book-keeping line: what the clock
                // buys is a threshold later beats guard (#1063's burial, #1068's channels, #1074's stop
                // orders), and every one of those authors its own words when it is built.
                case UndergroundComplex.ArrivalBeat.Seam:
                    ex.HiveSeamCrossed = true;
                    MarkHallsOpened(ex.Stop.Body.Id, level);
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
}
