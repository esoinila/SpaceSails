using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1074/#1063 · <b>THE FIVE PAPERS THE ARC WROTE, READ AWAY FROM THE ROOM THEY CAME OUT OF.</b>
///
/// <para>The field book rebuilds a seeded GENERIC document off a find id (#613's six forms), so a captain
/// who carried the rail's invoice out of the works floor and opened it in the sleeve was shown a torn
/// shipping manifest, and the decision card over a cost-centre line item headed it <i>"pay sheet,
/// allowances"</i>. #1074's canon pass of 2026-09-03 authored a title and a one-line body for each of the
/// five, and this is the suite that holds them.</para>
///
/// <para>One guard per paper, and each of them asks the same four things: the pair is the canon's words
/// character for character; the CERTAINTY is untouched — the same seeded roll the same id gets in a world
/// where nothing was ever closed or filled in; §8's reserved word is absent; and the control, which is the
/// half that matters, that an ordinary room of the same building on the same floor still reads as one of
/// #613's six seeded forms. A branch that swallowed every paper in the game would pass the first three.</para>
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names; the revert is
/// quoted on each one, in the shape this ground has used since #587's lesson — a guard that has never failed
/// is a guard nobody has checked.</para>
///
/// <para><b>THE WORLD THE GUARDS RUN IN IS DERIVED AND NEVER TYPED</b>, and it is deliberately a family of
/// ids no other suite asks about, exactly as <c>TheBurialTests</c>' and <c>TheMoneyTrailTests</c>' are: all
/// three registers only ever change the answer for the ids IN them, so a guard here that fills or closes a
/// ground of its own cannot move any other guard's world, whatever order xUnit runs them in. Restoring in a
/// <c>finally</c> is belt as well as braces.</para>
///
/// <para>What that discipline cannot cover is the RESTORE, which is global by nature — see
/// <see cref="StopRegisterCollection"/> — so this suite runs in that collection with the others that write
/// to the burial, stop and preservation registers.</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class ThePapersOwnHeadsTests
{
    /// <summary>How many generated rocks the sweep walks to find grounds with halls. The band is about one
    /// site in fifty; a ten-site sample tells you nothing about it. Same number and same reasoning as
    /// <c>TheBurialTests</c>' own sweep.</summary>
    private const int Probes = 4000;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>How many grounds each guard walks. Eight is enough to be a population rather than a case and
    /// small enough that five guards do not each rebuild a hundred floor plans.</summary>
    private const int Walked = 8;

    private static List<string> Grounds() => _grounds ??= Sweep();

    private static List<string>? _grounds;

    private static List<string> Sweep()
    {
        var found = new List<string>();
        for (int i = 0; i < Probes; i++)
        {
            string body = $"paper-head-ground-{i}";
            if (UndergroundComplex.HasFoundBand(body) && UndergroundComplex.TopPressurisedFloor(body) is not null)
            {
                found.Add(body);
            }
        }
        Assert.True(found.Count > 40,
            $"only {found.Count} of {Probes} generated grounds had halls and a floor that breathes — "
            + "this proves little.");
        return found;
    }

    // ══ THE CANON, RETYPED FROM THE ISSUE ════════════════════════════════════════════════════════════════

    /// <summary>#1074's canon pass of 2026-09-03, retyped here from the issue so the guards have a source the
    /// implementation cannot move. A test that asserted <c>PaperHeads.RailTitle == PaperHeads.RailTitle</c>
    /// would pass on any title anybody ever wrote into it.</summary>
    private static readonly (PaperHeads.Paper Paper, string Title, string Document)[] Authored =
    [
        (PaperHeads.Paper.MaintenanceLedger,
            "A maintenance ledger, three entries",
            "Plant's book. Every job cites an instruction; one job cites none."),
        (PaperHeads.Paper.ValveBook,
            "A valve-book, three entries",
            "Plant's book. An isolation per order, between two jobs per instruction."),
        (PaperHeads.Paper.Pour,
            "A line item: remediation",
            "Three hundred tonnes into the lower galleries, charged to Preservation."),
        (PaperHeads.Paper.Rail,
            "A line item: perimeter rail",
            "Sixteen sections, charged to Preservation."),
        (PaperHeads.Paper.Rota,
            "A line item: site watch",
            "Two hands, continuous, charged to Preservation."),
    ];

    /// <summary>#613's six seeded forms, retyped, because the control is <b>an ordinary paper still reads as
    /// an ordinary paper</b> and a control that only asserted "not one of the five" would pass on a branch
    /// that returned the empty string for every other document in the game.</summary>
    private static readonly string[] SeededTitles =
    [
        "movement order, third copy",
        "supply requisition, countersigned",
        "shipping manifest, torn",
        "maintenance log, two hands",
        "inspection schedule, margin list",
        "pay sheet, allowances",
    ];

    /// <summary>§8 — there is ONE of these and a paper's head never borrows the word. The same list
    /// <c>TheStopOrderAtTheDigTests</c>, <c>TheBurialTests</c> and <c>TheMoneyTrailTests</c> keep, because
    /// every paper this arc deals is one trigger's paperwork and a word forbidden on one is forbidden on all
    /// of them.</summary>
    private static readonly string[] Forbidden =
    [
        "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
        "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
    ];

    // ══ ONE GUARD PER PAPER ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// #1063 · THE MAINTENANCE LEDGER IS TITLED AS A MAINTENANCE LEDGER, IN THE POCKET AND ON THE CARD.
    ///
    /// <para><b>Reverts that reddened it (watched go red, then restored):</b> the
    /// <c>UndergroundComplex.AuthoredPaperOf</c> branch removed from <c>FieldClue.Title</c> —
    /// <i>"Assert.Equal() Failure: Strings differ … Expected: A maintenance ledger, three entries · Actual:
    /// inspection schedule, margin list"</i>; and the same branch removed from <c>FieldClue.Document</c> —
    /// the ledger opening as a countersigned supply requisition.</para>
    /// </summary>
    [Fact]
    public void TheMaintenanceLedgerReadsAsItselfAwayFromItsRoom() =>
        TheHeadIs(PaperHeads.Paper.MaintenanceLedger, Filled,
            body => UndergroundComplex.MaintenanceLedgerRoomFor(body)!.Value);

    /// <summary>
    /// #1074 beat 1 · …AND SO DOES THE PLANT'S VALVE-BOOK.
    ///
    /// <para><b>Revert that reddened it:</b> <c>WhatIsKeptIn</c>'s valve-book arm deleted —
    /// <i>"Assert.Equal() Failure: Strings differ … Expected: A valve-book, three entries · Actual: movement
    /// order, third copy"</i>, the one book on the closed working's own floor going back to being a carbon
    /// third copy of somebody's travel paperwork.</para>
    /// </summary>
    [Fact]
    public void TheValveBookReadsAsItselfAwayFromItsRoom() =>
        TheHeadIs(PaperHeads.Paper.ValveBook, Stopped,
            body => UndergroundComplex.ValveBookRoomFor(body)!.Value);

    /// <summary>
    /// #1074 beat 3 · THE POUR'S LINE ITEM, which is the one the marker in
    /// <c>UndergroundComplex.MoneyTrail.cs</c> was written about.
    ///
    /// <para><b>Revert that reddened it:</b> <c>WhatIsKeptIn</c>'s money-trail arm answering null —
    /// <i>"Assert.Equal() Failure: Strings differ … Expected: A line item: remediation · Actual: pay sheet,
    /// allowances"</i>, which is the marker's own sentence, reproduced.</para>
    /// </summary>
    [Fact]
    public void ThePoursLineItemReadsAsItselfAwayFromItsRoom() =>
        TheHeadIs(PaperHeads.Paper.Pour, Stopped,
            body => UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Pour)!.Value);

    /// <summary>
    /// #1074 beat 3 · …the rail's.
    ///
    /// <para><b>Revert that reddened it:</b> <c>PaperHeads.TitleOf</c>'s rail arm returning
    /// <c>RotaTitle</c> — <i>"Assert.Equal() Failure: Strings differ … Expected: A line item: perimeter rail
    /// · Actual: A line item: site watch"</i>, two purchases wearing one head, which is the exact shape of
    /// the bug a five-way switch invites.</para>
    /// </summary>
    [Fact]
    public void TheRailsLineItemReadsAsItselfAwayFromItsRoom() =>
        TheHeadIs(PaperHeads.Paper.Rail, Fenced,
            body => UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Rail)!.Value);

    /// <summary>
    /// #1074 beat 3 · …and the rota's.
    ///
    /// <para><b>Revert that reddened it:</b> <c>PaperHeads.DocumentOf</c>'s rota arm returning
    /// <c>RailDocument</c> — <i>"Assert.Equal() Failure: Strings differ … Expected: Two hands, continuous,
    /// charged to Preservation. · Actual: Sixteen sections, charged to Preservation."</i>, the watch's sheet
    /// opening as the fence's invoice.</para>
    /// </summary>
    [Fact]
    public void TheRotasLineItemReadsAsItselfAwayFromItsRoom() =>
        TheHeadIs(PaperHeads.Paper.Rota, Fenced,
            body => UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Rota)!.Value);

    // ══ AND THE TWO SWEEPS OVER THE TABLE ITSELF ═════════════════════════════════════════════════════════

    /// <summary>
    /// #1074 · THE TEN STRINGS ARE THE CANON'S WORDS, CHARACTER FOR CHARACTER, AND THERE IS NO ELEVENTH.
    ///
    /// <para>The reflection half is the one that matters over time: every public constant string the table
    /// publishes must be one of the ten <see cref="PaperHeads.AllProse"/> yields, so a helpful sentence added
    /// later cannot escape the canon grep by not being in the list. That is beat 1's own arrangement and it
    /// is kept for beat 1's own reason.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> <i>"A line item: the perimeter rail"</i> —
    /// <i>"Assert.Equal() Failure: Strings differ"</i>; and an eleventh constant added to the type —
    /// <i>"Assert.All() Failure: 1 out of 11 items in the collection did not pass"</i>, which is the sweep
    /// naming the string no canon grep could see.</para>
    /// </summary>
    [Fact]
    public void TheTenHeadsAreTheCanonsWordsAndThereIsNoEleventh()
    {
        foreach ((PaperHeads.Paper paper, string title, string document) in Authored)
        {
            Assert.Equal(title, PaperHeads.TitleOf(paper));
            Assert.Equal(document, PaperHeads.DocumentOf(paper));
        }

        Assert.Equal(
            Authored.SelectMany(a => new[] { a.Title, a.Document })
                .OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            PaperHeads.AllProse().OrderBy(s => s, StringComparer.Ordinal).ToArray());

        // …and every paper the enum names has a head, which is what makes the count above a count of the
        // whole table rather than of the part somebody remembered to list.
        Assert.Equal(Authored.Length, PaperHeads.All.Length);
        Assert.Equal(
            Enum.GetValues<PaperHeads.Paper>().OrderBy(p => p).ToArray(),
            PaperHeads.All.OrderBy(p => p).ToArray());

        var published = new List<string>();
        foreach (System.Reflection.FieldInfo f in
            typeof(PaperHeads).GetFields(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            if (f.FieldType == typeof(string) && f.GetValue(null) is string value)
            {
                published.Add(value);
            }
        }
        Assert.All(published, s =>
            Assert.True(PaperHeads.AllProse().Contains(s, StringComparer.Ordinal),
                $"PaperHeads publishes a string no canon grep can see: \"{s}\""));
    }

    /// <summary>
    /// #1074 · AN ID THIS BUILDING DID NOT MINT IS NOT ONE OF THE FIVE.
    ///
    /// <para>The branch is asked of a bare string that travels in a save, so the answer has to be right about
    /// strings as well as about rooms. A record out of the halls carries the same body, level and room index
    /// under a different prefix (<c>hall:</c>), and without the round trip it would be titled with a plant
    /// book — a section of wall opening as a valve-book, which is the third named bug class again.</para>
    ///
    /// <para><b>Reverts that reddened it:</b> the re-minted-id comparison in <c>AuthoredPaperOf</c> dropped
    /// — <i>"Assert.Null() Failure: Value of type 'Nullable&lt;Paper&gt;' has a value"</i> on the hall
    /// record; and <c>RoomOfFind</c> splitting from the LEFT — the same failure on a body id with a colon in
    /// it, which is the shape a save has never promised not to contain.</para>
    /// </summary>
    [Fact]
    public void OnlyAnIdThisBuildingWouldMintIsOneOfTheFive()
    {
        Assert.Null(UndergroundComplex.AuthoredPaperOf(null));
        Assert.Null(UndergroundComplex.AuthoredPaperOf(""));
        Assert.Null(UndergroundComplex.AuthoredPaperOf("kolt-premium-schedule"));
        Assert.Null(UndergroundComplex.AuthoredPaperOf("hive:probe-moon-3"));
        Assert.Null(UndergroundComplex.AuthoredPaperOf("hive:probe-moon-3:two:0"));

        foreach (string body in Grounds().Take(Walked))
        {
            using (Stopped(body))
            {
                (int level, int room) = UndergroundComplex.ValveBookRoomFor(body)!.Value;
                Assert.Equal(PaperHeads.Paper.ValveBook,
                    UndergroundComplex.AuthoredPaperOf(UndergroundComplex.FindId(body, level, room)));

                // The same room, wearing the prefix a gallery nobody dug hands out.
                Assert.Null(UndergroundComplex.AuthoredPaperOf(
                    $"{UndergroundComplex.HallFindPrefix}:{body}:{level}:{room}"));
            }

            // …and with the register empty, the very id that was the valve-book is nobody's paper.
            (int quietLevel, int quietRoom) = (UndergroundComplex.DepthOf(body), 1);
            Assert.Null(UndergroundComplex.AuthoredPaperOf(
                UndergroundComplex.FindId(body, quietLevel, quietRoom)));
        }
    }

    /// <summary>
    /// #1074/#603 · THE CERTAINTY BEHIND ALL FIVE IS STILL A ROLL, MEASURED OVER A POPULATION THAT CAN TELL
    /// PASS FROM FAIL.
    ///
    /// <para>Each per-paper guard already asserts the certainty is the same in both worlds for its eight
    /// grounds — but eight ids need not turn up all three faces, and a guard that only ever saw one would be
    /// blind to a branch that pinned the whole ladder. This walks the WHOLE swept population, all five
    /// papers, and demands the ladder come up in all three of its rungs: an answer wired to a constant could
    /// not do that, and neither could a branch that gave an authored paper a certainty of its own.</para>
    ///
    /// <para><b>Reverts that reddened it (watched go red, then restored):</b> <c>FieldClue.CertaintyOf</c>
    /// given an authored-paper arm returning <c>Certainty.Exact</c> — <i>"Assert.Equal() Failure: Values
    /// differ · Expected: Vague · Actual: Exact"</i> on the very first ground, the same id answering two
    /// different ways in two worlds; and the same arm without the world gate, so every paper in the game
    /// rolled one answer — <i>"Assert.Equal() Failure · Expected: 3 · Actual: 1"</i> on the faces the
    /// population came back with.</para>
    /// </summary>
    [Fact]
    [Trait("speed", "slow")]
    public void TheCertaintyBehindAllFiveIsStillTheOrdinarySeededRoll()
    {
        var faces = new HashSet<FieldClue.Certainty>();

        foreach (string body in Grounds())
        {
            foreach ((PaperHeads.Paper paper, Func<string, IDisposable> world,
                Func<string, (int Level, int RoomIndex)> roomOf) in TheFive)
            {
                int level, room;
                using (world(body))
                {
                    (level, room) = roomOf(body);
                }
                string findId = UndergroundComplex.FindId(body, level, room);
                FieldClue.Certainty quiet = FieldClue.CertaintyOf(findId);

                using (world(body))
                {
                    Assert.Equal(paper, UndergroundComplex.AuthoredPaperOf(findId));
                    Assert.Equal(quiet, FieldClue.CertaintyOf(findId));
                    Assert.Equal(
                        FieldClue.SpreadFor(quiet), FieldClue.SpreadFor(FieldClue.CertaintyOf(findId)));
                }

                faces.Add(quiet);
            }
        }

        Assert.Equal(3, faces.Count);
    }

    // ── the shape every per-paper guard runs ─────────────────────────────────────────────────────────────

    /// <summary>The five, each with the world it exists in and the room it is kept in. One table so a sweep
    /// over all of them cannot fall out of step with the five guards above.</summary>
    private static readonly (PaperHeads.Paper Paper, Func<string, IDisposable> World,
        Func<string, (int Level, int RoomIndex)> RoomOf)[] TheFive =
    [
        (PaperHeads.Paper.MaintenanceLedger, Filled,
            body => UndergroundComplex.MaintenanceLedgerRoomFor(body)!.Value),
        (PaperHeads.Paper.ValveBook, Stopped,
            body => UndergroundComplex.ValveBookRoomFor(body)!.Value),
        (PaperHeads.Paper.Pour, Stopped,
            body => UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Pour)!.Value),
        (PaperHeads.Paper.Rail, Fenced,
            body => UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Rail)!.Value),
        (PaperHeads.Paper.Rota, Fenced,
            body => UndergroundComplex.MoneyTrailRoomFor(body, MoneyTrail.Item.Rota)!.Value),
    ];

    /// <summary>What one paper's guard actually asks. Four questions, and the fourth is the one that stops
    /// this from being a branch that swallowed the building.</summary>
    private static void TheHeadIs(
        PaperHeads.Paper paper,
        Func<string, IDisposable> world,
        Func<string, (int Level, int RoomIndex)> roomOf)
    {
        (_, string title, string document) = Authored.Single(a => a.Paper == paper);

        // 3 · §8's reserved word, and everything else that would settle which reading of §10 is true, is
        //     absent from both halves of the head.
        foreach (string word in Forbidden)
        {
            Assert.DoesNotContain(word, title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(word, document, StringComparison.OrdinalIgnoreCase);
        }

        int walked = 0;

        foreach (string body in Grounds().Take(Walked))
        {
            int level, room;
            using (world(body))
            {
                (level, room) = roomOf(body);
            }
            string findId = UndergroundComplex.FindId(body, level, room);

            // THE SAME ID, IN A WORLD WHERE NOTHING WAS EVER CLOSED OR FILLED IN. This is the control's
            // first half and the certainty law's baseline in one: the register is empty, so the room holds
            // no authored paper and the id gets whatever #613 has always given it.
            Assert.Null(UndergroundComplex.AuthoredPaperOf(findId));
            Assert.Contains(FieldClue.Title(findId), SeededTitles);
            FieldClue.Certainty quietCertainty = FieldClue.CertaintyOf(findId);

            using (world(body))
            {
                // 1 · THE AUTHORED PAIR, VERBATIM, through the two functions every seam that names a paper
                //     away from its room goes through.
                Assert.Equal(paper, UndergroundComplex.AuthoredPaperOf(findId));
                Assert.Equal(title, FieldClue.Title(findId));
                Assert.Equal(document, FieldClue.Document(findId));

                // 2 · THE CERTAINTY IS NOT BRANCHED. Same id, both worlds, same roll — so the tracker's
                //     spread, the row's short word and FieldClue.Line are exactly what they always were.
                Assert.Equal(quietCertainty, FieldClue.CertaintyOf(findId));

                // 4 · AND THE BRANCH DID NOT SWALLOW THE BUILDING. An ordinary room of the same floor of the
                //     same ground, in the same closed-or-filled world, still reads as one of #613's six
                //     seeded forms — title and body both, the body checked by the seeded tail the generic
                //     composition appends and an authored head never carries.
                string ordinary = UndergroundComplex.FindId(body, level, OrdinaryRoomOn(body, level, room));
                Assert.Null(UndergroundComplex.AuthoredPaperOf(ordinary));
                Assert.Contains(FieldClue.Title(ordinary), SeededTitles);
                Assert.DoesNotContain(document, FieldClue.Document(ordinary), StringComparison.Ordinal);
                Assert.EndsWith(
                    Tail(FieldClue.CertaintyOf(ordinary)), FieldClue.Document(ordinary),
                    StringComparison.Ordinal);
            }

            walked++;
        }

        Assert.Equal(Walked, walked);
    }

    /// <summary>A room on the same floor that no designation has taken, which is what almost every room on
    /// every floor is. Derived rather than typed: a hard-coded index would be a control that quietly landed
    /// on a designated room the day somebody moved one.</summary>
    private static int OrdinaryRoomOn(string bodyId, int level, int taken)
    {
        int rooms = UndergroundComplex.Build(bodyId, level, Field).RoomCentres.Count;
        for (int room = rooms - 1; room >= 0; room--)
        {
            if (room != taken && UndergroundComplex.AuthoredPaperOf(
                    UndergroundComplex.FindId(bodyId, level, room)) is null)
            {
                return room;
            }
        }

        Assert.Fail($"every room of floor {level} on {bodyId} holds an authored paper — "
            + "the control has no world left to run in.");
        return -1;
    }

    /// <summary>#603's seeded tail — the sentence a GENERIC document always ends with and an authored head
    /// never carries. Retyped from <c>FieldClue.Document</c>'s canon so the control has a source the
    /// implementation cannot move.</summary>
    private static string Tail(FieldClue.Certainty certainty) => certainty switch
    {
        FieldClue.Certainty.Vague =>
            "There is a place named on it. Only named — the way you name somewhere you have never "
            + "had to find.",
        FieldClue.Certainty.Narrow =>
            "And there is a description: a landform, a bearing taken off it, a distance somebody "
            + "paced rather than measured. Enough to walk to in an afternoon.",
        _ =>
            "And there is a POSITION, written to the figure. Somebody wrote it down like that "
            + "because somebody else was going to have to drive there in the dark.",
    };

    // ── the worlds ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Fill this ground in for the length of one block, and put the world back afterwards whatever
    /// happens. #1063's own state, and the only one the maintenance ledger exists in.</summary>
    private static IDisposable Filled(string body)
    {
        Burial.Install([body]);
        return new Restore();
    }

    /// <summary>Close the working instead — beat 1's state, and the one the valve-book and the pour's line
    /// item exist in.</summary>
    private static IDisposable Stopped(string body)
    {
        StopOrder.Install([body]);
        return new Restore();
    }

    /// <summary>…and take it into official care as well, which is the only state the rail and the rota are
    /// ever bought in.</summary>
    private static IDisposable Fenced(string body)
    {
        StopOrder.Install([body]);
        PreservationZone.Install([body]);
        return new Restore();
    }

    private sealed class Restore : IDisposable
    {
        public void Dispose()
        {
            Burial.Install([]);
            StopOrder.Install([]);
            PreservationZone.Install([]);
        }
    }
}
