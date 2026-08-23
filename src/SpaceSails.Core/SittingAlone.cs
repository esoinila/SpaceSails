using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #757 · TAKING A TABLE — the normal way to operate in a bar, and the detective's passive verb.
///
/// <para>Owner, live in the hall: <i>"I have empty table but I cannot sit down."</i> Correct and by omission:
/// #746's sit interaction is <b>ask to join</b>, which needs a counterpart, so an empty top in an eighty-seat
/// room offered nothing at all. And then, sharpening it the same evening: <i>"Suppose I just want to sit down
/// and wait to be disturbed?"</i></para>
///
/// <h3>WAIT is not filler. It is the whole of asking.</h3>
///
/// <para>Sitting down alone in a room where nobody knows you is a CHOICE TO BE FINDABLE. You do not go and
/// get the scene; you hold a table and let the room decide whether it has anything for you. On the right
/// watch somebody crosses the floor. On the wrong one, nobody does — and, in the owner's own filing,
/// <b>nothing happening in an eighty-seat room that used to be loud IS the event</b>. So a wait that produces
/// nobody is a told outcome with words on it, never a button that did not respond.</para>
///
/// <h3>The approach is an ENTICEMENT — the roles are the other way round</h3>
///
/// <para>Owner, streamed while this was being built: <i>"a stranger may approach me and 1. ask to sit down,
/// 2. maybe offer to buy me a drink, 3. tell me what they have in mind… think Gandalf knocking on Bilbo's
/// door."</i> #746's table is the captain talking their way into somebody else's business; this is somebody
/// walking across a room to recruit the captain into theirs, and the ladder is the courtship: a chair, a
/// drink, and only then the thing they came over for.</para>
///
/// <para>Same machine. The whole of it is an <see cref="Encounter.Scene"/> — the rungs are
/// <see cref="Encounter.Requirement.ReplyToPriorMove"/>, which is #749's own law that a reply to a sentence
/// nobody has spoken is not a control that is disabled, it is a control that is not there. Nothing here is a
/// second engine, which is the claim <see cref="Encounter"/> has been making since #746.</para>
///
/// <para>Pure and deterministic: no clock, no <c>Random</c>, no world. The client owns which table you are at
/// and how many beats you have sat through; this owns the words, the law and the dice.</para>
/// </summary>
public static class SittingAlone
{
    /// <summary>The glyph a table of your own wears — a chair, at console size. A pocket, a board and a
    /// person all have one; this is the furniture's, and both plates below are built out of it so the room
    /// cannot end up with two chairs that are drawn differently.</summary>
    public const string Glyph = "🪑";

    /// <summary>What a free top is labelled on the deck. The whole of #757's complaint was that an empty
    /// table said nothing and answered nothing; this is the half of the fix the eye does.
    ///
    /// <para>#783 · …AND IT SAYS THE ACTION. Owner, live at a table and confused: <i>"Why not use words like
    /// SIT DOWN here if it means sitting down?"</i> The plate used to name the FURNITURE and leave the verb to
    /// be guessed; "take the table" — the phrase this issue's first draft reached for — reads as inventory.
    /// A prompt is text that must READ (#782), and the plainest word for sitting down is sitting down.</para>
    /// </summary>
    public const string FreeTablePlate = Glyph + " A FREE TABLE — SIT DOWN";

    /// <summary>Who you are sitting with. Nobody — and the panel says so plainly rather than leaving its
    /// counterpart line blank, which reads as a missing string.</summary>
    public const string OwnTablePlate = Glyph + " YOUR OWN TABLE";

    /// <summary>Where you are. <see cref="CanteenTable.Setting"/>'s own words — one room, one name for
    /// it.</summary>
    public const string Setting = CanteenTable.Setting;

    /// <summary>
    /// #973 L5b · …AND WHERE THAT TABLE IS, WHEN IT IS NOT IN A CANTEEN.
    ///
    /// <para>The eighth seat put a takeable top in a docked station's BAR, and the strip's company clause is
    /// built out of <see cref="Encounter.Scene.Setting"/> — so a woman standing at a top in The Stormwatch Bar
    /// was announced as being <i>"a table in the upper canteen"</i>, three hundred thousand kilometres from
    /// the nearest one. Found by looking at it, which is where this repository's "the sim doing one thing
    /// while a SENTENCE reports another" bug class has been found every single time.</para>
    ///
    /// <para>The room's own name, handed in, because it is per-station and Core does not know the berths.</para>
    /// </summary>
    public static string BarSetting(string? barName) =>
        string.IsNullOrWhiteSpace(barName)
            ? "a top in the bar, the room still lit behind you"
            : $"a top in {barName!.Trim()}, the room still lit behind you";

