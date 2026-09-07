using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #426 · CHAIN-OF-CUSTODY DREAD — <i>was this hull actually maintained across all its owners?</i>
///
/// <para><b>Owner, 2026-07-20, sailing through a storm:</b> <i>"The word 'storm' was spoken... makes one
/// think the ship's long chain of owners and maintenance organizations and insurers.. hope it all was
/// checked through the whole chain. 😅"</i></para>
///
/// <para>Your rustbucket has passed through many owners (#397's former names, owners-deep), each
/// maintenance org and each insurer signing off — or not. In a hull-shudder window (#424) the tremor stops
/// being weather for one line and becomes a question about the paperwork behind the weld that just spoke.</para>
///
/// <h3>What this is, and what it is emphatically not</h3>
///
/// <para><b>It is one sentence.</b> Nothing rolls, nothing breaks, nothing is scheduled, and there is no
/// maintenance-debt field — the issue files that hook as OPTIONAL and the 2026-08-02 walk-through
/// recommends against building it, for the reason the whole house style rests on: a hidden per-hull number
/// that makes stress events likelier is a punishment the player cannot see, cannot fix and did not choose,
/// and the moment a mechanic can inspect the weld and say "clean", the dread is spent. <b>It must never
/// resolve.</b> The wondering is the content (the same law #533 puts on texture-tier anomalies, and the
/// same law that governs the Reever origin).</para>
///
/// <para><b>It authors nothing.</b> The three lines below are Fable's canon pass (2026-09-05) and are the
/// only strings in this file. Every fact inside them — the yard, the year, the name she used to answer to —
/// is read out of the hull's own <see cref="ShipHistory"/>, never typed here. A guard sweeps this type's
/// string constants and fails on a fourth one, because a typed yard is how a hull's plate and a hull's
/// dread end up naming two different builders in front of the player.</para>
///
/// <h3>The laws</h3>
///
/// <list type="bullet">
/// <item><b>Deterministic per (hull, window).</b> Pure of a <see cref="ShipHistory"/> and one window seed —
/// no clock, no <see cref="System.Random"/> (repo agreement §9). The same ship worries the same way about
/// the same storm.</item>
/// <item><b>A hull with no chain says nothing.</b> No former name and no yard record is a hull with no
/// paperwork to doubt, and <see cref="Line"/> hands back null rather than inventing a past for her.</item>
/// <item><b>Line 2 needs a former name.</b> A hull that has never been renamed cannot be asked who signed
/// her survey under a name she never wore.</item>
/// <item><b>Status rank.</b> It is weather with a memory, not a reveal: nothing here changes what the
/// captain knows, owes or can do, so it never stands on <see cref="Telling.Floor"/> (#761).</item>
/// </list>
/// </summary>
public static class ChainOfCustody
{
    /// <summary>Which of the three worries a window is carrying, or <see cref="None"/> for a hull with no
    /// chain to worry about. Exposed so a guard can name the branch it is checking rather than matching on
    /// prose.</summary>
    public enum Doubt
    {
        /// <summary>No chain — nothing to say, and nothing is said.</summary>
        None = 0,

        /// <summary>The survey signed by a yard that closed the next year.</summary>
        TheSurvey = 1,

        /// <summary>The name she used to answer to, and whoever signed under it.</summary>
        TheName = 2,

        /// <summary>The yard that laid her down, and every owner since who trusted it.</summary>
        TheYard = 3,
    }

    // ── The three lines. Fable's canon pass, 2026-09-05, verbatim. Braces filled from the record. ──────

    private const string SurveyLine =
        "The hull groans at a seam. Somewhere in her chain of owners a survey was signed 'inspected — pass' "
        + "by a yard that closed the next year. You hope the weld agreed.";

    private const string NameLine =
        "A frame ticks under the load. She was {0} once; whoever signed her last survey under that name is "
        + "not answering the radio either.";

    private const string YardLine =
        "Something aft settles into a new shape. {0} laid her down in {1}, and every owner since has "
        + "trusted that.";

    /// <summary>
    /// The worry this hull carries in this window, or null when she has no chain to be uneasy about.
    /// </summary>
    /// <param name="history">The hull's own record — the ONLY source of every fact in the sentence.</param>
    /// <param name="windowSeed">The storm window's seed. The caller folds the hull and the window into it,
    /// which is what makes the choice stable for a given ship worrying about a given storm.</param>
    public static string? Line(ShipHistory history, ulong windowSeed)
    {
        ArgumentNullException.ThrowIfNull(history);

        return Which(history, windowSeed) switch
        {
            Doubt.TheSurvey => SurveyLine,
            Doubt.TheName => string.Format(CultureInfo.InvariantCulture, NameLine, history.GloryName),
            Doubt.TheYard => string.Format(
                CultureInfo.InvariantCulture, YardLine, history.Yard, history.Year),
            _ => null,
        };
    }

    /// <summary>
    /// Which worry this window draws — the choice, without the words.
    ///
    /// <para>Only the lines this hull's record can honestly fill are candidates, and the candidate list is
    /// built in one fixed order every time (a list built by appending is not a list in a stable order
    /// unless it is built the same way each call — the fourth named bug class in this repo). The seed then
    /// picks among however many she has.</para>
    /// </summary>
    public static Doubt Which(ShipHistory history, ulong windowSeed)
    {
        ArgumentNullException.ThrowIfNull(history);

        Span<Doubt> candidates = stackalloc Doubt[3];
        int count = 0;

        // Line 1 needs a chain of owners for the survey to be lost somewhere inside.
        if (history.HasFormerNames || history.OwnersDeep > 0)
        {
            candidates[count++] = Doubt.TheSurvey;
        }

        // Line 2 needs a name she used to answer to. (Fable's law: "only for a hull with at least one
        // former name; otherwise line 1 or 3".)
        if (history.GloryName is not null)
        {
            candidates[count++] = Doubt.TheName;
        }

        // Line 3 needs the yard and the year off her plate.
        if (history.HasYardRecord)
        {
            candidates[count++] = Doubt.TheYard;
        }

        // A brand-new hull with no chain says nothing (Fable's law) — no former name, no yard record, no
        // owner before this one, so there is no paperwork behind her to be uneasy about.
        return count == 0 ? Doubt.None : candidates[(int)(windowSeed % (ulong)count)];
    }
}
