namespace SpaceSails.Core.Tests;

/// <summary>
/// #292 — the nav screen is not a billboard. The tutorial promotion greets only the truly new: a fresh
/// cast-off from Earth by a captain who hasn't played the lessons. These pin the gating law at its
/// logic seam — fresh-start shows, a loaded save hides, a played tutorial hides — the same states the
/// Map component feeds <see cref="TutorialPromotion.ShouldPromote"/>.
/// </summary>
public class TutorialPromotionTests
{
    [Fact]
    public void FreshEarthStart_NotYetPlayed_Shows()
    {
        Assert.True(TutorialPromotion.ShouldPromote(TutorialStartMode.FreshFromEarth, tutorialPlayed: false));
    }

    [Fact]
    public void FreshEarthStart_AlreadyPlayed_Hides()
    {
        // "tutorial-completed hides" — even the one place the lessons fit stays clear once they're done.
        Assert.False(TutorialPromotion.ShouldPromote(TutorialStartMode.FreshFromEarth, tutorialPlayed: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadedSave_AlwaysHides(bool tutorialPlayed)
    {
        // "Loading a saved game shows NONE of them" — regardless of whether the flag rode along.
        Assert.False(TutorialPromotion.ShouldPromote(TutorialStartMode.LoadedSave, tutorialPlayed));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FreshNonEarthStart_AlwaysHides(bool tutorialPlayed)
    {
        // The lessons are Earth-anchored ("the Luna pod", "dock at Earth and sell") — out of place at a
        // gas giant or a docked bar, so a fresh start elsewhere never greets, played or not.
        Assert.False(TutorialPromotion.ShouldPromote(TutorialStartMode.FreshElsewhere, tutorialPlayed));
    }
}
