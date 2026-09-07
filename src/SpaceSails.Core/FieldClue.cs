using System;

namespace SpaceSails.Core;

/// <summary>
/// #603 · HOW MUCH A PIECE OF PAPER ACTUALLY TELLS YOU.
///
/// <para>Owner: <i>"we could have like stronger and stronger clues... most strong would put a dot about it
/// on the motion detector"</i>.</para>
///
/// <para>The tracker already speaks both ends of this language and has since #573: a <b>rumour</b> is a wide
/// soft wash, deliberately painted as an AREA because a dot would claim a precision the information does not
/// have; a <b>beacon</b> is a ring at a place, because a place is a fact. What was missing is everything in
/// between — and the owner's ladder is exactly that, with the strongest clue finally earning the dot.</para>
///
/// <para>This is why a clue is worth carrying rather than being granted on pickup (#603): a paper is not
/// "a lead", it is a lead OF SOME QUALITY, and knowing which is the difference between walking a quarter of
/// a field and walking to a spot.</para>
/// </summary>
public static class FieldClue
{
    /// <summary>How well a document pins a place.</summary>
    public enum Certainty
    {
        /// <summary>A mention. Somewhere on this moon, and that is genuinely all it says.</summary>
        Vague,

        /// <summary>A description — a landform, a bearing, a distance from something. It narrows the ground
        /// to a part of the field rather than to a spot.</summary>
        Narrow,

        /// <summary>Coordinates, or a plan with a mark on it. <b>This is the one that earns a dot.</b></summary>
        Exact,
    }

    /// <summary>#603 · Weighted so the good ones stay rare. A field full of exact clues is a field with no
    /// searching left in it — and the search is the game (#585's whole hunt). Roughly half are a mention,
    /// a third narrow it, and one in six is the one you tell people about.</summary>
    public static Certainty CertaintyOf(string paperId)
    {
        ArgumentNullException.ThrowIfNull(paperId);
        return DiceRule.Roll(DiceRule.Seed($"clue:certainty:{paperId}"), 6).Face switch
        {
            1 or 2 or 3 => Certainty.Vague,
            4 or 5 => Certainty.Narrow,
            _ => Certainty.Exact,
        };
    }

    /// <summary>How wide the tracker paints it, in deck units. Zero means a DOT — the mark is the place, not
    /// a region around it.
    ///
    /// <para>The vague end is deliberately enormous. A wash you can cross in twenty paces is a dot wearing a
    /// disguise, and it would quietly hand back the search it was only meant to narrow.</para></summary>
    public static double SpreadFor(Certainty certainty) => certainty switch
    {
        Certainty.Vague => 90.0,
        Certainty.Narrow => 34.0,
        _ => 0.0,
    };

    /// <summary>Whether this clue paints a point rather than a region.</summary>
    public static bool IsExact(Certainty certainty) => SpreadFor(certainty) <= 0.0;

    /// <summary>What the captain understands when they read it as a lead. Never says the mechanic — it says
    /// what the DOCUMENT is, and the instrument shows the rest.</summary>
    public static string Line(Certainty certainty) => certainty switch
    {
        Certainty.Vague =>
            "📡 It names the moon and nothing finer — a place mentioned the way you mention a place you have " +
            "never had to find. The fan takes it and washes half the ground with it, which is honestly all " +
            "the paper is worth.",

        Certainty.Narrow =>
            "📡 Better than a name: a landform, a bearing off it, a distance somebody paced rather than " +
            "measured. The wash pulls in to a part of the field. You could walk that in an afternoon and " +
            "have air left.",

        _ =>
            "📡 Not a description — a POSITION. Somebody wrote it down to the figure because somebody else " +
            "was going to have to drive there in the dark, and the fan puts a mark on it. Not a wash. A mark.",
    };

    /// <summary>The short word the satchel puts on the item itself, so a captain can see what they are
    /// carrying without offering it to anything.</summary>
    public static string Label(Certainty certainty) => certainty switch
    {
        Certainty.Vague => "a mention",
        Certainty.Narrow => "a description",
        _ => "a position",
    };

