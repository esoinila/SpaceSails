using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1074 beat 3 · THE MONEY TRAIL AS A CLUE KIND — <i>"if nothing is there, why forbid the look — empty +
/// expensive is still a signal."</i>
///
/// <para>Three line items, each a paper find on a closed or a fenced working, each clippable into the field
/// book under the office that is paying and the ground it is paying for. The cost centre is the tell and it
/// is the only tell.</para>
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names; the revert is
/// quoted on each one, in the shape this ground has used since #587's lesson — a guard that has never failed
/// is a guard nobody has checked.</para>
///
/// <para><b>THE WORLD THE GUARDS RUN IN IS DERIVED AND NEVER TYPED</b>, and it is deliberately a family of
/// ids no other suite asks about, exactly as <c>TheStopOrderAtTheDigTests</c>' is: both registers only ever
/// change the answer for the ids IN them, so a guard here that closes a working of its own cannot move any
/// other guard's world, whatever order xUnit runs them in. Restoring in a <c>finally</c> is belt as well as
/// braces.</para>
/// </summary>
public sealed class TheMoneyTrailTests
{
    /// <summary>How many generated rocks the sweeps walk to find grounds with halls. The band is about one
    /// site in fifty; a ten-site sample tells you nothing about it. Same number and same reasoning as
    /// <c>TheStopOrderAtTheDigTests</c>' own sweep.</summary>
    private const int Probes = 4000;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The site's name as the game prints it under the shuttle, which is the second of the two
    /// subjects. A fixed string here rather than a generated one because the guard is about WHICH NAME the
    /// author declared, not about what a generator called a crater.</summary>
    private const string SiteName = "The Crater Shelf";

    private static List<string> Grounds() => _grounds ??= Sweep();

    private static List<string>? _grounds;

