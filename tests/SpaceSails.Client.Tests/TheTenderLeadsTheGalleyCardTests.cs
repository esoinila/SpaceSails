using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1022 · <b>THE CARD HAS SOMEBODY BEHIND IT.</b> Owner, live (2026-08-30): <i>"The dialog, one imagines,
/// is a set of phrases to keep the customer talking :-D ... but there is something heart warming in those
/// scenes at the same time."</i>
///
/// <para>Core owns every word and every choice (<c>TheTenderIsASetOfPhrasesTests</c> holds those laws). What
/// is left for this file is the half Core cannot see: whether any of it REACHES THE GLASS. A pool that is
/// never drawn, a button whose press does not move the speaker, a beat whose second half is dropped by the
/// markup — none of those are visible to a Core test, and all of them are what a player would actually
/// meet.</para>
///
/// <para><b>Everything here presses.</b> The pours go through the render tree's own onclick id, dispatched
/// through the same channel the browser's JS side calls — #992's rule, and the reason a claim about the
/// button is a claim about the game rather than about a method name.</para>
/// </summary>
public sealed class TheTenderLeadsTheGalleyCardTests
{
    private const string FreeFlying = "/map?start=wreck";
    private const string Flashing = "/map?start=wreck&tender=flash";
    private const string TheCard = "galley-card";

    /// <summary>Everything he can open a beat with, in his own voice.</summary>
    private static IEnumerable<string> HisOwnVoice =>
        TheTender.Openers
            .Append(TheTender.RareOpener)
            .Concat(TheTender.Pours)
            .Concat(TheTender.Idles)
            .Append(TheTender.LastCall)
            .Append(TheTender.Recovery);

    // ── (e) THE PLATE AND A LINE ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CARD DRAWS HIS PLATE, HIS PICTURE AND SOMETHING HE ACTUALLY SAYS — read off the render tree, and
    /// the line is checked against the shipped pools rather than against a string typed in here, so a
    /// caption invented in the markup fails as loudly as an empty one.
    ///
    /// <para><b>RED PROOF:</b> take the <c>.tender-head</c> block out of the card's markup and this fails on
    /// the plate; leave the plate and drop the <c>@if (_tenderLine …)</c> block and it fails on the line
    /// ("the card drew his plate and then nothing").</para>
    /// </summary>
    [Fact]
    public async Task TheCardLeadsWithHisPlateAndALineHeOwns()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        bench.CallOnTheDispatcher("OpenGalleyCard");
        DeskBench.Painted painted = await bench.RenderAsync();

        DeskBench.Painted.Node card = TheCardNode(painted)
            ?? throw new Xunit.Sdk.XunitException("the card was not raised at all.");

        DeskBench.Painted.Node plate = card.Descendants().FirstOrDefault(n => n.HasClass("tender-plate"))
            ?? throw new Xunit.Sdk.XunitException(
                "the galley card draws no speaker plate — nothing on it says who is talking.");
        Assert.Equal(TheTender.Plate, plate.Spoken);

        DeskBench.Painted.Node line = card.Descendants().FirstOrDefault(n => n.HasClass("tender-line"))
            ?? throw new Xunit.Sdk.XunitException(
                "the card drew his plate and then nothing — a speaker with no line is a label.");
        Assert.Contains(line.Spoken, HisOwnVoice);

        // …and the picture the owner said the first version lacked, pointing at a file that is on disk.
        //
        // Read off the MARKUP BLOBS as well as the element walk, because the two are one question here: the
        // <img> carries no dynamic content, so the compiler folds it into a static run of HTML that arrives
        // as a single Markup frame and is never an element (DeskBench.Painted's own note). Asking only the
        // element walk would report "the card carries no art" about a card that draws it.
        const string Art = "art/b7v-the-tender.jpg";
        bool drawn = card.Descendants().Any(n => n.Element == "img"
                                                 && n.Attributes.GetValueOrDefault("src") == Art)
                     || painted.MarkupBlobs.Any(b => b.Contains(Art, StringComparison.Ordinal));
        Assert.True(drawn, "the card carries no art — the owner's complaint about the desk it replaced was "
            + "that it had none.");
        Assert.True(File.Exists(Path.Combine(ClientSource(), "wwwroot", Art.Replace('/', Path.DirectorySeparatorChar))),
            $"the card points at {Art} and the file is not in wwwroot — a picture that 404s is a card with a "
            + "hole in it.");

        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>
    /// PRESSING "POUR A TOT" MOVES HIM, AND THE THIRD ONE IS THE ONE THE SET ADVISES AGAINST. The threshold
    /// he speaks on is the drink law's own (<see cref="NerveModel.DrunkAt"/> of the tot the pour just
    /// counted) — pressed three times through the real button, so this is a claim about the wire and not
    /// about the seam Core already holds.
    ///
    /// <para><b>RED PROOF:</b> drop the <c>TheTenderPours()</c> call out of <c>PourRumFromGalley</c> and the
    /// first assertion fails — his line never changes off the greeting however many tots go in the glass.
    /// </para>
    /// </summary>
    [Fact]
    public async Task ThePourButtonAnswersInHisVoiceAndTheThirdTotIsTheThresholdOne()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        bench.CallOnTheDispatcher("OpenGalleyCard");
        DeskBench.Painted painted = await bench.RenderAsync();

        string greeting = SpokenLine(painted);
        var heard = new List<string>();

