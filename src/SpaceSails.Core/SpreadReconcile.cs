namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L3 · THE SPREAD'S RECONCILE — two papers laid together, and what the table says about them.
//
// #784 built the SPREAD as a place to WORK a find into the book: sit down, dig, file, bin. This is the
// other verb a table full of paper is for, and the owner asked for it in the same breath as the black
// book itself — lay two things side by side and see whether they are talking about the same world.
//
// THREE ANSWERS, AND ONLY THREE. They are not a ladder and there is no failure among them:
//
//   AGREE              both papers name the same thing, place or person. The memory gains goodwill —
//                      one point of CONFIDENCE — because a page a second piece of paper corroborates
//                      is a page you can lean on.
//   DISAGREE           one of them is a page that came back WRONG (#974 kept the hidden original,
//                      unread by any surface, for exactly this moment) and the other one says what it
//                      really said. The SPREAD puts the original back and marks the memory corrected.
//   NAMES NO DOCUMENT  a memory that names nothing the book has. Not a failure: a lead. It goes onto
//                      the THREADS page under its own heading and waits for the paper that answers it.
//
// AND THE ONE ARGUMENT THE TABLE IS EVER ALLOWED TO MAKE. Three sheets marked NOT ANYONE'S, laid
// together, agreeing with each other — that is the lattice, and it assembles the NEBULA fragment
// `the-bleed`. It is the only place in the arc where the captain's own filing produces a shard, which is
// why it takes three of the rarest thing in the game rather than a button.
//
// NOTHING IN THIS FILE NAMES THE THING. Every sentence here is Fable's, verbatim; not one of them says
// what the archive is, and the three that could have (the strays themselves) are about rain and a
// stairwell and a hand on a throttle.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 L3 · Laying two papers together on the SPREAD: what a pair settles into, the words the
/// table says, and the one arrangement that assembles a NEBULA shard. Pure and total — the client lays
/// the papers, applies the result to its two books, and decides nothing.</summary>
public static class SpreadReconcile
{
    /// <summary>What a laid pair settles into. Three, and the list is closed.</summary>
    public enum Verdict
    {
        /// <summary>They name the same thing. The memory warms.</summary>
        Agree = 0,

        /// <summary>One of them is lying, and the other one is holding the proof.</summary>
        Disagree = 1,

        /// <summary>Nothing in the book answers this one. Yet.</summary>
        NamesNoDocument = 2,
    }

    /// <summary>Which book a paper came out of. Only two, because there are only two: what somebody
    /// REMEMBERS and what somebody WROTE DOWN.</summary>
    public enum Kind
    {
        /// <summary>A held-memory sheet — the photograph, the fleet-day page, a slip, the signing, a stray.</summary>
        Memory = 0,

        /// <summary>A page of the Captain's ledger or an entry of the field book. A document.</summary>
        Document = 1,
    }

    /// <summary>
    /// One thing lying on the table.
    /// </summary>
    /// <param name="Id">The sheet id or the ledger entry id — whichever book it came out of.</param>
    /// <param name="Kind">Which book that was.</param>
    /// <param name="Label">What the row calls it on the page.</param>
    /// <param name="Text">Everything it says, joined — what a disagreement is looked for in.</param>
    /// <param name="Names">The names it writes down. A memory's <see cref="HeldMemory.Sheet.Threads"/>.</param>
    /// <param name="Tag">Money or love. Meaningless for a document, and never read for one.</param>
    /// <param name="Stray">Is this a memory marked <i>not anyone's</i>? The lattice's own question.</param>
    /// <param name="Which">Which detail on this paper was moved by a wrong recollection, if any.</param>
    /// <param name="Original">What it really said, kept hidden by #974 until this table asks.</param>
    public readonly record struct Paper(
        string Id,
        Kind Kind,
        string Label,
        string Text,
        IReadOnlyList<string> Names,
        HeldMemory.Theory Tag = HeldMemory.Theory.Money,
        bool Stray = false,
        FilingLine.Detail Which = FilingLine.Detail.None,
        string Original = "")
    {
        /// <summary>Does this paper carry a moved detail — a page that came back wrong and has an original
        /// somebody could catch it with?</summary>
        public bool IsLying => Which != FilingLine.Detail.None && Original.Length > 0;

        /// <summary>A held-memory sheet, laid down.</summary>
        public static Paper Of(HeldMemory.Sheet sheet, string label) =>
            new(sheet.Id, Kind.Memory, label, sheet.Text, sheet.Threads ?? [], sheet.Tag, sheet.IsStray);

        /// <summary>A page of the ledger, laid down — carrying whatever the filing line knows about it, so a
        /// page that came back wrong arrives at the table with its hidden original still hidden.</summary>
        public static Paper Of(LedgerPage page, FilingLine.Page standing, IReadOnlyList<string> names) =>
            new(page.Id, Kind.Document, page.Title,
                string.Join(" ", [page.Title ?? "", .. page.Lines ?? [], page.Provenance ?? ""]),
                names, HeldMemory.Theory.Money, Stray: false,
                standing.WasAltered ? standing.Which : FilingLine.Detail.None,
                standing.WasAltered ? standing.Original : "");
    }

