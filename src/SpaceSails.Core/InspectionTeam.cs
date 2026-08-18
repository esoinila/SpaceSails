namespace SpaceSails.Core;

/// <summary>
/// #538 · SOMEBODY ELSE'S TEAM, SWEEPING A HULL YOU ARE STANDING IN. Owner: <i>"One of the salvages could have a
/// black ops inspection team go through them where we take our guns and hide to let them pass. One more berthed
/// shuttle should be set to close off and don't shoot mode during that. 😎"</i> and <i>"That place would be really
/// cut-throat as they want to keep their secrets, but the rewards could be big also… It might be sweet if they
/// fought off some reevers etc while the pirates hide. 😎"</i>
///
/// <para>They are here to make sure a wreck stops existing as evidence, and they are not looking for you — until
/// they find you. Which is what makes this the scene that gives <see cref="SilentRunning"/> a POINT: until now,
/// going dark and standing the guns down cost a captain three things and bought them nothing, because nothing
/// aboard a wreck cared whether the boat was lit.</para>
///
/// <para><b>What makes them different from the pack, mechanically.</b> A Reever is unaware, near-blind and
/// omnidirectional: it notices what is close. A professional is the opposite — they see FAR and only where the
/// lamp is pointed, they hear better than anything else aboard, and they work a route instead of standing still.
/// So the counter-play inverts too: you do not out-run them, you stand still somewhere the cone is not.</para>
///
/// <para><b>And being seen is not a fight you win.</b> That is the whole design constraint the owner set with
/// "cut-throat": a captain who is seen is dealt with. The one mercy is that professionals CHALLENGE before they
/// shoot — a beat of "stand still, hands out" that a fast captain can spend on breaking line of sight. That beat
/// is the difference between a stealth scene and a coin flip.</para>
/// </summary>
public static class InspectionTeam
{
    /// <summary>What a sweeper is doing. Four states, and the one that matters is
    /// <see cref="Awareness.Challenging"/>: the beat where a captain still has a choice.</summary>
    public enum Awareness
    {
        /// <summary>Working the route, lamp forward, not looking for anybody in particular.</summary>
        Sweeping,

        /// <summary>Heard something and is walking to the place it came from. They know the PLACE, never you —
        /// the same idiom the pack's ear already uses aboard a wreck.</summary>
        Investigating,

        /// <summary>Has a captain in the cone and is telling them to stand still. The clock a captain spends on
        /// getting a bulkhead between them.</summary>
        Challenging,

        /// <summary>The challenge went unanswered. They are no longer sweeping a hull; they are clearing it.</summary>
        Hunting,

        /// <summary>
        /// #731 v2 · THE SWEEP IS OVER AND THEY ARE GOING BACK TO THEIR OWN BOAT.
        ///
        /// <para>The state this scene has never had. Until now the team was spawned, walked its route
        /// forever, and stopped existing only because the NEXT boarding cleared the list — so a captain who
        /// hid well enough simply never found out what happened, and the answer to <i>did they leave</i> was
        /// that the question was never asked.</para>
        ///
        /// <para>It costs no nerve (see <see cref="NervePerSecond"/>): a professional walking to their own
        /// airlock is not looking for you, and the whole point of the beat is that they never found you.
        /// <b>Being in it is not being safe</b> — a leaver who catches a captain in their lamp still
        /// challenges, because the sighting branch is asked before the state is.</para>
        /// </summary>
        Leaving,
    }

    /// <summary>One sweeper. Deliberately thin: a position, a heading, a state and how long it has been in it —
    /// everything a pure rule needs and nothing the client's list is better at owning.</summary>
    public readonly record struct Member(
        string Callsign,
        double X,
        double Y,
        double Facing,
        Awareness State,
        double StateSeconds);

    // ── How they are built ────────────────────────────────────────────────────────────────────────────

    /// <summary>How many sweep a hull. Enough that a corridor can be covered from two ends, few enough that a
    /// captain can hold a mental model of where they all are — the thing being hidden from has to be legible.</summary>
    public const int TeamSize = 3;