    private static List<string> Sweep()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"money-ground-{i}";
            if (UndergroundComplex.HasFoundBand(body))
            {
                found.Add(body);
            }
        }
        Assert.True(found.Count > 40,
            $"only {found.Count} of {Probes} generated money grounds had halls — this proves little.");
        return found;
    }

    /// <summary>Close the working on these grounds for the length of one guard, and put the world back
    /// afterwards whatever happens.</summary>
    private static IDisposable Stopped(params string[] bodies)
    {
        StopOrder.Install([.. bodies]);
        return new Restore();
    }

    /// <summary>…and take them into official care as well, which is the only state the rail and the rota are
    /// ever bought in.</summary>
    private static IDisposable Fenced(params string[] bodies)
    {
        StopOrder.Install([.. bodies]);
        PreservationZone.Install([.. bodies]);
        return new Restore();
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose()
        {
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }
    }

    // ══ LAW 1 · WHO BUYS WHAT, AND WHERE NOBODY BUYS ANYTHING ════════════════════════════════════════════

    /// <summary>
    /// #1074 · THE POUR IS ON A CLOSED WORKING, THE RAIL AND THE ROTA ARE ON A FENCED ONE, AND AN UNSTOPPED
    /// GROUND CARRIES NONE OF THE THREE.
    ///
    /// <para>Four worlds over one population of grounds, which is what makes the negatives mean anything: a
    /// world where nothing has been closed, a world where the working is closed and nothing more, a world
    /// where the site is in official care, and — the one the construction is meant to survive — a save that
    /// somehow hands over a preserved ground that nobody ever stopped. The last one buys nothing at all,
    /// because a line item on a ground nobody closed is the beat pointing at a site with nothing wrong with
    /// it.</para>
    ///
    /// <para><b>Reverts that reddened them (watched go red, then restored):</b> the <c>StopOrder.On</c>
    /// gate defeated in <c>MoneyTrailRoomFor</c> — <i>"Assert.Null() Failure: Value of type
    /// 'Nullable&lt;ValueTuple&lt;int, int&gt;&gt;' has a value"</i>, the pour on the books of a working
    /// nobody had closed; and the <c>MoneyTrail.NeedsTheFence</c> gate defeated — the same failure one line
    /// down, sixteen sections of perimeter rail invoiced for a site with no perimeter.</para>
    /// </summary>
    [Fact]
    public void ThePourIsOnAClosedWorkingAndTheRailAndRotaOnlyOnAFencedOne()
    {
        foreach (string body in Grounds().Take(8))
        {
            // 1 · A ground nobody has touched buys nothing.
            foreach (MoneyTrail.Item item in Items)
            {
                Assert.Null(UndergroundComplex.MoneyTrailRoomFor(body, item));
            }

            // 2 · A closed working carries the pour and nothing else.
            using (Stopped(body))
            {
                Assert.NotNull(UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Pour));
                Assert.Null(UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Rail));
                Assert.Null(UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Rota));
            }

            // 3 · A site in official care carries all three, in three different rooms of one floor.
            using (Fenced(body))
            {
                var at = new List<(int Level, int RoomIndex)>();
                foreach (MoneyTrail.Item item in Items)
                {
                    (int, int)? where = UndergroundComplex.MoneyTrailRoomFor(body, item);
                    Assert.NotNull(where);
                    at.Add(where!.Value);
                }
                Assert.Single(at.Select(a => a.Level).Distinct());
                Assert.Equal(3, at.Select(a => a.RoomIndex).Distinct().Count());
            }

            // 4 · …and a fence with no stop under it buys nothing at all.
            PreservationZone.Install([body]);
            try
            {
                foreach (MoneyTrail.Item item in Items)
                {
                    Assert.Null(UndergroundComplex.MoneyTrailRoomFor(body, item));
                }
            }
            finally
            {
                PreservationZone.Install([]);
            }
        }
    }

    /// <summary>
    /// #1074 · …AND THE ROOMS EXIST, ARE NEVER THE FIRST ROOM, AND COLLIDE WITH NO OTHER DESIGNATION.
    ///
    /// <para>Six designations now share this building and the whole point of a designation is that it is not
    /// a roll, so a collision would silently delete one authored paper and nothing on screen would ever say
    /// so. The room is also checked to EXIST on the field the rest of the suite builds — a designated index
    /// past the end of a floor's pool is a paper nobody can ever reach, which is the same silent absence the
    /// designations were written against.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <c>MoneyTrailPourRoom</c> set to 0 — <i>"Assert.NotEqual()
    /// Failure: Values are equal"</i>, the pour handed over by the first search on the floor; and
    /// <c>MoneyTrailRotaRoom</c> set to <c>MoneyTrailRailRoom</c>'s 2 — <i>"Assert.Equal() Failure: Strings
    /// differ"</i>, the rota's room reading back the rail's invoice because two designations had landed on
    /// one room and one of the papers was simply gone.</para>
    /// </summary>
    [Fact]
    public void TheRoomsExistAreNeverTheFirstAndCollideWithNothingElse()
    {
        foreach (string body in Grounds().Take(8))
        {
            using (Fenced(body))
            {
                foreach (MoneyTrail.Item item in Items)
                {
                    (int level, int room) = UndergroundComplex.MoneyTrailRoomFor(body, item)!.Value;

                    Assert.Equal(UndergroundComplex.TopPressurisedFloor(body), level);
                    Assert.NotEqual(0, room);
                    Assert.NotEqual(UndergroundComplex.KeyRoomFor(body), (level, room));
                    Assert.NotEqual(UndergroundComplex.RelicRoomFor(body), (level, room));
                    Assert.NotEqual(UndergroundComplex.FoundKeyRoomFor(body), (level, room));
                    Assert.NotEqual(UndergroundComplex.StandingOrderRoomFor(body), (level, room));
                    Assert.NotEqual(UndergroundComplex.ValveBookRoomFor(body), (level, room));
                    Assert.Null(UndergroundComplex.MaintenanceLedgerRoomFor(body));   // never both (#1063)

                    UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                    Assert.InRange(room, 0, floor.RoomCentres.Count - 1);

                    // The room holds the paper, and the paper is that item's authored line and no other.
                    Assert.Equal(UndergroundComplex.Haul.Records,
                        UndergroundComplex.InRoom(body, level, room));
                    Assert.Equal(
                        UndergroundComplex.MoneyTrailLine(item),
                        UndergroundComplex.HaulLine(
                            UndergroundComplex.Haul.Records, body, level, room, null));
                    Assert.Contains(MoneyTrail.TextOf(item),
                        UndergroundComplex.MoneyTrailLine(item), StringComparison.Ordinal);
                }
            }
        }
    }

    // ══ LAW 2 · RARITY, MEASURED IN A WORLD THAT CAN TELL PASS FROM FAIL ═════════════════════════════════

    /// <summary>
    /// #1074 · THREE PAPERS ON A FLOOR THE CAPTAIN HAS TO TURN OVER, AND NONE ANYWHERE ELSE.
    ///
    /// <para>The rarity shape is the valve-book's: designated, so it is always there, and buried in a pool
    /// big enough that finding it is a search. This measures that rather than asserting it — the share of
    /// the works floor that is a line item, over the whole swept population — and pins it inside a band, for
    /// the reason the found band's own incidence is measured rather than equated: a rate written down as an
    /// equality is a rate nobody may ever re-seed.</para>
    ///
    /// <para><b>AND THE WORLD CAN TELL PASS FROM FAIL</b>, which is the half this repo has been bitten by
    /// (the fifth named bug class: a guard handed a world where every answer is the same). The identical
    /// sweep is run twice over the identical grounds — once with the registers empty and once with the sites
    /// in care — and the first must find <b>zero</b> line items in the very rooms the second finds three.
    /// A predicate wired to a constant would fail that on the spot.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <c>MoneyTrailRotaRoom</c> raised to 14, one past the narrowest
    /// works floor in the family — <i>"the narrowest works floor in the family carries 14 searchable rooms,
    /// and the rota's paper is designated to room 14"</i>, a designation off the end of a pool and therefore
    /// a paper nobody could ever reach; <c>MoneyTrailPaperIn</c> answering the pour for every room on
    /// the floor — <i>"Assert.Equal() Failure: Values differ"</i> on the count of papers a fenced site
    /// carries, a floor that is nothing but invoices; and both register gates in <c>MoneyTrailRoomFor</c>
    /// defeated so the answer no longer depends on the world at all — the same failure on
    /// <c>quietPapers</c>, which is exactly the half a guard handed an undiscriminating world cannot
    /// see.</para>
    /// </summary>
    [Fact]
    [Trait("speed", "slow")]
    public void TheLineItemsAreThreeRoomsOfAFloorAndNothingInAWorldThatBoughtNothing()
    {
        List<string> grounds = Grounds();
        int rooms = 0, quietPapers = 0, fencedPapers = 0, narrowest = int.MaxValue;

        foreach (string body in grounds)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } works)
            {
                continue;
            }
            int count = UndergroundComplex.Build(body, works, Field).RoomCentres.Count;
            rooms += count;
            narrowest = Math.Min(narrowest, count);

            // The world nobody closed anything in — the same rooms, asked the same question.
            for (int r = 0; r < count; r++)
            {
                if (UndergroundComplex.MoneyTrailPaperIn(body, works, r) is not null)
                {
                    quietPapers++;
                }
            }

            using (Fenced(body))
            {
                for (int r = 0; r < count; r++)
                {
                    if (UndergroundComplex.MoneyTrailPaperIn(body, works, r) is not null)
                    {
                        fencedPapers++;
                    }
                }
            }
        }

        Assert.True(rooms > 500, $"only {rooms} works-floor rooms were swept — this proves little.");

        // THE NARROWEST WORKS FLOOR IN THE WHOLE FAMILY STILL HAS ROOM FOR THREE DESIGNATIONS, and it is
        // asked FIRST so that it names the cause rather than the symptom: the moment a floor came back
        // shorter than the highest index this beat designates, that paper would be past the end of a pool
        // and therefore unreachable forever, and every count below would merely come out low. It is fourteen
        // rooms at the narrowest, which is the number UndergroundComplex.MoneyTrail.cs's own note quotes —
        // measured here rather than remembered, which is this repo's rule about numbers in prose.
        Assert.True(narrowest >= UndergroundComplex.MoneyTrailRotaRoom + 1,
            $"the narrowest works floor in the family carries {narrowest} searchable rooms, and the rota's "
            + $"paper is designated to room {UndergroundComplex.MoneyTrailRotaRoom}");
        Assert.InRange(narrowest, UndergroundComplex.MoneyTrailRotaRoom + 1, 40);

        Assert.Equal(0, quietPapers);
        Assert.Equal(3 * grounds.Count, fencedPapers);

        // A search on this floor turns one up now and then, and never four presses in five.
        double share = (double)fencedPapers / rooms;
        Assert.InRange(share, 0.01, 0.30);
    }

    /// <summary>
    /// #1074/#603 · A LINE ITEM IS AN ORDINARY CLIPPED PAPER — SEEDED CERTAINTY AND NO NEW KIND.
    ///
    /// <para>#1063 flagged typed clues as optional and they stay optional. The paper goes into the pocket as
    /// <see cref="Satchel.Kind.Paper"/> under the building's own find id, and its certainty is rolled off
    /// that id exactly as every other paper in the game is. This asserts the seeding is genuinely a seeding:
    /// over the swept population all three certainties come up, which a fixed answer could not do.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>FieldClue.CertaintyOf</c>'s seed tag stripped of its paper
    /// id, so every paper in the game rolled one answer — <i>"Assert.Equal() Failure: Values differ"</i> on
    /// the count of certainties the population came back with (three expected, one seen).</para>
    /// </summary>
    [Fact]
    [Trait("speed", "slow")]
    public void ALineItemIsAnOrdinaryPaperWithAnOrdinarySeededCertainty()
    {
        var seen = new HashSet<FieldClue.Certainty>();

        foreach (string body in Grounds())
        {
            using (Fenced(body))
            {
                foreach (MoneyTrail.Item item in Items)
                {
                    (int level, int room) = UndergroundComplex.MoneyTrailRoomFor(body, item)!.Value;
                    string findId = UndergroundComplex.FindId(body, level, room);

                    UndergroundComplex.Pickup pick = UndergroundComplex.WhatGoesInThePocket(
                        UndergroundComplex.Haul.Records, body, null, findId, []);
                    Assert.True(pick.RoomEmptied);
                    Assert.NotNull(pick.Take);
                    Assert.Equal(Satchel.Kind.Paper, pick.Take!.Value.Kind);

                    seen.Add(FieldClue.CertaintyOf(findId));
                }
            }
        }

        Assert.Equal(3, seen.Count);
    }

    // ══ LAW 3 · WHAT THE BOOK FILES IT UNDER ═════════════════════════════════════════════════════════════

    /// <summary>
    /// #1074/#741 · EXACTLY TWO SUBJECTS — THE OFFICE AND THE GROUND — AND THEY ARE THE ONES THE REST OF THE
    /// GAME ALREADY USES.
    ///
    /// <para>The office is <see cref="StopOrder.Stamp"/> itself and not a second string spelled the same
    /// way, so the plate at the seal, the notice at the gate and the heading in the field book are one
    /// office; the ground is minted through <see cref="CaseSubjects.Place"/>, which is the same door #1083's
    /// dropped premium schedule goes through. Both are names the game has printed for the captain to read,
    /// which is <see cref="CaseSubjects"/>' first law.</para>
    ///
    /// <para>And every other room in the building files under NOTHING, which is what keeps this from being a
    /// subject the game sprays over the floor.</para>
    ///
    /// <para><b>Reverts that reddened them:</b> the office minted as
    /// <c>CaseSubjects.Office("Authority")</c> — <i>"Assert.Contains() Failure: Item not found in
    /// collection"</i>, the field book keeping its own spelling of an office the plate spells differently
    /// and quietly starting a second thread with it; and <c>MoneyTrailSubjectsFor</c> answering the subjects
    /// unconditionally — <i>"Assert.Equal() Failure: Strings differ"</i>, every stripped room on the floor
    /// filed under the Authority.</para>
    /// </summary>
    [Fact]
    public void EveryLineItemIsFiledUnderTheOfficeAndTheGroundAndNothingElseIs()
    {
        foreach (string body in Grounds().Take(6))
        {
            using (Fenced(body))
            {
                int works = UndergroundComplex.TopPressurisedFloor(body)!.Value;

                foreach (MoneyTrail.Item item in Items)
                {
                    (int level, int room) = UndergroundComplex.MoneyTrailRoomFor(body, item)!.Value;
                    string subjects =
                        UndergroundComplex.MoneyTrailSubjectsFor(body, level, room, SiteName);

                    var note = new FieldNote(
                        UndergroundComplex.MoneyTrailLine(item), 1, SiteName, "🔦", subjects);
                    IReadOnlyList<CaseSubjects.Subject> on = CaseSubjects.On(note);

                    Assert.Equal(2, on.Count);
                    Assert.Contains(CaseSubjects.Office(StopOrder.Stamp), on);
                    Assert.Contains(CaseSubjects.Place(SiteName), on);
                    Assert.DoesNotContain(on, s => s.Of == CaseSubjects.Kind.Person);
                }

                // …and every room on that floor that is NOT a line item names nothing at all.
                int count = UndergroundComplex.Build(body, works, Field).RoomCentres.Count;
                for (int r = 0; r < count; r++)
                {
                    if (UndergroundComplex.MoneyTrailPaperIn(body, works, r) is null)
                    {
                        Assert.Equal("",
                            UndergroundComplex.MoneyTrailSubjectsFor(body, works, r, SiteName));
                    }
                }
            }
        }
    }

    /// <summary>
    /// #1074/#934 · TWO OF THEM MAKE A THREAD UNDER THE AUTHORITY, AND THE BOOK SAYS NOTHING OVER IT.
    ///
    /// <para>The payoff of the whole beat, and the law that governs it (#741, and #587 before it): the
    /// THREADS view stacks a rail, a rota and a pour under one heading in the order they were written, and
    /// there is no total, no summary, no verdict and no arrow. The heading is the office's glyph and the
    /// office's name and nothing else — which is checked by comparing against
    /// <see cref="CaseSubjects.Subject.Heading"/> rather than by matching a string this guard made up.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the <c>Place</c> subject dropped from
    /// <c>MoneyTrail.SubjectsFor</c> — <i>"Assert.Contains() Failure: Filter not matched in
    /// collection"</i>, the ground never joining its own stack; and the office subject dropped —
    /// <i>"Assert.Single() Failure: The collection did not contain any matching items"</i>, three invoices
    /// about one office and no thread anywhere.</para>
    /// </summary>
    [Fact]
    public void TheThreeStackUnderOneHeadingAndTheBookKeepsNoOpinionAboutThem()
    {
        string body = Grounds()[0];
        using (Fenced(body))
        {
            var book = new List<FieldNote>();
            double t = 1;
            foreach (MoneyTrail.Item item in Items)
            {
                (int level, int room) = UndergroundComplex.MoneyTrailRoomFor(body, item)!.Value;
                book.Add(new FieldNote(
                    UndergroundComplex.MoneyTrailLine(item), t++, SiteName, "🔦",
                    UndergroundComplex.MoneyTrailSubjectsFor(body, level, room, SiteName)));
            }

            IReadOnlyList<CaseSubjects.SubjectThread> threads = CaseSubjects.ThreadsOf(book);

            CaseSubjects.Subject office = CaseSubjects.Office(StopOrder.Stamp);
            CaseSubjects.SubjectThread authority = Assert.Single(threads, x => x.Subject == office);
            Assert.Equal(3, authority.Entries.Count);
            Assert.Equal(office.Heading, authority.Heading);

            // The stack is in the order the book wrote it, and it carries the three papers and no fourth.
            Assert.Equal(
                Items.Select(UndergroundComplex.MoneyTrailLine).ToArray(),
                authority.Entries.Select(e => e.Text).ToArray());

            // …and the ground has its own stack, which is the second subject doing its job.
            Assert.Contains(threads, x => x.Subject == CaseSubjects.Place(SiteName));

            // THE BOOK KEEPS NO OPINION. Nothing anywhere in the stack sums, totals, concludes or connects.
            foreach (string said in authority.Entries.Select(e => e.Text)
                .Concat(CaseSubjects.AllProse()).Append(authority.Heading))
            {
                foreach (string verdict in
                    new[] { "total", "sum", "connect", "suspicious", "cover-up", "therefore", "note that" })
                {
                    Assert.DoesNotContain(verdict, said, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    // ══ LAW 4 · THE CANON, AND WHAT MAY NOT BE ON THE PAPER ══════════════════════════════════════════════

    /// <summary>#1074's canon pass of 2026-09-03, retyped here from the issue so the guard has a source the
    /// implementation cannot move. A test that asserted <c>MoneyTrail.RailLineItem == MoneyTrail.RailLineItem</c>
    /// would pass on any sentence anybody ever wrote into it.</summary>
    private static readonly (MoneyTrail.Item Item, string Text)[] Authored =
    [
        (MoneyTrail.Item.Rail,
            "Perimeter rail, sixteen sections, delivered and fixed. Charged to Preservation."),
        (MoneyTrail.Item.Rota,
            "Site watch, two hands, continuous. Charged to Preservation."),
        (MoneyTrail.Item.Pour,
            "Structural remediation, lower galleries, three hundred tonnes. Charged to Preservation."),
    ];

    /// <summary>
    /// #1074 · THE THREE ITEMS ARE THE CANON'S WORDS, CHARACTER FOR CHARACTER, AND THERE IS NO FOURTH
    /// STRING.
    ///
    /// <para>The reflection half is the one that matters over time: every public constant string the type
    /// publishes must be one of the three <see cref="MoneyTrail.AllProse"/> yields, so a helpful sentence
    /// added later cannot escape the canon grep by not being in the list. That is beat 1's own arrangement
    /// and it is kept for beat 1's own reason.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <i>"Site watch, two hands, continuous, day and night. Charged
    /// to Preservation."</i> — <i>"Assert.Equal() Failure: Strings differ"</i>; and a fourth constant added
    /// to the type — <i>"Assert.All() Failure: 1 out of 4 items in the collection did not pass"</i>, which
    /// is the sweep naming the string no canon grep could see.</para>
    /// </summary>
    [Fact]
    public void TheThreeItemsAreTheCanonsWordsAndThereIsNoFourth()
    {
        foreach ((MoneyTrail.Item item, string text) in Authored)
        {
            Assert.Equal(text, MoneyTrail.TextOf(item));
        }

        Assert.Equal(
            Authored.Select(a => a.Text).OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            MoneyTrail.AllProse().OrderBy(s => s, StringComparer.Ordinal).ToArray());

        var published = new List<string>();
        foreach (System.Reflection.FieldInfo f in
            typeof(MoneyTrail).GetFields(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            if (f.FieldType == typeof(string) && f.GetValue(null) is string value)
            {
                published.Add(value);
            }
        }
        Assert.All(published, s =>
            Assert.True(MoneyTrail.AllProse().Contains(s, StringComparer.Ordinal),
                $"MoneyTrail publishes a string no canon grep can see: \"{s}\""));
    }

    /// <summary>§8 — there is ONE of these and a line item never borrows the word. The same list
    /// <c>TheStopOrderAtTheDigTests</c> and <c>TheBurialTests</c> keep, because every paper this arc deals
    /// is one trigger's paperwork and a word forbidden on one is forbidden on all of them.</summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
    ];

    /// <summary>
    /// #1074/#672 · SCULLY READS THREE TRUE INVOICING LINES: NO AMOUNT IN MONEY, NO SIGNATURE, NO DEPARTMENT
    /// BUT THE COST CENTRE, AND NOT A WORD §8 RESERVES.
    ///
    /// <para><b>No amount in money</b> is checked as the absence of any DIGIT at all: every quantity on these
    /// papers is spelled out in words the way a works ledger spells them, and a figure would hand the reader
    /// the sum the beat exists to make them do. <b>No department but the cost centre</b> is checked by
    /// pulling every capitalised word that is not starting a sentence and demanding the set be exactly
    /// <i>Preservation</i> — which is also, and not by accident, the whole of the tell. <b>No signature</b>
    /// is the doctrine's first law: the enforcer is an office, and no name, initial or countersignature
    /// appears on any of the three.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <i>"Perimeter rail, 16 sections…"</i> — <i>"a line item
    /// carries a figure: \"16\""</i>; <i>"Charged to Preservation, Plant."</i> —
    /// <i>"Assert.DoesNotContain() Failure: Sub-string found"</i>, a second office named on a paper that may
    /// name one; and <i>"Structural remediation of the ancient galleries…"</i> — <i>"a line item settles
    /// what it must leave open"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingOnALineItemIsFalseAndNothingOnItIsAName()
    {
        var strings = new List<(string Where, string Text)>();
        foreach (string s in MoneyTrail.AllProse())
        {
            strings.Add(("MoneyTrail.AllProse", s));
        }
        foreach (MoneyTrail.Item item in Items)
        {
            strings.Add(("UndergroundComplex.MoneyTrailLine", UndergroundComplex.MoneyTrailLine(item)));
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
            "a line item settles what it must leave open:\n  " + string.Join("\n  ", named));

        foreach (string text in MoneyTrail.AllProse())
        {
            // NO AMOUNT IN MONEY, and no figure of any kind.
            Match figure = Regex.Match(text, @"\d+");
            Assert.False(figure.Success, $"a line item carries a figure: \"{figure.Value}\"");
            foreach (char sign in "$£€¤")
            {
                Assert.DoesNotContain(sign.ToString(), text, StringComparison.Ordinal);
            }

            // NO SIGNATURE. Nobody signs, initials or countersigns one of these.
            foreach (string hand in new[] { "signed", "signature", "countersign", "initial", "per pro", "sgd" })
            {
                Assert.DoesNotContain(hand, text, StringComparison.OrdinalIgnoreCase);
            }

            // NO DEPARTMENT BUT THE COST CENTRE. Every one of the eight the signage deals is absent…
            foreach (string department in UndergroundComplex.Departments)
            {
                Assert.DoesNotContain(department, text, StringComparison.OrdinalIgnoreCase);
            }

            // …and the only capitalised word that is not opening a sentence is the cost centre itself.
            var capitals = new List<string>();
            foreach (Match m in Regex.Matches(text, @"(?<=[^.!?]\s)\b[A-Z][a-z]+\b"))
            {
                capitals.Add(m.Value);
            }
            Assert.All(capitals, c =>
                Assert.True(string.Equals(c, "Preservation", StringComparison.Ordinal),
                    $"a line item names something other than its cost centre: {c}"));
            Assert.Contains("Charged to Preservation.", text, StringComparison.Ordinal);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The three, in the order a site buys them.</summary>
    private static readonly MoneyTrail.Item[] Items =
        [MoneyTrail.Item.Pour, MoneyTrail.Item.Rail, MoneyTrail.Item.Rota];
}
