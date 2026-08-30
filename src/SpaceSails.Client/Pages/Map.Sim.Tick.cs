using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — the frame: the rAF tick, the fixed-step accumulator, the walked-view paint and the near-body warp caps.
public partial class Map
{

    private void OnCanvasResized(double widthPx, double heightPx)
    {
        if (widthPx <= 0 || heightPx <= 0)
        {
            return;
        }

        _viewportWidth = (int)Math.Round(widthPx);
        _viewportHeight = (int)Math.Round(heightPx);
    }

    /// <summary>
    /// ONE FRAME, AS A LIST OF ITS PHASES.
    ///
    /// <para>#870 lane 7c: this was 505 lines in a straight line. Nothing about the order below has changed —
    /// the order IS the frame, and <c>EveryFrameLeavesTheSameFingerprintTests</c> holds twenty-four snapshots
    /// taken on the commit before the split to say so. What changed is that each phase now has a name, so the
    /// frame can be read at the altitude a reader actually needs: what happens, in what order, and where it
    /// stops.</para>
    ///
    /// <para>THREE PLACES IT CAN STOP EARLY, and each one is a different fiction: a long haul is crossing and
    /// the world is frozen; a shuttle run owns the glass; the captain is on their feet somewhere and the map is
    /// not being drawn at all.</para>
    /// </summary>
    private void OnTick(double highResTimestampMs)
    {
        if (_renderer is null || _ephemeris is null || _simulator is null)
        {
            return;
        }

        double dtRealSeconds = TakeTheFrameClock(highResTimestampMs);

        // #255: a long haul is crossing — the world is frozen mid-jump (the re-seed owns the clock, and
        // the void is never integrated). The overlay paints via Blazor; the canvas holds its last frame.
        if (_jumpInProgress)
        {
            return;
        }

        FlushVaultSaveIfDirty();  // #225: one debounced autosave write per frame when a durable event fired

        StepTheAmbientBeats(dtRealSeconds, highResTimestampMs);
        LookAroundAndPickTheWarp();
        FillTheAccumulator(dtRealSeconds);

        bool recordTrail = OpenThePursuitTrail();
        int stepsThisFrame = ConsumeTheAccumulator(recordTrail);
        PinHerToTheDockAndDriftTheGhost();
        AccountForWhatTheStepsDid(stepsThisFrame);

        StepEverybodyElseAboutUs();
        RefreshWhatTheInstrumentsSay(dtRealSeconds);

        UpdatePrediction();

        ReprojectThePassesOnTheirCadence(highResTimestampMs);
        ReprojectTheTrajectoryWhenItIsDue(highResTimestampMs);

        _pulse = _pulse.Expire(highResTimestampMs);
        SoundTheArcOnItsRisingEdge();

        if (FollowShip)
        {
            _camera.CenterOn(_ship.Position);
        }
        else if (FollowedDestinationPosition() is { } followed)
        {
            // #956 · the camera rides the NAV DESTINATION. Mutually exclusive with Follow Ship (above), so a
            // frame can only ever be told to centre on one thing.
            _camera.CenterOn(followed);
        }

        if (TheShuttleRunOwnsThisFrame(dtRealSeconds, highResTimestampMs))
        {
            return;
        }

        AdvanceHerOwnClocks(dtRealSeconds);

        if (TheWalkedViewOwnsThisFrame(dtRealSeconds, highResTimestampMs))
        {
            return;
        }

        PaintTheMapFrame();
        DrawTheScopeInsetIfItIsUp();
        LetTheShipSpeakIfAnybodyIsAboard(highResTimestampMs);
        RevealOneStepOfTheFiringSolution(highResTimestampMs);
        RefreshTheHudOnItsThrottle(highResTimestampMs);
    }

    /// <summary>How long since the last frame, in real seconds — and the one line into the stall clock both
    /// the banner and the controls read. Zero on the very first frame, and never negative.</summary>
    private double TakeTheFrameClock(double highResTimestampMs)
    {
        double dtRealSeconds = _lastTimestampMs is null
            ? 0
            : Math.Max(0, (highResTimestampMs - _lastTimestampMs.Value) / 1000.0);
        _lastTimestampMs = highResTimestampMs;
        _frameNowMs = highResTimestampMs;
        MarkFrameServiced(dtRealSeconds);   // #825: the REAL stall clock — the one both the banner and the controls read
        return dtRealSeconds;
    }

    /// <summary>The three ambient beats, in the order they were written — a tremor, its colder sibling, and
    /// the announcement that follows either of them.</summary>
    private void StepTheAmbientBeats(double dtRealSeconds, double highResTimestampMs)
    {
        StepShudder(dtRealSeconds, highResTimestampMs); // #424 HULL-SHUDDER: the ambient interior-deck tremor
        StepSignal(dtRealSeconds, highResTimestampMs);  // #424 THE UNEXPLAINED SIGNAL: the shudder's colder sibling
        StepCaution(highResTimestampMs);                // #424 THE CAUTION ANNOUNCEMENT: the rough-passage PA
    }

