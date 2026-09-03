using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #615's decision at
// the moment of a find: KEEP or LEAVE, the register of rooms already turned over, and the full sleeve.
public partial class Map
{
    /// <summary>
    /// #615/#573 · <b>WHICH ROOMS THIS CAPTAIN HAS ALREADY GONE THROUGH, ANYWHERE.</b>
    ///
    /// <para>A page-level register beside <c>_workedUp</c> and the satchel, and kept there for #1016's own
    /// reason said one issue later: <i>a register that belongs to the captain belongs to the captain, and a
    /// captain's things ride the vault.</i> The excursion's <c>HiveRoomsEmptied</c> is still the live set the
    /// deck builder reads — it has to be, it is keyed per floor and per frame — but it is now a VIEW of this
    /// one, seeded when the shuttle lands and written through on every strike-off.</para>
    ///
    /// <para><b>It is what makes LEAVE mean anything.</b> Before this, flying away re-filled a facility, so a
    /// room the captain emptied and a room the captain declined were indistinguishable the moment the shuttle
    /// lifted — and a guard that asked "is the paper still there when I come back?" would have been green
    /// against both, which is this repository's fifth named bug class wearing a test's clothes. Now the world
    /// can tell the two apart, which is the only condition under which the question is worth asking.</para>
    /// </summary>
    private readonly HashSet<string> _roomsTurnedOver = [];

    /// <summary>
    /// #615/#573 · <b>THE REGISTER, ON ITS WAY TO THE FILE.</b> Null when nothing has been gone through, so
    /// the save is exactly as large as the captain's history is — the section idiom every other optional
    /// register on the vault uses.
    ///
    /// <para>Here rather than inline in <c>Map.Vault.cs</c> because that file is the one this repo's size
    /// gate sits closest to: it had ten lines of daylight under the 1,500 line after this lane's first
    /// draft, and the gate's own margin check went red saying so. The register's two ends belong beside the
    /// register anyway.</para>
    /// </summary>
    private TurnedOverSection? TheRoomsGoneThrough() =>
        _roomsTurnedOver.Count > 0 ? new TurnedOverSection { Rooms = [.. _roomsTurnedOver] } : null;

    /// <summary>
    /// #615/#573 · …and on its way back. An unrecognised key is KEPT rather than dropped, the way
    /// <c>WorkedUp</c>'s is: it costs one string and can only ever say "already emptied" about a room this
    /// build cannot generate, while dropping it would hand a captain the same file on the same man twice.
    /// </summary>
    private void RestoreTheRoomsGoneThrough(Vault vault)
    {
        _roomsTurnedOver.Clear();
        foreach (string turned in vault.TurnedOver?.Rooms ?? [])
        {
            if (!string.IsNullOrWhiteSpace(turned))
            {
                _roomsTurnedOver.Add(turned);
            }
        }
    }

    /// <summary>#615 · Seed one landing's live set out of the durable register. Called at the one place an
    /// excursion begins, so no floor of the building can be drawn before the register has had its say.</summary>
    private void SeedTurnedOverRooms(SurfaceExcursion ex)
    {
        foreach (string key in _roomsTurnedOver)
        {
            // Core reads its own key. A transcription of the format here would be a second reader that
            // agrees with the writer until the day one of them is edited — and the failure would be a
            // facility quietly re-filling itself, which is the exact thing this register exists to stop.
            if (KeepOrLeave.TryReadKey(key, ex.Stop.Body.Id, out int level, out int room))
            {
                ex.HiveRoomsEmptied.Add(HiveInterior.RoomKey(level, room));
            }
        }
    }

    /// <summary>#615 · A room is struck off in BOTH registers or in neither. One method, so the live set the
    /// deck reads and the durable set the vault carries can never come to two views of one room — the seam
    /// this repo has paid for four times over.</summary>
    private void TheRoomHasBeenGoneThrough(SurfaceExcursion ex, int level, int roomIndex)
    {
        ex.HiveRoomsEmptied.Add(HiveInterior.RoomKey(level, roomIndex));
        _roomsTurnedOver.Add(KeepOrLeave.RoomKey(ex.Stop.Body.Id, level, roomIndex));
    }

    // ── #615 · THE DECISION ─────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "should we have like keep / leave option when we find stuff?"
    //
    // The find waits here between the room's sentence and the captain's hand. It is the room and not the
    // object (KeepOrLeave.Pending says why): what would go in the pocket is re-asked of Core the instant KEEP
    // is pressed, because the captain may have opened their satchel in between and made room, and a capacity
    // frozen at find time would be an answer given before the question that changed it.

