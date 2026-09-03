using System;

namespace SpaceSails.Core;

/// <summary>#693 · WHAT A PULSE IS FOR, and therefore who wins the one slot when several want it at once.
///
/// <para>The HUD's pulse line has exactly one slot. Until here the rule was "last write wins", which meant
/// the order the sayings happened to be written in was load-bearing everywhere and written down nowhere —
/// and the biggest sentence in the Hive (#592's first words on a floor that does not exist) had been losing
/// it to the routine air line since the day it shipped. Three call sites had comments explaining that they
/// were deliberately LAST; that is a contract, and a contract that lives in comments is not one.</para>
///
/// <para>The ranks are about what a line IS, never about how loud it is. A status line that has been given a
/// story rank to make it win is the same bug with better manners.</para></summary>
public enum PulseRank
{
    /// <summary>Instruments, hardware, prices, refusals, routine narration. Everything written before #693
    /// is this, and it is the default: a line that has not thought about its rank is a status line.</summary>
    Status = 0,

    /// <summary>Something happened once and the book will keep it — a gate read a card, a car dropped for
    /// the first time, a tank started counting. Worth more than the weather.</summary>
    Beat = 1,

    /// <summary>The sentence a whole feature was built to say. There are a handful of these in the game and
    /// they are all authored prose; nothing routine may stand on top of one.</summary>
    Climax = 2,
}

/// <summary>#761 · WHAT "PLOT-SIGNIFICANT" IS, in one place, so no call site ever decides it again.
///
/// <para><b>Owner ruling, 2026-08-08:</b> <i>"We should make sure we tell the user clearly when plot
/// significant things happen."</i> The bug family that ruling names had been arriving one instance at a
/// time — #689/#693 (the #592 climax losing the one slot to the routine air line), #736 (the outcome landing
/// behind a modal backdrop), #740 (the sentence misattributing its number) — and each got a local guard. A
/// law needs a definition, and the definition needs to be a thing code can be asked, not a paragraph.</para>
///
/// <para>So: <b>plot-significant means the top two ranks.</b> It changes what the captain knows, owes, is
/// owed, or can do — a reveal, a debt, a standing gained or lost, a door that will now open, somebody who
/// will now remember — and that is exactly the line <see cref="PulseRank.Beat"/> already draws against
/// <see cref="PulseRank.Status"/>. No second enum, no second vocabulary: #693's ranks were already the right
/// shape and were only ever missing a name for their top half.</para>
///
/// <para><b>This changes nothing that ships.</b> It writes down a comparison the game already makes — the
/// slot's own law is <c>rank &lt; Rank</c>, and everything at or above <see cref="Floor"/> is what that law
/// exists to protect. What it buys is a single place for <c>ThePlayerIsToldTests</c> to point at, instead of
/// a magic <c>&gt;= PulseRank.Beat</c> copied into a guard — which on this ground is the stale mirror, and
/// the stale mirror is how a law quietly stops being one.</para>
///
/// <para>The warning that belongs beside it is #693's own, unchanged: the ranks are about what a line IS,
/// never about how loud it is. Marking a status line plot-significant so it wins the slot does not make the
/// moment matter; it makes the rank stop meaning anything, and then the real climax loses to it.</para></summary>
public static class Telling
{
    /// <summary>The lowest rank that counts as plot-significant. Everything from here up is a moment the
    /// player must be told about on the surface they are looking at; everything below it is weather.</summary>
    public const PulseRank Floor = PulseRank.Beat;

    /// <summary>Is a line at this rank one the law is about?</summary>
    public static bool IsPlotSignificant(this PulseRank rank) => rank >= Floor;
}

