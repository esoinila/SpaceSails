namespace SpaceSails.Core;

/// <summary>
/// #537 · KNOCKING ON HER WALLS. Owner, across three messages that between them specify the whole mechanic:
/// <i>"We need to device a method to seach for hidden stuff on the ship… some kind of scanner to see what is a fake
/// wall etc where there is something to get at. Maybe a combi of detect tool that scans on timer like 5 seconds at
/// a spot snd a tool to get at it. That way we can gamify the seach."</i> Then: <i>"Sweeping could cost time… like
/// pumping vacuum does… idea is to know where to do it… so some logic on selection based on map or other
/// clues."</i> And then the one that closes it: <i>"Also it might be noisy to say knock on walls etc."</i>
///
/// <para><b>Those three sentences are one design.</b> A search that costs time makes WHERE the decision rather
/// than WHETHER — and a search that makes NOISE means you cannot buy your way out of thinking by simply sounding
/// every square metre, because on this hull noise is what wakes things (<c>MakeNoiseAboard</c>) and what brings
/// professionals down the corridor (<c>AlertSweepersToNoise</c>). Blind sweeping does not merely take a long time;
/// it takes a long time and then kills you.</para>
///
/// <para><b>So the deduction has to be real, and it has to be readable off the map.</b> That is what
/// <see cref="Discrepancy"/> is for: her compartments are drawn on the mimic board from the same rectangles the
/// deck is built from, and a hidden void is <b>space that does not add up</b>. A room shorter than its counterpart
/// across the spine is a fact a captain can SEE, without a console, without a die, and without being told. The
/// same discipline as the lifeboat cradles you can count from the doorway (#488) and the breadcrumb tells
/// (#533).</para>
///
/// <para><b>Two gears, and it is the pump's own asymmetry.</b> The roughing pump does 95% of the work and the tail
/// costs 50 s for nothing, which is a decision inside a machine cycle. Here: knuckles are quiet, slow and
/// short-ranged; the sounder is loud, quick and reaches. <b>You pay in time or you pay in noise.</b> Nobody gets
/// to pay in neither, and which one you can afford depends entirely on what else is aboard.</para>
/// </summary>
public static class HullSounding
{
    /// <summary>How a captain asks a bulkhead whether it is lying.</summary>
    public enum Method
    {
        /// <summary>The back of a glove, one frame at a time. Nearly silent, slow, and you have to be almost on
        /// top of it — the method for a hull with something awake in it.</summary>
        Knuckles,

        /// <summary>A powered sounder held against the plating. Quick, reaches, and audible the length of the
        /// ship — the method for a hull you believe is empty, or one you are in a hurry on.</summary>
        Sounder,
    }

    /// <summary>What one completed sounding says.</summary>
    public enum Reading
    {
        /// <summary>Frames, insulation, and the ship behind it. Nothing here.</summary>
        Solid,

        /// <summary>
        /// SOMETHING IS OFF. Not a find — a direction. The reason this exists rather than a bare hollow/solid:
        /// with only two answers a search is a lottery over N spots, and with three it CONVERGES. A captain who
        /// hears "odd" knows to spend the next sounding nearby instead of somewhere else entirely, which is the
        /// difference between gamifying a search and rolling dice at it.
        /// </summary>
        Odd,

        /// <summary>Empty space where the plans say ship. You have found it; getting into it is another tool.</summary>
        Hollow,
    }

    // ── What each gear costs ──────────────────────────────────────────────────────────────────────────

    /// <summary>How long one sounding takes, standing still. The owner's own five seconds for the powered tool;
    /// knuckles are more than twice that because a man tapping frames by hand is slow and that is the point.
    /// FLAGGED for tuning.</summary>
    public static double Seconds(Method method) => method == Method.Sounder ? 5.0 : 12.0;

    /// <summary>How far one sounding reaches, in deck units. The gap between the two is the whole trade — the
    /// sounder covers four times the area per spot, and announces itself while doing it.</summary>
    public static double Radius(Method method) => method == Method.Sounder ? 4.0 : 2.0;

