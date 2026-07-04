using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>
/// The captain's position (Saturday Plan PR-15, docs/SaturdayPlan/StationDesks.md addendum):
/// the ship's mission — the goal the captain sets for the crew, distinct from the moment-to-moment
/// piloting every other desk does. Pure, deterministic Core type (repo agreement §9): the
/// captain's desk (Pages/Stations/Captain.razor) is the only place that writes it, and every other
/// desk reads <see cref="ShipMission.Describe"/> for its mission summary chip.
/// </summary>
public enum MissionKind
{
    FreeSailing,
    Hunt,
    TradeRun,
    LayLow,
    Survey,
    FlyTo,
}

/// <summary>
/// One ship's mission. Only the fields relevant to <see cref="Kind"/> are populated; the rest stay
/// null. Fields hold scenario body ids (not display names) so a later PR's mission-relevant
/// highlighting (the hunted cargo class, the lay-low haven) can compare directly against ids — see
/// docs/SaturdayPlan/StationDesks.md's addendum. <see cref="Describe"/> humanizes ids for display
/// without needing an ephemeris lookup, so it stays a pure function of the record itself.
/// </summary>
public sealed record ShipMission(
    MissionKind Kind,
    string? TargetCargo = null,
    string? OriginBodyId = null,
    string? DestinationBodyId = null,
    string? HavenBodyId = null,
    string? CorridorA = null,
    string? CorridorB = null)
{
    /// <summary>The mission until the captain sets one (PR-15 work item 3 — the game starts with
    /// no orders given).</summary>
    public static readonly ShipMission Default = new(MissionKind.FreeSailing);

    /// <summary>
    /// The tight one-liner shown as the captain's chip on every other desk, and as the "ship's
    /// articles" headline on the captain's own desk. Matches the addendum's examples exactly:
    /// "Hunt: He3 haulers", "Trade run: Earth → Mars", "Lay low: Enceladus",
    /// "Survey: Saturn–Mars corridor", "Free sailing".
    /// </summary>
    public string Describe() => Kind switch
    {
        MissionKind.Hunt => $"Hunt: {TargetCargo} haulers",
        MissionKind.TradeRun => $"Trade run: {Humanize(OriginBodyId)} → {Humanize(DestinationBodyId)}",
        MissionKind.LayLow => $"Lay low: {Humanize(HavenBodyId)}",
        MissionKind.Survey => $"Survey: {Humanize(CorridorA)}–{Humanize(CorridorB)} corridor",
        MissionKind.FlyTo => $"Make for: {Humanize(DestinationBodyId)} orbit",
        _ => "Free sailing",
    };

    /// <summary>
    /// Turns a scenario body id ("mercury-compute") into a humanized display name ("Mercury
    /// Compute") without an ephemeris lookup — good enough for the tight mission one-liner. Every
    /// id used by <see cref="Describe"/> in practice is a real scenario body id, so this never
    /// needs to be more than a hyphen-split title-case.
    /// </summary>
    private static string Humanize(string? bodyId)
    {
        if (string.IsNullOrEmpty(bodyId))
        {
            return "?";
        }

        string[] words = bodyId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }
}

/// <summary>
/// Generates the SELECTABLE mission options for the captain's desk from a scenario's ephemeris and
/// traffic definition — a pure function, deterministic order (first-seen order walking the
/// scenario's route/body lists; never sorted or shuffled, so two scenarios with identical data
/// always offer identical option lists in identical order). Scenarios without a traffic section
/// (e.g. the Wheel of the World) simply offer no Hunt/Trade run/Survey options; Free sailing (a
/// single fixed choice, not scenario data) and Lay low (havens, read straight off the body list)
/// are unaffected.
/// </summary>
public static class MissionCatalog
{
    /// <summary>One Hunt option per distinct cargo class carried by the scenario's routes.</summary>
    public static IReadOnlyList<ShipMission> HuntOptions(TrafficDefinition? traffic)
    {
        if (traffic is null)
        {
            return [];
        }

        var seen = new HashSet<string>();
        var result = new List<ShipMission>();
        foreach (RouteDefinition route in traffic.Routes)
        {
            if (seen.Add(route.Cargo))
            {
                result.Add(new ShipMission(MissionKind.Hunt, TargetCargo: route.Cargo));
            }
        }

        return result;
    }

