using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #1151 slice 2 · <b>THE INTERVIEW.</b> The booked captain sits across the adjuster's desk and a seated card sequence
/// asks three questions — the loss, the ship, the fault — each answered by SHOWING A PAPER. The company's whole
/// character is in the arithmetic: it resurrects a man without a form and makes a sail want three.
///
/// <para><b>Fail Forward law</b> (the Ropecon key): the dice decide how it goes wrong, never whether the captain may try.
/// A wrong paper leaves the question standing and costs nothing; there is no retry gate anywhere. Question three takes
/// ONE offer — any paper, or an explicit nothing — and that single offer is what shifts the outcome's weights.</para>
///
/// <para><b>Determinism.</b> The three rolls — whether question three's paper is accepted, which outcome, which
/// clause-or-cause — are all salted from the BOOKING's <c>{when}</c> (the loss's identity, whole sim seconds), never
/// from a clock or a press: a save and a reload replays the same interview, and a different paper cannot buy a
/// different roll. Every magnitude below is FLAGGED: Fable's cut fixed the weights and the day-rate; the acceptance
/// chance and the clauses' bites are the crew's reading of a gap, said so on the PR.</para>
///
/// <para>Every player-facing word is Fable canon (2026-10-07 brief cut, slice 2), verbatim. The adjuster stays unnamed:
/// the plate is the name, NEBULA MUTUAL is who speaks. The clause is printed, never remembered — the flashback is
/// slice 3.</para>
/// </summary>
public static class ClaimInterview
{
    // ── THE CANON: SEATING AND THE THREE QUESTIONS ──────────────────────────────────────────────────────

    /// <summary>The booked captain sits at her desk on her watch.</summary>
    public const string SeatingLine = "She reads the form before she looks at you, which tells you the order of things here.";

    /// <summary>Q1 — her ask.</summary>
    public const string Q1Ask = "State the loss. The form states it; state it anyway. The company likes the two to match.";

    /// <summary>Q1 — the filled form answers, {n} the days.</summary>
    public static string Q1Right(int tenths) =>
        $"Hull, holed. {HullClaim.DaysText(tenths)} days under way. The form agrees with you. That is the last easy thing that will happen here.";

    /// <summary>Q1 — any other paper (the question stands; no penalty).</summary>
    public const string Q1Wrong = "That is a document. It is not this document.";

    /// <summary>Q2 — her ask.</summary>
    public const string Q2Ask =
        "The company pays captains, not ships. Show me the ship is yours the way the collectors would ask it.";

    /// <summary>Q2 — the ship↔captain proof answers.</summary>
    public const string Q2Right =
        "So the ship is yours. The collectors will be told you could prove it; it ruins their whole afternoon.";

    /// <summary>Q2 — any other paper (stands, no penalty).</summary>
    public const string Q2Wrong = "The company has files on who owns what. I am asking whether you do.";

    /// <summary>Q3 — her ask (the open question).</summary>
    public const string Q3Ask = "Last box. Why was this loss not your doing? You may show me anything. People do.";

    /// <summary>Q3 — accepted (the seeded roll).</summary>
    public const string Q3AcceptedLine =
        "…This will do. Not because it proves anything — because it is the kind of paper the file wants inside it.";

    /// <summary>Q3 — not accepted.</summary>
    public const string Q3RefusedLine = "I have read it. The file would read it differently.";

    /// <summary>Q3 — offering nothing, which is allowed.</summary>
    public const string Q3NothingLine = "No answer is an answer. The file has a box for it.";

    /// <summary>UI CHROME, NOT CANON (the crew's gap, flagged on the PR) · the row for question three's explicit nothing.</summary>
    public const string NothingLabel = "Nothing";

    /// <summary>UI CHROME, NOT CANON (the crew's gap, flagged on the PR) · the button on every paper's row.</summary>
    public const string ShowLabel = "Show";

    // ── THE CANON: THE THREE OUTCOMES ───────────────────────────────────────────────────────────────────

    /// <summary>PAID (rare), told.</summary>
    public const string PaidLine = "Paid in full. Do not tell anyone; the company has a reputation.";

