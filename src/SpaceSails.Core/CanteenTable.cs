using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #746 · THE PROTO TABLE — B1's canteen, three regulars, and the one job that goes downstairs.
///
/// <para>Owner, 2026-08-06: <i>"the proto is the lab bar: with all the charm we can muster, get the job that
/// takes us downstairs — and politely dodge the jobs that don't."</i></para>
///
/// <h3>What this file is, and what it deliberately is not</h3>
///
/// <para>It is CONTENT on <see cref="Encounter"/>. Every mechanic it uses — the three bands, the situational
/// modifiers, the requirement kinds, the free exit — belongs to the framework and is shared with a guard stop
/// that has not been written yet. Nothing here is a canteen-shaped special case, because the moment the
/// checkpoint arrives it must be a file like this one and not a second engine.</para>
///
/// <h3>The three of them are three ANSWERS, not three quest-givers</h3>
///
/// <list type="bullet">
/// <item><b>The Hand</b> is the door. Their ask-about-work is the only rolled move at this table, and all
/// three of its bands hand you something: the chit, the chit under a name you did not choose, or a refusal
/// that opens the fitter and arms the temp. <b>The scene moves in every one of them.</b></item>
/// <item><b>The Fitter</b> is the dead end, and is honest about it: real work, real pay, on the surface,
/// going nowhere down. Never rolled — honest work offered plainly is not a check — and the polite dodge is
/// its own labelled option that costs exactly nothing. That dodge IS the owner's smoke test.</item>
/// <item><b>The Temp</b> has no job at all. What they have is the house's ways, and knowing them first is the
/// +1 on the Hand's ask: belonging reads.</item>
/// </list>
///
/// <h3>§13.8 holds, hardest, here</h3>
///
/// <para>Not one line in this file says what the facility is for. The talk is freight, signatures, shifts,
/// pay, scaffold metres and a rota that corrects your name — and the most horrifying sentence available is a
/// temp explaining, helpfully, how to stop being who you were at the door.</para>
///
/// <para>Pure and deterministic. The client owns the WATCH state (which moves have been made at which table);
/// this owns the words, the effects and the arithmetic.</para>
/// </summary>
public static class CanteenTable
{
    /// <summary>Which of the three regulars a table's occupant is — or none, for the rest of the cast, who
    /// keep #709's one-breath tap and are not a scene.</summary>
    public enum Who
    {
        /// <summary>Nobody this table talks to at length. The carrier, the driver, the quiet one.</summary>
        None,

        /// <summary>The one who has been here longer than the contract said. The way down.</summary>
        Hand,

        /// <summary>Off a maintenance contract. Honest metres, going nowhere down.</summary>
        Fitter,

        /// <summary>First week, and already answering to somebody else's name.</summary>
        Temp,

        /// <summary>#751 · One of the hall's BACKGROUND PATRONS — a face in the crowd that is the cover.
        /// A thin scene and deliberately so: small talk, the round, your leave. No asks, no jobs. The depth
        /// stays with the named regulars, and the hall is still ALIVE to the social system.</summary>
        Stranger,
    }

    // ── WHO IS AT THE TABLE, read off the plate Core already wrote ────────────────────────────────────
    //
    // The plates below are CanteenRegulars' own, minus the glyph it prefixes every one of them with. They are
    // matched rather than re-authored, and a guard asserts each one still names exactly one member of that
    // cast — because the day somebody edits a plate, this file must go red rather than quietly stop having a
    // Hand in it.

    /// <summary>The Hand's plate, as <see cref="CanteenRegulars"/> writes it.</summary>
    public const string HandPlate = "A HAND WHO HAS BEEN HERE LONGER THAN THE CONTRACT SAID";

    /// <summary>The Fitter's plate.</summary>
    public const string FitterPlate = "A FITTER, OFF A MAINTENANCE CONTRACT";

    /// <summary>The Temp's plate.</summary>
    public const string TempPlate = "AN AGENCY TEMP, FIRST WEEK";

    /// <summary>Which of the three, if any, is behind a seated regular's plate.</summary>
    public static Who WhoIs(string? plate) => plate is null
        ? Who.None
        : plate.EndsWith(HandPlate, StringComparison.Ordinal) ? Who.Hand
        : plate.EndsWith(FitterPlate, StringComparison.Ordinal) ? Who.Fitter
        : plate.EndsWith(TempPlate, StringComparison.Ordinal) ? Who.Temp
        : Who.None;

