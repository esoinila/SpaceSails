using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1332 D and E · <b>THE FORWARDING DESK AND THE ADJUSTER'S ROOM — THE WORDS, THE HOURS AND THE PAPERS.</b> Fable
/// canon, verbatim; the ajar watch is one in five (D) and one in four (E), each seeded on the run off its own stream
/// and pure; each paper reads as papers read away from their room; the book files D's note under Plant and Cinder
/// Roost and files E's form under nothing; nothing spends a reserved word and nothing names anybody. The kit's
/// shape (<see cref="SideOffices"/>) is swept over all three offices, slice C's among them.
/// </summary>
public sealed class TheSideOfficesTests
{
    /// <summary>The offices D and E added — the two this file is about.</summary>
    public static TheoryData<string> TheTwoNewOffices => new() { "forwarding", "adjuster" };

    private static SideOffice Office(string id) => SideOffices.All.Single(o => o.Id == id);

    /// <summary>
    /// <b>THE CANON, TO THE BYTE, AND NOTHING ELSE.</b> Retyped from the design comment (#1332, 2026-09-30) so the
    /// guard has a source the implementation cannot move: plates, shut lines, book first-reads, ajar lines, the papers
    /// with their ids, titles and documents, the havens and the cabins.
    ///
    /// <para><b>Proven RED</b> by a hyphen for the forwarding plate's middle dot, a full stop dropped from the
    /// adjuster's shut line, and a seventh string in <c>AllProse</c>.</para>
    /// </summary>
    [Fact]
    public void EveryLineIsFablesVerbatim()
    {
        Assert.Equal("cinder-roost", ForwardingDesk.HavenId);
        Assert.Equal(2, ForwardingDesk.Cabin);
        Assert.Equal("COLD-CHAIN FORWARDING · TRADE ONLY", ForwardingDesk.DoorPlate);
        Assert.Equal("Consignments by arrangement. Arrangements are not made here.", ForwardingDesk.ShutLine);
        Assert.Equal(
            "A forwarding desk for goods that must not get warm. The berth hotel is a strange place to keep it.",
            ForwardingDesk.PlateReadLine);
        Assert.Equal("Cold. The room is colder than the corridor, and the corridor is a rock.", ForwardingDesk.AjarLine);
        Assert.Equal("forwarding-consignment", ForwardingDesk.SheetId);
        Assert.Equal("A consignment note", ForwardingDesk.SheetTitle);
        Assert.Equal(
            "Reagent, forty kilos, cold. Origin withheld. Destination: Deep Storage. Charged to Plant.",
            ForwardingDesk.SheetDocument);
        Assert.Equal("Plant", ForwardingDesk.TheOfficeName);
        Assert.Equal(5, ForwardingDesk.WatchesPerTurn);

        Assert.Equal("the-deep", AdjustersRoom.HavenId);
        Assert.Equal(4, AdjustersRoom.Cabin);
        Assert.Equal("NEBULA MUTUAL · CLAIMS · KNOCK", AdjustersRoom.DoorPlate);
        Assert.Equal("The adjuster keeps hours. The hours are kept elsewhere.", AdjustersRoom.ShutLine);
        Assert.Equal(
            "A claims room with the light off. The policy says they pay for death; the door says they take appointments.",
            AdjustersRoom.PlateReadLine);
        Assert.Equal(
            "A desk, two chairs, one of them for you. The form on the desk has your kind of loss on it and no line for your name.",
            AdjustersRoom.AjarLine);
        Assert.Equal("claim-form-blank", AdjustersRoom.SheetId);
        Assert.Equal("A claim form, blank", AdjustersRoom.SheetTitle);
        Assert.Equal(
            "Loss (other than death): describe. Witness: name. Captain present: yes. Nebula Mutual pays what the form says, when the form is complete.",
            AdjustersRoom.SheetDocument);
        Assert.Equal(4, AdjustersRoom.WatchesPerTurn);

        Assert.Equal(6, ForwardingDesk.AllProse().Count());
        Assert.Equal(6, ForwardingDesk.AllProse().Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(6, AdjustersRoom.AllProse().Count());
        Assert.Equal(6, AdjustersRoom.AllProse().Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// <b>NOT ONE WORD SPENDS A RESERVED WORD, AND NOT ONE NAMES ANYBODY.</b> An office, never a name (#1074): no
    /// regular's name, no digit, and no word of the arc the captain has not yet earned.
    ///
    /// <para><b>Proven RED</b> by a figure in the consignment note ("forty kilos" typed as "40 kilos").</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoNewOffices))]
    public void NotOneWordSpendsAReservedWordOrNamesAnybody(string id)
    {
        string[] reserved =
        [
            "monolith", "reever", "old one", "old ones", "ancient", "alien", "not ours", "not natural",
            "restore", "backup", "kaamos", "minister", "donor", "they were people", "whose", "who made",
        ];

        SideOffice office = Office(id);
        Assert.Equal(6, office.Prose().Count());
        foreach (string line in office.Prose())
        {
            foreach (string word in reserved)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }

            foreach (string regular in PatronRota.Roster)
            {
                Assert.DoesNotContain(regular, line, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain(line, char.IsDigit);
        }
    }

    /// <summary>
    /// <b>EACH DOOR STANDS AJAR ON ONE WATCH IN ITS OWN N, SEEDED ON THE RUN, AND PURE</b> — swept over sixty-four
    /// runs and many windows rather than sampled once. In every window of N consecutive watches (negative ones too:
    /// the clock has no start) exactly one is the door's; the same run answers the same twice; the offset reaches every
    /// one of the N values across runs (a clock that ignored its seed would put every run's door on the same watch).
    /// Both edges of the band are pinned: the watch before the door's and the watch after are shut, and the next
    /// period's is ajar again.
    ///
    /// <para><b>Proven RED</b> by <c>WatchesPerTurn</c> of the forwarding desk set to four (windows of five hold
    /// more than one), by <c>OffsetOf</c> answering 0 (the offsets collapse), and by <c>AjarOn</c> answering
    /// <c>watch % n == 0</c> (the seed ignored).</para>
    /// </summary>
    [Theory]
    [InlineData("forwarding", 5)]
    [InlineData("adjuster", 4)]
    public void TheDoorStandsAjarOneWatchInNSeededOnTheRunAndPure(string id, int n)
    {
        SideOffice office = Office(id);
        Assert.Equal(n, office.WatchesPerTurn);

        var offsets = new HashSet<int>();
        for (int run = 0; run < 64; run++)
        {
            ulong seed = DiceRule.Seed(0UL, $"thread-{run}");
            int offset = office.OffsetFor(seed);
            offsets.Add(offset);
            Assert.InRange(offset, 0, n - 1);
            for (long start = -12; start < 40; start++)
            {
                int ajar = 0;
                for (long w = start; w < start + n; w++)
                {
                    bool once = office.IsAjarOn(seed, w);
                    Assert.Equal(once, office.IsAjarOn(seed, w));
                    ajar += once ? 1 : 0;
                }

                Assert.Equal(1, ajar);
            }

            // The band's two edges: the watch it is on, the ones either side of it, and the next period's.
            long his = offset;
            Assert.True(office.IsAjarOn(seed, his));
            Assert.False(office.IsAjarOn(seed, his - 1));
            Assert.False(office.IsAjarOn(seed, his + 1));
            Assert.False(office.IsAjarOn(seed, his + n - 1));
            Assert.True(office.IsAjarOn(seed, his + n));
            Assert.True(office.IsAjarOn(seed, his - n));
        }

        Assert.Equal(n, offsets.Count);
    }

    /// <summary>
    /// <b>THE OFFSETS ARE PINNED TO THE RUNS, AND THE THREE DOORS KEEP THREE CLOCKS.</b> Three runs' offsets are
    /// literals (measured the day the office was built), so a moved seed tag or a moved mixing is RED rather than a
    /// silent re-roll of every captain's learned hours; and across sixty-four runs the doors do not all share one
    /// offset (the forwarding and adjuster desks, drawn on streams of their own, disagree somewhere).
    ///
    /// <para><b>Proven RED</b> by the adjuster's seed tag set to the forwarding desk's.</para>
    /// </summary>
    [Fact]
    public void TheOffsetsArePinnedAndTheDoorsKeepTheirOwnClocks()
    {
        ulong a = DiceRule.Seed(0UL, "thread-0"), b = DiceRule.Seed(0UL, "thread-1"), c = DiceRule.Seed(0UL, "thread-2");
        Assert.Equal([PinnedF0, PinnedF1, PinnedF2], new[] { a, b, c }.Select(SideOffices.Forwarding.OffsetFor));
        Assert.Equal([PinnedA0, PinnedA1, PinnedA2], new[] { a, b, c }.Select(SideOffices.Adjuster.OffsetFor));

        int disagree = 0;
        for (int run = 0; run < 64; run++)
        {
            ulong seed = DiceRule.Seed(0UL, $"thread-{run}");
            if (SideOffices.Adjuster.OffsetFor(seed) != SideOffices.Preservation.OffsetFor(seed))
            {
                disagree++;
            }
        }

        Assert.True(disagree > 8, $"the adjuster's clock and the Preservation clerk's agreed on {64 - disagree} of 64 runs.");
    }

    // MEASURED the day D and E were built, by running the assertion against placeholders and reading the actuals (the offsets
    // for runs thread-0, thread-1 and thread-2).
    private const int PinnedF0 = 0, PinnedF1 = 1, PinnedF2 = 3, PinnedA0 = 3, PinnedA1 = 2, PinnedA2 = 2;

    /// <summary>
    /// <b>EACH PAPER IS A PAPER, READ THE SAME IN EVERY DOOR.</b> Authored (never torn into pages, never reading as a
    /// clue), titled and worded as the canon wrote it by the sleeve, the glance and the card; the satchel's own item.
    ///
    /// <para><b>Proven RED</b> by dropping the side offices from <c>FieldClue.IsAuthored</c> (they read as clues),
    /// and by dropping the <c>CarriedObject.PaperReveal</c> arm (the card shows a certainty).</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheTwoNewOffices))]
    public void EachPaperIsAnAuthoredPaperReadTheSameEverywhere(string officeId)
    {
        SideOffice office = Office(officeId);
        string id = office.SheetId;
        Assert.True(office.IsTheSheet(id));
        Assert.False(office.IsTheSheet(id + "x"));
        Assert.Same(office, SideOffices.ForSheet(id));
        Assert.Equal(new Satchel.Item(Satchel.Kind.Paper, id), office.TheSheet);

        Assert.True(FieldClue.IsAuthored(id));
        Assert.False(FieldClue.ReadsAsAClue(id));
        Assert.Equal(office.SheetTitle, FieldClue.Title(id));
        Assert.Equal(office.SheetDocument, FieldClue.Document(id));

        CarriedObject.Reveal card = CarriedObject.PaperReveal(id);
        Assert.Equal(office.SheetTitle, card.Label);
        Assert.Equal(office.SheetDocument, card.Story);
        Assert.Equal("", card.ArtUrl);

        // …and no other office's paper answers for it.
        Assert.All(SideOffices.All.Where(o => o != office), o => Assert.False(o.IsTheSheet(id)));
    }

    /// <summary>
    /// <b>THE BOOK FILES THE PLATE AND THE NOTE UNDER EXACTLY TWO SUBJECTS</b> — D's under <i>Plant</i> (the office
    /// its own note prints: <i>Charged to Plant</i>) and Cinder Roost; E's under Nebula Mutual (the office the wire's
    /// own filing notice prints) and The Deep. An office and a place, in that order, and never a third.
    ///
    /// <para><b>Proven RED</b> by a third subject (the cost centre also as a place), and by the office and the place
    /// swapped.</para>
    /// </summary>
    [Fact]
    public void TheBookFilesEachUnderItsOfficeAndItsHaven()
    {
        Assert.Contains("Charged to Plant.", ForwardingDesk.SheetDocument, StringComparison.Ordinal);
        Assert.Contains("Nebula Mutual", AdjustersRoom.SheetDocument, StringComparison.Ordinal);

        (SideOffice office, string haven, string officeName)[] cases =
        [
            (SideOffices.Forwarding, "Cinder Roost", "Plant"),
            (SideOffices.Adjuster, "The Deep", NebulaLore.TermsRefiledOffice),
        ];
        Assert.Equal("Nebula Mutual", NebulaLore.TermsRefiledOffice);
        foreach ((SideOffice office, string haven, string officeName) in cases)
        {
            string line = office.Subjects(haven);
            Assert.Equal(CaseSubjects.Line(CaseSubjects.Office(officeName), CaseSubjects.Place(haven)), line);
            var note = new FieldNote("x", 0, haven, "📍", line);
            Assert.Equal(
                [CaseSubjects.Office(officeName), CaseSubjects.Place(haven)],
                CaseSubjects.On(in note).ToArray());
        }
    }

    /// <summary>
    /// <b>THE KIT'S SHAPE, SWEPT OVER ALL THREE OFFICES.</b> Three offices at three havens, one cabin each of the five,
    /// plates and ids and register tags that no two share (a shared tag would make two doors one memory), the clerk
    /// the Preservation office's alone, and the adjuster's blank form the one paper the book does not file.
    ///
    /// <para><b>Proven RED</b> by the adjuster's <c>FilesTheSheet</c> true, and by <c>HasAClerk</c> true on the
    /// forwarding desk.</para>
    /// </summary>
    [Fact]
    public void TheKitHasThreeOfficesThatShareNothing()
    {
        IReadOnlyList<SideOffice> all = SideOffices.All;
        Assert.Equal(["preservation", "forwarding", "adjuster"], all.Select(o => o.Id));
        Assert.Equal(["ringside-exchange", "cinder-roost", "the-deep"], all.Select(o => o.HavenId));
        Assert.Equal([3, 2, 4], all.Select(o => o.Cabin));
        Assert.All(all, o => Assert.InRange(o.Cabin, 1, HavenLevels.Cabins));

        string[] tags = [.. all.SelectMany(o => new[] { o.PlateReadTag, o.SheetTakenTag, o.AjarToldTag, o.SeedTag })];
        Assert.Equal(tags.Length, tags.Distinct(StringComparer.Ordinal).Count());
        string[] words = [.. all.SelectMany(o => new[] { o.DoorPlate, o.SheetId, o.SheetTitle })];
        Assert.Equal(words.Length, words.Distinct(StringComparer.Ordinal).Count());

        Assert.Equal([true, false, false], all.Select(o => o.HasAClerk));
        Assert.Equal([true, true, false], all.Select(o => o.FilesTheSheet));
        Assert.Equal([4, 5, 4], all.Select(o => o.WatchesPerTurn));

        Assert.Same(SideOffices.Forwarding, SideOffices.At("cinder-roost"));
        Assert.Null(SideOffices.At("the-space-bar"));
        Assert.Null(SideOffices.At(null));
        Assert.Null(SideOffices.ForSheet("a-paper-nobody-wrote"));
    }

    /// <summary>
    /// <b>THE DEV STARTS ARE IN THE FRONT DOOR'S LIST</b> — each new office shut and ajar, on the house's own
    /// address shape, and Ringside's two unmoved.
    ///
    /// <para><b>Proven RED</b> by the rows left out of <c>DevStarts.All</c>.</para>
    /// </summary>
    [Fact]
    public void EachNewDoorIsOneDevStartAwayShutOrAjar()
    {
        string[] urls = [.. DevStarts.All.Select(e => e.Url).Where(u => u.Contains("&office=", StringComparison.Ordinal))];
        Assert.Equal(
            [
                "/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut",
                "/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open",
                "/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=shut",
                "/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open",
                "/map?dock=the-deep&ashore=1&havenfloor=-1&office=shut",
                "/map?dock=the-deep&ashore=1&havenfloor=-1&office=open",
            ],
            urls);
    }
}
