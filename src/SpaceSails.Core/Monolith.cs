using System;

namespace SpaceSails.Core;

/// <summary>
/// #586 / #649 · THE MONOLITH — the thing you walk out there to SEE. There is exactly ONE of it, and it
/// stands on PHOBOS.
///
/// <para>Owner, standing at it after the ground rebuild: <i>"let's have gen AI image at the monolith and
/// some items appearing there now and then ... it is supposed to be impressive... now it looks like a box in
/// closet."</i></para>
///
/// <para><b>#649 · THE UNIQUENESS RULING (owner, 2026-08-03, recorded in
/// <c>docs/worldbuilding-notes.md</c> §8).</b> <i>"There is ONE monolith. Not a class of object, not a kind
/// of landmark a generator can roll twice. If two grounds both call something 'the monolith', one of them is
/// wrong and has to be renamed — the word is reserved."</i></para>
///
/// <para>Two grounds did. The MAP CARDS have paced off <c>the monolith</c> on <b>Phobos</b> since #164 —
/// <see cref="Landmarks.PhobosMonolith"/>, the genuine 85 m boulder on the Stickney rim, the flagship
/// treasure island and the port the worldbuilding notes (§7) are built around. The DRAWN SLAB stood on
/// <b>Miranda</b>, at the heart of the authored maze, because that is where the very first hand-built ground
/// happened to put it. So the game said PHOBOS on the card in your pocket and drew the thing on a different
/// moon, and #648 could only unify the PREDICATE — every caller now asks one function — not the FACT.</para>
///
/// <para>The fact is settled here: <see cref="BodyId"/> is <c>phobos</c>. The slab, its picture, the
/// once-in-a-life nerve hit, the selfie, the swept apron, the foot-offering and the map cards all resolve to
/// the one object on the one ground. Miranda's maze keeps its maze and its centre is
/// <see cref="FalseSlab"/> — a made thing, quarried and mortared, which is a different kind of object
/// entirely and never borrows the word.</para>
///
/// <para><b>The fix is NOT to draw a bigger box.</b> The deck plan is a crude grid on purpose (the owner's
/// own ruling — the NetHack look is the aesthetic, not a limitation), and on a crude grid every rectangle is
/// a rectangle. What makes a place impressive there is CEREMONY: an approach that is visibly cleared, a
/// plinth it stands on, a slab wide enough to be a wall rather than a crate, a picture when you put your
/// hand on it, and — the owner's second ask — the fact that <b>things are left at its foot</b>, and they are
/// not the same things they were last time.</para>
///
/// <para>That last part is the whole trick. A landmark that never changes is scenery you visit once. A
/// landmark where somebody has been since you were last here is a place with a life of its own, and it costs
/// nothing but a seed.</para>
/// </summary>
public static class Monolith
{
    // ── HOW BIG IT IS ───────────────────────────────────────────────────────────────────────────────────
    //
    // #649 · Owner: "The Phobos one's dimensions were HUGE and it should not live in a boxed backyard but
    // show more of its size and not having been built by us at least."
    //
    // Every number below comes off ONE fact, and it is a fact the game already held: the landmark the
    // treasure maps are minted from records the boulder's height, 85 m (Landmarks.PhobosMonolith, #164,
    // from the real Mars Global Surveyor object on the Stickney rim). Nothing here is typed in. That is
    // deliberate and it is the house law rather than tidiness — bug class 1 is "numbers typed into a
    // renderer that nothing derives or checks, found wrong three times out of three", and a landmark whose
    // canon size is 85 m and whose drawn size was six deck units is that bug in its purest form: the fact
    // and the picture disagreeing by a factor of twenty, in the open, for months.

    /// <summary>The one fact: the real object's height, off the landmark the map cards are minted from.</summary>
    public static double HeightMetres => Landmarks.PhobosMonolith.HeightMeters;

    /// <summary>Its height on the grid — 121 deck units, which is not a number that can be drawn top-down and
    /// is the whole difficulty. What CAN be drawn is what a thing that tall does to the ground around it:
    /// its footprint, and its shadow.</summary>
    public static double HeightDu => SurfaceScale.DeckUnits(HeightMetres);

    // The proportions are 1 : 4 : 9 — the squares of the first three integers, the one set of ratios that
    // comes out identical in every unit system anybody could ever measure it in. It is an homage and it is
    // doing real work: a slab at 1:4:9 has a footprint no quarry produces (a nine-to-one plan, sheer, with
    // the long faces dead parallel) and no yard would cut, which is the owner's "not having been built by
    // us" expressed as geometry instead of as a sentence. Nothing in the game ever states the ratio.

    /// <summary>The width face, as a fraction of the height.</summary>
    public const double WidthRatio = 4.0 / 9.0;

