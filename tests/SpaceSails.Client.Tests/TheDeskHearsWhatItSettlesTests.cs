using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1304 · <b>THE DESK HEARS WHAT IT SETTLES.</b> Found by the headless QA of #1302: the geocache sale's two
/// told lines — <i>"Coordinates sent. Nobody's name on either end."</i> and the 💳 escrow line — were written to
/// the page's pulse slot, and the pulse slot was drawn only in the Nav readout and the deck toast. The captain
/// pressing the button at Comms → 🕸 Dark web market saw his purse change and nothing else. #711's parcel
/// payment rides the same slot at the same desk open, so it went unseen the same way.
///
/// <para>Each law boots the SHIPPING page on a <see cref="DeskBench"/>, opens the desk through the comms tree
/// node a player clicks (the one door the payments hang off), presses the rows the desk drew, and then reads
/// the text INSIDE the dark-web desk's own subtree — never the page at large, where the Nav readout could be
/// the one saying it. Each asserts the premise first (the pulse really holds the line), so a green run is a
/// line the desk drew and not a line nobody wrote.</para>
///
/// <para><b>RED</b> for all three by dropping <c>PulseLine="@_pulse.Message"</c> from the desk in
/// <c>DeskPanels/CommsDesk.razor</c>: <i>the pulse holds the line and the desk does not carry it</i>.</para>
/// </summary>
[Collection(SpaceSails.Core.Tests.StopRegisterCollection.Name)]
public sealed class TheDeskHearsWhatItSettlesTests
{
    private const string Berth = "/map?dock=selene-gate";

    /// <summary>The sale is confirmed at the desk ⇒ the desk says <see cref="GeocacheSale.SentLine"/>.</summary>
    [Fact]
    public async Task TheSaleIsConfirmedInTheDeskItWasPressedAt()
    {
        DeskBench bench = await DeskBench.BootAsync(Berth + "&geocache=1");
        await OpenTheDarkWebDesk(bench);

        await Press(bench, GeocacheSale.Glyph);
        DeskBench.Painted quote = await bench.RenderAsync();
        DeskBench.Painted.Node sell = TheDesk(quote).Descendants()
            .First(n => n.Element == "button" && n.Name.EndsWith(" cr", StringComparison.Ordinal)
                        && n.Name.StartsWith('+'));
        await bench.PressAsync(sell.Handlers["onclick"]);

        Assert.Contains(GeocacheSale.SentLine, bench.Pulse, StringComparison.Ordinal);
        string desk = TheDesk(await bench.RenderAsync()).Spoken;
        Assert.Contains(GeocacheSale.SentLine, desk, StringComparison.Ordinal);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>The buyer has lifted it ⇒ opening the desk pays, and the desk says the 💳 line.</summary>
    [Fact]
    public async Task TheEscrowIsToldInTheDeskThatPaysIt()
    {
        DeskBench bench = await DeskBench.BootAsync(Berth + "&geocache=lifted");
        int before = (int)bench.Peek("_credits")!;
        string desk = TheDesk(await OpenTheDarkWebDesk(bench)).Spoken;

        Assert.True((int)bench.Peek("_credits")! > before, "the desk opened and paid nothing — no escrow to tell.");
        Assert.Contains("💳 ", bench.Pulse, StringComparison.Ordinal);
        Assert.Contains("💳 The hole was where you said.", desk, StringComparison.Ordinal);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    /// <summary>#711 · A parcel delivered and due ⇒ opening the desk pays, and the desk says
    /// <see cref="ParcelDrop.PaymentLine"/>. The chest is buried through the page's own ledger, on the ground
    /// Core names for the parcel, far enough back that its payment has come.</summary>
    [Fact]
    public async Task TheParcelPaymentIsToldInTheDeskThatPaysIt()
    {
        DeskBench bench = await DeskBench.BootAsync(Berth);
        await bench.RenderAsync();

        var ground = (IReadOnlyList<string>)bench.Call("TheLandableGround")!;
        Satchel.Item parcel = UnlistedParcel.FromTheDesk("selene-gate", 3);
        ParcelDrop.Destination where = ParcelDrop.For(parcel, ground)
            ?? throw new InvalidOperationException("no ground a parcel may name from this berth.");
        double now = (double)bench.Peek("SimTime")!;
        double buried = now - ((ParcelDrop.MostWatchesBeforeItLands + 1) * PatronRota.WatchSeconds);
        var caches = (CacheLedger)bench.Peek("_caches")!;
        caches.Bury(where.BodyId, 0, [], buried, "YOU", playerOwned: true,
            siteIndex: where.SiteIndex, buried: true, deposit: [parcel]);

        int before = (int)bench.Peek("_credits")!;
        string desk = TheDesk(await OpenTheDarkWebDesk(bench)).Spoken;

        Assert.True((int)bench.Peek("_credits")! > before, "the desk opened and paid nothing — no parcel to tell.");
        Assert.Contains(ParcelDrop.PaymentLine, bench.Pulse, StringComparison.Ordinal);
        Assert.Contains("💳 " + ParcelDrop.PaymentLine, desk, StringComparison.Ordinal);
        Assert.Empty(bench.EscapedPastTheGate);
    }

    // ── the player's road ────────────────────────────────────────────────────────────────────────────

    private static async Task<DeskBench.Painted> OpenTheDarkWebDesk(DeskBench bench)
    {
        await bench.SwitchAsync(ShipDesk.Comms);
        DeskBench.Painted comms = await bench.RenderAsync();
        DeskBench.Painted.Node node = comms.Root.Descendants()
            .First(n => n.HasClass("comms-node") && n.Spoken.Contains("Dark web market", StringComparison.Ordinal));
        await bench.PressAsync(node.Handlers["onclick"]);
        return await bench.RenderAsync();
    }

    private static async Task Press(DeskBench bench, string name)
    {
        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node button = TheDesk(painted).Descendants()
            .First(n => n.Element == "button" && n.Name == name);
        await bench.PressAsync(button.Handlers["onclick"]);
    }

    /// <summary>The dark-web desk's own subtree, and only it.</summary>
    private static DeskBench.Painted.Node TheDesk(DeskBench.Painted painted) =>
        painted.Root.Descendants().Single(n => n.HasClass("dark-web-card") && !n.Hidden);
}
