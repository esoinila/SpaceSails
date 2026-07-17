namespace SpaceSails.Core.Tests;

/// <summary>#196/#220 — the collision alarm trusts the autopilot when it owns the course. Armed with a
/// valid rehearsed plan it judges the PLAN pass (the insert resolves the ballistic impact); while
/// KEEPING a funded, in-tolerance park it is silent (the keeper trims away the between-trim dip); and
/// the instant the autopilot lets go it returns to the ballistic course and shouts red immediately.</summary>
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
        // No plan, not keeping: the ballistic impact is the whole truth — shout.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: false, keepingHoldsOrbit: false, ballisticPass: Impact(), planPass: null);
        Assert.NotNull(raised);
        Assert.Equal("Enceladus", raised!.Value.BodyName);
    }

    [Fact]
    public void ArmedWithCleanPlan_IsSilentDespiteABallisticImpact()
    {
        // The #196/#219 case: the ballistic projection DOES impact, but the rehearsed plan achieves a
        // clean park. That impact is the insert working, not news — no alarm.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: true, keepingHoldsOrbit: false, ballisticPass: Impact(), planPass: Clean());
        Assert.Null(raised);
    }

    [Fact]
    public void ArmedWithSubsurfacePlan_ShoutsRedImmediately()
    {
        // A genuinely bad plan shouts LOUDER, not softer: the plan's OWN achieved park goes subsurface.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: true, keepingHoldsOrbit: false, ballisticPass: Clean(), planPass: Impact());
        Assert.NotNull(raised);
        Assert.True(raised!.Value.Impact);
    }

    [Fact]
    public void DisarmReturnsToBallistic_TheInstantThePlanIsGone()
    {
        // Handback/disarm: the caller passes armedWithValidPlan=false and a null plan pass. The
        // ballistic impact is news again.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: false, keepingHoldsOrbit: false, ballisticPass: Impact(), planPass: null);
        Assert.NotNull(raised);
    }

    [Fact]
    public void CleanBallistic_AndNoPlan_IsSilence()
    {
        Assert.Null(CollisionAlertRule.Evaluate(false, false, Clean(), null));
    }

    // ---- #220: keeping earns the alarm's trust ----

    [Fact]
    public void Keeping_IsSilentDespiteASubsurfaceBallisticDip()
    {
        // The #220 case: a healthy kept park at a deep well. The plan is consumed (armed=false), so the
        // ballistic projection between trims dips subsurface — but the keeper trims it away. While
        // keeping holds (active, funded, in tolerance) the alarm trusts the kept orbit and is silent.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: false, keepingHoldsOrbit: true, ballisticPass: Impact(), planPass: null);
        Assert.Null(raised);
    }

    [Fact]
    public void KeepingEnds_TheAlarmReturnsImmediately()
    {
        // Dry tank / disarm / unbound: the caller clears keepingHoldsOrbit — the #183 backstop moment.
        // The same subsurface ballistic dip is news again and shouts red on the very next evaluation.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: false, keepingHoldsOrbit: false, ballisticPass: Impact(), planPass: null);
        Assert.NotNull(raised);
        Assert.True(raised!.Value.Impact);
    }

    [Fact]
    public void UnfundedKeeping_Alarms()
    {
        // "Funded" is the caller's precondition: an unfunded (can't afford the next trim) keeping state
        // is NOT trusted, so the caller passes keepingHoldsOrbit=false and the ballistic dip shouts.
        ClosestApproach.Pass? raised = CollisionAlertRule.Evaluate(
            armedWithValidPlan: false, keepingHoldsOrbit: false, ballisticPass: Impact(), planPass: null);
        Assert.NotNull(raised);
    }
}
