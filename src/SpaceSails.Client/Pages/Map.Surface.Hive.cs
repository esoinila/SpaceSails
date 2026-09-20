using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// Part of Map.Surface (#870 split; the header note lives in <c>Map.Surface.cs</c>) — THE SHAFT AND THE
/// PANEL ON IT: the disclosure clock's register of grounds already gone past the seam, the [E] at the cage,
/// which stops this band offers, the car the captain is standing in, and the button press that either
/// refuses and says so inside the panel or accepts and starts a ride.
///
/// <para>#251 · The rest of the Hive moved into partials of its own by concern — the ride itself in
/// <c>Map.Surface.Hive.Ride.cs</c>, the search and sign verbs in <c>Map.Surface.Hive.Search.cs</c>, and the
/// secret-lab thread (the assembled person, the detector, the lead, the monolith's foot) in
/// <c>Map.Surface.Hive.SecretLab.cs</c>. Fourteen test classes read this page as TEXT, so they read the
/// FAMILY: <c>MapMarkup.Read</c> hands any of them the four files concatenated in this declared order,
/// which is the same text they read out of one file before the cut. No partial of this family declares a
/// static field (#1163).</para>
/// </summary>
public partial class Map
{
    // ── #677 · THE DISCLOSURE CLOCK'S REGISTER ───────────────────────────────────────────────────────────
    //
    // The grounds this game-thread has been past the seam of, and the world-side window each was opened in.
    // Persisted per-universe in the vault's ProgressSection, the same idiom as _secretLabsFound — and for a
    // harder version of its reason: that one remembers where a door is, this one remembers WHEN, and a clock
    // that forgot across a reload would reset every threshold written against it with nothing on screen ever
    // saying so.
    //
    // Nothing reads it yet, on purpose. Core's DisclosureClock carries the whole argument.
    private IReadOnlyList<DisclosureClock.Opening> _hallsOpened = [];

    // The clock starts, once per ground. DisclosureClock.Note keeps the FIRST crossing, so a revisit cannot
    // move it, and the register is handed back by reference when there is nothing to add — which is what the
    // save is asked for on.
    private void MarkHallsOpened(string bodyId, int level)
    {
        if (DisclosureClock.Open(bodyId, level, SimTime) is not { } opening)
        {
            return;
        }
        IReadOnlyList<DisclosureClock.Opening> next = DisclosureClock.Note(_hallsOpened, opening);
        if (!ReferenceEquals(next, _hallsOpened))
        {
            _hallsOpened = next;
            RequestVaultSave();
        }
    }

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
    /// (<c>CanteenTable.Cover</c>) — asked of the same pocket the player can open and look in.</para>
    ///
    /// <para>#719 slice 2 · <b>AND A STOPPED CAR HAS NO FLOORS.</b> The break empties the list rather than
    /// drawing thirteen rows that all refuse: a refusing row is right where a GATE exists and the paper is
    /// missing (#590), and there is no gate here — there is no car. What stands in the list's place is the
    /// plate, drawn by <c>LiftPanel.razor</c>, and it is the whole of the telling.</para>
    ///
    /// <para>BOTH cars, and that is a ruling. The goods car is a car (#801) and the break is a fact about the
    /// building's lifts rather than about the one panel a captain happened to press; a live panel at the
    /// other end of the corridor would teach a captain that a maintenance break is something you walk around,
    /// which is the opposite of what it is. What you walk to is the stair.</para></summary>
    ///
    /// <para>#1253 · <b>AND A BERTH'S THREE CARS ANSWER HERE TOO.</b> This is the one list
    /// <c>LiftPanel.razor</c> draws, and a second lift surface would be a second set of buttons to keep in
    /// step with the first. The haven's stops are Core's (<see cref="HavenLevels.Panel"/>) and nothing about
    /// a moon is asked of them — no band, no card, no keypad, no dead air, and never <c>ShaftsOn(field)</c>,
    /// because there is no field. Asked FIRST, because a berth has no excursion for the arms below it to
    /// read.</para>
    private IReadOnlyList<UndergroundComplex.LiftStop> LiftStops() =>
        TheStationHasFloors
            ? HavenLiftStops()
            : TheCarIsStopped
            ? []
            : _surface is { } ex
            ? UndergroundComplex.LiftPanel(
                ex.Stop.Body.Id, ex.Floor, _liftCar, AuthorityCardIds(), _satchel,
                // #715 · …and what this site's outfit remembers, which is what decides whether the gate is
                // content with the paper or wants the face as well.
                IllegalHeat.HeatAtSite(_contacts, ex.Stop.Body.Id),
                // #602 · …and which bands the KEYPAD has been talked into on this trip. The excursion's own
                // set, never the vault's: a code opens a gate for the afternoon and the card opens it
                // forever, which is the whole line between the two papers.
                ex.LiftCodeOpened,
                // #1149 · …and whether an inspection is running on this trip — the excursion's own flag, on
                // the excursion for the pad's reason exactly. While it is, the ID CHECK row defers and the
                // SEALED row opens.
                ex.InspectionRunning)
            : [];

