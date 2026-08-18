using System;
using System.IO;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #760 · THE HANDSET'S FIFTH SWITCH, AND THE POCKET'S HEADINGS. Core decides everything about a send and
/// everything about a grouping (<c>RemoteSend</c>, <c>SiteOperator.Accesses</c>); what a Core test cannot
/// reach is whether the razor page ASKS — and a rule nothing calls is a rule nobody plays.
///
/// <para>Source-shape guards, for <see cref="TheChitRidesTheCageTests"/>'s reason: the wiring is a partial
/// class and a razor file, and the thing that must never come back is a decision taken in the page or a
/// sentence said where it cannot be read (#680/#686/#736 — in the DOM is not on the screen).</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRemoteSendsOnStandingTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    private static string TheSend() => Between(
        Pages("Map.Combat.Remote.cs"), "private void SendTheStanding()", "/// <summary>");

    /// <summary>
    /// #760 · THE SWITCH IS ON THE HANDSET, IT IS THE VERB THE OWNER NAMED, AND IT IS LIVE.
    ///
    /// <para><b>Proven RED</b> by pointing the switch at the fourth verb instead of the fifth:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// String:    "@if (_showCaptainsRemote)\n    {\n        &lt;"···
    /// Not found: "@onclick="SendTheStanding""
    /// </code>
    /// </summary>
    [Fact]
    public void TheHandsetDrawsTheSendSwitch()
    {
        string switches = Between(
            Pages("Map.razor"), "@if (_showCaptainsRemote)", "<div class=\"vent-readout\">@SilentRunning.SpinUpLabel");

        Assert.Contains("@onclick=\"SendTheStanding\"", switches, StringComparison.Ordinal);
        Assert.Contains("@RemoteSend.OpenLabel", switches, StringComparison.Ordinal);
        Assert.Contains("@RemoteSend.Blurb", switches, StringComparison.Ordinal);

        // …and it is never DISABLED. A switch that goes dead where it would refuse teaches a captain nothing
        // except that the game is arbitrary (#212/#603) — the refusals are the whole way this rule is learned.
        Assert.DoesNotContain("disabled", Between(
            switches, "@onclick=\"SendTheStanding\"", "</button>"), StringComparison.Ordinal);
    }

    /// <summary>
    /// #760 · THE PAGE DECIDES NOTHING, AND SAYS NOTHING OF ITS OWN. Which gate, whether it opens, what it
    /// costs and every word of the answer are Core's; the method presses the button and shows what came back.
    ///
    /// <para><b>Proven RED</b> by having <c>SendTheStanding</c> write its own sentence instead of Core's
    /// (<c>_remoteOutcome = "📡 Nothing answers.";</c>):</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// Not found: "_remoteOutcome = sent.Line"
    /// </code>
    /// </summary>
    [Fact]
    public void TheSendAsksCoreAndTellsCoresAnswerOnThePanelThatIsUp()
    {
        string send = TheSend();

        // #715 · …and the fourth argument is the outfit's own memory of this captain, which is the one thing
        // the page is allowed to look up and hand over. It still decides nothing: whether that memory changes
        // the answer is Core's clause (IllegalHeat.TheNetStopsAnswering), not a branch on this page.
        Assert.Contains("RemoteSend.Send(ex.Stop.Body.Id, ex.Floor, _satchel, ", send, StringComparison.Ordinal);
        Assert.Contains("IllegalHeat.HeatAtSite(_contacts, ex.Stop.Body.Id)", send, StringComparison.Ordinal);

        // #715 · …and what the send COST is banked, through the one call every crossing in the game goes
        // through. #929 published this charge and banked it nowhere; that wire is what this issue was.
        Assert.Contains("BankTheCrossing(sent.Charge)", send, StringComparison.Ordinal);

        // #736 · The answer lands on the pop-up that is up — the remote's own outcome line — and not on the
        // pulse, which renders under this modal's backdrop blur (in the DOM, not on the screen).
        Assert.Contains("_remoteOutcome = sent.Line", send, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPulseMessage", send, StringComparison.Ordinal);

        // The book keeps it whatever the screen does (#693).
        Assert.Contains("FileNote(sent.Line", send, StringComparison.Ordinal);
    }

    /// <summary>
    /// #760 · THE POCKET GROUPS BY CORE'S ANSWER, AND KEEPS ITS FLAT LIST FOR THE ONE-OUTFIT CASE. Both
    /// halves, because grouping always would be a heading that cannot be read for information and grouping
    /// never would be the feature missing.
    ///
    /// <para><b>Proven RED</b> by deleting the <c>else</c> branch (the page grouping unconditionally):</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// Not found: "else"
    /// </code>
    /// </summary>
    [Fact]
    public void TheWalletFoldDrawsCoresHeadingsAndCoresFlatList()
    {
        string cards = Between(
            Pages("Map.razor"), "<div class=\"satchel-wallet-cards\">", "@foreach (SpaceSails.Core.Satchel.Item item in _satchel)");

        Assert.Contains("SpaceSails.Core.SiteOperator.Accesses(wallet)", cards, StringComparison.Ordinal);
        Assert.Contains("outfit.Heading", cards, StringComparison.Ordinal);
        Assert.Contains("outfit.Cards", cards, StringComparison.Ordinal);
        Assert.Contains("else", cards, StringComparison.Ordinal);

        // The page owns no arithmetic about companies: it never asks who runs a site, it is TOLD.
        Assert.DoesNotContain("SiteOperator.Of(", cards, StringComparison.Ordinal);
    }

    /// <summary>#760 · And the strings the two surfaces draw are Core's constants rather than prose typed
    /// into a razor file — the thing that would let the switch and the sim drift apart one word at a
    /// time.</summary>
    [Fact]
    public void TheStringsAreCores()
    {
        Assert.False(string.IsNullOrWhiteSpace(RemoteSend.OpenLabel));
        Assert.False(string.IsNullOrWhiteSpace(RemoteSend.Blurb));
        Assert.StartsWith(SiteOperator.HeadingGlyph, $"{SiteOperator.HeadingGlyph} x", StringComparison.Ordinal);

        string razor = Pages("Map.razor");
        Assert.DoesNotContain("SEND STANDING", razor, StringComparison.Ordinal);
    }
}
