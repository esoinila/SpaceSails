namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L1 · THE FILING LINE — the amnesia is the fine print.
//
// docs/features/NebulaArc.md §2, first degree of fine print: a resurrection is not a restoration. What
// wakes at the clinic is spun off your LAST-FILED PATTERN, and the filing is a thing with a date on it —
// the sim time your premium was paid through (`PirateInsurance.PremiumPaidThroughSimTime`, in the world
// since #227). So a captain who dies remembers the book up to that date and nothing after it, and the
// policy's real product turns out to be continuity of memory: not "we bring you back", but "how much of
// you comes back".
//
// The rule is one comparison and it is the whole mechanic:
//
//     a page dated AFTER the line is a page you don't remember writing.
//
// Uninsured, the line is negative infinity and the whole book is grey (owner ruling 2026-08-23 §2:
// "uninsured wake with a blank ledger — true to the poster"). Covered and paid up, and almost nothing is,
// because the money reached past the day you died.
//
// WHICH POLICIES FILE, AND UP TO WHEN — and the one place this lane had to choose. The lane brief said
// "PremiumPaidThroughSimTime of the policy IN FORCE at death, or −∞ if Uninsured OR LAPSED". Written that
// way the line can never cut a book in half: `PirateInsurance.IsActiveAt` is `simTime <= paid-through`, so
// a policy in force at the death has its paid-through at or AFTER the death and nothing is dated past it,
// while a lapsed one files nothing at all. Every rebirth would be all of the book or none of it, and the
// wake card's own insured sentence — *"The pages come back to where the premium was paid. After that line,
// the book is grey"* — would be describing a grey part that could not exist.
//
// So the line asks the question that sentence asks: WAS ANYTHING EVER FILED, and up to when. A real tier
// filed you through the date the premium reached (in the future if you were paid up when you died, in the
// past if you let it run out — and the pages after that date are the ones you lose). No tier at all filed
// nothing, and `PirateInsurance.Uninsured` already carries negative infinity in that field, so the
// uninsured case needs no special number — only the tier check.
//
// `IsActiveAt` is NOT forked and NOT weakened: it goes on being the only answer to "does this policy PAY",
// which is `InsuranceRule.ApplyToRebirth`'s question at this same seam and a different question from this
// one. Two places computing one fact is the bug even while they agree (#621, one file over); two questions
// with two answers is not. FLAGGED for the owner — the alternative is one line here.
//
// WHAT IS NOT SAID ANYWHERE IN THIS FILE, by law (the same law the Reever origin keeps): the word for
// what the clinic actually does. The rep says continuity; the ledger says pages you don't remember; no
// surface in the game ever names the thing itself. `NoGameTextNamesTheThingTests` holds that.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 L1 · The filing line: where a rebirth's memory stops, which pages of the Captain's ledger
/// are drawn as somebody else's, and the two sentences the wake card says about it. Pure and total — the
/// client marks the book with it at the succession seam and the vault carries the marks.</summary>
public static class FilingLine
{
    // ── §1 · THE LINE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The sim time this captain was last filed through: the date the premium reached, for any real tier —
    /// and <see cref="double.NegativeInfinity"/> for a captain who never held a policy, because nothing was
    /// ever filed for them. See the header for why this is "was anything filed" rather than
    /// <see cref="PirateInsurance.IsActiveAt"/>'s "does this policy pay".
    ///
    /// <para>No special number is needed for the uninsured case:
    /// <see cref="PirateInsurance.Uninsured"/> already carries negative infinity in that field. The tier
    /// check is belt to that braces, and it is what makes a hand-built <c>new(None, 500)</c> file nothing
    /// rather than quietly file everything.</para>
    /// </summary>
    public static double At(PirateInsurance policy) =>
        policy.Tier == InsuranceTier.None ? double.NegativeInfinity : policy.PremiumPaidThroughSimTime;

    /// <summary>Is this page dated after the line — i.e. written by somebody the new captain has no pattern
    /// of? A page written exactly ON the line was filed and comes back.</summary>
    public static bool IsAfterTheLine(double line, double entrySimTime) => entrySimTime > line;

