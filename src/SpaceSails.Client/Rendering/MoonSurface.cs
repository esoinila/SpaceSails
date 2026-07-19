using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// PR-295 / PR-313 · The walked surface. The shuttle bay (ship's bottom edge — the wild side) grows a
/// DOWN-TUBE to a barren moon surface, welded onto the ship the captain is already standing in (owner:
/// "have the tube with 2 doors appear on the map so I can in this view walk there"). You walk it
/// continuously — bay → dual-door airlock → tube (with a shuttle glyph winking at the abstraction) →
/// surface — no scene switch, no teleport.
///
/// <para>#313 reshaped the surface into a PLACE, not a menu: a wide regolith field whose safe top holds
/// the landing area and a lonely automated kiosk, and whose deep far side holds THE MONOLITH at the
/// heart of a crude maze — prime Old-Ones ground where the cornering loss-condition is real geometry.
/// A visit commits to nothing; the ⛏ dig site only appears when there is a reason to dig (a chest in
/// cargo, or an own cache's ✗ already in the ground). The <c>fillDroids</c> delegate is the caller's so
/// the ship's crew AND the live, converging Old Ones ride the one droid buffer.</para>
/// </summary>
public static class MoonSurface
{
    // The down-tube mouth is the ship's bottom-hull SHUTTLE-BAY HATCH (DeckPlan.ShuttleHatchX1..X2).
    private const float TubeLeft = DeckPlan.ShuttleHatchX1;   // -9
    private const float TubeRight = DeckPlan.ShuttleHatchX2;  // -5
    private const float TubeCenterX = (TubeLeft + TubeRight) / 2f; // -7

    /// <summary>The surface's top rim / tube mouth. The regolith hangs below the ship's bottom hull.</summary>
    public const float SurfaceTopY = -20f;

    // #313: Miranda GREW — a wide field so dig-worthy distance costs commitment (distance = risk).
    /// <summary>The deep edge — the far bottom rim of the field. Lane-1 (owner, 2026-07-18): the tide of
    /// Reevers claws out of the regolith here, "coming from bottom of screen … at random intervals", far
    /// below the followed camera so each contact paints on the tracker long before it crests into view.</summary>
    public const float SurfaceBottomY = -84f;
    private const float SurfaceLeftX = -44f;
    private const float SurfaceRightX = 34f;

    /// <summary>The landing area's safe band just under the tube mouth — tube, kiosk and the way home
    /// cluster here; everything worth digging for is a long walk deeper.</summary>
    public const float LandingBandY = SurfaceTopY - 7f;

    /// <summary>The DEEP COMMITMENT ANCHOR — the heart of the deep field, at the far side. A shared LAW:
    /// every body's geography dresses this spot differently (Miranda's MONOLITH slab, Luna's mass-driver
    /// muzzle, a seeded fixture elsewhere — see <see cref="SurfaceLayout"/>), but the anchor itself is
    /// fixed so the nerve/sight and pack-spawn math is one thing across bodies. Named for Miranda's canon
    /// monolith, which still sits exactly here. TODO(#226): the #318 first-sight sanity hook keys off it.</summary>
    public const float MonolithX = -6f;
    public const float MonolithY = -70f;

    // #313's single fixed ⛏ DIG HERE field (DigFieldX/DigFieldY, deep by the monolith) is RETIRED by the
    // beach-comber kit (owner, Evening wind 2026-07-18: "bury anywhere"). Burying and probing now happen
    // where the captain STANDS — any diggable square (see IsDiggableGround) — so there is no one commitment
    // spot; the whole deep field is fair game, and a swept grid remembers where you've already checked.

    /// <summary>The crew-only threshold (owner): Old Ones are penned on the surface at the tube mouth and
    /// can never climb it — the door won't open to them. Fed to <c>ReeverChase.Step</c>.</summary>
    public const double ReeverBarrierY = SurfaceTopY;

