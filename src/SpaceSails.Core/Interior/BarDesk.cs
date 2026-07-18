namespace SpaceSails.Core.Interior;

/// <summary>
/// Where the bar counter sits <b>on the desk drawn in each haven's backdrop art</b> (owner ruling,
/// Saturday-evening playtest 2026-07-18, the "Evening wind" plan): "check all bars have the barkeep in
/// correct position. The image, as it always does, has a bar desk. Then the bar-keep service position —
/// one presses E at to get a drink — needs to be AT that desk. It should not be at the middle of the
/// empty floor in the pic. Not on top of a window — and the bar to be on top of the bar in the picture."
///
/// <para>The #247 first pass assumed <i>one</i> shared counter for all four bars and jammed it up against
/// the far wall (three deck-units off the window), so the barkeep and the pacing droid rendered up in the
/// window/ceiling band of every backdrop rather than down on the counter. This is the per-image
/// correction: each bar reads its desk straight off <i>its own</i> picture.</para>
///
/// <para>Every bar backdrop is drawn by <c>HavenInterior</c> at exactly the same size — a box
/// <see cref="BoxWidth"/> deck-units across by <see cref="BoxDepth"/> deep, its left edge at
/// <see cref="BoxLeft"/>, its far (top) edge the bar's window wall. So a spot in the picture is a pair of
/// fractions: <paramref name="ServiceU"/> across (0 = the left wall with the back-bar shelves, 1 = the
/// right wall) and <paramref name="ServiceV"/> into depth (0 = the far window wall, 1 = the hall-door
/// end). All four arts turn out to draw the same scene — bottle shelves and counter down the LEFT,
/// planet window top-right, patron tables to the right — so the numbers sit close together, but each is
/// read from its own frame and pinned in <c>BarkeepTests</c>. Pure data in Core, one tested truth the
/// client leans on rather than hand-placing pixels.</para>
/// </summary>
public sealed record BarDesk(
    string BodyId,
    float ServiceU, float ServiceV,
    float CounterHalfWidth)
{
    // The bar backdrop box, shared by every haven (HavenInterior draws each bar art at exactly this
    // size: BarLeft..BarRight wide, HallTopY..BarTopY deep). Fractions map linearly onto it. These are
    // the raw design constants — BoxLeft = BarLeft, BoxWidth = BarRight − BarLeft, BoxDepth =
    // BarTopY − HallTopY — kept here (not the hall-derived floats) so the mapping needs no frame math.
    public const float BoxLeft = -14f;
    public const float BoxWidth = 33f;  // BarRight (19) − BarLeft (−14)
    public const float BoxDepth = 22f;  // BarTopY − HallTopY

    /// <summary>The service point's deck X — where you belly up and the barkeep tends.</summary>
    public float ServiceX => BoxLeft + ServiceU * BoxWidth;

    /// <summary>The service point's deck Y as an offset ABOVE the hall's north wall: the caller adds it
    /// to <c>HallTopY</c>. 0 = the hall door (front of the bar), <see cref="BoxDepth"/> = the far window
    /// wall. Reading the desk mid-room keeps the keep off both the window (behind) and the empty floor
    /// (in front), exactly the owner's two "nots".</summary>
    public float ServiceYOffset => BoxDepth * (1f - ServiceV);
}

/// <summary>
/// The bar desks of the walkable havens, keyed by the station's body id — the same ids
/// <c>HavenInterior</c> builds interiors for and <c>Barkeeps</c> pours behind. One desk per bar; each
/// read off that bar's own backdrop (2026-07-18, per-image ruling). The four arts draw near-identical
/// cantinas, so the fractions cluster, but they are verified and stored one at a time.
/// </summary>
public static class BarDesks
{
    private static readonly BarDesk[] All =
    [
        // THE ROADSTEAD BAR — Mars (art/the-roadstead-bar.jpg). The long metal counter sweeps up the
        // LEFT from the near stools toward the shelves; the Rusty-Bolt service stretch reads a touch
        // right of the shelves, mid-depth. The Mars window is top-right, clear of the desk.
        new("the-space-bar", 0.270f, 0.620f, 4.5f),

        // THE CINDER LOUNGE — Venus (art/cinder-roost-bar.jpg). Glowing racks and counter down the LEFT,
        // sulphur-cloud window top-right with a patron table in front of it. Keep on the left counter.
        new("cinder-roost", 0.260f, 0.600f, 4.5f),

        // THE RINGSIDE BAR — Saturn (art/ringside-bar.jpg). Sticker-plastered counter along the LEFT,
        // Saturn framed dead-centre-right; the working length reads left-of-centre, mid-depth.
        new("ringside-exchange", 0.260f, 0.615f, 4.5f),

        // THE EARTHRISE BAR — Selene Gate / Luna (art/selene-gate-bar.jpg, #352). Bottle shelves and the
        // long metal counter run down the LEFT, the keep bent to his rag mid-counter; the Earthrise window
        // (home over the mare) fills the upper-RIGHT with patron tables under it. The service stretch reads
        // a touch more central than the inner-system bars — the counter angles out further — but still well
        // left of centre and clear of the window. Eyeballed off this frame; pinned in BarkeepTests.
        new("selene-gate", 0.300f, 0.585f, 4.5f),

        // THE TILT BAR — Uranus (art/the-tilt-bar.jpg). Lit niche shelves and a long counter down the
        // LEFT (a little further forward here), the big Uranus window to the right and a small dim port
        // mid-frame — the keep sits left of both, on the counter.
        new("the-tilt", 0.250f, 0.580f, 4.5f),
    ];

    /// <summary>The desk for a station body, or null if that berth has no bar (no walkable interior).</summary>
    public static BarDesk? For(string bodyId) => System.Array.Find(All, d => d.BodyId == bodyId);

    /// <summary>Every bar desk — for tests and any "where does each counter sit" listing.</summary>
    public static System.Collections.Generic.IReadOnlyList<BarDesk> AllDesks => All;
}