    // ── THE MOVES ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Small talk. Free, and the only move that is worth making twice.</summary>
    public const string SmallTalk = "smalltalk";

    /// <summary>The second line. Gated — on having heard the first, and for the Temp on a round having been
    /// bought (or on their having overheard the Hand wave you off).</summary>
    public const string SmallTalkAgain = "smalltalk2";

    /// <summary>Buy the round. Coin, and a +1 on every ask at this table afterwards.</summary>
    public const string Round = "round";

    /// <summary>Put something on the table. The satchel as a conversational move.</summary>
    public const string Show = "show";

    /// <summary>Ask about work. The rolled one.</summary>
    public const string Work = "work";

    /// <summary>Take your leave. <see cref="Encounter.Leave"/>, so the framework's own free-exit law
    /// applies to this table without this file restating it.</summary>
    public const string Leave = Encounter.Leave;

    /// <summary>Take the scaffold job you do not intend to work.</summary>
    public const string TakeScaffold = "scaffold-take";

    /// <summary>The polite dodge. Its own labelled option, and it costs nothing — the whole point.</summary>
    public const string DodgeScaffold = "scaffold-dodge";

    /// <summary>What a round costs down here. A canteen on a company floor is not a bar with a house special;
    /// three glasses of whatever is on tap is cheap, and it needs to be — the +1 it buys is the single most
    /// useful thing a stranger can do in this room. FLAGGED for the owner's tuning.</summary>
    public const int RoundPrice = 12;

    /// <summary>The button labels, kept beside the ids so a panel never invents its own vocabulary for a move
    /// the design named.</summary>
    public static string LabelOf(string moveId) => moveId switch
    {
        SmallTalk => "Small talk",
        // #746 · Not a second "Small talk". Two identically-labelled buttons side by side, one of them
        // greyed, reads as a bug in a screenshot — and the second line IS a different thing: it is what they
        // tell you once you have not left.
        SmallTalkAgain => "Keep talking",
        Round => $"Buy the round · {RoundPrice} cr",
        Show => "Put something on the table",
        Work => "Ask about work",
        TakeScaffold => "Take the scaffold job",
        DodgeScaffold => "Not my trade — but thanks",
        _ => "Take your leave",
    };

    /// <summary>Asking for the chair — the beat the owner said was missing (<i>"asking to sit is
    /// missing"</i>). No roll: sitting is cheap in bar culture and most working people wave you in, and the
    /// kinds who would not are a scene this proto does not have. It is still a thing you DO, and the wave-in
    /// is the answer to it rather than a caption that was on the screen all along.</summary>
    public const string Join = "join";

    /// <summary>What that button says.</summary>
    public const string AskToJoin = "Ask to join";

    // ── THE WAVE-INS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>What they say when you ask for the chair.</summary>
    public static string WaveIn(Who who) => who switch
    {
        Who.Hand => "There's the seat. Nobody's in it.",
        Who.Fitter => "Sit. Mind the grease.",
        Who.Temp => "Oh — sure. I mean, yes. Sit.",
        // #751 · A stranger's wave-in is a shrug and a chair moved two inches, which is exactly what it is
        // in a room of eighty people who have never seen you and will not remember you.
        Who.Stranger => "Chair's free.",
        _ => "",
    };

    /// <summary>
    /// #842 · …and what a top with NO CHAIR LEFT says instead. The counterpart to a wave-in, and it is a
    /// wave-in's opposite in the one way that matters: it is SAID.
    ///
    /// <para>Owner, filing it (#603's house law, one room over): a refusal is spoken, and <i>"a control that
    /// quietly does something else is how a player learns the wrong lesson."</i> Before #840 gave the tops
    /// honest heads, a genuinely full table barely existed; the moment it did, [E] at one fell through to the
    /// patron's one-breath card — readable, but not an answer to the thing the player pressed.</para>
    ///
    /// <para>ONE PRESS, ONE SENTENCE, WALK ON. Pressing harder does not open it: what is being said at a full
    /// top is something you OVERHEAR by sitting nearby, which the neighbour machinery already owns, and
    /// standing over four strangers pressing E at their food is not a way into a conversation in any room
    /// anybody has ever been in. The same shape as the counter's full row
    /// (<see cref="Interior.TheStools.RowIsFullLine"/>), which is the control this game teaches with.</para>
    /// </summary>
    public const string TableIsFullLine =
        "Every chair at this top is spoken for. Whatever is being said there, it is not waiting for you.";

