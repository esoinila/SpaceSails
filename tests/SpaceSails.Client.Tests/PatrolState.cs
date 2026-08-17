using System;
using System.Collections.Generic;
using System.Reflection;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6′b · WHERE THE PATROL'S STATE ACTUALLY IS.
///
/// <para>Three guards reach into a LIVE <see cref="Pages.Map"/> by reflection and ask it for
/// <c>_guards</c>, <c>_escort</c>, <c>_patrolBeat</c> and the rest —
/// <c>EveryRoundFingerprintsTheSameTests</c> (which pins a transcript of every frame of twelve rounds),
/// <c>EveryFrameLeavesTheSameFingerprintTests</c> (whose ledger reads the guards by name) and
/// <c>TwoGripsOneWalkTests</c> (which puts a guard on the captain's elbow). They are the strongest patrol
/// proof in the repository: they drive the real verbs and then look at the state, so a changed number
/// anywhere in the family shows up as a red hash rather than as a source file that still reads plausibly.
/// </para>
///
/// <para>6′b moved those twenty-two fields onto <c>Map.Patrol</c>, reached through the page's one
/// <c>_patrol</c> field. <b>The LOOKUP follows the state; not one assertion moved, and not one pinned
/// transcript line changed its wording.</b> That is the whole of this file, and it lives in one place
/// because three copies of "where the round is now" is precisely the law-transcribed-at-its-call-sites bug
/// this repository has paid for four times.</para>
///
/// <para>It is the seat's <see cref="SeatState"/> one building along, and it exists for the same reason
/// #909 does: a guard that names a component's fields <b>by string</b> is invisible to the compiler, so
/// two independently-green lanes can land a red tree. The way that does not happen is that the string is
/// written down ONCE.</para>
///
/// <para>It is deliberately NOT a fallback for any name: only the twenty-two are followed, and only when
/// the page really has a <c>_patrol</c> on it. A guard asking for a field that has simply been deleted must
/// still fail loudly at its own helper, which is what keeps the anti-vacuous property those guards rely on.
/// </para>
/// </summary>
public static class PatrolState
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags Either = Hidden | BindingFlags.Public;

    /// <summary>The twenty-two, and what each is called on the round now. Raw name in, property name out —
    /// in the order <c>ThePatrolKeepsItsOwnStateTests</c> lists them, which is the order they were declared
    /// in on the page.</summary>
    public static readonly IReadOnlyList<(string Raw, string On)> TheTwentyTwo =
    [
        ("_guards", "Guards"),
        ("_patrolReadables", "Readables"),
        ("_patrolBeat", "Beat"),
        ("_patrolFloorSeconds", "FloorSeconds"),
        ("_patrolHeardAgo", "HeardAgo"),
        ("_escort", "Escort"),
        ("_escortDue", "EscortDue"),
        ("_escortCar", "EscortCar"),
        ("_escortSeconds", "EscortSeconds"),
        ("_escortSaidPumps", "EscortSaidPumps"),
        ("_patrolWatch", "Watch"),
        ("_escortsThisWatch", "EscortsThisWatch"),
        ("_walkedAwayThisWatch", "WalkedAwayThisWatch"),
        ("_kickOutDue", "KickOutDue"),
        ("_kickOutRideDue", "KickOutRideDue"),
        ("_kickedOutPlateFor", "KickedOutPlateFor"),
        ("_paperInHand", "PaperInHand"),
        ("_walletFanOpen", "WalletFanOpen"),
        ("_shownBook", "ShownBook"),
        ("_walkedPastSaid", "WalkedPastSaid"),
        ("_patrolCheat", "RoundsCheat"),
        ("_badgeCheat", "BadgeCheat"),
    ];

    /// <summary>
    /// #870 lane 6′d · …AND THE GUARD'S OWN TWENTY-EIGHT, in the order they are declared on him.
    ///
    /// <para>6′d turned twenty-three of them from public mutable FIELDS into <c>{ get; private set; }</c>
    /// properties written only by named transitions. The five that stay settable are position and motion,
    /// which the stepper writes every frame; <c>DeckName</c> and <c>Plate</c> stay <c>init</c>.</para>
    ///
    /// <para>It is written down HERE, once, for the same reason the twenty-two above are: three harnesses
    /// read a guard's state <b>by string</b> (<c>EveryRoundFingerprintsTheSameTests</c> writes every one of
    /// them into its pinned transcript, <c>EveryFrameLeavesTheSameFingerprintTests</c> renders them into its
    /// ledger, <c>AStandingGuardIsStandingAtSomethingTests</c> reads the sign-in and the cover), and a name
    /// that quietly changed would leave all three green and about nothing. <c>ThePatrolKeepsItsOwnStateTests</c>
    /// asks the live type for every row and for the setter each one is supposed to have.</para>
    /// </summary>
    public static readonly IReadOnlyList<(string Name, bool Settable)> TheGuardsTwentyEight =
    [
        // The two the round is minted with, and never writes again.
        ("DeckName", false),
        ("Plate", false),

        // MOTION, not state: the stepper writes these every frame and 6′d deliberately left them open.
        ("X", true),
        ("Y", true),
        ("Facing", true),
        ("Vx", true),
        ("Vy", true),

        // …and the twenty-three a transition owns.
        ("Leg", false),
        ("Standing", false),
        ("SinceStop", false),
        ("Route", false),
        ("Planning", false),
        ("Retries", false),
        ("Seen", false),
        ("Held", false),
        ("WalkingUp", false),
        ("WalkUpFor", false),
        ("RePlanIn", false),
        ("AfterYou", false),
        ("Why", false),
        ("AfterYouFor", false),
        ("CallingIn", false),
        ("WallSide", false),
        ("SawYouShutIt", false),
        ("Knocking", false),
        ("Knocked", false),
        ("SignedPoint", false),
        ("CoverPoint", false),
        ("CoverAt", false),
        ("CoverFor", false),
    ];

    private static readonly Dictionary<string, string> Moved = Build();

    private static Dictionary<string, string> Build()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string raw, string on) in TheTwentyTwo)
        {
            map[raw] = on;
        }
        return map;
    }

    /// <summary>The round hanging off a live page, or null when this is not a page that has one.</summary>
    public static object? Round(object o) =>
        o.GetType().GetField("_patrol", Hidden)?.GetValue(o);

    /// <summary>
    /// #870 lane 6′c · …AND THE SAME FOR A VERB. 6′b moved the round's STATE; 6′c moved its VERBS, so a
    /// harness that drives the real code by name has to look in the same two places a read does — on the
    /// page first, because thirteen of them are still forwarded there under their old spellings, and then on
    /// the round hanging off it.
    ///
    /// <para>Page FIRST and never the other way round: a forwarder and the member it forwards to are the
    /// same call, and preferring the page is what keeps a harness pointed at exactly the entry point the
    /// shipped callers use.</para>
    ///
    /// <para>It is deliberately NOT a fallback that invents anything. A name that is on neither comes back
    /// null, and the caller's own assertion then fails loudly — which is what keeps the anti-vacuous
    /// property every one of these harnesses relies on.</para>
    /// </summary>
    public static (object Target, MethodInfo Call)? Verb(object page, string name)
    {
        if (page.GetType().GetMethod(name, Either) is { } onThePage)
        {
            return (page, onThePage);
        }

        if (Round(page) is { } round && round.GetType().GetMethod(name, Either) is { } onTheRound)
        {
            return (round, onTheRound);
        }

        return null;
    }

    /// <summary>Follow one of the twenty-two to where it now lives.</summary>
    /// <returns><c>true</c> only when <paramref name="name"/> is one of the twenty-two AND the page carries
    /// a round — in which case <paramref name="value"/> is the state a raw field read used to give. Anything
    /// else returns <c>false</c> and the caller's own lookup (and its own failure message) takes over.
    /// </returns>
    public static bool TryFollow(object o, string name, out object? value)
    {
        if (On(o, name) is not { } found)
        {
            value = null;
            return false;
        }

        value = found.Property.GetValue(found.Round);
        return true;
    }

    /// <summary>…and the same for a write. The bench SETS <c>_patrolCheat</c> to roster a floor and
    /// <c>_escortsThisWatch</c> to stand a captain on the top rung; both are properties now.</summary>
    public static bool TrySet(object o, string name, object? value)
    {
        if (On(o, name) is not { } found)
        {
            return false;
        }

        if (!found.Property.CanWrite)
        {
            throw new InvalidOperationException(
                $"#870 lane 6′b · the round's `{found.Property.Name}` has no setter, and a guard is trying " +
                $"to write `{name}` through it. The four collections are filled and cleared rather than " +
                "replaced; write through the object a read hands back instead of assigning a fresh one.");
        }

        found.Property.SetValue(found.Round, value);
        return true;
    }

    private static (object Round, PropertyInfo Property)? On(object o, string name)
    {
        if (!Moved.TryGetValue(name, out string? property) || Round(o) is not { } round)
        {
            return null;
        }

        PropertyInfo on = round.GetType().GetProperty(property, Either)
            ?? throw new InvalidOperationException(
                $"#870 lane 6′b · the round has no `{property}` — `{name}` was followed onto Patrol and is " +
                "not there. Rename it in this table in the same commit; do not delete the row, which would " +
                "send every patrol guard back to reading a dead name on Map.");

        return (round, on);
    }
}