    // ── THE MOVES ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Hold the table and let the room decide. The passive verb, and the only one a solo table
    /// has.</summary>
    public const string Wait = "wait";

    /// <summary>Stand up. <see cref="Encounter.Leave"/>, so the framework's free-exit law covers this scene
    /// without this file restating it.</summary>
    public const string Stand = Encounter.Leave;

    /// <summary>Pull the chair out for whoever has stopped at your table.</summary>
    public const string WaveIn = "wave-in";

    /// <summary>…or do not. Free, like every refusal in this game.</summary>
    public const string WaveOff = "wave-off";

    /// <summary>Let them stand you the drink they offered.</summary>
    public const string LetThemBuy = "let-them-buy";

    /// <summary>…or not, which is its own answer and costs nothing either.</summary>
    public const string NoDrink = "no-drink";

    /// <summary>The third rung: what they actually came over for.</summary>
    public const string HearThemOut = "hear-them-out";

    /// <summary>The button labels, beside the ids, so no panel invents its own vocabulary for a move the
    /// design named.
    ///
    /// <para>#783 · WAIT'S LABEL SAYS WHAT WAITING DOES. Owner, twice in one sitting: <i>"What does the WAIT
    /// option mean here?"</i> — and that is the red proof. "Wait" alone reads as a loading verb, so the
    /// button that IS this scene looked like the button that means the game is thinking. The label now says
    /// the posture and its point in the player's own words; the move id is untouched, because a saved game
    /// and a guard both key on the id and neither should ever have keyed on the caption.</para>
    /// </summary>
    public static string LabelOf(string moveId) => moveId switch
    {
        Wait => "SIT A WHILE — see who comes",
        WaveIn => "Pull the chair out",
        WaveOff => "Not tonight",
        LetThemBuy => "Let her buy",
        NoDrink => "You're alright, thanks",
        HearThemOut => "What's on your mind?",
        // #783 · Owner's own ruling on the panel's verbs: "'Stand up' stays — it already says the thing."
        // It was reaching the default arm and rendering as TAKE YOUR LEAVE, which is the courtesy you owe
        // somebody ELSE'S table (#746 keeps it, and should). Getting up from your own chair is standing up.
        Stand => "Stand up",
        _ => "Take your leave",
    };

    // ── TAKING THE TABLE, AND GIVING IT BACK ──────────────────────────────────────────────────────────

    /// <summary>#783 · THE STATE CHANGE, CONFIRMED FIRST, in the plainest words there are.
    ///
    /// <para>Owner ruling, live: <i>"On sitting, the panel's FIRST line must confirm the state change… so the
    /// player knows E worked BEFORE seeing more verbs."</i> The wary line below opens on a POSTURE, which is
    /// the right second sentence and the wrong first one: a captain who has just pressed a key needs to be
    /// told the key did something before they are told what kind of person it made them.</para></summary>
    public const string SatDownLine = "You sit down. The table is yours.";

    /// <summary>What taking a table is on a watch that is watching you. Not a transaction and not a menu: a
    /// posture — and #783's plain confirmation in front of it.</summary>
    public const string TookTheTableLine =
        SatDownLine + " " +
        "You take the chair with your back to the wall and your hands where they can be seen. In a room " +
        "like this, sitting down on your own is the whole of asking.";

    /// <summary>Standing up. Free, always, and it never costs a thing.</summary>
    public const string StoodUpLine = "You stand, and the table is a table again.";

