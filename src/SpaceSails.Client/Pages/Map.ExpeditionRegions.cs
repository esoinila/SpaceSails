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

    // ── #1063 slice 2 · SEALED ≠ FULL ────────────────────────────────────────────────────────────────────
    //
    // The one seal this captain has already forced and found boringly empty, written down as EmptySeal.Key
    // and persisted per-universe in the vault's ProgressSection. It is the whole of the client's half of the
    // feature, because everything else about it is a pure function in Core.
    //
    // IT IS PERSISTED FOR A HARDER VERSION OF _hallsBuried's REASON. A spend that forgot across a reload
    // would put the cache BACK into a room the captain's own field book says was empty — and the book being
    // the only witness is the whole of this arc — and it would hand him a second empty room later, which is
    // the one thing the spend must never do: two of them is a rate, and a rate is a new pattern to learn.
    //
    // Null until it has been spent, which is most of every voyage and all of most of them.
    private string? _emptySealSpentOn;

    /// <summary>#1063 · <b>THE ONE READER.</b> The chamber behind a forced door, as this captain's world has
    /// it: the authored region, hollowed out where it is the seal he already found empty
    /// (<see cref="EmptySeal.Hollow"/>). Every path that resolves a forced door goes through here — the
    /// compose that replays a site on a revisit, the force itself, the cache claim, the fog, the born-dark
    /// overlay — so a room can never be empty on the plan and full to the cache loop, which is exactly the
    /// shape of bug this ground has shipped before (§13.15: two callers reasoning about one building).
    ///
    /// <para>It asks <see cref="EmptySeal.IsSpentOn"/> and never <see cref="EmptySeal.WouldBeEmpty"/>: what
    /// is written down is the truth, and the nomination is consulted at exactly one moment, the moment a seal
    /// gives (<see cref="ForceOpenDoor"/>). A reader that re-decided would change its mind about a room the
    /// day the captain buried a ground on the other side of the system.</para></summary>
    private ExpeditionRegions.Region RegionOf(
        ExpeditionSiteKind kind, string bodyId, string doorId, in SurfaceLayout.Field field)
    {
        ExpeditionRegions.Region region = ExpeditionRegions.ForceOpen(kind, doorId, field);
        return EmptySeal.IsSpentOn(_emptySealSpentOn, bodyId, doorId) ? EmptySeal.Hollow(region) : region;
    }

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
            AppendRegionGeometry(
                RegionOf(kind, ex.Stop.Body.Id, doorId, field), walls, labels, consoles, ex);
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
        if (_surface is not { Expedition: true } ex || AnySlowThingUnderYourHands)
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

        // ── #1063 slice 2 · AND ONCE, EXACTLY ONCE, IT IS EMPTY ─────────────────────────────────────────
        //
        // The issue, under Scully protection (mandatory): "we SPEND one disappointment on purpose: at least
        // once, the captain forces a sealed thing early and it is exactly, boringly empty — sealed ≠ full —
        // so the pattern never hardens into proof."
        //
        // THE NOMINATION IS ASKED HERE AND NOWHERE ELSE, and the moment it answers yes the key is written
        // down. Every later reading of this room — the compose on a revisit, the cache loop, the fog — asks
        // the written key instead (see RegionOf), so what the captain walked out of is what he walks back
        // into, and no second door can ever become the empty one however long the voyage runs.
        if (EmptySeal.WouldBeEmpty(kind, ex.Stop.Body.Id, doorId, _emptySealSpentOn))
        {
            _emptySealSpentOn = EmptySeal.Key(ex.Stop.Body.Id, doorId);
        }
        bool nothingInIt = EmptySeal.IsSpentOn(_emptySealSpentOn, ex.Stop.Body.Id, doorId);

        ExpeditionRegions.Region region = RegionOf(kind, ex.Stop.Body.Id, doorId, field);
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
        (double X, double Y)? mouth = ExpeditionRegions.DoorPosition(kind, doorId, field);
        if (mouth is { } pos)
        {
            _deckPlan.RemoveConsoleAt((float)pos.X, (float)pos.Y, DeckPlan.ConsoleKind.SealedDoor, 0.2);
        }

        // #1063 · …and the empty one takes none of what follows. No reveal cue, no card, no ring on the fan,
        // no "new ground on the plan" — because a recess with nothing in it is not a discovery, and every one
        // of those channels would be the house insisting it was. It gets the authored line and the book gets
        // it too, filed under the door glyph the seals already wear, and then the game says nothing more
        // about it ever again. The geometry is already appended above: he can walk in and stand in it, which
        // is the entire beat.
        if (nothingInIt)
        {
            RequestVaultSave();   // the spend is written down the moment it is spent
            ShowAndFile(EmptySeal.Line, EmptySeal.Glyph);
            return;
        }

        RendererInterop.PlayCue("reveal");

        // #563 · The FIRST time this ever happens to a captain, the world stops and says what it just did.
        // Owner: "The expanding site, how we tell that story to user clearly (do we have pop-up with image)
        // is of great interest." It did not — the most distinctive thing this game does was announced by a
        // toast that faded, while a scuttled ship got a full card with art. After the first showing the
        // toast IS the right register: you know the rule now, and a card on every door would be a nag.
        //
        // #584 · …and the card said nothing about WHERE, which is the owner's other complaint about this same
        // moment ("I was left totally un-aware what that did and where?"). The mouth was already resolved
        // nine lines up, for the console it takes off the plan; it is handed to the one writer now, which
        // names it on the card and rings it on the fan for the rest of the excursion. A door whose position
        // the site will not resolve appends nothing a captain could walk to, so it is not claimed as ground.
        if (mouth is not { } where)
        {
            return;
        }
        if (!TheGroundJustGrew(ex, where.X, where.Y))
        {
            ShowPulseMessage("⚙ The door gives — cold air that hasn't moved in ten thousand years. New ground on the plan. Step through and look.");
        }
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
            // #1063 · …through the one reader, so the empty seal has no cache HERE either. A loop that asked
            // Core directly would happily bank 900 credits out of a room the plan draws as bare ground.
            ExpeditionRegions.Region region = RegionOf(kind, ex.Stop.Body.Id, doorId, field);
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
        IReadOnlyList<SurfaceCollision.Segment> segs = _deckPlan.CollisionField; // #448: the indexed twin

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
                    ExpeditionRegions.Region rg = RegionOf(kind, ex.Stop.Body.Id, doorId, field);
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
            ExpeditionRegions.Region rg = RegionOf(kind, ex.Stop.Body.Id, doorId, field);
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
