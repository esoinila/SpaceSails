namespace SpaceSails.Core;

/// <summary>
/// #583 · THEY LAND. Heat, delivered to the person who earned it, wherever that person is standing.
///
/// <para>Owner, 2026-08-01, working the problem out loud across an excursion:</para>
/// <list type="bullet">
/// <item><i>"the heat must follow the captain, not the ship"</i></item>
/// <item><i>"zero heat against empty ship (we don't have any playing there, clearly there is some lights on
/// keeper on ship but we do not play that)"</i></item>
/// <item><i>"but the heat should not target the ship when the player is not on it but only target the
/// captain... we could have some other shuttle land near ours on some sites ... that would be the heat when
/// we are on land or at a ship looting it"</i></item>
/// <item>and the whole design in one line — <b><i>"FBI does not arrest cars ... they look for the
/// driver"</i></b></item>
/// </list>
///
/// <para>#580 cut the wrong half: a hunter could reach and CATCH a docked hull with nobody aboard, so a good
/// long walk came home to a boarding party and the game quietly became about guarding a parking lot. Wolves
/// now hold station while the captain is away. That fixed the falsehood and left a HOLE — heat stopped
/// meaning anything at all during the part of the game where the captain actually is.</para>
///
/// <para>This is the other half. The collectors do not impound your car; they come and find you. A boat sets
/// down on the same regolith you are walking, near enough to your own that it is between you and your ride,
/// and a repo team gets out. It is the SAME people wanting the SAME thing as in space, so a contact opens
/// the SAME demand — submit, bribe, or resist. What changes is that out here you cannot burn away from them,
/// only outwalk them, and the tube is the only door that closes.</para>
///
/// <para>Every decision is a pure function of (heat, seed): determinism is law in Core. The client owns the
/// team's positions the way it already owns the Reevers'.</para>
/// </summary>
public static class CollectorLanding
{
    /// <summary>Do they come at all? Cold captains are never followed down — a moon is not watched, and the
    /// only thing that puts a repo boat over one is a name already on a list (#582 will add the other door:
    /// robbing something whose owners cannot report it).
    ///
    /// <para>Never certain, even at the top. A landing that is guaranteed at heat 3 turns every hot excursion
    /// into the same scene; a landing that is LIKELY at heat 3 makes the walk down feel like a wager, which
    /// is what the heat gauge is supposed to be for.</para></summary>
    public static bool WillFollowYouDown(int heatLevel, ulong seed)
    {
        if (heatLevel <= 0)
        {
            return false;
        }

        // 1 in 4 at heat 1, climbing to near-certain by the time the gauge is buried.
        int faces = Math.Max(2, 5 - heatLevel);
        return DiceRule.Roll(DiceRule.Seed($"collector-landing:{seed}:{heatLevel}"), faces).Face == 1;
    }

    /// <summary>The earliest a boat can break atmosphere over you, in excursion seconds.
    ///
    /// <para>It must be MID-MISSION. Arriving at the door would just be a wall in front of the airlock, and
    /// the player would learn to check the gauge before landing and never see the scene again. Arriving when
    /// you are already deep, already spent, and already carrying something is the moment the choice is
    /// interesting: run for the tube past them, or go to ground in a shelter and wait them out.</para>
    ///
    /// <para>The same shape as the owner's own leap-of-faith framing from days earlier — <i>"it has to be mid
    /// mission (so that you don't just bring more air) and time sensitive"</i>.</para></summary>
    public static double ArrivesAfterSeconds(int heatLevel, ulong seed)
    {
        // Hotter captains are found faster: they were being watched for.
        double window = Math.Max(90.0, 300.0 - (heatLevel * 45.0));
        double jitter = DiceRule.Roll(DiceRule.Seed($"collector-eta:{seed}"), 60).Face;
        return window + jitter;
    }

    /// <summary>How many get out of the boat. A repo crew, not an army — the threat is that they are between
    /// you and the tube and they do not tire, never that they outnumber the regolith.</summary>
    public static int PartySize(int heatLevel) => Math.Clamp(1 + (heatLevel / 2), 1, 4);

    /// <summary>Deck-units per second on foot. Deliberately a shade under a captain's run and a shade over a
    /// captain's walk: you can open the gap by sprinting and you lose it the moment you stop to work.
    ///
    /// <para>Fixed pace, no cleverness — the same law the heat-hunters fly under (owner's standing ruling:
    /// they chase with fixed thrust and no orbital mechanics, and the counter is a warning shot, not a
    /// better trajectory). A repo team that pathfinds would be a different, worse game.</para></summary>
    public const double PaceDuPerSecond = 5.2;

