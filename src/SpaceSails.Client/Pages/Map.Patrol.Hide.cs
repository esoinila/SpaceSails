using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — the PAGE's half of #821's hide: which cubicle the catch is over, which is a door's question and not a round's, and the one line of it a door asks the round for by name.
public sealed partial class Map
{
    // ── #821 · THE HIDE ───────────────────────────────────────────────────────────────────────────────

    /// <summary>#821 · Which cubicle the captain is shut into, or null. One question, off the one set of
    /// shut cells the deck itself is rebuilt from — a second opinion about which doors are over is a second
    /// answer to whether the captain is hidden at all.</summary>
    private (RingOffice.Stall Cell, string Key)? TheCubicleTheCaptainIsShutIn(SurfaceExcursion ex) =>
        CubicleAround(ex) is { Cell: { } cell, Key: { } key } && ex.CubiclesShut.Contains(key)
            ? (cell, key)
            : null;

    // #870 lane 6′c · IT STAYED HERE, and it is on IPatrolHost. It reads the ring's own stalls
    // (CubicleAround) and the excursion's own set of shut ones — a door's machinery, two page members deep
    // — so the round asks for the ANSWER rather than for the sweep. One host member instead of two, which
    // is the whole technique for keeping that interface short.

    /// <inheritdoc cref="Patrol.RememberWhoWatchedTheCatchGoOver"/>
    private void RememberWhoWatchedTheCatchGoOver(IReadOnlyList<SurfaceCollision.Segment> sight) =>
        _patrol.RememberWhoWatchedTheCatchGoOver(sight);
}
