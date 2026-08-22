using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #962 · THE WARM-UP THAT NEVER WARMED ANYTHING.
///
/// <para>Found while hunting the owner's "An unhandled error has occurred" with the game's own console open.
/// Every boot printed, and swallowed, this:</para>
///
/// <code>
/// boot surface warm-up skipped: System.NullReferenceException: Arg_NullReferenceException
///    at SpaceSails.Client.Rendering.DeckView.IsSweeper(String name)   DeckView.Frame.cs:1338
///    at SpaceSails.Client.Rendering.DeckView.DrawTheFigures(...)      DeckView.Frame.cs:777
///    at SpaceSails.Client.Rendering.DeckView.Draw(...)                DeckView.Frame.cs:122
///    at SpaceSails.Client.Pages.Map.WarmSurfaceDrawPathAtBootAsync()  Map.Sim.Boot.cs:141
/// </code>
///
/// <para><b>The fault is a plan lying about its own buffer.</b> The throwaway warm-up plan claimed
/// <c>DeckPlan.Ship.DroidCount</c> figures and handed in a no-op fill, so
/// <see cref="DeckView"/>'s figure loop — which walks <c>plan.DroidCount</c>, as it must — read three
/// default <see cref="DeckPlan.Droid"/> structs, whose <c>Name</c> is <c>null</c>, and asked
/// <c>name.StartsWith("SWEEP-")</c> of nothing. A count and a fill are ONE statement made twice, which is
/// this repo's oldest named fault; the previous appearances (#633, #731) drew nobody or threw an
/// <c>IndexOutOfRangeException</c>, and this one threw a <c>NullReferenceException</c> into a catch.</para>
///
/// <para><b>What it cost.</b> Nothing visible — and that is the point. The whole purpose of
/// <c>WarmSurfaceDrawPathAtBootAsync</c> (#371 Phase 1) is to pay the cold, interpreted first surface draw
/// once, invisibly, behind the start picker. It threw before it drew a single figure, so the paint never
/// happened, so the player went on paying that frame on the first real one, on every boot, for as long as
/// this has been here. A perf fix that silently does not run is indistinguishable from not having it.</para>
///
/// <para><b>The RED case.</b> Put <c>DeckPlan.Ship.DroidCount</c> back in
/// <c>Map.BootWarmUpPlan()</c> in place of <c>droidCount: 0</c> and this test throws the exact
/// <see cref="NullReferenceException"/> above — the guard is on the production expression itself, not on a
/// re-typed look-alike that could go on passing after that line changed.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBootWarmUpActuallyPaintsTests
{
    private const int WidthPx = 1200;
    private const int HeightPx = 700;

    /// <summary>The transcribing pen — this suite's standing idiom for asking about a DRAW CALL rather than
    /// about a field somebody hoped was drawn. Here it only has to count: did the pen get used at all?</summary>
    private sealed class CountingPen : IRenderer
    {
        public int Calls { get; private set; }
        public List<string> Text { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) { Calls = 0; Text.Clear(); }
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) => Calls++;
        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) => Calls++;
        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) => Calls++;
        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px sans-serif",
                             TextAlign align = TextAlign.Left) { Calls++; Text.Add(text ?? ""); }
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) => Calls++;
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) => Calls++;
    }

    private static DeckView.SurfaceHud EmptyHud() => new(
        DigProgress: -1, HasDroppedChest: false, DropX: 0, DropY: 0,
        Blips: Array.Empty<(double, double, bool)>(), Cadence: 0, Readout: "",
        CacheMarks: Array.Empty<(double, double, bool)>(),
        Nerve: SpaceSails.Core.NerveModel.Steady, NerveReadout: "");

    [Fact]
    public void TheBootWarmUpPlan_PaintsInsteadOfThrowing()
    {
        DeckPlan warm = SpaceSails.Client.Pages.Map.BootWarmUpPlan();

        var pen = new CountingPen();
        var state = new DeckView.State(
            MoonSurface.SpawnX, MoonSurface.SpawnY, 0, 0, 0,
            ShuttleAway: false, ElectricUniverse: false);

        // The exact call the boot makes. Before the fix this threw NullReferenceException out of
        // IsSweeper(null) — into a catch that turned a dead perf fix into silence.
        new DeckView(pen).Draw(warm, WidthPx, HeightPx, 0, in state, 0, 0, EmptyHud());

        Assert.True(pen.Calls > 0,
            "the warm-up painted nothing at all. Its ONE job is to pay the cold surface draw once, behind " +
            "the start picker; a warm-up that issues no draw calls has not warmed the path it exists for.");
    }

    [Fact]
    public void TheBootWarmUpPlan_ClaimsNoFiguresBecauseItFillsNone()
    {
        // The law under the fix, stated so it cannot drift back: a plan's DroidCount is a promise about
        // what its fill writes, and this plan's fill writes nothing. Any non-zero count here is a buffer
        // of default structs handed to a renderer that will read their names.
        DeckPlan warm = SpaceSails.Client.Pages.Map.BootWarmUpPlan();

        Assert.Equal(0, warm.DroidCount);
    }

    [Fact]
    public void EveryFigureAPlanPromises_HasAName()
    {
        // The general shape of the fault, asked of the throwaway plan's own buffer: fill it exactly as the
        // renderer does and check that no slot inside DroidCount came back a default struct. This is the
        // assertion that would have caught #633's silent version and #731's loud one alike.
        DeckPlan warm = SpaceSails.Client.Pages.Map.BootWarmUpPlan();

        var buffer = new DeckPlan.Droid[DeckPlan.MaxDroids];
        warm.FillDroids(0, buffer);

        for (int i = 0; i < warm.DroidCount; i++)
        {
            Assert.False(string.IsNullOrEmpty(buffer[i].Name),
                $"figure {i} of {warm.DroidCount} has no name. The renderer asks every figure's name to " +
                "decide what ink it gets (IsSweeper / IsGuardName / IsPatron), so an unnamed slot inside " +
                "the promised count is a NullReferenceException on the next frame that draws it.");
        }
    }
}
