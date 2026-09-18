using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1230 · <b>THE ONE SLOT DESTROYED A SPENT BEAT, SO THE HOLD IS A QUEUE.</b>
///
/// <para><b>Head coder's ruling, 2026-09-18 — correctness, not feel.</b> #1222's one-slot argument (<i>the
/// captain would read exactly one of them anyway</i>) is true of #768's one-breath case and false of its own:
/// behind a card left open all evening two spent beats can be MINUTES apart, and the old
/// <c>PulseHold.Hold</c> annihilated one. #1231 measured it — Beat-over-Beat, Climax-over-Beat and
/// Beat-under-Climax each kept the incumbent or the newcomer and threw the other away, and the tail's two
/// notice lines carry no durable record at all, so one of them could be spent, held, overwritten and gone
/// with no trace in the save. <b>A once-only beat that is spent and never said is exactly what #1214 was
/// filed for</b>, arriving through the fix for #1214.</para>
///
/// <para>The law's five clauses are written down on <see cref="PulseHold"/> itself. This file asks each of
/// them of the shipping type, and asks two of them of the game's OWN sentences rather than of lines typed in
/// here — #592's climax and the tail's two notice lines — because a law proved only on "A" and "B" is a law
/// about a world the game does not build (the house's fifth named bug class).</para>
///
/// <h3>Red proof — every guard here was watched against the one-slot body</h3>
///
/// <para>The revert is #768's <c>Hold</c> and <c>ReleaseInto</c> put back verbatim:
/// <c>Message is not null &amp;&amp; rank &lt; Rank ? this : new(message, rank)</c>, and a release that
/// empties the hold in one go. That is the body that shipped until this PR, and it is the body the ruling
/// calls wrong.</para>
/// </summary>
public sealed class TheHoldIsAQueueTests
{
    /// <summary>Two lines raised on the one frame — #768's whole world, and the only case in which rank is
    /// allowed to re-order anything at all.</summary>
    private const double OneBreath = 1_000.0;

    /// <summary>The played case's own shape: a second frame, later, behind the same card that was up for the
    /// first. Nine seconds is the tail's own <c>NoticeSeconds</c>; the ruling's case is minutes, and either
    /// way the only thing that matters is that it is NOT the same frame.</summary>
    private const double NineSecondsLater = OneBreath + 9_000.0;

    // ── CLAUSE 1 · every line is kept, in order, and said one at a time ───────────────────────────────────

    [Fact]
    public void BeatOverBeatKeepsBOTHSentencesAndSaysBothInOrder()
    {
        // The ruling's first named case, and #1231's first measured one. Two beats raised minutes apart
        // behind one open card: the old body kept the newcomer (equal rank is not `rank < Rank`) and the
        // first was gone before the glass ever cleared.
        PulseHold held = PulseHold.Empty
            .Hold("The gate reads the card and the door gives.", PulseRank.Beat, OneBreath)
            .Hold("A tank somewhere behind the wall starts counting.", PulseRank.Beat, NineSecondsLater);

        Assert.Equal(2, held.Count);
        Assert.Equal(
            ["The gate reads the card and the door gives.", "A tank somewhere behind the wall starts counting."],
            Drain(held));
    }

    [Fact]
    public void ClimaxOverBeatKeepsBOTHAndABeatRaisedUnderAClimaxIsStillSaid()
    {
        // The other two rows of #1231's table, both directions. Neither line may be dropped, and — because
        // these are raised on DIFFERENT frames — rank does not re-order them either: what happened first is
        // read first. A queue that sorted globally by rank would be a second way to lose the order, and the
        // captain would read the evening backwards.
        Assert.Equal(["a beat", "A CLIMAX"], Drain(PulseHold.Empty
            .Hold("a beat", PulseRank.Beat, OneBreath)
            .Hold("A CLIMAX", PulseRank.Climax, NineSecondsLater)));

        Assert.Equal(["A CLIMAX", "a beat"], Drain(PulseHold.Empty
            .Hold("A CLIMAX", PulseRank.Climax, OneBreath)
            .Hold("a beat", PulseRank.Beat, NineSecondsLater)));
    }

