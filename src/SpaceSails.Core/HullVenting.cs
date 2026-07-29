namespace SpaceSails.Core;

/// <summary>
/// #488 · BLOW THE COMPARTMENT. Owner, on the infested hull:
///
/// <para><i>"When we use space suits we could vent the infested boarded vessel into space to try get rid
/// of the infestation, but that control might be at a technical space :-D … there might be a small risk
/// that we kill possible survivors with that also … we might use dice throw … that would make the decision
/// hard :-D … in Battlestar Galactica a big note is made of those controls of the venting of the ship
/// compartments … maybe the bridge controls are non-functioning so we need to go to where the machinery
/// (valves etc) are … it could even be focused to room level spaces in the ship, just close the door before
/// the vent of particularly infested space before venting it. I guess there might be a big reward for
/// saving survivors of the crash."</i></para>
///
/// <para><b>The whole mechanic is one hard decision, made with bad information.</b> You are in a suit; the
/// wreck's air is nothing to you. Open a compartment to space and whatever is in it dies. The infestation
/// dies. So does anything else.</para>
///
/// <para>And the survivors are not a coincidence: this wreck's own evidence is that <b>every barricade was
/// built from the INSIDE</b>. Somebody sealed themselves in. Which means someone may still be behind one —
/// and it means the compartment most likely to hold a survivor is also the one most likely to hold the
/// thing they were hiding from.</para>
///
/// <para><b>Why the sensor cannot save you.</b> A life-sign read cannot tell a survivor from the
/// infestation — both are warm, both move. That is the entire design: the reading tells you something is
/// ALIVE in there, never WHAT. Venting on a positive read is a coin you chose to flip. Venting on a
/// negative read is a coin you chose not to look at.</para>
///
/// <para>Pure and seeded: the same wreck always hides its survivors in the same places, and the same read
/// always rolls the same face, so a test can pin every outcome and a save can be reloaded honestly.</para>
/// </summary>
public static class HullVenting
{
    /// <summary>Roughly one compartment in this many on an infested hull still holds someone alive, sealed
    /// in behind their own barricade. Rare enough that venting is usually clean, common enough that it is
    /// never a free action. FLAGGED for the owner's tuning.</summary>
    public const int SurvivorOneIn = 5;

    /// <summary>What a rescued survivor is worth. They are the reward for the careful road — and worth more
    /// than the wreck's own finder's fee, because a living witness is worth more than a filed opinion.
    /// FLAGGED for tuning.</summary>
    public const int SurvivorRescueCr = 45_000;

    /// <summary>Venting a compartment with someone alive in it. No credits change hands; this is what it
    /// costs the captain (nerve pips, through the #480 seam) and it is deliberately heavy.</summary>
    public const int VentedSurvivorNerveCost = 40;

    /// <summary>The die the life-sign read rolls.</summary>
    public const int ReadDie = DiceRule.D20;

    /// <summary>A read at or over this is CONFIDENT — the instrument is telling you something real, one way
    /// or the other. Under it, the return is mush and the captain is choosing blind.</summary>
    public const int ConfidentRead = 12;

    /// <summary>What the panel can say about a compartment before you pull the handle.</summary>
    public enum LifeSign
    {
        /// <summary>Nothing warm in there. As close to safe as this gets — but the instrument is old.</summary>
        Empty,

        /// <summary>Something alive. It cannot tell you WHAT, and that is the point: the infestation reads
        /// exactly like a survivor, because both are warm and both move.</summary>
        SomethingAlive,

        /// <summary>The return is mush — scatter off the bulkhead, a dying sensor, forty years of neglect.
        /// You will be deciding on nothing at all.</summary>
        Unreadable,
    }

    /// <summary>Whether a compartment can be blown at all. The door has to be SHUT: an open compartment
    /// vents the corridor you are standing in, and the panel refuses. Owner: <i>"just close the door before
    /// the vent of particularly infested space before venting it."</i></summary>
    public enum VentReadiness
    {
        /// <summary>Sealed and ready. Pull the handle.</summary>
        Ready,

        /// <summary>The door is open. Venting this would empty the spine with you in it.</summary>
        DoorOpen,

        /// <summary>Already blown — there is nothing left in there to kill.</summary>
        AlreadyVented,

        /// <summary>You are standing in it. The suit is rated for vacuum, not for being fired out of a
        /// compartment with the air — and a panel that let you do this to yourself would be a joke rather
        /// than a decision.</summary>
        CaptainInside,
    }