    /// <summary>The depth face, as a fraction of the height.</summary>
    public const double DepthRatio = 1.0 / 9.0;

    /// <summary>Half the slab's width on the plan — about 27 du, so the stone alone is four fifths of
    /// everything the camera can hold at once (<c>DeckView</c> frames roughly 64 du). It was 3.</summary>
    public static double HalfWidth => HeightDu * WidthRatio / 2.0;

    /// <summary>Half the slab's depth on the plan — about 7 du. Deliberately the SHORT axis: it keeps the
    /// deep field behind the slab walkable, and it is what makes the thing read as a slab standing edge-on to
    /// the walk rather than as a block sitting in the way.</summary>
    public static double HalfHeight => HeightDu * DepthRatio / 2.0;

    /// <summary>The cleared apron it stands on — drawn, never collided (<c>DeckPlan.Scenery</c>), because a
    /// ring you cannot walk across would turn the landmark into a wall and the pilgrimage into a puzzle.
    ///
    /// <para>This is what does the work the slab cannot: ground that is visibly SWEPT, a made platform, in a
    /// field where everything else is rubble and drift. You can see it from a long way off and it tells you
    /// somebody cared about this spot.</para></summary>
    public static double ApronRadius => HalfWidth * 1.6;

    /// <summary>How many segments the apron ring is drawn with — scaled with the ring, so the circle reads as
    /// a circle at any size instead of as a polygon that grew.</summary>
    public static int ApronSegments => 72;

    /// <summary>Four stubs at the compass points just inside the apron: the remains of an approach. Small,
    /// solid, and unmistakably PLACED — the difference between a rock and a ruin. Outside the slab's own ends
    /// by construction, because <see cref="ApronRadius"/> is.</summary>
    public static double MarkerRing => ApronRadius * 0.82;
    public static double MarkerHalf => 1.6;

    /// <summary>Everything the object and its ceremony occupy, as ONE claim that every placer on the ground
    /// asks for — the shelters, the outlying huts, the outpost, the lift head.
    ///
    /// <para>This is the part that stops "make it bigger" from being a disaster. Four separate placers used
    /// to keep the deep landmark at arm's length with a number each, all of them sized for a six-du slab: a
    /// flat 40 × 30 box in <see cref="SurfaceShelter"/>, a flat 26 in the outlying-structure loop, the lift
    /// head's own clash list, and the seeded ledger. Growing the stone twenty-fold under four constants that
    /// had never heard of it would have put a pressure drum — the building a captain walks to when their air
    /// runs out — inside a wall. One claim, published by the thing that makes it.</para></summary>
    public static double KeepOutRadius => ApronRadius + 10.0;

    /// <summary>Where the swept apron and the slab sit on a given ground, or nothing at all. The single
    /// question every placer asks; there is no second way to find out where the monolith is.</summary>
    public static (double X, double Y, double R)? KeepOutOn(
        string? bodyId, string? siteSalt, in SurfaceLayout.Field field) =>
        StandsOn(bodyId, siteSalt) ? (field.AnchorX, field.AnchorY, KeepOutRadius) : null;

    // ── AND HOW YOU KNOW IT IS THERE FROM THE OTHER END OF THE FIELD ────────────────────────────────────
    //
    // Owner: it must "read as large from a long way off, and keep getting larger as you walk, the way only
    // genuinely big things do." A top-down deck plan cannot draw a height at all — but a 121 du object with
    // the sun near the horizon throws a shadow LONGER THAN THE FIELD, and that is a thing the plan can draw.
    // So the captain steps off the pad into a lane of dark that runs off the bottom of the world, and the
    // only way to find out what is casting it is to walk down it.

    /// <summary>How high the sun stands over this ground. Low, because Phobos's day is seven and a half hours
    /// and any given hour of it is a low one; and because a landmark is not a landmark at noon.</summary>
    public const double SunAltitudeDegrees = 18.0;

    /// <summary>How far the shadow reaches up-field — around 370 du, which is longer than the whole walked
    /// field is deep. It is clamped to the ground when it is drawn; the point of the number is that there is
    /// nowhere on this moon you can stand and not be in it.</summary>
    public static double ShadowLengthDu =>
        HeightDu / System.Math.Tan(SunAltitudeDegrees * System.Math.PI / 180.0);

    /// <summary>How wide the shadow is at its far end, relative to the slab. A shadow cast by a low sun
    /// spreads: the penumbra of a 121 du object at this range is not subtle.</summary>
    public const double ShadowSpread = 1.35;

    /// <summary>How close you have to be for the thing to resolve out of the dark — the range the
    /// once-in-a-life nerve hit and the FirstMonolith selfie fire at.
    ///
    /// <para>Derived, not tuned. It was a flat 26 du typed into <c>Map.Surface</c>, sized by eye for a slab
    /// six deck units across; at the real size that would have put the biggest fright in the captain's life
    /// at the moment they walked into the stone. Sight is a function of how big the thing is, so this is
    /// one: three fifths of its height, about 73 du, which is a little over two screens — far enough that
    /// it resolves while it is still a shape, close enough that the resolving is what does it.</para></summary>
    public static double SightRangeDu => HeightDu * 0.6;

