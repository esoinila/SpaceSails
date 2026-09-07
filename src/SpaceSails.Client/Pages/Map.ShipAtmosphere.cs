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
///
/// <para>#251 · This file keeps her STATE and the handles the captain reaches by hand: which spaces are
/// dogged, which are open to space, the board log, the captain's word and what it is spent on, and the
/// doors themselves. The other three are named for what stands in front of them: <c>.Board</c> (the panel
/// raised at her valves or the bridge repeater, and the vent it authorises), <c>.Pumps</c> (the thrifty
/// road — roughing her down without losing the air, and sealing her up), and <c>.Read</c> (what the board
/// reads, and her mimic's geometry from her own numbers).</para>
///
/// <para>The family declares no static field, so the #1163 initializer hazard is absent by construction.
/// No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Which of her compartments are dogged shut. Empty on a ship at peace, which is nearly always
    /// — and that is the point: it costs nothing to have and everything to lack.</summary>
    private readonly HashSet<string> _shipDoorsShut = new(StringComparer.Ordinal);

    /// <summary>Which of her compartments are standing open to space. Empty on any ship anybody would care
    /// to serve on.</summary>
    private readonly HashSet<string> _shipVented = new(StringComparer.Ordinal);

    /// <summary>
    /// HOW LONG EACH OF HER COMPARTMENTS HAS BEEN OPEN TO SPACE. Owner, pointing at the derelict's board:
    /// <i>"see here now how there is a clock here for pumping down and vacuum."</i>
    ///
    /// <para>The wreck's clock counts up because the vacuum is doing work in there — the soak is what kills
    /// what it cannot see. On HER the number answers a different question and still matters: how long that
    /// compartment has been dead, which is how long the fire in it has been starved and how long ago you
    /// decided to spend the air.</para>
    /// </summary>
    private readonly Dictionary<string, double> _shipVacuumSeconds = [];

    /// <summary>
    /// HER BOARD KEEPS ITS OWN LOG, like a derelict's does. The wreck's board learned this the hard way: a
    /// panel can only ever show ONE line, so anything that happened while the captain was looking elsewhere —
    /// a pump banking, a hatch that would not move — was gone before it could be read.
    /// </summary>
    private readonly List<string> _shipBoardLog = [];

    /// <summary>The most her board keeps. Long enough to cover an incident, short enough to stay a log rather
    /// than a transcript.</summary>
    private const int ShipBoardLogDepth = 14;

    /// <summary>Every act her board takes goes to the ship's event log AND to the board's own — one call, so a
    /// new switch cannot be added that quietly writes to only one of them.</summary>
    private void ShipBoardLog(string line)
    {
        LogAutopilotEvent(line);

        _shipBoardLog.Add(line);
        if (_shipBoardLog.Count > ShipBoardLogDepth)
        {
            _shipBoardLog.RemoveAt(0);
        }
    }

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
    /// THE STANDING ORDER TO DAMAGE CONTROL. Owner: <i>"Let's add separate option to captains desk to
    /// authorize the back of the ship repair station"</i>, <i>"(even while the bridge still works)"</i>,
    /// <i>"like we have the fire at will checkbox"</i>.
    ///
    /// <para>So it is that checkbox, for the other set of handles. A captain who must be asked about every
    /// hatch during a fire is a captain who gets asked at the worst possible moment; delegation is a decision
    /// made once, in the calm, from the desk. The board says so out loud every time it acts under it
    /// (<see cref="ShipAuthority.StandingOrderStandsLine"/>) — a delegated act must never read like an
    /// unauthorized one.</para>
    /// </summary>
    private bool _dcStandingOrder;

    /// <summary>
    /// THE LIMITED PRE-OK. Owner: <i>"we have the authorize next shot, that kind of authorize damage control
    /// next vent action is missing, it would be usefull as limited pre-ok from captain."</i>
    ///
    /// <para>The rung between the two that existed. The word at the board is specific and needs the captain to
    /// be standing there; the standing order is open-ended and forever. This is the captain clearing the NEXT
    /// act from the desk without knowing yet which compartment it will be — which is exactly the shape of a
    /// fire, and exactly how a cleared shot already works (<c>_shotAuthorized</c>). Spent by the act.</para>
    /// </summary>
    private bool _dcNextActionCleared;

    /// <summary>Which of the captain's authorities answers for the compartment on the board right now.</summary>
    private ShipAuthority.VentAuthority ShipAuthorityNow() =>
        ShipAuthority.AuthorityFor(_shipSelected, _shipAuthorized, _dcStandingOrder, _dcNextActionCleared);

    /// <summary>Clear damage control's next act, or take the clearance back. Mirrors AUTHORIZE NEXT SHOT down
    /// to the wording, because it is the same promise about a different set of handles.</summary>
    private void AuthorizeNextDamageControlAct()
    {
        _dcNextActionCleared = !_dcNextActionCleared;

        string line = _dcNextActionCleared
            ? ShipAuthority.NextActionClearedLine
            : ShipAuthority.NextActionWithdrawnLine;
        ShowPulseMessage(line);
        _shipBoardMessage = line;
        LogAutopilotEvent(_dcNextActionCleared
            ? "✍ Damage control cleared for its next act."
            : "✍ Damage control's clearance withdrawn.");
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>Spend whatever authority answered, if spending is what it is for. A standing order stands; a
    /// named word and a next-act clearance are used up by the thing they permitted.</summary>
    private void SpendShipAuthority(ShipAuthority.VentAuthority authority)
    {
        if (!ShipAuthority.IsSpentByTheAct(authority))
        {
            return;
        }

        if (authority == ShipAuthority.VentAuthority.NamedWord)
        {
            _shipAuthorized = null;
        }
        else
        {
            _dcNextActionCleared = false;
        }
    }

    /// <summary>
    /// Whether her bridge repeater has a bus behind it. Owner: <i>"So there needs to be standing authorization
    /// here when the bridge is alive. If bridge is not alive then it should be doable from the rear of the
    /// ship. Like in the reevers infested ship."</i>
    ///
    /// <para>On a derelict that is a forty-year-old fact. On her it is REACHABLE, which is better: open your
    /// own bridge to space and the panel standing in it stops answering, so the ship you learned the board on
    /// becomes the ship the board was designed for. What is left is mechanical, aft — and a standing order
    /// given while the bridge still worked stands there.</para>
    /// </summary>
    private bool ShipBridgeAlive => !_shipVented.Contains("BRIDGE");

    /// <summary>Hand damage control the captain's authority, or take it back. Called from the captain's desk,
    /// beside the weapons authority, because that is where a captain's standing orders live.</summary>
    private void ToggleDamageControlAuthority()
    {
        _dcStandingOrder = !_dcStandingOrder;

        // A standing order supersedes any single-compartment word — leaving one armed underneath it would
        // mean withdrawing the order silently re-armed a room the captain named an hour ago.
        _shipAuthorized = null;

        string line = _dcStandingOrder
            ? ShipAuthority.StandingOrderGivenLine
            : ShipAuthority.StandingOrderWithdrawnLine;
        ShowPulseMessage(line);
        _shipBoardMessage = line;
        LogAutopilotEvent(_dcStandingOrder
            ? "⚓ Damage control given standing authority over her compartments."
            : "⚓ Damage control's standing authority withdrawn.");
        RendererInterop.PlayCue("board");
        RequestVaultSave();
    }

    /// <summary>Which of her two boards the captain is standing at — the aft valves, or the bridge repeater.
    /// Decided by WHERE THEY ARE rather than by a label, the same rule the wreck's pressure doors use: a name
    /// parsed back out of display text is a bug waiting for a rename.</summary>
    private bool AtTheBridgeRepeater()
    {
        double dxRepeater = ShipLayout.BridgeRepeaterStation.X - _avatarX;
        double dyRepeater = ShipLayout.BridgeRepeaterStation.Y - _avatarY;
        double dxValves = ShipLayout.ValveStation.X - _avatarX;
        double dyValves = ShipLayout.ValveStation.Y - _avatarY;
        return (dxRepeater * dxRepeater) + (dyRepeater * dyRepeater)
               < (dxValves * dxValves) + (dyValves * dyValves);
    }

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
        // #1119 item 1 · ALWAYS A BUILD, never the singleton — this answer becomes `_deckPlan`, the plan
        // `AppendRegion` writes into, and handing back `DeckPlan.Ship` would put the process-wide ship deck
        // back under the captain's feet the moment every hatch happened to be open. Same deck, own object.
        return DeckPlan.ShipWith(blocked);
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
        //
        // KNOWN GAP, stated rather than hidden: her consoles DO travel into the docked complex
        // (HavenInterior seeds itself from DeckPlan.Ship.Consoles), so the board can be opened and her
        // hatches dogged while clamped on — and this guard means the walls of the docked plan will not
        // change to match. The state is real and the map is stale until she casts off. Closing it properly
        // means teaching HavenInterior.DockedDeck about her door state (it takes a station and an unlock
        // set today, and caches on both), which is a bigger change than the board it would serve.
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

        ShipBoardLog(shut ? $"🔒 Dogged {room}." : $"🔓 Undogged {room}.");
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
}
