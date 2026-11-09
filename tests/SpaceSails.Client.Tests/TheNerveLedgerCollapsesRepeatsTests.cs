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
        public List<(float X, string Text, TextAlign Align)> At { get; } = [];
        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Said.Clear();
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left)
        {
            Said.Add(text);
            At.Add((x, text, align));
        }
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) { }
    }

    private static List<string> DrawnLedger(IReadOnlyList<string> ledger)
    {
        var plan = new DeckPlan([], [], [], [], spawnX: 0, spawnY: 0, droidCount: 0,
            fillDroids: (_, _) => { }, location: (_, _) => "field");
        var pen = new TextPen();
        var state = new DeckView.State(0, 0, 0, 0, ShuttleAway: false,
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

    /// <summary>U10-F10 · At 1024 wide the motion tracker's readout ("something alive - about 44 du, not walking") was
    /// centred on a dial that sits near the left edge and lost its first letter. Swept across window widths: wherever
    /// the text would leave the glass it is set from a margin instead, and wherever it fits it stays centred.</summary>
    [Fact]
    public void TheTrackersReadoutNeverLeavesTheGlass()
    {
        int leftAligned = 0, centred = 0;
        foreach (string Readout in new[] { "something alive - about 44 du, not walking", "no contact" })
        foreach (int width in new[] { 640, 800, 1024, 1280, 1600, 1920 })
        {
            var plan = new DeckPlan([], [], [], [], spawnX: 0, spawnY: 0, droidCount: 0,
                fillDroids: (_, _) => { }, location: (_, _) => "field");
            var pen = new TextPen();
            var hud = new DeckView.SurfaceHud(
                DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
                Blips: [(0.3, 12.0, true)], Cadence: 1, Readout: Readout, CacheMarks: [],
                Nerve: 80, NerveReadout: "STEADY", Instruments: true);
            var state = new DeckView.State(0, 0, 0, 0, ShuttleAway: false);
            new DeckView(pen).Draw(plan, width, 700, simTime: 0, in state, surface: hud);

            (float x, string text, TextAlign align) = pen.At.Single(a => a.Text == Readout);
            double w = CommsBand.WidthOf(Readout, 11);
            double left = align == TextAlign.Center ? x - (w / 2) : x;
            Assert.True(left >= 0, $"at {width} wide the readout starts at {left:0.#}, off the glass.");
            if (align == TextAlign.Left) { leftAligned++; } else { centred++; }
        }

        Assert.True(leftAligned > 0, "the long readout was never moved, so the clip branch asked nothing.");
        Assert.True(centred > 0, "no width kept the readout centred, so the fit branch asked nothing.");
    }
}
