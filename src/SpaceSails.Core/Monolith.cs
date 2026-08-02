using System;

namespace SpaceSails.Core;

/// <summary>
/// #586 · THE MONOLITH — the thing you walk out there to SEE.
///
/// <para>Owner, standing at it after the ground rebuild: <i>"let's have gen AI image at the monolith and
/// some items appearing there now and then ... it is supposed to be impressive... now it looks like a box in
/// closet."</i></para>
///
/// <para>He is right, and the measurement is brutal. The canon monolith was a slab
/// <b>2.4 by 5 deck units</b> — the captain is 1.4 du across, so the ancient artefact at the heart of the
/// canon ground was about two captains tall, sitting in a field 310 by 260. It is the deep commitment anchor
/// of the whole site: the reason to make the long walk, the thing the nerve/sight system keys off, and the
/// only object on Miranda that is older than everything else in the game put together. It read as a
/// cupboard.</para>
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
    /// <summary>Half-width of the slab itself. Six deck units across — four captains wide, so it reads as a
    /// WALL of something rather than a box, while still fitting the maze cell it has always stood in (the
    /// canon rows at ±6 and −4 are untouched: the maze is canon and stays exactly as authored).</summary>
    public const double HalfWidth = 3.0;

    /// <summary>Half-height of the slab. Seven deep, filling its cell without crossing the maze rows.</summary>
    public const double HalfHeight = 3.5;

    /// <summary>The cleared apron it stands on — drawn, never collided (<c>DeckPlan.Scenery</c>), because a
    /// ring you cannot walk across would turn the landmark into a wall and the pilgrimage into a puzzle.
    ///
    /// <para>This is what does the work the slab cannot: ground that is visibly SWEPT, a made platform, in a
    /// field where everything else is rubble and drift. You can see it from a long way off and it tells you
    /// somebody cared about this spot.</para></summary>
    public const double ApronRadius = 15.0;

    /// <summary>How many segments the apron ring is drawn with — enough to read as a circle at this scale
    /// without flooding the scenery list.</summary>
    public const int ApronSegments = 28;

    /// <summary>Four stubs at the compass points just inside the apron: the remains of an approach. Small,
    /// solid, and unmistakably PLACED — the difference between a rock and a ruin.</summary>
    public const double MarkerRing = 11.0;
    public const double MarkerHalf = 0.9;

    /// <summary>The body the slab stands on. It is ONE object in the whole system, not a species.</summary>
    public const string BodyId = "miranda";

    /// <summary>
    /// Does the monolith stand on THIS ground? Only on <see cref="BodyId"/>'s canon site — site 0, the
    /// Wild Plain, the empty-salt ground <c>SurfaceLayout.For</c> routes to the authored maze for. Every
    /// other site on the same moon is seeded ground with no slab in it, and every other body's deep anchor
    /// is its own thing (Luna's mass-driver muzzle, a seeded plinth elsewhere).
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
