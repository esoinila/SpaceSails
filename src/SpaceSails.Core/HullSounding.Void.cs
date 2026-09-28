namespace SpaceSails.Core;

/// <summary>
/// #251 · THE VOID AND WHAT IS SAID — the hidden void a hull is dealt, its frame and its reason, and every
/// line the sounding says.
///
/// <para>Split out of <c>HullSounding.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. <c>WhatIsInThere</c>, the class's one initialised static, stays in the
/// opening file (#1163).</para>
/// </summary>
public static partial class HullSounding
{
    /// <summary>
    /// Whether this hull is hiding one, and where. Seeded off her id, so a given ship always is or never was —
    /// a void that appeared on the second boarding would make the whole search a slot machine.
    ///
    /// <para><b>Two kinds of place, and that is the point.</b> The owner asked for interior padding specifically
    /// so the search would not be trivial: <i>"the reason I wanted padding on interior walls was to not make
    /// finding the hidden spaces too easy."</i> With hidden space only ever outboard, a captain learns in one
    /// boarding to knock along the skin and nowhere else. With bulkhead runs as well, the clue has to be read.</para>
    /// </summary>
    public static HiddenVoid? VoidFor(
        string wreckId,
        IReadOnlyList<(string Name, float X0, float X1, bool Top)> compartments,
        double spineHalfHeight, double topY, double bottomY)
    {
        ArgumentNullException.ThrowIfNull(wreckId);
        ArgumentNullException.ThrowIfNull(compartments);

        if (compartments.Count == 0)
        {
            return null;
        }

        // THE ID GOES IN AS A TAG, NOT AS A HASH CODE. The first cut folded in
        // wreckId.GetHashCode(StringComparison.Ordinal) — and .NET RANDOMISES string hashing per process, so a
        // hull's secret would have been re-rolled on every launch: found on Tuesday, honest on Wednesday, and the
        // manifest clue pointing at a wall with nothing behind it. The in-process law could never see it (it
        // re-queries the same process) and CI found it by running on a different machine. DiceRule.Seed's tag
        // path is a stable FNV-1a, which is what "seeded off her id" was always supposed to mean.
        ulong seed = DiceRule.Seed(0UL, "hull-void|" + wreckId);
        if (DiceRule.Roll(seed).Face > VoidOnARollOf)
        {
            return null;   // most hulls are exactly what they look like, and that is what makes one worth finding
        }

        int which = DiceRule.Roll(DiceRule.Seed(seed, "room"), compartments.Count).Face - 1;
        (string name, float x0, float x1, bool top) = compartments[which];

        (string holds, bool needsTheBand) =
            WhatIsInThere[DiceRule.Roll(DiceRule.Seed(seed, "holds"), WhatIsInThere.Length).Face - 1];

        // #537 slice 3 · WHICH PAPER SHE LIES IN. Rolled on its own tag so the placement rolls above and the
        // golden hull's pinned geometry are untouched by adding it — a new roll folded into the existing
        // stream would have re-cut every seeded hull in the game, and the pin exists precisely to catch that.
        ClueKind says = (ClueKind)(DiceRule.Roll(DiceRule.Seed(seed, "tell"),
                                                 System.Enum.GetValues<ClueKind>().Length).Face - 1);

        // A bulkhead run only exists where a room has a room on the other side of it, and only takes small
        // things. Anything bulky goes outboard whatever the roll says — the ship decides, not the dice.
        float[] bulkheads =
            [.. WreckLayout.InteriorBulkheads(top).Where(b => b > x0 - 0.01f && b < x1 + 0.01f)];

        bool outboard = needsTheBand
                     || bulkheads.Length == 0
                     || DiceRule.Roll(DiceRule.Seed(seed, "where"), 2).Face == 1;

        double roomDepth = System.Math.Abs(
            (top ? topY : bottomY) - (top ? -spineHalfHeight : spineHalfHeight));

        if (!outboard)
        {
            // Inside a bulkhead: the plate is on its face, and a captain stands in the room beside it.
            float bulkhead = bulkheads[
                DiceRule.Roll(DiceRule.Seed(seed, "which-bulk"), bulkheads.Length).Face - 1];
            double half = WreckLayout.BulkheadDepth / 2.0;
            double face = bulkhead > (x0 + x1) / 2f ? bulkhead - half : bulkhead + half;
            double insideY = top
                ? -spineHalfHeight - (roomDepth / 2.0)
                : spineHalfHeight + (roomDepth / 2.0);

            return new HiddenVoid(
                name, Outboard: false, bulkhead - half, bulkhead + half, top,
                face, insideY, WreckLayout.BulkheadDepth * roomDepth, holds, says);
        }

        // Outboard, in the shielding band: the plate is on the room's outboard wall, clear of its corners.
        const double clear = 2.0;
        double lo = x0 + clear, hi = x1 - clear;
        if (hi <= lo)
        {
            return null;   // too narrow a room to stand off a wall in; she keeps her secret
        }

        double plateX = lo + ((hi - lo) * (DiceRule.Roll(DiceRule.Seed(seed, "along"), 20).Face - 1) / 19.0);
        double bandHalf = VoidFrames / 2.0;

        return new HiddenVoid(
            name, Outboard: true,
            System.Math.Max(WreckLayout.TransomX, plateX - bandHalf),
            System.Math.Min(WreckLayout.ShieldingForwardEnd, plateX + bandHalf),
            top, plateX, top ? topY : bottomY,
            VoidFrames * WreckLayout.ShieldingDepth, holds, says);
    }

    /// <summary>Her paperwork's lie, as the same <see cref="Discrepancy"/> the geometry rule produces — one
    /// type, several sources, so a panel that can show one can show the others without knowing which it got.</summary>
    public static Discrepancy AsDiscrepancy(in HiddenVoid hidden) =>
        new(ReasonFor(hidden), hidden.X0, hidden.X1, hidden.Top, hidden.AreaSquareDu);

