using System;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #679 · WHAT A BODY IS CALLED WHEN THERE IS NO EPHEMERIS IN THE ROOM.
///
/// <para>Most of the game asks the scenario's ephemeris what a body is called, and should — that is the real
/// name, and the client's <c>BodyName</c> reads it. But a handful of pure things in Core hold only an id and
/// still have to print something a captain can read: a mission one-liner on a desk, and now the site
/// designation stamped on an authority card (#679).</para>
///
/// <para>It was a private helper on <see cref="ShipMission"/> for as long as exactly one of those existed.
/// The moment a second caller wanted the same answer it became the shape at the top of
/// <c>docs/features/the-landing-site.md</c> — two sources of truth for one fact — so it moved here and
/// <see cref="ShipMission"/> asks it like everybody else. One copy, read by everyone.</para>
///
/// <para>It is a hyphen-split title-case and nothing cleverer, and it must never become a hardcoded map:
/// bodies are scenario data and a table in Core would go stale the first time somebody added a moon.</para>
/// </summary>
public static class BodyNames
{
    /// <summary>A body id as a display name — <c>"mercury-compute"</c> → <c>"Mercury Compute"</c>. Empty or
    /// missing reads as <c>"?"</c>, because a mission line with a blank in it looks like a bug.</summary>
    public static string Display(string? bodyId)
    {
        if (string.IsNullOrWhiteSpace(bodyId))
        {
            return "?";
        }

        return string.Join(' ', bodyId
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    /// <summary>#679 · The same name in an office's own register — <c>"miranda"</c> → <c>"MIRANDA"</c>. Used
    /// where a facility refers to one of its own sites in print: caps, because everything that office
    /// stamps on a card is caps, and it is a SITE code rather than a place you could fly to.</summary>
    public static string Designation(string? bodyId) => Display(bodyId).ToUpperInvariant();
}
