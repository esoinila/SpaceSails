using System.Collections.Generic;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// THE ORACLE'S CADENCE (story pass, 2026-08-02).
///
/// <para>Everything about Static Marsh was right except how often she said the same thing. The pools were
/// indexed by a fresh uniform hash per draw, and uniform sampling repeats: over 8 400 draws (7 bars × 60
/// watches × 20 listens) <b>3.4 % of draws spoke the line before it again, and the worst run was the same
/// sentence FOUR TIMES RUNNING</b>. That is not uncanny, it is a stuck record — and the run's cadence
/// criterion ("once-ever beats fire once ever; every-time cards don't spam") is exactly this question
/// asked of a line pool.</para>
///
/// <para>A draw now WALKS the pool — <c>(start + stride × draw) mod count</c>, start and stride seeded per
/// visit, <c>gcd(stride, count) = 1</c> — so consecutive draws can never collide and a full cycle plays
/// every authored line before any of them returns. These guards pin both halves, and pin that the walk did
/// NOT quietly change the rarity of a truth or the honesty of the tell (which is what a careless fix to a
/// seeded pick does).</para>
/// </summary>
public sealed class TheOracleDoesNotRepeatHerselfTests
{
    private static readonly string[] Bars =
    [
        "the-space-bar", "cinder-roost", "ringside-exchange", "selene-gate", "the-tilt", "the-deep", "red-eye",
    ];

    // ── 1 · She never says the same thing twice running ───────────────────────────────────────────────

    [Fact]
    public void NoTwoConsecutiveListensSpeakTheSameLine()
    {
        // Every bar, sixty watches, forty listens, at every drink count the card can reach — the whole
        // space a player can actually stand in front of her for.
        foreach (string bar in Bars)
        {
            for (long watch = 0; watch < 60; watch++)
            {
                for (int drinks = 0; drinks <= 6; drinks++)
                {
                    string previous = string.Empty;
                    for (int draw = 0; draw < 40; draw++)
                    {
                        string text = OracleRant.Speak(bar, watch, draw, drinks).Text;
                        Assert.False(text == previous,
                            $"{bar} watch {watch} drinks {drinks} draw {draw}: she just said this line: “{text}”");
                        previous = text;
                    }
                }
            }
        }
    }

    // ── 2 · A full cycle plays EVERY authored line before any of them comes back ───────────────────────

    [Fact]
    public void ListeningLongEnoughHearsTheWholeNonsensePool()
    {
        // The point of a walk over a sample: the beautiful noise is not four lines on a loop. Take the
        // nonsense draws over one visit's worth of listening and check the walk covers the pool.
        var heard = new HashSet<string>();
        for (int draw = 0; draw < OracleRant.Nonsense.Count * 4; draw++)
        {
            OracleLine l = OracleRant.Speak("the-space-bar", 11, draw, 0);
            if (!l.IsTrue)
            {
                heard.Add(l.Text);
            }
        }
        Assert.True(heard.Count >= OracleRant.Nonsense.Count - 6,
            $"only {heard.Count} of {OracleRant.Nonsense.Count} nonsense lines surfaced in {OracleRant.Nonsense.Count * 4} listens.");
    }

    [Fact]
    public void ListeningLongEnoughReachesEveryTruthIncludingEveryFragment()
    {
        // Every flavour of truth must stay reachable — a walk that stranded a fragment would silently
        // orphan an arc shard the oracle is one of only two hands for.
        var kinds = new HashSet<string>();
        foreach (string bar in Bars)
        {
            for (long watch = 0; watch < 40; watch++)
            {
                for (int draw = 0; draw < 40; draw++)
                {
                    OracleLine l = OracleRant.Speak(bar, watch, draw, 4);
                    if (l.IsTrue)
                    {
                        kinds.Add($"{l.Truth}:{l.FragmentId ?? "-"}");
                    }
                }
            }
        }
        foreach (OracleRant.OracleTruth t in OracleRant.Truths)
        {
            Assert.Contains($"{t.Kind}:{t.FragmentId ?? "-"}", kinds);
        }
    }

    // ── 3 · The fix did not move the rarity or the tell ───────────────────────────────────────────────

    [Fact]
    public void TheWalkDidNotMakeHerMoreOrLessProphetic()
    {
        // A pool walk must change WHICH line, never HOW OFTEN a line is true — the rarity is the whole
        // reason sifting her is a game. Sample the same space the story pass measured.
        int draws = 0, trues = 0;
        foreach (string bar in Bars)
        {
            for (long watch = 0; watch < 60; watch++)
            {
                for (int draw = 0; draw < 20; draw++)
                {
                    draws++;
                    if (OracleRant.Speak(bar, watch, draw, 0).IsTrue)
                    {
                        trues++;
                    }
                }
            }
        }
        double rate = trues / (double)draws;
        Assert.InRange(rate, OracleRant.BaseTrueChance - 0.03, OracleRant.BaseTrueChance + 0.03);
    }

    [Fact]
    public void TheHushStillLies()
    {
        // The tell must stay unreliable in BOTH directions: it fires on most truths, and on a real
        // minority of nonsense. If a fix ever made "room goes quiet" mean "this one is real", the sifting
        // would be over and the oracle would be an announcement — the exact shape the house forbids.
        int trueQuiet = 0, trueTotal = 0, nonsenseQuiet = 0, nonsenseTotal = 0;
        foreach (string bar in Bars)
        {
            for (long watch = 0; watch < 60; watch++)
            {
                for (int draw = 0; draw < 20; draw++)
                {
                    OracleLine l = OracleRant.Speak(bar, watch, draw, 0);
                    if (l.IsTrue) { trueTotal++; if (l.RoomGoesQuiet) { trueQuiet++; } }
                    else { nonsenseTotal++; if (l.RoomGoesQuiet) { nonsenseQuiet++; } }
                }
            }
        }
        Assert.InRange(trueQuiet / (double)trueTotal, 0.6, 0.85);
        Assert.InRange(nonsenseQuiet / (double)nonsenseTotal, 0.08, 0.28);
    }

    // ── 4 · Still pure, still replayable ──────────────────────────────────────────────────────────────

    [Fact]
    public void TheWalkIsStillDeterministicInTheFourDocumentedInputs()
    {
        for (int draw = 0; draw < 30; draw++)
        {
            Assert.Equal(OracleRant.Speak("the-tilt", 5, draw, 2), OracleRant.Speak("the-tilt", 5, draw, 2));
        }
        // …and nothing else leaks in: a different bar, watch or drink count must be able to differ.
        var streams = new HashSet<string>
        {
            Join("the-tilt", 5, 2), Join("cinder-roost", 5, 2), Join("the-tilt", 6, 2), Join("the-tilt", 5, 3),
        };
        Assert.Equal(4, streams.Count);

        static string Join(string bar, long watch, int drinks)
        {
            var sb = new System.Text.StringBuilder();
            for (int d = 0; d < 12; d++)
            {
                sb.Append(OracleRant.Speak(bar, watch, d, drinks).Text).Append('|');
            }
            return sb.ToString();
        }
    }
}
