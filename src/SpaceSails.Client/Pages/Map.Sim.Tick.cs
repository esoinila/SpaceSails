using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — THE FRAME, AS A CONDUCTOR.
// Split by concern at 1,231 lines (#251) into Map.Sim.Tick.Step/Cadence/Views/Nearest/Warp; this file is
// `OnTick` itself and nothing that takes more than a screen to say. The rAF entry point, the canvas
// resize, and the five small passes at the head of every frame: the frame clock and the stall clock it
// feeds, the three ambient beats, the look-around that settles this frame's warp, the accumulator's
// purchase of sim-seconds, and the pursuit trail's open. Read `OnTick` as the order of the frame — the
// three places it can stop early are each a different fiction, and each is a named question further
// down.
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

        // #525 · HER OVERLOAD IS THE SHIP'S CLOCK, AND IT IS SPENT AHEAD OF EVERY EARLY STOP. It used to be
        // spent inside the walked view alone, which made the ninety seconds a thing that only happened while
        // the captain was on his feet: arm the charges, sit down at the nav board — which is where a captain
        // who has just told a boarder to back off actually sits — and the count froze. The threat was free.
        // Exactly the bug AdvanceHerOwnClocks (below) was written for, one field over.
        //
        // Ahead of the shuttle-run stop rather than inside that method, for the reason the ending itself is
        // written on: THE SHUTTLE BEING AWAY WITH HIM IN IT is one of the two ways this clock can reach zero
        // (ShipScuttle.Ending.Castaway, decided by CaptainWasAboardHer). A countdown that stops when the boat
        // leaves can never end with the captain clear of her, and the branch that says it can would be a
        // sentence about a frame that never runs.
        AdvanceShipCharges(dtRealSeconds);

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
        // #160: the milk-run lesson reads the live loop and says its next line. AFTER the warp is settled,
        // because one of its eight gates is the warp the game is actually honouring this frame.
        WatchTheMilkRun();
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
}
