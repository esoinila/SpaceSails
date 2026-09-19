using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #533 · <b>TWO INSTRUMENTS THAT DISAGREE</b> — the derived half of the wreck anomaly, and the one thing
/// the issue asks for that nobody has to author: <i>"a fact you can verify and cannot explain."</i>
///
/// <para>Owner: <i>"The story ones are exceptional in some sense that we are left to wonder. Like what was
/// such a rich ship doing there-kind of things 😎"</i> — which is a MECHANIC and not a mood. The captain
/// reads two numbers the game already computes, they do not sit together, and <b>nobody aboard is left to
/// ask.</b></para>
///
/// <h3>The discipline, which is the hard part</h3>
/// <list type="number">
/// <item><b>It survives being filed.</b> An anomaly is not a cause and must never become a dropdown option
/// — <see cref="Derelict.WreckCause"/> does not grow a word for it, ever. The report records the CAUSE; the
/// anomaly is a fact in the book beside it (<c>ThePaperworkGainsNoCause</c>).</item>
/// <item><b>No resolution, ever.</b> Nothing later explains one: no note found afterwards, no contact with
/// an answer, no arc card three lanes on. The wondering IS the content, and one explanation retroactively
/// cheapens every other wreck. Guarded as a SOURCE law — nothing outside the reading and the book may so
/// much as name this file (<c>NothingOutsideTheReadingAndTheBookReadsAnAnomaly</c>).</item>
/// <item><b>It never concludes.</b> Two facts, side by side, and nothing else. No causal word gets to
/// stand between them — the line says what both instruments read and stops
/// (<c>NoAnomalyStringConcludesAnything</c>).</item>
/// <item><b>The fittings stay identical.</b> Nothing about a hull carrying one is visible until she is
/// read: no extra console, no mark on the deck, no second painting. <see cref="WreckLayout.StandardFittings"/>
/// is untouched by this file.</item>
/// </list>
///
/// <h3>Never invent a number to make one fit</h3>
/// <para><b>The issue lists five derived anomalies and this build can honestly derive ONE of them.</b> That
/// is not a shortfall to be papered over with a plausible-looking constant: an anomaly is a thing the
/// captain can VERIFY, so both of its numbers have to be numbers the game already computes about that hull.
/// The audit, kept with the feature rather than in a PR nobody reads again:</para>
/// <list type="bullet">
/// <item><b>RICH HULL, POOR ROAD — shipped.</b> <see cref="Derelict.Wreck.AssessedValueCr"/> against
/// <see cref="ArrivalTube.ScheduledTonnage"/> — the traffic that is ON A BOARD at the berth she hangs off,
/// which is #541's own rule and already carries the fiction: <i>"a place served only by discreet haulers
/// has no scheduled foot traffic"</i>. Both numbers are real, both vary across the shipped world (Ringside
/// 23, Selene Gate 10, The Tilt and The Deep zero), and neither was invented for this.</item>
/// <item><b>LOST TOO LONG — not derivable.</b> <see cref="Derelict.SearchConeRadiusMeters"/> is
/// <c>years × DriftConePerYearMeters</c> and nothing else, so the two numbers the line would put side by
/// side are ONE number written twice: every inequality between them is true of every hull or of none. And
/// there is no search-RATE anywhere in the game for "closes in a season" to be checked against. Shipping it
/// would be a threshold that selects everything — this repo's fifth named bug class.</item>
/// <item><b>THE MANIFEST AND THE HOLD — not derivable.</b> The manifest is a VALUE and a sentence; there is
/// no count of manifest lines and no count of lots in the hold. Both arrive with <c>ShipPapers</c>
/// (<c>docs/features/the-paperwork.md</c> §7, build order item 1).</item>
/// <item><b>CRADLES AGAINST CREW — half derivable, and half is none.</b> The cradles are real and countable
/// from the doorway (<see cref="WreckLayout.CradleCount"/>, <see cref="Derelict.LifeboatsLaunched"/>); the
/// CREW LIST does not exist as a number anywhere in the game. It arrives with the crew list and watch bill,
/// the same build order item.</item>
/// <item><b>AN AGE THAT WILL NOT SIT STILL — not derivable.</b> Her laid-down year is real
/// (<see cref="ShipHistories.For"/>); no fitting aboard any hull carries a year of manufacture. A pump with
/// a date on it is content, and content is authored, not derived.</item>
/// </list>
/// <para>The four canon lines those wait on are written down in <c>docs/features/the-paperwork.md</c> §7c,
/// beside the fact each one needs. They are deliberately NOT constants here: a string in Core that nothing
/// reads is a truth the game is not telling (#646 found one and it cost a hull her own log).</para>
///
/// <para>Pure and deterministic, like every other wreck rule: the same hull always carries the same anomaly
/// or the same nothing, so leaving and coming back is not a re-roll and a test can pin her.</para>
/// </summary>
public static class WreckAnomaly
{
    // ── WHAT AN ANOMALY IS ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The anomalies this build can derive. One arm, because one is what the shipped facts honestly
    /// support — see the audit in the type's own summary. The enum is the growth seam: a kind lands the day
    /// the numbers behind it do, and not one day earlier.</summary>
    public enum Kind
    {
        /// <summary>A fitted-out ship, assessed high, on a road this world lists no traffic on.</summary>
        RichHullPoorRoad,
    }

