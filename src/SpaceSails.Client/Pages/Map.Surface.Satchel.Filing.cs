using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · SAYING IT AND FILING IT (#684, #768) — the authority ids, show-and-file, the sayings held behind a
/// card and released after it, and the one funnel every note in the book goes through.
///
/// <para>Split out of <c>Map.Surface.Satchel.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public partial class Map
{
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
    /// gets it now; the screen gets it when the card closes. Pair every caller with
    /// <see cref="ReleaseHeldSayingsUnlessACardStopsTheWorld"/> once the event's cards have been raised.
    ///
    /// <para>#1230 · the hold is a QUEUE, so an arrival's whole breath is now SAID rather than sorted down to
    /// one survivor — in this frame's own rank order, highest first, one line at a time. The clock handed in
    /// is what makes "this frame's own" a thing the queue can ask (<see cref="PulseHold.Waiting"/>).</para>
    /// </summary>
    private void HoldAndFile(string text, string glyph, PulseRank rank = PulseRank.Status)
    {
        _held = _held.Hold(text, rank, _lastTimestampMs ?? 0);
        FileNote(text, glyph);
    }

    /// <summary>#768 · The same, for a line the book does not keep — a warning about the here and now rather
    /// than a durable find.</summary>
    private void HoldSaying(string text, PulseRank rank = PulseRank.Status) =>
        _held = _held.Hold(text, rank, _lastTimestampMs ?? 0);

    /// <summary>#768 · Is something in front of the captain that a pulse would play UNDER? Asked of the world
    /// as it now stands rather than predicted from the conditions that raise them — a copy of those
    /// conditions is a second rule to keep in step, and this one cannot be wrong.
    ///
    /// <para>#1214 · <b>AND IT IS THE WHOLE CENSUS NOW, NOT TWO OF THIRTY-ONE.</b> This read
    /// <c>_viewObject is not null || _storyCard is not null</c> — the two cards #768's own events raise — and
    /// its docblock already said what was wrong with that: <i>a copy of those conditions is a second rule to
    /// keep in step</i>. It was one, and it had fallen thirty cards behind. The bug that found it: the tail's
    /// chair reading (#1062) pulsed under the FINDER's card (#417), which is <c>_finderCard</c> and was on
    /// neither side of that <c>||</c> — <i>"From this chair you can see the door…"</i>, spent, drawn at
    /// (400, 96), and invisible beneath a z-1320 backdrop.</para>
    ///
    /// <para>#1052 already built the honest answer and proved it cannot go stale: <see cref="AScrimIsUp"/>
    /// runs the <c>TheScrimCensus</c>, and <c>OnlyOneScrimAtATimeTests</c> walks <c>Map.razor</c> itself and
    /// requires the census to name every <c>.view-object-backdrop</c> gate in it. So a card typed tomorrow
    /// joins this law on the day it is typed, and the mirror that had gone stale cannot be one again.</para>
    /// </summary>
    private bool ACardStopsTheWorld => AScrimIsUp;

    /// <summary>#768 · The end of an event that had things to say: if nothing is in front of the captain the
    /// first held line is simply pulsed, here and now, exactly as it always was. If a card IS up, the queue
    /// stays held and <see cref="SayWhatTheScrimWasStandingOn"/> drips it out once the glass clears.
    ///
    /// <para>#1230 · ONE line, because the queue says one at a time — the rest of the event's breath follows
    /// from the frame check as the slot frees up, which is where a queue belongs rather than in a loop
    /// here.</para></summary>
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

    /// <summary>
    /// #1214 · <b>THE FRAME THE GLASS CLEARS.</b> A plot-significant line raised while a scrim was up is held
    /// (<see cref="ShowPulseMessage"/>); this is where it is finally said.
    ///
    /// <para>It is a frame check and not a closer hook, and that is the whole of why it works. #768's release
    /// hangs off <c>CloseViewObject</c> and <c>CloseStoryCard</c> — the two cards whose OWN events do the
    /// holding — and there are thirty-one cards in <c>TheScrimCensus</c>. A line held under the finder's pitch
    /// would have waited for a story card that was never coming. Asking the world once a frame costs nothing
    /// and cannot fall behind: the <c>_held.Any</c> test is one field read, and the census walk happens only
    /// on the handful of frames something is actually waiting — the same argument
    /// <see cref="PumpTheScrimQueue"/> makes for itself, for the same reason.</para>
    ///
    /// <para>Called from the tick beside <c>_pulse.Expire</c>, which is the pulse's own per-frame seam, so the
    /// held line is released into a slot on the same frame that slot is being aged.</para>
    ///
    /// <para>#1230 · <b>and it is what makes the QUEUE drip.</b> One line per frame at most, and the queue
    /// itself refuses to hand over the next until the one before it has had its whole dwell. So a card closed
    /// on three waiting beats says the first at once and the others as the slot comes free, in order, each
    /// for as long as its own length earns.</para>
    /// </summary>
    private void SayWhatTheScrimWasStandingOn()
    {
        if (_held.Any && !ACardStopsTheWorld)
        {
            ReleaseHeldSayings();
        }
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
