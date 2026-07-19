using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.ExpeditionRegions — #371 Phase 3, THE DOOR-OPEN DREAM (owner, cruise 2026-07-19: "I love the idea of
// progress bar of forcing a door to open in expedition and a new space appending to the map"). On an away-
// expedition site the captain finds SEALED DOORS; forcing one (a channeled progress bar, the dig-channel
// idiom) APPENDS a seeded inner chamber to the live surface — walls that are law for everyone, a discovery
// cache, and (bounded to depth 2) maybe a deeper door — WITHOUT a rebuild. The world grows, nobody teleports.
//
// This partial also carries the EXPEDITION FOG OF WAR (owner, same cruise): appended chambers are born dark
// until the captain's line of sight reaches them; an Old One behind cover drops off the walked map (the
// motion tracker still hears it) and leaves a fading "movement was here" echo when it slips from sight. The
// pure rules live in Core (ExpeditionRegions / ExpeditionVisibility); this is the thin client wiring.
public partial class Map
{
    // Reusable HUD buffers (Phase-1 perf discipline): the fog overlays are rebuilt every surface frame, so
    // these instance lists are cleared-and-refilled instead of freshly allocated. Consumed synchronously in
    // the same DrawWalkFrame, exactly like the other _hud* buffers.
    private readonly List<(double X0, double Y0, double X1, double Y1, int State)> _hudDark = [];
    private readonly List<(double X, double Y, double Alpha)> _hudEcho = [];

    // ── The site composition: sealed doors + every already-forced region, replayed onto a freshly-built
    //    base deck (called from RebuildSurfaceDeck for an expedition site). One append on top of the memoized
    //    base — never a regeneration. ──
    private void ComposeExpeditionSite(SurfaceExcursion ex)
    {
        if (!ExpeditionSite.TryParseKind(ex.Stop.Body.Id, out ExpeditionSiteKind kind))
        {
            return;
        }
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();

        var walls = new List<DeckPlan.Wall>();
        var labels = new List<(float X, float Y, string Text)>();
        var consoles = new List<DeckPlan.ConsoleSpot>();

        // The base site's sealed doors — the ones NOT yet forced still stand as consoles.
        foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.OuterDoors(kind, field))
        {
            if (!ex.OpenedDoors.Contains(d.Id))
            {
                consoles.Add(new(DeckPlan.ConsoleKind.SealedDoor, (float)d.X, (float)d.Y, d.Label));
            }
        }