    /// <summary>What is nearest, what a coast just brushed past, where the skip is taking us — and, out of all
    /// of that, how fast the clock is allowed to run this frame.</summary>
    private void LookAroundAndPickTheWarp()
    {
        UpdateNearestBody();
        CheckFetchPickup();     // coasting past the wreck grabs a fetch job's goods
        DriveSkip();            // #172: own the warp while skipping — arrive/announce, or yield to the helm
        UpdateEffectiveWarp();
    }

    /// <summary>Buy this frame's worth of sim seconds, and never more than the loop below can spend.</summary>
    private void FillTheAccumulator(double dtRealSeconds)
    {
        if (!Paused)
        {
            _simAccumulator += dtRealSeconds * _effectiveWarp;
            _simAccumulator = Math.Min(_simAccumulator, MaxStepsPerFrame * _simulator!.TimeStep); // Clamp accumulator
        }
    }

    /// <summary>Start this frame's trail, and say whether anything is going to be written to it.</summary>
    private bool OpenThePursuitTrail()
    {
        // The pursuit quantum trail (see SteerHuntersByQuantumTrail — the abort switch): remember
        // where the ship actually IS through this frame's integration, at the hunter-quantum
        // cadence, so pursuit steering can look up sim-time positions instead of the frame-end
        // one. Only paid while hunters fly; a berthed ship skips it (HoldAtDock pins the truth
        // AFTER this loop, so the trail would be staler than _ship).
        bool recordTrail = SteerHuntersByQuantumTrail && _hunters.Count > 0 && _dockedHavenId is null;
        _pursuitTrail.Clear();
        if (recordTrail)
        {
            _pursuitTrail.Add(new TrajectorySample(_ship.SimTime, _ship.Position));
        }

        return recordTrail;
    }

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

    /// <summary>The passes, on their own 300 ms cadence: which body we come nearest, which we could arm, which
    /// we could sling off, which we could skim — and what the destination's own departure would cost.</summary>
    private void ReprojectThePassesOnTheirCadence(double highResTimestampMs)
    {
        if (_passDirty && highResTimestampMs - _lastReprojectMs > 300)
        {
            _passDirty = false;
            _closestPass = null;
            _armablePass = null;
            _destinationPass = null;
            _slingablePass = null;
            _skimmablePass = null;
            _passes = [];
            if (_ephemeris is not null)
            {
                double bestArmable = double.MaxValue;
                double bestSling = double.MaxValue;
                double bestSkim = double.MaxValue;
                // #952: the pass list is KEPT this time round, not just folded into four fields. The arrive
                // step re-picks its candidate against the LIVE scrub (Map.Plot.Arrive) and re-judges its
                // valid/invalid bit off the same list, so dragging the scrub costs a lookup over a handful
                // of bodies instead of another 8000-sample scan.
                IReadOnlyList<ClosestApproach.Pass> passes = ClosestApproach.Passes(_samples, _ephemeris);
                _passes = passes;
                foreach (ClosestApproach.Pass pass in passes)
                {
                    if (_closestPass is null || pass.Severity < _closestPass.Value.Severity)
                    {
                        _closestPass = pass;
                    }

                    // Armable = tightest pass by a PLANET, even when the sun ranks more severe.
                    if (PassIsOrbitable(pass) is not null && pass.Severity < bestArmable)
                    {
                        (bestArmable, _armablePass) = (pass.Severity, pass);
                    }

                    // Slingable = tightest planet pass inside the body's Hill sphere (a real flyby the
                    // crank can bend), even when it's too fast/far to orbit. PR-G's panel handle.
                    if (PassIsSlingable(pass) && pass.Severity < bestSling)
                    {
                        (bestSling, _slingablePass) = (pass.Severity, pass);
                    }

                    // Skimmable = tightest pass by an atmosphere-bearing body — PR-I's corridor gauge handle.
                    if (PassIsSkimmable(pass) && pass.Severity < bestSkim)
                    {
                        (bestSkim, _skimmablePass) = (pass.Severity, pass);
                    }

                    if (pass.BodyId == _destinationBodyId)
                    {
                        _destinationPass = pass;
                    }
                }

                // #246: the destination's OWN planet (the void mode stops at its capture range) and the
                // solved cheap DEPARTURE the offer quotes — recomputed on the reprojection cadence. The
                // departure solve (not the current-coast Project) is what the offer keys off, so the button
                // is reachable from a berth or any coast (#249 fix). The current-coast Project stays too, but
                // only for the manual-coast PROMISE verdict line ("does NOT reach — closest pass X AU").
                _longHaulPlanet = LongHaulTargetPlanet(_destinationBodyId); // null unless a real void to cross
                _longHaulReach = _longHaulPlanet is { } lhPlanet ? LongHaul.Project(_ship, _ephemeris, lhPlanet) : null;
                _longHaulDeparture = _longHaulPlanet is { } lhp2 ? LongHaul.SolveDeparture(_ship, _ephemeris, lhp2) : null;
                // #267: price the destination departure's surface-clearance verdict on THIS cadence (once,
                // not per render) so the chip/card offer gate reads it cheaply — the arc-sampling scan is too
                // heavy to run every frame.
                _longHaulClearanceBlock = _longHaulPlanet is { } lhp3 && _longHaulDeparture is { Ok: true } lhDep
                    ? LongHaulClearanceBlock(lhDep, lhp3)
                    : null;
            }

            UpdateInterceptEstimate(); // M27: the war room's clock rides the same recompute
            UpdateCourseOpportunities(); // M29: what does this course conveniently brush by?
            // #952: does the plan still END SAFELY? Judged on the freshly rebuilt passes, so a mid-flight
            // edit or a missed burn flips the arrive row to ✗ and wakes the captain once.
            RefreshArriveValidity();
            // #989: …and can the plan be FLOWN as written at all? A cast off that is no longer the first
            // step, or a second one standing behind it, is a plan the ship cannot obey — judged on the same
            // cadence and woken with the same one-shot alarm, because both are "nobody is flying the ship".
            RefreshPlanShapeValidity();
        }
    }

