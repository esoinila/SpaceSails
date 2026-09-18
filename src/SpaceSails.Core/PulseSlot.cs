using System;
using System.Collections.Generic;

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
/// <para>So the event <b>holds</b> its sayings instead of saying them, and the card's dismissal lets them
/// go.</para>
///
/// <para><b>#1230 · AND THE HOLD IS A QUEUE, because one slot DESTROYED a spent beat.</b> #768 kept exactly
/// one line and chose it by rank, and #1222 made this funnel the biggest one in the game — every
/// plot-significant line the WORLD raises while any of the thirty-one scrims is up now comes here. The
/// one-slot argument (<i>the captain would read exactly one of them anyway</i>) is true of #768's own
/// one-breath case and false of #1222's: behind a card left open all evening two spent beats can be MINUTES
/// apart, and the old <c>Hold</c> annihilated one of them. The tail's chair reading and its losing line,
/// raised nine seconds apart under one open card, are the played case — and a once-only beat that is spent
/// and never said is exactly the class #1214 was filed for, arriving through #1214's own fix.</para>
///
/// <para><b>The law now, in five clauses</b> (head coder's ruling, 2026-09-18 — correctness, not feel):</para>
/// <list type="number">
///   <item><b>Every line is kept, IN ORDER</b>, and said one at a time when the glass clears — each for its
///   own full pulse duration, the next only after the previous has expired (<see cref="SayTheNextAfterMs"/>).
///   The HUD still has one slot; what changed is that the queue waits for it rather than writing over
///   itself.</item>
///   <item><b>Rank never DROPS a line.</b> It may only ORDER lines raised in the SAME FRAME, highest first —
///   which is #768's one-breath case and the whole of what rank was ever doing here. Across frames the order
///   raised is the order said, absolutely: a beat from ten minutes ago is not re-sorted behind one from now
///   because the newer one is bigger.</item>
///   <item><b>An identical sentence already waiting is not queued twice.</b> A world that raises the same
///   words twice behind one card has said one thing.</item>
///   <item><b>Ambient is unchanged</b> — never held by <c>ShowPulseMessage</c>, so never queued by it.
///   Weather is allowed to be missed, which is what makes it weather. (#768's own callers — an arrival
///   holding its whole breath — still hand this queue their Status lines, and those are the only droppable
///   things in it, which is clause 5.)</item>
///   <item><b>The bound is SOFT.</b> <see cref="TheBound"/> lines; on overflow the OLDEST AMBIENT-MOST line
///   is dropped, and <b>never</b> one at <see cref="Telling.Floor"/> or above. If eight beats are genuinely
///   waiting, all eight are kept — a bound that ate a beat would be this bug with a number on it.</item>
/// </list>
///
/// <para><b>Not persisted</b>, for #1222's own reason: a reload has already lost the card that caused the
/// hold, and nothing in <c>TheScrimCensus</c> is in the vault either. What is deferred is the doorbell; the
/// durable record was never deferred at all.</para>
///
/// <para>Releasing writes through <see cref="PulseSlot.Write"/>, so each freed line gets its ordinary dwell
/// (#766's length-scaled reading time) and can itself be outranked afterwards. A held line is a line that
/// has not been said yet — never a line with special powers: if the slot's own law refuses the write, the
/// line keeps its place at the head of the queue and is offered again next frame.</para></summary>
/// <param name="Lines">The sentences waiting, oldest first, or null/empty for nothing held.</param>
/// <param name="SayTheNextAfterMs">When the line released last has finished its dwell — nothing else may be
/// said before it, which is what "one at a time, each for its own full duration" is made of.</param>
public readonly record struct PulseHold(IReadOnlyList<PulseHold.Waiting>? Lines, double SayTheNextAfterMs)
{
    /// <summary>One sentence waiting its turn, with what it is and the frame it was raised on. The frame is
    /// carried rather than inferred because it is the whole of clause 2: rank may re-order lines raised in
    /// the SAME frame and nothing else.</summary>
    /// <param name="Message">The words.</param>
    /// <param name="Rank">What they are (#693).</param>
    /// <param name="RaisedAtMs">The client's real-time clock on the frame they were raised.</param>
    public readonly record struct Waiting(string Message, PulseRank Rank, double RaisedAtMs);

    /// <summary>How many lines may wait. Eight is a card left open a long while, not a leak; past it a
    /// player is not going to read a backlog anyway. It is SOFT: see clause 5 — this bound may never cost a
    /// line at <see cref="Telling.Floor"/> or above.</summary>
    public const int TheBound = 8;

    /// <summary>Nothing held — the state every scene starts and ends in.</summary>
    public static PulseHold Empty => new(Array.Empty<Waiting>(), 0.0);

    /// <summary>The queue, oldest first. Never null, so a defaulted struct reads as empty rather than
    /// throwing.</summary>
    public IReadOnlyList<Waiting> Queued => Lines ?? Array.Empty<Waiting>();

    /// <summary>Is there a sentence waiting for the card to close?</summary>
    public bool Any => Queued.Count > 0;

    /// <summary>How many are waiting.</summary>
    public int Count => Queued.Count;

    /// <summary>The next thing that will be said, or null for nothing waiting.</summary>
    public string? Message => Count > 0 ? Queued[0].Message : null;

    /// <summary>What the next thing that will be said IS.</summary>
    public PulseRank Rank => Count > 0 ? Queued[0].Rank : PulseRank.Status;

    /// <summary>Keep <paramref name="message"/> back for now. Nothing is displaced and nothing is dropped
    /// except under the soft bound; <paramref name="rank"/> decides only where in this frame's own run of
    /// lines it goes. Returns the hold as it stands afterwards.</summary>
    /// <param name="message">The sentence.</param>
    /// <param name="rank">What it is (#693).</param>
    /// <param name="raisedAtMs">The client's clock on this frame — two lines sharing it are two lines raised
    /// in one breath, and those are the only two rank may ever re-order.</param>
    public PulseHold Hold(string message, PulseRank rank, double raisedAtMs)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return this;   // an empty saying is not a saying; it must never take a place in the queue
        }

        IReadOnlyList<Waiting> queued = Queued;
        foreach (Waiting already in queued)
        {
            // CLAUSE 3. The words, and not the rank: a world that says the same sentence twice behind one
            // card has said one thing, and saying it twice in a row would read as a stutter.
            if (string.Equals(already.Message, message, StringComparison.Ordinal))
            {
                return this;
            }
        }

        var next = new List<Waiting>(queued);
        if (next.Count >= TheBound)
        {
            // CLAUSE 5. The oldest of the AMBIENT-most rank present — scanning forward and taking only a
            // strictly lower rank lands on the FIRST line of the lowest rank, which is the oldest of them.
            int drop = -1;
            for (int i = 0; i < next.Count; i++)
            {
                if (!next[i].Rank.IsPlotSignificant() && (drop < 0 || next[i].Rank < next[drop].Rank))
                {
                    drop = i;
                }
            }

            if (drop >= 0)
            {
                next.RemoveAt(drop);
            }
            else if (!rank.IsPlotSignificant())
            {
                // Nothing droppable is waiting and the newcomer is weather. A beat may not be dropped to
                // make room for the weather, and the weather is the thing that is allowed to be missed.
                return this;
            }

            // …otherwise eight beats are genuinely waiting and a ninth has arrived: ALL of them are kept and
            // the bound gives way. That is the clause, written as the absence of a drop.
        }

        // CLAUSE 2. Walk back over the lines raised on THIS frame only, past any of lower rank, and sit in
        // front of them. Lines from earlier frames are never passed, so the order raised is the order said.
        int at = next.Count;
        while (at > 0 && next[at - 1].RaisedAtMs == raisedAtMs && next[at - 1].Rank < rank)
        {
            at--;
        }

        next.Insert(at, new Waiting(message, rank, raisedAtMs));
        return new PulseHold(next, SayTheNextAfterMs);
    }

    /// <summary>The glass is clear: say the NEXT one. Returns the slot with that line written into it (by the
    /// ordinary law, so it dwells and can be outranked like anything else) and the hold with it gone.
    ///
    /// <para>One line per call, and never before <see cref="SayTheNextAfterMs"/> — the line released last has
    /// its own full dwell before the next one takes the slot. Called once a frame from the tick, so a queue
    /// simply drips.</para>
    ///
    /// <para>Nothing held is not an event: the slot comes back untouched, so a card dismissed on a quiet
    /// screen never blanks or re-writes whatever the world has said since. Nor is a REFUSAL an event — if the
    /// slot's own rank law will not take this line yet (something bigger is still inside its breath), the
    /// line keeps its place and is offered again on the next frame rather than being spent on a write that
    /// did not happen.</para></summary>
    public (PulseSlot Slot, PulseHold Held) ReleaseInto(PulseSlot slot, double nowMs)
    {
        if (!Any || nowMs < SayTheNextAfterMs)
        {
            return (slot, this);
        }

        Waiting head = Queued[0];
        PulseSlot after = slot.Write(head.Message, head.Rank, nowMs);
        if (after == slot)
        {
            return (slot, this);   // the slot refused it; it has not been said, so it is not spent
        }

        var rest = new List<Waiting>(Queued);
        rest.RemoveAt(0);
        return (after, new PulseHold(rest, after.ExpiresMs));
    }
}
