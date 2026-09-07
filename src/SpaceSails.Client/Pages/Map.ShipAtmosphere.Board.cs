using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// HER BOARD — press E at her valves, or at the bridge repeater, and the SAME board comes up. That is the
/// owner's own requirement (<i>"there must be controls to do so on the bridge"</i>) and one of the few
/// things that separates owning a hull from boarding one.
///
/// <para>Selecting a space, giving and withdrawing the captain's word, and the irreversible half itself:
/// venting a compartment to space.</para>
///
/// <para>Split out of <c>Map.ShipAtmosphere.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── Her board ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Press E at her valves, or at the bridge repeater: raise the board. The SAME board from both,
    /// which is the owner's own requirement — <i>"there must be controls to do so on the bridge"</i> — and one
    /// of the few things that separates owning a hull from boarding one.</summary>
    private void OpenShipVentPanel()
    {
        // THE REPEATER IS ONLY A REPEATER. Press it with her bridge open to space and it has nothing behind
        // it — the derelict's own arrangement, arrived at from the other direction, and the reason her aft
        // board is not redundant. The valves aft are mechanical and answer regardless.
        if (AtTheBridgeRepeater() && !ShipBridgeAlive)
        {
            ShowPulseMessage(ShipAuthority.DeadRepeaterLine(ShipLayout.ValveCompartment));
            RendererInterop.PlayCue("block");
            return;
        }

        _showShipBoard = true;
        _shipBoardMessage = _dcStandingOrder ? ShipAuthority.StandingOrderStandsLine : null;
        RendererInterop.PlayCue("board");
    }

    private void CloseShipBoard()
    {
        _showShipBoard = false;

        // THE WORD DOES NOT SURVIVE THE BOARD BEING SHUT. An authorization left armed behind a closed panel is
        // the "are you sure?" trap wearing a better coat: the captain would come back a watch later, press a
        // handle, and discover they had already agreed to something.
        _shipAuthorized = null;
    }

    /// <summary>Point the board at a compartment. THIS IS WHERE THE AUTHORITY LAPSES — it was given for one
    /// room by name, and moving the selection says so out loud rather than letting the captain find out at the
    /// handle.</summary>
    private void SelectShipSpace(string name)
    {
        if (_shipAuthorized is { } armed && !string.Equals(armed, name, StringComparison.Ordinal))
        {
            _shipAuthorized = null;
            _shipBoardMessage = ShipAuthority.LapsedFrom(armed);
        }
        else
        {
            _shipBoardMessage = null;
        }

        _shipSelected = name;
    }

    /// <summary>Give the word, for THIS compartment, by name. Still not the act — only the arming.</summary>
    private void GiveTheCaptainsWord(string room)
    {
        _shipAuthorized = room;
        _shipBoardMessage = ShipAuthority.ArmedFor(room);
        LogAutopilotEvent($"🔑 Captain's authority recorded for {room}.");
        RendererInterop.PlayCue("board");
    }

    /// <summary>Withdraw it. A captain who thinks better of it should not have to click somewhere else to say
    /// so.</summary>
    private void WithdrawTheCaptainsWord()
    {
        if (_shipAuthorized is not { } armed)
        {
            return;
        }

        _shipAuthorized = null;
        _shipBoardMessage = ShipAuthority.LapsedFrom(armed);
        RendererInterop.PlayCue("block");
    }

    /// <summary>
    /// PULL THE HANDLE ON YOUR OWN SHIP.
    ///
    /// <para>Two gates, in this order, and neither of them is a dialogue: the captain's word must name THIS
    /// compartment (<see cref="ShipAuthority.EvaluateVent"/> — the same shape as the boarding gate, which
    /// exists because the owner once got robbed by accident), and then the mechanical interlock must be
    /// satisfied (<see cref="HullVenting.Readiness"/> — hatch dogged, and not the room you are standing in).
    /// </para>
    /// </summary>
    private void VentShipCompartment(string room)
    {
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

        _shipVented.Add(room);
        _shipVacuumSeconds[room] = 0.0;   // her clock starts the moment the air leaves

        // AND WHATEVER ANSWERED IS SPENT, if spending is what it is for. One authorization, one act — a word
        // or a clearance that survived its own use would be a standing permission nobody granted.
        SpendShipAuthority(authority);

        _shipBoardMessage = ShipAtmosphere.IsBerth(room)
            ? $"💨 {room} is open to space. {ShipAtmosphere.TheSuitsBesideTheBunks}"
            : $"💨 {room} is open to space. It cost you the air in it and nothing else.";

        ShipBoardLog($"💨 Vented {room} — {ShipAuthority.UnderAuthority(authority)}.");
        RendererInterop.PlayCue("alarm");
        RebuildShipDeck();
        RequestVaultSave();
    }
}
