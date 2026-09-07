namespace SpaceSails.Core;

/// <summary>
/// #488 · REFILL — the other half of a damage-control board, and the one that can bring something back
/// with the air.
///
/// <para>Owner, mid-playtest: <i>"Now I vacuumed the bridge, but there should be controls to re-fill it
/// also?"</i> — correct, and a board with only the destroying half on it is not a board.</para>
///
/// <para>Refilling too soon is the trap this half exists to set: the soak has a length
/// (<see cref="SoakRequired"/>) and the captain cannot see it, so a room brought back to pressure
/// early is a room where the thing is warm again and has learned the sound of that valve.</para>
/// </summary>
public static partial class HullVenting
{
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

    /// <summary>
    /// How many compartments one boarding can bring back to pressure.
    ///
    /// <para>WAS TWO, AND TWO WAS PRICING THE WRONG THING. Owner: <i>"let's add a little more of those
    /// reserves … we can't have any blow out to space fun without it. Slow vacuum pumping just does not
    /// deliver that drama feel of blasting to space :-D"</i> — and he is right about what the small number
    /// was actually doing. It was supposed to stop "blow everything and refill whatever squeals" from being
    /// the dominant strategy. What it did instead was make BLOWING A ROOM AT ALL feel like a mistake: two
    /// charges is not a budget, it is a warning light, so the correct play became to pump every compartment
    /// and never once use the loudest, best button on the board.</para>
    ///
    /// <para>Five is a budget. It is enough to blow a room because blowing it is the right answer — or
    /// because you want to see it happen — and still put her back. It is not enough to blow all eight, so
    /// the pump keeps its whole reason for existing: the patient road is how you afford to be extravagant
    /// somewhere else. The dominant-strategy worry is answered by the SOAK, which no amount of reserve buys
    /// you out of, rather than by rationing the drama. FLAGGED for the owner's tuning.</para>
    /// </summary>
    public const int RefillChargesPerBoarding = 5;

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
}