    /// <summary>The paid settlement's title.</summary>
    public const string PaidTitle = "A settlement, paid";

    /// <summary>The paid settlement's document, {n} the days.</summary>
    public static string PaidDocument(int tenths) =>
        $"Hull, holed. {HullClaim.DaysText(tenths)} days under way at the policy's day-rate. Paid without deduction. "
        + "The adjuster notes, for the file, that this was the correct outcome and that she expects consequences.";

    /// <summary>ADJUSTED (the house outcome), told — {c} the clause's number.</summary>
    public static string AdjustedLine(int clause) =>
        $"Adjusted and paid. The deduction is Clause {clause}: '{ClauseText(clause)}'. You signed it. Most people did.";

    /// <summary>The adjusted settlement's title.</summary>
    public const string AdjustedTitle = "A settlement, adjusted";

    /// <summary>The adjusted settlement's document, {n} the days and {c} the clause.</summary>
    public static string AdjustedDocument(int tenths, int clause) =>
        $"Hull, holed. {HullClaim.DaysText(tenths)} days at the policy's day-rate, less Clause {clause}. "
        + "The arithmetic is enclosed. The arithmetic is correct. Correct is the company's favourite word.";

    /// <summary>DECLINED, told.</summary>
    public const string DeclinedLine =
        "Declined. The cause is procedural, which the company prefers, because procedure holds up.";

    /// <summary>The returned form's title.</summary>
    public const string ReturnedTitle = "A claim form, returned";

    /// <summary>The stamp's one word, on the book's loss line.</summary>
    public const string DeclinedStamp = " · DECLINED";

    /// <summary>The clause pool (1-based in print) — the seeded pick of an ADJUSTED outcome.</summary>
    public static string ClauseText(int clause) => clause switch
    {
        1 => "Days lost to weather of the company's own classification are borne gladly by the insured.",
        2 => "The first day of any loss is the captain's day, in recognition of the sea tradition.",
        3 => "Loss of use is compensated at the rate the use would probably have earned, as the company reckons probability.",
        _ => throw new ArgumentOutOfRangeException(nameof(clause)),
    };

    /// <summary>The cause pool (1-based) — the seeded pick of a DECLINED outcome.</summary>
    public static string CauseText(int cause) => cause switch
    {
        1 => "Filed on a form series the company retired while the claim was pending.",
        2 => "The loss and the claimant are in order; the policy's schedule for this class of loss is under revision.",
        3 => "The figures are correct but arrived in the wrong order.",
        _ => throw new ArgumentOutOfRangeException(nameof(cause)),
    };

    /// <summary>How many clauses and causes the pools hold.</summary>
    public const int PoolSize = 3;

    // ── THE FLAGGED NUMBERS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>FLAGGED (cut) · credits per day lost, the policy's day-rate.</summary>
    public const int ClaimDayRate = 40;

    /// <summary>FLAGGED (cut) · the outcome's weights out of <see cref="WeightTotal"/>: PAID, ADJUSTED, DECLINED.</summary>
    public static readonly int[] BaseWeights = [1, 8, 3];

    /// <summary>FLAGGED (cut) · …shifted toward the captain once question three was accepted.</summary>
    public static readonly int[] AcceptedWeights = [2, 9, 1];

    /// <summary>The die the outcome is rolled on (every weight set sums to this).</summary>
    public const int WeightTotal = 12;

    /// <summary>FLAGGED (CREW'S GAP) · the cut gives question three a seeded acceptance and no chance: this many in
    /// <see cref="WeightTotal"/> accept. TODO on the PR for Fable's number.</summary>
    public const int Q3AcceptedInTwelve = 5;

    /// <summary>FLAGGED (CREW'S GAP) · each clause's deduction, percent of the gross, by clause number − 1. The cut
    /// says the fractions are FLAGGED and gives none. TODO on the PR for Fable's numbers.</summary>
    public static readonly int[] ClauseBitePercent = [25, 15, 35];

    // ── THE DICE ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Is question three's paper accepted? One roll, salted from the booking's <c>{when}</c> alone.</summary>
    public static bool Q3Accepts(long when) =>
        DiceRule.Roll(DiceRule.Seed("claim:interview:q3", when), WeightTotal).Face <= Q3AcceptedInTwelve;

