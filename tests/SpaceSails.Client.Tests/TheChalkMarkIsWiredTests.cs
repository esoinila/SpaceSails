using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #794 slice 1 · THE CHALK MARK, THE PAGE'S HALF — the wiring Core cannot see.
///
/// <para>The judgement (which bench, which watches, what the wall says, when the move is on offer) is driven
/// in <c>TheChalkMarkTests</c> in the Core suite. What is pinned here is where the page hands those answers to
/// the player, in the shape #711's own client bench keeps (<c>ADropForNobodyYouHaveMetTests</c>): the source,
/// with the comments taken out. Every guard was watched go red against the revert its summary names (the
/// evidence is in the PR body for #794).</para>
/// </summary>
public sealed class TheChalkMarkIsWiredTests
{
    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    private static string Code(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, "//[^\n]*", " ");
    }

    private static string Chalk() => Code(Read("Pages", "Map.ChalkMark.cs"));

    /// <summary>The body of one method, cut forward to the next member at the same indentation.</summary>
    private static string Body(string code, string signature)
    {
        int at = code.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"no {signature} in Map.ChalkMark.cs");
        int next = code.IndexOf("\n    private ", at + signature.Length, StringComparison.Ordinal);
        return next < 0 ? code[at..] : code[at..next];
    }

    /// <summary>
    /// THE COLLECTION TAG IS WRITTEN ONCE PER COLLECTION — in one place, after the check that there is
    /// something under the slat, and nowhere else in the client. Core's <c>WasCollected</c> is what ends the
    /// return once the tag is in; this is the half that says the page writes it exactly there.
    /// </summary>
    [Fact]
    public void TheCollectionIsWrittenOnceAndOnlyWhenThereIsSomethingThere()
    {
        string all = string.Concat(
            Directory.EnumerateFiles(Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client"), "*.cs",
                    SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                .Select(p => Code(File.ReadAllText(p))));
        Assert.Single(Regex.Matches(all, @"\.CollectedOn\("));

        string felt = Body(Chalk(), "private bool TheSlatIsFelt(");
        int guard = felt.IndexOf("TheDropUnderThisSlat(ex, t) is not { } mark", StringComparison.Ordinal);
        int write = felt.IndexOf(".CollectedOn(", StringComparison.Ordinal);
        int give = felt.IndexOf("Satchel.Add(", StringComparison.Ordinal);
        Assert.True(guard > 0, "the move collects without asking whether anything is under the slat.");
        Assert.True(write > guard && give > guard,
            "the tag or the packet is written before the check that there is anything there.");
        Assert.Contains("ChalkMark.TheBench(t.SharedSeat, goodsUnderThisSlat: false)", felt,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// THE PRESS REACHES THE PAGE FIRST, AND THE CARD IS KEPT HONEST EVERY FRAME. The move is answered by the
    /// page (it moves the satchel and the register, which the seat does not own) and the bench's card is set
    /// from Core's <c>ChalkMark.TheBench</c> each frame — so the move is absent the moment it should be.
    /// </summary>
    [Fact]
    public void ThePressAndTheCardAreWired()
    {
        string table = Code(Read("Pages", "Map.Table.cs"));
        Assert.Contains("TheSlatIsFelt(moveId) ? Task.CompletedTask : _seating.TableMoveClicked(moveId)", table,
            StringComparison.Ordinal);

        string frame = Code(Read("Pages", "Map.Surface.Frame.cs"));
        Assert.Contains("KeepTheSlatHonest();", frame, StringComparison.Ordinal);
        int gate = frame.IndexOf("if (!TheSitBeatIsSettling)", StringComparison.Ordinal);
        int park = frame.IndexOf("CheckTheParkUnderfoot();", StringComparison.Ordinal);
        int chalk = frame.IndexOf("CheckTheChalkMark();", StringComparison.Ordinal);
        Assert.True(gate > 0 && park > gate && chalk > park,
            "the notice's poll is not beside the park's own, inside the sit-beat gate.");

        string honest = Body(Chalk(), "private void KeepTheSlatHonest(");
        Assert.Contains("ChalkMark.Offers(t.Scene) != goods", honest, StringComparison.Ordinal);
        Assert.Contains("ChalkMark.TheBench(t.SharedSeat, goods)", honest, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE PAYMENT EARNS THE RETURN, AND THE SENTENCE RIDES THE PAYMENT'S OWN PULSE — only through
    /// <c>TheReturnIsOwed</c>, which asks Core whether the dug ground keeps a park and says nothing when it
    /// does not.
    /// </summary>
    [Fact]
    public void ThePaymentEarnsTheReturn()
    {
        string drop = Code(Read("Pages", "Map.ParcelDrop.cs"));
        string paid = drop[drop.IndexOf("private void ThePaymentIsThere(", StringComparison.Ordinal)..];
        Assert.Contains("+ TheReturnIsOwed(paid)", paid, StringComparison.Ordinal);

        string owed = Body(Chalk(), "private string TheReturnIsOwed(");
        Assert.Contains("ChalkMark.TheParkUnder(paid.Where.BodyId", owed, StringComparison.Ordinal);
        Assert.Contains("return \"\";", owed, StringComparison.Ordinal);
        Assert.Contains("_roomsTurnedOver.Add(mark.Owed)", owed, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE CHALK IS DRAWN ONLY WHILE THE MARK IS UP, and nowhere does the page type a coordinate for it —
    /// the spot is Core's (<c>ChalkMark.WhereOnTheWall</c>).
    /// </summary>
    [Fact]
    public void TheChalkIsDrawnOnlyWhileTheMarkIsUp()
    {
        string wall = Body(Chalk(), "private (double X, double Y)? TheChalkOnTheWall(");
        int up = wall.IndexOf("mark.MarkIsUpAt(SimTime)", StringComparison.Ordinal);
        int where = wall.IndexOf("ChalkMark.WhereOnTheWall(in green)", StringComparison.Ordinal);
        Assert.True(up > 0 && where > up, "the chalk is placed without asking whether the mark is up.");

        string hud = Code(Read("Pages", "Map.Surface.Hud.cs"));
        Assert.Contains("Chalk: TheChalkOnTheWall()", hud, StringComparison.Ordinal);
    }

    /// <summary>
    /// #1296 · THE DEV START'S BENCH-NAMING LINE IS THE LAST PULSE THE PARK ROW WRITES. It used to be shown
    /// inside the plant and then written over, on the same tick and at the same rank, by <c>?park=1</c>'s own
    /// line — so a tester was never told which bench. Every pulse the park row writes after the plant must be
    /// the chalk line (or fall back to the park's own only when there is none), on the stand path AND on the
    /// <c>&amp;spread=1</c> path, whose bench row writes a pulse of its own.
    /// </summary>
    [Fact]
    public void TheChalkDevLineIsTheLastPulseThePARKRowWrites()
    {
        string stand = Code(Read("Pages", "Map.Surface.Cheats.Stand.cs"));
        int at = stand.IndexOf("private void StandInTheParkIfAsked(", StringComparison.Ordinal);
        Assert.True(at >= 0, "the park's dev row moved — this guard is watching a method that is gone.");
        int end = stand.IndexOf("\n    }", at, StringComparison.Ordinal);
        string row = stand[at..end];

        int plant = row.IndexOf("PlantTheChalkIfAsked(ex, in green)", StringComparison.Ordinal);
        Assert.True(plant > 0, "the park row no longer plants the chalk.");
        Assert.Contains("string? chalk = PlantTheChalkIfAsked(", row, StringComparison.Ordinal);

        foreach (Match m in Regex.Matches(row[plant..], @"ShowPulseMessage\(([^;]*)\);"))
        {
            Assert.True(m.Groups[1].Value.Contains("chalk", StringComparison.Ordinal),
                $"the park row writes a pulse after the plant that is not the chalk line, so it writes over "
                + $"it on the same tick: ShowPulseMessage({m.Groups[1].Value.Trim()})");
        }

        int sits = row.IndexOf("if (SitOnAFreeBenchIfAsked(in green))", StringComparison.Ordinal);
        int back = row.IndexOf("return;", sits, StringComparison.Ordinal);
        Assert.True(sits > plant && back > sits, "the bench branch moved.");
        Assert.Contains("ShowPulseMessage(chalk)", row[sits..back], StringComparison.Ordinal);

        // …and the plant itself no longer says anything; it hands its line back.
        string plantBody = Body(Chalk(), "private string? PlantTheChalkIfAsked(");
        Assert.DoesNotContain("ShowPulseMessage(", plantBody, StringComparison.Ordinal);
    }

    /// <summary>The mark's notes are filed under Core's glyph, never a literal typed on the page.</summary>
    [Fact]
    public void TheNotesWearCoresGlyph()
    {
        string chalk = Chalk();
        Assert.Contains("ShowAndFile(beat.Line, ChalkMark.Glyph, PulseRank.Beat)", chalk, StringComparison.Ordinal);
        Assert.Contains("FileNote(ChalkMark.CollectedEntry, ChalkMark.Glyph)", chalk, StringComparison.Ordinal);
        Assert.DoesNotContain(ChalkMark.Glyph, Read("Pages", "Map.ChalkMark.cs"), StringComparison.Ordinal);
    }
}