    /// <summary>The ribbon itself, on two clocks: a 250 ms one for a horizon the captain just changed, and a
    /// sim-time one for a coast that has simply outrun the last projection.</summary>
    private void ReprojectTheTrajectoryWhenItIsDue(double highResTimestampMs)
    {
        if (_horizonDirty && highResTimestampMs - _lastHorizonReprojectMs > 250)
        {
            _horizonDirty = false;
            _lastHorizonReprojectMs = highResTimestampMs;
            ReprojectTrajectory();
        }

        if (_ship.SimTime >= _nextProjectionSimTime)
        {
            ReprojectTrajectory();
        }
    }

    /// <summary>Thunder, once per arcing episode.</summary>
    private void SoundTheArcOnItsRisingEdge()
    {
        // Thunder on the rising edge of an arc (M10 polish) — once per arcing episode.
        bool arcing = _plasma is not null && _ship.Charge >= ArcChargeThreshold;
        if (arcing && !_wasArcing)
        {
            RendererInterop.PlayCue("arc");
        }
        _wasArcing = arcing;
    }

    /// <summary>The first of the frame's three early stops: a shuttle run owns the glass, so the map is not
    /// drawn at all. True when the frame is finished here.</summary>
    private bool TheShuttleRunOwnsThisFrame(double dtRealSeconds, double highResTimestampMs)
    {
        if (_shuttleRun is not null)
        {
            // Guarded: an exception escaping a frame callback kills renderer.js's rAF chain
            // and silently freezes the whole game — degrade to aborting the run instead.
            try
            {
                UpdateShuttleRun(dtRealSeconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"shuttle update failed: {ex}");
                EndShuttleRun(boarded: false, $"Shuttle fault: {ex.GetType().Name}");
            }
            if (_shuttleRun is not null)
            {
                try
                {
                    _shuttleView!.Draw(_viewportWidth, _viewportHeight, SimTime, _shuttleRun,
                        _deckKeys.Contains("w"), _deckKeys.Contains("s"),
                        _deckKeys.Contains("a"), _deckKeys.Contains("d"),
                        _captureEngaged ? 1 : 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"shuttle draw failed: {ex}");
                    EndShuttleRun(boarded: false, $"Shuttle fault: {ex.GetType().Name}");
                }

                RefreshTheHudOnItsThrottle(highResTimestampMs);
                return true;
            }
        }

        return false;
    }

    /// <summary>Her own clocks, which run whether or not the captain is aboard to watch them.</summary>
    private void AdvanceHerOwnClocks(double dtRealSeconds)
    {
        // #523 · HER CHARGE SYSTEMS BELONG TO THE SHIP, NOT TO A VIEW. The contactor holds the hull down and
        // spends expellant doing it, and the charge soaking into the boards behind the panel keeps climbing,
        // whether the captain is walking her corridor or sitting at the helm — the stealth tax is paid in
        // FLIGHT, which is exactly where it was invisible before. (Ticking it only in deck mode was the first
        // cut of this, and it would have made the whole system a curiosity you could only see while parked.)
        AdvanceChargeSystems(dtRealSeconds);

        // #538 · a boat that was told to wake keeps waking whether or not the captain is watching her do it.
        AdvanceBoatSpinUp(dtRealSeconds);
        AdvanceSweepTeam(dtRealSeconds);   // #538: somebody else's team, working the hull
        AdvanceSounding(dtRealSeconds);     // #537: the clock on a knock
        AdvanceLabAlarm(dtRealSeconds);     // #409+: the mountain counting

        // #528 · retire a plate whose seconds are up, and let a held card speak once the scene is calm. Ship
        // level for the same reason: a beat can be raised in flight (a shot, a sail, a hail) and must be
        // served there.
        AdvanceStoryCards();
    }

