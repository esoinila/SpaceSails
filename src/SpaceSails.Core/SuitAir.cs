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
public static partial class SuitAir
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

    // ── #325 · THE EXTENDED TANK, AND WHY THE CONSTANT ABOVE IS NO LONGER THE ANSWER ──────────────────────
    //
    //  Owner, #325 item 5: "The kiosk finally sells something load-bearing: extended tanks / spare bottles
    //  as purchasable margin ... the tourist shop becomes an outfitter."
    //
    //  A bottle that only moves a number on a gauge is not margin, it is decoration. What a captain actually
    //  buys is a bigger WORLD for one excursion: the ground reaches further, the turn-back point is further
    //  out, and the suit refuses the step further out — because every one of those is the same arithmetic,
    //  tank seconds against a walking pace, and always has been.
    //
    //  Which is exactly why TankSeconds may not be read directly any more. Before this, SIX places in src/
    //  multiplied or divided by the constant to answer "how big is this excursion" — the backstop radius,
    //  the tile lattice's own extent, the meter's full mark, the shelter rack's fill cap, its gauge line and
    //  the boot cheat's clamp. Fitting a tank and leaving any one of them on the constant is this project's
    //  third named bug class with a bottle in its hand: the sim doing one thing while a drawn shape or a
    //  sentence reports another. A captain would have walked out past the fence with air to spare and been
    //  told by the suit that the tank does not reach the tube — which it now does.
    //
    //  So PlayBudget is the ONE function, it takes the excursion's own fact, and the guard for it is a
    //  source sweep (TheChandleryTests) that fails on any direct reader of TankSeconds outside this file.

    /// <summary>#325 · What an extended tank multiplies the excursion's play budget by — and, because a
    /// chandlery prices what it sells rather than inventing a figure, what the haven charges for one in
    /// rounds at its own bar (<see cref="Chandlery.ExtendedTankPrice"/>). One number, both meanings: the
    /// day somebody re-tunes the bottle, the price moves with it and nobody has to remember.
    ///
    /// <para><b>Twice, and not more.</b> Fable's line for the fitting says <i>"twice the walk, and the walk
    /// back is still half"</i> — the second half of that sentence is the law (<see cref="ReserveFactor"/> is
    /// untouched, so the turn-back point stays where the fiction says it is, just further out), and the
    /// first half is this.</para></summary>
    public const int ExtendedTankFactor = 2;

    /// <summary>
    /// #325 · THE EXCURSION'S PLAY BUDGET — the one function anything that wants to know how much air this
    /// walk is worth must ask. Never <see cref="TankSeconds"/>, which is only ever the STANDARD bottle.
    ///
    /// <para>The reserve is deliberately NOT in here. <see cref="ReserveSeconds"/> is the EMU's separate
    /// secondary pack — half an hour that exists to get you home — and a chandlery does not sell you a
    /// bigger emergency. Buying margin must not quietly buy a longer grace period as well, or the one
    /// honest half-hour in the game becomes a function of your purse.</para>
    /// </summary>
    /// <para><b>What deliberately stays on the standard bottle, inside this file.</b> Five readers, and
    /// every one of them on purpose:</para>
    /// <list type="bullet">
    /// <item><see cref="ReserveSeconds"/> — the secondary pack is not for sale; see the paragraph above.
    /// </item>
    /// <item><see cref="SuitClock"/> — the play-to-fiction conversion, which is a RATE rather than a size.
    /// A full extended bottle reads 16h00 through it, which is the honest thing for a suit carrying twice
    /// the air.</item>
    /// <item><see cref="BandFor"/>'s CRITICAL threshold and <see cref="RunningLow"/>'s low mark — both are
    /// an ABSOLUTE quantity of air remaining, and ninety-six seconds left is ninety-six seconds left
    /// whichever bottle it came out of. Scaling them would have made a bought tank quieter as well as
    /// bigger, which is the one thing #564's "air must never be a silent timer" forbids.</item>
    /// <item><see cref="CostOfTask"/> — a job takes the air a job takes. Digging a hole does not get cheaper
    /// because you are carrying more.</item>
    /// </list>
    /// <param name="extendedTank">Whether the excursion left the ship with a fitted extended tank.</param>
    public static double PlayBudget(bool extendedTank) =>
        TankSeconds * (extendedTank ? ExtendedTankFactor : 1);

    /// <summary>Everything the suit carries on THIS excursion, primary and reserve, in play-seconds — the
    /// budget-aware twin of <see cref="FullSeconds"/>.</summary>
    public static double FullSecondsWith(bool extendedTank) => PlayBudget(extendedTank) + ReserveSeconds;

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

    /// <summary>#740 · THE TANK AS THE CAPTAIN READS IT — the one string anything on screen is allowed to
    /// print when it wants to say how much air is left.
    ///
    /// <para>The suit runs on two clocks and always has: <see cref="TankSeconds"/> is the PLAY budget, spent
    /// against the same compressed surface clock as everything else, and <see cref="PrimaryHours"/> is what
    /// the fiction says the same tank holds. <see cref="SuitClock"/> is the conversion, and every surface
    /// that has ever quoted air has gone through it — except one. The DEAD AIR card did its own arithmetic
    /// on the raw budget and printed <i>"you have 21 min 01 s"</i> at a captain whose gauge, two seconds
    /// later on the same floor, read <b>AIR 8h09</b>. Same tank, same instant, off by a factor of twenty
    /// three, and #612's law is that the instruments may never disagree about air.</para>
    ///
    /// <para>The two-places-computing-one-fact shape is the thing to remove, not the sum, so the reserve
    /// branch <see cref="Readout"/> already wrote twice lives here now and the card asks for it. Anything
    /// that ever wants to say a number of minutes of air asks HERE.</para></summary>
    public static string Clock(double airLeftSeconds) =>
        OnTheReserve(airLeftSeconds)
            ? $"RESERVE {(int)Math.Ceiling(airLeftSeconds / ReserveSeconds * ReserveMinutes)}m"
            : SuitClock(airLeftSeconds);

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
}
