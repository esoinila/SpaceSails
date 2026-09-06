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
///
/// <para>#251 · THIS FILE IS THE COMPARTMENT AND ITS ODDS — what a <see cref="Space"/> is, the dice the read
/// is rolled on, and the two enums the board answers in. The mechanic itself is seven siblings, each named
/// for the button it stands behind: <c>.Soak</c> (how long the vacuum needs), <c>.Doors</c> (a hatch held
/// shut by pressure), <c>.WholeShip</c> (the three ship-wide presses), <c>.Pump</c> (the thrifty way, and
/// the volume it turns out to be emptying), <c>.Spine</c> (the corridor and the shuttle's lock),
/// <c>.Refill</c> (putting the air back, too soon), and <c>.Vent</c> (the read, the blow, and the hull
/// somebody already blew). Nothing here declares a static FIELD — every value in this class is a
/// <c>const</c> — so the family is free of the initializer-order hazard
/// <c>NoPartialClassSpreadsItsStaticFieldsTests</c> guards.</para>
/// </summary>
public static partial class HullVenting
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
}
