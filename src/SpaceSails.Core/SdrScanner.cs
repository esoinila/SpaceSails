using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #763 · THE KIT THAT HEARS THE BUTTONS. Owner, 2026-08-08, streamed on the heels of #760:
///
/// <para><i>"The remote as the means to push a button for an elevator that has no button: you search for
/// signal in the vicinity — something in the BT/WiFi register — find it, and get the secret button pressed,
/// pretty much by just knowing where to go to look. The elevator can then be MUCH better hidden than any
/// drawn affordance allows."</i></para>
///
/// <para>And the refinement the same hour, which is the whole design: <i>"Passive detection is free and
/// automatic … CONNECTING is the first active act — and the first thing that may have consequences."</i>
/// Holden, hand terminal raised: <i>"There was a button. I pushed it."</i></para>
///
/// <h3>The other end of #760's axis</h3>
/// <para><see cref="RemoteSend"/> is who your ship CLAIMS TO BE on an operator's network — standing, vendor
/// relationships, the org chart. This is for a captain with none of that. It does not authenticate; it
/// LISTENS. Finding a lift's wake-word is not being entitled to it, and the two halves meet at exactly the
/// same doors and reach opposite conclusions about them.</para>
///
/// <h3>Four calls, each overrulable in one line</h3>
/// <list type="number">
/// <item><b>What is on the air is what a gate is.</b> The addressable set on a floor is the floor's own cars
/// (<see cref="UndergroundComplex.ShaftsOn"/>) and, where one exists, the gate to the band below
/// (<see cref="UndergroundComplex.NextShaftBelow"/>) — <b>the same set the panel and the remote already
/// operate</b>, asked of the plan rather than typed out again. Nothing is invented and no wall is redrawn:
/// v1 adds no affordance that did not already exist, it only makes an existing one AUDIBLE.</item>
/// <item><b>A hit never carries a plate.</b> A bearing, a rough range, and which of two kinds it is. The kit
/// hears a carrier; it does not read a stencil, and a line that named the shaft would hand the captain the
/// one inference this whole facility is arranged around them making themselves (§13.10).</item>
/// <item><b>A press with no standing is accepted only where no register covers the door</b> — the halls
/// nobody dug (#677). Everywhere else it is refused, and the refusal is a signature in THAT outfit's log
/// (#715), keyed to the operator and to nobody else, through <see cref="UndergroundComplex.RefusedAtTheGate"/>
/// — the same publication the remote's refusal uses.</item>
/// <item><b>The head office is QUIET.</b> Its outfit publishes no network (#411/#760) and the watchers emit
/// nothing (#649/#672). The kit says so in one line that says less than it knows, and a canon sweep keeps
/// every string in this file from ever claiming otherwise.</item>
/// </list>
///
/// <h3>What this deliberately does NOT do</h3>
/// <para><b>It never opens a band you have not paid for.</b> A press at a shaft gate is refused at every
/// listed operator's door, and the halls — the one place a press is accepted — are the bottom of the world,
/// with no band under them for a press to reach. §13.5 stands: depth past the first band is still the one
/// thing this game makes you earn, and the kit's payoff is KNOWING, not descending.</para>
///
/// <para>Pure and deterministic, like everything else in Core: the same floor scanned from the same square
/// gives the same lines, always.</para>
/// </summary>
public static class SdrScanner
{
    // ── THE OBJECT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The durable id the satchel and the vault hold. Short, and not a seed tag: there is one kind
    /// of these and a captain either has one or does not.</summary>
    public const string ItemId = "sdr-scanner";

    /// <summary>What the satchel row calls it.</summary>
    public const string ItemName = "📻 SDR SCANNER";

    /// <summary>What is stencilled on the case. Standard issue for the outlaw register, and the plate is the
    /// cover: carrying a receiver is not a crime anywhere, and every word on it is true.</summary>
    public const string Plate = "WIDEBAND FIELD RECEIVER · SPECTRUM SURVEY · RX ONLY";

    /// <summary>The kit's own card title (#528's idiom).</summary>
    public const string CardLabel = "📻 STANDARD ISSUE, IF YOU KNOW WHOSE";

    /// <summary>The kit, described as the object it is. It says what it does and it does not say what to do
    /// with it — the discipline every carried thing in this game keeps (#614).</summary>
    public const string CardStory =
        "A slab the size of a hymn book with a stub aerial and one screen, in a case somebody has re-lined " +
        "twice. The plate on the back says RX ONLY, which is true of the receiver and not of the little " +
        "second board wired across it by a hand that had done it before.\n\n" +
        "It sweeps, continuously, and it does not care what it is sweeping. Doors, lifts, hoists, tags, " +
        "counters — anything with a wake-word announces itself to a thing like this simply by being " +
        "powered, because nobody who wired those buildings ever imagined the listening was the hard " +
        "part.\n\n" +
        "Listening leaves no trace. There is no log anywhere in the system of a receiver having been " +
        "switched on. That changes the moment you answer one, and the case does not warn you, because the " +
        "people who carry these do not need telling.";

