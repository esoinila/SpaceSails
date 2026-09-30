using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: #1202 slice 4 · CHARGED TO PRESERVATION — the page's half. Core owns the words, which outcomes pay
// with paper and which one the rag notices (SpikeIt.Preservation); this file is the two moments Core cannot
// reach: the receipt going into the satchel with the desk's 💳, the rag's line on the wire the cycle after a
// SPIKED window, her empty bar seat for the watches she is away, the regular beside it asked about it, and her one
// line on coming back. No page field: the paper rides the satchel, the rag slice 1's Floored key, and the absence
// one watch index (Back) and two flags (Asked, Home) on the contract line.
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

    // ── HER ABSENCE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1202 slice 4 · The contract whose SPIKED window keeps her away from her bar seat on the room's own frozen
    /// watch (<see cref="BarWatch"/>, the watch the deck was welded at), or null. Read by
    /// <see cref="TheStringerIsElsewhere"/>, so her chair is simply empty — no console, no figure, nothing greyed
    /// and nothing said — for exactly the seeded watches, and she is back on her own cadence after.
    /// </summary>
    private Quest? TheSpikeKeepsHerAway()
    {
        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CarryThePress, State: QuestState.TurnedIn }
                && PassageOf(q) is var p && SpikeIt.IsAway(p.Outcome, p.Back, BarWatch))
            {
                return q;
            }
        }

        return null;
    }

    /// <summary>
    /// #1202 slice 4 · <b>A REGULAR, ASKED ABOUT HER EMPTY SEAT.</b> While she is away, on a watch that would have
    /// been hers here (her chair exists and stands empty), the regular sitting nearest it answers the first time
    /// the captain walks up to them — #1074 beat 4's own sentence, verbatim — and that is their one breath about
    /// it this absence (#709's once-per-person law). Every later press is their ordinary table. True when the
    /// press was this.
    /// </summary>
    private bool TheEmptySeatIsAskedAbout(string giver)
    {
        if (_dockedHavenId is not { } here || TheSpikeKeepsHerAway() is not { } q || PassageOf(q).Asked
            || HavenInterior.TheStringersChairAt(here, _dockVisitSimTime) is not { } chair)
        {
            return false;
        }

        var seated = new List<(string Name, double X, double Y)>();
        foreach (HavenInterior.SeatedRegular r in HavenInterior.ResolveRegulars(here, _dockVisitSimTime, TheBarsChurn))
        {
            if (r.State == PatronState.AtBar)
            {
                seated.Add((r.Label.Replace("◈", "").Trim(), r.X, r.Y));
            }
        }

        if (!string.Equals(SpikeIt.WhoIsAsked(seated, chair.X, chair.Y), giver, StringComparison.Ordinal))
        {
            return false;
        }

        SayItWhereTheyAreLooking(SpikeIt.TheRegularsAnswer);
        RewritePassage(q, PassageOf(q) with { Asked = true });
        RequestVaultSave();
        return true;
    }

    /// <summary>
    /// #1202 slice 4 · <b>HER RETURN, ONCE.</b> The first time the captain walks up to her own chair after the
    /// absence, she says the one line — about the pages and the payment, never about him — and the card waits
    /// for the next press. Never before the absence is over, never twice, and never for a window that was not
    /// SPIKED. True when the press was this.
    /// </summary>
    private bool HerReturnIsTold(string giver)
    {
        if (!string.Equals(giver, CarryThePress.Giver, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (Quest q in _quests)
        {
            if (q is { Kind: QuestKind.CarryThePress, State: QuestState.TurnedIn }
                && PassageOf(q) is var p && SpikeIt.HerReturnIsDue(p.Outcome, p.Back, p.Home, BarWatch))
            {
                SayItWhereTheyAreLooking(SpikeIt.ReturnLine);
                RewritePassage(q, p with { Home = true });
                RequestVaultSave();
                return true;
            }
        }

        return false;
    }
}
