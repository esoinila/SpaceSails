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
/// <para><b>And the cost is the good part.</b> The lab's doors are keyed, and the key is deeper in. So a captain
/// who locks the outer door to keep the pack off has locked their own way home until they go further in and find
/// the card — which is the vent board's oldest lesson, on the ground: <i>you will never be certain what you shut
/// the door on</i>, and now also <i>which side of it you are.</i></para>
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

    /// <summary>Whether a door can be walked through.</summary>
    public static bool Passable(State state) => state == State.Open;

    /// <summary>Whether it can be seen through. Same answer, and deliberately the SAME rule the ship's hatches
    /// follow (#465): opacity and solidity are not the same property, but a door happens to have both.</summary>
    public static bool Transparent(State state) => state == State.Open;

    /// <summary>Whether the pack can get through this one eventually. The single line the whole feature rests
    /// on: shut is a delay, locked is an answer.</summary>
    public static bool CanBeForced(State state) => state == State.Shut;

    /// <summary>Whether a crowd this size is enough to work on it.</summary>
    public static bool EnoughToForce(int howMany) => howMany >= ForcedByAtLeast;

    // ── What the captain may do to it ─────────────────────────────────────────────────────────────────

    /// <summary>A captain may always shut a door they are standing at, and always open one that is not keyed.
    /// Nothing about this needs a tool — it is a door.</summary>
    public static bool MayOpen(State state, bool hasKey) =>
        state == State.Shut || (state == State.Locked && hasKey);

    /// <summary>…and may shut an open one, always.</summary>
    public static bool MayShut(State state) => state == State.Open;

    /// <summary>
    /// THE LOCK NEEDS THE KEY, IN BOTH DIRECTIONS. A captain without the card cannot lock a door and cannot
    /// unlock one — which is what stops the lock being a free win. Find the card and you gain the ability to shut
    /// the pack out permanently AND the ability to shut yourself in permanently, in the same act.
    /// </summary>
    public static bool MayLock(State state, bool hasKey) => hasKey && state == State.Shut;

    /// <summary>What pressing the door does next, given what it is and what the captain is carrying.</summary>
    public static State Next(State state, bool hasKey) => state switch
    {
        State.Open => State.Shut,
        State.Shut => hasKey ? State.Locked : State.Open,
        _ => hasKey ? State.Shut : State.Locked,   // keyed and no card: it does not move
    };

    // ── What is said ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The prompt on the door itself, so a captain reads its state before touching it.</summary>
    public static string Label(State state, bool hasKey) => state switch
    {
        State.Open => "🚪 OPEN — press to shut",
        State.Shut => hasKey ? "🚪 SHUT — press to lock" : "🚪 SHUT — press to open",
        _ => hasKey ? "🔒 LOCKED — press to unlock" : "🔒 LOCKED",
    };

    /// <summary>Shutting one. Says what it buys AND what it does not, because a door that only advertised the
    /// good half would be the game lying to a captain about a decision.</summary>
    public const string ShutLine =
        "🚪 The door goes over and the bar drops. It will hold them — for a while. They do not work a handle; " +
        "they lean, and they have nothing else to do.";

    /// <summary>…and locking one.</summary>
    public const string LockedLine =
        "🔒 The card takes and the bolts go home. Nothing on the other side of that is getting through it. " +
        "Nothing on this side is getting back through it either, without the card in your hand.";

    /// <summary>A captain at a keyed door with nothing to open it with. States the fact and offers nothing —
    /// the way in is somewhere else, and finding it is the game.</summary>
    public const string NoKeyLine =
        "🔒 There is no handle on this side, and the reader wants something you are not carrying.";

    /// <summary>When the leaning starts. Deliberately a sound rather than a number: the clock is on the HUD, and
    /// the line is what makes a captain look at it.</summary>
    public static string BeingForcedLine(string what) =>
        $"🚪 Something heavy settles against {what}, and then something else does. The frame starts to talk.";

    /// <summary>And when it goes.</summary>
    public static string ForcedLine(string what) =>
        $"🚪 {what} comes off its track. Whatever a shut door was worth, you have spent it.";

    /// <summary>The thing a captain should be told once, the first time they lock one behind them — the
    /// vent board's oldest lesson, moved to the ground.</summary>
    public const string WhichSideLine =
        "You will never be quite certain what you shut the door on. Now there is a second question: which side " +
        "of it you are.";
}
