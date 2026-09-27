using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · READING ONE — what a shelf says when the captain stands at it, the subjects it files under, and
/// every line of shelf prose for the sweeps.
///
/// <para>Split out of <c>Shelves.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class Shelves
{
    // ── READING ONE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Everything one read of one shelf answers, in one pure call.
    ///
    /// <para><see cref="Gist"/> is null when this thread has already filed this shelf — that is the whole of
    /// the once-per-shelf law, and it lives here rather than in an <c>if</c> in a partial class so a test can
    /// walk it. <see cref="Filed"/> is the new read-list either way, so the caller never has to
    /// decide.</para></summary>
    public readonly record struct Reading(
        Shelf Of, string Title, string Card, string? Gist, IReadOnlyList<string> Filed);

    /// <summary>
    /// #701 · READ A SHELF WHERE IT STANDS. The odd book's own law, said about a fixture that is always
    /// there: looking is free and the card comes up every time; the casebook learns the gist once per shelf
    /// per game-thread (#603).
    ///
    /// <para>Once per SHELF and not per room, exactly as the odd book files per book and not per room: the
    /// clerk's shelf is the clerk's shelf in every records annex in the system, and a book that filed it
    /// eleven times would be a book keeping a tally of how many corridors a captain has walked.</para>
    ///
    /// <para><paramref name="filed"/> is the ids this game-thread has already put in the casebook — the odd
    /// book's own list (<c>Vault.Progress.OddBooksRead</c>), which these ids are namespaced against so the
    /// two features share one store and can never collide.</para></summary>
    public static Reading Read(Shelf shelf, IReadOnlyList<string>? filed)
    {
        var read = new List<string>(filed ?? []);
        bool first = !read.Contains(shelf.Id, StringComparer.Ordinal);
        if (first)
        {
            read.Add(shelf.Id);
        }
        return new Reading(shelf, shelf.Plate, shelf.Card, first ? shelf.Gist : null, read);
    }

    /// <summary>#741 · WHAT THE ENTRY IS ABOUT, declared by the author and never read back out of its words.
    ///
    /// <para>The subject is the PLACE. A shelf says what somebody did and who they were and it names nobody
    /// — there is no person here for the book to be about, and minting one would be the game detecting
    /// (§12.4, and #741's own refusal). What a captain will want the stack of, standing in a corridor two
    /// floors down, is <i>this building</i>: every shelf they have read in it, under one heading.</para></summary>
    public static string SubjectsFor(string place) =>
        CaseSubjects.Line(CaseSubjects.Place(place ?? string.Empty));

    /// <summary>Every sentence this file can put on a screen, for the canon sweep. The shelf line, the card
    /// and the gist of all thirteen, and the plate the room shows built out of them.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (Entry entry in Work)
        {
            yield return entry.Shelf;
            yield return entry.Card;
            yield return entry.Gist;
            yield return $"{Glyph} {entry.Shelf}";
        }
        foreach (Entry entry in Freetime)
        {
            yield return entry.Shelf;
            yield return entry.Card;
            yield return entry.Gist;
            yield return $"{Glyph} {entry.Shelf}";
        }
    }
}
