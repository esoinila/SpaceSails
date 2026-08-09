using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #741 · THE RED PEN — the guards on the lines a captain draws between two things they wrote down.
///
/// <para>Owner: <i>"select one, select the other — connected"</i>, and then <i>"the ORDER of items updates
/// so connected items become adjacent."</i> This file pins the model that has to be true for that gesture to
/// mean anything a week later: a thread is unordered, never duplicated, erasable, and survives a save; a
/// note's handle survives a save; and the reorder is a PERMUTATION — it can never lose an entry, however
/// tangled the case gets.</para>
///
/// <para>The north star gets a guard of its own: nothing in this model connects anything. Spotting is the
/// player's act.</para>
/// </summary>
public class TheRedPenDrawsTheLineTests
{
    private static FieldNote Note(string text, double at = 100.0, string place = "Miranda · The Maze") =>
        new(text, at, place, "✍");

    private static string Id(string text, double at = 100.0, string place = "Miranda · The Maze") =>
        CaseThreads.IdentityOf(Note(text, at, place));

    // ── THE THREAD MODEL ──────────────────────────────────────────────────────────────────────────────

    /// <summary>UNORDERED. A drawn from B is the same line as B drawn from A — and the second drawing does
    /// not make a second line.</summary>
    [Fact]
    public void AThread_IsUnordered_AndDrawingItBothWaysMakesOneLine()
    {
        string a = Id("the haulier's slip");
        string b = Id("the scaffolder's chit", at: 200.0);

        IReadOnlyList<CaseThreads.Thread> one = CaseThreads.Draw(null, a, b, out bool first);
        IReadOnlyList<CaseThreads.Thread> two = CaseThreads.Draw(one, b, a, out bool second);

        Assert.True(first);
        Assert.False(second);
        Assert.Single(one);
        Assert.Single(two);
        Assert.Equal(one[0], two[0]);

        // Canonical: the pair is held in ordinal order whichever end it was offered from.
        Assert.Equal(CaseThreads.Between(a, b), CaseThreads.Between(b, a));
        Assert.True(string.CompareOrdinal(two[0].A, two[0].B) < 0);
    }

    /// <summary>NO DUPLICATES, and the same two titles pressed again are still one line. This is also what
    /// the SOFT CUE is keyed on — <c>drawn</c> is true exactly once per pair, so an acknowledgment can only
    /// fire for a line that was not there a moment ago.</summary>
    [Fact]
    public void TheCue_FiresExactlyOncePerThread_HoweverManyTimesThePenIsDrawnOverIt()
    {
        string a = Id("one");
        string b = Id("two", at: 200.0);

        IReadOnlyList<CaseThreads.Thread> threads = [];
        int cues = 0;
        for (int press = 0; press < 5; press++)
        {
            threads = CaseThreads.Draw(threads, press % 2 == 0 ? a : b, press % 2 == 0 ? b : a, out bool drawn);
            if (drawn)
            {
                cues++;
            }
        }

        Assert.Equal(1, cues);
        Assert.Single(threads);
    }

    /// <summary>A note is never threaded to itself, whichever road the caller took there.</summary>
    [Fact]
    public void ANote_IsNeverThreadedToItself()
    {
        string a = Id("the only entry there is");

        Assert.Null(CaseThreads.Between(a, a));
        Assert.Empty(CaseThreads.Draw(null, a, a, out bool drawn));
        Assert.False(drawn);
        Assert.False(CaseThreads.AreThreaded(CaseThreads.Draw(null, a, a), a, a));
    }

    /// <summary>Blank ends draw nothing rather than a line to nowhere.</summary>
    [Fact]
    public void ABlankEnd_DrawsNothing()
    {
        Assert.Null(CaseThreads.Between(null, Id("x")));
        Assert.Null(CaseThreads.Between(Id("x"), ""));
        Assert.Empty(CaseThreads.Draw(null, "", Id("x")));
    }