    /// <summary>The second early stop: the captain is on their feet, so the frame is a walked view and the map
    /// is never opened. True when the frame is finished here.</summary>
    private bool TheWalkedViewOwnsThisFrame(double dtRealSeconds, double highResTimestampMs)
    {
        if (_deckMode)
        {
            MoveAvatar(dtRealSeconds);
            StepSurface(dtRealSeconds); // #295/#313: dig channel, the Old Ones' converging chase, linger trickle
            // #973 L0 · …and the docked bar's own metabolism, which StepSurface can never reach: it returns on
            // the first line when there is no excursion, and a berth has none. This is the room the owner
            // drinks in finally having people who move in it.
            AdvanceBarWalkers(dtRealSeconds);
            // #973 L5b · …AND THE SIT BEAT IS SPENT ASHORE TOO. It is a debt in real seconds owed to the
            // player for having pressed [E] (#865), and it was paid out of `StepSurface` alone — the one
            // clock a seated captain had, back when every seat in the game was on an excursion. The eighth
            // seat is not: a captain sitting at a bar top in a docked berth would have owed that beat
            // forever, and a beat that never runs out holds every deferrable card behind it for the rest of
            // the visit. Only where the surface clock cannot reach, so nothing is ever spent twice.
            if (_surface is null)
            {
                SpendTheSitBeat(dtRealSeconds);

                // #1016 · …AND SO IS THE DIG'S OWN CLOCK, one issue later and for the same reason. The
                // darkroom hold (#696) was stepped out of `StepSurface` alone, which returns on its first
                // line when there is no excursion — so a captain digging a sheet out at a bar top in a docked
                // berth would have sat watching a bar that could never move. Owner, 2026-08-30: "refactor the
                // working the case etc table options to not be tied to any location."
                //
                // Only where the surface clock cannot reach, so no tick is ever charged twice. There is no
                // tank to charge first here, which is why the ordering law that governs the OTHER call site
                // (StepProcessing strictly after StepSuitAir, so a hold can never finish a frame the suit was
                // not charged for) has nothing to say about this one: a berth is pressurised and the air sim
                // has already, correctly, stopped running.
                StepProcessing(dtRealSeconds);
            }
            AdvanceShipPumps(dtRealSeconds); // her own roughing pumps — the thrifty road, on her own deck
            AdvanceShipCharges(dtRealSeconds); // and her own overload, if the keys have turned
            DrawWalkFrame();

            DrawTheScopeInsetIfItIsUp();
            RefreshTheHudOnItsThrottle(highResTimestampMs);
            return true;
        }

        return false;
    }

    /// <summary>The map itself, opened and flushed in one place so the two halves of a frame cannot come
    /// apart. Everything between them is a layer, and the order of the layers is what is on top.</summary>
    private void PaintTheMapFrame()
    {
        _camera.SetViewport(_viewportWidth, _viewportHeight);
        _renderer!.BeginFrame(_viewportWidth, _viewportHeight, Background);

        AnchorThePlotFrameToItsBody();

        DrawStreams();
        // #953 · THE TRADE-LANE CORRIDORS ARE NOT PAINTED HERE ANY MORE. SundaySecondPlan PR-B drew a quad
        // and a name label per anchor pair; #971 hid them by default after the owner's "covered in faint
        // lines with no intersection"; this ruling retired the display outright — "we have never used them
        // to find anything." One flag records it (ShipLanes.Archived) and the lane GEOMETRY still serves the
        // telescope's lane sweeps; nothing on this stack draws it.
        DrawShipTrajectory();
        // #405 Routes → Flight plan & burns: the plotted autopilot path + its burn nodes (DrawNodeMarkers,
        // below). The ship's own live trajectory ribbon (DrawShipTrajectory, above) stays — that's the
        // nav essential, not part of the plan overlay.
        if (LayerVisible("routes.plan")) DrawAutopilotPlanPath();
        DrawPredictionCone();
        DrawPassEpochGhost();
        if (PlotMode)
        {
            DrawGhostBodies();
            DrawClosestPassMarker();
            DrawDestinationPassMarker();
        }
        RetireDeflectionIfDone(); // #394: a resolved gig clears once the crew is home at the saved port
        BeginFrameLabels();       // #402: reset the frame's de-collided label queue before the producers
        DrawCelestialBodies();
        DrawAsteroidThreat(); // #394: the inbound rock's rail + the ⚠ intersect + the threat line (bends on deflection)
        DrawCargoRunMarkers();
        if (LayerVisible("routes.plan")) DrawNodeMarkers(); // #405 Routes → Flight plan & burns (the burn nodes)
        if (PlotMode)
        {
            DrawGhostShip();
        }
        DrawNpcs();           // #402 follow-up: DEPOT name labels enqueue here, so the flush must follow it
        FlushNavLabels();     // #402: resolve overlapping body/threat/depot labels — priority wins, depots yield
        DrawHunters();
        DrawTargetReticle(); // #962: the red X on the tactical target, brackets on every held track
        DrawOrdnance();
        DrawPyramids();
        DrawShuttleRange();
        DrawBeaconGhost();
        if (_activeDesk == ShipDesk.Sensors)
        {
            // #405 Sensors family, split into two leaves: the active scan overlays (the wedge + the
            // pass flash) ride sensors.scans; the lost-contact search regions ride sensors.corridors.
            if (LayerVisible("sensors.scans"))
            {
                DrawScanWedge();
                DrawPassFlash();
            }
            if (LayerVisible("sensors.corridors"))
            {
                DrawLostSearchRegions();
            }
        }
        if (_activeDesk == ShipDesk.WarRoom)
        {
            // The orrery view: a cross-system shot's geometry on the live map behind the desk.
            DrawFirePlan();
        }
        if (_dockedHavenId is not null)
        {
            DrawDockArm();
        }
        DrawShip(_ship.Position);

        _renderer.EndFrame();
    }

