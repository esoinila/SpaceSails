namespace SpaceSails.Core;

/// <summary>
/// #411 · THE CYCLER WINDOW — the route the KAAMOS arc has been promising since its first line, made
/// into a thing that happens.
///
/// <para>The arc's own fiction, unchanged (<see cref="KaamosLore.CanReachEnceladus"/> and
/// <c>docs/features/KaamosPlotline.md</c> §5): reaching the ice moon is <b>not a longer shuttle hop</b>.
/// It is a slow free-return arc that comes round rarely and, for a ship that is <i>on the board</i> when
/// it opens, rides all the way in. The berth code is what puts you on the board; the supply run is what
/// files you; this class is the timetable.</para>
///
/// <para><b>What the owner ruled and what was chosen.</b> The 2026-08-03 ruling settled the DESTINATION
/// (the head of the organization) and left the vehicle open. Of #411's three costed options this is
/// <b>B — the berth code buys you a listed supply run</b>, with the cycler window as that run's
/// timetable rather than as a separate vehicle. The reasoning lives in
/// <c>docs/features/kaamos-head-office.md</c> §1 and the short version is: the horror in this game is
/// best when it is clerical, the bible's own sentence is <i>"filing for it answers it"</i>, and option B
/// is the only one where the captain literally files for it at a desk like any other job.</para>
///
/// <para><b>Pure, deterministic, world-blind.</b> No clock, no RNG, no state: the window is a grid laid
/// over sim time, so any moment can be asked when the next one is and the answer never depends on how
/// you got there. Every sentence the player reads about the window is authored here beside the predicate
/// that decides it — the #634 law, learned by an arc whose ledger counted down to the wrong number for
/// weeks because the sentence lived in the client.</para>
///
/// <para><b>What this class deliberately does NOT decide.</b> Whether the ice moon has anything on it.
/// The head office is a fact about the world, gated on the berth code and nothing else, so a captain who
/// crosses the deep black the long way round arrives at the same door as one who rode the window. The
/// window buys a timetable and a filing, never a monopoly on the destination — a route that is the only
/// way in is a railroad, and this game does not have those.</para>
/// </summary>
public static class CyclerWindow
{
    /// <summary>How often the arc comes round, in seconds of sim time. Rare enough that "hold the berth
    /// and wait for it" is a real instruction and not a formality, short enough that a captain who has
    /// earned the code is never told to come back next season. Everything downstream is derived from
    /// this one number, so tuning the wait is one edit.</summary>
    public const double PeriodSeconds = 40.0 * 86_400.0;

    /// <summary>How long it stands open once it comes. Two days: long enough that a captain who is not
    /// staring at the clock still catches it, short enough that missing it costs a wait.</summary>
    public const double OpenSeconds = 2.0 * 86_400.0;

    /// <summary>How long the ride in takes. It is a free-return arc, not a burn — the whole point of the
    /// thing is that it is slow and that the ship is a passenger on it. The sim clock advances by this,
    /// so heat bleeds, the traffic moves and the world is a month older when the ice comes up.</summary>
    public const double CrossingSeconds = 38.0 * 86_400.0;

    /// <summary>Where the ride puts the ship: co-moving with the ice moon at this range. Comfortably
    /// inside one shuttle hop (<see cref="ShuttleRange.RangeMeters"/>, 5e8 m) so the shuttle board can do
    /// the last mile the way it does everywhere else, and far outside the moon's own radius so nothing
    /// arrives underground. It is the same placement the <c>?start=enceladus</c> bench start has always
    /// used — the tested spawn, physics preserved.</summary>
    public const double ArrivalOffsetMeters = 1.0e7;

    /// <summary>#742 · How many ARRIVAL PHASES the window admits — the resolution at which the ice moon's
    /// own orbit is sampled when asking "and what if the ride let her go HERE instead?".
    ///
    /// <para>It is 24 because that is one an hour and a bit round a 32.9 h orbit, fine enough that no
    /// geometry hides between two samples and coarse enough to fly all of them in a test. The window opens
    /// every 40 days and stands open for two, so which of the 24 a captain gets is not a choice anyone
    /// makes — which is exactly why every one of them has to be safe, and why the guard sweeps all 24
    /// rather than spot-checking the one somebody happened to boot into.</para></summary>
    public const int ArrivalPhases = 24;

    /// <summary>The sim epoch at which arrival phase <paramref name="phase"/> lands, given the moon's own
    /// orbital period. The period is an ARGUMENT and not a constant here on purpose: a moon's number
    /// belongs to the moon, and a copy of it filed under the cycler is the house's oldest bug — a MOON
    /// constant living somewhere that is not the moon. Callers read it off the ephemeris body.</summary>
    public static double ArrivalPhaseEpoch(double moonPeriodSeconds, int phase) =>
        Math.Abs(moonPeriodSeconds) * (((phase % ArrivalPhases) + ArrivalPhases) % ArrivalPhases) / ArrivalPhases;

    /// <summary>Which window this sim time falls in or before — the index on the grid. Windows open at
    /// integer multiples of <see cref="PeriodSeconds"/>.</summary>
    private static double OpeningAtOrBefore(double simTime) =>
        Math.Floor(Math.Max(0.0, simTime) / PeriodSeconds) * PeriodSeconds;

    /// <summary>Is the window open right now?</summary>
    public static bool IsOpen(double simTime) =>
        simTime >= 0.0 && simTime - OpeningAtOrBefore(simTime) < OpenSeconds;

