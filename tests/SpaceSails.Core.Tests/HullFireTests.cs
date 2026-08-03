using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #524 · FIRE, AS LAWS. Owner: <i>"if a room is on fire then pumping air out of it or venting it to space is a
/// good way to stop the fire"</i>, and <i>"One of those could have the fire also"</i> — so it lands on a derelict
/// first, where the board is a tool for saving something rather than a weapon for killing something.
/// </summary>
public sealed class HullFireTests
{
    private static HullVenting.Space Room(bool vented = false, bool doorShut = false) =>
        new("NEAR HOLD", DoorShut: doorShut, Vented: vented, Infested: false,
            HoldsSurvivor: false, CaptainInside: false);

    /// <summary>NO AIR, NO FIRE — the whole mechanic in four words, and the reason the atmosphere board is the
    /// firefighting tool without needing a single new verb.</summary>
    [Fact]
    public void AVentedCompartmentCannotBurn()
    {
        Assert.True(HullFire.CanBurn(Room()));
        Assert.False(HullFire.CanBurn(Room(vented: true)));
    }

    /// <summary>THE VALVE IS INSTANT. That is what you buy with the air: vacuum does not negotiate, and a fire
    /// does not get a grace period to argue about it.</summary>
    [Fact]
    public void TheValvePutsItOutAtOnce() =>
        Assert.True(HullFire.IsOut(Room(vented: true), sealedSeconds: 0));

    /// <summary>THE FREE ANSWER IS THE SLOW ONE. Dog the hatch and it eats its own air — eventually. Until then
    /// it is still burning, which is the price of doing nothing.</summary>
    [Fact]
    public void SealingItWorksEventuallyAndNotBefore()
    {
        Assert.False(HullFire.IsOut(Room(doorShut: true), HullFire.SmotherSeconds - 1));
        Assert.True(HullFire.IsOut(Room(doorShut: true), HullFire.SmotherSeconds));

        // …and an OPEN compartment never smothers, however long you wait: it is drinking the whole ship's air.
        Assert.False(HullFire.IsOut(Room(doorShut: false), HullFire.SmotherSeconds * 10));
    }

    /// <summary>
    /// THE ORDERING THAT MAKES IT A DECISION: a fire ruins what is in the room FASTER than it smothers itself.
    /// If this ever inverted, sealing the hatch would be a free win and the other two roads would be pointless
    /// — the whole three-way choice rests on this one inequality.
    /// </summary>
    [Fact]
    public void SealingAndWaitingAlwaysCostsTheContents()
    {
        Assert.True(HullFire.RuinSeconds < HullFire.SmotherSeconds,
                    "a sealed fire must finish eating the cargo before it finishes eating the air");
        Assert.Equal(1.0, HullFire.RuinFraction(HullFire.SmotherSeconds));
    }

    [Fact]
    public void RuinRunsFromNothingToEverythingAndStops()
    {
        Assert.Equal(0.0, HullFire.RuinFraction(0));
        Assert.Equal(0.5, HullFire.RuinFraction(HullFire.RuinSeconds / 2), 3);
        Assert.Equal(1.0, HullFire.RuinFraction(HullFire.RuinSeconds));
        Assert.Equal(1.0, HullFire.RuinFraction(HullFire.RuinSeconds * 99));
    }

    /// <summary>Spreading has to be slower than deciding: a captain who runs for the board must be able to get
    /// there before the next compartment goes.</summary>
    [Fact]
    public void ItSpreadsSlowlyEnoughToBeAnsweredAndFastEnoughToMatter()
    {
        Assert.True(HullFire.SpreadSeconds >= 15);
        Assert.True(HullFire.SpreadSeconds <= 60);
    }

    // ── What it eats ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE STAKE. On a derelict the fire is not chasing the captain — it is eating the thing the captain came
    /// for, which is a far better reason to hurry than a health bar. Priced per compartment so the board is
    /// legible: look at what is burning, know roughly what it costs a minute.
    /// </summary>
    [Fact]
    public void FireEatsAnEqualShareOfHerPerCompartment()
    {
        const int assessed = 80_000;

        Assert.Equal(assessed, HullFire.ValueAfterFire(assessed, compartments: 8, ruinedShares: 0));
        Assert.Equal(70_000, HullFire.ValueAfterFire(assessed, compartments: 8, ruinedShares: 1));
        Assert.Equal(60_000, HullFire.ValueAfterFire(assessed, compartments: 8, ruinedShares: 2));
        Assert.Equal(0, HullFire.ValueAfterFire(assessed, compartments: 8, ruinedShares: 8));
    }

    /// <summary>A fire can take everything she is worth and never a credit more, and it can never make her worth
    /// more than she was — the sort of arithmetic that goes wrong quietly if nobody pins it.</summary>
    [Fact]
    public void ItNeverOwesTheCaptainMoneyAndNeverTakesMoreThanSheHas()
    {
        Assert.Equal(0, HullFire.ValueAfterFire(50_000, compartments: 8, ruinedShares: 99));
        Assert.Equal(0, HullFire.ValueAfterFire(0, compartments: 8, ruinedShares: 3));
        Assert.Equal(50_000, HullFire.ValueAfterFire(50_000, compartments: 8, ruinedShares: -5));
        Assert.Equal(50_000, HullFire.ValueAfterFire(50_000, compartments: 0, ruinedShares: 4));
    }

    [Fact]
    public void TheSalvageScreenSaysWhatTheFireCostAndSaysNothingWhenItCostNothing()
    {
        Assert.Contains("Nothing of hers burned", HullFire.CostLine(80_000, 80_000), StringComparison.Ordinal);
        Assert.Contains("10,000", HullFire.CostLine(80_000, 70_000), StringComparison.Ordinal);
    }

    /// <summary>The captain may walk into a burning compartment. The game tells them what that means rather than
    /// refusing — the same rule the vent board follows about the room you are standing in.</summary>
    [Fact]
    public void WalkingIntoAFireIsToldNotForbidden()
    {
        Assert.Contains("You can be in here", HullFire.WalkingIntoItLine, StringComparison.Ordinal);
        Assert.True(HullFire.NervePerSecondInside > 0);
    }

    /// <summary>The first sighting names all three roads and recommends none of them.</summary>
    [Fact]
    public void TheFirstSightingNamesEveryRoadAndPicksNone()
    {
        Assert.Contains("valve", HullFire.FoundLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pumps", HullFire.FoundLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dog the hatch", HullFire.FoundLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("should", HullFire.FoundLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("best", HullFire.FoundLine, StringComparison.OrdinalIgnoreCase);
    }
}
