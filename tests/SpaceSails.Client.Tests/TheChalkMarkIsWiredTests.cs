using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #794 · THE CHALK MARK, THE PAGE'S HALF — the wiring Core cannot see.
///
/// <para>The judgement (which table, which watches, what the stone says, when the move is on offer) is driven
/// in <c>TheChalkMarkTests</c> in the Core suite, and the live room in <see cref="TheDropIsUnderAGalleryTableTests"/>.
/// What is pinned here is where the page hands those answers to the player, in the shape #711's own client
/// bench keeps (<c>ADropForNobodyYouHaveMetTests</c>): the source, with the comments taken out. Slice 2
/// (owner's ruling 2026-09-28) re-pointed all six from the park to the gallery; every guard was watched go
/// red against the revert its summary names (the evidence is in the PR body for #794 slice 2).</para>
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
        Assert.True(at >= 0, $"no {signature} in the file");
        int next = code.IndexOf("\n    private ", at + signature.Length, StringComparison.Ordinal);
        return next < 0 ? code[at..] : code[at..next];
    }

    /// <summary>
    /// THE COLLECTION TAG IS WRITTEN ONCE PER COLLECTION — in one place, after the check that there is
    /// something under the lip, and nowhere else in the client. Core's <c>WasCollected</c> is what ends the
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

        string felt = Body(Chalk(), "private bool TheLipIsFelt(");
        int guard = felt.IndexOf("TheDropUnderThisLip(t) is not { } mark", StringComparison.Ordinal);
        int write = felt.IndexOf(".CollectedOn(", StringComparison.Ordinal);
        int give = felt.IndexOf("Satchel.Add(", StringComparison.Ordinal);
        Assert.True(guard > 0, "the move collects without asking whether anything is under the lip.");
        Assert.True(write > guard && give > guard,
            "the tag or the packet is written before the check that there is anything there.");
        Assert.Contains("ChalkMark.TheTable(t.Scene, goodsUnderThisLip: false)", felt, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE PRESS REACHES THE PAGE FIRST, AND THE CARD IS KEPT HONEST EVERY FRAME OF THE ROOM. The move is
    /// answered by the page (it moves the satchel and the register, which the seat does not own), and both
    /// polls run on the docked room's CONCOURSE path — after the level guard's early return, so nothing about
    /// the gallery is asked on the floor below — and no longer on the surface frame, where the park had them.
    /// </summary>
    [Fact]
    public void ThePressAndTheCardAreWiredToTheRoom()
    {
        string table = Code(Read("Pages", "Map.Table.cs") + Read("Pages", "Map.Table.Talk.cs"));
        Assert.Contains("TheLipIsFelt(moveId) ? Task.CompletedTask : _seating.TableMoveClicked(moveId)", table,
            StringComparison.Ordinal);

        string walkers = Body(Code(Read("Pages", "Map.BarWalkers.cs")), "private void AdvanceBarWalkers(");
        int below = walkers.IndexOf("if (!OnTheConcourse)", StringComparison.Ordinal);
        int back = walkers.IndexOf("return;", below, StringComparison.Ordinal);
        int poll = walkers.IndexOf("CheckTheChalkMark();", StringComparison.Ordinal);
        int honest = walkers.IndexOf("KeepTheLipHonest();", StringComparison.Ordinal);
        Assert.True(below > 0 && back > below, "the room's level guard moved.");
        Assert.True(poll > back && honest > back,
            "the gallery's polls are not on the concourse path of the room's frame.");

        string frame = Code(Read("Pages", "Map.Surface.Frame.cs"));
        Assert.DoesNotContain("CheckTheChalkMark", frame, StringComparison.Ordinal);
        Assert.DoesNotContain("KeepThe", frame, StringComparison.Ordinal);

        string keep = Body(Chalk(), "private void KeepTheLipHonest(");
        Assert.Contains("ChalkMark.Offers(t.Scene) != goods", keep, StringComparison.Ordinal);
        Assert.Contains("ChalkMark.TheTable(t.Scene, goods)", keep, StringComparison.Ordinal);
        Assert.Contains("AtTheGallerysTable(t)", keep, StringComparison.Ordinal);
        Assert.Contains("SittingAlone.TheTable().Id", keep, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE PAYMENT EARNS THE RETURN, AND THE SENTENCE RIDES THE PAYMENT'S OWN PULSE — only through
    /// <c>TheReturnIsOwed</c>, which keys the owed tag on the gallery's HAVEN (not on the ground that was
    /// dug) and says nothing when there is no table to leave it under.
    /// </summary>
    [Fact]
    public void ThePaymentEarnsTheReturnAtTheGallery()
    {
        string drop = Code(Read("Pages", "Map.ParcelDrop.cs"));
        string paid = drop[drop.IndexOf("private void ThePaymentIsThere(", StringComparison.Ordinal)..];
        Assert.Contains("+ TheReturnIsOwed(paid)", paid, StringComparison.Ordinal);

        string owed = Body(Chalk(), "private string TheReturnIsOwed(");
        Assert.Contains("ChalkMark.For(paid.ParcelId, ChalkMark.Haven,", owed, StringComparison.Ordinal);
        Assert.Contains("TheGallerysTables(ChalkMark.Haven)", owed, StringComparison.Ordinal);
        Assert.DoesNotContain("paid.Where", owed, StringComparison.Ordinal);
        Assert.Contains("return \"\";", owed, StringComparison.Ordinal);
        Assert.Contains("_roomsTurnedOver.Add(mark.Owed)", owed, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE CHALK IS DRAWN ONLY WHILE THE MARK IS UP, it rides the DECK's own state (a berth has no surface
    /// HUD), and nowhere does the page type a coordinate for it — the spot is Core's
    /// (<c>ChalkMark.WhereOnTheStone</c>), off the room's own machines and throat.
    /// </summary>
    [Fact]
    public void TheChalkIsDrawnOnlyWhileTheMarkIsUp()
    {
        string stone = Body(Chalk(), "private (double X, double Y)? TheChalkOnTheStone(");
        int up = stone.IndexOf("mark.MarkIsUpAt(SimTime)", StringComparison.Ordinal);
        int where = stone.IndexOf("ChalkMark.WhereOnTheStone(machines[mark.Table], throat.Y)", StringComparison.Ordinal);
        Assert.True(up > 0 && where > up, "the chalk is placed without asking whether the mark is up.");
        Assert.Contains("!OnTheConcourse", stone, StringComparison.Ordinal);

        Assert.Contains("Chalk: TheChalkOnTheStone()", Code(Read("Pages", "Map.Sim.Tick.Views.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("Chalk:", Code(Read("Pages", "Map.Surface.Hud.cs")), StringComparison.Ordinal);
        Assert.Contains("MarkTheGround(surface, state.Chalk, scale, project)",
            Code(Read("Rendering", "DeckView.Frame.cs")), StringComparison.Ordinal);
    }

    /// <summary>
    /// THE DEV START PLANTS AT THE GALLERY AND ITS LINE IS THE LAST PULSE IT WRITES — #1296's lesson carried
    /// over: it is called after <c>?ashore=1</c> has said its own line and sat anybody down, and its own
    /// tester line comes after the captain is stood in the gallery. The park's dev row carries no chalk
    /// plumbing any more.
    /// </summary>
    [Fact]
    public void TheChalkDevRowIsTheGallerysAndItsLineIsLast()
    {
        string start = Code(Read("Pages", "Map.Sim.World.Start.cs"));
        int ashore = start.IndexOf("if (q.AshoreCheat)", StringComparison.Ordinal);
        int sit = start.IndexOf("SitAtABarTopIfAsked();", ashore, StringComparison.Ordinal);
        int plant = start.IndexOf("PlantTheChalkIfAsked();", ashore, StringComparison.Ordinal);
        Assert.True(ashore > 0 && sit > ashore && plant > sit, "the chalk is not planted after the ashore row.");

        string body = Body(Chalk(), "private void PlantTheChalkIfAsked(");
        int stand = body.IndexOf("StandCaptainAt(", StringComparison.Ordinal);
        int last = body.LastIndexOf("ShowPulseMessage(", StringComparison.Ordinal);
        Assert.True(stand > 0 && last > stand, "the tester's line is not the last thing the row says.");
        Assert.Contains("mark.ThePaymentLine()", body[last..], StringComparison.Ordinal);

        string stand2 = Code(Read("Pages", "Map.Surface.Cheats.Stand.cs"));
        Assert.DoesNotContain("Chalk", stand2, StringComparison.Ordinal);
        Assert.DoesNotContain("chalk", stand2, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE PARK LOST THE DROP: the page's chalk file asks nothing of a park, a bench or an excursion floor —
    /// the table it answers about is the gallery's, by the seat's own key.
    /// </summary>
    [Fact]
    public void ThePageAsksNothingOfThePark()
    {
        string chalk = Chalk();
        foreach (string park in new[] { "ParkBenches", "Floor: < 0", "Bench", "TheGreenOnThisFloor", "_surface" })
        {
            Assert.DoesNotContain(park, chalk, StringComparison.Ordinal);
        }
        Assert.Contains("t.Key.StartsWith($\"gallery:{berth}:\"", chalk, StringComparison.Ordinal);
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