    [Fact]
    public void EachLineGetsItsOwnFULLDwellAndTheNextWaitsForIt()
    {
        // "One line at a time, each for its own full duration, the next only after the previous expires."
        // Asked of the clock rather than of the list: a release offered one millisecond early must decline,
        // and the line must still be waiting afterwards rather than spent on a write that did not happen.
        const string first = "The gate reads the card and the door gives.";
        const string second = "A tank somewhere behind the wall starts counting.";
        PulseHold held = PulseHold.Empty
            .Hold(first, PulseRank.Beat, OneBreath)
            .Hold(second, PulseRank.Beat, NineSecondsLater);

        (PulseSlot slot, PulseHold left) = held.ReleaseInto(PulseSlot.Empty, 10_000.0);
        Assert.Equal(first, slot.Message);
        Assert.Equal(10_000.0 + PulseSlot.DwellFor(first), slot.ExpiresMs);
        Assert.Single(left.Queued);

        // A millisecond before the first line's time is up, the second may not have the slot.
        (PulseSlot tooSoon, PulseHold stillWaiting) = left.ReleaseInto(slot, slot.ExpiresMs - 1.0);
        Assert.Equal(slot, tooSoon);
        Assert.Equal(second, stillWaiting.Message);

        // …and on the frame it IS up, it takes it, with its own length-scaled dwell (#766) and no other.
        (PulseSlot then, PulseHold empty) = left.ReleaseInto(slot, slot.ExpiresMs);
        Assert.Equal(second, then.Message);
        Assert.Equal(slot.ExpiresMs + PulseSlot.DwellFor(second), then.ExpiresMs);
        Assert.False(empty.Any);
    }

    [Fact]
    public void TheTailsChairLineAndItsLosingLineUnderOneCardAreBothSaid()
    {
        // The ruling's own worked example, in the game's own words: both of these are spent ONCE by
        // construction, neither writes a durable record, and #1231 measured them landing under a card left
        // open. Nine seconds apart is not a hypothetical — it is TheTailBehindYou.NoticeSeconds, the price of
        // both halves of the craft.
        PulseHold held = PulseHold.Empty
            .Hold(TheTailBehindYou.FromThisChairLine, PulseRank.Beat, OneBreath)
            .Hold(TheTailBehindYou.LostLine, PulseRank.Beat, NineSecondsLater);

        Assert.Equal([TheTailBehindYou.FromThisChairLine, TheTailBehindYou.LostLine], Drain(held));
    }

    // ── CLAUSE 2 · rank never DROPS a line; it only orders ties raised in the same frame ──────────────────

    [Fact]
    public void RankOrdersONLYWithinOneFrameAndNeverAcrossFrames()
    {
        // The clause, from both sides in one guard. In one breath the biggest sentence is read first, which
        // is #693's law and #768's, kept. Across frames nothing is re-ordered at all, however big it is.
        Assert.Equal(["A CLIMAX", "a beat", "the weather"], Drain(PulseHold.Empty
            .Hold("the weather", PulseRank.Status, OneBreath)
            .Hold("a beat", PulseRank.Beat, OneBreath)
            .Hold("A CLIMAX", PulseRank.Climax, OneBreath)));

        Assert.Equal(["the weather", "a beat", "A CLIMAX"], Drain(PulseHold.Empty
            .Hold("the weather", PulseRank.Status, OneBreath)
            .Hold("a beat", PulseRank.Beat, OneBreath + 1.0)
            .Hold("A CLIMAX", PulseRank.Climax, OneBreath + 2.0)));
    }

    [Fact]
    public void AndAmongEqualsInOneFrameTheORDERCOMPOSEDIsTheOrderSaid()
    {
        // #768's tie-break was "the last held wins", and it was that because a later WRITE overwrote an
        // earlier one in a slot. A queue overwrites nothing, so there is no argument left for reversing an
        // author — two climaxes are read the way they were written (#677's seam then arrival).
        Assert.Equal(["first", "second", "third"], Drain(PulseHold.Empty
            .Hold("first", PulseRank.Climax, OneBreath)
            .Hold("second", PulseRank.Climax, OneBreath)
            .Hold("third", PulseRank.Climax, OneBreath)));
    }