    /// <summary>Where the co-moving plot frame is standing this frame.</summary>
    private void AnchorThePlotFrameToItsBody()
    {
        // #135: re-anchor the co-moving plot frame to the frame body's CURRENT position, once per
        // frame. If the chosen body vanished (scenario reload), fall back to Sun/inertial.
        if (_plotFrameBodyId is not null && _ephemeris is not null)
        {
            if (_ephemeris.Bodies.Any(b => b.Id == _plotFrameBodyId))
            {
                _plotFrameAnchor = _ephemeris.Position(_plotFrameBodyId, SimTime);
            }
            else
            {
                _plotFrameBodyId = null;
            }
        }
    }

    /// <summary>The scope inset, which is its own little canvas and is drawn from BOTH the walked frame and
    /// the map frame — one call, so the two can never come to draw two different scopes.</summary>
    private void DrawTheScopeInsetIfItIsUp()
    {
        if (!_scopeMinimized && _scopeView is not null)
        {
            _scopeView.Draw(ScopeSizePx, SimTime, _ship.Position, _ship.Velocity, PickScopeTarget());
        }
    }

    /// <summary>Her whole channel — alarm strip, parrot, advert, the arrival-brake ask — in one place,
    /// gated once. The comment inside is #580's ruling, and the reason the gate is a wall rather than a
    /// filter.</summary>
    private void LetTheShipSpeakIfAnybodyIsAboard(double highResTimestampMs)
    {
        // #580 · THE SHIP'S VOICE DOES NOT REACH A CAPTAIN WHO IS NOT ABOARD HER. Owner, walking Miranda:
        // "in miranda here... why does the parrot talk about debt collectors now" / "we do not want any ship
        // type warnings received here on the surface ... that mechanic should not be active here" / "where
        // the player is not on empty ship".
        //
        // Right — the bird is on a perch on a ship that is docked and empty, and the captain is in a suit on
        // a moon. Everything below this line is the SHIP's channel: her alarm strip, her parrot, the long-
        // coast advert, the arrival-brake ask. None of it has a listener during an excursion, and squawking
        // it anyway does real damage: it drags the space fiction down onto the ground and buries the one
        // channel that IS live down there (air, tracker, nerve) under noise about somebody else's problem.
        //
        // Skipped wholesale rather than filtered, so nothing new added to the ship's side can leak down here
        // by forgetting to ask. On coming back aboard the detectors re-evaluate against live state, so a
        // condition that is still true announces itself then — which is when it can be acted on.
        if (_surface is null)
        {
            UpdateParrot(highResTimestampMs);
            UpdateShipAlerts(highResTimestampMs);
            EvaluateLongCoastAdvert(highResTimestampMs); // #172: next-event cache + long-coast squawk
            UpdateArrivalBrakeGate(highResTimestampMs);  // #304: the arrival-brake ask while the window is open
        }
    }

    /// <summary>One Newton iteration per beat, so the solution is watched being found rather than announced.</summary>
    private void RevealOneStepOfTheFiringSolution(double highResTimestampMs)
    {
        // M28: the CALCULATING FIRING SOLUTION reveal — one Newton iteration per beat.
        if (_fireSolution is { } fireSolution && _revealedIterations < fireSolution.Trace.Count
            && highResTimestampMs - _lastRevealMs > 250)
        {
            _lastRevealMs = highResTimestampMs;
            _revealedIterations++;
        }
    }

    /// <summary>
    /// The one HUD refresh, on the one 200 ms throttle — reached from all THREE ways a frame can end.
    ///
    /// <para>Blazor re-renders the whole page after every event unless it is told not to (see the
    /// <c>IHandleEvent</c> seam), which is why the HUD's refresh is the frame's job and not an event's. It
    /// was written out three times in the old straight-line frame; three copies of a throttle is three
    /// chances for one of them to drift, and the drift would look exactly like a HUD that stutters only
    /// while the shuttle is out.</para>
    /// </summary>
    private void RefreshTheHudOnItsThrottle(double highResTimestampMs)
    {
        if (highResTimestampMs - _lastHudUpdateMs > 200)
        {
            _lastHudUpdateMs = highResTimestampMs;
            InvokeAsync(StateHasChanged);
        }
    }