    /// <summary>Her frame number at a station, counted from the transom forward the way a builder counts.
    /// One place that arithmetic lives, so a clue and a placard can never disagree about which frame a wall
    /// is on.</summary>
    public static int FrameNumber(double x) =>
        (int)System.Math.Round(x - WreckLayout.TransomX);

    /// <summary>
    /// WHAT DOES NOT ADD UP, in the captain's own terms and never as an answer. Each arm states a
    /// measurement and stops; the conclusion is the player's, which is the difference between this and a
    /// treasure map (#533, and the law <c>TheClueStatesAMeasurementAndDrawsNoConclusion</c> pins).
    /// </summary>
    private static string ReasonFor(in HiddenVoid hidden) => hidden.Says switch
    {
        ClueKind.SkippedFrame =>
            $"her frames are numbered from the transom forward in one hand, and the plate on {hidden.NearRoom}'s " +
            $"bulkhead runs {FrameNumber(hidden.X0)} straight on to {FrameNumber(hidden.X1)} without ever " +
            $"writing down the {FrameNumber(hidden.X1) - FrameNumber(hidden.X0)} in between",

        ClueKind.StandingLoad =>
            $"the board is dead and every breaker on it is cold except one, and the bus that one carries runs " +
            $"outboard of {hidden.NearRoom} to a section of her that nobody has been in for eleven years",

        _ => hidden.Outboard
                ? $"her shielding is booked by the section, and the run outboard of {hidden.NearRoom} holds a " +
                  "third of what every other section of it does"
                : $"her bulkhead schedule books every frame at one thickness, and the one {hidden.NearRoom} " +
                  "shares is a hand's width off it",
    };

    /// <summary>
    /// WHAT THE SAME PAPER SAYS ON A HULL THAT IS NOT LYING — or on a lying hull whose lie is somewhere else.
    /// Every clue kind owes one, and it has to be an honest dead end: a document that only speaks up when
    /// there is something to find has stopped being a clue and become a pointer, and a captain would learn
    /// in two boardings to read the silence rather than the page.
    /// </summary>
    public static string HonestLine(ClueKind kind) => kind switch
    {
        ClueKind.SkippedFrame =>
            "📐 Her frame numbers run from the transom to the bow without a gap in them, in one hand, on a " +
            "plate that has been painted over twice.",
        ClueKind.StandingLoad =>
            "📐 The board is dead and every breaker on it is as cold as the next one.",
        _ =>
            "📐 Her shielding is booked section by section, and every section holds the same.",
    };

    // ── What is said ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The prompt, naming the cost before it is spent — including the part that is not time.</summary>
    public static string OfferLine(Method method) => method == Method.Sounder
        ? "📡 Put the sounder on the plating — 5 s, and it will be heard the length of her."
        : "✊ Knock along the frames — 12 s, quietly, and you have to be right on it.";

    /// <summary>Said while the clock runs, because standing still aboard a hull is the actual price.</summary>
    public static string WorkingLine(Method method) => method == Method.Sounder
        ? "📡 The sounder rings off the frames, one note per bay. Anything aboard with ears has the bearing."
        : "✊ Knuckle, listen, move a hand's width, knuckle again. Slow, and nobody else can hear it.";

    /// <summary>…and abandoned. The noise is not refunded.</summary>
    public const string AbandonedLine =
        "The reading is gone the moment you move off the spot. What you already made of it, she already heard.";

    /// <summary>What each reading tells the captain — and the ODD one is written to point without promising.</summary>
    public static string ReadingLine(Reading reading) => reading switch
    {
        Reading.Hollow =>
            "🕳 It rings like a drum. There is nothing behind this plate but room, and somebody went to the " +
            "trouble of making it look otherwise.",
        Reading.Odd =>
            "🔉 Not right. Not hollow either — the note goes dead a little too soon, the way it does near an " +
            "edge. Whatever is off, it is not quite here.",
        _ => "🔇 Frames, insulation, and ship behind it. This wall is only a wall.",
    };

    /// <summary>How a discrepancy is offered: as a measurement, never as an answer. A captain who is told
    /// "there is a void aft of the deep hold" has been given the find; one who is told the deep hold is four
    /// frames longer than the hold opposite it has been given the QUESTION.</summary>
    public static string ClueLine(in Discrepancy discrepancy) =>
        $"📐 {discrepancy.Reason}. Somewhere in those {discrepancy.AreaSquareDu:0} square metres, her plans and " +
        "her plating disagree.";

    /// <summary>The plate coming off, and what a captain finds. The find line NEVER says what it is worth — the
    /// contents describe themselves and the captain draws the conclusion, which is the #533 discipline applied at
    /// the only moment the game could get away with breaking it.</summary>
    public static string FoundItLine(in HiddenVoid hidden) =>
        $"🕳 The plate comes away from {hidden.NearRoom}'s " +
        (hidden.Outboard ? "outboard wall" : "bulkhead") +
        " in one piece — it was never welded, only made to look it. Behind it " +
        (hidden.Outboard ? "the shielding simply stops" : "the pipework stops short") +
        $", and there is a space in her nobody drew. {hidden.Holds}";

    /// <summary>And the honest warning, for a captain who would rather sound the whole ship than measure it.</summary>
    public static string BlindSearchLine(Method method, double hullArea) =>
        $"📐 Sounding her end to end would be {SpotsToCover(method, hullArea)} spots and " +
        $"{SecondsToCover(method, hullArea):0} seconds of standing still" +
        (method == Method.Sounder ? " — every one of them audible from anywhere aboard." : ", quietly.");
}