    /// <summary>The outcome: one roll over the weights — the accepted set only when question three was accepted.</summary>
    public static Outcome OutcomeOf(long when, bool q3Accepted)
    {
        int face = DiceRule.Roll(DiceRule.Seed("claim:interview:outcome", when), WeightTotal).Face;
        int[] w = q3Accepted ? AcceptedWeights : BaseWeights;
        return face <= w[0] ? Outcome.Paid : face <= w[0] + w[1] ? Outcome.Adjusted : Outcome.Declined;
    }

    /// <summary>The clause's number (1..3) of an ADJUSTED outcome, or the cause's of a DECLINED — one seeded pick.</summary>
    public static int PickOf(long when) =>
        DiceRule.Roll(DiceRule.Seed("claim:interview:pick", when), PoolSize).Face;

    /// <summary>What a loss is worth at the day-rate, before any clause.</summary>
    public static int GrossCr(int tenths) => (int)Math.Round(ClaimDayRate * tenths / 10.0, MidpointRounding.AwayFromZero);

    /// <summary>What the clause takes off the gross.</summary>
    public static int BiteCr(int tenths, int clause) =>
        (int)Math.Round(GrossCr(tenths) * ClauseBitePercent[clause - 1] / 100.0, MidpointRounding.AwayFromZero);

    // ── THE STATE MACHINE ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Which question the card is on.</summary>
    public enum Stage
    {
        /// <summary>The loss.</summary>
        Loss,

        /// <summary>The ship.</summary>
        Ship,

        /// <summary>The fault.</summary>
        Fault,

        /// <summary>Done: the outcome is told and settled.</summary>
        Settled,
    }

    /// <summary>The three honest outcomes.</summary>
    public enum Outcome
    {
        /// <summary>Rare, the full figure.</summary>
        Paid,

        /// <summary>The house outcome: the day-rate less a clause.</summary>
        Adjusted,

        /// <summary>Procedural, nothing paid, the form returned.</summary>
        Declined,
    }

    /// <summary>What a row on the card is.</summary>
    public enum ShowKind
    {
        /// <summary>A paper out of the satchel.</summary>
        Paper,

        /// <summary>The policy — the wallet card the collectors' law names as the ship↔captain proof.</summary>
        Policy,

        /// <summary>Question three's explicit nothing.</summary>
        Nothing,
    }

    /// <summary>One thing the captain can show.</summary>
    public readonly record struct Show(ShowKind Kind, string Id, string Label);

    /// <summary>The card's answer to one press: where it goes next and what is said.</summary>
    public readonly record struct Reply(Stage Next, string Said, bool Q3Accepted);

    /// <summary>Her ask for a stage.</summary>
    public static string AskOf(Stage stage) => stage switch
    {
        Stage.Loss => Q1Ask,
        Stage.Ship => Q2Ask,
        Stage.Fault => Q3Ask,
        _ => "",
    };

    /// <summary>
    /// <b>ONE PRESS.</b> The filled form for THIS loss answers question one (any other paper, a stale form included,
    /// leaves it standing); the policy — in force — answers question two; question three takes anything once and the
    /// seeded roll decides. Nothing is ever refused: a wrong show only says its line and the question stands.
    /// </summary>
    public static Reply Step(Stage stage, Show shown, HullClaim.Loss loss, bool policyInForce)
    {
        switch (stage)
        {
            case Stage.Loss:
                return shown.Kind == ShowKind.Paper
                    && string.Equals(shown.Id, HullClaim.FilledId(loss), StringComparison.Ordinal)
                    ? new Reply(Stage.Ship, Q1Right(loss.Tenths), false)
                    : new Reply(Stage.Loss, Q1Wrong, false);

            case Stage.Ship:
                return shown.Kind == ShowKind.Policy && policyInForce
                    ? new Reply(Stage.Fault, Q2Right, false)
                    : new Reply(Stage.Ship, Q2Wrong, false);

            case Stage.Fault:
                if (shown.Kind == ShowKind.Nothing)
                {
                    return new Reply(Stage.Settled, Q3NothingLine, false);
                }

                bool accepted = Q3Accepts(loss.DoneAt);
                return new Reply(Stage.Settled, accepted ? Q3AcceptedLine : Q3RefusedLine, accepted);

            default:
                return new Reply(Stage.Settled, "", false);
        }
    }

