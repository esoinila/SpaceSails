namespace SpaceSails.Core;

/// <summary>
/// PR-313 · The surface-excursion motion tracker (owner: "let's borrow the motion detector ... to see
/// the Reevers further away, so we don't have to run so fast"). A crude handheld sweep — a generic
/// sci-fi device, no film in the name — that reads MOVEMENT, not presence: it paints a blip for every
/// contact that is actually moving, by bearing and range, including well beyond the visible grid edge,
/// and says nothing at all about a contact holding still. The dread survives the tool: a quiet tracker
/// is not a safe moon, only a patient one.
///
/// <para>Pure and deterministic so the sweep, the motion gate and the ping cadence can all be pinned in
/// tests. Deck units, matching <see cref="OverlayBands"/>-compliant HUD placement the client draws in
/// the corner (never over the surface grid — the dig-channel watch law, #313).</para>
/// </summary>
public static class MotionTracker
{
    /// <summary>Below this speed (deck-units/second) a contact reads as STILL and the tracker is blind
    /// to it — motion only. Small enough that a shambling Old One always paints, large enough that render
    /// jitter never conjures a phantom blip.</summary>
    public const double StillSpeed = 0.15;

    /// <summary>One thing the tracker can see: its bearing (radians, world frame — atan2(dy,dx) from the
    /// captain) and its range (deck units). Only moving contacts are ever emitted.</summary>
    public readonly record struct Blip(double Bearing, double Range);

    /// <summary>An entity the tracker sweeps: where it is and how fast it is going. The client feeds one
    /// per Reever (and per own droid, if any move — it is a motion tracker, not a Reever detector).</summary>
    public readonly record struct Entity(double X, double Y, double Vx, double Vy);

    /// <summary>How fast the blips pulse — the audible/visual cadence that quickens as the nearest mover
    /// closes. The client maps this to a blink rate (and, TODO, an audio ping when an audio system
    /// exists — no audio today).</summary>
    public enum Cadence
    {
        /// <summary>Nothing moving in reach — the tracker rests.</summary>
        Silent,
        /// <summary>A mover far out — a slow, occasional blip.</summary>
        Distant,
        /// <summary>Closing — a steady pulse.</summary>
        Closing,
        /// <summary>On top of you — a frantic pulse.</summary>
        Imminent,
    }

    /// <summary>True if this contact is moving fast enough for the tracker to see it.</summary>
    public static bool IsMoving(double vx, double vy) => (vx * vx) + (vy * vy) > StillSpeed * StillSpeed;

    /// <summary>Read one entity relative to the captain at (<paramref name="originX"/>,
    /// <paramref name="originY"/>): a <see cref="Blip"/> if it is moving, else null (still → invisible).</summary>
    public static Blip? Read(double originX, double originY, in Entity e)
    {
        if (!IsMoving(e.Vx, e.Vy))
        {
            return null;
        }
        double dx = e.X - originX, dy = e.Y - originY;
        return new Blip(System.Math.Atan2(dy, dx), System.Math.Sqrt((dx * dx) + (dy * dy)));
    }

    /// <summary>Sweep every entity and return a blip for each mover, nearest first. Still contacts are
    /// dropped by construction (motion only).</summary>
    public static System.Collections.Generic.IReadOnlyList<Blip> Sweep(
        double originX, double originY, System.Collections.Generic.IEnumerable<Entity> entities)
    {
        System.ArgumentNullException.ThrowIfNull(entities);
        var blips = new System.Collections.Generic.List<Blip>();
        foreach (Entity e in entities)
        {
            if (Read(originX, originY, in e) is { } b)
            {
                blips.Add(b);
            }
        }
        blips.Sort((a, b) => a.Range.CompareTo(b.Range));
        return blips;
    }

    /// <summary>The ping cadence for the nearest moving contact's range, or <see cref="Cadence.Silent"/>
    /// when nothing moves. Bands: ≤6 du → imminent, ≤18 du → closing, farther → distant.</summary>
    public static Cadence CadenceFor(double? nearestRange) => nearestRange switch
    {
        null => Cadence.Silent,
        <= 6.0 => Cadence.Imminent,
        <= 18.0 => Cadence.Closing,
        _ => Cadence.Distant,
    };

    /// <summary>The house-voice readout for the tracker's nearest contact: "movement — 40 du, closing".
    /// <paramref name="closing"/> reflects whether the nearest range is shrinking (client-computed from
    /// the last sweep). No movers → the honest, cold "no movement — for now".</summary>
    public static string Readout(double? nearestRange, bool closing) => nearestRange is { } r
        ? $"movement — {r:F0} du, {(closing ? "closing" : "drifting")}"
        : "no movement — for now";
}
