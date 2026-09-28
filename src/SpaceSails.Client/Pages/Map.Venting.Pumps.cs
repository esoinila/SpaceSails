using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Venting — the pump crawl: one pump per atmosphere, the rough mark that banks the air, the corridor, and the whole-ship orders that dog, pump, flood, seal and unseal her in one press.
public sealed partial class Map
{
    /// <summary>
    /// Every compartment currently being pumped down, with its own clock and its own rough mark.
    ///
    /// <para>This was ONE pump for the whole ship, which was an arbitrary limit I introduced and could not
    /// defend when asked (owner: "why can I not pump down more than one room… I want to pump down
    /// several?"). A damage-control system has valves per compartment; the only thing that should bound the
    /// captain is the reserve and the clock. Running four at once is a real strategy now — and a slow one,
    /// which is the whole cost of the thrifty road.</para>
    /// </summary>
    private readonly Dictionary<string, PumpRun> _pumps = [];

    /// <summary>
    /// One pump, running on one ATMOSPHERE — however many compartments that turns out to be.
    ///
    /// <para>It used to be one pump per room, with a separate special case for the corridor and a refusal if
    /// any hatch stood open. Owner, correcting all three at once: <i>"if we only want to evacuate it then the
    /// doors to it need to be sealed … but if we evacuate multiple spaces then doors between those do not
    /// need to be sealed. The only check we need is to make sure we don't evacuate a room by accident of
    /// leaving its door open."</i> That is one rule where I had written three, and it is the true one: a
    /// pump empties the volume it is plumbed into, and the volume is whatever is standing open to it.</para>
    /// </summary>
    /// <param name="Volume">Every space this run will empty, from <see cref="HullVenting.SharedAtmosphere"/>.</param>
    /// <param name="Total">Its whole run in seconds — the sum of what is in it.</param>
    /// <param name="Charges">What it banks at the rough mark, likewise summed.</param>
    public sealed record PumpRun(
        IReadOnlyList<string> Volume, double Total, int Charges, double SecondsLeft, bool RoughBanked);

    /// <summary>The key a volume is filed under. Its members, in order — so the same atmosphere can never be
    /// put on two pumps at once, however the captain reached it.</summary>
    private static string VolumeKey(IReadOnlyList<string> volume) => string.Join("+", volume);

    /// <summary>The run this space is part of, if any. A compartment standing open to a corridor being
    /// pumped is BEING PUMPED, and every readout in the game asks this rather than looking up a name.</summary>
    private PumpRun? PumpOn(string space)
    {
        foreach (PumpRun run in _pumps.Values)
        {
            if (run.Volume.Contains(space, System.StringComparer.Ordinal))
            {
                return run;
            }
        }
        return null;
    }

    /// <summary>Every compartment as the rules see it right now, for Core's connectivity search.</summary>
    private IReadOnlyList<HullVenting.Space> SpacesNow()
    {
        var all = new List<HullVenting.Space>(_ventSpaces.Count);
        foreach (string name in _ventSpaces.Keys)
        {
            all.Add(SpaceNow(name));
        }
        return all;
    }

    /// <summary>What pressing this space would actually empty.</summary>
    private IReadOnlyList<string> AtmosphereAt(string space) =>
        HullVenting.SharedAtmosphere(space, SpacesNow());

    /// <summary>Whether the corridor can go on the pumps. It no longer needs the ship dogged shut — it needs
    /// only to still have air and not already be running. If hatches stand open, they are part of the volume
    /// and they go down with it, which the board says out loud before it starts.</summary>
    private bool SpinePumpable =>
        _spinePressurised && PumpOn(HullVenting.SpineName) is null;

    /// <summary>Put the corridor on the pumps. Now just a pump like any other, on the volume the corridor
    /// happens to be part of.</summary>
    private void StartSpinePump()
    {
        if (!_spinePressurised)
        {
            _ventMessage = HullVenting.SpineAlreadyEmptyLine;
            RendererInterop.PlayCue("block");
            return;
        }

        StartPumpDown(HullVenting.SpineName);
    }