    // ── SMALL TALK ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The first line of somebody's day.</summary>
    public static string SmallTalkFirst(Who who) => who switch
    {
        Who.Hand =>
            "Six weeks, the contract said. I stopped counting at the point where counting looked like a " +
            "complaint.",
        Who.Fitter =>
            "Scaffold work on the south face. Honest metres, paid in honest money, and the wind does the " +
            "swearing for you.",
        Who.Temp =>
            "They took my name at the door and put a different one on the rota. Said it's easier that way.",
        _ => "",
    };

    /// <summary>
    /// The second line — the one that is worth coming back for.
    ///
    /// <para>The Temp's is <b>THE HOUSE'S WAYS</b>, and it is gated on a round having been bought at their
    /// table (or, after the Hand waves you off, on their having overheard it — the scene moved). Learning it
    /// is the +1 on the Hand's ask, which is the whole mechanical shape of belonging in this room: you do not
    /// get the job by being impressive, you get it by having already been here a while.</para>
    /// </summary>
    public static string SmallTalkSecond(Who who) => who switch
    {
        Who.Hand =>
            "The freight cage goes down full and comes up full, and nobody upstairs asks what of. That's " +
            "not carelessness. That's policy.",
        Who.Fitter =>
            "I don't ask what's under this rock. A contract that doesn't say is a contract that pays extra " +
            "for not saying.",
        Who.Temp =>
            "You want to get along here? Answer to the name they give you, not the one you brought. The " +
            "rota isn't wrong. You are. That's the system.",
        _ => "",
    };

    /// <summary>Does this counterpart's second line have to be EARNED, or is it simply the next thing they
    /// say? The Temp's is the house's ways and is bought with a round or overheard after the Hand's refusal;
    /// the other two just keep talking if you stay.</summary>
    public static bool SecondLineIsEarned(Who who) => who == Who.Temp;

    /// <summary>Does hearing this line teach the house's ways — the +1 the Hand's ask reads? Only one line in
    /// the room does, and it is the one the Temp is not aware is worth anything.</summary>
    public static bool TeachesTheHouse(Who who, bool second) => who == Who.Temp && second;

    // ── BUY THE ROUND ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>What buying the round looks like. The drink WALKS OVER from the counter fixture Core already
    /// put in this room (#707) — a line, not an animation, and deliberately not a menu in the abstract.</summary>
    public const string RoundLine =
        "Three glasses walk over from the counter. Nobody says thank you and everybody drinks — down here " +
        "that IS thank you.";

    // ── PUT SOMETHING ON THE TABLE ────────────────────────────────────────────────────────────────────

    /// <summary>What a file on somebody does to a table in a company canteen. It is LOUD: the ask closes at
    /// this table for the watch, and the field book keeps the slip.</summary>
    public const string DirtLine =
        "Put that away. Not because I care — because the counter has eyes and you just taught it your face.";

    /// <summary>#751 · What a file on somebody does to a table the counter cannot see. The same slip, the
    /// same person, a different room — and the whole difference is stated by the counterpart rather than by
    /// a rule anybody reads out.</summary>
    public const string QuietLine =
        "They read it properly, which nobody does out there. Then they put it face down and leave their " +
        "hand on it. \"Say the rest.\"";

    /// <summary>#751 · What the field book keeps of that. It records what happened, never the mechanic.</summary>
    public const string CabinetLeverageNote =
        "You put a file on a table in a room with one door, and the conversation did not stop.";

    /// <summary>The deep authority card, on a table where nobody at it has ever seen one. The Hand goes
    /// quiet, and their ask stops being a favour and becomes fear.</summary>
    public const string DeepCardLine =
        "…Where did you get that. No — don't. Whatever you're here for, you're overqualified for the cage.";

    /// <summary>Everything else made of paper.</summary>
    public const string OtherPaperLine =
        "They look at it the way you look at weather on another moon.";