    /// <summary>
    /// How far the noise of it carries. These are the wreck lane's OWN two earshots (<c>QuietEarshot</c> 13 and
    /// <c>LoudEarshot</c> 26 in the client) rather than new numbers, so a knock is exactly as loud as dogging a
    /// hatch by hand and the sounder is exactly as loud as running a pump. Nothing about searching gets its own
    /// private acoustics.
    /// </summary>
    public static double Earshot(Method method) => method == Method.Sounder ? 26.0 : 13.0;

    /// <summary>How much of an <see cref="Odd"/>-reading band sits outside the flat find. Beyond the radius a
    /// void still colours the return for this multiple of it — the convergence signal.</summary>
    public const double OddBandFactor = 1.75;

    /// <summary>What one sounding at a point says about a void centred somewhere else. Pure geometry: inside the
    /// reach is a find, inside the odd band is a direction, past that is an honest nothing.</summary>
    public static Reading Read(Method method, double x, double y, double voidX, double voidY)
    {
        double dx = voidX - x, dy = voidY - y;
        double range = System.Math.Sqrt((dx * dx) + (dy * dy));
        double reach = Radius(method);

        return range <= reach ? Reading.Hollow
             : range <= reach * OddBandFactor ? Reading.Odd
             : Reading.Solid;
    }

    /// <summary>
    /// A SOUNDING THAT WAS INTERRUPTED IS A SOUNDING THAT DID NOT HAPPEN. It is the pump's rule and the archive
    /// node's rule: the clock buys the answer, so walking away mid-cycle buys nothing. What it does NOT undo is
    /// the noise already made — the ship heard the first half.
    /// </summary>
    public static bool Completed(double heldSeconds, Method method) => heldSeconds >= Seconds(method);

    /// <summary>How many spots it takes to cover an area with one gear, at best packing. Used by Lab 44 and by
    /// the panel that tells a captain what a blind search would cost them.</summary>
    public static int SpotsToCover(Method method, double areaSquareDu)
    {
        double perSpot = System.Math.PI * Radius(method) * Radius(method);
        return (int)System.Math.Ceiling(System.Math.Max(0, areaSquareDu) / perSpot);
    }

    /// <summary>…and what that costs in seconds of standing still.</summary>
    public static double SecondsToCover(Method method, double areaSquareDu) =>
        SpotsToCover(method, areaSquareDu) * Seconds(method);

    // ── Where to look: the map itself is the clue ─────────────────────────────────────────────────────

    /// <summary>
    /// A PLACE THE BOOKS DO NOT BALANCE. Owner: <i>"some logic on selection based on map or other clues."</i>
    /// </summary>
    /// <param name="Reason">What does not add up, in the captain's own terms — never "a void is here".</param>
    /// <param name="X0">Aft edge of the suspect band, deck units.</param>
    /// <param name="X1">Forward edge.</param>
    /// <param name="Top">Which side of the spine it is on.</param>
    /// <param name="AreaSquareDu">How much ship the discrepancy accounts for — i.e. how big the search is once
    /// you have believed the clue.</param>
    public readonly record struct Discrepancy(string Reason, double X0, double X1, bool Top, double AreaSquareDu);

    /// <summary>
    /// THE HULL'S OWN AREA, from her outline. Everything below is measured against this, so if the hull is ever
    /// re-cut the arithmetic follows rather than going quietly stale.
    /// </summary>
    public static double HullArea(double aftX, double bowX, double topY, double bottomY) =>
        System.Math.Abs((bowX - aftX) * (bottomY - topY));

