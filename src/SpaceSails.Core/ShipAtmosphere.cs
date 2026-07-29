namespace SpaceSails.Core;

/// <summary>
/// HER AIR, AND WHAT IT COSTS TO THROW IT AWAY. Owner: <i>"our venting must have captains ok mechanism in
/// it, like firing a shot or looting"</i>, <i>"and there must be controls to do so on the bridge, but the
/// bridge position needs captains ok to vent"</i>, and — the everyday half — <i>"we might want to isolate
/// cabins to keep our disease etc."</i>
///
/// <para>The RULES are not here. They are <see cref="HullVenting"/>, the same ones a derelict's board obeys,
/// because the owner's whole argument for giving her this equipment was consistency: <i>"they are so cool
/// and it would be consistent in the universe."</i> A ship is a ship. What lives here is the handful of facts
/// that are true of HER and not of a forty-year-old hull:</para>
///
/// <list type="bullet">
/// <item>she has her own air plant, so the reserve is generous and finite rather than whatever the shuttle
/// carried aboard;</item>
/// <item>her compartments have people asleep in them, and the board knows which;</item>
/// <item>and her crew are pirates, which is a mechanic — see <see cref="TheSuitsBesideTheBunks"/>.</item>
/// </list>
/// </summary>
public static class ShipAtmosphere
{
    /// <summary>
    /// What her tanks hold, in compartment-fills. A working ship with a working air plant is not a wreck
    /// being kept alive by a shuttle's bottles — but it is not free either, and that is the whole point of
    /// having a number: venting a compartment because it was quicker than walking around it should be a
    /// decision the captain notices on the gauge afterwards.
    /// </summary>
    /// <remarks>FLAGGED for the owner's tuning. Eight is "you can be careless three times and thoughtful
    /// afterwards", which is the shape of most of this game's economies.</remarks>
    public const int ReserveFills = 8;

    /// <summary>
    /// THE PIRATE'S ANSWER TO HIS OWN QUESTION. Owner, the moment he was handed a valve that opens his own
    /// crew's cabins to space: <i>"I guess as a pirate one would have an emergency suit nearby and have a way
    /// to break the seal you sleep behind."</i>
    ///
    /// <para>So it is a fact about the crew rather than an item on a shelf, and it does the work of three
    /// design decisions at once. Venting your own berths is a THREAT rather than an execution — the handle
    /// you can pull, that they know you can pull, and everybody aboard knows it might not work. It makes the
    /// act about intent rather than outcome, which is the only thing a ledger can honestly record. And it
    /// gives the crew an answer that is not the game refusing: nothing on the board is disabled, and their
    /// counter is competence.</para>
    /// </summary>
    public const string TheSuitsBesideTheBunks =
        "Every berth aboard has a suit within arm's reach of the bunk and a crew that has drilled getting " +
        "into it. Opening a cabin to space is a thing you can do. It is not a thing you can be sure of.";

    /// <summary>Where the crew actually sleep — the compartments the manifest puts names in. Her cabins, and
    /// nothing else: the hold and the bay hold cargo and a shuttle, and the cantina is where they are awake.
    ///
    /// <para>This is the list that makes venting HER different from venting a derelict, and it is why the
    /// authority gate exists at all (<see cref="ShipAuthority.WhyItNeedsSaying"/>): a wreck's board cannot
    /// tell you what is in the room, and hers can.</para></summary>
    public static IReadOnlyList<string> Berths => ["CABIN 1", "CABIN 2", "CABIN 3"];

    /// <summary>Whether anybody sleeps in this compartment.</summary>
    public static bool IsBerth(string room) =>
        Berths.Contains(room, System.StringComparer.Ordinal);

    /// <summary>
    /// What the board says about a compartment before the captain commits — the difference between an
    /// engineering act and one with a person on the other side of it.
    /// </summary>
    public static string WhatIsInThere(string room) =>
        IsBerth(room)
            ? $"{room} is a berth. Somebody sleeps in there, the manifest says so, and the log will say you knew."
            : $"{room} holds no bunks. Opening it to space costs you air and nothing else.";

    /// <summary>
    /// ISOLATION IS FREE AND VENTING IS NOT, said once, where the difference lives.
    ///
    /// <para>Owner: <i>"we might want to isolate cabins to keep our disease etc."</i> — the ordinary, daily,
    /// reversible use of the whole system. Quarantine a berth, contain a fire, keep a leak in one compartment.
    /// It asks nobody, because a ship where dogging a hatch needed a ceremony would teach the crew to leave
    /// every hatch open.</para>
    ///
    /// <para>The gate is on the irreversible half only. <see cref="ShipAuthority.EvaluateVent"/> owns it.</para>
    /// </summary>
    public const string IsolationNeedsNobody =
        "Dogging a hatch is seamanship. Opening one to space is an order, and it goes in the log with your " +
        "name on it.";

    /// <summary>Her own reserve spent and left, for the gauge on the board.</summary>
    public static string ReserveLine(int fillsLeft) =>
        fillsLeft <= 0
            ? "🫁 RESERVE EMPTY — the plant will make more, given a watch and nothing else to do. Until then, " +
              "what you open stays open."
            : $"🫁 Reserve: {fillsLeft} of {ReserveFills} fills in her tanks.";

    /// <summary>What refilling a compartment off her own tanks says. The rule the wreck's board established
    /// and hers inherits: air comes back, nobody does.</summary>
    public static string RefilledLine(string room, int fillsLeft) =>
        $"🫁 {room} back to pressure off her own tanks. {fillsLeft} fill{(fillsLeft == 1 ? "" : "s")} left. " +
        "Whatever went out with the air is still out there.";

    /// <summary>And the refusal, when the tanks cannot pay for it.</summary>
    public static string NoReserveLine(string room) =>
        $"Nothing left in the tanks to put back into {room}. She makes air slowly; you spent it quickly.";

    /// <summary>The bridge repeater's own line. A derelict has this panel and cannot power it — the fact that
    /// hers answers is most of what owning a ship means, and the reason the owner wanted the controls in two
    /// places: <i>"there must be controls to do so on the bridge."</i>
    ///
    /// <para>It is the SAME board, not a lesser one. What it is not is a shortcut around the captain's word:
    /// the authority gate is on the act, so it applies identically at the helm and at the valves.</para></summary>
    public const string RepeaterLine =
        "⚙ Bridge repeater — her aft board, mirrored at the helm. Every switch answers from here; the " +
        "captain's word is still the captain's word.";
}
