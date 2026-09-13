namespace SpaceSails.Core;

/// <summary>
/// #409+ · DOORS THAT LOCK, ON THE GROUND. Owner: <i>"We had a lot of landing sites also. I think we could bring
/// some nice things we developed for the ships into some of the landing sites. We could have buildings with doors.
/// I am thinking about a secret lab that extends into a mountain at some reever infested site. Doors that lock is
/// a cool feature for doing a secret lab."</i>
///
/// <para><b>The ship lane has spent months building door discipline and none of it has ever been used on the
/// ground.</b> A dogged hatch is a wall; a shut door is opaque as well as solid (#465); the pack has never
/// operated a hatch and is not going to start (#488's crew-only rule). All of that is sitting there, and a
/// building on a moon has never had so much as a doorway.</para>
///
/// <para><b>What a LOCK adds that a shut door does not.</b> If shutting were enough, a lock would be decoration —
/// so it is not enough. An Old One cannot open a door, but it can <b>lean on one</b>, and forty years of nothing
/// has not made them polite: a shut door buys <see cref="ForceSeconds"/> and then it does not. A LOCKED door buys
/// the room. That is the whole value, and it is an escalation of the existing rule rather than a contradiction of
/// it — they still never operate the door, they simply out-last it.</para>
///
/// <para><b>#563 · AND THE PRICE IS TIME, NEVER A KEY.</b> Owner ruling, 2026-09-13: <i>"I like the time instead
/// of a key, considering we have firepower and tools. We can create the same effect as needing a key by making it
/// slow, too noisy, or dangerous in other ways."</i> A key would turn a door into an inventory hunt — walk the
/// level until the card falls out of a drawer — and this game has a tether, a tracker and a pack, which are three
/// better clocks than a scavenger hunt. So a locked door now costs one of three things and never a carried item:
/// <list type="number">
/// <item><b>SLOW.</b> <see cref="ForceSeconds"/> with your shoulder on it, and the tracker sweeping the whole
/// time. The constant did not move; what moved is that it is now a price the CAPTAIN can pay.</item>
/// <item><b>NOISY.</b> The hold is heard. Every tick of it puts a <c>ReeverHearing.Noise.Clatter</c> at the
/// door, so the tide inside twelve du turns and walks to the doorway you are leaning on. A shut leaf still
/// hides you from SIGHT (#1154/#1161) and has never hidden you from ears.</item>
/// <item><b>DANGEROUS.</b> <see cref="ShootThePlate"/> — one round, instantly, at full earshot, and the leaf
/// is <see cref="State.Destroyed"/> for the rest of the excursion. You spend the door's whole future for
/// present speed, which is the sentries' own law arriving at a door: <i>buys time, never safety</i>.</item>
/// </list></para>
///
/// <para><b>What went with the key.</b> Vantar's card no longer turns anything with hinges: it is a credential a
/// PANEL respects (<c>LabSecurity.Approach.HasKeyCard</c>) and nothing else. <c>MayLock</c>, <c>NoKeyLine</c> and
/// <c>LockedLine</c> were the card's three sentences and they are gone with it — the house still keys every door
/// at once (the lockdown), and the captain's answer to that is the shoulder or the round rather than a walk to a
/// chair two rooms deeper.</para>
/// </summary>
public static class LockedDoor
{
    /// <summary>What a door is doing.</summary>
    public enum State
    {
        /// <summary>Standing open. Anything walks through, and anything sees through.</summary>
        Open,

        /// <summary>Shut but not keyed. A wall to the eye and to the boot — and a wall the pack can lean on
        /// until it is not one.</summary>
        Shut,

        /// <summary>Shut and keyed. The pack can lean on it for as long as they like.</summary>
        Locked,

