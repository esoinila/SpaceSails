using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #701 · THE ODD BOOK — and the two things that can kill it.
///
/// <para>Owner: <i>"a better alternative to finding an empty room. You look around but only one book catches
/// your attention."</i> The first thing that kills it is GENEROSITY — a shelf in every empty room turns the
/// walk into a shopping trip and spends the surprise inside one floor. The second is CANON: ten books written
/// by a department that reads everything and never found the leak, and one sentence naming what they never
/// found would undo the whole thing quietly.</para>
///
/// <para>So the split is <b>measured over generated sites</b> rather than asserted off the constant, and the
/// prose is grepped. Both were watched go red — see the PR body.</para>
/// </summary>
public sealed class TheEmptyRoomThatHoldsOneBookTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Enough generated ground that a rate can be measured instead of believed. Real landables plus
    /// synthetic ids, because the generator is pure of any registry — a site is a string, and sixty of them
    /// is sixty buildings.</summary>
    private static IEnumerable<string> Sites()
    {
        yield return "luna";
        yield return "phobos";
        yield return "europa";
        yield return "ganymede";
        yield return "callisto";
        yield return "titan";
        yield return "miranda";
        yield return "triton";
        yield return "the-clinker";
        yield return KaamosLore.IceMoonBodyId;
        for (int i = 0; i < 60; i++)
        {
            yield return $"generated-site-{i:D3}";
        }
    }

    private readonly record struct Room(string Body, int Level, int Index);

    private static List<Room> EveryRoom()
    {
        var rooms = new List<Room>();
        foreach (string body in Sites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                for (int i = 0; i < plan.RoomCentres.Count; i++)
                {
                    rooms.Add(new Room(body, level, i));
                }
            }
        }

        Assert.True(rooms.Count > 2000, $"only {rooms.Count} rooms swept — too few to measure a rate on.");
        return rooms;
    }

    // ── THE LOAD-BEARING LAW ─────────────────────────────────────────────────────────────────────────────

    /// <summary>MOST EMPTIES STAY EMPTY, AND SOME OF THEM DO NOT. Measured, not assumed.
    ///
    /// <para>Both halves matter and each has its own way of failing silently. A rate of zero is the shipped
    /// behaviour — the empty line, every time — and every other test in this file would still pass on it. A
    /// rate near one is the shopping trip.</para></summary>
    [Fact]
    public void OneWouldBeEmptyRoomInSixHoldsABookAndTheRestAreStillEmpty()
    {
        List<Room> rooms = EveryRoom();
        int empties = 0;
        int books = 0;
        foreach (Room r in rooms)
        {
            if (UndergroundComplex.InRoom(r.Body, r.Level, r.Index) == UndergroundComplex.Haul.Nothing)
            {
                empties++;
            }
            if (OddBooks.HoldsOne(r.Body, r.Level, r.Index))
            {
                books++;
            }
        }

        Assert.True(empties > 400, $"only {empties} would-be-empty rooms in {rooms.Count} — nothing to measure.");
        Assert.True(books > 0,
            $"{empties} would-be-empty room(s) swept and not one of them holds a book — the feature is dead.");

        double rate = (double)books / empties;
        Assert.True(rate is > 0.12 and < 0.22,
            $"the odd book turns up in {books} of {empties} would-be-empty rooms ({rate:P1}) — the law is " +
            $"one in {OddBooks.Rate} ({1.0 / OddBooks.Rate:P1}).");

        // And the whole building, not just the empty half of it: the room that pays nothing is still the
        // commonest thing down here, and the shelf is still a surprise.
        Assert.True(books * 3 < empties,
            $"{books} of {empties} would-be-empty rooms have something in them — that is a shopping trip.");
        Assert.True((double)books / rooms.Count < 0.08,
            $"{books} of {rooms.Count} rooms in the whole building hold a book — too many to be odd.");
    }

    /// <summary>A BOOK IS NEVER LAID ON TOP OF A FIND. It is what a would-be-empty room has INSTEAD of the
    /// empty line, so a room holding a file, a card, a crate or the pallet must never also hold a shelf —
    /// including under the cheat, which is a gate and not an invention.</summary>
    [Fact]
    public void ARoomThatHoldsSomethingNeverAlsoHoldsABook()
    {
        foreach (Room r in EveryRoom())
        {
            if (UndergroundComplex.InRoom(r.Body, r.Level, r.Index) == UndergroundComplex.Haul.Nothing)
            {
                continue;
            }

            Assert.False(OddBooks.HoldsOne(r.Body, r.Level, r.Index));
            Assert.Null(OddBooks.Search(r.Body, r.Level, r.Index, null));
            Assert.Null(OddBooks.Search(r.Body, r.Level, r.Index, null, forced: 0));
            for (int entry = 1; entry <= OddBooks.Catalog.Count; entry++)
            {
                Assert.Null(OddBooks.Search(r.Body, r.Level, r.Index, null, forced: entry));
            }
        }
    }

    /// <summary>A SHELF IS THAT SHELF FOREVER. Determinism is law in Core and it is also the design: a
    /// captain who walks back to a room they searched finds the same thing on it.</summary>
    [Fact]
    public void TheSameRoomAlwaysHoldsTheSameBook()
    {
        foreach (Room r in EveryRoom().Where(r => OddBooks.HoldsOne(r.Body, r.Level, r.Index)).Take(200))
        {
            OddBooks.Reading first = OddBooks.Search(r.Body, r.Level, r.Index, null)!.Value;
            OddBooks.Reading again = OddBooks.Search(r.Body, r.Level, r.Index, null)!.Value;
            Assert.Equal(first.Book.Id, again.Book.Id);
            Assert.Equal(first.Line, again.Line);
        }
    }

    // ── THE CATALOG ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE TEN AUTHORED TEXTS ARE THE ONLY BOOK PROSE THERE IS.
    ///
    /// <para>Everything a captain can be shown about a shelf is either an authored fragment lifted whole or
    /// the ONE house frame that carries it. The frame is proved to be one frame by taking the authored
    /// fragment back out of it: what is left must be identical for all ten, or somebody has written prose
    /// for a book.</para></summary>
    [Fact]
    public void NothingIsEverShownAboutABookThatIsNotInTheCatalog()
    {
        Assert.Equal(10, OddBooks.Catalog.Count);
        Assert.Equal(Enumerable.Range(1, 10), OddBooks.Catalog.Select(e => e.Number));
        Assert.Equal(10, OddBooks.Catalog.Select(e => e.Id).Distinct(StringComparer.Ordinal).Count());

        var frames = new HashSet<string>(StringComparer.Ordinal);
        foreach (OddBooks.Entry book in OddBooks.Catalog)
        {
            Assert.False(string.IsNullOrWhiteSpace(book.Shelf));
            Assert.False(string.IsNullOrWhiteSpace(book.Card));
            Assert.False(string.IsNullOrWhiteSpace(book.Gist));

            string line = OddBooks.ShelfLine(book);
            string title = OddBooks.CardTitle(book);

            // The fragment is carried character-for-character, not paraphrased into a sentence.
            Assert.Contains(book.Shelf, line, StringComparison.Ordinal);
            Assert.Equal($"{OddBooks.Glyph} {book.Shelf}", title);

            frames.Add(line.Replace(book.Shelf, "<shelf>", StringComparison.Ordinal));
        }

        Assert.True(frames.Count == 1,
            $"{frames.Count} different room frames — the shelf line is supposed to be one house sentence " +
            "with an authored fragment in it, not prose per book.");

        // And nothing a search can return carries text from anywhere else.
        var cards = OddBooks.Catalog.Select(e => e.Card).ToHashSet(StringComparer.Ordinal);
        var gists = OddBooks.Catalog.Select(e => e.Gist).ToHashSet(StringComparer.Ordinal);
        foreach (Room r in EveryRoom().Where(r => OddBooks.HoldsOne(r.Body, r.Level, r.Index)))
        {
            OddBooks.Reading read = OddBooks.Search(r.Body, r.Level, r.Index, null)!.Value;
            Assert.Contains(read.Card, cards);
            Assert.Contains(read.Gist!, gists);
            Assert.Equal(OddBooks.ShelfLine(read.Book), read.Line);
            Assert.Equal(OddBooks.CardTitle(read.Book), read.Title);
        }
    }

    /// <summary>NO BOOK EVER NAMES THE THING THE DEPARTMENT NEVER FOUND.
    ///
    /// <para>The same law <c>TheHiveTests.NothingDownHereEXPLAINSAnything</c> and
    /// <see cref="NothingOutHereEverSaysWhatItWasTests"/> hold everything else on this ground to, extended to
    /// the shelves. A staff that reads everything looking for leaks about the before-worlds, and a shelf that
    /// contains one, is a shelf that has answered the question the whole game is about.</para>
    ///
    /// <para><b>What is deliberately NOT on this list: "cyclopean".</b> Entry 6 is a weird-tales collection
    /// and says <i>cyclopean cities</i>, which is the pastiche doing its job — the game's own register
    /// looking back at itself out of a cheap paperback. The reserved words are the ones that would make a
    /// book name THIS world's canon, not the ones its own genre owns.</para></summary>
    [Fact]
    public void NoBookNamesTheMonolithTheOldOnesTheReeversOrTheHalls()
    {
        string[] reserved =
        [
            // §8 / §13.8 — the objects this game has and never explains.
            "monolith", "old one", "reever", "the ancients", "watcher",
            // The Hive's own list, for the reason it exists there.
            "restore", "backup", "revive", "resurrect", "clone", "slave",
            // §10 — the found halls (#677). The staff never found the leak either; a book carrying this
            // vocabulary IS the leak.
            "found hall", "the halls", "galleries", "below-keeper", "sipapu", "emergence",
            "fourth world", "fourth run", "caretaker", "spared into",
        ];

        var prose = new List<string>();
        foreach (OddBooks.Entry book in OddBooks.Catalog)
        {
            prose.Add(book.Shelf);
            prose.Add(book.Card);
            prose.Add(book.Gist);
            prose.Add(OddBooks.ShelfLine(book));
            prose.Add(OddBooks.CardTitle(book));
        }

        foreach (string line in prose)
        {
            foreach (string bad in reserved)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>EVERY ENTRY CAN ACTUALLY BE DRAWN. Ten authored books of which the seed can only ever reach
    /// seven is seven books and three that were written for nobody.</summary>
    [Fact]
    public void EveryBookInTheCatalogIsReachable()
    {
        var drawn = EveryRoom()
            .Where(r => OddBooks.HoldsOne(r.Body, r.Level, r.Index))
            .Select(r => OddBooks.Which(r.Body, r.Level, r.Index).Id)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            OddBooks.Catalog.Select(e => e.Id).OrderBy(s => s, StringComparer.Ordinal),
            drawn.OrderBy(s => s, StringComparer.Ordinal));
    }

    /// <summary>THE WORK BOOKS ARE COMMONER WHERE THE WORK IS — and never exclusive to it, either way. A
    /// weighting nobody can measure is a comment; a weighting that becomes a rule turns a shelf into a
    /// label.</summary>
    [Fact]
    public void TheTwoReferenceTextsWeighHeavierOnAFloorThatReadsForWork()
    {
        int workRooms = 0, workRefs = 0, elseRooms = 0, elseRefs = 0;
        var workSeen = new HashSet<string>(StringComparer.Ordinal);
        var elseSeen = new HashSet<string>(StringComparer.Ordinal);

        foreach (Room r in EveryRoom())
        {
            OddBooks.Entry book = OddBooks.Which(r.Body, r.Level, r.Index);
            bool reference = OddBooks.IsReferenceText(book);
            if (OddBooks.ReadsForWork(UndergroundComplex.KindOn(r.Body, r.Level)))
            {
                workRooms++;
                workRefs += reference ? 1 : 0;
                workSeen.Add(book.Id);
            }
            else
            {
                elseRooms++;
                elseRefs += reference ? 1 : 0;
                elseSeen.Add(book.Id);
            }
        }

        Assert.True(workRooms > 300 && elseRooms > 300,
            $"the two buckets are {workRooms} / {elseRooms} — one of them is too thin to compare.");

        double atWork = (double)workRefs / workRooms;
        double elsewhere = (double)elseRefs / elseRooms;
        Assert.True(atWork > elsewhere * 1.5,
            $"the reference texts are {atWork:P1} of shelves where the work is and {elsewhere:P1} " +
            "elsewhere — that is not a weighting.");

        // Everything stays reachable everywhere. A fat paperback in a laboratory is a fact about a person.
        Assert.Equal(10, workSeen.Count);
        Assert.Equal(10, elseSeen.Count);
    }

    /// <summary>The mapping itself, written down where changing it is a decision somebody made. The repo has
    /// no <c>Works</c> kind, so the catalog's "Laboratory/Works-flavoured" is the two kinds whose door
    /// vocabulary is about numbers and materials rather than about filing, grading or treating people.</summary>
    [Fact]
    public void OnlyTheLaboratoryAndTheTransitStationReadForWork()
    {
        foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
        {
            Assert.Equal(
                kind is UndergroundComplex.Kind.Laboratory or UndergroundComplex.Kind.TransitStation,
                OddBooks.ReadsForWork(kind));
        }
    }

    // ── THE ONE-SHOT ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>LOOKING IS FREE, KNOWLEDGE IS ONE-SHOT (#603). The card comes up every time; the casebook
    /// learns the gist once per book per thread — and "per book", not "per room", so the same title found on
    /// two moons is still one line in the book.</summary>
    [Fact]
    public void TheGistFilesExactlyOncePerThread()
    {
        List<Room> shelves = [.. EveryRoom().Where(r => OddBooks.HoldsOne(r.Body, r.Level, r.Index))];
        Assert.True(shelves.Count > 40, $"only {shelves.Count} shelves found — nothing to walk.");

        IReadOnlyList<string> filed = [];
        var filedGists = new List<string>();
        foreach (Room r in shelves)
        {
            OddBooks.Reading read = OddBooks.Search(r.Body, r.Level, r.Index, filed)!.Value;

            // The card is always there, whether or not anything files.
            Assert.Equal(read.Book.Card, read.Card);
            Assert.Equal(OddBooks.ShelfLine(read.Book), read.Line);

            if (read.Gist is { } gist)
            {
                filedGists.Add(gist);
            }
            filed = read.Filed;
        }

        int different = filedGists.Distinct(StringComparer.Ordinal).Count();
        Assert.True(filedGists.Count == different,
            $"{shelves.Count} shelves read, {filedGists.Count} gist(s) filed and only {different} of them " +
            "different — the casebook is learning the same book more than once.");
        Assert.True(filedGists.Count == OddBooks.Catalog.Count,
            $"{shelves.Count} shelves read across the whole system and {filedGists.Count} gist(s) filed — " +
            $"the catalog has {OddBooks.Catalog.Count} books in it and a thread learns each of them once.");
        Assert.Equal(OddBooks.Catalog.Count, filed.Count);

        // And re-reading a shelf the thread already knows files NOTHING, forever.
        foreach (Room r in shelves.Take(50))
        {
            OddBooks.Reading again = OddBooks.Search(r.Body, r.Level, r.Index, filed)!.Value;
            Assert.Null(again.Gist);
            Assert.Equal(filed.Count, again.Filed.Count);
            Assert.Equal(again.Book.Card, again.Card);   // the card still opens
        }
    }

    // ── THE CHEAT ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A SCENE NOBODY CAN REACH ON DEMAND SHIPS BROKEN — and the cheat is an ARGUMENT to the one
    /// ask, never a second answer beside it (§13.18). It takes the one-in-six gate off and does nothing
    /// else: it cannot put a book in an occupied room, and it cannot change what a book says.</summary>
    [Fact]
    public void TheCheatOpensEveryWouldBeEmptyRoomAndInventsNothing()
    {
        List<Room> rooms = [.. EveryRoom().Take(600)];
        int empties = 0;
        foreach (Room r in rooms)
        {
            bool empty =
                UndergroundComplex.InRoom(r.Body, r.Level, r.Index) == UndergroundComplex.Haul.Nothing;
            empties += empty ? 1 : 0;

            OddBooks.Reading? any = OddBooks.Search(r.Body, r.Level, r.Index, null, forced: 0);
            Assert.Equal(empty, any is not null);
            if (any is { } seeded)
            {
                // Seeded selection, unchanged: ?book=on is the shipped roll with the gate off.
                Assert.Equal(OddBooks.Which(r.Body, r.Level, r.Index).Id, seeded.Book.Id);
            }

            for (int entry = 1; entry <= OddBooks.Catalog.Count; entry++)
            {
                OddBooks.Reading? forced = OddBooks.Search(r.Body, r.Level, r.Index, null, forced: entry);
                Assert.Equal(empty, forced is not null);
                if (forced is { } pick)
                {
                    Assert.Equal(OddBooks.Catalog[entry - 1].Id, pick.Book.Id);
                    Assert.Equal(OddBooks.Catalog[entry - 1].Card, pick.Card);
                }
            }
        }

        Assert.True(empties > 100, $"only {empties} would-be-empty rooms in the sample — nothing was forced.");
    }
}
