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
public static partial class SittingAlone
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

    // ── #1016 · …AND WHERE IT IS WHEN THE ROOM IS YOUR OWN BOAT ───────────────────────────────────────
    //
    // Owner, live on 7 Deck with three drawn tops he could not pull a chair out at: "Why no table here to
    // sit at?", "Why no table in cabin either?", and the ruling that names the lane —
    // "I expect to have a bar table like this in this ships galley also.... feature complete."
    //
    // Two settings and one plate, because the ship is TWO rooms and they are not the same room. A cantina
    // top is a top in a room with a window and a counter in it; a cabin desk is a room with a DOOR, which
    // is the whole of why the spread is unconditional at one and not the other. Neither may borrow the
    // canteen's constant: a strip announcing "a table in the upper canteen" while the captain sits aboard
    // his own boat is this repository's "the sim doing one thing while a SENTENCE reports another" class,
    // and it is the exact fault #973 L5b caught one room over.

    /// <summary>#1016 · A top in the ship's own cantina. Owner-facing name for the room is CANTINA on the
    /// deck plan and "galley" in his own words; the sentence uses the one the plan draws, because the strip
    /// and the label must agree about which room the captain is in.</summary>
    public const string ShipCantinaSetting = "a top in your own cantina, the boat humming under it";

    /// <summary>#1016 · …and the desk in CABIN 1, which is the one seat aboard with a leaf between it and
    /// the rest of the ship. The door is the sentence's own fact and the privacy rung's at the same time.</summary>
    public const string ShipCabinSetting = "the desk in your own cabin, the door a step away";

    /// <summary>#1016 · Whose desk it is. Built out of <see cref="Glyph"/> exactly as
    /// <see cref="OwnTablePlate"/> is, so the family cannot end up with two chairs that are drawn
    /// differently.</summary>
    public const string OwnDeskPlate = Glyph + " YOUR OWN DESK";

    // ── #1040 · …AND THE THIRD ROOM ABOARD, WHICH IS THE COUNTER IN THE SAME ROOM AS THE FIRST ─────────
    //
    // Owner, on 7 Deck: "Our on ship bar can be upgraded to match the other bars... the UI represents code
    // long time ago." The cantina got the counter its own backdrop has always drawn, and a counter has
    // stools — which puts the captain on the BAR STOOL rung of the exposure ladder on his own boat, where
    // the gumshoe rule refuses the spread out loud (SeatedSpread.NotAtTheBarLine). That refusal is CORRECT
    // and it is funny: the man will not lay a case out at a bar, and it turns out he will not do it at his
    // own bar either. Nothing here bends to make it not so — the rung is the shipped rung, the refusal is
    // the shipped sentence, and the room simply has one more kind of seat in it.

    /// <summary>#1040 · A stool at the ship's own counter. Its own setting and not the cantina top's: the
    /// difference between the two is the whole of what the rung is about — a top has a wall you can put your
    /// back to, and a counter has your back to the room.</summary>
    public const string ShipCounterSetting = "a stool at your own counter, the galley dark behind it";

    /// <summary>#1040 · Whose counter it is, on the panel's plate. Built out of <see cref="Glyph"/> like the
    /// other two, because it is the same furniture family.</summary>
    public const string OwnStoolPlate = Glyph + " YOUR OWN COUNTER";

    /// <summary>#1040 · …and what the deck writes over the row before anything is pressed. It says the ACTION
    /// (#783's ruling on <see cref="FreeTablePlate"/>, one fixture along), because the plainest word for
    /// sitting down is sitting down.</summary>
    public const string FreeStoolPlate = Glyph + " THE COUNTER — TAKE A STOOL";

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

    /// <summary>
    /// #1016 · AND WHAT WAITING IS LIKE ABOARD YOUR OWN SHIP — a cantina with nobody else in it.
    ///
    /// <para>Its own pool and not the emptied hall's, for the reason every pool in this file is its own: the
    /// quiet pool counts eighty chairs and a card machine, and a captain sitting at one of three tops on a
    /// boat with a crew of droids would be told about a room three hundred thousand kilometres away. NOBODY
    /// EVER COMES to either of the ship's seats — there is nobody aboard to cross the floor — so these lines
    /// are not a wait that failed, they are what the ship sounds like when it does not need you.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> NobodyCameShipCantina =
    [
        "A while goes by. The boat ticks and settles around you the way she always does, and none of it " +
        "needs you.",
        "You wait. Somewhere aft a pump runs its minute and quits. The chair opposite stays yours.",
    ];

    /// <summary>#1016 · …and the same silence with a door between you and it. A cabin is the ship's cabinet
    /// rung, and the pool says so the way <see cref="NobodyCameCabinet"/> does: without ever stating it as a
    /// rule.</summary>
    public static readonly IReadOnlyList<string> NobodyCameShipCabin =
    [
        "A while goes by. The cabin holds exactly the amount of noise you brought into it.",
        "The boat carries on somewhere past the bulkhead, a long way off, like weather.",
    ];

    /// <summary>
    /// #1040 · …and the same boat again, heard from a stool at her counter with nobody serving.
    ///
    /// <para>Its own pool for the reason every pool in this file is its own: the cantina's second line says
    /// <i>the chair opposite stays yours</i>, and a stool has no chair opposite. A room's silence is made of
    /// the room's own furniture, and the fastest way this game has ever caught a lie is a sentence naming a
    /// thing the picture does not have.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> NobodyCameShipCounter =
    [
        "A while goes by. Nothing comes up out of the galley, because there is nobody down there and there " +
        "has not been for a long time.",
        "You sit at your own bar. The row of stools goes off into the dark and every one of them is empty, " +
        "which is either restful or it is not, depending on the day.",
    ];
}
