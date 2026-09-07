using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #371 · THE SURFACE AS A DECK — the one entry point that hands back a walkable plan of ship, airlock,
/// down-tube and open regolith, and the memo behind it.
///
/// <para>Everything that is a pure function of the <c>SurfaceDeckKey</c> is cached as a
/// <c>Layout</c>; the droids are NOT, because their buffer and fill delegate are re-bound on every
/// call so a cached layout can never capture a component reference. A wall stencil that is up for
/// fourteen seconds is passed outside the memo for the same reason: the cache holds the GROUND.</para>
///
/// <para>Split out of <c>MoonSurface.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class MoonSurface
{
    /// <summary>
    /// Build the ship + dual-door airlock + down-tube + wide barren surface as one continuous walkable
    /// plan. Burying and probing are now free-form (E where you stand — the beach-comber kit), so there is
    /// no fixed ⛏ console; only each own cache's ✗ plants a 🗺 dig console at its recorded spot
    /// (<paramref name="ownCaches"/>). <paramref name="fillDroids"/> and <paramref name="droidCount"/> come
    /// from the caller so the crew and the live Old Ones share one buffer.
    /// </summary>
    /// <param name="bigLabels">#835 · A wall stencil for the shed, or nothing — which is what the regolith
    /// normally has. It is passed OUTSIDE the memoized layout on purpose: the cache holds the ground (walls,
    /// consoles, labels), and a sign that is up for fourteen seconds is not the ground. The one caller today
    /// is the kick-out's KICKED OUT plate.</param>
    public static DeckPlan SurfaceDeck(
        string bodyId,
        string bodyDisplayName,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)> ownCaches,
        int droidCount, Action<double, DeckPlan.Droid[]> fillDroids,
        string siteSalt = "", string siteName = "", long monolithEpoch = 0,
        bool hasSecretSite = false,
        (float X, float Y, string Text, float Px, int Tone)[]? bigLabels = null)
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
        //
        // #1074 · …and whether the office has taken this site into care, which is asked HERE rather than
        // passed in. It is a fact about the world register (PreservationZone) and not about the excursion,
        // so there is exactly one place that can read it and no caller to keep in step — and it goes into
        // the KEY in the same breath it is read, because a cached deck built before the fence went up is
        // precisely the stale ground this memo has to be unable to serve.
        bool preserved = PreservationZone.On(bodyId);
        SurfaceDeckKey key = SurfaceDeckKey.For(
            bodyId, bodyDisplayName, ownCaches, siteSalt, monolithEpoch, hasSecretSite, preserved);
        // Cheap unbounded-growth guard: each distinct (body, cache-set) leaves one small entry, and a
        // long game of bury/lift cycles could accumulate stale sets nobody revisits. A generous cap
        // that never trips in normal play keeps the cache from creeping; on overflow we simply start
        // fresh (the next builds re-warm the live grounds). #1112 · the check-clear-insert that used to
        // stand here is now BoundedMemo's, so the haven's twin memo cannot drift away from this rule again.
        Layout layout = _layoutCache.GetOrBuild(key, () => BuildLayout(
            bodyId, bodyDisplayName, ownCaches, siteSalt, siteName, monolithEpoch, hasSecretSite,
            preserved));

        return new DeckPlan(
            layout.Walls, layout.Consoles, layout.Labels, layout.Backdrops,
            spawnX: SpawnX, spawnY: SpawnY,
            droidCount: droidCount, fillDroids: fillDroids,
            location: layout.Location,
            // #465: hand the tube's doors to the plan. `doors: null` here is what made the airlock invisible.
            doors: layout.Doors, shipFixtures: true, followCam: true, tables: DeckPlan.Ship.Tables,
            // #1040 · …and her counter's seats and its fill beside her tops, for the reason her tops are
            // here at all: this plan IS her deck with a landing site grown out of the shuttle hatch, so a
            // fixture she has that this list does not is a fixture that vanishes the moment she sets down.
            stools: DeckPlan.Ship.Stools, furniture: DeckPlan.Ship.Furniture,
            scenery: layout.Scenery,
            // #589: this world's own stone, so a glance at the walls says which moon this is.
            stoneInk: BodyPalette.For(bodyId),
            // #592: and its doors, so an IMPORTED one stands out as the sentence it is.
            doorInk: BodyPalette.DoorInk(bodyId),
            // #649: the monolith's filled mass — the one object on any moon drawn without a join in it.
            structures: layout.Structures,
            // #835: the shed's wall stencil, when there is one to paint. Outside the memo on purpose.
            bigLabels: bigLabels);
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
        SurfaceScenery.Mark[] Scenery,
        // #649: the filled masses. Exactly one ground in the game has one — the monolith — and it is the
        // opposite of the scenery array: drawn AND collided, by the walls that share its outline.
        DeckPlan.Structure[] Structures);

    // #585 · CONCURRENT, because the browser is not this cache's only caller. In WASM the game is
    // single-threaded and a plain Dictionary was safe; the AUDITS are not — xUnit runs test classes in
    // parallel, so two of them building surface decks at once raced on this dictionary and produced a shelter
    // list that did not match the ground. That surfaced as a guard which passed alone and failed in the full
    // run, which is worse than no guard at all: a flaky audit teaches you to ignore audits.
    //
    // Building a Layout is deterministic, so a racing double-build is pure waste and never a wrong answer —
    // only the dictionary itself needed protecting.
    //
    // #1112 · BOUNDED, and now bounded by a policy rather than by four lines up in SurfaceDeck: the cap and
    // the flush that were written here are BoundedMemo's, which HavenInterior's twin memo holds too. THAT one
    // had no cap at all until this issue — which is what a rule kept in a call site instead of a type costs.
    private static readonly BoundedMemo<SurfaceDeckKey, Layout> _layoutCache = new(BoundedMemo.DefaultCap);

    /// <summary>#1112 · The layout memo, for the guard that holds it to its cap. Test-visible only.</summary>
    internal static int LayoutCacheCount => _layoutCache.Count;

    /// <summary>#1112 · …and the cap it is held to.</summary>
    internal static int LayoutCacheCap => _layoutCache.Cap;
}