    /// <summary>
    /// What the table did.
    /// </summary>
    /// <param name="Verdict">Which of the three.</param>
    /// <param name="Line">What it says. Fable's words, one per verdict.</param>
    /// <param name="CorrectedId">On a DISAGREE, the paper that was lying — the ledger page whose hidden
    /// original goes back, and the sheet (if the book holds one under that id) that is marked corrected.
    /// Empty otherwise.</param>
    /// <param name="Restored">On a DISAGREE, what the page really said, so the caller does not have to reach
    /// back into the paper for it. Empty otherwise.</param>
    /// <param name="Corroborated">On an AGREE, every MEMORY in the pair — each gains one confidence.</param>
    /// <param name="LeadId">On a NAMES-NO-DOCUMENT, the memory that named nothing. It goes onto THREADS under
    /// <see cref="NotAnyonesYet"/> and waits. Empty otherwise.</param>
    /// <param name="Money">How many money sheets are in the laid pair — the second question.</param>
    /// <param name="Love">…and how many love ones.</param>
    public readonly record struct Result(
        Verdict Verdict,
        string Line,
        string CorrectedId,
        string Restored,
        IReadOnlyList<string> Corroborated,
        string LeadId,
        int Money,
        int Love)
    {
        /// <summary>The second question, under the verdict: <i>which theory do they serve</i>, answered by
        /// counting rather than by concluding. The book never says which theory is right.</summary>
        public string SecondQuestion =>
            $"{TheSecondQuestion} — {Money} {HeldMemory.Label(HeldMemory.Theory.Money)}"
            + $" · {Love} {HeldMemory.Label(HeldMemory.Theory.Love)}";
    }

    // ── THE WORDS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Both papers are talking about the same world.</summary>
    public const string AgreeLine = "They agree. The page warms a little more.";

    /// <summary>One of them is not, and the other one can prove it.</summary>
    public const string DisagreeLine =
        "They do not agree. One of them is lying, and paper does not lie on its own.";

    /// <summary>The memory names something the book has never heard of.</summary>
    public const string NamesNoDocumentLine = "Nothing in the book names this. It is not anyone's — yet.";

    /// <summary>The heading a lead waits under on the THREADS page — the second half of the line above,
    /// used as a thread name so the sheet stacks with the others rather than sitting in a special box.</summary>
    public const string NotAnyonesYet = "not anyone's — yet";

    /// <summary>The SPREAD's second question, in Fable's own words. The COUNT beside it is engine voice and
    /// is assembled in <see cref="Result.SecondQuestion"/>; the question itself is written once, here.</summary>
    public const string TheSecondQuestion = "which theory do they serve";

    /// <summary>What the table says for a verdict.</summary>
    public static string Line(Verdict verdict) => verdict switch
    {
        Verdict.Agree => AgreeLine,
        Verdict.Disagree => DisagreeLine,
        _ => NamesNoDocumentLine,
    };

    // ── THE RECONCILE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>The most confidence one sheet can hold. A cap rather than an open counter: goodwill for a
    /// piece of paper is a small feeling, and a number that could run to forty would invite farming one
    /// corroboration over and over. FLAGGED for the owner's tuning.</summary>
    public const int MostConfidence = 5;