    /// <summary>One compartment as the valve panel sees it. <paramref name="CaptainInside"/> is live state —
    /// the captain walks, so it changes under the panel while it is open.</summary>
    /// <param name="VacuumSeconds">How long this compartment has been open to space. Owner, 2026-07-29:
    /// <i>"there might be a counter on how long the room has been in vacuum … so it needs certain time for
    /// certain infestations."</i> The vacuum is the weapon and it works at its own speed.</param>
    /// <param name="Kind">What is growing in there, and therefore how long the vacuum needs. The panel
    /// never shows this — see <see cref="Read"/>.</param>
    public readonly record struct Space(
        string Name, bool DoorShut, bool Vented, bool Infested, bool HoldsSurvivor, bool CaptainInside = false,
        double VacuumSeconds = 0.0, Infestation Kind = Infestation.None);

    // ── The soak: vacuum kills, but not instantly ─────────────────────────────────────────────────────

    /// <summary>What has got into a compartment, and therefore how long it takes the vacuum to finish it.
    /// The captain is never told which of these they are holding: <see cref="Read"/> reports that something
    /// is alive, never what — so "how long is long enough" is a judgement, not a lookup.</summary>
    public enum Infestation
    {
        /// <summary>Nothing in there but forty years of stale air.</summary>
        None,

        /// <summary>The pack itself — warm, moving, lungs. Vacuum takes it quickly.</summary>
        Motile,

        /// <summary>The fibrous growth over the cargo racks. No lungs to lose; it dies of cold and boiling
        /// slowly, from the outside in.</summary>
        Fibrous,

        /// <summary>Encysted. It has done this before and it is in no hurry. If the away team leaves after a
        /// couple of minutes satisfied, this is the one that was still alive when they refilled the room.</summary>
        Encysted,
    }

    /// <summary>How long the vacuum needs, per kind. Tuned to the SHAPE OF A BOARDING rather than to
    /// biology: blowing a hold and standing there watching a clock is not a game, so the long soaks are
    /// long enough that the captain has to go and do something else — read the log, find the manifest — and
    /// come back. That is the point of the counter. FLAGGED for the owner's tuning.</summary>
    public const double MotileSoakSeconds = 20.0;
    public const double FibrousSoakSeconds = 75.0;
    public const double EncystedSoakSeconds = 180.0;

    /// <summary>The vacuum time this kind needs before it is certainly dead.</summary>
    public static double SoakRequired(Infestation kind) => kind switch
    {
        Infestation.Motile => MotileSoakSeconds,
        Infestation.Fibrous => FibrousSoakSeconds,
        Infestation.Encysted => EncystedSoakSeconds,
        _ => 0.0,
    };

    /// <summary>Which kind is in this compartment — seeded off the wreck and the compartment, so a given
    /// hull always holds the same thing in the same place and a reload cannot re-roll it into something
    /// easier. Only an infested compartment holds anything at all.</summary>
    public static Infestation InfestationIn(string wreckId, string compartment, bool infested)
    {
        if (!infested)
        {
            return Infestation.None;
        }

        // Weighted so the hardy one is the minority — but a minority you cannot identify, which is what
        // makes a two-minute soak feel like a decision rather than a formality.
        ulong h = StableHash.Of($"{wreckId}|kind|{compartment}");
        return (h % 6) switch
        {
            0 or 1 or 2 => Infestation.Motile,
            3 or 4 => Infestation.Fibrous,
            _ => Infestation.Encysted,
        };
    }

    /// <summary>Has this compartment been open to space long enough to have finished what was in it?</summary>
    public static bool SoakComplete(in Space space) =>
        space.Vented && space.VacuumSeconds >= SoakRequired(space.Kind);

    /// <summary>The counter on the board — mm:ss, the plainest possible readout. It shows how long the room
    /// has been open, and DELIBERATELY not how long it needs: the whole tension is that the second number
    /// does not exist for the captain.</summary>
    public static string SoakLabel(double vacuumSeconds)
    {
        int total = (int)System.Math.Max(0.0, vacuumSeconds);
        return $"{total / 60:00}:{total % 60:00}";
    }

    // ── Pressure locks: a door with vacuum behind it does not open ────────────────────────────────────

    /// <summary>
    /// THE DOOR IS HELD BY THE PRESSURE, NOT BY A LATCH. Owner, on walking straight into a compartment he
    /// had just blown: <i>"we need some kind of door locked due to vacuum feature … maybe a general gauge
    /// that says the door is shut due to pressure difference."</i>
    ///
    /// <para>He is right, and the physics is on his side: one atmosphere across a door is roughly ten
    /// tonnes trying to keep it closed. Nobody opens that by hand. So a compartment you have vented cannot
    /// be walked into — which is what finally makes <see cref="Refill"/> load-bearing rather than
    /// decorative, and turns venting the deep hold into a real trade: kill what is in there, or keep being
    /// able to get in there.</para>
    ///
    /// <para>It takes air on ONE side and vacuum on the other. A hull that has been open to space for forty
    /// years has no differential anywhere and every door on her swings freely — which is exactly right for
    /// <see cref="Derelict.WreckCause.VentedByOneOfTheirOwn"/>, where the only door that fights you is the
    /// one room somebody kept air in.</para>
    /// </summary>
    public static bool DoorHeldByPressure(in Space space, bool spinePressurised) =>
        !space.Vented != spinePressurised;   // air on exactly one side of it

