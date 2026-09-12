using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #798 item 2 · THE SPLIT, DRIVEN AT A REAL TABLE — and the one control it hangs off.
///
/// <para>Owner (2026-08-09): <i>"rip out the most compromising evidence and toss the rest inconspicuously…
/// pocket the one damning sheet (small, hideable, the photograph already in the book) and bin the innocent
/// bulk, which is ALSO cover. The processing UI (#784) is where the split happens — seated, with air."</i></para>
///
/// <h3>Why these are driven and not read</h3>
/// <para>A shape guard could assert that <c>SpreadTable.razor</c> types the word <c>SplitIsOffered</c> today
/// and go green for ever on the day the predicate stops asking the seat — this repo's fifth named bug class
/// is a guard that cannot tell a pass from a fail. So a captain is sat down at a real top in a real bar
/// through the game's own <c>[E]</c> verb, the row is pressed, and what is asserted is the SLEEVE, the BOOK
/// and the REGISTER on the other side of it.</para>
///
/// <para>The one guard here that IS about text is the markup, and it has to be: whether a control is drawn
/// behind its own gate is a fact about a razor file and there is no renderer in this suite. It reads the
/// COMPOSED page (<see cref="MapMarkup"/>), so it sees the satchel exactly as <c>Map.razor</c> hosts it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheSplitIsASeatedVerbTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;

    /// <summary>The classy great-port tier, and the berth <c>TheCaseIsNotTiedToAPlaceTests</c> sits its own
    /// captain down in — the same room, because the seat is the subject here too.</summary>
    private const string TheRedEye = "red-eye";

    private const string ThreadId = "b71f4a0c39d24e5ba8027c6f1d3e5490";

    /// <summary>A document long enough to have a page worth tearing out, FOUND rather than typed: the page
    /// count is rolled off the id, so a hard-coded "this one is four pages" would be a number that goes
    /// stale the day the weighting moves — the fifth bug class in a fixture.</summary>
    private static string AMultiPagePaper() =>
        Enumerable.Range(0, 200).Select(i => $"hive:doc:split-{i}")
            .First(PageGranularity.IsMultiPage);

    /// <summary>…and one with nothing in it to split, found the same way.</summary>
    private static string AOneSheetPaper() =>
        Enumerable.Range(0, 200).Select(i => $"hive:doc:split-{i}")
            .First(id => PageGranularity.PagesIn(id) == 1);

    // ── 0 · THE BENCH CAN TELL PASS FROM FAIL ────────────────────────────────────────────────────────

    /// <summary>The world these guards are stated against is the one the owner described: a captain in a
    /// seat the case may be spread at, holding a document of more than one page that the book already has.
    /// Asserted first, because every law below is vacuous in a world missing any of the three.</summary>
    [Fact]
    public void THE_BENCH_IsASeatedCaptainWithADugMultiPageDocument()
    {
        (Pages.Map map, Satchel.Item paper) = SeatedWithADugDocument();

        Assert.NotNull(Read(map, "SeatedIn"));
        Assert.Null(Read(map, "SpreadRefusal"));
        Assert.True(PageGranularity.PagesIn(paper.Id) >= 2,
            $"{paper.Id} is one sheet — this bench cannot tell a working split from a dead one.");
        Assert.True((bool)Invoke(map, "AlreadyWrittenUp", paper)!,
            "the bench never got the document into the book, and the split's own precondition is missing.");
    }

    // ── (a) THE CONTROL IS DRAWN ONLY WHERE IT APPLIES ───────────────────────────────────────────────

    /// <summary>
    /// THE SPLIT IS OFFERED FOR A WORKED, MULTI-PAGE, WHOLE DOCUMENT — AND NOWHERE ELSE.
    ///
    /// <para>Driven over the whole matrix a captain can actually be in: both page counts, both sides of the
    /// book, both halves of an already-split file and every kind the satchel has. #212/#603's rule is that a
    /// verb which does not apply is not drawn at all rather than greyed out, so this is the one question the
    /// row asks and the answer has to be right in every cell.</para>
    ///
    /// <para><b>Proven RED</b> by script-replacing <c>SplitIsOffered</c>'s body with
    /// <c>RipAndBin.IsEvidence(item.Kind)</c> — the shredder's own gate, which is the gate somebody would
    /// reach for if they were adding this control in a hurry:</para>
    /// <code>
    /// hive:doc:split-0 (1 page, in the book): the split is offered on a document with nothing to split.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_CONTROL_IsOfferedOnlyForAWorkedMultiPageDocument()
    {
        (Pages.Map map, _) = SeatedWithADugDocument();
        string many = AMultiPagePaper();
        string one = AOneSheetPaper();
        int offered = 0, withheld = 0;

        foreach (string id in new[] { many, one })
        {
            foreach (bool dug in new[] { true, false })
            {
                foreach (string useId in new[] { id, PageGranularity.SheetIdOf(id), PageGranularity.BulkIdOf(id) })
                {
                    foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
                    {
                        var item = new Satchel.Item(kind, useId);
                        Set(map, "_workedUp", new HashSet<string>(StringComparer.Ordinal));
                        if (dug)
                        {
                            ((HashSet<string>)Field(map, "_workedUp")!).Add($"{kind}:{id}");
                        }

                        bool want = kind == Satchel.Kind.Paper
                            && dug
                            && PageGranularity.PagesIn(id) >= 2
                            && PageGranularity.PartOf(useId) == PageGranularity.Part.Whole;
                        bool got = (bool)Invoke(map, "SplitIsOffered", item)!;

                        if (want)
                        {
                            offered++;
                        }
                        else
                        {
                            withheld++;
                        }

                        Assert.True(want == got,
                            $"{useId} ({PageGranularity.PagesIn(id)} page(s), {kind}, "
                            + $"{(dug ? "in the book" : "not yet dug")}): the split control is "
                            + $"{(got ? "offered" : "withheld")} and should be "
                            + $"{(want ? "offered" : "withheld")}.");
                    }
                }
            }
        }

        Assert.True(offered > 0 && withheld > 0,
            $"the matrix produced {offered} offered and {withheld} withheld cell(s) — a guard whose world "
            + "answers one way everywhere proves nothing.");
    }

    /// <summary>
    /// STAND THE CAPTAIN UP AND THE CONTROL GOES — AND A PRESS REFUSES OUT LOUD.
    ///
    /// <para>The owner's clause: <i>"seated, with air."</i> Both halves matter and they are different laws:
    /// the control follows the posture (#212), and a press that gets through anyway — the chair opposite
    /// filling while the papers are out is #784's own drama beat — says the seat family's own sentence
    /// rather than doing nothing, which is #603's rule and the only way this law is ever learned.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting the <c>SpreadRefusal</c> clause from
    /// <c>SplitIsOffered</c> (<c>seatedForTheSpread: true</c>):</para>
    /// <code>
    /// on their feet, the split is still on offer.
    /// </code>
    /// <para>…and, for the second half, by script-deleting the <c>SpreadRefusal</c> gate from the top of
    /// <c>SplitTheDocument</c>: the document comes apart standing up and nothing is said at all.</para>
    /// </summary>
    [Fact]
    public void ON_YOUR_FEET_TheControlGoesAndThePressRefusesOutLoud()
    {
        (Pages.Map map, Satchel.Item paper) = SeatedWithADugDocument();
        Assert.True((bool)Invoke(map, "SplitIsOffered", paper)!);

        Invoke(map, "StandUpFromTable");
        Assert.NotNull(Read(map, "SpreadRefusal"));
        Assert.False((bool)Invoke(map, "SplitIsOffered", paper)!,
            "on their feet, the split is still on offer.");

        IReadOnlyList<string> before = TheSleeve(map);
        Invoke(map, "SplitTheDocument", paper);

        Assert.Equal(before, TheSleeve(map));
        Assert.Equal(Read(map, "SpreadRefusal"), Field(map, "_satchelOutcome"));
    }

    // ── (b) THE ACT ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PRESS: ONE DOCUMENT BECOMES A SHEET AND A FOLDER, AND THE BOOK DOES NOT MOVE.
    ///
    /// <para>What the sleeve holds afterwards is the whole of the feature — the damning page and the boring
    /// bulk, both still the document they came out of — and what the BOOK holds afterwards is the whole of
    /// the discipline: not one entry more than it had, because splitting a file is tradecraft and not a
    /// finding. The register is checked in the same breath, because the bin picker reads it: both halves
    /// have to still be <i>in the book</i>, or the split would quietly undo its own precondition and start
    /// warning a captain that binning the folder loses what it had to say.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting the <c>PageGranularity.SourceOf</c> call from
    /// <c>WrittenUpKey</c>, which is the one line holding the register's half of this up:</para>
    /// <code>
    /// the torn-out sheet is not in the book any more — the split undid the dig that was its own
    /// precondition.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_PRESS_TurnsOneDocumentIntoASheetAndAFolder()
    {
        (Pages.Map map, Satchel.Item paper) = SeatedWithADugDocument();
        IReadOnlyList<string> bookBefore = TheBook(map);

        Invoke(map, "SplitTheDocument", paper);

        var sheet = new Satchel.Item(Satchel.Kind.Paper, PageGranularity.SheetIdOf(paper.Id));
        var bulk = new Satchel.Item(Satchel.Kind.Paper, PageGranularity.BulkIdOf(paper.Id));
        IReadOnlyList<Satchel.Item> sleeve = Sleeve(map);

        Assert.DoesNotContain(sleeve, i => i.Id == paper.Id);
        Assert.Contains(sleeve, i => i.Id == sheet.Id);
        Assert.Contains(sleeve, i => i.Id == bulk.Id);
        Assert.True(sleeve.Count(i => PageGranularity.SourceOf(i.Id) == paper.Id) == 2,
            "the split did not leave exactly two things in the sleeve.");

        // THE BOOK NEVER MOVES. Not one entry more: the dig filed what this document said, and the captain
        // taking it apart at a table is not a finding.
        Assert.Equal(bookBefore, TheBook(map));

        // …and both halves are still in the book, which is what the bin picker reads off the register.
        Assert.True((bool)Invoke(map, "AlreadyWrittenUp", sheet)!,
            "the torn-out sheet is not in the book any more — the split undid the dig that was its own "
            + "precondition.");
        Assert.True((bool)Invoke(map, "AlreadyWrittenUp", bulk)!,
            "the folder is not in the book any more — the picker will warn that binning it loses what it "
            + "had to say, about pages that are already written down.");
        Assert.Equal(RipAndBin.AlreadyInTheBookFlag, Ask<string>(map, "BinRowFlag", bulk));

        // It is SAID, once, in the captain's own register — into the DIALOG, because the spread stays open.
        Assert.Equal(PageGranularity.SplitLine, Field(map, "_satchelOutcome"));

        // …and the rows can be told apart on the page, which is the point of a citation.
        string sheetRow = Ask<string>(map, "SatchelLabel", sheet);
        string bulkRow = Ask<string>(map, "SatchelLabel", bulk);
        Assert.True(sheetRow != bulkRow,
            $"the sheet and the folder are the same row: \"{sheetRow}\".");
        Assert.Contains(FieldClue.Title(paper.Id), sheetRow, StringComparison.Ordinal);
        Assert.Contains(FieldClue.Title(paper.Id), bulkRow, StringComparison.Ordinal);
    }

    /// <summary>
    /// A DOCUMENT COMES APART ONCE, DRIVEN — press it again and nothing happens to anything.
    ///
    /// <para>The law is Core's and needs no state to hold, which is exactly the claim worth driving: after
    /// the split there is no whole document left in the sleeve for the control to be offered on, and the two
    /// halves refuse. The second press is made anyway, because a law that is only enforced by a control not
    /// being drawn is a law one stale render away from being broken.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting <c>&amp;&amp; PartOf(paperId) == Part.Whole</c> from
    /// <c>PageGranularity.CanSplit</c>:</para>
    /// <code>
    /// pressing the split on the torn-out sheet changed the sleeve — a document has come apart twice.
    /// </code>
    /// </summary>
    [Fact]
    public void A_SECOND_PRESS_DoesNothingToAnything()
    {
        (Pages.Map map, Satchel.Item paper) = SeatedWithADugDocument();
        Invoke(map, "SplitTheDocument", paper);

        IReadOnlyList<string> sleeve = TheSleeve(map);
        IReadOnlyList<string> book = TheBook(map);

        foreach (Satchel.Item half in Sleeve(map).ToList())
        {
            Assert.False((bool)Invoke(map, "SplitIsOffered", half)!,
                $"{half.Id}: half a document is still being offered the scissors.");
            Invoke(map, "SplitTheDocument", half);
            Assert.Equal(sleeve, TheSleeve(map));
            Assert.Equal(book, TheBook(map));
        }
    }

    // ── (c) THE FOLDER IN THE BIN READS AS NOTHING ───────────────────────────────────────────────────

    /// <summary>
    /// …AND WHEN THE FOLDER GOES IN A BIN, THE BOOK SAYS WHAT WAS LEFT THERE.
    ///
    /// <para>The other end of the owner's sentence: <i>"a file in the bin that reads boring explains itself;
    /// a missing file explains nothing."</i> The split is worth nothing unless the disposal it feeds reads
    /// differently, so the bulk is carried to a real bin on a real floor, binned through the shipping verb,
    /// and the FILED NOTE — the only durable record a later arc can read back — is compared against the note
    /// the same bucket files for a whole document.</para>
    ///
    /// <para><b>Proven RED</b> by script-deleting the <c>boring:</c> argument from <c>TheSheetIsGone</c>'s
    /// <c>DisposalNote</c> call:</para>
    /// <code>
    /// the folder went in the paper bin and the book filed it as a file somebody got rid of.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_FOLDER_InTheBinIsFiledAsAFileAboutNothingMuch()
    {
        (string body, int level, RipAndBin.Bin bin) = ABinSomewhere();
        string document = AMultiPagePaper();

        // The folder, binned.
        var bulk = new Satchel.Item(Satchel.Kind.Paper, PageGranularity.BulkIdOf(document));
        Pages.Map folder = AtTheBin(body, level, bin, bulk);
        Invoke(folder, "RipItUp", bulk);

        Assert.Empty(Sleeve(folder));
        Assert.Contains(TheBook(folder),
            line => line.Contains(PageGranularity.BulkDisposalClause, StringComparison.Ordinal));

        // …and the same bucket, handed the whole document, files the other clause. Both halves, because a
        // note that read "nothing much" for everything would pass the first assertion and mean nothing.
        var whole = new Satchel.Item(Satchel.Kind.Paper, document);
        Pages.Map entire = AtTheBin(body, level, bin, whole);
        Invoke(entire, "RipItUp", whole);

        Assert.DoesNotContain(TheBook(entire),
            line => line.Contains(PageGranularity.BulkDisposalClause, StringComparison.Ordinal));
        Assert.Contains(TheBook(entire),
            line => line.Contains("Nothing of it was left on the table", StringComparison.Ordinal));
    }

    // ── (d) THE CONTROL ON THE PAGE ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CONTROL IS DRAWN ON THE SPREAD, BEHIND ITS OWN GATE, AND NOWHERE ELSE.
    ///
    /// <para>Whether a button is drawn behind an <c>@if</c> is a fact about a razor file and this suite has
    /// no renderer, so this one guard is about text — read off the COMPOSED page, so it sees the satchel as
    /// <c>Map.razor</c> actually hosts it. Three claims: the press exists, it is inside
    /// <c>@if (SplitIsOffered(dig))</c>, and the spread's row is the ONLY place in the client that draws it
    /// — the standing satchel row and the bin's own picker are different pages with different laws, and a
    /// split offered on your feet is the owner's clause broken in the one place a driven guard cannot
    /// look.</para>
    /// </summary>
    [Fact]
    public void THE_CONTROL_LivesOnTheSpreadRowBehindItsOwnGate()
    {
        string page = MapMarkup.Text;

        Assert.Contains("SplitTheDocument(dig)", page, StringComparison.Ordinal);
        Assert.Contains("@if (SplitIsOffered(dig))", page, StringComparison.Ordinal);

        // The gate is the LAST thing typed before the press — no other control, no other condition, comes
        // between them. A block that drifted apart would draw the button under somebody else's `if`.
        int gate = page.IndexOf("@if (SplitIsOffered(dig))", StringComparison.Ordinal);
        int press = page.IndexOf("SplitTheDocument(dig)", StringComparison.Ordinal);
        Assert.True(gate >= 0 && press > gate,
            "the split's press is not drawn after its own gate.");
        Assert.DoesNotContain("@if (", page[gate..press].AsSpan()[25..], StringComparison.Ordinal);

        // …and the hint is the act's own sentence, so the price of a press is known before the press.
        Assert.Contains("title=\"@SplitHint(dig)\"", page, StringComparison.Ordinal);

        // One press in the whole client, and it is that one.
        Assert.Equal(1, CountAcrossTheClient("SplitTheDocument("));
    }

    /// <summary>How many times a string is typed across every razor file the client ships — the
    /// only-one-of-these claim above, measured rather than assumed.</summary>
    private static int CountAcrossTheClient(string needle)
    {
        int n = 0;
        foreach (string file in Directory.EnumerateFiles(
            Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client"), "*.razor",
            SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            for (int at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0;
                 at = text.IndexOf(needle, at + 1, StringComparison.Ordinal))
            {
                n++;
            }
        }
        return n;
    }

    // ── THE BENCHES ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A captain at a takeable top in The Stormwatch Bar, holding one multi-page document the book already
    /// has.
    ///
    /// <para>Built out of the shipping verbs at every step, the way <c>TheCaseIsNotTiedToAPlaceTests</c>
    /// builds the same seat: the deck is <c>SetDeckForDock</c>'s, the walk is
    /// <c>StandAtTheBarThreshold</c>'s, the sitting is <c>TryTakeBarTop</c> — the very handler <c>[E]</c>
    /// reaches — and the DIG is <c>TheWriteUpLands</c>, the far end of the game's own hold. Nothing here
    /// writes the register by hand, so the precondition these guards rest on is the one a player
    /// produces.</para>
    /// </summary>
    private static (Pages.Map Map, Satchel.Item Paper) SeatedWithADugDocument()
    {
        Pages.Map map = SatAtATopInTheBar();
        var paper = new Satchel.Item(Satchel.Kind.Paper, AMultiPagePaper());
        Set(map, "_satchel", new List<Satchel.Item> { paper });
        Set(map, "_showSatchel", true);

        Invoke(map, "TheWriteUpLands", paper, Invoke(map, "WhereYouAreStanding"));
        Assert.True((bool)Invoke(map, "AlreadyWrittenUp", paper)!,
            "the bench's dig did not land — the register is empty and every guard here would be vacuous.");
        return (map, paper);
    }

    private static Pages.Map SatAtATopInTheBar()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_ephemeris", CircularOrbitEphemeris.FromScenario(TestTree.Sol));
        Set(map, "_dockedHavenId", TheRedEye);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Invoke(map, "SetDeckForDock", TheRedEye);
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!,
            $"{TheRedEye} has no walkable bar to stand in — this bench has no room for its subject.");

        HavenInterior.BarFloor bar = HavenInterior.BarBand(TheRedEye)!.Value;
        foreach (DeckReachability.Point top in bar.Tops)
        {
            Set(map, "_avatarX", top.X);
            Set(map, "_avatarY", top.Y);
            if ((bool)Invoke(map, "TryTakeBarTop")! && Read(map, "SeatedIn") is not null)
            {
                return map;
            }
        }

        throw new InvalidOperationException(
            "no top in The Stormwatch Bar took the press — this whole file is arguing about a chair that "
            + "does not exist.");
    }

    /// <summary>A captain standing at one published bin on a real floor, holding one thing. The bins are the
    /// floor's own and the deck is the one the game builds for it — the bench
    /// <c>TheDisposalYouWatchTests</c> stands its own captain at, because the act being driven is the same
    /// act.</summary>
    private static Pages.Map AtTheBin(string body, int level, RipAndBin.Bin bin, Satchel.Item paper)
    {
        var map = new Pages.Map();

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        SetProp(ex, "Stop", stop);
        SetProp(ex, "RestoreHavenId", null);
        SetProp(ex, "Site", new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        SetProp(ex, "Floor", level);

        ((HashSet<string>)Field(map, "_workedUp")!)
            .Add($"{paper.Kind}:{PageGranularity.SourceOf(paper.Id)}");

        Set(map, "_surface", ex);
        Set(map, "_satchel", new List<Satchel.Item> { paper });
        Set(map, "_avatarX", bin.StandX);
        Set(map, "_avatarY", bin.StandY);
        Invoke(map, "RebuildSurfaceDeck");
        Set(map, "_showSatchel", true);
        return map;
    }

    /// <summary>The first bucket this build carves that is a BET rather than the watched machine — walked
    /// out of the generator rather than typed, so nothing here goes stale against a re-carve.</summary>
    private static (string Body, int Level, RipAndBin.Bin Bin) ABinSomewhere()
    {
        foreach (string body in new[] { "luna", "miranda", "europa" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                foreach (RipAndBin.Bin bin in
                    UndergroundComplex.Build(body, level, MoonSurface.ExpeditionField()).TheBins)
                {
                    if (RipAndBin.LeavesSomethingToFind(bin.Tier))
                    {
                        return (body, level, bin);
                    }
                }
            }
        }

        throw new InvalidOperationException(
            "this build carves no bin that leaves anything to find — the whole ladder is gone.");
    }

    // ── Reading the page ─────────────────────────────────────────────────────────────────────────────

    private static IReadOnlyList<Satchel.Item> Sleeve(Pages.Map map) =>
        (List<Satchel.Item>)Field(map, "_satchel")!;

    private static IReadOnlyList<string> TheSleeve(Pages.Map map) => [.. Sleeve(map).Select(i => i.Stored)];

    private static IReadOnlyList<string> TheBook(Pages.Map map) =>
        [.. ((List<FieldNote>)Field(map, "_fieldNotes")!).Select(n => n.ToString())];

    // ── Reflection plumbing ──────────────────────────────────────────────────────────────────────────

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name.");

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

    private static void Set(Pages.Map map, string name, object? value) => FieldOf(name).SetValue(map, value);

    private static object? Read(Pages.Map map, string property) =>
        (typeof(Pages.Map).GetProperty(property, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{property}` — this guard is reading a dead name."))
            .GetValue(map);

    private static T Ask<T>(Pages.Map map, string method, Satchel.Item item) =>
        (T)Invoke(map, method, item)!;

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo call = typeof(Pages.Map).GetMethod(method, Hidden)
            ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name.");
        try
        {
            return call.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static void SetProp(object o, string property, object? value) =>
        o.GetType().GetProperty(property)!.SetValue(o, value);
}
