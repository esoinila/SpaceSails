using System;

namespace SpaceSails.Core;

/// <summary>
/// #251 · WHAT BREATHING COSTS — the nested <c>Breathing</c> rates, the cost of a task, and drain and
/// refill.
///
/// <para>Split out of <c>SuitAir.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered; the nested <c>Breathing</c> class travels whole.</para>
/// </summary>
public static partial class SuitAir
{
    /// <summary>
    /// #573 · HOW HARD YOU ARE BREATHING — the multiplier on everything the suit spends.
    ///
    /// <para>All of this is the owner's, and all of it is from actually being underwater: <i>"when I was
    /// scuba diving they said to keep calm so the O2 does not run out ... well I saw a 2 meter shark after
    /// that :-D ... I think close encounters with reevers and running from them might consume more air than
    /// a lazy stroll or standing still"</i>; <i>"keep calm in face of danger so you don't choke :-D ... it
    /// gives nice mood"</i>; <i>"that could be hooked to the nerve-meter and taken damage"</i>; and the one
    /// that settles the injury term — <i>"I was diving once with an upset stomach (little sick, but I hid
    /// it) and that time my O2 ran much faster."</i></para>
    ///
    /// <para><b>Why this is the making of the mechanic.</b> Until now air was a distance tax and nothing
    /// else, which is a stopwatch. Breathing rate turns it into something a captain can PLAY: standing
    /// still while a pack goes past is now cheaper than running from it, so "keep your nerve" stops being
    /// flavour text and becomes the correct move. It also makes the nerve gauge and the condition pips
    /// matter twice — they were meters that described you; now they spend your air.</para>
    ///
    /// <para>Multiplicative, because the terms genuinely compound — a frightened, wounded captain sprinting
    /// is worse than any one of those — and capped, because there has to be a worst case a player can plan
    /// against.</para>
    /// </summary>
    public static class Breathing
    {
        /// <summary>Standing still, at rest. Cheaper than walking — the reward for holding position.</summary>
        public const double Still = 0.8;

        /// <summary>An ordinary walk. The baseline everything else is quoted against.</summary>
        public const double Walking = 1.0;

        /// <summary>Running. Owner's shark: the fastest way to empty a tank is to need it most.</summary>
        public const double Running = 1.7;

        /// <summary>Hard physical work — digging, levering, cutting. Owner: <i>"physical chores like digging
        /// a hole could cost more even though not taking as long."</i> Exactly so: effort is not measured in
        /// minutes.</summary>
        public const double HeavyLabour = 2.2;

        /// <summary>The most the fear and injury terms together may cost, so there is always a worst case a
        /// captain can plan against rather than an unbounded spiral.</summary>
        public const double MaxDistress = 2.4;

        /// <summary>The multiplier for a captain's state: what they are doing, how frightened they are, and
        /// how badly they are hurt.</summary>
        /// <param name="exertion">One of the constants above.</param>
        /// <param name="nerve">The #317 nerve gauge, 0..100, where 100 is steady hands.</param>
        /// <param name="hitsTaken">Blows landed, 0..<c>CaptainCondition.MaxHits</c>.</param>
        /// <param name="maxHits">The condition marker's full complement.</param>
        public static double Rate(double exertion, double nerve, int hitsTaken, int maxHits)
        {
            // Fear. Steady hands cost nothing extra; a shattered captain is gulping it.
            double calm = Math.Clamp(nerve / 100.0, 0.0, 1.0);
            double fear = 1.0 + (0.55 * (1.0 - calm) * (1.0 - calm));

            // Injury. The owner's upset stomach, generalised: a body in trouble burns more just existing.
            double hurt = maxHits <= 0 ? 1.0 : 1.0 + (0.5 * Math.Clamp(hitsTaken / (double)maxHits, 0, 1));

            double distress = Math.Min(fear * hurt, MaxDistress);
            return Math.Max(0.2, exertion) * distress;
        }

        /// <summary>The line the suit says when a captain's breathing is what is killing them, rather than
        /// the walk. Said once when the rate first goes properly bad — the mood the owner was after: keep
        /// calm in the face of the thing, or choke.</summary>
        public const string HardBreathingLine =
            "🫁 You can hear yourself in the helmet. Frightened, hurt, and moving — the suit is spending air " +
            "faster than the walk home is getting shorter. Stand still if you can bear to.";

        /// <summary>Above this the breathing itself is worth remarking on.</summary>
        public const double WorthMentioning = 1.9;
    }

    /// <summary>#573 · WORK COSTS AIR — the price, in play-seconds, of a job that took
    /// <paramref name="fictionMinutes"/> of a captain's life.
    ///
    /// <para>Owner: <i>"we could have long taking tasks on the sites that can eat up the oxygen ... that way
    /// we can kind of decide when it becomes an issue and when not"</i>, and then the shape of it —
    /// <i>"the user presses E to do something difficult / time consuming and we can jump to the time of the
    /// task being completed (and equivalent air consumed)"</i>.</para>
    ///
    /// <para>This is the piece that makes an eight-hour suit mean something. Walking cannot threaten it —
    /// nobody is going to walk for eight hours — but a hatch that takes forty minutes to cut is a real bite
    /// out of one, and it takes five seconds of an evening. It also hands the DESIGNER the dial: air becomes
    /// a problem exactly where a task is priced to make it one, and stays quiet everywhere else, which is
    /// the whole of "not an adversary scarier than the old ones".</para>
    ///
    /// <para>And it is honest about the compression the surface has always run on: the clock jumps because
    /// the work took that long, and the tank is charged for every minute of it.</para></summary>
    public static double CostOfTask(double fictionMinutes, double exertion = Breathing.Walking) =>
        Math.Max(0, fictionMinutes) / 60.0 / PrimaryHours * TankSeconds * Math.Max(0.2, exertion);

    /// <summary>How the suit reports a job it has just paid for.</summary>
    public static string TaskCostLine(string what, double fictionMinutes) =>
        $"⏱ {what} — {(int)Math.Round(fictionMinutes)} minutes of it, and the suit charged you for every one.";

    /// <summary>Would this task leave the captain unable to get home? The question worth asking BEFORE the
    /// clock jumps, because a time-skip that strands you is a trap rather than a decision.</summary>
    public static bool TaskWouldStrandYou(double airLeftSeconds, double fictionMinutes, double distanceHomeDu) =>
        PastPointOfNoReturn(airLeftSeconds - CostOfTask(fictionMinutes), distanceHomeDu);

    /// <summary>Air remaining after <paramref name="dt"/> seconds outside, clamped at empty.</summary>
    public static double Drain(double airLeftSeconds, double dt) =>
        Math.Max(0.0, airLeftSeconds - Math.Max(0.0, dt));

    /// <summary>Topping the tank up — from the ship's tube, or from something found out there. Never more
    /// than a full tank, so no cache can ever hand a captain more reach than the suit can hold.</summary>
    public static double Refill(double airLeftSeconds, double seconds) =>
        Math.Clamp(airLeftSeconds + Math.Max(0.0, seconds), 0.0, FullSeconds);
}