    // ── #783 · THE OTHER REGISTER: A SHORT REST ───────────────────────────────────────────────────────
    //
    // Owner addendum, live: "sitting should also have the RELAXATION register — feels good to sit down for a
    // change, lift legs to another chair and drink a cold drink with alcohol." The lines below are the owner's
    // own filing, canon-approved by authorship and lifted VERBATIM; nothing in this section rewrites them.
    // The posture reads the ROOM: back-to-the-wall is what you are in a hall that is full of people who could
    // be anybody, and it is not what you are in an emptied one with a cold glass in your hand.
    //
    // …AND THE SENTENCE OWNS ITS OWN FACTS (#740, canon review of #783). The filed relaxed line names a cold
    // glass, and the trigger fires on a quiet watch with or WITHOUT a purchase — so the register is two
    // openings, not one: the boots are the rest and are always there, the glass is the purchase and is
    // mentioned only when somebody actually bought it.
    //
    // WHAT THIS FILE DOES NOT DO: rest is not a mechanic here. Whether a rest heals anything is #784's lane,
    // and this scene deliberately owns only the words and the law that picks between them — one answer to
    // "is this a rest?", exported below, for that crew to consume rather than re-derive.

    /// <summary>
    /// The sit itself, WITH A BOUGHT POUR IN YOUR HAND. The cold glass in this sentence is a real glass:
    /// somebody paid for it at the counter and carried it over.
    /// </summary>
    public const string RelaxedSitLine =
        "It feels good to sit down for a change. You put your boots up on the spare chair and let the cold " +
        "glass sweat into your hand, and for as long as it lasts, nobody in this building needs anything " +
        "from you.";

    /// <summary>
    /// …and the same rest with NOTHING IN YOUR HAND, on a watch quiet enough to take one.
    ///
    /// <para>CANON REVIEW, ruled: the line above names a cold glass, and the owner's own trigger fires the
    /// relaxed register on a quiet watch <b>with or without</b> a purchase — so a drinkless rest was
    /// narrating a drink nobody bought. That is the #740 class exactly: a sentence must own its own facts.
    /// The boots stay up either way, because the boots are the rest; the glass is the purchase, and only the
    /// purchase may mention it.</para>
    /// </summary>
    public const string RelaxedSitDryLine =
        "It feels good to sit down for a change. You put your boots up on the spare chair, and for as long " +
        "as nobody needs you, nobody needs you.";

    /// <summary>The drink itself, said only when there actually is one — the counter's pour (#756/#772),
    /// carried to the table it was bought to be drunk at.</summary>
    public const string TheDrinkLine =
        "The pour is cold and it is honest about what it is. Somewhere below B4, a still is doing its quiet " +
        "best for you.";

    /// <summary>Standing up after a rest, which is not the same sentence as standing up from a watch.</summary>
    public const string StoodUpRelaxedLine =
        "You put the chair back the way it was. The minute is over, and it was a good minute.";

    /// <summary>
    /// #783 · DOES THIS SIT READ AS RELAXED? The one answer, so the panel's opening line, its goodbye
    /// and its picture cannot come to three different ones.
    ///
    /// <para>Owner's own condition, quoted: <i>"with a bought drink in hand, OR on a quiet watch."</i> Quiet
    /// is <see cref="BusyAt"/>'s own threshold — the same line that decides which silence a fruitless wait
    /// gets — so the hall cannot be indifferent-busy in one sentence and restful in the next.</para>
    ///
    /// <para>FIFTH-BUG-CLASS NOTE: both answers are reachable on watches the game actually has. The small
    /// watches sit at 0.15/0.30 and the working ones at 0.45 and up, so a guard can walk real watch indices
    /// and see this flip, rather than trusting the arithmetic in this comment.</para>
    /// </summary>
    /// <param name="drinkInHand">Whether a pour bought at the counter is still in the captain's hand.</param>
    /// <param name="watch">The shift, frozen when the floor was drawn (#709).</param>
    public static bool SitReadsAsRelaxed(bool drinkInHand, long watch) =>
        drinkInHand || Fill(watch) < BusyAt;

    // WHOSE DRINK, AND WHOSE REST — the seam with #784, stated once so nobody collapses the two.
    //
    // #784 ships the short rest as a MECHANIC: every solo sit is one (Map.CaptainIsRestingAtATable), and how
    // much it gives back is doubled by a pour in front of you (Map.APourInFrontOfYou, which is the client's
    // one reading of the counter's tot — this file deliberately keeps no second window of its own, because
    // a panel that said "cold glass" while the rest engine said "no pour" is the fault canon review already
    // caught in this very scene). What THIS file decides is narrower and is about WORDS AND PICTURES ONLY:
    // whether the sit READS as relaxed. A back-to-the-wall watch is still a short rest for the body; it is
    // simply not the sentence about boots and it is not the picture of them.

