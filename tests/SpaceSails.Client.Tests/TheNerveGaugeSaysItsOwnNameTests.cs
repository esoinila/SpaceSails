using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #380 item 2 · <b>THE GAUGE HAS TO SAY WHAT IT IS.</b>
///
/// <para>The mystery audit's second-ranked mystifier: a ten-pip meter drained in the corner of the screen
/// with <i>no name, no cause and no remedy</i> anywhere near it. The name landed in
/// <c>DeckView.DrawNerveGauge</c> — the plate is titled <b>NERVE</b>, the one diegetic noun every flavour
/// rung, band drop and shock pulse already speaks — and the cause and remedy landed in the band-drop pulse
/// and the nerve ledger. This class is the half of that a unit test can hold: <b>the plate carries its
/// label, in every mode the gauge is drawn in.</b></para>
///
/// <h3>Why this is not the UiGate's job</h3>
/// <para>The browser gate (<c>HudCollisionTests</c>) asks whether anything is drawn ON TOP of this corner,
/// which is a question about laid-out pixels and needs a real layout. It cannot ask whether the corner says
/// anything at all — a gauge whose label was deleted collides with nothing and passes that gate perfectly.
/// The label is a fact about the DRAW CALL, so it is asserted where draw calls can be recorded, and in both
/// arrangements, because the compact aboard-ship size and the full regolith size are two code paths through
/// one method and #559 has already buried one of them once.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheNerveGaugeSaysItsOwnNameTests
{
    private const int WidthPx = 1280;
    private const int HeightPx = 800;

    private sealed record Text(float X, float Y, string Value);

    /// <summary>The transcribing pen — this suite's standing idiom for asking about a DRAW CALL rather than
    /// about a field somebody hoped was drawn. Here it keeps the words and where they were put.</summary>
    private sealed class TextRecordingPen : IRenderer
    {
        public List<Text> Texts { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Texts.Clear();
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
            TextAlign align = TextAlign.Left) => Texts.Add(new Text(x, y, text ?? ""));
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
            float x, float y, float w, float h, float a = 1f) { }
    }

    /// <summary>One frame of the real ship deck, drawn by the real <see cref="DeckView"/>, with the gauge
    /// shown at the size the caller asks for.</summary>
    private static List<Text> Frame(bool compact)
    {
        var pen = new TextRecordingPen();
        new DeckView(pen).Draw(DeckPlan.Ship, WidthPx, HeightPx, 0,
            new DeckView.State(
                DeckPlan.Ship.SpawnX, DeckPlan.Ship.SpawnY, 0, CargoUnits: 0, Charge: 0,
                ShuttleAway: false, ElectricUniverse: false,
                Nerve: NerveModel.Max * 0.5, NerveReadout: "", ShowNerve: true, NerveCompact: compact),
            0, 0, null);
        return pen.Texts;
    }

    /// <summary>
    /// THE PLATE IS TITLED, ON THE REGOLITH AND ABOARD. The label asserted is the meter's own name as the
    /// rest of the game says it — the noun the pulse, the ledger heading and the guide all use — so a
    /// renderer that drew the pips with no title, or titled them something else, reddens here.
    ///
    /// <para><b>Proven RED</b> by deleting the <c>DrawText(x0, y0 - 6, "NERVE", …)</c> call from
    /// <c>DeckView.DrawNerveGauge</c>: both cases fail, aboard and compact.</para>
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheNervePlateIsDrawnWithItsName(bool compact)
    {
        List<Text> texts = Frame(compact);

        Assert.True(
            texts.Any(t => string.Equals(t.Value, "NERVE", StringComparison.Ordinal)),
            "the nerve gauge drew no label. #380 item 2 is the whole reason it has one: an unnamed meter "
            + "draining in the corner is the second-worst mystifier in this game. Words drawn this frame: "
            + string.Join(" · ", texts.Select(t => t.Value).Distinct(StringComparer.Ordinal)));
    }

    /// <summary>
    /// AND IT IS DRAWN WHERE THE GAUGE IS, NOT SOMEWHERE ELSE ON THE GLASS. The label sits just above the
    /// bar's own top edge, and the bar's top edge is <see cref="HudColumn"/>'s measurement — the one number
    /// the renderer and the motion tracker's anchor both read (#561). Asserted against that measurement
    /// rather than against a typed-in y, so moving the column moves the claim with it.
    ///
    /// <para><b>Proven RED</b> by drawing the label at a fixed <c>y0 - 60</c> instead of <c>y0 - 6</c>: it
    /// leaves the block it titles and this fails in both arrangements.</para>
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheNameSitsOnTheBlockItTitles(bool compact)
    {
        Text label = Frame(compact).Single(t => string.Equals(t.Value, "NERVE", StringComparison.Ordinal));
        HudColumn.NerveBlock block = HudColumn.For(compact);

        // Above the bar (a title, not an overlay) and no further above it than the plate's own head, which
        // is exactly the space the plate was widened to back.
        Assert.InRange(label.Y, (float)(block.BaseY - HudColumn.PlateHeadPx), (float)block.BaseY);
    }
}