    /// <summary>Their names. Numbers rather than names on purpose: nobody aboard is going to introduce
    /// themselves, and a captain listening on a stolen channel hears call-signs.</summary>
    public static IReadOnlyList<string> Callsigns { get; } = ["SWEEP-1", "SWEEP-2", "SWEEP-3"];

    /// <summary>
    /// How fast they work, deck units per second. SLOWER than the captain's 9 du/s on purpose — the fiction is a
    /// methodical sweep, and the mechanic is that a captain can always out-walk them and still lose, because the
    /// thing they are bad at is not speed.
    /// </summary>
    public const double SweepSpeed = 3.5;

    /// <summary>…and how fast they move once they have decided you are aboard. Faster than sweeping, still slower
    /// than a sprint: they do not need to catch you, they need to corner you.</summary>
    public const double HuntSpeed = 6.0;

    // ── What they can perceive ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The lamp: how far they see. Far — much farther than a Reever's <c>DormantSightRange</c> of 9 du — because a
    /// professional is carrying a light and looking down a corridor with it, and the corridor is the whole point.
    /// </summary>
    public const double LampRange = 20.0;

    /// <summary>
    /// …and how WIDE. Half-angle in degrees, so 35 is a 70° cone. This narrowness is the counter-play: their
    /// strength is reach and their weakness is that reach only exists where the lamp is pointed. Standing still
    /// three metres to the side of a sweeper is safer than running twenty metres down their axis.
    /// </summary>
    public const double LampConeHalfAngleDegrees = 35.0;

    /// <summary>
    /// What they hear. Better than the pack — comms, trained ears, and a reason to be listening — and it does NOT
    /// care where the lamp is pointed, which is why noise discipline is the real verb of the scene rather than
    /// crouching in the right corner.
    /// </summary>
    public const double EarshotDeckUnits = 34.0;

    /// <summary>Whether this sweeper can see a point right now: inside the lamp's reach, inside its cone, and with
    /// nothing solid in between. Walls are law for them exactly as they are for everyone else (#324).</summary>
    public static bool Sees(in Member member, double x, double y,
                            IReadOnlyList<SurfaceCollision.Segment>? walls)
    {
        double dx = x - member.X, dy = y - member.Y;
        double range = System.Math.Sqrt((dx * dx) + (dy * dy));

        if (range > LampRange)
        {
            return false;
        }

        if (range > 0.001 && OffAxisDegrees(member.Facing, dx, dy) > LampConeHalfAngleDegrees)
        {
            return false;   // in reach, out of the cone — the whole counter-play in one branch
        }

        return SurfaceCollision.HasLineOfSight(member.X, member.Y, x, y, walls);
    }

    /// <summary>Whether a noise made at a point reaches this sweeper. Range only: sound goes round corners, and a
    /// rule that let a bulkhead swallow a pump would make the quiet approach pointless.</summary>
    public static bool Hears(in Member member, double x, double y)
    {
        double dx = x - member.X, dy = y - member.Y;
        return (dx * dx) + (dy * dy) <= EarshotDeckUnits * EarshotDeckUnits;
    }

    /// <summary>How far off a sweeper's heading a direction lies, in degrees. Public because the renderer wants
    /// the same number to draw the cone with — a cone that is drawn wider than it is checked is a lie the player
    /// will learn the hard way.</summary>
    public static double OffAxisDegrees(double facing, double dx, double dy)
    {
        double to = System.Math.Atan2(dy, dx);
        double delta = System.Math.Abs(NormalisePi(to - facing));
        return delta * 180.0 / System.Math.PI;
    }

    private static double NormalisePi(double radians)
    {
        while (radians > System.Math.PI) { radians -= 2 * System.Math.PI; }
        while (radians < -System.Math.PI) { radians += 2 * System.Math.PI; }
        return radians;
    }

    // ── The beat that makes it a stealth scene ────────────────────────────────────────────────────────

