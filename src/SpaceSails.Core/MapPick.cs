namespace SpaceSails.Core;

/// <summary>
/// #402 follow-up — the click-pick hit-radius rule, settled in Core and pinned by tests. A body
/// answers a click within a forgiving screen radius: its drawn disc, floored so a pinprick world
/// stays tappable and capped so a zoomed-in one doesn't swallow every camera drag, but never tighter
/// than the caller's base tap radius.
///
/// The deflection gig's acceptance bar rides here too: the inbound THREAT ROCK must ALWAYS be one
/// click away, even though its own disc is a pinprick sitting a few pixels off the crowded station
/// knot — so it gets a widened, zoom-independent tolerance. Pure (pixel sizes in, a hit radius out),
/// so the Map picker measures the click distance against a rule the tests own rather than an inline
/// formula duplicated across the picker's body/haven/station passes.
/// </summary>
public static class MapPick
{
    /// <summary>The drawn disc is floored to this many pixels so pinprick worlds stay tappable.</summary>
    public const double MinBodyHitPx = 14;

    /// <summary>…and capped here so a zoomed-in world doesn't swallow every drag on the screen.</summary>
    public const double MaxBodyHitPx = 80;

    /// <summary>The deflection threat rock's generous, zoom-independent pick tolerance: a click
    /// anywhere on the tight cluster lands it, so it's never a pixel-hunt during the gig.</summary>
    public const double ThreatRockRadiusPx = 44;

    /// <summary>A body's pick radius: its drawn disc clamped to [<see cref="MinBodyHitPx"/>,
    /// <see cref="MaxBodyHitPx"/>], but never tighter than <paramref name="baseRadiusPx"/>.</summary>
    public static double BodyHitRadiusPx(double drawnPx, double baseRadiusPx) =>
        Math.Max(Math.Clamp(drawnPx, MinBodyHitPx, MaxBodyHitPx), baseRadiusPx);

    /// <summary>The deflection threat rock's pick radius: a body's radius, widened to
    /// <see cref="ThreatRockRadiusPx"/> so the inbound rock is always selectable in the cluster.</summary>
    public static double ThreatRockHitRadiusPx(double drawnPx, double baseRadiusPx) =>
        Math.Max(BodyHitRadiusPx(drawnPx, baseRadiusPx), ThreatRockRadiusPx);
}