        /// <summary>
        /// #563 · SHOT OFF ITS FRAME, AND THAT IS TERMINAL. Owner, 2026-09-13: <i>"We have firepower and
        /// tools."</i> A round through the hasp opens the door in the time it takes to pull a trigger — and
        /// takes the door out of the world as a door. It is <see cref="Passable"/> and
        /// <see cref="Transparent"/> forever, it can never be shut, locked or forced again, and nothing on
        /// either side of it will ever be shut out again.
        ///
        /// <para>That is the danger, and it is deliberately not a punishment the game announces: the captain
        /// spends the door's ENTIRE FUTURE — every retreat behind it, every time the pack would have been
        /// stopped by it — to save <see cref="ForceSeconds"/> now. The sentries' own law, at a doorway:
        /// buys time, never safety.</para>
        /// </summary>
        Destroyed,
    }

    /// <summary>
    /// How long a pack needs to force a door that is merely SHUT. Long enough that dogging one is a real answer
    /// to being chased, short enough that it is a delay rather than a solution — which is exactly the job a shut
    /// door should have when a lock exists to do the other one. FLAGGED for the owner's tuning.
    /// </summary>
    public const double ForceSeconds = 25.0;

    /// <summary>How many of them it takes to lean on one at all. One Old One rebounds off a dogged door; the
    /// second one is what turns a delay into a countdown, which keeps a lone straggler from being a crisis.</summary>
    public const int ForcedByAtLeast = 2;

    /// <summary>Whether a door can be walked through. A destroyed one always is — there is nothing left of it
    /// to stop anybody.</summary>
    public static bool Passable(State state) => state is State.Open or State.Destroyed;

    /// <summary>Whether it can be seen through. Same answer, and deliberately the SAME rule the ship's hatches
    /// follow (#465): opacity and solidity are not the same property, but a door happens to have both — and a
    /// door that is not there any more has neither.</summary>
    public static bool Transparent(State state) => state is State.Open or State.Destroyed;

    /// <summary>Whether the pack can get through this one eventually. The single line the whole feature rests
    /// on: shut is a delay, locked is an answer. (A destroyed leaf is not forced; it is walked through.)</summary>
    public static bool CanBeForced(State state) => state == State.Shut;

    /// <summary>Whether a crowd this size is enough to work on it.</summary>
    public static bool EnoughToForce(int howMany) => howMany >= ForcedByAtLeast;

    // ── What the captain may do to it ─────────────────────────────────────────────────────────────────

    /// <summary>A captain may open a shut door with their hands. A KEYED one does not answer hands at all any
    /// more — it answers <see cref="MayForce"/> (time) or <see cref="MayShootTheLock"/> (a round), which is the
    /// whole of the 2026-09-13 ruling: <i>a locked door is TIME, never a key.</i></summary>
    public static bool MayOpen(State state) => state == State.Shut;

    /// <summary>…and may shut an open one, always. Never a destroyed one: that is what terminal means.</summary>
    public static bool MayShut(State state) => state == State.Open;

    /// <summary>
    /// #563 · THE SLOW ROAD. A keyed door can be taken by leaning on it for <see cref="ForceSeconds"/> —
    /// the constant this class has always carried, now a price the captain can pay as well as the pack.
    ///
    /// <para>A SHUT door is not on this list because it does not need to be: hands open it in no time at all
    /// (<see cref="MayOpen"/>), and offering a twenty-five second hold for something free would be the control
    /// inventing a cost the world does not charge.</para>
    /// </summary>
    public static bool MayForce(State state) => state == State.Locked;

    /// <summary>
    /// #563 · THE FAST ROAD, AND ITS PRICE. Owner: <i>"We have firepower and tools."</i> Where the captain is
    /// carrying the hand-load, a shut or keyed leaf can be taken with <see cref="RoundsToShootTheLock"/> round
    /// — instantly, at full earshot, and permanently.
    ///
    /// <para><b>Unarmed is not a refusal, it is the absence of a verb</b>: the plate is simply not on the row
    /// (#212 — an affordance you cannot read is one you do not have), so nothing on screen ever offers a trade
    /// the pocket cannot pay for.</para>
    /// </summary>
    public static bool MayShootTheLock(State state, bool armed) =>
        armed && state is State.Shut or State.Locked;

