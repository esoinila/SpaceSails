namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 · A HELD MEMORY — a sheet in the black book that is not a document.
//
// The black book (`Satchel`: Carried / Notes / Threads / Spread) already holds what the captain FOUND.
// This is the other kind of evidence: what somebody REMEMBERS. A place, a face or a phrase, a mark
// saying whose memory it is, and — the second axis the owner asked for (addendum 2) — which of the
// detective's two theories it serves: follow the money, or follow the heart.
//
// L5a produces the DATA. L3 builds the sheet UI, the THREADS stacking and the SPREAD's reconcile; the
// only thing this lane owes that lane is a type it does not have to change.
//
// THE MARK IS THE WHOLE POINT. A memory marked MINE is the captain's own. One marked HIS or HERS was
// handed over by a person who was there — the strongest kind, because a second witness is holding it.
// One marked NOT ANYONE'S is the sort that fits nobody's life, and paying a nerve pip for it is the
// sanity seam #226 was kept for. Nothing in this file ever names what the clinic does.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 · One held memory: whose it is, which theory it serves, what it says, the names it puts
/// into THREADS, and whether the service filed it. Pure data with an opaque stored row — the house idiom
/// (see <c>Satchel.Item</c>, <c>FilingLine.Page</c>): the file carries the FACT and never the sentences.</summary>
public static class HeldMemory
{
    /// <summary>Whose memory this sheet is.</summary>
    public enum Mark
    {
        /// <summary>The captain's own — it fits his life, or the life he thinks he had.</summary>
        Mine = 0,

        /// <summary>A man handed it to you. He was there; you have a second witness.</summary>
        His = 1,

        /// <summary>A woman handed it to you. Same weight, and the book says which.</summary>
        Hers = 2,

        /// <summary>It fits nobody's life. This is the one that costs a pip.</summary>
        NotAnyones = 3,
    }

    /// <summary>MONEY &amp; LOVE (owner ruling §12) — the detective's two theories, as a tag on every sheet.
    /// A thread whose sheets are all money reads one way; one that turns out to be love-shaped reads
    /// another; and the truth under the arc sits beneath both.</summary>
    public enum Theory
    {
        /// <summary>Follow the money.</summary>
        Money = 0,

        /// <summary>Follow the heart.</summary>
        Love = 1,
    }

    /// <summary>The word the book prints for a mark.</summary>
    public static string Label(Mark mark) => mark switch
    {
        Mark.Mine => "mine",
        Mark.His => "his",
        Mark.Hers => "hers",
        _ => "not anyone's",
    };

    /// <summary>The word the book prints for a tag.</summary>
    public static string Label(Theory theory) => theory == Theory.Love ? "love" : "money";

