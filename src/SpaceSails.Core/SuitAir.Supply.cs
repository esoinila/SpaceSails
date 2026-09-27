using System;

namespace SpaceSails.Core;

/// <summary>
/// #251 · WHERE THE AIR COMES FROM — the supply, its source, label, glyph and plate, and the readout.
///
/// <para>Split out of <c>SuitAir.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class SuitAir
{
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
    /// <param name="bodyId">#677 · WHICH MOON's building. Whether a floor breathes stopped being arithmetic
    /// on a level the day a band of galleries nobody dug turned out to hold pressure on every floor of
    /// itself, so the one predicate has to be told where it is standing. Required, and there is deliberately
    /// no overload without it: an optional body would be a second answer, silently right everywhere except
    /// the four floors that matter.</param>
    /// <param name="floor">The excursion's floor: 0 or above is the regolith, negative is the Hive.</param>
    /// <param name="insideShelter">Standing inside an emergency shelter's drum.</param>
    /// <param name="aboard">Past the tube's surface-end door — hers, not yours.</param>
    /// <param name="inRefuge">#608 · Standing in a pressure refuge cut into a dead floor.</param>
    public static Supply SourceOf(
        string bodyId, int floor, bool insideShelter, bool aboard, bool inRefuge = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (floor < 0 && (UndergroundComplex.HoldsPressure(bodyId, floor) || inRefuge))
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
            string held = airLeftSeconds <= 0 ? "EMPTY" : Clock(airLeftSeconds);
            return supply == Supply.Ship
                ? $"AIR {held} · FILLING — you are on her air, not the tank."
                : $"AIR {held} · SEALED — you are breathing the room, not the tank.";
        }

        if (airLeftSeconds <= 0)
        {
            return "AIR — EMPTY";
        }

        string clock = Clock(airLeftSeconds);

        return BandFor(airLeftSeconds, distanceHomeDu) switch
        {
            Band.PastTheLine => $"AIR {clock} — PAST THE LINE. The walk back costs more than you are carrying.",
            Band.Critical => $"AIR {clock} — almost gone.",
            Band.Thinking => $"AIR {clock} ▼ · {RemainingReachDu(airLeftSeconds, distanceHomeDu):F0} du further, then turn.",
            _ => $"AIR {clock} ▼ · {RemainingReachDu(airLeftSeconds, distanceHomeDu):F0} du further and still home dry",
        };
    }
}
