using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · WHAT IS SAID AT THE TABLE — small talk, the round, paper on the table, asking about work, the
/// chit and its cover, the mess and the gate with the chit in your hand.
///
/// <para>Split out of <c>CanteenTable.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>, and the nested <c>Cover</c> class
/// travels whole.</para>
/// </summary>
public static partial class CanteenTable
{
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
}
