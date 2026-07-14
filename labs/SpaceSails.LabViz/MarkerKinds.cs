namespace SpaceSails.LabViz;

/// <summary>
/// The event-marker kinds the viewer knows how to draw. Each maps to a distinct glyph in
/// <c>viewer.html</c> (<c>MARKER_GLYPH</c>/<c>MARKER_COLOR</c>); a kind outside this set has no glyph,
/// so <see cref="VizScene.AddMarker"/> rejects it rather than letting the viewer fall back silently.
/// </summary>
public static class MarkerKinds
{
    /// <summary>A propulsive burn (launch, TCM). Glyph: ▲.</summary>
    public const string Burn = "burn";

    /// <summary>A gravity-assist flyby / closest approach to a body used as a lever. Glyph: ◆.</summary>
    public const string Flyby = "flyby";

    /// <summary>A closest pass to a destination (e.g. arrival). Glyph: ○.</summary>
    public const string Closest = "closest";

    /// <summary>A generic point event. Glyph: ✦.</summary>
    public const string Event = "event";

    /// <summary>True iff <paramref name="kind"/> is one of the four known marker kinds.</summary>
    public static bool IsKnown(string kind) =>
        kind is Burn or Flyby or Closest or Event;
}
