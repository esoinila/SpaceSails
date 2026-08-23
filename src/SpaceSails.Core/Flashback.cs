using System.Text;

namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L1 · READING A GREY PAGE.
//
// A page you don't remember writing is not furniture: it is a thing you can sit down with and try. The
// try is a roll on the ONE shared engine (`DiceRule`, owner ruling §5.0 — "one Core dice/modifier rule,
// every consequence system rolls on it"), 2D6 with the policy's tier as the named modifier the player
// can read, and it settles three ways:
//
//   HIGH    it comes back — the page warms and is yours again.
//   MIDDLE  it comes back WRONG — one detail on the page is not what it was, and the page never says
//           which. The original is kept in the vault, unread by any surface, so a later lane can lay
//           the page beside a document and find the disagreement.
//   LOW     nothing. The page stays somebody else's, and there is no second try this life.
//
// UNRELIABLE BY RULE, Ropecon-style (issue #973): a flashback never fails forward into nothing worth
// having — the worst outcome is WRONG, and wrong is a clue. The only price is the sanity seam the
// Ropecon sheets were kept for (#226): a page that comes back wrong costs one nerve pip.
//
// THE LIE IS MADE OF YOUR OWN BOOK. When a detail moves, what it moves TO is a detail off another page
// of the same ledger — a number you really did write down, a name who really did tell you something, a
// place you really were. Nothing is invented, which is why a wrong page is so hard to catch: every part
// of it is true, and only the arrangement is not.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 L1 · The flashback attempt: the shared-dice roll, its three outcomes, the words each one
/// says, and the deterministic single-detail alteration a wrong recollection makes. Pure — the client
/// spends the nerve pip and raises the beat.</summary>
public static class Flashback
{
    /// <summary>What a try at a grey page settles into.</summary>
    public enum Outcome
    {
        /// <summary>The page stays somebody else's, and stays grey. No second roll this life.</summary>
        Nothing = 0,

        /// <summary>It comes back, and one detail on it is not how you remember it.</summary>
        CameBackWrong = 1,

        /// <summary>It comes back whole.</summary>
        CameBack = 2,
    }

    /// <summary>2D6 + the tier at or over this and the page comes back whole. The PbtA/Fail-Forward
    /// ladder the rest of the homage uses. FLAGGED for the owner's tuning.</summary>
    public const int ComesBackAt = 10;

    /// <summary>…and at or over this (but under <see cref="ComesBackAt"/>) it comes back wrong. Under it,
    /// nothing — which on a 2D6 is a sixth of the range at Basic, and rather more of it uninsured.</summary>
    public const int ComesBackWrongAt = 7;

    /// <summary>
    /// What the policy is worth at the moment you try to remember something. The Premium tier is the one
    /// that bought storage of the pattern rather than a wake-up (NebulaArc §2, second degree of fine
    /// print), so it is the one that helps here; uninsured, there was never a filing to reach for.
    /// Named, because the whole point of the dice homage is that the player watches the numbers add up.
    /// </summary>
    public static DiceModifier TierModifier(InsuranceTier tier) => tier switch
    {
        InsuranceTier.Premium => new DiceModifier("Premium — the pattern was kept", +1),
        InsuranceTier.Basic => new DiceModifier("Basic — the wake-up was covered, nothing else", 0),
        _ => new DiceModifier("Uninsured — nobody filed anything for you", -1),
    };

    /// <summary>The seed for one row's one try. Salted by the thread (so two universes never agree), the
    /// entry (so two pages never agree) and the LIFE (so the next captain gets their own answer at the same
    /// page) — the three things the owner's ruling makes the roll about.</summary>
    public static ulong SeedFor(string threadId, string entryId, int life) =>
        DiceRule.Seed($"flashback|{threadId}|{entryId}", life);

