using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #488 · THE VALVE PANEL. Owner: <i>"I somehow vision the vent controls to be a pop-up with ship layout
/// and the ventable compartments marked as areas. There would also be lock switches to the doors there."</i>
///
/// <para>A damage-control mimic board, the Battlestar Galactica shape: the ship drawn as her own
/// compartments, each one a switch. Shut a door, read for life, blow the room. The board is aft in
/// ENGINEERING because the bridge panel is dead — so on an infested hull you walk toward the thing to
/// reach the tool that kills it, and back out past whatever you chose not to vent.</para>
///
/// <para>The rules are Core's (<see cref="HullVenting"/>, pure and tested). This is the board.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Live compartment state while aboard a wreck, keyed by compartment name. Built on boarding;
    /// the panel reads and writes it.</summary>
    private readonly Dictionary<string, HullVenting.Space> _ventSpaces = [];

    /// <summary>The life-sign readings taken so far, keyed by compartment. A reading is taken ONCE — you
    /// cannot stand at the panel re-rolling the die until it tells you what you want to hear.</summary>
    private readonly Dictionary<string, (DiceRoll Roll, HullVenting.LifeSign Sign)> _ventReads = [];

    private bool _showVentPanel;
    private string? _ventSelected;
    private string? _ventMessage;

    /// <summary>How many people the away team got off her alive. The reward for the careful road.</summary>
    private int _survivorsRescued;

    /// <summary>Prepare the board for a wreck: which rooms the thing has got into, and who sealed
    /// themselves in where. Seeded off the wreck, so a reload finds the same ship.</summary>
    private void PrepareVenting(in Derelict.Wreck wreck)
    {
        _ventSpaces.Clear();
        _ventReads.Clear();
        _ventSelected = null;
        _ventMessage = null;

        foreach ((string name, float x0, float x1, bool _) in WreckLayout.Compartments)
        {
            // The thing spread from the deep hold aft. Forward of amidships she is still clean — which is
            // why the away team can stand in the airlock at all.
            bool infested = wreck.Cause == Derelict.WreckCause.Infested && (x0 + x1) / 2 < 0;

            _ventSpaces[name] = new HullVenting.Space(
                Name: name,
                DoorShut: false,
                Vented: false,
                Infested: infested,
                HoldsSurvivor: HullVenting.HidesSurvivor(wreck.Id, name, wreck.Cause));
        }
    }

    /// <summary>Which compartment the captain is standing in right now — the panel refuses to blow it.</summary>
    private string? CaptainCompartment()
    {
        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            bool inX = _avatarX >= x0 && _avatarX <= x1;
            bool inY = top ? _avatarY < -WreckLayout.SpineHalfHeight : _avatarY > WreckLayout.SpineHalfHeight;
            if (inX && inY)
            {
                return name;
            }
        }
        return null;
    }

    /// <summary>The compartment as the board sees it THIS INSTANT — stored state plus where the captain
    /// happens to be standing.</summary>
    private HullVenting.Space SpaceNow(string name)
    {
        HullVenting.Space s = _ventSpaces.TryGetValue(name, out HullVenting.Space v)
            ? v
            : new HullVenting.Space(name, false, false, false, false);
        return s with { CaptainInside = CaptainCompartment() == name };
    }

    /// <summary>Press E at the valve station: raise the board.</summary>
    private void OpenVentPanel()
    {
        if (_wreck is null)
        {
            return;
        }
        _showVentPanel = true;
        _ventMessage = null;
        RendererInterop.PlayCue("board");
    }

    private void CloseVentPanel() => _showVentPanel = false;

    /// <summary>The dead bridge panel: a signpost, not a wall. Nobody should have to guess the answer is aft.</summary>
    private void TryDeadBridgePanel() => ShowPulseMessage(HullVenting.DeadBridgePanelLine);

    /// <summary>Throw a compartment's door switch. This is the interlock the vent handle checks.</summary>
    private void ToggleVentDoor(string name)
    {
        if (!_ventSpaces.TryGetValue(name, out HullVenting.Space s) || s.Vented)
        {
            return;
        }
        _ventSpaces[name] = s with { DoorShut = !s.DoorShut };
        RendererInterop.PlayCue("board");
    }

    /// <summary>Take the life-sign reading — ONCE. The die is rolled against the wreck's own seed and the
    /// compartment, and the result is kept: standing at the board re-reading until it says something
    /// comfortable is exactly the tension this mechanic exists to create.</summary>
    private void ReadLifeSigns(string name)
    {
        if (_wreck is not { } w || _ventReads.ContainsKey(name))
        {
            return;
        }

        ulong seed = DiceRule.Seed(w.Id, (long)name.GetHashCode(System.StringComparison.Ordinal));
        _ventReads[name] = HullVenting.Read(seed, SpaceNow(name));
        RendererInterop.PlayCue("reveal");
    }

    /// <summary>Pull the handle.</summary>
    private void VentCompartment(string name)
    {
        if (_wreck is null)
        {
            return;
        }

        HullVenting.Space space = SpaceNow(name);
        HullVenting.VentOutcome outcome = HullVenting.Vent(space);
        _ventMessage = outcome.Line;

        if (!outcome.Blown)
        {
            RendererInterop.PlayCue("block");
            return;
        }

        _ventSpaces[name] = _ventSpaces[name] with { Vented = true, Infested = false, HoldsSurvivor = false };

        // Whatever was in there went out with the air. On an infested hull that is the pack — the ones
        // standing in that compartment die with the room.
        if (outcome.InfestationCleared)
        {
            ClearReeversIn(name);
        }

        if (outcome.SurvivorKilled)
        {
            // No credits change hands. It costs the captain, through the #480 nerve seam, and it is meant
            // to be heavy — you will never be certain what you heard on the other side of that door.
            ApplyNerveShock(HullVenting.VentedSurvivorNerveCost, "you blew the compartment with someone in it");
            LogAutopilotEvent($"☠ Vented {name} — something alive went out with the air.");
        }
        else
        {
            LogAutopilotEvent($"💨 Vented {name}.");
        }

        RendererInterop.PlayCue("alarm");
        RequestVaultSave();
    }

    /// <summary>Kill the pack standing in a compartment that just went to vacuum.</summary>
    private void ClearReeversIn(string name)
    {
        (string _, float x0, float x1, bool top) = System.Array.Find(
            WreckLayout.Compartments, c => c.Name == name);

        for (int i = _reevers.Count - 1; i >= 0; i--)
        {
            Reever r = _reevers[i];
            bool inX = r.X >= x0 && r.X <= x1;
            bool inY = top ? r.Y < -WreckLayout.SpineHalfHeight : r.Y > WreckLayout.SpineHalfHeight;
            if (inX && inY)
            {
                _reevers.RemoveAt(i);
            }
        }
    }

    /// <summary>Rescue whoever is behind a door — the reward for the careful road. Only reachable by
    /// OPENING the compartment rather than blowing it, which is the whole point.</summary>
    private void RescueSurvivor(string name)
    {
        if (!_ventSpaces.TryGetValue(name, out HullVenting.Space s) || !s.HoldsSurvivor || s.Vented)
        {
            return;
        }

        _ventSpaces[name] = s with { HoldsSurvivor = false };
        _survivorsRescued++;
        _credits += HullVenting.SurvivorRescueCr;

        ShowPulseMessage(
            $"🧑‍🚀 Somebody is alive behind the {name} barricade — and has been for a very long time. " +
            $"They come out on their own legs. ({HullVenting.SurvivorRescueCr:N0} cr, and a witness.)");
        LogAutopilotEvent($"🧑‍🚀 Rescued a survivor from {name} — {HullVenting.SurvivorRescueCr:N0} cr.");
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
    }

    /// <summary>The compartments the board lists, aft to bow so the mimic reads like the ship does.</summary>
    private static IEnumerable<(string Name, bool Top)> VentBoardRow(bool top) =>
        WreckLayout.Compartments
            .Where(c => c.Top == top)
            .OrderBy(c => c.X0)
            .Select(c => (c.Name, c.Top));
}
