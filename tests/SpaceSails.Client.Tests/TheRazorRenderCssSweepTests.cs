using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;
using Pages = SpaceSails.Client.Pages;

namespace SpaceSails.Client.Tests;

/// <summary>
/// The ultracode review's razor / renderer / CSS lane: small promises the page and the glass made and did
/// not keep. Each guard names the line that fixes it; each was proven RED by reverting that line.
/// </summary>
public sealed class TheRazorRenderCssSweepTests
{
    private const int WidthPx = 1280, HeightPx = 720;
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    // ── the keybar is screen chrome, not world ───────────────────────────────────────────────────────

    private sealed class WordPen : IRenderer
    {
        public List<(float X, float Y, string Text)> Words { get; } = [];
        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) { }
        public void EndFrame() { }
        public int RegisterImage(string url) => 1;
        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }
        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) { }
        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }
        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f) { }
        public void DrawText(float x, float y, string text, RgbaColor color,
                             string font = "12px sans-serif", TextAlign align = TextAlign.Left) =>
            Words.Add((x, y, text));
    }

    /// <summary>
    /// On a follow-cam ground the world origin slides with the captain, so a keybar centred on it walked off
    /// the glass. Drawn with the captain at two places 30 du apart, the bar must stay on the screen's middle.
    ///
    /// <para>RED PROOF: put <c>ox</c> back as the x of the two DrawText calls in
    /// <c>DrawTheInstruments</c> and this fails with the bar 600px apart.</para>
    /// </summary>
    [Fact]
    public void TheKeybarStaysOnTheGlassWhileTheCaptainWalks()
    {
        DeckPlan ground = Scenes.Build("surface:miranda:0");
        Assert.True(ground.FollowCam, "this bench needs a follow-cam ground to be able to tell pass from fail");

        float BarX(double dx)
        {
            var state = new DeckView.State(ground.SpawnX + dx, ground.SpawnY, 0.9, 0, 0,
                ShuttleAway: false, ElectricUniverse: false);
            var pen = new WordPen();
            new DeckView(pen).Draw(ground, WidthPx, HeightPx, simTime: 0.0, in state, surface: null);
            return pen.Words.Single(w => w.Y == HeightPx - 10).X;
        }

        Assert.Equal(WidthPx / 2f, BarX(0), 3);
        Assert.Equal(WidthPx / 2f, BarX(30), 3);
    }

    // ── Rauha Lind is a patron ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// RED PROOF: drop the <c>CarryThePress.Plate</c> arm from <c>DeckView.IsPatron</c> and "Lind" is staff.
    /// </summary>
    [Fact]
    public void RauhaLindIsNotStaffForTheCrewGlance()
    {
        MethodInfo isCrew = typeof(DeckView).GetMethod(
            "IsCrew", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.False((bool)isCrew.Invoke(null, [CarryThePress.Plate])!,
            "the seated bar regular turns to look at the barkeep during a hull shudder as if she worked there");
        Assert.True((bool)isCrew.Invoke(null, ["Barkeep"])!, "the bench's own control: a working name is crew");
    }

    // ── the surface memo knows the site's name ───────────────────────────────────────────────────────

    /// <summary>
    /// RED PROOF: take <c>siteName</c> out of the <c>SurfaceDeckKey.For</c> call in
    /// <c>MoonSurface.SurfaceDeck</c> and the landing is served the warm-up layout with no header.
    /// </summary>
    [Fact]
    public void ALandingIsNotServedTheWarmUpsLayoutWithoutItsHeader()
    {
        static DeckPlan Build(string siteName) => MoonSurface.SurfaceDeck(
            "miranda", "Miranda", [], 0, (_, _) => { }, siteName: siteName);

        Build(""); // the boot warm-up, first into the memo
        DeckPlan landing = Build("The Wild Plain");
        Assert.Contains(landing.RoomLabels,
            l => l.Text.Contains("SET DOWN AT: THE WILD PLAIN", StringComparison.Ordinal));
    }

    // ── a hull is not regolith ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// RED PROOF: remove the <c>OnADeadHull</c> arm from <c>WhereYouAreStanding</c>.
    /// </summary>
    [Theory]
    [InlineData("wreck-test")]
    [InlineData("station-test")]
    public void SettingAPaperDownAboardAHullNeverSaysRegolith(string bodyId)
    {
        var map = new Pages.Map();
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(bodyId, bodyId, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;
        exType.GetProperty("Stop")!.SetValue(ex, stop);
        typeof(Pages.Map).GetField("_surface", Hidden)!.SetValue(map, ex);

        string said = (string)typeof(Pages.Map).GetMethod("WhereYouAreStanding", Hidden)!.Invoke(map, null)!;
        Assert.DoesNotContain("regolith", said, StringComparison.Ordinal);
    }

    // ── the three markup / css promises ──────────────────────────────────────────────────────────────

    /// <summary>
    /// RED PROOF: delete the <c>@onmousedown:preventDefault</c> from either button: the input's blur commits
    /// the draft before the click lands.
    /// </summary>
    [Fact]
    public void TheRenameButtonsDoNotStealFocusBeforeTheirClick()
    {
        string razor = Source("Pages", "Map", "SaveLoadRack.razor");
        foreach (string handler in new[] { "CommitRenameCaptain\">✔", "CancelRenameCaptain\">✕" })
        {
            int at = razor.IndexOf(handler, StringComparison.Ordinal);
            Assert.True(at > 0, $"the rename button ending `{handler}` is gone");
            string button = razor[razor.LastIndexOf("<button", at, StringComparison.Ordinal)..at];
            Assert.Contains("@onmousedown:preventDefault=\"true\"", button, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// RED PROOF: narrow the gate back to <c>hasRead || hasMsg</c>.
    /// </summary>
    [Fact]
    public void TheOperatingLogShowsWheneverItHasEntries()
    {
        string razor = Source("Pages", "Map", "VentPanel.razor");
        Assert.Contains("@if (hasRead || hasMsg || _ventLog.Count > 0)", razor, StringComparison.Ordinal);
    }

    /// <summary>
    /// RED PROOF: put the <c>font:</c> shorthand with the <c>inherit</c> fallback back.
    /// </summary>
    [Fact]
    public void TheLiftStickerHasAValidFont()
    {
        string css = Source("Pages", "Map", "LiftPanel.razor.css");
        int start = css.IndexOf(".lift-pad-sticker {", StringComparison.Ordinal);
        string rule = css[start..css.IndexOf('}', start)];
        Assert.DoesNotContain("--ui-font", rule, StringComparison.Ordinal);
        Assert.DoesNotContain("font:", rule, StringComparison.Ordinal);
        Assert.Contains("font-weight: 700", rule, StringComparison.Ordinal);
    }
}
