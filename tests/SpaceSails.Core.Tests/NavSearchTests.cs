namespace SpaceSails.Core.Tests;

/// <summary>
/// #406 — the Nav search box's pure match/rank contract. These pin what "type to find a target" does
/// without a browser: the substring match, the exact/prefix/interior ranking, and the stable-order
/// filter the client feeds its candidate list through.
/// </summary>
public class NavSearchTests
{
    // ---- Matches: trimmed, case-insensitive substring; empty query matches nothing ----

    [Theory]
    [InlineData("titan", "Titan")]
    [InlineData("TIT", "Titan")]
    [InlineData("tan", "Titan")]       // interior
    [InlineData("  ti  ", "Titan")]    // trimmed
    public void Matches_IsCaseInsensitiveTrimmedSubstring(string query, string name)
    {
        Assert.True(NavSearch.Matches(query, name));
    }

    [Theory]
    [InlineData("xyz", "Titan")]
    [InlineData("", "Titan")]
    [InlineData("   ", "Titan")]
    public void Matches_MissAndEmptyAreFalse(string query, string name)
    {
        Assert.False(NavSearch.Matches(query, name));
    }

    [Fact]
    public void EmptyQuery_MatchesNothing_NotEverything()
    {
        // The box shows no dropdown until you type — "search for nothing" is not "list everything".
        Assert.Empty(NavSearch.FilterAndRank("", new[] { "Titan", "Earth", "Rhea" }, s => s));
    }

    // ---- Rank: exact < prefix < interior (earlier interior wins); miss is int.MaxValue ----

    [Fact]
    public void Rank_ExactBeatsPrefixBeatsInterior()
    {
        int exact = NavSearch.Rank("titan", "Titan");
        int prefix = NavSearch.Rank("tit", "Titan");
        int interior = NavSearch.Rank("tan", "Titan");
        Assert.True(exact < prefix, $"exact {exact} should beat prefix {prefix}");
        Assert.True(prefix < interior, $"prefix {prefix} should beat interior {interior}");
    }

    [Fact]
    public void Rank_EarlierInteriorHitWins()
    {
        // "it" is at index 1 in "Titan", index 3 in "Mestitia" — the earlier one ranks better.
        Assert.True(NavSearch.Rank("it", "Titan") < NavSearch.Rank("it", "Mestitia"));
    }

    [Fact]
    public void Rank_MissIsMaxValue()
    {
        Assert.Equal(int.MaxValue, NavSearch.Rank("xyz", "Titan"));
        Assert.Equal(int.MaxValue, NavSearch.Rank("", "Titan"));
    }

    // ---- FilterAndRank: best-match-first, ties keep input order ----

    [Fact]
    public void FilterAndRank_DropsMisses_AndOrdersBestFirst()
    {
        string[] bodies = { "Mestitia", "Titan", "Earth", "Titania" };
        List<string> hits = NavSearch.FilterAndRank("tit", bodies, s => s);

        // "Earth" has no "tit" and is dropped. "Titan"/"Titania" are prefix hits (rank 1, input order
        // holds the tie). "Mestitia" hits "tit" INTERIOR (index 3 → rank 5), so it sorts last — proving
        // prefix beats interior even though it came first in the input.
        Assert.Equal(new[] { "Titan", "Titania", "Mestitia" }, hits);
    }

    [Fact]
    public void FilterAndRank_ExactHitFloatsAbovePrefix()
    {
        string[] bodies = { "Titania", "Titan" };  // input order puts the prefix-only one first
        List<string> hits = NavSearch.FilterAndRank("titan", bodies, s => s);
        // "Titan" is an exact whole-name hit, so it must jump ahead of "Titania" despite input order.
        Assert.Equal(new[] { "Titan", "Titania" }, hits);
    }

    [Fact]
    public void FilterAndRank_StableForEqualRank()
    {
        // Two equally-good (interior, same position) matches keep the caller's intent order — the client
        // relies on this to keep threats-then-contacts-then-bodies as the tiebreak.
        string[] items = { "aXa", "bXb" };
        List<string> hits = NavSearch.FilterAndRank("x", items, s => s);
        Assert.Equal(new[] { "aXa", "bXb" }, hits);
    }
}
