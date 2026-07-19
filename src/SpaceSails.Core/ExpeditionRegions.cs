using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #371 Phase 3 — THE DOOR-OPEN DREAM. The owner's cruise ruling (2026-07-19, verbatim): <b>"I love the
/// idea of progress bar of forcing a door to open in expedition and a new space appending to the map."</b>
/// On an away-expedition site the captain finds SEALED DOORS; forcing one open (a channeled progress bar,
/// abortable, watched — the dig-channel idiom) APPENDS a new interior region to the live surface: walls
/// that are law for everyone, a discovery cache, and — bounded to depth 2 — maybe a deeper sealed door.
///
/// <para>This is the pure, deterministic Core spine (repo law §9 — determinism is law in Core): the
/// authored per-<see cref="ExpeditionSiteKind"/> door layout and the geometry each door appends, laid
/// inside the shared <see cref="SurfaceLayout.Field"/> envelope exactly like every other scheme so the
/// edge lanes stay open and the way down is never sealed. The client (<c>MoonSurface</c> /
/// <c>Map.Surface</c>) maps these onto its live <c>DeckPlan</c> via an APPEND — no full rebuild — so the
/// world grows and nobody teleports (the study's law: "the world grows, nobody teleports").</para>
///
/// <para>Every region is a pure function of (kind, doorId, field): the same struck site always forces the
/// same ground. Depth is capped at <see cref="MaxDepth"/> — a depth-1 room may hold one nested door; the
/// depth-2 room it opens holds none.</para>
/// </summary>
public static class ExpeditionRegions
{
    /// <summary>Real seconds to force a sealed door — the channeled progress bar (owner's "progress bar of
    /// forcing a door"). A touch longer than a dig (<c>DigChannelSeconds</c> 3.6) so wrenching a door sealed
    /// ten thousand years reads as heavier work. Abortable by stepping away; on-site the event clock ticks
    /// on through it (the watch is the gameplay). OWNER-TUNABLE.</summary>
    public const double DoorForceSeconds = 5.0;

    /// <summary>Discovery credits a depth-1 chamber's cache banks to the gig when the captain claims it —
    /// composed into the payout through the existing <see cref="ExpeditionReward"/> discovery-bonus channel
    /// (banked to the excursion's running bonus). OWNER-TUNABLE.</summary>
    public const int DiscoveryBonusDepth1 = 900;

    /// <summary>The deeper chamber pays richer — going down into a depth-2 vault is the bigger risk and the
    /// bigger find. OWNER-TUNABLE.</summary>
    public const int DiscoveryBonusDepth2 = 1800;

    /// <summary>The hard depth cap (owner: "bounded depth 2"). A depth-1 room may seed ONE nested door; the
    /// depth-2 room it opens seeds none — the append can never runaway.</summary>
    public const int MaxDepth = 2;

    /// <summary>Half the sealed doorway's width in deck units — the gap left in a chamber wall the captain
    /// (and the Old Ones) walk through once it is forced. ~3.2 du wide, matching the ship's door law.</summary>
    private const double DoorwayHalf = 1.6;

    /// <summary>The kind of interactable a forced region carries. Kept a Core enum (no client dependency);
    /// the client maps each onto its own <c>DeckPlan.ConsoleKind</c>.</summary>
    public enum RegionConsoleKind
    {
        /// <summary>A discovery cache — press E to bank the chamber's discovery bonus to the gig.</summary>
        DiscoveryCache,

        /// <summary>A deeper sealed door — force it (another channel) to append the depth-2 region.</summary>
        SealedDoor,
    }

    /// <summary>One sealed door on a site: its stable id (the append/opened-state key), where its console
    /// sits on the ground, its house-voice label, and its depth (1 = on the base site, 2 = nested in a
    /// depth-1 room).</summary>
    public readonly record struct SealedDoor(string Id, double X, double Y, string Label, int Depth);

    /// <summary>One interactable inside a forced region — a cache to claim or a nested door to force.</summary>
    public readonly record struct RegionConsole(RegionConsoleKind Kind, string Id, double X, double Y, string Label, int Depth);

    /// <summary>The geometry a forced door appends to the live map: the chamber walls (collision law for
    /// everyone), its landmark label(s), the interactables inside (a cache, maybe a nested door), the
    /// discovery bonus its cache carries, the axis-aligned bounds (for the fog-of-war "born dark" overlay
    /// and the tests), and the reveal sample point (the chamber's heart — seen only through the doorway).</summary>
    public readonly record struct Region(
        string DoorId,
        string Scheme,
        int Depth,
        IReadOnlyList<SurfaceLayout.Wall> Walls,
        IReadOnlyList<SurfaceLayout.Landmark> Landmarks,
        IReadOnlyList<RegionConsole> Consoles,
        int DiscoveryBonus,
        double MinX, double MinY, double MaxX, double MaxY,
        double RevealX, double RevealY);

