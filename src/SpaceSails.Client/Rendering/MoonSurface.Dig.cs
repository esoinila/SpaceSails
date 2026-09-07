using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #681 · THE GROUND YOU CAN DIG, AND WHAT IS BURIED IN IT — the shared field envelope an expedition is
/// laid inside, which floors take a shovel at all, which squares of one are diggable, and where an own
/// cache's ✗ falls.
///
/// <para>A cache's position is DERIVED rather than stored: the record keeps a bearing and a pace count
/// (the words a captain would write down), not a grid point, so the mark is re-derived from a stable
/// hash of its id and a revisit finds it in the same place. Always below the landing band — every
/// chest is a committed walk.</para>
///
/// <para>Split out of <c>MoonSurface.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class MoonSurface
{
    /// <summary>#371 Phase 3 · the shared field envelope the surface geography (and now the appended
    /// expedition regions + fog) are laid inside — the same one <see cref="BuildLayout"/> hands to
    /// <see cref="SurfaceLayout.For"/>. Exposed so <c>Map.Surface</c> can resolve expedition door/region
    /// geometry against the identical anchor and bounds.</summary>
    public static SurfaceLayout.Field ExpeditionField() =>
        // #681 · …and the column the way home stands in, so the generator can keep its buildings off the
        // square a landing puts the captain on. It could not see that spot before and built a hut through it.
        new(SurfaceLeftX, SurfaceRightX, SurfaceTopY, SurfaceBottomY, LandingBandY, AnchorX, AnchorY, SpawnX);

    /// <summary>
    /// #723 · IS THE SHOVEL ONE OF THIS FLOOR'S VERBS AT ALL? Asked before any coordinate, because the
    /// coordinates cannot answer it: a Hive floor deliberately reuses this very envelope — "it is not
    /// beside the field, it is under it" (<see cref="SpaceSails.Core.UndergroundComplex"/>) — so the spine
    /// corridor of B1 carries the same (x, y) as a square of open regolith and clears the rim test below
    /// with room to spare.
    ///
    /// <para>Underground there is no ground. There is a floor somebody invoiced: poured rockcrete inside a
    /// funded facility, with no bedrock a foot down and nothing ever buried through it on any square of any
    /// corridor of the building. The kit's own first-time card draws the line — the shovel is for <i>"out
    /// on the open regolith"</i> — and this is that line written where the verb is chosen.</para>
    ///
    /// <para>The question is the LEVEL and only the level: <c>level &lt; 0</c> is the underground
    /// convention the whole game already speaks (<c>UndergroundComplex.HoldsPressure</c>, <c>IsDark</c>,
    /// <c>MetresDown</c>, and the excursion's own <c>Floor</c>), so nothing new is stored and nothing can
    /// drift out of step with it.</para>
    /// </summary>
    public static bool ShovelWorksOnThisFloor(int level) => level >= 0;

    /// <summary>The beach-comber kit's "reasonable surface square" test (owner, 2026-07-18: bury/probe
    /// anywhere "outside the landing band / walls"). A spot is diggable when it sits on the open regolith —
    /// deeper than the landing band (so the fused landing pad and the way home stay off-limits) and inside
    /// the field's fenced rim. Wall/maze squares never reach this: the shared collision keeps the avatar
    /// out of them, so a spot the captain can stand on and pass this check is genuine open ground.
    ///
    /// <para>#723 · …and it takes the FLOOR, because a spot is a place on a level and never a bare pair of
    /// numbers. A caller cannot ask about ground any more without saying which ground it means, which is
    /// what stops the [E] key, the standing prompt and the key bar from each deciding for
    /// themselves.</para></summary>
    public static bool IsDiggableGround(double x, double y, int level) =>
        ShovelWorksOnThisFloor(level) &&
        y < LandingBandY && y > SurfaceBottomY &&
        x > SurfaceLeftX && x < SurfaceRightX;

    /// <summary>A deterministic surface position for an own cache's ✗ — scattered through the deep field
    /// so revisits find each mark in a stable spot (the record stores bearing/paces text, not a grid
    /// point, so we derive one). Kept below the landing band: every chest is a committed walk.</summary>
    public static (double X, double Y) CachePosition(string cacheId)
    {
        int h = Math.Abs(StableHash(cacheId));
        double x = SurfaceLeftX + 4 + (h % 1000) / 1000.0 * (SurfaceRightX - SurfaceLeftX - 8);
        double y = (SurfaceTopY - 14) - (h / 1000 % 1000) / 1000.0 * (SurfaceBottomY - (SurfaceTopY - 14) + 6) * -1;
        // Clamp into the deep field.
        y = Math.Clamp(y, SurfaceBottomY + 3, SurfaceTopY - 12);
        return (x, y);
    }

    /// <summary>
    /// #650 · THE ✗ MARKS THAT BELONG TO THIS GROUND — the one projection from the hoard to the marks the
    /// surface deck plants, so the map and the shovel can never be built from different questions.
    ///
    /// <para>Two filters, both load-bearing. <b>Ours</b>: a rival's chest we merely hold a map to gets no ✗.
    /// <b>This site</b>: a body offers 2–4 landing sites since #320 and every one of them rebuilds the SAME
    /// local coordinate frame, so a chest buried out on the Wild Plain, filtered by body alone, planted its ✗
    /// at the identical x/y on the Ridge Camp — on ground the captain had never walked, and diggable there.
    /// A null-site (legacy save, rumour map) cache still answers for every ground on its body, so nothing
    /// buried before the field existed becomes unreachable.</para>
    ///
    /// <para>The position is the REAL dug spot when the free-form bury recorded one (playtest bug #5), else
    /// the deterministic <see cref="CachePosition"/> hash-scatter — unchanged.</para>
    /// </summary>
    public static List<(string Id, double X, double Y, int ReeverLevel)> OwnCacheMarks(
        CacheLedger caches, string bodyId, int siteIndex)
    {
        ArgumentNullException.ThrowIfNull(caches);
        var list = new List<(string, double, double, int)>();
        foreach (TreasureCache c in caches.CachesAt(bodyId, siteIndex))
        {
            if (!c.PlayerOwned)
            {
                continue;
            }
            (double x, double y) = CacheSpot(c);
            list.Add((c.Id, x, y, c.ReeverLevel));
        }
        return list;
    }

    /// <summary>WHERE ONE CHEST'S ✗ IS, and the only place that question is answered. The real dug spot when
    /// the free-form bury recorded one (playtest bug #5), else the deterministic
    /// <see cref="CachePosition"/> hash-scatter.
    ///
    /// <para>#316 · It became a function the day a THIRD reader needed it: the hole a rival leaves behind has
    /// to be at the ✗ and not near it, so the mark, the shovel and the robbery all ask this. Three copies of
    /// a projection is how a chest comes to be dug up in a place its map never pointed at.</para></summary>
    public static (double X, double Y) CacheSpot(TreasureCache cache) =>
        cache is { DigX: { } dx, DigY: { } dy } ? (dx, dy) : CachePosition(cache.Id);

    private static int StableHash(string s)
    {
        unchecked
        {
            int h = 17;
            foreach (char c in s) h = h * 31 + c;
            return h;
        }
    }
}