    /// <summary>
    /// #973 · <b>AN AUTHORED REVEAL — the hook a lane with two specific papers in mind plugs into.</b>
    ///
    /// <para>The three verdicts below are GENERAL rules about any two papers. A lane sometimes knows
    /// something particular instead: that <i>these</i> two, laid together, finish a sentence somebody left
    /// unfinished, or say out loud that two hands are one hand. That is not a rule about paper, it is a fact
    /// about a story, and it cannot be derived from names and numbers — so it arrives as a function and is
    /// asked FIRST, before any of the general rules get a chance to call it an ordinary agreement.</para>
    ///
    /// <para>Return null for a pair this lane has nothing authored about, which is almost every pair; return
    /// a <see cref="Result"/> (built with <see cref="Reveals"/>) for the ones it does. #973 L5b's walk-in
    /// is the first caller: her note beside the fleet-day page or the job's first slip, and her note beside
    /// any money-tagged old-crew slip.</para>
    /// </summary>
    public delegate Result? Reveal(Paper a, Paper b);

    /// <summary>
    /// Build a reveal's answer without having to fill in the counting. The money/love tally is the SPREAD's
    /// second question and belongs to the table rather than to whoever wrote the line, so it is computed
    /// here from the two papers and never passed in.
    /// </summary>
    /// <param name="verdict">Which of the three this reveal reads as. <see cref="Verdict.Disagree"/> for a
    /// reveal that corrects a sheet; <see cref="Verdict.Agree"/> for one that corroborates.</param>
    /// <param name="line">What the table says. The reveal's own authored words, not one of the three below.</param>
    /// <param name="a">The first paper laid.</param>
    /// <param name="b">The second.</param>
    /// <param name="correctedId">The sheet this reveal marks <i>corrected</i>, or empty.</param>
    /// <param name="corroborated">The sheets this reveal warms, or empty.</param>
    /// <param name="leadId">The sheet this reveal leaves waiting on THREADS, or empty.</param>
    public static Result Reveals(
        Verdict verdict,
        string line,
        Paper a,
        Paper b,
        string correctedId = "",
        IReadOnlyList<string>? corroborated = null,
        string leadId = "")
    {
        (int money, int love) = Tally(a, b);
        return new Result(verdict, line, correctedId, "", corroborated ?? [], leadId, money, love);
    }

    /// <summary>
    /// LAY THEM TOGETHER. The order of the questions is the design and not an accident:
    ///
    /// <para>An AUTHORED REVEAL first, because it knows something about these two papers that no general
    /// rule can derive. Then a DISAGREEMENT, because it is the only one of the three general answers with
    /// physical evidence behind it — a hidden original the captain has never seen, kept by #974 for this
    /// table — and a pair that both names a person AND catches a lie about them is a pair whose lie is the
    /// interesting half. Then AGREEMENT, which is a shared name (or, for two pages that are not anyone's, a
    /// shared tag — the only kind of agreement a memory naming nobody can have). Anything left is a memory
    /// the book cannot answer, which is a lead and never a failure.</para>
    /// </summary>
    public static Result Lay(Paper a, Paper b, Reveal? reveal = null)
    {
        if (reveal?.Invoke(a, b) is { } authored)
        {
            return authored;
        }

        (int money, int love) = Tally(a, b);

        if (TheLie(a, b) is { } lie)
        {
            return new Result(Verdict.Disagree, DisagreeLine, lie.Id, lie.Original, [], "", money, love);
        }

        if (TheyAgree(a, b))
        {
            var warmed = new List<string>(2);
            if (a.Kind == Kind.Memory)
            {
                warmed.Add(a.Id);
            }

            if (b.Kind == Kind.Memory)
            {
                warmed.Add(b.Id);
            }

            return new Result(Verdict.Agree, AgreeLine, "", "", warmed, "", money, love);
        }

        // The lead is the MEMORY of the pair — a document naming nothing a memory names is just another
        // document, and the book has no opinion about those. With two memories the first one is the lead,
        // because the laid pair is read left to right on the page it was laid on.
        string lead = a.Kind == Kind.Memory ? a.Id : b.Kind == Kind.Memory ? b.Id : "";
        return new Result(Verdict.NamesNoDocument, NamesNoDocumentLine, "", "", [], lead, money, love);
    }

