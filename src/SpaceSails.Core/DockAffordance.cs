using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>The ONE presented state of the ⚓ dock affordance — the single truth the toolbar button
/// and the envelope coaching line both read (#212). The bug this closes: the button used to bind to
/// the raw NEAREST body while the envelope line bound to the nearest DOCKABLE haven, so when a planet
/// momentarily photobombed the approach (always true docking at a station orbiting one) the button
/// vanished while the line still said "hit ⚓ Dock". Selection here is pure and testable.</summary>
public enum DockPhase
{
    /// <summary>Nothing dockable in play — no ⚓ affordance at all.</summary>
    None,

    /// <summary>A dockable haven is the target but the ship is still beyond the envelope — coast
    /// closer. The Nav "Nearest"/destination line coaches the range; no toolbar button yet.</summary>
    Approach,

    /// <summary>Inside the envelope AND matched to within <see cref="DockRule.MatchSpeed"/> — the
    /// plain ⚓ Dock: clamp on now.</summary>
    Clamp,

    /// <summary>Inside the envelope RANGE but too hot to clamp — the ⚓ Match &amp; clamp offer (#213):
    /// pressing it flies the terminal match burn (the same burn the armed autopilot flies) and leaves
    /// the plain clamp. Priced with the same kernel in <see cref="DockAffordance.MatchPulses"/>.</summary>
    MatchClamp,

    /// <summary>Inside the envelope RANGE, too hot, and the terminal match is unaffordable — the door
    /// refuses with the numbers (needed vs aboard). #213's "hopelessly hot" case.</summary>
    TooHot,
}

/// <summary>One candidate haven the dock-target selection considers. Carries the full
/// <see cref="CelestialBody"/> so the terminal-match price reuses <see cref="OrbitRule.ApproachPulseCost"/>
/// verbatim — the exact same kernel the live autopilot loop spends with (no parallel model).</summary>
/// <param name="Body">The station haven (μ=0). Its Id/Name/BodyRadius drive the readout.</param>
/// <param name="Position">The haven's world position this instant.</param>
/// <param name="Velocity">The haven's world velocity this instant (its drift the clamp must match).</param>
/// <param name="IsFocus">True when this haven is the captain's destination or armed target — the
/// focus-first tiebreak that makes the button read the SAME haven the envelope line (OrbitInfo,
/// destination-first) reads.</param>
public readonly record struct DockHaven(
    CelestialBody Body, Vector2d Position, Vector2d Velocity, bool IsFocus);

/// <summary>The resolved one-truth dock affordance for a frame.</summary>
/// <param name="Phase">What to present (see <see cref="DockPhase"/>).</param>
/// <param name="HavenId">The selected haven's id, or null when nothing is in play.</param>
/// <param name="HavenName">The selected haven's name, for the readout.</param>
/// <param name="Distance">Range to the selected haven (m).</param>
/// <param name="RelSpeed">Relative speed to the selected haven (m/s).</param>
/// <param name="MatchPulses">Terminal-match price (pulses) — the cost of the one burn that nulls the
/// drift into the clamp window, from <see cref="OrbitRule.ApproachPulseCost"/>.</param>
/// <param name="Latched">The hysteresis latch state to carry into the next frame (#211).</param>
public readonly record struct DockAffordance(
    DockPhase Phase, string? HavenId, string? HavenName,
    double Distance, double RelSpeed, int MatchPulses, bool Latched)
{
    /// <summary>Nothing to show.</summary>
    public static readonly DockAffordance Hidden = new(DockPhase.None, null, null, 0, 0, 0, false);

    /// <summary>A ⚓ toolbar button should render (enabled, offering a match, or refusing) — every phase
    /// but the coasting ones. The affordance never silently vanishes once the ship is at the berth.</summary>
    public bool ShowButton => Phase is DockPhase.Clamp or DockPhase.MatchClamp or DockPhase.TooHot;

    /// <summary>The ship can clamp on THIS instant — the plain ⚓ Dock is live.</summary>
    public bool CanClampNow => Phase is DockPhase.Clamp;

    /// <summary>The ship is in range but too hot — the ⚓ Match &amp; clamp burn is on offer.</summary>
    public bool NeedsMatch => Phase is DockPhase.MatchClamp;
}

