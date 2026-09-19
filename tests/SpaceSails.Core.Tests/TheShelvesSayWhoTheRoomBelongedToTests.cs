using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #701 · THE LIBRARY LAYER — the occupancy rule, the deal, and the canon wall.
///
/// <para>Owner: <i>"They have their work books and they have their freetime books there... provide soft
/// clues about what kind of people stay in those rooms."</i> Three things can kill this feature quietly and
/// each has its own guard below. <b>The rule</b> — a shelf in a storeroom, or a room somebody was given with
/// no shelves in it, and the layer stops being about people. <b>The deal</b> — the same paperbacks in every
/// room on a floor, and the freetime shelf is wallpaper. <b>The canon</b> — thirteen texts written by a
/// department that read everything and never found the leak, and one sentence naming what they never found
/// undoes the whole thing.</para>
///
/// <para>Every number here is MEASURED over the generated floors of the scenario's own sites plus a
/// generated sweep, never asserted off a constant. All three were watched go red — the verbatim runs are in
/// the pull request.</para>
/// </summary>
public sealed class TheShelvesSayWhoTheRoomBelongedToTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The scenario's own landable sites (<c>scenarios/sol.json</c>), the head office, and enough
    /// generated ground that a rate can be measured instead of believed — the generator is pure of any
    /// registry, so a site is a string and sixty of them is sixty buildings.</summary>
    private static IEnumerable<string> Sites()
    {
        yield return "luna";
        yield return "phobos";
        yield return "europa";
        yield return "ganymede";
        yield return "callisto";
        yield return "titan";
        yield return "enceladus";
        yield return "miranda";
        yield return "triton";
        yield return "the-clinker";
        yield return KaamosLore.IceMoonBodyId;
        for (int i = 0; i < 60; i++)
        {
            yield return $"generated-site-{i:D3}";
        }
    }

    /// <summary>One floor of one building, with everything the occupancy rule is asked in terms of.</summary>
    private readonly record struct Floor(
        string Body, int Level, UndergroundComplex.FloorPlan Plan,
        string? Department, UndergroundComplex.Kind Kind);

    private static List<Floor> EveryFloor()
    {
        var floors = new List<Floor>();
        foreach (string body in Sites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors.Add(new Floor(
                    body, level,
                    UndergroundComplex.Build(body, level, Field),
                    ChamberFitting.DepartmentOn(body, level),
                    UndergroundComplex.KindOn(body, level)));
            }
        }

        Assert.True(floors.Count > 300, $"only {floors.Count} floors swept — too few to measure anything on.");
        return floors;
    }

    // ── THE OCCUPANCY RULE ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY ROOM SOMEBODY WAS GIVEN HAS BOTH SHELVES, AND EVERY ROOM NOBODY WAS GIVEN HAS NONE.
    ///
    /// <para>The whole rule, in one sweep, and both halves fail silently on their own: a layer that puts
    /// shelves in every room would pass a "the occupied ones have shelves" test and say nothing about
    /// anybody, and a layer that placed none at all would pass an "unoccupied rooms have none" test with the
    /// feature dead.</para>
    ///
    /// <para><b>And never exactly one.</b> A room with a work shelf and no freetime shelf is the layer
    /// saying half a thing about somebody, which is worse than saying nothing — <c>Shelves.On</c>'s
    /// both-or-neither clause, pinned here because it is the clause a placement change would break.</para>
    /// </summary>
    [Fact]
    public void EveryOccupiedRoomHasBothShelvesAndEveryOtherRoomHasNone()
    {
        int occupied = 0, both = 0, half = 0, strays = 0, rooms = 0;
        foreach (Floor floor in EveryFloor())
        {
            var byRoom = new Dictionary<int, int>();
            foreach (Shelves.Shelf shelf in floor.Plan.TheShelves)
            {
                byRoom[shelf.Room] = byRoom.GetValueOrDefault(shelf.Room) + 1;
            }

            for (int r = 0; r < floor.Plan.TheRooms.Count; r++)
            {
                UndergroundComplex.Room room = floor.Plan.TheRooms[r];
                rooms++;
                int standing = byRoom.GetValueOrDefault(r);
                bool somebodys = Shelves.Occupied(in room, floor.Department, floor.Kind)
                    && OddBooks.ShelvesStandHere(floor.Body, floor.Level);

                if (!somebodys)
                {
                    strays += standing > 0 ? 1 : 0;
                    continue;
                }

                occupied++;
                both += standing == 2 ? 1 : 0;
                half += standing == 1 ? 1 : 0;
            }
        }

        Assert.True(rooms > 3000, $"only {rooms} rooms swept — nothing to measure a rule on.");
        Assert.True(occupied > 600,
            $"only {occupied} of {rooms} rooms are somebody's — the rule has stopped finding people.");
        Assert.True(occupied < rooms,
            $"all {rooms} rooms are somebody's — a storeroom has an occupant, and the rule says nothing.");
        Assert.True(strays == 0,
            $"{strays} room(s) nobody was given hold shelves — a storeroom with somebody's paperbacks in " +
            "it is the layer saying something about a person who was never there.");
        Assert.True(half == 0,
            $"{half} room(s) hold exactly one shelf — half a thing about somebody is worse than nothing.");
        Assert.True(both == occupied,
            $"{both} of {occupied} rooms somebody was given have both shelves; {occupied - both} have " +
            "none. A room somebody sat in every day with nothing on its walls is this layer not shipped.");
    }

    /// <summary>
    /// A STOREROOM HAS NO OCCUPANT, AND NEITHER DOES A GALLERY. The two clauses of the rule that say what it
    /// is NOT, asked of the ladder directly so they cannot be satisfied by a placer that happened to find no
    /// wall.
    /// </summary>
    [Fact]
    public void NobodyIsGivenAStoreAGalleryOrTheStoreThatSaysItIsEmpty()
    {
        foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
        {
            Assert.Equal(Shelves.Post.None, Shelves.PostIn("", null, kind));
            Assert.Equal(
                Shelves.Post.None, Shelves.PostIn(ChamberFitting.EmptyStorePlate, "ADMINISTRATION", kind));

            foreach (string store in new[] { "LONG STORAGE", "DEEP STORAGE" })
            {
                Assert.Equal(Shelves.Post.None, Shelves.PostIn("COLD STORE 2", store, kind));
            }
        }

        // …and the band nobody dug carries no shelf at all, whatever its rooms are plated — the odd book's
        // own question, asked here so the two features cannot drift apart about the halls.
        var halls = 0;
        foreach (Floor floor in EveryFloor())
        {
            if (!UndergroundComplex.IsFound(floor.Body, floor.Level))
            {
                continue;
            }
            halls++;
            Assert.Empty(floor.Plan.TheShelves);
        }
        Assert.True(halls > 0, "no gallery floors in the sweep — the halls clause proved nothing.");
    }

    /// <summary>
    /// THE RULE IS A FACT ABOUT THE PLATE AND THE DEPARTMENT, NOT A ROLL. Written as its own guard because
    /// "derived, never stored" is the whole of what the owner's brief asked for and a seeded occupancy would
    /// satisfy every other test in this file.
    /// </summary>
    [Fact]
    public void WhoseRoomItIsIsReadOffTheBuildingAndNeverRolled()
    {
        foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
        {
            foreach (string plate in UndergroundComplex.SignsFor(kind))
            {
                foreach (string? department in
                    new string?[] { null, "LABORATORIES", "ADMINISTRATION", "PLANT", "ISOLATION", "UNMARKED" })
                {
                    Shelves.Post first = Shelves.PostIn(plate, department, kind);
                    Assert.Equal(first, Shelves.PostIn(plate, department, kind));

                    // The two rungs above the kit ladder, stated rather than restated back from the shipped
                    // composition: a plate about who comes through the door is the guard's, and the one
                    // reading room in the game is the department that reads everything.
                    if (Shelves.IsAPost(plate))
                    {
                        Assert.Equal(Shelves.Post.Security, first);
                    }
                }
            }
        }

        Assert.Equal(
            Shelves.Post.ReadingRoom,
            Shelves.PostIn(
                "PRIVILEGED RECORDS · READING ROOM", "ADMINISTRATION",
                UndergroundComplex.Kind.RecordsAnnex));
        Assert.Contains("PRIVILEGED RECORDS · READING ROOM", UndergroundComplex.ParkViewPlates);
    }

    /// <summary>EVERY POST IS REACHABLE. Six authored work shelves of which the building can only produce
    /// four is four shelves and two written for nobody — and the reading room is the one that would go
    /// missing, because it hangs on a single plate in a single register.</summary>
    [Fact]
    public void EveryWorkShelfAndEveryPersonaTurnsUpSomewhereInTheSystem()
    {
        var work = new HashSet<string>(StringComparer.Ordinal);
        var free = new HashSet<string>(StringComparer.Ordinal);
        foreach (Floor floor in EveryFloor())
        {
            foreach (Shelves.Shelf shelf in floor.Plan.TheShelves)
            {
                (shelf.IsFreetime ? free : work).Add(shelf.Of.Id);
            }
        }

        Assert.Equal(
            Shelves.Work.Select(e => e.Id).OrderBy(s => s, StringComparer.Ordinal),
            work.OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal(
            Shelves.Freetime.Select(e => e.Id).OrderBy(s => s, StringComparer.Ordinal),
            free.OrderBy(s => s, StringComparer.Ordinal));
    }

    // ── THE DEAL ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NO TWO ROOMS IN A ROW SHARE A PERSONA, AND NONE REPEATS UNTIL ALL SEVEN HAVE BEEN SEEN.
    ///
    /// <para>The catalog asks for <i>never two alike on one floor</i>. A floor has far more than seven
    /// occupied rooms on it, so the literal form of that is arithmetically impossible and this is the honest
    /// form: a DEAL. The seven are shuffled and handed out in order, and only when all seven are gone are
    /// they shuffled again.</para>
    /// </summary>
    [Fact]
    public void TheFreetimeShelvesAreDealtAndNeverRepeatUntilAllSevenHaveBeenSeen()
    {
        int floorsWithAFullHand = 0, dealt = 0;
        foreach (Floor floor in EveryFloor())
        {
            List<string> hand =
                [.. floor.Plan.TheShelves.Where(s => s.IsFreetime).Select(s => s.Of.Id)];
            dealt += hand.Count;

            for (int i = 1; i < hand.Count; i++)
            {
                Assert.False(
                    string.Equals(hand[i], hand[i - 1], StringComparison.Ordinal),
                    $"B{-floor.Level} of {floor.Body}: rooms {i - 1} and {i} share `{hand[i]}` — two rooms " +
                    "in a row with the same paperbacks is the moment the layer stops saying anything.");
            }

            for (int cut = 0; cut + Shelves.Personas <= hand.Count; cut += Shelves.Personas)
            {
                int distinct = hand.Skip(cut).Take(Shelves.Personas)
                    .Distinct(StringComparer.Ordinal).Count();
                Assert.True(distinct == Shelves.Personas,
                    $"B{-floor.Level} of {floor.Body}: the hand dealt at room {cut} holds {distinct} of " +
                    $"{Shelves.Personas} personas — a persona repeated before the deal was spent.");
                floorsWithAFullHand++;
            }
        }

        Assert.True(dealt > 600, $"only {dealt} freetime shelves dealt — nothing to measure a deal on.");
        Assert.True(floorsWithAFullHand > 80,
            $"only {floorsWithAFullHand} full hands were dealt anywhere — the cycle law proved little.");
    }

    /// <summary>A SHELF IS THAT SHELF FOREVER. Determinism is law in Core and here it is also the design: a
    /// captain who walks back into a room finds the same two books on the same two walls.</summary>
    [Fact]
    public void AFloorStandsTheSameShelvesEveryVisit()
    {
        foreach (Floor floor in EveryFloor().Where(f => f.Plan.TheShelves.Count > 0).Take(120))
        {
            IReadOnlyList<Shelves.Shelf> again =
                UndergroundComplex.Build(floor.Body, floor.Level, Field).TheShelves;
            Assert.Equal(floor.Plan.TheShelves.Count, again.Count);
            for (int i = 0; i < again.Count; i++)
            {
                Assert.Equal(floor.Plan.TheShelves[i], again[i]);
            }
        }
    }

    // ── THE CATALOG AND THE CANON ────────────────────────────────────────────────────────────────────────

    /// <summary>THE THIRTEEN AUTHORED TEXTS ARE THE ONLY SHELF PROSE THERE IS. Everything a captain can be
    /// shown about a shelf is an authored fragment lifted whole, carried by the one house glyph — proved by
    /// taking the fragment back out of the plate and asking how many different remainders there are.</summary>
    [Fact]
    public void NothingIsEverShownAboutAShelfThatIsNotInTheCatalog()
    {
        Assert.Equal(6, Shelves.Work.Count);
        Assert.Equal(7, Shelves.Freetime.Count);
        Assert.Equal(Shelves.Personas, Shelves.Freetime.Count);

        List<Shelves.Entry> all = [.. Shelves.Work, .. Shelves.Freetime];
        Assert.Equal(all.Count, all.Select(e => e.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(all.Count, all.Select(e => e.Shelf).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(all.Count, all.Select(e => e.Gist).Distinct(StringComparer.Ordinal).Count());

        // The ids are namespaced against the odd book's, because one read-list carries both halves of #701.
        var books = OddBooks.Catalog.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        foreach (Shelves.Entry entry in all)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Shelf));
            Assert.False(string.IsNullOrWhiteSpace(entry.Card));
            Assert.False(string.IsNullOrWhiteSpace(entry.Gist));
            Assert.DoesNotContain(entry.Id, books);
            Assert.True(
                entry.Id.StartsWith("work:", StringComparison.Ordinal)
                || entry.Id.StartsWith("free:", StringComparison.Ordinal),
                $"`{entry.Id}` is not namespaced — one read-list carries the books and the shelves.");
        }

        var frames = new HashSet<string>(StringComparer.Ordinal);
        var cards = all.Select(e => e.Card).ToHashSet(StringComparer.Ordinal);
        var gists = all.Select(e => e.Gist).ToHashSet(StringComparer.Ordinal);
        foreach (Floor floor in EveryFloor().Where(f => f.Plan.TheShelves.Count > 0).Take(200))
        {
            foreach (Shelves.Shelf shelf in floor.Plan.TheShelves)
            {
                Assert.Contains(shelf.Of.Shelf, shelf.Plate, StringComparison.Ordinal);
                Assert.Contains(shelf.Card, cards);
                Assert.Contains(shelf.Gist, gists);
                frames.Add(shelf.Plate.Replace(shelf.Of.Shelf, "<shelf>", StringComparison.Ordinal));
            }
        }

        Assert.True(frames.Count == 1,
            $"{frames.Count} different room frames — the shelf line is one authored fragment behind one " +
            "house glyph, not prose per shelf.");
    }

    /// <summary>
    /// NO SHELF EVER NAMES THE THING THE DEPARTMENT NEVER FOUND.
    ///
    /// <para><see cref="TheEmptyRoomThatHoldsOneBookTests.NoBookNamesTheMonolithTheOldOnesTheReeversOrTheHalls"/>'s
    /// list, extended to the occupants' shelves — the same words, because it is the same department and the
    /// same joke: they read all of this and found nothing, and that fact is nowhere stated.</para>
    ///
    /// <para><b>Not on the list, deliberately: "collision".</b> Persona 7 is the famous wrong book and says
    /// the planets used to be somewhere else, which is a real mid-century title argued with in the margins —
    /// the game's own register looking back at itself, exactly as <i>cyclopean</i> is in the odd book's entry
    /// 6. The reserved words are the ones that would make a shelf name THIS world's canon.</para>
    /// </summary>
    [Fact]
    public void NoShelfNamesTheMonolithTheOldOnesTheReeversOrTheHalls()
    {
        string[] reserved =
        [
            // §8 / §13.8 — the objects this game has and never explains.
            "monolith", "old one", "reever", "the ancients", "watcher",
            // The Hive's own list, for the reason it exists there.
            "restore", "backup", "revive", "resurrect", "clone", "slave",
            // §10 — the found halls (#677). The staff never found the leak either.
            "found hall", "the halls", "galleries", "below-keeper", "sipapu", "emergence",
            "fourth world", "fourth run", "caretaker", "spared into",
        ];

        var prose = Shelves.AllProse().ToList();
        Assert.True(prose.Count >= 4 * (Shelves.Work.Count + Shelves.Freetime.Count),
            $"the sweep reads {prose.Count} strings — AllProse is not publishing every sentence.");

        foreach (string line in prose)
        {
            foreach (string bad in reserved)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // …and the edition numbers stay. They do a whole future's worth of work in two digits, and a lane
        // tidying prose is exactly how one goes missing.
        Assert.Contains(
            Shelves.Work, e => e.Shelf.Contains("twenty-seventh", StringComparison.Ordinal));
        Assert.Contains(
            Shelves.Work, e => e.Card.Contains("Twenty-seven editions", StringComparison.Ordinal));
        Assert.Contains(
            Shelves.Work, e => e.Gist.Contains("twenty-seventh edition", StringComparison.Ordinal));
    }

    // ── THE ONE-SHOT, AND WHAT THE BOOK IS TOLD ──────────────────────────────────────────────────────────

    /// <summary>LOOKING IS FREE, KNOWLEDGE IS ONE-SHOT (#603) — and once per SHELF rather than per room,
    /// exactly as the odd book files once per book. The clerk's shelf is the clerk's shelf in every records
    /// annex in the system, and a book that filed it eleven times would be keeping a tally of corridors.
    ///
    /// <para>Walked across the whole system on ONE thread, the way a captain walks it, and against the odd
    /// book's own read-list — which is the store both halves of this issue share.</para></summary>
    [Fact]
    public void TheGistFilesExactlyOncePerThread()
    {
        List<Shelves.Shelf> standing =
            [.. EveryFloor().SelectMany(f => f.Plan.TheShelves)];
        Assert.True(standing.Count > 600, $"only {standing.Count} shelves found — nothing to walk.");

        IReadOnlyList<string> filed = [];
        var gists = new List<string>();
        foreach (Shelves.Shelf shelf in standing)
        {
            Shelves.Reading read = Shelves.Read(shelf, filed);

            // The card is always there, whether or not anything files.
            Assert.Equal(shelf.Of.Card, read.Card);
            Assert.Equal(shelf.Plate, read.Title);

            if (read.Gist is { } gist)
            {
                gists.Add(gist);
            }
            filed = read.Filed;
        }

        int different = gists.Distinct(StringComparer.Ordinal).Count();
        Assert.True(gists.Count == different,
            $"{standing.Count} shelves read, {gists.Count} gist(s) filed and only {different} of them " +
            "different — the casebook is learning the same shelf more than once.");
        Assert.Equal(Shelves.Work.Count + Shelves.Freetime.Count, gists.Count);
        Assert.Equal(Shelves.Work.Count + Shelves.Freetime.Count, filed.Count);

        // And re-reading a shelf the thread already knows files NOTHING, forever.
        foreach (Shelves.Shelf shelf in standing.Take(80))
        {
            Shelves.Reading again = Shelves.Read(shelf, filed);
            Assert.Null(again.Gist);
            Assert.Equal(filed.Count, again.Filed.Count);
            Assert.Equal(shelf.Of.Card, again.Card);   // the card still opens
        }

        // A thread that has already read BOOKS keeps them: one store, two features, no collision.
        IReadOnlyList<string> withBooks = [.. OddBooks.Catalog.Select(e => e.Id)];
        Shelves.Reading beside = Shelves.Read(standing[0], withBooks);
        Assert.NotNull(beside.Gist);
        Assert.Equal(withBooks.Count + 1, beside.Filed.Count);
        foreach (string book in withBooks)
        {
            Assert.Contains(book, beside.Filed);
        }
    }

    /// <summary>#741 · THE ENTRY DECLARES ITS SUBJECT, AND THE SUBJECT IS THE PLACE. A shelf names nobody,
    /// so no personal subject may ever be minted from one — a heading naming a stranger the game has not
    /// printed is the one thing #741 exists to refuse, and a shelf is the most tempting seam it has, because
    /// the whole feature is about somebody.</summary>
    [Fact]
    public void TheCasebookEntryDeclaresItsSubjectAndItIsThePlace()
    {
        const string Place = "Miranda · The Ridge Camp";
        var note = new FieldNote(
            Shelves.Work[0].Gist, 0.0, Place, Shelves.Glyph, Shelves.SubjectsFor(Place));

        IReadOnlyList<CaseSubjects.Subject> read = CaseSubjects.On(in note);
        Assert.Single(read);
        Assert.Equal(CaseSubjects.Kind.Place, read[0].Of);
        Assert.Equal(Place, read[0].Name);
        Assert.DoesNotContain(read, s => s.Of == CaseSubjects.Kind.Person);
    }
}
