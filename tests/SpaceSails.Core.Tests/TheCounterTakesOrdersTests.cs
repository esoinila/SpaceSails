using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #756 · THE COUNTER TAKES ORDERS — the Core half. Owner, live playtest 2026-08-08, standing at the B1
/// cantina hall's counter: <i>"HOW DO I ORDER A DRINK FROM THE BAR?????? I walk to the bar to buy a
/// drink... not possible... WHY?"</i> and <i>"you don't have drink or food… what kind of bar is that."</i>
///
/// <para>The purchase math is NOT re-tested here — <c>BarkeepTests</c> and <c>DrinkMenuTests</c> already own
/// it, and that is the point of the fix: the counter is the same machine. What these pin is the seam and
/// the content, which are the two things that were new.</para>
/// </summary>
public sealed class TheCounterTakesOrdersTests
{
    /// <summary>A spread wide enough to be measured rather than sampled, matching the hall tests' own.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static Barkeep Counter =>
        CounterService.For("europa", UndergroundComplex.Comfort.UpperCanteen)
        ?? throw new InvalidOperationException("the branch counter stopped serving");

    [Fact]
    public void TheUpperCanteenServesAndNothingElseInTheBuildingDoes()
    {
        // The seam, at the granularity the [E] press asks it: a fixture either takes orders or it does not,
        // and the answer is a property of WHICH ROOM, never of which caller is asking.
        var wrong = new List<string>();
        int serving = 0;

        foreach (string body in Bodies)
        {
            foreach (UndergroundComplex.Comfort use in Enum.GetValues<UndergroundComplex.Comfort>())
            {
                Barkeep? keep = CounterService.For(body, use);
                bool shouldServe = use == UndergroundComplex.Comfort.UpperCanteen
                    && !UndergroundComplex.IsHeadOffice(body);

                if (shouldServe && keep is null)
                {
                    wrong.Add($"  {body} {use}: the counter refuses to serve anybody.");
                }
                else if (!shouldServe && keep is not null)
                {
                    wrong.Add($"  {body} {use}: this fixture is taking drink orders and should not be.");
                }

                if (keep is not null)
                {
                    serving++;
                }
            }
        }

        // #587's lesson: a sweep that swept nothing would pass this file green forever.
        Assert.True(serving >= 10, $"only {serving} serving counters in the whole sweep — the world went empty.");
        Assert.True(wrong.Count == 0, $"the counter serves in the wrong rooms:{Environment.NewLine}{string.Join(Environment.NewLine, wrong)}");
    }

    [Fact]
    public void TheCardCarriesFoodAndDrinkAndEveryRowPricesItself()
    {
        // Owner, in the same breath as the bug: "you don't have drink or FOOD". A counter that only poured
        // would have answered half the report and passed a test that only counted rows.
        IReadOnlyList<Drink> card = DrinkMenu.For(Counter);

        Assert.True(card.Count >= 5, $"the card is only {card.Count} rows long.");
        Assert.True(card.Any(d => d.Category == DrinkCategory.Food), "nothing on the card is FOOD.");
        Assert.True(card.Any(d => d.Category != DrinkCategory.Food), "nothing on the card is a DRINK.");

        foreach (Drink d in card)
        {
            Assert.True(d.Price > 0, $"“{d.Name}” has no price of its own, so it would be sold at the house rate.");
            Assert.Equal(d.Price, d.PriceAt(Counter.DrinkPrice));
            Assert.False(string.IsNullOrWhiteSpace(d.Flavor), $"“{d.Name}” has no line under it.");
        }

        // The prices actually differ, which is the whole reason Drink grew a Price. A card where every row
        // cost the same would let PriceAt be wrong in either direction and stay green.
        Assert.True(card.Select(d => d.Price).Distinct().Count() >= 3,
            "every row on the card costs the same — this guard could not tell PriceAt from the flat rate.");
    }

    [Fact]
    public void AVenueWithItsOwnCardDoesNotAlsoPourTheSpaceportStaples()
    {
        // The menu seam, both ways. The counter's card REPLACES the staples (a canteen 150 m under a rock
        // does not stock Space Gin), and every haven bar is untouched by the change.
        IReadOnlyList<Drink> counter = DrinkMenu.For(Counter);
        Assert.DoesNotContain(counter, d => d.Id == DrinkMenu.SpaceGin.Id);
        Assert.Equal(CounterService.Card.Count, counter.Count);

        foreach (Barkeep keep in Barkeeps.AllBarkeeps)
        {
            IReadOnlyList<Drink> menu = DrinkMenu.For(keep);
            Assert.Equal(DrinkMenu.Staples.Count + 1, menu.Count);
            Assert.Contains(menu, d => d.Id == DrinkMenu.SpaceGin.Id);
            Assert.Equal(DrinkMenu.SpecialtyOf(keep), menu[^1]);
            Assert.Null(keep.House);
        }
    }

