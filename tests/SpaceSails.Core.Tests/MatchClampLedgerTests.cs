namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for #268 pay-at-the-pump — the deferred ⚓ Match &amp; clamp bill (<see cref="MatchClampLedger"/>).
/// The owner's ruling: the autopilot's terminal-match pulses are charged AS/AFTER the flight, never taken
/// whole at the button press. So the redirect impulse fires at the press but its cost only SETTLES on the
/// clamp that lands the leg; an aborted or diverging approach — one that never clamps — keeps none of it.
/// These lock the accrue / settle / abort arithmetic that the client wiring leans on.
/// </summary>
public class MatchClampLedgerTests
{
    [Fact]
    public void Empty_OwesNothing()
    {
        Assert.False(MatchClampLedger.Empty.Owes);
        Assert.Null(MatchClampLedger.Empty.HavenId);
        Assert.Equal(0, MatchClampLedger.Empty.Pulses);
    }

    [Fact]
    public void Accrue_PutsTheBurnOnTheTab_ButNothingIsChargedYet()
    {
        MatchClampLedger tab = MatchClampLedger.Empty.Accrue("tilt", 57);

        Assert.True(tab.Owes);
        Assert.Equal("tilt", tab.HavenId);
        Assert.Equal(57, tab.Pulses); // on the tab — the tank is untouched until a clamp settles it
    }

    [Fact]
    public void Accrue_SameBerthTwice_Stacks_AHotApproachCanNeedMoreThanOneRedirect()
    {
        MatchClampLedger tab = MatchClampLedger.Empty.Accrue("tilt", 40).Accrue("tilt", 25);

        Assert.Equal("tilt", tab.HavenId);
        Assert.Equal(65, tab.Pulses);
    }

    [Fact]
    public void Accrue_DifferentBerth_AbandonsThePriorTabUncharged()
    {
        // Aiming a match at another berth means the first approach is over and never clamped: its tab is
        // dropped, not carried — only the new berth's burn is owed.
        MatchClampLedger tab = MatchClampLedger.Empty.Accrue("tilt", 57).Accrue("roadstead", 30);

        Assert.Equal("roadstead", tab.HavenId);
        Assert.Equal(30, tab.Pulses);
    }

    [Fact]
    public void Settle_OnTheMatchedBerth_ChargesTheTab_AndEmpties()
    {
        MatchClampLedger tab = MatchClampLedger.Empty.Accrue("tilt", 57);

        (int charge, MatchClampLedger next) = tab.Settle("tilt");

        Assert.Equal(57, charge);            // the leg delivered — pay what it burned, now
        Assert.Equal(MatchClampLedger.Empty, next);
        Assert.False(next.Owes);
    }

    [Fact]
    public void Settle_OnADifferentBerth_ChargesNothing_AndClearsTheAbandonedTab()
    {
        MatchClampLedger tab = MatchClampLedger.Empty.Accrue("tilt", 57);

        (int charge, MatchClampLedger next) = tab.Settle("roadstead");

        Assert.Equal(0, charge);             // never clamped the tilt — take nothing for it
        Assert.Equal(MatchClampLedger.Empty, next);
    }

    [Fact]
    public void Abort_DropsTheTabUncharged_TheDivergingApproachKeepsNoMoney()
    {
        MatchClampLedger tab = MatchClampLedger.Empty.Accrue("tilt", 57);

        MatchClampLedger next = tab.Abort();

        Assert.False(next.Owes);
        Assert.Equal(MatchClampLedger.Empty, next);
    }

    [Fact]
    public void MatchThenAbort_TakesNothing_ButMatchThenSettle_TakesExactlyWhatWasBurned()
    {
        // The #268 invariant in one place: two identical matches, one aborted and one clamped. The aborted
        // leg charges 0; the delivered leg charges exactly the pulses the redirect burned — never more, and
        // never at the press.
        const int matchCost = 57;

        MatchClampLedger aborted = MatchClampLedger.Empty.Accrue("tilt", matchCost).Abort();
        (int abortedCharge, _) = aborted.Settle("tilt");
        Assert.Equal(0, abortedCharge);

        (int deliveredCharge, _) = MatchClampLedger.Empty.Accrue("tilt", matchCost).Settle("tilt");
        Assert.Equal(matchCost, deliveredCharge);
    }

    [Fact]
    public void Accrue_NonPositivePulses_NeverGoesNegative()
    {
        MatchClampLedger tab = MatchClampLedger.Empty.Accrue("tilt", -5);
        Assert.Equal(0, tab.Pulses);
        Assert.Equal("tilt", tab.HavenId);
    }
}
