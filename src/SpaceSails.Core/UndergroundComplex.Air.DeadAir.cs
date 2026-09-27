using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE ONE THING YOU MUST NOT MISS (#609) — the dead-air card, the pressurised and dead lines, the
/// locked line, and how far down a floor is.
///
/// <para>Split out of <c>UndergroundComplex.Air.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered. Every field here is a <c>const</c>.</para>
/// </summary>
public static partial class UndergroundComplex
{
    // ── #609 · THE ONE THING YOU MUST NOT MISS ──────────────────────────────────────────────────────────
    //
    // Owner, after suffocating on B2: "I thought there is air in the base?" ... "there should be a warning
    // or something :-D" ... "maybe pop-up about you have air or you are in vacuum type ... it is vital info"
    // ... "like the basement is more dangerous than the surface now :-D" ... "on surface there are emergency
    // shelters :-D"
    //
    // He is right on every count, and the last two are the argument. The surface gives a captain a visible
    // building to run to; a dead floor gives them a number they have to have been told. The rule itself is
    // good and stays exactly as it is — the top of each shaft band holds pressure and the rest costs air —
    // but it was being announced in a pulse that fades in eight seconds, between one about bench hardware
    // and one about dust.
    //
    // So the first dead floor of an excursion stops the world and says it properly, WITH THE ARITHMETIC:
    // which floors have air, how far the nearest one is, and how long the tank has. After that the pulse
    // line is enough, because by then it is knowledge rather than news.

    public const string VacuumArtUrl = "art/the-dead-air.jpg";

    public const string VacuumCardLabel = "🫁 DEAD AIR";

    /// <summary>What the first dead floor says. It states the rule and does the sum — a warning that makes
    /// the captain work out their own margin is a warning delivered too late.
    ///
    /// <para>#740 · And it does the sum in the SUIT'S units, off <see cref="SuitAir.Clock"/>, because the
    /// card and the gauge are describing one tank and a captain compares them by eye.</para></summary>
    public static string VacuumCard(string bodyId, int level, double airSeconds)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int band = BandOf(level);
        int refuge = BandTop(band);          // the top of this band always holds pressure
        int floorsUp = -level - -refuge;     // how many floors between here and breathable

        // #740 · THE CARD READS THE GAUGE, it does not do its own sum. This used to format the raw play
        // budget as minutes and seconds — "you have 21 min 01 s" — while the HUD two seconds later on the
        // same floor said AIR 8h09. Both sentences were about the tank and both were honest about the number
        // they held; they were simply holding it in different units, because the card was the one surface in
        // the game that had never gone through SuitAir. A captain cannot be expected to know which of two
        // instruments is quoting the designer's stopwatch, so there is now one clock and the card asks for it.
        //
        // …and the sentence OWNS the quantity: it names the instrument the figure came off, so that a captain
        // who glances at their wrist a second later reads the same characters back, and so that the next hand
        // to edit this copy cannot quietly re-derive the number from something else.
        string margin = airSeconds > 0
            ? $"Your gauge reads {SuitAir.Clock(airSeconds)}, and that is the figure it will go on counting " +
              "down the whole way up."
            : "Your gauge is already reading empty, which is its own instruction.";

        string upstairs = floorsUp == 0
            ? "this floor"
            : $"{floorsUp} floor{(floorsUp == 1 ? "" : "s")} up";

        return
            "The doors part on nothing.\n\n" +
            "No pressure, no lights but yours, and the dust has not been disturbed since it settled. You " +
            $"are {MetresDown(level):F0} m under the regolith and your tank is now the clock.\n\n" +
            "THE RULE, because it is the only one down here that can kill you: the TOP FLOOR OF EVERY SHAFT " +
            "BAND holds pressure. Nothing else does. That is where the lobbies were, and the fans on those " +
            "floors are still turning on somebody's account.\n\n" +
            $"The nearest floor of air is {NameOf(bodyId, refuge)} — {upstairs}. {margin}\n\n" +
            // #608 · AND THE OTHER HALF, now that it is true. This card used to end "there are no shelters
            // down here", which was honest when it was written and is now the most dangerous sentence in the
            // game: a captain who believes it will ration a tank they did not have to ration. Owner: "there
            // should be like at least one air replenish station in each of the airless labs underground...
            // for pure safety". So the card says where the exception is, and says the two things about it
            // that decide whether it is any use — it is not beside the lift, and the instrument finds it.
            "There is a PRESSURE REFUGE on this floor. Every vacuum floor in this building has one: staff " +
            "worked these levels in suits all day, and somebody with a clipboard made the owners pay for " +
            "somewhere to go when a tank ran short. It is not beside the lift — it never is — and your " +
            "tracker paints it as a ring like any shelter on the surface.\n\n" +
            // #608 · AND THE SENTENCE THAT STOPS THE CARD PROMISING SOMETHING THE BUILDING CANNOT KEEP.
            // The paragraph above is about what was BUILT and every word of it is still true. What the card
            // may not do is let a captain read "somewhere to go when a tank ran short" as "air, forty du
            // that way" — because a quarter of these rooms have air, some hold and have nothing, and some
            // will not open at all. The plan is a drawing; the seal is decades of nobody paying for it.
            "The plan marks a refuge on this band. Whether it still holds is not on the plan.";
    }

    /// <summary>Said on stepping out on the top floor — the lie that makes the rest work.</summary>
    public const string PressurisedLine =
        "🫁 The doors part on warm air and standing lights. Your suit stops drawing and the readout holds. " +
        "Somewhere a fan is still turning, on somebody's account, decades after the last invoice.";

    /// <summary>And on every floor below it.</summary>
    public const string DeadAirLine =
        "🫁 The doors part on nothing. No pressure, no lights but yours, and the dust on the floor has not " +
        "been disturbed since it settled. Your tank starts counting again. From here down, depth costs air.";

    /// <summary>What a locked door says when the captain tries it. It never opens, and the game never pretends
    /// it might — a door that teases is a puzzle, and this is meant to be a WALL with a world behind it.
    ///
    /// <para>#1074 · <b>One door in the building answers with what is POSTED on it instead</b>, because one
    /// door in the building has something posted on it: the order at the seal. The plate is the heading and
    /// the sentence under it is the order, verbatim (<see cref="StopOrder.OrderLine"/>) — which is what a
    /// stop order IS, a piece of paper somebody stuck to a door, and it is the whole of what the world ever
    /// says about the closure. Nothing is composed here: the two strings are set side by side and no third
    /// sentence explains either of them.</para></summary>
    public static string LockedLine(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return StopOrder.IsPlate(sign)
            ? $"🔒 {sign}. {StopOrder.OrderLine}"
            : $"🔒 {sign}. The lock is not a lock you can argue with — it is a decision somebody made, and "
                + "it is still being enforced by a building whose owners stopped answering a long time ago.";
    }

    /// <summary>#600 · How far under the regolith the shed's floor a given level sits, in metres.
    ///
    /// <para>Owner: <i>"we can use seriously large numbers there :-D ... or depths (in meters)"</i>. He is
    /// right that the depth is the better number — <c>B4</c> is an index and <c>−76 m</c> is a fact about
    /// where you are standing, and it is the one that makes the walk back up mean something.</para>
    ///
    /// <para>The first floor is far down because the facility is BURIED — the shed on the surface is a lid
    /// over a shaft, and the descent card earns that ("service lamps go past in the wall at first, then a
    /// rhythm, and you find you have been counting them and have lost count"). After that a floor is a
    /// floor plus its slab, its services and the rock somebody left between levels.</para>
    ///
    /// <para>Owner, reading the paint on B1: <i>"also we could make it deeper like 150 meters :-D"</i> — and
    /// he is right, 40 m was a car park. The overburden is the number that has to sell the lid, because it
    /// is the whole ride down before the first door opens, and the descent card has always described a shaft
    /// long enough to lose count in. At 150 m it does.</para></summary>
    public const double OverburdenMetres = 150.0;

    /// <summary>Floor to floor, including the slab and the rock between.</summary>
    public const double MetresPerFloor = 12.0;

    /// <summary>Metres below the surface for a level. 0 on the surface, positive going down.</summary>
    public static double MetresDown(int level) =>
        level >= 0 ? 0 : OverburdenMetres + ((-level - 1) * MetresPerFloor);

    /// <summary>What is painted on the wall beside the lift, big enough to read on the way past.</summary>
    public static string DepthPaint(int level) =>
        level >= 0 ? "SURFACE" : $"−{MetresDown(level):F0} m";
}
