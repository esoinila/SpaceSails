namespace SpaceSails.Core;

/// <summary>
/// #327 · The ship calls home. The owner was marooned on Miranda when his mothership's orbit degraded
/// during a surface excursion — and LOVED it as emergent story ("one more reason to hurry back"). His
/// ruling flipped the issue: <b>degradation is a FEATURE</b> (pressure to hurry home); the <b>SILENCE
/// is the bug</b> — "got to get some warning about that before it just happens".
///
/// <para>This is the pure, in-voice instrument that speaks the truth the keeper already lives by
/// (Friday §0, <see cref="OrbitKeeping"/>: the autopilot HOLDS the park with trim burns priced from
/// Lab 25 until the tank can't pay). The keeper holds while the tank pays; the honest maroon happens
/// only when the pulses run out. This clock turns that law into words the captain can read:</para>
/// <list type="bullet">
/// <item><b>The hold clock</b> (<see cref="HoldSeconds"/>): how long the tank can sustain the trim bill
/// — remaining pulses ÷ the Lab 25 trim rate (pulses/sim-day). A free park (no tide bill) never
/// strands. This is the quote before you board down, and the countdown while you're on the ground.</item>
/// <item><b>The escalating ladder</b> (<see cref="StageFor"/>): as the hold erodes the ship calls down —
/// steady → slipping → failing → lost — thresholds are fractions of the hold you boarded with, so the
/// ladder is derived from the real clock and always fires in order before any degradation.</item>
/// <item><b>The comms lines</b> (<see cref="Comms"/>): the in-voice line the surface HUD shows for each
/// rung, never buried, never silent (the #324 HUD-visibility law, pointed at the mothership).</item>
/// </list>
/// The only forbidden outcome is silence: every rung above <see cref="Stage.Lost"/> carries a spoken
/// line, and <see cref="Stage.Lost"/> is itself announced — the maroon stays possible, fair, and loud.
/// </summary>
public static class OrbitHold
{
    /// <summary>One sim-day in seconds — the unit the Lab 25 trim budget is quoted in (pulses/day).</summary>
    private const double DaySeconds = 86400.0;

    /// <summary>How long the tank can sustain station-keeping, in seconds of sim time: the pulses on
    /// hand divided by the trim bill (pulses per sim-day, <see cref="OrbitKeeping.TrimPulsesPerDay"/>).
    /// A park with no tide bill (<paramref name="trimPulsesPerDay"/> ≤ 0) never strands →
    /// <see cref="double.PositiveInfinity"/>; an empty tank holds nothing → 0. This is the SAME law the
    /// keeper spends by, read as a clock: the keeper trims until <c>cost &gt; pulses</c>, so the last
    /// pulse buys the last trim and the hold is honestly pulses ÷ rate.</summary>
    public static double HoldSeconds(int pulsesOnHand, double trimPulsesPerDay)
    {
        if (trimPulsesPerDay <= 0)
        {
            return double.PositiveInfinity;
        }
        if (pulsesOnHand <= 0)
        {
            return 0;
        }
        return pulsesOnHand / trimPulsesPerDay * DaySeconds;
    }

    /// <summary>The rungs of the calling-home ladder, in strict escalation order. The keeper holds the
    /// park while the tank pays, so these are the honest stages of that hold eroding — never a jump
    /// straight to <see cref="Lost"/> without the warnings first (owner's demand).</summary>
    public enum Stage
    {
        /// <summary>The park holds with comfortable margin — the ship is on station.</summary>
        Steady = 0,
        /// <summary>Under two-fifths of the boarding hold left — "the ship is slipping, captain" (amber).</summary>
        Slipping = 1,
        /// <summary>Under the failing floor — "orbit failing — come home NOW" (red, the last call).</summary>
        Failing = 2,
        /// <summary>The hold is spent — the keeper can no longer pay; the orbit degrades honestly (red).</summary>
        Lost = 3,
    }

    /// <summary>Amber rung: the ship reports slipping once the remaining hold falls under this fraction
    /// of the hold the captain boarded down with. A fraction of the real clock, so a long hold warns
    /// with plenty of runway and a short one warns proportionally — the same ladder either way.</summary>
    public const double SlippingFraction = 0.40;

    /// <summary>Red rung: the last-call "come home NOW" fires under this fraction of the boarding hold —
    /// well before the tank is empty, so the captain has time to answer.</summary>
    public const double FailingFraction = 0.15;

