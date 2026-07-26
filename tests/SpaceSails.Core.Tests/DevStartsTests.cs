namespace SpaceSails.Core.Tests;

/// <summary>
/// #439 · The dev start sites — the quick-start catalogue the front door renders as buttons (owner,
/// 2026-07-26: "These special places to start should be shown in the UI and marked as dev start sites").
/// These pin the SHAPE of the list, so a row that would render blank, collide with another, or point at a
/// URL the boot-time cheat parser cannot read is caught here rather than in the browser.
/// </summary>
public class DevStartsTests
{
    [Fact]
    public void EveryEntry_IsFullyDressed()
    {
        Assert.NotEmpty(DevStarts.All);
        foreach (DevStarts.Entry e in DevStarts.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Icon), "a dev start with no glyph");
            Assert.False(string.IsNullOrWhiteSpace(e.Label), "a dev start with no label");
            Assert.False(string.IsNullOrWhiteSpace(e.Blurb), $"'{e.Label}' says nothing about what it drops you into");
            Assert.False(string.IsNullOrWhiteSpace(e.Url), $"'{e.Label}' goes nowhere");
        }
    }

    [Fact]
    public void EveryUrl_IsAMapBootUrl_WithAQuery()
    {
        // These are read ONCE, while the world is built — so each must be a /map URL carrying the cheat as
        // a query string (the client navigates with a full reload for exactly this reason).
        foreach (DevStarts.Entry e in DevStarts.All)
        {
            Assert.StartsWith("/map?", e.Url, System.StringComparison.Ordinal);
            string query = e.Url["/map?".Length..];
            Assert.NotEmpty(query);
            foreach (string pair in query.Split('&'))
            {
                Assert.Contains('=', pair);                              // every cheat is key=value
                Assert.False(pair.StartsWith('='), $"'{e.Url}' has a nameless cheat");
                Assert.False(pair.EndsWith('='), $"'{e.Url}' has a valueless cheat");
            }
        }
    }

    [Fact]
    public void TheListHasNoDuplicates()
    {
        // Two buttons that go to the same place (or wear the same name) is a catalogue bug, not a choice.
        Assert.Equal(DevStarts.All.Count, DevStarts.All.Select(e => e.Url).Distinct().Count());
        Assert.Equal(DevStarts.All.Count, DevStarts.All.Select(e => e.Label).Distinct().Count());
    }

    [Fact]
    public void TheSectionNamesItselfAsAServiceDoor()
    {
        // The banner is what keeps a smoke-test entrance from reading as a real voyage.
        Assert.Contains("Dev", DevStarts.SectionTitle, System.StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(DevStarts.SectionBlurb));
        Assert.Contains("not", DevStarts.SectionBlurb, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheGroundsAndTheBarBothHaveADoor()
    {
        // The two things the owner walks most: a surface to set down on, and a bar to stand in. If either
        // ever drops out of the catalogue, the list has lost its point.
        Assert.Contains(DevStarts.All, e => e.Url.Contains("site=", System.StringComparison.Ordinal));
        Assert.Contains(DevStarts.All, e => e.Url.Contains("dock=", System.StringComparison.Ordinal));
    }
}