    /// <summary>The rest's own opening, in the one of its two forms the captain's hand decides. THE GLASS IS
    /// ONLY MENTIONED WHEN THERE IS A GLASS — canon review's ruling, and the #740 law under it: a sentence
    /// owns its own facts, so a rest with nothing in your hand may not narrate a drink.</summary>
    public static string RelaxedOpening(bool drinkInHand) =>
        drinkInHand ? RelaxedSitLine : RelaxedSitDryLine;

    /// <summary>What sitting down says, in whichever register the room and the glass put you in. The drink's
    /// own line rides along only when there IS a drink — a sentence about a pour nobody bought is the kind of
    /// lie a panel tells once and a player never trusts again, and the opening it follows is chosen on the
    /// same fact so the two cannot disagree about whether you are holding anything.
    ///
    /// <para>THE ONE PLACE the opening sentence is chosen. <see cref="TheTable"/>'s opening is this call and
    /// not a second copy of this ternary, because a scene whose first line disagreed with the line the panel
    /// prints is this project's third named bug class with prose in it.</para></summary>
    public static string SitLine(bool relaxed, bool drinkInHand) =>
        !relaxed ? TookTheTableLine
        : drinkInHand ? RelaxedOpening(true) + " " + TheDrinkLine
        : RelaxedOpening(false);

    /// <summary>…and the same question asked of the ROOM instead of a flag: what does sitting down say on
    /// this watch, with or without a glass in your hand.</summary>
    public static string SatDown(bool drinkInHand, long watch) =>
        SitLine(SitReadsAsRelaxed(drinkInHand, watch), drinkInHand);

    /// <summary>What getting up says. The rest earns its own goodbye; the watch keeps #757's.</summary>
    public static string StoodUp(bool relaxed) => relaxed ? StoodUpRelaxedLine : StoodUpLine;

    // ── #783 · AND WHAT THE PANEL SHOWS YOU ───────────────────────────────────────────────────────────
    //
    // Owner, live at a taken table: "the pop up could have Gen AI here." Two states, two pictures, and the
    // state is the SAME one the prose above reads — a panel that said "boots up on the spare chair" over a
    // picture of an empty chair would be the third named bug class with a caption on it.

    /// <summary>The WAITING state: first-person from your chair, the empty one opposite pulled slightly out.
    /// The empty chair IS the wait beat.</summary>
    public const string WaitingArtUrl = "art/b1-your-own-table.jpg";

    /// <summary>The RESTING state: boots up on that same chair, a sweating glass, notebooks and papers and
    /// two plates of something the kitchen calls food.</summary>
    public const string RestingArtUrl = "art/b1-short-rest.jpg";

    /// <summary>Which of the two the panel wears.</summary>
    public static string ArtFor(bool relaxed) => relaxed ? RestingArtUrl : WaitingArtUrl;

    /// <summary>
    /// #757 · YOUR OWN TABLE, as an <see cref="Encounter.Scene"/> — two moves and no third.
    ///
    /// <para>Wait, and stand up. There is deliberately nothing else on it: buying your own drink is
    /// the counter's business (#756's lane, and this scene must not grow a second answer to it), and every
    /// other move at a table is something you say to somebody.</para>
    /// </summary>
    /// <param name="relaxed">#783 · Whether this sitting READS AS RELAXED — which decides the opening line,
    /// the line you get up on, and the picture the panel wears. <see cref="SitReadsAsRelaxed"/> is the one
    /// place that is decided; this only carries the answer into the scene.</param>
    /// <param name="drinkInHand">Whether there is a bought pour in hand, which adds its own sentence.</param>
    public static Encounter.Scene TheTable(bool relaxed = false, bool drinkInHand = false) => new(
        "canteen:table:alone",
        OwnTablePlate,
        Setting,
        SitLine(relaxed, drinkInHand),
        [
            new(Wait, LabelOf(Wait)),
            new(Stand, LabelOf(Stand), Says: StoodUp(relaxed)),
        ]);