    /// <summary>#603 · WHAT IS ACTUALLY ON THE PAGE. Owner: <i>"if we open inventory and view the paper from
    /// there then?"</i>
    ///
    /// <para>Reading a document and DECIDING it is a lead are two different acts, and the satchel was
    /// conflating them — one click both read the paper and spent it. Now looking is free and always
    /// available, and the decision is a second, deliberate press.</para>
    ///
    /// <para>The text describes the paper. It never says what to do with it, and it never names the moon —
    /// working that out is the act the player is paying for.</para></summary>
    public static string Document(string paperId)
    {
        ArgumentNullException.ThrowIfNull(paperId);

        // #1061 · THE ONE SHEET IN THIS GAME THAT WAS WRITTEN RATHER THAN SEEDED, and it is here rather than
        // in a second reader for the reason UndergroundComplex.IsHallRecord is inside RelicReveal: everything
        // that reads a paper — the sleeve's row, the free glance, the dig at a table, the gist filed when it
        // is put down — goes through this function and Title below. A second composition for one authored
        // sheet would be a document that read one way in the pocket and another on the card.
        //
        // It returns the body ALONE. The seeded tail below ("there is a place named on it…") is the paper
        // telling you how well it pins a place, and a rate schedule's answer to that is the whole of what it
        // already says: it prices the ground and never names what happens on it.
        if (HardcaseRep.IsTheSchedule(paperId))
        {
            return HardcaseRep.ScheduleBody;
        }

        // #1074/#1063 · …AND THE FIVE THE ARC WROTE, for exactly the same reason and through exactly the
        // same door. A paper out of one of the underground's five designated rooms is a paper somebody
        // composed a sentence for, and until this branch a captain who carried the rail's invoice out and
        // opened it in the sleeve was shown a torn shipping manifest.
        //
        // Body ALONE again, and see PaperHeads.DocumentOf for why: the seeded tail below is a generic paper
        // saying how well it pins a place, and these five pin nothing — they are the paperwork of one job on
        // the ground the captain is already standing on. THE CERTAINTY IS NOT BRANCHED. It still rolls off
        // the id exactly as it always did, so the tracker's spread, the row's short word and Line() are all
        // untouched: what changed is what the sheet SAYS, not what it is worth.
        if (UndergroundComplex.AuthoredPaperOf(paperId) is { } written)
        {
            return PaperHeads.DocumentOf(written);
        }

        // #602 · …and the code paper, the sixth head, through the same door as the five above. Until #602's
        // canon pass of 2026-09-05 this arm returned the sheet's own handwriting, digits and all; the pass
        // wrote it a head of its own instead, and a head DESCRIBES the page rather than transcribing it —
        // see LiftCode.PaperDocument for why the four digits stay on the works floor.
        //
        // Body ALONE, again, and the certainty is not branched: it still rolls off the id, so the tracker's
        // spread, the row's short word and Line() all say what they always said.
        if (UndergroundComplex.LiftCode.PaperIn(paperId) is not null)
        {
            return UndergroundComplex.LiftCode.PaperDocument;
        }

        string[] papers =
        [
            "A duplicate movement order, carbon third copy, the top two long gone. Somebody has ticked a " +
            "column of dates in pencil and initialled the bottom without dating it themselves.",

            "A supply requisition for a place named only by a code, countersigned by an office that signed " +
            "for a great many things that year. The quantities are for a facility, not a survey.",

            "Half a shipping manifest, torn on the fold. What is left is the ROUTE — where it left, where " +
            "it called, and the last line, which is a place and not a port.",

            "A maintenance log kept in one hand for years and then in another. The second hand records " +
            "fewer visits and stops recording them entirely, and does not say why.",

            "An inspection schedule with a site list down the margin. Most have a date beside them. One has " +
            "a date, a line through it, and a different date.",

            "A pay sheet. Names, grades, a column for the site allowance, and a footnote about the rate for " +
            "somewhere the sheet does not otherwise mention.",
        ];

        string body = papers[WhichPaper(paperId, papers.Length)];

        Certainty certainty = CertaintyOf(paperId);
        string worth = certainty switch
        {
            Certainty.Vague =>
                "\n\nThere is a place named on it. Only named — the way you name somewhere you have never " +
                "had to find.",
            Certainty.Narrow =>
                "\n\nAnd there is a description: a landform, a bearing taken off it, a distance somebody " +
                "paced rather than measured. Enough to walk to in an afternoon.",
            _ =>
                "\n\nAnd there is a POSITION, written to the figure. Somebody wrote it down like that " +
                "because somebody else was going to have to drive there in the dark.",
        };

        return body + worth;
    }

    /// <summary>Which of the papers this id is. ONE roll, shared by the title and the page — so the cover
    /// sheet in the satchel is the cover sheet of the document you will actually read. Rolling twice would
    /// give a captain a pay sheet in their pocket that opens as a shipping manifest.</summary>
    private static int WhichPaper(string paperId, int count) =>
        (int)(DiceRule.Seed($"clue:paper:{paperId}") % (ulong)count);

    /// <summary>#613 · WHAT THE PAPER IS CALLED, in a pocket. Owner, holding several: <i>"the operational
    /// papers could have individual short titles… now they look identical in inventory."</i>
    ///
    /// <para>He is right, and it was worse than cosmetic. A satchel of six lines all reading "operational
    /// paper" is a satchel you cannot reason about: you cannot tell which one you have already read, which
    /// one came from the floor with the sealed way, or whether picking up a seventh got you anything new. An
    /// inventory whose entries are indistinguishable is not an inventory, it is a counter.</para>
    ///
    /// <para>The titles are what the paperwork calls ITSELF — dry, filed, and about procedure. None of them
    /// says lead, clue, site or facility, because the whole point of the ladder is that the captain decides a
    /// document is worth something. A form that announced its own importance would be doing that for
    /// them.</para></summary>
    public static string Title(string paperId)
    {
        ArgumentNullException.ThrowIfNull(paperId);

        // #1061 · …and the authored sheet is called what it is called. See Document above for why the branch
        // is here and not in a reader of its own.
        if (HardcaseRep.IsTheSchedule(paperId))
        {
            return HardcaseRep.ScheduleLabel;
        }

        // #1074/#1063 · …and the five the arc wrote are called what they are called. Same door as Document
        // above, and the same one roll behind it: the row still ends with the seeded short word, so a
        // pocketful of papers is still comparable on the one thing worth comparing them on.
        if (UndergroundComplex.AuthoredPaperOf(paperId) is { } written)
        {
            return PaperHeads.TitleOf(written);
        }

        // #602 · …and the code paper is called what the canon pass named it, WITHOUT the digits — it used to
        // wear its own first clause sliced off the sentence, which is a title borrowed rather than written.
        // A row that shouted the number would put the answer in the inventory, where the captain never had
        // to open anything to get it.
        if (UndergroundComplex.LiftCode.PaperIn(paperId) is not null)
        {
            return UndergroundComplex.LiftCode.PaperTitle;
        }

        string[] titles =
        [
            "movement order, third copy",
            "supply requisition, countersigned",
            "shipping manifest, torn",
            "maintenance log, two hands",
            "inspection schedule, margin list",
            "pay sheet, allowances",
        ];

        return titles[WhichPaper(paperId, titles.Length)];
    }
}
