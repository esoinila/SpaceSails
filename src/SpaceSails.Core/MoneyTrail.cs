using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1074 beat 3 · <b>THE MONEY TRAIL</b> — what the hide costs, written down by the one office in the
/// building that has no opinion about any of it.
///
/// <para>#1074, verbatim: <i>"if nothing is there, why forbid the look — empty + expensive is still a
/// signal."</i> The fence (beat 2), the watch that keeps it, and the concrete that closed the working
/// (beat 1) each sit on somebody's line item, and line items can be clipped (#1052).</para>
///
/// <para><b>THE COST CENTRE IS THE TELL AND IT IS THE ONLY TELL.</b> Read the three items and every one of
/// them is charged to PRESERVATION — a line the budget did not have last year, now carrying a rail, a
/// continuous two-hand rota and three hundred tonnes of remediation, all of it spent on a site that is
/// officially empty. Nothing on any paper is false: the rail was delivered, the watch is kept, the pour was
/// made. A Scully reads an over-funded heritage office and is not being fooled — they are reading three true
/// invoicing lines. <b>And notice what they add up to.</b></para>
///
/// <para><b>WHAT IS DELIBERATELY NOT ON THESE PAPERS, and each absence is a law rather than an omission:</b>
/// no amount in money (a figure would turn an inference into an accusation and hand the reader the sum the
/// beat exists to make them do); no signature (the doctrine's first law — the enforcer is an OFFICE, and
/// <see cref="StopOrder.Stamp"/> is the whole of it); and no department but the cost centre itself, because
/// naming a directorate would mint the villain official #1074 forbids. §8's reserved word appears nowhere,
/// and <see cref="AllProse"/> is swept for it.</para>
///
/// <para><b>THE BOOK KEEPS NO OPINION</b> (#741's law, and #587's before it). Clipping one of these files an
/// ORDINARY entry under two subjects — the office and the ground — and the THREADS view (#934) stacks them
/// under one heading without a word of comment. There is no summary line, no total, no red thread drawn for
/// the player and no badge that says what three items about one cost centre mean. The pen is still the only
/// thing in this game that draws a line and a human hand is still the only thing that moves it.</para>
///
/// <para><b>NO NEW CLUE KIND.</b> #1063 flagged typed clues as optional and they stay optional: a line item
/// is filed as the ordinary clipped paper it is, with <see cref="FieldClue"/>'s ordinary seeded certainty,
/// because the thing that makes it evidence is the SUBJECT it lands under and not a label the game hangs on
/// it. A kind called "a line item" would be the book telling the captain it had noticed something.</para>
///
/// <para><b>CANON.</b> All three items are authored in #1074's canon pass of 2026-09-03 and lifted
/// character for character. Nothing here is composed.</para>
/// </summary>
public static class MoneyTrail
{
    // ── THE THREE LINE ITEMS, VERBATIM ──────────────────────────────────────────────────────────────────
    //
    // One purchase, one line, one cost centre, in the flat invoicing form a works ledger writes everything
    // in: what was bought, how much of it, and who is paying. There is no fourth item and nothing composes
    // a fourth: what a captain reads is one of these three and then the paper stops.

    /// <summary>#1074 · <b>THE RAIL</b> — beat 2's sixteen-gon fence, on the books. Authored, verbatim.
    ///
    /// <para><see cref="PreservationZone.RingSides"/> is sixteen and this line says <i>sixteen sections</i>,
    /// which is the same fence counted by the office that paid for it. That is a coincidence the game never
    /// points out and a reader may or may not walk the ring to check.</para></summary>
    public const string RailLineItem =
        "Perimeter rail, sixteen sections, delivered and fixed. Charged to Preservation.";

    /// <summary>#1074 · <b>THE ROTA</b> — a watch kept on a site whose significance is permanently under
    /// study. Authored, verbatim. <i>Continuous</i> is the word doing the work and no sentence anywhere
    /// remarks on it: a study that never ends is watched by two hands that never stop.</summary>
    public const string RotaLineItem =
        "Site watch, two hands, continuous. Charged to Preservation.";

    /// <summary>#1074 · <b>THE POUR</b> — the structural remediation beat 1's order closed the working
    /// <i>pending</i>. Authored, verbatim. The review had no published schedule; the concrete did not wait
    /// for one.</summary>
    public const string PourLineItem =
        "Structural remediation, lower galleries, three hundred tonnes. Charged to Preservation.";

