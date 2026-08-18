using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #937 · THE NUMBERS GET THE SAME FRONT DOOR. Owner (2026-08-18, flying the scrub): <i>"we could have the
/// small + / − symbols also on the thrust amount number dialog for iterating the scrub panel. Now writing a
/// new number there I have to switch to another input to see what the effect of numeric change is. So for
/// all those scrub burns the ±5° type iteration buttons would be useful with some comparable
/// increment."</i>
///
/// <para>And the ruling the same sitting: <i>"only the fine / ultra-fine tuning would be the captain
/// entering the numeric value to the field, but the rough estimation with those + − buttons like the
/// angle."</i> The buttons are the front door; the typed field is the exception, for the last decimal.</para>
///
/// <h3>What these guards hold, and what they cannot</h3>
/// <para>The ARITHMETIC of a step — how far one press moves a number, and where it stops — is Core's, and
/// it is what this file flies: the exact step, the clamp at either stop, and the faces that carry the unit.
/// Whether those steps are wired to real buttons on the panel and whether the plan re-solves under the
/// press is the SHAPE of the page, and lives in the client's <c>TheNumbersNudgeTooTests</c>.</para>
///
/// <h3>The green test this could have been</h3>
/// <para>"A nudge returns a number in range" passes for a function that returns the input unchanged. So
/// every law below asserts the step's EXACT size as well as its bounds, and LAW TWO runs an impostor — the
/// same step arithmetic with the clamp taken out — through the identical property and proves in this file
/// that the property goes red on it, so nobody has to take a RED PROOF paragraph on faith.</para>
/// </summary>
public sealed class TheNumbersNudgeTooTests
{
    // The planner's own bounds on a burn's magnitude (Map.Plot.cs: MinNodePulses / MaxNodePulses). The
    // client hands these in; the guards fly the same field the captain actually touches.
    private const int Min = 1;
    private const int Max = 20;

    // ── LAW ONE · a press moves the number by EXACTLY its step ──────────────────────────────────────

    /// <summary>
    /// One press = one step, in the plan's own unit. The fine step is a single pulse — the field's own
    /// resolution — and the coarse step is five, a quarter of the 1..20 range, which is the same bargain
    /// <see cref="NodeFrame.NudgeDegrees"/>'s five degrees strikes against a quarter turn.
    ///
    /// <para>RED PROOF: change <c>NudgePulsesCoarse</c> to 10 (the range's half — two presses from either
    /// stop to the other, the jump the owner did not ask for) and the coarse rows fail naming the
    /// arithmetic; make the sign branch fall through and the down-rows fail.</para>
    /// </summary>
    [Theory]
    [InlineData(8, +1, false, 9)]
    [InlineData(8, -1, false, 7)]
    [InlineData(8, +1, true, 13)]
    [InlineData(8, -1, true, 3)]
    [InlineData(1, +1, false, 2)]
    [InlineData(15, +1, true, 20)]
    public void OnePressMovesTheMagnitudeByExactlyItsStep(int from, int sign, bool coarse, int expected)
    {
        Assert.Equal(expected, NodeFrame.NudgeMagnitude(from, sign, coarse, Min, Max));

        // And the step really is the constant, not a number that happens to agree at these points.
        int step = coarse ? NodeFrame.NudgePulsesCoarse : NodeFrame.NudgePulsesFine;
        Assert.Equal(from + (sign >= 0 ? step : -step), expected);
    }

    /// <summary>The two steps are the ones documented, and the coarse one is a quarter of the field's
    /// range rather than half of it — the sentence in <c>NodeFrame</c>'s docblock, asserted.</summary>
    [Fact]
    public void TheStepsAreOnePulseAndFive_AQuarterOfTheField()
    {
        Assert.Equal(1, NodeFrame.NudgePulsesFine);
        Assert.Equal(5, NodeFrame.NudgePulsesCoarse);
        Assert.Equal(4, (Max - Min + 1) / NodeFrame.NudgePulsesCoarse);   // four presses cross the field
    }

    // ── LAW TWO · the clamps, and an impostor that proves the property can go red ────────────────────

    /// <summary>
    /// A magnitude button can never reach a number the typed field would refuse. Press down from the
    /// bottom of the field and you stay at the bottom; press up from the top and you stay at the top; and
    /// whatever bounds a caller hands in, the answer is never negative — a burn that spends negative
    /// reaction mass is not a burn.
    ///
    /// <para>THE IMPOSTOR, run below: the same step arithmetic with <c>Math.Clamp</c> removed. It is the
    /// exact edit a "simplification" would make, and this file proves the sweep catches it rather than
    /// asserting so in prose.</para>
    /// </summary>
    [Fact]
    public void TheMagnitudeNeverLeavesTheFieldsBounds_AndTheSweepCatchesAnImpostorThatDoes()
    {
        InRangeSweep((p, s, c) => NodeFrame.NudgeMagnitude(p, s, c, Min, Max));

        // RED, in this file: drop the clamp and pressing down from 1 lands on 0 (or −4 on the coarse step).
        Assert.Throws<Xunit.Sdk.InRangeException>(() => InRangeSweep(UnclampedNudge));

        // And the "never below zero" law holds even against a caller who hands in a nonsense floor.
        Assert.Equal(0, NodeFrame.NudgeMagnitude(0, -1, true, min: -50, max: Max));
        Assert.Equal(0, NodeFrame.NudgeMagnitude(3, -1, true, min: -50, max: Max));
    }