/// <summary>#693 · THE ONE SLOT, WITH A LAW.
///
/// <para>The law, in one line: <b>a lower-ranked line may not displace a higher-ranked one that is still
/// being held</b>; among equals the last written wins, exactly as before. So an arrival may compose its
/// sayings in whatever order reads best and the climax is the one on screen — no call site has to know what
/// the call sites after it are going to say.</para>
///
/// <para><b>The hold is short on purpose.</b> It is <see cref="MinDwellMs"/> — the pulse's own floor, the
/// shortest time any line is ever up — and not the full dwell of the winning line. A climax can dwell eight
/// seconds, and eight seconds in which a pressed button answers nothing is the #686 bug wearing this fix as
/// a disguise. A breath is enough: the lines that race for this slot race in the same frame or the tick
/// after it, which is exactly what the hold covers.</para>
///
/// <para>Pure and here in Core rather than in the razor page, so the sweep over every arrival the Hive's
/// generator admits can ask the shipping law rather than a copy of it.</para></summary>
/// <param name="Message">What is on screen, or null for an empty slot.</param>
/// <param name="Rank">What that message is, which is what decides who may overwrite it.</param>
/// <param name="ExpiresMs">When it fades, in the client's real-time clock.</param>
/// <param name="HeldUntilMs">Until when it outranks: the window inside which a lesser line is refused.</param>
public readonly record struct PulseSlot(
    string? Message, PulseRank Rank, double ExpiresMs, double HeldUntilMs)
{
    /// <summary>An empty slot — nothing on screen, and nothing to outrank.</summary>
    public static PulseSlot Empty => new(null, PulseRank.Status, 0.0, 0.0);

    /// <summary>Owner 2026-07-18 ("it autodisappears which is not convenient"): a line lingers long enough to
    /// READ, so the dwell scales with its length. Short status pulses keep the old brisk floor; long intel
    /// lines get up to <see cref="MaxDwellMs"/>.</summary>
    public const double MsPerChar = 45.0;

    /// <summary>The floor of the dwell, and the length of the rank hold. One number, two uses, so the hold
    /// can never be argued about: it is simply the shortest time the screen ever shows anything.</summary>
    public const double MinDwellMs = 1500.0;

    /// <summary>The ceiling of the dwell. The words a player paid a round to hear are not gone before they
    /// land, and a sentence is still not a modal.</summary>
    public const double MaxDwellMs = 8000.0;

    /// <summary>How long <paramref name="message"/> stays up.</summary>
    public static double DwellFor(string? message) =>
        Math.Clamp((message?.Length ?? 0) * MsPerChar, MinDwellMs, MaxDwellMs);

    /// <summary>Write a line into the slot, or decline to. Returns the slot as it stands afterwards — the
    /// same slot, unchanged, when the law refuses the write.</summary>
    public PulseSlot Write(string message, PulseRank rank, double nowMs)
    {
        // THE LAW. Note what it is NOT: it never asks which line is longer, more recent or more interesting,
        // and it never looks at the text at all. Only the rank, and only while the winner is still held.
        if (Message is not null && rank < Rank && nowMs < HeldUntilMs)
        {
            return this;
        }

        return new PulseSlot(message, rank, nowMs + DwellFor(message), nowMs + MinDwellMs);
    }

    /// <summary>Clear the slot if its line has had its time. An empty slot outranks nothing, which is what
    /// keeps the hold from leaking into the next scene.</summary>
    public PulseSlot Expire(double nowMs) =>
        Message is not null && nowMs > ExpiresMs ? Empty : this;
}

/// <summary>#768 · THE SAYINGS AN EVENT KEPT BACK, BECAUSE THE SAME EVENT RAISED A CARD OVER THEM.
///
/// <para><see cref="PulseSlot"/> settles a pulse losing to a pulse. It cannot settle the other loss, and the
/// other loss is total: an ARRIVAL that raises a card — the first descent (#585), the dead-air warning
/// (#609), the gate the paper opened (#689), the repo boat setting down (#583) — writes its line into the
/// slot and then puts a full-screen backdrop in front of it. No rank helps, because the line is not losing
/// to a bigger line; it is losing to the whole HUD, and by the time the card is dismissed the dwell has run
/// out and the sentence is gone. #680's family, arising from the world acting rather than from a press.</para>
///
/// <para>So the event <b>holds</b> its sayings instead of saying them, and the card's dismissal lets the
/// winner go. That is the queue shape #693 declined — deliberately, and it stays declined: this is not a
/// lifecycle the pulse's 400-odd call sites share, it is one slot for one situation, and an event that
/// raises no card never touches it (it releases on the spot, which is an ordinary pulse).</para>
///
/// <para><b>The winner is chosen by the same law, not by the order.</b> The lines an arrival holds were
/// composed in one breath, so who survives the card is a question of RANK — a lower-ranked held line may not
/// displace a higher-ranked one; among equals the last held wins. Identical to <see cref="PulseSlot.Write"/>
/// minus the clock, because there is no clock inside a breath, and identical on purpose: the sentence that
/// survives the card must be the sentence that would have been on screen had no card been raised. That
/// equivalence is the guard.</para>
///
/// <para>Releasing writes through <see cref="PulseSlot.Write"/>, so the freed line gets its ordinary dwell
/// (#766's length-scaled reading time) and can itself be outranked afterwards. A held line is a line that
/// has not been said yet — never a line with special powers.</para></summary>
/// <param name="Message">The best thing held so far, or null for nothing held.</param>
/// <param name="Rank">What that held message is, which is what decides whether a later one displaces it.</param>
public readonly record struct PulseHold(string? Message, PulseRank Rank)
{
    /// <summary>Nothing held — the state every scene starts and ends in.</summary>
    public static PulseHold Empty => new(null, PulseRank.Status);

    /// <summary>Is there a sentence waiting for the card to close?</summary>
    public bool Any => Message is not null;

    /// <summary>Keep <paramref name="message"/> back for now, or decline to because something bigger is
    /// already being kept back. Returns the hold as it stands afterwards.</summary>
    public PulseHold Hold(string message, PulseRank rank)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return this;   // an empty saying is not a saying; it must never displace a real one
        }

        // THE SAME LAW AS THE SLOT'S, and written out rather than borrowed, because the slot's is about a
        // line that is ON SCREEN and this one is about lines that never got there. Only the rank decides.
        return Message is not null && rank < Rank ? this : new(message, rank);
    }

    /// <summary>The card is gone: say the winner. Returns the slot with the freed line written into it (by
    /// the ordinary law, so it dwells and can be outranked like anything else) and an empty hold.
    ///
    /// <para>Nothing held is not an event: the slot comes back untouched, so a card dismissed on a quiet
    /// screen never blanks or re-writes whatever the world has said since.</para></summary>
    public (PulseSlot Slot, PulseHold Held) ReleaseInto(PulseSlot slot, double nowMs) =>
        Message is null ? (slot, this) : (slot.Write(Message, Rank, nowMs), Empty);
}
