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
/// the landing area and a lonely automated kiosk, and whose deep far side holds the body's own deep
/// landmark — Phobos's MONOLITH, Miranda's false slab at the heart of a crude maze, Luna's mass-driver
/// muzzle — prime Old-Ones ground where the cornering loss-condition is real geometry.
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
    /// every body's geography dresses this spot differently (Phobos's MONOLITH, Miranda's false slab,
    /// Luna's mass-driver muzzle, a seeded fixture elsewhere — see <see cref="SurfaceLayout"/>), but the
    /// anchor itself is fixed so the sight and pack-spawn math is one thing across bodies.
    ///
    /// <para>#649 · These were called <c>MonolithX</c>/<c>MonolithY</c>, and the name was the bug: a MONOLITH
    /// constant governing every ground in the game (bug class 2, and the exact reason a captain standing at
    /// Luna's launch muzzle was told <i>"the monolith resolves out of the dark"</i> and charged the
    /// once-in-a-life nerve hit for it — #648). The anchor belongs to the FIELD; what stands on it belongs to
    /// the body.</para></summary>
    public static readonly float AnchorX = (float)SurfaceLayout.DefaultField.AnchorX;
    public static readonly float AnchorY = (float)SurfaceLayout.DefaultField.AnchorY;

    // #313's single fixed ⛏ DIG HERE field (DigFieldX/DigFieldY, deep by the monolith) is RETIRED by the
    // beach-comber kit (owner, Evening wind 2026-07-18: "bury anywhere"). Burying and probing now happen
    // where the captain STANDS — any diggable square (see IsDiggableGround) — so there is no one commitment
    // spot; the whole deep field is fair game, and a swept grid remembers where you've already checked.

    /// <summary>The crew-only threshold (owner): Old Ones are penned on the surface at the tube mouth and
    /// can never climb it — the door won't open to them. Fed to <c>ReeverChase.Step</c>.</summary>
    public const double ReeverBarrierY = SurfaceTopY;

    /// <summary>Where a tide Reever claws out for spawn index — a deterministic, seed-jittered bearing on a
    /// ring AROUND THE CAPTAIN (<see cref="ReeverTide.SpawnAround"/>).
    ///
    /// <para>#563 · It used to be "just inside the bottom rim", and there is no rim: the ground is unbounded
    /// now. Rising around whoever is standing on the regolith is the more honest reading anyway — the deep is
    /// answering a captain, not a coordinate — and the ring is isotropic, so #453's deleted y-graded danger
    /// stays deleted rather than creeping back in through the spawner.</para></summary>
    public static (double X, double Y) TideSpawnPoint(
        ulong threatSeed, int spawnIndex, double captainX, double captainY) =>
        ReeverTide.SpawnAround(
            threatSeed, spawnIndex, captainX, captainY, ReeverTide.SpawnRingDu(SurfaceLayout.DefaultField));

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
    /// <summary>#602 · THE LIFT HEAD'S SHED, as one object everybody reads.
    ///
    /// <para>Owner, stepping out of the car: <i>"Oh I emerged into the wall on the surface... I cannot move
    /// :-D"</i> — and then, on where he expected to be: <i>"I would expect to spawn into the elevator box
    /// where we went down with."</i></para>
    ///
    /// <para>These four numbers used to be <c>const</c> locals inside the wall builder, so nothing outside
    /// this method could say where the shed was or how big it is. The lift's return path therefore invented
    /// its own answer, and put the captain in a wall. A test that hard-coded 5.0 and 4.0 to check it would
    /// have been the same bug wearing a lab coat — the mirrored-constant failure this ground keeps paying
    /// for. So the shed is a value now: built from it, returned into it, and asserted against it.</para>
    ///
    /// <para>#606 · And it is a <see cref="SurfaceStructure.Spec"/> now, not a hand-typed rectangle, because
    /// the shed had to stop being the one building on the moon that was drawn in a different hand. Everything
    /// here is derived from that spec — the room's clear floor, the doorway the car opens toward, the spot
    /// the captain lands on — so the picture and the arithmetic cannot disagree about a building neither of
    /// them owns any more.</para></summary>
    public readonly record struct LiftHeadBox(
        SurfaceStructure.Spec Hut,
        double HalfW, double HalfH,
        double DoorX, double DoorY,
        double CarX, double CarY,
        double StepX, double StepY)
    {
        public double CentreX => Hut.CentreX;
        public double CentreY => Hut.CentreY;

        /// <summary>Is this point inside the hut's clear floor — the room the car opens into? Rotation-proof,
        /// because the hut is seeded an angle like every other building and an axis-aligned answer would be
        /// right on one site in a hundred.</summary>
        public bool Contains(double x, double y, double clearance = 0)
        {
            double c = Math.Cos(-Hut.AngleRad), s = Math.Sin(-Hut.AngleRad);
            double dx = x - Hut.CentreX, dy = y - Hut.CentreY;
            double lx = (dx * c) - (dy * s), ly = (dx * s) + (dy * c);
            return Math.Abs(lx) <= HalfW - clearance && Math.Abs(ly) <= HalfH - clearance;
        }

        /// <summary>Where the car sets the captain down: inside the room, a pace in from the door it came up
        /// beside, which is what riding a lift up into a shed actually looks like.</summary>
        public (double X, double Y) CarFloor => (CarX, CarY);

        /// <summary>#681 · Where a LANDING sets the captain down: a pace OUTSIDE the same door, facing it.
        ///
        /// <para>The exact mirror of <see cref="CarFloor"/>, and it exists for the exact reason that one
        /// does. <c>?secretlab=…&amp;land=1</c> computed its own answer — the head spot with 7.5 du taken off
        /// its Y — and that number was written when the head was a hand-typed 10 x 8 box whose half-height was
        /// 4. #606 made the head an ordinary hut: 14–19.6 du wide, 11–15.4 deep, walls up to 3 du of piled
        /// regolith, and a SEEDED ANGLE. Seven and a half deck units below the middle of that is not a pace
        /// outside the door; on most seeds it is the far wall, and on <c>?secretlab=deep</c> it is the wall
        /// segment the owner spent an excursion standing inside of.</para>
        ///
        /// <para>Same bug class as #602, one head further along: a caller doing its own geometry about a
        /// building it does not own. So the landing asks the shed where its doorstep is, exactly as the car
        /// asks it where its floor is, and neither of them keeps a number.</para></summary>
        public (double X, double Y) DoorStep => (StepX, StepY);
    }

    /// <summary>How far in from the doorway the car sets you down, and where the panel is. Far enough inside
    /// that the captain's own width clears the jamb; near enough that the panel answers [E] from where a
    /// person stands when they walk in — the #585 report was a button at a room's centre and a captain in the
    /// doorway being told there was nothing here.</summary>
    private const double CarStepIn = 2.4;

    /// <summary>#681 · How far OUTSIDE the door a landing sets you down. Measured from the door's own centre
    /// like <see cref="CarStepIn"/> is, so it clears the wall's outer face by this much whatever thickness the
    /// hut was seeded — which is the whole point. Deliberately the same distance in as out: the cheat's
    /// promise is "a pace outside the shed's door, facing it", and a pace is a pace either way.</summary>
    private const double DoorStepOut = CarStepIn;

    /// <summary>The hut for this body and site, already moved clear of the shelters and the outpost.</summary>
    public static LiftHeadBox LiftHead(string bodyId, string? siteSalt, in SurfaceLayout.Field field)
    {
        SurfaceStructure.Spec hut = SecretLab.HeadHut(bodyId, siteSalt, field);
        SurfaceStructure.Envelope env = SurfaceStructure.EnvelopeOf(hut);

        // The door the car opens beside is the hut's OWN first opening, taken from the builder rather than
        // guessed from a face index — the builder ranks faces by length and the ranking is its business, not
        // this function's. (#587's lesson, one floor up: a caller that re-derives a builder's choice is a
        // second source of truth wearing a coordinate.)
        SurfaceStructure.Doorway way = SurfaceStructure.Build(hut).Doorways[0];
        double dx = way.CentreX - hut.CentreX, dy = way.CentreY - hut.CentreY;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        double ux = len > 0.001 ? dx / len : 0, uy = len > 0.001 ? dy / len : -1;

        return new LiftHeadBox(
            hut, env.InnerHalfW, env.InnerHalfH,
            way.CentreX, way.CentreY,
            // Straight in from the doorway, along the line from the room's middle to it.
            way.CentreX - (ux * (CarStepIn + (env.Thickness / 2))),
            way.CentreY - (uy * (CarStepIn + (env.Thickness / 2))),
            // #681 · …and straight OUT of it, the same distance the other way, so a landing stands on the
            // doorstep rather than in whichever wall happens to be 7.5 du below the middle of the hut.
            way.CentreX + (ux * (DoorStepOut + (env.Thickness / 2))),
            way.CentreY + (uy * (DoorStepOut + (env.Thickness / 2))));
    }

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
            (double x, double y) = c is { DigX: { } dx, DigY: { } dy } ? (dx, dy) : CachePosition(c.Id);
            list.Add((c.Id, x, y, c.ReeverLevel));
        }
        return list;
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
        Layout layout;
        if (!_layoutCache.TryGetValue(key, out layout))
        {
            layout = BuildLayout(
                bodyId, bodyDisplayName, ownCaches, siteSalt, siteName, monolithEpoch, hasSecretSite,
                preserved);
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

    // WASM is single-threaded, so a plain dictionary is safe. Bounded (see the growth guard above).
    private const int LayoutCacheCap = 64;
    // #585 · CONCURRENT, because the browser is not this cache's only caller. In WASM the game is
    // single-threaded and a plain Dictionary was safe; the AUDITS are not — xUnit runs test classes in
    // parallel, so two of them building surface decks at once raced on this dictionary and produced a shelter
    // list that did not match the ground. That surfaced as a guard which passed alone and failed in the full
    // run, which is worse than no guard at all: a flaky audit teaches you to ignore audits.
    //
    // Building a Layout is deterministic, so a racing double-build is pure waste and never a wrong answer —
    // only the dictionary itself needed protecting.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<SurfaceDeckKey, Layout>
        _layoutCache = new();

    private static Layout BuildLayout(
        string bodyId,
        string bodyDisplayName,
        IReadOnlyList<(string Id, double X, double Y, int ReeverLevel)> ownCaches,
        string siteSalt, string siteName, long monolithEpoch, bool hasSecretSite, bool preserved)
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
        // #585 · ONE FIELD, ONE EXPRESSION. This used to re-type the same seven constants that
        // ExpeditionField() is built from — identical values, and a SECOND COPY of the arithmetic that the
        // beacons, the shelter placer, the audits and the labs all read through ExpeditionField(). That is
        // the precise shape of the bug that made the map lie (SpecFor vs SpecsFor) and of the envelope drift
        // before it. There is nothing to keep in sync if there is only one of it.
        SurfaceLayout.Field field = ExpeditionField();

        // #563 · AND THEN THE BOUND CAME OFF ALTOGETHER. The wandering chain used to be laid here as three
        // unseen-but-solid runs, and it was the last thing making the ground finite. It is a far BACKSTOP now
        // (SurfaceEdge.BeyondBackstop, ten thousand du out, enforced as a radius rather than as stone), and
        // what stops a captain at any distance a captain actually walks is the tether: the tank, the
        // magazine, the walk home. The ground itself simply carries on — SurfaceTiles lays the next tile.

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
            // #649 · Unseen carries through: a solid's internal hatch may be collision-only (the monolith's
            // is), and the renderer already knows how to stop you at something it never paints — that is what
            // the field's own bound has always been.
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false,
                Unseen: w.Unseen, IsStone: w.IsHull));
        }

        // #573 · EVERY BUILDING GETS A REAL DOOR. Owner: "there seemed to be shelter like spaces that were
        // just missing the services and the doors.... let's fix those." They had openings the whole time —
        // the generator was discarding them — so a thick-walled ruin read as an unfinished shelter instead
        // of somewhere people used to live. An auto-door on each one makes it a building you enter.
        // #592 · AND ONE OF THEM COST SOMEBODY MONEY. Owner: "some special color not distinctive to the site
        // could then used to draw our attention to a place (like expensive door made with far away imported
        // materials)."
        //
        // Every ordinary hatch is drawn in the local stone, so an off-palette one is a SENTENCE — somebody
        // shipped materials across the system to seal this room, and nobody does that for a store cupboard.
        // Rare on purpose (one in seven): a signal that fires on every ruin is wallpaper. It is seeded per
        // doorway, so the room worth breaking into is a fact about the site rather than a fresh die.
        // #563 slice 2 · ASKED IN CORE, because the lattice's tiles hang exactly these and a rule written
        // out twice is the bug class where one copy gets edited. SurfaceTiles.Doors answers the HOME tile on
        // the site's own salt, so every door on this ground is the colour it has always been.
        foreach (SurfaceTiles.HungDoor d in SurfaceTiles.Doors(bodyId, siteSalt, SurfaceTiles.Home))
        {
            doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Imported: d.Imported));
        }

        // #573 · AND SOMETHING INSIDE ABOUT HALF OF THEM — the "services" half of the same report. A
        // thick-walled room you can enter and find nothing in is worse than no room at all, because the walk
        // in cost air and taught you not to bother next time. But if EVERY building paid out, entering them
        // would stop being a decision and become a chore performed on all of them, so the empty ones are
        // load-bearing: they are what make the others worth the suit-air.
        //
        // #563 slice 2 · …and this question, too, is Core's now, for the same reason the doors above are:
        // every tile in the lattice asks it and there must be exactly one asking.
        IReadOnlyList<SurfaceTiles.Drawer> salvageSpots =
            SurfaceTiles.Drawers(bodyId, siteSalt, SurfaceTiles.Home);

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

        // #649 · THE ONE FILLED MASS ON ANY MOON. Every other solid object on a landing site is drawn as a
        // hatched outline — the established idiom for piled regolith, and the reason a plinth reads as mass
        // rather than as a room. The monolith is drawn as neither: a single unbroken filled rectangle, with
        // no join anywhere in it, using the same primitive the ship's solid shielding uses (#537: "drawn
        // filled so it cannot be mistaken for somewhere you could be").
        //
        // This is the owner's "not having been built by us", in the picture rather than in a sentence. The
        // proportions are 1 : 4 : 9 and no quarry cuts that; the face has no seam and no course; and neither
        // fact is ever stated anywhere the captain can read it.
        var structures = new List<DeckPlan.Structure>();
        if (Monolith.StandsOn(bodyId, siteSalt))
        {
            structures.Add(new DeckPlan.Structure(
                AnchorX - (float)Monolith.HalfWidth, AnchorY - (float)Monolith.HalfHeight,
                AnchorX + (float)Monolith.HalfWidth, AnchorY + (float)Monolith.HalfHeight));
        }

        // #649 · HOW WIDE "THE DEEP AREA" IS depends on what is standing in it. This was a flat 16 du either
        // side of the anchor, which is honest for a six-du fixture and a lie for a fifty-four-du one: a
        // captain with a glove flat on the west end of the monolith would have been told they were on
        // PHOBOS SURFACE, which is the sim doing one thing and the sentence saying another (bug class 3), on
        // the one square of ground in the game where the sentence matters most.
        double deepHalfW = Math.Max(16.0, Monolith.StandsOn(bodyId, siteSalt)
            ? Monolith.ApronRadius
            : 0.0);
        double deepTopY = AnchorY + Math.Max(8.0, Monolith.StandsOn(bodyId, siteSalt)
            ? Monolith.HalfHeight + 8.0
            : 0.0);

        // The location line is a pure function of (position, bodyDisplayName, layout.Scheme, ship) — all
        // deterministic per body id, none of it component-bound — so the closure is safe to cache.
        Func<double, double, string> location =
            (x, y) => y > DeckPlan.ShuttleHatchY ? ship.Location(x, y)
                    : y > SurfaceTopY ? "DOWN-TUBE (the shuttle ride)"
                    : y > LandingBandY - 2 ? "LANDING AREA"
                    : y < deepTopY && Math.Abs(x - AnchorX) < deepHalfW ? layout.Scheme
                    : $"{bodyDisplayName.ToUpperInvariant()} SURFACE";

        // #563 · The terrain. Owner: "put something more interesting in the landscape." Crater rims, scree
        // fans, scarps and rilles, seeded per site and spread across the WHOLE field — including the flanks,
        // which are kept clear of WALLS so a walk-around always exists and were therefore the emptiest and
        // most walkable third of every site. Scenery cannot obstruct, so it is free to go exactly there.
        var sceneryList = new List<SurfaceScenery.Mark>(SurfaceScenery.For(bodyId, siteSalt, field));

        // ── #585 · THE CAMOUFLAGED LIFT HEAD. Owner: "on surface we would only need a camouflaged elevator.
        //    I think there are a lot of movie references to masked elevators to underground sites (The Hive
        //    in Resident Evil for example)."
        //
        // A LIFT LOBBY, not a ruin with a secret in the middle. The first attempt was both of those mistakes:
        // it was built through SurfaceStructure — which adds an interior PARTITION, so the middle of the shed
        // could be walled off from its own doorway — and the call button sat at the building's centre while
        // DeckPlan.InteractRadius is 3 du and the shed was fourteen across. The owner stood in the doorway,
        // which is exactly where a person stands at a door, and reported "it says nothing here", then
        // "there is no console ... I tried E to dig to find anything and found nothing but regolith."
        //
        // So: one small room, one door, one button a pace inside it where a lobby puts it. An affordance you
        // can see and cannot use is worse than none (#212).
        //
        // Drawn from hasSecretSite — the fact the EXCURSION resolved, which honours ?secretlab= — never from
        // SecretLab.Present(bodyId), the unforced seed. That was the other half of the same report: on a
        // cheat rock whose seed said "no lab", the head was never built and the feature was unreachable from
        // the one URL that exists to reach it.
        if (hasSecretSite)
        {
            // ── #606 · ONE MORE HUT AMONG THE HUTS. Owner, after a fresh look at the ground: "the elevator
            //    still stands out on surface like a sore thumb" — and, on the fix he wanted: "it could be in
            //    an ordinary hut, with 2 doors .. we have those. The expensive doors would be the clue... a
            //    clue we can get tipped about or find it in papers."
            //
            // What made it a sore thumb was never its colour. It was five thin lines in a 10 x 8 rectangle
            // standing on a moon where every other building is piled regolith with hatched thickness and a
            // seeded angle — the only structure on the ground drawn in a different hand. That is visible from
            // anywhere, to anybody, and no violet door was ever going to hide it.
            //
            // So it is a SurfaceStructure hut like its neighbours: same builder, same size range, same
            // masonry, seeded angle. Nothing about the silhouette says anything.
            //
            // #585's answer was the opposite — a maintenance plate above the door and "THE CAR IS STILL HERE"
            // below it — and both are gone. They were the game announcing itself, which is the one thing this
            // ground has a house rule against (docs/art-manifest-hive.md: the object is mundane, the
            // implication is not). The findability they were paying for moves to the INFORMATION: the tracker
            // beacon a tip lights (BuildBeacons), the detector gradient, the papers that name a moon. A clue
            // chain is a better game than a caption, and it is the one the owner asked for.
            LiftHeadBox head = LiftHead(bodyId, siteSalt, field);
            SurfaceStructure.Built built = SurfaceStructure.Build(head.Hut);

            foreach (SurfaceLayout.Wall w in built.Walls)
            {
                walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false, IsStone: true));
            }

            // ── AND THE ONE THING THAT DOES NOT MATCH. Every hatch on a landing site is swaged out of the
            //    hill it is set in (#592); these were flown here. IMPORTED puts them off the world's palette,
            //    and MACHINED draws them as what they are — a heavy sealed leaf in a wall of piled rubble,
            //    where every other hut on the moon has a thin one. A captain who never looks closely walks
            //    past. A captain who reads doors has just found a receipt, which is the only thing this
            //    operation has ever been careless with.
            foreach (SurfaceStructure.Doorway d in built.Doorways)
            {
                doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2,
                    Imported: true, Machined: true));
            }

            // Named for what it looks like bolted to a wall, never for what it does. The panel says the rest
            // once it is pressed — and until it is, this is a shack with an odd fitting in it.
            consoles.Add(new(DeckPlan.ConsoleKind.HiveHead,
                (float)head.CarX, (float)head.CarY, "▤ SERVICE PANEL"));

            // ── #1074 beat 2 · AND, ON A SITE THE OFFICE HAS TAKEN INTO CARE, A RAIL AND A SIGN.
            //
            // A working the Authority closed a shift ago is fenced, signed and under study a shift after
            // that: a small closed ring of rail round the shed, one gap in it, and the notice posted at the
            // gap. Two objects, and between them they are the entire beat — there is no card, no pulse, no
            // marker and nothing on the wire (#761: the world tells it plainly or not at all).
            //
            // THE RAILS ARE DRAWN AS ORDINARY INNER LINES. Not hull, which is the rim fence #565 took off
            // this ground for announcing a boundary that was not the real one; not stone, because a rail is
            // not mass; not unseen, because being seen is its whole job. It is the same ink every fallen
            // span and low ruin wall on every landing site already wears, which is what makes it read as a
            // thing somebody put up rather than as a new piece of chrome.
            //
            // AND THE GAP FACES THE TUBE, which is a law and not a flourish — see PreservationZone. The car
            // comes up in the middle of this ring, and a captain must never be fenced away from his own boat.
            if (preserved)
            {
                PreservationZone.Fence fence = PreservationZone.FenceAround(head.Hut, field);
                foreach (SurfaceLayout.Wall r in fence.Rails)
                {
                    walls.Add(new((float)r.X1, (float)r.Y1, (float)r.X2, (float)r.Y2, false, false));
                }

                // The notice, in the ground-label idiom this surface has posted every sign in since #313 —
                // a line of text on the regolith, no new chrome (the crude-grid deck aesthetic). It stands a
                // pace outside the gate on the approach, so it is read on the way in.
                labels.Add(((float)fence.SignX, (float)fence.SignY, PreservationZone.Notice));
            }
        }

        // #586 · THE SWEPT APRON. Owner: "it is supposed to be impressive... now it looks like a box in
        // closet." Widening the slab alone could never fix that — on a crude grid every rectangle is a
        // rectangle, and the grid is the aesthetic, not a limitation.
        //
        // What DOES read at this scale is ground that is visibly cleared. A ring of swept regolith around the
        // slab, in a field where everything else is rubble and drift, is legible from a long way off and says
        // the one thing the stone cannot say by itself: SOMEBODY CARED ABOUT THIS SPOT. Drawn as scenery, so
        // it can never become a fence around the landmark and turn a pilgrimage into a puzzle.
        //
        // #649 · AND ONLY WHERE THERE IS SOMETHING TO SWEEP AROUND. This loop ran unconditionally, so every
        // ground in the game — Luna's mass-driver muzzle, every seeded ancient spur, every slag field, every
        // second and third landing site on every moon — was drawn standing inside the monolith's ceremony
        // ring. That is the same fault as #648 one layer out in the picture instead of the predicate: a
        // ceremony that belongs to ONE object, laid on grounds that have never seen it, telling the captain
        // somebody cared about a patch of rubble. The apron belongs to the thing it is swept around, so it is
        // asked for by the thing it is swept around.
        AddApron(sceneryList, Monolith.StandsOn(bodyId, siteSalt)
            ? (Monolith.ApronRadius, Monolith.ApronSegments)
            : FalseSlab.StandsOn(bodyId, siteSalt)
                ? (FalseSlab.ApronRadius, FalseSlab.ApronSegments)
                : null);

        // #586 · THE PICTURE, AND WHAT IS AT ITS FOOT. Owner: "let's have gen AI image at the monolith and
        // some items appearing there now and then ... it is supposed to be impressive."
        //
        // The monolith's own ground only. Every body dresses this same deep anchor differently — Miranda's
        // false slab, Luna's mass-driver muzzle, a seeded plinth elsewhere — and putting THE MONOLITH's card
        // on a launch head would be the borrowed-prose bug (#574) wearing a landmark.
        // Monolith.StandsOn, not a literal body id: the slab's GEOMETRY is only laid on the canon empty-salt
        // ground (SurfaceLayout routes site 1+ to the seeded generator), so a bare body-id test put the
        // ▮ THE MONOLITH card and the foot-offering on that moon's seeded sites too, pointing at open
        // regolith. That is precisely what the note below forbids — a marker pointing at nothing.
        if (Monolith.StandsOn(bodyId, siteSalt))
        {
            // #649 · THE SHADOW, which is the only way a top-down plan can say 121 deck units TALL.
            //
            // Owner: it must "read as large from a long way off, and keep getting larger as you walk, the way
            // only genuinely big things do." Nothing about a footprint does that — a rectangle on a grid is a
            // rectangle whatever size it is, and the camera only frames about 64 du, so from the landing band
            // even a slab this big is simply off-screen. What IS visible from every square of the field is
            // what an object that tall does to the light: at 18° of sun it throws a lane of dark roughly 370
            // du up-field, which is longer than the walked world is deep.
            //
            // So the captain steps off the pad into shade that runs off the bottom of the map, and the only
            // way to find out what is casting it is to walk down it. Nothing says so. Drawn as scenery —
            // never collided — because a shadow is not a thing you can bump into, and because the one law
            // this ground has is that what stops you and what you can see are separate arrays.
            AddShadow(sceneryList, field);

            // The card, on the APPROACH side. It used to sit deep of the slab, which was harmless at six deck
            // units and is a walk of fifty round solid rock at the real size — an affordance you can see and
            // cannot reach is worse than none (#212). The captain always comes from up-field; the console
            // meets them at the face.
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject,
                AnchorX, AnchorY + (float)Monolith.HalfHeight + 2.5f,
                Monolith.ConsoleLabel, Monolith.ArtUrl, Monolith.Lore));

            // And whatever somebody left, if this window has anything. NOT a console that is always there
            // with an empty payload — a marker pointing at nothing is the map lying, which is the one thing
            // this ground is not allowed to do. Along the same face, a good way down it: things are left at
            // the FOOT of the thing, and the foot is now long enough to walk.
            Monolith.Offering left = Monolith.AtTheFoot(bodyId, siteSalt, monolithEpoch);
            if (left != Monolith.Offering.Nothing)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.MonolithFoot,
                    AnchorX + (float)(Monolith.HalfWidth * 0.55),
                    AnchorY + (float)Monolith.HalfHeight + 2.5f,
                    Monolith.FootLabel(left)));
            }
        }

        // #649 · AND MIRANDA'S OWN CARD. The maze centre had a picture and a card for as long as it was
        // called the monolith; taking the word back must not take the content with it, or the canon ground
        // the owner walks by default would quietly lose the one thing at the end of its long walk. It gets
        // its OWN card, which describes a stacked, mortared, weathering object and accounts for nothing —
        // reusing the monolith's plate here would have been #574 all over again.
        else if (FalseSlab.StandsOn(bodyId, siteSalt))
        {
            consoles.Add(new(DeckPlan.ConsoleKind.ViewObject,
                AnchorX, AnchorY - (float)FalseSlab.HalfHeight - 2f,
                FalseSlab.ConsoleLabel, FalseSlab.ArtUrl, FalseSlab.Lore));
        }

        SurfaceScenery.Mark[] scenery = [.. sceneryList];

        // #573 · THE SHELTER, deep in the field: one guaranteed building with a REAL door and air inside.
        // Owner: "just make one building into the middle there with working door" → "or lets put that near
        // the bottom there" → "there needs to be explorable space around that building we go to refill."
        //
        // The door is an actual DeckPlan.Door hung on the opening SurfaceStructure hands back — which is why
        // that returns the doorway as a segment rather than a midpoint: a point cannot say which way a
        // passage runs, and a door needs to know.
        // #573 · EVERY shelter, not just the first. THE MAP LIED, and this was why: SurfaceShelter grew
        // from one shelter to several (SpecsFor), the tracker beacons were switched to show all of them, and
        // THIS — the code that actually builds them — was left calling SpecFor and laying exactly one. So
        // two to four rings on the fan pointed at buildings that had never been built, including one the
        // owner was standing directly on top of.
        //
        // Same shape as the field-envelope drift earlier today: two places that had to agree, only one of
        // them updated. The fix here is that there is now ONE loop, and the beacons read the same list.
        // #563 slice 3 · ASKED OF THE TILE, so the ground under the tube and the ground nine hundred du out
        // are furnished by one question rather than two. SurfaceTiles.Shelters hands the home tile the site's
        // own salt and the default field — the exact pair this line used to pass by hand — so every drum at
        // the tube keeps its place, its angle and its seeded story.
        foreach (SurfaceStructure.Spec shelter in
                 SurfaceTiles.Shelters(bodyId, siteSalt, SurfaceTiles.Home))
        {
            FurnishShelter(shelter, walls, doors, consoles, labels);
        }

        foreach (SurfaceTiles.Drawer drawer in salvageSpots)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.RuinSalvage, (float)drawer.X, (float)drawer.Y,
                SurfaceSalvage.LabelFor(drawer.Find)));
        }


        return new Layout(
            walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(), location,
            doors.ToArray(), scenery, structures.ToArray());
    }

    /// <summary>
    /// #573/#563 slice 3 · ONE SHELTER, PUT ON A PLAN — its shell, its imported door, its two services and
    /// its label.
    ///
    /// <para><b>One body, two grounds.</b> The home tile's build (above) and the lattice's tile compose
    /// (<c>Map.TileRegion</c>) both lay shelters now, and a rule expressed twice is this project's fourth
    /// named bug class — the version that ships is the one where only one of the two got edited. #573's own
    /// scar is exactly that shape: the beacons were switched to every shelter and the builder was left laying
    /// the first, so rings pointed at buildings that had never been built.</para>
    ///
    /// <para>Every line below is the line that was inline here, in the order it was in, so the ground at the
    /// tube is byte for byte what it was.</para>
    /// </summary>
    public static void FurnishShelter(
        in SurfaceStructure.Spec shelter,
        List<DeckPlan.Wall> walls, List<DeckPlan.Door> doors,
        List<DeckPlan.ConsoleSpot> consoles, List<(float X, float Y, string Text)> labels)
    {
        SurfaceStructure.Built built = SurfaceStructure.Build(shelter);
        foreach (SurfaceLayout.Wall w in built.Walls)
        {
            walls.Add(new((float)w.X1, (float)w.Y1, (float)w.X2, (float)w.Y2, false, false, IsStone: true));
        }
        foreach (SurfaceStructure.Doorway d in built.Doorways)
        {
            // #592 · A shelter's door is ALWAYS imported, and that is not a hint — it is the truth about
            // the building. Nobody swages a pressure door out of regolith; somebody flew this out here
            // for strangers to find. It also means the one door on the field that will definitely save
            // your life is the one that reads differently from every wall around it.
            doors.Add(new((float)d.X1, (float)d.Y1, (float)d.X2, (float)d.Y2, Imported: true));
            // #585 · ITS OWN KIND, NOT THE SHIP'S HATCH. Owner, mid-excursion: "how did I just go to ship
            // from a shelter ... what happened" / "I was at surface shelter and now at ship shuttle bay
            // ... how". Because this console was laid as SurfaceAirlock — the SAME kind as the down
            // tube's "BOARD THE SHUTTLE" — so [E] on a shelter door ran the boarding path and flew him
            // home from the middle of the field, excursion and all.
            //
            // A console kind is a VERB, and two different doors were sharing one. Reusing it read as
            // tidy ("they are both doors") and was the same two-things-one-name mistake as every other
            // expensive bug on this ground.
            consoles.Add(new(DeckPlan.ConsoleKind.ShelterDoor,
                (float)d.CentreX, (float)d.CentreY, SurfaceShelter.DoorLabel));
        }
        consoles.Add(new(DeckPlan.ConsoleKind.ShelterTank,
            (float)shelter.CentreX, (float)shelter.CentreY, SurfaceShelter.TankLabel));
        consoles.Add(new(DeckPlan.ConsoleKind.ShelterLocker,
            (float)shelter.CentreX, (float)(shelter.CentreY - 5.5), SurfaceShelter.LockerLabel));
        labels.Add(((float)shelter.CentreX, (float)(shelter.CentreY - 7), "⛺ SHELTER"));
    }

    /// <summary>#649 · Lay a swept apron ring around the deep anchor, or lay nothing at all. One function so
    /// the two objects that have ceremony cannot drift apart in how it is drawn, and so the grounds that have
    /// none get nothing rather than somebody else's.</summary>
    private static void AddApron(List<SurfaceScenery.Mark> scenery, (double Radius, int Segments)? apron)
    {
        if (apron is not { } a || a.Segments <= 0)
        {
            return;
        }
        for (int i = 0; i < a.Segments; i++)
        {
            double a0 = i / (double)a.Segments * Math.Tau;
            double a1 = (i + 1) / (double)a.Segments * Math.Tau;
            scenery.Add(new SurfaceScenery.Mark(
                AnchorX + (Math.Cos(a0) * a.Radius), AnchorY + (Math.Sin(a0) * a.Radius),
                AnchorX + (Math.Cos(a1) * a.Radius), AnchorY + (Math.Sin(a1) * a.Radius),
                SurfaceScenery.Kind.Ridge));
        }
    }

    /// <summary>#649 · THE MONOLITH'S SHADOW — a lane of dark running up-field from the slab's face to the
    /// far end of the walked world, drawn and never collided.
    ///
    /// <para>Every number is the object's own (<see cref="Monolith.ShadowLengthDu"/>, <c>HalfWidth</c>,
    /// <c>ShadowSpread</c>) and none of them is typed here, which is the point: this is a picture OF a
    /// height, and if the height ever changes the picture has to change with it or the drawing starts lying
    /// about the thing it is drawn from — bug class 3, the one this ground keeps paying for.</para>
    ///
    /// <para>It reaches past the top of the field and is clamped there. Nothing marks where it ends, because
    /// the true edge of it is over the horizon and saying so would be the game explaining itself.</para>
    /// </summary>
    private static void AddShadow(List<SurfaceScenery.Mark> scenery, in SurfaceLayout.Field field)
    {
        double nearY = field.AnchorY + Monolith.HalfHeight;          // the lit face; the shade starts here
        double farY = Math.Min(field.LandingBandY, nearY + Monolith.ShadowLengthDu);
        if (farY <= nearY)
        {
            return;
        }

        double nearHalf = Monolith.HalfWidth;
        double farHalf = Monolith.HalfWidth * Monolith.ShadowSpread;   // a low sun's penumbra is not subtle

        // Nine strokes: the two edges of the umbra and seven streaks inside it. Enough that a captain crossing
        // it reads a REGION of shade rather than a channel with banks — a rille is two lines and this must
        // never be mistaken for one.
        const int Strokes = 9;
        for (int i = 0; i < Strokes; i++)
        {
            double t = (i / (double)(Strokes - 1) * 2.0) - 1.0;       // −1 … +1 across the lane
            // The inner streaks stop short and at staggered lengths, so the far end frays out instead of
            // ending on a line. A shadow with a hem is a wall's shadow; this one belongs to something whose
            // top nobody has seen.
            double reach = i == 0 || i == Strokes - 1 ? 1.0 : 0.55 + (0.4 * ((i * 7 % 5) / 4.0));
            scenery.Add(new SurfaceScenery.Mark(
                field.AnchorX + (t * nearHalf), nearY,
                field.AnchorX + (t * farHalf * reach), nearY + ((farY - nearY) * reach),
                SurfaceScenery.Kind.Rille));
        }
    }

    // The ship carries one amber shuttle-airlock door across the (bottom) hatch; drop it so the tube's
    // own doors take over the threshold.
    private static bool IsHatchDoor(DeckPlan.Door d) =>
        Math.Abs(d.Y1 - DeckPlan.ShuttleHatchY) < 0.01f && Math.Abs(d.Y2 - DeckPlan.ShuttleHatchY) < 0.01f;
}
