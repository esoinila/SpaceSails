namespace SpaceSails.Core.Tests;

/// <summary>
/// The autopilot's promise (issues #146/#147, owner ruling: "dropping from autopilot is the horrible
/// scenario — it should never ever happen when there was nothing external to cause it"). The arm-time
/// rehearsal flies the whole armed journey with the real OrbitRule decision logic and prices it, so
/// the feasibility question is settled on the click: the doomed Titan inbound is refused; the cheap
/// Enceladus park is accepted.
/// </summary>
public class AutopilotRehearsalTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public AutopilotRehearsalTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const int Tank = 250;

    private static (Simulator Sim, ICelestialEphemeris Eph) SaturnSystem()
    {
        var eph = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        return (new Simulator(eph, timeStepSeconds: 60), eph);
    }

    private static Vector2d BodyVel(ICelestialEphemeris eph, string id, double t) =>
        (eph.Position(id, t + 1.0) - eph.Position(id, t - 1.0)) / 2.0;

    [Fact]
    public void ReserveFloor_IsAFifthIshOfCapacity_ConstantInCore()
    {
        // 18% of the 500-pulse starter tank (#262) ≈ 90; of an upgraded 650-pulse tank ≈ 117. A real
        // cushion that also absorbs the rehearsal's step-size error.
        Assert.Equal(90, AutopilotRehearsal.ReservePulses(500));
        Assert.Equal(117, AutopilotRehearsal.ReservePulses(650));
        Assert.True(AutopilotRehearsal.ReservePulses(1) >= 1, "Reserve never rounds to zero.");
    }

    [Fact]
    public void Titan_InboundOnASaturnEllipse_IsRefused_ItWouldBleedTheTankDry()
    {
        // Tonight's live failure (build 6f483d6): armed auto-orbit Titan from ~2.4 M km while screaming
        // through Saturn's well. Titan's capture floor is 3e9 m, so 2.4e9 m is already "in range" — the
        // approach engages, Saturn keeps re-accelerating the ship, rel-speed climbs past the limit, and
        // it re-burns for days (33 km/s of Δv, 175→8 pulses). The rehearsal must SEE that and refuse.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d saturnVel = BodyVel(eph, "saturn", 0);
        Vector2d titanPos = eph.Position("titan", 0);

        // 2.4 M km from Titan, out past it from Saturn, on a fast prograde Saturn orbit (not co-moving
        // with Titan) — the "big Saturn ellipse" the owner was on.
        Vector2d outward = (titanPos - saturnPos).Normalized();
        Vector2d tangential = new Vector2d(-outward.Y, outward.X);
        var ship = new ShipState(titanPos + outward * 2.4e9, saturnVel + tangential * 8000, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(ship, eph, sim, "titan", budget);
        _out.WriteLine($"TITAN #146: captured={r.Captured} pulses={r.Pulses} burns={r.ApproachBurns} " +
            $"budgetExceeded={r.BudgetExceeded} horizon={r.HorizonReached} durDays={r.SimDurationSeconds / 86400:F2} budget={budget}");

        Assert.False(r.Deliverable,
            $"Titan inbound should be refused. captured={r.Captured} pulses={r.Pulses} burns={r.ApproachBurns} " +
            $"budgetExceeded={r.BudgetExceeded} horizon={r.HorizonReached} durDays={r.SimDurationSeconds / 86400:F1}");
        // And the honest headline: its rehearsed cost blows past what the tank can afford.
        Assert.True(r.Pulses > budget || r.HorizonReached,
            $"Expected the cost to exceed the affordable budget ({budget}) or fail to capture; got pulses={r.Pulses}.");
    }

    [Fact]
    public void Enceladus_AlongsideAtRest_IsAccepted_AndCheap()
    {
        // The realistic good case (mirrors the #136 e2e): a transfer put the ship alongside Enceladus,
        // co-moving with it in Saturn's frame; press auto-orbit. The rehearsal flies it to a bound
        // park for a fraction of the tank.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d encVel = BodyVel(eph, "enceladus", 0);
        Vector2d outward = (encPos - saturnPos).Normalized();
        var ship = new ShipState(encPos + outward * 5e6, encVel, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(ship, eph, sim, "enceladus", budget);
        _out.WriteLine($"ENCELADUS near-rest: captured={r.Captured} pulses={r.Pulses} burns={r.ApproachBurns} " +
            $"budgetExceeded={r.BudgetExceeded} horizon={r.HorizonReached} durDays={r.SimDurationSeconds / 86400:F2} budget={budget}");

        Assert.True(r.Deliverable,
            $"Enceladus alongside-at-rest should be accepted. captured={r.Captured} pulses={r.Pulses} " +
            $"burns={r.ApproachBurns} budgetExceeded={r.BudgetExceeded} horizon={r.HorizonReached}");
        Assert.True(r.Pulses > 0 && r.Pulses <= budget,
            $"Expected a cheap, affordable capture; got pulses={r.Pulses} vs budget={budget}.");
    }

    [Fact]
    public void CoastingOutsideCaptureRange_SpendsNoPulses()
    {
        // Proof the rehearsal only spends where the live loop would — inside capture range. A ship well
        // outside the capture floor and gently receding never gets a burn: it just coasts, so a
        // short-horizon rehearsal records a growing path at zero cost. (This is also why cheap moon
        // capture fundamentally needs a low-relative-speed arrival close to the moon — matching a
        // moon's 12.6 km/s orbital velocity from far out is the very fuel bleed the guard refuses;
        // the parent-frame transfer that flies it cheaply is the follow-up lane, #146.)
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d saturnVel = BodyVel(eph, "saturn", 0);
        Vector2d outward = (encPos - saturnPos).Normalized();
        // 6e9 m out (double the 3e9 capture floor), at rest in the sun frame but gently receding —
        // stays out of range across a one-day horizon.
        var ship = new ShipState(encPos + outward * 6e9, saturnVel + outward * 2000, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "enceladus", budget, capturePath: true, maxHorizonSeconds: 86400.0);

        Assert.Equal(0, r.Pulses);
        Assert.Equal(0, r.ApproachBurns);
        Assert.True(r.HorizonReached, "A far, receding coast should hit the horizon, not capture.");
        Assert.True(r.Path.Count > 1, "Coasting still advances the ship along a path.");
    }

    [Fact]
    public void PathCapture_RecordsTheIntendedTrajectory_WhenAsked()
    {
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d encVel = BodyVel(eph, "enceladus", 0);
        Vector2d outward = (encPos - saturnPos).Normalized();
        var ship = new ShipState(encPos + outward * 5e6, encVel, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult with = AutopilotRehearsal.Rehearse(ship, eph, sim, "enceladus", budget, capturePath: true);
        AutopilotRehearsal.RehearsalResult without = AutopilotRehearsal.Rehearse(ship, eph, sim, "enceladus", budget, capturePath: false);

        Assert.True(with.Path.Count > 1, "A captured journey should record a multi-point path.");
        Assert.Empty(without.Path);
        // The path is time-ordered and starts at the ship's arming position.
        Assert.Equal(ship.Position, with.Path[0].Position);
        Assert.True(with.Path[^1].SimTime > with.Path[0].SimTime, "Path samples advance in time.");
    }

    [Fact]
    public void PlanCollisionPass_TrustsTheDeliverableInsertion_NoFalseImpactOnAValidApproach()
    {
        // #219 repro at Core: armed auto-orbit at Enceladus, alongside at 3 M km. The rehearsal captures
        // cleanly (Deliverable), but the coarse-stepped terminal coast records a target graze that dips
        // BELOW Enceladus's surface a step before the insert circularizes it back to a safe park. Raw
        // MostSevere over that path reports an Impact — which made ROCKS AHEAD cry on a valid armed
        // approach. PlanCollisionPass judges the ACHIEVED PARK instead: no false impact.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d encVel = BodyVel(eph, "enceladus", 0);
        Vector2d outward = (encPos - saturnPos).Normalized();
        var ship = new ShipState(encPos + outward * 3e6, encVel, 0);

        int budget = Tank - AutopilotRehearsal.ReservePulses(Tank);
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "enceladus", budget, capturePath: true);
        Assert.True(r.Deliverable, "The alongside Enceladus park must be promisable (the arm succeeds).");

        // The raw whole-path pass IS a false Enceladus impact — the graze the insertion resolves.
        ClosestApproach.Pass? raw = ClosestApproach.MostSevere(r.Path, eph);
        Assert.NotNull(raw);
        Assert.True(raw!.Value is { BodyId: "enceladus", Impact: true },
            $"Precondition: the raw path grazes subsurface (was {raw.Value.BodyName}, impact={raw.Value.Impact}).");

        // PlanCollisionPass judges the achieved park — NOT an impact, so the alarm stays silent.
        ClosestApproach.Pass? plan = AutopilotRehearsal.PlanCollisionPass(r, eph, "enceladus");
        _out.WriteLine($"#219: raw sev={raw.Value.Severity:F3} impact={raw.Value.Impact}; " +
            $"plan pass sev={plan?.Severity:F3} impact={plan?.Impact}");
        Assert.NotNull(plan);
        Assert.False(plan!.Value.Impact, "The deliverable plan's ACHIEVED PARK is above the surface — no alarm.");

        // And the whole point: fed to the rule, an armed valid approach is silent.
        Assert.Null(CollisionAlertRule.Evaluate(
            armedWithValidPlan: true, keepingHoldsOrbit: false, ballisticPass: raw, planPass: plan));
    }

    [Fact]
    public void PlanCollisionPass_StillShoutsWhenTheAchievedParkIsSubsurface()
    {
        // A genuinely bad plan shouts LOUDER: if the last (achieved-park) sample is itself under the
        // surface, PlanCollisionPass reports the impact even for a "deliverable" result.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        CelestialBody enc = eph.Bodies.First(b => b.Id == "enceladus");

        // Hand-built rehearsal whose final sample sits inside Enceladus's surface (a subsurface park).
        // Each sample is offset from Enceladus's position AT THAT sample's time (the moon moves ~12.6 km/s).
        var path = new List<TrajectorySample>
        {
            new(0, eph.Position("enceladus", 0) + new Vector2d(1e7, 0)),
            new(100, eph.Position("enceladus", 100) + new Vector2d(enc.BodyRadius * 0.5, 0)), // "park": subsurface
        };
        var bad = new AutopilotRehearsal.RehearsalResult(
            Captured: true, Pulses: 10, ApproachBurns: 1, SimDurationSeconds: 100,
            BudgetExceeded: false, HorizonReached: false, Path: path);
        Assert.True(bad.Deliverable);

        ClosestApproach.Pass? plan = AutopilotRehearsal.PlanCollisionPass(bad, eph, "enceladus");
        Assert.NotNull(plan);
        Assert.True(plan!.Value is { BodyId: "enceladus", Impact: true },
            "A plan whose achieved park is subsurface must still surface as an impact.");
    }

    [Fact]
    public void NonOrbitableTarget_IsNotAJourney()
    {
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        var ship = new ShipState(new Vector2d(1e11, 0), Vector2d.Zero, 0);
        // The sun has no parent — not a bus stop.
        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(ship, eph, sim, "sun", 200);
        Assert.False(r.Deliverable);
        Assert.Equal(0, r.Pulses);
    }

    // ---- #146 in-well transfer schedule: the Enceladus→Titan milk run, rehearsed cheap ----
    //
    // The Saturn subsystem (Saturn + Titan + Enceladus on their sol.json rails — μ_Saturn =
    // 3.7931187e16, Titan/Enceladus per sol.json), via the same SaturnSystem() fixture the rest of this
    // file flies. The ship sits on Enceladus's doorstep (rail position + 3e6 m outward, co-moving with
    // the rail) and is armed for Titan. WITH the planner's schedule the rehearsal rides one Lambert arc
    // and captures Titan for a fraction of the tank; WITHOUT it the legacy loop is already "in range" of
    // Titan's 3e9 m capture floor from ~1e9 m out and hemorrhages (the #146 bleed).
    //
    // NB on the fixture (deviation from the brief's "parentless Saturn at origin" trio, and WHY): with
    // Saturn parentless at the origin its velocity is identically zero, so the ship's HELIOCENTRIC speed
    // near Titan collapses to ~1.9 km/s. OrbitRule.PulsesFor prices a burn as Δv / (0.01·worldSpeed), so
    // that zeroed world speed inflates the *identical* captured arc from 52 pulses to 118 — a pure
    // pricing artifact that never occurs in the live game, where Saturn carries its ~9.6 km/s
    // heliocentric velocity. Flying the moving-Saturn subsystem keeps the transfer geometry byte-for-byte
    // (same moon phases/radii/periods) while pricing the pulses the way the live loop actually will.

    // The ship on Enceladus's doorstep: rail position + 3e6 m outward of the moon, co-moving with the rail.
    private static ShipState DoorstepShip(ICelestialEphemeris eph)
    {
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d encPos = eph.Position("enceladus", 0);
        Vector2d encVel = BodyVel(eph, "enceladus", 0);
        Vector2d outward = (encPos - saturnPos).Normalized();
        return new ShipState(encPos + outward * 3e6, encVel, 0);
    }

    private static TransferPlanner.Result SolveTitanRun(Simulator sim, ICelestialEphemeris eph, ShipState ship) =>
        TransferPlanner.Solve(sim, eph, new TransferPlanner.Request(ship, "saturn", "titan", MaxWaitSeconds: 0));

    [Fact]
    public void MoonRun_WithSchedule_CapturesTitan_Cheap()
    {
        // T1: the planner finds the cheap arc, and rehearsing WITH its schedule delivers a bound Titan
        // park well inside the tank (< 100 pulses of a 250-pulse tank).
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        ShipState ship = DoorstepShip(eph);

        TransferPlanner.Result plan = SolveTitanRun(sim, eph, ship);
        Assert.True(plan.Ok, $"planner should find a transfer window; failure='{plan.Failure}'");

        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "titan", budgetPulses: 120, schedule: plan.ToSchedule());
        _out.WriteLine($"MOON-RUN with schedule: captured={r.Captured} pulses={r.Pulses} burns={r.ApproachBurns} " +
            $"deliverable={r.Deliverable} durDays={r.SimDurationSeconds / 86400:F2} planQuote={plan.EstimatedPulses} " +
            $"depDv={plan.Burns[0].DeltaV.Length:F0} arrRel={plan.ArrivalRelativeSpeed:F0} totDv={plan.PlannedDeltaVTotal:F0} " +
            $"tofDays={plan.TimeOfFlightSeconds / 86400:F2}");

        Assert.True(r.Captured, "The scheduled arc should reach a bound Titan insertion.");
        Assert.True(r.Deliverable, $"The Titan milk run should be promisable; pulses={r.Pulses}, captured={r.Captured}.");
        Assert.True(r.Pulses < 100, $"The scheduled run should be cheap; got pulses={r.Pulses}.");
    }

    [Fact]
    public void MoonRun_LegacyLoop_Hemorrhages_QuantifiedVersusSchedule()
    {
        // T2: the SAME doorstep ship armed for Titan WITHOUT the schedule runs the legacy approach loop,
        // which is "in range" of Titan's 3e9 m floor from the start and bleeds the tank — either it can
        // never be promised, or it costs more than twice the scheduled run.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        ShipState ship = DoorstepShip(eph);

        TransferPlanner.Result plan = SolveTitanRun(sim, eph, ship);
        Assert.True(plan.Ok, $"planner should find a transfer window; failure='{plan.Failure}'");
        AutopilotRehearsal.RehearsalResult scheduled = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "titan", budgetPulses: 120, schedule: plan.ToSchedule());

        AutopilotRehearsal.RehearsalResult legacy = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "titan", budgetPulses: 120);
        _out.WriteLine($"MOON-RUN legacy: deliverable={legacy.Deliverable} pulses={legacy.Pulses} vs scheduled pulses={scheduled.Pulses}");

        Assert.True(
            !legacy.Deliverable || legacy.Pulses > 2 * scheduled.Pulses,
            $"Legacy Titan inbound should hemorrhage: deliverable={legacy.Deliverable}, legacyPulses={legacy.Pulses}, " +
            $"scheduledPulses={scheduled.Pulses}.");
    }

    [Fact]
    public void MoonRun_Schedule_LandsExactlyOnBurnEpoch()
    {
        // T3: the rehearsal coasts exactly onto the burn time, so the path carries a sample within 60 s
        // of the departure epoch — the exact-landing guarantee that stops warp from applying the impulse
        // from a drifted state.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        ShipState ship = DoorstepShip(eph);

        TransferPlanner.Result plan = SolveTitanRun(sim, eph, ship);
        Assert.True(plan.Ok, $"planner should find a transfer window; failure='{plan.Failure}'");
        Assert.NotEmpty(plan.Burns);
        double burnEpoch = plan.Burns[0].SimTime;

        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "titan", budgetPulses: 120, capturePath: true, schedule: plan.ToSchedule());

        Assert.Contains(r.Path, s => Math.Abs(s.SimTime - burnEpoch) <= 60.0);
    }

    [Fact]
    public void MoonRun_Schedule_IsDeterministic()
    {
        // T4: two identical scheduled rehearsals produce identical cost and duration — the whole point
        // of the deterministic Core (client WASM and server must agree).
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        ShipState ship = DoorstepShip(eph);

        TransferPlanner.Result plan = SolveTitanRun(sim, eph, ship);
        Assert.True(plan.Ok, $"planner should find a transfer window; failure='{plan.Failure}'");
        TransferPlanner.Schedule schedule = plan.ToSchedule();

        AutopilotRehearsal.RehearsalResult a = AutopilotRehearsal.Rehearse(ship, eph, sim, "titan", 120, schedule: schedule);
        AutopilotRehearsal.RehearsalResult b = AutopilotRehearsal.Rehearse(ship, eph, sim, "titan", 120, schedule: schedule);

        Assert.Equal(a.Pulses, b.Pulses);
        Assert.Equal(a.SimDurationSeconds, b.SimDurationSeconds);
    }

    [Fact]
    public void MoonRun_Schedule_TinyBudget_BailsBudgetExceeded()
    {
        // T5: a 5-pulse budget can't even afford the departure burn — the rehearsal bails BudgetExceeded
        // before it ever captures, exactly like the Approach case.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        ShipState ship = DoorstepShip(eph);

        TransferPlanner.Result plan = SolveTitanRun(sim, eph, ship);
        Assert.True(plan.Ok, $"planner should find a transfer window; failure='{plan.Failure}'");

        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "titan", budgetPulses: 5, schedule: plan.ToSchedule());
        _out.WriteLine($"MOON-RUN tiny budget: captured={r.Captured} budgetExceeded={r.BudgetExceeded} pulses={r.Pulses}");

        Assert.True(r.BudgetExceeded, "A 5-pulse budget must trip BudgetExceeded on the departure burn.");
        Assert.False(r.Captured, "It must never report a capture it could not afford.");
    }

    // ---- #155 the last mile: co-orbital rendezvous with the Ringside Exchange STATION, rehearsed ----
    //
    // The owner's stranded arm: 92,640 km from Ringside Exchange (sol.json line 23: μ=0 station on
    // Saturn's rail, orbitRadius 1.35e9, period 1.6006e6) on nearly the same lane, where the legacy
    // autopilot DECLINED at ≈229 p — absurd against the geometry. Lane 1's rendezvous planner prices the
    // closed-form phasing bus (k=1 dip ≈ 1 pulse of Δv). Rehearsing WITH that two-burn schedule must
    // deliver the ship into the DOCK ENVELOPE (DockRule) — the μ=0 terminal success, no OrbitRule.Insert.
    //
    // Fixture note (same as the moon-run block above): the full sol.json SaturnSystem() carries Saturn's
    // real ~9.6 km/s heliocentric velocity, so OrbitRule.PulsesFor prices the burns the way the live loop
    // will, not against a zeroed world speed.

    private const double SaturnMu = 3.7931187e16;

    // A ship on the Ringside Exchange's Saturn rail, behindMeters along-track BEHIND the station (the
    // station leads it, CCW prograde), co-moving at the local circular speed. Built in Saturn's frame
    // (station world velocity + parent-relative circular velocity) so pulses price against the ship's true
    // heliocentric speed. 92,640 km behind = 9.264e7 m — the owner's exact #155 gap.
    private static ShipState RingsideShip(ICelestialEphemeris eph, double behindMeters = 9.264e7)
    {
        Vector2d saturnPos = eph.Position("saturn", 0);
        Vector2d saturnVel = BodyVel(eph, "saturn", 0);
        Vector2d ringPos = eph.Position("ringside-exchange", 0);
        Vector2d rel = ringPos - saturnPos;
        double ringR = rel.Length;
        double angle = Math.Atan2(rel.Y, rel.X) - behindMeters / ringR; // trailing the CCW station
        Vector2d relPos = new Vector2d(Math.Cos(angle), Math.Sin(angle)) * ringR;
        Vector2d tangent = new Vector2d(-Math.Sin(angle), Math.Cos(angle)); // CCW prograde
        double vCirc = Math.Sqrt(SaturnMu / ringR);
        return new ShipState(saturnPos + relPos, saturnVel + tangent * vCirc, 0);
    }

    private static TransferPlanner.Result SolveRingside(Simulator sim, ICelestialEphemeris eph, ShipState ship) =>
        TransferPlanner.Solve(sim, eph, new TransferPlanner.Request(ship, "saturn", "ringside-exchange", MaxWaitSeconds: 0));

    [Fact]
    public void StationRun_WithSchedule_DeliversIntoTheDockEnvelope_Cheap_WhereLegacyDeclinedAt229p()
    {
        // T6: Solve the phasing bus, then rehearse WITH its schedule. The μ=0 station is "captured" the
        // instant the ship is inside the dock envelope (DockRule) — matched and alongside — for the price
        // of the two rendezvous burns alone (no insertion). The last mile is ~1 pulse of Δv, not the
        // legacy 229-pulse refusal.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        ShipState ship = RingsideShip(eph);

        TransferPlanner.Result plan = SolveRingside(sim, eph, ship);
        Assert.True(plan.Ok, $"the rendezvous planner should quote a phasing bus; failure='{plan.Failure}'");
        Assert.Equal(2, plan.Burns.Count); // enter the phasing ellipse, re-match on return

        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "ringside-exchange", budgetPulses: 120, schedule: plan.ToSchedule());
        _out.WriteLine($"STATION-RUN with schedule: captured={r.Captured} deliverable={r.Deliverable} " +
            $"pulses={r.Pulses} burns={r.ApproachBurns} durDays={r.SimDurationSeconds / 86400:F2} " +
            $"planQuote={plan.EstimatedPulses} totDv={plan.PlannedDeltaVTotal:F1} alts={plan.Alternatives.Count}");

        Assert.True(r.Captured, "The scheduled rendezvous should reach the dock envelope (μ=0 station success).");
        Assert.True(r.Deliverable, $"The last mile must be promisable; pulses={r.Pulses}, captured={r.Captured}.");
        Assert.True(r.Pulses <= 10, $"The station rendezvous should be cheap (≤10 p); got pulses={r.Pulses}.");
    }

    [Fact]
    public void StationRun_Schedule_IsDeterministic()
    {
        // T7: the station case is deterministic — two identical scheduled rehearsals produce identical cost
        // and duration (client WASM and server must agree on the last-mile verdict).
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        ShipState ship = RingsideShip(eph);

        TransferPlanner.Result plan = SolveRingside(sim, eph, ship);
        Assert.True(plan.Ok, $"the rendezvous planner should quote a phasing bus; failure='{plan.Failure}'");
        TransferPlanner.Schedule schedule = plan.ToSchedule();

        AutopilotRehearsal.RehearsalResult a = AutopilotRehearsal.Rehearse(ship, eph, sim, "ringside-exchange", 120, schedule: schedule);
        AutopilotRehearsal.RehearsalResult b = AutopilotRehearsal.Rehearse(ship, eph, sim, "ringside-exchange", 120, schedule: schedule);

        Assert.True(a.Captured && b.Captured, "both runs must reach the dock envelope");
        Assert.Equal(a.Pulses, b.Pulses);
        Assert.Equal(a.SimDurationSeconds, b.SimDurationSeconds);
    }

    [Fact]
    public void StationRun_Schedule_TinyBudget_BailsBudgetExceeded()
    {
        // T8: a 1-pulse budget can't afford both rendezvous burns — the rehearsal trips BudgetExceeded
        // while flying the scheduled arc, and must never report a capture it could not pay for. The budget
        // guard is identical to the moon-run and Approach cases; a μ=0 station gets no free pass.
        (Simulator sim, ICelestialEphemeris eph) = SaturnSystem();
        ShipState ship = RingsideShip(eph);

        TransferPlanner.Result plan = SolveRingside(sim, eph, ship);
        Assert.True(plan.Ok, $"the rendezvous planner should quote a phasing bus; failure='{plan.Failure}'");

        AutopilotRehearsal.RehearsalResult r = AutopilotRehearsal.Rehearse(
            ship, eph, sim, "ringside-exchange", budgetPulses: 1, schedule: plan.ToSchedule());
        _out.WriteLine($"STATION-RUN tiny budget: captured={r.Captured} budgetExceeded={r.BudgetExceeded} pulses={r.Pulses}");

        Assert.True(r.BudgetExceeded, "A 1-pulse budget must trip BudgetExceeded flying the rendezvous.");
        Assert.False(r.Captured, "It must never report a dock-envelope capture it could not afford.");
    }
}
