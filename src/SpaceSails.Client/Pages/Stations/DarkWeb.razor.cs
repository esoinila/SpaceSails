using Microsoft.AspNetCore.Components;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

// DarkWeb — the code-behind for DarkWeb.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of DarkWeb.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class DarkWeb
{
    /// <summary>An off-the-books ship the dark web is willing to sell route intel about — a thin
    /// projection of Map.razor's own NPC state (mirrors TrackingPost.TrackingCandidate).</summary>
    public readonly record struct MarketShip(string Id, string Callsign, string CargoClass, int CargoUnits, string RouteLabel);

    /// <summary>One of the player's own tracked contacts, thin enough for the sell table (and
    /// Map.razor's per-contact comms actions) to share a single list.</summary>
    public readonly record struct TrackedShipInfo(
        string Id, string Callsign, string CargoClass, int CargoUnits, double Quality,
        Vector2d Position, Vector2d Velocity, bool PublishesTimetable, string DestinationName);

    [Parameter, EditorRequired] public double SimTime { get; set; }
    [Parameter, EditorRequired] public Vector2d ShipPosition { get; set; }
    [Parameter] public bool Visible { get; set; }
    [Parameter] public bool FullScreen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    [Parameter] public int Credits { get; set; }
    [Parameter] public EventCallback<int> CreditsChanged { get; set; }

    /// <summary>The player's bought intel — OWNED BY MAP.RAZOR so it survives desk switches;
    /// this component only reads and adds to it.</summary>
    [Parameter, EditorRequired] public IntelLedger Ledger { get; set; } = new();

    /// <summary>True when the player is currently at a body where <c>IntelMarket.CanTradeIntelAt</c>
    /// holds — computed by Map.razor from its own orbit/dock state so this component never needs
    /// to know how "docked" or "bound" works.</summary>
    [Parameter] public bool CanTrade { get; set; }
    [Parameter] public string? DisabledReason { get; set; }

    /// <summary>Heliocentric distance-from-Earth of the current point of sale, for pricing.</summary>
    [Parameter] public double TradeLocationDistanceFromEarthMeters { get; set; }

    [Parameter] public IReadOnlyList<MarketShip> MarketShips { get; set; } = [];
    [Parameter] public IReadOnlyList<TrackedShipInfo> TrackedShips { get; set; } = [];

    /// <summary>PR-WIRE — a wire-capable contact for the favor-bank panel: their id (for the callback),
    /// display name, and signed balance (+ = they hold our coin; − = we owe them).</summary>
    public readonly record struct WireContact(string ContactId, string DisplayName, long Balance);

    /// <summary>The dark-web-native contacts the player has history with — projected by Map. Empty until
    /// the player has done business with a wire-capable fixer.</summary>
    [Parameter] public IReadOnlyList<WireContact> WireContacts { get; set; } = [];

    /// <summary>Raised when the player opens a contact's account over the wire — Map opens the bank card
    /// (channel-checked) for that contact id.</summary>
    [Parameter] public EventCallback<string> OnOpenWireBank { get; set; }

    /// <summary>Raised after a successful intel buy (the callsign bought) — Map.razor drops a
    /// line on the news wire (PR-14); this component never needs to know the wire exists.</summary>
    [Parameter] public EventCallback<string> OnIntelPurchased { get; set; }

    /// <summary>#233 · What this desk's buyer will pay for the roadster's data chip, or null when the
    /// captain is not carrying one. Priced by Map off the contract's own pay
    /// (<see cref="CompromisingChip.FencePrice"/>) — this component never does arithmetic about it.</summary>
    [Parameter] public int? ChipPrice { get; set; }

    /// <summary>#233 · Raised when the captain sells the chip here. Map moves the coin, takes it out of the
    /// pocket, banks the band of heat and lets the fence speak; the desk only carries the press.</summary>
    [Parameter] public EventCallback OnSellChip { get; set; }

    /// <summary>#1149 · What this desk's fence wants for a set of inspectorate credentials, or null when
    /// there is nothing here to sell the captain — he is carrying the one there is, or the desk is not open
    /// for business. Priced by Map off <see cref="Inspectorate.FencePrice"/>; this component never does
    /// arithmetic about it, exactly as it never does any about the chip.</summary>
    [Parameter] public int? InspectorCardPrice { get; set; }

    /// <summary>#1149 · Raised when the captain buys it. Map moves the coin and puts the laminate in the
    /// wallet; the desk only carries the press.</summary>
    [Parameter] public EventCallback OnBuyInspectorCard { get; set; }

    /// <summary>#535 slice 2 · What this desk's fence wants for one black-ops key, or null when there is
    /// nothing here to sell — this port has already dealt its one for the watch, the pocket has no room, or
    /// the desk is shut. Priced by Map off <see cref="BlackOpsKey.FencePrice"/> (three times the BUSTED
    /// card's own bribe, through the very same function); this component never does arithmetic about it,
    /// exactly as it never does any about the chip or the card.</summary>
    [Parameter] public int? KeyPrice { get; set; }

    /// <summary>#535 slice 2 · The price as the desk prints it, composed by Map in the one credit typography
    /// its three off-the-books rows share. Passed as text rather than re-formatted here so this desk cannot
    /// grow a second way of writing a number.</summary>
    [Parameter] public string KeyPriceText { get; set; } = "";

    /// <summary>#535 slice 2 · Raised when the captain buys the key. Map moves the coin, puts it in the
    /// pocket and strikes this port off for the watch; the desk only carries the press.</summary>
    [Parameter] public EventCallback OnBuyKey { get; set; }

    /// <summary>#711 slice 1 · Whether there is an unlisted parcel to be had across this desk — the desk is
    /// open, the captain is not already carrying one, and the pocket has room. A bool rather than a price
    /// because no coin moves: a parcel is handed over, and what it costs is not credits. Map answers it;
    /// this component does no arithmetic about it, exactly as it does none about the other three rows.</summary>
    [Parameter] public bool ParcelOnOffer { get; set; }

    /// <summary>#711 slice 1 · Raised when the captain takes the parcel. Map puts it in the pocket; the desk
    /// only carries the press.</summary>
    [Parameter] public EventCallback OnTakeParcel { get; set; }

    private IEnumerable<TrackedShipInfo> SellableTracks =>
        TrackedShips.Where(t => IntelMarket.CanSellTrack(t.Quality));

    protected override void OnParametersSet() => Ledger.PruneStale(SimTime);

    private int BuyPriceFor(MarketShip ship) =>
        IntelMarket.BuyPrice(ship.CargoUnits * CargoMarket.UnitValue(ship.CargoClass), TradeLocationDistanceFromEarthMeters);

    private int SellPriceFor(TrackedShipInfo t) =>
        IntelMarket.SellPrice(t.Quality, t.CargoUnits * CargoMarket.UnitValue(t.CargoClass));

    private async Task BuyIntel(MarketShip ship, int price)
    {
        if (Ledger.Knows(ship.Id, SimTime) || Credits < price)
        {
            return;
        }

        await SpendCredits(-price);
        Ledger.Add(new RouteIntel(ship.Id, SimTime, RouteIntel.DefaultValiditySeconds, price));
        await OnIntelPurchased.InvokeAsync(ship.Callsign);
    }

    private async Task SellTrack(TrackedShipInfo t, int price)
    {
        if (price <= 0)
        {
            return;
        }

        await SpendCredits(price);
    }

    private async Task SpendCredits(int delta)
    {
        Credits += delta;
        await CreditsChanged.InvokeAsync(Credits);
    }
}