    /// <summary>#801 · Which of the two cars the open panel belongs to — set by the press that opened it,
    /// and read by the panel, the ride and the placement so that all three are talking about one machine.
    /// The cage until somebody walks to the other end of the corridor.</summary>
    private UndergroundComplex.ShaftKind _liftCar = UndergroundComplex.ShaftKind.Cage;

    /// <summary>#801 · What the open panel says under its title. The cage's line is the one it has always
    /// had; the goods car's names where the cage is, which is the anti-choke feature said in a sentence.</summary>
    ///
    /// <para>#1253 · …and a STATION's car says nothing at all under its title, which is a ruling and not an
    /// omission. Both of the Hive's lines exist to tell a captain what a car will NOT do — a band it cannot
    /// leave, a surface it cannot climb to — and a haven's three cars each serve both of the building's two
    /// floors. There is nothing to warn anybody about, so nothing is written, and the row under the title is
    /// the sentence the panel has always had: <i>You are at CONCOURSE.</i></para>
    private string LiftPanelLine() =>
        TheStationHasFloors ? ""
        : _liftCar == UndergroundComplex.ShaftKind.Cage
            ? "This car serves its own band and no further."
            : UndergroundComplex.ServiceCarPanelLine;

    /// <summary>#1253 · <b>WHAT FLOOR THE CAR SAYS IT IS ON</b>, under the panel's title — the one line in
    /// this surface that used to read the excursion directly (<c>liftEx.Floor</c>) and therefore could not be
    /// drawn at a berth at all.
    ///
    /// <para>Underground it is the depth paint this game has always used (<c>B4</c>, <c>SURFACE</c>); at a
    /// berth it is the level's own plate, which is the same string the button beside it carries. The car
    /// announces its floor the way the Hive's does, and it announces it out of ONE method, so the panel and
    /// the stop list cannot come to two names for one floor.</para></summary>
    private string LiftPanelDepth() =>
        TheStationHasFloors
            ? HavenLevels.NameOf(_havenFloor)
            : _surface is { } depthEx ? UndergroundComplex.DepthPaint(depthEx.Floor) : "";

    /// <summary>#600 · A button was pressed. A refusing button says why and the car does not move — a button
    /// that is present and explains itself is the entire reason it is not simply absent.</summary>
    private void PressLiftButton(UndergroundComplex.LiftStop stop)
    {
        // #1253 · A STATION'S CAR ANSWERS FIRST, and it answers with the whole of what it does: there is no
        // gate on it, no paper to read and nothing to refuse — the two floors of a building the captain is
        // already standing in. #600's scar is a car that only went down, and the honest way not to repeat it
        // is a panel that cannot.
        if (TheStationHasFloors)
        {
            if (HavenLevels.RideFrom(_havenFloor, in stop) is { } floor)
            {
                _showLiftPanel = false;
                _ = RideTheHavenLiftTo(floor, _havenLiftCage);
            }

            return;
        }

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
            //
            // #715 · …and the read is asked WITH what this outfit remembers. Past the meter's middle rung the
            // panel wants a face with the paper — the site's own pass, on the FIRST press — which is what
            // "the doors that were open are watched now" means in the vocabulary this building already has.
            // At any other outfit's identical gate the identical card opens, and that difference is the whole
            // of what the captain is meant to learn from it.
            UndergroundComplex.GateRead read = UndergroundComplex.TheGateReads(
                ex.Stop.Body.Id, ex.Floor, _satchel, IllegalHeat.HeatAtSite(_contacts, ex.Stop.Body.Id));
            int refusedBand = UndergroundComplex.BandOf(stop.Level);
            if (ex.HiveShaftsRefused.Add(refusedBand))
            {
                _liftOutcome = read.Line;
                FileNote(read.Line, "🔒");

                // #715 · ONE CROSSING, ONE BANK. It rides the same latch the note and the card ride — once
                // per shaft per excursion — because pressing one refusing gate eleven times is one refusal
                // that somebody wrote down, and eleven charges for it would be this feature's fourth guard
                // going red on its own game.
                BankTheCrossing(UndergroundComplex.RefusedAtTheGate(ex.Stop.Body.Id));

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

        // #602 · …and the pad's DISPLAY is cleared with it, along with the plate it last showed. Both belong
        // to one stand at one panel. What is deliberately NOT cleared is ex.LiftPad: the window forgets by
        // TIME and by nothing else, which is the owner's ruling — a captain who missed twice and stepped out
        // of the car for ten seconds has not been forgiven, and one who waited ninety has.
        if (_surface is { } ex)
        {
            ex.LiftPadEntry = "";
            ex.LiftPadSaid = null;
        }
    }

    /// <summary>#600 · Whether the lift panel is up. The sim keeps running behind it, exactly as it does
    /// behind the valve board.</summary>
    private bool _showLiftPanel;

    /// <summary>#686 · What the last refused button answered, said inside the panel itself. The pulse HUD
    /// renders under the modal backdrop's blur, so a refusal routed there is in the DOM and not on the
    /// screen. Cleared whenever the panel opens or closes — the line belongs to one stand at one gate.</summary>
    private string? _liftOutcome;
}