    /// <summary>
    /// THE DEDUCTION, and it is one a player can make by looking. Each compartment has a counterpart directly
    /// across the spine; a ship is built symmetrically about her keel because that is how frames are laid. So a
    /// room that is SHORTER than its opposite number is space that has gone somewhere, and the mimic board draws
    /// both rectangles from these very numbers — the clue is not a readout, it is the map being looked at
    /// properly.
    ///
    /// <para>Returns every band that does not balance, largest first, so a captain sounding on a clue starts with
    /// the biggest lie. An empty list means her books balance and there is nothing to find by measuring —
    /// which is itself worth knowing, and worth NOT spending two hundred seconds discovering.</para>
    /// </summary>
    public static IReadOnlyList<Discrepancy> Discrepancies(
        IReadOnlyList<(string Name, float X0, float X1, bool Top)> compartments,
        double spineHalfHeight, double topY, double bottomY)
    {
        ArgumentNullException.ThrowIfNull(compartments);

        // Lab 44 probe D found the FIRST version of this wrong, which is exactly what the lab is for. It measured
        // each room against the total overlap opposite it and then guessed where the shortfall sat — and guessed
        // both the END and the SIDE wrong: with NEAR HOLD's bulkhead moved four frames forward it pointed at
        // x −4…0 on the TOP side when the unaccounted space is x −15…−11 on the BOTTOM. A clue that names the
        // wrong wall is worse than no clue, because a captain sounds it, hears solid, and stops believing the
        // instrument.
        //
        // The honest construction does not guess at all: walk the ship's LENGTH and compare which side has a
        // room there. Space is unaccounted for exactly where one side is roomed and the other is not, and the
        // void is on the side that is MISSING the room. No inference, no reasoning about bulkheads — just an
        // interval on one list that is not on the other.
        List<Discrepancy> found = [];
        double depthTop = System.Math.Abs(-spineHalfHeight - topY);
        double depthBottom = System.Math.Abs(bottomY - spineHalfHeight);

        foreach (bool roomedSide in new[] { true, false })
        {
            var roomed = compartments.Where(c => c.Top == roomedSide)
                                     .OrderBy(c => c.X0)
                                     .ToList();
            var opposite = compartments.Where(c => c.Top != roomedSide)
                                       .OrderBy(c => c.X0)
                                       .ToList();

            foreach ((string name, float x0, float x1, bool _) in roomed)
            {
                // Subtract every opposite-side room from this one's run, and whatever is left over is ship that
                // exists on one side of the keel and not the other.
                List<(double A, double B)> remaining = [(x0, x1)];
                foreach ((string _, float ox0, float ox1, bool _) in opposite)
                {
                    List<(double A, double B)> next = [];
                    foreach ((double a, double b) in remaining)
                    {
                        if (ox1 <= a || ox0 >= b)
                        {
                            next.Add((a, b));
                            continue;
                        }
                        if (ox0 > a) { next.Add((a, ox0)); }
                        if (ox1 < b) { next.Add((ox1, b)); }
                    }
                    remaining = next;
                }

                foreach ((double a, double b) in remaining)
                {
                    double width = b - a;
                    if (width <= 0.5)
                    {
                        continue;   // balanced, or within a frame's worth of measuring error
                    }

                    // The VOID is on the side with no room in it, and it is exactly that interval long.
                    bool voidSide = !roomedSide;
                    double depth = voidSide ? depthBottom : depthTop;
                    found.Add(new Discrepancy(
                        $"{name} runs {width:0.#} frames further {(a < 0 ? "aft" : "forward")} than anything " +
                        $"across the spine from it",
                        a, b, voidSide, width * depth));
                }
            }
        }

        return [.. found.OrderByDescending(d => d.AreaSquareDu)];
    }

    /// <summary>
    /// WHAT BELIEVING THE CLUE IS WORTH, as one number a panel can print. The ratio of a blind hull-wide search
    /// to a search inside the discrepancy — the thing that decides whether any of this is a mechanic or a chore.
    /// </summary>
    public static double CluedSpeedup(Method method, double hullArea, double clueArea) =>
        clueArea <= 0 ? double.PositiveInfinity
        : SecondsToCover(method, hullArea) / System.Math.Max(1e-9, SecondsToCover(method, clueArea));

    // ── The lie is in the paperwork, not the plating ──────────────────────────────────────────────────

