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

    /// <summary>Whether the spine — the corridor the captain walks — still holds the ship's stale air. This
    /// is the OTHER side of every pressure-locked door. A hull that has been open to space for decades has
    /// none, and every door on her swings freely.</summary>
    private bool _spinePressurised = true;

    /// <summary>Breaths of the shuttle's reserve left to spend bringing compartments back to pressure.
    /// Refilled at the shuttle, never aboard — the wreck has nothing to give.</summary>
    private int _refillCharges = HullVenting.RefillChargesPerBoarding;

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
        _refillCharges = HullVenting.RefillChargesPerBoarding;

        // The hull one of her own opened has no air anywhere — including the corridor. So none of her doors
        // fight you, except the one into the room somebody kept breathing in.
        _spinePressurised = wreck.Cause != Derelict.WreckCause.VentedByOneOfTheirOwn;

        foreach ((string name, float x0, float x1, bool _) in WreckLayout.Compartments)
        {
            // The thing spread from the deep hold aft. Forward of amidships she is still clean — which is
            // why the away team can stand in the airlock at all.
            bool infested = wreck.Cause == Derelict.WreckCause.Infested && (x0 + x1) / 2 < 0;

            // The hull one of her own opened to space arrives with the job already done — every compartment
            // but the one they were standing in. The board is the confession.
            bool preVented = HullVenting.StartsVented(wreck.Cause, name);

            _ventSpaces[name] = new HullVenting.Space(
                Name: name,
                DoorShut: preVented,
                Vented: preVented,
                Infested: infested,
                HoldsSurvivor: HullVenting.HidesSurvivor(wreck.Id, name, wreck.Cause),
                CaptainInside: false,
                // Forty years is a long soak. Nothing that was aboard her is still alive.
                VacuumSeconds: preVented ? YearsOfVacuumSeconds : 0.0,
                Kind: HullVenting.InfestationIn(wreck.Id, name, infested));
        }
    }

    /// <summary>The vacuum age of a compartment that was blown long before the away team arrived. Any number
    /// past the longest soak does; this one is honest about the scale.</summary>
    private const double YearsOfVacuumSeconds = 60 * 60 * 24 * 365 * 40.0;

    /// <summary>Run the vacuum clocks. Owner: <i>"there might be a counter on how long the room has been in
    /// vacuum … so it needs certain time for certain infestations."</i> A vented compartment counts up for
    /// as long as the away team is aboard, which is what turns venting from a button into a decision about
    /// WHEN — blow the hold, go and read the log, come back to a room that has been open four minutes.</summary>
    private void AdvanceVacuumClocks(double dtSeconds)
    {
        if (_wreck is null || _ventSpaces.Count == 0)
        {
            return;
        }

        foreach (string name in _ventSpaces.Keys.ToList())
        {
            HullVenting.Space s = _ventSpaces[name];
            if (!s.Vented)
            {
                continue;
            }

            bool wasDone = HullVenting.SoakComplete(s);
            s = s with { VacuumSeconds = s.VacuumSeconds + dtSeconds };
            _ventSpaces[name] = s;

            // The edge where the vacuum finishes the job — the moment the handle used to deliver.
            if (!wasDone && HullVenting.SoakComplete(s) && s.Infested)
            {
                _ventSpaces[name] = s with { Infested = false };
                ClearReeversIn(name);
                LogAutopilotEvent($"💨 {name} has been open to space long enough. Whatever was in there is finished.");
            }
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

    /// <summary>Pick a compartment off the map. Clears the last outcome so the board never shows a result
    /// from one room next to the switches for another.</summary>
    private void SelectVentSpace(string name)
    {
        _ventSelected = name;
        _ventMessage = null;
    }

    /// <summary>The dead bridge panel: a signpost, not a wall. Nobody should have to guess the answer is aft.</summary>
    private void TryDeadBridgePanel() => ShowPulseMessage(HullVenting.DeadBridgePanelLine);

    /// <summary>Throw a compartment's door switch. This is the interlock the vent handle checks.</summary>
    private void ToggleVentDoor(string name)
    {
        if (!_ventSpaces.TryGetValue(name, out HullVenting.Space s))
        {
            return;
        }

        // The ONLY thing that stops a door moving is a pressure differential — ten tonnes in a frame. It
        // used to refuse on any vented compartment, which was correct when venting was terminal and became
        // a dead end the moment refilling existed: a vented room's hatch could never be dogged, and refill
        // needs it dogged, so a room you blew could never be brought back. (Found by the owner, mid-play:
        // "I cannot seem to refill the engineering now.")
        if (HullVenting.DoorHeldByPressure(s, _spinePressurised))
        {
            _ventMessage = HullVenting.PressureLockLine(name, s.Vented);
            RendererInterop.PlayCue("block");
            return;
        }

        _ventMessage = null;   // a fresh action clears the last outcome, so the panel never mixes them
        _ventSpaces[name] = s with { DoorShut = !s.DoorShut };
        RebuildWreckDeck();    // a dogged hatch is a wall; an undogged one is a doorway
        RendererInterop.PlayCue("board");
    }

    /// <summary>Take the life-sign reading — ONCE. The die is rolled against the wreck's own seed and the
    /// compartment, and the result is kept: standing at the board re-reading until it says something
    /// comfortable is exactly the tension this mechanic exists to create.</summary>
    private void ReadLifeSigns(string name)
    {
        if (_wreck is not { } w)
        {
            return;
        }

        HullVenting.Space space = SpaceNow(name);

        // THE INSTRUMENT IS A RECORD, SO IT NEVER REFUSES TO BE READ. Owner: "that button should probably
        // never be disabled there." It used to be disabled once read and again once vented, which meant the
        // one question the soak creates — IS IT FINISHED YET — could not be asked at all.
        //
        // The anti-re-roll law survives without the grey-out, and more honestly: the reading is SEEDED on
        // the compartment's state, so asking twice about the same room in the same condition returns the
        // same answer, every time, forever. You cannot shake a different result out of a record. What DOES
        // earn a new reading is the room genuinely changing — opening it to space, letting the vacuum
        // finish, putting the air back — because that is a different measurement, not a second attempt at
        // the same one.
        long stateKey = (space.Vented ? 1 : 0) | (HullVenting.SoakComplete(space) ? 2 : 0);
        ulong seed = DiceRule.Seed(
            w.Id, (long)name.GetHashCode(System.StringComparison.Ordinal), stateKey);

        _ventMessage = null;
        _ventReads[name] = HullVenting.Read(seed, space);
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

        // The room opens; the clock starts. Infested stays TRUE until the vacuum has had long enough —
        // AdvanceVacuumClocks owns that edge now. The survivor, having lungs, does not get a clock.
        _ventSpaces[name] = _ventSpaces[name] with
        {
            Vented = true,
            HoldsSurvivor = false,
            VacuumSeconds = 0.0,
        };

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
        RebuildWreckDeck();   // vacuum that side, air this side: the doorway is now ten tonnes in a frame
        RequestVaultSave();
    }

    /// <summary>The compartment currently being pumped down, and how long the pump still has to run. The
    /// thrifty road: slower than the handle, and it banks the air instead of losing it.</summary>
    private string? _pumpingRoom;
    private double _pumpSecondsLeft;

    /// <summary>Whether this run has already passed the rough mark and banked its charge — so the readout
    /// stops counting at the captain and starts offering them the choice.</summary>
    private bool _pumpRoughBanked;

    /// <summary>Stop the pump. Past the rough mark this is the THRIFTY finish, not an abort: the air is
    /// already in the tanks and all you are giving up is a pressure low enough to kill.</summary>
    private void StopPump()
    {
        if (_pumpingRoom is not { } name)
        {
            return;
        }

        _pumpingRoom = null;
        _pumpSecondsLeft = 0;
        _ventMessage = _pumpRoughBanked
            ? $"You shut the pump down. {name} keeps what little is left in it, and the rest is aboard."
            : $"You shut the pump down early. {name} still has most of her air, and none of it is yours.";
        _pumpRoughBanked = false;
        RendererInterop.PlayCue("block");
    }

    /// <summary>Start the pump. Same interlock as the handle — a shut hatch, or you are pumping the ship.</summary>
    private void StartPumpDown(string name)
    {
        if (_wreck is null || _pumpingRoom is not null)
        {
            return;
        }

        HullVenting.Space space = SpaceNow(name);
        HullVenting.VentReadiness readiness = HullVenting.PumpReadiness(space);
        if (readiness != HullVenting.VentReadiness.Ready)
        {
            _ventMessage = HullVenting.RefusalLine(readiness, name);
            RendererInterop.PlayCue("block");
            return;
        }

        _pumpingRoom = name;
        _pumpSecondsLeft = HullVenting.PumpDownSeconds;
        _ventMessage = HullVenting.PumpRunningLine(name, _pumpSecondsLeft);
        RendererInterop.PlayCue("board");
    }

    /// <summary>Run the pump. It keeps running while the captain walks away — this is a machine, not a
    /// minigame — but the room does not reach vacuum until it finishes, so the soak clock starts LATE.
    /// That is the price of the thrifty road, on top of the wait.</summary>
    private void AdvancePump(double dtSeconds)
    {
        if (_pumpingRoom is not { } name || _wreck is null)
        {
            return;
        }

        double before = _pumpSecondsLeft;
        _pumpSecondsLeft -= dtSeconds;

        // The rough mark: the mechanical stage is done and the air is home. Everything after this is the
        // long pull to a killing pressure, and it returns nothing to the tanks.
        double roughAt = HullVenting.PumpDownSeconds - HullVenting.PumpRoughSeconds;
        if (before > roughAt && _pumpSecondsLeft <= roughAt)
        {
            _refillCharges += HullVenting.PumpDownYieldsCharges;
            _pumpRoughBanked = true;
            _ventMessage = HullVenting.PumpRoughDoneLine(name);
            LogAutopilotEvent($"🛢 {name} roughed out — the air is in the tanks ({_refillCharges} charges).");
            RendererInterop.PlayCue("reveal");
            return;
        }

        if (_pumpSecondsLeft > 0)
        {
            if (_showVentPanel && !_pumpRoughBanked)
            {
                _ventMessage = HullVenting.PumpRunningLine(name, _pumpSecondsLeft);
            }
            return;
        }

        _pumpingRoom = null;
        _pumpSecondsLeft = 0;
        _pumpRoughBanked = false;

        // Only NOW is the room lethal. The charge was banked at the rough mark, a long time ago.
        if (_ventSpaces.TryGetValue(name, out HullVenting.Space s))
        {
            _ventSpaces[name] = s with { Vented = true, VacuumSeconds = 0.0, HoldsSurvivor = false };
        }

        _ventMessage = HullVenting.PumpDoneLine(name);
        LogAutopilotEvent($"🛢 Pumped {name} down — the air is in the tanks ({_refillCharges} charges).");
        RendererInterop.PlayCue("alarm");
        RebuildWreckDeck();
        RequestVaultSave();
    }

    /// <summary>Whether any room is still counting toward being certainly dead — the clock worth watching
    /// from the corridor. A hull that arrived vented decades ago is not "soaking"; it is finished.</summary>
    private bool AnyRoomSoaking
    {
        get
        {
            foreach (HullVenting.Space s in _ventSpaces.Values)
            {
                if (s.Vented && s.Infested && s.VacuumSeconds < YearsOfVacuumSeconds)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>The rooms whose vacuum clocks are worth showing while the captain is out in the corridor
    /// deciding whether the sentry can hold the lane long enough.</summary>
    private IEnumerable<(string Room, double Seconds)> RoomsSoaking =>
        _ventSpaces.Values
            .Where(s => s.Vented && s.Infested && s.VacuumSeconds < YearsOfVacuumSeconds)
            .OrderByDescending(s => s.VacuumSeconds)
            .Select(s => (s.Name, s.VacuumSeconds));

    /// <summary>Every compartment whose doorway is currently ten tonnes of atmosphere in a frame. The deck
    /// walls this set, so a room you blew is a room you cannot walk into.</summary>
    private HashSet<string> HeldDoors()
    {
        var held = new HashSet<string>(System.StringComparer.Ordinal);
        foreach ((string name, HullVenting.Space s) in _ventSpaces)
        {
            if (HullVenting.DoorHeldByPressure(s, _spinePressurised))
            {
                held.Add(name);
            }
        }
        return held;
    }

    /// <summary>Every doorway the captain cannot walk through: loaded by pressure, or dogged shut by hand.</summary>
    private HashSet<string> BlockedDoors()
    {
        var blocked = new HashSet<string>(System.StringComparer.Ordinal);
        foreach ((string name, HullVenting.Space s) in _ventSpaces)
        {
            if (HullVenting.DoorwayBlocked(s, _spinePressurised))
            {
                blocked.Add(name);
            }
        }
        return blocked;
    }

    /// <summary>The compartment whose pressure door the captain is standing at, once the card is up.</summary>
    private string? _pressureDoor;

    /// <summary>Walk up to a door that will not move: the gauge, why, and the two roads through it. Which
    /// door is decided by where the captain is STANDING rather than by a label — the deck dispatches on
    /// console kind alone, and a name parsed back out of display text is a bug waiting for a rename.</summary>
    private void OpenPressureDoorCard()
    {
        string? nearest = null;
        double best = double.MaxValue;

        foreach ((string name, float x0, float x1, bool top) in WreckLayout.Compartments)
        {
            if (!_ventSpaces.TryGetValue(name, out HullVenting.Space s)
                || !HullVenting.DoorHeldByPressure(s, _spinePressurised))
            {
                continue;
            }

            double dx = VentDoorX(x0, x1) - _avatarX;
            double dy = (top ? -WreckLayout.SpineHalfHeight : WreckLayout.SpineHalfHeight) - _avatarY;
            double d2 = (dx * dx) + (dy * dy);
            if (d2 < best)
            {
                best = d2;
                nearest = name;
            }
        }

        if (nearest is null)
        {
            return;
        }

        _pressureDoor = nearest;
        RendererInterop.PlayCue("block");
    }

    private void ClosePressureDoorCard() => _pressureDoor = null;

    /// <summary>Crack the equalisation valve at the door itself — the thing every real pressure door has,
    /// and the reason this can never strand a captain. Free, and it costs the ship her air: whichever side
    /// still had an atmosphere does not afterwards.</summary>
    private void EqualiseAtDoor(string name)
    {
        if (_wreck is null || !_ventSpaces.TryGetValue(name, out HullVenting.Space s))
        {
            return;
        }

        // One volume, one pressure, one valve: the spine and every room standing OPEN to it empty together.
        // What survives is exactly what somebody dogged a hatch on.
        HullVenting.EqualiseResult r = HullVenting.EqualiseAt(
            name, [.. _ventSpaces.Values], _spinePressurised);

        foreach (HullVenting.Space updated in r.Spaces)
        {
            _ventSpaces[updated.Name] = updated;
        }
        _spinePressurised = !r.SpineVented && _spinePressurised;

        string extra = r.RoomsOpened > 0
            ? $" {r.RoomsOpened} compartment{(r.RoomsOpened == 1 ? "" : "s")} went with it — every one that " +
              "had its door standing open."
            : "";

        // The warning on the valve was not decoration. Anyone behind an open door stops.
        if (r.SurvivorsLost > 0)
        {
            extra += " And in one of those rooms, something that had been alive for a very long time was not " +
                     "behind a dogged hatch.";
            ApplyNerveShock(HullVenting.VentedSurvivorNerveCost, "you emptied the ship with someone still in it");
            LogAutopilotEvent($"☠ Equalising took {r.SurvivorsLost} survivor(s) — their doors were open.");
        }

        ShowPulseMessage(HullVenting.EqualiseLine + extra);
        LogAutopilotEvent($"🎚 Cracked the {name} valve — {(r.SpineVented ? "the ship is vacuum now" : "pressures even")}.");
        RendererInterop.PlayCue("alarm");
        RebuildWreckDeck();
        RequestVaultSave();
    }

    /// <summary>Dog a hatch down by hand, at the hatch. The counterplay to the whole-ship valve: a sealed
    /// room keeps its air no matter what anyone does to the pressure two compartments away — and the
    /// infestation has not read the ship's manual, so it will never close one behind itself.</summary>
    private void ToggleSealAtHand(string name)
    {
        if (!_ventSpaces.TryGetValue(name, out HullVenting.Space s)
            || HullVenting.DoorHeldByPressure(s, _spinePressurised))
        {
            return;   // ten tonnes says no; the card offers the valve instead
        }

        bool sealing = !s.DoorShut;
        _ventSpaces[name] = s with { DoorShut = sealing };

        ShowPulseMessage(sealing ? HullVenting.SealLine(name) : HullVenting.UnsealLine(name));
        RendererInterop.PlayCue("board");
        RebuildWreckDeck();
    }

    /// <summary>Bring a compartment back to pressure — the other half of the board, and the half that
    /// tempts you into being impatient. Air comes back; nothing that went out with it does.</summary>
    private void RefillCompartment(string name)
    {
        if (_wreck is null)
        {
            return;
        }

        HullVenting.Space space = SpaceNow(name);
        HullVenting.RefillOutcome outcome = HullVenting.Refill(space, _refillCharges);
        _ventMessage = outcome.Line;

        if (!outcome.Filled)
        {
            RendererInterop.PlayCue("block");
            return;
        }

        _refillCharges--;
        _ventSpaces[name] = _ventSpaces[name] with { Vented = false, VacuumSeconds = 0.0 };

        // A reading taken on a vacuum compartment says nothing about a pressurised one — and if something
        // in there just took its first breath, the captain is entitled to go and ask the instrument again.
        _ventReads.Remove(name);

        if (outcome.SomethingSurvived)
        {
            ApplyNerveShock(HullVenting.RefilledTooSoonNerveCost, "you gave it the air back before it was finished");
            LogAutopilotEvent($"🫁 Refilled {name} too early — it was not finished.");
        }
        else
        {
            LogAutopilotEvent($"🫁 Brought {name} back to pressure ({_refillCharges} left).");
        }

        RendererInterop.PlayCue("board");
        RebuildWreckDeck();   // the door this room shares with the spine is now loaded — or no longer is
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

    // ── The mimic's geometry, taken from the ship's own numbers ───────────────────────────────────────

    /// <summary>A deck unit as an SVG coordinate: ALWAYS invariant. Blazor renders a float attribute in the
    /// current culture, so on a Finnish browser 20.5 becomes "20,5" — which SVG reads as two numbers and the
    /// map comes apart. It would have broken for the owner and for nobody else.</summary>
    private static string Du(double v) =>
        v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The mimic's frame — the audit's own playable bounds, so the map shows exactly the ship the
    /// A* walks.</summary>
    private static string VentViewBox
    {
        get
        {
            (double minX, double minY, double maxX, double maxY) = WreckLayout.Bounds;
            return $"{Du(minX)} {Du(minY)} {Du(maxX - minX)} {Du(maxY - minY)}";
        }
    }

    /// <summary>The hull outline, built from <see cref="WreckLayout"/>'s constants rather than drawn by
    /// hand: flat transom aft, tapering bow forward. If the ship's shape ever changes, the mimic changes
    /// with it. (Symmetric about the spine, so negating Y for SVG leaves it unchanged.)</summary>
    private static string VentHullOutline
    {
        get
        {
            float taper = WreckLayout.BowX - 6;
            return string.Join(' ',
                $"{Du(WreckLayout.AftX)},{Du(WreckLayout.BottomY)}",
                $"{Du(taper)},{Du(WreckLayout.BottomY)}",
                $"{Du(WreckLayout.BowX)},2",
                $"{Du(WreckLayout.BowX)},-2",
                $"{Du(taper)},{Du(WreckLayout.TopY)}",
                $"{Du(WreckLayout.AftX)},{Du(WreckLayout.TopY)}");
        }
    }

    /// <summary>Where THIS compartment's doorway onto the spine actually is. The spine has four openings,
    /// not eight — each one serves the room above it and the room below it — so a door drawn at every
    /// compartment's midpoint would be showing the captain a way through that is not there.</summary>
    private static float VentDoorX(float x0, float x1)
    {
        foreach (float centre in WreckLayout.DoorCentres())
        {
            if (centre > x0 && centre < x1)
            {
                return centre;
            }
        }
        return (x0 + x1) / 2f;
    }

    /// <summary>The one word a compartment wears on the mimic, and the class that colours it. Empty when the
    /// room has nothing to say yet — an unread compartment should look unread.</summary>
    private (string Text, string Class) VentAreaTag(string name, HullVenting.Space space, float roomWidth)
    {
        if (space.Vented)
        {
            // The counter the owner asked for. It says how long the room has been open and DELIBERATELY not
            // how long it needs — the second number does not exist for the captain, which is the whole
            // decision. A hull that arrived vented decades ago just reads VACUUM; no clock is interesting.
            if (space.VacuumSeconds >= YearsOfVacuumSeconds)
            {
                return ("VACUUM", "vent-tag");
            }

            // FORWARD LOCKER is seven units wide and "VACUUM 00:22" is twelve characters, so the running
            // clock hung straight out over the hull (owner, mid-vent: "the vacuum text is clipped during
            // the process"). Offer the same fact at three lengths and let the room pick — the clock is the
            // part that must never be dropped, and the room's own dark fill already says vacuum.
            string clock = HullVenting.SoakLabel(space.VacuumSeconds);
            return (Longest(roomWidth, [$"VACUUM {clock}", $"VAC {clock}", clock]), "vent-tag");
        }
        if (space.CaptainInside)
        {
            return ("YOU", "vent-tag here-tag");
        }
        if (_ventReads.TryGetValue(name, out (DiceRoll Roll, HullVenting.LifeSign Sign) rd))
        {
            return rd.Sign switch
            {
                HullVenting.LifeSign.SomethingAlive => ("ALIVE?", "vent-tag alive-tag"),
                HullVenting.LifeSign.Empty => ("cold", "vent-tag"),
                _ => ("??", "vent-tag"),
            };
        }
        return ("", "vent-tag");
    }

    /// <summary>The compartment's name (and its one-word state) as SVG. Razor reserves <c>&lt;text&gt;</c>
    /// for its own control flow, so the labels are built here and injected as markup. Names are drawn INSIDE
    /// the room they belong to — owner: <i>"a map with named sections so if you don't remember the name of
    /// the room you still know to vent the right place."</i></summary>
    private static string VentAreaLabelSvg(
        string label, float cx, float cy, float roomWidth, (string Text, string Class) tag)
    {
        System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
        string x = cx.ToString("0.##", inv);

        // FIT THE NAME TO THE ROOM. "FORWARD LOCKER" is fourteen characters in a seven-unit compartment,
        // and at one size for every room it ran straight over its neighbours (owner, mid-playtest: "maybe
        // some text overlap on map on smaller rooms"). Wrap onto two lines first, because a name at a
        // readable size on two lines beats a name shrunk to fit on one; only then shrink.
        float avail = System.Math.Max(2f, roomWidth - LabelPadding);
        string[] lines = [label];
        if (Widest(lines) * BaseLabelSize > avail && label.Contains(' ', System.StringComparison.Ordinal))
        {
            lines = label.Split(' ');
        }

        float size = System.Math.Min(BaseLabelSize, avail / System.Math.Max(1f, Widest(lines)));
        float lead = size * 1.25f;
        float top = cy - ((lines.Length - 1) * lead / 2f);

        var svg = new System.Text.StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            svg.Append(inv, $"""<text x="{x}" y="{(top + (i * lead)).ToString("0.##", inv)}" """)
               .Append(inv, $"""font-size="{size.ToString("0.##", inv)}" text-anchor="middle">""")
               .Append(System.Net.WebUtility.HtmlEncode(lines[i]))
               .Append("</text>");
        }

        if (tag.Text.Length > 0)
        {
            // Backstop: even the shortest candidate has to fit a narrow room, so shrink rather than clip.
            float tagSize = System.Math.Min(TagLabelSize, avail / System.Math.Max(1f, tag.Text.Length * 0.6f));
            string ty = (cy + 2.1f).ToString("0.##", inv);
            svg.Append(inv, $"""<text class="{tag.Class}" x="{x}" y="{ty}" """)
               .Append(inv, $"""font-size="{tagSize.ToString("0.##", inv)}" text-anchor="middle">""")
               .Append(System.Net.WebUtility.HtmlEncode(tag.Text))
               .Append("</text>");
        }

        return svg.ToString();
    }

    /// <summary>The longest of these that actually fits inside the compartment at the tag's own size. The
    /// candidates must be ordered fullest-first and the LAST one must always fit — it is the fallback, so
    /// it should carry only the part that cannot be dropped.</summary>
    private static string Longest(float roomWidth, string[] candidates)
    {
        float avail = System.Math.Max(2f, roomWidth - LabelPadding);
        foreach (string c in candidates)
        {
            if (c.Length * 0.6f * TagLabelSize <= avail)
            {
                return c;
            }
        }
        return candidates[^1];
    }

    /// <summary>The label size a roomy compartment gets. Narrow ones come down from here, never up.</summary>
    private const float BaseLabelSize = 1.55f;

    /// <summary>The state tag's size — kept in step with the <c>.vent-tag</c> rule in the stylesheet, since
    /// the fitting arithmetic has to know what the browser will actually draw.</summary>
    private const float TagLabelSize = 1.35f;

    /// <summary>Clearance left inside each compartment's bulkheads, in deck units.</summary>
    private const float LabelPadding = 1.2f;

    /// <summary>Width of the longest line in EM, for a monospace face (advance ≈ 0.6 em per character).
    /// Multiply by a font size to get deck units.</summary>
    private static float Widest(string[] lines)
    {
        int longest = 0;
        foreach (string s in lines)
        {
            longest = System.Math.Max(longest, s.Length);
        }
        return longest * 0.6f;
    }

    /// <summary>The compartments the board lists, aft to bow so the mimic reads like the ship does.</summary>
    private static IEnumerable<(string Name, bool Top)> VentBoardRow(bool top) =>
        WreckLayout.Compartments
            .Where(c => c.Top == top)
            .OrderBy(c => c.X0)
            .Select(c => (c.Name, c.Top));
}
