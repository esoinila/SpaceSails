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
        bool Filed = false)
    {
        /// <summary>The one line the book prints under the text: whose it is and which theory it serves.</summary>
        public string Byline => $"{Label(Mark)} · {Label(Tag)}";

        /// <summary>The opaque row the vault stores. Fields are pipe-separated with the pipe escaped in every
        /// one of them, and the threads are joined on a second separator, so any text round-trips.</summary>
        public string Stored =>
            string.Join('|',
                Esc(Id), ((int)Mark).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ((int)Tag).ToString(System.Globalization.CultureInfo.InvariantCulture),
                SimTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                Filed ? "1" : "0",
                Esc(string.Join('␟', Threads ?? [])),
                Esc(Text));

        /// <summary>Read one back. Anything this build cannot parse is dropped rather than thrown over — the
        /// same tolerance the satchel and the filing marks get.</summary>
        public static bool TryParse(string? stored, out Sheet sheet)
        {
            sheet = default;
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            string[] p = stored.Split('|', 7);
            if (p.Length != 7
                || p[0].Length == 0
                || !int.TryParse(p[1], out int mark) || !Enum.IsDefined((Mark)mark)
                || !int.TryParse(p[2], out int tag) || !Enum.IsDefined((Theory)tag)
                || !double.TryParse(p[3], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double at))
            {
                return false;
            }

            string threads = Unesc(p[5]);
            sheet = new Sheet(
                Unesc(p[0]), (Mark)mark, (Theory)tag, Unesc(p[6]),
                threads.Length == 0 ? [] : threads.Split('␟'),
                at,
                p[4] == "1");
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
}