    /// <summary>The gauge on the door, 0…1 — how hard it is being held. One side against the other; there
    /// is no in-between, so this is 1 when there is a differential and 0 when there is not. Kept as a
    /// fraction rather than a bool because the readout is a DIAL and a dial that only ever reads full or
    /// empty is still a dial.</summary>
    public static double PressureGauge(in Space space, bool spinePressurised) =>
        DoorHeldByPressure(space, spinePressurised) ? 1.0 : 0.0;

    /// <summary>What the captain reads at a door that will not move.</summary>
    public static string PressureLockLine(string name, bool compartmentIsTheVacuumSide) =>
        compartmentIsTheVacuumSide
            ? $"The {name} door will not move. The gauge beside it is hard over: hard vacuum that side, air " +
              "this side, and about ten tonnes of atmosphere holding the door in its frame. It is not " +
              "locked. It is LOADED."
            : $"The {name} door will not move — the gauge reads the differential the wrong way round. There " +
              "is still air in there and nothing at all out here, and the door is being held shut from the " +
              "inside by the last breathable room on the ship.";

    /// <summary>Cracking the equalisation valve on the door itself — the thing every real pressure door
    /// has, and the reason this mechanic can never strand a captain. It costs nothing and it costs
    /// everything: the two volumes even out, which means THE AIR GOES. Whichever side still had an
    /// atmosphere does not have one afterwards.
    ///
    /// <para>So there are two roads into a room you blew, and they are priced differently: crack the valve
    /// and lose the ship's air for free, or spend a <see cref="RefillChargesPerBoarding">charge</see> off
    /// the shuttle and keep it.</para></summary>
    public const string EqualiseLine =
        "You crack the equalisation valve and step back. The gauge falls off its stop over about four " +
        "seconds, and the door comes off its frame the moment there is nothing left holding it. Both sides " +
        "read the same now — which is to say both sides read nothing.";

    /// <summary>What one cracked valve did to the whole ship.</summary>
    /// <param name="SpineVented">The corridor lost its air — which is what makes this a whole-ship move.</param>
    /// <param name="Spaces">Every compartment's new state, in the order they were handed in.</param>
    /// <param name="RoomsOpened">How many compartments went to vacuum along with the spine. These are the
    /// ones whose doors were standing OPEN: one volume, one pressure, one valve.</param>
    /// <param name="SurvivorsLost">People who were behind an OPEN door when the ship equalised. The warning
    /// on the valve says this will happen; the seal is how you stop it.</param>
    public readonly record struct EqualiseResult(
        bool SpineVented, IReadOnlyList<Space> Spaces, int RoomsOpened, int SurvivorsLost);

    /// <summary>
    /// THE CORRIDOR IS NOT REFILLABLE, AND THAT IS THE PRICE. Owner, immediately after venting the ship
    /// through one door: <i>"but can I now undo the venting of the hallway?"</i>
    ///
    /// <para>No. A compartment is a room; the spine is the length of the ship, and the away team's whole
    /// reserve is two compartments' worth. Cracking a valve is FREE and IRREVERSIBLE; refilling a room
    /// COSTS and is reversible. That asymmetry is the only thing that makes the choice at the door weigh
    /// anything — otherwise the equalisation valve is a shortcut with no downside and the valve board might
    /// as well not exist.</para>
    /// </summary>
    public const string SpineRefillRefusal =
        "You cannot fill a corridor from a shuttle. That is not a room — it is the length of the ship, and " +
        "everything you brought out here would not raise it off zero. The air you let out of her is gone.";

    /// <summary>Whether somebody can be walked off this ship alive. You cannot carry a person out through a
    /// vacuum corridor, so a captain who vents the hallway has written off every survivor aboard — which
    /// the valve warned them about before they cracked it.</summary>
    public static bool CanCarrySurvivorOut(bool spinePressurised) => spinePressurised;

    /// <summary>Why the rescue will not happen.</summary>
    public const string NoAirToCarryThemOut =
        "There is nothing to carry them out through. The corridor is vacuum from here to the airlock and " +
        "they have no suit — whatever you were going to do for them, you did it when you cracked that valve.";

