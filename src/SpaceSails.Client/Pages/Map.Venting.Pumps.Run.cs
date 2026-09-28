using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · A PUMP RUN — stopping one, starting a pump-down, and advancing every run a frame.
///
/// <para>Split out of <c>Map.Venting.Pumps.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Stop a pump. Past the rough mark this is the THRIFTY finish, not an abort: the air is
    /// already in the tanks and all you are giving up is a pressure low enough to kill.</summary>
    private void StopPump(string name)
    {
        if (PumpOn(name) is not { } p)
        {
            return;
        }

        // Stopping a pump stops the RUN, not one room of it — the volume shares a machine as much as it
        // shares an atmosphere.
        _pumps.Remove(VolumeKey(p.Volume));
        name = p.Volume.Count > 1 ? $"{p.Volume.Count}-space" : p.Volume[0];
        _ventMessage = p.RoughBanked
            ? $"You shut the {name} pump down. It keeps what little is left in it, and the rest is aboard."
            : $"You shut the {name} pump down early. It still has most of its air, and none of it is yours.";
        RendererInterop.PlayCue("block");
    }

    /// <summary>Start the pump. Same interlock as the handle — a shut hatch, or you are pumping the ship.</summary>
    private void StartPumpDown(string name)
    {
        if (_wreck is null || PumpOn(name) is not null)
        {
            return;
        }

        // WHAT AM I ACTUALLY ABOUT TO EMPTY. Core's flood fill answers, across whatever doors stand open.
        IReadOnlyList<string> volume = AtmosphereAt(name);

        // Nothing left in any of it? Then there is nothing to pump, and that is the only refusal left —
        // the door interlock is gone, because an open door is not an error, it is a bigger volume.
        bool anythingToPump = false;
        foreach (string member in volume)
        {
            bool empty = member == HullVenting.SpineName
                ? !_spinePressurised
                : _ventSpaces.TryGetValue(member, out HullVenting.Space m) && m.Vented;
            if (!empty)
            {
                anythingToPump = true;
                break;
            }
        }
        if (!anythingToPump)
        {
            _ventMessage = HullVenting.RefusalLine(HullVenting.VentReadiness.AlreadyVented, name);
            RendererInterop.PlayCue("block");
            return;
        }

        (double seconds, int charges) = HullVenting.PumpJob(volume);
        _pumps[VolumeKey(volume)] = new PumpRun(volume, seconds, charges, seconds, RoughBanked: false);

        // THE ONE CHECK THE OWNER ASKED FOR, AND THE ONLY ONE: "make sure we don't evacuate a room by
        // accident of leaving its door open." It does not refuse — evacuating half a ship on purpose is a
        // real play — it NAMES what else is going, because the accident being guarded against is a hatch
        // left open and forgotten, never a decision.
        string reaches = HullVenting.PumpReachesFurtherLine(name, volume);
        _ventMessage = reaches.Length > 0
            ? reaches
            : CaptainCompartment() == name
                ? HullVenting.PumpUnderfootLine(name, seconds)
                : HullVenting.PumpRunningLine(name, seconds);
        RendererInterop.PlayCue("board");

        BoardLog(volume.Count > 1
            ? $"🛢 Pump started on {volume.Count} spaces at once: {string.Join(", ", volume)}."
            : $"🛢 Pump started on {name}.");

        // A pump running in a dead ship is a heartbeat, and it runs for the best part of a minute.
        if (name == HullVenting.SpineName)
        {
            MakeNoiseAboard(0, 0, LoudEarshot * 2);
        }
        else
        {
            MakeNoiseAboard(RoomCentre(name).X, RoomCentre(name).Y, LoudEarshot);
        }
    }

    /// <summary>Run the pumps. Each one owns its whole volume: one clock, one rough mark, one payout, and
    /// every space in it goes to vacuum together — because they were one atmosphere the entire time.</summary>
    private void AdvancePump(double dtSeconds)
    {
        if (_wreck is null || _pumps.Count == 0)
        {
            return;
        }

        foreach (string key in _pumps.Keys.ToList())
        {
            PumpRun run = _pumps[key];
            double before = run.SecondsLeft;
            double left = before - dtSeconds;

            // The rough mark, per RUN — the mechanical stage is done and the air is home. Everything after
            // it is the long pull to a killing pressure, which returns nothing to the tanks.
            //
            // This was once a variable declared outside the loop and overwritten whenever the corridor came
            // up in the enumeration, so any pump processed after it measured against the SPINE's mark — one
            // its own shorter clock starts below and can never cross. Charges silently vanished. It belongs
            // to the run or it belongs to nobody.
            double roughAt = run.Total - HullVenting.PumpRoughSeconds;
            bool banked = run.RoughBanked;
            string label = run.Volume.Count > 1
                ? $"{run.Volume.Count} spaces"
                : run.Volume[0];

            if (before > roughAt && left <= roughAt)
            {
                _refillCharges += run.Charges;
                banked = true;
                _ventMessage = HullVenting.PumpRoughDoneLine(label);
                BoardLog($"🛢 {label} roughed out — the air is in the tanks ({_refillCharges} charges).");
                RendererInterop.PlayCue("reveal");
            }

            if (left > 0)
            {
                _pumps[key] = run with { SecondsLeft = left, RoughBanked = banked };
                if (_showVentPanel && !banked && _ventSelected is { } watching
                    && run.Volume.Contains(watching, System.StringComparer.Ordinal))
                {
                    _ventMessage = HullVenting.PumpRunningLine(label, left);
                }
                continue;
            }

            _pumps.Remove(key);

            // Only NOW is any of it lethal. The charge was banked at the rough mark, a long time ago.
            foreach (string member in run.Volume)
            {
                if (member == HullVenting.SpineName)
                {
                    // The corridor goes to vacuum — and unlike cracking a valve, the air is IN THE TANKS.
                    // Same end state, opposite economics, and everything standing in it starts running out
                    // of time.
                    _spinePressurised = false;
                    _spineVacuumSeconds = 0.0;
                }
                else if (_ventSpaces.TryGetValue(member, out HullVenting.Space s))
                {
                    _ventSpaces[member] = s with { Vented = true, VacuumSeconds = 0.0, HoldsSurvivor = false };
                }
            }

            _ventMessage = HullVenting.PumpDoneLine(label);
            BoardLog($"🛢 Pumped {label} down — the air is in the tanks ({_refillCharges} charges).");
            RendererInterop.PlayCue("alarm");
            RebuildWreckDeck();
            RequestVaultSave();
        }
    }
}