    // ── WHETHER ANYBODY COMES ─────────────────────────────────────────────────────────────────────────
    //
    // FIFTH-BUG-CLASS WARNING, paid up front: a threshold that selects everything, or nothing, is a guard
    // that asserts nothing. The die is DiceRule's own d20 and the threshold below is derived from the hall's
    // own WatchFill — 0.15 on the small watch, 0.95 in the middle of the day — so on EVERY watch the game
    // has, both answers are reachable, and a guard measures that against real rolls rather than trusting the
    // arithmetic in this comment.

    /// <summary>How many faces the approach is rolled on. The house d20, like everything else.</summary>
    public const int Faces = DiceRule.D20;

    /// <summary>How many of those faces bring somebody over when the hall is FULL. Roughly two beats in
    /// five at the heaving watch, and the fraction scales down with how many people are in the room, so a
    /// dead hall is a dead hall. FLAGGED for the owner's tuning — this number is the whole tempo of waiting.
    /// </summary>
    public const int FacesWhenPacked = 8;

    /// <summary>How full the hall is on this watch. <see cref="CanteenRegulars.WatchFill"/>'s own number and
    /// never a second one — the room the captain is looking at is the room that decides whether anybody has
    /// a reason to cross it.</summary>
    public static double Fill(long watch)
    {
        IReadOnlyList<double> bill = CanteenRegulars.WatchFill;
        return bill[(int)(((watch % bill.Count) + bill.Count) % bill.Count)];
    }

    /// <summary>How many faces of the d20 bring somebody over on THIS watch. At least one on every watch —
    /// a room with people in it is never a room where nobody can possibly walk up — and it is a floor rather
    /// than a rounding, so the emptiest shift stays the emptiest shift.</summary>
    public static int FacesThatBringSomebody(long watch) =>
        Math.Clamp((int)Math.Round(FacesWhenPacked * Fill(watch), MidpointRounding.AwayFromZero), 1, Faces);

    /// <summary>
    /// #757 · DOES ANYBODY CROSS THE ROOM THIS BEAT?
    ///
    /// <para>Seeded on (site, floor, table, watch, beat) and nothing else, so the same wait at the same table
    /// on the same shift is the same answer — a captain cannot re-press their way into company, and a test
    /// can walk beats to reach either outcome instead of mocking a die.</para>
    /// </summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="level">The floor.</param>
    /// <param name="tableIndex">Which top — Core's own ordinal, never a pair of doubles.</param>
    /// <param name="watch">The shift, frozen when the floor was drawn (#709).</param>
    /// <param name="beat">How many times you have waited at this table this sitting, from zero.</param>
    /// <param name="quiet">#751 · Whether this top is in a CABINET — a room the hall cannot see into. Then
    /// NOBODY comes, ever, and that is not a gap in the content: waiting at a table is a choice to be
    /// findable, and a cabinet is the room you take when you have chosen the opposite. The one law states
    /// itself twice, once in each direction.</param>
    public static bool SomebodyComes(
        string bodyId, int level, int tableIndex, long watch, int beat, bool quiet = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (quiet)
        {
            return false;
        }
        DiceRoll roll = DiceRule.Roll(
            DiceRule.Seed($"canteen:approach:{bodyId}:{tableIndex}", level, watch, beat), Faces);
        return roll.Face <= FacesThatBringSomebody(watch);
    }

    // ── AND WHEN NOBODY DOES ──────────────────────────────────────────────────────────────────────────
    //
    // An empty room on the wrong watch IS the event (owner, filing #757). So the two pools below are not one
    // pool with different words: a busy hall that has no time for you and a hall that has been emptied are
    // two different sentences about the same silence, and which one you get is decided by the room rather
    // than by a mood setting. Nothing anywhere announces which watch you walked into.

    /// <summary>At or above this fill, the hall is BUSY and the silence at your table is indifference.
    /// Below it, the silence is the room's. Sits between the small watches (0.15, 0.30) and the working ones
    /// (0.45 and up), so both pools are reachable on the watches the game actually has.</summary>
    public const double BusyAt = 0.40;

    /// <summary>What waiting is like in a hall that is full of people who are not interested in you.</summary>
    public static readonly IReadOnlyList<string> NobodyCameBusy =
    [
        "A while goes by. The hall eats and argues and settles its own business, and none of it is yours.",
        "Somebody laughs two tops over at something you did not hear. Nothing comes your way.",
        "A tray goes past. A chair scrapes. Your table stays your table.",
        "A while goes by. You are the only person in here with nothing in front of them, and nobody has " +
        "noticed.",
    ];