    /// <summary>How close a collector has to be to put a hand on your shoulder. Body-to-body, the one contact
    /// law #469 settled for everything on this surface.</summary>
    public const double ReachDu = 1.4;

    /// <summary>Where the boat sets down: off your own shuttle by this much, on the tube side of you.
    ///
    /// <para>Not ON your shuttle — they are not here to steal it, and a boat parked on the hatch would end
    /// the excursion by geometry instead of by decision. Off to one side and BETWEEN you and the way home is
    /// the position that asks a question.</para></summary>
    public const double SetsDownOffsetDu = 34.0;

    /// <summary>Which side of the tube they put down on, so it is not always the same picture.</summary>
    public static double SetsDownX(double tubeX, ulong seed) =>
        DiceRule.Roll(DiceRule.Seed($"collector-side:{seed}"), 2).Face == 1
            ? tubeX - SetsDownOffsetDu
            : tubeX + SetsDownOffsetDu;

    /// <summary>The callsign painted on the boat. Repo outfits, not navies.</summary>
    public static string CallsignFor(ulong seed)
    {
        string[] names =
        [
            "LIEN & FORFEIT", "THE LONG ARM", "RECOVERY 6", "ARREARS", "DUE NOTICE",
            "SECOND DEMAND", "THE BAILIFF", "COLLATERAL 9",
        ];
        return names[(int)(DiceRule.Roll(DiceRule.Seed($"collector-name:{seed}"), names.Length).Face - 1)];
    }

    /// <summary>What the sky says when their boat comes down. Said once, and it is the whole warning the
    /// player gets — after this the only information is the tracker.</summary>
    public static string ArrivalLine(string callsign) =>
        $"🚨 A second boat comes down hard to the west of yours — no beacon, no hail on the port channel, " +
        $"landing lights off. {callsign}, painted small under the hatch. They did not come for the ship: " +
        "she is docked and empty and they flew straight past her. They came for you.";

    /// <summary>The hail that follows, so it is unmistakable who this is and what they want.</summary>
    public const string HailLine =
        "📻 Your suit picks up a narrowband hail, close and unbothered: \"Captain. We have a writ and we " +
        "have all day. You have a tank.\"";

    /// <summary>Said when the tracker first shows them moving. They walk. That is the point.</summary>
    public const string ClosingLine =
        "🚨 They are not hurrying. Four contacts on the fan, spread wide and walking — the spacing of people " +
        "who have done this before and expect the ground to do the work for them.";

    /// <summary>What it says when a hand closes on your arm out here.</summary>
    public static string ContactLine(string callsign) =>
        $"🚨 A gloved hand closes on your suit's carry loop and does not let go. {callsign} has you, on foot, " +
        "on somebody else's moon — and the writ they are reading from is the same one they would have read " +
        "on your own deck.";

    /// <summary>What it says when the tube seals behind you and they are still out there.</summary>
    public const string EscapedLine =
        "🚪 The tube reads your suit and cycles. Through the port they are still coming on, unhurried, and " +
        "one of them lifts a hand — not a wave. They know the ship. They will know the next port too.";

    /// <summary>Can a shelter hide you from them? No — and the line says so plainly, because a refuge that
    /// silently fails to be one is the worst thing a survival game can do.</summary>
    public const string ShelterIsNotSanctuaryLine =
        "⛺ The shelter's door cycles shut behind you and holds. It was built to keep vacuum out, and it will " +
        "— it was not built to keep a writ out. Through the port, they take up positions and settle in to " +
        "wait. Your air is the clock now, and they know it.";

    /// <summary>Is this contact close enough to be a catch?</summary>
    public static bool HasYou(double collectorX, double collectorY, double avatarX, double avatarY)
    {
        double dx = collectorX - avatarX, dy = collectorY - avatarY;
        return Math.Sqrt((dx * dx) + (dy * dy)) <= ReachDu;
    }

    /// <summary>One step of the walk toward the captain — the plain, wall-obeying pursuit the Reevers already
    /// use, at this pace. Kept as a named call so the collectors' movement law is written down in one place
    /// rather than borrowed silently.</summary>
    public static (double X, double Y) Step(
        double collectorX, double collectorY, double avatarX, double avatarY, double dtSeconds,
        IReadOnlyList<SurfaceCollision.Segment>? walls, double radius, int wallSide = 1) =>
        ReeverChase.Step(
            collectorX, collectorY, avatarX, avatarY, PaceDuPerSecond * Math.Max(0, dtSeconds),
            // They have their own boat and their own airlock: no crew-only barrier pens them to the regolith
            // the way it pens an Old One. What stops them is the tube door, and only because it is a door.
            barrierY: double.MaxValue, walls, radius, wallSide);
}