    /// <summary>
    /// #973 L5a · <b>THE ONE PAGE THE LINE CANNOT TAKE.</b> Owner ruling 2026-08-23 §13, and the joke has
    /// teeth: a page the SERVICE filed comes back whatever the policy did, because the filing that preserved
    /// it was not the captain's and was not paid for by him. There is one in the game — the summer-party
    /// page, preserved perfectly because it was written up as a report against him — and this is the law
    /// that keeps it, stated where the greying is decided rather than special-cased at a renderer.
    ///
    /// <para>It is deliberately a property of the PAGE and not a list of ids kept here. A rule that named the
    /// exempt row by key would be a second place that has to agree with the world about what that row is,
    /// and this house keeps a table of the bugs that shape produces.</para>
    /// </summary>
    public static bool TheServiceFiledIt(LedgerPage page) => page.Filed;

    /// <summary>Does this page grey for a captain whose memory stops at <paramref name="line"/>? Dated after
    /// the line and not filed by the service — both halves, and it is the only place the question is asked.</summary>
    public static bool Greys(double line, LedgerPage page) =>
        !TheServiceFiledIt(page) && IsAfterTheLine(line, page.SimTime);

    // ── §2 · HOW IT READS ────────────────────────────────────────────────────────────────────────────

    /// <summary>The mark beside a page you don't remember writing. A glyph rather than a colour, because a
    /// greyed row on a dark ledger is not a difference everybody can see.</summary>
    public const string Mark = "⟂";

    /// <summary>What the mark means, spelled out on the row itself.</summary>
    public const string Label = "a page you don't remember writing";

    /// <summary>The wake-up notice for a captain who died with no policy in force. One line, on the
    /// succession card the rebirth already raises — this lane adds no card of its own.</summary>
    public const string UninsuredWake =
        "Every page before today is in a hand you do not recognise. It is yours.";

    /// <summary>The wake-up notice for a captain whose premium was paid through some date. The book comes
    /// back as far as the money went and no further.</summary>
    public const string InsuredWake =
        "The pages come back to where the premium was paid. After that line, the book is grey.";

    /// <summary>The one line the succession card says about the filing: whether anything was filed for this
    /// captain at all. Decided by the LINE and never by how many rows happened to grey — a captain covered
    /// through next month and a captain whose premium ran out on day four are both told the same true thing
    /// about where their book stops, and only a captain nobody ever filed for is told the other one.</summary>
    public static string WakeNotice(double line) =>
        double.IsNegativeInfinity(line) ? UninsuredWake : InsuredWake;

    /// <summary>The same sentence, asked of the policy directly.</summary>
    public static string WakeNotice(PirateInsurance policy) => WakeNotice(At(policy));

    // ── §3 · THE BOOK ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where one page of the ledger stands with the captain now reading it.</summary>
    public enum PageState
    {
        /// <summary>Filed, and therefore yours. The row draws as it always did.</summary>
        Remembered = 0,

        /// <summary>Dated after the line, and never yet read at. Grey, marked, clickable.</summary>
        Unremembered = 1,

        /// <summary>Read at this life and nothing came. Stays grey; no second roll until the next rebirth
        /// re-greys the book (the roll is salted by the life, so the next captain gets their own answer).</summary>
        Refused = 2,

        /// <summary>It came back. The row un-greys and is yours again.</summary>
        CameBack = 3,

        /// <summary>It came back wrong. The row un-greys with exactly one detail altered; the original is
        /// kept here, unread by any surface, so a later lane's SPREAD can lay the page beside a document and
        /// find the lie.</summary>
        CameBackWrong = 4,
    }

    /// <summary>Which detail on a page a wrong recollection can move. Three, and the list is closed: a
    /// NUMBER out of the page's own lines, and the two halves of the provenance line the ledger already
    /// writes as "&lt;who&gt; · &lt;where&gt; · day N" — the NAME who told you and the PLACE it was told.
    /// Nothing else on a page is a detail a person would misremember one of.</summary>
    public enum Detail
    {
        /// <summary>Nothing on this page could be moved — it came back whole.</summary>
        None = 0,

        /// <summary>A number in the page's own lines (or, failing that, its title). Never one out of the
        /// provenance: half of those are an AGE, and a lie written against a clock stops matching within
        /// the hour.</summary>
        Number = 1,

        /// <summary>Who the page says told you: the first field of the provenance line.</summary>
        Name = 2,

        /// <summary>Where the page says it happened: the second field of the provenance line.</summary>
        Place = 3,
    }

