namespace SpaceSails.Core.Tests;

/// <summary>#266: the adrift rescue offer's terms format the same on every machine — the pop-up and
/// these tests read the same words, so a stranded captain always sees who comes, what it costs, and
/// what happens to hot cargo before accepting.</summary>
public class RescueOfferTests
{
    [Fact]
    public void TowPromise_NamesTheReactionMassRestored()
    {
        string promise = RescueOffer.TowPromise(500);
        Assert.Contains("tug", promise, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("500 p", promise);
        Assert.Contains("reaction mass", promise, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FeeHeadline_EmptyHold_SaysNothingToLose()
    {
        string headline = RescueOffer.FeeHeadline([]);
        Assert.Contains("empty", headline, StringComparison.OrdinalIgnoreCase);
        // No fee figure when the hold is bare.
        Assert.DoesNotContain("cr", headline);
    }

    [Fact]
    public void FeeHeadline_SumsUnitsAndValue()
    {
        RescueOffer.FeeLine[] lines =
        [
            new("He3", 6, 4200, Hot: false),
            new("Water ice", 4, 800, Hot: false),
        ];

        string headline = RescueOffer.FeeHeadline(lines);
        Assert.Contains("10 units", headline);          // 6 + 4
        Assert.Contains("5,000", headline);             // 4200 + 800, invariant thousands separator
        Assert.Contains("confiscated", headline, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FeeHeadline_NamesHotUnits()
    {
        RescueOffer.FeeLine[] lines =
        [
            new("Contraband", 3, 9000, Hot: true),
            new("Water ice", 2, 400, Hot: false),
        ];

        string headline = RescueOffer.FeeHeadline(lines);
        Assert.Contains("3 of them hot", headline);
    }

    [Fact]
    public void FeeHeadline_SingularUnit_Reads_1_Unit()
    {
        RescueOffer.FeeLine[] lines = [new("Ore", 1, 120, Hot: false)];
        string headline = RescueOffer.FeeHeadline(lines);
        Assert.Contains("1 unit", headline);
        Assert.DoesNotContain("1 units", headline);
    }
}
