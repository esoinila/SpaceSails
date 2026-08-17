using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #741 v1 · THE THREADS — what the book has already written down more than once.
///
/// <para>Owner, on the reading the notebook still could not give him: <i>"that is also the 'conspiracy
/// red-line detective'-view in a sense, to try to understand the big picture, so we should put effort into
/// making sure it tells the story we discover."</i> And the shape of it, in the issue's own words: <i>"a
/// third tab — THREADS — grouping existing entries by the entities they already name… the THREAD is just the
/// stack of what you wrote about one name, and the captain draws the line."</i></para>
///
/// <h3>The law this file exists to obey</h3>
/// <para><b>A SUBJECT COMES FROM THE AUTHOR, NEVER FROM THE PROSE.</b> Nothing here reads a note's text.
/// The gist author already knows what its sentence is about — it built the sentence out of a person, an
/// office and a door — so it declares them at writing time and the book keeps them beside the words. A
/// subject extractor that went looking for capitals in a sentence would find OFFICE OF WORKS on Tuesday and
/// The Tilt's harbourmaster never, and the day somebody rewrote a line the case would quietly come apart.
/// <see cref="AllProse"/>'s sibling guard (<c>TheThreadsAreTheAuthorsTests</c>) reads this file's SOURCE and
/// fails if a regex is ever pointed at <see cref="FieldNote.Text"/>.</para>
///
/// <para><b>NEVER A PERSONAL NAME THE GAME HAS NOT PRINTED.</b> A subject naming a stranger is only ever
/// minted by an author that is <i>printing that stranger's name on the card the captain is reading</i>
/// (<see cref="Person"/>). The dossier prints them — the name, the kin who shares the family name, the in
/// that is the dead person's own name — so those three entries genuinely rhyme, and the rhyme was always
/// there. What must never happen is a thread heading naming somebody the captain has never been told about:
/// that is the game doing the detecting, and it is the one thing #741 exists to refuse.</para>
///
/// <para><b>AND IT CONCLUDES NOTHING.</b> A thread is a STACK, not an accusation. There is no arrow, no
/// "these are connected", no score. Two entries about one office sit under one heading in the order they
/// were written and the book keeps no opinion about them — the red pen (<see cref="CaseThreads"/>) is still
/// the only thing in the game that draws a line, and a human hand is still the only thing that moves
/// it.</para>
///
/// <para>Pure and deterministic, like everything else in Core.</para>
/// </summary>
public static class CaseSubjects
{
    /// <summary>What the ids of one entry's subjects are joined with inside <see cref="FieldNote.Subjects"/>.
    /// A control character the house never writes, exactly as <see cref="CaseThreads"/> does it, so no name
    /// can ever spell a separator and no split can be ambiguous.</summary>
    private const char Separator = (char)0x1F;

    /// <summary>What the KIND is stamped on the front of an id with. An ordinary colon rather than a second
    /// control character, and it must never be the same character as <see cref="Separator"/>: an id that
    /// carried the separator inside it would be torn in half by the very split that reads the field back.
    /// (It was, in this file's first draft, and every stack came back empty.)</summary>
    private const char Mark = ':';

    /// <summary>How many entries make a thread. Two, and it is not a tunable: one entry about a name is a
    /// note, and the whole feature is the moment a SECOND one lands on top of it.</summary>
    public const int MakesAThread = 2;

    // ── WHAT A SUBJECT IS ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The three sorts of thing the book's entries are about. Deliberately small: a kind that could
    /// not be pointed at something already printed in the game would be a heading nobody can earn.</summary>
    public enum Kind
    {
        /// <summary>An office, a directorate, a letterhead — the institutional hand behind a piece of paper.
        /// The one the issue names: <i>"the second entry about OFFICE OF WORKS"</i>.</summary>
        Office,

        /// <summary>A station, a berth, a bar with a door in it — somewhere you could go.</summary>
        Place,

        /// <summary>A person, <b>and only one the game has printed for the captain to read</b>.</summary>
        Person,
    }

    /// <summary>One thing the book's entries can be about: what sort of thing it is, and what it is called —
    /// verbatim, in the words the game printed, because a heading that re-spelled a name would be a second
    /// source for one fact.</summary>
    public readonly record struct Subject(Kind Of, string Name)
    {
        /// <summary>The durable id kept on the note. The kind leads so an office and a person who happen to
        /// share a string are two subjects, and the name is stored whole so the heading needs no lookup
        /// table that a save could outlive.</summary>
        public string Id => $"{Letter(Of)}{Mark}{Name}";

        /// <summary>The glyph the heading leads with, so a page of stacks can be skimmed by sort.</summary>
        public string Glyph => Of switch
        {
            Kind.Office => "🏛",
            Kind.Place => "📍",
            _ => "👤",
        };

        /// <summary>The heading over the stack. The glyph and the name and nothing else — no count, no
        /// verdict, no "connected to".</summary>
        public string Heading => $"{Glyph} {Name}";
    }

    private static char Letter(Kind of) => of switch
    {
        Kind.Office => 'o',
        Kind.Place => 'p',
        _ => 'w',
    };

