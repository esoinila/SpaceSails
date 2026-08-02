using System;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// A NERVE YOU CAN BOOT AT (issue #428, the <c>?nerve=N</c> boot cheat).
///
/// <para>The nerve only ever fell by being hunted for minutes, so every sanity beat in the game — the
/// overdraw break, the monolith's lump landing on an already-frayed captain, the archive node's dwell —
/// sat a long walk away from any boot and could not be reached on demand. <c>?nerve=N</c> seeds the gauge,
/// and this file guards the two things about that seed which can quietly be wrong.</para>
///
/// <para><b>1 · The UNIT.</b> The cheat's N is <b>pips</b> — the same ten the corner gauge draws (#480) —
/// not points out of a hundred. The two readings differ by a factor of ten and both parse, so a URL written
/// against one and implemented against the other is silently wrong rather than broken: <c>?nerve=1</c>
/// would mean "one pip left, one hand from the break" under one and "already past the overdraw line,
/// dead on the first touch" under the other. The documented repro URL depends on which it is, so the
/// distinction is pinned here against <see cref="CaptainSuccession.EmptyThreshold"/> rather than left to
/// a comment.</para>
///
/// <para><b>2 · BOTH ENDS CLAMP.</b> A seed off the end of the gauge must land ON the gauge, not off it —
/// the <c>?air=N</c> idiom, which clamps to <c>SuitAir.TankSeconds</c>. Every value the cheat can hand over
/// is exercised through the very call the cheat makes (<see cref="NervePips.FromPips"/>), so a clamp lost
/// in Core fails here.</para>
/// </summary>
public sealed class TheNerveSeedIsMeasuredInPipsTests
{
    // EXACTLY what the boot does with a parsed N — the one call, not a paraphrase of it. This matters
    // more than it looks: the first cut of this file re-applied the clamp here before calling FromPips,
    // and stayed green when the clamp was deleted from the shipped code, because the guard was proving
    // its own arithmetic. The cheat now clamps in one place only (Core's), so breaking that one place
    // turns this file red.
    private static double Seed(int n) => NervePips.FromPips(n);

    // ── 0 · The world can tell the two units apart ────────────────────────────────────────────────────

    [Fact]
    public void PipsAndPointsAreGenuinelyDifferentNumbers()
    {
        // If one pip happened to equal one storage point, every assertion below would pass on either
        // reading and this whole file would be decoration.
        Assert.True(NervePips.PipUnit > 1.0,
            $"a pip is {NervePips.PipUnit} storage units — the unit guards below cannot tell pips from points.");
    }

    // ── 1 · The seed is measured in pips ──────────────────────────────────────────────────────────────

    [Fact]
    public void NIsPipsSoTheGaugeReadsBackTheNumberYouAskedFor()
    {
        // The tester's contract: type N, see N lit segments. Anything else and the cheat is lying to the
        // eye it exists to serve.
        for (int n = NervePips.MinPips; n <= NervePips.MaxPips; n++)
        {
            Assert.Equal(n, NervePips.PipsOf(Seed(n)));
        }
    }

    [Fact]
    public void OnePipIsNotYetOverdrawnButAnEmptyGaugeIs()
    {
        // This is the assertion that pins the UNIT, and it is the one the documented repro URL rests on.
        // `?nerve=1&…&reevers=1` promises the real two-step break — a hand takes the last pip, the NEXT
        // hand breaks the captain — which is only true if N is pips. Read as points, `?nerve=1` would be
        // 1.0 of 100, already under CaptainSuccession.EmptyThreshold, and the very first hand would kill:
        // the cheat would have invented the death it was meant to let you watch.
        Assert.False(CaptainSuccession.OverdrawQualifies(Seed(1)),
            "?nerve=1 boots the captain ALREADY overdrawn — the seed is being read as points, not pips.");
        Assert.True(CaptainSuccession.OverdrawQualifies(Seed(0)),
            "?nerve=0 does not qualify as empty — there is no seed that reaches the overdraw break.");
    }

    [Fact]
    public void TheSeedWalksTheWholeDisplayLadder()
    {
        // A gauge cheat that could only produce two of the five rungs would leave most of the sanity
        // flavour unreachable, which is the exact complaint it was filed against. Every band the ladder
        // has must be seedable from some N.
        var seen = new System.Collections.Generic.HashSet<NerveModel.NerveBand>();
        for (int n = NervePips.MinPips; n <= NervePips.MaxPips; n++)
        {
            seen.Add(NerveModel.BandFor(Seed(n)));
        }
        foreach (NerveModel.NerveBand band in Enum.GetValues<NerveModel.NerveBand>())
        {
            Assert.Contains(band, seen);
        }
    }

    // ── 2 · Both ends clamp, and the seed is always a legal gauge value ───────────────────────────────

    [Fact]
    public void ASeedOffEitherEndOfTheGaugeLandsOnTheGauge()
    {
        // ?air=N's idiom. A negative seed is the floor; an over-full one is steady hands; neither may
        // produce a nerve outside the model's own bounds, because everything downstream (the bands, the
        // overdraw predicate, the vault) assumes a value on the gauge.
        Assert.Equal(NervePips.FromPips(NervePips.MinPips), Seed(-1));
        Assert.Equal(NervePips.FromPips(NervePips.MinPips), Seed(int.MinValue));
        Assert.Equal(NervePips.FromPips(NervePips.MaxPips), Seed(NervePips.MaxPips + 1));
        Assert.Equal(NervePips.FromPips(NervePips.MaxPips), Seed(int.MaxValue));

        Assert.Equal(NerveModel.Min, Seed(int.MinValue));
        Assert.Equal(NerveModel.Max, Seed(int.MaxValue));
    }

    [Fact]
    public void EverySeedIsAWholePipAndNeverLeavesTheGauge()
    {
        // The #480 storage law: the canonical nerve is a double, but it is ALWAYS a whole multiple of
        // PipUnit. A seed that landed between pips would draw a half-lit segment — a bug by that law —
        // and would drift on the next beat.
        foreach (int n in new[] { int.MinValue, -7, -1, 0, 1, 4, 7, 10, 11, 99, int.MaxValue })
        {
            double seeded = Seed(n);
            Assert.InRange(seeded, NerveModel.Min, NerveModel.Max);
            Assert.Equal(seeded, NervePips.Snap(seeded));
        }
    }

    [Fact]
    public void AFullSeedIsExactlyTheGaugeAFreshCaptainCarries()
    {
        // ?nerve=10 must be indistinguishable from not passing the cheat at all — otherwise the "control"
        // run of any sanity test is subtly not the shipped default.
        Assert.Equal(NerveModel.Steady, Seed(NervePips.MaxPips));
    }
}