    /// <summary>THE ERASER END. The same two presses take the line off again, and a line that is not there
    /// erases to no complaint and no change.</summary>
    [Fact]
    public void TheSameGestureReversed_TakesTheLineOffAgain()
    {
        string a = Id("a");
        string b = Id("b", at: 200.0);
        string c = Id("c", at: 300.0);

        IReadOnlyList<CaseThreads.Thread> drawn = CaseThreads.Draw(CaseThreads.Draw(null, a, b), b, c);
        Assert.Equal(2, drawn.Count);

        IReadOnlyList<CaseThreads.Thread> rubbed = CaseThreads.Erase(drawn, b, a);
        Assert.Single(rubbed);
        Assert.False(CaseThreads.AreThreaded(rubbed, a, b));
        Assert.True(CaseThreads.AreThreaded(rubbed, b, c));

        // The original list is untouched: pure, like everything else in Core.
        Assert.Equal(2, drawn.Count);

        // Erasing a line nobody drew is simply nothing.
        Assert.Single(CaseThreads.Erase(rubbed, a, b));
    }

    /// <summary>A NOTE KNOWS ITS THREAD COUNT — the number the collapsed title carries, so the list of
    /// titles is itself a case status.</summary>
    [Fact]
    public void ANote_KnowsHowManyLinesRunOffIt()
    {
        string hub = Id("the hand that files them");
        string one = Id("one", at: 200.0);
        string two = Id("two", at: 300.0);
        string loose = Id("nothing to do with any of it", at: 400.0);

        IReadOnlyList<CaseThreads.Thread> threads =
            CaseThreads.Draw(CaseThreads.Draw(null, hub, one), hub, two);

        Assert.Equal(2, CaseThreads.CountFor(threads, hub));
        Assert.Equal(1, CaseThreads.CountFor(threads, one));
        Assert.Equal(0, CaseThreads.CountFor(threads, loose));
    }

    /// <summary>The book keeps a bounded number of lines, oldest off the front — the same law the book's own
    /// entries live under.</summary>
    [Fact]
    public void TheThreads_AreCapped_OldestOffTheFront()
    {
        IReadOnlyList<CaseThreads.Thread> threads = [];
        for (int i = 0; i < CaseThreads.Cap + 25; i++)
        {
            threads = CaseThreads.Draw(threads, Id("hub"), Id($"n{i}", at: 1000 + i));
        }

        Assert.Equal(CaseThreads.Cap, threads.Count);
        Assert.False(CaseThreads.AreThreaded(threads, Id("hub"), Id("n0", at: 1000)));
        Assert.True(CaseThreads.AreThreaded(threads, Id("hub"), Id("n224", at: 1224)));
    }

    // ── IDENTITY, AND WHAT SURVIVES A SAVE ────────────────────────────────────────────────────────────

    /// <summary>A handle is a pure function of the note, not of the process it was computed in. This is the
    /// guard that would have caught the <c>string.GetHashCode</c> version of this file: that one is
    /// randomised per process, so the two computations below would agree here and disagree across a
    /// reload.</summary>
    [Fact]
    public void AHandle_IsThePureFunctionOfTheNote()
    {
        FieldNote note = Note("📋 shipping manifest, torn — a description, read and left on the floor of B1.");

        Assert.Equal(CaseThreads.IdentityOf(note), CaseThreads.IdentityOf(note));
        Assert.Equal(16, CaseThreads.IdentityOf(note).Length);

        // A different word, a different moment or a different ground is a different note.
        Assert.NotEqual(CaseThreads.IdentityOf(note), CaseThreads.IdentityOf(note with { Text = note.Text + "." }));
        Assert.NotEqual(CaseThreads.IdentityOf(note), CaseThreads.IdentityOf(note with { SimTime = 100.5 }));
        Assert.NotEqual(CaseThreads.IdentityOf(note), CaseThreads.IdentityOf(note with { Place = "Luna · The Rails" }));

        // The glyph is a skim hint, not part of what the note IS — and a line survives a glyph changing.
        Assert.Equal(CaseThreads.IdentityOf(note), CaseThreads.IdentityOf(note with { Glyph = "📋" }));
    }