    /// <summary>
    /// One sheet.
    /// </summary>
    /// <param name="Id">Stable across a save and a reload; the key a THREAD and a SPREAD are drawn against.</param>
    /// <param name="Mark">Whose memory it is.</param>
    /// <param name="Tag">Which theory it serves.</param>
    /// <param name="Text">What the sheet says. Authored words — never assembled at read time.</param>
    /// <param name="Threads">The names on it, each of which stacks under its own name in THREADS.</param>
    /// <param name="SimTime">When the sheet entered the book, or when the memory is dated.</param>
    /// <param name="HandedBy">#973 L3 · Who put it in your hand, in the book's own display name, or empty for a
    /// sheet nobody handed over (your own page; a stray). The BOOK shows it under the byline, because a memory
    /// with a second witness holding it is worth more than one without, and the sheet has to say which it is.</param>
    /// <param name="Confidence">#973 L3 · How many times the SPREAD has laid this sheet beside something that
    /// agreed with it. Goodwill, for a memory. Nothing spends it and nothing gates on it — it is the number the
    /// reconcile moves, so a captain can see a page they have corroborated warm up.</param>
    /// <param name="Corrected">#973 L3 · The SPREAD laid this memory beside the document it contradicted and the
    /// hidden original came back (<see cref="FilingLine.Page.Original"/>). A corrected sheet is the one kind of
    /// evidence in the book that has been caught lying and made to say the truth instead.</param>
    /// <param name="Filed">
    /// #973 L5a · THE SERVICE FILED THIS ONE. Set on exactly one sheet in the game — the summer-party page,
    /// which was written up as a fraternization report and is therefore the single piece of the captain's
    /// decent past that was preserved perfectly, by the people who preserved it against him.
    ///
    /// <para>It is a fact about the WORLD and not a flag on a renderer, which is why it lives here and why
    /// <see cref="FilingLine.MarkTheBook"/> reads it: a page the service filed is a page the captain
    /// remembers, and no rebirth — not even an uninsured one, where the line is at negative infinity and the
    /// whole book goes grey — can take it away.</para>
    /// </param>
    public readonly record struct Sheet(
        string Id,
        Mark Mark,
        Theory Tag,
        string Text,
        IReadOnlyList<string> Threads,
        double SimTime,
        bool Filed = false,
        string HandedBy = "",
        int Confidence = 0,
        bool Corrected = false)
    {
        /// <summary>The one line the book prints under the text: whose it is and which theory it serves.</summary>
        public string Byline => $"{Label(Mark)} · {Label(Tag)}";

        /// <summary>#973 L3 · Is this a page that fits nobody's life? The one mark that costs a pip, the one
        /// the lattice is made of, and the question three different rules ask — so it is asked once.</summary>
        public bool IsStray => Mark == Mark.NotAnyones;

        /// <summary>
        /// #973 L3 · The line the BOOK prints under a sheet: whose it is, which theory it serves, who put it
        /// in your hand, and the day. The ledger's own <c>&lt;who&gt; · &lt;where&gt; · day N</c> idiom, with
        /// the mark standing where the place would be — because for a memory the answer to <i>where did this
        /// come from</i> is a person's head and not a room.
        /// </summary>
        public string BookLine
        {
            get
            {
                string line = HandedBy is { Length: > 0 } who ? $"{Byline} · {who}" : Byline;
                line += $" · day {((int)(SimTime / 86400)).ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                if (Corrected)
                {
                    line += " · corrected";
                }

                if (Confidence > 0)
                {
                    line += $" · confidence {Confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                }

                return line;
            }
        }

        /// <summary>One more corroboration, capped. Returns the same sheet once the cap is reached, so a
        /// captain laying the same agreeing pair twenty times gains nothing the twenty-first time.</summary>
        public Sheet Warmer(int cap) =>
            Confidence >= cap ? this : this with { Confidence = Confidence + 1 };

        /// <summary>Add a name to the ones this sheet writes down, if it does not have it — how a lead the
        /// book cannot answer gets onto the THREADS page and waits there.</summary>
        public Sheet Naming(string name)
        {
            foreach (string had in Threads ?? [])
            {
                if (string.Equals(had, name, StringComparison.Ordinal))
                {
                    return this;
                }
            }

            return this with { Threads = [.. Threads ?? [], name] };
        }

        /// <summary>
        /// The opaque row the vault stores. Fields are pipe-separated with the pipe escaped in every one of
        /// them, and the threads are joined on a second separator, so any text round-trips.
        ///
        /// <para>#973 L3 APPENDED three fields and did not move one. The reader below takes any row with at
        /// least the original seven and defaults whatever a shorter one does not carry, which is exactly what
        /// an L5a save has: a sheet nobody handed over, never laid on the table, never corrected. Additive is
        /// the whole contract — an old file must load clean, not load differently.</para>
        /// </summary>
        public string Stored =>
            string.Join('|',
                Esc(Id), ((int)Mark).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ((int)Tag).ToString(System.Globalization.CultureInfo.InvariantCulture),
                SimTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                Filed ? "1" : "0",
                Esc(string.Join('␟', Threads ?? [])),
                Esc(Text),
                Esc(HandedBy ?? ""),
                Confidence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Corrected ? "1" : "0");

        /// <summary>Read one back. Anything this build cannot parse is dropped rather than thrown over — the
        /// same tolerance the satchel and the filing marks get. A row from before #973 L3 is short by three
        /// fields and comes back with those three at their defaults.</summary>
        public static bool TryParse(string? stored, out Sheet sheet)
        {
            sheet = default;
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            // Every field is escaped, so a full split can never be fooled by a pipe inside the prose.
            string[] p = stored.Split('|');
            if (p.Length < 7
                || p[0].Length == 0
                || !int.TryParse(p[1], out int mark) || !Enum.IsDefined((Mark)mark)
                || !int.TryParse(p[2], out int tag) || !Enum.IsDefined((Theory)tag)
                || !double.TryParse(p[3], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double at))
            {
                return false;
            }

            string threads = Unesc(p[5]);
            int confidence = 0;
            if (p.Length > 8)
            {
                _ = int.TryParse(p[8], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out confidence);
            }

            sheet = new Sheet(
                Unesc(p[0]), (Mark)mark, (Theory)tag, Unesc(p[6]),
                threads.Length == 0 ? [] : threads.Split('␟'),
                at,
                p[4] == "1",
                p.Length > 7 ? Unesc(p[7]) : "",
                confidence,
                p.Length > 9 && p[9] == "1");
            return true;
        }

        // The two characters the row format spends, neither of which any of our text uses, and both swaps
        // are symmetric — so a value containing either survives a round trip unchanged.
        private static string Esc(string s) => s.Replace('|', '│');

        private static string Unesc(string s) => s.Replace('│', '|');
    }

    /// <summary>Add a sheet to the book, replacing any sheet already filed under the same id. Idempotent by
    /// construction: handing over the same photograph twice is one photograph.</summary>
    public static IReadOnlyList<Sheet> Put(IReadOnlyList<Sheet> book, Sheet sheet)
    {
        ArgumentNullException.ThrowIfNull(book);
        var next = new List<Sheet>(book.Count + 1);
        bool replaced = false;
        foreach (Sheet s in book)
        {
            if (string.Equals(s.Id, sheet.Id, StringComparison.Ordinal))
            {
                next.Add(sheet);
                replaced = true;
            }
            else
            {
                next.Add(s);
            }
        }

        if (!replaced)
        {
            next.Add(sheet);
        }

        return next;
    }

    /// <summary>The sheet filed under this id, or null.</summary>
    public static Sheet? Find(IReadOnlyList<Sheet> book, string? id)
    {
        ArgumentNullException.ThrowIfNull(book);
        foreach (Sheet s in book)
        {
            if (string.Equals(s.Id, id, StringComparison.Ordinal))
            {
                return s;
            }
        }

        return null;
    }

    /// <summary>The sheet id a shipmate's slip rides under — one per person per thread, so a second good
    /// glass with the same friend does not fill the book with the same page.</summary>
    public static string SlipId(string shipmateId) => $"slip:{shipmateId}";

    /// <summary>The photograph's sheet id. One per game, whoever hands it over.</summary>
    public const string PhotographId = "photograph";

    /// <summary>What the row over a sheet says, wherever a sheet is drawn — the Captain's ledger and the
    /// black book both. One rule, so the fleet-day page cannot end up being two different rows in two
    /// different surfaces. Every heading is a NOUN and never a summary: a heading that told you what the
    /// memory meant would be doing the reading for you.</summary>
    public static string RowTitle(Sheet sheet) =>
        string.Equals(sheet.Id, OldCrewScene.SummerPartyId, StringComparison.Ordinal)
            ? OldCrewScene.SummerPartyTitle
            : "🎞 A held memory";

    // ── #973 L3 · THREADS ────────────────────────────────────────────────────────────────────────────
    //
    // #741's THREADS page is one heading per NAME the field book wrote down more than once, and under it
    // the entries that wrote it. The sheets stack on exactly the same principle and on the same page: the
    // photograph writes down four faces, so it puts four names on the table at once, and the day a slip
    // from one of those four arrives it lands under the name it shares.
    //
    // THE ONE DIFFERENCE FROM #741'S STACKS is deliberate: a name is a thread here from the FIRST sheet
    // that writes it, not the second. The field book's rule earns its threshold — a place mentioned once
    // is noise — but a held memory naming a person is never noise: the photograph naming Hollis Grey once
    // is the only place in the game that name is ever spoken to the captain.

    /// <summary>One stack on the THREADS page: a name the sheets have written down, and every sheet that
    /// wrote it, oldest first.</summary>
    public readonly record struct Stack(string Name, IReadOnlyList<Sheet> Sheets)
    {
        /// <summary>What the page puts over the stack — the #741 idiom, with the sheets' own glyph.</summary>
        public string Heading => $"🎞 {Name}";

        /// <summary>How many money sheets and how many love ones are in this stack — the second question,
        /// asked of a whole thread rather than of a laid pair.</summary>
        public int Money => Sheets.Count(s => s.Tag == Theory.Money);

        /// <summary>…and the other theory.</summary>
        public int Love => Sheets.Count(s => s.Tag == Theory.Love);
    }

    /// <summary>
    /// THE SHEETS, STACKED BY THE NAMES THEY WRITE DOWN. Ordered by the most recent sheet in each stack,
    /// newest stack first, with the name as the tiebreak — the same ordering
    /// <see cref="CaseSubjects.ThreadsOf"/> gives the field book's threads, so the two halves of one page
    /// do not read in two different directions.
    /// </summary>
    /// <param name="book">Every sheet the captain holds.</param>
    /// <param name="tag">The money/love filter (owner ruling §12), or null for the whole book.</param>
    public static IReadOnlyList<Stack> Stacks(IReadOnlyList<Sheet> book, Theory? tag = null)
    {
        ArgumentNullException.ThrowIfNull(book);

        var byName = new Dictionary<string, List<Sheet>>(StringComparer.Ordinal);
        foreach (Sheet s in book)
        {
            if (tag is { } want && s.Tag != want)
            {
                continue;
            }

            foreach (string name in s.Threads ?? [])
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!byName.TryGetValue(name, out List<Sheet>? stack))
                {
                    stack = [];
                    byName[name] = stack;
                }

                stack.Add(s);
            }
        }