    /// <summary>
    /// CRACK ONE VALVE, VENT THE SHIP. Owner, on finding the strategy himself: <i>"I kind of like that a
    /// lot since now just having a single vented space allows venting the ship from that pressure
    /// equalization valve, if the actual controls are too crowded by infestation."</i>
    ///
    /// <para>It works because it is only physics: an open door is not a boundary, it is a hole. The spine
    /// and every compartment standing open to it are ONE VOLUME, so equalising any of them against a vacuum
    /// compartment empties all of it at once. A captain who cannot fight their way aft to the valve board
    /// can still vent the whole hull from a single door — the long way round, for free, and giving up every
    /// breath aboard her to do it.</para>
    ///
    /// <para>What survives it is exactly what somebody shut a door on. That is the counterplay, and the
    /// reason a room-by-room seal is worth having: <b>the infestation has not read the ship's manual.</b>
    /// It does not close doors behind itself, so door discipline is a tool that only the captain holds.</para>
    /// </summary>
    public static EqualiseResult EqualiseAt(
        string doorName, IReadOnlyList<Space> spaces, bool spinePressurised)
    {
        System.ArgumentNullException.ThrowIfNull(spaces);

        var result = new List<Space>(spaces.Count);
        Space cracked = default;
        foreach (Space s in spaces)
        {
            if (string.Equals(s.Name, doorName, System.StringComparison.Ordinal))
            {
                cracked = s;
            }
        }

        // Air out here, vacuum in there: the corridor empties into it, and takes every room standing open
        // to the corridor with it. Sealed rooms keep what they have.
        if (cracked.Vented && spinePressurised)
        {
            int opened = 0;
            int lost = 0;
            foreach (Space s in spaces)
            {
                bool sharesTheVolume = !s.DoorShut && !s.Vented;
                if (sharesTheVolume)
                {
                    opened++;
                    lost += s.HoldsSurvivor ? 1 : 0;
                    result.Add(s with { Vented = true, VacuumSeconds = 0.0, HoldsSurvivor = false });
                }
                else
                {
                    result.Add(s);
                }
            }
            return new EqualiseResult(SpineVented: true, result, opened, lost);
        }

        // Vacuum out here, air in there: the room empties into the ship. Nothing else changes — the spine
        // had nothing to lose.
        int emptied = 0;
        foreach (Space s in spaces)
        {
            bool isTheOne = string.Equals(s.Name, doorName, System.StringComparison.Ordinal) && !s.Vented;
            if (isTheOne)
            {
                emptied += s.HoldsSurvivor ? 1 : 0;
                result.Add(s with { Vented = true, VacuumSeconds = 0.0, DoorShut = false, HoldsSurvivor = false });
            }
            else
            {
                result.Add(s);
            }
        }
        return new EqualiseResult(SpineVented: !spinePressurised, result, cracked.Vented ? 0 : 1, emptied);
    }

    /// <summary>A compartment whose door is dogged shut is a WALL, not a suggestion — you open it at the
    /// door. That is what makes sealing a room a real act: it keeps the captain out, it keeps the ship's
    /// air in, and it is the only thing that survives somebody cracking a valve two compartments away.</summary>
    public static bool DoorwayBlocked(in Space space, bool spinePressurised) =>
        space.DoorShut || DoorHeldByPressure(space, spinePressurised);

    /// <summary>The line for sealing a compartment by hand, at its own door. The counterplay to the
    /// whole-ship valve, and the reason it is worth walking to a door you could have thrown from the
    /// board.</summary>
    public static string SealLine(string name) =>
        $"You dog the {name} hatch down by hand, all six of them, and put your weight on the last one. " +
        "Whatever happens to the pressure in the rest of this ship now happens without it.";

    public static string UnsealLine(string name) =>
        $"You undog the {name} hatch. It swings when you push it — nothing is holding it but the hinges.";

    // ── Pumping down: the thrifty way to empty a room ─────────────────────────────────────────────────

    /// <summary>
    /// PUMP IT DOWN INSTEAD OF BLOWING IT. Owner, from the lab bench: <i>"I used to pump vacuum chamber to
    /// be empty so that air can be collected instead of venting, when things have power."</i>
    ///
    /// <para>Which is the honest third button, and it turns the board's economy into a loop instead of a
    /// one-way drain. Blowing a compartment is instant, free and WASTEFUL — the air is gone into the dark
    /// and you will be buying it back off the shuttle a breath at a time. Pumping the same room down takes
    /// <see cref="PumpDownSeconds"/> and puts what was in it INTO THE RESERVE: one compartment out, one
    /// refill charge in.</para>
    ///
    /// <para>The cost is the clock, and on an infested hull the clock is the whole game — you are standing
    /// at a board listening to a pump for the best part of a minute while something roams a corridor you
    /// have to walk back down. Fast and wasteful, or slow and thrifty. That is a better decision than
    /// either button had on its own.</para>
    /// </summary>
    public const double PumpDownSeconds = 50.0;

