namespace SpaceSails.Core;

/// <summary>
/// The piracy capture rule (plan §M6, revised by the owner's boarding-shuttle design): the
/// mothership doesn't dock — it opens a *window of opportunity* for small boarding craft.
/// Inside <see cref="CaptureRadiusMeters"/> at under <see cref="MaxRelativeSpeed"/> the
/// shuttles can fly, but their transit gets harder the sloppier the pass:
/// <see cref="RequiredSecondsFor"/> scales boarding time with distance and relative speed.
/// A tight rendezvous boards in ~<see cref="BaseBoardingSeconds"/>; a fast drive-by needs a
/// long window that its own geometry rarely grants. Core owns the pure math; callers
/// accumulate progress (client now, server post-M9).
/// </summary>
public static class CaptureRule
{
    /// <summary>Shuttle operating range. Wider than a boarding tube — the mothership can
    /// stand off ~a lunar distance and still fly craft across.</summary>
    public const double CaptureRadiusMeters = 5e8;

    /// <summary>Max relative speed at which shuttles can chase the target down. One ±10%
    /// pulse at interplanetary speed is a ~3 km/s quantum; shuttles forgive a bit more than
    /// one quantum of mismatch — but they pay for it in transit time.</summary>
    public const double MaxRelativeSpeed = 5000;

    /// <summary>Boarding time for a perfect match at point-blank range.</summary>
    public const double BaseBoardingSeconds = 30;

    /// <summary>Relative speed that doubles shuttle transit time.</summary>
    public const double RelativeSpeedPenalty = 1500;

    /// <summary>Stand-off distance that doubles shuttle transit time.</summary>
    public const double DistancePenalty = 2e8;

    /// <summary>Kept for HUD scale/legacy callers: the nominal window length.</summary>
    public const double RequiredSeconds = 60;

    public static bool IsInWindow(ShipState player, ShipState target) =>
        (player.Position - target.Position).LengthSquared <= CaptureRadiusMeters * CaptureRadiusMeters
        && (player.Velocity - target.Velocity).LengthSquared <= MaxRelativeSpeed * MaxRelativeSpeed;

    /// <summary>Whether a boarding may PROCEED this instant (#177/#178). Boarding is a felony, and
    /// the owner got robbed-by-accident when autopilot flew him through a moon and a selected depot
    /// slid into the window — so proximity ALONE must never board.</summary>
    public enum BoardingIntent
    {
        /// <summary>No boardable target in the window — nothing to decide.</summary>
        NoWindow,

        /// <summary>In the window, but the captain has not declared hostile intent on THIS target:
        /// an opportunity to surface, never an act to commit.</summary>
        Opportunity,

        /// <summary>The captain has explicitly authorized boarding THIS target — the felony is
        /// deliberate, and the shuttles may fly.</summary>
        Authorized,
    }

    /// <summary>
    /// The hostile-intent gate: only when the captain's authorization names the SAME target that is
    /// in the window does boarding proceed (<see cref="BoardingIntent.Authorized"/>). In the window
    /// without that word is an <see cref="BoardingIntent.Opportunity"/> — the caller may offer it,
    /// but must not accrue a single tick of boarding. This is the structural fix for auto-piracy:
    /// no input of proximity alone can ever return <see cref="BoardingIntent.Authorized"/>.
    /// </summary>
    public static BoardingIntent EvaluateBoarding(bool inWindow, string? targetId, string? authorizedTargetId) =>
        !inWindow || targetId is null ? BoardingIntent.NoWindow
        : authorizedTargetId is not null && authorizedTargetId == targetId ? BoardingIntent.Authorized
        : BoardingIntent.Opportunity;

    /// <summary>The result of resolving whether to surface the plunder OFFER this frame, honouring a
    /// stand-down: <see cref="Offer"/> is whether to show the prompt, and <see cref="DeclinedTargetId"/>
    /// is the memory the caller should carry into the next frame (re-armed as needed).</summary>
    public readonly record struct PlunderPrompt(bool Offer, string? DeclinedTargetId);

    /// <summary>
    /// Decide whether a plunder opportunity should be OFFERED, given a possible earlier stand-down.
    /// The every-frame capture tick re-evaluates the same geometry, so a bare "dismiss" would be
    /// immortal — the prompt would re-appear the very next frame while the hull is still in the
    /// window. So a decline is remembered per-hull: while THAT hull is the one on offer the prompt
    /// stays silent, but the memory lapses the moment the intent leaves the window (the pass ended)
    /// or a DIFFERENT hull comes on offer (a new selection) — so a fresh pass, or a new target, may
    /// offer again, while the same continuous pass never nags. Pure, so the state machine is tested
    /// in Core rather than inferred from the razor tick.
    /// </summary>
    public static PlunderPrompt ResolvePlunderPrompt(BoardingIntent intent, string? targetId, string? declinedTargetId)
    {
        // A standing decline suppresses the offer only while the SAME hull is the one in the window.
        if (intent == BoardingIntent.Opportunity && declinedTargetId is not null && declinedTargetId == targetId)
        {
            return new PlunderPrompt(Offer: false, DeclinedTargetId: declinedTargetId);
        }

        // Anything else — no window, an authorized boarding, or a different hull on offer — re-arms:
        // the decline lapses, and a genuine opportunity (a new/undeclined hull) is offered.
        return new PlunderPrompt(Offer: intent == BoardingIntent.Opportunity, DeclinedTargetId: null);
    }

    /// <summary>
    /// Sim seconds of continuous window the shuttles need at this instant's geometry.
    /// Tight+slow ≈ 30 s; at the envelope's sloppy corner (5 km/s, 5e8 m) ≈ 455 s — more
    /// window than a straight-line drive-by through the envelope can provide, so genuine
    /// flybys still fail; a rough-but-honest pass succeeds where docking never would.
    /// </summary>
    public static double RequiredSecondsFor(ShipState player, ShipState target)
    {
        double distance = (player.Position - target.Position).Length;
        double relativeSpeed = (player.Velocity - target.Velocity).Length;
        return BaseBoardingSeconds
            * (1 + relativeSpeed / RelativeSpeedPenalty)
            * (1 + distance / DistancePenalty);
    }
}

/// <summary>What a fence pays per unit, by cargo class. He3 is the prize; pods are milk runs.</summary>
public static class CargoMarket
{
    public static int UnitValue(string cargoClass) => cargoClass switch
    {
        "He3" => 1200,              // the prize — scooped at the giants, fought over in the Hormuz corridors
        "Deuterium slush" => 900,   // the giants' other fusion draught, refined at the ring stations (#291)
        "Reactor mass" => 800,      // fuel & shielding for the outpost fusion plants (#291)
        "Mining rigs" => 550,       // capital gear inbound to the new extraction setups (#291)
        "Compute cores" => 400,
        "Habitat structurals" => 350, // fabricated hab sections for the Europa mega-works (#291)
        "Alloys" => 300,
        "Machinery" => 250,
        "Ice" => 100,
        _ => 50,
    };
}
