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

        // THE CORRIDOR KEEPS TIME TOO. Owner: "the spine should also show the vacuum time etc." It was the
        // one volume on the board with no instruments — a bare true/false — even though it is the volume the
        // captain spends the most time standing in and the only one they cannot shut a door against.
        if (!_spinePressurised)
        {
            _spineVacuumSeconds += dtSeconds;
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

                // WHAT YOU GOT FOR THE WAIT — SHOWN WHEN YOU LOOK, NEVER BEFORE. Owner: "that should be
                // told with GEN AI images … what did we get from venting", and then immediately: "I got
                // the what the vacuum left even though I had not yet gone to the room."
                //
                // Quite right, and it broke the rule the rest of this hull now runs on: THE MAP IS YOUR
                // EYES. A card firing off a clock hands the captain a photograph of a compartment they have
                // never entered — the same sin as drawing contacts through bulkheads, dressed as a reward.
                // So the soak only ever leaves the room WORTH LOOKING AT; the looking is still the
                // captain's to do, at the station, on their own feet.
                _ventPayoff.Add(name);
            }
        }
    }

    /// <summary>
    /// #488 · THE NEST IS A SOURCE. Owner, after the pack walked out into the corridor: <i>"kind of a pity
    /// there was nothing left to vent in the reactor room. How is that logical, are we now venting an empty
    /// room or could there be a reever spawn point there :-D … all the reevers came out to the spine, none
    /// left to vent."</i>
    ///
    /// <para>He is right that it read as pointless, and the honest fix is the one he proposes. A nest that
    /// has kept something alive aboard a dead ship for forty years is not a decoration the pack happens to
    /// stand near — it is where they come FROM. So while it is intact it keeps producing, slowly, and
    /// venting the room is not "killing an empty compartment": it is cutting the supply.</para>
    ///
    /// <para>That is also what finally makes the vacuum soak worth the wait rather than a chore. Clear the
    /// corridor and you have bought minutes. Vent the nest and you have bought the ship.</para>
    /// </summary>
    private void AdvanceNests(double dtSeconds)
    {
        if (_wreck is not { Cause: Derelict.WreckCause.Infested } || _ventSpaces.Count == 0)
        {
            return;
        }

        // ONE NEST. Owner, watching them come out of half the ship: "I thought there was only one nest?"
        // There is — the deep hold, where her station stands and where every line of text has always said
        // it was. Every aft compartment being flagged Infested is a statement about what LIVES in them and
        // how long the vacuum needs to finish it; it was never meant to mean each one produces more.
        string nest = WreckLayout.NestCompartment;
        if (!_ventSpaces.TryGetValue(nest, out HullVenting.Space s) || !s.Infested || s.Vented)
        {
            return;   // vented, or this hull never had one: the supply is cut
        }

        // NOTHING IS EVER BORN IN THE ROOM THE CAPTAIN IS STANDING IN. Owner, ringed by six of them at the
        // valve board with a dogged hatch and a loaded sentry: "I am surrounded by reevers though I was in
        // a closed space?" Nothing came through anything — they were BORN there. A nest is a place you can
        // walk away from, or vent, or decide to live with. It is not an ambush that materialises inside
        // your personal space. It holds while you are in the room.
        if (CaptainCompartment() == nest)
        {
            return;
        }

        _nestClocks.TryGetValue(nest, out double t);
        t += dtSeconds;
        if (t < NestBroodSeconds)
        {
            _nestClocks[nest] = t;
            return;
        }
        _nestClocks[nest] = 0;

        // The pacing law this hull is tuned to (owner: "they come one or two at a time"). A nest is a slow
        // drip and never a wave — and it stops entirely at the ceiling, so leaving one alive costs you a
        // steady trickle rather than an unwinnable ship.
        if (_reevers.Count >= NestPackCeiling)
        {
            return;
        }

        DeckReachability.Point at = WreckLayout.CauseStation(Derelict.WreckCause.Infested);

        _reevers.Add(new Reever
        {
            // Out of the nest itself, not out of thin air in the middle of the room.
            X = at.X + 1.2,
            Y = at.Y,
            Facing = 0,
            JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)_reevers.Count + 31UL,
            // It comes out of the nest knowing nothing — same rule as the sleepers. It has to find you.
            EverSeen = false,
            VisibleOnMap = false,
        });

        LogAutopilotEvent($"🕷 Something else pulls itself out of the nest in the {nest}.");
    }

    /// <summary>How long an intact nest takes to put another one on its feet. Slow on purpose: the drip has
    /// to be survivable, and the point of it is to make VENTING urgent, not to overrun the captain.
    /// FLAGGED for the owner's tuning.</summary>
    private const double NestBroodSeconds = 55.0;

    /// <summary>The most a nest will ever have walking at once. Past this it holds — a hull you refuse to
    /// vent gets steadily worse, never impossible.</summary>
    private const int NestPackCeiling = 9;

    /// <summary>Per-compartment brood timers.</summary>
    private readonly Dictionary<string, double> _nestClocks = [];

    /// <summary>Rooms the vacuum has finished with, waiting for the captain to walk in and see it. The
    /// payoff is EARNED BY LOOKING — the soak leaves something worth looking at, and nothing more.</summary>
    private readonly HashSet<string> _ventPayoff = new(System.StringComparer.Ordinal);

    /// <summary>Whether this compartment is holding a "what the vacuum left" card for whenever the captain
    /// gets there.</summary>
    private bool HasVentPayoff(string name) => _ventPayoff.Contains(name);

    /// <summary>Called as the captain walks: the first time they set foot in a room the vacuum has finished
    /// with, the room shows them what it left. Not a clock, not a notification — you went and looked.</summary>
    private void CheckVentPayoffUnderfoot()
    {
        if (_ventPayoff.Count == 0 || _wreckLook is not null || CaptainCompartment() is not { } here)
        {
            return;
        }
        if (!_ventPayoff.Remove(here))
        {
            return;
        }

        _wreckLook = new WreckLook(
            $"💨 {here} — WHAT THE VACUUM LEFT",
            "art/vented-nest-dead.jpg",
            "The nest is collapsed and hollow, frost-shattered off the racks, and nothing in it is intact. " +
            "Deep parallel gouges cross the deck toward a sealed hatch and stop there. Whatever spent forty " +
            "years working at that door is not working at it now.");
        RendererInterop.PlayCue("reveal");
    }

    /// <summary>
    /// #488 · VACUUM IS GROUND, NOT AN EVENT. A room that is open to space keeps being open to space, so
    /// anything standing in it keeps dying — including whatever wanders in ten minutes later.
    ///
    /// <para>The old rule killed only at the instant a compartment's soak completed, which left a hull full
    /// of hard vacuum that was mechanically scenery (owner: "I pumped the near hold to vacuum but there are
    /// still reevers in it?"). Exposure is per-contact now: a walker takes <see cref="HullVenting.Infestation.Motile"/>
    /// seconds of it — lungs are the fastest thing to lose — and the clock resets the moment it steps back
    /// into air.</para>
    ///
    /// <para>Which turns a vented compartment into a WEAPON you can leave lying around: blow a room, back
    /// through it, and the corridor behind you is lethal ground for anything that follows.</para>
    /// </summary>
    private void AdvanceVacuumExposure(double dtSeconds)
    {
        if (_wreck is null || _reevers.Count == 0)
        {
            return;
        }

        double lethal = HullVenting.SoakRequired(HullVenting.Infestation.Motile);

        for (int i = _reevers.Count - 1; i >= 0; i--)
        {
            Reever r = _reevers[i];

            // Which volume is it standing in? A compartment's own state, or the corridor's.
            bool inVacuum = RoomAt(r.X, r.Y) is { } room
                ? _ventSpaces.TryGetValue(room, out HullVenting.Space s) && s.Vented
                : !_spinePressurised;

            if (!inVacuum)
            {
                r.VacuumSeconds = 0;   // back in air: whatever it took, it is over
                continue;
            }

            r.VacuumSeconds += dtSeconds;

            // And it FAILS while it does it (owner: "yeah they really should slow down there :-D"). The
            // drag is read off the same exposure, so the vacuum is legible long before it is fatal: a thing
            // that charged into a blown compartment is visibly labouring halfway across it, and the captain
            // can watch the room doing the work. See VacuumDrag.
            if (r.VacuumSeconds >= lethal)
            {
                _reevers.RemoveAt(i);
                LogAutopilotEvent("💨 One of them stopped moving in the vacuum.");
            }
        }
    }

    /// <summary>How much of its speed a contact still has, given how long it has been in vacuum. Full pace
    /// on the way in, down to a crawl as it runs out — so the room's work is something the captain watches
    /// happen rather than a body that abruptly stops.</summary>
    private static double VacuumDrag(Reever r)
    {
        double lethal = HullVenting.SoakRequired(HullVenting.Infestation.Motile);
        if (r.VacuumSeconds <= 0 || lethal <= 0)
        {
            return 1.0;
        }
        double spent = System.Math.Clamp(r.VacuumSeconds / lethal, 0, 1);
        return 1.0 - (spent * VacuumSlowdown);
    }

    /// <summary>How much of its pace the vacuum has taken by the time it kills. Not to zero — a thing that
    /// freezes solid before it drops reads as a bug rather than as suffocation. FLAGGED for tuning.</summary>
    private const double VacuumSlowdown = 0.72;

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
            LogAutopilotEvent($"☠ Vented {name} — something alive went out with the air.");
        }
        else
        {
            LogAutopilotEvent($"💨 Vented {name}.");
        }

        // A compartment blowing to space is the loudest thing that has happened aboard her in forty years.
        MakeNoiseAboard(RoomCentre(name).X, RoomCentre(name).Y, LoudEarshot);

        RendererInterop.PlayCue("alarm");
        RebuildWreckDeck();   // vacuum that side, air this side: the doorway is now ten tonnes in a frame
        RequestVaultSave();
    }

    /// <summary>
    /// Every compartment currently being pumped down, with its own clock and its own rough mark.
    ///
    /// <para>This was ONE pump for the whole ship, which was an arbitrary limit I introduced and could not
    /// defend when asked (owner: "why can I not pump down more than one room… I want to pump down
    /// several?"). A damage-control system has valves per compartment; the only thing that should bound the
    /// captain is the reserve and the clock. Running four at once is a real strategy now — and a slow one,
    /// which is the whole cost of the thrifty road.</para>
    /// </summary>
    private readonly Dictionary<string, (double SecondsLeft, bool RoughBanked)> _pumps = [];

    /// <summary>Whether the corridor can go on the pumps: it still has air, it is not already running, and
    /// every compartment is shut or already empty. Otherwise the pump is draining the whole ship through
    /// eight open doorways.</summary>
    private bool SpinePumpable =>
        _spinePressurised
        && !_pumps.ContainsKey(HullVenting.SpineName)
        && _ventSpaces.Values.All(s => s.DoorShut || s.Vented);

    /// <summary>Put the corridor itself on the pumps — the end of "lock all the doors and pump them down".</summary>
    private void StartSpinePump()
    {
        if (_wreck is null || _pumps.ContainsKey(HullVenting.SpineName))
        {
            return;
        }
        if (!_spinePressurised)
        {
            _ventMessage = HullVenting.SpineAlreadyEmptyLine;
            RendererInterop.PlayCue("block");
            return;
        }
        if (!SpinePumpable)
        {
            _ventMessage = HullVenting.SpineNotSealedLine;
            RendererInterop.PlayCue("block");
            return;
        }

        _pumps[HullVenting.SpineName] =
            (HullVenting.PumpDownSeconds * HullVenting.SpinePumpMultiplier, false);
        _ventMessage = HullVenting.PumpRunningLine(HullVenting.SpineName,
            HullVenting.PumpDownSeconds * HullVenting.SpinePumpMultiplier);
        RendererInterop.PlayCue("board");

        // The loudest thing yet: the whole corridor draining, from one end of her to the other.
        MakeNoiseAboard(0, 0, LoudEarshot * 2);
    }

    /// <summary>Every compartment that could be pumped right now: sealed, still holding air, and not
    /// already running. What "pump all" acts on.</summary>
    private IReadOnlyList<string> PumpableRooms()
    {
        var ready = new List<string>();
        foreach ((string name, HullVenting.Space s) in _ventSpaces)
        {
            if (!_pumps.ContainsKey(name)
                && HullVenting.PumpReadiness(SpaceNow(name)) == HullVenting.VentReadiness.Ready)
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
        if (_pumps.TryGetValue(HullVenting.SpineName, out (double SecondsLeft, bool RoughBanked) pumping))
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
        LogAutopilotEvent($"🌬 The hull comes back to pressure — {cost} charges spent, {_refillCharges} left.");
        RendererInterop.PlayCue("reveal");
        RebuildWreckDeck();
        RequestVaultSave();
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
            LogAutopilotEvent($"🔒 {dogged} hatches dogged from the board.");

            // Eight doors slamming down the length of a dead ship is not a quiet thing to do.
            MakeNoiseAboard(0, 0, LoudEarshot);
            RequestVaultSave();
        }
    }

    /// <summary>Stop a pump. Past the rough mark this is the THRIFTY finish, not an abort: the air is
    /// already in the tanks and all you are giving up is a pressure low enough to kill.</summary>
    private void StopPump(string name)
    {
        if (!_pumps.TryGetValue(name, out (double SecondsLeft, bool RoughBanked) p))
        {
            return;
        }

        _pumps.Remove(name);
        _ventMessage = p.RoughBanked
            ? $"You shut the {name} pump down. It keeps what little is left in it, and the rest is aboard."
            : $"You shut the {name} pump down early. It still has most of its air, and none of it is yours.";
        RendererInterop.PlayCue("block");
    }

    /// <summary>Start the pump. Same interlock as the handle — a shut hatch, or you are pumping the ship.</summary>
    private void StartPumpDown(string name)
    {
        if (_wreck is null || _pumps.ContainsKey(name))
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

        _pumps[name] = (HullVenting.PumpDownSeconds, false);
        _ventMessage = CaptainCompartment() == name
            ? HullVenting.PumpUnderfootLine(name, HullVenting.PumpDownSeconds)
            : HullVenting.PumpRunningLine(name, HullVenting.PumpDownSeconds);
        RendererInterop.PlayCue("board");

        // A pump running in a dead ship is a heartbeat, and it runs for the best part of a minute.
        MakeNoiseAboard(RoomCentre(name).X, RoomCentre(name).Y, LoudEarshot);
    }

    /// <summary>Run the pump. It keeps running while the captain walks away — this is a machine, not a
    /// minigame — but the room does not reach vacuum until it finishes, so the soak clock starts LATE.
    /// That is the price of the thrifty road, on top of the wait.</summary>
    private void AdvancePump(double dtSeconds)
    {
        if (_wreck is null || _pumps.Count == 0)
        {
            return;
        }

        foreach (string name in _pumps.Keys.ToList())
        {
            (double left, bool banked) = _pumps[name];
            double before = left;
            left -= dtSeconds;

            // The rough mark: the mechanical stage is done and the air is home. Everything after this is
            // the long pull to a killing pressure, and it returns nothing to the tanks.
            //
            // PER PUMP, NOT PER FRAME. This was one variable declared outside the loop and OVERWRITTEN the
            // moment the corridor came up in the enumeration — so every pump processed after the spine in
            // the same frame was measured against the SPINE's rough mark (72s), which its own 50s clock
            // never reaches. The crossing test fires on exactly one frame, so those compartments silently
            // never banked their charge: the air went out of the room, the pump ran to the end, and nothing
            // arrived in the tanks. Owner, stranded with an empty hull and an empty reserve: "I run out of
            // air trying to fill the ship … I even had used the pumping on all so I should still have the
            // air mostly plus the reserves." He should have. It was being thrown away between frames.
            bool isSpine = name == HullVenting.SpineName;
            double roughAt = HullVenting.PumpRoughMark(isSpine);

            if (before > roughAt && left <= roughAt)
            {
                _refillCharges += HullVenting.PumpYield(isSpine);
                banked = true;
                _ventMessage = HullVenting.PumpRoughDoneLine(name);
                LogAutopilotEvent($"🛢 {name} roughed out — the air is in the tanks ({_refillCharges} charges).");
                RendererInterop.PlayCue("reveal");
            }

            if (left > 0)
            {
                _pumps[name] = (left, banked);
                if (_showVentPanel && !banked && _ventSelected == name)
                {
                    _ventMessage = HullVenting.PumpRunningLine(name, left);
                }
                continue;
            }

            _pumps.Remove(name);

            // Only NOW is it lethal. The charge was banked at the rough mark, a long time ago.
            if (isSpine)
            {
                // The corridor goes to vacuum — and unlike cracking a valve, the air is IN THE TANKS.
                // Same end state, opposite economics: the patient road keeps what the impatient one throws
                // away, and everything standing in that corridor now starts running out of time.
                _spinePressurised = false;
                _spineVacuumSeconds = 0.0;
            }
            else if (_ventSpaces.TryGetValue(name, out HullVenting.Space s))
            {
                _ventSpaces[name] = s with { Vented = true, VacuumSeconds = 0.0, HoldsSurvivor = false };
            }

            _ventMessage = HullVenting.PumpDoneLine(name);
            LogAutopilotEvent($"🛢 Pumped {name} down — the air is in the tanks ({_refillCharges} charges).");
            RendererInterop.PlayCue("alarm");
            RebuildWreckDeck();
            RequestVaultSave();
        }
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

    /// <summary>Rooms whose sealed door has already been opened once — so what was in there comes out ONCE,
    /// not every time a hatch swings.</summary>
    private readonly HashSet<string> _released = new(System.StringComparer.Ordinal);

    /// <summary>
    /// UNDOGGING A HATCH OPENS IT BOTH WAYS. Owner: <i>"and once unlocked the door starts to open also for
    /// the reevers :-D"</i>
    ///
    /// <para>Which is the entire reason the crew dogged it. A sealed room is a decision with teeth: the
    /// operating log says something in there is warm and moving and will never say what, so the captain
    /// chooses between never knowing and finding out — and finding out is not free. Whatever was shut in
    /// comes out into the corridor, aware, between the away team and nothing at all.</para>
    ///
    /// <para>It fires from the BOARD as readily as from the door, which is worse and correct: throw that
    /// switch from aft in ENGINEERING and you have opened a door you are not standing at.</para>
    /// </summary>
    private void ReleaseWhatWasSealedIn(string name)
    {
        if (_wreck is null
            || !_ventSpaces.TryGetValue(name, out HullVenting.Space s)
            || !s.Infested
            || s.Vented
            || !_released.Add(name))
        {
            return;
        }

        (string _, float x0, float x1, bool top) = System.Array.Find(
            WreckLayout.Compartments, c => c.Name == name);

        int came = 1 + DiceRule.Roll(
            DiceRule.Seed("sealed-count", (long)_wreck.Value.Id.GetHashCode(System.StringComparison.Ordinal),
                          name.GetHashCode(System.StringComparison.Ordinal)), 2).Face - 1;
        for (int i = 0; i < came && _reevers.Count < ReeverEngineCeiling; i++)
        {
            _reevers.Add(new Reever
            {
                X = ((x0 + x1) / 2.0) + (i * 2.0) - 1.0,
                Y = top ? -6.0 : 6.0,
                Facing = 0,
                JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)i + 7UL,
                EverSeen = true,
                LastSeenX = _avatarX,
                LastSeenY = _avatarY,
            });
        }

        ShowPulseMessage(
            $"🕷 The {name} hatch comes off its dogs — and it opens BOTH ways. Whatever the last crew shut " +
            "in there has been waiting on the other side of it, and it does not need a second invitation.");
        LogAutopilotEvent($"🕷 Opened the sealed {name} — {came} came out.");
        ApplyNerveShock(NervePips.SightingPips * (int)NervePips.PipUnit, "you opened the door they sealed");
        RendererInterop.PlayCue("alarm");
    }

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
        bool spineHadAir = _spinePressurised;
        _spinePressurised = !r.SpineVented && _spinePressurised;
        if (spineHadAir && !_spinePressurised)
        {
            _spineVacuumSeconds = 0.0;
        }

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

        MakeNoiseAboard(RoomCentre(name).X, RoomCentre(name).Y, LoudEarshot);   // a ship equalising is not quiet
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
        MakeNoiseAboard(RoomCentre(name).X, RoomCentre(name).Y, QuietEarshot);   // six dogs, by hand
        if (!sealing)
        {
            ReleaseWhatWasSealedIn(name);   // at the door, and it opens both ways
        }
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
        // PUMPING RIGHT NOW. Owner: "we have the vacuum indicated but not the pumping on the map … should
        // there be some kind of pumping right now indicator here." Yes — with several pumps able to run at
        // once, the board is the only place that can tell you which rooms are working and how far along
        // each one is. The room says so itself, and it counts DOWN, because unlike the soak this clock has
        // a known end.
        if (_pumps.TryGetValue(name, out (double SecondsLeft, bool RoughBanked) pumping))
        {
            string t = HullVenting.SoakLabel(pumping.SecondsLeft);
            return (Longest(roomWidth, [$"PUMPING {t}", $"PUMP {t}", t]),
                    pumping.RoughBanked ? "vent-tag banked-tag" : "vent-tag pumping-tag");
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
