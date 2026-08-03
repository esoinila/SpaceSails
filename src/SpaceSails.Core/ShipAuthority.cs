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
    /// <param name="standingOrder">Whether the captain has DELEGATED damage control to the station aft — the
    /// owner's own arrangement: <i>"Let's add separate option to captains desk to authorize the back of the
    /// ship repair station"</i>, and, pointedly, <i>"(even while the bridge still works)"</i>.
    ///
    /// <para>It is the same shape as <c>fire at will</c>, which the gun deck has had since the Expanse consult,
    /// and it exists for the same reason: a captain who has to be asked about every hatch during a fire is a
    /// captain who will be asked at the worst possible moment. Delegation is a decision the captain makes
    /// ONCE, in the calm, from the desk — and the log carries who was holding the authority when the valve
    /// opened.</para>
    ///
    /// <para>Note what it does NOT do: it never authorizes a compartment that is not selected, so the board
    /// still cannot act on its own. And it is a standing order, not a secret — see
    /// <see cref="StandingOrderStandsLine"/>, which the board says every time it acts under it.</para></param>
    /// <param name="nextActionCleared">Whether the captain has cleared the NEXT damage-control act from the
    /// desk, without naming a room. Owner: <i>"we have the authorize next shot, that kind of authorize damage
    /// control next vent action is missing, it would be usefull as limited pre-ok from captain."</i>
    ///
    /// <para>The middle rung, and the one the desk was missing. The named word is specific and given at the
    /// valve; the standing order is open-ended and given once; this is a captain saying <i>"you may do the next
    /// one"</i> from the bridge without knowing yet which compartment it will be — which is exactly the shape
    /// of a fire. It is SPENT by the act, like a cleared shot.</para></param>
    public static VentIntent EvaluateVent(
        string? selectedRoom, string? authorizedRoom,
        bool standingOrder = false, bool nextActionCleared = false) =>
        AuthorityFor(selectedRoom, authorizedRoom, standingOrder, nextActionCleared) switch
        {
            VentAuthority.None => selectedRoom is null ? VentIntent.NothingSelected : VentIntent.Opportunity,
            _ => VentIntent.Authorized,
        };

    /// <summary>Which of the captain's three authorities actually answers for this act — because the log has to
    /// record what let it happen, and because only some of them are SPENT by it.</summary>
    public enum VentAuthority
    {
        /// <summary>Nothing authorizes this. The board may offer; it may not take.</summary>
        None,

        /// <summary>The standing order to damage control. Never spent — that is what standing means.</summary>
        StandingOrder,

        /// <summary>The captain's word, naming this compartment. Spent by the act.</summary>
        NamedWord,

        /// <summary>The desk's pre-clearance for the next act, no room named. Spent by the act.</summary>
        NextActionCleared,
    }

    /// <summary>
    /// Which authority answers, in a deliberate order of preference.
    ///
    /// <para>The STANDING ORDER answers first because it costs nothing to use — a captain who has delegated
    /// should never silently burn a limited pre-clearance. Then the NAMED WORD, because it was given for this
    /// exact compartment and spending it is what "by name" means. Then the desk's pre-clearance, which is the
    /// general permission and therefore the last resort.</para>
    /// </summary>
    public static VentAuthority AuthorityFor(
        string? selectedRoom, string? authorizedRoom, bool standingOrder, bool nextActionCleared)
    {
        if (selectedRoom is null)
        {
            return VentAuthority.None;
        }
        if (standingOrder)
        {
            return VentAuthority.StandingOrder;
        }
        if (authorizedRoom is not null
            && string.Equals(authorizedRoom, selectedRoom, System.StringComparison.Ordinal))
        {
            return VentAuthority.NamedWord;
        }
        return nextActionCleared ? VentAuthority.NextActionCleared : VentAuthority.None;
    }

    /// <summary>Whether using this authority uses it UP. A standing order stands; the other two are spent by
    /// the act they permitted, which is the whole difference between delegating and clearing.</summary>
    public static bool IsSpentByTheAct(VentAuthority authority) =>
        authority is VentAuthority.NamedWord or VentAuthority.NextActionCleared;

    /// <summary>What the log records about who let this happen. A delegated or pre-cleared act must never be
    /// indistinguishable from one the captain stood there and ordered.</summary>
    public static string UnderAuthority(VentAuthority authority) => authority switch
    {
        VentAuthority.StandingOrder => "under the captain's standing order to damage control",
        VentAuthority.NamedWord => "on the captain's word, by name",
        VentAuthority.NextActionCleared => "under the captain's clearance for the next act",
        _ => "with no authority at all",
    };

    /// <summary>The desk's own line when the next act is cleared — no room named, because the captain does not
    /// know yet which one it will be. That is the point of it.</summary>
    public const string NextActionClearedLine =
        "✍ Damage control is cleared for its NEXT act — one compartment, whichever one it turns out to be. " +
        "Spent when they use it.";

    /// <summary>…and when the captain takes it back before it is used.</summary>
    public const string NextActionWithdrawnLine =
        "The clearance for damage control's next act is withdrawn.";

    // ── Delegation, and where the authority can be exercised ──────────────────────────────────────────

    /// <summary>What the captain's desk says when damage control has been handed the authority.</summary>
    public const string StandingOrderGivenLine =
        "⚓ Damage control has your authority. The station aft may dog, pump and open compartments without " +
        "asking — and the log records that you gave it, not that they took it.";

    /// <summary>…and when it is taken back.</summary>
    public const string StandingOrderWithdrawnLine =
        "Damage control's standing authority is withdrawn. Every compartment needs your word again, by name.";

    /// <summary>What the BOARD says while acting under the standing order, every time, so a delegated act
    /// never looks like an unauthorized one.</summary>
    public const string StandingOrderStandsLine =
        "Acting under the captain's standing order to damage control.";

    /// <summary>
    /// HER BRIDGE REPEATER, WHEN THERE IS NOTHING BEHIND IT. The derelict's whole layout argument
    /// (<see cref="HullVenting.DeadBridgePanelLine"/>) is that a dead bridge panel sends you aft to the
    /// valves; the owner's ruling is that HER ship works the same way round —
    /// <i>"So there needs to be standing authorization here when the bridge is alive. If bridge is not alive
    /// then it should be doable from the rear of the ship. Like in the reevers infested ship."</i>
    ///
    /// <para>On her the state is reachable rather than a forty-year-old fact: open her own bridge to space and
    /// the repeater on it stops answering, exactly as a derelict's does. What is left is mechanical, aft, in
    /// the machinery space — and any standing order given while the bridge still worked stands there.</para>
    /// </summary>
    public static string DeadRepeaterLine(string valveCompartment) =>
        $"⚙ The bridge repeater is dead — no bus behind it. Whatever she has left is mechanical now: the " +
        $"valves themselves, aft in {valveCompartment}.";

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