        var stacks = new List<Stack>(byName.Count);
        foreach (KeyValuePair<string, List<Sheet>> pair in byName)
        {
            pair.Value.Sort((a, b) => a.SimTime.CompareTo(b.SimTime));
            stacks.Add(new Stack(pair.Key, pair.Value));
        }

        stacks.Sort((a, b) =>
        {
            int byTime = b.Sheets[^1].SimTime.CompareTo(a.Sheets[^1].SimTime);
            return byTime != 0 ? byTime : string.CompareOrdinal(a.Name, b.Name);
        });

        return stacks;
    }

    /// <summary>The sheets this filter shows, newest first — what the book draws when a thread is not the
    /// arrangement being asked for.</summary>
    public static IReadOnlyList<Sheet> Filtered(IReadOnlyList<Sheet> book, Theory? tag = null)
    {
        ArgumentNullException.ThrowIfNull(book);
        var kept = new List<Sheet>(book.Count);
        foreach (Sheet s in book)
        {
            if (tag is null || s.Tag == tag)
            {
                kept.Add(s);
            }
        }

        kept.Sort((a, b) => b.SimTime.CompareTo(a.SimTime));
        return kept;
    }

    /// <summary>How many money sheets and how many love ones are in a set — the SPREAD's second question,
    /// and the THREADS filter's own count, asked once so the two can never disagree.</summary>
    public static (int Money, int Love) MoneyAndLove(IEnumerable<Sheet> sheets)
    {
        ArgumentNullException.ThrowIfNull(sheets);
        int money = 0;
        int love = 0;
        foreach (Sheet s in sheets)
        {
            if (s.Tag == Theory.Love)
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
}
