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
}
