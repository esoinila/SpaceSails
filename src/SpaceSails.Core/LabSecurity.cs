namespace SpaceSails.Core;

/// <summary>
/// #409+ · THE ALARM, AND HACKING IT. Owner, sketching the mountain lab in four messages: <i>"We could have
/// buildings with doors"</i>, <i>"a secret lab that extends into a mountain at some reever infested site"</i>,
/// <i>"Doors that lock is a cool feature for doing a secret lab"</i>, <i>"Surely some control panels based on the
/// vent panel can be added 🤠"</i>, and the one that ties them together — <i>"Alarm system panel maybe … something
/// to try to hack."</i>
///
/// <para><b>The alarm is what stops the locks being a gift.</b> On their own, doors that lock are a tool that only
/// ever helps: shut the pack out, walk on. An alarm turns the same mechanism against the captain — because
/// LOCKDOWN locks every door in the place, including the ones behind you, and the deepest chamber is the one with
/// the card in it. His cool feature becomes the thing that traps you, using no new machinery at all.</para>
///
/// <para><b>Why it is a hack rather than a switch.</b> This game already has a dice engine whose house law is that
/// the die is SHOWN (<see cref="DiceRule"/>): a seeded roll, a stack of named modifiers, and a number the player
/// can argue with. A hack is the natural shape for that — every modifier is a thing the captain did or did not do
/// earlier, so the roll is a report on their whole approach rather than a coin flip at a panel.</para>
///
/// <para><b>And forty years of emergency power is the fiction that makes it fair.</b> The system is old, slow and
/// half-blind — which is why a captain gets a countdown rather than an instant lockdown, and why the panel can be
/// argued with at all.</para>
/// </summary>
public static class LabSecurity
{
    /// <summary>What the house is doing about you.</summary>
    public enum State
    {
        /// <summary>Forty years of nobody. It has not noticed anything yet.</summary>
        Dormant,

        /// <summary>It has noticed. A countdown is running and it can still be argued with.</summary>
        Armed,

        /// <summary>Argued with successfully. It will not notice again — this trip.</summary>
        Disarmed,

        /// <summary>LOCKDOWN. Every door in the place is keyed, and the way out is one of them.</summary>
        LockedDown,
    }

    /// <summary>
    /// How long the countdown runs once the alarm arms. Long enough to walk to the panel from anywhere in the
    /// lab and think about it; short enough that "finish reading first" is a gamble rather than a plan.
    /// FLAGGED for the owner's tuning.
    /// </summary>
    public const double ArmedSeconds = 75.0;

    /// <summary>What arms it. The first door FORCED (not opened — forced) is the trip: a hidden door taken off
    /// its frame is exactly what a security system forty years asleep is still listening for.</summary>
    public const string ArmedByLine =
        "🔔 Something under the floor wakes up and starts counting. It has been waiting forty years to do that, " +
        "and it is in no hurry at all.";

    /// <summary>The panel itself, in the middle chamber — the same idiom as the vent board: a thing bolted to a
    /// wall that tells you the truth about a system you are inside.</summary>
    public const string PanelTitle = "🔔 SECURITY — VANTAR LABS";

    // ── The roll ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What a hack needs on a d20 before modifiers. Deliberately steep: a bare-handed captain who
    /// walked straight to the panel should usually fail, and everything below is a way to earn it down.</summary>
    public const int HackTarget = 14;

    /// <summary>
    /// What the captain brings to it. Every one of these is something they DID earlier, which is the point: the
    /// roll reports on the approach rather than on the moment.
    /// </summary>
    /// <param name="HasKeyCard">Vantar's card, from the heart. It is a credential, and the panel knows it.</param>
    /// <param name="LogsRead">How many of his logs have been read — his own passwords are in his own writing.</param>
    /// <param name="AlarmAlreadyRunning">Working with the countdown going is worse than working before it.</param>
    /// <param name="NerveBand">The #480 nerve band, 0 (gone) … 4 (steady). Shaking hands lose the timing.</param>
    /// <param name="CarryingASentry">A bot's brain will talk to a lab's brain, given a cable.</param>
    public readonly record struct Approach(
        bool HasKeyCard,
        int LogsRead,
        bool AlarmAlreadyRunning,
        int NerveBand,
        bool CarryingASentry);