    [Fact]
    public void NOTHINGIsEverDroppedByRankHoweverManyLinesRace()
    {
        // The anti-vacuous shape of clause 2, swept: every ranked line handed to the queue comes back out,
        // whatever the ranks around it were, in every order they could have been raised in. The old body
        // failed this on the first pair.
        PulseRank[] ranks = [PulseRank.Status, PulseRank.Beat, PulseRank.Climax];
        int cases = 0;

        foreach (PulseRank a in ranks)
        {
            foreach (PulseRank b in ranks)
            {
                foreach (PulseRank c in ranks)
                {
                    foreach (double frame in new[] { OneBreath, double.NaN })
                    {
                        cases++;
                        // NaN stands for "each on its own frame", spelled out below so the two sweeps differ
                        // in exactly one thing.
                        bool sameFrame = !double.IsNaN(frame);
                        PulseHold held = PulseHold.Empty
                            .Hold("A", a, OneBreath)
                            .Hold("B", b, sameFrame ? OneBreath : OneBreath + 1.0)
                            .Hold("C", c, sameFrame ? OneBreath : OneBreath + 2.0);

                        List<string> said = Drain(held);
                        Assert.Equal(["A", "B", "C"], said.OrderBy(s => s, StringComparer.Ordinal).ToList());
                        if (!sameFrame)
                        {
                            Assert.Equal(["A", "B", "C"], said);
                        }
                    }
                }
            }
        }

        Assert.Equal(54, cases);
    }

    // ── CLAUSE 3 · an identical sentence already waiting is not queued twice ──────────────────────────────

    [Fact]
    public void AnIdenticalSentenceAlreadyWaitingIsNotQueuedTwice()
    {
        // A world that raises the same words twice behind one card has said one thing, and saying it twice in
        // a row would read as a stutter. The words decide, not the rank and not the frame.
        PulseHold held = PulseHold.Empty
            .Hold(TheTailBehindYou.FromThisChairLine, PulseRank.Beat, OneBreath)
            .Hold(TheTailBehindYou.FromThisChairLine, PulseRank.Beat, NineSecondsLater)
            .Hold(TheTailBehindYou.FromThisChairLine, PulseRank.Climax, NineSecondsLater + 1.0);

        Assert.Single(held.Queued);
        Assert.Equal([TheTailBehindYou.FromThisChairLine], Drain(held));

        // …and a DIFFERENT sentence is not refused by a near miss: the test is the whole string.
        Assert.Equal(2, held.Hold(TheTailBehindYou.LostLine, PulseRank.Beat, NineSecondsLater).Count);
    }

    // ── CLAUSE 4 · ambient is unchanged ──────────────────────────────────────────────────────────────────

    [Fact]
    public void WeatherIsNotPlotSignificantAndIsThereforeNeverHeldAtAll()
    {
        // The clause lives at the funnel (ShowPulseMessage holds at Telling.Floor and above and nowhere
        // else), and the client guard is the one that drives it. What Core can say — and must, because the
        // floor is the thing both halves read — is that the floor really is the top two ranks, so "held" and
        // "plot-significant" cannot drift apart into two definitions.
        Assert.False(PulseRank.Status.IsPlotSignificant());
        Assert.True(PulseRank.Beat.IsPlotSignificant());
        Assert.True(PulseRank.Climax.IsPlotSignificant());
        Assert.Equal(PulseRank.Beat, Telling.Floor);
    }

    // ── CLAUSE 5 · the bound is soft and may never cost a Beat ────────────────────────────────────────────

    [Fact]
    public void TheBoundDropsTheOldestAMBIENTMostLineAndNeverABeat()
    {
        PulseHold held = PulseHold.Empty;
        for (int i = 0; i < PulseHold.TheBound; i++)
        {
            // Weather first, then beats — so the oldest lines in the queue are the droppable ones.
            held = held.Hold($"line {i}", i < 3 ? PulseRank.Status : PulseRank.Beat, OneBreath + i);
        }

        Assert.Equal(PulseHold.TheBound, held.Count);

        held = held.Hold("the ninth", PulseRank.Beat, NineSecondsLater);
        Assert.Equal(PulseHold.TheBound, held.Count);
        Assert.DoesNotContain(held.Queued, w => w.Message == "line 0");   // the oldest weather went
        Assert.Contains(held.Queued, w => w.Message == "line 1");         // …and only the oldest
        Assert.Contains(held.Queued, w => w.Message == "the ninth");
    }