    /// <summary>The ladder rung for the live hold, derived from the hold the captain boarded with. As
    /// <paramref name="holdRemainingSeconds"/> falls monotonically from <paramref name="holdAtBoardingSeconds"/>
    /// to 0 it steps Steady → Slipping → Failing → Lost in strict order — the guarantee that the maroon
    /// is never silent. An infinite hold (a free park) is forever <see cref="Stage.Steady"/>; a boarding
    /// hold of 0 (boarded onto an orbit no one is keeping) is already <see cref="Stage.Lost"/> — the
    /// caller shows the "not holding" call instead in that case.</summary>
    public static Stage StageFor(double holdRemainingSeconds, double holdAtBoardingSeconds)
    {
        if (double.IsPositiveInfinity(holdRemainingSeconds))
        {
            return Stage.Steady;
        }
        if (holdRemainingSeconds <= 0 || holdAtBoardingSeconds <= 0)
        {
            return Stage.Lost;
        }
        double frac = holdRemainingSeconds / holdAtBoardingSeconds;
        if (frac <= FailingFraction)
        {
            return Stage.Failing;
        }
        if (frac <= SlippingFraction)
        {
            return Stage.Slipping;
        }
        return Stage.Steady;
    }

    /// <summary>How loudly the surface HUD paints a rung: 0 calm (steady), 1 amber (slipping), 2 red
    /// (failing / lost). The #324 visibility law reads this to colour the comms line.</summary>
    public static int Severity(Stage stage) => stage switch
    {
        Stage.Steady => 0,
        Stage.Slipping => 1,
        _ => 2,
    };

    /// <summary>The in-voice line the ship calls down to the surface for a rung — the comms channel the
    /// captain reads on the ground. Never empty: even <see cref="Stage.Steady"/> speaks (the instrument
    /// is always present, the "fourth timer"), so the ladder can never fall silent between rungs.</summary>
    public static string Comms(Stage stage, double holdRemainingSeconds) => stage switch
    {
        Stage.Steady => $"🛰 the ship holds the orbit — ~{Humanize(holdRemainingSeconds)} on the tank",
        Stage.Slipping => $"🛰 the ship is slipping, captain — orbit holds ~{Humanize(holdRemainingSeconds)} more",
        Stage.Failing => $"⚠ orbit failing — come home NOW (~{Humanize(holdRemainingSeconds)} left)",
        _ => "⚠ the ship has slipped its orbit — it's adrift; the shuttle rides its own way home",
    };

    /// <summary>The call the ship makes when NObody is keeping the orbit — the captain boarded down onto
    /// a park the autopilot never armed, so the tide is stripping it with no trims paid. Not a rung of
    /// the funded-hold ladder (there is no clock to count down); a standing red the moment the excursion
    /// begins, so an unkept excursion is as loud as a dry-tank one — never silent.</summary>
    public const string NotHoldingComms =
        "⚠ the ship is NOT holding this orbit — no one is trimming it; it will drift while you're down";

    /// <summary>The quote the boarding panel states before the captain commits to walking down — in
    /// voice, honest, from the live tank. A kept orbit quotes its hold; an unkept one says so plainly.</summary>
    public static string BoardingQuote(bool orbitKept, double holdSeconds)
    {
        if (!orbitKept)
        {
            return "⚠ the ship is NOT holding this orbit — it will drift while you're down. Arm auto-orbit before you board, or hurry.";
        }
        if (double.IsPositiveInfinity(holdSeconds))
        {
            return "🛰 the ship holds this orbit for free — no tide bill here; take your time.";
        }
        return $"🛰 the ship can hold this orbit ~{Humanize(holdSeconds)} on the tank.";
    }

    /// <summary>A compact, honest duration for a hold clock: minutes under 90 min, hours under two days,
    /// days beyond — so the quote reads "~40 min" when it's tight and "~3 days" when it's roomy, never a
    /// wall of seconds. Non-finite (a free park) reads "indefinitely"; ≤ 0 reads "no time".</summary>
    public static string Humanize(double seconds)
    {
        if (double.IsPositiveInfinity(seconds))
        {
            return "indefinitely";
        }
        if (seconds <= 0)
        {
            return "no time";
        }
        if (seconds < 90 * 60)
        {
            return $"{Math.Max(1, (int)Math.Round(seconds / 60.0))} min";
        }
        if (seconds < 48 * 3600)
        {
            return $"{Math.Round(seconds / 3600.0, 1)} h";
        }
        return $"{Math.Round(seconds / DaySeconds, 1)} days";
    }
}
