using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Quests — #160, THE MILK RUN. The fourth lesson: the whole working loop, end to end,
// flown by the autopilot. The eight LINES are canon and live in Core (MilkRunLesson.Lines); this file is the
// eight GATES — the real state that finishes each step — and the watcher that says one line at a time.
public partial class Map
{
    // ── WHAT THIS LESSON IS, AND WHY IT IS SHAPED LIKE THIS ─────────────────────────────────────────────
    //
    // Owner, 2026-07-16, mid-playtest: "I think we should have a tutorial for mission also? Some easy milk
    // run with autopilot from moon to moon, maybe?" The three lessons that already existed teach a hunt, a
    // gun and a haven — every one of them a thing that happens when the work goes WRONG. Nothing taught the
    // work, which is why the last line of this one is the one it is.
    //
    // THE LESSON POSTS ITS OWN CONTRACT. #1091's ruling, in the owner's words: "a schedule based tutorial
    // only works at certain time. It is kind of a bad design like this. The tutorial selection should
    // trigger the launch of the target vehicles." A milk run's target vehicle is a notice on a wall, so
    // choosing the lesson is what puts the job on the board — priced, addressed and pitched by the board's
    // own generator (MakeCargoRunOffer), from the berth the captain is actually standing in, at the moment
    // they are standing in it. There is no window to miss.
    //
    // EACH LINE IS SAID ONCE, WHEN ITS STEP BECOMES THE ONE TO DO. Line k is step k's instruction, so it is
    // spoken as step k becomes current — not when it finishes. That is why line 7 ("Arrival is autopilot
    // then… Dock, and the contract pays at the counter") is said when the coast begins and not when the
    // clamp bites: it is telling the captain what is about to happen to them.
    //
    // AND NEVER AGAIN ON A RELOAD. _tutorialStep is not vaulted — it rests at 0 for every captain who never
    // took a lesson, which is load-bearing elsewhere (KeepTheLessonsPreyInTheWorld) — so this lesson vaults
    // its own place instead, and restores _tutorialStep from it. A deliberate REPLAY from the Tutorials tab
    // is a different thing from a reload and does start over: SeedMilkRun puts the place back to 1.
    //
    // WHAT IS POINTED AT AND NOT TAUGHT. Step 4's rehearsal is where the cheaper-vs-sooner phasing table is
    // on screen; the lesson tells the captain to read the number and believe it, and says nothing about
    // choosing between two of them. That is a later lesson (the owner's own note: "that comes in handy when
    // there is heat on us"), and this one has no line for it, because no line for it was written.

    /// <summary>The lesson's own place, and the ONE piece of it that is vaulted (ProgressSection ·
    /// MilkRunLessonStep). 0 = never taken. 1..8 = that step is the current one AND its line has already
    /// been said. <see cref="MilkRunFinished"/> = the loop was walked to the end.</summary>
    private int _milkRunStep;

    /// <summary>The value <see cref="_milkRunStep"/> rests at once the coin is on the counter — one past the
    /// last step, the same idiom <c>_tutorialStep</c> uses for a finished track.</summary>
    private static int MilkRunFinished => MilkRunLesson.StepCount + 1;

    /// <summary>Has the contract already been laid on this berth's board during this trip ashore? Session
    /// state, deliberately: a captain who PASSES the card must be able to pass it (the general UI law — no
    /// pop-up that cannot be closed), so the offer is posted once per walk ashore and comes back the next
    /// time they walk in, or the moment they raise the checklist (RelaunchTheLessonsPreyIfSheIsGone).</summary>
    private bool _milkRunPostedAshore;

    /// <summary>Real-time stamp of the last line said, so two of them can never land in one breath. Negative
    /// infinity means "nothing said yet", which is the only value that lets the first line go out at once.
    /// </summary>
    private double _milkRunSaidAtMs = double.NegativeInfinity;

    /// <summary>A beat between lines, and it is not a number typed for this lesson: it is
    /// <see cref="PulseSlot.MinDwellMs"/>, "the shortest time the screen ever shows anything". The gates are
    /// read off live state and several of them can already be true when their step comes round (a full tank,
    /// a warp the captain never dropped) — without this, a lesson that is correctly ticking steps off would
    /// overwrite its own teaching inside one frame and the captain would read only the last of them.</summary>
    private static double MilkRunLineSpacingMs => PulseSlot.MinDwellMs;

    /// <summary>The lesson's contract, once it is in the captain's hand — found by the id it was stamped
    /// with, which is what quests are vaulted under, so a reload finds it again.</summary>
    private Quest? TheMilkRun
    {
        get
        {
            foreach (Quest q in _quests)
            {
                if (q.Id == MilkRunLesson.QuestId)
                {
                    return q;
                }
            }

            return null;
        }
    }

    // ── TAKING THE LESSON ON ────────────────────────────────────────────────────────────────────────────

