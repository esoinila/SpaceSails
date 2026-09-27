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
    public enum SatchelPage
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
    public enum NotesView
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
}
