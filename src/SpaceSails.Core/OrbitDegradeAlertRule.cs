namespace SpaceSails.Core;

/// <summary>
/// #962 — <b>which ship the park-degradation watchdog is allowed to judge.</b> The #180 alert reads
/// <see cref="OrbitRule.ParkStability"/>: the two-body conic of the ship about the body she is bound to,
/// extrapolated to its own periapsis. That is exactly the right question for a PARKED ship (a park either
/// holds or it decays), and exactly the wrong one for a ship a thrusting autopilot is flying somewhere
/// else — an osculating conic is a prediction about a coast that is never going to happen.
///
/// <para><b>The sighting</b> (owner, 2026-08-21, issue #962). Ship in Jupiter's well, banner reading
/// <i>"AUTOPILOT HAS THE SHIP — NOW: approaching The Red Eye — autopilot flying"</i>, and a red
/// <i>"⚠ orbit degrading at Jupiter — periapsis under the surface — impact coming; re-park (≈48 p) or
/// leave"</i> across it. <i>"What is this … autopilot crashing us?"</i> — and a moment later, <i>"Now the
/// collision alert went away"</i>. Reconstructed on the shipping <c>sol.json</c> the whole thing is one
/// instant of arithmetic: the terminal approach loop re-points the ship at the station every step, so her
/// conic ABOUT JUPITER swings momentarily to a periapsis of 0.06 R — while the rehearsed plan's tightest
/// Jupiter pass is 1.35 R, the flown track's own closest approach is 1.44 R, and the trip ends clamped on
/// at The Red Eye. Nothing was ever going to hit anything.</para>
///
/// <para><b>So the trust is the #196/#220 trust, one alarm over.</b> <see cref="CollisionAlertRule"/>
/// already settled this shape for ROCKS AHEAD: when the autopilot has taken responsibility for the course,
/// judge what the REHEARSAL achieves, not the ballistic extrapolation the burns are about to erase. Here
/// the same rule keeps the park watchdog quiet while the autopilot flies a path whose rehearsal cleared
/// the very body the alarm is about.</para>
///
/// <para><b>It is a deferral, never a mute.</b> Four ways the alarm still shouts through it, and each is a
/// test in <c>OrbitDegradeAlertRuleTests</c>:</para>
/// <list type="number">
/// <item>A plan whose OWN rehearsed path comes below the surface floor at that body is not evidence of
/// anything — the raw verdict stands, and a bad plan shouts red immediately.</item>
/// <item>The ship must still be where a ship flying that plan would be: still holding
/// <see cref="PlanClearanceMarginKept"/> of the clearance the rehearsal's own closest pass had above the
/// floor. Spend more of that margin than the plan did and the plan has stopped describing this flight —
/// the raw verdict is judged again, all the way down.</item>
/// <item>The trust ends the instant the autopilot lets go — disarm, #147 handback, dry tank — because the
/// caller then has no armed plan to pass in. That is the #183/#193 backstop moment.</item>
/// <item>It covers only a ship being FLOWN. A parked ship is judged by the keeping rule below, and a ship
/// nobody is flying is judged raw, which is the whole of #180 unchanged.</item>
/// </list>
///
/// <para>Pure and tiny by design, like its sibling: the "trust the autopilot" law lives in Core against
/// concrete numbers rather than inferred from the razor tick.</para>
/// </summary>
public static class OrbitDegradeAlertRule
{
    /// <summary>
    /// The fraction of the plan's OWN clearance margin above the surface floor that the flight must
    /// still be holding for the plan to be believed about this body. The rehearsal is a faithful
    /// predictor, not a bit-identical replay (see <see cref="AutopilotRehearsal"/>'s fidelity note), so
    /// the flown track comes at a body a little tighter or wider than the coarse-stepped rehearsal did;
    /// spending a quarter of the margin absorbs that. Spending more than a quarter of it does not mean
    /// the ship is doomed — it means the rehearsal has stopped describing this flight, so it is no
    /// longer evidence and the raw verdict is judged again.
    /// </summary>
    public const double PlanClearanceMarginKept = 0.75;

    /// <summary>The distance from the body's centre at which the plan stops being believed: the surface
    /// floor plus <see cref="PlanClearanceMarginKept"/> of the margin the plan itself cleared by. Always
    /// at or above the floor — the deferral can never reach down through the surface.</summary>
    public static double PlanTrustFloor(double planClosestApproach, double surfaceFloor) =>
        surfaceFloor + PlanClearanceMarginKept * Math.Max(0, planClosestApproach - surfaceFloor);

