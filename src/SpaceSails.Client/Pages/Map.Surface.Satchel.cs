using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the satchel, the field book, what you set down, and what a card is allowed to say.
public partial class Map
{
    /// <summary>#603 · "Check your items" — the satchel, opened AT something, so every item is a thing you
    /// can offer rather than a thing you can look at.</summary>
    private void OpenSatchelAtTheDoor()
    {
        if (_lockedDoor is { } door)
        {
            // #680 · Say what the thing IS, not just what is painted on it. "Offering it to MEDICAL"
            // read as a person until the owner asked what it meant — and a line the owner has to ask
            // about is a line that is not doing its job. The sign stays, the noun arrives.
            _satchelTarget = (door.Target, null,
                door.Target == SatchelTry.Target.SealedWay
                    ? $"the sealed way ({door.Sign})"
                    : $"the locked {door.Sign} door");
        }
        _lockedDoor = null;
        _satchelOutcome = null;
        TheSatchelOpensOnThePocket();
        _showSatchel = true;
    }

    /// <summary>#603 · Open it from nowhere in particular — just to see what you are carrying.</summary>
    private void OpenSatchel()
    {
        _satchelTarget = null;
        _satchelOutcome = null;
        TheSatchelOpensOnThePocket();

        // #784 · SEATED, [I] IS THE DOOR TO PROCESSING. Owner, live: "I would like to be able to see the
        // NPCs move with A* and press I to open inventory and process the loot into my detective book."
        // Standing, the key does exactly what it has always done and lands on the pocket — the fork is the
        // POSTURE and nothing else, which is why it is written here, at the one place an open happens, and
        // not in the key handler where a mouse would miss it.
        _satchelPage = CaptainIsSeatedAnywhere ? SatchelPage.Spread : SatchelPage.Carried;
        _showSatchel = true;
    }

    /// <summary>#784 · The strip's own way in, and the mouse's. Forces the spread page rather than toggling,
    /// because the button says what it opens — and says WHY NOT on the page itself when the seat refuses,
    /// since a control that opens onto an unexplained empty list is #603's founding sin with a lid on it.
    ///
    /// <para><b>#1016 · AND THE GROUND HAS NO SAY IN IT.</b> This method opened with
    /// <c>if (_surface is null) return;</c> — a line nobody had reason to doubt while every seat in the game
    /// was on an excursion. #973 L5b built the eighth seat in a DOCKED BAR, which has no excursion, and the
    /// bail made the strip's own button live and dead: the owner sat at a top in The Stormwatch Bar, pressed
    /// <b>Work the case</b>, and nothing happened at all — no book, no refusal, no sentence, which is #603's
    /// founding sin with the lid screwed down.</para>
    ///
    /// <para>Owner's ruling, 2026-08-30: <i>"Maybe it might be good idea to refactor the working the case etc
    /// table options to not be tied to any location? Kind of clean separation from the arriving random
    /// encounters that are more place tied events."</i> So the gate here is POSTURE AND PRIVACY and nothing
    /// else — which is what it always should have been, because those two are the only things the page itself
    /// says out loud (<see cref="SpreadRefusal"/>, which reads <c>SeatedIn</c>/<c>SeatedAlone</c> and has
    /// never asked for a ground). The encounters, the approach rolls, the walkers and the watch stay exactly
    /// as place-tied as they were: none of them is in this file.</para></summary>
    private void OpenTheSpread()
    {
        _satchelTarget = null;
        _satchelOutcome = SpreadRefusal;
        _satchelPage = SatchelPage.Spread;
        _showSatchel = true;
        StateHasChanged();
    }

    /// <summary>#688 · The I key, both ways. Owner: <i>"If I press I when inventory is open, let's close it
    /// then."</i>
    ///
    /// <para>A pocket you open by reflex has to shut by the same reflex. Note which way this closes: a satchel
    /// opened AT a door still closes on I, because the captain's hand is on the key and not on the fiction —
    /// and <see cref="CloseSatchel"/> clears the target, so the next I is an honest look at what you have.</para></summary>
    private void ToggleSatchel()
    {
        if (_showSatchel)
        {
            CloseSatchel();
            return;
        }

        OpenSatchel();
    }

