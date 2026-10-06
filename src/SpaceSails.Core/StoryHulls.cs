using System;

namespace SpaceSails.Core;

/// <summary>
/// #1357 · <b>THE STORY-HULL PREDICATE</b> — one question for every hull the story parks on the board as an
/// ordinary depot-shaped <c>NpcState</c> (the charter survey hull, the old ship). They are fixtures: each sits
/// on the traffic board with a cargo class and 0 units, which is exactly the shape the trade list, the hunt
/// booth, the boarding window, the war room, the ordnance loop and the finder all walk. Every one of those
/// verbs asks <see cref="IsOne"/> and leaves the hull out, so the next parked narrative ship is closed to all
/// of them by adding one line here, not by re-opening the holes one verb at a time.
///
/// <para>Because an excluded hull can never be boarded, disabled or sold to, her Boarded/Disabled flags (which
/// are not persisted) have nothing to remember: the "repeats after every reload" hole closes with the
/// boarding path itself.</para>
/// </summary>
public static class StoryHulls
{
    /// <summary>Is this ship id a story hull? Add a story hull's own id test here and nowhere else.</summary>
    public static bool IsOne(string? shipId) =>
        ReturningShuttle.IsTheHull(shipId) || TheOldShip.IsHer(shipId);
}
