using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #741 · THE RED PEN, in the hand — the client half of the case threads.
///
/// <para>Owner, authorising the build: <i>"I dream of drawing those conspiracy board connecting red
/// lines… I guess it could be a red pen only used to connect the things."</i> And the gesture, exactly:
/// <i>"select one, select the other — connected"</i>, after which <i>"the ORDER of items updates so
/// connected items become adjacent."</i></para>
///
/// <h3>What this file decides, and what it does not</h3>
///
/// <para>It decides nothing about the case. Which entries are threaded, how the page is ordered, which rows
/// carry a red edge, what a title says and whether the pen may come out at all are every one of them
/// <see cref="Core.CaseThreads"/> answers — this file holds three pieces of hand state (is the pen out,
/// which title is being held, which nodes are open) and routes presses into Core.</para>
///
/// <para>THE PEN IS AN INSTRUMENT, NOT A MODE, in the fiction: picking it up IS entering connect-thinking,
/// so one control switches what pressing a title MEANS — read it, or draw from it. That is why there is no
/// second row of little connect buttons beside the titles: the notebook would stop being a notebook.</para>
///
/// <para>THE ACKNOWLEDGMENT IS QUIET. One soft cue, on the press that actually made a line — never on a pen
/// re-drawn over one that was already there, because <see cref="Core.CaseThreads.Draw"/> says which. The
/// register is the owner's own: the whisky glass set down, not a slot machine. Nothing congratulates,
/// because nothing here knows whether the line is right.</para>
/// </summary>
public partial class Map
{
    /// <summary>#741 · The red lines the captain has drawn, in the order they were drawn. Durable — a case
    /// assembled over a month of excursions that did not survive a reload would not be a case.</summary>
    private List<Core.CaseThreads.Thread> _caseThreads = [];

    /// <summary>#741 · Is the pen out? One flag, because one pen.</summary>
    private bool _penInHand;

    /// <summary>#741 · The handle of the title already picked up, waiting for its other end. Null between
    /// gestures, and cleared by everything that ends the gesture — closing the pocket, putting the pen away,
    /// changing which page of the book is open.</summary>
    private string? _penHoldingId;

    /// <summary>#741 · Which nodes are OPEN. Handles rather than indices, so a node stays open across the
    /// reorder that a drawn line causes — the row moves and the reading does not close under the captain's
    /// eye.</summary>
    private readonly HashSet<string> _notesExpanded = [];

    /// <summary>#741 · Why the pen may not come out here, or null when it may. Core's one predicate, asked
    /// with the two facts this component owns — and it is <see cref="SeatedSpread"/>'s own ladder rather
    /// than a second one, because drawing a line between two entries is the same deliberate table work as
    /// laying the papers out.</summary>
    private string? PenRefusal => Core.CaseThreads.PenRefusal(SeatedIn, SeatedAlone);

    /// <summary>#741 · Take it out, or put it away. Putting it away drops whatever end was being held: a
    /// half-drawn gesture is not a state worth keeping, and a pen in the satchel is not holding a title.
    ///
    /// <para>The refusal is SAID (#603/#680) rather than the control being hidden or greyed, because WHERE
    /// the case may be worked is a law a player learns by pressing this once at a bar stool.</para></summary>
    private void TakeUpTheRedPen()
    {
        if (_penInHand)
        {
            PutTheRedPenAway();
            return;
        }

        if (PenRefusal is { Length: > 0 } no)
        {
            _satchelOutcome = no;
            return;
        }

        _penInHand = true;
        _satchelOutcome = Core.CaseThreads.PenInHandHint;
    }

    private void PutTheRedPenAway()
    {
        _penInHand = false;
        _penHoldingId = null;
        _satchelOutcome = null;
    }