    // ── The authored per-kind door specs. OuterDoors and ForceOpen both read this ONE table, so the base
    //    site's sealed doors and the geometry they append can never disagree. Positions are anchor-relative
    //    (the field's deep-commitment anchor) so a scheme's ground and its doors move together. Every room
    //    is placed in a hand-verified OPEN pocket beside the kind's authored features and clamped well
    //    inside the field's safe span, so nothing overlaps existing geometry or the kept-open edge lanes. ──
    private readonly record struct DoorSpec(
        string Id, double Ox, double Oy, double DirX, double DirY,
        double Depth, double Width, int Level, string Scheme, int Bonus, string? NestedId);

    private static IReadOnlyList<DoorSpec> Specs(ExpeditionSiteKind kind) => kind switch
    {
        ExpeditionSiteKind.CrashedHull =>
        [
            new("wreck-a", 8, 6, 1, 0, 10, 10, 1, "▤ THE INTACT HOLD", DiscoveryBonusDepth1, "wreck-a2"),
            new("wreck-a2", 18, 6, 1, 0, 6, 8, 2, "▤ THE INNER HOLD", DiscoveryBonusDepth2, null),
            new("wreck-b", -6, 6, -1, 0, 9, 9, 1, "▤ THE SEALED LOCKER", DiscoveryBonusDepth1, null),
        ],
        ExpeditionSiteKind.SealedTunnel =>
        [
            new("tunnel-a", 7, 8, 1, 0, 10, 10, 1, "▥ THE DEEP VAULT", DiscoveryBonusDepth1, "tunnel-a2"),
            new("tunnel-a2", 17, 8, 1, 0, 6, 8, 2, "▥ THE INNER VAULT", DiscoveryBonusDepth2, null),
            new("tunnel-b", -7, 8, -1, 0, 9, 9, 1, "▥ THE TOMB ANNEX", DiscoveryBonusDepth1, null),
        ],
        _ => // MysticalRuins — a stone gallery under the henge
        [
            new("ruins-a", 13, 0, 1, 0, 10, 10, 1, "▦ THE STONE GALLERY", DiscoveryBonusDepth1, "ruins-a2"),
            new("ruins-a2", 23, 0, 1, 0, 6, 8, 2, "▦ THE INNER GALLERY", DiscoveryBonusDepth2, null),
            new("ruins-b", -13, 0, -1, 0, 9, 9, 1, "▦ THE SIDE VAULT", DiscoveryBonusDepth1, null),
        ],
    };

    /// <summary>The sealed doors visible on the base site of <paramref name="kind"/> — the depth-1 doors the
    /// captain finds and can force. (Nested depth-2 doors are not here; they appear only inside a forced
    /// depth-1 room.)</summary>
    public static IReadOnlyList<SealedDoor> OuterDoors(ExpeditionSiteKind kind, in SurfaceLayout.Field field)
    {
        var list = new List<SealedDoor>();
        foreach (DoorSpec s in Specs(kind))
        {
            if (s.Level == 1)
            {
                list.Add(new SealedDoor(s.Id, field.AnchorX + s.Ox, field.AnchorY + s.Oy, "⚙ SEALED DOOR", 1));
            }
        }
        return list;
    }

    /// <summary>Every sealed door of <paramref name="kind"/> — depth-1 (on the base site) AND depth-2
    /// (nested in a depth-1 room). The client uses this to resolve a door console's ground position back to
    /// its id when the captain forces it.</summary>
    public static IReadOnlyList<SealedDoor> AllDoors(ExpeditionSiteKind kind, in SurfaceLayout.Field field)
    {
        var list = new List<SealedDoor>();
        foreach (DoorSpec s in Specs(kind))
        {
            string label = s.Level == 1 ? "⚙ SEALED DOOR" : "⚙ SEALED DOOR (deeper)";
            list.Add(new SealedDoor(s.Id, field.AnchorX + s.Ox, field.AnchorY + s.Oy, label, s.Level));
        }
        return list;
    }

