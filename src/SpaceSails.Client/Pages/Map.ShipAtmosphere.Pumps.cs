using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE THRIFTY ROAD — her roughing pumps. Owner, at the board: <i>"I want to pump the air out but not
/// lose it with vacuum pumps. So I expect an option of vacuuming a space without losing the air"</i>, and
/// then the whole shape of it in one sentence: <i>"There should be 2 ways to evacuate the room air, one
/// by venting to space…"</i>
///
/// <para>The pump runs, the refill, and the two whole-ship handles — sealing her up and unsealing her.</para>
///
/// <para>Split out of <c>Map.ShipAtmosphere.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── The thrifty road ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HER ROUGHING PUMPS. Owner, at the board: <i>"I want to pump the air out but not lose it with vacuum
    /// pumps. So I expect an option of vacuuming a space without losing the air"</i>, and then the whole shape
    /// of it in one sentence — <i>"There should be 2 ways to evacuate the room air, one by venting to space in
    /// an emergency hurry and another where we more slowly pump it into our stores with rough vacuum pumps and
    /// it is kept in the ship."</i>
    ///
    /// <para>That is the derelict's economics exactly, and it was his to begin with (his own lab bench: a
    /// roughing pump does ~95% of the work). Cracking a valve is instant and throws the air away; the pump is
    /// slow and the air ends up in her tanks. The mechanical stage BANKS the fill early and the long tail
    /// afterwards only buys a pressure low enough to be lethal — so stopping at the rough mark is a real
    /// choice rather than an abort.</para>
    ///
    /// <para>THE GATE DOES NOT RELAX FOR BEING THRIFTY. The end state is a compartment of your own ship at
    /// vacuum, which is precisely what the captain's word is for; a pump that skipped it would be a loophole
    /// around the rule rather than a second road to the same place.</para>
    /// </summary>
    private readonly Dictionary<string, PumpRun> _shipPumps = [];

    /// <summary>The run this space is part of, if any — asked by every readout, so a compartment standing open
    /// to one that is being pumped knows it is being pumped.</summary>
    private PumpRun? ShipPumpOn(string space)
    {
        foreach (PumpRun run in _shipPumps.Values)
        {
            if (run.Volume.Contains(space, StringComparer.Ordinal))
            {
                return run;
            }
        }
        return null;
    }

    /// <summary>Start her pump on a compartment — and on whatever is standing open to it.</summary>
    private void StartShipPump(string room)
    {
        if (ShipPumpOn(room) is not null)
        {
            return;
        }

        ShipAuthority.VentAuthority authority = ShipAuthorityNow();
        if (authority == ShipAuthority.VentAuthority.None)
        {
            _shipBoardMessage = ShipAuthority.AskFor(room);
            RendererInterop.PlayCue("block");
            return;
        }

        HullVenting.Space space = ShipSpaceNow(room);
        HullVenting.VentReadiness readiness = HullVenting.Readiness(space);
        if (readiness != HullVenting.VentReadiness.Ready)
        {
            _shipBoardMessage = HullVenting.RefusalLine(readiness, room);
            RendererInterop.PlayCue("block");
            return;
        }

        // WHAT AM I ACTUALLY ABOUT TO EMPTY. Core's flood fill answers, across whatever hatches stand open —
        // asked with HER corridor's name, because the door graph has to know what the volume opens onto.
        IReadOnlyList<string> volume = HullVenting.SharedAtmosphere(
            room, ShipSpacesNow(), ShipLayout.SpineName);

        (double seconds, int charges) = HullVenting.PumpJob(volume);
        _shipPumps[string.Join("+", volume)] =
            new PumpRun(volume, seconds, charges, seconds, RoughBanked: false);

        // Spent by STARTING the run, exactly as by pulling the handle: one authorization, one act. Stopping
        // and restarting asks again, which is right — it is the same decision twice.
        SpendShipAuthority(authority);

        _shipBoardMessage = HullVenting.PumpRunningLine(room, seconds);
        ShipBoardLog(
            $"🛢 Pump started on {room} — her air goes to the tanks, not to space, "
            + $"{ShipAuthority.UnderAuthority(authority)}.");
        RendererInterop.PlayCue("board");
    }

    /// <summary>Stop it. Past the rough mark this is the THRIFTY FINISH rather than an abort: the fill is
    /// already banked, and all you give up is a pressure low enough to kill.</summary>
    private void StopShipPump(string room)
    {
        if (ShipPumpOn(room) is not { } run)
        {
            return;
        }

        _shipPumps.Remove(string.Join("+", run.Volume));
        _shipBoardMessage = run.RoughBanked
            ? $"Pump shut down. Her tanks kept what came out of {room}, and what is left in there is still " +
              "breathable — which is what stopping at the mark means."
            : $"Pump shut down early. Most of {room}'s air is still in {room}, and none of it is in her tanks.";
        RendererInterop.PlayCue("block");
    }

    /// <summary>Run her pumps. One clock per VOLUME, one rough mark, one payout — the spaces on a run were one
    /// atmosphere the whole time.</summary>
    private void AdvanceShipPumps(double dtSeconds)
    {
        if (OnWreck)
        {
            return;
        }

        // Her vacuum clocks run whether or not a pump is going — a compartment that has been open to space
        // for four minutes is a different fact from one opened a moment ago.
        foreach (string vented in _shipVented)
        {
            _shipVacuumSeconds[vented] = _shipVacuumSeconds.GetValueOrDefault(vented) + dtSeconds;
        }

        if (_shipPumps.Count == 0)
        {
            return;
        }

        foreach (string key in _shipPumps.Keys.ToList())
        {
            PumpRun run = _shipPumps[key];
            double before = run.SecondsLeft;
            double left = before - dtSeconds;

            // The rough mark belongs to the RUN and never to a variable outside the loop — that exact mistake
            // cost the wreck's board a silent charge leak, a room measured against the corridor's mark that
            // its own shorter clock could never cross.
            double roughAt = run.Total - HullVenting.PumpRoughSeconds;
            bool banked = run.RoughBanked;
            string label = run.Volume.Count > 1 ? $"{run.Volume.Count} spaces" : run.Volume[0];

            if (before > roughAt && left <= roughAt)
            {
                _shipReserve += run.Charges;
                banked = true;
                _shipBoardMessage = HullVenting.PumpRoughDoneLine(label);
                LogAutopilotEvent(
                    $"🛢 {label} roughed out — {run.Charges} fill(s) into her tanks ({_shipReserve}).");
                RendererInterop.PlayCue("reveal");
            }

            if (left > 0)
            {
                _shipPumps[key] = run with { SecondsLeft = left, RoughBanked = banked };
                if (_showShipBoard && !banked && _shipSelected is { } watching
                    && run.Volume.Contains(watching, StringComparer.Ordinal))
                {
                    _shipBoardMessage = HullVenting.PumpRunningLine(label, left);
                }
                continue;
            }

            _shipPumps.Remove(key);

            // Only NOW is any of it lethal. The fill was banked at the rough mark, a long time ago.
            foreach (string member in run.Volume)
            {
                if (!string.Equals(member, ShipLayout.SpineName, StringComparison.Ordinal))
                {
                    _shipVented.Add(member);
                    _shipVacuumSeconds[member] = 0.0;
                }
            }

            _shipBoardMessage = HullVenting.PumpDoneLine(label);
            ShipBoardLog($"🛢 Pumped {label} down — her tanks hold {_shipReserve} fills.");
            RendererInterop.PlayCue("alarm");
            RebuildShipDeck();
            RequestVaultSave();
        }
    }

    /// <summary>Put the air back, off her own tanks. Air comes back; nobody does.</summary>
    private void RefillShipCompartment(string room)
    {
        HullVenting.Space space = ShipSpaceNow(room);
        HullVenting.RefillReadiness refill = HullVenting.RefillState(space, _shipReserve);
        if (refill != HullVenting.RefillReadiness.Ready)
        {
            _shipBoardMessage = refill == HullVenting.RefillReadiness.NoReserve
                ? ShipAtmosphere.NoReserveLine(room)
                : HullVenting.RefillRefusalLine(refill, room);
            RendererInterop.PlayCue("block");
            return;
        }

        _shipVented.Remove(room);
        _shipVacuumSeconds.Remove(room);   // air in it again: the clock has nothing to count
        _shipReserve--;

        _shipBoardMessage = ShipAtmosphere.RefilledLine(room, _shipReserve);
        ShipBoardLog($"🫁 {room} back to pressure — {_shipReserve} fills left in her tanks.");
        RendererInterop.PlayCue("reveal");
        RebuildShipDeck();
        RequestVaultSave();
    }

    /// <summary>THE REFLEX, on her own hull. Owner, on the derelict's version: <i>"lock all doors would be
    /// nice also :-D … for that heat of the moment feel … a kind of rescue thing to do to contain any leaks
    /// … and oxygen from going to feed the fire."</i> Which is what her board is FOR, nearly always — a fire,
    /// a leak, or something the med bay has a name for.</summary>
    private void SealHerUp()
    {
        int dogged = 0, held = 0;
        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            HullVenting.Space space = ShipSpaceNow(room.Name);
            if (space.DoorShut)
            {
                continue;
            }
            if (HullVenting.DoorHeldByPressure(space, ShipCorridorPressurised))
            {
                held++;
                continue;
            }

            _shipDoorsShut.Add(room.Name);
            dogged++;
        }

        _shipBoardMessage = HullVenting.SealTheShipLine(dogged, held);
        RendererInterop.PlayCue(dogged > 0 ? "board" : "block");

        if (dogged > 0)
        {
            ShipBoardLog($"🔒 {dogged} hatches dogged from the board.");
            RebuildShipDeck();
            RequestVaultSave();
        }
    }

    /// <summary>And open her up again — every hatch the pressure is not holding.</summary>
    private void UnsealHer()
    {
        int opened = 0, held = 0;
        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            HullVenting.Space space = ShipSpaceNow(room.Name);
            if (!space.DoorShut)
            {
                continue;
            }
            if (HullVenting.DoorHeldByPressure(space, ShipCorridorPressurised))
            {
                held++;
                continue;
            }

            _shipDoorsShut.Remove(room.Name);
            opened++;
        }

        _shipBoardMessage = HullVenting.UnsealTheShipLine(opened, held);
        RendererInterop.PlayCue(opened > 0 ? "board" : "block");

        if (opened > 0)
        {
            ShipBoardLog($"🔓 {opened} hatches undogged from the board.");
            RebuildShipDeck();
            RequestVaultSave();
        }
    }
}
