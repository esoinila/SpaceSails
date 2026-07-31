using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #531 · THE HOP — flying the shuttle from one part of a dead station to another part of the same station.
///
/// <para>Owner: <i>"It is a big change, instead of always going to or from our ship, we go between two or
/// more away places."</i> This is the pure law for the first case of that: the away place you are going to
/// is the one you are already standing on, round the other side of a tube that will not let you through.</para>
///
/// <para><b>Why it needs no ground rebuild.</b> A station is ONE coordinate space — the hub and every arm
/// share a plan. Walking between modules is stopped by walls, not by the map ending. So a hop does not load
/// anything or move you to a new world: it takes time, and then you are standing somewhere the walls would
/// not have let you walk to. That is the same honesty the ship's own down-tube already trades on, where the
/// corridor IS the ride.</para>
///
/// <para><b>THE RULE THIS FILE EXISTS TO HOLD: a relocate is not a rest.</b> If flying between two away
/// places quietly restored nerve, air or ammunition, it becomes the dominant strategy and the whole
/// excursion loop collapses into "hop out whenever it gets hard". The mothership's tube is where a captain
/// is made whole (#562); another away place is just more away. So this returns a duration and a destination
/// and NOTHING ELSE — there is deliberately no field here for anything that could be topped up.</para>
/// </summary>
public static class StationHop
{
    /// <summary>How fast the boat crosses her own hull, in deck units per second. Slow on purpose: the
    /// interesting cost of a hop is the TIME, because nothing that is hunting you pauses while you fly, and
    /// a clock you can feel is what makes routing a decision rather than a menu.</summary>
    public const double CrossingSpeedDu = 6.0;

    /// <summary>The floor on any hop — undocking, standing off, matching, and coming alongside again. Even
    /// the shortest flight round the drum costs this, so hopping is never cheaper than walking a tube that
    /// happens to be intact.</summary>
    public const double CastOffSeconds = 12.0;

    /// <summary>Why a hop may be refused.</summary>
    public enum Refusal
    {
        /// <summary>It is allowed.</summary>
        None,

        /// <summary>You are already there — the module you asked for is one you can already walk to.</summary>
        AlreadyWalkable,

        /// <summary>That module has no serviceable lock. Cut a way in first; the boat cannot open what is
        /// not a door.</summary>
        NeedsACut,
    }

    /// <summary>A quoted hop: whether it is allowed, why not, how long it takes and where it puts you.</summary>
    public readonly record struct Quote(
        Refusal Refused, double Seconds, double LandX, double LandY, string Destination);

    /// <summary>Quote a flight from where the captain is standing to <paramref name="target"/>'s own access.
    ///
    /// <para><paramref name="cutFaces"/> is the set of modules this away team has already cut into. A module
    /// whose lock does not cycle is refused until it is in that set — the boat can come alongside anything,
    /// but it cannot open a hull that has no door. Making one is the away team's job, on foot, with the
    /// tracker running.</para></summary>
    public static Quote QuoteHop(
        string stationId,
        StationWreck.ModuleId standingIn,
        StationWreck.ModuleId target,
        IReadOnlyCollection<StationWreck.ModuleId>? cutFaces = null)
    {
        ArgumentNullException.ThrowIfNull(stationId);

        StationWreck.Module dest = StationWreck.ModuleOf(target);
        StationWreck.Access access = StationWreck.Accesses(stationId).First(a => a.Module == target);
        (double landX, double landY) = StationWreck.SpawnFor(access);

        // Somewhere you could simply walk to is not a flight. Refusing this is a kindness, not a rule
        // lawyer's nicety: a captain who burns two minutes of a live clock flying to a module twenty paces
        // down an intact tube has been let down by the interface, not by their own judgement.
        if (StationWreck.WalkableFrom(stationId, standingIn).Contains(target))
        {
            return new(Refusal.AlreadyWalkable, 0, landX, landY, dest.Name);
        }

        if (access.Kind == StationWreck.AccessKind.CutFace
            && (cutFaces is null || !cutFaces.Contains(target)))
        {
            return new(Refusal.NeedsACut, 0, landX, landY, dest.Name);
        }

        return new(Refusal.None, SecondsBetween(stationId, standingIn, target), landX, landY, dest.Name);
    }

    /// <summary>Every module this captain could fly to right now, with each one's quote — the destination
    /// list a shuttle console shows. Refused destinations are INCLUDED, because "the Foundry needs cutting"
    /// is information a captain plans around and hiding it would make the station unreadable.</summary>
    public static IReadOnlyList<(StationWreck.ModuleId Module, Quote Quote)> Destinations(
        string stationId,
        StationWreck.ModuleId standingIn,
        IReadOnlyCollection<StationWreck.ModuleId>? cutFaces = null)
    {
        ArgumentNullException.ThrowIfNull(stationId);

        return [.. StationWreck.Modules
            .Select(m => m.Id)
            .Where(id => !StationWreck.WalkableFrom(stationId, standingIn).Contains(id))
            .Select(id => (id, QuoteHop(stationId, standingIn, id, cutFaces)))];
    }

    /// <summary>How long the boat takes to get from one module's access to another's — cast-off plus the
    /// distance she actually has to cross, measured access to access around the hull.</summary>
    public static double SecondsBetween(
        string stationId, StationWreck.ModuleId from, StationWreck.ModuleId to)
    {
        ArgumentNullException.ThrowIfNull(stationId);

        IReadOnlyList<StationWreck.Access> ways = StationWreck.Accesses(stationId);
        StationWreck.Access a = ways.First(w => w.Module == from);
        StationWreck.Access b = ways.First(w => w.Module == to);

        double dx = a.X - b.X, dy = a.Y - b.Y;
        return CastOffSeconds + (Math.Sqrt((dx * dx) + (dy * dy)) / CrossingSpeedDu);
    }

    /// <summary>The line the boat says as she lets go. Names the destination and the honest cost, because a
    /// flight that starts a clock must say the clock is starting.</summary>
    public static string CastOffLine(in Quote quote) =>
        $"🛸 Casting off — coming alongside {quote.Destination} in about {quote.Seconds:F0}s. " +
        "Nothing aboard her waits while you fly.";

    /// <summary>What to say when a hop is refused, in the captain's own terms.</summary>
    public static string RefusalLine(in Quote quote, string standingInName) => quote.Refused switch
    {
        Refusal.AlreadyWalkable =>
            $"You can walk to {quote.Destination} from {standingInName}. Save the fuel.",
        Refusal.NeedsACut =>
            $"{quote.Destination} has no lock that will cycle. The boat can come alongside her all day and " +
            "never get you in — somebody has to cut a way, and that somebody is standing here.",
        _ => "",
    };
}
