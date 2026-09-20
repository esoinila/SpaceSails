using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #247 · <b>EVERY BAR POURS ITS OWN, AND CHALKS A BOARD BESIDE IT.</b>
///
/// <para>Owner's scope, in two breaths. First the glasses: <i>"many different themed drinks to the place
/// and ingredients available… The ingredient list doubles as worldbuilding — every cocktail is a geography
/// lesson."</i> Then the food: <i>"FOOD too — 'the Special is really a wild one usually.' … Eating = the
/// meal-sized shore-leave beat beside the drink (bigger #226 relief, small cr), and ordering the Special is
/// a tiny dice moment — usually delicious, occasionally a story."</i></para>
///
/// <para><b>What these guards are actually for.</b> Fourteen of the strings under test are CANON the owner
/// or Fable wrote, and the cheapest way in the world to lose canon is to improve it — a comma, a dash, a
/// "the" in front of a name that is deliberately bare. So they are pinned byte for byte, and a sweep proves
/// none of them says the one thing this world never says out loud.</para>
///
/// <para><b>And the seams.</b> The house drink must be bought through the SAME seam as a Space Gin (the
/// keep's own rate, via <c>Drink.PriceAt</c>) and the board must be bought through the SAME purse math as
/// everything else on that counter — those two claims are what stop this lane from growing a second till
/// beside the first one, which is how a card comes to quote one price and charge another.</para>
/// </summary>
public class EveryBarPoursItsOwnTests
{
    private static readonly string[] Bars =
        ["the-space-bar", "cinder-roost", "ringside-exchange", "the-tilt", "selene-gate", "red-eye", "the-deep"];

    // ── THE HOUSE DRINKS, VERBATIM ──────────────────────────────────────────────────────────────────────
    //
    // Transcribed from the issue, not read back out of the code: a guard that asked Core for its own strings
    // and compared them with themselves would pass on any rewrite at all, which is this repo's fifth bug
    // class — a world that cannot tell pass from fail. DO NOT REFORMAT. The bytes are the point.
    public static TheoryData<string, string, string> HouseDrinks() => new()
    {
        { "the-space-bar", "DUST DEVIL",
          "Mescal off a Hellas still, a red chilli tincture, salt from the flats. It is served in a heavy "
          + "glass because the light ones leave." },
        { "cinder-roost", "SULPHUR SOUR",
          "Cane spirit, lime grown in cloud condensate, and a yellow bitters the keep will not name. Drink "
          + "it before the colour settles." },
        { "ringside-exchange", "RINGSIDE",
          "Rye over one shard of ring ice, and a coin on the counter you did not order. Everybody leaves "
          + "the coin." },
        { "the-tilt", "THE LIST",
          "Aquavit, a spoon of park brine, in a glass that leans. Out here everything leans; the glass is "
          + "only honest." },
        { "selene-gate", "EARTHRISE",
          "Regolith-filtered vodka, curaçao off the Earth freight, one lump of polar ice that took longer "
          + "to get here than you did." },
        { "red-eye", "THE BLINK",
          "Gin, a hydroponic tomato, cracked black pepper. Finish it before the Spot moves; the Spot does "
          + "not move." },
        { "the-deep", "TRENCH",
          "Dark rum, cold tea, a drop of ink bitters, no ice. The walls are the ice." },
    };

    [Theory]
    [MemberData(nameof(HouseDrinks))]
    public void EachBarsHouseDrinkIsItsCanonNameAndLine(string bodyId, string name, string line)
    {
        Barkeep keep = Barkeeps.For(bodyId)!;
        Assert.Equal(name, keep.DrinkName);
        Assert.Equal(line, keep.DrinkFlavor);
    }