    /// <summary>
    /// How long a captain has between being seen and being shot.
    ///
    /// <para>THE MOST IMPORTANT NUMBER IN THIS FILE. Without it, "being seen" and "being dead" are the same
    /// event and hiding becomes a coin flip nobody can play well. With it, a professional does the professional
    /// thing — orders you to stand still — and a captain gets one honest chance to put a bulkhead between them.
    /// At <see cref="SweepSpeed"/> they close 10.5 du in this window while a captain covers 27, so the escape is
    /// real and it costs the whole of the beat. FLAGGED for the owner's tuning.</para>
    /// </summary>
    public const double ChallengeSeconds = 3.0;

    /// <summary>
    /// #731 v2 · HOW MANY TIMES ROUND THE HULL BEFORE THEY GO.
    ///
    /// <para>Once. They came to look at a ship; when they have looked at all of it, they leave — and the
    /// route already ends at their own lock, because <c>SpawnSweepTeam</c> laid it that way from the first
    /// build (<i>"they work the ship the way anybody would who has done it before — from the far end back to
    /// their own boat"</i>). So the departure is not a new behaviour bolted on; it is the route finally being
    /// allowed to finish.</para>
    ///
    /// <para>A second lap would be a team that does not trust its own sweep, which is a different scene and
    /// a much less frightening one: the dread in this beat is that they were thorough, they were unhurried,
    /// and they went home without ever knowing you were aboard.</para>
    /// </summary>
    public const int LapsBeforeTheyGo = 1;

    /// <summary>#731 v2 · How far apart they stand when they queue for the lock, in deck units.
    ///
    /// <para>Owner's word for the exit is <b>single file</b>, and this is what makes it one: two body-widths
    /// (<c>DeckPlan.AvatarRadius</c> is 0.7, so a body is 1.4 across) plus a little, which is close enough
    /// that three figures on the spine read as a QUEUE rather than as three people who happen to be walking
    /// the same way, and far enough that nobody is standing in anybody's back. Below a body-width they would
    /// be drawn inside one another; much above it and the file reads as a crowd drifting forward.</para>
    /// </summary>
    public const double FileSpacingDu = 3.0;

    /// <summary>#731 v2 · How long one body takes to work the lock and be gone, in seconds.
    ///
    /// <para>Unhurried, which is the owner's other word for this beat. A hatch that swallowed three people in
    /// one frame would be a despawn wearing a door; this is long enough that the captain watches them go one
    /// at a time and short enough that the way home is not held for a whole watch. FLAGGED for the owner's
    /// tuning.</para>
    /// </summary>
    public const double ThroughTheLockSeconds = 3.0;

    /// <summary>How long they search the place a noise came from before giving up and rejoining the route. Long
    /// enough that making a racket and walking away does not simply work, short enough that one mistake is not the
    /// end of the boarding.</summary>
    public const double SearchSeconds = 12.0;

    /// <summary>Once they are hunting, they stay hunting for this long after losing sight — a professional does not
    /// shrug and go back to the checklist. It is the cost of having been seen at all, paid after the escape.</summary>
    public const double HuntPersistenceSeconds = 45.0;

    /// <summary>What a state change costs the captain's nerve per second, the #480 seam. Being challenged is the
    /// worst thing that happens in this scene short of being shot, and it should read that way.</summary>
    public static double NervePerSecond(Awareness state) => state switch
    {
        Awareness.Challenging => 4.0,
        Awareness.Hunting => 2.5,
        Awareness.Investigating => 0.8,
        _ => 0.0,
    };

    /// <summary>
    /// Whether the challenge has run out. Separated from everything else because this is the only branch in the
    /// scene that kills a captain, and it should be one line that a test can point at.
    /// </summary>
    public static bool ChallengeExpired(in Member member) =>
        member.State == Awareness.Challenging && member.StateSeconds >= ChallengeSeconds;

    // ── The three-body rule ───────────────────────────────────────────────────────────────────────────

    /// <summary>Who a sweeper deals with when both a captain and the pack are in front of them.</summary>
    public enum Priority
    {
        /// <summary>Nothing to deal with.</summary>
        Neither,

