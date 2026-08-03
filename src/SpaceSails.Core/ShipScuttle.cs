namespace SpaceSails.Core;

/// <summary>
/// HER OWN CHARGES. Owner: <i>"that ship also has the scuttling charges... let's have a captains approval
/// mechanic for that also on our ship. :-D ... it is the last defence against the Borg in Star Trek :-D"</i>
/// and then the part that makes it a MECHANIC rather than an ending: <i>"they only need to activate it for the
/// borg to back down enough."</i>
///
/// <para>That is the whole design in one sentence, and it is the same shape as two rules this game already
/// lives by. Venting your own berths is a THREAT rather than an execution, because the crew sleep beside their
/// suits (<see cref="ShipAtmosphere.TheSuitsBesideTheBunks"/>). A warning shot is the captain talking rather
/// than the captain shooting. Her scuttling charges are the largest member of that family: the value is in
/// ARMING them where somebody can see it, and a captain who has to actually let the clock run has already
/// lost the argument.</para>
///
/// <para>The timing is <see cref="Scuttle"/>'s, unchanged — one law for both ships, so a captain who has
/// overloaded a derelict's reactor knows exactly how long ninety seconds feels. What is different is who may
/// turn the keys.</para>
/// </summary>
public static class ShipScuttle
{
    /// <summary>
    /// THE ONE AUTHORITY THAT CANNOT BE DELEGATED, and the reason to say so in Core rather than in a button.
    ///
    /// <para>The captain may hand damage control a standing order over her compartments
    /// (<see cref="ShipAuthority.StandingOrderGivenLine"/>) — that is a working ship delegating work. The
    /// charges are not work. Star Trek's own arrangement is the argument: a self-destruct takes more than one
    /// voice precisely so that no single station, however trusted, can end the ship on its own initiative.</para>
    ///
    /// <para>So the standing order stops at the bulkhead: it authorizes venting, pumping and dogging, and it
    /// authorizes nothing about the charges. This mirrors the sentence already on the captain's desk —
    /// <i>guns cleared ≠ boarding authorized</i> — and for the same reason: two acts of wildly different weight
    /// must never share one word.</para>
    /// </summary>
    public const string NeverDelegable =
        "Damage control's standing order stops at the charges. Scuttling her is not work you delegate — it " +
        "takes the captain, by name, at the panel.";

    /// <summary>Whether the CAPTAIN's half is satisfied: only their own word, naming HER, and never a standing
    /// order. Deliberately takes the standing order as an argument and ignores it, so a future caller cannot
    /// "helpfully" pass it in and quietly widen the gate.</summary>
    public static bool MayArm(bool captainSaidItByName, bool damageControlStandingOrder) =>
        captainSaidItByName;

    // ── The second key ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SECOND VOICE, AND WHO HOLDS IT. Owner, working out the Star Trek arrangement for a ship that has no
    /// first officer: <i>"I guess we need the another opinion we just don't have a desk for that on the bridge
    /// yet."</i>
    ///
    /// <para>She does not need one. The second key belongs to the CREW, and this game already knows exactly
    /// where the crew stand with their captain — <see cref="CrewTemp.StandingOf"/>, the sheet on his own desk.
    /// A pirate crew is not a chain of command with a spare officer in it; it is a company of people who agreed
    /// to sail under someone, and ending the ship is the one decision that is unambiguously theirs too.</para>
    ///
    /// <para>Which makes the crew temperature LOAD-BEARING instead of decorative, and delivers the promise
    /// written into the character design (<c>docs/features/the-captains-character.md</c> §3): a monstrous
    /// captain with a devoted crew can turn both keys, and a decent captain nobody trusts finds that nobody
    /// moves. The board does not judge him. His crew do.</para>
    /// </summary>
    public enum SecondKey
    {
        /// <summary>They turn it with him. No comment, or none he has to hear.</summary>
        Turned,

        /// <summary>They turn it — and they have something to say about the fact that he asked.</summary>
        TurnedAndSaidSo,

