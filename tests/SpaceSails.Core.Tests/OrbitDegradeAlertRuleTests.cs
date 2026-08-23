namespace SpaceSails.Core.Tests;

using Verdict = OrbitRule.ParkStabilityVerdict;

/// <summary>
/// #962 — the park-degradation watchdog's jurisdiction, settled in numbers.
///
/// <para>The owner's sighting is in <see cref="OrbitDegradeAlertRule"/>'s own remarks; the flight that
/// produced it is reconstructed against the shipping <c>sol.json</c> in the client bench
/// (<c>TheWatchdogDoesNotJudgeAShipTheAutopilotIsFlyingTests</c>). This file pins the LAW those numbers
/// are fed to: the deferral, and every one of the four ways the alarm still shouts through it. The
/// deferral is worth nothing if it cannot be broken, so most of this file is the breaking.</para>
/// </summary>
public class OrbitDegradeAlertRuleTests
{
    // A round Jupiter-ish body: floor at 1.1 R, and a plan that cleared it at 1.35 R.
    private const double R = 7.0e7;
    private const double Floor = 1.1 * R;
    private const double PlanPass = 1.35 * R;

    private static Verdict Judge(
        Verdict raw,
        bool flying = true,
        double planPass = PlanPass,
        double shipDistance = 17.0 * R,
        bool keeping = false) =>
        OrbitDegradeAlertRule.Evaluate(raw, keeping, flying, planPass, shipDistance, Floor);

    // ── (1) THE SIGHTING: a flown ship on a cleared plan is not a decaying park ────────────────────────

    /// <summary>The exact instant the owner photographed: bound to Jupiter, the conic's periapsis under the
    /// surface, the autopilot flying a rehearsed approach that cleared Jupiter at 1.35 R, the ship 17 R out.
    /// Silence.</summary>
    [Fact]
    public void AFlownShipOnAPlanThatClearedThisBody_IsNotShoutedAt()
    {
        Assert.Equal(Verdict.Stable, Judge(Verdict.Subsurface));
        Assert.Equal(Verdict.Stable, Judge(Verdict.TideRisk));
    }

    // ── (2) …AND EVERY WAY IT STILL SHOUTS ────────────────────────────────────────────────────────────

    /// <summary>#1 — a plan whose OWN path comes below the floor is not evidence of anything. A bad plan
    /// shouts LOUDER, not softer (the #219 law, one alarm over).</summary>
    [Fact]
    public void APlanThatDoesNotItselfClearTheFloor_IsNoDefence()
    {
        Assert.Equal(Verdict.Subsurface, Judge(Verdict.Subsurface, planPass: 0.9 * R));
        Assert.Equal(Verdict.Subsurface, Judge(Verdict.Subsurface, planPass: Floor - 1));
        // …and exactly AT the floor it is still evidence: the plan cleared, by nothing to spare.
        Assert.Equal(Verdict.Stable, Judge(Verdict.Subsurface, planPass: Floor));
    }

    /// <summary>#2 — the ship must still be flying something like that plan. Spend more than a quarter of
    /// the clearance margin the plan had and the rehearsal has stopped describing this flight.</summary>
    [Fact]
    public void AShipDeeperThanThePlanEverWent_IsJudgedRawAgain()
    {
        double trustFloor = OrbitDegradeAlertRule.PlanTrustFloor(PlanPass, Floor);
        Assert.True(trustFloor > Floor, "the trust floor must sit ABOVE the surface floor, never through it.");
        Assert.True(trustFloor < PlanPass, "…and below the plan's own pass, or the plan is trusted nowhere.");

        Assert.Equal(Verdict.Stable, Judge(Verdict.Subsurface, shipDistance: trustFloor + 1));
        Assert.Equal(Verdict.Subsurface, Judge(Verdict.Subsurface, shipDistance: trustFloor - 1));
        Assert.Equal(Verdict.Subsurface, Judge(Verdict.Subsurface, shipDistance: 1.15 * R));
    }

