namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for <see cref="FuelMarket"/> — #157 "How do I fill her up?", the price of a pulse and the
/// arithmetic of a fill (buy caps at the tank, buy caps at the purse, credits decrement exactly, and the
/// inner/outer price seam). Bands and invariants, not brittle exact-purchase scripting.
/// </summary>
public class FuelMarketTests
{
    private const int Tank = 250;

    [Fact]
    public void PricePerPulse_InnerIsCheaper_OuterIsDearer_AtTheBeltSeam()
    {
        // Inside the belt (Earth ~1.5e11 m): the inner price.
        Assert.Equal(FuelMarket.InnerPricePerPulse, FuelMarket.PricePerPulse(1.496e11));
        // Beyond the belt (Saturn ~1.43e12 m): the outer markup.
        Assert.Equal(FuelMarket.OuterPricePerPulse, FuelMarket.PricePerPulse(1.43e12));
        // The markup is a real surcharge, not a rename.
        Assert.True(FuelMarket.OuterPricePerPulse > FuelMarket.InnerPricePerPulse);
        // Exactly at the threshold reads as outer (>= boundary).
        Assert.Equal(FuelMarket.OuterPricePerPulse, FuelMarket.PricePerPulse(FuelMarket.OuterMarkupThresholdMeters));
    }

    [Fact]
    public void FullTankRefill_IsMeaningfulButNotCrushing_AgainstTheStartingPurse()
    {
        // A full-from-empty refill at the inner price: half a 1,500-cr starting purse — felt, never a soft-lock.
        FuelMarket.Quote q = FuelMarket.QuoteFill(0, Tank, FuelMarket.InnerPricePerPulse, credits: 100_000, pulsesWanted: int.MaxValue);
        Assert.Equal(Tank, q.Pulses);
        Assert.Equal(Tank * FuelMarket.InnerPricePerPulse, q.Cost);
        Assert.InRange(q.Cost, 400, 900); // meaningful (~a third to a half of 1,500 cr), not crushing
    }

    [Fact]
    public void Buy_CapsAtCapacity_NeverOverfills()
    {
        // "Fill her up" from a near-full tank buys only the room that remains.
        FuelMarket.Quote q = FuelMarket.QuoteFill(currentPulses: 240, Tank, FuelMarket.InnerPricePerPulse, credits: 100_000, pulsesWanted: int.MaxValue);
        Assert.Equal(10, q.Pulses);
        Assert.Equal(10 * FuelMarket.InnerPricePerPulse, q.Cost);

        // An explicit over-ask is clamped to the room too.
        FuelMarket.Quote partial = FuelMarket.QuoteFill(currentPulses: 245, Tank, FuelMarket.InnerPricePerPulse, credits: 100_000, pulsesWanted: 50);
        Assert.Equal(5, partial.Pulses);

        // Already full: nothing to sell.
        FuelMarket.Quote none = FuelMarket.QuoteFill(currentPulses: Tank, Tank, FuelMarket.InnerPricePerPulse, credits: 100_000, pulsesWanted: int.MaxValue);
        Assert.Equal(0, none.Pulses);
        Assert.Equal(0, none.Cost);
    }

    [Fact]
    public void Buy_CostIsExactlyPulsesTimesPrice_CreditsDecrementExactly()
    {
        const int price = FuelMarket.InnerPricePerPulse;
        FuelMarket.Quote q = FuelMarket.QuoteFill(currentPulses: 100, Tank, price, credits: 100_000, pulsesWanted: 40);
        Assert.Equal(40, q.Pulses);
        Assert.Equal(40 * price, q.Cost);

        // The ledger identity the desk relies on: after paying, the purse drops by exactly the quote.
        int purse = 100_000;
        purse -= q.Cost;
        Assert.Equal(100_000 - 40 * price, purse);
    }

    [Fact]
    public void Buy_WhenShort_TakesWhatYouCanAfford_NeverGoesIntoDebt()
    {
        const int price = FuelMarket.InnerPricePerPulse;
        // Wants a full 250-p fill but only holds enough for 20 pulses' worth of credits.
        FuelMarket.Quote q = FuelMarket.QuoteFill(currentPulses: 0, Tank, price, credits: 20 * price + 2, pulsesWanted: int.MaxValue);
        Assert.Equal(20, q.Pulses);                 // floor(62/3) = 20
        Assert.Equal(20 * price, q.Cost);
        Assert.True(q.Cost <= 20 * price + 2);       // never spends more than on hand

        // Flat broke: buys nothing, at no cost.
        FuelMarket.Quote broke = FuelMarket.QuoteFill(currentPulses: 0, Tank, price, credits: 0, pulsesWanted: int.MaxValue);
        Assert.Equal(0, broke.Pulses);
        Assert.Equal(0, broke.Cost);
    }

    [Fact]
    public void Quote_NeverNegative_ForAnyInputs()
    {
        foreach (int cur in new[] { -5, 0, 100, Tank, 999 })
        foreach (int credits in new[] { -100, 0, 7, 5000 })
        foreach (int want in new[] { -3, 0, 5, int.MaxValue })
        {
            FuelMarket.Quote q = FuelMarket.QuoteFill(cur, Tank, FuelMarket.InnerPricePerPulse, credits, want);
            Assert.True(q.Pulses >= 0, $"pulses negative for cur={cur} cr={credits} want={want}");
            Assert.True(q.Cost >= 0, $"cost negative for cur={cur} cr={credits} want={want}");
            Assert.True(cur + q.Pulses <= Math.Max(cur, Tank), "must never fill past capacity");
        }
    }
}
