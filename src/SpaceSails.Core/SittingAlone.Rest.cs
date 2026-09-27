using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE OTHER REGISTER: A SHORT REST (#783) — when a sit reads as rest, the lines for it, and what the
/// panel shows you.
///
/// <para>Split out of <c>SittingAlone.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>.</para>
/// </summary>
public static partial class SittingAlone
{
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

    /// <summary>
    /// #1016 · …AND THE SAME GLASS ABOARD YOUR OWN BOAT, where B4 is three hundred million kilometres away.
    ///
    /// <para>Found by the #1019 crew playing it and left for the canon hand, correctly: the line above rode
    /// to the ship's own cantina and told the captain about a still in a basement the boat has never been
    /// near. Same beat, same two-sentence rhythm, right world — the pour aboard is the rum locker's
    /// (<c>PourRum</c> is the one funnel), and the locker's whole character is that it is yours.</para>
    /// </summary>
    public const string TheDrinkAboardLine =
        "The pour is cold and it is honest about what it is. It came out of your own locker, and nobody " +
        "waters what they pour for themselves.";

    /// <summary>#1016 · The rest with a glass, aboard. <see cref="RelaxedSitLine"/> ends on <i>nobody in
    /// this building</i>, and a boat is not a building — the clause aboard is the boat's, and <i>she</i> is
    /// the ship, which is how this game has always said it.</summary>
    public const string RelaxedSitAboardLine =
        "It feels good to sit down for a change. You put your boots up on the spare chair and let the cold " +
        "glass sweat into your hand, and for as long as it lasts, she asks nothing of you.";

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
    /// <param name="aboard">#1016 · Whether this seat is on the captain's own ship — in which case the sit
    /// ALWAYS reads relaxed. The watch clause above is a question about how full a public room is, and the
    /// boat's rooms are not filled by anybody's rota: a busy hour ashore was putting the captain's back to
    /// the wall of his own empty cantina, hands where they could be seen by nobody. Your own boat is the
    /// rest register by construction; the glass only decides whether it gets its own sentence.</param>
    public static bool SitReadsAsRelaxed(bool drinkInHand, long watch, bool aboard = false) =>
        aboard || drinkInHand || Fill(watch) < BusyAt;

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
    /// <param name="aboard">#1016 · On the captain's own ship the glass sentences are the boat's — the
    /// counter's still and the canteen's building both live somewhere else. The dry rest is one line in both
    /// worlds on purpose: <i>"for as long as nobody needs you, nobody needs you"</i> owns no venue, and a
    /// second copy of it with a boat in it would be a fork with nothing on it.</param>
    public static string SitLine(bool relaxed, bool drinkInHand, bool aboard = false) =>
        !relaxed ? TookTheTableLine
        : drinkInHand && aboard ? RelaxedSitAboardLine + " " + TheDrinkAboardLine
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
    /// <param name="aboard">#1016 · Whether the table is on the captain's own ship, which picks the boat's
    /// glass sentences over the counter's — see <see cref="SitLine"/>. The scene is otherwise the shipped
    /// one, because the POSTURE is the same posture everywhere it exists.</param>
    public static Encounter.Scene TheTable(bool relaxed = false, bool drinkInHand = false, bool aboard = false) => new(
        "canteen:table:alone",
        OwnTablePlate,
        Setting,
        SitLine(relaxed, drinkInHand, aboard),
        [
            new(Wait, LabelOf(Wait)),
            new(Stand, LabelOf(Stand), Says: StoodUp(relaxed)),
        ]);
}