    /// <summary>
    /// WHERE THE ROUGHING PUMP FINISHES AND THE LONG TAIL BEGINS. Owner, on how the real thing works: <i>"I
    /// think NASA uses cryopumps as high vacuums and some mechanical pumps for rough vacuum in that
    /// satellite simulation chamber of theirs, though the rough vacuum probably already does 95% of the job
    /// or more, so the rest is not that significant in material savings / pressure wise."</i>
    ///
    /// <para>That asymmetry is a mechanic for free, so it is modelled exactly as he describes it: the
    /// mechanical stage is quick and recovers essentially ALL of the air — the charge is banked here — and
    /// everything after it is the slow pull down to a pressure that will actually kill something, which
    /// returns nothing at all to the tanks.</para>
    ///
    /// <para>So the captain gets a real decision in the middle of a machine cycle: take the air and go, or
    /// stand there through the tail for the vacuum. Walking away at the rough mark is not a failure — it is
    /// the whole saving, and it costs you the kill.</para>
    /// </summary>
    public const double PumpRoughSeconds = 18.0;

    /// <summary>What a pumped-down compartment puts back in the reserve. One room, one breath — banked at
    /// the rough mark, not at the end, because that is where the air actually comes back.</summary>
    public const int PumpDownYieldsCharges = 1;

    /// <summary>The line at the rough mark — the air is home, the room is not yet lethal.</summary>
    public static string PumpRoughDoneLine(string name) =>
        $"The mechanical stage finishes and the note of the pump changes. {name} is down to a few percent " +
        "and effectively all of her air is in the tanks — that is the saving, and it is already yours. " +
        "What is left is the long pull to a pressure that kills, which recovers nothing. You can walk away " +
        "with the air, or stand here and wait for the vacuum.";

    /// <summary>Whether the pump can be run at all. It needs the same shut hatch a vent does — you cannot
    /// pump down a room that is open to the corridor, you would just be pumping the ship — and it needs
    /// somewhere for the air to GO, so a full reserve has nothing to offer.</summary>
    public static VentReadiness PumpReadiness(in Space space) => Readiness(space);

    /// <summary>The line while the pump runs. Deliberately unglamorous: this is the patient road.</summary>
    public static string PumpRunningLine(string name, double secondsLeft) =>
        $"Pumping {name} down — {System.Math.Max(0, (int)secondsLeft)}s. The reserve is filling as the room " +
        "empties. Whatever is in there has time to notice.";

    public static string PumpDoneLine(string name) =>
        $"{name} reads hard vacuum, and it did not cost you a thing — the air is in the shuttle's tanks " +
        "instead of forty kilometres behind her. The pump winds down. The room is as dead as if you had " +
        "blown it, and you are one breath richer than if you had.";

    // ── The shuttle's own lock ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SHUTTLE IS NOT PART OF THIS SHIP'S VOLUME, and that is the whole point of a lock. Owner: <i>"Let's
    /// keep the shuttle door locked in such a way that we don't vent our own shuttle by accident. Also we
    /// don't want any uninvited infestations going there … if our shuttle has an airlock then that could
    /// match the pressure outside first."</i>
    ///
    /// <para>So it CYCLES rather than refuses. Going home always works: the lock matches whatever the wreck
    /// is reading before the outer door moves, which means the shuttle's own air is never once exposed to
    /// the hull — crack every valve aboard her and the shuttle does not notice. That is a working airlock
    /// rather than a rule bolted on to protect the player from themselves.</para>
    /// </summary>
    public static string ShuttleLockLine(bool spinePressurised) =>
        spinePressurised
            ? "The lock reads the ship: stale, thin, breathable-if-you-had-to. Equal enough. The inner door " +
              "comes off its dogs and the outer follows, and you are back aboard your own boat with her air " +
              "still in her."
            : "The lock will not open onto vacuum with the boat's air behind it. It pumps down first — " +
              "thirty seconds of your own atmosphere going back into the tanks rather than out of the door — " +
              "then matches the nothing outside and lets you through. Your boat keeps every breath she came " +
              "with.";

    /// <summary>The other half of the lock's job, and the one that matters on an infested hull. The same
    /// crew-only rule the ship's tube runs on: the away team work it, and nothing else aboard can. Whatever
    /// is loose on this wreck does not get a vote on whether it comes home with you.</summary>
    public const string ShuttleLockHoldsLine =
        "The lock is dogged behind you and it stays dogged. It is keyed to the away team, and the thing " +
        "loose on this hull has never operated a hatch in its life — it can reach the door. It cannot open " +
        "the door.";

