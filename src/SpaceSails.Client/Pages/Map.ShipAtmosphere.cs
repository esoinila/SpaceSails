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
/// <see cref="HullVenting.SharedAtmosphere"/> for what is standing open to what. A ship is a ship. What
/// differs is that hers has a live bridge repeater and a hull nobody has opened to space, so the board is a
/// tool rather than a weapon — until the day something comes through that airlock.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Which of her compartments are dogged shut. Empty on a ship at peace, which is nearly always
    /// — and that is the point: it costs nothing to have and everything to lack.</summary>
    private readonly HashSet<string> _shipDoorsShut = new(StringComparer.Ordinal);

    /// <summary>Her deck as it stands right now, doors included.</summary>
    private DeckPlan ShipDeckNow() =>
        _shipDoorsShut.Count == 0 ? DeckPlan.Ship : DeckPlan.ShipWith(_shipDoorsShut);

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

    /// <summary>Dog or undog the hatch the captain is standing at. Her doors have power and hinges and
    /// nothing wrong with them, so unlike a derelict's this never fights back.</summary>
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

        // THE ONE REFUSAL SHE HAS: you cannot dog a hatch with yourself on the wrong side of it. Every
        // control stands in the corridor, so shutting one always means shutting a room you are NOT in —
        // except that the captain can walk into the room and press it through the doorway, which would seal
        // them in a cabin with no board and no reason.
        if (!_shipDoorsShut.Contains(nearest.Name) && ShipCompartment() == nearest.Name)
        {
            ShowPulseMessage($"You would be dogging {nearest.Name} from the inside. Step into the corridor.");
            RendererInterop.PlayCue("block");
            return;
        }

        if (!_shipDoorsShut.Remove(nearest.Name))
        {
            _shipDoorsShut.Add(nearest.Name);
        }

        bool shut = _shipDoorsShut.Contains(nearest.Name);
        ShowPulseMessage(shut
            ? $"🔒 {nearest.Name} dogged. Her gauge holds steady — both sides still have air."
            : $"🔓 {nearest.Name} undogged.");
        RendererInterop.PlayCue("board");
        RebuildShipDeck();
        RequestVaultSave();
    }

    /// <summary>
    /// Her board. Same panel, same rules, same law — a ship is a ship.
    ///
    /// <para>Deliberately NOT a second implementation. The compartments are handed to the same
    /// <see cref="HullVenting"/> functions the wreck's board calls, which is the entire reason the owner's
    /// "consistent in the universe" instinct was worth following: one set of rules, learned once.</para>
    /// </summary>
    private void OpenShipVentPanel()
    {
        var lines = new List<string>();
        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            bool shut = _shipDoorsShut.Contains(room.Name);
            lines.Add($"{(shut ? "🔒" : "🔓")} {room.Name}");
        }

        IReadOnlyList<HullVenting.Space> spaces = ShipSpacesNow();
        IReadOnlyList<string> corridor = HullVenting.SharedAtmosphere(HullVenting.SpineName, spaces);

        // What the board is FOR, stated as the thing it can tell you that nothing else can: how much of your
        // own ship is currently one volume. On a ship at peace that is all of her, and the number is dull.
        // The day it is not all of her is the day this panel matters.
        string breathing = corridor.Count == ShipLayout.Rooms.Length + 1
            ? "She is breathing as one ship, bow to stern."
            : $"{corridor.Count} of {ShipLayout.Rooms.Length + 1} spaces share the corridor's air. "
              + $"{ShipLayout.Rooms.Length + 1 - corridor.Count} dogged off.";

        ShowPulseMessage($"⚙ ATMOSPHERE — {breathing}  {string.Join("  ", lines)}");
        RendererInterop.PlayCue("board");
    }

    /// <summary>Her compartments as the shared rules see them. Nothing aboard is vented or infested — she is
    /// a working ship — so this is door state and nothing else, which is exactly what the connectivity
    /// search needs.</summary>
    private IReadOnlyList<HullVenting.Space> ShipSpacesNow()
    {
        var spaces = new List<HullVenting.Space>(ShipLayout.Rooms.Length);
        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            spaces.Add(new HullVenting.Space(
                room.Name,
                DoorShut: _shipDoorsShut.Contains(room.Name),
                Vented: false,
                Infested: false,
                HoldsSurvivor: false,
                CaptainInside: ShipCompartment() == room.Name));
        }
        return spaces;
    }
}
