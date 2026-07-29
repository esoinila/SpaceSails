namespace SpaceSails.Core;

/// <summary>
/// VENTING YOUR OWN SHIP TAKES THE CAPTAIN'S WORD. Owner: <i>"venting a space would require captain's
/// consent … but add in the captain's ok requirement, like we have for shooting and looting."</i>
///
/// <para>So it is built on exactly that rule. <see cref="CaptureRule.EvaluateBoarding"/> exists because the
/// owner got robbed by accident — autopilot flew him through a moon, a depot slid into the boarding window
/// and the game committed a felony on his behalf. The lesson written into it was: PROXIMITY IS NEVER
/// CONSENT, and an authorization must name the same target it is spent on.</para>
///
/// <para>Every word of that applies here and the stakes are worse. A wreck's compartments hold forty-year-old
/// vacuum and something that wants you dead; YOUR compartments hold your crew, asleep in their berths. On a
/// derelict the handle is the whole mechanic and pulling it fast is the point. On your own ship the same
/// handle is a thing you should have to say out loud, name the room, and mean.</para>
///
/// <para>Note what this is NOT: a confirmation dialogue. A dialogue asks "are you sure?" about whatever is
/// under the cursor, and the answer arrives detached from the question. This carries the ROOM'S NAME — so
/// changing the selection silently revokes it, exactly the way re-aiming revokes a boarding authorization,
/// and the captain cannot arm one compartment and blow another.</para>
/// </summary>
public static class ShipAuthority
{
    /// <summary>Whether a compartment of the player's own ship may be blown this instant.</summary>
    public enum VentIntent
    {
        /// <summary>Nothing is selected — there is no question on the table.</summary>
        NothingSelected,

        /// <summary>A compartment is selected, and the captain has not said the word for THIS one. The board
        /// may OFFER it. It may never take it.</summary>
        Opportunity,

        /// <summary>The captain has authorized this exact compartment. The valve will answer.</summary>
        Authorized,
    }

    /// <summary>
    /// The gate. Only when the captain's authorization names the SAME compartment that is selected does the
    /// valve open — and any other combination is an <see cref="VentIntent.Opportunity"/> at best.
    ///
    /// <para>Deliberately identical in shape to the boarding gate, because it is the same promise: no amount
    /// of selecting, hovering, standing near or pressing quickly can ever return
    /// <see cref="VentIntent.Authorized"/> on its own.</para>
    /// </summary>
    public static VentIntent EvaluateVent(string? selectedRoom, string? authorizedRoom) =>
        selectedRoom is null ? VentIntent.NothingSelected
        : authorizedRoom is not null && string.Equals(authorizedRoom, selectedRoom, System.StringComparison.Ordinal)
            ? VentIntent.Authorized
            : VentIntent.Opportunity;

    /// <summary>What the board asks for, naming the room so the answer cannot drift onto another one.</summary>
    public static string AskFor(string room) =>
        $"⚠ {room} IS PART OF YOUR SHIP. Opening it to space needs the captain's word, by name, and the log " +
        "will carry it. Say it, or leave the valve alone.";

    /// <summary>What the board says once the word is given — still not the act, only the arming.</summary>
    public static string ArmedFor(string room) =>
        $"Captain's authority recorded: {room}. The valve will answer while that stands. Choose another " +
        "compartment and it lapses.";

    /// <summary>And when the selection moves, so the captain is told the authority went with it rather than
    /// discovering later that it did not.</summary>
    public static string LapsedFrom(string room) =>
        $"The authority for {room} lapses — it was given for that compartment and no other.";

    /// <summary>
    /// THE REASON THE CREW ARE DIFFERENT FROM THE INFESTATION, said once, where the rule lives.
    ///
    /// <para>The wreck's board is a weapon and its whole design is that the instrument cannot tell a survivor
    /// from the thing in the walls. Hers can: the crew are ABOARD, they are known, and the ship's own manifest
    /// says where they sleep. There is no dice roll to hide behind here, which is exactly why the act needs a
    /// name attached to it.</para>
    /// </summary>
    public const string WhyItNeedsSaying =
        "On a derelict the panel cannot tell you what is in the room. On your own ship it can, and that is " +
        "the difference: nobody aboard her is a question mark.";

    // ── Isolation, which is the everyday use and needs no authority at all ────────────────────────────

    /// <summary>
    /// SHUTTING A DOOR IS NOT VENTING A ROOM, and the board must never make them feel alike. Owner: <i>"we
    /// might want to isolate cabins to keep our disease etc."</i>
    ///
    /// <para>That is the ordinary, daily, reversible use of the whole system — quarantine a berth, contain a
    /// fire, keep a leak in one compartment — and it costs nothing and asks nobody. The authority gate exists
    /// solely for the irreversible half. A ship where dogging a hatch required a ceremony would teach the
    /// crew to leave every hatch open.</para>
    /// </summary>
    public static string IsolatedLine(string room) =>
        $"🔒 {room} isolated. Her air is her own now — nothing crosses that hatch in either direction until " +
        "you undog it.";

    public static string ReleasedLine(string room) =>
        $"🔓 {room} back on the ship's air.";
}