    /// <summary>The settled interview: what is told, what is paid, which paper the desk hands over.</summary>
    public readonly record struct Settlement(Outcome Outcome, int Credits, int Pick, string Told, string PaperId);

    /// <summary>
    /// <b>THE OUTCOME.</b> Rolled once, after the last question, from the booking's <c>{when}</c>. PAID pays the gross;
    /// ADJUSTED pays the gross less its clause's bite; DECLINED pays nothing and returns the form stamped.
    /// </summary>
    public static Settlement Settle(HullClaim.Loss loss, bool q3Accepted)
    {
        Outcome outcome = OutcomeOf(loss.DoneAt, q3Accepted);
        int pick = PickOf(loss.DoneAt);
        return outcome switch
        {
            Outcome.Paid => new Settlement(
                outcome, GrossCr(loss.Tenths), 0, PaidLine, PaidId(loss)),
            Outcome.Adjusted => new Settlement(
                outcome, GrossCr(loss.Tenths) - BiteCr(loss.Tenths, pick), pick, AdjustedLine(pick), AdjustedId(loss, pick)),
            _ => new Settlement(outcome, 0, pick, DeclinedLine, ReturnedId(loss, pick)),
        };
    }

    // ── THE PAPERS ──────────────────────────────────────────────────────────────────────────────────────

    private const string SettlementPrefix = "claim-settlement:";
    private const string ReturnedPrefix = "claim-form-returned:";
    private const string SettledTagPrefix = "claim:settled:";

    /// <summary>The register tag that closes the booked row: the interview was had, whatever it decided.</summary>
    public static string SettledTag(long when) => $"{SettledTagPrefix}{when}";

    /// <summary>Is this loss's booked row still open — booked, and not yet settled?</summary>
    public static bool IsOpen(IEnumerable<string> register, long when)
    {
        ArgumentNullException.ThrowIfNull(register);
        bool booked = false;
        foreach (string tag in register)
        {
            if (string.Equals(tag, SettledTag(when), StringComparison.Ordinal))
            {
                return false;
            }

            booked |= string.Equals(tag, HullClaim.BookedTag(when), StringComparison.Ordinal);
        }

        return booked;
    }

    /// <summary>The paid settlement's id.</summary>
    public static string PaidId(HullClaim.Loss loss) => $"{SettlementPrefix}paid:{loss.DoneAt}:{loss.Tenths}";

    /// <summary>The adjusted settlement's id.</summary>
    public static string AdjustedId(HullClaim.Loss loss, int clause) =>
        $"{SettlementPrefix}adjusted:{loss.DoneAt}:{loss.Tenths}:{clause}";

    /// <summary>The returned form's id.</summary>
    public static string ReturnedId(HullClaim.Loss loss, int cause) =>
        $"{ReturnedPrefix}{loss.DoneAt}:{loss.Tenths}:{cause}";

    /// <summary>Is this one of the three papers an interview hands over?</summary>
    public static bool IsAPaper(string? paperId) => TryRead(paperId, out _, out _, out _);

    private static bool TryRead(string? id, out Outcome outcome, out int tenths, out int pick)
    {
        outcome = default;
        tenths = 0;
        pick = 0;
        if (id is null)
        {
            return false;
        }

        string[] parts;
        if (id.StartsWith(SettlementPrefix, StringComparison.Ordinal))
        {
            parts = id[SettlementPrefix.Length..].Split(':');
            if (parts.Length == 3 && parts[0] == "paid")
            {
                outcome = Outcome.Paid;
                return NumL(parts[1], out _) && Num(parts[2], out tenths);
            }

            if (parts.Length == 4 && parts[0] == "adjusted")
            {
                outcome = Outcome.Adjusted;
                return NumL(parts[1], out _) && Num(parts[2], out tenths) && Num(parts[3], out pick) && pick is >= 1 and <= PoolSize;
            }

            return false;
        }

        if (id.StartsWith(ReturnedPrefix, StringComparison.Ordinal))
        {
            parts = id[ReturnedPrefix.Length..].Split(':');
            outcome = Outcome.Declined;
            return parts.Length == 3 && NumL(parts[0], out _) && Num(parts[1], out tenths) && Num(parts[2], out pick)
                && pick is >= 1 and <= PoolSize;
        }

        return false;
    }