    /// <summary>Roll a grey page, on the one shared engine. Deterministic: the same thread, page and life
    /// always settle the same way, so a reload can never re-roll a refusal into a recollection.</summary>
    public static DicePool Roll(string threadId, string entryId, int life, InsuranceTier tier) =>
        DiceRule.RollPool(SeedFor(threadId, entryId, life), 2, 6, [TierModifier(tier)]);

    /// <summary>Read the roll. Ties go upward at each rung — the ladder is written as "at or over".</summary>
    public static Outcome Read(DicePool pool) => pool.Total switch
    {
        >= ComesBackAt => Outcome.CameBack,
        >= ComesBackWrongAt => Outcome.CameBackWrong,
        _ => Outcome.Nothing,
    };

    /// <summary>What the captain is told. Three sentences, and the middle one never says WHICH detail
    /// moved — a flashback that came with a footnote would not be a flashback.</summary>
    public static string Toast(Outcome outcome) => outcome switch
    {
        Outcome.CameBack => "It comes back — the page warms.",
        Outcome.CameBackWrong =>
            "It comes back — but not like this. One thing on the page is not how you remember it.",
        _ => "Nothing. The page stays somebody else's.",
    };

    /// <summary>The nerve a wrong page costs, in whole pips. The only nerve this lane spends (#226's sanity
    /// seam; owner ruling 2026-08-23 §4).</summary>
    public const int WrongPageNervePips = 1;

    /// <summary>The house-voice label that pip is filed under in the nerve ledger, so a captain reading
    /// "what broke you?" afterwards can see this among the causes.</summary>
    public const string WrongPageNerveLabel = "a page that is not how you remember it";

    // ── THE LIE ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What one wrong recollection did: which detail moved, what the page really said, and what it
    /// says now. <see cref="FilingLine.Detail.None"/> when the page had nothing a person could misremember
    /// one of, in which case the recollection is honest despite the roll — the degrade is stated rather
    /// than faked.</summary>
    public readonly record struct Alteration(FilingLine.Detail Which, string Original, string Substitute);

    /// <summary>
    /// Move exactly one detail on <paramref name="page"/>, deterministically, drawing the replacement from
    /// the rest of the captain's own book.
    ///
    /// <para>The three candidates are the ones <see cref="FilingLine.Detail"/> names, harvested from the
    /// page's own fields: the first NUMBER in its lines (or, failing that, its title), and — only when the
    /// provenance really is the ledger's "&lt;who&gt; · &lt;where&gt; · day N" idiom — the NAME and the
    /// PLACE. A kind is only a candidate when the book can supply a donor of the same kind that differs
    /// from what the page says; a number can always fall back on arithmetic, so a page with a number in it
    /// always has something to lose.</para>
    /// </summary>
    public static Alteration Alter(LedgerPage page, IReadOnlyList<LedgerPage> book, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(book);

        var candidates = new List<Alteration>();

        if (FirstNumber(page) is { Length: > 0 } number)
        {
            string swap = PickDonor(Numbers(book, page.Id), number, seed) ?? Nudge(number, seed);
            if (!string.Equals(swap, number, StringComparison.Ordinal))
            {
                candidates.Add(new Alteration(FilingLine.Detail.Number, number, swap));
            }
        }

        string[] prov = ProvenanceFields(page.Provenance);
        if (prov.Length >= 3)
        {
            if (PickDonor(ProvenanceField(book, page.Id, 0), prov[0], seed) is { } who)
            {
                candidates.Add(new Alteration(FilingLine.Detail.Name, prov[0], who));
            }

            if (PickDonor(ProvenanceField(book, page.Id, 1), prov[1], seed) is { } where)
            {
                candidates.Add(new Alteration(FilingLine.Detail.Place, prov[1], where));
            }
        }

        if (candidates.Count == 0)
        {
            return new Alteration(FilingLine.Detail.None, "", "");
        }

        return candidates[(int)(seed % (ulong)candidates.Count)];
    }

