namespace SpaceSails.Core;

/// <summary>
/// #196/#220 — which trajectory the collision alarm judges. #148's lesson (draw the INTENDED path,
/// not the ballistic loops the ship will never fly) applied to alerts: when the autopilot has taken
/// responsibility for the course, a ballistic impact is not news — the autopilot resolves it, so the
/// impact is the machinery working, not danger. Two ways it takes responsibility:
///
/// <list type="bullet">
/// <item><b>Armed with a valid rehearsed plan</b> (#196): the insert burn resolves the ballistic
/// impact, so the alarm judges the PLAN pass (what the rehearsal actually achieves) instead. The
/// caller supplies a plan pass that reflects the plan's ACHIEVED PARK, not the powered approach —
/// a coarse-stepped terminal coast grazing the surface just before the insertion is the insert
/// working, not news (#219). A plan whose OWN achieved park is subsurface still shouts red: a
/// genuinely bad plan shouts LOUDER, not softer.</item>
/// <item><b>Keeping the park</b> (#220): once parked, the autopilot HOLDS the orbit with trim burns.
/// At a deep well the parent's tide forces an eccentricity whose ballistic projection between trims
/// dips toward — even under — the surface, and the keeper trims it away every quarter period. While
/// keeping is ACTIVE, FUNDED (the next trim is affordable) and inside tolerance, that dip is keeping
/// working, so the alarm trusts the kept orbit and stays silent.</item>
/// </list>
///
/// <para>The ballistic alarm returns the INSTANT the autopilot lets go — disarm, handback, dry tank,
/// or an unbound orbit — at which point the caller passes <c>armedWithValidPlan: false</c> and
/// <c>keepingHoldsOrbit: false</c> and the raw course is judged again. That is the #183/#193 backstop
/// moment, and it must shout red immediately.</para>
///
/// <para>Pure and tiny by design, so the "trust the autopilot" rule is settled in Core (tested
/// against concrete passes) rather than inferred from the razor tick.</para>
/// </summary>
public static class CollisionAlertRule
{
    /// <summary>
    /// The pass the collision alarm should shout about this tick, or null when there is nothing to
    /// shout. While <paramref name="keepingHoldsOrbit"/> the kept orbit is trusted and the alarm is
    /// silent (the keeper resolves the between-trim dip). Otherwise, when
    /// <paramref name="armedWithValidPlan"/> the PLAN pass is judged (the rehearsal flew it); else the
    /// ballistic projection is judged. Either way, only an actual
    /// <see cref="ClosestApproach.Pass.Impact"/> raises — a near-but-clear pass is silence.
    /// </summary>
    /// <param name="armedWithValidPlan">The autopilot is armed and a valid rehearsed plan exists.</param>
    /// <param name="keepingHoldsOrbit">The autopilot is holding a parked orbit — keeping active,
    /// funded (next trim affordable) and inside tolerance. False the instant keeping ends.</param>
    /// <param name="ballisticPass">The tightest pass of the ballistic projection (the live course).</param>
    /// <param name="planPass">The plan's achieved-outcome pass, when armed (see the type remarks).</param>
    public static ClosestApproach.Pass? Evaluate(
        bool armedWithValidPlan,
        bool keepingHoldsOrbit,
        ClosestApproach.Pass? ballisticPass,
        ClosestApproach.Pass? planPass)
    {
        // #220: a held orbit is the autopilot's promise kept — the between-trim dip is not news. The
        // instant keeping ends the caller clears this flag and the ballistic course is judged again.
        if (keepingHoldsOrbit)
        {
            return null;
        }

        ClosestApproach.Pass? judged = armedWithValidPlan ? planPass : ballisticPass;
        return judged is { Impact: true } ? judged : null;
    }
}