    /// <summary>One Trade run option per distinct directed (From, To) route pair.</summary>
    public static IReadOnlyList<ShipMission> TradeRunOptions(TrafficDefinition? traffic)
    {
        if (traffic is null)
        {
            return [];
        }

        var seen = new HashSet<(string From, string To)>();
        var result = new List<ShipMission>();
        foreach (RouteDefinition route in traffic.Routes)
        {
            if (seen.Add((route.From, route.To)))
            {
                result.Add(new ShipMission(MissionKind.TradeRun, OriginBodyId: route.From, DestinationBodyId: route.To));
            }
        }

        return result;
    }

    /// <summary>M26: one Fly to option per orbitable world — planets and moons, not stations
    /// (no Hill sphere to park in) and not the sun (you already orbit it).</summary>
    public static IReadOnlyList<ShipMission> FlyToOptions(IReadOnlyList<CelestialBody> bodies)
    {
        var result = new List<ShipMission>();
        foreach (CelestialBody body in bodies)
        {
            if (body.ParentId is not null && body.Kind != BodyKind.Station)
            {
                result.Add(new ShipMission(MissionKind.FlyTo, DestinationBodyId: body.Id));
            }
        }

        return result;
    }

    /// <summary>One Lay low option per haven body in the scenario.</summary>
    public static IReadOnlyList<ShipMission> LayLowOptions(IReadOnlyList<CelestialBody> bodies)
    {
        var result = new List<ShipMission>();
        foreach (CelestialBody body in bodies)
        {
            if (body.IsHaven)
            {
                result.Add(new ShipMission(MissionKind.LayLow, HavenBodyId: body.Id));
            }
        }

        return result;
    }

    /// <summary>
    /// One Survey option per distinct trade-anchor pair — the same two route endpoints, direction
    /// collapsed ("Saturn"/"Mars" and "Mars"/"Saturn" are the same corridor, ordered ordinally so
    /// the result is stable regardless of which direction a scenario happens to list first).
    /// </summary>
    public static IReadOnlyList<ShipMission> SurveyOptions(TrafficDefinition? traffic)
    {
        if (traffic is null)
        {
            return [];
        }

        var seen = new HashSet<(string A, string B)>();
        var result = new List<ShipMission>();
        foreach (RouteDefinition route in traffic.Routes)
        {
            (string a, string b) = string.CompareOrdinal(route.From, route.To) <= 0
                ? (route.From, route.To)
                : (route.To, route.From);
            if (seen.Add((a, b)))
            {
                result.Add(new ShipMission(MissionKind.Survey, CorridorA: a, CorridorB: b));
            }
        }

        return result;
    }

    /// <summary>
    /// All four scenario-dependent selectable groups, built off one ephemeris in one pass. Free
    /// sailing isn't listed here — it has no scenario-dependent data; the captain's desk renders it
    /// as a single fixed card.
    /// </summary>
    public static MissionOptions Build(ICelestialEphemeris ephemeris) => new(
        HuntOptions(ephemeris.Traffic),
        TradeRunOptions(ephemeris.Traffic),
        LayLowOptions(ephemeris.Bodies),
        SurveyOptions(ephemeris.Traffic),
        FlyToOptions(ephemeris.Bodies));
}

/// <summary>The captain's desk's scenario-dependent selectable groups (see
/// <see cref="MissionCatalog.Build"/>).</summary>
public sealed record MissionOptions(
    IReadOnlyList<ShipMission> Hunt,
    IReadOnlyList<ShipMission> TradeRuns,
    IReadOnlyList<ShipMission> LayLow,
    IReadOnlyList<ShipMission> Survey,
    IReadOnlyList<ShipMission> FlyTo);