/// <summary>The pure dock-target selection + affordance/latch/quote logic behind the ⚓ Dock button and
/// its envelope line. One truth so text and button can never disagree (#212), latched so it does not
/// flicker as phasing wobbles the relative speed (#211), and quoting the terminal match with the same
/// kernel the autopilot flies (#213). UI-free — the client feeds it candidates and reads the verdict.</summary>
public static class DockAffordanceRule
{
    /// <summary>Hysteresis release speed (#211): the affordance latches on when the ship first drops to
    /// the clamp match speed inside the door, and stays latched — showing the plain clamp or the match
    /// offer as the drift wobbles — until the ship is CLEARLY gone, i.e. the relative speed climbs past
    /// this. Owner's live read: "show me the dock already when rel drops below [8]" and release at ~10.
    /// Entering at 8 (<see cref="DockRule.MatchSpeed"/>) and releasing at 10 covers the corkscrew wobble
    /// without a per-tick blink.</summary>
    public const double ReleaseSpeed = 10_000;

    /// <summary>Hysteresis release range (#211): once latched, the affordance holds until the ship
    /// coasts beyond this multiple of the envelope radius — a margin so the phasing geometry nudging the
    /// range across the door edge does not drop the offer.</summary>
    public const double ReleaseRangeFactor = 1.10;

    /// <summary>Resolve the one-truth dock affordance for the current frame. Selection is focus-first
    /// (destination/armed haven) then nearest dockable haven — the SAME priority the envelope line reads
    /// — so the toolbar button and the coaching text never name different havens. Phase, latch, and the
    /// terminal-match quote all follow from <see cref="DockRule"/> and <see cref="OrbitRule"/>.</summary>
    /// <param name="ship">The ship state this instant.</param>
    /// <param name="havens">Every dockable station haven in play (with live position/velocity).</param>
    /// <param name="availablePulses">Reaction mass aboard — the door refuses a match it can't pay for.</param>
    /// <param name="wasLatched">The latch state carried from the previous frame.</param>
    public static DockAffordance Evaluate(
        ShipState ship, IReadOnlyList<DockHaven> havens, int availablePulses, bool wasLatched)
    {
        if (Select(ship, havens) is not { } haven)
        {
            return DockAffordance.Hidden;
        }

        double distance = (ship.Position - haven.Position).Length;
        double relSpeed = (ship.Velocity - haven.Velocity).Length;
        double bodyRadius = haven.Body.BodyRadius;

        bool inRange = distance > bodyRadius && distance <= DockRule.EnvelopeMeters;
        bool canClamp = inRange && relSpeed <= DockRule.MatchSpeed; // == DockRule.InEnvelope

        // Hysteresis latch (#211): engage the instant the ship is truly clampable; hold it across the
        // phasing wobble; release only when the ship is clearly gone — inside the body, over the release
        // speed, or well beyond the door.
        bool clearlyOut = distance <= bodyRadius
            || relSpeed > ReleaseSpeed
            || distance > DockRule.EnvelopeMeters * ReleaseRangeFactor;
        bool latched = canClamp || (wasLatched && !clearlyOut);

        // Price the terminal match with the SAME kernel the live autopilot spends: a μ=0 station has no
        // Hill sphere and no obstacle, so this is the one Approach burn that nulls the drift into the
        // clamp window (the owner's 57 p for 10.5 km/s).
        int matchPulses = OrbitRule.ApproachPulseCost(ship, haven.Position, haven.Velocity, haven.Body, null, 0);

        DockPhase phase =
            canClamp ? DockPhase.Clamp
            : (inRange || latched) ? (matchPulses <= availablePulses ? DockPhase.MatchClamp : DockPhase.TooHot)
            : haven.IsFocus ? DockPhase.Approach
            : DockPhase.None;

        return new DockAffordance(
            phase, haven.Body.Id, haven.Body.Name, distance, relSpeed, matchPulses, latched);
    }

    /// <summary>#204/#186 — the autopilot completes the clamp itself only for an HONEST arrival: the
    /// armed destination is a dock haven and nothing about the errand is hostile-flagged. A felony keeps
    /// the captain's-word grammar (#178): hostile-flagged anything NEVER auto-docks. Pure so the boundary
    /// is unit-testable.</summary>
    public static bool ShouldAutoDock(bool armedTargetIsDockHaven, bool hostileFlagged) =>
        armedTargetIsDockHaven && !hostileFlagged;

    /// <summary>Focus-first dock-target selection: the captain's destination/armed haven wins (so the
    /// button reads the same haven the envelope line does), else the nearest dockable haven. Returns null
    /// when there are no candidates.</summary>
    private static DockHaven? Select(ShipState ship, IReadOnlyList<DockHaven> havens)
    {
        DockHaven? nearest = null;
        double best = double.MaxValue;
        foreach (DockHaven h in havens)
        {
            if (h.IsFocus)
            {
                return h;
            }

            double d2 = (ship.Position - h.Position).LengthSquared;
            if (d2 < best)
            {
                best = d2;
                nearest = h;
            }
        }

        return nearest;
    }
}