    // The one walked-view paint — the top-down deck — for whatever plan is welded on right now.
    // Pulled out of OnTick (#348) so the descent can render the FIRST surface frame once under the
    // still-up door (WarmFirstSurfaceFrameAsync): the cold DeckView.Draw of the enlarged regolith is
    // the last synchronous block that tripped Chrome's page-unresponsive dialog, and paying it there
    // — off the rAF loop, on its own yield — leaves the live loop warm.
    private void DrawWalkFrame()
    {
        // #841 / Lab 46 · the draw-cost probe's outer bracket, and it is a LOCAL rather than a field —
        // #905's frame ledger sweeps every field of this component into a pinned hash, and a wall-clock
        // stamp is the one kind of reading that cannot be in it. Null unless ?perf=1 armed the probe;
        // DeckView.Draw closes the bracket. What this catches that Draw alone cannot is the surface HUD
        // the page BUILDS before it can call Draw at all — blips, smudges, ghosts, beacons, the swept
        // grid — which is draw-side work by any honest reading and is not inside the conductor.
        _deckView?.Perf?.OpenWalkFrame();

        // #424 HULL-SHUDDER: a live tremor throws the whole frame a few pixels (added to the render pan,
        // never to an entity anchor) and — on the ship / a haven — freezes every patron in a unison held
        // breath (the frozen npc-hold time). Both are zero/null when no shudder is being felt.
        (double sdx, double sdy) = ShudderShakeOffset();
        _deckView!.Draw(_deckPlan, _viewportWidth, _viewportHeight, SimTime, new DeckView.State(
            _avatarX, _avatarY, _avatarHeading,
            _cargoUnits, _ship.Charge, ShuttleAway: _shuttleRun is not null, _plasma is not null,
            Docked: _dockedHavenId is not null && HavenInterior.HasInterior(_dockedHavenId),
            // #330: the nerve gauge rides every walk mode — full-size on the regolith, a compact
            // whisper aboard the ship or in a haven bar. (Flight never draws a DeckView, so it
            // stays gauge-free by construction.)
            Nerve: _nerve, NerveReadout: NerveModel.Readout(_nerve),
            ShowNerve: true, NerveCompact: _surface is null,
            // #453: the condition pips ride under the nerve bar, and only while skin is being counted —
            // off an excursion there is nothing to count, so they leave the corner entirely.
            HitsTaken: _surface?.HitsTaken ?? -1,
            // #480: the gauge never moves anonymously — the flash names the pip that just went, the
            // ledger keeps the last few so "what broke me?" has an answer after the fact.
            NerveFlash: LiveNerveFlash,
            NerveLedger: NerveLedgerLines,
            // #708: the ONE darkness ask, put to Core and handed down — the renderer never works it out
            // for itself (the #591 one-reach lesson).
            Dark: DarkHere(),
            // #784: and the POSTURE, the same way — the sim knows whether the captain is in a chair
            // (the table panel IS the chair, #757) and the figure is drawn from that one answer.
            Seated: CaptainIsSeated,
            // #825 · and whether the MACHINE is keeping up, off the one clock the input path reads.
            StallBanner: TheStallBanner()),
            _deckPanX + sdx, _deckPanY + sdy, BuildSurfaceHud(), ShudderNpcHold(), SignalCrewGlancing());
    }

    // #954 — the nearest reading, with the flicker taken out of it. It used to take the literal minimum
    // every frame, which is why the HUD (and the scope's AUTO lock, which reads the same field) alternated
    // between Mars and The Rusty Roadstead twice per two-hour station orbit: from 0.16 AU the two ARE the
    // same distance, and "closest" was re-decided on a hair. Now the incumbent keeps the slot until a
    // challenger beats it by a real margin (NearestRule.Unseats) — and the pair that used to trade places
    // is named together, "Mars › The Rusty Roadstead", by UpdateNearestNeighbourhood below.
    private void UpdateNearestBody()
    {
        // The incumbent, re-read from the live ephemeris (it may have been hidden or charted since).
        CelestialBody? incumbent = _nearestBody is { } held && !IsBodyHidden(held.Id)
            ? _ephemeris!.Bodies.FirstOrDefault(b => b.Id == held.Id)
            : null;
        double incumbentDistSq = incumbent is null
            ? double.MaxValue
            : (_ship.Position - _ephemeris!.Position(incumbent.Id, SimTime)).LengthSquared;

        CelestialBody? challenger = null;
        double minDistanceSq = double.MaxValue;
        foreach (var body in _ephemeris!.Bodies)
        {
            if (IsBodyHidden(body.Id)) continue; // a hidden wreck is never "Nearest" until charted (PR-A)
            var bodyPos = _ephemeris.Position(body.Id, SimTime);
            double distSq = (_ship.Position - bodyPos).LengthSquared;
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                challenger = body;
            }
        }

        // With no incumbent the closest takes the slot outright; otherwise it has to earn it.
        _nearestBody = incumbent is null || (challenger is not null
            && NearestRule.UnseatsSquared(incumbentDistSq, minDistanceSq))
            ? challenger ?? incumbent
            : incumbent;