    [Fact]
    public void NobodyIsBehindItAndItSaysSoInItsOwnVoice()
    {
        // #618 canon: skeleton staff. The counter does its own serving, which is worse — so the card must
        // not offer the two verbs that need a person, and the greeting must not be a barkeep's line.
        Barkeep counter = Counter;
        Assert.True(counter.SelfService);
        Assert.Empty(counter.Rumors);
        Assert.NotNull(counter.Welcome);
        Assert.Equal(counter.Welcome, counter.Greeting);
        Assert.DoesNotContain("What'll it be", counter.Greeting, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("art/b1-bar-desk.jpg", counter.DeskArtUrl);

        // …and the default is unchanged for everybody who does have somebody behind the bar.
        foreach (Barkeep keep in Barkeeps.AllBarkeeps)
        {
            Assert.False(keep.SelfService);
            Assert.Null(keep.DeskArtUrl);
            Assert.Contains(keep.DrinkName, keep.Greeting, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheMenuMayJokeAboutTheDeepAndMayNeverBeRightAboutIt()
    {
        // Owner's ruling on the menu's voice, final: "the drinks and food should make fun of the deep" —
        // AND the law that came with it, that no item may state a TRUE fact about the lower bands. Comedy as
        // camouflage, never as leak (§13.8). The words below are the ones the building actually uses about
        // itself; a menu that learned any of them would be the joke doing the opposite of its job.
        string[] neverOnAMenu =
        [
            "ARCHIVE", "MORTUARY", "DESTRUCTION QUEUE", "REEVER", "OLD ONE", "KAAMOS", "restore", "backup",
        ];

        foreach (Drink d in CounterService.Card)
        {
            string text = $"{d.Name} {d.Flavor}";
            foreach (string leak in neverOnAMenu)
            {
                Assert.False(
                    text.Contains(leak, StringComparison.OrdinalIgnoreCase),
                    $"“{d.Name}” says “{leak}” — the menu is allowed to joke about the deep and never to be right about it.");
            }
        }

        // And it does joke: the card is ABOUT down, or the register is not the one the owner ruled for.
        int deep = CounterService.Card.Count(d =>
            $"{d.Name} {d.Flavor}".Contains("bottom", StringComparison.OrdinalIgnoreCase)
            || $"{d.Name} {d.Flavor}".Contains("down", StringComparison.OrdinalIgnoreCase)
            || $"{d.Name} {d.Flavor}".Contains("below", StringComparison.OrdinalIgnoreCase)
            || $"{d.Name} {d.Flavor}".Contains("far down", StringComparison.OrdinalIgnoreCase));
        Assert.True(deep >= 4, $"only {deep} rows on the card joke about what is below.");
    }

    [Fact]
    public void TheHallPublishesTheArtItsFloorWears()
    {
        // #756 part one, at its source. The renderer is not allowed to choose a picture for a room it does
        // not own, so the choosing happens here — and every hall a captain can walk into either wears one or
        // says out loud that it does not.
        var wrong = new List<string>();
        int painted = 0;

        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in
                UndergroundComplex.Build(body, level, SurfaceLayout.DefaultField).Amenities)
            {
                if (a.Hall is not { } hall)
                {
                    continue;
                }

                string? expected = UndergroundComplex.HallArtFor(body, a.Use);
                if (hall.ArtUrl != expected)
                {
                    wrong.Add($"  {body} B{-level} {a.Use}: the hall wears “{hall.ArtUrl}” and the building says “{expected}”.");
                }

                if (a.Use == UndergroundComplex.Comfort.UpperCanteen && !UndergroundComplex.IsHeadOffice(body))
                {
                    if (hall.ArtUrl != UndergroundComplex.CantinaHallArtUrl)
                    {
                        wrong.Add($"  {body} B{-level}: the cantina hall's floor is bare.");
                    }
                    else
                    {
                        painted++;
                    }
                }
            }
        }

        Assert.True(painted >= 5, $"only {painted} painted cantina halls in the whole sweep — the sweep found nothing.");
        Assert.True(wrong.Count == 0, $"halls wearing the wrong art:{Environment.NewLine}{string.Join(Environment.NewLine, wrong)}");

        // The alpha is a legibility decision and belongs to a number somebody can argue with, not to a
        // literal buried in a renderer.
        Assert.InRange(UndergroundComplex.HallArtAlpha, 0.4f, 0.85f);
    }
}
