using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1151 slice 3 · <b>THE UNEASE FLASHBACK.</b> The one beat where the claim touches #973's amnesia canon. It fires on
/// the FIRST ADJUSTED settlement of the run — ADJUSTED is the outcome that quotes a clause the captain signed, and only
/// the first hearing cuts (the Kosh law: rare, private, made a show of). PAID quotes nothing; DECLINED quotes causes,
/// which are procedure, not signature. Later ADJUSTED outcomes just quote, beat-free.
///
/// <para>Exactly one record goes with it, decided by what the captain holds: the signing sheet (#973) grows ONE margin
/// line, once ever; with no such sheet the book's 📍 entry says it instead, once. Nothing confirms where the memory
/// went (§13.8 discipline — the same silence, again). The dab rides the pendant's sting path (the shock carry); its
/// label lives here and NerveModel is untouched. Every word is Fable canon (2026-10-08 brief cut), verbatim.</para>
/// </summary>
public static partial class ClaimInterview
{
    /// <summary>The subject the Flashback beat is raised with. The plate wears the GENERIC stamp (A PAGE YOU DON'T
    /// REMEMBER WRITING — always this page's); the subject only chooses the caption.</summary>
    public const string FlashbackSubject = "the clause in her voice";

    /// <summary>The plate's caption (canon, verbatim).</summary>
    public const string FlashbackCaption =
        "Her voice does the clause in the company's cadence, and halfway through it your own hand answers — the pen, "
        + "the cold of a counter, a date you could not produce now for any money. You signed this. The memory ends at "
        + "the signature, as if the signature were a door.";

    /// <summary>The plate's caption for this subject, or null for every other subject.</summary>
    public static string? FlashbackCaptionFor(string? subject) =>
        string.Equals(subject, FlashbackSubject, StringComparison.Ordinal) ? FlashbackCaption : null;

    /// <summary>FLAGGED (cut) · the unease's dab, raw nerve — the pendant's money sting's own idiom, banked in the shock
    /// carry (the gauge holds).</summary>
    public const double UneaseNerve = 4.0;

    /// <summary>The shock's reason text (canon, verbatim).</summary>
    public const string UneaseShockLabel = "the clause answered in your own hand";

    /// <summary>The line the signing sheet grows, once ever (canon, verbatim).</summary>
    public const string MarginLine = "In the margin, in a hand that is not yours: 'Read to claimant in full.'";

    /// <summary>What the unease's 📍 entry files under (owner ruling on #1371): the company only - Nebula Mutual, NO place arm,
    /// because the clause was read to the captain by the company, not by a room. Mirrors <see cref="HullClaim.SubjectsFor"/> without its place.</summary>
    public static string UneaseSubjects => CaseSubjects.Line(CaseSubjects.Office(NebulaLore.TermsRefiledOffice));

    /// <summary>The book's 📍 entry when the signing memory is NOT held, once (canon, verbatim).</summary>
    public const string UneaseBookLine =
        "She read a clause today that I knew before she finished it. Filed under: things the fine print kept.";

    /// <summary>The register tag that latches the beat: once per run, whatever the outcome that follows.</summary>
    public const string UneaseTag = "claim:unease";

    /// <summary>FLAGGED (CREW'S GAP) · the dev start's booking <c>{when}</c> for <c>?claim=3</c>, seed-searched on the real
    /// dice (<see cref="OutcomeOf"/>) so the interview lands ADJUSTED whichever way question three is answered — never a
    /// forced roll; determinism stays the booking's.</summary>
    public const long Claim3When = 1001;

    /// <summary>Does this settlement raise the unease? Only an ADJUSTED one, and only while the run's latch is unspent.</summary>
    public static bool RaisesTheUnease(Outcome outcome, IEnumerable<string> register)
    {
        ArgumentNullException.ThrowIfNull(register);
        if (outcome != Outcome.Adjusted)
        {
            return false;
        }

        foreach (string tag in register)
        {
            if (string.Equals(tag, UneaseTag, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Does this sheet text already carry the margin line?</summary>
    public static bool HasTheMargin(string? text) =>
        text is not null && text.Contains(MarginLine, StringComparison.Ordinal);

    /// <summary>The sheet's text with the margin line grown onto it (a space, as the rebirth line is) — once: a text that
    /// already carries it is returned unchanged.</summary>
    public static string WithTheMargin(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return HasTheMargin(text) ? text : $"{text} {MarginLine}";
    }
}