    /// <summary>Every magnitude in the field, both signs, both steps — the answer stays inside
    /// <c>[Min, Max]</c>.</summary>
    private static void InRangeSweep(Func<int, int, bool, int> nudge)
    {
        foreach (int pulses in Enumerable.Range(Min, Max - Min + 1))
        {
            foreach (int sign in new[] { -1, +1 })
            {
                foreach (bool coarse in new[] { false, true })
                {
                    Assert.InRange(nudge(pulses, sign, coarse), Min, Max);
                }
            }
        }
    }

    /// <summary>The impostor: the step, applied, with nothing stopping it at either end.</summary>
    private static int UnclampedNudge(int pulses, int sign, bool coarse)
    {
        int step = coarse ? NodeFrame.NudgePulsesCoarse : NodeFrame.NudgePulsesFine;
        return pulses + (sign >= 0 ? step : -step);
    }

    // ── LAW THREE · the epoch steps, and the floor they can never cross ─────────────────────────────

    /// <summary>
    /// An hour and a day, exactly — the scrub slider's own units (it steps in whole hours) and the scale a
    /// transfer is judged on.
    ///
    /// <para>RED PROOF: set <c>NudgeTimeFineSeconds</c> to 60 and the hour rows fail; swap the coarse and
    /// fine branches and every row fails naming the value it got.</para>
    /// </summary>
    [Theory]
    [InlineData(+1, false, 3600.0)]
    [InlineData(-1, false, -3600.0)]
    [InlineData(+1, true, 86400.0)]
    [InlineData(-1, true, -86400.0)]
    public void OnePressMovesTheEpochByExactlyItsStep(int sign, bool coarse, double delta)
    {
        // A floor far in the past, so nothing here is clamping — this row is about the step alone.
        const double start = 40 * 86400.0;
        Assert.Equal(start + delta, NodeFrame.NudgeEpoch(start, sign, coarse, floorSimTime: 0), 6);

        Assert.Equal(3600.0, NodeFrame.NudgeTimeFineSeconds);
        Assert.Equal(86400.0, NodeFrame.NudgeTimeCoarseSeconds);
    }

    /// <summary>
    /// The floor is a floor. A node may be walked back toward it press after press and it lands ON it,
    /// never before it — and pressing again from the floor is a no-op rather than a refusal, because a
    /// control the captain can push into an error is a control that blocks him.
    ///
    /// <para>THE IMPOSTOR, run below: the same step with the <c>Math.Max</c> removed. That is the whole
    /// bug this law exists for — a burn scheduled before the ship reaches it is a burn that never
    /// fires.</para>
    /// </summary>
    [Fact]
    public void TheEpochNeverLandsBeforeItsFloor_AndTheSweepCatchesAnImpostorThatDoes()
    {
        const double floor = 100_000.0;

        FloorSweep((t, s, c) => NodeFrame.NudgeEpoch(t, s, c, floor), floor);

        // RED, in this file: no floor, and eight presses down from just above it walk straight past.
        Assert.Throws<Xunit.Sdk.InRangeException>(() => FloorSweep(UnflooredNudge, floor));

        // Walking down lands exactly ON the floor and stops there.
        double at = floor + 2 * NodeFrame.NudgeTimeFineSeconds;
        at = NodeFrame.NudgeEpoch(at, -1, coarse: false, floor);
        at = NodeFrame.NudgeEpoch(at, -1, coarse: false, floor);
        Assert.Equal(floor, at, 6);
        Assert.Equal(floor, NodeFrame.NudgeEpoch(at, -1, coarse: false, floor), 6);
        Assert.Equal(floor, NodeFrame.NudgeEpoch(at, -1, coarse: true, floor), 6);

        // Up from the floor still moves — the clamp is one-sided.
        Assert.Equal(floor + NodeFrame.NudgeTimeCoarseSeconds,
                     NodeFrame.NudgeEpoch(floor, +1, coarse: true, floor), 6);
    }

    /// <summary>Press down repeatedly from a handful of starts near the floor; the answer never goes
    /// under it.</summary>
    private static void FloorSweep(Func<double, int, bool, double> nudge, double floor)
    {
        foreach (double offset in new[] { 0.0, 900.0, 3600.0, 7200.0, 86400.0, 5 * 86400.0 })
        {
            foreach (bool coarse in new[] { false, true })
            {
                double at = floor + offset;
                for (int press = 0; press < 8; press++)
                {
                    at = nudge(at, -1, coarse);
                    Assert.InRange(at, floor, double.MaxValue);
                }
            }
        }
    }

