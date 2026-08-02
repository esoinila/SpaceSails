using System;

namespace SpaceSails.Core;

/// <summary>
/// #564 · THE TANK — the suit air a captain spends standing on an airless world, and the one thing that
/// tells them they have gone too far while they can still do something about it.
///
/// <para>The game has promised this since #440. <see cref="GroundLesson"/>'s very first law reads <i>"The
/// walk back is half the tank. Turn around before you think you need to."</i> — taught to every new captain,
/// about a resource that did not exist. Nothing drained, nothing displayed, nothing could run out. The one
/// place the game speaks with authority to somebody who cannot check, and it was describing a rule it did
/// not enforce.</para>
///
/// <para><b>Why air and not something else.</b> Owner, 2026-07-31: <i>"we can also use the suit limits of
/// air / oxygen to tell the player that they are crossing a point of no return"</i>, and — the part that
/// decides it — <i>"that one does not need the site to be hostile ... it is neutral in a sense."</i> Ammunition
/// prices FIGHTING and the pack prices NOISE, so both bite only where there is something to fight; on a
/// quiet moon nothing spends and distance is free. Air spends everywhere. It is what gives a harmless,
/// empty site a shape without having to put something nasty on it.</para>
///
/// <para>It is also the only tether that can COMPUTE the point of no return, because the walk home has a
/// known cost in the same units as the thing draining. Ammunition cannot tell you that. Geometry certainly
/// cannot — and per #453 it must not try: the suit does not care how DEEP you are, only how far you are
/// from the way home, which is a fact about the route rather than about a coordinate.</para>
///
/// <para><b>THE RULE THIS IS BUILT UNDER: air must never be a silent timer that kills you.</b> The tell is
/// a line you CROSSED, said once and plainly, not a number you failed to watch. A countdown that quietly
/// runs out is the same design failure as an invisible wall — the game knew and did not say.</para>
/// </summary>
public static class SuitAir
{
    /// <summary>A full tank, in seconds of time outside. THE tuning dial for how far a landing site
    /// reaches.
    ///
    /// <para>Sized against the owner's own acceptance test: <i>"walk in some direction until I get warning
    /// that my point of no return to walk back is soon... then I just continue the same distance more and
    /// suffocate."</i> That works out when the warning lands near the halfway mark, which is what
    /// <see cref="ReserveFactor"/> arranges — so a full tank has to be about twice the longest walk worth
    /// taking. At the deck's 9 du/s this is roughly 1,600 deck units of travel, or some twenty times the
    /// width of the old fenced field.</para>
    ///
    /// <para>Deliberately generous for ordinary work: a dig-and-bury run inside the landing area should
    /// never come close to it. Air is meant to price DISTANCE, not to hurry a captain who is busy.</para>
    ///
    /// <para>Raised from six minutes to TWENTY on the owner's reckoning: <i>"The air should be like at least
    /// 10 minutes... like typical dive tanks give like 30 minutes."</i> Which is the right comparison — a
    /// working set of tanks is measured in tens of minutes, and six was a stopwatch rather than a supply. At
    /// the deck's 9 du/s this is some 10,800 deck units of walking, so the field is now the thing that runs
    /// out first, and the tank is what makes the far end of it a decision.</para></summary>
    public const double TankSeconds = 1200.0;

    /// <summary>#573 · WHAT THE SUIT SAYS IT HOLDS, in hours — the figure a captain reads, as opposed to the
    /// budget the game spends.
    ///
    /// <para>Owner: <i>"I think rebreather tanks for divers give much more time.. let's mimic those times...
    /// also how long do the NASA suits give... let's have similar lengths of air."</i> The real numbers:
    /// open-circuit scuba is 30–60 minutes, a closed-circuit REBREATHER is three to six hours, and a NASA
    /// EMU carries about EIGHT hours of primary life support plus a separate half-hour secondary pack.</para>
    ///
    /// <para>So the suit says eight hours, because that is what a suit like this holds. What it does NOT do
    /// is make an excursion take eight hours of anybody's evening — the surface clock has always been
    /// compressed (a captain crosses a 310 du field in half a minute of walking and nobody believes that is
    /// a real hike). The tank is spent against the same compressed clock as everything else, and merely
    /// REPORTED in the units the fiction uses.</para></summary>
    public const double PrimaryHours = 8.0;