    /// <summary>#746 · WHICH card silences a table. Shaft 4 is deep enough that a hand who has spent six
    /// weeks in the cage has never held one — and deeper is more so, which is why this is a floor and not an
    /// equality. <see cref="UndergroundComplex.CardTitle"/> prints the band one-based, so band 3 is the card
    /// that reads SHAFT 4.</summary>
    public const int OverqualifiedBand = 3;

    /// <summary>Is this the card that stops the conversation?</summary>
    public static bool IsOverqualifying(Satchel.Item item) =>
        item.Kind == Satchel.Kind.Authority
        && UndergroundComplex.AuthorityCard.TryParse(item.Id, out UndergroundComplex.AuthorityCard card)
        && card.Band >= OverqualifiedBand;

    // ── ASK ABOUT WORK ────────────────────────────────────────────────────────────────────────────────

    /// <summary>YES. The cage is short-handed and you look like hands.</summary>
    public const string HandWorkYes =
        "Cage crew's short a pair of hands since Tuesday's Tuesday. Take this to the lift and don't be " +
        "clever near the counter.";

    /// <summary>YES, BUT — and the BUT is that a stranger is about to exist downstairs with your face and
    /// somebody else's name. The em-dash pause is the beat; it is one string on purpose.</summary>
    public const string HandWorkYesBut =
        "Cage crew's short. I'll put you in the book— what name am I writing? …No. I'll pick one. Easier " +
        "for everybody.";

    /// <summary>NO — AND THE SCENE MOVES. Waved off toward the fitter, in front of the temp, who hears it.</summary>
    public const string HandWorkNoAnd =
        "Not you. No offence — you hold your shoulders like somebody who's never carried for pay. The " +
        "fitter's hiring, if metres don't scare you.";

    /// <summary>The Fitter's offer. Never rolled: honest work offered plainly is not a check.</summary>
    public const string FitterWorkLine =
        "South face scaffold, four watches, pay at the end of each. Wind's free.";

    /// <summary>Taking the scaffold job. The field book is blunt about what you have just agreed to.</summary>
    public const string ScaffoldTakenNote =
        "You have a scaffold job you do not intend to work.";

    /// <summary>The polite dodge, and its whole cost. <b>Nothing changes.</b></summary>
    public const string ScaffoldDodgedLine =
        "He nods. The wind will do the swearing either way.";

    /// <summary>Standing up. Free, always available, never penalised — and the line says why that is a
    /// skill rather than a formality.</summary>
    public const string LeaveLine =
        "You stand, and nobody minds. Down here leaving a table right is a skill, and you have it.";

    // ── THE CHIT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The chit, as it was given: a name on a list that you gave them.</summary>
    public const string ChitId = "chit:daylabour";

    /// <summary>The chit, as it was written: a name on a list that the Hand chose. Same paper, different
    /// FACT — and it is the fact that #718's rollback ladder will pull, so it lives in the id the vault
    /// stores rather than in a sentence the field book happens to keep.</summary>
    public const string ChitUnderAnotherNameId = "chit:daylabour:another-name";

    /// <summary>What is printed on it.</summary>
    public const string ChitTitle =
        "DAY-LABOUR CHIT · CARRIERS & CONTRACTORS · CAGE CREW · SHOW AT THE CAGE";

    /// <summary>The glyph the satchel row wears. The card's own words are untouched; a pocket puts an icon
    /// in front of everything it lists.</summary>
    public const string ChitGlyph = "🎟";

    /// <summary>What the field book makes of it. Not "you got a job" — the book says what the paper is FOR,
    /// which is the only reading of it that matters two floors down.</summary>
    public const string ChitGist =
        "The chit is cover: a reason to be in the cage, a name on a list, a door that opens because " +
        "paperwork says so.";

    /// <summary>And what it makes of the other way of getting it.</summary>
    public const string ChitUnderAnotherNameGist =
        "You are on the cage crew's book now, under a name the Hand chose. Somebody downstairs believes " +
        "that person exists.";

    /// <summary>The chit as a thing in the satchel.</summary>
    public static Satchel.Item Chit(bool underAnotherName) =>
        new(Satchel.Kind.Chit, underAnotherName ? ChitUnderAnotherNameId : ChitId);

