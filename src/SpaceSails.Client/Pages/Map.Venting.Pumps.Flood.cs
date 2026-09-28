using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE SPINE TAG, AND FLOODING, UNSEALING AND SEALING THE WHOLE SHIP.
///
/// <para>Split out of <c>Map.Venting.Pumps.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>The corridor's own line on the mimic, built exactly the way a compartment's is: what it is
    /// doing now beats what it was, and the clock is never dropped.</summary>
    private (string Text, string Class) SpineTag()
    {
        if (PumpOn(HullVenting.SpineName) is { } pumping)
        {
            return ($"PUMPING {HullVenting.SoakLabel(pumping.SecondsLeft)}",
                    pumping.RoughBanked ? "vent-spine-tag banked-tag" : "vent-spine-tag pumping-tag");
        }
        if (!_spinePressurised)
        {
            return _spineVacuumSeconds >= YearsOfVacuumSeconds
                ? ("VACUUM", "vent-spine-tag")
                : ($"VACUUM {HullVenting.SoakLabel(_spineVacuumSeconds)}", "vent-spine-tag");
        }
        return CaptainCompartment() is null ? ("YOU", "vent-spine-tag here-tag") : ("", "vent-spine-tag");
    }

    /// <summary>Compartments the flood would reach: at vacuum, and standing open to a vented corridor. One
    /// volume, one pressure — the equalisation valve played backwards.</summary>
    private IReadOnlyList<string> FloodableRooms()
    {
        var open = new List<string>();
        if (_spinePressurised)
        {
            return open;   // the corridor already has air; a room at a time is the only honest way
        }

        foreach ((string name, HullVenting.Space s) in _ventSpaces)
        {
            if (s.Vented && !s.DoorShut)
            {
                open.Add(name);
            }
        }
        open.Sort(System.StringComparer.Ordinal);
        return open;
    }

    /// <summary>What bringing her whole hull back would cost right now.</summary>
    private int FloodCost() => HullVenting.WholeShipRefillCost(FloodableRooms().Count);

    /// <summary>Open the reserve wide. Fills the corridor and every compartment standing open to it; a
    /// dogged hatch stays dead, which is the whole tactic.</summary>
    private void FloodTheShip()
    {
        if (_wreck is null)
        {
            return;
        }
        if (_spinePressurised)
        {
            _ventMessage = HullVenting.WholeShipAlreadyFullLine;
            RendererInterop.PlayCue("block");
            return;
        }

        IReadOnlyList<string> rooms = FloodableRooms();
        int cost = HullVenting.WholeShipRefillCost(rooms.Count);
        if (_refillCharges < cost)
        {
            _ventMessage = HullVenting.WholeShipRefillRefusal(cost, _refillCharges);
            RendererInterop.PlayCue("block");
            return;
        }

        _refillCharges -= cost;
        _spinePressurised = true;
        _spineVacuumSeconds = 0.0;

        foreach (string name in rooms)
        {
            // AIR COMES BACK. NOBODY DOES — the law the single-room refill is built on, and the flood is
            // not an exception to it. The soak clock resets because the room has air in it again; nothing
            // that the vacuum finished comes back with it.
            _ventSpaces[name] = _ventSpaces[name] with { Vented = false, VacuumSeconds = 0 };
        }

        int sealedLeftDead = _ventSpaces.Values.Count(s => s.Vented && s.DoorShut);
        _ventMessage = HullVenting.WholeShipRefillLine(rooms.Count, cost, sealedLeftDead);
        BoardLog($"🌬 The hull comes back to pressure — {cost} charges spent, {_refillCharges} left.");
        RendererInterop.PlayCue("reveal");
        RebuildWreckDeck();
        RequestVaultSave();
    }

    /// <summary>The way back out: undog every hatch the pressure is not holding. On a hull at uniform
    /// vacuum that is all of them. Owner: "I want to unlock all the doors after the ship is in vacuum."</summary>
    private void UnsealTheShip()
    {
        if (_wreck is null)
        {
            return;
        }

        int opened = 0, held = 0;
        foreach (string name in _ventSpaces.Keys.ToList())
        {
            HullVenting.Space s = SpaceNow(name);
            if (!s.DoorShut)
            {
                continue;
            }
            if (HullVenting.DoorHeldByPressure(s, _spinePressurised))
            {
                held++;
                continue;
            }

            _ventSpaces[name] = _ventSpaces[name] with { DoorShut = false };
            opened++;
        }

        _ventMessage = HullVenting.UnsealTheShipLine(opened, held);
        RendererInterop.PlayCue(opened > 0 ? "board" : "block");

        if (opened > 0)
        {
            // An open doorway stops being a wall, and the walls are BUILT. Skipping this is how a door the
            // player can see standing open goes on stopping them (and stopping bullets).
            RebuildWreckDeck();
            BoardLog($"🔓 {opened} hatches undogged from the board.");
            MakeNoiseAboard(0, 0, LoudEarshot);
            RequestVaultSave();
        }
    }

    /// <summary>THE REFLEX. Dog every hatch that will move, in one press. Owner: "lock all doors would be
    /// nice also :-D … for that heat of the moment feel."</summary>
    private void SealTheShip()
    {
        if (_wreck is null)
        {
            return;
        }

        int dogged = 0, held = 0;
        foreach (string name in _ventSpaces.Keys.ToList())
        {
            HullVenting.Space s = SpaceNow(name);
            if (s.DoorShut)
            {
                continue;
            }
            if (HullVenting.DoorHeldByPressure(s, _spinePressurised))
            {
                held++;
                continue;
            }

            _ventSpaces[name] = _ventSpaces[name] with { DoorShut = true };
            dogged++;
        }

        _ventMessage = HullVenting.SealTheShipLine(dogged, held);
        RendererInterop.PlayCue(dogged > 0 ? "board" : "block");

        if (dogged > 0)
        {
            // A dogged hatch is a WALL, and the walls are built rather than inferred. Skipping this is how
            // a shut door lets a Reever walk through it.
            RebuildWreckDeck();
            BoardLog($"🔒 {dogged} hatches dogged from the board.");

            // Eight doors slamming down the length of a dead ship is not a quiet thing to do.
            MakeNoiseAboard(0, 0, LoudEarshot);
            RequestVaultSave();
        }
    }
}