    /// <summary>Caption-only, deliberately (#528, the odd book, the found record): a card that never claims
    /// a picture rather than one that wires an unpainted file and hides it on error.</summary>
    public const string ArtUrl = "";

    /// <summary>Is THIS the kit? One question, asked here, so no caller teaches itself the kind and the id.</summary>
    public static bool IsTheKit(Satchel.Item item) =>
        item.Kind == Satchel.Kind.Tool && string.Equals(item.Id, ItemId, StringComparison.Ordinal);

    /// <summary>Is a kit in the pocket at all?</summary>
    public static bool InThePocket(IReadOnlyList<Satchel.Item>? carried) =>
        Satchel.CountOf(carried, Satchel.Kind.Tool, ItemId) > 0;

    /// <summary>One kit, ready to go in a pocket.</summary>
    public static Satchel.Item TheKit => new(Satchel.Kind.Tool, ItemId);

    // ── THE COUNTER ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#763 · What one costs over a back counter. Fable's default for v1 and the one number to
    /// flip: dearer than a night's drinking and cheaper than a wreck, because the thing being sold is not
    /// the hardware — a receiver is a receiver — but the little second board across the back of it and the
    /// fact that nobody wrote your name down.</summary>
    public const int PriceCr = 320;

    /// <summary>What the switch on the bar card says.</summary>
    public static string BuyLabel => $"📻 SDR SCANNER — {PriceCr} cr";

    /// <summary>#763 · What the counter did about it.</summary>
    /// <param name="Taken">Whether one is now in the pocket.</param>
    /// <param name="Cost">What it cost. Zero on every refusal.</param>
    /// <param name="RemainingCredits">The purse afterwards.</param>
    /// <param name="Line">Told on-screen, always. Never empty, in any of the four cases.</param>
    public readonly record struct Bought(bool Taken, int Cost, int RemainingCredits, string Line);

    /// <summary>#763 · BUY ONE. Pure: the purse in, the purse out, and the receipt — the same shape the bar's
    /// own <c>BarTab</c> has, because it is the same counter and a captain should not be able to tell which
    /// of the two things they just bought went through different code.</summary>
    public static Bought Buy(int credits, IReadOnlyList<Satchel.Item>? carried)
    {
        if (InThePocket(carried))
        {
            return new Bought(false, 0, credits, AlreadyCarryingLine);
        }
        if (credits < PriceCr)
        {
            return new Bought(false, 0, credits, ShortLine);
        }
        if (!Satchel.CanTake(carried, TheKit))
        {
            return new Bought(false, 0, credits, NoRoomLine);
        }
        return new Bought(true, PriceCr, credits - PriceCr, SoldLine);
    }

    /// <summary>#763 · The receipt. Nobody writes anything down, which is the product.</summary>
    public const string SoldLine =
        "📻 It comes up from under the counter already in its case, and nothing about the transaction is " +
        "written anywhere. “Survey gear,” she says, to the room rather than to you. “Spectrum work. Very " +
        "dull.”";

    /// <summary>#763 · Not enough in the purse. The counter's own register, not the kit's.</summary>
    public static string ShortLine =>
        $"“It's {PriceCr} cr and it does not go on a tab, friend. Come back when it does not have to.”";

    /// <summary>#763 · One is enough. A second receiver hears exactly what the first one hears.</summary>
    public const string AlreadyCarryingLine =
        "“You have got one. It is not a better one for having two of it.”";

    /// <summary>#763 · The pockets will not take it — it is a bulky thing and the satchel's own arithmetic
    /// says so (#688). Said rather than swallowed: a purchase that quietly takes the coin and hands over
    /// nothing is the worst refusal in the game.</summary>
    public const string NoRoomLine =
        "“Not until you have got somewhere to put it. It is a box, not a card.”";

    // ── THE VERBS ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The glyph the kit's lines wear, in the book and on the screen.</summary>
    public const string Glyph = "📻";

    /// <summary>What the satchel's own switch says. The verb the owner named.</summary>
    public const string ScanLabel = "📻 SCAN";