    /// <summary>#615 · The find in front of the captain, waiting on an answer. Null the rest of the time,
    /// which is nearly always.</summary>
    private KeepOrLeave.Pending? _pendingFind;

    /// <summary>#615 · Is the decision's own card the thing in front of the captain? Asked of the card that
    /// is actually up rather than of a flag beside it — <c>TheKitsCardIsUp</c>'s rule, for its reason: a flag
    /// can outlive the surface it describes and a card cannot outlive itself.</summary>
    private bool TheFindIsWaitingOnAnAnswer => _pendingFind is not null && _viewObject is not null;

    /// <summary>
    /// #615 · <b>OFFER IT.</b> The room has said what is in it; nothing has moved.
    ///
    /// <para>It reuses the <c>ViewObject</c> card — the find/console idiom this ground already raises over a
    /// pallet, a shelf, a dead floor and Kolt's dropped schedule — rather than growing an overlay of its own.
    /// That is not only economy: the card is the one surface in the game that already obeys the closing law
    /// (<c>OverlayShell</c>, ✕ and Esc alike), and closing it is the SAME ANSWER as LEAVE. Nothing is taken,
    /// the room is not struck off, and the find is exactly where it was — so there is no state a captain can
    /// be trapped in by walking away, which is what makes the two-verb card honest rather than modal.</para>
    ///
    /// <para>The card's story is the ROOM's own line, unchanged. The decision adds two words to this game and
    /// they are both on buttons; a card that argued for one answer would be the automatic pickup back again,
    /// one layer up and wearing prose.</para>
    /// </summary>
    private void OfferKeepOrLeave(KeepOrLeave.Pending pending, string label, string? artUrl)
    {
        _pendingFind = pending;
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            label, artUrl, pending.RoomLine);
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>
    /// #615 · <b>KEEP.</b> The pickup the search verb stopped half way through, finished — and finished
    /// through the one funnel every find in this game goes through
    /// (<c>UndergroundComplex.WhatGoesInThePocket</c>), asked fresh against the sleeve as it is NOW.
    ///
    /// <para><b>A full sleeve opens the sleeve.</b> Core's refusal (<c>PocketFullLine</c>) has always said
    /// the find is still lying there and always been true; what it could not do was hand the captain the one
    /// control that resolves it. So this lands on the compartments (#691) with that sentence written on the
    /// page, and the find STAYS PENDING behind it — leave a manifest, shut the pockets, press Keep again. It
    /// is never a silent drop and never a silent refusal, which is #678's founding sin in both directions.
    /// </para>
    /// </summary>
    private void KeepTheFind()
    {
        if (_pendingFind is not { } find || _surface is not { } ex)
        {
            return;
        }

        UndergroundComplex.Pickup pick = UndergroundComplex.WhatGoesInThePocket(
            find.Haul, find.BodyId, find.Minted, find.FindId, _satchel);

        if (!pick.RoomEmptied)
        {
            // The pockets, and the reason they are open, on the page they are open on. The card stays up
            // underneath with both verbs still on it: this is a pause in the decision, not an answer to it.
            OpenSatchel();
            _satchelOutcome = UndergroundComplex.PocketFullLine.Trim();
            StateHasChanged();
            return;
        }

        // #768 · The card comes down through the ONE door every road out of it uses, which also frees the
        // sayings it was standing on — a hand-cleared _viewObject is that issue's shipped bug wearing a new
        // hat, and its guard sweeps this file. `find` is already in hand, and closing clears the pending one.
        CloseViewObject();
        TheFindGoesInThePocket(ex, find, pick);
    }

    /// <summary>
    /// #615 · <b>LEAVE.</b> Nothing moves, and that is the entire implementation.
    ///
    /// <para>The room is not struck off, so it keeps its console and its find, and searching it again offers
    /// the same two verbs. The register that rides the vault is the register of rooms GONE THROUGH, so this
    /// room is not in it — fly away, come back a month later, and the paper is where it was. That is #615's
    /// second bullet answered by the world rather than by a store: what the captain declined never left the
    /// room, so nothing has to remember carrying it.</para>
    ///
    /// <para><b>Why not <see cref="LeftBehind"/>.</b> That store holds what a captain picked up and then set
    /// down, and every sentence it prints says <i>"where you left it"</i> / <i>"take back what you left"</i> —
    /// wording that is simply false about a sheet that was never in anybody's hand, and false in the one
    /// register this issue is about. Its own docblock also records the v1 ruling that it is excursion-scoped
    /// deliberately. Writing a declined find into it would have bought a marker at the price of a lie and a
    /// duplicate: the room would still hold the find as well.</para>
    ///
    /// <para>FABLE: line needed — what the captain is told at the moment they look at a find and decide to
    /// walk away from it. Nothing in the store's prose covers a thing that was never picked up, and inventing
    /// one here is exactly what this lane is not allowed to do. Until it exists the card simply closes and
    /// the room console is still standing there, which is the building's own signage saying the true thing.</para>
    /// </summary>
    private void LeaveTheFind()
    {
        // …through the same one door the ✕ uses, for #768's reason and for this feature's: closing IS
        // leaving, so the button and the corner cannot mean two different things.
        CloseViewObject();
        StateHasChanged();
    }