    /// <summary>Which of the three a paper is. Three named items rather than a list index, because a caller
    /// deciding what to deal on a ground is choosing between three THINGS the office bought and not between
    /// three positions in an array somebody may reorder.</summary>
    public enum Item
    {
        /// <summary>The concrete. Dealt on any ground whose working the Authority has closed.</summary>
        Pour,

        /// <summary>The fence. Dealt only where there is a fence — a site in official care.</summary>
        Rail,

        /// <summary>The watch on that fence. Dealt only where there is a fence.</summary>
        Rota,
    }

    /// <summary>What one item actually says, and the one place any caller may get it from. There is no
    /// default arm: an <see cref="Item"/> nobody has authored a line for would be a paper with an invented
    /// sentence on it, which is the one thing this beat forbids.</summary>
    public static string TextOf(Item item) => item switch
    {
        Item.Rail => RailLineItem,
        Item.Rota => RotaLineItem,
        Item.Pour => PourLineItem,
        _ => throw new ArgumentOutOfRangeException(nameof(item)),
    };

    /// <summary>#1074 · <b>DOES THIS ITEM NEED A FENCE TO EXIST?</b> True for the rail and the watch on it,
    /// which are things a site in official care has and a merely closed working does not; false for the
    /// pour, which is what closed the working in the first place.
    ///
    /// <para>Stated here rather than at the placement, because it is a fact about the PURCHASE — you cannot
    /// invoice a perimeter rail for a site that has no perimeter — and a rule about purchases kept inside a
    /// room-designation function would be a rule only the underground could see.</para></summary>
    public static bool NeedsTheFence(Item item) => item is Item.Rail or Item.Rota;

    // ── WHAT THE BOOK FILES IT UNDER ────────────────────────────────────────────────────────────────────
    //
    // TWO SUBJECTS AND NEVER A THIRD, and both are already printed for the captain to read, which is
    // CaseSubjects' own first law: the office is stamped on the plate at the seal and on the notice at the
    // gate (StopOrder.Stamp), and the site is named on the plate the shuttle sets you down under. Nothing
    // here reads a note's prose and nothing here mints a name the game has not shown him.
    //
    // THE OFFICE IS THE ONE THAT JOINS THEM UP. #1083's dropped premium schedule files under its own
    // letterhead and the ground; this files under the AUTHORITY and the ground, in the same two-subject
    // shape, so the THREADS view stacks a rail, a rota and a pour under one heading the moment a captain
    // has clipped a second one. That stack is the whole of the payoff and the book says nothing over it.

    /// <summary>#1074/#741 · What the book's entry for a line item is ABOUT: the office that is paying, and
    /// the ground it is paying for.</summary>
    /// <param name="siteName">The site's own name, as the game prints it on the ground the captain is
    /// standing on. Empty and null are the same answer and drop the subject rather than minting a blank
    /// heading (<see cref="CaseSubjects.Line(IEnumerable{CaseSubjects.Subject})"/>'s own rule).</param>
    public static string SubjectsFor(string? siteName) =>
        CaseSubjects.Line(TheOffice, CaseSubjects.Place(siteName ?? ""));

    /// <summary>#1074 · The office every one of these papers is about — <see cref="StopOrder.Stamp"/>, beat
    /// 1's own constant and not a second one spelled the same way, so the plate at the seal, the notice at
    /// the gate and the heading in the field book are one office rather than three.</summary>
    public static CaseSubjects.Subject TheOffice => CaseSubjects.Office(StopOrder.Stamp);

    /// <summary>#1074 · …and the ground, named as the game names it. Published beside
    /// <see cref="TheOffice"/> so a guard can ask for the two subjects by name instead of re-splitting the
    /// joined line and hoping it split the way the author meant.</summary>
    public static CaseSubjects.Subject TheSite(string? siteName) => CaseSubjects.Place(siteName ?? "");

    /// <summary>#1074 · Every player-facing string this beat publishes — three, and they are the three the
    /// canon pass authored. The same <c>AllProse</c> discipline every prose-bearing type in Core keeps, and
    /// the list the reserved-word sweep and the no-fourth-string sweep both walk.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return PourLineItem;
        yield return RailLineItem;
        yield return RotaLineItem;
    }
}
