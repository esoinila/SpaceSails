using System.Collections.Generic;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// The run-scoped counters and told-once latches: page state that is no vault field, so the BuildVault audit
/// (#1374/#1376) never saw it. Each survived a New voyage or a Load/Import in the same tab and then lied on a card
/// (a first ride never told, a climax that never fires, a signing sheet never filed, a clinic ledger "woken here
/// before" on a first death). One door each way: <c>ResetLiveStateForNewGame</c> and <c>ApplyVault</c>.
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheRunCountersDoNotOutliveTheLifeTests
{
    public static IEnumerable<object[]> Doors() => [["ResetLiveStateForNewGame"], ["ApplyVault"]];

    private static void Pass(Pages.Map map, string door)
    {
        if (door == "ApplyVault") { Invoke(map, door, new Vault()); }
        else { Invoke(map, door); }
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void TheToldOnceLinesAreOwedAgainToTheNextLife(string door)
    {
        Pages.Map map = Boot("run-toldonce-" + door);
        string first = ToldOnce.Key(ToldOnce.OfficeAjar, "cinder-roost");
        ((ToldOnce)Read(map, "_toldOnce")!).Tell(first);
        Assert.True(((ToldOnce)Read(map, "_toldOnce")!).Has(first));

        Pass(map, door);

        Assert.False(((ToldOnce)Read(map, "_toldOnce")!).Has(first));
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void TheRebirthHeadOfficeAndPosterCountersStartOverWithTheLife(string door)
    {
        Pages.Map map = Boot("run-counters-" + door);
        Set(map, "_rebirthsSeen", 2);
        Set(map, "_insurancePosterReads", 2);
        Set(map, "_heatInsuranceAdvised", true);
        Set(map, "_lastQuietMinuteSimTime", 5.0);
        ((HashSet<int>)Read(map, "_headOfficeBeatsSeen")!).Add(3);

        Pass(map, door);

        Assert.Equal(0, Read(map, "_rebirthsSeen"));
        Assert.Equal(0, Read(map, "_insurancePosterReads"));
        Assert.False((bool)Read(map, "_heatInsuranceAdvised")!);
        Assert.Equal(double.NegativeInfinity, Read(map, "_lastQuietMinuteSimTime"));
        Assert.Empty((HashSet<int>)Read(map, "_headOfficeBeatsSeen")!);
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void TheLootLedgerAndTheCrackedHatchesDoNotRideIntoTheNextLife(string door)
    {
        Pages.Map map = Boot("run-ledger-" + door);
        ((List<LootRecord>)Read(map, "_lootLedger")!).Add(LootRecord.ForHaul("He3", 4, "Larkspur", "Mars", 10));
        ((HashSet<string>)Read(map, "_unlockedHatches")!).Add("the-tilt:back");

        Pass(map, door);

        Assert.Empty((List<LootRecord>)Read(map, "_lootLedger")!);
        Assert.Empty((HashSet<string>)Read(map, "_unlockedHatches")!);
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void TheRepsLatchesKeyedOnTheBareLifeNumberStartOverWithTheLife(string door)
    {
        Pages.Map map = Boot("run-rep-" + door);
        Set(map, "_repSigningToldInLife", 1);
        Set(map, "_posterReadInLife", 1);
        Set(map, "_repMeetings", 4);
        Set(map, "_repVisitIndex", 6);

        Pass(map, door);

        Assert.Equal(0, Read(map, "_repSigningToldInLife"));
        Assert.Equal(0, Read(map, "_posterReadInLife"));
        Assert.Equal(0, Read(map, "_repMeetings"));
        Assert.Equal(-1, Read(map, "_repVisitIndex"));
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void AnAbsentNerveChandleryHeatOrUpgradeSectionMeansTheDefaultNotTheLastLife(string door)
    {
        Pages.Map map = Boot("run-absent-" + door);
        Set(map, "_nerve", 12.0);
        Set(map, "_monolithSeen", true);
        Set(map, "_pendantFirstOpened", true);
        Set(map, "_extendedTanks", 3);
        Set(map, "_pills", 0);
        Set(map, "_heat", new HeatState(2, 1.0));
        Set(map, "_massLevel", 2);
        Set(map, "_holdLevel", 2);

        Pass(map, door);   // a bare Vault carries none of the four sections

        Assert.Equal(NerveModel.Steady, Read(map, "_nerve"));
        Assert.False((bool)Read(map, "_monolithSeen")!);
        Assert.False((bool)Read(map, "_pendantFirstOpened")!);
        Assert.Equal(0, Read(map, "_extendedTanks"));
        Assert.Equal(Chandlery.MedKitFullStock, Read(map, "_pills"));
        Assert.Equal(HeatState.None, Read(map, "_heat"));
        Assert.Equal(0, Read(map, "_massLevel"));
        Assert.Equal(0, Read(map, "_holdLevel"));
    }
}
