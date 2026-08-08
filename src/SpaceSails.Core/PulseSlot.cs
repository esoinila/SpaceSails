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