    /// <summary>
    /// #741 · A TITLE WAS PRESSED. What that means is what is in the hand.
    ///
    /// <para>Pen down, it is a node: open it, or fold it again. Pen up, it is one end of a line — the first
    /// press picks a title up, the second draws to it, and pressing the one already held is a change of mind
    /// rather than a line from a note to itself (Core refuses that outright).</para>
    ///
    /// <para>Two titles that are ALREADY threaded erase instead. The eraser end is the same gesture
    /// reversed, on purpose: a captain who has just learned to connect already knows how to undo it, and it
    /// is stated in the pen's own hint because a reversible act nobody knows is reversible is not one.</para>
    /// </summary>
    private void NoteTitlePressed(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        if (!_penInHand)
        {
            if (!_notesExpanded.Remove(id))
            {
                _notesExpanded.Add(id);
            }
            return;
        }

        // The pen came out at a seat that allowed it and the room can change under you — somebody takes the
        // chair opposite and the papers go back in the sleeve. Asked again at the moment of the mark, so the
        // gate cannot be walked round by having picked the pen up a minute earlier.
        if (PenRefusal is { Length: > 0 } no)
        {
            _satchelOutcome = no;
            PutTheRedPenAway();
            return;
        }

        if (_penHoldingId is not { Length: > 0 } holding)
        {
            _penHoldingId = id;
            _satchelOutcome = Core.CaseThreads.HoldingOneLine;
            return;
        }

        if (string.Equals(holding, id, System.StringComparison.Ordinal))
        {
            _penHoldingId = null;                                  // Put that end down again.
            _satchelOutcome = Core.CaseThreads.PenInHandHint;
            return;
        }

        if (Core.CaseThreads.AreThreaded(_caseThreads, holding, id))
        {
            _caseThreads = [.. Core.CaseThreads.Erase(_caseThreads, holding, id)];
            _penHoldingId = null;
            _satchelOutcome = Core.CaseThreads.PenInHandHint;
            RequestVaultSave();
            return;
        }

        _caseThreads = [.. Core.CaseThreads.Draw(_caseThreads, holding, id, out bool drawn)];
        _penHoldingId = null;
        _satchelOutcome = Core.CaseThreads.PenInHandHint;

        if (drawn)
        {
            // THE QUIET ACKNOWLEDGMENT, and it is one soft cue and the line settling into place — nothing
            // else. "rum" is the warm, low chime the toast and the short rest already use; "board" is the
            // prize jingle a found card gets and would be exactly the slot-machine register the owner ruled
            // out. Keyed on Core's own `drawn`, so a pen dragged back over an existing line makes no claim
            // and no sound.
            RendererInterop.PlayCue("rum");
            RequestVaultSave();
        }
    }

    /// <summary>#741 · Turn to another reading of the book. It puts the held end DOWN: the gesture is a
    /// sentence about two titles on one page, and half of it, said on a page that no longer shows the other
    /// title, is not a sentence. The pen itself stays out — turning a page is not putting your pen away.</summary>
    private void TheNotebookTurnsTo(NotesView view)
    {
        _notesView = view;
        _penHoldingId = null;
    }

    /// <summary>#741 · THE PAGE, as the notebook draws it — Core's own rows for whichever list of entries
    /// the open filter is showing. The client renders these and decides nothing about their order.</summary>
    private IReadOnlyList<Core.CaseThreads.Row> NotebookPage(IReadOnlyList<Core.FieldNote> notes) =>
        Core.CaseThreads.Page(notes, _caseThreads);

    /// <summary>#741 · The whole book, newest first — the case view's own list. <see cref="_fieldNotes"/> is
    /// kept oldest-first (the book appends), and every reading surface in the game shows it the other way
    /// up; reversing here rather than re-deriving a grouping keeps that one convention.</summary>
    private IReadOnlyList<Core.FieldNote> TheWholeCase() => [.. Enumerable.Reverse(_fieldNotes)];

    /// <summary>#741 · Is this row's node open? Handles, not indices — see <see cref="_notesExpanded"/>.</summary>
    private bool NodeIsOpen(string id) => _notesExpanded.Contains(id);

    /// <summary>#741 · Is this the title being held, waiting for its other end?</summary>
    private bool NodeIsHeld(string id) =>
        _penHoldingId is { Length: > 0 } held && string.Equals(held, id, System.StringComparison.Ordinal);

    // ── #741 v1 · THE THREADS ───────────────────────────────────────────────────────────────────────────
    //
    // The issue's own v1, and it is a READING of the book rather than a second store: "a third tab —
    // THREADS — grouping existing entries by the entities they already name… the THREAD is just the stack
    // of what you wrote about one name, and the captain draws the line."
    //
    // Nothing here decides what a subject is. Core's authors declared them at writing time (CaseSubjects),
    // Core groups them, Core writes the heading and the count word. This file turns a page and routes a
    // press, exactly as it does for the pen.