    /// <summary>The secondary oxygen pack, in minutes — the EMU's real emergency reserve, and the best gift
    /// the research made to this design. When the primary is gone you are not dead, you are ON THE RESERVE:
    /// a hard, separate, loudly-announced half hour that exists for exactly one purpose, which is getting
    /// you back. It turns "the tank ran out" from an ending into the last decision of the walk.</summary>
    public const double ReserveMinutes = 30.0;

    /// <summary>The reserve's share of the play budget, held past <see cref="TankSeconds"/>.</summary>
    public static double ReserveSeconds => TankSeconds * (ReserveMinutes / 60.0 / PrimaryHours);

    /// <summary>Everything the suit carries, primary and reserve, in play-seconds.</summary>
    public static double FullSeconds => TankSeconds + ReserveSeconds;

    /// <summary>Is the captain down to the secondary pack?</summary>
    public static bool OnTheReserve(double airLeftSeconds) =>
        airLeftSeconds > 0 && airLeftSeconds <= ReserveSeconds;

    /// <summary>The suit's own reading of what is left, in hours and minutes — <see cref="PrimaryHours"/>
    /// worth at full, counting down the way a real one would.</summary>
    public static string SuitClock(double airLeftSeconds)
    {
        double hoursLeft = Math.Max(0, airLeftSeconds) / TankSeconds * PrimaryHours;
        int h = (int)hoursLeft;
        int m = (int)Math.Round((hoursLeft - h) * 60);
        if (m == 60) { h++; m = 0; }
        return $"{h}h{m:00}";
    }

    /// <summary>The line as the primary gives out and the secondary pack cuts in — once, loudly.</summary>
    public const string ReserveEngagedLine =
        "🫁 PRIMARY EXHAUSTED — the secondary pack cuts in. Thirty minutes, and it is the last thirty you " +
        "have. Whatever you were going to do out here, you are doing it on the way back now.";

    /// <summary>How much more air than the bare walk home the suit insists on before it stops warning. The
    /// walk back is never clean — you will detour round a scarp, you will stop, and something may make you
    /// run. A margin of nothing is a promise the ground cannot keep.</summary>
    public const double ReserveFactor = 1.15;

    /// <summary>The captain's walking pace in deck units per second — the deck's own movement speed, so the
    /// arithmetic the suit does and the arithmetic the boots do are the same arithmetic.</summary>
    public const double WalkSpeedDu = 9.0;

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

    /// <summary>
    /// #612 · WHERE THE AIR IS COMING FROM — the one fact the gauge has never stated.
    ///
    /// <para>Owner, standing on a pressurised floor a hundred and fifty metres under a moon: <i>"I thought
    /// there is air in the base?"</i> / <i>"where here does it say if I consume tanks or have air?"</i> /
    /// <i>"Maybe we should have on our hud a AIR: Tanks / External symbol... now we don't see if we need to
    /// worry about O2 from anywhere. That is really important info for the suit hud to tell us."</i></para>
    ///
    /// <para><b>Why it is the most consequential thing on the instrument.</b> The gauge shows a clock. A
    /// clock counting down and a clock that is simply parked look nearly identical at a glance, and the
    /// difference between them is the difference between <i>every minute out here costs me</i> and <i>this
    /// one is free</i>. The whole tank mechanic is built under one rule — air must never be a silent timer
    /// — and a timer whose captain cannot tell whether it is running is precisely that.</para>
    ///
    /// <para>Three roofs, because the game genuinely has three and they are not the same promise: the suit
    /// itself, an atmosphere somebody else is paying for, and the ship — which is the only one that gives
    /// the tank back.</para>
    /// </summary>
    public enum Supply
    {
        /// <summary>The suit. The clock is real and it is running.</summary>
        Tanks,