        for (int press = 1; press <= NerveModel.DrunkTotCount; press++)
        {
            DeskBench.Painted.Node pour = TheCardNode(painted)!.Descendants()
                .FirstOrDefault(n => n.Handlers.ContainsKey("onclick")
                                     && n.Name.Contains("Pour a tot", StringComparison.Ordinal))
                ?? throw new Xunit.Sdk.XunitException("the card has no \"Pour a tot\" button.");

            await bench.PressAsync(pour.Handlers["onclick"]);
            painted = await bench.RenderAsync();
            heard.Add(SpokenLine(painted));
        }

        Assert.Equal(NerveModel.DrunkTotCount, (int)bench.Field("_rumTots")!);
        Assert.DoesNotContain(greeting, heard);
        Assert.All(heard, line => Assert.Contains(line, HisOwnVoice));

        // The tot DrunkAt calls drunk is the one he says it on. Everything under it is the pour pool's.
        Assert.Equal(TheTender.LastCall, heard[^1]);
        Assert.All(heard.Take(heard.Count - 1), line => Assert.NotEqual(TheTender.LastCall, line));
        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>
    /// THE OTHER REGISTER IS DRAWN AS A DIFFERENT THING, AND WHAT FOLLOWS IT IS ON THE CARD TOO. Core makes
    /// the pairing structural; this asks the render tree whether the markup honoured it, which is the half a
    /// record type cannot enforce — a template that drew the announcement and forgot the second row would
    /// leave every Core guard green.
    ///
    /// <para><b>RED PROOF:</b> delete the <c>.tender-line</c> row from the card's head and this fails
    /// ("nothing followed it"); merge the two rows into one <c>div</c> and it fails on the separate class,
    /// which is what makes the two registers look like two registers.</para>
    /// </summary>
    [Fact]
    public async Task TheRareBeatIsDrawnApartAndNeverArrivesAlone()
    {
        using DeskBench bench = await DeskBench.BootAsync(Flashing);
        Assert.True((bool)bench.Field("_tenderFlashCheat")!,
            "?tender=flash was not read at boot — the rest of this guard would be waiting on a 1-in-12.");

        bench.CallOnTheDispatcher("OpenGalleyCard");
        DeskBench.Painted painted = await bench.RenderAsync();

        DeskBench.Painted.Node card = TheCardNode(painted)
            ?? throw new Xunit.Sdk.XunitException("the card was not raised at all.");

        DeskBench.Painted.Node announced = card.Descendants()
                .FirstOrDefault(n => n.HasClass("tender-announcement"))
            ?? throw new Xunit.Sdk.XunitException(
                "the roll was forced and the card drew nothing in the other register.");
        Assert.Contains(announced.Spoken, TheTender.Announcements);

        DeskBench.Painted.Node under = card.Descendants().FirstOrDefault(n => n.HasClass("tender-line"))
            ?? throw new Xunit.Sdk.XunitException("nothing followed it.");
        Assert.Equal(TheTender.Recovery, under.Spoken);

        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>
    /// …AND IT IS STILL ONCE A SITTING WITH THE CHEAT HELD DOWN. The lever forces the roll and nothing else,
    /// so shutting the card and opening it again inside one visit does not buy a second one — which is the
    /// law a tester is most likely to break by accident, because the cheat is the state they test in.
    ///
    /// <para><b>RED PROOF:</b> the same one that reds the Core law (delete the <c>FlashbackSpent</c>
    /// early-out) — the second open comes up in the other register again.</para>
    /// </summary>
    [Fact]
    public async Task ForcingTheRollStillDoesNotBuyASecondOneInTheSameSitting()
    {
        using DeskBench bench = await DeskBench.BootAsync(Flashing);

        bench.CallOnTheDispatcher("OpenGalleyCard");
        DeskBench.Painted first = await bench.RenderAsync();
        Assert.NotNull(TheCardNode(first)!.Descendants().FirstOrDefault(n => n.HasClass("tender-announcement")));

        bench.CallOnTheDispatcher("CloseGalleyCard");
        await bench.RenderAsync();
        bench.CallOnTheDispatcher("OpenGalleyCard");
        DeskBench.Painted again = await bench.RenderAsync();

        Assert.Null(TheCardNode(again)!.Descendants().FirstOrDefault(n => n.HasClass("tender-announcement")));
        Assert.Contains(SpokenLine(again), HisOwnVoice);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>The lever is in the guide, where a tester looks for it. This repo's standing habit: a dev
    /// cheat nobody wrote down is a cheat only its author can use.</summary>
    [Fact]
    public void TheLeverIsWrittenDownWhereATesterWouldLookForIt()
    {
        string guide = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "testing-guide.md"));
        Assert.Contains("?tender=flash", guide, StringComparison.Ordinal);
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    private static string SpokenLine(DeskBench.Painted painted) =>
        (TheCardNode(painted)?.Descendants().FirstOrDefault(n => n.HasClass("tender-line"))?.Spoken)
        ?? throw new Xunit.Sdk.XunitException("the card is not saying anything at all.");

    private static DeskBench.Painted.Node? TheCardNode(DeskBench.Painted painted) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass(TheCard) && !n.Hidden);

    private static string ClientSource() => Path.Combine(RepoRoot(), "src", "SpaceSails.Client");

    private static string RepoRoot()
    {
        var at = new DirectoryInfo(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException("the repo root is not above the test binary.");
    }
}
