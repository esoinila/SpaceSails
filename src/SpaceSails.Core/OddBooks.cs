using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #701 · THE ODD BOOK — the empty room that is worth having walked into.
///
/// <para>Owner, 2026-08-04: <i>"a better alternative to finding an empty room. You look around but only one
/// book catches your attention."</i> And the framing that states the stakes best, from the issue this one
/// swallowed: <b>searching a room and finding nothing is currently the most common outcome in the Hive and
/// the least written.</b> That is the beat this exists to fix — the same payout as an empty room, one string,
/// and something the player will remember.</para>
///
/// <h3>The engine, which is never on screen</h3>
///
/// <para>The owner's morning expansion: the facility runs a books-as-intelligence function. Staff who know
/// they are told nothing, reading EVERYTHING — fiction, myth, fringe cosmology — sifting for leaks about the
/// before-worlds. <i>"I love that the secret lab peoples knowing the siloed need to know information policy
/// would collect all kinds of books, including Lovecraft, to do the same."</i> The homage that shape comes
/// from stays unnamed, and so does the department. <b>The books never explain. The READING of them is the
/// staff doing exactly the inference the player is doing</b> — and they never found the leak either. That
/// fact is nowhere stated and everywhere present.</para>
///
/// <h3>The register test, adopted as law for this feature</h3>
///
/// <para><b>"The find has to be something the player can be delighted to disbelieve."</b> Every one of the
/// ten below passes it. Anything with a live court docket and real victims fails it before any other
/// consideration applies — not squeamishness, craft: it stops being folklore the player enjoys being wrong
/// about and becomes the game <i>saying something</i>.</para>
///
/// <h3>The two laws that keep it from becoming a shopping trip</h3>
///
/// <list type="number">
/// <item><b>Most empties stay empty.</b> One would-be-empty room in <see cref="Rate"/> holds a book. The
/// emptiness is load-bearing (§10.3): if there were always something, the walk is a shopping trip. Measured,
/// not assumed — see the guards.</item>
/// <item><b>Nothing ever enters the satchel.</b> A book is read where it stands and left where it stands.
/// The room is never struck off, which is what makes re-reading possible; the CASEBOOK is where the payout
/// lands, once per book per game-thread (#603: looking is free, knowledge is one-shot).</item>
/// </list>
///
/// <para>Pure and world-blind, like everything else this file's neighbours answer: the client asks, and never
/// keeps an opinion of its own about whether a room has a book in it.</para>
/// </summary>
public static class OddBooks
{
    /// <summary>One would-be-empty room in six. Not a third, not a half: the room that pays nothing is the
    /// most common thing in this building and it has to STAY the most common thing, or the surprise of a
    /// shelf with something on it is spent inside one floor.</summary>
    public const int Rate = 6;

    /// <summary>The glyph the shelf line and the casebook entry both carry.</summary>
    public const string Glyph = "\U0001F4D6";

    /// <summary>One entry of the authored catalog. <see cref="Shelf"/> is what the room shows,
    /// <see cref="Card"/> is what [E] reads, <see cref="Gist"/> is what the casebook keeps.
    ///
    /// <para>All three are AUTHORED TEXT and are lifted verbatim (#701's canonical catalog comment). Nothing
    /// in this file may reword them; the framing sentence around the shelf line is house prose and the
    /// authored fragment inside it is asserted present character-for-character by the guards.</para></summary>
    public readonly record struct Entry(int Number, string Id, string Shelf, string Card, string Gist);