    /// <summary>How close you have to be for it to stop being a shape and become the sky — the range
    /// <see cref="ApproachLine"/> lands at, which is the moment you cross onto the swept ground.</summary>
    public static double ApproachRangeDu => ApronRadius + HalfHeight;

    /// <summary>The body the slab stands on. It is ONE object in the whole system, not a species.
    ///
    /// <para>#649 · <b>PHOBOS</b>, and it is not a free choice: <see cref="Landmarks.PhobosMonolith"/> has
    /// paced every treasure map off <i>the monolith</i> on this moon since #164, and the worldbuilding notes
    /// build a whole port around it (§7 — deals in its shadow, hoards in its regolith, the beanstalk anchor
    /// bolts that went in suspiciously easily). Reading the same word off two moons was the bug; the card
    /// was never the half that was wrong.</para></summary>
    public const string BodyId = Landmarks.MonolithBodyId;

    /// <summary>
    /// Does the monolith stand on THIS ground? Only on <see cref="BodyId"/>'s canon site — site 0, the
    /// Wild Plain, the empty-salt ground <c>SurfaceLayout.For</c> routes to the authored plan for. Every
    /// other site on the same moon is seeded ground with no slab in it, and every other body's deep anchor
    /// is its own thing (Miranda's false slab, Luna's mass-driver muzzle, a seeded plinth elsewhere).
    ///
    /// <para><b>Why this is a function and not three literals.</b> The slab, its picture, the once-in-a-life
    /// nerve hit and the first-monolith selfie each decided this for themselves, and they disagreed: the
    /// geometry was built for <c>miranda</c> + empty salt, the console and the foot-offering for any
    /// <c>miranda</c> site, and the nerve/selfie for ANY body at all — they keyed off the deep anchor's
    /// coordinates, which every ground has. So walking up to Luna's launch muzzle spent the marquee
    /// once-in-a-life beat, narrated as <i>"the monolith resolves out of the dark"</i>, on a broken machine
    /// — and because that flag is kept for life, the real slab could never deliver it afterwards. That is
    /// the borrowed-prose bug (#574) wearing a landmark, which the renderer's own comment forbids two lines
    /// from where it happened. One predicate; everything asks it.</para>
    /// </summary>
    public static bool StandsOn(string? bodyId, string? siteSalt) =>
        string.Equals(bodyId, BodyId, StringComparison.Ordinal) && string.IsNullOrEmpty(siteSalt);

    /// <summary>The label on the ground.</summary>
    public const string ConsoleLabel = "▮ THE MONOLITH";

    /// <summary>The picture, on [E]. Not a diagram of the slab — the moment of standing at it.</summary>
    public const string ArtUrl = "art/monolith.jpg";

    /// <summary>What the card says beside the picture.
    ///
    /// <para>Explains NOTHING, on purpose. The Old Ones' origin is canon and is never stated by a card or
    /// confirmed by a sensor (owner ruling: the Reevers are failed restores, and the game must never say so).
    /// The monolith is older than the question and does not answer it. What the copy can do is give the
    /// captain something true to hold: it is not natural, it is not theirs, and it was here first.</para></summary>
    public const string Lore =
        "You put a glove on it because everyone does.\n\n" +
        "No seam. No weathering — and this ground has had four billion years to weather it. No dust holds to " +
        "the face, though it has drifted deep against the foot, which means the face has been moving, or the " +
        "dust has, and neither is a comfortable thing to have worked out while standing here.\n\n" +
        "Your suit reports the surface at the same temperature as the regolith, to the limit of what it can " +
        "measure. That is the only fact anybody has ever brought home from this spot. The survey that named " +
        "it filed one line and never came back: STANDING. NOT OURS. NOT NATURAL.\n\n" +
        "Whatever it was for, it was for something. It is still doing it.";

    // ── What is at its foot this visit ──────────────────────────────────────────────────────────────────
    //
    // Owner: "some items appearing there now and then". Seeded on the site AND a slow clock bucket, so the
    // ground changes between visits without a die being thrown at the player every frame — and so a captain
    // who comes back the same afternoon finds what they left, which is the object-persistence law he set
    // ("the long walk should walk with expected object persistence").

    /// <summary>How long one "visit window" lasts in sim-seconds. Long enough that a single excursion never
    /// sees the foot of the monolith change under it; short enough that coming back later is worth it.</summary>
    public const double EpochSeconds = 4_000.0;

