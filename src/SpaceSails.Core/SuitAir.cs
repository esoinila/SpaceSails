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
    /// never come close to it. Air is meant to price DISTANCE, not to hurry a captain who is busy.</para></summary>
    public const double TankSeconds = 360.0;

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

    /// <summary>The gauge line: how long you have, and — the part that matters — how much further you may
    /// go and still come home. A bare percentage would be exactly the silent timer this must not be.</summary>
    public static string Readout(double airLeftSeconds, double distanceHomeDu)
    {
        if (airLeftSeconds <= 0)
        {
            return "AIR — EMPTY";
        }

        string clock = $"{(int)(airLeftSeconds / 60)}:{(int)(airLeftSeconds % 60):00}";
        return BandFor(airLeftSeconds, distanceHomeDu) switch
        {
            Band.PastTheLine => $"AIR {clock} — PAST THE LINE. The walk back costs more than you are carrying.",
            Band.Critical => $"AIR {clock} — almost gone.",
            Band.Thinking => $"AIR {clock} · {RemainingReachDu(airLeftSeconds, distanceHomeDu):F0} du further, then turn.",
            _ => $"AIR {clock} · {RemainingReachDu(airLeftSeconds, distanceHomeDu):F0} du further and still home dry",
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

    /// <summary>Is the tank low enough to say so regardless of where the captain is standing?</summary>
    public static bool RunningLow(double airLeftSeconds) =>
        airLeftSeconds > 0 && airLeftSeconds <= TankSeconds * LowAirFraction;

    /// <summary>The low-air line. Says the number, says what it buys, and does NOT pretend to know whether
    /// the captain is in trouble — that is what the point-of-no-return line is for.</summary>
    public static string LowAirWarning(double airLeftSeconds, double distanceHomeDu) =>
        $"🫁 AIR LOW — {(int)(airLeftSeconds / 60)}:{(int)(airLeftSeconds % 60):00} left in the tank. " +
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

    /// <summary>Air remaining after <paramref name="dt"/> seconds outside, clamped at empty.</summary>
    public static double Drain(double airLeftSeconds, double dt) =>
        Math.Max(0.0, airLeftSeconds - Math.Max(0.0, dt));

    /// <summary>Topping the tank up — from the ship's tube, or from something found out there. Never more
    /// than a full tank, so no cache can ever hand a captain more reach than the suit can hold.</summary>
    public static double Refill(double airLeftSeconds, double seconds) =>
        Math.Clamp(airLeftSeconds + Math.Max(0.0, seconds), 0.0, TankSeconds);
}
