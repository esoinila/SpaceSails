using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #741 · THE RED PEN — the lines the captain draws between two things they wrote down.
///
/// <para>Owner, filing the build: <i>"I dream of drawing those conspiracy board connecting red lines... like
/// connecting the thread in the detective notebook... I guess it could be a red pen only used to connect the
/// things."</i> And the gesture, in his own words: <i>"select one, select the other — connected"</i>, after
/// which <i>"the ORDER of items updates so connected items become adjacent."</i></para>
///
/// <h3>The north star this file must not break</h3>
/// <para><b>SPOTTING is the player's act.</b> Nothing here connects anything. There is no inference, no
/// suggestion, no "two entries mention the same hand" badge. Every thread in this model was drawn by a human
/// who read two titles and saw the rhyme; the model's whole job is to remember that they did, and to arrange
/// the page so the catch is POSSIBLE. A board that connects itself has taken the only moment this feature
/// exists for.</para>
///
/// <para><b>MARKING is quiet.</b> Nothing in this file congratulates. There is no score, no count of "leads
/// solved", no sentence that says you were right — because there is nothing here that knows whether you
/// were. Owner's final cut of the register: <i>comprehension-without-acceptance</i>; the world keeps
/// functioning politely around what you now know.</para>
///
/// <h3>What a thread IS</h3>
/// <para>An unordered pair of note identities, drawn by hand, never duplicated, erasable, and persisted in
/// the vault like everything else. It carries no words: a thread that had a LABEL would be the game stating
/// what the connection means, which is the one thing it must never do.</para>
///
/// <para>Pure and deterministic, like everything else in Core.</para>
/// </summary>
public static class CaseThreads
{
    /// <summary>How many threads the book keeps, newest kept. Larger than the book's own eighty notes
    /// (<see cref="FieldNotes.Cap"/>) because a case that is worth drawing at all tends to be denser than
    /// one line per entry — and small enough that the vault section stays a handful of short strings.</summary>
    public const int Cap = 200;

    /// <summary>What the three fields of a note are joined with before they are hashed. A control character
    /// the house never writes, so no combination of a place and a text can spell another combination —
    /// <c>"A"</c> at <c>"BC"</c> and <c>"AB"</c> at <c>"C"</c> are different notes and must not collide.</summary>
    private const char Separator = (char)0x1F;

    // ── IDENTITY: what a red line is tied TO ────────────────────────────────────────────────────────────

    /// <summary>
    /// The durable handle for one entry in the field book.
    ///
    /// <para><see cref="FieldNote"/> has no id — it never needed one, because nothing had ever wanted to
    /// point AT a note. Rather than add a field (which every existing save would lack, and which would have
    /// to be minted somewhere at file time), the handle is DERIVED from the three fields the vault already
    /// round-trips verbatim: the place, the moment, and the words. A note that came back out of a save is
    /// the same note, so it keeps its threads.</para>
    ///
    /// <para>It is a 64-bit FNV-1a written by hand and <b>never <c>string.GetHashCode</c></b>: .NET
    /// randomises string hashing per process, so a handle built on it would be a different handle after the
    /// tab was reloaded — every thread in the vault would come back pointing at nothing. That bug would have
    /// been invisible in a single session and total across two.</para>
    ///
    /// <para><see cref="FieldNote.SimTime"/> goes in under the round-trip format for a reason: two identical
    /// finds at the same place — the book's dedup only refuses CONSECUTIVE repeats — would otherwise share
    /// one handle and one red line would tie both.</para>
    /// </summary>
    public static string IdentityOf(in FieldNote note)
    {
        string material =
            $"{note.Place}{Separator}{note.SimTime.ToString("R", CultureInfo.InvariantCulture)}{Separator}{note.Text}";

        // FNV-1a, 64-bit, over UTF-16 code units. Arithmetic only — the same answer on every runtime, this
        // year and next.
        ulong hash = 14695981039346656037UL;
        foreach (char c in material)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }
        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    // ── THE THREAD ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One red line. <paramref name="A"/> and <paramref name="B"/> are
    /// <see cref="IdentityOf"/> handles held in ordinal order, so the pair is UNORDERED by construction —
    /// there is no such thing as connecting A to B and then B to A, and no caller can create one by holding
    /// the two notes the other way round.</summary>
    public readonly record struct Thread(string A, string B)
    {
        /// <summary>Written down as one field. The handles are hex, so the separator can never occur inside
        /// one and the parse can never be ambiguous.</summary>
        public string Stored => $"{A}|{B}";

        /// <summary>Read one back. Anything this build cannot parse is dropped rather than thrown over — the
        /// vault is tolerant everywhere else, and a line the captain drew is not worth losing a game
        /// for.</summary>
        public static bool TryParse(string? stored, out Thread thread)
        {
            thread = default;
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            string[] parts = stored.Split('|');
            if (parts.Length != 2)
            {
                return false;
            }

            Thread? made = Between(parts[0], parts[1]);
            if (made is not { } ok)
            {
                return false;
            }

            thread = ok;
            return true;
        }
    }

