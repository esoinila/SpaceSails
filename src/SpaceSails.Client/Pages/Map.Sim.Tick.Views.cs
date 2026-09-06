using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Sim.Tick (the header note lives in Map.Sim.Tick.cs) — WHICH VIEW OWNS THIS FRAME, AND WHAT
// IT PAINTS. The frame's two early stops in the order `OnTick` asks them (a shuttle run owns the glass; the
// captain is on his feet and the map is never opened), the clocks that keep running through them, and then
// the map frame itself: the plot anchored to its body, the scope inset, the ship's own line, one step of
// the firing solution, and the HUD on its throttle. `DrawWalkFrame` is the walked view's paint. NOTE:
// `DrawStreams` was the last member of the monolith, a hundred lines below the warp caps, and it is a map
// pass — `PaintTheMapFrame` calls it — so it is filed here with the frame that draws it. Byte-identical
// code; nothing about it was re-decided.
public partial class Map
{
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
        AdvanceTheCut(dtRealSeconds);       // #537 slice 3: the clock on a cut — and the scar going cold
        AdvanceLabAlarm(dtRealSeconds);     // #409+: the mountain counting

        // #663 · …and the crew's own standing, which is a pure reading of the whole voyage rather than an
        // event anybody could hook. BEFORE the story cards, so a deputation raised on this frame is served
        // by the same frame's card clock instead of waiting a tick — and the seam itself short-circuits on
        // every frame where the sheet's inputs have not moved.
        WatchWhereTheCrewStand();

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
            // #417 · …and the fourth name, which is tied up outside whether or not this berth has a bar in
            // it. Raised here rather than in the room's metabolism for exactly that reason.
            TheRevealAtTheBerth();
            // #1052 · …AND A CARD HELD BEHIND A SCRIM GETS ITS TURN. One line, in the one frame a bar or a
            // canteen runs at all, so the salesman who arrived while the captain was reading the galley card
            // speaks on the frame after the captain shuts it rather than dimming the room a second time.
            // AFTER the room has stepped, for the room's own reason (Map.BarWalkers.cs): a decision about
            // what somebody should do next is a decision about a floor whose bodies have already moved.
            PumpTheScrimQueue();
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
            // #525 · HER OVERLOAD IS NOT SPENT HERE ANY MORE — it is a ship clock now, run ahead of every
            // early stop in OnTick. It was the walked view's alone, and a captain at the nav board watched
            // ninety seconds not pass.
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
        // #167 · the burn's beat washes the stretch of that ribbon she is about to fly — over the ribbon
        // it re-tints, and only for the second the flame off her stern lasts.
        DrawTheBurnBeatAlongThePathAhead();
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