    /// <summary>
    /// The facts about a hull that the hull cannot answer for herself — where she is, read against the world
    /// she is in. Handed in rather than reached for, so this file stays pure and a test can state the world
    /// exactly.
    /// </summary>
    /// <param name="ListedTonnageOnHerRoad">The traffic that appears on a departures board for the road she
    /// hangs on — <see cref="ArrivalTube.ScheduledTonnage"/> of the berth she was found off, whose
    /// neighbourhood is the system she is lost in. Zero is the whole of "poor road": whatever comes this
    /// way, none of it is listed anywhere. It does not move with the clock (it is the route table and the
    /// body tree, nothing else), so a hull's anomaly is the same on the second boarding as on the first.</param>
    public readonly record struct Facts(double ListedTonnageOnHerRoad);

    /// <summary>
    /// One anomaly, as the captain meets it: the two facts on one line at the station they are already
    /// standing at, and the same two facts as the book keeps them.
    /// </summary>
    /// <param name="Of">Which one.</param>
    /// <param name="Line">Canon, verbatim, with the hull's own numbers in it — the fourth line at the
    /// evidence reading.</param>
    /// <param name="Gist">What the field book writes down: the same facts, §12 voice, no conclusion.</param>
    /// <param name="Subjects">#741's law — the subject is declared by the AUTHOR of the sentence, never
    /// worked out of its words afterwards. This gist prints the hull's name, so the hull is its subject.</param>
    public readonly record struct Reading(Kind Of, string Line, string Gist, string Subjects);

    /// <summary>The glyph the book's entry leads with. A measurement and not a verdict: the whole point is
    /// that the captain has two readings and no finding.</summary>
    public const string Glyph = "📐";

    // ── HOW OFTEN ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ONE HULL IN THIS MANY, among the hulls whose own numbers support an anomaly at all, actually carries
    /// one. FLAGGED for the owner's tuning, and the only frequency in this file — the issue's first rule is
    /// a rarity budget rather than a content budget (<i>"if every wreck is a mystery, none of them is"</i>).
    ///
    /// <para>It sits ON TOP of support, not beside it: a hull the numbers cannot honestly say anything about
    /// is never dealt one whatever this is set to, so the rate a captain actually meets is this one THROUGH
    /// how often the world deals a rich hull on an empty road. <c>TwoInstrumentsDisagreeTests</c> measures
    /// both and prints them.</para>
    /// </summary>
    public const int CarriesOneInN = 3;

    /// <summary>
    /// WHERE "RICH" BEGINS, and it is not a number typed here: the top quarter of the span every wreck's
    /// cargo is assessed inside (<see cref="Derelict.AssessedFloorCr"/> … <see cref="Derelict.AssessedCeilingCr"/>).
    /// Retune the span and this follows it instead of drifting away from it. FLAGGED for tuning.
    /// </summary>
    public static int RichFromCr =>
        Derelict.AssessedFloorCr + (3 * (Derelict.AssessedCeilingCr - Derelict.AssessedFloorCr) / 4);

    // ── WHICH ONES HER NUMBERS ACTUALLY SUPPORT ─────────────────────────────────────────────────────────

    /// <summary>
    /// Is this anomaly TRUE of this hull? The whole of the honesty rule: an anomaly is only ever dealt to a
    /// ship whose own numbers really do disagree, so a captain who checks the two instruments finds exactly
    /// what the line said they would.
    /// </summary>
    public static bool Supports(Kind kind, in Derelict.Wreck wreck, in Facts facts) => kind switch
    {
        Kind.RichHullPoorRoad => wreck.AssessedValueCr >= RichFromCr && facts.ListedTonnageOnHerRoad <= 0,
        _ => false,
    };