        // Every region already forced open this visit — walls, landmark, and its live interactables (skip a
        // claimed cache and an already-forced nested door).
        foreach (string doorId in ex.OpenedDoors)
        {
            ExpeditionRegions.Region region = ExpeditionRegions.ForceOpen(kind, doorId, field);
            AppendRegionGeometry(region, walls, labels, consoles, ex);
        }

        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), []));
    }

    // Map a Core region's walls/landmark/consoles into the growing deck-region lists, honoring looted caches
    // and forced nested doors.
    private static void AppendRegionGeometry(
        in ExpeditionRegions.Region region,
        List<DeckPlan.Wall> walls, List<(float X, float Y, string Text)> labels,
        List<DeckPlan.ConsoleSpot> consoles, SurfaceExcursion ex)
    {
        foreach (SurfaceLayout.Wall w in region.Walls)
        {
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, w.IsHull));
        }
        foreach (SurfaceLayout.Landmark m in region.Landmarks)
        {
            labels.Add(((float)m.X, (float)m.Y, m.Label));
        }
        foreach (ExpeditionRegions.RegionConsole rc in region.Consoles)
        {
            if (rc.Kind == ExpeditionRegions.RegionConsoleKind.DiscoveryCache && ex.LootedCaches.Contains(rc.Id))
            {
                continue;
            }
            if (rc.Kind == ExpeditionRegions.RegionConsoleKind.SealedDoor && ex.OpenedDoors.Contains(rc.Id))
            {
                continue;
            }
            DeckPlan.ConsoleKind kind = rc.Kind == ExpeditionRegions.RegionConsoleKind.SealedDoor
                ? DeckPlan.ConsoleKind.SealedDoor
                : DeckPlan.ConsoleKind.DiscoveryCache;
            consoles.Add(new(kind, (float)rc.X, (float)rc.Y, rc.Label));
        }
    }

    // ── Forcing a sealed door [E]: a channeled progress bar, abortable by stepping away. ──
    private void SealedDoorInteract()
    {
        if (_surface is not { Expedition: true } ex || ex.AnyChannel)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.SealedDoor } spot)
        {
            return;
        }
        if (!ExpeditionSite.TryParseKind(ex.Stop.Body.Id, out ExpeditionSiteKind kind))
        {
            return;
        }
        string? doorId = DoorIdAt(kind, spot.X, spot.Y);
        if (doorId is null)
        {
            return;
        }
        ex.DoorChannel = new DoorChannel { DoorId = doorId, AnchorX = spot.X, AnchorY = spot.Y };
        RendererInterop.PlayCue("board");
        ShowPulseMessage("⚙ Setting your shoulder to the door… hold position. Ten thousand years of seal — this takes a moment. Step away to abort.");
    }

    private void StepDoorChannel(double dtRealSeconds)
    {
        if (_surface is not { DoorChannel: { } ch } ex)
        {
            return;
        }
        // Away from the door → abort (the same anchor law the dig uses).
        double dx = _avatarX - ch.AnchorX, dy = _avatarY - ch.AnchorY;
        if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
        {
            ex.DoorChannel = null;
            ShowPulseMessage("You step back — the door holds. It stays sealed.");
            return;
        }
        ch.Progress += dtRealSeconds / ExpeditionRegions.DoorForceSeconds;
        if (ch.Progress >= 1.0)
        {
            ex.DoorChannel = null;
            ForceOpenDoor(ex, ch.DoorId);
        }
    }

    // The moment (owner's love): the door gives, and new ground joins the plan — appended live, no rebuild.
    private void ForceOpenDoor(SurfaceExcursion ex, string doorId)
    {
        if (!ExpeditionSite.TryParseKind(ex.Stop.Body.Id, out ExpeditionSiteKind kind))
        {
            return;
        }
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        ExpeditionRegions.Region region = ExpeditionRegions.ForceOpen(kind, doorId, field);
        ex.OpenedDoors.Add(doorId);

        // Append the chamber to the LIVE plan (walls + landmark + interactables) — incremental, rebuild-free.
        var walls = new List<DeckPlan.Wall>();
        var labels = new List<(float X, float Y, string Text)>();
        var consoles = new List<DeckPlan.ConsoleSpot>();
        AppendRegionGeometry(region, walls, labels, consoles, ex);
        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), []));

        // The forced door's own console becomes the open doorway — drop it (only the small console array
        // rebuilds; the geometry just grew).
        if (ExpeditionRegions.DoorPosition(kind, doorId, field) is { } pos)
        {
            _deckPlan.RemoveConsoleAt((float)pos.X, (float)pos.Y, DeckPlan.ConsoleKind.SealedDoor, 0.2);
        }

        RendererInterop.PlayCue("reveal");
        ShowPulseMessage("⚙ The door gives — cold air that hasn't moved in ten thousand years. New ground on the plan. Step through and look.");
    }

    // ── Claiming a discovery cache [E]: bank its bonus to the gig (composed into the payout). ──
    private void DiscoveryCacheInteract()
    {
        if (_surface is not { Expedition: true } ex)
        {
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.DiscoveryCache } spot)
        {
            return;
        }
        if (!ExpeditionSite.TryParseKind(ex.Stop.Body.Id, out ExpeditionSiteKind kind))
        {
            return;
        }
        // Resolve which cache (and its bonus) by the chamber whose cache sits here.
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        foreach (string doorId in ex.OpenedDoors)
        {
            ExpeditionRegions.Region region = ExpeditionRegions.ForceOpen(kind, doorId, field);
            foreach (ExpeditionRegions.RegionConsole rc in region.Consoles)
            {
                if (rc.Kind != ExpeditionRegions.RegionConsoleKind.DiscoveryCache || ex.LootedCaches.Contains(rc.Id))
                {
                    continue;
                }
                double dx = rc.X - spot.X, dy = rc.Y - spot.Y;
                if ((dx * dx) + (dy * dy) <= 0.5)
                {
                    ex.LootedCaches.Add(rc.Id);
                    ex.ExpeditionBonus += region.DiscoveryBonus;
                    _deckPlan.RemoveConsoleAt(spot.X, spot.Y, DeckPlan.ConsoleKind.DiscoveryCache, 0.2);
                    RendererInterop.PlayCue("reveal");
                    ShowPulseMessage($"🗝 A discovery cache — {region.Scheme.TrimStart('▦', '▤', '▥', ' ')} yields its find. +{region.DiscoveryBonus:N0} cr banked to the gig.");
                    return;
                }
            }
        }
    }

    private string? DoorIdAt(ExpeditionSiteKind kind, double x, double y)
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        foreach (ExpeditionRegions.SealedDoor d in ExpeditionRegions.AllDoors(kind, field))
        {
            double dx = d.X - x, dy = d.Y - y;
            if ((dx * dx) + (dy * dy) <= 0.25)
            {
                return d.Id;
            }
        }
        return null;
    }

    // ── The fog of war: born-dark regions, behind-cover contacts, movement echoes. Expedition sites only. ──
    private void StepExpeditionFog(double dtRealSeconds)
    {
        if (_surface is not { Expedition: true } ex)
        {
            return;
        }
        IReadOnlyList<SurfaceCollision.Segment> segs = _deckPlan.CollisionSegments;

        // Contacts: per-frame LOS (they move). A mover that slips behind cover leaves a fading echo.
        foreach (Reever r in _reevers)
        {
            bool vis = ExpeditionVisibility.PointVisible(_avatarX, _avatarY, r.X, r.Y, segs);
            if (r.VisibleOnMap && !vis && MotionTracker.IsMoving(r.Vx, r.Vy))
            {
                AddEcho(ex, r.X, r.Y, SimTime); // it slipped behind cover while moving — leave a ripple
            }
            r.VisibleOnMap = vis;
        }

        // Regions: recompute only on a captain-cell move (cheap cadence, owner's ask). A region seen once
        // stays "explored" (drawn dim); those in sight right now are "visible" (drawn lit).
        (int, int) cell = ExpeditionVisibility.CaptainCell(_avatarX, _avatarY);
        if (ex.LastFogCell != cell)
        {
            ex.LastFogCell = cell;
            ex.VisibleRegions.Clear();
            if (ExpeditionSite.TryParseKind(ex.Stop.Body.Id, out ExpeditionSiteKind kind))
            {
                SurfaceLayout.Field field = MoonSurface.ExpeditionField();
                foreach (string doorId in ex.OpenedDoors)
                {
                    ExpeditionRegions.Region rg = ExpeditionRegions.ForceOpen(kind, doorId, field);
                    if (ExpeditionVisibility.RegionVisible(_avatarX, _avatarY,
                            rg.RevealX, rg.RevealY, rg.MinX, rg.MinY, rg.MaxX, rg.MaxY, segs))
                    {
                        ex.VisibleRegions.Add(doorId);
                        ex.SeenRegions.Add(doorId);
                    }
                }
            }
        }

        // Decay: drop echoes past their life.
        double now = SimTime;
        ex.Echoes.RemoveAll(e => ExpeditionVisibility.EchoAlpha(now - e.Born, ExpeditionVisibility.EchoLifetimeSeconds) <= 0.0);
        _ = dtRealSeconds;
    }

    // A movement echo (bounded list). A fresh echo very close to an existing one just refreshes THAT one (a
    // contact flickering at a wall edge doesn't stipple the ground with a cloud of ripples).
    private const int MaxEchoes = 24;
    private static void AddEcho(SurfaceExcursion ex, double x, double y, double now)
    {
        for (int i = 0; i < ex.Echoes.Count; i++)
        {
            double dx = ex.Echoes[i].X - x, dy = ex.Echoes[i].Y - y;
            if ((dx * dx) + (dy * dy) < 4.0)
            {
                ex.Echoes[i] = (x, y, now); // move to the last-seen spot, fade timer reset
                return;
            }
        }
        if (ex.Echoes.Count >= MaxEchoes)
        {
            ex.Echoes.RemoveAt(0); // oldest out
        }
        ex.Echoes.Add((x, y, now));
    }

    // Build the born-dark / explored overlay for the renderer: each forced chamber's bounds + its state
    // (0 = unseen, 1 = explored, 2 = visible). Only OPENED regions carry a rect (a still-sealed door shows
    // its console, no void).
    private System.Collections.Generic.IReadOnlyList<(double X0, double Y0, double X1, double Y1, int State)> BuildDarkRegions(SurfaceExcursion ex)
    {
        _hudDark.Clear();
        if (!ex.Expedition || ex.OpenedDoors.Count == 0
            || !ExpeditionSite.TryParseKind(ex.Stop.Body.Id, out ExpeditionSiteKind kind))
        {
            return _hudDark;
        }
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        foreach (string doorId in ex.OpenedDoors)
        {
            ExpeditionRegions.Region rg = ExpeditionRegions.ForceOpen(kind, doorId, field);
            int state = ex.VisibleRegions.Contains(doorId) ? 2 : ex.SeenRegions.Contains(doorId) ? 1 : 0;
            _hudDark.Add((rg.MinX, rg.MinY, rg.MaxX, rg.MaxY, state));
        }
        return _hudDark;
    }

    private System.Collections.Generic.IReadOnlyList<(double X, double Y, double Alpha)> BuildEchoes(SurfaceExcursion ex)
    {
        _hudEcho.Clear();
        if (!ex.Expedition)
        {
            return _hudEcho;
        }
        double now = SimTime;
        foreach ((double x, double y, double born) in ex.Echoes)
        {
            double a = ExpeditionVisibility.EchoAlpha(now - born, ExpeditionVisibility.EchoLifetimeSeconds);
            if (a > 0.0)
            {
                _hudEcho.Add((x, y, a));
            }
        }
        return _hudEcho;
    }
}
