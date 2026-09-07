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
///
/// <para>#251 · THIS FILE IS THE GROUND'S GEOMETRY — where the top of the regolith is, where the tube
/// mouths, where the field ends, and the two predicates the rest of the client asks (<see cref="IsSafeAboard"/>,
/// <see cref="IsInDownTube"/>). The rest is five siblings: <c>.Lift</c> (the shed the car comes up in),
/// <c>.Dig</c> (which ground takes a shovel, and where a cache's ✗ falls), <c>.Deck</c> (the one entry
/// point, and the memo behind it), <c>.Layout</c> (the pure build the memo stores), and <c>.Shelter</c>
/// (one shelter put on a plan, plus the apron and the shadow under a landed boat).</para>
///
/// <para>Every static field in the family is declared HERE, in the opening file, and every one of them is
/// measured off <c>SurfaceLayout.DefaultField</c> or a <c>const</c> rather than off a sibling — so there is
/// no chain to break across a file boundary. The one exception is <c>.Deck</c>'s layout memo, which reads
/// nothing of this class's and is read by nothing of it. See
/// <c>NoPartialClassSpreadsItsStaticFieldsTests</c> for what a chain would cost.</para>
/// </summary>
public static partial class MoonSurface
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
}