    /// <summary>
    /// One page's standing, as the vault carries it: which entry, where it stands, and — for a page that
    /// came back wrong — which detail was moved, what it really said, and what it says now.
    ///
    /// <para>The alteration is stored rather than recomputed. Recomputing the lie at render time would have
    /// made it a function of whatever else is in the book that minute, so a page would quietly change its
    /// story as the ledger grew; and the whole point of keeping <see cref="Original"/> is that there is a
    /// fixed thing for a later reconcile to disagree with.</para>
    /// </summary>
    public readonly record struct Page(
        string EntryId, PageState State, Detail Which, string Original, string Substitute)
    {
        /// <summary>A page that has not been filed against at all — the default for every row.</summary>
        public static Page Remembered(string entryId) => new(entryId, PageState.Remembered, Detail.None, "", "");

        /// <summary>Drawn grey, marked, and open to being read at.</summary>
        public bool IsGrey => State is PageState.Unremembered or PageState.Refused;

        /// <summary>May the captain roll on this row right now? Once per row per life: a refusal latches
        /// until the next rebirth re-greys the book.</summary>
        public bool MayBeRead => State == PageState.Unremembered;

        /// <summary>Does this page carry a moved detail — whatever its grey state is now?</summary>
        public bool WasAltered => Which != Detail.None && Original.Length > 0;

        /// <summary>The opaque row the vault stores. The house idiom (see <c>CaseThreads.Thread</c>,
        /// <c>Satchel.Item</c>): the file carries the FACT and never the sentences, so the words can be
        /// rewritten without invalidating a save. The pipe is escaped in every field but the last, so an
        /// original or a substitute containing one still round-trips.</summary>
        public string Stored =>
            $"{(int)State}|{(int)Which}|{Esc(EntryId)}|{Esc(Original)}|{Substitute}";

        /// <summary>Read one back. Anything this build cannot parse is dropped rather than thrown over —
        /// the vault is tolerant everywhere else, and a grey page is not worth losing a game for.</summary>
        public static bool TryParse(string? stored, out Page page)
        {
            page = default;
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            string[] parts = stored.Split('|', 5);
            if (parts.Length != 5
                || !int.TryParse(parts[0], out int state)
                || !int.TryParse(parts[1], out int which)
                || !Enum.IsDefined((PageState)state)
                || !Enum.IsDefined((Detail)which)
                || parts[2].Length == 0)
            {
                return false;
            }

            page = new Page(Unesc(parts[2]), (PageState)state, (Detail)which, Unesc(parts[3]), parts[4]);
            return true;
        }

        // The one character the row format spends. U+2502 is not a character any of our text uses, and the
        // swap is symmetric, so a value containing a pipe survives a round trip unchanged.
        private static string Esc(string s) => s.Replace('|', '│');

        private static string Unesc(string s) => s.Replace('│', '|');
    }

    /// <summary>
    /// THE REBIRTH, APPLIED TO THE BOOK. Every page dated after the line becomes one you don't remember
    /// writing; every page at or before it is yours. Called once at the succession seam and nowhere else.
    ///
    /// <para>A page that came back WRONG in an earlier life keeps its moved detail through this — the lie
    /// was written into the book and a new captain inherits the book, not the truth. What the rebirth
    /// resets is only where each page STANDS: a recovered page greys again if it is past the new line, and
    /// a refusal is lifted, because the roll is salted by the life and the next captain is owed their own
    /// answer.</para>
    ///
    /// <para>Rows for entries that are no longer in the ledger are dropped, so the book cannot grow without
    /// bound across a long universe.</para>
    /// </summary>
    public static IReadOnlyList<Page> MarkTheBook(
        IReadOnlyList<Page> book, IEnumerable<LedgerPage> pages, double line)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(pages);

        var was = new Dictionary<string, Page>(StringComparer.Ordinal);
        foreach (Page p in book)
        {
            was[p.EntryId] = p;
        }

        var marked = new List<Page>();
        foreach (LedgerPage entry in pages)
        {
            was.TryGetValue(entry.Id, out Page before);
            PageState state = Greys(line, entry) ? PageState.Unremembered : PageState.Remembered;
            marked.Add(new Page(entry.Id, state, before.Which, before.Original ?? "", before.Substitute ?? ""));
        }

