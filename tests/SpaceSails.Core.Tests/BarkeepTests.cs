using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// The barkeep behind every haven bar (#247). Covers the pure purchase math (debit + affordability +
/// receipt), that each walkable station has a named keep with its own house special (the Tilt cold and
/// blue, the Ringside with a ring in it), the deterministic rumor rotation, and the ContactLedger
/// goodwill seam that "buy a round for the room" leans on (kin #224) — including its vault round-trip.
/// </summary>
public class BarkeepTests
{
    // The station ids HavenInterior builds bars for; the-space-bar is the Rusty Roadstead's berth.
    private static readonly string[] WalkableStations =
        ["the-space-bar", "cinder-roost", "ringside-exchange", "the-tilt"];

    [Fact]
    public void EveryWalkableStation_HasANamedBarkeepWithAHouseSpecial()
    {
        foreach (string id in WalkableStations)
        {
            Barkeep? keep = Barkeeps.For(id);
            Assert.NotNull(keep);
            Assert.False(string.IsNullOrWhiteSpace(keep!.Name), $"{id} barkeep has no name");
            Assert.False(string.IsNullOrWhiteSpace(keep.DrinkName), $"{id} has no house special");
            Assert.False(string.IsNullOrWhiteSpace(keep.DrinkFlavor), $"{id} special has no flavor text");
            Assert.True(keep.DrinkPrice > 0, $"{id} drink must cost a few credits");
            Assert.True(keep.RoundPrice > keep.DrinkPrice, $"{id} a round must cost more than one glass");
            Assert.NotEmpty(keep.Rumors);
        }
    }

