using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using CargoManifestEntry = SpaceSails.Client.Pages.Map.CargoManifestEntry;
using NpcState = SpaceSails.Client.Pages.Map.NpcState;
using ScopeIntel = SpaceSails.Client.Pages.Map.ScopeIntel;
using ShipBot = SpaceSails.Client.Pages.Map.ShipBot;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// TradeDesk — the code-behind for DeskPanels/TradeDesk.razor.
//
// #251 · Every [Parameter] below is DeskPanels' own declaration, copied line for line with its doc: the
// same member under the same name and type, so the moved markup reads exactly what it read on the desk.
public partial class TradeDesk
{
    /// <summary>the page's `int _credits` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_credits` and the assignment still lands on the page.</summary>
    [Parameter] public int _creditsValue { get; set; }
    /// <summary>The page's `_credits = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<int> _creditsSet { get; set; } = default!;
    private int _credits { get => _creditsValue; set { _creditsValue = value; _creditsSet(value); } }
    /// <summary>the page's `SpaceSails.Client.Pages.Stations.LocalSpace? _localSpace` — and this markup WRITES it, so it crosses as a pair: the
    /// value in, the page's own setter out. The private property below keeps the member's own name, so
    /// the moved block still assigns to `_localSpace` and the assignment still lands on the page.</summary>
    [Parameter] public SpaceSails.Client.Pages.Stations.LocalSpace? _localSpaceValue { get; set; }
    /// <summary>The page's `_localSpace = value`, handed down so the write survives the move.</summary>
    [Parameter] public Action<SpaceSails.Client.Pages.Stations.LocalSpace?> _localSpaceSet { get; set; } = default!;
    private SpaceSails.Client.Pages.Stations.LocalSpace? _localSpace { get => _localSpaceValue; set { _localSpaceValue = value; _localSpaceSet(value); } }
    [Parameter] public Func<string?> BestWireLenderId { get; set; } = default!;
    [Parameter] public Func<string, string> BodyName { get; set; } = default!;
    [Parameter] public EventCallback BuyExtendedTank { get; set; }
    [Parameter] public Action<int> BuyFuel { get; set; } = default!;
    [Parameter] public EventCallback BuyMedKitRefill { get; set; }
    [Parameter] public EventCallback BuyNetJammer { get; set; }
    [Parameter] public Action<string> BuyUpgrade { get; set; } = default!;
    [Parameter] public EventCallback CallInFavorAtPump { get; set; }
    [Parameter] public int CargoCapacity { get; set; }
    [Parameter] public Func<IReadOnlyList<CargoManifestEntry>> CargoManifest { get; set; } = default!;
    [Parameter] public Func<bool> ChandleryOpen { get; set; } = default!;
    [Parameter] public Func<int> ChandleryPillsMissing { get; set; } = default!;
    [Parameter] public Func<int> ChandleryRefillPrice { get; set; } = default!;
    [Parameter] public Func<int> ChandleryTankPrice { get; set; } = default!;
    [Parameter] public Func<int> ChandleryTanksAboard { get; set; } = default!;
    [Parameter] public Func<string?> DockedBodyName { get; set; } = default!;
    [Parameter] public Func<double, string> FormatDistance { get; set; } = default!;
    [Parameter] public Func<string> FuelDirectionsLine { get; set; } = default!;
    [Parameter] public Func<IReadOnlyList<CommerceRule.LocalContact>> LocalContacts { get; set; } = default!;
    [Parameter] public string? LocalSpaceBodyId { get; set; }
    [Parameter] public int NetJammerPriceCr { get; set; }
    [Parameter] public Func<int, int, long> PumpLoanPrincipal { get; set; } = default!;
    [Parameter] public int ReactionMassCapacity { get; set; }
    [Parameter] public EventCallback RestockSentries { get; set; }
    [Parameter] public EventCallback SellCargo { get; set; }
    [Parameter] public Func<int> SentryRestockCost { get; set; } = default!;
    [Parameter] public Func<int> SentryRoundsMissing { get; set; } = default!;
    [Parameter] public double SimTime { get; set; }
    [Parameter] public Action<string> StartLocalBuy { get; set; } = default!;
    [Parameter] public Action<string> StartLocalTrade { get; set; } = default!;
    [Parameter] public Action<ShipDesk> SwitchDesk { get; set; } = default!;
    [Parameter] public Func<int, int> UpgradePrice { get; set; } = default!;
    [Parameter] public int _cargoUnits { get; set; }
    [Parameter] public int _cargoValue { get; set; }
    [Parameter] public string? _dockBodyId { get; set; }
    [Parameter] public bool _docked { get; set; }
    [Parameter] public string? _dockedHavenId { get; set; }
    [Parameter] public ICelestialEphemeris? _ephemeris { get; set; }
    [Parameter] public bool _hasNetJammer { get; set; }
    [Parameter] public int _holdLevel { get; set; }
    [Parameter] public string? _localTradeMessage { get; set; }
    [Parameter] public double _localTradeProgress { get; set; }
    [Parameter] public string? _localTradeTargetId { get; set; }
    [Parameter] public int _massLevel { get; set; }
    [Parameter] public string? _orbitedBodyId { get; set; }
    [Parameter] public int _reactionMassPulses { get; set; }
    [Parameter] public int _sensorLevel { get; set; }
    [Parameter] public ShipState _ship { get; set; } = default!;
    [Parameter] public List<ShipBot> _shipBots { get; set; } = default!;
    [Parameter] public int _telescopeLevel { get; set; }

    // The page's own event dispatch, repeated: no automatic re-render per event.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}
