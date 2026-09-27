using System;

namespace SpaceSails.Core;

/// <summary>
/// #251 · GETTING HOME — the band a tank is in, the walk home and the point of no return, running low, and
/// the warnings and the last line.
///
/// <para>Split out of <c>SuitAir.cs</c> under #251 as a pure move: two runs of the base file, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>; the tank constants they read stay
/// in the opening file.</para>
/// </summary>
public static partial class SuitAir
{
    /// <summary>How the tank reads right now. A band, not a raw number, because the whole point is that a
    /// captain should be able to glance rather than calculate.</summary>
    public enum Band
    {
        /// <summary>Plenty. The walk home is not in question.</summary>
        Easy,

        /// <summary>The margin is thinning. Still fine, but this is the moment to have a plan.</summary>
        Thinking,

        /// <summary>At or past the point of no return — going further means not coming back.</summary>
        PastTheLine,

        /// <summary>Nearly gone.</summary>
        Critical,

        /// <summary>Empty.</summary>
        Gone,
    }

    /// <summary>Seconds of air a walk home of <paramref name="distanceDu"/> deck units costs — the honest
    /// number the whole mechanic hangs on.</summary>
    public static double WalkHomeSeconds(double distanceDu) =>
        Math.Max(0.0, distanceDu) / WalkSpeedDu;

    /// <summary>The air a captain must still be holding to be able to turn round here and make it, margin
    /// included. Below this and every further step is spending somebody else's air.</summary>
    public static double NeededToGetHome(double distanceDu) =>
        WalkHomeSeconds(distanceDu) * ReserveFactor;

    /// <summary>Has this captain crossed the line? Pure, and takes a DISTANCE rather than a position — the
    /// suit has no opinion about which direction "deep" is (#453).</summary>
    public static bool PastPointOfNoReturn(double airLeftSeconds, double distanceHomeDu) =>
        airLeftSeconds < NeededToGetHome(distanceHomeDu);

    /// <summary>How far a captain can still walk OUT and expect to get back — the number that makes the
    /// warning actionable instead of merely alarming. Zero once the line is behind them.</summary>
    public static double RemainingReachDu(double airLeftSeconds, double distanceHomeDu)
    {
        double spare = airLeftSeconds - NeededToGetHome(distanceHomeDu);
        if (spare <= 0)
        {
            return 0;
        }
        // Every step out must be paid for twice — out and back — both at the reserve rate.
        return spare * WalkSpeedDu / (2.0 * ReserveFactor);
    }

    /// <summary>The band the readout shows.</summary>
    public static Band BandFor(double airLeftSeconds, double distanceHomeDu)
    {
        if (airLeftSeconds <= 0)
        {
            return Band.Gone;
        }
        if (airLeftSeconds <= TankSeconds * 0.08)
        {
            return Band.Critical;
        }
        if (PastPointOfNoReturn(airLeftSeconds, distanceHomeDu))
        {
            return Band.PastTheLine;
        }
        return RemainingReachDu(airLeftSeconds, distanceHomeDu) < 60 ? Band.Thinking : Band.Easy;
    }

    /// <summary>#573 · The absolute low-air mark, as a fraction of a full tank. A SECOND warning that does
    /// not depend on distance at all.
    ///
    /// <para>The point-of-no-return line is the good one and it is useless in the world that exists. It
    /// fires when the air left drops under the cost of walking home — which on a 45-second tank needs the
    /// captain to be ~352 du from the tube, and on a full one ~1507 du. <b>The field is 78 x 64 du.</b> From
    /// anywhere in it the walk home is under ten seconds, so the line is unreachable at any tank size and
    /// the owner simply ran out, flat, having been warned about nothing — the exact silent timer this whole
    /// mechanic forbids.</para>
    ///
    /// <para>So: the tank getting low is worth saying ON ITS OWN, in any size of world. The distance line
    /// stays and starts mattering when the ground stops being a rectangle (#563).</para></summary>
    public const double LowAirFraction = 0.35;

