namespace SpaceSails.Core;

/// <summary>
/// A named surface site on a landable body — the fixed point a treasure map paces off from
/// (#164, #223). The flagship is the Phobos monolith: a genuine ~85 m boulder near the Stickney
/// rim, photographed by Mars Global Surveyor. The house rule (lesson-11/12 framing) holds — the
/// PLACE is real, the deals struck in its shadow are the fiction we build there.
///
/// <para>Landmarks are deliberately a small Core datum, NOT ephemeris bodies: a map needs a name
/// to pace from ("from the monolith, 40 paces anti-spinward"), not another thing on rails that the
/// picker, depots and traffic pipelines must all learn about. Every landable body has one via the
/// generic fallback, so a cache buried anywhere always mints a legible map.</para>
/// </summary>
/// <param name="BodyId">The body this site sits on (e.g. "phobos").</param>
/// <param name="Name">The site's map-facing name, article included ("the monolith").</param>
/// <param name="HeightMeters">The landmark's height for flavor (0 when not a raised feature).</param>
/// <param name="Note">A one-line composition note — doubles as the image-manifest brief for the
/// grok art lane.</param>
public readonly record struct Landmark(string BodyId, string Name, double HeightMeters, string Note);

/// <summary>The catalogue of named landing sites, keyed by body id (#164). One flagship today —
/// the Phobos monolith — plus a generic fallback so every landable body yields a legible map. A
/// plain static registry, the same "one table, not a system" shape as the other Core data rules.</summary>
public static class Landmarks
{
    /// <summary>The Phobos monolith (#164): the 85 m boulder by the Stickney rim, the outer-system's
    /// quiet meeting place for deals struck away from station security.</summary>
    public static readonly Landmark PhobosMonolith = new(
        "phobos", "the monolith", 85.0,
        "an 85 m monolith boulder on grey regolith near the Stickney crater rim, long shadow, deals done in its shade");

    private static readonly Dictionary<string, Landmark> ByBody = new()
    {
        [PhobosMonolith.BodyId] = PhobosMonolith,
    };

    /// <summary>The best named site on a body, or a generic landing-beacon fallback so a map paced
    /// off any landable body still reads honestly. The fallback carries the body id so its map text
    /// and image brief still name the place.</summary>
    public static Landmark For(string bodyId) =>
        ByBody.TryGetValue(bodyId, out Landmark l)
            ? l
            : new Landmark(bodyId, "the landing beacon", 0.0, "the survey landing beacon on open regolith");

    /// <summary>True when a body has a hand-authored flagship landmark (not the generic fallback) —
    /// the fetch-a-cache giver prefers to seed rumours at these.</summary>
    public static bool HasNamedSite(string bodyId) => ByBody.ContainsKey(bodyId);
}
