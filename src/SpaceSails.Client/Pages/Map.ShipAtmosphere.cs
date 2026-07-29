using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// HER OWN DOORS, AND HER OWN DAMAGE CONTROL. Owner, after a weekend of it on other people's ships:
/// <i>"I think our ship should also have these controls. They are so cool and it would be consistent in the
/// universe."</i> Then, looking at her: <i>"we don't even have the doors in our own ship :-D"</i> And on
/// where the board belongs: <i>"I like that the vent is in engineering."</i>
///
/// <para>Consistency in the universe was his whole argument and it is the right one — but it earns its keep
/// twice over. A captain who has learned this board on their own ship, in port, with nothing at stake, walks
/// onto a derelict already knowing what the handles do. The safest ship in the game is the tutorial for the
/// most dangerous one, and neither of them has to say so.</para>
///
/// <para>The rules are the SAME rules: <see cref="HullVenting"/> for the atmosphere,
/// <see cref="HullVenting.SharedAtmosphere"/> for what is standing open to what,
/// <see cref="HullVenting.Readiness"/> for whether the handle moves,
/// <see cref="HullVenting.DoorHeldByPressure"/> for the ten tonnes in the frame. What differs is
/// <see cref="ShipAtmosphere"/> — her own tanks, her berths, and the crew asleep in them — and the gate in
/// front of the irreversible half, <see cref="ShipAuthority"/>.</para>
///
/// <para>THE FIRST CUT WAS A SIGN THAT SAID "BOARD". Owner, standing at her valves: <i>"those panels won't
/// open there, so implementation is not there"</i> — right on both counts. It printed a status line and had no
/// valves at all; and both of her boards were unreachable anyway, because
/// <see cref="DeckPlan.NearestConsoleSpot"/> returned the first console in array order rather than the nearest
/// and an earlier console always took the key. This is the board.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Which of her compartments are dogged shut. Empty on a ship at peace, which is nearly always
    /// — and that is the point: it costs nothing to have and everything to lack.</summary>
    private readonly HashSet<string> _shipDoorsShut = new(StringComparer.Ordinal);

    /// <summary>Which of her compartments are standing open to space. Empty on any ship anybody would care
    /// to serve on.</summary>
    private readonly HashSet<string> _shipVented = new(StringComparer.Ordinal);

    /// <summary>What is left in her tanks, in compartment-fills. Her plant makes more; not quickly.</summary>
    private int _shipReserve = ShipAtmosphere.ReserveFills;

    /// <summary>Whether her board is up.</summary>
    private bool _showShipBoard;

    /// <summary>The compartment the board is pointed at.</summary>
    private string? _shipSelected;

    /// <summary>
    /// THE CAPTAIN'S WORD, AND THE ROOM IT WAS GIVEN FOR. Owner: <i>"our venting must have captains ok
    /// mechanism in it, like firing a shot or looting"</i>, and on the second board:
    /// <i>"the bridge position needs captains ok to vent"</i>.
    ///
    /// <para>It carries the compartment's NAME, which is the whole difference between this and a confirmation
    /// dialogue: changing the selection revokes it, exactly the way re-aiming revokes a boarding
    /// authorization. The captain cannot arm one compartment and blow another. And because the gate is on the
    /// ACT rather than on the console, it applies identically at the valves and at the helm — which is what
    /// makes the repeater a convenience rather than a loophole.</para>
    /// </summary>
    private string? _shipAuthorized;

    /// <summary>The board's last word — an outcome, a refusal, or the authority it is holding.</summary>
    private string? _shipBoardMessage;

    /// <summary>
    /// Whether her corridor still holds air. It always does: there is no handle on her board that empties the
    /// volume the captain is standing in, and a corridor pump is the one piece of the derelict's board she has
    /// not been given yet.
    ///
    /// <para>It is named rather than written as <c>true</c> at six call sites because the shared rules
    /// (<see cref="HullVenting.DoorHeldByPressure"/> and friends) all take the corridor's pressure as an
    /// argument, and the day she gets a corridor pump this is the one line that has to change.</para>
    /// </summary>
    private const bool ShipCorridorPressurised = true;

    /// <summary>Her deck as it stands right now: dogged hatches AND the ones ten tonnes of atmosphere is
    /// holding shut.</summary>
    private DeckPlan ShipDeckNow()
    {
        HashSet<string> blocked = BlockedShipDoors();
        return blocked.Count == 0 ? DeckPlan.Ship : DeckPlan.ShipWith(blocked);
    }

    /// <summary>Every doorway of hers the captain cannot walk through: dogged by hand, or loaded by a pressure
    /// differential. The wreck's law applied to her — a vented compartment's hatch is a wall whether anybody
    /// dogged it or not, and she can do that to herself now.</summary>
    private HashSet<string> BlockedShipDoors()
    {
        var blocked = new HashSet<string>(StringComparer.Ordinal);
        foreach (HullVenting.Space space in ShipSpacesNow())
        {
            if (HullVenting.DoorwayBlocked(space, ShipCorridorPressurised))
            {
                blocked.Add(space.Name);
            }
        }
        return blocked;
    }

    /// <summary>Rebuild after a hatch moves. A dogged hatch is a WALL and the walls are BUILT — the lesson
    /// the wreck taught by letting a Reever walk through a door the player could see was shut.</summary>
    private void RebuildShipDeck()
    {
        // ONLY WHEN THE DECK UNDER FOOT IS ACTUALLY HERS. Docked, _deckPlan holds the haven welded on, and a
        // rebuild that did not check would swap a whole station for the bare ship because a cabin door moved.
        // The door controls only exist on her own deck, so this can only fire from there — but "can only" is
        // exactly the reasoning that put a moon constant in charge of a wreck four times this weekend.
        if (!OnWreck && _deckMode && string.IsNullOrEmpty(_havenName))
        {
            _deckPlan = ShipDeckNow();
        }
    }

    /// <summary>The compartment the captain is standing in aboard their own ship, or null in the corridor.</summary>
    private string? ShipCompartment() => ShipLayout.CompartmentAt(_avatarX, _avatarY);

    /// <summary>Dog or undog the hatch the captain is standing at.</summary>
    private void ToggleShipDoorAtHand()
    {
        ShipLayout.Room nearest = default;
        double best = double.MaxValue;

        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            DeckReachability.Point at = ShipLayout.DoorConsolePoint(room);
            double dx = at.X - _avatarX, dy = at.Y - _avatarY;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < best)
            {
                best = d2;
                nearest = room;
            }
        }

        if (nearest.Name is null)
        {
            return;
        }

        ToggleShipDoor(nearest.Name, atTheDoor: true);
    }

    /// <summary>One hatch, thrown either from the doorway or from the board. Same rules both ways, because a
    /// door does not care where the order came from — and because the wreck's board proved that throwing a
    /// switch for a door you are not standing at is the interesting version.</summary>
    private void ToggleShipDoor(string room, bool atTheDoor)
    {
        HullVenting.Space space = ShipSpaceNow(room);

        // TEN TONNES IN A FRAME. The only thing that stops one of her hatches moving is a pressure
        // differential — and she can make one now, which is exactly why this check has to exist on her board
        // as well as a derelict's. (The wreck learned it the expensive way: a vented room's hatch could never
        // be dogged again, and refilling needs it dogged, so a blown room could never be brought back.)
        if (HullVenting.DoorHeldByPressure(space, ShipCorridorPressurised))
        {
            Say(atTheDoor, HullVenting.PressureLockLine(room, space.Vented));
            RendererInterop.PlayCue("block");
            return;
        }

        // THE ONE REFUSAL SHE HAS AT THE DOOR ITSELF: you cannot dog a hatch with yourself on the wrong side
        // of it. Every control stands in the corridor, so shutting one always means shutting a room you are
        // NOT in — except that the captain can step into the room and press it through the doorway, which
        // would seal them in a cabin with no board and no reason.
        if (atTheDoor && !_shipDoorsShut.Contains(room) && ShipCompartment() == room)
        {
            ShowPulseMessage($"You would be dogging {room} from the inside. Step into the corridor.");
            RendererInterop.PlayCue("block");
            return;
        }

        if (!_shipDoorsShut.Remove(room))
        {
            _shipDoorsShut.Add(room);
        }

        bool shut = _shipDoorsShut.Contains(room);
        Say(atTheDoor, shut ? ShipAuthority.IsolatedLine(room) : ShipAuthority.ReleasedLine(room));

        LogAutopilotEvent(shut ? $"🔒 Dogged {room}." : $"🔓 Undogged {room}.");
        RendererInterop.PlayCue("board");
        RebuildShipDeck();
        RequestVaultSave();
    }

    /// <summary>Where a line goes: the deck's pulse when the captain is at the door, the board's own line when
    /// they are reading the panel. One helper, so no switch can be added that talks to the wrong one.</summary>
    private void Say(bool atTheDoor, string line)
    {
        if (atTheDoor)
        {
            ShowPulseMessage(line);
        }
        else
        {
            _shipBoardMessage = line;
        }
    }

    // ── Her board ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Press E at her valves, or at the bridge repeater: raise the board. The SAME board from both,
    /// which is the owner's own requirement — <i>"there must be controls to do so on the bridge"</i> — and one
    /// of the few things that separates owning a hull from boarding one.</summary>
    private void OpenShipVentPanel()
    {
        _showShipBoard = true;
        _shipBoardMessage = null;
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
        if (ShipAuthority.EvaluateVent(_shipSelected, _shipAuthorized) != ShipAuthority.VentIntent.Authorized)
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

        // THE WORD IS SPENT. One authorization, one compartment, one act — otherwise "the captain said yes"
        // becomes a standing permission, which is the thing this whole gate exists to refuse.
        _shipAuthorized = null;

        _shipBoardMessage = ShipAtmosphere.IsBerth(room)
            ? $"💨 {room} is open to space. {ShipAtmosphere.TheSuitsBesideTheBunks}"
            : $"💨 {room} is open to space. It cost you the air in it and nothing else.";

        LogAutopilotEvent($"💨 Vented {room} — on the captain's own authority, by name.");
        RendererInterop.PlayCue("alarm");
        RebuildShipDeck();
        RequestVaultSave();
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
        _shipReserve--;

        _shipBoardMessage = ShipAtmosphere.RefilledLine(room, _shipReserve);
        LogAutopilotEvent($"🫁 {room} back to pressure — {_shipReserve} fills left in her tanks.");
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
            LogAutopilotEvent($"🔒 {dogged} hatches dogged from the board.");
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
            LogAutopilotEvent($"🔓 {opened} hatches undogged from the board.");
            RebuildShipDeck();
            RequestVaultSave();
        }
    }

    // ── What the board reads ──────────────────────────────────────────────────────────────────────────

    /// <summary>One compartment as the shared rules see it THIS INSTANT: her stored state, plus where the
    /// captain happens to be standing.</summary>
    private HullVenting.Space ShipSpaceNow(string room) =>
        new(room,
            DoorShut: _shipDoorsShut.Contains(room),
            Vented: _shipVented.Contains(room),
            Infested: false,
            HoldsSurvivor: false,
            CaptainInside: ShipCompartment() == room);

    /// <summary>Her compartments as the shared rules see them — for the connectivity search, the board, and
    /// the walls.</summary>
    private IReadOnlyList<HullVenting.Space> ShipSpacesNow()
    {
        var spaces = new List<HullVenting.Space>(ShipLayout.Rooms.Length);
        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            spaces.Add(ShipSpaceNow(room.Name));
        }
        return spaces;
    }

    /// <summary>How much of her is currently one volume — the thing this board can tell you that nothing else
    /// can. On a ship at peace it is all of her and the number is dull; the day it is not all of her is the
    /// day the panel matters.</summary>
    private string ShipBreathingLine()
    {
        IReadOnlyList<string> corridor = HullVenting.SharedAtmosphere(
            ShipLayout.SpineName, ShipSpacesNow(), ShipLayout.SpineName);
        int spaces = ShipLayout.Rooms.Length + 1;

        if (_shipVented.Count > 0)
        {
            return $"{_shipVented.Count} compartment{(_shipVented.Count == 1 ? "" : "s")} open to space. "
                   + $"{corridor.Count} of {spaces} spaces still share the corridor's air.";
        }

        return corridor.Count == spaces
            ? "She is breathing as one ship, bow to stern."
            : $"{corridor.Count} of {spaces} spaces share the corridor's air. "
              + $"{spaces - corridor.Count} dogged off.";
    }

    /// <summary>The one word a compartment wears on her mimic — what it is DOING beats what it is.</summary>
    private (string Text, string Class) ShipAreaTag(string room)
    {
        if (_shipVented.Contains(room))
        {
            return ("VACUUM", "vent-tag");
        }
        if (ShipCompartment() == room)
        {
            return ("YOU", "vent-tag");
        }
        return _shipDoorsShut.Contains(room) ? ("DOGGED", "vent-tag") : ("", "vent-tag");
    }

    // ── Her mimic's geometry, from her own numbers ─────────────────────────────────────────────────────

    /// <summary>The frame of her mimic. Her hull runs x −24…30 and y −10…10; the box is that plus a margin, so
    /// the drawing carries her proportions rather than being a diagram of a ship in general.</summary>
    private static string ShipViewBox => "-25.5 -11.5 57 23";

    /// <summary>Her hull outline, read off the hull walls in <c>DeckPlan.BuildShip</c>: the pointed bow at
    /// x 30, the long flanks, and the aft quarters cut back to the transom. Y is negated for SVG, and she is
    /// symmetric about the corridor, so the same list serves either way round.</summary>
    private static string ShipHullOutline =>
        "30,0 20,-10 -18,-10 -24,-7 -24,7 -18,10 20,10";
}
