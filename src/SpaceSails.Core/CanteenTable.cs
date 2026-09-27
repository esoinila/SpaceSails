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
public static partial class CanteenTable
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
}
