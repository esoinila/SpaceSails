namespace SpaceSails.Core.Tests;

/// <summary>
/// #177/#178 — hostile acts are NEVER automatic. The owner got robbed-by-accident when autopilot
/// flew him through a moon and a selected depot slid into the boarding window. These lock in the
/// structural fix: proximity alone can only ever surface an OPPORTUNITY; boarding proceeds only on
/// the captain's explicit, target-matched authorization.
/// </summary>
public class DeliberateBoardingTests
{
    [Fact]
    public void ProximityAlone_NeverAuthorizesABoarding()
    {
        // In the window (proximity + speed match), but the captain has said nothing: an OPPORTUNITY,
        // never an act. This is the exact autopilot-through-a-moon case from #177.
        CaptureRule.BoardingIntent intent =
            CaptureRule.EvaluateBoarding(inWindow: true, targetId: "enceladus-depot", authorizedTargetId: null);

        Assert.Equal(CaptureRule.BoardingIntent.Opportunity, intent);
        Assert.NotEqual(CaptureRule.BoardingIntent.Authorized, intent);
    }

    [Fact]
    public void AuthorizedTarget_InWindow_MayBoard()
    {
        CaptureRule.BoardingIntent intent =
            CaptureRule.EvaluateBoarding(inWindow: true, targetId: "lark", authorizedTargetId: "lark");

        Assert.Equal(CaptureRule.BoardingIntent.Authorized, intent);
    }

    [Fact]
    public void AuthorizationForOneTarget_DoesNotBleedToAnother()
    {
        // The captain OK'd the Lark; drifting into a DIFFERENT hull's window must not proceed —
        // the authorization is keyed to the hull, so it stays a mere opportunity.
        CaptureRule.BoardingIntent intent =
            CaptureRule.EvaluateBoarding(inWindow: true, targetId: "enceladus-depot", authorizedTargetId: "lark");

        Assert.Equal(CaptureRule.BoardingIntent.Opportunity, intent);
    }

    [Fact]
    public void OutOfWindow_IsNoWindow_EvenWhenAuthorized()
    {
        // A standing authorization does nothing until the geometry is actually a boarding window —
        // no felony fires from a stale OK across the system.
        Assert.Equal(
            CaptureRule.BoardingIntent.NoWindow,
            CaptureRule.EvaluateBoarding(inWindow: false, targetId: "lark", authorizedTargetId: "lark"));
    }

    [Fact]
    public void NoTarget_IsNoWindow()
    {
        Assert.Equal(
            CaptureRule.BoardingIntent.NoWindow,
            CaptureRule.EvaluateBoarding(inWindow: true, targetId: null, authorizedTargetId: null));
    }

    [Fact]
    public void EvaluateBoarding_IsPure_AndAgreesAcrossRepeatedCalls()
    {
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(
                CaptureRule.BoardingIntent.Authorized,
                CaptureRule.EvaluateBoarding(true, "prey", "prey"));
            Assert.Equal(
                CaptureRule.BoardingIntent.Opportunity,
                CaptureRule.EvaluateBoarding(true, "prey", null));
        }
    }
}
