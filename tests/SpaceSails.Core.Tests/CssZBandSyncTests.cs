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

    /// <summary>
    /// #251 item 3 · THE MAP'S WHOLE CASCADE, not one file of it. <c>Map.razor.css</c> was split into the
    /// page's own sheet plus seventy-six surface sheets under <c>Pages/Map/</c>, and the overlay bands this
    /// gate resolves went with their cards: <c>.jump-overlay</c> is <c>JumpCard.razor.css</c>'s now,
    /// <c>.busted-backdrop</c> is <c>BustedCard.razor.css</c>'s. A z-index is a statement about the whole
    /// stylesheet anyway, so this reads the whole stylesheet — the page's sheet first, then the surfaces in
    /// the order the build bundles them (project-relative path, case-insensitive), which is the order the
    /// browser resolves them in.
    /// </summary>
    private static string ReadMapCascade()
    {
        string surfaces = Path.Combine(CssDir, "Map");
        Assert.True(Directory.Exists(surfaces),
            $"the Map surface stylesheets were not copied for the sync gate: {surfaces}. Without them this " +
            "gate can only see the page's own half of the cascade and would pass by never finding the "
            + "selectors it is supposed to be checking.");

        string[] sheets = [.. Directory.EnumerateFiles(surfaces, "*.razor.css").OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];
        Assert.True(sheets.Length > 60,
            $"only {sheets.Length} Map surface stylesheet(s) beside the test assembly — the copy is broken.");

        return ReadCss("Map.razor.css") + "\n" + string.Join("\n", sheets.Select(File.ReadAllText));
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

    /// <summary>Resolve a selector's <c>z-index</c> expression in the Map cascade to an integer, using the
    /// band values parsed from app.css's <c>:root</c>. Understands <c>var(--z-band)</c> and
    /// <c>calc(var(--z-band) ± N)</c> — the only forms the migrated stylesheet uses for overlay layers.</summary>
    /// <para>#251 item 3 · THE LAST block that sets it, not the first that mentions the selector. A class
    /// can be written in more than one rule — most of these are named in the #735 capped-card family list as
    /// well as in their own block — and it is the LAST z-index in the cascade that the browser resolves. The
    /// old reader took the first match, which was the right answer only while every rule for a card sat in
    /// one file above the family law; the split put `.deck-shuttle-card`'s own block in
    /// <c>ShuttleBayCard.razor.css</c>, AFTER the family list that mentions it and declares no z-index at
    /// all, and the first-match reader went red on a stylesheet that had not changed a value.</para>
    private static int ResolveSelectorZ(string css, string selector, IReadOnlyDictionary<string, int> bands)
    {
        MatchCollection blocks = Regex.Matches(css, Regex.Escape(selector) + @"\s*\{(?<body>.*?)\}", RegexOptions.Singleline);
        Assert.True(blocks.Count > 0, $"selector {selector} not found anywhere in the Map cascade");

        Match z = blocks.Reverse()
            .Select(b => Regex.Match(b.Groups["body"].Value, @"z-index:\s*(?<expr>[^;]+);"))
            .FirstOrDefault(m => m.Success) ?? Match.Empty;
        Assert.True(z.Success, $"selector {selector} has no z-index in any of its {blocks.Count} rule(s)");
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
        { ".map-layers", OverlayBands.MapLayers },
        { ".map-loading", OverlayBands.MapLoading },
        { ".deck-pulse-toast", OverlayBands.DeckPulseToast },
        { ".deck-offer-card", OverlayBands.DeckOfferCard },
        { ".deck-shuttle-card", OverlayBands.DeckShuttleCard },
        { ".arrival-brake-card", OverlayBands.ArrivalBrakeCard },
        { ".start-picker-backdrop", OverlayBands.StartPickerBackdrop },
        // #1052 · The docked news panel joins the sync gate the day it is created, and its number is the one
        // in this table chosen as a REFUSAL: it stays UNDER the cards, so a rep's pitch lands over the paper
        // rather than the paper over the man. A CSS edit that nudged it past 1320 would silently put a
        // newspaper on top of every card a bar can raise.
        { ".seated-news", OverlayBands.SeatedNewsPanel },
        { ".view-object-backdrop", OverlayBands.ViewObjectBackdrop },
        { ".pin-backdrop", OverlayBands.PinBackdrop },
        { ".satchel-backdrop", OverlayBands.SatchelBackdrop },
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
        string css = ReadMapCascade();
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
        string css = ReadMapCascade();
        IReadOnlyDictionary<string, int> bands = ParseRootBands();

        int lifeline = ResolveSelectorZ(css, ".map-adrift", bands);
        // #1027 · `.satchel-backdrop` joins the sweep the day it is created. It is the HIGHEST thing in the
        // desks-and-pop-ups band (1330, one over the cards it was buried under), which makes it the sharpest
        // case this assertion has: if the pocket ever climbed past the lifeline, a captain reading his own
        // satchel could no longer see the button that calls the tow.
        foreach (string popup in new[] { ".deck-offer-card", ".arrival-brake-card", ".start-picker-backdrop", ".view-object-backdrop", ".pin-backdrop", ".satchel-backdrop" })
        {
            Assert.True(lifeline > ResolveSelectorZ(css, popup, bands), $"lifeline must out-rank {popup}");
        }
        Assert.True(lifeline < ResolveSelectorZ(css, ".rescue-backdrop", bands), "lifeline must sit below its rescue modal");
    }
}