        /// <summary>Nobody moves. The panel is waiting on a hand that is not coming.</summary>
        Refused,
    }

    /// <summary>Where the crew come down on it, from where they already stand with him. Cohesion is in that
    /// number, which is the right shape: a crew that has been through four bad things with this captain is
    /// slower to refuse him the fifth.</summary>
    public static SecondKey SecondVoice(CrewTemp.Standing standing) => standing switch
    {
        CrewTemp.Standing.Solid => SecondKey.Turned,
        CrewTemp.Standing.Grumbling => SecondKey.Turned,
        CrewTemp.Standing.Petition => SecondKey.TurnedAndSaidSo,
        _ => SecondKey.Refused,
    };

    /// <summary>What happens at the panel when he reaches for the second key. Never a rules explanation — the
    /// crew's own behaviour is the read-out, exactly as the character design requires.</summary>
    public static string SecondVoiceLine(SecondKey key) => key switch
    {
        SecondKey.Turned =>
            "The engineer's hand is already on the other key. They turn together, and nobody says anything at " +
            "all, because there is nothing to say that everyone aboard has not already decided.",

        SecondKey.TurnedAndSaidSo =>
            "The engineer looks at the key, then at you, then turns it — and holds your eye a beat too long " +
            "afterwards. Whatever the deputation came to say last watch, this is going in it.",

        _ =>
            "You put your hand on your key. Nobody puts theirs on the other one. The engineer steps back from " +
            "the panel and finds something to look at, and the silence goes on long enough to be an answer.",
    };

    /// <summary>And the reason a refusal is not a bug: the crew being able to say no is the mechanic, so the
    /// panel says where the answer came from and where to go and fix it.</summary>
    public const string RefusedPointerLine =
        "The charges need two hands. Read the crew's own sheet at your desk — 🌡 Crew — and see what they are " +
        "not saying to your face.";

    /// <summary>What the panel asks for. It names the SHIP rather than a compartment, because that is what is
    /// on the table — the same reason the vent gate names the room.</summary>
    public static string AskFor(string shipName) =>
        $"⚠ THE CHARGES ARE UNDER {shipName.ToUpperInvariant()}'S OWN KEEL. Arming them is the captain's word, " +
        "by name, at this panel — and nobody else's, however you have written your standing orders.";

    /// <summary>What it says once the captain has said it and the keys are ready to turn — still not the act.
    /// The ship's name is in it so the word cannot drift onto something smaller.</summary>
    public static string ArmedFor(string shipName) =>
        $"Captain's authority recorded against {shipName}. Turn the keys and she is on a clock.";

    // ── The deterrent, which is the point ─────────────────────────────────────────────────────────────

    /// <summary>
    /// WHY YOU ARM THEM. Owner: <i>"they only need to activate it for the borg to back down enough."</i>
    ///
    /// <para>An armed hull is a hull that is worth nothing to anybody. A boarder wants the cargo, a collector
    /// wants the debt, a hunter wants the bounty — and none of that survives the ship. So the countdown is a
    /// bargaining position, and the moment it starts, the arithmetic of everyone chasing her changes.</para>
    /// </summary>
    public static string DeterrentLine(string pursuerName) =>
        $"{pursuerName} reads the overload on her hull and breaks off. There is no prize in a ship that is " +
        "about to stop existing — and no arguing with a captain who has already turned the keys.";

    /// <summary>And the line for arming with nobody watching, which is the captain threatening an empty room.
    /// Said plainly, because a mechanic that only pays off with an audience should tell you when there is
    /// none.</summary>
    public const string NobodyWatchingLine =
        "The charges are live and nobody is out there to care. A threat is only a threat where somebody can " +
        "read it — and the clock does not know that.";

    /// <summary>Whether arming will actually deter anything, given how many pursuers can see her.</summary>
    public static bool AnybodyToDeter(int pursuersInReach) => pursuersInReach > 0;

    // ── What it costs, said before it is spent ────────────────────────────────────────────────────────