    /// <summary>Choosing the milk run from the Captain's Tutorials tab. Puts the lesson's place back to its
    /// first step and says the first line; the contract itself is laid on the board the moment the captain
    /// is standing at one (<see cref="PostTheMilkRunContractIfTheBoardIsClear"/>), because the purse and the
    /// address are both read off the berth the job is taken at.</summary>
    private void SeedMilkRun()
    {
        _milkRunStep = 1;
        _tutorialStep = StepTakeTheMilkRun;   // StartTutorial has already done this; a replay from anywhere else has not
        _milkRunPostedAshore = false;
        _milkRunSaidAtMs = double.NegativeInfinity;
        SayTheMilkRunLine(1);
        RequestVaultSave();
    }

    /// <summary>#1091's law, reaching the notice on the wall: if the lesson still wants its contract taken
    /// and it is not in the captain's hand, put it back on the board at the next look. Called from the
    /// checklist's own door (RelaunchTheLessonsPreyIfSheIsGone) — a run already accepted is left alone.
    /// </summary>
    private void PostTheMilkRunContractAgain()
    {
        if (_milkRunStep == 1 && TheMilkRun is null)
        {
            _milkRunPostedAshore = false;
        }
    }

    /// <summary>Put the checklist back on the row the reload found the lesson on. Only while the loop is
    /// still being walked: a finished milk run leaves <c>_tutorialStep</c> alone, because writing it past the
    /// last row would tick every OTHER lesson's card as done in the picker for a captain who never flew them.
    /// Nothing here raises the checklist — #292's law is that a loaded save never does.</summary>
    private void RestoreTheMilkRunsPlace()
    {
        if (_milkRunStep >= 1 && _milkRunStep <= MilkRunLesson.StepCount)
        {
            _tutorialStep = StepTakeTheMilkRun + _milkRunStep - 1;
        }
    }

    // ── THE WATCHER ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Runs once a frame beside the other player-driven quest checks (Map.Sim.Tick ·
    /// LookAroundAndPickTheWarp). Reads the live world, closes at most ONE step per call, and says the next
    /// line as it becomes the thing to do. Cheap enough to sit in the loop: an int compare before anything
    /// else is touched, and nothing at all for the captain who never took the lesson.</summary>
    private void WatchTheMilkRun()
    {
        if (_milkRunStep <= 0 || _milkRunStep >= MilkRunFinished)
        {
            return;   // never taken, or the loop is walked
        }

        PostTheMilkRunContractIfTheBoardIsClear();

        if (_frameNowMs - _milkRunSaidAtMs < MilkRunLineSpacingMs || !MilkRunStepIsDone(_milkRunStep))
        {
            return;
        }

        // Through the checklist's own one writer, so the row ticks over exactly as the other lessons' do
        // (and the last step of the last track marks the captain no longer new, which AdvanceTutorial owns).
        AdvanceTutorial(StepTakeTheMilkRun + _milkRunStep - 1);
        _milkRunStep++;
        RequestVaultSave();

        if (_milkRunStep < MilkRunFinished)
        {
            SayTheMilkRunLine(_milkRunStep);
        }
    }

    /// <summary>Lay the contract on the board — once per trip ashore, at a berth, and never over a card the
    /// captain is already reading.</summary>
    private void PostTheMilkRunContractIfTheBoardIsClear()
    {
        if (!_deckMode || _dockedHavenId is null)
        {
            _milkRunPostedAshore = false;   // off the concourse; the board is a thing you walk up to again
            return;
        }

        if (_milkRunPostedAshore || _milkRunStep != 1 || TheMilkRun is not null
            || _pendingOffer is not null || _patronDrink is not null)
        {
            return;
        }

        if (TheMilkRunContractFor(_dockedHavenId) is { } posted)
        {
            _pendingOffer = posted;
            _milkRunPostedAshore = true;
        }
    }

    /// <summary>The lesson's contract, written by the board's own hand. The destination is the nearest OTHER
    /// clampable berth — nearest by how far out in the system it sits, which is the number the purse is
    /// scaled on anyway — because the loop this lesson teaches ENDS AT A COUNTER: a moon haven is reached by
    /// parking in its orbit and has no ⚓ to clamp and nobody behind a desk to pay you (#175), and step 7's
    /// line says "Dock, and the contract pays at the counter".
    ///
    /// <para>// FABLE: line needed — step 1 names "Drums from Enceladus to Titan", and that pair cannot be
    /// flown: Enceladus is a moon haven with no berth to take a contract at, and Titan is neither a haven
    /// nor clampable, so the run would end in an orbit with no counter in it. The lesson issues the real
    /// board's nearest berth-to-berth haul instead — the offer card names the true address — but line 1's
    /// two place-names are the one thing in this lane the world cannot make true.</para></summary>
    private Quest? TheMilkRunContractFor(string berthId)
    {
        if (NearestOtherBerth(berthId) is not { } dest)
        {
            return null;
        }

        // Stamped with the lesson's id so the watcher — and a reload, which restores quests by id — can find
        // this one run among the captain's other work. Everything else about it is the board's.
        return MakeCargoRunOffer(MilkRunLesson.BoardGiver, dest) is { } run
            ? run with { Id = MilkRunLesson.QuestId }
            : null;
    }

