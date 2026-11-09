using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>U02-F24 (#1416): every post prices a cargo class at its own deterministic factor.</summary>
public sealed class PostSpreadTests
{
    private static readonly string[] Posts =
    [
        "earth", "satellite-factory", "selene-gate", "mercury", "mercury-compute", "venus", "cinder-roost", "mars",
        "the-space-bar", "jupiter", "red-eye", "saturn", "enceladus", "ringside-exchange", "uranus", "the-tilt",
        "neptune", "the-deep",
    ];

    private static readonly string[] Classes = ["He3", "Compute cores", "Alloys", "Machinery", "Ice", "Deuterium slush"];

    [Fact]
    public void EveryFactorLiesInTheBandAndParPostsReturnExactlyOne()
    {
        foreach (string post in Posts)
        {
            foreach (string cls in Classes)
            {
                double f = PostSpread.Factor(post, cls);
                Assert.InRange(f, PostSpread.MinFactor, PostSpread.MaxFactor);
            }
        }

        foreach (string par in new[] { "selene-gate", "the-space-bar", "ringside-exchange" })
        {
            Assert.All(Classes, c => Assert.Equal(1.00, PostSpread.Factor(par, c)));
        }

        Assert.Equal(0.85, PostSpread.Factor("earth", "Machinery"));
        Assert.Equal(1.25, PostSpread.Factor("the-tilt", "Machinery"));
        Assert.Equal(0.80, PostSpread.Factor("mars", "Ice"));
        Assert.Equal(1.00, PostSpread.Factor("nowhere", "He3"));
        Assert.Equal(1.00, PostSpread.Factor(null, "He3"));
        Assert.Equal(1.00, PostSpread.Factor("derelict-roadster", "He3"));
    }

    [Fact]
    public void ParPostsSayNothingAndOthersTellTheirDeviationInTheTrueMinus()
    {
        Assert.Null(PostSpread.Tail("selene-gate", "He3"));
        Assert.Null(PostSpread.Tail("nowhere", "He3"));
        Assert.Equal("\u221215% here \u2014 Earth is short of nothing.", PostSpread.Tail("earth", "Ice"));
        Assert.Equal("+15% here \u2014 the works builds with it.", PostSpread.Tail("satellite-factory", "Alloys"));
        Assert.Equal("+25% here \u2014 the end of the line.", PostSpread.Tail("the-deep", "Machinery"));
        Assert.Equal("+5% here \u2014 the long haul.", PostSpread.Tail("jupiter", "Ice"));
    }

    [Fact]
    public void BuyAndSellScaleByTheFactorAndTheFeesStayTheSame()
    {
        foreach (CommerceRule.TradeMode mode in new[] { CommerceRule.TradeMode.Shuttle, CommerceRule.TradeMode.DroneMatch })
        {
            int unit = PostSpread.UnitValueCr("earth", "Machinery"); // 250 x 0.85
            Assert.Equal(212, unit);
            int fee = CommerceRule.TransferFeeCr(mode, 10);
            Assert.Equal(10 * unit + fee, CommerceRule.BuyCostCr(mode, 10, unit));
            Assert.Equal(10 * unit - fee, CommerceRule.SellPayoutCr(mode, 10, 10 * unit));
            Assert.True(CommerceRule.SellPayoutCr(mode, 10, 10 * unit) < CommerceRule.SellPayoutCr(mode, 10, 2500));
        }
    }

    [Fact]
    public void TheThreeNeighboursNoLongerPayTheSameAndAMarsIceRunToCinderRoostProfits()
    {
        // The finding: Selene Gate, Highport Satellite Works and Earth Depot all paid exactly the same for a hold.
        var hold = new System.Collections.Generic.Dictionary<string, int> { ["Alloys"] = 20, ["Machinery"] = 10 };
        int face = 20 * 300 + 10 * 250;
        int[] pays = new[] { "selene-gate", "satellite-factory", "earth" }
            .Select(p => CommerceRule.SellPayoutCr(CommerceRule.TradeMode.Shuttle, 30, PostSpread.HoldValueCr(p, hold, face)))
            .ToArray();
        Assert.Equal(3, pays.Distinct().Count());

        // Mars Ice (0.80) -> Cinder Roost Ice (1.15): a full hold nets a profit after both ferry fees.
        const int holdUnits = 20; // a hold; drone-tier ferry (a shuttle's 25 cr/u outweighs Ice's 35 cr spread)
        int buy = CommerceRule.BuyCostCr(CommerceRule.TradeMode.DroneMatch, holdUnits, PostSpread.UnitValueCr("mars", "Ice"));
        var ice = new System.Collections.Generic.Dictionary<string, int> { ["Ice"] = holdUnits };
        int sell = CommerceRule.SellPayoutCr(CommerceRule.TradeMode.DroneMatch, holdUnits,
            PostSpread.HoldValueCr("cinder-roost", ice, holdUnits * CargoMarket.UnitValue("Ice")));
        Assert.True(sell > buy, $"sold {sell} <= bought {buy}");
    }
}