        return marked;
    }

    /// <summary>This page's standing, or a plain remembered one for an entry the book has never heard of
    /// (which is every entry of a captain who has never died, and every row of a save written before this
    /// lane existed).</summary>
    public static Page Standing(IReadOnlyList<Page> book, string entryId)
    {
        ArgumentNullException.ThrowIfNull(book);
        foreach (Page p in book)
        {
            if (string.Equals(p.EntryId, entryId, StringComparison.Ordinal))
            {
                return p;
            }
        }

        return Page.Remembered(entryId);
    }

    /// <summary>
    /// #973 L3 · HOW MANY GREY PAGES THIS CAPTAIN HAS READ AT. Derived from the book rather than counted in
    /// a field, and that is not thrift — it is the only version of this number that survives a reload.
    ///
    /// <para><see cref="MarkTheBook"/> resets every page to <see cref="PageState.Remembered"/> or
    /// <see cref="PageState.Unremembered"/> at the succession seam, so the only way a row can be standing in
    /// any of the three states below is that somebody sat down with it SINCE the last rebirth. A counter
    /// kept beside the book would have had to be persisted, cleared on the rebirth, and remembered about at
    /// the one seam that clears it; this cannot be wrong.</para>
    /// </summary>
    public static int GreyPagesReadThisLife(IReadOnlyList<Page> book)
    {
        ArgumentNullException.ThrowIfNull(book);
        int read = 0;
        foreach (Page p in book)
        {
            if (p.State is PageState.Refused or PageState.CameBack or PageState.CameBackWrong)
            {
                read++;
            }
        }

        return read;
    }

    /// <summary>Write one page's new standing into the book, replacing any row it already had.</summary>
    public static IReadOnlyList<Page> Put(IReadOnlyList<Page> book, Page page)
    {
        ArgumentNullException.ThrowIfNull(book);
        var next = new List<Page>(book.Count + 1);
        bool replaced = false;
        foreach (Page p in book)
        {
            if (string.Equals(p.EntryId, page.EntryId, StringComparison.Ordinal))
            {
                next.Add(page);
                replaced = true;
            }
            else
            {
                next.Add(p);
            }
        }

        if (!replaced)
        {
            next.Add(page);
        }

        return next;
    }

    // ── §4 · WHICH ROW IS WHICH ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A stable id for one dated row of the Captain's ledger. The ledger is assembled fresh every render
    /// out of six different books (the autopilot's receipts, the plunder, the scope fixes, the route tips,
    /// what was overheard, what was found on the ground), so the row needs an identity that survives being
    /// rebuilt — and survives a save, a reload and a rebirth.
    ///
    /// <para>Kind + the whole second of the stamp + a hash of the row's own key. Never the row's rendered
    /// words: a wording change must not silently re-grey a page the captain already won back.</para>
    /// </summary>
    public static string EntryId(string kind, double simTime, string key)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(key);
        long stamp = (long)Math.Round(simTime);
        return $"{kind}:{stamp.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{Hash(key):x8}";
    }

    // FNV-1a/32, the same cheap stable hash idiom the rest of Core uses for opaque keys. Platform-stable
    // by construction (no string hashing from the runtime, which is randomized per process).
    private static uint Hash(string s)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        uint hash = offsetBasis;
        foreach (char c in s)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}

/// <summary>
/// #973 L1 · One dated row of the Captain's ledger, as the filing line sees it: an id that outlives a
/// re-render, the stamp that decides which side of the line it falls on, and the three text fields a
/// wrong recollection can move a detail in.
///
/// <para><paramref name="Provenance"/> is the ledger's own "&lt;who&gt; · &lt;where&gt; · day N" idiom
/// (<c>Map.Alerts.ProvenanceLine</c>) where the row has one, and whatever else the row files there
/// otherwise ("standing note", "logged 2h ago"). The two named halves are only read when it really does
/// split into three, which is why a row with no attribution can only ever misremember a number.</para>
/// </summary>
/// <param name="Filed">#973 L5a · THE SERVICE FILED THIS ONE, and so no rebirth can take it — not even an
/// uninsured one, where the line sits at negative infinity and every other page in the book goes grey. False
/// for every row the game assembles out of its six books; true for exactly one seeded page, and the reason
/// is written on <see cref="FilingLine.TheServiceFiledIt"/>. Defaulted, so every existing construction of
/// this record still describes a page the filing line may take.</param>
public readonly record struct LedgerPage(
    string Id, double SimTime, string Title, IReadOnlyList<string> Lines, string Provenance,
    bool Filed = false);
