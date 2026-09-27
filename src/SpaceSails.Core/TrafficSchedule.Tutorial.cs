using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE TUTORIAL'S TWO PREY — the starter pod and the stubborn freighter, each a pure function of the
/// player's own start, with the constants that place them.
///
/// <para>Split out of <c>TrafficSchedule.cs</c> under #251 as a pure move: one contiguous run of the base
/// file, no member renamed, re-scoped or re-ordered. The run holds only <c>const</c>s, which are folded
/// into their uses at compile time, so no file order can initialise one late.</para>
/// </summary>
public static partial class TrafficSchedule
{
    /// <summary>Abeam offset of the starter pod from the player — a short intercept the player can
    /// close with one plotted burn; well inside sensor reach, outside the instant boarding window.</summary>
    public const double StarterPodStandoffMeters = 1.5e9;

    /// <summary>Cross-drift of the starter pod relative to the player — comfortably under
    /// <see cref="CaptureRule.MaxRelativeSpeed"/>, so closing the distance is the only task.</summary>
    public const double StarterPodDriftMetersPerSecond = 2000;

    /// <summary>
    /// A guaranteed-catchable tutorial pod, placed relative to the player's own start so the "first
    /// hunt" is always deliverable regardless of seed luck (docs/MondayPonder/UIUsabilityNotes.md:
    /// interplanetary traffic runs 80–160 km/s relative, far past the 5 km/s boarding limit — the
    /// only catch from a standing start is a ship still co-moving with a body you share). This is
    /// that ship: a compute-core canister just flung from Luna's driver, still drifting abeam in
    /// Earth's neighbourhood at a couple of km/s — a fresh departure caught before it built transfer
    /// speed. Pure function of the player state, so client and (future) server agree.
    /// </summary>
    public static NpcShip StarterPod(ShipState player)
    {
        // Offset perpendicular to the player's velocity: the pod sits abeam a short hop away and
        // drifts slowly further off, so the player must plot a real (but small) intercept to close.
        Vector2d along = player.Velocity.Normalized();
        var abeam = new Vector2d(-along.Y, along.X);
        Vector2d position = player.Position + abeam * StarterPodStandoffMeters;
        Vector2d velocity = player.Velocity + abeam * StarterPodDriftMetersPerSecond;
        var state = new ShipState(position, velocity, player.SimTime);

        return new NpcShip(
            StarterPodId, "Sitting Duck", "Compute cores", "luna", "venus",
            RoutePersonality.Economical, player.SimTime, player.SimTime, state,
            ManeuverPlan.Empty, player.SimTime + 60 * Day,
            CargoUnits: 5, ManeuverBudget: 0, IsPod: true);
    }

    /// <summary>Ship id of the first-hunt pod — a stable handle so the hunt picker can (re)seed her.</summary>
    public const string StarterPodId = "pod-starter";

    /// <summary>Ship id of the second-hunt freighter. Chosen so <see cref="EncounterRule.ComplianceOf"/>
    /// rolls <see cref="ComplianceState.Stubborn"/> at heat 0 — she never heaves to, so the gun is the
    /// only way to take her (docs/MondayPonder: the second hunt teaches firing).</summary>
    public const string StarterFreighterId = "freighter-lark-5";

    /// <summary>Standoff of the tutorial's stubborn He3 freighter — on the opposite beam from the
    /// starter pod so the two hunts don't stack, still inside sensor reach but a real intercept away.</summary>
    public const double StarterFreighterStandoffMeters = 1.2e9;

    /// <summary>Cross-drift of the stubborn freighter — matchable (under
    /// <see cref="CaptureRule.MaxRelativeSpeed"/>), so once her sail is holed she can always be
    /// boarded. The gun's job here is to stop her bolting, not to slow her down.</summary>
    public const double StarterFreighterDriftMetersPerSecond = 2500;

    /// <summary>She's rigged to bolt: this many days after start her drive spins up and she jinks off
    /// her matched course (a perpendicular X-Pilot burn). Hole her sail first and the burn never fires
    /// — she drifts, boardable. Dawdle and you'll have to run her down again.</summary>
    public const double StarterFreighterEvadeDelayDays = 2.0;

    /// <summary>He3 out of the moons — the prize the tutorial's payoff line already promises.</summary>
    public const int StarterFreighterCargoUnits = 12;

    /// <summary>
    /// The second hunt's prey (docs/MondayPonder/UIUsabilityNotes.md — "the gun tutorial"): a stubborn
    /// He3 hauler that will NOT heave to, so a warning shot only makes her call for muscle. She starts
    /// co-moving with the player (matchable, like <see cref="StarterPod"/>) but her plan carries an
    /// escape jink — a perpendicular X-Pilot burn that swings her off the player's matched velocity and
    /// breaks the boarding window. Holing her sail (<c>Disabled</c> → the plan stops stepping) cancels
    /// the jink and leaves her drifting, boardable: the gun is what makes an evasive ship catchable.
    /// A perpendicular Vector burn (not a prograde Factor bolt) is used on purpose so her *speed* stays
    /// matchable — a holed Lark is always recoverable, never a stuck tutorial. Pure function of the
    /// player state, so client and (future) server agree.
    /// </summary>
    public static NpcShip StarterFreighter(ShipState player)
    {
        Vector2d along = player.Velocity.Normalized();
        var abeam = new Vector2d(-along.Y, along.X);
        // Opposite beam from the starter pod so the two tutorial prey don't overlap on the map.
        Vector2d position = player.Position - abeam * StarterFreighterStandoffMeters;
        Vector2d velocity = player.Velocity - abeam * StarterFreighterDriftMetersPerSecond;
        var state = new ShipState(position, velocity, player.SimTime);

        // The escape jink: one X-Pilot pulse perpendicular to her course. She swings off the player's
        // matched velocity (rel speed jumps past the 5 km/s window) without the runaway prograde speed
        // a Factor burn would build — so she stays close enough to run down and hole even if she bolts.
        double headingRad = Math.Atan2(-abeam.Y, -abeam.X);
        var evade = new ManeuverPlan(
        [
            new ManeuverNode(
                player.SimTime + StarterFreighterEvadeDelayDays * Day,
                ManeuverAction.Accelerate, Pulses: 1, Percent: 20,
                Mode: BurnMode.Vector, HeadingDegrees: headingRad * 180.0 / Math.PI),
        ]);

        return new NpcShip(
            StarterFreighterId, "Nervous Lark", "He3", "titan", "earth",
            RoutePersonality.Economical, player.SimTime, player.SimTime, state,
            evade, player.SimTime + 60 * Day,
            CargoUnits: StarterFreighterCargoUnits, ManeuverBudget: 1.0, IsPod: false);
    }
}
