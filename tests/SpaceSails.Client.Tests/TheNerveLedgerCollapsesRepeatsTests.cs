using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U09-F13 · THE NERVE LEDGER PRINTED NINE IDENTICAL "warm air and standing lights - the floor is holding +1"
/// ROWS. Consecutive identical causes now read as one row with a count. Asked of the real renderer through a pen
/// that remembers every line of text it was handed.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheNerveLedgerCollapsesRepeatsTests
{
    private sealed class TextPen : IRenderer
    {
        public List<string> Said { get; } = [];
        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Said.Clear();
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) => Said.Add(text);
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) { }
    }

    private static List<string> DrawnLedger(IReadOnlyList<string> ledger)
    {
        var plan = new DeckPlan([], [], [], [], spawnX: 0, spawnY: 0, droidCount: 0,
            fillDroids: (_, _) => { }, location: (_, _) => "field");
        var pen = new TextPen();
        var state = new DeckView.State(0, 0, 0, 0, 0, ShuttleAway: false, ElectricUniverse: false,
            ShowNerve: true, NerveLedger: ledger);
        new DeckView(pen).Draw(plan, 1200, 700, simTime: 0, in state);
        return pen.Said;
    }

    [Fact]
    public void NineIdenticalRowsAreDrawnOnceWithACount_AndDistinctRowsAreUntouched()
    {
        const string Ease = "warm air and standing lights - the floor is holding +1";
        const string Other = "it laid hands on you  -1";
        var ledger = Enumerable.Repeat(Ease, 9).Append(Other).ToList();

        List<string> said = DrawnLedger(ledger);

        Assert.Contains(said, t => t.StartsWith(Ease, StringComparison.Ordinal) && t.Contains("\u00d79", StringComparison.Ordinal));
        Assert.Equal(1, said.Count(t => t.Contains("the floor is holding", StringComparison.Ordinal)));
        Assert.Contains(Other, said);   // the anti-vacuous half: a different cause is still its own row
    }

    [Fact]
    public void ARepeatThatIsNotAdjacentIsNotMerged()
    {
        IReadOnlyList<string> rows = DeckView.CollapseTheLedgersRuns(["a +1", "b -1", "a +1"])!;
        Assert.Equal(["a +1", "b -1", "a +1"], rows);
    }
}