    /// <summary>Is the tank low enough to be worth saying — AND far enough from home for it to matter?
    ///
    /// <para>Owner: <i>"we want to keep that tank mechanism for cases where we travel far out in the open
    /// ... not an adversary scarier than the old ones :-D"</i>. The first version of this was purely
    /// absolute, so it would have announced AIR LOW at a captain standing twenty paces from the tube with a
    /// refill in reach — and a warning that fires when nothing is wrong is how a resource stops being a
    /// constraint and starts being a nag. Nagging is precisely how air would out-frighten the Old Ones,
    /// which are supposed to be the scary thing here.</para>
    ///
    /// <para>So it stays quiet while the walk home is cheap against what is left. Out in the open, where
    /// getting back is a real fraction of the tank, it speaks.</para></summary>
    public static bool RunningLow(double airLeftSeconds, double distanceHomeDu) =>
        airLeftSeconds > 0
        && airLeftSeconds <= TankSeconds * LowAirFraction
        && WalkHomeSeconds(distanceHomeDu) > airLeftSeconds * HomeIsCloseEnough;

    /// <summary>Below this share of the remaining air, the walk home is cheap enough that the suit keeps its
    /// opinions to itself. A quarter: comfortably far from trouble, comfortably short of complacent.</summary>
    public const double HomeIsCloseEnough = 0.25;

    /// <summary>The low-air line. Says the number, says what it buys, and does NOT pretend to know whether
    /// the captain is in trouble — that is what the point-of-no-return line is for.</summary>
    public static string LowAirWarning(double airLeftSeconds, double distanceHomeDu) =>
        $"🫁 AIR LOW — {SuitClock(airLeftSeconds)} left in the suit. " +
        (PastPointOfNoReturn(airLeftSeconds, distanceHomeDu)
            ? "And the walk back already costs more than that."
            : $"Enough for about {RemainingReachDu(airLeftSeconds, distanceHomeDu):F0} du further out, then home.");

    /// <summary>THE ONE-TIME CROSSING LINE — said on the single step where a captain goes from being able to
    /// get home to not. This is the whole mechanic: not a number that ran out, a line that was crossed, and
    /// the game saying so while there is still a decision in it.</summary>
    public const string CrossingWarning =
        "🫁 THAT WAS THE LINE. From here the walk back costs more air than you are carrying — you are not " +
        "coming home on what is in the tank. Turn now and you make it on the reserve; go on and you had " +
        "better be right about what is out there.";

    /// <summary>#563 law 7 · THE BACKSTOP, SAID IN THE SUIT'S OWN VOICE — what a captain is told the once,
    /// ten thousand eight hundred deck units out, when the world declines to go further.
    ///
    /// <para>It lives HERE, beside <see cref="CrossingWarning"/>, and that placement is the design of it. The
    /// backstop is not a fence and must never sound like one: an invisible wall that says nothing is the
    /// failure #563 opened with, and an invisible wall that explains a technical limit is worse. What is
    /// actually true at that distance is the TANK — a captain standing out there emptied it getting there and
    /// crossed the point of no return at less than half the way — so the refusal is the same arithmetic
    /// <see cref="PastPointOfNoReturn"/> has been running since #325, stated flatly by the thing that runs it.
    /// No card, no overlay, no new register: the suit's line, on the suit's own channel.</para>
    ///
    /// <para>Said ONCE per excursion (<see cref="SurfaceEdge.BackstopVoice"/>) and never inside the radius,
    /// because a line repeated at every step along a boundary is a nag, and a nag is how a vital fact turns
    /// into wallpaper.</para>
    ///
    /// <para>Fable's canon pass, 2026-09-03, quoted on #563 and shipped VERBATIM — no channel glyph is
    /// prepended, because the authored sentence is the whole of what the suit says.</para></summary>
    public const string BackstopRefusal =
        "The suit refuses the step. Its arithmetic is simple: from here, the tank does not reach the tube.";

    /// <summary>The line as the last of it goes. Suffocation is a death the game owes the player an honest
    /// account of — they were told, once, plainly, and they chose.</summary>
    public const string SuffocationLine =
        "🫁 The tank reads empty and the suit stops pretending. You knew where the line was; you crossed it " +
        "on purpose. There are worse epitaphs.";
}