    /// <summary>
    /// #746/#618 · THE COVER, as a fact Core can be asked about.
    ///
    /// <para>The chit's PRESENCE is the state. There is no second flag, no parallel ledger, no "cover: true"
    /// written anywhere — the possession is the record, it is already durable because the satchel is, and it
    /// is already destroyed by leaving the paper behind because #688's drop is. When #618's guards arrive
    /// they read this, and what they read cannot have drifted from what the player is carrying.</para>
    /// </summary>
    public static class Cover
    {
        /// <summary>Is the captain carrying a reason to be in the cage?</summary>
        public static bool Held(IReadOnlyList<Satchel.Item>? carried) =>
            Satchel.CountOf(carried, Satchel.Kind.Chit) > 0;

        /// <summary>Is the name on the list one the captain gave? #718's thread: the chit that came with a
        /// YES-BUT was written under a name the Hand picked, and somewhere downstairs a book says that person
        /// exists.</summary>
        public static bool UnderAnotherName(IReadOnlyList<Satchel.Item>? carried) =>
            Satchel.CountOf(carried, Satchel.Kind.Chit, ChitUnderAnotherNameId) > 0;
    }

    // ── THE MESS, WITH THE CHIT IN YOUR HAND ──────────────────────────────────────────────────────────

    /// <summary>#743/#746 · What showing the chit at the staff mess is worth. One beat, once per excursion,
    /// in the room the pass exists for — the payoff that proves the paper is a possession and not a token in
    /// a ledger nobody sees.</summary>
    public const string MessBeatLine =
        "You show the chit to a room with nobody in it, and eat. Company food tastes the same on every " +
        "world, and that is somehow the most human thing this building has done.";

    /// <summary>What eating gives back, in whole pips through the ordinary nerve system. Small — it is a
    /// meal, not a bunk — and it is the one relief this building has ever offered. FLAGGED for tuning.</summary>
    public const int MessBeatPips = 1;

    // ── #752 · THE GATE, WITH THE CHIT IN YOUR HAND ───────────────────────────────────────────────────
    //
    // The Hand says "take this to the lift and don't be clever near the counter", and until now the lift had
    // never heard of it: the sealed row answered beautifully and never once looked at the wallet, so the
    // sentence the job was hired to finish stopped one door short of the door it was about.
    //
    // These are the two sentences that finish it — one said when the doors open, one written in the book —
    // and the tone is the whole point. The countersignature card is answered by an office that outlived its
    // owners and is still vouching for whoever holds the paper. The chit is answered by a man doing a shift.

    /// <summary>#752 · What the cage's gate makes of a day-labour chit, said when the doors open on the band
    /// below. Nothing salutes and nothing recognises you: the paperwork simply balances.</summary>
    public const string ChitGateLine =
        "The gate reads the chit the way a tired man reads a timesheet: date, crew, done. The cage takes " +
        "you down as freight takes the cage — on somebody's account, no questions carried.";

    /// <summary>#752 · And what the field book makes of it. Not "the gate opened" — the book keeps what the
    /// paper turned out to be worth, which is the only reading of it that matters on the floor it just put
    /// you on.</summary>
    public const string ChitGateGist =
        "The chit works. Downstairs is a place you are now paid to be.";

    // ── THE SCENE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What the setting is called, for <see cref="Encounter.Scene.Setting"/>. The room's own sign
    /// (#707) says what it is; this says where in it you are.</summary>
    public const string Setting = "a table in the upper canteen";