        /// <summary>The captain — a person, in a place they should not be.</summary>
        TheCaptain,

        /// <summary>The pack. Owner: <i>"It might be sweet if they fought off some reevers etc while the pirates
        /// hide."</i></summary>
        ThePack,
    }

    /// <summary>
    /// THE PACK WINS THE ARGUMENT, and it is not a courtesy to the player. A Reever is charging them at a range
    /// where a rifle is barely enough; a captain is a compliance problem twenty metres down a corridor. Any team
    /// that engaged the compliance problem first would be a team that died aboard.
    ///
    /// <para>Which hands the scene its best moment for free: a captain can stand still and watch two things that
    /// both want them dead spend themselves on each other. Helping either one is a decision with no obviously
    /// correct answer, which is the kind this game likes.</para>
    /// </summary>
    public static Priority WhoTheyDealWith(bool captainInSight, bool packInSight, double packRange) =>
        packInSight && packRange <= LampRange ? Priority.ThePack
        : captainInSight ? Priority.TheCaptain
        : Priority.Neither;

    // ── What is said ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The pre-knowledge, so choosing this hull is a decision rather than an ambush. Owner: <i>"Some kind of
    /// pre-knowledge that this one is dangerous."</i> Deniable on its own, in the #533 discipline: it states four
    /// facts and draws no conclusion.
    /// </summary>
    public const string PreKnowledgeLine =
        "🕶 She has been on the lists for eleven years and nobody has stripped her. Her transponder still answers, " +
        "with a code that is not a merchant's. There is a service advisory on her coordinates that does not say " +
        "what the service is. Whatever else is true, somebody is coming back for her.";

    /// <summary>The moment their boat mates on — the scene starting, and the last cheap moment to be somewhere
    /// else.</summary>
    public const string TheyArriveLine =
        "🕶 Something clamps on aft, and it does it well: one push, one mate, no second try. Whoever that is has " +
        "done it before, and they did not hail.";

    /// <summary>Sweeping past, heard rather than seen. Said sparingly — the point of the scene is the waiting.</summary>
    public static string SweepingLine(string callsign) =>
        $"🕶 {callsign} works the next compartment: a lamp across a doorway, a hatch tried and left as it was found.";

    /// <summary>They heard you. A place, never a target.</summary>
    public static string InvestigatingLine(string callsign) =>
        $"🕶 {callsign} stops. Then comes toward where the noise was, unhurried, which is worse than hurried.";

    /// <summary>THE BEAT. Three seconds of it, and the captain chooses what to do with them.</summary>
    public static string ChallengeLine(string callsign) =>
        $"🕶 The lamp finds you and stops. {callsign}, flat and bored: stand still, hands where I can see them. " +
        "Nobody is shouting. That is the part that should frighten you.";

    /// <summary>The captain spent the beat well.</summary>
    public const string BrokeContactLine =
        "🕶 You get a bulkhead between you before the lamp comes back round. They know somebody is aboard now, and " +
        "they will not stop knowing it.";

    /// <summary>…or spent it badly.</summary>
    public const string ChallengeUnansweredLine =
        "🕶 The lamp does not move and neither do they. This was never going to be a fight you win.";

    /// <summary>The owner's favourite outcome, and the best thing in the scene.</summary>
    public static string ThreeBodyLine(string callsign) =>
        $"🕶 {callsign} turns away from you mid-sentence — because something came round the corner behind them " +
        "that has no hands up at all. Two things that both want you dead, spending themselves on each other. You " +
        "were not offered this and you are not required to help either one.";

    /// <summary>And the discipline the whole scene rests on, said once when they arrive, because a captain who has
    /// not turned their own automation off has already lost and deserves to be told why.</summary>
    public const string YourGunsWillGiveYouAwayLine =
        "🤖 Your bots do not know the difference between a Reever and a professional, and the boat's tube gun will " +
        "fire on the first person through that hatch. Nothing you are hiding behind matters while your own kit is " +
        "still making decisions.";
}