    private static bool Num(string s, out int n) =>
        int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out n);

    private static bool NumL(string s, out long n) =>
        long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out n);

    /// <summary>The paper's title, or "" for any other id.</summary>
    public static string TitleOf(string? paperId) =>
        !TryRead(paperId, out Outcome o, out _, out _) ? "" : o switch
        {
            Outcome.Paid => PaidTitle,
            Outcome.Adjusted => AdjustedTitle,
            _ => ReturnedTitle,
        };

    /// <summary>The paper's document, or "" for any other id. The returned form is the filled form's own document
    /// with a final line — <c>— RETURNED. {cause}</c>.</summary>
    public static string DocumentOf(string? paperId) =>
        !TryRead(paperId, out Outcome o, out int tenths, out int pick) ? "" : o switch
        {
            Outcome.Paid => PaidDocument(tenths),
            Outcome.Adjusted => AdjustedDocument(tenths, pick),
            _ => HullClaim.FilledDocument(tenths) + "\n— RETURNED. " + CauseText(pick),
        };

    /// <summary>The book's loss line, stamped — only the DECLINED outcome writes it.</summary>
    public static string StampedLossLine(int tenths) => HullClaim.LossLine(tenths) + DeclinedStamp;

    /// <summary>
    /// The book after a DECLINED outcome: this loss's 📋 line gains the stamp's one word. The line is found by its own
    /// words AND the loss's own moment (the page files it on the frame the mend completes, so its sim-second is the
    /// loss's) — another loss of the same length is never stamped in its place. A line already stamped, or trimmed off a
    /// full book, leaves the book as it was.
    /// </summary>
    public static IReadOnlyList<FieldNote> Stamp(IReadOnlyList<FieldNote> book, HullClaim.Loss loss)
    {
        ArgumentNullException.ThrowIfNull(book);
        string words = HullClaim.LossLine(loss.Tenths);
        int at = -1;
        for (int i = 0; i < book.Count; i++)
        {
            if (!string.Equals(book[i].Text, words, StringComparison.Ordinal)
                || !string.Equals(book[i].Glyph, HullClaim.LossGlyph, StringComparison.Ordinal))
            {
                continue;
            }

            if ((long)Math.Floor(book[i].SimTime) == loss.DoneAt)
            {
                at = i;
            }
        }

        if (at < 0)
        {
            return book;
        }

        var stamped = new List<FieldNote>(book);
        stamped[at] = stamped[at] with { Text = StampedLossLine(loss.Tenths) };
        return stamped;
    }

    /// <summary>Every player-facing string this slice publishes, at the figures a sweep can pin (2.0 days, every clause
    /// and every cause).</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return SeatingLine;
        yield return Q1Ask;
        yield return Q1Right(20);
        yield return Q1Wrong;
        yield return Q2Ask;
        yield return Q2Right;
        yield return Q2Wrong;
        yield return Q3Ask;
        yield return Q3AcceptedLine;
        yield return Q3RefusedLine;
        yield return Q3NothingLine;
        yield return PaidLine;
        yield return PaidTitle;
        yield return PaidDocument(20);
        yield return AdjustedTitle;
        yield return DeclinedLine;
        yield return ReturnedTitle;
        yield return DeclinedStamp.Trim();
        for (int i = 1; i <= PoolSize; i++)
        {
            yield return ClauseText(i);
            yield return AdjustedLine(i);
            yield return AdjustedDocument(20, i);
            yield return CauseText(i);
            yield return DocumentOf(ReturnedId(new HullClaim.Loss(1, 20), i));
        }
    }
}