    private static Kind KindOf(char letter) => letter switch
    {
        'o' => Kind.Office,
        'p' => Kind.Place,
        _ => Kind.Person,
    };

    // ── MINTING: the only three doors into a subject ────────────────────────────────────────────────────

    /// <summary>An office, a directorate, a letterhead. Whatever the paper stamps on itself.</summary>
    public static Subject Office(string letterhead) => new(Kind.Office, Clean(letterhead));

    /// <summary>Somewhere with a door: a station, a berth, a bar.</summary>
    public static Subject Place(string name) => new(Kind.Place, Clean(name));

    /// <summary>
    /// A person — <b>and the caller is stating, by calling this, that the sentence it is writing PRINTS
    /// this name</b>. There is no other way to mint one, and the canon sweep walks every authored gist in
    /// the game checking that promise against the words the entry actually carries.
    ///
    /// <para>That is the whole of the personal-name law. It is a promise made by an author about its own
    /// sentence rather than a filter over a pool of names, because a filter would have to know every name
    /// the game will ever print and would be wrong the first time somebody adds one.</para>
    /// </summary>
    public static Subject Person(string printedName) => new(Kind.Person, Clean(printedName));

    private static string Clean(string? name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Replace(Separator, ' ').Trim();
    }

    // ── WHAT THE NOTE CARRIES ───────────────────────────────────────────────────────────────────────────

    /// <summary>The field a gist author hands to <see cref="FieldNote"/>: its subjects, joined, in the order
    /// it declared them. Empty when the sentence is about nothing the game has named — which is the ordinary
    /// case and not a defect: a maintenance log that stops recording and does not say why names nobody.</summary>
    public static string Line(params Subject[] subjects) => Line((IEnumerable<Subject>)(subjects ?? []));

    /// <summary>…the same, for an author holding a collection. Blank names are dropped and duplicates are
    /// folded, so a note is never twice about one thing.</summary>
    public static string Line(IEnumerable<Subject>? subjects)
    {
        var ids = new List<string>();
        foreach (Subject one in subjects ?? [])
        {
            if (one.Name.Length == 0)
            {
                continue;
            }
            string id = one.Id;
            if (!ids.Contains(id, StringComparer.Ordinal))
            {
                ids.Add(id);
            }
        }
        return string.Join(Separator, ids);
    }