    /// <summary>Why keeping the air is worth a charge: a compartment nobody can breathe in is a compartment
    /// nobody can be carried out of. The rescue seam (<see cref="SurvivorRescueCr"/>) is the reason the
    /// expensive road exists at all.</summary>
    public const string EqualiseWarnsLine =
        "⚠ Equalising empties the whole ship. Anyone still breathing behind a door aboard her stops.";

    // ── Refill: the other half of a damage-control board ──────────────────────────────────────────────

    /// <summary>Whether a compartment can be brought back to pressure. Owner, mid-playtest: <i>"Now I
    /// vacuumed the bridge, but there should be controls to re-fill it also?"</i> — correct, and a board
    /// with only one direction is a trigger rather than a board.</summary>
    public enum RefillReadiness
    {
        /// <summary>Shut, empty to space, and there is a charge to spend.</summary>
        Ready,

        /// <summary>It already has air in it.</summary>
        NotVented,

        /// <summary>The door is open — you would be filling the whole ship through a doorway.</summary>
        DoorOpen,

        /// <summary>The shuttle has nothing left to give. She is a forty-year-old wreck; every breath of
        /// this came aboard with the away team.</summary>
        NoReserve,
    }

    /// <summary>What it costs the captain to hear something take a breath in a room they had already
    /// decided was finished. Smaller than <see cref="VentedSurvivorNerveCost"/> — you have not killed
    /// anybody, you have only been impatient in front of something patient. FLAGGED for tuning.</summary>
    public const int RefilledTooSoonNerveCost = 12;

    /// <summary>How many compartments one boarding can bring back to pressure. Small on purpose: refilling
    /// is a real spend, not a toggle, and the number is what stops "blow everything and refill whatever
    /// squeals" from being the dominant strategy. FLAGGED for tuning.</summary>
    public const int RefillChargesPerBoarding = 2;

    /// <summary>Can this one be filled?</summary>
    public static RefillReadiness RefillState(in Space space, int chargesLeft) =>
        !space.Vented ? RefillReadiness.NotVented
        : !space.DoorShut ? RefillReadiness.DoorOpen
        : chargesLeft <= 0 ? RefillReadiness.NoReserve
        : RefillReadiness.Ready;

    /// <summary>Why the fill valve will not move.</summary>
    public static string RefillRefusalLine(RefillReadiness readiness, string name) => readiness switch
    {
        RefillReadiness.NotVented => $"{name} already has air in it. Bad air, but air.",
        RefillReadiness.DoorOpen =>
            $"{name} is open to the spine — you would be venting the shuttle's reserve into the whole ship " +
            "through one doorway. Shut the door first.",
        RefillReadiness.NoReserve =>
            "The shuttle has nothing left to give. Every breath aboard this hull came out here with you, and " +
            "you have spent it.",
        _ => "",
    };

    /// <summary>What bringing a compartment back to pressure actually did.</summary>
    /// <param name="Filled">Whether a charge was spent and the room came back to pressure.</param>
    /// <param name="SomethingSurvived">The room was refilled BEFORE the vacuum finished what was in it. The
    /// captain is told plainly — this is not a hidden failure, it is the cost of being impatient, and they
    /// have one fewer charge to try again with.</param>
    public readonly record struct RefillOutcome(bool Filled, bool SomethingSurvived, string Line);

    /// <summary>
    /// Fill it. AIR COMES BACK. NOBODY DOES.
    ///
    /// <para>This is the rule the whole feature is balanced on. A refill that undid a vent would gut the
    /// decision the panel exists for — you would blow every compartment and restore the ones that squealed.
    /// So what returns is pressure and nothing else: not the nest, and not whoever was beating on the door
    /// from the inside.</para>
    ///
    /// <para>What a refill CAN do is save something that is not dead yet — which is precisely the mistake
    /// available to a captain who did not leave it long enough.</para>
    /// </summary>
    public static RefillOutcome Refill(in Space space, int chargesLeft)
    {
        RefillReadiness readiness = RefillState(space, chargesLeft);
        if (readiness != RefillReadiness.Ready)
        {
            return new RefillOutcome(false, false, RefillRefusalLine(readiness, space.Name));
        }

        bool survived = space.Infested && !SoakComplete(space);

        string line = survived
            ? $"{space.Name} comes back to pressure — and something in there takes the first breath before " +
              "you do. It was not finished. Now it is warm again, and it has learned the sound of that valve."
            : $"{space.Name} comes back to pressure. For about four seconds it sounds like a ship again. " +
              "Nothing that went out with the air comes back in with it.";

        return new RefillOutcome(true, survived, line);
    }

