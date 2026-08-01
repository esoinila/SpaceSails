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
    // #573 · THE FIELD GREW, roughly four times in each direction and sixteen in area. Owner, walking it:
    // "the site cannot just run out so soon... there needs to be explorable space around that building we
    // go to refill." He was right, and it was worse than aesthetics: at 78 x 64 du the walk home from
    // ANYWHERE was under ten seconds, so the suit's point-of-no-return could never fire at any tank size
    // and air was a pure countdown (#573). A tether needs room to pull against.
    // #573 · Read from Core's SurfaceLayout.DefaultField, NOT declared here. These used to be the master
    // copy, with the tests and labs each keeping a hand-made duplicate — so growing the field shipped a new
    // world while the audits went on checking the old one, and passing.
    public static readonly float SurfaceBottomY = (float)SurfaceLayout.DefaultField.BottomY;
    private static readonly float SurfaceLeftX = (float)SurfaceLayout.DefaultField.LeftX;
    private static readonly float SurfaceRightX = (float)SurfaceLayout.DefaultField.RightX;

    /// <summary>The landing area's safe band just under the tube mouth — tube, kiosk and the way home
    /// cluster here; everything worth digging for is a long walk deeper.</summary>
    public const float LandingBandY = SurfaceTopY - 7f;

    /// <summary>The DEEP COMMITMENT ANCHOR — the heart of the deep field, at the far side. A shared LAW:
    /// every body's geography dresses this spot differently (Miranda's MONOLITH slab, Luna's mass-driver
    /// muzzle, a seeded fixture elsewhere — see <see cref="SurfaceLayout"/>), but the anchor itself is
    /// fixed so the nerve/sight and pack-spawn math is one thing across bodies. Named for Miranda's canon
    /// monolith, which still sits exactly here. TODO(#226): the #318 first-sight sanity hook keys off it.</summary>
    public static readonly float MonolithX = (float)SurfaceLayout.DefaultField.AnchorX;
    public static readonly float MonolithY = (float)SurfaceLayout.DefaultField.AnchorY;

    // #313's single fixed ⛏ DIG HERE field (DigFieldX/DigFieldY, deep by the monolith) is RETIRED by the
    // beach-comber kit (owner, Evening wind 2026-07-18: "bury anywhere"). Burying and probing now happen
    // where the captain STANDS — any diggable square (see IsDiggableGround) — so there is no one commitment
    // spot; the whole deep field is fair game, and a swept grid remembers where you've already checked.

    /// <summary>The crew-only threshold (owner): Old Ones are penned on the surface at the tube mouth and
    /// can never climb it — the door won't open to them. Fed to <c>ReeverChase.Step</c>.</summary>
    public const double ReeverBarrierY = SurfaceTopY;

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

    /// <summary>#562 · Standing INSIDE the down-tube itself — past the surface-end door, not yet through
    /// the ship-end one. The umbilical between the regolith and the shuttle bay, three deck units wide.
    ///
    /// <para>This is where the ship rearms you. Owner, playtesting: <i>"I expect them to be reloaded at
    /// that tube I was at."</i> It is the right place for it, and not only because he said so: the tube is
    /// already an airlock where only one door stands open at a time, already the barrier the Old Ones
    /// visibly stop at, and already covered by the shuttle's own built-in gun. Resupplying in the one spot
    /// that is genuinely safe is the whole shape of a retreat.</para>
    ///
    /// <para>Note this is the SHIP-TO-SURFACE tube, which exists on every excursion — not the station
    /// gangway in <c>HavenInterior</c>, which only exists while clamped onto a haven. That distinction is
    /// what makes the supply line universal: a rock you flew to yourself has an anchor exactly as much as a
    /// moon you shuttled to from a berth.</para></summary>
    public static bool IsInDownTube(double avatarX, double avatarY) =>
        avatarY > SurfaceTopY && avatarY <= DeckPlan.ShuttleHatchY
        && avatarX > TubeLeft && avatarX < TubeRight;

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
        int droidCount, Action<double, DeckPlan.Droid[]> fillDroids,
        string siteSalt = "", string siteName = "")
    {
        ArgumentNullException.ThrowIfNull(fillDroids);
        ownCaches ??= [];
        siteSalt ??= "";
        siteName ??= "";

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
        SurfaceDeckKey key = SurfaceDeckKey.For(bodyId, bodyDisplayName, ownCaches, siteSalt);
        Layout layout;
        if (!_layoutCache.TryGetValue(key, out layout))
        {
            layout = BuildLayout(bodyId, bodyDisplayName, ownCaches, siteSalt, siteName);
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
            // #465: hand the tube's doors to the plan. `doors: null` here is what made the airlock invisible.
            doors: layout.Doors, shipFixtures: true, followCam: true, tables: DeckPlan.Ship.Tables,
            scenery: layout.Scenery);
    }

    // #371 Phase 1 · the memoized, delegate-free layout: everything in a surface deck that is a pure
    // function of the SurfaceDeckKey inputs. The droids (buffer size + fill delegate) are NOT here — they
    // are re-bound on every SurfaceDeck call so a cached layout never captures a component reference.
    private readonly record struct Layout(
        DeckPlan.Wall[] Walls, DeckPlan.ConsoleSpot[] Consoles,
        (float X, float Y, string Text)[] Labels, DeckPlan.Backdrop[] Backdrops,
        Func<double, double, string> Location,
        // #465: the tube's TWO DOORS. They were built here and then dropped on the floor — the memoized
        // layout (#371 Phase 1) never carried them and SurfaceDeck passed `doors: null`, so the tube has
        // been drawn WIDE OPEN since the day it was written. That is the "the door that does not open for
        // them is MISSING" report, and why the Old Ones appeared to halt at nothing.
        DeckPlan.Door[] Doors,
        // #563: the terrain layer — drawn, never collided. Memoised alongside the walls because it is a
        // pure function of the same key, and regenerating a dozen craters per frame would be silly.
        SurfaceScenery.Mark[] Scenery);

    // WASM is single-threaded, so a plain dictionary is safe. Bounded (see the growth guard above).
    private const int LayoutCacheCap = 64;
    private static readonly Dictionary<SurfaceDeckKey, Layout> _layoutCache = new();

    private static Layout BuildLayout(
        string bodyId,
        string bodyDisplayName,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)> ownCaches,
        string siteSalt, string siteName)
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
        // #462: the tube IS an airlock — its two doors share an interlock group, so only the end nearest the
        // captain may stand open and the far end is always drawn SHUT (owner: "only one door in a tube is
        // open at a time… think of airlock"). That is the barrier the Old Ones visibly stop at, and it is
        // what shuts a tailgater in the tube with the built-in gun (#461) instead of letting it follow you
        // aboard. Before this, standing at the threshold held BOTH ends retracted — so the pack piled up
        // against a gap painted wide open.
        const int TubeAirlock = 1;
        doors.Add(new(TubeLeft, DeckPlan.ShuttleHatchY, TubeRight, DeckPlan.ShuttleHatchY, Interlock: TubeAirlock)); // ship-end: crew-only door
        doors.Add(new(TubeLeft, SurfaceTopY, TubeRight, SurfaceTopY, Interlock: TubeAirlock));                       // surface-end
        // The shuttle glyph mid-tube — the map winking at its own abstraction (this corridor IS the ride).
        labels.Add((TubeCenterX, (DeckPlan.ShuttleHatchY + SurfaceTopY) / 2f, "🛸"));

        // ── The wide barren field. #563 · TWO DIFFERENT KINDS OF EDGE, and they must not be drawn alike.
        //
        //    The TOP rim is the ship's own underside — the hull you just walked out of, a made thing with a
        //    real outside. Owner, 2026-07-31: "The space ships come with outside borders but the landing
        //    site out-doors should not." So this one keeps its hull ink; it is honest.
        walls.Add(new(SurfaceLeftX, SurfaceTopY, TubeLeft, SurfaceTopY, false, true));   // top rim, port of the tube
        walls.Add(new(TubeRight, SurfaceTopY, SurfaceRightX, SurfaceTopY, false, true)); // top rim, starboard of the tube

        //    The other three sides are the FIELD ENVELOPE — a technical limit on how far the ground is
        //    generated, with no object in the world to be. Drawn as bright hull lines they made a square
        //    fence around a moon ("it seems artificial on a Moon… it spoils the site feeling"), and worse,
        //    they announced a boundary that is not the real one: the honest edge of a landing site is where
        //    the magazine and the pack behind you say turn around — "the reevers and supply line are kind of
        //    the invisible tether to players distance" — the #453 law that depth is priced by sentries and
        //    nerve, never by geometry. So they are UNSEEN: they collide, and nothing is ever drawn for them.
        //
        //    That first pass hid the fence and left the SHAPE alone, which the owner went straight to:
        //    "But the limit to movement is still a box here?" It was. So the bound is no longer three
        //    straight walls but a wandering chain (SurfaceEdge), seeded per site — the limit to movement is
        //    not a rectangle in the collision either, not merely in the picture.
        //
        //    It only ever bulges OUTWARD from the nominal rectangle, which is what makes it safe to lay
        //    under a game already generating near the edge: the outpost huts (#563) are built INTO the far
        //    edge lane, and a boundary free to wander inward would eventually eat one. Outward can only add
        //    bare regolith. The bulge tapers to nothing at every corner, so the chain closes exactly and
        //    there is no gap for a captain to walk out of the world through.
        var field = new SurfaceLayout.Field(
            SurfaceLeftX, SurfaceRightX, SurfaceTopY, SurfaceBottomY, LandingBandY, MonolithX, MonolithY);
        foreach ((double x1, double y1, double x2, double y2) in SurfaceEdge.Bound(bodyId, siteSalt, field))
        {
            walls.Add(new((float)x1, (float)y1, (float)x2, (float)y2, false, false, Unseen: true));
        }

        // ── The PER-BODY geography (Sunday-morning wind #1–#2): the deep-field ruin/maze walls and the
        //    landmark vary by body — Miranda keeps THE MONOLITH maze (canon), Luna gets the mass-driver
        //    ruins, every other landable body a seeded signature — so no two grounds are the same. The
        //    field envelope above is the shared LAW; only what's inside it is the body's own. Walls are
        //    collision law for everyone (the pure Core SurfaceLayout is where a test pins the geography). ──
        // #320: the chosen landing site parameterizes the ground — an empty salt is the canon site 0
        // (Miranda's maze, Luna's rails, the seeded signature), a non-empty salt re-seeds a distinct wing.
        SurfaceLayout.Plan layout = SurfaceLayout.For(bodyId, field, siteSalt);
        foreach (SurfaceLayout.Wall w in layout.Walls)
        {
            // #563 · A body's own geography is ROCK, never pressure hull. SurfaceLayout's IsHull flag means
            // "solid mass" — the monolith, Luna's mass-driver muzzle, a seeded plinth or ancient spur — as
            // opposed to a fallen span, and that distinction is worth keeping. What was wrong was the ink:
            // it drew in the ship's cold blue-white hull stroke, so 16 of the Ridge Camp's 25 segments were
            // painted as spaceship. Same flag, translated to stone.
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false,
                IsStone: w.IsHull));
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
        // #320: the surface header names WHERE you set down — the chosen landing site, plainly, at the
        // landing band (the crude-grid deck aesthetic: a label on the ground, no new chrome). Site 0's
        // name still reads even though its ground is the canon signature.
        if (siteName.Length > 0)
        {
            labels.Add((TubeCenterX, SurfaceTopY - 5.2f, $"🛬 SET DOWN AT: {siteName.ToUpperInvariant()}"));
        }
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

        // #563 · The terrain. Owner: "put something more interesting in the landscape." Crater rims, scree
        // fans, scarps and rilles, seeded per site and spread across the WHOLE field — including the flanks,
        // which are kept clear of WALLS so a walk-around always exists and were therefore the emptiest and
        // most walkable third of every site. Scenery cannot obstruct, so it is free to go exactly there.
        SurfaceScenery.Mark[] scenery = [.. SurfaceScenery.For(bodyId, siteSalt, field)];

        // #573 · THE SHELTER, deep in the field: one guaranteed building with a REAL door and air inside.
        // Owner: "just make one building into the middle there with working door" → "or lets put that near
        // the bottom there" → "there needs to be explorable space around that building we go to refill."
        //
        // The door is an actual DeckPlan.Door hung on the opening SurfaceStructure hands back — which is why
        // that returns the doorway as a segment rather than a midpoint: a point cannot say which way a
        // passage runs, and a door needs to know.
        SurfaceStructure.Spec shelter = SurfaceShelter.SpecFor(bodyId, siteSalt, field);
        SurfaceStructure.Built built = SurfaceStructure.Build(shelter);
        foreach (SurfaceLayout.Wall w in built.Walls)
        {
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false, IsStone: true));
        }
        foreach (SurfaceStructure.Doorway d in built.Doorways)
        {
            doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2));
            consoles.Add(new(DeckPlan.ConsoleKind.SurfaceAirlock,
                (float)d.CentreX, (float)d.CentreY, SurfaceShelter.DoorLabel));
        }
        consoles.Add(new(DeckPlan.ConsoleKind.ShelterTank,
            (float)shelter.CentreX, (float)shelter.CentreY, SurfaceShelter.TankLabel));
        // Andy Weir's bubble shelters carry survival kit, not just air (owner). Set well off the rack so
        // each is reachable as the ANSWERING console — the #520 law.
        consoles.Add(new(DeckPlan.ConsoleKind.ShelterLocker,
            (float)shelter.CentreX, (float)(shelter.CentreY - 5.5), SurfaceShelter.LockerLabel));
        labels.Add(((float)shelter.CentreX, (float)(shelter.CentreY - 7), "⛺ SHELTER"));

        return new Layout(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(), location,
            doors.ToArray(), scenery);
    }

    // The ship carries one amber shuttle-airlock door across the (bottom) hatch; drop it so the tube's
    // own doors take over the threshold.
    private static bool IsHatchDoor(DeckPlan.Door d) =>
        Math.Abs(d.Y1 - DeckPlan.ShuttleHatchY) < 0.01f && Math.Abs(d.Y2 - DeckPlan.ShuttleHatchY) < 0.01f;
}
