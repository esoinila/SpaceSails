using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: #1202 slice 4 · CHARGED TO PRESERVATION — the page's half. Core owns the words, which outcomes pay
// with paper and which one the rag notices (SpikeIt.Preservation); this file is the two moments Core cannot
// reach: the receipt going into the satchel with the desk's 💳, and the rag's line on the wire the cycle after
// a SPIKED window. No page field: the paper rides the satchel and the rag rides slice 1's Floored key on the
// contract line.
public sealed partial class Map
{
    /// <summary>
    /// #1202 slice 4 · <b>THE OFFICE PAYS LIKE AN OFFICE.</b> Called by <see cref="TheSpikeIsSettled"/> on the one
    /// press that pays, so it lands once per contract with the 💳 and never says a word: on a SPIKED or ALTERED
    /// payout a line item goes into the satchel, and the book files its document under <i>the Authority</i> and
    /// the story's body — #1074 beat 3's own door. A LATE window pays nothing and itemises nothing. A sleeve too
    /// full to take it is a state the canon wrote no line for: the paper simply does not land.
    /// </summary>
    private void TheOfficePaysWithPaper(Quest q, SpikeIt.Outcome outcome)
    {
        if (!SpikeIt.ReceiptLands(outcome) || q.DestBodyId is not { } bodyId)
        {
            return;
        }

        string body = BodyName(bodyId);
        Core.Satchel.Item receipt = SpikeIt.TheReceipt(body);
        if (!Core.Satchel.CanTake(_satchel, receipt))
        {
            return;
        }

        _satchel = [.. Core.Satchel.Add(_satchel, receipt)];
        FileNoteAbout(SpikeIt.ReceiptDocument, SpikeIt.ReceiptGlyph, SpikeIt.ReceiptSubjects(body));
        RequestVaultSave();
    }

    /// <summary>
    /// #1202 slice 4 · <b>THE RAG NOTICES THE HOLE</b> — the cycle after a SPIKED window (the floor's own clock),
    /// on a port's rag only (the floor-reaction kind's own masthead rule), once: the line is fixed, so the wire's
    /// one-dated-line rule keeps a second contract from printing it twice. Pushed events are not saved, so this is
    /// asked on every advance like the rest of her wire; the contract's <c>Floored</c> key remembers it ran. It
    /// names no body and no byline, so a ✂ CLIP of it files under nothing.
    /// </summary>
    private CarryThePress.Passage TheRagNoticesTheHole(in CarryThePress.Passage p, CarryThePress.Passage next)
    {
        if (!SpikeIt.TheRagNoticesTheHole(next.Outcome) || !CarryThePress.FloorIsDue(p, SimTime))
        {
            return next;
        }

        OnTheWire(NewsWire.NewsEventKind.PressFloorReaction, CarryThePress.FloorAt(p)!.Value,
            SpikeIt.BylineMissingLine, "");
        return next with { Floored = true };
    }
}
