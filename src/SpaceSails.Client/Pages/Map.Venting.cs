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
        _ventLog.Clear();       // a new ship keeps her own log, not the last one's
        _placardRead = false;   // and a new ship is a ship you have never read the plate on
        _refillCharges = HullVenting.RefillChargesPerBoarding;
        _liveNestSeen = false;  // #528: a new ship is a nest you have never stood in front of
        _ventPayoff.Clear();    // and no room aboard her is holding a card the LAST hull earned

        // The hull one of her own opened has no air anywhere — including the corridor. So none of her doors
        // fight you, except the one into the room somebody kept breathing in.
        PrepareFire(wreck);   // #524: the reactor cascade is still burning somewhere aft

        _spinePressurised = wreck.Cause != Derelict.WreckCause.VentedByOneOfTheirOwn;
        // Forty years, same as the compartments she arrived empty with — the clock reads VACUUM flat,
        // because no number past the longest soak is interesting.
        _spineVacuumSeconds = _spinePressurised ? 0.0 : YearsOfVacuumSeconds;

        foreach ((string name, float x0, float x1, bool _) in WreckLayout.Compartments)
        {
            // The thing spread from the deep hold aft. Forward of amidships she is still clean — which is
            // why the away team can stand in the airlock at all.
            //
            // NEVER THE MACHINERY SPACE, and that is not a kindness — it is the only way the room works.
            // The board refuses to blow the compartment the captain is standing in, and the board is IN
            // ENGINEERING, so a nest there could never be vented by anyone, ever: it would brood forever,
            // every time the captain stepped out, in the one room they have to keep coming back to. The
            // fiction is better for it, too — the crew held their machinery space to the end, which is
            // precisely why her valves still answer.
            bool infested = wreck.Cause == Derelict.WreckCause.Infested
                            && (x0 + x1) / 2 < 0
                            && name != HullVenting.ValveCompartment;

            // The hull one of her own opened to space arrives with the job already done — every compartment
            // but the one they were standing in. The board is the confession.
            bool preVented = HullVenting.StartsVented(wreck.Cause, name);

            // ONE ROOM THE CREW SEALED BEFORE THE END. Owner: "maybe a room has been sealed off from the
            // panel :-D" — which is the whole mechanic standing in a single compartment. The hatch is
            // dogged, so it is a wall; the operating log reads that something in there is warm and moving;
            // and the instrument will not say what. Leave it shut and never know. Vent it and never know.
            // Open it and find out.
            //
            // Never ENGINEERING — the valve board is in there, and a captain locked out of the board has
            // been handed a puzzle with no pieces. Everything else is fair, because with air on both sides
            // there is no differential and the hatch can be undogged by hand at the door.
            bool crewSealedIt = wreck.Cause == Derelict.WreckCause.Infested
                                && infested
                                && name != HullVenting.ValveCompartment
                                && HullVenting.HidesSurvivor(wreck.Id, name, wreck.Cause) == false
                                && DiceRule.Roll(
                                       DiceRule.Seed("sealed-room", (long)wreck.Id.GetHashCode(System.StringComparison.Ordinal),
                                                     name.GetHashCode(System.StringComparison.Ordinal)),
                                       3).Face == 1;

            _ventSpaces[name] = new HullVenting.Space(
                Name: name,
                DoorShut: preVented || crewSealedIt,
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

    /// <summary>
    /// THE SHIP'S OWN OPERATING LOG, STILL BEING WRITTEN. Owner: <i>"the operation log should get our new
    /// operational entries logged into it."</i>
    ///
    /// <para>Obvious once said, and it closes a loop the whole wreck lane is built on. The captain reads
    /// this ship's log to work out what her crew did in their last hours — and then does forty minutes of
    /// far stranger things to her, none of which she records. She is a working ship again for as long as
    /// somebody is aboard pulling her handles, and a damage-control board that did not keep a log would be
    /// the only instrument on her that does not.</para>
    ///
    /// <para>It is also the answer to a real usability problem: the panel could only ever show ONE line at a
    /// time, so anything that happened while the captain was looking elsewhere — a pump banking, a room
    /// finishing its soak, a hatch that would not move — was gone before it could be read.</para>
    /// </summary>
    private readonly List<string> _ventLog = [];

    /// <summary>The most the board keeps. Long enough to cover a whole boarding, short enough to stay a log
    /// rather than a transcript.</summary>
    private const int VentLogDepth = 60;

    /// <summary>Every action the board takes goes here AND to the ship's event log — one call, so a new
    /// switch cannot be added that quietly writes to only one of them.</summary>
    private void BoardLog(string line)
    {
        LogAutopilotEvent(line);

        _ventLog.Add(line);
        if (_ventLog.Count > VentLogDepth)
        {
            _ventLog.RemoveAt(0);
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

    /// <summary>The placard at the lock — the first thing aboard, and the one that answers "where do I go".
    /// Reads in full once and briefly after that: a briefing the first time, a reminder every time.</summary>
    private void ReadDamageControlPlacard()
    {
        ShowPulseMessage(_placardRead ? HullVenting.PlacardAgainLine : HullVenting.PlacardLine);

        if (!_placardRead)
        {
            _placardRead = true;
            BoardLog($"🪧 Read her placard — atmosphere control is aft in {HullVenting.ValveCompartment}.");
        }
    }

    /// <summary>Whether the captain has read the plate by the lock on this boarding.</summary>
    private bool _placardRead;

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
        if (s.DoorShut)
        {
            ReleaseWhatWasSealedIn(name);   // thrown from the board: a door you are not standing at
        }
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

        // OWNER: "maybe the log should open in top most popup and the rest of the ui be disabled until it
        // is closed." Right — this is the most important sentence the mechanic produces, and as a small
        // line tucked under the switches it read like a status bar. It is not a status: it is the moment
        // the captain is handed an answer that refuses to finish itself, and everything they do next is
        // decided by it. So it takes the screen, and the board waits.
        _ventReadCard = name;
        RendererInterop.PlayCue("reveal");
    }

    /// <summary>The compartment whose operating-log card is currently up, over the valve board.</summary>
    private string? _ventReadCard;

    private void CloseVentReadCard() => _ventReadCard = null;

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
            BoardLog($"☠ Vented {name} — something alive went out with the air.");
        }
        else
        {
            BoardLog($"💨 Vented {name}.");
        }

        // A compartment blowing to space is the loudest thing that has happened aboard her in forty years.
        MakeNoiseAboard(RoomCentre(name).X, RoomCentre(name).Y, LoudEarshot);

        RendererInterop.PlayCue("alarm");
        RebuildWreckDeck();   // vacuum that side, air this side: the doorway is now ten tonnes in a frame
        RequestVaultSave();
    }
}