    /// <summary>The region door <paramref name="doorId"/> of <paramref name="kind"/> appends when forced.
    /// Pure and deterministic; clamped inside the field's safe span. Unknown ids return an empty region
    /// (defensive — a stale id never crashes the append).</summary>
    public static Region ForceOpen(ExpeditionSiteKind kind, string doorId, in SurfaceLayout.Field field)
    {
        foreach (DoorSpec s in Specs(kind))
        {
            if (s.Id == doorId)
            {
                return Build(s, kind, field);
            }
        }
        return new Region(doorId, "", 0, [], [], [], 0, 0, 0, 0, 0, 0, 0);
    }

    /// <summary>The ground position of any door's console — outer OR nested — for the client to place and,
    /// on force, remove it. Unknown ids return null.</summary>
    public static (double X, double Y)? DoorPosition(ExpeditionSiteKind kind, string doorId, in SurfaceLayout.Field field)
    {
        foreach (DoorSpec s in Specs(kind))
        {
            if (s.Id == doorId)
            {
                return (field.AnchorX + s.Ox, field.AnchorY + s.Oy);
            }
        }
        return null;
    }

    private static Region Build(in DoorSpec s, ExpeditionSiteKind kind, in SurfaceLayout.Field field)
    {
        double cx = field.AnchorX + s.Ox, cy = field.AnchorY + s.Oy; // the doorway centre (near face)
        double dx = s.DirX, dy = s.DirY;                             // unit axis into the room
        double px = -dy, py = dx;                                    // perpendicular (the wall width axis)
        double half = s.Width / 2.0;

        // The four room corners: near face at the door, far face `Depth` in along dir.
        double nearLx = cx + px * half, nearLy = cy + py * half;
        double nearRx = cx - px * half, nearRy = cy - py * half;
        double farCx = cx + dx * s.Depth, farCy = cy + dy * s.Depth;
        double farLx = farCx + px * half, farLy = farCy + py * half;
        double farRx = farCx - px * half, farRy = farCy - py * half;

        var walls = new List<SurfaceLayout.Wall>();
        // Two side walls (hull — solid, opaque cover).
        walls.Add(new(nearLx, nearLy, farLx, farLy, true));
        walls.Add(new(nearRx, nearRy, farRx, farRy, true));
        // The near face, split into two stubs leaving the doorway gap at the door centre.
        walls.Add(new(nearLx, nearLy, cx + px * DoorwayHalf, cy + py * DoorwayHalf, true));
        walls.Add(new(nearRx, nearRy, cx - px * DoorwayHalf, cy - py * DoorwayHalf, true));
        // The far face: solid, unless this room seeds a nested door — then leave a matching doorway gap.
        if (s.NestedId is null)
        {
            walls.Add(new(farLx, farLy, farRx, farRy, true));
        }
        else
        {
            walls.Add(new(farLx, farLy, farCx + px * DoorwayHalf, farCy + py * DoorwayHalf, true));
            walls.Add(new(farRx, farRy, farCx - px * DoorwayHalf, farCy - py * DoorwayHalf, true));
        }

        // The chamber's heart — the landmark label and the discovery cache both sit here; the reveal sample
        // (fog-of-war) is here too, so the room stays dark until the captain sees INTO it through the doorway.
        double heartX = cx + dx * (s.Depth / 2.0), heartY = cy + dy * (s.Depth / 2.0);

        var consoles = new List<RegionConsole>
        {
            new(RegionConsoleKind.DiscoveryCache, s.Id + "-cache", heartX, heartY, "🗝 DISCOVERY CACHE", s.Level),
        };
        if (s.NestedId is not null)
        {
            // The nested sealed door sits in the far-wall gap — force it to append the depth-2 room beyond.
            consoles.Add(new(RegionConsoleKind.SealedDoor, s.NestedId, farCx, farCy, "⚙ SEALED DOOR (deeper)", 2));
        }

        var marks = new List<SurfaceLayout.Landmark> { new(heartX, heartY, s.Scheme) };

        // Axis-aligned bounds for the born-dark overlay + the tests (min/max over the room corners).
        double minX = System.Math.Min(System.Math.Min(nearLx, nearRx), System.Math.Min(farLx, farRx));
        double maxX = System.Math.Max(System.Math.Max(nearLx, nearRx), System.Math.Max(farLx, farRx));
        double minY = System.Math.Min(System.Math.Min(nearLy, nearRy), System.Math.Min(farLy, farRy));
        double maxY = System.Math.Max(System.Math.Max(nearLy, nearRy), System.Math.Max(farLy, farRy));

        _ = kind;
        return new Region(s.Id, s.Scheme, s.Level, walls, marks, consoles, s.Bonus,
            minX, minY, maxX, maxY, heartX, heartY);
    }
}
