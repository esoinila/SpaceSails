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
    public readonly record struct Space(
        string Name, bool DoorShut, bool Vented, bool Infested, bool HoldsSurvivor, bool CaptainInside = false);

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

        string line = space.HoldsSurvivor
            ? $"{space.Name} blows out in a single breath. Something goes with it that was beating on the " +
              "door from the inside — and you will not know for certain what it was."
            : space.Infested
                ? $"{space.Name} blows out in a single breath, and the nest goes with it."
                : $"{space.Name} blows out in a single breath. Nothing was in there but forty years of stale air.";

        return new VentOutcome(true, space.Infested, space.HoldsSurvivor, line);
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

    /// <summary>What the dead bridge panel says when the captain tries it first — a signpost, not a wall.
    /// Nobody should have to guess that the answer is aft.</summary>
    public const string DeadBridgePanelLine =
        "⚙ The bridge vent panel is dead — no bus, no pressure, forty years cold. Whatever she has left is " +
        "mechanical now: the valves themselves, aft in ENGINEERING.";
}