    /// <summary>And what it is like in a hall that has been emptied. Not one of these says why.</summary>
    public static readonly IReadOnlyList<string> NobodyCameQuiet =
    [
        "A while goes by. Eighty chairs, and the loudest thing in the room is the machine at the back " +
        "thinking about somebody's card.",
        // #783 · Wiped steel, not linen. The line was written for a canteen this game does not have: the
        // no-tablecloths ruling on #759 says a mining hall's tables are bare metal, and a sentence that hands
        // the player linen is the art and the prose describing two different buildings. Same beat, same
        // rhythm, right world — owner-authored, lifted verbatim.
        "Nobody comes. Every table you can see is wiped steel, and it has been wiped a while.",
        "You wait. The room is not empty so much as emptied, which is a different thing, and there is " +
        "nobody in here to ask about it.",
        "A while goes by. Somewhere behind the counter a fridge cycles, stops, and starts again.",
    ];

    /// <summary>#751/#757 · And what waiting is like BEHIND A DOOR. Nobody comes; that is what the room is
    /// for, and the lines say so without ever saying it is a rule.
    ///
    /// <para>#758 · The first of them said <i>the door is shut</i>, and usually it is not: a cabinet stands
    /// open behind a curtain until somebody dogs it (<see cref="CabinetPrivacy"/>). It says the thing that
    /// is true at BOTH stages now — the hall does not walk in here — because a line asserting a leaf the sim
    /// has left open is the third named bug class shipping inside the feature that opened it.</para></summary>
    public static readonly IReadOnlyList<string> NobodyCameCabinet =
    [
        "A while goes by. Nobody comes, and nobody was going to — the hall does not walk in here, and that " +
        "is the whole of what you are paying for.",
        "The hall carries on somewhere past the panelling, a long way off, like weather.",
    ];