    /// <summary>Which window a moment falls in. Pure — no clock is read in Core, the caller passes sim time.</summary>
    public static long EpochAt(double simTime) =>
        (long)Math.Floor(Math.Max(0, simTime) / EpochSeconds);

    /// <summary>What somebody left at the foot of the monolith. <see cref="Nothing"/> is the common case and
    /// is load-bearing: if there were always something, the walk would be a shopping trip rather than a
    /// pilgrimage, and finding something would stop meaning anything.</summary>
    public enum Offering { Nothing, Tools, Marker, Remains, Tribute }

    /// <summary>What is at the foot this window. Roughly half of all windows are empty.</summary>
    public static Offering AtTheFoot(string bodyId, string siteSalt, long epoch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        return DiceRule.Roll(DiceRule.Seed($"monolith:foot:{bodyId}:{siteSalt}:{epoch}"), 8).Face switch
        {
            1 or 2 or 3 or 4 => Offering.Nothing,
            5 => Offering.Tools,
            6 => Offering.Marker,
            7 => Offering.Remains,
            _ => Offering.Tribute,
        };
    }

    /// <summary>The console label for whatever is lying there — named vaguely, because half the point is
    /// walking over to find out.</summary>
    public static string FootLabel(Offering what) => what switch
    {
        Offering.Tools => "🧰 SOMETHING LEFT HERE",
        Offering.Marker => "🪧 SOMETHING LEFT HERE",
        Offering.Remains => "🦴 SOMETHING LEFT HERE",
        Offering.Tribute => "📿 SOMETHING LEFT HERE",
        _ => "",
    };

    /// <summary>What the captain finds. Every line is somebody ELSE's visit — the monolith itself never
    /// speaks, never reacts, and is never confirmed to have noticed anything.</summary>
    public static string FootLine(Offering what, string bodyId, string siteSalt, long epoch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        ulong seed = DiceRule.Seed($"monolith:line:{bodyId}:{siteSalt}:{epoch}");
        string[] pool = what switch
        {
            Offering.Tools => ToolLines,
            Offering.Marker => MarkerLines,
            Offering.Remains => RemainsLines,
            Offering.Tribute => TributeLines,
            _ => [],
        };
        return pool.Length == 0 ? "" : pool[(int)(seed % (ulong)pool.Length)];
    }

    private static readonly string[] ToolLines =
    [
        "🧰 A cutting rig, laid down neatly and never picked up. The cell is flat, the blade is unworn, and " +
        "somebody coiled the lead before they left it — which is not what you do when you are in a hurry.",
        "🧰 A survey tripod with no instrument on it, feet sunk two centimetres into the dust. Whoever set it " +
        "up levelled it properly first.",
        "🧰 A sample case, open, empty, and free of dust inside. It has not been open long.",
    ];

    private static readonly string[] MarkerLines =
    [
        "🪧 A stake driven into the regolith with a plate wired to it. The plate has been scoured blank — " +
        "not corroded, scoured, by hand, thoroughly, by somebody who wanted it read once and never again.",
        "🪧 A cairn of five stones, too deliberate to be an accident and too small to be a marker for anything " +
        "you could find. It has been rebuilt at least twice; the older stones are set deeper.",
        "🪧 Somebody has scratched a line in the dust from the foot of the slab out to the north, arrow-straight " +
        "for as far as your lamp carries. It does not point at anything you can see.",
    ];

    private static readonly string[] RemainsLines =
    [
        "🦴 A suit glove, palm-down, fingers curled. Nothing in it. The wrist seal was opened from the outside, " +
        "which takes two hands and a reason.",
        "🦴 Bootprints, a lot of them, all facing the slab. None leading away. The dust is deep enough here to " +
        "hold a print for a very long time, and that is the only reassuring thought available.",
        "🦴 A helmet, visor to the ground. You do not turn it over, and you spend the walk back deciding whether " +
        "that was sense or cowardice.",
    ];

    private static readonly string[] TributeLines =
    [
        "📿 Somebody has left a ration bar at the foot of it, upright in the dust like a candle. Unopened. The " +
        "wrapper is a brand that stopped being made before you were born.",
        "📿 A child's toy — a die-cast tug boat, paint worn to bare metal at the bow. Nobody brings a thing like " +
        "that this far by accident.",
        "📿 A ship's plate, unbolted from somewhere and carried here: hull number, yard, year. Whoever pulled it " +
        "off their own boat meant this to stand in for something.",
    ];

    /// <summary>Said the first time a captain gets close enough for the slab to fill the view — before any
    /// console, before any prompt. The one beat of pure scale the crude grid cannot draw by itself.</summary>
    public const string ApproachLine =
        "▮ The ground goes flat and swept, and the thing at the middle of it stops being a shape on the " +
        "tracker and becomes a wall across the sky. Your lamp does not reach the top of it.";
}
