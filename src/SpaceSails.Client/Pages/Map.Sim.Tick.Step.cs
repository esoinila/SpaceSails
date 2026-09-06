using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Sim.Tick (the header note lives in Map.Sim.Tick.cs) — THE FIXED-STEP ADVANCE. Everything
// that can interrupt an advance is in here, because every one of them is a thing that happens BETWEEN two
// integrator steps: a scheduled burn epoch inside the quantum, a surface crossing inside it, a drag peak
// inside it. `ConsumeTheAccumulator` spends the frame's purchase and says how many steps it bought;
// `PinHerToTheDockAndDriftTheGhost` overrides the integrator for a berthed hull; `AccountForWhatTheSteps
// Did` bills what the advance cost; `StepEverybodyElseAboutUs` moves everything that is not the ship; and
// `RefreshWhatTheInstrumentsSay` re-reads the gauges off the world the steps just left behind.
public partial class Map
{
    /// <summary>
    /// THE FIXED-STEP LOOP: spend the accumulator, and hand back how many steps it bought.
    ///
    /// <para>Everything that can interrupt an advance lives in here, because every one of them is a thing that
    /// happens BETWEEN two integrator steps: a scheduled burn epoch inside the quantum, a surface crossing
    /// inside the quantum, a drag peak inside the quantum. A clamped ship takes the clock-only branch.</para>
    /// </summary>
    private int ConsumeTheAccumulator(bool recordTrail)
    {
        int stepsThisFrame = 0;
        // PR-I: watch the drag load through this frame's steps so a cloud-top dip can hole the sail. Only
        // paid near an atmosphere-bearing body (where warp auto-drops to 1 s steps, so the peak is caught).
        _frameMaxDragDecel = 0;
        bool watchDrag = _dockedHavenId is null && _nearestBody?.Atmosphere is not null;
        while (_simAccumulator >= _simulator!.TimeStep)
        {
            if (stepsThisFrame >= MaxStepsPerFrame)
            {
                _simAccumulator = 0;
                break;
            }

            // M19: at high warp, consume the accumulator in fixed 60 s quanta on the planner's
            // adaptive clock — one leapfrog step instead of sixty in deep space, auto-refining
            // to 1 s steps near bodies (where warp auto-drop puts us back on the fixed path
            // anyway). Fixed quanta keep the trajectory independent of frame timing.
            bool useAdaptive = _effectiveWarp >= AdaptiveWarpThreshold && _simAccumulator >= AdaptiveWarpQuantum;
            double quantum = useAdaptive ? AdaptiveWarpQuantum : _simulator.TimeStep;

            // #955 NAV-1 SPLIT-ADVANCE — THE CAST OFF IS LANDED ON, EXACTLY. The plan's first step is the one
            // step that changes WHICH BRANCH this loop takes: while she is clamped the branch below advances
            // the clock only, and the maneuver plan is never applied. So a quantum that swallowed both the
            // cast-off and the clearance burn a minute behind it would let go of the clamp and skip the
            // out-thrust in the same breath — the plan reporting a departure the ship never flew. Landed on
            // the same way #146 lands on a transfer burn epoch: shorten the quantum onto it, or, if it is
            // already due, run it now and re-loop with no clock spent (the next pass flies her free).
            if (NextCastOffStep() is { } castOff)
            {
                double toCastOff = castOff.SimTime - _ship.SimTime;
                if (toCastOff <= 0)
                {
                    RunTheCastOffStep(castOff);
                    continue;
                }
                if (toCastOff < quantum)
                {
                    quantum = toCastOff;
                }
            }

            // #146 split-advance: if a scheduled transfer burn epoch falls inside this quantum, advance
            // EXACTLY onto it first (the way Simulator.RunAdaptive lands on a ManeuverPlan node), so the
            // impulse is applied from the true drifted state — never from a state warped thousands of
            // sim-seconds past the epoch. A burn already due (epoch reached) fires this iteration with no
            // advance; otherwise the quantum is shortened to land on the epoch and the impulse follows.
            bool applyTransferBurnAfterStep = false;
            Vector2d pendingBurnDeltaV = default;
            if (_dockedHavenId is null && _armedOrbitBodyId is not null
                && _armedTransferSchedule is { } advSched && _armedTransferBurnsFired < advSched.Burns.Count)
            {
                TransferPlanner.BurnStep nextBurn = advSched.Burns[_armedTransferBurnsFired];
                double toBurn = nextBurn.SimTime - _ship.SimTime;
                if (toBurn <= 0)
                {
                    // Epoch already reached — apply the impulse now, from the current state, and re-loop
                    // (no clock advance this pass, so the accumulator is untouched; the next pass advances
                    // normally now that this burn has fired).
                    ApplyTransferBurn(nextBurn.DeltaV);
                    continue;
                }
                if (toBurn < quantum)
                {
                    quantum = toBurn; // land exactly on the burn epoch this step, then apply the impulse
                    applyTransferBurnAfterStep = true;
                    pendingBurnDeltaV = nextBurn.DeltaV;
                }
            }

            // #264: remember where this quantum started so a surface crossing can be caught across the
            // whole advance — the ship AND the body move, and SurfaceImpact interpolates both.
            Vector2d posBeforeStep = _ship.Position;
            double timeBeforeStep = _ship.SimTime;

            if (_dockedHavenId is not null)
            {
                // Clamped in a dock: don't run the gravity integrator at all — it would fling the
                // ship off the mass-less station each step, leaving HoldAtDock forever yanking it
                // back and the berth visibly wandering at warp. Advance the clock only; HoldAtDock
                // pins the position after the loop so the ship rides the dock, dead-steady.
                _ship = _ship with { SimTime = _ship.SimTime + quantum };
            }
            else if (useAdaptive || quantum < _simulator.TimeStep)
            {
                // Adaptive at warp, OR a shortened split step to land on a transfer burn epoch — either
                // way RunAdaptive lands exactly on the requested duration.
                _ship = _simulator.RunAdaptive(_ship, quantum, _plan);
            }
            else
            {
                // #264: StepGuarded, not Step — a deep, fast periapsis substeps so it stays energy-honest
                // instead of shedding km/s on integration error (the Uranus "flower"). Identical to Step
                // everywhere the pass isn't close and fast.
                _ship = _simulator.StepGuarded(_ship, _plan);
            }
            _simAccumulator -= quantum;
            stepsThisFrame++;

            // #264: the say-the-state law's missing consequence. If this integrated step actually reached
            // a body's surface radius, that is an impact — end the flight at the crossing (never having
            // flown the interior) through the shared BUSTED freeze-frame → clinic re-birth. Docked ships
            // took the clock-only branch above and havens carry no BodyRadius, so both are exempt.
            if (_dockedHavenId is null && _busted is null && _ephemeris is not null
                && SurfaceImpact.FirstCrossing(posBeforeStep, timeBeforeStep, _ship.Position, _ship.SimTime, _ephemeris)
                    is { } surfaceHit)
            {
                TriggerImpact(surfaceHit);
                _simAccumulator = 0;
                break; // the freeze-frame owns the moment; stop consuming the accumulator this frame
            }

            if (applyTransferBurnAfterStep)
            {
                ApplyTransferBurn(pendingBurnDeltaV); // impulse at the exact epoch (may loudly hand back)
            }
            if (watchDrag)
            {
                double decel = _simulator.DragAcceleration(_ship.Position, _ship.Velocity, _ship.SimTime).Length;
                if (decel > _frameMaxDragDecel)
                {
                    _frameMaxDragDecel = decel;
                }
            }
            if (recordTrail && _ship.SimTime - _pursuitTrail[^1].SimTime >= EncounterRule.HunterStepSeconds - 0.5)
            {
                _pursuitTrail.Add(new TrajectorySample(_ship.SimTime, _ship.Position));
            }
        }
        SimTime = _ship.SimTime;
        if (recordTrail && _pursuitTrail[^1].SimTime < _ship.SimTime)
        {
            _pursuitTrail.Add(new TrajectorySample(_ship.SimTime, _ship.Position));
        }

        return stepsThisFrame;
    }

