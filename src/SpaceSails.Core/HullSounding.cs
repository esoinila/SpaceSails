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
public static partial class HullSounding
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
    /// <param name="Says">WHICH of her papers does not add up — see <see cref="ClueKind"/>. Defaults to the
    /// manifest, which is the one that shipped first and the one every existing caller means.</param>
    public readonly record struct HiddenVoid(
        string NearRoom,
        bool Outboard,
        double X0,
        double X1,
        bool Top,
        double PlateX,
        double PlateY,
        double AreaSquareDu,
        string Holds,
        ClueKind Says = ClueKind.Manifest);

    /// <summary>
    /// #537 slice 3 · WHICH OF HER PAPERS DOES NOT ADD UP. The manifest was one, and a hull that only ever
    /// lied in the same document would teach a captain to read one page and skip the rest of the ship.
    ///
    /// <para><b>Each is deniable on its own</b>, which is #533's whole discipline: a stuck breaker, a
    /// draughtsman who renumbered after a refit, a shielding section booked light because somebody was lazy.
    /// None of the three is proof of anything. Two of them agreeing would be — and they never do, because
    /// <b>a hull tells exactly one</b>. Read the wrong page on a lying hull and you get an honest dead end
    /// and learn nothing, which is the price of not reading all three.</para>
    ///
    /// <para><b>And on a clean hull every one of them dead-ends</b> (<see cref="HonestLine"/>). A document
    /// that only speaks up when there is something to find is not a clue, it is a pointer — the law the
    /// manifest already shipped under, now owed by three papers instead of by one.</para>
    /// </summary>
    public enum ClueKind
    {
        /// <summary>The cargo manifest: her shielding is booked section by section, and one section is booked
        /// light. Read at the manifest station.</summary>
        Manifest,

        /// <summary>The builder's frame numbering, off the damage-control placard by the lock: the plate
        /// counts from the transom forward and steps over a run of frames it never writes down.</summary>
        SkippedFrame,

        /// <summary>The dead bridge panel — dead except for one breaker warm to the back of a glove. A
        /// standing load on a bus that runs to a compartment nobody is using.</summary>
        StandingLoad,
    }

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
}
