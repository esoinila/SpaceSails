using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #4–#5 SundayMorningWind — the talking drinks menu. Covers the per-bar menu (shared staples + the
/// bar's own house special), seeded-deterministic favourites (with the authored anchors that make a
/// bar special somebody's favourite by construction), the choice-as-tell channel, and the small honest
/// "their usual" edge once a favourite is known — plus that a learned favourite survives the Vault.
/// </summary>
public class DrinkMenuTests
{
    // ── The menu: shared staples the owner named, plus each bar's own special ──

    [Fact]
    public void EveryBarMenu_PoursSpaceGinAndSpaceBeer_PlusItsOwnHouseSpecial()
    {
        foreach (Barkeep keep in Barkeeps.AllBarkeeps)
        {
            IReadOnlyList<Drink> menu = DrinkMenu.For(keep);

            Assert.Contains(menu, d => d.Id == DrinkMenu.SpaceGin.Id);
            Assert.Contains(menu, d => d.Id == DrinkMenu.SpaceBeer.Id);

            // The bar's own special, verbatim from the keep — same name and flavour, categorised local.
            Drink special = menu.Single(d => d.Category == DrinkCategory.Specialty);
            Assert.Equal(keep.DrinkName, special.Name);
            Assert.Equal(keep.DrinkFlavor, special.Flavor);
        }
    }

    [Fact]
    public void Menu_HasMoreThanOneDrinkType_TheOwnersWholePoint()
    {
        Barkeep keep = Barkeeps.For("the-space-bar")!;
        IReadOnlyList<Drink> menu = DrinkMenu.For(keep);
        Assert.True(menu.Count >= 3); // gin, beer, (rocket fuel,) + the special — more than one type
        Assert.True(menu.Select(d => d.Category).Distinct().Count() >= 2);
    }

