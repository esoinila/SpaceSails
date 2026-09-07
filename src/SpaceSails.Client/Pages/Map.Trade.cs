using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Trade — the coin: the market and cargo hold, fuel and upgrades, rescue, the dark web,
// the trade corridors and local-space dealing. Lifted from Map.razor for #251, motion only.

/// <summary>
/// #251 · WHAT THE SHIP OWNS — the manifest the Trade desk reads, the purse, the hold, and the three
/// upgrade tracks with the price ladder they climb.
///
/// <para>This is the opening file of a six-part family, and the other five are named for the counter they
/// stand at: <c>.Lanes</c> (the corridors' geometry, kept for the telescope after #953 archived the
/// drawing), <c>.Pump</c> (selling, refuelling, restocking, the wire lender's favour and the honest
/// directions when there is no pump here), <c>.Yard</c> (upgrades, rescue, and what the cargo needs
/// next), <c>.DarkWeb</c> (PR-6's market), and <c>.LocalSpace</c> (PR-5's orbital commerce).</para>
///
/// <para>The three static fields in the family — <c>LocalContactRingColor</c>, <c>MarketBodies</c> and
/// <c>StartingManifest</c> — are declared HERE, in the opening file, and every one of them reads nothing
/// but literals. See <c>NoPartialClassSpreadsItsStaticFieldsTests</c> for why that is the thing to check
/// and not "are the statics spread". No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public partial class Map
{
    /// <summary>One line of the Trade desk's cargo manifest panel (PR-13): what's actually in the
    /// hold, broken down by class, with its fence value via <see cref="CargoMarket"/> — a
    /// read-model over <see cref="_cargoByClass"/>, which Board() keeps in step with the existing
    /// aggregate _cargoUnits/_cargoValue totals.</summary>
    public readonly record struct CargoManifestEntry(string CargoClass, int Units, int Value);

    private IReadOnlyList<CargoManifestEntry> CargoManifest()
    {
        var list = new List<CargoManifestEntry>();
        foreach ((string cargoClass, int units) in _cargoByClass)
        {
            if (units <= 0)
            {
                continue;
            }

            list.Add(new CargoManifestEntry(cargoClass, units, units * CargoMarket.UnitValue(cargoClass)));
        }

        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        return list;
    }

    // ---- PR-5: orbital commerce — trade from orbit or course-matched with drones ----
    private SpaceSails.Client.Pages.Stations.LocalSpace? _localSpace;
    private string? _localTradeTargetId;                // LocalContact id mid-transfer, if any
    private double _localTradeProgress;                  // drone transfer progress fraction [0,1)
    private string? _localTradeMessage;
    private static readonly RgbaColor LocalContactRingColor = new(120, 200, 255, 150);
    private static readonly string[] MarketBodies = ["earth", "mars", "venus"];

    // The captain's starting book (owner: "It is an operating ship with some history" — the
    // same world-does-not-wait principle as the populated sky at t=0). The purse is the last
    // run's takings; the hold carries that gig's leftovers, mixed classes like a real
    // manifest, not a bare hull. 1,500 cr covers Earth Depot's whole stock via shuttles
    // (4×250 + 100 cr fee) with change, and stays deliberately below the 2,000 cr upgrade
    // price so the first pod run remains the tutorial's real payday.
    private const int StartingCredits = 1500;
    private static readonly (string Class, int Units)[] StartingManifest =
        [("Alloys", 2), ("Ice", 3)];

    private int _credits;
    private int _cargoUnits;
    private int _cargoValue;
    // Trade desk cargo manifest (PR-13): a per-class breakdown of the hold, alongside the
    // pre-existing aggregate _cargoUnits/_cargoValue totals every other flow (SellCargo, drone
    // trade, Adrift/hunter-catch confiscation) already reads/clears. Additive bookkeeping only —
    // Board() is still the single place cargo is granted, so this dictionary and the totals never
    // drift apart.
    private readonly Dictionary<string, int> _cargoByClass = [];
    private int _massLevel;                            // reaction-mass capacity: 250 + 150/level
    private int _sensorLevel;                          // sensor range: base × 1.4/level
    private int _holdLevel;                            // cargo hold: 10 + 10/level

    private int ReactionMassCapacity => 500 + 150 * _massLevel;
    private int CargoCapacity => 10 + 10 * _holdLevel;
    private static int UpgradePrice(int level) => 2000 * (1 << level);
}