    private void CloseSatchel()
    {
        _showSatchel = false;
        _satchelTarget = null;
        _satchelOutcome = null;

        // #741 · …and the pen goes back in the satchel with the book. A pen still in the hand of a captain
        // who has stood up and walked to a door is a state nothing on screen would be saying, and the held
        // end of a half-drawn line is not worth keeping across a shut lid.
        PutTheRedPenAway();

        // #837 · …and the load chooser is not left half-open behind a shut lid either. Its whole content is
        // live — the pocket, the guns, the reach — so a page reopened onto yesterday's split would be the
        // dialog remembering a world that has walked off.
        CloseTheLoadChooser();
    }

    private bool _showSatchel;

    // ── #690 · THE OTHER THING A SATCHEL HOLDS ──────────────────────────────────────────────────────────
    //
    // Owner, designing the paper-shedding loop: "should we have notes / clues section in our inventory ui?"
    // — and, on the register it should be written in: "it's like our detective notepad :-D".
    //
    // The field book (#587) rendered only in the Captain's ledger, a ship-brain surface. #688 made that a
    // real cost: leaving a paper files its gist to the book, so knowledge was being deliberately moved into
    // a place unreachable from the ground it came off. Record the essential data, throw out the paper, and
    // be able to read the record standing in the dark.
    //
    // A pocket and a notebook are both things a satchel holds. There is NO second store here: the tab reads
    // _fieldNotes, the one book (#587's law — one place that can never be forgotten about), through the same
    // Core projection the ledger renders.
    private enum SatchelPage
    {
        /// <summary>What you are carrying. Always where an open lands — the pocket is the primary tool.</summary>
        Carried,

        /// <summary>What the ground has told you.</summary>
        Notes,

        /// <summary>#741 v1 · THREADS — the same book, stacked by the names it has written down more than
        /// once. Always drawn, like the compass and unlike the spread: an empty threads page is an ANSWER
        /// ("nothing in this book names the same thing twice, yet"), and a tab that vanished until the case
        /// was already forming would hide the page exactly while a captain was wondering whether it
        /// existed.</summary>
        Threads,

        /// <summary>#784 · THE SPREAD — the papers laid out on the table, one dig at a time. Reachable only
        /// while seated: the tab is not drawn on your feet, and the page itself refuses out loud if you
        /// somehow arrive on it standing (<see cref="SeatedSpread"/>).</summary>
        Spread,

        /// <summary>#828 · THE BIN — the sleeve held open over the bucket you are standing at, worked sheets
        /// leading. Reachable only while a bin is within reach, exactly as the spread follows the posture:
        /// away from one this is not a page you are being refused, it is a page that does not apply. The
        /// press on a row is the same act the spread's own shredder fires (<see cref="RipAndBin"/>).</summary>
        Bin,

        /// <summary>#727 · MISSIONS — the carried compass. Owner: <i>"a mission that works outside the ship
        /// UI is something new… filter out / minimize ship-specific stuff to appropriate ('cannot do in this
        /// UI level, but high level: go to Moon X') type info in the carried mission UI."</i> What you owe,
        /// beside what you learned (📓) and what you are carrying (🎒) — one pocket per question. It reads
        /// the SAME <c>_quests</c> the captain's desk reads, through
        /// <see cref="MissionProjection.OnFoot"/>: one model, two projections, never two mission lists.
        /// Always drawn — an empty compass is an answer ("nothing owed on foot"), not a missing page.</summary>
        Missions,
    }

    private SatchelPage _satchelPage = SatchelPage.Carried;

    /// <summary>#690/#741 · WHICH READING OF THE BOOK the NOTES tab is showing.</summary>
    private enum NotesView
    {
        /// <summary>#690 · This ground alone, and where an open always lands: a captain at a door wants what
        /// THIS building has told them, not the memoirs.</summary>
        Here,

        /// <summary>#690 · The whole book, grouped by the ground it was written on — "where you were
        /// standing" is the thing a walk is reconstructed from (<see cref="Core.FieldNotes.PerPlace"/>).</summary>
        Everywhere,

        /// <summary>#741 · THE CASE. The same book with the geography demoted to a tag and the ORDER given
        /// over to the red lines — the one reading where a thread drawn between two grounds can put its two
        /// entries side by side, which is the whole reason the pen exists. Grouping by place and clustering
        /// by thread are two different arrangements of one list and cannot both be the layout, so they are
        /// two readings of it instead.</summary>
        TheCase,
    }

    private NotesView _notesView = NotesView.Here;

