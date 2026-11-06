using System;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1405 R1-F45 / R1-F10 · ONCE THE HULL IS ALONGSIDE THE ICE THERE IS NO RIDE LEFT TO TAKE, AND THE LEDGER KNOWS.
///
/// <para>The cycler row stayed on Enceladus's menu with the hull already keeping station with the ice (it
/// re-armed at the next window; on <c>?kaamos=hq</c> it silently burned 38 more days). The ledger's resting
/// line said the window was "not yet open" whatever the clock. This boots the head-office seat (hull let go
/// alongside) and asks the page both questions, then moves the hull away and asks again.</para>
/// </summary>
public sealed class TheCyclerRideStopsOnceAlongsideTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static bool RowVisible(PastTheGateBench.Boot b) =>
        (bool)typeof(Map).GetMethod("KaamosCyclerRowVisible", Hidden)!.Invoke(b.Page, [KaamosLore.IceMoonBodyId])!;

    [Fact]
    public async Task AlongsideTheIceTheRideIsNotOnTheMenuAndAwayFromItItIs()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?kaamos=hq");
        Assert.Null(b.Threw);

        Assert.False(RowVisible(b), "the hull is alongside the ice and the ride is still offered");

        var ship = b.Read<ShipState>("_ship");
        typeof(Map).GetField("_ship", Hidden)!.SetValue(b.Page,
            ship with { Position = ship.Position + new Vector2d(CyclerWindow.ArrivalOffsetMeters * 20.0, 0) });
        Assert.True(RowVisible(b), "the world cannot tell alongside from away: the row never shows");
    }

    [Fact]
    public async Task TheLedgerRestingLineFollowsTheWindowAndTheBerth()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?kaamos=hq");
        Assert.Null(b.Threw);
        var kaamos = b.Read<KaamosProgress>("_kaamos");

        string shut = KaamosLore.LedgerProgressLine(kaamos, 10 * 86_400.0, alongside: false);
        string open = KaamosLore.LedgerProgressLine(kaamos, CyclerWindow.PeriodSeconds + 3600.0, alongside: false);
        string along = KaamosLore.LedgerProgressLine(kaamos, 10 * 86_400.0, alongside: true);

        Assert.Contains("opens in 30 d", shut);
        Assert.DoesNotContain("not yet open", shut);
        Assert.Contains("Put her on it at the ice.", open);
        Assert.Contains("She is alongside the ice.", along);

        // ...and the page really hands the ledger the live clock and berth (hq boots alongside; day 0).
        object tip = typeof(Map).GetMethod("KaamosLedgerTip", Hidden)!.Invoke(b.Page, null)!;
        string[] lines = (string[])tip.GetType().GetProperty("Lines")!.GetValue(tip)!;
        Assert.Contains(lines, l => l.Contains("She is alongside the ice", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.Contains("not yet open", StringComparison.Ordinal));

        // An open window with NO run filed must not say go: the ride would refuse (NoRunFiled).
        string openUnfiled = KaamosLore.LedgerProgressLine(
            kaamos, CyclerWindow.PeriodSeconds + 3600.0, alongside: false, runFiled: false);
        Assert.DoesNotContain("Put her on it", openUnfiled);
        Assert.Equal(KaamosLore.ReachLedgerLine, openUnfiled);

        Assert.Equal(3, new[] { shut, open, along }.Distinct().Count());
    }
}

/// <summary>#1405 R1-F44 · THE KAAMOS RUN QUOTES NO LANE ESTIMATE. The cycler carries it; "~6.5 y by the
/// lanes" beside "the arc does the flying" was a trip nobody makes. Measured away from the ice so a lane
/// figure WOULD exist for an ordinary run (the control is the same quest under an ordinary id).</summary>
public sealed class TheKaamosRunQuotesNoLaneEstimateTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [Fact]
    public async Task TheSupplyRunPrintsNoLaneTimeWhereAnOrdinaryRunToTheSameBerthDoes()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?kaamos=hq");
        Assert.Null(b.Threw);
        var ship = b.Read<ShipState>("_ship");
        typeof(Map).GetField("_ship", Hidden)!.SetValue(b.Page,
            ship with { Position = ship.Position + new Vector2d(5.0e9, 0) });

        var quests = b.Read<System.Collections.Generic.List<Map.Quest>>("_quests");
        Map.Quest run = System.Linq.Enumerable.Single(quests, q => q.Id == KaamosLore.SupplyRunQuestId);
        MethodInfo facts = typeof(Map).GetMethod("JobFactsFor", Hidden)!;

        var ordinary = (JobFacts)facts.Invoke(b.Page, [run with { Id = "ordinary-run-to-the-ice" }])!;
        var kaamos = (JobFacts)facts.Invoke(b.Page, [run])!;

        Assert.Contains("by the lanes", JobTerms.EffortLine(ordinary));   // the premise: the world can tell
        Assert.DoesNotContain("by the lanes", JobTerms.EffortLine(kaamos));
        Assert.Contains("≈", JobTerms.EffortLine(kaamos));
    }
}

/// <summary>#1405 · Cast-off clears the pass projected while clamped; the alarm is handed the dock.</summary>
public sealed class TheCastOffDropsTheDockedPassTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [Fact]
    public async Task UndockClearsTheClosestPassAndDirtiesTheProjection()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?dock=ringside-exchange");
        Assert.Null(b.Threw);
        Assert.NotNull(typeof(Map).GetField("_dockedHavenId", Hidden)!.GetValue(b.Page));

        typeof(Map).GetField("_closestPass", Hidden)!.SetValue(b.Page,
            new ClosestApproach.Pass("x", "x", 1000, 10, 0, default));
        typeof(Map).GetField("_passDirty", Hidden)!.SetValue(b.Page, false);

        typeof(Map).GetMethod("Undock", Hidden)!.Invoke(b.Page, null);

        Assert.Null(typeof(Map).GetField("_dockedHavenId", Hidden)!.GetValue(b.Page));
        Assert.Null(typeof(Map).GetField("_closestPass", Hidden)!.GetValue(b.Page));
        Assert.True((bool)typeof(Map).GetField("_passDirty", Hidden)!.GetValue(b.Page)!);
    }

    [Fact]
    public async Task ADockedShipWithAnImpactPassRaisesNoCollisionAlert()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?dock=ringside-exchange");
        Assert.Null(b.Threw);
        typeof(Map).GetField("_closestPass", Hidden)!.SetValue(b.Page,
            new ClosestApproach.Pass("The Rusty Roadstead", "x", 1000, 10, 0, default));

        typeof(Map).GetMethod("UpdateShipAlerts", Hidden)!.Invoke(b.Page, [0.0]);

        var alerts = (ShipAlerts)typeof(Map).GetField("_shipAlerts", Hidden)!.GetValue(b.Page)!;
        Assert.False(alerts.IsActive(AlertKind.Collision));
    }
}