    /// <summary>
    /// What one round from the hand-load costs a lock. ONE — the captain is standing at arm's length with the
    /// hasp in front of them, which is not the sentry's lane (<c>ShootTheLock.RoundsPerHasp</c> prices a
    /// machine shooting a door across a corridor). Owner's brief, 2026-09-13: <i>one round from the
    /// hand-load</i>. FLAGGED for the owner's tuning.
    /// </summary>
    public const int RoundsToShootTheLock = 1;

    /// <summary>Fire on the lock. The leaf is gone — not open, GONE — and no state walks back out of
    /// <see cref="State.Destroyed"/> anywhere in this file. A door the round could not have been fired at
    /// (already open, already destroyed) does not move, so a caller that forgot to ask
    /// <see cref="MayShootTheLock"/> cannot spend a round on nothing.</summary>
    public static State Shoot(State state) =>
        state is State.Shut or State.Locked ? State.Destroyed : state;

    /// <summary>What pressing the door does next. Open ⇄ Shut, by hand, and nothing else: a keyed leaf does not
    /// move for a press (it wants time or a round) and a destroyed one has nothing left to press.
    ///
    /// <para>#563 · There is no longer a rung from Shut to Locked. That rung WAS the card, and with the card
    /// retired the captain cannot key a door at all — the house still can, all of them at once, and the answer
    /// to that is the shoulder or the round.</para></summary>
    public static State Next(State state) => state switch
    {
        State.Open => State.Shut,
        State.Shut => State.Open,
        _ => state,   // keyed or gone: a press does not move it
    };

    // ── What is said ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The prompt on the door itself, so a captain reads its state before touching it.</summary>
    public static string Label(State state) => state switch
    {
        State.Open => "🚪 OPEN — press to shut",
        State.Shut => "🚪 SHUT — press to open",
        State.Destroyed => "🕳 THE LOCK IS GONE — and the door with it",
        _ => "🔒 LOCKED",
    };

    /// <summary>Shutting one. Says what it buys AND what it does not, because a door that only advertised the
    /// good half would be the game lying to a captain about a decision.</summary>
    public const string ShutLine =
        "🚪 The door goes over and the bar drops. It will hold them — for a while. They do not work a handle; " +
        "they lean, and they have nothing else to do.";

    /// <summary>When the leaning starts. Deliberately a sound rather than a number: the clock is on the HUD, and
    /// the line is what makes a captain look at it.</summary>
    public static string BeingForcedLine(string what) =>
        $"🚪 Something heavy settles against {what}, and then something else does. The frame starts to talk.";

    /// <summary>And when it goes.</summary>
    public static string ForcedLine(string what) =>
        $"🚪 {what} comes off its track. Whatever a shut door was worth, you have spent it.";

    /// <summary>#563 · The plate for the fast road, on the door prompt row beside [E]. Fable-authored;
    /// implemented verbatim.</summary>
    public const string ShootThePlate = "F — SHOOT THE LOCK";

    /// <summary>#563 · Said once, when the lock is shot — the captain's own register, and the only thing the
    /// game ever says about what that cost. Fable-authored; implemented verbatim. Nothing here announces the
    /// noise: the ear is rung by the sim and the pack arriving IS the telling (#456, inference horror).</summary>
    public const string LockShotLine =
        "The lock is gone, and the door with it as a door. Nothing behind you closes now.";

    /// <summary>The thing a captain should be told once, the first time they are on the wrong side of one — the
    /// vent board's oldest lesson, moved to the ground.</summary>
    public const string WhichSideLine =
        "You will never be quite certain what you shut the door on. Now there is a second question: which side " +
        "of it you are.";
}