    /// <summary>Where the loop actually left her — which, for a berthed ship and for a lie that is still out
    /// there flying, is not where the integrator put them.</summary>
    private void PinHerToTheDockAndDriftTheGhost()
    {
        // Clamped in a dock: the gravity integrator just coasted the ship off on its own arc, but a
        // berthed ship rides the station instead. Pin it back onto the dock at the new SimTime — this
        // is what lets you warp the heat away without steering (owner: "no guiding while docked").
        if (_dockedHavenId is not null)
        {
            HoldAtDock();
        }

        // M29: the fake beacon's ghost flies the abandoned course ballistically, kept in
        // step with the real clock — one extra body, integrated only while the lie is out.
        if (_beaconGhost is { } ghost && SimTime > ghost.SimTime)
        {
            _beaconGhost = _simulator!.RunAdaptive(ghost, SimTime - ghost.SimTime);
        }
    }

    /// <summary>The consequences that are only owed when the clock actually moved. A frame that bought no
    /// steps bills none of them.</summary>
    private void AccountForWhatTheStepsDid(int stepsThisFrame)
    {
        if (stepsThisFrame > 0)
        {
            CheckSailHole(); // PR-I: a too-deep cloud-top dip holes the sail (before burns can fire)
            TrackAerobrakePass(); // #305: a completed haze pass rolls its 2D6 episode into the dice tray
            AccountForFiredNodes();
            if (_dockedHavenId is null)
            {
                CheckArmedInsertion(); // a clamped ship isn't flying an approach
            }
            CheckLockedFire();
        }
    }

    /// <summary>Everybody else who is moving out there, and the sweep that notices them on its own clock.</summary>
    private void StepEverybodyElseAboutUs()
    {
        StepNpcs();
        StepOrdnance();
        CheckPyramids();

        if (_ship.SimTime >= _nextSweepSimTime)
        {
            SweepSensors();
            _nextSweepSimTime = _ship.SimTime + SensorSweepSimSeconds;
        }
    }

    /// <summary>The once-a-frame recomputes: what the ⚓ says, what the 🛬 says, what the window says, what the
    /// nerve says. Every one of them is a question the HUD is about to be asked.</summary>
    private void RefreshWhatTheInstrumentsSay(double dtRealSeconds)
    {
        UpdateDockStatus();
        UpdateDockAffordance(); // #212/#211/#213: recompute the one-truth ⚓ affordance (runs paused too)
        UpdateLandableInRange(); // #339-follow: cache which landable grounds the shuttle can reach now (map 🛬 bright state)
        UpdateOrbitedBody();
        UpdateCapture(dtRealSeconds);
        UpdateEncounters();
        UpdateLocalTrade(dtRealSeconds);
        // The archive node's two edges (walking into the field, walking to arm's length) BEFORE the nerve
        // step, so a throw forced by the approach is billed on the same tick the captain crossed the line.
        StepArchiveNode();
        StepNerve(dtRealSeconds); // #317: the nerve gauge advances every tick — regolith drains, the ship eases
    }
}
