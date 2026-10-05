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

        bool Blocked(double x0, double y0, double x1, double y1)
        {
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