    /// <summary>The impostor: the step, applied, with no floor under it.</summary>
    private static double UnflooredNudge(double simTime, int sign, bool coarse)
    {
        double step = coarse ? NodeFrame.NudgeTimeCoarseSeconds : NodeFrame.NudgeTimeFineSeconds;
        return simTime + (sign >= 0 ? step : -step);
    }

    // ── LAW FOUR · every face carries its unit, and no face is a bare ± ─────────────────────────────

    /// <summary>
    /// #916 sent the reflex-flying plus-or-minus idiom out of the planner and #838 guards that it stays
    /// out. These eight faces come back into the panel wearing UNITS — <c>+1 p</c>, <c>−5 p</c>,
    /// <c>+1 h</c>, <c>−1 d</c> — so what they spend is on the button and nobody can read one as the
    /// factor control that left.
    ///
    /// <para>RED PROOF: label any of them "±1", or drop the unit and return a bare "+1", and this fails
    /// naming the face — as would #838's own <c>ThePlanner_OffersNoPlusMinusControl</c> if the bare glyph
    /// reached the markup.</para>
    /// </summary>
    [Fact]
    public void EveryStepFaceCarriesItsUnit_AndNoneIsABarePlusMinus()
    {
        Assert.Equal("+1 p", NodeFrame.NudgeMagnitudeLabel(+1, coarse: false));
        Assert.Equal("−1 p", NodeFrame.NudgeMagnitudeLabel(-1, coarse: false));
        Assert.Equal("+5 p", NodeFrame.NudgeMagnitudeLabel(+1, coarse: true));
        Assert.Equal("−5 p", NodeFrame.NudgeMagnitudeLabel(-1, coarse: true));
        Assert.Equal("+1 h", NodeFrame.NudgeEpochLabel(+1, coarse: false));
        Assert.Equal("−1 h", NodeFrame.NudgeEpochLabel(-1, coarse: false));
        Assert.Equal("+1 d", NodeFrame.NudgeEpochLabel(+1, coarse: true));
        Assert.Equal("−1 d", NodeFrame.NudgeEpochLabel(-1, coarse: true));

        foreach (string face in AllFaces())
        {
            Assert.DoesNotContain("±", face);
            Assert.True(face.Length >= 4, $"the face \"{face}\" has lost its unit — a bare sign and a "
                + "number is exactly the reflex-flying idiom #916 sent out of this panel.");
            Assert.True(face.EndsWith(" p", StringComparison.Ordinal)
                        || face.EndsWith(" h", StringComparison.Ordinal)
                        || face.EndsWith(" d", StringComparison.Ordinal),
                $"the face \"{face}\" names no unit.");
        }

        // Eight distinct faces: no two buttons on the panel can read the same.
        Assert.Equal(8, AllFaces().Distinct().Count());
    }

    /// <summary>And the hints say the thing the owner was missing out loud: the course re-solves under
    /// the press, so nobody has to change focus to see what a number did.</summary>
    [Fact]
    public void EveryHintPromisesTheCourseReSolvesUnderThePress()
    {
        foreach (string hint in new[]
                 {
                     NodeFrame.NudgeMagnitudeHint(+1, false), NodeFrame.NudgeMagnitudeHint(-1, false),
                     NodeFrame.NudgeMagnitudeHint(+1, true), NodeFrame.NudgeMagnitudeHint(-1, true),
                     NodeFrame.NudgeEpochHint(+1, false), NodeFrame.NudgeEpochHint(-1, false),
                     NodeFrame.NudgeEpochHint(+1, true), NodeFrame.NudgeEpochHint(-1, true),
                 })
        {
            Assert.Contains("re-solves as you press", hint);
        }

        // The plural reads right at both steps — "1 more pulse", "5 more pulses".
        Assert.Contains("1 more pulse ", NodeFrame.NudgeMagnitudeHint(+1, false));
        Assert.Contains("5 more pulses ", NodeFrame.NudgeMagnitudeHint(+1, true));
        Assert.Contains("1 fewer pulse ", NodeFrame.NudgeMagnitudeHint(-1, false));
    }

    private static IEnumerable<string> AllFaces() =>
        new[]
        {
            NodeFrame.NudgeMagnitudeLabel(+1, false), NodeFrame.NudgeMagnitudeLabel(-1, false),
            NodeFrame.NudgeMagnitudeLabel(+1, true), NodeFrame.NudgeMagnitudeLabel(-1, true),
            NodeFrame.NudgeEpochLabel(+1, false), NodeFrame.NudgeEpochLabel(-1, false),
            NodeFrame.NudgeEpochLabel(+1, true), NodeFrame.NudgeEpochLabel(-1, true),
        };
}