    /// <summary>Every anomaly this hull's numbers support, in a stable order — the pool one is dealt from,
    /// and empty for the ordinary hull, which is most of them.</summary>
    public static IReadOnlyList<Kind> SupportedBy(in Derelict.Wreck wreck, in Facts facts)
    {
        var supported = new List<Kind>();
        foreach (Kind kind in Enum.GetValues<Kind>())
        {
            if (Supports(kind, wreck, facts))
            {
                supported.Add(kind);
            }
        }

        return supported;
    }

    // ── THE DEAL ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>ZERO OR ONE, AND THE SAME ONE FOREVER.</b> Seeded off the hull's id, so a rumour that names her can
    /// be trusted, a reload finds the same ship, and a test can pin her.
    ///
    /// <para>The id goes into the seed WHOLE (<see cref="DiceRule.Seed(string, long[])"/> folds the
    /// characters itself) rather than through <c>string.GetHashCode</c>, which .NET randomises per process:
    /// seeded that way a hull would carry an anomaly this afternoon and not tomorrow. The same lesson
    /// <see cref="BlackOpsKey.IsAboard"/> paid for and measured.</para>
    /// </summary>
    public static Kind? Dealt(in Derelict.Wreck wreck, in Facts facts)
    {
        IReadOnlyList<Kind> supported = SupportedBy(wreck, facts);
        if (supported.Count == 0)
        {
            return null;
        }

        if (DiceRule.Roll(DiceRule.Seed($"wreck-anomaly-aboard:{wreck.Id}"), CarriesOneInN).Face != 1)
        {
            return null;
        }

        // Which one, when a hull's numbers support more than one. A second, differently-tagged stream, so
        // widening the pool cannot change WHETHER a hull carries one — only which of hers she carries.
        ulong which = DiceRule.Seed($"wreck-anomaly-which:{wreck.Id}");
        return supported[(int)(which % (ulong)supported.Count)];
    }

    /// <summary>The whole reading for a hull, or null for the ordinary ship. The one call the client makes.</summary>
    public static Reading? For(in Derelict.Wreck wreck, in Facts facts)
    {
        if (Dealt(wreck, facts) is not { } kind)
        {
            return null;
        }

        return new Reading(kind, Line(kind, wreck), Gist(kind, wreck), SubjectsOf(wreck));
    }

    // ── WHAT IT SAYS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// CANON (Fable, on #533), verbatim, with the hull's own numbers formatted the way every other number on
    /// this ship is formatted — the manifest's own <c>N0 cr</c>, so the value on the anomaly line and the
    /// value on the manifest two rooms away are the same number in the same clothes.
    ///
    /// <para>Two facts, side by side, and nothing else. There is no third sentence and there must never be
    /// one: the third sentence is where a conclusion goes.</para>
    /// </summary>
    public static string Line(Kind kind, in Derelict.Wreck wreck) => kind switch
    {
        Kind.RichHullPoorRoad =>
            $"Assessed at {wreck.AssessedValueCr:N0} cr. Traffic on this road, listed, this year: none.",
        _ => "",
    };

    /// <summary>What the field book keeps: the same two facts in the book's own §12 voice, under the hull's
    /// name, ending where every one of these ends — with the thing that makes it an anomaly rather than a
    /// question, which is that there is nobody to put it to.</summary>
    public static string Gist(Kind kind, in Derelict.Wreck wreck) =>
        kind switch
        {
            Kind.RichHullPoorRoad =>
                $"{wreck.ShipName} — assessed at {wreck.AssessedValueCr:N0} cr, no listed traffic on this "
                + "road this year, and nobody aboard to ask",
            _ => "",
        };

    /// <summary>
    /// #741's law, obeyed by the author rather than by a parser: this gist PRINTS the hull's name, so the
    /// hull is what it is about. A wreck is somewhere you went and stood, so she is a
    /// <see cref="CaseSubjects.Kind.Place"/> — which is also what makes two boardings of one hull stack into
    /// a thread the captain drew and the game did not.
    /// </summary>
    public static string SubjectsOf(in Derelict.Wreck wreck) =>
        CaseSubjects.Line(CaseSubjects.Place(wreck.ShipName));

    /// <summary>Every sentence this feature can put on a screen, for the audit that reads them all.</summary>
    public static IEnumerable<string> EveryLine(Derelict.Wreck wreck)
    {
        var said = new List<string>();
        foreach (Kind kind in Enum.GetValues<Kind>())
        {
            said.Add(Line(kind, wreck));
            said.Add(Gist(kind, wreck));
        }

        return said;
    }
}