    /// <summary>The canonical thread between two handles, or null when there is no thread to be had — a
    /// blank handle, or a note offered to itself. <b>Every</b> thread in the game is minted here, so the
    /// ordering law lives in one place and no caller can build an uncanonical pair by hand.</summary>
    public static Thread? Between(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return null;
        }

        int order = string.CompareOrdinal(a, b);
        if (order == 0)
        {
            // A note is not connected to itself. Pressing the same title twice is a change of mind, and the
            // client reads it as one — but the model refuses it outright so no stored line can ever say a
            // note rhymes with itself.
            return null;
        }

        return order < 0 ? new Thread(a, b) : new Thread(b, a);
    }

    /// <summary>
    /// DRAW THE LINE. Pure: returns a new list, never mutates the input.
    ///
    /// <para><paramref name="drawn"/> is true only when a thread that was NOT there is now there. That is
    /// what the soft cue is keyed on — the acknowledgment belongs to the act of connecting, and a pen
    /// re-drawn over a line that already exists is a pen making no new claim. Without this out-parameter a
    /// caller would have to compare list lengths, which is the same answer arrived at less honestly.</para>
    /// </summary>
    public static IReadOnlyList<Thread> Draw(
        IReadOnlyList<Thread>? threads, string? a, string? b, out bool drawn)
    {
        drawn = false;
        var list = new List<Thread>(threads ?? []);
        if (Between(a, b) is not { } thread)
        {
            return list;
        }

        foreach (Thread already in list)
        {
            if (already == thread)
            {
                return list;   // No duplicates. The same two titles connected twice is one line.
            }
        }

        list.Add(thread);
        if (list.Count > Cap)
        {
            list.RemoveRange(0, list.Count - Cap);
        }
        drawn = true;
        return list;
    }

    /// <summary>Draw, when the caller does not care whether it was new.</summary>
    public static IReadOnlyList<Thread> Draw(IReadOnlyList<Thread>? threads, string? a, string? b) =>
        Draw(threads, a, b, out _);

    /// <summary>
    /// RUB IT OUT. The pen has an eraser end, and it is the SAME GESTURE REVERSED: select one title, select
    /// the other, and a line that is already there comes off.
    ///
    /// <para>Chosen over a second instrument or a per-line unpin control for one reason — the gesture is the
    /// feature. Owner's whole ask is <i>select one, select the other</i>; a captain who has just learned
    /// that gesture already knows how to undo it, and a notebook page that grew a row of little ✕ buttons
    /// beside every title would be a database front end rather than a notebook. It is stated in the pen's
    /// own hint, because a reversible act nobody knows is reversible is not reversible.</para>
    /// </summary>
    public static IReadOnlyList<Thread> Erase(IReadOnlyList<Thread>? threads, string? a, string? b)
    {
        var list = new List<Thread>(threads ?? []);
        if (Between(a, b) is not { } thread)
        {
            return list;
        }

        list.RemoveAll(t => t == thread);
        return list;
    }

    /// <summary>Is there a line between these two already? What the client asks to know whether the second
    /// press draws or erases.</summary>
    public static bool AreThreaded(IReadOnlyList<Thread>? threads, string? a, string? b)
    {
        if (Between(a, b) is not { } thread)
        {
            return false;
        }

        foreach (Thread t in threads ?? [])
        {
            if (t == thread)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>How many lines run off one note — the number the COLLAPSED title carries, so the list of
    /// titles is itself a case status: which of them are still loose ends. Owner's own reading of it:
    /// the unconnected ones <i>"are just showing the title and an expand text button"</i>.</summary>
    public static int CountFor(IReadOnlyList<Thread>? threads, string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return 0;
        }

        int n = 0;
        foreach (Thread t in threads ?? [])
        {
            if (string.Equals(t.A, id, StringComparison.Ordinal)
                || string.Equals(t.B, id, StringComparison.Ordinal))
            {
                n++;
            }
        }
        return n;
    }

    // ── TITLE AND BULLETS: the expanding node ───────────────────────────────────────────────────────────
    //
    // Owner's UI spec, from the chair: "title-first, collapsed by default… every note shows its TITLE and an
    // expand button… the opened node houses its text as bullets… the note is structured for CONNECTING, not
    // for prose-reading; the prose lives in the log."
    //
    // A FieldNote has no title field and will not be given one: every entry already in every existing save
    // would lack it, and a title minted at file time could not be improved afterwards. So the title is READ
    // off the words the book already keeps, and — this is the part that keeps it honest — the full first
    // sentence is ALSO the first bullet, so expanding a node loses nothing to the clip.

    /// <summary>How long a title may run before it is clipped at a word boundary. Sized off the two shapes
    /// the book actually files — a clue line (<c>"📋 shipping manifest, torn — a description, read through
    /// B1 and copied out."</c>) and a dossier's opening (<c>"🗂 Ilse Vandermeer — a specialist in continuity
    /// engineering, carried out here by a directorate that files under Labour."</c>) — so the thing the eye
    /// is hunting for, the NAME at the front, survives the clip. #782 governs: it wraps to two lines at
    /// phone width and no more.</summary>
    public const int TitleLength = 88;

    /// <summary>The title of an entry: its first sentence, clipped at a word boundary with an ellipsis when
    /// it runs long. Never empty for a note with any words in it.</summary>
    public static string TitleOf(in FieldNote note) => Clip(FirstSentence(note.Text ?? ""));

    /// <summary>The expanded node: the entry's own sentences, one bullet each, in the order they were
    /// written. The first bullet is the FULL first sentence — the title above it may be a clipped copy, and
    /// a node that hid the rest of its own opening line behind a "…" would be the read-once bug this book
    /// was built to end (#587).</summary>
    public static IReadOnlyList<string> BulletsOf(in FieldNote note)
    {
        string text = (note.Text ?? "").Trim();
        if (text.Length == 0)
        {
            return [];
        }

        var bullets = new List<string>();
        int at = 0;
        while (at < text.Length)
        {
            int end = SentenceEnd(text, at);
            string one = text[at..end].Trim();
            if (one.Length > 0)
            {
                bullets.Add(one);
            }
            at = end;
        }
        return bullets;
    }

    /// <summary>Where the sentence starting at <paramref name="from"/> ends — just past its stop, or the end
    /// of the string. A stop is <c>. ? !</c> followed by whitespace, which is enough for prose the house
    /// writes and deliberately does not try to be a parser: the worst case is a bullet with two sentences in
    /// it, and the worst case of a clever one is a bullet cut in half mid-abbreviation.</summary>
    private static int SentenceEnd(string text, int from)
    {
        for (int i = from; i < text.Length - 1; i++)
        {
            if ((text[i] == '.' || text[i] == '?' || text[i] == '!') && char.IsWhiteSpace(text[i + 1]))
            {
                return i + 1;
            }
        }
        return text.Length;
    }

    private static string FirstSentence(string text)
    {
        string trimmed = text.Trim();
        return trimmed.Length == 0 ? "" : trimmed[..SentenceEnd(trimmed, 0)].Trim();
    }

    private static string Clip(string line)
    {
        if (line.Length <= TitleLength)
        {
            return line;
        }

        int cut = line.LastIndexOf(' ', Math.Min(TitleLength, line.Length - 1));
        return (cut > 0 ? line[..cut] : line[..TitleLength]).TrimEnd(' ', ',', ';', '—', '-') + "…";
    }

    // ── THE REORDER: connected items become adjacent ────────────────────────────────────────────────────
    //
    // Owner: "then the ORDER of items updates so connected items become adjacent — the list itself
    // reorganizes around the case's structure."

    /// <summary>
    /// THE ONE ORDERING LAW. Cluster by connected component; components in the order of their EARLIEST
    /// original member; original order inside a component.
    ///
    /// <para>Stable and comprehensible, which is the whole requirement: a captain who draws one line watches
    /// exactly two rows move together and nothing else jump. Ordering the clusters by their earliest member
    /// means the page does not reshuffle top to bottom every time a line is drawn — the block a note was
    /// already in stays where it was.</para>
    ///
    /// <para>It works on INDICES and returns a permutation of them, so it cannot lose, duplicate or invent an
    /// entry however tangled the threads are — including the case where two entries genuinely share a handle.
    /// The caller applies the permutation to whatever it is holding.</para>
    /// </summary>
    public static IReadOnlyList<int> ClusterOrder(IReadOnlyList<string>? ids, IReadOnlyList<Thread>? threads) =>
        Blocks(ids, threads).Order;

    /// <summary>The page as one walk: the permutation, and for each output position the block it belongs to.
    /// One computation, so the order and the blocks drawn around it can never come to two different views of
    /// the same set of threads — the class of bug this repo has named (a sentence reporting one thing while
    /// the sim does another).</summary>
    private static (IReadOnlyList<int> Order, IReadOnlyList<int> Cluster, IReadOnlyList<int> Size) Blocks(
        IReadOnlyList<string>? ids, IReadOnlyList<Thread>? threads)
    {
        int n = ids?.Count ?? 0;
        if (n == 0)
        {
            return ([], [], []);
        }

        // Union-find over the handles present in THIS list. A thread naming a note that is not on the page
        // (a different ground, an entry the cap has since trimmed) simply unions nothing — it is still in
        // the vault, and the day that note is on screen again the line is there waiting.
        var parent = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string id in ids!)
        {
            parent.TryAdd(id ?? "", id ?? "");
        }

        foreach (Thread t in threads ?? [])
        {
            if (parent.ContainsKey(t.A) && parent.ContainsKey(t.B))
            {
                Union(parent, t.A, t.B);
            }
        }

        // Each component, keyed by its root, remembers where its earliest member sat.
        var firstSeen = new Dictionary<string, int>(StringComparer.Ordinal);
        var roots = new string[n];
        for (int i = 0; i < n; i++)
        {
            string root = Find(parent, ids[i] ?? "");
            roots[i] = root;
            firstSeen.TryAdd(root, i);
        }

        var order = new List<int>(n);
        var cluster = new List<int>(n);
        var size = new List<int>(n);
        int block = -1;

        for (int i = 0; i < n; i++)
        {
            if (firstSeen[roots[i]] != i)
            {
                continue;   // Not this component's turn yet — it will be taken at its earliest member.
            }

            block++;
            int from = order.Count;
            for (int j = i; j < n; j++)
            {
                if (string.Equals(roots[j], roots[i], StringComparison.Ordinal))
                {
                    order.Add(j);
                    cluster.Add(block);
                    size.Add(0);
                }
            }
            for (int k = from; k < size.Count; k++)
            {
                size[k] = size.Count - from;
            }
        }
        return (order, cluster, size);
    }

    private static string Find(Dictionary<string, string> parent, string x)
    {
        while (!string.Equals(parent[x], x, StringComparison.Ordinal))
        {
            parent[x] = parent[parent[x]];
            x = parent[x];
        }
        return x;
    }

    private static void Union(Dictionary<string, string> parent, string a, string b)
    {
        string ra = Find(parent, a);
        string rb = Find(parent, b);
        if (!string.Equals(ra, rb, StringComparison.Ordinal))
        {
            // Ordinal-lowest root wins, so the same set of threads always produces the same forest whatever
            // order they came out of the vault in.
            if (string.CompareOrdinal(ra, rb) < 0)
            {
                parent[rb] = ra;
            }
            else
            {
                parent[ra] = rb;
            }
        }
    }

    // ── THE PAGE, as the notebook draws it ──────────────────────────────────────────────────────────────

    /// <summary>One node on the notebook page: the entry, its handle, how many lines run off it, which block
    /// it belongs to and how big that block is, and whether a red line runs from this row to the very next
    /// one — which is what the list idiom can actually DRAW. A full board layout is the ship's wall, not the
    /// notebook's.</summary>
    /// <param name="Note">The entry itself, untouched.</param>
    /// <param name="Id"><see cref="IdentityOf"/>, computed once here rather than at every use.</param>
    /// <param name="Threads">Lines running off this note — including to notes not on this page.</param>
    /// <param name="Cluster">Which block of the page this row is in, counted from the top.</param>
    /// <param name="ClusterSize">How many rows are in that block. One means a loose end.</param>
    /// <param name="ThreadedToNext">A line runs from this row to the row immediately under it — the edge the
    /// page draws in red. False on the last row of a block and on the last row of the page.</param>
    public readonly record struct Row(
        FieldNote Note, string Id, int Threads, int Cluster, int ClusterSize, bool ThreadedToNext);

    /// <summary>
    /// THE PAGE. The entries in case order, each carrying everything the notebook needs to draw it — so the
    /// client renders and decides nothing.
    ///
    /// <para>The order is <see cref="ClusterOrder"/>'s and nothing else, so the law is pinned once.</para>
    /// </summary>
    public static IReadOnlyList<Row> Page(IReadOnlyList<FieldNote>? notes, IReadOnlyList<Thread>? threads)
    {
        int n = notes?.Count ?? 0;
        if (n == 0)
        {
            return [];
        }

        var ids = new string[n];
        for (int i = 0; i < n; i++)
        {
            ids[i] = IdentityOf(notes![i]);
        }

        (IReadOnlyList<int> order, IReadOnlyList<int> cluster, IReadOnlyList<int> size) =
            Blocks(ids, threads);

        var rows = new List<Row>(n);
        for (int at = 0; at < order.Count; at++)
        {
            int i = order[at];
            bool sameBlockBelow = at + 1 < order.Count && cluster[at + 1] == cluster[at];
            rows.Add(new Row(
                notes![i],
                ids[i],
                CountFor(threads, ids[i]),
                cluster[at],
                size[at],
                sameBlockBelow && AreThreaded(threads, ids[i], ids[order[at + 1]])));
        }
        return rows;
    }

    // ── WHERE THE PEN WORKS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// MAY THE PEN COME OUT HERE? The <b>same</b> ladder the papers come out on
    /// (<see cref="SeatedSpread.CanSpreadTheCase"/>) behind the <b>same</b> posture gate the deliberate
    /// write-up sits behind (<see cref="SeatedPosture.RefusalIfStanding"/>) — deliberately not a second one.
    ///
    /// <para>Drawing a line between two entries is table work by the same reasoning the owner gave for the
    /// spread: it is the deliberate register, it wants both hands and a surface, and a gumshoe does not lay
    /// the case out where the keep can read it. Reading the collapsed titles is free anywhere — a captain at
    /// a sealed door consults their own notebook (#690) and always could.</para>
    ///
    /// <para><paramref name="seat"/> is null on your feet.</para>
    /// </summary>
    public static string? PenRefusal(SeatedHud.Seat? seat, bool alone) =>
        SeatedPosture.RefusalIfStanding(seat is not null)
        ?? (seat is { } s ? SeatedSpread.RefusalAt(s, alone) : null);

    /// <summary>…and the same question the other way up, for a control that needs a boolean.</summary>
    public static bool CanConnectHere(SeatedHud.Seat? seat, bool alone) => PenRefusal(seat, alone) is null;

    // ── WHAT THE PAGE SAYS ──────────────────────────────────────────────────────────────────────────────
    //
    // Minimal by ruling. The pen is an object, the threads are red, and nothing congratulates.

    /// <summary>What the instrument is called where it is drawn. An object out of the satchel, in the same
    /// physical register as the book and the sheet — not a mode, not a tool palette.</summary>
    public const string PenLabel = "🖊 THE RED PEN";

    /// <summary>The hint on it before it is picked up.</summary>
    public const string PenHint = "Take the red pen out — it does one thing, and this is it";

    /// <summary>The hint on it once it is in the hand, which is also where the eraser end is stated.</summary>
    public const string PenInHandHint =
        "Pen in hand. Press one title, then another: a line goes between them, and the same two presses take "
        + "it off again.";

    /// <summary>What the page says over the titles while the pen is down — the reading register.</summary>
    public const string ReadingBlurb =
        "Titles, newest first. Open the ones you are working on; the book keeps the rest folded.";

    /// <summary>…and what it says while the pen is up. It names the act and refuses to name a result: there
    /// is nothing here that knows whether a line is right.</summary>
    public const string ConnectingBlurb =
        "Two titles make a line. The book does not ask what it means and will not tell you.";

    /// <summary>The one title that has been picked up, waiting for its other end.</summary>
    public const string HoldingOneLine = "Holding one end. Press the title it rhymes with.";

    /// <summary>What the collapsed node says instead of a thread count when it has none. Deliberately a
    /// STATE and not a scold — a loose end is the ordinary condition of a note.</summary>
    public const string LooseEndLabel = "loose end";

    /// <summary>The whole page, empty. Two entries is the smallest case there is.</summary>
    public const string NothingToConnectLine =
        "One entry, and nothing to lay it against. A line needs two things that were written down apart.";

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return PenLabel;
        yield return PenHint;
        yield return PenInHandHint;
        yield return ReadingBlurb;
        yield return ConnectingBlurb;
        yield return HoldingOneLine;
        yield return LooseEndLabel;
        yield return NothingToConnectLine;
    }
}
