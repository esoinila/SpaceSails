using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #409+ · THE MOUNTAIN LAB, ON THE GROUND. The rules are <see cref="LockedDoor"/>, <see cref="LabSecurity"/> and
/// <see cref="SecretLab"/>; this is the doors moving, the countdown running, and the muscle standing up.
///
/// <para><b>The loop the owner asked for, in his order.</b> Force the hidden door → the alarm starts counting
/// (<i>"Alarm system panel maybe … something to try to hack"</i>) → walk in past two doors that can be shut and,
/// with his card, locked (<i>"Doors that lock is a cool feature"</i>) → find the board that throws all of them
/// from one wall (<i>"Surely some control panels based on the vent panel can be added 🤠"</i>) → beat the panel
/// and the muscle never wakes, or lose to it and every door in the mountain keys at once with the card two rooms
/// deeper than you are.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Where each lab door stands. Keyed by the door's stable id so the board and the ground cannot
    /// disagree about a door — they read one dictionary.</summary>
    private readonly Dictionary<string, LockedDoor.State> _labDoors = new(StringComparer.Ordinal);

    /// <summary>What the house is doing about the captain.</summary>
    private LabSecurity.State _labAlarm = LabSecurity.State.Dormant;

    /// <summary>How long the countdown has been running, and how many wrong answers the panel has been given.</summary>
    private double _labAlarmElapsed;
    private int _labHackFailures;

    /// <summary>Vantar's card, from the deepest room. The only thing that locks, unlocks, or survives a
    /// lockdown.</summary>
    private bool _hasVantarCard;

    /// <summary>Whether the door board is open, and the last roll the alarm panel showed — the house law is that
    /// the die is SHOWN, so it stays on screen after it lands.</summary>
    private bool _showDoorBoard;
    private DiceRoll? _labHackRoll;

    /// <summary>Seconds left before lockdown, or null when nothing is counting. Read by the clock strip.</summary>
    private double? LabAlarmSecondsLeft =>
        _labAlarm == LabSecurity.State.Armed
            ? LabSecurity.SecondsLeftAfter(_labAlarmElapsed, _labHackFailures)
            : null;

    private bool InsideTheLab => _surface is { Lab.HasLab: true } && _labDoors.Count > 0;

    // ── Arming, counting, and the thing at the end of the count ───────────────────────────────────────

    /// <summary>Reset for a fresh boarding — a lab remembers nothing between visits except what the vault
    /// carries.</summary>
    private void ResetLabSecurity()
    {
        _labDoors.Clear();
        _labAlarm = LabSecurity.State.Dormant;
        _labAlarmElapsed = 0;
        _labHackFailures = 0;
        _hasVantarCard = false;
        _labHackRoll = null;
        _showDoorBoard = false;
        _doorBoardOutcome = null;
        _alarmOutcome = null;
    }

    /// <summary>
    /// The hidden door coming off its frame is what wakes the system. Forcing, not opening: a door worked
    /// properly is a door the house has no opinion about, and a door taken off its frame is exactly what
    /// something forty years asleep is still listening for.
    /// </summary>
    private void ArmTheLabAlarm(IReadOnlyList<SecretLab.LabDoor> doors)
    {
        foreach (SecretLab.LabDoor d in doors)
        {
            // They failed CLOSED forty years ago, which is what a door does when the power goes: shut, but not
            // keyed. Keying takes a card, and the card is at the bottom.
            _labDoors[d.Id] = LockedDoor.State.Shut;
        }

        if (_labAlarm != LabSecurity.State.Dormant)
        {
            return;
        }

        _labAlarm = LabSecurity.State.Armed;
        _labAlarmElapsed = 0;
        ShowPulseMessage(LabSecurity.ArmedByLine);
        LogAutopilotEvent(LabSecurity.ArmedByLine);
        RendererInterop.PlayCue("alarm");

        // The muscle stands up with the countdown — on their feet, and the captain still unseen, which is the
        // state the whole scene is about.
        ShowPulseMessage(LabSecurity.GarrisonWakesLine);
        LogAutopilotEvent(LabSecurity.GarrisonWakesLine);
        SpawnSweepTeam(LabSecurity.GarrisonSize);
    }

    /// <summary>Run the countdown. Called once a frame from the sim.</summary>
    private void AdvanceLabAlarm(double dtRealSeconds)
    {
        if (_labAlarm != LabSecurity.State.Armed || _surface is null)
        {
            return;
        }

        _labAlarmElapsed += Math.Min(dtRealSeconds, 0.1);
        if (LabSecurity.SecondsLeftAfter(_labAlarmElapsed, _labHackFailures) > 0)
        {
            return;
        }

        Lockdown();
    }

    /// <summary>
    /// LOCKDOWN. Every door keyed at once — including the one behind the captain — and the only thing that opens
    /// them is two rooms deeper than they probably are. The owner's cool feature, turned against him.
    /// </summary>
    private void Lockdown()
    {
        _labAlarm = LabSecurity.State.LockedDown;

        foreach (string id in _labDoors.Keys.ToList())
        {
            _labDoors[id] = LockedDoor.State.Locked;
        }

        ShowPulseMessage(LabSecurity.LockdownLine);
        LogAutopilotEvent(LabSecurity.LockdownLine);
        LogAutopilotEvent(_hasVantarCard
            ? LabSecurity.LockdownWithTheCardLine
            : LabSecurity.LockdownWithoutTheCardLine);

        RendererInterop.PlayCue("alarm");
        ApplyNerveShock(NervePips.SightingPips * (int)NervePips.PipUnit, "every door in the mountain just keyed");
        RebuildSurfaceDeck();
        StateHasChanged();
    }

    // ── The doors themselves ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Press a door. Open ⇄ Shut by hand, and a KEYED one does not answer hands at all.
    ///
    /// <para>#563 · <b>THE CARD IS GONE FROM THIS METHOD.</b> Owner ruling, 2026-09-13: <i>"I like the time
    /// instead of a key, considering we have firepower and tools. We can create the same effect as needing a
    /// key by making it slow, too noisy, or dangerous in other ways."</i> So a lockdown is no longer a walk to
    /// a chair two rooms deeper than you are: it is a decision about what you are willing to spend. The
    /// shoulder (<see cref="LockedDoor.ForceSeconds"/>, heard every second it runs) or the round
    /// (<see cref="ShootTheLockNow"/>, heard by half the field, and the leaf never comes back).</para>
    ///
    /// <para>Pressed AT the door the shoulder goes on by itself, because standing at a keyed leaf and being
    /// told nothing would be the control doing nothing and saying nothing — the satchel's founding law.
    /// Pressed on the BOARD, two rooms away, there is no shoulder to put on it and the state does not move.</para>
    /// </summary>
    private void WorkTheDoor(string doorId)
    {
        if (!_labDoors.TryGetValue(doorId, out LockedDoor.State was))
        {
            return;
        }

        // #736 · Every answer this board gives is said ON the board. The door being worked is two rooms away
        // and the board's own backdrop is over the world, so a line pulsed from here is in the DOM and not on
        // the screen — #680's law, arriving at the panel the owner asked for in #409's own words.
        LockedDoor.State now = LockedDoor.Next(was);
        if (now == was)
        {
            // A keyed leaf, or one that is not a leaf any more. If the captain's own hands are on it, the
            // press starts the hold; from the board it is the fact and nothing else.
            if (LockedDoor.MayForce(was) && StandingAtLabDoor(doorId))
            {
                BeginForcingALabDoor(doorId);
                return;
            }
            SayItWhereTheyAreLooking(LockedDoor.Label(was));
            RendererInterop.PlayCue("block");
            return;
        }

        _labDoors[doorId] = now;
        SayItWhereTheyAreLooking(now switch
        {
            LockedDoor.State.Shut => LockedDoor.ShutLine,
            _ => "🚪 The door goes back and the dark past it is colder than the room you are in.",
        });

        // A door is a WALL, so the ground has to be rebuilt or the map and the boot disagree — which is the
        // lesson #465 paid for on the ship's own hatches.
        RebuildSurfaceDeck();
        MakeNoiseAboard(0, 0, 0);   // no-op off a wreck; the lab's own ear is the garrison's, below
        AlertSweepersToNoise(_avatarX, _avatarY);
        RendererInterop.PlayCue("door");
        StateHasChanged();
    }

    /// <summary>Are the captain's own hands on this leaf? The board throws doors from a wall two rooms away
    /// and a shoulder does not reach that far — asked of <see cref="NearestLabDoorId"/>, the one lookup the
    /// prompt and the press already share, so the two can never disagree about which door is under a hand.</summary>
    private bool StandingAtLabDoor(string doorId) =>
        _deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is { Kind: DeckPlan.ConsoleKind.LabDoor }
        && string.Equals(NearestLabDoorId(), doorId, StringComparison.Ordinal);

    /// <summary>
    /// #563 · THE SLOW ROAD, ON A KEYED LAB DOOR. The fourth force channel in the game and the only one
    /// priced at <see cref="LockedDoor.ForceSeconds"/> — the constant THIS kind of door owns. The other three
    /// force a seal that rotted (<c>ExpeditionRegions.DoorForceSeconds</c>, 5 s); this one forces bolts a
    /// security system shot home on purpose, and the garrison is awake the whole time.
    /// </summary>
    private void BeginForcingALabDoor(string doorId)
    {
        if (_surface is not { } ex || AnySlowThingUnderYourHands)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.LabDoor } spot)
        {
            return;
        }
        ex.LabDoorChannel = new DoorChannel { DoorId = doorId, AnchorX = spot.X, AnchorY = spot.Y };
        RendererInterop.PlayCue("board");
        ShowPulseMessage(LockedDoor.BeingForcedLine("the door") + " Hold position — step away to abort.");
    }

    /// <summary>One frame of that hold. Stepping off the anchor aborts (the same law every other channel in
    /// the game keeps), and every tick of it is HEARD — see <see cref="TheHoldIsHeard"/>.</summary>
    private void StepLabDoorChannel(double dtRealSeconds)
    {
        if (_surface is not { LabDoorChannel: { } ch } ex)
        {
            return;
        }
        double dx = _avatarX - ch.AnchorX, dy = _avatarY - ch.AnchorY;
        if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
        {
            ex.LabDoorChannel = null;
            ShowPulseMessage("You take your shoulder off it. The bolts are where they were.");
            return;
        }

        TheHoldIsHeard(ch);

        ch.Progress += dtRealSeconds / LockedDoor.ForceSeconds;
        if (ch.Progress >= 1.0)
        {
            ex.LabDoorChannel = null;
            _labDoors[ch.DoorId] = LockedDoor.State.Open;
            RebuildSurfaceDeck();
            RendererInterop.PlayCue("door");
            SayItWhereTheyAreLooking(LockedDoor.ForcedLine("The door"));
            AlertSweepersToNoise(_avatarX, _avatarY);
            StateHasChanged();
        }
    }

    /// <summary>Whether a lab door blocks the way — asked by the deck builder, so a shut door is a wall to the
    /// boot and to the eye exactly as a dogged hatch is.</summary>
    private bool LabDoorBlocks(string doorId) =>
        _labDoors.TryGetValue(doorId, out LockedDoor.State s) && !LockedDoor.Passable(s);

    // ── The board, and the panel ──────────────────────────────────────────────────────────────────────

    /// <summary>Which door the captain is standing at. The consoles carry the chamber name in their label, so
    /// the id is recovered by position — nearest door wins, exactly as NearestConsoleSpot does for everything
    /// else, and for the same reason: one lookup, or the prompt and the key disagree.</summary>
    private string NearestLabDoorId()
    {
        if (_surface is not { Lab: { HasLab: true } placement })
        {
            return string.Empty;
        }

        SecretLab.Region region = SecretLab.Build(
            _surface.Stop.Body.Id, MoonSurface.ExpeditionField(), placement.DoorX, placement.DoorY);

        string best = string.Empty;
        double bestRange = double.MaxValue;

        foreach (SecretLab.LabDoor d in region.Doors)
        {
            double dx = d.X - _avatarX, dy = d.Y - _avatarY;
            double range = (dx * dx) + (dy * dy);
            if (range < bestRange)
            {
                bestRange = range;
                best = d.Id;
            }
        }

        return best;
    }

    private void OpenAlarmPanel()
    {
        _showAlarmPanel = true;
        _alarmOutcome = null;   // #736: the last argument belongs to the last stand at this panel
        RendererInterop.PlayCue("board");
    }

    private void CloseAlarmPanel()
    {
        _showAlarmPanel = false;
        _alarmOutcome = null;
    }

    private bool _showAlarmPanel;

    /// <summary>#736 · What the last press of the panel answered, said INSIDE the panel. The panel stays open
    /// on every outcome except the lockdown — that is what makes it a panel you argue with — so a line pulsed
    /// from a failed hack played under this modal's own backdrop, blurred. Same law, same shape as #686's
    /// <c>_liftOutcome</c>; the autopilot log still keeps the record.</summary>
    private string? _alarmOutcome;

    /// <summary>The stack the panel is about to be argued with, for the offer line.</summary>
    private int LabHackStack => _surface is null ? 0 : LabSecurity.Modifiers(CurrentApproach()).Sum(m => m.Value);

    private LabSecurity.Approach CurrentApproach() => new(
        HasKeyCard: _hasVantarCard,
        LogsRead: _surface?.SecretLabLogsRead?.Count ?? 0,
        AlarmAlreadyRunning: _labAlarm == LabSecurity.State.Armed,
        NerveBand: (int)NerveModel.BandFor(_nerve),
        CarryingASentry: _surface?.Bots.Exists(b => !b.Deployed) ?? false);

    private void OpenDoorBoard()
    {
        _showDoorBoard = true;
        _doorBoardOutcome = null;
        RendererInterop.PlayCue("board");
    }

    private void CloseDoorBoard()
    {
        _showDoorBoard = false;
        _doorBoardOutcome = null;
    }

    /// <summary>#736 · What the last thrown door answered, said inside the board. A door two rooms away is
    /// the one thing on this board a captain cannot check by looking, which is exactly why the refusal —
    /// <c>LockedDoor.NoKeyLine</c>, the one that names what is missing — must not be behind the blur.</summary>
    private string? _doorBoardOutcome;

    /// <summary>
    /// Have a go at the panel. The die is SHOWN — target, stack, and the number — because a roll a captain
    /// cannot argue with is a roll they did not take part in.
    /// </summary>
    private void HackTheAlarm()
    {
        // #736 · The panel stays open through every one of these answers, so all of them are said on it —
        // the die is already shown here (the house law), and the sentence that reads the die had been
        // playing behind the panel's own blur.
        if (_labAlarm is not (LabSecurity.State.Armed or LabSecurity.State.Dormant))
        {
            SayItWhereTheyAreLooking(_labAlarm == LabSecurity.State.Disarmed
                ? "🔔 The panel is dark. You already had this argument."
                : LabSecurity.LockdownWithoutTheCardLine);
            return;
        }

        LabSecurity.Approach approach = new(
            HasKeyCard: _hasVantarCard,
            LogsRead: _surface?.SecretLabLogsRead?.Count ?? 0,
            AlarmAlreadyRunning: _labAlarm == LabSecurity.State.Armed,
            NerveBand: (int)NerveModel.BandFor(_nerve),
            CarryingASentry: _surface?.Bots.Exists(b => !b.Deployed) ?? false);

        ulong seed = DiceRule.Seed("lab-hack", (long)SimTime, _labHackFailures);
        DiceRoll roll = LabSecurity.Attempt(seed, approach);
        _labHackRoll = roll;

        if (LabSecurity.Beat(roll))
        {
            _labAlarm = LabSecurity.State.Disarmed;
            SayItWhereTheyAreLooking(LabSecurity.HackedLine);
            LogAutopilotEvent(LabSecurity.HackedLine);
            RendererInterop.PlayCue("reveal");
            StateHasChanged();
            return;
        }

        _labHackFailures++;
        double left = LabSecurity.SecondsLeftAfter(_labAlarmElapsed, _labHackFailures);
        SayItWhereTheyAreLooking(LabSecurity.FailedLine(left));
        LogAutopilotEvent(LabSecurity.FailedLine(left));
        RendererInterop.PlayCue("block");

        if (left <= 0)
        {
            Lockdown();
            return;
        }

        StateHasChanged();
    }

    /// <summary>Take the card. It is the only thing in the lab that changes what a door will do.</summary>
    private void TakeVantarsCard()
    {
        if (_hasVantarCard)
        {
            return;
        }

        _hasVantarCard = true;
        // #563 · FABLE: line needed. The owner ruled on 2026-09-13 that a locked door is TIME and never a
        // key, so the card stopped being a key the moment LockedDoor lost its hasKey parameter — it is a
        // CREDENTIAL THE ALARM PANEL RESPECTS (LabSecurity.Modifiers: "Vantar's card in the reader", +4) and
        // nothing else. Both sentences below are therefore the authored ones with the door clause DELETED
        // rather than rewritten: no prose was written by an implementation crew. What is wanted in its place
        // is one authored sentence saying what the card IS now — a man's building pass, good at the panel he
        // sat in front of, useless against hinges — and that sentence is Fable's to write.
        ShowPulseMessage(
            "🗝 Vantar's card, on a lanyard, still round the neck of the chair.");
        LogAutopilotEvent("🗝 Vantar's card taken.");
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        StateHasChanged();
    }
}
