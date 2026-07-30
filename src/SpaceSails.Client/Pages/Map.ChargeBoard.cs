using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #523 · THE CHARGE BOARD, aft with the machinery. Owner, in her engine room: <i>"I like you make a PR about
/// how spaceships manage their electric charge build up and make us a panel about it into the rear of the ship.
/// Make an issue, research the topic and make a panel. Now that desk is kind of lame on the ship, so let's add
/// some kind of on automatic there."</i>
///
/// <para>He was right that the console was lame and righter than he knew about why. The charge mechanic has been
/// live since the Electric Universe layer: the hull takes the local potential, and
/// <see cref="SensorModel.ChargeGlowFactor"/> means a charged hull is SEEN FROM THREE TIMES AS FAR. A pirate has
/// been paying a stealth tax for the whole game, and the only instrument that mentioned it was a joke generator
/// that halved the number and told you about a cat.</para>
///
/// <para>So the console keeps the dump — the joke is good and the act is real — and grows the board around it:
/// what she is sitting at, what is driving it, what it is costing you in the one currency that matters
/// (visibility), and the owner's automatic. Rules in <see cref="HullCharge"/>, where a test can reach them.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Whether her charge board is up.</summary>
    private bool _showChargeBoard;

    /// <summary>
    /// The plasma contactor — the owner's "on automatic". A hollow cathode that ties the hull to the ambient and
    /// holds it there without anybody watching a gauge. Costs expellant while it runs, which is what keeps it a
    /// decision rather than a switch nobody would ever turn off.
    /// </summary>
    private bool _contactorOn;

    /// <summary>How long the boards behind the panel have been soaking up high-energy electrons. Deliberately
    /// never rendered as a number: this is the second system in the game to obey the wreck sensor's law — the
    /// instrument says something is happening, never what.</summary>
    private double _internalSoakSeconds;

    /// <summary>Whether the ozone hint has been given for this soak, so it lands once rather than every frame.</summary>
    private bool _internalHintGiven;

    /// <summary>Expellant the cathode has used but not yet been charged for, in fractions of a pulse. The tank
    /// is counted in whole pulses and a contactor sips — without this, a rate below one pulse per frame rounds
    /// to free, and a running cost that rounds to free is not a cost.</summary>
    private double _contactorOwedPulses;

    /// <summary>Whether the board has already said the cathode is losing on this stretch of river, so the line
    /// lands once per stream rather than every frame.</summary>
    private bool _contactorOutmatchedSaid;

    /// <summary>The board's last word.</summary>
    private string? _chargeBoardMessage;

    private void OpenChargeBoard()
    {
        _showChargeBoard = true;
        _chargeBoardMessage = null;
        RendererInterop.PlayCue("board");
    }

    private void CloseChargeBoard() => _showChargeBoard = false;

    /// <summary>What the hull is sitting at right now, 0…1.</summary>
    private double HullChargeNow() => System.Math.Clamp(_ship.Charge, 0, 1);

    /// <summary>The local potential she is being dragged toward — the environment, not her state.</summary>
    private double AmbientChargeNow() =>
        _plasma?.AmbientCharge(_ship.Position, SimTime) ?? 0.0;

    /// <summary>Turn the automatic on or off. Refuses when there is nothing to spray, because a cathode with no
    /// expellant is a cathode.</summary>
    private void ToggleContactor()
    {
        if (!_contactorOn && _reactionMassPulses <= 0)
        {
            _chargeBoardMessage = HullCharge.ContactorDryLine;
            RendererInterop.PlayCue("block");
            return;
        }

        _contactorOn = !_contactorOn;
        _chargeBoardMessage = _contactorOn ? HullCharge.ContactorOnLine : HullCharge.ContactorOffLine;
        LogAutopilotEvent(_contactorOn
            ? "🜁 Plasma contactor running — she will be held near the ambient potential."
            : "🜁 Plasma contactor secured — she will take the local potential and wear it.");
        RendererInterop.PlayCue("board");
        RequestVaultSave();
    }

    /// <summary>
    /// Run the charge systems: the automatic holds her down and spends expellant doing it, and the buried charge
    /// nobody can read keeps climbing regardless — because a contactor clamps the HULL, and the boards behind the
    /// panel are a different problem with a different answer (shielding and time).
    /// </summary>
    private void AdvanceChargeSystems(double dtSeconds)
    {
        if (_plasma is null || dtSeconds <= 0)
        {
            return;   // a Newtonian scenario has no charge layer at all
        }

        double ambient = AmbientChargeNow();

        // THE AUTOMATIC. It holds her near the plasma's own potential — never at zero, because a contactor
        // clamps to the ambient rather than to nothing, and because leaving a little on her is what keeps
        // running it cheaper than being seen rather than free.
        if (_contactorOn)
        {
            if (_reactionMassPulses <= 0)
            {
                _contactorOn = false;
                ShowPulseMessage(HullCharge.ContactorDryLine);
                LogAutopilotEvent("🜁 The contactor ran dry and secured itself.");
            }
            else
            {
                // Expellant is counted in WHOLE pulses, and a cathode sips. So the fractional cost banks up
                // and is drawn a pulse at a time — otherwise a rate below one pulse per frame would round to
                // free, which is how a running cost quietly becomes no cost at all.
                _contactorOwedPulses += HullCharge.ContactorPulseCost(dtSeconds);
                while (_contactorOwedPulses >= 1.0 && _reactionMassPulses > 0)
                {
                    _contactorOwedPulses -= 1.0;
                    _reactionMassPulses--;
                }

                // WHAT IT CAN ACTUALLY HOLD HER AT, given what this space is throwing at her. Lab 43: the
                // cathode out-argues the cold dark, middling space and the inner halo without noticing — and
                // LOSES inside a plasma stream, where 23.8 mA arrives against the ~10 mA it emits. So the hold
                // is a target rather than a constant, and the stream is the one place the automatic cannot keep
                // you quiet. Ride the river or go unheard; not both.
                double holdAt = HullCharge.ContactorHoldTarget(ambient);
                if (_ship.Charge > holdAt)
                {
                    _ship = _ship with { Charge = holdAt };
                }

                if (!HullCharge.ContactorWinsHere(ambient) && !_contactorOutmatchedSaid)
                {
                    _contactorOutmatchedSaid = true;
                    ShowPulseMessage(HullCharge.ContactorOutmatchedLine);
                    LogAutopilotEvent(HullCharge.ContactorOutmatchedLine);
                }
                else if (HullCharge.ContactorWinsHere(ambient))
                {
                    _contactorOutmatchedSaid = false;   // she left the river; say it again next time
                }
            }
        }

        // AND THE ONE IT CANNOT HELP WITH. Only the high-energy end of the environment buries anything, so cold
        // space is free and a stream is expensive — and the hull gauge reads exactly the same either way.
        _internalSoakSeconds += HullCharge.InternalSoakGain(ambient, dtSeconds);

        if (!_internalHintGiven && HullCharge.InternalHintDue(_internalSoakSeconds))
        {
            _internalHintGiven = true;
            ShowPulseMessage(HullCharge.InternalHintLine);
            LogAutopilotEvent(HullCharge.InternalHintLine);
        }

        if (HullCharge.InternalArcDue(_internalSoakSeconds))
        {
            InternalArc();
        }
    }

    /// <summary>
    /// The buried charge lets go. Narrated as an event aboard rather than as a stat line, because the captain
    /// feels an internal arc in what stops working — and what it takes is the SENSOR, which is the cruellest
    /// honest choice: the ship that has been quietly glowing at everyone now cannot see them either.
    /// </summary>
    private void InternalArc()
    {
        _internalSoakSeconds = 0;
        _internalHintGiven = false;

        ShowPulseMessage(HullCharge.InternalArcLine);
        LogAutopilotEvent(HullCharge.InternalArcLine);
        _shipAlerts.Raise(AlertKind.Collision, AlertSeverity.Amber, "⚡ INTERNAL ARC — a board is gone", SimTime);
        RendererInterop.PlayCue("alarm");

        // One notch off the sensor fit, floored — a repairable loss rather than a spiral, and the reason the
        // board says out loud that it cannot see this coming.
        if (_sensorLevel > 0)
        {
            _sensorLevel--;
            RebuildSensor();
            LogAutopilotEvent("📡 Sensor fit down a notch — the arc found the receiver boards.");
        }

        ApplyNerveShock(NervePips.PipUnit, "a board let go behind the panel");
        RequestVaultSave();
    }
}