    /// <summary>
    /// #746 · The table, as an <see cref="Encounter.Scene"/>.
    ///
    /// <para>This is the file's real claim: the proto is CONTENT. Everything the panel needs — which moves
    /// exist, what each costs, which are rolled — is data on the framework's own types, and a guard stop will
    /// be another function exactly like this one.</para>
    /// </summary>
    /// <param name="who">Which regular.</param>
    /// <param name="overheard">Whether the Temp overheard the Hand wave you off — the NO-AND's third effect.
    /// It relaxes their second line's requirement rather than granting it, because the line is still
    /// something you have to sit down and ask for.</param>
    public static Encounter.Scene SceneFor(Who who, bool overheard = false)
    {
        var moves = new List<Encounter.Move>
        {
            new(SmallTalk, LabelOf(SmallTalk)),
        };

        // The second line. For the Hand and the Fitter it is simply the next thing they say if you stay; for
        // the Temp it is the house's ways, and it is bought with a round — unless the scene already moved.
        moves.Add(SecondLineIsEarned(who) && !overheard
            ? new(SmallTalkAgain, LabelOf(SmallTalkAgain), Encounter.Requirement.PriorMoveThisWatch, After: Round)
            : new(SmallTalkAgain, LabelOf(SmallTalkAgain), Encounter.Requirement.PriorMoveThisWatch, After: SmallTalk));

        moves.Add(new(Round, LabelOf(Round), Encounter.Requirement.Credits, RoundPrice));

        // No KIND named: the gesture is "put something on the table", and which something is the next press.
        moves.Add(new(Show, LabelOf(Show), Encounter.Requirement.SatchelItem));

        // The Hand's is the only rolled move at this table. The Fitter's is honest work offered plainly, and
        // the Temp has none at all — which is not a gap, it is who they are in their first week.
        if (who == Who.Hand)
        {
            moves.Add(new(Work, LabelOf(Work), Rolled: true));
        }
        else if (who == Who.Fitter)
        {
            // #749 · THE ANSWERS ARE ANSWERS. Both of these are replies to one sentence — "South face
            // scaffold, four watches, pay at the end of each" — and until he has said it there is nothing to
            // reply to, so they are not in the panel at all. They used to be shown greyed with "Not yet." on
            // them, which is a menu with two items you may not order (found by playing it, #749).
            moves.Add(new(Work, LabelOf(Work), Says: FitterWorkLine));
            moves.Add(new(TakeScaffold, LabelOf(TakeScaffold),
                Encounter.Requirement.ReplyToPriorMove, After: Work));
            moves.Add(new(DodgeScaffold, LabelOf(DodgeScaffold),
                Encounter.Requirement.ReplyToPriorMove, After: Work, Says: ScaffoldDodgedLine));
        }

        // Last, and free, and on every scene this file will ever build.
        moves.Add(new(Leave, LabelOf(Leave), Says: LeaveLine));

        return new Encounter.Scene(
            $"canteen:table:{who}".ToLowerInvariant(),
            PlateOf(who),
            Setting,
            WaveIn(who),
            moves);
    }

    /// <summary>
    /// #751 · A STRANGER'S TABLE — the thin scene, on the very same machine.
    ///
    /// <para>Three moves and no fourth. Small talk (their bark, drawn by <see cref="CanteenRegulars"/> per
    /// patron per watch), buy the round (the +1 applies exactly as it does anywhere — a stranger's table is
    /// where you warm up cheap), and take your leave. <b>Ask-about-work is not on it</b>, and that is the
    /// design rather than an omission: a hall where every one of eighty faces has a job to hand out is a
    /// quest hub, and the room's whole job is to be a room.</para>
    ///
    /// <para>Everything about it is <see cref="Encounter"/>'s — which is the claim this file has been making
    /// since #746, now tested by a second kind of counterpart existing at all.</para>
    /// </summary>
    /// <param name="plate">Who they read as, as the deck drew them.</param>
    public static Encounter.Scene StrangerScene(string plate) => new(
        $"canteen:table:{Who.Stranger}".ToLowerInvariant(),
        plate ?? "",
        Setting,
        WaveIn(Who.Stranger),
        [
            new(SmallTalk, LabelOf(SmallTalk)),
            new(Round, LabelOf(Round), Encounter.Requirement.Credits, RoundPrice),
            new(Leave, LabelOf(Leave), Says: LeaveLine),
        ]);

    /// <summary>#751 · One line of a stranger's day: the bark they were dealt this watch, and nothing
    /// else — no state, no modifier, no memory. It is a room being a room.</summary>
    public static Answer StrangerSaid(string bark) => new(bark ?? "");

    /// <summary>The plate this counterpart reads as, without the glyph.</summary>
    public static string PlateOf(Who who) => who switch
    {
        Who.Hand => HandPlate,
        Who.Fitter => FitterPlate,
        Who.Temp => TempPlate,
        _ => "",
    };

