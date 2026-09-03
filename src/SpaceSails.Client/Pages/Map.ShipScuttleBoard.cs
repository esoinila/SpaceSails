using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// HER OWN CHARGES, AND THE TWO HANDS IT TAKES. Owner: <i>"that ship also has the scuttling charges... let's
/// have a captains approval mechanic for that also on our ship. :-D ... it is the last defence against the Borg
/// in Star Trek :-D"</i>, then the sentence that makes it a mechanic instead of an ending — <i>"they only need
/// to activate it for the borg to back down enough"</i> — and then, working out the Trek arrangement for a ship
/// with no first officer: <i>"I guess we need the another opinion we just don't have a desk for that on the
/// bridge yet."</i>
///
/// <para>She does not need a new desk. THE SECOND KEY IS THE CREW'S, read off the sheet that already exists on
/// the captain's own desk (<see cref="CrewTemp.StandingOf"/>) — which turns the crew temperature from a report
/// card into the load-bearing thing it was always meant to be, and delivers the promise written into
/// <c>docs/features/the-captains-character.md</c> §3: a monstrous captain with a devoted crew can turn both
/// keys, and a decent captain nobody trusts finds that nobody moves.</para>
///
/// <para>The timing is <see cref="Scuttle"/>'s, unchanged, because a captain who has overloaded a derelict's
/// reactor already knows how long ninety seconds feels. The rules about WHO may turn the keys are
/// <see cref="ShipScuttle"/>'s.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Whether her charge panel is up.</summary>
    private bool _showShipScuttlePanel;

    /// <summary>The captain's word against HER, by name — never delegable to damage control
    /// (<see cref="ShipScuttle.NeverDelegable"/>), and cleared when the panel closes.</summary>
    private bool _shipScuttleWordGiven;

    /// <summary>What the crew did when the captain reached for the second key, if they have been asked.</summary>
    private ShipScuttle.SecondKey? _shipScuttleSecondKey;

    /// <summary>Seconds left on her overload, or null when the charges are cold.</summary>
    private double? _shipChargesSeconds;

    /// <summary>The panel's last word.</summary>
    private string? _shipScuttleMessage;

    /// <summary>Whether the deterrent has already been spent on the pursuers watching this run — the message
    /// fires once per arming, not once per frame.</summary>
    private bool _shipScuttleDeterrentSaid;

    /// <summary>Press E at the charges.</summary>
    private void OpenShipScuttlePanel()
    {
        _showShipScuttlePanel = true;
        _shipScuttleMessage = null;
        RendererInterop.PlayCue("board");
    }

    private void CloseShipScuttlePanel()
    {
        _showShipScuttlePanel = false;

        // The word does not survive the panel being shut, exactly as the vent word does not. An authority left
        // armed behind a closed door is the "are you sure?" trap wearing a better coat.
        if (_shipChargesSeconds is null)
        {
            _shipScuttleWordGiven = false;
            _shipScuttleSecondKey = null;
        }
    }

    /// <summary>Say it, by name, against her. Still not the act — and still only half of it.</summary>
    private void GiveTheWordAgainstHer()
    {
        _shipScuttleWordGiven = !_shipScuttleWordGiven;
        if (!_shipScuttleWordGiven)
        {
            _shipScuttleSecondKey = null;
            _shipScuttleMessage = "You take your hand off the key. Nothing under the deck plates has changed.";
            RendererInterop.PlayCue("block");
            return;
        }

        _shipScuttleMessage = ShipScuttle.ArmedFor(ShipNameNow());
        ShipBoardLog($"☢ Captain's authority recorded against {ShipNameNow()} — the charges await the second key.");
        RendererInterop.PlayCue("board");
    }

    /// <summary>
    /// ASK THE CREW. This is the whole mechanic: the captain's own word is not enough, and what answers is not
    /// a rule but the people he has been sailing with — read off the same sheet his desk shows him.
    /// </summary>
    private void AskTheCrewForTheSecondKey()
    {
        if (!_shipScuttleWordGiven)
        {
            _shipScuttleMessage = ShipScuttle.AskFor(ShipNameNow());
            RendererInterop.PlayCue("block");
            return;
        }

        CrewTemp.Standing standing = CrewTemp.StandingOf(CrewVoyage());
        ShipScuttle.SecondKey answer = ShipScuttle.SecondVoice(standing);
        _shipScuttleSecondKey = answer;

        _shipScuttleMessage = ShipScuttle.SecondVoiceLine(answer)
                              + (answer == ShipScuttle.SecondKey.Refused
                                  ? " " + ShipScuttle.RefusedPointerLine
                                  : "");
        ShipBoardLog(answer == ShipScuttle.SecondKey.Refused
            ? "☢ The crew would not turn the second key."
            : "☢ The crew turned the second key.");
        RendererInterop.PlayCue(answer == ShipScuttle.SecondKey.Refused ? "block" : "board");
    }

    /// <summary>Both keys together. From here she is on a clock, and the recall window is the whole reason it is
    /// a decision rather than a toggle.</summary>
    private void TurnBothKeys()
    {
        if (!ShipScuttle.MayArm(_shipScuttleWordGiven, _dcStandingOrder)
            || _shipScuttleSecondKey is not { } second
            || second == ShipScuttle.SecondKey.Refused
            || _shipChargesSeconds is not null)
        {
            RendererInterop.PlayCue("block");
            return;
        }

        _shipChargesSeconds = Scuttle.OverloadSeconds;
        _shipScuttleDeterrentSaid = false;
        _shipScuttleMessage = ShipScuttle.KeysTurnedLine;
        ShipBoardLog($"☢ OVERLOAD ARMED on {ShipNameNow()} — {Scuttle.OverloadSeconds:0} seconds.");
        _shipAlerts.Raise(AlertKind.Collision, AlertSeverity.Red, "☢ OVERLOAD ARMED — her own charges", SimTime);
        RendererInterop.PlayCue("alarm");
        RequestVaultSave();
    }

    /// <summary>Back the keys out, while there is still a panel that will take them.</summary>
    private void BackTheKeysOut()
    {
        if (_shipChargesSeconds is not { } left)
        {
            return;
        }
        if (!Scuttle.CanAbort(left))
        {
            _shipScuttleMessage = Scuttle.PastRecallLine;
            RendererInterop.PlayCue("block");
            return;
        }

        _shipChargesSeconds = null;
        _shipScuttleWordGiven = false;
        _shipScuttleSecondKey = null;
        _shipScuttleMessage = Scuttle.AbortedLine;
        ShipBoardLog("☢ Overload called off — the keys come back out.");
        RendererInterop.PlayCue("board");
        RequestVaultSave();
    }

    /// <summary>
    /// Run her clock. Two jobs: the PA counts (<see cref="Scuttle.PaCall"/>, the same voice a derelict uses),
    /// and — the point of the whole thing — anybody chasing her reads the overload and breaks off.
    /// </summary>
    private void AdvanceShipCharges(double dtSeconds)
    {
        if (_shipChargesSeconds is not { } left || OnWreck)
        {
            return;
        }

        double before = left;
        left -= dtSeconds;

        if (Scuttle.PaCall(before, left) is { } call)
        {
            ShowPulseMessage(call);
            ShipBoardLog(call);
        }

        // THE DETERRENT, which is why a captain arms them at all. Owner: "they only need to activate it for the
        // borg to back down enough." An armed hull is worth nothing to anybody — no cargo, no debt, no bounty —
        // so the arithmetic of everyone chasing her changes the moment the note comes up half a tone.
        if (!_shipScuttleDeterrentSaid)
        {
            _shipScuttleDeterrentSaid = true;
            int scared = 0;
            for (int h = 0; h < _hunters.Count; h++)
            {
                HunterState hunter = _hunters[h];
                if (hunter.BrokenOff || hunter.CaughtPlayer)
                {
                    continue;
                }

                _hunters[h] = hunter with { BrokenOff = true };
                scared++;
                ShowPulseMessage(ShipScuttle.DeterrentLine(hunter.Callsign));
                ShipBoardLog($"☢ {hunter.Callsign} broke off — nobody boards a ship that is about to stop existing.");
                SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
            }

            if (!ShipScuttle.AnybodyToDeter(scared))
            {
                ShowPulseMessage(ShipScuttle.NobodyWatchingLine);
                ShipBoardLog(ShipScuttle.NobodyWatchingLine);
            }
        }

        if (left > 0)
        {
            _shipChargesSeconds = left;
            return;
        }

        _shipChargesSeconds = null;
        SheGoes();
    }

    /// <summary>
    /// THE CLOCK REACHED ZERO. One question decides the ending — was the captain still ON her — and the two
    /// answers are a death and a beginning (<see cref="ShipScuttle.Ending"/>), which is the owner's own
    /// objection answered: <i>"if you can not get away with ship, then propably not with shuttle either."</i>
    /// True about speed, and beside the point. The shuttle does not outrun anybody; the prize evaporates.
    /// </summary>
    private void SheGoes()
    {
        if (ShipScuttle.EndingFor(CaptainWasAboardHer()) == ShipScuttle.Ending.WentWithHer)
        {
            SheGoesWithHim();
            return;
        }

        SheGoesWithoutHim();
    }

    /// <summary>
    /// Was the captain aboard her when it happened?
    ///
    /// <para>Two signals say NO for certain and are the only ones this trusts: the shuttle is away with him in
    /// it, or he is standing on a surface. Everything else — walking her own deck, or ashore in a haven's bar
    /// while she sits at the berth — counts as aboard, because guessing generously about survival is how a
    /// mechanic quietly stops having stakes. (Blowing her AT a station berth is its own scene, and its own
    /// crime, and it is not this slice: see issue #525.)</para>
    /// </summary>
    private bool CaptainWasAboardHer() => _surface is null && _shuttleRun is null;

    /// <summary>
    /// He stayed. Through the SAME brain-backup death the collector, the impact and the regolith all use — the
    /// death machinery is shared, never duplicated — with her own cause, because a captain who scuttled his own
    /// ship and stood on her did not die of anything else.
    /// </summary>
    private void SheGoesWithHim()
    {
        if (_busted is not null)
        {
            return;   // already mid-reckoning; one death at a time
        }

        Warp = 1;
        _effectiveWarp = 1;

        string her = ShipNameNow();
        RendererInterop.PlayCue("alarm");
        RendererInterop.PlayCue("gameover");

        _busted = new BustedEncounter
        {
            HunterId = string.Empty,       // nobody collected her; the captain did
            HunterCallsign = her,
            Heat = 0,
            Seed = DiceRule.Seed("scuttled", (long)SimTime),
            Bribe = default,               // there is nobody to bribe at the end of your own countdown
            Phase = BustedEncounter.Stage.SurfaceEnd,   // the generic what-happened beat, data-driven by Cause
            Cause = DeathCause.Scuttled,
            DeathBodyName = _nearestBody?.Name,
        };

        string line = $"☢ {her} goes, with her captain aboard. The charges kept their end of the bargain.";
        LogAutopilotEvent(line);
        ShowPulseMessage(line);
        StateHasChanged();
    }

    /// <summary>
    /// HE GOT CLEAR — the castaway ending, and the one worth having. She goes; the hold goes with her; every
    /// pursuer's reason to care evaporates; and the captain is alive, shipless, and somebody's paperwork rather
    /// than somebody's payday.
    ///
    /// <para>Reuses the loss half of the rebirth machinery and NONE of the death half: the insurance kit hands
    /// over the hull nobody else wanted, but there is no clinic bill (he was never hurt), no successor captain
    /// (he lived), and his nerve is not reset — it is CHARGED, because watching your own ship go is not free
    /// whatever it bought.</para>
    /// </summary>
    private void SheGoesWithoutHim()
    {
        Warp = 1;
        _effectiveWarp = 1;

        RendererInterop.PlayCue("alarm");

        // The hold went with her, and so did anything hot in it — which is one of the few honest ways to make
        // evidence disappear, and the game should not pretend otherwise.
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        _hotCargo.Launder();

        // The hull nobody else wanted, from the same rule that supplies a rebirth's rustbucket — no clinic
        // bill, because nobody treated him for anything.
        BustedRule.ResurrectionKit kit =
            InsuranceRule.ApplyToRebirth(_insurance, SimTime, InsuranceRule.DefaultRebirth(WakeTankPulses())).Kit;

        _reactionMassPulses = Math.Min(kit.ReactionMassPulses, ReactionMassCapacityFor(kit.MassLevel));
        _slugAmmo = kit.SlugAmmo;
        _missileAmmo = kit.MissileAmmo;
        _massLevel = kit.MassLevel;
        _sensorLevel = kit.SensorLevel;
        _holdLevel = kit.HoldLevel;
        _telescopeLevel = kit.TelescopeLevel;
        RebuildSensor();

        // The excursion, if he was on one, ends here — there is nothing overhead to go back up to. Folded
        // the way the shuttle's own lift-off folds it (Map.Surface.cs), including the writ that followed his
        // heat down: a collector party is a body ON THAT GROUND, and the ground is gone.
        bool wasOnSurface = _surface is not null;
        if (wasOnSurface)
        {
            _surface = null;
            _reevers.Clear();
            _collectors.Clear();
            _lastNearestReeverRange = null;
        }
        _shuttleRun = null;

        // #525 · AND NOBODY IS CHASING WHAT IS NOT THERE. The deterrent breaks off whoever was watching at
        // the moment the keys turned — that is the mechanic, spent once per arming so the line is not said
        // every frame. It cannot cover a hunter who arrived DURING the ninety seconds, and on the old code he
        // went on flying his intercept at a hull that had stopped existing. Said with nothing at all: the
        // arithmetic changed, not the conversation, and the card is already up.
        for (int h = 0; h < _hunters.Count; h++)
        {
            HunterState hunter = _hunters[h];
            if (!hunter.BrokenOff && !hunter.CaughtPlayer)
            {
                _hunters[h] = hunter with { BrokenOff = true };
            }
        }

        // Picked up, and berthed on somebody's charity.
        string haven = WakeAtNearestHaven();
        if (wasOnSurface)
        {
            SetDeckForDock(null);
        }

        // Her own compartments come back with the new hull: no dogged hatches, no vacuum, full tanks.
        _shipDoorsShut.Clear();
        _shipVented.Clear();
        _shipVacuumSeconds.Clear();
        _shipPumps.Clear();
        _shipReserve = ShipAtmosphere.ReserveFills;
        _shipAuthorized = null;
        _shipScuttleWordGiven = false;
        _shipScuttleSecondKey = null;

        ApplyNerveShock(ShipScuttle.CastawayNerveCost, "you watched your own ship go");

        _shipEpitaph = new ShipEpitaph(
            ShipScuttle.CastawayLine,
            ShipScuttle.CastawaySurvivesLine,
            ShipScuttle.RescuedAtLine(haven));

        string line = $"☢ She is gone, and you are not. Picked up at {haven}, with the hull nobody else wanted.";
        LogAutopilotEvent(line);
        ShipBoardLog(line);

        // …and it is written down, by this ending rather than by a side effect of the wake. A berth's clamp
        // does ask for a save, but a haven with no berth to clamp to does not — and an ending that empties
        // the hold, launders the stamp on it and swaps the hull must never depend on which kind of rock
        // happened to be nearest. A reload does not resurrect her.
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>The card a surviving captain reads once. Three lines: what happened, what is still his, and who
    /// came for him.</summary>
    private sealed record ShipEpitaph(string Went, string Survives, string Rescue);

    private ShipEpitaph? _shipEpitaph;

    private void CloseShipEpitaph() => _shipEpitaph = null;

    /// <summary>What the panel calls her. The ship has no name of her own in the model yet, so it uses the one
    /// thing that is always true and always the captain's: his own command.</summary>
    private string ShipNameNow() => "THIS SHIP";

    /// <summary>Whether the charges can still be called off — for the panel and the clock strip.</summary>
    private bool ShipChargesCanAbort =>
        _shipChargesSeconds is { } left && Scuttle.CanAbort(left);
}
