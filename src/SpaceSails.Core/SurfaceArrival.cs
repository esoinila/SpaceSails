namespace SpaceSails.Core;

/// <summary>
/// #461 · THE LANDING IS NOT AN AMBUSH. Owner, live 2026-07-27, having been jumped on the pad: <i>"That
/// makes no sense… how did they know the shuttle would land just there."</i> … <i>"I had to use a sentry to
/// even get out the door."</i>
///
/// <para>And the law behind it, in his words: <i>"The reevers only get excited when they spot a non-reever.
/// So ship in itself does not attract them. They expect it is their ship. The player walking around or
/// digging around gets their attention. There should be at minimum a timer of player getting out of the ship
/// before the first reever spots them. Also there should always be one un-paid-for sentry at the door."</i></para>
///
/// <para>Two things follow. A hull setting down is not news to the Old Ones — the deep is full of hulls, and
/// they take it for one of their own. It is the WARM BODY walking out of it that is news. So the ground gets
/// a <see cref="SpotGraceSeconds"/> grace from the moment the shuttle mates: no Old One may notice the
/// captain, by eye or by ear, until it has passed. Long enough to get out, look around and decide — never so
/// long that the deep becomes safe.</para>
///
/// <para>And the door is never unwatched: a house sentry stands at the tube mouth on every landing. It is not
/// bought, not carried, and not counted against the sling — the shuttle's own fixture, so that walking out is
/// always possible even for a captain who brought nothing.</para>
/// </summary>
public static class SurfaceArrival
{
    /// <summary>How long after the shuttle mates before ANY Old One may notice the captain. FLAGGED for
    /// tuning — this is the whole "let me get out of the door" dial.</summary>
    public const double SpotGraceSeconds = 20.0;

    /// <summary>May a Reever notice the captain yet? False during the grace, whatever it sees or hears.
    /// Pure so the arrival law is pinned without any client dependency.</summary>
    public static bool CanBeSpotted(double secondsSinceLanding) =>
        !double.IsNaN(secondsSinceLanding) && secondsSinceLanding >= SpotGraceSeconds;

    /// <summary>Seconds of grace still owed — for the client's own countdown line, so the player can SEE
    /// the quiet they have been given rather than guessing at it. 0 once the ground is live.</summary>
    public static double GraceLeft(double secondsSinceLanding) =>
        double.IsNaN(secondsSinceLanding) ? 0 : System.Math.Max(0, SpotGraceSeconds - secondsSinceLanding);

    /// <summary>The gun BUILT INTO THE TUBE. Owner, 2026-07-27: <i>"there should always be one un-paid-for
    /// sentry at the door"</i> … <i>"inside the tube there is always an unlimited ammo sentry built in."</i>
    /// It is the shuttle's own fixture, not the captain's: never bought, never counted against the sling,
    /// never carried home, and never out of rounds. The way back through the door is always covered — you
    /// should not have to spend one of your two bots just to get out and back in.</summary>
    public const string DoorSentryUnit = "GATE-1";

    /// <summary>The built-in gun's magazine. Topped back up after every volley, so it never runs dry — the
    /// readout sits at full and the threshold simply holds.</summary>
    public const int DoorSentryRounds = SentryBot.MaxMagazine;

    /// <summary>Is this the tube's built-in gun rather than one of the captain's? Keeps it out of the
    /// retrieve/abandon ledger at liftoff and out of the "bots you carry" arithmetic.</summary>
    public static bool IsDoorSentry(string unit) =>
        string.Equals(unit, DoorSentryUnit, System.StringComparison.Ordinal);
}