        /// <summary>An atmosphere already here — a shelter's drum, a floor of the Hive that still holds
        /// pressure, or a #608 pressure refuge on a dead one. The intake shuts and the clock is parked. It is
        /// not a refill: standing here is safety, not resupply.</summary>
        Room,

        /// <summary>Her own air, aboard or in her tube — the one place the tank fills.</summary>
        Ship,
    }

    /// <summary>
    /// THE ONE PREDICATE. Everything that has an opinion about the captain's air — the drain that spends
    /// it, the gauge that reports it, and the plate painted by the lift — asks this and nothing else.
    ///
    /// <para>Owner: <i>"It has to agree with the plate by the lift on every floor."</i> Two instruments
    /// disagreeing about whether you can breathe is worse than one instrument saying nothing, and the way
    /// that happens is never malice — it is two places each working the answer out for themselves and one
    /// of them being edited. So there is one place, and it is here, in Core, where it can be pinned.</para>
    ///
    /// <para>The ORDER is load-bearing and is the drain's own order: the floor is asked first (a pressurised
    /// floor holds whatever is standing on it), then a refuge cut into a dead one, then the shelter underfoot,
    /// then the ship. Anything else and the sim would spend on a rule the readout did not report.</para>
    ///
    /// <para><b>It has already failed once for want of this.</b> #612's first cut computed the source as its
    /// own expression beside the drain's branches, and #608 then added a fourth way to breathe — a pressure
    /// refuge on an airless floor. The drain learned about it and the gauge did not: a captain sitting in
    /// air, being told in colour that their tank was running out. <b>Anything that ever becomes a new place
    /// to breathe is added HERE, once, and every surface follows.</b></para>
    /// </summary>
    /// <param name="floor">The excursion's floor: 0 or above is the regolith, negative is the Hive.</param>
    /// <param name="insideShelter">Standing inside an emergency shelter's drum.</param>
    /// <param name="aboard">Past the tube's surface-end door — hers, not yours.</param>
    /// <param name="inRefuge">#608 · Standing in a pressure refuge cut into a dead floor.</param>
    public static Supply SourceOf(int floor, bool insideShelter, bool aboard, bool inRefuge = false)
    {
        if (floor < 0 && (UndergroundComplex.HoldsPressure(floor) || inRefuge))
        {
            return Supply.Room;
        }
        if (insideShelter)
        {
            return Supply.Room;
        }
        return aboard ? Supply.Ship : Supply.Tanks;
    }

    /// <summary>Is the tank actually being spent? The single bit the whole readout turns on.</summary>
    public static bool Drawing(Supply supply) => supply == Supply.Tanks;

    /// <summary>The source, as a word. Short enough to sit on the gauge beside AIR.</summary>
    public static string SourceLabel(Supply supply) => supply switch
    {
        Supply.Room => "ROOM",
        Supply.Ship => "SHIP",
        _ => "TANKS",
    };

    /// <summary>The source, as a SYMBOL — because a captain glances at this, they do not parse it.
    ///
    /// <para>The glyph carries the consequential half (is the clock running: falling, parked, or filling)
    /// and the word carries the source. Each one is distinct from every other, so the shape alone separates
    /// them at a size where the letters have stopped being letters.</para></summary>
    public static string SourceGlyph(Supply supply) => supply switch
    {
        Supply.Room => "■",
        Supply.Ship => "▲",
        _ => "▼",
    };

    /// <summary>What a floor's own plate says about itself — the sign by the lift, in the same words the
    /// suit uses, off the same predicate. Two lines that were computed separately would eventually drift;
    /// these cannot.</summary>
    public static string PlateLine(Supply supply) => supply switch
    {
        Supply.Room => "PRESSURISED · TANK STOPPED",
        Supply.Ship => "SHIP'S AIR · TANK FILLING",
        _ => "NO ATMOSPHERE · TANK RUNNING",
    };

