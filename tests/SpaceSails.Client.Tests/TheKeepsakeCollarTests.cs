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
/// #620 slice 3 · THE COLLAR ON THE SHELF, AT THE PLACE THE PRESS HAPPENS. Core pins the find's bands
/// (<c>KeepsakeFindTests</c>); these hold the page's half: the shelf the page hands the component is derived from
/// the held satchel (put the collar down and the row goes), a press on the collar in the cabin spends the shared
/// window without ever touching the pendant's first-opening latch or raising a plate, and the shelf component,
/// RENDERED, draws the row with the #614 title and the find's own art class. The #614 lens card is untouched.
/// </summary>
public class TheKeepsakeCollarTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const double CabinX = 12.75, CabinY = -6.5;
    private static readonly Satchel.Item CollarItem = new(Satchel.Kind.Relic, "hive:luna:-3:7");

    private static object? Get(object o, string name) =>
        (o.GetType().GetField(name, Hidden) as object ?? o.GetType().GetProperty(name, Hidden))
            is { } m ? (m is FieldInfo f ? f.GetValue(o) : ((PropertyInfo)m).GetValue(o)) : throw new MissingMemberException(name);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);

    private static Map AMapHoldingTheCollar(double simTime = 0.0, double nerve = 40.0)
    {
        var map = new Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", Hidden)!.SetValue(map, true);
        Set(map, "_avatarX", CabinX);
        Set(map, "_avatarY", CabinY);
        Set(map, "_nerve", nerve);
        Set(map, "SimTime", simTime);
        Set(map, "_showSatchel", true);
        Set(map, "_satchel", new List<Satchel.Item> { CollarItem });
        return map;
    }

    private static string? Said(Map map) => (string?)Get(map, "_keepsakeSaid");
    private static double Nerve(Map map) => (double)Get(map, "_nerve")!;
    private static IReadOnlyList<Keepsake.Piece> Pieces(Map map) => (IReadOnlyList<Keepsake.Piece>)Invoke(map, "KeepsakePieces")!;

    [Fact]
    public void ThePagesShelfCarriesTheCollarWhileHeld_AndLosesItWhenItIsPutDown()
    {
        Map map = AMapHoldingTheCollar();
        Assert.Equal([Keepsake.PendantId, Keepsake.CollarId], Pieces(map).Select(p => p.Id));

        Set(map, "_satchel", new List<Satchel.Item>());
        Assert.Equal([Keepsake.PendantId], Pieces(map).Select(p => p.Id));
    }

    [Fact]
    public void PressingTheCollarInTheCabin_RestoresCleanAtTwentyTwo_EvenAtAKnownStingTime_AndNeverTouchesThePendantsLatchOrRaisesAPlate()
    {
        Map map = AMapHoldingTheCollar(simTime: 38.0); // sim-time 38: the band fires for the pendant and a Money sheet
        Invoke(map, "PressKeepsake", Keepsake.Collar);

        Assert.Contains(Said(map)!, Keepsake.FindCleanPool);
        // the page applies the answer exactly as it does for every other piece (ApplyNerveRelief): a pendant
        // press on a twin map restores by the same amount, and a STING here would have LOWERED the gauge.
        Map twin = AMapHoldingTheCollar(simTime: 0.0);
        Invoke(twin, "PressKeepsake", Keepsake.Pendant);
        Assert.True(Nerve(map) > 40.0, $"the collar should steady the captain even on a sting roll (nerve {Nerve(map)}).");
        Assert.Equal(Nerve(twin), Nerve(map), 6);
        Assert.NotNull(Get(map, "_keepsakeOpenId")); // a minute spent folds the card open
        Assert.False((bool)Get(map, "_pendantFirstOpened")!, "a find never spends the pendant's first opening.");
        Assert.Null(Get(map, "_storyPlate"));
        Assert.True((bool)Get(map, "_showSatchel")!, "no flashback, so the pocket stays up.");
        Assert.Equal(38.0, (double)Get(map, "_lastQuietMinuteSimTime")!, 6); // the one shared window is stamped
    }

    [Fact]
    public void TheWindowIsSharedBothWays_CollarThenPendant_AndPendantThenCollar()
    {
        Map map = AMapHoldingTheCollar(simTime: 0.0);
        Invoke(map, "PressKeepsake", Keepsake.Collar);
        Set(map, "SimTime", 600.0);
        Invoke(map, "PressKeepsake", Keepsake.Pendant);
        Assert.Equal(Keepsake.SatietyLine, Said(map));
        Assert.False((bool)Get(map, "_pendantFirstOpened")!, "a refusal does not spend the first opening.");

        Map other = AMapHoldingTheCollar(simTime: 0.0);
        Invoke(other, "PressKeepsake", Keepsake.Pendant); // the first opening spends the window
        Set(other, "_showSatchel", true);
        Set(other, "SimTime", 600.0);
        Invoke(other, "PressKeepsake", Keepsake.Collar);
        Assert.Equal(Keepsake.SatietyLine, Said(other));
    }

    [Fact]
    public void OutsideTheCabinTheCollarDoesNotOpen()
    {
        Map map = AMapHoldingTheCollar();
        Set(map, "_avatarX", 12.0);
        Set(map, "_avatarY", 0.6);
        Invoke(map, "PressKeepsake", Keepsake.Collar);
        Assert.Contains(Said(map)!, Keepsake.NotHerePool);
        Assert.Equal(40.0, Nerve(map), 6);
    }

    [Fact]
    public void TheNumberFourteenLensCardIsUntouched_TheShelfIsAdditive()
    {
        CarriedObject.Reveal? card = CarriedObject.Card(CollarItem, "luna");
        Assert.NotNull(card);
        Assert.Equal(CarriedObject.CollarLabel, card!.Value.Label);
        Assert.Equal(CarriedObject.CollarStory, card.Value.Story);
        Assert.Equal(CarriedObject.CollarArtUrl, card.Value.ArtUrl);
    }

    private static string Render(string? openId, string? askedId, string? said)
    {
        var services = new ServiceCollection().AddSingleton<ILoggerFactory, NullLoggerFactory>().BuildServiceProvider();
        using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<KeepsakeShelf>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(KeepsakeShelf.KeepsakePieces)] = (Func<IReadOnlyList<Keepsake.Piece>>)(() => Keepsake.Shelf([], [CollarItem])),
                    [nameof(KeepsakeShelf.KeepsakeOpenId)] = openId,
                    [nameof(KeepsakeShelf.KeepsakeAskedId)] = askedId,
                    [nameof(KeepsakeShelf.KeepsakeSaid)] = said,
                    [nameof(KeepsakeShelf.PressKeepsake)] = (Action<Keepsake.Piece>)(_ => { }),
                    [nameof(KeepsakeShelf.FoldKeepsake)] = (Action)(() => { }),
                }));
            return root.ToHtmlString();
        }).GetAwaiter().GetResult();
    }

    [Fact]
    public void TheClosedShelfDrawsTheCollarRowWithItsNumberFourteenTitleAndTheFindsArt_PinnedAboveTheSheets()
    {
        string html = System.Net.WebUtility.HtmlDecode(Render(null, null, null));
        Assert.Contains(Keepsake.PendantTitle, html);
        Assert.Contains(CarriedObject.CollarLabel, html);
        Assert.Contains(Keepsake.Collar.CardLine, html);
        Assert.Contains("keepsake-art closed art-find", html);
        Assert.DoesNotContain("keepsake-sheets", html); // a find is pinned beside the pendant, not in the sheet scroller
    }

    [Fact]
    public void TheOpenCollarShowsItsInsideArtAndOneCanonLine_AndACloseControl()
    {
        string line = Keepsake.FindCleanPool[1];
        string html = Render(Keepsake.CollarId, Keepsake.CollarId, line);
        Assert.Contains("keepsake-art inside art-find", html);
        Assert.Contains(line, System.Net.WebUtility.HtmlDecode(html));
        Assert.Contains("Close", html);
    }
}