    [Fact]
    public void ButEightBeatsWaitingAreALLKeptAndTheBoundGivesWay()
    {
        // "…if eight Beats are genuinely waiting, keep them all and let the bound be soft." A bound that ate
        // a beat would be the bug this PR fixes, with a number on it.
        PulseHold held = PulseHold.Empty;
        for (int i = 0; i < PulseHold.TheBound; i++)
        {
            held = held.Hold($"beat {i}", PulseRank.Beat, OneBreath + i);
        }

        PulseHold ninth = held.Hold("beat 8", PulseRank.Climax, NineSecondsLater);
        Assert.Equal(PulseHold.TheBound + 1, ninth.Count);
        Assert.Equal(Enumerable.Range(0, 9).Select(i => $"beat {i}").ToList(), Drain(ninth));

        // …and the weather does NOT get to push one of them out on its way in: what is allowed to be missed
        // is the thing that is missed.
        PulseHold weather = held.Hold("the weather", PulseRank.Status, NineSecondsLater);
        Assert.Equal(PulseHold.TheBound, weather.Count);
        Assert.DoesNotContain(weather.Queued, w => w.Message == "the weather");
    }

    // ── Not persisted, and the empty cases ───────────────────────────────────────────────────────────────

    [Fact]
    public void TheQueueIsNotPersistedAndIsNotInTheVaultsShapeAtAll()
    {
        // The ruling: "Not persisted, for #1222's own reason (a reload has already lost the card)." The
        // honest way to ask that of Core is to ask the vault what it carries — if the hold ever grew a row
        // there, this goes red on the day it is typed rather than on the day a save is loaded.
        IEnumerable<System.Reflection.MemberInfo> carried = typeof(Vault)
            .GetProperties()
            .Cast<System.Reflection.MemberInfo>()
            .Concat(typeof(Vault).GetFields());

        Assert.DoesNotContain(carried, m => TypeOf(m).Name.Contains("PulseHold", StringComparison.Ordinal));
        Assert.DoesNotContain(carried, m => TypeOf(m).Name.Contains("PulseSlot", StringComparison.Ordinal));
        Assert.DoesNotContain(carried, m => m.Name.Contains("Pulse", StringComparison.Ordinal));
    }

    [Fact]
    public void ADefaultedHoldReadsAsEmptyRatherThanThrowing()
    {
        // A record struct's default is not its Empty, and this one carries a list. A page field that had
        // never been assigned would take the whole frame down with it, which is a worse bug than the one
        // being fixed.
        PulseHold nothing = default;
        Assert.False(nothing.Any);
        Assert.Equal(0, nothing.Count);
        Assert.Null(nothing.Message);
        Assert.Empty(nothing.Queued);

        (PulseSlot slot, PulseHold left) = nothing.ReleaseInto(PulseSlot.Empty, 0.0);
        Assert.Equal(PulseSlot.Empty, slot);
        Assert.False(left.Any);
        Assert.Equal("a line", nothing.Hold("a line", PulseRank.Beat, OneBreath).Message);
    }

    // ── Plumbing ─────────────────────────────────────────────────────────────────────────────────────────

    private static Type TypeOf(System.Reflection.MemberInfo member) => member switch
    {
        System.Reflection.PropertyInfo p => p.PropertyType,
        System.Reflection.FieldInfo f => f.FieldType,
        _ => typeof(void),
    };

    /// <summary>Everything the hold is carrying, said onto a real slot at the pace the law gives it: one at a
    /// time, the next only once the one before has had its whole dwell. The clock is stepped by the pulse's
    /// own <see cref="PulseSlot.DwellFor"/> rather than by a number chosen here, so this helper cannot
    /// disagree with the shipping dwell about when the glass is free.</summary>
    private static List<string> Drain(PulseHold held)
    {
        var said = new List<string>();
        PulseSlot slot = PulseSlot.Empty;
        double nowMs = 0.0;

        for (int guard = 0; held.Any && guard <= PulseHold.TheBound + 4; guard++)
        {
            (PulseSlot next, PulseHold left) = held.ReleaseInto(slot, nowMs);
            Assert.NotEqual(held.Count, left.Count);   // no progress would mean an endless loop, not a pass
            said.Add(next.Message!);
            slot = next;
            held = left;
            nowMs = next.ExpiresMs;
        }

        Assert.False(held.Any, "the queue would not drain — something is stuck at its head.");
        return said;
    }
}
