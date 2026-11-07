using SpaceSails.Client.Rendering;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>#1478 · At the monolith the key-hint captions under the motion tracker (T deploy here, shift-T hold my
/// line home...) ran down into the NERVE LEDGER anchored at the bottom of the same column. The ledger gives up
/// its OLDEST rows until its block clears the captions.</summary>
public sealed class TheLedgerSitsClearOfTheKeyHintsTests
{
    [Fact]
    public void WithNoCaptionsEveryLineFits()
    {
        Assert.Equal(5, DeckView.NerveLedgerLinesThatFit(5, false, 13f, 720, 0f));
    }

    [Fact]
    public void CaptionsThatReachDownTrimTheOldestRows()
    {
        // 720 - 136 = 584 is the ledger's floor. Captions ending at 520 leave 64 - 6 = 58 px: header + 3 lines of 13.
        int keep = DeckView.NerveLedgerLinesThatFit(8, false, 13f, 720, 520f);
        Assert.Equal(3, keep);
        float top = 720 - DeckView.NerveLedgerBottomClearancePx - ((keep + 1) * 13f);
        Assert.True(top >= 520f + 6f, "the kept block still reaches into the captions.");
    }

    [Fact]
    public void NoRoomAtAllDrawsNoLedger()
    {
        Assert.Equal(0, DeckView.NerveLedgerLinesThatFit(8, false, 13f, 720, 580f));
    }
}