    /// <summary>Lane-1 · The TIDE's northern limit (owner, 2026-07-18): tide Reevers hold the deep and
    /// "will stop venturing too far" toward the landing. Well south of the absolute
    /// <see cref="ReeverBarrierY"/> — so the deep dig-ground floods no matter how many sentries hold a
    /// spot (time there is bounded), while a sightseer up at the landing is never reached. Derived from
    /// the field geometry by the pure <see cref="ReeverTide.HomeRangeY"/> so the law is Core-testable.</summary>
    public static readonly double ReeverTideHomeRangeY = ReeverTide.HomeRangeY(SurfaceTopY, SurfaceBottomY);

    /// <summary>Where a tide Reever claws out for spawn index — a deterministic, seed-jittered x spread
    /// across the deep edge (<see cref="ReeverTide.SpawnX"/>), just inside the bottom rim so it walks the
    /// field rather than piling against the outer wall.</summary>
    public static (double X, double Y) TideSpawnPoint(ulong threatSeed, int spawnIndex) =>
        (ReeverTide.SpawnX(threatSeed, spawnIndex, SurfaceLeftX + 3, SurfaceRightX - 3), SurfaceBottomY + 1.5);

    /// <summary>The avatar's fallback spawn (the excursion keeps the captain where they stood at the bay,
    /// so this is only a safety default).</summary>
    public const double SpawnX = TubeCenterX;
    public const double SpawnY = SurfaceTopY - 1.5;

    /// <summary>True once the digger is back in the tube / aboard — clear of every Old One by the
    /// crew-only-door law. The sprint is won here.</summary>
    public static bool IsSafeAboard(double avatarY) => avatarY > SurfaceTopY;

    /// <summary>#371 Phase 3 · the shared field envelope the surface geography (and now the appended
    /// expedition regions + fog) are laid inside — the same one <see cref="BuildLayout"/> hands to
    /// <see cref="SurfaceLayout.For"/>. Exposed so <c>Map.Surface</c> can resolve expedition door/region
    /// geometry against the identical anchor and bounds.</summary>
    public static SurfaceLayout.Field ExpeditionField() =>
        new(SurfaceLeftX, SurfaceRightX, SurfaceTopY, SurfaceBottomY, LandingBandY, MonolithX, MonolithY);

    /// <summary>The beach-comber kit's "reasonable surface square" test (owner, 2026-07-18: bury/probe
    /// anywhere "outside the landing band / walls"). A spot is diggable when it sits on the open regolith —
    /// deeper than the landing band (so the fused landing pad and the way home stay off-limits) and inside
    /// the field's fenced rim. Wall/maze squares never reach this: the shared collision keeps the avatar
    /// out of them, so a spot the captain can stand on and pass this check is genuine open ground.</summary>
    public static bool IsDiggableGround(double x, double y) =>
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

    private static int StableHash(string s)
    {
        unchecked
        {
            int h = 17;
            foreach (char c in s) h = h * 31 + c;
            return h;
        }
    }