    /// <summary>
    /// True when the rehearsed plan is honest evidence about <i>this</i> body, right now: its own path
    /// cleared the surface floor there, and the ship is still holding most of the margin that plan
    /// cleared by (<see cref="PlanTrustFloor"/>). Public so the same predicate can be read in a test, a
    /// lab, or a diagnostic line rather than re-derived.
    /// </summary>
    /// <param name="planClosestApproach">The rehearsed path's tightest pass by this body, in metres
    /// (<see cref="AutopilotRehearsal.PlanPasses"/>). Non-finite when no plan has one.</param>
    /// <param name="shipDistanceNow">The ship's ACTUAL distance to the body this instant, in metres.</param>
    /// <param name="surfaceFloor">The floor the verdict is taken against —
    /// <see cref="OrbitRule.SurfaceParkRadii"/>·R for the body.</param>
    public static bool RehearsalStillSpeaksForTheFlight(
        double planClosestApproach, double shipDistanceNow, double surfaceFloor) =>
        double.IsFinite(planClosestApproach)
        && planClosestApproach >= surfaceFloor
        && shipDistanceNow >= PlanTrustFloor(planClosestApproach, surfaceFloor);

    /// <summary>
    /// The verdict the #180 watchdog should actually speak this tick.
    /// </summary>
    /// <param name="verdict">The raw <see cref="OrbitRule.ParkStability"/> reading for the bound body.</param>
    /// <param name="keepingHoldsOrbit">The autopilot is station-keeping this park (Friday §0). The forced
    /// eccentricity a deep well pumps between trims routinely touches the band ceiling, and keeping trims
    /// it away — so <see cref="OrbitRule.ParkStabilityVerdict.TideRisk"/> is the keeper working. A true
    /// <see cref="OrbitRule.ParkStabilityVerdict.Subsurface"/> still shouts: the one failure keeping must
    /// never mask.</param>
    /// <param name="autopilotFlyingRehearsedPath">The autopilot is armed with a valid rehearsed plan AND
    /// actively flying the approach — not merely armed and waiting for a pass that has not come round
    /// (#969), and not parked.</param>
    /// <param name="planClosestApproach">See <see cref="RehearsalStillSpeaksForTheFlight"/>.</param>
    /// <param name="shipDistanceNow">See <see cref="RehearsalStillSpeaksForTheFlight"/>.</param>
    /// <param name="surfaceFloor">See <see cref="RehearsalStillSpeaksForTheFlight"/>.</param>
    public static OrbitRule.ParkStabilityVerdict Evaluate(
        OrbitRule.ParkStabilityVerdict verdict,
        bool keepingHoldsOrbit,
        bool autopilotFlyingRehearsedPath,
        double planClosestApproach,
        double shipDistanceNow,
        double surfaceFloor)
    {
        // Friday §0, unchanged and still first: a kept park's between-trim brush at the band ceiling is
        // the keeper working. Only the amber verdict is absorbed; Subsurface still gets through.
        if (keepingHoldsOrbit && verdict == OrbitRule.ParkStabilityVerdict.TideRisk)
        {
            return OrbitRule.ParkStabilityVerdict.Stable;
        }

        // #962: a ship being FLOWN along a rehearsed path that cleared this body has no park to degrade.
        // Both risk verdicts are deferred — "the tide-stable band" is a statement about a park, and a
        // transfer arc is not one — but only while the rehearsal is still honest evidence (see above).
        if (autopilotFlyingRehearsedPath
            && verdict is OrbitRule.ParkStabilityVerdict.TideRisk or OrbitRule.ParkStabilityVerdict.Subsurface
            && RehearsalStillSpeaksForTheFlight(planClosestApproach, shipDistanceNow, surfaceFloor))
        {
            return OrbitRule.ParkStabilityVerdict.Stable;
        }

        return verdict;
    }

    /// <summary>
    /// The choice the warning offers, which must be a choice the captain actually has. A free ship is
    /// told the re-park bill; a ship the autopilot is flying is NOT — offering her a manual re-park is
    /// offering her a burn that fights the plan still being flown (the second half of #962: "re-park
    /// (≈48 p) or leave", to a ship under autopilot). She is told what has the helm instead.
    /// </summary>
    /// <param name="autopilotHasTheShip">The autopilot is flying this ship — armed and on its approach,
    /// or holding a park.</param>
    /// <param name="reparkPulses">Ballpark pulses the corrective insertion would cost from here.</param>
    public static string Offer(bool autopilotHasTheShip, int reparkPulses) =>
        autopilotHasTheShip
            ? "the autopilot is flying this approach — stand it down before you re-park by hand"
            : $"re-park (≈{reparkPulses} p) or leave";
}
