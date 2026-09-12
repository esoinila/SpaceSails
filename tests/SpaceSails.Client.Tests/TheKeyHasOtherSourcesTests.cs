using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #535 slice 2 · <b>THE FAVOUR AND THE FENCE, DRIVEN ON THE SHIPPING PAGE.</b>
///
/// <para>Core proves the two laws; this proves the two verbs. Each one is checked twice over: the markup is
/// read as the page composes it (<see cref="MapMarkup"/>) to prove the control is drawn <b>only</b> where its
/// law holds and wired to the handler that keeps it, and then the handler itself is driven on a booted page
/// to prove what it does to the purse, the pocket and the book.</para>
///
/// <para><b>What makes these able to fail.</b> The joint cap is the one worth reading: it is driven end to
/// end — the favour is TAKEN at a berth, and then the fence's own price function is asked at the same port on
/// the same watch and has to come back null. Two sources keeping two registers would sail straight through a
/// guard that only asked each of them about itself.</para>
/// </summary>
public sealed class TheKeyHasOtherSourcesTests
{
    private const string Giver = "madam-coil";
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    // ── THE MARKUP: WHERE THE TWO VERBS ARE DRAWN ───────────────────────────────────────────────────────

    /// <summary>
    /// THE FAVOUR IS ONE MORE VERB ON THE SEAT'S EXISTING CARD, AND IT IS DRAWN ONLY WHERE THE LAW HOLDS.
    ///
    /// <para>In <c>ContactDrinkOffer</c> — the one fragment both doorways to a seated contact render (the
    /// barkeep's counter card and the patron's own table) — so the offer appears wherever the captain meets
    /// that person at a bar, and nowhere a new pop-up would have had to be opened.</para>
    ///
    /// <para>And it is <b>absent</b> rather than greyed: a disabled verb under somebody's name would announce
    /// that this person has something, which is the one thing this object is written against.</para>
    /// </summary>
    [Fact]
    public void TheFavourIsAVerbOnTheSeatsCardGatedByItsOwnLaw()
    {
        string fragment = TheContactsCard();

        Assert.Contains("BlackOpsKey.FavourVerb", fragment, StringComparison.Ordinal);
        Assert.Contains("TheFavourIsOnTheTable(giver)", fragment, StringComparison.Ordinal);
        Assert.Contains("TakeTheFavour(giver)", fragment, StringComparison.Ordinal);

        // The contact's own line is printed on the card the verb sits on — the canon, read off Core and
        // never retyped into markup.
        Assert.Contains("BlackOpsKey.FavourLine", fragment, StringComparison.Ordinal);

        // Drawn where it applies, never shown and denied.
        string block = Between(fragment, "TheFavourIsOnTheTable(giver)", "BlackOpsKey.FavourVerb");
        Assert.DoesNotContain("disabled", block, StringComparison.OrdinalIgnoreCase);

        // …and it waits behind the face gate, like the glass does: nobody hands a key to a man whose face
        // they are still trying to place.
        int gate = fragment.IndexOf("TheyWouldNoticeTheFace(giver)", StringComparison.Ordinal);
        int favour = fragment.IndexOf("TheFavourIsOnTheTable(giver)", StringComparison.Ordinal);
        Assert.True(gate >= 0 && favour > gate,
            "the favour no longer waits on the face gate — it would be offered to somebody who is still "
            + "asking the captain who he is.");
        Assert.Contains("!TheyWouldNoticeTheFace(giver) && TheFavourIsOnTheTable(giver)", fragment,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// THE FENCE'S STOCK IS A ROW, AND IT EXISTS ONLY WHILE IT APPLIES.
    ///
    /// <para>On the dark-web desk beside the chip's row and the inspector's card, in the same shape: a price
    /// that is <c>null</c> when there is nothing to sell, so the row is absent rather than refusing, and the
    /// desk's one existing <c>disabled</c> for a purse that cannot meet a price it is allowed to see.</para>
    ///
    /// <para>The desk composes no prose and no arithmetic of its own — the object's plate, its look-card line
    /// and the fence's line all come off Core, and the price arrives already formatted by Map.</para>
    /// </summary>
    [Fact]
    public void TheFencesKeyIsARowOnTheDeskThatOnlyExistsWhileItApplies()
    {
        string desk = File.ReadAllText(Path.Combine(
            TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Stations", "DarkWeb.razor"));

        Assert.Contains("@if (KeyPrice is { } keyPrice)", desk, StringComparison.Ordinal);
        Assert.Contains("BlackOpsKey.FenceRowLine", desk, StringComparison.Ordinal);
        Assert.Contains("BlackOpsKey.FenceVerb", desk, StringComparison.Ordinal);
        Assert.Contains("BlackOpsKey.LookCardLine", desk, StringComparison.Ordinal);
        Assert.Contains("OnBuyKey.InvokeAsync()", desk, StringComparison.Ordinal);

        string row = Between(desk, "@if (KeyPrice is { } keyPrice)", "Your sellable tracks");
        Assert.Contains("disabled=\"@(Credits < keyPrice)\"", row, StringComparison.Ordinal);

        // No second way of writing a number on a desk that already has two rows quoting credits.
        Assert.Contains("@KeyPriceText", row, StringComparison.Ordinal);
        Assert.DoesNotContain("ToString(", row, StringComparison.Ordinal);

        // …and Map really hands the desk those three, or the row is drawn against nothing.
        string panels = File.ReadAllText(Path.Combine(
            TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map", "DeskPanels.razor"));
        Assert.Contains("KeyPrice=\"TheFencesKeyPrice()\"", panels, StringComparison.Ordinal);
        Assert.Contains("OnBuyKey=\"BuyTheKeyFromTheFence\"", panels, StringComparison.Ordinal);
    }

    // ── THE FAVOUR, DRIVEN ──────────────────────────────────────────────────────────────────────────────

    /// <summary>TAKING IT DEALS EXACTLY ONE KEY, DROPS THE BAND, AND MARKS THE BOOK — on the page, through
    /// the handler the markup wired.</summary>
    [Fact]
    public void TakingTheFavourDealsOneKeyAndCostsTheBand()
    {
        Pages.Map map = ABarWhereSomebodyOwesYouOne(out string port);
        int before = Book(map).For(Giver).Goodwill;

        Assert.True((bool)Invoke(map, "TheFavourIsOnTheTable", Giver)!);

        Invoke(map, "TakeTheFavour", Giver);

        Assert.Equal(1, BlackOpsKey.CountIn(Carried(map)));
        Assert.Equal(before - BlackOpsKey.FavourCostsGoodwill, Book(map).For(Giver).Goodwill);
        Assert.True(Book(map).For(Giver).FavourSpent);
        Assert.False((bool)Invoke(map, "TheFavourIsOnTheTable", Giver)!);
        Assert.Contains(BlackOpsKey.ThePortHasDealtOne(port, BlackOpsKey.FenceWindow(0)), Register(map));
    }

    /// <summary>DECLINING CHANGES NOTHING AND THE OFFER STANDS TO A LATER SIT. Declining is not pressing —
    /// there is no third button — so this reads the whole of the captain's state around an offer that was on
    /// the table and never taken.</summary>
    [Fact]
    public void DecliningTheFavourChangesNothingOnThePage()
    {
        Pages.Map map = ABarWhereSomebodyOwesYouOne(out _);
        int goodwill = Book(map).For(Giver).Goodwill;
        int pocket = Carried(map).Count;
        int register = Register(map).Count;

        Assert.True((bool)Invoke(map, "TheFavourIsOnTheTable", Giver)!);

        // …the captain closes the card. Nothing was pressed, so nothing is called.
        Assert.Equal(goodwill, Book(map).For(Giver).Goodwill);
        Assert.Equal(pocket, Carried(map).Count);
        Assert.Equal(register, Register(map).Count);
        Assert.True((bool)Invoke(map, "TheFavourIsOnTheTable", Giver)!);
    }

    /// <summary>…AND IT IS NOT ON THE TABLE WHEN THE LAW DOES NOT HOLD. Three worlds, each differing from the
    /// world above in exactly one thing: nobody standing in the top band, a book that marks a lie, and a
    /// captain who is not at a berth at all.</summary>
    [Fact]
    public void TheFavourIsNotOnTheTableWhenItsLawDoesNotHold()
    {
        Pages.Map cold = ADeskThatWillDeal(out _);
        Book(cold).AddGoodwill(Giver, Giver, BlackOpsKey.TopGoodwillBand - 1);
        Assert.Equal(BlackOpsKey.TopGoodwillBand - 1, Book(cold).For(Giver).Goodwill);
        Assert.False((bool)Invoke(cold, "TheFavourIsOnTheTable", Giver)!);

        Pages.Map lied = ABarWhereSomebodyOwesYouOne(out _);
        Book(lied).RecordLie(Giver, Giver);
        Assert.False((bool)Invoke(lied, "TheFavourIsOnTheTable", Giver)!);

        Pages.Map adrift = ABarWhereSomebodyOwesYouOne(out _);
        Set(adrift, "_dockedHavenId", null);
        Assert.False((bool)Invoke(adrift, "TheFavourIsOnTheTable", Giver)!);
    }

    // ── THE FENCE, DRIVEN ───────────────────────────────────────────────────────────────────────────────

    /// <summary>THE DESK QUOTES THREE BRIBES OFF THE CAPTAIN'S OWN METER — asked of the page and answered by
    /// calling Core's own price with the page's own heat, so a quote composed here rather than read from
    /// there is red.</summary>
    [Fact]
    public void TheDeskQuotesCoresPriceOffTheCaptainsOwnHeat()
    {
        Pages.Map map = ADeskThatWillDeal(out string port);
        Set(map, "_heat", new HeatState(2, 0));

        long window = BlackOpsKey.FenceWindow(0);
        int expected = BlackOpsKey.FencePrice(2, BlackOpsKey.FenceSeed(port, window));

        Assert.Equal(expected, (int)Invoke(map, "TheFencesKeyPrice")!);
        Assert.Equal(expected.ToString("N0", System.Globalization.CultureInfo.InvariantCulture),
            (string)Invoke(map, "TheFencesKeyPriceText")!);

        // The meter really is being read, or the quote is a constant wearing a function's clothes.
        Set(map, "_heat", new HeatState(3, 0));
        Assert.NotEqual(expected, (int)Invoke(map, "TheFencesKeyPrice")!);
    }

    /// <summary>BUYING IT DEBITS THE CAPTAIN AND PUTS THE SAME OBJECT IN THE POCKET.</summary>
    [Fact]
    public void BuyingTheKeyTakesTheCoinAndHandsOverTheSameObject()
    {
        Pages.Map map = ADeskThatWillDeal(out string port);
        int price = (int)Invoke(map, "TheFencesKeyPrice")!;
        Set(map, "_credits", price + 17);

        Invoke(map, "BuyTheKeyFromTheFence");

        Assert.Equal(17, Get<int>(map, "_credits"));
        Assert.Equal(1, BlackOpsKey.CountIn(Carried(map)));
        Assert.Equal(Satchel.Kind.BlackOpsKey, Carried(map).Single(BlackOpsKey.IsTheKey).Kind);
        Assert.Contains(BlackOpsKey.ThePortHasDealtOne(port, BlackOpsKey.FenceWindow(0)), Register(map));

        // …and the row is gone: this port has dealt its one for the watch.
        Assert.Null(Invoke(map, "TheFencesKeyPrice"));
    }

    /// <summary>CANNOT PAY IS THE DESK'S EXISTING REFUSAL — the row still quotes (a price you cannot meet is
    /// a price you are allowed to see) and the press moves nothing.</summary>
    [Fact]
    public void APurseBelowThePriceBuysNothingAndTheRowStillQuotes()
    {
        Pages.Map map = ADeskThatWillDeal(out _);
        int price = (int)Invoke(map, "TheFencesKeyPrice")!;
        Set(map, "_credits", price - 1);

        Invoke(map, "BuyTheKeyFromTheFence");

        Assert.Equal(price - 1, Get<int>(map, "_credits"));
        Assert.Equal(0, BlackOpsKey.CountIn(Carried(map)));
        Assert.Empty(Register(map));
        Assert.Equal(price, (int)Invoke(map, "TheFencesKeyPrice")!);
    }

    /// <summary>
    /// <b>ONE KEY PER PORT PER WINDOW, COUNTING BOTH SOURCES TOGETHER — driven end to end.</b>
    ///
    /// <para>The favour is TAKEN at the berth, and then the fence's own quote is asked at the same port on
    /// the same watch. It has to be gone. This is the guard two registers cannot pass: strike the favour off
    /// in a book of its own and the desk would still be selling the second key of the afternoon.</para>
    ///
    /// <para>…and the watch after, the fence is stocked again, or the cap would be a ban.</para>
    /// </summary>
    [Fact]
    public void ThePortDealsOneKeyAWatchWhicheverSourceDealtIt()
    {
        Pages.Map map = ABarWhereSomebodyOwesYouOne(out string port);
        Assert.Equal(port, Invoke(map, "TheFencesPort"));
        Assert.NotNull(Invoke(map, "TheFencesKeyPrice"));

        Invoke(map, "TakeTheFavour", Giver);

        Assert.Equal(1, BlackOpsKey.CountIn(Carried(map)));
        Assert.Null(Invoke(map, "TheFencesKeyPrice"));

        // The watch turns and the fence is stocked again — a cap, not a ban.
        Set(map, "SimTime", Core.Interior.PatronRota.WatchSeconds * 1.5);
        Assert.NotNull(Invoke(map, "TheFencesKeyPrice"));
    }

    // ── The bench ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A berth where the dark web will deal, which is also where a bar with contacts in it is. Both
    /// verbs are reachable from here, which is what lets the joint cap be asked at all.</summary>
    private static Pages.Map ADeskThatWillDeal(out string port)
    {
        Pages.Map map = Booted();
        ICelestialEphemeris eph = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        CelestialBody haven = eph.Bodies.First(b => b.IsHaven && HavenInterior.HasInterior(b.Id));

        Set(map, "_docked", true);
        Set(map, "_dockBodyId", haven.Id);
        Set(map, "_dockedHavenId", haven.Id);
        Set(map, "SimTime", 0.0);

        Assert.True((bool)Invoke(map, "DarkWebCanTrade")!, $"the desk will not deal at {haven.Id}.");
        port = haven.Id;
        return map;
    }

    /// <summary>…and somebody at that bar in the top goodwill band who has never been lied to.</summary>
    private static Pages.Map ABarWhereSomebodyOwesYouOne(out string port)
    {
        Pages.Map map = ADeskThatWillDeal(out port);
        Book(map).AddGoodwill(Giver, Giver, BlackOpsKey.TopGoodwillBand + 2);
        Assert.True(BlackOpsKey.FavourIsOnTheTable(Book(map).For(Giver)),
            "the bench did not actually put anybody in the top band, so every favour guard below would be "
            + "green against a verb that is never drawn.");
        return map;
    }

    private static Pages.Map Booted()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        Set(map, "_scenarioName", TestTree.Sol.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_ship", new ShipState(Vector2d.Zero, Vector2d.Zero, 0.0));
        return map;
    }

    /// <summary>The contact card both doorways render, cut out of the page as the page composes it.</summary>
    private static string TheContactsCard()
    {
        string razor = MapMarkup.Read(Path.Combine(
            TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));
        int start = razor.IndexOf("private RenderFragment ContactDrinkOffer(", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the shared drink-offer fragment — the favour's verb "
            + "has nowhere to be and this guard cannot find what it audits.");
        return razor[start..razor.IndexOf("</text>;", start, StringComparison.Ordinal)];
    }

    private static string Between(string source, string from, string to)
    {
        int a = source.IndexOf(from, StringComparison.Ordinal);
        Assert.True(a >= 0, $"`{from}` is not in the markup this guard reads.");
        int b = source.IndexOf(to, a, StringComparison.Ordinal);
        Assert.True(b > a, $"`{to}` does not follow `{from}` in the markup this guard reads.");
        return source[a..b];
    }

    private static ContactLedger Book(Pages.Map map) => Get<ContactLedger>(map, "_contacts");

    private static IReadOnlyList<Satchel.Item> Carried(Pages.Map map) =>
        Get<IReadOnlyList<Satchel.Item>>(map, "_satchel");

    private static HashSet<string> Register(Pages.Map map) => Get<HashSet<string>>(map, "_roomsTurnedOver");

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);
}