    /// <summary>THE AUTHORED CATALOG. Ten entries, verbatim.
    ///
    /// <para>The order is the catalog's own and is part of the contract: <see cref="Entry.Number"/> is what
    /// the <c>?book=N</c> cheat names, so reordering this list renames every row in the testing guide.</para>
    ///
    /// <para><see cref="Entry.Id"/> is what the vault stores — short, stable, and never shown. Renaming one
    /// re-files a book a captain has already read, so they are as fixed as the prose.</para></summary>
    public static IReadOnlyList<Entry> Catalog { get; } =
    [
        new Entry(1, "the-oldest-sea-story",
            "a sewn binding, older than the paper it protects",
            "An old translation of an older poem: a man ten years coming home from a war, and everything in " +
            "the water that would rather he didn't. Somebody has marked the passages where he lies about " +
            "his name.",
            "the old poem — the marked passages are the ones where he lies about his name"),

        new Entry(2, "the-knight",
            "two volumes, the spine of the second broken",
            "The one about the old man who read too many books of the wrong kind, and set out to live in " +
            "the world they described. The margins here are argumentative. Whoever owned it kept taking the " +
            "knight's side.",
            "the knight's book — its reader kept taking the knight's side"),

        new Entry(3, "the-travels",
            "a facsimile in a library slipcase, much handled",
            "A fourteenth-century geography of places that are not on any chart: kingdoms measured in days " +
            "of riding, healing waters, cities with polite and specific names. It is written in the voice of " +
            "a man filing a report, which is the thing nobody has ever managed to explain about it.",
            "the 1357 travels — fable in the voice of a filed report"),

        new Entry(4, "the-catalog-slip",
            "a paper slip where a book should be",
            "A withdrawal slip in institutional Latin shorthand, citing a manuscript by library, number and " +
            "folio range — ff. 47–51 — in a collection on Earth. The withdrawal date is " +
            "recent. The return date is blank.",
            "a withdrawal slip for folios 47–51 of a manuscript on Earth — never returned"),

        new Entry(5, "the-tales-of-ratiocination",
            "a slim collection, read to softness",
            "Early detective stories, from the century that invented them: locked rooms, purloined letters, " +
            "the insistence that the answer is present in the account of the problem. Somebody here believed " +
            "that. The underlinings are all in the method, never in the solutions.",
            "the detective stories — underlined for method, never for answers"),

        new Entry(6, "the-horror-collection",
            "a cheap edition that has been carefully repaired",
            "Weird tales from the early twentieth century — cyclopean cities, dead things dreaming, " +
            "scholars who learn one true thing and stop sleeping. The repairs are careful and repeated. " +
            "Somebody kept reading it after it started falling apart.",
            "the weird tales — repaired too often to be casual reading"),

        new Entry(7, "worlds-in-collision",
            "a mid-twentieth-century hardback, library-bound",
            "The famous wrong book: planets on unstable courses within human memory, myth read as testimony. " +
            "The reviews it collected are folded inside, all hostile. Under the reviews, in a different " +
            "hand, a single sheet of neat notes with no verdict on it at all.",
            "the famous wrong book — hostile reviews enclosed; one reader kept neutral notes"),

        new Entry(8, "the-mechanics-text",
            "a thick standard text, the corners gone",
            "Orbital mechanics for undergraduates, twenty-seventh edition. Transfer windows, two-body " +
            "assumptions, the patched conic. The worked examples use the same constants the ship's own " +
            "computer does, because they are the same constants.",
            "the mechanics text, 27th ed. — the ship's own constants, taught to teenagers"),

        new Entry(9, "the-materials-reference",
            "a reference thick enough to stop a door",
            "Structural materials, thirty-first edition: alloys, ceramics, fatigue tables. The " +
            "regolith-composites chapter is the most thumbed. The chapter on materials with no known failure " +
            "mode is three pages long, and someone has written a question mark on the last one.",
            "the materials reference — a question mark in the no-known-failure-mode chapter"),

        new Entry(10, "the-fat-paperback",
            "a fat paperback, cover creased white",
            "Far-future space opera: a ship with a mind, a name too long for its spine, a civilisation so " +
            "comfortable it has to invent its own emergencies. Shelved here without a catalog number — " +
            "this one is nobody's work.",
            "the fat paperback — the only book here that is nobody's work"),
    ];

    /// <summary>The two REFERENCE TEXTS — the labs' own working books, in their 27th and 31st editions
    /// because the year is what it is. They are the only entries whose odds depend on where you are
    /// standing.</summary>
    public static bool IsReferenceText(Entry book) => book.Number is 8 or 9;

    /// <summary>DOES THIS FLOOR READ FOR WORK — the one predicate the weighting asks, so there is exactly one
    /// place to argue with it.
    ///
    /// <para>The repo has no <c>Works</c> kind, so the catalog's "Laboratory/Works-flavoured" is mapped onto
    /// the two kinds in <see cref="UndergroundComplex.Kind"/> whose door vocabulary is about doing something
    /// with numbers and materials rather than about filing, grading or treating people: the LABORATORY
    /// (ASSAY, CALIBRATION, PATTERN INTEGRITY, COLD STORE) and the TRANSIT STATION (LOADING, BONDED HOLD,
    /// OUTBOUND — the one place in the building where somebody needs a transfer window).</para>
    ///
    /// <para>The records annex, the processing depot, the clinic and the head office are all clerical or
    /// medical, and a structural-materials reference on their shelves is exactly as odd as everything else on
    /// them — which is the point of the other eight entries.</para>
    ///
    /// <para>Asked of <see cref="UndergroundComplex.KindOn"/> and not of <c>KindFor</c>, so the band nobody
    /// listed reads by ITS OWN kind: a laboratory under a records annex has laboratory shelves.</para></summary>
    public static bool ReadsForWork(UndergroundComplex.Kind kind) =>
        kind is UndergroundComplex.Kind.Laboratory or UndergroundComplex.Kind.TransitStation;

