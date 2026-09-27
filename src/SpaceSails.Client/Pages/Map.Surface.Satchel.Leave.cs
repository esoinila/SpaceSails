using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · WHAT YOU DO WITH A THING IN YOUR HAND (#688, #696) — the item card, leaving it and setting it
/// down, saying it where they are looking, picking up what you left, and the book's name for where you
/// stand.
///
/// <para>Split out of <c>Map.Surface.Satchel.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field — every field of the satchel stays in the opening
/// file.</para>
/// </summary>
public partial class Map
{
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
    /// about the world, said on the world.</para>
    ///
    /// <para>#761 · <paramref name="rank"/> is for the last row only, and it is the owner's law arriving at
    /// the one exit that has a contest in it. Every pop-up row above returns into a surface that is already
    /// the captain's whole screen — nothing races there — but the fall-through writes into the HUD's single
    /// slot, where #693's ranks decide who is standing when the frame ends. A plot-significant sentence sent
    /// down that road at <see cref="PulseRank.Status"/> can be displaced by the next instrument reading, and
    /// then the moment was told to nobody. It defaults to Status because that is what nearly every line in
    /// the game is; see <see cref="Telling"/> for what the top two ranks mean and the warning that goes with
    /// them.</para></summary>
    private void SayItWhereTheyAreLooking(string line, PulseRank rank = PulseRank.Status)
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
        ShowPulseMessage(line, rank);
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
            // #1253 — AND WHICH FLOOR OF IT, but only when it is not the one the berth's name already
            // implies. A note filed in the bar goes on reading exactly as it did (the suffix is empty at the
            // concourse), and a note filed on a service level says so, because a captain going back through
            // his own book for where he was standing is asking about a PLACE, and two floors of one station
            // are two places. The suffix is Core's, so the drawer a note is filed in and the drawer the
            // ledger's PerPlace reading groups it under are one string.
            //
            // (The separator itself is never spelled here — that is #690's law, and this method's own
            // guard reads the body for it.)
            string floor = HavenLevels.BookSuffix(_havenFloor);
            return Core.FieldNotes.PlaceLabel(
                DockedStationName(),
                floor.Length == 0 ? HavenInterior.BarNameOf(berth) : floor);
        }
        return Core.FieldNotes.PlaceLabel(Core.FieldNotes.YourOwnBoat, null);
    }
}
