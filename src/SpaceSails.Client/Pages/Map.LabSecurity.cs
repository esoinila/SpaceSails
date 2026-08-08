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

    /// <summary>Press a door. Walks it round <see cref="LockedDoor.Next"/>, which never skips a state — so a
    /// captain cannot lock something by accident on the way past.</summary>
    private void WorkTheDoor(string doorId)
    {
        if (!_labDoors.TryGetValue(doorId, out LockedDoor.State was))
        {
            return;
        }

        // #736 · Every answer this board gives is said ON the board. The door being worked is two rooms away
        // and the board's own backdrop is over the world, so a line pulsed from here is in the DOM and not on
        // the screen — #680's law, arriving at the panel the owner asked for in #409's own words.
        LockedDoor.State now = LockedDoor.Next(was, _hasVantarCard);
        if (now == was)
        {
            SayItWhereTheyAreLooking(LockedDoor.NoKeyLine);
            RendererInterop.PlayCue("block");
            return;
        }

        _labDoors[doorId] = now;
        SayItWhereTheyAreLooking(now switch
        {
            LockedDoor.State.Shut => LockedDoor.ShutLine,
            LockedDoor.State.Locked => LockedDoor.LockedLine,
            _ => "🚪 The door goes back and the dark past it is colder than the room you are in.",
        });

        // Said once, ever: the vent board's oldest lesson, moved to the ground.
        if (now == LockedDoor.State.Locked && !_saidWhichSide)
        {
            _saidWhichSide = true;
            LogAutopilotEvent(LockedDoor.WhichSideLine);
        }

        // A door is a WALL, so the ground has to be rebuilt or the map and the boot disagree — which is the
        // lesson #465 paid for on the ship's own hatches.
        RebuildSurfaceDeck();
        MakeNoiseAboard(0, 0, 0);   // no-op off a wreck; the lab's own ear is the garrison's, below
        AlertSweepersToNoise(_avatarX, _avatarY);
        RendererInterop.PlayCue("door");
        StateHasChanged();
    }

    private bool _saidWhichSide;

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
        ShowPulseMessage(
            "🗝 Vantar's card, on a lanyard, still round the neck of the chair. Every door in the mountain " +
            "answers to it — the ones ahead of you and the one you came in through.");
        LogAutopilotEvent("🗝 Vantar's card taken — every door in the lab now answers.");
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        StateHasChanged();
    }
}