    /// <summary>What the ROOM says instead of the empty line. The authored shelf fragment is carried inside a
    /// house frame character-for-character — the frame is this file's prose, the fragment is not.</summary>
    public static string ShelfLine(Entry book) =>
        $"{Glyph} Stripped like every other room on this floor, except the shelf, which still has one thing " +
        $"on it: {book.Shelf}. You can read it where you stand.";

    /// <summary>What the look-card is titled. The shelf line, upright, with nothing added to it — a captain
    /// reads the same words on the card that the room just said.</summary>
    public static string CardTitle(Entry book) => $"{Glyph} {book.Shelf}";

    /// <summary>WHICH book is on this room's shelf. Seeded on site + floor + room, so a shelf is that shelf
    /// forever and a captain who walks back to it finds the same thing on it.
    ///
    /// <para>Weighted, and only in one direction: on a floor that <see cref="ReadsForWork"/> the two
    /// reference texts count triple. Everything stays reachable everywhere — a fat paperback in a laboratory
    /// is a fact about a person, and weighting the work books to exclusivity would turn a shelf into a
    /// label.</para></summary>
    public static Entry Which(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        bool atWork = ReadsForWork(UndergroundComplex.KindOn(bodyId, level));
        int total = 0;
        foreach (Entry e in Catalog)
        {
            total += Weight(e, atWork);
        }

        int ticket = DiceRule.Roll(
            DiceRule.Seed($"hive:oddbook-which:{bodyId}:{level}:{roomIndex}"), total).Face;
        foreach (Entry e in Catalog)
        {
            ticket -= Weight(e, atWork);
            if (ticket <= 0)
            {
                return e;
            }
        }

        return Catalog[^1];   // unreachable: the ticket is drawn from the same total the walk spends.
    }

    private static int Weight(Entry book, bool readsForWork) =>
        readsForWork && IsReferenceText(book) ? 3 : 1;

    /// <summary>IS THERE A BOOK IN THIS ROOM. False for every room that holds anything at all — the book is
    /// what a WOULD-BE-EMPTY room has instead of the empty line, never a second find laid on top of a
    /// pallet or a file. And false for five would-be-empty rooms in six.</summary>
    public static bool HoldsOne(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UndergroundComplex.InRoom(bodyId, level, roomIndex) == UndergroundComplex.Haul.Nothing
            && DiceRule.Roll(DiceRule.Seed($"hive:oddbook:{bodyId}:{level}:{roomIndex}"), Rate).Face == 1;
    }

    /// <summary>Everything one search of a shelf room answers, in one pure call.
    ///
    /// <para><see cref="Gist"/> is null when this thread has already filed this book — that is the whole of
    /// the once-per-book law, and it lives here rather than in an <c>if</c> in a partial class so a test can
    /// walk it. <see cref="Filed"/> is the new read-list either way, so the caller never has to decide.</para></summary>
    public readonly record struct Reading(
        Entry Book, string Line, string Title, string Card, string? Gist, IReadOnlyList<string> Filed);

    /// <summary>SEARCH A ROOM FOR THE ODD BOOK. Null means this room has no book in it and the ordinary haul
    /// stands — which is what happens in five would-be-empty rooms in six, and in every room that holds
    /// anything.
    ///
    /// <para><paramref name="filed"/> is the ids this game-thread has already put in the casebook.</para>
    ///
    /// <para><paramref name="forced"/> is the <c>?book=</c> dev cheat and nothing else: null is the shipped
    /// roll, 0 puts the seeded book in every would-be-empty room, and 1..10 puts THAT catalog entry in every
    /// would-be-empty room. It is an ARGUMENT to this one ask and never a second answer OR-ed in at a call
    /// site (§13.18's rule, learned the expensive way). Note what it does NOT override: a room that holds a
    /// file, a card or a pallet still holds it, because a cheat that invents a book in an occupied room would
    /// have the tester playtesting a room the game cannot produce.</para></summary>
    public static Reading? Search(
        string bodyId, int level, int roomIndex, IReadOnlyList<string>? filed, int? forced = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        bool wouldBeEmpty =
            UndergroundComplex.InRoom(bodyId, level, roomIndex) == UndergroundComplex.Haul.Nothing;
        bool here = forced is { } f
            ? wouldBeEmpty && (f == 0 || (f >= 1 && f <= Catalog.Count))
            : HoldsOne(bodyId, level, roomIndex);
        if (!here)
        {
            return null;
        }

        Entry book = forced is { } pick && pick >= 1 && pick <= Catalog.Count
            ? Catalog[pick - 1]
            : Which(bodyId, level, roomIndex);

        var read = new List<string>(filed ?? []);
        bool first = !read.Contains(book.Id, StringComparer.Ordinal);
        if (first)
        {
            read.Add(book.Id);
        }

        return new Reading(
            book, ShelfLine(book), CardTitle(book), book.Card, first ? book.Gist : null, read);
    }
}
