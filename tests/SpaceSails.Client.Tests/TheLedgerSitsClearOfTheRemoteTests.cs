using System;
using System.IO;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>#1401 R1-F73 · The nerve ledger grows upward from the bottom of the left column, and the 📻 Remote
/// button stands in the same corner. The ledger's last line must sit above the button's top edge, read off the
/// button's own stylesheet rule rather than a number copied into the test.</summary>
public sealed class TheLedgerSitsClearOfTheRemoteTests
{
    [Fact]
    public void TheLastLedgerLineIsAboveTheTopOfTheRemoteButton()
    {
        string dir = AppContext.BaseDirectory;
        string? root = dir;
        while (root is not null && !File.Exists(Path.Combine(root, "SpaceSails.slnx")))
        {
            root = Path.GetDirectoryName(root);
        }
        Assert.NotNull(root);

        string css = File.ReadAllText(Path.Combine(root!, "src", "SpaceSails.Client", "Pages", "Map", "CaptainsRemoteButton.razor.css"));
        Match rule = Regex.Match(css, @"\.captains-remote-btn\s*\{[^}]*?bottom:\s*([0-9.]+)rem", RegexOptions.Singleline);
        Assert.True(rule.Success, "the remote button no longer states its bottom offset in rem.");
        double buttonBottomPx = double.Parse(rule.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) * 16.0;
        const double smallButtonHeightPx = 30.0;

        Assert.True(DeckView.NerveLedgerBottomClearancePx >= buttonBottomPx + smallButtonHeightPx,
            $"the ledger leaves {DeckView.NerveLedgerBottomClearancePx}px but the remote reaches {buttonBottomPx + smallButtonHeightPx}px up.");
    }
}
