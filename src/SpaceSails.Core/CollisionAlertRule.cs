namespace SpaceSails.Core;

/// <summary>
/// #196 — which trajectory the collision alarm judges. #148's lesson (draw the INTENDED path, not
/// the ballistic loops the ship will never fly) applied to alerts: while the autopilot is ARMED with
/// a valid rehearsed plan, a ballistic impact is not news — the insert burn resolves it, so the
/// impact is the plan working. The alarm judges the PLAN path (the rehearsal samples) instead.
/// The ballistic alarm returns the instant the plan is gone — disarm, handback, or invalidation, at
/// which point the caller passes <c>armedWithValidPlan: false</c>. A plan whose OWN path goes
/// subsurface still shouts red immediately: a genuinely bad plan shouts LOUDER, not softer.
///
/// <para>Pure and tiny by design, so the "trust the plan" rule is settled in Core (tested against
/// concrete passes) rather than inferred from the razor tick.</para>
/// </summary>
public static class CollisionAlertRule
{
    /// <summary>
    /// The pass the collision alarm should shout about this tick, or null when there is nothing to
    /// shout. When <paramref name="armedWithValidPlan"/> the PLAN pass is judged (the rehearsal flew
    /// it); otherwise the ballistic projection is judged. Either way, only an actual
    /// <see cref="ClosestApproach.Pass.Impact"/> raises — a near-but-clear pass is silence.
    /// </summary>
    /// <param name="armedWithValidPlan">The autopilot is armed and a valid rehearsed plan exists.</param>
    /// <param name="ballisticPass">The tightest pass of the ballistic projection (the live course).</param>
    /// <param name="planPass">The tightest pass of the rehearsed plan path, when one exists.</param>
    public static ClosestApproach.Pass? Evaluate(
        bool armedWithValidPlan,
        ClosestApproach.Pass? ballisticPass,
        ClosestApproach.Pass? planPass)
    {
        ClosestApproach.Pass? judged = armedWithValidPlan ? planPass : ballisticPass;
        return judged is { Impact: true } ? judged : null;
    }
}
