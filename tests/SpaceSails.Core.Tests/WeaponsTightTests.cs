using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #538 · WEAPONS TIGHT — the order that makes your own guns stop volunteering. Owner, designing a hull the
/// captain hides inside while somebody else's team sweeps it: <i>"where we take our guns and hide to let them
/// pass. One more berthed shuttle should be set to close off and don't shoot mode during that."</i>
/// </summary>
public sealed class WeaponsTightTests
{
    /// <summary>The whole rule: while the order stands, nothing on the captain's side opens fire.</summary>
    [Fact]
    public void TightMeansNothingOfYoursFires()
    {
        Assert.False(SentryBot.MayOpenFire(weaponsTight: true));
        Assert.True(SentryBot.MayOpenFire(weaponsTight: false));
    }

    /// <summary>
    /// IT NEVER DISARMS THE CAPTAIN, and the line has to say so — a captain deciding to shoot and a machine
    /// deciding for them are different acts, which is the distinction the entire authority idiom rests on
    /// (the word, the next-act clearance, the standing order).
    /// </summary>
    [Fact]
    public void TheOrderTiesTheMachinesAndNotTheCaptain()
    {
        Assert.Contains("your own trigger still works", SentryBot.WeaponsTightLine,
                        StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tube gun", SentryBot.WeaponsTightLine, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AND IT SAYS THE COST OUT LOUD. Forgetting that a tight gun will not save you either is exactly the
    /// mistake this order creates, so the game warns once rather than letting a captain discover it with a pack
    /// in the corridor.
    /// </summary>
    [Fact]
    public void TheOrderAdmitsThatItAlsoStopsDefendingYou()
    {
        Assert.Contains("Tight means tight", SentryBot.TightIsAlsoUndefendedLine, StringComparison.Ordinal);
        Assert.Contains("behind you", SentryBot.TightIsAlsoUndefendedLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LiftingItGivesTheArcsBack() =>
        Assert.Contains("Weapons free", SentryBot.WeaponsFreeLine, StringComparison.Ordinal);
}