    /// <summary>Can this one be blown? Checked in the order the captain would care about.</summary>
    public static VentReadiness Readiness(in Space space) =>
        space.Vented ? VentReadiness.AlreadyVented
        : space.CaptainInside ? VentReadiness.CaptainInside
        : !space.DoorShut ? VentReadiness.DoorOpen
        : VentReadiness.Ready;

    /// <summary>Why the handle will not move, in the panel's own voice.</summary>
    public static string RefusalLine(VentReadiness readiness, string name) => readiness switch
    {
        VentReadiness.DoorOpen =>
            $"The interlock holds. {name} is still open to the spine — shut the door before you pull this, " +
            "unless you fancy going out with it.",
        VentReadiness.AlreadyVented =>
            $"{name} is already open to space. There is nothing left in there to kill.",
        VentReadiness.CaptainInside =>
            $"The interlock holds — YOU are in {name}. The suit is rated for vacuum, not for being fired out " +
            "of a room with the air.",
        _ => "",
    };

    /// <summary>
    /// Is someone alive in this compartment? Seeded off the wreck and the compartment name, so a given
    /// wreck always hides her survivors in the same places — a reload cannot re-roll them, and a test can
    /// pin them.
    ///
    /// <para>Only an INFESTED hull has survivors to find, and only in a compartment the thing has also got
    /// into. That is not cruelty for its own sake: the barricades were built from the inside, so the room
    /// somebody sealed themselves into is exactly the room something was trying to get at.</para>
    /// </summary>
    public static bool HidesSurvivor(string wreckId, string compartment, Derelict.WreckCause cause)
    {
        if (cause != Derelict.WreckCause.Infested)
        {
            return false;
        }
        ulong h = StableHash.Of($"{wreckId}|survivor|{compartment}");
        return h % (ulong)SurvivorOneIn == 0;
    }

    /// <summary>
    /// The life-sign read at the valve panel — a seeded d20 the player SEES, in the house's dice idiom.
    ///
    /// <para>A confident roll reports honestly whether anything in there is alive. A poor roll returns
    /// <see cref="LifeSign.Unreadable"/>. What it can NEVER do is distinguish a survivor from the
    /// infestation, so <see cref="LifeSign.SomethingAlive"/> on an infested compartment that also holds a
    /// survivor reads identically to one that does not. The instrument is not broken; the question is
    /// simply not answerable from out here.</para>
    /// </summary>
    public static (DiceRoll Roll, LifeSign Sign) Read(ulong seed, in Space space)
    {
        DiceRoll roll = DiceRule.Roll(DiceRule.Seed(seed, $"lifesign:{space.Name}"), ReadDie);
        if (roll.Face < ConfidentRead)
        {
            return (roll, LifeSign.Unreadable);
        }
        bool anythingWarm = space.Infested || space.HoldsSurvivor;
        return (roll, anythingWarm ? LifeSign.SomethingAlive : LifeSign.Empty);
    }

    /// <summary>What the captain reads on the way to pulling the reading — the whole rule of the
    /// instrument, in the place a rule belongs: on the control, before you use it, not in a card afterwards.
    ///
    /// <para>The framing matters. This is not a scan the away team is running; it is <b>the compartment's
    /// own instruments, read back</b> — which is why a ship that has been dead for forty years still has an
    /// answer, and why the answer is a record rather than a detection. It says something is warm and
    /// moving. It has never been able to say what.</para></summary>
    public const string OperatingLogHint =
        "The compartment's own instruments, read back — thermal, movement, the last time a door cycled. " +
        "Ask as often as you like: a record says the same thing every time until the room itself changes. " +
        "It records that something in there is warm and moving. It has never been able to tell you WHAT.";

    /// <summary>The words the panel says for a reading — never more certain than the instrument is.</summary>
    public static string ReadLine(LifeSign sign) => sign switch
    {
        LifeSign.Empty => "cold. nothing moving in there.",
        LifeSign.SomethingAlive => "SOMETHING IS ALIVE IN THERE. The return cannot tell you what.",
        LifeSign.Unreadable => "the return is mush — scatter, or a dying sensor. It tells you nothing.",
        _ => "",
    };

    /// <summary>What blowing a compartment actually did.</summary>
    public readonly record struct VentOutcome(
        bool Blown,
        bool InfestationCleared,
        bool SurvivorKilled,
        string Line);