        if (_nearestBody is not null)
        {
            _nearestBodyPosition = _ephemeris.Position(_nearestBody.Id, SimTime);
            // Same numeric derivative as the ship's initial state — can't disagree with the ephemeris.
            const double h = 1.0;
            _nearestBodyVelocity = (_ephemeris.Position(_nearestBody.Id, SimTime + h)
                                  - _ephemeris.Position(_nearestBody.Id, SimTime - h)) / (2 * h);
        }

        UpdateNearestNeighbourhood();
    }

    // #954 · The hierarchy the single "Nearest" slot could not hold. Owner: "present the hierarchy — Mars is
    // closest and it contains (in its Hill sphere) The Rusty Roadstead." Two shapes qualify, and they are
    // deliberately the SAME readout, so whichever of the pair happens to hold the slot the line reads alike:
    //   (a) the nearest thing itself orbits a body that orbits something else — a moon or a station, never
    //       the nonsense "Sun › Mars"; and
    //   (b) the nearest thing IS a planet, and one of its own dockable havens is in the same breath (inside
    //       the hysteresis band) — the very body the readout used to flip to every orbit.
    private void UpdateNearestNeighbourhood()
    {
        _nearestParentName = null;
        _nearestChildName = null;
        _nearestHaven = null;

        if (_ephemeris is null || _nearestBody is not { } near)
        {
            return;
        }

        if (IsDockableHaven(near))
        {
            _nearestHaven = near;
        }

        // (a) A satellite of a satellite-bearing body: Phobos and the Roadstead qualify, Mars does not
        // (its parent is the sun, which orbits nothing — "Sun › Mars" is not a neighbourhood).
        CelestialBody? parent = near.ParentId is { } pid
            ? _ephemeris.Bodies.FirstOrDefault(b => b.Id == pid)
            : null;
        if (parent is { ParentId: not null })
        {
            _nearestParentName = parent.Name;
            _nearestChildName = near.Name;
            return;
        }

        // (b) The planet holds the slot — look for the haven riding beside it, close enough that neither
        // could unseat the other. That is exactly the pair whose swap the owner watched flicker.
        double nearDistSq = (_ship.Position - _nearestBodyPosition).LengthSquared;
        CelestialBody? haven = null;
        double havenDistSq = double.MaxValue;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (body.ParentId != near.Id || IsBodyHidden(body.Id) || !IsDockableHaven(body))
            {
                continue;
            }

            double d = (_ship.Position - _ephemeris.Position(body.Id, SimTime)).LengthSquared;
            if (d < havenDistSq)
            {
                (havenDistSq, haven) = (d, body);
            }
        }

        if (haven is not null && NearestRule.InTheSameBreathSquared(nearDistSq, havenDistSq))
        {
            _nearestParentName = near.Name;
            _nearestChildName = haven.Name;
            _nearestHaven = haven;
        }
    }

    // The neighbourhood the nearest reading belongs to: the containing body's name and the thing inside it,
    // or nulls when the nearest is just itself. Read by the HUD line and by the scope's AUTO sub-line.
    private string? _nearestParentName;
    private string? _nearestChildName;

    // The dockable haven this neighbourhood offers — the nearest body itself when IT is the haven, else the
    // one riding beside it. The ⚓ affordance hint follows this, so it no longer blinks out on the frames
    // the planet held the slot.
    private CelestialBody? _nearestHaven;

    // The one line the "Nearest:" readout speaks — "Mars › The Rusty Roadstead" when there is a hierarchy
    // to present, the plain name when there is not.
    private string NearestReadoutName() =>
        _nearestParentName is { } p && _nearestChildName is { } c
            ? NearestRule.Hierarchy(p, c)
            : _nearestBody?.Name ?? "";

    private void UpdateEffectiveWarp()
    {
        // Clamped in a dock: the ship is held fast (HoldAtDock overrides the integrator), so there's
        // nothing to overshoot or collide with — warp freely. This is what makes lying low to bleed
        // off heat a quick fast-forward (heat cools ~5 sim-days/level at a haven) instead of an
        // hours-long crawl under the near-body warp cap.
        if (_dockedHavenId is not null)
        {
            _effectiveWarp = Warp;
            return;
        }

        // Bound to a planet (M20)? No encounter to overshoot — let the orbit spin at up to
        // 1000x instead of crawling on the near-body tiers.
        if (OrbitInfo() is { } orbitInfo
            && OrbitRule.IsBound(_ship, _nearestBodyPosition, _nearestBodyVelocity, orbitInfo.Body, orbitInfo.Hill))
        {
            _effectiveWarp = Math.Min(Warp, 1000);
            return;
        }

        if (_nearestBody == null)
        {
            _effectiveWarp = Warp;
            return;
        }

        // Absolute tiers with a body-radius floor so the Sun's huge radius still gets a sane
        // (small) zone while planets use encounter-scale distances. Pure BodyRadius multiples
        // don't work: ×5000 on the Sun caps warp across ~23 AU, i.e. the whole inner system.
        double distance = (_ship.Position - _nearestBodyPosition).Length;
        double encounterRadius = Math.Max(1e9, _nearestBody.BodyRadius * 30);   // ~3 lunar distances at Earth
        double closeRadius = Math.Max(1e8, _nearestBody.BodyRadius * 6);
        double grazingRadius = _nearestBody.BodyRadius * 3;

        int cap = int.MaxValue;
        if (distance < grazingRadius)
        {
            cap = 10;
        }
        else if (distance < closeRadius)
        {
            cap = 100;
        }
        else if (distance < encounterRadius)
        {
            cap = 1000;
        }

        _effectiveWarp = Math.Min(Warp, cap);

        // A live capture window is a close encounter by definition: cap warp so the 60 s window
        // is actually holdable. Selection alone doesn't cap — only an engaged window.
        NpcState? captureTarget = SelectedCaptureTarget();
        if (captureTarget is not null && CaptureRule.IsInWindow(_ship, captureTarget.State))
        {
            _effectiveWarp = Math.Min(_effectiveWarp, CaptureWarpCap);
        }

        // #136: a deep-well moon's parking band is only tens of km wide — far thinner than the
        // grazing-tier step at 10×. When armed for such a moon, cap warp so one tick advances only
        // a fraction of the distance still to close, easing to 1× right at the band the way the
        // 60 s unit test threads it. Inert for planets/roomy moons (band far outside the grazing
        // radius) and when not armed. Keyed off the nearest body, which IS the armed one on final.
        _effectiveWarp = Math.Min(_effectiveWarp, DeepWellInsertionWarpCap(distance));
    }

    // The warp ceiling that keeps an armed deep-well insertion holdable (issue #136). Returns
    // int.MaxValue (no cap) unless the ship is armed for the nearest body and that body is a deep
    // well whose whole parking band sits inside its grazing radius.
    private int DeepWellInsertionWarpCap(double distanceToNearest)
    {
        if (_armedOrbitBodyId is null || _ephemeris is null || _nearestBody is null
            || _armedOrbitBodyId != _nearestBody.Id || _nearestBody.ParentId is null)
        {
            return int.MaxValue;
        }

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == _nearestBody.ParentId) { parent = candidate; break; }
        }
        if (parent is null) return int.MaxValue;

        double hill = OrbitRule.HillRadius(_nearestBody, parent.Mu);
        double park = OrbitRule.ParkingRadius(_nearestBody, hill);
        if (park >= _nearestBody.BodyRadius * 3 || distanceToNearest > OrbitRule.CaptureRange(hill))
        {
            return int.MaxValue; // roomy moon/planet, or not yet closing — the tiers suffice
        }

        // Advance at most ~⅓ of the room left to the band per 60 s tick; never below 1×. As the
        // ship reaches the band the room shrinks to a body radius and the cap eases to 1×.
        double closing = Math.Max(1.0, Math.Abs(OrbitRule.ClosingSpeed(_ship, _nearestBodyPosition, _nearestBodyVelocity)));
        double room = Math.Max(distanceToNearest - park, _nearestBody.BodyRadius);
        return Math.Max(1, (int)(room / (3 * 60 * closing)));
    }

    // Plasma stream ribbons (M7): one translucent wide segment per stream, between the two
    // endpoint bodies at the current sim time. Drawn first so everything else layers on top.
    // No-op outside an Electric Universe scenario.
    private void DrawStreams()
    {
        if (_plasma is null) return;

        // Drawn as flowing filaments, not one flat band — a single thick polyline read as "a
        // strange rectangle" (owner report). Four narrow ribbons undulate along the axis with
        // sim-time phase; alpha fades toward the edges.
        Span<float> pts = stackalloc float[34];
        foreach ((string fromId, string toId, double halfWidth) in _plasma.Streams)
        {
            Vector2d a = _ephemeris!.Position(fromId, SimTime);
            Vector2d b = _ephemeris.Position(toId, SimTime);
            Vector2d axis = b - a;
            double len = axis.Length;
            if (len <= 0) continue;
            Vector2d dir = axis / len;
            Vector2d perp = new(-dir.Y, dir.X);

            for (int ribbon = 0; ribbon < 4; ribbon++)
            {
                double lane = (ribbon - 1.5) / 1.5;              // -1 … 1 across the width
                double phase = SimTime * 4e-7 + ribbon * 1.7;
                for (int k = 0; k <= 16; k++)
                {
                    double t = k / 16.0;
                    double wobble = Math.Sin(t * 9.0 + phase) * 0.25;
                    Vector2d world = a + dir * (len * t) + perp * (halfWidth * (lane * 0.8 + wobble));
                    (float sx, float sy) = _camera.WorldToScreen(world);
                    pts[k * 2] = sx;
                    pts[k * 2 + 1] = sy;
                }
                byte alpha = (byte)(30 - 12 * Math.Abs(lane));
                float widthPx = (float)Math.Clamp(halfWidth * 0.5 / _camera.MetersPerPixel, 1, 60);
                _renderer!.DrawPolyline(pts, new RgbaColor(80, 220, 220, alpha), widthPx);
            }
        }
    }
}