    /// <summary>The separator does its job. Two notes whose fields RUN TOGETHER into the same string — a
    /// ground called <c>A</c> at moment <c>12</c>, and a ground called <c>A1</c> at moment <c>2</c> — are
    /// different notes and must not share a handle. Without a separator between the fields they would, and a
    /// red line drawn to one would come out of the vault attached to the other.</summary>
    [Fact]
    public void TheFields_CannotBeSpelledIntoEachOther()
    {
        Assert.NotEqual(
            CaseThreads.IdentityOf(new FieldNote("the same words", 12.0, "A", "✍")),
            CaseThreads.IdentityOf(new FieldNote("the same words", 2.0, "A1", "✍")));

        // …and the seam on the other side of the moment, for the same reason.
        Assert.NotEqual(
            CaseThreads.IdentityOf(new FieldNote("2 the words", 1.0, "A", "✍")),
            CaseThreads.IdentityOf(new FieldNote("the words", 12.0, "A", "✍")));
    }

    /// <summary>PERSISTED IN THE VAULT LIKE EVERYTHING ELSE — and the handles still find their notes on the
    /// other side, which is the whole reason the handle is derived rather than minted.</summary>
    [Fact]
    public void TheLines_SurviveTheVault_AndStillFindTheirNotes()
    {
        FieldNote first = Note("🗂 Ilse Vandermeer — a specialist in continuity engineering.", at: 120.25);
        FieldNote second = Note("🎟 A name, and where to spend it: \"Ilse Vandermeer sent me.\"", at: 340.5);
        List<FieldNote> book = [first, second];

        IReadOnlyList<CaseThreads.Thread> threads =
            CaseThreads.Draw(null, CaseThreads.IdentityOf(first), CaseThreads.IdentityOf(second));

        var vault = new Vault
        {
            FieldNotes = new FieldNotesSection { Notes = book },
            CaseThreads = new CaseThreadsSection { Threads = [.. threads.Select(t => t.Stored)] },
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.False(loaded.Tampered);

        IReadOnlyList<FieldNote> reread = loaded.FieldNotes!.Notes;
        List<CaseThreads.Thread> back = [];
        foreach (string stored in loaded.CaseThreads!.Threads)
        {
            Assert.True(CaseThreads.Thread.TryParse(stored, out CaseThreads.Thread one));
            back.Add(one);
        }

        Assert.Single(back);
        Assert.True(CaseThreads.AreThreaded(
            back, CaseThreads.IdentityOf(reread[0]), CaseThreads.IdentityOf(reread[1])));

        // …and the page comes back with the two rows in one block, exactly as it was left.
        IReadOnlyList<CaseThreads.Row> page = CaseThreads.Page(reread, back);
        Assert.Equal(2, page[0].ClusterSize);
        Assert.True(page[0].ThreadedToNext);
    }

    /// <summary>A pre-#741 file simply lacks the section and comes back with a book full of loose ends —
    /// which is exactly what it was.</summary>
    [Fact]
    public void APreRedPenSave_ComesBackWithNoLines_AndLosesNothing()
    {
        var vault = new Vault { FieldNotes = new FieldNotesSection { Notes = [Note("one"), Note("two", 200)] } };
        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));