    /// <summary>Every line in all three pools, for the canon grep. The guard walks THIS, so a line added tomorrow
    /// is checked tomorrow.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string s in NobodyCameBusy)
        {
            yield return s;
        }
        foreach (string s in NobodyCameQuiet)
        {
            yield return s;
        }
        foreach (string s in NobodyCameCabinet)
        {
            yield return s;
        }
        yield return TookTheTableLine;
        yield return StoodUpLine;
        // #783 · the other register, checked by the same grep the wary one is.
        yield return SatDownLine;
        yield return RelaxedSitLine;
        yield return RelaxedSitDryLine;
        yield return TheDrinkLine;
        yield return StoodUpRelaxedLine;
        yield return ApproachOpening;
        yield return WaveInLine;
        yield return WaveOffLine;
        yield return DrinkTakenLine;
        yield return DrinkDeclinedLine;
        yield return TheAskLine;
        yield return TheAskNote;
        yield return VisitorPlate;
        yield return FreeTablePlate;
        yield return OwnTablePlate;
    }

    /// <summary>
    /// #757 · WHAT NOTHING HAPPENING SOUNDS LIKE — the told outcome, so a wait that produced nobody reads as
    /// an answer rather than as a control that did not respond.
    /// </summary>
    /// <param name="watch">The shift, which decides which of the two silences this is.</param>
    /// <param name="beat">Which wait this was, so a captain who sits through four of them is told four
    /// different things and the room does not loop.</param>
    /// <param name="quiet">Whether this is a cabinet, where nobody was ever going to come.</param>
    public static string NobodyCame(long watch, int beat, bool quiet = false)
    {
        IReadOnlyList<string> pool = quiet
            ? NobodyCameCabinet
            : Fill(watch) >= BusyAt ? NobodyCameBusy : NobodyCameQuiet;
        return pool[(int)(((beat % pool.Count) + pool.Count) % pool.Count)];
    }

    // ── THE ONE WHO COMES OVER ────────────────────────────────────────────────────────────────────────
    //
    // She is a HAULIER with her coat still on, which is the register test (#701) surviving contact with a
    // quest-giver: she is not mysterious, she is not interesting, and she is not the plot. She is somebody
    // with an ordinary reason to cross a room, and every word she says is about her own family and her own
    // week. What is horrifying about it is a thing the game never states.
    //
    // AND SHE POINTS AT WHAT IS ALREADY BUILT. The Hand who has been here longer than the contract said
    // (#746) writes the names, the chit he writes them on rides the cage (#752), and the cage goes down.
    // Nothing new is promised by this scene that the game cannot already deliver.

    /// <summary>Who she reads as, at a glance, before she says anything.</summary>
    public const string VisitorPlate = "◈ A HAULIER WITH HER COAT STILL ON";

    /// <summary>What stopping at your table looks like from your side of it.</summary>
    public const string ApproachOpening =
        "Somebody has crossed the whole hall to stand at your table with her coat still on. \"Nobody's in " +
        "that one, are they.\"";

    /// <summary>The first rung: you pull the chair out, and she has a reason ready for why she should stay
    /// at it. The drink is offered by HER — which is the rung, and it is the offer this scene inverts.</summary>
    public const string WaveInLine =
        "She sits like somebody who has practised sitting down at strangers' tables. \"Let me get these " +
        "in. You don't have to drink it.\"";

    /// <summary>…or you do not, and it costs nothing. She goes, and the way she goes is the whole
    /// characterisation.</summary>
    public const string WaveOffLine =
        "\"Right. Fair enough.\" She goes back the way she came, and does not stop at anybody else's table " +
        "on the way.";

    /// <summary>The second rung, taken.</summary>
    public const string DrinkTakenLine =
        "Two glasses come over from the counter on her tab. She does not touch hers.";

    /// <summary>The second rung, declined — and declining is not a refusal of her, it is just an answer.</summary>
    public const string DrinkDeclinedLine =
        "\"Suit yourself.\" She folds her hands on the table, which is somehow worse.";

    /// <summary>
    /// THE THIRD RUNG — what she came over for, said plainly, the way somebody asks a stranger for a
    /// favour they have already rehearsed.
    ///
    /// <para>#761 · Told clearly. There is nothing to infer about what she wants; the inference is somewhere
    /// else entirely, and she does not know she is standing next to it.</para>
    /// </summary>
    public const string TheAskLine =
        "\"My brother took a down-contract here in the spring. The money still comes home every month, " +
        "regular as a clock, and there hasn't been a word with it since March. I can't get in the cage — " +
        "they know my face at the counter. You're new. Ask the hand who's been here longest. He's the one " +
        "who writes the names.\"";

    /// <summary>What the field book keeps of it. The book records what she said and what it points at, and
    /// never what it might mean.</summary>
    public const string TheAskNote =
        "A haulier's brother took a down-contract here in the spring. The money still comes home; he does " +
        "not write. She cannot get in the cage. The hand who has been here longest writes the names.";

    /// <summary>
    /// #757 · THE APPROACH, as an <see cref="Encounter.Scene"/> — the three-rung ladder.
    ///
    /// <para>Every rung is <see cref="Encounter.Requirement.ReplyToPriorMove"/>, so nothing is on the panel
    /// before the sentence it answers has been spoken (#749). Read down the list and the courtship is legible
    /// as data: the chair, then the drink, then the ask — and the ask needs the drink ANSWERED, either way,
    /// because turning a drink down is still having heard the offer.</para>
    /// </summary>
    public static Encounter.Scene TheVisitor() => new(
        "canteen:table:approach",
        VisitorPlate,
        Setting,
        ApproachOpening,
        [
            new(WaveIn, LabelOf(WaveIn), Says: WaveInLine),
            new(WaveOff, LabelOf(WaveOff), Says: WaveOffLine),

            new(LetThemBuy, LabelOf(LetThemBuy),
                Encounter.Requirement.ReplyToPriorMove, After: WaveIn, Says: DrinkTakenLine),
            new(NoDrink, LabelOf(NoDrink),
                Encounter.Requirement.ReplyToPriorMove, After: WaveIn, Says: DrinkDeclinedLine),

            // …and it is the ONE rung the field book keeps. The note rides the move (#757's addition to
            // Encounter.Move), so no client author has to remember which sentence was worth writing down.
            new(HearThemOut, LabelOf(HearThemOut),
                Encounter.Requirement.ReplyToPriorMove, After: LetThemBuy, OrAfter: NoDrink,
                Says: TheAskLine, Note: TheAskNote),

            new(Stand, LabelOf(Stand), Says: CanteenTable.LeaveLine),
        ]);
}
