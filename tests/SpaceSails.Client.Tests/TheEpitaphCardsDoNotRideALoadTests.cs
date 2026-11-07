using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>#1440 · A load folds away the endings of the life being left: the ship-went and scuttle epitaph
/// cards belong to that life, so neither may stand over the loaded one.
/// <b>Proven RED</b> by deleting the two epitaph lines from ForgetThePageSessionRiders.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheEpitaphCardsDoNotRideALoadTests
{
    [Fact]
    public void ALoadTakesTheOldLifesEpitaphCardsOffTheGlass()
    {
        Pages.Map map = Boot("epitaph-load");
        Set(map, "_scuttleEpitaph", "the line the old life wrote");
        Set(map, "_shipEpitaph", new Pages.Map.ShipEpitaph("went", "survives", "rescued"));
        Assert.NotNull(Read(map, "_scuttleEpitaph"));
        Assert.NotNull(Read(map, "_shipEpitaph"));

        Invoke(map, "ApplyVault", new Vault());

        Assert.Null(Read(map, "_scuttleEpitaph"));
        Assert.Null(Read(map, "_shipEpitaph"));
    }
}
