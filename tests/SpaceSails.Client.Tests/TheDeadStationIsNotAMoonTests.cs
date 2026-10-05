using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #653 · THE SIXTH OCCURRENCE OF THE PATTERN <see cref="AwayTeamSide"/> EXISTS TO CLOSE: a MOON constant
/// governing a PLACE. A station's deck is laid out around (0, 0) — well inside the regolith's top rim at y = −20
/// — so the moon's "am I back at the boat" rule (<see cref="MoonSurface.IsSafeAboard"/>) is satisfied by almost
/// every square of her, and a station that asked it would hand the captain her tank back everywhere.
///
/// <para>These pin the station's own answer: a sphere around whichever access the boat is mated to. The premise
/// the whole file leans on — that the moon's rule WOULD say yes in the middle of the drum — is asserted first, so
/// the day the regolith's rim moves and this stops being a trap the guard says so instead of passing vacuously.</para>
/// </summary>
public sealed class TheDeadStationIsNotAMoonTests
{
    private const double R = 0.7;
    private const string Id = "dev-station-6";

    private static StationWreck.Access Hub => StationAboard.AccessOf(Id, StationWreck.ModuleId.Hub);

    [Fact]
    public void ThePremiseHolds_TheMoonsRuleSaysYesInTheMiddleOfTheDrum()
    {
        Assert.True(MoonSurface.IsSafeAboard(0), "the regolith's rim moved — the trap this file guards is gone.");
        Assert.True(AwayTeamSide.BackAtTheShuttle(onWreck: false, 0, 0, R),
            "with no station dock the moon's rule is what answers.");
    }

    [Fact]
    public void ACaptainInTheMiddleOfTheDrumIsAwayFromTheBoatAndOneAtTheLockIsBackAtIt()
    {
        (double sx, double sy) = StationWreck.SpawnFor(Hub);

        Assert.True(AwayTeamSide.BackAtTheShuttle(false, sx, sy, R, Hub));
        Assert.False(AwayTeamSide.BackAtTheShuttle(false, 0, 0, R, Hub));

        // …and wherever the boat is mated is where "back" is: flown to the Habitat's lock, the drum is far and
        // the Habitat's own lock is near.
        StationWreck.Access habitat = StationAboard.AccessOf(Id, StationWreck.ModuleId.Habitat);
        (double hx, double hy) = StationWreck.SpawnFor(habitat);
        Assert.True(AwayTeamSide.BackAtTheShuttle(false, hx, hy, R, habitat));
        Assert.False(AwayTeamSide.BackAtTheShuttle(false, sx, sy, R, habitat));
    }

    [Fact]
    public void TheCommsDropIsRarestAtTheBoatAndGrowsWithTheDistanceFromIt()
    {
        (double sx, double sy) = StationWreck.SpawnFor(Hub);
        double atTheBoat = AwayTeamSide.CommsOnsetBias(false, sx, sy, R, Hub);
        double midway = AwayTeamSide.CommsOnsetBias(false, 0, 20, R, Hub);
        double farthest = AwayTeamSide.CommsOnsetBias(false, 41, -41, R, Hub);

        Assert.Equal(0.5, atTheBoat);
        Assert.InRange(midway, 1.0, 2.0);
        Assert.True(farthest > midway, "deeper in should be worse, as it is on a moon and in a derelict.");
        Assert.InRange(farthest, 1.0, 2.0);
    }
}