    // ── THE ANSWER ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// What one move did: the words, and every consequence, as data. The client applies it; nothing about
    /// what a band GRANTS is decided in a razor file, so a guard can force a band and pin the state.
    /// </summary>
    /// <param name="Line">What is said. Rendered inside the panel (#680), never only pulsed.</param>
    /// <param name="Band">Which band it settled in. <see cref="Encounter.Band.Yes"/> for unrolled moves.</param>
    /// <param name="GrantsChit">The day-labour chit goes in the wallet.</param>
    /// <param name="UnderAnotherName">…and the name in the book is not one you gave.</param>
    /// <param name="OpensFitter">The fitter's own ask about work is now on offer.</param>
    /// <param name="HardensTable">This table is harder for the rest of the watch (−1 on further asks).</param>
    /// <param name="ArmsTheTemp">The temp overheard it, and their second line is available without a round.</param>
    /// <param name="ClosesTheAsk">Ask-about-work is shut at this table for this watch.</param>
    /// <param name="TeachesTheHouse">The house's ways are learned (+1 on the Hand's ask).</param>
    /// <param name="NervePips">Whole pips spent, through <see cref="NervePips"/> and nothing else.</param>
    /// <param name="Note">What the field book keeps, or null when the moment is not worth filing.</param>
    public readonly record struct Answer(
        string Line,
        Encounter.Band Band = Encounter.Band.Yes,
        bool GrantsChit = false,
        bool UnderAnotherName = false,
        bool OpensFitter = false,
        bool HardensTable = false,
        bool ArmsTheTemp = false,
        bool ClosesTheAsk = false,
        bool TeachesTheHouse = false,
        int NervePips = 0,
        string? Note = null);

    /// <summary>
    /// #746 · THE HAND'S ASK, resolved. Three bands, three granted states, and the scene moves in all three.
    ///
    /// <para>The nerve cost is <see cref="Encounter.NervePipsFor"/> — the framework's, not a number invented
    /// here — so a table and a checkpoint charge fear the same way.</para>
    /// </summary>
    public static Answer HandAsksAboutWork(Encounter.Band band) => band switch
    {
        Encounter.Band.Yes => new(
            HandWorkYes, band,
            GrantsChit: true,
            ClosesTheAsk: true,
            NervePips: Encounter.NervePipsFor(band),
            Note: ChitGist),

        Encounter.Band.YesBut => new(
            HandWorkYesBut, band,
            GrantsChit: true,
            UnderAnotherName: true,
            ClosesTheAsk: true,
            NervePips: Encounter.NervePipsFor(band),
            Note: ChitUnderAnotherNameGist),

        // NO — AND. The refusal is the busiest outcome in the file, which is exactly what "the scene moves"
        // has to mean if it means anything: a door shuts, a different door opens, and a third person in the
        // room now has something to say that they did not have a minute ago.
        _ => new(
            HandWorkNoAnd, band,
            OpensFitter: true,
            HardensTable: true,
            ArmsTheTemp: true,
            NervePips: Encounter.NervePipsFor(band),
            Note: null),
    };

    /// <summary>The Fitter's ask. Never rolled, and it is not a refusal to leave it on the table.</summary>
    public static Answer FitterAsksAboutWork() => new(FitterWorkLine);

    /// <summary>Taking the scaffold job.</summary>
    public static Answer ScaffoldTaken() => new(FitterWorkLine, Note: ScaffoldTakenNote);

    /// <summary>
    /// #749 · THE ANSWER TO A MOVE WHOSE OUTCOME IS FIXED: what it says, and nothing else.
    ///
    /// <para>The framework's <see cref="Encounter.Move.Says"/> — "the outcome is FIXED, and here is the line
    /// it is fixed to" — made into this file's <see cref="Answer"/>. It exists so a panel can hand back a
    /// reply it has never heard of: the client that presses a move looks at what the move CARRIES rather than
    /// at a list of ids somebody remembered to write down, which is how the polite dodge came to be the only
    /// free move in the game that spoke.</para>
    ///
    /// <para><b>THE POLITE DODGE IS THIS, and the law it carries is the default of every other field:
    /// it costs nothing and changes nothing.</b> The owner smoke-tests it by hand ("politely dodge the jobs
    /// that don't"), and a dodge that quietly spent a pip or hardened a table would fail that test without
    /// anybody noticing for a month — so there is nowhere for a cost to be added except by hand, here, to
    /// every fixed outcome in the game at once.</para>
    /// </summary>
    /// <para>#757 · …and whatever the move says the BOOK should keep, by the same law and for the same
    /// reason: a content file names what is worth writing down, and no client author has to remember to.
    /// The dodge's is null, which is the nothing it has always cost.</para>
    public static Answer SaidPlainly(Encounter.Move move) => new(move.Says ?? "", Note: move.Note);