    [Fact]
    public void EveryDrink_HasAColourfulFlavourLine()
    {
        foreach (Drink d in DrinkMenu.Catalog)
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Name));
            Assert.True(d.Flavor.Length > 40, $"{d.Name} needs real Larry-style colour");
        }
    }

    [Fact]
    public void Catalog_HoldsEveryBarsSpecial_Deduped_AndTheStaples()
    {
        IReadOnlyList<Drink> catalog = DrinkMenu.Catalog;
        Assert.Equal(catalog.Select(d => d.Id).Distinct().Count(), catalog.Count); // no dupes
        Assert.Contains(catalog, d => d.Id == DrinkMenu.SpaceGin.Id);
        foreach (Barkeep keep in Barkeeps.AllBarkeeps)
        {
            Assert.Contains(catalog, d => d.Id == DrinkMenu.SpecialtyOf(keep).Id);
        }
    }

    [Fact]
    public void ById_RoundTripsEveryCatalogDrink_AndIsNullForUnknown()
    {
        foreach (Drink d in DrinkMenu.Catalog)
        {
            Assert.Equal(d.Id, DrinkMenu.ById(d.Id)!.Value.Id);
        }
        Assert.Null(DrinkMenu.ById("no-such-pour"));
        Assert.Null(DrinkMenu.ById(null));
    }

    // ── Favourites: seeded-deterministic, with authored anchors for flavour ──

    [Fact]
    public void FavoriteFor_IsStableAcrossCalls_ForTheSameContact()
    {
        foreach (string id in new[] { "MADAM COIL", "THE FIXER", "GILT-EYE", "ONE-EYE SILAS", "some-random-patron" })
        {
            Assert.Equal(DrinkFavorites.FavoriteFor(id).Id, DrinkFavorites.FavoriteFor(id).Id);
        }
    }

    [Fact]
    public void FavoriteFor_IsCaseInsensitiveOnTheContactId()
    {
        Assert.Equal(DrinkFavorites.FavoriteFor("madam coil").Id, DrinkFavorites.FavoriteFor("MADAM COIL").Id);
    }

    [Fact]
    public void OneEyeSilas_FavoursTheRoadsteadSpecial_MakingAHouseSpecialSomeonesFavouriteByConstruction()
    {
        // The gruff fence at the Roadstead bar drinks the local iron whiskey — an authored anchor that is
        // exactly "the barkeep's special is somebody's favourite by construction".
        Barkeep roadstead = Barkeeps.For("the-space-bar")!;
        Assert.Equal(DrinkMenu.SpecialtyOf(roadstead).Id, DrinkFavorites.FavoriteFor("ONE-EYE SILAS").Id);
    }

    [Fact]
    public void GiltEye_FavoursTheEarthshine_TheDrinkThatRemembersEverything()
    {
        Barkeep selene = Barkeeps.For("selene-gate")!;
        Assert.Equal(DrinkMenu.SpecialtyOf(selene).Id, DrinkFavorites.FavoriteFor("GILT-EYE").Id);
    }

    [Theory]
    [InlineData("MADAM COIL", "space-gin")]
    [InlineData("THE FIXER", "rocket-fuel")]
    [InlineData("THE MAGPIE", "space-beer")]
    public void KnownCast_HaveTheirAnchoredFavourite(string contactId, string expectedDrinkId) =>
        Assert.Equal(expectedDrinkId, DrinkFavorites.FavoriteFor(contactId).Id);

    [Fact]
    public void UnknownPatron_StillGetsAFavourite_FromTheCatalog()
    {
        Drink fav = DrinkFavorites.FavoriteFor("a-face-with-no-anchor");
        Assert.Contains(DrinkMenu.Catalog, d => d.Id == fav.Id);
    }

    // ── The choice IS the tell ──

    [Fact]
    public void ChoosesDrink_TakesTheFavourite_WhenThisBarPoursIt()
    {
        Barkeep roadstead = Barkeeps.For("the-space-bar")!;
        IReadOnlyList<Drink> menu = DrinkMenu.For(roadstead);
        // Silas favours the Roadstead special, which IS on this menu — he reaches for it.
        Assert.Equal(DrinkFavorites.FavoriteFor("ONE-EYE SILAS").Id, DrinkChoice.ChoosesDrink("ONE-EYE SILAS", menu).Id);
    }

    [Fact]
    public void ChoosesDrink_FallsToSameCategory_WhenTheFavouriteIsntPoured()
    {
        // Gilt-Eye's favourite is the Earthshine (a Specialty) — NOT poured at the Roadstead. He reaches
        // for another Specialty there (the local house special), never a random gin.
        Barkeep roadstead = Barkeeps.For("the-space-bar")!;
        IReadOnlyList<Drink> menu = DrinkMenu.For(roadstead);
        Drink chosen = DrinkChoice.ChoosesDrink("GILT-EYE", menu);
        Assert.Equal(DrinkCategory.Specialty, chosen.Category);
    }

    [Fact]
    public void ChoosesDrink_IsDeterministic()
    {
        Barkeep keep = Barkeeps.For("the-tilt")!;
        IReadOnlyList<Drink> menu = DrinkMenu.For(keep);
        Assert.Equal(DrinkChoice.ChoosesDrink("wanderer", menu).Id, DrinkChoice.ChoosesDrink("wanderer", menu).Id);
    }

    [Fact]
    public void ChannelFor_MapsCategoryToTheTellChannel()
    {
        Assert.Equal(TellChannel.Business, DrinkTell.ChannelFor(DrinkMenu.SpaceGin));
        Assert.Equal(TellChannel.Business, DrinkTell.ChannelFor(DrinkMenu.RocketFuel));
        Assert.Equal(TellChannel.SmallTalk, DrinkTell.ChannelFor(DrinkMenu.SpaceBeer));
        Barkeep keep = Barkeeps.For("red-eye")!;
        Assert.Equal(TellChannel.LocalRumor, DrinkTell.ChannelFor(DrinkMenu.SpecialtyOf(keep)));
    }

    [Fact]
    public void LeadFor_SpeaksInCharacter_NamingTheDrink()
    {
        string lead = DrinkTell.LeadFor(DrinkMenu.SpaceBeer, "The Magpie");
        Assert.Contains("The Magpie", lead);
        Assert.Contains(DrinkMenu.SpaceBeer.Name, lead);
    }

    // ── Knowing the favourite matters: the +1 "their usual" edge ──

    [Fact]
    public void OfferingTheirUsual_AddsAPlusOneEdge_ToTheAcceptRoll()
    {
        ulong seed = DiceRule.Seed("drink-offer:MADAM COIL", 12345);
        DrinkOfferResult plain = ContactDrink.OfferDrink(seed, currentGoodwill: 0, holdingSecret: false);
        DrinkOfferResult usual = ContactDrink.OfferDrink(seed, currentGoodwill: 0, holdingSecret: false, offeringFavorite: true);

        Assert.Equal(plain.Pips, usual.Pips);           // same dice
        Assert.Equal(plain.Total + 1, usual.Total);     // their usual nudges it up by one
        Assert.Contains(usual.Modifiers, m => m.Label == "their usual" && m.Value == +1);
        Assert.DoesNotContain(plain.Modifiers, m => m.Label == "their usual");
    }

    [Fact]
    public void OfferingTheirUsual_AddsAPlusOneEdge_ToTheSharedDrinkRoll()
    {
        ulong seed = DiceRule.Seed("drink:THE FIXER", 999);
        DrinkParley plain = ContactDrink.Roll(seed, currentGoodwill: 0, holdingSecret: false);
        DrinkParley usual = ContactDrink.Roll(seed, currentGoodwill: 0, holdingSecret: false, offeringFavorite: true);

        Assert.Equal(plain.Pips, usual.Pips);
        Assert.Equal(plain.Total + 1, usual.Total);
        Assert.Contains(usual.Modifiers, m => m.Label == "their usual" && m.Value == +1);
    }

    [Fact]
    public void TheEdgeIsSmallAndHonest_NeverMoreThanPlusOne()
    {
        // Never OP (§5.0): the whole "their usual" bonus is a single point, on top of the existing warmth.
        ulong seed = DiceRule.Seed("drink-offer:ONE-EYE SILAS", 7);
        DrinkOfferResult usual = ContactDrink.OfferDrink(seed, ContactDrink.WarmThreshold, holdingSecret: false, offeringFavorite: true);
        Assert.Equal(1, usual.Modifiers.Where(m => m.Label == "their usual").Sum(m => m.Value));
    }

    // ── Learning a favourite is progress, and it persists ──

    [Fact]
    public void WithKnownFavorite_RecordsIt_AndIsIdempotent()
    {
        ContactHistory h = ContactHistory.New("gilt-eye", "Gilt-Eye");
        Assert.False(h.FavoriteKnown);

        h = h.WithKnownFavorite("space-gin");
        Assert.True(h.FavoriteKnown);
        Assert.Equal("space-gin", h.KnownFavorite);
        Assert.True(h.HasHistory); // knowing what they drink is history worth reading

        // Learning the same favourite again changes nothing.
        Assert.Equal(h, h.WithKnownFavorite("space-gin"));
        Assert.Equal(h, h.WithKnownFavorite(""));
    }

    [Fact]
    public void RecordKnownFavorite_OnTheLedger_CreatesTheContactOnFirstLearn()
    {
        var ledger = new ContactLedger();
        ContactHistory after = ledger.RecordKnownFavorite("madam-coil", "Madam Coil", "space-gin");
        Assert.Equal("space-gin", after.KnownFavorite);
        Assert.Equal("space-gin", ledger.For("madam-coil").KnownFavorite);
    }

    [Fact]
    public void KnownFavorite_SurvivesTheVaultRoundTrip()
    {
        var ledger = new ContactLedger();
        ledger.AddGoodwill("one-eye-silas", "One-Eye Silas", 3);
        ledger.RecordKnownFavorite("one-eye-silas", "One-Eye Silas", "special:the-space-bar");

        ContactsSection section = VaultMapper.ToSection(ledger);
        var restored = new ContactLedger();
        VaultMapper.Apply(section, restored);

        ContactHistory back = restored.For("one-eye-silas");
        Assert.Equal("special:the-space-bar", back.KnownFavorite);
        Assert.True(back.FavoriteKnown);
    }

    [Fact]
    public void PreExistingHistory_ReadsWithNoKnownFavorite_ByDefault()
    {
        Assert.False(default(ContactHistory).FavoriteKnown);
        Assert.Equal(string.Empty, ContactHistory.New("x", "X").KnownFavorite);
    }
}
