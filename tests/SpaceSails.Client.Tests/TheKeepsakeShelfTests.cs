using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #620 slice 1 · THE KEEPSAKE SHELF, AT THE PLACE THE PRESS HAPPENS. Core pins the bands
/// (<c>KeepsakeTests</c>); these hold the two halves Core cannot see — the page that decides WHERE the captain
/// is standing and WHEN he last looked (the shipping <see cref="Map"/>, driven through its own
/// <c>PressKeepsake</c>, the method the shelf's button invokes), and the shelf component itself, RENDERED to
/// HTML so the line a captain reads is asserted in the slot the real panel draws it in.
///
/// <para>The worlds are not typed-in fields where every body builds nothing: the cabin is the deck's own
/// bounds (<see cref="DeckPlan.InCaptainsCabin"/>), the corridor is the deck's own corridor, and the sim
/// times are ones Core's tests know are clean draws.</para>
/// </summary>
public class TheKeepsakeShelfTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // The cabin's own floor (CABIN 1, x 11..14.5, y -10..-3) and the corridor outside it.
    private const double CabinX = 12.75, CabinY = -6.5;
    private const double CorridorX = 12.0, CorridorY = 0.6;

    private static object? Get(object o, string name) =>
        (o.GetType().GetField(name, Hidden) as object ?? o.GetType().GetProperty(name, Hidden))
            is { } m ? (m is FieldInfo f ? f.GetValue(o) : ((PropertyInfo)m).GetValue(o)) : throw new MissingMemberException(name);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        o.GetType().GetMethod(method, Hidden)!.Invoke(o, args);

    private static Map AMapAt(double x, double y, double nerve = 40.0, double simTime = 0.0)
    {
        var map = new Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", Hidden)!.SetValue(map, true);
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
        Set(map, "_nerve", nerve);
        Set(map, "SimTime", simTime);
        Set(map, "_showSatchel", true);
        return map;
    }

    private static void Press(Map map) => Invoke(map, "PressKeepsake", Keepsake.Pendant);
    private static string? Said(Map map) => (string?)Get(map, "_keepsakeSaid");
    private static bool Folded(Map map) => Get(map, "_keepsakeOpenId") is not null;
    private static double Nerve(Map map) => (double)Get(map, "_nerve")!;
    private static bool Filed(Map map) =>
        ((IEnumerable<FieldNote>)Get(map, "_fieldNotes")!).Any(n => n.Text == Keepsake.FieldBookLine);

    // ── THE CABIN IS THE DECK'S OWN CABIN ─────────────────────────────────────────────────────────────

    [Fact]
    public void TheCabinIsTheBunksBerthAndNotTheCorridorOutsideIt()
    {
        Assert.True(DeckPlan.InCaptainsCabin(CabinX, CabinY));
        Assert.False(DeckPlan.InCaptainsCabin(CorridorX, CorridorY));
        Assert.False(DeckPlan.InCaptainsCabin(9.25, CabinY));  // CABIN 2 is not the captain's
        Assert.False(DeckPlan.InCaptainsCabin(16.0, CabinY));  // the head is not
        // …and the bunk console really does stand inside it (the berth the comment names).
        Assert.Contains(DeckPlan.Ship.Consoles, c => c.Kind == DeckPlan.ConsoleKind.Bunk
            && DeckPlan.InCaptainsCabin(c.X, c.Y));
    }

    // ── THE PRESS, IN THE CABIN ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheFirstOpeningInTheCabinFilesTheFieldBookLineOnce_SteadiesTheCaptain_AndShutsThePocketOnTheCard()
    {
        Map map = AMapAt(CabinX, CabinY, nerve: 40.0);
        Press(map);

        Assert.True(Filed(map), "the first opening files the pendant's field-book line.");
        Assert.True(Nerve(map) >= 40.0 + 20.0, $"the minute should steady the captain (nerve {Nerve(map)}).");
        Assert.False((bool)Get(map, "_showSatchel")!, "the pocket goes back so the card is seen.");
        Assert.Null(Said(map));      // the card's caption IS the line; the keepsake card has nothing to repeat
        // U09-F02 (Fable ruling 2026-11-07) · it is a CARD the player closes, not a plate that fades unread.
        Assert.Null(Get(map, "_storyPlate"));
        object card = Get(map, "_storyCard") ?? throw new Xunit.Sdk.XunitException("the first opening raised no card.");
        Assert.Equal(StoryBeats.Beat.KeepsakeFirstOpening, card.GetType().GetField("Item1")!.GetValue(card));
        Assert.Equal(Keepsake.PendantSubject, card.GetType().GetField("Item2")!.GetValue(card));
        Assert.Equal(FilingLine.Mark + " A PAGE THAT WAS NEVER WRITTEN",
            StoryBeats.Title((StoryBeats.Beat)card.GetType().GetField("Item1")!.GetValue(card)!, (string?)card.GetType().GetField("Item2")!.GetValue(card)));
        Assert.Equal(Keepsake.FirstOpeningLine,
            StoryBeats.Caption((StoryBeats.Beat)card.GetType().GetField("Item1")!.GetValue(card)!, (string?)card.GetType().GetField("Item2")!.GetValue(card)));
        Assert.False(Folded(map));
        Assert.Equal(0.0, (double)Get(map, "_lastQuietMinuteSimTime")!, 6); // the shared window is stamped
        Assert.Equal(1, ((IEnumerable<FieldNote>)Get(map, "_fieldNotes")!).Count(n => n.Text == Keepsake.FieldBookLine));
    }

    [Fact]
    public void ALaterOpeningFoldsTheCardOpenWithACanonLine_AndNeverFilesOrRaisesTheFirstAgain()
    {
        Map map = AMapAt(CabinX, CabinY, nerve: 40.0, simTime: 0.0);
        Press(map);                                           // the first opening
        Set(map, "_showSatchel", true);
        Set(map, "SimTime", Keepsake.QuietWindowSeconds + 4.0); // past the window (either draw, clean or stung, is a canon line)
        Press(map);

        string? said = Said(map);
        Assert.NotNull(said);
        Assert.True(Keepsake.CleanPool.Contains(said!) || Keepsake.StingPool.Contains(said!), $"not a canon line: {said}");
        Assert.True(Folded(map), "a minute actually spent folds the card open on its picture.");
        Assert.True((bool)Get(map, "_showSatchel")!, "a later opening leaves the pocket up.");
        Assert.Equal(1, ((IEnumerable<FieldNote>)Get(map, "_fieldNotes")!).Count(n => n.Text == Keepsake.FieldBookLine));
    }

    [Fact]
    public void TheFirstOpeningIsOncePerRun_EvenWhenALongRunTrimsTheFieldBookLineAway()
    {
        Map map = AMapAt(CabinX, CabinY, nerve: 40.0, simTime: 0.0);
        Press(map);
        Assert.True((bool)Get(map, "_pendantFirstOpened")!);

        // A long run: the book's cap trims its front, the pendant's line with it.
        for (int i = 0; i < FieldNotes.Cap + 5; i++)
        {
            Invoke(map, "FileNote", $"a long run's note {i}", "x");
        }
        Assert.False(Filed(map), "the cap should have trimmed the pendant's line off the front of the book.");

        Set(map, "_showSatchel", true);
        Set(map, "_storyCard", null);
        Set(map, "SimTime", Keepsake.QuietWindowSeconds + 4.0);
        Press(map);

        Assert.False(Filed(map), "a second field-book line was filed — the first opening replayed.");
        Assert.Null(Get(map, "_storyCard")); // no second first-opening card
        Assert.True(Folded(map));
        string said = Said(map)!;
        Assert.True(Keepsake.CleanPool.Contains(said) || Keepsake.StingPool.Contains(said));
    }

    [Fact]
    public void TheLatchRidesTheVaultAndAFreshUniverseClearsIt()
    {
        Map map = AMapAt(CabinX, CabinY);
        Press(map);
        Vault vault = (Vault)Invoke(map, "BuildVault", "", "")!;
        Assert.True(vault.Nerve!.PendantFirstOpened);

        Invoke(map, "ResetLiveStateForNewGame");
        Assert.False((bool)Get(map, "_pendantFirstOpened")!);
        Assert.Equal(double.NegativeInfinity, (double)Get(map, "_lastQuietMinuteSimTime")!);
    }

    [Fact]
    public void TheWindowIsOneForTheWholeShelf_AndTheRefusalIsTheCanonLine_WithNothingRestored()
    {
        Map map = AMapAt(CabinX, CabinY, nerve: 40.0, simTime: 0.0);
        Press(map); // first opening spends the window
        Set(map, "_showSatchel", true);
        double before = Nerve(map);
        Set(map, "SimTime", 600.0); // ten sim-minutes later: inside the window

        Press(map);

        Assert.Equal(Keepsake.SatietyLine, Said(map));
        Assert.False(Folded(map), "a refusal never folds the card open.");
        Assert.Equal(before, Nerve(map), 6);
        Assert.Equal(0.0, (double)Get(map, "_lastQuietMinuteSimTime")!, 6); // a refusal does not re-stamp the window
    }

    // ── THE PRESS, ELSEWHERE ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OutsideTheCabinTheLocketDoesNotOpen_NothingIsRestored_NothingIsFiled_NothingIsStamped()
    {
        Map map = AMapAt(CorridorX, CorridorY, nerve: 40.0);
        Press(map);

        Assert.Contains(Said(map)!, Keepsake.NotHerePool);
        Assert.False(Folded(map));
        Assert.Equal(40.0, Nerve(map), 6);
        Assert.False(Filed(map));
        Assert.Equal(double.NegativeInfinity, (double)Get(map, "_lastQuietMinuteSimTime")!);
        Assert.True((bool)Get(map, "_showSatchel")!, "a refusal leaves the pocket as it was.");
    }

    [Fact]
    public void OnOpenGroundTheRefusalNeverSaysTheConcourseIsWatching_AtAnySimTime()
    {
        // U09-F12 · the refusal is picked by place, not by roll: sweep the clock so both pool lines would turn up.
        for (int t = 0; t < 40; t++)
        {
            Map ashore = AMapAt(CabinX, CabinY);
            Set(ashore, "SimTime", (double)t);
            Set(ashore, "_surface", Activator.CreateInstance(
                typeof(Map).GetNestedType("SurfaceExcursion", Hidden)!, nonPublic: true));
            Press(ashore);
            Assert.DoesNotContain("concourse", Said(ashore)!);
            Assert.Contains(Said(ashore)!, Keepsake.NotHerePool);
        }
    }

    [Fact]
    public void AboardShipOutsideTheCabinTheRefusalNeverSaysTheConcourseIsWatching_AtAnySimTime()
    {
        // U09-F12 (#1460 inspection) · aboard her own ship in flight there is no concourse to watch:
        // the satchel opens anywhere on foot (U01-F05), so the place test must ask whether she is ashore.
        bool sawConcourseAshore = false;
        for (int t = 0; t < 40; t++)
        {
            Map aboard = AMapAt(0.0, 0.0);
            Set(aboard, "SimTime", (double)t);
            Press(aboard);
            Assert.DoesNotContain("concourse", Said(aboard)!);
            Assert.Contains(Said(aboard)!, Keepsake.NotHerePool);

            Map haven = AMapAt(0.0, 0.0);
            Set(haven, "SimTime", (double)t);
            Set(haven, "_ashore", true);
            Press(haven);
            sawConcourseAshore |= Said(haven)!.Contains("concourse");
        }
        Assert.True(sawConcourseAshore, "on a haven floor the concourse line must still be in the roll.");
    }

    [Fact]
    public void OnAHavenFloorOrASurfaceEvenAtTheCabinsCoordinatesTheLocketDoesNotOpen()
    {
        Map below = AMapAt(CabinX, CabinY);
        Set(below, "_havenFloor", HavenLevels.ServiceLevel);
        Press(below);
        Assert.Contains(Said(below)!, Keepsake.NotHerePool);

        Map ashore = AMapAt(CabinX, CabinY);
        Set(ashore, "_surface", Activator.CreateInstance(
            typeof(Map).GetNestedType("SurfaceExcursion", Hidden)!, nonPublic: true));
        Press(ashore);
        Assert.Contains(Said(ashore)!, Keepsake.NotHerePool);
    }

    [Fact]
    public void TheFoldClosesWithItsOwnButtonAndTheLidShutsIt()
    {
        Map map = AMapAt(CabinX, CabinY, simTime: 0.0);
        Press(map);
        Set(map, "_showSatchel", true);
        Set(map, "SimTime", Keepsake.QuietWindowSeconds + 4.0);
        Press(map);
        Assert.True(Folded(map));

        Invoke(map, "FoldKeepsake");
        Assert.False(Folded(map));
        Assert.Null(Said(map));

        Press(map); // sated now (window re-stamped) → refusal under the card; the lid wipes it
        Invoke(map, "CloseSatchel");
        Assert.Null(Said(map));
        Assert.Null(Get(map, "_keepsakeAskedId"));
    }

    // ── THE SHELF, RENDERED ───────────────────────────────────────────────────────────────────────────

    private static string Render(string? openId, string? askedId, string? said)
    {
        var services = new ServiceCollection().AddSingleton<ILoggerFactory, NullLoggerFactory>().BuildServiceProvider();
        using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<KeepsakeShelf>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(KeepsakeShelf.KeepsakePieces)] = (Func<IReadOnlyList<Keepsake.Piece>>)Keepsake.Shelf,
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
    public void TheClosedShelfDrawsThePendantAsAPictureWithItsCardAndNoFold()
    {
        string html = Render(null, null, null);
        Assert.Contains("keepsake-art closed", html);
        Assert.Contains(Keepsake.PendantTitle, html);
        Assert.Contains(Keepsake.PendantCardLine, html);
        Assert.DoesNotContain("keepsake-fold", html);
        Assert.DoesNotContain("keepsake-said", html);
    }

    [Fact]
    public void TheOpenCardShowsThePictureInsideAndOneLine_AndACloseControl()
    {
        string line = Keepsake.CleanPool[0];
        string html = Render(Keepsake.PendantId, Keepsake.PendantId, line);
        Assert.Contains("keepsake-art inside", html);
        Assert.Contains("keepsake-fold", html);
        Assert.Contains($"<div class=\"keepsake-said\"", html);
        Assert.Contains(line, System.Net.WebUtility.HtmlDecode(html));
        Assert.Contains("Close", html); // nothing opens that cannot be closed
    }

    [Fact]
    public void ARefusalIsDrawnUnderItsOwnClosedCard_NotInsideAFold()
    {
        string html = Render(null, Keepsake.PendantId, Keepsake.SatietyLine);
        Assert.Contains("keepsake-refusal", html);
        Assert.Contains(Keepsake.SatietyLine, System.Net.WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("keepsake-fold", html);
        Assert.Contains("keepsake-art closed", html);

        // …and a line asked of some OTHER piece is not drawn under this one.
        Assert.DoesNotContain("keepsake-refusal", Render(null, "some-other-piece", Keepsake.SatietyLine));
    }
}
