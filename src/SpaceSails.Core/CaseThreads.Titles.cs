using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #251 · TITLE AND BULLETS — the expanding node: a note's glyph, its title and its bullets.
///
/// <para>Split out of <c>CaseThreads.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Its one field is a <c>const</c>.</para>
/// </summary>
public static partial class CaseThreads
{
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

    /// <summary>
    /// THE GLYPH THE NODE LEADS WITH — the note's own, <b>unless the entry already opens with it</b>, in
    /// which case nothing, because it is already there.
    ///
    /// <para>Found by booting the demo and reading the titles off the screen: every dossier entry came out
    /// <c>📇 📇 Nkechi Sarkisyan — …</c>. The book stores a skim glyph beside the text, and three of the
    /// four sentences a kit produces are WRITTEN with theirs at the front — so a surface that prints
    /// <c>Glyph + Text</c> prints it twice. The old flat notes list had exactly the same bug and nobody had
    /// looked at it beside a title.</para>
    ///
    /// <para>This is not a new law: <see cref="FieldDossier.DebriefBlock"/> has made this exact decision
    /// since #774, in the same words. It is stated here so the notebook and the card cannot come to two
    /// different views of one entry.</para>
    /// </summary>
    public static string GlyphFor(in FieldNote note)
    {
        string glyph = note.Glyph ?? "";
        string text = (note.Text ?? "").TrimStart();
        return glyph.Length > 0 && text.StartsWith(glyph, StringComparison.Ordinal) ? "" : glyph;
    }

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
}