    [Fact]
    public void HouseSpecials_AreDistinctPerBar_WithTheOwnerRequestedFlavors()
    {
        var drinks = WalkableStations.Select(id => Barkeeps.For(id)!.DrinkName).ToList();
        Assert.Equal(drinks.Count, drinks.Distinct().Count()); // no two bars pour the same special

        // The Tilt serves something cold and blue.
        string tilt = Barkeeps.For("the-tilt")!.DrinkName + " " + Barkeeps.For("the-tilt")!.DrinkFlavor;
        Assert.Contains("blue", tilt, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cold", tilt, System.StringComparison.OrdinalIgnoreCase);

        // The Ringside serves something with a ring in it.
        string ringside = Barkeeps.For("ringside-exchange")!.DrinkName + " " + Barkeeps.For("ringside-exchange")!.DrinkFlavor;
        Assert.Contains("ring", ringside, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void For_UnknownBerth_HasNoBarkeep()
    {
        Assert.Null(Barkeeps.For("earth"));
        Assert.Null(Barkeeps.For("not-a-station"));
    }

    // --- The bar DESK, placed on the counter drawn in each backdrop (Lane 2, 2026-07-18 "Evening wind").
    // Owner: "the bar-keep service position … needs to be AT that desk … not the middle of the empty
    // floor … Not on top of a window — and the bar to be on top of the bar in the picture." These pin the
    // per-image geometry so a future art swap or refactor can't quietly drift the keep back into the
    // window band the #247 first pass parked them in.

    // The bar patron stools HavenInterior seats down the LEFT wall, in the same (X, Y-offset-above-HallTopY)
    // space BarDesk maps into — so a clearance check needs no hall-frame math. One-Eye Silas is the near
    // one (a stool right where the counter service point wants to be); The Fixer is the back-corner one.
    private const float SilasX = -9f, SilasYOff = 6f;
    private const float FixerX = -9f, FixerYOff = 16f;
    private const double InteractRadius = 3.0; // mirrors DeckPlan.InteractRadius — E grabs a console within this

    [Fact]
    public void EveryWalkableStation_HasABarDesk()
    {
        foreach (string id in WalkableStations)
        {
            Assert.NotNull(BarDesks.For(id));
        }
        Assert.Equal(WalkableStations.Length, BarDesks.AllDesks.Count);
    }

    [Fact]
    public void For_UnknownBerth_HasNoBarDesk()
    {
        Assert.Null(BarDesks.For("earth"));
        Assert.Null(BarDesks.For("not-a-station"));
    }

    [Fact]
    public void EveryBarDesk_SitsOnTheLeftCounter_OffTheWindow_AndOffTheFront()
    {
        foreach (BarDesk desk in BarDesks.AllDesks)
        {
            // On the LEFT (where every backdrop draws the counter + back-bar shelves): left of room centre.
            Assert.True(desk.ServiceX < 0, $"{desk.BodyId} keep should be on the left counter (x<0), was {desk.ServiceX}");
            Assert.InRange(desk.ServiceU, 0.10f, 0.45f);

            // NOT on the window: the far window wall is BoxDepth (22) above the hall; the service point
            // and the droid one du behind the counter (ServiceYOffset + ~3) must stay well short of it.
            Assert.True(desk.ServiceYOffset <= 18f,
                $"{desk.BodyId} keep too close to the window wall (offset {desk.ServiceYOffset})");

            // NOT the middle of the empty floor at the front: kept back from the hall door.
            Assert.True(desk.ServiceYOffset >= 5f,
                $"{desk.BodyId} keep too far forward, into the open floor (offset {desk.ServiceYOffset})");
        }
    }

    [Fact]
    public void EveryBarkeepConsole_IsReachable_WithoutGrabbingAPatronStool()
    {
        // Standing AT the service point, no earlier-listed regular may be inside E's radius, or E would
        // talk to them instead of pouring a drink (NearestConsoleSpot returns the first console in reach).
        foreach (BarDesk desk in BarDesks.AllDesks)
        {
            double toSilas = Dist(desk.ServiceX, desk.ServiceYOffset, SilasX, SilasYOff);
            double toFixer = Dist(desk.ServiceX, desk.ServiceYOffset, FixerX, FixerYOff);
            Assert.True(toSilas > InteractRadius, $"{desk.BodyId} keep only {toSilas:0.00} from Silas — E would grab the stool");
            Assert.True(toFixer > InteractRadius, $"{desk.BodyId} keep only {toFixer:0.00} from The Fixer");
        }
    }

    [Fact]
    public void BarDeskFractions_ArePinnedPerImage()
    {
        // The exact per-image reads (Roadstead / Cinder / Ringside / Tilt). Change these only alongside
        // the art or a deliberate re-placement — they are the owner-facing "the bar is on the bar" contract.
        AssertDesk("the-space-bar", 0.270f, 0.620f);
        AssertDesk("cinder-roost", 0.260f, 0.600f);
        AssertDesk("ringside-exchange", 0.260f, 0.615f);
        AssertDesk("the-tilt", 0.250f, 0.580f);
    }

    private static void AssertDesk(string id, float u, float v)
    {
        BarDesk desk = BarDesks.For(id)!;
        Assert.Equal(u, desk.ServiceU, 3);
        Assert.Equal(v, desk.ServiceV, 3);
    }

    private static double Dist(float ax, float ay, float bx, float by)
        => System.Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));

    [Fact]
    public void PourHouseSpecial_DebitsExactPrice_WhenAffordable()
    {
        Barkeep keep = Barkeeps.For("the-space-bar")!;
        BarTab tab = keep.PourHouseSpecial(100);

        Assert.True(tab.Poured);
        Assert.Equal(keep.DrinkPrice, tab.Cost);
        Assert.Equal(100 - keep.DrinkPrice, tab.RemainingCredits);
        Assert.Contains(keep.DrinkName, tab.Line);
    }

    [Fact]
    public void PourHouseSpecial_Refuses_WhenPurseIsShort_AndTakesNothing()
    {
        Barkeep keep = Barkeeps.For("the-space-bar")!;
        BarTab tab = keep.PourHouseSpecial(keep.DrinkPrice - 1);

        Assert.False(tab.Poured);
        Assert.Equal(keep.DrinkPrice - 1, tab.RemainingCredits); // purse untouched
    }

    [Fact]
    public void BuyRound_CostsMoreThanADrink_AndRefusesWhenShort()
    {
        Barkeep keep = Barkeeps.For("cinder-roost")!;

        BarTab paid = keep.BuyRound(keep.RoundPrice);
        Assert.True(paid.Poured);
        Assert.Equal(0, paid.RemainingCredits);

        BarTab broke = keep.BuyRound(keep.RoundPrice - 1);
        Assert.False(broke.Poured);
        Assert.Equal(keep.RoundPrice - 1, broke.RemainingCredits);
    }

    [Fact]
    public void RumorAt_IsDeterministic_AndAlwaysInRange()
    {
        Barkeep keep = Barkeeps.For("ringside-exchange")!;

        // Same sim time -> same rumor (no wall clock, no RNG).
        Assert.Equal(keep.RumorAt(12345.0), keep.RumorAt(12345.0));

        // Every hour of a long day lands on a real, non-empty rumor line.
        for (double t = 0; t < 86400 * 3; t += 3600)
        {
            Assert.Contains(keep.RumorAt(t), keep.Rumors);
        }
    }

    [Fact]
    public void AddGoodwill_WarmsAContact_CreatingTheRecordOnFirstRound()
    {
        var ledger = new ContactLedger();

        ContactHistory after = ledger.AddGoodwill("madam-coil", "Madam Coil", 1);
        Assert.Equal(1, after.Goodwill);
        Assert.True(after.HasHistory); // a round stood is history, even with no job done

        ledger.AddGoodwill("madam-coil", "Madam Coil", 2);
        Assert.Equal(3, ledger.For("madam-coil").Goodwill);

        // Goodwill is not coin — it never touches the signed bank balance.
        Assert.Equal(0, ledger.For("madam-coil").CreditBalance);
    }

    [Fact]
    public void Goodwill_SurvivesTheVaultRoundTrip()
    {
        var ledger = new ContactLedger();
        ledger.AddGoodwill("one-eye-silas", "One-Eye Silas", 4);

        ContactsSection section = VaultMapper.ToSection(ledger);
        var restored = new ContactLedger();
        VaultMapper.Apply(section, restored);

        Assert.Equal(4, restored.For("one-eye-silas").Goodwill);
    }
}
