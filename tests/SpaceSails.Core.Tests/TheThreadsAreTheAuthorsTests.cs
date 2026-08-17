using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #741 v1 · THE THREADS — the guards on the subjects the book's entries declare.
///
/// <para>Owner's issue: <i>"a third tab — THREADS — grouping existing entries by the entities they already
/// name… the THREAD is just the stack of what you wrote about one name, and the captain draws the line."</i>
/// And the fence around it, from the north-star comment: <i>"SPOTTING is the player's act, not the
/// game's."</i></para>
///
/// <para>Every law worth having here is a law about where a subject COMES FROM, so most of these guards are
/// about provenance rather than about a value: the author declares it, the prose is never read, no personal
/// name is ever a heading unless the game printed it, and a save that predates all of this loads as a book
/// with nothing said twice.</para>
/// </summary>
public sealed class TheThreadsAreTheAuthorsTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string CoreSource(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Core", file));

    /// <summary>The bodies a sweep walks — the same set <c>LandingSiteTests</c> uses, so a body added to the
    /// game is added to both in one edit.</summary>
    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "ganymede", "callisto", "titan", "enceladus", "triton"];

    /// <summary>Everything a kit says, filed as the game files it — one book, oldest first, the way
    /// <c>AssembleSomebody</c> and the demo's pre-file both do it.</summary>
    private static IReadOnlyList<FieldNote> BookFrom(
        IReadOnlyList<FieldNote>? into, string bodyId, int siteIndex, int roomIndex)
    {
        LandingSite site = LandingSites.For(bodyId)[siteIndex];
        string place = FieldNotes.PlaceLabel(BodyNames.Display(bodyId), site.Name);
        IReadOnlyList<FieldNote> book = into ?? [];
        double at = book.Count == 0 ? 0.0 : book[^1].SimTime;

        foreach (FieldDossier.Saying one in
            FieldDossier.Debrief(bodyId, site.LayoutSalt, roomIndex, everySaying: true))
        {
            at += 240.0;
            book = FieldNotes.Append(book, new FieldNote(one.Text, at, place, one.Glyph, one.Subjects));
        }
        return book;
    }

    // ── GUARD (a) · A SUBJECT COMES FROM THE AUTHOR, NEVER FROM THE PROSE ────────────────────────────────

    /// <summary>
    /// THE SOURCE-SHAPE GUARD. No part of the subject model reads a note's WORDS.
    ///
    /// <para>This is the law the whole feature stands on and the one that cannot be tested by value: a
    /// regex-based extractor would pass every behavioural test on the day it was written and would silently
    /// stop finding OFFICE OF WORKS the first time somebody reworded a card. So the guard reads
    /// <c>CaseSubjects.cs</c> itself and fails if it ever grows a parser.</para>
    ///
    /// <para><b>Proven able to fail:</b> add <c>note.Text.Contains("OFFICE")</c> anywhere in
    /// <c>CaseSubjects</c> and this goes red naming the token.</para>
    /// </summary>
    [Fact]
    public void NoSubjectIsEverReadOutOfANotesWords()
    {
        string source = CoreSource("CaseSubjects.cs");

        // The whole surface of "reading the prose": the regex engine, and any reach for the note's own text
        // or the author's glyph. The doc comments name Text once each in prose; the tokens below are all
        // CODE shapes, which is why they carry their punctuation.
        foreach (string parser in new[]
        {
            "Regex", "System.Text.RegularExpressions",
            "note.Text", ".Text.", "Text.Contains", "Text.IndexOf", "Text.Split", "Text.StartsWith",
        })
        {
            Assert.False(source.Contains(parser, StringComparison.Ordinal),
                $"CaseSubjects reads `{parser}` — a subject worked out of a sentence is a subject that "
                + "changes when somebody rewords the sentence (#741's founding law: the AUTHOR declares).");
        }

        // …and the one member that could plausibly be handed a note is handed a note for its SUBJECTS field
        // and nothing else.
        Assert.Contains("note.Subjects", source, StringComparison.Ordinal);
    }

    // ── GUARD (b) · ANTI-VACUOUS: the world actually declares subjects, and stacks actually form ─────────

    /// <summary>
    /// THE SWEEP. Over every landable body's sites and a spread of rooms, the kits the game can hand a
    /// captain declare a real spread of distinct subjects — offices, doors and the strangers whose names are
    /// printed — and they are not all one string.
    ///
    /// <para><b>Proven able to fail:</b> drop the <c>CaseSubjects.Line(...)</c> argument from any one of
    /// <see cref="FieldDossier.Debrief"/>'s author sites and the distinct count collapses under the floor.</para>
    /// </summary>
    [Fact]
    public void TheWorldsKitsDeclareASpreadOfSubjects()
    {
        var offices = new HashSet<string>(StringComparer.Ordinal);
        var doors = new HashSet<string>(StringComparer.Ordinal);
        var people = new HashSet<string>(StringComparer.Ordinal);
        int sayingsWithNone = 0;
        int sayings = 0;

        foreach (string body in Bodies)
        {
            IReadOnlyList<LandingSite> sites = LandingSites.For(body);
            for (int s = 0; s < sites.Count; s++)
            {
                for (int room = 0; room < 8; room++)
                {
                    foreach (FieldDossier.Saying one in
                        FieldDossier.Debrief(body, sites[s].LayoutSalt, room, everySaying: true))
                    {
                        sayings++;
                        var note = new FieldNote(one.Text, room, "somewhere", one.Glyph, one.Subjects);
                        IReadOnlyList<CaseSubjects.Subject> subjects = CaseSubjects.On(note);
                        if (subjects.Count == 0)
                        {
                            sayingsWithNone++;
                        }

                        foreach (CaseSubjects.Subject subject in subjects)
                        {
                            switch (subject.Of)
                            {
                                case CaseSubjects.Kind.Office: offices.Add(subject.Name); break;
                                case CaseSubjects.Kind.Place: doors.Add(subject.Name); break;
                                default: people.Add(subject.Name); break;
                            }
                        }
                    }
                }
            }
        }

        // The pools the dossier actually holds: five employers, six doors, and a great many strangers.
        Assert.True(offices.Count >= 5, $"only {offices.Count} distinct offices across the whole sweep");
        Assert.True(doors.Count >= 6, $"only {doors.Count} distinct doors across the whole sweep");
        Assert.True(people.Count >= 40, $"only {people.Count} distinct printed strangers across the sweep");

        // …and the sentence that names NOBODY still names nobody. A world where every gist declared a
        // subject would mean somebody had started inventing them (the family's lead is four sentences about
        // letters and a countersignature and prints no name at all).
        Assert.True(sayingsWithNone > 0,
            "every single saying in the sweep declares a subject — the lead hint prints no name and must "
            + "declare none (#741: a gist forced to be about something is an extraction).");
        Assert.True(sayings > 200, $"the sweep only walked {sayings} sayings; it is not proving much");
    }

    /// <summary>
    /// …AND A STACK FORMS OFF ONE ROOM, AND KEEPS FORMING OFF TWO. One assembled kit already names its
    /// stranger in three of its four sentences — that rhyme has been in the dossier since #588 and nothing
    /// has ever collected it — and a second room turned over on the SAME ground stacks its own.
    ///
    /// <para><b>Proven able to fail:</b> remove <c>CaseSubjects.Person(who.Name)</c> from the WhoTheyWere
    /// author and the three-deep stack drops to two; remove it from two and no thread forms at all.</para>
    /// </summary>
    [Fact]
    public void TwoDigsOnOneGroundBuildRealStacks()
    {
        LandingSite site = LandingSites.For("miranda")[0];
        FieldDossier.Person who = FieldDossier.Who("miranda", site.LayoutSalt, 3);

        IReadOnlyList<FieldNote> oneRoom = BookFrom(null, "miranda", 0, 3);
        IReadOnlyList<CaseSubjects.SubjectThread> first = CaseSubjects.ThreadsOf(oneRoom);

        CaseSubjects.SubjectThread stranger =
            Assert.Single(first, t => t.Subject == CaseSubjects.Person(who.Name));
        Assert.Equal(3, stranger.Entries.Count);
        Assert.Equal($"👤 {who.Name}", stranger.Heading);

        // Chronological, oldest first — the order the book wrote them, because the book only appends.
        for (int i = 1; i < stranger.Entries.Count; i++)
        {
            Assert.True(stranger.Entries[i - 1].SimTime < stranger.Entries[i].SimTime);
        }

        // A second room on the same ground: strictly more stacks, and none of the first room's is lost.
        IReadOnlyList<FieldNote> twoRooms = BookFrom(oneRoom, "miranda", 0, 11);
        IReadOnlyList<CaseSubjects.SubjectThread> after = CaseSubjects.ThreadsOf(twoRooms);
        Assert.True(after.Count > first.Count,
            $"two digs of one estate produced {after.Count} stacks where one produced {first.Count}");
        foreach (CaseSubjects.SubjectThread was in first)
        {
            Assert.Contains(after, t => t.Subject == was.Subject);
        }

        // And nothing is a stack on its own: a subject named once is a note, not a thread.
        Assert.All(after, t => Assert.True(t.Entries.Count >= CaseSubjects.MakesAThread));
    }

    /// <summary>The demo start's own six entries — the ones a tester boots into — already carry the stack
    /// the whole feature is for, and it is the rhyme the dossier has always quietly held.</summary>
    [Fact]
    public void TheDemoCase_BootsWithItsStackAlreadyStanding()
    {
        IReadOnlyList<FieldNote> book = BookFrom(null, "miranda", 0, 3);
        book = BookFrom(book, "luna", 1, 22);

        IReadOnlyList<CaseSubjects.SubjectThread> stacks = CaseSubjects.ThreadsOf(book);
        Assert.NotEmpty(stacks);

        FieldDossier.Person who = FieldDossier.Who("miranda", LandingSites.For("miranda")[0].LayoutSalt, 3);
        Assert.Contains(stacks, t => t.Subject == CaseSubjects.Person(who.Name) && t.Entries.Count == 3);

        // The heading names the thing and says nothing else. No verdict word anywhere on the page.
        foreach (CaseSubjects.SubjectThread stack in stacks)
        {
            foreach (string tell in new[] { "connect", "related", "suspicious", "link", "because", "proves" })
            {
                Assert.DoesNotContain(tell, stack.Heading, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── GUARD (c) · NEVER A PERSONAL NAME THE GAME HAS NOT PRINTED ──────────────────────────────────────

    /// <summary>
    /// THE CANON SWEEP. Over every kit in the world: a subject that names a PERSON is only ever declared by
    /// a sentence that prints that person's name, and no office or door is secretly a stranger's name off
    /// the dossier's own pools.
    ///
    /// <para>This is #741's spotting law made checkable. A heading naming somebody the captain has never
    /// been told about would be the game doing the detecting — and on the one surface in the game where a
    /// player goes LOOKING for repeated names, a name they have not read is worse than useless.</para>
    ///
    /// <para><b>Proven able to fail:</b> plant <c>CaseSubjects.Person(kin.Name)</c> on the lead-hint saying
    /// (which prints no name) and this goes red naming the beat and the name.</para>
    /// </summary>
    [Fact]
    public void NoSubjectNamesAPersonTheGameHasNotPrinted()
    {
        var names = new HashSet<string>(FieldDossier.GivenNames, StringComparer.Ordinal);
        names.UnionWith(FieldDossier.FamilyNames);

        foreach (string body in Bodies)
        {
            IReadOnlyList<LandingSite> sites = LandingSites.For(body);
            for (int s = 0; s < sites.Count; s++)
            {
                for (int room = 0; room < 8; room++)
                {
                    foreach (FieldDossier.Saying one in
                        FieldDossier.Debrief(body, sites[s].LayoutSalt, room, everySaying: true))
                    {
                        var note = new FieldNote(one.Text, room, "somewhere", one.Glyph, one.Subjects);
                        foreach (CaseSubjects.Subject subject in CaseSubjects.On(note))
                        {
                            if (subject.Of == CaseSubjects.Kind.Person)
                            {
                                // PRINTED, in this very sentence. Not "printed somewhere in the game" —
                                // the captain is reading THIS entry, and a heading they cannot trace back
                                // to words they read is a heading the game invented.
                                Assert.True(one.Text.Contains(subject.Name, StringComparison.Ordinal),
                                    $"{body}/{s}/{room} · {one.Beat} declares the person \"{subject.Name}\" "
                                    + "and does not print it (#741: never a personal name the game has not "
                                    + $"printed). The sentence was: {one.Text}");
                                continue;
                            }

                            // …and the other way round: an office or a door that is really somebody's name
                            // would smuggle a person onto the page under a 🏛.
                            foreach (string part in subject.Name.Split(' ',
                                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            {
                                Assert.False(names.Contains(part),
                                    $"{body}/{s}/{room} · {one.Beat} files \"{subject.Name}\" as "
                                    + $"{subject.Of}, and \"{part}\" is a name off the dossier's own pools.");
                            }
                        }
                    }
                }
            }
        }
    }

    // ── GUARD (d) · THE BADGE, ON THE SECOND ENTRY, ONCE ────────────────────────────────────────────────

    /// <summary>
    /// The badge exists exactly when a stack has just become a stack, and it counts in words. The FIRST
    /// entry about a name gets nothing — noticing a thread form is the beat, and a badge on a fresh subject
    /// would be the game announcing an ordinary note.
    ///
    /// <para><b>Proven able to fail:</b> return a badge when the count is 1 and the first assertion goes
    /// red; count from zero and the word comes out wrong.</para>
    /// </summary>
    [Fact]
    public void TheBadgeLandsOnTheSecondEntryAndCountsInWords()
    {
        string works = CaseSubjects.Line(CaseSubjects.Office("OFFICE OF WORKS · SUB-REGISTRY"));
        var first = new FieldNote("🎫 a card, countersigned.", 10, "Luna · A", "🎫", works);
        var second = new FieldNote("🎫 another card, countersigned.", 20, "Luna · B", "🎫", works);
        var third = new FieldNote("🎫 a third card.", 30, "Miranda · C", "🎫", works);

        IReadOnlyList<FieldNote> book = FieldNotes.Append(null, first);
        Assert.Null(CaseSubjects.NewThreadBadge(first, book));

        book = FieldNotes.Append(book, second);
        Assert.Equal("second entry about OFFICE OF WORKS · SUB-REGISTRY",
            CaseSubjects.NewThreadBadge(second, book));

        book = FieldNotes.Append(book, third);
        Assert.Equal("third entry about OFFICE OF WORKS · SUB-REGISTRY",
            CaseSubjects.NewThreadBadge(third, book));

        // An entry that names nothing gets nothing, whatever else is in the book.
        var quiet = new FieldNote("📡 It names the moon and nothing finer.", 40, "Luna · A", "📡");
        Assert.Null(CaseSubjects.NewThreadBadge(quiet, FieldNotes.Append(book, quiet)));

        // The count word is the house's, all the way up, and it stops counting rather than printing "11th".
        Assert.Equal("second", CaseSubjects.Ordinal(2));
        Assert.Equal("tenth", CaseSubjects.Ordinal(10));
        Assert.Equal("another", CaseSubjects.Ordinal(11));
        Assert.Equal("another", CaseSubjects.Ordinal(1));
    }

    /// <summary>Where an entry names two things that both already have stacks, the badge names the DEEPER
    /// one — one line about one name, never a list, and never the shallowest thing on the page.</summary>
    [Fact]
    public void TheBadgeNamesTheDeepestStackAndSaysOneThing()
    {
        CaseSubjects.Subject office = CaseSubjects.Office("ESTATES · SPECIAL PROJECTS");
        CaseSubjects.Subject door = CaseSubjects.Place("The Tilt");

        IReadOnlyList<FieldNote> book =
        [
            new("a", 1, "p", "📋", CaseSubjects.Line(office)),
            new("b", 2, "p", "📋", CaseSubjects.Line(office)),
            new("c", 3, "p", "📋", CaseSubjects.Line(office, door)),
        ];

        string? badge = CaseSubjects.NewThreadBadge(book[2], book);
        Assert.Equal("third entry about ESTATES · SPECIAL PROJECTS", badge);
        Assert.DoesNotContain("The Tilt", badge!, StringComparison.Ordinal);
    }

    /// <summary>THE BADGE IS A CARD LINE, NOT A BANNER, and the pen is untouched by it. The client half —
    /// the badge is composed onto the pop-up in front of the captain (#736), once — is pinned in
    /// <c>TheThreadsPageIsInTheSatchelTests</c>; this end proves the model offers no banner to raise: it
    /// returns one sentence and knows nothing about a HUD.</summary>
    [Fact]
    public void TheBadgeIsOneSentenceAndCongratulatesNobody()
    {
        string line = CaseSubjects.NewThreadBadge(
            new FieldNote("x", 2, "p", "📋", CaseSubjects.Line(CaseSubjects.Place("Selene Gate"))),
            [
                new("w", 1, "p", "📋", CaseSubjects.Line(CaseSubjects.Place("Selene Gate"))),
                new("x", 2, "p", "📋", CaseSubjects.Line(CaseSubjects.Place("Selene Gate"))),
            ])!;

        Assert.DoesNotContain("\n", line, StringComparison.Ordinal);
        foreach (string tell in new[]
        {
            "!", "well done", "nice", "connected", "lead", "solved", "unlocked", "bonus",
        })
        {
            Assert.DoesNotContain(tell, line, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── GUARD (e) · A SAVE THAT PREDATES ALL OF THIS ────────────────────────────────────────────────────

    /// <summary>
    /// An old book loads, keeps every word, and has nothing said twice — which is honestly what it had.
    /// A save WITH subjects round-trips them and comes back with its stacks standing.
    ///
    /// <para><b>Proven able to fail:</b> make <see cref="CaseSubjects.On"/> throw on a null field, or read
    /// the subjects off anything but the note, and the pre-#741 half goes red.</para>
    /// </summary>
    [Fact]
    public void APreThreadsSave_LoadsWithNothingSaidTwice()
    {
        // Notes minted the old way — four fields, exactly as every save before this build holds them.
        var old = new Vault
        {
            FieldNotes = new FieldNotesSection
            {
                Notes =
                [
                    new FieldNote("🗂 Ilse Vandermeer — a specialist in continuity engineering.", 100, "Luna · A", "🗂"),
                    new FieldNote("🎟 \"Ilse Vandermeer sent me.\" — at The Tilt.", 200, "Luna · A", "🎟"),
                ],
            },
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(old));
        IReadOnlyList<FieldNote> reread = loaded.FieldNotes!.Notes;

        Assert.Equal(2, reread.Count);
        Assert.All(reread, n => Assert.Empty(CaseSubjects.On(n)));

        // Two entries that plainly rhyme, and the book says NOTHING about them — because nobody declared a
        // subject when they were written and this build does not go back and read the prose to find one.
        Assert.Empty(CaseSubjects.ThreadsOf(reread));
        Assert.Null(CaseSubjects.NewThreadBadge(reread[1], reread));

        // …and the red pen still finds them, which is the point of the handle being derived (#741's pen).
        Assert.Equal(2, CaseThreads.Page(reread, null).Count);
    }

    /// <summary>…and a save written by THIS build brings its subjects home, headings and all.</summary>
    [Fact]
    public void ASaveWithSubjects_BringsItsStacksBack()
    {
        string works = CaseSubjects.Line(
            CaseSubjects.Office("OFFICE OF WORKS · SUB-REGISTRY"), CaseSubjects.Place("The Tilt"));

        var vault = new Vault
        {
            FieldNotes = new FieldNotesSection
            {
                Notes =
                [
                    new FieldNote("🎫 one", 100, "Luna · A", "🎫", works),
                    new FieldNote("🎫 two", 200, "Miranda · B", "🎫", works),
                ],
            },
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.False(loaded.Tampered);

        IReadOnlyList<CaseSubjects.SubjectThread> stacks = CaseSubjects.ThreadsOf(loaded.FieldNotes!.Notes);
        Assert.Equal(2, stacks.Count);
        Assert.Contains(stacks, t => t.Heading == "🏛 OFFICE OF WORKS · SUB-REGISTRY");
        Assert.Contains(stacks, t => t.Heading == "📍 The Tilt");
        Assert.All(stacks, t => Assert.Equal(2, t.Entries.Count));
    }

    /// <summary>Junk in the field is dropped rather than thrown over, exactly as a stored red line is.</summary>
    [Fact]
    public void ASubjectThisBuildCannotReadIsDropped()
    {
        Assert.False(CaseSubjects.TryRead(null, out _));
        Assert.False(CaseSubjects.TryRead("", out _));
        Assert.False(CaseSubjects.TryRead("o", out _));
        Assert.False(CaseSubjects.TryRead("no separator here", out _));
        Assert.True(CaseSubjects.TryRead(CaseSubjects.Office("A").Id, out CaseSubjects.Subject read));
        Assert.Equal(CaseSubjects.Kind.Office, read.Of);

        // A note whose field is half-readable keeps the half it can read.
        var note = new FieldNote("x", 1, "p", "📋", $"garbage{CaseSubjects.Place("The Deep").Id}");
        Assert.Equal([CaseSubjects.Place("The Deep")], CaseSubjects.On(note));

        // An author that declares the same thing twice files one subject, not two.
        Assert.Equal(
            CaseSubjects.Line(CaseSubjects.Place("The Deep")),
            CaseSubjects.Line(CaseSubjects.Place("The Deep"), CaseSubjects.Place("The Deep")));
    }

    // ── THE CANON SWEEP ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every sentence this feature can put on a screen is listed for the owner's blessing, none is
    /// a placeholder, and not one of them draws a conclusion or congratulates. The register is the book's:
    /// it keeps it, and it keeps no opinion about it.</summary>
    [Fact]
    public void EverySentenceIsListedAndNoneOfThemConcludes()
    {
        List<string> prose = [.. CaseSubjects.AllProse()];
        Assert.Equal(6, prose.Count);

        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain("TODO", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TBD", line, StringComparison.OrdinalIgnoreCase);

            foreach (string tell in new[]
            {
                "congratulat", "well done", "you were right", "score", "suspicious", "proves", "conspiracy",
            })
            {
                Assert.DoesNotContain(tell, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.Equal("Nothing in this book names the same thing twice. Yet.", CaseSubjects.NothingTwiceLine);
        Assert.Equal("1 entry", CaseSubjects.EntriesLabel(1));
        Assert.Equal("4 entries", CaseSubjects.EntriesLabel(4));
    }
}
