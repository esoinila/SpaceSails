using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1386 · THE BERTH THAT "ACKNOWLEDGED THE RUN" HAS SETTLED IT.
///
/// <para>The cycler lets the ship go 10,000 km off the ice on the moon's own track; Enceladus's Hill sphere is
/// under a thousand, so the moon-haven rule read her as bound to Saturn and the KAAMOS supply run stayed
/// <c>On the hook</c>. The settle that <c>RideTheCyclerWindow</c> now runs after the re-seed is the ordinary
/// <c>CompleteBoundCargoRunQuests</c>; this boots the dev seat that leaves her exactly where the ride does
/// (<c>BerthState.CoOrbital</c> at <c>ArrivalOffsetMeters</c>, run filed and Active) and runs that settle.</para>
/// </summary>
public sealed class TheSupplyRunSettlesAlongsideTheIceTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static async Task<PastTheGateBench.Boot> Alongside()
    {
        PastTheGateBench.Boot b = await PastTheGateBench.BootAsync("/map?kaamos=hq");
        Assert.Null(b.Threw);
        return b;
    }

    private static List<Map.Quest> Quests(PastTheGateBench.Boot b) => b.Read<List<Map.Quest>>("_quests");

    private static void Settle(PastTheGateBench.Boot b) =>
        typeof(Map).GetMethod("CompleteBoundCargoRunQuests", Hidden)!.Invoke(b.Page, null);

    [Fact]
    public async Task TheRunLetGoAlongsideTheIceIsDeliveredAndPaid()
    {
        PastTheGateBench.Boot b = await Alongside();
        Map.Quest run = Quests(b).Single(q => q.Id == KaamosLore.SupplyRunQuestId);
        Assert.Equal(Map.QuestState.Active, run.State);
        int credits = b.Read<int>("_credits");

        Settle(b);

        Assert.Equal(Map.QuestState.TurnedIn, Quests(b).Single(q => q.Id == KaamosLore.SupplyRunQuestId).State);
        Assert.Equal(credits + run.Reward, b.Read<int>("_credits"));
    }

    [Fact]
    public async Task AnOrdinaryRunToTheIceMoonDoesNotGetTheWiderReach()
    {
        PastTheGateBench.Boot b = await Alongside();
        List<Map.Quest> quests = Quests(b);
        Map.Quest supply = quests.Single(q => q.Id == KaamosLore.SupplyRunQuestId);
        quests.Remove(supply);
        quests.Add(supply with { Id = "ordinary-run-to-the-ice" });

        Settle(b);

        Assert.Equal(Map.QuestState.Active, Quests(b).Single(q => q.Id == "ordinary-run-to-the-ice").State);
    }

    [Fact]
    public async Task AFlybyThroughTheSameSphereDoesNotSettleTheRun()
    {
        PastTheGateBench.Boot b = await Alongside();
        var ship = b.Read<ShipState>("_ship");
        typeof(Map).GetField("_ship", Hidden)!.SetValue(b.Page, ship with { Velocity = ship.Velocity + new Vector2d(0, 5000.0) });

        Settle(b);

        Assert.Equal(Map.QuestState.Active, Quests(b).Single(q => q.Id == KaamosLore.SupplyRunQuestId).State);
    }

    [Fact]
    public async Task TheRunFarFromTheIceIsNotDelivered()
    {
        PastTheGateBench.Boot b = await Alongside();
        var ship = b.Read<ShipState>("_ship");
        typeof(Map).GetField("_ship", Hidden)!.SetValue(b.Page, ship with { Position = ship.Position + new Vector2d(CyclerWindow.ArrivalOffsetMeters * 2.0, 0) });

        Settle(b);

        Assert.Equal(Map.QuestState.Active, Quests(b).Single(q => q.Id == KaamosLore.SupplyRunQuestId).State);
    }

    private static string Said(PastTheGateBench.Boot b) =>
        b.Read<object>("_pulse") is { } p ? (string?)p.GetType().GetProperty("Message")!.GetValue(p) ?? "" : "";

    [Fact]
    public async Task TheBerthSignsForTheConsignmentInItsOwnVoiceAndNeverOnTheCounter()
    {
        PastTheGateBench.Boot b = await Alongside();
        Map.Quest run = Quests(b).Single(q => q.Id == KaamosLore.SupplyRunQuestId);

        Settle(b);

        string said = Said(b);
        Assert.Equal(KaamosLore.SupplyRunSettledLine(run.Reward), said);
        Assert.Contains("The berth signs for the consignment", said);
        Assert.Contains($"{run.Reward:N0} cr", said);
        Assert.DoesNotContain("on the counter", said);
    }

    [Fact]
    public async Task AnOrdinaryMoonHavenRunStillSaysOnTheCounter()
    {
        PastTheGateBench.Boot b = await Alongside();
        List<Map.Quest> quests = Quests(b);
        Map.Quest supply = quests.Single(q => q.Id == KaamosLore.SupplyRunQuestId);
        quests.Remove(supply);
        quests.Add(supply with { Id = "ordinary-run-to-the-ice" });
        // Park her genuinely inside the moon's Hill sphere (100 km off, co-moving) so the ORDINARY rule settles it.
        var ship = b.Read<ShipState>("_ship");
        var eph = b.Read<ICelestialEphemeris>("_ephemeris");
        double t = b.Read<double>("SimTime");
        typeof(Map).GetField("_ship", Hidden)!.SetValue(b.Page, ship with
        {
            Position = eph.Position(KaamosLore.IceMoonBodyId, t) + new Vector2d(100_000.0, 0),
            Velocity = TransferMath.BodyVelocity(eph, KaamosLore.IceMoonBodyId, t),
        });

        Settle(b);

        Assert.Equal(Map.QuestState.TurnedIn, Quests(b).Single(q => q.Id == "ordinary-run-to-the-ice").State);
        string said = Said(b);
        Assert.Contains("on the counter", said);
        Assert.DoesNotContain("berth signs", said);
    }

    [Fact]
    public async Task TheLedgerReadsFilledOnceTheRunIsSignedForAlongside()
    {
        PastTheGateBench.Boot b = await Alongside();
        var kaamos = b.Read<KaamosProgress>("_kaamos");
        string before = KaamosLore.LedgerProgressLine(kaamos, 10 * 86_400.0, alongside: true, runFiled: true, runSignedFor: false);
        Assert.DoesNotContain("FILLED", before);

        Settle(b);

        object tip = typeof(Map).GetMethod("KaamosLedgerTip", Hidden)!.Invoke(b.Page, null)!;
        string[] lines = (string[])tip.GetType().GetProperty("Lines")!.GetValue(tip)!;
        Assert.Contains(lines, l => l.Contains("The listing reads FILLED.", StringComparison.Ordinal));
        Assert.Contains("run is signed for", KaamosLore.LedgerProgressLine(kaamos, 10 * 86_400.0, alongside: true, runFiled: false, runSignedFor: true));
        // earlier branches unchanged
        Assert.Equal(before, KaamosLore.LedgerProgressLine(kaamos, 10 * 86_400.0, alongside: true));
        Assert.Contains("opens in 30 d", KaamosLore.LedgerProgressLine(kaamos, 10 * 86_400.0, alongside: false, runFiled: true, runSignedFor: true));
    }
}
