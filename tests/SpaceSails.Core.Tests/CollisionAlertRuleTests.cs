namespace SpaceSails.Core.Tests;

/// <summary>#196 — the collision alarm trusts the plan the rehearsal already flew. While armed with a
/// valid rehearsed plan it judges the PLAN path (the insert burn resolves the ballistic impact); the
/// ballistic alarm returns the instant the plan is gone; a plan whose OWN path goes subsurface shouts
/// red immediately.</summary>
public class CollisionAlertRuleTests
{
    // A ballistic course diving into Enceladus (61 km surface; distance well inside the radius).
    private static ClosestApproach.Pass Impact(string body = "Enceladus") =>
        new(body, body, BodyRadius: 252_000, Distance: 30_000, SimTime: 100, ShipPosition: default);

    // A clean pass — the rehearsed insertion parks at orbital radius, comfortably above the surface.
    private static ClosestApproach.Pass Clean(string body = "Enceladus") =>
        new(body, body, BodyRadius: 252_000, Distance: 313_000, SimTime: 100, ShipPosition: default);

    [Fact]
    public void Unarmed_JudgesTheBallisticCourse()
    {
        // No plan: the ballistic impact is the whole truth — shout.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: false, ballisticPass: Impact(), planPass: null);
        Assert.NotNull(raised);
        Assert.Equal("Enceladus", raised!.Value.BodyName);
    }

    [Fact]
    public void ArmedWithCleanPlan_IsSilentDespiteABallisticImpact()
    {
        // The #196 case: the ballistic projection DOES impact, but the rehearsed plan inserts cleanly.
        // That impact is the plan working, not news — no alarm.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: true, ballisticPass: Impact(), planPass: Clean());
        Assert.Null(raised);
    }

    [Fact]
    public void ArmedWithSubsurfacePlan_ShoutsRedImmediately()
    {
        // A genuinely bad plan shouts LOUDER, not softer: the plan's OWN path goes subsurface.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: true, ballisticPass: Clean(), planPass: Impact());
        Assert.NotNull(raised);
        Assert.True(raised!.Value.Impact);
    }

    [Fact]
    public void DisarmReturnsToBallistic_TheInstantThePlanIsGone()
    {
        // Handback/disarm: the caller passes armedWithValidPlan=false and a null plan pass. The
        // ballistic impact is news again.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: false, ballisticPass: Impact(), planPass: null);
        Assert.NotNull(raised);
    }

    [Fact]
    public void CleanBallistic_AndNoPlan_IsSilence()
    {
        Assert.Null(CollisionAlertRule.Evaluate(false, Clean(), null));
    }
}