    /// <summary>What the switch is FOR, on its own face. It promises listening and nothing else, because
    /// that is all it does: the press is a second, deliberate control on the kit's own screen.</summary>
    public const string ScanBlurb =
        "Read what the kit has been hearing since you walked in. Listening is free and leaves nothing behind " +
        "anywhere; answering one of them is a separate press.";

    /// <summary>What answering one is called. Never automatic, ever: crossing from listening to transmitting
    /// is exactly the kind of plot-significant choice the game has to put in the player's hand, and the
    /// owner ruled it so in the same breath he filed the kit.</summary>
    public const string PressLabel = "📻 PRESS";

    /// <summary>What the press promises and what it costs, before the press. The whole of #715 in a
    /// sentence: you are about to stop being a receiver.</summary>
    public const string PressBlurb =
        "Send its wake-word with nothing behind it. You are not entitled to this and the answer may be no — " +
        "and a no is a line in somebody's register with the time, this floor, and a caller they cannot place.";

    /// <summary>The heading over the sweep on the kit's own screen.</summary>
    public const string SweepHeading = "📻 ON THE AIR, WITHIN REACH:";

    // ── WHAT IT HEARS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Which kind of thing is on the air. Two, because the building has two, and because a kit
    /// that could tell you more than "a lift or a door" would be reading a stencil it cannot see.</summary>
    public enum Emitter
    {
        /// <summary>A car's call. One per car on this floor, cage and goods car alike — both answer a
        /// button, and a receiver has no way to tell which of them is the one with the hut on top.</summary>
        LiftCall,

        /// <summary>The gate to the band below, where this floor has one. <b>This is the feature.</b> The
        /// panel is entitled to say nothing at all about a band the building does not admit to (#592); a
        /// carrier is not entitled to anything, and it is still there.</summary>
        Door,
    }

    /// <summary>One thing the kit heard. <b>A bearing, a rough range and a kind — and nothing else.</b> No
    /// plate, no sign, no shaft number, no company: the whole discipline of this feature is in what this
    /// record does not have room for.</summary>
    /// <param name="What">Which of the two kinds it is.</param>
    /// <param name="X">Where it is, in the floor's own coordinates — for the client's own use, never
    /// printed.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Bearing">Which way, in the building's own compass (<see cref="Bearings"/>).</param>
    /// <param name="RangeDu">How far, ALREADY ROUNDED to <see cref="RangeStepDu"/> — the rough band the kit
    /// is honest about, decided here rather than by whoever formats it.</param>
    public readonly record struct Hit(Emitter What, double X, double Y, string Bearing, double RangeDu);

    /// <summary>How far the kit hears. Fable's default for v1, and the whole of its difficulty: the owner's
    /// <i>"proximity is the only skill"</i> is this number. Two cars on one floor stand at least
    /// <see cref="UndergroundComplex.MinShaftSeparationOn"/> apart — about a hundred du — so a captain
    /// standing at one of them usually cannot hear the other, and walking the corridor is the game.</summary>
    public const double ScanReachDu = 90.0;

    /// <summary>How rough "rough" is. A receiver estimating range off signal strength is not a tape measure,
    /// and a kit that reported 41.7 du would be a rangefinder wearing a radio's case.</summary>
    public const double RangeStepDu = 10.0;

    /// <summary>The building's own compass, in the register the plan already speaks: the block is drawn
    /// W│…│E, and the ribs are already told apart by whether they run toward the deep field or back toward
    /// the landing band (<see cref="UndergroundComplex.Rib"/>). Four words, no true north — there is none
    /// down here and there is none on the surface either (<c>CacheMint.Bearings</c>).</summary>
    public static readonly IReadOnlyList<string> Bearings =
        ["eastward", "westward", "landingward", "deepward"];

    /// <summary>Which way, from one square to another, in the building's own compass. The dominant axis and
    /// no diagonals: a needle that swung between eight words at a corridor junction would be reporting
    /// precision the estimate does not have.</summary>
    public static string BearingFrom(double dx, double dy) =>
        Math.Abs(dx) >= Math.Abs(dy)
            ? (dx >= 0 ? Bearings[0] : Bearings[1])
            : (dy >= 0 ? Bearings[2] : Bearings[3]);

