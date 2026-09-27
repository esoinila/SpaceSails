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
public static partial class CaseThreads
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