    /// <summary>
    /// The page as the captain now remembers it. Exactly one occurrence of exactly one field is replaced,
    /// so everything else on the row is byte-for-byte what it always was — which is what makes the lie
    /// worth finding rather than obvious.
    /// </summary>
    public static LedgerPage Apply(LedgerPage page, Alteration lie)
    {
        if (lie.Which == FilingLine.Detail.None || lie.Original.Length == 0)
        {
            return page;
        }

        if (lie.Which is FilingLine.Detail.Name or FilingLine.Detail.Place)
        {
            string[] fields = ProvenanceFields(page.Provenance);
            int at = lie.Which == FilingLine.Detail.Name ? 0 : 1;
            if (fields.Length < 3 || !string.Equals(fields[at], lie.Original, StringComparison.Ordinal))
            {
                return page; // the row no longer says what the lie was written against — leave it alone
            }

            fields[at] = lie.Substitute;
            return page with { Provenance = string.Join(" · ", fields) };
        }

        // A number: the first occurrence, scanning the lines in order and then the title — the same order,
        // and the same two fields, the candidate was harvested from, so the two can never point at different
        // text.
        var lines = new List<string>(page.Lines ?? []);
        for (int i = 0; i < lines.Count; i++)
        {
            int at = lines[i].IndexOf(lie.Original, StringComparison.Ordinal);
            if (at >= 0)
            {
                lines[i] = Splice(lines[i], at, lie.Original.Length, lie.Substitute);
                return page with { Lines = lines };
            }
        }

        int inTitle = (page.Title ?? "").IndexOf(lie.Original, StringComparison.Ordinal);
        if (inTitle >= 0)
        {
            return page with { Title = Splice(page.Title!, inTitle, lie.Original.Length, lie.Substitute) };
        }

        return page;
    }

    // ── Harvesting ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The provenance line's fields, when it is the ledger's "&lt;who&gt; · &lt;where&gt; · day N"
    /// idiom. Anything else ("standing note", "logged 2h ago") comes back as one field and names nobody, so
    /// only a number on such a row can move.</summary>
    private static string[] ProvenanceFields(string? provenance) =>
        string.IsNullOrWhiteSpace(provenance)
            ? []
            : provenance!.Split(" · ", StringSplitOptions.None);

    /// <summary>
    /// The first run of digits (with the separators a written number carries) on the page, scanning its lines
    /// in order and then its title.
    ///
    /// <para>The PROVENANCE is deliberately not searched. Half the ledger's provenance lines are an AGE
    /// (<c>LedgerClock.Age</c> — "logged 2h ago"), which is a number that changes every minute the game runs;
    /// a lie written against one would stop matching the page within the hour and quietly become a no-op. The
    /// two provenance fields that ARE stable — who and where — have their own detail kinds.</para>
    /// </summary>
    private static string? FirstNumber(LedgerPage page)
    {
        foreach (string line in page.Lines ?? [])
        {
            if (NumberIn(line) is { } n)
            {
                return n;
            }
        }

        return NumberIn(page.Title ?? "");
    }

    private static string? NumberIn(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsAsciiDigit(text[i]))
            {
                continue;
            }

            int start = i;
            int end = i;
            while (end < text.Length
                   && (char.IsAsciiDigit(text[end])
                       || ((text[end] is ',' or '.') && end + 1 < text.Length && char.IsAsciiDigit(text[end + 1]))))
            {
                end++;
            }

