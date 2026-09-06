namespace SpaceSails.Core;

/// <summary>
/// #488 · THE SOAK — vacuum kills, but not instantly, and the captain is never told which clock they
/// are watching.
///
/// <para>What is growing in a compartment decides how long the door has to stay open: a motile thing
/// goes in twenty seconds, a fibrous mat takes seventy-five, an encysted one takes three minutes. The
/// read (<see cref="Read"/>) reports that something is ALIVE in there, never WHAT — so "is that long
/// enough?" is a judgement made against a clock whose length is a secret.</para>
///
/// <para>Which kind it is, is seeded off the wreck and the compartment, so a reload cannot re-roll it.</para>
/// </summary>
public static partial class HullVenting
{
    // ── The soak: vacuum kills, but not instantly ─────────────────────────────────────────────────────

    /// <summary>What has got into a compartment, and therefore how long it takes the vacuum to finish it.
    /// The captain is never told which of these they are holding: <see cref="Read"/> reports that something
    /// is alive, never what — so "how long is long enough" is a judgement, not a lookup.</summary>
    public enum Infestation
    {
        /// <summary>Nothing in there but forty years of stale air.</summary>
        None,

        /// <summary>The pack itself — warm, moving, lungs. Vacuum takes it quickly.</summary>
        Motile,

        /// <summary>The fibrous growth over the cargo racks. No lungs to lose; it dies of cold and boiling
        /// slowly, from the outside in.</summary>
        Fibrous,

        /// <summary>Encysted. It has done this before and it is in no hurry. If the away team leaves after a
        /// couple of minutes satisfied, this is the one that was still alive when they refilled the room.</summary>
        Encysted,
    }

    /// <summary>How long the vacuum needs, per kind. Tuned to the SHAPE OF A BOARDING rather than to
    /// biology: blowing a hold and standing there watching a clock is not a game, so the long soaks are
    /// long enough that the captain has to go and do something else — read the log, find the manifest — and
    /// come back. That is the point of the counter. FLAGGED for the owner's tuning.</summary>
    public const double MotileSoakSeconds = 20.0;
    public const double FibrousSoakSeconds = 75.0;
    public const double EncystedSoakSeconds = 180.0;

    /// <summary>The vacuum time this kind needs before it is certainly dead.</summary>
    public static double SoakRequired(Infestation kind) => kind switch
    {
        Infestation.Motile => MotileSoakSeconds,
        Infestation.Fibrous => FibrousSoakSeconds,
        Infestation.Encysted => EncystedSoakSeconds,
        _ => 0.0,
    };

    /// <summary>Which kind is in this compartment — seeded off the wreck and the compartment, so a given
    /// hull always holds the same thing in the same place and a reload cannot re-roll it into something
    /// easier. Only an infested compartment holds anything at all.</summary>
    public static Infestation InfestationIn(string wreckId, string compartment, bool infested)
    {
        if (!infested)
        {
            return Infestation.None;
        }

        // Weighted so the hardy one is the minority — but a minority you cannot identify, which is what
        // makes a two-minute soak feel like a decision rather than a formality.
        ulong h = StableHash.Of($"{wreckId}|kind|{compartment}");
        return (h % 6) switch
        {
            0 or 1 or 2 => Infestation.Motile,
            3 or 4 => Infestation.Fibrous,
            _ => Infestation.Encysted,
        };
    }

    /// <summary>Has this compartment been open to space long enough to have finished what was in it?</summary>
    public static bool SoakComplete(in Space space) =>
        space.Vented && space.VacuumSeconds >= SoakRequired(space.Kind);

    /// <summary>The counter on the board — mm:ss, the plainest possible readout. It shows how long the room
    /// has been open, and DELIBERATELY not how long it needs: the whole tension is that the second number
    /// does not exist for the captain.</summary>
    public static string SoakLabel(double vacuumSeconds)
    {
        int total = (int)System.Math.Max(0.0, vacuumSeconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