    /// <summary>The paper that is lying, when the other one is holding what it really said. Null when
    /// neither carries a moved detail, or when nothing on the table contradicts the one that does.</summary>
    private static Paper? TheLie(Paper a, Paper b)
    {
        if (a.IsLying && Contradicts(b, a.Original))
        {
            return a;
        }

        return b.IsLying && Contradicts(a, b.Original) ? b : null;
    }

    /// <summary>Does this paper say what the other one really said? Its own words or one of the names it
    /// writes down — a page whose moved detail was a NAME is caught by the sheet that names that person, and
    /// one whose moved detail was a number is caught by the document that carries the number.</summary>
    private static bool Contradicts(Paper paper, string original)
    {
        if (original.Length == 0)
        {
            return false;
        }

        if ((paper.Text ?? "").Contains(original, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string name in paper.Names ?? [])
        {
            if (string.Equals(name, original, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Do they name the same thing, place or person?
    ///
    /// <para>Two kinds of agreement, and the second is the whole reason the lattice can ever assemble. The
    /// ordinary kind is a shared NAME. The other is two pages that are NOT ANYONE'S and carry the same tag:
    /// a memory that fits nobody's life names nobody by construction, so a rule that only knew about names
    /// would have made the strays permanently unable to agree with anything — including each other, which is
    /// the one thing the arc needs them to be able to do.</para>
    /// </summary>
    public static bool TheyAgree(Paper a, Paper b)
    {
        if (a.Stray && b.Stray)
        {
            return a.Tag == b.Tag;
        }

        foreach (string name in a.Names ?? [])
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            foreach (string other in b.Names ?? [])
            {
                if (string.Equals(name, other, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // …and a name one paper WRITES DOWN is answered by a document that merely says it. The photograph
            // names four faces; the customs receipt names one of them in a sentence, and that is the same
            // agreement said in two different registers.
            if (b.Kind == Kind.Document && (b.Text ?? "").Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (string name in b.Names ?? [])
        {
            if (!string.IsNullOrWhiteSpace(name)
                && a.Kind == Kind.Document
                && (a.Text ?? "").Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static (int Money, int Love) Tally(Paper a, Paper b)
    {
        int money = 0;
        int love = 0;
        foreach (Paper p in new[] { a, b })
        {
            if (p.Kind != Kind.Memory)
            {
                continue;
            }

            if (p.Tag == HeldMemory.Theory.Love)
            {
                love++;
            }
            else
            {
                money++;
            }
        }

        return (money, love);
    }

    // ── THE LATTICE ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many pages that fit nobody's life have to be on the table at once before they start
    /// talking to each other. Three, and the number is the fragment's own prose — <i>a corridor, a name, a
    /// glass</i> — so it is not a knob that can be turned without rewriting a shard.</summary>
    public const int StraysForTheBleed = 3;

    /// <summary>The NEBULA fragment three agreeing strays assemble. Named here as well as in the pool so the
    /// table and the arc bind to one string rather than two literals.</summary>
    public const string TheBleedId = "the-bleed";

    /// <summary>
    /// THREE THAT AGREE. Every paper on the table is a memory marked <i>not anyone's</i>, there are at least
    /// <see cref="StraysForTheBleed"/> of them, and they all serve the same theory — which is the only kind of
    /// agreement a page naming nobody can have (<see cref="TheyAgree"/>). Two never do it; a stray beside a
    /// document never does it; and neither does a table with a real memory mixed in, because the whole point
    /// of the shard is that NOTHING on the table belongs to anybody.
    /// </summary>
    public static bool TheBleedAssembles(IReadOnlyList<Paper> laid)
    {
        ArgumentNullException.ThrowIfNull(laid);
        if (laid.Count < StraysForTheBleed)
        {
            return false;
        }

        HeldMemory.Theory tag = laid[0].Tag;
        foreach (Paper p in laid)
        {
            if (p.Kind != Kind.Memory || !p.Stray || p.Tag != tag)
            {
                return false;
            }
        }

        return true;
    }
}