        Assert.Null(loaded.CaseThreads);
        Assert.Equal(2, loaded.FieldNotes!.Notes.Count);
        Assert.Equal(2, CaseThreads.Page(loaded.FieldNotes.Notes, null).Count);
    }

    /// <summary>Garbage in the section is dropped rather than thrown over — the vault is tolerant
    /// everywhere else.</summary>
    [Fact]
    public void AStoredPairThisBuildCannotRead_IsDropped()
    {
        Assert.False(CaseThreads.Thread.TryParse(null, out _));
        Assert.False(CaseThreads.Thread.TryParse("", out _));
        Assert.False(CaseThreads.Thread.TryParse("only-one-end", out _));
        Assert.False(CaseThreads.Thread.TryParse("a|b|c", out _));
        Assert.False(CaseThreads.Thread.TryParse("same|same", out _));
        Assert.True(CaseThreads.Thread.TryParse(new CaseThreads.Thread("aaa", "bbb").Stored, out _));
    }

    // ── THE REORDER ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>CONNECTED ITEMS BECOME ADJACENT — the owner's explicit ask, on a page with THREE blocks on
    /// it, so a one-cluster arrangement cannot pass this by accident.</summary>
    [Fact]
    public void ConnectingReorders_SoConnectedTitlesSitTogether_AcrossSeveralBlocks()
    {
        // Filed in this order: a b c d e f. Then two separate cases are drawn out of it — a↔d and b↔f —
        // and e is left a loose end in the middle of them.
        List<FieldNote> book = [.. new[] { "a", "b", "c", "d", "e", "f" }.Select((t, i) => Note(t, 100 + i))];
        string H(int i) => CaseThreads.IdentityOf(book[i]);

        IReadOnlyList<CaseThreads.Thread> threads =
            CaseThreads.Draw(CaseThreads.Draw(null, H(0), H(3)), H(1), H(5));

        IReadOnlyList<CaseThreads.Row> page = CaseThreads.Page(book, threads);
        Assert.Equal("a d b f c e", string.Join(" ", page.Select(r => r.Note.Text)));

        // Three blocks: {a,d}, {b,f}, then the two loose ends in their original order.
        Assert.Equal([0, 0, 1, 1, 2, 3], page.Select(r => r.Cluster).ToArray());
        Assert.Equal([2, 2, 2, 2, 1, 1], page.Select(r => r.ClusterSize).ToArray());

        // The red edges the list idiom draws: one inside each pair, and nothing between the blocks.
        Assert.Equal([true, false, true, false, false, false], page.Select(r => r.ThreadedToNext).ToArray());
    }

    /// <summary>A CHAIN, and a STAR. Three or more entries in one component all travel together, and the
    /// block's own order is the order they were written in.</summary>
    [Fact]
    public void AWholeComponentTravelsTogether_ChainOrStar()
    {
        List<FieldNote> book = [.. new[] { "a", "b", "c", "d", "e" }.Select((t, i) => Note(t, 100 + i))];
        string H(int i) => CaseThreads.IdentityOf(book[i]);

        // A chain c–e–a: three entries filed apart, one component.
        IReadOnlyList<CaseThreads.Thread> chain =
            CaseThreads.Draw(CaseThreads.Draw(null, H(2), H(4)), H(4), H(0));
        IReadOnlyList<CaseThreads.Row> page = CaseThreads.Page(book, chain);

        Assert.Equal("a c e b d", string.Join(" ", page.Select(r => r.Note.Text)));
        Assert.Equal([3, 3, 3, 1, 1], page.Select(r => r.ClusterSize).ToArray());

        // a–c is not a line anybody drew, so no red edge is claimed between them; c–e is.
        Assert.Equal([false, true, false, false, false], page.Select(r => r.ThreadedToNext).ToArray());

        // A star on b: b–a, b–d, b–e. One component of four, opened by its earliest member.
        IReadOnlyList<CaseThreads.Thread> star =
            CaseThreads.Draw(CaseThreads.Draw(CaseThreads.Draw(null, H(1), H(0)), H(1), H(3)), H(1), H(4));
        IReadOnlyList<CaseThreads.Row> starred = CaseThreads.Page(book, star);

        Assert.Equal("a b d e c", string.Join(" ", starred.Select(r => r.Note.Text)));
        Assert.Equal([4, 4, 4, 4, 1], starred.Select(r => r.ClusterSize).ToArray());
    }

    /// <summary>THE PROPERTY SWEEP. Over a hundred randomly threaded books the page is always a PERMUTATION
    /// of the entries — nothing lost, nothing duplicated, nothing invented — and every block is contiguous.
    /// This is the guard that matters: the reorder is the one operation in this feature that could quietly
    /// eat a captain's evidence.</summary>
    [Fact]
    public void TheReorder_IsAlwaysAPermutation_NoEntryEverLost()
    {
        var rng = new Random(741);
        for (int trial = 0; trial < 100; trial++)
        {
            int n = 1 + rng.Next(14);
            List<FieldNote> book = [.. Enumerable.Range(0, n).Select(i => Note($"entry {i}", 100 + i))];

            IReadOnlyList<CaseThreads.Thread> threads = [];
            int lines = rng.Next(n * 2);
            for (int k = 0; k < lines; k++)
            {
                threads = CaseThreads.Draw(
                    threads,
                    CaseThreads.IdentityOf(book[rng.Next(n)]),
                    CaseThreads.IdentityOf(book[rng.Next(n)]));
            }

            IReadOnlyList<CaseThreads.Row> page = CaseThreads.Page(book, threads);

            Assert.Equal(n, page.Count);
            Assert.Equal(
                book.Select(b => b.Text).OrderBy(t => t, StringComparer.Ordinal),
                page.Select(r => r.Note.Text).OrderBy(t => t, StringComparer.Ordinal));

            // Blocks are contiguous and numbered from the top, and every row agrees about its block's size.
            var seen = new Dictionary<int, int>();
            int last = -1;
            foreach (CaseThreads.Row row in page)
            {
                Assert.True(row.Cluster == last || row.Cluster == last + 1);
                last = row.Cluster;
                seen[row.Cluster] = seen.GetValueOrDefault(row.Cluster) + 1;
            }
            foreach (CaseThreads.Row row in page)
            {
                Assert.Equal(seen[row.Cluster], row.ClusterSize);
            }

            // A claimed red edge is always a line somebody actually drew.
            for (int i = 0; i + 1 < page.Count; i++)
            {
                if (page[i].ThreadedToNext)
                {
                    Assert.True(CaseThreads.AreThreaded(threads, page[i].Id, page[i + 1].Id));
                }
            }
        }
    }

    /// <summary>A line to a note that is not on this page — a different ground, an entry the cap has trimmed
    /// — arranges nothing and loses nothing. It stays in the vault against the day that note is on screen
    /// again.</summary>
    [Fact]
    public void ALineToANoteNotOnThisPage_ArrangesNothingAndLosesNothing()
    {
        List<FieldNote> here = [Note("a"), Note("b", 200)];
        IReadOnlyList<CaseThreads.Thread> threads =
            CaseThreads.Draw(null, CaseThreads.IdentityOf(here[0]), Id("somewhere else entirely", 900, "Luna · The Rails"));

        IReadOnlyList<CaseThreads.Row> page = CaseThreads.Page(here, threads);
        Assert.Equal("a b", string.Join(" ", page.Select(r => r.Note.Text)));
        Assert.Equal([1, 1], page.Select(r => r.ClusterSize).ToArray());

        // …and the note still knows the line runs off it, which is what its collapsed title says.
        Assert.Equal(1, page[0].Threads);
    }

    /// <summary>THE NORTH STAR, as a guard. The model never connects anything: a book of entries that
    /// plainly rhyme comes back in the order it was written, in one-row blocks, until a hand draws a line.
    /// Spotting is the player's act.</summary>
    [Fact]
    public void NothingConnectsItself_HoweverLoudlyTheEntriesRhyme()
    {
        List<FieldNote> book =
        [
            Note("🗂 Ilse Vandermeer — a specialist in continuity engineering.", 100),
            Note("📇 Marek Vandermeer — their brother — is at The Tilt.", 200),
            Note("🎟 A name, and where to spend it: \"Ilse Vandermeer sent me.\"", 300),
        ];

        IReadOnlyList<CaseThreads.Row> page = CaseThreads.Page(book, null);

        Assert.Equal(book.Select(b => b.Text), page.Select(r => r.Note.Text));
        Assert.All(page, r => Assert.Equal(1, r.ClusterSize));
        Assert.All(page, r => Assert.Equal(0, r.Threads));
        Assert.All(page, r => Assert.False(r.ThreadedToNext));
    }

    // ── TITLE AND BULLETS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>TITLE-FIRST. The title is the entry's first sentence, and the thing the eye is hunting for —
    /// the name at the front — survives the clip.</summary>
    [Fact]
    public void ATitle_IsTheFirstSentence_AndKeepsTheNameAtTheFront()
    {
        FieldNote dossier = Note(
            "🗂 Ilse Vandermeer — a specialist in continuity engineering, carried out here by a directorate "
            + "that files under Labour. The kit is theirs, all of it, and now you know their name.");

        string title = CaseThreads.TitleOf(dossier);

        Assert.Contains("Ilse Vandermeer", title, StringComparison.Ordinal);
        Assert.DoesNotContain("The kit is theirs", title, StringComparison.Ordinal);
        Assert.True(title.Length <= CaseThreads.TitleLength + 1, $"title ran to {title.Length}: {title}");

        // A short entry is its own title, unclipped.
        Assert.Equal("📋 a torn manifest.", CaseThreads.TitleOf(Note("📋 a torn manifest.")));
    }

    /// <summary>EXPANDED = BULLETS, and the node loses nothing to the clip: the FULL first sentence is the
    /// first bullet, and every word of the entry is on the page somewhere.</summary>
    [Fact]
    public void ExpandingANode_LosesNoWord()
    {
        FieldNote note = Note(
            "🗂 Ilse Vandermeer — a specialist in continuity engineering, carried out here by a directorate "
            + "that files under Labour. The kit is theirs, all of it. The lanyard is expired.");

        IReadOnlyList<string> bullets = CaseThreads.BulletsOf(note);

        Assert.Equal(3, bullets.Count);
        Assert.StartsWith("🗂 Ilse Vandermeer", bullets[0], StringComparison.Ordinal);
        Assert.Equal("The lanyard is expired.", bullets[2]);
        Assert.Equal(
            note.Text.Replace(" ", "", StringComparison.Ordinal),
            string.Concat(bullets).Replace(" ", "", StringComparison.Ordinal));

        Assert.Empty(CaseThreads.BulletsOf(Note("")));
    }

    // ── WHERE THE PEN WORKS ───────────────────────────────────────────────────────────────────────────

    /// <summary>BOTH DIRECTIONS, over the WHOLE ladder. On your feet the pen never comes out, whatever the
    /// seat; in a seat the answer is exactly the spread's — same predicate, not a second one.</summary>
    [Fact]
    public void ThePen_IsTableWork_AndTheLadderIsTheSpreadsOwn()
    {
        // On your feet: no seat at all, and the refusal is the posture one.
        Assert.False(CaseThreads.CanConnectHere(null, alone: true));
        Assert.False(CaseThreads.CanConnectHere(null, alone: false));
        Assert.Equal(SeatedPosture.NotOnYourFeetLine, CaseThreads.PenRefusal(null, alone: true));

        // Seated: every rung, both ways round, and never a different answer from the spread's.
        foreach (SeatedHud.Seat seat in SeatedSpread.Ladder)
        {
            foreach (bool alone in new[] { true, false })
            {
                Assert.Equal(
                    SeatedSpread.CanSpreadTheCase(seat, alone),
                    CaseThreads.CanConnectHere(seat, alone));
                Assert.Equal(
                    SeatedSpread.RefusalAt(seat, alone),
                    CaseThreads.PenRefusal(seat, alone));
            }
        }

        // …and the two ends of that ladder are really both reached, so this sweep is not vacuous.
        Assert.True(CaseThreads.CanConnectHere(SeatedHud.Seat.Cabinet, alone: false));
        Assert.False(CaseThreads.CanConnectHere(SeatedHud.Seat.BarStool, alone: true));
        Assert.True(CaseThreads.CanConnectHere(SeatedHud.Seat.HallTable, alone: true));
        Assert.False(CaseThreads.CanConnectHere(SeatedHud.Seat.HallTable, alone: false));
    }

    // ── THE CANON SWEEP ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Every new sentence is listed for the canon review, none is a placeholder, and none of them
    /// congratulates. The register is the whisky glass set down, not a slot machine.</summary>
    [Fact]
    public void EVERY_NEW_SENTENCE_IsListedForTheCanonSweep_AndNoneOfThemCheers()
    {
        List<string> sweep = [.. CaseThreads.AllProse()];

        Assert.NotEmpty(sweep);
        foreach (string line in sweep)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain("TODO", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lorem", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("!", line, StringComparison.Ordinal);

            foreach (string cheer in new[] { "well done", "correct", "you were right", "solved", "congrat" })
            {
                Assert.DoesNotContain(cheer, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.Contains(CaseThreads.PenLabel, sweep);
        Assert.Contains(CaseThreads.ConnectingBlurb, sweep);
    }
}
