namespace SpaceSails.Core;

/// <summary>
/// #524 · FIRE, AND THE THREE WAYS TO PUT IT OUT. Owner, looking at her new atmosphere board and asking what it
/// is FOR: <i>"if a room is on fire then pumping air out of it or venting it to space is a good way to stop the
/// fire"</i> — and then, on where it should live first: <i>"One of those could have the fire also."</i>
///
/// <para>So it goes on a derelict before it ever goes on her: the REACTOR CASCADE, whose whole fiction is that
/// the aft third is slag and the radiation is still interesting. A hull that burned is a hull that can still be
/// burning, forty years on, wherever a pocket of atmosphere and something combustible are still shut in
/// together.</para>
///
/// <para><b>Why fire is the right second system.</b> The infested hull has four things that act while you are
/// aboard — a nest that breeds, a pack that hunts, a vacuum that kills, pumps that race it — and the other nine
/// causes have none. Fire is the cheapest possible way to give a second hull life, because it needs no new
/// verbs: it spreads along <see cref="HullVenting.SharedAtmosphere"/>'s own door graph, it dies to the two
/// handles the board already has, and it threatens the one thing a salvage captain actually came for.</para>
///
/// <para><b>And it makes the board a tool rather than a weapon.</b> On the infested hull the valves are how you
/// kill something. Here they are how you save something — the same switches, the opposite intent, which is the
/// most economical way this game has ever been handed a second meaning for a thing it already built.</para>
/// </summary>
public static class HullFire
{
    /// <summary>
    /// THE THREE ROADS, and the reason this is a decision rather than a chore. Each one costs something
    /// different, and no captain gets to have all three.
    /// </summary>
    public enum Answer
    {
        /// <summary>Crack the valve. Instant, and the air is gone — you spent the atmosphere to save the
        /// cargo.</summary>
        VentIt,

        /// <summary>Put it on the pumps. Slower, and the air ends up in the tanks — but the fire keeps eating
        /// while the pump runs.</summary>
        PumpItDown,

        /// <summary>Dog the hatch and walk away. Free, and the slowest of all: it burns until it has eaten its
        /// own air, and everything in there burns with it.</summary>
        SealItAndWait,
    }

    /// <summary>How long a fire takes to reach an adjoining space that is standing open to it. Slow enough that
    /// dogging a hatch is a real answer, fast enough that walking the length of the ship to think about it is
    /// not. FLAGGED for the owner's tuning.</summary>
    public const double SpreadSeconds = 30.0;

    /// <summary>How long a SEALED fire takes to eat its own air and go out. This is the price of the free
    /// answer: it works, it needs nothing, and it is the road that loses the most cargo.</summary>
    public const double SmotherSeconds = 120.0;

    /// <summary>How long a fire needs to ruin what is in a compartment. Deliberately shorter than
    /// <see cref="SmotherSeconds"/>: sealing a fire and waiting always works, and always costs you the
    /// contents.</summary>
    public const double RuinSeconds = 75.0;

    /// <summary>Whether a space can burn at all. No air, no fire — which is the whole mechanic in four
    /// words.</summary>
    public static bool CanBurn(in HullVenting.Space space) => !space.Vented;

    /// <summary>Whether a fire in this space is out. A vented compartment is out INSTANTLY: the valve is the
    /// fast answer precisely because vacuum does not negotiate.</summary>
    public static bool IsOut(in HullVenting.Space space, double sealedSeconds) =>
        space.Vented || (space.DoorShut && sealedSeconds >= SmotherSeconds);

    /// <summary>How much of a compartment's contents a fire has eaten, 0…1. Clamped, so a fire left burning
    /// forever ruins everything in the room and nothing beyond it.</summary>
    public static double RuinFraction(double burningSeconds) =>
        System.Math.Clamp(burningSeconds / RuinSeconds, 0, 1);

    /// <summary>
    /// What is left of a wreck's assessed value once fire has eaten part of her.
    ///
    /// <para>Pure, and priced per COMPARTMENT: each one holds an equal share of what she is worth, so a fire in
    /// one of eight spaces can cost you an eighth and no more. That keeps the stake legible at the board — the
    /// captain can look at what is burning and know roughly what it is costing per minute — and it keeps a
    /// long fire from zeroing a hull the player never even reached.</para>
    /// </summary>
    public static int ValueAfterFire(int assessedValue, int compartments, double ruinedShares)
    {
        if (assessedValue <= 0 || compartments <= 0)
        {
            return System.Math.Max(0, assessedValue);
        }

        double perShare = (double)assessedValue / compartments;
        double lost = System.Math.Clamp(ruinedShares, 0, compartments) * perShare;
        return (int)System.Math.Round(System.Math.Max(0, assessedValue - lost));
    }

    // ── What it says ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The board's tag for a burning compartment. It counts UP, like the vacuum clock, because the
    /// question it answers is "how much of this have I already lost".</summary>
    public static string Tag(double burningSeconds) =>
        $"FIRE {HullVenting.SoakLabel(burningSeconds)}";

    /// <summary>What the captain is told the first time they find one aboard. Names the three roads without
    /// recommending one, because the choice between them is the mechanic.</summary>
    public const string FoundLine =
        "🔥 There is fire in her. Forty years, and a pocket of her atmosphere was still shut in with something " +
        "that would burn. It spreads through every hatch left open — and it dies three ways: crack the valve " +
        "and lose the air, put it on the pumps and lose the time, or dog the hatch and lose whatever is in " +
        "there while it eats its own air.";

    /// <summary>When it reaches the next compartment, because a hatch was open.</summary>
    public static string SpreadLine(string into) =>
        $"🔥 It is through into {into}. Every hatch you left open is a road it already knows.";

    /// <summary>When the valve answers.</summary>
    public static string VentedOutLine(string room) =>
        $"💨 {room} opens to space and the fire goes out between one breath and the next. Vacuum does not " +
        "negotiate — and her air is gone with it.";

    /// <summary>When the pump finally wins.</summary>
    public static string PumpedOutLine(string room) =>
        $"🛢 {room} comes down to nothing and the fire dies with the pressure. Slower than the valve, and her " +
        "air is in the tanks instead of in the dark.";

    /// <summary>When a sealed compartment finishes eating its own air.</summary>
    public static string SmotheredLine(string room) =>
        $"🔥 {room} has burned out behind its own hatch. Nothing in there needed you to do anything — which is " +
        "exactly what it cost.";

    /// <summary>What walking into it is like. Not a refusal: a captain in a suit may absolutely walk into a
    /// burning compartment, and should be told what that means rather than prevented.</summary>
    public const string WalkingIntoItLine =
        "🔥 The suit's outer layer starts complaining the moment you cross the coaming. You can be in here. You " +
        "cannot be in here for long.";

    /// <summary>What it costs to stand in one, per second, in nerve pips — the #480 seam. A fire is frightening
    /// before it is dangerous, which is the right order for this game.</summary>
    public const double NervePerSecondInside = 1.5;

    /// <summary>And the honest summary the salvage screen owes the captain when the numbers are short.</summary>
    public static string CostLine(int assessedValue, int afterFire)
    {
        int lost = System.Math.Max(0, assessedValue - afterFire);
        return lost <= 0
            ? "Nothing of hers burned while you were aboard."
            : $"🔥 The fire ate {lost:N0} cr of her while you were deciding what to do about it.";
    }
}