    /// <summary>
    /// #615 · <b>THE PICKUP ITSELF</b> — everything that used to run on the frame a room was searched, now
    /// run on the frame the captain says KEEP.
    ///
    /// <para>A pure MOVE out of <c>SearchThisRoom</c> and deliberately nothing else: the strike-off, the
    /// lead, the credits, the casebook, the shock, the one add, the relic's card and the authority's card,
    /// in the order that method spent five issues getting right (#678's ordering, #684's spent lead, #677's
    /// one-register rule). The only line that changed is the strike-off, which now writes through to the
    /// durable register as well as to the excursion's set — see <see cref="TheRoomHasBeenGoneThrough"/>.</para>
    ///
    /// <para>It is reached two ways and they are the same way: a find that IS a decision arrives here when
    /// the captain presses Keep, and a find that is not one (a crate, an empty room, the key) arrives here
    /// straight off the search. One body, so the two paths cannot drift into two different pickups.</para>
    /// </summary>
    private void TheFindGoesInThePocket(
        SurfaceExcursion ex, KeepOrLeave.Pending find, UndergroundComplex.Pickup pick)
    {
        TheRoomHasBeenGoneThrough(ex, find.Level, find.RoomIndex);

        // #684 · NOW the lead is spent — the room has been turned over for good and whatever it held is in
        // the pocket. Said BEFORE the room's own line for the reason everything in this method is ordered:
        // the pulse keeps one slot and the last write wins, and the haul is the sentence worth surviving.
        if (find.FarLead is { } lead)
        {
            AnnounceLabLead(lead);
        }

        if (find.Haul == UndergroundComplex.Haul.Equipment)
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
        if (UndergroundComplex.CasebookGistOf(find.Haul, find.BodyId, find.Level) is { } hallGist)
        {
            ShowPulseMessage(find.RoomLine + pick.Line);
            if (!ex.HiveHallRecordShown)
            {
                FileNote(hallGist, "\u2b55");
            }
        }
        else
        {
            // #1074 beat 3 - a cost-centre line item is filed UNDER TWO NAMES rather than as a loose entry:
            // the office that is paying, and the ground it is paying for. Core answers what the find is
            // about (CaseSubjects' law - a subject comes from the AUTHOR of the sentence, never from a
            // reader of it), and every other room in the building answers the empty line, which is what
            // FileNoteAbout has always meant by a note that names nothing.
            //
            // It is filed HERE and nowhere else, which #615 is what made true: every find a captain can
            // decide about arrives at this one body whichever way it got here, so a line item cannot be
            // filed one way when it was kept and another when it simply fell into the pocket.
            ShowAndFileAbout(
                find.RoomLine + pick.Line,
                find.Haul == UndergroundComplex.Haul.Dirt ? "\ud83d\uddc3" : "\ud83d\udd26",
                UndergroundComplex.MoneyTrailSubjectsFor(
                    find.BodyId, find.Level, find.RoomIndex, ex.Site.Name));
        }

        if (find.Haul == UndergroundComplex.Haul.Dirt)
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

        if (find.Haul == UndergroundComplex.Haul.Relic)
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
            CarriedObject.Reveal shown = CarriedObject.RelicReveal(find.FindId);
            bool recurs = UndergroundComplex.IsHallRecord(find.FindId);
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
        else if (find.Haul == UndergroundComplex.Haul.Dirt)
        {
            // A file still names a moon on its own — it is about a PERSON and the person is somewhere. Only
            // the operational paper became a thing you have to decide about.
            GrantLabLead(DiceRule.Seed($"lead:hive:{find.BodyId}:{find.Level}:{find.RoomIndex}"));
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
        if (find.Minted is { } card && pick.Take is not null && !ex.HiveAuthorityShown)
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
}