    /// <summary>
    /// Blow it. Everything in the compartment goes out with the air.
    ///
    /// <para>Refuses if the door is not shut — venting an open compartment empties the corridor the captain
    /// is standing in, and the panel is old but it is not stupid.</para>
    /// </summary>
    public static VentOutcome Vent(in Space space)
    {
        VentReadiness readiness = Readiness(space);
        if (readiness != VentReadiness.Ready)
        {
            return new VentOutcome(false, false, false, RefusalLine(readiness, space.Name));
        }

        // The vacuum is the weapon and it does not work instantly. Venting an infested compartment STARTS
        // the clock; SoakComplete finishes it. So the panel can no longer promise "the nest goes with it"
        // in the same breath as the handle — the captain has to leave it, and decide when leaving it is
        // long enough.
        string line = space.HoldsSurvivor
            ? $"{space.Name} blows out in a single breath. Something goes with it that was beating on the " +
              "door from the inside — and you will not know for certain what it was."
            : space.Infested
                ? $"{space.Name} blows out in a single breath. Whatever is in there is in vacuum now. " +
                  "That is not the same as dead — give it time, and do not open this door to find out."
                : $"{space.Name} blows out in a single breath. Nothing was in there but forty years of stale air.";

        // InfestationCleared is now only ever true for a compartment with nothing in it to kill. What is
        // alive in there dies on the clock, not on the handle.
        return new VentOutcome(true, InfestationCleared: false, space.HoldsSurvivor, line);
    }

    /// <summary>
    /// The valve station is NOT on the bridge. Owner, borrowing from Battlestar Galactica: <i>"maybe the
    /// bridge controls are non-functioning so we need to go to where the machinery (valves etc) are to
    /// activate those."</i>
    ///
    /// <para>That is the whole reason this mechanic has any tension. A bridge switch would let the captain
    /// clear the ship from the doorway they arrived at. The valves are aft, in the technical spaces —
    /// which on an infested hull means walking TOWARD the thing to get the tool that kills it, and then
    /// walking back out past whatever you did not vent.</para>
    /// </summary>
    public const string ValveCompartment = "ENGINEERING";

    // ── The hull that was already vented, by one of her own ───────────────────────────────────────────

    /// <summary>Whether this compartment is ALREADY hard vacuum when the away team arrives — true only on
    /// <see cref="Derelict.WreckCause.VentedByOneOfTheirOwn"/>, and true of every compartment but one.
    ///
    /// <para>Owner, 2026-07-29: <i>"one ship destiny might be that somebody went crazy due to an object and
    /// vented most of the ship."</i> The exception is not chosen — it FALLS OUT of a rule the game already
    /// enforces and the player has already been refused by. <see cref="Readiness"/> will not blow the room
    /// you are standing in, so the one room with air in it is the room the person who did this was standing
    /// in: <see cref="ValveCompartment"/>, at the board, with every handle pulled.</para>
    ///
    /// <para>Which means the mimic map does the accusing. The captain raises the valve board — the same
    /// panel, the same eight rooms — and seven of them read VACUUM around the one they are standing in.
    /// Nothing has to say who did it. An interlock written as a safety feature, read years later as a
    /// confession.</para></summary>
    public static bool StartsVented(Derelict.WreckCause cause, string compartment) =>
        cause == Derelict.WreckCause.VentedByOneOfTheirOwn
        && !string.Equals(compartment, ValveCompartment, System.StringComparison.Ordinal);

    /// <summary>The one room with air in it, and the reason there is one.</summary>
    public const string TheRoomTheyWereStandingIn = ValveCompartment;

    /// <summary>What the log station reads on a vented hull — the SECOND madness. Owner: <i>"the survivors
    /// also fell to some other craziness after that."</i> They did not write down what happened, because in
    /// the log nothing did: months of watch rotations and meal counts in one immaculate administrative
    /// hand, forty names signing on and off a ship with nobody aboard. Deliberately the same forty as the
    /// ice moon's and the glitch card's PATTERN 40 — the third place the motif surfaces, and the first
    /// where the captain finds it as an object instead of a rumour.</summary>
    public const string VentedShipLogLine =
        "The log does not stop at the venting. It runs on for eleven more months in one steady " +
        "administrative hand: watch rotations, meal counts, stores drawn, forty names signing on and off " +
        "watch, every entry countersigned. Nobody is ever recorded as absent. Nothing is ever recorded as " +
        "having happened. The last page is a routine handover to the next watch, and it is signed by four " +
        "people who were already in vacuum when it was written.";

    /// <summary>What the dead bridge panel says when the captain tries it first — a signpost, not a wall.
    /// Nobody should have to guess that the answer is aft.</summary>
    public const string DeadBridgePanelLine =
        "⚙ The bridge vent panel is dead — no bus, no pressure, forty years cold. Whatever she has left is " +
        "mechanical now: the valves themselves, aft in ENGINEERING.";
}
