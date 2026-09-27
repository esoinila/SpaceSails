using System;
using System.IO;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #319 slice 2 · THE GEOCACHE SALE, THE PAGE'S HALF — the wiring Core cannot see, in the shape #711's own
/// client bench keeps (<c>ADropForNobodyYouHaveMetTests</c>): the page is a partial class on a component no
/// test can instantiate, so the arithmetic is driven in <c>TheGeocacheSaleTests</c> and what is pinned here is
/// the order the page does things in. Every guard was watched go red on the revert its summary names.
/// </summary>
public sealed class TheGeocacheSaleIsWiredTests
{
    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    private static string Code(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, "//[^\n]*", " ");
    }

    private static string Sale() => Code(Read("Pages", "Map.GeocacheSale.cs"));

    private static string Body(string code, string signature)
    {
        int at = code.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"no {signature} in Map.GeocacheSale.cs");
        int next = code.IndexOf("\n    private ", at + signature.Length, StringComparison.Ordinal);
        return next < 0 ? code[at..] : code[at..next];
    }

    /// <summary>
    /// THE ROW EXISTS ONLY FOR A PRICED KIND AND ONLY ONCE PER CACHE: it is offered off the fence's own price
    /// (which is null for anything the fence does not buy), never for a cache already sold, and never at a
    /// desk shut by a sale the captain killed.
    /// </summary>
    [Fact]
    public void TheRowIsOfferedOnlyForAPricedUnsoldCacheAtAnOpenDesk()
    {
        string row = Body(Sale(), "private (TreasureCache Cache, int Gross)? TheLocationForSale(");
        Assert.Contains("TheFencesPriceFor(cache) is { } gross", row, StringComparison.Ordinal);
        Assert.Contains("!GeocacheSale.WasSold(_roomsTurnedOver, cache.Id)", row, StringComparison.Ordinal);
        Assert.Contains("_roomsTurnedOver.Contains(GeocacheSale.ShutAt(port.Id))", row, StringComparison.Ordinal);

        string price = Body(Sale(), "private int? TheFencesPriceFor(");
        Assert.Contains("GeocacheSale.IsSellable(cache)", price, StringComparison.Ordinal);
        Assert.Contains("CompromisingChip.FencePrice(job.Reward)", price, StringComparison.Ordinal);
    }

    /// <summary>
    /// CREDITS ONLY AFTER THE LIFT, NEVER BEFORE: the desk keeps the buyer's schedule first, and the only
    /// credits this feature moves are paid off an ESCROW OWED tag, which only the lift writes — and the lift is
    /// gated on <c>GeocacheSale.IsLiftedAt</c>.
    /// </summary>
    [Fact]
    public void CreditsMoveOnlyOffWhatTheLiftWrote()
    {
        string code = Sale();
        Assert.Single(Regex.Matches(code, @"_credits \+="));

        string settle = Body(code, "private void TheGeocacheEscrowSettles(");
        int keep = settle.IndexOf("TheBuyerKeepsHisSchedule();", StringComparison.Ordinal);
        int pay = settle.IndexOf("_credits += owed.Net;", StringComparison.Ordinal);
        Assert.True(keep >= 0 && pay > keep, "the desk pays before it has kept the buyer's schedule.");
        Assert.Contains("foreach (GeocacheSale.Owed owed in GeocacheSale.OwedIn(_roomsTurnedOver))", settle,
            StringComparison.Ordinal);

        string lift = Body(code, "private void TheBuyerKeepsHisSchedule(");
        int gate = lift.IndexOf("!GeocacheSale.IsLiftedAt(sale.CacheId, sale.SoldWatch, SimTime)", StringComparison.Ordinal);
        int owes = lift.IndexOf("GeocacheSale.OwedOn(", StringComparison.Ordinal);
        Assert.True(gate >= 0 && owes > gate, "the escrow is owed without asking whether the buyer has lifted it.");
        Assert.Single(Regex.Matches(code, @"GeocacheSale\.OwedOn\("));
    }

    /// <summary>
    /// THE CAPTAIN LIFTS IT FIRST ⇒ NO CREDITS, THE LINE ONCE, THE ROW ABSENT FOR THE VISIT: the dead sale is
    /// settled (so it is never told twice, and no escrow can ever be owed on it), the desk is shut, and the
    /// shut desk opens again at the clamp.
    /// </summary>
    [Fact]
    public void ASaleTheCaptainKilledIsToldOnceAndShutsTheDesk()
    {
        string settle = Body(Sale(), "private void TheGeocacheEscrowSettles(");
        int settled = settle.IndexOf("GeocacheSale.IsSettled(_roomsTurnedOver, sale.CacheId)", StringComparison.Ordinal);
        int dead = settle.IndexOf("GeocacheSale.DeadOn(", StringComparison.Ordinal);
        int shut = settle.IndexOf("GeocacheSale.ShutAt(port.Id)", StringComparison.Ordinal);
        int told = settle.IndexOf("ShowPulseMessage(GeocacheSale.LiftedFirstLine)", StringComparison.Ordinal);
        Assert.True(settled >= 0 && dead > settled && shut > dead && told > shut,
            "the dead sale is not settled, shut and told in that order.");

        string berth = Code(Read("Pages", "Map.Docking.Berth.cs"));
        Assert.Contains("TheDeskIsOpenAgainBehindYou();", berth, StringComparison.Ordinal);
    }

    /// <summary>THE DOORS: the desk-opening setter settles the escrow beside the parcel's payment, and the
    /// surface frame keeps the schedule and reads the hole.</summary>
    [Fact]
    public void TheEscrowAndTheScheduleAreCalledWhereTheyBelong()
    {
        string alerts = Code(Read("Pages", "Map.Alerts.cs"));
        int parcel = alerts.IndexOf("ThePaymentIsThere();", StringComparison.Ordinal);
        int escrow = alerts.IndexOf("TheGeocacheEscrowSettles();", StringComparison.Ordinal);
        Assert.True(parcel >= 0 && escrow > parcel, "the escrow is not settled at the desk's one door.");

        string frame = Code(Read("Pages", "Map.Surface.Frame.cs"));
        Assert.Contains("TheBuyerKeepsHisSchedule();", frame, StringComparison.Ordinal);
        Assert.Contains("TheLiftedHoleIsReadHere();", frame, StringComparison.Ordinal);

        string desk = Read("Pages", "Map", "DeskPanels.razor");
        Assert.Contains("Geocache=\"GeocacheOnOffer()\" OnSellLocation=\"SellTheLocation\"", desk, StringComparison.Ordinal);
        foreach (string file in new[] { "Map.razor", Path.Combine("Map", "FlowColumn.razor") })
        {
            Assert.Contains("GeocacheOnOffer=\"@GeocacheOnOffer\" SellTheLocation=\"@SellTheLocation\"",
                Read("Pages", file), StringComparison.Ordinal);
        }
    }

    /// <summary>The desk draws only Core's words for this row.</summary>
    [Fact]
    public void TheDeskDrawsCoresWords()
    {
        string desk = Read("Pages", "Stations", "DarkWeb.razor");
        Assert.Contains("@GeocacheSale.RowLabel", desk, StringComparison.Ordinal);
        Assert.DoesNotContain(GeocacheSale.RowLabel, Code(Read("Pages", "Map.GeocacheSale.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("SELL THE LOCATION", desk, StringComparison.Ordinal);
    }
}