    /// <summary>
    /// A SPACE ABOARD THAT IS NOT ON THE DECK PLAN.
    ///
    /// <para><b>Why the discrepancy lives in her DOCUMENTS rather than in her geometry.</b> The obvious build was
    /// to shorten a compartment and let <see cref="Discrepancies"/> find it — but every wreck is drawn from ONE
    /// shared layout, and giving a hull its own rectangles means threading a per-wreck compartment list through
    /// the deck builder, the room lookup, the noise sources and the vent spaces. Leave any one of those on the
    /// shared list and the map disagrees with the sim, which is this repo's most expensive bug class and not a
    /// thing to volunteer for.</para>
    ///
    /// <para>So her plating is honest and her <b>manifest</b> is not: it declares a compartment longer than the
    /// deck plan draws it, and the frames it claims and cannot show are behind that compartment's far bulkhead.
    /// Which is better fiction as well as safer code — somebody had to write the false number down, and the
    /// captain catches them at it by comparing a document with a wall.</para>
    /// </summary>
    /// <param name="NearRoom">The compartment a captain stands in to reach it.</param>
    /// <param name="Outboard">In the shielding band (deep) rather than inside a bulkhead (thin).</param>
    /// <param name="X0">Aft end of the run that is not what it says it is.</param>
    /// <param name="X1">Forward end.</param>
    /// <param name="Top">Which side of the keel.</param>
    /// <param name="PlateX">The false plate — what you knock on and what comes off.</param>
    /// <param name="PlateY">…on the wall of a room, so a captain stands in the room and reaches it.</param>
    /// <param name="AreaSquareDu">How much ship it accounts for — the search, once the clue is believed.</param>
    /// <param name="Holds">What is in there, in the captain's own words when they get in.</param>
    public readonly record struct HiddenVoid(
        string NearRoom,
        bool Outboard,
        double X0,
        double X1,
        bool Top,
        double PlateX,
        double PlateY,
        double AreaSquareDu,
        string Holds);

    /// <summary>How many hulls carry one at all. Rare on purpose: a void on every wreck makes measuring routine,
    /// and the whole appeal is that most ships are exactly what they look like.</summary>
    public const int VoidOnARollOf = 4;   // d20 <= 4, so about one hull in five

    /// <summary>How long a section of her shielding an outboard void takes up. Wide enough to be worth the
    /// trouble of hiding, short enough that the sections either side of it read normal.</summary>
    public const double VoidFrames = 6.0;

    /// <summary>
    /// What is behind the plate — and whether it will fit in a bulkhead run at all.
    ///
    /// <para>THIS IS THE OWNER'S HEURISTIC, AS DATA: <i>"Still a room with a wall to technical space is a good
    /// bet on large enough hiding space."</i> A folded gun mount and a cold locker with somebody in it need the
    /// depth of the shielding band; papers and a rack of keys go anywhere. So <b>what a thing is decides where
    /// it can be</b>, which makes outboard a good BET rather than a rule — and gives a captain who knows what
    /// they are looking for a reason to knock in one place before another.</para>
    /// </summary>
    private static readonly (string Text, bool NeedsTheBand)[] WhatIsInThere =
    [
        ("A rack of code keys in a foam cutout, and one empty slot where somebody took theirs and ran.", false),
        ("A gun mount, folded flat against the frames. The skin over it is thinner than the skin around it.", true),
        ("Ship's papers for three different vessels, and a photograph of this one wearing another name.", false),
        ("A cold locker with one person in it, in a flight suit that is not this ship's.", true),
    ];

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

        ulong seed = DiceRule.Seed("hull-void", wreckId.GetHashCode(System.StringComparison.Ordinal));
        if (DiceRule.Roll(seed).Face > VoidOnARollOf)
        {
            return null;   // most hulls are exactly what they look like, and that is what makes one worth finding
        }

        int which = DiceRule.Roll(DiceRule.Seed(seed, "room"), compartments.Count).Face - 1;
        (string name, float x0, float x1, bool top) = compartments[which];

        (string holds, bool needsTheBand) =
            WhatIsInThere[DiceRule.Roll(DiceRule.Seed(seed, "holds"), WhatIsInThere.Length).Face - 1];

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
                face, insideY, WreckLayout.BulkheadDepth * roomDepth, holds);
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
            VoidFrames * WreckLayout.ShieldingDepth, holds);
    }

    /// <summary>The manifest's lie, as the same <see cref="Discrepancy"/> the geometry rule produces — one type,
    /// two sources, so a panel that can show one can show the other without knowing which it got.</summary>
    public static Discrepancy AsDiscrepancy(in HiddenVoid hidden) =>
        new(hidden.Outboard
                ? $"her shielding is booked by the section, and the run outboard of {hidden.NearRoom} holds a " +
                  "third of what every other section of it does"
                : $"her bulkhead schedule books every frame at one thickness, and the one {hidden.NearRoom} " +
                  "shares is a hand's width off it",
            hidden.X0, hidden.X1, hidden.Top, hidden.AreaSquareDu);

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
