using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceSails.Client.Pages;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #620 slice 2 · THE SHEETS ON THE SHELF, AT THE PLACE THE PRESS HAPPENS. Core pins every band
/// (<c>KeepsakeSheetTests</c>); these hold what only the page knows — the ONE satiety stamp the whole shelf
/// shares (a sheet's minute sates the pendant and the pendant's sates a sheet: both directions), that a sheet
/// raises no flashback and files no field-book line and does not touch the pendant's own first-opening latch,
/// that the shelf holds exactly the sheets the captain holds — and the shelf component RENDERED, so the rows a
/// captain reads are asserted in the markup the real panel draws.
/// </summary>
public class TheSheetsOnTheShelfTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const double CabinX = 12.75, CabinY = -6.5;

    // t=0 is a known CLEAN roll; 38 is a known STING roll (Core's literal pins).
    private const double CleanAt = 0.0, StingAt = 38.0;

    private static readonly HeldMemory.Sheet Photo = new(
        HeldMemory.PhotographId, HeldMemory.Mark.His, HeldMemory.Theory.Love, "four faces at a table",
        ["Hollis Grey"], 86400.0 * 2, HandedBy: "Hollis");

    private static readonly HeldMemory.Sheet Slip = new(
        HeldMemory.SlipId("fixer"), HeldMemory.Mark.His, HeldMemory.Theory.Money, "a deal that had not landed",
        ["Brant"], 86400.0 * 3, HandedBy: "Brant");

    private static object? Get(object o, string name) =>
        (o.GetType().GetField(name, Hidden) as object ?? o.GetType().GetProperty(name, Hidden))
            is { } m ? (m is FieldInfo f ? f.GetValue(o) : ((PropertyInfo)m).GetValue(o)) : throw new MissingMemberException(name);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);

    private static Map ACabinMap(IReadOnlyList<HeldMemory.Sheet> book, double nerve = 40.0, double simTime = CleanAt)
    {
        var map = new Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", Hidden)!.SetValue(map, true);
        Set(map, "_avatarX", CabinX);
        Set(map, "_avatarY", CabinY);
        Set(map, "_nerve", nerve);
        Set(map, "SimTime", simTime);
        Set(map, "_showSatchel", true);
        Set(map, "_heldMemories", book);
        return map;
    }

    private static IReadOnlyList<Keepsake.Piece> Pieces(Map map) =>
        (IReadOnlyList<Keepsake.Piece>)Invoke(map, "KeepsakePieces")!;

    private static void PressId(Map map, string id) =>
        Invoke(map, "PressKeepsake", Pieces(map).Single(p => p.Id == id));

    private static string? Said(Map map) => (string?)Get(map, "_keepsakeSaid");
    private static bool Folded(Map map) => Get(map, "_keepsakeOpenId") is not null;
    private static double Nerve(Map map) => (double)Get(map, "_nerve")!;
    private static double Stamp(Map map) => (double)Get(map, "_lastQuietMinuteSimTime")!;
    private static bool PendantLatch(Map map) => (bool)Get(map, "_pendantFirstOpened")!;
    private static bool Filed(Map map) =>
        ((IEnumerable<FieldNote>)Get(map, "_fieldNotes")!).Any(n => n.Text == Keepsake.FieldBookLine);

    // ── THE SHELF HOLDS EXACTLY WHAT THE CAPTAIN HOLDS ────────────────────────────────────────────────

    [Fact]
    public void TheShelfIsThePendantPlusEveryHeldSheet_AndNoSheetMeansNoRow()
    {
        Assert.Equal([Keepsake.PendantId], Pieces(ACabinMap([])).Select(p => p.Id));
        Assert.Equal([Keepsake.PendantId, HeldMemory.PhotographId, HeldMemory.SlipId("fixer")],
            Pieces(ACabinMap([Photo, Slip])).Select(p => p.Id));

        // the page's live book, not a copy: handing a sheet over later puts it on the shelf
        Map map = ACabinMap([]);
        Set(map, "_heldMemories", HeldMemory.Put((IReadOnlyList<HeldMemory.Sheet>)Get(map, "_heldMemories")!, Photo));
        Assert.Contains(Pieces(map), p => p.Id == HeldMemory.PhotographId);
    }

    // ── THE PRESS ON A SHEET, IN THE CABIN ────────────────────────────────────────────────────────────

    [Fact]
    public void ALoveSheetInTheCabin_SteadiesTheCaptainClean_FoldsOpen_AndRaisesNothing()
    {
        Map map = ACabinMap([Photo], nerve: 40.0);
        PressId(map, HeldMemory.PhotographId);

        // the page lands the relief as WHOLE pips (NervePips.ApplyRelief): 22 = two pips = +20 at a pip of 10
        Assert.Equal(NervePips.ApplyRelief(40.0, NerveModel.KeepsakeRestore).Nerve, Nerve(map), 6);
        Assert.Equal(40.0 + 2 * NervePips.PipUnit, Nerve(map), 6);
        Assert.Contains(Said(map)!, Keepsake.SheetCleanPool);
        Assert.True(Folded(map));
        Assert.True((bool)Get(map, "_showSatchel")!, "a sheet's minute leaves the pocket up — no plate to see.");
        Assert.Null(Get(map, "_storyPlate")); // no flashback beat: the photograph's fired at handover
        Assert.False(Filed(map));             // no field-book line
        Assert.False(PendantLatch(map), "a sheet never spends the pendant's own first opening.");
        Assert.Equal(CleanAt, Stamp(map), 6);
    }

    [Fact]
    public void AMoneySheetInTheCabin_SteadiesLess()
    {
        Map map = ACabinMap([Slip], nerve: 40.0);
        PressId(map, HeldMemory.SlipId("fixer"));

        // 14 = ONE pip: visibly less than the loved face's two
        Assert.Equal(NervePips.ApplyRelief(40.0, NerveModel.KeepsakeMoneyRestore).Nerve, Nerve(map), 6);
        Assert.Equal(40.0 + NervePips.PipUnit, Nerve(map), 6);
        Assert.Contains(Said(map)!, Keepsake.SheetCleanPool);
        Assert.Null(Get(map, "_storyPlate"));
        Assert.False(PendantLatch(map));
    }

    [Fact]
    public void AMoneySheetOnTheKnownBadRoll_CostsADab_AndSaysOneOfTheThreeMoneyLines()
    {
        Map map = ACabinMap([Slip], nerve: 40.0, simTime: StingAt);
        PressId(map, HeldMemory.SlipId("fixer"));

        // the dab is sub-pip: it BANKS in the shock carry (the pendant's sting does the same) and the gauge holds
        Assert.Equal(Keepsake.MoneyStingNerve, (double)Get(map, "_nerveShockCarry")!, 6);
        Assert.Equal(40.0, Nerve(map), 6);
        Assert.Equal(Keepsake.MoneyStingPool[2], Said(map)); // sim-time 38 draws line 2 (Core's pin)
        Assert.True(Folded(map), "a stung minute is still a minute spent: the card folds open on the line.");
        Assert.Equal(StingAt, Stamp(map), 6);
        Assert.False(PendantLatch(map));
    }

    // ── ONE WINDOW FOR THE WHOLE SHELF — BOTH DIRECTIONS ──────────────────────────────────────────────

    [Fact]
    public void ASheetsMinuteSatesThePendant_AndThePendantStillOwesItsFirstOpening()
    {
        Map map = ACabinMap([Photo], nerve: 40.0);
        PressId(map, HeldMemory.PhotographId);
        double after = Nerve(map);

        Set(map, "SimTime", 600.0); // ten sim-minutes later, inside the window
        PressId(map, Keepsake.PendantId);

        Assert.Equal(Keepsake.SatietyLine, Said(map));
        Assert.Equal(after, Nerve(map), 6);
        Assert.Equal(CleanAt, Stamp(map), 6); // the refusal does not re-stamp
        Assert.False(PendantLatch(map), "a refused pendant has not had its first opening.");
        Assert.False(Filed(map));

        // …and once the window has passed, the pendant's first opening still lands, whole
        Set(map, "SimTime", Keepsake.QuietWindowSeconds + 1.0);
        PressId(map, Keepsake.PendantId);
        Assert.True(PendantLatch(map));
        Assert.True(Filed(map));
        Assert.NotNull(Get(map, "_storyPlate"));
    }

    [Fact]
    public void ThePendantsMinuteSatesEverySheet_LoveAndMoneyAlike()
    {
        foreach (HeldMemory.Sheet sheet in new[] { Photo, Slip })
        {
            Map map = ACabinMap([sheet], nerve: 40.0);
            PressId(map, Keepsake.PendantId); // the first opening spends the window
            Set(map, "_showSatchel", true);
            double after = Nerve(map);
            Set(map, "SimTime", 600.0);

            PressId(map, sheet.Id);

            Assert.Equal(Keepsake.SatietyLine, Said(map));
            Assert.False(Folded(map), "a refusal never folds the card open.");
            Assert.Equal(after, Nerve(map), 6);
            Assert.Equal(CleanAt, Stamp(map), 6);
        }
    }

    [Fact]
    public void OneSheetsMinuteSatesTheOtherSheet()
    {
        Map map = ACabinMap([Photo, Slip], nerve: 40.0);
        PressId(map, HeldMemory.PhotographId);
        double after = Nerve(map);
        Set(map, "SimTime", 600.0);

        PressId(map, HeldMemory.SlipId("fixer"));

        Assert.Equal(Keepsake.SatietyLine, Said(map));
        Assert.Equal(after, Nerve(map), 6);
    }

    [Fact]
    public void ASheetPressedOutsideTheCabinDoesNotOpen_NothingRestored_NothingStamped()
    {
        Map map = ACabinMap([Slip], nerve: 40.0, simTime: StingAt);
        Set(map, "_avatarX", 12.0);
        Set(map, "_avatarY", 0.6); // the corridor
        PressId(map, HeldMemory.SlipId("fixer"));

        Assert.Contains(Said(map)!, Keepsake.NotHerePool);
        Assert.Equal(40.0, Nerve(map), 6);
        Assert.Equal(double.NegativeInfinity, Stamp(map));
        Assert.False(Folded(map));
    }

    // ── THE SHELF, RENDERED ───────────────────────────────────────────────────────────────────────────

    private static string Render(IReadOnlyList<Keepsake.Piece> pieces, string? openId = null, string? askedId = null, string? said = null)
    {
        var services = new ServiceCollection().AddSingleton<ILoggerFactory, NullLoggerFactory>().BuildServiceProvider();
        using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<KeepsakeShelf>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(KeepsakeShelf.KeepsakePieces)] = (Func<IReadOnlyList<Keepsake.Piece>>)(() => pieces),
                    [nameof(KeepsakeShelf.KeepsakeOpenId)] = openId,
                    [nameof(KeepsakeShelf.KeepsakeAskedId)] = askedId,
                    [nameof(KeepsakeShelf.KeepsakeSaid)] = said,
                    [nameof(KeepsakeShelf.PressKeepsake)] = (Action<Keepsake.Piece>)(_ => { }),
                    [nameof(KeepsakeShelf.FoldKeepsake)] = (Action)(() => { }),
                }));
            return root.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    private static int CardsIn(string html) =>
        System.Text.RegularExpressions.Regex.Matches(html, "class=\"keepsake-card").Count;

    [Fact]
    public void TheRenderedShelfHasOneRowForThePendantAndExactlyOneForEachHeldSheet()
    {
        Assert.Equal(1, CardsIn(Render(Keepsake.Shelf([]))));
        Assert.Equal(2, CardsIn(Render(Keepsake.Shelf([Photo]))));
        Assert.Equal(3, CardsIn(Render(Keepsake.Shelf([Photo, Slip]))));
    }

    [Fact]
    public void ASheetsRowCarriesItsOwnTitleAndLine_AndAThemedPlaceholderOnTheSameArtBox()
    {
        string html = System.Net.WebUtility.HtmlDecode(Render(Keepsake.Shelf([Photo, Slip])));

        Assert.Contains(HeldMemory.RowTitle(Photo), html);
        Assert.Contains(Photo.BookLine, html);
        Assert.Contains(Slip.BookLine, html);
        Assert.Contains("keepsake-art closed art-love", html);
        Assert.Contains("keepsake-art closed art-money", html);
        Assert.Contains("keepsake-art closed art-unsettled", html); // the pendant keeps its own
        // a CSS placeholder only: no <img>, so a background-image can be dropped on the box later
        Assert.DoesNotContain("<img", html);
    }

    [Fact]
    public void ADozenSheetsLiveInACappedScrollRegion_ThePendantStaysPinnedOutsideIt_AndThePocketsAreStillThere()
    {
        HeldMemory.Sheet[] twelve = Enumerable.Range(0, 12)
            .Select(i => new HeldMemory.Sheet(HeldMemory.SlipId($"mate-{i}"), HeldMemory.Mark.His,
                i % 2 == 0 ? HeldMemory.Theory.Love : HeldMemory.Theory.Money, "a page", ["Somebody"], 86400.0 * i))
            .ToArray();
        string html = Render(Keepsake.Shelf(twelve));

        Assert.Equal(13, CardsIn(html));
        int region = html.IndexOf("class=\"keepsake-sheets\"", StringComparison.Ordinal);
        Assert.True(region > 0, "the sheet rows need their capped container.");
        int pendantCard = html.IndexOf(Keepsake.PendantTitle, StringComparison.Ordinal);
        Assert.True(pendantCard > 0 && pendantCard < region, "the pendant row sits above and outside the scroll region.");
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, "keepsake-art (closed|inside) art-unsettled"));
        string inRegion = html[region..];
        Assert.Equal(12, System.Text.RegularExpressions.Regex.Matches(inRegion, "class=\"keepsake-card").Count);
        Assert.DoesNotContain("art-unsettled", inRegion);

        // no sheets, no region (scroll only where needed)
        Assert.DoesNotContain("keepsake-sheets", Render(Keepsake.Shelf([])));

        // the cap is the CappedScrollPanel idiom, and the shelf still sits AFTER the carried pockets in the panel
        string css = File.ReadAllText(Path.Combine(SurfaceComposition.RepoRoot(), "src", "SpaceSails.Client",
            "Pages", "Map", "SatchelPanel.razor.css"));
        int rule = css.IndexOf(".keepsake-sheets {", StringComparison.Ordinal);
        Assert.True(rule > 0);
        string body = css[rule..css.IndexOf('}', rule)];
        Assert.Contains("max-height:", body);
        Assert.Contains("overflow-y: auto", body);
        Assert.Contains("min-height: 0", body);

        string panel = File.ReadAllText(Path.Combine(SurfaceComposition.RepoRoot(), "src", "SpaceSails.Client",
            "Pages", "Map", "SatchelPanel.razor"));
        int pockets = panel.IndexOf("<CarriedPockets", StringComparison.Ordinal);
        int shelf = panel.IndexOf("<KeepsakeShelf", StringComparison.Ordinal);
        Assert.True(pockets > 0 && shelf > pockets, "the pockets list is still in the panel, ahead of the shelf.");
    }

    [Fact]
    public void AnOpenSheetFoldsOpenWithItsLineAndAClose_AndARefusalSitsUnderItsOwnClosedCard()
    {
        IReadOnlyList<Keepsake.Piece> shelf = Keepsake.Shelf([Photo, Slip]);
        string line = Keepsake.MoneyStingPool[0];
        string open = System.Net.WebUtility.HtmlDecode(Render(shelf, HeldMemory.SlipId("fixer"), HeldMemory.SlipId("fixer"), line));
        Assert.Contains("keepsake-art inside art-money", open);
        Assert.Contains("keepsake-fold", open);
        Assert.Contains(line, open);
        Assert.Contains("Close", open); // nothing opens that cannot be closed

        string refused = System.Net.WebUtility.HtmlDecode(Render(shelf, null, HeldMemory.PhotographId, Keepsake.SatietyLine));
        Assert.Contains("keepsake-refusal", refused);
        Assert.DoesNotContain("keepsake-fold", refused);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(refused, "keepsake-refusal"));
    }
}
