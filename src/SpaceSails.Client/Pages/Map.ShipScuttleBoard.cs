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
    /// The clock reached zero with the captain still aboard. Through the SAME brain-backup death the collector,
    /// the impact and the regolith all use — the death machinery is shared, never duplicated — with her own
    /// cause, because a captain who scuttled his own ship and stayed on her did not die of anything else.
    /// </summary>
    private void SheGoes()
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

    /// <summary>What the panel calls her. The ship has no name of her own in the model yet, so it uses the one
    /// thing that is always true and always the captain's: his own command.</summary>
    private string ShipNameNow() => "THIS SHIP";

    /// <summary>Whether the charges can still be called off — for the panel and the clock strip.</summary>
    private bool ShipChargesCanAbort =>
        _shipChargesSeconds is { } left && Scuttle.CanAbort(left);
}