    /// <summary>EXACTLY ONE HOUSE DRINK PER BAR, and seven different ones. Two bars pouring the same thing
    /// would make "the local pour" say nothing about the locality, which is the whole ask.</summary>
    [Fact]
    public void EveryBarHasExactlyOneHouseDrink_AndNoTwoBarsPourTheSame()
    {
        Assert.Equal(Bars.Length, Barkeeps.AllBarkeeps.Count);

        foreach (string id in Bars)
        {
            Barkeep keep = Barkeeps.For(id)!;
            // One on the record…
            Assert.False(string.IsNullOrWhiteSpace(keep.DrinkName));
            Assert.False(string.IsNullOrWhiteSpace(keep.DrinkFlavor));
            // …and exactly one on the card this bar actually hands you.
            Drink special = Assert.Single(DrinkMenu.For(keep), d => d.Category == DrinkCategory.Specialty);
            Assert.Equal(keep.DrinkName, special.Name);
            Assert.Equal(keep.DrinkFlavor, special.Flavor);
        }

        Assert.Equal(Bars.Length, Bars.Select(id => Barkeeps.For(id)!.DrinkName).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>THE SAME PRICE SEAM AS THE STAPLES, and no second one. The house drink names no price of its
    /// own (<c>Price == 0</c>), so <see cref="Drink.PriceAt"/> answers the keep's going rate — the identical
    /// number a Space Gin is charged at, the number the card's own header quotes, and the number the debit
    /// reads. A "small multiple" would have been a second price written down somewhere, and the button's
    /// label, its enabled-ness and the debit would then have had somewhere to disagree.</summary>
    [Fact]
    public void TheHouseDrinkIsBoughtAtTheKeepsOwnRate_TheSameSeamAsASpaceGin()
    {
        foreach (string id in Bars)
        {
            Barkeep keep = Barkeeps.For(id)!;
            Drink special = DrinkMenu.SpecialtyOf(keep);

            Assert.Equal(0, special.Price); // names no price of its own — that IS the shared seam
            Assert.Equal(keep.DrinkPrice, special.PriceAt(keep.DrinkPrice));
            Assert.Equal(DrinkMenu.SpaceGin.PriceAt(keep.DrinkPrice), special.PriceAt(keep.DrinkPrice));

            // And the keep's own one-press purchase charges the very same coin.
            BarTab tab = keep.PourHouseSpecial(1000);
            Assert.True(tab.Poured);
            Assert.Equal(keep.DrinkPrice, tab.Cost);
        }
    }

    /// <summary>ONE POUR, ONE PLATE, ONE TRUTH. The photograph is asked of the same record the name and the
    /// line come off, so the row's picture cannot end up being of a drink this bar stopped pouring.</summary>
    [Fact]
    public void EveryHouseDrinkCarriesItsOwnPlate_OffTheSameRecordAsItsName()
    {
        foreach (string id in Bars)
        {
            Barkeep keep = Barkeeps.For(id)!;
            Assert.False(string.IsNullOrWhiteSpace(keep.DrinkArtUrl), $"{id} pours a drink nobody photographed");
            Assert.StartsWith("art/drink-", keep.DrinkArtUrl!, StringComparison.Ordinal);
            Assert.EndsWith(".jpg", keep.DrinkArtUrl!, StringComparison.Ordinal);
            Assert.Equal(keep.DrinkArtUrl, DrinkMenu.SpecialtyOf(keep).ArtUrl);
        }

        // Seven bars, seven different photographs — a shared plate would make two bars look like one place.
        Assert.Equal(Bars.Length,
                     Bars.Select(id => Barkeeps.For(id)!.DrinkArtUrl).Distinct(StringComparer.Ordinal).Count());

        // …and the one counter that is NOT a spaceport bar keeps its own card and hangs no pour-plate.
        Assert.Null(CounterService.For("luna", UndergroundComplex.Comfort.UpperCanteen)!.DrinkArtUrl);
    }

    // ── THE BOARD ───────────────────────────────────────────────────────────────────────────────────────

    // Transcribed from the issue. DO NOT REFORMAT.
    public static TheoryData<string, string> Specials() => new()
    {
        { "the-space-bar", "SPECIAL." },
        { "cinder-roost", "Roost hen, brined in cloud water. It never saw the ground either." },
        { "ringside-exchange", "Hydroponic eel, Ringside style — caught it ourselves, don't ask where." },
        { "the-tilt", "Everything frozen. The thawing lamp is rented by the minute." },
        { "selene-gate", "Bean stew with the park's greens — grown ten metres from your table, by somebody's count." },
        { "red-eye", "Storm noodles. The broth is whatever the last freighter brought." },
        { "the-deep", "Pressure-cooked anything. It comes up from a depth we do not print." },
    };

    [Theory]
    [MemberData(nameof(Specials))]
    public void EachBarsSpecialIsItsCanonLine(string bodyId, string line)
    {
        TheMenuBoard.Board board = TheMenuBoard.For(bodyId)!;
        Assert.Equal(line, board.BoardLine);
    }

    /// <summary>THE ROADSTEAD WILL NOT ELABORATE. Owner's own joke, and the one bar whose board and whose
    /// plate are different sentences: one word chalked up, four words when it lands, and neither an answer.
    /// Everywhere else the plate reads back exactly what the board promised.</summary>
    [Fact]
    public void TheRoadsteadSaysOneWordAndThenFourMore_AndEveryOtherBoardKeepsItsWord()
    {
        TheMenuBoard.Board roadstead = TheMenuBoard.For("the-space-bar")!;
        Assert.Equal("SPECIAL.", roadstead.BoardLine);
        Assert.Equal("It is the Special.", roadstead.PlateLine);

        foreach (string id in Bars.Where(b => b != "the-space-bar"))
        {
            TheMenuBoard.Board board = TheMenuBoard.For(id)!;
            Assert.Equal(board.BoardLine, board.PlateLine);
        }
    }

    [Fact]
    public void EveryBarHasExactlyOneSpecial_AndNoBerthWithoutAKitchenHasOne()
    {
        Assert.Equal(Bars.Length, TheMenuBoard.AllBoards.Count);
        foreach (string id in Bars)
        {
            Assert.Single(TheMenuBoard.AllBoards, b => b.BodyId == id);
            Assert.NotNull(TheMenuBoard.For(id));
        }

        // The Hive's self-serving counter has no kitchen behind it, and nowhere else does either.
        Assert.Null(TheMenuBoard.For("hive-upper-canteen"));
        Assert.Null(TheMenuBoard.For("earth"));
        Assert.Null(TheMenuBoard.For(null));
        Assert.Null(TheMenuBoard.SpecialOn("hive-upper-canteen", 0));
    }

    /// <summary>THE TWO OUTCOMES, VERBATIM. One is the kitchen's own review of itself; the other is the
    /// story, and it is still a compliment, which is the joke and also the horror.</summary>
    [Fact]
    public void TheTwoOutcomeLinesAreTheCanonOnes()
    {
        Assert.Equal("It is better than it had any right to be.", TheMenuBoard.DeliciousLine);
        Assert.Equal("Something in it moved, and then it was delicious.", TheMenuBoard.StoryLine);
    }

    /// <summary>THE ROTATION, STATED HONESTLY. With one Special per bar the dish does NOT rotate — the
    /// PRICE does, per watch, and so does the outcome pair. So the price must actually move across watches
    /// (a "rotation" that returned the same number every shift would be a rotation in name only) and it must
    /// stay inside the small-coin band the owner asked for.</summary>
    [Fact]
    public void ThePriceIsWhatRotates_DeterministicPerWatch_AndAlwaysSmallCoin()
    {
        foreach (string id in Bars)
        {
            var seen = new HashSet<int>();
            for (long watch = 0; watch < 200; watch++)
            {
                int price = TheMenuBoard.PriceOn(id, watch);
                Assert.InRange(price, TheMenuBoard.PriceFloor, TheMenuBoard.PriceCeiling);
                Assert.Equal(price, TheMenuBoard.PriceOn(id, watch)); // same shift, same number, always
                Assert.Equal(price, TheMenuBoard.SpecialOn(id, watch)!.Value.Price);
                seen.Add(price);
            }

            // The board is re-chalked: over two hundred shifts it must have worn more than one number.
            Assert.True(seen.Count > 1, $"{id}'s board never changed its price — that is not a rotation");
        }

        // …and small coin means small: never dearer than the cheapest house glass in the system.
        int cheapestGlass = Bars.Min(id => Barkeeps.For(id)!.DrinkPrice);
        Assert.True(TheMenuBoard.PriceCeiling <= cheapestGlass,
                    "a plate must not read as the luxury on a card whose cheapest pour is a drink");
    }

    /// <summary>THE DICE MOMENT IS DETERMINISTIC, and it is keyed on all three things it claims to be keyed
    /// on. Same bar, same watch, same captain ⇒ the same plate, every time it is asked. Change the captain
    /// or the watch and it is a fresh roll — which is what lets two captains sit at one counter on one shift
    /// and get different suppers.</summary>
    [Fact]
    public void TheOutcomeIsDeterministicOn_Bar_Watch_AndCaptain()
    {
        foreach (string id in Bars)
        {
            DiceRoll a = TheMenuBoard.RollTheSpecial(id, 12, "ADA LOVELACE");
            DiceRoll b = TheMenuBoard.RollTheSpecial(id, 12, "ADA LOVELACE");
            Assert.Equal(a.Face, b.Face);
            Assert.Equal(TheMenuBoard.OutcomeOf(a), TheMenuBoard.OutcomeOf(b));

            // Case on the licence is not a different captain.
            Assert.Equal(a.Face, TheMenuBoard.RollTheSpecial(id, 12, "ada lovelace").Face);
        }

        // Somebody, somewhere, is having a different watch and a different supper — otherwise the seed is
        // not reading the fields it says it reads.
        Assert.Contains(Enumerable.Range(0, 60),
                        w => TheMenuBoard.RollTheSpecial("the-tilt", w, "ADA LOVELACE").Face
                             != TheMenuBoard.RollTheSpecial("the-tilt", 0, "ADA LOVELACE").Face);
        Assert.Contains(new[] { "GRACE HOPPER", "KATHERINE JOHNSON", "MARY JACKSON", "ANNIE EASLEY" },
                        c => TheMenuBoard.RollTheSpecial("the-tilt", 0, c).Face
                             != TheMenuBoard.RollTheSpecial("the-tilt", 0, "ADA LOVELACE").Face);
    }

    /// <summary>BOTH OUTCOMES ARE REACHABLE AT EVERY BAR, and the usual one is usual. Swept over two hundred
    /// watches: a captain who eats at this counter meets the story, and a captain who eats here often is not
    /// living in a horror film about it. A threshold nudged out of reach would leave the rare line shipped,
    /// authored and unreachable — a beat said into the dark.</summary>
    [Fact]
    public void BothOutcomesAreReachedAtEveryBar_AndTheUsualOneIsUsual()
    {
        const int Watches = 200;
        foreach (string id in Bars)
        {
            int stories = 0;
            for (long watch = 0; watch < Watches; watch++)
            {
                if (TheMenuBoard.IsAStory(TheMenuBoard.RollTheSpecial(id, watch, "ADA LOVELACE")))
                {
                    stories++;
                }
            }

            Assert.True(stories > 0, $"{id}: the story outcome is authored and unreachable");
            Assert.True(stories < Watches, $"{id}: every plate is a story, which makes none of them one");
            Assert.True(stories * 4 < Watches, $"{id}: {stories}/{Watches} is not 'occasionally'");
        }
    }

    /// <summary>The outcome line is the roll's own answer, read one way. The line and the flag may never
    /// disagree about which plate just landed.</summary>
    [Fact]
    public void TheOutcomeLineIsWhateverTheFlagSays()
    {
        for (long watch = 0; watch < 200; watch++)
        {
            DiceRoll roll = TheMenuBoard.RollTheSpecial("red-eye", watch, "ADA LOVELACE");
            Assert.Equal(TheMenuBoard.IsAStory(roll) ? TheMenuBoard.StoryLine : TheMenuBoard.DeliciousLine,
                         TheMenuBoard.OutcomeOf(roll));
        }
    }

    /// <summary>THE BOARD IS NOT THE DRINKS CARD. The Special is bought at the same counter with the same
    /// coin and it is still FOOD: it must never appear on the pour menu, and it must never reach the
    /// catalog a contact's FAVOURITE is drawn from — a known face's usual may not one day come back a bowl
    /// of noodles.</summary>
    [Fact]
    public void TheSpecialIsNeverOnTheDrinksCardAndNeverInTheFavouritesCatalog()
    {
        foreach (string id in Bars)
        {
            Barkeep keep = Barkeeps.For(id)!;
            Drink plate = TheMenuBoard.SpecialOn(id, 7)!.Value;

            Assert.Equal(DrinkCategory.Food, plate.Category);
            Assert.DoesNotContain(DrinkMenu.For(keep), d => d.Id == plate.Id);
            Assert.DoesNotContain(DrinkMenu.Catalog, d => d.Id == plate.Id);
            Assert.Null(DrinkMenu.ById(plate.Id));
            Assert.Null(plate.ArtUrl); // the board is words — the pours took the photographs
        }
    }

    /// <summary>THE PLATE PRICES ITSELF, through the same one call every row on that card prices itself
    /// through. It names its own number (a plate is not a pour, and #756 grew this exact field for a card
    /// whose coffee is 2 cr and whose double is 12), and <see cref="Drink.PriceAt"/> hands that number back
    /// whatever the house glass costs — so the label, the enabled-ness and the debit are one number.</summary>
    [Fact]
    public void ThePlateIsBoughtThroughTheCardsOwnPriceSeam()
    {
        foreach (string id in Bars)
        {
            Barkeep keep = Barkeeps.For(id)!;
            for (long watch = 0; watch < 40; watch++)
            {
                Drink plate = TheMenuBoard.SpecialOn(id, watch)!.Value;
                Assert.Equal(TheMenuBoard.PriceOn(id, watch), plate.PriceAt(keep.DrinkPrice));
                Assert.Equal(plate.Price, plate.PriceAt(keep.DrinkPrice));
                Assert.Equal(plate.Price, plate.PriceAt(999)); // its own price, not the house's
            }
        }
    }

    // ── THE MEAL'S STEP ON THE NERVE ────────────────────────────────────────────────────────────────────

    /// <summary>A MEAL IS A BIGGER EASE THAN A TOT — at EVERY level, which is the whole reason it is flat
    /// rather than curved. A lone tot is weak medicine that weakens as the captain worsens (you cannot drink
    /// your way back from the edge alone); supper is food, and food does not care how the day went.</summary>
    [Fact]
    public void AMealEasesMoreThanATot_AtEveryLevelOfTheGauge()
    {
        for (double nerve = 0.0; nerve <= NerveModel.Max; nerve += 5.0)
        {
            double meal = NerveModel.RestoreAmount(NerveModel.DrinkKind.Meal, nerve, totNumber: 1);
            double tot = NerveModel.RestoreAmount(NerveModel.DrinkKind.GalleyTot, nerve, totNumber: 1);
            Assert.True(meal > tot, $"at nerve {nerve} a plate ({meal}) must beat a tot ({tot})");
            Assert.Equal(NerveModel.MealRestore, meal, 6); // flat — the one bigger step, stated once
        }
    }

    /// <summary>…AND IT IS STILL ONLY SUPPER. The order the model is built on, pinned so a tuning pass
    /// cannot quietly make a bowl of noodles better than a night's sleep.</summary>
    [Fact]
    public void TheMealSitsBetweenATotAndAPill()
    {
        Assert.True(NerveModel.MealRestore > NerveModel.GalleyTotBaseRestore);
        Assert.True(NerveModel.MealRestore < NerveModel.CalmingPillRestore);
        Assert.True(NerveModel.CalmingPillRestore < NerveModel.SharedDrinkRestore);
        Assert.True(NerveModel.SharedDrinkRestore < NerveModel.SleepRestore);
    }

    /// <summary>A PLATE IS NOT A POUR — and it is served exactly the way the med bay's pill is served.
    ///
    /// <para>The spree is the CALLER'S to count, here as everywhere: <c>RestoreAmount</c> prices a
    /// diminishing repeat for anything handed a rising tot number, which is why the pill and the bunk are
    /// dosed at <c>totNumber: 1</c> and why a meal is too. So what Core can state is the SHAPE — a meal is
    /// the pill's kind of relief, flat and level-independent, and never the tot's curved kind — and that a
    /// plate is never told the rum has stopped helping, which is a sentence about drink said to somebody
    /// who is eating. That the counter actually doses it at one, and never through <c>PourRum</c>, is the
    /// client's own guard to make (<c>TheBoardIsNotThePourTests</c>).</para></summary>
    [Fact]
    public void AMealIsServedThePillsWay_AndIsNeverToldTheRumStopped()
    {
        // The pill's SHAPE, not the tot's: flat at every level, where a lone tot sags toward one point.
        foreach (double nerve in new[] { 0.0, 25.0, 50.0, 75.0, 100.0 })
        {
            Assert.Equal(NerveModel.RestoreAmount(NerveModel.DrinkKind.CalmingPill, nerve, 1)
                         / NerveModel.CalmingPillRestore,
                         NerveModel.RestoreAmount(NerveModel.DrinkKind.Meal, nerve, 1)
                         / NerveModel.MealRestore, 6);
        }

        // And its own voice, at any tot count a caller could hand it — including a drunk one.
        foreach (int tot in new[] { 1, 2, 3, 7 })
        {
            string note = NerveModel.SteadyingNote(NerveModel.DrinkKind.Meal, tot, restored: 14.0);
            Assert.DoesNotContain("rum", note, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("drunk", note, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(note));
        }
    }

    // ── THE RESERVED-WORD SWEEP, EXTENDED ───────────────────────────────────────────────────────────────

    /// <summary>NOTHING ON EITHER MENU NAMES THE THING. Fourteen canon lines, seven names and two outcomes
    /// go past the sweep <c>docs/worldbuilding-notes.md</c> §8 keeps: there is one monolith and the word is
    /// reserved, the Old Ones are never named by a card, and a drinks list is precisely the sort of place a
    /// future hand reaches for a bit of spooky colour.
    ///
    /// <para>The park line is the pointed case. <i>"grown ten metres from your table, by somebody's
    /// count"</i> is a COUNT and not an explanation — it says how far, never who grows them, never why a
    /// block has a park at all — so the sweep bars the words an explanation would need.</para></summary>
    [Fact]
    public void NotOneLineOnEitherMenuSpendsAReservedWord()
    {
        string[] reserved =
        [
            "monolith", "reever", "old one", "old ones", "ancient", "alien", "not ours", "not natural",
            "restore", "backup", "kaamos", "minister", "donor", "they were people", "whose", "who made",
        ];

        var lines = new List<string>();
        foreach (string id in Bars)
        {
            Barkeep keep = Barkeeps.For(id)!;
            lines.Add(keep.DrinkName);
            lines.Add(keep.DrinkFlavor);
            TheMenuBoard.Board board = TheMenuBoard.For(id)!;
            lines.Add(board.BoardLine);
            lines.Add(board.PlateLine);
        }

        lines.Add(TheMenuBoard.DeliciousLine);
        lines.Add(TheMenuBoard.StoryLine);
        lines.Add(TheMenuBoard.BoardHead);
        lines.Add(TheMenuBoard.SpecialLabel);

        foreach (string line in lines)
        {
            foreach (string word in reserved)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>The park's greens stay a COUNT. Pinned on its own because it is the one line in the set that
    /// is ABOUT something the game does not explain, and the temptation to finish the sentence is real.</summary>
    [Fact]
    public void TheParksGreensAreACountAndNotAnAccount()
    {
        string line = TheMenuBoard.For("selene-gate")!.BoardLine;
        Assert.Contains("ten metres", line, StringComparison.Ordinal);
        Assert.Contains("by somebody's count", line, StringComparison.Ordinal);
        foreach (string word in new[] { "because", "grown by", "the block", "tended", "in order to" })
        {
            Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
        }
    }
}