    /// <summary>
    /// Build the ship + dual-door airlock + down-tube + wide barren surface as one continuous walkable
    /// plan. Burying and probing are now free-form (E where you stand — the beach-comber kit), so there is
    /// no fixed ⛏ console; only each own cache's ✗ plants a 🗺 dig console at its recorded spot
    /// (<paramref name="ownCaches"/>). <paramref name="fillDroids"/> and <paramref name="droidCount"/> come
    /// from the caller so the crew and the live Old Ones share one buffer.
    /// </summary>
    public static DeckPlan SurfaceDeck(
        string bodyId,
        string bodyDisplayName,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)> ownCaches,
        int droidCount, Action<double, DeckPlan.Droid[]> fillDroids)
    {
        ArgumentNullException.ThrowIfNull(fillDroids);
        ownCaches ??= [];

        // #371 Phase 1 (perf study, owner-approved 2026-07-19: "Let's go phase one for now"): MEMOIZE the
        // deterministic layout. The study cites SurfaceLayout.For — and with it the whole wall/console/
        // label build — as a pure function of (bodyId, display name, own-cache set). A revisit to a moon
        // with the same buried ✗ set therefore skips the entire ~100-op rebuild and reuses the built
        // arrays. Only the DELEGATE-FREE layout is cached: the droid buffer size and the live fill-droids
        // delegate (bound to the calling game component, and stale across sessions) are re-bound FRESH on
        // every build below, so the cache can never hand back a plan wired to a disposed ship — the one
        // way a shared surface deck could go quietly wrong. Invalidation is honest by construction: any
        // bury / lift / drop that changes the own-cache set changes the key (SurfaceDeckKey), so the ✗
        // marks are never stale.
        SurfaceDeckKey key = SurfaceDeckKey.For(bodyId, bodyDisplayName, ownCaches);
        Layout layout;
        if (!_layoutCache.TryGetValue(key, out layout))
        {
            layout = BuildLayout(bodyId, bodyDisplayName, ownCaches);
            // Cheap unbounded-growth guard: each distinct (body, cache-set) leaves one small entry, and a
            // long game of bury/lift cycles could accumulate stale sets nobody revisits. A generous cap
            // that never trips in normal play keeps the cache from creeping; on overflow we simply start
            // fresh (the next builds re-warm the live grounds).
            if (_layoutCache.Count >= LayoutCacheCap)
            {
                _layoutCache.Clear();
            }
            _layoutCache[key] = layout;
        }

        return new DeckPlan(
            layout.Walls, layout.Consoles, layout.Labels, layout.Backdrops,
            spawnX: SpawnX, spawnY: SpawnY,
            droidCount: droidCount, fillDroids: fillDroids,
            location: layout.Location,
            doors: null, shipFixtures: true, followCam: true, tables: DeckPlan.Ship.Tables);
    }

    // #371 Phase 1 · the memoized, delegate-free layout: everything in a surface deck that is a pure
    // function of the SurfaceDeckKey inputs. The droids (buffer size + fill delegate) are NOT here — they
    // are re-bound on every SurfaceDeck call so a cached layout never captures a component reference.
    private readonly record struct Layout(
        DeckPlan.Wall[] Walls, DeckPlan.ConsoleSpot[] Consoles,
        (float X, float Y, string Text)[] Labels, DeckPlan.Backdrop[] Backdrops,
        Func<double, double, string> Location);

    // WASM is single-threaded, so a plain dictionary is safe. Bounded (see the growth guard above).
    private const int LayoutCacheCap = 64;
    private static readonly Dictionary<SurfaceDeckKey, Layout> _layoutCache = new();

    private static Layout BuildLayout(
        string bodyId,
        string bodyDisplayName,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)> ownCaches)
    {
        DeckPlan ship = DeckPlan.Ship;

        // Start from the ship, minus the sealed bottom-hull hatch (the surface opens it) — the same move
        // the docked complex makes with the top airlock hatch, so the walk grammar is identical.
        var sealedHatch = new DeckPlan.Wall(TubeLeft, DeckPlan.ShuttleHatchY, TubeRight, DeckPlan.ShuttleHatchY, false, true);
        var walls = new List<DeckPlan.Wall>(ship.Walls.Where(w => !w.Equals(sealedHatch)));
        var doors = new List<DeckPlan.Door>(ship.Doors.Where(d => !IsHatchDoor(d)));
        var labels = new List<(float X, float Y, string Text)>(ship.RoomLabels);

        // ── The dual-door airlock + down-tube (owner: "that airlock vibe on the docking... to the shuttle
        //    bay also"). Door / chamber / door, exactly like the topside station tube: two hull walls with
        //    an auto-door at each end. The ship-end door is the crew-only Reever lock. ──
        walls.Add(new(TubeLeft, DeckPlan.ShuttleHatchY, TubeLeft, SurfaceTopY, false, true));
        walls.Add(new(TubeRight, DeckPlan.ShuttleHatchY, TubeRight, SurfaceTopY, false, true));
        doors.Add(new(TubeLeft, DeckPlan.ShuttleHatchY, TubeRight, DeckPlan.ShuttleHatchY));       // ship-end: crew-only door
        doors.Add(new(TubeLeft, SurfaceTopY, TubeRight, SurfaceTopY));                             // surface-end auto-door
        // The shuttle glyph mid-tube — the map winking at its own abstraction (this corridor IS the ride).
        labels.Add((TubeCenterX, (DeckPlan.ShuttleHatchY + SurfaceTopY) / 2f, "🛸"));

        // ── The wide barren field: a fenced rim of hull lines with the tube mouth open at the top. ──
        walls.Add(new(SurfaceLeftX, SurfaceTopY, TubeLeft, SurfaceTopY, false, true));   // top rim, port of the tube
        walls.Add(new(TubeRight, SurfaceTopY, SurfaceRightX, SurfaceTopY, false, true)); // top rim, starboard of the tube
        walls.Add(new(SurfaceLeftX, SurfaceTopY, SurfaceLeftX, SurfaceBottomY, false, true));
        walls.Add(new(SurfaceRightX, SurfaceTopY, SurfaceRightX, SurfaceBottomY, false, true));
        walls.Add(new(SurfaceLeftX, SurfaceBottomY, SurfaceRightX, SurfaceBottomY, false, true));

        // ── The PER-BODY geography (Sunday-morning wind #1–#2): the deep-field ruin/maze walls and the
        //    landmark vary by body — Miranda keeps THE MONOLITH maze (canon), Luna gets the mass-driver
        //    ruins, every other landable body a seeded signature — so no two grounds are the same. The
        //    field envelope above is the shared LAW; only what's inside it is the body's own. Walls are
        //    collision law for everyone (the pure Core SurfaceLayout is where a test pins the geography). ──
        var field = new SurfaceLayout.Field(
            SurfaceLeftX, SurfaceRightX, SurfaceTopY, SurfaceBottomY, LandingBandY, MonolithX, MonolithY);
        SurfaceLayout.Plan layout = SurfaceLayout.For(bodyId, field);
        foreach (SurfaceLayout.Wall w in layout.Walls)
        {
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, w.IsHull));
        }

        var consoles = new List<DeckPlan.ConsoleSpot>(
            ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock))
        {
            // The way home: board the shuttle just off the tube mouth (kept clear of the tube walls). Always here.
            new(DeckPlan.ConsoleKind.SurfaceAirlock, TubeCenterX + 3.5f, SurfaceTopY - 2.5f, "🛸 BOARD THE SHUTTLE"),
            // The lonely automated kiosk — a PLACE has amenities (owner addendum 2). Near the landing, port
            // of the tube. Last restocked before the war.
            new(DeckPlan.ConsoleKind.Kiosk, TubeCenterX - 9f, LandingBandY, "🛒 SOUVENIR KIOSK"),
        };

        // No fixed ⛏ console any more (beach-comber kit): burying is free-form, E where you stand. Only an
        // own cache's ✗ gets a dig console at its mark (contextual 'dig at the X').
        foreach ((string _, double cx, double cy, int _) in ownCaches)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.DigSite, (float)cx, (float)cy, "🗺 DIG AT THE X"));
        }

        labels.Add((TubeCenterX, SurfaceTopY - 3.5f, $"— {bodyDisplayName.ToUpperInvariant()} SURFACE —"));
        foreach (SurfaceLayout.Landmark m in layout.Landmarks)
        {
            labels.Add(((float)m.X, (float)m.Y, m.Label));
        }
        labels.Add((SurfaceRightX - 8, SurfaceBottomY + 3, "REGOLITH · NO ATMOSPHERE"));

        var backdrops = new List<DeckPlan.Backdrop>(ship.Backdrops);

        // The location line is a pure function of (position, bodyDisplayName, layout.Scheme, ship) — all
        // deterministic per body id, none of it component-bound — so the closure is safe to cache.
        Func<double, double, string> location =
            (x, y) => y > DeckPlan.ShuttleHatchY ? ship.Location(x, y)
                    : y > SurfaceTopY ? "DOWN-TUBE (the shuttle ride)"
                    : y > LandingBandY - 2 ? "LANDING AREA"
                    : y < MonolithY + 8 && Math.Abs(x - MonolithX) < 16 ? layout.Scheme
                    : $"{bodyDisplayName.ToUpperInvariant()} SURFACE";

        return new Layout(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(), location);
    }

    // The ship carries one amber shuttle-airlock door across the (bottom) hatch; drop it so the tube's
    // own doors take over the threshold.
    private static bool IsHatchDoor(DeckPlan.Door d) =>
        Math.Abs(d.Y1 - DeckPlan.ShuttleHatchY) < 0.01f && Math.Abs(d.Y2 - DeckPlan.ShuttleHatchY) < 0.01f;
}
