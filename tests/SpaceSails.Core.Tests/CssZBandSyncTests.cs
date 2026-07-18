namespace SpaceSails.Core.Tests;

using System.Text.RegularExpressions;
using SpaceSails.Core;

/// <summary>
/// #299 — the sync gate that closes Lab 34's one blind spot. The rescue registry (#293) hand-transcribed
/// z-indexes from the CSS, so an un-mirrored stylesheet edit escaped the reachability gate. This test parses
/// the LIVE stylesheets and asserts they agree with <see cref="OverlayBands"/> — the single source of truth —
/// so a CSS edit that buries a critical control (drops a band, or points an overlay at the wrong band) fails
/// <c>dotnet test</c> instead of surfacing in a playtest. Pure file parsing: no browser, no new CI stage.
///
/// <para>The stylesheets are copied beside the test assembly at build (see the test .csproj), so every run
/// reads the current source.</para>
/// </summary>
public class CssZBandSyncTests
{
    private static string CssDir => Path.Combine(AppContext.BaseDirectory, "cssource");

    private static string ReadCss(string name)
    {
        string path = Path.Combine(CssDir, name);
        Assert.True(File.Exists(path), $"stylesheet not copied for the sync gate: {path}");
        return File.ReadAllText(path);
    }

    /// <summary>The five band anchors declared in app.css's <c>:root</c>, name → value.</summary>
    private static IReadOnlyDictionary<string, int> ParseRootBands()
    {
        string css = ReadCss("app.css");
        Match root = Regex.Match(css, @":root\s*\{(?<body>[^}]*)\}", RegexOptions.Singleline);
        Assert.True(root.Success, "app.css must declare a :root block with the --z-* band anchors (#299)");

        Dictionary<string, int> bands = new(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(root.Groups["body"].Value, @"--z-(?<name>[a-z-]+)\s*:\s*(?<val>\d+)\s*;"))
        {
            bands[m.Groups["name"].Value] = int.Parse(m.Groups["val"].Value, System.Globalization.CultureInfo.InvariantCulture);
        }
        return bands;
    }

