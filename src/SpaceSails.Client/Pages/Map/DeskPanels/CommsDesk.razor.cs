using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using CargoManifestEntry = SpaceSails.Client.Pages.Map.CargoManifestEntry;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using ScopeIntel = SpaceSails.Client.Pages.Map.ScopeIntel;
using ShipBot = SpaceSails.Client.Pages.Map.ShipBot;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// CommsDesk — the code-behind for DeskPanels/CommsDesk.razor.
//
// #251 · Every [Parameter] below is DeskPanels' own declaration, copied line for line with its doc: the
// same member under the same name and type, so the moved markup reads exactly what it read on the desk.
public partial class CommsDesk
{
    /// <summary>the page's `string? _commsSelectedId` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_commsSelectedId` and the assignment still lands on the page.</summary>
    [Parameter] public string? _commsSelectedIdValue { get; set; }
    /// <summary>The page's `_commsSelectedId = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<string?> _commsSelectedIdSet { get; set; } = default!;
    private string? _commsSelectedId { get => _commsSelectedIdValue; set { _commsSelectedIdValue = value; _commsSelectedIdSet(value); } }
    /// <summary>the page's `int _credits` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_credits` and the assignment still lands on the page.</summary>
    [Parameter] public int _creditsValue { get; set; }
    /// <summary>The page's `_credits = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<int> _creditsSet { get; set; } = default!;
    private int _credits { get => _creditsValue; set { _creditsValue = value; _creditsSet(value); } }
    [Parameter] public Func<int, NewsWire.NewsScope, string?, IReadOnlyList<NewsWire.NewsItem>> OnNewsFeed { get; set; } = default!;
    private IReadOnlyList<NewsWire.NewsItem> NewsFeed(int ambientCount,
        NewsWire.NewsScope scope = NewsWire.NewsScope.SystemWire, string? salt = null)
        => OnNewsFeed(ambientCount, scope, salt);
    [Parameter] public Action<string, bool> OnOpenBank { get; set; } = default!;
    private void OpenBank(string contactId, bool viaWire) => OnOpenBank(contactId, viaWire);
    [Parameter] public Action<NewsWire.NewsEventKind, string, string?> OnPushNewsEvent { get; set; } = default!;
    private void PushNewsEvent(NewsWire.NewsEventKind kind, string subject, string? detail = null)
        => OnPushNewsEvent(kind, subject, detail);
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    /// <summary>#1149 · Buy it. Map moves the coin and puts the laminate in the wallet.</summary>
    [Parameter] public Action BuyTheInspectorCard { get; set; } = default!;
    /// <summary>#535 slice 2 · Buy it — Map moves the coin and strikes the port off for the watch.</summary>
    [Parameter] public Action BuyTheKeyFromTheFence { get; set; } = default!;
    [Parameter] public Func<int?> ChipFencePrice { get; set; } = default!;
    [Parameter] public Func<NpcState, bool> CommsCanFence { get; set; } = default!;
    [Parameter] public Func<NpcState, int?> CommsFencePrice { get; set; } = default!;
    [Parameter] public Func<List<(string Label, List<NpcState> Members)>> CommsGroups { get; set; } = default!;
    [Parameter] public Action<NpcState> CommsHail { get; set; } = default!;
    [Parameter] public Action<NpcState> CommsLaserRange { get; set; } = default!;
    [Parameter] public Action<NpcState> CommsSellTrack { get; set; } = default!;
    [Parameter] public Func<NpcState, (string Label, string Css)> CommsStatusBadge { get; set; } = default!;
    [Parameter] public int CommsTickerAmbientDays { get; set; }
    [Parameter] public int CommsTickerItemCount { get; set; }
    [Parameter] public Func<bool> DarkWebCanTrade { get; set; } = default!;
    [Parameter] public Func<string> DarkWebDisabledReason { get; set; } = default!;
    [Parameter] public Func<double> DarkWebDistanceFromEarth { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.MarketShip>> DarkWebMarketShips { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.TrackedShipInfo>> DarkWebTrackedShips { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<SpaceSails.Client.Pages.Stations.DarkWeb.WireContact>> DarkWebWireContacts { get; set; } = default!;
    [Parameter] public Func<NpcShip, string> DepartureLabel { get; set; } = default!;
    [Parameter] public Func<string, NpcState?> FindNpc { get; set; } = default!;
    [Parameter] public Func<double, string> FormatSimTime { get; set; } = default!;
    /// <summary>#319 slice 2 · The location the desk would sell — Map's own <c>GeocacheOnOffer</c>, null
    /// when there is nothing to sell.</summary>
    [Parameter] public Func<SpaceSails.Client.Pages.Stations.DarkWeb.GeocacheOffer?> GeocacheOnOffer { get; set; } = default!;
    /// <summary>#1149 · What the dark-web fence wants for a set of inspectorate credentials, or null when
    /// there is nothing to sell — Map's own <c>InspectorCardPrice</c>, derived and never typed.</summary>
    [Parameter] public Func<int?> InspectorCardPrice { get; set; } = default!;
    [Parameter] public Func<string, double> IntelStaleInDays { get; set; } = default!;
    [Parameter] public int OffBooksCount { get; set; }
    /// <summary>#711 slice 1 · Whether the desk has an unlisted parcel to hand over — Map's own
    /// <c>ParcelOnOffer</c>. A bool and not a price: no coin moves through that row.</summary>
    [Parameter] public Func<bool> ParcelOnOffer { get; set; } = default!;
    [Parameter] public Action<ScopeIntel> PointScopeWhereIntelSays { get; set; } = default!;
    [Parameter] public Func<NpcShip, string> RouteLabel { get; set; } = default!;
    [Parameter] public Func<string, ScopeIntel?> ScopeIntelById { get; set; } = default!;
    [Parameter] public Action<string> SelectCommsShip { get; set; } = default!;
    [Parameter] public Func<NpcState?> SelectedTrackedTarget { get; set; } = default!;
    [Parameter] public Action SellTheChipToTheFence { get; set; } = default!;
    /// <summary>#319 slice 2 · Send the coordinates. Map writes the sale; nothing else moves.</summary>
    [Parameter] public Action SellTheLocation { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Action<ShipDesk> SwitchDesk { get; set; } = default!;
    /// <summary>#711 slice 1 · Take it. Map puts it in the pocket; nothing else moves.</summary>
    [Parameter] public Action TakeTheUnlistedParcel { get; set; } = default!;
    /// <summary>#535 slice 2 · What the desk's fence wants for one black-ops key, or null when this port has
    /// already dealt its one for the watch — Map's own <c>TheFencesKeyPrice</c>, three times the BUSTED
    /// card's bribe through that card's own function and never typed.</summary>
    [Parameter] public Func<int?> TheFencesKeyPrice { get; set; } = default!;
    /// <summary>#535 slice 2 · That price in the desk's one credit typography, composed by Map.</summary>
    [Parameter] public Func<string> TheFencesKeyPriceText { get; set; } = default!;
    /// <summary>#711 slice 2 · The ground the box already in the pocket is going to, as the desk prints it —
    /// Map's own <c>TheParcelsDestinationRow</c>, empty while no parcel is carried.</summary>
    [Parameter] public Func<string> TheParcelsDestinationRow { get; set; } = default!;
    [Parameter] public EventCallback TogglePin { get; set; }
    [Parameter] public int Warp { get; set; }
    [Parameter] public string? _commsActionMessage { get; set; }
    /// <summary>#1304 · The page's pulse slot, for the one desk that settles money on it (the dark web).</summary>
    [Parameter] public PulseSlot _pulse { get; set; } = PulseSlot.Empty;
    [Parameter] public string? _commsHailAnswer { get; set; }
    [Parameter] public IntelLedger _intelLedger { get; set; } = default!;
    [Parameter] public bool _pinned { get; set; }
    [Parameter] public List<ScopeIntel> _scopeIntel { get; set; } = default!;
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public SpaceSails.Client.Pages.Stations.TrackingPost? _trackingPost { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
