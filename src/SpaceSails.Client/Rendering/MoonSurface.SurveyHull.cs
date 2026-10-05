using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// #1074 beat 5 · THE CHARTER HULL'S PLACE ON A PRESERVED GROUND. Core owns the geometry
// (ReturningShuttle.Park); this file asks it the one question only the renderer can answer — what is
// already standing on this ground — by reading the SAME memoised layout the deck is built from, so the
// answer cannot drift from the picture. It builds nothing and caches nothing of its own.
public static partial class MoonSurface
{
    /// <summary>#1074 beat 5 · Where the charter hull stands on this ground, or null when the ground is not
    /// fenced (no head, no fence, no hull) or has nowhere to put her. Deterministic in its arguments.</summary>
    public static ReturningShuttle.Hull? SurveyHullOn(
        string bodyId, string bodyDisplayName,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)> ownCaches,
        string siteSalt, string siteName, long monolithEpoch, bool hasSecretSite)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (!hasSecretSite || !PreservationZone.On(bodyId))
        {
            return null;
        }

        ownCaches ??= [];
        siteSalt ??= "";
        siteName ??= "";
        SurfaceDeckKey key = SurfaceDeckKey.For(
            bodyId, bodyDisplayName, ownCaches, siteSalt, monolithEpoch, hasSecretSite, preserved: true);
        Layout layout = _layoutCache.GetOrBuild(key, () => BuildLayout(
            bodyId, bodyDisplayName, ownCaches, siteSalt, siteName, monolithEpoch, hasSecretSite, true));

        SurfaceLayout.Field field = ExpeditionField();
        LiftHeadBox head = LiftHead(bodyId, siteSalt, field);
        PreservationZone.Fence fence = PreservationZone.FenceAround(head.Hut, field);

        // WHAT THE DECK APPENDS LATER, seeded and so knowable now: the secret lab's chamber (and its hidden
        // door) and the home tile's outpost hut. They are not in the memoised layout, so a hull placed against
        // the layout alone could be parked across either. The player's OWN marks are deliberately left out:
        // they move, and the hull must stay where she was put.
        var later = new List<(double X0, double Y0, double X1, double Y1)>();
        SecretLab.Placement lab = SecretLab.OnThisSite(bodyId, siteSalt, field, forcePresent: true);
        SecretLab.Region chamber = SecretLab.Build(bodyId, field, lab.DoorX, lab.DoorY);
        later.Add((chamber.MinX - 1, chamber.MinY - 1, chamber.MaxX + 1, chamber.MaxY + 1));
        later.Add((lab.DoorX - 2, lab.DoorY - 2, lab.DoorX + 2, lab.DoorY + 2));
        SurfaceOutpost.Placement hut = SurfaceOutpost.ForTile(bodyId, siteSalt, SurfaceTiles.Home);
        if (hut.HasOutpost)
        {
            SurfaceOutpost.Region room = SurfaceOutpost.Build(bodyId, siteSalt, hut);
            later.Add((room.MinX - 1, room.MinY - 1, room.MaxX + 1, room.MaxY + 1));
            later.Add((hut.DoorX - 2, hut.DoorY - 2, hut.DoorX + 2, hut.DoorY + 2));
        }

        bool Blocked(double x0, double y0, double x1, double y1)
        {
            foreach ((double lx0, double ly0, double lx1, double ly1) in later)
            {
                if (x0 <= lx1 && x1 >= lx0 && y0 <= ly1 && y1 >= ly0)
                {
                    return true;
                }
            }

            foreach (DeckPlan.Wall w in layout.Walls)
            {
                if (ReturningShuttle.SegmentHitsRect(w.X1, w.Y1, w.X2, w.Y2, x0, y0, x1, y1))
                {
                    return true;
                }
            }
            foreach (DeckPlan.ConsoleSpot c in layout.Consoles)
            {
                if (c.X >= x0 && c.X <= x1 && c.Y >= y0 && c.Y <= y1)
                {
                    return true;
                }
            }
            foreach ((float lx, float ly, string _) in layout.Labels)
            {
                if (lx >= x0 && lx <= x1 && ly >= y0 && ly <= y1)
                {
                    return true;
                }
            }
            return false;
        }

        return ReturningShuttle.Park(fence.CentreX, fence.CentreY, fence.Radius, field, Blocked);
    }
}