    /// <summary>Resolve a selector's <c>z-index</c> expression in Map.razor.css to an integer, using the
    /// band values parsed from app.css's <c>:root</c>. Understands <c>var(--z-band)</c> and
    /// <c>calc(var(--z-band) ± N)</c> — the only forms the migrated stylesheet uses for overlay layers.</summary>
    private static int ResolveSelectorZ(string css, string selector, IReadOnlyDictionary<string, int> bands)
    {
        Match block = Regex.Match(css, Regex.Escape(selector) + @"\s*\{(?<body>.*?)\}", RegexOptions.Singleline);
        Assert.True(block.Success, $"selector {selector} not found in Map.razor.css");

        Match z = Regex.Match(block.Groups["body"].Value, @"z-index:\s*(?<expr>[^;]+);");
        Assert.True(z.Success, $"selector {selector} has no z-index");
        string expr = z.Groups["expr"].Value.Trim();

        Match var = Regex.Match(expr, @"var\(\s*--z-(?<band>[a-z-]+)\s*\)");
        Assert.True(var.Success, $"selector {selector} must set z-index through a band variable, not a raw value: '{expr}'");
        Assert.True(bands.TryGetValue(var.Groups["band"].Value, out int bandValue),
            $"selector {selector} references unknown band --z-{var.Groups["band"].Value}");

        Match offset = Regex.Match(expr, @"([+\-])\s*(\d+)");
        if (offset.Success)
        {
            int n = int.Parse(offset.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            return offset.Groups[1].Value == "-" ? bandValue - n : bandValue + n;
        }
        return bandValue;
    }

    [Fact]
    public void RootBandAnchors_MatchTheCoreConstants()
    {
        IReadOnlyDictionary<string, int> bands = ParseRootBands();

        Assert.Equal(OverlayBands.BaseMap, bands["base-map"]);
        Assert.Equal(OverlayBands.MapChrome, bands["map-chrome"]);
        Assert.Equal(OverlayBands.DesksAndPopups, bands["desks-popups"]);
        Assert.Equal(OverlayBands.DistressLifeline, bands["distress-lifeline"]);
        Assert.Equal(OverlayBands.Modal, bands["modal"]);
    }

    /// <summary>Every migrated overlay selector, with the exact z it must resolve to. This is the contract
    /// the stylesheet may not break silently: change a band value or repoint a selector and the number here
    /// (an <see cref="OverlayBands"/> constant) no longer matches — the build goes red.</summary>
    public static TheoryData<string, int> Overlays => new()
    {
        { ".map-dest-panel", OverlayBands.MapDestPanel },
        { ".desk-layer", OverlayBands.DeskLayer },
        { ".map-body-menu", OverlayBands.MapBodyMenu },
        { ".map-dossier", OverlayBands.MapDossier },
        { ".map-hud", OverlayBands.MapHud },
        { ".map-topstack", OverlayBands.MapTopstack },
        { ".parrot-perch", OverlayBands.ParrotPerch },
        { ".deck-view-toggle", OverlayBands.DeckViewToggle },
        { ".map-layers", OverlayBands.MapLayers },
        { ".map-loading", OverlayBands.MapLoading },
        { ".deck-pulse-toast", OverlayBands.DeckPulseToast },
        { ".deck-offer-card", OverlayBands.DeckOfferCard },
        { ".deck-shuttle-card", OverlayBands.DeckShuttleCard },
        { ".arrival-brake-card", OverlayBands.ArrivalBrakeCard },
        { ".start-picker-backdrop", OverlayBands.StartPickerBackdrop },
        { ".view-object-backdrop", OverlayBands.ViewObjectBackdrop },
        { ".pin-backdrop", OverlayBands.PinBackdrop },
        { ".map-adrift", OverlayBands.MapAdrift },
        { ".rescue-backdrop", OverlayBands.RescueBackdrop },
        { ".mission-celebration-backdrop", OverlayBands.MissionCelebrationBackdrop },
        { ".jump-overlay", OverlayBands.JumpOverlay },
        { ".busted-backdrop", OverlayBands.BustedBackdrop },
    };

    [Theory]
    [MemberData(nameof(Overlays))]
    public void EveryOverlaySelector_ResolvesToItsBandConstant(string selector, int expectedZ)
    {
        string css = ReadCss("Map.razor.css");
        IReadOnlyDictionary<string, int> bands = ParseRootBands();

        Assert.Equal(expectedZ, ResolveSelectorZ(css, selector, bands));
    }

    [Fact]
    public void TheDiceTray_ResolvesToItsBandConstant_AndSitsBelowTheLifeline()
    {
        // #305 — the shared dice tray is a separate component, but its z-index still rides the band scheme.
        // The gate parses its scoped stylesheet and pins it to OverlayBands.DiceTray, and asserts it can
        // never out-rank the distress lifeline (a dice reveal must never bury the rescue button).
        string trayCss = ReadCss("DiceTray.razor.css");
        IReadOnlyDictionary<string, int> bands = ParseRootBands();

        int tray = ResolveSelectorZ(trayCss, ".dice-tray", bands);
        Assert.Equal(OverlayBands.DiceTray, tray);
        Assert.True(tray < OverlayBands.DistressLifeline, "the dice tray must sit below the distress lifeline");
    }

    [Fact]
    public void TheDistressLifeline_OutRanksEveryDesksAndPopupsOverlay_InTheLiveCss()
    {
        // The load-bearing invariant, verified against the stylesheet itself: the reserved lifeline band
        // sits above every routine desk/pop-up overlay and below the rescue modal it opens.
        string css = ReadCss("Map.razor.css");
        IReadOnlyDictionary<string, int> bands = ParseRootBands();

        int lifeline = ResolveSelectorZ(css, ".map-adrift", bands);
        foreach (string popup in new[] { ".deck-offer-card", ".arrival-brake-card", ".start-picker-backdrop", ".view-object-backdrop", ".pin-backdrop" })
        {
            Assert.True(lifeline > ResolveSelectorZ(css, popup, bands), $"lifeline must out-rank {popup}");
        }
        Assert.True(lifeline < ResolveSelectorZ(css, ".rescue-backdrop", bands), "lifeline must sit below its rescue modal");
    }
}
