namespace SpaceSails.Core;

/// <summary>
/// #653 slice 2 · <b>THE ROUTE TABLE.</b> Every place a captain can stand on foot is routed by the id of the body
/// the excursion is on (plus, for the Hive, which floor): a derelict by <see cref="Derelict.BodyIdPrefix"/>, a dead
/// station by <see cref="StationAboard.BodyIdPrefix"/>, a floor of the Hive by being below the surface, and anything
/// else is regolith. The if-chains that asked this had grown a copy in the frame's deck builder, the boarding, the
/// wreck's hull and five instruments' "is this a hull" guards, and a fourth kind of place made the fourth copy — so the
/// question lives HERE and the callers ask it. No interface and no base class: the house idiom is static layout
/// classes and factory functions, and the routing needs one answer, not an abstraction.
///
/// <para>A PURE MOVE: the order of the questions is the order the client always asked them (a floor below the
/// surface first, then the station, then the wreck, then the ground), and a derelict whose hull the page does not hold
/// is still a <see cref="Kind.Wreck"/> here — it is the CALLER that falls through to the ground when it has no hull to
/// build, exactly as it did.</para>
/// </summary>
public static class SiteRoute
{
    /// <summary>What kind of place an excursion is standing in.</summary>
    public enum Kind
    {
        /// <summary>Regolith: a moon, a rock, an expedition site. The default every other kind is carved out of.</summary>
        Ground = 0,

        /// <summary>A floor of the underground complex, below the surface.</summary>
        Hive = 1,

        /// <summary>A dead station (<see cref="StationAboard.BodyIdPrefix"/>).</summary>
        Station = 2,

        /// <summary>A derelict hull (<see cref="Derelict.BodyIdPrefix"/>).</summary>
        Wreck = 3,
    }

    /// <summary>The place an excursion on <paramref name="bodyId"/>, standing on <paramref name="floor"/> (negative =
    /// below the surface), is in. The one question the deck builder routes on.</summary>
    public static Kind Of(string? bodyId, int floor)
    {
        if (floor < 0)
        {
            return Kind.Hive;
        }

        return Of(bodyId);
    }

    /// <summary>The place a body id names, not counting floors: station, wreck, or ground.</summary>
    public static Kind Of(string? bodyId)
    {
        if (StationAboard.TryParseStationId(bodyId, out _))
        {
            return Kind.Station;
        }

        return Derelict.TryParseWreckId(bodyId, out _) ? Kind.Wreck : Kind.Ground;
    }

    /// <summary>The id of the station a body id names, or null off a station — the id the deck is built from.</summary>
    public static string? StationIdOf(string? bodyId) =>
        StationAboard.TryParseStationId(bodyId, out string id) ? id : null;

    /// <summary>Is this body a dead station?</summary>
    public static bool IsStation(string? bodyId) => Of(bodyId) == Kind.Station;

    /// <summary>Is this body a generated derelict (a wreck id, not the scenario's roadster — see
    /// <see cref="Derelict.IsWreckBody"/> for that wider question)?</summary>
    public static bool IsWreck(string? bodyId) => Of(bodyId) == Kind.Wreck;

    /// <summary>Is this body a HULL — a derelict or a dead station — rather than a ground? The question every "no
    /// regolith here" guard asks (no tide, no shelter, no lab, no regolith card).</summary>
    public static bool IsHull(string? bodyId) => Of(bodyId) is Kind.Station or Kind.Wreck;
}
