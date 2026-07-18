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

    /// <summary>THE MONOLITH — the ancients' fixture at the maze heart (worldbuilding ancients thread).
    /// The deep vault: burying here is a long, awful walk out and a worse sprint back. TODO(#226): a
    /// sanity-throw hook fires on first sight of the monolith — not built here (out of scope).</summary>
    public const float MonolithX = -6f;
    public const float MonolithY = -70f;

    /// <summary>Where a NEW chest goes into the ground — deep by the monolith (open floor to its port,
    /// clear of the maze spurs), the commitment spot: a long walk out, a worse sprint back.</summary>
    public const float DigFieldX = MonolithX - 5f;
    public const float DigFieldY = MonolithY;

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
    /// plan. <paramref name="carryingChest"/> grows the ⛏ DIG HERE site (deep, by the monolith);
    /// <paramref name="ownCaches"/> plant a 🗺 dig console at each own cache's ✗. <paramref name="fillDroids"/>
    /// and <paramref name="droidCount"/> come from the caller so the crew and the live Old Ones share one
    /// buffer.
    /// </summary>
    public static DeckPlan SurfaceDeck(
        string bodyDisplayName, bool carryingChest,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)> ownCaches,
        int droidCount, Action<double, DeckPlan.Droid[]> fillDroids)
    {
        ArgumentNullException.ThrowIfNull(fillDroids);
        ownCaches ??= [];
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

        // ── The monolith maze (deep, far side): crude grid corridors so the Old Ones' encirclement is
        //    real geometry. Light by design — a handful of staggered segments, no generator. ──
        AddMonolithMaze(walls, labels);

        var consoles = new List<DeckPlan.ConsoleSpot>(
            ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock))
        {
            // The way home: board the shuttle just off the tube mouth (kept clear of the tube walls). Always here.
            new(DeckPlan.ConsoleKind.SurfaceAirlock, TubeCenterX + 3.5f, SurfaceTopY - 2.5f, "🛸 BOARD THE SHUTTLE"),
            // The lonely automated kiosk — a PLACE has amenities (owner addendum 2). Near the landing, port
            // of the tube. Last restocked before the war.
            new(DeckPlan.ConsoleKind.Kiosk, TubeCenterX - 9f, LandingBandY, "🛒 SOUVENIR KIOSK"),
        };

        // The ⛏ dig site only exists when there is a reason to dig — a chest in cargo to bury (deep, by
        // the monolith — the commitment vault).
        if (carryingChest)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.DigSite, DigFieldX, DigFieldY, "⛏ DIG HERE"));
        }

        // An own cache's ✗ gets a dig console at its mark (contextual 'dig at the X').
        foreach ((string _, double cx, double cy, int _) in ownCaches)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.DigSite, (float)cx, (float)cy, "🗺 DIG AT THE X"));
        }

        labels.Add((TubeCenterX, SurfaceTopY - 3.5f, $"— {bodyDisplayName.ToUpperInvariant()} SURFACE —"));
        labels.Add((MonolithX, MonolithY - 3, "▮ THE MONOLITH"));
        labels.Add((SurfaceRightX - 8, SurfaceBottomY + 3, "REGOLITH · NO ATMOSPHERE"));

        var backdrops = new List<DeckPlan.Backdrop>(ship.Backdrops);

        return new DeckPlan(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(),
            spawnX: SpawnX, spawnY: SpawnY,
            droidCount: droidCount, fillDroids: fillDroids,
            location: (x, y) => y > DeckPlan.ShuttleHatchY ? ship.Location(x, y)
                              : y > SurfaceTopY ? "DOWN-TUBE (the shuttle ride)"
                              : y > LandingBandY - 2 ? "LANDING AREA"
                              : y < MonolithY + 8 && Math.Abs(x - MonolithX) < 16 ? "THE MONOLITH MAZE"
                              : $"{bodyDisplayName.ToUpperInvariant()} SURFACE",
            doors: null, shipFixtures: true, followCam: true, tables: ship.Tables);
    }

    // A crude, cheap maze: three staggered corridor walls around the monolith, gaps offset so the only
    // ways to the heart cut angles — grid geometry the Old Ones exploit to corner a dawdler.
    private static void AddMonolithMaze(List<DeckPlan.Wall> walls, List<(float X, float Y, string Text)> labels)
    {
        const float mzLeft = MonolithX - 18f, mzRight = MonolithX + 18f;
        // Outer maze wall rows (horizontal), each with a single offset gap the walker must find.
        AddGappedRow(walls, mzLeft, mzRight, MonolithY + 12f, gapCenter: MonolithX + 10f, gapHalf: 3f);
        AddGappedRow(walls, mzLeft, mzRight, MonolithY + 6f, gapCenter: MonolithX - 11f, gapHalf: 3f);
        AddGappedRow(walls, mzLeft, mzRight, MonolithY - 4f, gapCenter: MonolithX + 9f, gapHalf: 3f);
        // Two vertical spurs to make dead-ends / cutting lanes.
        walls.Add(new(MonolithX - 6f, MonolithY + 12f, MonolithX - 6f, MonolithY + 6f, false, false));
        walls.Add(new(MonolithX + 4f, MonolithY + 6f, MonolithX + 4f, MonolithY - 4f, false, false));
        // The monolith itself: a short, dark, freestanding slab (a tiny box of walls) at the heart.
        walls.Add(new(MonolithX - 1.2f, MonolithY + 2.5f, MonolithX + 1.2f, MonolithY + 2.5f, false, true));
        walls.Add(new(MonolithX - 1.2f, MonolithY - 2.5f, MonolithX + 1.2f, MonolithY - 2.5f, false, true));
        walls.Add(new(MonolithX - 1.2f, MonolithY + 2.5f, MonolithX - 1.2f, MonolithY - 2.5f, false, true));
        walls.Add(new(MonolithX + 1.2f, MonolithY + 2.5f, MonolithX + 1.2f, MonolithY - 2.5f, false, true));
        _ = labels; // label added by caller
    }

    private static void AddGappedRow(List<DeckPlan.Wall> walls, float x1, float x2, float y, float gapCenter, float gapHalf)
    {
        walls.Add(new(x1, y, gapCenter - gapHalf, y, false, false));
        walls.Add(new(gapCenter + gapHalf, y, x2, y, false, false));
    }

    // The ship carries one amber shuttle-airlock door across the (bottom) hatch; drop it so the tube's
    // own doors take over the threshold.
    private static bool IsHatchDoor(DeckPlan.Door d) =>
        Math.Abs(d.Y1 - DeckPlan.ShuttleHatchY) < 0.01f && Math.Abs(d.Y2 - DeckPlan.ShuttleHatchY) < 0.01f;
}