    /// <summary>The clampable berth nearest this one, measured on the heliocentric reach the purse is priced
    /// from. Ties break on id so the lesson issues the same job twice running.</summary>
    private CelestialBody? NearestOtherBerth(string berthId)
    {
        double here = HelioRadiusMeters(berthId);
        CelestialBody? best = null;
        double bestGap = double.MaxValue;
        foreach (CelestialBody b in _ephemeris?.Bodies ?? [])
        {
            if (b.Id == berthId || !IsDockableHaven(b))
            {
                continue;
            }

            double gap = Math.Abs(HelioRadiusMeters(b.Id) - here);
            if (gap < bestGap || (gap == bestGap && best is not null
                                  && string.CompareOrdinal(b.Id, best.Id) < 0))
            {
                best = b;
                bestGap = gap;
            }
        }

        return best;
    }

    // ── THE EIGHT GATES ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>Is step <paramref name="step"/> (1-based, matching <see cref="MilkRunLesson.Lines"/>) done?
    /// One row per line, read off the live world and nothing else — no flag the lesson sets for itself, so
    /// there is no way for the checklist to tick a step the ship did not actually fly.</summary>
    private bool MilkRunStepIsDone(int step) => step switch
    {
        // 1 · The contract is in hand. Taking it is the Accept on the card the board laid out.
        1 => TheMilkRun is not null,

        // 2 · A plan that is a whole trip: it starts by casting off from this berth and it ENDS at the
        // delivery berth, as a dock. Both halves matter — a route with no cast-off is not dock-to-dock, and
        // an arrival at some other body is not this contract's trip.
        2 => PlanBeginsWithCastOff && MilkRunArrivalIsPlotted,

        // 3 · The tank is full. #157's own sentence: the autopilot cannot quote what you did not load.
        3 => _reactionMassPulses >= ReactionMassCapacity,

        // 4 · Armed AT PLAN TIME (#955/#969 — the arrival is a THEN), with a rehearsal quote to read. The
        // budget is non-zero only after a rehearsal that came back deliverable, which is the same number
        // the banner and the arrive step's own line are showing.
        4 => MilkRunArrivalIsArmedAtPlanTime && _armedBudgetPulses > 0,

        // 5 · The departure fired itself. The cast-off row is the one the frame loop lands on exactly, at
        // its epoch (Map.Sim.Tick · ConsumeTheAccumulator → RunTheCastOffStep); when it has run, the clamp
        // is off and the row is gone from the live list. Both, so a hand-flown undock is not mistaken for it.
        5 => _dockedHavenId is null && !PlanStillHoldsACastOff,

        // 6 · Warp is running. Read off the EFFECTIVE warp, not the commanded one — near a body the clock
        // auto-drops, and a lesson that ticked on a number the game was not honouring would be teaching the
        // slider rather than the clock.
        6 => !Paused && _effectiveWarp > 1,

        // 7 · Clamped at the berth the contract names.
        7 => TheMilkRun is { DestBodyId: { } dest } && _dockedHavenId == dest,

        // 8 · Paid. TurnedIn is the one state that means the coin has changed hands (PayCompletedQuests),
        // and the clamp is what triggers it — which is the whole of "the contract pays at the counter".
        8 => TheMilkRun is { State: QuestState.TurnedIn },

        _ => false,
    };

    /// <summary>The plan ends by clamping onto the berth this contract is bound for.</summary>
    private bool MilkRunArrivalIsPlotted =>
        _arrive is { Kind: ArrivalStepRule.ArrivalKind.Dock } step
        && TheMilkRun is { DestBodyId: { } dest }
        && step.BodyId == dest;

    /// <summary>The arrival on the board is the plan-time promise, not a NOW arm — the sentence #955 shipped
    /// and step 7's line repeats. Same three terms ArrivePlanCompleteLine reads.</summary>
    private bool MilkRunArrivalIsArmedAtPlanTime =>
        _arrive is { } step && ArmedArrivalStillAhead && _armedOrbitBodyId == step.BodyId;

    /// <summary>Is a cast-off still waiting to run? Fired rows are pruned and struck rows are marked, so a
    /// live Undock row is the only thing that means "she has not left the berth on the plan's own word".
    /// </summary>
    private bool PlanStillHoldsACastOff
    {
        get
        {
            foreach (PlanNode n in _planNodes)
            {
                if (n.Kind == PlanStepKind.Undock && !n.Stale && !n.Executed)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // ── SAYING IT ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Say step <paramref name="step"/>'s line, on the channel every other lesson beat uses. Plain
    /// Status rank: this is teaching, not a plot-significant telling, and the checklist beside it is already
    /// carrying the same words as the row the captain is on.</summary>
    private void SayTheMilkRunLine(int step)
    {
        if (step < 1 || step > MilkRunLesson.StepCount)
        {
            return;
        }

        ShowPulseMessage(MilkRunLesson.Lines[step - 1]);
        _milkRunSaidAtMs = _frameNowMs;
    }
}