    /// <summary>
    /// The named modifier stack, in the house idiom — every line is shown to the player beside the die, because a
    /// roll a captain cannot argue with is a roll they did not take part in.
    /// </summary>
    public static IReadOnlyList<DiceModifier> Modifiers(in Approach approach)
    {
        var mods = new List<DiceModifier>();

        if (approach.HasKeyCard)
        {
            mods.Add(new("Vantar's card in the reader", 4));
        }

        if (approach.LogsRead > 0)
        {
            // His passwords are in his own logs, in his own hand. Capped so reading everything is not a skeleton
            // key — the last two logs teach you nothing the first two did not.
            mods.Add(new($"his own logs, {System.Math.Min(approach.LogsRead, 2)} of them", System.Math.Min(approach.LogsRead, 2)));
        }

        if (approach.CarryingASentry)
        {
            mods.Add(new("a sentry's brain on the end of a cable", 2));
        }

        if (approach.AlarmAlreadyRunning)
        {
            mods.Add(new("working while it counts", -3));
        }

        if (approach.NerveBand <= 1)
        {
            mods.Add(new("hands not steady", -2));
        }

        return mods;
    }

    /// <summary>Roll it. Seeded and pure; the client shows the die and the stack.</summary>
    public static DiceRoll Attempt(ulong seed, in Approach approach) =>
        DiceRule.Roll(seed, DiceRule.D20, Modifiers(approach));

    /// <summary>Whether that roll beat the panel.</summary>
    public static bool Beat(in DiceRoll roll) => roll.Total >= HackTarget;

    /// <summary>
    /// WHAT A FAILED HACK COSTS, and it must cost something or the panel is a slot machine you pull until it
    /// pays. A failed attempt does not merely fail: it takes the countdown down by
    /// <see cref="FailureCostsSeconds"/>, so the second try is worse than the first and the third may not exist.
    /// </summary>
    public const double FailureCostsSeconds = 25.0;

    /// <summary>…and the countdown left after a given number of failures. Pure so the panel and the HUD clock
    /// cannot disagree about how much rope is left.</summary>
    public static double SecondsLeftAfter(double elapsed, int failures) =>
        System.Math.Max(0, ArmedSeconds - elapsed - (failures * FailureCostsSeconds));

    // ── What is said ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The panel, before anybody touches it. States the target and the stack, because the whole point
    /// of showing the die is that the captain may decide NOT to roll it.</summary>
    public static string OfferLine(int target, int stack) =>
        $"🔔 The panel wants {target} or better on the die, and you are carrying {(stack >= 0 ? "+" : "")}{stack}. " +
        "A wrong answer costs you time you do not have much of.";

    /// <summary>Beat it.</summary>
    public const string HackedLine =
        "🔔 The counting stops. Somewhere behind the wall a relay lets go, and the lab goes back to being a room " +
        "with the lights off.";

    /// <summary>Missed it, with rope left.</summary>
    public static string FailedLine(double secondsLeft) =>
        $"🔔 Wrong. The panel does not argue — it simply takes {FailureCostsSeconds:0} seconds off you, and there " +
        $"are {HullVenting.SoakLabel(secondsLeft)} of them left.";

    /// <summary>And the thing the whole feature exists to make possible.</summary>
    public const string LockdownLine =
        "🔒 LOCKDOWN. Every door in the mountain goes over at once — the one ahead of you, the one behind you, " +
        "and the one you came in through. Vantar built this to keep something in.";

    /// <summary>The way out of a lockdown, and it is the card. Which is why the card is in the DEEPEST room: a
    /// captain who ran at the first alarm never had it, and now needs it, and it is behind two locked doors.</summary>
    public const string LockdownWithTheCardLine =
        "🔒 Every door is keyed — and you are holding the thing that keys them. Whatever Vantar built this to " +
        "keep in, you can walk past it.";

    /// <summary>…and without it.</summary>
    public const string LockdownWithoutTheCardLine =
        "🔒 Every door is keyed and there is no handle on this side of any of them. The card is where you did " +
        "not go.";
}
