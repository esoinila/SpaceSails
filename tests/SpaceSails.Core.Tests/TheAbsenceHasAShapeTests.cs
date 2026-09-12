using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1063 slice 2 · THE TWO NAMED REMAINDERS — the missing-middle absence note, and the one seal that is
/// boringly empty.
///
/// <para>The issue, on the first: <i>"A slow honest raise leaves permits, complaints, invoices; a sudden
/// burial leaves rumor. The captain can hunt the paper trail that must exist under EITHER story — and finds
/// neither. That absence has a shape: an absence in the exact shape of the thing removed — and
/// finding-the-absence is filed as its own note kind."</i></para>
///
/// <para>And on the second, under <i>Scully protection (mandatory)</i>: <i>"we SPEND one disappointment on
/// purpose: at least once, the captain forces a sealed thing early and it is exactly, boringly empty —
/// sealed ≠ full — so the pattern never hardens into proof."</i></para>
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names; the revert is
/// quoted on each one, in the shape this ground has used since #587's lesson — a guard that has never failed
/// is a guard nobody has checked.</para>
///
/// <para><b>THE WORLD THE GUARDS RUN IN IS DERIVED AND NEVER TYPED</b>, exactly as <see cref="TheBurialTests"/>
/// does it and for the same reason: a typed-in envelope is the fifth named bug class, and a burial register
/// installed by one guard must not be able to move another's world. This file owns its own id family and
/// restores the register in a <c>finally</c>.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheAbsenceHasAShapeTests
{
    /// <summary>How many generated rocks the sweeps walk to find grounds with halls — the found band is about
    /// one site in fifty, so a small sample tells you nothing. <see cref="TheBurialTests"/>' own number and
    /// reasoning.</summary>
    private const int Probes = 4000;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Grounds in this file's own id family that really carry halls — asserted to be a real
    /// population rather than merely non-empty, because a population of one proves nothing and an empty one
    /// passes every negative law here for the wrong reason.</summary>
    private static List<string> Grounds()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"absence-ground-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        Assert.True(found.Count > 40,
            $"only {found.Count} of {Probes} generated grounds had halls — this proves little.");
        return found;
    }

    /// <summary>…and the same sweep's grounds with no halls, which is what almost every site is.</summary>
    private static List<string> Plain(int want)
    {
        var plain = new List<string>();
        for (int i = 0; i < Probes && plain.Count < want; i++)
        {
            string body = $"absence-ground-{i}";
            if (!UndergroundComplex.HasFoundBand(body))
            {
                plain.Add(body);
            }
        }
        Assert.True(plain.Count >= want, $"only {plain.Count} plain grounds — the sample IS the population.");
        return plain;
    }

    private static IDisposable Buried(params string[] bodies) =>
        Installed([.. bodies], [.. bodies.Select(b => new DisclosureClock.Opening(b, 0))]);

    private static IDisposable Opened(params string[] bodies) =>
        Installed([], [.. bodies.Select(b => new DisclosureClock.Opening(b, 0))]);

    private static IDisposable Installed(
        IReadOnlyList<string> filled, IReadOnlyList<DisclosureClock.Opening> opened)
    {
        Burial.Install(filled, opened);
        return new Restore();
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose() => Burial.Install([], []);
    }

    /// <summary>The place the book files this ground under, built by the book's own formatter and never
    /// typed — a guard that spelled its own place label would agree with itself and with nothing else.</summary>
    private static string PlaceOf(string bodyId) => FieldNotes.PlaceLabel(bodyId, "The Works");

    // ══ THE ABSENCE · IT IS ITS OWN KIND ════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1063 · <b>THE ABSENCE IS FILED AS ITS OWN NOTE KIND.</b> The book tells one sort of entry from
    /// another by <see cref="FieldNote.Glyph"/> and by nothing else — the ledger card, the satchel's NOTES
    /// tab and the THREADS page all read that one field — so a kind of its own IS a glyph nothing else in
    /// the game files under.
    ///
    /// <para>Checked against every glyph Core publishes, swept by reflection rather than against a typed
    /// list: a hand-written list of six glyphs is a list the seventh is left off, and the collision this
    /// guard exists to catch would land silently in the one view where a captain goes LOOKING for repeated
    /// marks.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>MissingMiddle.Glyph</c> set to the find glyph
    /// <c>"\U0001F526"</c> — <i>"the absence note is filed under a glyph Core already publishes"</i>.</para>
    /// </summary>
    [Fact]
    public void TheAbsenceIsFiledUnderAKindOfItsOwn()
    {
        var taken = new List<(string Where, string Glyph)>();
        foreach (Type type in typeof(Burial).Assembly.GetTypes())
        {
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!f.IsLiteral || f.FieldType != typeof(string)
                    || !f.Name.Contains("Glyph", StringComparison.Ordinal)
                    || type == typeof(MissingMiddle))
                {
                    continue;
                }
                if (f.GetRawConstantValue() is string g && g.Length > 0)
                {
                    taken.Add(($"{type.Name}.{f.Name}", g));
                }
            }
        }

        // The sweep has to have found a real population, or "no collision" is a fact about an empty list.
        Assert.True(taken.Count >= 10, $"only {taken.Count} published glyphs were swept — this proves little.");

        List<string> clashes =
            [.. taken.Where(t => t.Glyph == MissingMiddle.Glyph).Select(t => t.Where)];
        Assert.True(clashes.Count == 0,
            "the absence note shares its kind with: " + string.Join(", ", clashes));

        // …and the same question asked of the SOURCE, because half the glyphs this book files under are
        // client literals typed at the filing site (the find's torch, the file, the wall, the clipping) and
        // no reflection over Core can see one of those. The mark may appear in exactly one file.
        string root = TestTree.RepoRoot();
        List<string> wearers =
        [
            .. System.IO.Directory
                .EnumerateFiles(System.IO.Path.Combine(root, "src"), "*.cs", System.IO.SearchOption.AllDirectories)
                .Concat(System.IO.Directory.EnumerateFiles(
                    System.IO.Path.Combine(root, "src"), "*.razor", System.IO.SearchOption.AllDirectories))
                .Select(p => System.IO.Path.GetRelativePath(root, p).Replace('\\', '/'))
                .Where(rel => !rel.Contains("/obj/", StringComparison.Ordinal)
                              && !rel.Contains("/bin/", StringComparison.Ordinal))
                .Where(rel => System.IO.File.ReadAllText(System.IO.Path.Combine(root, rel))
                              .Contains(MissingMiddle.Glyph, StringComparison.Ordinal))
                .OrderBy(rel => rel, StringComparer.Ordinal),
        ];
        Assert.Equal(["src/SpaceSails.Core/MissingMiddle.cs"], wearers);

        // And the note it mints really wears it, along with everything else a book entry needs.
        string body = Grounds()[0];
        using (Buried(body))
        {
            FieldNote note = MissingMiddle.Note(body, PlaceOf(body), 1234.5, "The Works");
            Assert.Equal(MissingMiddle.Glyph, note.Glyph);
            Assert.Equal(MissingMiddle.Line, note.Text);
            Assert.Equal(PlaceOf(body), note.Place);
            Assert.NotEqual("", note.Subjects);
        }
    }

    /// <summary>
    /// #1063 · <b>IT IS WRITTEN ONLY ON A BURIED GROUND, AND ONLY AFTER THE CAPTAIN'S ACT.</b> The act is
    /// working the maintenance ledger — the one paper the burial leaves — in the room it is kept in. Nowhere
    /// else on that floor, nowhere on any other floor, and nowhere at all on a ground nobody has filled in.
    ///
    /// <para>Both halves are asked of a REAL gallery-carrying ground, twice: once with nothing buried, where
    /// the answer must be no everywhere (which is what proves the guard is holding a world that can tell pass
    /// from fail), and once buried, where exactly one room answers yes.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <c>ShouldBeWritten</c> returning <c>!AlreadyMeasured(...)</c>
    /// alone, with the room condition dropped — <i>"a room on an unburied ground writes the absence"</i>;
    /// and <c>IsTheLedgerRoom</c> comparing only the level — <i>"every room on the works floor writes
    /// it"</i>.</para>
    /// </summary>
    [Fact]
    public void TheAbsenceIsWrittenOnlyOnABuriedGroundAndOnlyWhereTheLedgerIs()
    {
        int measured = 0;
        foreach (string body in Grounds().Take(12))
        {
            string place = PlaceOf(body);

            // ── NOTHING BURIED. There is no ledger, so there is no act and nothing is written anywhere.
            Assert.Null(UndergroundComplex.MaintenanceLedgerRoomFor(body));
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                for (int room = 0; room < 4; room++)
                {
                    Assert.False(MissingMiddle.ShouldBeWritten(body, level, room, [], place));
                }
            }

            // ── BURIED. Exactly one room in the whole building answers yes, and it is the ledger's.
            using (Buried(body))
            {
                (int Level, int RoomIndex) ledger = UndergroundComplex.MaintenanceLedgerRoomFor(body)!.Value;
                Assert.True(MissingMiddle.ShouldBeWritten(
                    body, ledger.Level, ledger.RoomIndex, [], place));
                measured++;

                var alsoYes = new List<string>();
                foreach (int level in UndergroundComplex.FloorsOf(body))
                {
                    for (int room = 0; room < 8; room++)
                    {
                        if ((level, room) == (ledger.Level, ledger.RoomIndex))
                        {
                            continue;
                        }
                        if (MissingMiddle.ShouldBeWritten(body, level, room, [], place))
                        {
                            alsoYes.Add($"B{level} room {room}");
                        }
                    }
                }
                Assert.True(alsoYes.Count == 0,
                    $"{body}: a second room writes the absence: " + string.Join(", ", alsoYes));
            }
        }
        Assert.Equal(12, measured);

        // ── AND NOWHERE THE LEDGER IS NOT. The measurement is downstream of the one paper the burial
        //    leaves, so a ground with no ledger room writes nothing — which is every ground in every world
        //    nobody has filled anything in, and the ordinary case by an enormous margin. (A site that never
        //    had halls can never get into the register at all: nothing down there was ever opened.)
        foreach (string plain in Plain(15))
        {
            if (UndergroundComplex.TopPressurisedFloor(plain) is not { } works)
            {
                continue;
            }
            Assert.Null(UndergroundComplex.MaintenanceLedgerRoomFor(plain));
            Assert.False(MissingMiddle.ShouldBeWritten(plain, works, 0, [], PlaceOf(plain)));
        }
    }

    /// <summary>
    /// #1063 · <b>IT IS NEVER WRITTEN TWICE.</b> One measurement per ground: a second identical line under
    /// one place reads as the book stuttering, and the thing measured is a ground rather than a room.
    ///
    /// <para>And the vacuity pair that makes it mean something: a SECOND buried ground is still owed its own
    /// entry, so this is a once-per-place law and not a once-per-captain one.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>AlreadyMeasured</c> returning <c>false</c> unconditionally —
    /// <i>"the book takes the same measurement twice on one ground"</i>.</para>
    /// </summary>
    [Fact]
    public void TheAbsenceIsNeverWrittenTwiceOnOneGround()
    {
        List<string> grounds = Grounds();
        string first = grounds[0], second = grounds[1];
        string placeA = PlaceOf(first), placeB = PlaceOf(second);

        using (Buried(first, second))
        {
            (int Level, int RoomIndex) at = UndergroundComplex.MaintenanceLedgerRoomFor(first)!.Value;
            Assert.True(MissingMiddle.ShouldBeWritten(first, at.Level, at.RoomIndex, [], placeA));

            IReadOnlyList<FieldNote> book =
                FieldNotes.Append([], MissingMiddle.Note(first, placeA, 10.0, "The Works"));

            // Asked again at the same ground: no.
            Assert.False(MissingMiddle.ShouldBeWritten(first, at.Level, at.RoomIndex, book, placeA));

            // …and it is not a ban on the note, it is a ban on a second one HERE.
            (int Level, int RoomIndex) other = UndergroundComplex.MaintenanceLedgerRoomFor(second)!.Value;
            Assert.True(MissingMiddle.ShouldBeWritten(second, other.Level, other.RoomIndex, book, placeB));

            // A book full of other kinds at this place does not count as a measurement either — the kind is
            // what makes it this note, and a match on the place alone would silence the book on any ground
            // the captain had already written a single line about.
            IReadOnlyList<FieldNote> otherKinds =
                FieldNotes.Append([], new FieldNote("a paper, read", 5.0, placeA, "🔦"));
            Assert.True(MissingMiddle.ShouldBeWritten(first, at.Level, at.RoomIndex, otherKinds, placeA));
        }
    }

    /// <summary>
    /// #1063 · <b>THE BOOK NEVER LIES</b> — the burial's own binding law, aimed at the one act that adds a
    /// line to the book because of a burial. The absence is filed BESIDE the find and never instead of it:
    /// the ledger's own entry, written the moment the paper went into the pocket, is still there, still says
    /// what it said, and still comes FIRST — because the find is what makes the absence measurable at all.
    ///
    /// <para>The find is composed by the real generator (<see cref="UndergroundComplex.HaulLine"/>) and never
    /// typed: a guard that wrote its own ledger line would be comparing a string to itself.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the absence filed through a book built as <c>[note]</c> instead
    /// of appended to the one the captain had — <i>"the ledger's own entry is gone from the book after the
    /// absence is measured"</i>.</para>
    /// </summary>
    [Fact]
    public void TheBookKeepsTheOriginalFindWhenTheAbsenceIsMeasured()
    {
        string body = Grounds()[0];
        string place = PlaceOf(body);

        using (Buried(body))
        {
            (int Level, int RoomIndex) at = UndergroundComplex.MaintenanceLedgerRoomFor(body)!.Value;

            // What the captain actually carried out of that room, in the building's own words.
            string found = UndergroundComplex.HaulLine(
                UndergroundComplex.Haul.Records, body, at.Level, at.RoomIndex, null);
            Assert.Contains(Burial.LedgerLine, found, StringComparison.Ordinal);

            IReadOnlyList<FieldNote> book = FieldNotes.Append([], new FieldNote(found, 10.0, place, "🔦"));
            book = FieldNotes.Append(book, MissingMiddle.Note(body, place, 11.0, "The Works"));

            Assert.Equal(2, book.Count);
            Assert.Equal(found, book[0].Text);
            Assert.Equal(MissingMiddle.Line, book[1].Text);

            // …and the page the book actually renders for that ground carries both, newest first.
            IReadOnlyList<FieldNote> page = FieldNotes.Here(book, place);
            Assert.Equal(2, page.Count);
            Assert.Equal(MissingMiddle.Line, page[0].Text);
            Assert.Equal(found, page[1].Text);
        }
    }

    /// <summary>
    /// #1063/#741 · <b>THE SUBJECT IS DECLARED BY THE AUTHOR</b>, at writing time, so the THREADS page can
    /// stack it. Two subjects and never a third, both already printed for the captain to read: the site's own
    /// operator — the letterhead the missing permits and invoices would have carried, and the one the rag's
    /// clipping is filed under — and the ground itself.
    ///
    /// <para>No <see cref="CaseSubjects.Person"/> is minted, which is the law that matters here: the line
    /// prints nobody's name, and a subject naming somebody the sentence does not print is the game doing the
    /// detecting.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>SubjectsFor</c> returning <c>""</c> —
    /// <i>"the absence joins no stack, so the rag's clipping and the captain's own measurement of it never
    /// meet"</i>.</para>
    /// </summary>
    [Fact]
    public void TheAbsenceDeclaresItsOwnSubjectsAndNamesNobody()
    {
        string body = Grounds()[0];
        using (Buried(body))
        {
            FieldNote note = MissingMiddle.Note(body, PlaceOf(body), 7.0, "The Works");
            Assert.Equal(MissingMiddle.SubjectsFor(body, "The Works"), note.Subjects);
            IReadOnlyList<CaseSubjects.Subject> on = CaseSubjects.On(note);

            Assert.Equal(2, on.Count);
            Assert.Contains(on, s => s.Of == CaseSubjects.Kind.Office && s.Name == Burial.RagOffice(body));
            Assert.Contains(on, s => s.Of == CaseSubjects.Kind.Place && s.Name == "The Works");
            Assert.DoesNotContain(on, s => s.Of == CaseSubjects.Kind.Person);

            // The rag's clipping is filed under that same office, which is the whole point of declaring it:
            // the world's cheerful account of the job and the captain's measurement of what is missing from
            // it stack under one heading, and the book says nothing over them.
            Assert.Equal(SiteOperator.Of(body).Name, Burial.RagOffice(body));
        }
    }

    // ══ THE EMPTY SEAL · SPENT ONCE, AND BORINGLY ═══════════════════════════════════════════════════════

    private static readonly ExpeditionSiteKind[] Kinds =
        [.. Enum.GetValues<ExpeditionSiteKind>()];

    /// <summary>
    /// #1063 · <b>WHICH SEAL IS THE EMPTY ONE IS DETERMINISTIC FOR A SEED</b>, and it is always a LEAF —
    /// a door with nothing nested behind it. So the spend costs a cache and never costs a route: an empty
    /// seal that was also the only way into a depth-2 chamber would be a disappointment paid for by deleting
    /// a reward, which is a different and much worse bargain than the one the issue asked for.
    ///
    /// <para>The nomination is asked twice per ground and over a real population of grounds, and the
    /// population is asserted to actually vary — a nomination that always answered the same door would pass
    /// every determinism check ever written for exactly the wrong reason (the fifth named bug class).</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the nomination seeded on the KIND instead of the body —
    /// <i>"every ground in the system nominates the same door"</i> (the variation assert); and
    /// <c>LeafDoorIds</c> returning every door id — <i>"the nominated seal is wreck-a, which is the only way
    /// to the inner hold"</i>.</para>
    /// </summary>
    [Fact]
    public void TheNominatedSealIsDeterministicForASeedAndIsAlwaysALeaf()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            IReadOnlyList<string> leaves = ExpeditionRegions.LeafDoorIds(kind);
            Assert.NotEmpty(leaves);

            // A leaf really is a leaf: forcing it appends no further sealed door.
            foreach (string leaf in leaves)
            {
                ExpeditionRegions.Region region = ExpeditionRegions.ForceOpen(kind, leaf, Field);
                Assert.DoesNotContain(
                    region.Consoles, c => c.Kind == ExpeditionRegions.RegionConsoleKind.SealedDoor);
            }

            var nominated = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < 400; i++)
            {
                string body = $"absence-seal-{(int)kind}-{i}";
                string? one = EmptySeal.TheNominatedLeaf(kind, body);
                Assert.NotNull(one);
                Assert.Contains(one!, leaves);

                // Asked twice, the same answer — the choice is a fact about the world, not about the order
                // somebody walked it.
                Assert.Equal(one, EmptySeal.TheNominatedLeaf(kind, body));
                nominated.Add(one!);
            }

            // …and the die is really a die. Every leaf this kind seals is nominated by some ground.
            Assert.Equal(leaves.Count, nominated.Count);
        }
    }

    /// <summary>
    /// #1063 · <b>THE DISAPPOINTMENT IS SPENT EXACTLY ONCE.</b> Before it is spent, the nominated leaf of a
    /// ground answers yes and every other door answers no. The moment the key is written down, nothing
    /// anywhere answers yes again — not another door on that ground, not the same door on another ground, not
    /// the nominated leaf of any other ground in the system.
    ///
    /// <para>And the one that WAS spent keeps answering <see cref="EmptySeal.IsSpentOn"/> for ever, which is
    /// the half that makes the room survive a reload: what is written down is what every later reading asks.
    /// </para>
    ///
    /// <para><b>Reverts that reddened it:</b> the <c>spentOn is not null</c> arm removed from
    /// <c>WouldBeEmpty</c> — <i>"a second ground's nominated seal is empty too, so emptiness has a rate"</i>;
    /// and <c>IsSpentOn</c> comparing only the door id — <i>"the same door id on every other moon in the
    /// system comes back empty"</i>.</para>
    /// </summary>
    [Fact]
    public void TheEmptySealIsSpentExactlyOnce()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            string body = $"absence-seal-spend-{(int)kind}";
            string leaf = EmptySeal.TheNominatedLeaf(kind, body)!;

            // ── UNSPENT. The nominated leaf, and nothing else in the building.
            Assert.True(EmptySeal.WouldBeEmpty(kind, body, leaf, null));
            foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(kind, Field))
            {
                if (!string.Equals(d.Id, leaf, StringComparison.Ordinal))
                {
                    Assert.False(EmptySeal.WouldBeEmpty(kind, body, d.Id, null));
                }
            }

            // ── SPENT. The key is written down, and after that nothing is ever empty again.
            string key = EmptySeal.Key(body, leaf);
            Assert.False(EmptySeal.WouldBeEmpty(kind, body, leaf, key));
            foreach (ExpeditionSiteKind other in Kinds)
            {
                for (int i = 0; i < 120; i++)
                {
                    string elsewhere = $"absence-seal-after-{(int)other}-{i}";
                    foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(other, Field))
                    {
                        Assert.False(EmptySeal.WouldBeEmpty(other, elsewhere, d.Id, key));
                    }
                }
            }

            // ── …AND IT STAYS THAT ROOM. The written key answers for the one pair and for no other.
            Assert.True(EmptySeal.IsSpentOn(key, body, leaf));
            Assert.False(EmptySeal.IsSpentOn(key, body + "x", leaf));
            Assert.False(EmptySeal.IsSpentOn(key, body, leaf + "x"));
            Assert.False(EmptySeal.IsSpentOn(null, body, leaf));
        }
    }

    /// <summary>
    /// #1063 · <b>AND ONLY EARLY — BEFORE THE BURIAL THRESHOLD OF ITS GROUND.</b> The disappointment is only
    /// protection if it is spent before there is a story to read the emptiness into: a captain who has
    /// already watched a ground get filled in and then finds a bare recess files it as part of the pattern,
    /// which is the exact opposite of what it is for.
    ///
    /// <para>Both sides of the threshold are checked on ONE ground, so the guard holds a world that can tell
    /// pass from fail: the same body, the same nominated door, yes with the register empty and no with the
    /// works on and no again once it is filled.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>Burial.IsFilled || Burial.WorksAreOn</c> arm removed from
    /// <c>WouldBeEmpty</c> — <i>"a seal on a ground that has already been filled in comes back empty"</i>.</para>
    /// </summary>
    [Fact]
    public void TheEmptySealIsOnlySpentBeforeTheBurialThresholdOfItsGround()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            string body = $"absence-seal-early-{(int)kind}";
            string leaf = EmptySeal.TheNominatedLeaf(kind, body)!;

            Assert.True(EmptySeal.WouldBeEmpty(kind, body, leaf, null));

            using (Opened(body))
            {
                Assert.False(EmptySeal.WouldBeEmpty(kind, body, leaf, null));
            }
            using (Buried(body))
            {
                Assert.False(EmptySeal.WouldBeEmpty(kind, body, leaf, null));
            }

            // The register puts itself back, and the ground is early again — which is what proves the two
            // refusals above were caused by the register and not by something else in this guard.
            Assert.True(EmptySeal.WouldBeEmpty(kind, body, leaf, null));
        }
    }

    /// <summary>
    /// #1063 · <b>WHAT AN EMPTY SEAL OPENS ON: THE SAME ROOM, WITH NOTHING IN IT.</b> No cache, no landmark,
    /// no name, no bonus — and the WALLS AND BOUNDS UNTOUCHED, which is the half that makes the beat work.
    /// The captain forces the door, walks in, stands in the middle of it and there is nothing there; a seal
    /// that appended no ground at all would read as a refusal or as a bug.
    ///
    /// <para><b>Reverts that reddened it:</b> <c>Hollow</c> returning the region unchanged — <i>"the empty
    /// seal still banks 900 credits out of a discovery cache"</i>; and <c>Hollow</c> clearing
    /// <c>Walls</c> too — <i>"the empty seal opens on no ground at all"</i>.</para>
    /// </summary>
    [Fact]
    public void AnEmptySealOpensOnGroundWithNothingInIt()
    {
        foreach (ExpeditionSiteKind kind in Kinds)
        {
            foreach (string leaf in ExpeditionRegions.LeafDoorIds(kind))
            {
                ExpeditionRegions.Region full = ExpeditionRegions.ForceOpen(kind, leaf, Field);

                // The guard has to be holding a room that really had something in it, or "it is empty now"
                // is a fact about a region that was always empty.
                Assert.NotEmpty(full.Walls);
                Assert.Contains(
                    full.Consoles, c => c.Kind == ExpeditionRegions.RegionConsoleKind.DiscoveryCache);
                Assert.NotEmpty(full.Landmarks);
                Assert.True(full.DiscoveryBonus > 0);

                ExpeditionRegions.Region bare = EmptySeal.Hollow(full);

                Assert.Empty(bare.Consoles);
                Assert.Empty(bare.Landmarks);
                Assert.Equal(0, bare.DiscoveryBonus);
                Assert.Equal("", bare.Scheme);

                // …and it is the SAME room. Every wall, every bound, the same door and the same heart.
                Assert.Equal(full.Walls, bare.Walls);
                Assert.Equal(full.DoorId, bare.DoorId);
                Assert.Equal(full.Depth, bare.Depth);
                Assert.Equal(
                    (full.MinX, full.MinY, full.MaxX, full.MaxY),
                    (bare.MinX, bare.MinY, bare.MaxX, bare.MaxY));
                Assert.Equal((full.RevealX, full.RevealY), (bare.RevealX, bare.RevealY));
            }
        }
    }

    /// <summary>
    /// #1063 · <b>THE KEPT SPECIMEN IS UNTOUCHED.</b> #1082's preserved doorway — a five-du recess off a
    /// blind end of the listed bottom's spine, one seamless leaf, opening on nothing, captioned by nobody —
    /// is the other leaf in this arc that opens on nothing, and slice 2 may not have moved one number of it.
    ///
    /// <para>Measured against the real generator on real grounds, with the empty-seal machinery pointed at
    /// the same worlds: the specimen's floor, its recess depth, its segment and its uncaptioned silence are
    /// what slice 1 shipped, and <see cref="EmptySeal"/> cannot name it — the seal's whole vocabulary is the
    /// expedition's authored door ids, and no door id in the game is a specimen.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>SpecimenRecessDu</c> nudged to 4.0 — <i>"the kept specimen's
    /// recess is not five du"</i> (the segment assert below moves with it).</para>
    /// </summary>
    [Fact]
    public void TheKeptSpecimenIsUntouchedBySliceTwo()
    {
        Assert.Equal(5.0, UndergroundComplex.SpecimenRecessDu);

        int seen = 0;
        foreach (string body in Grounds().Take(20))
        {
            using (Buried(body))
            {
                Assert.NotNull(UndergroundComplex.SpecimenFloorOf(body));
                int floor = UndergroundComplex.SpecimenFloorOf(body)!.Value;
                Assert.True(UndergroundComplex.HasSpecimenOn(body, floor));

                if (UndergroundComplex.SpecimenOn(body, floor, Field) is not { } leaf
                    || UndergroundComplex.SpecimenRecessAt(Field) is not { } at)
                {
                    continue;   // the ribs run to both caps: this site keeps no specimen, the honest answer
                }
                seen++;

                // It is a leaf drawn across a five-du recess, and it is not a door any seal could be about:
                // no id, no label, nothing said. The empty seal's whole vocabulary is elsewhere.
                Assert.Equal(at.Y + UndergroundComplex.SpecimenRecessDu, leaf.Y1, 6);
                Assert.Equal(leaf.Y1, leaf.Y2, 6);
                Assert.Equal(at.X, leaf.X, 6);

                foreach (ExpeditionSiteKind kind in Kinds)
                {
                    Assert.DoesNotContain(
                        ExpeditionRegions.LeafDoorIds(kind), id => id.Contains("specimen", StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        Assert.True(seen > 10, $"only {seen} grounds kept a specimen — this proves little.");
    }

    /// <summary>
    /// #1063 · <b>ONE READER, ONE WRITER.</b> §13.15's second cause is a caller reasoning about the shape of
    /// a building it does not own, and this beat has five of them: the compose that replays a site, the force
    /// itself, the cache claim, the fog and the born-dark overlay all resolve the chamber behind a forced
    /// door. A room that came back empty to one of them and full to another would bank 900 credits out of
    /// bare ground, or draw a landmark over a room with nothing under it, and nothing on screen would say
    /// which was lying.
    ///
    /// <para>So the source is swept: the client resolves a forced region in exactly one place, decides the
    /// spend in exactly one place, and writes the key down in exactly one place.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the cache-claim loop pointed back at
    /// <c>ExpeditionRegions.ForceOpen</c> — <i>"2 places in the client resolve a forced chamber"</i>.</para>
    /// </summary>
    [Fact]
    public void TheForcedChamberHasOneReaderAndTheSpendOneWriter()
    {
        string root = TestTree.RepoRoot();
        string client = System.IO.Path.Combine(root, "src", "SpaceSails.Client");

        Dictionary<string, List<string>> where = new(StringComparer.Ordinal)
        {
            ["ExpeditionRegions.ForceOpen("] = [],
            ["EmptySeal.WouldBeEmpty("] = [],
            ["EmptySeal.Key("] = [],
        };

        foreach (string full in System.IO.Directory
            .EnumerateFiles(client, "*.cs", System.IO.SearchOption.AllDirectories)
            .Concat(System.IO.Directory.EnumerateFiles(client, "*.razor", System.IO.SearchOption.AllDirectories)))
        {
            string rel = System.IO.Path.GetRelativePath(root, full).Replace('\\', '/');
            if (rel.Contains("/obj/", StringComparison.Ordinal) || rel.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }
            string text = System.IO.File.ReadAllText(full);
            foreach (string call in where.Keys)
            {
                for (int at = text.IndexOf(call, StringComparison.Ordinal); at >= 0;
                     at = text.IndexOf(call, at + call.Length, StringComparison.Ordinal))
                {
                    where[call].Add(rel);
                }
            }
        }

        foreach ((string call, List<string> sites) in where)
        {
            Assert.True(sites.Count == 1,
                $"{sites.Count} places in the client call {call} — there may be exactly one: "
                + string.Join(", ", sites));
            Assert.Equal("src/SpaceSails.Client/Pages/Map.ExpeditionRegions.cs", sites[0]);
        }
    }

    // ══ THE SCULLY LAW, AND §8's RESERVED WORD ══════════════════════════════════════════════════════════

    /// <summary>§8's reserved word, and every other word that would settle which reading of §10 is true —
    /// <see cref="TheBurialTests"/>' own list, kept in step with it because both sweep the same arc.</summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
    ];

    /// <summary>
    /// #1063/#672 · NO STRING SLICE 2 PUBLISHES NAMES THE RESERVED THING, and neither of them settles
    /// anything. The absence note lists three ordinary documents and stops at a measurement; the empty seal
    /// supplies the mundane explanation itself, out loud, in the captain's own voice.
    ///
    /// <para>…and there are exactly two of them. A beat that grew a third string would be a sentence
    /// somebody wrote to fill a gap, which is how this feature dies (§13.20's own lesson).</para>
    ///
    /// <para><b>Revert that reddened it:</b> the word planted in the absence line — <i>"a slice-2 string
    /// settles what it must leave open: MissingMiddle.AllProse"</i>.</para>
    /// </summary>
    [Fact]
    public void NoStringInSliceTwoNamesTheReservedThing()
    {
        var strings = new List<(string Where, string Text)>();
        foreach (string s in MissingMiddle.AllProse())
        {
            strings.Add(("MissingMiddle.AllProse", s));
        }
        foreach (string s in EmptySeal.AllProse())
        {
            strings.Add(("EmptySeal.AllProse", s));
        }

        var named = new List<string>();
        foreach ((string where, string text) in strings)
        {
            foreach (string word in Forbidden)
            {
                if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    named.Add($"{where}: \"{word}\" in \"{text}\"");
                }
            }
        }
        Assert.True(named.Count == 0,
            "a slice-2 string settles what it must leave open:\n  " + string.Join("\n  ", named));

        Assert.Single(MissingMiddle.AllProse());
        Assert.Single(EmptySeal.AllProse());
    }

    /// <summary>
    /// #1063 · THE TWO AUTHORED LINES, CHARACTER FOR CHARACTER. These are Fable's own sentences for slice 2
    /// and an implementer may not reword one of them.
    ///
    /// <para><b>Revert that reddened it:</b> a full stop moved in the absence line — <i>"Assert.Equal()
    /// Failure … Expected: Looked for the paper a raise this size leaves…"</i>.</para>
    /// </summary>
    [Fact]
    public void TheSliceTwoLinesAreVerbatim()
    {
        Assert.Equal(
            "Looked for the paper a raise this size leaves: permit, complaint, invoice. Nothing under "
            + "either story. The absence has a shape, and I have measured it.",
            MissingMiddle.Line);

        Assert.Equal(
            "Sealed, and empty. A room somebody closed because there was nothing in it, which is a reason.",
            EmptySeal.Line);

        // …and the one composition either seam is allowed: the kind, then the sentence, and not one word
        // besides. A pulse and a book entry that were composed separately would be two authorings of a line
        // that has one author.
        Assert.Equal(EmptySeal.Glyph + " " + EmptySeal.Line, EmptySeal.Said);
    }
}