    /// <summary>
    /// THE HONEST PRICE, on the panel, before the keys. She is the game: the hold, the tank, the contacts, the
    /// hoard's maps and every mile flown are aboard her. The brain-backup means the CAPTAIN continues; nothing
    /// else does.
    /// </summary>
    public static string PriceLine(int cargoUnits, long cargoValue, int credits) =>
        $"Everything aboard goes with her: {cargoUnits} unit{(cargoUnits == 1 ? "" : "s")} in the hold " +
        $"(worth {cargoValue:N0} cr), {credits:N0} cr in the purse, and the ship herself. The backup will " +
        "wake somebody who remembers all of it. That is not the same as having it.";

    // ── What happens at zero, which is not the same question as "did she blow up" ──────────────────────

    /// <summary>
    /// HOW IT ENDS, and the owner's own objection answered. Waiting at a ferry terminal with the charges
    /// freshly built: <i>"Suppose the crew sets the countdown 90 seconds and evacuates with shuttle. 🤔 If you
    /// can not get away with ship, then propably not with shuttle either 🤔 … we need to think about that."</i>
    ///
    /// <para>The objection is right about SPEED and wrong about the point. Scuttling is not flight, it is
    /// DENIAL: a debt collector wants the hull as an asset, a heat-hunter wants the bounty on her, a boarder
    /// wants the hold — and all of that is aboard the ship. The moment she stops existing, chasing a lifeboat
    /// costs fuel and time for a payout of zero. The shuttle never has to outrun anybody; it has to stop being
    /// worth the trip.</para>
    ///
    /// <para>So the clock reaching zero asks one question — WAS THE CAPTAIN STILL ON HER — and the two answers
    /// are a death and a beginning, not a win and a loss.</para>
    /// </summary>
    public enum Ending
    {
        /// <summary>He was aboard. The charges kept their end of the bargain.</summary>
        WentWithHer,

        /// <summary>He was clear — in the shuttle, on a surface, ashore. No ship, no hold, and nobody left
        /// with a reason to chase him.</summary>
        Castaway,
    }

    /// <summary>The whole rule, and deliberately this small: everything else about the ending is consequence.</summary>
    public static Ending EndingFor(bool captainWasAboard) =>
        captainWasAboard ? Ending.WentWithHer : Ending.Castaway;

    /// <summary>What the card says to a captain who got clear. It is not a congratulation — he has just spent
    /// his ship to stop somebody else having her.</summary>
    public const string CastawayLine =
        "She goes at a distance, and the light of it arrives before the sound would if there were any. " +
        "Whatever was chasing her has nothing left to chase: the hull was the asset, the bounty and the prize, " +
        "and the captain is a man in a small cold boat who is worth the fuel to nobody.";

    /// <summary>And what survives — said plainly, because a player who has just lost a ship needs to know what
    /// he still has before he can decide whether it was worth it.</summary>
    public const string CastawaySurvivesLine =
        "What is still yours: the purse in your pocket, every mark you buried off-ship, and the people who " +
        "remember your name. What is not: her, and everything that was in her hold.";

    /// <summary>The rescue, which is the shuttle's real problem — air and range and being found, never speed.
    /// A lifeboat running cold is far harder to see than a charged hull, which is the one advantage it has.</summary>
    public static string RescuedAtLine(string havenName) =>
        $"A boat with a beacon and no cargo is somebody's paperwork rather than somebody's payday. {havenName} " +
        "sends out for you, and the berth they give you comes with a hull nobody else wanted.";

    /// <summary>Watching your own ship go is not free, whatever it bought. Nerve pips, through the #480 seam.</summary>
    public const int CastawayNerveCost = 30;

    /// <summary>The last line before the clock — the one the recall window exists for.</summary>
    public const string KeysTurnedLine =
        "Both keys turn together. The note of her comes up half a tone, the PA starts counting, and every " +
        "hatch you dogged is now a decision you made about your own way out.";
}