    /// <summary>The trust floor can never reach down through the surface, however wide the plan cleared —
    /// a plan that missed by a light-second still buys no trust inside the atmosphere.</summary>
    [Fact]
    public void TheTrustFloorNeverReachesThroughTheSurface()
    {
        foreach (double planPass in new[] { Floor, 2 * R, 50 * R, 5000 * R })
        {
            Assert.True(OrbitDegradeAlertRule.PlanTrustFloor(planPass, Floor) >= Floor,
                $"a plan clearing at {planPass / R:F0} R must not license a pass below the floor.");
        }
    }

    /// <summary>#3 — the instant the autopilot lets go (disarm, #147 handback, dry tank) the caller has no
    /// plan to pass in, and the raw verdict is judged. The #183/#193 backstop moment.</summary>
    [Fact]
    public void WithNoPlanAtAll_TheRawVerdictStands()
    {
        Assert.Equal(Verdict.Subsurface, Judge(Verdict.Subsurface, planPass: double.NaN));
        Assert.Equal(Verdict.TideRisk, Judge(Verdict.TideRisk, planPass: double.NaN));
    }

    /// <summary>#4 — it covers a ship being FLOWN. A ship nobody is flying is the whole of #180, untouched:
    /// the owner's Enceladus strand must still be discovered by the banner, not by looking.</summary>
    [Fact]
    public void AShipNobodyIsFlying_IsJudgedRaw_WhichIsTheWholeOf180()
    {
        Assert.Equal(Verdict.Subsurface, Judge(Verdict.Subsurface, flying: false));
        Assert.Equal(Verdict.TideRisk, Judge(Verdict.TideRisk, flying: false));
    }

    /// <summary>A clean orbit is never talked INTO a verdict by any of this.</summary>
    [Fact]
    public void StableAndNotBoundArePassedThroughUntouched()
    {
        Assert.Equal(Verdict.Stable, Judge(Verdict.Stable));
        Assert.Equal(Verdict.NotBound, Judge(Verdict.NotBound));
        Assert.Equal(Verdict.NotBound, Judge(Verdict.NotBound, flying: false, planPass: double.NaN));
    }

    // ── (3) FRIDAY §0's KEEPING RULE, MOVED IN AND UNCHANGED ──────────────────────────────────────────

    /// <summary>The kept park's between-trim brush at the band ceiling is the keeper working — and a true
    /// Subsurface still shouts through keeping, the one failure keeping may never mask.</summary>
    [Fact]
    public void KeepingAbsorbsTheAmberBrush_ButNeverTheRedOne()
    {
        Assert.Equal(Verdict.Stable, Judge(Verdict.TideRisk, flying: false, planPass: double.NaN, keeping: true));
        Assert.Equal(Verdict.Subsurface, Judge(Verdict.Subsurface, flying: false, planPass: double.NaN, keeping: true));
    }

    // ── (4) THE OFFER MUST BE A CHOICE THE CAPTAIN HAS ────────────────────────────────────────────────

    /// <summary>The second half of #962: the banner offered "re-park (≈48 p) or leave" one line under
    /// "AUTOPILOT HAS THE SHIP". A ship under the autopilot is told what has the helm instead — never a
    /// price for a burn that would fight the plan still being flown.</summary>
    [Fact]
    public void TheOfferSpeaksToWhoHasTheHelm()
    {
        string free = OrbitDegradeAlertRule.Offer(autopilotHasTheShip: false, reparkPulses: 48);
        Assert.Contains("re-park", free);
        Assert.Contains("48 p", free);
        Assert.Contains("leave", free);

        string flown = OrbitDegradeAlertRule.Offer(autopilotHasTheShip: true, reparkPulses: 48);
        Assert.Contains("autopilot", flown);
        Assert.Contains("stand it down", flown);
        Assert.DoesNotContain("48 p", flown);
    }
}