    /// <summary>#741 v1 · The stacks, off the one book. Asked per draw rather than cached, like every other
    /// projection in the notebook — a cached case is a second store, and #587's whole law is that there is
    /// one book.</summary>
    private IReadOnlyList<Core.CaseSubjects.SubjectThread> TheBookThreads() =>
        Core.CaseSubjects.ThreadsOf(_fieldNotes);

    /// <summary>#741 v1 · Turn to another pocket of the satchel, putting whatever end the pen was holding
    /// DOWN — the same reason <see cref="TheNotebookTurnsTo"/> does it: half a gesture, said on a page that
    /// no longer shows the other title, is not a sentence. The pen itself stays out.</summary>
    private void TheSatchelTurnsTo(SatchelPage page)
    {
        _satchelPage = page;
        _penHoldingId = null;
    }

    /// <summary>
    /// #741 v1 · RUN THE PEN DOWN A STACK. One press, one line from each entry to the next, oldest to
    /// newest — the same <see cref="Core.CaseThreads.Draw"/> the two-press gesture uses, called along the
    /// stack the captain is looking at.
    ///
    /// <para>It saves the wrist and NOT the judgement: the captain chose this heading, the lines it draws
    /// are ordinary lines, and every one of them comes off again with the ordinary two presses. Nothing is
    /// drawn automatically — a stack sitting on the page is never connected until a hand asks for it, which
    /// is the north star this whole feature is fenced by.</para>
    ///
    /// <para>The pen's own ladder is asked again at the moment of the mark, exactly as
    /// <see cref="NoteTitlePressed"/> does, so a heading cannot walk round the gate a title obeys.</para>
    /// </summary>
    private void RunThePenDownTheStack(Core.CaseSubjects.SubjectThread stack)
    {
        if (PenRefusal is { Length: > 0 } no)
        {
            _satchelOutcome = no;
            PutTheRedPenAway();
            return;
        }

        bool anyDrawn = false;
        for (int i = 1; i < stack.Entries.Count; i++)
        {
            _caseThreads =
            [
                .. Core.CaseThreads.Draw(
                    _caseThreads,
                    Core.CaseThreads.IdentityOf(stack.Entries[i - 1]),
                    Core.CaseThreads.IdentityOf(stack.Entries[i]),
                    out bool drawn),
            ];
            anyDrawn |= drawn;
        }

        _penHoldingId = null;
        _satchelOutcome = Core.CaseThreads.PenInHandHint;

        if (anyDrawn)
        {
            // The same quiet cue one line earns, once for the run — not once per pair, which would be the
            // slot machine the owner ruled out arriving by arithmetic.
            RendererInterop.PlayCue("rum");
            RequestVaultSave();
        }
    }

    /// <summary>
    /// #741 v1 · "second entry about OFFICE OF WORKS" — ON THE CARD IN FRONT OF YOU, ONCE.
    ///
    /// <para>#736's law, and the reason this is not a banner: a line about a card's own contents belongs on
    /// the card, in its outcome region, where a backdrop cannot blur it and where the captain is already
    /// looking. A HUD banner would play under the dossier's own backdrop and be gone in eight seconds,
    /// which is the exact bug (#587/#774) this notebook exists because of.</para>
    ///
    /// <para>With no card raised nothing is said at all. The badge is a NOTICING, and there is no beat to
    /// notice in the middle of a corridor — the durable answer, always, is the THREADS page, which is
    /// standing there whether or not anybody was told.</para>
    ///
    /// <para>Once: never appended twice to one card, so a kit that files four sentences under one raised
    /// dossier cannot stack four copies of the same line.</para>
    /// </summary>
    private void TheThreadBadgeGoesOnTheCard(in Core.FieldNote note)
    {
        if (_viewObject is not { } card
            || Core.CaseSubjects.NewThreadBadge(note, _fieldNotes) is not { Length: > 0 } badge)
        {
            return;
        }

        string already = card.Outcome ?? "";
        if (already.Contains(badge, System.StringComparison.Ordinal))
        {
            return;
        }

        _viewObject = card with
        {
            Outcome = already.Length > 0 ? $"{already}\n\n🧵 {badge}" : $"🧵 {badge}",
        };
    }
}