    /// <summary>#690 · Every open lands on the pocket, on this ground, whatever the last one was left on. The
    /// tab choice is not a setting — it is where you were looking a moment ago, and a moment ago is over.
    /// One method rather than two copies, because a third opener would forget one of these lines.</summary>
    private void TheSatchelOpensOnThePocket()
    {
        _satchelPage = SatchelPage.Carried;
        _notesView = NotesView.Here;

        // #697 · And the wallet is folded shut. A folder that reopened expanded would be the card rows it
        // replaced with an extra line above them, which is the whole row spent for nothing.
        _walletOpen = false;
    }

    /// <summary>#690 · The ground underfoot, named the way the BOOK names it — through
    /// <see cref="Core.FieldNotes.PlaceLabel"/> and never re-derived here, so the filter can never drift off
    /// the labels <see cref="FileNote"/> wrote.
    ///
    /// <para>#1016 · It used to be null off a surface, "where there is no ground to be on" — and the NOTES
    /// tab's HERE reading was consequently empty in every room the captain could sit down in ashore, one
    /// press after they had filed something into it. One answer now (<see cref="TheBooksNameForHere"/>),
    /// asked by the writer and the filter alike.</para></summary>
    private string PlaceUnderfoot() => TheBooksNameForHere();

    /// <summary>#690 · What the NOTES tab shows on this ground: the one book, filtered by the ledger's own
    /// grouping. Read-only — the book is capped and durable by its own laws and the satchel just holds it
    /// open.</summary>
    private IReadOnlyList<Core.FieldNote> NotesFromThisGround() =>
        Core.FieldNotes.Here(_fieldNotes, PlaceUnderfoot());

    /// <summary>#680 · What the last failed offer answered, said inside the dialog itself. The pulse HUD
    /// renders under the modal backdrop's blur, so a refusal routed there is in the DOM and not on the
    /// screen. Cleared whenever the satchel opens or closes — the line belongs to one conversation at one
    /// door, not to the pocket.</summary>
    private string? _satchelOutcome;

    /// <summary>What the satchel is currently open AT, if anything: the target, whatever that target needs
    /// to judge an offer, and what to call it on screen.</summary>
    private (SatchelTry.Target Target, string? Context, string Label)? _satchelTarget;

    /// <summary>#614 · The card for a carried thing, or null if it is ordinary. Asked once per row while the
    /// satchel draws, which is why <see cref="CarriedObject.Card"/> is cheap and pure.</summary>
    private CarriedObject.Reveal? LookAtItem(Core.Satchel.Item item) =>
        _surface is { } ex ? CarriedObject.Card(item, ex.Stop.Body.Id) : null;