            return text[start..end];
        }

        return null;
    }

    /// <summary>Every number the rest of the book wrote down.</summary>
    private static List<string> Numbers(IReadOnlyList<LedgerPage> book, string exceptId)
    {
        var found = new List<string>();
        foreach (LedgerPage p in book)
        {
            if (string.Equals(p.Id, exceptId, StringComparison.Ordinal))
            {
                continue;
            }

            if (FirstNumber(p) is { Length: > 0 } n)
            {
                found.Add(n);
            }
        }

        return found;
    }

    /// <summary>Every name (field 0) or place (field 1) the rest of the book attributes something to.</summary>
    private static List<string> ProvenanceField(IReadOnlyList<LedgerPage> book, string exceptId, int at)
    {
        var found = new List<string>();
        foreach (LedgerPage p in book)
        {
            if (string.Equals(p.Id, exceptId, StringComparison.Ordinal))
            {
                continue;
            }

            string[] fields = ProvenanceFields(p.Provenance);
            if (fields.Length >= 3 && fields[at].Length > 0)
            {
                found.Add(fields[at]);
            }
        }

        return found;
    }

    /// <summary>Pick one donor that differs from what the page says, deterministically. Null when the book
    /// has nothing of that kind to lend — which is how a kind stops being a candidate.</summary>
    private static string? PickDonor(List<string> donors, string original, ulong seed)
    {
        var usable = new List<string>();
        foreach (string d in donors)
        {
            if (!string.Equals(d, original, StringComparison.Ordinal) && !usable.Contains(d, StringComparer.Ordinal))
            {
                usable.Add(d);
            }
        }

        usable.Sort(StringComparer.Ordinal); // the book's order is a render order; the lie must not depend on it
        return usable.Count == 0 ? null : usable[(int)(seed % (ulong)usable.Count)];
    }

    /// <summary>The number a page with no donor misremembers itself as: the same shape, the digits moved.
    /// Guaranteed to differ from the original, and never to grow a digit or lose one, so "764 cr" becomes
    /// another three-figure sum rather than something obviously wrong.</summary>
    private static string Nudge(string number, ulong seed)
    {
        var sb = new StringBuilder(number.Length);
        int step = (int)(seed % 8UL) + 1; // 1..8 — never 0 and never 9's identity under mod 10
        bool moved = false;
        for (int i = number.Length - 1; i >= 0; i--)
        {
            char c = number[i];
            if (!moved && char.IsAsciiDigit(c))
            {
                sb.Insert(0, (char)('0' + ((c - '0' + step) % 10)));
                moved = true;
            }
            else
            {
                sb.Insert(0, c);
            }
        }

        return sb.ToString();
    }

    private static string Splice(string text, int at, int length, string with) =>
        string.Concat(text.AsSpan(0, at), with, text.AsSpan(at + length));

    // ── #973 L3 · NOT ANYONE'S — THE STRAY MEMORIES ──────────────────────────────────────────────────
    //
    // The fourth thing a grey page can do, and it is a rarity hidden inside the WORST outcome rather than
    // beside the best. Reading a page that stays somebody else's is the one result that gives the captain
    // nothing; once in a long while what arrives in that silence is a page that was never theirs at all —
    // a corridor they never walked, a name they turned for, rain.
    //
    // Where they come from is never said and never will be. The arc's whole discipline (NebulaLore, the
    // Reever law) is that no surface names the thing; a stray memory is EVIDENCE of the archive and not a
    // statement about it, which is why every one of these six is a domestic detail and not one of them
    // mentions a policy, a clinic or a file. Three of them laid together do say something, and that is the
    // one place this ever becomes an argument (`SpreadReconcile` → the-bleed).
    //
    // THREE CADENCE RULES, all of them the owner's shape:
    //   · never the FIRST grey page read in a life. The first one has to teach the ordinary three outcomes,
    //     and a rarity that can land on the tutorial is a rarity the player reads as the rule.
    //   · at most one NOTHING in four. A quarter of the worst outcome, which over a whole run of a long
    //     universe is a handful of them — and the sweep test holds the ratio rather than trusting the die.
    //   · without replacement, per thread. A stray that came twice would stop being a memory and start
    //     being a message, and the six of them are a set that is meant to be completed, not farmed.

    /// <summary>The nerve a stray costs, in whole pips — the same price a page that came back wrong asks,
    /// and for the same reason (#226's sanity seam; owner ruling 2026-08-23 §4). A memory that fits nobody's
    /// life is not free to hold.</summary>
    public const int StrayNervePips = 1;

    /// <summary>The house-voice label that pip is filed under, so "what broke you?" can say so afterwards.</summary>
    public const string StrayNerveLabel = "a memory that is not anyone's";

    /// <summary>How many <i>nothing</i>s in this many carry a stray instead. One in four — the rarity is
    /// arithmetic on the shared dice rather than a probability nobody can test. FLAGGED for the owner's
    /// tuning.</summary>
    public const int StrayInNothings = 4;

    /// <summary>
    /// THE SIX. Fable's, verbatim (#973 L3), in authored order — the order is part of the identity of a
    /// draw, because the id a drawn stray rides under is its index here.
    /// </summary>
    public static IReadOnlyList<string> Strays { get; } =
    [
        "A corridor with the lights on the floor instead of the ceiling. You walked it barefoot. You have "
        + "never been barefoot on a ship in your life.",

        "Somebody calling a name down a stairwell, and you turning, because it was yours. It is not yours.",

        "The taste of a drink you have never liked, and liking it.",

        "A child's drawing of a ship taped inside a locker door. You do not know the ship. You know the tape.",

        "Rain on a hull. Rain. You have never once been anywhere it rains.",

        "A hand over yours on a throttle, teaching. You were the one teaching.",
    ];

    /// <summary>The sheet id one stray rides under in the black book. Keyed by the INDEX rather than by the
    /// page that produced it, which is what makes "never the same stray twice" a question the book can answer
    /// by itself — the drawn set is the sheets already in it, and nothing extra has to be persisted.</summary>
    public static string StrayId(int index) => $"stray:{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>Is this a stray's sheet id?</summary>
    public static bool IsStrayId(string? id) =>
        id is not null && id.StartsWith("stray:", StringComparison.Ordinal);

    /// <summary>
    /// DOES A STRAY COME INSTEAD OF THE NOTHING? Asked only when <see cref="Read"/> has already settled on
    /// <see cref="Outcome.Nothing"/>, so it can never take a recollection away from a captain who earned one.
    /// </summary>
    /// <param name="threadId">The universe.</param>
    /// <param name="entryId">The page being read at — so two pages in one life never agree.</param>
    /// <param name="life">Which captain (<c>Retired.Count + 1</c>), so the next one gets their own answer.</param>
    /// <param name="greyPagesReadBefore">How many grey pages this LIFE has already been read at. Zero is the
    /// first read of a life and is never a stray.</param>
    public static bool AStrayComesInstead(string threadId, string entryId, int life, int greyPagesReadBefore) =>
        greyPagesReadBefore >= 1
        && DiceRule.Roll(DiceRule.Seed($"flashback|stray|{threadId}|{entryId}", life), StrayInNothings).Face == 1;

    /// <summary>
    /// WHICH ONE. Drawn without replacement from the six: the ids already in the book are struck off, and one
    /// of the rest is taken deterministically off the shared dice. Null once the thread has all six — a
    /// seventh stray is not a thing that exists, and the roll silently falls back to the ordinary nothing.
    /// </summary>
    /// <param name="alreadyDrawn">The stray sheet ids the book already holds.</param>
    /// <param name="seed">The same seed the outcome was settled on, so a reload draws the same page.</param>
    public static int? DrawStray(IEnumerable<string> alreadyDrawn, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(alreadyDrawn);

        var held = new HashSet<string>(alreadyDrawn, StringComparer.Ordinal);
        var left = new List<int>(Strays.Count);
        for (int i = 0; i < Strays.Count; i++)
        {
            if (!held.Contains(StrayId(i)))
            {
                left.Add(i);
            }
        }

        return left.Count == 0 ? null : left[(int)(seed % (ulong)left.Count)];
    }

    /// <summary>What the captain is told when one arrives: the sheet's FIRST SENTENCE and nothing else. The
    /// rest of it is on the sheet, in the book, where a memory belongs — and a toast that read out the whole
    /// page would be the pulse doing the black book's job for it.</summary>
    public static string StrayToast(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int stop = text.IndexOf(". ", StringComparison.Ordinal);
        return stop >= 0 ? text[..(stop + 1)] : text;
    }
}
