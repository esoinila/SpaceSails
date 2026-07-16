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

    // ---- The stand-down must actually stick. The every-frame capture tick re-evaluates the same
    // geometry, so a bare dismiss is immortal; ResolvePlunderPrompt carries a declined memory. ----

    [Fact]
    public void FreshOpportunity_IsOffered()
    {
        CaptureRule.PlunderPrompt p =
            CaptureRule.ResolvePlunderPrompt(CaptureRule.BoardingIntent.Opportunity, "depot", declinedTargetId: null);

        Assert.True(p.Offer);
        Assert.Null(p.DeclinedTargetId);
    }

    [Fact]
    public void DeclinedHull_StaysSilent_WhileStillInTheWindow()
    {
        // The captain stood down from "depot"; every following frame the geometry still reads
        // Opportunity for "depot" — but the prompt must NOT re-surface, and the memory persists.
        for (int frame = 0; frame < 100; frame++)
        {
            CaptureRule.PlunderPrompt p =
                CaptureRule.ResolvePlunderPrompt(CaptureRule.BoardingIntent.Opportunity, "depot", declinedTargetId: "depot");

            Assert.False(p.Offer);                       // never nags
            Assert.Equal("depot", p.DeclinedTargetId);   // remembers the stand-down across frames
        }
    }

    [Fact]
    public void LeavingTheWindow_ReArms_SoTheNextPassMayOfferAgain()
    {
        // Pass ends → NoWindow → the decline lapses.
        CaptureRule.PlunderPrompt exited =
            CaptureRule.ResolvePlunderPrompt(CaptureRule.BoardingIntent.NoWindow, targetId: null, declinedTargetId: "depot");
        Assert.False(exited.Offer);
        Assert.Null(exited.DeclinedTargetId);            // re-armed

        // A fresh pass over the same hull now offers again.
        CaptureRule.PlunderPrompt reapproach =
            CaptureRule.ResolvePlunderPrompt(CaptureRule.BoardingIntent.Opportunity, "depot", exited.DeclinedTargetId);
        Assert.True(reapproach.Offer);
    }

    [Fact]
    public void SelectingADifferentHull_ReArms_AndOffersTheNewOne()
    {
        // Declined "depot", but now a DIFFERENT hull ("lark") is the one in the window: the stale
        // decline lapses and the new hull is offered.
        CaptureRule.PlunderPrompt p =
            CaptureRule.ResolvePlunderPrompt(CaptureRule.BoardingIntent.Opportunity, "lark", declinedTargetId: "depot");

        Assert.True(p.Offer);
        Assert.Null(p.DeclinedTargetId);
    }

    [Fact]
    public void AuthorizingClearsTheDeclineMemory()
    {
        CaptureRule.PlunderPrompt p =
            CaptureRule.ResolvePlunderPrompt(CaptureRule.BoardingIntent.Authorized, "depot", declinedTargetId: "depot");

        Assert.False(p.Offer);                 // no opportunity prompt during an authorized boarding
        Assert.Null(p.DeclinedTargetId);       // and the stale decline is cleared
    }
}