    /// <summary>Every compartment that could go on a pump right now: still holding air and not already on
    /// one. A DOGGED HATCH IS NO LONGER REQUIRED — an open one just means the pump has more to empty, and
    /// the board says which rooms before it starts.</summary>
    private IReadOnlyList<string> PumpableRooms()
    {
        var ready = new List<string>();
        foreach ((string name, HullVenting.Space s) in _ventSpaces)
        {
            if (!s.Vented && PumpOn(name) is null)
            {
                ready.Add(s.Name);
            }
        }
        ready.Sort(System.StringComparer.Ordinal);
        return ready;
    }

    /// <summary>Start every one of them. The owner's play, in one press: dog the hatches, get to the board,
    /// and put the whole ship on the pumps.</summary>
    private void PumpEverySealedRoom()
    {
        foreach (string name in PumpableRooms())
        {
            StartPumpDown(name);
        }
    }

    /// <summary>
    /// THE WHOLE SHIP, AS ONE ORDER. Owner, having found "pump all sealed": <i>"there could be a pump the
    /// whole ship button though :-D"</i> — and there is a real difference between that and pressing the
    /// other two in turn. The corridor cannot go on the pumps until every hatch is shut, so a captain doing
    /// this by hand has to dog eight doors, start eight pumps, and then remember to come back for the spine.
    ///
    /// <para>This is that whole sequence as a standing order: dog what can be dogged, start what can be
    /// started, and take the corridor the moment it becomes possible. The board keeps the order in its head
    /// so the captain does not have to — which is exactly what a damage-control board is for.</para>
    ///
    /// <para>It does NOT spare the room the captain is standing in, and it says so. Sparing it silently
    /// would leave one compartment full of air and the corridor permanently un-pumpable, and the captain
    /// would never learn why. The pump is slow and the hatch is not held until the pressure actually
    /// differs — so this is a countdown to be somewhere else, not a trap.</para>
    /// </summary>
    private void OrderWholeShipPumped()
    {
        if (_wreck is null)
        {
            return;
        }

        // Dog every hatch the pressure is not already holding. This is the half of the owner's own play
        // ("lock all doors and pump them down") that the board could always do and never offered.
        foreach (string name in _ventSpaces.Keys.ToList())
        {
            HullVenting.Space s = SpaceNow(name);
            if (!s.DoorShut && !HullVenting.DoorHeldByPressure(s, _spinePressurised))
            {
                _ventSpaces[name] = _ventSpaces[name] with { DoorShut = true };
            }
        }

        RebuildWreckDeck();   // a dogged hatch is a wall, and the walls are built, not inferred

        int started = PumpableRooms().Count;
        PumpEverySealedRoom();

        // The corridor goes on the pumps as soon as it can — now, if every hatch answered; otherwise the
        // order stands and ServeStandingPumpOrder takes it the moment the last one does.
        _shipPumpOrder = true;
        ServeStandingPumpOrder();

        _ventMessage = HullVenting.WholeShipOrderLine(started, CaptainCompartment());
        RendererInterop.PlayCue("board");
    }

    /// <summary>The standing order, served once a frame: the corridor is taken the moment it becomes
    /// takeable, and the order clears itself the moment there is nothing left to take.</summary>
    private void ServeStandingPumpOrder()
    {
        if (!_shipPumpOrder || _wreck is null)
        {
            return;
        }

        if (!_spinePressurised)
        {
            _shipPumpOrder = false;   // done, or somebody cracked a valve and did it the wasteful way
            return;
        }

        if (SpinePumpable)
        {
            StartSpinePump();
            _shipPumpOrder = false;
        }
    }

    /// <summary>Whether the board is holding an order to take the corridor as soon as it can.</summary>
    private bool _shipPumpOrder;

    /// <summary>How long the corridor has been open to space. Same clock the compartments keep, for the same
    /// reason: it says how long it HAS been, never how long it needs.</summary>
    private double _spineVacuumSeconds;
}
