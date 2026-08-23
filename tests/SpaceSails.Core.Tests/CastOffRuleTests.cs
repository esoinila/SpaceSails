using System;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #955 NAV-1 · <b>THE HARBOUR SIZES THE CAST-OFF, AND EVERY STEP KIND HAS WORDS.</b>
///
/// <para>The browser-side half of this lane (TheCastOffIsAStepTests) flies the owner's sentence from a berth
/// at Selene Gate all the way to a clamp at The Rusty Roadstead. This is the part that needs no browser: that
/// the clearance's size is a CONSEQUENCE of <see cref="DockRule"/> rather than a number somebody typed, and
/// that no <see cref="PlanStepKind"/> can be added without the words the captain reads it by.</para>
/// </summary>
public sealed class CastOffRuleTests
{
    /// <summary>Both of the clearance's numbers are the harbour's own, read off the one place the envelope
    /// lives. If someone re-tunes <see cref="DockRule"/>, the cast-off follows it — that is the point.</summary>
    [Fact]
    public void TheClearanceIsMeasuredOffTheClampsOwnEnvelope()
    {
        Assert.Equal(DockRule.EnvelopeMeters * CastOffRule.MarginFactor, CastOffRule.ClearRangeMeters, 6);
        Assert.Equal(DockRule.MatchSpeed * CastOffRule.DepartureShareOfMatchSpeed, CastOffRule.OutboundSpeedMps, 6);

        // The margin is real: standing exactly at the clamp's reach is NOT clear.
        Assert.False(CastOffRule.Cleared(DockRule.EnvelopeMeters));
        Assert.True(CastOffRule.Cleared(CastOffRule.ClearRangeMeters));

        // …and she leaves no faster than the clamp would have accepted her coming in.
        Assert.True(CastOffRule.OutboundSpeedMps <= DockRule.MatchSpeed);
    }

    /// <summary>The berth's own shove is real speed and is not paid for twice — and a shove that already did
    /// the whole job leaves nothing to buy.</summary>
    [Fact]
    public void TheShoveIsNotPaidForTwice()
    {
        const double shove = 300;   // the client's UndockPushMps
        Assert.Equal(CastOffRule.OutboundSpeedMps - shove, CastOffRule.DeltaVMps(shove), 6);
        Assert.Equal(0, CastOffRule.DeltaVMps(CastOffRule.OutboundSpeedMps * 2), 6);
        Assert.Equal(0, CastOffRule.Pulses(30_000, CastOffRule.OutboundSpeedMps * 2));

        // A negative "already" cannot buy the captain a cheaper departure.
        Assert.Equal(CastOffRule.OutboundSpeedMps, CastOffRule.DeltaVMps(-5000), 6);
    }

    /// <summary>Priced with the one kernel every other burn in the game is priced with, and the node's
    /// per-pulse strength is that same kernel's — so the burn the ship flies delivers the Δv the row quoted
    /// rather than ten times it (the Percent registers in this codebase differ by exactly that factor).</summary>
    [Fact]
    public void ThePriceIsTheGamesOwnPulseKernel()
    {
        const double heliocentricSpeed = 29_800;   // a Sol berth, near enough
        const double shove = 300;

        Assert.Equal(
            OrbitRule.PulsesFor(CastOffRule.DeltaVMps(shove), heliocentricSpeed),
            CastOffRule.Pulses(heliocentricSpeed, shove));
        Assert.Equal(OrbitRule.DeltaVPerPulseFraction * 100.0, CastOffRule.PulsePercent, 9);

        // The quoted pulses, spent at the quoted per-pulse strength, really do close the gap.
        int pulses = CastOffRule.Pulses(heliocentricSpeed, shove);
        double delivered = pulses * (CastOffRule.PulsePercent / 100.0) * heliocentricSpeed;
        Assert.True(delivered >= CastOffRule.DeltaVMps(shove),
            $"{pulses} p at {CastOffRule.PulsePercent}% of {heliocentricSpeed} m/s delivers {delivered:F0} m/s, "
            + $"short of the {CastOffRule.DeltaVMps(shove):F0} m/s the row promised.");
    }

    /// <summary>How long the harbour takes to fall behind, from where she is — zero once it already has.</summary>
    [Fact]
    public void TheClearanceSaysHowLongItTakes()
    {
        Assert.Equal(0, CastOffRule.SecondsToClear(CastOffRule.ClearRangeMeters + 1), 6);
        Assert.Equal(
            CastOffRule.ClearRangeMeters / CastOffRule.OutboundSpeedMps,
            CastOffRule.SecondsToClear(0), 6);
        Assert.True(CastOffRule.SecondsToClear(0) > CastOffRule.SecondsToClear(CastOffRule.ClearRangeMeters / 2));
    }

    /// <summary>
    /// NO STEP KIND WITHOUT ITS WORDS. Walks the enum — a new kind added without a label or a glance line
    /// fails here rather than reaching a captain as a blank row. (Its EXECUTOR is proved on the other side of
    /// the wall, in TheCastOffIsAStepTests.EVERY_STEP_KIND_HasWordsAndAnExecutor, because only the page knows
    /// what flies what.)
    /// </summary>
    [Fact]
    public void EveryStepKindHasALabelAndAGlanceLine()
    {
        foreach (PlanStepKind kind in Enum.GetValues<PlanStepKind>())
        {
            string label = CastOffRule.StepLabel(kind, "Selene Gate", 2);
            string glance = CastOffRule.GlanceLine(kind, "Selene Gate", 2, "in 1 h");
            Assert.False(string.IsNullOrWhiteSpace(label), $"{kind} has no label");
            Assert.False(string.IsNullOrWhiteSpace(glance), $"{kind} has no glance line");
            Assert.Contains("in 1 h", glance);
        }

        // And a value that is not a named kind is a cast integer, not a step — it must not be given a
        // plausible label (the no-silent-default law this file exists to hold).
        Assert.Throws<ArgumentOutOfRangeException>(() => CastOffRule.StepLabel((PlanStepKind)99, "X", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CastOffRule.GlanceLine((PlanStepKind)99, "X", 1, "now"));
    }

    /// <summary>The refusal a clamped arm hears is said in the ⚓ register the nav lock already speaks in, and
    /// it names the one press that fixes it — never a shrug.</summary>
    [Fact]
    public void TheRefusalNamesThePressThatFixesIt()
    {
        Assert.Contains("Cast off", CastOffRule.ArmNeedsCastOff);
        Assert.Contains("Cast off", CastOffRule.ComposeButton);
        Assert.Contains("Selene Gate", CastOffRule.CastingOffNow("Selene Gate"));
        Assert.Contains("casting off", CastOffRule.CastingOffNow("Selene Gate"));
    }

    /// <summary>The clearance row explains itself in the harbour's own numbers, both sides of the line.</summary>
    [Fact]
    public void TheClearanceExplainsItselfInNumbers()
    {
        string notYet = CastOffRule.ClearanceWhy(0);
        Assert.Contains(ArrivalStepRule.FormatDistance(CastOffRule.ClearRangeMeters), notYet);
        Assert.Contains(ArrivalStepRule.FormatDistance(DockRule.EnvelopeMeters), notYet);

        string done = CastOffRule.ClearanceWhy(CastOffRule.ClearRangeMeters * 2);
        Assert.Contains("already clear", done);
    }
}