    /// <summary>#614 · LOOK AT IT PROPERLY. Owner: <i>"we could have gen-AI images of plotwise important
    /// items... maybe they say something about what door they open."</i>
    ///
    /// <para>Free and repeatable, per #603's ruling that reading a thing and DECIDING something with it are
    /// two different acts — the owner asked for the paper to be viewable many times and the same law covers
    /// every object worth a card. The satchel stays open underneath, because a captain comparing three
    /// authority cards should not have to reopen their pockets between each one.</para></summary>
    private void OpenItemCard(Core.Satchel.Item item)
    {
        if (LookAtItem(item) is not { } card)
        {
            return;
        }

        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            card.Label, card.ArtUrl, card.Story);
    }

    /// <summary>#688 · LEAVE IT. Owner, live: <i>"The keycard story is already big, but no way to drop
    /// stuff."</i>
    ///
    /// <para>The satchel had a verb for offering a thing and a verb for looking at one, and none at all for
    /// putting one down — while the game's own prose kept telling the captain that something had to be
    /// <i>read, spent or left behind</i>. The only way to make room was to spend something.</para>
    ///
    /// <para>It is its own small control per row rather than a mode, for #614's reason one size down: a
    /// captain making room must never be one mis-click away from offering a relic to a bulkhead. The satchel
    /// STAYS OPEN — you are putting a thing down in order to pick a thing up — which is exactly why the
    /// confirmation is stored for the dialog rather than pulsed (#680: the pulse HUD renders under the
    /// backdrop's blur, so a line sent there is in the DOM and not on the screen).</para>
    ///
    /// <para>#615's law, unbroken: leaving never destroys. It is lying on the square the captain is standing
    /// on, and <see cref="TryPickUpWhatYouLeft"/> hands it straight back.</para></summary>
    private void LeaveItem(Core.Satchel.Item item)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        string standing = WhereYouAreStanding();

        // ── #696 · A DOCUMENT IS NOT DROPPED. IT IS PROCESSED, AND THAT TAKES TIME ──
        //
        // Owner: "we take time to process the loot." Leaving a paper files what it SAID (below), which is
        // the captain photographing it — and that is seconds of standing still, not a click. Only documents
        // hold: there is no gist to a handful of rounds, so there is nothing to stand still for, and the
        // question "is there a gist" is asked ONCE, by Core, here and at the far end alike.
        if (LeftBehind.GistOf(item, standing) is { Length: > 0 })
        {
            BeginProcessing(Core.Processing.Work.File, item, standing, at: null);
            return;
        }

        SetItDown(ex, item, standing);
    }

    /// <summary>#688 · The drop itself, once whatever had to happen first has happened. Everything that was
    /// in <see cref="LeaveItem"/> before #696 put a clock in front of it — unchanged, because the effect at
    /// the far end of a hold must be the effect the game already had, not a second copy of it.</summary>
    private void SetItDown(SurfaceExcursion ex, Core.Satchel.Item item, string standing)
    {
        // Rounds go down as the whole stack: six rounds is ONE thing you are carrying (#603), so it is one
        // thing you set down. Leaving them a round at a time would be inventory management.
        (int sqX, int sqY) = BeachComber.SquareOf(_avatarX, _avatarY);
        ex.Ground.Leave(LeftBehind.SpotKey(ex.Floor, sqX, sqY), item);
        _satchel = [.. Core.Satchel.Remove(_satchel, item.Kind, item.Id, item.Count)];

        // ── #688 · A DOCUMENT LEAVES ITS GIST BEHIND IN THE BOOK ──
        //
        // Owner refinement on the drop verb: leaving a paper files what it said before the paper leaves the
        // pocket. A captain does not abandon a pay sheet without having looked at it — what they are putting
        // down is the SHEET, and the sheet was only ever costing them bulk. The sleeve empties; the knowledge
        // does not, which is #587's law ("a find that is shown once is a find that is lost") arriving at the
        // moment the captain lets go.
        //
        // FileNote and not ShowAndFile: the SAYING is one line and it goes wherever the captain is actually
        // looking (#680/#686), which is what SayItWhereTheyAreLooking decides. A second pulse for the gist
        // would be the book reading itself out loud.
        string? gist = LeftBehind.GistOf(item, standing);
        if (gist is { Length: > 0 })
        {
            FileNote(gist, item.Kind == Core.Satchel.Kind.Dirt ? "🗃" : "📋");
        }

        SayItWhereTheyAreLooking(LeftBehind.LeaveLine(SatchelLabel(item), standing, gist is not null));

        // #698 · AND THE DECK SAYS SO IMMEDIATELY. Owner: "there was nothing marked onto the map?" The
        // marks are composed by the rebuild, so a drop that did not rebuild would leave the captain looking
        // at the ground they just used and seeing the same nothing that produced the complaint. It is the
        // same one-append-on-a-memoized-base the bury and the door-force already pay.
        RebuildSurfaceDeck();
        RequestVaultSave();
    }

    /// <summary>#680 + #696 · Say it where the captain is looking. The law #680 wrote is about the BLUR and
    /// not about the pulse: with the satchel open over the world a line sent to the HUD is in the DOM and not
    /// on the screen, and with the satchel shut a line stored for the dialog is nowhere at all.
    ///
    /// <para>It became a fork rather than a fact the moment #696 let a leave finish with the dialog closed —
    /// the hold shuts the satchel so the captain can watch the fan, and they may or may not have opened it
    /// again by the time the shutter closes. One method, asked by every caller, so no site has to guess.</para>
    ///
    /// <para>#736 · AND THE FORK GREW THE REST OF THE POP-UPS. Owner, restating the law in general: <i>"When
    /// an action is made on a pop-up, the result text must be readable on that pop-up — not on the blurred
    /// background. All actions, like using the elevator, items, etc. should report to the pop-up so the text
    /// is not blurred by the modal backdrop."</i> The seam existed and knew about exactly one dialog, so
    /// every other pop-up in the game went on losing its answers to the blur one organ at a time (#680 the
    /// satchel, #686 the car panel, #736 the freight agent's receipt behind his own card).</para>
    ///
    /// <para>The table below is that law, in one place, read TOP-DOWN in z-order: the pop-up nearest the
    /// captain's eye owns the answer, because that is the one their eye is on and the one whose subtree the
    /// backdrop cannot blur. Nothing in front of them at all, and the HUD's pulse is exactly right — a line
    /// about the world, said on the world.</para></summary>
    private void SayItWhereTheyAreLooking(string line)
    {
        // #774 · The object card, and it is FIRST because it is on top: both full-screen cards are drawn
        // with the same backdrop class, and this one is written later in Map.razor, so when an event raises
        // both (the outpost's effects plate under the dossier it assembles) this is the one the captain's
        // eye is on.
        //
        // It APPENDS where every other row below replaces, and that difference is the whole of #774. The
        // events that raise this card have two to five things to say IN ONE BREATH, all at the same rank —
        // a slot has one winner and picking it by write order is the contract #693 killed, while a region
        // has room for all of them and so has no winner to pick. Answers arriving one press at a time (the
        // panels below) are a slot's proper business and stay one.
        if (_viewObject is { } shown)
        {
            _viewObject = shown with
            {
                Outcome = shown.Outcome is { Length: > 0 } already ? $"{already}\n\n{line}" : line,
            };
            return;
        }

        // A raised card is the most modal thing in the game — it stops the world and waits to be dismissed,
        // and everything else on this list is behind it. Its answer rides the card record itself, so it
        // cannot outlive the card it belongs to.
        //
        // #664 · The card this used to find was the one out of the deleted Map.RevealCard.cs, the client-only
        // twin the reunification merge kept on purpose. There is one card now — the StoryBeats one, which
        // carries the same outcome field and, unlike its twin, a cadence and a deferral. (The dead field's
        // NAME is not written here on purpose: `ThereIsOnlyOneRevealCardSystemLeft` sweeps for it, and a
        // guard that had to learn the difference between a call and a comment would be the weaker guard.)
        if (_storyCard is { } card)
        {
            _storyCard = (card.Beat, card.Subject, line);
            return;
        }
        if (_showSatchel)
        {
            _satchelOutcome = line;     // #680 — the dialog this law was first written for
            return;
        }
        if (_showLiftPanel)
        {
            _liftOutcome = line;        // #686 — the car panel, the same disease's second organ
            return;
        }
        if (_pinJob is not null)
        {
            _pinOutcome = line;         // the hatch keypad: a buzz that is not read is a keypad that is broken
            return;
        }
        if (_showAlarmPanel)
        {
            _alarmOutcome = line;
            return;
        }
        if (_showDoorBoard)
        {
            _doorBoardOutcome = line;
            return;
        }
        if (_showCaptainsRemote)
        {
            _remoteOutcome = line;
            return;
        }
        if (_showChargeBoard)
        {
            _chargeBoardMessage = line; // the board already had a slot of its own — this only aims at it
            return;
        }
        if (_showVentPanel)
        {
            _ventMessage = line;        // ditto: the valve board says everything else it does right here
            return;
        }
        if (_barMenu is not null)
        {
            _barNotice = line;          // the counter-example #736 was filed against — the keep answers on his card
            return;
        }
        ShowPulseMessage(line);
    }

    /// <summary>#688 · What is at your feet, answered before what is in the walls. Returns true when the press
    /// was spent on the ground — the console under the captain gets the NEXT one, which is the honest order:
    /// a thing you put down yourself is not a thing you should have to walk away from to reach.</summary>
    private bool TryPickUpWhatYouLeft()
    {
        if (_surface is not { } ex)
        {
            return false;
        }

        // The square the captain is standing on, and the ring around it. A three-metre grid cell is smaller
        // than a captain's idea of "where I put it down", and #615's law is only real if the way back is
        // real — a relic you cannot find again was destroyed, whatever the store says it is holding.
        //
        // #698 · THE RING IS ONE RING. This scan used to be written out here, which made the keybar's new
        // offer ("E — take back what you left") a SECOND transcription of the same geometry — and a prompt
        // measured off a copy of the law is the bug class this repo has already paid for four times. Core
        // owns it now; the key and the sentence ask the identical function.
        if (ex.Ground.SpotInReach(ex.Floor, _avatarX, _avatarY) is not { } spot)
        {
            return false;
        }

        LeftBehind.Recovery back = ex.Ground.PickUp(spot, _satchel);
        _satchel = [.. back.Pocket];

        // Both halves are named. A recovery that quietly left something on the floor without saying so would
        // be #678's silent drop one verb later — and this time the captain put it there on purpose.
        ShowPulseMessage(LeftBehind.FoundAgainLine(
            [.. back.Taken.Select(SatchelLabel)],
            [.. back.StillThere.Select(SatchelLabel)]));

        // #698 · The mark goes when the spot does — and STAYS when it does not. A recovery that could not
        // take everything leaves the rest lying there (the one operation whose failure mode must leave the
        // world as it found it), so the redraw is the honest one either way: the composer reads the store.
        RebuildSurfaceDeck();
        RequestVaultSave();
        return true;
    }

    /// <summary>#698 · Is the captain inside the recovery ring of a spot with something lying on it? The
    /// keybar's question, and it is <see cref="LeftBehind.SpotInReach"/> — the SAME call
    /// <see cref="TryPickUpWhatYouLeft"/> makes — so the offer on the bar and the press it advertises can
    /// never come apart. Off an excursion there is no ground to have left anything on.</summary>
    private bool StandingOnWhatYouLeft() =>
        _surface is { } ex && ex.Ground.AnythingInReach(ex.Floor, _avatarX, _avatarY);

    /// <summary>#688 · Where the captain is, in their own words, for the line that says where a thing was
    /// left. Underground it is a floor with a number painted on it; up top it is the ground.
    ///
    /// <para>#1016 · <b>AND THERE ARE TWO MORE PLACES A CASE CAN BE WORKED NOW.</b> Off an excursion this
    /// answered <i>"on the regolith at your feet"</i> — a sentence about a moon, printed for a captain sitting
    /// in a station bar or in her own galley, which is #562's class exactly: the prose reporting one world
    /// while the sim is standing in another. It was invisible while the seat verbs were gated on a ground and
    /// arrived the moment they stopped being. The two clauses are Fable's own words and the disposition
    /// clause folds them in without a seam: <see cref="LeftBehind.GistOf"/> writes <i>"read through
    /// {where} and copied out"</i> for a kept sheet and <i>"read and left {where}"</i> for a photographed
    /// one, so each of the four readings is a sentence somebody would say out loud.</para>
    ///
    /// <para>ASKED TOP-DOWN, ground first: an excursion is the most specific thing the captain can be
    /// standing on, and a captain who is on one is never also ashore in a berth.</para></summary>
    private string WhereYouAreStanding() =>
        _surface is { Floor: < 0 } ex ? $"on the floor of B{-ex.Floor}"
        : _surface is not null ? "on the regolith at your feet"
        // Past the tube, in the station's own rooms — the flag the deck keeps (`RefreshAshore`), never a
        // coordinate re-measured here.
        : _dockedHavenId is not null && _ashore ? "on the haven's deck"
        : "aboard your own boat";

    /// <summary>
    /// #1016 · WHAT THE BOOK CALLS THE PLACE THE CAPTAIN IS IN — the one answer
    /// <see cref="FileNoteAbout"/> writes onto an entry and <see cref="PlaceUnderfoot"/> filters the NOTES
    /// tab by, so the two can never name one place differently.
    ///
    /// <para>It was <c>_surface</c>'s alone, which is why filing a note off an excursion did not merely land
    /// in the wrong drawer — <c>FileNoteAbout</c> RETURNED, and the entry a dig had just spent twenty seconds
    /// on was dropped on the floor. The dig at a bar top would have filled its bar, said its line and written
    /// nothing at all.</para>
    ///
    /// <para>Built through <see cref="Core.FieldNotes.PlaceLabel"/> in every arm, never assembled here, so a
    /// berth's grouping is the same shape a moon's is and the ledger's own <c>PerPlace</c> reading needs to
    /// learn nothing about a station. A berth is named by its BODY and its bar (<i>The Red Eye · The
    /// Stormwatch Bar</i>); the boat is named the way the game names her everywhere else.</para></summary>
    private string TheBooksNameForHere()
    {
        if (_surface is { } ex)
        {
            return Core.FieldNotes.PlaceLabel(ex.Stop.Body.Name, ex.Site.Name);
        }
        if (_dockedHavenId is { } berth && _ashore)
        {
            return Core.FieldNotes.PlaceLabel(DockedStationName(), HavenInterior.BarNameOf(berth));
        }
        return Core.FieldNotes.PlaceLabel(Core.FieldNotes.YourOwnBoat, null);
    }

    // ── #587 · THE FIELD BOOK ──────────────────────────────────────────────────────────────────────────
    //
    // Owner: "we should maybe collect the tips to ledger if we don't show them again?" — and he is right,
    // because until now they were NOT shown again. Every find out there arrived through ShowPulseMessage,
    // which fades after at most eight seconds and is then gone: you walk twenty minutes across a vacuum for
    // a sentence you cannot read twice.
    //
    // Exactly the bug he already ruled on for the bar (#347: the words a player paid for "may not hide"),
    // so it gets exactly the same answer — a durable, capped, vault-persisted book, projected into the
    // captain's ledger grouped by PLACE. On the ground the thing you want back is not who told you, it is
    // where you were standing.
    private List<Core.FieldNote> _fieldNotes = [];

    // #585 · MOONS SOMEBODY HAS NAMED. Owner: "We will be needing some kind of clue in the plot arc to the
    // radar to really find it in reasonable time in the game :-D ... now we kind of found it by just knowing
    // it is here somewhere."
    //
    // This is the missing link in the whole gumshoe chain. A clue found in a facility, a ruin or a dead
    // specialist's family names a MOON; landing on a named moon wakes the detector and the tracker's vague
    // wash. Without it the search was a lottery with four thousand tickets.
    private readonly HashSet<string> _labLeads = [];

    // ── #603 · THE SATCHEL: everything the captain is carrying on foot ──────────────────────────────────
    //
    // Owner: "we should have some option to try use those at the locked doors... maybe we need like
    // on-site-carried-items inventory ... The captains ledger has the ship stuff but we should have
    // something similar on foot."
    //
    // This REPLACES the #590 card set. Cards were the first thing the captain carried that the player could
    // not see, and by the time papers, files and loose rounds joined them there would have been four private
    // stores and still no pockets. One list now, and a panel draws it.
    //
    // Durable, not per-excursion: you find a thing eleven floors under a moon, fly home, come back a month
    // later and it is still in your pocket. So it rides in the vault beside the leads rather than on the
    // SurfaceExcursion, which is thrown away the moment the shuttle lifts.
    private List<Core.Satchel.Item> _satchel = [];

    /// <summary>#590 · The card ids the lift panel and its gate ask about, DERIVED from the satchel so there
    /// is one store. A second copy kept in step by hand is the failure this ground's spec opens with a table
    /// of.</summary>
    private HashSet<string> AuthorityCardIds() =>
        [.. Core.Satchel.OfKind(_satchel, Core.Satchel.Kind.Authority).Select(i => i.Id)];

    // #684 · HeldAuthorities() lived here — the wallet parsed back into cards, sorted, so the panel's own
    // refusal line could name every one of them. It went with WrongCardLine: the gate's answer is the
    // matrix's now, and the matrix is handed the SATCHEL rather than a second list derived from it.

    /// <summary>Say it AND keep it. Every durable find on a surface goes through here rather than through
    /// ShowPulseMessage directly, so there is one place that can never be forgotten about — the pulse is the
    /// doorbell, the book is the record.
    ///
    /// <para>#693 · The book keeps everything whatever the screen does, so <paramref name="rank"/> only ever
    /// decides the doorbell. A line that loses the slot is still filed, in the order it was said.</para>
    /// </summary>
    private void ShowAndFile(string text, string glyph, PulseRank rank = PulseRank.Status)
    {
        ShowPulseMessage(text, rank);
        FileNote(text, glyph);
    }

    /// <summary>#1074 · <see cref="ShowAndFile"/> for a find whose AUTHOR knows what its sentence is about —
    /// the same relationship <see cref="FileNoteAbout"/> has to <see cref="FileNote"/>, and it exists for the
    /// identical reason. An empty <paramref name="subjects"/> is the ordinary case and is exactly what
    /// <see cref="ShowAndFile"/> already files, so the two never disagree about a note that names
    /// nothing.</summary>
    private void ShowAndFileAbout(
        string text, string glyph, string subjects, PulseRank rank = PulseRank.Status)
    {
        ShowPulseMessage(text, rank);
        FileNoteAbout(text, glyph, subjects);
    }

    /// <summary>#774 · <see cref="ShowAndFile"/>'s sibling for a durable find announced in the same breath as
    /// a card that will stand in front of it: the book keeps it, and the SAYING goes wherever the captain's
    /// eye actually is (<see cref="SayItWhereTheyAreLooking"/>) rather than to a HUD behind a backdrop. With
    /// nothing raised the two are the same method — there is no rank because there is no contest: this line
    /// is not competing for a slot, it is being written where it can be read.</summary>
    private void SayWhereTheyAreLookingAndFile(string text, string glyph)
    {
        SayItWhereTheyAreLooking(text);
        FileNote(text, glyph);
    }

    // ── #768 · WHEN THE EVENT THAT SPEAKS ALSO RAISES A CARD ───────────────────────────────────────────
    //
    // #693 gave the one pulse slot a law, and left one loss it could not settle: an ARRIVAL that raises a
    // card says its lines and then puts a full-screen backdrop in front of them. The first-descent card
    // (#585) over the gate-accepted beat (#689) is the case the owner filed; the repo boat's plate (#583)
    // over its own arrival line is the same shape. No rank helps — the line is not losing to a bigger line,
    // it is losing to the whole HUD, and the dwell runs out behind the blur while the captain reads the card.
    //
    // So an event that may raise a card HOLDS its sayings (PulseHold, Core — the same rank law, minus the
    // clock, because the lines were composed in one breath) and the card's dismissal lets the winner go. The
    // book still keeps every one of them at the moment they were said: what is deferred is the DOORBELL, not
    // the record, and not the event.
    //
    // Deliberately NOT a general queue: #693 declined that and it stays declined. An event that raises no
    // card releases on the spot, which is an ordinary pulse and indistinguishable from one.

    /// <summary>#768 · Say it AND keep it — but not yet, if a card is about to stand in front of it. The book
    /// gets it now; the screen gets the winner when the card closes. Pair every caller with
    /// <see cref="ReleaseHeldSayingsUnlessACardStopsTheWorld"/> once the event's cards have been raised.</summary>
    private void HoldAndFile(string text, string glyph, PulseRank rank = PulseRank.Status)
    {
        _held = _held.Hold(text, rank);
        FileNote(text, glyph);
    }

    /// <summary>#768 · The same, for a line the book does not keep — a warning about the here and now rather
    /// than a durable find.</summary>
    private void HoldSaying(string text, PulseRank rank = PulseRank.Status) =>
        _held = _held.Hold(text, rank);

    /// <summary>#768 · Is something in front of the captain that a pulse would play UNDER? The two full-screen
    /// cards, asked of the world as it now stands rather than predicted from the conditions that raise
    /// them — a copy of those conditions is a second rule to keep in step, and this one cannot be wrong.</summary>
    private bool ACardStopsTheWorld => _viewObject is not null || _storyCard is not null;

    /// <summary>#768 · The end of an event that had things to say: if nothing is in front of the captain the
    /// held winner is simply pulsed, here and now, exactly as it always was. If a card IS up, it stays held
    /// and <see cref="ReleaseHeldSayings"/> says it when that card is dismissed.</summary>
    private void ReleaseHeldSayingsUnlessACardStopsTheWorld()
    {
        if (ACardStopsTheWorld)
        {
            return;
        }
        ReleaseHeldSayings();
    }

    /// <summary>#768 · The card is gone — say what it was standing on. Called from every road out of a card,
    /// which is why all of them go through CloseViewObject / CloseStoryCard and none of them clear the field
    /// by hand. A released line takes its ordinary dwell and may be outranked afterwards like any other: a
    /// held line is a line that has not been said yet, never a line with special powers.</summary>
    private void ReleaseHeldSayings()
    {
        if (!_held.Any)
        {
            return;
        }
        (_pulse, _held) = _held.ReleaseInto(_pulse, _lastTimestampMs ?? 0);
    }

    /// <summary>#686 · The record half alone, for a line whose SAYING happens inside an open dialog — the
    /// pulse would play under that dialog's blur, but the book must still remember. What almost every one of
    /// the four dozen filing sites calls, because almost every sentence in the game names nothing the game
    /// has printed.</summary>
    private void FileNote(string text, string glyph) => FileNoteAbout(text, glyph, "");

    /// <summary>#741 v1 · The same act, by an author that KNOWS WHAT ITS SENTENCE IS ABOUT — it built the
    /// words out of a person, an office and a door, so it says so
    /// (<see cref="Core.CaseSubjects.Line(Core.CaseSubjects.Subject[])"/>) and nothing downstream ever reads
    /// the prose back to find out.
    ///
    /// <para>A separate name rather than a third parameter on <see cref="FileNote"/>: the two-argument form
    /// is the one four dozen sites call and several guards reach for by reflection, and a defaulted
    /// parameter would quietly change that signature for all of them.</para></summary>
    private void FileNoteAbout(string text, string glyph, string subjects)
    {
        // #1016 · The excursion clause that used to stand here was the quiet half of the dead button: a dig
        // at a bar top can fill its bar and say its line, and the entry it was FOR would have been dropped
        // right here for want of a moon. What a note needs is a sentence and a name for the place, and both
        // exist wherever the captain is standing.
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var note = new Core.FieldNote(text, SimTime, TheBooksNameForHere(), glyph, subjects);
        _fieldNotes = [.. Core.FieldNotes.Append(_fieldNotes, note)];
        TheThreadBadgeGoesOnTheCard(note);
    }
}