    /// <summary>
    /// #763 · <b>WHAT IS ON THE AIR WITHIN REACH OF THIS SQUARE.</b>
    ///
    /// <para>Asked of the plan and never of a literal: the cars are <see cref="UndergroundComplex.ShaftsOn"/>'s
    /// own list and the gate is <see cref="UndergroundComplex.NextShaftBelow"/>'s own answer, so the kit can
    /// never hear a door the building does not have, nor miss one it does.</para>
    ///
    /// <para>Empty is a real answer and the commonest one: on the surface, out of reach, and at every site
    /// whose operator answers no radio at all (<see cref="Quiet"/>).</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor. Zero and above is the surface, where this building has nothing.</param>
    /// <param name="x">Where the captain is standing.</param>
    /// <param name="y">The same.</param>
    /// <param name="field">The ground the floor was laid on — the same field the renderer builds it with.</param>
    /// <param name="reachDu">How far the kit hears. Defaulted; a caller passes one only in a lab.</param>
    public static IReadOnlyList<Hit> Hits(
        string bodyId, int level, double x, double y, in SurfaceLayout.Field field,
        double reachDu = ScanReachDu)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Nothing here answers a radio, and nothing here is going to start. One question, asked before any
        // geometry, so the silence can never be a coincidence of where somebody happened to be standing.
        if (level >= 0 || Quiet(bodyId))
        {
            return [];
        }

        var heard = new List<Hit>();
        bool wayDown = UndergroundComplex.NextShaftBelow(bodyId, level) is not null;

        foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(field))
        {
            (double lx, double ly) = car.Landing;
            if (Within(x, y, lx, ly, reachDu) is not { } range)
            {
                continue;
            }

            string bearing = BearingFrom(lx - x, ly - y);
            heard.Add(new Hit(Emitter.LiftCall, lx, ly, bearing, range));

            // …and the bolt in the shaft under it, where there is one. Only the cage runs the gate (#801),
            // and it runs the gate whether or not the panel is prepared to admit the band exists (#592).
            if (wayDown && car.RunsTheGate)
            {
                heard.Add(new Hit(Emitter.Door, lx, ly, bearing, range));
            }
        }

        return heard;
    }

    /// <summary>In reach? Hands back the ROUNDED range so the rounding happens exactly once, here, and no
    /// caller can print a number the record does not hold.</summary>
    private static double? Within(double x, double y, double tx, double ty, double reachDu)
    {
        double d = Math.Sqrt(((tx - x) * (tx - x)) + ((ty - y) * (ty - y)));
        if (d > reachDu)
        {
            return null;
        }
        return Math.Max(RangeStepDu, Math.Round(d / RangeStepDu, MidpointRounding.AwayFromZero) * RangeStepDu);
    }

    /// <summary>What one hit reads as on the kit's screen. <b>Never a plate and never a shaft number</b> —
    /// see <see cref="Hit"/>, whose whole shape is this sentence's fence.</summary>
    public static string HitLine(Hit hit) =>
        $"📻 {WhatItIs(hit.What)} · about {hit.RangeDu:0} du · {hit.Bearing}";

    /// <summary>The two things the kit is prepared to say a carrier is.</summary>
    public static string WhatItIs(Emitter what) => what == Emitter.LiftCall ? "a lift call" : "a door";

    /// <summary>What a scan that heard nothing says. Said, because a control that does nothing and says
    /// nothing is indistinguishable from a bug (#603's founding law) — and it says nothing about whether
    /// there is anything to hear from somewhere else on this floor, because the kit does not know.</summary>
    public const string NothingHeardLine =
        "📻 The sweep runs and the screen stays flat. Nothing within reach of where you are standing is " +
        "powered and talking. Whether that is because there is nothing, or because you are in the wrong " +
        "part of the corridor, the kit has no opinion.";

    // ── THE SILENCE THAT IS THE POINT ───────────────────────────────────────────────────────────────────

    /// <summary>#649/#672/#763 · <b>IS THIS PLACE QUIET?</b> One outfit answers no radio anywhere
    /// (<see cref="SiteOperator.Operator.PublishesNetwork"/>), and it is the one at the head of everything.
    /// Asked of the operator rather than of the body, so there is one fact and not two spellings of it.</summary>
    public static bool Quiet(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return !SiteOperator.Of(bodyId).PublishesNetwork;
    }

    /// <summary>#763 · The one line, and it says LESS than it knows.
    ///
    /// <para>CANON, harder here than anywhere else in this file: it reports a flat screen and stops. It does
    /// not say something is being hidden, does not say the silence is unusual, does not say anything
    /// acknowledged or answered or went quiet when you arrived. A kit that hears every hidden door on every
    /// moon and hears NOTHING here is already a sentence, and the sentence is the player's to finish.</para></summary>
    public const string QuietLine =
        "📻 The sweep runs clean from one end of the band to the other and comes back with an empty screen. " +
        "No carriers. Not weak ones, not encrypted ones — none. Whatever is running here is not running on " +
        "anything a receiver can be pointed at.";

    // ── THE PRESS ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#763 · <b>WHO ANSWERS FOR THE RADIO ON THIS FLOOR.</b>
    ///
    /// <para>The site's own operator everywhere the register reaches — and <b>nobody</b> in the halls, which
    /// no register reaches: nothing down there is a facility, no company ever enrolled it, and the hardware
    /// a shaft crew lowered into it was hung on a door with no list behind it. Null is therefore not a
    /// missing answer, it is the answer, and it is the one place in the game a wake-word with no standing is
    /// enough (#677).</para>
    ///
    /// <para>Fable's default for v1, and the one line to flip: widen this to <c>IsUnlisted</c> as well and
    /// the kit starts opening the band nobody listed, which is depth without paper and §13.5's to
    /// give.</para></summary>
    public static SiteOperator.Operator? OperatorOf(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UndergroundComplex.IsFound(bodyId, level) ? null : SiteOperator.Of(bodyId);
    }

    /// <summary>#763 · What answering a carrier did.</summary>
    /// <param name="Worked">Whether it opened, came, or moved.</param>
    /// <param name="Line">Told on-screen, always (#603/#684/#736). Never empty, in either case.</param>
    /// <param name="Charge">What it cost and who is owed it (#715). Zero on an accepted press: nobody was
    /// crossed, because nobody was there to cross.</param>
    public readonly record struct Pressed(bool Worked, string Line, UndergroundComplex.HeatCharge Charge);

    /// <summary>#763 · <b>SEND THE WAKE-WORD WITH NO STANDING.</b> The first active act, and the first one
    /// with a consequence.
    ///
    /// <para>It is accepted where nobody is listed to refuse it and refused everywhere else, and the refusal
    /// costs exactly what a refused card costs at a gate, owed to the outfit whose door it was and to nobody
    /// else — <see cref="UndergroundComplex.RefusedAtTheGate"/>, the same publication #760's send uses, so
    /// the day #715's meter lands there is one number to wire and no second spelling to find.</para></summary>
    public static Pressed Press(string bodyId, int level, Hit hit)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (OperatorOf(bodyId, level) is null)
        {
            return new Pressed(true, AcceptedLine(hit.What), UndergroundComplex.NothingOwed);
        }

        return new Pressed(false, RefusedLine(hit.What), UndergroundComplex.RefusedAtTheGate(bodyId));
    }

    /// <summary>#763 · What it is like when it works, which is the epigraph the owner supplied — Holden, hand
    /// terminal raised, <i>"There was a button. I pushed it."</i> Nothing acknowledges. The thing simply
    /// does what it was built to do, for the first time in a long time, for somebody with no right to ask
    /// it.</summary>
    public static string AcceptedLine(Emitter what) => what == Emitter.LiftCall
        ? "📻 You send it, and after a pause long enough to be a decision somewhere, machinery takes up " +
          "load. The car is coming. Nothing asked who you were, because there is nothing here that has ever " +
          "been told to."
        : "📻 You send it, and the bolt goes back. That is the whole event: a fitting doing what it was made " +
          "to do, for the first time in a very long time, on the word of somebody who has no standing " +
          "anywhere in this building.";

    /// <summary>#763 · What it is like when it does not, and what it costs. The preamble is the kit's own
    /// and not the remote's (#760's goes out in the SHIP's name; this goes out in nobody's), and the answer
    /// is the same answer a listed operator's door has always given a stranger — a machine noting the fact
    /// and filing it.</summary>
    public static string RefusedLine(Emitter what) =>
        "📻 It goes out in nobody's name, because there is nothing in the kit to put in that field, and the " +
        $"answer is back inside a second. {(what == Emitter.LiftCall ? "The car does not move" : "The bolt does not move")}. " +
        "What does happen is a line in a register somewhere with a time on it, and an unrecognised caller, " +
        "and this floor.";

    // ── THE BEAT ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#761/#777 · A hit is a STORY BEAT, and it is told ONCE per floor, ON THE KIT'S OWN CARD —
    /// hosted, never raised as a second modal over the one the player is already reading.
    ///
    /// <para>The key the client remembers it by. Per floor and per site, because "there is something on the
    /// air here" is a fact about a floor and stops being news the moment you have been told it.</para></summary>
    public static string BeatKey(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return $"sdr:{bodyId}:{level}";
    }

    /// <summary>#761 · The beat itself. It says what changed for the CAPTAIN — that the building is talking
    /// and has been the whole time — and it names nothing on it.</summary>
    public const string BeatLine =
        "📻 The screen fills. It has been filling since you walked in here, quietly, in a pocket: this floor " +
        "is not silent and never was. Somewhere within a corridor of you something is powered, waiting to be " +
        "spoken to, and has been waiting long enough that nobody left alive is expecting the call.";
}
