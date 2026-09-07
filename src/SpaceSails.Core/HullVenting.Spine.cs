namespace SpaceSails.Core;

/// <summary>
/// #488 · THE CORRIDOR ITSELF, AND THE SHUTTLE'S OWN LOCK — the two volumes that are not compartments.
///
/// <para>The spine is a bigger volume than any room off it: it pumps 1.8× slower and yields two charges
/// instead of one, and it cannot be pumped at all while a hatch off it is standing open. The shuttle's
/// lock is the other end of the same fact — the way back aboard is through an airlock, and an airlock
/// with vacuum on the inboard side is a door you may not open.</para>
/// </summary>
public static partial class HullVenting
{
    // ── Pumping the corridor itself ───────────────────────────────────────────────────────────────────

    /// <summary>The spine, as something the board can act on. Owner: <i>"I would like to pump the spine
    /// also"</i> — and it is the natural end of his own play, <i>"run to the control room and lock all doors
    /// and pump them down"</i>. Dog every hatch, empty every room, and then take the corridor.</summary>
    public const string SpineName = "THE SPINE";

    /// <summary>The corridor runs the length of the ship, so it takes proportionally longer than a room.
    /// This is the same asymmetry that makes <see cref="SpineRefillRefusal"/> true: pumping a volume that
    /// large out is merely slow, and putting it back is impossible.</summary>
    public const double SpinePumpMultiplier = 1.8;

    /// <summary>And it is worth more when it lands, because there was more of it.</summary>
    public const int SpinePumpYieldsCharges = 2;

    /// <summary>Why the corridor cannot go on the pumps yet. It needs every compartment shut or already
    /// empty — otherwise the pump is not draining a corridor, it is draining the whole ship through eight
    /// open doorways, and it would never finish.</summary>
    public const string SpineNotSealedLine =
        "The pump will not hold a vacuum on the corridor while compartments are still open to it — you " +
        "would be pulling on the whole ship through eight doorways. Dog the hatches first, or empty the " +
        "rooms, and then take the spine.";

    /// <summary>Already done.</summary>
    public const string SpineAlreadyEmptyLine = "The corridor is already open to space. There is nothing in it to pump.";

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
}
