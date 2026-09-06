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
using SpaceSails.Client.Components;
using SpaceSails.Client.Layout;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

// LocalSpace — the code-behind for LocalSpace.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of LocalSpace.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class LocalSpace
{
    [Parameter, EditorRequired] public bool Visible { get; set; }
    [Parameter] public string? BodyName { get; set; }
    [Parameter] public string? OrbitBodyId { get; set; }
    [Parameter] public string? ContextBodyId { get; set; }
    [Parameter] public IReadOnlyList<CommerceRule.LocalContact> Contacts { get; set; } = [];
    [Parameter, EditorRequired] public Vector2d ShipPosition { get; set; }
    [Parameter, EditorRequired] public Vector2d ShipVelocity { get; set; }
    [Parameter] public int CargoUnits { get; set; }
    [Parameter] public int CargoValue { get; set; }
    [Parameter] public int Credits { get; set; }
    [Parameter] public int HoldSpaceUnits { get; set; }
    [Parameter] public string? ActiveTradeTargetId { get; set; }
    [Parameter] public double TradeProgress { get; set; }
    [Parameter] public string? TradeMessage { get; set; }
    [Parameter] public bool FullScreen { get; set; }
    [Parameter] public string? DockedBodyId { get; set; }
    [Parameter] public string? DockedBodyName { get; set; }
    [Parameter] public Func<string, string>? BodyNameLookup { get; set; }
    [Parameter] public RenderFragment? DockMarket { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<string> OnStartTrade { get; set; }
    [Parameter] public EventCallback<string> OnStartBuy { get; set; }

    // ---- The tree (master) ----

    /// <summary>One row of the explorer tree. <see cref="ContactId"/> ties action/item nodes
    /// back to their post so the ▶ transfer marker and selection resolution stay simple.</summary>
    private sealed record TreeNode(
        string Id, int Depth, string Icon, string Label,
        string? ContactId = null, string? Badge = null, string BadgeClass = "bg-secondary");

    private string? _selectedId;

    private List<TreeNode> BuildTree()
    {
        var tree = new List<TreeNode>();

        // Hosts nearest-first; the docked body always leads — it's where business is happening.
        List<string> hostIds = Contacts
            .GroupBy(c => c.HostBodyId ?? "?")
            .OrderBy(g => g.Min(ShipDistance))
            .Select(g => g.Key)
            .ToList();
        if (DockedBodyId is { } dockedId)
        {
            hostIds.Remove(dockedId);
            hostIds.Insert(0, dockedId);
        }

        foreach (string hostId in hostIds)
        {
            tree.Add(new TreeNode($"body:{hostId}", 0, "🪐", HostName(hostId)));

            if (hostId == DockedBodyId)
            {
                tree.Add(new TreeNode("dock", 1, "⚓", $"{HostName(hostId)} dockyard",
                    Badge: "docked", BadgeClass: "bg-success"));
            }

            foreach (CommerceRule.LocalContact contact in Contacts
                .Where(c => (c.HostBodyId ?? "?") == hostId)
                .OrderBy(ShipDistance))
            {
                CommerceRule.TradeMode mode = TradeModeFor(contact);
                (string badge, string badgeClass) = (contact.Actions & CommerceRule.ActionKind.Trade) == 0
                    ? ("board", "bg-secondary")
                    : mode switch
                    {
                        CommerceRule.TradeMode.SameOrbit => ("same orbit", "bg-success"),
                        CommerceRule.TradeMode.DroneMatch => ("drones", "bg-success"),
                        CommerceRule.TradeMode.Shuttle => ("shuttles", "bg-info text-dark"),
                        _ => ("out of reach", "bg-secondary"),
                    };
                tree.Add(new TreeNode(contact.Id, 1, Icon(contact.Kind), contact.Name, contact.Id, badge, badgeClass));

                if ((contact.Actions & CommerceRule.ActionKind.Trade) == 0)
                {
                    continue;
                }

                if (contact.CargoUnits > 0 && contact.CargoClass is { } stockClass)
                {
                    tree.Add(new TreeNode($"buy:{contact.Id}", 2, "🛒", "Buy", contact.Id));
                    tree.Add(new TreeNode($"item:{contact.Id}", 3, "📦",
                        $"{stockClass} × {contact.CargoUnits}", contact.Id));
                }

                tree.Add(new TreeNode($"sell:{contact.Id}", 2, "📤", "Sell your hold here", contact.Id));
            }
        }

        return tree;
    }

    /// <summary>The selection, healed: falls back to the dockyard (when docked) or the first
    /// post when the remembered node no longer exists (stock sold out, contact left reach).</summary>
    private string EffectiveSelection(List<TreeNode> tree)
    {
        if (_selectedId is { } id && tree.Any(n => n.Id == id))
        {
            return id;
        }

        return tree.FirstOrDefault(n => n.Id == "dock")?.Id
            ?? tree.FirstOrDefault(n => n.ContactId is not null)?.Id
            ?? tree.FirstOrDefault()?.Id
            ?? "";
    }

    private string HostName(string hostId) =>
        hostId == "?" ? "Deep space" : BodyNameLookup?.Invoke(hostId) ?? hostId;

    /// <summary>The clickable path to the selection — every segment is a real tree node id.</summary>
    private List<(string Id, string Label)> Breadcrumbs(string selectedId)
    {
        var crumbs = new List<(string, string)>();
        if (selectedId == "dock")
        {
            if (DockedBodyId is { } dockedId)
            {
                crumbs.Add(($"body:{dockedId}", HostName(dockedId)));
            }

            crumbs.Add(("dock", "Dockyard"));
            return crumbs;
        }

        if (selectedId.StartsWith("body:"))
        {
            crumbs.Add((selectedId, HostName(selectedId["body:".Length..])));
            return crumbs;
        }

        if (FindContact(selectedId) is not { } contact)
        {
            return crumbs;
        }

        crumbs.Add(($"body:{contact.HostBodyId ?? "?"}", HostName(contact.HostBodyId ?? "?")));
        crumbs.Add((contact.Id, contact.Name));
        switch (SectionOf(selectedId))
        {
            case "buy":
                crumbs.Add(($"buy:{contact.Id}", "Buy"));
                break;
            case "item":
                crumbs.Add(($"buy:{contact.Id}", "Buy"));
                crumbs.Add(($"item:{contact.Id}", contact.CargoClass ?? "item"));
                break;
            case "sell":
                crumbs.Add(($"sell:{contact.Id}", "Sell"));
                break;
        }

        return crumbs;
    }

    private static string SectionOf(string selectedId) =>
        selectedId.StartsWith("buy:") ? "buy"
        : selectedId.StartsWith("item:") ? "item"
        : selectedId.StartsWith("sell:") ? "sell"
        : "post";

    private CommerceRule.LocalContact? FindContact(string selectedId)
    {
        string contactId = SectionOf(selectedId) == "post"
            ? selectedId
            : selectedId[(selectedId.IndexOf(':') + 1)..];
        foreach (CommerceRule.LocalContact c in Contacts)
        {
            if (c.Id == contactId)
            {
                return c;
            }
        }

        return null;
    }

    // ---- Detail-pane copy (strings in code, markup stays thin) ----

    private string OutOfReachText =>
        $"out of reach — close within {FormatDistance(CommerceRule.ShuttleRangeMeters)} under {CommerceRule.ShuttleMaxRelativeSpeed / 1000:F0} km/s rel to send shuttles, or match orbit to trade dockside";

    private string BuyButtonLabel(
        CommerceRule.TradeMode mode, CommerceRule.LocalContact contact,
        int buyUnits, int unitValue, double relSpeed, double distance)
    {
        if (buyUnits <= 0)
        {
            return $"🛒 Buy — {(HoldSpaceUnits <= 0 ? "hold full" : "not enough credits (price + ferry fee)")}";
        }

        int cost = CommerceRule.BuyCostCr(mode, buyUnits, unitValue);
        return mode switch
        {
            CommerceRule.TradeMode.SameOrbit => $"🛒 Buy {buyUnits}u dockside now — {cost:N0} cr · no fee",
            CommerceRule.TradeMode.DroneMatch =>
                $"🛒 Buy {buyUnits}u via drones now — {cost:N0} cr (incl {CommerceRule.TransferFeeCr(mode, buyUnits):N0} cr fee) · ~{EstimateMinutes(mode, relSpeed, distance, buyUnits)} min",
            _ =>
                $"🛒 Buy {buyUnits}u via shuttles now — {cost:N0} cr (incl {CommerceRule.TransferFeeCr(mode, buyUnits):N0} cr fee) · ~{EstimateMinutes(mode, relSpeed, distance, buyUnits)} min",
        };
    }

    private string SellButtonLabel(CommerceRule.TradeMode mode, double relSpeed, double distance)
    {
        int payout = CommerceRule.SellPayoutCr(mode, CargoUnits, CargoValue);
        return mode switch
        {
            CommerceRule.TradeMode.SameOrbit => $"📤 Sell hold dockside now — +{payout:N0} cr · no fee",
            CommerceRule.TradeMode.DroneMatch =>
                $"📤 Sell hold via drones now — +{payout:N0} cr · ~{EstimateMinutes(mode, relSpeed, distance, CargoUnits)} min",
            _ =>
                $"📤 Sell hold via shuttles now — +{payout:N0} cr (after {CommerceRule.TransferFeeCr(mode, CargoUnits):N0} cr fee) · ~{EstimateMinutes(mode, relSpeed, distance, CargoUnits)} min",
        };
    }

    // ---- Shared helpers (unchanged mechanics) ----

    // Depots/stations/havens are on rails at ContextBodyId — definitionally "in orbit" there, so
    // they satisfy CommerceRule's same-body case whenever the player is bound to that body too.
    // A passing Ship contact isn't assumed to share an orbit — it only trades when genuinely
    // course-matched or (M29) inside shuttle reach.
    private CommerceRule.TradeMode TradeModeFor(CommerceRule.LocalContact contact)
    {
        string? partnerOrbitBodyId = contact.Kind == CommerceRule.LocalContactKind.Ship ? null : ContextBodyId;
        var player = new ShipState(ShipPosition, ShipVelocity, 0);
        return CommerceRule.Classify(player, contact.Position, contact.Velocity, OrbitBodyId, partnerOrbitBodyId);
    }

    private double ShipDistance(CommerceRule.LocalContact contact) =>
        (contact.Position - ShipPosition).Length;

    private static int EstimateMinutes(CommerceRule.TradeMode mode, double relSpeed, double distance, int units) =>
        Math.Max(1, (int)Math.Ceiling(
            CommerceRule.TransferSeconds(mode, relSpeed, distance, Math.Max(1, units)) / 60));

    private static string Icon(CommerceRule.LocalContactKind kind) => kind switch
    {
        CommerceRule.LocalContactKind.Depot => "🛰",
        CommerceRule.LocalContactKind.Station => "🏭",
        CommerceRule.LocalContactKind.Moon => "🌙",
        CommerceRule.LocalContactKind.Haven => "🏴",
        CommerceRule.LocalContactKind.Ship => "🚀",
        _ => "•",
    };

    private static string FormatDistance(double meters)
    {
        const double metersPerAu = 1.495978707e11;
        if (meters >= metersPerAu / 10)
            return $"{meters / metersPerAu:F2} AU";
        if (meters >= 1e9)
            return $"{meters / 1e9:F2} M km";
        return $"{meters / 1000:F0} km";
    }
}
