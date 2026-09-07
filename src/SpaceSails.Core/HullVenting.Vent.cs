namespace SpaceSails.Core;

/// <summary>
/// #488 · THE READ AND THE BLOW — the hard decision itself, the hull that somebody already made it on,
/// and the placard that tells a stranger where the valves are.
///
/// <para>A life-sign read cannot tell a survivor from the infestation: both are warm, both move. That
/// is the whole design. Venting on a positive read is a coin you chose to flip; venting on a negative
/// read is a coin you chose not to look at. Both the read and the survivor behind the barricade are
/// seeded off the wreck, so a reload cannot re-roll either.</para>
///
/// <para>The other half of this file is the hull where the decision was already taken:
/// <see cref="Derelict.WreckCause.VentedByOneOfTheirOwn"/> arrives with every compartment but one
/// already hard vacuum, her bridge panel dead, and nothing left aboard but the mechanical valves aft —
/// which is what the placard at the lock is for.</para>
/// </summary>
public static partial class HullVenting
{
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
        "mechanical now: the valves themselves, aft in " + ValveCompartment + ".";

    // ── The placard at the lock ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// WHERE THE CONTROLS ARE, TOLD TO SOMEBODY WHO HAS NEVER BEEN ABOARD. Owner, thinking past his own
    /// tenth boarding: <i>"did we tell somewhere where to find the manual airlock controls? Just thinking
    /// about first time player of that ship, could they go directly to the right space."</i>
    ///
    /// <para>They could not. The only signpost was the dead bridge panel, and it only speaks if the captain
    /// walks to the BOW and presses it — so anyone who turned aft, or who never touched the bridge at all,
    /// was told nothing. The deck carried a label reading ATMOSPHERE VALVES, which means nothing to a player
    /// who does not yet know they want it.</para>
    ///
    /// <para>A ship answers this with a placard at the lock, and so does she. It names the compartment
    /// rather than describing it, because a first-timer needs a NOUN they can go and find — and it is built
    /// from <see cref="ValveCompartment"/> rather than typed out, so the sign cannot ever point at a room
    /// the board is not in.</para>
    /// </summary>
    public const string PlacardTitle = "🪧 DAMAGE-CONTROL PLACARD";

    public static string PlacardLine =>
        $"🪧 A steel plate bolted by the lock, painted forty years ago and still perfectly legible: a plan " +
        $"of the ship with her compartments numbered, her lifeboat cradles marked, and one box in heavier " +
        $"type. ATMOSPHERE CONTROL — {ValveCompartment}, AFT. Under it, in the same hand, the sentence " +
        "every ship writes and every crew stops reading: THE BRIDGE PANEL IS THE MASTER; THE VALVES ARE " +
        "THE LAST WORD.";

    /// <summary>What the placard tells you a second time, when you already know. Short, because by then it
    /// is a reminder rather than a briefing.</summary>
    public static string PlacardAgainLine =>
        $"🪧 The plan of the ship, and the box in heavier type: ATMOSPHERE CONTROL — {ValveCompartment}, AFT.";
}
