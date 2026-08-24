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

    // ===== #989 — THE PLAN GRAMMAR: A SHIP LEAVES A BERTH ONCE =====

    /// <summary>
    /// THE GOOD SHAPE, ACCEPTED. The owner's own sentence — cast off, clear the harbour, then the burns —
    /// is well-formed, and so are the two states a departure passes through on its way out: the plan of a
    /// free-flying ship with no departure in it at all, and the minute between the clamp letting go (the ⚓
    /// row retired, and so no longer live) and the clearance firing.
    /// </summary>
    [Fact]
    public void THE_GOOD_SHAPE_IsAccepted()
    {
        Assert.Equal(CastOffRule.PlanShapeFault.None, CastOffRule.CheckShape(
            [PlanStepKind.Undock, PlanStepKind.ClearHarbour, PlanStepKind.Burn], clamped: true));

        Assert.Equal(CastOffRule.PlanShapeFault.None, CastOffRule.CheckShape(
            [PlanStepKind.Burn, PlanStepKind.Burn], clamped: false));

        // Mid-departure: the clamp has let go, the out-thrust has not fired yet. She is FREE with a live
        // clearance row — the feature working, and it must never read as a malformed plan.
        Assert.Equal(CastOffRule.PlanShapeFault.None, CastOffRule.CheckShape(
            [PlanStepKind.ClearHarbour, PlanStepKind.Burn], clamped: false));

        // An empty plan is a plan with nothing wrong with it.
        Assert.Equal(CastOffRule.PlanShapeFault.None, CastOffRule.CheckShape([], clamped: true));
    }

    /// <summary>
    /// TWO CAST-OFFS IN A ROW ARE NOT A PLAN. Owner, off the second #989 screenshot: <i>"2 cast-off in
    /// sequence sounds kind of silly — we should have some logic check."</i> This is that check: a second
    /// departure of either row is refused, wherever in the list it stands.
    /// </summary>
    [Fact]
    public void A_SECOND_CAST_OFF_IsRefused()
    {
        Assert.Equal(CastOffRule.PlanShapeFault.SecondCastOff, CastOffRule.CheckShape(
            [PlanStepKind.Undock, PlanStepKind.ClearHarbour, PlanStepKind.Undock, PlanStepKind.ClearHarbour],
            clamped: true));

        Assert.Equal(CastOffRule.PlanShapeFault.SecondClearance, CastOffRule.CheckShape(
            [PlanStepKind.Undock, PlanStepKind.ClearHarbour, PlanStepKind.ClearHarbour], clamped: true));
    }

    /// <summary>NOTHING GOES AHEAD OF THE CLAMP. A burn plotted before the cast off is a burn the clamp
    /// eats — the plan says one thing and the ship does another, which is this repo's third named bug
    /// class. And the clearance belongs to the departure it was laid with, not three rows down.</summary>
    [Fact]
    public void THE_PAIR_MustBeTheFirstStepsInThePlan()
    {
        Assert.Equal(CastOffRule.PlanShapeFault.CastOffNotFirst, CastOffRule.CheckShape(
            [PlanStepKind.Burn, PlanStepKind.Undock, PlanStepKind.ClearHarbour], clamped: true));

        Assert.Equal(CastOffRule.PlanShapeFault.ClearanceOutOfPlace, CastOffRule.CheckShape(
            [PlanStepKind.Undock, PlanStepKind.Burn, PlanStepKind.ClearHarbour], clamped: true));
    }

    /// <summary>A CAST OFF NEEDS A CLAMP. A ship already under way cannot let go of a berth she is not
    /// tied to — a row like that is a step no executor will ever run.</summary>
    [Fact]
    public void A_CAST_OFF_InThePlanOfAFreeShip_IsRefused()
    {
        Assert.Equal(CastOffRule.PlanShapeFault.CastOffWhileFree, CastOffRule.CheckShape(
            [PlanStepKind.Undock, PlanStepKind.ClearHarbour], clamped: false));

        Assert.False(CastOffRule.ShapeIsWellFormed([PlanStepKind.Undock], clamped: false));
        Assert.True(CastOffRule.ShapeIsWellFormed([PlanStepKind.Undock], clamped: true));
    }

    /// <summary>EVERY FAULT HAS ITS ONE SENTENCE. A plan flipped invalid for a reason nobody can read is
    /// no better than one flipped silently — so the words are total over the enum, and a value that is not
    /// a named fault is a cast integer and gets no plausible sentence.</summary>
    [Fact]
    public void EVERY_PLAN_SHAPE_FAULT_HasItsWords()
    {
        foreach (CastOffRule.PlanShapeFault fault in Enum.GetValues<CastOffRule.PlanShapeFault>())
        {
            string said = CastOffRule.ShapeComplaint(fault);
            Assert.False(string.IsNullOrWhiteSpace(said), $"{fault} has no words");
        }

        Assert.Contains("only leave this berth once", CastOffRule.ShapeComplaint(CastOffRule.PlanShapeFault.SecondCastOff));
        Assert.Contains("before the clamp lets go", CastOffRule.ShapeComplaint(CastOffRule.PlanShapeFault.CastOffNotFirst));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CastOffRule.ShapeComplaint((CastOffRule.PlanShapeFault)99));
    }

    /// <summary>#989 — THE TWO BANNER STATES ARE TWO SENTENCES. A ship still tied up is WAITING; only a
    /// ship whose epoch has come is CASTING OFF. One sentence trying to be both is what read "casting off
    /// from The Red Eye in 0 h" while the ship sat at her berth for another day and a half.</summary>
    [Fact]
    public void THE_WAITING_LINE_AndTheCastingOffLine_AreDifferentSentences()
    {
        string waiting = CastOffRule.WaitingAtTheBerth("The Red Eye", "in 1 d 9 h");
        Assert.Contains("YOU HAVE THE SHIP", waiting);
        Assert.Contains("docked at The Red Eye", waiting);
        Assert.Contains("the plan casts off in 1 d 9 h", waiting);
        Assert.DoesNotContain("casting off", waiting);

        string now = CastOffRule.CastingOffNow("The Red Eye");
        Assert.Contains("AUTOPILOT HAS THE SHIP", now);
        Assert.Contains("casting off", now);
        Assert.NotEqual(waiting, now);
    }
}
