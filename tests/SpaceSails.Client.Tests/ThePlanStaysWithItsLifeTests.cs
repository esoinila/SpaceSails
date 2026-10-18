using System.Collections;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// The flight plan is the page's, not the vault's. A load or import arrives with an empty board and a disarmed
/// autopilot, so the old life's pending cast-off cannot undock the new life's berth; a departure row only releases
/// the berth it was laid at; and a cast-off the tank cannot clear is refused, not announced.
/// </summary>
public sealed class ThePlanStaysWithItsLifeTests
{
    private static Vault At(string haven) => new()
    {
        Resume = new ResumeSection { HavenId = haven, HavenName = haven, WasDocked = true },
    };

    private static Pages.Map ADockedMapWithACastOffLaid()
    {
        Pages.Map map = Boot("plan-life");
        Invoke(map, "ApplyVault", At("selene-gate"));
        Set(map, "_reactionMassPulses", 500);
        Invoke(map, "AddCastOffAtTop");
        Assert.NotNull(Invoke(map, "PendingUndockStep"));
        return map;
    }

    [Fact]
    public void ALoadedLifeArrivesWithNoPlanAndNoArm()
    {
        Pages.Map map = ADockedMapWithACastOffLaid();
        Set(map, "_armedOrbitBodyId", "mars");
        Set(map, "_destinationBodyId", "mars");

        Invoke(map, "ApplyVault", At("red-eye"));

        Assert.Empty((IList)Read(map, "_planNodes")!);
        Assert.Null(Invoke(map, "PendingUndockStep"));
        Assert.Null(Read(map, "_armedOrbitBodyId"));
        Assert.Null(Read(map, "_destinationBodyId"));
        Assert.Equal("red-eye", Read(map, "_dockedHavenId"));
    }

    [Fact]
    public void ADepartureRowOnlyReleasesTheBerthItWasLaidAt()
    {
        Pages.Map map = ADockedMapWithACastOffLaid();
        Set(map, "_dockedHavenId", "red-eye");
        object row = Invoke(map, "PendingUndockStep")!;
        Invoke(map, "RunTheCastOffStep", row);
        Assert.Equal("red-eye", Read(map, "_dockedHavenId")); // the foreign row was retired, never obeyed
        Assert.True((bool)row.GetType().GetField("Executed")!.GetValue(row)!);
    }

    [Fact]
    public void ACastOffTheTankCannotClearIsRefusedAndLaysNothing()
    {
        Pages.Map map = Boot("plan-tank");
        Invoke(map, "ApplyVault", At("selene-gate"));
        Set(map, "_reactionMassPulses", 0);
        Invoke(map, "AddCastOffAtTop");
        Assert.Empty((IList)Read(map, "_planNodes")!);
    }
}
