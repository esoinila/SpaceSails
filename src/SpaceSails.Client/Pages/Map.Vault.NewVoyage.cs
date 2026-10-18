using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Vault.NewVoyage — the second half of ResetLiveStateForNewGame (#1374). Split out because Map.Vault.cs sits
// near the size gate's line, and because this half is one idea: everything BuildVault writes that the first half
// never named. Each line here is the blank-vault answer ApplyVault already knows how to give for the same field.
public partial class Map
{
    /// <summary>
    /// #1374 · …and the rest of the life that rides the vault. #1372 found the tag register missing from a reset whose
    /// contract is "the exact inverse of BuildVault"; the audit that followed read every section BuildVault writes
    /// against the reset and found these unreset: a New voyage in the same session left each of them standing, and the
    /// fresh thread's first autosave wrote the old life's field book, satchel, ground marks, burials and clocks into
    /// itself — permanently, because nothing ever clears a thread's vault but a new thread.
    ///
    /// <para>Where a section has a <c>Restore*(null)</c> (a file that lacks the section), that is the blank answer and
    /// is used, so the reset and a load of an empty vault cannot drift. The Core registers the Install* writers feed are
    /// STATIC, so each is handed its empty list again.</para>
    /// </summary>
    private void ForgetTheLifeThatRidesTheVault()
    {
        _fieldNotes = [];     // #587 · the one book (the pendant's first-opening latch moved off it in #1356)
        _caseThreads = [];    // #741 · the red lines across that book
        _satchel = [];        // #603 · what was carried on foot
        _workedUp.Clear();    // #1016 · which sheets are already in the book
        _groundMemory = GroundMemory.Restore(null);   // #563 · the marks left on every moon
        ForgetThePaperTrail();                        // #836 · which identity was handed to which man

        _oddBooksRead = [];
        _secretLabsFound.Clear();
        _workingStopsSinceShoreLeave = 0;
        _emptySealSpentOn = null;
        _observationWalkSpentOn = null;
        _observationWalkSightingAt = null;

        // #677/#1063/#1068/#1074 · the disclosure clock and everything keyed on it, and Core's static copies.
        _hallsOpened = [];
        _hallsBuried = [];
        _hallsDeclined = [];
        _hallsHandled = [];
        _hallsStopped = [];
        _hallsPreserved = [];
        InstallBurialRegister();
        InstallDeclineRegister();
        InstallQuietHandsRegister();
        InstallStopRegister();
        InstallPreserveRegister();

        RestoreShuttle(null);          // #1074 beat 5 · the boat, the seen grounds and the on-the-board latch
        RestoreClearedCollar(null);    // #525
        RestoreWalkInSection(null);    // #973 L5b
        RestoreFinderSection(null);    // #417
        RestoreTheWeatherSection(null);

        // #638 · the void's countdown, and the sweep that feeds it, back to "no clock running".
        _voidDeclaredDay = VoidRule.ClockNotRunning;
        _voidLastToldDay = VoidRule.NothingToldYet;
        _voidSweptDay = long.MinValue;
        _voidHavenInReach = true;

        ForgetTheFlightPlan();
        ForgetThePageSessionRiders();
        ForgetTheRunCounters();
        ForgetWhatAnAbsentSectionWouldHaveSaid();
    }

    /// <summary>
    /// The run-scoped counters and told-once latches: page state that is no vault field, so neither the BuildVault audit
    /// nor a Load/Import ever reset it, while a New voyage did only for the handful ResetLiveStateForNewGame named. Each
    /// of them LIES on a card when it outlives its life (the clinic-ledger "woken here before" on a first death, the
    /// climax that never fires, the flashback that is never filed, a first ride that is never told). Both doors reach
    /// it through <see cref="ForgetTheLifeThatRidesTheVault"/>.
    /// </summary>
    private void ForgetTheRunCounters()
    {
        _toldOnce = new ToldOnce();   // ToldOnce has no Clear on purpose: the run's holder is replaced when the run ends

        // #422 story pass · what the counters arc 2 gates its beats on
        _headOfficeBeatsSeen.Clear();
        _rebirthsSeen = 0;
        _insurancePosterReads = 0;
        _oracleStation = null;
        _oracleOpen = false;
        _oracleLine = null;
        _oracleDraw = 0;
        _oracleDrinks = 0;
        _lastQuietMinuteSimTime = double.NegativeInfinity;   // #620 · the new life has never taken a quiet minute
        _heatInsuranceAdvised = false;                        // #380 · the first heat of THIS run gets the advice

        // #1151 · the claim desk and the salesman's latch, for the same reason as the claims file
        _claimDesk = null;
        _repOffersToLodge = false;

        // #394 · the Ringside dedication and any live deflection gig
        _ringsideSaved = false;
        _deflection = null;
        _deflectionResolved = null;
        _deflectionRaiseMeters = 0;
        _deflectionLeftPort = false;

        // The books of the boardings (what they made hot reads off them), and the back-room hatches the last life cracked
        _lootLedger.Clear();
        _unlockedHatches.Clear();

        // #973 L2 · the rep's per-life latches are keyed on the bare life number, and every thread starts at life 1;
        // his bleed and rota clocks are the thread's
        _posterReadInLife = 0;
        _repSigningToldInLife = 0;
        _repMeetings = 0;
        _repVisitIndex = -1;
        _repVisitBody = null;
        _repWorkingHere = false;
        _repSaidPassing = false;
        _repMemory = NebulaRepVisit.Fresh;
    }

    /// <summary>
    /// ApplyVault applies a section only when the file has it, and its comments say an absent section "defaults calm" —
    /// true on a cold boot, false in a running tab. These are the new-game answers for the nerve, chandlery, upgrade and
    /// heat sections, so that an absent section really does mean the default and not the last life's values.
    /// </summary>
    private void ForgetWhatAnAbsentSectionWouldHaveSaid()
    {
        _nerve = NerveModel.Steady;
        _monolithSeen = false;
        _pendantFirstOpened = false;
        _extendedTanks = 0;
        _pills = Chandlery.MedKitFullStock;
        _heat = HeatState.None;
        _massLevel = 0;
        _sensorLevel = 0;
        _holdLevel = 0;
        _telescopeLevel = 0;
        RebuildSensor();
    }

    /// <summary>
    /// The page-session state that is not a vault field (so the BuildVault audit never saw it) but still belongs to one
    /// life: the collectors on the chase (a leftover hunter would be stepped across the whole epoch gap in one frame, and
    /// would keep refusing the long haul), the news wire and hail answers (the wire's hull-loss entries are claim
    /// receipts), and the mission-moment latch and ledger (a latched roadster raises no card when found again).
    /// </summary>
    private void ForgetThePageSessionRiders()
    {
        if (_interestTargetId is not null && _hunters.Any(h => h.Id == _interestTargetId))
        {
            _interestTargetId = null;
        }
        _hunters.Clear();
        _newsEvents.Clear();
        _hailAnswers.Clear();
        _momentsRaisedFor.Clear();
        _missionMomentLedger.Clear();
        _missionMomentQueue.Clear();
        _missionMoment = null;
    }

    /// <summary>
    /// The plotted plan and the flight intent are the page's, not the vault's (autopilot plans are never saved):
    /// a load or import arrives with no burns, no cast-off row, no arrival step and no armed autopilot, so an old
    /// life's pending undock cannot release the new life's berth and its armed transfer cannot fly the new ship.
    /// </summary>
    private void ForgetTheFlightPlan()
    {
        _planNodes.Clear();
        _selectedPlanNode = null;
        if (_openEditor is FlightEditorKind.Burn or FlightEditorKind.Arrive)
        {
            _openEditor = FlightEditorKind.None;
        }
        RebuildPlan();
        ClearArriveStep();
        _destinationBodyId = null;
        _armedOrbitBodyId = null;
        ResetAutopilotBudget();
        _autopilotStandDownReason = null;
        _dockReadyStatus = null;
    }

}