    /// <summary>When the next window opens — the one currently open, if one is, else the next on the
    /// grid. Always ≥ 0.</summary>
    public static double NextOpening(double simTime)
    {
        double at = OpeningAtOrBefore(simTime);
        return IsOpen(simTime) ? at : at + PeriodSeconds;
    }

    /// <summary>Seconds until the window opens; zero while it is open.</summary>
    public static double SecondsUntilOpen(double simTime) =>
        IsOpen(simTime) ? 0.0 : Math.Max(0.0, NextOpening(simTime) - simTime);

    /// <summary>Seconds until an OPEN window shuts; zero when it is not open.</summary>
    public static double SecondsUntilShut(double simTime) =>
        IsOpen(simTime) ? Math.Max(0.0, OpeningAtOrBefore(simTime) + OpenSeconds - simTime) : 0.0;

    // ── The gate ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Why the ride is not available. Every one of these has a sentence, because a control that
    /// is visible-but-disabled with no reason is the thing #212 was filed about.</summary>
    public enum Blocker
    {
        /// <summary>Nothing in the way — the window is open and this hull is on the board.</summary>
        None,

        /// <summary>The berth code is not in hand (or the intel behind it is not), so the berth is not
        /// listed to this hull and no arc will carry it.</summary>
        NotOnTheBoard,

        /// <summary>On the board, but the run has not been taken. The window carries a FILING, not a
        /// sightseer.</summary>
        NoRunFiled,

        /// <summary>Filed, and the arc is simply not here yet.</summary>
        WindowShut,
    }

    /// <summary>The whole gate, pure. <paramref name="onTheBoard"/> is
    /// <see cref="KaamosLore.CanReachEnceladus"/>; <paramref name="runFiled"/> is whether the KAAMOS
    /// supply run is in hand.</summary>
    public static Blocker Evaluate(bool onTheBoard, bool runFiled, double simTime)
    {
        if (!onTheBoard)
        {
            return Blocker.NotOnTheBoard;
        }

        if (!runFiled)
        {
            return Blocker.NoRunFiled;
        }

        return IsOpen(simTime) ? Blocker.None : Blocker.WindowShut;
    }

    // ── The voice ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A duration as the arc's own people would say it: whole days, and hours under a day.
    /// Deliberately coarse — a cycler timetable that quoted seconds would be a spreadsheet.</summary>
    public static string Wait(double seconds)
    {
        double s = Math.Max(0.0, seconds);
        if (s < 3600.0)
        {
            return "within the hour";
        }

        if (s < 86_400.0)
        {
            int hours = (int)Math.Round(s / 3600.0);
            return $"in {hours} h";
        }

        int days = (int)Math.Round(s / 86_400.0);
        return $"in {days} d";
    }

    /// <summary>The button, when it can be pressed.</summary>
    public const string MenuAction = "❄ Ride the cycler window in";

    /// <summary>The button's label when the arc is not here. It quotes the wait, because "not yet" with
    /// no number is the thing a captain cannot plan around.</summary>
    public static string MenuWaiting(double simTime) =>
        $"❄ The cycler window opens {Wait(SecondsUntilOpen(simTime))}";

    /// <summary>Why the control will not go, in words, per blocker. Never a production note.</summary>
    public static string RefusalText(Blocker blocker, double simTime) => blocker switch
    {
        Blocker.NotOnTheBoard =>
            "The berth is not listed to this hull. Nothing that comes round out here stops for a ship " +
            "that nobody has filed for.",
        Blocker.NoRunFiled =>
            "You are on the board and you are carrying nothing. The arc takes a run, not a passenger — " +
            "file for the berth, load what the berth asked for, and it will take you both.",
        Blocker.WindowShut =>
            $"The arc is not here. It comes round {Wait(SecondsUntilOpen(simTime))}, and it does not wait " +
            "and cannot be hurried. Hold the berth.",
        _ => "",
    };

    /// <summary>The line the moment the ride is committed — said once, as the ship stops manoeuvring and
    /// starts being carried. It is the arc's biggest promise being kept, and it keeps it in the arc's own
    /// register: paperwork, and a long quiet.</summary>
    public const string Departing =
        "❄❄ You put her on the arc and stop flying. The board acknowledges without comment — one line in a " +
        "queue of thousands, a consignment moving as consignments do — and then there is nothing to do for " +
        "a very long time but be carried, and check, every few days, that the number on the manifest is " +
        "still yours.";

    /// <summary>And the line on arrival. It names the ice, the berth and the silence, and stops. It does
    /// not say what is down there, because nothing here ever does.</summary>
    public const string Arrived =
        "❄❄ The arc lets her go where it always meant to and the ice comes up white in the lamps, edge to " +
        "edge, no terminator anywhere on it. The berth acknowledges the run. It does not say welcome and " +
        "it does not say anything else; the manifest simply moves from AWAITED to ALONGSIDE, which is what " +
        "a manifest does when a ship arrives, and the ship it has been waiting for is yours.";

    /// <summary>The wire's version — routine, filed by a clerk, and not about you at all. This is the
    /// arc landing on the news (<c>StoryBeats.Beat.ArcNewsBreaks</c>, #663): the world noticing in the
    /// flattest possible voice, which is the only voice that makes it frightening.</summary>
    public const string BerthListedHeadline =
        "Exchange housekeeping: one dormant long-haul berth has come back onto the active listing after " +
        "decades on the held register. The Exchange notes only that a hull has filed against it.";
}
