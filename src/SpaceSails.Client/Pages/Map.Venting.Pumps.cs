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
