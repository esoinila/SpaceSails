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