    /// <summary>Standing up. Free, and it files nothing.</summary>
    public static Answer TookTheirLeave() => new(LeaveLine);

    /// <summary>Buying the round.</summary>
    public static Answer BoughtTheRound() => new(RoundLine);

    /// <summary>One line of somebody's day. The only one that changes anything is the Temp's second, and what
    /// it changes is what YOU know — which is the modifier the Hand's ask reads.</summary>
    public static Answer MadeSmallTalk(Who who, bool second) =>
        new(second ? SmallTalkSecond(who) : SmallTalkFirst(who),
            TeachesTheHouse: TeachesTheHouse(who, second));

    /// <summary>
    /// Putting something on the table.
    ///
    /// <para>Three readings, in the order a room would have them: the file on somebody is LOUD and shuts the
    /// ask; the deep card silences the Hand and turns their ask into fear rather than friendship; everything
    /// else made of paper is weather on another moon.</para>
    /// </summary>
    /// <param name="item">What went on the table.</param>
    /// <param name="who">Who is sitting at it — the deep card only lands on the Hand, because they are the
    /// only one at this table who knows what it is worth.</param>
    /// <param name="quiet">#751 · Whether this table is in a CABINET. The LOUD reading of a file is not a
    /// fact about the paper, it is a fact about the ROOM — <i>"the counter has eyes"</i> — so in a room with
    /// no line of sight to the counter it does not apply. See <see cref="QuietLine"/>.</param>
    public static Answer PutOnTheTable(Satchel.Item item, Who who, bool quiet = false)
    {
        if (item.Kind == Satchel.Kind.Dirt)
        {
            // ── #751 · THE QUIET RULE ────────────────────────────────────────────────────────────────
            //
            // Owner: "have cabinet-spaces for sensitive negotiations." This is what makes one — not a
            // label on a door, but the one mechanic in the game that a room can switch off.
            //
            // #746's LOUD closure has always been about the counter rather than about the slip: "not
            // because I care — because the counter has eyes and you just taught it your face." A cabinet
            // is a room the counter cannot see, so the sentence does not apply in it, and NOTHING here
            // closes. The player is never told; they put a file down in a cabinet one day and the ask is
            // still there afterwards, and the card they read on the way in already said why.
            if (quiet)
            {
                return new(QuietLine, Note: CabinetLeverageNote);
            }

            // The line IS the note. Inventing a second sentence about the same slip would be two voices
            // describing one event, and the one the captain heard is the one worth keeping.
            return new(DirtLine, ClosesTheAsk: true, Note: DirtLine);
        }

        if (who == Who.Hand && IsOverqualifying(item))
        {
            // Fear, not friendship. Nothing is granted HERE — the card does not hand you a chit, it settles
            // the ask you have not made yet (see ForcesTheYesBut), which is why this answer changes no state
            // and only speaks. A move that both spoke and granted would be two beats on one press.
            return new(DeepCardLine, Encounter.Band.YesBut);
        }

        return new(OtherPaperLine);
    }

    /// <summary>Does what is on the table read as relevant — the +1 in <see cref="Encounter.Situation"/>?
    /// Only the card the Hand cannot look away from. A shipping manifest is paper; it is not leverage.</summary>
    public static bool CountsAsPaperOnTheTable(Satchel.Item item, Who who) =>
        who == Who.Hand && IsOverqualifying(item);

    /// <summary>#746 · Does putting THIS card down settle the Hand's ask without a roll? The card does not
    /// persuade them; it frightens them into writing you down. Auto-resolved as YES-BUT, because being
    /// afraid of you is exactly a yes that costs something.</summary>
    public static bool ForcesTheYesBut(Satchel.Item item, Who who) => CountsAsPaperOnTheTable(item, who);
}