    /// <summary>What this entry is about, in the order its author declared it. An entry off a save that
    /// predates #741 has no subjects and comes back with none — which is honestly what it had.</summary>
    public static IReadOnlyList<Subject> On(in FieldNote note)
    {
        string line = note.Subjects ?? "";
        if (line.Length == 0)
        {
            return [];
        }

        var subjects = new List<Subject>();
        foreach (string id in line.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryRead(id, out Subject one) && !subjects.Contains(one))
            {
                subjects.Add(one);
            }
        }
        return subjects;
    }

    /// <summary>Read one id back. Anything this build cannot parse is dropped rather than thrown over: the
    /// vault is tolerant everywhere else, and a stack the captain cannot see is not worth a lost game.</summary>
    public static bool TryRead(string? id, out Subject subject)
    {
        subject = default;
        if (id is null || id.Length < 3 || id[1] != Mark)
        {
            return false;
        }

        string name = id[2..].Trim();
        if (name.Length == 0)
        {
            return false;
        }

        subject = new Subject(KindOf(id[0]), name);
        return true;
    }

    // ── THE STACKS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One thread: a subject the book has written about more than once, and every entry that named
    /// it, in the order they were written.</summary>
    /// <param name="Subject">The thing itself.</param>
    /// <param name="Heading">What the page puts over the stack — <see cref="CaseSubjects.Subject.Heading"/>,
    /// carried here so a surface never composes a second version of it.</param>
    /// <param name="Entries">The entries that named it, oldest first. Never fewer than
    /// <see cref="MakesAThread"/>.</param>
    public readonly record struct SubjectThread(
        Subject Subject, string Heading, IReadOnlyList<FieldNote> Entries);

    /// <summary>
    /// THE THREADS PAGE. Every subject the book names at least twice, each with its entries in the order the
    /// book wrote them — which is chronological, because the book only ever appends.
    ///
    /// <para>The stacks come back <b>most recently written-to first</b>, the same convention
    /// <see cref="FieldNotes.PerPlace"/> uses for grounds: the thread you just added to is the one you are
    /// trying to think about. Ties are broken by heading so the page is deterministic on a book filed in one
    /// tick (the demo case is exactly that).</para>
    ///
    /// <para>Notice what is NOT here: no ranking by size, no "strongest lead", no cross-thread arrow. The
    /// page is a stack of stacks and the captain does the rest.</para>
    /// </summary>
    public static IReadOnlyList<SubjectThread> ThreadsOf(IReadOnlyList<FieldNote>? notes)
    {
        if (notes is null || notes.Count == 0)
        {
            return [];
        }

        var order = new List<Subject>();
        var bySubject = new Dictionary<Subject, List<FieldNote>>();
        var latest = new Dictionary<Subject, double>();

        foreach (FieldNote note in notes)
        {
            foreach (Subject subject in On(note))
            {
                if (!bySubject.TryGetValue(subject, out List<FieldNote>? stack))
                {
                    stack = [];
                    bySubject[subject] = stack;
                    order.Add(subject);
                }
                stack.Add(note);
                latest[subject] = Math.Max(latest.TryGetValue(subject, out double was) ? was : double.MinValue,
                    note.SimTime);
            }
        }

        var threads = new List<SubjectThread>();
        foreach (Subject subject in order)
        {
            List<FieldNote> stack = bySubject[subject];
            if (stack.Count >= MakesAThread)
            {
                threads.Add(new SubjectThread(subject, subject.Heading, stack));
            }
        }

        threads.Sort((a, b) =>
        {
            int byTime = latest[b.Subject].CompareTo(latest[a.Subject]);
            return byTime != 0 ? byTime : string.CompareOrdinal(a.Heading, b.Heading);
        });
        return threads;
    }

    /// <summary>How many entries in this book name that subject. What the badge counts and what a heading
    /// says out loud.</summary>
    public static int CountFor(IReadOnlyList<FieldNote>? notes, Subject subject)
    {
        int n = 0;
        foreach (FieldNote note in notes ?? [])
        {
            foreach (Subject one in On(note))
            {
                if (one == subject)
                {
                    n++;
                    break;
                }
            }
        }
        return n;
    }

    // ── THE BADGE: noticing a thread FORM ───────────────────────────────────────────────────────────────
    //
    // The issue's nice-to-have, and it is the detective-fiction beat rather than a feature: "a thread badge
    // on new gists ('second entry about OFFICE OF WORKS'), because noticing a thread FORM is the dopamine".
    //
    // It is a COUNT and a NAME. It is not a suggestion, it does not say the two entries are related, and it
    // never appears as a banner — #736's law governs, so it is composed onto the card the captain is already
    // reading, once, and then it is gone. The place it lives permanently is the THREADS page.

    /// <summary>
    /// "second entry about OFFICE OF WORKS" — or null when this entry's subjects are all fresh, and null
    /// when it declared none.
    ///
    /// <para><paramref name="book"/> is the book WITH this note already in it (the badge is asked after the
    /// filing, because the count it says out loud is the count that is now true). Where an entry names
    /// several things that already have stacks, the badge names the deepest one, and ties go to the subject
    /// the author declared first — one line, about one name, never a list.</para>
    /// </summary>
    public static string? NewThreadBadge(in FieldNote note, IReadOnlyList<FieldNote>? book)
    {
        Subject deepest = default;
        int most = 0;
        foreach (Subject subject in On(note))
        {
            int count = CountFor(book, subject);
            if (count > most)
            {
                most = count;
                deepest = subject;
            }
        }

        return most >= MakesAThread ? $"{Ordinal(most)} entry about {deepest.Name}" : null;
    }

    /// <summary>The count word, in the house's voice — the book says <i>second</i>, never <i>×2</i>. Past
    /// the tenth it stops counting out loud, because a captain who has ten entries about one office is not
    /// being told anything by an eleventh number.</summary>
    public static string Ordinal(int count) => count switch
    {
        2 => "second",
        3 => "third",
        4 => "fourth",
        5 => "fifth",
        6 => "sixth",
        7 => "seventh",
        8 => "eighth",
        9 => "ninth",
        10 => "tenth",
        _ => "another",
    };

    // ── WHAT THE PAGE SAYS ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The tab, beside the pocket and the book.</summary>
    public const string TabLabel = "🧵 THREADS";

    /// <summary>The hint on the tab.</summary>
    public const string TabHint = "What this book has named more than once";

    /// <summary>What the page says over the stacks. It states what it did — grouped by a name — and refuses
    /// to state why, which is the book's frame law (#587: it keeps it, it keeps no opinion about it).</summary>
    public const string Blurb =
        "Names this book has written down more than once, and every entry that wrote them. What any of them "
        + "has to do with the others is your business.";

    /// <summary>The whole page, empty. Not a scold and not a promise — a state, and the "Yet." is the book
    /// being honest that it is still early rather than the game hinting there is something to find.</summary>
    public const string NothingTwiceLine = "Nothing in this book names the same thing twice. Yet.";

    /// <summary>The pen, offered at a heading: one press runs it down the stack. It is still the pen, still
    /// the same gesture and still erasable pair by pair — what it saves is the wrist, never the judgement.</summary>
    public const string ConnectTheStackLabel = "🖊 run the pen down this stack";

    /// <summary>…and the hint on it, which refuses to say the stack means anything.</summary>
    public const string ConnectTheStackHint =
        "One line from each entry to the next, oldest to newest. The pen makes no claim; you did.";

    /// <summary>How many entries are under a heading, said in words rather than drawn as a badge.</summary>
    public static string EntriesLabel(int count) => count == 1 ? "1 entry" : $"{count} entries";

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return TabLabel;
        yield return TabHint;
        yield return Blurb;
        yield return NothingTwiceLine;
        yield return ConnectTheStackLabel;
        yield return ConnectTheStackHint;
    }
}
