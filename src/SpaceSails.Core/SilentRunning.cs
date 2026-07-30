namespace SpaceSails.Core;

/// <summary>
/// #538 · RIG FOR SILENT RUNNING. Owner, building the scene where a black-ops team sweeps a hull the captain is
/// hiding inside: <i>"we take our guns and hide to let them pass"</i>, then <i>"the remote to sentries should be
/// in the mobile hud not at captains desk"</i>, then <i>"Maybe small button to open the sentry settings pop-up 😎.
/// Shuttle should also power down to make it less of an anomaly."</i>
///
/// <para>That last sentence is the one that makes this a system rather than a switch. Hiding is not a state of
/// the captain — it is a state of <b>everything the captain brought with them</b>. A folded-up pirate behind a
/// bulkhead is perfectly concealed and perfectly betrayed by a lit shuttle clamped to the lock with its
/// transponder answering and a gun tracking the hatch.</para>
///
/// <para>And it is the same law Lab 43 found for the hull: <b>you are given away by what you EMIT.</b> The lab
/// killed the idea that a charged hull is *seen* — she is heard. This is that finding applied to a boarding: the
/// boat's lights, its transponder, its warm reactor and its automation are all emissions, and an inspection team
/// notices an anomaly long before it notices a man.</para>
///
/// <para><b>Both switches cost something real</b>, which is what keeps this from being a free "win stealth"
/// button: tight guns will not defend you (<see cref="SentryBot.TightIsAlsoUndefendedLine"/>), and a cold boat
/// is not a ride until it has warmed up. Hiding well means being slow to leave, and that is exactly the trade a
/// captain should be making with somebody else's team aboard.</para>
/// </summary>
public static class SilentRunning
{
    /// <summary>
    /// How long a powered-down shuttle takes to be a ride again. Long enough that going dark is a commitment and
    /// running for the boat is a bad plan; short enough that it is not a death sentence when the plan fails.
    /// </summary>
    /// <remarks>FLAGGED for the owner's tuning. Twenty-five seconds is about the walk from amidships to the lock,
    /// which is the honest shape: if you have to run, you arrive at a boat that is still waking up.</remarks>
    public const double SpinUpSeconds = 25.0;

    /// <summary>What the panel says the boat is doing, given how far through the spin-up it is.</summary>
    public static string SpinUpLabel(double secondsLeft) =>
        secondsLeft <= 0
            ? "🛸 Boat warm and answering."
            : $"🛸 Coming up — {HullVenting.SoakLabel(secondsLeft)} before she will fly.";

    /// <summary>Whether the shuttle will actually take the captain anywhere right now.</summary>
    public static bool ReadyToFly(bool poweredDown, double spinUpLeft) => !poweredDown && spinUpLeft <= 0;

    // ── What each switch is for, in the panel's own voice ──────────────────────────────────────────────

    /// <summary>
    /// The heading on the pop-up. The owner named it himself — <i>"Captain's remote"</i> — and the name is the
    /// design: this is a handheld thing a captain carries and thumbs while crouched behind a bulkhead, not a
    /// settings menu on a console somewhere else. What it commands is everything of theirs that is still making
    /// decisions without them.
    /// </summary>
    public const string PanelTitle = "📻 CAPTAIN'S REMOTE — RIG FOR SILENT RUNNING";

    /// <summary>Said once at the top, because the panel exists to make one point: concealment is about the whole
    /// away team, not about where the captain is standing.</summary>
    public const string PanelBlurb =
        "Hiding is not something you are; it is something everything you brought is doing. A folded-up captain " +
        "behind a bulkhead is given away by a lit boat, an answering transponder and a gun that tracks the hatch.";

    /// <summary>The boat going dark.</summary>
    public const string BoatDarkLine =
        "🛸 The boat goes cold: lamps out, transponder silent, the tube gun stood down and the reactor idled to " +
        "nothing anybody sweeps for. She is a shape clamped to a dead hull now — and she is not a ride until she " +
        "has warmed up.";

    /// <summary>…and coming back up.</summary>
    public const string BoatWakingLine =
        "🛸 Bringing her up. Lamps, bus, transponder — in that order, and none of it is quiet.";

    /// <summary>The refusal, when a captain runs for a boat they told to sleep. Not a punishment: the cost of the
    /// thing that hid them.</summary>
    public static string NotARideYetLine(double secondsLeft) =>
        $"🛸 She is still coming up — {HullVenting.SoakLabel(secondsLeft)}. This is what going dark bought you, " +
        "and it is being charged now.";

    /// <summary>The honest summary a captain deserves before they commit to it.</summary>
    public const string WhatItCostsLine =
        "Tight guns will not defend you. A cold boat will not fly you. Everything that makes you hard to find " +
        "makes you slow to leave.";
}
