using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// WHAT THE BOARD READS, AND WHAT IT DRAWS — one compartment as the shared rules see it THIS INSTANT
/// (her stored state, plus where the captain happens to be standing), the breathing line, the area tags,
/// and her mimic's geometry taken from her own numbers.
///
/// <para>The mimic's frame is her hull plus a margin, so the drawing carries HER proportions rather than
/// being a diagram of a ship in general.</para>
///
/// <para>Split out of <c>Map.ShipAtmosphere.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public sealed partial class Map
{
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

    /// <summary>
    /// The one word a compartment wears on her mimic — what it is DOING beats what it is.
    ///
    /// <para>PUMPING COMES FIRST, and it carries its clock. Owner, watching a pump run on her board and seeing
    /// nothing on the map: <i>"I expect to see the pumping here now as we have on the reever infested salvage
    /// ship."</i> Quite right — the derelict's board has shown a running pump on the compartment since the day
    /// several could run at once, because the board is the only place that can tell you which rooms are working
    /// and how far along each one is. A run you can only infer from a sentence is a run you will forget you
    /// started.</para>
    /// </summary>
    private (string Text, string Class) ShipAreaTag(string room)
    {
        if (ShipPumpOn(room) is { } pumping)
        {
            // It counts DOWN, unlike the vacuum soak: this clock has a known end. Past the rough mark the
            // colour changes, because from there on the air is already hers and the rest is just pressure.
            return ($"PUMPING {HullVenting.SoakLabel(pumping.SecondsLeft)}",
                    pumping.RoughBanked ? "vent-tag banked-tag" : "vent-tag pumping-tag");
        }
        if (_shipVented.Contains(room))
        {
            double open = _shipVacuumSeconds.GetValueOrDefault(room);
            return (open > 0 ? $"VACUUM {HullVenting.SoakLabel(open)}" : "VACUUM", "vent-tag");
        }
        if (ShipCompartment() == room)
        {
            return ("YOU", "vent-tag here-tag");
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

    /// <summary>Her corridor's own tag on the mimic, built as markup because Razor reserves
    /// <c>&lt;text&gt;</c> for control flow. It says where the captain is, the way the wreck's spine does.</summary>
    private string ShipCorridorTagSvg()
    {
        if (ShipCompartment() is not null)
        {
            return "";
        }

        return """<text class="vent-spine-tag here-tag" x="2" y="1.2" text-anchor="middle" """
               + """style="font-size:1.35px">YOU</text>""";
    }
}