    /// <summary>Said ONCE, on the step where the tank starts or stops — the owner's <i>"maybe pop-up about
    /// you have air or you are in vacuum type ... it is vital info :-D"</i>.
    ///
    /// <para>Only ever on a crossing, never as a state that repeats: the gauge is for the state, and a line
    /// that fires every frame you stand somewhere is how a vital fact becomes wallpaper.</para></summary>
    public static string SupplyChangedLine(Supply now) => now switch
    {
        Supply.Room =>
            "🫁 PRESSURE. The suit's intake shuts and the tank stops — you are breathing the room. Every " +
            "minute you spend in here is free. The moment you step out it is not.",
        Supply.Ship =>
            "🫁 HER AIR. The tank stops and starts filling — the only place in the world it does.",
        _ =>
            "🫁 VACUUM. The suit seals and the tank cuts in. From here the clock is real, and it is yours.",
    };

    /// <summary>The gauge line: how long you have, and — the part that matters — how much further you may
    /// go and still come home. A bare percentage would be exactly the silent timer this must not be.
    ///
    /// <para>#612 · And WHERE it is coming from. It did not say, anywhere: the gauge showed a duration and a
    /// distance and left the single most important bit — whether that duration is going DOWN — to be inferred
    /// from where the captain thought they were standing. On a surface that was survivable, because the answer
    /// was always yes. It stopped being survivable the day floors existed that hold pressure and floors that
    /// do not, sixty seconds apart by lift: a captain sitting in a pressurised lobby watching a countdown they
    /// believe is running will leave far too early, and one who thinks they are sealed when they are not will
    /// not leave at all.</para>
    ///
    /// <para>A parked clock must not be able to read like a running one, so the sentence changes SHAPE rather
    /// than gaining an adjective: the reach advice ("N du further, then turn") is arithmetic about spending,
    /// and quoting it at somebody who is not spending is the instrument answering a question nobody asked.
    /// The held branch is taken BEFORE the empty one, because an empty tank in an atmosphere is not an
    /// emergency and a bare "AIR — EMPTY" over a captain who is breathing fine would be the gauge lying in
    /// the most frightening direction it has.</para></summary>
    public static string Readout(double airLeftSeconds, double distanceHomeDu, Supply supply = Supply.Tanks)
    {
        if (!Drawing(supply))
        {
            string held = airLeftSeconds <= 0
                ? "EMPTY"
                : OnTheReserve(airLeftSeconds)
                    ? $"RESERVE {(int)Math.Ceiling(airLeftSeconds / ReserveSeconds * ReserveMinutes)}m"
                    : SuitClock(airLeftSeconds);
            return supply == Supply.Ship
                ? $"AIR {held} · FILLING — you are on her air, not the tank."
                : $"AIR {held} · SEALED — you are breathing the room, not the tank.";
        }

        if (airLeftSeconds <= 0)
        {
            return "AIR — EMPTY";
        }

        string clock = OnTheReserve(airLeftSeconds)
            ? $"RESERVE {(int)Math.Ceiling(airLeftSeconds / ReserveSeconds * ReserveMinutes)}m"
            : SuitClock(airLeftSeconds);

        return BandFor(airLeftSeconds, distanceHomeDu) switch
        {
            Band.PastTheLine => $"AIR {clock} — PAST THE LINE. The walk back costs more than you are carrying.",
            Band.Critical => $"AIR {clock} — almost gone.",
            Band.Thinking => $"AIR {clock} ▼ · {RemainingReachDu(airLeftSeconds, distanceHomeDu):F0} du further, then turn.",
            _ => $"AIR {clock} ▼ · {RemainingReachDu(airLeftSeconds, distanceHomeDu):F0} du further and still home dry",
        };
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

    /// <summary>The line as the last of it goes. Suffocation is a death the game owes the player an honest
    /// account of — they were told, once, plainly, and they chose.</summary>
    public const string SuffocationLine =
        "🫁 The tank reads empty and the suit stops pretending. You knew where the line was; you crossed it " +
        "on purpose. There are worse epitaphs.";

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
